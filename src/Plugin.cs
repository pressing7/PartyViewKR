using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

namespace PartyViewKR;

public sealed partial class Plugin : IDalamudPlugin
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
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
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
    private bool requestedUniqueJobs;
    private bool requestedPrivate;
    private bool requestedCompletion;
    private bool requestedItemLevel;
    private ListingRuleKind? requestedRule;
    internal ListingRuleDraft? RuleDraft { get; private set; }
    internal string RuleDraftStatus { get; private set; } = "";
    internal ListingJobDraft? JobDraft { get; private set; }
    internal string JobDraftStatus { get; private set; } = "";
    internal ListingDescriptionDraft? DescriptionDraft { get; private set; }
    internal string DescriptionDraftStatus { get; private set; } = "";
    internal ListingObjectiveDraft? ObjectiveDraft { get; private set; }
    internal string ObjectiveDraftStatus { get; private set; } = "";
    internal ListingBeginnerDraft? BeginnerDraft { get; private set; }
    internal string BeginnerDraftStatus { get; private set; } = "";
    internal ListingCompletionDraft? CompletionDraft { get; private set; }
    internal string CompletionDraftStatus { get; private set; } = "";
    internal ListingItemLevelDraft? ItemLevelDraft { get; private set; }
    internal string ItemLevelDraftStatus { get; private set; } = "";
    internal ListingPrivateDraft? PrivateDraft { get; private set; }
    internal string PrivateDraftStatus { get; private set; } = "";
    internal ListingUniqueJobsDraft? UniqueJobsDraft { get; private set; }
    internal string UniqueJobsDraftStatus { get; private set; } = "";
    internal PartySnapshot Snapshot { get; private set; } = PartySnapshot.Empty;
    internal ListingSnapshot Listing { get; private set; } = ListingSnapshot.Empty;
    internal ListingRefresh Refresh { get; }
    internal ListingPendingChanges PendingChanges { get; } = new();
    internal string PendingStatus { get; private set; } = "";
    internal bool CanQueue(object edit) => PendingChanges.CanQueue(new(edit));
    private bool QueueChange(object edit)
    {
        var queued = PendingChanges.Queue(new(edit), out var error);
        PendingStatus = error;
        return queued;
    }
    internal ListingObjectiveTrace ObjectiveTrace { get; }
    internal bool ListingBusy => listingReader.Busy;
    internal string Diagnostic => Snapshot.Diagnostic + "\n" + Listing.Diagnostic + "\n" + Refresh.Diagnostic + "\n" + refreshHost.Diagnostic + "\n" + ObjectiveTrace.Diagnostic + "\n" + afk.Diagnostic + "\n" + afkReader.Diagnostic;

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
        InitializeChatCapture();
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
    internal void QueryOwnListing() => TryQueryOwnListing(out _);
    internal void RefreshOwnListing()
    {
        if (ObjectiveTrace.Busy || Refresh.Busy || ListingBusy || Refresh.CoolingDown(Environment.TickCount64) ||
            JobDraft != null || DescriptionDraft != null || ObjectiveDraft != null || BeginnerDraft != null || UniqueJobsDraft != null ||
            PrivateDraft != null || CompletionDraft != null || ItemLevelDraft != null || RuleDraft != null) return;
        if (PendingChanges.Items.Count > 0)
            Refresh.RequestBatch(Environment.TickCount64, PendingChanges);
        else Refresh.Request(Environment.TickCount64);
        PendingStatus = "";
        RequestRead();
    }
    internal void CancelRefresh() { Refresh.Cancel(); RequestRead(); }
    internal void RequestSlotEdit(int position) { if (!ObjectiveTrace.Busy && !Refresh.Busy && RuleDraft == null && ItemLevelDraft == null && CompletionDraft == null && DescriptionDraft == null && ObjectiveDraft == null && BeginnerDraft == null && UniqueJobsDraft == null && PrivateDraft == null) { requestedSlot = position; RequestRead(); } }
    internal void RequestDescriptionEdit() { if (!ObjectiveTrace.Busy && !Refresh.Busy && RuleDraft == null && ItemLevelDraft == null && CompletionDraft == null && JobDraft == null && ObjectiveDraft == null && BeginnerDraft == null && UniqueJobsDraft == null && PrivateDraft == null) { requestedDescription = true; RequestRead(); } }
    internal void RequestObjectiveEdit() { if (ListingObjectives.AutomaticEditingAvailable && !ObjectiveTrace.Busy && !Refresh.Busy && RuleDraft == null && ItemLevelDraft == null && CompletionDraft == null && JobDraft == null && DescriptionDraft == null && BeginnerDraft == null && UniqueJobsDraft == null && PrivateDraft == null) { requestedObjective = true; RequestRead(); } }
    internal void CloseDescriptionDraft() => DescriptionDraft = null;
    internal void CloseJobDraft() => JobDraft = null;
    internal void CloseObjectiveDraft() => ObjectiveDraft = null;
    internal void RequestBeginnerEdit()
    {
        if (!ObjectiveTrace.Busy && !Refresh.Busy && RuleDraft == null && ItemLevelDraft == null && CompletionDraft == null && JobDraft == null && DescriptionDraft == null && ObjectiveDraft == null && UniqueJobsDraft == null && PrivateDraft == null)
        { requestedBeginner = true; RequestRead(); }
    }
    internal void CloseBeginnerDraft() => BeginnerDraft = null;
    internal bool ApplyBeginnerDraft()
    {
        if (RuleDraft != null || ItemLevelDraft != null || CompletionDraft != null || ObjectiveTrace.Busy || BeginnerDraft == null || JobDraft != null || DescriptionDraft != null || ObjectiveDraft != null || UniqueJobsDraft != null || PrivateDraft != null ||
            !CanQueue(BeginnerDraft.Request) || Refresh.Busy || ListingBusy || Refresh.CoolingDown(Environment.TickCount64)) return false;
        if (!QueueChange(BeginnerDraft.Request)) return false;
        BeginnerDraft = null; BeginnerDraftStatus = "";
        RequestRead();
        return true;
    }
    internal void RequestUniqueJobsEdit()
    {
        if (!ObjectiveTrace.Busy && !Refresh.Busy && RuleDraft == null && ItemLevelDraft == null && CompletionDraft == null && JobDraft == null && DescriptionDraft == null && ObjectiveDraft == null && BeginnerDraft == null && PrivateDraft == null)
        { requestedUniqueJobs = true; RequestRead(); }
    }
    internal void CloseUniqueJobsDraft() => UniqueJobsDraft = null;
    internal bool ApplyUniqueJobsDraft()
    {
        if (RuleDraft != null || ItemLevelDraft != null || CompletionDraft != null || ObjectiveTrace.Busy || UniqueJobsDraft == null || PrivateDraft != null || JobDraft != null || DescriptionDraft != null || ObjectiveDraft != null || BeginnerDraft != null ||
            !CanQueue(UniqueJobsDraft.Request) || Refresh.Busy || ListingBusy || Refresh.CoolingDown(Environment.TickCount64)) return false;
        if (!QueueChange(UniqueJobsDraft.Request)) return false;
        UniqueJobsDraft = null; UniqueJobsDraftStatus = "";
        RequestRead();
        return true;
    }
    internal void RequestPrivateEdit()
    {
        if (!ObjectiveTrace.Busy && !Refresh.Busy && RuleDraft == null && ItemLevelDraft == null && CompletionDraft == null && JobDraft == null && DescriptionDraft == null && ObjectiveDraft == null && BeginnerDraft == null && UniqueJobsDraft == null)
        { requestedPrivate = true; RequestRead(); }
    }
    internal void ClosePrivateDraft() => PrivateDraft = null;
    internal bool ApplyPrivateDraft()
    {
        if (RuleDraft != null || ItemLevelDraft != null || CompletionDraft != null || ObjectiveTrace.Busy || PrivateDraft == null || JobDraft != null || DescriptionDraft != null || ObjectiveDraft != null || BeginnerDraft != null || UniqueJobsDraft != null ||
            !CanQueue(PrivateDraft.Request) || Refresh.Busy || ListingBusy || Refresh.CoolingDown(Environment.TickCount64)) return false;
        if (!QueueChange(PrivateDraft.Request)) return false;
        PrivateDraft = null; PrivateDraftStatus = "";
        RequestRead();
        return true;
    }
    internal void RequestCompletionEdit()
    {
        if (!ObjectiveTrace.Busy && !Refresh.Busy && RuleDraft == null && ItemLevelDraft == null && JobDraft == null && DescriptionDraft == null && ObjectiveDraft == null && BeginnerDraft == null && UniqueJobsDraft == null && PrivateDraft == null)
        { requestedCompletion = true; RequestRead(); }
    }
    internal void CloseCompletionDraft() => CompletionDraft = null;
    internal bool ApplyCompletionDraft()
    {
        if (RuleDraft != null || ItemLevelDraft != null || ObjectiveTrace.Busy || CompletionDraft == null || JobDraft != null || DescriptionDraft != null || ObjectiveDraft != null || BeginnerDraft != null || UniqueJobsDraft != null || PrivateDraft != null ||
            !CanQueue(CompletionDraft.Request) || Refresh.Busy || ListingBusy || Refresh.CoolingDown(Environment.TickCount64)) return false;
        if (!QueueChange(CompletionDraft.Request)) return false;
        CompletionDraft = null; CompletionDraftStatus = "";
        RequestRead();
        return true;
    }
    internal void RequestItemLevelEdit()
    {
        if (RuleDraft != null) return;
        if (!ObjectiveTrace.Busy && !Refresh.Busy && CompletionDraft == null && JobDraft == null && DescriptionDraft == null && ObjectiveDraft == null && BeginnerDraft == null && UniqueJobsDraft == null && PrivateDraft == null)
        { requestedItemLevel = true; RequestRead(); }
    }
    internal void CloseItemLevelDraft() => ItemLevelDraft = null;
    internal bool ApplyItemLevelDraft()
    {
        if (RuleDraft != null) return false;
        if (ObjectiveTrace.Busy || ItemLevelDraft == null || CompletionDraft != null || JobDraft != null || DescriptionDraft != null || ObjectiveDraft != null || BeginnerDraft != null || UniqueJobsDraft != null || PrivateDraft != null ||
            !CanQueue(ItemLevelDraft.Request) || Refresh.Busy || ListingBusy || Refresh.CoolingDown(Environment.TickCount64)) return false;
        if (!QueueChange(ItemLevelDraft.Request)) return false;
        ItemLevelDraft = null; ItemLevelDraftStatus = "";
        RequestRead();
        return true;
    }
    internal void RequestRuleEdit(ListingRuleKind kind)
    {
        if (!ObjectiveTrace.Busy && !Refresh.Busy && RuleDraft == null && ItemLevelDraft == null && CompletionDraft == null &&
            JobDraft == null && DescriptionDraft == null && ObjectiveDraft == null && BeginnerDraft == null && UniqueJobsDraft == null && PrivateDraft == null)
        { requestedRule = kind; RequestRead(); }
    }
    internal void CloseRuleDraft() => RuleDraft = null;
    internal bool ApplyRuleDraft()
    {
        if (ObjectiveTrace.Busy || RuleDraft == null || ItemLevelDraft != null || CompletionDraft != null || JobDraft != null ||
            DescriptionDraft != null || ObjectiveDraft != null || BeginnerDraft != null || UniqueJobsDraft != null || PrivateDraft != null ||
            !CanQueue(RuleDraft.Request) || Refresh.Busy || ListingBusy || Refresh.CoolingDown(Environment.TickCount64)) return false;
        if (!QueueChange(RuleDraft.Request)) return false;
        RuleDraft = null; RuleDraftStatus = "";
        RequestRead();
        return true;
    }
    internal bool ApplyJobDraft()
    {
        if (RuleDraft != null || ItemLevelDraft != null || CompletionDraft != null || ObjectiveTrace.Busy) return false;
        if (JobDraft == null || DescriptionDraft != null || ObjectiveDraft != null || BeginnerDraft != null || UniqueJobsDraft != null || PrivateDraft != null || !CanQueue(JobDraft.Request) || Refresh.Busy || ListingBusy || Refresh.CoolingDown(Environment.TickCount64)) return false;
        if (!QueueChange(JobDraft.Request)) return false;
        JobDraft = null; JobDraftStatus = "";
        RequestRead();
        return true;
    }
    internal bool ApplyDescriptionDraft()
    {
        if (RuleDraft != null || ItemLevelDraft != null || CompletionDraft != null || ObjectiveTrace.Busy) return false;
        if (DescriptionDraft == null || JobDraft != null || ObjectiveDraft != null || BeginnerDraft != null || UniqueJobsDraft != null || PrivateDraft != null || !CanQueue(DescriptionDraft.Request) || Refresh.Busy || ListingBusy || Refresh.CoolingDown(Environment.TickCount64)) return false;
        if (!QueueChange(DescriptionDraft.Request)) return false;
        DescriptionDraft = null; DescriptionDraftStatus = "";
        RequestRead();
        return true;
    }
    internal bool ApplyObjectiveDraft()
    {
        if (RuleDraft != null || ItemLevelDraft != null || CompletionDraft != null || !ListingObjectives.AutomaticEditingAvailable || ObjectiveTrace.Busy) return false;
        if (ObjectiveDraft == null || JobDraft != null || DescriptionDraft != null || BeginnerDraft != null || UniqueJobsDraft != null || PrivateDraft != null || !CanQueue(ObjectiveDraft.Request) ||
            Refresh.Busy || ListingBusy || Refresh.CoolingDown(Environment.TickCount64)) return false;
        if (!QueueChange(ObjectiveDraft.Request)) return false;
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
        if (RuleDraft != null || ItemLevelDraft != null || CompletionDraft != null || Refresh.Busy || ListingBusy || DescriptionDraft != null || ObjectiveDraft != null || BeginnerDraft != null || UniqueJobsDraft != null || PrivateDraft != null) return;
        if (refreshHost.BlockReason is { } reason) { JobDraftStatus = reason; return; }
        if (!refreshHost.TryGetOwn(out var target, out var error)) { JobDraftStatus = error; return; }
        if (!ListingJobEdit.Supports(target))
        { JobDraftStatus = "직업 수정은 일반 4·8인, 연합 24·48인 및 친선경기 5·5·8인 모집에서 지원합니다. 파티 구성을 확인한 뒤 다시 시도해 주세요."; return; }
        if (position < 1 || position > target.TotalSlots || target.Slots.Slots[position - 1].HasMember || !target.Slots.JobRules.Complete)
        { JobDraftStatus = "수정할 빈자리의 직업 조건을 확인하지 못했습니다. 다시 조회해 주세요."; return; }
        JobDraft = new(target, position, target.Slots.JobRules);
        if (PendingChanges.Find(new ListingChange(JobDraft.Request).Key)?.Edit is ListingJobEdit saved) { JobDraft.Selected = saved.Selected; }
        JobDraftStatus = "";
    }

    private void PrepareDescriptionDraft()
    {
        if (!ClientState.IsLoggedIn || !PlayerState.IsLoaded || DescriptionDraft != null && DescriptionDraft.Original.SelfId != PlayerState.ContentId)
            DescriptionDraft = null;
        if (!requestedDescription) return;
        requestedDescription = false;
        if (RuleDraft != null || ItemLevelDraft != null || CompletionDraft != null || Refresh.Busy || ListingBusy || JobDraft != null || ObjectiveDraft != null || BeginnerDraft != null || UniqueJobsDraft != null || PrivateDraft != null) return;
        if (refreshHost.BlockReason is { } reason) { DescriptionDraftStatus = reason; return; }
        if (!refreshHost.TryGetOwn(out var target, out var error)) { DescriptionDraftStatus = error; return; }
        if (!ListingJobEdit.SupportsSettings(target))
        { DescriptionDraftStatus = "등록된 자리와 파티 구성을 확인한 뒤 소개말을 수정할 수 있습니다."; return; }
        DescriptionDraft = new(target);
        if (PendingChanges.Find(new ListingChange(DescriptionDraft.Request).Key)?.Edit is ListingDescriptionEdit saved) { DescriptionDraft.Text = saved.Value; }
        DescriptionDraftStatus = "";
    }

    private void PrepareObjectiveDraft()
    {
        if (!ClientState.IsLoggedIn || !PlayerState.IsLoaded || ObjectiveDraft != null && ObjectiveDraft.Original.SelfId != PlayerState.ContentId)
            ObjectiveDraft = null;
        if (!requestedObjective) return;
        requestedObjective = false;
        if (RuleDraft != null || ItemLevelDraft != null || CompletionDraft != null || Refresh.Busy || ListingBusy || JobDraft != null || DescriptionDraft != null || BeginnerDraft != null || UniqueJobsDraft != null || PrivateDraft != null) return;
        if (refreshHost.BlockReason is { } reason) { ObjectiveDraftStatus = reason; return; }
        if (!refreshHost.TryGetOwn(out var target, out var error)) { ObjectiveDraftStatus = error; return; }
        if (!ListingObjectiveEdit.Supports(target))
        { ObjectiveDraftStatus = "현재 모집의 목적과 파티 구성을 확인한 뒤 수정할 수 있습니다."; return; }
        ObjectiveDraft = new(target);
        if (PendingChanges.Find(new ListingChange(ObjectiveDraft.Request).Key)?.Edit is ListingObjectiveEdit saved) { ObjectiveDraft.SelectedIndex = saved.SelectedIndex; }
        ObjectiveDraftStatus = "";
    }

    private void PrepareBeginnerDraft()
    {
        if (!ClientState.IsLoggedIn || !PlayerState.IsLoaded || BeginnerDraft != null && BeginnerDraft.Original.SelfId != PlayerState.ContentId)
            BeginnerDraft = null;
        if (!requestedBeginner) return;
        requestedBeginner = false;
        if (RuleDraft != null || ItemLevelDraft != null || CompletionDraft != null || Refresh.Busy || ListingBusy || JobDraft != null || DescriptionDraft != null || ObjectiveDraft != null || UniqueJobsDraft != null || PrivateDraft != null) return;
        if (refreshHost.BlockReason is { } reason) { BeginnerDraftStatus = reason; return; }
        if (!refreshHost.TryGetOwn(out var target, out var error)) { BeginnerDraftStatus = error; return; }
        if (!ListingBeginnerEdit.Supports(target))
        { BeginnerDraftStatus = "현재 모집의 초보자 환영 설정과 파티 구성을 확인한 뒤 수정할 수 있습니다."; return; }
        BeginnerDraft = new(target);
        if (PendingChanges.Find(new ListingChange(BeginnerDraft.Request).Key)?.Edit is ListingBeginnerEdit saved) { BeginnerDraft.Enabled = saved.Enabled; }
        BeginnerDraftStatus = "";
    }

    private void PrepareUniqueJobsDraft()
    {
        if (!ClientState.IsLoggedIn || !PlayerState.IsLoaded || UniqueJobsDraft != null && UniqueJobsDraft.Original.SelfId != PlayerState.ContentId)
            UniqueJobsDraft = null;
        if (!requestedUniqueJobs) return;
        requestedUniqueJobs = false;
        if (RuleDraft != null || ItemLevelDraft != null || CompletionDraft != null || Refresh.Busy || ListingBusy || JobDraft != null || DescriptionDraft != null || ObjectiveDraft != null || BeginnerDraft != null || PrivateDraft != null) return;
        if (refreshHost.BlockReason is { } reason) { UniqueJobsDraftStatus = reason; return; }
        if (!refreshHost.TryGetOwn(out var target, out var error)) { UniqueJobsDraftStatus = error; return; }
        if (!ListingUniqueJobsEdit.Supports(target))
        { UniqueJobsDraftStatus = "현재 모집의 잡 중복 없음 설정과 파티 구성을 확인한 뒤 수정할 수 있습니다."; return; }
        UniqueJobsDraft = new(target);
        if (PendingChanges.Find(new ListingChange(UniqueJobsDraft.Request).Key)?.Edit is ListingUniqueJobsEdit saved) { UniqueJobsDraft.Enabled = saved.Enabled; }
        UniqueJobsDraftStatus = "";
    }

    private void PreparePrivateDraft()
    {
        if (!ClientState.IsLoggedIn || !PlayerState.IsLoaded || PrivateDraft != null && PrivateDraft.Original.SelfId != PlayerState.ContentId)
            PrivateDraft = null;
        if (!requestedPrivate) return;
        requestedPrivate = false;
        if (RuleDraft != null || ItemLevelDraft != null || CompletionDraft != null || Refresh.Busy || ListingBusy || JobDraft != null || DescriptionDraft != null || ObjectiveDraft != null || BeginnerDraft != null || UniqueJobsDraft != null) return;
        if (refreshHost.BlockReason is { } reason) { PrivateDraftStatus = reason; return; }
        if (!refreshHost.TryGetOwn(out var target, out var error)) { PrivateDraftStatus = error; return; }
        if (!ListingPrivateEdit.Supports(target))
        { PrivateDraftStatus = "현재 모집의 비공개 설정과 파티 구성을 확인한 뒤 수정할 수 있습니다."; return; }
        PrivateDraft = new(target);
        if (PendingChanges.Find(new ListingChange(PrivateDraft.Request).Key)?.Edit is ListingPrivateEdit saved) { PrivateDraft.Enabled = saved.Enabled; PrivateDraft.Password = saved.Password; }
        PrivateDraftStatus = "";
    }

    private void PrepareCompletionDraft()
    {
        if (!ClientState.IsLoggedIn || !PlayerState.IsLoaded || CompletionDraft != null && CompletionDraft.Original.SelfId != PlayerState.ContentId)
            CompletionDraft = null;
        if (!requestedCompletion) return;
        requestedCompletion = false;
        if (RuleDraft != null || ItemLevelDraft != null || Refresh.Busy || ListingBusy || JobDraft != null || DescriptionDraft != null || ObjectiveDraft != null || BeginnerDraft != null || UniqueJobsDraft != null || PrivateDraft != null) return;
        if (refreshHost.BlockReason is { } reason) { CompletionDraftStatus = reason; return; }
        if (!refreshHost.TryGetOwn(out var target, out var error)) { CompletionDraftStatus = error; return; }
        if (!ListingCompletionEdit.Supports(target))
        { CompletionDraftStatus = "현재 모집의 완료조건 설정과 파티 구성을 확인한 뒤 수정할 수 있습니다."; return; }
        CompletionDraft = new(target);
        if (PendingChanges.Find(new ListingChange(CompletionDraft.Request).Key)?.Edit is ListingCompletionEdit saved) { CompletionDraft.Enabled = saved.Enabled; CompletionDraft.SelectedIndex = saved.SelectedIndex; }
        CompletionDraftStatus = "";
    }

    private void PrepareItemLevelDraft()
    {
        if (!ClientState.IsLoggedIn || !PlayerState.IsLoaded || ItemLevelDraft != null && ItemLevelDraft.Original.SelfId != PlayerState.ContentId)
            ItemLevelDraft = null;
        if (!requestedItemLevel) return;
        requestedItemLevel = false;
        if (RuleDraft != null) return;
        if (CompletionDraft != null || Refresh.Busy || ListingBusy || JobDraft != null || DescriptionDraft != null || ObjectiveDraft != null || BeginnerDraft != null || UniqueJobsDraft != null || PrivateDraft != null) return;
        if (refreshHost.BlockReason is { } reason) { ItemLevelDraftStatus = reason; return; }
        if (!refreshHost.TryGetOwn(out var target, out var error)) { ItemLevelDraftStatus = error; return; }
        if (!ListingItemLevelEdit.Supports(target))
        { ItemLevelDraftStatus = "현재 모집의 평균 아이템 레벨 설정과 파티 구성을 확인한 뒤 수정할 수 있습니다."; return; }
        ItemLevelDraft = new(target);
        if (PendingChanges.Find(new ListingChange(ItemLevelDraft.Request).Key)?.Edit is ListingItemLevelEdit saved) { ItemLevelDraft.Enabled = saved.Enabled; ItemLevelDraft.LevelText = saved.LevelText; }
        ItemLevelDraftStatus = "";
    }

    private void PrepareRuleDraft()
    {
        if (!ClientState.IsLoggedIn || !PlayerState.IsLoaded || RuleDraft != null && RuleDraft.Original.SelfId != PlayerState.ContentId)
            RuleDraft = null;
        if (requestedRule is not { } kind) return;
        requestedRule = null;
        if (Refresh.Busy || ListingBusy || RuleDraft != null || ItemLevelDraft != null || CompletionDraft != null || JobDraft != null ||
            DescriptionDraft != null || ObjectiveDraft != null || BeginnerDraft != null || UniqueJobsDraft != null || PrivateDraft != null) return;
        if (refreshHost.BlockReason is { } reason) { RuleDraftStatus = reason; return; }
        if (!refreshHost.TryGetOwn(out var target, out var error)) { RuleDraftStatus = error; return; }
        if (!ListingRuleEdit.Supports(target, kind))
        { RuleDraftStatus = "현재 모집의 규칙과 파티 구성을 확인한 뒤 수정할 수 있습니다."; return; }
        RuleDraft = new(target, kind);
        if (PendingChanges.Find(new ListingChange(RuleDraft.Request).Key)?.Edit is ListingRuleEdit saved) { RuleDraft.SelectedValue = saved.Value; }
        RuleDraftStatus = "";
    }

    private void OnUpdate(IFramework framework)
    {
        if (disposed) return;
        RestoreDiscordConnection();
        UpdateDiscordChat();
        ObjectiveTrace.Update();
        var now = Environment.TickCount64;
        if (!readRequested && now - lastRead < 500) return;
        var readParty = readRequested || window.IsOpen;
        readRequested = false;
        lastRead = now;
        ProcessRecovery(now);
        try
        {
            if (!ClientState.IsLoggedIn) Snapshot = PartySnapshot.Empty;
            else if (readParty) Snapshot = reader.Read(Snapshot);
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
            webHost?.BeforeRead(now);
            if (afkHost != null) afk.BeforeRead(afkHost, now);
            Listing = listingReader.Read(Snapshot, Listing);
            lastListingError = "";
        }
        catch (Exception ex)
        {
            var message = ex.GetBaseException().Message;
            listingReader.RecordFailure(message);
            if (Refresh.Busy && !(Refresh.IsBatch && Refresh.Stage is RefreshStage.OpeningEditor or RefreshStage.EditingBatch or RefreshStage.WaitingResult))
                Refresh.Fail(message);
            Listing = Listing with { Status = "모집글 조회 오류 — 마지막 결과", IsLive = false, Note = message,
                Diagnostic = "listingRead=failed\nlistingError=" + message };
            if (message != lastListingError) Log.Error(ex, "PartyViewKR: listing read failed");
            lastListingError = message;
        }
        if (!ClientState.IsLoggedIn || !PlayerState.IsLoaded ||
            PendingChanges.Items.FirstOrDefault() is { } pending && pending.Original.SelfId != PlayerState.ContentId)
            PendingChanges.Clear();
        try {
            if (!Recovering) {
                PrepareJobDraft(); PrepareDescriptionDraft(); PrepareObjectiveDraft(); PrepareBeginnerDraft(); PrepareUniqueJobsDraft(); PreparePrivateDraft(); PrepareCompletionDraft(); PrepareItemLevelDraft(); PrepareRuleDraft(); Refresh.Update(now);
            }
        }
        catch (Exception ex)
        {
            Refresh.Fail(ex.GetBaseException().Message);
            Log.Error(ex, "PartyViewKR: manual listing refresh stopped");
        }
        UpdateWeb(now);
        if (afkHost != null) afk.Update(afkHost, now);
        UpdateDiscordCompletion();
        UpdateDiscordChat();
    }

    public void Dispose()
    {
        disposed = true;
        chatCapture?.Dispose();
        if (afkHost != null) afk.SetEnabled(false, afkHost, Environment.TickCount64);
        StopWeb();
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
