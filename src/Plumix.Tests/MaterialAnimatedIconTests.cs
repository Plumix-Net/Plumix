using System.Reflection;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialAnimatedIconTests
{
    [Fact]
    public void AnimatedIcons_ExposesCompleteFlutterCatalogWithSourceMetadata()
    {
        var catalog = typeof(AnimatedIcons)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(AnimatedIconData))
            .ToDictionary(property => property.Name, property => (AnimatedIconData)property.GetValue(null)!);

        Assert.Equal(
            [
                "AddEvent",
                "ArrowMenu",
                "CloseMenu",
                "EllipsisSearch",
                "EventAdd",
                "HomeMenu",
                "ListView",
                "MenuArrow",
                "MenuClose",
                "MenuHome",
                "PausePlay",
                "PlayPause",
                "SearchEllipsis",
                "ViewList",
            ],
            catalog.Keys.OrderBy(name => name));
        Assert.True(catalog["ArrowMenu"].MatchTextDirection);
        Assert.True(catalog["MenuArrow"].MatchTextDirection);
        Assert.True(catalog["ListView"].MatchTextDirection);
        Assert.True(catalog["ViewList"].MatchTextDirection);
        Assert.False(catalog["PlayPause"].MatchTextDirection);

        var addEvent = Assert.IsType<AnimatedIconDataImpl>(catalog["AddEvent"]);
        var ellipsisSearch = Assert.IsType<AnimatedIconDataImpl>(catalog["EllipsisSearch"]);
        var viewList = Assert.IsType<AnimatedIconDataImpl>(catalog["ViewList"]);
        Assert.Equal(new Size(48, 48), addEvent.Size);
        Assert.Equal(5, addEvent.Paths.Count);
        Assert.Equal(new Size(96, 96), ellipsisSearch.Size);
        Assert.Equal(5, ellipsisSearch.Paths.Count);
        Assert.Equal(12, viewList.Paths.Count);
    }

    [Fact]
    public void AnimatedIcon_ResolvesThemeSizeColorOpacitySemanticsAndRtlMirroring()
    {
        using var animation = new AnimationController(duration: TimeSpan.FromMilliseconds(200));
        animation.SetValue(0.5);
        var color = Color.Parse("#FF2468AC");
        using var harness = new WidgetRenderHarness(
            new Directionality(
                TextDirection.Rtl,
                new IconTheme(
                    new IconThemeData(Color: color, Size: 36, Opacity: 0.5),
                    new AnimatedIcon(
                        AnimatedIcons.MenuArrow,
                        animation,
                        semanticLabel: "Open navigation"))));

        harness.Pump(new Size(100, 100));

        var customPaint = Assert.IsType<RenderCustomPaint>(FindDescendant<RenderCustomPaint>(harness.RenderView));
        var painter = Assert.IsType<AnimatedIconPainter>(customPaint.Painter);
        var semantics = Assert.IsType<RenderSemanticsAnnotations>(
            FindDescendant<RenderSemanticsAnnotations>(harness.RenderView));
        Assert.Equal(new Size(36, 36), customPaint.Size);
        Assert.Equal(0.75, painter.Scale, 10);
        Assert.Equal(Color.FromArgb(128, color.R, color.G, color.B), painter.Color);
        Assert.True(painter.ShouldMirror);
        Assert.Equal("Open navigation", semantics.Label);
    }

    [Fact]
    public void AnimatedIcon_ExplicitValuesOverrideThemeAndNonDirectionalIconDoesNotMirror()
    {
        using var animation = new AnimationController(duration: TimeSpan.FromMilliseconds(200));
        var explicitColor = Color.Parse("#FF9A3412");
        using var harness = new WidgetRenderHarness(
            new Directionality(
                TextDirection.Rtl,
                new IconTheme(
                    new IconThemeData(Color: Colors.Blue, Size: 18, Opacity: 0.25),
                    new AnimatedIcon(
                        AnimatedIcons.PlayPause,
                        animation,
                        color: explicitColor,
                        size: 40,
                        textDirection: TextDirection.Ltr))));

        harness.Pump(new Size(100, 100));

        var customPaint = Assert.IsType<RenderCustomPaint>(FindDescendant<RenderCustomPaint>(harness.RenderView));
        var painter = Assert.IsType<AnimatedIconPainter>(customPaint.Painter);
        Assert.Equal(new Size(40, 40), customPaint.Size);
        Assert.Equal((byte)64, painter.Color.A);
        Assert.Equal((explicitColor.R, explicitColor.G, explicitColor.B),
            (painter.Color.R, painter.Color.G, painter.Color.B));
        Assert.False(painter.ShouldMirror);
    }

    [Fact]
    public void AnimatedIcon_SourceFramesUseFlutterLinearFrameInterpolation()
    {
        var data = Assert.IsType<AnimatedIconDataImpl>(AnimatedIcons.MenuArrow);
        var move = Assert.IsType<PathMoveTo>(data.Paths[0].Commands[0]);

        Assert.Equal(new Point(6, 26), move.Interpolate(0.0));
        Point midpoint = move.Interpolate(0.5);
        Assert.Equal(36.23501636298456, midpoint.X, 10);
        Assert.Equal(12.973675163618006, midpoint.Y, 10);
        Assert.Equal(new Point(39.94921875, 22), move.Interpolate(1.0));
    }

    [Fact]
    public void AnimatedIcon_ProgressMarksCustomPaintDirtyWithoutWidgetRebuild()
    {
        using var animation = new AnimationController(duration: TimeSpan.FromMilliseconds(200));
        using var harness = new WidgetRenderHarness(
            new Directionality(
                TextDirection.Ltr,
                new AnimatedIcon(AnimatedIcons.ArrowMenu, animation)));
        harness.Pump(new Size(100, 100));
        var customPaint = Assert.IsType<RenderCustomPaint>(FindDescendant<RenderCustomPaint>(harness.RenderView));
        Assert.False(customPaint.NeedsPaint);

        animation.SetValue(0.5);

        Assert.True(customPaint.NeedsPaint);
        harness.FlushPaint();
        Assert.False(customPaint.NeedsPaint);
    }

    [Fact]
    public void AnimatedIcon_RejectsInvalidSize()
    {
        using var animation = new AnimationController(duration: TimeSpan.FromMilliseconds(200));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AnimatedIcon(AnimatedIcons.MenuArrow, animation, size: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AnimatedIcon(AnimatedIcons.MenuArrow, animation, size: double.NaN));
    }

    private static T? FindDescendant<T>(RenderObject? root) where T : RenderObject
    {
        if (root is null)
        {
            return null;
        }

        if (root is T target)
        {
            return target;
        }

        T? result = null;
        root.VisitChildren(child => result ??= FindDescendant<T>(child));
        return result;
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _rootElement;
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget rootWidget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _rootElement = new HarnessRootElement(RenderView, rootWidget);
            _rootElement.Attach(_owner);
            _rootElement.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public void FlushPaint()
        {
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public void Dispose() => _rootElement.Unmount();

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget)
            {
                _renderView = renderView;
            }

            public override RenderObject? RenderObject => _child?.RenderObject;
            public override Element? RenderObjectAttachingChild => _child;
            protected override void OnMount() { base.OnMount(); Rebuild(); }
            protected override void PerformRebuild()
            {
                base.PerformRebuild();
                _child = UpdateChild(_child, Widget, Slot);
            }
            public override void Update(Widget newWidget) { base.Update(newWidget); Rebuild(force: true); }
            public override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
            public override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
            public void InsertRenderObjectChild(RenderObject child, object? slot) =>
                _renderView.Child = (RenderBox)child;
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null;
            }
            public override void Unmount()
            {
                if (_child is not null) { UnmountChild(_child); _child = null; }
                base.Unmount();
            }
        }
    }
}
