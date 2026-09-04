using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/scroll_delegate.dart

namespace Plumix.Widgets;

/// <summary>
/// Maps a child and its position in the delegate onto the index assistive technologies see, or
/// <c>null</c> to give that child no semantic index at all.
/// </summary>
/// <remarks>Flutter's <c>SemanticIndexCallback</c>.</remarks>
public delegate int? SemanticIndexCallback(Widget widget, int localIndex);

public abstract class SliverChildDelegate
{
    /// <remarks>Flutter's <c>_kDefaultSemanticIndexCallback</c>.</remarks>
    public static int? DefaultSemanticIndexCallback(Widget widget, int localIndex) => localIndex;

    public abstract Widget? Build(BuildContext context, int index);

    /// <summary>
    /// An estimate of the number of children this delegate will build, or null when the child list
    /// is unbounded or too hard to count.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>estimatedChildCount</c>. Once <see cref="Build"/> has returned null this must be
    /// precise, because <see cref="IRenderSliverBoxChildManager.ChildCount"/> is built on it.
    /// </remarks>
    public virtual int? EstimatedChildCount => null;

    /// <summary>
    /// An estimate of the max scroll extent for all the children, or null to let the caller
    /// extrapolate it from the laid-out range.
    /// </summary>
    /// <remarks>Flutter's <c>SliverChildDelegate.estimateMaxScrollOffset</c>.</remarks>
    public virtual double? EstimateMaxScrollOffset(
        int firstIndex,
        int lastIndex,
        double leadingScrollOffset,
        double trailingScrollOffset) => null;

    /// <summary>
    /// Called at the end of layout with the index range of the children that were included in it.
    /// </summary>
    /// <remarks>Flutter's <c>SliverChildDelegate.didFinishLayout</c>.</remarks>
    public virtual void DidFinishLayout(int firstIndex, int lastIndex)
    {
    }

    /// <summary>
    /// Whether a sliver that was given a new instance of this delegate class has to rebuild its
    /// children, because the new instance represents different information than the old one.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>SliverChildDelegate.shouldRebuild</c>. When it returns false, the
    /// <see cref="Build"/> call might be optimized away.
    /// </remarks>
    public abstract bool ShouldRebuild(SliverChildDelegate oldDelegate);

    public virtual int? FindIndexByKey(Key key) => null;

    public override string ToString()
    {
        var description = new List<string>();
        DebugFillDescription(description);
        return $"{Diagnostics.DescribeIdentity(this)}({string.Join(", ", description)})";
    }

    /// <summary>Adds additional information to the given description for use by <see cref="ToString"/>.</summary>
    /// <remarks>Flutter's <c>SliverChildDelegate.debugFillDescription</c>; overrides must call base.</remarks>
    protected virtual void DebugFillDescription(List<string> description)
    {
        try
        {
            int? children = EstimatedChildCount;
            if (children is not null)
            {
                description.Add($"estimated child count: {children}");
            }
        }
        catch (Exception exception)
        {
            // The exception is forwarded to the widget inspector.
            description.Add($"estimated child count: EXCEPTION ({exception.GetType().Name})");
        }
    }

    /// <summary>
    /// Applies the delegate's wrapper options to one built child, in Flutter's order.
    /// </summary>
    /// <remarks>
    /// Dart repeats this block verbatim in both delegates' <c>build</c>; it lives here once because
    /// nothing about it is delegate-specific. Nesting, outermost first, is
    /// <c>KeyedSubtree &gt; AutomaticKeepAlive &gt; _SelectionKeepAlive &gt; IndexedSemantics &gt;
    /// RepaintBoundary &gt; child</c>, and the <see cref="KeyedSubtree"/> is added even when the child
    /// has no key.
    /// </remarks>
    private protected static Widget WrapChild(
        Widget child,
        int index,
        bool addAutomaticKeepAlives,
        bool addRepaintBoundaries,
        bool addSemanticIndexes,
        SemanticIndexCallback semanticIndexCallback,
        int semanticIndexOffset)
    {
        Key? key = child.Key is null ? null : new SliverChildKey(child.Key);
        if (addRepaintBoundaries)
        {
            child = new RepaintBoundary(child);
        }

        if (addSemanticIndexes)
        {
            // Dart passes the already-wrapped child, not the raw one, to the callback.
            int? semanticIndex = semanticIndexCallback(child, index);
            if (semanticIndex is not null)
            {
                child = new IndexedSemantics(index: semanticIndex.Value + semanticIndexOffset, child: child);
            }
        }

        if (addAutomaticKeepAlives)
        {
            child = new AutomaticKeepAlive(new SelectionKeepAlive(child));
        }

        return new KeyedSubtree(child, key);
    }

    /// <remarks>Flutter's file-level <c>_createErrorWidget</c>.</remarks>
    private protected static Widget CreateErrorWidget(Exception exception)
    {
        var details = new FlutterErrorDetails(
            exception: exception,
            stack: exception.StackTrace,
            library: "widgets library",
            context: new ErrorDescription("building"));
        FlutterError.ReportError(details);
        return ErrorWidget.Builder(details);
    }
}

/// <remarks>
/// Flutter's <c>_SaltedValueKey</c>: a <c>ValueKey&lt;Key&gt;</c> whose distinct runtime type lets
/// <c>findIndexByKey</c> tell a salted child key from a caller's own key and unwrap it.
/// </remarks>
internal sealed record SliverChildKey(Key Value) : LocalKey;

public sealed class SliverChildBuilderDelegate : SliverChildDelegate
{
    public SliverChildBuilderDelegate(
        NullableIndexedWidgetBuilder builder,
        int? childCount = null,
        bool addAutomaticKeepAlives = true,
        bool addRepaintBoundaries = true,
        bool addSemanticIndexes = true,
        SemanticIndexCallback? semanticIndexCallback = null,
        int semanticIndexOffset = 0,
        ChildIndexGetter? findChildIndexCallback = null)
    {
        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        ChildCount = childCount;
        AddAutomaticKeepAlives = addAutomaticKeepAlives;
        AddRepaintBoundaries = addRepaintBoundaries;
        AddSemanticIndexes = addSemanticIndexes;
        SemanticIndexCallback = semanticIndexCallback ?? DefaultSemanticIndexCallback;
        SemanticIndexOffset = semanticIndexOffset;
        FindChildIndexCallback = findChildIndexCallback;
    }

    /// <summary>Called to build children for the sliver, for indices in <c>[0, ChildCount)</c>.</summary>
    public NullableIndexedWidgetBuilder Builder { get; }

    /// <summary>The total number of children this delegate can provide, or null when unbounded.</summary>
    public int? ChildCount { get; }

    /// <summary>Whether to wrap each child in an <see cref="AutomaticKeepAlive"/>.</summary>
    public bool AddAutomaticKeepAlives { get; }

    /// <summary>Whether to wrap each child in a <see cref="RepaintBoundary"/>.</summary>
    public bool AddRepaintBoundaries { get; }

    /// <summary>Whether to wrap each child in an <see cref="IndexedSemantics"/>.</summary>
    public bool AddSemanticIndexes { get; }

    /// <summary>Maps a built child and its position onto its semantic index.</summary>
    public SemanticIndexCallback SemanticIndexCallback { get; }

    /// <summary>An initial offset to add to the semantic indices generated by this delegate.</summary>
    public int SemanticIndexOffset { get; }

    public override int? EstimatedChildCount => ChildCount;

    public ChildIndexGetter? FindChildIndexCallback { get; }

    public override int? FindIndexByKey(Key key)
    {
        if (FindChildIndexCallback is null)
        {
            return null;
        }

        return FindChildIndexCallback(key is SliverChildKey childKey ? childKey.Value : key);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Flutter's <c>SliverChildBuilderDelegate.shouldRebuild</c> is unconditionally true: a new
    /// delegate instance always re-runs the builder (which is what makes
    /// <see cref="FindChildIndexCallback"/> run on every widget update).
    /// </remarks>
    public override bool ShouldRebuild(SliverChildDelegate oldDelegate) => true;

    public override Widget? Build(BuildContext context, int index)
    {
        if (index < 0 || (ChildCount.HasValue && index >= ChildCount.Value))
        {
            return null;
        }

        Widget? child;
        try
        {
            child = Builder(context, index);
        }
        catch (Exception exception)
        {
            child = CreateErrorWidget(exception);
        }

        if (child is null)
        {
            return null;
        }

        return WrapChild(
            child,
            index,
            AddAutomaticKeepAlives,
            AddRepaintBoundaries,
            AddSemanticIndexes,
            SemanticIndexCallback,
            SemanticIndexOffset);
    }
}

public sealed class SliverChildListDelegate : SliverChildDelegate
{
    /// <summary>
    /// Maps a child's key to its index, filled lazily by <see cref="FindIndexByKey"/>. Null for a
    /// <see cref="Fixed"/> delegate, whose children never move.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>SliverChildListDelegate._keyToIndex</c>. Dart parks the scan cursor in the same
    /// map under the <c>null</c> key; a C# <c>Dictionary</c> takes no null key, so the cursor is
    /// <see cref="_keyToIndexCursor"/>.
    /// </remarks>
    private readonly Dictionary<Key, int>? _keyToIndex;
    private int _keyToIndexCursor;

    public SliverChildListDelegate(
        IReadOnlyList<Widget> children,
        bool addAutomaticKeepAlives = true,
        bool addRepaintBoundaries = true,
        bool addSemanticIndexes = true,
        SemanticIndexCallback? semanticIndexCallback = null,
        int semanticIndexOffset = 0)
        : this(children, [], addAutomaticKeepAlives, addRepaintBoundaries, addSemanticIndexes,
            semanticIndexCallback, semanticIndexOffset)
    {
    }

    private SliverChildListDelegate(
        IReadOnlyList<Widget> children,
        Dictionary<Key, int>? keyToIndex,
        bool addAutomaticKeepAlives,
        bool addRepaintBoundaries,
        bool addSemanticIndexes,
        SemanticIndexCallback? semanticIndexCallback,
        int semanticIndexOffset)
    {
        Children = children;
        _keyToIndex = keyToIndex;
        AddAutomaticKeepAlives = addAutomaticKeepAlives;
        AddRepaintBoundaries = addRepaintBoundaries;
        AddSemanticIndexes = addSemanticIndexes;
        SemanticIndexCallback = semanticIndexCallback ?? DefaultSemanticIndexCallback;
        SemanticIndexOffset = semanticIndexOffset;
    }

    /// <summary>
    /// A delegate for a child list that will not be mutated, so no key-to-index bookkeeping is kept
    /// and <see cref="FindIndexByKey"/> never remaps a child.
    /// </summary>
    /// <remarks>Flutter's <c>SliverChildListDelegate.fixed</c>.</remarks>
    public static SliverChildListDelegate Fixed(
        IReadOnlyList<Widget> children,
        bool addAutomaticKeepAlives = true,
        bool addRepaintBoundaries = true,
        bool addSemanticIndexes = true,
        SemanticIndexCallback? semanticIndexCallback = null,
        int semanticIndexOffset = 0)
    {
        return new SliverChildListDelegate(
            children,
            keyToIndex: null,
            addAutomaticKeepAlives,
            addRepaintBoundaries,
            addSemanticIndexes,
            semanticIndexCallback,
            semanticIndexOffset);
    }

    /// <summary>The widgets to display.</summary>
    public IReadOnlyList<Widget> Children { get; }

    /// <summary>Whether to wrap each child in an <see cref="AutomaticKeepAlive"/>.</summary>
    public bool AddAutomaticKeepAlives { get; }

    /// <summary>Whether to wrap each child in a <see cref="RepaintBoundary"/>.</summary>
    public bool AddRepaintBoundaries { get; }

    /// <summary>Whether to wrap each child in an <see cref="IndexedSemantics"/>.</summary>
    public bool AddSemanticIndexes { get; }

    /// <summary>Maps a built child and its position onto its semantic index.</summary>
    public SemanticIndexCallback SemanticIndexCallback { get; }

    /// <summary>An initial offset to add to the semantic indices generated by this delegate.</summary>
    public int SemanticIndexOffset { get; }

    private bool IsConstantInstance => _keyToIndex is null;

    public override int? EstimatedChildCount => Children.Count;

    /// <inheritdoc />
    /// <remarks>
    /// Flutter's <c>SliverChildListDelegate.shouldRebuild</c>: <c>List</c> has no value equality in
    /// Dart, so this is reference identity of the child list.
    /// </remarks>
    public override bool ShouldRebuild(SliverChildDelegate oldDelegate)
    {
        return !ReferenceEquals(Children, ((SliverChildListDelegate)oldDelegate).Children);
    }

    public override int? FindIndexByKey(Key key)
    {
        Key childKey = key is SliverChildKey saltedKey ? saltedKey.Value : key;
        return FindChildIndex(childKey);
    }

    /// <remarks>Flutter's <c>SliverChildListDelegate._findChildIndex</c>.</remarks>
    private int? FindChildIndex(Key key)
    {
        if (IsConstantInstance)
        {
            return null;
        }

        if (_keyToIndex!.TryGetValue(key, out int cached))
        {
            return cached;
        }

        int index = _keyToIndexCursor;
        while (index < Children.Count)
        {
            Widget child = Children[index];
            if (child.Key is not null)
            {
                _keyToIndex[child.Key] = index;
            }

            if (Equals(child.Key, key))
            {
                // Record the current index for the next call.
                _keyToIndexCursor = index + 1;
                return index;
            }

            index += 1;
        }

        _keyToIndexCursor = index;
        return null;
    }

    public override Widget? Build(BuildContext context, int index)
    {
        if (index < 0 || index >= Children.Count)
        {
            return null;
        }

        return WrapChild(
            Children[index],
            index,
            AddAutomaticKeepAlives,
            AddRepaintBoundaries,
            AddSemanticIndexes,
            SemanticIndexCallback,
            SemanticIndexOffset);
    }
}

/// <summary>
/// Keeps a list child that holds part of the active selection alive while it is scrolled out of
/// view, by interposing itself as the <see cref="ISelectionRegistrar"/> its descendants register with.
/// </summary>
/// <remarks>Flutter's private <c>_SelectionKeepAlive</c>.</remarks>
internal sealed class SelectionKeepAlive : StatefulWidget
{
    public SelectionKeepAlive(Widget child)
    {
        Child = child;
    }

    /// <summary>The widget below this widget in the tree.</summary>
    public Widget Child { get; }

    public override State CreateState() => new SelectionKeepAliveState();

    private sealed class SelectionKeepAliveState : AutomaticKeepAliveClientMixin, ISelectionRegistrar
    {
        private HashSet<ISelectable>? _selectablesWithSelections;
        private Dictionary<ISelectable, Action>? _selectableAttachments;
        private ISelectionRegistrar? _registrar;
        private bool _wantKeepAlive;

        private SelectionKeepAlive CurrentWidget => (SelectionKeepAlive)Element.Widget;

        protected override bool WantKeepAlive => _wantKeepAlive;

        private bool KeepAliveWanted
        {
            set
            {
                if (_wantKeepAlive != value)
                {
                    _wantKeepAlive = value;
                    UpdateKeepAlive();
                }
            }
        }

        public override void DidChangeDependencies()
        {
            base.DidChangeDependencies();
            ISelectionRegistrar? newRegistrar = SelectionContainer.MaybeOf(Context);
            if (!ReferenceEquals(_registrar, newRegistrar))
            {
                if (_registrar is not null && _selectableAttachments is not null)
                {
                    foreach (ISelectable selectable in _selectableAttachments.Keys)
                    {
                        _registrar.Remove(selectable);
                    }
                }

                _registrar = newRegistrar;
                if (_registrar is not null && _selectableAttachments is not null)
                {
                    foreach (ISelectable selectable in _selectableAttachments.Keys)
                    {
                        _registrar.Add(selectable);
                    }
                }
            }
        }

        public void Add(ISelectable selectable)
        {
            Action attachment = ListensTo(selectable);
            selectable.AddListener(attachment);
            _selectableAttachments ??= [];
            _selectableAttachments[selectable] = attachment;
            _registrar!.Add(selectable);
            if (selectable.Value.HasSelection)
            {
                UpdateSelectablesWithSelections(selectable, add: true);
            }
        }

        public void Remove(ISelectable selectable)
        {
            if (_selectableAttachments is null)
            {
                return;
            }

            if (!_selectableAttachments.Remove(selectable, out Action? attachment))
            {
                return;
            }

            selectable.RemoveListener(attachment);
            _registrar!.Remove(selectable);
            UpdateSelectablesWithSelections(selectable, add: false);
        }

        public override void Dispose()
        {
            if (_selectableAttachments is not null)
            {
                foreach ((ISelectable selectable, Action attachment) in _selectableAttachments)
                {
                    _registrar!.Remove(selectable);
                    selectable.RemoveListener(attachment);
                }

                _selectableAttachments = null;
            }

            _selectablesWithSelections = null;
            base.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            return _registrar is null
                ? CurrentWidget.Child
                : new SelectionRegistrarScope(this, CurrentWidget.Child);
        }

        private Action ListensTo(ISelectable selectable)
        {
            return () => UpdateSelectablesWithSelections(selectable, add: selectable.Value.HasSelection);
        }

        private void UpdateSelectablesWithSelections(ISelectable selectable, bool add)
        {
            if (add)
            {
                _selectablesWithSelections ??= [];
                _selectablesWithSelections.Add(selectable);
            }
            else
            {
                _selectablesWithSelections?.Remove(selectable);
            }

            KeepAliveWanted = _selectablesWithSelections?.Count > 0;
        }
    }
}
