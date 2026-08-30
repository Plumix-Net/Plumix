using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialTooltipTests
{
    [Fact]
    public void Tooltip_ValidatesOnlyFlutterConstructorAssertions()
    {
        Assert.Throws<ArgumentException>(() => new Tooltip(
            message: "tip",
            height: 24,
            constraints: new BoxConstraints(MinHeight: 24)));
        Assert.Throws<ArgumentException>(() => new TooltipThemeData(
            Height: 24,
            Constraints: new BoxConstraints(MinHeight: 24)));

        var tooltip = new Tooltip(
            message: "tip",
            verticalOffset: -1,
            waitDuration: TimeSpan.FromMilliseconds(-1));
        Assert.Equal(-1, tooltip.VerticalOffset);
        Assert.Equal(TimeSpan.FromMilliseconds(-1), tooltip.WaitDuration);
    }

    [Fact]
    public void Tooltip_RequiresExactlyOneOfMessageAndRichMessage()
    {
        Assert.Throws<ArgumentException>(() => new Tooltip(child: new SizedBox()));
        Assert.Throws<ArgumentException>(() => new Tooltip(
            message: "tip",
            richMessage: new TextSpan(text: "tip"),
            child: new SizedBox()));
    }

    [Fact]
    public void Tooltip_RichMessage_RendersTheSuppliedSpanAndItsPlainSemanticsText()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                ThemeData.Light,
                new Tooltip(
                    richMessage: new TextSpan(
                        text: "Save ",
                        children:
                        [
                            new TextSpan(
                                text: "now",
                                style: new TextStyle(FontWeight: FontWeight.Bold)),
                        ]),
                    child: new SizedBox(width: 24, height: 24))));
        harness.Pump(new Size(200, 120));

        var state = harness.FindState<TooltipState>();
        Assert.True(state.EnsureTooltipVisible());
        Scheduler.PumpFrameForTests();
        harness.Pump(new Size(200, 120));

        RenderParagraph bubble = Assert.Single(
            FindDescendants<RenderParagraph>(harness.RenderView),
            paragraph => paragraph.PlainText == "Save now");
        var root = Assert.IsType<TextSpan>(bubble.Text);
        InlineSpan supplied = Assert.Single(root.Children!);
        Assert.Equal("Save now", supplied.ToPlainText());
    }

    [Fact]
    public void Tooltip_PlainAndRichMessagesResolveSourcePointerDefaults()
    {
        using var plainHarness = new WidgetRenderHarness(
            new Theme(
                ThemeData.Light,
                new Tooltip(message: "Plain", child: new SizedBox(width: 24, height: 24))));
        plainHarness.Pump(new Size(160, 80));
        RawTooltip plain = Assert.Single(plainHarness.FindWidgets<RawTooltip>());
        Assert.True(plain.IgnorePointer);
        Assert.Equal(MouseCursor.Defer, Assert.Single(plainHarness.FindWidgets<MouseRegion>()).Cursor);

        using var richHarness = new WidgetRenderHarness(
            new Theme(
                ThemeData.Light,
                new Tooltip(
                    richMessage: new TextSpan(text: "Rich"),
                    child: new SizedBox(width: 24, height: 24))));
        richHarness.Pump(new Size(160, 80));
        RawTooltip rich = Assert.Single(richHarness.FindWidgets<RawTooltip>());
        Assert.False(rich.IgnorePointer);
    }

    [Fact]
    public void TooltipThemeData_CopyLerpDiagnosticsAndInheritedCaptureMatchSource()
    {
        var shape = new ShapeDecoration(new StadiumBorder(), Color: Colors.Teal);
        var source = new TooltipThemeData(
            Height: 20,
            Padding: EdgeInsetsGeometry.DirectionalOnly(start: 4, end: 12),
            Margin: EdgeInsetsGeometry.All(3),
            VerticalOffset: 6,
            PreferBelow: false,
            ExcludeFromSemantics: true,
            Decoration: shape,
            TextStyle: new TextStyle(Color: Colors.Orange, FontSize: 10),
            TextAlign: TextAlign.Center,
            WaitDuration: TimeSpan.FromMilliseconds(100),
            ShowDuration: TimeSpan.FromMilliseconds(200),
            ExitDuration: TimeSpan.FromMilliseconds(300),
            TriggerMode: TooltipTriggerMode.Tap,
            EnableFeedback: true);

        TooltipThemeData copy = source.CopyWith(verticalOffset: 10);
        Assert.Equal(10, copy.VerticalOffset);
        Assert.Same(shape, copy.Decoration);
        Assert.Null(copy.ExitDuration);

        Assert.Null(TooltipThemeData.Lerp(null, null, 0.5));
        Assert.Same(source, TooltipThemeData.Lerp(source, source, 0.5));
        TooltipThemeData midpoint = TooltipThemeData.Lerp(
            source,
            new TooltipThemeData(Height: 40, VerticalOffset: 14, PreferBelow: true),
            0.5)!;
        Assert.Equal(30, midpoint.Height);
        Assert.Equal(10, midpoint.VerticalOffset);
        Assert.True(midpoint.PreferBelow);
        Assert.Null(midpoint.WaitDuration);
        Assert.Null(midpoint.ShowDuration);
        Assert.Null(midpoint.ExitDuration);
        Assert.Null(midpoint.TriggerMode);
        Assert.Null(midpoint.EnableFeedback);

        var defaultDiagnostics = new DiagnosticPropertiesBuilder();
        new TooltipThemeData().DebugFillProperties(defaultDiagnostics);
        Assert.DoesNotContain(
            defaultDiagnostics.Properties,
            node => !node.IsFiltered(DiagnosticLevel.Info));

        var theme = new TooltipTheme(source, new SizedBox());
        Assert.IsAssignableFrom<InheritedTheme>(theme);
        var wrapped = Assert.IsType<TooltipTheme>(theme.Wrap(default, new Text("wrapped")));
        Assert.Same(source, wrapped.Data);
    }

    [Fact]
    public void Tooltip_UsesDirectionalInsetsAndArbitraryDecoration()
    {
        var decoration = new ShapeDecoration(new StadiumBorder(), Color: Colors.Teal);
        using var harness = new WidgetRenderHarness(
            new Theme(
                ThemeData.Light,
                new Directionality(
                    TextDirection.Rtl,
                    new Tooltip(
                        message: "Directional",
                        decoration: decoration,
                        padding: EdgeInsetsGeometry.DirectionalOnly(start: 12, top: 3, end: 2, bottom: 4),
                        margin: EdgeInsetsGeometry.DirectionalOnly(start: 7, end: 1),
                        child: new SizedBox(width: 24, height: 24)))));
        harness.Pump(new Size(220, 100));
        Assert.True(harness.FindState<TooltipState>().EnsureTooltipVisible());
        harness.Pump(new Size(220, 100));

        Assert.Contains(
            FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.DecorationValue is ShapeDecoration
            {
                Color: var color,
                Shape: StadiumBorder,
            } && color == Colors.Teal);
        Assert.Contains(
            FindDescendants<RenderPadding>(harness.RenderView),
            padding => padding.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr)
                == new Thickness(2, 3, 12, 4));
        Assert.Contains(
            FindDescendants<RenderPadding>(harness.RenderView),
            padding => padding.Padding.Resolve(padding.TextDirection ?? TextDirection.Ltr)
                == new Thickness(1, 0, 7, 0));
    }

    [Fact]
    public void Tooltip_ShowEmitsTooltipSemanticEventAndSourceDiagnostics()
    {
        SemanticsService.ResetForTests();
        try
        {
            TooltipSemanticEvent? received = null;
            SemanticsService.SemanticsEventRequested += semanticsEvent =>
            {
                received = semanticsEvent as TooltipSemanticEvent;
            };
            var tooltip = new Tooltip(message: "Semantic event", child: new SizedBox(width: 24, height: 24));

            // `debugFillProperties` is an assert-only body in Dart, so it fills nothing outside a
            // debug build; the semantics event below is emitted in every build.
            if (Constants.KDebugMode)
            {
                var diagnostics = new DiagnosticPropertiesBuilder();
                tooltip.DebugFillProperties(diagnostics);
                DiagnosticsNode diagnostic = Assert.Single(
                    diagnostics.Properties,
                    node => !node.IsFiltered(DiagnosticLevel.Info));
                Assert.Equal("message", diagnostic.Name);
                Assert.False(diagnostic.ShowName);
                Assert.Equal("\"Semantic event\"", diagnostic.ToDescription());
                Assert.Equal("\"Semantic event\"", diagnostic.ToString());

                var richDiagnostics = new DiagnosticPropertiesBuilder();
                new Tooltip(richMessage: new TextSpan(text: "Rich diagnostic"))
                    .DebugFillProperties(richDiagnostics);
                DiagnosticsNode richDiagnostic = Assert.Single(
                    richDiagnostics.Properties,
                    node => !node.IsFiltered(DiagnosticLevel.Info));
                Assert.Equal("richMessage", richDiagnostic.Name);
                Assert.Equal("\"Rich diagnostic\"", richDiagnostic.ToDescription());
            }

            using var harness = new WidgetRenderHarness(new Theme(ThemeData.Light, tooltip));
            harness.Pump(new Size(160, 80));
            Assert.True(harness.FindState<TooltipState>().EnsureTooltipVisible());
            Assert.Equal("Semantic event", Assert.IsType<TooltipSemanticEvent>(received).Message);
        }
        finally
        {
            SemanticsService.ResetForTests();
        }
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
    public void TooltipVisibility_DisablesProgrammaticPointerAndSemanticTooltipOutput()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                ThemeData.Light,
                new TooltipVisibility(
                    visible: false,
                    child: new Tooltip(message: "Hidden tip", child: new SizedBox(width: 24, height: 24)))));

        harness.Pump(new Size(120, 60));

        var state = harness.FindState<TooltipState>();
        Assert.False(state.EnsureTooltipVisible());
        Assert.NotNull(FindTooltipListener(harness.RenderView));
        Assert.Empty(harness.FindWidgets<RawTooltip>());
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.2));
        harness.Pump(new Size(120, 60));

        Assert.Null(FindParagraph(harness.RenderView, "Hidden tip"));
        Assert.Null(
            FindSemantics(
                harness.PumpAndGetSemantics(new Size(120, 60)),
                node => node.Tooltip == "Hidden tip"));
    }

    [Fact]
    public void TooltipVisibility_ClosestScopeWins()
    {
        using var harness = new WidgetRenderHarness(
            new Theme(
                ThemeData.Light,
                new TooltipVisibility(
                    visible: false,
                    child: new TooltipVisibility(
                        visible: true,
                        child: new Tooltip(message: "Inner tip", child: new SizedBox(width: 24, height: 24))))));

        harness.Pump(new Size(120, 60));

        Assert.True(harness.FindState<TooltipState>().EnsureTooltipVisible());
        harness.Pump(new Size(120, 60));
        Assert.NotNull(FindParagraph(harness.RenderView, "Inner tip"));
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
            AnimationPump.Prime();
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.20));
            harness.Pump(new Size(160, 80));
            Assert.Null(FindParagraph(harness.RenderView, "Delayed tip"));

            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(clock + 0.40));
            harness.Pump(new Size(160, 80));
            Assert.NotNull(FindParagraph(harness.RenderView, "Delayed tip"));
            Assert.Equal(0, triggered);

            listener = FindTooltipListener(harness.RenderView);
            listener!.HandleEvent(PointerExit(1), new BoxHitTestEntry(listener, new Point(100, 5)));
            clock = Scheduler.CurrentSeconds;
            AnimationPump.Prime();
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

        Assert.NotNull(FindSemantics(semantics, node => node.Tooltip == "Semantic tip"));
        Assert.Null(FindParagraph(harness.RenderView, "Semantic tip"));
    }

    [Fact]
    public void Tooltip_PositionPolicyFlipsAboveAndCustomDelegateReceivesResolvedValues()
    {
        TooltipPositionContext? received = null;
        using var harness = new WidgetRenderHarness(
            new Theme(
                ThemeData.Light,
                new Align(
                    alignment: Alignment.BottomCenter,
                    child: new Tooltip(
                        message: "Positioned tip",
                        constraints: new BoxConstraints(
                            MinWidth: 50,
                            MaxWidth: 50,
                            MinHeight: 20,
                            MaxHeight: 20),
                        verticalOffset: 10,
                        preferBelow: true,
                        positionDelegate: context =>
                        {
                            received = context;
                            return RawTooltipPositionLayoutDelegate.PositionDependentBox(
                                context.OverlaySize,
                                context.TooltipSize,
                                context.Target,
                                context.PreferBelow,
                                context.VerticalOffset);
                        },
                        child: new SizedBox(width: 20, height: 20)))));

        harness.Pump(new Size(200, 100));
        Assert.True(harness.FindState<TooltipState>().EnsureTooltipVisible());
        harness.Pump(new Size(200, 100));

        Assert.NotNull(received);
        Assert.Equal(10, received!.VerticalOffset);
        Assert.True(received.PreferBelow);
        Assert.Equal(new Point(100, 90), received.Target);
        RenderCustomSingleChildLayoutBox layout =
            Assert.Single(FindDescendants<RenderCustomSingleChildLayoutBox>(harness.RenderView));
        Assert.Equal(
            60,
            Assert.IsType<BoxParentData>(layout.Child!.parentData).offset.Y,
            precision: 3);
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
            IReadOnlyList<TooltipState> tooltipStates = harness.FindStates<TooltipState>();
            Assert.Equal(2, tooltipStates.Count);
            foreach (TooltipState state in tooltipStates)
            {
                Assert.True(state.EnsureTooltipVisible());
            }
            harness.Pump(new Size(120, 60));
            List<RenderParagraph> paragraphs = FindDescendants<RenderParagraph>(harness.RenderView);
            Assert.Contains(paragraphs, paragraph => paragraph.PlainText == "One");
            Assert.Contains(paragraphs, paragraph => paragraph.PlainText == "Two");

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
        return FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.PlainText == text);
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
            _rootElement = new HarnessRootElement(
                RenderView,
                new Directionality(TextDirection.Ltr, child: Overlay.Wrap(rootWidget)));
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
            return _pipeline.SemanticsOwner!.RootNode;
        }

        public T FindState<T>() where T : State => FindStates<T>().Single();

        public IReadOnlyList<T> FindStates<T>() where T : State
        {
            var states = new List<T>();
            CollectStates(_rootElement, states);
            return states;
        }

        public IReadOnlyList<T> FindWidgets<T>() where T : Widget
        {
            var widgets = new List<T>();
            CollectWidgets(_rootElement, widgets);
            return widgets;
        }

        public void Dispose() => _rootElement.Unmount();

        private static void CollectStates<T>(Element element, List<T> states) where T : State
        {
            if (element is StatefulElement stateful && stateful.State is T state) states.Add(state);
            element.VisitChildren(child => CollectStates(child, states));
        }

        private static void CollectWidgets<T>(Element element, List<T> widgets) where T : Widget
        {
            if (element.Widget is T widget)
            {
                widgets.Add(widget);
            }

            element.VisitChildren(child => CollectWidgets(child, widgets));
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
