namespace PartyViewKR;

internal enum RecruitmentWindow { Editor, Roles }
internal sealed record RecruitmentWindowTicket(nint Address, long ClosedRevision);
internal sealed record RecruitmentWindows(RecruitmentWindowTicket Editor, RecruitmentWindowTicket Roles);
internal interface IListingRecoveryHost
{
    ulong SelfId { get; }
    string? RecoveryBlockReason { get; }
    void StopForRecovery();
    bool ReadRecoveryWindows(out RecruitmentWindows windows);
    bool CloseRecoveryWindow(RecruitmentWindow kind, RecruitmentWindowTicket ticket, out string error);
}
internal sealed record WebRecovery(string Id, string State, string Message);

internal sealed partial class ListingWebBridge
{
    private readonly Dictionary<string, WebRecovery> recoveries = new(StringComparer.Ordinal);
    private WebRecovery? recovery;
    private RecruitmentWindows? recoveryWindows;
    private long recoveryTick;
    private bool recoveryRoleSent, recoveryEditorSent;
    private long recoveryRevision;
    internal long RecoveryRevision { get { lock (gate) return recoveryRevision; } }
    internal bool RecoveryActive { get { lock (gate) return recovery?.State is "queued" or "running"; } }

    internal WebReply QueueCloseEditor(string id, string session, long now)
    {
        lock (gate)
        {
            if (stopped) return Error(503, "연결이 종료됐습니다.");
            if (!Guid.TryParseExact(id, "D", out var parsed)) return Error(400, "요청 번호가 올바르지 않습니다.");
            id = parsed.ToString("D");
            if (session != view.Session) return Error(409, "접속 상태가 바뀌었습니다. /pvkr로 다시 열어 주세요.");
            if (recoveries.TryGetValue(id, out var prior)) return new(200, prior);
            if (!view.LoggedIn || now - publishedTick > 5000) return Error(409, "게임 응답을 기다리는 중입니다.");
            if (RecoveryActive) return Error(409, "수정창을 닫고 있습니다.");
            if (recoveries.Count >= 1024) return Error(409, "연결을 다시 켜 주세요.");
            recoveryRevision++;
            recoveryTick = now; recoveryWindows = null; recoveryRoleSent = recoveryEditorSent = false;
            SetRecovery(new(id, "queued", "진행을 중단하고 수정창을 닫는 중입니다."));
            // Supersede older queued work immediately. Native cancellation is
            // dispatched on Framework before the next query/refresh step.
            if (Active && view.Query is { } query) SetQuery(query with { State = "failed", Message = "수정창 닫기 요청으로 조회를 중단했습니다." });
            if (WriteActive && view.Operation is { } operation)
                SetOperation(operation with { State = "cancelled", Message = "수정창 닫기 요청으로 진행을 중단했습니다. 이미 제출된 변경은 취소되지 않습니다." });
            pendingWrite = null; source = null; sourceRevision = "";
            view = view with { CanQuery = false, CanRefresh = false, Editor = null,
                Reason = recovery!.Message, RefreshReason = recovery.Message };
            Serialize(); return new(202, recovery);
        }
    }

    internal WebReply ReadRecovery(string id)
    {
        lock (gate) return recoveries.TryGetValue(id, out var result) ? new(200, result) : Error(404, "닫기 요청을 찾지 못했습니다.");
    }

    // Also called before ListingReader.Read / Refresh.Update by Plugin. UI
    // recovery must not wait behind a submit in the normal bridge update.
    internal void BeforeReadRecovery(IListingRecoveryHost host, long now)
    {
        lock (gate) ProcessRecovery(host, now);
    }

    private void ProcessRecovery(IListingRecoveryHost? host, long now)
    {
        if (stopped || !RecoveryActive) return;
        if (host == null || self == 0 || host.SelfId != self)
        { FinishRecovery(false, "접속 상태가 바뀌어 닫기 요청을 중단했습니다."); return; }
        try
        {
            if (recovery!.State == "queued")
            {
                if (now - recoveryTick > 5000) { FinishRecovery(false, "닫기 요청 대기 시간이 지났습니다. 다시 눌러 주세요."); return; }
                SetRecovery(recovery with { State = "running" });
                // Consume the request before cancellation/native dispatch.
                host.StopForRecovery();
                if (host.RecoveryBlockReason is { } blocked) { FinishRecovery(false, blocked); return; }
                if (!host.ReadRecoveryWindows(out var initial)) { FinishRecovery(false, "수정창 상태를 읽지 못했습니다."); return; }
                recoveryWindows = initial; recoveryTick = now;
            }
            if (host.RecoveryBlockReason is { } reason) { FinishRecovery(false, reason); return; }
            if (!host.ReadRecoveryWindows(out var windows) || recoveryWindows == null)
            { FinishRecovery(false, "수정창 상태를 확인하지 못했습니다."); return; }
            if (windows.Editor.Address == 0 && windows.Roles.Address == 0)
            { FinishRecovery(true, "진행을 중단하고 수정창을 닫았습니다. /pvkr로 현재 모집 상태를 확인해 주세요."); return; }
            if (now - recoveryTick >= 5000) { FinishRecovery(false, "진행은 중단했지만 수정창이 닫혔는지 확인하지 못했습니다. 다시 눌러 주세요."); return; }
            // Never close a replacement window opened after this request.
            if (windows.Editor.Address != 0 && windows.Editor != recoveryWindows.Editor ||
                windows.Roles.Address != 0 && windows.Roles != recoveryWindows.Roles)
            { FinishRecovery(false, "수정창이 다른 창으로 바뀌어 닫기를 중단했습니다."); return; }
            if (windows.Roles.Address != 0)
            {
                if (recoveryRoleSent) return;
                recoveryRoleSent = true;
                if (!host.CloseRecoveryWindow(RecruitmentWindow.Roles, windows.Roles, out var error)) FinishRecovery(false, error);
                return;
            }
            if (!recoveryEditorSent)
            {
                recoveryEditorSent = true;
                if (!host.CloseRecoveryWindow(RecruitmentWindow.Editor, windows.Editor, out var error)) FinishRecovery(false, error);
            }
        }
        catch (Exception) { FinishRecovery(false, "진행을 중단하는 동안 오류가 발생했습니다. 창 상태를 다시 확인해 주세요."); }
    }

    private void FinishRecovery(bool complete, string message) => SetRecovery(recovery! with { State = complete ? "complete" : "failed", Message = message });
    private void SetRecovery(WebRecovery value) { recovery = value; recoveries[value.Id] = value; }
    private void ClearRecovery() { recovery = null; recoveryWindows = null; recoveries.Clear(); }
}
