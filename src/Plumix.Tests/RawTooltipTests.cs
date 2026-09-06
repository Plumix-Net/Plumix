using Avalonia;
using Plumix;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class RawTooltipTests
{
    [Fact]
    public void RawTooltip_DefaultsAndDurationGuardsMatchFlutter()
    {
        var tooltip = new RawTooltip(
            semanticsTooltip: "Raw tip",
            tooltipBuilder: (_, _) => new SizedBox(),
            child: new SizedBox());

        Assert.Equal(TimeSpan.Zero, tooltip.HoverDelay);
        Assert.Equal(TimeSpan.FromMilliseconds(1500), tooltip.TouchDelay);
        Assert.Equal(TimeSpan.FromMilliseconds(100), tooltip.DismissDelay);
        Assert.Equal(TooltipTriggerMode.LongPress, tooltip.TriggerMode);
        Assert.True(tooltip.EnableTapToDismiss);
        Assert.True(tooltip.EnableFeedback);
        Assert.False(tooltip.IgnorePointer);
        var positionContext = new TooltipPositionContext(
            new Point(),
            new Size(),
            new Size(),
            VerticalOffset: 0);
        Assert.Equal(double.PositiveInfinity, positionContext.OverlaySize.Width);
        Assert.Equal(double.PositiveInfinity, positionContext.OverlaySize.Height);
        Assert.Throws<ArgumentOutOfRangeException>(() => new RawTooltip(
            semanticsTooltip: "Raw tip",
            tooltipBuilder: (_, _) => new SizedBox(),
            child: new SizedBox(),
            dismissDelay: TimeSpan.FromMilliseconds(-1)));
    }

    [Fact]
    public void RawTooltip_ProgrammaticShowUsesOverlayAndFlipsAboveAtBottomEdge()
    {
        var key = new LabeledGlobalKey<RawTooltipState>("raw-position");
        using var harness = new WidgetRenderHarness(
            new Align(
                alignment: Alignment.BottomCenter,
                child: new RawTooltip(
                    key: key,
                    semanticsTooltip: "Positioned raw tip",
                    tooltipBuilder: (_, animation) => new FadeTransition(
                        opacity: animation,
                        child: new SizedBox(width: 60, height: 20)),
                    child: new SizedBox(width: 20, height: 20))));

        harness.Pump(new Size(200, 100));
        Assert.True(key.CurrentState!.EnsureTooltipVisible());
        harness.Pump(new Size(200, 100));

        RenderCustomSingleChildLayoutBox layout =
            Assert.Single(FindDescendants<RenderCustomSingleChildLayoutBox>(harness.RenderView));
        var childParentData = Assert.IsType<BoxParentData>(layout.Child!.parentData);
        Assert.Equal(70, childParentData.offset.X, precision: 3);
        Assert.Equal(70, childParentData.offset.Y, precision: 3);
        Assert.Equal(new Size(200, 100), layout.Size);
    }

    [Fact]
    public void RawTooltip_CustomPositionDelegateReceivesTargetAndOverlayGeometry()
    {
        TooltipPositionContext? received = null;
        var key = new LabeledGlobalKey<RawTooltipState>("raw-custom-position");
        using var harness = new WidgetRenderHarness(
            new Align(
                alignment: Alignment.TopLeft,
                child: new RawTooltip(
                    key: key,
                    semanticsTooltip: "Custom raw tip",
                    positionDelegate: context =>
                    {
                        received = context;
                        return new Point(7, 9);
                    },
                    tooltipBuilder: (_, _) => new SizedBox(width: 40, height: 12),
                    child: new SizedBox(width: 30, height: 20))));

        harness.Pump(new Size(180, 90));
        key.CurrentState!.EnsureTooltipVisible();
        harness.Pump(new Size(180, 90));

        Assert.NotNull(received);
        Assert.Equal(new Point(15, 10), received!.Target);
        Assert.Equal(new Size(30, 20), received.TargetSize);
        Assert.Equal(new Size(40, 12), received.TooltipSize);
        Assert.Equal(new Size(180, 90), received.OverlaySize);
        RenderCustomSingleChildLayoutBox layout =
            Assert.Single(FindDescendants<RenderCustomSingleChildLayoutBox>(harness.RenderView));
        Assert.Equal(new Point(7, 9), Assert.IsType<BoxParentData>(layout.Child!.parentData).offset);
    }

    [Fact]
    public void RawTooltip_TapTriggersFeedbackCallbackAndTimedDismiss()
    {
        Scheduler.ResetForTests();
        try
        {
            int triggered = 0;
            int feedback = 0;
            void HandleFeedback(FeedbackType _) => feedback++;
            Feedback.FeedbackTriggered += HandleFeedback;
            var key = new LabeledGlobalKey<RawTooltipState>("raw-tap");
            using var harness = new WidgetRenderHarness(
                new Align(
                    alignment: Alignment.TopLeft,
                    child: new RawTooltip(
                        key: key,
                        semanticsTooltip: "Tap raw tip",
                        triggerMode: TooltipTriggerMode.Tap,
                        touchDelay: TimeSpan.FromMilliseconds(200),
                        onTriggered: () => triggered++,
                        tooltipBuilder: (_, _) => new Text("Tap overlay"),
                        child: new SizedBox(width: 40, height: 40))));
            harness.Pump(new Size(160, 80));

            DateTime timestamp = DateTime.UtcNow;
            harness.Dispatch(new PointerDownEvent(
                12,
                PointerDeviceKind.Touch,
                new Point(10, 10),
                PointerButtons.Primary,
                timestamp));
            harness.Dispatch(new PointerUpEvent(
                12,
                PointerDeviceKind.Touch,
                new Point(10, 10),
                PointerButtons.None,
                timestamp.AddMilliseconds(20)));
            harness.Pump(new Size(160, 80));

            Assert.Equal(1, triggered);
            Assert.Equal(1, feedback);
            Assert.NotNull(FindParagraph(harness.RenderView, "Tap overlay"));

            double clock = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.25));
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.40));
            harness.Pump(new Size(160, 80));
            Assert.Null(FindParagraph(harness.RenderView, "Tap overlay"));
            Feedback.FeedbackTriggered -= HandleFeedback;
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void RawTooltip_UsesTooltipSemanticsAndEmptyTextSkipsInteraction()
    {
        using var semanticHarness = new WidgetRenderHarness(
            new RawTooltip(
                semanticsTooltip: "Semantic raw tip",
                tooltipBuilder: (_, _) => new SizedBox(),
                child: new SizedBox(width: 20, height: 20)));
        SemanticsNode? semantics = semanticHarness.PumpAndGetSemantics(new Size(80, 40));
        Assert.NotNull(FindSemantics(semantics, node => node.Tooltip == "Semantic raw tip"));

        using var emptyHarness = new WidgetRenderHarness(
            new RawTooltip(
                semanticsTooltip: string.Empty,
                tooltipBuilder: (_, _) => new SizedBox(),
                child: new SizedBox(width: 20, height: 20)));
        emptyHarness.Pump(new Size(80, 40));
        Assert.Empty(FindDescendants<RenderExclusiveMouseRegion>(emptyHarness.RenderView));
    }

    private static RenderParagraph? FindParagraph(RenderObject? root, string text)
    {
        return FindDescendants<RenderParagraph>(root)
            .FirstOrDefault(paragraph => paragraph.PlainText == text);
    }

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null)
        {
            return result;
        }

        if (root is T target)
        {
            result.Add(target);
        }

        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private static SemanticsNode? FindSemantics(
        SemanticsNode? node,
        Func<SemanticsNode, bool> predicate)
    {
        if (node is null || predicate(node))
        {
            return node;
        }

        foreach (SemanticsNode child in node.Children)
        {
            SemanticsNode? result = FindSemantics(child, predicate);
            if (result is not null)
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
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget rootWidget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _rootElement = new HarnessRootElement(
                RenderView,
                Overlay.Wrap(rootWidget));
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

        public void Dispatch(PointerEvent @event)
        {
            GestureBinding.Instance.HandlePointerEvent(RenderView, @event);
        }

        public SemanticsNode? PumpAndGetSemantics(Size size)
        {
            Pump(size);
            _pipeline.RequestSemanticsUpdate();
            _pipeline.FlushSemantics();
            return _pipeline.SemanticsOwner!.RootNode;
        }

        public void Dispose()
        {
            _rootElement.Unmount();
        }

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public HarnessRootElement(
                RenderView renderView,
                Widget widget) : base(widget)
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

            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
                _renderView.Child = (RenderBox)child;
            }

            public void MoveRenderObjectChild(
                RenderObject child,
                object? oldSlot,
                object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(_renderView.Child, child))
                {
                    _renderView.Child = null;
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
        }
    }
}
