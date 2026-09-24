using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using FontFeature = Plumix.UI.FontFeature;

// Dart parity source: flutter/packages/flutter/test/painting/text_style_test.dart, plus the
// dart:ui `FontWeight`/`FontFeature`/`FontVariation`/`TextStyle.toString` cases of
// engine/src/flutter/testing/dart/text_test.dart that the painting TextStyle depends on.

namespace Plumix.Tests;

public sealed class TextStyleTests
{
    private static FontWeight W(int value) => (FontWeight)value;

    // -- TextStyle control test ----------------------------------------------------------------

    [DebugOnlyFact]
    public void ControlTest()
    {
        Assert.Equal("TextStyle(inherit: false, <no style specified>)", new TextStyle(Inherit: false).ToString());
        Assert.Equal("TextStyle(<all styles inherited>)", new TextStyle().ToString());

        var s1 = new TextStyle(FontSize: 10.0, FontWeight: W(800), Height: 123.0);
        Assert.Null(s1.FontFamily);
        Assert.Equal(10.0, s1.FontSize);
        Assert.Equal(W(800), s1.FontWeight);
        Assert.Equal(123.0, s1.Height);
        Assert.Equal(s1, s1);
        Assert.Equal("TextStyle(inherit: true, size: 10.0, weight: 800, height: 123.0x)", s1.ToString());

        // Check that the inherit flag can be set with copyWith.
        Assert.Equal(
            "TextStyle(inherit: false, size: 10.0, weight: 800, height: 123.0x)",
            s1.CopyWith(inherit: false).ToString());

        TextStyle s2 = s1.CopyWith(
            color: Color.FromUInt32(0xFF00FF00),
            height: 100.0,
            leadingDistribution: TextLeadingDistribution.Even);
        Assert.Null(s1.FontFamily);
        Assert.Equal(10.0, s1.FontSize);
        Assert.Equal(W(800), s1.FontWeight);
        Assert.Equal(123.0, s1.Height);
        Assert.Null(s1.Color);
        Assert.Null(s2.FontFamily);
        Assert.Equal(10.0, s2.FontSize);
        Assert.Equal(W(800), s2.FontWeight);
        Assert.Equal(100.0, s2.Height);
        Assert.Equal(Color.FromUInt32(0xFF00FF00), s2.Color);
        Assert.Equal(TextLeadingDistribution.Even, s2.LeadingDistribution);
        Assert.NotEqual(s1, s2);
        Assert.Equal(
            "TextStyle(inherit: true, color: Color(alpha: 1.0000, red: 0.0000, green: 1.0000, blue: 0.0000, "
            + "colorSpace: ColorSpace.sRGB), size: 10.0, weight: 800, height: 100.0x, leadingDistribution: even)",
            s2.ToString());

        TextStyle s3 = s1.Apply(fontSizeFactor: 2.0, fontSizeDelta: -2.0, fontWeightDelta: -4);
        Assert.Null(s1.FontFamily);
        Assert.Equal(10.0, s1.FontSize);
        Assert.Equal(W(800), s1.FontWeight);
        Assert.Equal(123.0, s1.Height);
        Assert.Null(s1.Color);
        Assert.Null(s3.FontFamily);
        Assert.Equal(18.0, s3.FontSize);
        Assert.Equal(W(400), s3.FontWeight);
        Assert.Equal(123.0, s3.Height);
        Assert.Null(s3.Color);
        Assert.NotEqual(s1, s3);

        Assert.Equal(W(100), s1.Apply(fontWeightDelta: -10).FontWeight);
        Assert.Equal(W(900), s1.Apply(fontWeightDelta: 2).FontWeight);
        Assert.Equal(s1, s1.Merge(null));

        TextStyle s4 = s2.Merge(s1);
        Assert.Null(s1.FontFamily);
        Assert.Equal(10.0, s1.FontSize);
        Assert.Equal(W(800), s1.FontWeight);
        Assert.Equal(123.0, s1.Height);
        Assert.Null(s1.Color);
        Assert.Null(s2.FontFamily);
        Assert.Equal(10.0, s2.FontSize);
        Assert.Equal(W(800), s2.FontWeight);
        Assert.Equal(100.0, s2.Height);
        Assert.Equal(Color.FromUInt32(0xFF00FF00), s2.Color);
        Assert.Equal(TextLeadingDistribution.Even, s2.LeadingDistribution);
        Assert.NotEqual(s2, s4);
        Assert.Null(s4.FontFamily);
        Assert.Equal(10.0, s4.FontSize);
        Assert.Equal(W(800), s4.FontWeight);
        Assert.Equal(123.0, s4.Height);
        Assert.Equal(Color.FromUInt32(0xFF00FF00), s4.Color);
        Assert.Equal(TextLeadingDistribution.Even, s4.LeadingDistribution);

        TextStyle s5 = TextStyle.Lerp(s1, s3, 0.25)!;
        Assert.Null(s1.FontFamily);
        Assert.Equal(10.0, s1.FontSize);
        Assert.Equal(W(800), s1.FontWeight);
        Assert.Equal(123.0, s1.Height);
        Assert.Null(s1.Color);
        Assert.Null(s3.FontFamily);
        Assert.Equal(18.0, s3.FontSize);
        Assert.Equal(W(400), s3.FontWeight);
        Assert.Equal(123.0, s3.Height);
        Assert.Null(s3.Color);
        Assert.NotEqual(s3, s5);
        Assert.Null(s5.FontFamily);
        Assert.Equal(12.0, s5.FontSize);
        Assert.Equal(W(700), s5.FontWeight);
        Assert.Equal(123.0, s5.Height);
        Assert.Null(s5.Color);

        Assert.Null(TextStyle.Lerp(null, null, 0.5));

        TextStyle s6 = TextStyle.Lerp(null, s3, 0.25)!;
        Assert.Null(s3.FontFamily);
        Assert.Equal(18.0, s3.FontSize);
        Assert.Equal(W(400), s3.FontWeight);
        Assert.Equal(123.0, s3.Height);
        Assert.Null(s3.Color);
        Assert.NotEqual(s3, s6);
        Assert.Null(s6.FontFamily);
        Assert.Null(s6.FontSize);
        Assert.Equal(W(400), s6.FontWeight);
        Assert.Null(s6.Height);
        Assert.Null(s6.Color);

        TextStyle s7 = TextStyle.Lerp(null, s3, 0.75)!;
        Assert.Null(s3.FontFamily);
        Assert.Equal(18.0, s3.FontSize);
        Assert.Equal(W(400), s3.FontWeight);
        Assert.Equal(123.0, s3.Height);
        Assert.Null(s3.Color);
        Assert.Equal(s3, s7);
        Assert.Null(s7.FontFamily);
        Assert.Equal(18.0, s7.FontSize);
        Assert.Equal(W(400), s7.FontWeight);
        Assert.Equal(123.0, s7.Height);
        Assert.Null(s7.Color);

        TextStyle s8 = TextStyle.Lerp(s3, null, 0.25)!;
        Assert.Equal(s3, s8);
        Assert.Null(s8.FontFamily);
        Assert.Equal(18.0, s8.FontSize);
        Assert.Equal(W(400), s8.FontWeight);
        Assert.Equal(123.0, s8.Height);
        Assert.Null(s8.Color);

        TextStyle s9 = TextStyle.Lerp(s3, null, 0.75)!;
        Assert.NotEqual(s3, s9);
        Assert.Null(s9.FontFamily);
        Assert.Null(s9.FontSize);
        Assert.Equal(W(400), s9.FontWeight);
        Assert.Null(s9.Height);
        Assert.Null(s9.Color);

        ParagraphTextStyle ts5 = s5.GetTextStyle();
        Assert.Equal(new ParagraphTextStyle(FontWeight: W(700), FontSize: 12.0, Height: 123.0), ts5);
        Assert.Equal(
            UiTextStyleString(fontWeight: "FontWeight.w700", fontSize: "12.0", height: "123.0x"),
            ts5.ToString());
        ParagraphTextStyle ts2 = s2.GetTextStyle();
        Assert.Equal(
            new ParagraphTextStyle(
                Color: Color.FromUInt32(0xFF00FF00),
                FontWeight: W(800),
                FontSize: 10.0,
                Height: 100.0,
                LeadingDistribution: TextLeadingDistribution.Even),
            ts2);
        Assert.Equal(
            UiTextStyleString(
                color: Color.FromUInt32(0xFF00FF00).ToDartString(),
                fontWeight: "FontWeight.w800",
                fontSize: "10.0",
                height: "100.0x",
                leadingDistribution: "TextLeadingDistribution.even"),
            ts2.ToString());

        Assert.Equal(
            new ParagraphStyle(
                TextAlign: TextAlign.Center,
                FontWeight: W(800),
                FontSize: 10.0,
                Height: 100.0,
                TextHeightBehavior: new TextHeightBehavior(LeadingDistribution: TextLeadingDistribution.Even)),
            s2.GetParagraphStyle(textAlign: TextAlign.Center));
        Assert.Equal(
            new ParagraphStyle(FontWeight: W(700), FontSize: 12.0, Height: 123.0),
            s5.GetParagraphStyle());
    }

    [Fact]
    public void TextStyleWithTextDirection()
    {
        Assert.Equal(
            new ParagraphStyle(TextDirection: TextDirection.Ltr, FontSize: 14.0),
            new TextStyle().GetParagraphStyle(textDirection: TextDirection.Ltr));
        Assert.Equal(
            new ParagraphStyle(TextDirection: TextDirection.Rtl, FontSize: 14.0),
            new TextStyle().GetParagraphStyle(textDirection: TextDirection.Rtl));
    }

    [Fact]
    public void TextStyleUsingPackageFont()
    {
        var s6 = new TextStyle(FontFamily: new FontFamily("test"));
        Assert.Equal("test", s6.FontFamily!.Name);
        Assert.Equal(UiTextStyleString(fontFamily: "test"), s6.GetTextStyle().ToString());

        var s7 = new TextStyle(FontFamily: new FontFamily("test"), Package: "p");
        Assert.Equal("packages/p/test", s7.FontFamily!.Name);
        Assert.Equal(UiTextStyleString(fontFamily: "packages/p/test"), s7.GetTextStyle().ToString());

        var s8 = new TextStyle(FontFamilyFallback: ["test", "test2"], Package: "p");
        Assert.Equal("packages/p/test", s8.FontFamilyFallback![0]);
        Assert.Equal("packages/p/test2", s8.FontFamilyFallback[1]);
        Assert.Equal(2, s8.FontFamilyFallback.Count);

        var s9 = new TextStyle(Package: "p");
        Assert.Null(s9.FontFamilyFallback);

        var s10 = new TextStyle(FontFamilyFallback: [], Package: "p");
        Assert.Equal([], s10.FontFamilyFallback!);

        // Ensure that package prefix is not duplicated after copying.
        TextStyle s11 = s8.CopyWith();
        Assert.Equal("packages/p/test", s11.FontFamilyFallback![0]);
        Assert.Equal("packages/p/test2", s11.FontFamilyFallback[1]);
        Assert.Equal(2, s11.FontFamilyFallback.Count);
        Assert.Equal(s8, s11);

        // Ensure that package prefix is not duplicated after applying.
        TextStyle s12 = s8.Apply();
        Assert.Equal("packages/p/test", s12.FontFamilyFallback![0]);
        Assert.Equal("packages/p/test2", s12.FontFamilyFallback[1]);
        Assert.Equal(2, s12.FontFamilyFallback.Count);
        Assert.Equal(s8, s12);
    }

    [Fact]
    public void TextStylePackageFontMerge()
    {
        var s1 = new TextStyle(Package: "p", FontFamily: new FontFamily("font1"), FontFamilyFallback: ["fallback1"]);
        var s2 = new TextStyle(Package: "p", FontFamily: new FontFamily("font2"), FontFamilyFallback: ["fallback2"]);

        TextStyle emptyMerge = new TextStyle().Merge(s1);
        Assert.Equal("packages/p/font1", emptyMerge.FontFamily!.Name);
        Assert.Equal(["packages/p/fallback1"], emptyMerge.FontFamilyFallback!);

        TextStyle lerp1 = TextStyle.Lerp(s1, s2, 0)!;
        Assert.Equal("packages/p/font1", lerp1.FontFamily!.Name);
        Assert.Equal(["packages/p/fallback1"], lerp1.FontFamilyFallback!);

        TextStyle lerp2 = TextStyle.Lerp(s1, s2, 1.0)!;
        Assert.Equal("packages/p/font2", lerp2.FontFamily!.Name);
        Assert.Equal(["packages/p/fallback2"], lerp2.FontFamilyFallback!);
    }

    [Fact]
    public void TextStyleFontFamilyFallback()
    {
        var s1 = new TextStyle(FontFamilyFallback: ["Roboto", "test"]);
        Assert.Equal("Roboto", s1.FontFamilyFallback![0]);
        Assert.Equal("test", s1.FontFamilyFallback[1]);
        Assert.Equal(2, s1.FontFamilyFallback.Count);

        var s2 = new TextStyle(FontFamily: new FontFamily("foo"), FontFamilyFallback: ["Roboto", "test"]);
        Assert.Equal("Roboto", s2.FontFamilyFallback![0]);
        Assert.Equal("test", s2.FontFamilyFallback[1]);
        Assert.Equal("foo", s2.FontFamily!.Name);
        Assert.Equal(2, s2.FontFamilyFallback.Count);

        var s3 = new TextStyle(FontFamily: new FontFamily("foo"));
        Assert.Equal("foo", s3.FontFamily!.Name);
        Assert.Null(s3.FontFamilyFallback);

        var s4 = new TextStyle(FontFamily: new FontFamily("foo"), FontFamilyFallback: []);
        Assert.Equal("foo", s4.FontFamily!.Name);
        Assert.Equal([], s4.FontFamilyFallback!);
        Assert.Empty(s4.FontFamilyFallback!);

        Assert.Equal(
            UiTextStyleString(fontFamily: "foo", fontFamilyFallback: "[Roboto, test]"),
            s2.GetTextStyle().ToString());

        Assert.Equal("foo", s2.Apply().FontFamily!.Name);
        Assert.Equal(["Roboto", "test"], s2.Apply().FontFamilyFallback!);
        Assert.Equal("bar", s2.Apply(fontFamily: new FontFamily("bar")).FontFamily!.Name);
        Assert.Equal(["Banana"], s2.Apply(fontFamilyFallback: ["Banana"]).FontFamilyFallback!);
    }

    [DebugOnlyFact]
    public void TextStyleDebugLabel()
    {
        var unknown = new TextStyle();
        var foo = new TextStyle(DebugLabel: "foo", FontSize: 1.0);
        var bar = new TextStyle(DebugLabel: "bar", FontSize: 2.0);
        var baz = new TextStyle(DebugLabel: "baz", FontSize: 3.0);

        Assert.Null(unknown.DebugLabel);
        Assert.Equal("TextStyle(<all styles inherited>)", unknown.ToString());
        Assert.Null(unknown.CopyWith().DebugLabel);
        Assert.Equal("123", unknown.CopyWith(debugLabel: "123").DebugLabel);
        Assert.Null(unknown.Apply().DebugLabel);

        Assert.Equal("foo", foo.DebugLabel);
        Assert.Equal("TextStyle(debugLabel: foo, inherit: true, size: 1.0)", foo.ToString());
        Assert.Equal("(foo).merge(bar)", foo.Merge(bar).DebugLabel);
        Assert.Equal("((foo).merge(bar)).merge(baz)", foo.Merge(bar).Merge(baz).DebugLabel);
        Assert.Equal("(foo).copyWith", foo.CopyWith().DebugLabel);
        Assert.Equal("(foo).apply", foo.Apply().DebugLabel);
        Assert.Equal("lerp(foo ⎯0.5→ bar)", TextStyle.Lerp(foo, bar, 0.5)!.DebugLabel);
        Assert.Equal(
            "(lerp((foo).merge(bar) ⎯0.5→ baz)).copyWith",
            TextStyle.Lerp(foo.Merge(bar), baz, 0.51)!.CopyWith().DebugLabel);
    }

    [Fact]
    public void TextStyleHashCode()
    {
        var a = new TextStyle(
            FontFamilyFallback: ["Roboto"],
            Shadows: [new Shadow()],
            FontFeatures: [new FontFeature("abcd")],
            FontVariations: [new FontVariation("wght", 123.0)]);
        var b = new TextStyle(
            FontFamilyFallback: ["Noto"],
            Shadows: [new Shadow()],
            FontFeatures: [new FontFeature("abcd")],
            FontVariations: [new FontVariation("wght", 123.0)]);
        Assert.Equal(a.GetHashCode(), a.GetHashCode());
        Assert.NotEqual(a.GetHashCode(), b.GetHashCode());

        var c = new TextStyle(LeadingDistribution: TextLeadingDistribution.Even);
        var d = new TextStyle(LeadingDistribution: TextLeadingDistribution.Proportional);
        Assert.Equal(c.GetHashCode(), c.GetHashCode());
        Assert.NotEqual(c.GetHashCode(), d.GetHashCode());
    }

    [Fact]
    public void TextStyleShadows()
    {
        var shadow1 = new Shadow(blurRadius: 1.0, offset: new Point(1.0, 1.0));
        var shadow2 = new Shadow(blurRadius: 2.0, color: Color.FromUInt32(0xFF111111), offset: new Point(2.0, 2.0));
        var shadow3 = new Shadow(blurRadius: 3.0, color: Color.FromUInt32(0xFF222222), offset: new Point(3.0, 3.0));
        var shadow4 = new Shadow(blurRadius: 4.0, color: Color.FromUInt32(0xFF333333), offset: new Point(4.0, 4.0));

        var s1 = new TextStyle(Shadows: [shadow1, shadow2]);
        var s2 = new TextStyle(Shadows: [shadow3, shadow4]);
        TextStyle s3 = TextStyle.Lerp(s1, s2, 0.5)!;
        Assert.Equal(2, s3.Shadows!.Count);
        Assert.Equal(Shadow.Lerp(shadow1, shadow3, 0.5), s3.Shadows[0]);
        Assert.Equal(2.0, s3.Shadows[0].BlurRadius);
        Assert.Equal(new Point(2.0, 2.0), s3.Shadows[0].Offset);
        Assert.Equal(Shadow.Lerp(shadow2, shadow4, 0.5), s3.Shadows[1]);
        Assert.Equal(3.0, s3.Shadows[1].BlurRadius);
        Assert.Equal(new Point(3.0, 3.0), s3.Shadows[1].Offset);
    }

    [Fact]
    public void TextStyleForegroundAndColorCombos()
    {
        Color red = Color.FromArgb(255, 255, 0, 0);
        Color blue = Color.FromArgb(255, 0, 0, 255);
        var redTextStyle = new TextStyle(Color: red);
        var blueTextStyle = new TextStyle(Color: blue);
        var redPaintTextStyle = new TextStyle(Foreground: new Paint { Color = red });
        var bluePaintTextStyle = new TextStyle(Foreground: new Paint { Color = blue });

        // merge/copyWith
        TextStyle redBlueBothForegroundMerged = redTextStyle.Merge(blueTextStyle);
        Assert.Equal(blue, redBlueBothForegroundMerged.Color);
        Assert.Null(redBlueBothForegroundMerged.Foreground);

        TextStyle redBlueBothPaintMerged = redPaintTextStyle.Merge(bluePaintTextStyle);
        Assert.Null(redBlueBothPaintMerged.Color);
        Assert.Same(bluePaintTextStyle.Foreground, redBlueBothPaintMerged.Foreground);

        TextStyle redPaintBlueColorMerged = redPaintTextStyle.Merge(blueTextStyle);
        Assert.Null(redPaintBlueColorMerged.Color);
        Assert.Same(redPaintTextStyle.Foreground, redPaintBlueColorMerged.Foreground);

        TextStyle blueColorRedPaintMerged = blueTextStyle.Merge(redPaintTextStyle);
        Assert.Null(blueColorRedPaintMerged.Color);
        Assert.Same(redPaintTextStyle.Foreground, blueColorRedPaintMerged.Foreground);

        // apply
        Assert.Null(redPaintTextStyle.Apply(color: blue).Color);
        Assert.Equal(red, redPaintTextStyle.Apply(color: blue).Foreground!.Color);
        Assert.Equal(blue, redTextStyle.Apply(color: blue).Color);

        // lerp
        Assert.Equal(ColorUtilities.Lerp(red, blue, 0.25), TextStyle.Lerp(redTextStyle, blueTextStyle, 0.25)!.Color);
        Assert.Null(TextStyle.Lerp(redTextStyle, bluePaintTextStyle, 0.25)!.Color);
        Assert.Equal(red, TextStyle.Lerp(redTextStyle, bluePaintTextStyle, 0.25)!.Foreground!.Color);
        Assert.Equal(blue, TextStyle.Lerp(redTextStyle, bluePaintTextStyle, 0.75)!.Foreground!.Color);

        Assert.Null(TextStyle.Lerp(redPaintTextStyle, bluePaintTextStyle, 0.25)!.Color);
        Assert.Equal(red, TextStyle.Lerp(redPaintTextStyle, bluePaintTextStyle, 0.25)!.Foreground!.Color);
        Assert.Equal(blue, TextStyle.Lerp(redPaintTextStyle, bluePaintTextStyle, 0.75)!.Foreground!.Color);
    }

    [DebugOnlyFact]
    public void BackgroundColor()
    {
        var s1 = new TextStyle();
        Assert.Null(s1.BackgroundColor);
        Assert.Equal("TextStyle(<all styles inherited>)", s1.ToString());

        var s2 = new TextStyle(BackgroundColor: Color.FromUInt32(0xFF00FF00));
        Assert.Equal(Color.FromUInt32(0xFF00FF00), s2.BackgroundColor);
        Assert.Equal(
            "TextStyle(inherit: true, backgroundColor: Color(alpha: 1.0000, red: 0.0000, green: 1.0000, "
            + "blue: 0.0000, colorSpace: ColorSpace.sRGB))",
            s2.ToString());

        ParagraphTextStyle ts2 = s2.GetTextStyle();
        Assert.Matches(new Regex(@"background: Paint\(Color\(.*\).*\)"), ts2.ToString());
    }

    [Fact]
    public void TextStyleBackgroundAndBackgroundColorCombos()
    {
        Color red = Color.FromArgb(255, 255, 0, 0);
        Color blue = Color.FromArgb(255, 0, 0, 255);
        var redTextStyle = new TextStyle(BackgroundColor: red);
        var blueTextStyle = new TextStyle(BackgroundColor: blue);
        var redPaintTextStyle = new TextStyle(Background: new Paint { Color = red });
        var bluePaintTextStyle = new TextStyle(Background: new Paint { Color = blue });

        // merge/copyWith
        TextStyle redBlueBothForegroundMerged = redTextStyle.Merge(blueTextStyle);
        Assert.Equal(blue, redBlueBothForegroundMerged.BackgroundColor);
        Assert.Null(redBlueBothForegroundMerged.Foreground);

        TextStyle redBlueBothPaintMerged = redPaintTextStyle.Merge(bluePaintTextStyle);
        Assert.Null(redBlueBothPaintMerged.BackgroundColor);
        Assert.Same(bluePaintTextStyle.Background, redBlueBothPaintMerged.Background);

        TextStyle redPaintBlueColorMerged = redPaintTextStyle.Merge(blueTextStyle);
        Assert.Null(redPaintBlueColorMerged.BackgroundColor);
        Assert.Same(redPaintTextStyle.Background, redPaintBlueColorMerged.Background);

        TextStyle blueColorRedPaintMerged = blueTextStyle.Merge(redPaintTextStyle);
        Assert.Null(blueColorRedPaintMerged.BackgroundColor);
        Assert.Same(redPaintTextStyle.Background, blueColorRedPaintMerged.Background);

        // apply
        Assert.Null(redPaintTextStyle.Apply(backgroundColor: blue).BackgroundColor);
        Assert.Equal(red, redPaintTextStyle.Apply(backgroundColor: blue).Background!.Color);
        Assert.Equal(blue, redTextStyle.Apply(backgroundColor: blue).BackgroundColor);

        // lerp
        Assert.Equal(
            ColorUtilities.Lerp(red, blue, 0.25),
            TextStyle.Lerp(redTextStyle, blueTextStyle, 0.25)!.BackgroundColor);
        Assert.Null(TextStyle.Lerp(redTextStyle, bluePaintTextStyle, 0.25)!.BackgroundColor);
        Assert.Equal(red, TextStyle.Lerp(redTextStyle, bluePaintTextStyle, 0.25)!.Background!.Color);
        Assert.Equal(blue, TextStyle.Lerp(redTextStyle, bluePaintTextStyle, 0.75)!.Background!.Color);

        Assert.Null(TextStyle.Lerp(redPaintTextStyle, bluePaintTextStyle, 0.25)!.BackgroundColor);
        Assert.Equal(red, TextStyle.Lerp(redPaintTextStyle, bluePaintTextStyle, 0.25)!.Background!.Color);
        Assert.Equal(blue, TextStyle.Lerp(redPaintTextStyle, bluePaintTextStyle, 0.75)!.Background!.Color);
    }

    [Fact]
    public void TextStyleStrutTextScaler()
    {
        // Tests that textScaler is propagated to strut paragraph style.
        var style0 = new TextStyle(FontSize: 10);
        ParagraphStyle paragraphStyle0 = style0.GetParagraphStyle(textScaler: TextScaler.Linear(2.5));

        var style1 = new TextStyle(FontSize: 25);
        ParagraphStyle paragraphStyle1 = style1.GetParagraphStyle();

        Assert.Equal(paragraphStyle0, paragraphStyle1);
    }

    [Fact]
    public void TextStyleApply()
    {
        var style = new TextStyle(
            FontSize: 10,
            Shadows: [],
            FontStyle: FontStyle.Normal,
            FontFeatures: [],
            FontVariations: [],
            TextBaseline: TextBaseline.Alphabetic,
            LeadingDistribution: TextLeadingDistribution.Even);

        Assert.Empty(style.Apply().Shadows!);
        Assert.Equal([new Shadow(blurRadius: 2.0)], style.Apply(shadows: [new Shadow(blurRadius: 2.0)]).Shadows!);

        Assert.Equal(FontStyle.Normal, style.Apply().FontStyle);
        Assert.Equal(FontStyle.Italic, style.Apply(fontStyle: FontStyle.Italic).FontStyle);

        Assert.Null(style.Apply().Locale);
        Assert.Equal(new Locale("es"), style.Apply(locale: new Locale("es")).Locale);

        Assert.Empty(style.Apply().FontFeatures!);
        Assert.Equal(
            [FontFeature.Enable("test")],
            style.Apply(fontFeatures: [FontFeature.Enable("test")]).FontFeatures!);

        Assert.Empty(style.Apply().FontVariations!);
        Assert.Equal(
            [new FontVariation("test", 100.0)],
            style.Apply(fontVariations: [new FontVariation("test", 100.0)]).FontVariations!);

        Assert.Equal(TextBaseline.Alphabetic, style.Apply().TextBaseline);
        Assert.Equal(TextBaseline.Ideographic, style.Apply(textBaseline: TextBaseline.Ideographic).TextBaseline);

        Assert.Equal(TextLeadingDistribution.Even, style.Apply().LeadingDistribution);
        Assert.Equal(
            TextLeadingDistribution.Proportional,
            style.Apply(leadingDistribution: TextLeadingDistribution.Proportional).LeadingDistribution);

        Assert.Equal(
            TextDefaults.TextHeightNone,
            new TextStyle(Height: TextDefaults.TextHeightNone).Apply(heightFactor: 1000, heightDelta: 1000).Height);
    }

    [Fact]
    public void TextStyleFontFamilyAndPackage()
    {
        var p = new TextStyle(FontFamily: new FontFamily("fontFamily"), Package: "foo");
        var q = new TextStyle(FontFamily: new FontFamily("fontFamily"), Package: "bar");
        Assert.NotEqual(p, q);
        Assert.NotEqual(p.GetHashCode(), q.GetHashCode());

        Assert.Equal("fontFamily", new TextStyle(FontFamily: new FontFamily("fontFamily")).FontFamily!.Name);
        Assert.Equal(
            "packages/bar/fontFamily",
            new TextStyle(FontFamily: new FontFamily("fontFamily")).CopyWith(package: "bar").FontFamily!.Name);
        Assert.Equal("packages/foo/fontFamily", p.FontFamily!.Name);
        Assert.Equal("packages/bar/fontFamily", p.CopyWith(package: "bar").FontFamily!.Name);
        Assert.Equal("packages/bar/fontFamily", new TextStyle().Merge(q).FontFamily!.Name);
        Assert.Equal(
            "packages/foo/fontFamily",
            new TextStyle().Apply(fontFamily: new FontFamily("fontFamily"), package: "foo").FontFamily!.Name);
        Assert.Equal(
            "packages/bar/fontFamily",
            p.Apply(fontFamily: new FontFamily("fontFamily"), package: "bar").FontFamily!.Name);
    }

    [Fact]
    public void TextStyleLerpIdenticalAB()
    {
        Assert.Null(TextStyle.Lerp(null, null, 0));
        var style = new TextStyle();
        Assert.Same(style, TextStyle.Lerp(style, style, 0.5));
    }

    [DebugOnlyFact]
    public void ThrowsWhenLerpingBetweenInheritTrueAndFalseWithUnspecifiedFields()
    {
        var fromStyle = new TextStyle();
        var toStyle = new TextStyle(Inherit: false);
        FlutterError error = Assert.Throws<FlutterError>(() => TextStyle.Lerp(fromStyle, toStyle, 0.5));
        Assert.StartsWith("Failed to interpolate TextStyles with different inherit values.", error.Message);
        Assert.Contains(
            "\"color\", \"backgroundColor\", \"fontSize\", \"letterSpacing\", \"wordSpacing\", \"height\", "
            + "\"decorationColor\", \"decorationThickness\"",
            error.ToString(),
            StringComparison.Ordinal);
        Assert.Equal(fromStyle, TextStyle.Lerp(fromStyle, fromStyle, 0.5));
    }

    [Fact]
    public void DoesNotThrowWhenLerpingBetweenInheritTrueAndFalseButFullySpecifiedStyles()
    {
        var fromStyle = new TextStyle();
        var toStyle = new TextStyle(
            Inherit: false,
            Color: Color.FromUInt32(0x87654321),
            BackgroundColor: Color.FromUInt32(0x12345678),
            FontSize: 20,
            LetterSpacing: 1,
            WordSpacing: 1,
            Height: 20,
            DecorationColor: Color.FromUInt32(0x11111111),
            DecorationThickness: 5);

        Assert.Equal(toStyle, TextStyle.Lerp(fromStyle, toStyle, 1));
    }

    [Fact]
    public void LerpFontVariations()
    {
        // nil cases
        Assert.Equal([], TextStyle.LerpFontVariations([], [], 0.0)!);
        Assert.Equal([], TextStyle.LerpFontVariations([], [], 0.5)!);
        Assert.Equal([], TextStyle.LerpFontVariations([], [], 1.0)!);
        Assert.Null(TextStyle.LerpFontVariations(null, [], 0.0));
        Assert.Equal([], TextStyle.LerpFontVariations(null, [], 0.5)!);
        Assert.Equal([], TextStyle.LerpFontVariations(null, [], 1.0)!);
        Assert.Equal([], TextStyle.LerpFontVariations([], null, 0.0)!);
        Assert.Null(TextStyle.LerpFontVariations([], null, 0.5));
        Assert.Null(TextStyle.LerpFontVariations([], null, 1.0));
        Assert.Null(TextStyle.LerpFontVariations(null, null, 0.0));
        Assert.Null(TextStyle.LerpFontVariations(null, null, 0.5));
        Assert.Null(TextStyle.LerpFontVariations(null, null, 1.0));

        FontVariation w100 = FontVariation.Weight(100.0);
        FontVariation w120 = FontVariation.Weight(120.0);
        FontVariation w150 = FontVariation.Weight(150.0);
        FontVariation w200 = FontVariation.Weight(200.0);
        FontVariation w300 = FontVariation.Weight(300.0);
        FontVariation w1000 = FontVariation.Weight(1000.0);

        // one axis
        Assert.Equal([w100], TextStyle.LerpFontVariations([w100], [w200], 0.0)!);
        Assert.Equal([w120], TextStyle.LerpFontVariations([w100], [w200], 0.2)!);
        Assert.Equal([w150], TextStyle.LerpFontVariations([w100], [w200], 0.5)!);
        Assert.Equal([w300], TextStyle.LerpFontVariations([w100], [w200], 2.0)!);

        // weird one axis cases
        Assert.Equal([w100, w1000], TextStyle.LerpFontVariations([w100, w1000], [w300], 0.0)!);
        Assert.Equal([w200], TextStyle.LerpFontVariations([w100, w1000], [w300], 0.5)!);
        Assert.Equal([w300], TextStyle.LerpFontVariations([w100, w1000], [w300], 1.0)!);
        Assert.Equal([], TextStyle.LerpFontVariations([w100, w1000], [], 0.5)!);

        FontVariation sn80 = FontVariation.Slant(-80.0);
        FontVariation sn40 = FontVariation.Slant(-40.0);
        FontVariation s0 = FontVariation.Slant(0.0);
        FontVariation sp40 = FontVariation.Slant(40.0);
        FontVariation sp80 = FontVariation.Slant(80.0);

        // two axis matched order
        Assert.Equal([w200, s0], TextStyle.LerpFontVariations([w100, sn80], [w300, sp80], 0.5)!);

        // two axis unmatched order
        Assert.Equal([sn80, w100], TextStyle.LerpFontVariations([sn80, w100], [w300, sp80], 0.0)!);
        AssertUnordered([s0, w200], TextStyle.LerpFontVariations([sn80, w100], [w300, sp80], 0.5)!);
        Assert.Equal([w300, sp80], TextStyle.LerpFontVariations([sn80, w100], [w300, sp80], 1.0)!);

        // two axis with duplicates
        AssertUnordered([sp80, w200], TextStyle.LerpFontVariations([sn80, w100, sp80], [w300, sp80], 0.5)!);

        // mixed axis counts
        Assert.Equal([w200], TextStyle.LerpFontVariations([sn80, w100], [w300], 0.5)!);
        Assert.Equal([sn80], TextStyle.LerpFontVariations([sn80], [w300], 0.0)!);
        Assert.Equal([sn80], TextStyle.LerpFontVariations([sn80], [w300], 0.1)!);
        Assert.Equal([w300], TextStyle.LerpFontVariations([sn80], [w300], 0.9)!);
        Assert.Equal([w300], TextStyle.LerpFontVariations([sn80], [w300], 1.0)!);
        IReadOnlyList<FontVariation> mixed = TextStyle.LerpFontVariations([sn40, s0, w100], [sp40, w300, sp80], 0.5)!;
        Assert.True(
            mixed.SequenceEqual([s0, w200, sp40]) || mixed.SequenceEqual([s0, sp40, w200]),
            string.Join(", ", mixed));
    }

    // -- dart:ui (engine text_test.dart) ------------------------------------------------------------

    [Fact]
    public void FontWeightLerp()
    {
        Assert.Equal(W(500), TextStyle.LerpFontWeight(W(400), W(600), 0.5));
        Assert.Null(TextStyle.LerpFontWeight(null, null, 0));
        Assert.Equal(W(400), TextStyle.LerpFontWeight(null, W(400), 0));
        Assert.Equal(W(400), TextStyle.LerpFontWeight(W(400), null, 1));
        // Not snapped to a multiple of 100, clamped to w100–w900.
        Assert.Equal(W(450), TextStyle.LerpFontWeight(W(400), W(500), 0.5));
        Assert.Equal(W(900), TextStyle.LerpFontWeight(W(900), W(950), 1.0));
    }

    [Fact]
    public void UiTextStyleToString()
    {
        Assert.Equal(
            UiTextStyleString(fontWeight: "FontWeight.w700", fontSize: "12.0", height: "123.0x"),
            new ParagraphTextStyle(FontWeight: W(700), FontSize: 12.0, Height: 123.0).ToString());
        Assert.Equal(
            UiTextStyleString(
                color: Color.FromUInt32(0xFF00FF00).ToDartString(),
                fontWeight: "FontWeight.w800",
                fontSize: "10.0",
                height: "100.0x"),
            new ParagraphTextStyle(
                Color: Color.FromUInt32(0xFF00FF00),
                FontWeight: W(800),
                FontSize: 10.0,
                Height: 100.0).ToString());
        Assert.Equal(
            UiTextStyleString(fontFamily: "test"),
            new ParagraphTextStyle(FontFamily: new FontFamily("test")).ToString());
        Assert.Equal(
            UiTextStyleString(fontFamily: "foo", fontFamilyFallback: "[Roboto, test]"),
            new ParagraphTextStyle(FontFamily: new FontFamily("foo"), FontFamilyFallback: ["Roboto", "test"])
                .ToString());
        Assert.Equal(
            UiTextStyleString(leadingDistribution: "TextLeadingDistribution.even"),
            new ParagraphTextStyle(LeadingDistribution: TextLeadingDistribution.Even).ToString());
        Assert.Equal(
            UiTextStyleString(height: "kTextHeightNone"),
            new ParagraphTextStyle(Height: TextDefaults.TextHeightNone).ToString());
    }

    [Fact]
    public void FontFeatureClass()
    {
        Assert.Equal(new FontFeature("aalt"), FontFeature.Alternative(1));
        Assert.Equal(new FontFeature("aalt", 5), FontFeature.Alternative(5));
        Assert.Equal(new FontFeature("afrc"), FontFeature.AlternativeFractions());
        Assert.Equal(new FontFeature("calt"), FontFeature.ContextualAlternates());
        Assert.Equal(new FontFeature("case"), FontFeature.CaseSensitiveForms());
        Assert.Equal(new FontFeature("cv01"), FontFeature.CharacterVariant(1));
        Assert.Equal(new FontFeature("cv18"), FontFeature.CharacterVariant(18));
        Assert.Equal(new FontFeature("cv99"), FontFeature.CharacterVariant(99));
        Assert.Equal(new FontFeature("dnom"), FontFeature.Denominator());
        Assert.Equal(new FontFeature("frac"), FontFeature.Fractions());
        Assert.Equal(new FontFeature("hist"), FontFeature.HistoricalForms());
        Assert.Equal(new FontFeature("hlig"), FontFeature.HistoricalLigatures());
        Assert.Equal(new FontFeature("lnum"), FontFeature.LiningFigures());
        Assert.Equal(new FontFeature("locl"), FontFeature.LocaleAware());
        Assert.Equal(new FontFeature("locl", 0), FontFeature.LocaleAware(enable: false));
        Assert.Equal(new FontFeature("nalt"), FontFeature.NotationalForms());
        Assert.Equal(new FontFeature("nalt", 5), FontFeature.NotationalForms(5));
        Assert.Equal(new FontFeature("numr"), FontFeature.Numerators());
        Assert.Equal(new FontFeature("onum"), FontFeature.OldstyleFigures());
        Assert.Equal(new FontFeature("ordn"), FontFeature.OrdinalForms());
        Assert.Equal(new FontFeature("pnum"), FontFeature.ProportionalFigures());
        Assert.Equal(new FontFeature("rand"), FontFeature.Randomize());
        Assert.Equal(new FontFeature("salt"), FontFeature.StylisticAlternates());
        Assert.Equal(new FontFeature("sinf"), FontFeature.ScientificInferiors());
        Assert.Equal(new FontFeature("ss01"), FontFeature.StylisticSet(1));
        Assert.Equal(new FontFeature("ss18"), FontFeature.StylisticSet(18));
        Assert.Equal(new FontFeature("subs"), FontFeature.Subscripts());
        Assert.Equal(new FontFeature("sups"), FontFeature.Superscripts());
        Assert.Equal(new FontFeature("swsh"), FontFeature.Swash());
        Assert.Equal(new FontFeature("swsh", 0), FontFeature.Swash(0));
        Assert.Equal(new FontFeature("swsh", 5), FontFeature.Swash(5));
        Assert.Equal(new FontFeature("tnum"), FontFeature.TabularFigures());
        Assert.Equal(new FontFeature("zero"), FontFeature.SlashedZero());
        Assert.Equal(new FontFeature("TEST"), FontFeature.Enable("TEST"));
        Assert.Equal(new FontFeature("TEST", 0), FontFeature.Disable("TEST"));
        Assert.Equal("FEAT", new FontFeature("FEAT", 1000).Feature);
        Assert.Equal(1000, new FontFeature("FEAT", 1000).Value);
        Assert.Equal("FontFeature('FEAT', 1000)", new FontFeature("FEAT", 1000).ToString());
    }

    [Fact]
    public void FontVariationConstructorsAndLerp()
    {
        Assert.Equal("wght", FontVariation.Weight(123.0).Axis);
        Assert.Equal(123.0, FontVariation.Weight(123.0).Value);
        Assert.Equal("wdth", FontVariation.Width(123.0).Axis);
        Assert.Equal("slnt", FontVariation.Slant(45.0).Axis);
        Assert.Equal("opsz", FontVariation.OpticalSize(67.0).Axis);
        Assert.Equal("ital", FontVariation.Italic(0.8).Axis);
        Assert.Equal(0.8, FontVariation.Italic(0.8).Value);
        Assert.Equal("FontVariation('wght', 123.0)", FontVariation.Weight(123.0).ToString());

        Assert.Equal(
            FontVariation.Weight(200.0),
            FontVariation.Lerp(FontVariation.Weight(100.0), FontVariation.Weight(300.0), 0.5));
        Assert.Equal(
            FontVariation.Slant(-20.0),
            FontVariation.Lerp(FontVariation.Slant(0.0), FontVariation.Slant(-80.0), 0.25));
        Assert.Equal(
            FontVariation.Width(90.0),
            FontVariation.Lerp(FontVariation.Width(90.0), FontVariation.Italic(0.2), 0.1));
        Assert.Equal(
            FontVariation.Italic(0.2),
            FontVariation.Lerp(FontVariation.Width(90.0), FontVariation.Italic(0.2), 0.9));
    }

    [Fact]
    public void PaintToString()
    {
        Assert.Equal("Paint()", new Paint().ToString());
        Assert.Equal(
            $"Paint({Color.FromUInt32(0xFFFF0000).ToDartString()})",
            new Paint { Color = Color.FromUInt32(0xFFFF0000) }.ToString());
        Assert.Equal(
            "Paint(PaintingStyle.stroke 2.0 StrokeCap.round StrokeJoin.bevel; antialias off; "
            + "BlendMode.srcIn; maskFilter: MaskFilter.blur(BlurStyle.normal, 1.5); invert: true)",
            new Paint
            {
                Style = PaintingStyle.Stroke,
                StrokeWidth = 2.0,
                StrokeCap = StrokeCap.Round,
                StrokeJoin = StrokeJoin.Bevel,
                IsAntiAlias = false,
                BlendMode = BlendMode.SourceIn,
                MaskFilter = MaskFilter.Blur(BlurStyle.Normal, 1.5),
                InvertColors = true,
            }.ToString());
        Assert.Equal(
            "Paint(PaintingStyle.stroke hairline StrokeJoin.miter up to 6.0)",
            new Paint { Style = PaintingStyle.Stroke, StrokeMiterLimit = 6.0 }.ToString());
    }

    // -- helpers --------------------------------------------------------------------------------------

    private static void AssertUnordered(IReadOnlyList<FontVariation> expected, IReadOnlyList<FontVariation> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        foreach (FontVariation variation in expected)
        {
            Assert.Contains(variation, actual);
        }
    }

    // Dart's `ui.TextStyle.toString` shape, every argument defaulting to `unspecified`.
    private static string UiTextStyleString(
        string color = "unspecified",
        string fontWeight = "unspecified",
        string fontFamily = "unspecified",
        string fontFamilyFallback = "unspecified",
        string fontSize = "unspecified",
        string height = "unspecified",
        string leadingDistribution = "unspecified")
    {
        return $"TextStyle(color: {color}, decoration: unspecified, decorationColor: unspecified, "
               + "decorationStyle: unspecified, decorationThickness: unspecified, "
               + $"fontWeight: {fontWeight}, fontStyle: unspecified, textBaseline: unspecified, "
               + $"fontFamily: {fontFamily}, fontFamilyFallback: {fontFamilyFallback}, fontSize: {fontSize}, "
               + "letterSpacing: unspecified, wordSpacing: unspecified, "
               + $"height: {height}, leadingDistribution: {leadingDistribution}, locale: unspecified, "
               + "background: unspecified, foreground: unspecified, shadows: unspecified, "
               + "fontFeatures: unspecified, fontVariations: unspecified)";
    }
}
