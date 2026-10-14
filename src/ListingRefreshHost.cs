using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace PartyViewKR;

internal sealed unsafe class ListingRefreshHost(Plugin plugin, ListingReader reader) : IListingRefreshHost
{
    private long editorRevision, editorClosedRevision;
    private RefreshTarget? editingTarget;
    private long editingFromRevision, editingFromClosedRevision;
    private RecruitmentEditorMode buttonMode;
    private bool buttonTextReadable, buttonEnabled;
    private int buttonTextChars;
    private string check = "not_started";
    internal string Diagnostic => $"refreshNativeCheck={check}; refreshEditorRevision={EditorRevision}; refreshEditorClosedRevision={EditorClosedRevision}\n" +
        $"refreshButtonMode={buttonMode}; refreshButtonTextReadable={buttonTextReadable}; refreshButtonEnabled={buttonEnabled}; refreshButtonTextChars={buttonTextChars}";
    public ulong SelfId => Plugin.PlayerState.IsLoaded ? Plugin.PlayerState.ContentId : 0;
    public bool QueryBusy => reader.Busy;
    public bool QuerySucceeded => reader.LastOwnRequestSucceeded;
    public long DetailRevision => reader.DetailRevision;
    public long EditorRevision => Interlocked.Read(ref editorRevision);
    public long EditorClosedRevision => Interlocked.Read(ref editorClosedRevision);
    public bool EditorVisible => Visible("LookingForGroupCondition");
    public bool EditorReady => ReadEditorAction();
    public RecruitmentEditorMode EditorActionMode { get { ReadEditorAction(); return buttonMode; } }

    public string? BlockReason
    {
        get
        {
            if (!Plugin.ClientState.IsLoggedIn || SelfId == 0) return "로그인 상태가 아니어서 중단했습니다.";
            if (PartyReader.LoadedNative != PartyReader.ExpectedNative) return "구조 DLL 버전이 달라 갱신을 중단했습니다.";
            if (Plugin.Condition[ConditionFlag.BetweenAreas] || Plugin.Condition[ConditionFlag.BetweenAreas51]) return "지역 이동 중이어서 중단했습니다.";
            if (Visible("SelectYesno")) return "게임의 확인 창을 먼저 처리한 뒤 다시 눌러 주세요.";
            return null;
        }
    }

    internal void EditorUpdated() => Interlocked.Increment(ref editorRevision);
    internal void EditorClosed() => Interlocked.Increment(ref editorClosedRevision);
    public void QueryOwn() => reader.RequestOwn();
    public void CancelQuery() => reader.FailRequest();

    public bool TryGetOwn(out RefreshTarget target, out string error)
    {
        target = null!;
        error = "현재 모집글의 작성자가 본인인지 확인되지 않았습니다. 진단 정보를 확인해 주세요.";
        var listing = plugin.Listing;
        var visible = Visible("LookingForGroupDetail");
        var ready = Ready("LookingForGroupDetail");
        if (!listing.IsLive || listing.Owner != ListingOwner.Self || !ready)
        {
            // These are the values from the refresh check, not the later passive
            // listing snapshot that keeps updating after a failed attempt.
            check = $"detail_waiting; checkedLive={listing.IsLive}; checkedOwner={listing.Owner}; checkedVisible={visible}; checkedReady={ready}";
            return false;
        }
        var agent = AgentLookingForGroup.Instance();
        if (!NativeReadGuard.Readable(agent, (nuint)sizeof(AgentLookingForGroup)))
        { check = "agent_unreadable"; return false; }
        ref var detail = ref agent->LastViewedListing;
        if (detail.LeaderContentId != SelfId || detail.ListingId == 0 || detail.TotalSlots is < 1 or > 48 ||
            !NativeReadGuard.FixedText(detail.Leader, out var name) || name != Plugin.PlayerState.CharacterName ||
            !NativeReadGuard.FixedText(detail.Comment, out var comment) || !ListingIdentity.SameText(comment, listing.Description))
        { check = "native_detail_mismatch"; return false; }
        target = new(SelfId, detail.ListingId, detail.DutyId, listing.Description,
            DetailCriteria(ref detail), RefreshEvidence.ParseSeconds(listing.Remaining), detail.TotalSlots);
        check = "own_verified";
        error = "";
        return true;
    }

    public bool OpenEditor(RefreshTarget target, out string error)
    {
        editingTarget = null;
        if (!TryGetOwn(out var current, out error)) return false;
        if (current.ListingId != target.ListingId || !RefreshEvidence.SameRecruitment(target, current))
        { check = "detail_changed_before_edit"; error = "조회한 모집글이 변경되어 중단했습니다."; return false; }
        var detail = Plugin.GameGui.GetAddonByName<AtkUnitBase>("LookingForGroupDetail", 1);
        editingFromRevision = EditorRevision;
        editingFromClosedRevision = EditorClosedRevision;
        check = "opening_existing_criteria";
        if (!ClickZero(detail, out error)) return false;
        editingTarget = target;
        return true;
    }

    public bool Submit(RefreshTarget target, out string error)
    {
        error = "기존 모집 조건을 확인하지 못해 재등록하지 않았습니다.";
        if (SelfId != target.SelfId || BlockReason != null) { check = "session_changed_before_submit"; return false; }
        if (!ReferenceEquals(editingTarget, target) || EditorRevision <= editingFromRevision ||
            EditorClosedRevision != editingFromClosedRevision || target.TotalSlots is < 1 or > 48)
        { check = "editor_not_bound_to_verified_own_listing"; return false; }
        if (!ReadEditorAction() || buttonMode != RecruitmentEditorMode.Refresh)
        {
            check = "editor_not_in_refresh_mode";
            error = "‘모집 내용 갱신’ 버튼을 확인하지 못해 실행하지 않았습니다.";
            return false;
        }
        var addon = Plugin.GameGui.GetAddonByName<AddonLookingForGroupCondition>("LookingForGroupCondition", 1);
        var agent = AgentLookingForGroup.Instance();
        if (!NativeReadGuard.Readable(addon, (nuint)sizeof(AddonLookingForGroupCondition)) || !addon->IsReady || !addon->IsVisible ||
            !NativeReadGuard.Readable(agent, (nuint)sizeof(AgentLookingForGroup)))
        { check = "editor_or_agent_not_ready"; return false; }
        // In the KR client, entering the edit screen stops recruitment. Bind to
        // the own listing verified BEFORE opening this editor, its lifecycle,
        // and the visible refresh action; active-listing fields may now clear.

        var input = addon->CommentTextInput;
        var inputRead = NativeReadGuard.Readable(input, (nuint)sizeof(AtkComponentTextInput));
        var text = "";
        inputRead = inputRead && NativeReadGuard.Text(&input->EvaluatedString, out text);
        if (NativeReadGuard.Readable(input, (nuint)sizeof(AtkComponentTextInput)) &&
            (!inputRead || !ListingIdentity.SameText(text, target.Description)))
            inputRead = NativeReadGuard.Text(&input->RawString, out text);
        // Empty comments are valid, but a non-empty comment must agree with the visible input.
        if (!inputRead || !ListingIdentity.SameText(text, target.Description))
        { check = $"editor_comment_mismatch; editorTextReadable={inputRead}; editorTextChars={text.Length}"; return false; }
        ref var draft = ref agent->StoredRecruitmentInfo;
        var storedCommentAgrees = NativeReadGuard.FixedText(draft.Comment, out var storedComment) &&
            ListingIdentity.SameText(storedComment, target.Description);
        var draftCriteria = DraftCriteria(ref draft, target.TotalSlots);
        if (!storedCommentAgrees || draftCriteria != target.Criteria)
        {
            check = $"stored_criteria_mismatch; draftCommentAgrees={storedCommentAgrees}; " +
                $"draftDutyAgrees={draft.SelectedDutyId == target.DutyId}; draftDifferentFields={DifferentCriteria(target.Criteria, draftCriteria)}";
            return false;
        }

        // Recheck immediately before dispatch, including the localized label.
        if (!ReadEditorAction() || buttonMode != RecruitmentEditorMode.Refresh)
        { check = "refresh_button_changed_before_submit"; error = "게임의 갱신 버튼 상태가 바뀌어 중단했습니다."; return false; }
        check = "submit_existing_criteria_once";
        editingTarget = null;
        return ClickZero((AtkUnitBase*)addon, out error);
    }

    private bool ReadEditorAction()
    {
        buttonMode = RecruitmentEditorMode.Unknown;
        buttonTextReadable = buttonEnabled = false;
        buttonTextChars = 0;
        var addon = Plugin.GameGui.GetAddonByName<AddonLookingForGroupCondition>("LookingForGroupCondition", 1);
        if (!NativeReadGuard.Readable(addon, (nuint)sizeof(AddonLookingForGroupCondition)) || !addon->IsVisible || !addon->IsReady)
            return false;
        var button = addon->RecruitMembersButton;
        if (!NativeReadGuard.Readable(button, (nuint)sizeof(AtkComponentButton))) return false;
        buttonTextReadable = NativeReadGuard.TextNode(button->ButtonTextNode, out var text);
        buttonTextChars = text.Length;
        if (buttonTextReadable) buttonMode = RefreshEvidence.ClassifyEditorAction(text);
        buttonEnabled = button->IsEnabled && NativeReadGuard.Readable(button->OwnerNode, (nuint)sizeof(AtkComponentNode)) &&
            button->OwnerNode->IsVisible();
        return buttonTextReadable && buttonEnabled;
    }

    private static string DetailCriteria(ref AgentLookingForGroup.Detailed detail) =>
        Criteria((uint)detail.Category, detail.DutyId, (byte)detail.Objective, detail.BeginnerFriendly,
            (byte)detail.CompletionStatus, (byte)detail.DutyFinderSettingFlags, (byte)detail.LootRule,
            (byte)detail.LanguageFlags, detail.SlotFlags[..detail.TotalSlots]);

    private static string DraftCriteria(ref AgentLookingForGroup.RecruitmentSub draft, int slots) =>
        Criteria((uint)draft.SelectedCategory, draft.SelectedDutyId, (byte)draft.Objective, draft.BeginnerFriendly,
            (byte)draft.CompletionStatus, (byte)draft.DutyFinderSettingFlags, (byte)draft.LootRule,
            (byte)draft.LanguageFlags, draft.SlotFlags[..slots]);

    private static string Criteria(uint category, ushort duty, byte objective, byte beginner, byte completion,
        byte finder, byte loot, byte language, ReadOnlySpan<ulong> roles) =>
        $"{category}/{duty}/{objective}/{beginner}/{completion}/{finder}/{loot}/{language}/" +
        Convert.ToHexString(MemoryMarshal.AsBytes(roles));

    private static string DifferentCriteria(string expected, string actual)
    {
        string[] fields = ["category", "duty", "objective", "beginner", "completion", "finder", "loot", "language", "roles"];
        var before = expected.Split('/');
        var after = actual.Split('/');
        if (before.Length != fields.Length || after.Length != fields.Length) return "format";
        return string.Join(",", fields.Where((_, i) => before[i] != after[i]));
    }

    private static bool ClickZero(AtkUnitBase* addon, out string error)
    {
        error = "대상 창이 준비되지 않아 중단했습니다.";
        if (!NativeReadGuard.Readable(addon, (nuint)sizeof(AtkUnitBase)) || !addon->IsReady || !addon->IsVisible ||
            !NativeReadGuard.Readable(addon->VirtualTable, (nuint)sizeof(AtkUnitBase.AtkUnitBaseVirtualTable))) return false;
        // Verified in the supplied PFRemote 1.0.0.12 IL (methods 156, 426, 427):
        // ReceiveEvent(ButtonClick=25, 0, zero event, zero eventData), first on
        // LookingForGroupDetail, then LookingForGroupCondition. Not FireCallback(0).
        var evt = new AtkEvent();
        var data = new AtkEventData();
        addon->ReceiveEvent(AtkEventType.ButtonClick, 0, &evt, &data);
        error = "";
        return true;
    }

    private static bool Visible(string name)
    {
        var addon = Plugin.GameGui.GetAddonByName<AtkUnitBase>(name, 1);
        return NativeReadGuard.Readable(addon, (nuint)sizeof(AtkUnitBase)) && addon->IsVisible;
    }

    private static bool Ready(string name)
    {
        var addon = Plugin.GameGui.GetAddonByName<AtkUnitBase>(name, 1);
        return NativeReadGuard.Readable(addon, (nuint)sizeof(AtkUnitBase)) && addon->IsVisible && addon->IsReady;
    }
}
