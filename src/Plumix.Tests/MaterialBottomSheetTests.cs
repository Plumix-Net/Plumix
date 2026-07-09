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
public sealed class MaterialBottomSheetTests : IDisposable
{
    public MaterialBottomSheetTests()
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
    public void BottomSheetAndTheme_ValidateFlutterContracts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BottomSheet(() => { }, _ => new SizedBox(), elevation: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BottomSheet(() => { }, _ => new SizedBox(), dragHandleSize: new Size(-1, 4)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BottomSheetThemeData(Elevation: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BottomSheetThemeData(ModalElevation: double.PositiveInfinity));
    }

    [Fact]
    public void BottomSheet_M3DefaultsAndThemeWidgetPrecedenceMatchSource()
    {
        var theme = ThemeData.Light with
        {
            BottomSheetTheme = new BottomSheetThemeData(
                BackgroundColor: Colors.Purple,
                Elevation: 0,
                Shape: ShapeBorder.RoundedRectangle(12),
                ShowDragHandle: true,
                DragHandleColor: MaterialStateProperty<Color?>.All(Colors.Green),
                DragHandleSize: new Size(40, 6)),
        };
        using var harness = new WidgetRenderHarness(Wrap(
            theme,
            new BottomSheet(
                onClosing: () => { },
                builder: _ => new SizedBox(width: 200, height: 80),
                animationController: BottomSheet.CreateAnimationController(),
                backgroundColor: Colors.Orange,
                shape: ShapeBorder.RoundedRectangle(8),
                showDragHandle: true,
                enableDrag: false)));
        var semantics = harness.PumpAndGetSemantics(new Size(800, 400));

        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView), box =>
            box.Decoration.Color == Colors.Orange
            && box.Decoration.EffectiveBorderRadius == BorderRadius.Circular(8));
        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView), box =>
            box.Decoration.Color == Colors.Green
            && box.Decoration.EffectiveBorderRadius == BorderRadius.Circular(3));
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView), box =>
            box.AdditionalConstraints.MaxWidth == 640);
        Assert.NotNull(FindSemantics(semantics, node =>
            node.Label == "Dismiss"
            && node.Flags.HasFlag(SemanticsFlags.IsButton)
            && node.Actions.HasFlag(SemanticsActions.Tap)));
    }

    [Fact]
    public void ModalLayout_UsesNineSixteenthsCapUnlessScrollControlled()
    {
        using var capped = new WidgetRenderHarness(new ModalBottomSheetLayout(
            animationValue: 1,
            isScrollControlled: false,
            maxHeightRatio: 9.0 / 16.0,
            child: new SizedBox(height: 1000)));
        capped.Pump(new Size(400, 320));
        var cappedLayout = Assert.Single(FindDescendants<RenderModalBottomSheetLayout>(capped.RenderView));
        Assert.Equal(180, cappedLayout.Child!.Size.Height, precision: 3);
        Assert.Equal(140, ((BoxParentData)cappedLayout.Child.parentData!).offset.Y, precision: 3);

        using var full = new WidgetRenderHarness(new ModalBottomSheetLayout(
            animationValue: 1,
            isScrollControlled: true,
            maxHeightRatio: 9.0 / 16.0,
            child: new SizedBox(height: 1000)));
        full.Pump(new Size(400, 320));
        Assert.Equal(320, Assert.Single(FindDescendants<RenderModalBottomSheetLayout>(full.RenderView)).Child!.Size.Height, precision: 3);
    }

    [Fact]
    public void BottomSheet_DragBelowHalfClosesAndReportsClosing()
    {
        var controller = BottomSheet.CreateAnimationController();
        controller.SetValue(1);
        int closingCalls = 0;
        bool? reportedClosing = null;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new BottomSheet(
                onClosing: () => closingCalls++,
                builder: _ => new SizedBox(width: 240, height: 200),
                animationController: controller,
                onDragEnd: (_, closing) => reportedClosing = closing)));
        harness.Pump(new Size(240, 300));

        var binding = GestureBinding.Instance;
        var start = DateTime.UtcNow;
        DispatchDown(binding, harness.RenderView, 41, new Point(120, 20), start);
        DispatchMove(binding, harness.RenderView, 41, new Point(120, 145), start.AddMilliseconds(300));
        DispatchUp(binding, harness.RenderView, 41, new Point(120, 145), start.AddMilliseconds(600));

        Assert.True(reportedClosing);
        Assert.Equal(1, closingCalls);
        Assert.True(controller.Value < 0.5);
    }

    [Fact]
    public async Task ModalBottomSheet_RouteKeepsUnderlyingPageAndReturnsTypedResult()
    {
        BuildContext captured = default;
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(context => new CaptureContext(
                value => captured = value,
                child: new Text("Underlying"))))));
        harness.Pump(new Size(500, 400));

        var result = MaterialBottomSheets.ShowModalBottomSheet<string>(
            captured,
            _ => new SizedBox(height: 120, child: new Text("Modal sheet")),
            barrierLabel: "Close sheet");
        PumpAnimation();
        var semantics = harness.PumpAndGetSemantics(new Size(500, 400));

        Assert.NotNull(FindParagraph(harness.RenderView, "Underlying"));
        Assert.NotNull(FindParagraph(harness.RenderView, "Modal sheet"));
        Assert.NotNull(FindSemantics(semantics, node =>
            node.Label == "Close sheet" && node.Actions.HasFlag(SemanticsActions.Tap)));
        Navigator.Of(captured).Pop("done");
        PumpAnimation();
        harness.Pump(new Size(500, 400));
        Assert.Equal("done", await result);
        Assert.Null(FindParagraph(harness.RenderView, "Modal sheet"));
    }

    [Fact]
    public async Task PersistentBottomSheet_ControllerRebuildCloseAndLocalHistoryMatchScaffoldFlow()
    {
        BuildContext captured = default;
        string label = "First";
        using var harness = new WidgetRenderHarness(Wrap(
            ThemeData.Light,
            new Navigator(new BuilderPageRoute(_ => new Scaffold(
                body: new CaptureContext(value => captured = value, new Text("Body")))))));
        harness.Pump(new Size(500, 400));

        var controller = MaterialBottomSheets.ShowBottomSheet(
            captured,
            _ => new SizedBox(height: 96, child: new Text(label)));
        PumpAnimation();
        harness.Pump(new Size(500, 400));
        Assert.NotNull(FindParagraph(harness.RenderView, "First"));
        Assert.True(ModalRoute.Of(captured).ImpliesAppBarDismissal);

        controller.SetState(() => label = "Second");
        harness.Pump(new Size(500, 400));
        Assert.NotNull(FindParagraph(harness.RenderView, "Second"));
        controller.Close();
        PumpAnimation();
        harness.Pump(new Size(500, 400));
        await controller.Closed;
        Assert.Null(FindParagraph(harness.RenderView, "Second"));
    }

    private static Widget Wrap(ThemeData theme, Widget child) => new Directionality(
        TextDirection.Ltr,
        new MediaQuery(
            new MediaQueryData(Size: new Size(800, 600)),
            new MaterialLocalizationsScope(DefaultMaterialLocalizations.Instance, new Theme(theme, child))));

    private static void PumpAnimation()
    {
        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.30));
    }

    private static void DispatchDown(GestureBinding binding, RenderView view, int pointer, Point position, DateTime time) =>
        binding.HandlePointerEvent(view, new PointerDownEvent(pointer, PointerDeviceKind.Mouse, position, PointerButtons.Primary, time));
    private static void DispatchMove(GestureBinding binding, RenderView view, int pointer, Point position, DateTime time) =>
        binding.HandlePointerEvent(view, new PointerMoveEvent(pointer, PointerDeviceKind.Mouse, position, PointerButtons.Primary, true, time));
    private static void DispatchUp(GestureBinding binding, RenderView view, int pointer, Point position, DateTime time) =>
        binding.HandlePointerEvent(view, new PointerUpEvent(pointer, PointerDeviceKind.Mouse, position, PointerButtons.None, time));

    private static RenderParagraph? FindParagraph(RenderObject? root, string text) =>
        FindDescendants<RenderParagraph>(root).FirstOrDefault(value => value.Text == text);

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
            var found = FindSemantics(child, predicate);
            if (found is not null) return found;
        }
        return null;
    }

    private sealed class CaptureContext : StatelessWidget
    {
        private readonly Action<BuildContext> _capture;
        private readonly Widget _child;
        public CaptureContext(Action<BuildContext> capture, Widget child) { _capture = capture; _child = child; }
        public override Widget Build(BuildContext context) { _capture(context); return _child; }
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _root;
        private readonly PipelineOwner _pipeline;
        public WidgetRenderHarness(Widget widget)
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
        public void Pump(Size size) { _owner.FlushBuild(); _pipeline.RequestLayout(); _pipeline.FlushLayout(size); _pipeline.FlushCompositingBits(); _pipeline.FlushPaint(); }
        public SemanticsNode? PumpAndGetSemantics(Size size) { Pump(size); _pipeline.RequestSemanticsUpdate(); _pipeline.FlushSemantics(); return _pipeline.SemanticsOwner.RootNode; }
        public void Dispose() => _root.Unmount();

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _view;
            private Element? _child;
            public HarnessRootElement(RenderView view, Widget widget) : base(widget) => _view = view;
            public override RenderObject? RenderObject => _child?.RenderObject;
            internal override Element? RenderObjectAttachingChild => _child;
            protected override void OnMount() { base.OnMount(); Rebuild(); }
            internal override void Rebuild() { Dirty = false; _child = UpdateChild(_child, Widget, Slot); }
            internal override void Update(Widget newWidget) { base.Update(newWidget); Rebuild(); }
            internal override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
            internal override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
            public void InsertRenderObjectChild(RenderObject child, object? slot) => _view.Child = (RenderBox)child;
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot) { if (ReferenceEquals(_view.Child, child)) _view.Child = null; }
            internal override void Unmount() { if (_child is not null) { UnmountChild(_child); _child = null; } base.Unmount(); }
        }
    }

}
