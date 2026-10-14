namespace PartyViewKR;

internal readonly record struct ListingDescription(bool Readable, string Text, string Source,
    bool NativeAgrees, bool EmptyPlaceholder)
{
    internal static ListingDescription Resolve(bool nodeRead, string nodeText,
        bool bufferRead, string bufferText, bool nativeRead, string nativeText)
    {
        var readable = nodeRead || bufferRead;
        var text = nodeRead ? nodeText : bufferRead ? bufferText : "";
        var source = nodeRead ? "DescriptionTextNode" : "DescriptionString";
        if (readable && nativeRead && ListingIdentity.SameText(text, nativeText))
            return new(true, text, source, true, false);

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
