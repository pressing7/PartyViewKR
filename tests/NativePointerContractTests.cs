using System.Reflection;
using System.Reflection.Emit;
using PartyViewKR.Compatibility;

static class NativePointerContractTests
{
    private const string Ui = "FFXIVClientStructs.FFXIV.Client.UI.UIModule";
    private const string Vtable = Ui + "+UIModuleVirtualTable";

    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        test("unused UIModule storage can move or disappear without disabling native access", () =>
        {
            var before = Capture();
            check(before.Differences(Capture(moveUnusedStorage: true)).Length == 0,
                "private module storage or overall size still gates pointer-only use");
            check(before.Differences(Capture(omitUnusedStorage: true)).Length == 0,
                "unused field removal still gates compatibility");
            check(!before.Entries.ContainsKey($"field:{Ui}.AddonSheetName"), "unused field was captured");
        });
        test("UIModule dispatch pointer and acquaintance virtual slot changes remain blocked", () =>
        {
            var before = Capture();
            check(before.Differences(Capture(moveVtable: true)).Contains($"field:{Ui}.VirtualTable"),
                "moved vtable pointer accepted");
            check(before.Differences(Capture(moveSlot: true)).Contains($"field:{Vtable}.GetAcquaintanceModule"),
                "moved virtual slot accepted");
            check(before.Differences(Capture(omitSlot: true)).Contains($"field:{Vtable}.GetAcquaintanceModule"),
                "missing virtual slot accepted");
        });
        test("missing or changed chat entry methods remain blocked", () =>
        {
            var before = Capture();
            check(before.Differences(Capture(changeSendSignature: true))
                .Any(k => k.StartsWith($"method:{Ui}.ProcessChatBoxEntry(")), "changed send signature accepted");
            check(before.Differences(Capture(omitSend: true))
                .Any(k => k.StartsWith($"method:{Ui}.ProcessChatBoxEntry(")), "missing send method accepted");
        });
        test("pointer-only storage policy does not apply to similarly named unrelated structs", () =>
        {
            const string other = "Other.UIModule";
            var before = Capture(uiName: other);
            var differences = before.Differences(Capture(uiName: other, moveUnusedStorage: true));
            check(differences.Contains("type:" + other) && differences.Contains($"field:{other}.AddonSheetName"),
                "policy incorrectly ignored a directly accessed type's storage");
        });
        test("shipped contract retains chat calls and all directly read reply and string fields", () =>
        {
            using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "native-contract.json"));
            var baseline = NativeContract.Parse(stream);
            foreach (var method in new[] { "Instance", "ProcessChatBoxEntry", "GetAcquaintanceModule" })
                check(baseline.Entries.Keys.Any(k => k.StartsWith($"method:{Ui}.{method}(")), "missing method: " + method);
            foreach (var field in new[]
            {
                Ui + ".VirtualTable", Vtable + ".GetAcquaintanceModule",
                "FFXIVClientStructs.FFXIV.Client.UI.Misc.AcquaintanceModule.NumTellHistoryEntries",
                "FFXIVClientStructs.FFXIV.Client.UI.Misc.AcquaintanceModule+Acquaintance.Name",
                "FFXIVClientStructs.FFXIV.Client.UI.Misc.AcquaintanceModule+Acquaintance.WorldId",
                "FFXIVClientStructs.FFXIV.Client.UI.Misc.AcquaintanceModule+Acquaintance.ContentId",
                "FFXIVClientStructs.FFXIV.Client.System.String.Utf8String.StringPtr"
            }) check(baseline.Entries.ContainsKey("field:" + field), "missing field: " + field);
            var slot = baseline.Entries[$"field:{Vtable}.GetAcquaintanceModule"];
            check(slot.StartsWith("fnptr(") && slot.Contains(";offset=120;"), "native virtual signature/slot absent");
            check(!baseline.Entries.ContainsKey($"field:{Ui}.AddonSheetName"), "shipped baseline still contains unused storage");
        });
    }

    // Generate two real PE metadata layouts without loading or invoking native code.
    // The only differences can be unused storage, dispatch layout, or a used method.
    private static NativeContract Capture(bool moveUnusedStorage = false, bool omitUnusedStorage = false,
        bool moveVtable = false, bool moveSlot = false, bool omitSlot = false,
        bool changeSendSignature = false, bool omitSend = false, string uiName = Ui)
    {
        var assembly = new PersistedAssemblyBuilder(new AssemblyName("FFXIVClientStructs"), typeof(object).Assembly);
        var module = assembly.DefineDynamicModule("Fixture");
        const TypeAttributes layout = TypeAttributes.Sealed | TypeAttributes.ExplicitLayout;
        var ui = module.DefineType(uiName, TypeAttributes.Public | layout, typeof(ValueType),
            PackingSize.Unspecified, moveUnusedStorage ? 256 : 128);
        var table = ui.DefineNestedType("UIModuleVirtualTable", TypeAttributes.NestedPublic | layout,
            typeof(ValueType), PackingSize.Unspecified, moveUnusedStorage ? 256 : 128);
        ui.DefineField("VirtualTable", typeof(nint), FieldAttributes.Public).SetOffset(moveVtable ? 8 : 0);
        if (!omitUnusedStorage)
            ui.DefineField("AddonSheetName", typeof(nint), FieldAttributes.Public).SetOffset(moveUnusedStorage ? 32 : 16);
        if (!omitSlot)
            table.DefineField("GetAcquaintanceModule", typeof(nint), FieldAttributes.Public).SetOffset(moveSlot ? 112 : 120);
        table.CreateType();
        DefineStub(ui, "Instance", MethodAttributes.Public | MethodAttributes.Static, typeof(nint), []);
        DefineStub(ui, "GetAcquaintanceModule", MethodAttributes.Public, typeof(nint), []);
        if (!omitSend)
            DefineStub(ui, "ProcessChatBoxEntry", MethodAttributes.Public, typeof(void),
                [typeof(nint), typeof(nint), changeSendSignature ? typeof(int) : typeof(bool)]);
        ui.CreateType();
        var path = Path.Combine(Path.GetTempPath(), "pvkr-native-contract-" + Guid.NewGuid().ToString("N") + ".dll");
        try
        {
            assembly.Save(path);
            return NativeContract.Read(path, new()
            {
                [uiName] = ["Instance", "ProcessChatBoxEntry", "GetAcquaintanceModule"],
                [uiName + "+UIModuleVirtualTable"] = []
            }, 15);
        }
        finally { File.Delete(path); }
    }

    private static void DefineStub(TypeBuilder type, string name, MethodAttributes attributes, Type result, Type[] arguments)
    {
        var il = type.DefineMethod(name, attributes, result, arguments).GetILGenerator();
        il.Emit(OpCodes.Newobj, typeof(NotSupportedException).GetConstructor(Type.EmptyTypes)!);
        il.Emit(OpCodes.Throw);
    }
}
