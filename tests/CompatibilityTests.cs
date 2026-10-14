using System.Text;
using System.Text.Json;
using PartyViewKR.Compatibility;

static class CompatibilityTests
{
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "native-contract.json"));
        var baseline = NativeContract.Parse(stream);
        NativeContract Clone() => baseline with { Entries = new(baseline.Entries, StringComparer.Ordinal) };
        test("different DLL identity with unchanged required structure passes", () =>
            check(baseline.Differences(baseline with { BuildMvid = Guid.NewGuid(), BuildVersion = "99.0.0.0" }).Length == 0, "MVID still gates compatibility"));
        test("required native layout, offsets, constants and virtual slots fail closed", () => {
            foreach (var key in baseline.Entries.Keys)
            {
                var changed = Clone(); changed.Entries[key] += "changed";
                check(baseline.Differences(changed).Contains(key), "undetected change: " + key);
                var missing = Clone(); missing.Entries.Remove(key);
                check(baseline.Differences(missing).Contains(key), "undetected removal: " + key);
            }
        });
        test("unrelated metadata additions do not disable compatible builds", () => {
            var added = Clone(); added.Entries.Add("method:Unrelated.Addition()", "Void");
            check(baseline.Differences(added).Length == 0, "extra entry rejected");
        });
        test("different API, assembly, schema or empty baseline is rejected", () => {
            check(baseline.Differences(baseline with { ApiLevel = 16 }).Contains("dalamud_api"), "API");
            check(baseline.Differences(baseline with { AssemblyName = "Wrong" }).Contains("assembly"), "assembly");
            check(baseline.Differences(baseline with { Schema = 2 }).Contains("schema"), "schema");
            check((baseline with { Entries = new() }).Differences(baseline).Contains("empty_contract"), "empty");
        });
        test("contract serialization round trip preserves checks", () => {
            using var copy = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(baseline)));
            check(baseline.Differences(NativeContract.Parse(copy)).Length == 0, "round trip");
        });
        test("metadata reader detects a missing required type without loading code", () => {
            var actual = NativeContract.Read(typeof(CompatibilityTests).Assembly.Location, baseline.Types, 15);
            check(baseline.Differences(actual).Contains("assembly"), "wrong DLL accepted");
            check(baseline.Differences(actual).Any(k => k.StartsWith("type:")), "missing type not detected");
        });
        test("malformed baseline does not silently become compatible", () => {
            using var invalid = new MemoryStream(Encoding.UTF8.GetBytes("null"));
            try { NativeContract.Parse(invalid); throw new Exception("null baseline accepted"); }
            catch (InvalidDataException) { }
        });
    }
}
