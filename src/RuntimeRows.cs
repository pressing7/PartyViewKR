using System.Reflection;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;

namespace PartyViewKR;

// The uploaded KR Dalamud references Lumina 1.0.0.0, whereas the public
// distribution references Lumina 7.0.0.0. Resolve the runtime's public RowRef
// and ExcelSheet APIs instead of compiling against the wrong Lumina assembly.
// No memory offsets, network access, or game-state writes are used here.
internal sealed class RuntimeRows
{
    internal static readonly PropertyInfo MemberWorld = typeof(IPartyMember).GetProperty("World")!;
    internal static readonly PropertyInfo MemberJob = typeof(IPartyMember).GetProperty("ClassJob")!;
    internal static readonly PropertyInfo PlayerWorld = typeof(IPlayerState).GetProperty("HomeWorld")!;
    internal static readonly PropertyInfo PlayerJob = typeof(IPlayerState).GetProperty("ClassJob")!;
    private readonly Dictionary<uint, string> worlds = [];
    private readonly Dictionary<uint, string> jobs = [];
    private object? worldSheet;
    private object? jobSheet;
    internal string Error { get; private set; } = "";
    internal void BeginRead() => Error = "";

    internal static uint RowId(PropertyInfo property, object owner)
    {
        var row = property.GetValue(owner) ?? throw new InvalidOperationException($"{property.Name} is null");
        return Convert.ToUInt32(row.GetType().GetProperty("RowId")!.GetValue(row));
    }

    internal string World(uint id) => Lookup(id, MemberWorld, worlds, ref worldSheet, "서버");
    internal string Job(uint id) => id == 0 ? "확인 중" : Lookup(id, MemberJob, jobs, ref jobSheet, "직업");

    private string Lookup(uint id, PropertyInfo property, Dictionary<uint, string> cache, ref object? sheet, string label)
    {
        if (id == 0) return "확인 중";
        if (cache.TryGetValue(id, out var text)) return text;
        try
        {
            if (sheet == null)
            {
                var rowType = property.PropertyType.GenericTypeArguments.Single();
                var getSheet = typeof(IDataManager).GetMethods().Single(m =>
                    m.Name == "GetExcelSheet" && m.IsGenericMethodDefinition && m.GetParameters().Length == 2);
                sheet = getSheet.MakeGenericMethod(rowType).Invoke(Plugin.DataManager, [null, null]);
            }
            var row = sheet!.GetType().GetMethod("GetRow", [typeof(uint)])!.Invoke(sheet, [id]);
            text = row!.GetType().GetProperty("Name")!.GetValue(row)?.ToString();
            if (!string.IsNullOrWhiteSpace(text)) { cache[id] = text; return text; }
        }
        catch (Exception ex) { Error = ex.GetBaseException().Message; }
        return $"{label} #{id}";
    }
}
