using Avalonia;
using Plumix;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class DisplayFeatureSubScreenTests : IDisposable
{
    private static readonly Size ScreenSize = new(800, 600);

    private static readonly MediaQueryData VerticalHinge = new(
        Size: ScreenSize,
        Padding: new Thickness(10),
        DisplayFeatures: [new DisplayFeature(new Rect(390, 0, 20, 600), DisplayFeatureType.Hinge)]);

    public DisplayFeatureSubScreenTests() => Scheduler.ResetForTests();

    public void Dispose() => Scheduler.ResetForTests();

    [Fact]
    public void SubScreen_WithoutDisplayFeatures_LeavesTheChildUntouched()
    {
        Rect bounds = MeasureChild(new MediaQueryData(Size: ScreenSize), anchorPoint: null, TextDirection.Ltr);

        Assert.Equal(new Rect(default, ScreenSize), bounds);
    }

    [Fact]
    public void SubScreen_PicksTheHalfClosestToTheAnchorPoint()
    {
        Rect leading = MeasureChild(VerticalHinge, new Point(0, 0), TextDirection.Ltr);
        Assert.Equal(new Rect(0, 0, 390, 600), leading);

        Rect trailing = MeasureChild(VerticalHinge, new Point(1000, 0), TextDirection.Ltr);
        Assert.Equal(new Rect(410, 0, 390, 600), trailing);
    }

    [Fact]
    public void SubScreen_FallsBackToTheDirectionalLeadingEdge()
    {
        Rect ltr = MeasureChild(VerticalHinge, anchorPoint: null, TextDirection.Ltr);
        Assert.Equal(new Rect(0, 0, 390, 600), ltr);

        Rect rtl = MeasureChild(VerticalHinge, anchorPoint: null, TextDirection.Rtl);
        Assert.Equal(new Rect(410, 0, 390, 600), rtl);
    }

    [Fact]
    public void RemoveDisplayFeatures_ShrinksInsetsAndDropsTheFeatures()
    {
        var data = new MediaQueryData(
            Size: ScreenSize,
            Padding: new Thickness(10, 20, 30, 40),
            ViewInsets: new Thickness(10, 20, 30, 40),
            ViewPadding: new Thickness(10, 20, 30, 40),
            DisplayFeatures: [new DisplayFeature(new Rect(390, 0, 20, 600), DisplayFeatureType.Hinge)]);

        MediaQueryData trailing = data.RemoveDisplayFeatures(new Rect(410, 0, 390, 600));

        Assert.Equal(new Size(390, 600), trailing.Size);
        Assert.Empty(trailing.DisplayFeatures!);
        // The left inset is fully consumed by the sub-screen origin; the right one survives untouched.
        Assert.Equal(0, trailing.Padding.Left);
        Assert.Equal(20, trailing.Padding.Top);
        Assert.Equal(30, trailing.Padding.Right);
        Assert.Equal(40, trailing.Padding.Bottom);
        Assert.Equal(0, trailing.ViewInsets.Left);
        Assert.Equal(0, trailing.ViewPadding.Left);
    }

    [Fact]
    public void RemoveDisplayFeatures_RejectsSubScreensOutsideTheScreen()
    {
        var data = new MediaQueryData(Size: ScreenSize);

        Assert.Throws<ArgumentOutOfRangeException>(() => data.RemoveDisplayFeatures(new Rect(0, 0, 900, 600)));
        Assert.Throws<ArgumentOutOfRangeException>(() => data.RemoveDisplayFeatures(new Rect(-10, 0, 100, 600)));
    }

    private static Rect MeasureChild(MediaQueryData media, Point? anchorPoint, TextDirection direction)
    {
        MediaQueryData? childMedia = null;
        using var harness = new Harness(new Directionality(
            direction,
            new MediaQuery(
                media,
                new DisplayFeatureSubScreen(
                    anchorPoint: anchorPoint,
                    child: new Builder(context =>
                    {
                        childMedia = MediaQuery.Of(context);
                        return new ConstrainedBox(BoxConstraints.Expand());
                    })))));
        harness.Pump(media.Size);

        var box = Assert.Single(FindDescendants<RenderConstrainedBox>(harness.RenderView));
        Assert.Equal(childMedia!.Size, box.Size);
        return new Rect(box.LocalToGlobal(default), box.Size);
    }

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null) return result;
        if (root is T target) result.Add(target);
        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private sealed class Harness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _root;
        private readonly PipelineOwner _pipeline;

        public Harness(Widget widget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _root = new HarnessRootElement(RenderView, widget);
            _root.Attach(_owner);
            _root.Mount(null, null);
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

        public void Dispose() => _root.Unmount();

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _view;
            private Element? _child;
            public HarnessRootElement(RenderView view, Widget widget) : base(widget) => _view = view;
            public override RenderObject? RenderObject => _child?.RenderObject;
            public override Element? RenderObjectAttachingChild => _child;
            protected override void OnMount() { base.OnMount(); Rebuild(); }
            public override void Rebuild() { Dirty = false; _child = UpdateChild(_child, Widget, Slot); }
            public override void Update(Widget newWidget) { base.Update(newWidget); Rebuild(); }
            public override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
            public override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
            public void InsertRenderObjectChild(RenderObject child, object? slot) => _view.Child = (RenderBox)child;
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(_view.Child, child)) _view.Child = null;
            }

            public override void Unmount()
            {
                if (_child is not null) { UnmountChild(_child); _child = null; }
                base.Unmount();
            }
        }
    }
}
