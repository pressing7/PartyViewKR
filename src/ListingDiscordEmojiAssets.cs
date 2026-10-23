using Discord;

namespace PartyViewKR;

internal sealed record DiscordEmojiAsset(string Key, string FileName)
{
    internal string Name => "pvkr_" + Key;
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

    private static IReadOnlyList<DiscordEmojiAsset> Build()
    {
        (uint Id, string File)[] jobs = [
            (19,"pld"), (21,"war"), (32,"drk"), (37,"gnb"),
            (24,"whm"), (33,"ast"), (28,"sch"), (40,"sge"),
            (20,"mnk"), (22,"drg"), (30,"nin"), (34,"sam"), (39,"rpr"), (41,"vip"), (43,"bst"),
            (23,"brd"), (31,"mch"), (38,"dnc"), (25,"blm"), (27,"smn"), (35,"rdm"), (42,"pic"), (36,"blu"),
            (1,"gla"), (3,"mrd"), (6,"cnj"), (2,"pgl"), (4,"lnc"), (29,"rog"), (5,"arc"), (7,"thm"), (26,"acn")
        ];
        (string Key, string File)[] common = [
            ("anyone", "any"), ("role_th", "TH"), ("role_td", "TD"), ("role_hd", "HD"), ("role_thd", "THD")
        ];
        var kinds = jobs.Select(j => (Key: "job_" + j.Id, File: j.File)).Concat(common);
        return Array.AsReadOnly(kinds.SelectMany(k => new[] {
            new DiscordEmojiAsset(k.Key, k.File + ".png"),
            new DiscordEmojiAsset("checked_" + k.Key, "checked_" + k.File + ".png")
        }).ToArray());
    }

    internal static async Task Sync(ListingDiscordIcons icons,
        Func<CancellationToken, Task<IReadOnlyCollection<Emote>>> list,
        Func<string, Stream, CancellationToken, Task<Emote>> create,
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
            using var image = asset.Open();
            var emote = await create(asset.Name, image, cancellation);
            if (emote.Id == 0 || emote.Name != asset.Name) throw new InvalidOperationException("Unexpected emoji response");
            icons.Register(asset.Key, emote.Id);
            progress(++completed, All.Count);
        }
        // Never delete, rename or overwrite other application emojis.
    }
}
