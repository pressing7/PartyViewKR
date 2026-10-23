using Discord;

namespace PartyViewKR;

internal sealed record DiscordEmojiAsset(string Key, string FileName, string Name, string? LegacyName)
{
    internal Stream Open()
    {
        var stream = typeof(DiscordEmojiAsset).Assembly.GetManifestResourceStream("PartyViewKR.DiscordIcons." + FileName)
            ?? throw new InvalidOperationException("Missing embedded emoji asset");
        if (stream.Length is <= 0 or > 256 * 1024) {
            stream.Dispose(); throw new InvalidOperationException("Invalid emoji asset size");
        }
        return stream;
    }
}

internal static class ListingDiscordEmojiAssets
{
    // Preserve the supplied PNGs (including transparent corners and check marks).
    // Discord owns the uploaded copy; no hosted image URL or user file is needed.
    internal static IReadOnlyList<DiscordEmojiAsset> All { get; } = Build();
    private static readonly Dictionary<string, DiscordEmojiAsset> ByKey = All.ToDictionary(a => a.Key);
    internal static string NameFor(string key) => ByKey.TryGetValue(key, out var asset) ? asset.Name : "pvkr_" + key;

    private static IReadOnlyList<DiscordEmojiAsset> Build()
    {
        (uint Id, string File, string Name)[] jobs = [
            (19,"pld","paladin_job"), (21,"war","warrior_job"), (32,"drk","dark_knight_job"), (37,"gnb","gunbreaker_job"),
            (24,"whm","white_mage_job"), (33,"ast","astrologian_job"), (28,"sch","scholar_job"), (40,"sge","sage_job"),
            (20,"mnk","monk_job"), (22,"drg","dragoon_job"), (30,"nin","ninja_job"), (34,"sam","samurai_job"),
            (39,"rpr","reaper_job"), (41,"vip","viper_job"), (43,"bst","beastmaster_job"),
            (23,"brd","bard_job"), (31,"mch","machinist_job"), (38,"dnc","dancer_job"),
            (25,"blm","black_mage_job"), (27,"smn","summoner_job"), (35,"rdm","red_mage_job"),
            (42,"pic","pictomancer_job"), (36,"blu","blue_mage_job"),
            (1,"gla","gladiator_class"), (3,"mrd","marauder_class"), (6,"cnj","conjurer_class"),
            (2,"pgl","pugilist_class"), (4,"lnc","lancer_class"), (29,"rog","rogue_class"),
            (5,"arc","archer_class"), (7,"thm","thaumaturge_class"), (26,"acn","arcanist_class")
        ];
        (string Key, string File, string Name, bool Legacy)[] common = [
            ("anyone", "any", "any_role", true),
            ("role_th", "TH", "tank_healer_role", true), ("role_td", "TD", "tank_dealer_role", true),
            ("role_hd", "HD", "healer_dealer_role", true), ("role_thd", "THD", "tank_healer_dealer_role", true),
            ("role_t", "tanker", "tank_role", false), ("role_h", "healer", "healer_role", false),
            ("role_d", "dealer", "dealer_role", false)
        ];
        var kinds = jobs.Select(j => (Key: "job_" + j.Id, j.File, j.Name, Legacy: true)).Concat(common);
        return Array.AsReadOnly(kinds.SelectMany(k => new[] {
            new DiscordEmojiAsset(k.Key, k.File + ".png", k.Name, k.Legacy ? "pvkr_" + k.Key : null),
            new DiscordEmojiAsset("checked_" + k.Key, "checked_" + k.File + ".png", "join_" + k.Name,
                k.Legacy ? "pvkr_checked_" + k.Key : null)
        }).ToArray());
    }

    internal static async Task Sync(ListingDiscordIcons icons,
        Func<CancellationToken, Task<IReadOnlyCollection<Emote>>> list,
        Func<string, Stream, CancellationToken, Task<Emote>> create,
        Func<ulong, string, CancellationToken, Task<Emote>> rename,
        Action<int, int> progress, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        // A failed list must not be treated as an empty list (duplicate uploads).
        var existing = (await list(cancellation)).Where(e => e.Id != 0)
            .GroupBy(e => e.Name, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.OrderBy(e => e.Id).First(), StringComparer.Ordinal);
        var missing = new List<DiscordEmojiAsset>();
        var completed = 0;
        foreach (var asset in All) {
            if (existing.TryGetValue(asset.Name, out var emote)) { icons.Register(asset.Key, emote.Id); completed++; }
            else missing.Add(asset);
        }
        progress(completed, All.Count);
        foreach (var asset in missing) {
            cancellation.ThrowIfCancellationRequested();
            Emote emote;
            if (asset.LegacyName is { } legacy && existing.TryGetValue(legacy, out var old)) {
                // Migrate only exact names emitted by PVKR 0.3.45. Preserve IDs
                // so already-posted cards still reference the same images.
                emote = await rename(old.Id, asset.Name, cancellation);
                if (emote.Id != old.Id) throw new InvalidOperationException("Unexpected renamed emoji ID");
            } else {
                using var image = asset.Open();
                emote = await create(asset.Name, image, cancellation);
            }
            if (emote.Id == 0 || emote.Name != asset.Name) throw new InvalidOperationException("Unexpected emoji response");
            icons.Register(asset.Key, emote.Id);
            progress(++completed, All.Count);
        }
        // Never delete or overwrite other application emojis; current names win
        // if both a migrated name and a legacy name already exist.
    }
}
