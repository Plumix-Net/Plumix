using Plumix.Foundation;

namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/services/message_codec.dart

/// <summary>A message encoding/decoding mechanism.</summary>
/// <remarks>
/// Both operations throw an exception if conversion fails. Such situations should be treated as
/// programming errors.
/// </remarks>
public abstract class MessageCodec<T>
{
    /// <summary>Encodes the specified <paramref name="message"/> in binary.</summary>
    /// <remarks>Returns <c>null</c> if <paramref name="message"/> is <c>null</c>.</remarks>
    public abstract ByteData? EncodeMessage(T message);

    /// <summary>Decodes the specified <paramref name="message"/> from binary.</summary>
    /// <remarks>Returns <c>null</c> if <paramref name="message"/> is <c>null</c>.</remarks>
    public abstract T? DecodeMessage(ByteData? message);
}

/// <summary>A command object representing the invocation of a named method.</summary>
public class MethodCall
{
    /// <summary>Creates a <see cref="MethodCall"/> representing the invocation of
    /// <paramref name="method"/> with the specified <paramref name="arguments"/>.</summary>
    public MethodCall(string method, object? arguments = null)
    {
        ArgumentNullException.ThrowIfNull(method);
        Method = method;
        Arguments = arguments;
    }

    /// <summary>The name of the method to be called.</summary>
    public string Method { get; }

    /// <summary>The arguments for the method.</summary>
    /// <remarks>Must be a valid value for the <see cref="MethodCodec"/> used.</remarks>
    public object? Arguments { get; }

    public override string ToString() => $"{nameof(MethodCall)}({Method}, {DartString.Of(Arguments)})";
}

/// <summary>A codec for method calls and enveloped results.</summary>
/// <remarks>
/// All operations throw an exception if conversion fails.
/// </remarks>
public abstract class MethodCodec
{
    /// <summary>Encodes the specified <paramref name="methodCall"/> into binary.</summary>
    public abstract ByteData EncodeMethodCall(MethodCall methodCall);

    /// <summary>Decodes the specified <paramref name="methodCall"/> from binary.</summary>
    public abstract MethodCall DecodeMethodCall(ByteData? methodCall);

    /// <summary>Decodes the specified result <paramref name="envelope"/> from binary.</summary>
    /// <remarks>Throws <see cref="PlatformException"/> if <paramref name="envelope"/> represents an error.</remarks>
    public abstract object? DecodeEnvelope(ByteData envelope);

    /// <summary>Encodes a successful <paramref name="result"/> into a binary envelope.</summary>
    public abstract ByteData EncodeSuccessEnvelope(object? result);

    /// <summary>Encodes an error result into a binary envelope.</summary>
    /// <param name="code">An error code string.</param>
    /// <param name="message">A human-readable error message.</param>
    /// <param name="details">Error details, possibly <c>null</c>.</param>
    public abstract ByteData EncodeErrorEnvelope(string code, string? message = null, object? details = null);
}

/// <summary>Thrown to indicate that a platform interaction failed in the platform plugin.</summary>
public class PlatformException : Exception
{
    /// <summary>Creates a <see cref="PlatformException"/> with the specified error
    /// <paramref name="code"/> and optional details.</summary>
    public PlatformException(
        string code,
        string? message = null,
        object? details = null,
        string? stacktrace = null)
        : base(Describe(code, message, details, stacktrace))
    {
        ArgumentNullException.ThrowIfNull(code);
        Code = code;
        ErrorMessage = message;
        Details = details;
        Stacktrace = stacktrace;
    }

    /// <summary>An error code.</summary>
    public string Code { get; }

    /// <summary>
    /// A human-readable error message, possibly <c>null</c>. Dart parity source:
    /// <c>PlatformException.message</c>; <c>Exception.Message</c> is already taken by .NET and cannot be
    /// nullable, so the Dart field lives here.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>Error details, possibly <c>null</c>.</summary>
    public object? Details { get; }

    /// <summary>Native stacktrace for the error, possibly <c>null</c>.</summary>
    public string? Stacktrace { get; }

    public override string ToString() =>
        $"{nameof(PlatformException)}({Describe(Code, ErrorMessage, Details, Stacktrace)})";

    private static string Describe(string code, string? message, object? details, string? stacktrace) =>
        $"{code}, {DartString.Of(message)}, {DartString.Of(details)}, {DartString.Of(stacktrace)}";
}

/// <summary>Thrown to indicate that a platform interaction failed to find a handling plugin.</summary>
public class MissingPluginException : Exception
{
    /// <summary>Creates a <see cref="MissingPluginException"/> with an optional human-readable
    /// error message.</summary>
    public MissingPluginException(string? message = null)
        : base(message ?? "null")
    {
        ErrorMessage = message;
    }

    /// <summary>
    /// A human-readable error message, possibly <c>null</c>. Dart parity source:
    /// <c>MissingPluginException.message</c>.
    /// </summary>
    public string? ErrorMessage { get; }

    public override string ToString() => $"{nameof(MissingPluginException)}({DartString.Of(ErrorMessage)})";
}

/// <summary>
/// C#-only helper: Dart's string interpolation prints <c>null</c> where .NET prints an empty string, and
/// every <c>toString</c> in this layer is asserted verbatim by Flutter's tests.
/// </summary>
internal static class DartString
{
    public static string Of(object? value) => value?.ToString() ?? "null";
}
