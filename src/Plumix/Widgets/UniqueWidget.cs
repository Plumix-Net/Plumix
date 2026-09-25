namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/unique_widget.dart

/// <summary>
/// Base class for stateful widgets that have exactly one inflated instance in the tree.
/// </summary>
/// <remarks>
/// Such widgets must be given a <see cref="GlobalKey{T}"/> when they are constructed, and that key
/// gives <see cref="CurrentState"/> direct access to their <see cref="State"/>.
/// </remarks>
public abstract class UniqueWidget<T> : StatefulWidget where T : State
{
    /// <summary>Creates a widget that has exactly one inflated instance in the tree.</summary>
    protected UniqueWidget(GlobalKey<T> key) : base(key ?? throw new ArgumentNullException(nameof(key)))
    {
    }

    /// <inheritdoc/>
    public abstract override T CreateState();

    /// <summary>The state for the unique inflated instance of this widget, or null when none is mounted.</summary>
    public T? CurrentState => ((GlobalKey<T>)Key!).CurrentState;
}
