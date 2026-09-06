using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
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

    [Fact]
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
        Assert.Throws<ArgumentOutOfRangeException>(() => new RawScrollbar(
            child: new SizedBox(),
            thickness: 0));
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
        Assert.Equal(6, painter.Radius);
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
        Assert.Equal(8, desktop.Radius);
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
        Assert.Equal(CupertinoScrollbar.DefaultRadius, cupertino.Radius);
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

    [Fact]
    public void RawScrollbar_ForcedVisibilityRequiresExactlyOneAttachedPosition()
    {
        using var timers = new FakeGestureTimers();
        Scheduler.ResetForTests();
        using var missingControllerHarness = new WidgetRenderHarness(new RawScrollbar(
            thumbVisibility: true,
            child: new SizedBox()));
        missingControllerHarness.Pump(ViewportSize);
        double schedulerNow = Scheduler.CurrentSeconds;
        InvalidOperationException missing = Assert.Throws<InvalidOperationException>(() =>
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(schedulerNow + 0.01)));
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
        InvalidOperationException multiple = Assert.Throws<InvalidOperationException>(() =>
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(schedulerNow + 0.01)));
        Assert.Contains("more than one ScrollPosition", multiple.Message);
        Scheduler.ResetForTests();
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

    private static void Dispatch(WidgetRenderHarness harness, PointerEvent @event) =>
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, @event);

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

        public void UpdateWidget(Widget widget)
        {
            _root.Update(widget);
            _owner.FlushBuild();
        }

        public void Dispose() => _root.Unmount();
    }

    private sealed class WidgetTree : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly TreeRootElement _root;

        public WidgetTree(Widget widget)
        {
            _root = new TreeRootElement(widget);
            _root.Attach(_owner);
            _root.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
        }

        public T? FindWidget<T>() where T : Widget => FindWidget<T>(_root.Child);

        public void Dispose() => _root.Unmount();

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
        public override void Update(Widget newWidget) { base.Update(newWidget); Rebuild(force: true); }
        public override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
        public override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
        public override void Unmount()
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
        public override void Update(Widget newWidget) { base.Update(newWidget); Rebuild(force: true); }
        public override void VisitChildren(Action<Element> visitor) { if (Child is not null) visitor(Child); }
        public override void ForgetChild(Element child) { if (ReferenceEquals(Child, child)) Child = null; }
        public override void Unmount() { if (Child is not null) { UnmountChild(Child); Child = null; } base.Unmount(); }
        public void InsertRenderObjectChild(RenderObject child, object? slot) { }
        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
        public void RemoveRenderObjectChild(RenderObject child, object? slot) { }
    }
}
