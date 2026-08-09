namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/services/haptic_feedback.dart

public enum HapticFeedbackType
{
    MediumImpact,
}

public static class HapticFeedback
{
    private static Action<HapticFeedbackType>? _feedbackRequested;

    public static event Action<HapticFeedbackType>? FeedbackRequested
    {
        add => _feedbackRequested += value;
        remove => _feedbackRequested -= value;
    }

    public static void MediumImpact()
    {
        _feedbackRequested?.Invoke(HapticFeedbackType.MediumImpact);
    }

    internal static void ResetForTests()
    {
        _feedbackRequested = null;
    }
}
