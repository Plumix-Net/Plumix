using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/notification_listener.dart

namespace Plumix.Widgets;

public abstract class Notification
{
    public BuildContext? Context { get; private set; }

    public virtual bool Dispatch(BuildContext? target)
    {
        if (target is not BuildContext resolvedTarget)
        {
            return false;
        }

        SetContext(resolvedTarget);
        for (Element? ancestor = resolvedTarget.Owner.Parent; ancestor != null; ancestor = ancestor.Parent)
        {
            if (ancestor.Widget is Viewport && this is IViewportNotification viewportNotification)
            {
                viewportNotification.IncrementDepth();
            }

            if (ancestor is INotificationListener listener && listener.OnNotification(this))
            {
                return true;
            }
        }

        return false;
    }

    protected void SetContext(BuildContext target)
    {
        Context ??= target;
    }
}

public abstract class LayoutChangedNotification : Notification
{
}

internal interface INotificationListener
{
    bool OnNotification(Notification notification);
}

internal interface IViewportNotification
{
    void IncrementDepth();
}

public class NotificationListener<TNotification> : ProxyWidget
    where TNotification : Notification
{
    public NotificationListener(
        Widget child,
        Func<TNotification, bool>? onNotification = null,
        Key? key = null) : base(child, key)
    {
        ArgumentNullException.ThrowIfNull(child);
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
