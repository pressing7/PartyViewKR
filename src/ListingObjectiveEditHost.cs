using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace PartyViewKR;

internal sealed unsafe partial class ListingRefreshHost
{
    private ObjectiveSession? objectiveSession;
    private string objectiveCheck = "not_started";
    private int objectiveUiIndex = -1;
    private byte? objectiveStored;
    private string ObjectiveEditDiagnostic => $"objectiveEditCheck={objectiveCheck}; objectiveEditPhase={objectiveSession?.Phase ?? "none"}; " +
        $"objectiveCallbackCalls={objectiveSession?.CallbackCalls ?? 0}; objectiveCallbackReturned={objectiveSession?.CallbackReturned?.ToString() ?? "unknown"}; " +
        $"objectiveRequestedIndex={objectiveSession?.Edit.SelectedIndex.ToString() ?? "unknown"}; " +
        $"objectiveRequestedValue={objectiveSession?.Edit.Value?.ToString() ?? "unknown"}; " +
        $"objectiveUiIndex={objectiveUiIndex}; objectiveStoredValue={objectiveStored?.ToString() ?? "unknown"}";

    private sealed class ObjectiveSession(RefreshTarget original, ListingObjectiveEdit edit, string invariant)
    {
        internal readonly RefreshTarget Original = original;
        internal readonly ListingObjectiveEdit Edit = edit;
        internal readonly RefreshTarget Expected = edit.Expected(original);
        internal readonly string Invariant = invariant;
        internal readonly long Started = Environment.TickCount64;
        internal string Phase = "validating";
        internal int CallbackCalls;
        internal bool? CallbackReturned;
        internal bool Completed;
    }

    public bool BeginObjectiveEdit(RefreshTarget target, ListingObjectiveEdit edit, out string error)
    {
        objectiveSession = null; objectiveCheck = "validating"; objectiveUiIndex = -1; objectiveStored = null;
        error = "기존 모집 목적을 확인하지 못했습니다.";
        if (!edit.MatchesOriginal(target) || Visible("LookingForGroupSelectRole")) return false;
        if (!ValidateEditor(target, out var addon, out var agent, out error)) return false;
        if (!ReadObjectiveControl(addon, out error)) return false;
        objectiveStored = (byte)agent->StoredRecruitmentInfo.Objective;
        if (objectiveStored != ListingObjectives.Read(target.Criteria) || ListingObjectives.Value(objectiveUiIndex) != objectiveStored)
        { objectiveCheck = "original_objective_mismatch"; error = "게임 수정창의 목적이 조회한 모집글과 다릅니다."; return false; }
        if (!ListingSlotRoute.TryCreate(target.Slots, 1, out var route) ||
            !route.MatchesEditorShape(agent->StoredRecruitmentInfo.NumberOfGroups, agent->StoredRecruitmentInfo.NumberOfSlotsInMainParty) ||
            !agent->StoredRecruitmentInfo.MemberContentIds[..target.TotalSlots].SequenceEqual(target.Slots.Slots.Select(s => s.MemberContentId).ToArray()))
        { objectiveCheck = "members_or_shape_changed"; error = "모집 수정창의 파티 구성이 달라 중단했습니다."; return false; }

        objectiveSession = new(target, edit, ObjectiveInvariant(agent));
        objectiveSession.Phase = "verifying";
        objectiveSession.CallbackCalls++;
        objectiveCheck = "selecting_objective_once";
        // PFRemote 1.0.0.12 CreatePf b__0 / FireLfgCommand(uint, uint):
        // command 20, dropdown index, selector 0. Index 0 must be sent too when
        // clearing an existing purpose, unlike the original new-listing path.
        var values = stackalloc AtkValue[3];
        new Span<AtkValue>(values, 3).Clear();
        values[0].Type = AtkValueType.Int; values[0].Int = 20;
        values[1].Type = AtkValueType.UInt; values[1].UInt = (uint)edit.SelectedIndex;
        values[2].Type = AtkValueType.UInt; values[2].UInt = 0;
        objectiveSession.CallbackReturned = ((AtkUnitBase*)addon)->FireCallback(3, values, true);
        error = "";
        return true;
    }

    public SlotEditProgress UpdateObjectiveEdit(out RefreshTarget expected, out string error)
    {
        expected = null!; error = "목적 반영 결과를 확인하지 못했습니다.";
        var session = objectiveSession;
        if (session == null) return SlotEditProgress.Failed;
        ReadEditorAction();
        if (SelfId != session.Original.SelfId || BlockReason != null ||
            !ReferenceEquals(editingTarget, session.Original) || EditorClosedRevision != editingFromClosedRevision ||
            !buttonTextReadable || buttonMode != RecruitmentEditorMode.Refresh || Visible("LookingForGroupSelectRole"))
        { objectiveCheck = "editor_binding_changed"; error = "모집 수정창의 상태가 바뀌어 중단했습니다."; return SlotEditProgress.Failed; }
        var addon = Plugin.GameGui.GetAddonByName<AddonLookingForGroupCondition>("LookingForGroupCondition", 1);
        var agent = AgentLookingForGroup.Instance();
        if (!NativeReadGuard.Readable(addon, (nuint)sizeof(AddonLookingForGroupCondition)) || !addon->IsVisible || !addon->IsReady ||
            !NativeReadGuard.Readable(agent, (nuint)sizeof(AgentLookingForGroup))) return SlotEditProgress.Failed;
        if (!ValidateObjectiveGuard(addon, agent, session, false, out error)) return SlotEditProgress.Failed;
        if (Environment.TickCount64 - session.Started < 100) return SlotEditProgress.Waiting;
        if (objectiveStored != session.Edit.Value || !buttonEnabled)
        {
            objectiveCheck = "objective_result_waiting";
            error = "게임 수정창의 목적이 아직 요청한 값과 일치하지 않습니다.";
            return Environment.TickCount64 - session.Started <= 3000 ? SlotEditProgress.Waiting : SlotEditProgress.Failed;
        }
        editingTarget = session.Expected;
        if (!ValidateEditor(session.Expected, out _, out _, out error)) return SlotEditProgress.Failed;
        session.Completed = true; session.Phase = "complete"; objectiveCheck = "draft_objective_verified";
        expected = session.Expected;
        return SlotEditProgress.Complete;
    }

    private bool ValidateObjectiveGuard(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent,
        ObjectiveSession session, bool final, out string error)
    {
        error = "목적 수정 중 참가자 또는 다른 모집 조건이 바뀌어 중단했습니다.";
        if (ObjectiveInvariant(agent) != session.Invariant)
        { objectiveCheck = "other_criteria_changed"; return false; }
        if (!ReadObjectiveControl(addon, out error)) return false;
        objectiveStored = (byte)agent->StoredRecruitmentInfo.Objective;
        var original = ListingObjectives.Read(session.Original.Criteria);
        if (objectiveStored != session.Edit.Value && (final || objectiveStored != original))
        { objectiveCheck = "stored_objective_mismatch"; error = "목적이 요청과 다른 값으로 바뀌어 중단했습니다."; return false; }
        // The command updates the agent's draft. A dropdown preview can still
        // show its original selection; record it without issuing a second UI
        // event. Reject unrelated choices and require the stored value before
        // submission, then independently verify the newly queried listing.
        var visibleValue = ListingObjectives.Value(objectiveUiIndex);
        if (visibleValue != original && visibleValue != session.Edit.Value)
        { objectiveCheck = "visible_objective_changed"; error = "게임 목적 선택이 다른 항목으로 바뀌어 중단했습니다."; return false; }
        error = "";
        return true;
    }

    private bool ReadObjectiveControl(AddonLookingForGroupCondition* addon, out string error)
    {
        objectiveUiIndex = -1;
        error = "현재 게임 수정창에서는 목적을 선택할 수 없습니다.";
        var dropdown = addon->ObjectiveDropDown;
        if (!NativeReadGuard.Readable(dropdown, (nuint)sizeof(AtkComponentDropDownList)) ||
            !NativeReadGuard.Readable(dropdown->OwnerNode, (nuint)sizeof(AtkComponentNode)) || !dropdown->OwnerNode->IsVisible() ||
            !NativeReadGuard.Readable(dropdown->List, (nuint)sizeof(AtkComponentList)))
        { objectiveCheck = "objective_dropdown_unavailable"; return false; }
        var checkbox = dropdown->Checkbox;
        if (!NativeReadGuard.Readable(checkbox, (nuint)sizeof(AtkComponentCheckBox)) ||
            !NativeReadGuard.Readable(checkbox->OwnerNode, (nuint)sizeof(AtkComponentNode)) || !checkbox->IsEnabled)
        { objectiveCheck = "objective_dropdown_disabled"; return false; }
        objectiveUiIndex = dropdown->GetSelectedItemIndex();
        if (!ListingObjectives.Value(objectiveUiIndex).HasValue)
        { objectiveCheck = "objective_selection_unknown"; return false; }
        error = "";
        return true;
    }

    private static string ObjectiveInvariant(AgentLookingForGroup* agent) => DraftInvariant(agent, includeObjective: false) + "/" +
        Convert.ToHexString(MemoryMarshal.AsBytes(agent->StoredRecruitmentInfo.SlotFlags));
}
