using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class SelectionOverlayTests : IDisposable
{
    public SelectionOverlayTests()
    {
        Scheduler.ResetForTests();
        Feedback.ResetForTests();
    }

    public void Dispose()
    {
        Feedback.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void SelectionOverlay_ShowsAndHidesHandlesToolbarAndEverything()
    {
        using var fixture = new OverlayFixture();
        SelectionOverlay overlay = fixture.CreateOverlay(
            startHandleType: TextSelectionHandleType.Left,
            endHandleType: TextSelectionHandleType.Right);

        overlay.ShowHandles();
        fixture.Pump();
        Assert.Equal(2, fixture.FindWidgets<SelectionHandleOverlay>().Count);
        Assert.True(overlay.HandlesAreInserted);

        overlay.HideHandles();
        fixture.Pump();
        Assert.Empty(fixture.FindWidgets<SelectionHandleOverlay>());

        overlay.ShowToolbar();
        fixture.Pump();
        Assert.True(overlay.ToolbarIsVisible);
        Assert.Single(fixture.FindWidgets<SelectionToolbarWrapper>());

        overlay.HideToolbar();
        fixture.Pump();
        Assert.False(overlay.ToolbarIsVisible);
        Assert.Empty(fixture.FindWidgets<SelectionToolbarWrapper>());

        overlay.ShowHandles();
        overlay.ShowToolbar();
        fixture.Pump();
        Assert.Equal(2, fixture.FindWidgets<SelectionHandleOverlay>().Count);
        Assert.Single(fixture.FindWidgets<SelectionToolbarWrapper>());

        overlay.Hide();
        fixture.Pump();
        Assert.Empty(fixture.FindWidgets<SelectionHandleOverlay>());
        Assert.Empty(fixture.FindWidgets<SelectionToolbarWrapper>());

        overlay.Dispose();
    }

    [Fact]
    public void SelectionOverlay_PaintsOnlyOneCollapsedHandle()
    {
        using var fixture = new OverlayFixture();
        SelectionOverlay overlay = fixture.CreateOverlay(
            startHandleType: TextSelectionHandleType.Collapsed,
            endHandleType: TextSelectionHandleType.Collapsed);

        overlay.ShowHandles();
        fixture.Pump();

        SelectionHandleOverlay handle = Assert.Single(fixture.FindWidgets<SelectionHandleOverlay>());
        Assert.Equal(TextSelectionHandleType.Collapsed, handle.Type);
        Assert.Empty(fixture.FindWidgets<TypedHandleProbe>()
            .Where(probe => probe.Type != TextSelectionHandleType.Collapsed));

        Assert.Same(overlay.StartHandleLayerLink, handle.HandleLayerLink);

        // A collapsed end handle stays suppressed while the start handle is the one being dragged,
        // so exactly one handle is still built.
        handle.OnSelectionHandleDragStart!(TouchDragStart(new Point(4, 4)));
        overlay.MarkNeedsBuild();
        fixture.Pump();

        SelectionHandleOverlay draggedHandle = Assert.Single(fixture.FindWidgets<SelectionHandleOverlay>());
        Assert.Same(overlay.StartHandleLayerLink, draggedHandle.HandleLayerLink);

        overlay.Dispose();
    }

    [Fact]
    public void SelectionOverlay_RebuildsLiveHandlesWhenTypeAndLineHeightChange()
    {
        using var fixture = new OverlayFixture();
        SelectionOverlay overlay = fixture.CreateOverlay(
            startHandleType: TextSelectionHandleType.Left,
            endHandleType: TextSelectionHandleType.Right,
            lineHeightAtStart: 10.0,
            lineHeightAtEnd: 11.0);

        overlay.ShowHandles();
        fixture.Pump();

        Assert.Equal(10.0, ProbeFor(fixture, TextSelectionHandleType.Left).PreferredLineHeight);
        Assert.Equal(11.0, ProbeFor(fixture, TextSelectionHandleType.Right).PreferredLineHeight);

        overlay.StartHandleType = TextSelectionHandleType.Right;
        overlay.LineHeightAtStart = 12.0;
        overlay.EndHandleType = TextSelectionHandleType.Left;
        overlay.LineHeightAtEnd = 13.0;
        fixture.Pump();

        Assert.Equal(13.0, ProbeFor(fixture, TextSelectionHandleType.Left).PreferredLineHeight);
        Assert.Equal(12.0, ProbeFor(fixture, TextSelectionHandleType.Right).PreferredLineHeight);

        overlay.Dispose();
    }

    [Fact]
    public void SelectionOverlay_HandleTapInvokesCallback()
    {
        using var fixture = new OverlayFixture();
        int taps = 0;
        SelectionOverlay overlay = fixture.CreateOverlay(
            startHandleType: TextSelectionHandleType.Left,
            endHandleType: TextSelectionHandleType.Right,
            onSelectionHandleTapped: () => taps++);

        overlay.ShowHandles();
        fixture.Pump();

        foreach (TypedHandleProbe probe in fixture.FindWidgets<TypedHandleProbe>())
        {
            probe.OnTap!();
        }

        Assert.Equal(2, taps);
        overlay.Dispose();
    }

    [Fact]
    public void SelectionOverlay_TouchHandleDragForwardsDetailsAndTracksDragState()
    {
        using var fixture = new OverlayFixture();
        var starts = new List<DragStartDetails>();
        var updates = new List<DragUpdateDetails>();
        int ends = 0;
        SelectionOverlay overlay = fixture.CreateOverlay(
            startHandleType: TextSelectionHandleType.Left,
            endHandleType: TextSelectionHandleType.Right,
            onStartHandleDragStart: starts.Add,
            onStartHandleDragUpdate: updates.Add,
            onStartHandleDragEnd: _ => ends++);

        overlay.ShowHandles();
        fixture.Pump();
        SelectionHandleOverlay startHandle = fixture.FindWidgets<SelectionHandleOverlay>()[0];

        startHandle.OnSelectionHandleDragStart!(TouchDragStart(new Point(30, 40)));
        Assert.True(overlay.IsDraggingStartHandle);
        Assert.Equal(new Point(30, 40), Assert.Single(starts).GlobalPosition);

        startHandle.OnSelectionHandleDragUpdate!(TouchDragUpdate(new Point(20, 20)));
        Assert.Single(starts);
        Assert.Equal(new Point(20, 20), Assert.Single(updates).GlobalPosition);

        startHandle.OnSelectionHandleDragEnd!(new DragEndDetails(0.0));
        Assert.False(overlay.IsDraggingStartHandle);
        Assert.Equal(1, ends);

        overlay.Dispose();
    }

    [Fact]
    public void SelectionOverlay_NonTouchDragSynthesizesStartOnFirstUpdate()
    {
        using var fixture = new OverlayFixture();
        var starts = new List<DragStartDetails>();
        SelectionOverlay overlay = fixture.CreateOverlay(
            startHandleType: TextSelectionHandleType.Left,
            endHandleType: TextSelectionHandleType.Right,
            onStartHandleDragStart: starts.Add);

        overlay.ShowHandles();
        fixture.Pump();
        SelectionHandleOverlay startHandle = fixture.FindWidgets<SelectionHandleOverlay>()[0];

        startHandle.OnSelectionHandleDragStart!(new DragStartDetails(
            GlobalPosition: new Point(5, 5),
            Kind: PointerDeviceKind.Mouse));
        startHandle.OnSelectionHandleDragUpdate!(new DragUpdateDetails(
            GlobalPosition: new Point(9, 9),
            LocalPosition: new Point(1, 1),
            Delta: new Point(4, 4),
            PrimaryDelta: 0.0,
            Kind: PointerDeviceKind.Mouse));

        Assert.Equal(2, starts.Count);
        Assert.Equal(new Point(9, 9), starts[1].GlobalPosition);

        overlay.Dispose();
    }

    [Fact]
    public void SelectionOverlay_DragCallbacksAreIgnoredWhenHandlesAreNotInserted()
    {
        using var fixture = new OverlayFixture();
        int starts = 0;
        SelectionOverlay overlay = fixture.CreateOverlay(
            startHandleType: TextSelectionHandleType.Left,
            endHandleType: TextSelectionHandleType.Right,
            onStartHandleDragStart: _ => starts++);

        overlay.ShowHandles();
        fixture.Pump();
        SelectionHandleOverlay startHandle = fixture.FindWidgets<SelectionHandleOverlay>()[0];
        overlay.HideHandles();

        startHandle.OnSelectionHandleDragStart!(TouchDragStart(new Point(1, 1)));

        Assert.Equal(0, starts);
        Assert.False(overlay.IsDraggingStartHandle);
        overlay.Dispose();
    }

    [Fact]
    public void SelectionOverlay_EndpointChangeEmitsSelectionHapticOnlyWhileDragging()
    {
        using var fixture = new OverlayFixture();
        var feedback = new List<FeedbackType>();
        Feedback.FeedbackTriggered += feedback.Add;
        SelectionOverlay overlay = fixture.CreateOverlay(
            startHandleType: TextSelectionHandleType.Left,
            endHandleType: TextSelectionHandleType.Right);

        overlay.ShowHandles();
        fixture.Pump();
        overlay.SelectionEndpoints = [new TextSelectionPoint(new Point(1, 2), null)];
        Assert.Empty(feedback);

        SelectionHandleOverlay startHandle = fixture.FindWidgets<SelectionHandleOverlay>()[0];
        startHandle.OnSelectionHandleDragStart!(TouchDragStart(new Point(3, 3)));
        overlay.SelectionEndpoints = [new TextSelectionPoint(new Point(7, 9), null)];

        Assert.Equal(
            PlatformDefaults.TargetPlatform == TargetPlatform.Android
                ? [FeedbackType.SelectionClick]
                : Array.Empty<FeedbackType>(),
            feedback);

        overlay.Dispose();
    }

    [Fact]
    public void SelectionOverlay_ShowsMagnifierWithoutHandlesAndUpdatesInfoNotifier()
    {
        using var fixture = new OverlayFixture();
        ValueNotifier<MagnifierInfo>? capturedInfo = null;
        SelectionOverlay overlay = fixture.CreateOverlay(
            startHandleType: TextSelectionHandleType.Left,
            endHandleType: TextSelectionHandleType.Right,
            magnifierConfiguration: new TextMagnifierConfiguration((_, _, info) =>
            {
                capturedInfo = info;
                return new SizedBox(width: 20, height: 10);
            }));

        var initial = new MagnifierInfo(
            GlobalGesturePosition: new Point(4, 5),
            CaretRect: new Rect(0, 0, 2, 12),
            FieldBounds: new Rect(0, 0, 100, 20),
            CurrentLineBoundaries: new Rect(0, 0, 100, 12));
        overlay.ShowMagnifier(initial);
        fixture.Pump();

        Assert.False(overlay.HandlesAreInserted);
        Assert.True(overlay.MagnifierExists);
        Assert.Equal(initial, capturedInfo!.Value);

        var moved = initial with { GlobalGesturePosition = new Point(40, 5) };
        overlay.UpdateMagnifier(moved);
        Assert.Equal(moved, capturedInfo.Value);

        overlay.HideMagnifier();
        fixture.Pump();
        Assert.False(overlay.MagnifierIsVisible);

        overlay.Dispose();
    }

    [Fact]
    public void SelectionOverlay_DisabledMagnifierConfigurationInsertsNothing()
    {
        using var fixture = new OverlayFixture();
        SelectionOverlay overlay = fixture.CreateOverlay(
            startHandleType: TextSelectionHandleType.Left,
            endHandleType: TextSelectionHandleType.Right);

        overlay.ShowMagnifier(MagnifierInfo.Empty);
        fixture.Pump();

        Assert.False(overlay.MagnifierExists);
        overlay.Dispose();
    }

    [Fact]
    public void SelectionHandleOverlay_UsesFlutterInteractiveGeometryAndFollowerOffset()
    {
        using var fixture = new OverlayFixture();
        SelectionOverlay overlay = fixture.CreateOverlay(
            startHandleType: TextSelectionHandleType.Left,
            endHandleType: TextSelectionHandleType.Right);

        overlay.ShowHandles();
        fixture.Pump();

        RenderFollowerLayer follower = Assert.Single(
            FindDescendants<RenderFollowerLayer>(fixture.RenderView),
            value => ReferenceEquals(value.Link, overlay.StartHandleLayerLink));

        // 22x22 handle grown to the 48x48 minimum interactive size leaves 13px of padding per side,
        // and the follower backs that padding out of the anchor.
        Assert.Equal(new Vector(-TestControls.HandleAnchor.X - 13, -TestControls.HandleAnchor.Y - 13),
            follower.Offset);
        Assert.False(follower.ShowWhenUnlinked);

        Assert.Contains(FindDescendants<RenderConstrainedBox>(fixture.RenderView), box =>
            box.AdditionalConstraints == BoxConstraints.Tight(new Size(48, 48)));
        Assert.Contains(FindDescendants<RenderPadding>(fixture.RenderView), value =>
            value.Padding == new Thickness(13, 13, 13, 13));

        overlay.Dispose();
    }

    [Fact]
    public void SelectionHandleOverlay_ZeroSizedHandleKeepsEmptyInteractiveRect()
    {
        using var fixture = new OverlayFixture();
        SelectionOverlay overlay = fixture.CreateOverlay(
            startHandleType: TextSelectionHandleType.Left,
            endHandleType: TextSelectionHandleType.Right,
            selectionControls: EmptyTextSelectionControls.Instance);

        overlay.ShowHandles();
        fixture.Pump();

        Assert.Contains(FindDescendants<RenderPadding>(fixture.RenderView), value =>
            value.Padding == default);
        overlay.Dispose();
    }

    [Fact]
    public void TextSelectionControls_DefaultEnablementFollowsTheDelegate()
    {
        TextSelectionControls controls = new TestControls();
        var collapsed = new FakeSelectionDelegate(new TextEditingValue("hello", TextSelection.Collapsed(1)));
        var ranged = new FakeSelectionDelegate(new TextEditingValue("hello", new TextSelection(1, 3)));
        var empty = new FakeSelectionDelegate(new TextEditingValue(string.Empty));

#pragma warning disable CS0618 // Exercising the deprecated Flutter surface on purpose.
        Assert.False(controls.CanCut(collapsed));
        Assert.True(controls.CanCut(ranged));
        Assert.False(controls.CanCopy(collapsed));
        Assert.True(controls.CanCopy(ranged));
        Assert.True(controls.CanPaste(collapsed));
        Assert.True(controls.CanSelectAll(collapsed));
        Assert.False(controls.CanSelectAll(ranged));
        Assert.False(controls.CanSelectAll(empty));

        controls.HandleCut(ranged);
        controls.HandleCopy(ranged);
        controls.HandlePaste(ranged);
        controls.HandleSelectAll(ranged);
#pragma warning restore CS0618

        Assert.Equal(["cut", "copy", "paste", "selectAll"], ranged.Calls);
        Assert.All(ranged.Causes, cause => Assert.Equal(SelectionChangedCause.Toolbar, cause));
    }

    [Fact]
    public void TextSelectionControls_DisabledDelegateBlocksEveryAction()
    {
        TextSelectionControls controls = new TestControls();
        var disabled = new FakeSelectionDelegate(
            new TextEditingValue("hello", new TextSelection(1, 3)),
            enabled: false);

#pragma warning disable CS0618 // Exercising the deprecated Flutter surface on purpose.
        Assert.False(controls.CanCut(disabled));
        Assert.False(controls.CanCopy(disabled));
        Assert.False(controls.CanPaste(disabled));
        Assert.False(controls.CanSelectAll(disabled));
#pragma warning restore CS0618
    }

    [Fact]
    public void EmptyTextSelectionControls_BuildsNothingAndHasNoGeometry()
    {
        TextSelectionControls controls = EmptyTextSelectionControls.Instance;

        Assert.Equal(default, controls.GetHandleSize(24));
        Assert.Equal(default, controls.GetHandleAnchor(TextSelectionHandleType.Left, 24));
        Assert.Same(EmptyTextSelectionControls.Instance, EmptyTextSelectionControls.Instance);
    }

    [Fact]
    public void ClipboardStatusNotifier_UpdatesFromTheClipboardAndNotifiesOnce()
    {
        var notifier = new ClipboardStatusNotifier();
        int notifications = 0;
        notifier.AddListener(() => notifications++);

        TextClipboard.SetText("copied");
        notifier.Update();
        Assert.Equal(ClipboardStatus.Pasteable, notifier.Value);
        Assert.Equal(1, notifications);

        notifier.Update();
        Assert.Equal(1, notifications);

        TextClipboard.SetText(string.Empty);
        notifier.Update();
        Assert.Equal(ClipboardStatus.NotPasteable, notifier.Value);
        Assert.Equal(2, notifications);
    }

    private static DragStartDetails TouchDragStart(Point position)
    {
        return new DragStartDetails(GlobalPosition: position, Kind: PointerDeviceKind.Touch);
    }

    private static DragUpdateDetails TouchDragUpdate(Point position)
    {
        return new DragUpdateDetails(
            GlobalPosition: position,
            LocalPosition: position,
            Delta: default,
            PrimaryDelta: 0.0,
            Kind: PointerDeviceKind.Touch);
    }

    private static TypedHandleProbe ProbeFor(OverlayFixture fixture, TextSelectionHandleType type)
    {
        return Assert.Single(fixture.FindWidgets<TypedHandleProbe>(), probe => probe.Type == type);
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

    /// <summary>A stand-in for the platform controls that exposes what the overlay asked it to build.</summary>
    private sealed class TestControls : TextSelectionControls
    {
        public static Point HandleAnchor { get; } = new(3, 5);

        public override Size GetHandleSize(double textLineHeight) => new(22, 22);

        public override Point GetHandleAnchor(TextSelectionHandleType type, double textLineHeight)
        {
            return HandleAnchor;
        }

        public override Widget BuildHandle(
            BuildContext context,
            TextSelectionHandleType type,
            double textLineHeight,
            Action? onTap = null)
        {
            return new TypedHandleProbe(type, textLineHeight, onTap);
        }

        [Obsolete("Matches the deprecated Flutter surface.")]
        public override Widget BuildToolbar(
            BuildContext context,
            Rect globalEditableRegion,
            double textLineHeight,
            Point selectionMidpoint,
            IReadOnlyList<TextSelectionPoint> endpoints,
            ITextSelectionDelegate @delegate,
            IValueListenable<ClipboardStatus>? clipboardStatus,
            Point? lastSecondaryTapDownPosition)
        {
            return new SizedBox(width: 60, height: 20);
        }
    }

    private sealed class TypedHandleProbe : StatelessWidget
    {
        public TypedHandleProbe(TextSelectionHandleType type, double preferredLineHeight, Action? onTap)
        {
            Type = type;
            PreferredLineHeight = preferredLineHeight;
            OnTap = onTap;
        }

        public TextSelectionHandleType Type { get; }

        public double PreferredLineHeight { get; }

        public Action? OnTap { get; }

        public override Widget Build(BuildContext context) => new SizedBox(width: 22, height: 22);
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
            Calls.Add("update");
            if (cause.HasValue)
            {
                Causes.Add(cause.Value);
            }
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
            Calls.Add("hideToolbar");
        }
    }

    private sealed class OverlayFixture : IDisposable
    {
        private readonly WidgetRenderHarness _harness;
        private readonly ContextProbeState _probe;

        public OverlayFixture()
        {
            _harness = new WidgetRenderHarness(new Directionality(
                TextDirection.Ltr,
                new Overlay(initialEntries:
                [
                    new OverlayEntry(_ => new Navigator(new BuilderPageRoute(_ => new ContextProbe()))),
                ])));
            _harness.Pump(new Size(400, 300));
            _probe = _harness.FindState<ContextProbeState>();
        }

        public RenderView RenderView => _harness.RenderView;

        public SelectionOverlay CreateOverlay(
            TextSelectionHandleType startHandleType,
            TextSelectionHandleType endHandleType,
            double lineHeightAtStart = 14.0,
            double lineHeightAtEnd = 14.0,
            TextSelectionControls? selectionControls = null,
            Action? onSelectionHandleTapped = null,
            Action<DragStartDetails>? onStartHandleDragStart = null,
            Action<DragUpdateDetails>? onStartHandleDragUpdate = null,
            Action<DragEndDetails>? onStartHandleDragEnd = null,
            TextMagnifierConfiguration? magnifierConfiguration = null)
        {
            return new SelectionOverlay(
                context: _probe.Context,
                startHandleType: startHandleType,
                lineHeightAtStart: lineHeightAtStart,
                endHandleType: endHandleType,
                lineHeightAtEnd: lineHeightAtEnd,
                selectionEndpoints: [new TextSelectionPoint(new Point(0, 14), null)],
                selectionControls: selectionControls ?? new TestControls(),
                selectionDelegate: new FakeSelectionDelegate(new TextEditingValue("hello")),
                clipboardStatus: new ClipboardStatusNotifier(),
                startHandleLayerLink: new LayerLink(),
                endHandleLayerLink: new LayerLink(),
                toolbarLayerLink: new LayerLink(),
                onSelectionHandleTapped: onSelectionHandleTapped,
                onStartHandleDragStart: onStartHandleDragStart,
                onStartHandleDragUpdate: onStartHandleDragUpdate,
                onStartHandleDragEnd: onStartHandleDragEnd,
                magnifierConfiguration: magnifierConfiguration);
        }

        public void Pump() => _harness.Pump(new Size(400, 300));

        public IReadOnlyList<T> FindWidgets<T>() where T : Widget => _harness.FindWidgets<T>();

        public void Dispose() => _harness.Dispose();
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
