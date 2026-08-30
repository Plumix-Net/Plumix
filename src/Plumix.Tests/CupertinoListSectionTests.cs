using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class CupertinoListSectionTests
{
    private static readonly Size ViewSize = new(360.0, 640.0);

    [Fact]
    public void Constructors_ExposeSourceDefaultsAndValidateContent()
    {
        var child = new Text("Child");
        var baseSection = new CupertinoListSection(children: [child]);

        Assert.Equal(CupertinoListSectionType.Base, baseSection.Type);
        Assert.Equal(EdgeInsetsGeometry.Only(bottom: 8.0), baseSection.Margin);
        Assert.Equal(CupertinoColors.SystemGroupedBackground, baseSection.BackgroundColor);
        Assert.Equal(Clip.None, baseSection.ClipBehavior);
        Assert.Equal(20.0, baseSection.DividerMargin);
        Assert.Equal(44.0, baseSection.AdditionalDividerMargin);
        Assert.Equal(22.0, baseSection.TopMargin);

        var inset = CupertinoListSection.InsetGrouped(children: [child]);
        Assert.Equal(CupertinoListSectionType.InsetGrouped, inset.Type);
        Assert.Equal(
            EdgeInsetsGeometry.DirectionalOnly(start: 20.0, top: 20.0, end: 20.0, bottom: 10.0),
            inset.Margin);
        Assert.Equal(Clip.HardEdge, inset.ClipBehavior);
        Assert.Equal(14.0, inset.DividerMargin);
        Assert.Equal(42.0, inset.AdditionalDividerMargin);
        Assert.Null(inset.TopMargin);

        var insetWithoutLeading = CupertinoListSection.InsetGrouped(
            children: [child],
            header: new Text("Header"),
            hasLeading: false);
        Assert.Equal(
            EdgeInsetsGeometry.DirectionalOnly(start: 20.0, end: 20.0, bottom: 10.0),
            insetWithoutLeading.Margin);
        Assert.Equal(14.0, insetWithoutLeading.AdditionalDividerMargin);

        Assert.Throws<ArgumentException>(() => new CupertinoListSection());
        Assert.Throws<ArgumentException>(() => CupertinoListSection.InsetGrouped());
    }

    [Theory]
    [InlineData(false, 1, 3)]
    [InlineData(false, 2, 5)]
    [InlineData(true, 1, 1)]
    [InlineData(true, 2, 3)]
    public void Build_InsertsSourceDividerStructure(bool insetGrouped, int rowCount, int composedCount)
    {
        Widget[] rows = Enumerable.Range(0, rowCount)
            .Select(index => (Widget)new Text($"Row {index}"))
            .ToArray();
        CupertinoListSection section = insetGrouped
            ? CupertinoListSection.InsetGrouped(children: rows)
            : new CupertinoListSection(children: rows);
        using var harness = new CupertinoThemeTestHarness(Wrap(section));
        harness.Pump(ViewSize);

        Column rowsColumn = Assert.Single(
            harness.FindWidgets<Column>(),
            column => column.Children.Any(child => ReferenceEquals(child, rows[0])));
        Assert.Equal(composedCount, rowsColumn.Children.Count);
    }

    [Fact]
    public void Build_ShowsHeaderFooterAndAppliesSourceTypography()
    {
        using var baseHarness = new CupertinoThemeTestHarness(Wrap(new CupertinoListSection(
            children: [new Text("Row")],
            header: new Text("Header"),
            footer: new Text("Footer"))));
        baseHarness.Pump(ViewSize);

        RenderParagraph baseHeader = Assert.IsType<RenderParagraph>(FindParagraph(baseHarness.RenderView, "Header"));
        Assert.Equal(13.0, baseHeader.FontSize);
        Assert.Equal(0xFF6C6C6Cu, Assert.IsType<SolidColorBrush>(baseHeader.Foreground).Color.ToUInt32());
        Assert.NotNull(FindParagraph(baseHarness.RenderView, "Footer"));

        using var insetHarness = new CupertinoThemeTestHarness(Wrap(CupertinoListSection.InsetGrouped(
            children: [new Text("Row")],
            header: new Text("Inset header"),
            footer: new Text("Inset footer"))));
        insetHarness.Pump(ViewSize);

        RenderParagraph insetHeader = Assert.IsType<RenderParagraph>(
            FindParagraph(insetHarness.RenderView, "Inset header"));
        Assert.Equal(20.0, insetHeader.FontSize);
        Assert.Equal(FontWeight.Bold, insetHeader.FontWeight);
        Assert.NotNull(FindParagraph(insetHarness.RenderView, "Inset footer"));
    }

    [Fact]
    public void Build_ResolvesBackgroundSeparatorAndDevicePixelDividerHeight()
    {
        Color background = Color.FromUInt32(0xFF123456);
        Color separator = Color.FromUInt32(0xFF8FC133);
        var section = new CupertinoListSection(
            children: [new Text("One"), new Text("Two")],
            backgroundColor: background,
            separatorColor: separator);
        using var harness = new CupertinoThemeTestHarness(Wrap(section, devicePixelRatio: 2.0));
        harness.Pump(ViewSize);

        Assert.Contains(
            harness.FindWidgets<DecoratedBox>(),
            box => box.Decoration is BoxDecoration { Color: { } color } && color == background);
        Container[] dividers = harness.FindWidgets<Container>()
            .Where(container => container.Color == separator)
            .ToArray();
        Assert.Equal(3, dividers.Length);
        Assert.All(dividers, divider => Assert.Equal(0.5, divider.Constraints?.MaxHeight));
        Assert.Equal(
            EdgeInsetsGeometry.DirectionalOnly(start: 64.0),
            Assert.Single(dividers, divider => divider.Margin.HasValue).Margin);
    }

    [Fact]
    public void Build_DefaultDynamicColorsResolveForDarkBrightness()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoListSection(
                children: [new Text("One"), new Text("Two")],
                header: new Text("Header")),
            brightness: PlatformBrightness.Dark));
        harness.Pump(ViewSize);

        Color separator = CupertinoColors.Separator.DarkColor;
        Assert.Equal(
            3,
            harness.FindWidgets<Container>().Count(container => container.Color == separator));
        Assert.Contains(
            harness.FindWidgets<DecoratedBox>(),
            box => box.Decoration is BoxDecoration { Color: { } color }
                && color == CupertinoColors.SystemGroupedBackground.DarkColor);
        RenderParagraph header = Assert.IsType<RenderParagraph>(FindParagraph(harness.RenderView, "Header"));
        Assert.Equal(0xFF8E8E92u, Assert.IsType<SolidColorBrush>(header.Foreground).Color.ToUInt32());
    }

    [Fact]
    public void ClipBehavior_OnlyComposesRoundedSuperellipseWhenRequested()
    {
        using var baseHarness = new CupertinoThemeTestHarness(Wrap(new CupertinoListSection(
            children: [new Text("Row")])));
        baseHarness.Pump(ViewSize);
        Assert.Empty(baseHarness.FindWidgets<ClipRSuperellipse>());

        using var clippedHarness = new CupertinoThemeTestHarness(Wrap(new CupertinoListSection(
            children: [new Text("Row")],
            clipBehavior: Clip.AntiAlias)));
        clippedHarness.Pump(ViewSize);
        ClipRSuperellipse clip = Assert.Single(clippedHarness.FindWidgets<ClipRSuperellipse>());
        Assert.Equal(BorderRadius.Zero, clip.BorderRadius);
        Assert.Equal(Clip.AntiAlias, clip.ClipBehavior);

        using var insetHarness = new CupertinoThemeTestHarness(Wrap(CupertinoListSection.InsetGrouped(
            children: [new Text("Row")])));
        insetHarness.Pump(ViewSize);
        ClipRSuperellipse insetClip = Assert.Single(insetHarness.FindWidgets<ClipRSuperellipse>());
        Assert.Equal(BorderRadius.Circular(10.0), insetClip.BorderRadius);
        Assert.Equal(Clip.HardEdge, insetClip.ClipBehavior);
    }

    [Fact]
    public void Layout_CustomMarginOffsetsRowsDirectionally()
    {
        var defaultRow = new CupertinoListTile(title: new Text("Default row"));
        using var defaultHarness = new CupertinoThemeTestHarness(Wrap(new CupertinoListSection(
            children: [defaultRow],
            header: new Text("Header"))));
        defaultHarness.Pump(ViewSize);
        RenderParagraph defaultParagraph = Assert.IsType<RenderParagraph>(
            FindParagraph(defaultHarness.RenderView, "Default row"));

        var customRow = new CupertinoListTile(title: new Text("Custom row"));
        using var customHarness = new CupertinoThemeTestHarness(Wrap(new CupertinoListSection(
            children: [customRow],
            header: new Text("Header"),
            margin: EdgeInsetsGeometry.All(10.0))));
        customHarness.Pump(ViewSize);
        RenderParagraph customParagraph = Assert.IsType<RenderParagraph>(
            FindParagraph(customHarness.RenderView, "Custom row"));

        Point defaultPosition = defaultParagraph.LocalToGlobal(default);
        Point customPosition = customParagraph.LocalToGlobal(default);
        Assert.Equal(10.0, customPosition.X - defaultPosition.X, precision: 5);
        Assert.Equal(10.0, customPosition.Y - defaultPosition.Y, precision: 5);
    }

    [Fact]
    public void ZeroArea_HeaderOnlySectionLaysOutWithoutCrashing()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(new SizedBox(
            width: 0.0,
            height: 0.0,
            child: new CupertinoListSection(header: new Text("X")))));
        harness.Pump(ViewSize);

        Assert.Contains(
            FindAll<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints == BoxConstraints.Tight(default) && box.Size == default);
    }

    private static Widget Wrap(
        Widget child,
        PlatformBrightness brightness = PlatformBrightness.Light,
        double devicePixelRatio = 1.0)
    {
        return new MediaQuery(
            data: new MediaQueryData(
                DevicePixelRatio: devicePixelRatio,
                PlatformBrightness: brightness),
            child: new Localizations(
                locale: new Locale("en", "US"),
                delegates:
                [
                    DefaultWidgetsLocalizations.Delegate,
                    DefaultCupertinoLocalizations.Delegate,
                ],
                child: new Directionality(
                    TextDirection.Ltr,
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
