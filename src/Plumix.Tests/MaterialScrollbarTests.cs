using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Cupertino;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using MaterialScrollbar = Plumix.Material.Scrollbar;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialScrollbarTests
{
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
            shape: ShapeBorder.RoundedRectangle(4),
            radius: 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MaterialScrollbar(
            child: new SizedBox(),
            thickness: 0));
    }

    [Fact]
    public void RawScrollbar_OverlaysChildAndComputesVerticalGeometry()
    {
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new RawScrollbar(
            controller: controller,
            thumbVisibility: true,
            thickness: 6,
            radius: 3,
            minThumbLength: 18,
            child: BuildVerticalList(controller, 20)));

        harness.Pump(new Size(200, 240));

        var overlay = RequireOverlay(harness.RenderView);
        var geometry = Assert.IsType<ScrollbarGeometry>(overlay.Geometry);
        Assert.Equal(new Size(200, 240), overlay.Size);
        Assert.Equal(new Size(200, 240), overlay.Child!.Size);
        Assert.Equal(Axis.Vertical, geometry.Axis);
        Assert.Equal(new Rect(194, 0, 6, 240), geometry.TrackRect);
        Assert.Equal(72, geometry.ThumbRect.Height, precision: 3);
        Assert.Equal(0, geometry.ThumbRect.Y, precision: 3);
        Assert.Equal(6, overlay.Thickness);
        Assert.Equal(1, overlay.Opacity);
    }

    [Fact]
    public void RawScrollbar_HorizontalBottomGeometryHonorsMarginsAndPadding()
    {
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

        harness.Pump(new Size(300, 120));

        var overlay = RequireOverlay(harness.RenderView);
        var geometry = Assert.IsType<ScrollbarGeometry>(overlay.Geometry);
        Assert.Equal(Axis.Horizontal, geometry.Axis);
        Assert.Equal(12, geometry.TrackRect.X);
        Assert.Equal(270, geometry.TrackRect.Width);
        Assert.Equal(92, geometry.TrackRect.Y);
        Assert.True(overlay.TrackVisible);
        Assert.True(geometry.ThumbRect.Width >= 18);
    }

    [Fact]
    public void RawScrollbar_DefaultVerticalOrientationFollowsTextDirection()
    {
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new Directionality(
            textDirection: TextDirection.Rtl,
            child: new RawScrollbar(
                controller: controller,
                thumbVisibility: true,
                thickness: 6,
                child: BuildVerticalList(controller, 20))));

        harness.Pump(new Size(200, 240));

        var geometry = RequireOverlay(harness.RenderView).Geometry!.Value;
        Assert.Equal(0, geometry.TrackRect.X);
        Assert.Equal(ScrollbarOrientation.Left,
            Assert.IsType<RawScrollbarOverlay>(FindWidgetObject<RawScrollbarOverlay>(harness.RootElement)).Orientation);
    }

    [Fact]
    public void RawScrollbar_ControllerMovementShowsTransientThumb()
    {
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new RawScrollbar(
            controller: controller,
            child: BuildVerticalList(controller, 20)));

        harness.Pump(new Size(200, 240));
        Assert.Equal(0, RequireOverlay(harness.RenderView).Opacity);

        controller.JumpTo(80);
        harness.Pump(new Size(200, 240));

        Assert.Equal(1, RequireOverlay(harness.RenderView).Opacity);
        Assert.True(RequireOverlay(harness.RenderView).Geometry!.Value.ThumbRect.Y > 0);
    }

    [Fact]
    public void RawScrollbar_MouseHoverNearThumbRevealsFadedScrollbarButContentHoverDoesNot()
    {
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new RawScrollbar(
            controller: controller,
            child: BuildVerticalList(controller, 20)));
        harness.Pump(new Size(200, 240));

        var overlay = RequireOverlay(harness.RenderView);
        Assert.Equal(0, overlay.Opacity);

        var geometry = overlay.Geometry!.Value;
        var now = DateTime.UtcNow;
        var binding = GestureBinding.Instance;
        binding.HandlePointerEvent(harness.RenderView, new PointerHoverEvent(
            8,
            PointerDeviceKind.Mouse,
            new Point(100, geometry.ThumbRect.Center.Y),
            PointerButtons.None,
            now));
        harness.Pump(new Size(200, 240));
        Assert.Equal(0, RequireOverlay(harness.RenderView).Opacity);

        var proximityPoint = new Point(geometry.ThumbRect.Center.X - 23, geometry.ThumbRect.Center.Y);
        binding.HandlePointerEvent(harness.RenderView, new PointerHoverEvent(
            8,
            PointerDeviceKind.Mouse,
            proximityPoint,
            PointerButtons.None,
            now.AddMilliseconds(20)));
        double schedulerNow = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(schedulerNow + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(schedulerNow + 0.35));
        harness.Pump(new Size(200, 240));

        Assert.Equal(1, RequireOverlay(harness.RenderView).Opacity);
    }

    [Fact]
    public void RawScrollbar_ContentClickDoesNotTriggerTrackPaging()
    {
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
        harness.Pump(new Size(200, 240));

        var point = new Point(100, 100);
        var now = DateTime.UtcNow;
        var binding = GestureBinding.Instance;
        binding.HandlePointerEvent(harness.RenderView, new PointerDownEvent(
            18, PointerDeviceKind.Mouse, point, PointerButtons.Primary, now));
        binding.HandlePointerEvent(harness.RenderView, new PointerUpEvent(
            18, PointerDeviceKind.Mouse, point, PointerButtons.None, now.AddMilliseconds(20)));

        Assert.Equal(2, tappedIndex);
        Assert.Equal(0, controller.Offset);
    }

    [Fact]
    public void RawScrollbar_ThumbDragMapsTrackTravelToScrollExtent()
    {
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new RawScrollbar(
            controller: controller,
            thumbVisibility: true,
            interactive: true,
            child: BuildVerticalList(controller, 30)));
        harness.Pump(new Size(200, 240));

        var overlay = RequireOverlay(harness.RenderView);
        var geometry = overlay.Geometry!.Value;
        double x = geometry.ThumbRect.Center.X;
        double downY = geometry.ThumbRect.Center.Y;
        var now = DateTime.UtcNow;
        overlay.HandleEvent(new PointerDownEvent(
            pointer: 9,
            kind: PointerDeviceKind.Mouse,
            position: new Point(x, downY),
            buttons: PointerButtons.Primary,
            timestampUtc: now), new BoxHitTestEntry(overlay, new Point(x, downY)));
        overlay.HandleEvent(new PointerMoveEvent(
            pointer: 9,
            kind: PointerDeviceKind.Mouse,
            position: new Point(x, 235),
            buttons: PointerButtons.Primary,
            down: true,
            timestampUtc: now.AddMilliseconds(20)), new BoxHitTestEntry(overlay, new Point(x, 235)));
        overlay.HandleEvent(new PointerUpEvent(
            pointer: 9,
            kind: PointerDeviceKind.Mouse,
            position: new Point(x, 235),
            buttons: PointerButtons.None,
            timestampUtc: now.AddMilliseconds(30)), new BoxHitTestEntry(overlay, new Point(x, 235)));

        Assert.True(controller.Offset > controller.PrimaryPosition!.MaxScrollExtent * 0.9);
    }

    [Fact]
    public void RawScrollbar_ThumbDragWorksThroughGestureBindingPointerRoute()
    {
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new RawScrollbar(
            controller: controller,
            thumbVisibility: true,
            interactive: true,
            child: BuildVerticalList(controller, 30)));
        harness.Pump(new Size(200, 240));

        var geometry = RequireOverlay(harness.RenderView).Geometry!.Value;
        var start = geometry.ThumbRect.Center;
        var end = new Point(start.X, 235);
        var now = DateTime.UtcNow;
        var binding = GestureBinding.Instance;
        binding.HandlePointerEvent(harness.RenderView, new PointerDownEvent(
            19, PointerDeviceKind.Mouse, start, PointerButtons.Primary, now));
        binding.HandlePointerEvent(harness.RenderView, new PointerMoveEvent(
            19, PointerDeviceKind.Mouse, end, PointerButtons.Primary, true, now.AddMilliseconds(20)));
        binding.HandlePointerEvent(harness.RenderView, new PointerUpEvent(
            19, PointerDeviceKind.Mouse, end, PointerButtons.None, now.AddMilliseconds(30)));

        Assert.True(controller.Offset > controller.PrimaryPosition!.MaxScrollExtent * 0.9);
    }

    [Fact]
    public void RawScrollbar_TrackPressPagesTowardPointer()
    {
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new RawScrollbar(
            controller: controller,
            thumbVisibility: true,
            trackVisibility: true,
            interactive: true,
            child: BuildVerticalList(controller, 30)));
        harness.Pump(new Size(200, 240));

        var overlay = RequireOverlay(harness.RenderView);
        var geometry = overlay.Geometry!.Value;
        var point = new Point(geometry.TrackRect.Center.X, geometry.TrackRect.Bottom - 2);
        overlay.HandleEvent(new PointerDownEvent(
            pointer: 10,
            kind: PointerDeviceKind.Mouse,
            position: point,
            buttons: PointerButtons.Primary,
            timestampUtc: DateTime.UtcNow), new BoxHitTestEntry(overlay, point));

        Assert.Equal(240, controller.Offset, precision: 3);
    }

    [Fact]
    public void RawScrollbar_PressDurationDelaysThumbDrag()
    {
        using var controller = new ScrollController();
        using var harness = new WidgetRenderHarness(new RawScrollbar(
            controller: controller,
            thumbVisibility: true,
            interactive: true,
            pressDuration: TimeSpan.FromMilliseconds(100),
            child: BuildVerticalList(controller, 30)));
        harness.Pump(new Size(200, 240));

        var overlay = RequireOverlay(harness.RenderView);
        var geometry = overlay.Geometry!.Value;
        var start = geometry.ThumbRect.Center;
        var end = new Point(start.X, 235);
        var now = DateTime.UtcNow;
        overlay.HandleEvent(new PointerDownEvent(21, PointerDeviceKind.Touch, start, PointerButtons.Primary, now),
            new BoxHitTestEntry(overlay, start));
        overlay.HandleEvent(new PointerMoveEvent(21, PointerDeviceKind.Touch, end, PointerButtons.Primary, true, now.AddMilliseconds(20)),
            new BoxHitTestEntry(overlay, end));
        Assert.Equal(0, controller.Offset);

        double schedulerNow = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(schedulerNow + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(schedulerNow + 0.12));
        overlay.HandleEvent(new PointerMoveEvent(21, PointerDeviceKind.Touch, end, PointerButtons.Primary, true, now.AddMilliseconds(120)),
            new BoxHitTestEntry(overlay, end));

        Assert.True(controller.Offset > controller.PrimaryPosition!.MaxScrollExtent * 0.9);
    }

    [Fact]
    public void MaterialScrollbar_DesktopDefaultsAndThemeStatesMatchFlutter()
    {
        var themeData = new ScrollbarThemeData(
            thumbVisibility: MaterialStateProperty<bool?>.All(true),
            trackVisibility: MaterialStateProperty<bool?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Hovered)),
            thickness: MaterialStateProperty<double?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Hovered) ? 14 : 9),
            thumbColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Dragged) ? Colors.Crimson : Colors.DarkCyan),
            trackColor: MaterialStateProperty<Color?>.All(Colors.Beige),
            trackBorderColor: MaterialStateProperty<Color?>.All(Colors.Brown),
            radius: 6,
            crossAxisMargin: 4,
            mainAxisMargin: 3,
            minThumbLength: 52,
            interactive: true);

        using var tree = new WidgetTree(new Theme(
            data: ThemeData.Light with
            {
                Platform = TargetPlatform.Windows,
                ScrollbarTheme = themeData,
            },
            child: new MaterialScrollbar(child: new SizedBox())));

        var raw = Assert.IsType<RawScrollbar>(tree.FindWidget<RawScrollbar>());
        Assert.Equal(6, raw.Radius);
        Assert.Equal(4, raw.CrossAxisMargin);
        Assert.Equal(3, raw.MainAxisMargin);
        Assert.Equal(52, raw.MinThumbLength);
        Assert.True(raw.Interactive);
        Assert.True(raw.ThumbVisibilityResolver!(ScrollbarInteractionState.None));
        Assert.False(raw.TrackVisibilityResolver!(ScrollbarInteractionState.None));
        Assert.True(raw.TrackVisibilityResolver!(ScrollbarInteractionState.Hovered));
        Assert.Equal(9, raw.ThicknessResolver!(ScrollbarInteractionState.None));
        Assert.Equal(14, raw.ThicknessResolver!(ScrollbarInteractionState.Hovered));
        Assert.Equal(Colors.DarkCyan, raw.ThumbColorResolver!(ScrollbarInteractionState.None));
        Assert.Equal(Colors.Crimson, raw.ThumbColorResolver!(ScrollbarInteractionState.Dragged));
        Assert.Equal(Colors.Beige, raw.TrackColorResolver!(ScrollbarInteractionState.Hovered));
        Assert.Equal(Colors.Brown, raw.TrackBorderColorResolver!(ScrollbarInteractionState.Hovered));
    }

    [Fact]
    public void MaterialScrollbar_HoverAndDragUpdateResolvedPainterState()
    {
        using var controller = new ScrollController();
        var scrollbarTheme = new ScrollbarThemeData(
            thumbVisibility: MaterialStateProperty<bool?>.All(true),
            trackVisibility: MaterialStateProperty<bool?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Hovered)),
            thickness: MaterialStateProperty<double?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Hovered) ? 14 : 9),
            thumbColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Dragged) ? Colors.Crimson : Colors.DarkCyan));
        using var harness = new WidgetRenderHarness(new Theme(
            data: ThemeData.Light with
            {
                Platform = TargetPlatform.Windows,
                ScrollbarTheme = scrollbarTheme,
            },
            child: new MaterialScrollbar(
                controller: controller,
                child: BuildVerticalList(controller, 30))));
        harness.Pump(new Size(200, 240));

        var overlay = RequireOverlay(harness.RenderView);
        Assert.Equal(9, overlay.Thickness);
        Assert.Equal(Colors.DarkCyan, overlay.ThumbColor);
        Assert.False(overlay.TrackVisible);
        var point = overlay.Geometry!.Value.ThumbRect.Center;
        overlay.HandleEvent(new PointerHoverEvent(
            33, PointerDeviceKind.Mouse, point, PointerButtons.None, DateTime.UtcNow),
            new BoxHitTestEntry(overlay, point));
        harness.Pump(new Size(200, 240));

        overlay = RequireOverlay(harness.RenderView);
        Assert.Equal(14, overlay.Thickness);
        Assert.True(overlay.TrackVisible);

        point = overlay.Geometry!.Value.ThumbRect.Center;
        overlay.HandleEvent(new PointerDownEvent(
            34, PointerDeviceKind.Mouse, point, PointerButtons.Primary, DateTime.UtcNow),
            new BoxHitTestEntry(overlay, point));
        harness.Pump(new Size(200, 240));

        overlay = RequireOverlay(harness.RenderView);
        Assert.Equal(Colors.Crimson, overlay.ThumbColor);
    }

    [Fact]
    public void MaterialScrollbar_PlatformDefaultsMatchAndroidAndDesktopPaths()
    {
        using var androidTree = new WidgetTree(new Theme(
            data: ThemeData.Light with { Platform = TargetPlatform.Android },
            child: new MaterialScrollbar(child: new SizedBox())));
        var android = Assert.IsType<RawScrollbar>(androidTree.FindWidget<RawScrollbar>());
        Assert.Equal(0, android.Radius);
        Assert.Equal(0, android.CrossAxisMargin);
        Assert.False(android.Interactive);
        Assert.Equal(4, android.ThicknessResolver!(ScrollbarInteractionState.None));

        using var desktopTree = new WidgetTree(new Theme(
            data: ThemeData.Light with { Platform = TargetPlatform.Windows },
            child: new MaterialScrollbar(child: new SizedBox(), trackVisibility: true)));
        var desktop = Assert.IsType<RawScrollbar>(desktopTree.FindWidget<RawScrollbar>());
        Assert.Equal(8, desktop.Radius);
        Assert.Equal(2, desktop.CrossAxisMargin);
        Assert.True(desktop.Interactive);
        Assert.Equal(8, desktop.ThicknessResolver!(ScrollbarInteractionState.None));
        Assert.Equal(12, desktop.ThicknessResolver!(ScrollbarInteractionState.Hovered));
        Assert.Equal(ApplyOpacity(ThemeData.Light.OnSurfaceColor, 0.10),
            desktop.ThumbColorResolver!(ScrollbarInteractionState.None));
        Assert.Equal(ApplyOpacity(ThemeData.Light.OnSurfaceColor, 0.60),
            desktop.ThumbColorResolver!(ScrollbarInteractionState.Dragged));
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
        var raw = Assert.IsType<RawScrollbar>(tree.FindWidget<RawScrollbar>());
        Assert.Equal(CupertinoScrollbar.DefaultThickness, cupertino.Thickness);
        Assert.Equal(CupertinoScrollbar.DefaultThicknessWhileDragging, cupertino.ThicknessWhileDragging);
        Assert.Equal(CupertinoScrollbar.DefaultRadius, cupertino.Radius);
        Assert.Equal(TimeSpan.FromMilliseconds(250), raw.FadeDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(1200), raw.TimeToFade);
        Assert.Equal(TimeSpan.FromMilliseconds(100), raw.PressDuration);
        Assert.False(raw.TrackTapEnabled);
        Assert.Equal(3, raw.ThicknessResolver!(ScrollbarInteractionState.None));
        Assert.Equal(8, raw.ThicknessResolver!(ScrollbarInteractionState.Dragged));
        Assert.Equal(1.5, raw.RadiusResolver!(ScrollbarInteractionState.None));
        Assert.Equal(4, raw.RadiusResolver!(ScrollbarInteractionState.Dragged));
    }

    private static Widget BuildVerticalList(ScrollController controller, int count) => ListView.Builder(
        itemCount: count,
        controller: controller,
        itemExtent: 40,
        itemBuilder: (_, index) => new SizedBox(height: 40, child: new Text($"row {index}")),
        addAutomaticKeepAlives: false);

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

    private static Color ApplyOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Clamp((int)(255 * opacity), 0, 255), color.R, color.G, color.B);

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
        internal override Element? RenderObjectAttachingChild => _child;
        protected override void OnMount() { base.OnMount(); Rebuild(); }
        internal override void Rebuild() { Dirty = false; _child = UpdateChild(_child, Widget, Slot); }
        internal override void Update(Widget newWidget) { base.Update(newWidget); Rebuild(); }
        internal override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
        internal override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
        internal override void Unmount() { if (_child is not null) { UnmountChild(_child); _child = null; } base.Unmount(); }
        public void InsertRenderObjectChild(RenderObject child, object? slot) => _renderView.Child = (RenderBox)child;
        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
        public void RemoveRenderObjectChild(RenderObject child, object? slot) { if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null; }
    }

    private sealed class TreeRootElement : Element, IRenderObjectHost
    {
        public TreeRootElement(Widget widget) : base(widget) { }
        public Element? Child { get; private set; }
        public override RenderObject? RenderObject => Child?.RenderObject;
        internal override Element? RenderObjectAttachingChild => Child;
        protected override void OnMount() { base.OnMount(); Rebuild(); }
        internal override void Rebuild() { Dirty = false; Child = UpdateChild(Child, Widget, Slot); }
        internal override void Update(Widget newWidget) { base.Update(newWidget); Rebuild(); }
        internal override void VisitChildren(Action<Element> visitor) { if (Child is not null) visitor(Child); }
        internal override void ForgetChild(Element child) { if (ReferenceEquals(Child, child)) Child = null; }
        internal override void Unmount() { if (Child is not null) { UnmountChild(Child); Child = null; } base.Unmount(); }
        public void InsertRenderObjectChild(RenderObject child, object? slot) { }
        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
        public void RemoveRenderObjectChild(RenderObject child, object? slot) { }
    }
}
