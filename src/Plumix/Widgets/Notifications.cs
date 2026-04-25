using Plumix.Foundation;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/notification_listener.dart (approximate)

namespace Plumix.Widgets;

public abstract class Notification
{
    public bool Dispatch(BuildContext target)
    {
        for (var ancestor = target.Owner.Parent; ancestor != null; ancestor = ancestor.Parent)
        {
            if (ancestor is INotificationListener listener && listener.OnNotification(this))
            {
                return true;
            }
        }

        return false;
    }
}

internal interface INotificationListener
{
    bool OnNotification(Notification notification);
}

public sealed class NotificationListener<TNotification> : ProxyWidget
    where TNotification : Notification
{
    public NotificationListener(
        Widget child,
        Func<TNotification, bool>? onNotification = null,
        Key? key = null) : base(child, key)
    {
        OnNotification = onNotification;
    }

    public Func<TNotification, bool>? OnNotification { get; }

    internal override Element CreateElement()
    {
        return new NotificationListenerElement<TNotification>(this);
    }
}

internal sealed class NotificationListenerElement<TNotification> : ProxyElement, INotificationListener
    where TNotification : Notification
{
    public NotificationListenerElement(NotificationListener<TNotification> widget) : base(widget)
    {
    }

    private NotificationListener<TNotification> TypedWidget => (NotificationListener<TNotification>)Widget;

    bool INotificationListener.OnNotification(Notification notification)
    {
        if (notification is not TNotification typedNotification)
        {
            return false;
        }

        var callback = TypedWidget.OnNotification;
        if (callback == null)
        {
            return false;
        }

        return callback(typedNotification);
    }
}
