using System.Text;

namespace PartyViewKR;

internal sealed class ListingDescriptionDraft(RefreshTarget original)
{
    internal RefreshTarget Original { get; } = original;
    internal string Text { get; set; } = original.Description;
    internal ListingDescriptionEdit Request => new(Original, Text);
}

internal sealed record ListingDescriptionEdit(RefreshTarget Original, string Text)
{
    // Detailed.Comment / RecruitmentSub.Comment both have 192 bytes. The
    // game's input may impose a stricter limit; check it again before SetText.
    internal const int MaxBytes = 192;
    private static readonly UTF8Encoding Utf8 = new(false, true);
    internal static string Normalize(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
    internal string Value => Normalize(Text);
    internal bool Changed => !ListingIdentity.SameText(Original.Description, Value);
    internal int ByteCount { get { try { return Utf8.GetByteCount(Value); } catch (EncoderFallbackException) { return -1; } } }
    internal static string Validate(string text)
    {
        var value = Normalize(text);
        int bytes;
        try { bytes = Utf8.GetByteCount(value); } catch (EncoderFallbackException) { return "입력할 수 없는 문자가 포함되어 있습니다."; }
        return value.Any(c => c == '\ufffd' || char.IsControl(c) && c != '\n')
            ? "입력할 수 없는 문자가 포함되어 있습니다."
            : bytes > MaxBytes ? "소개말의 최대 입력 길이를 초과했습니다." : "";
    }
    internal string ValidationError => Validate(Text);
    internal bool Valid => ListingJobEdit.SupportsSettings(Original) && Changed && ValidationError.Length == 0;

    internal bool MatchesOriginal(RefreshTarget current) => Valid && ListingJobEdit.SupportsSettings(current) &&
        current.ListingId == Original.ListingId && RefreshEvidence.SameRecruitment(Original, current) &&
        ListingJobEdit.SameSlots(Original.Slots, current.Slots);

    internal RefreshTarget Expected(RefreshTarget before)
    {
        if (!Valid) throw new InvalidDataException("invalid_description_edit");
        return before with { Description = Value };
    }

    internal bool MatchesResult(RefreshTarget before, RefreshTarget after) => Valid &&
        ListingJobEdit.SupportsSettings(before) && ListingJobEdit.SupportsSettings(after) &&
        RefreshEvidence.SameRecruitment(Expected(before), after) && ListingJobEdit.SameSlots(before.Slots, after.Slots);
}
