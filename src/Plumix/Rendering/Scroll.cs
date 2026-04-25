using Plumix;
using Plumix.Foundation;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/scroll_position.dart; flutter/packages/flutter/lib/src/widgets/scroll_physics.dart; flutter/packages/flutter/lib/src/widgets/scroll_activity.dart (adapted)

namespace Plumix.Rendering;

public interface IScrollMetrics
{
    double Pixels { get; }
    double MinScrollExtent { get; }
    double MaxScrollExtent { get; }
    double ViewportDimension { get; }
}

public enum CacheExtentStyle
{
    Pixel,
    Viewport
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

public abstract class Simulation
{
    public abstract double X(double timeSeconds);

    public abstract double DX(double timeSeconds);

    public abstract bool IsDone(double timeSeconds);
}

public sealed class FrictionSimulation : Simulation
{
    private readonly double _drag;
    private readonly double _position;
    private readonly double _velocity;

    public FrictionSimulation(double drag, double position, double velocity)
    {
        _drag = Math.Max(0.0001, drag);
        _position = position;
        _velocity = velocity;
    }

    public override double X(double timeSeconds)
    {
        var decay = Math.Exp(-_drag * timeSeconds);
        return _position + (_velocity / _drag) * (1 - decay);
    }

    public override double DX(double timeSeconds)
    {
        return _velocity * Math.Exp(-_drag * timeSeconds);
    }

    public override bool IsDone(double timeSeconds)
    {
        return Math.Abs(DX(timeSeconds)) < 5.0;
    }
}

public abstract class ScrollPhysics
{
    protected ScrollPhysics(ScrollPhysics? parent = null)
    {
        Parent = parent;
    }

    public ScrollPhysics? Parent { get; }

    public virtual double ApplyPhysicsToUserOffset(IScrollMetrics position, double offset)
    {
        if (Parent != null)
        {
            return Parent.ApplyPhysicsToUserOffset(position, offset);
        }

        return offset;
    }

    public virtual double ApplyBoundaryConditions(IScrollMetrics position, double value)
    {
        if (Parent != null)
        {
            return Parent.ApplyBoundaryConditions(position, value);
        }

        return 0;
    }

    public virtual Simulation? CreateBallisticSimulation(IScrollMetrics position, double velocity)
    {
        if (Parent != null)
        {
            return Parent.CreateBallisticSimulation(position, velocity);
        }

        return null;
    }
}

public sealed class ClampingScrollPhysics : ScrollPhysics
{
    public ClampingScrollPhysics(ScrollPhysics? parent = null) : base(parent)
    {
    }

    public override double ApplyBoundaryConditions(IScrollMetrics position, double value)
    {
        if (value < position.Pixels && position.Pixels <= position.MinScrollExtent)
        {
            return value - position.Pixels;
        }

        if (position.MaxScrollExtent <= position.Pixels && position.Pixels < value)
        {
            return value - position.Pixels;
        }

        if (value < position.MinScrollExtent && position.MinScrollExtent < position.Pixels)
        {
            return value - position.MinScrollExtent;
        }

        if (position.Pixels < position.MaxScrollExtent && position.MaxScrollExtent < value)
        {
            return value - position.MaxScrollExtent;
        }

        return base.ApplyBoundaryConditions(position, value);
    }

    public override Simulation? CreateBallisticSimulation(IScrollMetrics position, double velocity)
    {
        var outOfRange = position.Pixels < position.MinScrollExtent || position.Pixels > position.MaxScrollExtent;
        if (outOfRange)
        {
            var target = Math.Clamp(position.Pixels, position.MinScrollExtent, position.MaxScrollExtent);
            var correctedVelocity = (target - position.Pixels) * 8.0;
            return new FrictionSimulation(6.0, position.Pixels, correctedVelocity);
        }

        if (Math.Abs(velocity) < 20)
        {
            return null;
        }

        return new FrictionSimulation(4.5, position.Pixels, velocity);
    }
}

public abstract class ScrollActivity : IDisposable
{
    protected ScrollActivity(ScrollPosition position)
    {
        Position = position;
    }

    protected ScrollPosition Position { get; }

    public virtual void Dispose()
    {
    }
}

public sealed class IdleScrollActivity(ScrollPosition position) : ScrollActivity(position)
{
}

public sealed class DragScrollActivity(ScrollPosition position) : ScrollActivity(position)
{
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
        _ticker = new Ticker(OnTick);
        _ticker.Start();
    }

    public override void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _ticker.Stop();
    }

    private void OnTick(TimeSpan elapsed)
    {
        if (_disposed)
        {
            return;
        }

        _elapsedSeconds += elapsed.TotalSeconds;
        Position.SetPixelsFromActivity(_simulation.X(_elapsedSeconds));

        var outOfRange = Position.Pixels < Position.MinScrollExtent || Position.Pixels > Position.MaxScrollExtent;
        if (_simulation.IsDone(_elapsedSeconds) || outOfRange)
        {
            Position.GoIdle();
        }
    }
}

public sealed class ScrollPosition : ChangeNotifier, IScrollMetrics
{
    private readonly ScrollPhysics _physics;
    private double _pixels;
    private double _minScrollExtent;
    private double _maxScrollExtent;
    private double _viewportDimension;
    private ScrollActivity _activity;

    public ScrollPosition(double initialPixels = 0.0, ScrollPhysics? physics = null)
    {
        _pixels = initialPixels;
        _physics = physics ?? new ClampingScrollPhysics();
        _activity = new IdleScrollActivity(this);
    }

    public double Pixels => _pixels;

    public double MinScrollExtent => _minScrollExtent;

    public double MaxScrollExtent => _maxScrollExtent;

    public double ViewportDimension => _viewportDimension;

    public ScrollPhysics Physics => _physics;

    public ScrollActivity Activity => _activity;

    public void JumpTo(double value)
    {
        GoIdle();
        SetPixels(value);
    }

    public void BeginDrag()
    {
        BeginActivity(new DragScrollActivity(this));
    }

    public void EndDrag(double primaryPointerVelocity)
    {
        var scrollVelocity = -primaryPointerVelocity;
        var simulation = Physics.CreateBallisticSimulation(this, scrollVelocity);
        if (simulation == null)
        {
            GoIdle();
            return;
        }

        BeginActivity(new BallisticScrollActivity(this, simulation));
    }

    public void ApplyUserOffset(double delta)
    {
        var adjusted = Physics.ApplyPhysicsToUserOffset(this, delta);
        SetPixels(Pixels - adjusted);
    }

    public void ApplyPointerScrollDelta(double delta)
    {
        GoIdle();
        SetPixels(Pixels + delta);
    }

    public bool ApplyViewportDimension(double viewportDimension)
    {
        if (Math.Abs(_viewportDimension - viewportDimension) < 0.0001)
        {
            return false;
        }

        _viewportDimension = viewportDimension;
        return true;
    }

    public bool ApplyContentDimensions(double minScrollExtent, double maxScrollExtent)
    {
        var minChanged = Math.Abs(_minScrollExtent - minScrollExtent) > 0.0001;
        var maxChanged = Math.Abs(_maxScrollExtent - maxScrollExtent) > 0.0001;
        _minScrollExtent = minScrollExtent;
        _maxScrollExtent = maxScrollExtent;

        var changed = SetPixels(_pixels);
        return changed || minChanged || maxChanged;
    }

    public override void Dispose()
    {
        _activity.Dispose();
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
    }

    internal void GoIdle()
    {
        BeginActivity(new IdleScrollActivity(this));
    }

    internal bool SetPixelsFromActivity(double value)
    {
        return SetPixels(value);
    }

    private bool SetPixels(double value)
    {
        var overscroll = Physics.ApplyBoundaryConditions(this, value);
        var newPixels = value - overscroll;
        newPixels = Math.Clamp(newPixels, _minScrollExtent, _maxScrollExtent);

        if (Math.Abs(newPixels - _pixels) < 0.0001)
        {
            return false;
        }

        _pixels = newPixels;
        NotifyListeners();
        return true;
    }
}
