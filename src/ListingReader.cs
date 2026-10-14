using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace PartyViewKR;

internal sealed record ListingSnapshot(string Status, ListingOwner Owner, string Leader, string Duty,
    string Description, string Remaining, DateTime? ReadAt, bool IsLive, string Note, string Diagnostic)
{
    internal static ListingSnapshot Empty { get; } = new("아직 읽은 모집글 없음", ListingOwner.Unknown,
        "", "", "", "", null, false, "내 모집글 조회를 누르거나 게임에서 모집글 상세 창을 열어주세요.", "listingRead=not_started");
}

internal sealed class ListingReader
{
    private bool userRequested;
    private bool openingMain;
    private bool showIssued;
    private long requestTick;
    private long? waitingRevision;
    private long detailRevision;
    private string requestNote = "";
    private string requestDiagnostic = "request=none";
    private ulong observedSelf;

    internal bool Busy => userRequested || openingMain || waitingRevision.HasValue;
    internal bool LastOwnRequestSucceeded { get; private set; }
    internal long DetailRevision => Interlocked.Read(ref detailRevision);
    internal void RequestOwn() { if (!Busy) { LastOwnRequestSucceeded = false; userRequested = true; } }
    internal void DetailUpdated() => Interlocked.Increment(ref detailRevision);

    internal unsafe ListingSnapshot Read(PartySnapshot party, ListingSnapshot previous)
    {
        if (!Plugin.ClientState.IsLoggedIn || !Plugin.PlayerState.IsLoaded)
        {
            Reset();
            return ListingSnapshot.Empty;
        }
        if (observedSelf != Plugin.PlayerState.ContentId)
        {
            var requested = userRequested;
            Reset();
            observedSelf = Plugin.PlayerState.ContentId;
            userRequested = requested;
            previous = ListingSnapshot.Empty;
        }
        if (PartyReader.LoadedNative != PartyReader.ExpectedNative)
        {
            userRequested = openingMain = false;
            waitingRevision = null;
            LastOwnRequestSucceeded = false;
            return ListingSnapshot.Empty with { Note = "구조 DLL 버전이 달라 모집글 조회를 중지했습니다.", Diagnostic = "listingRead=skipped; reason=assembly_mismatch" };
        }
        if (Plugin.Condition[ConditionFlag.BetweenAreas] || Plugin.Condition[ConditionFlag.BetweenAreas51])
            return previous with { Status = "이동 중 — 마지막 모집글", IsLive = false };

        HandleRequest();
        if (waitingRevision is { } expected && Interlocked.Read(ref detailRevision) <= expected)
        {
            if (Environment.TickCount64 - requestTick < 7000)
                return previous with { Status = "내 모집글 응답 대기 중", IsLive = false, Note = "게임의 상세 조회 응답을 기다리고 있습니다.", Diagnostic = requestDiagnostic + "; detailRefreshSeen=false" };
            waitingRevision = null;
            LastOwnRequestSucceeded = false;
            requestNote = "자동 조회의 새 응답을 확인하지 못했습니다. 게임에서 내 모집 확인 창을 열면 내용을 읽습니다.";
            requestDiagnostic += "; timedOut=true";
        }
        else if (waitingRevision.HasValue)
        {
            waitingRevision = null;
            LastOwnRequestSucceeded = true;
            requestNote = "";
            requestDiagnostic += "; detailRefreshSeen=true";
        }
        if (openingMain)
            return previous with { Status = "파티찾기 창 여는 중", IsLive = false, Note = "", Diagnostic = requestDiagnostic };

        var addon = Plugin.GameGui.GetAddonByName<AddonLookingForGroupDetail>("LookingForGroupDetail", 1);
        if (!NativeReadGuard.Readable(addon, (nuint)sizeof(AddonLookingForGroupDetail)) || !addon->IsVisible || !addon->IsReady)
            return previous with {
                Status = previous.ReadAt == null ? "모집글 상세 창 대기" : "마지막으로 읽은 모집글 — 상세 창 닫힘",
                IsLive = false,
                Note = requestNote.Length > 0 ? requestNote : "내 모집글 조회를 누르거나 게임에서 모집글 상세 창을 열어주세요.",
                Diagnostic = $"listingDetailVisible=false\n{requestDiagnostic}"
            };

        var leaderRead = NativeReadGuard.TextNode(addon->PartyLeaderTextNode, out var leader);
        var descriptionRead = NativeReadGuard.TextNode(addon->DescriptionTextNode, out var description);
        var descriptionSource = "DescriptionTextNode";
        if (!descriptionRead)
        {
            descriptionRead = NativeReadGuard.Text(&addon->DescriptionString, out description);
            descriptionSource = "DescriptionString";
        }
        var dutyRead = NativeReadGuard.TextNode(addon->DutyNameTextNode, out var duty);
        var timeRead = NativeReadGuard.TextNode(addon->TimeRemainingTextNode, out var remaining);

        var owner = ListingOwner.Unknown;
        var agentDiagnostic = "listingAgent=unavailable";
        var agent = AgentLookingForGroup.Instance();
        if (NativeReadGuard.Readable(agent, (nuint)sizeof(AgentLookingForGroup)))
        {
            ref var detail = ref agent->LastViewedListing;
            var nativeNameRead = NativeReadGuard.FixedText(detail.Leader, out var nativeName);
            var nativeCommentRead = NativeReadGuard.FixedText(detail.Comment, out var nativeComment);
            // OwnListingId and StoredRecruitmentInfo can be stale or draft data.
            // Ownership requires the received detail to agree with visible text.
            var nameAgrees = leaderRead && nativeNameRead && ListingIdentity.SameText(leader, nativeName);
            var commentAgrees = descriptionRead && nativeCommentRead && ListingIdentity.SameText(description, nativeComment);
            var shapePlausible = detail.ListingId != 0 && detail.TotalSlots is >= 1 and <= 48 && detail.SlotsFilled <= detail.TotalSlots;
            owner = ListingIdentity.Resolve(detail.LeaderContentId, nativeName,
                shapePlausible && nameAgrees && commentAgrees, Plugin.PlayerState.ContentId,
                Plugin.PlayerState.CharacterName, party.Members.Select(m => (m.ContentId, m.Name, m.IsLeader)).ToArray());
            agentDiagnostic = $"listingIdPresent={detail.ListingId != 0}; ownIdRaw={agent->OwnListingId}; " +
                $"leaderCidIsSelf={detail.LeaderContentId == Plugin.PlayerState.ContentId}; " +
                $"leaderNameAgrees={nameAgrees}; commentAgrees={commentAgrees}; " +
                $"nativeCommentChars={nativeComment.Length}; nativeSlots={detail.SlotsFilled}/{detail.TotalSlots}; nativeTimeRaw={detail.TimeLeft}";
        }

        var diagnostic = $"listingDetailVisible=true\nlistingDetailRevision={Interlocked.Read(ref detailRevision)}\n" +
            $"listingOwner={owner}\nuiLeaderReadable={leaderRead}; uiLeaderChars={leader.Length}\n" +
            $"uiDescriptionReadable={descriptionRead}; uiDescriptionSource={descriptionSource}; uiDescriptionChars={description.Length}\n" +
            $"uiDutyReadable={dutyRead}; uiTimeReadable={timeRead}\n{agentDiagnostic}\n{requestDiagnostic}";
        if (!descriptionRead)
            return previous with { Status = "상세 창의 모집 문구를 읽지 못함", IsLive = false,
                Note = "진단 정보와 게임 상세 창 화면을 확인해야 합니다.", Diagnostic = diagnostic };

        var note = requestNote;
        if (owner == ListingOwner.Unknown)
            note = (note + " 현재 열린 상세 창의 내용입니다. 내 모집글인지 여부는 아직 확인되지 않았습니다.").Trim();
        return new("상세 창에서 읽은 모집글", owner, leader, duty, description,
            remaining, DateTime.Now, true, note, diagnostic);
    }

    private unsafe void HandleRequest()
    {
        if (userRequested)
        {
            userRequested = false;
            openingMain = true;
            showIssued = false;
            requestTick = Environment.TickCount64;
            requestNote = "";
            requestDiagnostic = "request=own_by_content_id";
        }
        if (!openingMain) return;
        var agent = AgentLookingForGroup.Instance();
        if (!NativeReadGuard.Readable(agent, (nuint)sizeof(AgentLookingForGroup)))
        { FinishFailed("파티찾기 정보를 아직 사용할 수 없습니다.", "agent_unreadable"); return; }
        var main = Plugin.GameGui.GetAddonByName<AtkUnitBase>("LookingForGroup", 1);
        if (!NativeReadGuard.Readable(main, (nuint)sizeof(AtkUnitBase)) || !main->IsVisible || !main->IsReady)
        {
            if (!showIssued) { showIssued = true; agent->Show(); }
            if (Environment.TickCount64 - requestTick > 5000)
                FinishFailed("파티찾기 창을 열지 못했습니다. 게임에서 파티찾기를 연 뒤 다시 조회해 주세요.", "main_timeout");
            return;
        }
        openingMain = false;
        waitingRevision = Interlocked.Read(ref detailRevision);
        requestTick = Environment.TickCount64;
        // This requests a detail view. It never edits/reposts/joins a listing.
        var accepted = agent->OpenListingByContentId(Plugin.PlayerState.ContentId);
        requestDiagnostic += $"; requestAccepted={accepted}";
        if (!accepted)
            FinishFailed("상세 조회 요청이 받아들여지지 않았습니다. 게임에서 내 모집 확인 창을 열어 주세요.", "request_rejected");
    }

    private void FinishFailed(string message, string reason)
    {
        LastOwnRequestSucceeded = false;
        openingMain = false;
        waitingRevision = null;
        requestNote = message;
        requestDiagnostic += $"; failure={reason}";
    }

    internal void FailRequest() { userRequested = openingMain = LastOwnRequestSucceeded = false; waitingRevision = null; }

    internal void RecordFailure(string message)
    {
        FailRequest();
        requestNote = "모집글 조회 오류: " + message;
        requestDiagnostic = "request=failed; error=" + message;
    }

    private void Reset()
    {
        FailRequest();
        showIssued = false;
        requestNote = "";
        requestDiagnostic = "request=none";
        observedSelf = 0;
    }
}
