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
}
