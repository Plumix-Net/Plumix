using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialDesktopTextSelectionToolbarTests : IDisposable
{
    public MaterialDesktopTextSelectionToolbarTests()
    {
        Scheduler.ResetForTests();
        MouseCursorManager.ResetForTests();
    }

    public void Dispose()
    {
        MouseCursorManager.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void DesktopToolbar_ValidatesChildrenAndExposesFlutterConstants()
    {
        Assert.Throws<ArgumentException>(() => new DesktopTextSelectionToolbar(default, []));
        Assert.Equal(8.0, DesktopTextSelectionToolbar.ToolbarScreenPadding);
        Assert.Equal(222.0, DesktopTextSelectionToolbar.ToolbarWidth);
    }

    [Fact]
    public void LayoutDelegate_LoosensChildAndClampsOnlyPositiveViewportOverhang()
    {
        var constraints = new BoxConstraints(MinWidth: 80, MaxWidth: 300, MinHeight: 60, MaxHeight: 200);
        var layoutDelegate = new DesktopTextSelectionToolbarLayoutDelegate(new Point(260, 180));

        Assert.Equal(new BoxConstraints(MaxWidth: 300, MaxHeight: 200),
            layoutDelegate.GetConstraintsForChild(constraints));
        Assert.Equal(new Point(180, 150),
            layoutDelegate.GetPositionForChild(new Size(300, 200), new Size(120, 50)));

        var negativeAnchor = new DesktopTextSelectionToolbarLayoutDelegate(new Point(-12, -7));
        Assert.Equal(new Point(-12, -7),
            negativeAnchor.GetPositionForChild(new Size(300, 200), new Size(120, 50)));
        Assert.True(layoutDelegate.ShouldRelayout(
            new DesktopTextSelectionToolbarLayoutDelegate(new Point(20, 30))));
        Assert.False(layoutDelegate.ShouldRelayout(
            new DesktopTextSelectionToolbarLayoutDelegate(new Point(260, 180))));
    }

    [Fact]
    public void DesktopToolbar_AppliesSafePaddingCardSurfaceWidthAndBottomRightClamping()
    {
        using var harness = CreateHarness(
            ThemeData.Light,
            new DesktopTextSelectionToolbar(
                anchor: new Point(380, 280),
                children:
                [
                    new DesktopTextSelectionToolbarButton(() => { }, new Text("Copy")),
                    new DesktopTextSelectionToolbarButton(() => { }, new Text("Paste")),
                ]),
            padding: new Thickness(0, 12, 0, 0));

        harness.Pump(new Size(400, 300));

        var padding = Assert.Single(FindDescendants<RenderPadding>(harness.RenderView), value =>
            value.Padding == new Thickness(8, 20, 8, 8));
        Assert.Equal(new Size(400, 300), padding.Size);

        var customLayout = Assert.Single(FindDescendants<RenderCustomSingleChildLayoutBox>(harness.RenderView));
        Assert.Equal(new Size(384, 272), customLayout.Size);
        Assert.Equal(new Point(162, 176), ((BoxParentData)customLayout.Child!.parentData!).offset);

        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView), box =>
            box.AdditionalConstraints == BoxConstraints.TightFor(width: 222));
        Assert.Contains(FindDescendants<RenderClipRRect>(harness.RenderView), clip =>
            clip.BorderRadius == BorderRadius.Circular(7));
        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView), box =>
            box.Decoration.Color == ThemeData.Light.CardColor
            && box.Decoration.EffectiveBorderRadius == BorderRadius.Circular(7));
    }

    [Fact]
    public void DesktopToolbarButton_UsesFlutterGeometryTypographyCursorAndTapSemantics()
    {
        int taps = 0;
        using var harness = CreateHarness(
            ThemeData.Light,
            new Builder(context => DesktopTextSelectionToolbarButton.Text(
                context,
                () => taps++,
                "Copy a very long selection")));

        SemanticsNode? semantics = harness.PumpAndGetSemantics(new Size(222, 60));

        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView), box =>
            box.AdditionalConstraints == BoxConstraints.TightFor(width: double.PositiveInfinity));
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView), box =>
            box.AdditionalConstraints.MinWidth == 48
            && box.AdditionalConstraints.MinHeight == 36);
        Assert.Contains(FindDescendants<RenderPadding>(harness.RenderView), value =>
            value.Padding == new Thickness(20, 0, 20, 3));
        Assert.Contains(FindDescendants<RenderAlign>(harness.RenderView), align =>
            align.Alignment == Alignment.CenterLeft);

        RenderParagraph paragraph = Assert.Single(FindDescendants<RenderParagraph>(harness.RenderView), value =>
            value.Text == "Copy a very long selection");
        Assert.Equal(14.0, paragraph.FontSize);
        Assert.Equal(-0.15, paragraph.LetterSpacing);
        Assert.Equal(FontWeight.Normal, paragraph.FontWeight);
        Assert.Equal(TextOverflow.Ellipsis, paragraph.Overflow);
        Assert.Equal(Color.FromArgb(0xDE, 0x00, 0x00, 0x00),
            Assert.IsType<SolidColorBrush>(paragraph.Foreground).Color);

        SemanticsNode actionNode = Assert.IsType<SemanticsNode>(FindSemantics(
            semantics,
            node => node.Actions.HasFlag(SemanticsActions.Tap)));
        Assert.True(actionNode.PerformAction(SemanticsActions.Tap));
        Assert.Equal(1, taps);

        using IDisposable outerCursor = MouseCursorManager.PushCursor(SystemMouseCursors.Click);
        RenderPointerListener listener = Assert.Single(
            FindDescendants<RenderPointerListener>(harness.RenderView),
            value => value.OnPointerEnter is not null && value.OnPointerExit is not null);
        listener.HandleEvent(
            new PointerEnterEvent(
                1,
                PointerDeviceKind.Mouse,
                new Point(10, 10),
                PointerButtons.None,
                DateTime.UtcNow),
            new BoxHitTestEntry(listener, new Point(10, 10)));
        Assert.Equal(SystemMouseCursors.Basic, MouseCursorManager.CurrentCursor);
    }

    [Fact]
    public void DesktopToolbarButton_DarkThemeUsesWhiteTextAndNullCallbackIsDisabled()
    {
        using var harness = CreateHarness(
            ThemeData.Dark,
            new Builder(context => DesktopTextSelectionToolbarButton.Text(context, null, "Paste")));

        SemanticsNode? semantics = harness.PumpAndGetSemantics(new Size(222, 60));
        RenderParagraph paragraph = Assert.Single(FindDescendants<RenderParagraph>(harness.RenderView), value =>
            value.Text == "Paste");

        Assert.Equal(Colors.White, Assert.IsType<SolidColorBrush>(paragraph.Foreground).Color);
        Assert.Null(FindSemantics(semantics, node => node.Actions.HasFlag(SemanticsActions.Tap)));
    }

    [Fact]
    public void DesktopToolbarButton_CustomOnSurfaceColorOverridesPlatformFallback()
    {
        using var harness = CreateHarness(
            ThemeData.Light with { OnSurfaceColor = Colors.DarkOrange },
            new Builder(context => DesktopTextSelectionToolbarButton.Text(context, () => { }, "Custom")));

        harness.Pump(new Size(222, 60));
        RenderParagraph paragraph = Assert.Single(FindDescendants<RenderParagraph>(harness.RenderView), value =>
            value.Text == "Custom");

        Assert.Equal(Colors.DarkOrange, Assert.IsType<SolidColorBrush>(paragraph.Foreground).Color);
    }

    private static WidgetRenderHarness CreateHarness(
        ThemeData theme,
        Widget child,
        Thickness padding = default)
    {
        return new WidgetRenderHarness(
            new Theme(
                theme,
                new MediaQuery(
                    new MediaQueryData(Padding: padding),
                    new Directionality(TextDirection.Ltr, child))));
    }

    private static SemanticsNode? FindSemantics(SemanticsNode? node, Func<SemanticsNode, bool> predicate)
    {
        if (node is null)
        {
            return null;
        }

        if (predicate(node))
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
                _renderView.Child = (RenderBox)child;
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
