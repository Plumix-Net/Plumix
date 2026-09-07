using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/semantics/semantics.dart

namespace Plumix.Rendering;

/// <summary>
/// The outcome of validating a form field, as reported to assistive technologies.
/// </summary>
/// <remarks>Flutter's <c>SemanticsValidationResult</c>.</remarks>
public enum SemanticsValidationResult
{
    /// <summary>No validation information is available for this node.</summary>
    None,

    /// <summary>The node's content passed validation.</summary>
    Valid,

    /// <summary>The node's content failed validation.</summary>
    Invalid,
}

/// <summary>Signature for a semantics handler that moves the text cursor.</summary>
/// <remarks>Flutter's <c>MoveCursorHandler</c>.</remarks>
public delegate void MoveCursorHandler(bool extendSelection);

/// <summary>Signature for a semantics handler that changes the text selection.</summary>
/// <remarks>Flutter's <c>SetSelectionHandler</c>.</remarks>
public delegate void SetSelectionHandler(TextSelection selection);

/// <summary>Signature for a semantics handler that replaces the node's text.</summary>
/// <remarks>Flutter's <c>SetTextHandler</c>.</remarks>
public delegate void SetTextHandler(string text);

/// <summary>
/// Hints an assistive technology announces for the standard tap and long-press actions instead of
/// its own wording ("double tap to activate").
/// </summary>
/// <remarks>Flutter's <c>SemanticsHintOverrides</c>.</remarks>
public sealed class SemanticsHintOverrides : DiagnosticableTree, IEquatable<SemanticsHintOverrides>
{
    public SemanticsHintOverrides(string? onTapHint = null, string? onLongPressHint = null)
    {
        if (onTapHint is not null && onTapHint.Length == 0)
        {
            throw new ArgumentException("A tap hint override cannot be the empty string.", nameof(onTapHint));
        }

        if (onLongPressHint is not null && onLongPressHint.Length == 0)
        {
            throw new ArgumentException(
                "A long-press hint override cannot be the empty string.",
                nameof(onLongPressHint));
        }

        OnTapHint = onTapHint;
        OnLongPressHint = onLongPressHint;
    }

    /// <summary>The hint announced for <see cref="SemanticsActions.Tap"/>.</summary>
    public string? OnTapHint { get; }

    /// <summary>The hint announced for <see cref="SemanticsActions.LongPress"/>.</summary>
    public string? OnLongPressHint { get; }

    /// <summary>Whether either hint is set.</summary>
    public bool IsNotEmpty => OnTapHint is not null || OnLongPressHint is not null;

    /// <inheritdoc />
    public bool Equals(SemanticsHintOverrides? other) =>
        other is not null && other.OnTapHint == OnTapHint && other.OnLongPressHint == OnLongPressHint;

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as SemanticsHintOverrides);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(OnTapHint, OnLongPressHint);

    public static bool operator ==(SemanticsHintOverrides? left, SemanticsHintOverrides? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(SemanticsHintOverrides? left, SemanticsHintOverrides? right) =>
        !(left == right);

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new StringProperty("onTapHint", OnTapHint, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new StringProperty(
            "onLongPressHint",
            OnLongPressHint,
            defaultValue: DiagnosticsDefaults.NullValue));
    }
}

/// <summary>
/// The complete set of semantic annotations one <see cref="Widgets.Semantics"/> widget contributes
/// to the semantics tree.
/// </summary>
/// <remarks>
/// Flutter's <c>SemanticsProperties</c>. It is a value object with no equality of its own: the
/// render object's <see cref="RenderSemanticsAnnotations.Properties"/> setter compares by reference,
/// exactly as Dart does, which is why the widget layer rebuilds one on every update.
/// </remarks>
public sealed class SemanticsProperties : DiagnosticableTree
{
    public SemanticsProperties(
        bool? enabled = null,
        bool? @checked = null,
        bool? mixed = null,
        bool? expanded = null,
        bool? toggled = null,
        bool? selected = null,
        bool? button = null,
        bool? link = null,
        bool? header = null,
        bool? textField = null,
        bool? slider = null,
        bool? keyboardKey = null,
        bool? readOnly = null,
        bool? focusable = null,
        bool? focused = null,
        AccessibilityFocusBlockType? accessibilityFocusBlockType = null,
        bool? inMutuallyExclusiveGroup = null,
        bool? hidden = null,
        bool? obscured = null,
        bool? multiline = null,
        bool? scopesRoute = null,
        bool? namesRoute = null,
        bool? image = null,
        bool? liveRegion = null,
        bool? isRequired = null,
        int? maxValueLength = null,
        int? currentValueLength = null,
        string? identifier = null,
        object? traversalParentIdentifier = null,
        object? traversalChildIdentifier = null,
        string? label = null,
        AttributedString? attributedLabel = null,
        string? value = null,
        AttributedString? attributedValue = null,
        string? increasedValue = null,
        AttributedString? attributedIncreasedValue = null,
        string? decreasedValue = null,
        AttributedString? attributedDecreasedValue = null,
        string? hint = null,
        AttributedString? attributedHint = null,
        string? tooltip = null,
        int? headingLevel = null,
        SemanticsHintOverrides? hintOverrides = null,
        TextDirection? textDirection = null,
        SemanticsSortKey? sortKey = null,
        SemanticsTag? tagForChildren = null,
        Uri? linkUrl = null,
        Action? onTap = null,
        Action? onLongPress = null,
        Action? onScrollLeft = null,
        Action? onScrollRight = null,
        Action? onScrollUp = null,
        Action? onScrollDown = null,
        Action? onIncrease = null,
        Action? onDecrease = null,
        Action? onCopy = null,
        Action? onCut = null,
        Action? onPaste = null,
        MoveCursorHandler? onMoveCursorForwardByCharacter = null,
        MoveCursorHandler? onMoveCursorBackwardByCharacter = null,
        MoveCursorHandler? onMoveCursorForwardByWord = null,
        MoveCursorHandler? onMoveCursorBackwardByWord = null,
        SetSelectionHandler? onSetSelection = null,
        SetTextHandler? onSetText = null,
        Action? onDidGainAccessibilityFocus = null,
        Action? onDidLoseAccessibilityFocus = null,
        Action? onFocus = null,
        Action? onDismiss = null,
        Action? onExpand = null,
        Action? onCollapse = null,
        IReadOnlyDictionary<CustomSemanticsAction, Action>? customSemanticsActions = null,
        SemanticsRole? role = null,
        IReadOnlySet<string>? controlsNodes = null,
        SemanticsValidationResult validationResult = SemanticsValidationResult.None,
        SemanticsHitTestBehavior? hitTestBehavior = null,
        SemanticsInputType? inputType = null,
        string? maxValue = null,
        string? minValue = null)
    {
        if (label is not null && attributedLabel is not null)
        {
            throw new ArgumentException("Only one of label or attributedLabel should be provided", nameof(label));
        }

        if (value is not null && attributedValue is not null)
        {
            throw new ArgumentException("Only one of value or attributedValue should be provided", nameof(value));
        }

        if (increasedValue is not null && attributedIncreasedValue is not null)
        {
            throw new ArgumentException(
                "Only one of increasedValue or attributedIncreasedValue should be provided",
                nameof(increasedValue));
        }

        if (decreasedValue is not null && attributedDecreasedValue is not null)
        {
            throw new ArgumentException(
                "Only one of decreasedValue or attributedDecreasedValue should be provided",
                nameof(decreasedValue));
        }

        if (hint is not null && attributedHint is not null)
        {
            throw new ArgumentException("Only one of hint or attributedHint should be provided", nameof(hint));
        }

        if (headingLevel is not null && (headingLevel <= 0 || headingLevel > 6))
        {
            throw new ArgumentOutOfRangeException(nameof(headingLevel), "Heading level must be between 1 and 6");
        }

        if (linkUrl is not null && link != true)
        {
            throw new ArgumentException("If linkUrl is set then link must be true", nameof(linkUrl));
        }

        Enabled = enabled;
        Checked = @checked;
        Mixed = mixed;
        Expanded = expanded;
        Toggled = toggled;
        Selected = selected;
        Button = button;
        Link = link;
        Header = header;
        TextField = textField;
        Slider = slider;
        KeyboardKey = keyboardKey;
        ReadOnly = readOnly;
        Focusable = focusable;
        Focused = focused;
        AccessibilityFocusBlockType = accessibilityFocusBlockType;
        InMutuallyExclusiveGroup = inMutuallyExclusiveGroup;
        Hidden = hidden;
        Obscured = obscured;
        Multiline = multiline;
        ScopesRoute = scopesRoute;
        NamesRoute = namesRoute;
        Image = image;
        LiveRegion = liveRegion;
        IsRequired = isRequired;
        MaxValueLength = maxValueLength;
        CurrentValueLength = currentValueLength;
        Identifier = identifier;
        TraversalParentIdentifier = traversalParentIdentifier;
        TraversalChildIdentifier = traversalChildIdentifier;
        Label = label;
        AttributedLabel = attributedLabel;
        Value = value;
        AttributedValue = attributedValue;
        IncreasedValue = increasedValue;
        AttributedIncreasedValue = attributedIncreasedValue;
        DecreasedValue = decreasedValue;
        AttributedDecreasedValue = attributedDecreasedValue;
        Hint = hint;
        AttributedHint = attributedHint;
        Tooltip = tooltip;
        HeadingLevel = headingLevel;
        HintOverrides = hintOverrides;
        TextDirection = textDirection;
        SortKey = sortKey;
        TagForChildren = tagForChildren;
        LinkUrl = linkUrl;
        OnTap = onTap;
        OnLongPress = onLongPress;
        OnScrollLeft = onScrollLeft;
        OnScrollRight = onScrollRight;
        OnScrollUp = onScrollUp;
        OnScrollDown = onScrollDown;
        OnIncrease = onIncrease;
        OnDecrease = onDecrease;
        OnCopy = onCopy;
        OnCut = onCut;
        OnPaste = onPaste;
        OnMoveCursorForwardByCharacter = onMoveCursorForwardByCharacter;
        OnMoveCursorBackwardByCharacter = onMoveCursorBackwardByCharacter;
        OnMoveCursorForwardByWord = onMoveCursorForwardByWord;
        OnMoveCursorBackwardByWord = onMoveCursorBackwardByWord;
        OnSetSelection = onSetSelection;
        OnSetText = onSetText;
        OnDidGainAccessibilityFocus = onDidGainAccessibilityFocus;
        OnDidLoseAccessibilityFocus = onDidLoseAccessibilityFocus;
        OnFocus = onFocus;
        OnDismiss = onDismiss;
        OnExpand = onExpand;
        OnCollapse = onCollapse;
        CustomSemanticsActions = customSemanticsActions;
        Role = role;
        ControlsNodes = controlsNodes;
        ValidationResult = validationResult;
        HitTestBehavior = hitTestBehavior;
        InputType = inputType;
        MaxValue = maxValue;
        MinValue = minValue;
    }

    /// <summary>Whether the node is enabled, or <c>null</c> when it has no enabled/disabled state.</summary>
    public bool? Enabled { get; }

    /// <summary>Whether the node is checked; mutually exclusive with <see cref="Toggled"/>.</summary>
    public bool? Checked { get; }

    /// <summary>Whether a checkbox-like node is in its indeterminate state.</summary>
    public bool? Mixed { get; }

    /// <summary>Whether an expandable node is currently expanded.</summary>
    public bool? Expanded { get; }

    /// <summary>Whether a switch-like node is on; mutually exclusive with <see cref="Checked"/>.</summary>
    public bool? Toggled { get; }

    /// <summary>Whether the node is selected.</summary>
    public bool? Selected { get; }

    /// <summary>Whether the node is a button.</summary>
    public bool? Button { get; }

    /// <summary>Whether the node is a link.</summary>
    public bool? Link { get; }

    /// <summary>Whether the node is a header for other content.</summary>
    public bool? Header { get; }

    /// <summary>Whether the node is an editable text field.</summary>
    public bool? TextField { get; }

    /// <summary>Whether the node is a slider.</summary>
    public bool? Slider { get; }

    /// <summary>Whether the node is a keyboard key.</summary>
    public bool? KeyboardKey { get; }

    /// <summary>Whether an editable node rejects edits.</summary>
    public bool? ReadOnly { get; }

    /// <summary>Whether the node can hold input focus.</summary>
    /// <remarks>
    /// Flutter deprecated <c>SemanticsProperties.focusable</c> in favour of <see cref="Focused"/>,
    /// which implies it; the field is kept because Flutter still honours it.
    /// </remarks>
    public bool? Focusable { get; }

    /// <summary>Whether the node currently holds input focus.</summary>
    public bool? Focused { get; }

    /// <summary>Whether accessibility focus is blocked for the node, its subtree, or neither.</summary>
    public AccessibilityFocusBlockType? AccessibilityFocusBlockType { get; }

    /// <summary>Whether the node belongs to a group where only one member may be selected.</summary>
    public bool? InMutuallyExclusiveGroup { get; }

    /// <summary>Whether the node is currently off-screen but still in the tree.</summary>
    public bool? Hidden { get; }

    /// <summary>Whether the node's value is obscured, as in a password field.</summary>
    public bool? Obscured { get; }

    /// <summary>Whether the node's value spans several lines.</summary>
    public bool? Multiline { get; }

    /// <summary>Whether the node introduces a route scope; requires explicit child nodes.</summary>
    public bool? ScopesRoute { get; }

    /// <summary>Whether the node's label names the route it is inside.</summary>
    public bool? NamesRoute { get; }

    /// <summary>Whether the node is an image.</summary>
    public bool? Image { get; }

    /// <summary>Whether changes to the node's content should be announced.</summary>
    public bool? LiveRegion { get; }

    /// <summary>Whether the node must be filled in before its form can be submitted.</summary>
    public bool? IsRequired { get; }

    /// <summary>The maximum number of characters the node's value accepts.</summary>
    public int? MaxValueLength { get; }

    /// <summary>The number of characters the node's value currently holds.</summary>
    public int? CurrentValueLength { get; }

    /// <summary>
    /// A stable identifier for the node, used by UI testing frameworks rather than announced to the
    /// user. Setting it forces the annotation to introduce its own semantics node.
    /// </summary>
    public string? Identifier { get; }

    /// <summary>Identifies this node as the traversal parent other nodes graft onto.</summary>
    public object? TraversalParentIdentifier { get; }

    /// <summary>Names the traversal parent this subtree is traversed under.</summary>
    public object? TraversalChildIdentifier { get; }

    /// <summary>The node's label; mutually exclusive with <see cref="AttributedLabel"/>.</summary>
    public string? Label { get; }

    /// <summary>The node's label with string attributes.</summary>
    public AttributedString? AttributedLabel { get; }

    /// <summary>The node's value; mutually exclusive with <see cref="AttributedValue"/>.</summary>
    public string? Value { get; }

    /// <summary>The node's value with string attributes.</summary>
    public AttributedString? AttributedValue { get; }

    /// <summary>The value the node will read after <see cref="OnIncrease"/> runs.</summary>
    public string? IncreasedValue { get; }

    /// <summary>The increased value with string attributes.</summary>
    public AttributedString? AttributedIncreasedValue { get; }

    /// <summary>The value the node will read after <see cref="OnDecrease"/> runs.</summary>
    public string? DecreasedValue { get; }

    /// <summary>The decreased value with string attributes.</summary>
    public AttributedString? AttributedDecreasedValue { get; }

    /// <summary>A brief description of what happens when the node is activated.</summary>
    public string? Hint { get; }

    /// <summary>The hint with string attributes.</summary>
    public AttributedString? AttributedHint { get; }

    /// <summary>The node's tooltip.</summary>
    public string? Tooltip { get; }

    /// <summary>The heading level, from 1 to 6, when the node is a heading.</summary>
    public int? HeadingLevel { get; }

    /// <summary>Replacement wording for the standard tap and long-press hints.</summary>
    public SemanticsHintOverrides? HintOverrides { get; }

    /// <summary>The reading direction for this subtree's semantic strings.</summary>
    public TextDirection? TextDirection { get; }

    /// <summary>The sort key that orders this node among its siblings during traversal.</summary>
    public SemanticsSortKey? SortKey { get; }

    /// <summary>The tag attached to every semantics node created below this annotation.</summary>
    public SemanticsTag? TagForChildren { get; }

    /// <summary>The URL a link node navigates to; requires <see cref="Link"/> to be <c>true</c>.</summary>
    public Uri? LinkUrl { get; }

    /// <summary>Handler for <see cref="SemanticsActions.Tap"/>.</summary>
    public Action? OnTap { get; }

    /// <summary>Handler for <see cref="SemanticsActions.LongPress"/>.</summary>
    public Action? OnLongPress { get; }

    /// <summary>Handler for <see cref="SemanticsActions.ScrollLeft"/>.</summary>
    public Action? OnScrollLeft { get; }

    /// <summary>Handler for <see cref="SemanticsActions.ScrollRight"/>.</summary>
    public Action? OnScrollRight { get; }

    /// <summary>Handler for <see cref="SemanticsActions.ScrollUp"/>.</summary>
    public Action? OnScrollUp { get; }

    /// <summary>Handler for <see cref="SemanticsActions.ScrollDown"/>.</summary>
    public Action? OnScrollDown { get; }

    /// <summary>Handler for <see cref="SemanticsActions.Increase"/>.</summary>
    public Action? OnIncrease { get; }

    /// <summary>Handler for <see cref="SemanticsActions.Decrease"/>.</summary>
    public Action? OnDecrease { get; }

    /// <summary>Handler for <see cref="SemanticsActions.Copy"/>.</summary>
    public Action? OnCopy { get; }

    /// <summary>Handler for <see cref="SemanticsActions.Cut"/>.</summary>
    public Action? OnCut { get; }

    /// <summary>Handler for <see cref="SemanticsActions.Paste"/>.</summary>
    public Action? OnPaste { get; }

    /// <summary>Handler for <see cref="SemanticsActions.MoveCursorForwardByCharacter"/>.</summary>
    public MoveCursorHandler? OnMoveCursorForwardByCharacter { get; }

    /// <summary>Handler for <see cref="SemanticsActions.MoveCursorBackwardByCharacter"/>.</summary>
    public MoveCursorHandler? OnMoveCursorBackwardByCharacter { get; }

    /// <summary>Handler for <see cref="SemanticsActions.MoveCursorForwardByWord"/>.</summary>
    public MoveCursorHandler? OnMoveCursorForwardByWord { get; }

    /// <summary>Handler for <see cref="SemanticsActions.MoveCursorBackwardByWord"/>.</summary>
    public MoveCursorHandler? OnMoveCursorBackwardByWord { get; }

    /// <summary>Handler for <see cref="SemanticsActions.SetSelection"/>.</summary>
    public SetSelectionHandler? OnSetSelection { get; }

    /// <summary>Handler for <see cref="SemanticsActions.SetText"/>.</summary>
    public SetTextHandler? OnSetText { get; }

    /// <summary>Handler for <see cref="SemanticsActions.DidGainAccessibilityFocus"/>.</summary>
    public Action? OnDidGainAccessibilityFocus { get; }

    /// <summary>Handler for <see cref="SemanticsActions.DidLoseAccessibilityFocus"/>.</summary>
    public Action? OnDidLoseAccessibilityFocus { get; }

    /// <summary>Handler for <see cref="SemanticsActions.Focus"/>.</summary>
    public Action? OnFocus { get; }

    /// <summary>Handler for <see cref="SemanticsActions.Dismiss"/>.</summary>
    public Action? OnDismiss { get; }

    /// <summary>Handler for <see cref="SemanticsActions.Expand"/>.</summary>
    public Action? OnExpand { get; }

    /// <summary>Handler for <see cref="SemanticsActions.Collapse"/>.</summary>
    public Action? OnCollapse { get; }

    /// <summary>Extra actions the node exposes beyond the standard ones.</summary>
    public IReadOnlyDictionary<CustomSemanticsAction, Action>? CustomSemanticsActions { get; }

    /// <summary>The node's role, or <c>null</c> for <see cref="SemanticsRole.None"/>.</summary>
    public SemanticsRole? Role { get; }

    /// <summary>The <see cref="Identifier"/>s of the nodes whose visibility this node controls.</summary>
    public IReadOnlySet<string>? ControlsNodes { get; }

    /// <summary>The node's form-validation outcome.</summary>
    public SemanticsValidationResult ValidationResult { get; }

    /// <summary>How the node participates in accessibility hit testing.</summary>
    public SemanticsHitTestBehavior? HitTestBehavior { get; }

    /// <summary>The kind of input the node accepts.</summary>
    public SemanticsInputType? InputType { get; }

    /// <summary>The maximum value of a range node.</summary>
    public string? MaxValue { get; }

    /// <summary>The minimum value of a range node.</summary>
    public string? MinValue { get; }

    /// <inheritdoc />
    public override string ToStringShort() => nameof(SemanticsProperties);

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        object? nullDefault = DiagnosticsDefaults.NullValue;
        properties.Add(new DiagnosticsProperty<bool?>("checked", Checked, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<bool?>("mixed", Mixed, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<bool?>("expanded", Expanded, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<bool?>("selected", Selected, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<bool?>("isRequired", IsRequired, defaultValue: nullDefault));
        properties.Add(new StringProperty("identifier", Identifier, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<object>(
            "traversalParentIdentifier",
            TraversalParentIdentifier,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<object>(
            "traversalChildIdentifier",
            TraversalChildIdentifier,
            defaultValue: nullDefault));
        properties.Add(new StringProperty("label", Label, defaultValue: nullDefault));
        properties.Add(new AttributedStringProperty("attributedLabel", AttributedLabel, defaultValue: nullDefault));
        properties.Add(new StringProperty("value", Value, defaultValue: nullDefault));
        properties.Add(new AttributedStringProperty("attributedValue", AttributedValue, defaultValue: nullDefault));
        properties.Add(new StringProperty("increasedValue", IncreasedValue, defaultValue: nullDefault));
        properties.Add(new AttributedStringProperty(
            "attributedIncreasedValue",
            AttributedIncreasedValue,
            defaultValue: nullDefault));
        properties.Add(new StringProperty("decreasedValue", DecreasedValue, defaultValue: nullDefault));
        properties.Add(new AttributedStringProperty(
            "attributedDecreasedValue",
            AttributedDecreasedValue,
            defaultValue: nullDefault));
        properties.Add(new StringProperty("hint", Hint, defaultValue: nullDefault));
        properties.Add(new AttributedStringProperty("attributedHint", AttributedHint, defaultValue: nullDefault));
        properties.Add(new StringProperty("tooltip", Tooltip, defaultValue: nullDefault));
        properties.Add(new EnumProperty<TextDirection>("textDirection", TextDirection, defaultValue: nullDefault));
        properties.Add(new EnumProperty<SemanticsRole>("role", Role, defaultValue: nullDefault));
        properties.Add(new EnumProperty<SemanticsValidationResult>(
            "validationResult",
            ValidationResult,
            defaultValue: SemanticsValidationResult.None));
        properties.Add(new DiagnosticsProperty<SemanticsSortKey>("sortKey", SortKey, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<SemanticsHintOverrides>(
            "hintOverrides",
            HintOverrides,
            defaultValue: nullDefault));
    }
}
