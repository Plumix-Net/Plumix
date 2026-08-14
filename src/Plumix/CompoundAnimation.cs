using System.Numerics;

namespace Plumix;

// Dart parity source: flutter/packages/flutter/lib/src/animation/animations.dart

/// <summary>
/// An interface for combining multiple animations. Subclasses need to implement the
/// <see cref="Animation{T}.Value"/> getter to control how the child animations are combined.
/// </summary>
/// <remarks>
/// Ports Flutter's <c>CompoundAnimation</c>. C# has no mixins, so the
/// <c>AnimationLazyListenerMixin</c>/<c>AnimationLocalListenersMixin</c>/
/// <c>AnimationLocalStatusListenersMixin</c> behavior is inlined here.
/// </remarks>
public abstract class CompoundAnimation<T> : Animation<T>
{
    private readonly List<Action> _listeners = [];
    private readonly List<Action<AnimationStatus>> _statusListeners = [];
    private AnimationStatus? _lastStatus;
    private T? _lastValue;
    private bool _hasLastValue;
    private bool _listening;

    protected CompoundAnimation(Animation<T> first, Animation<T> next)
    {
        First = first ?? throw new ArgumentNullException(nameof(first));
        Next = next ?? throw new ArgumentNullException(nameof(next));
    }

    /// <summary>The first sub-animation. Its status takes precedence if neither is animating.</summary>
    public Animation<T> First { get; }

    /// <summary>The second sub-animation.</summary>
    public Animation<T> Next { get; }

    public override AnimationStatus Status => Next.Status.IsAnimating() ? Next.Status : First.Status;

    public override void AddListener(Action listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        _listeners.Add(listener);
        DidRegisterListener();
    }

    public override void RemoveListener(Action listener)
    {
        if (_listeners.Remove(listener))
        {
            DidUnregisterListener();
        }
    }

    public override void AddStatusListener(Action<AnimationStatus> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        _statusListeners.Add(listener);
        DidRegisterListener();
    }

    public override void RemoveStatusListener(Action<AnimationStatus> listener)
    {
        if (_statusListeners.Remove(listener))
        {
            DidUnregisterListener();
        }
    }

    private void DidRegisterListener()
    {
        if (_listening)
        {
            return;
        }

        _listening = true;
        First.AddListener(MaybeNotifyListeners);
        First.AddStatusListener(MaybeNotifyStatusListeners);
        Next.AddListener(MaybeNotifyListeners);
        Next.AddStatusListener(MaybeNotifyStatusListeners);
    }

    private void DidUnregisterListener()
    {
        if (!_listening || _listeners.Count > 0 || _statusListeners.Count > 0)
        {
            return;
        }

        _listening = false;
        First.RemoveListener(MaybeNotifyListeners);
        First.RemoveStatusListener(MaybeNotifyStatusListeners);
        Next.RemoveListener(MaybeNotifyListeners);
        Next.RemoveStatusListener(MaybeNotifyStatusListeners);
    }

    private void MaybeNotifyStatusListeners(AnimationStatus status)
    {
        _ = status;
        AnimationStatus current = Status;
        if (_lastStatus == current)
        {
            return;
        }

        _lastStatus = current;
        foreach (var listener in _statusListeners.ToArray())
        {
            listener(current);
        }
    }

    private void MaybeNotifyListeners()
    {
        T current = Value;
        if (_hasLastValue && EqualityComparer<T>.Default.Equals(_lastValue, current))
        {
            return;
        }

        _lastValue = current;
        _hasLastValue = true;
        foreach (var listener in _listeners.ToArray())
        {
            listener();
        }
    }
}

/// <summary>An animation that tracks the maximum of two other animations.</summary>
public class AnimationMax<T> : CompoundAnimation<T> where T : INumber<T>
{
    public AnimationMax(Animation<T> first, Animation<T> next) : base(first, next)
    {
    }

    public override T Value => T.Max(First.Value, Next.Value);
}

/// <summary>An animation that tracks the minimum of two other animations.</summary>
public class AnimationMin<T> : CompoundAnimation<T> where T : INumber<T>
{
    public AnimationMin(Animation<T> first, Animation<T> next) : base(first, next)
    {
    }

    public override T Value => T.Min(First.Value, Next.Value);
}

/// <summary>An animation that tracks the mean of two other animations.</summary>
public class AnimationMean : CompoundAnimation<double>
{
    public AnimationMean(Animation<double> left, Animation<double> right) : base(left, right)
    {
    }

    public override double Value => (First.Value + Next.Value) / 2.0;
}

/// <summary>
/// This animation starts by proxying one animation, but when the value of that animation crosses the value
/// of the second (either because the second is going in the opposite direction, or because the one overtakes
/// the other), the animation hops over to proxying the second animation.
/// </summary>
public sealed class TrainHoppingAnimation : Animation<double>, IDisposable
{
    private readonly List<Action> _listeners = [];
    private readonly List<Action<AnimationStatus>> _statusListeners = [];
    private Animation<double>? _currentTrain;
    private Animation<double>? _nextTrain;
    private TrainHoppingMode? _mode;
    private AnimationStatus? _lastStatus;
    private double? _lastValue;
    private bool _disposed;

    public TrainHoppingAnimation(
        Animation<double> currentTrain,
        Animation<double>? nextTrain,
        Action? onSwitchedTrain = null)
    {
        _currentTrain = currentTrain ?? throw new ArgumentNullException(nameof(currentTrain));
        _nextTrain = nextTrain;
        OnSwitchedTrain = onSwitchedTrain;
        if (_nextTrain is not null)
        {
            if (_currentTrain.Value == _nextTrain.Value)
            {
                _currentTrain = _nextTrain;
                _nextTrain = null;
            }
            else if (_currentTrain.Value > _nextTrain.Value)
            {
                _mode = TrainHoppingMode.Maximize;
            }
            else
            {
                _mode = TrainHoppingMode.Minimize;
            }
        }

        _currentTrain.AddStatusListener(HandleStatusChanged);
        _currentTrain.AddListener(HandleValueChanged);
        _nextTrain?.AddListener(HandleValueChanged);
    }

    private enum TrainHoppingMode
    {
        Minimize,
        Maximize,
    }

    /// <summary>The animation that is currently driving this animation.</summary>
    public Animation<double>? CurrentTrain => _currentTrain;

    /// <summary>Called when this animation switches to be driven by the second animation.</summary>
    public Action? OnSwitchedTrain { get; set; }

    public override double Value => _currentTrain!.Value;

    public override AnimationStatus Status => _currentTrain!.Status;

    public override void AddListener(Action listener)
    {
        _listeners.Add(listener ?? throw new ArgumentNullException(nameof(listener)));
    }

    public override void RemoveListener(Action listener) => _listeners.Remove(listener);

    public override void AddStatusListener(Action<AnimationStatus> listener)
    {
        _statusListeners.Add(listener ?? throw new ArgumentNullException(nameof(listener)));
    }

    public override void RemoveStatusListener(Action<AnimationStatus> listener) =>
        _statusListeners.Remove(listener);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _currentTrain!.RemoveStatusListener(HandleStatusChanged);
        _currentTrain.RemoveListener(HandleValueChanged);
        _currentTrain = null;
        _nextTrain?.RemoveListener(HandleValueChanged);
        _nextTrain = null;
        _listeners.Clear();
        _statusListeners.Clear();
    }

    private void HandleStatusChanged(AnimationStatus status)
    {
        if (_lastStatus == status)
        {
            return;
        }

        foreach (var listener in _statusListeners.ToArray())
        {
            listener(status);
        }

        _lastStatus = status;
    }

    private void HandleValueChanged()
    {
        bool hop = false;
        if (_nextTrain is not null)
        {
            hop = _mode switch
            {
                TrainHoppingMode.Minimize => _nextTrain.Value <= _currentTrain!.Value,
                _ => _nextTrain.Value >= _currentTrain!.Value,
            };
            if (hop)
            {
                _currentTrain!.RemoveStatusListener(HandleStatusChanged);
                _currentTrain.RemoveListener(HandleValueChanged);
                _currentTrain = _nextTrain;
                _nextTrain = null;
                _currentTrain.AddStatusListener(HandleStatusChanged);
                HandleStatusChanged(_currentTrain.Status);
            }
        }

        double newValue = Value;
        if (newValue != _lastValue)
        {
            foreach (var listener in _listeners.ToArray())
            {
                listener();
            }

            _lastValue = newValue;
        }

        if (hop)
        {
            OnSwitchedTrain?.Invoke();
        }
    }
}
