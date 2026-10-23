using Discord;
using PartyViewKR;

static class ListingDiscordEmojiTests
{
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        var assets = ListingDiscordEmojiAssets.All;
        var ids = assets.Select((a, i) => new KeyValuePair<string, ulong>(a.Key, 10000000000000000000ul + (ulong)i)).ToDictionary();
        var icons = new ListingDiscordIcons(ids);
        string Icon(string key) => $"<:{assets.Single(a => a.Key == key).Name}:{ids[key]}>";
        static Task<Emote> NoRename(ulong id, string name, CancellationToken ct) => throw new Exception("unexpected rename");
        static WebSlot Slot(bool member, bool anyone, params uint[] jobs) =>
            new(0, "1", member, "나이트", anyone, true, []) { JobId = 19, AllowedJobIds = jobs };

        test("discord emoji manifest embeds all 80 distinct PNGs with readable joined names", () => {
            check(assets.Count == 80 && assets.Select(a => a.Key).Distinct().Count() == 80 &&
                assets.Select(a => a.FileName).Distinct().Count() == 80 && assets.Select(a => a.Name).Distinct().Count() == 80,
                "missing or duplicate emoji asset");
            check(assets.Count(a => a.LegacyName != null) == 74, "legacy migration includes newly supplied artwork");
            Span<byte> header = stackalloc byte[24];
            foreach (var asset in assets) {
                using var stream = asset.Open();
                stream.ReadExactly(header);
                check(header[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }), "not a PNG: " + asset.FileName);
                check(stream.Length <= 256 * 1024 && asset.Name.Length is >= 2 and <= 32, "invalid upload asset");
                check(asset.Name.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_'), "invalid emoji name characters");
                if (!asset.Key.StartsWith("checked_"))
                    check(assets.Single(a => a.Key == "checked_" + asset.Key).Name == "join_" + asset.Name, "missing joined name");
            }
            check(ListingDiscordEmojiAssets.NameFor("job_19") == "paladin_job" &&
                ListingDiscordEmojiAssets.NameFor("job_1") == "gladiator_class" &&
                ListingDiscordEmojiAssets.NameFor("checked_role_th") == "join_tank_healer_role", "human-readable names lost");
            check(assets.Count(a => a.Name.EndsWith("_job")) == 46 && assets.Count(a => a.Name.EndsWith("_class")) == 18 &&
                assets.Count(a => a.Name.EndsWith("_role")) == 16, "job/class/role naming incorrect");
        });
        test("discord occupied TH stays TH until a new listing changes its flags to paladin", () => {
            var sample = ListingWebTests.Sample(); var rules = sample.Slots.JobRules;
            var th = rules.Choices.Where(j => j.Id is 19 or 24).Aggregate(0ul, (a, j) => a | j.Flag);
            var paladin = rules.Choices.Single(j => j.Id == 19).Flag;
            WebSlot Read(ulong flags, bool joined) {
                var slots = ListingSlots.Read(true, 1, joined ? 1 : 0, 1, false,
                    new byte[] { joined ? (byte)19 : (byte)0 }, new ulong[] { joined ? 500ul : 0ul },
                    new[] { flags }, _ => "나이트", rules);
                return ListingWebView.From(sample with { Slots = slots }, true, false)!.Groups[0].Slots[0];
            }
            check(icons.ForSlot(Read(th, false)) == Icon("role_th"), "empty TH incorrect");
            var occupied = Read(th, true);
            check(occupied.JobId == 19 && occupied.AllowedJobIds.SequenceEqual(new uint[] { 19, 24 }), "occupied flags lost in projection");
            check(icons.ForSlot(occupied) == Icon("checked_role_th"), "joining substituted participant's job for TH");
            check(icons.ForSlot(Read(paladin, true)) == Icon("checked_job_19"), "fresh listing did not replace TH");
            check(icons.ForSlot(occupied) == Icon("checked_role_th"), "old snapshot mutated");
        });
        test("discord role combinations include classes and need only one allowed job per role", () => {
            foreach (var (jobs, key) in new (uint[], string)[] {
                ([19, 24], "role_th"), ([1, 6], "role_th"), ([19, 25], "role_td"),
                ([3, 2], "role_td"), ([6, 26], "role_hd"), ([1, 6, 5], "role_thd"),
                ([19, 24, 43], "role_thd"), ([19, 21], "role_t"), ([1, 3], "role_t"),
                ([24, 28], "role_h"), ([6, 40], "role_h"), ([20, 22], "role_d"), ([2, 26], "role_d")
            }) foreach (var member in new[] { false, true })
                check(icons.ForSlot(Slot(member, false, jobs)) == Icon((member ? "checked_" : "") + key), "wrong combined role icon");
            check(icons.ForSlot(Slot(false, true, 19, 24, 25)) == Icon("anyone"), "complete runtime set not anyone");
            check(icons.ForSlot(Slot(true, true, 19, 24, 25)) == Icon("checked_anyone"), "occupied anyone became participant job");
        });
        test("discord single jobs map all supplied classes jobs and occupied variants", () => {
            foreach (var asset in assets.Where(a => a.Key.StartsWith("job_"))) {
                var id = uint.Parse(asset.Key[4..]);
                check(icons.ForSlot(Slot(false, false, id)) == Icon(asset.Key), "wrong empty job icon");
                check(icons.ForSlot(Slot(true, false, id)) == Icon("checked_" + asset.Key), "wrong occupied job icon");
            }
        });
        test("discord unknown rules and unprepared emoji never invent a participant icon", () => {
            foreach (var jobs in new uint[][] { [], [44], [19, 44] }) {
                check(icons.ForSlot(Slot(false, false, jobs)) == "🟨", "unknown rules guessed");
                check(icons.ForSlot(Slot(true, false, jobs)) == "🟦", "participant job wrongly substituted");
            }
            check(icons.ForSlot(Slot(false, true, 19) with { RulesComplete = false }) == "⬛", "unreadable rules became anyone");
            check(new ListingDiscordIcons().ForSlot(Slot(true, false, 19, 24)) == "🟦", "missing emoji dropped slot");
        });
        test("discord emoji setup reuses bot-owned names uploads only missing and reuses on reconnect", () => {
            var existing = assets.Take(10).Select(a => new Emote(ids[a.Key], a.Name)).ToList();
            existing.Add(new(42, "unrelated"));
            var registry = new ListingDiscordIcons(); var uploads = 0;
            Task<IReadOnlyCollection<Emote>> List(CancellationToken ct) => Task.FromResult<IReadOnlyCollection<Emote>>(existing.ToArray());
            Task<Emote> Create(string name, Stream stream, CancellationToken ct) {
                check(!existing.Any(e => e.Name == name) && stream.Length > 0, "duplicate or empty upload");
                var asset = assets.Single(a => a.Name == name); var emoji = new Emote(ids[asset.Key], name);
                existing.Add(emoji); uploads++; return Task.FromResult(emoji);
            }
            ListingDiscordEmojiAssets.Sync(registry, List, Create, NoRename, (_, _) => { }, default).GetAwaiter().GetResult();
            check(uploads == 70 && registry.Count == 80 && existing.Any(e => e.Id == 42), "existing emojis changed");
            var reconnect = new ListingDiscordIcons();
            ListingDiscordEmojiAssets.Sync(reconnect, List, Create, NoRename, (_, _) => { }, default).GetAwaiter().GetResult();
            check(uploads == 70 && reconnect.ForSlot(Slot(true, false, 19, 24)) == Icon("checked_role_th"), "reconnect uploaded duplicates or lost IDs");
        });
        test("discord emoji list failures never trigger uploads and separate bots never share IDs", () => {
            var uploads = 0; var registry = new ListingDiscordIcons();
            try {
                ListingDiscordEmojiAssets.Sync(registry, _ => throw new IOException("offline"),
                    (_, _, _) => { uploads++; throw new Exception("must not upload"); }, NoRename, (_, _) => { }, default).GetAwaiter().GetResult();
                throw new Exception("expected list failure");
            } catch (IOException) { }
            check(uploads == 0 && registry.Count == 0, "list failure treated as empty application");
            registry.Register("checked_role_th", 77);
            check(new ListingDiscordIcons().ForSlot(Slot(true, false, 19, 24)) == "🟦", "ID shared across bots");
        });
        test("discord cancellation keeps completed emoji registrations and prevents more uploads", () => {
            using var stop = new CancellationTokenSource(); var count = 0; var registry = new ListingDiscordIcons();
            try {
                ListingDiscordEmojiAssets.Sync(registry, _ => Task.FromResult<IReadOnlyCollection<Emote>>([]),
                    (name, _, _) => { count++; stop.Cancel(); return Task.FromResult(new Emote(77, name)); },
                    NoRename, (_, _) => { }, stop.Token).GetAwaiter().GetResult();
                throw new Exception("expected cancellation");
            } catch (OperationCanceledException) { }
            check(count == 1 && registry.Count == 1, "shutdown lost completed registration or continued uploading");
        });
        test("discord all 48 real emoji slots remain within message limits", () => {
            var host = new WebFakeHost { Listing = ListingWebTests.Sample(48) }; var bridge = new ListingWebBridge("test");
            bridge.Update(host, 1000);
            var panel = new ListingDiscordPanel(bridge, 500, 600, icons); var screen = panel.Complete(1000).Screen!;
            check(screen.Text.Split("<:").Length - 1 == 48, "some party slots omitted");
            check(screen.Text.Length <= 4000 && ListingDiscordCard.Walk(screen.Components.Components).Count() <= 40, "48 emojis exceed Discord limits");
        });
        test("discord emoji migration renames all legacy entries in place and uploads only six role icons", () => {
            var existing = assets.Where(a => a.LegacyName != null).Select(a => new Emote(ids[a.Key], a.LegacyName!)).ToList();
            existing.Add(new(42, "pvkr_unrelated"));
            var registry = new ListingDiscordIcons(); var uploads = 0; var renames = 0;
            Task<IReadOnlyCollection<Emote>> List(CancellationToken ct) => Task.FromResult<IReadOnlyCollection<Emote>>(existing.ToArray());
            Task<Emote> Create(string name, Stream stream, CancellationToken ct) {
                var asset = assets.Single(a => a.Name == name);
                check(asset.LegacyName == null, "re-uploaded existing image");
                var emote = new Emote(ids[asset.Key], name); existing.Add(emote); uploads++; return Task.FromResult(emote);
            }
            Task<Emote> Rename(ulong id, string name, CancellationToken ct) {
                var index = existing.FindIndex(e => e.Id == id);
                check(index >= 0 && assets.Single(a => a.Name == name).LegacyName == existing[index].Name, "renamed unrelated emoji");
                var emote = new Emote(id, name); existing[index] = emote; renames++; return Task.FromResult(emote);
            }
            ListingDiscordEmojiAssets.Sync(registry, List, Create, Rename, (_, _) => { }, default).GetAwaiter().GetResult();
            check(renames == 74 && uploads == 6 && registry.Count == 80 && existing.Any(e => e.Id == 42 && e.Name == "pvkr_unrelated"), "migration counts or unrelated emoji changed");
            foreach (var asset in assets) check(existing.Single(e => e.Name == asset.Name).Id == ids[asset.Key], "emoji ID changed");
            ListingDiscordEmojiAssets.Sync(new(), List, Create, NoRename, (_, _) => { }, default).GetAwaiter().GetResult();
            check(uploads == 6, "reconnect re-uploaded migrated images");
        });
        test("discord emoji migration keeps current names and resumes after a partial rename failure", () => {
            var existing = assets.Select((a, i) => new Emote(ids[a.Key], i < 4 ? a.Name : a.LegacyName ?? a.Name)).ToList();
            existing.Add(new(42, assets[0].LegacyName!)); // Current spelling wins over a duplicate legacy entry.
            var registry = new ListingDiscordIcons(); var renames = 0; var fail = true;
            Task<IReadOnlyCollection<Emote>> List(CancellationToken ct) => Task.FromResult<IReadOnlyCollection<Emote>>(existing.ToArray());
            Task<Emote> Create(string name, Stream stream, CancellationToken ct) => throw new Exception("unexpected upload during migration");
            Task<Emote> Rename(ulong id, string name, CancellationToken ct) {
                check(id != 42, "renamed duplicate legacy entry despite current name");
                if (++renames == 2 && fail) throw new IOException("offline");
                var emote = new Emote(id, name); existing[existing.FindIndex(e => e.Id == id)] = emote; return Task.FromResult(emote);
            }
            try {
                ListingDiscordEmojiAssets.Sync(registry, List, Create, Rename, (_, _) => { }, default).GetAwaiter().GetResult();
                throw new Exception("expected rename failure");
            } catch (IOException) { }
            check(registry.Count == 11, "partial migration progress lost"); // 4 current + 6 new roles + 1 renamed.
            fail = false; var retry = new ListingDiscordIcons();
            ListingDiscordEmojiAssets.Sync(retry, List, Create, Rename, (_, _) => { }, default).GetAwaiter().GetResult();
            check(retry.Count == 80 && renames == 71 && existing.Single(e => e.Id == 42).Name == assets[0].LegacyName,
                "retry lost IDs, repeated completed renames, or changed duplicate entry");
        });
        test("discord longest joined role names show all 48 occupied slots without fallback squares", () => {
            var sample = ListingWebTests.Sample(48); var rules = sample.Slots.JobRules;
            var thd = rules.Choices.Where(j => j.Id is 19 or 24 or 25).Aggregate(0ul, (a, j) => a | j.Flag);
            var slots = ListingSlots.Read(true, 48, 48, 6, true, Enumerable.Repeat((byte)19, 48).ToArray(),
                Enumerable.Range(1, 48).Select(i => (ulong)i).ToArray(), Enumerable.Repeat(thd, 48).ToArray(), _ => "나이트", rules);
            var host = new WebFakeHost { Listing = sample with { Slots = slots } }; var bridge = new ListingWebBridge("test");
            bridge.Update(host, 1000);
            var panel = new ListingDiscordPanel(bridge, 500, 600, icons); var screen = panel.Complete(1000).Screen!;
            check(screen.Text.Split("<:join_tank_healer_dealer_role:").Length - 1 == 48, "long names omitted slots");
            check(!screen.Text.Contains("🟨") && !screen.Text.Contains("🟦"), "fallback squares shown for prepared roles");
            check(screen.Text.Length <= 4000 && ListingDiscordCard.Walk(screen.Components.Components).Count() <= 40, "long names exceed Discord limits");
        });
    }
}
