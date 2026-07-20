using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class DebugPaintingWidgetsTests
{
    [Fact]
    public void Placeholder_UsesFlutterDefaultsAndPainterComposition()
    {
        var widget = new Placeholder();

        Assert.Equal(Color.FromRgb(0x45, 0x5A, 0x64), widget.Color);
        Assert.Equal(2.0, widget.StrokeWidth);
        Assert.Equal(400.0, widget.FallbackWidth);
        Assert.Equal(400.0, widget.FallbackHeight);
        Assert.Null(widget.Child);

        using var harness = new WidgetRenderHarness(
            new UnconstrainedBox(
                alignment: Alignment.TopLeft,
                child: widget));
        harness.Pump(new Size(800, 600));

        var paint = Assert.Single(FindDescendants<RenderCustomPaint>(harness.RenderView));
        var painter = Assert.IsType<PlaceholderPainter>(paint.Painter);
        Assert.Null(paint.ForegroundPainter);
        Assert.Equal(new Size(400, 400), paint.Size);
        Assert.Equal(widget.Color, painter.Color);
        Assert.Equal(widget.StrokeWidth, painter.StrokeWidth);
        Assert.False(painter.HitTest(default));
    }

    [Fact]
    public void Placeholder_UsesCustomFallbackOnlyOnUnboundedAxesAndPreservesChild()
    {
        var child = new SizedBox(width: 40, height: 24);
        using var unbounded = new WidgetRenderHarness(
            new UnconstrainedBox(
                alignment: Alignment.TopLeft,
                child: new Placeholder(
                    fallbackWidth: 123,
                    fallbackHeight: 77)));
        unbounded.Pump(new Size(300, 200));
        Assert.Equal(
            new Size(123, 77),
            Assert.Single(FindDescendants<RenderCustomPaint>(unbounded.RenderView)).Size);

        using var withChild = new WidgetRenderHarness(
            new UnconstrainedBox(
                alignment: Alignment.TopLeft,
                child: new Placeholder(
                    fallbackWidth: 123,
                    fallbackHeight: 77,
                    child: child)));
        withChild.Pump(new Size(300, 200));
        Assert.Equal(
            new Size(40, 24),
            Assert.Single(FindDescendants<RenderCustomPaint>(withChild.RenderView)).Size);

        using var bounded = new WidgetRenderHarness(
            new SizedBox(
                width: 80,
                height: 60,
                child: new Placeholder(
                    fallbackWidth: 123,
                    fallbackHeight: 77)));
        bounded.Pump(new Size(80, 60));
        Assert.Equal(
            new Size(80, 60),
            Assert.Single(FindDescendants<RenderCustomPaint>(bounded.RenderView)).Size);
    }

    [Fact]
    public void PlaceholderPainter_RepaintsOnlyForSourceFields()
    {
        var original = new PlaceholderPainter(Placeholder.DefaultColor, 2);
        var same = new PlaceholderPainter(Placeholder.DefaultColor, 2);
        var changedColor = new PlaceholderPainter(Colors.Orange, 2);
        var changedStroke = new PlaceholderPainter(Placeholder.DefaultColor, 3);

        Assert.False(same.ShouldRepaint(original));
        Assert.True(changedColor.ShouldRepaint(original));
        Assert.True(changedStroke.ShouldRepaint(original));
    }

    [Fact]
    public void GridPaper_UsesFlutterDefaultsAndForegroundPainterComposition()
    {
        var widget = new GridPaper(child: new SizedBox(width: 120, height: 80));

        Assert.Equal(Color.FromArgb(0x7F, 0xC3, 0xE8, 0xF3), widget.Color);
        Assert.Equal(100.0, widget.Interval);
        Assert.Equal(2, widget.Divisions);
        Assert.Equal(5, widget.Subdivisions);

        using var harness = new WidgetRenderHarness(widget);
        harness.Pump(new Size(120, 80));

        var paint = Assert.Single(FindDescendants<RenderCustomPaint>(harness.RenderView));
        Assert.Null(paint.Painter);
        var painter = Assert.IsType<GridPaperPainter>(paint.ForegroundPainter);
        Assert.Equal(new Size(120, 80), paint.Size);
        Assert.Equal(widget.Color, painter.Color);
        Assert.Equal(widget.Interval, painter.Interval);
        Assert.Equal(widget.Divisions, painter.Divisions);
        Assert.Equal(widget.Subdivisions, painter.Subdivisions);
        Assert.False(painter.HitTest(default));
    }

    [Fact]
    public void GridPaper_ValidatesCountsAndUsesSourceStrokeHierarchy()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GridPaper(divisions: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GridPaper(subdivisions: 0));

        var painter = new GridPaperPainter(GridPaper.DefaultColor, interval: 100, divisions: 2, subdivisions: 5);
        Assert.Equal(1.0, painter.StrokeWidthAt(0));
        Assert.Equal(0.25, painter.StrokeWidthAt(10));
        Assert.Equal(0.5, painter.StrokeWidthAt(20));
        Assert.Equal(1.0, painter.StrokeWidthAt(100));
    }

    [Fact]
    public void GridPaperPainter_RepaintsForEverySourceField()
    {
        var original = new GridPaperPainter(Colors.Blue, 100, 2, 5);

        Assert.False(new GridPaperPainter(Colors.Blue, 100, 2, 5).ShouldRepaint(original));
        Assert.True(new GridPaperPainter(Colors.Red, 100, 2, 5).ShouldRepaint(original));
        Assert.True(new GridPaperPainter(Colors.Blue, 80, 2, 5).ShouldRepaint(original));
        Assert.True(new GridPaperPainter(Colors.Blue, 100, 4, 5).ShouldRepaint(original));
        Assert.True(new GridPaperPainter(Colors.Blue, 100, 2, 4).ShouldRepaint(original));
    }

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is T match)
        {
            result.Add(match);
        }

        root?.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
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

            internal override Element? RenderObjectAttachingChild => _child;

            protected override void OnMount()
            {
                base.OnMount();
                Rebuild();
            }

            internal override void Rebuild()
            {
                Dirty = false;
                _child = UpdateChild(_child, Widget, Slot);
            }

            internal override void Update(Widget newWidget)
            {
                base.Update(newWidget);
                Rebuild();
            }

            internal override void ForgetChild(Element child)
            {
                if (ReferenceEquals(_child, child))
                {
                    _child = null;
                }
            }

            internal override void VisitChildren(Action<Element> visitor)
            {
                if (_child is not null)
                {
                    visitor(_child);
                }
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
                _renderView.Child = (RenderBox)child;
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(_renderView.Child, child))
                {
                    _renderView.Child = null;
                }
            }

            internal override void Unmount()
            {
                if (_child is not null)
                {
                    UnmountChild(_child);
                    _child = null;
                }

                base.Unmount();
            }
        }
    }
}
