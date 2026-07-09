using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialTooltipTests
{
    [Fact]
    public void Tooltip_ValidatesMutuallyExclusiveSizingAndDurations()
    {
        Assert.Throws<ArgumentException>(() => new Tooltip(
            message: "tip",
            height: 24,
            constraints: new BoxConstraints(MinHeight: 24)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Tooltip(message: "tip", verticalOffset: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Tooltip(message: "tip", waitDuration: TimeSpan.FromMilliseconds(-1)));
    }

    [Fact]
    public void Tooltip_EmptyMessage_ReturnsChildWithoutTriggerWrapper()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                ThemeData.Light,
                new Tooltip(message: string.Empty, child: new Text("child"))));

        harness.Pump(new Size(120, 60));

        Assert.NotNull(FindParagraph(harness.RenderView, "child"));
        Assert.DoesNotContain(
            FindDescendants<RenderPointerListener>(harness.RenderView),
            listener => listener.OnPointerEnter is not null && listener.OnPointerExit is not null);
    }

    [Fact]
    public void Tooltip_DefaultDesktopAppearance_UsesPlatformMetricsAndBrightnessColors()
    {
        Scheduler.ResetForTests();
        try
        {
            var theme = ThemeData.Light with { Platform = TargetPlatform.Windows };
            using var harness = new WidgetRenderHarness(
                new Theme(theme, new Tooltip(message: "Desktop tip", child: new SizedBox(width: 24, height: 24))));
            harness.Pump(new Size(160, 80));

            Assert.True(harness.FindState<TooltipState>().EnsureTooltipVisible());
            harness.Pump(new Size(160, 80));

            var paragraph = FindParagraph(harness.RenderView, "Desktop tip");
            Assert.NotNull(paragraph);
            Assert.Equal(12, paragraph!.FontSize);
            Assert.Equal(Colors.White, Assert.IsType<SolidColorBrush>(paragraph.Foreground).Color);
            Assert.Contains(
                FindDescendants<RenderConstrainedBox>(harness.RenderView),
                box => box.AdditionalConstraints.MinHeight == 24);
            Assert.Contains(
                FindDescendants<RenderDecoratedBox>(harness.RenderView),
                box => box.Decoration.Color == Color.FromArgb(0xE6, 0x61, 0x61, 0x61));
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Tooltip_WidgetOverridesLocalAndGlobalThemeValues()
    {
        Scheduler.ResetForTests();
        try
        {
            var globalTheme = ThemeData.Light with
            {
                TooltipTheme = new TooltipThemeData(
                    Constraints: new BoxConstraints(MinHeight: 28),
                    Decoration: new BoxDecoration(Color: Colors.DarkGreen),
                    TextStyle: new TextStyle(Color: Colors.Gold, FontSize: 10)),
            };
            var localTheme = new TooltipThemeData(
                Constraints: new BoxConstraints(MinHeight: 30),
                Decoration: new BoxDecoration(Color: Colors.Purple),
                TextStyle: new TextStyle(Color: Colors.Orange, FontSize: 11));
            using var harness = new WidgetRenderHarness(
                new Theme(
                    globalTheme,
                    new TooltipTheme(
                        localTheme,
                        new Tooltip(
                            message: "Override tip",
                            constraints: new BoxConstraints(MinHeight: 34),
                            decoration: new BoxDecoration(Color: Colors.Navy),
                            textStyle: new TextStyle(Color: Colors.LimeGreen, FontSize: 13),
                            child: new SizedBox(width: 24, height: 24)))));
            harness.Pump(new Size(180, 80));
            harness.FindState<TooltipState>().EnsureTooltipVisible();
            harness.Pump(new Size(180, 80));

            var paragraph = FindParagraph(harness.RenderView, "Override tip");
            Assert.NotNull(paragraph);
            Assert.Equal(13, paragraph!.FontSize);
            Assert.Equal(Colors.LimeGreen, Assert.IsType<SolidColorBrush>(paragraph.Foreground).Color);
            Assert.Contains(
                FindDescendants<RenderConstrainedBox>(harness.RenderView),
                box => box.AdditionalConstraints.MinHeight == 34);
            Assert.Contains(
                FindDescendants<RenderDecoratedBox>(harness.RenderView),
                box => box.Decoration.Color == Colors.Navy);
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Tooltip_HoverHonorsWaitAndExitDurationsAndTriggeredCallback()
    {
        Scheduler.ResetForTests();
        try
        {
            int triggered = 0;
            using var harness = new WidgetRenderHarness(
                new Theme(
                    ThemeData.Light,
                    new Tooltip(
                        message: "Delayed tip",
                        waitDuration: TimeSpan.FromMilliseconds(300),
                        exitDuration: TimeSpan.FromMilliseconds(200),
                        onTriggered: () => triggered++,
                        child: new SizedBox(width: 24, height: 24))));
            harness.Pump(new Size(160, 80));

            var listener = FindTooltipListener(harness.RenderView);
            Assert.NotNull(listener);
            listener!.HandleEvent(PointerEnter(1), new BoxHitTestEntry(listener, new Point(5, 5)));
            double clock = Scheduler.CurrentSeconds;
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.20));
            harness.Pump(new Size(160, 80));
            Assert.Null(FindParagraph(harness.RenderView, "Delayed tip"));

            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.40));
            harness.Pump(new Size(160, 80));
            Assert.NotNull(FindParagraph(harness.RenderView, "Delayed tip"));
            Assert.Equal(1, triggered);

            listener = FindTooltipListener(harness.RenderView);
            listener!.HandleEvent(PointerExit(1), new BoxHitTestEntry(listener, new Point(100, 5)));
            clock = Scheduler.CurrentSeconds;
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.10));
            harness.Pump(new Size(160, 80));
            Assert.NotNull(FindParagraph(harness.RenderView, "Delayed tip"));

            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.30));
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.50));
            harness.Pump(new Size(160, 80));
            Assert.Null(FindParagraph(harness.RenderView, "Delayed tip"));
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    [Fact]
    public void Tooltip_ContributesMessageSemanticsBeforeOverlayIsVisible()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                ThemeData.Light,
                new Tooltip(message: "Semantic tip", child: new SizedBox(width: 24, height: 24))));

        var semantics = harness.PumpAndGetSemantics(new Size(100, 60));

        Assert.NotNull(FindSemantics(semantics, node => node.Label == "Semantic tip"));
        Assert.Null(FindParagraph(harness.RenderView, "Semantic tip"));
    }

    [Fact]
    public void Tooltip_DismissAllToolTips_ClosesVisibleInstances()
    {
        Scheduler.ResetForTests();
        try
        {
            using var harness = new WidgetRenderHarness(
                new Theme(
                    ThemeData.Light,
                    new Row(
                        children:
                        [
                            new Tooltip(message: "One", child: new SizedBox(width: 24, height: 24)),
                            new Tooltip(message: "Two", child: new SizedBox(width: 24, height: 24)),
                        ])));
            harness.Pump(new Size(120, 60));
            foreach (var state in harness.FindStates<TooltipState>())
            {
                state.EnsureTooltipVisible();
            }
            harness.Pump(new Size(120, 60));
            Assert.NotNull(FindParagraph(harness.RenderView, "One"));
            Assert.NotNull(FindParagraph(harness.RenderView, "Two"));

            Assert.True(Tooltip.DismissAllToolTips());
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.20));
            harness.Pump(new Size(120, 60));
            Assert.Null(FindParagraph(harness.RenderView, "One"));
            Assert.Null(FindParagraph(harness.RenderView, "Two"));
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    private static PointerEnterEvent PointerEnter(int pointer) => new(
        pointer, PointerDeviceKind.Mouse, new Point(5, 5), PointerButtons.None, DateTime.UtcNow);

    private static PointerExitEvent PointerExit(int pointer) => new(
        pointer, PointerDeviceKind.Mouse, new Point(100, 5), PointerButtons.None, DateTime.UtcNow);

    private static RenderPointerListener? FindTooltipListener(RenderObject? root)
    {
        return FindDescendants<RenderPointerListener>(root)
            .FirstOrDefault(listener => listener.OnPointerEnter is not null && listener.OnPointerExit is not null);
    }

    private static RenderParagraph? FindParagraph(RenderObject? root, string text)
    {
        return FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.Text == text);
    }

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null) return result;
        if (root is T target) result.Add(target);
        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private static SemanticsNode? FindSemantics(SemanticsNode? node, Func<SemanticsNode, bool> predicate)
    {
        if (node is null || predicate(node)) return node;
        foreach (var child in node.Children)
        {
            var result = FindSemantics(child, predicate);
            if (result is not null) return result;
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

        public SemanticsNode? PumpAndGetSemantics(Size size)
        {
            Pump(size);
            _pipeline.RequestSemanticsUpdate();
            _pipeline.FlushSemantics();
            return _pipeline.SemanticsOwner.RootNode;
        }

        public T FindState<T>() where T : State => FindStates<T>().Single();

        public IReadOnlyList<T> FindStates<T>() where T : State
        {
            var states = new List<T>();
            CollectStates(_rootElement, states);
            return states;
        }

        public void Dispose() => _rootElement.Unmount();

        private static void CollectStates<T>(Element element, List<T> states) where T : State
        {
            if (element is StatefulElement stateful && stateful.State is T state) states.Add(state);
            element.VisitChildren(child => CollectStates(child, states));
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
            internal override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
            internal override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
            public void InsertRenderObjectChild(RenderObject child, object? slot) => _renderView.Child = (RenderBox)child;
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot) { if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null; }
            internal override void Unmount() { if (_child is not null) { UnmountChild(_child); _child = null; } base.Unmount(); }
        }
    }
}
