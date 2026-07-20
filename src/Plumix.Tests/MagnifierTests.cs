using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using MaterialMagnifier = Plumix.Material.Magnifier;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MagnifierTests : IDisposable
{
    public MagnifierTests()
    {
        Scheduler.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
    }

    [Fact]
    public void RawMagnifier_UsesFlutterDefaultsAndValidatesContracts()
    {
        var widget = new RawMagnifier(size: new Size(80, 40));

        Assert.Equal(Clip.None, widget.ClipBehavior);
        Assert.Equal(default, widget.FocalPointOffset);
        Assert.Equal(1, widget.MagnificationScale);
        Assert.Equal(1, widget.Decoration.Opacity);
        Assert.Null(widget.Decoration.Shadows);
        Assert.Equal(0, widget.Decoration.Shape.BorderRadius.Radius);
        Assert.Throws<ArgumentOutOfRangeException>(() => new RawMagnifier(new Size(-1, 10)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RawMagnifier(new Size(10, 10), magnificationScale: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MagnifierDecoration(opacity: 1.1));
    }

    [Fact]
    public void RawMagnifier_WiresLayoutPaintPropertiesAndCompositedLensLayer()
    {
        var decoration = new MagnifierDecoration(
            opacity: 0.75,
            shape: ShapeBorder.RoundedRectangle(9, new BorderSide(Colors.Blue, 2)),
            shadows: new BoxShadows(new BoxShadow { Blur = 2, Color = Colors.Black }));
        using var harness = new WidgetRenderHarness(new Align(
            alignment: Alignment.TopLeft,
            child: new RawMagnifier(
                size: new Size(80, 40),
                focalPointOffset: new Point(3, 12),
                magnificationScale: 1.5,
                decoration: decoration,
                clipBehavior: Clip.HardEdge,
                child: new ColoredBox(Colors.Gray))));

        harness.Pump(new Size(240, 160));

        var render = FindDescendant<RenderMagnifier>(harness.RenderView);
        Assert.NotNull(render);
        Assert.Equal(new Size(80, 40), render!.Size);
        Assert.Equal(new Point(3, 12), render.FocalPointOffset);
        Assert.Equal(1.5, render.MagnificationScale);
        Assert.Equal(decoration, render.Decoration);
        Assert.Equal(Clip.HardEdge, render.ClipBehavior);

        var layer = FindLayer<MagnifierLayer>(harness.Pipeline.RootLayer);
        Assert.NotNull(layer);
        Assert.Equal(new Size(80, 40), layer!.LensRect.Size);
        Assert.Equal(new Point(3, 12), layer.FocalPointOffset);

    }

    [Fact]
    public void MagnifierController_ShiftWithinBoundsUsesShortestTranslationAndGuardsSize()
    {
        var bounds = new Rect(10, 20, 100, 80);

        Assert.Equal(
            new Rect(10, 20, 30, 20),
            MagnifierController.ShiftWithinBounds(new Rect(0, 5, 30, 20), bounds));
        Assert.Equal(
            new Rect(80, 80, 30, 20),
            MagnifierController.ShiftWithinBounds(new Rect(95, 95, 30, 20), bounds));
        Assert.Equal(
            new Rect(30, 40, 30, 20),
            MagnifierController.ShiftWithinBounds(new Rect(30, 40, 30, 20), bounds));
        Assert.Throws<ArgumentException>(() =>
            MagnifierController.ShiftWithinBounds(new Rect(0, 0, 101, 20), bounds));
    }

    [Fact]
    public async Task MagnifierController_ShowsAndRemovesRootRoute()
    {
        var probe = new ContextProbe();
        using var harness = new WidgetRenderHarness(new Navigator(
            new BuilderPageRoute(_ => probe)));
        harness.Pump(new Size(240, 160));
        var probeState = harness.FindState<ContextProbeState>();
        var controller = new MagnifierController();

        await controller.Show(
            probeState.Context,
            _ => new Positioned(
                left: 20,
                top: 20,
                child: new RawMagnifier(size: new Size(60, 30))));
        harness.Pump(new Size(240, 160));

        Assert.True(controller.Shown);
        Assert.NotNull(controller.OverlayEntry);
        Assert.NotNull(FindDescendant<RenderMagnifier>(harness.RenderView));

        await controller.Hide();
        harness.Pump(new Size(240, 160));

        Assert.False(controller.Shown);
        Assert.Null(controller.OverlayEntry);
        Assert.Null(FindDescendant<RenderMagnifier>(harness.RenderView));
    }

    [Fact]
    public void MaterialMagnifier_BuildsRawMagnifierWithAndroid12Defaults()
    {
        using var harness = new WidgetRenderHarness(new MaterialMagnifier());
        harness.Pump(new Size(200, 120));

        var render = FindDescendant<RenderMagnifier>(harness.RenderView);
        Assert.NotNull(render);
        Assert.Equal(MaterialMagnifier.DefaultMagnifierSize, render!.Size);
        Assert.Equal(1.25, render.MagnificationScale);
        Assert.Equal(
            new Point(
                0,
                MaterialMagnifier.StandardVerticalFocalPointShift
                + (MaterialMagnifier.DefaultMagnifierSize.Height / 2.0)),
            render.FocalPointOffset);
        Assert.Equal(40, render.Decoration.Shape.BorderRadius.Radius);
        Assert.Equal(Clip.HardEdge, render.ClipBehavior);
        Assert.Equal(1, render.Decoration.Shadows!.Value.Count);
    }

    [Fact]
    public void TextMagnifier_ClampsLineScreenAndFieldFocalGeometry()
    {
        var info = new ValueNotifier<MagnifierInfo>(new MagnifierInfo(
            GlobalGesturePosition: new Point(8, 12),
            CaretRect: new Rect(8, 10, 2, 20),
            FieldBounds: new Rect(20, 5, 140, 80),
            CurrentLineBoundaries: new Rect(20, 10, 120, 20)));
        using var harness = new WidgetRenderHarness(
            new MediaQuery(
                new MediaQueryData(Size: new Size(200, 120)),
                new Stack(children: [new TextMagnifier(info)])));

        harness.Pump(new Size(200, 120));
        var state = harness.FindState<TextMagnifier.TextMagnifierState>();

        Assert.Equal(0, state.MagnifierPosition!.Value.X, 3);
        Assert.Equal(0, state.MagnifierPosition.Value.Y, 3);
        Assert.True(state.ExtraFocalPointOffset.X > 0);
        Assert.False(state.PositionShouldBeAnimated);

        info.Value = info.Value with
        {
            GlobalGesturePosition = new Point(100, 60),
            CaretRect = new Rect(100, 58, 2, 20),
            CurrentLineBoundaries = new Rect(20, 58, 120, 20),
        };
        harness.Pump(new Size(200, 120));

        Assert.True(state.MagnifierPosition.Value.Y > 0);
        Assert.True(state.PositionShouldBeAnimated);
    }

    [Fact]
    public void AdaptiveConfigurationRoutesAndroidIOSAndDesktopLikeFlutter()
    {
        var info = new ValueNotifier<MagnifierInfo>(MagnifierInfo.Empty);
        var controller = new MagnifierController();

        Assert.IsType<TextMagnifier>(BuildAdaptive(TargetPlatform.Android, controller, info));
        Assert.IsType<CupertinoTextMagnifier>(BuildAdaptive(TargetPlatform.IOS, controller, info));
        Assert.Null(BuildAdaptive(TargetPlatform.Windows, controller, info));
        Assert.True(TextMagnifier.AdaptiveMagnifierConfiguration.ShouldDisplayHandlesInMagnifier
                    == OperatingSystem.IsIOS());
    }

    [Fact]
    public void CupertinoMagnifier_UsesSourceGeometryAndAnimationValues()
    {
        var animation = new AnimationController(TimeSpan.FromMilliseconds(150));
        animation.SetValue(0.5);
        using var harness = new WidgetRenderHarness(new CupertinoMagnifier(
            inOutAnimation: animation,
            additionalFocalPointOffset: new Point(2, 3)));

        harness.Pump(new Size(200, 120));

        var render = FindDescendant<RenderMagnifier>(harness.RenderView);
        Assert.NotNull(render);
        Assert.Equal(CupertinoMagnifier.DefaultSize, render!.Size);
        Assert.Equal(0.5, render.Decoration.Opacity);
        Assert.Equal(2, render.Decoration.Shape.Side!.Value.Width);
        Assert.Equal(1, render.MagnificationScale);
        Assert.Equal(2, render.FocalPointOffset.X);
        Assert.Equal(
            (((CupertinoMagnifier.DefaultSize.Height / 2.0)
              - CupertinoMagnifier.MagnifierAboveFocalPoint) * 0.5) + 3,
            render.FocalPointOffset.Y,
            3);
        animation.Dispose();
    }

    private static Widget? BuildAdaptive(
        TargetPlatform platform,
        MagnifierController controller,
        ValueNotifier<MagnifierInfo> info)
    {
        Widget? result = null;
        using var harness = new WidgetRenderHarness(new Theme(
            ThemeData.Light with { Platform = platform },
            new Builder(context =>
            {
                result = TextMagnifier.AdaptiveMagnifierConfiguration.MagnifierBuilder(
                    context,
                    controller,
                    info);
                return new SizedBox();
            })));
        harness.Pump(new Size(100, 100));
        return result;
    }

    private static T? FindDescendant<T>(RenderObject? root) where T : RenderObject
    {
        if (root is T target)
        {
            return target;
        }

        T? result = null;
        root?.VisitChildren(child => result ??= FindDescendant<T>(child));
        return result;
    }

    private static T? FindLayer<T>(Layer layer) where T : Layer
    {
        if (layer is T target)
        {
            return target;
        }

        if (layer is not ContainerLayer container)
        {
            return null;
        }

        foreach (Layer child in container.Children)
        {
            T? result = FindLayer<T>(child);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _rootElement;

        public WidgetRenderHarness(Widget rootWidget)
        {
            RenderView = new RenderView();
            Pipeline = new PipelineOwner(RenderView);
            Pipeline.Attach(RenderView);
            _rootElement = new HarnessRootElement(RenderView, rootWidget);
            _rootElement.Attach(_owner);
            _rootElement.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public PipelineOwner Pipeline { get; }

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            Pipeline.RequestLayout();
            Pipeline.FlushLayout(size);
            Pipeline.FlushCompositingBits();
            Pipeline.FlushPaint();
        }

        public T FindState<T>() where T : State
        {
            return FindState<T>(_rootElement)
                   ?? throw new InvalidOperationException($"State {typeof(T).Name} was not found.");
        }

        public void Dispose() => _rootElement.Unmount();

        private static T? FindState<T>(Element element) where T : State
        {
            if (element is StatefulElement stateful && stateful.State is T state)
            {
                return state;
            }

            T? result = null;
            element.VisitChildren(child => result ??= FindState<T>(child));
            return result;
        }

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
            protected override void OnMount() { base.OnMount(); Rebuild(); }
            internal override void Rebuild() { Dirty = false; _child = UpdateChild(_child, Widget, Slot); }
            internal override void Update(Widget newWidget) { base.Update(newWidget); Rebuild(); }
            internal override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
            internal override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
                _renderView.Child = (RenderBox)child;
            }
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null;
            }
            internal override void Unmount()
            {
                if (_child is not null) { UnmountChild(_child); _child = null; }
                base.Unmount();
            }
        }
    }

    private sealed class ContextProbe : StatefulWidget
    {
        public override State CreateState() => new ContextProbeState();
    }

    private sealed class ContextProbeState : State
    {
        public override Widget Build(BuildContext context) => new SizedBox();
    }
}
