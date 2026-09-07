using System.Diagnostics;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Physics;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/scroll_position_with_single_context.dart

namespace Plumix.Rendering;

/// <summary>
/// A scroll position that manages scroll activities for a single
/// <see cref="IScrollActivityDelegate"/>.
/// </summary>
/// <remarks>
/// This class is a concrete subclass of <see cref="ScrollPosition"/> logic that handles a single
/// <see cref="IScrollContext"/>, such as a <see cref="Scrollable"/>. An instance of this class
/// manages <see cref="ScrollActivity"/> instances, which change what content is visible in the
/// <see cref="Scrollable"/>'s <c>Viewport</c>.
/// </remarks>
public class ScrollPositionWithSingleContext : ScrollPosition, IScrollActivityDelegate
{
    private ScrollDragController? _currentDrag;
    private double _heldPreviousVelocity;
    private ScrollDirection _userScrollDirection = ScrollDirection.Idle;

    /// <summary>Creates a scroll position that manages scroll activities for a single delegate.</summary>
    /// <param name="initialPixels">
    /// The offset to start at. Null leaves <see cref="ScrollPosition.HasPixels"/> false until a
    /// subclass supplies one, which is what lets a subclass derive its offset from the viewport
    /// (Flutter's <c>initialPixels: null</c>).
    /// </param>
    public ScrollPositionWithSingleContext(
        ScrollPhysics physics,
        IScrollContext context,
        double? initialPixels = 0.0,
        bool keepScrollOffset = true,
        ScrollPosition? oldPosition = null,
        string? debugLabel = null)
        : base(physics, context, keepScrollOffset, oldPosition, debugLabel)
    {
        // If oldPosition is not null, the superclass will first call absorb(), which may set
        // _pixels and _activity.
        if (!HasPixels && initialPixels is { } pixels)
        {
            CorrectPixels(pixels);
        }

        if (CurrentActivity == null)
        {
            GoIdle();
        }

        Debug.Assert(CurrentActivity != null);
    }

    /// <inheritdoc />
    public override AxisDirection AxisDirection => Context.AxisDirection;

    /// <inheritdoc />
    public override double SetPixels(double newPixels)
    {
        Debug.Assert(Activity.IsScrolling);
        return base.SetPixels(newPixels);
    }

    /// <inheritdoc />
    public override void Absorb(ScrollPosition other)
    {
        base.Absorb(other);
        if (other is not ScrollPositionWithSingleContext typedOther)
        {
            GoIdle();
            return;
        }

        Activity.UpdateDelegate(this);
        _userScrollDirection = typedOther._userScrollDirection;
        Debug.Assert(_currentDrag == null);
        if (typedOther._currentDrag != null)
        {
            _currentDrag = typedOther._currentDrag;
            _currentDrag.UpdateDelegate(this);
            typedOther._currentDrag = null;
        }
    }

    /// <inheritdoc />
    protected override void ApplyNewDimensions()
    {
        base.ApplyNewDimensions();
        Context.SetCanDrag(Physics.ShouldAcceptUserOffset(this));
    }

    /// <inheritdoc />
    public override void BeginActivity(ScrollActivity? newActivity)
    {
        _heldPreviousVelocity = 0.0;
        if (newActivity == null)
        {
            return;
        }

        Debug.Assert(ReferenceEquals(newActivity.Delegate, this));
        base.BeginActivity(newActivity);
        _currentDrag?.Dispose();
        _currentDrag = null;
        if (!Activity.IsScrolling)
        {
            UpdateUserScrollDirection(ScrollDirection.Idle);
        }
    }

    /// <inheritdoc />
    public virtual void ApplyUserOffset(double delta)
    {
        UpdateUserScrollDirection(delta > 0.0 ? ScrollDirection.Forward : ScrollDirection.Reverse);
        SetPixels(Pixels - Physics.ApplyPhysicsToUserOffset(this, delta));
    }

    /// <inheritdoc />
    public virtual void GoIdle()
    {
        BeginActivity(new IdleScrollActivity(this));
    }

    /// <summary>
    /// Start a physics-driven simulation that settles the <see cref="ScrollPosition.Pixels"/>
    /// position, starting at a particular velocity.
    /// </summary>
    /// <remarks>
    /// This method defers to <see cref="ScrollPhysics.CreateBallisticSimulation"/> to actually
    /// create the simulation. If that returns null, this scroll position goes idle instead.
    /// </remarks>
    public virtual void GoBallistic(double velocity)
    {
        Debug.Assert(HasPixels);
        Simulation? simulation = Physics.CreateBallisticSimulation(this, velocity);
        if (simulation != null)
        {
            BeginActivity(new BallisticScrollActivity(this, simulation, Context.Vsync, ShouldIgnorePointer));
        }
        else
        {
            GoIdle();
        }
    }

    /// <inheritdoc />
    public override ScrollDirection UserScrollDirection => _userScrollDirection;

    /// <summary>
    /// Set <see cref="UserScrollDirection"/> to the given value.
    /// </summary>
    /// <remarks>
    /// If this changes the value, then a <see cref="UserScrollNotification"/> is dispatched.
    /// </remarks>
    protected internal virtual void UpdateUserScrollDirection(ScrollDirection value)
    {
        if (UserScrollDirection == value)
        {
            return;
        }

        _userScrollDirection = value;
        DidUpdateScrollDirection(value);
    }

    /// <inheritdoc />
    public override Task AnimateTo(double to, TimeSpan duration, Curve? curve = null)
    {
        if (NearEqual(to, Pixels, Physics.ToleranceFor(this).Distance))
        {
            // Skip the animation, go straight to the position as we are already close.
            JumpTo(to);
            return Task.CompletedTask;
        }

        var activity = new DrivenScrollActivity(
            this,
            from: Pixels,
            to: to,
            duration: duration,
            curve: curve ?? Curves.Ease,
            vsync: Context.Vsync);
        BeginActivity(activity);
        return activity.Done;
    }

    /// <inheritdoc />
    public override void JumpTo(double value)
    {
        GoIdle();
        if (Pixels != value)
        {
            double oldPixels = Pixels;
            ForcePixels(value);
            DidStartScroll();
            DidUpdateScrollPositionBy(Pixels - oldPixels);
            DidEndScroll();
        }

        GoBallistic(0.0);
    }

    /// <inheritdoc />
    public override void PointerScroll(double delta)
    {
        // If an update is made to pointer scrolling here, consider if the same change should be
        // made to TwoDimensionalScrollPosition.
        if (delta == 0.0)
        {
            GoBallistic(0.0);
            return;
        }

        double targetPixels = Math.Min(Math.Max(Pixels + delta, MinScrollExtent), MaxScrollExtent);
        if (targetPixels != Pixels)
        {
            GoIdle();
            UpdateUserScrollDirection(-delta > 0.0 ? ScrollDirection.Forward : ScrollDirection.Reverse);
            double oldPixels = Pixels;
            // Set the notifier before calling ForcePixels, so that its listeners see the scroll.
            IsScrollingNotifier.Value = true;
            ForcePixels(targetPixels);
            DidStartScroll();
            DidUpdateScrollPositionBy(Pixels - oldPixels);
            DidEndScroll();
            // Don't allow the scrolling momentum to add to the pointer scroll.
            GoBallistic(0.0);
        }
    }

    /// <inheritdoc />
    [Obsolete(
        "This method bypasses scroll activity management and can cause inconsistent layouts or "
        + "scrolling behavior. Use JumpTo or a custom ScrollPosition instead.")]
    public override void JumpToWithoutSettling(double value)
    {
        GoIdle();
        if (Pixels != value)
        {
            double oldPixels = Pixels;
            ForcePixels(value);
            DidStartScroll();
            DidUpdateScrollPositionBy(Pixels - oldPixels);
            DidEndScroll();
        }
    }

    /// <inheritdoc />
    public override IScrollHoldController Hold(Action? holdCancelCallback = null)
    {
        double previousVelocity = Activity.Velocity;
        var holdActivity = new HoldScrollActivity(@delegate: this, onHoldCanceled: holdCancelCallback);
        BeginActivity(holdActivity);
        _heldPreviousVelocity = previousVelocity;
        return holdActivity;
    }

    /// <inheritdoc />
    public override ScrollDragController Drag(DragStartDetails details, Action? dragCancelCallback = null)
    {
        var drag = new ScrollDragController(
            @delegate: this,
            details: details,
            onDragCanceled: dragCancelCallback,
            carriedVelocity: Physics.CarriedMomentum(_heldPreviousVelocity),
            motionStartDistanceThreshold: Physics.DragStartDistanceMotionThreshold);
        BeginActivity(new DragScrollActivity(this, drag));
        Debug.Assert(_currentDrag == null);
        _currentDrag = drag;
        return drag;
    }

    public override void Dispose()
    {
        _currentDrag?.Dispose();
        _currentDrag = null;
        base.Dispose();
    }

    /// <inheritdoc />
    protected override void DebugFillDescription(List<string> description)
    {
        base.DebugFillDescription(description);
        description.Add(Context.GetType().Name);
        description.Add(Physics.ToString() ?? string.Empty);
        description.Add(CurrentActivity?.ToString() ?? "null");
        description.Add(UserScrollDirection.ToString());
    }

    /// <remarks>Flutter's <c>nearEqual</c>.</remarks>
    private static bool NearEqual(double a, double b, double epsilon)
    {
        return (a > b - epsilon && a < b + epsilon) || a == b;
    }
}
