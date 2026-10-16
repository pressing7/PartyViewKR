using System.Runtime.InteropServices;
using System.Text;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace PartyViewKR;

internal sealed unsafe partial class ListingRefreshHost
{
    private DescriptionSession? descriptionSession;
    private string descriptionCheck = "not_started";
    private int descriptionVisibleChars, descriptionStoredChars;
    private uint descriptionMaxByte, descriptionMaxChar;
    private string DescriptionEditDiagnostic => $"descriptionEditCheck={descriptionCheck}; " +
        $"descriptionEditPhase={descriptionSession?.Phase ?? "none"}; descriptionInputCalls={descriptionSession?.InputCalls ?? 0}; " +
        $"descriptionCallbackCalls={descriptionSession?.CallbackCalls ?? 0}; " +
        $"descriptionSubmitCallbackCalls={descriptionSession?.SubmitCallbackCalls ?? 0}; " +
        $"descriptionSubmitCallbackReturned={descriptionSession?.SubmitCallbackReturned?.ToString() ?? "unknown"}; " +
        $"descriptionRequestedBytes={descriptionSession?.Edit.ByteCount ?? 0}; descriptionVisibleChars={descriptionVisibleChars}; " +
        $"descriptionStoredChars={descriptionStoredChars}; descriptionMaxByte={descriptionMaxByte}; descriptionMaxChar={descriptionMaxChar}";

    private sealed class DescriptionSession(RefreshTarget original, ListingDescriptionEdit edit, string invariant)
    {
        internal readonly RefreshTarget Original = original;
        internal readonly ListingDescriptionEdit Edit = edit;
        internal readonly RefreshTarget Expected = edit.Expected(original);
        internal readonly string Invariant = invariant;
        internal readonly long Started = Environment.TickCount64;
        internal string Phase = "input";
        internal int InputCalls, CallbackCalls, SubmitCallbackCalls;
        internal bool? SubmitCallbackReturned;
        internal bool Completed;
    }

    public bool BeginDescriptionEdit(RefreshTarget target, ListingDescriptionEdit edit, out string error)
    {
        descriptionSession = null; descriptionCheck = "validating";
        descriptionVisibleChars = descriptionStoredChars = 0;
        descriptionMaxByte = descriptionMaxChar = 0;
        error = "기존 모집 소개말을 확인하지 못했습니다.";
        if (!edit.MatchesOriginal(target) || Visible("LookingForGroupSelectRole")) return false;
        if (!ValidateEditor(target, out var addon, out var agent, out error)) return false;
        var input = addon->CommentTextInput;
        if (!NativeReadGuard.Readable(input, (nuint)sizeof(AtkComponentTextInput)) || !input->Enabled)
        { descriptionCheck = "input_unavailable"; error = "게임의 소개말 입력칸이 준비되지 않았습니다."; return false; }
        if (!ListingSlotRoute.TryCreate(target.Slots, 1, out var route) ||
            !route.MatchesEditorShape(agent->StoredRecruitmentInfo.NumberOfGroups, agent->StoredRecruitmentInfo.NumberOfSlotsInMainParty) ||
            !target.Slots.MatchesMembers(agent->StoredRecruitmentInfo.MemberContentIds))
        { descriptionCheck = "members_or_shape_changed"; error = "모집 수정창의 파티 구성이 달라 중단했습니다."; return false; }
        descriptionMaxByte = input->ComponentTextData.MaxByte;
        descriptionMaxChar = input->ComponentTextData.MaxChar;
        if (descriptionMaxByte > 0 && edit.ByteCount > descriptionMaxByte ||
            descriptionMaxChar > 0 && edit.Value.EnumerateRunes().Count() > descriptionMaxChar)
        { descriptionCheck = "input_limit_exceeded"; error = "게임 소개말 입력칸의 길이 제한을 초과했습니다."; return false; }

        // SetText applies the game's input limits and formatting. Do not write
        // directly into StoredRecruitmentInfo or bypass the visible text input.
        descriptionSession = new(target, edit, DescriptionInvariant(agent));
        descriptionSession.InputCalls++;
        descriptionCheck = "setting_input_once";
        input->SetText(edit.Value);
        error = "";
        return true;
    }

    public SlotEditProgress UpdateDescriptionEdit(out RefreshTarget expected, out string error)
    {
        expected = null!; error = "소개말 반영 결과를 확인하지 못했습니다.";
        var session = descriptionSession;
        if (session == null) return SlotEditProgress.Failed;
        ReadEditorAction();
        if (SelfId != session.Original.SelfId || BlockReason != null ||
            !ReferenceEquals(editingTarget, session.Original) || EditorClosedRevision != editingFromClosedRevision ||
            !buttonTextReadable || buttonMode != RecruitmentEditorMode.Refresh || Visible("LookingForGroupSelectRole"))
        { descriptionCheck = "editor_binding_changed"; error = "모집 수정창의 상태가 바뀌어 중단했습니다."; return SlotEditProgress.Failed; }
        var addon = Plugin.GameGui.GetAddonByName<AddonLookingForGroupCondition>("LookingForGroupCondition", 1);
        var agent = AgentLookingForGroup.Instance();
        if (!NativeReadGuard.Readable(addon, (nuint)sizeof(AddonLookingForGroupCondition)) || !addon->IsVisible || !addon->IsReady ||
            !NativeReadGuard.Readable(agent, (nuint)sizeof(AgentLookingForGroup))) return SlotEditProgress.Failed;
        if (!ValidateDescriptionGuard(addon, agent, session, false, out error)) return SlotEditProgress.Failed;
        if (Environment.TickCount64 - session.Started < 100) return SlotEditProgress.Waiting;

        var input = addon->CommentTextInput;
        // A failed input read must not be interpreted as a request for empty text.
        var visibleText = "";
        var visibleRead = NativeReadGuard.Readable(input, (nuint)sizeof(AtkComponentTextInput)) &&
            NativeReadGuard.Text(&input->EvaluatedString, out visibleText);
        var storedRead = NativeReadGuard.FixedText(agent->StoredRecruitmentInfo.Comment, out var stored);
        descriptionVisibleChars = visibleText.Length; descriptionStoredChars = stored.Length;
        if (!visibleRead || !storedRead || !ListingIdentity.SameText(visibleText, session.Edit.Value))
        {
            descriptionCheck = "input_result_waiting";
            error = "ゲーム 입력창의 소개말이 요청한 내용과 다릅니다. 길이와 입력 가능한 문자를 확인해 주세요.";
            return Environment.TickCount64 - session.Started <= 3000 ? SlotEditProgress.Waiting : SlotEditProgress.Failed;
        }
        if (session.Phase == "input")
        {
            if (!ListingIdentity.SameText(stored, session.Edit.Value) && !ListingIdentity.SameText(stored, session.Original.Description))
            { descriptionCheck = "stored_comment_changed_elsewhere"; error = "소개말이 다른 내용으로 바뀌어 중단했습니다."; return SlotEditProgress.Failed; }
            session.Phase = "verifying";
            // SetText may already have notified the agent. Otherwise use the
            // original PFRemote CreatePf b__0 / FireLfgCommand(string): command
            // 15, UTF-8 text, FireCallback(3, values, true). Command 0 with the
            // same comment is sent later by Submit, after every final guard.
            if (!ListingIdentity.SameText(stored, session.Edit.Value))
            {
                session.CallbackCalls++;
                descriptionCheck = "notifying_comment_once";
                FireDescriptionCallback((AtkUnitBase*)addon, 15, session.Edit.Value);
                return SlotEditProgress.Waiting;
            }
        }
        if (!ValidateDescriptionGuard(addon, agent, session, true, out error) || !buttonEnabled)
            return SlotEditProgress.Waiting;
        editingTarget = session.Expected;
        if (!ValidateEditor(session.Expected, out _, out _, out error)) return SlotEditProgress.Failed;
        session.Completed = true; session.Phase = "complete"; descriptionCheck = "draft_description_verified";
        expected = session.Expected;
        return SlotEditProgress.Complete;
    }

    private bool ValidateDescriptionGuard(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent,
        DescriptionSession session, bool final, out string error)
    {
        error = "소개말 수정 중 참가자 또는 다른 모집 조건이 바뀌어 중단했습니다.";
        if (DescriptionInvariant(agent) != session.Invariant)
        { descriptionCheck = "other_criteria_changed"; return false; }
        if (final)
        {
            var input = addon->CommentTextInput;
            if (!NativeReadGuard.Readable(input, (nuint)sizeof(AtkComponentTextInput)) ||
                !NativeReadGuard.Text(&input->EvaluatedString, out var visible) ||
                !NativeReadGuard.FixedText(agent->StoredRecruitmentInfo.Comment, out var stored) ||
                !ListingIdentity.SameText(visible, session.Edit.Value) || !ListingIdentity.SameText(stored, session.Edit.Value))
            { descriptionCheck = "description_result_waiting"; error = "소개말이 아직 요청한 내용과 일치하지 않습니다."; return false; }
        }
        error = "";
        return true;
    }

    private static string DescriptionInvariant(AgentLookingForGroup* agent) => DraftInvariant(agent, false) + "/" +
        Convert.ToHexString(MemoryMarshal.AsBytes(agent->StoredRecruitmentInfo.SlotFlags));

    private static bool FireDescriptionCallback(AtkUnitBase* addon, int command, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text + '\0');
        var values = stackalloc AtkValue[3];
        new Span<AtkValue>(values, 3).Clear();
        values[0].Type = AtkValueType.Int; values[0].Int = command;
        fixed (byte* pointer = bytes)
        {
            // Borrow the pinned string only for this synchronous callback.
            values[1].Type = AtkValueType.String; values[1].String.Value = pointer;
            return addon->FireCallback(3, values, true);
        }
    }
}
