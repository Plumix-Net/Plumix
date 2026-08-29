using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/focus_scope.dart

namespace Plumix.Widgets;

/// <summary>Dart parity source: <c>_FocusInheritedScope</c>.</summary>
internal sealed class FocusInheritedScope : InheritedNotifier<FocusNode>
{
    public FocusInheritedScope(FocusNode node, Widget child) : base(node, child)
    {
    }
}

/// <summary>Dart parity source: <c>Focus</c>.</summary>
public class Focus : StatefulWidget
{
    public Focus(
        Widget child,
        FocusNode? focusNode = null,
        FocusNode? parentNode = null,
        bool autofocus = false,
        bool? canRequestFocus = null,
        bool? skipTraversal = null,
        bool? descendantsAreFocusable = null,
        bool? descendantsAreTraversable = null,
        Action<bool>? onFocusChange = null,
        FocusOnKeyEventCallback? onKeyEvent = null,
        FocusOnTextInputCallback? onTextInput = null,
        FocusOnTextCompositionCallback? onTextComposition = null,
        FocusOnTextInputStateCallback? onTextInputState = null,
        FocusOnTextSelectionChangedCallback? onTextSelectionChanged = null,
        string? debugLabel = null,
        Key? key = null) : this(
            child: child,
            includeSemantics: false,
            focusNode: focusNode,
            parentNode: parentNode,
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
            debugLabel: debugLabel,
            key: key)
    {
    }

    public Focus(
        Widget child,
        bool includeSemantics,
        FocusNode? focusNode = null,
        FocusNode? parentNode = null,
        bool autofocus = false,
        bool? canRequestFocus = null,
        bool? skipTraversal = null,
        bool? descendantsAreFocusable = null,
        bool? descendantsAreTraversable = null,
        Action<bool>? onFocusChange = null,
        FocusOnKeyEventCallback? onKeyEvent = null,
        FocusOnTextInputCallback? onTextInput = null,
        FocusOnTextCompositionCallback? onTextComposition = null,
        FocusOnTextInputStateCallback? onTextInputState = null,
        FocusOnTextSelectionChangedCallback? onTextSelectionChanged = null,
        string? debugLabel = null,
        Key? key = null) : base(key)
    {
        Child = child;
        FocusNode = focusNode;
        ParentNode = parentNode;
        Autofocus = autofocus;
        RawCanRequestFocus = canRequestFocus;
        RawSkipTraversal = skipTraversal;
        RawDescendantsAreFocusable = descendantsAreFocusable;
        RawDescendantsAreTraversable = descendantsAreTraversable;
        OnFocusChange = onFocusChange;
        IncludeSemantics = includeSemantics;
        RawOnKeyEvent = onKeyEvent;
        OnTextInput = onTextInput;
        OnTextComposition = onTextComposition;
        OnTextInputState = onTextInputState;
        OnTextSelectionChanged = onTextSelectionChanged;
        RawDebugLabel = debugLabel;
    }

    public Widget Child { get; }

    public FocusNode? FocusNode { get; }

    /// <summary>Dart parity source: <c>Focus.parentNode</c>.</summary>
    public FocusNode? ParentNode { get; }

    public bool Autofocus { get; }

    public bool IncludeSemantics { get; }

    public Action<bool>? OnFocusChange { get; }

    public FocusOnTextInputCallback? OnTextInput { get; }

    public FocusOnTextCompositionCallback? OnTextComposition { get; }

    public FocusOnTextInputStateCallback? OnTextInputState { get; }

    public FocusOnTextSelectionChangedCallback? OnTextSelectionChanged { get; }

    /// <summary>Whether the node, rather than this widget, owns the focus configuration.</summary>
    internal virtual bool UsingExternalFocus => false;

    private protected bool? RawCanRequestFocus { get; }

    private protected bool? RawSkipTraversal { get; }

    private protected bool? RawDescendantsAreFocusable { get; }

    private protected virtual bool? RawDescendantsAreTraversable { get; }

    private protected FocusOnKeyEventCallback? RawOnKeyEvent { get; }

    private protected string? RawDebugLabel { get; }

    public virtual FocusOnKeyEventCallback? OnKeyEvent => RawOnKeyEvent ?? FocusNode?.OnKeyEvent;

    public virtual bool CanRequestFocus => RawCanRequestFocus ?? FocusNode?.CanRequestFocus ?? true;

    public virtual bool SkipTraversal => RawSkipTraversal ?? FocusNode?.SkipTraversal ?? false;

    public virtual bool DescendantsAreFocusable =>
        RawDescendantsAreFocusable ?? FocusNode?.DescendantsAreFocusable ?? true;

    public virtual bool DescendantsAreTraversable =>
        RawDescendantsAreTraversable ?? FocusNode?.DescendantsAreTraversable ?? true;

    public virtual string? DebugLabel => RawDebugLabel ?? FocusNode?.DebugLabel;

    /// <summary>Dart parity source: <c>Focus.withExternalFocusNode</c>.</summary>
    public static Focus WithExternalFocusNode(
        FocusNode focusNode,
        Widget child,
        FocusNode? parentNode = null,
        bool autofocus = false,
        Action<bool>? onFocusChange = null,
        bool includeSemantics = false,
        Key? key = null)
    {
        ArgumentNullException.ThrowIfNull(focusNode);
        return new FocusWithExternalFocusNode(
            child: child,
            focusNode: focusNode,
            parentNode: parentNode,
            autofocus: autofocus,
            onFocusChange: onFocusChange,
            includeSemantics: includeSemantics,
            key: key);
    }

    /// <summary>Dart parity source: <c>Focus.of</c>.</summary>
    public static FocusNode Of(BuildContext context, bool scopeOk = false, bool createDependency = true)
    {
        return MaybeOf(context, scopeOk, createDependency)
               ?? throw new InvalidOperationException(
                   "Focus.Of() was called with a context that does not contain a Focus widget.");
    }

    /// <summary>Dart parity source: <c>Focus.maybeOf</c>.</summary>
    public static FocusNode? MaybeOf(BuildContext context, bool scopeOk = false, bool createDependency = true)
    {
        FocusInheritedScope? scope = createDependency
            ? context.DependOnInherited<FocusInheritedScope>()
            : context.GetInherited<FocusInheritedScope>();
        return scope?.Notifier switch
        {
            null => null,
            FocusScopeNode when !scopeOk => null,
            { } node => node,
        };
    }

    /// <summary>Dart parity source: <c>Focus.isAt</c>.</summary>
    public static bool IsAt(BuildContext context) => MaybeOf(context)?.HasFocus ?? false;

    public override State CreateState() => new FocusState();
}

/// <summary>Dart parity source: <c>_FocusWithExternalFocusNode</c>.</summary>
internal sealed class FocusWithExternalFocusNode : Focus
{
    public FocusWithExternalFocusNode(
        Widget child,
        FocusNode focusNode,
        FocusNode? parentNode = null,
        bool autofocus = false,
        Action<bool>? onFocusChange = null,
        bool includeSemantics = false,
        Key? key = null) : base(
            child: child,
            includeSemantics: includeSemantics,
            focusNode: focusNode,
            parentNode: parentNode,
            autofocus: autofocus,
            onFocusChange: onFocusChange,
            key: key)
    {
    }

    internal override bool UsingExternalFocus => true;

    public override FocusOnKeyEventCallback? OnKeyEvent => FocusNode!.OnKeyEvent;

    public override bool CanRequestFocus => FocusNode!.CanRequestFocus;

    public override bool SkipTraversal => FocusNode!.SkipTraversal;

    public override bool DescendantsAreFocusable => FocusNode!.DescendantsAreFocusable;

    private protected override bool? RawDescendantsAreTraversable => FocusNode!.DescendantsAreTraversable;

    public override string? DebugLabel => FocusNode!.DebugLabel;
}

/// <summary>Dart parity source: <c>_FocusState</c>.</summary>
internal class FocusState : State
{
    private FocusNode? _internalNode;
    private bool _hadPrimaryFocus;
    private bool _couldRequestFocus;
    private bool _descendantsWereFocusable;
    private bool _descendantsWereTraversable;
    private bool _didAutofocus;

    private protected FocusAttachment? FocusAttachment;

    private protected Focus Widget => (Focus)Element.Widget;

    private protected FocusNode FocusNode => Widget.FocusNode ?? (_internalNode ??= CreateNode());

    /// <summary>
    /// The identity of the last pointer-down press a <see cref="Widgets.Focus"/> claimed. Click-to-focus
    /// is a C#-only host adaptation (Flutter leaves it to the embedder), and hit-test dispatch runs
    /// deepest-first, so without this guard a shallower ancestor would steal the focus its descendant
    /// just took for the same press.
    /// </summary>
    private static PointerEvent? _lastClaimedPointerDown;

    public override void InitState()
    {
        InitNode();
    }

    private protected virtual FocusNode CreateNode()
    {
        return new FocusNode(
            debugLabel: Widget.DebugLabel,
            canRequestFocus: Widget.CanRequestFocus,
            descendantsAreFocusable: Widget.DescendantsAreFocusable,
            descendantsAreTraversable: Widget.DescendantsAreTraversable,
            skipTraversal: Widget.SkipTraversal);
    }

    private void InitNode()
    {
        if (!Widget.UsingExternalFocus)
        {
            FocusNode.DescendantsAreFocusable = Widget.DescendantsAreFocusable;
            FocusNode.DescendantsAreTraversable = Widget.DescendantsAreTraversable;
            FocusNode.SkipTraversal = Widget.SkipTraversal;
            FocusNode.CanRequestFocus = Widget.CanRequestFocus;
        }

        _couldRequestFocus = FocusNode.CanRequestFocus;
        _descendantsWereFocusable = FocusNode.DescendantsAreFocusable;
        _descendantsWereTraversable = FocusNode.DescendantsAreTraversable;
        _hadPrimaryFocus = FocusNode.HasPrimaryFocus;
        FocusAttachment = FocusNode.Attach(Context, onKeyEvent: Widget.OnKeyEvent);
        ApplyTextInputCallbacks();
        FocusNode.AddListener(HandleFocusChanged);
    }

    public override void Dispose()
    {
        FocusNode.RemoveListener(HandleFocusChanged);
        FocusAttachment!.Detach();
        _internalNode?.Dispose();
    }

    public override void DidChangeDependencies()
    {
        FocusAttachment?.Reparent();
        HandleAutofocus();
    }

    private void HandleAutofocus()
    {
        if (!_didAutofocus && Widget.Autofocus)
        {
            FocusScope.Of(Context).Autofocus(FocusNode);
            _didAutofocus = true;
        }
    }

    public override void Deactivate()
    {
        FocusAttachment?.Reparent();
        _didAutofocus = false;
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var oldFocusWidget = (Focus)oldWidget;
        if (ReferenceEquals(oldFocusWidget.FocusNode, Widget.FocusNode))
        {
            if (!Widget.UsingExternalFocus)
            {
                if (Widget.DebugLabel != FocusNode.DebugLabel)
                {
                    FocusNode.DebugLabel = Widget.DebugLabel;
                }

                if (Widget.OnKeyEvent != FocusNode.OnKeyEvent)
                {
                    FocusNode.OnKeyEvent = Widget.OnKeyEvent;
                }

                FocusNode.SkipTraversal = Widget.SkipTraversal;
                FocusNode.CanRequestFocus = Widget.CanRequestFocus;
                FocusNode.DescendantsAreFocusable = Widget.DescendantsAreFocusable;
                FocusNode.DescendantsAreTraversable = Widget.DescendantsAreTraversable;
                ApplyTextInputCallbacks();
            }
        }
        else
        {
            FocusAttachment!.Detach();
            oldFocusWidget.FocusNode?.RemoveListener(HandleFocusChanged);
            InitNode();
        }

        if (oldFocusWidget.Autofocus != Widget.Autofocus)
        {
            HandleAutofocus();
        }
    }

    /// <summary>C#-only: the IME callbacks Plumix routes through the focus tree.</summary>
    private void ApplyTextInputCallbacks()
    {
        FocusNode.OnTextInput = Widget.OnTextInput;
        FocusNode.OnTextComposition = Widget.OnTextComposition;
        FocusNode.OnTextInputState = Widget.OnTextInputState;
        FocusNode.OnTextSelectionChanged = Widget.OnTextSelectionChanged;
    }

    private void HandleFocusChanged()
    {
        bool hasPrimaryFocus = FocusNode.HasPrimaryFocus;
        bool canRequestFocus = FocusNode.CanRequestFocus;
        bool descendantsAreFocusable = FocusNode.DescendantsAreFocusable;
        bool descendantsAreTraversable = FocusNode.DescendantsAreTraversable;
        Widget.OnFocusChange?.Invoke(FocusNode.HasFocus);
        if (_hadPrimaryFocus != hasPrimaryFocus)
        {
            SetState(() => _hadPrimaryFocus = hasPrimaryFocus);
        }

        if (_couldRequestFocus != canRequestFocus)
        {
            SetState(() => _couldRequestFocus = canRequestFocus);
        }

        if (_descendantsWereFocusable != descendantsAreFocusable)
        {
            SetState(() => _descendantsWereFocusable = descendantsAreFocusable);
        }

        if (_descendantsWereTraversable != descendantsAreTraversable)
        {
            SetState(() => _descendantsWereTraversable = descendantsAreTraversable);
        }
    }

    public override Widget Build(BuildContext context)
    {
        FocusAttachment!.Reparent(parent: Widget.ParentNode);
        Widget child = new Listener(
            child: Widget.Child,
            behavior: HitTestBehavior.Translucent,
            onPointerDown: HandlePointerDown);
        if (Widget.IncludeSemantics)
        {
            SemanticsFlags flags = _couldRequestFocus ? SemanticsFlags.IsFocusable : SemanticsFlags.None;
            if (_couldRequestFocus && _hadPrimaryFocus)
            {
                flags |= SemanticsFlags.IsFocused;
            }

            child = new Semantics(child: child, flags: flags)
            {
                OnFocus = _couldRequestFocus ? RequestSemanticFocus : null,
            };
        }

        return new FocusInheritedScope(FocusNode, child);
    }

    private void RequestSemanticFocus() => FocusNode.RequestFocus();

    private void HandlePointerDown(PointerDownEvent @event)
    {
        if (!FocusNode.CanRequestFocus)
        {
            return;
        }

        PointerEvent identity = @event.Original ?? @event;
        if (ReferenceEquals(_lastClaimedPointerDown, identity))
        {
            return;
        }

        _lastClaimedPointerDown = identity;
        FocusNode.RequestFocus();
    }
}

/// <summary>Dart parity source: <c>FocusScope</c>.</summary>
public class FocusScope : Focus
{
    public FocusScope(
        Widget child,
        FocusScopeNode? focusScopeNode = null,
        FocusNode? parentNode = null,
        bool autofocus = false,
        bool? canRequestFocus = null,
        bool? skipTraversal = null,
        bool? descendantsAreFocusable = null,
        bool? descendantsAreTraversable = null,
        Action<bool>? onFocusChange = null,
        FocusOnKeyEventCallback? onKeyEvent = null,
        bool includeSemantics = false,
        string? debugLabel = null,
        Key? key = null) : base(
            child: child,
            includeSemantics: includeSemantics,
            focusNode: focusScopeNode,
            parentNode: parentNode,
            autofocus: autofocus,
            canRequestFocus: canRequestFocus,
            skipTraversal: skipTraversal,
            descendantsAreFocusable: descendantsAreFocusable,
            descendantsAreTraversable: descendantsAreTraversable,
            onFocusChange: onFocusChange,
            onKeyEvent: onKeyEvent,
            debugLabel: debugLabel,
            key: key)
    {
    }

    /// <summary>The scope node this widget was given, if any.</summary>
    public FocusScopeNode? FocusScopeNode => (FocusScopeNode?)FocusNode;

    /// <summary>Dart parity source: <c>FocusScope.withExternalFocusNode</c>.</summary>
    public static FocusScope WithExternalFocusNode(
        FocusScopeNode focusScopeNode,
        Widget child,
        FocusNode? parentNode = null,
        bool autofocus = false,
        bool includeSemantics = false,
        Action<bool>? onFocusChange = null,
        Key? key = null)
    {
        ArgumentNullException.ThrowIfNull(focusScopeNode);
        return new FocusScopeWithExternalFocusNode(
            child: child,
            focusScopeNode: focusScopeNode,
            parentNode: parentNode,
            autofocus: autofocus,
            includeSemantics: includeSemantics,
            onFocusChange: onFocusChange,
            key: key);
    }

    /// <summary>Dart parity source: <c>FocusScope.of</c>.</summary>
    public static FocusScopeNode Of(BuildContext context, bool createDependency = true)
    {
        return Focus.MaybeOf(context, scopeOk: true, createDependency: createDependency)?.NearestScope
               ?? FocusManager.Instance.RootScope;
    }

    /// <summary>The enclosing scope node, or <c>null</c> when there is no <see cref="Focus"/> ancestor.</summary>
    public static FocusScopeNode? MaybeOf(BuildContext context)
    {
        return Focus.MaybeOf(context, scopeOk: true)?.NearestScope;
    }

    public override State CreateState() => new FocusScopeState();
}

/// <summary>Dart parity source: <c>_FocusScopeWithExternalFocusNode</c>.</summary>
internal sealed class FocusScopeWithExternalFocusNode : FocusScope
{
    public FocusScopeWithExternalFocusNode(
        Widget child,
        FocusScopeNode focusScopeNode,
        FocusNode? parentNode = null,
        bool autofocus = false,
        bool includeSemantics = false,
        Action<bool>? onFocusChange = null,
        Key? key = null) : base(
            child: child,
            focusScopeNode: focusScopeNode,
            parentNode: parentNode,
            autofocus: autofocus,
            includeSemantics: includeSemantics,
            onFocusChange: onFocusChange,
            key: key)
    {
    }

    internal override bool UsingExternalFocus => true;

    public override FocusOnKeyEventCallback? OnKeyEvent => FocusNode!.OnKeyEvent;

    public override bool CanRequestFocus => FocusNode!.CanRequestFocus;

    public override bool SkipTraversal => FocusNode!.SkipTraversal;

    public override bool DescendantsAreFocusable => FocusNode!.DescendantsAreFocusable;

    private protected override bool? RawDescendantsAreTraversable => FocusNode!.DescendantsAreTraversable;

    public override string? DebugLabel => FocusNode!.DebugLabel;
}

/// <summary>Dart parity source: <c>_FocusScopeState</c>.</summary>
internal sealed class FocusScopeState : FocusState
{
    private protected override FocusNode CreateNode()
    {
        return new FocusScopeNode(
            debugLabel: Widget.DebugLabel,
            canRequestFocus: Widget.CanRequestFocus,
            skipTraversal: Widget.SkipTraversal);
    }

    public override Widget Build(BuildContext context)
    {
        FocusAttachment!.Reparent(parent: Widget.ParentNode);
        Widget result = new FocusInheritedScope(FocusNode, Widget.Child);
        if (Widget.IncludeSemantics)
        {
            result = new Semantics(child: result, explicitChildNodes: true);
        }

        return result;
    }
}

/// <summary>Dart parity source: <c>ExcludeFocus</c>.</summary>
public sealed class ExcludeFocus : StatelessWidget
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
        return new Focus(
            child: Child,
            canRequestFocus: false,
            skipTraversal: true,
            includeSemantics: false,
            descendantsAreFocusable: !Excluding);
    }
}
