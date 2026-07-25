using System.Diagnostics;
using Plumix.Foundation;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/scroll_notification_observer.dart

public delegate void ScrollNotificationCallback(ScrollNotification notification);

public sealed class ScrollNotificationObserver : StatefulWidget
{
    public ScrollNotificationObserver(
        Widget child,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public Widget Child { get; }

    public static ScrollNotificationObserverState? MaybeOf(BuildContext context)
    {
        return context
            .DependOnInherited<ScrollNotificationObserverScope>()
            ?.ScrollNotificationObserverState;
    }

    public static ScrollNotificationObserverState Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException(
                   "ScrollNotificationObserver.Of() was called with a context that does not contain a " +
                   "ScrollNotificationObserver ancestor.");
    }

    public override State CreateState()
    {
        return new ScrollNotificationObserverState();
    }
}

public sealed class ScrollNotificationObserverState : State
{
    private List<ListenerEntry>? _listeners = [];

    private ScrollNotificationObserver CurrentWidget =>
        (ScrollNotificationObserver)StateWidget;

    public void AddListener(ScrollNotificationCallback listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        List<ListenerEntry> listeners = RequireListeners();
        listeners.Add(new ListenerEntry(listener));
    }

    public void RemoveListener(ScrollNotificationCallback listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        List<ListenerEntry> listeners = RequireListeners();
        int index = listeners.FindIndex(entry => entry.Listener == listener);
        if (index >= 0)
        {
            listeners.RemoveAt(index);
        }
    }

    public override Widget Build(BuildContext context)
    {
        return new NotificationListener<ScrollMetricsNotification>(
            onNotification: notification =>
            {
                NotifyListeners(notification.AsScrollUpdate());
                return false;
            },
            child: new NotificationListener<ScrollNotification>(
                onNotification: notification =>
                {
                    NotifyListeners(notification);
                    return false;
                },
                child: new ScrollNotificationObserverScope(
                    scrollNotificationObserverState: this,
                    child: CurrentWidget.Child)));
    }

    public override void Dispose()
    {
        _listeners = null;
        base.Dispose();
    }

    private void NotifyListeners(ScrollNotification notification)
    {
        List<ListenerEntry> listeners = RequireListeners();
        if (listeners.Count == 0)
        {
            return;
        }

        ListenerEntry[] localListeners = listeners.ToArray();
        foreach (ListenerEntry entry in localListeners)
        {
            if (!listeners.Contains(entry))
            {
                continue;
            }

            try
            {
                entry.Listener(notification);
            }
            catch (Exception exception)
            {
                Trace.TraceError(
                    $"Exception while dispatching notifications for {nameof(ScrollNotificationObserverState)}: " +
                    exception);
            }
        }
    }

    private List<ListenerEntry> RequireListeners()
    {
        return _listeners
               ?? throw new ObjectDisposedException(nameof(ScrollNotificationObserverState));
    }

    private sealed class ListenerEntry(ScrollNotificationCallback listener)
    {
        public ScrollNotificationCallback Listener { get; } = listener;
    }
}

internal sealed class ScrollNotificationObserverScope : InheritedWidget
{
    public ScrollNotificationObserverScope(
        ScrollNotificationObserverState scrollNotificationObserverState,
        Widget child)
    {
        ScrollNotificationObserverState =
            scrollNotificationObserverState ?? throw new ArgumentNullException(nameof(scrollNotificationObserverState));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public ScrollNotificationObserverState ScrollNotificationObserverState { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !ReferenceEquals(
            ScrollNotificationObserverState,
            ((ScrollNotificationObserverScope)oldWidget).ScrollNotificationObserverState);
    }
}
