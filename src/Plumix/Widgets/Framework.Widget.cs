using System.Runtime.CompilerServices;
using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/framework.dart (approximate)

namespace Plumix.Widgets;

/// <summary>
/// Describes the configuration for an [Element].
///
/// Widgets are the central class hierarchy in the Plumix.Sample framework. A widget
/// is an immutable description of part of a user interface. Widgets can be
/// inflated into elements, which manage the underlying render tree.
///
/// </summary>
public abstract class Widget(Key? key = null) : DiagnosticableTree
{
    public Key? Key { get; } = key;

    public static bool CanUpdate(Widget oldWidget, Widget newWidget)
    {
        return oldWidget.GetType() == newWidget.GetType() && Equals(oldWidget.Key, newWidget.Key);
    }

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
}

public abstract class StatelessWidget : Widget
{
    protected StatelessWidget(Key? key = null) : base(key)
    {
    }

    public abstract Widget Build(BuildContext context);
    public override Element CreateElement() => new StatelessElement(this);
}

public abstract class StatefulWidget : Widget
{
    protected StatefulWidget(Key? key = null) : base(key)
    {
    }

    public abstract State CreateState();
    public override Element CreateElement() => new StatefulElement(this);
}

public abstract class ProxyWidget : Widget
{
    protected ProxyWidget(Widget child, Key? key = null) : base(key)
    {
        Child = child;
    }

    public Widget Child { get; }
}

internal interface IParentDataWidget
{
    bool DebugIsValidRenderObject(RenderObject renderObject);
    void ApplyParentData(RenderObject renderObject);
    Type DebugTypicalAncestorWidgetType { get; }
    string DebugTypicalAncestorWidgetDescription { get; }
    Type DebugParentDataType { get; }

    IEnumerable<DiagnosticsNode> DebugDescribeIncorrectParentDataType(
        IParentData? parentData,
        RenderObjectWidget? parentDataCreator,
        DiagnosticsNode? ownershipChain);
}

public abstract class ParentDataWidget<T> : ProxyWidget, IParentDataWidget where T : IParentData
{
    protected ParentDataWidget(Widget child, Key? key = null) : base(child, key)
    {
    }

    public abstract Type DebugTypicalAncestorWidgetType { get; }

    /// <summary>
    /// How the ancestor this widget expects is named in error messages, when naming the single
    /// <see cref="DebugTypicalAncestorWidgetType"/> is not enough.
    /// </summary>
    /// <remarks>Flutter's <c>ParentDataWidget.debugTypicalAncestorWidgetDescription</c>.</remarks>
    public virtual string DebugTypicalAncestorWidgetDescription => DebugTypicalAncestorWidgetType.Name;

    /// <summary>
    /// Whether the parent data this widget writes may be applied outside of the build phase, because
    /// the write cannot invalidate layout.
    /// </summary>
    /// <remarks>Flutter's <c>ParentDataWidget.debugCanApplyOutOfTurn</c>; false by default.</remarks>
    public virtual bool DebugCanApplyOutOfTurn() => false;

    public override Element CreateElement() => new ParentDataElement<T>(this);

    protected virtual bool DebugIsValidRenderObject(RenderObject renderObject)
    {
        return renderObject.parentData is T;
    }

    protected abstract void ApplyParentData(RenderObject renderObject);

    bool IParentDataWidget.DebugIsValidRenderObject(RenderObject renderObject)
    {
        return DebugIsValidRenderObject(renderObject);
    }

    void IParentDataWidget.ApplyParentData(RenderObject renderObject)
    {
        ApplyParentData(renderObject);
    }

    Type IParentDataWidget.DebugParentDataType => typeof(T);

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

public abstract class State : Diagnosticable, ITickerProvider
{
    private HashSet<Ticker>? _tickers;
    private IValueListenable<TickerModeData>? _tickerModeNotifier;
    private StatefulWidget? _widget;
    private StateLifecycle _debugLifecycleState = StateLifecycle.Created;

    internal StatefulElement Element = null!;
    public BuildContext Context
    {
        get
        {
            if (!Mounted)
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

    protected StatefulWidget StateWidget => _widget ?? (StatefulWidget)Element.Widget;

    internal void AttachElement(StatefulElement element, StatefulWidget widget)
    {
        if (Constants.KDebugMode && Element is not null)
        {
            throw new AssertionError(
                $"The createState function for {widget} returned an old or invalid state instance, "
                + "which is already attached to an element, violating the contract for createState.");
        }

        Element = element;
        if (Constants.KDebugMode && _widget is not null)
        {
            throw new AssertionError(
                $"The createState function for {widget} returned an old or invalid state instance: "
                + $"{_widget}, which is not null, violating the contract for createState.");
        }

        _widget = widget;
    }

    internal void SetWidget(StatefulWidget widget) => _widget = widget;

    internal void DetachElement()
    {
        Element = null!;
        _widget = null;
    }

    /// <summary>Dart's <c>StatefulElement._firstBuild</c> calling <c>state.initState()</c>.</summary>
    internal void RunInitState()
    {
        if (Constants.KDebugMode && _debugLifecycleState != StateLifecycle.Created)
        {
            throw new AssertionError($"{GetType().Name}.InitState() was called more than once.");
        }

        InitState();
        if (Constants.KDebugMode)
        {
            _debugLifecycleState = StateLifecycle.Initialized;
        }
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
                new ErrorSummary($"{GetType().Name}.Dispose failed to call base.Dispose."),
                new ErrorDescription(
                    "Dispose() implementations must always call their base class Dispose() method, to "
                    + "ensure that all the resources used by the widget are fully released."),
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
                    $"DependOnInherited<{targetType}>() or DependOnInheritedElement() was called before "
                    + $"{GetType().Name}.InitState() completed."),
                new ErrorDescription(
                    "When an inherited widget changes, for example if the value of Theme.Of() changes, its "
                    + "dependent widgets are rebuilt. If the dependent widget's reference to the inherited "
                    + "widget is in a constructor or an InitState() method, then the rebuilt dependent "
                    + "widget will not reflect the changes in the inherited widget."),
                new ErrorHint(
                    "Typically references to inherited widgets should occur in widget Build() methods. "
                    + "Alternatively, initialization based on inherited widgets can be placed in the "
                    + "DidChangeDependencies method, which is called after InitState and whenever the "
                    + "dependencies change thereafter."),
            ]);
        }

        if (_debugLifecycleState == StateLifecycle.Defunct)
        {
            throw new FlutterError(
            [
                new ErrorSummary(
                    $"DependOnInherited<{targetType}>() or DependOnInheritedElement() was called after "
                    + $"Dispose(): {dependent}"),
                new ErrorDescription(
                    "This error happens if you call DependOnInherited() on the BuildContext for a widget "
                    + "that no longer appears in the widget tree (e.g., whose parent widget no longer "
                    + "includes the widget in its build). This error can occur when code calls "
                    + "DependOnInherited() from a timer or an animation callback."),
                new ErrorHint(
                    "The preferred solution is to cancel the timer or stop listening to the animation in "
                    + "the Dispose() callback. Another solution is to check the \"Mounted\" property of "
                    + "this object before calling DependOnInherited() to ensure the object is still in the "
                    + "tree."),
                new ErrorHint(
                    "This error might indicate a memory leak if DependOnInherited() is being called "
                    + "because another object is retaining a reference to this State object after it has "
                    + "been removed from the tree. To avoid memory leaks, consider breaking the reference "
                    + "to this object during Dispose()."),
            ]);
        }
    }

    public virtual void InitState()
    {
    }

    public virtual void DidUpdateWidget(StatefulWidget oldWidget)
    {
    }

    public virtual void DidChangeDependencies()
    {
    }

    public virtual void Activate()
    {
    }

    public virtual void Deactivate()
    {
    }

    public virtual void Dispose()
    {
        if (Constants.KDebugMode)
        {
            if (_debugLifecycleState != StateLifecycle.Ready)
            {
                throw new AssertionError(
                    $"{GetType().Name}.Dispose() was called while the state was in the "
                    + $"{_debugLifecycleState} phase.");
            }

            _debugLifecycleState = StateLifecycle.Defunct;
        }
    }

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

    /// Called whenever the application is reassembled during debugging, for
    /// example during hot reload.
    ///
    /// This method should rerun any initialization logic that depends on
    /// global state, for example, image loading from asset bundles (since the
    /// asset bundle may have changed).
    ///
    /// In addition to this method being invoked, it is guaranteed that the
    /// [Build] method will be invoked when a reassemble is signaled. Most
    /// widgets therefore do not need to do anything in the [Reassemble] method.
    ///
    /// See also:
    ///
    ///  * [Element.Reassemble]
    public virtual void Reassemble()
    {
    }

    public abstract Widget Build(BuildContext context);

    /// <summary>
    /// Dart's <c>State.setState</c>. The two debug guards run before the callback (defunct state,
    /// state still in its constructor) and the third after it (an accidentally asynchronous callback).
    /// </summary>
    protected void SetState(Action updater)
    {
        ArgumentNullException.ThrowIfNull(updater);
        DebugCheckCanSetState();
        updater();
        DebugCheckSetStateCallbackWasSynchronous(updater);
        Element.MarkNeedsBuild();
    }

    /// <summary>
    /// Dart inspects the callback's return value and rejects a <c>Future</c>, which catches a closure
    /// accidentally marked <c>async</c>. A C# <see cref="Action"/> cannot return a value, so the same
    /// mistake shows up as an <c>async void</c> lambda; this reads the compiler's state-machine marker
    /// to find it.
    /// </summary>
    private void DebugCheckSetStateCallbackWasSynchronous(Action updater)
    {
        if (!Constants.KDebugMode
            || !updater.Method.IsDefined(typeof(AsyncStateMachineAttribute), inherit: false))
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

    // helper for external callers
    public void InvokeSetState(Action updater) => SetState(updater);
}

// Inherited widgets
public abstract class InheritedWidget : Widget
{
    protected InheritedWidget(Key? key = null) : base(key)
    {
    }

    public abstract Widget Build(BuildContext context);

    protected abstract bool UpdateShouldNotify(InheritedWidget oldWidget);

    internal bool InvokeUpdateShouldNotify(InheritedWidget oldWidget) => UpdateShouldNotify(oldWidget);

    public override Element CreateElement() => new InheritedElement(this);
}

public abstract class InheritedModel<TAspect> : InheritedWidget
{
    protected InheritedModel(Key? key = null) : base(key)
    {
    }

    protected abstract bool UpdateShouldNotifyDependent(
        InheritedModel<TAspect> oldWidget,
        IReadOnlySet<TAspect> dependencies);

    internal bool InvokeUpdateShouldNotifyDependent(InheritedModel<TAspect> oldWidget, IReadOnlySet<TAspect> dependencies)
        => UpdateShouldNotifyDependent(oldWidget, dependencies);

    protected virtual bool IsSupportedAspect(object aspect) => true;

    public override Element CreateElement() => new InheritedModelElement<TAspect>(this);

    public static TModel? InheritFrom<TModel>(BuildContext context, object? aspect = null)
        where TModel : InheritedModel<TAspect>
    {
        if (aspect == null)
        {
            return context.DependOnInherited<TModel>();
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
    protected InheritedNotifier(TNotifier? notifier, Widget child, Key? key = null) : base(key)
    {
        Notifier = notifier;
        Child = child;
    }

    public TNotifier? Notifier { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
        => !ReferenceEquals(((InheritedNotifier<TNotifier>)oldWidget).Notifier, Notifier);

    public override Element CreateElement() => new InheritedNotifierElement<TNotifier>(this);
}
