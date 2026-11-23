using CoreText;
using Foundation;

namespace Plumix.Impeller.iOS;

// C#-only host infrastructure; no Dart parity source. The engine's iOS font manager (Skia's CoreText manager) answers
// "CupertinoSystemText"/"CupertinoSystemDisplay" and the .SF names with the system UI font and falls back through
// CoreText's cascade. The prototype reads the font files CoreText reports (the system font is variable: weights come
// from the shaper's wght instancing) and registers them under those names, plus a few families and fallbacks.

/// <summary>Feeds the system's fonts, located through CoreText, to the Impeller text shaper.</summary>
internal static class IosFonts
{
    // Families the framework asks for on iOS that mean the system UI font.
    private static readonly string[] SystemFamilies =
    [
        "CupertinoSystemText", "CupertinoSystemDisplay", ".SF UI Text", ".SF UI Display", ".SF Pro Text",
        ".SF Pro Display", ".AppleSystemUIFont", "sans-serif", "Roboto",
    ];

    /// <summary>The system UI font's file, for the shaper's default face.</summary>
    public static string? SystemFontPath() => PathOf(new CTFont(CTFontUIFontType.System, 17, null!));

    public static void AddSystemFonts(ImpellerTextShaper shaper)
    {
        string? upright = SystemFontPath();
        string? italic = PathOf(new CTFont(CTFontUIFontType.System, 17, null!)
            .WithSymbolicTraits(17, CTFontSymbolicTraits.Italic, CTFontSymbolicTraits.Italic));
        foreach (string family in SystemFamilies)
        {
            if (upright is not null)
            {
                shaper.AddSystemFace(family, upright, 0, 400, italic: false, actualWeight: null);
            }

            if (italic is not null && italic != upright)
            {
                shaper.AddSystemFace(family, italic, 0, 400, italic: true, actualWeight: null);
            }
        }

        AddNamed(shaper, "monospace", "Menlo");
        AddNamed(shaper, "Menlo", "Menlo");
        AddNamed(shaper, "Courier", "Courier");
        AddNamed(shaper, "serif", "Times New Roman");
        AddNamed(shaper, "Helvetica", "Helvetica");
        string[] fallbacks = ["Apple Color Emoji", "PingFang SC", "Hiragino Sans", "Apple SD Gothic Neo"];
        foreach (string fallback in fallbacks)
        {
            if (PathOf(new CTFont(fallback, 17)) is { } path)
            {
                shaper.AddFallback(path, 0);
            }
        }
    }

    private static void AddNamed(ImpellerTextShaper shaper, string family, string name)
    {
        if (PathOf(new CTFont(name, 17)) is { } path)
        {
            shaper.AddSystemFace(family, path, 0, 400, italic: false, actualWeight: null);
        }
    }

    // The file behind a CoreText font (kCTFontURLAttribute).
    private static string? PathOf(CTFont? font) =>
        (font?.GetFontDescriptor().GetAttribute(CTFontDescriptorAttributeKey.Url) as NSUrl)?.Path;
}
