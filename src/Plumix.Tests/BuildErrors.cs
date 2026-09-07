using Plumix.Foundation;
using Xunit;

namespace Plumix.Tests;

/// <summary>
/// The test-side counterpart of Flutter's <c>WidgetTester.takeException()</c>.
///
/// Dart's <c>ComponentElement.performRebuild</c> never lets a failing <c>build()</c> escape: it
/// reports the error through <see cref="FlutterError.ReportError"/> and swaps an
/// <c>ErrorWidget</c> into the tree. A test that wants to assert on such an error therefore has to
/// collect what was reported instead of catching what was thrown.
/// </summary>
internal static class BuildErrors
{
    /// <summary>
    /// Runs <paramref name="action"/> with the reported-error stream captured, and returns the first
    /// failure — whichever of the two ways it surfaced: thrown out of <paramref name="action"/>, or
    /// reported to <see cref="FlutterError.OnError"/> from inside a build.
    /// </summary>
    public static Exception TakeException(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var reported = new List<FlutterErrorDetails>();
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = reported.Add;
        Exception? thrown = null;
        try
        {
            action();
        }
        catch (Exception exception)
        {
            thrown = exception;
        }
        finally
        {
            FlutterError.OnError = previous;
        }

        if (thrown is not null)
        {
            return thrown;
        }

        Assert.NotEmpty(reported);
        return reported[0].Exception as Exception
               ?? new InvalidOperationException(reported[0].Exception.ToString());
    }

    /// <summary>Asserts the failure is exactly <typeparamref name="T"/> and returns it.</summary>
    public static T Throws<T>(Action action) where T : Exception => Assert.IsType<T>(TakeException(action));

    /// <summary>Asserts the failure is a <typeparamref name="T"/> or a subtype, and returns it.</summary>
    public static T ThrowsAny<T>(Action action) where T : Exception
        => Assert.IsAssignableFrom<T>(TakeException(action));
}
