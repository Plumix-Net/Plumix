using System.Collections;
using System.Reflection;
using System.Text;
using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit.Sdk;
using TextDirection = Plumix.UI.TextDirection;

// C#-only test infrastructure: flutter_test's semantics surface — `tester.ensureSemantics`,
// `tester.getSemantics`, `tester.semantics.find` (controller.dart), `tester.takeAnnouncements` and
// `CapturedAccessibilityAnnouncement` (binding.dart), `matchesSemantics`/`containsSemantics` and
// `isAccessibilityAnnouncement` (matchers.dart), and semantics_tester.dart's `SemanticsTester`.

namespace Plumix.Tests;

internal sealed partial class FrameworkDartTester
{
    private SemanticsHandle? _semanticsHandle;
    private SemanticsController? _semantics;
    private MessageHandler? _announcementHandler;
    private readonly List<CapturedAccessibilityAnnouncement> _channelAnnouncements = [];

    /// <summary>Dart's <c>tester.semantics</c>.</summary>
    public SemanticsController Semantics => _semantics ??= new SemanticsController(this);

    /// <summary>
    /// Dart's <c>tester.ensureSemantics()</c>: keeps the view producing a semantics tree until the
    /// handle is disposed. The tree is built by the next frame, so pump after enabling.
    /// </summary>
    public SemanticsHandle EnsureSemantics()
    {
        PipelineOwner owner = RenderView.Owner!;
        bool createsOwner = owner.SemanticsOwner is null;
        SemanticsHandle handle = owner.EnsureSemantics();
        if (createsOwner)
        {
            // flutter_test's view clears every cached configuration before its initial semantics.
            RenderView.ClearSemantics();
            RenderView.ScheduleInitialSemantics();
        }

        return handle;
    }

    /// <summary>
    /// The semantics half of Dart's <c>drawFrame</c> for this view: the root pipeline owner only flushes
    /// its children while it has semantics itself, and <see cref="EnsureSemantics"/> enables them on the
    /// view's owner alone.
    /// </summary>
    private void FlushOwnSemantics()
    {
        if (RendererBinding.Instance.SendFramesToEngine
            && RendererBinding.Instance.RenderViews.FirstOrDefault(IsOwnRenderView)?.Owner is { } owner)
        {
            owner.FlushSemantics();
        }
    }

    /// <summary>
    /// Dart's <c>tester.getSemantics(finder)</c>: the semantics node of the single element's render
    /// object, or of the nearest ancestor's when that one has none or is merged into its parent.
    /// </summary>
    public SemanticsNode GetSemantics(Finder finder) => GetSemantics(SingleElement(finder, "getSemantics"));

    /// <summary>Dart's <c>tester.getSemantics</c> for an element already found.</summary>
    public SemanticsNode GetSemantics(Element element)
    {
        RenderObject? renderObject = element.FindRenderObject();
        SemanticsNode? result = renderObject?.SemanticsNode;
        while (renderObject is not null && (result is null || result.IsMergedIntoParent))
        {
            renderObject = renderObject.Parent;
            result = renderObject?.SemanticsNode;
        }

        return result ?? throw new InvalidOperationException(
            "No Semantics data found. Did you forget tester.EnsureSemantics() (or semanticsEnabled: true)?");
    }

    /// <summary>The root of the view's semantics tree, or null while semantics are off.</summary>
    public SemanticsNode? SemanticsRootNode => RenderView.Owner?.SemanticsOwner?.RootNode;

    /// <summary>
    /// Dart's <c>tester.takeAnnouncements()</c>: every accessibility announcement made since the last
    /// call, oldest first.
    /// </summary>
    /// <remarks>
    /// Captured the way flutter_test's binding captures them: as <c>{type: announce, data: ...}</c>
    /// messages on <see cref="SystemChannels.Accessibility"/>.
    /// </remarks>
    public List<CapturedAccessibilityAnnouncement> TakeAnnouncements()
    {
        var announcements = new List<CapturedAccessibilityAnnouncement>(_channelAnnouncements);
        _channelAnnouncements.Clear();
        return announcements;
    }

    private void InstallAnnouncementCapture()
    {
        // Dart sets the handler only if there is currently none.
        if (PlatformMessageHandlerOf(SystemChannels.Accessibility.Name) is null)
        {
            _announcementHandler = HandleAccessibilityMessage;
            ServicesBinding.Instance.DefaultBinaryMessenger.SetPlatformMessageHandler(
                SystemChannels.Accessibility.Name,
                _announcementHandler);
        }
    }

    private void UninstallAnnouncementCapture()
    {
        if (_announcementHandler is not null)
        {
            if (ReferenceEquals(PlatformMessageHandlerOf(SystemChannels.Accessibility.Name), _announcementHandler))
            {
                ServicesBinding.Instance.DefaultBinaryMessenger.SetPlatformMessageHandler(
                    SystemChannels.Accessibility.Name,
                    null);
            }

            _announcementHandler = null;
        }

        _channelAnnouncements.Clear();
    }

    /// <summary>The platform-side handler registered for <paramref name="channel"/>, if it can be read.</summary>
    internal static MessageHandler? PlatformMessageHandlerOf(string channel)
    {
        BinaryMessenger messenger = ServicesBinding.Instance.DefaultBinaryMessenger;
        FieldInfo? field = typeof(PlatformBinaryMessenger).GetField(
            "_platformHandlers",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (messenger is not PlatformBinaryMessenger || field?.GetValue(messenger) is not IDictionary handlers)
        {
            return null;
        }

        return handlers.Contains(channel) ? handlers[channel] as MessageHandler : null;
    }

    // binding.dart's `_handleAnnouncementMessage`.
    private Task<ByteData?>? HandleAccessibilityMessage(ByteData? message)
    {
        object? decoded = SystemChannels.Accessibility.Codec.DecodeMessage(message);
        if (decoded is IDictionary map
            && Equals(map["type"], "announce")
            && map["data"] is IDictionary data)
        {
            _channelAnnouncements.Add(new CapturedAccessibilityAnnouncement(
                Message: data["message"]?.ToString() ?? string.Empty,
                ViewId: data["viewId"] is { } viewId ? Convert.ToInt32(viewId) : 0,
                TextDirection: data["textDirection"] is { } direction
                    ? (TextDirection)Convert.ToInt32(direction)
                    : TextDirection.Ltr,
                Assertiveness: data["assertiveness"] is { } assertiveness
                    ? (Assertiveness)Convert.ToInt32(assertiveness)
                    : Assertiveness.Polite));
        }

        return Task.FromResult<ByteData?>(null);
    }

}

/// <summary>flutter_test's <c>CapturedAccessibilityAnnouncement</c> (binding.dart).</summary>
internal sealed record CapturedAccessibilityAnnouncement(
    string Message,
    int ViewId,
    TextDirection TextDirection,
    Assertiveness Assertiveness = Assertiveness.Polite);

/// <summary>flutter_test's <c>SemanticsController</c> (<c>tester.semantics</c>), the lookups the tests use.</summary>
internal sealed class SemanticsController(FrameworkDartTester tester)
{
    /// <summary>
    /// Dart's <c>tester.semantics.find(finder)</c>: the semantics node of the single element, climbing
    /// to the first ancestor that has an unmerged node.
    /// </summary>
    public SemanticsNode Find(Finder finder) => tester.GetSemantics(finder);
}

/// <summary>
/// semantics_tester.dart's <c>SemanticsTester</c>: holds a semantics handle for its lifetime and
/// answers <c>includesNodeWith</c>/<c>nodesWith</c>. (<c>hasSemantics</c> over a <c>TestSemantics</c>
/// tree is not ported here.)
/// </summary>
internal sealed class SemanticsTester : IDisposable
{
    private readonly FrameworkDartTester _tester;
    private SemanticsHandle? _handle;

    public SemanticsTester(FrameworkDartTester tester)
    {
        _tester = tester;
        _handle = tester.EnsureSemantics();
    }

    /// <summary>The root of the semantics tree.</summary>
    public SemanticsNode RootNode => _tester.SemanticsRootNode
                                     ?? throw new InvalidOperationException("Semantics are not built yet.");

    /// <summary>
    /// <c>nodesWith(...)</c>: every node (depth-first) whose given properties all match; flags and
    /// actions, when given, must match exactly.
    /// </summary>
    public List<SemanticsNode> NodesWith(
        string? label = null,
        string? value = null,
        string? hint = null,
        TextDirection? textDirection = null,
        SemanticsActions? actions = null,
        SemanticsFlags? flags = null,
        IReadOnlySet<SemanticsTag>? tags = null,
        double? scrollPosition = null,
        double? scrollExtentMax = null,
        double? scrollExtentMin = null,
        int? currentValueLength = null,
        int? maxValueLength = null)
    {
        var result = new List<SemanticsNode>();
        void Visit(SemanticsNode node)
        {
            SemanticsData data = node.GetSemanticsData();
            if ((label is null || data.Label == label)
                && (value is null || data.Value == value)
                && (hint is null || data.Hint == hint)
                && (textDirection is null || data.TextDirection == textDirection)
                && (actions is null || data.Actions == actions)
                && (flags is null || data.Flags == flags)
                && (tags is null || (data.Tags is { } actual && actual.SetEquals(tags)))
                && (scrollPosition is null || data.ScrollPosition == scrollPosition)
                && (scrollExtentMax is null || data.ScrollExtentMax == scrollExtentMax)
                && (scrollExtentMin is null || data.ScrollExtentMin == scrollExtentMin)
                && (currentValueLength is null || data.CurrentValueLength == currentValueLength)
                && (maxValueLength is null || data.MaxValueLength == maxValueLength))
            {
                result.Add(node);
            }

            foreach (SemanticsNode child in node.Children)
            {
                Visit(child);
            }
        }

        if (_tester.SemanticsRootNode is { } root)
        {
            Visit(root);
        }

        return result;
    }

    /// <summary><c>includesNodeWith(...)</c>: whether <see cref="NodesWith"/> finds any node.</summary>
    public bool IncludesNodeWith(
        string? label = null,
        string? value = null,
        string? hint = null,
        TextDirection? textDirection = null,
        SemanticsActions? actions = null,
        SemanticsFlags? flags = null,
        IReadOnlySet<SemanticsTag>? tags = null) =>
        NodesWith(label, value, hint, textDirection, actions, flags, tags).Count > 0;

    public void Dispose()
    {
        _handle?.Dispose();
        _handle = null;
    }
}

/// <summary>
/// flutter_test's <c>_MatchesSemanticsData</c>: built by <see cref="SemanticsMatchers.MatchesSemantics"/>
/// (every flag and action not listed must be off) or <see cref="SemanticsMatchers.ContainsSemantics"/>
/// (only the listed ones are checked).
/// </summary>
internal sealed class SemanticsMatcher
{
    public string? Identifier { get; init; }

    public string? Label { get; init; }

    public string? Hint { get; init; }

    public string? Value { get; init; }

    public string? IncreasedValue { get; init; }

    public string? DecreasedValue { get; init; }

    public string? Tooltip { get; init; }

    public TextDirection? TextDirection { get; init; }

    public Rect? Rect { get; init; }

    public Size? Size { get; init; }

    public int? PlatformViewId { get; init; }

    public int? MaxValueLength { get; init; }

    public int? CurrentValueLength { get; init; }

    public SemanticsValidationResult? ValidationResult { get; init; }

    public SemanticsInputType? InputType { get; init; }

    public string? MinValue { get; init; }

    public string? MaxValue { get; init; }

    public SemanticsRole? Role { get; init; }

    /// <summary>The flags to check: true must be set, false must be clear.</summary>
    public IReadOnlyDictionary<SemanticsFlags, bool> Flags { get; init; } = new Dictionary<SemanticsFlags, bool>();

    /// <summary>The actions to check: true must be present, false must be absent.</summary>
    public IReadOnlyDictionary<SemanticsActions, bool> Actions { get; init; } =
        new Dictionary<SemanticsActions, bool>();

    /// <summary>Matchers for the node's children, in order; null skips the check.</summary>
    public IReadOnlyList<SemanticsMatcher>? Children { get; init; }

    /// <summary>The first mismatch, or null when <paramref name="node"/> matches.</summary>
    public string? Describe(SemanticsNode node, string path = "node")
    {
        SemanticsData data = node.GetSemanticsData();
        string? failure = DescribeData(data);
        if (failure is not null)
        {
            return $"{path}: {failure}";
        }

        if (Children is null)
        {
            return null;
        }

        if (Children.Count != node.Children.Count)
        {
            string plural = Children.Count == 1 ? string.Empty : "ren";
            return $"{path}: expected {Children.Count} child{plural}, found {node.Children.Count}";
        }

        for (int i = 0; i < Children.Count; i++)
        {
            if (Children[i].Describe(node.Children[i], $"{path}.children[{i}]") is { } childFailure)
            {
                return childFailure;
            }
        }

        return null;
    }

    /// <summary>The first mismatch against <paramref name="data"/> (children are not checked).</summary>
    public string? DescribeData(SemanticsData data)
    {
        string? Mismatch(string name, string? expected, string actual) =>
            expected is not null && expected != actual ? $"{name} was: \"{actual}\" (expected \"{expected}\")" : null;

        string? failure = Mismatch("identifier", Identifier, data.Identifier)
                          ?? Mismatch("label", Label, data.Label)
                          ?? Mismatch("hint", Hint, data.Hint)
                          ?? Mismatch("value", Value, data.Value)
                          ?? Mismatch("increasedValue", IncreasedValue, data.IncreasedValue)
                          ?? Mismatch("decreasedValue", DecreasedValue, data.DecreasedValue)
                          ?? Mismatch("tooltip", Tooltip, data.Tooltip);
        if (failure is not null)
        {
            return failure;
        }

        if (TextDirection is not null && TextDirection != data.TextDirection)
        {
            return $"textDirection was: {data.TextDirection}";
        }

        if (Rect is { } rect && rect != data.Rect)
        {
            return $"rect was: {data.Rect}";
        }

        if (Size is { } size && size != data.Rect.Size)
        {
            return $"size was: {data.Rect.Size}";
        }

        if (PlatformViewId is not null && PlatformViewId != data.PlatformViewId)
        {
            return $"platformViewId was: {data.PlatformViewId}";
        }

        if (CurrentValueLength is not null && CurrentValueLength != data.CurrentValueLength)
        {
            return $"currentValueLength was: {data.CurrentValueLength}";
        }

        if (MaxValueLength is not null && MaxValueLength != data.MaxValueLength)
        {
            return $"maxValueLength was: {data.MaxValueLength}";
        }

        if (ValidationResult is not null && ValidationResult != data.ValidationResult)
        {
            return $"validationResult was: {data.ValidationResult}";
        }

        if (InputType is not null && InputType != data.InputType)
        {
            return $"inputType was: {data.InputType}";
        }

        if ((MinValue is not null && MinValue != data.MinValue) || (MaxValue is not null && MaxValue != data.MaxValue))
        {
            return $"minValue/maxValue was: {data.MinValue}/{data.MaxValue}";
        }

        if (Role is not null && Role != data.Role)
        {
            return $"role was: {data.Role}";
        }

        List<string> missingActions = Actions.Where(entry => entry.Value && !data.HasAction(entry.Key))
            .Select(entry => entry.Key.ToString())
            .ToList();
        List<string> unexpectedActions = Actions.Where(entry => !entry.Value && data.HasAction(entry.Key))
            .Select(entry => entry.Key.ToString())
            .ToList();
        if (missingActions.Count > 0 || unexpectedActions.Count > 0)
        {
            return $"missing actions: [{string.Join(", ", missingActions)}] "
                   + $"unexpected actions: [{string.Join(", ", unexpectedActions)}]";
        }

        List<string> missingFlags = Flags.Where(entry => entry.Value && !data.HasFlag(entry.Key))
            .Select(entry => entry.Key.ToString())
            .ToList();
        List<string> unexpectedFlags = Flags.Where(entry => !entry.Value && data.HasFlag(entry.Key))
            .Select(entry => entry.Key.ToString())
            .ToList();
        if (missingFlags.Count > 0 || unexpectedFlags.Count > 0)
        {
            return $"missing flags: {string.Join(",", missingFlags)} "
                   + $"unexpected flags: {string.Join(",", unexpectedFlags)}";
        }

        return null;
    }
}

/// <summary>
/// flutter_test's semantics and announcement matchers. Use with <c>using static
/// Plumix.Tests.SemanticsMatchers;</c>: <c>ExpectSemantics(tester.GetSemantics(Find.Text("x")),
/// MatchesSemantics(label: "x", isButton: true))</c>.
/// </summary>
internal static class SemanticsMatchers
{
    /// <summary><c>expect(node, matcher)</c>: fails with the first mismatch and the node's data.</summary>
    public static void ExpectSemantics(SemanticsNode node, SemanticsMatcher matcher)
    {
        if (matcher.Describe(node) is { } failure)
        {
            throw new XunitException($"Semantics did not match: {failure}\nActual: {node.GetSemanticsData()}");
        }
    }

    /// <summary><c>expect(finder, matchesSemantics(...))</c>, through <c>tester.getSemantics</c>.</summary>
    public static void ExpectSemantics(Finder finder, SemanticsMatcher matcher) =>
        ExpectSemantics(FrameworkDartTester.RequireCurrent().GetSemantics(finder), matcher);

    /// <summary><c>expect(node, isNot(matcher))</c>.</summary>
    public static void ExpectNotSemantics(SemanticsNode node, SemanticsMatcher matcher)
    {
        if (matcher.Describe(node) is null)
        {
            throw new XunitException($"Semantics unexpectedly matched: {node.GetSemanticsData()}");
        }
    }

    /// <summary>
    /// flutter_test's <c>matchesSemantics</c>: the strings given must match, and every flag and action
    /// must be exactly as given (false when not listed). <c>validationResult</c> is always compared.
    /// </summary>
    public static SemanticsMatcher MatchesSemantics(
        string? identifier = null,
        string? label = null,
        string? hint = null,
        string? value = null,
        string? increasedValue = null,
        string? decreasedValue = null,
        string? tooltip = null,
        TextDirection? textDirection = null,
        Rect? rect = null,
        Size? size = null,
        int? platformViewId = null,
        int? maxValueLength = null,
        int? currentValueLength = null,
        SemanticsValidationResult validationResult = SemanticsValidationResult.None,
        SemanticsInputType? inputType = null,
        string? maxValue = null,
        string? minValue = null,
        SemanticsRole? role = null,
        // Flags
        bool hasCheckedState = false,
        bool isChecked = false,
        bool isCheckStateMixed = false,
        bool isSelected = false,
        bool hasSelectedState = false,
        bool isButton = false,
        bool isSlider = false,
        bool isKeyboardKey = false,
        bool isLink = false,
        bool isFocused = false,
        bool isFocusable = false,
        bool isTextField = false,
        bool isReadOnly = false,
        bool hasEnabledState = false,
        bool isEnabled = false,
        bool isInMutuallyExclusiveGroup = false,
        bool isHeader = false,
        bool isObscured = false,
        bool isMultiline = false,
        bool namesRoute = false,
        bool scopesRoute = false,
        bool isHidden = false,
        bool isImage = false,
        bool isLiveRegion = false,
        bool hasToggledState = false,
        bool isToggled = false,
        bool hasImplicitScrolling = false,
        bool hasExpandedState = false,
        bool isExpanded = false,
        bool hasRequiredState = false,
        bool isRequired = false,
        // Actions
        bool hasTapAction = false,
        bool hasFocusAction = false,
        bool hasLongPressAction = false,
        bool hasScrollLeftAction = false,
        bool hasScrollRightAction = false,
        bool hasScrollUpAction = false,
        bool hasScrollDownAction = false,
        bool hasIncreaseAction = false,
        bool hasDecreaseAction = false,
        bool hasShowOnScreenAction = false,
        bool hasMoveCursorForwardByCharacterAction = false,
        bool hasMoveCursorBackwardByCharacterAction = false,
        bool hasMoveCursorForwardByWordAction = false,
        bool hasMoveCursorBackwardByWordAction = false,
        bool hasSetTextAction = false,
        bool hasSetSelectionAction = false,
        bool hasCopyAction = false,
        bool hasCutAction = false,
        bool hasPasteAction = false,
        bool hasDidGainAccessibilityFocusAction = false,
        bool hasDidLoseAccessibilityFocusAction = false,
        bool hasDismissAction = false,
        IReadOnlyList<SemanticsMatcher>? children = null)
    {
        return ContainsSemantics(
            identifier, label, hint, value, increasedValue, decreasedValue, tooltip, textDirection, rect, size,
            platformViewId, maxValueLength, currentValueLength, validationResult, inputType, maxValue, minValue, role,
            hasCheckedState, isChecked, isCheckStateMixed, isSelected, hasSelectedState, isButton, isSlider,
            isKeyboardKey, isLink, isFocused, isFocusable, isTextField, isReadOnly, hasEnabledState, isEnabled,
            isInMutuallyExclusiveGroup, isHeader, isObscured, isMultiline, namesRoute, scopesRoute, isHidden, isImage,
            isLiveRegion, hasToggledState, isToggled, hasImplicitScrolling, hasExpandedState, isExpanded,
            hasRequiredState, isRequired,
            hasTapAction, hasFocusAction, hasLongPressAction, hasScrollLeftAction, hasScrollRightAction,
            hasScrollUpAction, hasScrollDownAction, hasIncreaseAction, hasDecreaseAction, hasShowOnScreenAction,
            hasMoveCursorForwardByCharacterAction, hasMoveCursorBackwardByCharacterAction,
            hasMoveCursorForwardByWordAction, hasMoveCursorBackwardByWordAction, hasSetTextAction,
            hasSetSelectionAction, hasCopyAction, hasCutAction, hasPasteAction, hasDidGainAccessibilityFocusAction,
            hasDidLoseAccessibilityFocusAction, hasDismissAction, children);
    }

    /// <summary>
    /// flutter_test's <c>containsSemantics</c>: only the properties, flags and actions given (non-null) are
    /// checked.
    /// </summary>
    public static SemanticsMatcher ContainsSemantics(
        string? identifier = null,
        string? label = null,
        string? hint = null,
        string? value = null,
        string? increasedValue = null,
        string? decreasedValue = null,
        string? tooltip = null,
        TextDirection? textDirection = null,
        Rect? rect = null,
        Size? size = null,
        int? platformViewId = null,
        int? maxValueLength = null,
        int? currentValueLength = null,
        SemanticsValidationResult? validationResult = null,
        SemanticsInputType? inputType = null,
        string? maxValue = null,
        string? minValue = null,
        SemanticsRole? role = null,
        // Flags
        bool? hasCheckedState = null,
        bool? isChecked = null,
        bool? isCheckStateMixed = null,
        bool? isSelected = null,
        bool? hasSelectedState = null,
        bool? isButton = null,
        bool? isSlider = null,
        bool? isKeyboardKey = null,
        bool? isLink = null,
        bool? isFocused = null,
        bool? isFocusable = null,
        bool? isTextField = null,
        bool? isReadOnly = null,
        bool? hasEnabledState = null,
        bool? isEnabled = null,
        bool? isInMutuallyExclusiveGroup = null,
        bool? isHeader = null,
        bool? isObscured = null,
        bool? isMultiline = null,
        bool? namesRoute = null,
        bool? scopesRoute = null,
        bool? isHidden = null,
        bool? isImage = null,
        bool? isLiveRegion = null,
        bool? hasToggledState = null,
        bool? isToggled = null,
        bool? hasImplicitScrolling = null,
        bool? hasExpandedState = null,
        bool? isExpanded = null,
        bool? hasRequiredState = null,
        bool? isRequired = null,
        // Actions
        bool? hasTapAction = null,
        bool? hasFocusAction = null,
        bool? hasLongPressAction = null,
        bool? hasScrollLeftAction = null,
        bool? hasScrollRightAction = null,
        bool? hasScrollUpAction = null,
        bool? hasScrollDownAction = null,
        bool? hasIncreaseAction = null,
        bool? hasDecreaseAction = null,
        bool? hasShowOnScreenAction = null,
        bool? hasMoveCursorForwardByCharacterAction = null,
        bool? hasMoveCursorBackwardByCharacterAction = null,
        bool? hasMoveCursorForwardByWordAction = null,
        bool? hasMoveCursorBackwardByWordAction = null,
        bool? hasSetTextAction = null,
        bool? hasSetSelectionAction = null,
        bool? hasCopyAction = null,
        bool? hasCutAction = null,
        bool? hasPasteAction = null,
        bool? hasDidGainAccessibilityFocusAction = null,
        bool? hasDidLoseAccessibilityFocusAction = null,
        bool? hasDismissAction = null,
        IReadOnlyList<SemanticsMatcher>? children = null)
    {
        var flags = new Dictionary<SemanticsFlags, bool>();
        void Flag(SemanticsFlags flag, bool? expected)
        {
            if (expected is { } on)
            {
                flags[flag] = on;
            }
        }

        Flag(SemanticsFlags.HasCheckedState, hasCheckedState);
        Flag(SemanticsFlags.IsChecked, isChecked);
        Flag(SemanticsFlags.IsCheckStateMixed, isCheckStateMixed);
        Flag(SemanticsFlags.IsSelected, isSelected);
        Flag(SemanticsFlags.HasSelectedState, hasSelectedState);
        Flag(SemanticsFlags.IsButton, isButton);
        Flag(SemanticsFlags.IsSlider, isSlider);
        Flag(SemanticsFlags.IsKeyboardKey, isKeyboardKey);
        Flag(SemanticsFlags.IsLink, isLink);
        Flag(SemanticsFlags.IsFocused, isFocused);
        Flag(SemanticsFlags.IsFocusable, isFocusable);
        Flag(SemanticsFlags.IsTextField, isTextField);
        Flag(SemanticsFlags.IsReadOnly, isReadOnly);
        Flag(SemanticsFlags.HasEnabledState, hasEnabledState);
        Flag(SemanticsFlags.IsEnabled, isEnabled);
        Flag(SemanticsFlags.IsInMutuallyExclusiveGroup, isInMutuallyExclusiveGroup);
        Flag(SemanticsFlags.IsHeader, isHeader);
        Flag(SemanticsFlags.IsObscured, isObscured);
        Flag(SemanticsFlags.IsMultiline, isMultiline);
        Flag(SemanticsFlags.NamesRoute, namesRoute);
        Flag(SemanticsFlags.ScopesRoute, scopesRoute);
        Flag(SemanticsFlags.IsHidden, isHidden);
        Flag(SemanticsFlags.IsImage, isImage);
        Flag(SemanticsFlags.IsLiveRegion, isLiveRegion);
        Flag(SemanticsFlags.HasToggledState, hasToggledState);
        Flag(SemanticsFlags.IsToggled, isToggled);
        Flag(SemanticsFlags.HasImplicitScrolling, hasImplicitScrolling);
        Flag(SemanticsFlags.HasExpandedState, hasExpandedState);
        Flag(SemanticsFlags.IsExpanded, isExpanded);
        Flag(SemanticsFlags.HasRequiredState, hasRequiredState);
        Flag(SemanticsFlags.IsRequired, isRequired);

        var actions = new Dictionary<SemanticsActions, bool>();
        void Action(SemanticsActions action, bool? expected)
        {
            if (expected is { } on)
            {
                actions[action] = on;
            }
        }

        Action(SemanticsActions.Tap, hasTapAction);
        Action(SemanticsActions.Focus, hasFocusAction);
        Action(SemanticsActions.LongPress, hasLongPressAction);
        Action(SemanticsActions.ScrollLeft, hasScrollLeftAction);
        Action(SemanticsActions.ScrollRight, hasScrollRightAction);
        Action(SemanticsActions.ScrollUp, hasScrollUpAction);
        Action(SemanticsActions.ScrollDown, hasScrollDownAction);
        Action(SemanticsActions.Increase, hasIncreaseAction);
        Action(SemanticsActions.Decrease, hasDecreaseAction);
        Action(SemanticsActions.ShowOnScreen, hasShowOnScreenAction);
        Action(SemanticsActions.MoveCursorForwardByCharacter, hasMoveCursorForwardByCharacterAction);
        Action(SemanticsActions.MoveCursorBackwardByCharacter, hasMoveCursorBackwardByCharacterAction);
        Action(SemanticsActions.MoveCursorForwardByWord, hasMoveCursorForwardByWordAction);
        Action(SemanticsActions.MoveCursorBackwardByWord, hasMoveCursorBackwardByWordAction);
        Action(SemanticsActions.SetText, hasSetTextAction);
        Action(SemanticsActions.SetSelection, hasSetSelectionAction);
        Action(SemanticsActions.Copy, hasCopyAction);
        Action(SemanticsActions.Cut, hasCutAction);
        Action(SemanticsActions.Paste, hasPasteAction);
        Action(SemanticsActions.DidGainAccessibilityFocus, hasDidGainAccessibilityFocusAction);
        Action(SemanticsActions.DidLoseAccessibilityFocus, hasDidLoseAccessibilityFocusAction);
        Action(SemanticsActions.Dismiss, hasDismissAction);

        return new SemanticsMatcher
        {
            Identifier = identifier,
            Label = label,
            Hint = hint,
            Value = value,
            IncreasedValue = increasedValue,
            DecreasedValue = decreasedValue,
            Tooltip = tooltip,
            TextDirection = textDirection,
            Rect = rect,
            Size = size,
            PlatformViewId = platformViewId,
            MaxValueLength = maxValueLength,
            CurrentValueLength = currentValueLength,
            ValidationResult = validationResult,
            InputType = inputType,
            MaxValue = maxValue,
            MinValue = minValue,
            Role = role,
            Flags = flags,
            Actions = actions,
            Children = children,
        };
    }

    /// <summary>
    /// flutter_test's <c>isAccessibilityAnnouncement(message, textDirection:, assertiveness:, viewId:)</c>:
    /// the properties given must match.
    /// </summary>
    public static void ExpectAnnouncement(
        CapturedAccessibilityAnnouncement announcement,
        string message,
        TextDirection? textDirection = null,
        Assertiveness? assertiveness = null,
        int? viewId = null)
    {
        var failures = new StringBuilder();
        if (announcement.Message != message)
        {
            failures.Append($" message was \"{announcement.Message}\" (expected \"{message}\");");
        }

        if (textDirection is not null && announcement.TextDirection != textDirection)
        {
            failures.Append($" textDirection was {announcement.TextDirection};");
        }

        if (assertiveness is not null && announcement.Assertiveness != assertiveness)
        {
            failures.Append($" assertiveness was {announcement.Assertiveness};");
        }

        if (viewId is not null && announcement.ViewId != viewId)
        {
            failures.Append($" viewId was {announcement.ViewId};");
        }

        if (failures.Length > 0)
        {
            throw new XunitException("Announcement did not match:" + failures);
        }
    }
}
