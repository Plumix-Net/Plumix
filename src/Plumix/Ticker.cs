namespace Plumix;

public sealed class Ticker
{
    private readonly Action<TimeSpan> _onTick;
    private double _lastSeconds;
    internal bool Active { get; private set; }

    public Ticker(Action<TimeSpan> onTick) => _onTick = onTick;

    public void Start()
    {
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
        double delta = nowSeconds - _lastSeconds;
        
        _lastSeconds = nowSeconds;
        
        if (delta < 0) return;
        
        _onTick(TimeSpan.FromSeconds(delta));
    }
}

// Dart parity source (reference): flutter/packages/flutter/lib/src/scheduler/ticker.dart (approximate)
