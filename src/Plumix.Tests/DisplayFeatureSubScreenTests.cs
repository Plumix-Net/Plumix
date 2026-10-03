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

    // Flutter: 'media_query_test.dart: MediaQuery.removeDisplayFeatures removes specified display
    // features and padding'
    [Fact]
    public void RemoveDisplayFeaturesRemovesSpecifiedDisplayFeaturesAndPadding()
    {
        MediaQueryData subScreenMediaQuery = SubScreenData(
            viewPadding: new Thickness(10.0, 6.0, 8.0, 12.0),
            subScreen: new Rect(new Point(20, 10), new Point(40, 20)));

        Assert.Equal(new Size(82.0, 40.0), subScreenMediaQuery.Size);
        Assert.Equal(2.0, subScreenMediaQuery.DevicePixelRatio);
        Assert.Equal(new Thickness(0), subScreenMediaQuery.Padding);
        Assert.Equal(new Thickness(0), subScreenMediaQuery.ViewPadding);
        Assert.Equal(new Thickness(0), subScreenMediaQuery.ViewInsets);
        Assert.True(subScreenMediaQuery.AlwaysUse24HourFormat);
        Assert.True(subScreenMediaQuery.DisableAnimations);
        Assert.Empty(subScreenMediaQuery.DisplayFeatures!);
    }

    // Flutter: 'media_query_test.dart: MediaQuery.removePadding only removes specified display
    // features and padding'
    [Fact]
    public void RemoveDisplayFeaturesOnlyRemovesSpecifiedDisplayFeaturesAndPadding()
    {
        MediaQueryData subScreenMediaQuery = SubScreenData(
            viewPadding: new Thickness(46.0, 6.0, 8.0, 12.0),
            subScreen: new Rect(new Point(42, 0), new Point(82, 40)));

        Assert.Equal(new Size(82.0, 40.0), subScreenMediaQuery.Size);
        Assert.Equal(2.0, subScreenMediaQuery.DevicePixelRatio);
        Assert.Equal(new Thickness(0, 1.0, 2.0, 4.0), subScreenMediaQuery.Padding);
        Assert.Equal(new Thickness(4.0, 6.0, 8.0, 12.0), subScreenMediaQuery.ViewPadding);
        Assert.Equal(new Thickness(0, 5.0, 6.0, 8.0), subScreenMediaQuery.ViewInsets);
        Assert.True(subScreenMediaQuery.AlwaysUse24HourFormat);
        Assert.True(subScreenMediaQuery.DisableAnimations);
        Assert.Equal([CutoutDisplayFeature], subScreenMediaQuery.DisplayFeatures!);
    }

    private static readonly DisplayFeature CutoutDisplayFeature = new(
        Bounds: new Rect(new Point(70, 10), new Point(74, 14)),
        Type: DisplayFeatureType.Cutout,
        State: DisplayFeatureState.Unknown);

    private static MediaQueryData SubScreenData(Thickness viewPadding, Rect subScreen)
    {
        var data = new MediaQueryData(
            Size: new Size(82.0, 40.0),
            DevicePixelRatio: 2.0,
            Padding: new Thickness(3.0, 1.0, 2.0, 4.0),
            ViewPadding: viewPadding,
            ViewInsets: new Thickness(7.0, 5.0, 6.0, 8.0),
            AlwaysUse24HourFormat: true,
            DisableAnimations: true,
            DisplayFeatures:
            [
                new DisplayFeature(
                    Bounds: new Rect(new Point(40, 0), new Point(42, 40)),
                    Type: DisplayFeatureType.Hinge,
                    State: DisplayFeatureState.PostureFlat),
                CutoutDisplayFeature,
            ]);
        return data.RemoveDisplayFeatures(subScreen);
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
        // Dart's removeDisplayFeatures keeps the full screen size; only the box shrinks.
        Assert.Equal(media.Size, childMedia!.Size);
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
        private readonly BuildOwner _owner = TestBuildOwner.Create();
        private readonly HarnessRootElement _root;
        private readonly PipelineOwner _pipeline;

        public Harness(Widget widget)
        {
            RenderView = new RenderView(new FlutterView(new Size(800, 600)));
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _root = new HarnessRootElement(RenderView, widget);
            _root.Attach(_owner);
            _root.Owner!.BuildScope(_root, () => _root.Mount(null, null));
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
            _pipeline.CompositeFrame();
        }

        public void Dispose() => _root.UnmountRoot();

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _view;
            private Element? _child;
            public HarnessRootElement(RenderView view, Widget widget) : base(widget) => _view = view;
            public override RenderObject? RenderObject => _child?.RenderObject;
            public override Element? RenderObjectAttachingChild => _child;
            protected override void OnMount() { base.OnMount(); Rebuild(); }
            protected override void PerformRebuild()
            {
                base.PerformRebuild();
                _child = UpdateChild(_child, Widget, Slot);
            }
            public override void Update(Widget newWidget)
            {
                base.Update(newWidget);
                Owner!.BuildScope(this, () => Rebuild(force: true));
            }
            public override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
            public override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
            public void InsertRenderObjectChild(RenderObject child, object? slot) => _view.Child = (RenderBox)child;
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(_view.Child, child)) _view.Child = null;
            }

        }
    }
}
