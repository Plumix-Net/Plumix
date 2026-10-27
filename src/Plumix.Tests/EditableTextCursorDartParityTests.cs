using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/test/widgets/editable_text_cursor_test.dart (the
// blinking cursor and the briefly shown password character) and the obscured-text and
// "selection behavior when receiving focus" tests of
// flutter/packages/flutter/test/widgets/editable_text_test.dart.
public sealed class EditableTextCursorDartParityTests : IDisposable
{
    private static readonly Color CursorColor = new Color(0xFF2196F3);

    private readonly RecordingTextInputControl _control = new();
    private readonly TextEditingController _controller = new();
    private readonly FocusNode _focusNode = new(debugLabel: "EditableText Node");

    public EditableTextCursorDartParityTests()
    {
        FocusManager.Instance.ResetForTests();
        UI.TextInput.SetInputControl(_control);
    }

    public void Dispose()
    {
        UI.TextInput.RestorePlatformInputControl();
        PlatformDefaults.DebugTargetPlatformOverride = null;
        PlatformDispatcher.Instance.UpdateBrieflyShowPassword(true);
        EditableText.DebugDeterministicCursor = false;
        FocusManager.Instance.ResetForTests();
    }

    public static TheoryData<TargetPlatform> MobilePlatforms =>
        new(TargetPlatform.Android, TargetPlatform.Fuchsia, TargetPlatform.IOS);

    private EditableText Field(
        bool obscureText = false,
        bool cursorOpacityAnimates = false,
        bool? showCursor = null,
        bool autofocus = true,
        bool multiline = false) =>
        new(
            controller: _controller,
            focusNode: _focusNode,
            autofocus: autofocus,
            style: new TextStyle(FontSize: 10.0),
            cursorColor: CursorColor,
            backgroundCursorColor: new Color(0xFF9E9E9E),
            obscureText: obscureText,
            cursorOpacityAnimates: cursorOpacityAnimates,
            showCursor: showCursor,
            maxLines: multiline ? null : 1);

    private static FrameworkDartTester Pump(Widget field, TargetPlatform platform = TargetPlatform.Android)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(Wrap(field));
        tester.Pump();
        return tester;
    }

    private static Widget Wrap(Widget field) => new TestWidgetsApp(
        home: new Align(
            alignment: Alignment.TopLeft,
            child: new SizedBox(width: 400, height: 300, child: field)));

    private static EditableText.EditableTextState State(FrameworkDartTester tester) =>
        tester.State<EditableText.EditableTextState>();

    private static RenderEditable Editable(FrameworkDartTester tester) =>
        tester.AllElements().Select(element => element.RenderObject).OfType<RenderEditable>().Distinct().Single();

    private static string Shown(FrameworkDartTester tester) => Editable(tester).Text!.ToPlainText();

    // Dart's `tester.enterText`: the platform replaces the text and puts the caret at its end.
    private static void EnterText(FrameworkDartTester tester, string text)
    {
        State(tester).UpdateEditingValue(new TextEditingValue(text, TextSelection.Collapsed(text.Length)));
        tester.Pump();
    }

    // ------------------------------------------------------------------ blinking

    [Fact]
    public void CursorCanAnimate()
    {
        using FrameworkDartTester tester = Pump(Field(cursorOpacityAnimates: true), TargetPlatform.IOS);
        RenderEditable renderEditable = Editable(tester);
        Assert.Equal(255, renderEditable.CursorColor!.Alpha);

        int walltimeMicrosecond = 0;
        double lastVerifiedOpacity = 1.0;

        void VerifyKeyFrame(double opacity, int at)
        {
            const int delta = 1;
            tester.Pump(TimeSpan.FromTicks((at - delta - walltimeMicrosecond) * 10L));
            // The opacity is verified immediately *before* each key frame.
            Assert.Equal(lastVerifiedOpacity, renderEditable.CursorColor!.Alpha / 255.0, 2);
            walltimeMicrosecond = at - delta;
            lastVerifiedOpacity = opacity;
        }

        VerifyKeyFrame(opacity: 1.0, at: 500000);
        VerifyKeyFrame(opacity: 0.75, at: 537500);
        VerifyKeyFrame(opacity: 0.5, at: 575000);
        VerifyKeyFrame(opacity: 0.25, at: 612500);
        VerifyKeyFrame(opacity: 0.0, at: 650000);
        VerifyKeyFrame(opacity: 0.0, at: 850000);
        VerifyKeyFrame(opacity: 0.25, at: 887500);
        VerifyKeyFrame(opacity: 0.5, at: 925000);
        VerifyKeyFrame(opacity: 0.75, at: 962500);
        VerifyKeyFrame(opacity: 1.0, at: 1000000);
    }

    [Fact]
    public void CursorIsStaticWhenCursorOpacityAnimatesIsFalse()
    {
        using FrameworkDartTester tester = Pump(Field(multiline: true));
        tester.PumpAndSettle();

        for (int i = 0; i < 40; i += 1)
        {
            tester.Pump(TimeSpan.FromMilliseconds(100));
            Assert.Equal(0, SchedulerBinding.Instance.TransientCallbackCount);
            int alpha = Editable(tester).CursorColor!.Alpha;
            Assert.True(alpha is 0 or 255, $"alpha {alpha}");
        }
    }

    [Fact]
    public void CursorDoesNotAnimateWithDebugDeterministicCursor()
    {
        EditableText.DebugDeterministicCursor = true;
        using FrameworkDartTester tester = Pump(Field(cursorOpacityAnimates: true, multiline: true));
        RenderEditable renderEditable = Editable(tester);
        Assert.Equal(255, renderEditable.CursorColor!.Alpha);

        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.Equal(255, renderEditable.CursorColor!.Alpha);
        Assert.True(renderEditable.ShowCursor.Value);

        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.Equal(255, renderEditable.CursorColor!.Alpha);

        // No more transient calls.
        tester.PumpAndSettle();
        Assert.Equal(255, renderEditable.CursorColor!.Alpha);
    }

    [Fact]
    public void CursorAnimationRestartsWhenItIsMovedUsingKeysOnDesktop()
    {
        _controller.Text = "Some text long enough to move the cursor around";
        using FrameworkDartTester tester = Pump(Field(), TargetPlatform.MacOS);
        RenderEditable renderEditable = Editable(tester);

        tester.Pump();
        Assert.Equal(255, renderEditable.CursorColor!.Alpha);

        // The cursor goes from exactly on to exactly off on the 500ms dot.
        tester.Pump(TimeSpan.FromMilliseconds(499));
        Assert.Equal(255, renderEditable.CursorColor!.Alpha);

        KeySim.SendKeyCombination(LogicalKeyboardKey.ArrowLeft);
        tester.Pump();
        Assert.Equal(255, renderEditable.CursorColor!.Alpha);

        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.Equal(255, renderEditable.CursorColor!.Alpha);

        tester.Pump(TimeSpan.FromMilliseconds(299));
        Assert.Equal(255, renderEditable.CursorColor!.Alpha);

        KeySim.SendKeyCombination(LogicalKeyboardKey.ArrowRight);
        tester.Pump();
        KeySim.SendKeyCombination(LogicalKeyboardKey.ArrowRight);
        tester.Pump();
        Assert.Equal(255, renderEditable.CursorColor!.Alpha);

        tester.Pump(TimeSpan.FromMilliseconds(200));
        Assert.Equal(255, renderEditable.CursorColor!.Alpha);

        tester.Pump(TimeSpan.FromMilliseconds(299));
        Assert.Equal(255, renderEditable.CursorColor!.Alpha);

        tester.Pump(TimeSpan.FromMilliseconds(1));
        Assert.Equal(0, renderEditable.CursorColor!.Alpha);
        Assert.False(renderEditable.ShowCursor.Value);
    }

    [Fact]
    public void CursorDoesNotShowWhenShowCursorIsFalse()
    {
        using FrameworkDartTester tester = Pump(Field(showCursor: false, multiline: true));
        RenderEditable renderEditable = Editable(tester);

        for (int i = 0; i < 3; i += 1)
        {
            tester.Pump(TimeSpan.FromMilliseconds(200));
            Assert.False(renderEditable.ShowCursor.Value);
            Assert.False(State(tester).CursorCurrentlyVisible);
        }
    }

    [Fact]
    public void CursorDoesNotShowWhenNotFocused()
    {
        using FrameworkDartTester tester = Pump(Field());
        Assert.True(_focusNode.HasFocus);
        RenderEditable renderEditable = Editable(tester);

        _focusNode.Unfocus();
        tester.Pump();
        for (int i = 0; i < 10; i += 1)
        {
            Assert.False(renderEditable.ShowCursor.Value);
            Assert.Equal(0, SchedulerBinding.Instance.TransientCallbackCount);
            tester.Pump(TimeSpan.FromMilliseconds(29));
        }

        // Refocus and it should show the caret.
        _focusNode.RequestFocus();
        tester.Pump();
        tester.Pump(TimeSpan.FromMilliseconds(100));
        Assert.True(renderEditable.ShowCursor.Value);
    }

    [Fact]
    public void DraggingTheFloatingCursorStopsTheBlinking()
    {
        _controller.Text = "hello world this is fun and cool and awesome!";
        using FrameworkDartTester tester = Pump(Field(), TargetPlatform.IOS);
        EditableText.EditableTextState state = State(tester);

        // Check that the cursor visibility toggles after each blink interval. Or if it's not
        // blinking at all, it stays on.
        void CheckCursorBlinking(bool isBlinking = true)
        {
            bool initialShowCursor = true;
            if (isBlinking)
            {
                initialShowCursor = state.CursorCurrentlyVisible;
            }

            TimeSpan interval = state.CursorBlinkInterval;
            tester.Pump(interval);
            Assert.Equal(isBlinking ? !initialShowCursor : initialShowCursor, state.CursorCurrentlyVisible);
            tester.Pump(interval);
            Assert.Equal(initialShowCursor, state.CursorCurrentlyVisible);
            tester.Pump(interval / 10);
            Assert.Equal(initialShowCursor, state.CursorCurrentlyVisible);
            tester.Pump(interval);
            Assert.Equal(isBlinking ? !initialShowCursor : initialShowCursor, state.CursorCurrentlyVisible);
            tester.Pump(interval);
            Assert.Equal(initialShowCursor, state.CursorCurrentlyVisible);
        }

        // Before dragging, the cursor should blink.
        CheckCursorBlinking();

        state.UpdateFloatingCursor(new RawFloatingCursorPoint(FloatingCursorDragState.Start));
        // When drag cursor, the cursor shouldn't blink.
        CheckCursorBlinking(isBlinking: false);

        state.UpdateFloatingCursor(new RawFloatingCursorPoint(FloatingCursorDragState.End));
        tester.PumpAndSettle();
        // After dragging, the cursor should blink.
        CheckCursorBlinking();
    }

    [Fact]
    public void TurningShowCursorOffStopsTheCursor()
    {
        using FrameworkDartTester tester = Pump(Field(showCursor: false, cursorOpacityAnimates: true));
        EditableText.EditableTextState state = State(tester);

        // No cursor even when focused.
        Assert.False(state.CursorCurrentlyVisible);

        // The EditableText still has focus, so the cursor should start blinking.
        tester.PumpWidget(Wrap(Field(showCursor: true, cursorOpacityAnimates: true)));
        Assert.True(state.CursorCurrentlyVisible);
        tester.Pump();
        Assert.True(state.CursorCurrentlyVisible);

        tester.PumpWidget(Wrap(Field(showCursor: false, cursorOpacityAnimates: true)));
        Assert.False(state.CursorCurrentlyVisible);
        tester.Pump();
        Assert.False(state.CursorCurrentlyVisible);
    }

    [Fact]
    public void CursorBlinkIntervalIsHalfASecond()
    {
        using FrameworkDartTester tester = Pump(Field());
        Assert.Equal(TimeSpan.FromMilliseconds(500), State(tester).CursorBlinkInterval);
    }

    // ------------------------------------------------ briefly shown password character

    [Theory]
    [MemberData(nameof(MobilePlatforms))]
    public void PasswordBrieflyShowsLastCharacterWhenEnteredOnMobile(TargetPlatform platform)
    {
        using FrameworkDartTester tester = Pump(Field(obscureText: true), platform);

        EnterText(tester, "AA");
        EnterText(tester, "AAA");
        Assert.Equal("••A", Shown(tester));

        tester.Pump(TimeSpan.FromMilliseconds(500));
        tester.Pump(TimeSpan.FromMilliseconds(500));
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal("•••", Shown(tester));
    }

    [Theory]
    [InlineData(TargetPlatform.Linux)]
    [InlineData(TargetPlatform.MacOS)]
    [InlineData(TargetPlatform.Windows)]
    public void PasswordNeverShowsTheLastCharacterOnDesktop(TargetPlatform platform)
    {
        using FrameworkDartTester tester = Pump(Field(obscureText: true), platform);

        EnterText(tester, "AA");
        EnterText(tester, "AAA");
        Assert.Equal("•••", Shown(tester));
    }

    [Fact]
    public void PasswordBrieflyDoesNotShowLastCharacterWhenDisabledBySystem()
    {
        using FrameworkDartTester tester = Pump(Field(obscureText: true));

        EnterText(tester, "AA");
        EnterText(tester, "AAA");
        PlatformDispatcher.Instance.UpdateBrieflyShowPassword(false);

        // Nothing rebuilt yet, so the character is still shown.
        Assert.Equal("••A", Shown(tester));

        // The next cursor tick hides it at once.
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal("•••", Shown(tester));

        tester.Pump(TimeSpan.FromMilliseconds(500));
        tester.Pump(TimeSpan.FromMilliseconds(500));
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal("•••", Shown(tester));
    }

    [Theory]
    [MemberData(nameof(MobilePlatforms))]
    public void TogglingObscureTextHidesTheLastCharacterAgain(TargetPlatform platform)
    {
        using FrameworkDartTester tester = Pump(Field(obscureText: true), platform);

        EnterText(tester, "H");
        EnterText(tester, "HH");
        Assert.Equal("•H", Shown(tester));
        tester.Pump(TimeSpan.FromMilliseconds(500));
        tester.Pump(TimeSpan.FromMilliseconds(500));
        tester.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal("••", Shown(tester));

        EnterText(tester, "HHH");
        Assert.Equal("••H", Shown(tester));

        tester.PumpWidget(Wrap(Field(obscureText: false)));
        Assert.Equal("HHH", Shown(tester));

        tester.PumpWidget(Wrap(Field(obscureText: true)));
        Assert.Equal("•••", Shown(tester));
    }

    [Fact]
    public void ProgrammaticChangesNeverRevealACharacter()
    {
        using FrameworkDartTester tester = Pump(Field(obscureText: true));

        _controller.Text = "secret";
        tester.Pump();
        Assert.Equal("••••••", Shown(tester));
    }

    // ------------------------------------------------- autofocus lifecycle

    [Theory]
    [InlineData(TargetPlatform.Android)]
    [InlineData(TargetPlatform.Fuchsia)]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.Linux)]
    [InlineData(TargetPlatform.MacOS)]
    [InlineData(TargetPlatform.Windows)]
    public void AutofocusPreservesValidSelectionAndCollapsesInvalidSelectionAtTheEnd(TargetPlatform platform)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        TextSelection[] selections = [TextSelection.Collapsed(-1), TextSelection.Collapsed(3), new(2, 5)];

        foreach (TextSelection selection in selections)
        {
            tester.PumpWidget(new SizedBox());
            _controller.Value = new TextEditingValue("Flutter!", selection);
            tester.PumpWidget(Wrap(Field()));

            Assert.True(_focusNode.HasFocus);
            TextSelection expected = selection.IsValid ? selection : TextSelection.Collapsed(8);
            Assert.Equal(expected, _controller.Selection);

            // Focus completes after the first frame; the next frame paints that selection.
            tester.Pump();
            Assert.Equal(expected, Editable(tester).Selection);
        }
    }

    [Fact]
    public void AutofocusWithoutLayoutIsSkippedAndNeverRetried()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        _controller.Text = "Flutter!";
        Widget Build(bool tickersEnabled) => Wrap(new TickerMode(enabled: tickersEnabled, child: Field()));
        tester.PumpWidget(Build(true), phase: EnginePhase.Build);

        Assert.False(Editable(tester).HasSize);
        Assert.False(_focusNode.HasFocus);
        Assert.Equal(TextSelection.Collapsed(-1), _controller.Selection);

        tester.PumpWidget(Build(false));
        tester.PumpWidget(Build(true));

        Assert.True(Editable(tester).HasSize);
        Assert.False(_focusNode.HasFocus);
        Assert.Equal(TextSelection.Collapsed(-1), _controller.Selection);
    }

    [Fact]
    public void EnablingAutofocusWaitsForADependencyChange()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        Widget Build(bool autofocus, bool tickersEnabled = true) =>
            Wrap(new TickerMode(enabled: tickersEnabled, child: Field(autofocus: autofocus)));
        tester.PumpWidget(Build(false));
        tester.PumpWidget(Build(true));
        Assert.False(_focusNode.HasFocus);

        tester.PumpWidget(Build(true, tickersEnabled: false));
        Assert.True(_focusNode.HasFocus);

        _focusNode.Unfocus();
        tester.Pump();
        tester.PumpWidget(Build(true));
        Assert.False(_focusNode.HasFocus);
    }

    [Fact]
    public void MultilineAutofocusOnTheFirstFramePreservesUpstreamAffinity()
    {
        _controller.Value = new TextEditingValue(
            "Flutter!", new TextSelection(0, 0, TextAffinity.Upstream));
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(Wrap(new EditableText(
            controller: _controller,
            focusNode: _focusNode,
            autofocus: true,
            maxLines: 10,
            showSelectionHandles: true,
            style: new TextStyle(FontSize: 10.0),
            cursorColor: CursorColor,
            backgroundCursorColor: new Color(0xFF9E9E9E))));
        tester.PumpAndSettle();

        Assert.True(_focusNode.HasFocus);
        Assert.Equal(new TextSelection(0, 0, TextAffinity.Upstream), _controller.Selection);
        Assert.Null(tester.TakeException());
    }

    // ------------------------------------------------- focus after the app resumed

    [Theory]
    [InlineData(TargetPlatform.Android)]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.Linux)]
    [InlineData(TargetPlatform.MacOS)]
    [InlineData(TargetPlatform.Windows)]
    public void FocusRegainedAfterTheAppResumedKeepsTheCollapsedSelection(TargetPlatform platform)
    {
        _controller.Text = "Flutter!";
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(Wrap(Field()));
        FocusManager.Instance.ListenToApplicationLifecycleChangesIfSupported();
        Assert.True(_focusNode.HasFocus);
        Assert.Equal(TextSelection.Collapsed(8), _controller.Selection);

        try
        {
            WidgetsBinding.Instance.HandleAppLifecycleStateChanged(AppLifecycleState.Inactive);
            tester.Pump();
            WidgetsBinding.Instance.HandleAppLifecycleStateChanged(AppLifecycleState.Resumed);
            tester.Pump();

            Assert.True(_focusNode.HasFocus);
            Assert.Equal(TextSelection.Collapsed(8), _controller.Selection);
        }
        finally
        {
            WidgetsBinding.Instance.HandleAppLifecycleStateChanged(AppLifecycleState.Resumed);
            WidgetsBinding.Instance.ResetLifecycleStateForTests();
        }
    }

    [Theory]
    [InlineData(TargetPlatform.Linux)]
    [InlineData(TargetPlatform.MacOS)]
    [InlineData(TargetPlatform.Windows)]
    public void MovingFocusAfterTheAppResumedSelectsAllOnDesktop(TargetPlatform platform)
    {
        _controller.Text = "Flutter!";
        using var secondController = new TextEditingController("Dart!");
        using var secondFocusNode = new FocusNode(debugLabel: "Second");
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(new TestWidgetsApp(home: new Column(children:
        [
            Field(),
            new EditableText(
                controller: secondController,
                focusNode: secondFocusNode,
                style: new TextStyle(FontSize: 10.0),
                cursorColor: CursorColor,
                backgroundCursorColor: new Color(0xFF9E9E9E),
                maxLines: 1),
        ])));
        FocusManager.Instance.ListenToApplicationLifecycleChangesIfSupported();
        Assert.True(_focusNode.HasFocus);

        try
        {
            WidgetsBinding.Instance.HandleAppLifecycleStateChanged(AppLifecycleState.Inactive);
            tester.Pump();
            WidgetsBinding.Instance.HandleAppLifecycleStateChanged(AppLifecycleState.Resumed);
            tester.Pump();

            KeySim.SendKeyCombination(LogicalKeyboardKey.Tab);
            tester.Pump();

            Assert.True(secondFocusNode.HasFocus);
            Assert.Equal(TextSelection.Collapsed(8), _controller.Selection);
            Assert.Equal(new TextSelection(0, 5), secondController.Selection);
        }
        finally
        {
            WidgetsBinding.Instance.HandleAppLifecycleStateChanged(AppLifecycleState.Resumed);
            WidgetsBinding.Instance.ResetLifecycleStateForTests();
        }
    }

    private sealed class RecordingTextInputControl : TextInputControl
    {
        public override void SetEditingState(TextEditingValue value)
        {
        }
    }
}
