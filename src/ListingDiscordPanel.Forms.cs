using System.Text.Json.Nodes;
using Discord;

namespace PartyViewKR;

internal sealed partial class ListingDiscordPanel
{
    private static string[] FormFields(string form) => form switch {
        "description" => ["description"], "settings" => ["objective", "completion", "itemLevel", "loot", .. ConditionFlags, .. RuleFlags],
        "private" => ["private"], _ => []
    };
    private static bool FormSupported(Panel p, string form) => FormFields(form).Any(p.View.Editor!.Supported.Contains);
    private static Modal BuildForm(Panel p, string form)
    {
        var modal = new ModalBuilder().WithTitle(form switch {
            "description" => "소개말 수정", "settings" => "모집 설정 함께 수정", _ => "비공개 설정"
        }).WithCustomId(Id(p, "submit:" + form));
        var supported = p.View.Editor!.Supported;
        if (form == "description")
            modal.AddTextInput("소개말 · UTF-8 192바이트까지", "value", TextInputStyle.Paragraph,
                maxLength: 192, required: false, value: Value(p, "description")?.GetValue<string>() ?? "");
        else if (form == "private") {
            modal.AddCheckBox("비공개 모집", "enabled", Enabled(p, "private"));
            modal.AddTextInput("비밀번호 네 자리 · 비워두면 기존 값 유지", "password", TextInputStyle.Short,
                maxLength: 4, required: false, placeholder: "0000~9999 · 새로 켤 때는 반드시 입력");
        } else {
            // Three radio groups, one numeric input and one checkbox group fit
            // in a single five-component modal. Description/PIN stay separate.
            string[] radioKeys = ["objective", "completion", "loot"];
            foreach (var key in radioKeys.Where(supported.Contains)) {
                var radio = new RadioGroupBuilder().WithCustomId(key).WithRequired(true);
                foreach (var option in Options(key)) radio.AddOption(option.Label, option.Value, isDefault: Value(p, key)?.ToString() == option.Value);
                modal.AddRadioGroup(Label(key), radio);
            }
            if (supported.Contains("itemLevel"))
                modal.AddTextInput("평균 아이템 레벨 · 1~999, 제한 없음은 0", "itemLevel", TextInputStyle.Short,
                    maxLength: 3, required: true, value: Value(p, "itemLevel")?.ToString() ?? "0");
            var flags = ConditionFlags.Concat(RuleFlags).Where(supported.Contains).ToArray();
            if (flags.Length > 0) {
                var checks = new CheckboxGroupBuilder().WithCustomId("toggles").WithRequired(false).WithMinValues(0).WithMaxValues(flags.Length);
                foreach (var key in flags) checks.AddOption(Label(key), key, defaultState: Enabled(p, key));
                modal.AddCheckBoxGroup("사용할 옵션", checks);
            }
        }
        return modal.Build();
    }

    // Validate the entire form before changing the draft. Missing checkbox lists
    // are malformed, while an explicit empty list means all options are off.
    private static bool ReadForm(Panel p, string form, DiscordInput input, out List<JsonObject> changes, out string error)
    {
        changes = []; error = "입력값을 다시 확인해 주세요.";
        var supported = p.View.Editor!.Supported;
        if (form == "description") {
            if (!Keys(input.Fields, ["value"]) || input.Choices.Count != 0 || input.Checks.Count != 0) return false;
            error = ListingDescriptionEdit.Validate(input.Fields["value"]);
            if (error.Length > 0) return false;
            changes.Add(new() { ["field"] = "description", ["value"] = ListingDescriptionEdit.Normalize(input.Fields["value"]) });
        } else if (form == "private") {
            if (!Keys(input.Fields, ["password"]) || !Keys(input.Checks, ["enabled"]) || input.Choices.Count != 0) return false;
            var enabled = input.Checks["enabled"]; var password = input.Fields["password"];
            if (!enabled) changes.Add(new() { ["field"] = "private", ["value"] = false });
            else if (password.Length > 0) {
                if (!ListingPrivate.TryPassword(password, out _)) { error = "비밀번호는 0000~9999의 숫자 네 자리입니다."; return false; }
                changes.Add(new() { ["field"] = "private", ["value"] = true, ["password"] = password });
            } else if (p.Changes.TryGetValue("private", out var pending) && pending.ContainsKey("password")) {
                // Keep the PIN already supplied to this draft; never echo it.
            } else if (Original(p, "private")?.GetValue<bool>() == true)
                changes.Add(new() { ["field"] = "private", ["value"] = true });
            else { error = "비공개를 새로 켤 때는 비밀번호 네 자리가 필요합니다."; return false; }
        } else {
            var scalarKeys = new[] { "objective", "completion", "itemLevel", "loot" }.Where(supported.Contains).ToArray();
            var flags = ConditionFlags.Concat(RuleFlags).Where(supported.Contains).ToArray();
            if (!Keys(input.Fields, scalarKeys) || !Keys(input.Choices, flags.Length > 0 ? ["toggles"] : []) || input.Checks.Count != 0) return false;
            foreach (var key in scalarKeys) {
                var text = input.Fields[key];
                if (key == "itemLevel") {
                    if (text.Length is 0 or > 3 || text.Any(c => c is < '0' or > '9') || !int.TryParse(text, out var level) || level > 999) {
                        error = "평균 아이템 레벨은 1~999, 제한 없음은 0을 입력해 주세요."; return false;
                    }
                    changes.Add(new() { ["field"] = key, ["value"] = level });
                } else {
                    if (!Options(key).Any(o => o.Value == text)) return false;
                    changes.Add(new() { ["field"] = key, ["value"] = int.Parse(text, System.Globalization.CultureInfo.InvariantCulture) });
                }
            }
            if (flags.Length > 0) {
                var selected = input.Choices["toggles"];
                if (selected.Distinct().Count() != selected.Length || selected.Any(k => !flags.Contains(k))) return false;
                changes.AddRange(flags.Select(k => FlagChange(k, selected.Contains(k))));
            }
        }
        error = ""; return true;
    }
    private static bool Keys<T>(Dictionary<string, T> values, string[] expected) => values.Count == expected.Length && expected.All(values.ContainsKey);
}
