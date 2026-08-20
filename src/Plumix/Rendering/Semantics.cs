using Avalonia;
using System.Text;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/semantics/semantics.dart

namespace Plumix.Rendering;

public abstract record SemanticsSortKey(string? Name) : IComparable<SemanticsSortKey>
{
    public abstract int CompareTo(SemanticsSortKey? other);
}

public sealed record OrdinalSortKey(double Order, string? GroupName = null)
    : SemanticsSortKey(GroupName)
{
    public override int CompareTo(SemanticsSortKey? other)
    {
        if (other is null)
        {
            return -1;
        }

        if (other is not OrdinalSortKey ordinal)
        {
            return string.Compare(GetType().FullName, other.GetType().FullName, StringComparison.Ordinal);
        }

        int nameComparison = string.Compare(Name, ordinal.Name, StringComparison.Ordinal);
        return nameComparison != 0 ? nameComparison : Order.CompareTo(ordinal.Order);
    }
}

/// <summary>
/// How a semantics node participates in the platform's accessibility hit testing.
/// </summary>
public enum SemanticsHitTestBehavior
{
    /// <summary>Defer to the platform's default hit-test behavior inference.</summary>
    Defer,

    /// <summary>Consume pointer events within the node's bounds, blocking nodes behind it.</summary>
    Opaque,

    /// <summary>Let pointer events pass through to the elements behind the node.</summary>
    Transparent,
}

public enum SemanticsInputType
{
    None,
    Text,
    Url,
    Phone,
    Search,
    Email,
}

[Flags]
public enum SemanticsFlags
{
    None = 0,
    IsButton = 1 << 0,
    IsEnabled = 1 << 1,
    IsSelected = 1 << 2,
    IsChecked = 1 << 3,
    IsTextField = 1 << 4,
    IsFocused = 1 << 5,
    IsHeader = 1 << 6,
    IsLink = 1 << 7,
    IsImage = 1 << 8,
    IsSlider = 1 << 9,
    IsHidden = 1 << 10,
    HasExpandedState = 1 << 11,
    IsExpanded = 1 << 12,
    IsInMutuallyExclusiveGroup = 1 << 13,
    IsLiveRegion = 1 << 14,
    IsDialog = 1 << 15,
    IsAlertDialog = 1 << 16,
    ScopesRoute = 1 << 17,
    NamesRoute = 1 << 18,
    HasCheckedState = 1 << 19,
    IsInvalid = 1 << 20,
    IsFocusable = 1 << 21,
    IsCheckStateMixed = 1 << 22,
    HasEnabledState = 1 << 23,
    HasSelectedState = 1 << 24,

    /// <summary>
    /// Whether the platform may scroll this node implicitly (for example when accessibility focus
    /// moves onto an offscreen descendant) instead of only through the explicit scroll actions.
    /// </summary>
    HasImplicitScrolling = 1 << 25,
    HasToggledState = 1 << 26,
    IsToggled = 1 << 27,
}

[Flags]
public enum SemanticsActions
{
    None = 0,
    Tap = 1 << 0,
    LongPress = 1 << 1,
    ScrollLeft = 1 << 2,
    ScrollRight = 1 << 3,
    ScrollUp = 1 << 4,
    ScrollDown = 1 << 5,
    Increase = 1 << 6,
    Decrease = 1 << 7,
    Focus = 1 << 8,
    Dismiss = 1 << 9,
    ShowOnScreen = 1 << 10,

    /// <summary>
    /// Move a scrollable to an absolute offset. The action argument carries the target offset.
    /// </summary>
    ScrollToOffset = 1 << 11,

    /// <summary>Expand a collapsed, expandable node (Flutter's <c>SemanticsAction.expand</c>).</summary>
    Expand = 1 << 12,

    /// <summary>Collapse an expanded node (Flutter's <c>SemanticsAction.collapse</c>).</summary>
    Collapse = 1 << 13,
}

/// <summary>
/// Signature for a semantics action handler. <paramref name="args"/> is <c>null</c> for the
/// argument-less actions and carries the action's payload otherwise.
/// </summary>
/// <remarks>Flutter's <c>SemanticsActionHandler</c>.</remarks>
public delegate void SemanticsActionHandler(object? args);

/// <summary>
/// Signature for <see cref="SemanticsConfiguration.OnScrollToOffset"/>.
/// </summary>
/// <remarks>Flutter's <c>ScrollToOffsetHandler</c>.</remarks>
public delegate void ScrollToOffsetHandler(Point targetOffset);

public sealed record CustomSemanticsAction
{
    public CustomSemanticsAction(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("A custom semantics action label cannot be empty.", nameof(label));
        }

        Label = label;
    }

    public string Label { get; }
}

/// A tag for a [SemanticsNode].
///
/// Tags can be interpreted by the parent of a [SemanticsNode] and depending on the presence of a tag
/// the parent can for example decide how to add the tagged node as a child.
///
/// Tags are compared by identity, exactly as in Flutter: two tags with the same name are distinct.
public sealed class SemanticsTag
{
    public SemanticsTag(string name)
    {
        Name = name;
    }

    /// A human-readable name for this tag used for debugging.
    public string Name { get; }

    public override string ToString() => $"{nameof(SemanticsTag)}({Name})";
}

public delegate ChildSemanticsConfigurationsResult ChildSemanticsConfigurationsDelegate(
    List<SemanticsConfiguration> childConfigurations);

public sealed class ChildSemanticsConfigurationsResult
{
    public ChildSemanticsConfigurationsResult(
        List<SemanticsConfiguration> mergeUp,
        List<List<SemanticsConfiguration>> siblingMergeGroups)
    {
        MergeUp = mergeUp;
        SiblingMergeGroups = siblingMergeGroups;
    }

    public List<SemanticsConfiguration> MergeUp { get; }

    public List<List<SemanticsConfiguration>> SiblingMergeGroups { get; }
}

/// The builder to build a [ChildSemanticsConfigurationsResult] based on its annotations.
public sealed class ChildSemanticsConfigurationsResultBuilder
{
    private readonly List<SemanticsConfiguration> _mergeUp = [];
    private readonly List<List<SemanticsConfiguration>> _siblingMergeGroups = [];

    /// Marks the [SemanticsConfiguration] to be merged into the parent semantics node.
    public void MarkAsMergeUp(SemanticsConfiguration config)
    {
        _mergeUp.Add(config);
    }

    /// Marks a group of [SemanticsConfiguration]s to merge into the same sibling node.
    public void MarkAsSiblingMergeGroup(List<SemanticsConfiguration> configs)
    {
        _siblingMergeGroups.Add(configs);
    }

    /// Builds a [ChildSemanticsConfigurationsResult] that contains the annotations.
    public ChildSemanticsConfigurationsResult Build()
    {
        return new ChildSemanticsConfigurationsResult([.. _mergeUp], [.. _siblingMergeGroups]);
    }
}

public sealed class SemanticsConfiguration
{
    public bool IsSemanticBoundary { get; set; }
    public bool IsMergingSemanticsOfDescendants { get; set; }
    public bool ExplicitChildNodes { get; set; }
    public bool IsBlockingSemanticsOfPreviouslyPaintedNodes { get; set; }
    public bool IsBlockingUserActions { get; set; }
    public ChildSemanticsConfigurationsDelegate? ChildConfigurationsDelegate { get; set; }
    public string? Label { get; set; }
    public string? Hint { get; set; }
    public string? OnTapHint { get; set; }
    public string? Tooltip { get; set; }
    public string? Value { get; set; }
    public string? IncreasedValue { get; set; }
    public string? DecreasedValue { get; set; }
    public string? MinValue { get; set; }
    public string? MaxValue { get; set; }
    public SemanticsRole Role { get; set; }
    public SemanticsInputType InputType { get; set; }
    public SemanticsHitTestBehavior HitTestBehavior { get; set; } = SemanticsHitTestBehavior.Defer;
    public SemanticsFlags Flags { get; set; } = SemanticsFlags.None;
    public SemanticsActions Actions { get; set; } = SemanticsActions.None;
    public int? IndexInParent { get; set; }
    public SemanticsSortKey? SortKey { get; set; }

    /// <summary>
    /// The reading direction for the text in <see cref="Label"/>, <see cref="Value"/>,
    /// <see cref="Hint"/> and friends, and the direction the default traversal sort walks siblings in.
    /// </summary>
    /// <remarks>Flutter's <c>SemanticsConfiguration.textDirection</c>.</remarks>
    public TextDirection? TextDirection { get; set; }

    /// <summary>
    /// Whether the node is currently not visible on screen but still part of the semantics tree.
    /// </summary>
    /// <remarks>Flutter's <c>SemanticsConfiguration.isHidden</c>, backed by the same flag.</remarks>
    public bool IsHidden
    {
        get => Flags.HasFlag(SemanticsFlags.IsHidden);
        set => Flags = value
            ? Flags | SemanticsFlags.IsHidden
            : Flags & ~SemanticsFlags.IsHidden;
    }

    private HashSet<SemanticsTag>? _tagsForChildren;

    /// The tags that this configuration attaches to the semantics nodes created below it.
    public IReadOnlyCollection<SemanticsTag>? TagsForChildren => _tagsForChildren;

    /// Whether the child semantics nodes of this configuration are tagged with `tag`.
    public bool TagsChildrenWith(SemanticsTag tag) => _tagsForChildren?.Contains(tag) ?? false;

    /// Tags all child semantics nodes with `tag`.
    public void AddTagForChildren(SemanticsTag tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        _tagsForChildren ??= [];
        _tagsForChildren.Add(tag);
    }

    private Dictionary<SemanticsActions, SemanticsActionHandler>? _actionHandlers;
    private Dictionary<CustomSemanticsAction, Action>? _customActionHandlers;
    internal bool HasActionHandlers => _actionHandlers is { Count: > 0 };
    internal bool HasCustomActionHandlers => _customActionHandlers is { Count: > 0 };
    internal IReadOnlyDictionary<SemanticsActions, SemanticsActionHandler> ActionHandlers =>
        _actionHandlers ?? EmptyHandlers;
    internal IReadOnlyDictionary<CustomSemanticsAction, Action> CustomActionHandlers =>
        _customActionHandlers ?? EmptyCustomHandlers;

    private static readonly IReadOnlyDictionary<SemanticsActions, SemanticsActionHandler> EmptyHandlers =
        new Dictionary<SemanticsActions, SemanticsActionHandler>();
    private static readonly IReadOnlyDictionary<CustomSemanticsAction, Action> EmptyCustomHandlers =
        new Dictionary<CustomSemanticsAction, Action>();

    /// <remarks>Flutter's <c>SemanticsConfiguration._addArgumentlessAction</c>.</remarks>
    public void AddActionHandler(SemanticsActions action, Action handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        AddActionHandler(action, _ => handler());
    }

    /// <remarks>Flutter's <c>SemanticsConfiguration._addAction</c>.</remarks>
    public void AddActionHandler(SemanticsActions action, SemanticsActionHandler handler)
    {
        if (action == SemanticsActions.None)
        {
            throw new ArgumentException("Action handler cannot be registered for SemanticsActions.None.");
        }

        ArgumentNullException.ThrowIfNull(handler);
        _actionHandlers ??= [];
        _actionHandlers[action] = handler;
        Actions |= action;
    }

    /// <summary>
    /// The current scroll position in logical pixels, or <c>null</c> when this configuration does not
    /// describe a scrollable. <see cref="ScrollExtentMin"/> and <see cref="ScrollExtentMax"/> bound it.
    /// </summary>
    public double? ScrollPosition { get; set; }

    /// <summary>The maximum in-range value for <see cref="ScrollPosition"/>.</summary>
    public double? ScrollExtentMax { get; set; }

    /// <summary>The minimum in-range value for <see cref="ScrollPosition"/>.</summary>
    public double? ScrollExtentMin { get; set; }

    /// <summary>
    /// The total number of scrollable children, or <c>null</c> when the count is unknown or unbounded.
    /// </summary>
    public int? ScrollChildCount { get; set; }

    /// <summary>The index of the first visible scrollable child.</summary>
    public int? ScrollIndex { get; set; }

    /// <summary>
    /// Whether the platform may scroll this node without an explicit scroll action, for example to
    /// follow accessibility focus onto an offscreen child.
    /// </summary>
    public bool HasImplicitScrolling
    {
        get => Flags.HasFlag(SemanticsFlags.HasImplicitScrolling);
        set => Flags = value
            ? Flags | SemanticsFlags.HasImplicitScrolling
            : Flags & ~SemanticsFlags.HasImplicitScrolling;
    }

    public Action? OnScrollLeft
    {
        get => _onScrollLeft;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            AddActionHandler(SemanticsActions.ScrollLeft, value);
            _onScrollLeft = value;
        }
    }

    public Action? OnScrollRight
    {
        get => _onScrollRight;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            AddActionHandler(SemanticsActions.ScrollRight, value);
            _onScrollRight = value;
        }
    }

    public Action? OnScrollUp
    {
        get => _onScrollUp;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            AddActionHandler(SemanticsActions.ScrollUp, value);
            _onScrollUp = value;
        }
    }

    public Action? OnScrollDown
    {
        get => _onScrollDown;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            AddActionHandler(SemanticsActions.ScrollDown, value);
            _onScrollDown = value;
        }
    }

    /// <summary>
    /// Moves the scrollable to an absolute offset. The action argument is the target
    /// <see cref="Point"/> (a host bridge may also pass a two-element <c>double</c> list).
    /// </summary>
    public ScrollToOffsetHandler? OnScrollToOffset
    {
        get => _onScrollToOffset;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            AddActionHandler(SemanticsActions.ScrollToOffset, args => value(ResolveOffsetArgument(args)));
            _onScrollToOffset = value;
        }
    }

    private Action? _onScrollLeft;
    private Action? _onScrollRight;
    private Action? _onScrollUp;
    private Action? _onScrollDown;
    private ScrollToOffsetHandler? _onScrollToOffset;

    private static Point ResolveOffsetArgument(object? args)
    {
        return args switch
        {
            Point point => point,
            IReadOnlyList<double> { Count: >= 2 } list => new Point(list[0], list[1]),
            _ => throw new ArgumentException(
                "SemanticsActions.ScrollToOffset requires a Point or a two-element double list.",
                nameof(args))
        };
    }

    /// <summary>
    /// An explicit handler for <see cref="SemanticsActions.ShowOnScreen"/>, which replaces the
    /// node's default "ask my render object to reveal itself" behavior.
    /// </summary>
    public Action? OnShowOnScreen
    {
        get => _onShowOnScreen;
        set
        {
            _onShowOnScreen = value;
            if (value is null)
            {
                _actionHandlers?.Remove(SemanticsActions.ShowOnScreen);
                Actions &= ~SemanticsActions.ShowOnScreen;
                return;
            }

            AddActionHandler(SemanticsActions.ShowOnScreen, value);
        }
    }

    private Action? _onShowOnScreen;

    public void AddCustomActionHandler(CustomSemanticsAction action, Action handler)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(handler);
        _customActionHandlers ??= [];
        _customActionHandlers[action] = handler;
    }

    internal void ReplaceActionHandlers(Dictionary<SemanticsActions, SemanticsActionHandler> handlers)
    {
        _actionHandlers = handlers.Count == 0 ? null : handlers;
    }

    internal void ReplaceCustomActionHandlers(Dictionary<CustomSemanticsAction, Action> handlers)
    {
        _customActionHandlers = handlers.Count == 0 ? null : handlers;
    }

    internal SemanticsConfiguration Clone()
    {
        var clone = new SemanticsConfiguration
        {
            IsSemanticBoundary = IsSemanticBoundary,
            IsMergingSemanticsOfDescendants = IsMergingSemanticsOfDescendants,
            ExplicitChildNodes = ExplicitChildNodes,
            IsBlockingSemanticsOfPreviouslyPaintedNodes = IsBlockingSemanticsOfPreviouslyPaintedNodes,
            IsBlockingUserActions = IsBlockingUserActions,
            ChildConfigurationsDelegate = ChildConfigurationsDelegate,
            Label = Label,
            Hint = Hint,
            OnTapHint = OnTapHint,
            Tooltip = Tooltip,
            Value = Value,
            IncreasedValue = IncreasedValue,
            DecreasedValue = DecreasedValue,
            MinValue = MinValue,
            MaxValue = MaxValue,
            Role = Role,
            InputType = InputType,
            HitTestBehavior = HitTestBehavior,
            Flags = Flags,
            Actions = Actions,
            IndexInParent = IndexInParent,
            SortKey = SortKey,
            TextDirection = TextDirection,
            ScrollPosition = ScrollPosition,
            ScrollExtentMax = ScrollExtentMax,
            ScrollExtentMin = ScrollExtentMin,
            ScrollChildCount = ScrollChildCount,
            ScrollIndex = ScrollIndex,
            _onScrollLeft = _onScrollLeft,
            _onScrollRight = _onScrollRight,
            _onScrollUp = _onScrollUp,
            _onScrollDown = _onScrollDown,
            _onScrollToOffset = _onScrollToOffset,
            _onShowOnScreen = _onShowOnScreen
        };

        if (_tagsForChildren is { Count: > 0 })
        {
            clone._tagsForChildren = [.. _tagsForChildren];
        }

        if (_actionHandlers is { Count: > 0 })
        {
            clone._actionHandlers = new Dictionary<SemanticsActions, SemanticsActionHandler>(_actionHandlers);
        }

        if (_customActionHandlers is { Count: > 0 })
        {
            clone._customActionHandlers = new Dictionary<CustomSemanticsAction, Action>(_customActionHandlers);
        }

        return clone;
    }

    internal void ClearActionHandlers()
    {
        Actions = SemanticsActions.None;
        _actionHandlers = null;
        _customActionHandlers = null;
        _onScrollLeft = null;
        _onScrollRight = null;
        _onScrollUp = null;
        _onScrollDown = null;
        _onScrollToOffset = null;
        _onShowOnScreen = null;
    }

    /// <summary>The shared empty configuration Flutter calls <c>_kEmptyConfig</c>.</summary>
    internal static SemanticsConfiguration Empty { get; } = new();

    internal bool HasBeenAnnotated =>
        !string.IsNullOrWhiteSpace(Label)
        || !string.IsNullOrWhiteSpace(Hint)
        || !string.IsNullOrWhiteSpace(OnTapHint)
        || !string.IsNullOrWhiteSpace(Tooltip)
        || !string.IsNullOrWhiteSpace(Value)
        || !string.IsNullOrWhiteSpace(IncreasedValue)
        || !string.IsNullOrWhiteSpace(DecreasedValue)
        || !string.IsNullOrWhiteSpace(MinValue)
        || !string.IsNullOrWhiteSpace(MaxValue)
        || Role != SemanticsRole.None
        || InputType != SemanticsInputType.None
        || HitTestBehavior != SemanticsHitTestBehavior.Defer
        || Flags != SemanticsFlags.None
        || Actions != SemanticsActions.None
        || TextDirection.HasValue
        || SortKey is not null
        || IndexInParent.HasValue
        || ScrollPosition.HasValue
        || ScrollExtentMax.HasValue
        || ScrollExtentMin.HasValue
        || ScrollChildCount.HasValue
        || ScrollIndex.HasValue
        || HasActionHandlers
        || HasCustomActionHandlers;

    internal bool IsCompatibleWith(SemanticsConfiguration? other)
    {
        if (other == null || !other.HasBeenAnnotated || !HasBeenAnnotated)
        {
            return true;
        }

        if ((Actions & other.Actions) != SemanticsActions.None)
        {
            return false;
        }

        if (CustomActionHandlers.Keys.Any(other.CustomActionHandlers.ContainsKey))
        {
            return false;
        }

        if ((Flags & other.Flags) != SemanticsFlags.None)
        {
            return false;
        }

        if (Role != SemanticsRole.None
            && other.Role != SemanticsRole.None
            && Role != other.Role)
        {
            return false;
        }

        if (InputType != SemanticsInputType.None
            && other.InputType != SemanticsInputType.None
            && InputType != other.InputType)
        {
            return false;
        }

        if (HitTestBehavior != SemanticsHitTestBehavior.Defer
            || other.HitTestBehavior != SemanticsHitTestBehavior.Defer)
        {
            return false;
        }

        return true;
    }

    internal void Absorb(SemanticsConfiguration child)
    {
        if (ExplicitChildNodes)
        {
            return;
        }

        if (!child.HasBeenAnnotated)
        {
            return;
        }

        Flags |= child.Flags;
        Actions |= child.Actions;
        TextDirection ??= child.TextDirection;
        IndexInParent ??= child.IndexInParent;
        SortKey ??= child.SortKey;
        ScrollPosition ??= child.ScrollPosition;
        ScrollExtentMax ??= child.ScrollExtentMax;
        ScrollExtentMin ??= child.ScrollExtentMin;
        ScrollChildCount ??= child.ScrollChildCount;
        ScrollIndex ??= child.ScrollIndex;
        _onScrollLeft ??= child._onScrollLeft;
        _onScrollRight ??= child._onScrollRight;
        _onScrollUp ??= child._onScrollUp;
        _onScrollDown ??= child._onScrollDown;
        _onScrollToOffset ??= child._onScrollToOffset;
        _onShowOnScreen ??= child._onShowOnScreen;
        if (Role == SemanticsRole.None)
        {
            Role = child.Role;
        }
        if (InputType == SemanticsInputType.None)
        {
            InputType = child.InputType;
        }

        if (HitTestBehavior == SemanticsHitTestBehavior.Defer
            && child.HitTestBehavior != SemanticsHitTestBehavior.Defer)
        {
            HitTestBehavior = child.HitTestBehavior;
        }

        // Flutter's `_concatAttributedString` separates the two labels with a newline.
        if (!string.IsNullOrWhiteSpace(child.Label))
        {
            Label = string.IsNullOrWhiteSpace(Label) ? child.Label : $"{Label}\n{child.Label}";
        }

        Value ??= child.Value;
        IncreasedValue ??= child.IncreasedValue;
        DecreasedValue ??= child.DecreasedValue;
        MinValue ??= child.MinValue;
        MaxValue ??= child.MaxValue;

        if (!string.IsNullOrWhiteSpace(child.Hint))
        {
            Hint = string.IsNullOrWhiteSpace(Hint) ? child.Hint : $"{Hint}\n{child.Hint}";
        }

        OnTapHint ??= child.OnTapHint;

        if (!string.IsNullOrWhiteSpace(child.Tooltip))
        {
            Tooltip = string.IsNullOrWhiteSpace(Tooltip)
                ? child.Tooltip
                : $"{Tooltip}\n{child.Tooltip}";
        }

        if (child.HasActionHandlers)
        {
            _actionHandlers ??= [];
            foreach (var pair in child.ActionHandlers)
            {
                _actionHandlers.TryAdd(pair.Key, pair.Value);
            }
        }


        if (child.HasCustomActionHandlers)
        {
            _customActionHandlers ??= [];
            foreach (var pair in child.CustomActionHandlers)
            {
                _customActionHandlers.TryAdd(pair.Key, pair.Value);
            }
        }
    }
}

public sealed class SemanticsNode
{
    private readonly List<SemanticsNode> _children = [];
    private readonly Dictionary<SemanticsActions, SemanticsActionHandler> _actionHandlers = [];
    private readonly Dictionary<CustomSemanticsAction, Action> _customActionHandlers = [];

    internal SemanticsNode(int id, string? debugOwner = null)
    {
        Id = id;
        DebugOwner = debugOwner;
    }

    /// <summary>The name of the render object that produced this node, for diagnostics.</summary>
    /// <remarks>Flutter's <c>SemanticsNode.debugOwner</c>.</remarks>
    public string? DebugOwner { get; }

    public int Id { get; }

    /// <summary>
    /// Scrolls this node into view when nothing registered an explicit
    /// <see cref="SemanticsActions.ShowOnScreen"/> handler.
    /// </summary>
    /// <remarks>Flutter's private <c>SemanticsNode._showOnScreen</c>.</remarks>
    internal Action? ShowOnScreenRequest { get; set; }

    /// <summary>The bounding box for this node in <em>its own</em> coordinate system.</summary>
    /// <remarks>
    /// Flutter's <c>SemanticsNode.rect</c>. Use <see cref="Transform"/> to map it into the parent
    /// node's coordinates, or <see cref="GlobalRect"/> to resolve it all the way to the root.
    /// </remarks>
    public Rect Rect { get; set; }

    /// <summary>
    /// The transform from this node's coordinate system to its parent's, or <c>null</c> for the
    /// identity transform.
    /// </summary>
    /// <remarks>Flutter's <c>SemanticsNode.transform</c>.</remarks>
    public Matrix4? Transform
    {
        get => _transform;
        set => _transform = value is null || MatrixUtils.IsIdentity(value) ? null : value;
    }

    private Matrix4? _transform;

    /// <summary>The semantic clip an ancestor applied, in this node's coordinate system.</summary>
    public Rect? ParentSemanticsClipRect { get; internal set; }

    /// <summary>The paint clip an ancestor applied, in this node's coordinate system.</summary>
    public Rect? ParentPaintClipRect { get; internal set; }

    /// <summary>Whether this node merges its information into an ancestor node.</summary>
    /// <remarks>Flutter's <c>SemanticsNode.isMergedIntoParent</c>.</remarks>
    public bool IsMergedIntoParent { get; internal set; }

    /// <summary>The parent of this node in the semantics tree, or <c>null</c> for the root.</summary>
    public SemanticsNode? Parent { get; private set; }

    /// <summary>
    /// Whether this node has zero area or a degenerate transform, in which case it is dropped from
    /// the compiled tree.
    /// </summary>
    /// <remarks>Flutter's <c>SemanticsNode.isInvisible</c>.</remarks>
    public bool IsInvisible =>
        !IsMergedIntoParent && (Rect.Width <= 0 || Rect.Height <= 0 || IsZeroTransform(_transform));

    /// <summary>This node's <see cref="Rect"/> resolved into the root node's coordinate system.</summary>
    /// <remarks>
    /// Flutter has no such accessor — its consumers compose <see cref="Transform"/> themselves while
    /// walking down from the root. Plumix keeps it because callers and tests routinely want the
    /// absolute box of a single node.
    /// </remarks>
    public Rect GlobalRect
    {
        get
        {
            Matrix4 transform = Matrix4.Identity();
            for (SemanticsNode? node = this; node != null; node = node.Parent)
            {
                if (node._transform is { } nodeTransform)
                {
                    // Ancestors sit to the left of descendants in Flutter's column-vector convention.
                    MatrixUtils.MultiplyInPlace(nodeTransform, transform);
                }
            }

            return TransformRect(transform, Rect);
        }
    }

    internal static Rect TransformRect(Matrix4 transform, Rect rect)
    {
        if (MatrixUtils.IsIdentity(transform))
        {
            return rect;
        }

        return MatrixUtils.TransformRect(transform, rect);
    }

    private static bool IsZeroTransform(Matrix4? transform) => transform is { } value && value.IsZero();

    public string? Label { get; internal set; }
    public string? Hint { get; internal set; }
    public string? OnTapHint { get; internal set; }
    public string? Tooltip { get; internal set; }
    public string? Value { get; internal set; }
    public string? IncreasedValue { get; internal set; }
    public string? DecreasedValue { get; internal set; }
    public string? MinValue { get; internal set; }
    public string? MaxValue { get; internal set; }
    public SemanticsRole Role { get; internal set; }
    public SemanticsInputType InputType { get; internal set; }
    public SemanticsHitTestBehavior HitTestBehavior { get; internal set; } = SemanticsHitTestBehavior.Defer;
    public SemanticsFlags Flags { get; internal set; }
    public SemanticsActions Actions { get; internal set; }
    public int? IndexInParent { get; set; }
    public SemanticsSortKey? SortKey { get; internal set; }

    /// <summary>The current scroll position in logical pixels, or <c>null</c> when not scrollable.</summary>
    public double? ScrollPosition { get; internal set; }

    /// <summary>The maximum in-range value for <see cref="ScrollPosition"/>.</summary>
    public double? ScrollExtentMax { get; internal set; }

    /// <summary>The minimum in-range value for <see cref="ScrollPosition"/>.</summary>
    public double? ScrollExtentMin { get; internal set; }

    /// <summary>The total number of scrollable children, <c>null</c> when unknown or unbounded.</summary>
    public int? ScrollChildCount { get; internal set; }

    /// <summary>The index of the first visible scrollable child.</summary>
    public int? ScrollIndex { get; internal set; }

    /// The tags the render objects between this node and its parent node attached to it, through
    /// their configurations' `AddTagForChildren`.
    public IReadOnlyCollection<SemanticsTag>? Tags => _tags;

    /// Whether this node carries `tag`.
    public bool IsTagged(SemanticsTag tag) => _tags is not null && _tags.Contains(tag);

    private HashSet<SemanticsTag>? _tags;

    internal void AddTags(IReadOnlyCollection<SemanticsTag> tags)
    {
        _tags ??= [];
        foreach (SemanticsTag tag in tags)
        {
            _tags.Add(tag);
        }
    }

    internal void ClearTags() => _tags = null;

    internal void ReplaceTags(IReadOnlyCollection<SemanticsTag>? tags)
    {
        _tags = tags is { Count: > 0 } ? [.. tags] : null;
    }

    /// <summary>Whether the node is not visible on screen but still part of the tree.</summary>
    /// <remarks>Flutter's <c>SemanticsFlag.isHidden</c>, exposed as a property for convenience.</remarks>
    public bool IsHidden => Flags.HasFlag(SemanticsFlags.IsHidden);
    public IReadOnlyList<SemanticsNode> Children => _children;
    public IReadOnlyDictionary<CustomSemanticsAction, Action> CustomSemanticsActions => _customActionHandlers;
    internal bool IsSemanticBoundary { get; set; }

    /// <summary>
    /// Reconfigures this node with <paramref name="config"/> and replaces its children.
    /// </summary>
    /// <remarks>
    /// Geometry (<see cref="Rect"/>, <see cref="IsHidden"/>) is owned by the semantics compiler and is not
    /// touched here, so a render object that synthesizes extra nodes from
    /// <c>RenderObject.AssembleSemanticsNode</c> assigns it explicitly.
    /// </remarks>
    public void UpdateWith(
        SemanticsConfiguration? config,
        IReadOnlyList<SemanticsNode>? childrenInInversePaintOrder = null)
    {
        // A null configuration resets the node to Flutter's shared `_kEmptyConfig`, which is how the
        // two-pane scroll split strips the outer node of everything it handed to the inner one.
        config ??= SemanticsConfiguration.Empty;
        Label = config.Label;
        Hint = config.Hint;
        OnTapHint = config.OnTapHint;
        Tooltip = config.Tooltip;
        Value = config.Value;
        IncreasedValue = config.IncreasedValue;
        DecreasedValue = config.DecreasedValue;
        MinValue = config.MinValue;
        MaxValue = config.MaxValue;
        Role = config.Role;
        InputType = config.InputType;
        HitTestBehavior = config.HitTestBehavior;
        Flags = config.Flags;
        // Flutter masks the actions with `_kUnblockedUserActions` when the node blocks user actions;
        // that mask only keeps the two accessibility-focus actions, neither of which Plumix models.
        AreUserActionsBlocked = config.IsBlockingUserActions;
        Actions = AreUserActionsBlocked ? SemanticsActions.None : config.Actions;
        IndexInParent = config.IndexInParent;
        SortKey = config.SortKey;
        ScrollPosition = config.ScrollPosition;
        ScrollExtentMax = config.ScrollExtentMax;
        ScrollExtentMin = config.ScrollExtentMin;
        ScrollChildCount = config.ScrollChildCount;
        ScrollIndex = config.ScrollIndex;
        TextDirection = config.TextDirection;
        IsSemanticBoundary = config.IsSemanticBoundary;
        ReplaceChildren(childrenInInversePaintOrder ?? []);
        SetActionHandlers(AreUserActionsBlocked ? EmptyActionHandlers : config.ActionHandlers);
        SetCustomActionHandlers(AreUserActionsBlocked ? EmptyCustomActionHandlers : config.CustomActionHandlers);
    }

    internal void ReplaceChildren(IReadOnlyList<SemanticsNode> children)
    {
        foreach (SemanticsNode child in _children)
        {
            if (ReferenceEquals(child.Parent, this))
            {
                child.Parent = null;
            }
        }

        _children.Clear();
        _children.AddRange(children);
        foreach (SemanticsNode child in _children)
        {
            child.Parent = this;
        }
    }

    /// <summary>
    /// This node's children in the order assistive technologies traverse them.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>SemanticsNode._childrenInTraversalOrder</c>: children are first ordered by the
    /// geometry-driven default sort (when a text direction is inherited), then the sort keys are
    /// applied within groups of comparable keys.
    /// </remarks>
    public IReadOnlyList<SemanticsNode> ChildrenInTraversalOrder => SemanticsTraversal.Sort(this);

    /// <summary>The reading direction for this node's text, and the direction siblings are sorted in.</summary>
    public TextDirection? TextDirection { get; internal set; }

    /// <summary>Whether an ancestor asked this node to stop exposing its user actions.</summary>
    /// <remarks>Flutter's <c>SemanticsNode.areUserActionsBlocked</c>.</remarks>
    public bool AreUserActionsBlocked { get; private set; }

    private static readonly IReadOnlyDictionary<SemanticsActions, SemanticsActionHandler> EmptyActionHandlers =
        new Dictionary<SemanticsActions, SemanticsActionHandler>();

    private static readonly IReadOnlyDictionary<CustomSemanticsAction, Action> EmptyCustomActionHandlers =
        new Dictionary<CustomSemanticsAction, Action>();

    internal void SetActionHandlers(IReadOnlyDictionary<SemanticsActions, SemanticsActionHandler> handlers)
    {
        _actionHandlers.Clear();
        foreach (var pair in handlers)
        {
            _actionHandlers[pair.Key] = pair.Value;
        }
    }

    internal void CopyActionHandlersTo(Dictionary<SemanticsActions, SemanticsActionHandler> target)
    {
        foreach (var pair in _actionHandlers)
        {
            target.TryAdd(pair.Key, pair.Value);
        }
    }

    internal void SetCustomActionHandlers(IReadOnlyDictionary<CustomSemanticsAction, Action> handlers)
    {
        _customActionHandlers.Clear();
        foreach (var pair in handlers)
        {
            _customActionHandlers[pair.Key] = pair.Value;
        }
    }

    internal void CopyCustomActionHandlersTo(Dictionary<CustomSemanticsAction, Action> target)
    {
        foreach (var pair in _customActionHandlers)
        {
            target.TryAdd(pair.Key, pair.Value);
        }
    }

    internal bool PerformAction(SemanticsActions action, object? args = null)
    {
        if (_actionHandlers.TryGetValue(action, out var handler))
        {
            handler(args);
            return true;
        }

        // Flutter falls back to the node's own show-on-screen closure, so a plain list item needs no
        // explicit handler to be scrolled into view.
        if (action == SemanticsActions.ShowOnScreen && ShowOnScreenRequest is { } showOnScreen)
        {
            showOnScreen();
            return true;
        }

        return false;
    }

    internal bool PerformCustomAction(CustomSemanticsAction action)
    {
        if (_customActionHandlers.TryGetValue(action, out var handler))
        {
            handler();
            return true;
        }

        return false;
    }
}

public sealed class SemanticsOwner
{
    private int _nextNodeId;
    private readonly Dictionary<int, SemanticsNode> _index = [];

    public SemanticsNode? RootNode { get; private set; }

    /// <summary>Creates or reuses the node a render object owns.</summary>
    internal SemanticsNode CreateNodeFor(RenderObject renderObject)
    {
        // Every node backed by a render object can be asked to scroll itself into view, even when
        // nothing registered an explicit handler.
        return new SemanticsNode(++_nextNodeId, renderObject.GetType().Name)
        {
            ShowOnScreenRequest = () => renderObject.ShowOnScreen()
        };
    }

    /// <summary>
    /// Creates a node that no render object owns, for a sibling merge group or an inner node.
    /// </summary>
    internal SemanticsNode CreateDetachedNode(RenderObject? showOnScreenSource = null)
    {
        return new SemanticsNode(++_nextNodeId, showOnScreenSource?.GetType().Name)
        {
            ShowOnScreenRequest = showOnScreenSource is { } renderObject
                ? () => renderObject.ShowOnScreen()
                : null
        };
    }

    internal void UpdateRoot(SemanticsNode? root)
    {
        RootNode = root;
        RebuildIndex();
    }

    public bool PerformAction(int nodeId, SemanticsActions action, object? args = null)
    {
        if (action == SemanticsActions.None)
        {
            return false;
        }

        return _index.TryGetValue(nodeId, out var node) && node.PerformAction(action, args);
    }

    public bool PerformCustomAction(int nodeId, CustomSemanticsAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return _index.TryGetValue(nodeId, out var node) && node.PerformCustomAction(action);
    }

    public string DebugDumpTree()
    {
        if (RootNode == null)
        {
            return "<empty>";
        }

        var builder = new StringBuilder();
        WriteNode(builder, RootNode, depth: 0);
        return builder.ToString().TrimEnd();
    }

    private void RebuildIndex()
    {
        _index.Clear();
        if (RootNode == null)
        {
            return;
        }

        VisitNode(RootNode, node => _index[node.Id] = node);
    }

    private static void VisitNode(SemanticsNode node, Action<SemanticsNode> visitor)
    {
        visitor(node);
        foreach (var child in node.Children)
        {
            VisitNode(child, visitor);
        }
    }

    private static void WriteNode(StringBuilder builder, SemanticsNode node, int depth)
    {
        builder.Append(' ', depth * 2);
        builder.Append('#').Append(node.Id);
        if (node.DebugOwner is { } debugOwner)
        {
            builder.Append('(').Append(debugOwner).Append(')');
        }

        builder.Append(" rect=").Append(node.Rect);

        if (!string.IsNullOrEmpty(node.Label))
        {
            builder.Append(" label=\"").Append(node.Label).Append('"');
        }

        if (!string.IsNullOrEmpty(node.Value))
        {
            builder.Append(" value=\"").Append(node.Value).Append('"');
        }

        if (!string.IsNullOrEmpty(node.OnTapHint))
        {
            builder.Append(" onTapHint=\"").Append(node.OnTapHint).Append('"');
        }

        if (node.Flags != SemanticsFlags.None)
        {
            builder.Append(" flags=").Append(node.Flags);
        }

        if (node.InputType != SemanticsInputType.None)
        {
            builder.Append(" inputType=").Append(node.InputType);
        }

        if (node.HitTestBehavior != SemanticsHitTestBehavior.Defer)
        {
            builder.Append(" hitTestBehavior=").Append(node.HitTestBehavior);
        }

        if (node.Actions != SemanticsActions.None)
        {
            builder.Append(" actions=").Append(node.Actions);
        }

        if (node.IndexInParent.HasValue)
        {
            builder.Append(" indexInParent=").Append(node.IndexInParent.Value);
        }

        if (node.ScrollChildCount.HasValue)
        {
            builder.Append(" scrollChildren=").Append(node.ScrollChildCount.Value);
        }

        if (node.ScrollIndex.HasValue)
        {
            builder.Append(" scrollIndex=").Append(node.ScrollIndex.Value);
        }

        if (node.ScrollPosition.HasValue)
        {
            builder.Append(" scrollPosition=").Append(node.ScrollPosition.Value);
        }

        if (node.IsHidden)
        {
            builder.Append(" hidden");
        }

        if (node.Tags is { Count: > 0 } tags)
        {
            builder.Append(" tags=[").AppendJoin(',', tags.Select(static tag => tag.Name)).Append(']');
        }

        builder.AppendLine();

        foreach (var child in node.Children)
        {
            WriteNode(builder, child, depth + 1);
        }
    }


}
