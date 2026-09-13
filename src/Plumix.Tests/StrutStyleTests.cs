using Avalonia.Media;
using Plumix.Painting;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/test/painting/strut_style_test.dart

public sealed class StrutStyleTests
{
    [Fact]
    public void Diagnostics_MatchDartToString()
    {
        Assert.Equal(
            "StrutStyle(family: Serif, size: 14.0)",
            new StrutStyle(FontFamily: new FontFamily("Serif"), FontSize: 14).ToString());

        var s1 = new StrutStyle(FontFamily: new FontFamily("Serif"), FontSize: 14, ForceStrutHeight: true);
        Assert.Equal("Serif", s1.FontFamily!.Name);
        Assert.Equal(14.0, s1.FontSize);
        Assert.Equal(s1, s1);
        Assert.Equal("StrutStyle(family: Serif, size: 14.0, <strut height forced>)", s1.ToString());

        var s2 = new StrutStyle(FontFamily: new FontFamily("Serif"), FontSize: 14, ForceStrutHeight: false);
        Assert.Equal("StrutStyle(family: Serif, size: 14.0, <strut height normal>)", s2.ToString());

        Assert.Equal("StrutStyle", new StrutStyle().ToString());
        Assert.Equal("StrutStyle(<strut height normal>)", new StrutStyle(ForceStrutHeight: false).ToString());
        Assert.Equal("StrutStyle(<strut height forced>)", new StrutStyle(ForceStrutHeight: true).ToString());

        var even = new StrutStyle(Height: 14, LeadingDistribution: TextLeadingDistribution.Even);
        Assert.Equal("StrutStyle(height: 14.0x, leadingDistribution: even)", even.ToString());
        var proportional = new StrutStyle(Height: 14, LeadingDistribution: TextLeadingDistribution.Proportional);
        Assert.Equal("StrutStyle(height: 14.0x, leadingDistribution: proportional)", proportional.ToString());
        Assert.NotEqual(even, proportional);
    }

    [Fact]
    public void LeadingDistribution_IsIgnoredWithoutHeight()
    {
        var a = new StrutStyle(LeadingDistribution: TextLeadingDistribution.Even);
        var b = new StrutStyle(LeadingDistribution: TextLeadingDistribution.Proportional);
        Assert.Equal(a, b);
        Assert.Equal(RenderComparison.Identical, a.CompareTo(b));
        Assert.Equal(
            RenderComparison.Layout,
            new StrutStyle(Height: 1, LeadingDistribution: TextLeadingDistribution.Even)
                .CompareTo(new StrutStyle(Height: 1, LeadingDistribution: TextLeadingDistribution.Proportional)));
    }

    [Fact]
    public void CompareTo_ReportsLayoutOrIdentical()
    {
        var style = new StrutStyle(FontSize: 10, FontFamilyFallback: ["a"]);
        Assert.Equal(RenderComparison.Identical, style.CompareTo(style));
        Assert.Equal(
            RenderComparison.Identical,
            style.CompareTo(new StrutStyle(FontSize: 10, FontFamilyFallback: ["a"])));
        Assert.Equal(RenderComparison.Layout, style.CompareTo(new StrutStyle(FontSize: 11, FontFamilyFallback: ["a"])));
        Assert.Equal(RenderComparison.Layout, style.CompareTo(new StrutStyle(FontSize: 10, FontFamilyFallback: ["b"])));
        Assert.Equal(RenderComparison.Layout, style.CompareTo(style.Merge(new StrutStyle(Leading: 1))));
        Assert.Equal(
            RenderComparison.Identical,
            style.CompareTo(new StrutStyle(FontSize: 10, FontFamilyFallback: ["a"], DebugLabel: "x")));
    }

    [Fact]
    public void Disabled_HasZeroHeightAndLeading()
    {
        Assert.Equal(0.0, StrutStyle.Disabled.Height);
        Assert.Equal(0.0, StrutStyle.Disabled.Leading);
        Assert.Equal(StrutStyle.Disabled, new StrutStyle(Height: 0.0, Leading: 0.0));
    }

    [Fact]
    public void Constructor_AssertsAndPackagePrefix()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new StrutStyle(FontSize: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new StrutStyle(Leading: -1));
        Assert.Throws<ArgumentException>(() => new StrutStyle(Package: "p"));

        var style = new StrutStyle(FontFamily: new FontFamily("F"), FontFamilyFallback: ["G"], Package: "p");
        Assert.Equal("packages/p/F", style.FontFamily!.Name);
        Assert.Equal(["packages/p/G"], style.FontFamilyFallback!);

        var fallbackOnly = new StrutStyle(FontFamilyFallback: ["G"], Package: "p");
        Assert.Equal("packages/p/null", fallbackOnly.FontFamily!.Name);
    }

    [Fact]
    public void FromTextStyle_TakesTextStyleValuesButNotLeadingOrForce()
    {
        var textStyle = new TextStyle(
            FontFamily: new FontFamily("Serif"),
            FontSize: 12,
            FontWeight: FontWeight.Bold,
            FontStyle: FontStyle.Italic,
            Height: 1.5,
            LeadingDistribution: TextLeadingDistribution.Even,
            FontFamilyFallback: ["Fallback"]);
        StrutStyle strut = StrutStyle.FromTextStyle(textStyle);
        Assert.Equal("Serif", strut.FontFamily!.Name);
        Assert.Equal(12, strut.FontSize);
        Assert.Equal(FontWeight.Bold, strut.FontWeight);
        Assert.Equal(FontStyle.Italic, strut.FontStyle);
        Assert.Equal(1.5, strut.Height);
        Assert.Equal(TextLeadingDistribution.Even, strut.LeadingDistribution);
        Assert.Equal(["Fallback"], strut.FontFamilyFallback!);
        Assert.Null(strut.Leading);
        Assert.Null(strut.ForceStrutHeight);

        StrutStyle overridden = StrutStyle.FromTextStyle(textStyle, fontSize: 20, leading: 0.5, forceStrutHeight: true);
        Assert.Equal(20, overridden.FontSize);
        Assert.Equal(0.5, overridden.Leading);
        Assert.True(overridden.ForceStrutHeight);

        StrutStyle packaged = StrutStyle.FromTextStyle(textStyle, fontFamily: new FontFamily("F"), package: "p");
        Assert.Equal("packages/p/packages/p/F", packaged.FontFamily!.Name);
    }

    [Fact]
    public void InheritFromTextStyle_FillsNullsAndGatesLeadingDistribution()
    {
        var strut = new StrutStyle(FontSize: 10, Leading: 1);
        Assert.Same(strut, strut.InheritFromTextStyle(null));

        StrutStyle noHeight = strut.InheritFromTextStyle(
            new TextStyle(FontWeight: FontWeight.Bold, LeadingDistribution: TextLeadingDistribution.Even));
        Assert.Equal(10, noHeight.FontSize);
        Assert.Equal(FontWeight.Bold, noHeight.FontWeight);
        Assert.Null(noHeight.Height);
        Assert.Null(noHeight.LeadingDistribution);
        Assert.Equal(1, noHeight.Leading);

        StrutStyle withHeight = strut.InheritFromTextStyle(
            new TextStyle(FontSize: 30, Height: 2, LeadingDistribution: TextLeadingDistribution.Even));
        Assert.Equal(10, withHeight.FontSize);
        Assert.Equal(2, withHeight.Height);
        Assert.Equal(TextLeadingDistribution.Even, withHeight.LeadingDistribution);
    }

    [Fact]
    public void Merge_OtherWins()
    {
        var a = new StrutStyle(FontSize: 10, Height: 1, ForceStrutHeight: false, DebugLabel: "a");
        Assert.Same(a, a.Merge(null));
        StrutStyle merged = a.Merge(new StrutStyle(FontSize: 20, Leading: 2, ForceStrutHeight: true));
        Assert.Equal(20, merged.FontSize);
        Assert.Equal(1, merged.Height);
        Assert.Equal(2, merged.Leading);
        Assert.True(merged.ForceStrutHeight);
        Assert.Equal("a", merged.DebugLabel);
    }

    [Fact]
    public void HashCode_IgnoresFallbackDistributionAndLabel()
    {
        var a = new StrutStyle(FontSize: 10, FontFamilyFallback: ["a"], DebugLabel: "a");
        var b = new StrutStyle(FontSize: 10, FontFamilyFallback: ["b"], DebugLabel: "b");
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, b);
    }
}
