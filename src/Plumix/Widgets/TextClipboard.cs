// C#-only compatibility cache. Framework editing and selection use UI.Clipboard.

namespace Plumix.Widgets;

public static class TextClipboard
{
    private static string _cachedText = string.Empty;

    public static string CurrentText => _cachedText;

    public static void SetText(string? text)
    {
        _cachedText = text ?? string.Empty;
    }

    public static string? GetText()
    {
        return _cachedText;
    }

    internal static void ResetForTests()
    {
        _cachedText = string.Empty;
    }
}
