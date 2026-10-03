using Plumix.UI;

// C#-only test infrastructure: material_ui's test/feedback_tester.dart (`FeedbackTester`).

namespace Plumix.Tests;

/// <summary>
/// material-ui-src/test/feedback_tester.dart's <c>FeedbackTester</c>: counts the haptic and click-sound
/// requests the framework sends on <see cref="SystemChannels.Platform"/>.
/// </summary>
internal sealed class FeedbackTester : IDisposable
{
    private readonly MockMethodCallHandler _handler = new(SystemChannels.Platform);

    /// <summary>Number of times haptic feedback was requested (vibration).</summary>
    public int HapticCount => _handler.Log.Count(call => call.Method == "HapticFeedback.vibrate");

    /// <summary>Number of times the click sound was requested to play.</summary>
    public int ClickSoundCount => _handler.Log.Count(
        call => call.Method == "SystemSound.play" && Equals(call.Arguments, "SystemSoundType.click"));

    public void Dispose() => _handler.Dispose();
}
