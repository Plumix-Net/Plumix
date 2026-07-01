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

        var semantics = harness.PumpAndGetSemantics(new Size(360, 240));
        var header = FindNodes(semantics!, node => node.Flags.HasFlag(SemanticsFlags.HasExpandedState)).Single();
        Assert.False(header.Flags.HasFlag(SemanticsFlags.IsExpanded));
        Assert.Null(FindParagraphByText(harness.RenderView, "Controlled body"));

        Assert.True(harness.PerformSemanticsAction(header.Id, SemanticsActions.Tap));
        semantics = harness.PumpAndGetSemantics(new Size(360, 240));

        header = FindNodes(semantics!, node => node.Flags.HasFlag(SemanticsFlags.HasExpandedState)).Single();
        Assert.True(header.Flags.HasFlag(SemanticsFlags.IsExpanded));
        Assert.NotNull(FindParagraphByText(harness.RenderView, "Controlled body"));
        Assert.Contains(FindDescendants<RenderAlign>(harness.RenderView), align => align.HeightFactor is >= 0 and < 1);

        var now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.25));
        harness.Pump(new Size(360, 240));
        Assert.Contains(FindDescendants<RenderAlign>(harness.RenderView), align => align.HeightFactor is >= 0.99);
    }

    [Fact]
    public void ExpansionPanelList_IconOnlyMode_DoesNotToggleFromHeaderArea()
    {
        var calls = 0;
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

        var semantics = harness.PumpAndGetSemantics(new Size(360, 280));
        var firstHeader = FindNodes(
            semantics!,
            node => node.Flags.HasFlag(SemanticsFlags.HasExpandedState)).First();
        Assert.True(harness.PerformSemanticsAction(firstHeader.Id, SemanticsActions.Tap));

        var now = Scheduler.CurrentSeconds;
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

        var semantics = harness.PumpAndGetSemantics(new Size(360, 320));
        var headers = FindNodes(semantics!, node => node.Flags.HasFlag(SemanticsFlags.HasExpandedState)).ToArray();
        Assert.Equal(2, headers.Length);
        Assert.True(headers[0].Flags.HasFlag(SemanticsFlags.IsExpanded));
        Assert.False(headers[1].Flags.HasFlag(SemanticsFlags.IsExpanded));
        Assert.NotNull(FindParagraphByText(harness.RenderView, "Radio body one"));
        Assert.Null(FindParagraphByText(harness.RenderView, "Radio body two"));

        Assert.True(harness.PerformSemanticsAction(headers[1].Id, SemanticsActions.Tap));
        semantics = harness.PumpAndGetSemantics(new Size(360, 320));
        headers = FindNodes(semantics!, node => node.Flags.HasFlag(SemanticsFlags.HasExpandedState)).ToArray();

        Assert.Equal([(0, false), (1, true)], callbacks);
        Assert.False(headers[0].Flags.HasFlag(SemanticsFlags.IsExpanded));
        Assert.True(headers[1].Flags.HasFlag(SemanticsFlags.IsExpanded));
        Assert.NotNull(FindParagraphByText(harness.RenderView, "Radio body two"));
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

        var semantics = harness.PumpAndGetSemantics(new Size(360, 300));
        var openHeader = FindNodes(
            semantics!,
            node => node.Flags.HasFlag(SemanticsFlags.HasExpandedState)
                    && node.Flags.HasFlag(SemanticsFlags.IsExpanded)).Single();

        Assert.True(harness.PerformSemanticsAction(openHeader.Id, SemanticsActions.Tap));
        semantics = harness.PumpAndGetSemantics(new Size(360, 300));

        Assert.Equal([(0, false)], callbacks);
        Assert.DoesNotContain(
            FindNodes(semantics!, node => node.Flags.HasFlag(SemanticsFlags.HasExpandedState)),
            node => node.Flags.HasFlag(SemanticsFlags.IsExpanded));
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

        Assert.Contains(FindDescendants<RenderColoredBox>(harness.RenderView), box => box.Color == background);
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

    private static RenderParagraph? FindParagraphByText(RenderObject? root, string text)
    {
        return FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.Text == text);
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

    private static IEnumerable<SemanticsNode> FindNodes(
        SemanticsNode node,
        Func<SemanticsNode, bool> predicate)
    {
        if (predicate(node))
        {
            yield return node;
        }

        foreach (var child in node.Children)
        {
            foreach (var match in FindNodes(child, predicate))
            {
                yield return match;
            }
        }
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

        public bool PerformSemanticsAction(int nodeId, SemanticsActions action)
        {
            return _pipeline.SemanticsOwner.PerformAction(nodeId, action);
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
