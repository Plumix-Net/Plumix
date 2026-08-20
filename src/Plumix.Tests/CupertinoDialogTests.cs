using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Cupertino;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class CupertinoDialogTests : IDisposable
{
    public CupertinoDialogTests()
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
    public void AlertDialog_UsesFixedWidthTitleAndContentStylesAndSurfaceColor()
    {
        using var harness = new RenderHarness(Wrap(new CupertinoAlertDialog(
            title: new Text("The Title"),
            content: new Text("Content"),
            actions: [new CupertinoDialogAction(new Text("OK"), onPressed: () => { })])));
        harness.Pump(new Size(600, 600));

        RenderParagraph title = FindParagraph(harness.RenderView, "The Title")!;
        Assert.Equal(17.0, title.FontSize);
        Assert.Equal(Avalonia.Media.FontWeight.SemiBold, title.FontWeight);
        RenderParagraph content = FindParagraph(harness.RenderView, "Content")!;
        Assert.Equal(13.0, content.FontSize);
        Assert.Equal(270.0, FindDescendants<RenderClipPath>(harness.RenderView).First().Size.Width);
        // The content section paints the translucent light dialog fill.
        Assert.Contains(FindDescendants<RenderColoredBox>(harness.RenderView), box =>
            box.Color == Color.FromUInt32(0xCCF2F2F2));
    }

    [Fact]
    public void AlertDialog_DividerSpansFullWidthWithSeparatorColorAndThickness()
    {
        using var harness = new RenderHarness(Wrap(new CupertinoAlertDialog(
            title: new Text("Title"),
            actions:
            [
                new CupertinoDialogAction(new Text("One"), onPressed: () => { }),
                new CupertinoDialogAction(new Text("Two"), onPressed: () => { }),
            ])));
        harness.Pump(new Size(600, 600));

        Color separator = Color.FromArgb(73, 60, 60, 67);
        var dividers = FindDescendants<RenderColoredBox>(harness.RenderView)
            .Where(box => box.Color == separator)
            .ToList();
        Assert.NotEmpty(dividers);
        // The section divider spans the dialog width at the 0.3 hairline thickness.
        Assert.Contains(dividers, divider =>
            Math.Abs(divider.Size.Height - 0.3) < 0.001 && Math.Abs(divider.Size.Width - 270.0) < 0.001);
    }

    [Fact]
    public void DialogAction_DefaultDestructiveDisabledAndMergedStylesResolve()
    {
        using var harness = new RenderHarness(Wrap(new CupertinoAlertDialog(
            title: new Text("Title"),
            actions:
            [
                new CupertinoDialogAction(new Text("Plain"), onPressed: () => { }),
                new CupertinoDialogAction(new Text("Bold"), onPressed: () => { }, isDefaultAction: true),
                new CupertinoDialogAction(new Text("Delete"), onPressed: () => { }, isDestructiveAction: true),
                new CupertinoDialogAction(new Text("Disabled")),
            ])));
        harness.Pump(new Size(600, 600));

        RenderParagraph plain = FindParagraph(harness.RenderView, "Plain")!;
        Assert.Equal(16.8, plain.FontSize);
        Assert.Equal(CupertinoColors.SystemBlue.Color, ParagraphColor(plain));
        Assert.Equal(Avalonia.Media.FontWeight.SemiBold, FindParagraph(harness.RenderView, "Bold")!.FontWeight);
        Assert.Equal(
            CupertinoColors.SystemRed.Color,
            ParagraphColor(FindParagraph(harness.RenderView, "Delete")!));
        Color disabled = ParagraphColor(FindParagraph(harness.RenderView, "Disabled")!);
        Assert.InRange(disabled.A, (byte)127, (byte)128);
    }

    [Fact]
    public void ActionsLayout_TwoShortButtonsSitSideBySideTallLabelsStack()
    {
        using var sideBySide = new RenderHarness(Wrap(new CupertinoAlertDialog(
            title: new Text("Title"),
            actions:
            [
                new CupertinoDialogAction(new Text("No"), onPressed: () => { }),
                new CupertinoDialogAction(new Text("Yes"), onPressed: () => { }),
            ])));
        sideBySide.Pump(new Size(600, 600));
        RenderParagraph no = FindParagraph(sideBySide.RenderView, "No")!;
        RenderParagraph yes = FindParagraph(sideBySide.RenderView, "Yes")!;
        Assert.Equal(
            no.GetPaintOffsetToRoot().Y,
            yes.GetPaintOffsetToRoot().Y,
            precision: 1);
        Assert.True(yes.GetPaintOffsetToRoot().X > no.GetPaintOffsetToRoot().X);

        using var stacked = new RenderHarness(Wrap(new CupertinoAlertDialog(
            title: new Text("Title"),
            actions:
            [
                new CupertinoDialogAction(
                    new Text("This is a very long action label that cannot fit"),
                    onPressed: () => { }),
                new CupertinoDialogAction(new Text("Short"), onPressed: () => { }),
            ])));
        stacked.Pump(new Size(600, 600));
        RenderParagraph longLabel = FindParagraph(
            stacked.RenderView, "This is a very long action label that cannot fit")!;
        RenderParagraph shortLabel = FindParagraph(stacked.RenderView, "Short")!;
        Assert.True(shortLabel.GetPaintOffsetToRoot().Y > longLabel.GetPaintOffsetToRoot().Y);
    }

    [Fact]
    public void PriorityColumn_SqueezedActionsSectionKeepsMinimumHeight()
    {
        using var harness = new RenderHarness(Wrap(new CupertinoAlertDialog(
            title: new Text("Title"),
            content: new Column(children:
            [
                new SizedBox(width: 100, height: 400),
            ]),
            actions:
            [
                new CupertinoDialogAction(new Text("One"), onPressed: () => { }),
                new CupertinoDialogAction(new Text("Two long enough to stack the pair"), onPressed: () => { }),
                new CupertinoDialogAction(new Text("Three"), onPressed: () => { }),
            ])));
        harness.Pump(new Size(600, 400));

        var priorityColumn = Assert.IsType<RenderPriorityColumn>(
            FindDescendants<RenderFlex>(harness.RenderView).First(flex => flex is RenderPriorityColumn));
        RenderBox? top = null;
        RenderBox? bottom = null;
        priorityColumn.VisitChildren(child =>
        {
            if (top is null) top = (RenderBox)child;
            else bottom ??= (RenderBox)child;
        });
        // Bottom = section divider (0.3) + the squeezed actions viewport at 67.8.
        Assert.Equal(67.8 + 0.3, bottom!.Size.Height, precision: 1);
    }

    [Fact]
    public void SlidingTap_HighlightsOnDownSlidesBetweenButtonsAndConfirmsOnRelease()
    {
        string? confirmed = null;
        using var harness = new RenderHarness(Wrap(new CupertinoAlertDialog(
            title: new Text("Title"),
            actions:
            [
                new CupertinoDialogAction(new Text("One"), onPressed: () => confirmed = "One"),
                new CupertinoDialogAction(new Text("Two"), onPressed: () => confirmed = "Two"),
            ])));
        harness.Pump(new Size(600, 600));

        Point onePosition = CenterOf(FindParagraph(harness.RenderView, "One")!);
        Point twoPosition = CenterOf(FindParagraph(harness.RenderView, "Two")!);
        var binding = GestureBinding.Instance;
        binding.HandlePointerEvent(harness.RenderView, new PointerDownEvent(
            pointer: 1, kind: PointerDeviceKind.Touch, position: onePosition,
            buttons: PointerButtons.Primary, timestampUtc: DateTime.UtcNow));
        harness.Pump(new Size(600, 600));
        // The pressed fill paints on the very next frame.
        Assert.Contains(FindDescendants<RenderColoredBox>(harness.RenderView), box =>
            box.Color == Color.FromUInt32(0xFFE1E1E1));

        binding.HandlePointerEvent(harness.RenderView, new PointerMoveEvent(
            pointer: 1, kind: PointerDeviceKind.Touch, position: twoPosition,
            buttons: PointerButtons.Primary, down: true, timestampUtc: DateTime.UtcNow));
        binding.HandlePointerEvent(harness.RenderView, new PointerUpEvent(
            pointer: 1, kind: PointerDeviceKind.Touch, position: twoPosition,
            buttons: PointerButtons.None, timestampUtc: DateTime.UtcNow));
        Assert.Equal("Two", confirmed);
    }

    [Fact]
    public void SlidingTap_DisabledActionDoesNotHighlightOrConfirm()
    {
        bool confirmed = false;
        using var harness = new RenderHarness(Wrap(new CupertinoAlertDialog(
            title: new Text("Title"),
            actions:
            [
                new CupertinoDialogAction(new Text("Disabled")),
                new CupertinoDialogAction(new Text("Enabled"), onPressed: () => confirmed = true),
            ])));
        harness.Pump(new Size(600, 600));

        Point disabledPosition = CenterOf(FindParagraph(harness.RenderView, "Disabled")!);
        var binding = GestureBinding.Instance;
        binding.HandlePointerEvent(harness.RenderView, new PointerDownEvent(
            pointer: 1, kind: PointerDeviceKind.Touch, position: disabledPosition,
            buttons: PointerButtons.Primary, timestampUtc: DateTime.UtcNow));
        harness.Pump(new Size(600, 600));
        Assert.DoesNotContain(FindDescendants<RenderColoredBox>(harness.RenderView), box =>
            box.Color == Color.FromUInt32(0xFFE1E1E1));

        binding.HandlePointerEvent(harness.RenderView, new PointerUpEvent(
            pointer: 1, kind: PointerDeviceKind.Touch, position: disabledPosition,
            buttons: PointerButtons.None, timestampUtc: DateTime.UtcNow));
        Assert.False(confirmed);
    }

    [Fact]
    public void AlertDialog_SemanticsExposeAlertRoleRouteLabelAndButtons()
    {
        using var harness = new RenderHarness(Wrap(new CupertinoAlertDialog(
            title: new Text("The Title"),
            content: new Text("Content"),
            actions:
            [
                new CupertinoDialogAction(new Text("Cancel"), onPressed: () => { }),
                new CupertinoDialogAction(new Text("OK"), onPressed: () => { }),
            ])));
        SemanticsNode? semantics = harness.PumpAndGetSemantics(new Size(600, 600));

        SemanticsNode alert = FindSemantics(semantics, node =>
            node.Label == "Alert"
            && node.Flags.HasFlag(SemanticsFlags.ScopesRoute)
            && node.Flags.HasFlag(SemanticsFlags.NamesRoute))!;
        Assert.NotNull(alert);
        Assert.NotNull(FindSemantics(semantics, node =>
            node.Label == "OK" && node.Flags.HasFlag(SemanticsFlags.IsButton)));
    }

    [Fact]
    public void AccessibilityTextScale_WidensDialogTo310()
    {
        using var harness = new RenderHarness(Wrap(
            new CupertinoAlertDialog(
                title: new Text("Title"),
                actions: [new CupertinoDialogAction(new Text("OK"), onPressed: () => { })]),
            textScaleFactor: 3.0));
        harness.Pump(new Size(600, 600));

        Assert.Equal(310.0, FindDescendants<RenderClipPath>(harness.RenderView).First().Size.Width);
    }

    [Fact]
    public async Task ShowCupertinoDialog_BarrierNotDismissibleByDefault()
    {
        BuildContext captured = default;
        using var harness = new RenderHarness(Wrap(new Navigator(new BuilderPageRoute(context =>
            new CaptureContext(value => captured = value, new Text("Underlying"))))));
        harness.Pump(new Size(600, 600));

        Task<string?> result = CupertinoDialogs.ShowCupertinoDialog<string>(
            captured,
            _ => new CupertinoAlertDialog(
                title: new Text("Alert body"),
                actions: [new CupertinoDialogAction(new Text("OK"), onPressed: () => { })]));
        PumpSpring();
        SemanticsNode? semantics = harness.PumpAndGetSemantics(new Size(600, 600));
        Assert.NotNull(FindParagraph(harness.RenderView, "Alert body"));

        // The default barrier is not dismissible: no dismiss action reaches the barrier node.
        SemanticsNode? barrier = FindSemantics(semantics, node =>
            node.Actions.HasFlag(SemanticsActions.Dismiss));
        Assert.Null(barrier);
        Assert.False(result.IsCompleted);

        Navigator.Of(captured, rootNavigator: true).Pop("done");
        Assert.Equal("done", await result);
    }

    [Fact]
    public void ShowCupertinoDialog_EntranceScalesFromOnePointThreeAndExitOnlyFades()
    {
        BuildContext captured = default;
        using var harness = new RenderHarness(Wrap(new Navigator(new BuilderPageRoute(context =>
            new CaptureContext(value => captured = value, new Text("Underlying"))))));
        harness.Pump(new Size(600, 600));

        CupertinoDialogs.ShowCupertinoDialog<string>(
            captured,
            _ => new CupertinoAlertDialog(title: new Text("Springy")));
        // One micro-frame in: the scale transition is still near its 1.3 starting value.
        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.001));
        harness.Pump(new Size(600, 600));
        Assert.NotEmpty(FindDescendants<RenderTransform>(harness.RenderView));

        PumpSpring();
        harness.Pump(new Size(600, 600));
        Assert.NotNull(FindParagraph(harness.RenderView, "Springy"));

        Navigator.Of(captured, rootNavigator: true).Pop();
        now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.001));
        harness.Pump(new Size(600, 600));
        // Exit runs fade-only: the reverse-status transition drops the scale wrapper.
        Assert.NotNull(FindParagraph(harness.RenderView, "Springy"));
        PumpSpring();
        harness.Pump(new Size(600, 600));
        Assert.Null(FindParagraph(harness.RenderView, "Springy"));
    }

    [Fact]
    public void ShowCupertinoModalPopup_ResolvesDarkBarrierAndForwardsRouteOptions()
    {
        BuildContext captured = default;
        Widget navigator = new Navigator(new BuilderPageRoute(context =>
            new CaptureContext(value => captured = value, new Text("Underlying"))));
        using var harness = new RenderHarness(Wrap(new CupertinoTheme(
            new CupertinoThemeData(brightness: PlatformBrightness.Dark),
            navigator)));
        harness.Pump(new Size(600, 600));

        _ = CupertinoDialogs.ShowCupertinoModalPopup<string>(
            captured,
            _ => new Text("Popup"),
            barrierDismissible: false,
            semanticsDismissible: true,
            routeSettings: new RouteSettings(Name: "/popup"),
            anchorPoint: new Point(500.0, 0.0),
            requestFocus: false);

        var route = Assert.IsType<CupertinoModalPopupRoute<string>>(
            Navigator.Of(captured).CurrentRoute);
        Assert.Equal(Color.FromUInt32(0x7A000000), route.BarrierColor);
        Assert.False(route.BarrierDismissible);
        Assert.True(route.SemanticsDismissible);
        Assert.Equal("/popup", route.Settings.Name);
        Assert.Equal(new Point(500.0, 0.0), route.AnchorPoint);
        Assert.False(route.RequestFocus);
    }

    [Fact]
    public void ShowCupertinoModalPopup_UsesRootNavigatorByDefaultAndCanTargetNestedNavigator()
    {
        BuildContext rootContext = default;
        BuildContext nestedContext = default;
        var rootRoute = new BuilderPageRoute(context =>
        {
            rootContext = context;
            return new Navigator(new BuilderPageRoute(nestedRouteContext =>
            {
                nestedContext = nestedRouteContext;
                return new Text("Nested");
            }));
        });
        using var harness = new RenderHarness(Wrap(new Navigator(rootRoute)));
        harness.Pump(new Size(600, 600));

        _ = CupertinoDialogs.ShowCupertinoModalPopup<object?>(
            nestedContext,
            _ => new Text("Root popup"));
        Assert.IsType<CupertinoModalPopupRoute<object?>>(Navigator.Of(rootContext).CurrentRoute);
        Assert.IsNotType<CupertinoModalPopupRoute<object?>>(Navigator.Of(nestedContext).CurrentRoute);

        Navigator.Of(rootContext).Pop();
        PumpSpring();
        harness.Pump(new Size(600, 600));

        _ = CupertinoDialogs.ShowCupertinoModalPopup<object?>(
            nestedContext,
            _ => new Text("Nested popup"),
            useRootNavigator: false);
        Assert.IsType<CupertinoModalPopupRoute<object?>>(Navigator.Of(nestedContext).CurrentRoute);
    }

    private static Widget Wrap(Widget child, double textScaleFactor = 1.0) =>
        new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                new MediaQueryData(Size: new Size(600, 600), TextScaleFactor: textScaleFactor),
                new Localizations(
                    locale: new Locale("en", "US"),
                    delegates:
                    [
                        DefaultWidgetsLocalizations.Delegate,
                        DefaultCupertinoLocalizations.Delegate,
                    ],
                    child: child)));

    private static void PumpSpring()
    {
        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.5));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1.0));
    }

    private static Color ParagraphColor(RenderParagraph paragraph) =>
        ((Avalonia.Media.ISolidColorBrush)paragraph.Foreground).Color;

    private static Point CenterOf(RenderBox box)
    {
        Point origin = box.GetPaintOffsetToRoot();
        return new Point(origin.X + (box.Size.Width / 2), origin.Y + (box.Size.Height / 2));
    }

    private static RenderParagraph? FindParagraph(RenderObject? root, string text) =>
        FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.PlainText == text);

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
        foreach (SemanticsNode child in node.Children)
        {
            SemanticsNode? result = FindSemantics(child, predicate);
            if (result is not null) return result;
        }

        return null;
    }

    private sealed class CaptureContext : StatelessWidget
    {
        private readonly Action<BuildContext> _capture;
        private readonly Widget _child;

        public CaptureContext(Action<BuildContext> capture, Widget child)
        {
            _capture = capture;
            _child = child;
        }

        public override Widget Build(BuildContext context)
        {
            _capture(context);
            return _child;
        }
    }

    private sealed class RenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _rootElement;
        private readonly PipelineOwner _pipeline;

        public RenderHarness(Widget rootWidget)
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

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget)
            {
                _renderView = renderView;
            }

            public override RenderObject? RenderObject => _child?.RenderObject;
            internal override Element? RenderObjectAttachingChild => _child;

            protected override void OnMount()
            {
                base.OnMount();
                Rebuild();
            }

            internal override void Rebuild()
            {
                Dirty = false;
                _child = UpdateChild(_child, Widget, Slot);
            }

            internal override void Update(Widget newWidget)
            {
                base.Update(newWidget);
                Rebuild();
            }

            internal override void ForgetChild(Element child)
            {
                if (ReferenceEquals(_child, child)) _child = null;
            }

            internal override void VisitChildren(Action<Element> visitor)
            {
                if (_child is not null) visitor(_child);
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
                if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null;
            }

            internal override void Unmount()
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
