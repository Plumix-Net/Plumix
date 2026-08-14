using Plumix;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/default_text_editing_shortcuts.dart
// Mirrors flutter/packages/flutter/test/widgets/default_text_editing_shortcuts_test.dart.

namespace Plumix.Tests;

public sealed class DefaultTextEditingShortcutsTests : IDisposable
{
    private readonly TargetPlatform? _previousPlatform = PlatformDefaults.DebugTargetPlatformOverride;
    private readonly bool? _previousWeb = PlatformDefaults.DebugIsWebOverride;

    public void Dispose()
    {
        PlatformDefaults.DebugTargetPlatformOverride = _previousPlatform;
        PlatformDefaults.DebugIsWebOverride = _previousWeb;
    }

    [Theory]
    [InlineData(TargetPlatform.Android, true)]
    [InlineData(TargetPlatform.Fuchsia, true)]
    [InlineData(TargetPlatform.Windows, true)]
    [InlineData(TargetPlatform.Linux, false)]
    public void HomeAndEndSelectToTheLineBreakOnNonAppleePlatforms(TargetPlatform platform, bool continuesAtWrap)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;

        var home = Assert.IsType<ExtendSelectionToLineBreakIntent>(Resolve(Key("Home")));
        Assert.False(home.Forward);
        Assert.True(home.CollapseSelection);
        Assert.Equal(continuesAtWrap, home.ContinuesAtWrap);

        var end = Assert.IsType<ExtendSelectionToLineBreakIntent>(Resolve(Key("End")));
        Assert.True(end.Forward);
        Assert.True(end.CollapseSelection);
        Assert.Equal(continuesAtWrap, end.ContinuesAtWrap);

        var shiftHome = Assert.IsType<ExtendSelectionToLineBreakIntent>(Resolve(Key("Home", shift: true)));
        Assert.False(shiftHome.CollapseSelection);
        Assert.Equal(continuesAtWrap, shiftHome.ContinuesAtWrap);
    }

    [Theory]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.MacOS)]
    public void HomeAndEndScrollTheDocumentOnApplePlatforms(TargetPlatform platform)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;

        var home = Assert.IsType<ScrollToDocumentBoundaryIntent>(Resolve(Key("Home")));
        Assert.False(home.Forward);
        var end = Assert.IsType<ScrollToDocumentBoundaryIntent>(Resolve(Key("End")));
        Assert.True(end.Forward);

        var shiftHome = Assert.IsType<ExpandSelectionToDocumentBoundaryIntent>(Resolve(Key("Home", shift: true)));
        Assert.False(shiftHome.Forward);
        var shiftEnd = Assert.IsType<ExpandSelectionToDocumentBoundaryIntent>(Resolve(Key("End", shift: true)));
        Assert.True(shiftEnd.Forward);
    }

    [Theory]
    [InlineData(TargetPlatform.Android)]
    [InlineData(TargetPlatform.Linux)]
    [InlineData(TargetPlatform.Windows)]
    public void ControlHomeAndEndSelectToTheDocumentBoundary(TargetPlatform platform)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;

        var home = Assert.IsType<ExtendSelectionToDocumentBoundaryIntent>(Resolve(Key("Home", control: true)));
        Assert.False(home.Forward);
        Assert.True(home.CollapseSelection);

        var shiftEnd = Assert.IsType<ExtendSelectionToDocumentBoundaryIntent>(
            Resolve(Key("End", control: true, shift: true)));
        Assert.True(shiftEnd.Forward);
        Assert.False(shiftEnd.CollapseSelection);
    }

    [Theory]
    [InlineData(TargetPlatform.Android)]
    [InlineData(TargetPlatform.Linux)]
    [InlineData(TargetPlatform.Windows)]
    public void ClipboardShortcutsFollowTheXeroxAndCuaTables(TargetPlatform platform)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;

        var cut = Assert.IsType<CopySelectionTextIntent>(Resolve(Key("X", control: true)));
        Assert.True(cut.CollapseSelection);
        var copy = Assert.IsType<CopySelectionTextIntent>(Resolve(Key("C", control: true)));
        Assert.False(copy.CollapseSelection);
        Assert.IsType<PasteTextIntent>(Resolve(Key("V", control: true)));

        var cuaCut = Assert.IsType<CopySelectionTextIntent>(Resolve(Key("Delete", shift: true)));
        Assert.True(cuaCut.CollapseSelection);
        var cuaCopy = Assert.IsType<CopySelectionTextIntent>(Resolve(Key("Insert", control: true)));
        Assert.False(cuaCopy.CollapseSelection);
        Assert.IsType<PasteTextIntent>(Resolve(Key("Insert", shift: true)));

        Assert.IsType<SelectAllTextIntent>(Resolve(Key("A", control: true)));
        Assert.IsType<UndoTextIntent>(Resolve(Key("Z", control: true)));
        Assert.IsType<RedoTextIntent>(Resolve(Key("Z", control: true, shift: true)));
    }

    [Fact]
    public void AppleClipboardShortcutsUseTheMetaModifier()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.MacOS;

        Assert.IsType<SelectAllTextIntent>(Resolve(Key("A", meta: true)));
        var copy = Assert.IsType<CopySelectionTextIntent>(Resolve(Key("C", meta: true)));
        Assert.False(copy.CollapseSelection);
        Assert.IsType<PasteTextIntent>(Resolve(Key("V", meta: true)));
        Assert.IsType<TransposeCharactersIntent>(Resolve(Key("T", control: true)));

        // Control+A/E are the emacs-style line movements on Apple platforms, not select-all.
        var lineStart = Assert.IsType<ExtendSelectionToLineBreakIntent>(Resolve(Key("A", control: true)));
        Assert.False(lineStart.Forward);
        var lineEnd = Assert.IsType<ExtendSelectionToLineBreakIntent>(Resolve(Key("E", control: true)));
        Assert.True(lineEnd.Forward);
    }

    [Fact]
    public void AppleWordAndLineMovementUseAltAndMeta()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.MacOS;

        var word = Assert.IsType<ExtendSelectionToNextWordBoundaryIntent>(Resolve(Key("ArrowLeft", alt: true)));
        Assert.False(word.Forward);
        Assert.True(word.CollapseSelection);

        var line = Assert.IsType<ExtendSelectionToLineBreakIntent>(Resolve(Key("ArrowRight", meta: true)));
        Assert.True(line.Forward);
        Assert.True(line.CollapseSelection);

        var wordSelect = Assert.IsType<ExtendSelectionToNextWordBoundaryOrCaretLocationIntent>(
            Resolve(Key("ArrowLeft", shift: true, alt: true)));
        Assert.False(wordSelect.Forward);
        Assert.True(wordSelect.CollapseAtReversal);

        Assert.IsType<ExpandSelectionToLineBreakIntent>(Resolve(Key("ArrowLeft", shift: true, meta: true)));
        Assert.IsType<ExpandSelectionToDocumentBoundaryIntent>(Resolve(Key("ArrowUp", shift: true, meta: true)));
    }

    [Fact]
    public void NonAppleWordMovementUsesControlAndDocumentMovementUsesAlt()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Windows;

        var word = Assert.IsType<ExtendSelectionToNextWordBoundaryIntent>(
            Resolve(Key("ArrowRight", control: true)));
        Assert.True(word.Forward);
        Assert.True(word.CollapseSelection);

        var document = Assert.IsType<ExtendSelectionToDocumentBoundaryIntent>(
            Resolve(Key("ArrowUp", alt: true)));
        Assert.False(document.Forward);

        var paragraph = Assert.IsType<ExtendSelectionToNextParagraphBoundaryIntent>(
            Resolve(Key("ArrowDown", control: true, shift: true)));
        Assert.True(paragraph.Forward);
        Assert.False(paragraph.CollapseSelection);
    }

    [Fact]
    public void MacOsPageKeysScrollWhileShiftPageKeysExtendTheSelection()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.MacOS;

        var scrollUp = Assert.IsType<ScrollIntent>(Resolve(Key("PageUp")));
        Assert.Equal(AxisDirection.Up, scrollUp.Direction);
        Assert.Equal(ScrollIncrementType.Page, scrollUp.Type);

        var extend = Assert.IsType<ExtendSelectionVerticallyToAdjacentPageIntent>(
            Resolve(Key("PageDown", shift: true)));
        Assert.True(extend.Forward);
        Assert.False(extend.CollapseSelection);
    }

    [Fact]
    public void LinuxNumpadShortcutsDependOnTheNumLockState()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Linux;

        var locked = Assert.IsType<ExtendSelectionByCharacterIntent>(
            Resolve(Key("Numpad4", shift: true, numLockOn: true)));
        Assert.False(locked.Forward);
        Assert.False(locked.CollapseSelection);

        var unlocked = Assert.IsType<ExtendSelectionByCharacterIntent>(
            Resolve(Key("Numpad4", numLockOn: false)));
        Assert.False(unlocked.Forward);
        Assert.True(unlocked.CollapseSelection);

        var lockedWord = Assert.IsType<ExtendSelectionToNextWordBoundaryIntent>(
            Resolve(Key("Numpad6", control: true, shift: true, numLockOn: true)));
        Assert.True(lockedWord.Forward);

        Assert.IsType<DeleteCharacterIntent>(Resolve(Key("NumpadDecimal", numLockOn: false)));
    }

    [Fact]
    public void DeleteAndBackspaceMapToTheDeletionIntents()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;

        var backspace = Assert.IsType<DeleteCharacterIntent>(Resolve(Key("Backspace")));
        Assert.False(backspace.Forward);
        var delete = Assert.IsType<DeleteCharacterIntent>(Resolve(Key("Delete")));
        Assert.True(delete.Forward);
        Assert.IsType<DeleteToNextWordBoundaryIntent>(Resolve(Key("Backspace", control: true)));
        Assert.IsType<DeleteToLineBreakIntent>(Resolve(Key("Backspace", alt: true)));

        // The shift variants map to the same intents so a held shift does not swallow the key.
        Assert.IsType<DeleteCharacterIntent>(Resolve(Key("Backspace", shift: true)));
    }

    [Fact]
    public void IOsHandsPlainDeletionsBackToTheImeButKeepsLineDeletion()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.IOS;
        IReadOnlyDictionary<ShortcutActivator, Intent>? disabling =
            DefaultTextEditingShortcuts.GetDisablingShortcut();

        Assert.NotNull(disabling);
        Assert.IsType<DoNothingAndStopPropagationTextIntent>(Resolve(Key("Backspace"), disabling!));
        Assert.IsType<DoNothingAndStopPropagationTextIntent>(Resolve(Key("Delete", alt: true), disabling!));
        // Meta-modified deletion depends on text layout, so it stays with the framework.
        Assert.Null(Resolve(Key("Backspace", meta: true), disabling!));
        Assert.IsType<DeleteToLineBreakIntent>(Resolve(Key("Backspace", meta: true)));
    }

    [Fact]
    public void OnlyApplePlatformsAndTheWebDisableShortcuts()
    {
        PlatformDefaults.DebugIsWebOverride = false;
        foreach (TargetPlatform platform in (TargetPlatform[])
                 [TargetPlatform.Android, TargetPlatform.Fuchsia, TargetPlatform.Linux, TargetPlatform.Windows])
        {
            PlatformDefaults.DebugTargetPlatformOverride = platform;
            Assert.Null(DefaultTextEditingShortcuts.GetDisablingShortcut());
        }

        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.MacOS;
        Assert.NotNull(DefaultTextEditingShortcuts.GetDisablingShortcut());

        PlatformDefaults.DebugIsWebOverride = true;
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        IReadOnlyDictionary<ShortcutActivator, Intent>? web = DefaultTextEditingShortcuts.GetDisablingShortcut();
        Assert.NotNull(web);
        Assert.IsType<DoNothingAndStopPropagationTextIntent>(Resolve(Key("ArrowLeft"), web!));
        Assert.IsType<DoNothingAndStopPropagationTextIntent>(Resolve(Key("C", control: true), web!));

        // Web + Linux additionally hands every numpad activator back to the browser.
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Linux;
        IReadOnlyDictionary<ShortcutActivator, Intent> webLinux =
            DefaultTextEditingShortcuts.GetDisablingShortcut()!;
        Assert.IsType<DoNothingAndStopPropagationTextIntent>(
            Resolve(Key("Numpad4", numLockOn: false), webLinux));
    }

    [Fact]
    public void SpaceAndEnterAreHandedToTheImeOnEveryPlatform()
    {
        foreach (TargetPlatform platform in Enum.GetValues<TargetPlatform>())
        {
            PlatformDefaults.DebugTargetPlatformOverride = platform;
            Assert.IsType<DoNothingAndStopPropagationTextIntent>(Resolve(Key("Space")));
            Assert.IsType<DoNothingAndStopPropagationTextIntent>(Resolve(Key("Enter")));
        }
    }

    [Fact]
    public void MacOsSelectorsMapToTheirIntents()
    {
        Assert.IsType<DeleteCharacterIntent>(MacOsSelectors.IntentForMacOsSelector("deleteBackward:"));
        Assert.IsType<ExtendSelectionByCharacterIntent>(MacOsSelectors.IntentForMacOsSelector("moveLeft:"));
        Assert.IsType<ExpandSelectionToLineBreakIntent>(
            MacOsSelectors.IntentForMacOsSelector("moveToLeftEndOfLineAndModifySelection:"));
        var scroll = Assert.IsType<ScrollIntent>(MacOsSelectors.IntentForMacOsSelector("scrollPageDown:"));
        Assert.Equal(AxisDirection.Down, scroll.Direction);
        Assert.IsType<DismissIntent>(MacOsSelectors.IntentForMacOsSelector("cancelOperation:"));
        Assert.IsType<NextFocusIntent>(MacOsSelectors.IntentForMacOsSelector("insertTab:"));
        Assert.Null(MacOsSelectors.IntentForMacOsSelector("notASelector:"));
    }

    // -- Helpers ---------------------------------------------------------------------------------

    private static KeyEvent Key(
        string trigger,
        bool control = false,
        bool shift = false,
        bool alt = false,
        bool meta = false,
        bool numLockOn = false)
    {
        return new KeyEvent(
            trigger,
            isDown: true,
            isShiftPressed: shift,
            isControlPressed: control,
            isAltPressed: alt,
            isMetaPressed: meta,
            isNumLockOn: numLockOn);
    }

    /// Mirrors `ShortcutManager`'s first-accepting-activator rule against a given map.
    private static Intent? Resolve(KeyEvent @event, IReadOnlyDictionary<ShortcutActivator, Intent>? map = null)
    {
        foreach ((ShortcutActivator activator, Intent intent) in
                 map ?? DefaultTextEditingShortcuts.PlatformShortcuts)
        {
            if (activator.Accepts(@event, HardwareKeyboard.Instance))
            {
                return intent;
            }
        }

        return null;
    }
}
