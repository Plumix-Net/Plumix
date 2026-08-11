using Plumix;
using Plumix.Foundation;
using Plumix.Physics;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/scroll_position.dart; flutter/packages/flutter/lib/src/widgets/scroll_physics.dart; flutter/packages/flutter/lib/src/widgets/scroll_activity.dart (adapted)

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/scroll_metrics.dart
public interface IScrollMetrics
{
    double Pixels { get; }
    double MinScrollExtent { get; }
    double MaxScrollExtent { get; }
    double ViewportDimension { get; }

    /// <summary>Whether the <see cref="Pixels"/> value is outside the <c>Min</c>/<c>Max</c> extents.</summary>
    bool OutOfRange => Pixels < MinScrollExtent || Pixels > MaxScrollExtent;

    /// <summary>The number of device pixels for each logical pixel of the view the scrollable is in.</summary>
    double DevicePixelRatio => 1.0;
}

/// <summary>An immutable snapshot of values associated with a <see cref="ScrollPosition"/>.</summary>
public sealed record FixedScrollMetrics(
    double Pixels,
    double MinScrollExtent,
    double MaxScrollExtent,
    double ViewportDimension,
    double DevicePixelRatio = 1.0) : IScrollMetrics
{
    /// <summary>Whether the <see cref="Pixels"/> value is outside the min/max extents.</summary>
    public bool OutOfRange => Pixels < MinScrollExtent || Pixels > MaxScrollExtent;

    public static FixedScrollMetrics From(IScrollMetrics metrics) => new(
        metrics.Pixels,
        metrics.MinScrollExtent,
        metrics.MaxScrollExtent,
        metrics.ViewportDimension,
        metrics.DevicePixelRatio);
}

public enum CacheExtentStyle
{
    Pixel,
    Viewport
}

// Dart parity source: flutter/packages/flutter/lib/src/rendering/viewport.dart
public sealed record ScrollCacheExtent
{
    private ScrollCacheExtent(double value, CacheExtentStyle style)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        Value = value;
        Style = style;
    }

    public double Value { get; }

    public CacheExtentStyle Style { get; }

    public static ScrollCacheExtent Pixels(double pixels) => new(pixels, CacheExtentStyle.Pixel);

    public static ScrollCacheExtent Viewport(double value) => new(value, CacheExtentStyle.Viewport);

    internal double CalculateCacheOffset(double mainAxisExtent)
    {
        return Style == CacheExtentStyle.Viewport ? Value * mainAxisExtent : Value;
    }
}

public enum AxisDirection
{
    Up,
    Right,
    Down,
    Left
}

public enum GrowthDirection
{
    Forward,
    Reverse
}

// Dart parity source: flutter/packages/flutter/lib/src/rendering/viewport_offset.dart
public enum ScrollDirection
{
    Idle,
    Forward,
    Reverse
}

public static class ScrollDirectionUtils
{
    public static Axis AxisDirectionToAxis(AxisDirection direction)
    {
        return direction switch
        {
            AxisDirection.Up => Axis.Vertical,
            AxisDirection.Down => Axis.Vertical,
            AxisDirection.Left => Axis.Horizontal,
            AxisDirection.Right => Axis.Horizontal,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }

    public static bool AxisDirectionIsReversed(AxisDirection direction)
    {
        return direction == AxisDirection.Up || direction == AxisDirection.Left;
    }

    public static AxisDirection DefaultAxisDirection(Axis axis)
    {
        return axis == Axis.Vertical ? AxisDirection.Down : AxisDirection.Right;
    }
}

public abstract class ScrollActivity : IDisposable
{
    protected ScrollActivity(ScrollPosition position)
    {
        Position = position;
    }

    protected ScrollPosition Position { get; }

    /// <summary>Whether performing this activity constitutes scrolling.</summary>
    public virtual bool IsScrolling => true;

    /// <summary>The velocity at which the scroll offset is currently independently changing.</summary>
    public virtual double Velocity => 0.0;

    /// <summary>
    /// Called when the viewport or content dimensions change, so the activity can react to a
    /// position that the new dimensions may have put out of range.
    /// </summary>
    public virtual void ApplyNewDimensions()
    {
    }

    public virtual void Dispose()
    {
    }
}

public sealed class IdleScrollActivity(ScrollPosition position) : ScrollActivity(position)
{
    public override bool IsScrolling => false;

    public override void ApplyNewDimensions() => Position.GoBallistic(0.0);
}

public sealed class DragScrollActivity(ScrollPosition position) : ScrollActivity(position)
{
}

public sealed class PointerScrollActivity(ScrollPosition position) : ScrollActivity(position)
{
}

public sealed class DrivenScrollActivity : ScrollActivity
{
    private readonly Curve _curve;
    private readonly TimeSpan _duration;
    private readonly double _from;
    private readonly Ticker _ticker;
    private readonly double _to;
    private TimeSpan _elapsed;
    private bool _disposed;

    public DrivenScrollActivity(
        ScrollPosition position,
        double to,
        TimeSpan duration,
        Curve curve) : base(position)
    {
        if (!double.IsFinite(to))
        {
            throw new ArgumentOutOfRangeException(nameof(to));
        }
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        _from = position.Pixels;
        _to = to;
        _duration = duration;
        _curve = curve ?? throw new ArgumentNullException(nameof(curve));
        _ticker = position.TickerProvider?.CreateTicker(OnTick) ?? new Ticker(OnTick);
        _ticker.Start();
    }

    public override void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _ticker.Dispose();
    }

    private void OnTick(TimeSpan elapsed)
    {
        if (_disposed)
        {
            return;
        }

        _elapsed += elapsed;
        double progress = Math.Clamp(_elapsed.TotalSeconds / _duration.TotalSeconds, 0.0, 1.0);
        double value = _from + ((_to - _from) * _curve(progress));
        Position.SetPixelsFromActivity(value);
        if (progress >= 1.0)
        {
            Position.GoIdle();
        }
    }
}

public sealed class BallisticScrollActivity : ScrollActivity
{
    private readonly Simulation _simulation;
    private readonly Ticker _ticker;
    private double _elapsedSeconds;
    private bool _disposed;

    public BallisticScrollActivity(ScrollPosition position, Simulation simulation) : base(position)
    {
        _simulation = simulation;
        _ticker = position.TickerProvider?.CreateTicker(OnTick) ?? new Ticker(OnTick);
        _ticker.Start();
    }

    public override double Velocity => _disposed ? 0.0 : _simulation.DX(_elapsedSeconds);

    public override void ApplyNewDimensions() => Position.GoBallistic(Velocity);

    public override void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _ticker.Dispose();
    }

    private void OnTick(TimeSpan elapsed)
    {
        if (_disposed)
        {
            return;
        }

        _elapsedSeconds += elapsed.TotalSeconds;

        // The simulation drives the position directly; the physics decide whether the proposed value
        // is reachable. A non-zero overscroll means the boundary conditions clipped the value, so the
        // simulation can no longer be followed and the activity ends.
        if (Math.Abs(Position.SetPixelsFromActivity(_simulation.X(_elapsedSeconds)))
            >= Constants.PrecisionErrorTolerance)
        {
            Position.GoIdle();
            return;
        }

        if (_simulation.IsDone(_elapsedSeconds))
        {
            // A completed ballistic run restarts ballistic with zero velocity, which springs the
            // position back when it is still out of range and goes idle otherwise.
            Position.GoBallistic(0.0);
        }
    }
}

public class ScrollPosition : ChangeNotifier, IScrollMetrics
{
    private readonly ScrollPhysics _physics;
    private double _pixels;
    private double _minScrollExtent;
    private double _maxScrollExtent;
    private double _viewportDimension;
    private ScrollActivity _activity;
    private ScrollDirection _userScrollDirection = ScrollDirection.Idle;
    private FixedScrollMetrics? _lastMetrics;
    private bool _didChangeViewportDimensionOrReceiveCorrection = true;

    public ScrollPosition(double initialPixels = 0.0, ScrollPhysics? physics = null)
    {
        _pixels = initialPixels;
        _physics = physics ?? new ClampingScrollPhysics();
        _activity = new IdleScrollActivity(this);
        IsScrollingNotifier = new ValueNotifier<bool>(false);
    }

    public double Pixels => _pixels;

    public double MinScrollExtent => _minScrollExtent;

    public double MaxScrollExtent => _maxScrollExtent;

    public double ViewportDimension => _viewportDimension;

    /// <summary>Whether the <see cref="Pixels"/> value is outside the min/max scroll extents.</summary>
    public bool OutOfRange => _pixels < _minScrollExtent || _pixels > _maxScrollExtent;

    public ScrollPhysics Physics => _physics;

    public ScrollActivity Activity => _activity;

    public AxisDirection AxisDirection { get; internal set; } = AxisDirection.Down;

    public double DevicePixelRatio { get; internal set; } = 1.0;

    public ValueNotifier<bool> IsScrollingNotifier { get; }

    internal ITickerProvider? TickerProvider { get; set; }

    public ScrollDirection UserScrollDirection => _userScrollDirection;

    public void JumpTo(double value)
    {
        GoIdle();
        SetPixels(value);

        // Physics that allow out-of-range offsets settle the jump back into range.
        GoBallistic(0.0);
    }

    public void AnimateTo(double value, TimeSpan duration, Curve? curve = null)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }
        if (duration == TimeSpan.Zero)
        {
            JumpTo(value);
            return;
        }

        BeginActivity(new DrivenScrollActivity(this, value, duration, curve ?? Curves.Linear));
    }

    public void RestoreOffset(double offset, bool initialRestore = false)
    {
        if (!double.IsFinite(offset))
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "offset must be finite.");
        }

        if (initialRestore)
        {
            _pixels = offset;
            return;
        }

        JumpTo(offset);
    }

    public void BeginDrag()
    {
        BeginActivity(new DragScrollActivity(this));
    }

    internal void UpdateDragTo(double value)
    {
        if (Activity is not DragScrollActivity)
        {
            BeginDrag();
        }
        SetPixels(value);
    }

    public void EndDrag(double primaryPointerVelocity)
    {
        GoBallistic(-primaryPointerVelocity);
    }

    /// <summary>
    /// Starts a ballistic activity with the given velocity, or goes idle when the physics report
    /// that no simulation is needed.
    /// </summary>
    public void GoBallistic(double velocity)
    {
        Simulation? simulation = Physics.CreateBallisticSimulation(this, velocity);
        if (simulation == null)
        {
            GoIdle();
            return;
        }

        BeginActivity(new BallisticScrollActivity(this, simulation));
    }

    public void ApplyUserOffset(double delta)
    {
        double adjusted = Physics.ApplyPhysicsToUserOffset(this, delta);
        double targetPixels = Pixels - adjusted;
        UpdateUserScrollDirection(targetPixels);
        SetPixels(targetPixels);
    }

    public void ApplyPointerScrollDelta(double delta)
    {
        if (delta == 0.0)
        {
            GoBallistic(0.0);
            return;
        }

        // A pointer scroll never overscrolls: unlike a drag, the target is clamped into range and
        // written directly, bypassing the physics' boundary conditions.
        double targetPixels = Math.Min(Math.Max(Pixels + delta, MinScrollExtent), MaxScrollExtent);
        if (Math.Abs(targetPixels - Pixels) < Constants.PrecisionErrorTolerance)
        {
            return;
        }

        BeginActivity(new PointerScrollActivity(this));
        UpdateUserScrollDirection(targetPixels);
        CorrectPixels(targetPixels);
        GoBallistic(0.0);
    }

    public virtual bool ApplyViewportDimension(double viewportDimension)
    {
        if (Math.Abs(_viewportDimension - viewportDimension) < 0.0001)
        {
            return false;
        }

        _viewportDimension = viewportDimension;
        _didChangeViewportDimensionOrReceiveCorrection = true;
        return true;
    }

    public virtual bool ApplyContentDimensions(double minScrollExtent, double maxScrollExtent)
    {
        bool minChanged = Math.Abs(_minScrollExtent - minScrollExtent) > 0.0001;
        bool maxChanged = Math.Abs(_maxScrollExtent - maxScrollExtent) > 0.0001;
        if (!minChanged && !maxChanged && !_didChangeViewportDimensionOrReceiveCorrection)
        {
            return false;
        }

        _minScrollExtent = minScrollExtent;
        _maxScrollExtent = maxScrollExtent;
        _didChangeViewportDimensionOrReceiveCorrection = false;

        var currentMetrics = FixedScrollMetrics.From(this);
        if (_lastMetrics != null)
        {
            // The physics decide where the position lands after the dimensions change: clamping
            // physics reinforce the boundary, bouncing physics keep the relative overscroll.
            double newPixels = Physics.AdjustPositionForNewDimensions(
                oldPosition: _lastMetrics,
                newPosition: currentMetrics,
                isScrolling: Activity.IsScrolling,
                velocity: Activity.Velocity);
            if (Math.Abs(newPixels - _pixels) > 0.0001)
            {
                CorrectPixels(newPixels);
            }
        }

        // Lets the current activity settle a position the new dimensions put out of range.
        Activity.ApplyNewDimensions();
        _lastMetrics = FixedScrollMetrics.From(this);
        return true;
    }

    public override void Dispose()
    {
        _activity.Dispose();
        IsScrollingNotifier.Dispose();
        base.Dispose();
    }

    internal void BeginActivity(ScrollActivity activity)
    {
        if (ReferenceEquals(_activity, activity))
        {
            return;
        }

        _activity.Dispose();
        _activity = activity;
        IsScrollingNotifier.Value = activity is not IdleScrollActivity;
    }

    internal void GoIdle()
    {
        BeginActivity(new IdleScrollActivity(this));
    }

    internal double SetPixelsFromActivity(double value)
    {
        return SetPixels(value);
    }

    protected bool CorrectPixels(double value)
    {
        if (Math.Abs(value - _pixels) < 0.0001)
        {
            return false;
        }

        _pixels = value;
        NotifyListeners();
        return true;
    }

    /// <summary>
    /// Updates the scroll position to the given value, applying the physics' boundary conditions.
    /// </summary>
    /// <returns>
    /// The overscroll: how far the value went beyond what the physics allow, or 0.0 when the whole
    /// change was applied. Physics that accept arbitrary offsets (such as
    /// <see cref="BouncingScrollPhysics"/>) always report 0.0, which is what lets the position travel
    /// outside the scroll extents.
    /// </returns>
    protected virtual double SetPixels(double value)
    {
        if (Math.Abs(value - _pixels) < Constants.PrecisionErrorTolerance)
        {
            return 0.0;
        }

        double overscroll = Physics.ApplyBoundaryConditions(this, value);
        double oldPixels = _pixels;
        _pixels = value - overscroll;
        if (Math.Abs(_pixels - oldPixels) > Constants.PrecisionErrorTolerance)
        {
            NotifyListeners();
        }

        return Math.Abs(overscroll) > Constants.PrecisionErrorTolerance ? overscroll : 0.0;
    }

    private void UpdateUserScrollDirection(double targetPixels)
    {
        if (targetPixels < Pixels)
        {
            _userScrollDirection = ScrollDirection.Forward;
        }
        else if (targetPixels > Pixels)
        {
            _userScrollDirection = ScrollDirection.Reverse;
        }
    }
}
