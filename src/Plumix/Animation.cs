using Plumix.Foundation;

namespace Plumix;

// Dart parity sources:
// flutter/packages/flutter/lib/src/animation/animation.dart
// flutter/packages/flutter/lib/src/animation/animations.dart

public enum AnimationStatus
{
    Dismissed,
    Forward,
    Reverse,
    Completed,
}

public static class AnimationStatusExtensions
{
    public static bool IsAnimating(this AnimationStatus status)
    {
        return status is AnimationStatus.Forward or AnimationStatus.Reverse;
    }

    public static bool IsForwardOrCompleted(this AnimationStatus status)
    {
        return status is AnimationStatus.Forward or AnimationStatus.Completed;
    }
}

public abstract class Animation<T> : IValueListenable<T>
{
    public abstract T Value { get; }

    public abstract AnimationStatus Status { get; }

    public abstract void AddListener(Action listener);

    public abstract void RemoveListener(Action listener);

    public abstract void AddStatusListener(Action<AnimationStatus> listener);

    public abstract void RemoveStatusListener(Action<AnimationStatus> listener);
}

public sealed class CurvedAnimation : Animation<double>, IDisposable
{
    private readonly Animation<double> _parent;
    private readonly List<Action> _listeners = [];
    private readonly List<Action<AnimationStatus>> _statusListeners = [];
    private AnimationStatus? _curveDirection;
    private bool _disposed;

    public CurvedAnimation(
        Animation<double> parent,
        Curve curve,
        Curve? reverseCurve = null)
    {
        _parent = parent ?? throw new ArgumentNullException(nameof(parent));
        Curve = curve ?? throw new ArgumentNullException(nameof(curve));
        ReverseCurve = reverseCurve;
        _parent.AddListener(NotifyListeners);
        _parent.AddStatusListener(HandleStatusChanged);
    }

    public Curve Curve { get; }

    public Curve? ReverseCurve { get; }

    public override double Value
    {
        get
        {
            Curve activeCurve = _curveDirection == AnimationStatus.Reverse
                ? ReverseCurve ?? Curve
                : Curve;
            return activeCurve(Math.Clamp(_parent.Value, 0.0, 1.0));
        }
    }

    public override AnimationStatus Status => _parent.Status;

    public override void AddListener(Action listener)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _listeners.Add(listener ?? throw new ArgumentNullException(nameof(listener)));
    }

    public override void RemoveListener(Action listener)
    {
        if (!_disposed)
        {
            _ = _listeners.Remove(listener);
        }
    }

    public override void AddStatusListener(Action<AnimationStatus> listener)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _statusListeners.Add(listener ?? throw new ArgumentNullException(nameof(listener)));
    }

    public override void RemoveStatusListener(Action<AnimationStatus> listener)
    {
        if (!_disposed)
        {
            _ = _statusListeners.Remove(listener);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _parent.RemoveListener(NotifyListeners);
        _parent.RemoveStatusListener(HandleStatusChanged);
        _listeners.Clear();
        _statusListeners.Clear();
        _disposed = true;
    }

    private void HandleStatusChanged(AnimationStatus status)
    {
        switch (status)
        {
            case AnimationStatus.Dismissed:
            case AnimationStatus.Completed:
                _curveDirection = null;
                break;
            case AnimationStatus.Forward:
                _curveDirection = AnimationStatus.Forward;
                break;
            case AnimationStatus.Reverse when _curveDirection != AnimationStatus.Forward:
                _curveDirection = AnimationStatus.Reverse;
                break;
        }

        foreach (var listener in _statusListeners.ToArray())
        {
            listener(status);
        }
    }

    private void NotifyListeners()
    {
        foreach (var listener in _listeners.ToArray())
        {
            listener();
        }
    }
}

internal sealed class MappedDoubleAnimation : Animation<double>, IDisposable
{
    private readonly Animation<double> _parent;
    private readonly Func<double, double> _transform;
    private readonly List<Action> _listeners = [];
    private readonly List<Action<AnimationStatus>> _statusListeners = [];
    private bool _disposed;

    public MappedDoubleAnimation(Animation<double> parent, Func<double, double> transform)
    {
        _parent = parent ?? throw new ArgumentNullException(nameof(parent));
        _transform = transform ?? throw new ArgumentNullException(nameof(transform));
        _parent.AddListener(NotifyListeners);
        _parent.AddStatusListener(NotifyStatusListeners);
    }

    public override double Value => _transform(_parent.Value);

    public override AnimationStatus Status => _parent.Status;

    public override void AddListener(Action listener)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _listeners.Add(listener ?? throw new ArgumentNullException(nameof(listener)));
    }

    public override void RemoveListener(Action listener)
    {
        if (!_disposed)
        {
            _ = _listeners.Remove(listener);
        }
    }

    public override void AddStatusListener(Action<AnimationStatus> listener)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _statusListeners.Add(listener ?? throw new ArgumentNullException(nameof(listener)));
    }

    public override void RemoveStatusListener(Action<AnimationStatus> listener)
    {
        if (!_disposed)
        {
            _ = _statusListeners.Remove(listener);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _parent.RemoveListener(NotifyListeners);
        _parent.RemoveStatusListener(NotifyStatusListeners);
        _listeners.Clear();
        _statusListeners.Clear();
        _disposed = true;
    }

    private void NotifyListeners()
    {
        foreach (var listener in _listeners.ToArray())
        {
            listener();
        }
    }

    private void NotifyStatusListeners(AnimationStatus status)
    {
        foreach (var listener in _statusListeners.ToArray())
        {
            listener(status);
        }
    }
}
