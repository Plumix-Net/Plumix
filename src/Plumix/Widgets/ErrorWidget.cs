using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart (ErrorWidget)

namespace Plumix.Widgets;

/// <summary>
/// Signature for the constructor that is called when an error occurs while building a widget.
/// </summary>
/// <remarks>Dart's <c>ErrorWidgetBuilder</c>.</remarks>
public delegate Widget ErrorWidgetBuilder(FlutterErrorDetails details);

/// <summary>
/// A widget that renders an exception's message.
/// </summary>
/// <remarks>
/// Dart's <c>ErrorWidget</c>. Used when a build method fails, to help with determining where the
/// problem lies. Exceptions are also reported through <see cref="FlutterError.ReportError"/>.
/// </remarks>
public sealed class ErrorWidget : LeafRenderObjectWidget
{
    /// <summary>
    /// Creates a widget that displays the given exception. The message is the stringification of the
    /// exception, unless computing that value itself throws, in which case it is <c>"Error"</c>.
    /// </summary>
    public ErrorWidget(object exception) : base(new UniqueKey())
    {
        Message = Stringify(exception);
        _flutterError = exception as FlutterError;
    }

    private ErrorWidget(string message, FlutterError? error) : base(new UniqueKey())
    {
        Message = message;
        _flutterError = error;
    }

    private readonly FlutterError? _flutterError;

    /// <summary>
    /// Creates a widget that displays the given error message, with an explicit
    /// <see cref="FlutterError"/> to report to inspection tools. It need not match the message.
    /// </summary>
    /// <remarks>Dart's <c>ErrorWidget.withDetails</c>.</remarks>
    public static ErrorWidget WithDetails(string message = "", FlutterError? error = null)
    {
        return new ErrorWidget(message, error);
    }

    /// <summary>The configurable factory for <see cref="ErrorWidget"/>.</summary>
    /// <remarks>
    /// Dart's <c>ErrorWidget.builder</c>. When an error occurs while building a widget, the broken
    /// widget is replaced by the widget this returns. The system is typically in an unstable state
    /// when it is called, so the widget it returns should do the least amount of work possible — a
    /// <see cref="LeafRenderObjectWidget"/> over a render box that survives absurd incoming
    /// constraints is the best choice. The default shows the exception's message in debug mode and
    /// nothing but a gray background in release builds.
    /// </remarks>
    public static ErrorWidgetBuilder Builder { get; set; } = DefaultErrorWidgetBuilder;

    /// <summary>The message to display.</summary>
    public string Message { get; }

    public override RenderObject CreateRenderObject(BuildContext context) => new RenderErrorBox(Message);

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        if (_flutterError is null)
        {
            properties.Add(new StringProperty("message", Message, quoted: false));
        }
        else
        {
            properties.Add(_flutterError.ToDiagnosticsNode(style: DiagnosticsTreeStyle.Whitespace));
        }
    }

    private static Widget DefaultErrorWidgetBuilder(FlutterErrorDetails details)
    {
        string message = string.Empty;
        if (Constants.KDebugMode)
        {
            message = $"{Stringify(details.Exception)}\nSee also: https://docs.flutter.dev/testing/errors";
        }

        return WithDetails(message, details.Exception as FlutterError);
    }

    private static string Stringify(object? exception)
    {
        try
        {
            return exception?.ToString() ?? "Error";
        }
        catch (Exception)
        {
            // If we get here, it means things have really gone off the rails, and we're better off
            // just returning a simple string and letting the developer find out what the root cause
            // of all their problems is by looking at the console logs.
        }

        return "Error";
    }
}
