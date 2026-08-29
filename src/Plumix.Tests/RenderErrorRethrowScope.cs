using Plumix.Foundation;

namespace Plumix.Tests;

/// <summary>
/// Restores exception propagation out of the rendering pipeline for the duration of a test.
/// </summary>
/// <remarks>
/// Flutter's <c>RenderObject.layout</c> and <c>RenderObject._paintWithContext</c> catch whatever
/// <c>performResize</c>, <c>performLayout</c> and <c>paint</c> throw and hand it to
/// <c>FlutterError.reportError</c>, so the rest of the frame still runs. Flutter's own tests still see
/// those errors because <c>TestWidgetsFlutterBinding</c> installs an <c>onError</c> that records and
/// re-surfaces them; this scope is the Plumix equivalent for a test that asserts on a layout error.
/// </remarks>
internal sealed class RenderErrorRethrowScope : IDisposable
{
    private readonly FlutterExceptionHandler? _previous;

    private RenderErrorRethrowScope()
    {
        _previous = FlutterError.OnError;
        FlutterError.OnError = static details =>
        {
            if (details.Exception is Exception exception)
            {
                throw exception;
            }
        };
    }

    /// <summary>Installs the rethrowing error handler until the returned scope is disposed.</summary>
    public static RenderErrorRethrowScope Enter() => new();

    public void Dispose() => FlutterError.OnError = _previous;
}
