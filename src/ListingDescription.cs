namespace PartyViewKR;

internal readonly record struct ListingDescription(bool Readable, string Text, string Source,
    bool NativeAgrees, bool EmptyPlaceholder)
{
    private const string KoreanEmptyPlaceholder = "소개말 없음";

    internal static ListingDescription Resolve(bool nodeRead, string nodeText,
        bool bufferRead, string bufferText, bool nativeRead, string nativeText)
    {
        var readable = nodeRead || bufferRead;
        var text = nodeRead ? nodeText : bufferRead ? bufferText : "";
        var source = nodeRead ? "DescriptionTextNode" : "DescriptionString";
        if (readable && nativeRead && ListingIdentity.SameText(text, nativeText))
            return new(true, text, source, true, false);

        // The rendered node is not the authored comment. Accept inserted line
        // breaks only when the independent addon buffer and native comment agree.
        // Keep the canonical buffer text so refresh never submits display wrapping.
        if (nodeRead && bufferRead && nativeRead && ListingIdentity.SameText(bufferText, nativeText) &&
            OnlyAddedLineBreaks(nodeText, bufferText))
            return new(true, bufferText, "DescriptionStringWrappedNode", true, false);

        // The KR detail UI stores this placeholder in both the node and string buffer.
        // A literal authored comment already matched above and must remain nonempty.
        if (nativeRead && ListingIdentity.SameText(nativeText, "") && nodeRead && bufferRead &&
            ListingIdentity.SameText(nodeText, KoreanEmptyPlaceholder) &&
            ListingIdentity.SameText(bufferText, KoreanEmptyPlaceholder))
            return new(true, "", "KoreanEmptyPlaceholder", true, true);

        // Do not treat arbitrary displayed text as empty based only on nativeText.
        // Require the addon's separate string buffer to independently confirm empty.
        // Keep the canonical comment empty all the way through editor validation.
        if (nativeRead && bufferRead && ListingIdentity.SameText(nativeText, "") &&
            ListingIdentity.SameText(bufferText, ""))
            return new(true, "", "DescriptionStringEmpty", true,
                nodeRead && !ListingIdentity.SameText(nodeText, ""));

        return new(readable, text, source, false, false);
    }

    private static string NormalizeLines(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n').Trim();

    private static bool OnlyAddedLineBreaks(string displayed, string authored)
    {
        displayed = NormalizeLines(displayed); authored = NormalizeLines(authored);
        if (authored.Length == 0) return false;
        var position = 0; var added = 0;
        foreach (var c in displayed)
        {
            if (position < authored.Length && c == authored[position]) { position++; continue; }
            if (c != '\n') return false;
            added++;
        }
        return position == authored.Length && added > 0;
    }

    internal static string CompareSources(bool nodeRead, string nodeText,
        bool bufferRead, string bufferText, bool nativeRead, string nativeText) =>
        $"descriptionNodeNativeDifference={(nodeRead && nativeRead ? Difference(nodeText, nativeText) : "unreadable")}; " +
        $"descriptionBufferNativeDifference={(bufferRead && nativeRead ? Difference(bufferText, nativeText) : "unreadable")}; " +
        $"descriptionNodeLineBreaks={NormalizeLines(nodeText).Count(c => c == '\n')}; " +
        $"descriptionBufferLineBreaks={NormalizeLines(bufferText).Count(c => c == '\n')}; " +
        $"descriptionNativeLineBreaks={NormalizeLines(nativeText).Count(c => c == '\n')}";

    private static string Difference(string displayed, string authored)
    {
        displayed = NormalizeLines(displayed); authored = NormalizeLines(authored);
        var index = 0;
        while (index < displayed.Length && index < authored.Length && displayed[index] == authored[index]) index++;
        if (index == displayed.Length && index == authored.Length) return "same";
        // UTF-16 units, matching the existing string.Length diagnostics. Do not
        // include whole recruitment comments in the diagnostic clipboard output.
        return $"index:{index},ui:{(index < displayed.Length ? $"0x{(int)displayed[index]:X4}" : "end")}," +
            $"native:{(index < authored.Length ? $"0x{(int)authored[index]:X4}" : "end")}";
    }
}
