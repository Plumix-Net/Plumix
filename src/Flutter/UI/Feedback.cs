namespace Flutter.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/feedback.dart (approximate)

public enum FeedbackType
{
    Tap,
    LongPress,
}

public static class Feedback
{
    private static Action<FeedbackType>? _feedbackTriggered;

    public static event Action<FeedbackType>? FeedbackTriggered
    {
        add => _feedbackTriggered += value;
        remove => _feedbackTriggered -= value;
    }

    public static void ForTap()
    {
        _feedbackTriggered?.Invoke(FeedbackType.Tap);
    }

    public static void ForLongPress()
    {
        _feedbackTriggered?.Invoke(FeedbackType.LongPress);
    }

    internal static void ResetForTests()
    {
        _feedbackTriggered = null;
    }
}
