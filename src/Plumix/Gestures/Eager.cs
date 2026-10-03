using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/gestures/eager.dart

namespace Plumix.Gestures;

/// <summary>A gesture recognizer that eagerly claims victory in all gesture arenas.</summary>
/// <remarks>
/// This is typically passed in <c>AndroidView.GestureRecognizers</c> in order to immediately dispatch
/// all touch events inside the view bounds to the embedded Android view.
/// </remarks>
public class EagerGestureRecognizer : OneSequenceGestureRecognizer
{
    /// <summary>Create an eager gesture recognizer.</summary>
    public EagerGestureRecognizer(
        IReadOnlySet<PointerDeviceKind>? supportedDevices = null,
        AllowedButtonsFilter? allowedButtonsFilter = null)
        : base(supportedDevices: supportedDevices, allowedButtonsFilter: allowedButtonsFilter)
    {
    }

    protected override void AddAllowedPointer(PointerDownEvent @event)
    {
        base.AddAllowedPointer(@event);
        Resolve(GestureDisposition.Accepted);
        StopTrackingPointer(@event.Pointer);
    }

    /// <inheritdoc />
    public override string DebugDescription => "eager";

    protected override void DidStopTrackingLastPointer(int pointer)
    {
    }

    protected override void HandleEvent(PointerEvent @event)
    {
    }
}
