using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace PartyViewKR;

internal sealed partial class PartyWindow : Window
{
    private readonly Plugin plugin;
    private ListingJobDraft? shownDraft;
    private ListingDescriptionDraft? shownDescriptionDraft;
    private ListingObjectiveDraft? shownObjectiveDraft;
    private ListingBeginnerDraft? shownBeginnerDraft;
    private ListingUniqueJobsDraft? shownUniqueJobsDraft;
    private ListingPrivateDraft? shownPrivateDraft;
    private ListingCompletionDraft? shownCompletionDraft;
    private ListingItemLevelDraft? shownItemLevelDraft;
    private ListingRuleDraft? shownRuleDraft;
    internal PartyWindow(Plugin plugin) : base("Party View KR###PartyViewKR")
    {
        this.plugin = plugin;
        Size = new Vector2(720, 630);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(500, 250), MaximumSize = new Vector2(float.MaxValue) };
    }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("pvkr-tabs"))
        {
            DrawTab("디스코드", DrawDiscord);
            DrawTab("자동화", DrawAutomation);
            DrawTab("설정", DrawSettings);
            DrawTab("디버그", DrawDebug);
            ImGui.EndTabBar();
        }

        // Keep popup IDs outside tab/child scopes. Async drafts must still open
        // or close correctly if the selected tab changes while a read completes.
        DrawJobEditor();
        DrawDescriptionEditor();
        DrawObjectiveEditor();
        DrawBeginnerEditor();
        DrawUniqueJobsEditor();
        DrawPrivateEditor();
        DrawCompletionEditor();
        DrawItemLevelEditor();
        DrawRuleEditor();
    }

    private static void DrawTab(string label, Action drawContent)
    {
        if (!ImGui.BeginTabItem(label)) return;
        // Independent scroll positions keep the tab bar visible on small windows.
        if (ImGui.BeginChild("content", Vector2.Zero, false)) drawContent();
        ImGui.EndChild();
        ImGui.EndTabItem();
    }

    private static void DrawSection(string label)
    {
        ImGui.Spacing();
        ImGui.TextUnformatted(label);
        ImGui.Separator();
    }
}
