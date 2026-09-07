using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Physics;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/scroll_position.dart

namespace Plumix.Rendering;

/// <summary>
/// The policy to use when applying the <c>alignment</c> parameter of
/// <see cref="ScrollPosition.EnsureVisible"/>.
/// </summary>
public enum ScrollPositionAlignmentPolicy
{
    /// <summary>Use the <c>alignment</c> property of <see cref="ScrollPosition.EnsureVisible"/> to decide
    /// where to align the visible object.</summary>
    Explicit,

    /// <summary>
    /// Find the bottom edge of the scroll container, and scroll the container, if necessary, to show
    /// the bottom of the object.
    /// </summary>
    /// <remarks>
    /// This is useful when it is not necessary to move the entire object into view, and the object
    /// is taller than the container. Scrolling will only occur if the bottom of the object is below
    /// the bottom of the container.
    /// </remarks>
    KeepVisibleAtEnd,

    /// <summary>
    /// Find the top edge of the scroll container, and scroll the container if necessary to show the
    /// top of the object.
    /// </summary>
    /// <remarks>
    /// Scrolling will only occur if the top of the object is above the top of the container.
    /// </remarks>
    KeepVisibleAtStart,
}

/// <summary>
/// Determines which portion of the content is visible in a scroll view.
/// </summary>
/// <remarks>
/// The <see cref="Pixels"/> value determines the scroll offset that the scroll view uses to select
/// which part of its content to display. As the user scrolls the viewport, this value changes, which
/// changes the content that is displayed.
/// <para>
/// This object is a <see cref="Listenable"/> that notifies its listeners when <see cref="Pixels"/>
/// changes.
/// </para>
/// <para>
/// This class is a concrete subclass of <see cref="ViewportOffset"/>; the
/// <see cref="ScrollPositionWithSingleContext"/> subclass adds the
/// <see cref="IScrollActivityDelegate"/> half.
/// </para>
/// </remarks>
public abstract class ScrollPosition : ViewportOffset, IScrollMetrics
{
    private ScrollActivity? _activity;
    private bool _didChangeViewportDimensionOrReceiveCorrection = true;
    private bool _haveDimensions;
    private bool _haveScheduledUpdateNotification;
    private double _impliedVelocity;
    private Axis? _lastAxis;
    private IScrollMetrics? _lastMetrics;
    private double? _maxScrollExtent;
    private double? _minScrollExtent;
    private bool _pendingDimensions;
    private double? _pixels;
    private SemanticsActions? _semanticActions;
    private double? _viewportDimension;

    /// <summary>Creates an object that determines which portion of the content is visible in a scroll view.</summary>
    /// <param name="physics">How the position should respond to user input.</param>
    /// <param name="context">The scrollable this position drives, and reads its vsync from.</param>
    /// <param name="keepScrollOffset">Whether the offset persists through <see cref="PageStorage"/>.</param>
    /// <param name="oldPosition">A position this one takes the in-flight scroll state over from.</param>
    /// <param name="debugLabel">A label used in <see cref="ViewportOffset.ToString"/> output.</param>
    protected ScrollPosition(
        ScrollPhysics physics,
        IScrollContext context,
        bool keepScrollOffset = true,
        ScrollPosition? oldPosition = null,
        string? debugLabel = null)
    {
        ArgumentNullException.ThrowIfNull(physics);
        ArgumentNullException.ThrowIfNull(context);
        Physics = physics;
        Context = context;
        KeepScrollOffset = keepScrollOffset;
        DebugLabel = debugLabel;
        if (oldPosition != null)
        {
            Absorb(oldPosition);
        }

        if (keepScrollOffset)
        {
            RestoreScrollOffset();
        }
    }

    /// <summary>How the scroll position should respond to user input.</summary>
    public ScrollPhysics Physics { get; }

    /// <summary>Where the scrolling is taking place.</summary>
    public IScrollContext Context { get; }

    /// <summary>
    /// Save the current scroll offset with <see cref="PageStorage"/> and restore it if this scroll
    /// position's scrollable is recreated.
    /// </summary>
    public bool KeepScrollOffset { get; }

    /// <summary>A label that is used in the <see cref="ViewportOffset.ToString"/> output.</summary>
    public string? DebugLabel { get; }

    /// <summary>
    /// The minimum in-range value for <see cref="Pixels"/>.
    /// </summary>
    /// <remarks>
    /// Flutter throws when the extents have not been established yet; Plumix reports zero and
    /// exposes <see cref="HasContentDimensions"/> instead (see <c>docs/ai/DIVERGENCES.md</c>).
    /// </remarks>
    public double MinScrollExtent => _minScrollExtent ?? 0.0;

    /// <inheritdoc cref="MinScrollExtent"/>
    public double MaxScrollExtent => _maxScrollExtent ?? 0.0;

    /// <summary>Whether the min and max scroll extents have been established by a layout.</summary>
    public bool HasContentDimensions => _minScrollExtent != null && _maxScrollExtent != null;

    /// <inheritdoc cref="MinScrollExtent"/>
    public override double Pixels => _pixels ?? 0.0;

    /// <summary>Whether <see cref="Pixels"/> has been established by a layout or a correction.</summary>
    public override bool HasPixels => _pixels != null;

    /// <inheritdoc cref="MinScrollExtent"/>
    public double ViewportDimension => _viewportDimension ?? 0.0;

    /// <summary>Whether <see cref="ViewportDimension"/> has been supplied by a layout.</summary>
    public bool HasViewportDimension => _viewportDimension != null;

    /// <summary>
    /// Whether <see cref="ApplyNewDimensions"/> should be called.
    /// </summary>
    /// <remarks>
    /// Returns true if the number of dimensions is not zero, which means the scroll view has been
    /// laid out at least once.
    /// </remarks>
    public bool HaveDimensions => _haveDimensions;

    /// <summary>The direction in which the scroll view scrolls.</summary>
    public abstract AxisDirection AxisDirection { get; }

    /// <summary>The axis along which the scroll view scrolls.</summary>
    public Axis Axis => ScrollMetricsUtils.AxisOf(this);

    /// <inheritdoc />
    public double DevicePixelRatio => Context.DevicePixelRatio;

    /// <summary>Whether the <see cref="Pixels"/> value is outside the min/max scroll extents.</summary>
    public bool OutOfRange => ScrollMetricsUtils.OutOfRange(this);

    /// <summary>Whether <see cref="Pixels"/> sits exactly on one of the two scroll extents.</summary>
    public bool AtEdge => ScrollMetricsUtils.AtEdge(this);

    /// <summary>The quantity of content conceptually "above" the viewport.</summary>
    public double ExtentBefore => ScrollMetricsUtils.ExtentBefore(this);

    /// <summary>The quantity of content conceptually "inside" the viewport.</summary>
    public double ExtentInside => ScrollMetricsUtils.ExtentInside(this);

    /// <summary>The quantity of content conceptually "below" the viewport.</summary>
    public double ExtentAfter => ScrollMetricsUtils.ExtentAfter(this);

    /// <summary>The total quantity of content available.</summary>
    public double ExtentTotal => ScrollMetricsUtils.ExtentTotal(this);

    /// <summary>
    /// Whether a viewport is allowed to change <see cref="Pixels"/> implicitly to respond to a call
    /// to <c>RenderObject.ShowOnScreen</c>.
    /// </summary>
    public override bool AllowImplicitScrolling => Physics.AllowImplicitScrolling;

    /// <summary>
    /// Whether scrollables should absorb pointer events at this position.
    /// </summary>
    /// <remarks>
    /// This value relates to the current <see cref="ScrollActivity"/>, which determines if
    /// additional touch input should be received by the scroll view or its children. If the position
    /// is overscrolled, as is allowed by <c>BouncingScrollPhysics</c>, children of the scroll view
    /// will receive pointer events as the scroll view settles back from the overscrolled state.
    /// </remarks>
    public bool ShouldIgnorePointer => !OutOfRange && (CurrentActivity?.ShouldIgnorePointer ?? true);

    /// <summary>
    /// Notifies whether the scroll view is currently scrolling, which is to say whether its
    /// <see cref="Activity"/> is one that <see cref="ScrollActivity.IsScrolling"/> reports.
    /// </summary>
    public ValueNotifier<bool> IsScrollingNotifier { get; } = new(false);

    /// <summary>
    /// The currently operative <see cref="ScrollActivity"/>.
    /// </summary>
    /// <remarks>
    /// Dart types this getter as nullable because a position has no activity between its base and
    /// subclass constructors; every use site is a null assertion, so C# exposes the non-null shape
    /// and keeps the nullable one for subclasses as <see cref="CurrentActivity"/>.
    /// </remarks>
    public ScrollActivity Activity => _activity!;

    /// <summary>The activity, or null while the position is still being constructed or disposed.</summary>
    private protected ScrollActivity? CurrentActivity => _activity;

    /// <summary>
    /// Take any current applicable state from the given <see cref="ScrollPosition"/>.
    /// </summary>
    /// <remarks>
    /// This method is called by the constructor when it is given an <c>oldPosition</c>. Implementations
    /// of this method should call <c>base.Absorb</c> after setting any metrics-related or
    /// activity-related state, since this method may restart the activity and scroll activities tend
    /// to use those metrics when being restarted.
    /// <para>Calling this method is destructive to the given position.</para>
    /// </remarks>
    public virtual void Absorb(ScrollPosition other)
    {
        ArgumentNullException.ThrowIfNull(other);
        Debug.Assert(ReferenceEquals(other.Context, Context));
        Debug.Assert(_pixels == null);
        if (other.HasContentDimensions)
        {
            _minScrollExtent = other.MinScrollExtent;
            _maxScrollExtent = other.MaxScrollExtent;
        }

        if (other.HasPixels)
        {
            _pixels = other.Pixels;
        }

        if (other.HasViewportDimension)
        {
            _viewportDimension = other.ViewportDimension;
        }

        Debug.Assert(_activity == null);
        Debug.Assert(other._activity != null);
        _activity = other._activity;
        other._activity = null;
        if (other.GetType() != GetType())
        {
            Activity.ResetActivity();
        }

        Context.SetIgnorePointer(Activity.ShouldIgnorePointer);
        IsScrollingNotifier.Value = Activity.IsScrolling;
    }

    /// <summary>
    /// Update the scroll position to the given pixel value.
    /// </summary>
    /// <returns>
    /// The overscroll, if any. If the return value is 0.0, that means that <see cref="Pixels"/> now
    /// returns the given value. If the return value is positive, then <see cref="Pixels"/> is less
    /// than the requested value, and if it is negative it is greater.
    /// </returns>
    public virtual double SetPixels(double newPixels)
    {
        Debug.Assert(HasPixels);
        Debug.Assert(
            Scheduler.Phase != SchedulerPhase.PersistentCallbacks,
            "A scrollable's position should not change during the build, layout, and paint phases, "
            + "otherwise the rendering will be confused.");
        if (newPixels != Pixels)
        {
            double overscroll = ApplyBoundaryConditions(newPixels);
            Debug.Assert(
                Math.Abs(overscroll) <= Math.Abs(newPixels - Pixels),
                $"{GetType().Name}.ApplyBoundaryConditions returned invalid overscroll value.");
            double oldPixels = Pixels;
            _pixels = newPixels - overscroll;
            if (Pixels != oldPixels)
            {
                if (OutOfRange)
                {
                    Context.SetIgnorePointer(false);
                }

                NotifyListeners();
                DidUpdateScrollPositionBy(Pixels - oldPixels);
            }

            if (Math.Abs(overscroll) > Constants.PrecisionErrorTolerance)
            {
                DidOverscrollBy(overscroll);
                return overscroll;
            }
        }

        return 0.0;
    }

    /// <summary>
    /// Change the value of <see cref="Pixels"/> to the new value, without notifying any customers.
    /// </summary>
    /// <remarks>
    /// This is used to adjust the position while doing layout. In particular, this is typically
    /// called as a response to <c>ApplyViewportDimension</c> or <c>ApplyContentDimensions</c> (in
    /// both cases, if this method is called, those methods should then return false to indicate that
    /// the position has been adjusted).
    /// </remarks>
    protected void CorrectPixels(double value)
    {
        _pixels = value;
    }

    /// <summary>
    /// Apply a layout-time correction to the scroll offset.
    /// </summary>
    /// <remarks>
    /// This method should change the <see cref="Pixels"/> value by <paramref name="correction"/>,
    /// but without calling <see cref="ViewportOffset.NotifyListeners"/>.
    /// </remarks>
    public override void CorrectBy(double correction)
    {
        if (!HasPixels)
        {
            throw new InvalidOperationException(
                "An initial pixels value must exist by calling CorrectPixels on the ScrollPosition");
        }

        _pixels += correction;
        _didChangeViewportDimensionOrReceiveCorrection = true;
    }

    /// <summary>
    /// Change the value of <see cref="Pixels"/> to the new value, and notify any customers, but
    /// without honoring the physics' boundary conditions.
    /// </summary>
    protected void ForcePixels(double value)
    {
        Debug.Assert(HasPixels);
        _impliedVelocity = value - Pixels;
        _pixels = value;
        NotifyListeners();
        Scheduler.AddPostFrameCallback(_ => _impliedVelocity = 0);
    }

    /// <summary>
    /// Called whenever scrolling ends, to store the current scroll offset in a storage mechanism
    /// with a lifetime that matches the app's lifetime.
    /// </summary>
    protected virtual void SaveScrollOffset()
    {
        BuildContext context = Context.StorageContext;
        PageStorage.MaybeOf(context)?.WriteState(context, Pixels);
    }

    /// <summary>
    /// Called whenever the <see cref="ScrollPosition"/> is created, to restore the scroll offset the
    /// <see cref="SaveScrollOffset"/> stored.
    /// </summary>
    protected virtual void RestoreScrollOffset()
    {
        if (!HasPixels)
        {
            BuildContext context = Context.StorageContext;
            if (PageStorage.MaybeOf(context)?.ReadState(context) is double value)
            {
                CorrectPixels(value);
            }
        }
    }

    /// <summary>
    /// Called by <see cref="IScrollContext"/> to restore the scroll offset to the provided value.
    /// </summary>
    /// <remarks>
    /// The provided value has previously been provided to <see cref="IScrollContext.SaveOffset"/>.
    /// </remarks>
    public virtual void RestoreOffset(double offset, bool initialRestore = false)
    {
        if (initialRestore)
        {
            CorrectPixels(offset);
        }
        else
        {
            JumpTo(offset);
        }
    }

    /// <summary>
    /// Called whenever scrolling ends, to persist the current scroll offset for state restoration
    /// purposes.
    /// </summary>
    protected virtual void SaveOffset()
    {
        Debug.Assert(HasPixels);
        Context.SaveOffset(Pixels);
    }

    /// <summary>
    /// Returns the overscroll by subtracting the boundary conditions from the given
    /// <paramref name="value"/>.
    /// </summary>
    protected virtual double ApplyBoundaryConditions(double value)
    {
        double result = Physics.ApplyBoundaryConditions(this, value);
        Debug.Assert(
            Math.Abs(result) <= Math.Abs(value - Pixels),
            $"{Physics.GetType().Name}.ApplyBoundaryConditions returned invalid overscroll value. "
            + "The applyBoundaryConditions method is only supposed to reduce the possible range of "
            + "movement, not increase it.");
        return result;
    }

    /// <inheritdoc />
    public override bool ApplyViewportDimension(double viewportDimension)
    {
        if (_viewportDimension != viewportDimension)
        {
            _viewportDimension = viewportDimension;
            _didChangeViewportDimensionOrReceiveCorrection = true;
            // If this is called, you can rely on ApplyContentDimensions being called soon
            // afterwards in the same layout phase. So we put all the logic that relies on both
            // values being computed into ApplyContentDimensions.
        }

        return true;
    }

    /// <summary>Whether the metrics that a <see cref="ScrollMetricsNotification"/> reports moved.</summary>
    private bool IsMetricsChanged()
    {
        Debug.Assert(HaveDimensions);
        IScrollMetrics currentMetrics = CopyWith();
        return _lastMetrics == null
               || !(currentMetrics.ExtentBefore == _lastMetrics.ExtentBefore
                    && currentMetrics.ExtentInside == _lastMetrics.ExtentInside
                    && currentMetrics.ExtentAfter == _lastMetrics.ExtentAfter
                    && currentMetrics.AxisDirection == _lastMetrics.AxisDirection);
    }

    /// <inheritdoc />
    public override bool ApplyContentDimensions(double minScrollExtent, double maxScrollExtent)
    {
        Debug.Assert(HaveDimensions == (_lastMetrics != null));
        if (!NearEqual(_minScrollExtent, minScrollExtent, Tolerance.DefaultTolerance.Distance)
            || !NearEqual(_maxScrollExtent, maxScrollExtent, Tolerance.DefaultTolerance.Distance)
            || _didChangeViewportDimensionOrReceiveCorrection
            || _lastAxis != Axis)
        {
            if (minScrollExtent > maxScrollExtent)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minScrollExtent),
                    "minScrollExtent must be less than or equal to maxScrollExtent.");
            }

            _minScrollExtent = minScrollExtent;
            _maxScrollExtent = maxScrollExtent;
            _lastAxis = Axis;
            IScrollMetrics? currentMetrics = HaveDimensions ? CopyWith() : null;
            _didChangeViewportDimensionOrReceiveCorrection = false;
            _pendingDimensions = true;
            if (HaveDimensions && !CorrectForNewDimensions(_lastMetrics!, currentMetrics!))
            {
                return false;
            }

            _haveDimensions = true;
        }

        Debug.Assert(HaveDimensions);
        if (_pendingDimensions)
        {
            ApplyNewDimensions();
            _pendingDimensions = false;
        }

        Debug.Assert(
            !_didChangeViewportDimensionOrReceiveCorrection,
            "Use CorrectForNewDimensions() (and return true) to change the scroll offset during "
            + "ApplyContentDimensions().");

        if (IsMetricsChanged())
        {
            // It is too late to send useful notifications, because the potential listeners have
            // already been built this frame. To make sure the notification is sent at all, defer it
            // until after the frame is complete.
            if (!_haveScheduledUpdateNotification)
            {
                Scheduler.ScheduleMicrotask(DidUpdateScrollMetrics);
                _haveScheduledUpdateNotification = true;
            }

            _lastMetrics = CopyWith();
        }

        return true;
    }

    /// <summary>
    /// Verify that the new content and viewport dimensions are acceptable.
    /// </summary>
    /// <remarks>
    /// Called by <see cref="ApplyContentDimensions"/> to determine its return value. Should return
    /// true if the current scroll offset is correct given the new dimensions, otherwise should call
    /// <see cref="CorrectPixels"/> to correct the scroll offset and return false.
    /// </remarks>
    protected virtual bool CorrectForNewDimensions(IScrollMetrics oldPosition, IScrollMetrics newPosition)
    {
        double newPixels = Physics.AdjustPositionForNewDimensions(
            oldPosition: oldPosition,
            newPosition: newPosition,
            isScrolling: Activity.IsScrolling,
            velocity: Activity.Velocity);
        if (newPixels != Pixels)
        {
            CorrectPixels(newPixels);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Notifies the activity that the dimensions of the underlying viewport or contents have changed.
    /// </summary>
    protected virtual void ApplyNewDimensions()
    {
        Debug.Assert(HasPixels);
        Debug.Assert(_pendingDimensions);
        Activity.ApplyNewDimensions();
        UpdateSemanticActions(); // will potentially request a semantics update.
    }

    /// <summary>
    /// Recomputes which directional scroll actions are still available.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>ScrollPosition._updateSemanticActions</c>; Plumix's <see cref="SemanticsActions"/>
    /// is a flags enum rather than a set, so the "did it change" check is a flags comparison.
    /// </remarks>
    private void UpdateSemanticActions()
    {
        (SemanticsActions forward, SemanticsActions backward) = AxisDirection switch
        {
            AxisDirection.Up => (SemanticsActions.ScrollDown, SemanticsActions.ScrollUp),
            AxisDirection.Down => (SemanticsActions.ScrollUp, SemanticsActions.ScrollDown),
            AxisDirection.Left => (SemanticsActions.ScrollRight, SemanticsActions.ScrollLeft),
            _ => (SemanticsActions.ScrollLeft, SemanticsActions.ScrollRight),
        };

        SemanticsActions actions = SemanticsActions.None;
        if (Pixels > MinScrollExtent)
        {
            actions |= backward;
        }

        if (Pixels < MaxScrollExtent)
        {
            actions |= forward;
        }

        if (_semanticActions == actions)
        {
            return;
        }

        _semanticActions = actions;
        Context.SetSemanticsActions(actions);
    }

    private static ScrollPositionAlignmentPolicy MaybeFlipAlignment(ScrollPositionAlignmentPolicy policy)
    {
        return policy switch
        {
            // Neither start nor end aligned, so nothing to flip.
            ScrollPositionAlignmentPolicy.Explicit => policy,
            ScrollPositionAlignmentPolicy.KeepVisibleAtEnd => ScrollPositionAlignmentPolicy.KeepVisibleAtStart,
            _ => ScrollPositionAlignmentPolicy.KeepVisibleAtEnd,
        };
    }

    private ScrollPositionAlignmentPolicy ApplyAxisDirectionToAlignmentPolicy(
        ScrollPositionAlignmentPolicy policy)
    {
        return AxisDirection switch
        {
            // Start and end alignments must account for the direction of the scroll.
            AxisDirection.Up or AxisDirection.Left => MaybeFlipAlignment(policy),
            _ => policy,
        };
    }

    /// <summary>
    /// Animates the position such that the given object is as visible as possible by just scrolling
    /// this position.
    /// </summary>
    /// <param name="target">The render object to reveal.</param>
    /// <param name="alignment">0.0 aligns the leading edge, 1.0 the trailing edge, 0.5 centers.</param>
    /// <param name="duration">Zero jumps rather than animating.</param>
    /// <param name="curve">The animation curve; <c>Curves.Ease</c> when omitted.</param>
    /// <param name="alignmentPolicy">
    /// Whether <paramref name="alignment"/> is used as given, or only far enough to keep the target
    /// visible at one edge without scrolling the other way.
    /// </param>
    /// <param name="targetRenderObject">
    /// The innermost object the caller actually wants revealed, when <paramref name="target"/> is an
    /// enclosing scrollable's render object rather than the original target.
    /// </param>
    public Task EnsureVisible(
        RenderObject target,
        double alignment = 0.0,
        TimeSpan duration = default,
        Curve? curve = null,
        ScrollPositionAlignmentPolicy alignmentPolicy = ScrollPositionAlignmentPolicy.Explicit,
        RenderObject? targetRenderObject = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        Debug.Assert(target.Attached);
        IRenderAbstractViewport? viewport = RenderAbstractViewport.MaybeOf(target);
        if (viewport is null)
        {
            return Task.CompletedTask;
        }

        Rect? targetRect = null;
        if (targetRenderObject != null && !ReferenceEquals(targetRenderObject, target))
        {
            targetRect = RenderObject.TransformRect(
                targetRenderObject.GetTransformTo(target),
                target.PaintBounds.Intersect(targetRenderObject.PaintBounds));
        }

        double resolved;
        switch (ApplyAxisDirectionToAlignmentPolicy(alignmentPolicy))
        {
            case ScrollPositionAlignmentPolicy.Explicit:
                resolved = Math.Clamp(
                    viewport.GetOffsetToReveal(target, alignment, targetRect, Axis).Offset,
                    MinScrollExtent,
                    MaxScrollExtent);
                break;
            case ScrollPositionAlignmentPolicy.KeepVisibleAtEnd:
                resolved = Math.Clamp(
                    viewport.GetOffsetToReveal(target, 1.0, targetRect, Axis).Offset,
                    MinScrollExtent,
                    MaxScrollExtent);
                if (resolved < Pixels)
                {
                    resolved = Pixels;
                }

                break;
            default:
                resolved = Math.Clamp(
                    viewport.GetOffsetToReveal(target, 0.0, targetRect, Axis).Offset,
                    MinScrollExtent,
                    MaxScrollExtent);
                if (resolved > Pixels)
                {
                    resolved = Pixels;
                }

                break;
        }

        if (resolved == Pixels)
        {
            return Task.CompletedTask;
        }

        if (duration == TimeSpan.Zero)
        {
            JumpTo(resolved);
            return Task.CompletedTask;
        }

        return AnimateTo(resolved, duration, curve ?? Curves.Ease);
    }

    /// <summary>
    /// Animates the position from its current value to the given value.
    /// </summary>
    /// <returns>
    /// A task that completes when the animation ends, whether it completed successfully or whether
    /// it was interrupted prematurely.
    /// </returns>
    public abstract override Task AnimateTo(double to, TimeSpan duration, Curve? curve = null);

    /// <summary>Jumps the scroll position from its current value to the given value.</summary>
    public abstract override void JumpTo(double value);

    /// <summary>
    /// Changes the scrolling position based on a pointer signal from current value to delta without
    /// animation and without checking if new value is in range.
    /// </summary>
    public abstract void PointerScroll(double delta);

    /// <summary>
    /// Calls <see cref="JumpTo"/> or <see cref="AnimateTo"/>, depending on whether
    /// <paramref name="duration"/> was supplied.
    /// </summary>
    public override Task MoveTo(
        double to,
        TimeSpan? duration = null,
        Curve? curve = null,
        bool? clamp = null)
    {
        if (clamp ?? true)
        {
            to = Math.Clamp(to, MinScrollExtent, MaxScrollExtent);
        }

        return base.MoveTo(to, duration, curve);
    }

    /// <summary>
    /// Jump the scroll position from its current value to the given value, without settling the
    /// position afterwards.
    /// </summary>
    [Obsolete(
        "This method bypasses scroll activity management and can cause inconsistent layouts or "
        + "scrolling behavior. Use JumpTo or a custom ScrollPosition instead.")]
    public abstract void JumpToWithoutSettling(double value);

    /// <summary>
    /// Stop the current activity and start a <see cref="HoldScrollActivity"/>.
    /// </summary>
    public abstract IScrollHoldController Hold(Action? holdCancelCallback = null);

    /// <summary>Start a drag activity corresponding to the given <see cref="DragStartDetails"/>.</summary>
    public abstract IDrag Drag(DragStartDetails details, Action? dragCancelCallback = null);

    /// <summary>
    /// Change the current <see cref="Activity"/>, disposing of the old one and sending scroll
    /// notifications as necessary.
    /// </summary>
    /// <remarks>
    /// If the argument is null, this method has no effect. This is convenient for cases where the
    /// new activity is obtained from another method, and that method might return null.
    /// </remarks>
    public virtual void BeginActivity(ScrollActivity? newActivity)
    {
        if (newActivity == null)
        {
            return;
        }

        bool wasScrolling;
        bool oldIgnorePointer;
        if (_activity != null)
        {
            oldIgnorePointer = _activity.ShouldIgnorePointer;
            wasScrolling = _activity.IsScrolling;
            if (wasScrolling && !newActivity.IsScrolling)
            {
                // Notifies and then saves the scroll offset.
                DidEndScroll();
            }

            _activity.Dispose();
        }
        else
        {
            oldIgnorePointer = false;
            wasScrolling = false;
        }

        _activity = newActivity;
        if (oldIgnorePointer != Activity.ShouldIgnorePointer)
        {
            Context.SetIgnorePointer(Activity.ShouldIgnorePointer);
        }

        IsScrollingNotifier.Value = Activity.IsScrolling;
        if (!wasScrolling && _activity.IsScrolling)
        {
            DidStartScroll();
        }
    }

    /// <summary>Called by <see cref="BeginActivity"/> to report when an activity has started.</summary>
    public void DidStartScroll()
    {
        Activity.DispatchScrollStartNotification(CopyWith(), Context.NotificationContext);
    }

    /// <summary>
    /// Called by <see cref="SetPixels"/> to report a change to the <see cref="Pixels"/> position.
    /// </summary>
    public void DidUpdateScrollPositionBy(double delta)
    {
        if (Context.NotificationContext is { } context)
        {
            Activity.DispatchScrollUpdateNotification(CopyWith(), context, delta);
        }
    }

    /// <summary>Called by <see cref="BeginActivity"/> to report when an activity has ended.</summary>
    /// <remarks>This also saves the scroll offset using <see cref="SaveScrollOffset"/>.</remarks>
    public void DidEndScroll()
    {
        if (Context.NotificationContext is { } context)
        {
            Activity.DispatchScrollEndNotification(CopyWith(), context);
        }

        SaveOffset();
        if (KeepScrollOffset)
        {
            SaveScrollOffset();
        }
    }

    /// <summary>
    /// Called by <see cref="SetPixels"/> to report overscroll when an attempt is made to change the
    /// <see cref="Pixels"/> position.
    /// </summary>
    public void DidOverscrollBy(double value)
    {
        Debug.Assert(Activity.IsScrolling);
        if (Context.NotificationContext is { } context)
        {
            Activity.DispatchOverscrollNotification(CopyWith(), context, value);
        }
    }

    /// <summary>
    /// Dispatches a notification that the <see cref="ViewportOffset.UserScrollDirection"/> has
    /// changed.
    /// </summary>
    public void DidUpdateScrollDirection(ScrollDirection direction)
    {
        if (Context.NotificationContext is { } context)
        {
            new UserScrollNotification(CopyWith(), direction, sourceContext: context).Dispatch(context);
        }
    }

    /// <summary>
    /// Dispatches a notification that the <see cref="IScrollMetrics"/> have changed.
    /// </summary>
    public void DidUpdateScrollMetrics()
    {
        Debug.Assert(Scheduler.Phase != SchedulerPhase.PersistentCallbacks);
        Debug.Assert(_haveScheduledUpdateNotification);
        _haveScheduledUpdateNotification = false;
        if (Context.NotificationContext is { } context)
        {
            new ScrollMetricsNotification(CopyWith(), context).Dispatch(context);
        }
    }

    /// <summary>
    /// Provides a heuristic to determine if expensive frame-bound tasks should be deferred.
    /// </summary>
    public bool RecommendDeferredLoading(BuildContext context)
    {
        Debug.Assert(CurrentActivity != null);
        return Physics.RecommendDeferredLoading(
            Activity.Velocity + _impliedVelocity,
            CopyWith(),
            context);
    }

    /// <summary>An immutable snapshot of this position's metrics.</summary>
    /// <remarks>
    /// Flutter's <c>ScrollMetrics.copyWith</c>, which a <see cref="ScrollPosition"/> inherits from
    /// the mixin. Subclasses that carry extra metrics (a page view's viewport fraction, for
    /// instance) override this and return their own <see cref="FixedScrollMetrics"/> subclass.
    /// </remarks>
    public virtual IScrollMetrics CopyWith(
        double? minScrollExtent = null,
        double? maxScrollExtent = null,
        double? pixels = null,
        double? viewportDimension = null,
        AxisDirection? axisDirection = null,
        double? devicePixelRatio = null)
    {
        return ScrollMetricsUtils.Copy(
            this,
            minScrollExtent,
            maxScrollExtent,
            pixels,
            viewportDimension,
            axisDirection,
            devicePixelRatio);
    }

    public override void Dispose()
    {
        CurrentActivity?.Dispose();
        _activity = null;
        IsScrollingNotifier.Dispose();
        base.Dispose();
    }

    public override void NotifyListeners()
    {
        UpdateSemanticActions(); // will potentially request a semantics update.
        base.NotifyListeners();
    }

    /// <inheritdoc />
    protected override void DebugFillDescription(List<string> description)
    {
        if (DebugLabel != null)
        {
            description.Add(DebugLabel);
        }

        base.DebugFillDescription(description);
        description.Add(
            $"range: {Format(_minScrollExtent)}..{Format(_maxScrollExtent)}");
        description.Add($"viewport: {Format(_viewportDimension)}");
    }

    private static string Format(double? value)
    {
        return value?.ToString("F1", CultureInfo.InvariantCulture) ?? "null";
    }

    /// <remarks>Flutter's <c>nearEqual</c>, which treats two nulls as equal.</remarks>
    private static bool NearEqual(double? a, double? b, double epsilon)
    {
        if (a == null || b == null)
        {
            return a == b;
        }

        return (a > b - epsilon && a < b + epsilon) || a == b;
    }
}
