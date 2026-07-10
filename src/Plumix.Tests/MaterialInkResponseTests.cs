using Avalonia;
using Avalonia.Media;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialInkResponseTests : IDisposable
{
    public MaterialInkResponseTests()
    {
        FocusManager.Instance.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    public void Dispose()
    {
        FocusManager.Instance.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    [Fact]
    public void Ink_ValidatesShorthandAndDimensions()
    {
        Assert.Throws<ArgumentException>(() => new Ink(
            color: Colors.Red,
            decoration: new BoxDecoration(Color: Colors.Blue)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Ink(width: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Ink(padding: new Thickness(-1, 0, 0, 0)));

        var provider = new MemoryImage([1, 2, 3]);
        var image = Ink.Image(provider);
        Assert.NotNull(image.Decoration?.Image);
        Assert.Same(provider, image.Decoration!.Image!.Image);
    }

    [Fact]
    public void Ink_PaintsDecorationBelowInkWellAndAppliesPaddingAndSize()
    {
        using var harness = CreateHarness(new Ink(
            width: 100,
            height: 56,
            padding: new Thickness(8, 4),
            color: Color.Parse("#FFEADDFF"),
            child: new InkWell(
                onTap: () => { },
                child: new Center(child: new Text("Ink")))));
        harness.Pump(new Size(160, 100));

        var decoration = Assert.Single(FindDescendants<RenderDecoratedBox>(harness.RenderView));
        Assert.Equal(Color.Parse("#FFEADDFF"), decoration.Decoration.Color);
        Assert.Equal(100, decoration.Size.Width, 3);
        Assert.Equal(56, decoration.Size.Height, 3);
        Assert.Contains(
            FindDescendants<RenderPadding>(harness.RenderView),
            padding => padding.Padding == new Thickness(8, 4));
        Assert.Single(FindDescendants<RenderInkResponsePaint>(harness.RenderView));
    }

    [Fact]
    public void Ink_WithoutChild_ExpandsToTheParentConstraints()
    {
        using var harness = CreateHarness(new Ink(color: Colors.Blue));
        harness.Pump(new Size(160, 100));

        var decoration = Assert.Single(FindDescendants<RenderDecoratedBox>(harness.RenderView));
        Assert.Equal(new Size(160, 100), decoration.Size);
    }

    [Fact]
    public void InkResponseAndInkWell_DefaultGeometryMatchesFlutter()
    {
        var response = new InkResponse();
        var well = new InkWell();

        Assert.False(response.ContainedInkWell);
        Assert.Equal(BoxShape.Circle, response.HighlightShape);
        Assert.True(well.ContainedInkWell);
        Assert.Equal(BoxShape.Rectangle, well.HighlightShape);
        Assert.True(response.EnableFeedback);
        Assert.True(response.CanRequestFocus);
        Assert.False(response.Autofocus);
        Assert.False(response.ExcludeFromSemantics);
        Assert.Throws<ArgumentOutOfRangeException>(() => new InkResponse(radius: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InkWell(hoverDuration: TimeSpan.FromMilliseconds(-1)));
    }

    [Fact]
    public void InkResponse_UsesCircleAndUncontainedSplashWhileInkWellClipsRectangle()
    {
        using var responseHarness = CreateHarness(new InkResponse(
            radius: 30,
            borderRadius: BorderRadius.Circular(12),
            onTap: () => { },
            child: new SizedBox(width: 80, height: 48)));
        responseHarness.Pump(new Size(120, 80));
        var responsePaint = Assert.Single(FindDescendants<RenderInkResponsePaint>(responseHarness.RenderView));
        Assert.Equal(BoxShape.Circle, responsePaint.HighlightShape);
        Assert.False(responsePaint.ContainedInkWell);
        Assert.Equal(30, responsePaint.SplashRadius);

        using var wellHarness = CreateHarness(new InkWell(
            radius: 24,
            borderRadius: BorderRadius.Circular(12),
            onTap: () => { },
            child: new SizedBox(width: 80, height: 48)));
        wellHarness.Pump(new Size(120, 80));
        var wellPaint = Assert.Single(FindDescendants<RenderInkResponsePaint>(wellHarness.RenderView));
        Assert.Equal(BoxShape.Rectangle, wellPaint.HighlightShape);
        Assert.True(wellPaint.ContainedInkWell);
        Assert.Equal(BorderRadius.Circular(12), wellPaint.BorderRadius);
    }

    [Fact]
    public void InkWell_PrimaryTapCallbacksAndStatesControllerFollowGestureLifecycle()
    {
        var events = new List<string>();
        var states = new MaterialStatesController();
        using var harness = CreateHarness(new InkWell(
            statesController: states,
            onTapDown: _ => events.Add("down"),
            onTapUp: _ => events.Add("up"),
            onHighlightChanged: value => events.Add(value ? "highlight-on" : "highlight-off"),
            onTap: () => events.Add("tap"),
            child: new SizedBox(width: 80, height: 48)));
        harness.Pump(new Size(120, 80));

        var now = DateTime.UtcNow;
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerDownEvent(
            701, PointerDeviceKind.Mouse, new Point(20, 20), PointerButtons.Primary, now));
        Assert.True(states.Value.HasFlag(MaterialState.Pressed));
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerUpEvent(
            701, PointerDeviceKind.Mouse, new Point(20, 20), PointerButtons.None, now.AddMilliseconds(20)));

        Assert.False(states.Value.HasFlag(MaterialState.Pressed));
        Assert.Equal(["highlight-on", "down", "up", "highlight-off", "tap"], events);
    }

    [Fact]
    public void InkResponse_SecondaryTapUsesDedicatedCallbacksWithoutPrimaryTap()
    {
        var events = new List<string>();
        using var harness = CreateHarness(new InkResponse(
            onTap: () => events.Add("primary"),
            onSecondaryTapDown: _ => events.Add("secondary-down"),
            onSecondaryTapUp: _ => events.Add("secondary-up"),
            onSecondaryTap: () => events.Add("secondary"),
            child: new SizedBox(width: 80, height: 48)));
        harness.Pump(new Size(120, 80));

        var now = DateTime.UtcNow;
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerDownEvent(
            702, PointerDeviceKind.Mouse, new Point(20, 20), PointerButtons.Secondary, now));
        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerUpEvent(
            702, PointerDeviceKind.Mouse, new Point(20, 20), PointerButtons.None, now.AddMilliseconds(20)));

        Assert.Equal(["secondary-down", "secondary-up", "secondary"], events);
    }

    [Fact]
    public void InkResponse_OverlayColorResolvesHoveredAndPressedStates()
    {
        var hovered = Color.Parse("#2200FF00");
        var pressed = Color.Parse("#330000FF");
        var controller = new MaterialStatesController();
        using var harness = CreateHarness(new InkResponse(
            statesController: controller,
            overlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Pressed) ? pressed
                : states.HasFlag(MaterialState.Hovered) ? hovered
                : null),
            onTap: () => { },
            child: new SizedBox(width: 80, height: 48)));
        harness.Pump(new Size(120, 80));

        var hoverListener = FindDescendants<RenderPointerListener>(harness.RenderView)
            .Single(listener => listener.OnPointerEnter is not null && listener.OnPointerExit is not null);
        hoverListener.HandleEvent(
            new PointerEnterEvent(703, PointerDeviceKind.Mouse, new Point(10, 10), PointerButtons.None, DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(10, 10)));
        harness.Pump(new Size(120, 80));
        Assert.True(controller.Value.HasFlag(MaterialState.Hovered));
        Assert.Equal(hovered, Assert.Single(FindDescendants<RenderInkResponsePaint>(harness.RenderView)).HighlightColor);

        GestureBinding.Instance.HandlePointerEvent(harness.RenderView, new PointerDownEvent(
            704, PointerDeviceKind.Mouse, new Point(20, 20), PointerButtons.Primary, DateTime.UtcNow));
        harness.Pump(new Size(120, 80));
        Assert.Equal(pressed, Assert.Single(FindDescendants<RenderInkResponsePaint>(harness.RenderView)).HighlightColor);
    }

    [Fact]
    public void InkResponse_SemanticsExposeOnlyConfiguredPrimaryActions()
    {
        int taps = 0;
        int longPresses = 0;
        using var harness = CreateHarness(new InkResponse(
            onTap: () => taps++,
            onLongPress: () => longPresses++,
            child: new SizedBox(width: 80, height: 48)));

        var semantics = harness.PumpAndGetSemantics(new Size(120, 80));
        var actionNode = FindSemantics(semantics, node =>
            node.Actions.HasFlag(SemanticsActions.Tap)
            && node.Actions.HasFlag(SemanticsActions.LongPress));
        Assert.NotNull(actionNode);
        Assert.True(actionNode!.PerformAction(SemanticsActions.Tap));
        Assert.True(actionNode.PerformAction(SemanticsActions.LongPress));
        Assert.Equal(1, taps);
        Assert.Equal(1, longPresses);

        using var excludedHarness = CreateHarness(new InkResponse(
            excludeFromSemantics: true,
            onTap: () => { },
            child: new SizedBox(width: 80, height: 48)));
        var excluded = excludedHarness.PumpAndGetSemantics(new Size(120, 80));
        Assert.Null(FindSemantics(excluded, node => node.Actions.HasFlag(SemanticsActions.Tap)));
    }

    private static WidgetRenderHarness CreateHarness(Widget child) => new(
        new Theme(ThemeData.Light, new Directionality(TextDirection.Ltr, child)));

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
        if (node is null) return null;
        if (predicate(node)) return node;
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

        public void Dispose() => _rootElement.Unmount();

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
