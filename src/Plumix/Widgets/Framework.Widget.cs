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
public abstract class Widget(Key? key = null)
{
    public Key? Key { get; } = key;

    internal static bool CanUpdate(Widget oldWidget, Widget newWidget)
    {
        return oldWidget.GetType() == newWidget.GetType() && Equals(oldWidget.Key, newWidget.Key);
    }

    internal abstract Element CreateElement();
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

public abstract class State
{
    internal StatefulElement Element = null!;
    public BuildContext Context => new(Element);
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
