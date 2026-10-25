using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace PartyViewKR;

public sealed partial class Plugin
{
    private ListingWebBridge? ActiveBridge => WebEnabled ? webServer!.Bridge : RelayEnabled ? relayClient!.Bridge : DiscordEnabled ? discordClient!.Bridge : null;
    private bool Recovering => ActiveBridge?.RecoveryActive == true;
    private void ProcessRecovery(long now)
    {
        if (webHost != null) ActiveBridge?.BeforeReadRecovery(webHost, now);
    }

    private void StopForRecovery()
    {
        Refresh.Cancel(); listingReader.FailRequest(); ObjectiveTrace.RequestStop();
        requestedSlot = null; requestedDescription = requestedObjective = requestedBeginner = requestedUniqueJobs =
            requestedPrivate = requestedCompletion = requestedItemLevel = false; requestedRule = null;
        JobDraft = null; DescriptionDraft = null; ObjectiveDraft = null; BeginnerDraft = null;
        UniqueJobsDraft = null; PrivateDraft = null; CompletionDraft = null; ItemLevelDraft = null; RuleDraft = null;
        PendingChanges.Clear();
        RequestRead();
    }

    private sealed partial class WebHost : IListingRecoveryHost
    {
        public string? RecoveryBlockReason => plugin.disposed ? "플러그인이 종료 중입니다." :
            !ClientState.IsLoggedIn || SelfId == 0 ? "게임에 로그인해 주세요." :
            !NativeCompatibility.IsCompatible ? "구조 DLL의 호환 검사를 통과하지 못해 창을 닫지 못했습니다." :
            Condition[ConditionFlag.BetweenAreas] || Condition[ConditionFlag.BetweenAreas51] ? "지역 이동 중입니다. 이동이 끝나면 다시 눌러 주세요." : null;
        public void StopForRecovery() { plugin.StopForRecovery(); ownedQuery.Clear(); ownedRefresh.Clear(); }
        public bool ReadRecoveryWindows(out RecruitmentWindows windows) => plugin.refreshHost.ReadRecoveryWindows(out windows);
        public bool CloseRecoveryWindow(RecruitmentWindow kind, RecruitmentWindowTicket ticket, out string error) =>
            plugin.refreshHost.CloseRecoveryWindow(kind, ticket, out error);
    }
}

internal sealed unsafe partial class ListingRefreshHost
{
    internal bool ReadRecoveryWindows(out RecruitmentWindows windows)
    {
        windows = null!;
        if (!ReadRecoveryWindow(RecruitmentWindow.Editor, out var editor) || !ReadRecoveryWindow(RecruitmentWindow.Roles, out var role)) return false;
        windows = new(editor, role); return true;
    }

    private bool ReadRecoveryWindow(RecruitmentWindow kind, out RecruitmentWindowTicket ticket)
    {
        ticket = new(0, 0);
        var addon = Plugin.GameGui.GetAddonByName<AtkUnitBase>(kind == RecruitmentWindow.Editor ? "LookingForGroupCondition" : "LookingForGroupSelectRole", 1);
        if (addon == null) return true;
        if (!NativeReadGuard.Readable(addon, (nuint)sizeof(AtkUnitBase))) return false;
        if (!addon->IsVisible) return true;
        ticket = new((nint)addon, kind == RecruitmentWindow.Editor ? EditorClosedRevision : Interlocked.Read(ref roleClosedRevision));
        return true;
    }

    internal bool CloseRecoveryWindow(RecruitmentWindow kind, RecruitmentWindowTicket ticket, out string error)
    {
        error = "수정창이 준비되지 않아 닫지 못했습니다.";
        if (!NativeCompatibility.IsCompatible || !ReadRecoveryWindow(kind, out var current) ||
            current != ticket || ticket.Address == 0) return false;
        var addon = (AtkUnitBase*)ticket.Address;
        if (!addon->IsReady || !NativeReadGuard.Readable(addon->VirtualTable, (nuint)sizeof(AtkUnitBase.AtkUnitBaseVirtualTable))) return false;
        // Explicit user recovery only: close the named editor/child, never
        // Confirm, Submit, Create, withdrawal, or unrelated confirmation dialogs.
        addon->Close(true);
        error = ""; return true;
    }
}
