using Plumix.Widgets;

namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/feedback.dart

public enum FeedbackType
{
    Tap,
    LongPress,
    SelectionClick,
}

public static class Feedback
{
    private static Action<FeedbackType>? _feedbackTriggered;

    public static async Task ForTap(BuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.FindRenderObject()!.SendSemanticsEvent(new TapSemanticEvent());
        _feedbackTriggered?.Invoke(FeedbackType.Tap);

        if (PlatformDefaults.TargetPlatform is TargetPlatform.Android or TargetPlatform.Fuchsia)
        {
            await SystemSound.Play(SystemSoundType.Click);
        }
    }

    public static Action? WrapForTap(Action? callback, BuildContext context)
    {
        if (callback is null)
        {
            return null;
        }

        return () =>
        {
            _ = ForTap(context);
            callback();
        };
    }

    public static Task ForLongPress(BuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.FindRenderObject()!.SendSemanticsEvent(new LongPressSemanticsEvent());
        _feedbackTriggered?.Invoke(FeedbackType.LongPress);

        return PlatformDefaults.TargetPlatform switch
        {
            TargetPlatform.Android or TargetPlatform.Fuchsia => HapticFeedback.Vibrate(),
            TargetPlatform.IOS => Task.WhenAll(
                SystemSound.Play(SystemSoundType.Click),
                HapticFeedback.HeavyImpact()),
            _ => Task.CompletedTask,
        };
    }

    public static Action? WrapForLongPress(Action? callback, BuildContext context)
    {
        if (callback is null)
        {
            return null;
        }

        return () =>
        {
            _ = ForLongPress(context);
            callback();
        };
    }

    // Context-free helpers are retained for the existing host feedback hook.
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

    public static void ForSelectionClick()
    {
        _feedbackTriggered?.Invoke(FeedbackType.SelectionClick);
    }

    internal static void ResetForTests()
    {
        _feedbackTriggered = null;
    }
}
