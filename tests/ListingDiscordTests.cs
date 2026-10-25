using System.Text;
using System.Text.Json;
using Discord;
using PartyViewKR;

static partial class ListingDiscordTests
{
    private static ulong serial = 100;
    private const ulong App = 500, Owner = 600;
    private static DiscordInput Input(string custom = "", string[]? values = null, Dictionary<string, string>? fields = null) =>
        new(++serial, App, Owner, "", custom, values ?? [], fields ?? []);
    private static string Id(DiscordScreen screen, string action) => ListingDiscordCard.Walk(screen.Components.Components)
        .Select(c => c switch { ButtonComponent b => b.CustomId, SelectMenuComponent s => s.CustomId, _ => "" })
        .Single(id => id.EndsWith(":" + action, StringComparison.Ordinal));
    private static SelectMenuComponent Menu(DiscordScreen screen, string action) => ListingDiscordCard.Walk(screen.Components.Components)
        .OfType<SelectMenuComponent>().Single(m => m.CustomId.EndsWith(":" + action, StringComparison.Ordinal));
    private static DiscordScreen Act(ListingDiscordPanel panel, DiscordScreen screen, string action, params string[] values) =>
        panel.Handle(Input(Id(screen, action), values), 1100).Screen!;
    private static DiscordScreen Field(ListingDiscordPanel panel, DiscordScreen screen, string field, string value)
    {
        screen = Settings(panel, screen);
        var modal = panel.Handle(Input(Id(screen, "form:settings")), 1100).Modal!;
        var input = FormInput(modal);
        if (field is "beginner" or "uniqueJobs" or "unrestricted" or "minimum_item_level" or "silence_echo") {
            var selected = input.Choices["toggles"].ToHashSet();
            if (value == "1") selected.Add(field); else selected.Remove(field);
            input.Choices["toggles"] = selected.ToArray();
        } else input.Fields[field] = value;
        return panel.Handle(input, 1100).Screen!;
    }
    private static DiscordScreen Settings(ListingDiscordPanel panel, DiscordScreen screen) =>
        ListingDiscordCard.Walk(screen.Components.Components).OfType<ButtonComponent>()
            .Any(m => m.CustomId.EndsWith(":form:settings", StringComparison.Ordinal)) ? screen : Act(panel, screen, "settings");
    private static DiscordScreen Text(ListingDiscordPanel panel, DiscordScreen screen, string field, string text)
    {
        screen = Settings(panel, screen);
        var form = field == "itemLevel" ? "settings" : field;
        var modal = panel.Handle(Input(Id(screen, "form:" + form)), 1100).Modal!;
        var input = FormInput(modal);
        input.Fields[field == "private" ? "password" : field == "description" ? "value" : field] = text;
        if (field == "private") input.Checks["enabled"] = true;
        return panel.Handle(input, 1100).Screen!;
    }
    private static DiscordInput FormInput(Modal modal)
    {
        var input = Input(modal.CustomId);
        foreach (var c in ListingDiscordCard.Walk(modal.Component.Components)) {
            switch (c) {
                case TextInputComponent t: input.Fields[t.CustomId] = t.Value ?? ""; break;
                case RadioGroupComponent r: input.Fields[r.CustomId] = r.Options.FirstOrDefault(o => o.IsDefault == true).Value ?? ""; break;
                case CheckboxGroupComponent g: input.Choices[g.CustomId] = g.Options.Where(o => o.DefaultState == true).Select(o => o.Value).ToArray(); break;
                case CheckboxComponent b: input.Checks[b.CustomId] = b.DefaultState == true; break;
            }
        }
        return input;
    }
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        RunCardFlow(test, check);
        RunRoster(test, check);
        ListingDiscordEmojiTests.Run(test, check);
        RunResults(test, check);
        test("discord rejects wrong owner or app without reading or queuing a game action", () => {
            var (b, h, _) = ListingWebEditTests.Ready(); var p = new ListingDiscordPanel(b, App, Owner);
            foreach (var input in new[] { Input() with { Command = "pvkr", UserId = 7 }, Input() with { Command = "pvkr", ApplicationId = 8 } }) {
                var result = p.Handle(input, 1100);
                check(result.Work == null && result.Modal == null && !result.Screen!.HasControls, "unauthorized controls exposed");
            }
            b.Update(h, 1200); check(h.StartCalls == 0 && !h.Native.QueryBusy, "unauthorized game access");
        });
        test("discord query uses the shared framework queue and shows the result", () => {
            var (b, h, _) = ListingWebEditTests.Ready(); var p = new ListingDiscordPanel(b, App, Owner);
            var work = p.Handle(Input() with { Command = "pvkr" }, 1100).Work!;
            check(!h.Native.QueryBusy && p.Start(work, 1100).Status == 202 && !h.Native.QueryBusy, "query bypassed framework");
            b.Update(h, 1200); check(h.Native.QueryBusy, "query not queued");
            h.Advance(1500); b.Update(h, 1500);
            check(p.Progress(work).Body is WebQuery { State: "complete" } && p.Complete(1500).Screen!.Text.Contains("테스트 모집자"), "query result missing");
        });
        test("discord stale screens, expired drafts and changed character cannot edit", () => {
            foreach (var mode in new[] { "old", "expired", "character", "revision" }) {
                var (b, h, _) = ListingWebEditTests.Ready(); var p = new ListingDiscordPanel(b, App, Owner);
                var screen = p.Complete(1000).Screen!; var id = Id(screen, "apply");
                if (mode == "old") p.Complete(1001);
                if (mode == "character") { h.Native.Target = h.Native.Target with { SelfId = 99 }; b.Update(h, 1001); }
                if (mode == "revision") { h.Native.Target = h.Native.Target with { Description = "바뀐 모집" }; b.Update(h, 1001); }
                var result = p.Handle(Input(id), mode == "expired" ? 901000 : 1100);
                check(result.Work == null && !result.Replace && h.StartCalls == 0, "stale screen edited: " + mode);
            }
        });
        test("discord slash command keeps a current draft but recovers from a stale draft", () => {
            var (b, h, _) = ListingWebEditTests.Ready(); var p = new ListingDiscordPanel(b, App, Owner);
            var screen = Field(p, p.Complete(1000).Screen!, "objective", "3");
            check(p.Handle(Input() with { Command = "pvkr" }, 1100).Screen!.Text.Contains("적용 대기"), "pending draft lost");
            h.Native.Target = h.Native.Target with { Description = "외부에서 수정" }; b.Update(h, 1200);
            check(p.Handle(Input() with { Command = "pvkr" }, 1300).Work is { Query: true }, "stale draft trapped user");
        });
        test("discord combines all settings and preserves PIN 0000 without displaying it", () => {
            var (b, h, v) = ListingWebEditTests.Ready(); var p = new ListingDiscordPanel(b, App, Owner);
            var screen = Settings(p, p.Complete(1000).Screen!);
            var modal = p.Handle(Input(Id(screen, "form:settings")), 1100).Modal!;
            check(modal.Component.Components.Count == 5, "unified form exceeds modal limit");
            var input = FormInput(modal);
            input.Fields["objective"] = "3"; input.Fields["completion"] = "4";
            input.Fields["itemLevel"] = "999"; input.Fields["loot"] = "2";
            input.Choices["toggles"] = ["beginner", "uniqueJobs", "unrestricted", "minimum_item_level", "silence_echo"];
            screen = p.Handle(input, 1100).Screen!;
            check(screen.Text.Contains("적용 대기 9개"), "one general form lost fields");
            var reopened = FormInput(p.Handle(Input(Id(screen, "form:settings")), 1100).Modal!);
            check(reopened.Fields.OrderBy(k => k.Key).SequenceEqual(input.Fields.OrderBy(k => k.Key)) &&
                reopened.Choices["toggles"].ToHashSet().SetEquals(input.Choices["toggles"]), "reopened unified form lost draft defaults");
            screen = Text(p, screen, "description", "새 소개말");
            screen = Text(p, screen, "private", "0000");
            check(!screen.Text.Contains("0000"), "PIN appeared in response");
            var work = p.Handle(Input(Id(screen, "apply")), 1100).Work!;
            check(ListingWebEdit.Decode(work.Body!, h.Native.Target, v.Editor!.Revision, out var changes, out var error), error);
            check(changes.Length == 11 && changes.Select(c => c.Edit).OfType<ListingPrivateEdit>().Single().Password == "0000", "combined edits lost");
            check(p.Start(work, 1100).Status == 202, "batch not accepted");
            for (long now = 1500; now < 60000; now += 500) {
                h.Advance(now); b.Update(h, now);
                if (p.Progress(work).Body is WebOperation { State: not ("queued" or "running") }) break;
            }
            check(h.Native.SubmitCalls == 1 && p.Progress(work).Body is WebOperation { Results.Length: 11 }, "batch did not submit once");
        });
        test("discord validates UTF8 byte limit, item-level bounds and four ASCII PIN digits", () => {
            foreach (var (field, value) in new[] { ("description", new string('가', 65)), ("description", "bad\0text"),
                ("itemLevel", "1000"), ("itemLevel", "-1"), ("private", "123"), ("private", "１２３４") }) {
                var (b, _, _) = ListingWebEditTests.Ready(); var p = new ListingDiscordPanel(b, App, Owner);
                var screen = Text(p, p.Complete(1000).Screen!, field, value);
                check(!screen.HasControls && !screen.Text.Contains(value), "invalid value accepted or echoed");
            }
            var (bridge, host, view) = ListingWebEditTests.Ready(); var panel = new ListingDiscordPanel(bridge, App, Owner);
            var valid = Text(panel, panel.Complete(1000).Screen!, "description", new string('가', 64));
            var work = panel.Handle(Input(Id(valid, "apply")), 1100).Work!;
            check(ListingWebEdit.Decode(work.Body!, host.Native.Target, view.Editor!.Revision, out _, out _), "192-byte boundary rejected");
        });
        test("discord job selector preserves 4, 24, 48 and friendly native positions", () => {
            foreach (var (count, friendly) in new[] { (4, false), (24, false), (48, false), (18, true) }) {
                var (b, h, _) = ListingWebEditTests.Ready(); h.Native.Target = WebEditFakeHost.Target(count, friendly); b.Update(h, 1001);
                var p = new ListingDiscordPanel(b, App, Owner); var screen = Act(p, p.Complete(1001).Screen!, "settings");
                screen = Act(p, screen, "jobs");
                var lastGroup = b.ReadView().Listing!.Groups.Length - 1;
                if (lastGroup > 0) screen = Act(p, screen, "group", lastGroup.ToString());
                var indices = Menu(screen, "slot").Options.Select(o => int.Parse(o.Value)).ToArray();
                var expected = friendly ? 23 : count - 1;
                check(indices[^1] == expected && (!friendly || indices.SequenceEqual(Enumerable.Range(16, 8))), "group slots packed/truncated");
                screen = Act(p, screen, "slot", expected.ToString());
                screen = Act(p, screen, "choosejobs:0", "19");
                var work = p.Handle(Input(Id(screen, "apply")), 1100).Work!;
                using var document = JsonDocument.Parse(work.Body!);
                check(document.RootElement.GetProperty("changes")[0].GetProperty("slot").GetInt32() == expected, "display index sent as native index");
            }
        });
        test("discord five role menus include every job and class and preserve other selections", () => {
            var (b, h, _) = ListingWebEditTests.Ready();
            uint[] ids = [19,21,32,37,24,33,28,40,20,22,30,34,39,41,43,23,31,38,25,27,35,42,36,1,3,6,2,4,29,5,7,26];
            var rules = ListingJobRules.Read(ids.Select(id => new RecruitmentJob(id, "직업" + id)));
            h.Native.Target = WebEditFakeHost.Target(48);
            h.Native.Target = h.Native.Target with { Slots = h.Native.Target.Slots with { JobRules = rules } }; b.Update(h, 1001);
            var p = new ListingDiscordPanel(b, App, Owner); var screen = Act(p, p.Complete(1001).Screen!, "settings");
            screen = Act(p, screen, "jobs"); screen = Act(p, screen, "alljobs");
            (string Label, uint[] Ids)[] groups = [
                ("방어", [19,21,32,37,1,3]), ("회복", [24,33,28,40,6]),
                ("근거리 공격", [20,22,30,34,39,41,43,2,4,29]),
                ("원거리 물리", [23,31,38,5]), ("원거리 마법", [25,27,35,42,36,7,26])
            ];
            var controls = ListingDiscordCard.Walk(screen.Components.Components).ToArray();
            check(controls.OfType<SelectMenuComponent>().Count(m => m.CustomId.Contains(":choosejobs:")) == 5 &&
                controls.Length <= 40 && screen.Text.Length <= 4000, "five role menus exceed Discord limits");
            var expected = ids.ToHashSet();
            for (var group = 0; group < groups.Length; group++) {
                var menu = Menu(screen, "choosejobs:" + group);
                check(menu.Placeholder == groups[group].Label && screen.Text.Contains("**" + groups[group].Label + "**") &&
                    menu.Options.Select(o => uint.Parse(o.Value)).SequenceEqual(groups[group].Ids) &&
                    menu.MinValues == 0 && menu.MaxValues == groups[group].Ids.Length, "wrong role membership or label: " + group);
                screen = Act(p, screen, "choosejobs:" + group, groups[group].Ids[^1].ToString());
                expected.ExceptWith(groups[group].Ids); expected.Add(groups[group].Ids[^1]);
                var actual = ListingDiscordCard.Walk(screen.Components.Components).OfType<SelectMenuComponent>()
                    .Where(m => m.CustomId.Contains(":choosejobs:")).SelectMany(m => m.Options)
                    .Where(o => o.IsDefault == true).Select(o => uint.Parse(o.Value)).ToHashSet();
                check(actual.SetEquals(expected), "editing one role changed another role: " + group);
            }
            check(!p.Handle(Input(Id(screen, "choosejobs:0"), ["6"]), 1100).Replace, "healer class accepted in defense menu");
            check(!p.Handle(Input(Id(screen, "choosejobs:0"), ["19", "19"]), 1100).Replace, "duplicate job accepted");
            for (var group = 0; group < groups.Length; group++) screen = Act(p, screen, "choosejobs:" + group);
            check(p.Handle(Input(Id(screen, "apply")), 1100).Work == null, "empty job list submitted");
            screen = Act(p, screen, "alljobs");
            var work = p.Handle(Input(Id(screen, "apply")), 1100).Work!;
            using var doc = JsonDocument.Parse(work.Body!);
            check(doc.RootElement.GetProperty("changes")[0].GetProperty("jobs").EnumerateArray().Select(x => x.GetUInt32()).SequenceEqual(ids), "job order changed");
        });
        test("discord rejects occupied, unregistered and forged job IDs", () => {
            var (b, h, _) = ListingWebEditTests.Ready(); h.Native.Target = WebEditFakeHost.Target(18, true); b.Update(h, 1001);
            var p = new ListingDiscordPanel(b, App, Owner); var screen = Act(p, p.Complete(1001).Screen!, "settings"); screen = Act(p, screen, "jobs");
            foreach (var index in new[] { "0", "5", "23" }) check(p.Handle(Input(Id(screen, "slot"), [index]), 1100).Replace == false, "wrong-group/occupied/padding selected");
            check(p.Handle(Input(Id(screen, "choosejobs:0"), ["999"]), 1100).Replace == false, "unknown job accepted");
        });
        test("discord output fits message/component limits with long 48-slot changes", () => {
            var (b, h, _) = ListingWebEditTests.Ready(); h.Native.Target = WebEditFakeHost.Target(48); b.Update(h, 1001);
            var p = new ListingDiscordPanel(b, App, Owner); var screen = p.Complete(1001, new string('한', 3000)).Screen!;
            check(screen.Text.Length <= 4000 && ListingDiscordCard.Walk(screen.Components.Components).Count() <= 40, "Discord payload oversized");
            var cut = ListingDiscordPanel.Limit(new string('x', 1898) + "😀끝");
            check(cut.Length < 1901 && !char.IsHighSurrogate(cut[^2]), "surrogate split");
        });
        test("discord failed ACK and shutdown during ACK never enqueue a mutation", () => {
            foreach (var cancel in new[] { false, true }) {
                var (b, h, _) = ListingWebEditTests.Ready(); using var cts = new CancellationTokenSource();
                var session = new ListingDiscordSession(b, App, Owner, cts.Token, () => 1100);
                h.Blocked = "query blocked"; b.Update(h, 1001);
                var response = new FakeDiscordResponse(); session.Process(Input() with { Command = "pvkr" }, response).GetAwaiter().GetResult();
                h.Blocked = null; b.Update(h, 1002);
                // Complete screen reflects the earlier blocked state but apply is checked live.
                response.FailDefer = !cancel; response.OnDefer = cancel ? cts.Cancel : null;
                session.Process(Input(Id(response.Last!, "apply")), response).GetAwaiter().GetResult();
                b.Update(h, 1200);
                check(b.ReadView().Operation == null && h.StartCalls == 0, "mutation ran without ACK");
            }
        });
        test("discord busy and duplicate events never replay queries", () => {
            var (b, h, _) = ListingWebEditTests.Ready(); using var cts = new CancellationTokenSource();
            var session = new ListingDiscordSession(b, App, Owner, cts.Token, () => 1100);
            var ack = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var first = new FakeDiscordResponse { Ack = ack.Task }; var request = Input() with { Command = "pvkr" };
            var processing = session.Process(request, first);
            var concurrent = new FakeDiscordResponse(); session.Process(Input() with { Command = "pvkr" }, concurrent).GetAwaiter().GetResult();
            check(concurrent.Last!.Text.Contains("처리 중") && b.ReadView().Query == null, "busy event queued");
            ack.SetException(new IOException("ACK failed")); processing.GetAwaiter().GetResult();
            var replay = new FakeDiscordResponse(); session.Process(request, replay).GetAwaiter().GetResult();
            check(replay.Last!.Text.Contains("이미 접수") && b.ReadView().Query == null && h.StartCalls == 0, "duplicate event replayed");
        });
        test("discord acknowledged refresh is executed once on the framework and returns a usable screen", () => {
            var (b, h, _) = ListingWebEditTests.Ready(); using var cts = new CancellationTokenSource(); long tick = 1100;
            var session = new ListingDiscordSession(b, App, Owner, cts.Token, () => tick);
            var response = new FakeDiscordResponse();
            response.OnProgress = () => {
                for (var i = 0; i < 100; i++) {
                    tick += 500; h.Advance(tick); b.Update(h, tick);
                    var view = b.ReadView();
                    if (view.Operation != null ? view.Operation.State is not ("queued" or "running") : view.Query?.State is not ("queued" or "running")) break;
                }
            };
            session.Process(Input() with { Command = "pvkr" }, response).GetAwaiter().GetResult();
            var input = Input(Id(response.Last!, "apply"));
            response.OnDefer = () => check(h.Native.SubmitCalls == 0 && b.ReadView().Operation == null, "mutation happened before ACK");
            session.Process(input, response).GetAwaiter().GetResult();
            check(h.Native.SubmitCalls == 1 && response.Last!.Text.Contains("테스트 모집자") && response.Last.Components.Components.Count > 0, "refresh result unusable");
            response.OnDefer = null;
            session.Process(input, response).GetAwaiter().GetResult();
            check(response.Last!.Text.Contains("이미 접수") && h.Native.SubmitCalls == 1, "accepted refresh replayed");
        });
        test("discord unsupported setting is skipped and reports remaining successful changes", () => {
            var (b, h, _) = ListingWebEditTests.Ready(); h.Native.RuleEditAllowed = false;
            var p = new ListingDiscordPanel(b, App, Owner); var screen = p.Complete(1000).Screen!;
            screen = Field(p, screen, "unrestricted", "1"); screen = Text(p, screen, "description", "반영되는 소개말");
            var work = p.Handle(Input(Id(screen, "apply")), 1100).Work!; p.Start(work, 1100);
            for (long now = 1500; now < 30000; now += 500) {
                h.Advance(now); b.Update(h, now);
                if (p.Progress(work).Body is WebOperation { State: not ("queued" or "running") }) break;
            }
            var result = p.Progress(work); check(h.Native.SubmitCalls == 1 && h.Native.Target.Description == "반영되는 소개말", "unsupported setting aborted batch");
            check(ListingDiscordSession.Describe(result).Contains("[미적용] 제한 해제"), "partial result missing");
        });
    }
    private sealed class FakeDiscordResponse : IDiscordResponse
    {
        internal DiscordScreen? Last;
        internal bool FailDefer;
        internal Action? OnDefer;
        internal Task? Ack;
        internal Action? OnProgress;
        public Task Defer() { OnDefer?.Invoke(); return FailDefer ? Task.FromException(new IOException("ACK failure")) : Ack ?? Task.CompletedTask; }
        public Task Modal(Modal modal) => Task.CompletedTask;
        public Task Screen(DiscordScreen screen, bool replace, bool deferred) {
            Last = screen;
            if (deferred && (!screen.HasControls || ListingDiscordCard.Walk(screen.Components.Components)
                .OfType<ButtonComponent>().Any(b => b.CustomId.StartsWith(ListingDiscordPanel.ClosePrefix, StringComparison.Ordinal)) &&
                !ListingDiscordCard.Walk(screen.Components.Components).OfType<ButtonComponent>().Any(b => b.CustomId.EndsWith(":query", StringComparison.Ordinal)))) OnProgress?.Invoke();
            return Task.CompletedTask;
        }
    }
}
