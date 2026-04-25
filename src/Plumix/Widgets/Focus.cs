using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/focus_manager.dart; flutter/packages/flutter/lib/src/widgets/focus_scope.dart (adapted)

namespace Plumix.Widgets;

public enum KeyEventResult
{
    Ignored,
    Handled
}

public readonly record struct FocusTextInputState(
    string SurroundingText,
    int SelectionBaseOffset,
    int SelectionExtentOffset,
    Rect CursorRectangle)
{
    public int SelectionStart => Math.Min(SelectionBaseOffset, SelectionExtentOffset);

    public int SelectionEnd => Math.Max(SelectionBaseOffset, SelectionExtentOffset);

    internal FocusTextInputState Normalize()
    {
        var normalizedText = SurroundingText ?? string.Empty;
        var clampedBaseOffset = Math.Clamp(SelectionBaseOffset, 0, normalizedText.Length);
        var clampedExtentOffset = Math.Clamp(SelectionExtentOffset, 0, normalizedText.Length);
        return new FocusTextInputState(
            normalizedText,
            clampedBaseOffset,
            clampedExtentOffset,
            CursorRectangle);
    }
}

public delegate KeyEventResult FocusOnKeyEventCallback(FocusNode node, KeyEvent @event);
public delegate bool FocusOnTextInputCallback(FocusNode node, string text);
public delegate bool FocusOnTextCompositionCallback(FocusNode node, string text, bool isCommit);
public delegate FocusTextInputState? FocusOnTextInputStateCallback(FocusNode node);
public delegate bool FocusOnTextSelectionChangedCallback(FocusNode node, int baseOffset, int extentOffset);

public class FocusNode : ChangeNotifier
{
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

    public bool RequestFocus()
    {
        return (Manager ?? FocusManager.Instance).RequestFocus(this);
    }

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
        return OnKeyEvent?.Invoke(this, @event) ?? KeyEventResult.Ignored;
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
                childToParentTransform *= renderTransform.Transform;
            }

            transformToRoot = childToParentTransform * transformToRoot;
            child = parent;
        }

        return transformToRoot;
    }

    public override void Dispose()
    {
        (Manager ?? FocusManager.Instance).UnregisterNode(this);
        base.Dispose();
    }
}

public sealed class FocusScopeNode : FocusNode
{
    private readonly List<FocusNode> _members = [];

    public FocusNode? FocusedChild { get; private set; }

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
    private readonly FocusScopeNode _rootScope = new()
    {
        CanRequestFocus = false,
        SkipTraversal = true
    };

    public FocusManager()
    {
        _rootScope.AttachManager(this);
    }

    public static FocusManager Instance { get; } = new();

    public FocusNode? PrimaryFocus { get; private set; }

    public FocusScopeNode RootScope => _rootScope;

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
        var candidates = CollectTraversalCandidates();
        if (candidates.Count == 0)
        {
            return false;
        }

        var currentIndex = PrimaryFocus != null ? candidates.IndexOf(PrimaryFocus) : -1;
        var startIndex = currentIndex >= 0 ? currentIndex + 1 : 0;

        for (var index = startIndex; index < candidates.Count; index++)
        {
            if (RequestFocus(candidates[index]))
            {
                return true;
            }
        }

        return false;
    }

    public bool FocusPrevious()
    {
        var candidates = CollectTraversalCandidates();
        if (candidates.Count == 0)
        {
            return false;
        }

        var currentIndex = PrimaryFocus != null ? candidates.IndexOf(PrimaryFocus) : -1;
        var startIndex = currentIndex >= 0 ? currentIndex - 1 : candidates.Count - 1;

        for (var index = startIndex; index >= 0; index--)
        {
            if (RequestFocus(candidates[index]))
            {
                return true;
            }
        }

        return false;
    }

    public bool HandleKeyEvent(KeyEvent @event)
    {
        if (PrimaryFocus != null && PrimaryFocus.HandleKeyEvent(@event) == KeyEventResult.Handled)
        {
            return true;
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
                    ? FocusTraversalDirection.Down
                    : FocusTraversalDirection.Right);
            }

            if (IsDirectionalPreviousKey(@event.Key))
            {
                return FocusInDirection(direction: @event.Key is "ArrowUp" or "Up"
                    ? FocusTraversalDirection.Up
                    : FocusTraversalDirection.Left);
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
        var scope = PrimaryFocus?.Scope ?? _rootScope;
        var result = new List<FocusNode>();

        foreach (var candidate in scope.Members)
        {
            if (candidate is FocusScopeNode)
            {
                continue;
            }

            if (!candidate.CanRequestFocus || candidate.SkipTraversal)
            {
                continue;
            }

            result.Add(candidate);
        }

        return result;
    }

    private bool FocusInDirection(FocusTraversalDirection direction)
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
                FocusTraversalDirection.Left => RequestFocus(candidates[candidates.Count - 1]),
                FocusTraversalDirection.Up => RequestFocus(candidates[candidates.Count - 1]),
                _ => RequestFocus(candidates[0])
            };
        }

        var sourceRect = PrimaryFocus.ResolveTraversalRect();
        if (!sourceRect.HasValue)
        {
            return direction is FocusTraversalDirection.Left or FocusTraversalDirection.Up
                ? FocusPrevious()
                : FocusNext();
        }

        FocusNode? bestNode = null;
        var bestPrimaryDistance = double.PositiveInfinity;
        var bestSecondaryDistance = double.PositiveInfinity;
        var bestDistanceSquared = double.PositiveInfinity;

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

            var dx = candidateRect.Value.Center.X - sourceRect.Value.Center.X;
            var dy = candidateRect.Value.Center.Y - sourceRect.Value.Center.Y;
            if (!TryComputeDirectionalDistance(direction, dx, dy, out var primaryDistance, out var secondaryDistance))
            {
                continue;
            }

            var distanceSquared = (dx * dx) + (dy * dy);
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

        return direction is FocusTraversalDirection.Left or FocusTraversalDirection.Up
            ? FocusPrevious()
            : FocusNext();
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
        FocusTraversalDirection direction,
        double dx,
        double dy,
        out double primaryDistance,
        out double secondaryDistance)
    {
        switch (direction)
        {
            case FocusTraversalDirection.Right:
                primaryDistance = dx;
                secondaryDistance = Math.Abs(dy);
                return primaryDistance > 0;
            case FocusTraversalDirection.Left:
                primaryDistance = -dx;
                secondaryDistance = Math.Abs(dy);
                return primaryDistance > 0;
            case FocusTraversalDirection.Down:
                primaryDistance = dy;
                secondaryDistance = Math.Abs(dx);
                return primaryDistance > 0;
            case FocusTraversalDirection.Up:
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

internal enum FocusTraversalDirection
{
    Left,
    Right,
    Up,
    Down
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

    protected internal override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !ReferenceEquals(((FocusScopeMarker)oldWidget).ScopeNode, ScopeNode);
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
            return new Listener(
                child: Widget.Child,
                behavior: HitTestBehavior.Translucent,
                onPointerDown: HandlePointerDown);
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
            node.CanRequestFocus = Widget.CanRequestFocus;
            node.SkipTraversal = Widget.SkipTraversal;
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
    }
}
