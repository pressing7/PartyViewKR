namespace PartyViewKR;

internal interface IListingBatchHost : IListingRefreshHost
{
    bool PrepareBatchStep(RefreshTarget identity, out RefreshTarget current, out string error);
    SlotEditProgress FinishBatchStep(RefreshTarget identity, out RefreshTarget current, out string error);
    bool SubmitBatch(RefreshTarget identity, out string error);
}

internal sealed class ListingChangeResult(ListingChange change)
{
    internal ListingChange Change { get; } = change;
    internal string Error { get; set; } = "";
    internal bool EditorVerified { get; set; }
    internal bool? Applied { get; set; }
    internal string Text => Applied switch {
        true => $"{Change.Title}: 반영 확인" + (Change.IsPrivatePassword ? " (비밀번호는 모집글에서 조회 불가)" : ""),
        false => $"{Change.Title}: 미반영 — {(Error.Length > 0 ? Error : "갱신된 모집글이 요청한 값과 다릅니다.")}",
        _ => $"{Change.Title}: 확인 필요 — {(Error.Length > 0 ? Error : "최종 모집글 조회 대기") }"
    };
}

// One editor, sequential item operations, one submit, one fresh result query.
// Item errors are collected. They never retry a click or abort the remaining items.
internal sealed class ListingBatchRefresh(IListingBatchHost host, ListingChange[] changes, long now, Action? editorStarting = null)
{
    internal RefreshStage Stage { get; private set; } = RefreshStage.Queued;
    internal bool Busy => Stage is not (RefreshStage.Completed or RefreshStage.Unconfirmed or RefreshStage.Stopped);
    internal string Status { get; private set; } = "일괄 수정 준비 중";
    internal IReadOnlyList<ListingChangeResult> Results => results;
    internal long CooldownUntil { get; private set; }
    internal string Diagnostic => $"batchStage={Stage}; batchPhase={phase}; batchItems={results.Length}; batchIndex={index}; " +
        $"batchEditCalls={editCalls}; batchSubmitCalls={submitCalls}; batchQueryCalls={queryCalls}; batchResult={result}\n" +
        string.Join("\n", results.Select((r, i) => $"batchItem={i + 1}; key={r.Change.Key}; editorVerified={r.EditorVerified}; applied={r.Applied?.ToString() ?? "unknown"}; error={r.Error}"));
    private readonly ListingChangeResult[] results = changes.Select(c => new ListingChangeResult(c)).ToArray();
    private RefreshTarget? original, current;
    private ListingChange? active;
    private int index, editCalls, submitCalls, queryCalls;
    private long stageTick = now, itemTick, revision, closedRevision;
    private ulong selfId;
    private string phase = "waiting", result = "pending";

    internal void Update(long tick)
    {
        if (!Busy) return;
        if (host.BlockReason is { } blocked) { Stop(blocked, "blocked"); return; }
        if (selfId != 0 && selfId != host.SelfId) { Stop("캐릭터가 바뀌었습니다.", "character_changed"); return; }
        switch (Stage)
        {
            case RefreshStage.Queued:
                if (changes.Length == 0 || changes.Any(c => !c.Valid)) { Stop("변경 목록을 다시 확인해 주세요.", "invalid_changes"); return; }
                if (host.QueryBusy || host.EditorVisible) { Stop("진행 중인 조회 또는 게임 수정창을 먼저 마쳐 주세요.", "already_busy"); return; }
                selfId = host.SelfId;
                Query(tick, false);
                break;
            case RefreshStage.ReadingOwn:
                if (tick - stageTick > 15000) { Stop("내 모집글 조회를 확인하지 못했습니다.", "initial_query_timeout"); return; }
                if (host.QueryBusy) return;
                if (!host.QuerySucceeded || host.DetailRevision <= revision) { Stop("새 모집글 조회에 실패했습니다.", "initial_query_failed"); return; }
                if (!host.TryGetOwn(out var own, out _)) return;
                if (own.SelfId != host.SelfId || changes.Any(c => !ListingPendingChanges.SameSource(c.Original, own)))
                { Stop("편집하는 동안 모집글 또는 참가자가 바뀌었습니다. 현재 모집글에서 다시 선택해 주세요.", "stale_changes"); return; }
                original = current = own;
                revision = host.EditorRevision; closedRevision = host.EditorClosedRevision;
                Set(RefreshStage.OpeningEditor, tick, "기존 모집 수정창 여는 중");
                editCalls++;
                // Preflight failures keep the user's queue. Consume it only
                // when entering the first native edit attempt, never on retry.
                editorStarting?.Invoke();
                if (!host.OpenEditor(own, out var openError)) Stop(openError, "open_failed");
                break;
            case RefreshStage.OpeningEditor:
                if (host.EditorClosedRevision != closedRevision) { Stop("모집 수정창이 닫혔습니다.", "editor_closed"); return; }
                if (tick - stageTick > 7000) { Stop("모집 내용 갱신 화면을 확인하지 못했습니다.", "editor_timeout"); return; }
                if (tick - stageTick < 500 || !host.EditorReady || host.EditorRevision <= revision || host.EditorActionMode != RecruitmentEditorMode.Refresh) return;
                Set(RefreshStage.EditingBatch, tick, "변경사항 적용 중"); phase = "next";
                break;
            case RefreshStage.EditingBatch:
                if (!host.EditorVisible || host.EditorClosedRevision != closedRevision)
                { Stop("모집 수정창이 닫혔습니다.", "editor_closed"); return; }
                UpdateItems(tick);
                break;
            case RefreshStage.WaitingResult:
                if (tick - stageTick > 10000) { Stop("갱신 화면이 닫히지 않았습니다. 게임 메시지를 확인해 주세요.", "submit_response_timeout"); return; }
                if (!host.EditorVisible && tick - stageTick >= 1500) Query(tick, true);
                break;
            case RefreshStage.Verifying:
                if (tick - stageTick > 15000) { Stop("갱신 후 모집글 조회를 확인하지 못했습니다.", "verify_timeout"); return; }
                if (host.QueryBusy) return;
                if (!host.QuerySucceeded || host.DetailRevision <= revision) { Stop("갱신 후 새 모집글 조회에 실패했습니다.", "verify_failed"); return; }
                if (!host.TryGetOwn(out var after, out _)) return;
                if (after.SelfId != original!.SelfId) { Stop("조회된 모집자가 달라 결과를 확인하지 못했습니다.", "owner_changed"); return; }
                foreach (var item in results)
                {
                    // The server never exposes the PIN. A failed PIN edit is
                    // not declared successful just because the private flag matches.
                    item.Applied = item.Change.MatchesListing(after) && (!item.Change.IsPrivatePassword || item.EditorVerified);
                    if (item.Change.IsPrivatePassword && !item.EditorVerified && item.Change.MatchesListing(after)) item.Applied = null;
                }
                var confirmed = after.ListingId != original.ListingId || after.RemainingSeconds is { } seconds &&
                    original.RemainingSeconds is { } before && seconds > before;
                var missed = results.Count(r => r.Applied != true);
                result = missed > 0 ? "partial_result" : "all_changes_seen";
                Set(confirmed ? RefreshStage.Completed : RefreshStage.Unconfirmed, tick,
                    (confirmed ? "갱신 완료" : "갱신 요청 후 내 모집글을 다시 조회했습니다") +
                    $" — 반영 {results.Length - missed}개, 미반영·확인 필요 {missed}개." +
                    (confirmed ? "" : " 남은 시간 초기화 여부는 게임에서 확인해 주세요."));
                break;
        }
    }

    private void UpdateItems(long tick)
    {
        if (phase == "finish")
        {
            try
            {
                var progress = host.FinishBatchStep(current!, out var read, out var error);
                if (progress == SlotEditProgress.Waiting && tick - itemTick <= 4000) return;
                if (progress == SlotEditProgress.Complete) current = read;
                else if (results[index].Error.Length == 0) results[index].Error = error.Length > 0 ? error : "수정창 정리 결과를 확인하지 못했습니다.";
            }
            catch (Exception ex) { results[index].Error = "수정창 확인 오류 (" + ex.GetType().Name + ")"; }
            index++; active = null; phase = "next";
            return;
        }
        if (phase == "next")
        {
            if (index >= results.Length) { Submit(tick); return; }
            var item = results[index];
            Status = $"변경사항 적용 중 ({index + 1}/{results.Length}) · {item.Change.Title}";
            itemTick = tick;
            // Mark the item consumed BEFORE entering native code.
            phase = "updating";
            try
            {
                if (!host.PrepareBatchStep(current!, out var read, out var error)) { FinishItem(tick, false, error); return; }
                current = read;
                active = item.Change.Rebase(current);
                if (!active.IsPrivatePassword && active.MatchesListing(current)) { FinishItem(tick, true, ""); return; }
                if (!active.Valid) { FinishItem(tick, false, "현재 모집 상태에서는 선택한 변경을 적용할 수 없습니다."); return; }
                if (!active.Begin(host, out error)) FinishItem(tick, false, error);
            }
            catch (Exception ex) { FinishItem(tick, false, "항목 처리 오류 (" + ex.GetType().Name + ")"); }
            return;
        }
        if (phase == "updating")
        {
            if (tick - itemTick > 12000) { FinishItem(tick, false, "반영 확인 시간이 초과되었습니다."); return; }
            try
            {
                var progress = active!.Update(host, out var expected, out var error);
                if (progress == SlotEditProgress.Waiting) return;
                if (progress == SlotEditProgress.Complete) current = expected;
                FinishItem(tick, progress == SlotEditProgress.Complete, error);
            }
            catch (Exception ex) { FinishItem(tick, false, "항목 처리 오류 (" + ex.GetType().Name + ")"); }
        }
    }

    private void FinishItem(long tick, bool verified, string error)
    {
        results[index].EditorVerified = verified;
        if (!verified) results[index].Error = error.Length > 0 ? error : "변경 결과를 확인하지 못했습니다.";
        phase = "finish"; itemTick = tick;
    }
    private void Submit(long tick)
    {
        Set(RefreshStage.WaitingResult, tick, "적용 가능한 변경으로 모집 갱신 중");
        submitCalls++; CooldownUntil = tick + 5000;
        // Submitting is never retried, even if dispatch throws after reaching the game.
        try { if (!host.SubmitBatch(current!, out var error)) Stop(error, "submit_failed"); }
        catch (Exception ex) { Stop("갱신 요청 오류 (" + ex.GetType().Name + ")", "submit_exception"); }
    }
    private void Query(long tick, bool verify)
    {
        revision = host.DetailRevision;
        Set(verify ? RefreshStage.Verifying : RefreshStage.ReadingOwn, tick, verify ? "갱신된 모집글의 실제 결과 확인 중" : "내 모집글 확인 중");
        queryCalls++; host.QueryOwn();
    }
    internal void Cancel() { if (Busy) Stop("사용자가 진행을 중단했습니다.", "cancelled"); }
    internal void Fail(string message) => Stop(message, "unexpected_error");
    private void Stop(string message, string code)
    {
        host.CancelQuery(); result = code;
        Stage = submitCalls > 0 ? RefreshStage.Unconfirmed : RefreshStage.Stopped;
        Status = message + (editCalls > 0 && host.EditorVisible ? " 게임 수정창에서 ‘모집 내용 갱신’을 눌러 모집을 재개할 수 있습니다." : "");
    }
    private void Set(RefreshStage stage, long tick, string status) { Stage = stage; stageTick = tick; Status = status; }
}
