using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/single_child_scroll_view.dart
// Contract cases: flutter/packages/flutter/test/widgets/single_child_scroll_view_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class SingleChildScrollViewTests
{
    [Fact]
    public void DefaultsAndEmptyChildMatchDart()
    {
        var widget = new SingleChildScrollView();
        Assert.Null(widget.Child);
        Assert.Null(widget.Primary);
        Assert.Null(widget.Padding);
        Assert.Null(widget.Controller);
        Assert.Null(widget.Physics);
        Assert.Null(widget.KeyboardDismissBehavior);
        Assert.Null(widget.RestorationId);
        Assert.False(widget.Reverse);
        Assert.Equal(Axis.Vertical, widget.ScrollDirection);
        Assert.Equal(Clip.HardEdge, widget.ClipBehavior);
        Assert.Equal(HitTestBehavior.Opaque, widget.HitTestBehavior);
        Assert.Equal(DragStartBehavior.Start, widget.DragStartBehavior);
        using var harness = new FocusLayoutHarness(widget);
        harness.Layout(new Size(100, 100));
        Assert.Equal(new Size(), FindViewport(harness.RenderView).Size);
    }

    [Theory]
    [InlineData(AxisDirection.Down)]
    [InlineData(AxisDirection.Up)]
    [InlineData(AxisDirection.Right)]
    [InlineData(AxisDirection.Left)]
    public void LayoutDryLayoutIntrinsicsAndCorrectionMatch(AxisDirection direction)
    {
        var child = new ContentBox(new Size(300, 400));
        var offset = new MutableOffset(1000);
        var viewport = new RenderSingleChildViewport(direction, offset, child);
        var constraints = new BoxConstraints(MaxWidth: 100, MaxHeight: 80);
        Assert.Equal(new Size(100, 80), viewport.GetDryLayout(constraints));
        viewport.Layout(constraints);
        bool horizontal = viewport.Axis == Axis.Horizontal;
        Assert.Equal(horizontal ? new Size(300, 80) : new Size(100, 400), child.Size);
        Assert.Equal(horizontal ? 200 : 320, offset.Pixels);
        Assert.Equal(new[] { "correct", "viewport", "content" }, offset.Calls);
        Assert.Equal(300, viewport.GetMinIntrinsicWidth(0));
        Assert.Equal(300, viewport.GetMaxIntrinsicWidth(0));
        Assert.Equal(400, viewport.GetMinIntrinsicHeight(0));
        Assert.Equal(400, viewport.GetMaxIntrinsicHeight(0));
        Assert.Null(viewport.GetDryBaseline(constraints, TextBaseline.Alphabetic));
        Assert.Null(viewport.GetDistanceToBaseline(TextBaseline.Alphabetic, onlyReal: true));
        Assert.IsType<ParentData>(child.parentData);
        Assert.True(viewport.IsRepaintBoundary);

        offset.SetPixels(-40);
        viewport.MarkNeedsLayout();
        viewport.Layout(constraints);
        Assert.Equal(0, offset.Pixels);
        viewport.Child = null;
        offset.SetPixels(50);
        viewport.Layout(constraints);
        Assert.Equal(new Size(), viewport.Size);
        Assert.Equal(0, offset.Pixels);
        Assert.Equal(new Size(), viewport.GetDryLayout(constraints));
    }

    [Theory]
    [InlineData(AxisDirection.Down, 100, 100, false)]
    [InlineData(AxisDirection.Down, 100, 100.1, true)]
    [InlineData(AxisDirection.Right, 100, 100, false)]
    [InlineData(AxisDirection.Right, 100.1, 100, true)]
    public void PaintClipsOnlyOverflow(AxisDirection direction, double width, double height, bool clips)
    {
        var child = new ContentBox(new Size(width, height));
        var viewport = new RenderSingleChildViewport(direction, ViewportOffset.Zero(), child);
        viewport.Layout(BoxConstraints.Tight(new Size(100, 100)));
        viewport.UpdateCompositingBits();
        foreach (Clip clip in Enum.GetValues<Clip>())
        {
            viewport.ClipBehavior = clip;
            var layer = new OffsetLayer();
            var context = new PaintingContext(layer);
            viewport.Paint(context, default);
            var clipLayer = layer.Children.OfType<ClipRectLayer>().SingleOrDefault();
            bool expectedClip = clips && clip != Clip.None;
            Assert.Equal(expectedClip, clipLayer != null);
            Assert.Equal(expectedClip, viewport.InvokeDescribeApproximatePaintClip(child) != null);
            if (clipLayer != null) Assert.Equal(clip, clipLayer.ClipBehavior);
            Assert.Equal(default, child.LastPaintOffset);
        }
    }

    [Fact]
    public void ScrollInvalidatesPaintAndSemanticsWithoutLayoutAndReassignsListener()
    {
        var first = new MutableOffset(0);
        var second = new MutableOffset(0);
        var child = new ContentBox(new Size(100, 1000));
        var viewport = new RenderSingleChildViewport(AxisDirection.Down, first, child);
        var root = new RenderView(new FlutterView(new Size(100, 100))) { Child = viewport };
        var pipeline = new PipelineOwner(root);
        pipeline.Attach(root);
        pipeline.FlushLayout(new Size(100, 100));
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();
        pipeline.FlushSemantics();
        first.SetPixels(25);
        Assert.False(viewport.NeedsLayout);
        Assert.True(viewport.NeedsPaint);
        Assert.Equal(!Constants.KReleaseMode, viewport.DebugNeedsSemanticsUpdate);
        var transform = Matrix4.Identity();
        viewport.ApplyPaintTransform(child, transform);
        Assert.Equal(new Point(0, -25), MatrixUtils.TransformPoint(transform, default));
        viewport.Offset = second;
        Assert.False(first.IsObserved);
        Assert.True(second.IsObserved);
        root.Child = null;
        Assert.False(second.IsObserved);
    }

    [Fact]
    public void WidgetUpdatesClipControllerAxisAndDirectionalPadding()
    {
        using var first = new ScrollController(initialScrollOffset: 20);
        using var second = new ScrollController(initialScrollOffset: 40);
        Widget Build(ScrollController controller, Clip clip, bool reverse) => new Directionality(
            TextDirection.Rtl,
            new SingleChildScrollView(
                controller: controller,
                scrollDirection: Axis.Horizontal,
                reverse: reverse,
                clipBehavior: clip,
                padding: EdgeInsetsGeometry.DirectionalOnly(start: 10, end: 30),
                child: new SizedBox(width: 500, height: 50)));
        using var harness = new FocusLayoutHarness(Build(first, Clip.HardEdge, false));
        harness.Layout(new Size(100, 100));
        RenderSingleChildViewport viewport = FindViewport(harness.RenderView);
        Assert.Equal(AxisDirection.Left, viewport.AxisDirection);
        Assert.Equal(20, viewport.OffsetPixels);
        Assert.Equal(540, viewport.Child!.Size.Width);
        var padding = Assert.IsType<RenderPadding>(viewport.Child);
        Assert.Equal(new Thickness(30, 0, 10, 0), padding.Padding.Resolve(TextDirection.Rtl));
        harness.Update(Build(second, Clip.AntiAlias, true), new Size(100, 100));
        Assert.Same(viewport, FindViewport(harness.RenderView));
        Assert.Equal(AxisDirection.Right, viewport.AxisDirection);
        Assert.Equal(Clip.AntiAlias, viewport.ClipBehavior);
        Assert.False(first.HasClients);
        Assert.True(second.HasClients);
        Assert.Same(second.Position, viewport.Offset);
    }

    [Theory]
    [InlineData(TargetPlatform.Android, true)]
    [InlineData(TargetPlatform.IOS, true)]
    [InlineData(TargetPlatform.Fuchsia, true)]
    [InlineData(TargetPlatform.Linux, false)]
    [InlineData(TargetPlatform.MacOS, false)]
    [InlineData(TargetPlatform.Windows, false)]
    public void PrimaryInheritanceMatchesPlatformAndShieldsNestedView(TargetPlatform platform, bool inherits)
    {
        using var primary = new ScrollController();
        ScrollController? nestedPrimary = primary;
        using var harness = new FocusLayoutHarness(new ScrollConfiguration(
            new PlatformBehavior(platform),
            new PrimaryScrollController(primary,
                new SingleChildScrollView(child: new Builder(context =>
                {
                    nestedPrimary = PrimaryScrollController.MaybeOf(context);
                    return new SizedBox(height: 500);
                })))));
        harness.Layout(new Size(100, 100));
        Assert.Equal(inherits, primary.HasClients);
        Assert.Equal(inherits, nestedPrimary == null);
    }

    [Fact]
    public void NestedViewportIncrementsNotificationDepth()
    {
        var depths = new List<int>();
        BuildContext? innerContext = null;
        using var harness = new FocusLayoutHarness(new NotificationListener<ScrollUpdateNotification>(
            onNotification: notification => { depths.Add(notification.Depth); return false; },
            child: new SingleChildScrollView(child: new SizedBox(height: 200,
                child: new SingleChildScrollView(child: new Builder(context =>
                {
                    innerContext = context;
                    return new SizedBox(height: 500);
                }))))));
        harness.Layout(new Size(100, 100));
        depths.Clear();
        var metrics = new FixedScrollMetrics(0, 400, 0, 100, AxisDirection.Down, 1);
        new ScrollUpdateNotification(metrics, sourceContext: innerContext, scrollDelta: 1).Dispatch(innerContext);
        Assert.Equal(new[] { 2 }, depths);
    }

    [Theory]
    [InlineData(AxisDirection.Up)]
    [InlineData(AxisDirection.Left)]
    public void ReversedRevealAndSemanticsClipCoverEntireContent(AxisDirection direction)
    {
        bool horizontal = direction == AxisDirection.Left;
        var child = new ContentBox(horizontal ? new Size(2000, 300) : new Size(300, 2000));
        var viewport = new RenderSingleChildViewport(direction, ViewportOffset.Fixed(300), child);
        viewport.Layout(BoxConstraints.Tight(horizontal ? new Size(200, 300) : new Size(300, 200)));
        var pipeline = new PipelineOwner();
        pipeline.Attach(viewport);
        var targetRect = horizontal ? new Rect(1400, 0, 100, 300) : new Rect(0, 1400, 300, 100);
        RevealedOffset leading = viewport.GetOffsetToReveal(child, 0, targetRect, axis: Axis.Horizontal);
        RevealedOffset trailing = viewport.GetOffsetToReveal(child, 1, targetRect);
        Assert.Equal(500, leading.Offset);
        Assert.Equal(400, trailing.Offset);
        Assert.Equal(horizontal ? new Rect(100, 0, 100, 300) : new Rect(0, 100, 300, 100), leading.Rect);
        Assert.Equal(horizontal ? new Rect(0, 0, 100, 300) : new Rect(0, 0, 300, 100), trailing.Rect);
        var subrect = horizontal ? new Rect(1440, 40, 10, 10) : new Rect(40, 1440, 10, 10);
        Assert.Equal(550, viewport.GetOffsetToReveal(child, 0, subrect).Offset);
        Assert.Equal(360, viewport.GetOffsetToReveal(child, 1, subrect).Offset);
        Rect semanticsClip = viewport.InvokeDescribeSemanticsClip(child)!.Value;
        Assert.Equal(child.Size, semanticsClip.Size);
    }

    [Fact]
    public void ZeroAreaStillPaintsWithoutCrashing()
    {
        var child = new ContentBox(new Size(100, 100));
        var viewport = new RenderSingleChildViewport(AxisDirection.Down, ViewportOffset.Zero(), child);
        viewport.Layout(BoxConstraints.Tight(default));
        viewport.UpdateCompositingBits();
        viewport.Paint(new PaintingContext(new OffsetLayer()), default);
        Assert.Equal(new Size(), viewport.Size);
    }

    [Theory]
    [InlineData(Axis.Vertical)]
    [InlineData(Axis.Horizontal)]
    public void SemanticsRetainsHiddenContentAtEveryScrollPosition(Axis axis)
    {
        using var controller = new ScrollController();
        Widget[] children = Enumerable.Range(0, 30).Select(index => (Widget)new Semantics(
            label: $"tile {index}",
            child: new SizedBox(width: 200, height: 200))).ToArray();
        Widget content = axis == Axis.Vertical ? new Column(children: children) : new Row(children: children);
        var harness = new ScrollSemanticsHarness(new Directionality(TextDirection.Ltr,
            new SingleChildScrollView(controller: controller, scrollDirection: axis, child: content)));
        var size = new Size(600, 600);
        foreach (double position in new[] { 0.0, 3000.0, 5400.0 })
        {
            harness.Pump(size);
            controller.JumpTo(position);
            harness.Pump(size);
            for (int index = 0; index < 30; index++)
            {
                SemanticsNode node = Assert.IsType<SemanticsNode>(harness.FindSemanticsNode($"tile {index}"));
                bool hidden = index * 200 < position || index * 200 >= position + 600;
                Assert.Equal(hidden, node.IsHidden);
            }

            RenderSingleChildViewport viewport = FindViewport(harness.RenderView);
            Rect clip = viewport.InvokeDescribeSemanticsClip(viewport.Child)!.Value;
            Assert.Equal(6000, axis == Axis.Vertical ? clip.Height : clip.Width);
        }

        harness.RootElement.UnmountRoot();
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 100)]
    [InlineData(0, 0, 5, 400, 200)]
    [InlineData(200, 200, 2, 200, 200)]
    [InlineData(200, 200, 5, 400, 200)]
    [InlineData(0, 100, 1, 0, 200)]
    public void SameAxisNestedShowOnScreenMatchesFlutter(
        double initialInner, double initialOuter, int targetIndex, double expectedInner, double expectedOuter)
    {
        using var inner = new ScrollController(initialScrollOffset: initialInner);
        using var outer = new ScrollController(initialScrollOffset: initialOuter);
        var key = new GlobalObjectKey<State>(new object());
        Widget[] children = Enumerable.Range(0, 10).Select(index => (Widget)new SizedBox(
            height: 100, key: index == targetIndex ? key : null)).ToArray();
        using var harness = new FocusLayoutHarness(new SingleChildScrollView(
            controller: outer,
            child: new Column(children:
            [
                new SizedBox(height: 200),
                new SizedBox(height: 200, child: new SingleChildScrollView(
                    controller: inner, child: new Column(children: children))),
                new SizedBox(height: 200),
            ])));
        harness.Layout(new Size(200, 200));
        key.CurrentContext!.FindRenderObject()!.ShowOnScreen();
        harness.Layout(new Size(200, 200));
        Assert.Equal(expectedInner, inner.Offset);
        Assert.Equal(expectedOuter, outer.Offset);
    }

    [Theory]
    [InlineData(4, 4, 400, 400)]
    [InlineData(3, 4, 400, 300)]
    [InlineData(6, 4, 400, 500)]
    [InlineData(4, 3, 300, 400)]
    [InlineData(4, 6, 500, 400)]
    [InlineData(3, 3, 300, 300)]
    [InlineData(6, 3, 300, 500)]
    [InlineData(3, 6, 500, 300)]
    [InlineData(6, 6, 500, 500)]
    public void CrossAxisNestedRevealMatchesFlutter(int row, int column, double expectedX, double expectedY)
    {
        using var horizontal = new ScrollController(initialScrollOffset: 400);
        using var vertical = new ScrollController(initialScrollOffset: 400);
        var key = new GlobalObjectKey<State>(new object());
        Widget[] rows = Enumerable.Range(0, 10).Select(y => (Widget)new Row(
            children: Enumerable.Range(0, 10).Select(x => (Widget)new SizedBox(
                width: 100, height: 100, key: y == row && x == column ? key : null)).ToArray())).ToArray();
        using var harness = new FocusLayoutHarness(new Directionality(TextDirection.Ltr,
            new SingleChildScrollView(controller: vertical,
                child: new SingleChildScrollView(controller: horizontal, scrollDirection: Axis.Horizontal,
                    child: new Column(children: rows)))));
        harness.Layout(new Size(200, 200));
        key.CurrentContext!.FindRenderObject()!.ShowOnScreen();
        harness.Layout(new Size(200, 200));
        Assert.Equal(expectedX, horizontal.Offset);
        Assert.Equal(expectedY, vertical.Offset);
    }

    [Fact]
    public void ControllerCanChangeWhileLayoutBuilderIsDirty()
    {
        using var controller = new ScrollController();
        Widget Build(bool explicitController) => new LayoutBuilder((context, constraints) =>
            new SingleChildScrollView(controller: explicitController ? controller : null,
                child: new SizedBox(height: 2000)));
        using var harness = new FocusLayoutHarness(Build(false));
        harness.Layout(new Size(750, 600));
        harness.Update(Build(true), new Size(700, 600));
        Assert.True(controller.HasClients);
        Assert.Same(controller.Position, FindViewport(harness.RenderView).Offset);
    }

    private static RenderSingleChildViewport FindViewport(RenderObject root)
    {
        RenderSingleChildViewport? found = root as RenderSingleChildViewport;
        root.VisitChildren(child => found ??= TryFindViewport(child));
        return found ?? throw new InvalidOperationException("Missing viewport.");
    }

    private static RenderSingleChildViewport? TryFindViewport(RenderObject root)
    {
        RenderSingleChildViewport? found = root as RenderSingleChildViewport;
        root.VisitChildren(child => found ??= TryFindViewport(child));
        return found;
    }

    private sealed class PlatformBehavior(TargetPlatform platform) : ScrollBehavior
    {
        public override TargetPlatform GetPlatform(BuildContext context) => platform;
    }

    private sealed class ContentBox(Size desiredSize) : RenderBox
    {
        public Point LastPaintOffset { get; private set; }
        protected override void PerformLayout() => Size = Constraints.Constrain(desiredSize);
        protected override Size ComputeDryLayout(BoxConstraints constraints) => constraints.Constrain(desiredSize);
        protected override double ComputeMinIntrinsicWidth(double height) => desiredSize.Width;
        protected override double ComputeMaxIntrinsicWidth(double height) => desiredSize.Width;
        protected override double ComputeMinIntrinsicHeight(double width) => desiredSize.Height;
        protected override double ComputeMaxIntrinsicHeight(double width) => desiredSize.Height;
        protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline) => 20;
        protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline) => 20;
        public override void Paint(PaintingContext context, Point offset) => LastPaintOffset = offset;
    }

    private sealed class MutableOffset(double pixels) : ViewportOffset
    {
        private double _pixels = pixels;
        public List<string> Calls { get; } = [];
        public bool IsObserved => HasListeners;
        public override double Pixels => _pixels;
        public override bool HasPixels => true;
        public override ScrollDirection UserScrollDirection => ScrollDirection.Idle;
        public override bool AllowImplicitScrolling => false;
        public override void CorrectBy(double correction) { Calls.Add("correct"); _pixels += correction; }
        public override bool ApplyViewportDimension(double dimension) { Calls.Add("viewport"); return true; }
        public override bool ApplyContentDimensions(double min, double max) { Calls.Add("content"); return true; }
        public void SetPixels(double value) { _pixels = value; NotifyListeners(); }
        public override void JumpTo(double value) => SetPixels(value);
        public override Task AnimateTo(double to, TimeSpan duration, Curve? curve = null)
        {
            SetPixels(to);
            return Task.CompletedTask;
        }
    }
}
