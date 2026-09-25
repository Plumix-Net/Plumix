using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Physics;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/editable_text.dart

namespace Plumix.Widgets;

/// <summary>A key frame of a <see cref="DiscreteKeyFrameSimulation"/>. Dart's <c>_KeyFrame</c>.
/// </summary>
internal readonly record struct KeyFrame(double Time, double Value)
{
    // Values extracted from iOS 15.4 UIKit.
    public static readonly IReadOnlyList<KeyFrame> IOSBlinkingCaretKeyFrames =
    [
        new(0, 1), // 0
        new(0.5, 1), // 1
        new(0.5375, 0.75), // 2
        new(0.575, 0.5), // 3
        new(0.6125, 0.25), // 4
        new(0.65, 0), // 5
        new(0.85, 0), // 6
        new(0.8875, 0.25), // 7
        new(0.925, 0.5), // 8
        new(0.9625, 0.75), // 9
        new(1, 1), // 10
    ];
}

/// <summary>A simulation that holds each key frame's value until the next key frame's time.
/// Dart's <c>_DiscreteKeyFrameSimulation</c>.</summary>
internal sealed class DiscreteKeyFrameSimulation : Simulation
{
    private readonly IReadOnlyList<KeyFrame> _keyFrames;

    // The index of the KeyFrame corresponds to the most recent input `time`.
    private int _lastKeyFrameIndex;

    private DiscreteKeyFrameSimulation(IReadOnlyList<KeyFrame> keyFrames, double maxDuration)
    {
        if (Constants.KDebugMode)
        {
            if (keyFrames.Count == 0 || keyFrames[^1].Time > maxDuration)
            {
                throw new AssertionError("The key frames must be non-empty and end before maxDuration.");
            }

            for (int i = 0; i < keyFrames.Count - 1; i += 1)
            {
                if (keyFrames[i].Time > keyFrames[i + 1].Time)
                {
                    throw new AssertionError("The key frame sequence must be sorted by time.");
                }
            }
        }

        _keyFrames = keyFrames;
        MaxDuration = maxDuration;
    }

    /// <summary>Dart's <c>_DiscreteKeyFrameSimulation.iOSBlinkingCaret</c>.</summary>
    public static DiscreteKeyFrameSimulation IOSBlinkingCaret() => new(KeyFrame.IOSBlinkingCaretKeyFrames, 1);

    public double MaxDuration { get; }

    public override double DX(double time) => 0;

    public override bool IsDone(double time) => time >= MaxDuration;

    public override double X(double time)
    {
        int length = _keyFrames.Count;

        // Perform a linear search in the sorted key frame list, starting from the last key frame
        // found, since the input `time` usually monotonically increases by a small amount.
        int searchIndex;
        int endIndex;
        if (_keyFrames[_lastKeyFrameIndex].Time > time)
        {
            // The simulation may have restarted. Search within the index range
            // [0, _lastKeyFrameIndex).
            searchIndex = 0;
            endIndex = _lastKeyFrameIndex;
        }
        else
        {
            searchIndex = _lastKeyFrameIndex;
            endIndex = length;
        }

        // Find the target key frame. Don't have to check (endIndex - 1): if (endIndex - 2) doesn't
        // work we'll have to pick (endIndex - 1) anyways.
        while (searchIndex < endIndex - 1)
        {
            KeyFrame next = _keyFrames[searchIndex + 1];
            if (time < next.Time)
            {
                break;
            }

            searchIndex += 1;
        }

        _lastKeyFrameIndex = searchIndex;
        return _keyFrames[_lastKeyFrameIndex].Value;
    }
}

public sealed partial class EditableText
{
    /// <summary>
    /// Whether the blinking cursor is disabled, so it is always shown and does not blink. Dart's
    /// <c>EditableText.debugDeterministicCursor</c>, for tests that compare rendered pixels.
    /// </summary>
    public static bool DebugDeterministicCursor { get; set; }

    public sealed partial class EditableTextState
    {
        // The time it takes for the cursor to fade from fully opaque to fully transparent and vice
        // versa. A full cursor blink, from transparent to opaque to transparent, is twice this
        // duration.
        private static readonly TimeSpan CursorBlinkHalfPeriod = TimeSpan.FromMilliseconds(500);

        // Number of cursor ticks during which the most recently entered character is shown in an
        // obscured text field.
        private const int ObscureShowLatestCharCursorTicks = 3;

        private CursorTimer? _cursorTimer;
        private AnimationController? _backingCursorBlinkOpacityController;
        private DiscreteKeyFrameSimulation? _iosBlinkCursorSimulationBacking;
        private int _obscureShowCharTicksPending;
        private int? _obscureLatestCharIndex;

        // Whether `TickerMode.Of(context)` is true and animations (like blinking the cursor) are
        // supposed to run.
        private bool _tickersEnabled = true;

        // The cursor color the last build resolved, before the blink opacity is applied.
        private Color _resolvedCursorColor = new(0x00000000);

        private AnimationController CursorBlinkOpacityController
        {
            get
            {
                if (_backingCursorBlinkOpacityController is null)
                {
                    _backingCursorBlinkOpacityController = new AnimationController(vsync: this);
                    _backingCursorBlinkOpacityController.AddListener(OnCursorColorTick);
                }

                return _backingCursorBlinkOpacityController;
            }
        }

        private DiscreteKeyFrameSimulation IosBlinkCursorSimulation =>
            _iosBlinkCursorSimulationBacking ??= DiscreteKeyFrameSimulation.IOSBlinkingCaret();

        /// <summary>Whether the blinking cursor is actually visible at this precise moment (it's
        /// hidden half the time, since it blinks).</summary>
        public bool CursorCurrentlyVisible => CursorBlinkOpacityController.Value > 0;

        /// <summary>The cursor blink interval (the amount of time the cursor is in the "on" state
        /// or the "off" state). A complete cursor blink period is twice this value.</summary>
        public TimeSpan CursorBlinkInterval => CursorBlinkHalfPeriod;

        /// Dart's `widget.showCursor`, which the widget resolves to `showCursor ?? !readOnly`.
        private bool EffectiveShowCursor => Widget.ShowCursor ?? !Widget.ReadOnly;

        private bool ShowBlinkingCursor =>
            _focusNode?.HasFocus == true
            && EditingValue.Selection.IsCollapsed
            && EffectiveShowCursor
            && _tickersEnabled
            && RenderEditable?.FloatingCursorOn != true;

        private void OnCursorColorTick()
        {
            double effectiveOpacity = Math.Min(_resolvedCursorColor.Alpha / 255.0, CursorBlinkOpacityController.Value);
            if (RenderEditable is { } renderEditable)
            {
                renderEditable.CursorColor = _resolvedCursorColor.WithOpacity(effectiveOpacity);
            }

            UpdateCursorVisibility();
        }

        /// The cursor visibility Dart's `_onCursorColorTick` computes. The placeholder that hides
        /// the cursor is C#-only.
        private void UpdateCursorVisibility()
        {
            _cursorVisibilityNotifier.Value =
                EffectiveShowCursor
                && !ShowPlaceholder
                && (DebugDeterministicCursor || CursorBlinkOpacityController.Value > 0);
        }

        private void StartCursorBlink()
        {
            if (Constants.KDebugMode
                && _cursorTimer?.IsActive == true
                && _backingCursorBlinkOpacityController?.IsAnimating == true)
            {
                throw new AssertionError("The cursor timer and the blink animation must not run together.");
            }

            if (!EffectiveShowCursor)
            {
                return;
            }

            if (!_tickersEnabled)
            {
                return;
            }

            _cursorTimer?.Cancel();
            CursorBlinkOpacityController.SetValue(1.0);
            if (DebugDeterministicCursor)
            {
                return;
            }

            if (Widget.CursorOpacityAnimates)
            {
                CursorBlinkOpacityController.AnimateWith(IosBlinkCursorSimulation).WhenComplete(OnCursorTick);
            }
            else
            {
                _cursorTimer = CursorTimer.Periodic(CursorBlinkHalfPeriod, OnCursorTick);
            }
        }

        private void OnCursorTick()
        {
            if (_obscureShowCharTicksPending > 0)
            {
                _obscureShowCharTicksPending = PlatformDispatcher.Instance.BrieflyShowPassword
                    ? _obscureShowCharTicksPending - 1
                    : 0;
                if (_obscureShowCharTicksPending == 0)
                {
                    SetState(static () => { });
                }
            }

            if (Widget.CursorOpacityAnimates)
            {
                _cursorTimer?.Cancel();
                // Schedule this as an async task to avoid blocking tester.pumpAndSettle
                // indefinitely.
                _cursorTimer = CursorTimer.OneShot(
                    TimeSpan.Zero,
                    () => CursorBlinkOpacityController
                        .AnimateWith(IosBlinkCursorSimulation)
                        .WhenComplete(OnCursorTick));
            }
            else
            {
                if (_cursorTimer?.IsActive != true && _tickersEnabled)
                {
                    _cursorTimer = CursorTimer.Periodic(CursorBlinkHalfPeriod, OnCursorTick);
                }

                CursorBlinkOpacityController.SetValue(CursorBlinkOpacityController.Value == 0 ? 1 : 0);
            }
        }

        private void StopCursorBlink(bool resetCharTicks = true)
        {
            // If the cursor is animating, stop the animation, and we always want the cursor to be
            // visible when the floating cursor is enabled.
            CursorBlinkOpacityController.SetValue(RenderEditable?.FloatingCursorOn == true ? 1.0 : 0.0);
            _cursorTimer?.Cancel();
            _cursorTimer = null;
            if (resetCharTicks)
            {
                _obscureShowCharTicksPending = 0;
            }
        }

        private void StartOrStopCursorTimerIfNeeded()
        {
            if (!ShowBlinkingCursor)
            {
                StopCursorBlink();
            }
            else if (_cursorTimer is null)
            {
                StartCursorBlink();
            }
        }

        /// The ticker-mode part of Dart's `didChangeDependencies`: restart or stop the blinking
        /// cursor when <see cref="TickerMode"/> changes.
        private void DidChangeDependenciesForCursor()
        {
            bool newTickerEnabled = TickerMode.Of(Context);
            if (_tickersEnabled != newTickerEnabled)
            {
                _tickersEnabled = newTickerEnabled;
                if (ShowBlinkingCursor)
                {
                    StartCursorBlink();
                }
                else if (!_tickersEnabled && _cursorTimer is not null)
                {
                    StopCursorBlink();
                }
            }
        }

        /// The obscured text Dart's `buildTextSpan` shows: every character replaced by the
        /// obscuring character, except the most recently typed one for a few cursor ticks on
        /// mobile platforms when the platform allows it.
        private string BuildObscuredText(string text)
        {
            string obscured = string.Concat(Enumerable.Repeat(Widget.ObscuringCharacter, text.Length));
            bool brieflyShowPassword = PlatformDispatcher.Instance.BrieflyShowPassword
                                       && PlatformDefaults.TargetPlatform
                                           is TargetPlatform.Android or TargetPlatform.Fuchsia or TargetPlatform.IOS;
            if (brieflyShowPassword)
            {
                int? o = _obscureShowCharTicksPending > 0 ? _obscureLatestCharIndex : null;
                if (o is { } index && index >= 0 && index < obscured.Length)
                {
                    obscured = string.Concat(
                        obscured.AsSpan(0, index),
                        text.AsSpan(index, 1),
                        obscured.AsSpan(index + 1));
                }
            }

            return obscured;
        }

        /// <summary>A cancellable one-shot or periodic timer, like Dart's <c>Timer</c>.</summary>
        /// <remarks>C#-only: runs on <see cref="GestureTimer"/>, whose factory the test harness
        /// drives with its pump clock the way <c>FakeAsync</c> drives Dart's timers.</remarks>
        private sealed class CursorTimer
        {
            private GestureTimer? _timer;

            private CursorTimer()
            {
            }

            public bool IsActive { get; private set; } = true;

            public static CursorTimer OneShot(TimeSpan duration, Action callback)
            {
                var timer = new CursorTimer();
                timer._timer = GestureTimer.Start(duration, () =>
                {
                    timer.IsActive = false;
                    callback();
                });
                return timer;
            }

            public static CursorTimer Periodic(TimeSpan period, Action callback)
            {
                var timer = new CursorTimer();
                void Tick()
                {
                    if (!timer.IsActive)
                    {
                        return;
                    }

                    timer._timer = GestureTimer.Start(period, Tick);
                    callback();
                }

                timer._timer = GestureTimer.Start(period, Tick);
                return timer;
            }

            public void Cancel()
            {
                IsActive = false;
                _timer?.Cancel();
                _timer = null;
            }
        }
    }
}
