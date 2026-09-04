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

    internal static bool CanUpdate(Widget oldWidget, Widget newWidget)
    {
        return oldWidget.GetType() == newWidget.GetType() && Equals(oldWidget.Key, newWidget.Key);
    }

    internal abstract Element CreateElement();

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
    internal override Element CreateElement() => new StatelessElement(this);
}

public abstract class StatefulWidget : Widget
{
    protected StatefulWidget(Key? key = null) : base(key)
    {
    }

    public abstract State CreateState();
    internal override Element CreateElement() => new StatefulElement(this);
}

public abstract class ProxyWidget : Widget
{
    protected ProxyWidget(Widget child, Key? key = null) : base(key)
    {
        Child = child;
    }

    public Widget Child { get; }

    internal override Element CreateElement() => new ProxyElement(this);
}

internal interface IParentDataWidget
{
    bool DebugIsValidRenderObject(RenderObject renderObject);
    void ApplyParentData(RenderObject renderObject);
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

    internal override Element CreateElement() => new ParentDataElement<T>(this);

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
}

public abstract class State : Diagnosticable, ITickerProvider
{
    private HashSet<Ticker>? _tickers;
    private IValueListenable<TickerModeData>? _tickerModeNotifier;

    internal StatefulElement Element = null!;
    public BuildContext Context
    {
        get
        {
            if (!Mounted)
            {
                throw new InvalidOperationException(
                    "This State no longer has a context because its Element was unmounted.");
            }

            return new BuildContext(Element);
        }
    }

    public bool Mounted => Element is not null && Element.IsMounted;
    protected StatefulWidget StateWidget => (StatefulWidget)Element.Widget;

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

    protected void SetState(Action updater)
    {
        updater();
        Element.MarkNeedsBuild();
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

    internal override Element CreateElement() => new InheritedElement(this);
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

    internal override Element CreateElement() => new InheritedModelElement<TAspect>(this);

    public static TModel? InheritFrom<TModel>(BuildContext context, object? aspect = null)
        where TModel : InheritedModel<TAspect>
    {
        if (aspect == null)
        {
            return context.DependOnInherited<TModel>();
        }

        var models = new List<InheritedElement>();
        FindModels<TModel>(context.Owner, aspect, models);
        if (models.Count == 0)
        {
            return null;
        }

        TModel? value = null;
        foreach (var model in models)
        {
            value = (TModel)context.Owner.DependOnInheritedElement(model, aspect);
        }

        return value;
    }

    private static void FindModels<TModel>(
        Element contextElement,
        object aspect,
        List<InheritedElement> results)
        where TModel : InheritedModel<TAspect>
    {
        var model = GetNearestAncestorModel<TModel>(contextElement);
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

        FindModels<TModel>(model, aspect, results);
    }

    private static InheritedElement? GetNearestAncestorModel<TModel>(Element contextElement)
        where TModel : InheritedModel<TAspect>
    {
        for (var ancestor = contextElement.Parent; ancestor != null; ancestor = ancestor.Parent)
        {
            if (ancestor is InheritedElement inheritedElement && inheritedElement.Widget is TModel)
            {
                return inheritedElement;
            }
        }

        return null;
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

    internal override Element CreateElement() => new InheritedNotifierElement<TNotifier>(this);
}
