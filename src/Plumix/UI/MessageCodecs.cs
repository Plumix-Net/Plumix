using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Plumix.Foundation;

namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/services/message_codecs.dart

/// <summary>[MessageCodec] with unencoded binary messages represented using <see cref="ByteData"/>.</summary>
/// <remarks>
/// On Android, messages will be represented using <c>java.nio.ByteBuffer</c>. On iOS, messages will be
/// represented using <c>NSData</c>.
/// </remarks>
public class BinaryCodec : MessageCodec<ByteData?>
{
    public override ByteData? DecodeMessage(ByteData? message) => message;

    public override ByteData? EncodeMessage(ByteData? message) => message;
}

/// <summary>[MessageCodec] with UTF-8 encoded String messages.</summary>
public class StringCodec : MessageCodec<string?>
{
    internal static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public override string? DecodeMessage(ByteData? message)
    {
        if (message is null)
        {
            return null;
        }

        return Utf8.GetString(message.ToUint8List());
    }

    public override ByteData? EncodeMessage(string? message)
    {
        if (message is null)
        {
            return null;
        }

        return ByteData.SublistView(Utf8.GetBytes(message));
    }
}

/// <summary>[MessageCodec] with UTF-8 encoded JSON messages.</summary>
/// <remarks>
/// Supported messages are acyclic values of these forms: <c>null</c>, <c>bool</c>, <c>int</c>,
/// <c>long</c>, <c>double</c>, <c>string</c>, lists of supported values, maps from strings to
/// supported values.
/// </remarks>
public class JsonMessageCodec : MessageCodec<object?>
{
    private static readonly StringCodec Strings = new StringCodec();

    public override ByteData? EncodeMessage(object? message)
    {
        if (message is null)
        {
            return null;
        }

        return Strings.EncodeMessage(Json.Encode(message));
    }

    public override object? DecodeMessage(ByteData? message)
    {
        if (message is null)
        {
            return null;
        }

        return Json.Decode(Strings.DecodeMessage(message)!);
    }
}

/// <summary>[MethodCodec] with UTF-8 encoded JSON method calls and result envelopes.</summary>
/// <remarks>
/// Values supported as method arguments and result payloads are those supported by
/// <see cref="JsonMessageCodec"/>. A method call is encoded as a two-entry map with the method name
/// keyed by <c>method</c> and the arguments keyed by <c>args</c>. A reply envelope is a one-element
/// list for success and a three-element list (code, message, details) for failure.
/// </remarks>
public class JsonMethodCodec : MethodCodec
{
    private static readonly JsonMessageCodec Messages = new JsonMessageCodec();

    public override ByteData EncodeMethodCall(MethodCall methodCall)
    {
        ArgumentNullException.ThrowIfNull(methodCall);
        return Messages.EncodeMessage(new Dictionary<string, object?>
        {
            ["method"] = methodCall.Method,
            ["args"] = methodCall.Arguments,
        })!;
    }

    public override MethodCall DecodeMethodCall(ByteData? methodCall)
    {
        object? decoded = Messages.DecodeMessage(methodCall);
        if (decoded is not IDictionary map)
        {
            throw new FormatException($"Expected method call Map, got {DartString.Of(decoded)}");
        }

        object? method = map["method"];
        if (method is string name)
        {
            return new MethodCall(name, map["args"]);
        }

        throw new FormatException($"Invalid method call: {DartString.Of(decoded)}");
    }

    public override object? DecodeEnvelope(ByteData envelope)
    {
        object? decoded = Messages.DecodeMessage(envelope);
        if (decoded is not IList list)
        {
            throw new FormatException($"Expected envelope List, got {DartString.Of(decoded)}");
        }

        if (list.Count == 1)
        {
            return list[0];
        }

        if (list.Count == 3 && list[0] is string && (list[1] is null || list[1] is string))
        {
            throw new PlatformException((string)list[0]!, list[1] as string, list[2]);
        }

        if (list.Count == 4
            && list[0] is string
            && (list[1] is null || list[1] is string)
            && (list[3] is null || list[3] is string))
        {
            throw new PlatformException((string)list[0]!, list[1] as string, list[2], list[3] as string);
        }

        throw new FormatException($"Invalid envelope: {DartString.Of(decoded)}");
    }

    public override ByteData EncodeSuccessEnvelope(object? result)
    {
        return Messages.EncodeMessage(new List<object?> { result })!;
    }

    public override ByteData EncodeErrorEnvelope(string code, string? message = null, object? details = null)
    {
        ArgumentNullException.ThrowIfNull(code);
        return Messages.EncodeMessage(new List<object?> { code, message, details })!;
    }
}

/// <summary>[MessageCodec] using the Flutter standard binary encoding.</summary>
/// <remarks>
/// Supported messages are acyclic values of these forms: <c>null</c>, <c>bool</c>, <c>int</c>,
/// <c>long</c>, <c>double</c>, <c>string</c>, <c>byte[]</c>, <c>int[]</c>, <c>long[]</c>,
/// <c>float[]</c>, <c>double[]</c>, lists of supported values, maps from supported values to
/// supported values.
/// <para>
/// The type discriminators and the size encoding are byte-compatible with Flutter's own
/// <c>StandardMessageCodec</c>, so a message written here decodes unchanged in Dart.
/// </para>
/// </remarks>
public class StandardMessageCodec : MessageCodec<object?>
{
    /// <summary>The capacity Flutter starts every standard-codec write buffer with.</summary>
    protected const int WriteBufferStartCapacity = 64;

    protected const int ValueNull = 0;
    protected const int ValueTrue = 1;
    protected const int ValueFalse = 2;
    protected const int ValueInt32 = 3;
    protected const int ValueInt64 = 4;
    protected const int ValueLargeInt = 5;
    protected const int ValueFloat64 = 6;
    protected const int ValueString = 7;
    protected const int ValueUint8List = 8;
    protected const int ValueInt32List = 9;
    protected const int ValueInt64List = 10;
    protected const int ValueFloat64List = 11;
    protected const int ValueList = 12;
    protected const int ValueMap = 13;
    protected const int ValueFloat32List = 14;

    public override ByteData? EncodeMessage(object? message)
    {
        if (message is null)
        {
            return null;
        }

        var buffer = new WriteBuffer(startCapacity: WriteBufferStartCapacity);
        WriteValue(buffer, message);
        return buffer.Done();
    }

    public override object? DecodeMessage(ByteData? message)
    {
        if (message is null)
        {
            return null;
        }

        var buffer = new ReadBuffer(message);
        object? result = ReadValue(buffer);
        if (buffer.HasRemaining)
        {
            throw new FormatException("Message corrupted");
        }

        return result;
    }

    /// <summary>Writes <paramref name="value"/> to <paramref name="buffer"/> by first writing a type
    /// discriminator byte, then the value itself.</summary>
    public virtual void WriteValue(WriteBuffer buffer, object? value)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        switch (value)
        {
            case null:
                buffer.PutUint8(ValueNull);
                return;
            case bool boolValue:
                buffer.PutUint8(boolValue ? (byte)ValueTrue : (byte)ValueFalse);
                return;
            case double doubleValue:
                buffer.PutUint8(ValueFloat64);
                buffer.PutFloat64(doubleValue);
                return;
            case int intValue:
                buffer.PutUint8(ValueInt32);
                buffer.PutInt32(intValue);
                return;
            case long longValue:
                if (longValue >= int.MinValue && longValue <= int.MaxValue)
                {
                    buffer.PutUint8(ValueInt32);
                    buffer.PutInt32((int)longValue);
                }
                else
                {
                    buffer.PutUint8(ValueInt64);
                    buffer.PutInt64(longValue);
                }

                return;
            case string stringValue:
                buffer.PutUint8(ValueString);
                byte[] bytes = StringCodec.Utf8.GetBytes(stringValue);
                WriteSize(buffer, bytes.Length);
                buffer.PutUint8List(bytes);
                return;
            case byte[] uint8List:
                buffer.PutUint8(ValueUint8List);
                WriteSize(buffer, uint8List.Length);
                buffer.PutUint8List(uint8List);
                return;
            case int[] int32List:
                buffer.PutUint8(ValueInt32List);
                WriteSize(buffer, int32List.Length);
                buffer.PutInt32List(int32List);
                return;
            case long[] int64List:
                buffer.PutUint8(ValueInt64List);
                WriteSize(buffer, int64List.Length);
                buffer.PutInt64List(int64List);
                return;
            case float[] float32List:
                buffer.PutUint8(ValueFloat32List);
                WriteSize(buffer, float32List.Length);
                buffer.PutFloat32List(float32List);
                return;
            case double[] float64List:
                buffer.PutUint8(ValueFloat64List);
                WriteSize(buffer, float64List.Length);
                buffer.PutFloat64List(float64List);
                return;
            case IDictionary map:
                buffer.PutUint8(ValueMap);
                WriteSize(buffer, map.Count);
                foreach (DictionaryEntry entry in map)
                {
                    WriteValue(buffer, entry.Key);
                    WriteValue(buffer, entry.Value);
                }

                return;
            case IList list:
                buffer.PutUint8(ValueList);
                WriteSize(buffer, list.Count);
                foreach (object? item in list)
                {
                    WriteValue(buffer, item);
                }

                return;
            default:
                throw new ArgumentException($"Invalid argument(s): {value}", nameof(value));
        }
    }

    /// <summary>Reads a value from <paramref name="buffer"/> as written by <see cref="WriteValue"/>.</summary>
    public virtual object? ReadValue(ReadBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (!buffer.HasRemaining)
        {
            throw new FormatException("Message corrupted");
        }

        int type = buffer.GetUint8();
        return ReadValueOfType(type, buffer);
    }

    /// <summary>Reads a value of the indicated <paramref name="type"/> from <paramref name="buffer"/>.</summary>
    public virtual object? ReadValueOfType(int type, ReadBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        switch (type)
        {
            case ValueNull:
                return null;
            case ValueTrue:
                return true;
            case ValueFalse:
                return false;
            case ValueInt32:
                return buffer.GetInt32();
            case ValueInt64:
                return buffer.GetInt64();
            case ValueFloat64:
                return buffer.GetFloat64();
            case ValueLargeInt:
            case ValueString:
                return StringCodec.Utf8.GetString(buffer.GetUint8List(ReadSize(buffer)));
            case ValueUint8List:
                return buffer.GetUint8List(ReadSize(buffer));
            case ValueInt32List:
                return buffer.GetInt32List(ReadSize(buffer));
            case ValueInt64List:
                return buffer.GetInt64List(ReadSize(buffer));
            case ValueFloat32List:
                return buffer.GetFloat32List(ReadSize(buffer));
            case ValueFloat64List:
                return buffer.GetFloat64List(ReadSize(buffer));
            case ValueList:
            {
                int length = ReadSize(buffer);
                var result = new List<object?>(length);
                for (int i = 0; i < length; i++)
                {
                    result.Add(ReadValue(buffer));
                }

                return result;
            }

            case ValueMap:
            {
                int length = ReadSize(buffer);
                var result = new Dictionary<object, object?>(length);
                for (int i = 0; i < length; i++)
                {
                    object? key = ReadValue(buffer);
                    object? entryValue = ReadValue(buffer);
                    if (key is null)
                    {
                        // Dart's `Map<Object?, Object?>` accepts a null key; .NET dictionaries do not.
                        // See `docs/ai/DIVERGENCES.md`.
                        throw new FormatException("Message corrupted: null is not a valid map key.");
                    }

                    result[key] = entryValue;
                }

                return result;
            }

            default:
                throw new FormatException("Message corrupted");
        }
    }

    /// <summary>Writes a non-negative 32-bit integer <paramref name="value"/> to
    /// <paramref name="buffer"/> using an expanding 1-5 byte encoding.</summary>
    protected static void WriteSize(WriteBuffer buffer, int value)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        if (value < 254)
        {
            buffer.PutUint8((byte)value);
        }
        else if (value <= 0xffff)
        {
            buffer.PutUint8(254);
            buffer.PutUint16((ushort)value);
        }
        else
        {
            buffer.PutUint8(255);
            buffer.PutUint32((uint)value);
        }
    }

    /// <summary>Reads a non-negative int from <paramref name="buffer"/> as written by
    /// <see cref="WriteSize"/>.</summary>
    protected static int ReadSize(ReadBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        byte value = buffer.GetUint8();
        return value switch
        {
            254 => buffer.GetUint16(),
            255 => checked((int)buffer.GetUint32()),
            _ => value,
        };
    }
}

/// <summary>[MethodCodec] using the Flutter standard binary encoding.</summary>
/// <remarks>
/// The standard codec is guaranteed to be compatible with the corresponding standard codec for
/// FlutterMethodChannels on the platform side.
/// </remarks>
public class StandardMethodCodec : MethodCodec
{
    /// <summary>Creates a <see cref="MethodCodec"/> using the Flutter standard binary encoding.</summary>
    public StandardMethodCodec(StandardMessageCodec? messageCodec = null)
    {
        MessageCodec = messageCodec ?? new StandardMessageCodec();
    }

    /// <summary>The message codec that this method codec uses for encoding values.</summary>
    public StandardMessageCodec MessageCodec { get; }

    public override ByteData EncodeMethodCall(MethodCall methodCall)
    {
        ArgumentNullException.ThrowIfNull(methodCall);
        var buffer = new WriteBuffer(startCapacity: 64);
        MessageCodec.WriteValue(buffer, methodCall.Method);
        MessageCodec.WriteValue(buffer, methodCall.Arguments);
        return buffer.Done();
    }

    public override MethodCall DecodeMethodCall(ByteData? methodCall)
    {
        ArgumentNullException.ThrowIfNull(methodCall);
        var buffer = new ReadBuffer(methodCall);
        object? method = MessageCodec.ReadValue(buffer);
        object? arguments = MessageCodec.ReadValue(buffer);
        if (method is string name && !buffer.HasRemaining)
        {
            return new MethodCall(name, arguments);
        }

        throw new FormatException("Invalid method call");
    }

    public override ByteData EncodeSuccessEnvelope(object? result)
    {
        var buffer = new WriteBuffer(startCapacity: 64);
        buffer.PutUint8(0);
        MessageCodec.WriteValue(buffer, result);
        return buffer.Done();
    }

    public override ByteData EncodeErrorEnvelope(string code, string? message = null, object? details = null)
    {
        ArgumentNullException.ThrowIfNull(code);
        var buffer = new WriteBuffer(startCapacity: 64);
        buffer.PutUint8(1);
        MessageCodec.WriteValue(buffer, code);
        MessageCodec.WriteValue(buffer, message);
        MessageCodec.WriteValue(buffer, details);
        return buffer.Done();
    }

    public override object? DecodeEnvelope(ByteData envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if (envelope.LengthInBytes == 0)
        {
            throw new FormatException("Expected envelope, got nothing");
        }

        var buffer = new ReadBuffer(envelope);
        if (buffer.GetUint8() == 0)
        {
            return MessageCodec.ReadValue(buffer);
        }

        object? errorCode = MessageCodec.ReadValue(buffer);
        object? errorMessage = MessageCodec.ReadValue(buffer);
        object? errorDetails = MessageCodec.ReadValue(buffer);
        string? errorStacktrace = buffer.HasRemaining ? (string?)MessageCodec.ReadValue(buffer) : null;
        if (errorCode is string code && (errorMessage is null || errorMessage is string) && !buffer.HasRemaining)
        {
            throw new PlatformException(code, errorMessage as string, errorDetails, errorStacktrace);
        }

        throw new FormatException("Invalid envelope");
    }
}

/// <summary>
/// C#-only helper: the JSON text shape Dart's <c>dart:convert</c> produces and consumes, so
/// <see cref="JsonMessageCodec"/> stays wire-compatible with Flutter's <c>JSONMessageCodec</c>.
/// </summary>
internal static class Json
{
    public static string Encode(object? value)
    {
        var builder = new StringBuilder();
        Write(builder, value);
        return builder.ToString();
    }

    public static object? Decode(string source)
    {
        try
        {
            using var document = JsonDocument.Parse(source, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
            });
            return Convert(document.RootElement);
        }
        catch (JsonException exception)
        {
            throw new FormatException(exception.Message, exception);
        }
    }

    private static void Write(StringBuilder builder, object? value)
    {
        switch (value)
        {
            case null:
                builder.Append("null");
                return;
            case bool boolValue:
                builder.Append(boolValue ? "true" : "false");
                return;
            case string stringValue:
                WriteString(builder, stringValue);
                return;
            case int intValue:
                builder.Append(intValue.ToString(CultureInfo.InvariantCulture));
                return;
            case long longValue:
                builder.Append(longValue.ToString(CultureInfo.InvariantCulture));
                return;
            case double doubleValue:
                WriteDouble(builder, doubleValue);
                return;
            case IDictionary map:
                WriteMap(builder, map);
                return;
            case IList list:
                builder.Append('[');
                for (int i = 0; i < list.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(',');
                    }

                    Write(builder, list[i]);
                }

                builder.Append(']');
                return;
            default:
                throw new ArgumentException($"Converting object to an encodable object failed: {value}");
        }
    }

    private static void WriteMap(StringBuilder builder, IDictionary map)
    {
        builder.Append('{');
        bool first = true;
        foreach (DictionaryEntry entry in map)
        {
            if (entry.Key is not string key)
            {
                throw new ArgumentException(
                    $"Converting object to an encodable object failed: {DartString.Of(entry.Key)}");
            }

            if (!first)
            {
                builder.Append(',');
            }

            first = false;
            WriteString(builder, key);
            builder.Append(':');
            Write(builder, entry.Value);
        }

        builder.Append('}');
    }

    private static void WriteDouble(StringBuilder builder, double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentException($"Converting object to an encodable object failed: {value}");
        }

        string text = value.ToString("R", CultureInfo.InvariantCulture);
        builder.Append(text);
        if (text.IndexOfAny(['.', 'e', 'E']) < 0)
        {
            // Dart prints every double with a fractional part; `1.0` must not serialize as `1`.
            builder.Append(".0");
        }
    }

    private static void WriteString(StringBuilder builder, string value)
    {
        builder.Append('"');
        foreach (char character in value)
        {
            switch (character)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (character < 0x20)
                    {
                        builder.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(character);
                    }

                    break;
            }
        }

        builder.Append('"');
    }

    private static object? Convert(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return null;
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.Number:
                if (element.TryGetInt32(out int intValue))
                {
                    return intValue;
                }

                if (element.TryGetInt64(out long longValue))
                {
                    return longValue;
                }

                return element.GetDouble();
            case JsonValueKind.Array:
            {
                var list = new List<object?>();
                foreach (JsonElement item in element.EnumerateArray())
                {
                    list.Add(Convert(item));
                }

                return list;
            }

            default:
            {
                var map = new Dictionary<string, object?>();
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    map[property.Name] = Convert(property.Value);
                }

                return map;
            }
        }
    }
}
