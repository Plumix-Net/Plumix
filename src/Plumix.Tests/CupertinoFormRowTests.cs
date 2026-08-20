using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: cupertino_ui/test/form_row_test.dart

public sealed class CupertinoFormRowTests
{
    private static readonly Size ViewSize = new(320.0, 180.0);

    [Fact]
    public void Constructor_ExposesSourceApiAndDefaultPadding()
    {
        var child = new Text("Child");
        var row = new CupertinoFormRow(child);

        Assert.Same(child, row.Child);
        Assert.Null(row.Prefix);
        Assert.Null(row.Padding);
        Assert.Null(row.Helper);
        Assert.Null(row.Error);
        Assert.Throws<ArgumentNullException>(() => new CupertinoFormRow(null!));

        using var harness = new CupertinoThemeTestHarness(Wrap(row));
        harness.Pump(ViewSize);
        Padding padding = Assert.Single(harness.FindWidgets<Padding>());
        Assert.Equal(
            EdgeInsetsGeometry.DirectionalOnly(start: 20.0, top: 6.0, end: 6.0, bottom: 6.0),
            padding.InsetsGeometry);
    }

    [Fact]
    public void Build_ShowsContentInSourceOrderAndAppliesHelperAndErrorStyles()
    {
        var prefix = new Text("Prefix");
        var child = new Text("Child");
        var helper = new Text("Helper");
        var error = new Text("Error");
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoFormRow(
            child: child,
            prefix: prefix,
            helper: helper,
            error: error)));

        harness.Pump(ViewSize);

        Assert.Contains(harness.FindWidgets<Text>(), text => ReferenceEquals(text, prefix));
        Assert.Contains(harness.FindWidgets<Text>(), text => ReferenceEquals(text, child));
        Assert.Contains(harness.FindWidgets<Text>(), text => ReferenceEquals(text, helper));
        Assert.Contains(harness.FindWidgets<Text>(), text => ReferenceEquals(text, error));

        DefaultTextStyle helperStyle = Assert.Single(
            harness.FindWidgets<DefaultTextStyle>(),
            style => ReferenceEquals(style.Child, helper));
        Assert.Equal(CupertinoColors.Label.Color, helperStyle.Style.Color);
        DefaultTextStyle errorStyle = Assert.Single(
            harness.FindWidgets<DefaultTextStyle>(),
            style => ReferenceEquals(style.Child, error));
        Assert.Equal(CupertinoColors.DestructiveRed.Color, errorStyle.Style.Color);
        Assert.Equal(FontWeight.Medium, errorStyle.Style.FontWeight);

        RenderParagraph helperParagraph = Assert.IsType<RenderParagraph>(FindParagraph(harness.RenderView, "Helper"));
        RenderParagraph errorParagraph = Assert.IsType<RenderParagraph>(FindParagraph(harness.RenderView, "Error"));
        Assert.True(
            errorParagraph.LocalToGlobal(default).Y > helperParagraph.LocalToGlobal(default).Y,
            "The helper must be laid out above the error.");
    }

    [Theory]
    [InlineData(TextDirection.Ltr, false)]
    [InlineData(TextDirection.Rtl, true)]
    public void Layout_UsesDirectionalPrefixAndTrailingChild(TextDirection direction, bool prefixAfterChild)
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoFormRow(
                child: new Text("Child"),
                prefix: new Text("Prefix")),
            direction: direction));

        harness.Pump(ViewSize);

        RenderParagraph prefix = Assert.IsType<RenderParagraph>(FindParagraph(harness.RenderView, "Prefix"));
        RenderParagraph child = Assert.IsType<RenderParagraph>(FindParagraph(harness.RenderView, "Child"));
        bool actualPrefixAfterChild = prefix.LocalToGlobal(default).X > child.LocalToGlobal(default).X;
        Assert.Equal(prefixAfterChild, actualPrefixAfterChild);
    }

    [Theory]
    [InlineData(PlatformBrightness.Light)]
    [InlineData(PlatformBrightness.Dark)]
    public void Build_ResolvesPrefixAndHelperLabelColorForBrightness(PlatformBrightness brightness)
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoFormRow(
                child: new Text("Child"),
                prefix: new Text("Prefix"),
                helper: new Text("Helper")),
            brightness: brightness));

        harness.Pump(ViewSize);

        Color expected = brightness == PlatformBrightness.Light
            ? CupertinoColors.Label.Color
            : CupertinoColors.Label.DarkColor;
        RenderParagraph prefix = Assert.IsType<RenderParagraph>(FindParagraph(harness.RenderView, "Prefix"));
        RenderParagraph helper = Assert.IsType<RenderParagraph>(FindParagraph(harness.RenderView, "Helper"));
        Assert.Equal(expected, Assert.IsType<SolidColorBrush>(prefix.Foreground).Color);
        Assert.Equal(expected, Assert.IsType<SolidColorBrush>(helper.Foreground).Color);
    }

    [Fact]
    public void Layout_ZeroAreaDoesNotCrash()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(new SizedBox(
            width: 0.0,
            height: 0.0,
            child: new CupertinoFormRow(child: new Text("X")))));

        harness.Pump(ViewSize);

        Assert.Contains(
            FindAll<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints == BoxConstraints.Tight(default) && box.Size == default);
    }

    private static Widget Wrap(
        Widget child,
        TextDirection direction = TextDirection.Ltr,
        PlatformBrightness brightness = PlatformBrightness.Light)
    {
        return new MediaQuery(
            data: new MediaQueryData(PlatformBrightness: brightness),
            child: new Localizations(
                locale: new Locale("en", "US"),
                delegates:
                [
                    DefaultWidgetsLocalizations.Delegate,
                    DefaultCupertinoLocalizations.Delegate,
                ],
                child: new Directionality(
                    direction,
                    new CupertinoTheme(
                        new CupertinoThemeData(brightness: brightness),
                        new Center(child: child)))));
    }

    private static RenderParagraph? FindParagraph(RenderObject? root, string text)
    {
        return FindAll<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.PlainText == text);
    }

    private static IReadOnlyList<T> FindAll<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null)
        {
            return result;
        }

        if (root is T typed)
        {
            result.Add(typed);
        }

        root.VisitChildren(child => result.AddRange(FindAll<T>(child)));
        return result;
    }
}
