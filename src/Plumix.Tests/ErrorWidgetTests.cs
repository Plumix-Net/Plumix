using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Ported from flutter/packages/flutter/test/widgets/error_widget_test.dart and the
// `RenderErrorBox` contract in flutter/packages/flutter/lib/src/rendering/error.dart.

namespace Plumix.Tests;

public sealed class ErrorWidgetTests
{
    /// <remarks>
    /// `RenderErrorBox` is sized by its parent, tries to be 100000 logical pixels each way when the
    /// constraints are unbounded, and hit-tests itself.
    /// </remarks>
    [Fact]
    public void RenderErrorBox_MatchesDartsSizingContract()
    {
        var box = new RenderErrorBox("boom");

        Assert.Equal("boom", box.Message);
        Assert.Equal(100000.0, box.GetMaxIntrinsicWidth(double.PositiveInfinity));
        Assert.Equal(100000.0, box.GetMaxIntrinsicHeight(double.PositiveInfinity));
        Assert.Equal(
            new Size(100000.0, 100000.0),
            box.GetDryLayout(new BoxConstraints(
                MaxWidth: double.PositiveInfinity,
                MaxHeight: double.PositiveInfinity)));
        Assert.Equal(new Size(80, 40), box.GetDryLayout(BoxConstraints.Loose(new Size(80, 40))));
    }

    /// <remarks>
    /// `ErrorWidget(exception)` stringifies the exception and always takes a fresh `UniqueKey`, so
    /// two error widgets for the same exception never reuse each other's element.
    /// </remarks>
    [Fact]
    public void ErrorWidget_StringifiesTheExceptionUnderAUniqueKey()
    {
        var exception = new InvalidOperationException("kaboom");
        var first = new ErrorWidget(exception);
        var second = new ErrorWidget(exception);

        Assert.Contains("kaboom", first.Message);
        Assert.IsType<UniqueKey>(first.Key);
        Assert.NotEqual(first.Key, second.Key);

        var box = Assert.IsType<RenderErrorBox>(first.CreateRenderObject(null!));
        Assert.Equal(first.Message, box.Message);
    }

    /// <remarks>
    /// `ErrorWidget.withDetails` takes the message verbatim, and the default `ErrorWidget.builder`
    /// shows the exception's message in debug mode.
    /// </remarks>
    [Fact]
    public void ErrorWidgetBuilder_DefaultsToTheExceptionMessage()
    {
        Assert.Equal(string.Empty, ErrorWidget.WithDetails().Message);
        Assert.Equal("explicit", ErrorWidget.WithDetails("explicit").Message);

        var built = Assert.IsType<ErrorWidget>(
            ErrorWidget.Builder(new FlutterErrorDetails(new InvalidOperationException("kaboom"))));
        Assert.Equal(Constants.KDebugMode, built.Message.Contains("kaboom", StringComparison.Ordinal));
    }
}
