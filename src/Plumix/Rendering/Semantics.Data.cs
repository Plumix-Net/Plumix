using Avalonia;
using System.Diagnostics;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/semantics/semantics.dart

namespace Plumix.Rendering;

/// <summary>
/// The flattened annotations of one <see cref="SemanticsNode"/>, with every descendant merged into
/// it that the node asked to absorb.
/// </summary>
/// <remarks>
/// Flutter's <c>SemanticsData</c>. Dart splits the tri-state flags into a <c>SemanticsFlags</c>
/// value object; Plumix keeps the framework's <see cref="SemanticsFlags"/> bit set, so
/// <c>flagsCollection</c>/<c>flags</c> collapse into <see cref="Flags"/>. Semantic text is non-null,
/// with unset strings represented by <c>AttributedString('')</c> just as in Dart.
/// </remarks>
public sealed class SemanticsData : Diagnosticable, IEquatable<SemanticsData>
{
    /// <summary>Creates a flattened annotation record.</summary>
    public SemanticsData(
        SemanticsFlags flags,
        SemanticsActions actions,
        string identifier,
        object? traversalParentIdentifier,
        object? traversalChildIdentifier,
        AttributedString attributedLabel,
        AttributedString attributedValue,
        AttributedString attributedIncreasedValue,
        AttributedString attributedDecreasedValue,
        AttributedString attributedHint,
        string tooltip,
        TextDirection? textDirection,
        Rect rect,
        TextSelection? textSelection,
        int? scrollIndex,
        int? scrollChildCount,
        double? scrollPosition,
        double? scrollExtentMax,
        double? scrollExtentMin,
        int? platformViewId,
        int? maxValueLength,
        int? currentValueLength,
        int headingLevel,
        Uri? linkUrl,
        SemanticsRole role,
        IReadOnlySet<string>? controlsNodes,
        SemanticsValidationResult validationResult,
        SemanticsHitTestBehavior hitTestBehavior,
        SemanticsInputType inputType,
        Locale? locale,
        string? minValue,
        string? maxValue,
        IReadOnlySet<SemanticsTag>? tags = null,
        Matrix4? transform = null,
        IReadOnlyList<int>? customSemanticsActionIds = null)
    {
        ArgumentNullException.ThrowIfNull(identifier);
        ArgumentNullException.ThrowIfNull(attributedLabel);
        ArgumentNullException.ThrowIfNull(attributedValue);
        ArgumentNullException.ThrowIfNull(attributedIncreasedValue);
        ArgumentNullException.ThrowIfNull(attributedDecreasedValue);
        ArgumentNullException.ThrowIfNull(attributedHint);
        ArgumentNullException.ThrowIfNull(tooltip);
        Debug.Assert(
            string.IsNullOrEmpty(tooltip) || textDirection is not null,
            $"A SemanticsData object with tooltip \"{tooltip}\" had a null textDirection.");
        Debug.Assert(
            IsEmptyString(attributedLabel) || textDirection is not null,
            $"A SemanticsData object with label \"{attributedLabel.String}\" had a null textDirection.");
        Debug.Assert(
            IsEmptyString(attributedValue) || textDirection is not null,
            $"A SemanticsData object with value \"{attributedValue.String}\" had a null textDirection.");
        Debug.Assert(
            IsEmptyString(attributedDecreasedValue) || textDirection is not null,
            $"A SemanticsData object with decreasedValue \"{attributedDecreasedValue.String}\" "
            + "had a null textDirection.");
        Debug.Assert(
            IsEmptyString(attributedIncreasedValue) || textDirection is not null,
            $"A SemanticsData object with increasedValue \"{attributedIncreasedValue.String}\" "
            + "had a null textDirection.");
        Debug.Assert(
            IsEmptyString(attributedHint) || textDirection is not null,
            $"A SemanticsData object with hint \"{attributedHint.String}\" had a null textDirection.");
        Debug.Assert(headingLevel is >= 0 and <= 6, "Heading level must be between 0 and 6");
        Debug.Assert(
            linkUrl is null || flags.HasFlag(SemanticsFlags.IsLink),
            "A SemanticsData object with a linkUrl must have the isLink flag set to true");

        Flags = flags;
        Actions = actions;
        Identifier = identifier;
        TraversalParentIdentifier = traversalParentIdentifier;
        TraversalChildIdentifier = traversalChildIdentifier;
        AttributedLabel = attributedLabel;
        AttributedValue = attributedValue;
        AttributedIncreasedValue = attributedIncreasedValue;
        AttributedDecreasedValue = attributedDecreasedValue;
        AttributedHint = attributedHint;
        Tooltip = tooltip;
        TextDirection = textDirection;
        Rect = rect;
        TextSelection = textSelection;
        ScrollIndex = scrollIndex;
        ScrollChildCount = scrollChildCount;
        ScrollPosition = scrollPosition;
        ScrollExtentMax = scrollExtentMax;
        ScrollExtentMin = scrollExtentMin;
        PlatformViewId = platformViewId;
        MaxValueLength = maxValueLength;
        CurrentValueLength = currentValueLength;
        HeadingLevel = headingLevel;
        LinkUrl = linkUrl;
        Role = role;
        ControlsNodes = controlsNodes;
        ValidationResult = validationResult;
        HitTestBehavior = hitTestBehavior;
        InputType = inputType;
        Locale = locale;
        MinValue = minValue;
        MaxValue = maxValue;
        Tags = tags;
        Transform = transform;
        CustomSemanticsActionIds = customSemanticsActionIds;
    }

    private static bool IsEmptyString(AttributedString value) => value.String.Length == 0;

    /// <summary>The semantic flags that apply to this node.</summary>
    public SemanticsFlags Flags { get; }

    /// <summary>The actions this node handles.</summary>
    public SemanticsActions Actions { get; }

    /// <summary>The node's stable string identifier.</summary>
    public string Identifier { get; }

    /// <summary>The key naming this node as a traversal-graft parent.</summary>
    public object? TraversalParentIdentifier { get; }

    /// <summary>The key naming this node as a traversal-graft child.</summary>
    public object? TraversalChildIdentifier { get; }

    /// <summary>The label, with its string attributes.</summary>
    public AttributedString AttributedLabel { get; }

    /// <summary>The current value, with its string attributes.</summary>
    public AttributedString AttributedValue { get; }

    /// <summary>The value after <see cref="SemanticsActions.Increase"/>.</summary>
    public AttributedString AttributedIncreasedValue { get; }

    /// <summary>The value after <see cref="SemanticsActions.Decrease"/>.</summary>
    public AttributedString AttributedDecreasedValue { get; }

    /// <summary>The hint about what acting on this node does.</summary>
    public AttributedString AttributedHint { get; }

    /// <summary>The node's tooltip.</summary>
    public string Tooltip { get; }

    /// <summary>The reading direction of every string above.</summary>
    public TextDirection? TextDirection { get; }

    /// <summary>The node's bounding box in its own coordinate system.</summary>
    public Rect Rect { get; }

    /// <summary>The selection inside <see cref="Value"/> for a text field.</summary>
    public TextSelection? TextSelection { get; }

    /// <summary>The index of the first visible semantic child of a scroll node.</summary>
    public int? ScrollIndex { get; }

    /// <summary>The number of scrollable children contributing semantics.</summary>
    public int? ScrollChildCount { get; }

    /// <summary>The current scroll offset in logical pixels.</summary>
    public double? ScrollPosition { get; }

    /// <summary>The maximum in-range scroll position.</summary>
    public double? ScrollExtentMax { get; }

    /// <summary>The minimum in-range scroll position.</summary>
    public double? ScrollExtentMin { get; }

    /// <summary>The platform view whose semantics replace this node's children.</summary>
    public int? PlatformViewId { get; }

    /// <summary>The maximum number of characters an editable field accepts.</summary>
    public int? MaxValueLength { get; }

    /// <summary>The number of characters currently entered.</summary>
    public int? CurrentValueLength { get; }

    /// <summary>0 when this node is not a heading, otherwise 1 through 6.</summary>
    public int HeadingLevel { get; }

    /// <summary>The URL this node links to.</summary>
    public Uri? LinkUrl { get; }

    /// <summary>The node's role.</summary>
    public SemanticsRole Role { get; }

    /// <summary>The identifiers of the nodes this node controls.</summary>
    public IReadOnlySet<string>? ControlsNodes { get; }

    /// <summary>Whether this node's content passed validation.</summary>
    public SemanticsValidationResult ValidationResult { get; }

    /// <summary>How this node takes part in semantic hit testing.</summary>
    public SemanticsHitTestBehavior HitTestBehavior { get; }

    /// <summary>The kind of input this node accepts.</summary>
    public SemanticsInputType InputType { get; }

    /// <summary>The locale assistive technologies interpret this node's content in.</summary>
    public Locale? Locale { get; }

    /// <summary>The smallest value a range control accepts.</summary>
    public string? MinValue { get; }

    /// <summary>The largest value a range control accepts.</summary>
    public string? MaxValue { get; }

    /// <summary>The tags on this node, unioned with those of every merged descendant.</summary>
    public IReadOnlySet<SemanticsTag>? Tags { get; }

    /// <summary>This node's coordinate system relative to its parent's; <c>null</c> means identity.</summary>
    public Matrix4? Transform { get; }

    /// <summary>The ids of the custom actions this node exposes, in increasing order.</summary>
    public IReadOnlyList<int>? CustomSemanticsActionIds { get; }

    /// <summary>The plain text of <see cref="AttributedLabel"/>.</summary>
    public string Label => AttributedLabel.String;

    /// <summary>The plain text of <see cref="AttributedValue"/>.</summary>
    public string Value => AttributedValue.String;

    /// <summary>The plain text of <see cref="AttributedIncreasedValue"/>.</summary>
    public string IncreasedValue => AttributedIncreasedValue.String;

    /// <summary>The plain text of <see cref="AttributedDecreasedValue"/>.</summary>
    public string DecreasedValue => AttributedDecreasedValue.String;

    /// <summary>The plain text of <see cref="AttributedHint"/>.</summary>
    public string Hint => AttributedHint.String;

    /// <remarks>Flutter's <c>SemanticsData.hasFlag</c>.</remarks>
    public bool HasFlag(SemanticsFlags flag) => (Flags & flag) != SemanticsFlags.None;

    /// <remarks>Flutter's <c>SemanticsData.hasAction</c>.</remarks>
    public bool HasAction(SemanticsActions action) => (Actions & action) != SemanticsActions.None;

    /// <inheritdoc />
    public override string ToStringShort() => Diagnostics.ObjectRuntimeType(this, nameof(SemanticsData));

    /// <inheritdoc />
    /// <remarks>
    /// Dart dereferences <c>customSemanticsActionIds!</c> here, so a hand-built
    /// <c>SemanticsData</c> without ids throws on <c>toString()</c>; an empty list is used instead.
    /// </remarks>
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<Rect>("rect", Rect, showName: false));
        properties.Add(new TransformProperty("transform", Transform, showName: false, defaultValue: null));
        List<string> actionSummary = [.. DescribeEnumFlags(Actions)];
        IEnumerable<string?> customSemanticsActionSummary = (CustomSemanticsActionIds ?? [])
            .Select(static id => CustomSemanticsAction.GetAction(id)?.Label);
        properties.Add(new IterableProperty<string>("actions", actionSummary, ifEmpty: null));
        properties.Add(
            new IterableProperty<string?>("customActions", customSemanticsActionSummary, ifEmpty: null));
        List<string> flagSummary = [.. DescribeEnumFlags(Flags)];
        properties.Add(new IterableProperty<string>("flags", flagSummary, ifEmpty: null));
        properties.Add(new StringProperty("identifier", Identifier, defaultValue: string.Empty));
        properties.Add(new DiagnosticsProperty<object>(
            "traversalParentIdentifier",
            TraversalParentIdentifier,
            defaultValue: null));
        properties.Add(new DiagnosticsProperty<object>(
            "traversalChildIdentifier",
            TraversalChildIdentifier,
            defaultValue: null));
        properties.Add(new AttributedStringProperty("label", AttributedLabel));
        properties.Add(new AttributedStringProperty("value", AttributedValue));
        properties.Add(new AttributedStringProperty("increasedValue", AttributedIncreasedValue));
        properties.Add(new AttributedStringProperty("decreasedValue", AttributedDecreasedValue));
        properties.Add(new AttributedStringProperty("hint", AttributedHint));
        properties.Add(new StringProperty("tooltip", Tooltip, defaultValue: string.Empty));
        properties.Add(new EnumProperty<TextDirection>("textDirection", TextDirection, defaultValue: null));
        if (TextSelection is { IsValid: true } selection)
        {
            properties.Add(new MessageProperty("textSelection", $"[{selection.Start}, {selection.End}]"));
        }

        properties.Add(new IntProperty("platformViewId", PlatformViewId, defaultValue: null));
        properties.Add(new IntProperty("maxValueLength", MaxValueLength, defaultValue: null));
        properties.Add(new IntProperty("currentValueLength", CurrentValueLength, defaultValue: null));
        properties.Add(new IntProperty("scrollChildren", ScrollChildCount, defaultValue: null));
        properties.Add(new IntProperty("scrollIndex", ScrollIndex, defaultValue: null));
        properties.Add(new DoubleProperty("scrollExtentMin", ScrollExtentMin, defaultValue: null));
        properties.Add(new DoubleProperty("scrollPosition", ScrollPosition, defaultValue: null));
        properties.Add(new DoubleProperty("scrollExtentMax", ScrollExtentMax, defaultValue: null));
        properties.Add(new IntProperty("headingLevel", HeadingLevel, defaultValue: 0));
        properties.Add(new DiagnosticsProperty<Uri>("linkUrl", LinkUrl, defaultValue: null));
        if (ControlsNodes is not null)
        {
            properties.Add(new IterableProperty<string>("controls", ControlsNodes, ifEmpty: null));
        }

        if (Role != SemanticsRole.None)
        {
            properties.Add(new EnumProperty<SemanticsRole>("role", Role, defaultValue: SemanticsRole.None));
        }

        if (ValidationResult != SemanticsValidationResult.None)
        {
            properties.Add(new EnumProperty<SemanticsValidationResult>(
                "validationResult",
                ValidationResult,
                defaultValue: SemanticsValidationResult.None));
        }

        properties.Add(new StringProperty("minValue", MinValue, defaultValue: null));
        properties.Add(new StringProperty("maxValue", MaxValue, defaultValue: null));
    }

    /// <summary>
    /// The names of the flags set in <paramref name="value"/>, in declaration order.
    /// </summary>
    /// <remarks>
    /// Dart reads these off the engine's <c>SemanticsFlags.toStrings()</c> and
    /// <c>SemanticsAction.values</c>; Plumix models both as <c>[Flags]</c> enums, so the names come
    /// from the enum instead.
    /// </remarks>
    private static IEnumerable<string> DescribeEnumFlags<T>(T value)
        where T : struct, Enum
    {
        long bits = Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);
        foreach (T candidate in Enum.GetValues<T>())
        {
            long bit = Convert.ToInt64(candidate, System.Globalization.CultureInfo.InvariantCulture);
            if (bit != 0 && (bits & bit) == bit)
            {
                yield return Diagnostics.EnumName(candidate);
            }
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Flutter's <c>operator ==</c>; <see cref="Locale"/> takes no part, exactly as in Dart.
    /// </remarks>
    public bool Equals(SemanticsData? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is not null
               && Flags == other.Flags
               && Actions == other.Actions
               && Identifier == other.Identifier
               && Equals(TraversalParentIdentifier, other.TraversalParentIdentifier)
               && Equals(TraversalChildIdentifier, other.TraversalChildIdentifier)
               && Equals(AttributedLabel, other.AttributedLabel)
               && Equals(AttributedValue, other.AttributedValue)
               && Equals(AttributedIncreasedValue, other.AttributedIncreasedValue)
               && Equals(AttributedDecreasedValue, other.AttributedDecreasedValue)
               && Equals(AttributedHint, other.AttributedHint)
               && Tooltip == other.Tooltip
               && TextDirection == other.TextDirection
               && Rect == other.Rect
               && SetsEqual(Tags, other.Tags)
               && ScrollChildCount == other.ScrollChildCount
               && ScrollIndex == other.ScrollIndex
               && TextSelection == other.TextSelection
               && ScrollPosition.Equals(other.ScrollPosition)
               && ScrollExtentMax.Equals(other.ScrollExtentMax)
               && ScrollExtentMin.Equals(other.ScrollExtentMin)
               && PlatformViewId == other.PlatformViewId
               && MaxValueLength == other.MaxValueLength
               && CurrentValueLength == other.CurrentValueLength
               && Equals(Transform, other.Transform)
               && HeadingLevel == other.HeadingLevel
               && LinkUrl == other.LinkUrl
               && Role == other.Role
               && ValidationResult == other.ValidationResult
               && InputType == other.InputType
               && HitTestBehavior == other.HitTestBehavior
               && SortedListsEqual(CustomSemanticsActionIds, other.CustomSemanticsActionIds)
               && SetsEqual(ControlsNodes, other.ControlsNodes)
               && MinValue == other.MinValue
               && MaxValue == other.MaxValue;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as SemanticsData);

    /// <inheritdoc />
    /// <remarks>
    /// Dart hashes <c>tags</c> by object identity and <c>controlsNodes</c> in list order while
    /// comparing both with <c>setEquals</c>; the two sets are hashed by size here so that equal
    /// values always hash equally. <see cref="Locale"/> is not hashed, as in Dart.
    /// </remarks>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Flags);
        hash.Add(Actions);
        hash.Add(Identifier);
        hash.Add(AttributedLabel);
        hash.Add(AttributedValue);
        hash.Add(AttributedIncreasedValue);
        hash.Add(AttributedDecreasedValue);
        hash.Add(AttributedHint);
        hash.Add(Tooltip);
        hash.Add(TextDirection);
        hash.Add(Rect);
        hash.Add(Tags?.Count ?? 0);
        hash.Add(TextSelection);
        hash.Add(ScrollChildCount);
        hash.Add(ScrollIndex);
        hash.Add(ScrollPosition);
        hash.Add(ScrollExtentMax);
        hash.Add(ScrollExtentMin);
        hash.Add(PlatformViewId);
        hash.Add(MaxValueLength);
        hash.Add(CurrentValueLength);
        hash.Add(Transform);
        hash.Add(HeadingLevel);
        hash.Add(LinkUrl);
        foreach (int id in CustomSemanticsActionIds ?? [])
        {
            hash.Add(id);
        }

        hash.Add(Role);
        hash.Add(ValidationResult);
        hash.Add(ControlsNodes?.Count ?? 0);
        hash.Add(InputType);
        hash.Add(HitTestBehavior);
        hash.Add(TraversalParentIdentifier);
        hash.Add(TraversalChildIdentifier);
        hash.Add(MinValue);
        hash.Add(MaxValue);
        return hash.ToHashCode();
    }

    /// <remarks>Flutter's private <c>SemanticsData._sortedListsEqual</c>: order matters.</remarks>
    private static bool SortedListsEqual(IReadOnlyList<int>? left, IReadOnlyList<int>? right)
    {
        if (left is null && right is null)
        {
            return true;
        }

        if (left is null || right is null || left.Count != right.Count)
        {
            return false;
        }

        for (int index = 0; index < left.Count; index += 1)
        {
            if (left[index] != right[index])
            {
                return false;
            }
        }

        return true;
    }

    /// <remarks>Flutter's <c>setEquals</c> from `foundation/collections.dart`.</remarks>
    private static bool SetsEqual<T>(IReadOnlySet<T>? left, IReadOnlySet<T>? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null || left.Count != right.Count)
        {
            return false;
        }

        return left.All(right.Contains);
    }
}

public sealed partial class SemanticsNode
{
    /// <summary>
    /// Flattens this node's annotations, merging in every descendant when
    /// <see cref="MergeAllDescendantsIntoThisNode"/> is set.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>SemanticsNode.getSemanticsData</c>. Nothing is cached: every call recomputes the
    /// walk, exactly as Dart does.
    /// </remarks>
    public SemanticsData GetSemanticsData()
    {
        SemanticsFlags flags = Flags;

        // The action bits are filtered for this node at the very end, because the filtering has to
        // happen after its descendants have been merged in.
        SemanticsActions actions = Actions;
        string identifier = Identifier;
        object? traversalParentIdentifier = TraversalParentIdentifier;
        object? traversalChildIdentifier = TraversalChildIdentifier;
        AttributedString attributedLabel = AttributedLabel;
        AttributedString attributedValue = AttributedValue;
        AttributedString attributedIncreasedValue = AttributedIncreasedValue;
        AttributedString attributedDecreasedValue = AttributedDecreasedValue;
        AttributedString attributedHint = AttributedHint;
        string tooltip = Tooltip;
        TextDirection? textDirection = TextDirection;
        HashSet<SemanticsTag>? mergedTags = _tags is null ? null : [.. _tags];
        TextSelection? textSelection = TextSelection;
        int? scrollChildCount = ScrollChildCount;
        int? scrollIndex = ScrollIndex;
        double? scrollPosition = ScrollPosition;
        double? scrollExtentMax = ScrollExtentMax;
        double? scrollExtentMin = ScrollExtentMin;
        int? platformViewId = PlatformViewId;
        int? maxValueLength = MaxValueLength;
        int? currentValueLength = CurrentValueLength;
        int headingLevel = HeadingLevel;
        Uri? linkUrl = LinkUrl;
        SemanticsRole role = Role;
        IReadOnlySet<string>? controlsNodes = ControlsNodes;
        SemanticsValidationResult validationResult = ValidationResult;
        SemanticsHitTestBehavior hitTestBehavior = HitTestBehavior;
        SemanticsInputType inputType = InputType;

        // The locale is never taken from a merged descendant: `Semantics.localeForSubtree` already
        // makes two subtrees with different locales incompatible.
        Locale? locale = Locale;
        var customSemanticsActionIds = new HashSet<int>();
        string? minValue = MinValue;
        string? maxValue = MaxValue;

        CollectCustomActionIds(customSemanticsActionIds, _customActionHandlers, HintOverrides);

        if (MergeAllDescendantsIntoThisNode)
        {
            VisitDescendants(node =>
            {
                Debug.Assert(node.IsMergedIntoParent);
                flags |= node.Flags;
                actions |= node.Actions;
                textDirection ??= node.TextDirection;
                textSelection ??= node.TextSelection;
                scrollChildCount ??= node.ScrollChildCount;
                scrollIndex ??= node.ScrollIndex;
                scrollPosition ??= node.ScrollPosition;
                scrollExtentMax ??= node.ScrollExtentMax;
                scrollExtentMin ??= node.ScrollExtentMin;
                platformViewId ??= node.PlatformViewId;
                maxValueLength ??= node.MaxValueLength;
                currentValueLength ??= node.CurrentValueLength;
                linkUrl ??= node.LinkUrl;
                headingLevel = SemanticsConfiguration.MergeHeadingLevels(
                    sourceLevel: node.HeadingLevel,
                    targetLevel: headingLevel);
                if (string.IsNullOrEmpty(identifier))
                {
                    identifier = node.Identifier;
                }

                traversalParentIdentifier ??= node.TraversalParentIdentifier;
                traversalChildIdentifier ??= node.TraversalChildIdentifier;
                if (IsEmptyAttributedString(attributedValue))
                {
                    attributedValue = node.AttributedValue;
                }

                if (IsEmptyAttributedString(attributedIncreasedValue))
                {
                    attributedIncreasedValue = node.AttributedIncreasedValue;
                }

                if (IsEmptyAttributedString(attributedDecreasedValue))
                {
                    attributedDecreasedValue = node.AttributedDecreasedValue;
                }

                if (role == SemanticsRole.None)
                {
                    role = node.Role;
                }

                if (inputType == SemanticsInputType.None)
                {
                    inputType = node.InputType;
                }

                if (hitTestBehavior == SemanticsHitTestBehavior.Defer)
                {
                    hitTestBehavior = node.HitTestBehavior;
                }

                if (string.IsNullOrEmpty(tooltip))
                {
                    tooltip = node.Tooltip;
                }

                if (node.Tags is { Count: > 0 } nodeTags)
                {
                    mergedTags ??= [];
                    mergedTags.UnionWith(nodeTags);
                }

                CollectCustomActionIds(
                    customSemanticsActionIds,
                    node.CustomSemanticsActions,
                    node.HintOverrides);

                attributedLabel = SemanticsConfiguration.ConcatAttributedString(
                    attributedLabel,
                    textDirection,
                    node.AttributedLabel,
                    node.TextDirection);
                attributedHint = SemanticsConfiguration.ConcatAttributedString(
                    attributedHint,
                    textDirection,
                    node.AttributedHint,
                    node.TextDirection);
                if (controlsNodes is null)
                {
                    controlsNodes = node.ControlsNodes;
                }
                else if (node.ControlsNodes is not null)
                {
                    controlsNodes = new HashSet<string>(controlsNodes.Union(node.ControlsNodes));
                }

                minValue ??= node.MinValue;
                maxValue ??= node.MaxValue;
                if (validationResult == SemanticsValidationResult.None)
                {
                    validationResult = node.ValidationResult;
                }
                else if (validationResult == SemanticsValidationResult.Valid
                         && node.ValidationResult != SemanticsValidationResult.None
                         && node.ValidationResult != SemanticsValidationResult.Valid)
                {
                    // An invalid descendant wins, so validation information is never lost.
                    validationResult = node.ValidationResult;
                }

                return true;
            });
        }

        List<int> sortedActionIds = [.. customSemanticsActionIds];
        sortedActionIds.Sort();
        return new SemanticsData(
            flags: flags,
            actions: AreUserActionsBlocked
                ? actions & SemanticsConfiguration.UnblockedUserActions
                : actions,
            identifier: identifier,
            traversalParentIdentifier: traversalParentIdentifier,
            traversalChildIdentifier: traversalChildIdentifier,
            attributedLabel: attributedLabel,
            attributedValue: attributedValue,
            attributedIncreasedValue: attributedIncreasedValue,
            attributedDecreasedValue: attributedDecreasedValue,
            attributedHint: attributedHint,
            tooltip: tooltip,
            textDirection: textDirection,
            rect: Rect,
            textSelection: textSelection,
            scrollIndex: scrollIndex,
            scrollChildCount: scrollChildCount,
            scrollPosition: scrollPosition,
            scrollExtentMax: scrollExtentMax,
            scrollExtentMin: scrollExtentMin,
            platformViewId: platformViewId,
            maxValueLength: maxValueLength,
            currentValueLength: currentValueLength,
            headingLevel: headingLevel,
            linkUrl: linkUrl,
            role: role,
            controlsNodes: controlsNodes,
            validationResult: validationResult,
            hitTestBehavior: hitTestBehavior,
            inputType: inputType,
            locale: locale,
            minValue: minValue,
            maxValue: maxValue,
            tags: mergedTags,
            transform: Transform,
            customSemanticsActionIds: sortedActionIds);
    }

    private static bool IsEmptyAttributedString(AttributedString value) => value.String.Length == 0;

    /// <summary>
    /// Adds the ids of <paramref name="customActions"/> and of the two hint overrides to
    /// <paramref name="target"/>.
    /// </summary>
    /// <remarks>
    /// The block Flutter's <c>getSemanticsData</c> spells out once for the node itself and once per
    /// merged descendant.
    /// </remarks>
    private static void CollectCustomActionIds(
        HashSet<int> target,
        IReadOnlyDictionary<CustomSemanticsAction, Action> customActions,
        SemanticsHintOverrides? hintOverrides)
    {
        foreach (CustomSemanticsAction action in customActions.Keys)
        {
            target.Add(CustomSemanticsAction.GetIdentifier(action));
        }

        if (hintOverrides is null)
        {
            return;
        }

        if (hintOverrides.OnTapHint is { } onTapHint)
        {
            target.Add(CustomSemanticsAction.GetIdentifier(
                CustomSemanticsAction.OverridingAction(onTapHint, SemanticsActions.Tap)));
        }

        if (hintOverrides.OnLongPressHint is { } onLongPressHint)
        {
            target.Add(CustomSemanticsAction.GetIdentifier(
                CustomSemanticsAction.OverridingAction(onLongPressHint, SemanticsActions.LongPress)));
        }
    }
}
