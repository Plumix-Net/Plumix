using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/object.dart

namespace Plumix.Rendering;

/// <summary>
/// The diagnostics half of <see cref="RenderObject"/>: Dart's `DiagnosticableTreeMixin` surface plus
/// the `toStringShort`/`debugFillProperties`/`debugDescribeChildren` overrides that make the render
/// tree dumpable.
/// </summary>
public abstract partial class RenderObject
{
    /// <summary>Whether the parent passed <c>parentUsesSize: true</c> to the last layout call.</summary>
    /// <remarks>Flutter's <c>RenderObject._debugCanParentUseSize</c>.</remarks>
    private bool? _debugCanParentUseSize;

    /// The object responsible for creating this render object.
    ///
    /// Used in debug messages.
    ///
    /// See also:
    ///
    ///  * [DebugCreator], which the [Widgets] library uses as values for this field.
    public object? DebugCreator { get; set; }

    /// Returns a human understandable name.
    public override string ToStringShort()
    {
        string header = Diagnostics.DescribeIdentity(this);
        if (DebugDisposed)
        {
            return $"{header} DISPOSED";
        }

        int count = 0;
        for (RenderObject? node = this; node is not null && node._isRelayoutBoundary != true; node = node.Parent)
        {
            if (node._isRelayoutBoundary is null)
            {
                count = -1;
                break;
            }

            count += 1;
        }

        if (count > 0)
        {
            header += $" relayoutBoundary=up{count}";
        }

        if (_needsLayout)
        {
            header += " NEEDS-LAYOUT";
        }

        if (_needsPaint)
        {
            header += " NEEDS-PAINT";
        }

        if (_needsCompositingBitsUpdate)
        {
            header += " NEEDS-COMPOSITING-BITS-UPDATE";
        }

        if (!Attached)
        {
            header += " DETACHED";
        }

        return header;
    }

    /// <inheritdoc />
    public override string ToString(DiagnosticLevel minLevel) => ToStringShort();

    /// <inheritdoc />
    public override string ToStringDeep(
        string prefixLineOne = "",
        string? prefixOtherLines = null,
        DiagnosticLevel minLevel = DiagnosticLevel.Debug,
        int wrapWidth = 65)
    {
        return WithDebugActiveLayoutCleared(
            () => base.ToStringDeep(prefixLineOne, prefixOtherLines, minLevel, wrapWidth));
    }

    /// <inheritdoc />
    public override string ToStringShallow(
        string joiner = ", ",
        DiagnosticLevel minLevel = DiagnosticLevel.Debug)
    {
        return WithDebugActiveLayoutCleared(() => base.ToStringShallow(joiner, minLevel));
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new FlagProperty("needsCompositing", NeedsCompositing, ifTrue: "needs compositing"));
        properties.Add(new DiagnosticsProperty<object>(
            "creator",
            DebugCreator,
            defaultValue: DiagnosticsDefaults.NullValue,
            level: DiagnosticLevel.Debug));
        properties.Add(new DiagnosticsProperty<IParentData>(
            "parentData",
            parentData,
            tooltip: _debugCanParentUseSize == true ? "can use size" : null,
            missingIfNull: true));
        properties.Add(new DiagnosticsProperty<IConstraints>("constraints", _constraints, missingIfNull: true));

        // Don't access it via the "Layer" getter since that's only valid when we don't need paint.
        properties.Add(new DiagnosticsProperty<Layer>(
            "layer",
            DebugLayer,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DiagnosticsProperty<SemanticsNode>(
            "semantics node",
            SemanticsNode,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new FlagProperty(
            "isBlockingSemanticsOfPreviouslyPaintedNodes",
            _semantics.ConfigProvider.Effective.IsBlockingSemanticsOfPreviouslyPaintedNodes,
            ifTrue: "blocks semantics of earlier render objects below the common boundary"));
        properties.Add(new FlagProperty(
            "isSemanticBoundary",
            _semantics.ConfigProvider.Effective.IsSemanticBoundary,
            ifTrue: "semantic boundary"));
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren() => [];

    /// Adds a debug representation of a [RenderObject] optimized for including
    /// in error messages.
    ///
    /// The default `style` of [DiagnosticsTreeStyle.shallow] ensures that all of
    /// the properties of the render object are included in the error output but
    /// none of the children of the object.
    public DiagnosticsNode DescribeForError(
        string name,
        DiagnosticsTreeStyle style = DiagnosticsTreeStyle.Shallow)
    {
        return ToDiagnosticsNode(name: name, style: style);
    }

    /// The single child of a render object that has at most one child, described
    /// the way Dart's `RenderObjectWithChildMixin.debugDescribeChildren` describes it.
    ///
    /// C# has no mixins, so the mixin's body lives here and the single-child render objects call it
    /// from their own <see cref="DebugDescribeChildren"/> override.
    private protected static List<DiagnosticsNode> DebugDescribeSingleChild(RenderObject? child)
    {
        return child is null ? [] : [child.ToDiagnosticsNode(name: "child")];
    }

    /// <summary>
    /// Checks that <paramref name="child"/> is of the child type <paramref name="parent"/> expects.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>RenderObjectWithChildMixin.debugValidateChild</c> and
    /// <c>ContainerRenderObjectMixin.debugValidateChild</c>, which share one body. C# has no mixins,
    /// so the body lives here and both child-holding shapes call it.
    /// </remarks>
    internal static bool DebugValidateChildType<TChild>(RenderObject parent, RenderObject child)
        where TChild : RenderObject
    {
        if (!Constants.KDebugMode || child is TChild)
        {
            return true;
        }

        throw new FlutterError([
            new ErrorSummary(
                $"A {parent.GetType().Name} expected a child of type {typeof(TChild).Name} but received a "
                + $"child of type {child.GetType().Name}."),
            new ErrorDescription(
                "RenderObjects expect specific types of children because they coordinate with their "
                + "children during layout and paint. For example, a RenderSliver cannot be the child of "
                + "a RenderBox because a RenderSliver does not understand the RenderBox layout protocol."),
            new ErrorSpacer(),
            new DiagnosticsProperty<object>(
                $"The {parent.GetType().Name} that expected a {typeof(TChild).Name} child was created by",
                parent.DebugCreator,
                style: DiagnosticsTreeStyle.ErrorProperty),
            new ErrorSpacer(),
            new DiagnosticsProperty<object>(
                $"The {child.GetType().Name} that did not match the expected child type was created by",
                child.DebugCreator,
                style: DiagnosticsTreeStyle.ErrorProperty),
        ]);
    }

    private static T WithDebugActiveLayoutCleared<T>(Func<T> callback)
    {
        RenderObject? previousActiveLayout = _debugActiveLayout;
        _debugActiveLayout = null;
        try
        {
            return callback();
        }
        finally
        {
            _debugActiveLayout = previousActiveLayout;
        }
    }
}
