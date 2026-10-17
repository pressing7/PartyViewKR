using System.Text;
using Discord;

namespace PartyViewKR;

internal sealed partial class ListingDiscordPanel
{
    // Results have their own card so listing headers/48-slot rosters cannot
    // consume their text budget. Keep every character, including long lines,
    // without splitting Unicode scalars or Markdown escape pairs.
    private static string[] PaginateResult(string value)
    {
        const int pageSize = 3200;
        var pages = new List<string>();
        var page = new StringBuilder();
        void Flush() { if (page.Length > 0) { pages.Add(page.ToString()); page.Clear(); } }
        var lines = value.Split('\n');
        for (var i = 0; i < lines.Length; i++) {
            var line = lines[i] + (i < lines.Length - 1 ? "\n" : "");
            var escaped = Escape(line);
            if (escaped.Length <= pageSize) {
                if (page.Length + escaped.Length > pageSize) Flush();
                page.Append(escaped);
            } else {
                Flush();
                foreach (var rune in line.EnumerateRunes()) {
                    var token = Escape(rune.ToString());
                    if (page.Length + token.Length > pageSize) Flush();
                    page.Append(token);
                }
            }
        }
        Flush(); return pages.ToArray();
    }

    private static DiscordScreen DrawResults(ListingDiscordCard card, Panel p)
    {
        card.Text($"## 최근 작업 결과 · {p.ResultPage + 1}/{p.ResultPages.Length}");
        card.Text(p.ResultPages[p.ResultPage]);
        if (p.ResultPages.Length > 1)
            card.Row(Button(p, "resultpage:" + (p.ResultPage - 1), "이전", disabled: p.ResultPage == 0),
                Button(p, "resultpage:" + (p.ResultPage + 1), "다음", disabled: p.ResultPage == p.ResultPages.Length - 1));
        return card.Build();
    }
}
