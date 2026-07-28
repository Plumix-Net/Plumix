using Plumix.Foundation;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/actions.dart
//
// C# already reserves Action/Action<T> for delegates. Flutter's Action<TIntent> is therefore named
// FlutterAction<TIntent>; the remaining API and lookup/invocation behavior follow the Dart source.

public abstract class Intent
{
    public static DoNothingIntent DoNothing { get; } = new();
}

public abstract class FlutterAction
{
    private readonly List<System.Action<FlutterAction>> _listeners = [];

    public abstract Type IntentType { get; }

    internal abstract bool IsEnabledObject(Intent intent, BuildContext? context);

    internal abstract bool ConsumesKeyObject(Intent intent);

    internal abstract object? InvokeObject(Intent intent, BuildContext? context);

    public void AddActionListener(System.Action<FlutterAction> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        if (!_listeners.Contains(listener))
        {
            _listeners.Add(listener);
        }
    }

    public void RemoveActionListener(System.Action<FlutterAction> listener)
    {
        _listeners.Remove(listener);
    }

    protected void NotifyActionListeners()
    {
        foreach (System.Action<FlutterAction> listener in _listeners.ToArray())
        {
            listener(this);
        }
    }

    internal abstract KeyEventResult ToKeyEventResultObject(Intent intent, object? invokeResult);
}

public abstract class FlutterAction<TIntent> : FlutterAction where TIntent : Intent
{
    public override Type IntentType => typeof(TIntent);

    public virtual bool IsActionEnabled => true;

    public virtual bool IsEnabled(TIntent intent) => IsActionEnabled;

    public virtual bool ConsumesKey(TIntent intent) => true;

    public virtual KeyEventResult ToKeyEventResult(TIntent intent, object? invokeResult)
    {
        return ConsumesKey(intent)
            ? KeyEventResult.Handled
            : KeyEventResult.SkipRemainingHandlers;
    }

    public abstract object? Invoke(TIntent intent);

    internal override bool IsEnabledObject(Intent intent, BuildContext? context)
    {
        return intent is TIntent typedIntent && IsEnabledWithContext(typedIntent, context);
    }

    internal override bool ConsumesKeyObject(Intent intent)
    {
        return intent is TIntent typedIntent && ConsumesKey(typedIntent);
    }

    internal override object? InvokeObject(Intent intent, BuildContext? context)
    {
        if (intent is not TIntent typedIntent)
        {
            throw new ArgumentException(
                $"Action for {typeof(TIntent).Name} cannot invoke {intent.GetType().Name}.",
                nameof(intent));
        }

        return InvokeWithContext(typedIntent, context);
    }

    internal override KeyEventResult ToKeyEventResultObject(Intent intent, object? invokeResult)
    {
        if (intent is not TIntent typedIntent)
        {
            throw new ArgumentException(
                $"Action for {typeof(TIntent).Name} cannot handle {intent.GetType().Name}.",
                nameof(intent));
        }

        return ToKeyEventResult(typedIntent, invokeResult);
    }

    protected internal virtual bool IsEnabledWithContext(TIntent intent, BuildContext? context)
    {
        return IsEnabled(intent);
    }

    protected internal virtual object? InvokeWithContext(TIntent intent, BuildContext? context)
    {
        return Invoke(intent);
    }
}

public abstract class ContextAction<TIntent> : FlutterAction<TIntent> where TIntent : Intent
{
    public virtual bool IsEnabled(TIntent intent, BuildContext? context)
    {
        return base.IsEnabled(intent);
    }

    public abstract object? Invoke(TIntent intent, BuildContext? context);

    public sealed override object? Invoke(TIntent intent)
    {
        return Invoke(intent, context: null);
    }

    protected internal sealed override bool IsEnabledWithContext(TIntent intent, BuildContext? context)
    {
        return IsEnabled(intent, context);
    }

    protected internal sealed override object? InvokeWithContext(TIntent intent, BuildContext? context)
    {
        return Invoke(intent, context);
    }
}

public sealed class CallbackAction<TIntent> : FlutterAction<TIntent> where TIntent : Intent
{
    public CallbackAction(Func<TIntent, object?> onInvoke)
    {
        OnInvoke = onInvoke ?? throw new ArgumentNullException(nameof(onInvoke));
    }

    public Func<TIntent, object?> OnInvoke { get; }

    public override object? Invoke(TIntent intent) => OnInvoke(intent);
}

public class ActionDispatcher
{
    public virtual object? InvokeAction(FlutterAction action, Intent intent, BuildContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(intent);
        BuildContext? target = ResolveContext(context);
        if (!action.IsEnabledObject(intent, target))
        {
            throw new InvalidOperationException("Action must be enabled when InvokeAction is called.");
        }

        return action.InvokeObject(intent, target);
    }

    public virtual (bool Enabled, object? Result) InvokeActionIfEnabled(
        FlutterAction action,
        Intent intent,
        BuildContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(intent);
        BuildContext? target = ResolveContext(context);
        return action.IsEnabledObject(intent, target)
            ? (true, action.InvokeObject(intent, target))
            : (false, null);
    }

    private static BuildContext? ResolveContext(BuildContext? context)
    {
        Element? focusedElement = FocusManager.Instance.PrimaryFocus?.AttachmentElement;
        return context ?? (focusedElement == null ? null : new BuildContext(focusedElement));
    }
}

public sealed class ActionListener : StatefulWidget
{
    public ActionListener(
        FlutterAction action,
        System.Action<FlutterAction> listener,
        Widget child,
        Key? key = null) : base(key)
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
        Listener = listener ?? throw new ArgumentNullException(nameof(listener));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public FlutterAction Action { get; }

    public System.Action<FlutterAction> Listener { get; }

    public Widget Child { get; }

    public override State CreateState() => new ActionListenerState();

    private sealed class ActionListenerState : State
    {
        private ActionListener Current => (ActionListener)StateWidget;

        public override void InitState()
        {
            Current.Action.AddActionListener(HandleActionChanged);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldListener = (ActionListener)oldWidget;
            if (!ReferenceEquals(oldListener.Action, Current.Action))
            {
                oldListener.Action.RemoveActionListener(HandleActionChanged);
                Current.Action.AddActionListener(HandleActionChanged);
            }
        }

        public override Widget Build(BuildContext context) => Current.Child;

        public override void Dispose()
        {
            Current.Action.RemoveActionListener(HandleActionChanged);
        }

        private void HandleActionChanged(FlutterAction action)
        {
            Current.Listener(action);
        }
    }
}

public sealed class Actions : StatefulWidget
{
    public Actions(
        IReadOnlyDictionary<Type, FlutterAction> actions,
        Widget child,
        ActionDispatcher? dispatcher = null,
        Key? key = null) : base(key)
    {
        ActionsMap = actions ?? throw new ArgumentNullException(nameof(actions));
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Dispatcher = dispatcher;
    }

    public IReadOnlyDictionary<Type, FlutterAction> ActionsMap { get; }

    public Widget Child { get; }

    public ActionDispatcher? Dispatcher { get; }

    public static System.Action? Handler<TIntent>(BuildContext context, TIntent intent)
        where TIntent : Intent
    {
        FlutterAction<TIntent>? action = MaybeFind(context, intent);
        if (action == null || !action.IsEnabledObject(intent, context))
        {
            return null;
        }

        return () =>
        {
            if (action.IsEnabledObject(intent, context))
            {
                Of(context).InvokeAction(action, intent, context);
            }
        };
    }

    public static FlutterAction<TIntent> Find<TIntent>(BuildContext context, TIntent? intent = null)
        where TIntent : Intent
    {
        return MaybeFind(context, intent)
               ?? throw new InvalidOperationException(
                   $"Unable to find an action for {(intent?.GetType() ?? typeof(TIntent)).Name}.");
    }

    public static FlutterAction<TIntent>? MaybeFind<TIntent>(BuildContext context, TIntent? intent = null)
        where TIntent : Intent
    {
        Type intentType = intent?.GetType() ?? typeof(TIntent);
        foreach ((InheritedElement element, ActionsScope scope) in VisitScopes(context))
        {
            if (!scope.ActionsMap.TryGetValue(intentType, out FlutterAction? action))
            {
                continue;
            }

            if (action is not FlutterAction<TIntent> typedAction)
            {
                throw new InvalidOperationException(
                    $"{intentType.Name} cannot be handled by {action.GetType().Name}.");
            }

            _ = context.Owner.DependOnInheritedElement(element, aspect: null);
            return typedAction;
        }

        return null;
    }

    internal static FlutterAction? MaybeFind(BuildContext context, Intent intent)
    {
        Type intentType = intent.GetType();
        foreach ((InheritedElement _, ActionsScope scope) in VisitScopes(context))
        {
            if (scope.ActionsMap.TryGetValue(intentType, out FlutterAction? action))
            {
                return action;
            }
        }

        return null;
    }

    public static ActionDispatcher Of(BuildContext context)
    {
        bool isNearest = true;
        foreach ((InheritedElement element, ActionsScope scope) in VisitScopes(context))
        {
            if (isNearest)
            {
                _ = context.Owner.DependOnInheritedElement(element, aspect: null);
                isNearest = false;
            }

            if (scope.Dispatcher == null)
            {
                continue;
            }

            return scope.Dispatcher;
        }

        return new ActionDispatcher();
    }

    public static object? Invoke<TIntent>(BuildContext context, TIntent intent) where TIntent : Intent
    {
        ArgumentNullException.ThrowIfNull(intent);
        Type intentType = intent.GetType();
        foreach ((InheritedElement _, ActionsScope scope) in VisitScopes(context))
        {
            if (!scope.ActionsMap.TryGetValue(intentType, out FlutterAction? action))
            {
                continue;
            }

            if (!action.IsEnabledObject(intent, context))
            {
                throw new InvalidOperationException($"The action for {intentType.Name} is disabled.");
            }

            return FindDispatcherFromScope(context, scope).InvokeAction(action, intent, context);
        }

        throw new InvalidOperationException($"Unable to find an action for {intentType.Name}.");
    }

    public static object? MaybeInvoke<TIntent>(BuildContext context, TIntent intent) where TIntent : Intent
    {
        ArgumentNullException.ThrowIfNull(intent);
        Type intentType = intent.GetType();
        foreach ((InheritedElement _, ActionsScope scope) in VisitScopes(context))
        {
            if (!scope.ActionsMap.TryGetValue(intentType, out FlutterAction? action))
            {
                continue;
            }

            return action.IsEnabledObject(intent, context)
                ? FindDispatcherFromScope(context, scope).InvokeAction(action, intent, context)
                : null;
        }

        return null;
    }

    public override State CreateState() => new ActionsState();

    private static IEnumerable<(InheritedElement Element, ActionsScope Scope)> VisitScopes(BuildContext context)
    {
        for (Element? ancestor = context.Owner.Parent; ancestor != null; ancestor = ancestor.Parent)
        {
            if (ancestor is InheritedElement { Widget: ActionsScope scope } inheritedElement)
            {
                yield return (inheritedElement, scope);
            }
        }
    }

    private static ActionDispatcher FindDispatcherFromScope(BuildContext context, ActionsScope matchedScope)
    {
        bool matched = false;
        foreach ((InheritedElement _, ActionsScope scope) in VisitScopes(context))
        {
            if (!matched)
            {
                matched = ReferenceEquals(scope, matchedScope);
            }

            if (matched && scope.Dispatcher != null)
            {
                return scope.Dispatcher;
            }
        }

        return new ActionDispatcher();
    }

    private sealed class ActionsState : State
    {
        private readonly HashSet<FlutterAction> _listenedActions = [];
        private int _version;

        private Actions Current => (Actions)StateWidget;

        public override void InitState()
        {
            UpdateActionListeners();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            UpdateActionListeners();
        }

        public override Widget Build(BuildContext context)
        {
            return new ActionsScope(
                actionsMap: Current.ActionsMap,
                child: Current.Child,
                dispatcher: Current.Dispatcher,
                version: _version);
        }

        public override void Dispose()
        {
            foreach (FlutterAction action in _listenedActions)
            {
                action.RemoveActionListener(HandleActionChanged);
            }

            _listenedActions.Clear();
        }

        private void UpdateActionListeners()
        {
            var nextActions = Current.ActionsMap.Values.ToHashSet();
            foreach (FlutterAction action in _listenedActions.Except(nextActions).ToArray())
            {
                action.RemoveActionListener(HandleActionChanged);
                _listenedActions.Remove(action);
            }

            foreach (FlutterAction action in nextActions.Except(_listenedActions))
            {
                action.AddActionListener(HandleActionChanged);
                _listenedActions.Add(action);
            }
        }

        private void HandleActionChanged(FlutterAction action)
        {
            SetState(() => _version += 1);
        }
    }
}

internal sealed class ActionsScope : InheritedWidget
{
    public ActionsScope(
        IReadOnlyDictionary<Type, FlutterAction> actionsMap,
        Widget child,
        ActionDispatcher? dispatcher,
        int version) : base()
    {
        ActionsMap = actionsMap;
        Child = child;
        Dispatcher = dispatcher;
        Version = version;
    }

    public IReadOnlyDictionary<Type, FlutterAction> ActionsMap { get; }

    public Widget Child { get; }

    public ActionDispatcher? Dispatcher { get; }

    public int Version { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        var oldScope = (ActionsScope)oldWidget;
        return !ReferenceEquals(oldScope.ActionsMap, ActionsMap)
               || !ReferenceEquals(oldScope.Dispatcher, Dispatcher)
               || oldScope.Version != Version;
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/widgets/actions.dart
public sealed class FocusableActionDetector : StatefulWidget
{
    public FocusableActionDetector(
        Widget child,
        bool enabled = true,
        FocusNode? focusNode = null,
        bool autofocus = false,
        bool descendantsAreFocusable = true,
        bool descendantsAreTraversable = true,
        IReadOnlyDictionary<ShortcutActivator, Intent>? shortcuts = null,
        IReadOnlyDictionary<Type, FlutterAction>? actions = null,
        Action<bool>? onShowFocusHighlight = null,
        Action<bool>? onShowHoverHighlight = null,
        Action<bool>? onFocusChange = null,
        MouseCursor? mouseCursor = null,
        bool includeFocusSemantics = true,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Enabled = enabled;
        FocusNode = focusNode;
        Autofocus = autofocus;
        DescendantsAreFocusable = descendantsAreFocusable;
        DescendantsAreTraversable = descendantsAreTraversable;
        Shortcuts = shortcuts;
        Actions = actions;
        OnShowFocusHighlight = onShowFocusHighlight;
        OnShowHoverHighlight = onShowHoverHighlight;
        OnFocusChange = onFocusChange;
        MouseCursor = mouseCursor ?? Plumix.Widgets.MouseCursor.Defer;
        IncludeFocusSemantics = includeFocusSemantics;
    }

    public bool Enabled { get; }

    public FocusNode? FocusNode { get; }

    public bool Autofocus { get; }

    public bool DescendantsAreFocusable { get; }

    public bool DescendantsAreTraversable { get; }

    public IReadOnlyDictionary<Type, FlutterAction>? Actions { get; }

    public IReadOnlyDictionary<ShortcutActivator, Intent>? Shortcuts { get; }

    public Action<bool>? OnShowFocusHighlight { get; }

    public Action<bool>? OnShowHoverHighlight { get; }

    public Action<bool>? OnFocusChange { get; }

    public MouseCursor MouseCursor { get; }

    public bool IncludeFocusSemantics { get; }

    public Widget Child { get; }

    public override State CreateState() => new FocusableActionDetectorState();

    private sealed class FocusableActionDetectorState : State
    {
        private readonly GlobalObjectKey<State> _mouseRegionKey;
        private bool _canShowHighlight;
        private bool _hovering;
        private bool _focused;

        public FocusableActionDetectorState()
        {
            _mouseRegionKey = new GlobalObjectKey<State>(this);
        }

        private FocusableActionDetector Current => (FocusableActionDetector)StateWidget;

        public override void InitState()
        {
            Scheduler.AddPostFrameCallback(_ =>
            {
                if (Mounted)
                {
                    UpdateHighlightMode(FocusManager.Instance.HighlightMode);
                }
            });
            FocusManager.Instance.AddHighlightModeListener(HandleFocusHighlightModeChange);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldDetector = (FocusableActionDetector)oldWidget;
            if (oldDetector.Enabled == Current.Enabled)
            {
                return;
            }

            Scheduler.AddPostFrameCallback(_ =>
            {
                if (Mounted)
                {
                    MayTriggerCallback(oldWidget: oldDetector);
                }
            });
        }

        public override Widget Build(BuildContext context)
        {
            Widget child = new MouseRegion(
                key: _mouseRegionKey,
                onEnter: HandleMouseEnter,
                onExit: HandleMouseExit,
                cursor: Current.MouseCursor,
                child: new Focus(
                    focusNode: Current.FocusNode,
                    autofocus: Current.Autofocus,
                    descendantsAreFocusable: Current.DescendantsAreFocusable,
                    descendantsAreTraversable: Current.DescendantsAreTraversable,
                    canRequestFocus: CanRequestFocus,
                    onFocusChange: HandleFocusChange,
                    includeSemantics: Current.IncludeFocusSemantics,
                    child: Current.Child));

            if (Current.Enabled && Current.Actions is { Count: > 0 })
            {
                child = new Actions(Current.Actions, child);
            }

            if (Current.Enabled && Current.Shortcuts is { Count: > 0 })
            {
                child = new Shortcuts(Current.Shortcuts, child);
            }

            return child;
        }

        public override void Dispose()
        {
            FocusManager.Instance.RemoveHighlightModeListener(HandleFocusHighlightModeChange);
        }

        private bool CanRequestFocus =>
            MediaQuery.MaybeNavigationModeOf(Context) == NavigationMode.Directional || Current.Enabled;

        private void UpdateHighlightMode(FocusHighlightMode mode)
        {
            MayTriggerCallback(() => _canShowHighlight = mode == FocusHighlightMode.Traditional);
        }

        private void HandleFocusHighlightModeChange(FocusHighlightMode mode)
        {
            if (Mounted)
            {
                UpdateHighlightMode(mode);
            }
        }

        private void HandleMouseEnter(PointerEnterEvent @event)
        {
            if (!_hovering)
            {
                MayTriggerCallback(() => _hovering = true);
            }
        }

        private void HandleMouseExit(PointerExitEvent @event)
        {
            if (_hovering)
            {
                MayTriggerCallback(() => _hovering = false);
            }
        }

        private void HandleFocusChange(bool focused)
        {
            if (_focused == focused)
            {
                return;
            }

            MayTriggerCallback(() => _focused = focused);
            Current.OnFocusChange?.Invoke(_focused);
        }

        private void MayTriggerCallback(
            System.Action? task = null,
            FocusableActionDetector? oldWidget = null)
        {
            FocusableActionDetector oldTarget = oldWidget ?? Current;
            bool didShowHoverHighlight = ShouldShowHoverHighlight(oldTarget);
            bool didShowFocusHighlight = ShouldShowFocusHighlight(oldTarget);
            task?.Invoke();
            bool doShowHoverHighlight = ShouldShowHoverHighlight(Current);
            bool doShowFocusHighlight = ShouldShowFocusHighlight(Current);

            if (didShowFocusHighlight != doShowFocusHighlight)
            {
                Current.OnShowFocusHighlight?.Invoke(doShowFocusHighlight);
            }

            if (didShowHoverHighlight != doShowHoverHighlight)
            {
                Current.OnShowHoverHighlight?.Invoke(doShowHoverHighlight);
            }
        }

        private bool ShouldShowHoverHighlight(FocusableActionDetector target)
        {
            return _hovering && target.Enabled && _canShowHighlight;
        }

        private bool ShouldShowFocusHighlight(FocusableActionDetector target)
        {
            bool canRequestFocus = MediaQuery.MaybeNavigationModeOf(Context) == NavigationMode.Directional
                                   || target.Enabled;
            return _focused && _canShowHighlight && canRequestFocus;
        }
    }
}

public sealed class VoidCallbackIntent : Intent
{
    public VoidCallbackIntent(System.Action callback)
    {
        Callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    public System.Action Callback { get; }
}

public sealed class VoidCallbackAction : FlutterAction<VoidCallbackIntent>
{
    public override object? Invoke(VoidCallbackIntent intent)
    {
        intent.Callback();
        return null;
    }
}

public sealed class DoNothingIntent : Intent
{
    public DoNothingIntent()
    {
    }
}

public sealed class DoNothingAndStopPropagationIntent : Intent
{
    public static DoNothingAndStopPropagationIntent Instance { get; } = new();

    public DoNothingAndStopPropagationIntent()
    {
    }
}

public sealed class DoNothingAction : FlutterAction<Intent>
{
    public DoNothingAction(bool consumesKey = true)
    {
        ConsumesKeyValue = consumesKey;
    }

    public bool ConsumesKeyValue { get; }

    public override bool ConsumesKey(Intent intent) => ConsumesKeyValue;

    public override object? Invoke(Intent intent) => null;
}

public sealed class ActivateIntent : Intent
{
}

public abstract class ActivateAction : FlutterAction<ActivateIntent>
{
}

public sealed class ButtonActivateIntent : Intent
{
}

public sealed class SelectIntent : Intent
{
}

public abstract class SelectAction : FlutterAction<SelectIntent>
{
}

public sealed class DismissIntent : Intent
{
}

public abstract class DismissAction : FlutterAction<DismissIntent>
{
}

public sealed class PrioritizedIntents : Intent
{
    public PrioritizedIntents(IReadOnlyList<Intent> orderedIntents)
    {
        OrderedIntents = orderedIntents ?? throw new ArgumentNullException(nameof(orderedIntents));
    }

    public IReadOnlyList<Intent> OrderedIntents { get; }
}

public sealed class PrioritizedAction : ContextAction<PrioritizedIntents>
{
    public override bool IsEnabled(PrioritizedIntents intent, BuildContext? context)
    {
        return context.HasValue && ResolveEnabledAction(intent, context.Value).HasValue;
    }

    public override object? Invoke(PrioritizedIntents intent, BuildContext? context)
    {
        if (!context.HasValue)
        {
            return null;
        }

        (FlutterAction Action, Intent Intent)? resolved = ResolveEnabledAction(intent, context.Value);
        return resolved.HasValue
            ? Actions.Of(context.Value).InvokeAction(resolved.Value.Action, resolved.Value.Intent, context)
            : null;
    }

    private static (FlutterAction Action, Intent Intent)? ResolveEnabledAction(
        PrioritizedIntents prioritized,
        BuildContext context)
    {
        foreach (Intent intent in prioritized.OrderedIntents)
        {
            FlutterAction? action = Actions.MaybeFind(context, intent);
            if (action != null && action.IsEnabledObject(intent, context))
            {
                return (action, intent);
            }
        }

        return null;
    }
}
