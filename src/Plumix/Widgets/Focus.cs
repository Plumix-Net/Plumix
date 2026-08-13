using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/focus_manager.dart; flutter/packages/flutter/lib/src/widgets/focus_scope.dart (adapted)

namespace Plumix.Widgets;

public enum KeyEventResult
{
    Ignored,
    Handled,
    SkipRemainingHandlers
}

public enum FocusHighlightMode
{
    Touch,
    Traditional,
}

public enum FocusHighlightStrategy
{
    Automatic,
    AlwaysTouch,
    AlwaysTraditional,
}

public readonly record struct FocusTextInputState(
    string SurroundingText,
    int SelectionBaseOffset,
    int SelectionExtentOffset,
    Rect CursorRectangle,
    TextInputConfiguration? Configuration = null)
{
    public int SelectionStart => Math.Min(SelectionBaseOffset, SelectionExtentOffset);

    public int SelectionEnd => Math.Max(SelectionBaseOffset, SelectionExtentOffset);

    internal FocusTextInputState Normalize()
    {
        string normalizedText = SurroundingText ?? string.Empty;
        int clampedBaseOffset = Math.Clamp(SelectionBaseOffset, 0, normalizedText.Length);
        int clampedExtentOffset = Math.Clamp(SelectionExtentOffset, 0, normalizedText.Length);
        return new FocusTextInputState(
            normalizedText,
            clampedBaseOffset,
            clampedExtentOffset,
            CursorRectangle,
            Configuration);
    }
}

public delegate KeyEventResult FocusOnKeyEventCallback(FocusNode node, KeyEvent @event);
public delegate bool FocusOnTextInputCallback(FocusNode node, string text);
public delegate bool FocusOnTextCompositionCallback(FocusNode node, string text, bool isCommit);
public delegate FocusTextInputState? FocusOnTextInputStateCallback(FocusNode node);
public delegate bool FocusOnTextSelectionChangedCallback(FocusNode node, int baseOffset, int extentOffset);

public class FocusNode : ChangeNotifier
{
    private readonly List<FocusOnKeyEventCallback> _keyEventHandlers = [];
    private readonly Dictionary<object, bool> _traversalEligibility = [];
    private bool _hasFocus;
    private bool _canRequestFocus = true;
    private bool _skipTraversal;

    public bool HasFocus => _hasFocus;

    public bool CanRequestFocus
    {
        get => _canRequestFocus;
        set
        {
            if (_canRequestFocus == value)
            {
                return;
            }

            _canRequestFocus = value;

            if (!_canRequestFocus && _hasFocus)
            {
                Unfocus();
            }
        }
    }

    public bool SkipTraversal
    {
        get => _skipTraversal;
        set => _skipTraversal = value;
    }

    public FocusOnKeyEventCallback? OnKeyEvent { get; set; }

    public FocusOnTextInputCallback? OnTextInput { get; set; }

    public FocusOnTextCompositionCallback? OnTextComposition { get; set; }

    public FocusOnTextInputStateCallback? OnTextInputState { get; set; }

    public FocusOnTextSelectionChangedCallback? OnTextSelectionChanged { get; set; }

    public Rect? TraversalRect { get; set; }

    internal FocusManager? Manager { get; private set; }

    internal FocusScopeNode? Scope { get; private set; }

    internal Element? AttachmentElement { get; private set; }

    internal FocusTraversalGroupNode? TraversalGroup { get; set; }

    internal bool IsTraversalEligible => _traversalEligibility.Values.All(eligible => eligible);

    public bool RequestFocus()
    {
        return (Manager ?? FocusManager.Instance).RequestFocus(this);
    }

    /// <summary>Moves focus to the next node in the traversal order.</summary>
    public bool NextFocus()
    {
        return (Manager ?? FocusManager.Instance).FocusNext();
    }

    /// <summary>Moves focus to the previous node in the traversal order.</summary>
    public bool PreviousFocus()
    {
        return (Manager ?? FocusManager.Instance).FocusPrevious();
    }

    /// <summary>Moves focus to the closest node in <paramref name="direction"/>.</summary>
    public bool FocusInDirection(TraversalDirection direction)
    {
        return (Manager ?? FocusManager.Instance).FocusInDirection(direction);
    }

    /// <summary>Whether this node currently holds the primary focus.</summary>
    public bool HasPrimaryFocus =>
        ReferenceEquals((Manager ?? FocusManager.Instance).PrimaryFocus, this);

    public void Unfocus()
    {
        (Manager ?? FocusManager.Instance).Unfocus(this);
    }

    internal void AttachManager(FocusManager manager)
    {
        Manager = manager;
    }

    internal void DetachManager(FocusManager manager)
    {
        if (ReferenceEquals(Manager, manager))
        {
            Manager = null;
        }
    }

    internal void AttachScope(FocusScopeNode scope)
    {
        Scope = scope;
    }

    internal void DetachScope()
    {
        Scope = null;
    }

    internal void AttachElement(Element element)
    {
        AttachmentElement = element;
    }

    internal void DetachElement(Element element)
    {
        if (ReferenceEquals(AttachmentElement, element))
        {
            AttachmentElement = null;
        }
    }

    internal void SetHasFocus(bool value)
    {
        if (_hasFocus == value)
        {
            return;
        }

        _hasFocus = value;
        NotifyListeners();
    }

    internal KeyEventResult HandleKeyEvent(KeyEvent @event)
    {
        foreach (FocusOnKeyEventCallback handler in _keyEventHandlers.ToArray())
        {
            KeyEventResult result = handler(this, @event);
            if (result != KeyEventResult.Ignored)
            {
                return result;
            }
        }

        return OnKeyEvent?.Invoke(this, @event) ?? KeyEventResult.Ignored;
    }

    internal void AddKeyEventHandler(FocusOnKeyEventCallback handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        if (!_keyEventHandlers.Contains(handler))
        {
            _keyEventHandlers.Add(handler);
        }
    }

    internal void RemoveKeyEventHandler(FocusOnKeyEventCallback handler)
    {
        _keyEventHandlers.Remove(handler);
    }

    internal void SetTraversalEligibility(object owner, bool eligible)
    {
        ArgumentNullException.ThrowIfNull(owner);
        _traversalEligibility[owner] = eligible;
    }

    internal void RemoveTraversalEligibility(object owner)
    {
        _traversalEligibility.Remove(owner);
    }

    internal bool HandleTextInput(string text)
    {
        return OnTextInput?.Invoke(this, text) ?? false;
    }

    internal bool HandleTextComposition(string text, bool isCommit)
    {
        return OnTextComposition?.Invoke(this, text, isCommit) ?? false;
    }

    internal FocusTextInputState? ResolveTextInputState()
    {
        return OnTextInputState?.Invoke(this);
    }

    internal bool HandleTextSelectionChanged(int baseOffset, int extentOffset)
    {
        return OnTextSelectionChanged?.Invoke(this, baseOffset, extentOffset) ?? false;
    }

    internal Rect? ResolveTraversalRect()
    {
        if (TraversalRect.HasValue)
        {
            return TraversalRect.Value;
        }

        if (AttachmentElement?.RenderObject is not RenderBox renderBox || !renderBox.HasSize)
        {
            return null;
        }

        var localRect = new Rect(new Point(0, 0), renderBox.Size);
        var transformToRoot = ResolveRenderObjectTransformToRoot(renderBox);
        return RenderObject.TransformRect(transformToRoot, localRect);
    }

    private static Matrix ResolveRenderObjectTransformToRoot(RenderObject renderObject)
    {
        var transformToRoot = Matrix.Identity;
        RenderObject? child = renderObject;

        while (child?.Parent != null)
        {
            var parent = child.Parent;
            var childOffset = child.parentData is BoxParentData boxParentData
                ? boxParentData.offset
                : default;
            var childToParentTransform = Matrix.CreateTranslation(childOffset.X, childOffset.Y);

            if (parent is RenderTransform renderTransform)
            {
                childToParentTransform *= renderTransform.EffectiveTransform;
            }

            transformToRoot = childToParentTransform * transformToRoot;
            child = parent;
        }

        return transformToRoot;
    }

    public override void Dispose()
    {
        _keyEventHandlers.Clear();
        _traversalEligibility.Clear();
        (Manager ?? FocusManager.Instance).UnregisterNode(this);
        base.Dispose();
    }
}

public sealed class FocusScopeNode : FocusNode
{
    private readonly List<FocusNode> _members = [];

    public FocusNode? FocusedChild { get; private set; }

    /// <summary>How Tab/Shift-Tab traversal behaves at the first/last node of this scope.</summary>
    public TraversalEdgeBehavior TraversalEdgeBehavior { get; set; } = TraversalEdgeBehavior.ClosedLoop;

    /// <summary>How arrow-key traversal behaves at the edge node of this scope.</summary>
    public TraversalEdgeBehavior DirectionalTraversalEdgeBehavior { get; set; } = TraversalEdgeBehavior.Stop;

    /// <summary>Whether this scope or one of its descendants currently holds the primary focus.</summary>
    /// <remarks>Matches Flutter's ancestor-inclusive `FocusNode.hasFocus` for scopes.</remarks>
    public bool HasFocusInScope
    {
        get
        {
            FocusNode? node = (Manager ?? FocusManager.Instance).PrimaryFocus;
            while (node is not null)
            {
                if (ReferenceEquals(node, this))
                {
                    return true;
                }

                node = node.Scope;
            }

            return false;
        }
    }

    internal IReadOnlyList<FocusNode> Members => _members;

    internal void AddMember(FocusNode node)
    {
        if (_members.Contains(node))
        {
            return;
        }

        _members.Add(node);
    }

    internal void RemoveMember(FocusNode node)
    {
        if (!_members.Remove(node))
        {
            return;
        }

        if (ReferenceEquals(FocusedChild, node))
        {
            FocusedChild = null;
        }
    }

    internal void SetFocusedChild(FocusNode? node)
    {
        if (node != null && !ReferenceEquals(node.Scope, this))
        {
            return;
        }

        FocusedChild = node;
    }

    internal void ResetForTests()
    {
        _members.Clear();
        FocusedChild = null;
    }
}

public sealed class FocusManager
{
    private readonly List<FocusNode> _nodes = [];
    private readonly List<Action<FocusHighlightMode>> _highlightModeListeners = [];
    private readonly FocusScopeNode _rootScope = new()
    {
        CanRequestFocus = false,
        SkipTraversal = true
    };

    public FocusManager() : this(registerGlobalHandlers: false)
    {
    }

    private FocusManager(bool registerGlobalHandlers)
    {
        _rootScope.AttachManager(this);
        if (registerGlobalHandlers)
        {
            GestureBinding.PointerEventReceived += HandlePointerEvent;
        }
    }

    public static FocusManager Instance { get; } = new(registerGlobalHandlers: true);

    public FocusNode? PrimaryFocus { get; private set; }

    internal event Action? PrimaryFocusChanged;

    private FocusHighlightMode _highlightMode = ResolveDefaultHighlightMode();
    private FocusHighlightStrategy _highlightStrategy = FocusHighlightStrategy.Automatic;
    private bool? _lastInteractionRequiresTraditionalHighlights;

    public FocusScopeNode RootScope => _rootScope;

    public FocusHighlightMode HighlightMode => _highlightMode;

    public FocusHighlightStrategy HighlightStrategy
    {
        get => _highlightStrategy;
        set
        {
            if (_highlightStrategy == value)
            {
                return;
            }

            _highlightStrategy = value;
            UpdateHighlightMode();
        }
    }

    public void AddHighlightModeListener(Action<FocusHighlightMode> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        if (!_highlightModeListeners.Contains(listener))
        {
            _highlightModeListeners.Add(listener);
        }
    }

    public void RemoveHighlightModeListener(Action<FocusHighlightMode> listener)
    {
        _highlightModeListeners.Remove(listener);
    }

    public void RegisterNode(FocusNode node, FocusScopeNode? scope = null)
    {
        var effectiveScope = scope ?? _rootScope;

        if (node.Manager != null && !ReferenceEquals(node.Manager, this))
        {
            node.Manager.UnregisterNode(node);
        }

        if (!ReferenceEquals(effectiveScope.Manager, this))
        {
            if (effectiveScope.Manager != null)
            {
                effectiveScope.Manager.UnregisterNode(effectiveScope);
            }

            RegisterNode(effectiveScope, _rootScope);
        }

        if (_nodes.Contains(node))
        {
            MoveNodeToScope(node, effectiveScope);
            return;
        }

        _nodes.Add(node);
        node.AttachManager(this);
        MoveNodeToScope(node, effectiveScope);
    }

    public void UnregisterNode(FocusNode node)
    {
        if (!_nodes.Remove(node))
        {
            return;
        }

        node.DetachManager(this);
        node.Scope?.RemoveMember(node);
        node.DetachScope();

        if (ReferenceEquals(PrimaryFocus, node))
        {
            SetPrimaryFocus(null);
        }
    }

    public bool RequestFocus(FocusNode node)
    {
        if (!node.CanRequestFocus)
        {
            return false;
        }

        RegisterNode(node, node.Scope ?? _rootScope);

        if (ReferenceEquals(PrimaryFocus, node))
        {
            return true;
        }

        SetPrimaryFocus(node);
        return true;
    }

    public void Unfocus(FocusNode node)
    {
        if (ReferenceEquals(PrimaryFocus, node))
        {
            SetPrimaryFocus(null);
        }
    }

    public bool FocusNext()
    {
        return MoveFocusOrdinal(forward: true, directional: false);
    }

    public bool FocusPrevious()
    {
        return MoveFocusOrdinal(forward: false, directional: false);
    }

    private bool MoveFocusOrdinal(bool forward, bool directional)
    {
        var candidates = CollectTraversalCandidates();
        if (candidates.Count == 0)
        {
            return false;
        }

        int currentIndex = PrimaryFocus != null ? candidates.IndexOf(PrimaryFocus) : -1;
        if (forward)
        {
            for (int index = currentIndex >= 0 ? currentIndex + 1 : 0; index < candidates.Count; index++)
            {
                if (RequestFocus(candidates[index]))
                {
                    return true;
                }
            }
        }
        else
        {
            for (int index = currentIndex >= 0 ? currentIndex - 1 : candidates.Count - 1; index >= 0; index--)
            {
                if (RequestFocus(candidates[index]))
                {
                    return true;
                }
            }
        }

        return currentIndex >= 0 && HandleTraversalEdge(candidates, currentIndex, forward, directional);
    }

    /// <summary>
    /// Flutter's <c>FocusTraversalPolicy._moveFocus</c> edge handling: what happens after the traversal
    /// walked past the first/last node of the primary focus's nearest scope without finding a taker.
    /// Directional moves that fell back to ordinal order consult the directional edge behavior.
    /// </summary>
    private bool HandleTraversalEdge(List<FocusNode> candidates, int currentIndex, bool forward, bool directional)
    {
        FocusNode? current = PrimaryFocus;
        FocusScopeNode scope = NearestScopeOf(current);
        switch (directional ? scope.DirectionalTraversalEdgeBehavior : scope.TraversalEdgeBehavior)
        {
            case TraversalEdgeBehavior.LeaveFlutterView:
                current?.Unfocus();
                return false;
            case TraversalEdgeBehavior.Stop:
                return false;
            case TraversalEdgeBehavior.ParentScope:
                FocusScopeNode? parentScope = ((FocusNode)scope).Scope;
                if (parentScope != null && !ReferenceEquals(parentScope, _rootScope))
                {
                    current?.Unfocus();
                    var parentCandidates = CollectScopeCandidates(parentScope, current ?? parentScope)
                        .Where(candidate => !IsInsideScope(candidate, scope))
                        .ToList();
                    if (parentCandidates.Count == 0)
                    {
                        return false;
                    }

                    foreach (FocusNode candidate in forward ? parentCandidates : Reversed(parentCandidates))
                    {
                        if (RequestFocus(candidate))
                        {
                            return true;
                        }
                    }

                    return false;
                }

                goto case TraversalEdgeBehavior.ClosedLoop;
            case TraversalEdgeBehavior.ClosedLoop:
            default:
                if (forward)
                {
                    for (int index = 0; index <= currentIndex; index++)
                    {
                        if (RequestFocus(candidates[index]))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    for (int index = candidates.Count - 1; index >= currentIndex; index--)
                    {
                        if (RequestFocus(candidates[index]))
                        {
                            return true;
                        }
                    }
                }

                return false;
        }
    }

    /// <summary>Flutter's <c>FocusNode.nearestScope</c>: a scope node is its own nearest scope.</summary>
    private FocusScopeNode NearestScopeOf(FocusNode? node) => node switch
    {
        FocusScopeNode scopeNode => scopeNode,
        { } focusNode => focusNode.Scope ?? _rootScope,
        null => _rootScope,
    };

    private static bool IsInsideScope(FocusNode node, FocusScopeNode scope)
    {
        for (FocusScopeNode? ancestor = node.Scope; ancestor != null; ancestor = ((FocusNode)ancestor).Scope)
        {
            if (ReferenceEquals(ancestor, scope))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<FocusNode> Reversed(List<FocusNode> nodes)
    {
        for (int index = nodes.Count - 1; index >= 0; index--)
        {
            yield return nodes[index];
        }
    }

    public bool HandleKeyEvent(KeyEvent @event)
    {
        UpdateHighlightModeForKeyEvent();
        bool handledByHardwareKeyboard = HardwareKeyboard.Instance.HandleKeyEvent(@event);

#pragma warning disable CS0618
        RawKeyboard.Instance.HandleKeyEvent(@event);
#pragma warning restore CS0618

        if (handledByHardwareKeyboard)
        {
            return true;
        }

        if (PrimaryFocus != null)
        {
            KeyEventResult result = PrimaryFocus.HandleKeyEvent(@event);
            if (result != KeyEventResult.Ignored)
            {
                return result == KeyEventResult.Handled;
            }

            for (Element? ancestor = PrimaryFocus.AttachmentElement?.Parent;
                 ancestor != null;
                 ancestor = ancestor.Parent)
            {
                FocusNode? ancestorNode = _nodes.FirstOrDefault(
                    node => !ReferenceEquals(node, PrimaryFocus)
                            && ReferenceEquals(node.AttachmentElement, ancestor));
                if (ancestorNode == null)
                {
                    continue;
                }

                result = ancestorNode.HandleKeyEvent(@event);
                if (result != KeyEventResult.Ignored)
                {
                    return result == KeyEventResult.Handled;
                }
            }
        }

        if (!@event.IsDown)
        {
            return false;
        }

        if (!string.Equals(@event.Key, "Tab", StringComparison.Ordinal))
        {
            if (IsDirectionalNextKey(@event.Key))
            {
                return FocusInDirection(direction: @event.Key is "ArrowDown" or "Down"
                    ? TraversalDirection.Down
                    : TraversalDirection.Right);
            }

            if (IsDirectionalPreviousKey(@event.Key))
            {
                return FocusInDirection(direction: @event.Key is "ArrowUp" or "Up"
                    ? TraversalDirection.Up
                    : TraversalDirection.Left);
            }

            return false;
        }

        return @event.IsShiftPressed ? FocusPrevious() : FocusNext();
    }

    public bool HandleTextInput(string text)
    {
        if (PrimaryFocus == null || string.IsNullOrEmpty(text))
        {
            return false;
        }

        return PrimaryFocus.HandleTextInput(text);
    }

    public bool HandleTextCompositionUpdate(string text)
    {
        if (PrimaryFocus == null)
        {
            return false;
        }

        return PrimaryFocus.HandleTextComposition(text ?? string.Empty, isCommit: false);
    }

    public bool HandleTextCompositionCommit(string text)
    {
        if (PrimaryFocus == null)
        {
            return false;
        }

        return PrimaryFocus.HandleTextComposition(text ?? string.Empty, isCommit: true);
    }

    public FocusTextInputState? ResolveTextInputState()
    {
        return PrimaryFocus?.ResolveTextInputState()?.Normalize();
    }

    public bool HandleTextSelectionChanged(int baseOffset, int extentOffset)
    {
        if (PrimaryFocus == null)
        {
            return false;
        }

        return PrimaryFocus.HandleTextSelectionChanged(baseOffset, extentOffset);
    }

    internal void ResetForTests()
    {
        SetPrimaryFocus(null);

        foreach (var node in _nodes.ToArray())
        {
            node.Scope?.RemoveMember(node);
            node.DetachManager(this);
            node.DetachScope();
        }

        foreach (var node in _nodes)
        {
            if (node is FocusScopeNode scopeNode)
            {
                scopeNode.ResetForTests();
            }
        }

        _nodes.Clear();
        _rootScope.ResetForTests();
        _highlightModeListeners.Clear();
        _highlightMode = ResolveDefaultHighlightMode();
        _highlightStrategy = FocusHighlightStrategy.Automatic;
        _lastInteractionRequiresTraditionalHighlights = null;
        HardwareKeyboard.Instance.ResetForTests();
#pragma warning disable CS0618
        RawKeyboard.Instance.ResetForTests();
#pragma warning restore CS0618
    }

    internal void HandlePointerEvent(PointerEvent @event)
    {
        if (@event.Kind is not (PointerDeviceKind.Touch
            or PointerDeviceKind.Stylus
            or PointerDeviceKind.InvertedStylus))
        {
            return;
        }

        if (_lastInteractionRequiresTraditionalHighlights == true)
        {
            return;
        }

        _lastInteractionRequiresTraditionalHighlights = true;
        UpdateHighlightMode();
    }

    private void UpdateHighlightModeForKeyEvent()
    {
        if (_lastInteractionRequiresTraditionalHighlights == false)
        {
            return;
        }

        _lastInteractionRequiresTraditionalHighlights = false;
        UpdateHighlightMode();
    }

    private void UpdateHighlightMode()
    {
        FocusHighlightMode nextMode = _highlightStrategy switch
        {
            FocusHighlightStrategy.AlwaysTouch => FocusHighlightMode.Touch,
            FocusHighlightStrategy.AlwaysTraditional => FocusHighlightMode.Traditional,
            _ when _lastInteractionRequiresTraditionalHighlights == true => FocusHighlightMode.Touch,
            _ => FocusHighlightMode.Traditional,
        };

        if (_highlightMode == nextMode)
        {
            return;
        }

        _highlightMode = nextMode;
        foreach (Action<FocusHighlightMode> listener in _highlightModeListeners.ToArray())
        {
            listener(_highlightMode);
        }
    }

    private static FocusHighlightMode ResolveDefaultHighlightMode()
    {
        return OperatingSystem.IsAndroid() || OperatingSystem.IsIOS()
            ? FocusHighlightMode.Touch
            : FocusHighlightMode.Traditional;
    }

    private void SetPrimaryFocus(FocusNode? next)
    {
        if (ReferenceEquals(PrimaryFocus, next))
        {
            return;
        }

        var previous = PrimaryFocus;
        PrimaryFocus = next;

        if (previous != null && !ReferenceEquals(previous.Scope, next?.Scope))
        {
            previous.Scope?.SetFocusedChild(null);
        }

        next?.Scope?.SetFocusedChild(next);
        previous?.SetHasFocus(false);
        next?.SetHasFocus(true);
        PrimaryFocusChanged?.Invoke();
    }

    private void MoveNodeToScope(FocusNode node, FocusScopeNode scope)
    {
        if (ReferenceEquals(node.Scope, scope))
        {
            return;
        }

        node.Scope?.RemoveMember(node);
        node.AttachScope(scope);
        scope.AddMember(node);
    }

    private List<FocusNode> CollectTraversalCandidates()
    {
        FocusScopeNode scope = NearestScopeOf(PrimaryFocus);
        FocusNode currentNode = PrimaryFocus ?? scope;
        return CollectScopeCandidates(scope, currentNode);
    }

    /// <summary>
    /// The traversable nodes of <paramref name="scope"/> in reading order. Nested scopes are spliced in at
    /// their own position, mirroring Flutter's <c>FocusNode.traversalDescendants</c>, which walks the whole
    /// subtree instead of stopping at scope boundaries.
    /// </summary>
    private static List<FocusNode> CollectScopeCandidates(FocusScopeNode scope, FocusNode currentNode)
    {
        var directMembers = scope.Members
            .Where(candidate => candidate.TraversalGroup == null)
            .Where(candidate => candidate is FocusScopeNode nested
                ? nested.CanRequestFocus && !nested.SkipTraversal
                : IsTraversalCandidate(candidate, currentNode))
            .ToList();
        IReadOnlyList<FocusNode> sorted = new ReadingOrderTraversalPolicy()
            .SortDescendants(directMembers, currentNode);
        return FlattenTraversalGroups(sorted, scope, currentNode);
    }

    private static List<FocusNode> FlattenTraversalGroups(
        IEnumerable<FocusNode> sorted,
        FocusScopeNode scope,
        FocusNode currentNode)
    {
        var result = new List<FocusNode>();
        foreach (FocusNode node in sorted)
        {
            if (node is FocusScopeNode nestedScope)
            {
                result.AddRange(CollectScopeCandidates(nestedScope, currentNode));
                continue;
            }

            if (node is not FocusTraversalGroupNode groupNode)
            {
                result.Add(node);
                continue;
            }

            var directMembers = scope.Members
                .Where(candidate => ReferenceEquals(candidate.TraversalGroup, groupNode))
                .Where(candidate => IsTraversalCandidate(candidate, currentNode))
                .ToList();
            IReadOnlyList<FocusNode> groupSorted = groupNode.Policy
                .SortDescendants(directMembers, currentNode);
            result.AddRange(FlattenTraversalGroups(groupSorted, scope, currentNode));
        }

        return result;
    }

    private static bool IsTraversalCandidate(FocusNode candidate, FocusNode currentNode)
    {
        if (candidate is FocusTraversalGroupNode)
        {
            return true;
        }

        return ReferenceEquals(candidate, currentNode)
               || (candidate.CanRequestFocus && !candidate.SkipTraversal && candidate.IsTraversalEligible);
    }

    /// <summary>
    /// Moves the primary focus to the closest focusable node in <paramref name="direction"/>, falling
    /// back to ordinal traversal when no directional candidate exists.
    /// </summary>
    public bool FocusInDirection(TraversalDirection direction)
    {
        var candidates = CollectTraversalCandidates();
        if (candidates.Count == 0)
        {
            return false;
        }

        if (PrimaryFocus == null)
        {
            return direction switch
            {
                TraversalDirection.Left => RequestFocus(candidates[candidates.Count - 1]),
                TraversalDirection.Up => RequestFocus(candidates[candidates.Count - 1]),
                _ => RequestFocus(candidates[0])
            };
        }

        var sourceRect = PrimaryFocus.ResolveTraversalRect();
        if (!sourceRect.HasValue)
        {
            return MoveFocusOrdinal(
                forward: direction is TraversalDirection.Right or TraversalDirection.Down,
                directional: true);
        }

        FocusNode? bestNode = null;
        double bestPrimaryDistance = double.PositiveInfinity;
        double bestSecondaryDistance = double.PositiveInfinity;
        double bestDistanceSquared = double.PositiveInfinity;

        foreach (var candidate in candidates)
        {
            if (ReferenceEquals(candidate, PrimaryFocus))
            {
                continue;
            }

            var candidateRect = candidate.ResolveTraversalRect();
            if (!candidateRect.HasValue)
            {
                continue;
            }

            double dx = candidateRect.Value.Center.X - sourceRect.Value.Center.X;
            double dy = candidateRect.Value.Center.Y - sourceRect.Value.Center.Y;
            if (!TryComputeDirectionalDistance(direction, dx, dy, out double primaryDistance, out double secondaryDistance))
            {
                continue;
            }

            double distanceSquared = (dx * dx) + (dy * dy);
            if (primaryDistance < bestPrimaryDistance - 0.0001
                || (Math.Abs(primaryDistance - bestPrimaryDistance) <= 0.0001
                    && (secondaryDistance < bestSecondaryDistance - 0.0001
                        || (Math.Abs(secondaryDistance - bestSecondaryDistance) <= 0.0001
                            && distanceSquared < bestDistanceSquared))))
            {
                bestNode = candidate;
                bestPrimaryDistance = primaryDistance;
                bestSecondaryDistance = secondaryDistance;
                bestDistanceSquared = distanceSquared;
            }
        }

        if (bestNode != null)
        {
            return RequestFocus(bestNode);
        }

        return HandleDirectionalEdge(candidates, direction);
    }

    /// <summary>
    /// Flutter's <c>_onEdgeForDirection</c>: what happens when no candidate exists further in
    /// <paramref name="direction"/> from the current primary focus.
    /// </summary>
    private bool HandleDirectionalEdge(List<FocusNode> candidates, TraversalDirection direction)
    {
        FocusNode? current = PrimaryFocus;
        FocusScopeNode scope = NearestScopeOf(current);
        switch (scope.DirectionalTraversalEdgeBehavior)
        {
            case TraversalEdgeBehavior.LeaveFlutterView:
                current?.Unfocus();
                return false;
            case TraversalEdgeBehavior.ClosedLoop:
            case TraversalEdgeBehavior.ParentScope:
                // ParentScope falls back to the closed loop when the enclosing scope has no candidate,
                // which is always the case in this framework's flattened traversal model.
                FocusNode? opposite = FindOppositeEdgeCandidate(candidates, direction);
                return opposite != null && RequestFocus(opposite);
            case TraversalEdgeBehavior.Stop:
            default:
                return false;
        }
    }

    /// <summary>The candidate farthest in the opposite direction: the wrap target of a closed loop.</summary>
    private FocusNode? FindOppositeEdgeCandidate(List<FocusNode> candidates, TraversalDirection direction)
    {
        FocusNode? best = null;
        double bestCoordinate = double.PositiveInfinity;
        foreach (FocusNode candidate in candidates)
        {
            if (ReferenceEquals(candidate, PrimaryFocus))
            {
                continue;
            }

            Rect? rect = candidate.ResolveTraversalRect();
            if (!rect.HasValue)
            {
                continue;
            }

            double coordinate = direction switch
            {
                TraversalDirection.Right => rect.Value.Center.X,
                TraversalDirection.Left => -rect.Value.Center.X,
                TraversalDirection.Down => rect.Value.Center.Y,
                _ => -rect.Value.Center.Y,
            };
            if (coordinate < bestCoordinate)
            {
                bestCoordinate = coordinate;
                best = candidate;
            }
        }

        return best;
    }

    private static bool IsDirectionalNextKey(string key)
    {
        return string.Equals(key, "ArrowRight", StringComparison.Ordinal)
               || string.Equals(key, "ArrowDown", StringComparison.Ordinal)
               || string.Equals(key, "Right", StringComparison.Ordinal)
               || string.Equals(key, "Down", StringComparison.Ordinal);
    }

    private static bool IsDirectionalPreviousKey(string key)
    {
        return string.Equals(key, "ArrowLeft", StringComparison.Ordinal)
               || string.Equals(key, "ArrowUp", StringComparison.Ordinal)
               || string.Equals(key, "Left", StringComparison.Ordinal)
               || string.Equals(key, "Up", StringComparison.Ordinal);
    }

    private static bool TryComputeDirectionalDistance(
        TraversalDirection direction,
        double dx,
        double dy,
        out double primaryDistance,
        out double secondaryDistance)
    {
        switch (direction)
        {
            case TraversalDirection.Right:
                primaryDistance = dx;
                secondaryDistance = Math.Abs(dy);
                return primaryDistance > 0;
            case TraversalDirection.Left:
                primaryDistance = -dx;
                secondaryDistance = Math.Abs(dy);
                return primaryDistance > 0;
            case TraversalDirection.Down:
                primaryDistance = dy;
                secondaryDistance = Math.Abs(dx);
                return primaryDistance > 0;
            case TraversalDirection.Up:
                primaryDistance = -dy;
                secondaryDistance = Math.Abs(dx);
                return primaryDistance > 0;
            default:
                primaryDistance = double.PositiveInfinity;
                secondaryDistance = double.PositiveInfinity;
                return false;
        }
    }
}

internal sealed class FocusScopeMarker : InheritedWidget
{
    public FocusScopeMarker(
        FocusScopeNode scopeNode,
        Widget child,
        Key? key = null) : base(key)
    {
        ScopeNode = scopeNode;
        Child = child;
    }

    public FocusScopeNode ScopeNode { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !ReferenceEquals(((FocusScopeMarker)oldWidget).ScopeNode, ScopeNode);
    }
}

internal sealed class FocusDescendantsScope : InheritedWidget
{
    public FocusDescendantsScope(
        bool descendantsAreFocusable,
        bool descendantsAreTraversable,
        Widget child) : base()
    {
        DescendantsAreFocusable = descendantsAreFocusable;
        DescendantsAreTraversable = descendantsAreTraversable;
        Child = child;
    }

    public bool DescendantsAreFocusable { get; }

    public bool DescendantsAreTraversable { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        var oldScope = (FocusDescendantsScope)oldWidget;
        return oldScope.DescendantsAreFocusable != DescendantsAreFocusable
               || oldScope.DescendantsAreTraversable != DescendantsAreTraversable;
    }

    internal static (bool Focusable, bool Traversable) Resolve(BuildContext context)
    {
        bool focusable = true;
        bool traversable = true;
        foreach (FocusDescendantsScope scope in context.DependOnInheritedAncestors<FocusDescendantsScope>())
        {
            focusable &= scope.DescendantsAreFocusable;
            traversable &= scope.DescendantsAreTraversable;
        }

        return (focusable, traversable);
    }
}

public sealed class FocusScope : StatefulWidget
{
    public FocusScope(
        Widget child,
        FocusScopeNode? focusScopeNode = null,
        bool autofocus = false,
        bool canRequestFocus = true,
        bool skipTraversal = true,
        Key? key = null) : base(key)
    {
        Child = child;
        FocusScopeNode = focusScopeNode;
        Autofocus = autofocus;
        CanRequestFocus = canRequestFocus;
        SkipTraversal = skipTraversal;
    }

    public Widget Child { get; }

    public FocusScopeNode? FocusScopeNode { get; }

    public bool Autofocus { get; }

    public bool CanRequestFocus { get; }

    public bool SkipTraversal { get; }

    public static FocusScopeNode? MaybeOf(BuildContext context)
    {
        return context.DependOnInherited<FocusScopeMarker>()?.ScopeNode;
    }

    public override State CreateState()
    {
        return new FocusScopeState();
    }

    private sealed class FocusScopeState : State
    {
        private FocusScopeNode? _scopeNode;
        private bool _ownsScopeNode;
        private bool _autofocusApplied;

        private FocusScope Widget => (FocusScope)Element.Widget;

        public override void InitState()
        {
            AttachScopeNode(Widget.FocusScopeNode);
            ApplyWidgetConfiguration();
            EnsureScopeRegistration(scope: FocusManager.Instance.RootScope);
        }

        public override void DidChangeDependencies()
        {
            EnsureScopeRegistration(ResolveParentScope());
            ApplyAutofocusIfNeeded();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldScopeWidget = (FocusScope)oldWidget;
            var scopeWidget = Widget;

            if (!ReferenceEquals(oldScopeWidget.FocusScopeNode, scopeWidget.FocusScopeNode))
            {
                DetachScopeNode(disposeOwned: true);
                AttachScopeNode(scopeWidget.FocusScopeNode);
            }

            ApplyWidgetConfiguration();
            EnsureScopeRegistration(ResolveParentScope());

            if (!oldScopeWidget.Autofocus && scopeWidget.Autofocus)
            {
                _autofocusApplied = false;
            }

            ApplyAutofocusIfNeeded();
        }

        public override Widget Build(BuildContext context)
        {
            return new FocusScopeMarker(
                scopeNode: _scopeNode!,
                child: Widget.Child);
        }

        public override void Dispose()
        {
            DetachScopeNode(disposeOwned: true);
        }

        private FocusScopeNode ResolveParentScope()
        {
            return FocusScope.MaybeOf(Context) ?? FocusManager.Instance.RootScope;
        }

        private void AttachScopeNode(FocusScopeNode? externalNode)
        {
            _scopeNode = externalNode ?? new FocusScopeNode();
            _ownsScopeNode = externalNode is null;
        }

        private void DetachScopeNode(bool disposeOwned)
        {
            if (_scopeNode == null)
            {
                return;
            }

            FocusManager.Instance.UnregisterNode(_scopeNode);

            if (disposeOwned && _ownsScopeNode)
            {
                _scopeNode.Dispose();
            }

            _scopeNode = null;
            _ownsScopeNode = false;
            _autofocusApplied = false;
        }

        private void EnsureScopeRegistration(FocusScopeNode scope)
        {
            FocusManager.Instance.RegisterNode(_scopeNode!, scope);
        }

        private void ApplyWidgetConfiguration()
        {
            var node = _scopeNode!;
            node.CanRequestFocus = Widget.CanRequestFocus;
            node.SkipTraversal = Widget.SkipTraversal;
        }

        private void ApplyAutofocusIfNeeded()
        {
            if (!Widget.Autofocus || _autofocusApplied)
            {
                return;
            }

            _autofocusApplied = true;
            _scopeNode!.RequestFocus();
        }
    }
}

public sealed class Focus : StatefulWidget
{
    public Focus(
        Widget child,
        FocusNode? focusNode = null,
        bool autofocus = false,
        bool canRequestFocus = true,
        bool skipTraversal = false,
        bool descendantsAreFocusable = true,
        bool descendantsAreTraversable = true,
        Action<bool>? onFocusChange = null,
        FocusOnKeyEventCallback? onKeyEvent = null,
        FocusOnTextInputCallback? onTextInput = null,
        FocusOnTextCompositionCallback? onTextComposition = null,
        FocusOnTextInputStateCallback? onTextInputState = null,
        FocusOnTextSelectionChangedCallback? onTextSelectionChanged = null,
        Key? key = null) : this(
            child: child,
            includeSemantics: false,
            focusNode: focusNode,
            autofocus: autofocus,
            canRequestFocus: canRequestFocus,
            skipTraversal: skipTraversal,
            descendantsAreFocusable: descendantsAreFocusable,
            descendantsAreTraversable: descendantsAreTraversable,
            onFocusChange: onFocusChange,
            onKeyEvent: onKeyEvent,
            onTextInput: onTextInput,
            onTextComposition: onTextComposition,
            onTextInputState: onTextInputState,
            onTextSelectionChanged: onTextSelectionChanged,
            key: key)
    {
    }

    public Focus(
        Widget child,
        bool includeSemantics,
        FocusNode? focusNode = null,
        bool autofocus = false,
        bool canRequestFocus = true,
        bool skipTraversal = false,
        bool descendantsAreFocusable = true,
        bool descendantsAreTraversable = true,
        Action<bool>? onFocusChange = null,
        FocusOnKeyEventCallback? onKeyEvent = null,
        FocusOnTextInputCallback? onTextInput = null,
        FocusOnTextCompositionCallback? onTextComposition = null,
        FocusOnTextInputStateCallback? onTextInputState = null,
        FocusOnTextSelectionChangedCallback? onTextSelectionChanged = null,
        Key? key = null) : base(key)
    {
        Child = child;
        FocusNode = focusNode;
        Autofocus = autofocus;
        CanRequestFocus = canRequestFocus;
        SkipTraversal = skipTraversal;
        DescendantsAreFocusable = descendantsAreFocusable;
        DescendantsAreTraversable = descendantsAreTraversable;
        OnFocusChange = onFocusChange;
        IncludeSemantics = includeSemantics;
        OnKeyEvent = onKeyEvent;
        OnTextInput = onTextInput;
        OnTextComposition = onTextComposition;
        OnTextInputState = onTextInputState;
        OnTextSelectionChanged = onTextSelectionChanged;
    }

    public Widget Child { get; }

    public FocusNode? FocusNode { get; }

    public bool Autofocus { get; }

    public bool CanRequestFocus { get; }

    public bool SkipTraversal { get; }

    public bool DescendantsAreFocusable { get; }

    public bool DescendantsAreTraversable { get; }

    public Action<bool>? OnFocusChange { get; }

    public bool IncludeSemantics { get; }

    public FocusOnKeyEventCallback? OnKeyEvent { get; }

    public FocusOnTextInputCallback? OnTextInput { get; }

    public FocusOnTextCompositionCallback? OnTextComposition { get; }

    public FocusOnTextInputStateCallback? OnTextInputState { get; }

    public FocusOnTextSelectionChangedCallback? OnTextSelectionChanged { get; }

    public override State CreateState()
    {
        return new FocusState();
    }

    private sealed class FocusState : State
    {
        private FocusNode? _focusNode;
        private bool _ownsFocusNode;
        private bool _autofocusApplied;
        private bool _focused;

        private Focus Widget => (Focus)Element.Widget;

        public override void InitState()
        {
            AttachNode(Widget.FocusNode);
            ApplyWidgetConfiguration();
            EnsureNodeRegistration(scope: FocusManager.Instance.RootScope);
        }

        public override void DidChangeDependencies()
        {
            EnsureNodeRegistration(ResolveScope());
            ApplyWidgetConfiguration();
            ApplyAutofocusIfNeeded();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldFocusWidget = (Focus)oldWidget;
            var focusWidget = Widget;

            if (!ReferenceEquals(oldFocusWidget.FocusNode, focusWidget.FocusNode))
            {
                DetachNode(disposeOwned: true);
                AttachNode(focusWidget.FocusNode);
            }

            ApplyWidgetConfiguration();
            EnsureNodeRegistration(ResolveScope());

            if (!oldFocusWidget.Autofocus && focusWidget.Autofocus)
            {
                _autofocusApplied = false;
            }

            ApplyAutofocusIfNeeded();
        }

        public override Widget Build(BuildContext context)
        {
            Widget child = new FocusDescendantsScope(
                descendantsAreFocusable: Widget.DescendantsAreFocusable,
                descendantsAreTraversable: Widget.DescendantsAreTraversable,
                child: Widget.Child);
            child = new Listener(
                child: child,
                behavior: HitTestBehavior.Translucent,
                onPointerDown: HandlePointerDown);

            if (Widget.IncludeSemantics)
            {
                SemanticsFlags flags = _focusNode!.CanRequestFocus
                    ? SemanticsFlags.IsFocusable
                    : SemanticsFlags.None;
                if (_focusNode.CanRequestFocus && _focusNode.HasFocus)
                {
                    flags |= SemanticsFlags.IsFocused;
                }

                child = new Semantics(
                    child: child,
                    flags: flags)
                {
                    OnFocus = _focusNode.CanRequestFocus ? RequestSemanticFocus : null
                };
            }

            return child;
        }

        public override void Dispose()
        {
            DetachNode(disposeOwned: true);
        }

        private void HandlePointerDown(PointerDownEvent @event)
        {
            if (_focusNode == null || !_focusNode.CanRequestFocus)
            {
                return;
            }

            _focusNode.RequestFocus();
        }

        private void AttachNode(FocusNode? externalNode)
        {
            _focusNode = externalNode ?? new FocusNode();
            _ownsFocusNode = externalNode is null;
            _focusNode.AttachElement(Element);
            _focusNode.AddListener(HandleFocusChanged);
        }

        private FocusScopeNode ResolveScope()
        {
            return FocusScope.MaybeOf(Context) ?? FocusManager.Instance.RootScope;
        }

        private void DetachNode(bool disposeOwned)
        {
            if (_focusNode == null)
            {
                return;
            }

            _focusNode.RemoveListener(HandleFocusChanged);
            _focusNode.RemoveTraversalEligibility(this);
            _focusNode.TraversalGroup = null;
            FocusManager.Instance.UnregisterNode(_focusNode);
            _focusNode.DetachElement(Element);

            if (_ownsFocusNode)
            {
                _focusNode.OnKeyEvent = null;
                _focusNode.OnTextInput = null;
                _focusNode.OnTextComposition = null;
                _focusNode.OnTextInputState = null;
                _focusNode.OnTextSelectionChanged = null;
            }

            if (disposeOwned && _ownsFocusNode)
            {
                _focusNode.Dispose();
            }

            _focusNode = null;
            _ownsFocusNode = false;
            _autofocusApplied = false;
        }

        private void EnsureNodeRegistration(FocusScopeNode scope)
        {
            FocusManager.Instance.RegisterNode(_focusNode!, scope);
        }

        private void ApplyWidgetConfiguration()
        {
            var node = _focusNode!;
            bool includedByExcludeFocus = ExcludeFocus.DescendantsAreFocusableOf(Context);
            (bool focusable, bool traversable) = FocusDescendantsScope.Resolve(Context);
            node.CanRequestFocus = Widget.CanRequestFocus && includedByExcludeFocus && focusable;
            node.SkipTraversal = Widget.SkipTraversal;
            node.SetTraversalEligibility(
                this,
                includedByExcludeFocus && focusable && traversable);
            node.TraversalGroup = Context.GetInherited<FocusTraversalGroupMarker>()?.GroupNode;
            node.OnKeyEvent = Widget.OnKeyEvent;
            node.OnTextInput = Widget.OnTextInput;
            node.OnTextComposition = Widget.OnTextComposition;
            node.OnTextInputState = Widget.OnTextInputState;
            node.OnTextSelectionChanged = Widget.OnTextSelectionChanged;
        }

        private void ApplyAutofocusIfNeeded()
        {
            if (!Widget.Autofocus || _autofocusApplied)
            {
                return;
            }

            _autofocusApplied = true;
            _focusNode!.RequestFocus();
        }

        private void HandleFocusChanged()
        {
            bool focused = _focusNode?.HasFocus == true;
            if (_focused != focused)
            {
                _focused = focused;
                Widget.OnFocusChange?.Invoke(focused);
            }

            SetState(static () => { });
        }

        private void RequestSemanticFocus()
        {
            _focusNode?.RequestFocus();
        }
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/widgets/focus_traversal.dart
public sealed class ExcludeFocusTraversal : StatelessWidget
{
    public ExcludeFocusTraversal(
        Widget child,
        bool excluding = true,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Excluding = excluding;
    }

    public Widget Child { get; }

    public bool Excluding { get; }

    public override Widget Build(BuildContext context)
    {
        return new Focus(
            child: Child,
            canRequestFocus: false,
            skipTraversal: true,
            includeSemantics: false,
            descendantsAreTraversable: !Excluding);
    }
}

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/focus_scope.dart (ExcludeFocus)
public sealed class ExcludeFocus : InheritedWidget
{
    public ExcludeFocus(
        Widget child,
        bool excluding = true,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Excluding = excluding;
    }

    public Widget Child { get; }

    public bool Excluding { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return ((ExcludeFocus)oldWidget).Excluding != Excluding;
    }

    internal static bool DescendantsAreFocusableOf(BuildContext context)
    {
        return context.DependOnInheritedAncestors<ExcludeFocus>().All(scope => !scope.Excluding);
    }
}
