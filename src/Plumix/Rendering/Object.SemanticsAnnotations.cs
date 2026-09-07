using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/object.dart (SemanticsAnnotationsMixin)

namespace Plumix.Rendering;

/// <summary>
/// The shared implementation of Dart's <c>SemanticsAnnotationsMixin</c>: it holds one
/// <see cref="SemanticsProperties"/> value object plus the six render-object level switches, and
/// writes them into a <see cref="SemanticsConfiguration"/>.
/// </summary>
/// <remarks>
/// C# has no mixins, and the two users derive from different bases
/// (<see cref="RenderProxyBox"/> and <see cref="RenderProxySliver"/>), so the mixin's state and
/// behavior live in this helper and each render object forwards to it. The
/// <c>_perform*</c> trampolines are members of this class exactly as they are members of the Dart
/// mixin: registering a stable delegate rather than the caller's closure is what lets a rebuild
/// swap one non-null handler for another without the node's action set changing.
/// </remarks>
internal sealed class SemanticsAnnotations
{
    private readonly Action _markNeedsSemanticsUpdate;
    private SemanticsProperties _properties;
    private bool _container;
    private bool _explicitChildNodes;
    private bool _excludeSemantics;
    private bool _blockUserActions;
    private TextDirection? _textDirection;
    private AttributedString? _attributedLabel;
    private AttributedString? _attributedValue;
    private AttributedString? _attributedIncreasedValue;
    private AttributedString? _attributedDecreasedValue;
    private AttributedString? _attributedHint;

    /// <remarks>Flutter's <c>SemanticsAnnotationsMixin.initSemanticsAnnotations</c>.</remarks>
    internal SemanticsAnnotations(
        Action markNeedsSemanticsUpdate,
        SemanticsProperties properties,
        bool container,
        bool explicitChildNodes,
        bool excludeSemantics,
        bool blockUserActions,
        TextDirection? textDirection)
    {
        ArgumentNullException.ThrowIfNull(markNeedsSemanticsUpdate);
        ArgumentNullException.ThrowIfNull(properties);
        _markNeedsSemanticsUpdate = markNeedsSemanticsUpdate;
        _properties = properties;
        _container = container;
        _explicitChildNodes = explicitChildNodes;
        _excludeSemantics = excludeSemantics;
        _blockUserActions = blockUserActions;
        _textDirection = textDirection;
        UpdateAttributedFields(properties);
    }

    /// <remarks>Flutter's <c>SemanticsAnnotationsMixin.properties</c>.</remarks>
    internal SemanticsProperties Properties
    {
        get => _properties;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            // Dart compares by identity: `SemanticsProperties` declares no `==`.
            if (ReferenceEquals(_properties, value))
            {
                return;
            }

            _properties = value;
            UpdateAttributedFields(value);
            _markNeedsSemanticsUpdate();
        }
    }

    /// <remarks>Flutter's <c>SemanticsAnnotationsMixin.container</c>.</remarks>
    internal bool Container
    {
        get => _container;
        set => Assign(ref _container, value);
    }

    /// <remarks>Flutter's <c>SemanticsAnnotationsMixin.explicitChildNodes</c>.</remarks>
    internal bool ExplicitChildNodes
    {
        get => _explicitChildNodes;
        set => Assign(ref _explicitChildNodes, value);
    }

    /// <remarks>Flutter's <c>SemanticsAnnotationsMixin.excludeSemantics</c>.</remarks>
    internal bool ExcludeSemantics
    {
        get => _excludeSemantics;
        set => Assign(ref _excludeSemantics, value);
    }

    /// <remarks>Flutter's <c>SemanticsAnnotationsMixin.blockUserActions</c>.</remarks>
    internal bool BlockUserActions
    {
        get => _blockUserActions;
        set => Assign(ref _blockUserActions, value);
    }

    /// <remarks>Flutter's <c>SemanticsAnnotationsMixin.textDirection</c>.</remarks>
    internal TextDirection? TextDirection
    {
        get => _textDirection;
        set
        {
            if (_textDirection == value)
            {
                return;
            }

            _textDirection = value;
            _markNeedsSemanticsUpdate();
        }
    }

    private void Assign(ref bool field, bool value)
    {
        if (field == value)
        {
            return;
        }

        field = value;
        _markNeedsSemanticsUpdate();
    }

    /// <remarks>Flutter's private <c>SemanticsAnnotationsMixin._updateAttributedFields</c>.</remarks>
    private void UpdateAttributedFields(SemanticsProperties value)
    {
        _attributedLabel = Resolve(value.AttributedLabel, value.Label);
        _attributedValue = Resolve(value.AttributedValue, value.Value);
        _attributedIncreasedValue = Resolve(value.AttributedIncreasedValue, value.IncreasedValue);
        _attributedDecreasedValue = Resolve(value.AttributedDecreasedValue, value.DecreasedValue);
        _attributedHint = Resolve(value.AttributedHint, value.Hint);
    }

    private static AttributedString? Resolve(AttributedString? attributed, string? plain) =>
        attributed ?? (plain is null ? null : new AttributedString(plain));

    /// <remarks>Flutter's <c>SemanticsAnnotationsMixin.describeSemanticsConfiguration</c>.</remarks>
    internal void DescribeSemanticsConfiguration(SemanticsConfiguration config)
    {
        config.IsSemanticBoundary = _container || _properties.Identifier is not null;
        config.ExplicitChildNodes = _explicitChildNodes;
        config.IsBlockingUserActions = _blockUserActions;

        if (_properties.ScopesRoute == true && !_explicitChildNodes)
        {
            throw new InvalidOperationException(
                "explicitChildNodes must be set to true if scopes route is true");
        }

        if (_properties.Toggled == true && _properties.Checked == true)
        {
            throw new InvalidOperationException(
                "A semantics node cannot be toggled and checked at the same time");
        }

        if (_properties.Enabled is not null)
        {
            config.IsEnabled = _properties.Enabled;
        }

        if (_properties.Checked is not null)
        {
            config.IsChecked = _properties.Checked;
        }

        if (_properties.Mixed is not null)
        {
            config.IsCheckStateMixed = _properties.Mixed;
        }

        if (_properties.Toggled is not null)
        {
            config.IsToggled = _properties.Toggled;
        }

        if (_properties.Selected is not null)
        {
            config.IsSelected = _properties.Selected.Value;
        }

        if (_properties.Button is not null)
        {
            config.IsButton = _properties.Button.Value;
        }

        if (_properties.Expanded is not null)
        {
            config.IsExpanded = _properties.Expanded;
        }

        if (_properties.Link is not null)
        {
            config.IsLink = _properties.Link.Value;
        }

        if (_properties.LinkUrl is not null)
        {
            config.LinkUrl = _properties.LinkUrl;
        }

        if (_properties.Slider is not null)
        {
            config.IsSlider = _properties.Slider.Value;
        }

        if (_properties.KeyboardKey is not null)
        {
            config.IsKeyboardKey = _properties.KeyboardKey.Value;
        }

        if (_properties.Header is not null)
        {
            config.IsHeader = _properties.Header.Value;
        }

        if (_properties.HeadingLevel is not null)
        {
            config.HeadingLevel = _properties.HeadingLevel.Value;
        }

        if (_properties.TextField is not null)
        {
            config.IsTextField = _properties.TextField.Value;
        }

        if (_properties.ReadOnly is not null)
        {
            config.IsReadOnly = _properties.ReadOnly.Value;
        }

        if (_properties.Focusable is not null)
        {
            config.IsFocusable = _properties.Focusable.Value;
        }

        if (_properties.Focused is not null)
        {
            config.IsFocused = _properties.Focused;
        }

        if (_properties.AccessibilityFocusBlockType is not null)
        {
            config.AccessibilityFocusBlockType = _properties.AccessibilityFocusBlockType.Value;
        }

        if (_properties.InMutuallyExclusiveGroup is not null)
        {
            config.IsInMutuallyExclusiveGroup = _properties.InMutuallyExclusiveGroup.Value;
        }

        if (_properties.Obscured is not null)
        {
            config.IsObscured = _properties.Obscured.Value;
        }

        if (_properties.Multiline is not null)
        {
            config.IsMultiline = _properties.Multiline.Value;
        }

        if (_properties.Hidden is not null)
        {
            config.IsHidden = _properties.Hidden.Value;
        }

        if (_properties.Image is not null)
        {
            config.IsImage = _properties.Image.Value;
        }

        if (_properties.IsRequired is not null)
        {
            config.IsRequired = _properties.IsRequired;
        }

        if (_properties.Identifier is not null)
        {
            config.Identifier = _properties.Identifier;
        }

        if (_properties.TraversalParentIdentifier is not null)
        {
            config.TraversalParentIdentifier = _properties.TraversalParentIdentifier;
        }

        if (_properties.TraversalChildIdentifier is not null)
        {
            config.TraversalChildIdentifier = _properties.TraversalChildIdentifier;
        }

        if (_attributedLabel is not null)
        {
            config.AttributedLabel = _attributedLabel;
        }

        if (_attributedValue is not null)
        {
            config.AttributedValue = _attributedValue;
        }

        if (_attributedIncreasedValue is not null)
        {
            config.AttributedIncreasedValue = _attributedIncreasedValue;
        }

        if (_attributedDecreasedValue is not null)
        {
            config.AttributedDecreasedValue = _attributedDecreasedValue;
        }

        if (_attributedHint is not null)
        {
            config.AttributedHint = _attributedHint;
        }

        if (_properties.Tooltip is not null)
        {
            config.Tooltip = _properties.Tooltip;
        }

        if (_properties.HintOverrides is { IsNotEmpty: true })
        {
            config.HintOverrides = _properties.HintOverrides;
        }

        if (_properties.ScopesRoute is not null)
        {
            config.ScopesRoute = _properties.ScopesRoute.Value;
        }

        if (_properties.NamesRoute is not null)
        {
            config.NamesRoute = _properties.NamesRoute.Value;
        }

        if (_properties.LiveRegion is not null)
        {
            config.LiveRegion = _properties.LiveRegion.Value;
        }

        if (_properties.MaxValueLength is not null)
        {
            config.MaxValueLength = _properties.MaxValueLength;
        }

        if (_properties.CurrentValueLength is not null)
        {
            config.CurrentValueLength = _properties.CurrentValueLength;
        }

        if (_textDirection is not null)
        {
            config.TextDirection = _textDirection;
        }

        if (_properties.SortKey is not null)
        {
            config.SortKey = _properties.SortKey;
        }

        if (_properties.TagForChildren is not null)
        {
            config.AddTagForChildren(_properties.TagForChildren);
        }

        if (_properties.Role is not null)
        {
            config.Role = _properties.Role.Value;
        }

        if (_properties.ControlsNodes is not null)
        {
            config.ControlsNodes = _properties.ControlsNodes;
        }

        if (config.ValidationResult != _properties.ValidationResult)
        {
            config.ValidationResult = _properties.ValidationResult;
        }

        if (_properties.HitTestBehavior is not null)
        {
            config.HitTestBehavior = _properties.HitTestBehavior.Value;
        }

        if (_properties.InputType is not null)
        {
            config.InputType = _properties.InputType.Value;
        }

        if (_properties.MinValue is not null)
        {
            config.MinValue = _properties.MinValue;
        }

        if (_properties.MaxValue is not null)
        {
            config.MaxValue = _properties.MaxValue;
        }

        if (_properties.OnTap is not null)
        {
            config.OnTap = PerformTap;
        }

        if (_properties.OnLongPress is not null)
        {
            config.OnLongPress = PerformLongPress;
        }

        if (_properties.OnDismiss is not null)
        {
            config.OnDismiss = PerformDismiss;
        }

        if (_properties.OnScrollLeft is not null)
        {
            config.OnScrollLeft = PerformScrollLeft;
        }

        if (_properties.OnScrollRight is not null)
        {
            config.OnScrollRight = PerformScrollRight;
        }

        if (_properties.OnScrollUp is not null)
        {
            config.OnScrollUp = PerformScrollUp;
        }

        if (_properties.OnScrollDown is not null)
        {
            config.OnScrollDown = PerformScrollDown;
        }

        if (_properties.OnIncrease is not null)
        {
            config.OnIncrease = PerformIncrease;
        }

        if (_properties.OnDecrease is not null)
        {
            config.OnDecrease = PerformDecrease;
        }

        if (_properties.OnCopy is not null)
        {
            config.OnCopy = PerformCopy;
        }

        if (_properties.OnCut is not null)
        {
            config.OnCut = PerformCut;
        }

        if (_properties.OnPaste is not null)
        {
            config.OnPaste = PerformPaste;
        }

        if (_properties.OnMoveCursorForwardByCharacter is not null)
        {
            config.OnMoveCursorForwardByCharacter = PerformMoveCursorForwardByCharacter;
        }

        if (_properties.OnMoveCursorBackwardByCharacter is not null)
        {
            config.OnMoveCursorBackwardByCharacter = PerformMoveCursorBackwardByCharacter;
        }

        if (_properties.OnMoveCursorForwardByWord is not null)
        {
            config.OnMoveCursorForwardByWord = PerformMoveCursorForwardByWord;
        }

        if (_properties.OnMoveCursorBackwardByWord is not null)
        {
            config.OnMoveCursorBackwardByWord = PerformMoveCursorBackwardByWord;
        }

        if (_properties.OnSetSelection is not null)
        {
            config.OnSetSelection = PerformSetSelection;
        }

        if (_properties.OnSetText is not null)
        {
            config.OnSetText = PerformSetText;
        }

        if (_properties.OnDidGainAccessibilityFocus is not null)
        {
            config.OnDidGainAccessibilityFocus = PerformDidGainAccessibilityFocus;
        }

        if (_properties.OnDidLoseAccessibilityFocus is not null)
        {
            config.OnDidLoseAccessibilityFocus = PerformDidLoseAccessibilityFocus;
        }

        if (_properties.OnFocus is not null)
        {
            config.OnFocus = PerformFocus;
        }

        if (_properties.OnExpand is not null)
        {
            config.OnExpand = PerformExpand;
        }

        if (_properties.OnCollapse is not null)
        {
            config.OnCollapse = PerformCollapse;
        }

        if (_properties.CustomSemanticsActions is not null)
        {
            foreach (KeyValuePair<CustomSemanticsAction, Action> pair in _properties.CustomSemanticsActions)
            {
                config.AddCustomActionHandler(pair.Key, pair.Value);
            }
        }
    }

    private void PerformTap() => _properties.OnTap?.Invoke();

    private void PerformLongPress() => _properties.OnLongPress?.Invoke();

    private void PerformDismiss() => _properties.OnDismiss?.Invoke();

    private void PerformScrollLeft() => _properties.OnScrollLeft?.Invoke();

    private void PerformScrollRight() => _properties.OnScrollRight?.Invoke();

    private void PerformScrollUp() => _properties.OnScrollUp?.Invoke();

    private void PerformScrollDown() => _properties.OnScrollDown?.Invoke();

    private void PerformIncrease() => _properties.OnIncrease?.Invoke();

    private void PerformDecrease() => _properties.OnDecrease?.Invoke();

    private void PerformCopy() => _properties.OnCopy?.Invoke();

    private void PerformCut() => _properties.OnCut?.Invoke();

    private void PerformPaste() => _properties.OnPaste?.Invoke();

    private void PerformMoveCursorForwardByCharacter(bool extendSelection) =>
        _properties.OnMoveCursorForwardByCharacter?.Invoke(extendSelection);

    private void PerformMoveCursorBackwardByCharacter(bool extendSelection) =>
        _properties.OnMoveCursorBackwardByCharacter?.Invoke(extendSelection);

    private void PerformMoveCursorForwardByWord(bool extendSelection) =>
        _properties.OnMoveCursorForwardByWord?.Invoke(extendSelection);

    private void PerformMoveCursorBackwardByWord(bool extendSelection) =>
        _properties.OnMoveCursorBackwardByWord?.Invoke(extendSelection);

    private void PerformSetSelection(Widgets.TextSelection selection) =>
        _properties.OnSetSelection?.Invoke(selection);

    private void PerformSetText(string text) => _properties.OnSetText?.Invoke(text);

    private void PerformDidGainAccessibilityFocus() => _properties.OnDidGainAccessibilityFocus?.Invoke();

    private void PerformDidLoseAccessibilityFocus() => _properties.OnDidLoseAccessibilityFocus?.Invoke();

    private void PerformFocus() => _properties.OnFocus?.Invoke();

    private void PerformExpand() => _properties.OnExpand?.Invoke();

    private void PerformCollapse() => _properties.OnCollapse?.Invoke();
}
