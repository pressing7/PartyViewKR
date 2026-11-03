using System.Text.Json;
using PartyViewKR;

static class AfkRefreshSchedulerTests
{
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        test("automatic refresh is opt-in and saved settings do not activate AFK", () => {
            var c = JsonSerializer.Deserialize<RelayConfiguration>("{}")!;
            check(!c.ReadAfkSettings().RefreshEnabled && c.ReadAfkSettings().RefreshMinutes == 30, "unsafe old-config defaults");
            c.WriteAfkSettings(new(true, 7, false, 20, true, true, 55));
            var json = JsonSerializer.Serialize(c);
            var saved = JsonSerializer.Deserialize<RelayConfiguration>(json)!;
            check(saved.ReadAfkSettings() == new AfkSettings(true, 7, false, 20, true, true, 55) && !json.Contains("AfkEnabled"), "save changed other preferences or saved runtime activation");
            check(new AfkSettings(RefreshMinutes: 0).Normalize().RefreshMinutes == 1 &&
                new AfkSettings(RefreshMinutes: int.MaxValue).Normalize().RefreshMinutes == 55, "interval bounds");
        });
        test("automatic refresh requires both AFK and its independent option", () => {
            var f = new Fixture(); f.Scheduler.Configure(new(), 0); f.Tick(1_000_000);
            check(f.Refresh.RequestRevision == 0, "default option submitted");
            f.Host.AfkEnabled = false; f.Scheduler.Configure(new(RefreshEnabled: true, RefreshMinutes: 1), 1_000_000);
            f.Tick(2_000_000); check(f.Refresh.RequestRevision == 0, "AFK-off submitted");
            f.Host.AfkEnabled = true; f.Tick(2_000_500); f.Tick(2_060_499);
            check(f.Refresh.RequestRevision == 0, "enable immediately refreshed");
            f.Tick(2_060_500); check(f.Refresh.RequestRevision == 1, "separate refresh option did not schedule");
        });
        test("automatic refresh honors exact one and fifty-five minute periods", () => {
            foreach (var minutes in new[] { 1, 55 }) {
                var f = new Fixture(minutes); var due = minutes * 60000L;
                f.Tick(due - 1); check(f.Refresh.RequestRevision == 0, "early refresh");
                f.Tick(due); check(f.Refresh.RequestRevision == 1 && f.Scheduler.InFlight, "missing scheduled refresh");
                f.Tick(due + 100); check(f.Refresh.RequestRevision == 1 && f.Native.QueryCalls == 1, "duplicate run");
            }
        });
        test("automatic refresh uses fresh conditions and one submission per interval", () => {
            var f = new Fixture(); f.Tick(60000); f.Tick(60500);
            f.Native.Target = f.Native.Target with { ListingId = 15, Description = "새로 확인한 모집글", RemainingSeconds = 400 };
            f.Native.FinishQuery(); f.Tick(61000); f.Tick(61500);
            check(f.Native.SubmitCalls == 1 && f.Native.SubmittedDescription == "새로 확인한 모집글" &&
                f.Native.JobEditCalls == 0 && f.Native.DescriptionEditCalls == 0 && !f.Refresh.IsBatch, "stale conditions or pending edits applied");
            f.Tick(63000); f.Native.Target = f.Native.Target with { RemainingSeconds = 3600 };
            f.Native.FinishQuery(); f.Tick(63500);
            check(f.Refresh.Stage == RefreshStage.Completed && f.Scheduler.NextDue == 123500 && f.Scheduler.LastResult.Length > 0, "result or next interval missing");
            f.Tick(123499); check(f.Native.SubmitCalls == 1 && f.Refresh.RequestRevision == 1, "second submission early");
            f.Tick(123500); check(f.Refresh.RequestRevision == 2 && f.Native.SubmitCalls == 1, "next cycle skipped or bypassed own query");
        });
        test("manual editing and pending changes defer automatic refresh without losing its deadline", () => {
            foreach (var reason in new[] { "PVKR 편집 중", "적용 대기 변경사항", "게임 수정창", "지역 이동", "구조 불일치" }) {
                var f = new Fixture(); f.Host.Blocked = reason; f.Tick(60000); f.Tick(120000);
                check(f.Refresh.RequestRevision == 0 && f.Scheduler.Status.Contains(reason), "block was ignored");
                f.Host.Blocked = null; f.Tick(120500);
                check(f.Refresh.RequestRevision == 1, "deferred cycle lost");
            }
        });
        test("unreadable roster unknown recruitment and full party do not start automatic refresh", () => {
            var f = new Fixture(); f.Host.RosterReadable = false; f.Tick(60000);
            f.Host.RosterReadable = true; f.Host.RecruitmentKnown = false; f.Tick(120000);
            f.Host.RecruitmentKnown = true; f.Host.RecruitmentComplete = true; f.Tick(180000);
            check(f.Refresh.RequestRevision == 0 && f.Native.SubmitCalls == 0, "invalid/full recruitment refreshed");
            f.Host.RecruitmentComplete = false; f.Tick(180500);
            check(f.Refresh.RequestRevision == 1, "new active recruitment never resumed");
        });
        test("manual refresh completion restarts the automatic interval", () => {
            var f = new Fixture(); f.Refresh.Request(55000); f.Tick(55000);
            f.Native.FinishQuery(); f.Tick(55500); f.Tick(56000); f.Tick(57500);
            f.Native.Target = f.Native.Target with { RemainingSeconds = 3600 };
            f.Native.FinishQuery(); f.Tick(58000); f.Tick(60000);
            check(f.Refresh.RequestRevision == 1 && f.Scheduler.NextDue == 118000, "manual refresh followed by immediate auto refresh");
            f.Tick(118000); check(f.Refresh.RequestRevision == 2, "manual refresh stopped scheduling permanently");
        });
        test("changing only query preferences leaves automatic refresh deadline unchanged", () => {
            var f = new Fixture(); f.Scheduler.Configure(new(true, 5, false, 23, true, true, 1), 20000);
            f.Tick(60000); check(f.Refresh.RequestRevision == 1, "unrelated option postponed automatic refresh");
        });
        test("changing refresh interval starts a new full interval", () => {
            var f = new Fixture(); f.Scheduler.Configure(new(RefreshEnabled: true, RefreshMinutes: 2), 20000);
            f.Tick(60000); f.Tick(139999); check(f.Refresh.RequestRevision == 0, "old deadline survived");
            f.Tick(140000); check(f.Refresh.RequestRevision == 1, "changed interval missing");
        });
        test("queued automatic refresh rechecks blocks before native dispatch", () => {
            var f = new Fixture(); f.Tick(60000); f.Host.Blocked = "사용자 편집 시작"; f.Tick(60500);
            check(!f.Refresh.Busy && f.Native.QueryCalls == 0 && f.Native.EditCalls == 0, "queued request escaped preflight");
            f.Host.Blocked = null; f.Tick(61000);
            check(f.Refresh.RequestRevision == 1, "blocked request immediately retried");
        });
        test("disabling before an editor opens cancels queued and reading automatic work", () => {
            foreach (var reading in new[] { false, true }) {
                var f = new Fixture(); f.Tick(60000); if (reading) f.Tick(60500);
                f.Scheduler.Configure(new(), 60750); f.Tick(61000); f.Tick(200000);
                check(!f.Refresh.Busy && f.Native.EditCalls == 0 && f.Native.SubmitCalls == 0 && f.Refresh.RequestRevision == 1, "disabled work opened or resubmitted");
            }
        });
        test("disabling AFK after editor opens finishes that run but starts no next run", () => {
            var f = new Fixture(); f.Tick(60000); f.Tick(60500); f.Native.FinishQuery(); f.Tick(61000);
            f.Host.AfkEnabled = false; f.Tick(61500);
            check(f.Native.SubmitCalls == 1, "disable stranded an already open recruitment editor");
            f.Tick(63000); f.Native.FinishQuery(); f.Tick(63500); f.Tick(200000);
            check(f.Refresh.RequestRevision == 1 && !f.Scheduler.InFlight && f.Scheduler.NextDue == null, "disabled schedule continued");
        });
        test("full recruitment before editor opening cancels automatic query", () => {
            var f = new Fixture(); f.Tick(60000); f.Tick(60500); f.Native.FinishQuery();
            f.Host.RecruitmentComplete = true; f.Tick(61000);
            check(f.Native.EditCalls == 0 && f.Native.SubmitCalls == 0 && !f.Refresh.Busy, "full recruitment opened editor");
        });
        test("logout character change and stale framework ticks cancel only the owned automatic run", () => {
            foreach (var mode in new[] { "logout", "character", "unloaded", "paused" }) {
                var f = new Fixture(); f.Tick(60000);
                if (mode == "logout") f.Host.LoggedIn = false;
                if (mode == "character") f.Host.SelfId = 20;
                if (mode == "unloaded") f.Host.SelfId = 0;
                f.Tick(mode == "paused" ? 66000 : 60500);
                check(!f.Refresh.Busy && f.Native.QueryCalls == 0, "stale queued dispatch: " + mode);
            }
            var n = new Fixture(); n.Tick(60000); n.Refresh.Cancel(); n.Refresh.Request(60100);
            n.Host.AfkEnabled = false; n.Tick(60500);
            check(n.Refresh.Busy && n.Native.QueryCalls == 1, "new manual run was cancelled as old auto run");
        });
        test("failed or unconfirmed automatic refresh waits for next interval without rapid retries", () => {
            var f = new Fixture(); f.Tick(60000); f.Tick(60500);
            f.Native.QueryBusy = false; f.Native.QuerySucceeded = false; f.Tick(61000);
            check(f.Refresh.Stage == RefreshStage.Stopped && f.Scheduler.LastResult.Length > 0, "query failure hidden");
            f.Tick(61001); f.Tick(120999); check(f.Refresh.RequestRevision == 1, "failure retry loop");
            f.Tick(121000); check(f.Refresh.RequestRevision == 2, "next interval lost after failure");

            var u = new Fixture(); u.Tick(60000); u.Tick(60500); u.Native.FinishQuery(); u.Tick(61000);
            u.Native.SubmitAllowed = false; u.Tick(61500); u.Tick(62000);
            check(u.Refresh.Stage == RefreshStage.Unconfirmed && u.Native.SubmitCalls == 1 && u.Scheduler.LastResult.Length > 0, "uncertain submit repeated or hidden");
            u.Tick(121500); check(u.Native.SubmitCalls == 1 && u.Refresh.RequestRevision == 1, "open failed editor was overwritten");
        });
        test("AFK off and back on starts a fresh interval without replaying missed ticks", () => {
            var f = new Fixture(); f.Host.AfkEnabled = false; f.Tick(30000); f.Tick(5_000_000);
            f.Host.AfkEnabled = true; f.Tick(5_000_500); f.Tick(5_060_499);
            check(f.Refresh.RequestRevision == 0, "missed cycles replayed");
            f.Tick(5_060_500); check(f.Refresh.RequestRevision == 1, "fresh activation failed");
        });
        test("manual window recovery postpones an overdue automatic cycle", () => {
            var f = new Fixture(); f.Host.Blocked = "수정창 열림"; f.Tick(60000);
            f.Scheduler.Postpone(70000); f.Host.Blocked = null; f.Tick(70500); f.Tick(129999);
            check(f.Refresh.RequestRevision == 0, "recovery immediately reopened the recruitment editor");
            f.Tick(130000); check(f.Refresh.RequestRevision == 1, "recovery permanently stopped scheduling");
        });
    }

    private sealed class Fixture
    {
        internal readonly FakeHost Native = new();
        internal readonly ListingRefresh Refresh;
        internal readonly SchedulerHost Host;
        internal readonly AfkRefreshScheduler Scheduler;
        internal Fixture(int minutes = 1)
        {
            Refresh = new(Native); Host = new(Refresh, Native); Scheduler = new(Refresh);
            Scheduler.Configure(new(RefreshEnabled: true, RefreshMinutes: minutes), 0);
            Tick(0);
        }
        internal void Tick(long now)
        {
            Scheduler.BeforeRead(Host, now);
            Refresh.Update(now);
            Scheduler.Update(Host, now);
        }
    }

    private sealed class SchedulerHost(ListingRefresh refresh, FakeHost native) : IAfkRefreshHost
    {
        public bool AfkEnabled { get; set; } = true;
        public bool LoggedIn { get; set; } = true;
        public ulong SelfId { get; set; } = 10;
        public bool RecruitmentKnown { get; set; } = true;
        public bool RosterReadable { get; set; } = true;
        public bool RecruitmentComplete { get; set; }
        internal string? Blocked;
        public string? RefreshBlockReason(bool ownQueuedRefresh) => Blocked ??
            (native.QueryBusy ? "조회 중" : native.EditorVisible ? "수정창 열림" : !ownQueuedRefresh && refresh.Busy ? "갱신 중" : null);
        public void WakeReader() { }
    }
}
