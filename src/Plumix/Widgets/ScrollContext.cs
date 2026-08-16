using Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/scroll_context.dart

namespace Plumix.Widgets;

/// <summary>
/// An interface that <see cref="Scrollable"/> widgets implement in order to use
/// <see cref="ScrollPosition"/>.
/// </summary>
/// <remarks>
/// Flutter's <c>ScrollContext</c> is an abstract class that <c>ScrollableState</c> implements; C#
/// states already derive from <see cref="State"/>, so the contract is an interface (the
/// <see cref="IScrollActivityDelegate"/> precedent).
/// </remarks>
public interface IScrollContext
{
    /// <summary>
    /// The <see cref="BuildContext"/> that should be used when dispatching
    /// <see cref="ScrollNotification"/>s.
    /// </summary>
    /// <remarks>
    /// This context is typically different than the context of the scrollable widget itself. For
    /// example, <see cref="Scrollable"/> uses a context outside the <see cref="Viewport"/> but inside
    /// the widgets created by <see cref="ScrollBehavior"/>.
    /// <para>Returns null when the scrollable has not been built yet.</para>
    /// </remarks>
    BuildContext? NotificationContext { get; }

    /// <summary>
    /// The <see cref="BuildContext"/> that should be used when searching for a
    /// <see cref="PageStorage"/>.
    /// </summary>
    /// <remarks>
    /// This context is typically the context of the scrollable widget itself. In particular, it
    /// should involve any <see cref="GlobalKey"/>s that are dynamically created as part of creating
    /// the scrolling widget, since those would be different each time the scrolling widget is
    /// constructed.
    /// </remarks>
    BuildContext StorageContext { get; }

    /// <summary>
    /// A <see cref="ITickerProvider"/> to use when animating the scroll position.
    /// </summary>
    ITickerProvider Vsync { get; }

    /// <summary>
    /// The direction in which the widget scrolls.
    /// </summary>
    AxisDirection AxisDirection { get; }

    /// <summary>
    /// The <see cref="FlutterView.DevicePixelRatio"/> of the view that the <see cref="Scrollable"/>
    /// this <see cref="IScrollContext"/> is associated with is drawn into.
    /// </summary>
    double DevicePixelRatio { get; }

    /// <summary>
    /// Whether the contents of the widget should ignore <see cref="PointerEvent"/> inputs.
    /// </summary>
    /// <remarks>
    /// Setting this value to true prevents the use from interacting with the contents of the widget
    /// with pointer events. The widget itself is still interactive.
    /// <para>
    /// For example, if the scroll position is being driven by an animation, it might be appropriate
    /// to set this value to ignore pointer events to prevent the user from accidentally interacting
    /// with the contents of the widget as it animates. The user will still be able to touch the
    /// widget, potentially stopping the animation.
    /// </para>
    /// </remarks>
    void SetIgnorePointer(bool value);

    /// <summary>
    /// Whether the user can drag the widget, for example to initiate a scroll.
    /// </summary>
    void SetCanDrag(bool value);

    /// <summary>
    /// Set the <see cref="SemanticsActions"/> that should be expose to the semantics tree.
    /// </summary>
    void SetSemanticsActions(SemanticsActions actions);

    /// <summary>
    /// Called by the <see cref="ScrollPosition"/> whenever scrolling ends, to persist the current
    /// scroll offset for state restoration purposes.
    /// </summary>
    /// <remarks>
    /// The <see cref="IScrollContext"/> may pass the value back to a <see cref="ScrollPosition"/> by
    /// calling <see cref="ScrollPosition.RestoreOffset"/> at a later point in time or after the
    /// application has restarted to restore the scroll offset.
    /// </remarks>
    void SaveOffset(double offset);
}
