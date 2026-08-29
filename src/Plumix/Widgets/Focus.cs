using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/focus_manager.dart

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

/// <summary>Dart parity source: <c>UnfocusDisposition</c>.</summary>
public enum UnfocusDisposition
{
    /// <summary>Focus falls back to the nearest enclosing scope, which forgets its focused children.</summary>
    Scope,

    /// <summary>Focus falls back to the scope's previously focused child, if there is one.</summary>
    PreviouslyFocusedChild,
}

/// <summary>
/// The IME state a focused node exposes to the host. C#-only: Plumix routes platform text input
/// through the focus tree instead of Flutter's <c>TextInputConnection</c> channel.
/// </summary>
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
public delegate KeyEventResult OnKeyEventCallback(KeyEvent @event);
public delegate bool FocusOnTextInputCallback(FocusNode node, string text);
public delegate bool FocusOnTextCompositionCallback(FocusNode node, string text, bool isCommit);
public delegate FocusTextInputState? FocusOnTextInputStateCallback(FocusNode node);
public delegate bool FocusOnTextSelectionChangedCallback(FocusNode node, int baseOffset, int extentOffset);

/// <summary>Dart parity source: <c>combineKeyEventResults</c>.</summary>
public static class KeyEventResults
{
    public static KeyEventResult Combine(IEnumerable<KeyEventResult> results)
    {
        bool hasSkipRemainingHandlers = false;
        foreach (KeyEventResult result in results)
        {
            switch (result)
            {
                case KeyEventResult.Handled:
                    return KeyEventResult.Handled;
                case KeyEventResult.SkipRemainingHandlers:
                    hasSkipRemainingHandlers = true;
                    break;
                case KeyEventResult.Ignored:
                default:
                    break;
            }
        }

        return hasSkipRemainingHandlers ? KeyEventResult.SkipRemainingHandlers : KeyEventResult.Ignored;
    }
}

/// <summary>Dart parity source: <c>_Autofocus</c>.</summary>
internal readonly record struct PendingAutofocus(FocusScopeNode Scope, FocusNode AutofocusNode)
{
    internal void ApplyIfValid(FocusManager manager)
    {
        bool shouldApply =
            (Scope.Parent != null || ReferenceEquals(Scope, manager.RootScope))
            && ReferenceEquals(Scope.Manager, manager)
            && Scope.FocusedChild == null
            && AutofocusNode.Ancestors.Contains(Scope);
        if (shouldApply)
        {
            AutofocusNode.DoRequestFocus(findFirstFocus: true);
        }
    }
}

/// <summary>Dart parity source: <c>FocusAttachment</c>.</summary>
public sealed class FocusAttachment
{
    private readonly FocusNode _node;

    internal FocusAttachment(FocusNode node)
    {
        _node = node;
    }

    public bool IsAttached => ReferenceEquals(_node.Attachment, this);

    public void Detach()
    {
        if (!IsAttached)
        {
            return;
        }

        if (_node.HasPrimaryFocus
            || (_node.Manager != null && ReferenceEquals(_node.Manager.MarkedForFocus, _node)))
        {
            _node.Unfocus(UnfocusDisposition.PreviouslyFocusedChild);
        }

        _node.Manager?.MarkDetached(_node);
        _node.Parent?.RemoveChild(_node);
        _node.Attachment = null;
    }

    public void Reparent(FocusNode? parent = null)
    {
        if (!IsAttached)
        {
            return;
        }

        if (_node.Context is not { } context)
        {
            return;
        }

        parent ??= Focus.MaybeOf(context, scopeOk: true);
        parent ??= FocusManager.Instance.RootScope;
        parent.Reparent(_node);
    }
}

/// <summary>Dart parity source: <c>FocusNode</c>.</summary>
public class FocusNode : ChangeNotifier
{
    private readonly List<FocusNode> _children = [];
    private readonly List<FocusOnKeyEventCallback> _keyEventHandlers = [];
    private List<FocusNode>? _ancestors;
    private List<FocusNode>? _descendants;
    private FocusScopeNode? _enclosingScope;
    private bool _skipTraversal;
    private bool _canRequestFocus;
    private bool _descendantsAreFocusable;
    private bool _descendantsAreTraversable;
    private bool _hasKeyboardToken;
    private bool _requestFocusWhenReparented;

    public FocusNode(
        string? debugLabel = null,
        FocusOnKeyEventCallback? onKeyEvent = null,
        bool skipTraversal = false,
        bool canRequestFocus = true,
        bool descendantsAreFocusable = true,
        bool descendantsAreTraversable = true)
    {
        _skipTraversal = skipTraversal;
        _canRequestFocus = canRequestFocus;
        _descendantsAreFocusable = descendantsAreFocusable;
        _descendantsAreTraversable = descendantsAreTraversable;
        OnKeyEvent = onKeyEvent;
        DebugLabel = debugLabel;
    }

    public string? DebugLabel { get; set; }

    public FocusOnKeyEventCallback? OnKeyEvent { get; set; }

    /// <summary>C#-only: the host's IME text reaches the focused node through these callbacks.</summary>
    public FocusOnTextInputCallback? OnTextInput { get; set; }

    public FocusOnTextCompositionCallback? OnTextComposition { get; set; }

    public FocusOnTextInputStateCallback? OnTextInputState { get; set; }

    public FocusOnTextSelectionChangedCallback? OnTextSelectionChanged { get; set; }

    /// <summary>C#-only override of <see cref="Rect"/> for nodes whose render object is not the focus box.</summary>
    public Rect? TraversalRect { get; set; }

    internal FocusManager? Manager { get; private set; }

    internal FocusAttachment? Attachment { get; set; }

    /// <summary>Dart parity source: <c>FocusNode.context</c>.</summary>
    public BuildContext? Context { get; private set; }

    /// <summary>The element <see cref="Context"/> belongs to.</summary>
    internal Element? AttachmentElement => Context?.Owner;

    /// <summary>Dart parity source: <c>FocusNode.parent</c>.</summary>
    public FocusNode? Parent { get; private set; }

    /// <summary>Dart parity source: <c>FocusNode.children</c>.</summary>
    public IReadOnlyList<FocusNode> Children => _children;

    /// <summary>Dart parity source: <c>FocusNode.skipTraversal</c>.</summary>
    public bool SkipTraversal
    {
        get
        {
            if (_skipTraversal)
            {
                return true;
            }

            foreach (FocusNode ancestor in Ancestors)
            {
                if (!ancestor.DescendantsAreTraversable)
                {
                    return true;
                }
            }

            return false;
        }
        set
        {
            if (value == _skipTraversal)
            {
                return;
            }

            _skipTraversal = value;
            Manager?.MarkPropertiesChanged(this);
        }
    }

    /// <summary>Dart parity source: <c>FocusNode.canRequestFocus</c>.</summary>
    public bool CanRequestFocus
    {
        get => RawCanRequestFocus && Ancestors.All(static ancestor => ancestor.DescendantsAreFocusable);
        set
        {
            if (value == _canRequestFocus)
            {
                return;
            }

            _canRequestFocus = value;
            if (HasFocus && !value)
            {
                Unfocus(UnfocusDisposition.PreviouslyFocusedChild);
            }

            Manager?.MarkPropertiesChanged(this);
        }
    }

    /// <summary>The node's own flag, before the ancestor chain is consulted.</summary>
    private protected bool RawCanRequestFocus => _canRequestFocus;

    /// <summary>Dart parity source: <c>FocusNode.descendantsAreFocusable</c>.</summary>
    public virtual bool DescendantsAreFocusable
    {
        get => _descendantsAreFocusable;
        set
        {
            if (value == _descendantsAreFocusable)
            {
                return;
            }

            _descendantsAreFocusable = value;
            if (!value && HasFocus)
            {
                Unfocus(UnfocusDisposition.PreviouslyFocusedChild);
            }

            Manager?.MarkPropertiesChanged(this);
        }
    }

    /// <summary>Dart parity source: <c>FocusNode.descendantsAreTraversable</c>.</summary>
    public bool DescendantsAreTraversable
    {
        get => _descendantsAreTraversable;
        set
        {
            if (value == _descendantsAreTraversable)
            {
                return;
            }

            _descendantsAreTraversable = value;
            Manager?.MarkPropertiesChanged(this);
        }
    }

    /// <summary>Dart parity source: <c>FocusNode.traversalChildren</c>.</summary>
    public virtual IEnumerable<FocusNode> TraversalChildren =>
        DescendantsAreFocusable
            ? Children.Where(static node => !node.SkipTraversal && node.CanRequestFocus)
            : [];

    /// <summary>Dart parity source: <c>FocusNode.descendants</c>.</summary>
    public IReadOnlyList<FocusNode> Descendants
    {
        get
        {
            if (_descendants == null)
            {
                var result = new List<FocusNode>();
                foreach (FocusNode child in _children)
                {
                    result.AddRange(child.Descendants);
                    result.Add(child);
                }

                _descendants = result;
            }

            return _descendants;
        }
    }

    /// <summary>Dart parity source: <c>FocusNode.traversalDescendants</c>.</summary>
    public virtual IEnumerable<FocusNode> TraversalDescendants =>
        DescendantsAreFocusable
            ? Descendants.Where(static node => !node.SkipTraversal && node.CanRequestFocus)
            : [];

    /// <summary>Dart parity source: <c>FocusNode.ancestors</c>.</summary>
    public IReadOnlyList<FocusNode> Ancestors
    {
        get
        {
            if (_ancestors == null)
            {
                var result = new List<FocusNode>();
                for (FocusNode? parent = Parent; parent != null; parent = parent.Parent)
                {
                    result.Add(parent);
                }

                _ancestors = result;
            }

            return _ancestors;
        }
    }

    /// <summary>Dart parity source: <c>FocusNode.hasFocus</c>.</summary>
    public bool HasFocus =>
        HasPrimaryFocus || (Manager?.PrimaryFocus?.Ancestors.Contains(this) ?? false);

    /// <summary>Dart parity source: <c>FocusNode.hasPrimaryFocus</c>.</summary>
    public bool HasPrimaryFocus => ReferenceEquals(Manager?.PrimaryFocus, this);

    public FocusHighlightMode HighlightMode => FocusManager.Instance.HighlightMode;

    /// <summary>Dart parity source: <c>FocusNode.nearestScope</c>.</summary>
    public virtual FocusScopeNode? NearestScope => EnclosingScope;

    /// <summary>Dart parity source: <c>FocusNode.enclosingScope</c>.</summary>
    public FocusScopeNode? EnclosingScope => _enclosingScope ??= Parent?.NearestScope;

    /// <summary>Alias kept for call sites written against the pre-tree model.</summary>
    internal FocusScopeNode? Scope => EnclosingScope;

    /// <summary>Dart parity source: <c>FocusNode.rect</c>.</summary>
    public Rect Rect => ResolveTraversalRect() ?? default;

    /// <summary>Dart parity source: <c>FocusNode.size</c>.</summary>
    public Size Size => Rect.Size;

    /// <summary>Dart parity source: <c>FocusNode.offset</c>.</summary>
    public Point Offset => Rect.TopLeft;

    /// <summary>Dart parity source: <c>FocusNode.unfocus</c>.</summary>
    public void Unfocus(UnfocusDisposition disposition = UnfocusDisposition.Scope)
    {
        if (!HasFocus && (Manager == null || !ReferenceEquals(Manager.MarkedForFocus, this)))
        {
            return;
        }

        FocusScopeNode? scope = EnclosingScope;
        if (scope == null)
        {
            return;
        }

        switch (disposition)
        {
            case UnfocusDisposition.Scope:
                if (scope.CanRequestFocus)
                {
                    scope.ClearFocusedChildren();
                }

                while (scope != null && !scope.CanRequestFocus)
                {
                    scope = scope.EnclosingScope ?? Manager?.RootScope;
                }

                scope?.DoRequestFocus(findFirstFocus: false);
                break;
            case UnfocusDisposition.PreviouslyFocusedChild:
                if (scope.CanRequestFocus)
                {
                    scope.RemoveFocusedChild(this);
                }

                while (scope != null && !scope.CanRequestFocus)
                {
                    scope.EnclosingScope?.RemoveFocusedChild(scope);
                    scope = scope.EnclosingScope ?? Manager?.RootScope;
                }

                scope?.DoRequestFocus(findFirstFocus: true);
                break;
        }
    }

    /// <summary>Dart parity source: <c>FocusNode.consumeKeyboardToken</c>.</summary>
    public bool ConsumeKeyboardToken()
    {
        if (!_hasKeyboardToken)
        {
            return false;
        }

        _hasKeyboardToken = false;
        return true;
    }

    /// <summary>Dart parity source: <c>FocusNode.requestFocus</c>; returns whether the node ended up focused.</summary>
    public bool RequestFocus()
    {
        DoRequestFocus(findFirstFocus: true);
        return HasFocus;
    }

    /// <summary>Dart parity source: <c>FocusNode.requestFocus(node)</c>.</summary>
    public bool RequestFocus(FocusNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.Parent == null)
        {
            Reparent(node);
        }

        node.DoRequestFocus(findFirstFocus: true);
        return node.HasFocus;
    }

    /// <summary>Dart parity source: <c>FocusNode.nextFocus</c>.</summary>
    public bool NextFocus() => FocusTraversalGroup.PolicyForNode(this).Next(this);

    /// <summary>Dart parity source: <c>FocusNode.previousFocus</c>.</summary>
    public bool PreviousFocus() => FocusTraversalGroup.PolicyForNode(this).Previous(this);

    /// <summary>Dart parity source: <c>FocusNode.focusInDirection</c>.</summary>
    public bool FocusInDirection(TraversalDirection direction) =>
        FocusTraversalGroup.PolicyForNode(this).InDirection(this, direction);

    /// <summary>Dart parity source: <c>FocusNode.attach</c>.</summary>
    public FocusAttachment Attach(BuildContext? context, FocusOnKeyEventCallback? onKeyEvent = null)
    {
        Context = context;
        OnKeyEvent = onKeyEvent ?? OnKeyEvent;
        Attachment = new FocusAttachment(this);
        return Attachment;
    }

    /// <summary>C#-only: extra key handlers layered under <see cref="OnKeyEvent"/>.</summary>
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

    internal bool HandleTextInput(string text) => OnTextInput?.Invoke(this, text) ?? false;

    internal bool HandleTextComposition(string text, bool isCommit) =>
        OnTextComposition?.Invoke(this, text, isCommit) ?? false;

    internal FocusTextInputState? ResolveTextInputState() => OnTextInputState?.Invoke(this);

    internal bool HandleTextSelectionChanged(int baseOffset, int extentOffset) =>
        OnTextSelectionChanged?.Invoke(this, baseOffset, extentOffset) ?? false;

    /// <summary>Dart parity source: <c>FocusNode.rect</c>, resolved through the attached render object.</summary>
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
        Matrix4 transformToRoot = renderBox.ComputePaintTransformToRoot();
        return RenderObject.TransformRect(transformToRoot, localRect);
    }

    /// <summary>Dart parity source: <c>FocusNode._reparent</c>.</summary>
    internal void Reparent(FocusNode child)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (ReferenceEquals(child, this))
        {
            throw new ArgumentException("Tried to make a child into a parent of itself.", nameof(child));
        }

        if (ReferenceEquals(child.Parent, this))
        {
            return;
        }

        FocusScopeNode? oldScope = child.EnclosingScope;
        bool hadFocus = child.HasFocus;
        child.Parent?.RemoveChild(child, removeScopeFocus: !ReferenceEquals(oldScope, NearestScope));
        _children.Add(child);
        child.Parent = this;
        child.ResetAncestorCache();
        child.UpdateManager(Manager);
        foreach (FocusNode ancestor in child.Ancestors)
        {
            ancestor._descendants = null;
        }

        child.ClearEnclosingScopeCache();
        if (hadFocus)
        {
            Manager?.PrimaryFocus?.SetAsFocusedChildForScope();
        }

        if (oldScope != null && child.Context != null && !ReferenceEquals(child.EnclosingScope, oldScope))
        {
            FocusTraversalGroup.MaybeOf(child.Context.Value)?.ChangedScope(child, oldScope);
        }

        if (child._requestFocusWhenReparented)
        {
            child._requestFocusWhenReparented = false;
            child.DoRequestFocus(findFirstFocus: true);
        }
    }

    /// <summary>Dart parity source: <c>FocusNode._removeChild</c>.</summary>
    internal void RemoveChild(FocusNode node, bool removeScopeFocus = true)
    {
        if (!_children.Contains(node))
        {
            return;
        }

        if (removeScopeFocus && node.EnclosingScope is { } nodeScope)
        {
            nodeScope.RemoveFocusedChild(node);
            foreach (FocusNode descendant in node.Descendants.ToArray())
            {
                if (ReferenceEquals(descendant.EnclosingScope, nodeScope))
                {
                    nodeScope.RemoveFocusedChild(descendant);
                }
            }
        }

        node.Parent = null;
        node.ClearEnclosingScopeCache();
        node.ResetAncestorCache();
        _children.Remove(node);
        foreach (FocusNode ancestor in Ancestors)
        {
            ancestor._descendants = null;
        }

        _descendants = null;
    }

    /// <summary>Dart parity source: <c>FocusNode._clearEnclosingScopeCache</c>.</summary>
    private void ClearEnclosingScopeCache()
    {
        FocusScopeNode? cachedScope = _enclosingScope;
        if (cachedScope == null)
        {
            return;
        }

        _enclosingScope = null;
        foreach (FocusNode child in _children)
        {
            if (ReferenceEquals(cachedScope, child._enclosingScope))
            {
                child.ClearEnclosingScopeCache();
            }
        }
    }

    /// <summary>Drops the cached ancestor list of this node and of everything below it.</summary>
    private void ResetAncestorCache()
    {
        _ancestors = null;
        foreach (FocusNode descendant in _children)
        {
            descendant.ResetAncestorCache();
        }
    }

    /// <summary>Dart parity source: <c>FocusNode._updateManager</c>.</summary>
    internal void UpdateManager(FocusManager? manager)
    {
        Manager = manager;
        foreach (FocusNode descendant in Descendants)
        {
            descendant.Manager = manager;
            descendant._ancestors = null;
        }
    }

    /// <summary>Dart parity source: <c>FocusNode._markNextFocus</c>.</summary>
    private protected void MarkNextFocus(FocusNode newFocus)
    {
        if (Manager != null)
        {
            Manager.MarkNextFocus(this);
            return;
        }

        newFocus.SetAsFocusedChildForScope();
        newFocus.Notify();
        if (!ReferenceEquals(newFocus, this))
        {
            Notify();
        }
    }

    /// <summary>Dart parity source: <c>FocusNode._notify</c>.</summary>
    internal void Notify()
    {
        if (Parent == null)
        {
            return;
        }

        if (HasPrimaryFocus)
        {
            SetAsFocusedChildForScope();
        }

        NotifyListeners();
    }

    /// <summary>Dart parity source: <c>FocusNode._doRequestFocus</c>.</summary>
    internal virtual void DoRequestFocus(bool findFirstFocus)
    {
        if (!CanRequestFocus)
        {
            return;
        }

        if (Parent == null)
        {
            _requestFocusWhenReparented = true;
            return;
        }

        SetAsFocusedChildForScope();
        if (HasPrimaryFocus
            && (Manager!.MarkedForFocus == null || ReferenceEquals(Manager.MarkedForFocus, this)))
        {
            return;
        }

        _hasKeyboardToken = true;
        MarkNextFocus(this);
    }

    /// <summary>Dart parity source: <c>FocusNode._setAsFocusedChildForScope</c>.</summary>
    internal void SetAsFocusedChildForScope()
    {
        FocusNode scopeFocus = this;
        foreach (FocusNode ancestor in Ancestors)
        {
            if (ancestor is not FocusScopeNode scopeAncestor)
            {
                continue;
            }

            scopeAncestor.RemoveFocusedChild(scopeFocus);
            scopeAncestor.AddFocusedChild(scopeFocus);
            scopeFocus = scopeAncestor;
        }
    }

    public override void Dispose()
    {
        Attachment?.Detach();
        _keyEventHandlers.Clear();
        base.Dispose();
    }

    public override string ToString()
    {
        string extra = DebugLabel ?? string.Empty;
        if (HasPrimaryFocus)
        {
            extra = extra.Length == 0 ? "[PRIMARY FOCUS]" : extra + " [PRIMARY FOCUS]";
        }
        else if (HasFocus)
        {
            extra = extra.Length == 0 ? "[IN FOCUS PATH]" : extra + " [IN FOCUS PATH]";
        }

        return extra.Length == 0 ? GetType().Name : $"{GetType().Name}({extra})";
    }
}

/// <summary>Dart parity source: <c>FocusScopeNode</c>.</summary>
public sealed class FocusScopeNode : FocusNode
{
    private readonly List<FocusNode> _focusedChildren = [];

    public FocusScopeNode(
        string? debugLabel = null,
        FocusOnKeyEventCallback? onKeyEvent = null,
        bool skipTraversal = false,
        bool canRequestFocus = true,
        TraversalEdgeBehavior traversalEdgeBehavior = TraversalEdgeBehavior.ClosedLoop,
        TraversalEdgeBehavior directionalTraversalEdgeBehavior = TraversalEdgeBehavior.Stop)
        : base(
            debugLabel: debugLabel,
            onKeyEvent: onKeyEvent,
            skipTraversal: skipTraversal,
            canRequestFocus: canRequestFocus,
            descendantsAreFocusable: true)
    {
        TraversalEdgeBehavior = traversalEdgeBehavior;
        DirectionalTraversalEdgeBehavior = directionalTraversalEdgeBehavior;
    }

    public override FocusScopeNode? NearestScope => this;

    public override bool DescendantsAreFocusable
    {
        get => RawCanRequestFocus && base.DescendantsAreFocusable;
        set => base.DescendantsAreFocusable = value;
    }

    /// <summary>How Tab/Shift-Tab traversal behaves at the first/last node of this scope.</summary>
    public TraversalEdgeBehavior TraversalEdgeBehavior { get; set; }

    /// <summary>How arrow-key traversal behaves at the edge node of this scope.</summary>
    public TraversalEdgeBehavior DirectionalTraversalEdgeBehavior { get; set; }

    /// <summary>Dart parity source: <c>FocusScopeNode.isFirstFocus</c>.</summary>
    public bool IsFirstFocus => ReferenceEquals(EnclosingScope?.FocusedChild, this);

    /// <summary>Dart parity source: <c>FocusScopeNode.focusedChild</c>.</summary>
    public FocusNode? FocusedChild => _focusedChildren.Count == 0 ? null : _focusedChildren[^1];

    /// <summary>Alias kept for call sites written against the pre-tree model.</summary>
    public bool HasFocusInScope => HasFocus;

    public override IEnumerable<FocusNode> TraversalChildren =>
        CanRequestFocus ? base.TraversalChildren : [];

    public override IEnumerable<FocusNode> TraversalDescendants =>
        CanRequestFocus ? base.TraversalDescendants : [];

    /// <summary>Dart parity source: <c>FocusScopeNode.setFirstFocus</c>.</summary>
    public void SetFirstFocus(FocusScopeNode scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        if (ReferenceEquals(scope, this))
        {
            throw new ArgumentException("Unexpected self-reference in SetFirstFocus.", nameof(scope));
        }

        if (scope.Parent == null)
        {
            Reparent(scope);
        }

        if (HasFocus)
        {
            scope.DoRequestFocus(findFirstFocus: true);
        }
        else
        {
            scope.SetAsFocusedChildForScope();
        }
    }

    /// <summary>Dart parity source: <c>FocusScopeNode.autofocus</c>.</summary>
    public void Autofocus(FocusNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.Parent == null)
        {
            Reparent(node);
        }

        Manager?.AddPendingAutofocus(new PendingAutofocus(this, node));
        Manager?.MarkNeedsUpdate();
    }

    /// <summary>Dart parity source: <c>FocusScopeNode.requestScopeFocus</c>.</summary>
    public void RequestScopeFocus() => DoRequestFocus(findFirstFocus: false);

    /// <summary>Dart parity source: <c>FocusScopeNode._doRequestFocus(findFirstFocus: true)</c>.</summary>
    internal bool RequestFirstFocus()
    {
        DoRequestFocus(findFirstFocus: true);
        return HasFocus;
    }

    internal void AddFocusedChild(FocusNode node) => _focusedChildren.Add(node);

    internal void RemoveFocusedChild(FocusNode node) => _focusedChildren.Remove(node);

    internal void ClearFocusedChildren() => _focusedChildren.Clear();

    internal IReadOnlyList<FocusNode> FocusedChildren => _focusedChildren;

    internal override void DoRequestFocus(bool findFirstFocus)
    {
        while (_focusedChildren.Count > 0
               && (!_focusedChildren[^1].CanRequestFocus || _focusedChildren[^1].EnclosingScope == null))
        {
            _focusedChildren.RemoveAt(_focusedChildren.Count - 1);
        }

        FocusNode? focusedChild = FocusedChild;
        if (!findFirstFocus || focusedChild == null)
        {
            if (CanRequestFocus)
            {
                SetAsFocusedChildForScope();
                MarkNextFocus(this);
            }

            return;
        }

        focusedChild.DoRequestFocus(findFirstFocus: true);
    }
}

/// <summary>Dart parity source: <c>_AppLifecycleListener</c>.</summary>
internal sealed class FocusAppLifecycleListener : WidgetsBindingObserver
{
    private readonly Action<AppLifecycleState> _onLifecycleStateChanged;

    internal FocusAppLifecycleListener(Action<AppLifecycleState> onLifecycleStateChanged)
    {
        _onLifecycleStateChanged = onLifecycleStateChanged;
    }

    public void DidChangeAppLifecycleState(AppLifecycleState state) => _onLifecycleStateChanged(state);
}

/// <summary>Dart parity source: <c>FocusManager</c> (with <c>_HighlightModeManager</c> folded in).</summary>
public sealed class FocusManager : ChangeNotifier
{
    private readonly List<Action<FocusHighlightMode>> _highlightModeListeners = [];
    private readonly List<OnKeyEventCallback> _earlyKeyEventHandlers = [];
    private readonly List<OnKeyEventCallback> _lateKeyEventHandlers = [];
    private readonly HashSet<FocusNode> _dirtyNodes = [];
    private readonly List<PendingAutofocus> _pendingAutofocuses = [];
    private FocusScopeNode _rootScope = new(debugLabel: "Root Focus Scope");
    private FocusHighlightMode? _highlightMode;
    private FocusHighlightStrategy _highlightStrategy = FocusHighlightStrategy.Automatic;
    private bool? _lastInteractionRequiresTraditionalHighlights;
    private bool _applyingFocusChanges;
    private FocusAppLifecycleListener? _appLifecycleListener;
    private FocusNode? _suspendedNode;

    public FocusManager() : this(registerGlobalHandlers: false)
    {
    }

    private FocusManager(bool registerGlobalHandlers)
    {
        _rootScope.UpdateManager(this);
        if (RespondToLifecycleChange)
        {
            _appLifecycleListener = new FocusAppLifecycleListener(HandleAppLifecycleChange);
            WidgetsBinding.Instance.AddObserver(_appLifecycleListener);
        }

        if (registerGlobalHandlers)
        {
            GestureBinding.PointerEventReceived += HandlePointerEvent;
            RegisterKeyMessageHandler();
        }
    }

    /// <summary>Dart parity source: <c>FocusManager._respondToLifecycleChange</c>.</summary>
    private static bool RespondToLifecycleChange =>
        OperatingSystem.IsBrowser()
        || PlatformDefaults.TargetPlatform switch
        {
            TargetPlatform.Android or TargetPlatform.IOS => false,
            _ => true,
        };

    /// <summary>Dart parity source: <c>FocusManager._appLifecycleChange</c>.</summary>
    private void HandleAppLifecycleChange(AppLifecycleState state)
    {
        if (state == AppLifecycleState.Resumed)
        {
            if (!ReferenceEquals(PrimaryFocus, _rootScope))
            {
                _suspendedNode = null;
            }
            else if (_suspendedNode != null)
            {
                if (MarkedForFocus == null)
                {
                    _suspendedNode.RequestFocus();
                }

                _suspendedNode = null;
            }

            return;
        }

        if (!ReferenceEquals(PrimaryFocus, _rootScope))
        {
            MarkedForFocus = _rootScope;
            _suspendedNode = PrimaryFocus;
            ApplyFocusChangesIfNeeded();
        }
    }

    /// <summary>Dart parity source: <c>FocusManager.listenToApplicationLifecycleChangesIfSupported</c>.</summary>
    internal void ListenToApplicationLifecycleChangesIfSupported()
    {
        if (_appLifecycleListener == null && RespondToLifecycleChange)
        {
            _appLifecycleListener = new FocusAppLifecycleListener(HandleAppLifecycleChange);
            WidgetsBinding.Instance.AddObserver(_appLifecycleListener);
        }
    }

    public static FocusManager Instance { get; } = new(registerGlobalHandlers: true);

    public FocusNode? PrimaryFocus { get; private set; }

    internal event Action? PrimaryFocusChanged;

    public FocusScopeNode RootScope => _rootScope;

    internal FocusNode? MarkedForFocus { get; private set; }

    public FocusHighlightMode HighlightMode => _highlightMode ?? DefaultModeForPlatform;

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

    /// <summary>Dart parity source: <c>FocusManager.addEarlyKeyEventHandler</c>.</summary>
    public void AddEarlyKeyEventHandler(OnKeyEventCallback handler) => _earlyKeyEventHandlers.Add(handler);

    public void RemoveEarlyKeyEventHandler(OnKeyEventCallback handler) => _earlyKeyEventHandlers.Remove(handler);

    public void AddLateKeyEventHandler(OnKeyEventCallback handler) => _lateKeyEventHandlers.Add(handler);

    public void RemoveLateKeyEventHandler(OnKeyEventCallback handler) => _lateKeyEventHandlers.Remove(handler);

    /// <summary>
    /// C#-only: attaches <paramref name="node"/> under <paramref name="scope"/> without going through a
    /// <see cref="FocusAttachment"/>, for nodes that have no widget of their own.
    /// </summary>
    public void RegisterNode(FocusNode node, FocusScopeNode? scope = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        FocusScopeNode effectiveScope = scope ?? _rootScope;
        if (!ReferenceEquals(effectiveScope, _rootScope) && effectiveScope.Parent == null)
        {
            _rootScope.Reparent(effectiveScope);
        }

        effectiveScope.Reparent(node);
    }

    /// <summary>C#-only counterpart of <see cref="RegisterNode"/>.</summary>
    public void UnregisterNode(FocusNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.HasPrimaryFocus || ReferenceEquals(MarkedForFocus, node))
        {
            node.Unfocus(UnfocusDisposition.PreviouslyFocusedChild);
        }

        MarkDetached(node);
        node.Parent?.RemoveChild(node);
    }

    /// <summary>C#-only convenience wrapper around <see cref="FocusNode.RequestFocus()"/>.</summary>
    public bool RequestFocus(FocusNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.Parent == null && !ReferenceEquals(node, _rootScope))
        {
            _rootScope.Reparent(node);
        }

        node.DoRequestFocus(findFirstFocus: true);
        return node.HasFocus;
    }

    /// <summary>C#-only convenience wrapper around <see cref="FocusNode.Unfocus"/>.</summary>
    public void Unfocus(FocusNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Unfocus();
    }

    public bool FocusNext() => TraversalOrigin().NextFocus();

    public bool FocusPrevious() => TraversalOrigin().PreviousFocus();

    public bool FocusInDirection(TraversalDirection direction) =>
        TraversalOrigin().FocusInDirection(direction);

    private FocusNode TraversalOrigin() => PrimaryFocus ?? _rootScope;

    /// <summary>Dart parity source: <c>FocusManager._markDetached</c>.</summary>
    internal void MarkDetached(FocusNode node)
    {
        if (ReferenceEquals(PrimaryFocus, node))
        {
            PrimaryFocus = null;
        }

        if (ReferenceEquals(MarkedForFocus, node))
        {
            MarkedForFocus = null;
        }

        if (ReferenceEquals(_suspendedNode, node))
        {
            _suspendedNode = null;
        }

        _dirtyNodes.Remove(node);
    }

    /// <summary>Dart parity source: <c>FocusManager._markPropertiesChanged</c>.</summary>
    internal void MarkPropertiesChanged(FocusNode node)
    {
        _dirtyNodes.Add(node);
        MarkNeedsUpdate();
    }

    /// <summary>Dart parity source: <c>FocusManager._markNextFocus</c>.</summary>
    internal void MarkNextFocus(FocusNode node)
    {
        if (ReferenceEquals(PrimaryFocus, node))
        {
            MarkedForFocus = null;
        }
        else
        {
            MarkedForFocus = node;
            MarkNeedsUpdate();
        }
    }

    internal void AddPendingAutofocus(PendingAutofocus autofocus) => _pendingAutofocuses.Add(autofocus);

    /// <summary>
    /// Dart schedules <c>applyFocusChangesIfNeeded</c> on a microtask; Plumix applies focus changes
    /// synchronously (see <c>docs/ai/DIVERGENCES.md</c>), so this runs the pass straight away and
    /// guards against re-entering it from a listener.
    /// </summary>
    internal void MarkNeedsUpdate()
    {
        if (_applyingFocusChanges)
        {
            return;
        }

        ApplyFocusChangesIfNeeded();
    }

    /// <summary>Dart parity source: <c>FocusManager.applyFocusChangesIfNeeded</c>.</summary>
    public void ApplyFocusChangesIfNeeded()
    {
        if (_applyingFocusChanges)
        {
            return;
        }

        _applyingFocusChanges = true;
        try
        {
            FocusNode? previousFocus = PrimaryFocus;

            foreach (PendingAutofocus autofocus in _pendingAutofocuses.ToArray())
            {
                autofocus.ApplyIfValid(this);
            }

            _pendingAutofocuses.Clear();

            if (PrimaryFocus == null && MarkedForFocus == null)
            {
                MarkedForFocus = _rootScope;
            }

            if (MarkedForFocus != null && !ReferenceEquals(MarkedForFocus, PrimaryFocus))
            {
                HashSet<FocusNode> previousPath = previousFocus?.Ancestors.ToHashSet() ?? [];
                HashSet<FocusNode> nextPath = MarkedForFocus.Ancestors.ToHashSet();
                _dirtyNodes.UnionWith(nextPath.Except(previousPath));
                _dirtyNodes.UnionWith(previousPath.Except(nextPath));

                PrimaryFocus = MarkedForFocus;
                MarkedForFocus = null;
            }

            if (!ReferenceEquals(previousFocus, PrimaryFocus))
            {
                if (previousFocus != null)
                {
                    _dirtyNodes.Add(previousFocus);
                }

                if (PrimaryFocus != null)
                {
                    _dirtyNodes.Add(PrimaryFocus);
                }
            }

            foreach (FocusNode node in _dirtyNodes.ToArray())
            {
                node.Notify();
            }

            _dirtyNodes.Clear();
            if (!ReferenceEquals(previousFocus, PrimaryFocus))
            {
                PrimaryFocusChanged?.Invoke();
                NotifyListeners();
            }
        }
        finally
        {
            _applyingFocusChanges = false;
        }
    }

    /// <summary>
    /// Records the event in <see cref="HardwareKeyboard"/> and then routes it through the focus
    /// tree. The host goes through <see cref="KeyEventManager"/> instead, which records the event
    /// itself and then calls <see cref="RouteKeyEvent"/>.
    /// </summary>
    public bool HandleKeyEvent(KeyEvent @event)
    {
        if (HardwareKeyboard.Instance.HandleKeyEvent(@event))
        {
            UpdateHighlightModeForKeyEvent();
            return true;
        }

        return RouteKeyEvent(@event);
    }

    /// <summary>Dart parity source: <c>_HighlightModeManager.handleKeyMessage</c>.</summary>
    internal bool RouteKeyEvent(KeyEvent @event)
    {
        UpdateHighlightModeForKeyEvent();

        if (_earlyKeyEventHandlers.Count > 0)
        {
            KeyEventResult early = KeyEventResults.Combine(
                _earlyKeyEventHandlers.ToArray().Select(callback => callback(@event)).ToList());
            if (early == KeyEventResult.Handled)
            {
                return true;
            }

            if (early == KeyEventResult.SkipRemainingHandlers)
            {
                return false;
            }
        }

        bool handled = false;
        if (PrimaryFocus != null)
        {
            foreach (FocusNode node in Enumerable.Repeat(PrimaryFocus, 1).Concat(PrimaryFocus.Ancestors))
            {
                KeyEventResult result = node.HandleKeyEvent(@event);
                if (result == KeyEventResult.Ignored)
                {
                    continue;
                }

                handled = result == KeyEventResult.Handled;
                break;
            }
        }

        if (!handled && _lateKeyEventHandlers.Count > 0)
        {
            KeyEventResult late = KeyEventResults.Combine(
                _lateKeyEventHandlers.ToArray().Select(callback => callback(@event)).ToList());
            if (late == KeyEventResult.Handled)
            {
                return true;
            }

            if (late == KeyEventResult.SkipRemainingHandlers)
            {
                return false;
            }
        }

        return handled || HandleDefaultTraversalKey(@event);
    }

    /// <summary>
    /// C#-only: Flutter routes Tab and the arrow keys through <c>WidgetsApp</c>'s default shortcuts.
    /// Plumix's hosts deliver key events straight to the manager, so the traversal fallback lives here.
    /// </summary>
    private bool HandleDefaultTraversalKey(KeyEvent @event)
    {
        if (@event is not KeyDownEvent)
        {
            return false;
        }

        if (@event.LogicalKey.Equals(LogicalKeyboardKey.Tab))
        {
            return HardwareKeyboard.Instance.IsShiftPressed ? FocusPrevious() : FocusNext();
        }

        if (@event.LogicalKey.Equals(LogicalKeyboardKey.ArrowRight))
        {
            return FocusInDirection(TraversalDirection.Right);
        }

        if (@event.LogicalKey.Equals(LogicalKeyboardKey.ArrowDown))
        {
            return FocusInDirection(TraversalDirection.Down);
        }

        if (@event.LogicalKey.Equals(LogicalKeyboardKey.ArrowLeft))
        {
            return FocusInDirection(TraversalDirection.Left);
        }

        if (@event.LogicalKey.Equals(LogicalKeyboardKey.ArrowUp))
        {
            return FocusInDirection(TraversalDirection.Up);
        }

        return false;
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
        return PrimaryFocus?.HandleTextComposition(text ?? string.Empty, isCommit: false) ?? false;
    }

    public bool HandleTextCompositionCommit(string text)
    {
        return PrimaryFocus?.HandleTextComposition(text ?? string.Empty, isCommit: true) ?? false;
    }

    public FocusTextInputState? ResolveTextInputState()
    {
        return PrimaryFocus?.ResolveTextInputState()?.Normalize();
    }

    public bool HandleTextSelectionChanged(int baseOffset, int extentOffset)
    {
        return PrimaryFocus?.HandleTextSelectionChanged(baseOffset, extentOffset) ?? false;
    }

    internal void ResetForTests()
    {
        PrimaryFocus = null;
        MarkedForFocus = null;
        _suspendedNode = null;
        _dirtyNodes.Clear();
        _pendingAutofocuses.Clear();
        foreach (FocusNode child in _rootScope.Children.ToArray())
        {
            _rootScope.RemoveChild(child);
        }

        _rootScope = new FocusScopeNode(debugLabel: "Root Focus Scope");
        _rootScope.UpdateManager(this);
        _highlightModeListeners.Clear();
        _earlyKeyEventHandlers.Clear();
        _lateKeyEventHandlers.Clear();
        _highlightMode = null;
        _highlightStrategy = FocusHighlightStrategy.Automatic;
        _lastInteractionRequiresTraditionalHighlights = null;
        HardwareKeyboard.Instance.ClearState();
#pragma warning disable CS0618
        RawKeyboard.Instance.ClearKeysPressed();
        RawKeyboard.Instance.ClearListeners();
#pragma warning restore CS0618
        KeyEventManager.Instance.ClearState();
        RegisterKeyMessageHandler();
    }

    /// <summary>
    /// Dart wires `ServicesBinding` to route assembled key messages into the focus tree; Plumix's
    /// focus manager registers itself with <see cref="KeyEventManager"/> for the same effect.
    /// </summary>
    private void RegisterKeyMessageHandler()
    {
#pragma warning disable CS0618
        KeyEventManager.Instance.KeyMessageHandler = message =>
        {
            bool handled = false;
            foreach (KeyEvent keyEvent in message.Events)
            {
                handled |= RouteKeyEvent(keyEvent);
            }

            return handled;
        };
#pragma warning restore CS0618
    }

    /// <summary>Dart parity source: <c>_HighlightModeManager.handlePointerEvent</c>.</summary>
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

    /// <summary>Dart parity source: <c>_HighlightModeManager.updateMode</c>.</summary>
    private void UpdateHighlightMode()
    {
        FocusHighlightMode newMode;
        switch (_highlightStrategy)
        {
            case FocusHighlightStrategy.Automatic:
                if (_lastInteractionRequiresTraditionalHighlights == null)
                {
                    return;
                }

                newMode = _lastInteractionRequiresTraditionalHighlights.Value
                    ? FocusHighlightMode.Touch
                    : FocusHighlightMode.Traditional;
                break;
            case FocusHighlightStrategy.AlwaysTouch:
                newMode = FocusHighlightMode.Touch;
                break;
            case FocusHighlightStrategy.AlwaysTraditional:
            default:
                newMode = FocusHighlightMode.Traditional;
                break;
        }

        FocusHighlightMode oldMode = HighlightMode;
        _highlightMode = newMode;
        if (HighlightMode == oldMode)
        {
            return;
        }

        foreach (Action<FocusHighlightMode> listener in _highlightModeListeners.ToArray())
        {
            listener(HighlightMode);
        }
    }

    private static FocusHighlightMode DefaultModeForPlatform =>
        OperatingSystem.IsAndroid() || OperatingSystem.IsIOS()
            ? FocusHighlightMode.Touch
            : FocusHighlightMode.Traditional;
}
