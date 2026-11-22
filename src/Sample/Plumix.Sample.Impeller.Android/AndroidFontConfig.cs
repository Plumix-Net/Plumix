using System.Xml.Linq;
using Plumix.Impeller;

namespace Plumix.Sample.Impeller.Android;

// C#-only host infrastructure; no Dart parity source. Android's font configuration (/system/etc/fonts.xml):
// its unnamed families form the system fallback chain, in order, the list minikin and Skia's Android font
// manager search for a code point the requested family lacks. The prototype reads only the first upright
// font of each fallback family.

/// <summary>Feeds Android's system fallback fonts to the Impeller text shaper.</summary>
internal static class AndroidFontConfig
{
    private const string ConfigPath = "/system/etc/fonts.xml";
    private const string FontDirectory = "/system/fonts/";

    public static void AddFallbacks(ImpellerTextShaper shaper)
    {
        if (!File.Exists(ConfigPath))
        {
            return;
        }

        XDocument config = XDocument.Load(ConfigPath);
        foreach (XElement family in config.Root!.Elements("family"))
        {
            if (family.Attribute("name") is not null)
            {
                continue;
            }

            XElement? font = family.Elements("font").FirstOrDefault(f => (string?)f.Attribute("style") != "italic")
                ?? family.Elements("font").FirstOrDefault();
            if (font is null)
            {
                continue;
            }

            // The file name is the element's own text, before any <axis> children.
            string file = font.Nodes().OfType<XText>().Select(text => text.Value.Trim()).FirstOrDefault() ?? "";
            if (file.Length == 0)
            {
                continue;
            }

            int index = int.TryParse((string?)font.Attribute("index"), out int parsed) ? parsed : 0;
            shaper.AddFallback(FontDirectory + file, index);
        }
    }
}
