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
public sealed class MaterialTextSelectionControlsTests : IDisposable
{
    public MaterialTextSelectionControlsTests()
    {
        Scheduler.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
    }

    [Fact]
    public void MaterialControls_HandleSizeIgnoresLineHeightAndAnchorsMatchFlutter()
    {
        TextSelectionControls controls = MaterialTextSelectionControls.Instance;

        Assert.Equal(new Size(22, 22), controls.GetHandleSize(10));
        Assert.Equal(new Size(22, 22), controls.GetHandleSize(40));
        Assert.Equal(new Point(11, -4), controls.GetHandleAnchor(TextSelectionHandleType.Collapsed, 10));
        Assert.Equal(new Point(22, 0), controls.GetHandleAnchor(TextSelectionHandleType.Left, 10));
        Assert.Equal(default, controls.GetHandleAnchor(TextSelectionHandleType.Right, 10));
        Assert.Equal(20.0, MaterialTextSelectionControls.ToolbarContentDistanceBelow);
        Assert.Equal(8.0, MaterialTextSelectionControls.ToolbarContentDistance);
    }

    [Theory]
    [InlineData("", 0, 0, false)]
    [InlineData("123", 1, 1, true)]
    [InlineData("123", 1, 2, true)]
    [InlineData("123", 0, 3, false)]
    public void MaterialControls_CanSelectAllFollowsAndroidRules(
        string text,
        int baseOffset,
        int extentOffset,
        bool expected)
    {
        var @delegate = new FakeSelectionDelegate(
            new TextEditingValue(text, new TextSelection(baseOffset, extentOffset)));

#pragma warning disable CS0618 // Exercising the deprecated Flutter surface on purpose.
        Assert.Equal(expected, MaterialTextSelectionControls.Instance.CanSelectAll(@delegate));
#pragma warning restore CS0618
    }

    [Fact]
    public void MaterialControls_BuildHandleRotatesPerTypeAndSizesTheHandle()
    {
        Assert.Null(RotationFor(TextSelectionHandleType.Right));
        Assert.Equal(Matrix4.RotationZ(Math.PI / 2.0), RotationFor(TextSelectionHandleType.Left));
        Assert.Equal(Matrix4.RotationZ(Math.PI / 4.0), RotationFor(TextSelectionHandleType.Collapsed));
    }

    [Fact]
    public void MaterialControls_HandleColorPrefersSelectionThemeThenColorSchemePrimary()
    {
        ThemeData theme = ThemeData.Light;

        Assert.Equal(theme.ColorScheme.Primary, HandleColor(theme, selectionHandleColor: null));
        Assert.Equal(Colors.DarkOrange, HandleColor(theme, selectionHandleColor: Colors.DarkOrange));
    }

    [Fact]
    public void MaterialControls_BuildHandleOmitsTapCallbackWhenNotSupplied()
    {
        using var harness = new WidgetRenderHarness(new Theme(
            ThemeData.Light,
            new Directionality(
                TextDirection.Ltr,
                new Builder(context => MaterialTextSelectionControls.Instance.BuildHandle(
                    context,
                    TextSelectionHandleType.Right,
                    10.0)))));

        harness.Pump(new Size(60, 60));

        RenderConstrainedBox box = Assert.Single(
            FindDescendants<RenderConstrainedBox>(harness.RenderView),
            value => value.AdditionalConstraints == BoxConstraints.Tight(new Size(22, 22)));
        Assert.Equal(new Size(22, 22), box.Size);
    }

    [Fact]
    public void HandlePainter_DrawsOneUnionPathAndRepaintsOnColorChange()
    {
        var painter = new TextSelectionHandlePainter(Color.FromArgb(0x55, 0x00, 0x00, 0xAA));

        // A single filled path (circle unioned with the square corner) so a translucent handle never
        // double-blends where the two shapes overlap. `radius` comes from the width alone.
        var combined = Assert.IsType<CombinedGeometry>(TextSelectionHandlePainter.BuildPath(new Size(22, 40)));
        Assert.Equal(GeometryCombineMode.Union, combined.GeometryCombineMode);
        Assert.Equal(new Rect(0, 0, 22, 22), Assert.IsType<EllipseGeometry>(combined.Geometry1).Rect);
        Assert.Equal(new Rect(0, 0, 11, 11), Assert.IsType<RectangleGeometry>(combined.Geometry2).Rect);

        var root = new ContainerLayer();
        var context = new PaintingContext(root);
        painter.Paint(context, new Size(22, 22));
        context.DebugStopRecordingIfNeeded();
        var picture = Assert.IsType<PictureLayer>(Assert.Single(root.Children));
        Assert.False(picture.IsEmpty);

        Assert.False(painter.ShouldRepaint(
            new TextSelectionHandlePainter(Color.FromArgb(0x55, 0x00, 0x00, 0xAA))));
        Assert.True(painter.ShouldRepaint(new TextSelectionHandlePainter(Colors.Red)));
    }

    [Fact]
    public void LegacyToolbar_UsesFlutterAnchorsAndFixedItemOrder()
    {
        var clipboardStatus = new ClipboardStatusNotifier(ClipboardStatus.Pasteable);
        var @delegate = new FakeSelectionDelegate(new TextEditingValue("hello", new TextSelection(1, 3)));

        using var harness = BuildToolbarHarness(@delegate, clipboardStatus);
        harness.Pump(new Size(400, 300));

        TextSelectionToolbar toolbar = Assert.Single(harness.FindWidgets<TextSelectionToolbar>());

        // anchorAbove = (editableRegion.left + midpoint.x, max(start.y - lineHeight, 0) + top - 8)
        // anchorBelow = (editableRegion.left + midpoint.x, top + end.y + 20)
        Assert.Equal(new Point(30, 16 + 40 - 8), toolbar.AnchorAbove);
        Assert.Equal(new Point(30, 40 + 42 + 20), toolbar.AnchorBelow);

        Assert.Equal(["Cut", "Copy", "Paste", "Select all"], ToolbarLabels(harness));
    }

    [Fact]
    public void LegacyToolbar_HidesPasteUntilTheClipboardIsKnownAndPasteable()
    {
        var @delegate = new FakeSelectionDelegate(new TextEditingValue("hello", new TextSelection(1, 3)));

        using var unknown = BuildToolbarHarness(@delegate, new ClipboardStatusNotifier());
        unknown.Pump(new Size(400, 300));
        Assert.Empty(unknown.FindWidgets<TextSelectionToolbar>());

        using var notPasteable = BuildToolbarHarness(
            @delegate,
            new ClipboardStatusNotifier(ClipboardStatus.NotPasteable));
        notPasteable.Pump(new Size(400, 300));
        Assert.Equal(["Cut", "Copy", "Select all"], ToolbarLabels(notPasteable));
    }

    [Fact]
    public void LegacyToolbar_BuildsNothingWhenEveryActionIsUnavailable()
    {
        var @delegate = new FakeSelectionDelegate(
            new TextEditingValue("hello", new TextSelection(0, 5)),
            enabled: false);

        using var harness = BuildToolbarHarness(@delegate, new ClipboardStatusNotifier(ClipboardStatus.Pasteable));
        harness.Pump(new Size(400, 300));

        Assert.Empty(harness.FindWidgets<TextSelectionToolbar>());
    }

    [Fact]
    public void LegacyToolbar_InvokesTheDelegateWithTheToolbarCause()
    {
        var @delegate = new FakeSelectionDelegate(new TextEditingValue("hello", new TextSelection(1, 3)));

        using var harness = BuildToolbarHarness(@delegate, new ClipboardStatusNotifier(ClipboardStatus.Pasteable));
        SemanticsNode? semantics = harness.PumpAndGetSemantics(new Size(400, 300));

        var taps = new List<SemanticsNode>();
        CollectSemantics(semantics, node => node.Actions.HasFlag(SemanticsActions.Tap), taps);
        Assert.Equal(4, taps.Count);
        foreach (SemanticsNode node in taps)
        {
            Assert.True(node.PerformAction(SemanticsActions.Tap));
        }

        Assert.Equal(["cut", "copy", "paste", "selectAll"], @delegate.Calls);
        Assert.All(@delegate.Causes, cause => Assert.Equal(SelectionChangedCause.Toolbar, cause));
    }

    [Fact]
    public void MaterialHandleControls_SuppressTheToolbarAndEveryLegacyAction()
    {
        TextSelectionControls controls = MaterialTextSelectionHandleControls.Instance;
        var @delegate = new FakeSelectionDelegate(new TextEditingValue("hello", new TextSelection(1, 3)));

        Assert.IsAssignableFrom<ITextSelectionHandleControls>(controls);
        Assert.Equal(new Size(22, 22), controls.GetHandleSize(10));
        Assert.Equal(new Point(22, 0), controls.GetHandleAnchor(TextSelectionHandleType.Left, 10));

#pragma warning disable CS0618 // Exercising the deprecated Flutter surface on purpose.
        Assert.False(controls.CanCut(@delegate));
        Assert.False(controls.CanCopy(@delegate));
        Assert.False(controls.CanPaste(@delegate));
        Assert.False(controls.CanSelectAll(@delegate));
        controls.HandleCut(@delegate);
        controls.HandleCopy(@delegate);
        controls.HandlePaste(@delegate);
        controls.HandleSelectAll(@delegate);
#pragma warning restore CS0618

        Assert.Empty(@delegate.Calls);
    }

    [Fact]
    public void MaterialHandleControls_MakeTheSelectionOverlayUseTheContextMenuPath()
    {
        using var harness = new WidgetRenderHarness(new Theme(
            ThemeData.Light,
            new Directionality(
                TextDirection.Ltr,
                new Overlay(initialEntries:
                [
                    new OverlayEntry(_ => new Navigator(new BuilderPageRoute(_ => new ContextProbe()))),
                ]))));
        harness.Pump(new Size(400, 300));
        BuildContext context = harness.FindState<ContextProbeState>().Context;

        var overlay = new SelectionOverlay(
            context: context,
            startHandleType: TextSelectionHandleType.Left,
            lineHeightAtStart: 14,
            endHandleType: TextSelectionHandleType.Right,
            lineHeightAtEnd: 14,
            selectionEndpoints: [new TextSelectionPoint(new Point(0, 14), null)],
            selectionControls: MaterialTextSelectionHandleControls.Instance,
            selectionDelegate: new FakeSelectionDelegate(new TextEditingValue("hello")),
            clipboardStatus: new ClipboardStatusNotifier(ClipboardStatus.Pasteable),
            startHandleLayerLink: new LayerLink(),
            endHandleLayerLink: new LayerLink(),
            toolbarLayerLink: new LayerLink());

        // With handle-only controls the legacy overlay entry is never used; visibility tracks the
        // context menu instead.
        overlay.ShowToolbar();
        harness.Pump(new Size(400, 300));
        Assert.False(overlay.ToolbarIsVisible);

        overlay.ShowToolbar(context, _ => new SizedBox(width: 40, height: 20));
        harness.Pump(new Size(400, 300));
        Assert.True(overlay.ToolbarIsVisible);

        overlay.HideToolbar();
        harness.Pump(new Size(400, 300));
        Assert.False(overlay.ToolbarIsVisible);

        overlay.Dispose();
    }

    private static string[] ToolbarLabels(WidgetRenderHarness harness)
    {
        return harness.FindWidgets<TextSelectionToolbarTextButton>()
            .Select(button => Assert.IsType<Text>(button.Child).Data!)
            .ToArray();
    }

    private static Matrix4? RotationFor(TextSelectionHandleType type)
    {
        Matrix4? rotation = null;
        using var harness = new WidgetRenderHarness(new Theme(
            ThemeData.Light,
            new Directionality(
                TextDirection.Ltr,
                new Builder(context =>
                {
                    Widget handle = MaterialTextSelectionControls.Instance.BuildHandle(
                        context,
                        type,
                        10.0,
                        () => { });
                    rotation = (handle as Plumix.Widgets.Transform)?.Matrix;
                    return handle;
                }))));
        harness.Pump(new Size(60, 60));
        return rotation;
    }

    private static Color HandleColor(ThemeData theme, Color? selectionHandleColor)
    {
        Widget child = new Builder(context => MaterialTextSelectionControls.Instance.BuildHandle(
            context,
            TextSelectionHandleType.Right,
            10.0));
        if (selectionHandleColor.HasValue)
        {
            child = new TextSelectionTheme(
                new TextSelectionThemeData(SelectionHandleColor: selectionHandleColor),
                child);
        }

        using var harness = new WidgetRenderHarness(new Theme(
            theme,
            new Directionality(TextDirection.Ltr, child)));
        harness.Pump(new Size(60, 60));

        RenderCustomPaint paint = Assert.Single(FindDescendants<RenderCustomPaint>(harness.RenderView));
        return Assert.IsType<TextSelectionHandlePainter>(paint.Painter).Color;
    }

    private static WidgetRenderHarness BuildToolbarHarness(
        ITextSelectionDelegate @delegate,
        ClipboardStatusNotifier clipboardStatus)
    {
#pragma warning disable CS0618 // Exercising the deprecated Flutter surface on purpose.
        return new WidgetRenderHarness(new Theme(
            ThemeData.Light,
            new MediaQuery(
                new MediaQueryData(),
                new Directionality(
                    TextDirection.Ltr,
                    new Builder(context => MaterialTextSelectionControls.Instance.BuildToolbar(
                        context,
                        globalEditableRegion: new Rect(10, 40, 200, 60),
                        textLineHeight: 14,
                        selectionMidpoint: new Point(20, 0),
                        endpoints:
                        [
                            new TextSelectionPoint(new Point(4, 30), null),
                            new TextSelectionPoint(new Point(60, 42), null),
                        ],
                        @delegate: @delegate,
                        clipboardStatus: clipboardStatus,
                        lastSecondaryTapDownPosition: null))))));
#pragma warning restore CS0618
    }

    private static void CollectSemantics(
        SemanticsNode? node,
        Func<SemanticsNode, bool> predicate,
        List<SemanticsNode> result)
    {
        if (node is null)
        {
            return;
        }

        if (predicate(node))
        {
            result.Add(node);
        }

        foreach (SemanticsNode child in node.Children)
        {
            CollectSemantics(child, predicate, result);
        }
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

    private sealed class FakeSelectionDelegate : ITextSelectionDelegate
    {
        private readonly bool _enabled;

        public FakeSelectionDelegate(TextEditingValue value, bool enabled = true)
        {
            TextEditingValue = value;
            _enabled = enabled;
        }

        public TextEditingValue TextEditingValue { get; private set; }

        public List<string> Calls { get; } = [];

        public List<SelectionChangedCause> Causes { get; } = [];

        public bool CutEnabled => _enabled;

        public bool CopyEnabled => _enabled;

        public bool PasteEnabled => _enabled;

        public bool SelectAllEnabled => _enabled;

        public void UserUpdateTextEditingValue(TextEditingValue value, SelectionChangedCause? cause)
        {
            TextEditingValue = value;
        }

        public void CutSelection(SelectionChangedCause cause)
        {
            Calls.Add("cut");
            Causes.Add(cause);
        }

        public void CopySelection(SelectionChangedCause cause)
        {
            Calls.Add("copy");
            Causes.Add(cause);
        }

        public void PasteText(SelectionChangedCause cause)
        {
            Calls.Add("paste");
            Causes.Add(cause);
        }

        public void SelectAll(SelectionChangedCause cause)
        {
            Calls.Add("selectAll");
            Causes.Add(cause);
        }

        public void HideToolbar(bool hideHandles = true)
        {
        }
    }

    private sealed class ContextProbe : StatefulWidget
    {
        public override State CreateState() => new ContextProbeState();
    }

    private sealed class ContextProbeState : State
    {
        public override Widget Build(BuildContext context) => new SizedBox(width: 200, height: 40);
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
            return _pipeline.SemanticsOwner!.RootNode;
        }

        public T FindState<T>() where T : State
        {
            return FindState<T>(_rootElement)
                   ?? throw new InvalidOperationException($"State {typeof(T).Name} was not found.");
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

        private static T? FindState<T>(Element element) where T : State
        {
            if (element is StatefulElement stateful && stateful.State is T state)
            {
                return state;
            }

            T? result = null;
            element.VisitChildren(child => result ??= FindState<T>(child));
            return result;
        }

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget)
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
