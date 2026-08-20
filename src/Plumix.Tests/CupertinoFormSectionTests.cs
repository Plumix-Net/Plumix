using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: cupertino_ui/test/form_section_test.dart

public sealed class CupertinoFormSectionTests
{
    private static readonly Size ViewSize = new(360.0, 640.0);

    [Fact]
    public void Constructors_ExposeSourceDefaultsAndRejectEmptyChildren()
    {
        var child = new Text("Child");
        var section = new CupertinoFormSection(children: [child]);

        Assert.Same(child, Assert.Single(section.Children));
        Assert.Null(section.Header);
        Assert.Null(section.Footer);
        Assert.Equal(EdgeInsetsGeometry.Zero, section.Margin);
        Assert.Equal(CupertinoColors.SystemGroupedBackground, section.BackgroundColor);
        Assert.Null(section.Decoration);
        Assert.Equal(Clip.None, section.ClipBehavior);

        CupertinoFormSection inset = CupertinoFormSection.InsetGrouped(children: [child]);
        Assert.Equal(
            EdgeInsetsGeometry.DirectionalOnly(start: 20.0, end: 20.0, bottom: 10.0),
            inset.Margin);
        Assert.Equal(Clip.None, inset.ClipBehavior);

        Assert.Throws<ArgumentException>(() => new CupertinoFormSection(children: []));
        Assert.Throws<ArgumentException>(() => CupertinoFormSection.InsetGrouped(children: []));
    }

    [Fact]
    public void Build_ComposesListSectionWithNoLeadingInsetAndSourceHeaderFooterStyle()
    {
        var header = new Text("Header");
        var footer = new Text("Footer");
        using var baseHarness = new CupertinoThemeTestHarness(Wrap(new CupertinoFormSection(
            header: header,
            footer: footer,
            children: [new Text("Row")])));

        baseHarness.Pump(ViewSize);

        CupertinoListSection baseSection = Assert.Single(baseHarness.FindWidgets<CupertinoListSection>());
        Assert.Equal(CupertinoListSectionType.Base, baseSection.Type);
        Assert.Equal(0.0, baseSection.AdditionalDividerMargin);
        Assert.Equal(EdgeInsetsGeometry.Zero, baseSection.Margin);
        Assert.Same(header, Assert.Single(
            baseHarness.FindWidgets<DefaultTextStyle>(),
            style => ReferenceEquals(style.Child, header)).Child);
        RenderParagraph baseHeader = Assert.IsType<RenderParagraph>(FindParagraph(baseHarness.RenderView, "Header"));
        Assert.Equal(13.0, baseHeader.FontSize);
        Assert.Equal(
            CupertinoColors.SecondaryLabel.Color,
            Assert.IsType<SolidColorBrush>(baseHeader.Foreground).Color);
        Assert.NotNull(FindParagraph(baseHarness.RenderView, "Footer"));

        using var insetHarness = new CupertinoThemeTestHarness(Wrap(CupertinoFormSection.InsetGrouped(
            header: new Text("Inset header"),
            footer: new Text("Inset footer"),
            children: [new Text("Inset row")])));
        insetHarness.Pump(ViewSize);

        CupertinoListSection insetSection = Assert.Single(insetHarness.FindWidgets<CupertinoListSection>());
        Assert.Equal(CupertinoListSectionType.InsetGrouped, insetSection.Type);
        Assert.Equal(14.0, insetSection.AdditionalDividerMargin);
        RenderParagraph insetHeader = Assert.IsType<RenderParagraph>(
            FindParagraph(insetHarness.RenderView, "Inset header"));
        Assert.Equal(13.0, insetHeader.FontSize);
        Assert.NotEqual(FontWeight.Bold, insetHeader.FontWeight);
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
        CupertinoFormSection section = insetGrouped
            ? CupertinoFormSection.InsetGrouped(children: rows)
            : new CupertinoFormSection(children: rows);
        using var harness = new CupertinoThemeTestHarness(Wrap(section));

        harness.Pump(ViewSize);

        Column rowsColumn = Assert.Single(
            harness.FindWidgets<Column>(),
            column => column.Children.Any(child => ReferenceEquals(child, rows[0])));
        Assert.Equal(composedCount, rowsColumn.Children.Count);
    }

    [Fact]
    public void Build_AppliesBackgroundClipAndDarkHeaderColor()
    {
        Color background = Color.FromUInt32(0xFF123456);
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoFormSection(
                header: new Text("Header"),
                backgroundColor: background,
                clipBehavior: Clip.AntiAlias,
                children: [new Text("Row")]),
            brightness: PlatformBrightness.Dark));

        harness.Pump(ViewSize);

        Assert.Contains(
            harness.FindWidgets<DecoratedBox>(),
            box => box.Decoration is BoxDecoration { Color: { } color } && color == background);
        ClipRSuperellipse clip = Assert.Single(harness.FindWidgets<ClipRSuperellipse>());
        Assert.Equal(Clip.AntiAlias, clip.ClipBehavior);
        RenderParagraph header = Assert.IsType<RenderParagraph>(FindParagraph(harness.RenderView, "Header"));
        Assert.Equal(
            CupertinoColors.SecondaryLabel.DarkColor,
            Assert.IsType<SolidColorBrush>(header.Foreground).Color);
    }

    [Fact]
    public void Layout_CustomMarginOffsetsRowsAndZeroAreaDoesNotCrash()
    {
        var defaultLabel = new Text("Row");
        var defaultRow = new CupertinoFormRow(
            child: new SizedBox(width: 0.0, height: 0.0),
            prefix: defaultLabel);
        using var defaultHarness = new CupertinoThemeTestHarness(Wrap(new CupertinoFormSection(
            header: new Text("Header"),
            children: [defaultRow]),
            center: false));
        defaultHarness.Pump(ViewSize);
        RenderParagraph defaultParagraph = Assert.IsType<RenderParagraph>(
            FindParagraph(defaultHarness.RenderView, "Row"));

        var customLabel = new Text("Row");
        var customRow = new CupertinoFormRow(
            child: new SizedBox(width: 0.0, height: 0.0),
            prefix: customLabel);
        using var customHarness = new CupertinoThemeTestHarness(Wrap(new CupertinoFormSection(
            header: new Text("Header"),
            margin: EdgeInsetsGeometry.All(35.0),
            children: [customRow]),
            center: false));
        customHarness.Pump(ViewSize);
        RenderParagraph customParagraph = Assert.IsType<RenderParagraph>(
            FindParagraph(customHarness.RenderView, "Row"));

        Point defaultPosition = defaultParagraph.LocalToGlobal(default);
        Point customPosition = customParagraph.LocalToGlobal(default);
        Assert.Equal(35.0, customPosition.X - defaultPosition.X, precision: 5);
        Assert.Equal(35.0, customPosition.Y - defaultPosition.Y, precision: 5);

        using var zeroHarness = new CupertinoThemeTestHarness(Wrap(new SizedBox(
            width: 0.0,
            height: 0.0,
            child: new CupertinoFormSection(children: [new Text("X"), new Text("Y")]))));
        zeroHarness.Pump(ViewSize);
        Assert.Contains(
            FindAll<RenderConstrainedBox>(zeroHarness.RenderView),
            box => box.AdditionalConstraints == BoxConstraints.Tight(default) && box.Size == default);
    }

    private static Widget Wrap(
        Widget child,
        PlatformBrightness brightness = PlatformBrightness.Light,
        bool center = true)
    {
        Widget content = center ? new Center(child: child) : child;
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
                    TextDirection.Ltr,
                    new CupertinoTheme(
                        new CupertinoThemeData(brightness: brightness),
                        content))));
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
