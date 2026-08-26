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

    public static bool IsCompleted(this AnimationStatus status)
    {
        return status == AnimationStatus.Completed;
    }

    public static bool IsDismissed(this AnimationStatus status)
    {
        return status == AnimationStatus.Dismissed;
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

public sealed class ConstantAnimation<T> : Animation<T>
{
    public ConstantAnimation(T value, AnimationStatus status = AnimationStatus.Completed)
    {
        Value = value;
        Status = status;
    }

    public override T Value { get; }

    public override AnimationStatus Status { get; }

    public override void AddListener(Action listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
    }

    public override void RemoveListener(Action listener)
    {
    }

    public override void AddStatusListener(Action<AnimationStatus> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
    }

    public override void RemoveStatusListener(Action<AnimationStatus> listener)
    {
    }
}

public sealed class ProxyAnimation : Animation<double>
{
    private readonly List<Action> _listeners = [];
    private readonly List<Action<AnimationStatus>> _statusListeners = [];
    private Animation<double>? _parent;
    private AnimationStatus _status = AnimationStatus.Dismissed;
    private double _value;

    public ProxyAnimation(Animation<double>? animation = null)
    {
        _parent = animation;
    }

    public Animation<double>? Parent
    {
        get => _parent;
        set
        {
            if (ReferenceEquals(_parent, value))
            {
                return;
            }

            double previousValue = Value;
            AnimationStatus previousStatus = Status;
            StopListening();
            _parent = value;
            if (_parent is null)
            {
                _value = previousValue;
                _status = previousStatus;
            }
            StartListening();

            if (Value != previousValue)
            {
                NotifyListeners();
            }
            if (Status != previousStatus)
            {
                NotifyStatusListeners(Status);
            }
        }
    }

    public override double Value => _parent?.Value ?? _value;

    public override AnimationStatus Status => _parent?.Status ?? _status;

    public override void AddListener(Action listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        bool wasListening = IsListening;
        _listeners.Add(listener);
        if (!wasListening)
        {
            StartListening();
        }
    }

    public override void RemoveListener(Action listener)
    {
        _ = _listeners.Remove(listener);
        if (!IsListening)
        {
            StopListening();
        }
    }

    public override void AddStatusListener(Action<AnimationStatus> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        bool wasListening = IsListening;
        _statusListeners.Add(listener);
        if (!wasListening)
        {
            StartListening();
        }
    }

    public override void RemoveStatusListener(Action<AnimationStatus> listener)
    {
        _ = _statusListeners.Remove(listener);
        if (!IsListening)
        {
            StopListening();
        }
    }

    private bool IsListening => _listeners.Count > 0 || _statusListeners.Count > 0;

    private void StartListening()
    {
        if (!IsListening || _parent is null)
        {
            return;
        }

        _parent.AddListener(NotifyListeners);
        _parent.AddStatusListener(NotifyStatusListeners);
    }

    private void StopListening()
    {
        if (_parent is null)
        {
            return;
        }

        _parent.RemoveListener(NotifyListeners);
        _parent.RemoveStatusListener(NotifyStatusListeners);
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

public sealed class ReverseAnimation : Animation<double>
{
    private readonly List<Action<AnimationStatus>> _statusListeners = [];

    public ReverseAnimation(Animation<double> parent)
    {
        Parent = parent ?? throw new ArgumentNullException(nameof(parent));
    }

    public Animation<double> Parent { get; }

    public override double Value => 1.0 - Parent.Value;

    public override AnimationStatus Status => ReverseStatus(Parent.Status);

    public override void AddListener(Action listener)
    {
        Parent.AddListener(listener ?? throw new ArgumentNullException(nameof(listener)));
    }

    public override void RemoveListener(Action listener)
    {
        Parent.RemoveListener(listener);
    }

    public override void AddStatusListener(Action<AnimationStatus> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        if (_statusListeners.Count == 0)
        {
            Parent.AddStatusListener(HandleStatusChanged);
        }
        _statusListeners.Add(listener);
    }

    public override void RemoveStatusListener(Action<AnimationStatus> listener)
    {
        _ = _statusListeners.Remove(listener);
        if (_statusListeners.Count == 0)
        {
            Parent.RemoveStatusListener(HandleStatusChanged);
        }
    }

    private static AnimationStatus ReverseStatus(AnimationStatus status)
    {
        return status switch
        {
            AnimationStatus.Forward => AnimationStatus.Reverse,
            AnimationStatus.Reverse => AnimationStatus.Forward,
            AnimationStatus.Completed => AnimationStatus.Dismissed,
            AnimationStatus.Dismissed => AnimationStatus.Completed,
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };
    }

    private void HandleStatusChanged(AnimationStatus status)
    {
        AnimationStatus reversedStatus = ReverseStatus(status);
        foreach (var listener in _statusListeners.ToArray())
        {
            listener(reversedStatus);
        }
    }
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

    public Curve Curve { get; set; }

    public Curve? ReverseCurve { get; set; }

    /// <summary>The animation this curved animation derives its value from.</summary>
    public Animation<double> Parent => _parent;

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

/// <summary>
/// Implements most of the <see cref="Animation{T}"/> interface by deferring its behavior to a given
/// <see cref="Parent"/>. Dart parity: <c>animation/animations.dart</c>
/// (<c>AnimationWithParentMixin</c>; C# has no mixins, so it is an abstract base class).
/// </summary>
public abstract class AnimationWithParentMixin<T> : Animation<T>
{
    /// <summary>
    /// The animation whose value this animation will proxy. This animation must remain the same for
    /// the lifetime of this object; use <see cref="ProxyAnimation"/> to swap parents over time.
    /// </summary>
    public abstract Animation<T> Parent { get; }

    public override AnimationStatus Status => Parent.Status;

    public override void AddListener(Action listener) => Parent.AddListener(listener);

    public override void RemoveListener(Action listener) => Parent.RemoveListener(listener);

    public override void AddStatusListener(Action<AnimationStatus> listener) =>
        Parent.AddStatusListener(listener);

    public override void RemoveStatusListener(Action<AnimationStatus> listener) =>
        Parent.RemoveStatusListener(listener);
}
