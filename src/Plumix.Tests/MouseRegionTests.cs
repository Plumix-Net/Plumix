using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/basic.dart (MouseRegion)
// flutter/packages/flutter/lib/src/rendering/proxy_box.dart (RenderMouseRegion)
// Behaviors mirrored from flutter/packages/flutter/test/widgets/mouse_region_test.dart.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MouseRegionTests : IDisposable
{
    private readonly List<string> _log = [];

    public MouseRegionTests()
    {
        Scheduler.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    public void Dispose()
    {
        GestureBinding.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void Constructor_ExposesFlutterDefaults()
    {
        var region = new MouseRegion(child: new SizedBox(width: 10, height: 10));

        Assert.Null(region.OnEnter);
        Assert.Null(region.OnExit);
        Assert.Null(region.OnHover);
        Assert.True(region.Opaque);
        Assert.Null(region.HitTestBehavior);
        Assert.Equal(MouseCursor.Defer, region.Cursor);
    }

    [Fact]
    public void RenderMouseRegion_Defaults_MatchFlutter()
    {
        var render = new RenderMouseRegion();

        Assert.Equal(MouseCursor.Defer, render.Cursor);
        Assert.True(render.Opaque);
        Assert.True(render.ValidForMouseTracker);
        Assert.Equal(HitTestBehavior.Opaque, render.HitTestBehavior);
        Assert.Equal(HitTestBehavior.Opaque, render.Behavior);

        // A null `hitTestBehavior` restores the opaque default rather than clearing it.
        render.HitTestBehavior = null;
        Assert.Equal(HitTestBehavior.Opaque, render.HitTestBehavior);
    }

    [Fact]
    public void RenderMouseRegion_SizesToTheBiggestConstraintWithoutAChild()
    {
        using var harness = new MouseTrackingHarness(new MouseRegion(), new Size(120, 90));

        RenderMouseRegion render = Assert.IsType<RenderMouseRegion>(
            FindDescendant<RenderMouseRegion>(harness.RenderView));
        Assert.Equal(new Size(120, 90), render.Size);
    }

    [Fact]
    public void OnHover_FiresOnTheEnteringEventAndOnEverySubsequentMoveInside()
    {
        using var harness = new MouseTrackingHarness(
            new Align(
                alignment: Alignment.TopLeft,
                child: new MouseRegion(
                    child: new SizedBox(width: 60, height: 40),
                    onEnter: _ => _log.Add("enter"),
                    onHover: e => _log.Add($"hover:{e.Position.X}"),
                    onExit: _ => _log.Add("exit"))));

        harness.SendPointer(MouseTrackingHarness.Hover(new Point(10, 10)));
        Assert.Equal(["enter", "hover:10"], _log);

        harness.SendPointer(MouseTrackingHarness.Hover(new Point(20, 10)));
        Assert.Equal(["enter", "hover:10", "hover:20"], _log);

        harness.SendPointer(MouseTrackingHarness.Hover(new Point(120, 10)));
        Assert.Equal(["enter", "hover:10", "hover:20", "exit"], _log);
    }

    [Fact]
    public void ExitEvent_CarriesTheNewOutsidePositionAndTheMatchingNegativeLocalPosition()
    {
        Point? exitPosition = null;
        Point? exitLocalPosition = null;
        using var harness = new MouseTrackingHarness(
            new Align(
                alignment: Alignment.TopLeft,
                child: new Padding(
                    insets: new Thickness(10, 10, 0, 0),
                    child: new MouseRegion(
                        child: new SizedBox(width: 60, height: 40),
                        onExit: e =>
                        {
                            exitPosition = e.Position;
                            exitLocalPosition = e.LocalPosition;
                        }))));

        harness.SendPointer(MouseTrackingHarness.Hover(new Point(20, 20)));
        harness.SendPointer(MouseTrackingHarness.Hover(new Point(5, 5)));

        Assert.Equal(new Point(5, 5), exitPosition);
        Assert.Equal(new Point(-5, -5), exitLocalPosition);
    }

    [Fact]
    public void EnterAndExit_FireWhileAMouseButtonIsPressed()
    {
        using var harness = new MouseTrackingHarness(
            new Align(
                alignment: Alignment.TopLeft,
                child: new MouseRegion(
                    child: new SizedBox(width: 60, height: 40),
                    onEnter: _ => _log.Add("enter"),
                    onExit: _ => _log.Add("exit"))));

        harness.SendPointer(new PointerDownEvent(
            1, PointerDeviceKind.Mouse, new Point(10, 10), PointerButtons.Primary, DateTime.UtcNow));
        Assert.Equal(["enter"], _log);

        harness.SendPointer(new PointerMoveEvent(
            1, PointerDeviceKind.Mouse, new Point(400, 400), PointerButtons.Primary, down: true, DateTime.UtcNow));
        Assert.Equal(["enter", "exit"], _log);
    }

    [Fact]
    public void HitTestBehavior_DeferToChildWithNoChild_NeverEnters()
    {
        using var harness = new MouseTrackingHarness(
            new MouseRegion(
                hitTestBehavior: HitTestBehavior.DeferToChild,
                onEnter: _ => _log.Add("enter")));

        harness.SendPointer(MouseTrackingHarness.Hover(new Point(10, 10)));
        Assert.Empty(_log);

        // Rebuilding with the opaque default makes the very same region hit-testable.
        harness.Update(new MouseRegion(onEnter: _ => _log.Add("enter")));
        harness.SendPointer(MouseTrackingHarness.Hover(new Point(12, 10)));
        Assert.Equal(["enter"], _log);
    }

    [Fact]
    public void HitTestBehavior_TranslucentRegionDoesNotBlockTheRegionBehindIt()
    {
        using var harness = new MouseTrackingHarness(BuildStack(
            behind: new MouseRegion(onEnter: _ => _log.Add("enter:behind")),
            front: new MouseRegion(
                hitTestBehavior: HitTestBehavior.Translucent,
                onEnter: _ => _log.Add("enter:front"))));

        harness.SendPointer(MouseTrackingHarness.Hover(new Point(10, 10)));

        Assert.Equal(["enter:behind", "enter:front"], _log);
    }

    [Fact]
    public void Opaque_DefaultsToTrueAndBlocksTheRegionBehindIt()
    {
        using var harness = new MouseTrackingHarness(BuildStack(
            behind: new MouseRegion(onEnter: _ => _log.Add("enter:behind")),
            front: new MouseRegion(onEnter: _ => _log.Add("enter:front"))));

        harness.SendPointer(MouseTrackingHarness.Hover(new Point(10, 10)));

        Assert.Equal(["enter:front"], _log);
    }

    [Fact]
    public void Opaque_False_LetsTheRegionBehindReceiveThePointerToo()
    {
        using var harness = new MouseTrackingHarness(BuildStack(
            behind: new MouseRegion(onEnter: _ => _log.Add("enter:behind")),
            front: new MouseRegion(opaque: false, onEnter: _ => _log.Add("enter:front"))));

        harness.SendPointer(MouseTrackingHarness.Hover(new Point(10, 10)));

        // Enters run back to front, so the region behind enters first.
        Assert.Equal(["enter:behind", "enter:front"], _log);
    }

    [Fact]
    public void ChangingOpaqueToFalse_RepaintsAndLetsTheRegionBehindEnterOnTheNextFrame()
    {
        Widget Tree(bool opaque) => BuildStack(
            behind: new MouseRegion(onEnter: _ => _log.Add("enter:behind")),
            front: new MouseRegion(opaque: opaque, onEnter: _ => _log.Add("enter:front")));

        using var harness = new MouseTrackingHarness(Tree(opaque: true));
        harness.SendPointer(MouseTrackingHarness.Hover(new Point(10, 10)));
        Assert.Equal(["enter:front"], _log);

        _log.Clear();
        harness.Update(Tree(opaque: false));
        Assert.Equal(["enter:behind"], _log);
    }

    [Fact]
    public void ChangingOnlyTheCallbacks_DoesNotReEnterTheRegion()
    {
        var first = new List<double>();
        var second = new List<double>();
        using var harness = new MouseTrackingHarness(
            new Align(
                alignment: Alignment.TopLeft,
                child: new MouseRegion(
                    child: new SizedBox(width: 60, height: 40),
                    onEnter: _ => _log.Add("enter"),
                    onHover: e => first.Add(e.Position.X))));

        harness.SendPointer(MouseTrackingHarness.Hover(new Point(10, 10)));
        Assert.Equal(["enter"], _log);

        harness.Update(
            new Align(
                alignment: Alignment.TopLeft,
                child: new MouseRegion(
                    child: new SizedBox(width: 60, height: 40),
                    onEnter: _ => _log.Add("enter"),
                    onHover: e => second.Add(e.Position.X))));
        harness.SendPointer(MouseTrackingHarness.Hover(new Point(30, 10)));

        Assert.Equal([10.0], first);
        Assert.Equal([30.0], second);
        Assert.Equal(["enter"], _log);
    }

    [DebugOnlyFact]
    public void ChangingTheCursor_UpdatesTheActiveCursorWithoutReEnteringTheRegion()
    {
        Widget Tree(MouseCursor cursor) => new MouseRegion(cursor: cursor, onEnter: _ => _log.Add("enter"));

        using var harness = new MouseTrackingHarness(Tree(SystemMouseCursors.Forbidden));
        harness.SendPointer(MouseTrackingHarness.Added(new Point(10, 10)));
        Assert.Equal(SystemMouseCursors.Forbidden, harness.MouseTracker.DebugDeviceActiveCursor(1));
        Assert.Equal(["enter"], _log);

        harness.Update(Tree(SystemMouseCursors.Text));
        Assert.Equal(SystemMouseCursors.Text, harness.MouseTracker.DebugDeviceActiveCursor(1));
        Assert.Equal(["enter"], _log);

        // Reverting to `defer` falls through to the fallback, since nothing is behind it.
        harness.Update(Tree(MouseCursor.Defer));
        Assert.Equal(SystemMouseCursors.Basic, harness.MouseTracker.DebugDeviceActiveCursor(1));
        Assert.Equal(["enter"], _log);
    }

    [Fact]
    public void RenderMouseRegion_NeverForcesCompositingAndMirrorsItsChild()
    {
        using var plain = new MouseTrackingHarness(new MouseRegion(child: new SizedBox(width: 20, height: 20)));
        RenderMouseRegion plainRegion = Assert.IsType<RenderMouseRegion>(
            FindDescendant<RenderMouseRegion>(plain.RenderView));
        Assert.False(plainRegion.NeedsCompositing);

        using var boundary = new MouseTrackingHarness(
            new MouseRegion(child: new RepaintBoundary(child: new SizedBox(width: 20, height: 20))));
        RenderMouseRegion boundaryRegion = Assert.IsType<RenderMouseRegion>(
            FindDescendant<RenderMouseRegion>(boundary.RenderView));
        Assert.True(boundaryRegion.NeedsCompositing);
    }

    [DebugOnlyFact]
    public void RenderMouseRegion_DebugFillProperties_MatchFlutter()
    {
        // Plumix's `RenderObject` base dumps more properties than Dart's, so the assertions cover
        // the ones `RenderMouseRegion` itself adds.
        List<string> bare = Describe(new RenderMouseRegion());
        Assert.Contains("behavior: opaque", bare);
        Assert.Contains("listeners: <none>", bare);
        Assert.DoesNotContain(bare, line => line.StartsWith("cursor:", StringComparison.Ordinal));
        Assert.DoesNotContain(bare, line => line.StartsWith("opaque:", StringComparison.Ordinal));
        Assert.DoesNotContain("invalid for MouseTracker", bare);

        List<string> full = Describe(new RenderMouseRegion(
            onEnter: _ => { },
            onHover: _ => { },
            onExit: _ => { },
            cursor: SystemMouseCursors.Click,
            opaque: false,
            validForMouseTracker: false));
        Assert.Contains("behavior: opaque", full);
        Assert.Contains("listeners: enter, hover, exit", full);
        Assert.Contains("cursor: SystemMouseCursor(click)", full);
        Assert.Contains("opaque: false", full);
        Assert.Contains("invalid for MouseTracker", full);
    }

    [DebugOnlyFact]
    public void MouseRegion_DebugFillProperties_ListsTheCallbacksAndTheNonDefaultFlags()
    {
        var builder = new DiagnosticPropertiesBuilder();
        new MouseRegion(onEnter: _ => { }, onHover: _ => { }, opaque: false, cursor: SystemMouseCursors.Text)
            .DebugFillProperties(builder);
        List<string> lines = builder.Properties.Select(property => property.ToString()).ToList();

        Assert.Contains("listeners: enter, hover", lines);
        Assert.Contains("cursor: SystemMouseCursor(text)", lines);
        Assert.Contains("opaque: false", lines);
    }

    private static List<string> Describe(RenderMouseRegion region)
    {
        var builder = new DiagnosticPropertiesBuilder();
        region.DebugFillProperties(builder);
        // Dart's text tree hides a property whose value equals its declared default, which is the
        // `fine` level here; `builder.Properties` keeps them, so the filter is applied explicitly.
        return builder.Properties
            .Where(property => !property.IsFiltered(DiagnosticLevel.Info))
            .Select(property => property.ToString())
            .ToList();
    }

    private static Widget BuildStack(Widget behind, Widget front) => new Stack(
        children:
        [
            new Positioned(left: 0, top: 0, width: 60, height: 40, child: behind),
            new Positioned(left: 0, top: 0, width: 60, height: 40, child: front),
        ]);

    private static T? FindDescendant<T>(RenderObject? root)
        where T : RenderObject
    {
        if (root is null)
        {
            return null;
        }

        if (root is T match)
        {
            return match;
        }

        T? result = null;
        root.VisitChildren(child => result ??= FindDescendant<T>(child));
        return result;
    }
}
