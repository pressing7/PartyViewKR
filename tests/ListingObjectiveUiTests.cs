using PartyViewKR;

static class ListingObjectiveUiTests
{
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        ObjectiveUiState Closed(int index, byte stored) => new(index, index, stored, false, false, false, 4);

        test("manual KR trace transitions require opening, item click, selection, then settled UI and draft", () => {
            // Observed 0.3.17 trace: callbacks 56,87,120,147 changed stored
            // values 1->2->4->8->1. Each ListItemClick returned with OLD indices;
            // ListItemHighlight arrived with NEW indices and updated the draft.
            // These snapshots test sequencing, not execution of native methods.
            var transitions = new[] { (0, (byte)1, 1, (byte)2), (1, (byte)2, 2, (byte)4),
                (2, (byte)4, 3, (byte)8), (3, (byte)8, 0, (byte)1) };
            foreach (var (before, oldValue, after, newValue) in transitions)
            {
                var ui = new ListingObjectiveUi(before, after); var old = Closed(before, oldValue);
                check(ui.Next(old, 1000) == ObjectiveUiAction.Open, "missing initial open");
                check(ui.Next(old with { Open = true, Pending = true, Checked = true }, 1010) == ObjectiveUiAction.Wait, "clicked before open settled");
                check(ui.Next(old with { Open = true, Checked = true }, 1050) == ObjectiveUiAction.Wait, "no opening delay");
                check(ui.Next(old with { Open = true, Checked = true }, 1500) == ObjectiveUiAction.ClickItem, "open not followed by click");
                check(ui.Next(old with { Pending = true, Checked = true }, 1500) == ObjectiveUiAction.SelectItem, "click did not precede selection");
                var selected = Closed(after, newValue);
                check(ui.Next(selected with { Pending = true, Checked = true }, 1500) == ObjectiveUiAction.Wait, "submitted during close transition");
                check(ui.Next(selected, 1550) == ObjectiveUiAction.Wait, "no result settling delay");
                check(ui.Next(selected, 2000) == ObjectiveUiAction.Complete, "matching result not verified");
                check(ui.Next(selected, 2500) == ObjectiveUiAction.Complete, "completion sent another action");
            }
        });
        test("dropdown that never opens times out without clicking or retrying", () => {
            var ui = new ListingObjectiveUi(0, 3); var old = Closed(0, 1);
            check(ui.Next(old, 1000) == ObjectiveUiAction.Open, "open missing");
            foreach (var t in new[] { 1500, 2000, 3000, 4000 })
                check(ui.Next(old, t) == ObjectiveUiAction.Wait, "opening was retried or click sent");
            check(ui.Next(old, 4001) == ObjectiveUiAction.Failed && ui.Check == "dropdown_open_timeout", "opening not bounded");
            check(ui.Next(old with { Open = true, Checked = true }, 4500) == ObjectiveUiAction.Failed, "failed run restarted itself");
        });
        test("UI-only, draft-only and unsettled selection never qualify for submission", () => {
            var original = Closed(1, 2); var correct = Closed(3, 8);
            foreach (var wrong in new[] { correct with { Stored = 2 }, original with { Stored = 8 },
                correct with { Pending = true }, correct with { Checked = true } })
            {
                var ui = new ListingObjectiveUi(1, 3);
                ui.Next(original, 1000); ui.Next(original with { Open = true, Checked = true }, 1500);
                ui.Next(original with { Pending = true, Checked = true }, 1500);
                check(ui.Next(wrong, 2000) == ObjectiveUiAction.Wait, "partial result accepted/retried");
                check(ui.Next(wrong, 4501) == ObjectiveUiAction.Failed, "partial result never stopped");
            }
        });
        test("wrong purpose from reported failure cannot pass the UI sequence", () => {
            var original = Closed(1, 2); var ui = new ListingObjectiveUi(1, 3);
            ui.Next(original, 1000); ui.Next(original with { Open = true, Checked = true }, 1500);
            ui.Next(original with { Pending = true, Checked = true }, 1500);
            check(ui.Next(Closed(1, 4), 2000) == ObjectiveUiAction.Failed, "Practice draft accepted for Loot");
            check(ui.Next(Closed(3, 8), 2500) == ObjectiveUiAction.Failed, "failure resumed on late change");
        });
        test("unexpected click behavior stops before sending a highlight or fallback callback", () => {
            var old = Closed(0, 1);
            foreach (var wrong in new[] { old with { Open = true, Checked = true }, Closed(2, 4) })
            {
                var ui = new ListingObjectiveUi(0, 2); ui.Next(old, 1000);
                check(ui.Next(old with { Open = true, Checked = true }, 1500) == ObjectiveUiAction.ClickItem, "click missing");
                check(ui.Next(wrong, 1500) == ObjectiveUiAction.Failed, "unobserved click result continued");
            }
        });
        test("stale, open, unknown or inconsistent starting controls send no events", () => {
            var old = Closed(0, 1);
            foreach (var wrong in new[] { old with { Open = true }, old with { Pending = true }, old with { Checked = true },
                old with { Count = 5 }, old with { ListIndex = 1 }, old with { Stored = 0 }, Closed(1, 2) })
                check(new ListingObjectiveUi(0, 3).Next(wrong, 1000) == ObjectiveUiAction.Failed, "unverified control opened");
            foreach (var requested in new[] { -1, 0, 4 })
                check(new ListingObjectiveUi(0, requested).Next(old, 1000) == ObjectiveUiAction.Failed, "invalid/no-op request opened");
        });
        test("user changing purpose while opening or after verification stops the sequence", () => {
            var old = Closed(0, 1); var selected = Closed(2, 4);
            var opening = new ListingObjectiveUi(0, 2); opening.Next(old, 1000);
            check(opening.Next(selected with { Open = true, Checked = true }, 1500) == ObjectiveUiAction.Failed, "manual change overwritten");
            var ui = new ListingObjectiveUi(0, 2); ui.Next(old, 1000);
            ui.Next(old with { Open = true, Checked = true }, 1500); ui.Next(old with { Pending = true, Checked = true }, 1500);
            check(ui.Next(selected, 2000) == ObjectiveUiAction.Complete, "not verified");
            check(ui.Next(selected with { Open = true }, 2100) == ObjectiveUiAction.Failed, "reopened dropdown accepted");
        });
    }
}
