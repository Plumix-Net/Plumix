using Plumix.UI;
using Path = System.IO.Path;

namespace Plumix.Impeller.Tests;

// C#-only test infrastructure; no Dart parity source. The backends are process-wide seams: every test class
// installs them through this one place, so parallel classes see the same shaper.

internal static class ImpellerTestBackend
{
    private static readonly Lazy<ImpellerTextShaper> s_shaper = new(Install);

    public static string FontsDirectory => Path.Combine(AppContext.BaseDirectory, "Fonts");

    public static string Ahem => Path.Combine(FontsDirectory, "ahem.ttf");

    public static string RobotoSlab => Path.Combine(FontsDirectory, "RobotoSlab-VariableFont_wght.ttf");

    public static ImpellerTextShaper Shaper => s_shaper.Value;

    private static ImpellerTextShaper Install()
    {
        ImpellerBackend.Install();
        ImpellerTextShaper shaper = ImpellerBackend.InstallText(Path.Combine(FontsDirectory, "FlutterTest.ttf"));
        shaper.Register(File.ReadAllBytes(Ahem), "Ahem");
        shaper.Register(File.ReadAllBytes(RobotoSlab), "RobotoSlab");
        shaper.AddSystemFace("monospace", Ahem, 0, 400, italic: false, actualWeight: 400);
        shaper.AddSystemAlias("courier", "monospace");
        return shaper;
    }

    public static Paragraph Paragraph(string text, string family, double fontSize = 10, double width = 1000,
        FontWeight? fontWeight = null)
    {
        _ = Shaper;
        var builder = new ParagraphBuilder(
            new ParagraphStyle(FontFamily: family, FontSize: fontSize, FontWeight: fontWeight));
        builder.AddText(text);
        Paragraph paragraph = builder.Build();
        paragraph.Layout(new ParagraphConstraints(width));
        return paragraph;
    }
}
