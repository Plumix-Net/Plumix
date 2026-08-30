using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/basic.dart (Semantics subset)

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

public sealed class Semantics : SingleChildRenderObjectWidget
{
    public Semantics(
        Widget? child = null,
        string? label = null,
        string? hint = null,
        string? onTapHint = null,
        string? tooltip = null,
        string? value = null,
        string? minValue = null,
        string? maxValue = null,
        string? increasedValue = null,
        string? decreasedValue = null,
        SemanticsFlags flags = SemanticsFlags.None,
        Action? onTap = null,
        Action? onLongPress = null,
        Action? onDismiss = null,
        Action? onExpand = null,
        Action? onCollapse = null,
        Action? onIncrease = null,
        Action? onDecrease = null,
        IReadOnlyDictionary<CustomSemanticsAction, Action>? customSemanticsActions = null,
        bool liveRegion = false,
        bool container = false,
        bool explicitChildNodes = false,
        SemanticsRole role = SemanticsRole.None,
        SemanticsInputType inputType = SemanticsInputType.None,
        SemanticsHitTestBehavior hitTestBehavior = SemanticsHitTestBehavior.Defer,
        bool scopesRoute = false,
        bool namesRoute = false,
        bool? expanded = null,
        bool? @checked = null,
        bool? mixed = null,
        bool? selected = null,
        bool? enabled = null,
        bool? focusable = null,
        SemanticsSortKey? sortKey = null,
        object? traversalParentIdentifier = null,
        object? traversalChildIdentifier = null,
        TextDirection? textDirection = null,
        SemanticsTag? tagForChildren = null,
        Key? key = null,
        bool mergeDescendants = false,
        AccessibilityFocusBlockType accessibilityFocusBlockType = AccessibilityFocusBlockType.None,
        bool? toggled = null) : base(child, key)
    {
        AccessibilityFocusBlockType = accessibilityFocusBlockType;
        TagForChildren = tagForChildren;
        Label = label;
        Hint = hint;
        OnTapHint = onTapHint;
        Tooltip = tooltip;
        Value = value;
        MinValue = minValue;
        MaxValue = maxValue;
        IncreasedValue = increasedValue;
        DecreasedValue = decreasedValue;
        Flags = flags
                | (scopesRoute ? SemanticsFlags.ScopesRoute : SemanticsFlags.None)
                | (namesRoute ? SemanticsFlags.NamesRoute : SemanticsFlags.None)
                | (expanded.HasValue ? SemanticsFlags.HasExpandedState : SemanticsFlags.None)
                | (expanded == true ? SemanticsFlags.IsExpanded : SemanticsFlags.None)
                | (@checked.HasValue ? SemanticsFlags.HasCheckedState : SemanticsFlags.None)
                | (@checked == true ? SemanticsFlags.IsChecked : SemanticsFlags.None)
                | (mixed == true ? SemanticsFlags.IsCheckStateMixed : SemanticsFlags.None)
                | (toggled.HasValue ? SemanticsFlags.HasToggledState : SemanticsFlags.None)
                | (toggled == true ? SemanticsFlags.IsToggled : SemanticsFlags.None)
                | (selected.HasValue ? SemanticsFlags.HasSelectedState : SemanticsFlags.None)
                | (selected == true ? SemanticsFlags.IsSelected : SemanticsFlags.None)
                | (enabled.HasValue ? SemanticsFlags.HasEnabledState : SemanticsFlags.None)
                | (enabled == true ? SemanticsFlags.IsEnabled : SemanticsFlags.None)
                | (focusable == true ? SemanticsFlags.IsFocusable : SemanticsFlags.None);
        OnTap = onTap;
        OnLongPress = onLongPress;
        OnDismiss = onDismiss;
        OnExpand = onExpand;
        OnCollapse = onCollapse;
        OnIncrease = onIncrease;
        OnDecrease = onDecrease;
        CustomSemanticsActions = customSemanticsActions;
        LiveRegion = liveRegion;
        Container = container;
        ExplicitChildNodes = explicitChildNodes;
        Role = role;
        InputType = inputType;
        HitTestBehavior = hitTestBehavior;
        ScopesRoute = scopesRoute;
        NamesRoute = namesRoute;
        Expanded = expanded;
        Checked = @checked;
        Mixed = mixed;
        Toggled = toggled;
        Selected = selected;
        Enabled = enabled;
        Focusable = focusable;
        SortKey = sortKey;
        TraversalParentIdentifier = traversalParentIdentifier;
        TraversalChildIdentifier = traversalChildIdentifier;
        TextDirection = textDirection;
        MergeDescendants = mergeDescendants;
    }

    public string? Label { get; }

    public string? Hint { get; }

    public string? OnTapHint { get; }

    public string? Tooltip { get; }

    public string? Value { get; }

    public string? MinValue { get; }

    public string? MaxValue { get; }

    public string? IncreasedValue { get; }

    public string? DecreasedValue { get; }

    public SemanticsFlags Flags { get; }

    public Action? OnTap { get; }

    public Action? OnLongPress { get; }

    public Action? OnDismiss { get; }

    /// <summary>Handler for <c>SemanticsAction.expand</c>, invoked to expand a collapsed node.</summary>
    public Action? OnExpand { get; }

    /// <summary>Handler for <c>SemanticsAction.collapse</c>, invoked to collapse an expanded node.</summary>
    public Action? OnCollapse { get; }

    public Action? OnIncrease { get; }

    public Action? OnDecrease { get; }

    public IReadOnlyDictionary<CustomSemanticsAction, Action>? CustomSemanticsActions { get; }

    public Action? OnFocus { get; init; }

    public bool LiveRegion { get; }

    public bool Container { get; }

    public bool ExplicitChildNodes { get; }

    public SemanticsRole Role { get; }

    public SemanticsInputType InputType { get; }

    public SemanticsHitTestBehavior HitTestBehavior { get; }

    public bool ScopesRoute { get; }

    public bool NamesRoute { get; }

    public bool? Expanded { get; }

    public bool? Checked { get; }

    public bool? Mixed { get; }

    public bool? Toggled { get; }

    public bool? Selected { get; }

    public bool? Enabled { get; }

    public bool? Focusable { get; }

    public SemanticsSortKey? SortKey { get; }

    /// <summary>
    /// Identifies this node as the traversal parent that nodes carrying the matching
    /// <see cref="TraversalChildIdentifier"/> are traversed under, wherever they sit in paint order.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>SemanticsProperties.traversalParentIdentifier</c>. The value must be unique
    /// across the whole semantics tree.
    /// </remarks>
    public object? TraversalParentIdentifier { get; }

    /// <summary>
    /// Names the <see cref="TraversalParentIdentifier"/> this subtree is traversed under.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>SemanticsProperties.traversalChildIdentifier</c>. Several nodes may share one
    /// value; they all graft onto the same traversal parent, in paint order.
    /// </remarks>
    public object? TraversalChildIdentifier { get; }

    /// <summary>
    /// The reading direction for this subtree's semantics, and the direction the default traversal
    /// sort walks siblings in.
    /// </summary>
    public TextDirection? TextDirection { get; }

    /// The tag attached to every semantics node created below this widget.
    public SemanticsTag? TagForChildren { get; }

    public bool MergeDescendants { get; }

    /// <summary>
    /// Whether assistive technologies may move accessibility focus onto this node, its subtree, or
    /// both. Blocking focus also stops the node reporting itself as keyboard focusable.
    /// </summary>
    public AccessibilityFocusBlockType AccessibilityFocusBlockType { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        var semantics = new RenderSemanticsAnnotations(
            label: Label,
            hint: Hint,
            onTapHint: OnTapHint,
            tooltip: Tooltip,
            value: Value,
            minValue: MinValue,
            maxValue: MaxValue,
            increasedValue: IncreasedValue,
            decreasedValue: DecreasedValue,
            role: Role,
            inputType: InputType,
            hitTestBehavior: HitTestBehavior,
            flags: Flags,
            onTap: OnTap,
            onLongPress: OnLongPress,
            onDismiss: OnDismiss,
            onExpand: OnExpand,
            onCollapse: OnCollapse,
            onIncrease: OnIncrease,
            onDecrease: OnDecrease,
            customSemanticsActions: CustomSemanticsActions,
            liveRegion: LiveRegion,
            container: Container,
            explicitChildNodes: ExplicitChildNodes,
            sortKey: SortKey,
            traversalParentIdentifier: TraversalParentIdentifier,
            traversalChildIdentifier: TraversalChildIdentifier,
            textDirection: TextDirection,
            mergeDescendants: MergeDescendants,
            tagForChildren: TagForChildren,
            accessibilityFocusBlockType: AccessibilityFocusBlockType);
        semantics.OnFocus = OnFocus;
        return semantics;
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var semantics = (RenderSemanticsAnnotations)renderObject;

        // Flutter assigns one `SemanticsProperties` value object and invalidates the semantics once;
        // Plumix has a setter per property, so the batch keeps the configuration from being
        // re-collected halfway through, before the callbacks below have been assigned.
        using RenderSemanticsAnnotations.PropertyBatch batch = semantics.BeginPropertyBatch();
        semantics.Label = Label;
        semantics.Hint = Hint;
        semantics.OnTapHint = OnTapHint;
        semantics.Tooltip = Tooltip;
        semantics.Value = Value;
        semantics.MinValue = MinValue;
        semantics.MaxValue = MaxValue;
        semantics.IncreasedValue = IncreasedValue;
        semantics.DecreasedValue = DecreasedValue;
        semantics.Role = Role;
        semantics.InputType = InputType;
        semantics.HitTestBehavior = HitTestBehavior;
        semantics.Flags = Flags;
        semantics.OnTap = OnTap;
        semantics.OnLongPress = OnLongPress;
        semantics.OnDismiss = OnDismiss;
        semantics.OnExpand = OnExpand;
        semantics.OnCollapse = OnCollapse;
        semantics.OnIncrease = OnIncrease;
        semantics.OnDecrease = OnDecrease;
        semantics.CustomSemanticsActions = CustomSemanticsActions;
        semantics.OnFocus = OnFocus;
        semantics.LiveRegion = LiveRegion;
        semantics.Container = Container;
        semantics.ExplicitChildNodes = ExplicitChildNodes;
        semantics.SortKey = SortKey;
        semantics.TraversalParentIdentifier = TraversalParentIdentifier;
        semantics.TraversalChildIdentifier = TraversalChildIdentifier;
        semantics.MergeDescendants = MergeDescendants;
        semantics.TagForChildren = TagForChildren;
        semantics.AccessibilityFocusBlockType = AccessibilityFocusBlockType;
    }

}

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/basic.dart (MergeSemantics)
public sealed class MergeSemantics : StatelessWidget
{
    public MergeSemantics(Widget child, Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return new Semantics(
            child: Child,
            container: true,
            mergeDescendants: true);
    }
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

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderExcludeSemantics(Excluding);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
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

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderBlockSemantics(Blocking);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
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

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderIndexedSemantics(Index);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderIndexedSemantics)renderObject).Index = Index;
    }
}
