using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/sliver_tree.dart

namespace Plumix.Widgets;

/// <summary>
/// Signature for a function that creates a <see cref="Widget"/> to represent the given
/// <see cref="TreeSliverNode"/> in the <see cref="TreeSliver{T}"/>.
/// </summary>
/// <remarks>Dart's <c>TreeSliverNodeBuilder</c>.</remarks>
public delegate Widget TreeSliverNodeBuilder(
    BuildContext context,
    TreeSliverNode node,
    AnimationStyle animationStyle);

/// <summary>
/// Signature for a function that returns an extent for the given <see cref="TreeSliverNode"/> in the
/// <see cref="TreeSliver{T}"/>.
/// </summary>
/// <remarks>Dart's <c>TreeSliverRowExtentBuilder</c>.</remarks>
public delegate double TreeSliverRowExtentBuilder(TreeSliverNode node, SliverLayoutDimensions dimensions);

/// <summary>
/// Signature for a function that is called when a <see cref="TreeSliverNode"/> is toggled, changing
/// its expanded state.
/// </summary>
/// <remarks>Dart's <c>TreeSliverNodeCallback</c>.</remarks>
public delegate void TreeSliverNodeCallback(TreeSliverNode node);

/// <summary>
/// The untyped face of a <see cref="TreeSliverNode{T}"/>: what Dart passes around as
/// <c>TreeSliverNode&lt;Object?&gt;</c>.
/// </summary>
/// <remarks>
/// C# generics are invariant, so the builders, callbacks and <see cref="TreeSliverController"/> take
/// this base class where Dart relies on <c>TreeSliverNode&lt;String&gt;</c> being a
/// <c>TreeSliverNode&lt;Object?&gt;</c>. Only <see cref="TreeSliverNode{T}"/> derives from it.
/// </remarks>
public abstract class TreeSliverNode
{
    private protected TreeSliverNode(bool expanded)
    {
        IsExpanded = expanded;
    }

    /// <summary>The subject matter of the node.</summary>
    public object? Content => UntypedContent;

    /// <summary>Other <see cref="TreeSliverNode"/>s that this node will be parent to.</summary>
    public IReadOnlyList<TreeSliverNode> Children => UntypedChildren;

    /// <summary>Whether or not this node is expanded in the tree.</summary>
    /// <remarks>Cannot be expanded if there are no children; only <see cref="TreeSliver{T}"/> writes it.</remarks>
    public bool IsExpanded { get; internal set; }

    /// <summary>
    /// The number of parent nodes between this node and the root of the tree; null until a
    /// <see cref="TreeSliver{T}"/> has unpacked it.
    /// </summary>
    public int? Depth { get; internal set; }

    /// <summary>The parent of this node, or null for a root node or before a <see cref="TreeSliver{T}"/>
    /// has unpacked it.</summary>
    public TreeSliverNode? Parent { get; internal set; }

    private protected abstract object? UntypedContent { get; }

    private protected abstract IReadOnlyList<TreeSliverNode> UntypedChildren { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        string depth = Depth == 0 ? "root" : Depth?.ToString(System.Globalization.CultureInfo.InvariantCulture)
            ?? "null";
        string kind = Children.Count == 0 ? "leaf" : $"parent, expanded: {(IsExpanded ? "true" : "false")}";
        return $"TreeSliverNode: {Content?.ToString() ?? "null"}, depth: {depth}, {kind}";
    }
}

/// <summary>A data structure for configuring children of a <see cref="TreeSliver{T}"/>.</summary>
/// <remarks>
/// Dart's <c>TreeSliverNode&lt;T&gt;</c>. The <see cref="Children"/> list is stored by reference, so
/// mutating it (and rebuilding the <see cref="TreeSliver{T}"/>) changes the tree. Equality is identity.
/// </remarks>
public sealed class TreeSliverNode<T> : TreeSliverNode
{
    /// <summary>Creates a <see cref="TreeSliverNode{T}"/> instance for use in a <see cref="TreeSliver{T}"/>.</summary>
    public TreeSliverNode(T content, List<TreeSliverNode<T>>? children = null, bool expanded = false)
        : base((children?.Count > 0) && expanded)
    {
        Content = content;
        Children = children ?? [];
    }

    /// <summary>The subject matter of the node.</summary>
    public new T Content { get; }

    /// <summary>Other <see cref="TreeSliverNode{T}"/>s that this node will be parent to.</summary>
    public new List<TreeSliverNode<T>> Children { get; }

    /// <summary>The parent of this node, if any.</summary>
    public new TreeSliverNode<T>? Parent => (TreeSliverNode<T>?)base.Parent;

    private protected override object? UntypedContent => Content;

    private protected override IReadOnlyList<TreeSliverNode> UntypedChildren => Children;
}

/// <summary>
/// The state of a <see cref="TreeSliver{T}"/>, as exposed to a <see cref="TreeSliverController"/>.
/// </summary>
/// <remarks>Dart's <c>TreeSliverStateMixin&lt;T&gt;</c>.</remarks>
public interface ITreeSliverStateMixin<T>
{
    /// <summary>Returns whether or not the given node is expanded.</summary>
    bool IsExpanded(TreeSliverNode<T> node);

    /// <summary>Returns whether or not the given node is enclosed within its parent node.</summary>
    bool IsActive(TreeSliverNode<T> node);

    /// <summary>Switches the given node between expanded and collapsed states.</summary>
    void ToggleNode(TreeSliverNode<T> node);

    /// <summary>Closes all parent nodes in the tree.</summary>
    void CollapseAll();

    /// <summary>Expands all parent nodes in the tree.</summary>
    void ExpandAll();

    /// <summary>Retrieves the node containing the associated content, if it exists.</summary>
    TreeSliverNode<T>? GetNodeFor(T content);

    /// <summary>Returns the current row index of the given node, or null if it is not active.</summary>
    int? GetActiveIndexFor(TreeSliverNode<T> node);
}

/// <summary>
/// C#-only: the untyped face of <see cref="TreeSliverState{T}"/>, Dart's
/// <c>TreeSliverStateMixin&lt;Object?&gt;</c> held by the controller.
/// </summary>
internal interface ITreeSliverState
{
    TreeSliverController Controller { get; }

    bool IsExpanded(TreeSliverNode node);

    bool IsActive(TreeSliverNode node);

    void ToggleNode(TreeSliverNode node);

    void CollapseAll();

    void ExpandAll();

    TreeSliverNode? GetNodeFor(object? content);

    int? GetActiveIndexFor(TreeSliverNode node);
}

/// <summary>Enables control over the <see cref="TreeSliverNode"/>s of a <see cref="TreeSliver{T}"/>.</summary>
/// <remarks>
/// Dart's <c>TreeSliverController</c>. It can be provided to a <see cref="TreeSliver{T}"/>, or
/// retrieved from one through <see cref="Of"/>.
/// </remarks>
public sealed class TreeSliverController
{
    // Dart's library-private `_state`.
    internal ITreeSliverState? State { get; set; }

    /// <summary>Whether the given node is expanded.</summary>
    public bool IsExpanded(TreeSliverNode node)
    {
        DebugAssertions.Assert(State is not null);
        return State!.IsExpanded(node);
    }

    /// <summary>Whether or not the given node is enclosed within its parent node.</summary>
    public bool IsActive(TreeSliverNode node)
    {
        DebugAssertions.Assert(State is not null);
        return State!.IsActive(node);
    }

    /// <summary>Returns the node that contains the given content, or null.</summary>
    public TreeSliverNode? GetNodeFor(object? content)
    {
        DebugAssertions.Assert(State is not null);
        return State!.GetNodeFor(content);
    }

    /// <summary>Switches the given node between expanded and collapsed states.</summary>
    public void ToggleNode(TreeSliverNode node)
    {
        DebugAssertions.Assert(State is not null);
        State!.ToggleNode(node);
    }

    /// <summary>Expands the node if it is collapsed; no-op otherwise.</summary>
    public void ExpandNode(TreeSliverNode node)
    {
        DebugAssertions.Assert(State is not null);
        if (!node.IsExpanded)
        {
            State!.ToggleNode(node);
        }
    }

    /// <summary>Expands all parent nodes in the tree.</summary>
    public void ExpandAll()
    {
        DebugAssertions.Assert(State is not null);
        State!.ExpandAll();
    }

    /// <summary>Closes all parent nodes in the tree.</summary>
    public void CollapseAll()
    {
        DebugAssertions.Assert(State is not null);
        State!.CollapseAll();
    }

    /// <summary>Collapses the node if it is expanded; no-op otherwise.</summary>
    public void CollapseNode(TreeSliverNode node)
    {
        DebugAssertions.Assert(State is not null);
        if (node.IsExpanded)
        {
            State!.ToggleNode(node);
        }
    }

    /// <summary>Returns the current row index of the given node, or null if it is not active.</summary>
    public int? GetActiveIndexFor(TreeSliverNode node)
    {
        DebugAssertions.Assert(State is not null);
        return State!.GetActiveIndexFor(node);
    }

    /// <summary>
    /// Finds the <see cref="TreeSliverController"/> of the closest enclosing <see cref="TreeSliver{T}"/>.
    /// </summary>
    /// <exception cref="FlutterError">No <see cref="TreeSliver{T}"/> encloses <paramref name="context"/>.</exception>
    public static TreeSliverController Of(BuildContext context)
    {
        ITreeSliverState? result = FindTreeSliverState(context);
        if (result is not null)
        {
            return result.Controller;
        }

        throw new FlutterError(
        [
            new ErrorSummary(
                "TreeController.of() called with a context that does not contain a TreeSliver."),
            new ErrorDescription(
                "No TreeSliver ancestor could be found starting from the context that was passed to "
                + "TreeController.of(). This usually happens when the context provided is from the same "
                + "StatefulWidget as that whose build function actually creates the TreeSliver widget "
                + "being sought."),
            new ErrorHint(
                "There are several ways to avoid this problem. The simplest is to use a Builder to get a "
                + "context that is \"under\" the TreeSliver."),
            new ErrorHint(
                "A more efficient solution is to split your build function into several widgets. This "
                + "introduces a new context from which you can obtain the TreeSliver. In this solution, "
                + "you would have an outer widget that creates the TreeSliver populated by instances of "
                + "your new inner widgets, and then in these inner widgets you would use "
                + "TreeController.of()."),
            context.DescribeElement("The context used was"),
        ]);
    }

    /// <summary>
    /// Finds the <see cref="TreeSliverController"/> of the closest enclosing <see cref="TreeSliver{T}"/>,
    /// or null if there is none.
    /// </summary>
    public static TreeSliverController? MaybeOf(BuildContext context) => FindTreeSliverState(context)?.Controller;

    // Dart's `findAncestorStateOfType<_TreeSliverState<Object?>>()`: Dart generics are covariant, so any
    // `_TreeSliverState<T>` matches.
    private static ITreeSliverState? FindTreeSliverState(BuildContext context)
    {
        ITreeSliverState? result = null;
        context.VisitAncestorElements(element =>
        {
            if (element is StatefulElement { State: ITreeSliverState state })
            {
                result = state;
                return false;
            }

            return true;
        });
        return result;
    }
}

/// <summary>Static members of <see cref="TreeSliver{T}"/>.</summary>
/// <remarks>
/// Dart's statics on <c>TreeSliver</c>. C# statics on a generic type exist once per type argument, so
/// they live on this non-generic class to stay shared, as in Dart.
/// </remarks>
public static class TreeSliver
{
    private const double DefaultRowExtent = 40.0;

    /// <summary>A default <see cref="Curve"/> for the toggle animation: <see cref="Curves.Linear"/>.</summary>
    public static readonly Curve DefaultAnimationCurve = Curves.Linear;

    /// <summary>A default duration for the toggle animation: 150 ms.</summary>
    public static readonly TimeSpan DefaultAnimationDuration = TimeSpan.FromMilliseconds(150);

    /// <summary>
    /// The default <see cref="AnimationStyle"/> used for node expand and collapse animations, when one
    /// has not been provided in <see cref="TreeSliver{T}.ToggleAnimationStyle"/>.
    /// </summary>
    public static AnimationStyle DefaultToggleAnimationStyle { get; set; } =
        new(Curve: DefaultAnimationCurve, Duration: DefaultAnimationDuration);

    /// <summary>
    /// A wrapper method for triggering the expansion or collapse of a <see cref="TreeSliverNode"/>.
    /// </summary>
    public static Widget WrapChildToToggleNode(TreeSliverNode node, Widget child)
    {
        return new Builder(context => new GestureDetector(
            onTap: () => TreeSliverController.Of(context).ToggleNode(node),
            child: child));
    }

    /// <summary>Returns the fixed default extent for rows in a <see cref="TreeSliver{T}"/>: 40 pixels.</summary>
    public static double DefaultTreeRowExtentBuilder(TreeSliverNode node, SliverLayoutDimensions dimensions)
    {
        return DefaultRowExtent;
    }

    /// <summary>Returns the default tree row for a given <see cref="TreeSliverNode"/>.</summary>
    /// <remarks>
    /// A rotating arrow for parents (wrapped through <see cref="WrapChildToToggleNode"/>) followed by the
    /// node content's text.
    /// </remarks>
    public static Widget DefaultTreeNodeBuilder(
        BuildContext context,
        TreeSliverNode node,
        AnimationStyle toggleAnimationStyle)
    {
        TimeSpan animationDuration = toggleAnimationStyle.Duration ?? DefaultAnimationDuration;
        Curve animationCurve = toggleAnimationStyle.Curve ?? DefaultAnimationCurve;
        int index = TreeSliverController.Of(context).GetActiveIndexFor(node)!.Value;
        return new Padding(
            EdgeInsets.All(8.0),
            new Row(
            [
                // Icon for parent nodes
                WrapChildToToggleNode(
                    node,
                    SizedBox.Square(
                        dimension: 30.0,
                        child: node.Children.Count > 0
                            ? new AnimatedRotation(
                                key: new ValueKey<int>(index),
                                turns: node.IsExpanded ? 0.25 : 0.0,
                                duration: animationDuration,
                                curve: animationCurve,
                                // Renders a unicode right-facing arrow. >
                                child: new Icon(new IconData(0x25BA), size: 14))
                            : null)),
                // Spacer
                new SizedBox(width: 8.0),
                // Content
                new Text(node.Content?.ToString() ?? "null"),
            ]));
    }

    // Dart's `_kDefaultSemanticIndexCallback`.
    internal static int? DefaultSemanticIndexCallback(Widget widget, int localIndex) => localIndex;
}

/// <summary>A widget that displays <see cref="TreeSliverNode{T}"/>s that expand and collapse in a
/// vertically and unidirectionally scrolling <see cref="Viewport"/>.</summary>
/// <remarks>
/// Dart's <c>TreeSliver&lt;T&gt;</c>. Only supports a vertical axis in the
/// <see cref="AxisDirection.Down"/> direction. Its static members live on <see cref="TreeSliver"/>.
/// </remarks>
public sealed class TreeSliver<T> : StatefulWidget
{
    /// <summary>Creates an instance of a <see cref="TreeSliver{T}"/> for displaying <see cref="TreeSliverNode{T}"/>s
    /// that animate expanding and collapsing of nodes.</summary>
    public TreeSliver(
        List<TreeSliverNode<T>> tree,
        TreeSliverNodeBuilder? treeNodeBuilder = null,
        TreeSliverRowExtentBuilder? treeRowExtentBuilder = null,
        TreeSliverController? controller = null,
        TreeSliverNodeCallback? onNodeToggle = null,
        AnimationStyle? toggleAnimationStyle = null,
        TreeSliverIndentationType? indentation = null,
        bool addAutomaticKeepAlives = true,
        bool addRepaintBoundaries = true,
        bool addSemanticIndexes = true,
        SemanticIndexCallback? semanticIndexCallback = null,
        int semanticIndexOffset = 0,
        ChildIndexGetter? findChildIndexCallback = null,
        Key? key = null) : base(key)
    {
        Tree = tree;
        TreeNodeBuilder = treeNodeBuilder ?? TreeSliver.DefaultTreeNodeBuilder;
        TreeRowExtentBuilder = treeRowExtentBuilder ?? TreeSliver.DefaultTreeRowExtentBuilder;
        Controller = controller;
        OnNodeToggle = onNodeToggle;
        ToggleAnimationStyle = toggleAnimationStyle;
        Indentation = indentation ?? TreeSliverIndentationType.Standard;
        AddAutomaticKeepAlives = addAutomaticKeepAlives;
        AddRepaintBoundaries = addRepaintBoundaries;
        AddSemanticIndexes = addSemanticIndexes;
        SemanticIndexCallback = semanticIndexCallback ?? TreeSliver.DefaultSemanticIndexCallback;
        SemanticIndexOffset = semanticIndexOffset;
        FindChildIndexCallback = findChildIndexCallback;
    }

    /// <summary>The list of <see cref="TreeSliverNode{T}"/>s that may be displayed.</summary>
    public List<TreeSliverNode<T>> Tree { get; }

    /// <summary>Called to build and entry of the <see cref="TreeSliver{T}"/> for the given node.</summary>
    public TreeSliverNodeBuilder TreeNodeBuilder { get; }

    /// <summary>Builds the leading extent of the row for the given node.</summary>
    public TreeSliverRowExtentBuilder TreeRowExtentBuilder { get; }

    /// <summary>If provided, the controller can be used to expand and collapse nodes.</summary>
    public TreeSliverController? Controller { get; }

    /// <summary>A callback that is called when a node is expanded or collapsed.</summary>
    public TreeSliverNodeCallback? OnNodeToggle { get; }

    /// <summary>The default <see cref="AnimationStyle"/> for expanding and collapsing nodes.</summary>
    /// <remarks>
    /// When null, <see cref="TreeSliver.DefaultToggleAnimationStyle"/> is used; to disable the animation,
    /// use <see cref="AnimationStyle.NoAnimation"/>.
    /// </remarks>
    public AnimationStyle? ToggleAnimationStyle { get; }

    /// <summary>The number of pixels children will be offset by in the cross axis per depth.</summary>
    public TreeSliverIndentationType Indentation { get; }

    /// <summary>Whether to wrap each row in an <see cref="AutomaticKeepAlive"/>.</summary>
    public bool AddAutomaticKeepAlives { get; }

    /// <summary>Whether to wrap each row in a <see cref="RepaintBoundary"/>.</summary>
    public bool AddRepaintBoundaries { get; }

    /// <summary>Whether to wrap each row in an <see cref="IndexedSemantics"/>.</summary>
    public bool AddSemanticIndexes { get; }

    /// <summary>A <see cref="Widgets.SemanticIndexCallback"/> to use when <see cref="AddSemanticIndexes"/>
    /// is true.</summary>
    public SemanticIndexCallback SemanticIndexCallback { get; }

    /// <summary>The offset added to the semantic index of each row.</summary>
    public int SemanticIndexOffset { get; }

    /// <summary>Called to find the new index of a row by its key when the tree is rebuilt.</summary>
    public ChildIndexGetter? FindChildIndexCallback { get; }

    /// <inheritdoc />
    public override State CreateState() => new TreeSliverState<T>();
}

/// <summary>Dart's <c>_TreeSliverState&lt;T&gt;</c>.</summary>
internal sealed class TreeSliverState<T> : State<TreeSliver<T>>, ITreeSliverStateMixin<T>, ITreeSliverState
{
    private readonly List<TreeSliverNode<T>> _activeNodes = [];

    private readonly Dictionary<TreeSliverNode<T>, AnimationRecord> _currentAnimationForParent =
        new(ReferenceEqualityComparer.Instance);

    private readonly Dictionary<UniqueKey, TreeSliverNodesAnimation> _activeAnimations = [];

    private TreeSliverController? _treeController;

    public TreeSliverController Controller => _treeController!;

    private bool ShouldUnpackNode(TreeSliverNode<T> node)
    {
        if (node.Children.Count == 0)
        {
            // No children to unpack.
            return false;
        }

        if (_currentAnimationForParent.ContainsKey(node))
        {
            // Whether expanding or collapsing, the child nodes are still active, so unpack.
            return true;
        }

        // If we are not animating, respect node.isExpanded.
        return node.IsExpanded;
    }

    private void UnpackActiveNodes(
        int depth = 0,
        List<TreeSliverNode<T>>? nodes = null,
        TreeSliverNode<T>? parent = null)
    {
        if (nodes is null)
        {
            _activeNodes.Clear();
            nodes = Widget.Tree;
        }

        foreach (TreeSliverNode<T> node in nodes)
        {
            node.Depth = depth;
            ((TreeSliverNode)node).Parent = parent;
            _activeNodes.Add(node);
            if (ShouldUnpackNode(node))
            {
                UnpackActiveNodes(depth + 1, node.Children, node);
            }
        }
    }

    public override void InitState()
    {
        UnpackActiveNodes();
        DebugAssertions.Assert(
            Widget.Controller?.State is null,
            "The provided TreeSliverController is already associated with another TreeSliver. A "
            + "TreeSliverController can only be associated with one TreeSliver.");
        _treeController = Widget.Controller ?? new TreeSliverController();
        _treeController.State = this;
        base.InitState();
    }

    public override void DidUpdateWidget(TreeSliver<T> oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        // Internal or provided, there is always a tree controller.
        DebugAssertions.Assert(_treeController is not null);
        if (oldWidget.Controller is null && Widget.Controller is not null)
        {
            // A new tree controller has been provided, update and dispose of the internally generated
            // one.
            _treeController!.State = null;
            _treeController = Widget.Controller;
            _treeController.State = this;
        }
        else if (oldWidget.Controller is not null && Widget.Controller is null)
        {
            // A tree controller had been provided, but was removed. We need to create one internally.
            DebugAssertions.Assert(ReferenceEquals(oldWidget.Controller, _treeController));
            oldWidget.Controller.State = null;
            _treeController = new TreeSliverController();
            _treeController.State = this;
        }
        else if (!ReferenceEquals(oldWidget.Controller, Widget.Controller))
        {
            DebugAssertions.Assert(oldWidget.Controller is not null);
            DebugAssertions.Assert(Widget.Controller is not null);
            DebugAssertions.Assert(ReferenceEquals(oldWidget.Controller, _treeController));
            // The tree is still being provided a controller, but it has changed. Just update it.
            _treeController!.State = null;
            _treeController = Widget.Controller;
            _treeController!.State = this;
        }

        // Internal or provided, there is always a tree controller.
        DebugAssertions.Assert(_treeController is not null);
        DebugAssertions.Assert(_treeController!.State is not null);
        UnpackActiveNodes();
    }

    public override void Dispose()
    {
        _treeController!.State = null;
        foreach (AnimationRecord record in _currentAnimationForParent.Values)
        {
            record.Animation.Dispose();
            record.Controller.Dispose();
        }

        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new SliverTree(
            itemCount: _activeNodes.Count,
            activeAnimations: _activeAnimations,
            itemBuilder: (BuildContext itemContext, int index) =>
            {
                TreeSliverNode<T> node = _activeNodes[index];
                Widget child = Widget.TreeNodeBuilder(
                    itemContext,
                    node,
                    Widget.ToggleAnimationStyle ?? TreeSliver.DefaultToggleAnimationStyle);

                if (Widget.AddRepaintBoundaries)
                {
                    child = new RepaintBoundary(child);
                }

                if (Widget.AddSemanticIndexes)
                {
                    int? semanticIndex = Widget.SemanticIndexCallback(child, index);
                    if (semanticIndex is not null)
                    {
                        child = new IndexedSemantics(semanticIndex.Value + Widget.SemanticIndexOffset, child);
                    }
                }

                return new TreeNodeParentDataWidget(node.Depth!.Value, child);
            },
            itemExtentBuilder: (int index, SliverLayoutDimensions dimensions) =>
                Widget.TreeRowExtentBuilder(_activeNodes[index], dimensions),
            addAutomaticKeepAlives: Widget.AddAutomaticKeepAlives,
            findChildIndexCallback: Widget.FindChildIndexCallback,
            indentation: Widget.Indentation.Value);
    }

    // TreeStateMixin Implementation

    public bool IsExpanded(TreeSliverNode<T> node) => GetNode(node.Content, Widget.Tree)?.IsExpanded ?? false;

    public bool IsActive(TreeSliverNode<T> node) => _activeNodes.Contains(node);

    public TreeSliverNode<T>? GetNodeFor(T content) => GetNode(content, Widget.Tree);

    private static TreeSliverNode<T>? GetNode(T content, List<TreeSliverNode<T>> tree)
    {
        var nextDepth = new List<TreeSliverNode<T>>();
        foreach (TreeSliverNode<T> node in tree)
        {
            if (EqualityComparer<T>.Default.Equals(node.Content, content))
            {
                return node;
            }

            if (node.Children.Count > 0)
            {
                nextDepth.AddRange(node.Children);
            }
        }

        if (nextDepth.Count > 0)
        {
            return GetNode(content, nextDepth);
        }

        return null;
    }

    public int? GetActiveIndexFor(TreeSliverNode<T> node)
    {
        if (_activeNodes.Contains(node))
        {
            return _activeNodes.IndexOf(node);
        }

        return null;
    }

    public void ExpandAll()
    {
        var activeNodesToExpand = new List<TreeSliverNode<T>>();
        ExpandAllNodes(Widget.Tree, activeNodesToExpand);
        for (int i = activeNodesToExpand.Count - 1; i >= 0; i--)
        {
            ToggleNode(activeNodesToExpand[i]);
        }
    }

    private void ExpandAllNodes(List<TreeSliverNode<T>> tree, List<TreeSliverNode<T>> activeNodesToExpand)
    {
        foreach (TreeSliverNode<T> node in tree)
        {
            if (node.Children.Count > 0)
            {
                // This is a parent node.
                // Expand all the children, and their children.
                ExpandAllNodes(node.Children, activeNodesToExpand);
                if (!node.IsExpanded)
                {
                    // The node itself needs to be expanded.
                    if (_activeNodes.Contains(node))
                    {
                        // This is an active node in the tree, add to the list to toggle once all
                        // hidden nodes have been handled.
                        activeNodesToExpand.Add(node);
                    }
                    else
                    {
                        // This is a hidden node. Update its expanded state.
                        node.IsExpanded = true;
                    }
                }
            }
        }
    }

    public void CollapseAll()
    {
        var activeNodesToCollapse = new List<TreeSliverNode<T>>();
        CollapseAllNodes(Widget.Tree, activeNodesToCollapse);
        for (int i = activeNodesToCollapse.Count - 1; i >= 0; i--)
        {
            ToggleNode(activeNodesToCollapse[i]);
        }
    }

    private void CollapseAllNodes(List<TreeSliverNode<T>> tree, List<TreeSliverNode<T>> activeNodesToCollapse)
    {
        foreach (TreeSliverNode<T> node in tree)
        {
            if (node.Children.Count > 0)
            {
                // This is a parent node.
                // Collapse all the children, and their children.
                CollapseAllNodes(node.Children, activeNodesToCollapse);
                if (node.IsExpanded)
                {
                    // The node itself needs to be collapsed.
                    if (_activeNodes.Contains(node))
                    {
                        // This is an active node in the tree, add to the list to toggle once all
                        // hidden nodes have been handled.
                        activeNodesToCollapse.Add(node);
                    }
                    else
                    {
                        // This is a hidden node. Update its expanded state.
                        node.IsExpanded = false;
                    }
                }
            }
        }
    }

    private void UpdateActiveAnimations()
    {
        _activeAnimations.Clear();
        foreach (KeyValuePair<TreeSliverNode<T>, AnimationRecord> entry in _currentAnimationForParent)
        {
            AnimationRecord animationRecord = entry.Value;
            int leadingChildIndex = _activeNodes.IndexOf(entry.Key) + 1;
            _activeAnimations[animationRecord.Key] = new TreeSliverNodesAnimation(
                FromIndex: leadingChildIndex,
                ToIndex: leadingChildIndex + entry.Key.Children.Count - 1,
                Value: animationRecord.Animation.Value);
        }
    }

    public void ToggleNode(TreeSliverNode<T> node)
    {
        DebugAssertions.Assert(_activeNodes.Contains(node));
        if (node.Children.Count == 0)
        {
            // No state to change.
            return;
        }

        SetState(() =>
        {
            node.IsExpanded = !node.IsExpanded;
            Widget.OnNodeToggle?.Invoke(node);

            if (_currentAnimationForParent.TryGetValue(node, out AnimationRecord? existing))
            {
                // Dispose of the old animation if this node was already animating.
                existing.Animation.Dispose();
            }

            // If animation is disabled or the duration is zero, we skip the animation and immediately
            // update the active nodes. This prevents the app from freezing due to the tree being
            // incorrectly updated when the animation duration is zero. This is because, in this case,
            // the node's children are no longer active.
            if (Equals(Widget.ToggleAnimationStyle, AnimationStyle.NoAnimation)
                || Widget.ToggleAnimationStyle?.Duration == TimeSpan.Zero)
            {
                UnpackActiveNodes();
                return;
            }

            // Dart's cascade binds to `existing ?? new`, so a reused controller gains another pair of
            // listeners on every toggle.
            AnimationController controller = existing?.Controller
                ?? new AnimationController(
                    value: node.IsExpanded ? 0.0 : 1.0,
                    vsync: this,
                    duration: Widget.ToggleAnimationStyle?.Duration ?? TreeSliver.DefaultAnimationDuration);
            controller.AddStatusListener(status =>
            {
                switch (status)
                {
                    case AnimationStatus.Dismissed:
                    case AnimationStatus.Completed:
                        _currentAnimationForParent[node].Animation.Dispose();
                        _currentAnimationForParent[node].Controller.Dispose();
                        _currentAnimationForParent.Remove(node);
                        UpdateActiveAnimations();
                        // If the node is collapsing, we need to unpack the active nodes to remove the
                        // ones that were removed from the tree. This is only necessary if the node is
                        // collapsing.
                        if (!node.IsExpanded)
                        {
                            UnpackActiveNodes();
                        }

                        break;
                    case AnimationStatus.Forward:
                    case AnimationStatus.Reverse:
                        break;
                }
            });
            controller.AddListener(() => SetState(UpdateActiveAnimations));

            switch (controller.Status)
            {
                case AnimationStatus.Forward:
                case AnimationStatus.Reverse:
                    // We're interrupting an animation already in progress.
                    controller.Stop();
                    break;
                case AnimationStatus.Dismissed:
                case AnimationStatus.Completed:
                    break;
            }

            var newAnimation = new CurvedAnimation(
                controller,
                Widget.ToggleAnimationStyle?.Curve ?? TreeSliver.DefaultAnimationCurve);
            _currentAnimationForParent[node] = new AnimationRecord(controller, newAnimation, new UniqueKey());

            if (node.IsExpanded)
            {
                // Expanding
                UnpackActiveNodes();
                controller.Forward();
            }
            else
            {
                // Collapsing
                controller.Reverse();
            }
        });
    }

    // The untyped face the controller uses: Dart's covariant `TreeSliverStateMixin<Object?>`, whose
    // calls are checked against `T` at run time.

    bool ITreeSliverState.IsExpanded(TreeSliverNode node) => IsExpanded((TreeSliverNode<T>)node);

    bool ITreeSliverState.IsActive(TreeSliverNode node) => IsActive((TreeSliverNode<T>)node);

    void ITreeSliverState.ToggleNode(TreeSliverNode node) => ToggleNode((TreeSliverNode<T>)node);

    TreeSliverNode? ITreeSliverState.GetNodeFor(object? content) => GetNodeFor((T)content!);

    int? ITreeSliverState.GetActiveIndexFor(TreeSliverNode node) => GetActiveIndexFor((TreeSliverNode<T>)node);

    // Dart's `_AnimationRecord` record.
    private sealed record AnimationRecord(AnimationController Controller, CurvedAnimation Animation, UniqueKey Key);
}

/// <summary>Dart's <c>_TreeNodeParentDataWidget</c>.</summary>
internal sealed class TreeNodeParentDataWidget : ParentDataWidget<TreeSliverNodeParentData>
{
    public TreeNodeParentDataWidget(int depth, Widget child) : base(child)
    {
        DebugAssertions.Assert(depth >= 0);
        Depth = depth;
    }

    public int Depth { get; }

    public override void ApplyParentData(RenderObject renderObject)
    {
        var parentData = (TreeSliverNodeParentData)renderObject.parentData!;
        bool needsLayout = false;

        if (parentData.Depth != Depth)
        {
            DebugAssertions.Assert(Depth >= 0);
            parentData.Depth = Depth;
            needsLayout = true;
        }

        if (needsLayout)
        {
            (renderObject.Parent as RenderObject)?.MarkNeedsLayout();
        }
    }

    public override Type DebugTypicalAncestorWidgetClass => typeof(SliverTree);

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new IntProperty("depth", Depth));
    }
}

/// <summary>Dart's <c>_SliverTree</c>.</summary>
internal sealed class SliverTree : SliverVariedExtentList
{
    public SliverTree(
        NullableIndexedWidgetBuilder itemBuilder,
        ItemExtentBuilder itemExtentBuilder,
        Dictionary<UniqueKey, TreeSliverNodesAnimation> activeAnimations,
        double indentation,
        int itemCount,
        ChildIndexGetter? findChildIndexCallback = null,
        bool addAutomaticKeepAlives = true)
        : base(
            new SliverChildBuilderDelegate(
                itemBuilder,
                findChildIndexCallback: findChildIndexCallback,
                childCount: itemCount,
                addAutomaticKeepAlives: addAutomaticKeepAlives,
                addRepaintBoundaries: false, // Added by the TreeSliver
                addSemanticIndexes: false), // Added by the TreeSliver
            itemExtentBuilder)
    {
        ActiveAnimations = activeAnimations;
        Indentation = indentation;
    }

    public Dictionary<UniqueKey, TreeSliverNodesAnimation> ActiveAnimations { get; }

    public double Indentation { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        var element = (SliverMultiBoxAdaptorElement)context;
        return new RenderTreeSliver(
            itemExtentBuilder: ItemExtentBuilder,
            activeAnimations: ActiveAnimations,
            indentation: Indentation,
            childManager: element);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var renderTreeSliver = (RenderTreeSliver)renderObject;
        renderTreeSliver.SetItemExtentBuilder(ItemExtentBuilder);
        renderTreeSliver.ActiveAnimations = ActiveAnimations;
        renderTreeSliver.Indentation = Indentation;
    }
}
