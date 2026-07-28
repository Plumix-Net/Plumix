namespace Plumix.Widgets;

// Dart parity source:
// flutter/packages/flutter/lib/src/widgets/disposable_build_context.dart

public sealed class DisposableBuildContext<T> : IDisposable where T : State
{
    private T? _state;

    public DisposableBuildContext(T state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!state.Mounted)
        {
            throw new ArgumentException(
                "A DisposableBuildContext was given a BuildContext for an Element that is not mounted.",
                nameof(state));
        }

        _state = state;
    }

    public BuildContext? Context
    {
        get
        {
            Validate();
            return _state?.Context;
        }
    }

    public void Dispose()
    {
        _state = null;
    }

    private void Validate()
    {
        if (_state is not null && !_state.Mounted)
        {
            throw new InvalidOperationException(
                "A DisposableBuildContext tried to access the BuildContext of a disposed State object. "
                + "This can happen when its creator fails to dispose the DisposableBuildContext.");
        }
    }
}
