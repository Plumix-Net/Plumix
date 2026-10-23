using Plumix.Widgets;

// C#-only test infrastructure: Dart has no counterpart. Flutter's tests assign
// `FocusManager.instance.highlightStrategy` and rely on flutter_test resetting it between tests.

namespace Plumix.Tests;

/// <summary>
/// Sets <see cref="FocusManager.HighlightStrategy"/> for the lifetime of a <c>using</c> scope and
/// restores the previous strategy afterwards, so a test that needs keyboard focus highlights on the
/// mobile default target platform does not leak its strategy into later tests.
/// </summary>
internal sealed class HighlightStrategyScope : IDisposable
{
    private readonly FocusManager _focusManager;
    private readonly FocusHighlightStrategy _previous;

    public HighlightStrategyScope(FocusHighlightStrategy strategy = FocusHighlightStrategy.AlwaysTraditional)
    {
        _focusManager = FocusManager.Instance;
        _previous = _focusManager.HighlightStrategy;
        _focusManager.HighlightStrategy = strategy;
    }

    public void Dispose() => _focusManager.HighlightStrategy = _previous;
}
