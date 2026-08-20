using Avalonia;
using Plumix;
using Plumix.Cupertino;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: cupertino_ui/test/expansion_tile_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoExpansionTileTests : IDisposable
{
    private static readonly Size ViewSize = new(320.0, 260.0);

    public CupertinoExpansionTileTests()
    {
        Scheduler.ResetForTests();
        GestureBinding.Instance.ResetForTests();
        PlatformDefaults.DebugTargetPlatformOverride = null;
    }

    public void Dispose()
    {
        PlatformDefaults.DebugTargetPlatformOverride = null;
        GestureBinding.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void Constructor_UsesFlutterDefaults()
    {
        var title = new Text("Title");
        var child = new Text("Child");
        var tile = new CupertinoExpansionTile(title, child);

        Assert.Same(title, tile.Title);
        Assert.Same(child, tile.Child);
        Assert.Null(tile.Controller);
        Assert.Equal(ExpansionTileTransitionMode.Fade, tile.TransitionMode);
    }

    [Fact]
    public void Controller_ExpandsCollapsesAndSwapsWithoutLeakingOldController()
    {
        using var first = new ExpansibleController();
        using var second = new ExpansibleController();
        using var harness = new CupertinoThemeTestHarness(WrapWithoutOverlay(
            new CupertinoExpansionTile(
                title: new Text("Title"),
                child: new Text("Content"),
                controller: first)));
        harness.Pump(ViewSize);
        Assert.True(Assert.Single(FindAll<RenderOffstage>(harness.RenderView)).Offstage);

        first.Expand();
        SettleAnimation(harness);
        Assert.False(Assert.Single(FindAll<RenderOffstage>(harness.RenderView)).Offstage);
        Assert.Equal(1.0, Assert.Single(FindAnimatedHeightFactors(harness.RenderView)), 3);

        harness.PumpWidget(WrapWithoutOverlay(new CupertinoExpansionTile(
            title: new Text("Title"),
            child: new Text("Content"),
            controller: second)));
        SettleAnimation(harness);
        Assert.True(Assert.Single(FindAll<RenderOffstage>(harness.RenderView)).Offstage);

        first.Collapse();
        first.Expand();
        harness.Pump(ViewSize);
        Assert.True(Assert.Single(FindAll<RenderOffstage>(harness.RenderView)).Offstage);

        second.Expand();
        SettleAnimation(harness);
        Assert.False(Assert.Single(FindAll<RenderOffstage>(harness.RenderView)).Offstage);
    }

    [Fact]
    public void HeaderTap_FadeModeClonesBodyAndRotatesChevronForThe250MsCurve()
    {
        using var controller = new ExpansibleController();
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoExpansionTile(
            title: new Text("Title"),
            child: new SizedBox(height: 50.0, child: new Text("Content")),
            controller: controller)));
        harness.Pump(ViewSize);

        Tap(harness.RenderView, new Point(160.0, 22.0), 811);
        harness.Pump(ViewSize);
        Assert.True(controller.IsExpanded);
        Assert.Equal(2, FindParagraphs(harness.RenderView, "Content").Count);

        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.125));
        harness.Pump(ViewSize);
        RenderTransform icon = Assert.Single(FindAll<RenderTransform>(harness.RenderView));
        Assert.InRange(icon.Transform[0], 0.70, 0.72);
        Assert.InRange(icon.Transform[1], 0.70, 0.72);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.30));
        harness.Pump(ViewSize);
        Assert.Single(FindParagraphs(harness.RenderView, "Content"));
        Assert.False(Assert.Single(FindAll<RenderOffstage>(harness.RenderView)).Offstage);
        Assert.InRange(icon.Transform[0], -0.01, 0.01);
        Assert.InRange(icon.Transform[1], 0.99, 1.01);
    }

    [Fact]
    public void ScrollMode_UsesOnlyTheHeightTransitionWithoutOverlayClone()
    {
        using var controller = new ExpansibleController();
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoExpansionTile(
            title: new Text("Title"),
            child: new SizedBox(height: 50.0, child: new Text("Content")),
            controller: controller,
            transitionMode: ExpansionTileTransitionMode.Scroll)));
        harness.Pump(ViewSize);

        Tap(harness.RenderView, new Point(160.0, 22.0), 812);
        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.125));
        harness.Pump(ViewSize);

        Assert.True(controller.IsExpanded);
        Assert.Single(FindParagraphs(harness.RenderView, "Content"));
        Assert.Single(FindAnimatedHeightFactors(harness.RenderView), factor => factor is > 0.0 and < 1.0);
    }

    [Fact]
    public void NestedTiles_KeepIndependentControllers()
    {
        using var outer = new ExpansibleController();
        using var inner = new ExpansibleController();
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoExpansionTile(
            title: new Text("Outer"),
            controller: outer,
            child: new CupertinoExpansionTile(
                title: new Text("Inner"),
                controller: inner,
                child: new Text("Content")))));
        harness.Pump(ViewSize);

        outer.Expand();
        SettleAnimation(harness);
        Assert.True(outer.IsExpanded);
        Assert.False(inner.IsExpanded);

        inner.Expand();
        SettleAnimation(harness);
        Assert.True(outer.IsExpanded);
        Assert.True(inner.IsExpanded);

        outer.Collapse();
        SettleAnimation(harness);
        Assert.False(outer.IsExpanded);
        Assert.True(inner.IsExpanded);
    }

    [Theory]
    [InlineData(TargetPlatform.IOS, true)]
    [InlineData(TargetPlatform.MacOS, true)]
    [InlineData(TargetPlatform.Android, false)]
    [InlineData(TargetPlatform.Windows, false)]
    public void Semantics_ExposeOppositeTapHintAndAppleStateHint(
        TargetPlatform platform,
        bool expectsStateHint)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        using var controller = new ExpansibleController();
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoExpansionTile(
            title: new Text("Semantic tile"),
            child: new Text("Body"),
            controller: controller)));

        SemanticsNode collapsedRoot = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(ViewSize));
        SemanticsNode collapsed = Assert.IsType<SemanticsNode>(FindSemantics(collapsedRoot, "Semantic tile"));
        Assert.Equal("Expand for more details", collapsed.OnTapHint);
        Assert.Equal(expectsStateHint ? "Collapsed\n double tap to expand" : null, collapsed.Hint);

        controller.Expand();
        SettleAnimation(harness);
        SemanticsNode expandedRoot = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(ViewSize));
        SemanticsNode expanded = Assert.IsType<SemanticsNode>(FindSemantics(expandedRoot, "Semantic tile"));
        Assert.Equal("Collapse", expanded.OnTapHint);
        Assert.Equal(expectsStateHint ? "Expanded\n double tap to collapse" : null, expanded.Hint);
        Assert.True(expanded.Actions.HasFlag(SemanticsActions.Tap));
    }

    [Fact]
    public void ZeroArea_ExpansionAndFadeOverlayLayOutWithoutCrashing()
    {
        using var controller = new ExpansibleController();
        using var harness = new CupertinoThemeTestHarness(WrapWithoutOverlay(new SizedBox(
            width: 0.0,
            height: 0.0,
            child: new CupertinoExpansionTile(
                title: new Text("X"),
                child: new Text("Y"),
                controller: controller)), constrainWidth: false));
        harness.Pump(ViewSize);

        controller.Expand();
        SettleAnimation(harness);

        RenderConstrainedBox zeroBox = Assert.Single(
            FindAll<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints.MaxWidth == 0.0
                   && box.AdditionalConstraints.MaxHeight == 0.0);
        Assert.Equal(default, zeroBox.Size);
    }

    private static Widget Wrap(Widget child, bool constrainWidth = true)
    {
        Widget content = constrainWidth ? new SizedBox(width: 320.0, child: child) : child;
        var entry = new OverlayEntry(_ => new Align(
            alignment: Alignment.TopCenter,
            child: content));
        return new MediaQuery(
            data: new MediaQueryData(PlatformBrightness: PlatformBrightness.Light),
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
                        new CupertinoThemeData(),
                        new Overlay(initialEntries: [entry])))));
    }

    private static Widget WrapWithoutOverlay(Widget child, bool constrainWidth = true)
    {
        Widget content = constrainWidth ? new SizedBox(width: 320.0, child: child) : child;
        return new MediaQuery(
            data: new MediaQueryData(PlatformBrightness: PlatformBrightness.Light),
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
                        new CupertinoThemeData(),
                        new Align(alignment: Alignment.TopCenter, child: content)))));
    }

    private static void SettleAnimation(CupertinoThemeTestHarness harness)
    {
        AnimationPump.Advance(0.30);
        harness.Pump(ViewSize);
    }

    private static IReadOnlyList<double> FindAnimatedHeightFactors(RenderObject? root)
    {
        return FindAll<RenderAlign>(root)
            .Where(align => align.HeightFactor.HasValue)
            .Select(align => align.HeightFactor!.Value)
            .ToArray();
    }

    private static IReadOnlyList<RenderParagraph> FindParagraphs(RenderObject? root, string text)
    {
        return FindAll<RenderParagraph>(root).Where(paragraph => paragraph.PlainText == text).ToArray();
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
