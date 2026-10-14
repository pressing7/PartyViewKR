using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

namespace PartyViewKR;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager Commands { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IPartyList PartyList { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;

    private readonly WindowSystem windows = new("PartyViewKR");
    private readonly PartyReader reader = new();
    private readonly ListingReader listingReader = new();
    private readonly ListingRefreshHost refreshHost;
    private readonly PartyWindow window;
    private long lastRead;
    private bool readRequested = true;
    private bool disposed;
    private string lastError = "";
    private string lastListingError = "";
    internal PartySnapshot Snapshot { get; private set; } = PartySnapshot.Empty;
    internal ListingSnapshot Listing { get; private set; } = ListingSnapshot.Empty;
    internal ListingRefresh Refresh { get; }
    internal bool ListingBusy => listingReader.Busy;
    internal string Diagnostic => Snapshot.Diagnostic + "\n" + Listing.Diagnostic + "\n" + Refresh.Diagnostic + "\n" + refreshHost.Diagnostic;

    public Plugin()
    {
        refreshHost = new ListingRefreshHost(this, listingReader);
        Refresh = new ListingRefresh(refreshHost);
        window = new PartyWindow(this);
        windows.AddWindow(window);
        Commands.AddHandler("/pvkr", new CommandInfo(OnCommand) { HelpMessage = "파티원·모집글 조회 및 수동 갱신 창 열기" });
        Commands.AddHandler("/partyviewkr", new CommandInfo(OnCommand) { HelpMessage = "파티원·모집글 조회 및 수동 갱신 창 열기" });
        PluginInterface.UiBuilder.Draw += windows.Draw;
        PluginInterface.UiBuilder.OpenMainUi += Open;
        PluginInterface.UiBuilder.OpenConfigUi += Open;
        Framework.Update += OnUpdate;
        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "LookingForGroupDetail", OnDetailUpdated);
        AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "LookingForGroupDetail", OnDetailUpdated);
        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "LookingForGroupCondition", OnEditorUpdated);
        AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "LookingForGroupCondition", OnEditorUpdated);
        AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "LookingForGroupCondition", OnEditorClosed);
    }

    private void OnCommand(string command, string arguments) => Open();
    private void Open() { window.IsOpen = true; window.BringToFront(); RequestRead(); }
    internal void RequestRead() => readRequested = true;
    internal void QueryOwnListing() { if (!Refresh.Busy) listingReader.RequestOwn(); RequestRead(); }
    internal void RefreshOwnListing() { Refresh.Request(Environment.TickCount64); RequestRead(); }
    internal void CancelRefresh() { Refresh.Cancel(); RequestRead(); }
    private void OnDetailUpdated(AddonEvent addonEvent, AddonArgs args) { listingReader.DetailUpdated(); RequestRead(); }
    private void OnEditorUpdated(AddonEvent addonEvent, AddonArgs args) { refreshHost.EditorUpdated(); RequestRead(); }
    private void OnEditorClosed(AddonEvent addonEvent, AddonArgs args) { refreshHost.EditorClosed(); RequestRead(); }

    private void OnUpdate(IFramework framework)
    {
        if (disposed) return;
        var now = Environment.TickCount64;
        if (!readRequested && now - lastRead < 500) return;
        readRequested = false;
        lastRead = now;
        try
        {
            Snapshot = reader.Read(Snapshot);
            lastError = "";
        }
        catch (Exception ex)
        {
            var message = ex.GetBaseException().Message;
            Snapshot = Snapshot with { Status = "조회 오류 — 마지막 조회 결과", IsStale = true, Error = message };
            if (message != lastError) Log.Error(ex, "PartyViewKR: party read failed");
            lastError = message;
        }
        try
        {
            Listing = listingReader.Read(Snapshot, Listing);
            lastListingError = "";
        }
        catch (Exception ex)
        {
            var message = ex.GetBaseException().Message;
            listingReader.RecordFailure(message);
            if (Refresh.Busy) Refresh.Fail(message);
            Listing = Listing with { Status = "모집글 조회 오류 — 마지막 결과", IsLive = false, Note = message,
                Diagnostic = "listingRead=failed\nlistingError=" + message };
            if (message != lastListingError) Log.Error(ex, "PartyViewKR: listing read failed");
            lastListingError = message;
        }
        try { Refresh.Update(now); }
        catch (Exception ex)
        {
            Refresh.Fail(ex.GetBaseException().Message);
            Log.Error(ex, "PartyViewKR: manual listing refresh stopped");
        }
    }

    public void Dispose()
    {
        disposed = true;
        Refresh.Cancel();
        AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "LookingForGroupDetail", OnDetailUpdated);
        AddonLifecycle.UnregisterListener(AddonEvent.PostRefresh, "LookingForGroupDetail", OnDetailUpdated);
        AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "LookingForGroupCondition", OnEditorUpdated);
        AddonLifecycle.UnregisterListener(AddonEvent.PostRefresh, "LookingForGroupCondition", OnEditorUpdated);
        AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "LookingForGroupCondition", OnEditorClosed);
        Framework.Update -= OnUpdate;
        PluginInterface.UiBuilder.Draw -= windows.Draw;
        PluginInterface.UiBuilder.OpenMainUi -= Open;
        PluginInterface.UiBuilder.OpenConfigUi -= Open;
        Commands.RemoveHandler("/pvkr");
        Commands.RemoveHandler("/partyviewkr");
        windows.RemoveAllWindows();
    }
}
