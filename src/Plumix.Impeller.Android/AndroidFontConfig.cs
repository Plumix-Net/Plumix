using System.Xml.Linq;
using Plumix.Impeller;

namespace Plumix.Impeller.Android;

// C#-only host infrastructure; no Dart parity source. Android's font configuration (/system/etc/fonts.xml),
// read the way Skia's Android font manager reads it: named families (sans-serif, serif, monospace...) and
// their aliases answer a requested family by name, and the unnamed families form the system fallback chain,
// in order, searched for a code point the requested family lacks. The prototype reads only the first upright
// font of each fallback family.

/// <summary>Feeds Android's system fonts to the Impeller text shaper.</summary>
internal static class AndroidFontConfig
{
    private const string ConfigPath = "/system/etc/fonts.xml";
    private const string FontDirectory = "/system/fonts/";

    public static void AddSystemFonts(ImpellerTextShaper shaper)
    {
        if (!File.Exists(ConfigPath))
        {
            return;
        }

        XDocument config = XDocument.Load(ConfigPath);
        foreach (XElement family in config.Root!.Elements("family"))
        {
            if ((string?)family.Attribute("name") is { } name)
            {
                foreach (XElement font in family.Elements("font"))
                {
                    if (FileOf(font) is { } file)
                    {
                        int weight = int.TryParse((string?)font.Attribute("weight"), out int w) ? w : 400;
                        bool italic = (string?)font.Attribute("style") == "italic";
                        // A font with <axis> children is a variable instance; the prototype cannot apply axes.
                        int? actualWeight = font.Elements("axis").Any() ? null : weight;
                        shaper.AddSystemFace(name, FontDirectory + file, IndexOf(font), weight, italic, actualWeight);
                    }
                }

                continue;
            }

            XElement? fallback = family.Elements("font").FirstOrDefault(f => (string?)f.Attribute("style") != "italic")
                ?? family.Elements("font").FirstOrDefault();
            if (fallback is not null && FileOf(fallback) is { } fallbackFile)
            {
                shaper.AddFallback(FontDirectory + fallbackFile, IndexOf(fallback));
            }
        }

        foreach (XElement alias in config.Root!.Elements("alias"))
        {
            if ((string?)alias.Attribute("name") is { } name && (string?)alias.Attribute("to") is { } to)
            {
                shaper.AddSystemAlias(name, to);
            }
        }
    }

    // The file name is the element's own text, before any <axis> children.
    private static string? FileOf(XElement font) =>
        font.Nodes().OfType<XText>().Select(text => text.Value.Trim()).FirstOrDefault(text => text.Length > 0);

    private static int IndexOf(XElement font) =>
        int.TryParse((string?)font.Attribute("index"), out int index) ? index : 0;
}
