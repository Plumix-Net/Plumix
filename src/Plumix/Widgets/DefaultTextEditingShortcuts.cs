using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/default_text_editing_shortcuts.dart

namespace Plumix.Widgets;

/// <summary>
/// A widget with the shortcuts used for the default text editing behavior.
/// </summary>
/// <remarks>
/// Flutter identifies triggers by `LogicalKeyboardKey`; Plumix models logical keys as normalized key
/// strings, so every activator below spells the source key name and
/// <see cref="LogicalKeySet.NormalizeKey"/> folds it onto the host's spelling.
/// </remarks>
public sealed class DefaultTextEditingShortcuts : StatelessWidget
{
    private static readonly DoNothingAndStopPropagationTextIntent DoNothing = new();

    private static IReadOnlyDictionary<ShortcutActivator, Intent>? _commonShortcutsCache;
    private static IReadOnlyDictionary<ShortcutActivator, Intent>? _clipboardShortcutsCache;
    private static IReadOnlyDictionary<ShortcutActivator, Intent>? _androidShortcutsCache;
    private static IReadOnlyDictionary<ShortcutActivator, Intent>? _linuxNumpadShortcutsCache;
    private static IReadOnlyDictionary<ShortcutActivator, Intent>? _linuxShortcutsCache;
    private static IReadOnlyDictionary<ShortcutActivator, Intent>? _macShortcutsCache;
    private static IReadOnlyDictionary<ShortcutActivator, Intent>? _windowsShortcutsCache;
    private static IReadOnlyDictionary<ShortcutActivator, Intent>? _commonDisablingCache;
    private static IReadOnlyDictionary<ShortcutActivator, Intent>? _iOSDisablingCache;
    private static IReadOnlyDictionary<ShortcutActivator, Intent>? _macDisablingCache;
    private static IReadOnlyDictionary<ShortcutActivator, Intent>? _webDisablingCache;
    private static IReadOnlyDictionary<ShortcutActivator, Intent>? _webLinuxDisablingCache;

    public DefaultTextEditingShortcuts(Widget child, Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        Widget result = Child;
        if (GetDisablingShortcut() is { } disablingShortcut)
        {
            result = new Shortcuts(
                shortcuts: disablingShortcut,
                debugLabel: "<Web Disabling Text Editing Shortcuts>",
                child: result);
        }

        return new Shortcuts(
            shortcuts: PlatformShortcuts,
            debugLabel: "<Default Text Editing Shortcuts>",
            child: result);
    }

    /// <summary>The platform's text-editing shortcut map.</summary>
    public static IReadOnlyDictionary<ShortcutActivator, Intent> PlatformShortcuts => PlatformDefaults
        .TargetPlatform switch
    {
        TargetPlatform.Android => AndroidShortcuts,
        TargetPlatform.Fuchsia => AndroidShortcuts,
        TargetPlatform.IOS => MacShortcuts,
        TargetPlatform.Linux => LinuxShortcuts,
        TargetPlatform.MacOS => MacShortcuts,
        _ => WindowsShortcuts,
    };

    /// <summary>
    /// The map that hands key combinations back to the platform, or null when the platform handles
    /// every text-editing shortcut itself.
    /// </summary>
    public static IReadOnlyDictionary<ShortcutActivator, Intent>? GetDisablingShortcut()
    {
        if (PlatformDefaults.IsWeb)
        {
            return PlatformDefaults.TargetPlatform == TargetPlatform.Linux
                ? WebLinuxDisablingTextShortcuts
                : WebDisablingTextShortcuts;
        }

        return PlatformDefaults.TargetPlatform switch
        {
            TargetPlatform.IOS => IOSDisablingTextShortcuts,
            TargetPlatform.MacOS => MacDisablingTextShortcuts,
            _ => null,
        };
    }

    // -- Maps -----------------------------------------------------------------------------------

    private static IReadOnlyDictionary<ShortcutActivator, Intent> CommonShortcuts =>
        _commonShortcutsCache ??= BuildCommonShortcuts();

    private static IReadOnlyDictionary<ShortcutActivator, Intent> ClipboardShortcuts =>
        _clipboardShortcutsCache ??= BuildClipboardShortcuts();

    private static IReadOnlyDictionary<ShortcutActivator, Intent> AndroidShortcuts =>
        _androidShortcutsCache ??= BuildAndroidShortcuts();

    private static IReadOnlyDictionary<ShortcutActivator, Intent> LinuxNumpadShortcuts =>
        _linuxNumpadShortcutsCache ??= BuildLinuxNumpadShortcuts();

    private static IReadOnlyDictionary<ShortcutActivator, Intent> LinuxShortcuts =>
        _linuxShortcutsCache ??= BuildLinuxShortcuts();

    private static IReadOnlyDictionary<ShortcutActivator, Intent> MacShortcuts =>
        _macShortcutsCache ??= BuildMacShortcuts();

    private static IReadOnlyDictionary<ShortcutActivator, Intent> WindowsShortcuts =>
        _windowsShortcutsCache ??= BuildWindowsShortcuts();

    private static IReadOnlyDictionary<ShortcutActivator, Intent> CommonDisablingTextShortcuts =>
        _commonDisablingCache ??= BuildCommonDisablingTextShortcuts();

    private static IReadOnlyDictionary<ShortcutActivator, Intent> IOSDisablingTextShortcuts =>
        _iOSDisablingCache ??= BuildIOSDisablingTextShortcuts();

    private static IReadOnlyDictionary<ShortcutActivator, Intent> MacDisablingTextShortcuts =>
        _macDisablingCache ??= BuildMacDisablingTextShortcuts();

    private static IReadOnlyDictionary<ShortcutActivator, Intent> WebDisablingTextShortcuts =>
        _webDisablingCache ??= BuildWebDisablingTextShortcuts();

    private static IReadOnlyDictionary<ShortcutActivator, Intent> WebLinuxDisablingTextShortcuts =>
        _webLinuxDisablingCache ??= BuildWebLinuxDisablingTextShortcuts();

    private static SingleActivator Act(
        string trigger,
        bool control = false,
        bool shift = false,
        bool alt = false,
        bool meta = false,
        LockState numLock = LockState.Ignored)
    {
        return new SingleActivator(trigger, control: control, shift: shift, alt: alt, meta: meta, numLock: numLock);
    }

    private static Dictionary<ShortcutActivator, Intent> BuildCommonShortcuts()
    {
        var map = new Dictionary<ShortcutActivator, Intent>();
        foreach (bool pressShift in (bool[])[true, false])
        {
            map[Act("Backspace", shift: pressShift)] = new DeleteCharacterIntent(forward: false);
            map[Act("Backspace", control: true, shift: pressShift)] =
                new DeleteToNextWordBoundaryIntent(forward: false);
            map[Act("Backspace", alt: true, shift: pressShift)] = new DeleteToLineBreakIntent(forward: false);
            map[Act("Delete", control: true, shift: pressShift)] = new DeleteToNextWordBoundaryIntent(forward: true);
            map[Act("Delete", alt: true, shift: pressShift)] = new DeleteToLineBreakIntent(forward: true);
        }

        map[Act("Delete")] = new DeleteCharacterIntent(forward: true);
        map[Act("ArrowLeft")] = new ExtendSelectionByCharacterIntent(forward: false, collapseSelection: true);
        map[Act("ArrowRight")] = new ExtendSelectionByCharacterIntent(forward: true, collapseSelection: true);
        map[Act("ArrowUp")] =
            new ExtendSelectionVerticallyToAdjacentLineIntent(forward: false, collapseSelection: true);
        map[Act("ArrowDown")] =
            new ExtendSelectionVerticallyToAdjacentLineIntent(forward: true, collapseSelection: true);
        map[Act("ArrowLeft", shift: true)] =
            new ExtendSelectionByCharacterIntent(forward: false, collapseSelection: false);
        map[Act("ArrowRight", shift: true)] =
            new ExtendSelectionByCharacterIntent(forward: true, collapseSelection: false);
        map[Act("ArrowUp", shift: true)] =
            new ExtendSelectionVerticallyToAdjacentLineIntent(forward: false, collapseSelection: false);
        map[Act("ArrowDown", shift: true)] =
            new ExtendSelectionVerticallyToAdjacentLineIntent(forward: true, collapseSelection: false);
        map[Act("ArrowLeft", alt: true)] =
            new ExtendSelectionToLineBreakIntent(forward: false, collapseSelection: true);
        map[Act("ArrowRight", alt: true)] =
            new ExtendSelectionToLineBreakIntent(forward: true, collapseSelection: true);
        map[Act("ArrowUp", alt: true)] =
            new ExtendSelectionToDocumentBoundaryIntent(forward: false, collapseSelection: true);
        map[Act("ArrowDown", alt: true)] =
            new ExtendSelectionToDocumentBoundaryIntent(forward: true, collapseSelection: true);
        map[Act("ArrowLeft", shift: true, alt: true)] =
            new ExtendSelectionToLineBreakIntent(forward: false, collapseSelection: false);
        map[Act("ArrowRight", shift: true, alt: true)] =
            new ExtendSelectionToLineBreakIntent(forward: true, collapseSelection: false);
        map[Act("ArrowUp", shift: true, alt: true)] =
            new ExtendSelectionToDocumentBoundaryIntent(forward: false, collapseSelection: false);
        map[Act("ArrowDown", shift: true, alt: true)] =
            new ExtendSelectionToDocumentBoundaryIntent(forward: true, collapseSelection: false);
        map[Act("ArrowLeft", control: true)] =
            new ExtendSelectionToNextWordBoundaryIntent(forward: false, collapseSelection: true);
        map[Act("ArrowRight", control: true)] =
            new ExtendSelectionToNextWordBoundaryIntent(forward: true, collapseSelection: true);
        map[Act("ArrowLeft", control: true, shift: true)] =
            new ExtendSelectionToNextWordBoundaryIntent(forward: false, collapseSelection: false);
        map[Act("ArrowRight", control: true, shift: true)] =
            new ExtendSelectionToNextWordBoundaryIntent(forward: true, collapseSelection: false);
        map[Act("ArrowUp", control: true, shift: true)] =
            new ExtendSelectionToNextParagraphBoundaryIntent(forward: false, collapseSelection: false);
        map[Act("ArrowDown", control: true, shift: true)] =
            new ExtendSelectionToNextParagraphBoundaryIntent(forward: true, collapseSelection: false);
        map[Act("PageUp")] =
            new ExtendSelectionVerticallyToAdjacentPageIntent(forward: false, collapseSelection: true);
        map[Act("PageDown")] =
            new ExtendSelectionVerticallyToAdjacentPageIntent(forward: true, collapseSelection: true);
        map[Act("PageUp", shift: true)] =
            new ExtendSelectionVerticallyToAdjacentPageIntent(forward: false, collapseSelection: false);
        map[Act("PageDown", shift: true)] =
            new ExtendSelectionVerticallyToAdjacentPageIntent(forward: true, collapseSelection: false);
        return map;
    }

    private static Dictionary<ShortcutActivator, Intent> BuildClipboardShortcuts()
    {
        return new Dictionary<ShortcutActivator, Intent>
        {
            // Cut, Copy, Paste as introduced by Xerox and popularized by Apple.
            [Act("X", control: true)] = CopySelectionTextIntent.Cut(SelectionChangedCause.Keyboard),
            [Act("C", control: true)] = CopySelectionTextIntent.Copy,
            [Act("V", control: true)] = new PasteTextIntent(SelectionChangedCause.Keyboard),
            // Cut, Copy, Paste as defined by IBM's Common User Access.
            [Act("Delete", shift: true)] = CopySelectionTextIntent.Cut(SelectionChangedCause.Keyboard),
            [Act("Insert", control: true)] = CopySelectionTextIntent.Copy,
            [Act("Insert", shift: true)] = new PasteTextIntent(SelectionChangedCause.Keyboard),
            [Act("A", control: true)] = new SelectAllTextIntent(SelectionChangedCause.Keyboard),
            [Act("Z", control: true)] = new UndoTextIntent(SelectionChangedCause.Keyboard),
            [Act("Z", control: true, shift: true)] = new RedoTextIntent(SelectionChangedCause.Keyboard),
            // Give the IME the space and enter keys while a field is focused.
            [Act("Space")] = DoNothing,
            [Act("Enter")] = DoNothing,
        };
    }

    private static Dictionary<ShortcutActivator, Intent> BuildAndroidShortcuts()
    {
        var map = new Dictionary<ShortcutActivator, Intent>(CommonShortcuts);
        foreach ((ShortcutActivator activator, Intent intent) in ClipboardShortcuts)
        {
            map[activator] = intent;
        }

        map[Act("Home")] = new ExtendSelectionToLineBreakIntent(
            forward: false, collapseSelection: true, continuesAtWrap: true);
        map[Act("End")] = new ExtendSelectionToLineBreakIntent(
            forward: true, collapseSelection: true, continuesAtWrap: true);
        map[Act("Home", shift: true)] = new ExtendSelectionToLineBreakIntent(
            forward: false, collapseSelection: false, continuesAtWrap: true);
        map[Act("End", shift: true)] = new ExtendSelectionToLineBreakIntent(
            forward: true, collapseSelection: false, continuesAtWrap: true);
        AddDocumentBoundaryHomeEnd(map);
        return map;
    }

    private static Dictionary<ShortcutActivator, Intent> BuildLinuxNumpadShortcuts()
    {
        return new Dictionary<ShortcutActivator, Intent>
        {
            // When numLock is on, the numpad shortcuts require shift to be pressed too.
            [Act("Numpad6", shift: true, numLock: LockState.Locked)] =
                new ExtendSelectionByCharacterIntent(forward: true, collapseSelection: false),
            [Act("Numpad4", shift: true, numLock: LockState.Locked)] =
                new ExtendSelectionByCharacterIntent(forward: false, collapseSelection: false),
            [Act("Numpad8", shift: true, numLock: LockState.Locked)] =
                new ExtendSelectionVerticallyToAdjacentLineIntent(forward: false, collapseSelection: false),
            [Act("Numpad2", shift: true, numLock: LockState.Locked)] =
                new ExtendSelectionVerticallyToAdjacentLineIntent(forward: true, collapseSelection: false),
            [Act("Numpad6", control: true, shift: true, numLock: LockState.Locked)] =
                new ExtendSelectionToNextWordBoundaryIntent(forward: true, collapseSelection: false),
            [Act("Numpad4", control: true, shift: true, numLock: LockState.Locked)] =
                new ExtendSelectionToNextWordBoundaryIntent(forward: false, collapseSelection: false),
            [Act("Numpad8", control: true, shift: true, numLock: LockState.Locked)] =
                new ExtendSelectionToNextParagraphBoundaryIntent(forward: false, collapseSelection: false),
            [Act("Numpad2", control: true, shift: true, numLock: LockState.Locked)] =
                new ExtendSelectionToNextParagraphBoundaryIntent(forward: true, collapseSelection: false),
            [Act("Numpad9", shift: true, numLock: LockState.Locked)] =
                new ExtendSelectionVerticallyToAdjacentPageIntent(forward: false, collapseSelection: false),
            [Act("Numpad3", shift: true, numLock: LockState.Locked)] =
                new ExtendSelectionVerticallyToAdjacentPageIntent(forward: true, collapseSelection: false),
            [Act("Numpad7", shift: true, numLock: LockState.Locked)] =
                new ExtendSelectionVerticallyToAdjacentLineIntent(forward: false, collapseSelection: false),
            [Act("Numpad1", shift: true, numLock: LockState.Locked)] =
                new ExtendSelectionVerticallyToAdjacentLineIntent(forward: true, collapseSelection: false),
            [Act("NumpadDecimal", shift: true, numLock: LockState.Locked)] =
                new DeleteCharacterIntent(forward: true),
            [Act("NumpadDecimal", control: true, shift: true, numLock: LockState.Locked)] =
                new DeleteToNextWordBoundaryIntent(forward: true),
            // When numLock is off, the numpad shortcuts require shift not to be pressed.
            [Act("Numpad6", numLock: LockState.Unlocked)] =
                new ExtendSelectionByCharacterIntent(forward: true, collapseSelection: true),
            [Act("Numpad4", numLock: LockState.Unlocked)] =
                new ExtendSelectionByCharacterIntent(forward: false, collapseSelection: true),
            [Act("Numpad8", numLock: LockState.Unlocked)] =
                new ExtendSelectionVerticallyToAdjacentLineIntent(forward: false, collapseSelection: true),
            [Act("Numpad2", numLock: LockState.Unlocked)] =
                new ExtendSelectionVerticallyToAdjacentLineIntent(forward: true, collapseSelection: true),
            [Act("Numpad6", control: true, numLock: LockState.Unlocked)] =
                new ExtendSelectionToNextWordBoundaryIntent(forward: true, collapseSelection: true),
            [Act("Numpad4", control: true, numLock: LockState.Unlocked)] =
                new ExtendSelectionToNextWordBoundaryIntent(forward: false, collapseSelection: true),
            [Act("Numpad8", control: true, numLock: LockState.Unlocked)] =
                new ExtendSelectionToNextParagraphBoundaryIntent(forward: false, collapseSelection: true),
            [Act("Numpad2", control: true, numLock: LockState.Unlocked)] =
                new ExtendSelectionToNextParagraphBoundaryIntent(forward: true, collapseSelection: true),
            [Act("Numpad9", numLock: LockState.Unlocked)] =
                new ExtendSelectionVerticallyToAdjacentPageIntent(forward: false, collapseSelection: true),
            [Act("Numpad3", numLock: LockState.Unlocked)] =
                new ExtendSelectionVerticallyToAdjacentPageIntent(forward: true, collapseSelection: true),
            [Act("Numpad7", numLock: LockState.Unlocked)] =
                new ExtendSelectionVerticallyToAdjacentLineIntent(forward: false, collapseSelection: true),
            [Act("Numpad1", numLock: LockState.Unlocked)] =
                new ExtendSelectionVerticallyToAdjacentLineIntent(forward: true, collapseSelection: true),
            [Act("NumpadDecimal", numLock: LockState.Unlocked)] = new DeleteCharacterIntent(forward: true),
            [Act("NumpadDecimal", control: true, numLock: LockState.Unlocked)] =
                new DeleteToNextWordBoundaryIntent(forward: true),
        };
    }

    private static Dictionary<ShortcutActivator, Intent> BuildLinuxShortcuts()
    {
        var map = new Dictionary<ShortcutActivator, Intent>(CommonShortcuts);
        foreach ((ShortcutActivator activator, Intent intent) in ClipboardShortcuts)
        {
            map[activator] = intent;
        }

        foreach ((ShortcutActivator activator, Intent intent) in LinuxNumpadShortcuts)
        {
            map[activator] = intent;
        }

        map[Act("Home")] = new ExtendSelectionToLineBreakIntent(forward: false, collapseSelection: true);
        map[Act("End")] = new ExtendSelectionToLineBreakIntent(forward: true, collapseSelection: true);
        map[Act("Home", shift: true)] =
            new ExtendSelectionToLineBreakIntent(forward: false, collapseSelection: false);
        map[Act("End", shift: true)] =
            new ExtendSelectionToLineBreakIntent(forward: true, collapseSelection: false);
        AddDocumentBoundaryHomeEnd(map);
        return map;
    }

    private static Dictionary<ShortcutActivator, Intent> BuildWindowsShortcuts()
    {
        var map = new Dictionary<ShortcutActivator, Intent>(CommonShortcuts);
        foreach ((ShortcutActivator activator, Intent intent) in ClipboardShortcuts)
        {
            map[activator] = intent;
        }

        map[Act("PageUp")] =
            new ExtendSelectionVerticallyToAdjacentPageIntent(forward: false, collapseSelection: true);
        map[Act("PageDown")] =
            new ExtendSelectionVerticallyToAdjacentPageIntent(forward: true, collapseSelection: true);
        map[Act("Home")] = new ExtendSelectionToLineBreakIntent(
            forward: false, collapseSelection: true, continuesAtWrap: true);
        map[Act("End")] = new ExtendSelectionToLineBreakIntent(
            forward: true, collapseSelection: true, continuesAtWrap: true);
        map[Act("Home", shift: true)] = new ExtendSelectionToLineBreakIntent(
            forward: false, collapseSelection: false, continuesAtWrap: true);
        map[Act("End", shift: true)] = new ExtendSelectionToLineBreakIntent(
            forward: true, collapseSelection: false, continuesAtWrap: true);
        AddDocumentBoundaryHomeEnd(map);
        return map;
    }

    private static void AddDocumentBoundaryHomeEnd(Dictionary<ShortcutActivator, Intent> map)
    {
        map[Act("Home", control: true)] =
            new ExtendSelectionToDocumentBoundaryIntent(forward: false, collapseSelection: true);
        map[Act("End", control: true)] =
            new ExtendSelectionToDocumentBoundaryIntent(forward: true, collapseSelection: true);
        map[Act("Home", control: true, shift: true)] =
            new ExtendSelectionToDocumentBoundaryIntent(forward: false, collapseSelection: false);
        map[Act("End", control: true, shift: true)] =
            new ExtendSelectionToDocumentBoundaryIntent(forward: true, collapseSelection: false);
    }

    private static Dictionary<ShortcutActivator, Intent> BuildMacShortcuts()
    {
        var map = new Dictionary<ShortcutActivator, Intent>();
        foreach (bool pressShift in (bool[])[true, false])
        {
            map[Act("Backspace", shift: pressShift)] = new DeleteCharacterIntent(forward: false);
            map[Act("Backspace", alt: true, shift: pressShift)] =
                new DeleteToNextWordBoundaryIntent(forward: false);
            map[Act("Backspace", meta: true, shift: pressShift)] = new DeleteToLineBreakIntent(forward: false);
            map[Act("Delete", shift: pressShift)] = new DeleteCharacterIntent(forward: true);
            map[Act("Delete", alt: true, shift: pressShift)] = new DeleteToNextWordBoundaryIntent(forward: true);
            map[Act("Delete", meta: true, shift: pressShift)] = new DeleteToLineBreakIntent(forward: true);
        }

        map[Act("ArrowLeft")] = new ExtendSelectionByCharacterIntent(forward: false, collapseSelection: true);
        map[Act("ArrowRight")] = new ExtendSelectionByCharacterIntent(forward: true, collapseSelection: true);
        map[Act("ArrowUp")] =
            new ExtendSelectionVerticallyToAdjacentLineIntent(forward: false, collapseSelection: true);
        map[Act("ArrowDown")] =
            new ExtendSelectionVerticallyToAdjacentLineIntent(forward: true, collapseSelection: true);
        map[Act("ArrowLeft", shift: true)] =
            new ExtendSelectionByCharacterIntent(forward: false, collapseSelection: false);
        map[Act("ArrowRight", shift: true)] =
            new ExtendSelectionByCharacterIntent(forward: true, collapseSelection: false);
        map[Act("ArrowUp", shift: true)] =
            new ExtendSelectionVerticallyToAdjacentLineIntent(forward: false, collapseSelection: false);
        map[Act("ArrowDown", shift: true)] =
            new ExtendSelectionVerticallyToAdjacentLineIntent(forward: true, collapseSelection: false);
        map[Act("ArrowLeft", alt: true)] =
            new ExtendSelectionToNextWordBoundaryIntent(forward: false, collapseSelection: true);
        map[Act("ArrowRight", alt: true)] =
            new ExtendSelectionToNextWordBoundaryIntent(forward: true, collapseSelection: true);
        map[Act("ArrowUp", alt: true)] =
            new ExtendSelectionToLineBreakIntent(forward: false, collapseSelection: true);
        map[Act("ArrowDown", alt: true)] =
            new ExtendSelectionToLineBreakIntent(forward: true, collapseSelection: true);
        map[Act("ArrowLeft", shift: true, alt: true)] =
            new ExtendSelectionToNextWordBoundaryOrCaretLocationIntent(forward: false);
        map[Act("ArrowRight", shift: true, alt: true)] =
            new ExtendSelectionToNextWordBoundaryOrCaretLocationIntent(forward: true);
        map[Act("ArrowUp", shift: true, alt: true)] =
            new ExtendSelectionToNextParagraphBoundaryOrCaretLocationIntent(forward: false);
        map[Act("ArrowDown", shift: true, alt: true)] =
            new ExtendSelectionToNextParagraphBoundaryOrCaretLocationIntent(forward: true);
        map[Act("ArrowLeft", meta: true)] =
            new ExtendSelectionToLineBreakIntent(forward: false, collapseSelection: true);
        map[Act("ArrowRight", meta: true)] =
            new ExtendSelectionToLineBreakIntent(forward: true, collapseSelection: true);
        map[Act("ArrowUp", meta: true)] =
            new ExtendSelectionToDocumentBoundaryIntent(forward: false, collapseSelection: true);
        map[Act("ArrowDown", meta: true)] =
            new ExtendSelectionToDocumentBoundaryIntent(forward: true, collapseSelection: true);
        map[Act("ArrowLeft", shift: true, meta: true)] = new ExpandSelectionToLineBreakIntent(forward: false);
        map[Act("ArrowRight", shift: true, meta: true)] = new ExpandSelectionToLineBreakIntent(forward: true);
        map[Act("ArrowUp", shift: true, meta: true)] =
            new ExpandSelectionToDocumentBoundaryIntent(forward: false);
        map[Act("ArrowDown", shift: true, meta: true)] =
            new ExpandSelectionToDocumentBoundaryIntent(forward: true);
        map[Act("T", control: true)] = new TransposeCharactersIntent();
        map[Act("Home")] = new ScrollToDocumentBoundaryIntent(forward: false);
        map[Act("End")] = new ScrollToDocumentBoundaryIntent(forward: true);
        map[Act("Home", shift: true)] = new ExpandSelectionToDocumentBoundaryIntent(forward: false);
        map[Act("End", shift: true)] = new ExpandSelectionToDocumentBoundaryIntent(forward: true);
        map[Act("PageUp")] = new ScrollIntent(direction: AxisDirection.Up, type: ScrollIncrementType.Page);
        map[Act("PageDown")] = new ScrollIntent(direction: AxisDirection.Down, type: ScrollIncrementType.Page);
        map[Act("PageUp", shift: true)] =
            new ExtendSelectionVerticallyToAdjacentPageIntent(forward: false, collapseSelection: false);
        map[Act("PageDown", shift: true)] =
            new ExtendSelectionVerticallyToAdjacentPageIntent(forward: true, collapseSelection: false);
        map[Act("X", meta: true)] = CopySelectionTextIntent.Cut(SelectionChangedCause.Keyboard);
        map[Act("C", meta: true)] = CopySelectionTextIntent.Copy;
        map[Act("V", meta: true)] = new PasteTextIntent(SelectionChangedCause.Keyboard);
        map[Act("A", meta: true)] = new SelectAllTextIntent(SelectionChangedCause.Keyboard);
        map[Act("Z", meta: true)] = new UndoTextIntent(SelectionChangedCause.Keyboard);
        map[Act("Z", shift: true, meta: true)] = new RedoTextIntent(SelectionChangedCause.Keyboard);
        map[Act("E", control: true)] =
            new ExtendSelectionToLineBreakIntent(forward: true, collapseSelection: true);
        map[Act("A", control: true)] =
            new ExtendSelectionToLineBreakIntent(forward: false, collapseSelection: true);
        map[Act("F", control: true)] =
            new ExtendSelectionByCharacterIntent(forward: true, collapseSelection: true);
        map[Act("B", control: true)] =
            new ExtendSelectionByCharacterIntent(forward: false, collapseSelection: true);
        map[Act("N", control: true)] =
            new ExtendSelectionVerticallyToAdjacentLineIntent(forward: true, collapseSelection: true);
        map[Act("P", control: true)] =
            new ExtendSelectionVerticallyToAdjacentLineIntent(forward: false, collapseSelection: true);
        map[Act("Space")] = DoNothing;
        map[Act("Enter")] = DoNothing;
        return map;
    }

    private static Dictionary<ShortcutActivator, Intent> BuildCommonDisablingTextShortcuts()
    {
        return new Dictionary<ShortcutActivator, Intent>
        {
            [Act("ArrowDown", alt: true)] = DoNothing,
            [Act("ArrowLeft", alt: true)] = DoNothing,
            [Act("ArrowRight", alt: true)] = DoNothing,
            [Act("ArrowUp", alt: true)] = DoNothing,
            [Act("ArrowDown", meta: true)] = DoNothing,
            [Act("ArrowLeft", meta: true)] = DoNothing,
            [Act("ArrowRight", meta: true)] = DoNothing,
            [Act("ArrowUp", meta: true)] = DoNothing,
            [Act("ArrowDown")] = DoNothing,
            [Act("ArrowLeft")] = DoNothing,
            [Act("ArrowRight")] = DoNothing,
            [Act("ArrowUp")] = DoNothing,
            [Act("ArrowLeft", control: true)] = DoNothing,
            [Act("ArrowRight", control: true)] = DoNothing,
            [Act("ArrowLeft", control: true, shift: true)] = DoNothing,
            [Act("ArrowRight", control: true, shift: true)] = DoNothing,
            [Act("Space")] = DoNothing,
            [Act("Enter")] = DoNothing,
        };
    }

    private static Dictionary<ShortcutActivator, Intent> BuildIOSDisablingTextShortcuts()
    {
        // Hand backspace/delete events that do not depend on text layout back to the IME so it can
        // update composing text properly.
        return new Dictionary<ShortcutActivator, Intent>
        {
            [Act("Backspace")] = DoNothing,
            [Act("Backspace", shift: true)] = DoNothing,
            [Act("Delete")] = DoNothing,
            [Act("Delete", shift: true)] = DoNothing,
            [Act("Backspace", shift: true, alt: true)] = DoNothing,
            [Act("Backspace", alt: true)] = DoNothing,
            [Act("Delete", shift: true, alt: true)] = DoNothing,
            [Act("Delete", alt: true)] = DoNothing,
        };
    }

    private static Dictionary<ShortcutActivator, Intent> BuildMacDisablingTextShortcuts()
    {
        var map = new Dictionary<ShortcutActivator, Intent>(CommonDisablingTextShortcuts);
        foreach ((ShortcutActivator activator, Intent intent) in IOSDisablingTextShortcuts)
        {
            map[activator] = intent;
        }

        map[Act("Escape")] = DoNothing;
        map[Act("Tab")] = DoNothing;
        map[Act("Tab", shift: true)] = DoNothing;
        map[Act("ArrowDown", shift: true, alt: true)] = DoNothing;
        map[Act("ArrowUp", shift: true, alt: true)] = DoNothing;
        map[Act("ArrowLeft", shift: true)] = DoNothing;
        map[Act("ArrowRight", shift: true)] = DoNothing;
        map[Act("ArrowLeft", shift: true, alt: true)] = DoNothing;
        map[Act("ArrowRight", shift: true, alt: true)] = DoNothing;
        map[Act("ArrowLeft", shift: true, meta: true)] = DoNothing;
        map[Act("ArrowRight", shift: true, meta: true)] = DoNothing;
        map[Act("PageUp")] = DoNothing;
        map[Act("PageDown")] = DoNothing;
        map[Act("End")] = DoNothing;
        map[Act("Home")] = DoNothing;
        map[Act("PageUp", shift: true)] = DoNothing;
        map[Act("PageDown", shift: true)] = DoNothing;
        map[Act("End", shift: true)] = DoNothing;
        map[Act("Home", shift: true)] = DoNothing;
        map[Act("End", control: true)] = DoNothing;
        map[Act("Home", control: true)] = DoNothing;
        return map;
    }

    private static Dictionary<ShortcutActivator, Intent> BuildWebDisablingTextShortcuts()
    {
        // Web handles its text selection natively, so none of these shortcuts run in Flutter.
        var map = new Dictionary<ShortcutActivator, Intent>();
        foreach (bool pressShift in (bool[])[true, false])
        {
            map[Act("Backspace", shift: pressShift)] = DoNothing;
            map[Act("Delete", shift: pressShift)] = DoNothing;
            map[Act("Backspace", alt: true, shift: pressShift)] = DoNothing;
            map[Act("Delete", alt: true, shift: pressShift)] = DoNothing;
            map[Act("Backspace", control: true, shift: pressShift)] = DoNothing;
            map[Act("Delete", control: true, shift: pressShift)] = DoNothing;
            map[Act("Backspace", meta: true, shift: pressShift)] = DoNothing;
            map[Act("Delete", meta: true, shift: pressShift)] = DoNothing;
        }

        foreach ((ShortcutActivator activator, Intent _) in CommonDisablingTextShortcuts)
        {
            map[activator] = DoNothing;
        }

        foreach ((ShortcutActivator activator, Intent _) in ClipboardShortcuts)
        {
            map[activator] = DoNothing;
        }

        map[Act("X", meta: true)] = DoNothing;
        map[Act("C", meta: true)] = DoNothing;
        map[Act("V", meta: true)] = DoNothing;
        map[Act("A", control: true)] = DoNothing;
        map[Act("A", meta: true)] = DoNothing;
        return map;
    }

    private static Dictionary<ShortcutActivator, Intent> BuildWebLinuxDisablingTextShortcuts()
    {
        var map = new Dictionary<ShortcutActivator, Intent>(WebDisablingTextShortcuts);
        foreach ((ShortcutActivator activator, Intent _) in LinuxNumpadShortcuts)
        {
            map[activator] = DoNothing;
        }

        return map;
    }
}

/// <summary>
/// Maps a macOS NSStandardKeyBindingResponding selector to the [Intent] that implements it, or null
/// when the selector is unknown.
/// </summary>
public static class MacOsSelectors
{
    private static readonly Dictionary<string, Intent> Selectors = new(StringComparer.Ordinal)
    {
        ["deleteBackward:"] = new DeleteCharacterIntent(forward: false),
        ["deleteWordBackward:"] = new DeleteToNextWordBoundaryIntent(forward: false),
        ["deleteToBeginningOfLine:"] = new DeleteToLineBreakIntent(forward: false),
        ["deleteForward:"] = new DeleteCharacterIntent(forward: true),
        ["deleteWordForward:"] = new DeleteToNextWordBoundaryIntent(forward: true),
        ["deleteToEndOfLine:"] = new DeleteToLineBreakIntent(forward: true),
        ["moveLeft:"] = new ExtendSelectionByCharacterIntent(forward: false, collapseSelection: true),
        ["moveRight:"] = new ExtendSelectionByCharacterIntent(forward: true, collapseSelection: true),
        ["moveForward:"] = new ExtendSelectionByCharacterIntent(forward: true, collapseSelection: true),
        ["moveBackward:"] = new ExtendSelectionByCharacterIntent(forward: false, collapseSelection: true),
        ["moveUp:"] = new ExtendSelectionVerticallyToAdjacentLineIntent(forward: false, collapseSelection: true),
        ["moveDown:"] = new ExtendSelectionVerticallyToAdjacentLineIntent(forward: true, collapseSelection: true),
        ["moveLeftAndModifySelection:"] =
            new ExtendSelectionByCharacterIntent(forward: false, collapseSelection: false),
        ["moveRightAndModifySelection:"] =
            new ExtendSelectionByCharacterIntent(forward: true, collapseSelection: false),
        ["moveUpAndModifySelection:"] =
            new ExtendSelectionVerticallyToAdjacentLineIntent(forward: false, collapseSelection: false),
        ["moveDownAndModifySelection:"] =
            new ExtendSelectionVerticallyToAdjacentLineIntent(forward: true, collapseSelection: false),
        ["moveWordLeft:"] = new ExtendSelectionToNextWordBoundaryIntent(forward: false, collapseSelection: true),
        ["moveWordRight:"] = new ExtendSelectionToNextWordBoundaryIntent(forward: true, collapseSelection: true),
        ["moveToBeginningOfParagraph:"] =
            new ExtendSelectionToLineBreakIntent(forward: false, collapseSelection: true),
        ["moveToEndOfParagraph:"] = new ExtendSelectionToLineBreakIntent(forward: true, collapseSelection: true),
        ["moveWordLeftAndModifySelection:"] =
            new ExtendSelectionToNextWordBoundaryOrCaretLocationIntent(forward: false),
        ["moveWordRightAndModifySelection:"] =
            new ExtendSelectionToNextWordBoundaryOrCaretLocationIntent(forward: true),
        ["moveParagraphBackwardAndModifySelection:"] =
            new ExtendSelectionToNextParagraphBoundaryOrCaretLocationIntent(forward: false),
        ["moveParagraphForwardAndModifySelection:"] =
            new ExtendSelectionToNextParagraphBoundaryOrCaretLocationIntent(forward: true),
        ["moveToLeftEndOfLine:"] = new ExtendSelectionToLineBreakIntent(forward: false, collapseSelection: true),
        ["moveToRightEndOfLine:"] = new ExtendSelectionToLineBreakIntent(forward: true, collapseSelection: true),
        ["moveToBeginningOfDocument:"] =
            new ExtendSelectionToDocumentBoundaryIntent(forward: false, collapseSelection: true),
        ["moveToEndOfDocument:"] =
            new ExtendSelectionToDocumentBoundaryIntent(forward: true, collapseSelection: true),
        ["moveToLeftEndOfLineAndModifySelection:"] = new ExpandSelectionToLineBreakIntent(forward: false),
        ["moveToRightEndOfLineAndModifySelection:"] = new ExpandSelectionToLineBreakIntent(forward: true),
        ["moveToBeginningOfDocumentAndModifySelection:"] =
            new ExpandSelectionToDocumentBoundaryIntent(forward: false),
        ["moveToEndOfDocumentAndModifySelection:"] = new ExpandSelectionToDocumentBoundaryIntent(forward: true),
        ["transpose:"] = new TransposeCharactersIntent(),
        ["scrollToBeginningOfDocument:"] = new ScrollToDocumentBoundaryIntent(forward: false),
        ["scrollToEndOfDocument:"] = new ScrollToDocumentBoundaryIntent(forward: true),
        ["scrollPageUp:"] = new ScrollIntent(direction: AxisDirection.Up, type: ScrollIncrementType.Page),
        ["scrollPageDown:"] = new ScrollIntent(direction: AxisDirection.Down, type: ScrollIncrementType.Page),
        ["pageUpAndModifySelection:"] =
            new ExtendSelectionVerticallyToAdjacentPageIntent(forward: false, collapseSelection: false),
        ["pageDownAndModifySelection:"] =
            new ExtendSelectionVerticallyToAdjacentPageIntent(forward: true, collapseSelection: false),
        // Escape key when there is no IME selection popup.
        ["cancelOperation:"] = new DismissIntent(),
        // Tab when there is no IME selection popup.
        ["insertTab:"] = new NextFocusIntent(),
        ["insertBacktab:"] = new PreviousFocusIntent(),
    };

    /// <summary>The [Intent] a macOS selector maps to, or null when it is unknown.</summary>
    public static Intent? IntentForMacOsSelector(string selectorName)
    {
        return Selectors.GetValueOrDefault(selectorName);
    }
}
