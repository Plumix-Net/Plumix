using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Cupertino;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: cupertino_ui/test/list_tile_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoListTileTests : IDisposable
{
    private static readonly Size ViewSize = new(320.0, 240.0);

    public CupertinoListTileTests()
    {
        GestureBinding.Instance.ResetForTests();
    }

    public void Dispose()
    {
        GestureBinding.Instance.ResetForTests();
    }

    [Fact]
    public void Constructors_UseFlutterDefaultsAndRetainEverySlot()
    {
        var title = new Text("Title");
        var subtitle = new Text("Subtitle");
        var info = new Text("Info");
        var leading = new Text("Leading");
        var trailing = new Text("Trailing");
        var baseTile = new CupertinoListTile(
            title,
            subtitle,
            info,
            leading,
            trailing);
        CupertinoListTile notched = CupertinoListTile.Notched(title);

        Assert.Same(title, baseTile.Title);
        Assert.Same(subtitle, baseTile.Subtitle);
        Assert.Same(info, baseTile.AdditionalInfo);
        Assert.Same(leading, baseTile.Leading);
        Assert.Same(trailing, baseTile.Trailing);
        Assert.Null(baseTile.OnTap);
        Assert.Null(baseTile.BackgroundColor);
        Assert.Null(baseTile.BackgroundColorActivated);
        Assert.Null(baseTile.Padding);
        Assert.Equal(28.0, baseTile.LeadingSize);
        Assert.Equal(16.0, baseTile.LeadingToTitle);
        Assert.Equal(30.0, notched.LeadingSize);
        Assert.Equal(12.0, notched.LeadingToTitle);
    }

    [Fact]
    public void Build_ComposesSlotsTypographyAndSourceHeights()
    {
        var tile = new CupertinoListTile(
            title: new Text("Title"),
            subtitle: new Text("Subtitle"),
            additionalInfo: new Text("Info"),
            leading: new Icon(CupertinoIcons.Add),
            trailing: new CupertinoListTileChevron());
        using var harness = new CupertinoThemeTestHarness(Wrap(tile));

        harness.Pump(ViewSize);

        Assert.NotNull(FindParagraph(harness.RenderView, "Title"));
        RenderParagraph subtitle = Assert.IsType<RenderParagraph>(FindParagraph(harness.RenderView, "Subtitle"));
        Assert.Equal(12.0, subtitle.FontSize);
        Assert.NotNull(FindParagraph(harness.RenderView, "Info"));
        RenderColoredBox background = Assert.Single(FindAll<RenderColoredBox>(harness.RenderView));
        Assert.Equal(48.0, Assert.Single(FindAll<RenderConstrainedBox>(
            harness.RenderView,
            box => box.AdditionalConstraints.MinHeight == 48.0)).AdditionalConstraints.MinHeight);
        Assert.Equal(CupertinoColors.Transparent, background.Color);

        using var notchedHarness = new CupertinoThemeTestHarness(Wrap(CupertinoListTile.Notched(
            title: new Text("Notched"),
            subtitle: new Text("Notched subtitle"))));
        notchedHarness.Pump(ViewSize);
        RenderParagraph notchedTitle = Assert.IsType<RenderParagraph>(
            FindParagraph(notchedHarness.RenderView, "Notched"));
        RenderParagraph notchedSubtitle = Assert.IsType<RenderParagraph>(
            FindParagraph(notchedHarness.RenderView, "Notched subtitle"));
        Assert.Equal(16.0, notchedTitle.FontSize);
        Assert.Equal(FontWeight.DemiBold, notchedTitle.FontWeight);
        Assert.Equal(14.0, notchedSubtitle.FontSize);
        Assert.Contains(
            FindAll<RenderConstrainedBox>(notchedHarness.RenderView),
            box => box.AdditionalConstraints.MinHeight == 50.0);
    }

    [Fact]
    public void Build_ResolvesDynamicColorsForNormalActivatedAndChevronStates()
    {
        var normal = CupertinoDynamicColor.WithBrightness(
            Color.FromUInt32(0xFF112233),
            Color.FromUInt32(0xFF223344));
        var activated = CupertinoDynamicColor.WithBrightness(
            Color.FromUInt32(0xFF334455),
            Color.FromUInt32(0xFF445566));
        var completion = new TaskCompletionSource();
        var tile = new CupertinoListTile(
            title: new Text("Async tile"),
            trailing: new CupertinoListTileChevron(),
            backgroundColor: normal,
            backgroundColorActivated: activated,
            onTap: () => completion.Task);
        using var harness = new CupertinoThemeTestHarness(Wrap(tile, PlatformBrightness.Dark));
        harness.Pump(ViewSize);

        Assert.Equal(0xFF223344u, BackgroundColor(harness.RenderView).ToUInt32());
        RenderParagraph chevron = Assert.IsType<RenderParagraph>(FindParagraph(
            harness.RenderView,
            char.ConvertFromUtf32(CupertinoIcons.RightChevron.CodePoint)));
        Assert.Equal(17.0, chevron.FontSize);
        Assert.Equal(
            0xFF636366u,
            Assert.IsType<SolidColorBrush>(chevron.Foreground).Color.ToUInt32());

        Tap(harness.RenderView, new Point(160.0, 120.0), 801);
        harness.Pump(ViewSize);
        Assert.Equal(0xFF445566u, BackgroundColor(harness.RenderView).ToUInt32());

        completion.SetResult();
        harness.Pump(ViewSize);
        Assert.Equal(0xFF223344u, BackgroundColor(harness.RenderView).ToUInt32());
    }

    [Fact]
    public void OnTap_AddsGestureAndSemanticsWhileDisabledTileStaysPassive()
    {
        using var passiveHarness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoListTile(new Text("Passive"))));
        passiveHarness.Pump(ViewSize);
        Assert.Empty(passiveHarness.FindWidgets<GestureDetector>());

        int calls = 0;
        using var activeHarness = new CupertinoThemeTestHarness(Wrap(new CupertinoListTile(
            title: new Text("Active"),
            onTap: () =>
            {
                calls += 1;
                return Task.CompletedTask;
            })));
        SemanticsNode root = Assert.IsType<SemanticsNode>(activeHarness.PumpAndGetSemantics(ViewSize));
        Assert.Single(activeHarness.FindWidgets<GestureDetector>());
        SemanticsNode active = Assert.IsType<SemanticsNode>(FindSemantics(root, "Active"));
        Assert.True(active.Actions.HasFlag(SemanticsActions.Tap));

        Tap(activeHarness.RenderView, new Point(160.0, 120.0), 802);
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData(TextDirection.Ltr, true)]
    [InlineData(TextDirection.Rtl, false)]
    public void Layout_OrdersLeadingTitleInfoAndTrailingDirectionally(
        TextDirection direction,
        bool increasing)
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(
            new CupertinoListTile(
                title: new Text("Title"),
                additionalInfo: new Text("Info"),
                leading: new Text("Leading"),
                trailing: new Text("Trailing")),
            direction: direction));
        harness.Pump(ViewSize);

        double leading = GlobalX(FindParagraph(harness.RenderView, "Leading")!);
        double title = GlobalX(FindParagraph(harness.RenderView, "Title")!);
        double info = GlobalX(FindParagraph(harness.RenderView, "Info")!);
        double trailing = GlobalX(FindParagraph(harness.RenderView, "Trailing")!);
        double[] positions = [leading, title, info, trailing];
        Assert.Equal(increasing, positions.SequenceEqual(positions.Order()));
    }

    [Fact]
    public void ZeroArea_ListTileAndChevronLayOutWithoutCrashing()
    {
        using var tileHarness = new CupertinoThemeTestHarness(Wrap(new SizedBox(
            width: 0.0,
            height: 0.0,
            child: new CupertinoListTile(
                title: new Text("X"),
                trailing: new CupertinoListTileChevron()))));
        tileHarness.Pump(ViewSize);
        Assert.Equal(default, Assert.Single(FindAll<RenderColoredBox>(tileHarness.RenderView)).Size);

        using var chevronHarness = new CupertinoThemeTestHarness(Wrap(new SizedBox(
            width: 0.0,
            height: 0.0,
            child: new CupertinoListTileChevron())));
        chevronHarness.Pump(ViewSize);
        Assert.All(
            FindAll<RenderParagraph>(chevronHarness.RenderView),
            paragraph => Assert.Equal(default, paragraph.Size));
    }

    private static Widget Wrap(
        Widget child,
        PlatformBrightness brightness = PlatformBrightness.Light,
        TextDirection direction = TextDirection.Ltr)
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

    private static Color BackgroundColor(RenderObject root)
    {
        return Assert.Single(FindAll<RenderColoredBox>(root)).Color;
    }

    private static double GlobalX(RenderBox box) => box.LocalToGlobal(default).X;

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

    private static IReadOnlyList<T> FindAll<T>(RenderObject? root, Func<T, bool> predicate)
        where T : RenderObject
    {
        return FindAll<T>(root).Where(predicate).ToArray();
    }

    private static SemanticsNode? FindSemantics(SemanticsNode node, string label)
    {
        if (node.Label?.Split('\n').Contains(label) == true)
        {
            return node;
        }

        foreach (SemanticsNode child in node.Children)
        {
            SemanticsNode? found = FindSemantics(child, label);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static void Tap(RenderView renderView, Point position, int pointer)
    {
        DateTime timestamp = DateTime.UtcNow;
        GestureBinding.Instance.HandlePointerEvent(
            renderView,
            new PointerDownEvent(
                pointer,
                PointerDeviceKind.Mouse,
                position,
                PointerButtons.Primary,
                timestamp));
        GestureBinding.Instance.HandlePointerEvent(
            renderView,
            new PointerUpEvent(
                pointer,
                PointerDeviceKind.Mouse,
                position,
                PointerButtons.None,
                timestamp.AddMilliseconds(20.0)));
    }
}
