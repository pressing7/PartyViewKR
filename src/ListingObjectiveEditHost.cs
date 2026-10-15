namespace PartyViewKR;

internal sealed unsafe partial class ListingRefreshHost
{
    private const string ObjectiveTraceRequired = "목적 자동 수정은 선택 이벤트 확인 중입니다. 목적 선택 진단에서 수동 조작을 기록해 주세요.";
    private string ObjectiveEditDiagnostic => "objectiveEditCheck=awaiting_manual_ui_trace; objectiveCallbackCalls=0; objectiveGeneratedEvents=0";

    // Unverified callback mappings are disabled until the manual trace shows
    // the actual UI route. No fallback callback or draft-memory write.
    public bool BeginObjectiveEdit(RefreshTarget target, ListingObjectiveEdit edit, out string error)
    { error = ObjectiveTraceRequired; return false; }

    public SlotEditProgress UpdateObjectiveEdit(out RefreshTarget expected, out string error)
    { expected = null!; error = ObjectiveTraceRequired; return SlotEditProgress.Failed; }
}
