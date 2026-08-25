using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoScrollbarTests
{
    private static readonly Color LightThumb = Color.FromArgb(0x59, 0x00, 0x00, 0x00);
    private static readonly Color DarkThumb = Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF);

    // Frame timestamps have to advance monotonically from where the scheduler's clock stands, or a
    // ticker started mid-frame (which anchors to `Scheduler.CurrentFrameTimeStamp`) never advances.
    private double _clock = Scheduler.CurrentSeconds;

    public CupertinoScrollbarTests()
    {
        GestureBinding.Instance.ResetForTests();
    }

    [Fact]
    public void Defaults_MatchFlutter()
    {
        var scrollbar = new CupertinoScrollbar(child: new SizedBox());

        Assert.Equal(3, CupertinoScrollbar.DefaultThickness);
        Assert.Equal(8.0, CupertinoScrollbar.DefaultThicknessWhileDragging);
        Assert.Equal(1.5, CupertinoScrollbar.DefaultRadius);
        Assert.Equal(4.0, CupertinoScrollbar.DefaultRadiusWhileDragging);

        Assert.Equal(CupertinoScrollbar.DefaultThickness, scrollbar.Thickness);
        Assert.Equal(CupertinoScrollbar.DefaultThicknessWhileDragging, scrollbar.ThicknessWhileDragging);
        Assert.Equal(CupertinoScrollbar.DefaultRadius, scrollbar.Radius);
        Assert.Equal(CupertinoScrollbar.DefaultRadiusWhileDragging, scrollbar.RadiusWhileDragging);
        Assert.False(scrollbar.ThumbVisibility);
        Assert.Null(scrollbar.Controller);
        Assert.Null(scrollbar.Shape);
        Assert.Null(scrollbar.ThumbColor);
        Assert.Equal(36, scrollbar.MinThumbLength);
        Assert.Equal(8, scrollbar.MinOverscrollLength);
        Assert.Equal(TimeSpan.FromMilliseconds(250), scrollbar.FadeDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(1200), scrollbar.TimeToFade);
        Assert.Equal(TimeSpan.FromMilliseconds(100), scrollbar.PressDuration);
        Assert.Equal(3, scrollbar.MainAxisMargin);
        Assert.Equal(3, scrollbar.CrossAxisMargin);
        Assert.Null(scrollbar.ScrollbarOrientation);
        Assert.Null(scrollbar.Padding);
        Assert.Equal((ScrollNotificationPredicate)RawScrollbar.DefaultScrollNotificationPredicate,
            scrollbar.NotificationPredicate);
    }

    [Fact]
    public void IsARawScrollbarAndValidatesDraggingMetrics()
    {
        Assert.IsAssignableFrom<RawScrollbar>(new CupertinoScrollbar(child: new SizedBox()));

        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoScrollbar(
            child: new SizedBox(),
            thicknessWhileDragging: double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoScrollbar(
            child: new SizedBox(),
            radiusWhileDragging: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoScrollbar(
            child: new SizedBox(),
            thickness: double.PositiveInfinity));
    }

    [Fact]
    public void CustomNotificationPredicateAndOrientationSurviveToTheBaseWidget()
    {
        bool Predicate(ScrollNotification notification) => true;
        var scrollbar = new CupertinoScrollbar(
            child: new SizedBox(),
            notificationPredicate: Predicate,
            scrollbarOrientation: ScrollbarOrientation.Left,
            mainAxisMargin: 9);

        Assert.Equal(ScrollbarOrientation.Left, scrollbar.ScrollbarOrientation);
        Assert.Equal(9, scrollbar.MainAxisMargin);
        Assert.Equal((ScrollNotificationPredicate)Predicate, scrollbar.NotificationPredicate);
    }

    // Flutter: "Tapping the track area pages the Scroll View except on iOS" — the initial rrect.
    [Fact]
    public void DefaultVerticalGeometryMatchesFlutter()
    {
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(BuildScrollbar(controller, contentExtent: 1000));

        harness.Pump(new Size(800, 600));

        var overlay = RequireOverlay(harness.RenderView);
        ScrollbarGeometry geometry = overlay.Geometry!.Value;
        Assert.Equal(Axis.Vertical, geometry.Axis);
        Assert.Equal(3, overlay.Thickness);
        // Thumb: x = 800 - crossAxisMargin(3) - thickness(3); y = mainAxisMargin(3);
        // extent = (600 - 2 * 3) * 600 / 1000.
        Assert.Equal(794, geometry.ThumbRect.X, precision: 3);
        Assert.Equal(3, geometry.ThumbRect.Y, precision: 3);
        Assert.Equal(3, geometry.ThumbRect.Width, precision: 3);
        Assert.Equal(356.4, geometry.ThumbRect.Height, precision: 3);
        // The track rect spans the whole padded viewport, thickness + 2 * crossAxisMargin wide.
        Assert.Equal(791, geometry.TrackRect.X, precision: 3);
        Assert.Equal(0, geometry.TrackRect.Y, precision: 3);
        Assert.Equal(9, geometry.TrackRect.Width, precision: 3);
        Assert.Equal(600, geometry.TrackRect.Height, precision: 3);
    }

    // Flutter: "CupertinoScrollbar scrollOrientation works correctly".
    [Fact]
    public void LeftScrollbarOrientationMatchesFlutter()
    {
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(BuildScrollbar(
            controller,
            contentExtent: 4000,
            scrollbarOrientation: ScrollbarOrientation.Left));

        harness.Pump(new Size(800, 600));

        ScrollbarGeometry geometry = RequireOverlay(harness.RenderView).Geometry!.Value;
        Assert.Equal(new Rect(0, 0, 9, 600), geometry.TrackRect);
        Assert.Equal(3, geometry.ThumbRect.X, precision: 3);
        Assert.Equal(3, geometry.ThumbRect.Y, precision: 3);
        Assert.Equal(3, geometry.ThumbRect.Width, precision: 3);
        Assert.Equal(89.1, geometry.ThumbRect.Height, precision: 3);
    }

    // Flutter: "Scrollbar dark mode".
    [Theory]
    [InlineData(PlatformBrightness.Light)]
    [InlineData(PlatformBrightness.Dark)]
    public void ThumbColorResolvesTheDynamicScrollbarColor(PlatformBrightness brightness)
    {
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(BuildScrollbar(
            controller,
            contentExtent: 1000,
            brightness: brightness));

        harness.Pump(new Size(800, 600));

        Color expected = brightness == PlatformBrightness.Dark ? DarkThumb : LightThumb;
        Assert.Equal(expected, RequireOverlay(harness.RenderView).ThumbColor);
    }

    [Fact]
    public void ThumbColorFollowsAnEnclosingCupertinoThemeOverTheMediaQuery()
    {
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new MediaQuery(
            data: new MediaQueryData(PlatformBrightness: PlatformBrightness.Light),
            child: new CupertinoTheme(
                data: new CupertinoThemeData(brightness: PlatformBrightness.Dark),
                child: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new CupertinoScrollbar(
                        controller: controller,
                        thumbVisibility: true,
                        child: BuildScrollable(controller, 1000))))));

        harness.Pump(new Size(800, 600));

        Assert.Equal(DarkThumb, RequireOverlay(harness.RenderView).ThumbColor);
    }

    // Flutter: "On first render with thumbVisibility: false, the thumb is hidden".
    [Fact]
    public void ThumbIsHiddenUntilScrolledWhenThumbVisibilityIsFalse()
    {
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(BuildScrollbar(
            controller,
            contentExtent: 1000,
            thumbVisibility: false));

        harness.Pump(new Size(800, 600));

        Assert.Equal(0, RequireOverlay(harness.RenderView).Opacity);
    }

    // Flutter: "Scrollbar changes thickness and radius when dragged" — linear over 100 ms.
    [Fact]
    public void DraggingAnimatesThicknessAndRadiusLinearlyOverTheResizeDuration()
    {
        using var controller = new ScrollController();
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);
        using var harness = new WidgetRenderHarness(BuildScrollbar(controller, contentExtent: 1000));

        harness.Pump(new Size(800, 600));
        var overlay = RequireOverlay(harness.RenderView);
        Assert.Equal(3, overlay.Thickness);

        PressThumb(overlay, harness);
        Assert.Empty(platform.Log);

        // t = 0 of the resize animation: still the idle thickness and radius.
        Assert.Equal(3, ReadThickness(harness), precision: 3);
        Assert.Equal(1.5, ReadRadius(harness), precision: 3);

        AdvanceAndPump(harness, 0.05);
        Assert.Equal(5.5, ReadThickness(harness), precision: 3);
        Assert.Equal(2.75, ReadRadius(harness), precision: 3);
        Assert.Empty(platform.Log);

        AdvanceAndPump(harness, 0.06);
        Assert.Equal(8, ReadThickness(harness), precision: 3);
        Assert.Equal(4, ReadRadius(harness), precision: 3);

        // Flutter buzzes once the resize animation finishes, not when the press starts.
        MethodCall call = Assert.Single(platform.Log);
        Assert.Equal("HapticFeedback.vibrate", call.Method);
        Assert.Equal("HapticFeedbackType.mediumImpact", call.Arguments);

        // The dragged thumb sits `thicknessWhileDragging` wide against the right edge.
        ScrollbarGeometry geometry = RequireOverlay(harness.RenderView).Geometry!.Value;
        Assert.Equal(789, geometry.ThumbRect.X, precision: 3);
        Assert.Equal(8, geometry.ThumbRect.Width, precision: 3);
    }

    [Fact]
    public void ReleasingTheThumbShrinksItBackAndBuzzesWhenItMoved()
    {
        using var controller = new ScrollController();
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);
        using var harness = new WidgetRenderHarness(BuildScrollbar(controller, contentExtent: 1000));

        harness.Pump(new Size(800, 600));
        var overlay = RequireOverlay(harness.RenderView);
        Point start = PressThumb(overlay, harness);
        AdvanceAndPump(harness, 0.11);
        Assert.Single(platform.Log);

        var moved = new Point(start.X, start.Y + 40);
        DateTime moveTime = PressTime.AddSeconds(5);
        overlay.HandleEvent(
            new PointerMoveEvent(71, PointerDeviceKind.Touch, moved, PointerButtons.Primary, true, moveTime),
            new BoxHitTestEntry(overlay, moved));
        overlay.HandleEvent(
            new PointerUpEvent(71, PointerDeviceKind.Touch, moved, PointerButtons.None, moveTime),
            new BoxHitTestEntry(overlay, moved));

        // A slow release buzzes a second time; the thumb then shrinks back to the idle thickness.
        Assert.Equal(2, platform.Log.Count);
        Assert.True(controller.PrimaryPosition!.Pixels > 0);

        AdvanceAndPump(harness, 0.11);
        Assert.Equal(3, ReadThickness(harness), precision: 3);
        Assert.Equal(1.5, ReadRadius(harness), precision: 3);
    }

    // Flutter: "Tapping the track area does not page the Scroll View on iOS".
    [Fact]
    public void TappingTheTrackDoesNotPageOnIos()
    {
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(BuildScrollbar(
            controller,
            contentExtent: 1000,
            platform: TargetPlatform.IOS));

        harness.Pump(new Size(800, 600));
        var overlay = RequireOverlay(harness.RenderView);
        TapTrackBelowThumb(overlay);
        harness.Pump(new Size(800, 600));

        Assert.Equal(0, controller.PrimaryPosition!.Pixels);
    }

    // Flutter: "Tapping the track area pages the Scroll View except on iOS".
    [Fact]
    public void TappingTheTrackPagesOnOtherPlatforms()
    {
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(BuildScrollbar(
            controller,
            contentExtent: 1000,
            platform: TargetPlatform.Android));

        harness.Pump(new Size(800, 600));
        var overlay = RequireOverlay(harness.RenderView);
        TapTrackBelowThumb(overlay);
        AdvanceAndPump(harness, 0.2);

        Assert.Equal(400, controller.PrimaryPosition!.Pixels, precision: 3);
    }

    // Flutter: "CupertinoScrollbar does not crash at zero area".
    [Fact]
    public void ZeroAreaDoesNotPaintOrCrash()
    {
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(BuildScrollbar(controller, contentExtent: 1000));

        harness.Pump(new Size(0, 0));

        Assert.Null(RequireOverlay(harness.RenderView).Geometry);
    }

    private Point PressThumb(RenderRawScrollbarOverlay overlay, WidgetRenderHarness harness)
    {
        Point point = overlay.Geometry!.Value.ThumbRect.Center;
        overlay.HandleEvent(
            new PointerDownEvent(71, PointerDeviceKind.Touch, point, PointerButtons.Primary, PressTime),
            new BoxHitTestEntry(overlay, point));
        // The press has to be held for `pressDuration` before the drag — and the resize — begins.
        AdvanceAndPump(harness, 0.11);
        return point;
    }

    private static void TapTrackBelowThumb(RenderRawScrollbarOverlay overlay)
    {
        Rect thumb = overlay.Geometry!.Value.ThumbRect;
        var point = new Point(thumb.Center.X, thumb.Bottom + 100);
        DateTime now = DateTime.UtcNow;
        overlay.HandleEvent(
            new PointerDownEvent(72, PointerDeviceKind.Touch, point, PointerButtons.Primary, now),
            new BoxHitTestEntry(overlay, point));
        overlay.HandleEvent(
            new PointerUpEvent(72, PointerDeviceKind.Touch, point, PointerButtons.None, now),
            new BoxHitTestEntry(overlay, point));
    }

    private static readonly DateTime PressTime = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private void AdvanceAndPump(WidgetRenderHarness harness, double seconds)
    {
        // Prime any freshly started ticker, then advance the frame clock.
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(_clock));
        _clock += seconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(_clock));
        harness.Pump(new Size(800, 600));
    }

    private static double ReadThickness(WidgetRenderHarness harness) => RequireOverlay(harness.RenderView).Thickness;

    private static double ReadRadius(WidgetRenderHarness harness) =>
        Assert.IsType<RawScrollbarOverlay>(FindWidgetObject<RawScrollbarOverlay>(harness.RootElement)).Radius;

    private static Widget BuildScrollbar(
        ScrollController controller,
        double contentExtent,
        bool thumbVisibility = true,
        PlatformBrightness brightness = PlatformBrightness.Light,
        ScrollbarOrientation? scrollbarOrientation = null,
        TargetPlatform platform = TargetPlatform.IOS)
    {
        return new MediaQuery(
            data: new MediaQueryData(PlatformBrightness: brightness),
            child: new Directionality(
                textDirection: TextDirection.Ltr,
                child: new ScrollConfiguration(
                    behavior: new TestScrollBehavior(platform),
                    child: new CupertinoScrollbar(
                        controller: controller,
                        thumbVisibility: thumbVisibility,
                        scrollbarOrientation: scrollbarOrientation,
                        child: BuildScrollable(controller, contentExtent)))));
    }

    private static Widget BuildScrollable(ScrollController controller, double contentExtent) =>
        new SingleChildScrollView(
            controller: controller,
            child: new SizedBox(width: 800, height: contentExtent));

    private static RenderRawScrollbarOverlay RequireOverlay(RenderObject root) =>
        Assert.IsType<RenderRawScrollbarOverlay>(FindDescendant<RenderRawScrollbarOverlay>(root));

    private static T? FindDescendant<T>(RenderObject? root) where T : RenderObject
    {
        if (root is null) return null;
        if (root is T match) return match;
        T? result = null;
        root.VisitChildren(child => result ??= FindDescendant<T>(child));
        return result;
    }

    private static Widget? FindWidgetObject<T>(Element? element) where T : Widget
    {
        if (element is null) return null;
        if (element.Widget is T match) return match;
        Widget? result = null;
        element.VisitChildren(child => result ??= FindWidgetObject<T>(child));
        return result;
    }

    private sealed class TestScrollBehavior : ScrollBehavior
    {
        private readonly TargetPlatform _platform;

        public TestScrollBehavior(TargetPlatform platform) => _platform = platform;

        public override TargetPlatform GetPlatform(BuildContext context) => _platform;
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _root;
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget widget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _root = new HarnessRootElement(RenderView, widget);
            _root.Attach(_owner);
            _root.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }
        public Element RootElement => _root;

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public void Dispose() => _root.Unmount();
    }

    private sealed class HarnessRootElement : Element, IRenderObjectHost
    {
        private readonly RenderView _renderView;
        private Element? _child;

        public HarnessRootElement(RenderView renderView, Widget widget) : base(widget) => _renderView = renderView;
        public override RenderObject? RenderObject => _child?.RenderObject;
        internal override Element? RenderObjectAttachingChild => _child;
        protected override void OnMount() { base.OnMount(); Rebuild(); }
        internal override void Rebuild() { Dirty = false; _child = UpdateChild(_child, Widget, Slot); }
        internal override void Update(Widget newWidget) { base.Update(newWidget); Rebuild(); }
        internal override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
        internal override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }

        internal override void Unmount()
        {
            if (_child is not null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot) => _renderView.Child = (RenderBox)child;
        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null;
        }
    }
}
