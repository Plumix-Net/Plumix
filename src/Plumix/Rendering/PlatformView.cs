using System.Diagnostics;
using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/platform_view.dart

namespace Plumix.Rendering;

/// <summary>How an embedded platform view behave during hit tests.</summary>
/// <remarks>Flutter's <c>PlatformViewHitTestBehavior</c>.</remarks>
public enum PlatformViewHitTestBehavior
{
    /// <summary>Opaque targets can be hit by hit tests, causing them to both receive events within
    /// their bounds and prevent targets visually behind them from also receiving events.</summary>
    Opaque,

    /// <summary>Translucent targets both receive events within their bounds and permit targets
    /// visually behind them to also receive events.</summary>
    Translucent,

    /// <summary>Transparent targets don't receive events within their bounds and permit targets
    /// visually behind them to receive events.</summary>
    Transparent,
}

internal enum PlatformViewState
{
    Uninitialized,
    Resizing,
    Ready,
}

/// <summary>The helpers Dart keeps at the top level of <c>platform_view.dart</c>.</summary>
internal static class PlatformViewFactories
{
    // Dart's `_factoryTypesSetEquals`.
    internal static bool FactoryTypesSetEquals<T>(
        IReadOnlySet<IFactory<T>>? a,
        IReadOnlySet<IFactory<T>>? b)
    {
        if (ReferenceEquals(a, b) || (a is not null && a.Equals(b)))
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        return FactoriesTypeSet(a).SetEquals(FactoriesTypeSet(b));
    }

    // Dart's `_factoriesTypeSet`.
    internal static HashSet<Type> FactoriesTypeSet<T>(IReadOnlySet<IFactory<T>> factories)
    {
        return factories.Select(factory => factory.Type).ToHashSet();
    }

    internal const string DuplicateFactoriesMessage =
        "There were multiple gesture recognizer factories for the same type, there must only be a single "
        + "gesture recognizer factory for each gesture recognizer type.";

    // The shared body of both recognizers' constructors: builds the team the recognizer captains.
    internal static HashSet<OneSequenceGestureRecognizer> CreateTeam(
        OneSequenceGestureRecognizer captain,
        IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> gestureRecognizerFactories)
    {
        var team = new GestureArenaTeam { Captain = captain };
        captain.Team = team;
        return gestureRecognizerFactories.Select(recognizerFactory =>
        {
            OneSequenceGestureRecognizer gestureRecognizer = recognizerFactory.Construct();
            gestureRecognizer.Team = team;
            // The below gesture recognizers requires at least one non-null callback to be set.
            // We set them to no-op so that the gesture recognizer won't drop the pointer.
            if (gestureRecognizer is LongPressGestureRecognizer longPress)
            {
                longPress.OnLongPress ??= () => { };
            }
            else if (gestureRecognizer is DragGestureRecognizer drag)
            {
                drag.OnDown ??= _ => { };
            }
            else if (gestureRecognizer is TapGestureRecognizer tap)
            {
                tap.OnTapDown ??= _ => { };
            }

            return gestureRecognizer;
        }).ToHashSet();
    }
}

/// <summary>A render object for an Android view.</summary>
/// <remarks>
/// Flutter's <c>RenderAndroidView</c>. Requires Android API level 23 or greater.
/// <see cref="RenderAndroidView"/> is responsible for sizing, displaying and passing touch events to
/// an Android View. The render object's layout behavior is to fill all available space, the parent of
/// this object must provide bounded layout constraints. It does not support semantics.
/// </remarks>
public class RenderAndroidView : PlatformViewRenderBox
{
    private PlatformViewState _state = PlatformViewState.Uninitialized;
    private Size? _currentTextureSize;
    private bool _isDisposed;
    private AndroidViewController _viewController;
    private Clip _clipBehavior = Clip.HardEdge;
    private readonly LayerHandle<ClipRectLayer> _clipRectLayer = new();

    /// <summary>Creates a render object for an Android view.</summary>
    public RenderAndroidView(
        AndroidViewController viewController,
        PlatformViewHitTestBehavior hitTestBehavior,
        IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> gestureRecognizers,
        Clip clipBehavior = Clip.HardEdge)
        : base(controller: viewController, hitTestBehavior: hitTestBehavior, gestureRecognizers: gestureRecognizers)
    {
        _viewController = viewController;
        _clipBehavior = clipBehavior;
        _viewController.PointTransformer = offset => GlobalToLocal(offset);
        UpdateGestureRecognizers(gestureRecognizers);
        _viewController.AddOnPlatformViewCreatedListener(OnPlatformViewCreated);
        HitTestBehavior = hitTestBehavior;
        SetOffset();
    }

    /// <summary>The Android view controller for the Android view associated with this render
    /// object.</summary>
    /// <remarks>Dart narrows the type to <see cref="AndroidViewController"/> through a covariant
    /// override; C# setters cannot narrow, so a non-Android controller fails the cast.</remarks>
    public override PlatformViewController Controller
    {
        get => _viewController;
        set
        {
            var controller = (AndroidViewController)value;
            Debug.Assert(!_isDisposed);
            if (ReferenceEquals(_viewController, controller))
            {
                return;
            }

            _viewController.RemoveOnPlatformViewCreatedListener(OnPlatformViewCreated);
            base.Controller = controller;
            _viewController = controller;
            _viewController.PointTransformer = offset => GlobalToLocal(offset);
            _ = SizePlatformView();
            if (_viewController.IsCreated)
            {
                MarkNeedsSemanticsUpdate();
            }

            _viewController.AddOnPlatformViewCreatedListener(OnPlatformViewCreated);
        }
    }

    /// <summary>How to clip the texture when it is larger than the widget.</summary>
    /// <remarks>Defaults to <see cref="Clip.HardEdge"/>.</remarks>
    public Clip ClipBehavior
    {
        get => _clipBehavior;
        set
        {
            if (value != _clipBehavior)
            {
                _clipBehavior = value;
                MarkNeedsPaint();
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    private void OnPlatformViewCreated(int id)
    {
        Debug.Assert(!_isDisposed);
        MarkNeedsSemanticsUpdate();
    }

    protected override bool SizedByParent => true;

    public override bool AlwaysNeedsCompositing => true;

    public override bool IsRepaintBoundary => true;

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        return constraints.Biggest;
    }

    protected override void PerformResize()
    {
        base.PerformResize();
        _ = SizePlatformView();
    }

    private async Task SizePlatformView()
    {
        // Android virtual displays cannot have a zero size.
        // Trying to size it to 0 crashes the app, which was happening when starting the app with a
        // locked screen (see: https://github.com/flutter/flutter/issues/20456).
        if (_state == PlatformViewState.Resizing || Size.IsEmpty)
        {
            return;
        }

        _state = PlatformViewState.Resizing;
        MarkNeedsPaint();

        Size targetSize;
        do
        {
            targetSize = Size;
            _currentTextureSize = await _viewController.SetSize(targetSize);
            if (_isDisposed)
            {
                return;
            }
            // We've resized the platform view to targetSize, but it is possible that while we were
            // resizing the render object's size was changed again. In that case we will resize the
            // platform view again.
        }
        while (Size != targetSize);

        _state = PlatformViewState.Ready;
        MarkNeedsPaint();
    }

    // Sets the offset of the underlying platform view on the platform side.
    //
    // This allows the Android native view to draw the a11y highlights in the same location on the
    // screen as the platform view widget in the Flutter framework.
    //
    // It also allows platform code to obtain the correct position of the Android native view on the
    // screen.
    private void SetOffset()
    {
        Scheduler.AddPostFrameCallback(
            timeStamp => { _ = SetOffsetAfterFrame(); },
            debugLabel: "RenderAndroidView.setOffset");
    }

    private async Task SetOffsetAfterFrame()
    {
        if (!_isDisposed)
        {
            if (Attached)
            {
                await _viewController.SetOffset(LocalToGlobal(default));
            }

            // Schedule a new post frame callback.
            SetOffset();
        }
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if ((_viewController.TextureId is null && !_viewController.RequiresViewComposition)
            || _currentTextureSize is null)
        {
            return;
        }

        // As resizing the Android view happens asynchronously we don't know exactly when is a resize
        // frame with the new size on the screen. To prevent stretching of the texture we always paint
        // the texture at the size of the last texture frame, and clip it when it is larger than the
        // widget.
        bool isTextureLargerThanWidget = _currentTextureSize.Value.Width > Size.Width
                                         || _currentTextureSize.Value.Height > Size.Height;
        if (isTextureLargerThanWidget && ClipBehavior != Clip.None)
        {
            _clipRectLayer.Layer = context.PushClipRect(
                true,
                offset,
                new Rect(offset, Size),
                PaintTexture,
                clipBehavior: ClipBehavior,
                oldLayer: _clipRectLayer.Layer);
            return;
        }

        _clipRectLayer.Layer = null;
        PaintTexture(context, offset);
    }

    public override void Dispose()
    {
        _isDisposed = true;
        _clipRectLayer.Layer = null;
        _viewController.RemoveOnPlatformViewCreatedListener(OnPlatformViewCreated);
        base.Dispose();
    }

    private void PaintTexture(PaintingContext context, Point offset)
    {
        if (_currentTextureSize is not { } textureSize)
        {
            return;
        }

        if (_viewController.RequiresViewComposition)
        {
            context.AddLayer(new PlatformViewLayer(
                rect: new Rect(offset, textureSize),
                viewId: _viewController.ViewId));
        }
        else
        {
            context.AddLayer(new TextureLayer(
                rect: new Rect(offset, textureSize),
                textureId: _viewController.TextureId!.Value));
        }
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        // Don't call the super implementation since `platformViewId` should be set only when the
        // platform view is created, but the concept of a "created" platform view belongs to this
        // subclass.
        configuration.IsSemanticBoundary = true;

        if (_viewController.IsCreated)
        {
            configuration.PlatformViewId = _viewController.ViewId;
            configuration.HitTestBehavior = SemanticsHitTestBehavior.Transparent;
        }
    }
}

/// <summary>Common render-layer functionality for iOS and macOS platform views.</summary>
/// <remarks>
/// Flutter's <c>RenderDarwinPlatformView</c>. Provides the basic rendering logic for iOS and macOS
/// platformviews. Subclasses shall override handleEvent in order to execute custom event logic.
/// <typeparamref name="T"/> represents the class of the view controller for the corresponding
/// widget.
/// </remarks>
public abstract class RenderDarwinPlatformView<T> : RenderBox
    where T : DarwinPlatformViewController
{
    private T _viewController;

    /// <summary>Creates a render object for a platform view.</summary>
    protected RenderDarwinPlatformView(
        T viewController,
        PlatformViewHitTestBehavior hitTestBehavior,
        IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> gestureRecognizers)
    {
        _viewController = viewController;
        HitTestBehavior = hitTestBehavior;
        // Dart's constructor body; the call is virtual there too.
        // ReSharper disable once VirtualMemberCallInConstructor
        UpdateGestureRecognizers(gestureRecognizers);
    }

    /// <summary>The unique identifier of the platform view controlled by this controller.</summary>
    public T ViewController
    {
        get => _viewController;
        set
        {
            if (ReferenceEquals(_viewController, value))
            {
                return;
            }

            bool needsSemanticsUpdate = _viewController.Id != value.Id;
            _viewController = value;
            MarkNeedsPaint();
            if (needsSemanticsUpdate)
            {
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    /// <summary>How to behave during hit testing.</summary>
    public PlatformViewHitTestBehavior HitTestBehavior { get; set; }

    protected override bool SizedByParent => true;

    public override bool AlwaysNeedsCompositing => true;

    public override bool IsRepaintBoundary => true;

    internal PointerEvent? LastPointerDownEvent { get; set; }

    internal UiKitViewGestureRecognizer? GestureRecognizer { get; set; }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        return constraints.Biggest;
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        context.AddLayer(new PlatformViewLayer(rect: new Rect(offset, Size), viewId: _viewController.Id));
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        if (HitTestBehavior == PlatformViewHitTestBehavior.Transparent || !Size.Contains(position))
        {
            return false;
        }

        result.Add(new BoxHitTestEntry(this, position));
        return HitTestBehavior == PlatformViewHitTestBehavior.Opaque;
    }

    protected override bool HitTestSelf(Point position) =>
        HitTestBehavior != PlatformViewHitTestBehavior.Transparent;

    protected override void OnAttach()
    {
        base.OnAttach();
        GestureBinding.Instance.PointerRouter.AddGlobalRoute(HandleGlobalPointerEvent);
    }

    protected override void OnDetach()
    {
        GestureBinding.Instance.PointerRouter.RemoveGlobalRoute(HandleGlobalPointerEvent);
        base.OnDetach();
    }

    private void HandleGlobalPointerEvent(PointerEvent @event)
    {
        if (!HasSize)
        {
            // An attached but not-laid-out box must not be interactive
            // (https://github.com/flutter/flutter/issues/83481).
            return;
        }

        if (@event is not PointerDownEvent)
        {
            return;
        }

        if (!Size.Contains(GlobalToLocal(@event.Position)))
        {
            return;
        }

        if (!ReferenceEquals(@event.Original ?? @event, LastPointerDownEvent))
        {
            // The pointer event is in the bounds of this render box, but we didn't get it in
            // handleEvent. This means that the pointer event was absorbed by a different render
            // object. Since on the platform side the FlutterTouchIntercepting view is seeing all
            // events that are within its bounds we need to tell it to reject the current touch
            // sequence.
            _ = _viewController.RejectGesture();
        }

        LastPointerDownEvent = null;
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        configuration.IsSemanticBoundary = true;
        configuration.PlatformViewId = _viewController.Id;
        configuration.HitTestBehavior = SemanticsHitTestBehavior.Transparent;
    }

    /// <summary>Sets the set of gesture recognizers that should be used for the platform view.</summary>
    public abstract void UpdateGestureRecognizers(
        IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> gestureRecognizers);
}

/// <summary>A render object for an iOS UIKit UIView.</summary>
/// <remarks>
/// Flutter's <c>RenderUiKitView</c>. <see cref="RenderUiKitView"/> is responsible for sizing and
/// displaying an iOS UIView. UIViews are added as subviews of the FlutterView and are composited by
/// Quartz. The viewController is typically generated by
/// <see cref="PlatformViewsRegistry.GetNextPlatformViewId"/>, the UIView must have been created by
/// calling <see cref="PlatformViewsService.InitUiKitView"/>.
/// </remarks>
public class RenderUiKitView : RenderDarwinPlatformView<UiKitViewController>, INativeHitTestTarget
{
    /// <summary>Creates a render object for an iOS UIView.</summary>
    public RenderUiKitView(
        UiKitViewController viewController,
        PlatformViewHitTestBehavior hitTestBehavior,
        IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> gestureRecognizers)
        : base(viewController, hitTestBehavior, gestureRecognizers)
    {
    }

    /// <summary>Sets the set of gesture recognizers that should be used for the platform view.</summary>
    /// <remarks>
    /// Any active gesture arena the `UiKitView` participates in is rejected when the set of gesture
    /// recognizers is changed.
    /// </remarks>
    public override void UpdateGestureRecognizers(
        IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> gestureRecognizers)
    {
        Debug.Assert(
            PlatformViewFactories.FactoriesTypeSet(gestureRecognizers).Count == gestureRecognizers.Count,
            PlatformViewFactories.DuplicateFactoriesMessage);
        if (PlatformViewFactories.FactoryTypesSetEquals(
                gestureRecognizers,
                GestureRecognizer?.GestureRecognizerFactories))
        {
            return;
        }

        GestureRecognizer?.Dispose();
        GestureRecognizer = new UiKitViewGestureRecognizer(ViewController, gestureRecognizers);
    }

    public override void HandleEvent(PointerEvent @event, HitTestEntry entry)
    {
        if (@event is not PointerDownEvent downEvent)
        {
            return;
        }

        GestureRecognizer!.AddPointer(downEvent);
        LastPointerDownEvent = @event.Original ?? @event;
    }

    protected override void OnDetach()
    {
        GestureRecognizer!.Reset();
        base.OnDetach();
    }

    public override void Dispose()
    {
        GestureRecognizer?.Dispose();
        base.Dispose();
    }
}

/// <summary>A render object for a macOS platform view.</summary>
/// <remarks>Flutter's <c>RenderAppKitView</c>.</remarks>
public class RenderAppKitView : RenderDarwinPlatformView<AppKitViewController>
{
    /// <summary>Creates a render object for a macOS AppKitView.</summary>
    public RenderAppKitView(
        AppKitViewController viewController,
        PlatformViewHitTestBehavior hitTestBehavior,
        IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> gestureRecognizers)
        : base(viewController, hitTestBehavior, gestureRecognizers)
    {
    }

    // TODO(schectman): Add gesture functionality to macOS platform view when implemented.
    // https://github.com/flutter/flutter/issues/128519
    // This method will need to behave the same as the same-named method for RenderUiKitView,
    // but use a _AppKitViewGestureRecognizer or equivalent, whose constructor shall accept an
    // AppKitViewController.
    /// <inheritdoc />
    public override void UpdateGestureRecognizers(
        IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> gestureRecognizers)
    {
    }
}

// This recognizer constructs gesture recognizers from a set of gesture recognizer factories it was
// give, adds all of them to a gesture arena team with the _UiKitViewGestureRecognizer as the team
// captain. When the team wins a gesture the recognizer notifies the engine that it should release
// the touch sequence to the embedded UIView.
internal sealed class UiKitViewGestureRecognizer : OneSequenceGestureRecognizer
{
    private readonly HashSet<OneSequenceGestureRecognizer> _gestureRecognizers;

    internal UiKitViewGestureRecognizer(
        UiKitViewController controller,
        IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> gestureRecognizerFactories)
    {
        Controller = controller;
        GestureRecognizerFactories = gestureRecognizerFactories;
        _gestureRecognizers = PlatformViewFactories.CreateTeam(this, gestureRecognizerFactories);
    }

    // We use OneSequenceGestureRecognizers as they support gesture arena teams.
    // TODO(amirh): get a list of GestureRecognizers here.
    // https://github.com/flutter/flutter/issues/20953
    internal IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> GestureRecognizerFactories { get; }

    internal UiKitViewController Controller { get; }

    protected override void AddAllowedPointer(PointerDownEvent @event)
    {
        base.AddAllowedPointer(@event);
        foreach (OneSequenceGestureRecognizer recognizer in _gestureRecognizers)
        {
            recognizer.AddPointer(@event);
        }
    }

    public override string DebugDescription => "UIKit view";

    protected override void DidStopTrackingLastPointer(int pointer)
    {
    }

    protected override void HandleEvent(PointerEvent @event)
    {
        StopTrackingIfPointerNoLongerDown(@event);
    }

    public override void AcceptGesture(int pointer)
    {
        _ = Controller.AcceptGesture();
    }

    public override void RejectGesture(int pointer)
    {
        _ = Controller.RejectGesture();
    }

    internal void Reset()
    {
        Resolve(GestureDisposition.Rejected);
    }
}

// This recognizer constructs gesture recognizers from a set of gesture recognizer factories it was
// give, adds all of them to a gesture arena team with the _PlatformViewGestureRecognizer as the team
// captain. As long as the gesture arena is unresolved, the recognizer caches all pointer events.
// When the team wins, the recognizer sends all the cached pointer events to `_handlePointerEvent`,
// and sets itself to a "forwarding mode" where it will forward any new pointer event to
// `_handlePointerEvent`.
internal sealed class PlatformViewGestureRecognizer : OneSequenceGestureRecognizer
{
    private readonly Func<PointerEvent, Task> _handlePointerEvent;
    private readonly HashSet<OneSequenceGestureRecognizer> _gestureRecognizers;

    internal PlatformViewGestureRecognizer(
        Func<PointerEvent, Task> handlePointerEvent,
        IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> gestureRecognizerFactories)
    {
        GestureRecognizerFactories = gestureRecognizerFactories;
        _gestureRecognizers = PlatformViewFactories.CreateTeam(this, gestureRecognizerFactories);
        _handlePointerEvent = handlePointerEvent;
    }

    // Maps a pointer to a list of its cached pointer events.
    // Before the arena for a pointer is resolved all events are cached here, if we win the arena
    // the cached events are dispatched to `_handlePointerEvent`, if we lose the arena we clear the
    // cache for the pointer.
    internal Dictionary<int, List<PointerEvent>> CachedEvents { get; } = [];

    // Pointer for which we have already won the arena, events for pointers in this set are
    // immediately dispatched to `_handlePointerEvent`.
    internal HashSet<int> ForwardedPointers { get; } = [];

    // We use OneSequenceGestureRecognizers as they support gesture arena teams.
    // TODO(ianh): get a list of GestureRecognizers here.
    // https://github.com/flutter/flutter/issues/20953
    internal IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> GestureRecognizerFactories { get; }

    protected override void AddAllowedPointer(PointerDownEvent @event)
    {
        base.AddAllowedPointer(@event);
        foreach (OneSequenceGestureRecognizer recognizer in _gestureRecognizers)
        {
            recognizer.AddPointer(@event);
        }
    }

    public override string DebugDescription => "Platform view";

    protected override void DidStopTrackingLastPointer(int pointer)
    {
    }

    protected override void HandleEvent(PointerEvent @event)
    {
        if (!ForwardedPointers.Contains(@event.Pointer))
        {
            CacheEvent(@event);
        }
        else
        {
            _ = _handlePointerEvent(@event);
        }

        StopTrackingIfPointerNoLongerDown(@event);
    }

    public override void AcceptGesture(int pointer)
    {
        FlushPointerCache(pointer);
        ForwardedPointers.Add(pointer);
    }

    public override void RejectGesture(int pointer)
    {
        StopTrackingPointer(pointer);
        CachedEvents.Remove(pointer);
    }

    private void CacheEvent(PointerEvent @event)
    {
        if (!CachedEvents.TryGetValue(@event.Pointer, out List<PointerEvent>? events))
        {
            events = [];
            CachedEvents[@event.Pointer] = events;
        }

        events.Add(@event);
    }

    private void FlushPointerCache(int pointer)
    {
        if (CachedEvents.Remove(pointer, out List<PointerEvent>? events))
        {
            foreach (PointerEvent @event in events)
            {
                _ = _handlePointerEvent(@event);
            }
        }
    }

    protected override void StopTrackingPointer(int pointer)
    {
        base.StopTrackingPointer(pointer);
        ForwardedPointers.Remove(pointer);
    }

    internal void Reset()
    {
        foreach (int pointer in ForwardedPointers.ToList())
        {
            base.StopTrackingPointer(pointer);
        }

        ForwardedPointers.Clear();
        foreach (int pointer in CachedEvents.Keys.ToList())
        {
            base.StopTrackingPointer(pointer);
        }

        CachedEvents.Clear();
        Resolve(GestureDisposition.Rejected);
    }
}

/// <summary>A render object for embedding a platform view.</summary>
/// <remarks>
/// Flutter's <c>PlatformViewRenderBox</c>. <see cref="PlatformViewRenderBox"/> presents a platform
/// view by adding a <see cref="PlatformViewLayer"/> layer, integrates it with the gesture arenas
/// system and adds relevant semantic nodes to the semantics tree. Dart's private
/// <c>_PlatformViewGestureMixin</c> has this class as its only user, so its members are declared
/// here.
/// </remarks>
public class PlatformViewRenderBox : RenderBox, IMouseTrackerAnnotation
{
    private PlatformViewController _controller;
    private PlatformViewHitTestBehavior? _hitTestBehavior;
    private Func<PointerEvent, Task>? _handlePointerEvent;
    private PlatformViewGestureRecognizer? _gestureRecognizer;

    /// <summary>Creating a render object for a <c>PlatformViewSurface</c>.</summary>
    public PlatformViewRenderBox(
        PlatformViewController controller,
        PlatformViewHitTestBehavior hitTestBehavior,
        IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> gestureRecognizers)
    {
        Debug.Assert(controller.ViewId > -1);
        _controller = controller;
        HitTestBehavior = hitTestBehavior;
        UpdateGestureRecognizers(gestureRecognizers);
    }

    /// <summary>The controller for this render object.</summary>
    public virtual PlatformViewController Controller
    {
        get => _controller;
        set
        {
            Debug.Assert(value.ViewId > -1);

            if (ReferenceEquals(_controller, value))
            {
                return;
            }

            bool needsSemanticsUpdate = _controller.ViewId != value.ViewId;
            _controller = value;
            MarkNeedsPaint();
            if (needsSemanticsUpdate)
            {
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    /// <summary>
    /// Sets the set of gesture recognizers that should be used for the platform view.
    /// </summary>
    /// <remarks>
    /// Any active gesture arena the `PlatformView` participates in is rejected when the set of gesture
    /// recognizers is changed.
    /// </remarks>
    public void UpdateGestureRecognizers(IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> gestureRecognizers)
    {
        UpdateGestureRecognizersWithCallBack(gestureRecognizers, _controller.DispatchPointerEvent);
    }

    protected override bool SizedByParent => true;

    public override bool AlwaysNeedsCompositing => true;

    public override bool IsRepaintBoundary => true;

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        return constraints.Biggest;
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        context.AddLayer(new PlatformViewLayer(rect: new Rect(offset, Size), viewId: _controller.ViewId));
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        configuration.IsSemanticBoundary = true;
        configuration.PlatformViewId = _controller.ViewId;
        configuration.HitTestBehavior = SemanticsHitTestBehavior.Transparent;
    }

    // ---- Dart's `_PlatformViewGestureMixin` ----

    /// <summary>How to behave during hit testing.</summary>
    /// <remarks>Dart's mixin declares a setter only.</remarks>
    public PlatformViewHitTestBehavior HitTestBehavior
    {
        set
        {
            if (value != _hitTestBehavior)
            {
                _hitTestBehavior = value;
                if (Owner is not null)
                {
                    MarkNeedsPaint();
                }
            }
        }
    }

    /// <summary>
    /// Any active gesture arena the `PlatformView` participates in is rejected when the set of gesture
    /// recognizers is changed.
    /// </summary>
    private void UpdateGestureRecognizersWithCallBack(
        IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> gestureRecognizers,
        Func<PointerEvent, Task> handlePointerEvent)
    {
        Debug.Assert(
            PlatformViewFactories.FactoriesTypeSet(gestureRecognizers).Count == gestureRecognizers.Count,
            PlatformViewFactories.DuplicateFactoriesMessage);
        if (PlatformViewFactories.FactoryTypesSetEquals(
                gestureRecognizers,
                _gestureRecognizer?.GestureRecognizerFactories))
        {
            return;
        }

        _gestureRecognizer?.Dispose();
        _gestureRecognizer = new PlatformViewGestureRecognizer(handlePointerEvent, gestureRecognizers);
        _handlePointerEvent = handlePointerEvent;
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        if (_hitTestBehavior == PlatformViewHitTestBehavior.Transparent || !Size.Contains(position))
        {
            return false;
        }

        result.Add(new BoxHitTestEntry(this, position));
        return _hitTestBehavior == PlatformViewHitTestBehavior.Opaque;
    }

    protected override bool HitTestSelf(Point position) =>
        _hitTestBehavior != PlatformViewHitTestBehavior.Transparent;

    /// <inheritdoc />
    public PointerEnterEventListener? OnEnter => null;

    /// <inheritdoc />
    public PointerExitEventListener? OnExit => null;

    /// <inheritdoc />
    public MouseCursor Cursor => Constants.KIsWeb ? MouseCursor.Defer : MouseCursor.Uncontrolled;

    /// <inheritdoc />
    public bool ValidForMouseTracker => true;

    public override void HandleEvent(PointerEvent @event, HitTestEntry entry)
    {
        if (@event is PointerDownEvent downEvent)
        {
            _gestureRecognizer!.AddPointer(downEvent);
        }

        if (@event is PointerHoverEvent)
        {
            _ = _handlePointerEvent?.Invoke(@event);
        }
    }

    protected override void OnDetach()
    {
        _gestureRecognizer!.Reset();
        base.OnDetach();
    }

    public override void Dispose()
    {
        _gestureRecognizer?.Dispose();
        base.Dispose();
    }
}
