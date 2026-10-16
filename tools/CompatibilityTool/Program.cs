using System.Text.Json;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using PartyViewKR.Compatibility;

if (args.Length == 2 && args[0] == "inspect")
{
    using var pe = new PEReader(File.OpenRead(args[1]));
    var md = pe.GetMetadataReader(); var a = md.GetAssemblyDefinition();
    Console.WriteLine(JsonSerializer.Serialize(new { name = md.GetString(a.Name), version = a.Version.ToString() }));
    return;
}
if (args.Length != 3 || args[0] is not ("capture" or "capture-runtime" or "verify"))
    throw new ArgumentException("capture <native.dll> <contract.json> OR verify <contract.json> <native.dll>");
if (args[0].StartsWith("capture", StringComparison.Ordinal))
{
    const string gui = "FFXIVClientStructs.FFXIV.Component.GUI.";
    const string ui = "FFXIVClientStructs.FFXIV.Client.UI.";
    var types = new Dictionary<string, string[]> {
        [ui + "Info.InfoProxyCrossRealm"] = ["Instance", "get_CrossRealmGroups"],
        [ui + "Info.CrossRealmGroup"] = ["get_GroupMembers"],
        [ui + "Info.CrossRealmMember"] = ["get_Name"],
        [ui + "Agent.AgentLookingForGroup"] = ["Instance", "Show", "OpenListingByContentId"],
        [ui + "Agent.AgentLookingForGroup+Detailed"] = ["get_SlotFlags", "get_Leader", "get_Comment", "get_Jobs", "get_MemberContentIds"],
        [ui + "Agent.AgentLookingForGroup+RecruitmentSub"] = ["get_SlotFlags", "get_Comment", "get_MemberContentIds"],
        [ui + "Agent.AgentLookingForGroup+DutyCategory"] = [],
        [ui + "Agent.AgentLookingForGroup+Objective"] = [],
        [ui + "Agent.AgentLookingForGroup+CompletionStatus"] = [],
        [ui + "Agent.AgentLookingForGroup+DutyFinderSetting"] = [],
        [ui + "Agent.AgentLookingForGroup+LootRule"] = [],
        [ui + "Agent.AgentLookingForGroup+JoinCondition"] = [],
        [ui + "Agent.AgentInterface"] = ["Show"],
        [ui + "AddonLookingForGroupDetail"] = ["get_IsVisible", "get_IsReady"],
        [ui + "AddonLookingForGroupCondition"] = ["get_IsVisible", "get_IsReady"],
        [gui + "AtkUnitBase"] = ["get_IsVisible", "get_IsReady", "ReceiveEvent", "GetNodeById", "FireCallback"],
        [gui + "AtkUnitBase+AtkUnitBaseVirtualTable"] = [],
        [gui + "AtkEventListener"] = ["ReceiveEvent"],
        [gui + "AtkEventListener+AtkEventListenerVirtualTable"] = [],
        [gui + "AtkResNode"] = ["IsVisible", "GetAsAtkComponentNode"],
        [gui + "AtkComponentNode"] = ["IsVisible"],
        [gui + "AtkComponentBase"] = [],
        [gui + "AtkComponentButton"] = ["get_IsEnabled"],
        [gui + "AtkComponentNumericInput"] = ["InnerSetValue"],
        [gui + "AtkUldComponentDataNumericInput"] = [],
        [gui + "NodeFlags"] = [],
        [gui + "AtkComponentTextInput"] = ["SetText"],
        [gui + "AtkComponentDropDownList"] = ["GetSelectedItemIndex"],
        [gui + "AtkComponentCheckBox"] = ["get_IsEnabled", "get_IsChecked", "SetChecked"],
        [gui + "AtkComponentList"] = ["DispatchItemEvent", "SelectItem", "GetItemDisabledState"],
        [gui + "AtkComponentList+AtkComponentListVirtualTable"] = [],
        [gui + "AtkUldComponentDataTextInput"] = [],
        [gui + "AtkValue"] = [], [gui + "AtkValueType"] = [],
        [gui + "AtkTextNode"] = [],
        [gui + "AtkEvent"] = [], [gui + "AtkEventData"] = [],
        [gui + "AtkEventData+AtkListItemData"] = [],
        [gui + "AtkEventListener+Delegates+ReceiveEvent"] = ["Invoke"],
        [gui + "AtkUnitBase+Delegates+FireCallback"] = ["Invoke"],
        [gui + "AtkEventManager"] = [], [gui + "AtkEventState"] = [],
        [gui + "AtkEventType"] = [], [gui + "NodeType"] = [],
        ["FFXIVClientStructs.FFXIV.Client.System.String.Utf8String"] = []
    };
    if (args[0] == "capture-runtime") types = new() { ["InteropGenerator.Runtime.CStringPointer"] = [] };
    var contract = NativeContract.Read(args[1], types, 15);
    foreach (var (type, methods) in types)
    {
        if (!contract.Entries.ContainsKey("type:" + type)) throw new InvalidDataException("Missing type: " + type);
        foreach (var method in methods)
            if (!contract.Entries.Keys.Any(k => k.StartsWith($"method:{type}.{method}(", StringComparison.Ordinal)))
                throw new InvalidDataException("Missing method: " + type + "." + method);
    }
    File.WriteAllText(args[2], JsonSerializer.Serialize(contract, NativeContract.JsonOptions) + "\n");
    Console.WriteLine($"Captured {contract.Entries.Count} entries; review before committing this baseline.");
}
else
{
    using var stream = File.OpenRead(args[1]);
    var expected = NativeContract.Parse(stream);
    var actual = NativeContract.Read(args[2], expected.Types, 15);
    var differences = expected.Differences(actual);
    Console.WriteLine($"native={actual.BuildVersion}; mvid={actual.BuildMvid}; checked={expected.Entries.Count}; differences={differences.Length}");
    foreach (var difference in differences) Console.WriteLine(difference);
    if (differences.Length > 0) Environment.ExitCode = 1;
}
