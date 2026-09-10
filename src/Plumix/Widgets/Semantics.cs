using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart (Semantics, SliverSemantics)

namespace Plumix.Widgets;

public enum SemanticsRole
{
    None,
    Dialog,
    AlertDialog,
    Menu,
    MenuItem,
    MenuItemCheckbox,
    TabBar,
    Tab,
    TabPanel,
    Form,
    Cell,
    RadioGroup,
    ProgressBar,
    LoadingSpinner,
    Table,
    Row,
    ColumnHeader,
}

/// <summary>
/// The shared base of <see cref="Semantics"/> and <see cref="SliverSemantics"/>: it carries the
/// <see cref="SemanticsProperties"/> and the four render-object switches, and resolves the reading
/// direction from the ambient <see cref="Directionality"/> when the properties do not name one.
/// </summary>
/// <remarks>Flutter's private <c>_SemanticsBase</c>.</remarks>
public abstract class SemanticsBase : SingleChildRenderObjectWidget
{
    /// <remarks>Flutter's <c>_SemanticsBase.fromProperties</c>.</remarks>
    protected SemanticsBase(
        SemanticsProperties properties,
        Widget? child = null,
        bool container = false,
        bool explicitChildNodes = false,
        bool excludeSemantics = false,
        bool blockUserActions = false,
        Locale? localeForSubtree = null,
        Key? key = null) : base(child, key)
    {
        ArgumentNullException.ThrowIfNull(properties);
        Properties = properties;
        Container = container;
        ExplicitChildNodes = explicitChildNodes;
        ExcludeSemantics = excludeSemantics;
        BlockUserActions = blockUserActions;
        LocaleForSubtree = localeForSubtree;
    }

    /// <summary>All the annotations this widget contributes to the semantics tree.</summary>
    public SemanticsProperties Properties { get; }

    /// <summary>Whether this widget introduces a semantics node of its own.</summary>
    public bool Container { get; }

    /// <summary>Whether the descendants must each produce their own semantics node.</summary>
    public bool ExplicitChildNodes { get; }

    /// <summary>Whether to drop all the semantics of the descendants.</summary>
    public bool ExcludeSemantics { get; }

    /// <summary>Whether to block user interactions for the descendant semantics nodes.</summary>
    public bool BlockUserActions { get; }

    /// <summary>The locale for the widgets in this subtree.</summary>
    /// <remarks>
    /// Flutter's <c>_SemanticsBase.localeForSubtree</c>. When <c>null</c> the subtree inherits the
    /// locale of the nearest ancestor that names one.
    /// </remarks>
    public Locale? LocaleForSubtree { get; }

    /// <remarks>Flutter's private <c>_SemanticsBase._getTextDirection</c>.</remarks>
    private protected TextDirection? GetTextDirection(BuildContext context)
    {
        if (Properties.TextDirection is not null)
        {
            return Properties.TextDirection;
        }

        bool containsText =
            Properties.AttributedLabel is not null
            || Properties.Label is not null
            || Properties.Value is not null
            || Properties.AttributedValue is not null
            || Properties.IncreasedValue is not null
            || Properties.AttributedIncreasedValue is not null
            || Properties.DecreasedValue is not null
            || Properties.AttributedDecreasedValue is not null
            || Properties.Hint is not null
            || Properties.AttributedHint is not null
            || Properties.Tooltip is not null;

        return containsText ? Directionality.MaybeOf(context) : null;
    }
}

/// <summary>
/// Annotates its box child's subtree with the semantics described by <see cref="SemanticsProperties"/>.
/// </summary>
/// <remarks>Flutter's <c>Semantics</c>.</remarks>
public sealed class Semantics : SemanticsBase
{
    /// <summary>
    /// Builds a <see cref="SemanticsProperties"/> out of the individual annotations and annotates
    /// <paramref name="child"/> with it.
    /// </summary>
    public Semantics(
        Widget? child = null,
        bool container = false,
        bool explicitChildNodes = false,
        bool excludeSemantics = false,
        bool blockUserActions = false,
        bool? enabled = null,
        bool? @checked = null,
        bool? mixed = null,
        bool? selected = null,
        bool? toggled = null,
        bool? button = null,
        bool? slider = null,
        bool? keyboardKey = null,
        bool? link = null,
        Uri? linkUrl = null,
        bool? header = null,
        int? headingLevel = null,
        bool? textField = null,
        bool? readOnly = null,
        bool? focusable = null,
        bool? focused = null,
        AccessibilityFocusBlockType? accessibilityFocusBlockType = null,
        bool? inMutuallyExclusiveGroup = null,
        bool? obscured = null,
        bool? multiline = null,
        bool? scopesRoute = null,
        bool? namesRoute = null,
        bool? hidden = null,
        bool? image = null,
        bool? liveRegion = null,
        bool? expanded = null,
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
        string? onTapHint = null,
        string? onLongPressHint = null,
        TextDirection? textDirection = null,
        SemanticsSortKey? sortKey = null,
        SemanticsTag? tagForChildren = null,
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
        Action? onDismiss = null,
        MoveCursorHandler? onMoveCursorForwardByCharacter = null,
        MoveCursorHandler? onMoveCursorBackwardByCharacter = null,
        SetSelectionHandler? onSetSelection = null,
        SetTextHandler? onSetText = null,
        Action? onDidGainAccessibilityFocus = null,
        Action? onDidLoseAccessibilityFocus = null,
        Action? onFocus = null,
        Action? onExpand = null,
        Action? onCollapse = null,
        IReadOnlyDictionary<CustomSemanticsAction, Action>? customSemanticsActions = null,
        SemanticsRole? role = null,
        IReadOnlySet<string>? controlsNodes = null,
        SemanticsValidationResult validationResult = SemanticsValidationResult.None,
        SemanticsHitTestBehavior? hitTestBehavior = null,
        SemanticsInputType? inputType = null,
        string? minValue = null,
        string? maxValue = null,
        Locale? localeForSubtree = null,
        Key? key = null)
        : this(
            properties: new SemanticsProperties(
                enabled: enabled,
                @checked: @checked,
                mixed: mixed,
                expanded: expanded,
                toggled: toggled,
                selected: selected,
                button: button,
                link: link,
                header: header,
                textField: textField,
                slider: slider,
                keyboardKey: keyboardKey,
                readOnly: readOnly,
                focusable: focusable,
                focused: focused,
                accessibilityFocusBlockType: accessibilityFocusBlockType,
                inMutuallyExclusiveGroup: inMutuallyExclusiveGroup,
                hidden: hidden,
                obscured: obscured,
                multiline: multiline,
                scopesRoute: scopesRoute,
                namesRoute: namesRoute,
                image: image,
                liveRegion: liveRegion,
                isRequired: isRequired,
                maxValueLength: maxValueLength,
                currentValueLength: currentValueLength,
                identifier: identifier,
                traversalParentIdentifier: traversalParentIdentifier,
                traversalChildIdentifier: traversalChildIdentifier,
                label: label,
                attributedLabel: attributedLabel,
                value: value,
                attributedValue: attributedValue,
                increasedValue: increasedValue,
                attributedIncreasedValue: attributedIncreasedValue,
                decreasedValue: decreasedValue,
                attributedDecreasedValue: attributedDecreasedValue,
                hint: hint,
                attributedHint: attributedHint,
                tooltip: tooltip,
                headingLevel: headingLevel,
                hintOverrides: onTapHint is not null || onLongPressHint is not null
                    ? new SemanticsHintOverrides(onTapHint: onTapHint, onLongPressHint: onLongPressHint)
                    : null,
                textDirection: textDirection,
                sortKey: sortKey,
                tagForChildren: tagForChildren,
                linkUrl: linkUrl,
                onTap: onTap,
                onLongPress: onLongPress,
                onScrollLeft: onScrollLeft,
                onScrollRight: onScrollRight,
                onScrollUp: onScrollUp,
                onScrollDown: onScrollDown,
                onIncrease: onIncrease,
                onDecrease: onDecrease,
                onCopy: onCopy,
                onCut: onCut,
                onPaste: onPaste,
                onMoveCursorForwardByCharacter: onMoveCursorForwardByCharacter,
                onMoveCursorBackwardByCharacter: onMoveCursorBackwardByCharacter,
                onSetSelection: onSetSelection,
                onSetText: onSetText,
                onDidGainAccessibilityFocus: onDidGainAccessibilityFocus,
                onDidLoseAccessibilityFocus: onDidLoseAccessibilityFocus,
                onFocus: onFocus,
                onDismiss: onDismiss,
                onExpand: onExpand,
                onCollapse: onCollapse,
                customSemanticsActions: customSemanticsActions,
                role: role,
                controlsNodes: controlsNodes,
                validationResult: validationResult,
                hitTestBehavior: hitTestBehavior,
                inputType: inputType,
                maxValue: maxValue,
                minValue: minValue),
            child: child,
            container: container,
            explicitChildNodes: explicitChildNodes,
            excludeSemantics: excludeSemantics,
            blockUserActions: blockUserActions,
            localeForSubtree: localeForSubtree,
            key: key)
    {
    }

    /// <summary>Annotates <paramref name="child"/> with an already built value object.</summary>
    /// <remarks>Flutter's <c>Semantics.fromProperties</c>.</remarks>
    public Semantics(
        SemanticsProperties properties,
        Widget? child = null,
        bool container = false,
        bool explicitChildNodes = false,
        bool excludeSemantics = false,
        bool blockUserActions = false,
        Locale? localeForSubtree = null,
        Key? key = null)
        : base(
            properties,
            child,
            container: container,
            explicitChildNodes: explicitChildNodes,
            excludeSemantics: excludeSemantics,
            blockUserActions: blockUserActions,
            localeForSubtree: localeForSubtree,
            key: key)
    {
    }

    /// <inheritdoc />
    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSemanticsAnnotations(
            properties: Properties,
            container: Container,
            explicitChildNodes: ExplicitChildNodes,
            excludeSemantics: ExcludeSemantics,
            blockUserActions: BlockUserActions,
            textDirection: GetTextDirection(context),
            localeForSubtree: LocaleForSubtree);
    }

    /// <inheritdoc />
    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var semantics = (RenderSemanticsAnnotations)renderObject;
        semantics.Container = Container;
        semantics.ExplicitChildNodes = ExplicitChildNodes;
        semantics.ExcludeSemantics = ExcludeSemantics;
        semantics.BlockUserActions = BlockUserActions;
        semantics.Properties = Properties;
        semantics.TextDirection = GetTextDirection(context);
        semantics.LocaleForSubtree = LocaleForSubtree;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<bool>("container", Container));
        properties.Add(new DiagnosticsProperty<SemanticsProperties>("properties", Properties));
        Properties.DebugFillProperties(properties);
    }
}

/// <summary>
/// The sliver counterpart of <see cref="Semantics"/>: annotates a sliver subtree instead of a box one.
/// </summary>
/// <remarks>Flutter's <c>SliverSemantics</c>.</remarks>
public sealed class SliverSemantics : SemanticsBase
{
    public SliverSemantics(
        Widget sliver,
        SemanticsProperties properties,
        bool container = false,
        bool explicitChildNodes = false,
        bool excludeSemantics = false,
        bool blockUserActions = false,
        Locale? localeForSubtree = null,
        Key? key = null)
        : base(
            properties,
            sliver,
            container: container,
            explicitChildNodes: explicitChildNodes,
            excludeSemantics: excludeSemantics,
            blockUserActions: blockUserActions,
            localeForSubtree: localeForSubtree,
            key: key)
    {
    }

    /// <inheritdoc />
    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverSemanticsAnnotations(
            properties: Properties,
            container: Container,
            explicitChildNodes: ExplicitChildNodes,
            excludeSemantics: ExcludeSemantics,
            blockUserActions: BlockUserActions,
            textDirection: GetTextDirection(context),
            localeForSubtree: LocaleForSubtree);
    }

    /// <inheritdoc />
    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var semantics = (RenderSliverSemanticsAnnotations)renderObject;
        semantics.Container = Container;
        semantics.ExplicitChildNodes = ExplicitChildNodes;
        semantics.ExcludeSemantics = ExcludeSemantics;
        semantics.BlockUserActions = BlockUserActions;
        semantics.Properties = Properties;
        semantics.TextDirection = GetTextDirection(context);
        semantics.LocaleForSubtree = LocaleForSubtree;
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart (MergeSemantics)
public sealed class MergeSemantics : SingleChildRenderObjectWidget
{
    public MergeSemantics(Widget? child = null, Key? key = null) : base(child, key)
    {
    }

    public override RenderObject CreateRenderObject(BuildContext context) => new RenderMergeSemantics();
}

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart (ExcludeSemantics)
public sealed class ExcludeSemantics : SingleChildRenderObjectWidget
{
    public ExcludeSemantics(
        Widget? child = null,
        bool excluding = true,
        Key? key = null) : base(child, key)
    {
        Excluding = excluding;
    }

    public bool Excluding { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderExcludeSemantics(Excluding);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderExcludeSemantics)renderObject).Excluding = Excluding;
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart (BlockSemantics)
public sealed class BlockSemantics : SingleChildRenderObjectWidget
{
    public BlockSemantics(
        Widget? child = null,
        bool blocking = true,
        Key? key = null) : base(child, key)
    {
        Blocking = blocking;
    }

    public bool Blocking { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderBlockSemantics(Blocking);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderBlockSemantics)renderObject).Blocking = Blocking;
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart (IndexedSemantics)
public sealed class IndexedSemantics : SingleChildRenderObjectWidget
{
    public IndexedSemantics(
        int index,
        Widget? child = null,
        Key? key = null) : base(child, key)
    {
        Index = index;
    }

    public int Index { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderIndexedSemantics(Index);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderIndexedSemantics)renderObject).Index = Index;
    }
}
