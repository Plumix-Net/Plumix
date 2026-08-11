// Dart parity source: flutter/packages/flutter/lib/src/widgets/scroll_physics.dart

using Plumix.Physics;

namespace Plumix.Rendering;

/// <summary>
/// The rate at which scroll momentum will be decelerated.
/// </summary>
public enum ScrollDecelerationRate
{
    /// <summary>Scroll momentum will be decelerated at a normal rate.</summary>
    Normal,

    /// <summary>Scroll momentum will be decelerated at a fast rate.</summary>
    Fast,
}

/// <summary>
/// Determines the physics of a <see cref="ScrollPosition"/>.
/// </summary>
/// <remarks>
/// Instances are immutable and combine through <see cref="ApplyTo"/>: each physics object delegates
/// to its <see cref="Parent"/> for behaviors it does not itself define.
/// </remarks>
public class ScrollPhysics
{
    // gestures/constants.dart: kTouchSlop, kMinFlingVelocity, kMaxFlingVelocity.
    internal const double KTouchSlop = 18.0;
    internal const double KMinFlingVelocity = 50.0;
    internal const double KMaxFlingVelocity = 8000.0;

    private static readonly SpringDescription DefaultSpring =
        SpringDescription.WithDampingRatio(mass: 0.5, stiffness: 100.0, ratio: 1.1);

    public ScrollPhysics(ScrollPhysics? parent = null)
    {
        Parent = parent;
    }

    /// <summary>The physics to delegate to for behaviors this object does not override.</summary>
    public ScrollPhysics? Parent { get; }

    /// <summary>
    /// If <see cref="Parent"/> is null, returns <paramref name="ancestor"/>; otherwise applies the
    /// parent to the ancestor and returns the result.
    /// </summary>
    protected ScrollPhysics? BuildParent(ScrollPhysics? ancestor) => Parent?.ApplyTo(ancestor) ?? ancestor;

    /// <summary>Creates a copy of this object appending <paramref name="ancestor"/> to the chain.</summary>
    public virtual ScrollPhysics ApplyTo(ScrollPhysics? ancestor) => new(BuildParent(ancestor));

    /// <summary>Used by <see cref="DragScrollActivity"/> and others to convert a drag to a scroll offset.</summary>
    public virtual double ApplyPhysicsToUserOffset(IScrollMetrics position, double offset)
    {
        return Parent?.ApplyPhysicsToUserOffset(position, offset) ?? offset;
    }

    /// <summary>Whether the scrollable should let the user adjust the scroll offset.</summary>
    public virtual bool ShouldAcceptUserOffset(IScrollMetrics position)
    {
        if (!AllowUserScrolling)
        {
            return false;
        }

        if (Parent == null)
        {
            return position.Pixels != 0.0 || position.MinScrollExtent != position.MaxScrollExtent;
        }

        return Parent.ShouldAcceptUserOffset(position);
    }

    /// <summary>
    /// Determines the overscroll by applying the boundary conditions.
    /// </summary>
    /// <returns>
    /// The amount of the proposed change that is over the boundary, or 0.0 if the value is within
    /// bounds. The sign matches the direction of the boundary overrun.
    /// </returns>
    public virtual double ApplyBoundaryConditions(IScrollMetrics position, double value)
    {
        return Parent?.ApplyBoundaryConditions(position, value) ?? 0.0;
    }

    /// <summary>
    /// Returns the new scroll position to use when the viewport dimensions or content extents change.
    /// </summary>
    public virtual double AdjustPositionForNewDimensions(
        IScrollMetrics oldPosition,
        IScrollMetrics newPosition,
        bool isScrolling,
        double velocity)
    {
        if (Parent == null)
        {
            return newPosition.Pixels;
        }

        return Parent.AdjustPositionForNewDimensions(oldPosition, newPosition, isScrolling, velocity);
    }

    /// <summary>
    /// Returns a simulation for ballistic scrolling starting from the given position with the given
    /// velocity, or null when the position is in range and the velocity is negligible.
    /// </summary>
    public virtual Simulation? CreateBallisticSimulation(IScrollMetrics position, double velocity)
    {
        return Parent?.CreateBallisticSimulation(position, velocity);
    }

    /// <summary>The spring to use for ballistic simulations.</summary>
    public virtual SpringDescription Spring => Parent?.Spring ?? DefaultSpring;

    /// <summary>The tolerance to use for ballistic simulations.</summary>
    public virtual Tolerance ToleranceFor(IScrollMetrics metrics)
    {
        return Parent?.ToleranceFor(metrics)
               ?? new Tolerance(
                   velocity: 1.0 / (0.050 * metrics.DevicePixelRatio), // logical pixels per second
                   distance: 1.0 / metrics.DevicePixelRatio); // logical pixels
    }

    /// <summary>The minimum distance an input pointer drag must have moved to be considered a fling.</summary>
    public virtual double MinFlingDistance => Parent?.MinFlingDistance ?? KTouchSlop;

    /// <summary>The minimum velocity for an input pointer drag to be considered a fling.</summary>
    public virtual double MinFlingVelocity => Parent?.MinFlingVelocity ?? KMinFlingVelocity;

    /// <summary>Scroll fling velocity magnitudes will be clamped to this value.</summary>
    public virtual double MaxFlingVelocity => Parent?.MaxFlingVelocity ?? KMaxFlingVelocity;

    /// <summary>Returns the velocity carried on repeated flings.</summary>
    public virtual double CarriedMomentum(double existingVelocity)
    {
        return Parent?.CarriedMomentum(existingVelocity) ?? 0.0;
    }

    /// <summary>The minimum amount of pixel distance drags must move by to start motion.</summary>
    public virtual double? DragStartDistanceMotionThreshold => Parent?.DragStartDistanceMotionThreshold;

    /// <summary>Whether a viewport is allowed to change its scroll position implicitly.</summary>
    public virtual bool AllowImplicitScrolling => true;

    /// <summary>Whether a viewport is allowed to change the scroll position as the result of user input.</summary>
    public virtual bool AllowUserScrolling => true;

    public override string ToString()
    {
        return Parent == null ? GetType().Name : $"{GetType().Name} -> {Parent}";
    }
}

/// <summary>
/// Scroll physics that attempt to keep the scroll position in range when the contents change
/// dimensions suddenly.
/// </summary>
public class RangeMaintainingScrollPhysics : ScrollPhysics
{
    public RangeMaintainingScrollPhysics(ScrollPhysics? parent = null) : base(parent)
    {
    }

    public override ScrollPhysics ApplyTo(ScrollPhysics? ancestor)
    {
        return new RangeMaintainingScrollPhysics(BuildParent(ancestor));
    }

    public override double AdjustPositionForNewDimensions(
        IScrollMetrics oldPosition,
        IScrollMetrics newPosition,
        bool isScrolling,
        double velocity)
    {
        bool maintainOverscroll = true;
        bool enforceBoundary = true;
        if (velocity != 0.0)
        {
            // Don't try to adjust an animating position, the jumping around would be distracting.
            maintainOverscroll = false;
            enforceBoundary = false;
        }

        if (oldPosition.MinScrollExtent == newPosition.MinScrollExtent
            && oldPosition.MaxScrollExtent == newPosition.MaxScrollExtent)
        {
            // If the extents haven't changed then ignore overscroll.
            maintainOverscroll = false;
        }

        if (oldPosition.Pixels != newPosition.Pixels)
        {
            // If the position has been changed already, then it might have been adjusted to expect
            // new overscroll, so don't try to maintain the relative overscroll.
            maintainOverscroll = false;
            if (double.IsFinite(oldPosition.MinScrollExtent)
                && double.IsFinite(oldPosition.MaxScrollExtent)
                && double.IsFinite(newPosition.MinScrollExtent)
                && double.IsFinite(newPosition.MaxScrollExtent))
            {
                // In addition, if the position changed then we don't enforce the new boundary if
                // both the new and previous boundaries are entirely finite.
                enforceBoundary = false;
            }
        }

        if (oldPosition.Pixels < oldPosition.MinScrollExtent
            || oldPosition.Pixels > oldPosition.MaxScrollExtent)
        {
            // If the old position was out of range, then we should not try to keep the new position
            // in range.
            enforceBoundary = false;
        }

        if (maintainOverscroll)
        {
            // Force the new position to be no more out of range than it was before, if it was
            // overscrolled and the extents have decreased.
            if (oldPosition.Pixels < oldPosition.MinScrollExtent
                && newPosition.MinScrollExtent > oldPosition.MinScrollExtent)
            {
                double oldDelta = oldPosition.MinScrollExtent - oldPosition.Pixels;
                return newPosition.MinScrollExtent - oldDelta;
            }

            if (oldPosition.Pixels > oldPosition.MaxScrollExtent
                && newPosition.MaxScrollExtent < oldPosition.MaxScrollExtent)
            {
                double oldDelta = oldPosition.Pixels - oldPosition.MaxScrollExtent;
                return newPosition.MaxScrollExtent + oldDelta;
            }
        }

        // If we're not forcing the overscroll, defer to other physics.
        double result = base.AdjustPositionForNewDimensions(oldPosition, newPosition, isScrolling, velocity);
        if (enforceBoundary)
        {
            // ...but if they put us out of range then reinforce the boundary.
            result = Math.Clamp(result, newPosition.MinScrollExtent, newPosition.MaxScrollExtent);
        }

        return result;
    }
}

/// <summary>
/// Scroll physics for environments that allow the scroll offset to go beyond the bounds of the
/// content, but then bounce the content back to the edge of those bounds.
/// </summary>
/// <remarks>
/// Used by default on iOS. Because it accepts arbitrary offsets, <see cref="ApplyBoundaryConditions"/>
/// always reports no overscroll; the rubber-band resistance lives in
/// <see cref="ApplyPhysicsToUserOffset"/> and the spring-back in <see cref="CreateBallisticSimulation"/>.
/// </remarks>
public class BouncingScrollPhysics : ScrollPhysics
{
    public BouncingScrollPhysics(
        ScrollDecelerationRate decelerationRate = ScrollDecelerationRate.Normal,
        ScrollPhysics? parent = null) : base(parent)
    {
        DecelerationRate = decelerationRate;
    }

    /// <summary>Used to determine parameters for friction simulations.</summary>
    public ScrollDecelerationRate DecelerationRate { get; }

    public override ScrollPhysics ApplyTo(ScrollPhysics? ancestor)
    {
        return new BouncingScrollPhysics(DecelerationRate, BuildParent(ancestor));
    }

    /// <summary>
    /// The multiplier applied to overscroll to make it appear that scrolling past the edge of the
    /// scrollable contents is harder than scrolling the list.
    /// </summary>
    public double FrictionFactor(double overscrollFraction)
    {
        return Math.Pow(1 - overscrollFraction, 2)
               * DecelerationRate switch
               {
                   ScrollDecelerationRate.Fast => 0.26,
                   _ => 0.52,
               };
    }

    public override double ApplyPhysicsToUserOffset(IScrollMetrics position, double offset)
    {
        if (!position.OutOfRange)
        {
            return offset;
        }

        double overscrollPastStart = Math.Max(position.MinScrollExtent - position.Pixels, 0.0);
        double overscrollPastEnd = Math.Max(position.Pixels - position.MaxScrollExtent, 0.0);
        double overscrollPast = Math.Max(overscrollPastStart, overscrollPastEnd);
        bool easing = (overscrollPastStart > 0.0 && offset < 0.0)
                      || (overscrollPastEnd > 0.0 && offset > 0.0);

        double friction = easing
            // Apply less resistance when easing the overscroll vs tensioning.
            ? FrictionFactor((overscrollPast - Math.Abs(offset)) / position.ViewportDimension)
            : FrictionFactor(overscrollPast / position.ViewportDimension);
        double direction = PhysicsUtils.Sign(offset);

        if (easing && DecelerationRate == ScrollDecelerationRate.Fast)
        {
            return direction * Math.Abs(offset);
        }

        return direction * ApplyFriction(overscrollPast, Math.Abs(offset), friction);
    }

    public override double ApplyBoundaryConditions(IScrollMetrics position, double value) => 0.0;

    public override Simulation? CreateBallisticSimulation(IScrollMetrics position, double velocity)
    {
        Tolerance tolerance = ToleranceFor(position);
        if (Math.Abs(velocity) >= tolerance.Velocity || position.OutOfRange)
        {
            return new BouncingScrollSimulation(
                position: position.Pixels,
                velocity: velocity,
                leadingExtent: position.MinScrollExtent,
                trailingExtent: position.MaxScrollExtent,
                spring: Spring,
                constantDeceleration: DecelerationRate switch
                {
                    ScrollDecelerationRate.Fast => 1400,
                    _ => 0,
                },
                tolerance: tolerance);
        }

        return null;
    }

    // The ballistic simulation here decelerates more slowly than the one for ClampingScrollPhysics
    // so we require a more deliberate input gesture to trigger a fling.
    public override double MinFlingVelocity => KMinFlingVelocity * 2.0;

    public override double CarriedMomentum(double existingVelocity)
    {
        return PhysicsUtils.Sign(existingVelocity)
               * Math.Min(0.000816 * Math.Pow(Math.Abs(existingVelocity), 1.967), 40000.0);
    }

    // Eyeballed from observation to counter the effect of an unintended scroll from the natural
    // motion of lifting the finger after a scroll.
    public override double? DragStartDistanceMotionThreshold => 3.5;

    public override double MaxFlingVelocity => DecelerationRate switch
    {
        ScrollDecelerationRate.Fast => KMaxFlingVelocity * 8.0,
        _ => base.MaxFlingVelocity,
    };

    public override SpringDescription Spring => DecelerationRate switch
    {
        ScrollDecelerationRate.Fast => SpringDescription.WithDampingRatio(
            mass: 0.3,
            stiffness: 75.0,
            ratio: 1.3),
        _ => base.Spring,
    };

    private static double ApplyFriction(double extentOutside, double absDelta, double gamma)
    {
        double total = 0.0;
        if (extentOutside > 0)
        {
            double deltaToLimit = extentOutside / gamma;
            if (absDelta < deltaToLimit)
            {
                return absDelta * gamma;
            }

            total += extentOutside;
            absDelta -= deltaToLimit;
        }

        return total + absDelta;
    }
}

/// <summary>
/// Scroll physics for environments that prevent the scroll offset from reaching beyond the bounds of
/// the content.
/// </summary>
/// <remarks>Used by default on Android.</remarks>
public class ClampingScrollPhysics : ScrollPhysics
{
    public ClampingScrollPhysics(ScrollPhysics? parent = null) : base(parent)
    {
    }

    public override ScrollPhysics ApplyTo(ScrollPhysics? ancestor)
    {
        return new ClampingScrollPhysics(BuildParent(ancestor));
    }

    public override double ApplyBoundaryConditions(IScrollMetrics position, double value)
    {
        if (value < position.Pixels && position.Pixels <= position.MinScrollExtent)
        {
            // Underscroll.
            return value - position.Pixels;
        }

        if (position.MaxScrollExtent <= position.Pixels && position.Pixels < value)
        {
            // Overscroll.
            return value - position.Pixels;
        }

        if (value < position.MinScrollExtent && position.MinScrollExtent < position.Pixels)
        {
            // Hit top edge.
            return value - position.MinScrollExtent;
        }

        if (position.Pixels < position.MaxScrollExtent && position.MaxScrollExtent < value)
        {
            // Hit bottom edge.
            return value - position.MaxScrollExtent;
        }

        return 0.0;
    }

    public override Simulation? CreateBallisticSimulation(IScrollMetrics position, double velocity)
    {
        Tolerance tolerance = ToleranceFor(position);
        if (position.OutOfRange)
        {
            double end = position.Pixels > position.MaxScrollExtent
                ? position.MaxScrollExtent
                : position.MinScrollExtent;
            return new ScrollSpringSimulation(
                Spring,
                position.Pixels,
                end,
                Math.Min(0.0, velocity),
                tolerance: tolerance);
        }

        if (Math.Abs(velocity) < tolerance.Velocity)
        {
            return null;
        }

        if (velocity > 0.0 && position.Pixels >= position.MaxScrollExtent)
        {
            return null;
        }

        if (velocity < 0.0 && position.Pixels <= position.MinScrollExtent)
        {
            return null;
        }

        return new ClampingScrollSimulation(
            position: position.Pixels,
            velocity: velocity,
            tolerance: tolerance);
    }
}
