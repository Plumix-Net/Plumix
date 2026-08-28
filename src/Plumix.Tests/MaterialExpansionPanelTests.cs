using System;
using System.Collections.Generic;
using System.Linq;
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
public sealed class MaterialExpansionPanelTests : IDisposable
{
    public MaterialExpansionPanelTests()
    {
        Scheduler.ResetForTests();
        FocusManager.Instance.ResetForTests();
        GestureBinding.Instance.ResetForTests();
    }

    public void Dispose()
    {
        GestureBinding.Instance.ResetForTests();
        FocusManager.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void ExpansionPanelList_DefaultsAndValidation_MatchFlutterSurface()
    {
        var list = new ExpansionPanelList();

        Assert.Empty(list.Children);
        Assert.Equal(TimeSpan.FromMilliseconds(200), list.AnimationDuration);
        Assert.Equal(new Thickness(0, 16), list.ExpandedHeaderPadding);
        Assert.Equal(2, list.Elevation);
        Assert.Equal(16, list.MaterialGapSize);
        Assert.Null(list.DividerColor);
        Assert.Null(list.ExpandIconColor);

        Assert.Throws<ArgumentOutOfRangeException>(() => new ExpansionPanelList(elevation: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExpansionPanelList(elevation: 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExpansionPanelList(materialGapSize: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExpansionPanelList(animationDuration: TimeSpan.FromMilliseconds(-1)));
        Assert.Throws<ArgumentException>(() => ExpansionPanelList.Radio(
            children:
            [
                RadioPanel("duplicate", "One", "Body one"),
                RadioPanel("duplicate", "Two", "Body two"),
            ]));
    }

    [Fact]
    public void ExpansionPanelList_ControlledHeaderTap_RequestsAndRendersExpandedState()
    {
        using var harness = new WidgetRenderHarness(
            BuildThemed(new ControlledPanelHost(canTapOnHeader: true)));

        harness.Pump(new Size(360, 240));
        Assert.Equal(CrossFadeState.ShowFirst, Assert.Single(harness.FindWidgets<AnimatedCrossFade>()).CrossFadeState);

        TapParagraph(harness.RenderView, "Collapsed header", pointer: 810);
        harness.Pump(new Size(360, 240));

        Assert.Equal(CrossFadeState.ShowSecond, Assert.Single(harness.FindWidgets<AnimatedCrossFade>()).CrossFadeState);
        Assert.Equal(
            new Thickness(0, 16),
            Assert.Single(
                harness.FindWidgets<AnimatedContainer>(),
                container => container.Child is ConstrainedBox box && box.Constraints.MinHeight == 48.0).Margin);

        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.25));
        harness.Pump(new Size(360, 240));
        Assert.Equal(CrossFadeState.ShowSecond, Assert.Single(harness.FindWidgets<AnimatedCrossFade>()).CrossFadeState);
    }

    [Fact]
    public void ExpansionPanelList_IconOnlyMode_DoesNotToggleFromHeaderArea()
    {
        int calls = 0;
        using var harness = new WidgetRenderHarness(
            BuildThemed(new ExpansionPanelList(
                expansionCallback: (_, _) => calls++,
                children:
                [
                    new ExpansionPanel(
                        headerBuilder: (_, _) => new Text("Icon-only header"),
                        body: new Text("Icon-only body")),
                ])));

        harness.Pump(new Size(360, 160));
        Tap(harness.RenderView, new Point(80, 24), pointer: 811);
        Assert.Equal(0, calls);

        Tap(harness.RenderView, new Point(286, 24), pointer: 812);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void ExpansionPanelList_OpeningPanel_AnimatesMaterialGapToConfiguredSize()
    {
        using var harness = new WidgetRenderHarness(
            BuildThemed(new TwoPanelHost()));

        harness.Pump(new Size(360, 280));
        TapParagraph(harness.RenderView, "First gap panel", pointer: 813);
        harness.Pump(new Size(360, 280));

        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.10));
        harness.Pump(new Size(360, 280));
        Assert.Contains(
            FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => box.AdditionalConstraints.MinHeight is > 0 and < 16
                   && Math.Abs(box.AdditionalConstraints.MinHeight - box.AdditionalConstraints.MaxHeight) < 0.001);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.25));
        harness.Pump(new Size(360, 280));
        Assert.Contains(
            FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => Math.Abs(box.AdditionalConstraints.MinHeight - 16) < 0.001
                   && Math.Abs(box.AdditionalConstraints.MaxHeight - 16) < 0.001);
    }

    [Fact]
    public void ExpansionPanelListRadio_InitialValueAndSwitch_CallbackOrderMatchFlutter()
    {
        var callbacks = new List<(int Index, bool Expanded)>();
        using var harness = new WidgetRenderHarness(
            BuildThemed(ExpansionPanelList.Radio(
                initialOpenPanelValue: "one",
                expansionCallback: (index, expanded) => callbacks.Add((index, expanded)),
                children:
                [
                    RadioPanel("one", "Radio one", "Radio body one", canTapOnHeader: true),
                    RadioPanel("two", "Radio two", "Radio body two", canTapOnHeader: true),
                ])));

        harness.Pump(new Size(360, 320));
        Assert.Equal(
            [CrossFadeState.ShowSecond, CrossFadeState.ShowFirst],
            harness.FindWidgets<AnimatedCrossFade>().Select(fade => fade.CrossFadeState).ToArray());

        TapParagraph(harness.RenderView, "Radio two", pointer: 814);
        harness.Pump(new Size(360, 320));

        Assert.Equal([(0, false), (1, true)], callbacks);
        Assert.Equal(
            [CrossFadeState.ShowFirst, CrossFadeState.ShowSecond],
            harness.FindWidgets<AnimatedCrossFade>().Select(fade => fade.CrossFadeState).ToArray());
    }

    [Fact]
    public void ExpansionPanelListRadio_TappingOpenPanel_OnlyReportsItsCollapse()
    {
        var callbacks = new List<(int Index, bool Expanded)>();
        using var harness = new WidgetRenderHarness(
            BuildThemed(ExpansionPanelList.Radio(
                initialOpenPanelValue: 1,
                expansionCallback: (index, expanded) => callbacks.Add((index, expanded)),
                children:
                [
                    RadioPanel(1, "Open radio", "Open body", canTapOnHeader: true),
                    RadioPanel(2, "Closed radio", "Closed body", canTapOnHeader: true),
                ])));

        harness.Pump(new Size(360, 300));
        TapParagraph(harness.RenderView, "Open radio", pointer: 815);
        harness.Pump(new Size(360, 300));

        Assert.Equal([(0, false)], callbacks);
        Assert.All(
            harness.FindWidgets<AnimatedCrossFade>(),
            fade => Assert.Equal(CrossFadeState.ShowFirst, fade.CrossFadeState));
    }

    [Fact]
    public void ExpansionPanelList_CompositionAndInteractionColors_MatchFlutter()
    {
        var splash = Color.Parse("#FF006C4C");
        var highlight = Color.Parse("#FFFFB4AB");
        using var harness = new WidgetRenderHarness(
            BuildThemed(new ExpansionPanelList(
                animationDuration: TimeSpan.FromMilliseconds(800),
                children:
                [
                    new ExpansionPanel(
                        canTapOnHeader: true,
                        splashColor: splash,
                        highlightColor: highlight,
                        headerBuilder: (_, _) => new Text("Structured header"),
                        body: new Text("Structured body")),
                ])));

        harness.Pump(new Size(360, 200));

        var crossFade = Assert.Single(harness.FindWidgets<AnimatedCrossFade>());
        Assert.Equal(TimeSpan.FromMilliseconds(800), crossFade.Duration);
        Assert.Equal(CrossFadeState.ShowFirst, crossFade.CrossFadeState);
        Assert.Equal(Curves.FastOutSlowIn(0.5), crossFade.FirstCurve(0.3), precision: 6);
        Assert.Equal(Curves.FastOutSlowIn(0.5), crossFade.SecondCurve(0.7), precision: 6);
        Assert.Equal(Curves.FastOutSlowIn(0.5), crossFade.SizeCurve(0.5), precision: 6);

        var inkWell = Assert.Single(harness.FindWidgets<InkWell>(), ink => ink.SplashColor == splash);
        Assert.Equal(highlight, inkWell.HighlightColor);
        Assert.NotNull(inkWell.OnTap);

        var expandIcon = Assert.Single(harness.FindWidgets<ExpandIcon>());
        var iconIgnorePointer = Assert.Single(
            harness.FindWidgets<IgnorePointer>(),
            ignore => ReferenceEquals(ignore.Child, expandIcon));
        Assert.True(iconIgnorePointer.Ignoring);
        Assert.Equal(new Thickness(12.0), expandIcon.Padding);
        Assert.NotNull(expandIcon.OnPressed);

        var mergeable = Assert.Single(harness.FindWidgets<MergeableMaterial>());
        var slice = Assert.IsType<MaterialSlice>(Assert.Single(mergeable.Children));
        var column = Assert.IsType<Column>(slice.Child);
        Assert.IsType<MergeSemantics>(column.Children[0]);
        Assert.Same(crossFade, column.Children[1]);
    }

    [Fact]
    public void ExpansionPanelList_VisualOverridesApplyToSliceDividerIconAndGap()
    {
        var background = Color.Parse("#FFFFE8D6");
        var divider = Color.Parse("#FF006C4C");
        var icon = Color.Parse("#FF6750A4");
        using var harness = new WidgetRenderHarness(
            BuildThemed(new ExpansionPanelList(
                dividerColor: divider,
                expandIconColor: icon,
                elevation: 0,
                materialGapSize: 22,
                children:
                [
                    new ExpansionPanel(
                        isExpanded: true,
                        backgroundColor: background,
                        headerBuilder: (_, _) => new Text("Styled panel"),
                        body: new SizedBox(height: 20, child: new Text("Styled body"))),
                    new ExpansionPanel(
                        headerBuilder: (_, _) => new Text("Collapsed panel"),
                        body: new Text("Collapsed body")),
                ])));

        harness.Pump(new Size(360, 300));

        Assert.Contains(
            FindDescendants<RenderDecoratedBox>(harness.RenderView),
            box => box.Decoration.Color == background);
        var iconGlyph = FindParagraphByText(harness.RenderView, char.ConvertFromUtf32(Icons.ExpandMore.CodePoint));
        Assert.NotNull(iconGlyph);
        Assert.Equal(icon, Assert.IsType<SolidColorBrush>(iconGlyph!.Foreground).Color);
        Assert.Contains(
            FindDescendants<RenderConstrainedBox>(harness.RenderView),
            box => Math.Abs(box.AdditionalConstraints.MinHeight - 22) < 0.001
                   && Math.Abs(box.AdditionalConstraints.MaxHeight - 22) < 0.001);
    }

    private static ExpansionPanelRadio RadioPanel(
        object value,
        string header,
        string body,
        bool canTapOnHeader = false)
    {
        return new ExpansionPanelRadio(
            value: value,
            headerBuilder: (_, _) => new Text(header),
            body: new Text(body),
            canTapOnHeader: canTapOnHeader);
    }

    private static Widget BuildThemed(Widget child)
    {
        return new Theme(
            data: ThemeData.Light,
            child: new SizedBox(width: 320, child: child));
    }

    private static void Tap(RenderView renderView, Point position, int pointer)
    {
        var timestamp = DateTime.UtcNow;
        GestureBinding.Instance.HandlePointerEvent(
            renderView,
            new PointerDownEvent(
                pointer,
                PointerDeviceKind.Mouse,
                position,
                PointerButtons.Primary,
                timestamp));
        GestureBinding.Instance.HandlePointerEvent(
            renderView,
            new PointerUpEvent(
                pointer,
                PointerDeviceKind.Mouse,
                position,
                PointerButtons.None,
                timestamp.AddMilliseconds(20)));
    }

    private static void TapParagraph(RenderView renderView, string text, int pointer)
    {
        var paragraph = FindParagraphByText(renderView, text)
                        ?? throw new InvalidOperationException($"Paragraph '{text}' was not found.");
        Point offset = GlobalOffsetOf(paragraph);
        Tap(
            renderView,
            new Point(offset.X + (paragraph.Size.Width / 2.0), offset.Y + (paragraph.Size.Height / 2.0)),
            pointer);
    }

    private static Point GlobalOffsetOf(RenderObject renderObject)
    {
        var result = new Point();
        RenderObject? current = renderObject;
        while (current is not null)
        {
            if (current.parentData is BoxParentData parentData)
            {
                result = new Point(
                    result.X + parentData.offset.X,
                    result.Y + parentData.offset.Y);
            }

            current = current.Parent;
        }

        return result;
    }

    private static RenderParagraph? FindParagraphByText(RenderObject? root, string text)
    {
        return FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.PlainText == text);
    }

    private static IEnumerable<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var results = new List<T>();
        CollectDescendants(root, results);
        return results;
    }

    private static void CollectDescendants<T>(RenderObject? root, List<T> results) where T : RenderObject
    {
        if (root is null)
        {
            return;
        }

        if (root is T typed)
        {
            results.Add(typed);
        }

        root.VisitChildren(child => CollectDescendants(child, results));
    }

    private sealed class ControlledPanelHost(bool canTapOnHeader) : StatefulWidget
    {
        public bool CanTapOnHeader { get; } = canTapOnHeader;

        public override State CreateState() => new ControlledPanelHostState();

        private sealed class ControlledPanelHostState : State
        {
            private bool _expanded;

            private ControlledPanelHost CurrentWidget => (ControlledPanelHost)StateWidget;

            public override Widget Build(BuildContext context)
            {
                return new ExpansionPanelList(
                    expansionCallback: (_, value) => SetState(() => _expanded = value),
                    children:
                    [
                        new ExpansionPanel(
                            isExpanded: _expanded,
                            canTapOnHeader: CurrentWidget.CanTapOnHeader,
                            headerBuilder: (_, expanded) => new Text(expanded ? "Expanded header" : "Collapsed header"),
                            body: new SizedBox(height: 40, child: new Text("Controlled body"))),
                    ]);
            }
        }
    }

    private sealed class TwoPanelHost : StatefulWidget
    {
        public override State CreateState() => new TwoPanelHostState();

        private sealed class TwoPanelHostState : State
        {
            private bool _firstExpanded;

            public override Widget Build(BuildContext context)
            {
                return new ExpansionPanelList(
                    expansionCallback: (index, value) => SetState(() => _firstExpanded = index == 0 && value),
                    children:
                    [
                        new ExpansionPanel(
                            isExpanded: _firstExpanded,
                            canTapOnHeader: true,
                            headerBuilder: (_, _) => new Text("First gap panel"),
                            body: new Text("First gap body")),
                        new ExpansionPanel(
                            canTapOnHeader: true,
                            headerBuilder: (_, _) => new Text("Second gap panel"),
                            body: new Text("Second gap body")),
                    ]);
            }
        }
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
                new Directionality(TextDirection.Ltr, child: rootWidget));
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

        public IReadOnlyList<T> FindWidgets<T>() where T : Widget
        {
            var widgets = new List<T>();
            Visit(_rootElement);
            return widgets;

            void Visit(Element element)
            {
                if (element.Widget is T widget)
                {
                    widgets.Add(widget);
                }

                element.VisitChildren(Visit);
            }
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
                if (ReferenceEquals(_child, child))
                {
                    _child = null;
                }
            }

            internal override void VisitChildren(Action<Element> visitor)
            {
                if (_child is not null)
                {
                    visitor(_child);
                }
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
                _renderView.Child = Assert.IsAssignableFrom<RenderBox>(child);
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(_renderView.Child, child))
                {
                    _renderView.Child = null;
                }
            }
        }
    }
}
