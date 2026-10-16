using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

namespace PartyViewKR;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager Commands { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IPartyList PartyList { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInterop { get; private set; } = null!;

    private readonly WindowSystem windows = new("PartyViewKR");
    private readonly PartyReader reader = new();
    private readonly ListingReader listingReader = new();
    private readonly ListingRefreshHost refreshHost;
    private readonly PartyWindow window;
    private long lastRead;
    private bool readRequested = true;
    private bool disposed;
    private string lastError = "";
    private string lastListingError = "";
    private int? requestedSlot;
    private bool requestedDescription;
    private bool requestedObjective;
    private bool requestedBeginner;
    internal ListingJobDraft? JobDraft { get; private set; }
    internal string JobDraftStatus { get; private set; } = "";
    internal ListingDescriptionDraft? DescriptionDraft { get; private set; }
    internal string DescriptionDraftStatus { get; private set; } = "";
    internal ListingObjectiveDraft? ObjectiveDraft { get; private set; }
    internal string ObjectiveDraftStatus { get; private set; } = "";
    internal ListingBeginnerDraft? BeginnerDraft { get; private set; }
    internal string BeginnerDraftStatus { get; private set; } = "";
    internal PartySnapshot Snapshot { get; private set; } = PartySnapshot.Empty;
    internal ListingSnapshot Listing { get; private set; } = ListingSnapshot.Empty;
    internal ListingRefresh Refresh { get; }
    internal ListingObjectiveTrace ObjectiveTrace { get; }
    internal bool ListingBusy => listingReader.Busy;
    internal string Diagnostic => Snapshot.Diagnostic + "\n" + Listing.Diagnostic + "\n" + Refresh.Diagnostic + "\n" + refreshHost.Diagnostic + "\n" + ObjectiveTrace.Diagnostic;

    public Plugin()
    {
        ObjectiveTrace = new ListingObjectiveTrace(this);
        refreshHost = new ListingRefreshHost(this, listingReader);
        Refresh = new ListingRefresh(refreshHost);
        window = new PartyWindow(this);
        windows.AddWindow(window);
        Commands.AddHandler("/pvkr", new CommandInfo(OnCommand) { HelpMessage = "파티원·모집글 조회 및 수동 갱신 창 열기" });
        Commands.AddHandler("/partyviewkr", new CommandInfo(OnCommand) { HelpMessage = "파티원·모집글 조회 및 수동 갱신 창 열기" });
        PluginInterface.UiBuilder.Draw += windows.Draw;
        PluginInterface.UiBuilder.OpenMainUi += Open;
        PluginInterface.UiBuilder.OpenConfigUi += Open;
        Framework.Update += OnUpdate;
        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "LookingForGroupDetail", OnDetailUpdated);
        AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "LookingForGroupDetail", OnDetailUpdated);
        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "LookingForGroupCondition", OnEditorUpdated);
        AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "LookingForGroupCondition", OnEditorUpdated);
        AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "LookingForGroupCondition", OnEditorClosed);
        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "LookingForGroupSelectRole", OnRoleOpened);
        AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "LookingForGroupSelectRole", OnRoleClosed);
    }

    private void OnCommand(string command, string arguments) => Open();
    private void Open() { window.IsOpen = true; window.BringToFront(); RequestRead(); }
    internal void RequestRead() => readRequested = true;
    internal void QueryOwnListing() { if (!Refresh.Busy && !ObjectiveTrace.Busy) listingReader.RequestOwn(); RequestRead(); }
    internal void RefreshOwnListing() { if (!ObjectiveTrace.Busy) Refresh.Request(Environment.TickCount64); RequestRead(); }
    internal void CancelRefresh() { Refresh.Cancel(); RequestRead(); }
    internal void RequestSlotEdit(int position) { if (!ObjectiveTrace.Busy && !Refresh.Busy && DescriptionDraft == null && ObjectiveDraft == null && BeginnerDraft == null) { requestedSlot = position; RequestRead(); } }
    internal void RequestDescriptionEdit() { if (!ObjectiveTrace.Busy && !Refresh.Busy && JobDraft == null && ObjectiveDraft == null && BeginnerDraft == null) { requestedDescription = true; RequestRead(); } }
    internal void RequestObjectiveEdit() { if (ListingObjectives.AutomaticEditingAvailable && !ObjectiveTrace.Busy && !Refresh.Busy && JobDraft == null && DescriptionDraft == null && BeginnerDraft == null) { requestedObjective = true; RequestRead(); } }
    internal void CloseDescriptionDraft() => DescriptionDraft = null;
    internal void CloseJobDraft() => JobDraft = null;
    internal void CloseObjectiveDraft() => ObjectiveDraft = null;
    internal void RequestBeginnerEdit()
    {
        if (!ObjectiveTrace.Busy && !Refresh.Busy && JobDraft == null && DescriptionDraft == null && ObjectiveDraft == null)
        { requestedBeginner = true; RequestRead(); }
    }
    internal void CloseBeginnerDraft() => BeginnerDraft = null;
    internal bool ApplyBeginnerDraft()
    {
        if (ObjectiveTrace.Busy || BeginnerDraft == null || JobDraft != null || DescriptionDraft != null || ObjectiveDraft != null ||
            !BeginnerDraft.Request.Valid || Refresh.Busy || ListingBusy || Refresh.CoolingDown(Environment.TickCount64)) return false;
        Refresh.Request(Environment.TickCount64, beginnerEdit: BeginnerDraft.Request);
        BeginnerDraft = null; BeginnerDraftStatus = "";
        RequestRead();
        return true;
    }
    internal bool ApplyJobDraft()
    {
        if (ObjectiveTrace.Busy) return false;
        if (JobDraft == null || DescriptionDraft != null || ObjectiveDraft != null || BeginnerDraft != null || !JobDraft.Request.Valid || Refresh.Busy || ListingBusy || Refresh.CoolingDown(Environment.TickCount64)) return false;
        Refresh.Request(Environment.TickCount64, JobDraft.Request);
        JobDraft = null; JobDraftStatus = "";
        RequestRead();
        return true;
    }
    internal bool ApplyDescriptionDraft()
    {
        if (ObjectiveTrace.Busy) return false;
        if (DescriptionDraft == null || JobDraft != null || ObjectiveDraft != null || BeginnerDraft != null || !DescriptionDraft.Request.Valid || Refresh.Busy || ListingBusy || Refresh.CoolingDown(Environment.TickCount64)) return false;
        Refresh.Request(Environment.TickCount64, descriptionEdit: DescriptionDraft.Request);
        DescriptionDraft = null; DescriptionDraftStatus = "";
        RequestRead();
        return true;
    }
    internal bool ApplyObjectiveDraft()
    {
        if (!ListingObjectives.AutomaticEditingAvailable || ObjectiveTrace.Busy) return false;
        if (ObjectiveDraft == null || JobDraft != null || DescriptionDraft != null || BeginnerDraft != null || !ObjectiveDraft.Request.Valid ||
            Refresh.Busy || ListingBusy || Refresh.CoolingDown(Environment.TickCount64)) return false;
        Refresh.Request(Environment.TickCount64, objectiveEdit: ObjectiveDraft.Request);
        ObjectiveDraft = null; ObjectiveDraftStatus = "";
        RequestRead();
        return true;
    }
    private void OnDetailUpdated(AddonEvent addonEvent, AddonArgs args) { listingReader.DetailUpdated(); RequestRead(); }
    private void OnEditorUpdated(AddonEvent addonEvent, AddonArgs args) { refreshHost.EditorUpdated(); RequestRead(); }
    private void OnEditorClosed(AddonEvent addonEvent, AddonArgs args) { ObjectiveTrace.EditorClosed(); refreshHost.EditorClosed(); RequestRead(); }
    private void OnRoleOpened(AddonEvent addonEvent, AddonArgs args) { refreshHost.RoleOpened(); RequestRead(); }
    private void OnRoleClosed(AddonEvent addonEvent, AddonArgs args) { refreshHost.RoleClosed(); RequestRead(); }

    private void PrepareJobDraft()
    {
        if (!ClientState.IsLoggedIn || !PlayerState.IsLoaded || JobDraft != null && JobDraft.Original.SelfId != PlayerState.ContentId)
            JobDraft = null;
        if (requestedSlot is not { } position) return;
        requestedSlot = null;
        if (Refresh.Busy || ListingBusy || DescriptionDraft != null || ObjectiveDraft != null || BeginnerDraft != null) return;
        if (refreshHost.BlockReason is { } reason) { JobDraftStatus = reason; return; }
        if (!refreshHost.TryGetOwn(out var target, out var error)) { JobDraftStatus = error; return; }
        if (!ListingJobEdit.Supports(target))
        { JobDraftStatus = "직업 수정은 일반 4·8인 및 연합 24·48인 모집에서 지원합니다. PvP이거나 파티 구성을 확인하지 못한 경우에는 수정할 수 없습니다."; return; }
        if (position < 1 || position > target.TotalSlots || target.Slots.Slots[position - 1].HasMember || !target.Slots.JobRules.Complete)
        { JobDraftStatus = "수정할 빈자리의 직업 조건을 확인하지 못했습니다. 다시 조회해 주세요."; return; }
        JobDraft = new(target, position, target.Slots.JobRules);
        JobDraftStatus = "";
    }

    private void PrepareDescriptionDraft()
    {
        if (!ClientState.IsLoggedIn || !PlayerState.IsLoaded || DescriptionDraft != null && DescriptionDraft.Original.SelfId != PlayerState.ContentId)
            DescriptionDraft = null;
        if (!requestedDescription) return;
        requestedDescription = false;
        if (Refresh.Busy || ListingBusy || JobDraft != null || ObjectiveDraft != null || BeginnerDraft != null) return;
        if (refreshHost.BlockReason is { } reason) { DescriptionDraftStatus = reason; return; }
        if (!refreshHost.TryGetOwn(out var target, out var error)) { DescriptionDraftStatus = error; return; }
        if (!ListingJobEdit.Supports(target))
        { DescriptionDraftStatus = "일반 4·8인 및 연합 24·48인 모집 정보를 확인한 뒤 소개말을 수정할 수 있습니다."; return; }
        DescriptionDraft = new(target);
        DescriptionDraftStatus = "";
    }

    private void PrepareObjectiveDraft()
    {
        if (!ClientState.IsLoggedIn || !PlayerState.IsLoaded || ObjectiveDraft != null && ObjectiveDraft.Original.SelfId != PlayerState.ContentId)
            ObjectiveDraft = null;
        if (!requestedObjective) return;
        requestedObjective = false;
        if (Refresh.Busy || ListingBusy || JobDraft != null || DescriptionDraft != null || BeginnerDraft != null) return;
        if (refreshHost.BlockReason is { } reason) { ObjectiveDraftStatus = reason; return; }
        if (!refreshHost.TryGetOwn(out var target, out var error)) { ObjectiveDraftStatus = error; return; }
        if (!ListingObjectiveEdit.Supports(target))
        { ObjectiveDraftStatus = "일반 4·8인 및 연합 24·48인 모집의 목적과 파티 구성을 확인한 뒤 수정할 수 있습니다."; return; }
        ObjectiveDraft = new(target);
        ObjectiveDraftStatus = "";
    }

    private void PrepareBeginnerDraft()
    {
        if (!ClientState.IsLoggedIn || !PlayerState.IsLoaded || BeginnerDraft != null && BeginnerDraft.Original.SelfId != PlayerState.ContentId)
            BeginnerDraft = null;
        if (!requestedBeginner) return;
        requestedBeginner = false;
        if (Refresh.Busy || ListingBusy || JobDraft != null || DescriptionDraft != null || ObjectiveDraft != null) return;
        if (refreshHost.BlockReason is { } reason) { BeginnerDraftStatus = reason; return; }
        if (!refreshHost.TryGetOwn(out var target, out var error)) { BeginnerDraftStatus = error; return; }
        if (!ListingBeginnerEdit.Supports(target))
        { BeginnerDraftStatus = "일반 4·8인 및 연합 24·48인 모집의 초보자 환영 설정과 파티 구성을 확인한 뒤 수정할 수 있습니다."; return; }
        BeginnerDraft = new(target);
        BeginnerDraftStatus = "";
    }

    private void OnUpdate(IFramework framework)
    {
        if (disposed) return;
        ObjectiveTrace.Update();
        var now = Environment.TickCount64;
        if (!readRequested && now - lastRead < 500) return;
        readRequested = false;
        lastRead = now;
        try
        {
            Snapshot = reader.Read(Snapshot);
            lastError = "";
        }
        catch (Exception ex)
        {
            var message = ex.GetBaseException().Message;
            Snapshot = Snapshot with { Status = "조회 오류 — 마지막 조회 결과", IsStale = true, Error = message };
            if (message != lastError) Log.Error(ex, "PartyViewKR: party read failed");
            lastError = message;
        }
        try
        {
            Listing = listingReader.Read(Snapshot, Listing);
            lastListingError = "";
        }
        catch (Exception ex)
        {
            var message = ex.GetBaseException().Message;
            listingReader.RecordFailure(message);
            if (Refresh.Busy) Refresh.Fail(message);
            Listing = Listing with { Status = "모집글 조회 오류 — 마지막 결과", IsLive = false, Note = message,
                Diagnostic = "listingRead=failed\nlistingError=" + message };
            if (message != lastListingError) Log.Error(ex, "PartyViewKR: listing read failed");
            lastListingError = message;
        }
        try { PrepareJobDraft(); PrepareDescriptionDraft(); PrepareObjectiveDraft(); PrepareBeginnerDraft(); Refresh.Update(now); }
        catch (Exception ex)
        {
            Refresh.Fail(ex.GetBaseException().Message);
            Log.Error(ex, "PartyViewKR: manual listing refresh stopped");
        }
    }

    public void Dispose()
    {
        disposed = true;
        ObjectiveTrace.Dispose();
        Refresh.Cancel();
        AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "LookingForGroupDetail", OnDetailUpdated);
        AddonLifecycle.UnregisterListener(AddonEvent.PostRefresh, "LookingForGroupDetail", OnDetailUpdated);
        AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "LookingForGroupCondition", OnEditorUpdated);
        AddonLifecycle.UnregisterListener(AddonEvent.PostRefresh, "LookingForGroupCondition", OnEditorUpdated);
        AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "LookingForGroupCondition", OnEditorClosed);
        AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "LookingForGroupSelectRole", OnRoleOpened);
        AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "LookingForGroupSelectRole", OnRoleClosed);
        Framework.Update -= OnUpdate;
        PluginInterface.UiBuilder.Draw -= windows.Draw;
        PluginInterface.UiBuilder.OpenMainUi -= Open;
        PluginInterface.UiBuilder.OpenConfigUi -= Open;
        Commands.RemoveHandler("/pvkr");
        Commands.RemoveHandler("/partyviewkr");
        windows.RemoveAllWindows();
    }
}
