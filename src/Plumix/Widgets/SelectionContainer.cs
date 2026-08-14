using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/selection_container.dart

/// A container that handles [SelectionEvent]s for the [ISelectable]s in the subtree.
public sealed class SelectionContainer : StatefulWidget
{
    public SelectionContainer(
        SelectionContainerDelegate @delegate,
        Widget child,
        ISelectionRegistrar? registrar = null,
        Key? key = null) : base(key)
    {
        Delegate = @delegate ?? throw new ArgumentNullException(nameof(@delegate));
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Registrar = registrar;
    }

    private SelectionContainer(Widget child, Key? key) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Delegate = null;
        Registrar = null;
    }

    /// Creates a selection container that disables selection for the subtree.
    public static SelectionContainer Disabled(Widget child, Key? key = null) => new(child, key);

    /// The [ISelectionRegistrar] this container is registered to.
    public ISelectionRegistrar? Registrar { get; }

    /// The child widget this selection container applies to.
    public Widget Child { get; }

    /// The delegate that handles the [SelectionEvent]s of the subtree.
    public SelectionContainerDelegate? Delegate { get; }

    internal bool IsDisabled => Delegate is null;

    /// Gets the immediate ancestor [ISelectionRegistrar] of the [BuildContext].
    ///
    /// Returns null when the immediate [SelectionContainer] is disabled or when
    /// there is no [SelectionContainer] above the context.
    public static ISelectionRegistrar? MaybeOf(BuildContext context)
    {
        SelectionRegistrarScope? scope = context.DependOnInherited<SelectionRegistrarScope>();
        return scope?.Registrar;
    }

    public override State CreateState() => new SelectionContainerState();
}

public sealed class SelectionContainerState : State, ISelectable
{
    private static readonly SelectionGeometry DisabledGeometry =
        new(status: SelectionStatus.None, hasContent: true);

    private readonly HashSet<Action> _listeners = [];
    private SelectionRegistrant _registrant = null!;

    private SelectionContainer Current => (SelectionContainer)StateWidget;

    private ISelectionRegistrar? Registrar
    {
        get => _registrant.Registrar;
        set => _registrant.Registrar = value;
    }

    public override void InitState()
    {
        base.InitState();
        _registrant = new SelectionRegistrant(this);
        if (Current.IsDisabled)
        {
            return;
        }

        Current.Delegate!.SelectionContainerContext = Context;
        if (Current.Registrar is not null)
        {
            Registrar = Current.Registrar;
        }
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var previous = (SelectionContainer)oldWidget;
        if (!ReferenceEquals(previous.Delegate, Current.Delegate))
        {
            if (!previous.IsDisabled)
            {
                previous.Delegate!.SelectionContainerContext = null;
                foreach (Action listener in _listeners)
                {
                    previous.Delegate.RemoveListener(listener);
                }
            }

            if (!Current.IsDisabled)
            {
                Current.Delegate!.SelectionContainerContext = Context;
                foreach (Action listener in _listeners)
                {
                    Current.Delegate.AddListener(listener);
                }
            }

            if (!Equals(previous.Delegate?.Value, Current.Delegate?.Value))
            {
                foreach (Action listener in _listeners.ToList())
                {
                    listener();
                }
            }
        }

        if (Current.IsDisabled)
        {
            Registrar = null;
        }
        else if (Current.Registrar is not null)
        {
            Registrar = Current.Registrar;
        }
    }

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        if (Current.Registrar is null && !Current.IsDisabled)
        {
            Registrar = SelectionContainer.MaybeOf(Context);
        }
    }

    public void AddListener(Action listener)
    {
        Current.Delegate!.AddListener(listener);
        _listeners.Add(listener);
    }

    public void RemoveListener(Action listener)
    {
        Current.Delegate?.RemoveListener(listener);
        _listeners.Remove(listener);
    }

    public void PushHandleLayers(LayerLink? startHandle, LayerLink? endHandle)
    {
        Current.Delegate!.PushHandleLayers(startHandle, endHandle);
    }

    public SelectedContent? GetSelectedContent() => Current.Delegate!.GetSelectedContent();

    public SelectedContentRange? GetSelection() => Current.Delegate!.GetSelection();

    public SelectionResult DispatchSelectionEvent(SelectionEvent @event)
    {
        return Current.Delegate!.DispatchSelectionEvent(@event);
    }

    public SelectionGeometry Value => Current.IsDisabled ? DisabledGeometry : Current.Delegate!.Value;

    public Matrix GetTransformTo(RenderObject? ancestor)
    {
        return Context.FindRenderObject()!.GetTransformTo(ancestor);
    }

    public int ContentLength => Current.Delegate!.ContentLength;

    public Size Size => ((RenderBox)Context.FindRenderObject()!).Size;

    public IReadOnlyList<Rect> BoundingBoxes => [((RenderBox)Context.FindRenderObject()!).PaintBounds];

    public override void Dispose()
    {
        if (!Current.IsDisabled)
        {
            Current.Delegate!.SelectionContainerContext = null;
            foreach (Action listener in _listeners)
            {
                Current.Delegate.RemoveListener(listener);
            }
        }

        _registrant.Dispose();
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return Current.IsDisabled
            ? SelectionRegistrarScope.Disabled(Current.Child)
            : new SelectionRegistrarScope(Current.Delegate!, Current.Child);
    }
}

/// An inherited widget that hosts a [ISelectionRegistrar] for the subtree.
public sealed class SelectionRegistrarScope : InheritedWidget
{
    public SelectionRegistrarScope(ISelectionRegistrar registrar, Widget child, Key? key = null) : base(key)
    {
        Registrar = registrar ?? throw new ArgumentNullException(nameof(registrar));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    private SelectionRegistrarScope(Widget child, Key? key) : base(key)
    {
        Registrar = null;
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    internal static SelectionRegistrarScope Disabled(Widget child, Key? key = null) => new(child, key);

    /// The [ISelectionRegistrar] hosted by this widget.
    public ISelectionRegistrar? Registrar { get; }

    /// The subtree this registrar applies to.
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !ReferenceEquals(((SelectionRegistrarScope)oldWidget).Registrar, Registrar);
    }
}

/// A delegate to handle [SelectionEvent]s for a [SelectionContainer].
public abstract class SelectionContainerDelegate : ChangeNotifier, ISelectionHandler, ISelectionRegistrar
{
    internal BuildContext? SelectionContainerContext { get; set; }

    public abstract void PushHandleLayers(LayerLink? startHandle, LayerLink? endHandle);

    public abstract SelectedContent? GetSelectedContent();

    public abstract SelectedContentRange? GetSelection();

    public abstract SelectionResult DispatchSelectionEvent(SelectionEvent @event);

    public abstract SelectionGeometry Value { get; }

    public abstract int ContentLength { get; }

    public abstract void Add(ISelectable selectable);

    public abstract void Remove(ISelectable selectable);

    /// Gets the transform from the `child` to the [SelectionContainer] of this delegate.
    public Matrix GetTransformFrom(ISelectable child)
    {
        return child.GetTransformTo(RequireContainerRenderBox());
    }

    /// Gets the transform from the [SelectionContainer] of this delegate to the `ancestor`.
    public Matrix GetTransformTo(RenderObject? ancestor)
    {
        return RequireContainerRenderBox().GetTransformTo(ancestor);
    }

    /// Whether the [SelectionContainer] has undergone layout and has a size.
    public bool HasSize => RequireContainerRenderBox().HasSize;

    /// The size of the [SelectionContainer] of this delegate.
    public Size ContainerSize
    {
        get
        {
            RenderBox box = RequireContainerRenderBox();
            if (!box.HasSize)
            {
                throw new InvalidOperationException(
                    "ContainerSize cannot be called before SelectionContainer is laid out.");
            }

            return box.Size;
        }
    }

    private RenderBox RequireContainerRenderBox()
    {
        if (SelectionContainerContext?.FindRenderObject() is not RenderBox box)
        {
            throw new InvalidOperationException(
                "The SelectionContainer must have a render object, such as after the first build has completed.");
        }

        return box;
    }
}
