namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/services/haptic_feedback.dart

public enum HapticFeedbackType
{
    Vibrate,
    MediumImpact,
    SelectionClick,
}

public static class HapticFeedback
{
    private static Action<HapticFeedbackType>? _feedbackRequested;

    public static event Action<HapticFeedbackType>? FeedbackRequested
    {
        add => _feedbackRequested += value;
        remove => _feedbackRequested -= value;
    }

    public static void Vibrate()
    {
        _feedbackRequested?.Invoke(HapticFeedbackType.Vibrate);
    }

    public static void MediumImpact()
    {
        _feedbackRequested?.Invoke(HapticFeedbackType.MediumImpact);
    }

    public static void SelectionClick()
    {
        _feedbackRequested?.Invoke(HapticFeedbackType.SelectionClick);
    }

    internal static void ResetForTests()
    {
        _feedbackRequested = null;
    }
}
