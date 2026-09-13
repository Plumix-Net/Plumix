using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using MaterialScrollbar = Plumix.Material.Scrollbar;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialScrollbarTests
{
    private static readonly Size ViewportSize = new(200, 240);
    private static readonly DateTime PressTime = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private double _clock = Scheduler.CurrentSeconds;

    public MaterialScrollbarTests()
    {
        GestureBinding.Instance.ResetForTests();
    }

    [Fact]
    public void RawScrollbar_Defaults_MatchFlutter()
    {
        var scrollbar = new RawScrollbar(child: new SizedBox());

        Assert.Null(scrollbar.Controller);
        Assert.Null(scrollbar.ThumbVisibility);
        Assert.Null(scrollbar.Shape);
        Assert.Null(scrollbar.Radius);
        Assert.Null(scrollbar.Thickness);
        Assert.Null(scrollbar.ThumbColor);
        Assert.Equal(18, scrollbar.MinThumbLength);
        Assert.Null(scrollbar.MinOverscrollLength);
        Assert.Null(scrollbar.TrackVisibility);
        Assert.Equal(TimeSpan.FromMilliseconds(300), scrollbar.FadeDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(600), scrollbar.TimeToFade);
        Assert.Equal(TimeSpan.Zero, scrollbar.PressDuration);
        Assert.Null(scrollbar.Interactive);
        Assert.Null(scrollbar.ScrollbarOrientation);
        Assert.Equal(0, scrollbar.MainAxisMargin);
        Assert.Equal(0, scrollbar.CrossAxisMargin);
        Assert.Null(scrollbar.Padding);
    }

    [DebugOnlyFact]
    public void RawScrollbar_ValidatesFlutterContracts()
    {
        Assert.Throws<ArgumentException>(() => new RawScrollbar(
            child: new SizedBox(),
            thumbVisibility: false,
            trackVisibility: true));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RawScrollbar(
            child: new SizedBox(),
            minThumbLength: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RawScrollbar(
            child: new SizedBox(),
            minThumbLength: 10,
            minOverscrollLength: 11));
        Assert.Throws<ArgumentException>(() => new RawScrollbar(
            child: new SizedBox(),
            shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(4)),
            radius: 4));
        Assert.Equal(0, new RawScrollbar(child: new SizedBox(), thickness: 0).Thickness);
    }

    [Fact]
    public void RawScrollbar_OverlaysChildAndComputesVerticalGeometry()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new RawScrollbar(
            controller: controller,
            thumbVisibility: true,
            thickness: 6,
            radius: 3,
            minThumbLength: 18,
            child: BuildVerticalList(controller, 20)));
        Settle(harness);

        ScrollbarPainter painter = RequirePainter(harness);
        var geometry = Assert.IsType<ScrollbarGeometry>(painter.Geometry);
        Assert.Equal(Axis.Vertical, geometry.Axis);
        Assert.Equal(new Rect(194, 0, 6, 240), geometry.TrackRect);
        Assert.Equal(72, geometry.ThumbRect.Height, precision: 3);
        Assert.Equal(0, geometry.ThumbRect.Y, precision: 3);
        Assert.Equal(6, painter.Thickness);
        Assert.Equal(1, painter.FadeoutOpacityAnimation.Value);
    }

    [Fact]
    public void RawScrollbar_HorizontalBottomGeometryHonorsMarginsAndPadding()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new RawScrollbar(
            controller: controller,
            thumbVisibility: true,
            trackVisibility: true,
            scrollbarOrientation: ScrollbarOrientation.Bottom,
            thickness: 8,
            mainAxisMargin: 5,
            crossAxisMargin: 3,
            padding: new Thickness(7, 11, 13, 17),
            child: new SingleChildScrollView(
                controller: controller,
                scrollDirection: Axis.Horizontal,
                child: new SizedBox(width: 900, height: 120))));
        Settle(harness, new Size(300, 120));

        ScrollbarPainter painter = RequirePainter(harness);
        var geometry = Assert.IsType<ScrollbarGeometry>(painter.Geometry);
        Assert.Equal(Axis.Horizontal, geometry.Axis);
        // Flutter's track rect spans the padded viewport; only the thumb is inset by mainAxisMargin.
        Assert.Equal(7, geometry.TrackRect.X);
        Assert.Equal(280, geometry.TrackRect.Width);
        Assert.Equal(89, geometry.TrackRect.Y);
        Assert.Equal(14, geometry.TrackRect.Height);
        Assert.Equal(12, geometry.TrackMainAxisStart);
        Assert.Equal(270, geometry.TrackMainAxisExtent);
        Assert.True(geometry.ThumbRect.Width >= 18);
    }

    [Fact]
    public void RawScrollbar_DefaultVerticalOrientationFollowsTextDirection()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new Directionality(
            textDirection: TextDirection.Rtl,
            child: new RawScrollbar(
                controller: controller,
                thumbVisibility: true,
                thickness: 6,
                child: BuildVerticalList(controller, 20))));
        Settle(harness);

        ScrollbarGeometry geometry = RequirePainter(harness).Geometry!.Value;
        Assert.Equal(0, geometry.TrackRect.X);
    }

    [Fact]
    public void RawScrollbar_ControllerMovementShowsTransientThumb()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new RawScrollbar(
            controller: controller,
            child: BuildVerticalList(controller, 20)));
        Settle(harness);
        Assert.Equal(0, RequirePainter(harness).FadeoutOpacityAnimation.Value);

        controller.JumpTo(80);
        AdvanceAndPump(harness, 0.4);

        Assert.Equal(1, RequirePainter(harness).FadeoutOpacityAnimation.Value);
        Assert.True(RequirePainter(harness).Geometry!.Value.ThumbRect.Y > 0);
    }

    // Flutter: "Scrollbar will fade back in when hovering over known track area".
    [Fact]
    public void RawScrollbar_MouseHoverNearThumbRevealsFadedScrollbarButContentHoverDoesNot()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new RawScrollbar(
            controller: controller,
            child: BuildVerticalList(controller, 20)));
        Settle(harness);
        Assert.Equal(0, RequirePainter(harness).FadeoutOpacityAnimation.Value);

        ScrollbarGeometry geometry = RequirePainter(harness).Geometry!.Value;
        Dispatch(harness, new PointerHoverEvent(
            8, PointerDeviceKind.Mouse, new Point(100, geometry.ThumbRect.Center.Y),
            PointerButtons.None, PressTime));
        AdvanceAndPump(harness, 0.4);
        Assert.Equal(0, RequirePainter(harness).FadeoutOpacityAnimation.Value);

        // Inside the 48-logical-pixel proximity rect Flutter grows around the thumb for hovering.
        var proximityPoint = new Point(geometry.ThumbRect.Center.X - 23, geometry.ThumbRect.Center.Y);
        Dispatch(harness, new PointerHoverEvent(
            8, PointerDeviceKind.Mouse, proximityPoint, PointerButtons.None, PressTime.AddMilliseconds(20)));
        AdvanceAndPump(harness, 0.4);

        Assert.Equal(1, RequirePainter(harness).FadeoutOpacityAnimation.Value);
    }

    [Fact]
    public void RawScrollbar_ContentClickDoesNotTriggerTrackPaging()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        int tappedIndex = -1;
        using var harness = new WidgetRenderHarness(new RawScrollbar(
            controller: controller,
            thumbVisibility: true,
            interactive: true,
            child: ListView.Builder(
                itemCount: 30,
                controller: controller,
                itemExtent: 40,
                itemBuilder: (_, index) => new GestureDetector(
                    behavior: HitTestBehavior.Opaque,
                    onTap: () => tappedIndex = index,
                    child: new SizedBox(height: 40, child: new Text($"row {index}"))),
                addAutomaticKeepAlives: false)));
        Settle(harness);

        var point = new Point(100, 100);
        Dispatch(harness, new PointerDownEvent(
            18, PointerDeviceKind.Mouse, point, PointerButtons.Primary, PressTime));
        Dispatch(harness, new PointerUpEvent(
            18, PointerDeviceKind.Mouse, point, PointerButtons.None, PressTime.AddMilliseconds(20)));

        Assert.Equal(2, tappedIndex);
        Assert.Equal(0, controller.Offset);
    }

    // Flutter: "hit test" — taps over the track and the thumb never reach the child.
    [Fact]
    public void RawScrollbar_TrackAndThumbAbsorbHitTestsFromTheChild()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        int tappedIndex = -1;
        using var harness = new WidgetRenderHarness(new RawScrollbar(
            controller: controller,
            thumbVisibility: true,
            trackVisibility: true,
            interactive: true,
            child: ListView.Builder(
                itemCount: 30,
                controller: controller,
                itemExtent: 40,
                itemBuilder: (_, index) => new GestureDetector(
                    behavior: HitTestBehavior.Opaque,
                    onTap: () => tappedIndex = index,
                    child: new SizedBox(height: 40, child: new Text($"row {index}"))),
                addAutomaticKeepAlives: false)));
        Settle(harness);

        ScrollbarGeometry geometry = RequirePainter(harness).Geometry!.Value;
        var onThumb = geometry.ThumbRect.Center;
        Dispatch(harness, new PointerDownEvent(
            50, PointerDeviceKind.Mouse, onThumb, PointerButtons.Primary, PressTime));
        Dispatch(harness, new PointerUpEvent(
            50, PointerDeviceKind.Mouse, onThumb, PointerButtons.None, PressTime.AddMilliseconds(20)));

        Assert.Equal(-1, tappedIndex);
    }

    // Flutter: "Scrollbar thumb can be dragged" — and, since 3.47, the drag engages on pointer down
    // (`dragStartBehavior: down` with `touchSlop: 0`), so the first move already scrolls.
    [Fact]
    public void RawScrollbar_ThumbDragMapsTrackTravelToScrollExtent()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new RawScrollbar(
            controller: controller,
            thumbVisibility: true,
            interactive: true,
            child: BuildVerticalList(controller, 30)));
        Settle(harness);

        ScrollbarGeometry geometry = RequirePainter(harness).Geometry!.Value;
        double x = geometry.ThumbRect.Center.X;
        double downY = geometry.ThumbRect.Center.Y;
        Dispatch(harness, new PointerDownEvent(
            9, PointerDeviceKind.Mouse, new Point(x, downY), PointerButtons.Primary, PressTime));
        Dispatch(harness, new PointerMoveEvent(
            9, PointerDeviceKind.Mouse, new Point(x, 235), PointerButtons.Primary, true,
            PressTime.AddMilliseconds(20)));
        Dispatch(harness, new PointerUpEvent(
            9, PointerDeviceKind.Mouse, new Point(x, 235), PointerButtons.None,
            PressTime.AddMilliseconds(30)));

        Assert.True(controller.Offset > controller.PrimaryPosition!.MaxScrollExtent * 0.9);
    }

    // Flutter: "Scrollbar thumb can be dragged" — a single one-pixel move is enough, because the
    // thumb recognizer runs with `DeviceGestureSettings(touchSlop: 0)`.
    [Fact]
    public void RawScrollbar_ThumbDragStartsWithoutCrossingTheTouchSlop()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new RawScrollbar(
            controller: controller,
            thumbVisibility: true,
            interactive: true,
            child: BuildVerticalList(controller, 30)));
        Settle(harness);

        ScrollbarGeometry geometry = RequirePainter(harness).Geometry!.Value;
        Point start = geometry.ThumbRect.Center;
        Dispatch(harness, new PointerDownEvent(
            11, PointerDeviceKind.Touch, start, PointerButtons.Primary, PressTime));
        Dispatch(harness, new PointerMoveEvent(
            11, PointerDeviceKind.Touch, new Point(start.X, start.Y + 2), PointerButtons.Primary, true,
            PressTime.AddMilliseconds(16)));

        Assert.True(controller.Offset > 0);
    }

    // Flutter: "Scrollbar thumb cannot be dragged into overscroll if the physics do not allow".
    [Fact]
    public void RawScrollbar_ThumbDragDoesNotEnterOverscrollAtTheTop()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new RawScrollbar(
            controller: controller,
            thumbVisibility: true,
            interactive: true,
            child: BuildVerticalList(controller, 30)));
        Settle(harness);

        Point start = RequirePainter(harness).Geometry!.Value.ThumbRect.Center;
        Dispatch(harness, new PointerDownEvent(
            12, PointerDeviceKind.Touch, start, PointerButtons.Primary, PressTime));
        Dispatch(harness, new PointerMoveEvent(
            12, PointerDeviceKind.Touch, new Point(start.X, start.Y - 40), PointerButtons.Primary, true,
            PressTime.AddMilliseconds(16)));

        Assert.Equal(0, controller.Offset);
    }

    // Flutter: "Scrollbar respect the NeverScrollableScrollPhysics physics".
    [Fact]
    public void RawScrollbar_RespectsNeverScrollableScrollPhysics()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new RawScrollbar(
            controller: controller,
            thumbVisibility: true,
            interactive: true,
            child: ListView.Builder(
                itemCount: 30,
                controller: controller,
                itemExtent: 40,
                physics: new NeverScrollableScrollPhysics(),
                itemBuilder: (_, index) => new SizedBox(height: 40, child: new Text($"row {index}")),
                addAutomaticKeepAlives: false)));
        Settle(harness);

        ScrollbarGeometry geometry = RequirePainter(harness).Geometry!.Value;
        Point start = geometry.ThumbRect.Center;
        Dispatch(harness, new PointerDownEvent(
            13, PointerDeviceKind.Touch, start, PointerButtons.Primary, PressTime));
        Dispatch(harness, new PointerMoveEvent(
            13, PointerDeviceKind.Touch, new Point(start.X, start.Y + 40), PointerButtons.Primary, true,
            PressTime.AddMilliseconds(16)));
        Assert.Equal(0, controller.Offset);

        var trackPoint = new Point(geometry.TrackRect.Center.X, geometry.TrackRect.Bottom - 2);
        Dispatch(harness, new PointerDownEvent(
            14, PointerDeviceKind.Mouse, trackPoint, PointerButtons.Primary, PressTime));
        AdvanceAndPump(harness, 0.2);
        Assert.Equal(0, controller.Offset);
    }

    [Fact]
    public void RawScrollbar_TrackPressPagesTowardPointer()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new RawScrollbar(
            controller: controller,
            thumbVisibility: true,
            trackVisibility: true,
            interactive: true,
            child: BuildVerticalList(controller, 30)));
        Settle(harness);

        ScrollbarGeometry geometry = RequirePainter(harness).Geometry!.Value;
        var point = new Point(geometry.TrackRect.Center.X, geometry.TrackRect.Bottom - 2);
        Dispatch(harness, new PointerDownEvent(
            10, PointerDeviceKind.Mouse, point, PointerButtons.Primary, PressTime));
        AdvanceAndPump(harness, 0.2);

        // `ScrollAction.getDirectionalIncrement` pages by 0.8 * viewportDimension.
        Assert.Equal(192, controller.Offset, precision: 3);
    }

    // Flutter: `pressDuration` is declared for source compatibility and never read.
    [Fact]
    public void RawScrollbar_PressDurationDoesNotDelayTheThumbDrag()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new RawScrollbar(
            controller: controller,
            thumbVisibility: true,
            interactive: true,
            pressDuration: TimeSpan.FromMilliseconds(100),
            child: BuildVerticalList(controller, 30)));
        Settle(harness);

        Point start = RequirePainter(harness).Geometry!.Value.ThumbRect.Center;
        Dispatch(harness, new PointerDownEvent(
            21, PointerDeviceKind.Touch, start, PointerButtons.Primary, PressTime));
        Dispatch(harness, new PointerMoveEvent(
            21, PointerDeviceKind.Touch, new Point(start.X, 235), PointerButtons.Primary, true,
            PressTime.AddMilliseconds(16)));

        Assert.True(controller.Offset > controller.PrimaryPosition!.MaxScrollExtent * 0.9);
    }

    // Flutter: "Scrollbar gestures disabled when maxScrollExtent == minScrollExtent".
    [Fact]
    public void RawScrollbar_InstallsNoRecognizersWhenTheContentFits()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new RawScrollbar(
            controller: controller,
            interactive: true,
            child: BuildVerticalList(controller, 2)));
        Settle(harness);

        var detector = Assert.IsType<RawGestureDetector>(FindWidgetObject<RawGestureDetector>(harness.RootElement));
        Assert.Empty(detector.Gestures!);
    }

    [Fact]
    public void RawScrollbar_InstallsThumbAndTrackRecognizersWhenScrollable()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new RawScrollbar(
            controller: controller,
            interactive: true,
            child: BuildVerticalList(controller, 30)));
        Settle(harness);

        var detector = Assert.IsType<RawGestureDetector>(FindWidgetObject<RawGestureDetector>(harness.RootElement));
        Assert.Equal(2, detector.Gestures!.Count);
    }

    // Flutter: "Scrollbar hit test area adjusts for PointerDeviceKind".
    [Fact]
    public void RawScrollbar_TouchHitTestAreaIsWiderThanTheMouseOne()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new RawScrollbar(
            controller: controller,
            thumbVisibility: true,
            interactive: true,
            thickness: 6,
            child: BuildVerticalList(controller, 30)));
        Settle(harness);

        ScrollbarPainter painter = RequirePainter(harness);
        ScrollbarGeometry geometry = painter.Geometry!.Value;
        var justOutside = new Point(geometry.ThumbRect.Left - 8, geometry.ThumbRect.Center.Y);

        Assert.True(painter.HitTestOnlyThumbInteractive(justOutside, PointerDeviceKind.Touch));
        Assert.False(painter.HitTestOnlyThumbInteractive(justOutside, PointerDeviceKind.Mouse));

        // The move stays under the scroll view's own 18 px touch slop, so only the scrollbar's
        // zero-slop recognizer claims the pointer — exactly the situation Flutter's test exercises.
        Dispatch(harness, new PointerDownEvent(
            15, PointerDeviceKind.Touch, justOutside, PointerButtons.Primary, PressTime));
        Dispatch(harness, new PointerMoveEvent(
            15, PointerDeviceKind.Touch, new Point(justOutside.X, justOutside.Y + 8),
            PointerButtons.Primary, true, PressTime.AddMilliseconds(16)));
        Assert.True(controller.Offset > 0);
    }

    [Fact]
    public void MaterialScrollbar_DesktopThemeStatesResolveOntoThePainter()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        var themeData = new ScrollbarThemeData(
            thumbVisibility: WidgetStateProperty<bool?>.All(true),
            trackVisibility: WidgetStateProperty<bool?>.ResolveWith(states =>
                states.Contains(WidgetState.Hovered)),
            thickness: WidgetStateProperty<double?>.ResolveWith(states =>
                states.Contains(WidgetState.Hovered) ? 14 : 9),
            thumbColor: WidgetStateProperty<Color?>.ResolveWith(states =>
                states.Contains(WidgetState.Dragged) ? Colors.Crimson : Colors.DarkCyan),
            trackColor: WidgetStateProperty<Color?>.All(Colors.Beige),
            trackBorderColor: WidgetStateProperty<Color?>.All(Colors.Brown),
            radius: 6,
            crossAxisMargin: 4,
            mainAxisMargin: 3,
            minThumbLength: 52,
            interactive: true);

        using var harness = new WidgetRenderHarness(new Theme(
            data: ThemeData.Light with
            {
                Platform = TargetPlatform.Windows,
                ScrollbarTheme = themeData,
            },
            child: new MaterialScrollbar(
                controller: controller,
                child: BuildVerticalList(controller, 30))));
        Settle(harness);

        ScrollbarPainter painter = RequirePainter(harness);
        Assert.Equal(Radius.Circular(6), painter.Radius);
        Assert.Equal(4, painter.CrossAxisMargin);
        Assert.Equal(3, painter.MainAxisMargin);
        Assert.Equal(52, painter.MinLength);
        Assert.Equal(9, painter.Thickness);
        Assert.Equal(Colors.DarkCyan, painter.Color);
        Assert.Equal(1, painter.FadeoutOpacityAnimation.Value);
        Assert.False(painter.IgnorePointer);
    }

    [Fact]
    public void MaterialScrollbar_HoverAndDragUpdateResolvedPainterState()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        var scrollbarTheme = new ScrollbarThemeData(
            thumbVisibility: WidgetStateProperty<bool?>.All(true),
            trackVisibility: WidgetStateProperty<bool?>.ResolveWith(states =>
                states.Contains(WidgetState.Hovered)),
            thickness: WidgetStateProperty<double?>.ResolveWith(states =>
                states.Contains(WidgetState.Hovered) ? 14 : 9),
            thumbColor: WidgetStateProperty<Color?>.ResolveWith(states =>
                states.Contains(WidgetState.Dragged) ? Colors.Crimson : Colors.DarkCyan),
            trackColor: WidgetStateProperty<Color?>.All(Colors.Beige));
        using var harness = new WidgetRenderHarness(new Theme(
            data: ThemeData.Light with
            {
                Platform = TargetPlatform.Windows,
                ScrollbarTheme = scrollbarTheme,
            },
            child: new MaterialScrollbar(
                controller: controller,
                child: BuildVerticalList(controller, 30))));
        Settle(harness);

        ScrollbarPainter painter = RequirePainter(harness);
        Assert.Equal(9, painter.Thickness);
        Assert.Equal(Colors.DarkCyan, painter.Color);
        Assert.Equal(Color.FromArgb(0, 0, 0, 0), painter.TrackColor);

        Point point = painter.Geometry!.Value.ThumbRect.Center;
        Dispatch(harness, new PointerHoverEvent(
            33, PointerDeviceKind.Mouse, point, PointerButtons.None, PressTime));
        AdvanceAndPump(harness, 0.3);

        painter = RequirePainter(harness);
        Assert.Equal(14, painter.Thickness);
        Assert.Equal(Colors.Beige, painter.TrackColor);

        point = painter.Geometry!.Value.ThumbRect.Center;
        Dispatch(harness, new PointerDownEvent(
            34, PointerDeviceKind.Mouse, point, PointerButtons.Primary, PressTime.AddMilliseconds(50)));
        AdvanceAndPump(harness, 0.05);

        Assert.Equal(Colors.Crimson, RequirePainter(harness).Color);
    }

    [Fact]
    public void MaterialScrollbar_PlatformDefaultsMatchAndroidAndDesktopPaths()
    {
        using var timers = new FakeGestureTimers();
        using var androidController = new ScrollController();
        using var androidHarness = new WidgetRenderHarness(new Theme(
            data: ThemeData.Light with { Platform = TargetPlatform.Android },
            child: new MaterialScrollbar(
                controller: androidController,
                thumbVisibility: true,
                child: BuildVerticalList(androidController, 30))));
        Settle(androidHarness);

        ScrollbarPainter android = RequirePainter(androidHarness);
        Assert.Null(android.Radius);
        Assert.Equal(0, android.CrossAxisMargin);
        Assert.Equal(4, android.Thickness);
        // `interactive` defaults to false on Android, which is what `ignorePointer` reports.
        Assert.True(android.IgnorePointer);

        using var desktopController = new ScrollController();
        using var desktopHarness = new WidgetRenderHarness(new Theme(
            data: ThemeData.Light with { Platform = TargetPlatform.Windows },
            child: new MaterialScrollbar(
                controller: desktopController,
                thumbVisibility: true,
                trackVisibility: true,
                child: BuildVerticalList(desktopController, 30))));
        Settle(desktopHarness);

        ScrollbarPainter desktop = RequirePainter(desktopHarness);
        Assert.Equal(Radius.Circular(8), desktop.Radius);
        Assert.Equal(2, desktop.CrossAxisMargin);
        Assert.Equal(8, desktop.Thickness);
        Assert.False(desktop.IgnorePointer);
        // With a visible track Dart skips the hover tween and uses the hover colour immediately.
        Assert.Equal(WithOpacity(ThemeData.Light.ColorScheme.OnSurface, 0.50), desktop.Color);
    }

    [Fact]
    public void MaterialScrollbar_IosDelegatesToCupertinoScrollbar()
    {
        using var tree = new WidgetTree(new Theme(
            data: ThemeData.Light with { Platform = TargetPlatform.IOS },
            child: new MaterialScrollbar(
                child: new SizedBox(),
                thumbVisibility: true)));

        var cupertino = Assert.IsType<CupertinoScrollbar>(tree.FindWidget<CupertinoScrollbar>());
        var raw = Assert.IsAssignableFrom<RawScrollbar>(tree.FindWidget<RawScrollbar>());
        Assert.Equal(CupertinoScrollbar.DefaultThickness, cupertino.Thickness);
        Assert.Equal(CupertinoScrollbar.DefaultThicknessWhileDragging, cupertino.ThicknessWhileDragging);
        Assert.Equal(Radius.Circular(CupertinoScrollbar.DefaultRadius), cupertino.Radius);
        Assert.Equal(TimeSpan.FromMilliseconds(250), raw.FadeDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(1200), raw.TimeToFade);
        Assert.Equal(TimeSpan.FromMilliseconds(100), raw.PressDuration);
        Assert.Same(cupertino, raw);
    }

    [Fact]
    public void ScrollbarThemeData_CopyWithLerpAndInheritedWrapMatchFlutter()
    {
        using var timers = new FakeGestureTimers();
        var idleStates = new HashSet<WidgetState>();
        var hoveredStates = new HashSet<WidgetState> { WidgetState.Hovered };
        var start = new ScrollbarThemeData(
            thumbVisibility: WidgetStateProperty<bool?>.All(false),
            thickness: WidgetStateProperty<double?>.All(4),
            trackVisibility: WidgetStateProperty<bool?>.All(false),
            thumbColor: WidgetStateProperty<Color?>.All(Colors.Black),
            radius: 2,
            interactive: false);
        ScrollbarThemeData copied = start.CopyWith(mainAxisMargin: 6);

        Assert.Same(start.ThumbVisibility, copied.ThumbVisibility);
        Assert.Same(start.Thickness, copied.Thickness);
        Assert.Equal(6, copied.MainAxisMargin);

        var end = new ScrollbarThemeData(
            thumbVisibility: WidgetStateProperty<bool?>.All(true),
            thickness: WidgetStateProperty<double?>.ResolveWith(states =>
                states.Contains(WidgetState.Hovered) ? 20 : 12),
            trackVisibility: WidgetStateProperty<bool?>.All(true),
            thumbColor: WidgetStateProperty<Color?>.All(Colors.White),
            radius: 10,
            interactive: true);
        ScrollbarThemeData firstHalf = ScrollbarThemeData.Lerp(start, end, 0.25);
        ScrollbarThemeData secondHalf = ScrollbarThemeData.Lerp(start, end, 0.75);

        Assert.False(firstHalf.ThumbVisibility!.Resolve(idleStates));
        Assert.True(secondHalf.ThumbVisibility!.Resolve(idleStates));
        Assert.Equal(6, firstHalf.Thickness!.Resolve(idleStates));
        Assert.Equal(8, firstHalf.Thickness.Resolve(hoveredStates));
        Assert.Equal(4, firstHalf.Radius);
        Assert.False(firstHalf.Interactive);
        Assert.True(secondHalf.Interactive);

        // The nearest `ScrollbarTheme` wins over the one installed by `ThemeData`.
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new Theme(
            data: ThemeData.Light with
            {
                Platform = TargetPlatform.Windows,
                ScrollbarTheme = start,
            },
            child: new ScrollbarTheme(
                data: end,
                child: new MaterialScrollbar(
                    controller: controller,
                    child: BuildVerticalList(controller, 30)))));
        Settle(harness);
        Assert.Equal(12, RequirePainter(harness).Thickness);

        var inherited = new ScrollbarTheme(end, new SizedBox());
        Assert.IsAssignableFrom<InheritedTheme>(inherited);
        Assert.Equal(end, Assert.IsType<ScrollbarTheme>(inherited.Wrap(null!, new SizedBox())).Data);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MaterialScrollbar_UsesDirectColorSchemeRolesWithoutMaterialVersionSplit(bool useMaterial3)
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        ColorScheme scheme = ThemeData.Light.ColorScheme.CopyWith(onSurface: Colors.Magenta);
        var data = new ThemeData(
            colorScheme: scheme,
            platform: TargetPlatform.Windows,
            useMaterial3: useMaterial3);
        using var harness = new WidgetRenderHarness(new Theme(
            data: data,
            child: new MaterialScrollbar(
                controller: controller,
                thumbVisibility: true,
                child: BuildVerticalList(controller, 30))));
        Settle(harness);

        Assert.Equal(WithOpacity(Colors.Magenta, 0.10), RequirePainter(harness).Color);
    }

    [DebugOnlyFact]
    public void RawScrollbar_ForcedVisibilityRequiresExactlyOneAttachedPosition()
    {
        using var timers = new FakeGestureTimers();
        Scheduler.ResetForTests();
        using var missingControllerHarness = new WidgetRenderHarness(new RawScrollbar(
            thumbVisibility: true,
            child: new SizedBox()));
        missingControllerHarness.Pump(ViewportSize);
        double schedulerNow = Scheduler.CurrentSeconds;

        // Dart's `_invokeFrameCallback` reports a throwing post-frame callback through
        // `FlutterError.reportError` rather than letting it escape the frame.
        InvalidOperationException missing = Assert.IsType<InvalidOperationException>(
            PumpAndCaptureReportedError(TimeSpan.FromSeconds(schedulerNow + 0.01)));
        Assert.Contains("ScrollController", missing.Message);

        Scheduler.ResetForTests();
        using var controller = new ScrollController();
        using var multipleHarness = new WidgetRenderHarness(new RawScrollbar(
            controller: controller,
            thumbVisibility: true,
            child: new Stack(
                children:
                [
                    BuildVerticalList(controller, 20),
                    BuildVerticalList(controller, 20),
                ])));
        multipleHarness.Pump(ViewportSize);
        schedulerNow = Scheduler.CurrentSeconds;
        FlutterError multiple = Assert.IsType<FlutterError>(
            PumpAndCaptureReportedError(TimeSpan.FromSeconds(schedulerNow + 0.01)));
        Assert.Contains("more than one ScrollPosition", multiple.Message);
        Scheduler.ResetForTests();
    }

    private static object PumpAndCaptureReportedError(TimeSpan timestamp)
    {
        List<FlutterErrorDetails> reported = [];
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = reported.Add;
        try
        {
            Scheduler.PumpFrameForTests(timestamp);
        }
        finally
        {
            FlutterError.OnError = previous;
        }

        return Assert.Single(reported).Exception;
    }

    [Fact]
    public void RawScrollbar_ThumbVisibilityToggleFadesWhileIdle()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(BuildMaterialScrollbar(controller, thumbVisibility: true));
        Settle(harness);
        Assert.Equal(1, RequirePainter(harness).FadeoutOpacityAnimation.Value);

        harness.UpdateWidget(BuildMaterialScrollbar(controller, thumbVisibility: false));
        AdvanceAndPump(harness, 0.4);
        Assert.Equal(0, RequirePainter(harness).FadeoutOpacityAnimation.Value, precision: 3);

        harness.UpdateWidget(BuildMaterialScrollbar(controller, thumbVisibility: true));
        AdvanceAndPump(harness, 0.4);
        Assert.Equal(1, RequirePainter(harness).FadeoutOpacityAnimation.Value, precision: 3);
    }

    [Fact]
    public void MaterialScrollbar_ThumbColorUsesTwoHundredMillisecondHoverTransition()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new Theme(
            data: ThemeData.Light with { Platform = TargetPlatform.Windows },
            child: new MaterialScrollbar(
                controller: controller,
                thumbVisibility: true,
                child: BuildVerticalList(controller, 30))));
        Settle(harness);

        Color idle = WithOpacity(ThemeData.Light.ColorScheme.OnSurface, 0.10);
        Color hovered = WithOpacity(ThemeData.Light.ColorScheme.OnSurface, 0.50);
        Assert.Equal(idle, RequirePainter(harness).Color);

        Point point = RequirePainter(harness).Geometry!.Value.ThumbRect.Center;
        Dispatch(harness, new PointerHoverEvent(
            41, PointerDeviceKind.Mouse, point, PointerButtons.None, PressTime));
        harness.Pump(ViewportSize);
        Assert.Equal(idle, RequirePainter(harness).Color);

        AdvanceAndPump(harness, 0.1);
        Color halfway = RequirePainter(harness).Color;
        Assert.NotEqual(idle, halfway);
        Assert.NotEqual(hovered, halfway);

        AdvanceAndPump(harness, 0.15);
        Assert.Equal(hovered, RequirePainter(harness).Color);
    }

    [Fact]
    public void ScrollbarPainter_ExposesFlutterGeometryHitTestsAndInfiniteExtentGuard()
    {
        using var painter = new ScrollbarPainter(
            color: Colors.Crimson,
            fadeoutOpacityAnimation: new ConstantAnimation<double>(1),
            textDirection: TextDirection.Ltr,
            thickness: 8,
            crossAxisMargin: 2,
            radius: 4,
            minLength: 48);
        FixedScrollMetrics metrics = TestScrollMetrics(0, 0, 560, 240);
        painter.Update(metrics, AxisDirection.Down);
        using var harness = new WidgetRenderHarness(new CustomPaint(
            foregroundPainter: painter,
            size: ViewportSize));
        harness.Pump(ViewportSize);

        ScrollbarGeometry geometry = Assert.IsType<ScrollbarGeometry>(painter.Geometry);
        Assert.Equal(new Rect(188, 0, 12, 240), geometry.TrackRect);
        Assert.Equal(new Rect(190, 0, 8, 72), geometry.ThumbRect);
        Assert.True(painter.HitTestOnlyThumbInteractive(
            new Point(geometry.ThumbRect.Left - 20, geometry.ThumbRect.Center.Y),
            PointerDeviceKind.Touch));
        Assert.False(painter.HitTestOnlyThumbInteractive(
            new Point(geometry.ThumbRect.Left - 30, geometry.ThumbRect.Center.Y),
            PointerDeviceKind.Mouse));
        // `getTrackToScroll` maps a *delta* along the thumb track onto a scroll delta.
        Assert.Equal(560, painter.GetTrackToScroll(geometry.MaxThumbTravel), precision: 3);
        Assert.Equal(0, painter.GetThumbScrollOffset(), precision: 3);
        Assert.Equal(geometry.MaxThumbTravel, painter.GetScrollToTrack(560), precision: 3);

        painter.Update(TestScrollMetrics(280, 0, 560, 240), AxisDirection.Down);
        harness.Pump(ViewportSize);
        Assert.Equal(geometry.MaxThumbTravel / 2, painter.GetThumbScrollOffset(), precision: 3);

        painter.Update(TestScrollMetrics(0, 0, double.PositiveInfinity, 240), AxisDirection.Down);
        harness.Pump(ViewportSize);
        Assert.Null(painter.Geometry);
    }

    [Fact]
    public void RawScrollbar_ZeroAreaDoesNotPaintOrCrash()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new RawScrollbar(
            controller: controller,
            thumbVisibility: true,
            child: BuildVerticalList(controller, 20)));

        var emptySize = new Size(0, 0);
        harness.Pump(emptySize);
        Scheduler.FlushMicrotasks();
        harness.Pump(emptySize);

        Assert.Null(RequirePainter(harness).Geometry);
    }

    // Flutter: "Track offset respects MediaQuery padding" / "RawScrollbar.padding replaces MediaQueryData.padding".
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RawScrollbar_InheritedPaddingAndExplicitReplacementMatchFlutter(bool explicitPadding)
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new MediaQuery(
            data: new MediaQueryData(Padding: new Thickness(50)),
            child: new RawScrollbar(
                controller: controller,
                thumbVisibility: true,
                padding: explicitPadding ? new Thickness(100) : null,
                minThumbLength: 21,
                minOverscrollLength: 8,
                child: new SingleChildScrollView(
                    controller: controller,
                    child: new SizedBox(width: 1000, height: 50000)))));
        Settle(harness, new Size(800, 600));

        ScrollbarGeometry geometry = RequirePainter(harness).Geometry!.Value;
        Assert.Equal(explicitPadding ? new Rect(694, 100, 6, 400) : new Rect(744, 50, 6, 500), geometry.TrackRect);
        Assert.Equal(explicitPadding ? new Rect(694, 100, 6, 21) : new Rect(744, 50, 6, 21), geometry.ThumbRect);
    }

    [Fact]
    public void RawScrollbar_PaddingChangesRepaintTheSameWidgetWithoutMovingItsChild()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        var scrollbar = new RawScrollbar(
            controller: controller,
            thumbVisibility: true,
            child: BuildVerticalList(controller, 20));
        using var harness = new WidgetRenderHarness(new MediaQuery(data: new MediaQueryData(), child: scrollbar));
        Settle(harness);
        ScrollbarPainter painter = RequirePainter(harness);
        controller.JumpTo(80);
        harness.UpdateWidget(new MediaQuery(
            data: new MediaQueryData(Padding: new Thickness(7, 11, 13, 17)),
            child: scrollbar));
        Settle(harness);

        Assert.Same(painter, RequirePainter(harness));
        Assert.Equal(new Rect(181, 11, 6, 212), painter.Geometry!.Value.TrackRect);
        Assert.Equal(80, controller.Offset);
        Assert.Equal(240, controller.Position.ViewportDimension);
    }

    [Fact]
    public void RawScrollbar_ExplicitZeroPaddingWorksWithoutMediaQuery()
    {
        using var harness = new WidgetRenderHarness(
            new RawScrollbar(padding: new Thickness(), child: new SizedBox()), provideMediaQuery: false);
        Assert.Equal(EdgeInsetsGeometry.Zero, RequirePainter(harness).Padding);
    }

    [DebugOnlyFact]
    public void RawScrollbar_MissingMediaQueryReportsAnErrorInsteadOfAssumingZeroPadding()
    {
        List<FlutterErrorDetails> reported = [];
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = reported.Add;
        try
        {
            using var harness = new WidgetRenderHarness(
                new RawScrollbar(child: new SizedBox()), provideMediaQuery: false);
            Assert.Contains(reported, error => error.Exception is FlutterError flutterError &&
                flutterError.Message.Contains("No MediaQuery widget ancestor found.", StringComparison.Ordinal));
        }
        finally
        {
            FlutterError.OnError = previous;
        }
    }

    // Flutter: "with EdgeInsetsDirectional".
    [Fact]
    public void ScrollbarPainter_DirectionalPaddingResolvesAgainWhenDirectionChanges()
    {
        using var painter = new ScrollbarPainter(
            color: Colors.Crimson,
            fadeoutOpacityAnimation: new ConstantAnimation<double>(1),
            textDirection: TextDirection.Ltr,
            padding: EdgeInsetsGeometry.DirectionalOnly(start: 1, top: 2, end: 3, bottom: 4));
        painter.Update(TestScrollMetrics(0, 0, 340, 80), AxisDirection.Down);
        painter.Paint(new PaintingContext(new OffsetLayer()), new Size(60, 80));
        Assert.Equal(new Rect(51, 2, 6, 74), painter.Geometry!.Value.TrackRect);
        painter.TextDirection = TextDirection.Rtl;
        painter.Paint(new PaintingContext(new OffsetLayer()), new Size(60, 80));
        Assert.Equal(new Rect(3, 2, 6, 74), painter.Geometry!.Value.TrackRect);
        painter.Padding = EdgeInsetsGeometry.DirectionalOnly(start: 5, end: 9);
        painter.Paint(new PaintingContext(new OffsetLayer()), new Size(60, 80));
        Assert.Equal(9, painter.Geometry!.Value.TrackRect.Left);
    }

    [DebugOnlyFact]
    public void ScrollbarPainter_ValidatesPaddingDirectionAndShapeContracts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ScrollbarPainter(
            Colors.Crimson, new ConstantAnimation<double>(1), padding: new Thickness(-1)));
        Assert.Throws<ArgumentNullException>(() => new ScrollbarPainter(
            Colors.Crimson, new ConstantAnimation<double>(1), padding: EdgeInsetsGeometry.DirectionalOnly(start: 1)));
        using var painter = new ScrollbarPainter(
            Colors.Crimson, new ConstantAnimation<double>(1), shape: new CircleBorder());
        painter.Update(TestScrollMetrics(0, 0, 560, 240), AxisDirection.Down);
        Assert.Throws<InvalidOperationException>(() => painter.Paint(
            new PaintingContext(new OffsetLayer()), ViewportSize));
        Assert.Throws<ArgumentException>(() => painter.UpdateThickness(8, Radius.Circular(4)));
        painter.TextDirection = TextDirection.Ltr;
        painter.ScrollbarOrientation = ScrollbarOrientation.Top;
        Assert.Throws<InvalidOperationException>(() => painter.Paint(
            new PaintingContext(new OffsetLayer()), ViewportSize));
    }

    [Theory]
    [InlineData(AxisDirection.Down)]
    [InlineData(AxisDirection.Up)]
    [InlineData(AxisDirection.Left)]
    [InlineData(AxisDirection.Right)]
    public void ScrollbarPainter_UsesMetricsViewportAndAcceptsNegativeMargins(AxisDirection direction)
    {
        using var painter = new ScrollbarPainter(
            Colors.Crimson, new ConstantAnimation<double>(1), textDirection: TextDirection.Ltr,
            mainAxisMargin: -10, padding: new Thickness(1, 2, 3, 4));
        painter.Update(TestScrollMetrics(0, 0, 560, 80), direction);
        painter.Paint(new PaintingContext(new OffsetLayer()), new Size(200, 240));
        ScrollbarGeometry geometry = painter.Geometry!.Value;
        bool vertical = direction is AxisDirection.Down or AxisDirection.Up;
        Assert.Equal(vertical ? 74 : 76, vertical ? geometry.TrackRect.Height : geometry.TrackRect.Width);
        Assert.Equal(vertical ? -8 : -9, geometry.TrackMainAxisStart);
        Assert.Equal(vertical ? 94 : 96, geometry.TrackMainAxisExtent);
    }

    [Fact]
    public void ScrollbarPainter_InfiniteMinimumLengthsFitTrackAndDeepOverscrollUsesFixedContentExtent()
    {
        using var painter = new ScrollbarPainter(
            Colors.Crimson, new ConstantAnimation<double>(1), textDirection: TextDirection.Ltr,
            minLength: double.PositiveInfinity, minOverscrollLength: double.PositiveInfinity);
        painter.Update(TestScrollMetrics(0, 0, 560, 240), AxisDirection.Down);
        painter.Paint(new PaintingContext(new OffsetLayer()), ViewportSize);
        Assert.Equal(240, painter.Geometry!.Value.ThumbRect.Height);
        painter.MinOverscrollLength = 8;
        painter.MinLength = 36;
        painter.Update(TestScrollMetrics(-24, 0, 560, 240), AxisDirection.Down);
        painter.Paint(new PaintingContext(new OffsetLayer()), ViewportSize);
        Assert.Equal(64.8, painter.Geometry!.Value.ThumbRect.Height, precision: 3);
        painter.Update(TestScrollMetrics(-300, 0, 560, 240), AxisDirection.Down);
        painter.Paint(new PaintingContext(new OffsetLayer()), ViewportSize);
        Assert.Equal(8, painter.Geometry!.Value.ThumbRect.Height);
        Assert.Equal(0, painter.Geometry!.Value.ThumbRect.Top);
        Assert.Equal(double.PositiveInfinity,
            new RawScrollbar(child: new SizedBox(), minThumbLength: double.PositiveInfinity).MinThumbLength);
    }

    [Fact]
    public void ScrollbarPainter_NotifiesOnlyChangedExtentsThatMayNeedPainting()
    {
        using var painter = new ScrollbarPainter(Colors.Crimson, new ConstantAnimation<double>(1));
        int notifications = 0;
        Action listener = () => notifications++;
        painter.AddListener(listener);
        painter.Update(TestScrollMetrics(0, 0, 0, 240), AxisDirection.Down);
        Assert.Equal(0, notifications);
        painter.Update(TestScrollMetrics(0, 0, 560, 240), AxisDirection.Down);
        Assert.Equal(1, notifications);
        painter.Update(TestScrollMetrics(0, 0, 560, 240), AxisDirection.Down);
        Assert.Equal(1, notifications);
        // Shifting pixels and both bounds preserves all three extents.
        painter.Update(TestScrollMetrics(10, 10, 570, 240), AxisDirection.Down);
        Assert.Equal(1, notifications);
        painter.Update(TestScrollMetrics(0, 0, 0, 240), AxisDirection.Down);
        Assert.Equal(2, notifications);
        painter.RemoveListener(listener);
        painter.Thickness = 8;
        Assert.Equal(2, notifications);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ScrollbarPainter_CustomShapeUsesItsInteriorOrPathThenPaintsItsBorder(bool preferInterior)
    {
        var shape = new RecordingScrollbarBorder(preferInterior);
        using var painter = new ScrollbarPainter(
            Colors.Crimson, new ConstantAnimation<double>(0.5), textDirection: TextDirection.Ltr, shape: shape);
        painter.Update(TestScrollMetrics(0, 0, 560, 240), AxisDirection.Down);
        painter.Paint(new PaintingContext(new OffsetLayer()), ViewportSize);
        Assert.Equal(preferInterior ? new[] { "interior", "border" } : new[] { "path", "border" }, shape.Calls);
        Assert.Equal(new Rect(194, 0, 6, 72), shape.PaintedRect);
        if (preferInterior)
        {
            Assert.Equal(128, shape.InteriorColor.A);
        }
        Assert.Equal(Colors.Blue, shape.Side.Color);
    }

    private sealed record RecordingScrollbarBorder(bool PreferInterior) : OutlinedBorder(new BorderSide(Colors.Blue, 2))
    {
        public List<string> Calls { get; } = [];
        public Rect PaintedRect { get; private set; }
        public Color InteriorColor { get; private set; }
        public override bool PreferPaintInterior => PreferInterior;
        public override OutlinedBorder CopyWith(BorderSide? side = null) => this with { Side = side ?? Side };
        public override ShapeBorder Scale(double t) => this;
        public override Plumix.UI.Path GetInnerPath(Rect rect, TextDirection? textDirection = null) =>
            GetOuterPath(rect);
        public override Plumix.UI.Path GetOuterPath(Rect rect, TextDirection? textDirection = null)
        {
            Calls.Add("path");
            PaintedRect = rect;
            return new CircleBorder().GetOuterPath(rect);
        }
        public override void PaintInterior(
            PaintingContext context, Rect rect, IBrush brush, TextDirection? textDirection = null)
        {
            Calls.Add("interior");
            PaintedRect = rect;
            InteriorColor = ((SolidColorBrush)brush).Color;
            new CircleBorder().PaintInterior(context, rect, brush);
        }
        public override void Paint(PaintingContext context, Rect rect, TextDirection? textDirection = null)
        {
            Calls.Add("border");
            new CircleBorder(Side).Paint(context, rect);
        }
    }

    // Flutter trackpad regressions 149999 / 150236: content pans bypass the thumb recognizers.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RawScrollbar_TrackpadPanScrollsContentWithoutStartingThumbDrag(bool horizontal)
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new RawScrollbar(
            controller: controller, thumbVisibility: true,
            child: new SingleChildScrollView(
                controller: controller, scrollDirection: horizontal ? Axis.Horizontal : Axis.Vertical,
                child: new SizedBox(width: 1000, height: 1000))));
        Settle(harness);
        var position = new Point(100, 100);
        Point delta = horizontal ? new Point(-30, 0) : new Point(0, -30);
        Dispatch(harness, new PointerPanZoomStartEvent(91, position, PressTime));
        Dispatch(harness, new PointerPanZoomUpdateEvent(91, position, PressTime.AddMilliseconds(20),
            pan: delta, panDelta: delta));
        Dispatch(harness, new PointerPanZoomUpdateEvent(91, position, PressTime.AddMilliseconds(40),
            pan: delta * 2, panDelta: delta));
        Assert.InRange(controller.Offset, 30, 60);
        Dispatch(harness, new PointerPanZoomEndEvent(91, position, PressTime.AddMilliseconds(60)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RawScrollbar_ReversedThumbDragMovesWithThePointer(bool horizontal)
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new RawScrollbar(
            controller: controller, thumbVisibility: true,
            child: new SingleChildScrollView(
                controller: controller, reverse: true,
                scrollDirection: horizontal ? Axis.Horizontal : Axis.Vertical,
                child: new SizedBox(width: 1000, height: 1000))));
        Settle(harness);
        Rect before = RequirePainter(harness).Geometry!.Value.ThumbRect;
        Point start = before.Center;
        Point end = start + (horizontal ? new Point(-10, 0) : new Point(0, -10));
        Dispatch(harness, new PointerDownEvent(92, PointerDeviceKind.Mouse, start, PointerButtons.Primary, PressTime));
        Dispatch(harness, new PointerMoveEvent(
            92, PointerDeviceKind.Mouse, end, PointerButtons.Primary,
            down: true, timestampUtc: PressTime.AddMilliseconds(20)));
        harness.Pump(ViewportSize);
        Rect after = RequirePainter(harness).Geometry!.Value.ThumbRect;
        Assert.Equal(horizontal ? before.X - 10 : before.Y - 10, horizontal ? after.X : after.Y, precision: 3);
        Assert.True(controller.Offset > 10);
        Dispatch(harness, new PointerUpEvent(
            92, PointerDeviceKind.Mouse, end, PointerButtons.None, PressTime.AddMilliseconds(40)));
    }

    [Fact]
    public void RawScrollbar_NegativeMinimumExtentEnablesGesturesAndDraggingWhenMaximumIsZero()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        var center = new UniqueKey();
        using var harness = new WidgetRenderHarness(new RawScrollbar(
            controller: controller, thumbVisibility: true,
            child: new CustomScrollView(controller: controller, center: center, slivers:
            [
                new SliverToBoxAdapter(child: new SizedBox(height: 100)),
                new SliverToBoxAdapter(key: center, child: new SizedBox(height: 100)),
                new SliverToBoxAdapter(child: new SizedBox(height: 100)),
            ])));
        Settle(harness);
        Assert.True(controller.Position.MinScrollExtent < 0);
        Assert.True(controller.Position.MaxScrollExtent <= 0);
        var detector = Assert.IsType<RawGestureDetector>(FindWidgetObject<RawGestureDetector>(harness.RootElement));
        Assert.Equal(2, detector.Gestures.Count);
        Point start = RequirePainter(harness).Geometry!.Value.ThumbRect.Center;
        Point end = start - new Point(0, 10);
        Dispatch(harness, new PointerDownEvent(93, PointerDeviceKind.Mouse, start, PointerButtons.Primary, PressTime));
        Dispatch(harness, new PointerMoveEvent(
            93, PointerDeviceKind.Mouse, end, PointerButtons.Primary,
            down: true, timestampUtc: PressTime.AddMilliseconds(20)));
        Assert.True(controller.Offset < 0);
        Dispatch(harness, new PointerUpEvent(
            93, PointerDeviceKind.Mouse, end, PointerButtons.None, PressTime.AddMilliseconds(40)));
    }

    [Fact]
    public void RawScrollbar_ControllerAxisWinsConflictingScrollNotifications()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        BuildContext? source = null;
        using var harness = new WidgetRenderHarness(new RawScrollbar(
            controller: controller, thumbVisibility: true, notificationPredicate: _ => true,
            child: new Builder(context =>
            {
                source = context;
                return BuildVerticalList(controller, 20);
            })));
        Settle(harness);
        ScrollbarGeometry before = RequirePainter(harness).Geometry!.Value;
        var metrics = new FixedScrollMetrics(
            minScrollExtent: 0, maxScrollExtent: 700, pixels: 80,
            viewportDimension: 200, axisDirection: AxisDirection.Right, devicePixelRatio: 1);
        new ScrollUpdateNotification(metrics, scrollDelta: 80).Dispatch(source);
        harness.Pump(ViewportSize);
        Assert.Equal(before, RequirePainter(harness).Geometry!.Value);
    }

    [Fact]
    public void RawScrollbar_GrowingAndShrinkingContentUpdatesThumbAndRecognizers()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        Widget Build(int count) => new RawScrollbar(
            controller: controller, thumbVisibility: true, child: BuildVerticalList(controller, count));
        using var harness = new WidgetRenderHarness(Build(2));
        Settle(harness);
        Assert.Null(RequirePainter(harness).Geometry);
        var detector = Assert.IsType<RawGestureDetector>(FindWidgetObject<RawGestureDetector>(harness.RootElement));
        Assert.Empty(detector.Gestures);
        harness.UpdateWidget(Build(20));
        Settle(harness);
        Assert.NotNull(RequirePainter(harness).Geometry);
        detector = Assert.IsType<RawGestureDetector>(FindWidgetObject<RawGestureDetector>(harness.RootElement));
        Assert.Equal(2, detector.Gestures.Count);
        harness.UpdateWidget(Build(2));
        Settle(harness);
        Assert.Null(RequirePainter(harness).Geometry);
        detector = Assert.IsType<RawGestureDetector>(FindWidgetObject<RawGestureDetector>(harness.RootElement));
        Assert.Empty(detector.Gestures);
    }

    [Fact]
    public void RawScrollbar_ControllerVisibilityAndInteractivityCanChangeInTheSameFrame()
    {
        using var timers = new FakeGestureTimers();
        using var controller = new ScrollController();
        var child = BuildVerticalList(controller, 20);
        using var harness = new WidgetRenderHarness(new RawScrollbar(
            thumbVisibility: false, interactive: false, child: child));
        Settle(harness);
        harness.UpdateWidget(new RawScrollbar(
            controller: controller, thumbVisibility: true, interactive: true, child: child));
        Settle(harness);
        Assert.Equal(1, RequirePainter(harness).FadeoutOpacityAnimation.Value);
        Assert.NotNull(RequirePainter(harness).Geometry);
    }

    [Fact]
    public void ScrollbarPainter_HitTestsAreEmptyUntilItsFirstPaint()
    {
        using var painter = new ScrollbarPainter(
            Colors.Crimson, new ConstantAnimation<double>(1), textDirection: TextDirection.Ltr);
        painter.Update(TestScrollMetrics(0, 0, 560, 240), AxisDirection.Down);
        Assert.Null(painter.HitTest(new Point(197, 30)));
        Assert.False(painter.HitTestInteractive(new Point(197, 30), PointerDeviceKind.Mouse));
        painter.Paint(new PaintingContext(new OffsetLayer()), ViewportSize);
        Assert.True(painter.HitTest(new Point(197, 30)));
    }

    [Theory]
    [InlineData(0, 36)]
    [InlineData(30, 27)]
    [InlineData(60, 18)]
    [InlineData(90, 9)]
    [InlineData(120, 8)]
    [InlineData(180, 8)]
    public void ScrollbarPainter_OverscrollShrinksThumbGradually(double overscroll, double expectedLength)
    {
        using var painter = new ScrollbarPainter(
            Colors.Crimson, new ConstantAnimation<double>(1), textDirection: TextDirection.Ltr,
            minLength: 36, minOverscrollLength: 8);
        painter.Update(TestScrollMetrics(-overscroll, 0, 49400, 600), AxisDirection.Down);
        painter.Paint(new PaintingContext(new OffsetLayer()), new Size(800, 600));
        Assert.Equal(expectedLength, painter.Geometry!.Value.ThumbRect.Height, precision: 3);
        Assert.Equal(0, painter.Geometry!.Value.ThumbRect.Y);
    }

    [Fact]
    public void ScrollbarPainter_EveryMutablePropertyParticipatesInShouldRepaint()
    {
        var animation = new ConstantAnimation<double>(1);
        Action<ScrollbarPainter>[] changes =
        [
            painter => painter.Color = Colors.Blue,
            painter => painter.TrackColor = Colors.Blue,
            painter => painter.TrackBorderColor = Colors.Blue,
            painter => painter.TextDirection = TextDirection.Rtl,
            painter => painter.Thickness = 9,
            painter => painter.MainAxisMargin = -10,
            painter => painter.CrossAxisMargin = 2,
            painter => painter.Radius = Radius.Elliptical(4, 8),
            painter => painter.TrackRadius = Radius.Elliptical(8, 4),
            painter => painter.Shape = new CircleBorder(),
            painter => painter.Padding = EdgeInsetsGeometry.DirectionalOnly(start: 9),
            painter => painter.MinLength = 24,
            painter => painter.MinOverscrollLength = 8,
            painter => painter.ScrollbarOrientation = ScrollbarOrientation.Left,
            painter => painter.IgnorePointer = true,
        ];
        using var original = new ScrollbarPainter(Colors.Crimson, animation, textDirection: TextDirection.Ltr);
        foreach (Action<ScrollbarPainter> change in changes)
        {
            using var changed = new ScrollbarPainter(Colors.Crimson, animation, textDirection: TextDirection.Ltr);
            Assert.False(changed.ShouldRepaint(original));
            change(changed);
            Assert.True(changed.ShouldRepaint(original));
        }
        using var differentAnimation = new ScrollbarPainter(
            Colors.Crimson, new ConstantAnimation<double>(1), textDirection: TextDirection.Ltr);
        Assert.True(differentAnimation.ShouldRepaint(original));
    }

    private static void Dispatch(WidgetRenderHarness harness, PointerEvent @event)
    {
        harness.RegisterForPointerEvents();
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, @event);
    }

    /// <summary>
    /// Lays out and paints, delivers the queued <c>ScrollMetricsNotification</c> (which is what gives
    /// the scrollbar its axis and installs its recognizers), then runs the fade to completion.
    /// </summary>
    private void Settle(WidgetRenderHarness harness) => Settle(harness, ViewportSize);

    private void Settle(WidgetRenderHarness harness, Size size)
    {
        harness.Pump(size);
        Scheduler.FlushMicrotasks();
        harness.Pump(size);
        AdvanceAndPump(harness, 0.4, size);
    }

    private void AdvanceAndPump(WidgetRenderHarness harness, double seconds) =>
        AdvanceAndPump(harness, seconds, ViewportSize);

    private void AdvanceAndPump(WidgetRenderHarness harness, double seconds, Size size)
    {
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(_clock));
        _clock += seconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(_clock));
        Scheduler.FlushMicrotasks();
        harness.Pump(size);
    }

    private static ScrollbarPainter RequirePainter(WidgetRenderHarness harness)
    {
        var paint = Assert.IsType<CustomPaint>(FindWidgetObject<CustomPaint>(harness.RootElement));
        return Assert.IsType<ScrollbarPainter>(paint.ForegroundPainter);
    }

    private static Widget BuildMaterialScrollbar(ScrollController controller, bool thumbVisibility) => new Theme(
        data: ThemeData.Light with { Platform = TargetPlatform.Windows },
        child: new MaterialScrollbar(
            controller: controller,
            thumbVisibility: thumbVisibility,
            child: BuildVerticalList(controller, 30)));

    private static Widget BuildVerticalList(ScrollController controller, int count) => ListView.Builder(
        itemCount: count,
        controller: controller,
        itemExtent: 40,
        itemBuilder: (_, index) => new SizedBox(height: 40, child: new Text($"row {index}")),
        addAutomaticKeepAlives: false);

    private static FixedScrollMetrics TestScrollMetrics(
        double pixels,
        double minScrollExtent,
        double maxScrollExtent,
        double viewportDimension) => new(
        minScrollExtent: minScrollExtent,
        maxScrollExtent: maxScrollExtent,
        pixels: pixels,
        viewportDimension: viewportDimension,
        axisDirection: AxisDirection.Down,
        devicePixelRatio: 1.0);

    private static Widget? FindWidgetObject<T>(Element? element) where T : Widget
    {
        if (element is null) return null;
        if (element.Widget is T match) return match;
        Widget? result = null;
        element.VisitChildren(child => result ??= FindWidgetObject<T>(child));
        return result;
    }

    // Dart's `Color.withOpacity` replaces the alpha channel outright, rounding to the nearest byte.
    private static Color WithOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Round(Math.Clamp(opacity, 0, 1) * 255), color.R, color.G, color.B);

    private static Widget Wrap(Widget widget) => new MediaQuery(data: new MediaQueryData(), child: widget);

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _root;
        private readonly PipelineOwner _pipeline;
        private bool _registeredForPointerEvents;
        private readonly bool _provideMediaQuery;

        public WidgetRenderHarness(Widget widget, bool provideMediaQuery = true)
        {
            _provideMediaQuery = provideMediaQuery;
            RenderView = new RenderView(new FlutterView(new Size(800, 600)));
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _root = new HarnessRootElement(RenderView, provideMediaQuery ? Wrap(widget) : widget);
            _root.Attach(_owner);
            _owner.BuildScope(_root, () => _root.Mount(parent: null, newSlot: null));
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }
        public Element RootElement => _root;

        public void RegisterForPointerEvents()
        {
            if (_registeredForPointerEvents)
            {
                return;
            }

            RendererBinding.Instance.AddRenderView(RenderView);
            _registeredForPointerEvents = true;
        }

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public void UpdateWidget(Widget widget)
        {
            _root.Update(_provideMediaQuery ? Wrap(widget) : widget);
            _owner.FlushBuild();
        }

        public void Dispose()
        {
            try
            {
                _root.UnmountRoot();
            }
            finally
            {
                if (_registeredForPointerEvents)
                {
                    RendererBinding.Instance.RemoveRenderView(RenderView);
                }
            }
        }
    }

    private sealed class WidgetTree : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly TreeRootElement _root;

        public WidgetTree(Widget widget)
        {
            _root = new TreeRootElement(Wrap(widget));
            _root.Attach(_owner);
            _owner.BuildScope(_root, () => _root.Mount(parent: null, newSlot: null));
            _owner.FlushBuild();
        }

        public T? FindWidget<T>() where T : Widget => FindWidget<T>(_root.Child);

        public void Dispose() => _root.UnmountRoot();

        private static T? FindWidget<T>(Element? element) where T : Widget
        {
            if (element is null) return null;
            if (element.Widget is T match) return match;
            T? result = null;
            element.VisitChildren(child => result ??= FindWidget<T>(child));
            return result;
        }
    }

    private sealed class HarnessRootElement : Element, IRenderObjectHost
    {
        private readonly RenderView _renderView;
        private Element? _child;

        public HarnessRootElement(RenderView renderView, Widget widget) : base(widget) => _renderView = renderView;
        public override RenderObject? RenderObject => _child?.RenderObject;
        public override Element? RenderObjectAttachingChild => _child;
        protected override void OnMount() { base.OnMount(); Rebuild(); }
        protected override void PerformRebuild() { base.PerformRebuild(); _child = UpdateChild(_child, Widget, Slot); }
        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Owner!.BuildScope(this, () => Rebuild(force: true));
        }
        public override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
        public override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
        public void InsertRenderObjectChild(RenderObject child, object? slot) => _renderView.Child = (RenderBox)child;
        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
        public void RemoveRenderObjectChild(RenderObject child, object? slot) { if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null; }
    }

    private sealed class TreeRootElement : Element, IRenderObjectHost
    {
        public TreeRootElement(Widget widget) : base(widget) { }
        public Element? Child { get; private set; }
        public override RenderObject? RenderObject => Child?.RenderObject;
        public override Element? RenderObjectAttachingChild => Child;
        protected override void OnMount() { base.OnMount(); Rebuild(); }
        protected override void PerformRebuild() { base.PerformRebuild(); Child = UpdateChild(Child, Widget, Slot); }
        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Owner!.BuildScope(this, () => Rebuild(force: true));
        }
        public override void VisitChildren(Action<Element> visitor) { if (Child is not null) visitor(Child); }
        public override void ForgetChild(Element child) { if (ReferenceEquals(Child, child)) Child = null; }
        public void InsertRenderObjectChild(RenderObject child, object? slot) { }
        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
        public void RemoveRenderObjectChild(RenderObject child, object? slot) { }
    }
}
