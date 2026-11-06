using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace PartyViewKR;

internal sealed class ListingReader
{
    private readonly RuntimeRows rows = new();
    private bool userRequested;
    private bool openingMain;
    private bool showIssued;
    private long requestTick;
    private long? waitingRevision;
    private long detailRevision;
    private long requestRevision;
    private string requestNote = "";
    private string requestDiagnostic = "request=none";
    private string responseMismatch = "";
    private ulong observedSelf;
    private bool ownOnly = true;
    private PartyRoster? requestedParty;
    private ulong[] candidates = [];
    private int candidateIndex;
    private ulong requestedLeader;
    private string confirmedPartyKey = "";
    private ulong confirmedPartyLeader;
    private PartyRoster? viewedParty;
    private long? partyChangedAtRevision;

    internal bool Busy => userRequested || openingMain || waitingRevision.HasValue;
    internal bool LastRequestSucceeded { get; private set; }
    internal bool LastOwnRequestSucceeded => ownOnly && LastRequestSucceeded;
    internal long QueryTimeoutMilliseconds => Math.Clamp(5000L + candidates.Length * 8000L, 15000, 55000);
    internal long DetailRevision => Interlocked.Read(ref detailRevision);
    internal long RequestRevision => Interlocked.Read(ref requestRevision);
    internal void RequestOwn()
    {
        if (Busy) return;
        ownOnly = true; requestedParty = null; candidates = []; candidateIndex = 0; requestedLeader = 0;
        responseMismatch = "";
        LastRequestSucceeded = false; userRequested = true; Interlocked.Increment(ref requestRevision);
    }
    internal bool RequestParty(PartyRoster roster)
    {
        if (Busy || roster.QueryCandidates.Length == 0) return false;
        RequestOwn(); ownOnly = false; requestedParty = roster;
        candidates = roster.QueryCandidates.OrderByDescending(id => roster.PartyKey == confirmedPartyKey && id == confirmedPartyLeader).ToArray();
        return true;
    }
    internal void DetailUpdated() => Interlocked.Increment(ref detailRevision);

    internal unsafe ListingSnapshot Read(PartySnapshot party, ListingSnapshot previous, PartyRoster? current)
    {
        if (!Plugin.ClientState.IsLoggedIn || !Plugin.PlayerState.IsLoaded)
        {
            Reset();
            return ListingSnapshot.Empty;
        }
        if (observedSelf != Plugin.PlayerState.ContentId)
        {
            // A first read may already have a queued request. A character switch
            // must discard the old character's target rather than replay it.
            if (observedSelf != 0) Reset();
            observedSelf = Plugin.PlayerState.ContentId;
            previous = ListingSnapshot.Empty;
        }
        if (!NativeCompatibility.IsCompatible)
        {
            userRequested = openingMain = false;
            waitingRevision = null;
            LastRequestSucceeded = false;
            return ListingSnapshot.Empty with { Note = "구조 DLL의 호환 검사를 통과하지 못해 모집글 조회를 중지했습니다.", Diagnostic = "listingRead=skipped; reason=structure_mismatch" };
        }
        if (Plugin.Condition[ConditionFlag.BetweenAreas] || Plugin.Condition[ConditionFlag.BetweenAreas51])
            return previous with { Status = "이동 중 — 마지막 모집글", IsLive = false };

        if (current != null)
        {
            if (viewedParty != null && !current.Continues(viewedParty))
            { partyChangedAtRevision = DetailRevision; previous = ListingSnapshot.Empty; }
            viewedParty = current;
        }

        if (!ownOnly && Busy && (current == null || requestedParty == null || !current.Continues(requestedParty)))
        {
            userRequested = false;
            FinishFailed("소속 파티가 바뀌었거나 파티 정보를 확인하지 못했습니다. 다시 조회해 주세요.", "party_context_changed");
        }
        HandleRequest();
        if (waitingRevision.HasValue && Environment.TickCount64 - requestTick >= 7000)
        {
            if (!NextCandidate()) FinishFailed(responseMismatch.Length == 0
                ? "현재 파티의 모집글 응답을 확인하지 못했습니다. 모집이 종료되었을 수 있습니다."
                : "모집글 응답은 받았지만 화면과 내부 정보를 일치하는 모집글로 확인하지 못했습니다. 진단 정보를 확인해 주세요.",
                responseMismatch.Length == 0 ? "response_timeout" : responseMismatch);
        }
        if (waitingRevision is { } expected && Interlocked.Read(ref detailRevision) <= expected)
            return previous with { Status = "모집글 응답 대기 중", IsLive = false, Note = "게임의 상세 조회 응답을 기다리고 있습니다.", Diagnostic = requestDiagnostic + "; detailRefreshSeen=false" };
        if (openingMain)
            return previous with { Status = "파티찾기 창 여는 중", IsLive = false, Note = "", Diagnostic = requestDiagnostic };
        if (partyChangedAtRevision is { } boundary)
        {
            if (DetailRevision <= boundary)
                return ListingSnapshot.Empty with { Status = "소속 파티가 바뀌었습니다. 현재 파티 모집글을 조회해 주세요.", Diagnostic = requestDiagnostic + "; partyDetailStale=true" };
            partyChangedAtRevision = null;
        }

        var addon = Plugin.GameGui.GetAddonByName<AddonLookingForGroupDetail>("LookingForGroupDetail", 1);
        if (!NativeReadGuard.Readable(addon, (nuint)sizeof(AddonLookingForGroupDetail)) || !addon->IsVisible || !addon->IsReady)
            return previous with {
                Status = previous.ReadAt == null ? "모집글 상세 창 대기" : "마지막으로 읽은 모집글 — 상세 창 닫힘",
                IsLive = false,
                Note = requestNote.Length > 0 ? requestNote : "현재 파티 모집글 조회를 누르거나 게임에서 모집글 상세 창을 열어주세요.",
                Diagnostic = $"listingDetailVisible=false\n{requestDiagnostic}"
            };

        var leaderRead = NativeReadGuard.TextNode(addon->PartyLeaderTextNode, out var leader);
        var descriptionNodeRead = NativeReadGuard.TextNode(addon->DescriptionTextNode, out var descriptionNode);
        var descriptionBufferRead = NativeReadGuard.Text(&addon->DescriptionString, out var descriptionBuffer);
        var description = ListingDescription.Resolve(descriptionNodeRead, descriptionNode,
            descriptionBufferRead, descriptionBuffer, false, "");
        var dutyRead = NativeReadGuard.TextNode(addon->DutyNameTextNode, out var duty);
        var timeRead = NativeReadGuard.TextNode(addon->TimeRemainingTextNode, out var remaining);

        var owner = ListingOwner.Unknown;
        ulong leaderContentId = 0;
        byte? objective = null;
        byte? completion = null;
        int? itemLevel = null;
        byte? dutyFlags = null, lootRule = null;
        bool? beginnerFriendly = null;
        bool? uniqueJobs = null;
        bool? privateListing = null;
        var slots = ListingSlots.Unavailable("agent_unreadable");
        rows.BeginRead();
        var agentDiagnostic = "listingAgent=unavailable";
        var agent = AgentLookingForGroup.Instance();
        if (NativeReadGuard.Readable(agent, (nuint)sizeof(AgentLookingForGroup)))
        {
            ref var detail = ref agent->LastViewedListing;
            var nativeNameRead = NativeReadGuard.FixedText(detail.Leader, out var nativeName);
            var nativeCommentRead = NativeReadGuard.FixedText(detail.Comment, out var nativeComment);
            description = ListingDescription.Resolve(descriptionNodeRead, descriptionNode,
                descriptionBufferRead, descriptionBuffer, nativeCommentRead, nativeComment);
            // OwnListingId and StoredRecruitmentInfo can be stale or draft data.
            // Require agreement with the detail UI, including the verified KR empty
            // placeholder; unreadable or unrelated text is not an empty comment.
            var nameAgrees = leaderRead && nativeNameRead && ListingIdentity.SameText(leader, nativeName);
            var commentAgrees = description.NativeAgrees;
            var shapePlausible = detail.ListingId != 0 && detail.TotalSlots is >= 1 and <= 48 && detail.SlotsFilled <= detail.TotalSlots;
            if (shapePlausible && nameAgrees && commentAgrees && ListingObjectives.Index((byte)detail.Objective) >= 0)
                objective = (byte)detail.Objective;
            if (shapePlausible && nameAgrees && commentAgrees)
            {
                if (ListingCompletions.Supported((byte)detail.CompletionStatus) || (byte)detail.CompletionStatus == 8) completion = (byte)detail.CompletionStatus;
                itemLevel = ListingItemLevels.Decode(detail.AvgItemLv);
                dutyFlags = (byte)detail.DutyFinderSettingFlags;
                if ((byte)detail.LootRule <= 2) lootRule = (byte)detail.LootRule;
                beginnerFriendly = ListingBeginners.Decode(detail.BeginnerFriendly);
                uniqueJobs = ListingUniqueJobs.FromFlags((byte)detail.JoinConditionFlags);
                privateListing = ListingPrivate.FromFlags((byte)detail.JoinConditionFlags);
            }
            slots = ListingSlots.Read(shapePlausible && nameAgrees && commentAgrees,
                detail.TotalSlots, detail.SlotsFilled, detail.NumberOfParties, detail.IsAlliance,
                detail.Jobs, detail.MemberContentIds, detail.SlotFlags, rows.Job, rows.RecruitmentJobs(), (byte)detail.JoinConditionFlags);
            owner = ListingIdentity.Resolve(detail.LeaderContentId, nativeName,
                shapePlausible && nameAgrees && commentAgrees, Plugin.PlayerState.ContentId,
                Plugin.PlayerState.CharacterName, party.Members.Select(m => (m.ContentId, m.Name, m.IsLeader)).ToArray());
            leaderContentId = detail.LeaderContentId;
            if (owner != ListingOwner.Self)
                owner = shapePlausible && nameAgrees && commentAgrees && current != null &&
                    current.Contains(leaderContentId) && current.QueryCandidates.Contains(leaderContentId) &&
                    slots.Slots.Any(s => s.HasMember && s.MemberContentId == current.SelfId)
                    ? ListingOwner.PartyLeader : owner == ListingOwner.PartyLeader ? ListingOwner.Other : owner;
            agentDiagnostic = $"listingIdPresent={detail.ListingId != 0}; ownIdRaw={agent->OwnListingId}; " +
                $"leaderCidIsSelf={detail.LeaderContentId == Plugin.PlayerState.ContentId}; " +
                $"leaderNameAgrees={nameAgrees}; commentAgrees={commentAgrees}; " +
                $"nativeCommentReadable={nativeCommentRead}; nativeCommentChars={nativeComment.Length}; " +
                ListingDescription.CompareSources(descriptionNodeRead, descriptionNode, descriptionBufferRead, descriptionBuffer, nativeCommentRead, nativeComment) + "; " +
                $"nativeSlots={detail.SlotsFilled}/{detail.TotalSlots}; nativeTimeRaw={detail.TimeLeft}; listingCategoryRaw={(uint)detail.Category}; " +
                $"listingCompletionRaw={(byte)detail.CompletionStatus}; listingCompletionReadable={completion.HasValue}; " +
                $"listingDutyFlags={(byte)detail.DutyFinderSettingFlags}; listingDutyFlagsReadable={dutyFlags.HasValue}; listingLootRuleRaw={(byte)detail.LootRule}; listingLootRuleReadable={lootRule.HasValue}; " +
                $"listingItemLevelRaw={detail.AvgItemLv}; listingItemLevelReadable={itemLevel.HasValue}; " +
                $"listingObjectiveRaw={(byte)detail.Objective}; listingObjectiveReadable={objective.HasValue}; " +
                $"listingBeginnerRaw={detail.BeginnerFriendly}; listingBeginnerReadable={beginnerFriendly.HasValue}; " +
                $"listingJoinConditionFlags={(byte)detail.JoinConditionFlags}; listingUniqueJobs={uniqueJobs?.ToString() ?? "unknown"}; listingPrivate={privateListing?.ToString() ?? "unknown"}";
        }

        var diagnostic = $"listingDetailVisible=true\nlistingDetailRevision={Interlocked.Read(ref detailRevision)}\n" +
            $"listingOwner={owner}\nuiLeaderReadable={leaderRead}; uiLeaderChars={leader.Length}\n" +
            $"uiDescriptionReadable={description.Readable}; uiDescriptionSource={description.Source}; uiDescriptionChars={description.Text.Length}\n" +
            $"uiDescriptionNodeReadable={descriptionNodeRead}; uiDescriptionNodeChars={descriptionNode.Length}; " +
            $"uiDescriptionBufferReadable={descriptionBufferRead}; uiDescriptionBufferChars={descriptionBuffer.Length}; " +
            $"emptyDescriptionPlaceholder={description.EmptyPlaceholder}\n" +
            $"uiDutyReadable={dutyRead}; uiTimeReadable={timeRead}\n{agentDiagnostic}\n{slots.Diagnostic}\n" +
            $"listingJobRowLookupError={rows.Error}\n{requestDiagnostic}";
        if (!description.Readable)
        {
            if (waitingRevision.HasValue) responseMismatch = "response_description_unreadable";
            return previous with { Status = "상세 창의 모집 문구를 읽지 못함", IsLive = false,
                Note = "진단 정보와 게임 상세 창 화면을 확인해야 합니다.", Diagnostic = diagnostic };
        }

        var note = requestNote;
        if (owner == ListingOwner.Unknown)
            note = (note + " 현재 열린 상세 창의 내용입니다. 내 모집글인지 여부는 아직 확인되지 않았습니다.").Trim();
        var result = new ListingSnapshot("상세 창에서 읽은 모집글", owner, leader, duty, description.Text,
            remaining, DateTime.Now, true, note, diagnostic) { ViewerContentId = Plugin.PlayerState.ContentId,
            LeaderContentId = leaderContentId, PartyKey = current?.PartyKey ?? "", Slots = slots, Objective = objective, Completion = completion, ItemLevel = itemLevel, DutyFlags = dutyFlags, LootRule = lootRule, BeginnerFriendly = beginnerFriendly, UniqueJobs = uniqueJobs, Private = privateListing };
        if (waitingRevision.HasValue)
        {
            if (!ListingPartyScope.CanConfirm(requestedLeader, ownOnly, result, current))
            {
                responseMismatch = requestedLeader != leaderContentId ? "response_leader_mismatch" :
                    !description.NativeAgrees ? "response_description_mismatch" : "response_party_unverified";
                return previous with { IsLive = false, Status = "요청한 파티의 모집글을 확인하는 중", Diagnostic = diagnostic + "; responseTargetAgrees=false" };
            }
            waitingRevision = null; LastRequestSucceeded = true; requestNote = "";
            if (!ownOnly) { confirmedPartyKey = current!.PartyKey; confirmedPartyLeader = requestedLeader; }
            requestDiagnostic += "; detailRefreshSeen=true; responseTargetAgrees=true";
            result = result with { Note = "", Diagnostic = diagnostic + "; detailRefreshSeen=true; responseTargetAgrees=true" };
        }
        return result;
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
            if (ownOnly) candidates = [Plugin.PlayerState.ContentId];
            requestDiagnostic = ownOnly ? "request=own_by_content_id" : "request=party_by_content_id";
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
        requestedLeader = candidates[candidateIndex];
        var accepted = agent->OpenListingByContentId(requestedLeader);
        requestDiagnostic += $"; requestCandidate={candidateIndex + 1}/{candidates.Length}; requestTargetIsSelf={requestedLeader == Plugin.PlayerState.ContentId}; requestAccepted={accepted}";
        if (!accepted)
            if (!NextCandidate()) FinishFailed("현재 파티의 모집글 조회 요청이 받아들여지지 않았습니다. 모집이 종료되었을 수 있습니다.", "request_rejected");
    }

    private bool NextCandidate()
    {
        if (ownOnly || candidateIndex + 1 >= candidates.Length) return false;
        responseMismatch = "";
        candidateIndex++; waitingRevision = null; openingMain = true; requestTick = Environment.TickCount64;
        return true;
    }

    private void FinishFailed(string message, string reason)
    {
        LastRequestSucceeded = false;
        openingMain = false;
        waitingRevision = null;
        requestNote = message;
        requestDiagnostic += $"; failure={reason}";
    }

    internal void FailRequest() { userRequested = openingMain = LastRequestSucceeded = false; waitingRevision = null; }

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
        responseMismatch = "";
        observedSelf = 0;
        viewedParty = null; partyChangedAtRevision = null;
        confirmedPartyKey = ""; confirmedPartyLeader = 0;
    }
}
