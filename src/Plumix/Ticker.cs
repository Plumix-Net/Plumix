// Dart parity source (reference): flutter/packages/flutter/lib/src/scheduler/ticker.dart (approximate)

namespace Plumix;

public interface ITickerProvider
{
    Ticker CreateTicker(Action<TimeSpan> onTick);
}

public sealed class Ticker : IDisposable
{
    private readonly Action<TimeSpan> _onTick;
    private readonly Action<Ticker>? _onDisposed;
    private double _lastSeconds;
    private bool _disposed;
    private bool _muted;
    internal bool Active { get; private set; }

    public Ticker(Action<TimeSpan> onTick) : this(onTick, onDisposed: null)
    {
    }

    internal Ticker(Action<TimeSpan> onTick, Action<Ticker>? onDisposed)
    {
        _onTick = onTick ?? throw new ArgumentNullException(nameof(onTick));
        _onDisposed = onDisposed;
    }

    public bool IsActive => Active;

    public bool IsTicking => Active && !Muted;

    public bool Muted
    {
        get => _muted;
        set
        {
            if (_muted == value)
            {
                return;
            }

            _muted = value;
            if (Active)
            {
                Scheduler.TickerSchedulingChanged();
            }
        }
    }

    public bool ForceFrames { get; set; }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (Active) return;

        Active = true;
        _lastSeconds = Scheduler.CurrentSeconds;
        Scheduler.Add(this);
    }

    public void Stop()
    {
        if (!Active) return;
        Active = false;
        Scheduler.Remove(this);
    }

    internal void InternalTick(double nowSeconds)
    {
        if (Muted)
        {
            return;
        }

        double delta = nowSeconds - _lastSeconds;
        _lastSeconds = nowSeconds;
        if (delta < 0) return;
        _onTick(TimeSpan.FromSeconds(delta));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _disposed = true;
        _onDisposed?.Invoke(this);
    }
}
