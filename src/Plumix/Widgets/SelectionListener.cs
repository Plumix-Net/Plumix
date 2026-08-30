using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/selectable_region.dart

/// Notifies its [SelectionListenerNotifier] when the selection under its subtree changes.
///
/// This widget does not listen to the selection of a nested [SelectableRegion] in its
/// subtree: that region is self-contained and does not bubble its selection upwards.
/// To listen to it, place another [SelectionListener] under the nested region.
public sealed class SelectionListener : StatefulWidget
{
    public SelectionListener(
        SelectionListenerNotifier selectionNotifier,
        Widget child,
        Key? key = null) : base(key)
    {
        SelectionNotifier = selectionNotifier ?? throw new ArgumentNullException(nameof(selectionNotifier));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    /// Notifies listeners when the selection under this [SelectionListener] has changed.
    public SelectionListenerNotifier SelectionNotifier { get; }

    /// The child widget this [SelectionListener] observes.
    public Widget Child { get; }

    public override State CreateState() => new SelectionListenerState();
}

internal sealed class SelectionListenerState : State
{
    private SelectionListenerDelegate? _selectionDelegateField;

    private SelectionListener Current => (SelectionListener)StateWidget;

    // Dart declares this field `late final` with an initializer, which runs on first access.
    private SelectionListenerDelegate SelectionDelegate =>
        _selectionDelegateField ??= new SelectionListenerDelegate(Current.SelectionNotifier);

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        if (!ReferenceEquals(((SelectionListener)oldWidget).SelectionNotifier, Current.SelectionNotifier))
        {
            SelectionDelegate.SetNotifier(Current.SelectionNotifier);
        }
    }

    public override void Dispose()
    {
        SelectionDelegate.Dispose();
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new SelectionContainer(SelectionDelegate, Current.Child);
    }
}

internal sealed class SelectionListenerDelegate : StaticSelectionContainerDelegate, ISelectionDetails
{
    public SelectionListenerDelegate(SelectionListenerNotifier selectionNotifier)
    {
        _selectionNotifier = selectionNotifier;
        _selectionNotifier.RegisterSelectionListenerDelegate(this);
    }

    private SelectionGeometry? _initialSelectionGeometry;

    private SelectionListenerNotifier _selectionNotifier;

    public void SetNotifier(SelectionListenerNotifier newNotifier)
    {
        _selectionNotifier.UnregisterSelectionListenerDelegate();
        _selectionNotifier = newNotifier;
        _selectionNotifier.RegisterSelectionListenerDelegate(this);
    }

    public override void NotifyListeners()
    {
        base.NotifyListeners();
        // Skip initial notification if selection is not valid.
        if (_initialSelectionGeometry is null && !Value.HasSelection)
        {
            _initialSelectionGeometry = Value;
            return;
        }

        _selectionNotifier.NotifyListeners();
    }

    public override void Dispose()
    {
        _selectionNotifier.UnregisterSelectionListenerDelegate();
        _initialSelectionGeometry = null;
        base.Dispose();
    }

    public SelectedContentRange? Range => GetSelection();

    public SelectionStatus Status => Value.Status;
}

/// The details of a selection under a [SelectionListener].
public interface ISelectionDetails
{
    /// The computed selection range of the owning [SelectionListener]s subtree.
    ///
    /// Returns null if there is nothing selected.
    SelectedContentRange? Range { get; }

    /// The status that indicates whether there is a selection and whether the selection is collapsed.
    SelectionStatus Status { get; }
}

/// Notifies listeners when the selection under a [SelectionListener] changes.
public sealed class SelectionListenerNotifier : ChangeNotifier
{
    private SelectionListenerDelegate? _selectionDelegate;

    /// The selection of the [SelectionListener] subtree this notifier has been registered to.
    ///
    /// Throws when no [SelectionListener] has been registered to this notifier.
    public ISelectionDetails Selection =>
        _selectionDelegate
        ?? throw new InvalidOperationException("Selection client has not been registered to this notifier.");

    /// Whether this notifier has been registered to a [SelectionListener].
    public bool Registered => _selectionDelegate is not null;

    internal void RegisterSelectionListenerDelegate(SelectionListenerDelegate selectionDelegate)
    {
        if (Constants.KDebugMode && Registered)
        {
            throw new InvalidOperationException(
                "This SelectionListenerNotifier is already registered to another SelectionListener. "
                + "Try providing a new SelectionListenerNotifier.");
        }

        _selectionDelegate = selectionDelegate;
    }

    internal void UnregisterSelectionListenerDelegate()
    {
        _selectionDelegate = null;
    }

    // From ChangeNotifier.
    public override void Dispose()
    {
        UnregisterSelectionListenerDelegate();
        base.Dispose();
    }

    /// Calls the listener every time the [SelectionGeometry] of the selection changes under a
    /// [SelectionListener].
    ///
    /// Listeners can be removed with [RemoveListener].
    public override void AddListener(Action listener) => base.AddListener(listener);
}
