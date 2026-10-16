using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ReceiveEvent = FFXIVClientStructs.FFXIV.Component.GUI.AtkEventListener.Delegates.ReceiveEvent;
using FireCallback = FFXIVClientStructs.FFXIV.Component.GUI.AtkUnitBase.Delegates.FireCallback;

namespace PartyViewKR;

// Observe manual interaction only. No event dispatch, selection, memory write,
// callback construction, editor opening or recruitment submission belongs here.
internal sealed unsafe class ListingObjectiveTrace(Plugin plugin) : IDisposable
{
    private readonly ObjectiveTraceBuffer buffer = new();
    private readonly Dictionary<nint, Hook<ReceiveEvent>> eventHooks = [];
    private readonly Dictionary<nint, string> listeners = [];
    private readonly Dictionary<nint, string> nodes = [];
    private Hook<FireCallback>? callbackHook;
    private nint addonAddress, dropdownAddress, checkboxAddress, listAddress;
    private ulong selfId;
    private bool startRequested, disposed;
    private long callId;
    private string lastState = "";
    private string startError = "none";
    private string status = "게임의 모집 수정창을 직접 연 뒤 기록을 시작해 주세요.";
    internal bool Busy => startRequested || buffer.Active || eventHooks.Count != 0 || callbackHook != null;
    internal bool Active => buffer.Active;
    internal string Status => buffer.Active ? $"기록 중 · {buffer.Count}줄 · 최대 2분" : status;
    internal string Diagnostic => $"objectiveTraceStartError={startError}\n" + buffer.Snapshot();

    internal void RequestStart() { if (!Busy && !disposed) startRequested = true; }
    internal void RequestStop() { startRequested = false; buffer.Stop("user_stopped"); }
    internal void EditorClosed() { startRequested = false; buffer.Stop("editor_closed"); }

    // Before the slower polling interval: closure/timeout removes hooks promptly.
    // Never dispose a hook from inside its detour.
    internal void Update()
    {
        if (disposed) return;
        if (startRequested)
        {
            startRequested = false;
            startError = "none";
            try { Start(); }
            catch (Exception ex)
            {
                startError = ex is InvalidDataException ? ex.Message : ex.GetType().Name;
                Record($"start_error={startError}"); buffer.Stop("start_failed");
                status = "목적 기록을 시작하지 못했습니다. 진단 정보를 복사해 주세요.";
                Cleanup();
            }
        }
        if (buffer.Active)
        {
            buffer.Tick(Environment.TickCount64);
            if (buffer.Active)
            {
                try
                {
                    var state = ReadState();
                    if (state == null) buffer.Stop("editor_binding_changed");
                    else if (state != lastState) { lastState = state; Record("state " + state); }
                }
                catch (Exception ex) { Record($"state_error={ex.GetType().Name}"); buffer.Stop("read_failed"); }
            }
        }
        if (!buffer.Active && (eventHooks.Count != 0 || callbackHook != null))
        {
            Cleanup();
            status = $"기록 종료 · {buffer.Count}줄 · {buffer.Reason} · 목적 기록 복사로 전달해 주세요.";
        }
    }

    private void Start()
    {
        if (plugin.Refresh.Busy || plugin.ListingBusy || plugin.JobDraft != null || plugin.DescriptionDraft != null || plugin.ObjectiveDraft != null || plugin.BeginnerDraft != null)
        { status = "진행 중인 조회·갱신과 PVKR 편집창을 끝낸 뒤 기록해 주세요."; return; }
        if (!Plugin.ClientState.IsLoggedIn || !Plugin.PlayerState.IsLoaded || !NativeCompatibility.IsCompatible)
        { status = "로그인 및 구조 DLL 호환 상태를 확인한 뒤 기록해 주세요."; return; }
        var addon = Plugin.GameGui.GetAddonByName<AddonLookingForGroupCondition>("LookingForGroupCondition", 1);
        if (!NativeReadGuard.Readable(addon, (nuint)sizeof(AddonLookingForGroupCondition)) || !addon->IsVisible || !addon->IsReady)
        { status = "게임의 파티 모집 수정창을 직접 연 뒤 기록 시작을 눌러 주세요."; return; }
        var dropdown = addon->ObjectiveDropDown;
        if (!NativeReadGuard.Readable(dropdown, (nuint)sizeof(AtkComponentDropDownList)) ||
            !NativeReadGuard.Readable(dropdown->Checkbox, (nuint)sizeof(AtkComponentCheckBox)) ||
            !NativeReadGuard.Readable(dropdown->List, (nuint)sizeof(AtkComponentList)))
        { status = "목적 드롭다운을 읽을 수 없습니다. 진단 정보를 확인해 주세요."; return; }
        addonAddress = (nint)addon; dropdownAddress = (nint)dropdown;
        checkboxAddress = (nint)dropdown->Checkbox; listAddress = (nint)dropdown->List;
        selfId = Plugin.PlayerState.ContentId;
        lastState = ReadState() ?? throw new InvalidDataException("objective_state_unreadable");
        callId = 0; buffer.Start(Environment.TickCount64);
        Record("start manual_only=true; generatedEvents=0; generatedCallbacks=0; " + lastState);
        listeners.Add(addonAddress, "editor"); listeners.Add(dropdownAddress, "dropdown");
        listeners.Add(checkboxAddress, "checkbox"); listeners.Add(listAddress, "list");
        BindNode(dropdown->OwnerNode, "dropdown");
        BindNode(dropdown->Checkbox->OwnerNode, "checkbox");
        BindNode(dropdown->List->OwnerNode, "list");
        foreach (var node in nodes) ReadRegisteredEvents((AtkResNode*)node.Key, node.Value);
        // Shared implementations are hooked once, filtered by component instance.
        foreach (var listener in listeners.Keys) AddEventHook((AtkEventListener*)listener);
        callbackHook = Plugin.GameInterop.HookFromAddress<FireCallback>(AtkUnitBase.MemberFunctionPointers.FireCallback, CallbackDetour);
        foreach (var hook in eventHooks.Values) hook.Enable();
        callbackHook.Enable();
        Record($"hooks receiveEvent={eventHooks.Count}; fireCallback=1");
        status = "기록 중";
    }

    private void BindNode(AtkComponentNode* node, string name)
    {
        if (!NativeReadGuard.Readable(node, (nuint)sizeof(AtkComponentNode))) throw new InvalidDataException("objective_node_unreadable");
        nodes.TryAdd((nint)node, name);
        Record($"node component={name}; nodeId={node->NodeId}");
    }

    private void AddEventHook(AtkEventListener* listener)
    {
        if (!NativeReadGuard.Readable(listener, (nuint)sizeof(AtkEventListener)) ||
            !NativeReadGuard.Readable(listener->VirtualTable, (nuint)sizeof(AtkEventListener.AtkEventListenerVirtualTable)))
            throw new InvalidDataException("listener_vtable_unreadable");
        var address = (nint)listener->VirtualTable->ReceiveEvent;
        if (address == 0) throw new InvalidDataException("receive_event_unresolved");
        if (eventHooks.ContainsKey(address)) return;
        Hook<ReceiveEvent>? hook = null;
        hook = Plugin.GameInterop.HookFromAddress<ReceiveEvent>(address, (self, type, param, evt, data) =>
        {
            long id = 0;
            try
            {
                if (buffer.Active && type != AtkEventType.MouseMove && listeners.TryGetValue((nint)self, out var component))
                {
                    id = Interlocked.Increment(ref callId);
                    Record($"pre#{id} receive component={component}; type={type}({(byte)type}); param={param}; " +
                        EventInfo(evt) + "; " + DataInfo(type, data) + "; " + (ReadState() ?? "state=unavailable"));
                }
            }
            catch (Exception ex) { RecordFault(ex); }
            // Exactly once, unmodified. Observation errors never swallow/retry it.
            hook!.Original(self, type, param, evt, data);
            if (id != 0)
            {
                try { Record($"post#{id} " + (ReadState() ?? "state=unavailable")); }
                catch (Exception ex) { RecordFault(ex); }
            }
        });
        eventHooks.Add(address, hook);
    }

    private bool CallbackDetour(AtkUnitBase* self, uint count, AtkValue* values, bool close)
    {
        long id = 0;
        try
        {
            if (buffer.Active && (nint)self == addonAddress)
            {
                id = Interlocked.Increment(ref callId);
                Record($"pre#{id} callback count={count}; close={close}; args={CallbackValues(count, values)}; " +
                    (ReadState() ?? "state=unavailable"));
            }
        }
        catch (Exception ex) { RecordFault(ex); }
        var result = callbackHook!.Original(self, count, values, close);
        if (id != 0)
        {
            try { Record($"post#{id} callbackReturned={result}; " + (ReadState() ?? "state=unavailable")); }
            catch (Exception ex) { RecordFault(ex); }
        }
        return result;
    }

    private string? ReadState()
    {
        if (!Plugin.ClientState.IsLoggedIn || !Plugin.PlayerState.IsLoaded || Plugin.PlayerState.ContentId != selfId ||
            !NativeCompatibility.IsCompatible) return null;
        var addon = Plugin.GameGui.GetAddonByName<AddonLookingForGroupCondition>("LookingForGroupCondition", 1);
        if ((nint)addon != addonAddress || !NativeReadGuard.Readable(addon, (nuint)sizeof(AddonLookingForGroupCondition)) ||
            !addon->IsVisible || !addon->IsReady) return null;
        var dropdown = addon->ObjectiveDropDown;
        if ((nint)dropdown != dropdownAddress || !NativeReadGuard.Readable(dropdown, (nuint)sizeof(AtkComponentDropDownList)) ||
            (nint)dropdown->Checkbox != checkboxAddress || (nint)dropdown->List != listAddress ||
            !NativeReadGuard.Readable(dropdown->Checkbox, (nuint)sizeof(AtkComponentCheckBox)) ||
            !NativeReadGuard.Readable(dropdown->List, (nuint)sizeof(AtkComponentList))) return null;
        var agent = AgentLookingForGroup.Instance();
        if (!NativeReadGuard.Readable(agent, (nuint)sizeof(AgentLookingForGroup))) return null;
        return $"uiIndex={dropdown->GetSelectedItemIndex()}; listIndex={dropdown->List->SelectedItemIndex}; " +
            $"storedValue={(byte)agent->StoredRecruitmentInfo.Objective}; open={dropdown->IsOpen}; pending={dropdown->OpenStateChangePending}; " +
            $"checked={dropdown->Checkbox->IsChecked}; itemCount={dropdown->List->ListLength}";
    }

    private void ReadRegisteredEvents(AtkResNode* node, string name)
    {
        var seen = new HashSet<nuint>();
        for (var evt = node->AtkEventManager.Event; evt != null; evt = evt->NextEvent)
        {
            if (seen.Count >= 32 || !seen.Add((nuint)evt) || !NativeReadGuard.Readable(evt, (nuint)sizeof(AtkEvent)))
            { Record($"registered component={name}; event_chain_unreadable"); break; }
            Record($"registered component={name}; type={evt->State.EventType}({(byte)evt->State.EventType}); " +
                $"param={evt->Param}; {EventInfo(evt)}");
        }
    }

    private string EventInfo(AtkEvent* evt)
    {
        if (!NativeReadGuard.Readable(evt, (nuint)sizeof(AtkEvent))) return "event=unreadable";
        var target = nodes.GetValueOrDefault((nint)evt->Target, evt->Target == null ? "null" : "other");
        var listener = listeners.GetValueOrDefault((nint)evt->Listener, evt->Listener == null ? "null" : "other");
        var nodeId = NativeReadGuard.Readable(evt->Node, (nuint)sizeof(AtkResNode)) ? evt->Node->NodeId.ToString() : "unknown";
        return $"eventParam={evt->Param}; eventType={evt->State.EventType}; target={target}; listener={listener}; nodeId={nodeId}; returnFlags={evt->State.ReturnFlags}";
    }

    private static string DataInfo(AtkEventType type, AtkEventData* data)
    {
        if (!NativeReadGuard.Readable(data, (nuint)sizeof(AtkEventData))) return "eventData=unreadable";
        return type is >= AtkEventType.ListItemRollOver and <= AtkEventType.ListItemSelect
            ? $"selectedIndex={data->ListItemData.SelectedIndex}; mouseButton={data->ListItemData.MouseButtonId}"
            : "eventData=present";
    }

    private static string CallbackValues(uint count, AtkValue* values)
    {
        if (count == 0) return "[]";
        if (count > 16 || !NativeReadGuard.Readable(values, (nuint)(count * sizeof(AtkValue)))) return "unreadable_or_oversized";
        var parts = new string[count];
        for (var i = 0; i < count; i++)
        {
            ref var value = ref values[i];
            // Never dereference strings, vectors, pointers or other payloads.
            parts[i] = value.Type switch {
                AtkValueType.Int => $"Int:{value.Int}", AtkValueType.UInt => $"UInt:{value.UInt}",
                AtkValueType.Bool => $"Bool:{value.Bool}", _ => $"{value.Type}:omitted"
            };
        }
        return "[" + string.Join(',', parts) + "]";
    }

    private void Record(string line) => buffer.Add(Environment.TickCount64, line);
    private void RecordFault(Exception ex)
    {
        try { Record($"observation_error={ex.GetType().Name}"); buffer.Stop("observation_failed"); }
        catch { /* no diagnostic failure may escape into the game */ }
    }

    private void Cleanup()
    {
        callbackHook?.Dispose(); callbackHook = null;
        foreach (var hook in eventHooks.Values) hook.Dispose();
        eventHooks.Clear(); listeners.Clear(); nodes.Clear();
    }

    public void Dispose()
    {
        disposed = true; startRequested = false; buffer.Stop("plugin_disposed"); Cleanup();
    }
}
