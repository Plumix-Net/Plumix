using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialSnackBarTests : IDisposable
{
    public MaterialSnackBarTests()
    {
        Scheduler.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    public void Dispose()
    {
        GestureBinding.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void SnackBar_ValidatesFlutterContractsAndCopiesAnimation()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Bar(elevation: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Bar(width: 0, behavior: SnackBarBehavior.Floating));
        Assert.Throws<ArgumentOutOfRangeException>(() => Bar(actionOverflowThreshold: 1.1));
        Assert.Throws<ArgumentException>(() => Bar(width: 200, margin: new Thickness(8), behavior: SnackBarBehavior.Floating));
        Assert.Throws<ArgumentException>(() => Bar(width: 200, behavior: SnackBarBehavior.Fixed));
        Assert.Throws<ArgumentOutOfRangeException>(() => Bar(margin: new Thickness(-1), behavior: SnackBarBehavior.Floating));
        Assert.Throws<ArgumentException>(() => new SnackBarThemeData(Width: 200));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SnackBarThemeData(ActionOverflowThreshold: -0.1));

        Assert.False(Bar().Persist);
        Assert.True(Bar(action: Action()).Persist);
        Assert.False(Bar(action: Action(), persist: false).Persist);

        using var animation = SnackBar.CreateAnimationController();
        var original = Bar(key: new ValueKey<string>("snack"));
        var copy = original.WithAnimation(animation, new ValueKey<string>("fallback"));
        Assert.Equal(TimeSpan.FromMilliseconds(250), animation.Duration);
        Assert.Same(animation, copy.Animation);
        Assert.Equal(original.Key, copy.Key);
    }

    [Fact]
    public void SnackBar_M3DefaultsAndThemePrecedenceMatchSource()
    {
        var themedData = ThemeData.Light with
        {
            SnackBarTheme = new SnackBarThemeData(
                BackgroundColor: Colors.Purple,
                ActionTextColor: Colors.Gold,
                ContentTextStyle: ThemeData.Light.TextTheme.BodyMedium.CopyWith(
                    color: Colors.Orange,
                    fontSize: 18),
                Elevation: 0,
                Behavior: SnackBarBehavior.Floating,
                InsetPadding: new Thickness(7),
                Shape: ShapeBorder.RoundedRectangle(9)),
        };
        using var themed = new WidgetRenderHarness(Wrap(themedData, Bar(action: Action())));
        themed.Pump(new Size(360, 180));

        Assert.Contains(FindDescendants<RenderDecoratedBox>(themed.RenderView), box =>
            box.Decoration.Color == Colors.Purple
            && box.Decoration.EffectiveBorderRadius == BorderRadius.Circular(9));
        var content = FindParagraph(themed.RenderView, "Message");
        Assert.NotNull(content);
        Assert.Equal(18, content!.FontSize);
        Assert.Equal(Colors.Orange, Assert.IsType<SolidColorBrush>(content.Foreground).Color);
        var action = FindParagraph(themed.RenderView, "UNDO");
        Assert.NotNull(action);
        Assert.Equal(Colors.Gold, Assert.IsType<SolidColorBrush>(action!.Foreground).Color);
        Assert.Contains(FindDescendants<RenderPadding>(themed.RenderView), value => value.Padding == new Thickness(7));

        using var explicitColor = new WidgetRenderHarness(Wrap(
            themedData,
            Bar(backgroundColor: Colors.Green, behavior: SnackBarBehavior.Floating)));
        explicitColor.Pump(new Size(360, 180));
        Assert.Contains(FindDescendants<RenderDecoratedBox>(explicitColor.RenderView), box =>
            box.Decoration.Color == Colors.Green);
    }

    [Fact]
    public void SnackBar_M3LightAndM2DarkDefaultsUseOppositeSurfaceContrast()
    {
        using var material3 = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            Bar(action: Action())));
        material3.Pump(new Size(360, 180));
        Assert.Contains(FindDescendants<RenderDecoratedBox>(material3.RenderView), box =>
            box.Decoration.Color == ThemeData.Light.InverseSurfaceColor);
        Assert.Equal(ThemeData.Light.OnInverseSurfaceColor,
            Assert.IsType<SolidColorBrush>(FindParagraph(material3.RenderView, "Message")!.Foreground).Color);
        Assert.Equal(ThemeData.Light.InversePrimaryColor,
            Assert.IsType<SolidColorBrush>(FindParagraph(material3.RenderView, "UNDO")!.Foreground).Color);

        var material2Dark = ThemeData.Dark with { UseMaterial3 = false };
        using var material2 = new WidgetRenderHarness(Wrap(material2Dark, Bar(action: Action())));
        material2.Pump(new Size(360, 180));
        Assert.Contains(FindDescendants<RenderDecoratedBox>(material2.RenderView), box =>
            box.Decoration.Color == material2Dark.OnSurfaceColor);
        Assert.Equal(Colors.Black,
            Assert.IsType<SolidColorBrush>(FindParagraph(material2.RenderView, "Message")!.Foreground).Color);
        Assert.Equal(material2Dark.SecondaryColor,
            Assert.IsType<SolidColorBrush>(FindParagraph(material2.RenderView, "UNDO")!.Foreground).Color);
    }

    [Fact]
    public void SnackBar_ActionOverflowUsesMeasuredThresholdAndRtlEndAlignment()
    {
        using var narrow = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            Bar(action: Action(), actionOverflowThreshold: 0.25),
            direction: TextDirection.Rtl));
        narrow.Pump(new Size(180, 180));

        var narrowLayout = Assert.Single(FindDescendants<RenderSnackBarContentLayout>(narrow.RenderView));
        var narrowContent = narrowLayout.FirstChild!;
        var narrowTrailing = narrowLayout.LastChild!;
        var narrowContentOffset = ((SnackBarContentParentData)narrowContent.parentData!).offset;
        var narrowTrailingOffset = ((SnackBarContentParentData)narrowTrailing.parentData!).offset;
        Assert.True(narrowTrailingOffset.Y >= narrowContent.Size.Height);
        Assert.Equal(0, narrowTrailingOffset.X, precision: 3);
        Assert.True(narrowContentOffset.X > 0);

        using var wide = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            Bar(action: Action(), actionOverflowThreshold: 0.25)));
        wide.Pump(new Size(600, 180));
        var wideLayout = Assert.Single(FindDescendants<RenderSnackBarContentLayout>(wide.RenderView));
        var wideContent = wideLayout.FirstChild!;
        var wideTrailing = wideLayout.LastChild!;
        var wideTrailingOffset = ((SnackBarContentParentData)wideTrailing.parentData!).offset;
        Assert.True(wideTrailingOffset.Y < wideContent.Size.Height);
        Assert.Equal(wideLayout.Size.Width - wideTrailing.Size.Width, wideTrailingOffset.X, precision: 3);
    }

    [Fact]
    public void SnackBarAction_CanOnlyBePressedOnceAndUsesDisabledColors()
    {
        int calls = 0;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new SizedBox(
                width: 160,
                height: 60,
                child: new SnackBarAction(
                    label: "UNDO",
                    onPressed: () => calls++,
                    textColor: Colors.Green,
                    disabledTextColor: Colors.Red))));
        harness.Pump(new Size(160, 60));

        Tap(harness.RenderView, new Point(80, 30), 901);
        harness.Pump(new Size(160, 60));
        Tap(harness.RenderView, new Point(80, 30), 902);
        harness.Pump(new Size(160, 60));

        Assert.Equal(1, calls);
        var label = FindParagraph(harness.RenderView, "UNDO");
        Assert.NotNull(label);
        Assert.Equal(Colors.Red, Assert.IsType<SolidColorBrush>(label!.Foreground).Color);
    }

    [Fact]
    public async Task ScaffoldMessenger_QueuesSnackBarsAndCompletesClosedReasons()
    {
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new ScaffoldMessenger(
                new Scaffold(body: new SizedBox()))));
        harness.Pump(new Size(360, 220));
        var messenger = harness.FindState<ScaffoldMessengerState>();

        var first = messenger.ShowSnackBar(Bar(content: "First"));
        var second = messenger.ShowSnackBar(Bar(content: "Second"));
        harness.Pump(new Size(360, 220));
        Assert.NotNull(FindParagraph(harness.RenderView, "First"));
        Assert.Null(FindParagraph(harness.RenderView, "Second"));

        messenger.RemoveCurrentSnackBar(SnackBarClosedReason.Remove);
        harness.Pump(new Size(360, 220));
        Assert.True(first.Closed.IsCompletedSuccessfully);
        Assert.Equal(SnackBarClosedReason.Remove, await first.Closed);
        Assert.Null(FindParagraph(harness.RenderView, "First"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Second"));

        second.Close();
        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.35));
        harness.Pump(new Size(360, 220));
        Assert.True(second.Closed.IsCompletedSuccessfully);
        Assert.Equal(SnackBarClosedReason.Hide, await second.Closed);
        Assert.Null(FindParagraph(harness.RenderView, "Second"));
    }

    [Fact]
    public async Task SnackBar_CloseIconAndSemanticsDismissUseMessengerReasons()
    {
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new ScaffoldMessenger(new Scaffold(body: new SizedBox()))));
        harness.Pump(new Size(360, 220));
        var messenger = harness.FindState<ScaffoldMessengerState>();
        var controller = messenger.ShowSnackBar(Bar(showCloseIcon: true, persist: true));
        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.35));
        var semantics = harness.PumpAndGetSemantics(new Size(360, 220));

        var liveRegion = FindSemantics(semantics, node =>
            node.Flags.HasFlag(SemanticsFlags.IsLiveRegion)
            && node.Actions.HasFlag(SemanticsActions.Dismiss));
        Assert.NotNull(liveRegion);
        Assert.NotNull(FindParagraph(harness.RenderView, char.ConvertFromUtf32(Icons.Close.CodePoint)));
        Assert.True(liveRegion!.PerformAction(SemanticsActions.Dismiss));
        harness.Pump(new Size(360, 220));
        Assert.True(controller.Closed.IsCompletedSuccessfully);
        Assert.Equal(SnackBarClosedReason.Dismiss, await controller.Closed);
    }

    private static SnackBar Bar(
        string content = "Message",
        Color? backgroundColor = null,
        double? elevation = null,
        Thickness? margin = null,
        Thickness? padding = null,
        double? width = null,
        SnackBarBehavior? behavior = null,
        SnackBarAction? action = null,
        double? actionOverflowThreshold = null,
        bool? showCloseIcon = null,
        bool? persist = null,
        Key? key = null) => new(
        content: new Text(content),
        backgroundColor: backgroundColor,
        elevation: elevation,
        margin: margin,
        padding: padding,
        width: width,
        behavior: behavior,
        action: action,
        actionOverflowThreshold: actionOverflowThreshold,
        showCloseIcon: showCloseIcon,
        persist: persist,
        key: key);

    private static SnackBarAction Action() => new("UNDO", () => { });

    private static Widget Wrap(
        ThemeData theme,
        Widget child,
        TextDirection direction = TextDirection.Ltr) =>
        new Directionality(
            direction,
            new MediaQuery(
                new MediaQueryData(Size: new Size(600, 240)),
                new Theme(theme, child)));

    private static void Tap(RenderView renderView, Point position, int pointer)
    {
        var binding = GestureBinding.Instance;
        var timestamp = DateTime.UtcNow;
        binding.HandlePointerEvent(renderView, new PointerDownEvent(
            pointer, PointerDeviceKind.Mouse, position, PointerButtons.Primary, timestamp));
        binding.HandlePointerEvent(renderView, new PointerUpEvent(
            pointer, PointerDeviceKind.Mouse, position, PointerButtons.None, timestamp.AddMilliseconds(20)));
    }

    private static RenderParagraph? FindParagraph(RenderObject? root, string text) =>
        FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.Text == text);

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

        public T FindState<T>() where T : State
        {
            var states = new List<T>();
            CollectStates(_rootElement, states);
            return Assert.Single(states);
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
