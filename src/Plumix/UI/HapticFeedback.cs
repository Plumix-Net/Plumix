namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/services/haptic_feedback.dart

/// <summary>Allows access to the haptic feedback interface on the device.</summary>
public static class HapticFeedback
{
    /// <summary>Provides vibration haptic feedback to the user for a short duration.</summary>
    public static Task Vibrate()
    {
        return SystemChannels.Platform.InvokeMethod<object>("HapticFeedback.vibrate");
    }

    /// <summary>Provides a haptic feedback corresponding a collision impact with a light mass.</summary>
    public static Task LightImpact()
    {
        return Feedback("HapticFeedbackType.lightImpact");
    }

    /// <summary>Provides a haptic feedback corresponding a collision impact with a medium mass.</summary>
    public static Task MediumImpact()
    {
        return Feedback("HapticFeedbackType.mediumImpact");
    }

    /// <summary>Provides a haptic feedback corresponding a collision impact with a heavy mass.</summary>
    public static Task HeavyImpact()
    {
        return Feedback("HapticFeedbackType.heavyImpact");
    }

    /// <summary>Provides a haptic feedback indicating a selection changing through discrete values.</summary>
    public static Task SelectionClick()
    {
        return Feedback("HapticFeedbackType.selectionClick");
    }

    /// <summary>Provides a haptic feedback indicating a task completed successfully.</summary>
    public static Task SuccessNotification()
    {
        return Feedback("HapticFeedbackType.successNotification");
    }

    /// <summary>Provides a haptic feedback indicating a warning.</summary>
    public static Task WarningNotification()
    {
        return Feedback("HapticFeedbackType.warningNotification");
    }

    /// <summary>Provides a haptic feedback indicating a task failed.</summary>
    public static Task ErrorNotification()
    {
        return Feedback("HapticFeedbackType.errorNotification");
    }

    private static Task Feedback(string type)
    {
        return SystemChannels.Platform.InvokeMethod<object>("HapticFeedback.vibrate", type);
    }
}
