using Avalonia;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/semantics/semantics.dart

namespace Plumix.Rendering;

/// <summary>
/// Used by <see cref="SemanticsNode.DebugDescribeChildren(DebugSemanticsDumpOrder)"/> and the dumps built
/// on it to specify the order in which child nodes are printed.
/// </summary>
/// <remarks>Flutter's <c>DebugSemanticsDumpOrder</c>.</remarks>
public enum DebugSemanticsDumpOrder
{
    /// <summary>Print nodes in inverse hit test order: the last child is hit tested first.</summary>
    InverseHitTest,

    /// <summary>Print nodes in semantic traversal order: the order assistive technology visits them in.</summary>
    TraversalOrder,
}

public sealed partial class SemanticsNode : DiagnosticableTree
{
    private SemanticsActions[] _declaredActions = [];

    /// <remarks>Flutter's private <c>SemanticsNode._debugIsActionBlocked</c>.</remarks>
    private bool DebugIsActionBlocked(SemanticsActions action)
    {
        // Plumix keeps the custom actions' handler out of the action bits; an unblocked node that has
        // custom actions performs customAction exactly like Dart's.
        return Constants.KDebugMode
               && (Actions & action) == SemanticsActions.None
               && !(action == SemanticsActions.CustomAction && CustomSemanticsActions.Count > 0);
    }

    /// <inheritdoc />
    /// <remarks>Flutter's <c>SemanticsNode.toStringShort</c>.</remarks>
    public override string ToStringShort() => $"{Diagnostics.ObjectRuntimeType(this, nameof(SemanticsNode))}#{Id}";

    /// <inheritdoc />
    /// <remarks>Flutter's <c>SemanticsNode.debugFillProperties</c>.</remarks>
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        bool hideOwner = true;
        if (IsDirty)
        {
            bool inDirtyNodes = Owner is { } owner && owner.IsDirtyNode(this);
            properties.Add(new FlagProperty("inDirtyNodes", value: inDirtyNodes, ifTrue: "dirty", ifFalse: "STALE"));
            hideOwner = inDirtyNodes;
        }

        properties.Add(new DiagnosticsProperty<SemanticsOwner>(
            "owner",
            Owner,
            level: hideOwner ? DiagnosticLevel.Hidden : DiagnosticLevel.Info));
        properties.Add(new FlagProperty("isMergedIntoParent", value: IsMergedIntoParent, ifTrue: "merged up ⬆️"));
        properties.Add(new FlagProperty(
            "mergeAllDescendantsIntoThisNode",
            value: MergeAllDescendantsIntoThisNode,
            ifTrue: "merge boundary ⛔️"));
        if (Locale is not null)
        {
            properties.Add(new StringProperty("locale", Locale.ToString()));
        }

        Point? offset = Transform is { } translation ? MatrixUtils.GetAsTranslation(translation) : null;
        if (offset is { } shift)
        {
            properties.Add(new DiagnosticsProperty<Rect>(
                "rect",
                Rect.Translate(new Vector(shift.X, shift.Y)),
                showName: false));
        }
        else
        {
            double? scale = Transform is { } scaleTransform ? MatrixUtils.GetAsScale(scaleTransform) : null;
            string? description = null;
            if (scale is { } factor)
            {
                description = $"{Diagnostics.DescribeValue(Rect)} scaled by {Diagnostics.ToStringAsFixed(factor, 1)}x";
            }
            else if (Transform is { } matrix && !MatrixUtils.IsIdentity(matrix))
            {
                string rows = string.Join(
                    "; ",
                    matrix.ToString().Split('\n').Take(4).Select(static line => line[4..]));
                description = $"{Diagnostics.DescribeValue(Rect)} with transform [{rows}]";
            }

            properties.Add(new DiagnosticsProperty<Rect>("rect", Rect, description: description, showName: false));
        }

        properties.Add(new IterableProperty<string>(
            "tags",
            Tags?.Select(static tag => tag.Name),
            defaultValue: DiagnosticsDefaults.NullValue));
        List<string> actions =
        [
            .. _declaredActions.Select(action =>
                $"{Diagnostics.EnumName(action)}{(DebugIsActionBlocked(action) ? "🚫️" : string.Empty)}"),
        ];
        actions.Sort(string.CompareOrdinal);
        List<string?> customSemanticsActions = [.. CustomSemanticsActions.Keys.Select(static action => action.Label)];
        properties.Add(new IterableProperty<string>("actions", actions, ifEmpty: null));
        properties.Add(new IterableProperty<string?>("customActions", customSemanticsActions, ifEmpty: null));
        properties.Add(new IterableProperty<string>("flags", [.. SemanticsData.DescribeFlags(Flags)], ifEmpty: null));
        properties.Add(new FlagProperty("isInvisible", value: IsInvisible, ifTrue: "invisible"));
        properties.Add(new FlagProperty("isHidden", value: IsHidden, ifTrue: "HIDDEN"));
        properties.Add(new StringProperty("identifier", Identifier, defaultValue: string.Empty));
        properties.Add(new DiagnosticsProperty<object>(
            "traversalParentIdentifier",
            TraversalParentIdentifier,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DiagnosticsProperty<object>(
            "traversalChildIdentifier",
            TraversalChildIdentifier,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new AttributedStringProperty("label", AttributedLabel));
        properties.Add(new AttributedStringProperty("value", AttributedValue));
        properties.Add(new AttributedStringProperty("increasedValue", AttributedIncreasedValue));
        properties.Add(new AttributedStringProperty("decreasedValue", AttributedDecreasedValue));
        properties.Add(new AttributedStringProperty("hint", AttributedHint));
        properties.Add(new StringProperty("tooltip", Tooltip, defaultValue: string.Empty));
        properties.Add(new EnumProperty<TextDirection>(
            "textDirection",
            TextDirection,
            defaultValue: DiagnosticsDefaults.NullValue));
        if (Role != SemanticsRole.None)
        {
            properties.Add(new EnumProperty<SemanticsRole>("role", Role));
        }

        properties.Add(new DiagnosticsProperty<SemanticsSortKey>(
            "sortKey",
            SortKey,
            defaultValue: DiagnosticsDefaults.NullValue));
        if (TextSelection is { IsValid: true } selection)
        {
            properties.Add(new MessageProperty("text selection", $"[{selection.Start}, {selection.End}]"));
        }

        properties.Add(new IntProperty("platformViewId", PlatformViewId, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new IntProperty("maxValueLength", MaxValueLength, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new IntProperty(
            "currentValueLength",
            CurrentValueLength,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new IntProperty(
            "scrollChildren",
            ScrollChildCount,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new IntProperty("scrollIndex", ScrollIndex, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DoubleProperty(
            "scrollExtentMin",
            ScrollExtentMin,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DoubleProperty(
            "scrollPosition",
            ScrollPosition,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DoubleProperty(
            "scrollExtentMax",
            ScrollExtentMax,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new IntProperty("indexInParent", IndexInParent, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new IntProperty("headingLevel", HeadingLevel, defaultValue: 0));
        if (InputType != SemanticsInputType.None)
        {
            properties.Add(new EnumProperty<SemanticsInputType>("inputType", InputType));
        }

        if (ValidationResult != SemanticsValidationResult.None)
        {
            properties.Add(new EnumProperty<SemanticsValidationResult>(
                "validationResult",
                ValidationResult,
                defaultValue: SemanticsValidationResult.None));
        }

        properties.Add(new StringProperty("minValue", MinValue, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new StringProperty("maxValue", MaxValue, defaultValue: DiagnosticsDefaults.NullValue));
    }

    /// <inheritdoc />
    public override string ToStringDeep(
        string prefixLineOne = "",
        string? prefixOtherLines = null,
        DiagnosticLevel minLevel = DiagnosticLevel.Debug,
        int wrapWidth = 65)
    {
        return ToStringDeep(
            DebugSemanticsDumpOrder.TraversalOrder,
            prefixLineOne,
            prefixOtherLines,
            minLevel,
            wrapWidth);
    }

    /// <summary>
    /// Returns a string representation of this node and its descendants, printing the children in
    /// <paramref name="childOrder"/>.
    /// </summary>
    /// <remarks>Flutter's <c>SemanticsNode.toStringDeep</c>.</remarks>
    public string ToStringDeep(
        DebugSemanticsDumpOrder childOrder,
        string prefixLineOne = "",
        string? prefixOtherLines = null,
        DiagnosticLevel minLevel = DiagnosticLevel.Debug,
        int wrapWidth = 65)
    {
        return ToDiagnosticsNode(childOrder: childOrder).ToStringDeep(
            prefixLineOne: prefixLineOne,
            prefixOtherLines: prefixOtherLines,
            minLevel: minLevel,
            wrapWidth: wrapWidth);
    }

    /// <inheritdoc />
    public override DiagnosticsNode ToDiagnosticsNode(string? name = null, DiagnosticsTreeStyle? style = null)
    {
        return ToDiagnosticsNode(name, style ?? DiagnosticsTreeStyle.Sparse, DebugSemanticsDumpOrder.TraversalOrder);
    }

    /// <summary>A diagnostics node describing this node, whose children follow <paramref name="childOrder"/>.</summary>
    /// <remarks>Flutter's <c>SemanticsNode.toDiagnosticsNode</c>.</remarks>
    public DiagnosticsNode ToDiagnosticsNode(
        string? name = null,
        DiagnosticsTreeStyle? style = DiagnosticsTreeStyle.Sparse,
        DebugSemanticsDumpOrder childOrder = DebugSemanticsDumpOrder.TraversalOrder)
    {
        return new SemanticsDiagnosticableNode(name, this, style, childOrder);
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren() =>
        DebugDescribeChildren(DebugSemanticsDumpOrder.TraversalOrder);

    /// <summary>Describes this node's children, in <paramref name="childOrder"/>.</summary>
    /// <remarks>Flutter's <c>SemanticsNode.debugDescribeChildren</c>.</remarks>
    public List<DiagnosticsNode> DebugDescribeChildren(DebugSemanticsDumpOrder childOrder)
    {
        return
        [
            .. DebugListChildrenInOrder(childOrder)
                .Select(node => node.ToDiagnosticsNode(childOrder: childOrder)),
        ];
    }

    /// <summary>Returns the list of direct children of this node in the specified order.</summary>
    /// <remarks>Flutter's <c>SemanticsNode.debugListChildrenInOrder</c>.</remarks>
    public IReadOnlyList<SemanticsNode> DebugListChildrenInOrder(DebugSemanticsDumpOrder childOrder)
    {
        if (Children.Count == 0)
        {
            return [];
        }

        return childOrder switch
        {
            DebugSemanticsDumpOrder.InverseHitTest => ChildrenInHitTestOrder,
            _ => ChildrenInTraversalOrder,
        };
    }

    /// <summary>Flutter's private <c>_SemanticsDiagnosticableNode</c>.</summary>
    private sealed class SemanticsDiagnosticableNode(
        string? name,
        SemanticsNode value,
        DiagnosticsTreeStyle? style,
        DebugSemanticsDumpOrder childOrder) : DiagnosticableNode<SemanticsNode>(name, value, style)
    {
        public override List<DiagnosticsNode> GetChildren() => TypedValue.DebugDescribeChildren(childOrder);
    }
}
