using System.Globalization;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Physics;
using Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/draggable_scrollable_sheet.dart

namespace Plumix.Widgets;

/// <summary>
/// The signature of a widget builder that creates a draggable scrollable sheet, receiving the
/// controller that must be handed to the scrollable the sheet drives.
/// </summary>
public delegate Widget ScrollableWidgetBuilder(BuildContext context, ScrollController scrollController);

/// <summary>
/// Controls a <see cref="DraggableScrollableSheet"/>, and reports its size changes to listeners.
/// </summary>
/// <remarks>
/// A controller notifies its listeners whenever the attached sheet changes size; it does not notify
/// on attach, nor when a parameter change leaves the current size untouched.
/// </remarks>
public class DraggableScrollableController : ChangeNotifier
{
    private readonly HashSet<AnimationController> _animationControllers = [];
    private DraggableScrollableSheetScrollController? _attachedController;

    /// <summary>The current size of the attached sheet, as a fraction of the available height.</summary>
    public double Size
    {
        get
        {
            AssertAttached();
            return _attachedController!.Extent.CurrentSize;
        }
    }

    /// <summary>The current height of the attached sheet, in logical pixels.</summary>
    public double Pixels
    {
        get
        {
            AssertAttached();
            return _attachedController!.Extent.CurrentPixels;
        }
    }

    /// <summary>Converts a sheet size fraction into logical pixels.</summary>
    public double SizeToPixels(double size)
    {
        AssertAttached();
        return _attachedController!.Extent.SizeToPixels(size);
    }

    /// <summary>Whether this controller is attached to a sheet that has a live scroll position.</summary>
    public bool IsAttached => _attachedController != null && _attachedController.HasClients;

    /// <summary>Converts a height in logical pixels into a sheet size fraction.</summary>
    public double PixelsToSize(double pixels)
    {
        AssertAttached();
        return _attachedController!.Extent.PixelsToSize(pixels);
    }

    /// <summary>
    /// Animates the attached sheet from its current size to the given size. Snapping is disabled for
    /// the duration, and resumes at the next user interaction.
    /// </summary>
    /// <returns>
    /// The animation's <see cref="TickerFuture"/>. Its <see cref="TickerFuture.Task"/> resolves only
    /// when the animation runs to completion, exactly as Dart's returned future does;
    /// <see cref="TickerFuture.OrCancel"/> faults with <see cref="TickerCanceled"/> when a drag,
    /// another animation or a detach interrupts it.
    /// </returns>
    public TickerFuture AnimateTo(double size, TimeSpan duration, Curve curve)
    {
        AssertAttached();
        ArgumentNullException.ThrowIfNull(curve);
        if (size is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "size must be between 0.0 and 1.0.");
        }
        if (duration == TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "duration must not be zero.");
        }

        DraggableScrollableSheetScrollController attached = _attachedController!;
        var animationController = AnimationController.Unbounded(
            value: attached.Extent.CurrentSize,
            vsync: attached.Position.Context.Vsync);
        _animationControllers.Add(animationController);
        attached.Position.GoIdle();
        // Disables snapping until the next user interaction.
        attached.Extent.HasDragged = false;
        attached.Extent.HasChanged = true;
        attached.Extent.StartActivity(onCanceled: () =>
        {
            // The controller may already have been disposed by a detach.
            if (animationController.IsAnimating)
            {
                animationController.Stop();
            }
        });
        animationController.AddListener(() =>
            attached.Extent.UpdateSize(animationController.Value, attached.Position.Context.NotificationContext));

        return animationController.AnimateTo(
            Math.Clamp(size, attached.Extent.MinSize, attached.Extent.MaxSize),
            duration,
            curve);
    }

    /// <summary>Jumps the attached sheet to the given size without animating.</summary>
    public void JumpTo(double size)
    {
        AssertAttached();
        if (size is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "size must be between 0.0 and 1.0.");
        }

        // Call start activity to interrupt any other playing activities.
        _attachedController!.Extent.StartActivity(onCanceled: () => { });
        _attachedController.Position.GoIdle();
        _attachedController.Extent.HasDragged = false;
        _attachedController.Extent.HasChanged = true;
        _attachedController.Extent.UpdateSize(size, _attachedController.Position.Context.NotificationContext);
    }

    /// <summary>Returns the attached sheet to its initial size, without snapping away from it.</summary>
    public void Reset()
    {
        AssertAttached();
        _attachedController!.Reset();
    }

    private void AssertAttached()
    {
        if (!IsAttached)
        {
            throw new InvalidOperationException(
                "DraggableScrollableController is not attached to a sheet. A DraggableScrollableController "
                + "must be used in a DraggableScrollableSheet before any of its methods are called.");
        }
    }

    internal void Attach(DraggableScrollableSheetScrollController scrollController)
    {
        if (_attachedController != null)
        {
            throw new InvalidOperationException(
                "Draggable scrollable controller is already attached to a sheet.");
        }

        _attachedController = scrollController;
        _attachedController.Extent.CurrentSizeListenable.AddListener(NotifyListeners);
        _attachedController.OnPositionDetached = DisposeAnimationControllers;
    }

    internal void OnExtentReplaced(DraggableSheetExtent previousExtent)
    {
        // We can't get the size from the previous extent's listenable, so notify manually when the
        // replacement changed the current size.
        _attachedController!.Extent.CurrentSizeListenable.AddListener(NotifyListeners);
        if (previousExtent.CurrentSize != _attachedController.Extent.CurrentSize)
        {
            NotifyListeners();
        }
    }

    internal void Detach(bool disposeExtent = false)
    {
        if (disposeExtent)
        {
            _attachedController?.Extent.Dispose();
        }
        else
        {
            _attachedController?.Extent.CurrentSizeListenable.RemoveListener(NotifyListeners);
        }

        DisposeAnimationControllers();
        _attachedController = null;
    }

    private void DisposeAnimationControllers()
    {
        foreach (var animationController in _animationControllers.ToArray())
        {
            animationController.Dispose();
        }

        _animationControllers.Clear();
    }
}

/// <summary>
/// A container for a scrollable that grows and shrinks as the user drags it, letting the scrollable
/// take over once the sheet has reached its maximum size.
/// </summary>
public sealed class DraggableScrollableSheet : StatefulWidget
{
    public DraggableScrollableSheet(
        ScrollableWidgetBuilder builder,
        double initialChildSize = 0.5,
        double minChildSize = 0.25,
        double maxChildSize = 1.0,
        bool expand = true,
        bool snap = false,
        IReadOnlyList<double>? snapSizes = null,
        TimeSpan? snapAnimationDuration = null,
        DraggableScrollableController? controller = null,
        bool shouldCloseOnMinExtent = true,
        Key? key = null) : base(key)
    {
        if (minChildSize < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(minChildSize), "minChildSize must be at least 0.0.");
        }
        if (maxChildSize > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxChildSize), "maxChildSize must be at most 1.0.");
        }
        if (minChildSize > initialChildSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialChildSize),
                "initialChildSize must be at least minChildSize.");
        }
        if (initialChildSize > maxChildSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialChildSize),
                "initialChildSize must be at most maxChildSize.");
        }
        if (snapAnimationDuration is { } duration && duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(snapAnimationDuration),
                "snapAnimationDuration must be positive.");
        }

        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        InitialChildSize = initialChildSize;
        MinChildSize = minChildSize;
        MaxChildSize = maxChildSize;
        Expand = expand;
        Snap = snap;
        SnapSizes = snapSizes;
        SnapAnimationDuration = snapAnimationDuration;
        Controller = controller;
        ShouldCloseOnMinExtent = shouldCloseOnMinExtent;
    }

    /// <summary>The initial fractional value of the parent container's height to use when displaying.</summary>
    public double InitialChildSize { get; }

    /// <summary>The minimum fractional value of the parent container's height to use when displaying.</summary>
    public double MinChildSize { get; }

    /// <summary>The maximum fractional value of the parent container's height to use when displaying.</summary>
    public double MaxChildSize { get; }

    /// <summary>Whether the widget should expand to fill the available space in its parent or not.</summary>
    public bool Expand { get; }

    /// <summary>Whether the widget should snap between <see cref="SnapSizes"/> when released.</summary>
    public bool Snap { get; }

    /// <summary>Sizes the sheet snaps to when released, in addition to min and max.</summary>
    public IReadOnlyList<double>? SnapSizes { get; }

    /// <summary>Fixed duration of the snapping animation, or null to derive it from the fling velocity.</summary>
    public TimeSpan? SnapAnimationDuration { get; }

    /// <summary>The controller that can be used to programmatically size and observe this sheet.</summary>
    public DraggableScrollableController? Controller { get; }

    /// <summary>
    /// Whether a listener of the emitted notification should close the sheet when it reaches
    /// <see cref="MinChildSize"/>.
    /// </summary>
    public bool ShouldCloseOnMinExtent { get; }

    /// <summary>Builds the widget that this sheet drags and scrolls.</summary>
    public ScrollableWidgetBuilder Builder { get; }

    public override State CreateState() => new DraggableScrollableSheetState();
}

/// <summary>
/// A <see cref="Notification"/> related to the extent, which is the size, and scroll offset, which
/// is the position of the child list, of a <see cref="DraggableScrollableSheet"/>.
/// </summary>
public class DraggableScrollableNotification : Notification, IViewportNotification
{
    public DraggableScrollableNotification(
        double extent,
        double minExtent,
        double maxExtent,
        double initialExtent,
        BuildContext context,
        bool shouldCloseOnMinExtent = true)
    {
        if (minExtent < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(minExtent), "minExtent must be at least 0.0.");
        }
        if (maxExtent > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExtent), "maxExtent must be at most 1.0.");
        }
        if (minExtent > extent || extent > maxExtent)
        {
            throw new ArgumentOutOfRangeException(nameof(extent), "extent must be between minExtent and maxExtent.");
        }
        if (minExtent > initialExtent || initialExtent > maxExtent)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialExtent),
                "initialExtent must be between minExtent and maxExtent.");
        }

        Extent = extent;
        MinExtent = minExtent;
        MaxExtent = maxExtent;
        InitialExtent = initialExtent;
        ShouldCloseOnMinExtent = shouldCloseOnMinExtent;
        SetContext(context);
        SourceContext = context;
    }

    /// <summary>The current value of the extent, between <see cref="MinExtent"/> and <see cref="MaxExtent"/>.</summary>
    public double Extent { get; }

    /// <summary>The minimum value of the extent.</summary>
    public double MinExtent { get; }

    /// <summary>The maximum value of the extent.</summary>
    public double MaxExtent { get; }

    /// <summary>The initial value of the extent.</summary>
    public double InitialExtent { get; }

    /// <summary>The build context of the widget that fired this notification.</summary>
    public BuildContext SourceContext { get; }

    /// <summary>Whether a listener should close the sheet when the extent reaches the minimum.</summary>
    public bool ShouldCloseOnMinExtent { get; }

    /// <summary>How many viewports this notification has bubbled through; zero for the nearest sheet.</summary>
    public int Depth { get; private set; }

    void IViewportNotification.IncrementDepth()
    {
        Depth += 1;
    }

    public override string ToString()
    {
        return $"{GetType().Name}(minExtent: {MinExtent}, extent: {Extent}, maxExtent: {MaxExtent}, "
               + $"initialExtent: {InitialExtent})";
    }
}

/// <summary>
/// A widget that can notify a descendant <see cref="DraggableScrollableSheet"/> that it should reset
/// its position to its initial state.
/// </summary>
public sealed class DraggableScrollableActuator : StatefulWidget
{
    public DraggableScrollableActuator(Widget child, Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    /// <summary>This widget's child.</summary>
    public Widget Child { get; }

    /// <summary>
    /// Notifies any descendant <see cref="DraggableScrollableSheet"/> that it should reset to its
    /// initial position.
    /// </summary>
    /// <returns>
    /// Whether an actuator was found and a sheet was listening; false means nothing was reset.
    /// </returns>
    public static bool Reset(BuildContext context)
    {
        var notifier = context.DependOnInherited<InheritedResetNotifier>();
        return notifier?.SendReset() ?? false;
    }

    public override State CreateState() => new DraggableScrollableActuatorState();

    private sealed class DraggableScrollableActuatorState : State
    {
        private readonly ResetNotifier _notifier = new();

        private DraggableScrollableActuator CurrentWidget => (DraggableScrollableActuator)StateWidget;

        public override Widget Build(BuildContext context)
        {
            return new InheritedResetNotifier(notifier: _notifier, child: CurrentWidget.Child);
        }

        public override void Dispose()
        {
            _notifier.Dispose();
        }
    }
}

/// <summary>A <see cref="ChangeNotifier"/> that carries a one-shot reset request.</summary>
internal sealed class ResetNotifier : ChangeNotifier
{
    /// <summary>Whether someone called <see cref="SendReset"/>; must be cleared after being read.</summary>
    public bool WasCalled { get; set; }

    public bool SendReset()
    {
        if (!HasListeners)
        {
            return false;
        }

        WasCalled = true;
        NotifyListeners();
        return true;
    }
}

internal sealed class InheritedResetNotifier : InheritedNotifier<ResetNotifier>
{
    public InheritedResetNotifier(ResetNotifier notifier, Widget child, Key? key = null)
        : base(notifier, child, key)
    {
    }

    /// <summary>
    /// Whether the descendant sheet should reset, clearing the request as it is read. Establishing a
    /// dependency here is what subscribes the sheet to the actuator.
    /// </summary>
    internal static bool ShouldReset(BuildContext context)
    {
        var widget = context.DependOnInherited<InheritedResetNotifier>();
        if (widget?.Notifier is not { } notifier)
        {
            return false;
        }

        bool wasCalled = notifier.WasCalled;
        notifier.WasCalled = false;
        return wasCalled;
    }

    internal bool SendReset() => Notifier!.SendReset();
}

internal sealed class DraggableScrollableSheetState : State
{
    private DraggableSheetExtent _extent = null!;
    private DraggableScrollableSheetScrollController _scrollController = null!;

    private DraggableScrollableSheet CurrentWidget => (DraggableScrollableSheet)StateWidget;

    public override void InitState()
    {
        _extent = new DraggableSheetExtent(
            minSize: CurrentWidget.MinChildSize,
            maxSize: CurrentWidget.MaxChildSize,
            snap: CurrentWidget.Snap,
            snapSizes: ImpliedSnapSizes(),
            snapAnimationDuration: CurrentWidget.SnapAnimationDuration,
            initialSize: CurrentWidget.InitialChildSize,
            shouldCloseOnMinExtent: CurrentWidget.ShouldCloseOnMinExtent);
        _scrollController = new DraggableScrollableSheetScrollController(_extent);
        CurrentWidget.Controller?.Attach(_scrollController);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var previous = (DraggableScrollableSheet)oldWidget;
        if (!ReferenceEquals(previous.Controller, CurrentWidget.Controller))
        {
            previous.Controller?.Detach();
            CurrentWidget.Controller?.Attach(_scrollController);
        }

        ReplaceExtent(previous);
    }

    public override void DidChangeDependencies()
    {
        if (InheritedResetNotifier.ShouldReset(Context))
        {
            _scrollController.Reset();
        }
    }

    public override Widget Build(BuildContext context)
    {
        return new ValueListenableBuilder<double>(
            valueListenable: _extent.CurrentSizeListenable,
            // The user builder is hoisted into `child` so that resizing the sheet does not rebuild it.
            child: CurrentWidget.Builder(context, _scrollController),
            builder: (_, currentSize, child) => new LayoutBuilder((_, constraints) =>
            {
                _extent.AvailablePixels = CurrentWidget.MaxChildSize * constraints.Biggest.Height;
                Widget sheet = new FractionallySizedBox(
                    heightFactor: currentSize,
                    alignment: Alignment.BottomCenter,
                    child: child);
                return CurrentWidget.Expand
                    ? new SizedBox(
                        width: double.PositiveInfinity,
                        height: double.PositiveInfinity,
                        child: sheet)
                    : sheet;
            }));
    }

    public override void Dispose()
    {
        if (CurrentWidget.Controller == null)
        {
            _extent.Dispose();
        }
        else
        {
            CurrentWidget.Controller.Detach(disposeExtent: true);
        }

        _scrollController.Dispose();
    }

    private List<double> ImpliedSnapSizes()
    {
        IReadOnlyList<double> snapSizes = CurrentWidget.SnapSizes ?? [];
        for (int index = 0; index < snapSizes.Count; index++)
        {
            double snapSize = snapSizes[index];
            if (snapSize < CurrentWidget.MinChildSize || snapSize > CurrentWidget.MaxChildSize)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(DraggableScrollableSheet.SnapSizes),
                    $"{SnapSizeErrorMessage(index)}\nSnap sizes must be between `minChildSize` and `maxChildSize`. ");
            }
            if (index != 0 && snapSize <= snapSizes[index - 1])
            {
                throw new ArgumentOutOfRangeException(
                    nameof(DraggableScrollableSheet.SnapSizes),
                    $"{SnapSizeErrorMessage(index)}\nSnap sizes must be in ascending order. ");
            }
        }

        if (snapSizes.Count == 0)
        {
            return [CurrentWidget.MinChildSize, CurrentWidget.MaxChildSize];
        }

        List<double> implied = [];
        if (snapSizes[0] != CurrentWidget.MinChildSize)
        {
            implied.Add(CurrentWidget.MinChildSize);
        }

        implied.AddRange(snapSizes);
        if (snapSizes[^1] != CurrentWidget.MaxChildSize)
        {
            implied.Add(CurrentWidget.MaxChildSize);
        }

        return implied;
    }

    private string SnapSizeErrorMessage(int invalidIndex)
    {
        IReadOnlyList<double> snapSizes = CurrentWidget.SnapSizes!;
        string[] snapSizesWithIndicator = new string[snapSizes.Count];
        for (int index = 0; index < snapSizes.Count; index++)
        {
            string snapSizeString = snapSizes[index].ToString(CultureInfo.InvariantCulture);
            snapSizesWithIndicator[index] = index == invalidIndex ? $">>> {snapSizeString} <<<" : snapSizeString;
        }

        return $"Invalid snapSize '{snapSizes[invalidIndex].ToString(CultureInfo.InvariantCulture)}' "
               + $"at index {invalidIndex} of:\n"
               + $"  [{string.Join(", ", snapSizesWithIndicator)}]";
    }

    private void ReplaceExtent(DraggableScrollableSheet oldWidget)
    {
        DraggableSheetExtent previousExtent = _extent;
        _extent = previousExtent.CopyWith(
            minSize: CurrentWidget.MinChildSize,
            maxSize: CurrentWidget.MaxChildSize,
            snap: CurrentWidget.Snap,
            snapSizes: ImpliedSnapSizes(),
            snapAnimationDuration: CurrentWidget.SnapAnimationDuration,
            initialSize: CurrentWidget.InitialChildSize,
            shouldCloseOnMinExtent: CurrentWidget.ShouldCloseOnMinExtent);
        // Modify the existing scroll controller instead of replacing it, so that descendant widgets
        // do not lose their scroll position when this widget rebuilds.
        _scrollController.Extent = _extent;
        CurrentWidget.Controller?.OnExtentReplaced(previousExtent);
        previousExtent.Dispose();

        if (!CurrentWidget.Snap
            || (CurrentWidget.Snap == oldWidget.Snap
                && ReferenceEquals(CurrentWidget.SnapSizes, oldWidget.SnapSizes))
            || !_scrollController.HasClients)
        {
            return;
        }

        // Trigger a snap in case snap or snapSizes has changed, deferred so that the sheet has a
        // chance to build and set the new extent's available pixels first.
        Scheduler.AddPostFrameCallback(_ =>
        {
            foreach (var position in _scrollController.Positions.ToArray())
            {
                ((DraggableScrollableSheetScrollPosition)position).GoBallistic(0);
            }
        });
    }
}

/// <summary>
/// Manages state between <see cref="DraggableScrollableSheetState"/>,
/// <see cref="DraggableScrollableSheetScrollController"/>, and
/// <see cref="DraggableScrollableSheetScrollPosition"/>.
/// </summary>
internal sealed class DraggableSheetExtent
{
    private readonly ValueNotifier<double> _currentSize;
    private Action? _cancelActivity;

    public DraggableSheetExtent(
        double minSize,
        double maxSize,
        bool snap,
        IReadOnlyList<double> snapSizes,
        double initialSize,
        TimeSpan? snapAnimationDuration = null,
        ValueNotifier<double>? currentSize = null,
        bool? hasDragged = null,
        bool? hasChanged = null,
        bool shouldCloseOnMinExtent = true)
    {
        if (minSize < 0.0 || maxSize > 1.0 || minSize > initialSize || initialSize > maxSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialSize),
                "Requires 0 <= minSize <= initialSize <= maxSize <= 1.");
        }

        MinSize = minSize;
        MaxSize = maxSize;
        Snap = snap;
        SnapSizes = snapSizes;
        SnapAnimationDuration = snapAnimationDuration;
        InitialSize = initialSize;
        ShouldCloseOnMinExtent = shouldCloseOnMinExtent;
        _currentSize = currentSize ?? new ValueNotifier<double>(initialSize);
        AvailablePixels = double.PositiveInfinity;
        HasDragged = hasDragged ?? false;
        HasChanged = hasChanged ?? false;
    }

    public double MinSize { get; }

    public double MaxSize { get; }

    public bool Snap { get; }

    public IReadOnlyList<double> SnapSizes { get; }

    public TimeSpan? SnapAnimationDuration { get; }

    public double InitialSize { get; }

    /// <summary>Whether a listener should close the sheet when it reaches <see cref="MinSize"/>.</summary>
    public bool ShouldCloseOnMinExtent { get; }

    /// <summary>The pixels the sheet can grow into; scaled by <see cref="MaxSize"/>.</summary>
    public double AvailablePixels { get; set; }

    /// <summary>Whether the sheet has been dragged since it was created or last reset.</summary>
    public bool HasDragged { get; set; }

    /// <summary>Whether the sheet has changed size at all since it was created or last reset.</summary>
    public bool HasChanged { get; set; }

    public IValueListenable<double> CurrentSizeListenable => _currentSize;

    public bool IsAtMin => MinSize >= _currentSize.Value;

    public bool IsAtMax => MaxSize <= _currentSize.Value;

    public double CurrentSize => _currentSize.Value;

    public double CurrentPixels => SizeToPixels(_currentSize.Value);

    public IReadOnlyList<double> PixelSnapSizes
    {
        get
        {
            double[] pixelSnapSizes = new double[SnapSizes.Count];
            for (int index = 0; index < SnapSizes.Count; index++)
            {
                pixelSnapSizes[index] = SizeToPixels(SnapSizes[index]);
            }

            return pixelSnapSizes;
        }
    }

    public double PixelsToSize(double pixels) => pixels / AvailablePixels * MaxSize;

    public double SizeToPixels(double size) => size / MaxSize * AvailablePixels;

    /// <summary>
    /// Registers the callback that cancels the activity now driving the sheet, cancelling whichever
    /// activity was running before.
    /// </summary>
    public void StartActivity(Action onCanceled)
    {
        _cancelActivity?.Invoke();
        _cancelActivity = onCanceled;
    }

    /// <summary>Cancels the activity currently driving the sheet, if any.</summary>
    public void CancelActivity() => _cancelActivity?.Invoke();

    /// <summary>
    /// Changes the size of the sheet by the given number of pixels, cancelling any running activity:
    /// a drag or ballistic tick always wins over a programmatic animation.
    /// </summary>
    public void AddPixelDelta(double delta, BuildContext? context)
    {
        _cancelActivity?.Invoke();
        _cancelActivity = null;
        HasDragged = true;
        HasChanged = true;
        if (AvailablePixels == 0)
        {
            return;
        }

        UpdateSize(CurrentSize + PixelsToSize(delta), context);
    }

    /// <summary>
    /// Sets the size of the sheet, clamped into range, and dispatches a
    /// <see cref="DraggableScrollableNotification"/> when the size actually changed.
    /// </summary>
    public void UpdateSize(double newSize, BuildContext? context)
    {
        double clampedSize = Math.Clamp(newSize, MinSize, MaxSize);
        if (_currentSize.Value == clampedSize)
        {
            return;
        }

        _currentSize.Value = clampedSize;
        if (context is not BuildContext target)
        {
            return;
        }

        new DraggableScrollableNotification(
            minExtent: MinSize,
            maxExtent: MaxSize,
            extent: CurrentSize,
            initialExtent: InitialSize,
            context: target,
            shouldCloseOnMinExtent: ShouldCloseOnMinExtent).Dispatch(target);
    }

    public void Dispose() => _currentSize.Dispose();

    public DraggableSheetExtent CopyWith(
        double minSize,
        double maxSize,
        bool snap,
        IReadOnlyList<double> snapSizes,
        double initialSize,
        TimeSpan? snapAnimationDuration,
        bool shouldCloseOnMinExtent)
    {
        return new DraggableSheetExtent(
            minSize: minSize,
            maxSize: maxSize,
            snap: snap,
            snapSizes: snapSizes,
            snapAnimationDuration: snapAnimationDuration,
            initialSize: initialSize,
            // Set the current size to the possibly updated initial size if the sheet has not changed
            // yet; otherwise the current size is preserved, clamped into the new bounds.
            currentSize: new ValueNotifier<double>(
                HasChanged ? Math.Clamp(_currentSize.Value, minSize, maxSize) : initialSize),
            hasDragged: HasDragged,
            hasChanged: HasChanged,
            shouldCloseOnMinExtent: shouldCloseOnMinExtent);
    }
}

internal sealed class DraggableScrollableSheetScrollController : ScrollController
{
    public DraggableScrollableSheetScrollController(DraggableSheetExtent extent)
    {
        Extent = extent;
    }

    public DraggableSheetExtent Extent { get; set; }

    /// <summary>Invoked when a position detaches, so an attached controller can drop its animations.</summary>
    public Action? OnPositionDetached { get; set; }

    public new DraggableScrollableSheetScrollPosition Position =>
        (DraggableScrollableSheetScrollPosition)base.Position;

    public override ScrollPosition CreateScrollPosition(
        ScrollPhysics physics,
        IScrollContext context,
        ScrollPosition? oldPosition)
    {
        return new DraggableScrollableSheetScrollPosition(
            // The sheet always accepts a drag: it resizes when its child cannot scroll further.
            physics: physics.ApplyTo(new AlwaysScrollableScrollPhysics()),
            context: context,
            initialPixels: InitialScrollOffset,
            oldPosition: oldPosition,
            // Resolved late, so replacing the extent is transparent to a live position.
            getExtent: () => Extent);
    }

    public void Reset()
    {
        Extent.CancelActivity();
        Extent.HasDragged = false;
        Extent.HasChanged = false;
        // Jumping can result in trying to replace semantics during build; animate very fast instead.
        if (Offset != 0.0)
        {
            AnimateTo(0.0, TimeSpan.FromMilliseconds(1), Curves.Linear);
        }

        Extent.UpdateSize(Extent.InitialSize, Position.Context.NotificationContext);
    }

    internal override void Detach(ScrollPosition position)
    {
        OnPositionDetached?.Invoke();
        base.Detach(position);
    }
}

internal sealed class DraggableScrollableSheetScrollPosition : ScrollPosition
{
    private readonly HashSet<AnimationController> _ballisticControllers = [];
    private readonly Func<DraggableSheetExtent> _getExtent;
    private Action? _dragCancelCallback;

    public DraggableScrollableSheetScrollPosition(
        ScrollPhysics physics,
        IScrollContext context,
        Func<DraggableSheetExtent> getExtent,
        double initialPixels = 0.0,
        ScrollPosition? oldPosition = null)
        : base(physics, context, initialPixels, oldPosition: oldPosition)
    {
        _getExtent = getExtent;
    }

    /// <summary>Whether the child list, rather than the sheet, should consume a drag.</summary>
    public bool ListShouldScroll => Pixels > 0.0;

    public DraggableSheetExtent Extent => _getExtent();

    /// <summary>How many ballistic or snap animations this position is currently running.</summary>
    public int BallisticControllerCount => _ballisticControllers.Count;

    public override void Absorb(ScrollPosition other)
    {
        base.Absorb(other);
        if (other is not DraggableScrollableSheetScrollPosition sheetPosition)
        {
            return;
        }

        // Cancelling a drag on the old position would leave the drag gesture unresolved, so the
        // callback moves across with the drag itself.
        if (sheetPosition._dragCancelCallback != null)
        {
            _dragCancelCallback = sheetPosition._dragCancelCallback;
            sheetPosition._dragCancelCallback = null;
        }
    }

    internal override void BeginActivity(ScrollActivity activity)
    {
        // Any new activity supersedes the sheet's own ballistic and snap animations.
        foreach (var ballisticController in _ballisticControllers.ToArray())
        {
            ballisticController.Stop();
        }

        base.BeginActivity(activity);
    }

    public override void ApplyUserOffset(double delta)
    {
        if (!ListShouldScroll
            && (!(Extent.IsAtMin || Extent.IsAtMax)
                || (Extent.IsAtMin && delta < 0)
                || (Extent.IsAtMax && delta > 0)))
        {
            Extent.AddPixelDelta(-delta, Context.NotificationContext);
        }
        else
        {
            base.ApplyUserOffset(delta);
        }
    }

    public override ScrollDragController Drag(DragStartDetails details, Action? dragCancelCallback = null)
    {
        // Save this so we can call it in goBallistic when the sheet takes the ballistic over itself.
        _dragCancelCallback = dragCancelCallback;
        return base.Drag(details, dragCancelCallback);
    }

    public override void GoBallistic(double velocity)
    {
        if ((velocity == 0.0 && !ShouldSnap())
            || (velocity < 0.0 && ListShouldScroll)
            || (velocity > 0.0 && Extent.IsAtMax))
        {
            base.GoBallistic(velocity);
            return;
        }

        // Scrollable expects that we will dispose of its current drag.
        _dragCancelCallback?.Invoke();
        _dragCancelCallback = null;

        Tolerance tolerance = Physics.ToleranceFor(this);
        Simulation simulation = Extent.Snap
            ? new SnappingSimulation(
                position: Extent.CurrentPixels,
                initialVelocity: velocity,
                pixelSnapSizes: Extent.PixelSnapSizes,
                snapAnimationDuration: Extent.SnapAnimationDuration,
                tolerance: tolerance)
            // Deliberately clamping even on iOS: bouncing is wrong while the sheet itself moves, and
            // the correct simulation is used again once the ballistic is handed back to the list.
            : new ClampingScrollSimulation(
                position: Extent.CurrentPixels,
                velocity: velocity,
                tolerance: tolerance);

        var ballisticController = AnimationController.Unbounded(vsync: Context.Vsync);
        _ballisticControllers.Add(ballisticController);
        double lastPosition = Extent.CurrentPixels;
        double currentVelocity = velocity;

        void Tick()
        {
            double delta = ballisticController.Value - lastPosition;
            lastPosition = ballisticController.Value;
            Extent.AddPixelDelta(delta, Context.NotificationContext);
            if ((currentVelocity > 0 && Extent.IsAtMax) || (currentVelocity < 0 && Extent.IsAtMin))
            {
                // Make sure we pass along enough velocity to keep scrolling, rather than bouncing.
                currentVelocity = ballisticController.Velocity
                                  + (tolerance.Velocity * Math.Sign(ballisticController.Velocity));
                base.GoBallistic(currentVelocity);
                ballisticController.Stop();
            }
            else if (ballisticController.Status.IsCompleted())
            {
                // Erase the rounding error the ticks accumulated, so the sheet can land exactly on
                // its snap size rather than a hair away from it.
                if (GetCurrentSnapSize() is { } snapSize)
                {
                    Extent.UpdateSize(snapSize, Context.NotificationContext);
                }

                base.GoBallistic(0);
            }
        }

        ballisticController.AddListener(Tick);
        ballisticController.AnimateWith(simulation).WhenCompleteOrCancel(() =>
        {
            if (_ballisticControllers.Remove(ballisticController))
            {
                ballisticController.Dispose();
            }
        });
    }

    public override void Dispose()
    {
        foreach (var ballisticController in _ballisticControllers.ToArray())
        {
            ballisticController.Dispose();
        }

        _ballisticControllers.Clear();
        base.Dispose();
    }

    /// <summary>The snap size the sheet currently sits on, within the physics' distance tolerance.</summary>
    private double? GetCurrentSnapSize()
    {
        double toleranceInSize = Extent.PixelsToSize(Physics.ToleranceFor(this).Distance);
        foreach (double snapSize in Extent.SnapSizes)
        {
            if (Math.Abs(Extent.CurrentSize - snapSize) <= toleranceInSize)
            {
                return snapSize;
            }
        }

        return null;
    }

    private bool IsAtSnapSize() => GetCurrentSnapSize() != null;

    private bool ShouldSnap() => Extent.Snap && Extent.HasDragged && !IsAtSnapSize();
}

/// <summary>
/// A constant-velocity simulation that carries the sheet to the snap size chosen from the drag's
/// direction and momentum, and stops exactly on it.
/// </summary>
internal sealed class SnappingSimulation : Simulation
{
    /// <summary>The minimum speed, in pixels per second, a snap animation runs at.</summary>
    public const double MinimumSpeed = 1600.0;

    private readonly double _pixelSnapSize;

    public SnappingSimulation(
        double position,
        double initialVelocity,
        IReadOnlyList<double> pixelSnapSizes,
        TimeSpan? snapAnimationDuration = null,
        Tolerance? tolerance = null) : base(tolerance)
    {
        Position = position;
        _pixelSnapSize = GetSnapSize(position, initialVelocity, pixelSnapSizes);

        long snapAnimationMilliseconds = snapAnimationDuration is { } duration
            ? (long)duration.TotalMilliseconds
            : 0;
        if (snapAnimationMilliseconds > 0)
        {
            Velocity = (_pixelSnapSize - position) * 1000 / snapAnimationMilliseconds;
        }
        else if (_pixelSnapSize < position)
        {
            // Check the direction of the target instead of the sign of the velocity: a very low
            // velocity can snap in the opposite direction of the drag.
            Velocity = Math.Min(-MinimumSpeed, initialVelocity);
        }
        else
        {
            Velocity = Math.Max(MinimumSpeed, initialVelocity);
        }
    }

    /// <summary>The sheet height, in pixels, the snap starts from.</summary>
    public double Position { get; }

    /// <summary>The constant velocity, in pixels per second, this simulation runs at.</summary>
    public double Velocity { get; }

    public override double DX(double time) => IsDone(time) ? 0 : Velocity;

    public override bool IsDone(double time) => X(time) == _pixelSnapSize;

    public override double X(double time)
    {
        double newPosition = Position + Velocity * time;
        if ((Velocity >= 0 && newPosition > _pixelSnapSize)
            || (Velocity < 0 && newPosition < _pixelSnapSize))
        {
            // We're passed the snap size, return it instead.
            return _pixelSnapSize;
        }

        return newPosition;
    }

    private double GetSnapSize(double position, double initialVelocity, IReadOnlyList<double> pixelSnapSizes)
    {
        int indexOfNextSize = -1;
        for (int index = 0; index < pixelSnapSizes.Count; index++)
        {
            if (pixelSnapSizes[index] >= position)
            {
                indexOfNextSize = index;
                break;
            }
        }

        if (indexOfNextSize == 0)
        {
            return pixelSnapSizes[0];
        }

        if (indexOfNextSize == -1)
        {
            // The sheet size is clamped to the maximum snap size, so this is unreachable from a valid
            // extent; Dart's `indexWhere` contract would index -1 and throw here.
            return pixelSnapSizes[^1];
        }

        double nextSize = pixelSnapSizes[indexOfNextSize];
        if (nextSize == position)
        {
            // Snap is in the middle of a size change, so keep the target it is already heading to.
            return nextSize;
        }

        double previousSize = pixelSnapSizes[indexOfNextSize - 1];
        if (Math.Abs(initialVelocity) <= Tolerance.Velocity)
        {
            // If velocity is negligible, snap to the closest size; ties go to the next size up.
            return position - previousSize < nextSize - position ? previousSize : nextSize;
        }

        return initialVelocity < 0.0 ? previousSize : nextSize;
    }
}
