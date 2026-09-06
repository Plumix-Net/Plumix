using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: cupertino_ui/test/activity_indicator_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoActivityIndicatorTests : IDisposable
{
    private static readonly Color LightTickColor = Color.FromUInt32(0xFF3C3C44);
    private static readonly Color DarkTickColor = Color.FromUInt32(0xFFEBEBF5);

    public CupertinoActivityIndicatorTests()
    {
        Scheduler.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
    }

    [Fact]
    public void ActivityIndicator_AnimatePropertyWorks()
    {
        using (var harness = new WidgetRenderHarness(BuildCupertinoActivityIndicator()))
        {
            harness.Pump(new Size(800, 600));
            Assert.Equal(1, Scheduler.TransientCallbackCount);

            harness.Update(BuildCupertinoActivityIndicator(animating: false));
            harness.Pump(new Size(800, 600));
            Assert.Equal(0, Scheduler.TransientCallbackCount);
        }

        using (var harness = new WidgetRenderHarness(BuildCupertinoActivityIndicator(animating: false)))
        {
            harness.Pump(new Size(800, 600));
            Assert.Equal(0, Scheduler.TransientCallbackCount);

            harness.Update(BuildCupertinoActivityIndicator());
            harness.Pump(new Size(800, 600));
            Assert.Equal(1, Scheduler.TransientCallbackCount);
        }
    }

    [Fact]
    public void ActivityIndicator_AnimationDrivesPainterPositionWithoutRebuild()
    {
        using var harness = new WidgetRenderHarness(BuildCupertinoActivityIndicator());
        harness.Pump(new Size(800, 600));

        var painter = FindPainter<CupertinoActivityIndicatorPainter>(harness.RenderView);
        Assert.NotNull(painter);
        Assert.Equal(0.0, painter!.Position.Value, 3);

        AnimationPump.Advance(0.25);
        Assert.Equal(0.25, painter.Position.Value, 3);
    }

    [Fact]
    public void ActivityIndicator_DefaultTickColor_ResolvesPlatformBrightness()
    {
        using (var light = new WidgetRenderHarness(
            new MediaQuery(
                data: new MediaQueryData(),
                child: new CupertinoActivityIndicator(animating: false, radius: 35))))
        {
            light.Pump(new Size(800, 600));
            var painter = FindPainter<CupertinoActivityIndicatorPainter>(light.RenderView);
            Assert.Equal(LightTickColor, painter!.ActiveColor);
        }

        using (var dark = new WidgetRenderHarness(
            new MediaQuery(
                data: new MediaQueryData(PlatformBrightness: PlatformBrightness.Dark),
                child: new CupertinoActivityIndicator(animating: false, radius: 35))))
        {
            dark.Pump(new Size(800, 600));
            var painter = FindPainter<CupertinoActivityIndicatorPainter>(dark.RenderView);
            Assert.Equal(DarkTickColor, painter!.ActiveColor);
        }
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void ActivityIndicator_PartiallyRevealed_ShowsProgressWithoutAnimating(double progress)
    {
        var widget = CupertinoActivityIndicator.PartiallyRevealed(progress: progress);
        Assert.False(widget.Animating);
        Assert.Equal(progress, widget.Progress, 3);

        using var harness = new WidgetRenderHarness(new Center(child: widget));
        harness.Pump(new Size(800, 600));

        Assert.Equal(0, Scheduler.TransientCallbackCount);
        var painter = FindPainter<CupertinoActivityIndicatorPainter>(harness.RenderView);
        Assert.Equal(progress, painter!.Progress, 3);
    }

    // Regression parity for https://github.com/flutter/flutter/issues/41345: the first tick sits at
    // 12 o'clock and uses the fundamental rounded-rect shape below.
    [Fact]
    public void ActivityIndicator_HasTheCorrectCornerRadius()
    {
        using var harness = new WidgetRenderHarness(
            new CupertinoActivityIndicator(animating: false, radius: 100));
        harness.Pump(new Size(800, 600));

        var painter = FindPainter<CupertinoActivityIndicatorPainter>(harness.RenderView);
        Assert.Equal(
            RRect.FromLTRBXY(-10, -100.0 / 3.0, 10, -100, 10, 10),
            painter!.TickFundamentalShape);
    }

    [Fact]
    public void ActivityIndicator_CanSpecifyColor()
    {
        var color = Color.FromUInt32(0xFF5D3FD3);
        using var harness = new WidgetRenderHarness(
            new Center(
                child: new CupertinoActivityIndicator(animating: false, color: color, radius: 100)));
        harness.Pump(new Size(800, 600));

        var painter = FindPainter<CupertinoActivityIndicatorPainter>(harness.RenderView);
        Assert.Equal(color, painter!.ActiveColor);

        // The value of 47 is the alpha applied to the first tick when fully revealed.
        Assert.Equal(new byte[] { 47, 47, 47, 47, 72, 97, 122, 147 }, CupertinoActivityIndicatorPainter.AlphaValues);
        Assert.Equal(147, CupertinoActivityIndicatorPainter.PartiallyRevealedAlpha);
    }

    [Fact]
    public void ActivityIndicator_LayoutIsSquareOfTwiceRadius()
    {
        using var harness = new WidgetRenderHarness(new Center(child: new CupertinoActivityIndicator()));
        harness.Pump(new Size(800, 600));
        var renderPaint = FindRenderCustomPaint(harness.RenderView, painter => painter
            is CupertinoActivityIndicatorPainter);
        Assert.Equal(new Size(20, 20), renderPaint!.Size);

        using var large = new WidgetRenderHarness(
            new Center(child: new CupertinoActivityIndicator(animating: false, radius: 35)));
        large.Pump(new Size(800, 600));
        renderPaint = FindRenderCustomPaint(large.RenderView, painter => painter
            is CupertinoActivityIndicatorPainter);
        Assert.Equal(new Size(70, 70), renderPaint!.Size);
    }

    [Fact]
    public void ActivityIndicator_DoesNotCrashAtZeroArea()
    {
        using var harness = new WidgetRenderHarness(
            new Center(
                child: new SizedBox(
                    width: 0,
                    height: 0,
                    child: new CupertinoActivityIndicator())));
        harness.Pump(new Size(800, 600));

        var renderPaint = FindRenderCustomPaint(harness.RenderView, painter => painter
            is CupertinoActivityIndicatorPainter);
        Assert.Equal(default, renderPaint!.Size);
    }

    [Fact]
    public void ActivityIndicatorPainter_PaintsBothProgressBranchesAndReportsRepaint()
    {
        using var controller = new AnimationController(duration: TimeSpan.FromSeconds(1));
        var full = new CupertinoActivityIndicatorPainter(controller, LightTickColor, 10, 1.0);
        full.Paint(new PaintingContext(new ContainerLayer()), new Size(20, 20));
        var partial = new CupertinoActivityIndicatorPainter(controller, LightTickColor, 10, 0.5);
        partial.Paint(new PaintingContext(new ContainerLayer()), new Size(20, 20));

        Assert.False(full.ShouldRepaint(new CupertinoActivityIndicatorPainter(controller, LightTickColor, 10, 1.0)));
        Assert.False(full.ShouldRepaint(new CupertinoActivityIndicatorPainter(controller, LightTickColor, 99, 1.0)));
        Assert.True(full.ShouldRepaint(partial));
        Assert.True(full.ShouldRepaint(new CupertinoActivityIndicatorPainter(controller, DarkTickColor, 10, 1.0)));
        using var otherPosition = new AnimationController(duration: TimeSpan.FromSeconds(1));
        Assert.True(full.ShouldRepaint(new CupertinoActivityIndicatorPainter(otherPosition, LightTickColor, 10, 1.0)));
    }

    [Fact]
    public void ActivityIndicator_ConstructorMatchesFlutterAssertions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoActivityIndicator(radius: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoActivityIndicator(radius: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoActivityIndicator(radius: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CupertinoActivityIndicator.PartiallyRevealed(progress: -0.1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CupertinoActivityIndicator.PartiallyRevealed(progress: 1.1));

        var widget = new CupertinoActivityIndicator();
        Assert.Null(widget.Color);
        Assert.True(widget.Animating);
        Assert.Equal(10.0, widget.Radius, 3);
        Assert.Equal(1.0, widget.Progress, 3);
    }

    [Fact]
    public void LinearActivityIndicator_DrawsBackgroundAndProgress()
    {
        using var harness = new WidgetRenderHarness(
            new Center(child: new CupertinoLinearActivityIndicator(progress: 0.2)));
        harness.Pump(new Size(800, 600));

        var renderPaint = FindRenderCustomPaint(harness.RenderView, painter => painter
            is CupertinoLinearActivityIndicatorPainter);
        Assert.Equal(new Size(800, 4.5), renderPaint!.Size);

        var painter = (CupertinoLinearActivityIndicatorPainter)renderPaint.Painter!;
        Assert.Equal(0.2, painter.Progress, 3);
        Assert.Null(painter.Color);
        Assert.Equal(CupertinoColors.SystemFill.Value, painter.BackgroundPaint.Color);
        Assert.Equal(CupertinoColors.ActiveBlue.Value, painter.ProgressPaint.Color);
        painter.Paint(new PaintingContext(new ContainerLayer()), renderPaint.Size);
    }

    [Fact]
    public void LinearActivityIndicator_DrawsWithCustomHeightAndColor()
    {
        using var harness = new WidgetRenderHarness(
            new Center(
                child: new CupertinoLinearActivityIndicator(
                    progress: 0.5,
                    height: 10,
                    color: CupertinoColors.ActiveGreen.Value)));
        harness.Pump(new Size(800, 600));

        var renderPaint = FindRenderCustomPaint(harness.RenderView, painter => painter
            is CupertinoLinearActivityIndicatorPainter);
        Assert.Equal(new Size(800, 10), renderPaint!.Size);

        var painter = (CupertinoLinearActivityIndicatorPainter)renderPaint.Painter!;
        Assert.Equal(0.5, painter.Progress, 3);
        Assert.Equal(CupertinoColors.ActiveGreen.Value, painter.ProgressPaint.Color);
        Assert.Equal(CupertinoColors.SystemFill.Value, painter.BackgroundPaint.Color);
    }

    [Fact]
    public void LinearActivityIndicator_DoesNotCrashAtZeroArea()
    {
        using var harness = new WidgetRenderHarness(
            new Center(
                child: new SizedBox(
                    width: 0,
                    height: 0,
                    child: new CupertinoLinearActivityIndicator(progress: 0.5))));
        harness.Pump(new Size(800, 600));

        var renderPaint = FindRenderCustomPaint(harness.RenderView, painter => painter
            is CupertinoLinearActivityIndicatorPainter);
        Assert.Equal(default, renderPaint!.Size);
    }

    [Fact]
    public void LinearActivityIndicator_ConstructorMatchesFlutterAssertions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoLinearActivityIndicator(0.5, height: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoLinearActivityIndicator(0.5, height: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoLinearActivityIndicator(-0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoLinearActivityIndicator(1.1));

        var widget = new CupertinoLinearActivityIndicator(0.3);
        Assert.Equal(0.3, widget.Progress, 3);
        Assert.Equal(4.5, widget.Height, 3);
        Assert.Null(widget.Color);

        var painter = new CupertinoLinearActivityIndicatorPainter(progress: 0.3);
        Assert.False(painter.ShouldRepaint(new CupertinoLinearActivityIndicatorPainter(progress: 0.3)));
        Assert.True(painter.ShouldRepaint(new CupertinoLinearActivityIndicatorPainter(progress: 0.7)));
        Assert.True(painter.ShouldRepaint(
            new CupertinoLinearActivityIndicatorPainter(progress: 0.3, color: Colors.Red)));
    }

    private static Widget BuildCupertinoActivityIndicator(bool animating = true)
    {
        return new MediaQuery(
            data: new MediaQueryData(),
            child: new CupertinoActivityIndicator(animating: animating));
    }

    private static T? FindPainter<T>(RenderObject? root) where T : CustomPainter
    {
        return FindRenderCustomPaint(root, painter => painter is T)?.Painter as T;
    }

    private static RenderCustomPaint? FindRenderCustomPaint(
        RenderObject? root,
        Func<CustomPainter?, bool> predicate)
    {
        if (root is null)
        {
            return null;
        }

        if (root is RenderCustomPaint customPaint && predicate(customPaint.Painter))
        {
            return customPaint;
        }

        RenderCustomPaint? match = null;
        root.VisitChildren(child =>
        {
            if (match is not null)
            {
                return;
            }

            match = FindRenderCustomPaint(child, predicate);
        });

        return match;
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

        public void Update(Widget newWidget)
        {
            _rootElement.Update(newWidget);
            _owner.FlushBuild();
        }

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public void Dispose()
        {
            _rootElement.Unmount();
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

            public override Element? RenderObjectAttachingChild => _child;

            protected override void OnMount()
            {
                base.OnMount();
                Rebuild();
            }

            protected override void PerformRebuild()
            {
                base.PerformRebuild();
                _child = UpdateChild(_child, Widget, Slot);
            }

            public override void Update(Widget newWidget)
            {
                base.Update(newWidget);
                Rebuild(force: true);
            }

            public override void ForgetChild(Element child)
            {
                if (ReferenceEquals(_child, child))
                {
                    _child = null;
                }
            }

            public override void VisitChildren(Action<Element> visitor)
            {
                if (_child is not null)
                {
                    visitor(_child);
                }
            }

            public override void Unmount()
            {
                if (_child is not null)
                {
                    UnmountChild(_child);
                    _child = null;
                }

                base.Unmount();
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
                if (child is RenderBox renderBox && ReferenceEquals(_renderView.Child, renderBox))
                {
                    _renderView.Child = null;
                }
            }
        }
    }
}
