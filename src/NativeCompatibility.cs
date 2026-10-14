using System.Reflection;
using System.Runtime.CompilerServices;
using PartyViewKR.Compatibility;

namespace PartyViewKR;

internal static class NativeCompatibility
{
    private sealed record Result(bool Compatible, Guid BuildMvid, Guid LoadedMvid, string Diagnostic);
    private static readonly Lazy<Result> ResultValue = new(Check);
    internal static bool IsCompatible => ResultValue.Value.Compatible;
    internal static Guid BuildMvid => ResultValue.Value.BuildMvid;
    internal static Guid LoadedMvid => ResultValue.Value.LoadedMvid;
    internal static string Diagnostic => ResultValue.Value.Diagnostic;

    private static Result Check()
    {
        Guid build = Guid.Empty, loaded = Guid.Empty;
        try
        {
            using var resource = typeof(NativeCompatibility).Assembly.GetManifestResourceStream("PartyViewKR.NativeContract.json")
                ?? throw new InvalidDataException("contract_missing");
            var expected = NativeContract.Parse(resource);
            build = expected.BuildMvid;
            var assembly = GetNativeAssembly();
            loaded = assembly.ManifestModule.ModuleVersionId;
            var api = typeof(Dalamud.Plugin.IDalamudPlugin).Assembly.GetName().Version?.Major ?? 0;
            var actual = NativeContract.Read(assembly.Location, expected.Types, api);
            if (actual.BuildMvid != loaded)
                return new(false, build, loaded, "nativeCheck=loaded_file_changed");
            using var runtimeResource = typeof(NativeCompatibility).Assembly.GetManifestResourceStream("PartyViewKR.InteropContract.json")
                ?? throw new InvalidDataException("interop_contract_missing");
            var runtimeExpected = NativeContract.Parse(runtimeResource);
            var runtimeAssembly = GetInteropAssembly();
            var runtimeActual = NativeContract.Read(runtimeAssembly.Location, runtimeExpected.Types, api);
            if (runtimeActual.BuildMvid != runtimeAssembly.ManifestModule.ModuleVersionId)
                return new(false, build, loaded, "nativeCheck=loaded_interop_file_changed");
            var differences = expected.Differences(actual).Concat(runtimeExpected.Differences(runtimeActual)).ToArray();
            return new(differences.Length == 0, build, loaded,
                $"nativeCheck={(differences.Length == 0 ? "structure_match" : "structure_mismatch")}; " +
                $"nativeBuildVersion={expected.BuildVersion}; nativeLoadedVersion={actual.BuildVersion}; " +
                $"nativeCheckedEntries={expected.Entries.Count + runtimeExpected.Entries.Count}; nativeDifferenceCount={differences.Length}; " +
                $"nativeDifferences={string.Join('|', differences.Take(6))}");
        }
        catch (Exception e)
        {
            // Unreadable/unknown metadata never permits native access. No path or personal data in diagnostics.
            return new(false, build, loaded, $"nativeCheck=inspection_failed; nativeCheckError={e.GetType().Name}");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Assembly GetInteropAssembly() => typeof(InteropGenerator.Runtime.CStringPointer).Assembly;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Assembly GetNativeAssembly() => typeof(FFXIVClientStructs.FFXIV.Client.UI.Info.InfoProxyCrossRealm).Assembly;
}
