using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using Transform = Plumix.Widgets.Transform;

namespace Plumix.Tests;

// Mirrors flutter/packages/flutter/test/widgets/icon_test.dart.
public sealed class IconTests
{
    private static readonly IconData Glyph = new(0x41, FontFamily: "Roboto");

    [Theory]
    [InlineData(null, null, null, null, 24.0)]
    [InlineData(96.0, null, null, null, 96.0)]
    [InlineData(null, 36.0, null, null, 36.0)]
    [InlineData(48.0, 36.0, null, null, 48.0)]
    [InlineData(null, null, true, null, 48.0)]
    [InlineData(96.0, null, true, null, 192.0)]
    [InlineData(null, 36.0, null, true, 72.0)]
    [InlineData(48.0, 36.0, false, true, 48.0)]
    public void Sizing_UsesExplicitThenThemeDefaultsAndScaling(
        double? size, double? themeSize, bool? scaling, bool? themeScaling, double expected)
    {
        using var tester = new FrameworkDartTester();
        foreach (IconData? glyph in new IconData?[] { null, Glyph })
        {
            tester.PumpWidget(new MediaQuery(
                new MediaQueryData(TextScaler: TextScaler.Linear(2.0)),
                Wrap(new IconTheme(
                    new IconThemeData(Size: themeSize, ApplyTextScaling: themeScaling),
                    new Icon(glyph, size: size, applyTextScaling: scaling)))));
            Assert.Equal(new Size(expected, expected), tester.GetSize(tester.ElementOfType<Icon>()));
            if (glyph is not null)
            {
                Assert.Equal(expected, Style(tester).FontSize);
                Assert.Equal(TextScaler.NoScaling, Rich(tester).TextScaler);
            }
        }
    }

    [Fact]
    public void NonlinearScaler_ScalesTheActualSizeAndIsNotAppliedTwice()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MediaQuery(
            new MediaQueryData(TextScaler: new NonlinearScaler()),
            Wrap(new Icon(Glyph, size: 36.0, applyTextScaling: true))));
        Assert.Equal(new Size(51.0, 51.0), tester.GetSize(tester.ElementOfType<Icon>()));
        Assert.Equal(51.0, Style(tester).FontSize);
        Assert.Equal(TextScaler.NoScaling, Rich(tester).TextScaler);
    }

    [Fact]
    public void FontStyle_IsIndependentOfAmbientTextAndIncludesFallbackAndSupplementaryGlyphs()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Wrap(new DefaultTextStyle(
            style: new TextStyle(FontSize: 80.0, LetterSpacing: 9.0, FontWeight: FontWeight.Black),
            child: new Icon(new IconData(0xf04b6, FontFamily: "Roboto", FontFamilyFallback: ["FallbackFont"]),
                fontWeight: FontWeight.Bold))));
        TextStyle style = Style(tester);
        Assert.False(style.Inherit);
        Assert.Equal(new FontFamily("Roboto"), style.FontFamily);
        Assert.Equal(new[] { "FallbackFont" }, style.FontFamilyFallback);
        Assert.Equal(FontWeight.Bold, style.FontWeight);
        Assert.Equal(24.0, style.FontSize);
        Assert.Equal(1.0, style.Height);
        Assert.Equal(TextLeadingDistribution.Even, style.LeadingDistribution);
        Assert.Null(style.LetterSpacing);
        Assert.Equal(char.ConvertFromUtf32(0xf04b6), Assert.IsType<TextSpan>(Rich(tester).Text).Text);
        Assert.Equal(TextOverflow.Visible, Rich(tester).Overflow);
        Assert.Null(Rich(tester).MaxLines);
        Assert.True(Rich(tester).SoftWrap);
        tester.PumpWidget(Wrap(new Icon(Glyph)));
        Assert.Null(Style(tester).FontWeight);
    }

    [Fact]
    public void VariationsAndShadows_ResolveDefaultsThemeAndWidgetOverridesInSourceOrder()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(Wrap(new Icon(Glyph)));
        Assert.Equal(Variations(0.0, 400.0, 0.0, 48.0), Style(tester).FontVariations);
        Assert.Null(Style(tester).Shadows);
        var themeShadows = new[] { new Shadow(color: Colors.Red, offset: new Point(2, 3), blurRadius: 4) };
        var widgetShadows = new[] { new Shadow(color: Colors.Blue, offset: new Point(-1, 2), blurRadius: 2) };
        var theme = new IconThemeData(Fill: 0.2, Weight: 3.0, Grade: 4.0, OpticalSize: 5.0, Shadows: themeShadows);
        tester.PumpWidget(Wrap(new IconTheme(theme, new Icon(Glyph))));
        Assert.Equal(Variations(0.2, 3.0, 4.0, 5.0), Style(tester).FontVariations);
        Assert.Same(themeShadows, Style(tester).Shadows);
        tester.PumpWidget(Wrap(new IconTheme(theme,
            new Icon(Glyph, fill: 0.6, weight: 7.0, grade: -8.0, opticalSize: 9.0, shadows: widgetShadows))));
        Assert.Equal(Variations(0.6, 7.0, -8.0, 9.0), Style(tester).FontVariations);
        Assert.Same(widgetShadows, Style(tester).Shadows);
        tester.PumpWidget(Wrap(new IconTheme(theme, new Icon(Glyph, shadows: []))));
        Assert.Empty(Style(tester).Shadows!);
    }

    [Fact]
    public void Opacity_AppliesToExplicitOrThemeColorBeforeForegroundBlendMode()
    {
        using var tester = new FrameworkDartTester();
        var themeColor = new Color(0xFF666666);
        var explicitColor = new Color(0x80663399);
        tester.PumpWidget(Wrap(new IconTheme(
            new IconThemeData(Color: themeColor, Opacity: 0.5), new Icon(Glyph))));
        ColorMatchers.AssertSameColorAs(themeColor.WithOpacity(0.5), Style(tester).Color);
        tester.PumpWidget(Wrap(new IconTheme(new IconThemeData(Opacity: 0.5),
            new Icon(Glyph, color: explicitColor))));
        ColorMatchers.AssertSameColorAs(explicitColor.WithOpacity(explicitColor.Opacity * 0.5), Style(tester).Color);
        tester.PumpWidget(Wrap(new IconTheme(new IconThemeData(Opacity: 0.5),
            new Icon(Glyph, color: explicitColor, blendMode: BlendMode.Clear))));
        TextStyle style = Style(tester);
        Assert.Null(style.Color);
        Assert.Equal(BlendMode.Clear, style.Foreground!.BlendMode);
        ColorMatchers.AssertSameColorAs(explicitColor.WithOpacity(explicitColor.Opacity * 0.5), style.Foreground.Color);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_AnnouncesOnlyTheLabelAndRetainsElementsWhenItChanges(bool empty)
    {
        using var tester = new FrameworkDartTester();
        using var semantics = new SemanticsTester(tester);
        IconData? glyph = empty ? null : Glyph;
        tester.PumpWidget(Wrap(new Icon(glyph)));
        Element leaf = empty ? tester.ElementOfType<SizedBox>() : tester.ElementOfType<RichText>();
        tester.PumpWidget(Wrap(new Icon(glyph, semanticLabel: "a label")));
        Assert.Single(semantics.NodesWith(label: "a label"));
        Assert.Empty(semantics.NodesWith(label: "A"));
        Assert.Same(leaf, empty ? tester.ElementOfType<SizedBox>() : tester.ElementOfType<RichText>());
        Assert.Equal(empty ? 0 : 1, tester.ElementsOfType<ExcludeSemantics>().Count);
    }

    [Theory]
    [InlineData(true, TextDirection.Rtl, null, true)]
    [InlineData(true, TextDirection.Ltr, null, false)]
    [InlineData(false, TextDirection.Rtl, null, false)]
    [InlineData(true, TextDirection.Ltr, TextDirection.Rtl, true)]
    [InlineData(true, TextDirection.Rtl, TextDirection.Ltr, false)]
    public void Mirroring_IsCenteredAndDoesNotTransformHitTests(
        bool directional, TextDirection ambient, TextDirection? explicitDirection, bool mirrored)
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(ambient,
            new Center(child: new Icon(Glyph with { MatchTextDirection = directional },
                textDirection: explicitDirection))));
        Assert.Equal(explicitDirection ?? ambient, Rich(tester).TextDirection);
        if (mirrored)
        {
            var transform = (Transform)tester.ElementOfType<Transform>().Widget;
            var expected = Matrix4.Identity();
            expected.ScaleByDouble(-1, 1, 1, 1);
            Assert.Equal(expected, transform.Matrix);
            Assert.Equal((AlignmentGeometry)Alignment.Center, transform.Alignment);
            Assert.False(transform.TransformHitTests);
        }
        else
        {
            Assert.Empty(tester.ElementsOfType<Transform>());
        }
    }

    [Fact]
    public void ZeroArea_DoesNotCrashAndExplicitDirectionNeedsNoAncestor()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Center(child: SizedBox.Shrink(
            child: new Icon(Glyph, textDirection: TextDirection.Ltr))));
        Assert.Equal(default, tester.GetSize(tester.ElementOfType<Icon>()));
        Assert.Null(tester.TakeException());
    }

    [DebugOnlyFact]
    public void Constructors_AssertSourceConstraintsAndEmptyIconStillRequiresDirectionality()
    {
        Assert.Throws<AssertionError>(() => new Icon(Glyph, fill: -0.1));
        Assert.Throws<AssertionError>(() => new Icon(Glyph, fill: 1.1));
        Assert.Throws<AssertionError>(() => new Icon(Glyph, weight: -0.1));
        Assert.Throws<AssertionError>(() => new Icon(Glyph, weight: 0));
        Assert.Throws<AssertionError>(() => new Icon(Glyph, opticalSize: -0.1));
        Assert.Throws<AssertionError>(() => new Icon(Glyph, opticalSize: 0));
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Icon(null));
        Assert.IsType<FlutterError>(tester.TakeException());
    }

    [DebugOnlyFact]
    public void Diagnostics_UseSourcePropertyNamesAndEmptyGlyphDescription()
    {
        var properties = new DiagnosticPropertiesBuilder();
        new Icon(null).DebugFillProperties(properties);
        Assert.Equal(new[] { "icon", "size", "fill", "weight", "grade", "opticalSize", "color", "shadows",
            "semanticLabel", "textDirection", "applyTextScaling" }, properties.Properties.Select(p => p.Name));
        Assert.Equal("<empty>", properties.Properties[0].ToDescription());
    }

    private static Widget Wrap(Widget child) => new Directionality(TextDirection.Ltr, new Center(child: child));
    private static RichText Rich(FrameworkDartTester tester) => (RichText)tester.ElementOfType<RichText>().Widget;
    private static TextStyle Style(FrameworkDartTester tester) => Rich(tester).Text.Style!;
    private static FontVariation[] Variations(double fill, double weight, double grade, double opticalSize) =>
    [
        new("FILL", fill), new("wght", weight), new("GRAD", grade), new("opsz", opticalSize),
    ];

    private sealed record NonlinearScaler : TextScaler
    {
        public override double Scale(double fontSize) => fontSize + 15.0;
        public override double TextScaleFactor => 9.0;
    }
}
