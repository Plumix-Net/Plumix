using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart

namespace Plumix.Widgets;

/// <summary>
/// Describes the configuration for an <see cref="Element"/>. Dart's <c>Widget</c>.
/// </summary>
public abstract class Widget(Key? key = null) : DiagnosticableTree
{
    /// <summary>Controls how one widget replaces another widget in the tree.</summary>
    public Key? Key { get; } = key;

    /// <summary>Inflates this configuration to a concrete instance.</summary>
    public abstract Element CreateElement();

    /// A short, textual description of this widget.
    public override string ToStringShort()
    {
        string type = Diagnostics.ObjectRuntimeType(this, "Widget");
        return Key is null ? type : $"{type}-{Key}";
    }

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.DefaultDiagnosticsTreeStyle = DiagnosticsTreeStyle.Dense;
    }

    /// <summary>Dart's <c>Widget.operator ==</c>, which is <c>@nonVirtual</c> identity.</summary>
    public sealed override bool Equals(object? obj) => ReferenceEquals(this, obj);

    /// <summary>Dart's <c>Widget.hashCode</c>, which is <c>@nonVirtual</c>.</summary>
    public sealed override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

    /// <summary>
    /// Whether the <paramref name="newWidget"/> can be used to update an <see cref="Element"/> that
    /// currently has the <paramref name="oldWidget"/> as its configuration.
    /// </summary>
    public static bool CanUpdate(Widget oldWidget, Widget newWidget)
    {
        return oldWidget.GetType() == newWidget.GetType() && Equals(oldWidget.Key, newWidget.Key);
    }

    /// <summary>
    /// Dart's <c>Widget._debugConcreteSubtype</c>: the numeric encoding of the concrete widget kind,
    /// matched against <c>Element._debugConcreteSubtype</c> in <see cref="Element.UpdateChild"/>.
    /// </summary>
    internal static int DebugConcreteSubtype(Widget widget)
    {
        return widget is StatefulWidget
            ? 1
            : widget is StatelessWidget
                ? 2
                : 0;
    }
}

/// <summary>A widget that does not require mutable state. Dart's <c>StatelessWidget</c>.</summary>
public abstract class StatelessWidget : Widget
{
    protected StatelessWidget(Key? key = null) : base(key)
    {
    }

    /// <summary>Creates a <see cref="StatelessElement"/> to manage this widget's location in the tree.</summary>
    public override Element CreateElement() => new StatelessElement(this);

    /// <summary>Describes the part of the user interface represented by this widget.</summary>
    public abstract Widget Build(BuildContext context);
}

/// <summary>A widget that has mutable state. Dart's <c>StatefulWidget</c>.</summary>
public abstract class StatefulWidget : Widget
{
    protected StatefulWidget(Key? key = null) : base(key)
    {
    }

    /// <summary>Creates a <see cref="StatefulElement"/> to manage this widget's location in the tree.</summary>
    public override Element CreateElement() => new StatefulElement(this);

    /// <summary>Creates the mutable state for this widget at a given location in the tree.</summary>
    public abstract State CreateState();
}

/// <summary>
/// Dart's private <c>_StateLifecycle</c>: the phases a <see cref="State"/> passes through. Tracked in
/// debug builds only, and used to reject <c>SetState</c> and inherited lookups outside their window.
/// </summary>
internal enum StateLifecycle
{
    /// <summary>The object is created. <c>InitState</c> is called at this time.</summary>
    Created,

    /// <summary>
    /// <c>InitState</c> has returned but the state is not ready to build.
    /// <c>DidChangeDependencies</c> is called at this time.
    /// </summary>
    Initialized,

    /// <summary>The state is ready to build and <c>Dispose</c> has not been called yet.</summary>
    Ready,

    /// <summary><c>Dispose</c> has been called; the state can never build again.</summary>
    Defunct
}

/// <summary>
/// The logic and internal state for a <see cref="StatefulWidget"/>. Dart's <c>State</c>; see
/// <see cref="State{T}"/> for the typed <c>widget</c> getter.
/// </summary>
public abstract class State : Diagnosticable, ITickerProvider
{
    private static readonly ConcurrentDictionary<(Type, string), bool> DebugAsyncOverrides = new();

    private HashSet<Ticker>? _tickers;
    private IValueListenable<TickerModeData>? _tickerModeNotifier;
    private StatefulWidget? _widget;
    private StateLifecycle _debugLifecycleState = StateLifecycle.Created;

    /// <summary>Dart's <c>State._element</c>.</summary>
    internal StatefulElement Element = null!;

    /// <summary>
    /// The location in the tree where this widget builds. Dart's <c>State.context</c>, which only
    /// checks for an unmounted state in debug builds.
    /// </summary>
    public BuildContext Context
    {
        get
        {
            if (Constants.KDebugMode && Element is null)
            {
                throw new FlutterError(
                    "This widget has been unmounted, so the State no longer has a context (and should be "
                    + "considered defunct). \n"
                    + "Consider canceling any active work during \"dispose\" or using the \"mounted\" getter "
                    + "to determine if the State is still active.");
            }

            return Element;
        }
    }

    /// <summary>
    /// Whether this state is currently in the tree. Dart's <c>State.mounted</c> is
    /// <c>_element != null</c>: it goes false the moment the element is unmounted, not when the
    /// element merely deactivates.
    /// </summary>
    public bool Mounted => Element is not null;

    /// <summary>The current configuration. Dart's <c>State.widget</c>, which is <c>_widget!</c>.</summary>
    protected StatefulWidget StateWidget => _widget!;

    /// <summary>Dart's <c>State._widget</c>, read by <see cref="StatefulElement.Update"/>.</summary>
    internal StatefulWidget? WidgetOrNull => _widget;

    /// <summary>Dart's <c>State._debugTypesAreRight</c>; <see cref="State{T}"/> narrows it.</summary>
    internal virtual bool DebugTypesAreRight(Widget widget) => true;

    /// <summary>The body of Dart's <c>StatefulElement</c> constructor after <c>createState</c>.</summary>
    internal void AttachElement(StatefulElement element, StatefulWidget widget)
    {
        DebugAssertions.Assert(Element is null);
        Element = element;
        if (Constants.KDebugMode && _widget is not null)
        {
            throw new AssertionError(
                $"The createState function for {widget} returned an old or invalid state instance: "
                + $"{_widget}, which is not null, violating the contract for createState.");
        }

        _widget = widget;
        DebugAssertions.Assert(_debugLifecycleState == StateLifecycle.Created);
    }

    internal void SetWidget(StatefulWidget widget) => _widget = widget;

    /// <summary>Dart's <c>state._element = null</c> in <c>StatefulElement.unmount</c>.</summary>
    internal void DetachElement()
    {
        Element = null!;
    }

    /// <summary>Dart's <c>StatefulElement._firstBuild</c> calling <c>state.initState()</c>.</summary>
    internal void RunInitState()
    {
        DebugAssertions.Assert(_debugLifecycleState == StateLifecycle.Created);
        InitState();
        if (!Constants.KDebugMode)
        {
            return;
        }

        if (DebugOverrideIsAsync(nameof(InitState), Type.EmptyTypes))
        {
            string stateType = Diagnostics.DescribeType(GetType());
            throw new FlutterError(
            [
                new ErrorSummary($"{stateType}.initState() returned a Future."),
                new ErrorDescription("State.initState() must be a void method without an `async` keyword."),
                new ErrorHint(
                    "Rather than awaiting on asynchronous work directly inside of initState, call a separate "
                    + "method to do this work without awaiting it."),
            ]);
        }

        _debugLifecycleState = StateLifecycle.Initialized;
    }

    /// <summary>Dart's <c>StatefulElement.update</c> calling <c>state.didUpdateWidget(oldWidget)</c>.</summary>
    internal void RunDidUpdateWidget(StatefulWidget oldWidget)
    {
        DidUpdateWidget(oldWidget);
        if (!Constants.KDebugMode || !DebugDidUpdateWidgetIsAsync())
        {
            return;
        }

        string stateType = Diagnostics.DescribeType(GetType());
        throw new FlutterError(
        [
            new ErrorSummary($"{stateType}.didUpdateWidget() returned a Future."),
            new ErrorDescription("State.didUpdateWidget() must be a void method without an `async` keyword."),
            new ErrorHint(
                "Rather than awaiting on asynchronous work directly inside of didUpdateWidget, call a separate "
                + "method to do this work without awaiting it."),
        ]);
    }

    private protected virtual bool DebugDidUpdateWidgetIsAsync() =>
        DebugOverrideIsAsync(nameof(DidUpdateWidget), [typeof(StatefulWidget)]);

    /// <summary>
    /// Dart checks whether <c>initState()</c>/<c>didUpdateWidget()</c> returned a <c>Future</c>, which
    /// catches an override accidentally marked <c>async</c>. A C# <c>void</c> override cannot return
    /// one, so the same mistake is an <c>async void</c> override; this reads the compiler's
    /// state-machine marker off the most-derived override.
    /// </summary>
    private protected bool DebugOverrideIsAsync(string name, Type[] parameterTypes)
    {
        return DebugAsyncOverrides.GetOrAdd(
            (GetType(), name + ":" + string.Join(",", parameterTypes.Select(type => type.FullName))),
            key =>
            {
                MethodInfo? method = key.Item1.GetMethod(
                    name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    parameterTypes);
                return method?.IsDefined(typeof(AsyncStateMachineAttribute), inherit: false) == true;
            });
    }

    internal void MarkReady()
    {
        if (Constants.KDebugMode)
        {
            _debugLifecycleState = StateLifecycle.Ready;
        }
    }

    internal void DebugAssertDisposedCalledSuper()
    {
        if (Constants.KDebugMode && _debugLifecycleState != StateLifecycle.Defunct)
        {
            throw new FlutterError(
            [
                new ErrorSummary($"{Diagnostics.DescribeType(GetType())}.dispose failed to call super.dispose."),
                new ErrorDescription(
                    "dispose() implementations must always call their superclass dispose() method, to ensure "
                    + "that all the resources used by the widget are fully released."),
            ]);
        }
    }

    /// <summary>
    /// Dart's <c>StatefulElement.dependOnInheritedElement</c> guards: an inherited lookup is illegal
    /// before <c>InitState</c> completed and after <c>Dispose</c> ran.
    /// </summary>
    internal void DebugCheckCanDependOnInherited(InheritedElement ancestor, Element dependent)
    {
        if (!Constants.KDebugMode)
        {
            return;
        }

        string targetType = Diagnostics.DescribeType(ancestor.Widget.GetType());
        if (_debugLifecycleState == StateLifecycle.Created)
        {
            throw new FlutterError(
            [
                new ErrorSummary(
                    $"dependOnInheritedWidgetOfExactType<{targetType}>() or dependOnInheritedElement() was "
                    + $"called before {Diagnostics.DescribeType(GetType())}.initState() completed."),
                new ErrorDescription(
                    "When an inherited widget changes, for example if the value of Theme.of() changes, its "
                    + "dependent widgets are rebuilt. If the dependent widget's reference to the inherited "
                    + "widget is in a constructor or an initState() method, then the rebuilt dependent widget "
                    + "will not reflect the changes in the inherited widget."),
                new ErrorHint(
                    "Typically references to inherited widgets should occur in widget build() methods. "
                    + "Alternatively, initialization based on inherited widgets can be placed in the "
                    + "didChangeDependencies method, which is called after initState and whenever the "
                    + "dependencies change thereafter."),
            ]);
        }

        if (_debugLifecycleState == StateLifecycle.Defunct)
        {
            throw new FlutterError(
            [
                new ErrorSummary(
                    $"dependOnInheritedWidgetOfExactType<{targetType}>() or dependOnInheritedElement() was "
                    + $"called after dispose(): {dependent}"),
                new ErrorDescription(
                    "This error happens if you call dependOnInheritedWidgetOfExactType() on the BuildContext "
                    + "for a widget that no longer appears in the widget tree (e.g., whose parent widget no "
                    + "longer includes the widget in its build). This error can occur when code calls "
                    + "dependOnInheritedWidgetOfExactType() from a timer or an animation callback."),
                new ErrorHint(
                    "The preferred solution is to cancel the timer or stop listening to the animation in the "
                    + "dispose() callback. Another solution is to check the \"mounted\" property of this object "
                    + "before calling dependOnInheritedWidgetOfExactType() to ensure the object is still in "
                    + "the tree."),
                new ErrorHint(
                    "This error might indicate a memory leak if dependOnInheritedWidgetOfExactType() is being "
                    + "called because another object is retaining a reference to this State object after it "
                    + "has been removed from the tree. To avoid memory leaks, consider breaking the reference "
                    + "to this object during dispose()."),
            ]);
        }
    }

    /// <summary>Called when this object is inserted into the tree. Dart's <c>State.initState</c>.</summary>
    public virtual void InitState()
    {
        DebugAssertions.Assert(_debugLifecycleState == StateLifecycle.Created);
        if (Constants.KDebugMode)
        {
            FoundationDebug.DebugMaybeDispatchCreated("widgets", "State", this);
        }
    }

    /// <summary>Called whenever the widget configuration changes. Dart's <c>State.didUpdateWidget</c>.</summary>
    public virtual void DidUpdateWidget(StatefulWidget oldWidget)
    {
    }

    /// <summary>
    /// Called whenever the application is reassembled during debugging, for example during hot
    /// reload. Dart's <c>State.reassemble</c>.
    /// </summary>
    public virtual void Reassemble()
    {
    }

    /// <summary>
    /// Notify the framework that the internal state of this object has changed. Dart's
    /// <c>State.setState</c>: the two debug guards run before the callback (defunct state, state
    /// still in its constructor) and the third after it (an accidentally asynchronous callback).
    /// </summary>
    public void SetState(Action fn)
    {
        ArgumentNullException.ThrowIfNull(fn);
        DebugCheckCanSetState();
        fn();
        DebugCheckSetStateCallbackWasSynchronous(fn);
        Element.MarkNeedsBuild();
    }

    /// <summary>
    /// Dart inspects the callback's return value and rejects a <c>Future</c>, which catches a closure
    /// accidentally marked <c>async</c>. A C# <see cref="Action"/> cannot return a value, so the same
    /// mistake shows up as an <c>async void</c> lambda; this reads the compiler's state-machine marker
    /// to find it.
    /// </summary>
    private void DebugCheckSetStateCallbackWasSynchronous(Action fn)
    {
        if (!Constants.KDebugMode
            || !fn.Method.IsDefined(typeof(AsyncStateMachineAttribute), inherit: false))
        {
            return;
        }

        throw new FlutterError(
        [
            new ErrorSummary("setState() callback argument returned a Future."),
            new ErrorDescription(
                $"The setState() method on {this} was called with a closure or method that returned a "
                + "Future. Maybe it is marked as \"async\"."),
            new ErrorHint(
                "Instead of performing asynchronous work inside a call to setState(), first execute the "
                + "work (without updating the widget state), and then synchronously update the state "
                + "inside a call to setState()."),
        ]);
    }

    private void DebugCheckCanSetState()
    {
        if (!Constants.KDebugMode)
        {
            return;
        }

        if (_debugLifecycleState == StateLifecycle.Defunct)
        {
            throw new FlutterError(
            [
                new ErrorSummary($"setState() called after dispose(): {this}"),
                new ErrorDescription(
                    "This error happens if you call setState() on a State object for a widget that no "
                    + "longer appears in the widget tree (e.g., whose parent widget no longer includes the "
                    + "widget in its build). This error can occur when code calls setState() from a timer, "
                    + "from an animation callback, or after an asynchronous operation (such as an awaited "
                    + "network request or other Future) completes after the widget has been removed from "
                    + "the tree."),
                new ErrorHint(
                    "The preferred solution is to cancel the timer or stop listening to the animation in "
                    + "the dispose() callback. Another solution is to check the \"mounted\" property of "
                    + "this object before calling setState() to ensure the object is still in the tree."),
                new ErrorHint(
                    "This error might indicate a memory leak if setState() is being called because another "
                    + "object is retaining a reference to this State object after it has been removed from "
                    + "the tree. To avoid memory leaks, consider breaking the reference to this object "
                    + "during dispose()."),
            ]);
        }

        if (_debugLifecycleState == StateLifecycle.Created && !Mounted)
        {
            throw new FlutterError(
            [
                new ErrorSummary($"setState() called in constructor: {this}"),
                new ErrorHint(
                    "This happens when you call setState() on a State object for a widget that hasn't been "
                    + "inserted into the widget tree yet. It is not necessary to call setState() in the "
                    + "constructor, since the state is already assumed to be dirty when it is initially "
                    + "created."),
            ]);
        }
    }

    /// <summary>Called when this object is removed from the tree. Dart's <c>State.deactivate</c>.</summary>
    public virtual void Deactivate()
    {
    }

    /// <summary>
    /// Called when this object is reinserted into the tree after having been removed via
    /// <see cref="Deactivate"/>. Dart's <c>State.activate</c>.
    /// </summary>
    public virtual void Activate()
    {
    }

    /// <summary>Called when this object is removed from the tree permanently. Dart's <c>State.dispose</c>.</summary>
    public virtual void Dispose()
    {
        DebugAssertions.Assert(_debugLifecycleState == StateLifecycle.Ready);
        if (Constants.KDebugMode)
        {
            _debugLifecycleState = StateLifecycle.Defunct;
            FoundationDebug.DebugMaybeDispatchDisposed(this);
        }
    }

    /// <summary>Describes the part of the user interface represented by this widget.</summary>
    public abstract Widget Build(BuildContext context);

    /// <summary>
    /// Called when a dependency of this <see cref="State"/> object changes. Dart's
    /// <c>State.didChangeDependencies</c>.
    /// </summary>
    public virtual void DidChangeDependencies()
    {
    }

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        if (Constants.KDebugMode)
        {
            properties.Add(new EnumProperty<StateLifecycle>(
                "lifecycle state",
                _debugLifecycleState,
                defaultValue: StateLifecycle.Ready));
        }

        properties.Add(new ObjectFlagProperty<StatefulWidget>("_widget", _widget, ifNull: "no widget"));
        properties.Add(new ObjectFlagProperty<StatefulElement>("_element", Element, ifNull: "not mounted"));
    }

    // ITickerProvider: Dart's TickerProviderStateMixin, folded into every State (see DIVERGENCES.md).

    public Ticker CreateTicker(TickerCallback onTick)
    {
        ArgumentNullException.ThrowIfNull(onTick);
        UpdateTickerModeNotifier();
        TickerModeData values = _tickerModeNotifier!.Value;
        var ticker = new WidgetTicker(onTick, RemoveTicker, $"created by {GetType().Name}")
        {
            Muted = !values.Enabled,
            ForceFrames = values.ForceFrames
        };
        _tickers ??= [];
        _tickers.Add(ticker);
        return ticker;
    }

    internal void ActivateTickerProvider()
    {
        if (_tickerModeNotifier is null && _tickers is null)
        {
            return;
        }

        UpdateTickerModeNotifier();
        UpdateTickers();
    }

    internal void DisposeTickerProvider()
    {
        _tickerModeNotifier?.RemoveListener(UpdateTickers);
        _tickerModeNotifier = null;

        if (_tickers is null)
        {
            return;
        }

        foreach (Ticker ticker in _tickers.ToArray())
        {
            ticker.Dispose();
        }

        _tickers = null;
    }

    private void UpdateTickerModeNotifier()
    {
        IValueListenable<TickerModeData> newNotifier = TickerMode.GetValuesNotifier(Context);
        if (ReferenceEquals(newNotifier, _tickerModeNotifier))
        {
            return;
        }

        _tickerModeNotifier?.RemoveListener(UpdateTickers);
        newNotifier.AddListener(UpdateTickers);
        _tickerModeNotifier = newNotifier;
    }

    private void UpdateTickers()
    {
        if (_tickers is null || _tickerModeNotifier is null)
        {
            return;
        }

        TickerModeData values = _tickerModeNotifier.Value;
        foreach (Ticker ticker in _tickers)
        {
            ticker.Muted = !values.Enabled;
            ticker.ForceFrames = values.ForceFrames;
        }
    }

    private void RemoveTicker(Ticker ticker)
    {
        _tickers?.Remove(ticker);
    }
}

/// <summary>
/// A <see cref="State"/> whose configuration is typed. Dart's <c>State&lt;T extends StatefulWidget&gt;</c>:
/// <see cref="Widget"/> is the current <typeparamref name="T"/>, and <see cref="StatefulElement"/>
/// rejects a state created for a different widget type.
/// </summary>
public abstract class State<T> : State where T : StatefulWidget
{
    /// <summary>The current configuration. Dart's <c>State.widget</c>.</summary>
    public T Widget => (T)StateWidget;

    internal override bool DebugTypesAreRight(Widget widget) => widget is T;

    /// <summary>Forwards to the typed <see cref="DidUpdateWidget(T)"/>.</summary>
    public sealed override void DidUpdateWidget(StatefulWidget oldWidget) => DidUpdateWidget((T)oldWidget);

    /// <summary>Called whenever the widget configuration changes. Dart's <c>State.didUpdateWidget</c>.</summary>
    public virtual void DidUpdateWidget(T oldWidget)
    {
    }

    private protected override bool DebugDidUpdateWidgetIsAsync() =>
        DebugOverrideIsAsync(nameof(DidUpdateWidget), [typeof(T)]);
}

/// <summary>
/// A widget that has a child widget provided to it, instead of building a new widget. Dart's
/// <c>ProxyWidget</c>.
/// </summary>
public abstract class ProxyWidget : Widget
{
    protected ProxyWidget(Widget child, Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    /// <summary>
    /// The widget below this widget in the tree. Dart's <c>ProxyWidget.child</c>; virtual because
    /// Dart declares it as a getter that subclasses such as <c>TextSelectionTheme</c> override to
    /// insert a wrapper without changing their public API.
    /// </summary>
    public virtual Widget Child { get; }
}

internal interface IParentDataWidget
{
    bool DebugIsValidRenderObject(RenderObject renderObject);
    void ApplyParentData(RenderObject renderObject);
    Type DebugTypicalAncestorWidgetClass { get; }
    string DebugTypicalAncestorWidgetDescription { get; }

    IEnumerable<DiagnosticsNode> DebugDescribeIncorrectParentDataType(
        IParentData? parentData,
        RenderObjectWidget? parentDataCreator,
        DiagnosticsNode? ownershipChain);
}

/// <summary>
/// Base class for widgets that hook <see cref="ParentData"/> information to children of
/// <see cref="RenderObjectWidget"/>s. Dart's <c>ParentDataWidget&lt;T extends ParentData&gt;</c>.
/// </summary>
public abstract class ParentDataWidget<T> : ProxyWidget, IParentDataWidget where T : IParentData
{
    protected ParentDataWidget(Widget child, Key? key = null) : base(child, key)
    {
    }

    public override Element CreateElement() => new ParentDataElement<T>(this);

    /// <summary>
    /// Checks if this widget can apply its parent data to the provided <paramref name="renderObject"/>.
    /// </summary>
    public virtual bool DebugIsValidRenderObject(RenderObject renderObject)
    {
        DebugAssertParentDataTypeIsSpecific();
        return renderObject.parentData is T;
    }

    /// <summary>Dart's <c>assert(T != dynamic); assert(T != ParentData);</c>.</summary>
    private static void DebugAssertParentDataTypeIsSpecific()
    {
        DebugAssertions.Assert(typeof(T) != typeof(IParentData));
        DebugAssertions.Assert(typeof(T) != typeof(ParentData));
    }

    /// <summary>
    /// Describes the <see cref="RenderObjectWidget"/> that is typically used to set up the
    /// <see cref="ParentData"/> that <see cref="ApplyParentData"/> will write to.
    /// </summary>
    public abstract Type DebugTypicalAncestorWidgetClass { get; }

    /// <summary>
    /// Describes the <see cref="RenderObjectWidget"/> that is typically used to set up the parent
    /// data. Dart's <c>ParentDataWidget.debugTypicalAncestorWidgetDescription</c>.
    /// </summary>
    public virtual string DebugTypicalAncestorWidgetDescription =>
        Diagnostics.DescribeType(DebugTypicalAncestorWidgetClass);

    /// <summary>
    /// Dart's <c>ParentDataWidget._debugDescribeIncorrectParentDataType</c>: the body of the
    /// "Incorrect use of ParentDataWidget" report, naming the parent data this widget wanted to
    /// write, what the render object accepts instead, and the ancestor it was expected to sit under.
    /// </summary>
    IEnumerable<DiagnosticsNode> IParentDataWidget.DebugDescribeIncorrectParentDataType(
        IParentData? parentData,
        RenderObjectWidget? parentDataCreator,
        DiagnosticsNode? ownershipChain)
    {
        DebugAssertParentDataTypeIsSpecific();
        string parentDataType = Diagnostics.DescribeType(typeof(T));
        string description =
            $"The ParentDataWidget {this} wants to apply ParentData of type {parentDataType} to a RenderObject";
        string self = Diagnostics.ObjectRuntimeType(this, "ParentDataWidget");

        var information = new List<DiagnosticsNode>
        {
            parentData is null
                ? new ErrorDescription($"{description}, which has not been set up to receive any ParentData.")
                : new ErrorDescription(
                    $"{description}, which has been set up to accept ParentData of incompatible type "
                    + $"{Diagnostics.DescribeType(parentData.GetType())}."),
            new ErrorHint(
                $"Usually, this means that the {self} widget has the wrong ancestor RenderObjectWidget. "
                + $"Typically, {self} widgets are placed directly inside "
                + $"{DebugTypicalAncestorWidgetDescription} widgets."),
        };

        if (parentDataCreator is not null)
        {
            information.Add(new ErrorHint(
                $"The offending {self} is currently placed inside a "
                + $"{Diagnostics.ObjectRuntimeType(parentDataCreator, "RenderObjectWidget")} widget."));
        }

        if (ownershipChain is not null)
        {
            information.Add(new ErrorDescription(
                "The ownership chain for the RenderObject that received the incompatible parent data was:\n  "
                + ownershipChain));
        }

        return information;
    }

    /// <summary>Write the data from this widget into the given render object's parent data.</summary>
    public abstract void ApplyParentData(RenderObject renderObject);

    /// <summary>
    /// Whether the parent data this widget writes may be applied outside of the build phase, because
    /// the write cannot invalidate layout.
    /// </summary>
    /// <remarks>Flutter's <c>ParentDataWidget.debugCanApplyOutOfTurn</c>; false by default.</remarks>
    public virtual bool DebugCanApplyOutOfTurn() => false;
}

/// <summary>
/// Base class for widgets that efficiently propagate information down the tree. Dart's
/// <c>InheritedWidget</c>.
/// </summary>
public abstract class InheritedWidget : ProxyWidget
{
    protected InheritedWidget(Widget child, Key? key = null) : base(child, key)
    {
    }

    public override Element CreateElement() => new InheritedElement(this);

    /// <summary>
    /// Whether the framework should notify widgets that inherit from this widget. Dart's
    /// <c>InheritedWidget.updateShouldNotify</c>.
    /// </summary>
    public abstract bool UpdateShouldNotify(InheritedWidget oldWidget);
}

public abstract class InheritedModel<TAspect> : InheritedWidget
{
    protected InheritedModel(Widget child, Key? key = null) : base(child, key)
    {
    }

    protected abstract bool UpdateShouldNotifyDependent(
        InheritedModel<TAspect> oldWidget,
        IReadOnlySet<TAspect> dependencies);

    internal bool InvokeUpdateShouldNotifyDependent(
        InheritedModel<TAspect> oldWidget,
        IReadOnlySet<TAspect> dependencies)
        => UpdateShouldNotifyDependent(oldWidget, dependencies);

    protected virtual bool IsSupportedAspect(object aspect) => true;

    public override Element CreateElement() => new InheritedModelElement<TAspect>(this);

    public static TModel? InheritFrom<TModel>(BuildContext context, object? aspect = null)
        where TModel : InheritedModel<TAspect>
    {
        if (aspect == null)
        {
            return context.DependOnInheritedWidgetOfExactType<TModel>();
        }

        var models = new List<InheritedElement>();
        FindModels<TModel>((Element)context, aspect, models);
        if (models.Count == 0)
        {
            return null;
        }

        TModel? value = null;
        foreach (var model in models)
        {
            value = (TModel)context.DependOnInheritedElement(model, aspect);
        }

        return value;
    }

    private static void FindModels<TModel>(
        Element contextElement,
        object aspect,
        List<InheritedElement> results)
        where TModel : InheritedModel<TAspect>
    {
        InheritedElement? model = contextElement.GetElementForInheritedWidgetOfExactType<TModel>();
        if (model == null)
        {
            return;
        }

        results.Add(model);

        var modelWidget = (TModel)model.Widget;
        if (modelWidget.IsSupportedAspect(aspect))
        {
            return;
        }

        // The model's own scope contains itself, so hop to its parent before looking again.
        Element? modelParent = null;
        model.VisitAncestorElements(ancestor =>
        {
            modelParent = ancestor;
            return false;
        });

        if (modelParent == null)
        {
            return;
        }

        FindModels<TModel>(modelParent, aspect, results);
    }
}

public abstract class InheritedNotifier<TNotifier> : InheritedWidget where TNotifier : class, IListenable
{
    protected InheritedNotifier(TNotifier? notifier, Widget child, Key? key = null) : base(child, key)
    {
        Notifier = notifier;
    }

    public TNotifier? Notifier { get; }

    public override bool UpdateShouldNotify(InheritedWidget oldWidget)
        => !ReferenceEquals(((InheritedNotifier<TNotifier>)oldWidget).Notifier, Notifier);

    public override Element CreateElement() => new InheritedNotifierElement<TNotifier>(this);
}
