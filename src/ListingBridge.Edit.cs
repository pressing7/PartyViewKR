namespace PartyViewKR;

internal sealed partial class ListingBridge
{
    // Retain mutation IDs for the entire session. Never evict an accepted write
    // and silently execute it again; only the last 32 full results are needed.
    private readonly HashSet<string> writeIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RemoteOperation> operations = new(StringComparer.Ordinal);
    private readonly Queue<string> operationOrder = new();
    private RefreshTarget? source;
    private string sourceRevision = "";
    private PendingWrite? pendingWrite;
    private sealed record PendingWrite(RefreshTarget Source, ListingChange[] Changes, long Tick);
    private bool WriteActive => view.Operation?.State is "queued" or "running";

    internal RemoteReply ReadOperation(string id)
    {
        lock (gate)
        {
            if (!Guid.TryParseExact(id, "D", out var parsed)) return Error(400, "요청 번호가 올바르지 않습니다.");
            id = parsed.ToString("D");
            if (operations.TryGetValue(id, out var operation)) return new(200, operation);
            return writeIds.Contains(id) ? Error(409, "이미 처리한 요청입니다. 현재 모집글을 조회해 주세요.") : Error(404, "해당 갱신 요청을 찾지 못했습니다.");
        }
    }

    internal RemoteReply QueueApply(string id, string session, ReadOnlyMemory<byte> body, long now, long? recoveryRevision = null)
    {
        lock (gate)
        {
            if (stopped) return Error(503, "디스코드 연결이 꺼져 있습니다.");
            if (!Guid.TryParseExact(id, "D", out var parsed)) return Error(400, "요청 번호가 올바르지 않습니다.");
            id = parsed.ToString("D");
            if (session != view.Session) return Error(409, "게임 접속 상태가 바뀌었습니다. 화면을 다시 확인해 주세요.");
            if (writeIds.Contains(id)) return ReadOperation(id);
            if (recoveryRevision.HasValue && recoveryRevision != this.recoveryRevision) return Error(409, "수정창 닫기 요청으로 이전 작업을 중단했습니다.");
            if (now - publishedTick > 5000) return Error(409, "게임 응답을 기다리는 중입니다.");
            if (!view.CanRefresh || Active || WriteActive || RecoveryActive || source == null)
                return Error(409, view.RefreshReason.Length > 0 ? view.RefreshReason : "다른 작업을 처리하는 중입니다.");
            if (writeIds.Count >= 1024) return Error(409, "디스코드 연결을 껐다 켠 뒤 /pvkr로 새 카드를 열어 주세요.");
            if (!ListingEditContract.Decode(body, source, sourceRevision, out var changes, out var error)) return Error(400, error);
            pendingWrite = new(source, changes, now);
            SetOperation(new(id, "queued", "Queued", changes.Length == 0 ? "현재 모집 조건으로 갱신을 요청했습니다." : $"{changes.Length}개 변경사항 적용 및 갱신을 요청했습니다.", false, false, []));
            // Consume this revision on acceptance, including failed preflight.
            // A timed-out POST can only be observed, never repeated implicitly.
            source = null; sourceRevision = "";
            view = view with { CanQuery = false, CanRefresh = false, Editor = null,
                Reason = "갱신 요청을 처리하는 중입니다.", RefreshReason = "갱신 요청을 처리하는 중입니다." };
            return new(202, view.Operation!);
        }
    }

    private void UpdateOperation(IListingEditHost? host, long now)
    {
        if (view.Operation is { State: "queued" } queued)
        {
            var request = pendingWrite; pendingWrite = null;
            var error = host == null || request == null ? "갱신 요청을 처리할 수 없습니다." :
                now - request.Tick > 5000 ? "갱신 요청 대기 시간이 지났습니다. 다시 확인해 주세요." :
                request.Source.SelfId != self ? "게임 접속 상태가 바뀌었습니다." : host.RefreshBlockReason;
            if (error != null) { SetOperation(queued with { State = "failed", Message = error }); return; }
            SetOperation(queued with { State = "running", Message = "내 모집글을 다시 확인한 뒤 변경사항을 적용합니다." });
            try
            {
                if (!host!.StartRefresh(request!.Source, request.Changes, out var startError))
                    SetOperation(queued with { State = "failed", Message = startError });
            }
            catch (Exception)
            {
                // Start may have succeeded before throwing. Observe its serial
                // on subsequent updates instead of submitting a second command.
                SetOperation(view.Operation! with { Message = "요청 결과를 확인하는 중입니다. 다시 전송하지 마세요." });
            }
        }
        else if (view.Operation is { State: "running" } running)
        {
            var progress = host?.ReadRefresh();
            if (progress == null) { SetOperation(running with { State = "unknown", Message = "갱신 결과를 확인하지 못했습니다. 내 모집글을 다시 조회해 주세요." }); return; }
            var state = progress.Busy ? "running" : progress.Stage switch {
                nameof(RefreshStage.Completed) => progress.Results.Any(r => r.Applied != true) ? "partial" : "complete",
                nameof(RefreshStage.Unconfirmed) => "unconfirmed", _ => "failed"
            };
            SetOperation(new(running.Id, state, progress.Stage, progress.Message, progress.EditorStarted, progress.Submitted, progress.Results));
        }
    }

    private void PublishEditor(IListingEditHost? host, ListingSnapshot listing)
    {
        if (host == null || self == 0 || listing.Owner != ListingOwner.Self) source = null;
        if (!WriteActive && !Active && !RecoveryActive && host != null && self != 0 && listing.Owner == ListingOwner.Self)
        {
            var captured = host.CaptureEditTarget();
            if (captured != null && captured.SelfId == self && captured.Slots.MappingValid && captured.Slots.CountsAgree)
            {
                if (source == null || !ListingEditContract.SameSource(source, captured)) sourceRevision = Guid.NewGuid().ToString("N");
                source = captured;
            }
            // A closed details window can retain its validated snapshot. Every
            // actual refresh queries again and compares all fields and members.
            else if (listing.IsLive && host.RefreshBlockReason == null) source = null;
        }
        var reason = RecoveryActive ? "수정창을 닫는 중입니다." : WriteActive ? "갱신 요청을 처리하는 중입니다." : Active ? "조회 요청을 처리하는 중입니다." :
            host?.RefreshBlockReason ?? (listing.Owner == ListingOwner.PartyLeader ? "참가한 파티는 조회와 알림을 사용할 수 있습니다. 수정·갱신은 모집자만 가능합니다." :
                source == null ? "내 모집글을 조회해 주세요." : "");
        if (self == 0) reason = "게임에 로그인해 주세요.";
        if (writeIds.Count >= 1024) reason = "디스코드 연결을 껐다 켠 뒤 /pvkr로 새 카드를 열어 주세요.";
        view = view with { Editor = source != null ? ListingEditContract.Model(source, sourceRevision) : null,
            CanRefresh = source != null && reason.Length == 0, RefreshReason = reason };
    }
    private void SetOperation(RemoteOperation operation)
    {
        if (writeIds.Add(operation.Id)) operationOrder.Enqueue(operation.Id);
        operations[operation.Id] = operation;
        while (operationOrder.Count > 32) operations.Remove(operationOrder.Dequeue());
        view = view with { Operation = operation };
    }
    private void ClearEdits()
    {
        source = null; sourceRevision = ""; pendingWrite = null;
        writeIds.Clear(); operations.Clear(); operationOrder.Clear();
    }
}
