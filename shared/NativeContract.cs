using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;

namespace PartyViewKR.Compatibility;

// Reads PE metadata only: never loads or executes the file being inspected.
internal sealed record NativeContract(int Schema, int ApiLevel, string AssemblyName,
    string BuildVersion, Guid BuildMvid, Dictionary<string, string[]> Types,
    SortedDictionary<string, string> Entries)
{
    internal static NativeContract Parse(Stream stream) =>
        JsonSerializer.Deserialize<NativeContract>(stream) ?? throw new InvalidDataException("empty_contract");

    internal static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    internal static NativeContract Read(string path, Dictionary<string, string[]> types, int apiLevel)
    {
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var pe = new PEReader(file);
        var md = pe.GetMetadataReader();
        var names = new MetadataNames(md);
        var entries = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var selected = new Dictionary<string, string[]>(types, StringComparer.Ordinal);
        var definitions = md.TypeDefinitions.ToDictionary(h => names.Definition(h));
        var queue = new Queue<string>(selected.Keys);
        while (queue.TryDequeue(out var name))
        {
            if (!definitions.TryGetValue(name, out var handle)) continue; // comparator reports missing type
            var type = md.GetTypeDefinition(handle);
            var layout = type.GetLayout();
            entries[$"type:{name}"] = $"size={layout.Size};pack={layout.PackingSize};layout={type.Attributes & TypeAttributes.LayoutMask};" +
                Attributes(md, names, type.GetCustomAttributes());
            // Sequential structs have offset=-1 in metadata, so declaration
            // order is also ABI evidence (including generated inline arrays).
            if ((type.Attributes & TypeAttributes.LayoutMask) == TypeAttributes.SequentialLayout)
                entries[$"layout:{name}.instanceFieldOrder"] = string.Join("|", type.GetFields()
                    .Select(md.GetFieldDefinition).Where(f => (f.Attributes & FieldAttributes.Static) == 0)
                    .Select(f => md.GetString(f.Name)));
            foreach (var fh in type.GetFields())
            {
                var field = md.GetFieldDefinition(fh);
                var fieldName = md.GetString(field.Name);
                // Only enum constants used by the plugin are part of the contract.
                if (name.EndsWith(".AtkEventType") && fieldName is not ("value__" or "ButtonClick" or "MouseOver" or "MouseOut")) continue;
                if (name.EndsWith(".NodeType") && fieldName is not ("value__" or "Text")) continue;
                if (name.EndsWith("+DutyCategory") && fieldName is not ("value__" or "PvP")) continue;
                var signature = field.DecodeSignature(names, (object?)null);
                var constant = "";
                if (!field.GetDefaultValue().IsNil)
                {
                    var value = md.GetConstant(field.GetDefaultValue());
                    constant = $";constant={value.TypeCode}:{Convert.ToHexString(md.GetBlobBytes(value.Value))}";
                }
                entries[$"field:{name}.{fieldName}"] = $"{signature};offset={field.GetOffset()};flags={(int)field.Attributes}{constant};" +
                    Attributes(md, names, field.GetCustomAttributes());
                // Generated inline array lengths/element types affect inlined indexing.
                foreach (var candidate in definitions.Keys.Where(n => n.Contains(".FixedSizeArray", StringComparison.Ordinal)))
                    if (signature.Contains(candidate, StringComparison.Ordinal) && selected.TryAdd(candidate, [])) queue.Enqueue(candidate);
            }
            var required = selected[name];
            foreach (var mh in type.GetMethods())
            {
                var method = md.GetMethodDefinition(mh);
                var methodName = md.GetString(method.Name);
                if (!required.Contains(methodName, StringComparer.Ordinal)) continue;
                var signature = method.DecodeSignature(names, (object?)null);
                var key = $"method:{name}.{methodName}({string.Join(",", signature.ParameterTypes)})";
                entries[key] = $"{signature.ReturnType};header={signature.Header.RawValue};generic={signature.GenericParameterCount};" +
                    Attributes(md, names, method.GetCustomAttributes());
            }
        }
        var assembly = md.GetAssemblyDefinition();
        return new(1, apiLevel, md.GetString(assembly.Name), assembly.Version.ToString(),
            md.GetGuid(md.GetModuleDefinition().Mvid), selected, entries);
    }

    internal string[] Differences(NativeContract actual)
    {
        var differences = new List<string>();
        if (Schema != 1 || actual.Schema != 1) differences.Add("schema");
        if (AssemblyName is not ("FFXIVClientStructs" or "InteropGenerator.Runtime") || actual.AssemblyName != AssemblyName) differences.Add("assembly");
        if (ApiLevel != actual.ApiLevel) differences.Add("dalamud_api");
        if (Entries.Count == 0) differences.Add("empty_contract");
        foreach (var (key, expected) in Entries)
            if (!actual.Entries.TryGetValue(key, out var value) || value != expected) differences.Add(key);
        // Build number and MVID are deliberately informational, not compatibility requirements.
        // Additions are allowed; required fields/methods/constants must still match exactly.
        return differences.ToArray();
    }

    private static string Attributes(MetadataReader md, MetadataNames names, CustomAttributeHandleCollection handles)
    {
        var values = new List<string>();
        foreach (var h in handles)
        {
            var attribute = md.GetCustomAttribute(h);
            var owner = attribute.Constructor.Kind == HandleKind.MemberReference
                ? md.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent
                : (EntityHandle)md.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType();
            var name = names.Entity(owner);
            // Native signature byte patterns are resolved by the loaded library, not baked into our code.
            // Structural inheritance, inline array lengths, and virtual slots DO affect the ABI.
            if (name.Contains("InheritsAttribute", StringComparison.Ordinal) || name.EndsWith(".InlineArrayAttribute") ||
                name.EndsWith(".VirtualFunctionAttribute") || name.EndsWith(".FixedBufferAttribute"))
                values.Add(name + ":" + Convert.ToHexString(md.GetBlobBytes(attribute.Value)));
        }
        return string.Join(";", values.Order(StringComparer.Ordinal));
    }
}

internal sealed class MetadataNames(MetadataReader reader) : ISignatureTypeProvider<string, object?>
{
    internal string Definition(TypeDefinitionHandle h)
    {
        var t = reader.GetTypeDefinition(h);
        return !t.GetDeclaringType().IsNil ? Definition(t.GetDeclaringType()) + "+" + reader.GetString(t.Name) :
            Join(reader.GetString(t.Namespace), reader.GetString(t.Name));
    }
    private string Reference(TypeReferenceHandle h)
    {
        var t = reader.GetTypeReference(h);
        return t.ResolutionScope.Kind == HandleKind.TypeReference ? Reference((TypeReferenceHandle)t.ResolutionScope) + "+" + reader.GetString(t.Name) :
            Join(reader.GetString(t.Namespace), reader.GetString(t.Name));
    }
    private static string Join(string ns, string name) => ns.Length == 0 ? name : ns + "." + name;
    internal string Entity(EntityHandle h) => h.Kind switch {
        HandleKind.TypeDefinition => Definition((TypeDefinitionHandle)h),
        HandleKind.TypeReference => Reference((TypeReferenceHandle)h),
        HandleKind.TypeSpecification => GetTypeFromSpecification(reader, null, (TypeSpecificationHandle)h, 0),
        _ => h.Kind.ToString()
    };
    public string GetArrayType(string x, ArrayShape s) => $"{x}[rank={s.Rank};sizes={string.Join(',', s.Sizes)};bounds={string.Join(',', s.LowerBounds)}]";
    public string GetByReferenceType(string x) => x + "&";
    public string GetFunctionPointerType(MethodSignature<string> s) => $"fnptr({s.Header.RawValue};{s.ReturnType};{string.Join(',', s.ParameterTypes)})";
    public string GetGenericInstantiation(string x, ImmutableArray<string> args) => x + "<" + string.Join(',', args) + ">";
    public string GetGenericMethodParameter(object? context, int index) => "!!" + index;
    public string GetGenericTypeParameter(object? context, int index) => "!" + index;
    public string GetModifiedType(string modifier, string x, bool required) => $"{x}:mod({required},{modifier})";
    public string GetPinnedType(string x) => x + ":pinned";
    public string GetPointerType(string x) => x + "*";
    public string GetPrimitiveType(PrimitiveTypeCode code) => code.ToString();
    public string GetSZArrayType(string x) => x + "[]";
    public string GetTypeFromDefinition(MetadataReader r, TypeDefinitionHandle h, byte kind) => Definition(h);
    public string GetTypeFromReference(MetadataReader r, TypeReferenceHandle h, byte kind) => Reference(h);
    public string GetTypeFromSpecification(MetadataReader r, object? context, TypeSpecificationHandle h, byte kind) => r.GetTypeSpecification(h).DecodeSignature(this, context);
}
