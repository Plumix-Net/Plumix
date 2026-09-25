using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using Color = Avalonia.Media.Color;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/test/widgets/editable_text_shortcuts_test.dart
//
// The keyboard behavior of `EditableText`: every key reaches the field through
// `DefaultTextEditingShortcuts` (in `WidgetsApp`) and the field's own `Actions`. A key the
// framework leaves unhandled goes on to the platform text input plugin
// (`KeySim.SendKeyCombination`), which on macOS turns it into AppKit selectors exactly like
// flutter_test's `MacOSTestTextInputKeyHandler`. Each Dart `TargetPlatformVariant` is a theory row.

public sealed class EditableTextShortcutsDartParityTests : IDisposable
{
    private const string TestText =
        "Now is the time for\n" // 20
        + "all good people\n" // 20 + 16 => 36
        + "to come to the aid\n" // 36 + 19 => 55
        + "of their country."; // 55 + 17 => 72

    private const string TestCluster = "👨‍👩‍👦👨‍👩‍👦👨‍👩‍👦"; // 8 * 3

    // Exactly 20 characters each line.
    private const string TestSoftwrapText =
        "0123456789ABCDEFGHIJ"
        + "0123456789ABCDEFGHIJ"
        + "0123456789ABCDEFGHIJ"
        + "0123456789ABCDEFGHIJ";

    private readonly TextEditingController _controller = new(TestText);
    private readonly ScrollController _scrollController = new();
    private readonly FocusNode _focusNode = new();
    private readonly MockClipboardPlatform _clipboard = new("empty");

    public EditableTextShortcutsDartParityTests()
    {
        FocusManager.Instance.ResetForTests();
    }

    public void Dispose()
    {
        PlatformDefaults.DebugTargetPlatformOverride = null;
        _clipboard.Dispose();
        FocusManager.Instance.ResetForTests();
    }

    public static TheoryData<TargetPlatform> AllPlatforms =>
        new(Enum.GetValues<TargetPlatform>());

    public static TheoryData<TargetPlatform> AllExceptIOS =>
        new(Enum.GetValues<TargetPlatform>().Where(platform => platform != TargetPlatform.IOS));

    public static TheoryData<TargetPlatform> AllExceptApple =>
        new(Enum.GetValues<TargetPlatform>()
            .Where(platform => platform is not TargetPlatform.IOS and not TargetPlatform.MacOS));

    private static bool IsApple(TargetPlatform platform) =>
        platform is TargetPlatform.IOS or TargetPlatform.MacOS;

    private Widget BuildEditableText(
        TextAlign textAlign = TextAlign.Left,
        bool readOnly = false,
        bool obscured = false,
        bool enableInteractiveSelection = true) =>
        new TestWidgetsApp(
            home: new Align(
                alignment: Alignment.TopLeft,
                child: new SizedBox(
                    // Softwrap at exactly 20 characters.
                    width: 201,
                    height: 200,
                    child: new EditableText(
                        controller: _controller,
                        showSelectionHandles: true,
                        autofocus: true,
                        focusNode: _focusNode,
                        style: new TextStyle(FontSize: 10.0),
                        padding: new Avalonia.Thickness(0),
                        // Avoid the cursor from taking up width.
                        cursorWidth: 0,
                        cursorColor: Color.FromUInt32(0xFF2196F3),
                        backgroundCursorColor: Color.FromUInt32(0xFF9E9E9E),
                        selectionControls: EmptyTextSelectionControls.Instance,
                        keyboardType: TextInputType.Text,
                        multiline: !obscured,
                        maxLines: null,
                        readOnly: readOnly,
                        textAlign: textAlign,
                        obscureText: obscured,
                        enableInteractiveSelection: enableInteractiveSelection,
                        scrollController: _scrollController))));

    private FrameworkDartTester Pump(TargetPlatform platform, Widget widget)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        var tester = new FrameworkDartTester();
        tester.PumpWidget(widget);
        tester.Pump();
        return tester;
    }

    private static void Send(
        LogicalKeyboardKey key,
        bool control = false,
        bool shift = false,
        bool alt = false,
        bool meta = false) =>
        KeySim.SendKeyCombination(key, control, shift, alt, meta);

    // `wordModifier`: alt on Apple platforms, control elsewhere.
    private static void SendWordModifier(TargetPlatform platform, LogicalKeyboardKey key, bool shift = false) =>
        Send(key, control: !IsApple(platform), alt: IsApple(platform), shift: shift);

    // `lineModifier`: meta on Apple platforms, alt elsewhere.
    private static void SendLineModifier(TargetPlatform platform, LogicalKeyboardKey key, bool shift = false) =>
        Send(key, alt: !IsApple(platform), meta: IsApple(platform), shift: shift);

    private void SetValue(string text, TextSelection selection)
    {
        _controller.Text = text;
        _controller.Selection = selection;
    }

    // ------------------------------------------------------------------ backspace

    [Theory]
    [MemberData(nameof(AllExceptIOS))]
    public void Backspace(TargetPlatform platform)
    {
        // Move the selection to the beginning of the 2nd line (after the newline character).
        SetValue(TestText, TextSelection.Collapsed(20, TextAffinity.Upstream));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        Send(LogicalKeyboardKey.Backspace);

        Assert.Equal(
            "Now is the time forall good people\n" + "to come to the aid\n" + "of their country.",
            _controller.Text);
        Assert.Equal(TextSelection.Collapsed(19), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void BackspaceReadonly(TargetPlatform platform)
    {
        SetValue(TestText, TextSelection.Collapsed(20, TextAffinity.Upstream));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText(readOnly: true));
        Send(LogicalKeyboardKey.Backspace);

        Assert.Equal(TestText, _controller.Text);
        Assert.Equal(TextSelection.Collapsed(20, TextAffinity.Upstream), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void BackspaceAtStart(TargetPlatform platform)
    {
        SetValue(TestText, TextSelection.Collapsed(0));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        Send(LogicalKeyboardKey.Backspace);

        Assert.Equal(TestText, _controller.Text);
        Assert.Equal(TextSelection.Collapsed(0), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllExceptIOS))]
    public void BackspaceAtEnd(TargetPlatform platform)
    {
        SetValue(TestText, TextSelection.Collapsed(72, TextAffinity.Upstream));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        Send(LogicalKeyboardKey.Backspace);

        Assert.Equal(TestText[..^1], _controller.Text);
        Assert.Equal(TextSelection.Collapsed(71), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllExceptIOS))]
    public void BackspaceInsideOfACluster(TargetPlatform platform)
    {
        SetValue(TestCluster, TextSelection.Collapsed(1, TextAffinity.Upstream));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        Send(LogicalKeyboardKey.Backspace);

        Assert.Equal("👨‍👩‍👦👨‍👩‍👦", _controller.Text);
        Assert.Equal(TextSelection.Collapsed(0), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllExceptIOS))]
    public void BackspaceAtClusterBoundary(TargetPlatform platform)
    {
        SetValue(TestCluster, TextSelection.Collapsed(8, TextAffinity.Upstream));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        Send(LogicalKeyboardKey.Backspace);

        Assert.Equal("👨‍👩‍👦👨‍👩‍👦", _controller.Text);
        Assert.Equal(TextSelection.Collapsed(0), _controller.Selection);
    }

    // --------------------------------------------------------------------- delete

    [Theory]
    [MemberData(nameof(AllExceptIOS))]
    public void Delete(TargetPlatform platform)
    {
        SetValue(TestText, TextSelection.Collapsed(20, TextAffinity.Upstream));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        Send(LogicalKeyboardKey.Delete);

        Assert.Equal(
            "Now is the time for\n" + "ll good people\n" + "to come to the aid\n" + "of their country.",
            _controller.Text);
        Assert.Equal(TextSelection.Collapsed(20), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllExceptIOS))]
    public void DeleteReadonly(TargetPlatform platform)
    {
        SetValue(TestText, TextSelection.Collapsed(20, TextAffinity.Upstream));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText(readOnly: true));
        Send(LogicalKeyboardKey.Delete);

        Assert.Equal(TestText, _controller.Text);
        Assert.Equal(TextSelection.Collapsed(20, TextAffinity.Upstream), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllExceptIOS))]
    public void DeleteAtStart(TargetPlatform platform)
    {
        SetValue(TestText, TextSelection.Collapsed(0));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        Send(LogicalKeyboardKey.Delete);

        Assert.Equal(TestText[1..], _controller.Text);
        Assert.Equal(TextSelection.Collapsed(0), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void DeleteAtEnd(TargetPlatform platform)
    {
        SetValue(TestText, TextSelection.Collapsed(72, TextAffinity.Upstream));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        Send(LogicalKeyboardKey.Delete);

        Assert.Equal(TestText, _controller.Text);
        Assert.Equal(TextSelection.Collapsed(72, TextAffinity.Upstream), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllExceptIOS))]
    public void DeleteInsideOfACluster(TargetPlatform platform)
    {
        SetValue(TestCluster, TextSelection.Collapsed(1, TextAffinity.Upstream));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        Send(LogicalKeyboardKey.Delete);

        Assert.Equal("👨‍👩‍👦👨‍👩‍👦", _controller.Text);
        Assert.Equal(TextSelection.Collapsed(0), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllExceptIOS))]
    public void DeleteAtClusterBoundary(TargetPlatform platform)
    {
        SetValue(TestCluster, TextSelection.Collapsed(8, TextAffinity.Upstream));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        Send(LogicalKeyboardKey.Delete);

        Assert.Equal("👨‍👩‍👦👨‍👩‍👦", _controller.Text);
        Assert.Equal(TextSelection.Collapsed(8), _controller.Selection);
    }

    // -------------------------------------------------------- non-collapsed delete

    [Theory]
    [MemberData(nameof(AllExceptIOS))]
    public void NonCollapsedDelete_InsideOfACluster(TargetPlatform platform)
    {
        SetValue(TestCluster, new TextSelection(9, 12));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        Send(LogicalKeyboardKey.Delete);

        Assert.Equal("👨‍👩‍👦👨‍👩‍👦", _controller.Text);
        Assert.Equal(TextSelection.Collapsed(8), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllExceptIOS))]
    public void NonCollapsedDelete_AtTheBoundariesOfACluster(TargetPlatform platform)
    {
        SetValue(TestCluster, new TextSelection(8, 16));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        Send(LogicalKeyboardKey.Delete);

        Assert.Equal("👨‍👩‍👦👨‍👩‍👦", _controller.Text);
        Assert.Equal(TextSelection.Collapsed(8), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllExceptIOS))]
    public void NonCollapsedDelete_CrossCluster(TargetPlatform platform)
    {
        SetValue(TestCluster, new TextSelection(1, 9));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        Send(LogicalKeyboardKey.Delete);

        Assert.Equal("👨‍👩‍👦", _controller.Text);
        Assert.Equal(TextSelection.Collapsed(0), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllExceptIOS))]
    public void NonCollapsedDelete_CrossClusterObscuredText(TargetPlatform platform)
    {
        SetValue(TestCluster, new TextSelection(1, 9));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText(obscured: true));
        Send(LogicalKeyboardKey.Delete);

        // The code point boundary expands only to the surrogate pairs.
        Assert.Equal("‍👩‍👦👨‍👩‍👦", _controller.Text);
        Assert.Equal(TextSelection.Collapsed(0), _controller.Selection);
    }

    // ------------------------------------------------- word modifier + backspace

    [Theory]
    [MemberData(nameof(AllExceptIOS))]
    public void WordModifierBackspace(TargetPlatform platform)
    {
        SetValue(TestText, TextSelection.Collapsed(29, TextAffinity.Upstream));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        SendWordModifier(platform, LogicalKeyboardKey.Backspace);

        Assert.Equal(
            "Now is the time for\n" + "all people\n" + "to come to the aid\n" + "of their country.",
            _controller.Text);
        Assert.Equal(TextSelection.Collapsed(24), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void WordModifierBackspace_Readonly(TargetPlatform platform)
    {
        SetValue(TestText, TextSelection.Collapsed(29, TextAffinity.Upstream));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText(readOnly: true));
        SendWordModifier(platform, LogicalKeyboardKey.Backspace);

        Assert.Equal(TestText, _controller.Text);
        Assert.Equal(TextSelection.Collapsed(29, TextAffinity.Upstream), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllExceptIOS))]
    public void WordModifierBackspace_AtEnd(TargetPlatform platform)
    {
        SetValue(TestText, TextSelection.Collapsed(72));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        SendWordModifier(platform, LogicalKeyboardKey.Backspace);

        Assert.Equal(
            "Now is the time for\n" + "all good people\n" + "to come to the aid\n" + "of their ",
            _controller.Text);
        Assert.Equal(TextSelection.Collapsed(64), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllExceptIOS))]
    public void WordModifierDelete(TargetPlatform platform)
    {
        SetValue(TestText, TextSelection.Collapsed(23, TextAffinity.Upstream));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        SendWordModifier(platform, LogicalKeyboardKey.Delete);

        Assert.Equal(
            "Now is the time for\n" + "all people\n" + "to come to the aid\n" + "of their country.",
            _controller.Text);
        Assert.Equal(TextSelection.Collapsed(23), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllExceptIOS))]
    public void WordModifierDelete_AtStart(TargetPlatform platform)
    {
        SetValue(TestText, TextSelection.Collapsed(0));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        SendWordModifier(platform, LogicalKeyboardKey.Delete);

        Assert.Equal(TestText[3..], _controller.Text);
        Assert.Equal(TextSelection.Collapsed(0), _controller.Selection);
    }

    // ------------------------------------------------- line modifier + backspace

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void LineModifierBackspace(TargetPlatform platform)
    {
        SetValue(TestText, TextSelection.Collapsed(29, TextAffinity.Upstream));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        SendLineModifier(platform, LogicalKeyboardKey.Backspace);

        Assert.Equal(
            "Now is the time for\n" + "people\n" + "to come to the aid\n" + "of their country.",
            _controller.Text);
        Assert.Equal(TextSelection.Collapsed(20), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void LineModifierBackspace_SoftwrapLineBoundaryUpstream(TargetPlatform platform)
    {
        SetValue(TestSoftwrapText, TextSelection.Collapsed(40, TextAffinity.Upstream));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        SendLineModifier(platform, LogicalKeyboardKey.Backspace);

        Assert.Equal(TestSoftwrapText[..20] + TestSoftwrapText[40..], _controller.Text);
        Assert.Equal(TextSelection.Collapsed(20), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void LineModifierBackspace_SoftwrapLineBoundaryDownstream(TargetPlatform platform)
    {
        SetValue(TestSoftwrapText, TextSelection.Collapsed(40));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        SendLineModifier(platform, LogicalKeyboardKey.Backspace);

        Assert.Equal(TestSoftwrapText, _controller.Text);
        Assert.Equal(TextSelection.Collapsed(40), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void LineModifierBackspace_AtEnd(TargetPlatform platform)
    {
        SetValue(TestText, TextSelection.Collapsed(72));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        SendLineModifier(platform, LogicalKeyboardKey.Backspace);

        Assert.Equal("Now is the time for\n" + "all good people\n" + "to come to the aid\n", _controller.Text);
        Assert.Equal(TextSelection.Collapsed(55), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void LineModifierDelete(TargetPlatform platform)
    {
        SetValue(TestText, TextSelection.Collapsed(23, TextAffinity.Upstream));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        SendLineModifier(platform, LogicalKeyboardKey.Delete);

        Assert.Equal(
            "Now is the time for\n" + "all\n" + "to come to the aid\n" + "of their country.",
            _controller.Text);
        Assert.Equal(TextSelection.Collapsed(23), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void LineModifierDelete_SoftwrapLineBoundaryUpstream(TargetPlatform platform)
    {
        SetValue(TestSoftwrapText, TextSelection.Collapsed(40, TextAffinity.Upstream));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        SendLineModifier(platform, LogicalKeyboardKey.Delete);

        Assert.Equal(TestSoftwrapText, _controller.Text);
        Assert.Equal(TextSelection.Collapsed(40, TextAffinity.Upstream), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void LineModifierDelete_SoftwrapLineBoundaryDownstream(TargetPlatform platform)
    {
        SetValue(TestSoftwrapText, TextSelection.Collapsed(40));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        SendLineModifier(platform, LogicalKeyboardKey.Delete);

        Assert.Equal(TestSoftwrapText[..40] + TestSoftwrapText[60..], _controller.Text);
        Assert.Equal(TextSelection.Collapsed(40), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void LineModifierDelete_InsideOfACluster(TargetPlatform platform)
    {
        SetValue(TestCluster, TextSelection.Collapsed(1));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        SendLineModifier(platform, LogicalKeyboardKey.Delete);

        Assert.Equal(string.Empty, _controller.Text);
        Assert.Equal(TextSelection.Collapsed(0), _controller.Selection);
    }

    // ---------------------------------------------------------- arrow movement

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void ArrowLeft_AtStart_EveryModifierKeepsTheCaret(TargetPlatform platform)
    {
        SetValue(TestText, TextSelection.Collapsed(0));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        foreach (bool shift in new[] { false, true })
        {
            foreach (bool control in new[] { false, true })
            {
                foreach (bool alt in new[] { false, true })
                {
                    foreach (bool meta in new[] { false, true })
                    {
                        Send(LogicalKeyboardKey.ArrowLeft, control, shift, alt, meta);
                        Assert.Equal(TextSelection.Collapsed(0), _controller.Selection);
                    }
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void ArrowLeft_BaseMovement(TargetPlatform platform)
    {
        SetValue(TestText, TextSelection.Collapsed(20));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        Send(LogicalKeyboardKey.ArrowLeft);

        Assert.Equal(TextSelection.Collapsed(19), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllExceptApple))]
    public void ArrowLeft_WordModifier(TargetPlatform platform)
    {
        SetValue(TestText, TextSelection.Collapsed(7)); // Before "the"
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        Send(LogicalKeyboardKey.ArrowLeft, control: true);

        Assert.Equal(TextSelection.Collapsed(4), _controller.Selection); // Before "is"
    }

    [Theory]
    [MemberData(nameof(AllExceptApple))]
    public void ArrowLeft_LineModifier(TargetPlatform platform)
    {
        SetValue(TestText, TextSelection.Collapsed(24)); // Before "good".
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        Send(LogicalKeyboardKey.ArrowLeft, alt: true);

        Assert.Equal(TextSelection.Collapsed(20), _controller.Selection); // Before "all".
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void ArrowRight_BaseMovement(TargetPlatform platform)
    {
        SetValue(TestText, TextSelection.Collapsed(20));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        Send(LogicalKeyboardKey.ArrowRight);

        Assert.Equal(TextSelection.Collapsed(21), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllExceptApple))]
    public void ArrowRight_WordModifier(TargetPlatform platform)
    {
        SetValue(TestText, TextSelection.Collapsed(7)); // Before "the"
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        Send(LogicalKeyboardKey.ArrowRight, control: true);

        Assert.Equal(TextSelection.Collapsed(10), _controller.Selection); // After "the"
    }

    [Theory]
    [MemberData(nameof(AllExceptApple))]
    public void ArrowRight_LineModifier(TargetPlatform platform)
    {
        SetValue(TestText, TextSelection.Collapsed(24)); // Before "good".
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        Send(LogicalKeyboardKey.ArrowRight, alt: true);

        // After "people".
        Assert.Equal(TextSelection.Collapsed(35, TextAffinity.Upstream), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void NonCollapsedSelection_BaseArrowMovementCollapses(TargetPlatform platform)
    {
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        foreach (TextSelection initial in new[] { new TextSelection(20, 23), new TextSelection(23, 20) })
        {
            _controller.Selection = initial;
            tester.Pump();
            Send(LogicalKeyboardKey.ArrowLeft);
            Assert.Equal(TextSelection.Collapsed(20), _controller.Selection);

            _controller.Selection = initial;
            tester.Pump();
            Send(LogicalKeyboardKey.ArrowRight);
            Assert.Equal(TextSelection.Collapsed(23), _controller.Selection);
        }
    }

    [Theory]
    [MemberData(nameof(AllExceptApple))]
    public void NonCollapsedSelection_WordModifierArrowMovement(TargetPlatform platform)
    {
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        (TextSelection Initial, bool Forward, int Expected)[] cases =
        [
            (new TextSelection(24, 43), false, 39),
            (new TextSelection(43, 24), false, 20),
            (new TextSelection(24, 43), true, 46),
            (new TextSelection(43, 24), true, 28),
        ];
        foreach ((TextSelection initial, bool forward, int expected) in cases)
        {
            _controller.Selection = initial;
            tester.Pump();
            Send(forward ? LogicalKeyboardKey.ArrowRight : LogicalKeyboardKey.ArrowLeft, control: true);
            Assert.Equal(TextSelection.Collapsed(expected), _controller.Selection);
        }
    }

    [Theory]
    [MemberData(nameof(AllExceptApple))]
    public void NonCollapsedSelection_LineModifierArrowMovement(TargetPlatform platform)
    {
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());
        (TextSelection Initial, bool Forward, TextSelection Expected)[] cases =
        [
            (new TextSelection(24, 43), false, TextSelection.Collapsed(36)),
            (new TextSelection(43, 24), false, TextSelection.Collapsed(20)),
            (new TextSelection(24, 43), true, TextSelection.Collapsed(54, TextAffinity.Upstream)),
            (new TextSelection(43, 24), true, TextSelection.Collapsed(35, TextAffinity.Upstream)),
        ];
        foreach ((TextSelection initial, bool forward, TextSelection expected) in cases)
        {
            _controller.Selection = initial;
            tester.Pump();
            Send(forward ? LogicalKeyboardKey.ArrowRight : LogicalKeyboardKey.ArrowLeft, alt: true);
            Assert.Equal(expected, _controller.Selection);
        }
    }

    // --------------------------------------------------------- vertical movement

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void VerticalMovement_Run(TargetPlatform platform)
    {
        SetValue("aa\n" + "a\n" + "aa\n" + "aaa\n" + "aaaa", TextSelection.Collapsed(2));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());

        TextSelection[] down =
        [
            TextSelection.Collapsed(4, TextAffinity.Upstream),
            TextSelection.Collapsed(7, TextAffinity.Upstream),
            TextSelection.Collapsed(10),
            TextSelection.Collapsed(14),
            TextSelection.Collapsed(16),
        ];
        foreach (TextSelection expected in down)
        {
            Send(LogicalKeyboardKey.ArrowDown);
            tester.Pump();
            Assert.Equal(expected, _controller.Selection);
        }

        TextSelection[] up =
        [
            TextSelection.Collapsed(10),
            TextSelection.Collapsed(7, TextAffinity.Upstream),
            TextSelection.Collapsed(4, TextAffinity.Upstream),
            TextSelection.Collapsed(2, TextAffinity.Upstream),
            TextSelection.Collapsed(0),
        ];
        foreach (TextSelection expected in up)
        {
            Send(LogicalKeyboardKey.ArrowUp);
            tester.Pump();
            Assert.Equal(expected, _controller.Selection);
        }

        Send(LogicalKeyboardKey.ArrowDown);
        tester.Pump();
        Assert.Equal(TextSelection.Collapsed(4, TextAffinity.Upstream), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllExceptApple))]
    public void VerticalMovement_RunWithPageDownAndUp(TargetPlatform platform)
    {
        string text = "aa\n" + "a\n" + "aa\n" + "aaa\n" + string.Concat(Enumerable.Repeat("aaa\n", 50)) + "aaaa";
        SetValue(text, TextSelection.Collapsed(2));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());

        Send(LogicalKeyboardKey.ArrowDown);
        tester.Pump();
        Assert.Equal(TextSelection.Collapsed(4, TextAffinity.Upstream), _controller.Selection);

        Send(LogicalKeyboardKey.PageDown);
        tester.Pump();
        Assert.Equal(TextSelection.Collapsed(82), _controller.Selection);

        Send(LogicalKeyboardKey.ArrowUp);
        tester.Pump();
        Assert.Equal(TextSelection.Collapsed(78), _controller.Selection);

        Send(LogicalKeyboardKey.PageUp);
        tester.Pump();
        Assert.Equal(TextSelection.Collapsed(2, TextAffinity.Upstream), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void VerticalMovement_RunCanBeInterruptedByLayoutChanges(TargetPlatform platform)
    {
        SetValue("aa\n" + "a\n" + "aa\n" + "aaa\n" + "aaaa", TextSelection.Collapsed(2));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());

        Send(LogicalKeyboardKey.ArrowUp);
        tester.Pump();
        Assert.Equal(TextSelection.Collapsed(0), _controller.Selection);

        // Layout changes.
        tester.PumpWidget(BuildEditableText(textAlign: TextAlign.Right));

        // Moves to the second line.
        Send(LogicalKeyboardKey.ArrowDown);
        tester.Pump();
        Assert.Equal(TextSelection.Collapsed(3), _controller.Selection);
    }

    [Theory]
    [MemberData(nameof(AllPlatforms))]
    public void VerticalMovement_RunCanBeInterruptedBySelectionChanges(TargetPlatform platform)
    {
        SetValue("aa\n" + "a\n" + "aa\n" + "aaa\n" + "aaaa", TextSelection.Collapsed(2));
        using FrameworkDartTester tester = Pump(platform, BuildEditableText());

        Send(LogicalKeyboardKey.ArrowUp);
        tester.Pump();
        Assert.Equal(TextSelection.Collapsed(0), _controller.Selection);

        _controller.Selection = TextSelection.Collapsed(1);
        tester.Pump();
        _controller.Selection = TextSelection.Collapsed(0);
        tester.Pump();

        Send(LogicalKeyboardKey.ArrowDown);
        tester.Pump();
        // Would have been 4 if the run wasn't interrupted.
        Assert.Equal(TextSelection.Collapsed(3), _controller.Selection);
    }

    // ------------------------------------------------------------ macOS shortcuts

    [Fact]
    public void MacOS_WordAndLineModifiersGoThroughSelectors()
    {
        using FrameworkDartTester tester = Pump(TargetPlatform.MacOS, BuildEditableText());

        _controller.Selection = TextSelection.Collapsed(7); // Before "the"
        tester.Pump();
        Send(LogicalKeyboardKey.ArrowLeft, alt: true);
        Assert.Equal(TextSelection.Collapsed(4), _controller.Selection);

        _controller.Selection = TextSelection.Collapsed(7);
        tester.Pump();
        Send(LogicalKeyboardKey.ArrowRight, alt: true);
        Assert.Equal(TextSelection.Collapsed(10), _controller.Selection);

        _controller.Selection = TextSelection.Collapsed(24); // Before "good".
        tester.Pump();
        Send(LogicalKeyboardKey.ArrowLeft, meta: true);
        Assert.Equal(TextSelection.Collapsed(20), _controller.Selection);

        _controller.Selection = TextSelection.Collapsed(24);
        tester.Pump();
        Send(LogicalKeyboardKey.ArrowRight, meta: true);
        Assert.Equal(TextSelection.Collapsed(35, TextAffinity.Upstream), _controller.Selection);
    }

    [Fact]
    public void MacOS_AltArrowsWithNonCollapsedSelectionMatchTheControlVariants()
    {
        using FrameworkDartTester tester = Pump(TargetPlatform.MacOS, BuildEditableText());
        (TextSelection Initial, bool Forward, int Expected)[] cases =
        [
            (new TextSelection(24, 43), false, 39),
            (new TextSelection(43, 24), false, 20),
            (new TextSelection(24, 43), true, 46),
            (new TextSelection(43, 24), true, 28),
        ];
        foreach ((TextSelection initial, bool forward, int expected) in cases)
        {
            _controller.Selection = initial;
            tester.Pump();
            Send(forward ? LogicalKeyboardKey.ArrowRight : LogicalKeyboardKey.ArrowLeft, alt: true);
            Assert.Equal(TextSelection.Collapsed(expected), _controller.Selection);
        }
    }
}
