using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/notification_listener.dart

namespace Plumix.Widgets;

public abstract class Notification
{
    public BuildContext? Context { get; private set; }

    /// <summary>
    /// Starts bubbling this notification at <paramref name="target"/>. Dart's
    /// <c>Notification.dispatch</c>: the walk itself is <see cref="BuildContext.DispatchNotification"/>,
    /// which follows the notification tree the <see cref="NotifiableElementMixin"/> elements built.
    /// </summary>
    public virtual void Dispatch(BuildContext? target)
    {
        if (target is null)
        {
            return;
        }

        SetContext(target);
        target.DispatchNotification(this);
    }

    protected void SetContext(BuildContext target)
    {
        Context ??= target;
    }
}

public abstract class LayoutChangedNotification : Notification
{
}

internal interface IViewportNotification
{
    void IncrementDepth();
}

/// <summary>
/// An <see cref="Element"/> that receives the notifications dispatched from itself or from its
/// descendants. Dart's <c>NotifiableElementMixin</c> from <c>widgets/framework.dart</c>; C# has no
/// mixins, so <see cref="Element.AttachNotificationTree"/> checks for this interface instead of
/// being overridden by it.
/// </summary>
public interface NotifiableElementMixin
{
    /// <summary>
    /// Called when a notification of the appropriate type arrives at this location in the tree.
    /// Return <see langword="true"/> to cancel the notification bubbling.
    /// </summary>
    bool OnNotification(Notification notification);
}

/// <summary>
/// A <see cref="NotifiableElementMixin"/> for viewport elements: it never handles a notification,
/// it only deepens a <c>ViewportNotificationMixin</c> as it bubbles past. Dart's
/// <c>ViewportElementMixin</c> from <c>widgets/scroll_notification.dart</c>.
/// </summary>
public interface ViewportElementMixin : NotifiableElementMixin
{
    bool NotifiableElementMixin.OnNotification(Notification notification)
    {
        if (notification is IViewportNotification viewportNotification)
        {
            viewportNotification.IncrementDepth();
        }

        return false;
    }
}

/// <summary>
/// One link of the notification tree. Dart's private <c>_NotificationNode</c>: an element's node points
/// at the nearest <see cref="NotifiableElementMixin"/> at or above it, so a dispatch visits only
/// listeners and never re-walks the element chain.
/// </summary>
internal sealed class NotificationNode(NotificationNode? parent, NotifiableElementMixin? current)
{
    public NotifiableElementMixin? Current { get; } = current;

    public NotificationNode? Parent { get; } = parent;

    public void DispatchNotification(Notification notification)
    {
        if (Current?.OnNotification(notification) ?? true)
        {
            return;
        }

        Parent?.DispatchNotification(notification);
    }
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

    public override Element CreateElement()
    {
        return new NotificationListenerElement<TNotification>(this);
    }
}

internal sealed class NotificationListenerElement<TNotification> : ProxyElement, NotifiableElementMixin
    where TNotification : Notification
{
    public NotificationListenerElement(NotificationListener<TNotification> widget) : base(widget)
    {
    }

    private NotificationListener<TNotification> TypedWidget => (NotificationListener<TNotification>)Widget;

    /// <summary>
    /// Dart's <c>_NotificationElement.notifyClients</c> is intentionally empty: the notification tree
    /// does not need to notify clients when its configuration changes.
    /// </summary>
    public override void NotifyClients(ProxyWidget oldWidget)
    {
    }

    public bool OnNotification(Notification notification)
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
