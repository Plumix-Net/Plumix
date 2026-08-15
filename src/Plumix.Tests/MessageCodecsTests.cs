using System.Collections;
using Plumix.Foundation;
using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

/// <summary>
/// Ports Flutter's <c>test/services/message_codecs_test.dart</c> and
/// <c>message_codecs_vm_test.dart</c>. The byte expectations assume a little-endian host, exactly like
/// Flutter's own tests.
/// </summary>
public sealed class MessageCodecsTests
{
    // ------------------------------------------------------------ binary codec

    [Fact]
    public void BinaryCodec_EncodesAndDecodesSimpleMessages()
    {
        var codec = new BinaryCodec();
        Assert.Null(codec.EncodeMessage(null));
        Assert.Null(codec.DecodeMessage(null));

        var empty = new ByteData(0);
        Assert.Same(empty, codec.EncodeMessage(empty));
        Assert.Same(empty, codec.DecodeMessage(empty));

        var data = new ByteData(4);
        data.SetInt32(0, -7);
        Assert.Same(data, codec.DecodeMessage(codec.EncodeMessage(data)));
    }

    // ------------------------------------------------------------ string codec

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("special chars >☺😂<")]
    public void StringCodec_EncodesAndDecodesSimpleMessages(string message)
    {
        var codec = new StringCodec();
        Assert.Equal(message, codec.DecodeMessage(codec.EncodeMessage(message)));
    }

    [Fact]
    public void StringCodec_DecodesNullAsNull()
    {
        var codec = new StringCodec();
        Assert.Null(codec.EncodeMessage(null));
        Assert.Null(codec.DecodeMessage(null));
    }

    [Fact]
    public void StringCodec_HonorsTheOffsetOfAByteDataView()
    {
        var codec = new StringCodec();
        ByteData full = codec.EncodeMessage("hello world")!;
        int offset = codec.EncodeMessage("hello")!.LengthInBytes;
        ByteData view = ByteData.View(full.Buffer, offset, full.LengthInBytes - offset);

        Assert.Equal(" world", codec.DecodeMessage(view));
    }

    // -------------------------------------------------------- JSON message codec

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(7)]
    [InlineData(-7)]
    [InlineData(98742923489L)]
    [InlineData(-98742923489L)]
    [InlineData(3.14)]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("special chars >☺😂<")]
    public void JsonMessageCodec_EncodesAndDecodesSimpleMessages(object? message)
    {
        CheckEncodeDecode(new JsonMessageCodec(), message);
    }

    [Fact]
    public void JsonMessageCodec_EncodesAndDecodesCompositeMessages()
    {
        var message = new List<object?>
        {
            null,
            true,
            false,
            -707,
            -7000000007L,
            -3.14,
            string.Empty,
            "hello",
            new List<object?> { "nested", new List<object?>() },
            new Dictionary<string, object?> { ["a"] = "nested", ["b"] = new Dictionary<string, object?>() },
            "world",
        };

        CheckEncodeDecode(new JsonMessageCodec(), message);
    }

    [Fact]
    public void JsonMessageCodec_WritesDartShapedJson()
    {
        var codec = new JsonMessageCodec();
        var message = new Dictionary<string, object?>
        {
            ["int"] = 7,
            ["double"] = 1.0,
            ["text"] = "a\"b\\c\nd",
            ["list"] = new List<object?> { 1, null, true },
        };

        Assert.Equal(
            "{\"int\":7,\"double\":1.0,\"text\":\"a\\\"b\\\\c\\nd\",\"list\":[1,null,true]}",
            new StringCodec().DecodeMessage(codec.EncodeMessage(message)));
    }

    // --------------------------------------------------------- JSON method codec

    [Fact]
    public void JsonMethodCodec_EncodesAndDecodesMethodCalls()
    {
        var codec = new JsonMethodCodec();
        MethodCall call = codec.DecodeMethodCall(codec.EncodeMethodCall(new MethodCall("sayHello", "hello")));

        Assert.Equal("sayHello", call.Method);
        Assert.Equal("hello", call.Arguments);
    }

    [Fact]
    public void JsonMethodCodec_DecodesErrorEnvelopeWithoutNativeStacktrace()
    {
        var codec = new JsonMethodCodec();
        var exception = Assert.Throws<PlatformException>(() =>
            codec.DecodeEnvelope(codec.EncodeErrorEnvelope("errorCode", "errorMessage", "errorDetails")));

        Assert.Equal("errorCode", exception.Code);
        Assert.Equal("errorMessage", exception.ErrorMessage);
        Assert.Equal("errorDetails", exception.Details);
        Assert.Null(exception.Stacktrace);
    }

    [Fact]
    public void JsonMethodCodec_DecodesErrorEnvelopeWithNativeStacktrace()
    {
        var codec = new JsonMethodCodec();
        ByteData envelope = new StringCodec()
            .EncodeMessage("[\"errorCode\",\"errorMessage\",\"errorDetails\",\"errorStacktrace\"]")!;

        var exception = Assert.Throws<PlatformException>(() => codec.DecodeEnvelope(envelope));
        Assert.Equal("errorStacktrace", exception.Stacktrace);
    }

    [Fact]
    public void JsonMethodCodec_AllowsANullErrorMessage()
    {
        var codec = new JsonMethodCodec();
        var exception = Assert.Throws<PlatformException>(() =>
            codec.DecodeEnvelope(codec.EncodeErrorEnvelope("errorCode", details: "errorDetails")));

        Assert.Equal("errorCode", exception.Code);
        Assert.Null(exception.ErrorMessage);
        Assert.Equal("errorDetails", exception.Details);
    }

    [Fact]
    public void JsonMethodCodec_RejectsMalformedCallsAndEnvelopes()
    {
        var codec = new JsonMethodCodec();
        var strings = new StringCodec();

        Assert.Throws<FormatException>(() => codec.DecodeMethodCall(strings.EncodeMessage("[1]")));
        Assert.Throws<FormatException>(() => codec.DecodeMethodCall(strings.EncodeMessage("{\"args\":1}")));
        Assert.Throws<FormatException>(() => codec.DecodeEnvelope(strings.EncodeMessage("{}")!));
        Assert.Throws<FormatException>(() => codec.DecodeEnvelope(strings.EncodeMessage("[1,2]")!));
    }

    [Fact]
    public void JsonMethodCodec_DecodesSuccessEnvelope()
    {
        var codec = new JsonMethodCodec();
        Assert.Equal("hello world", codec.DecodeEnvelope(codec.EncodeSuccessEnvelope("hello world")));
    }

    // ---------------------------------------------------- standard message codec

    [Fact]
    public void StandardMessageCodec_EncodesSizesCorrectlyAtBoundaryCases()
    {
        var codec = new StandardMessageCodec();

        CheckEncoding(codec, new byte[253], Bytes([8, 253], new byte[253]));
        CheckEncoding(codec, new byte[254], Bytes([8, 254, 254, 0], new byte[254]));
        CheckEncoding(codec, new byte[0xffff], Bytes([8, 254, 0xff, 0xff], new byte[0xffff]));
        CheckEncoding(codec, new byte[0xffff + 1], Bytes([8, 255, 0, 0, 1, 0], new byte[0xffff + 1]));
    }

    [Fact]
    public void StandardMessageCodec_AlignsDoublesToEightBytes()
    {
        CheckEncoding(
            new StandardMessageCodec(),
            1.0,
            [6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xf0, 0x3f]);
    }

    [Fact]
    public void StandardMessageCodec_EncodesIntegersCorrectlyAtBoundaryCases()
    {
        var codec = new StandardMessageCodec();

        CheckEncoding(codec, int.MinValue, [3, 0x00, 0x00, 0x00, 0x80]);
        CheckEncoding(codec, int.MinValue - 1L, [4, 0xff, 0xff, 0xff, 0x7f, 0xff, 0xff, 0xff, 0xff]);
        CheckEncoding(codec, int.MaxValue, [3, 0xff, 0xff, 0xff, 0x7f]);
        CheckEncoding(codec, int.MaxValue + 1L, [4, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00]);
        CheckEncoding(codec, long.MinValue, [4, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80]);
        CheckEncoding(codec, long.MaxValue, [4, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x7f]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(7)]
    [InlineData(-7)]
    [InlineData(98742923489L)]
    [InlineData(-98742923489L)]
    [InlineData(3.14)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NaN)]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("special chars >☺😂<")]
    public void StandardMessageCodec_EncodesAndDecodesSimpleMessages(object? message)
    {
        CheckEncodeDecode(new StandardMessageCodec(), message);
    }

    [Fact]
    public void StandardMessageCodec_EncodesAndDecodesCompositeMessages()
    {
        var message = new List<object?>
        {
            null,
            true,
            false,
            -707,
            -7000000007L,
            -3.14,
            string.Empty,
            "hello",
            new byte[] { 0xBA, 0x5E, 0xBA, 0x11 },
            new[] { int.MinValue, 0, int.MaxValue },
            null,

            // Keeps the offset of the following list unaligned, the way Flutter's own test does.
            null,
            new[]
            {
                double.NegativeInfinity,
                -double.MaxValue,
                -double.Epsilon,
                -0.0,
                0.0,
                double.Epsilon,
                double.MaxValue,
                double.PositiveInfinity,
                double.NaN,
            },
            new[]
            {
                float.NegativeInfinity,
                -float.MaxValue,
                -float.Epsilon,
                -0.0f,
                0.0f,
                float.Epsilon,
                float.MaxValue,
                float.PositiveInfinity,
                float.NaN,
            },
            new long[] { long.MinValue, 0, long.MaxValue },
            new List<object?> { "nested", new List<object?>() },
            new Dictionary<object, object?> { ["a"] = "nested", ["b"] = new Dictionary<object, object?>() },
            "world",
        };

        CheckEncodeDecode(new StandardMessageCodec(), message);
    }

    [Fact]
    public void StandardMessageCodec_DecodesMapsWithNonStringKeys()
    {
        var codec = new StandardMessageCodec();
        var message = new Dictionary<object, object?> { ["foo"] = true, [3] = "fizz" };

        var decoded = (IDictionary<object, object?>)codec.DecodeMessage(codec.EncodeMessage(message))!;
        Assert.Equal(true, decoded["foo"]);
        Assert.Equal("fizz", decoded[3]);
    }

    [Fact]
    public void StandardMessageCodec_RejectsCorruptedMessages()
    {
        var codec = new StandardMessageCodec();

        // Trailing bytes after a complete value.
        ByteData encoded = codec.EncodeMessage(7)!;
        byte[] withTrailer = [.. encoded.ToUint8List(), 0];
        Assert.Throws<FormatException>(() => codec.DecodeMessage(ByteData.SublistView(withTrailer)));

        // Unknown type discriminator, and an empty message.
        Assert.Throws<FormatException>(() => codec.DecodeMessage(ByteData.SublistView([127])));
        Assert.Throws<FormatException>(() => codec.DecodeMessage(new ByteData(0)));
    }

    [Fact]
    public void StandardMessageCodec_RejectsUnsupportedValues()
    {
        var codec = new StandardMessageCodec();
        Assert.Throws<ArgumentException>(() => codec.EncodeMessage(new object()));
    }

    [Fact]
    public void StandardMessageCodec_RejectsNullMapKeysOnDecode()
    {
        // Divergence from Dart, which decodes into a null-tolerant `Map<Object?, Object?>`.
        // See `docs/ai/DIVERGENCES.md`.
        var buffer = new WriteBuffer();
        buffer.PutUint8(13);
        buffer.PutUint8(1);
        buffer.PutUint8(0);
        buffer.PutUint8(0);

        Assert.Throws<FormatException>(() => new StandardMessageCodec().DecodeMessage(buffer.Done()));
    }

    // ----------------------------------------------------- standard method codec

    [Fact]
    public void StandardMethodCodec_EncodesAndDecodesMethodCalls()
    {
        var codec = new StandardMethodCodec();
        MethodCall call = codec.DecodeMethodCall(
            codec.EncodeMethodCall(new MethodCall("sayHello", new List<object?> { "hello", 7 })));

        Assert.Equal("sayHello", call.Method);
        Assert.Equal(new List<object?> { "hello", 7 }, (IEnumerable<object?>)call.Arguments!);
    }

    [Fact]
    public void StandardMethodCodec_DecodesObjectsProducedFromTheCodec()
    {
        var codec = new StandardMethodCodec();
        var message = new Dictionary<object, object?> { ["foo"] = true, [3] = "fizz" };

        var decoded = (IDictionary<object, object?>)codec.DecodeEnvelope(codec.EncodeSuccessEnvelope(message))!;
        Assert.Equal(true, decoded["foo"]);
        Assert.Equal("fizz", decoded[3]);
    }

    [Fact]
    public void StandardMethodCodec_DecodesErrorEnvelopeWithoutNativeStacktrace()
    {
        var codec = new StandardMethodCodec();
        var exception = Assert.Throws<PlatformException>(() =>
            codec.DecodeEnvelope(codec.EncodeErrorEnvelope("errorCode", "errorMessage", "errorDetails")));

        Assert.Equal("errorCode", exception.Code);
        Assert.Equal("errorMessage", exception.ErrorMessage);
        Assert.Equal("errorDetails", exception.Details);
        Assert.Null(exception.Stacktrace);
    }

    [Fact]
    public void StandardMethodCodec_DecodesErrorEnvelopeWithNativeStacktrace()
    {
        var codec = new StandardMethodCodec();
        var buffer = new WriteBuffer();
        buffer.PutUint8(1);
        codec.MessageCodec.WriteValue(buffer, "errorCode");
        codec.MessageCodec.WriteValue(buffer, "errorMessage");
        codec.MessageCodec.WriteValue(buffer, "errorDetails");
        codec.MessageCodec.WriteValue(buffer, "errorStacktrace");

        var exception = Assert.Throws<PlatformException>(() => codec.DecodeEnvelope(buffer.Done()));
        Assert.Equal("errorStacktrace", exception.Stacktrace);
    }

    [Fact]
    public void StandardMethodCodec_AllowsANullErrorMessage()
    {
        var codec = new StandardMethodCodec();
        var exception = Assert.Throws<PlatformException>(() =>
            codec.DecodeEnvelope(codec.EncodeErrorEnvelope("errorCode", details: "errorDetails")));

        Assert.Equal("errorCode", exception.Code);
        Assert.Null(exception.ErrorMessage);
        Assert.Equal("errorDetails", exception.Details);
    }

    [Fact]
    public void StandardMethodCodec_RejectsMalformedCallsAndEnvelopes()
    {
        var codec = new StandardMethodCodec();

        Assert.Throws<FormatException>(() => codec.DecodeEnvelope(new ByteData(0)));

        var buffer = new WriteBuffer();
        codec.MessageCodec.WriteValue(buffer, 7);
        codec.MessageCodec.WriteValue(buffer, null);
        Assert.Throws<FormatException>(() => codec.DecodeMethodCall(buffer.Done()));
    }

    // ------------------------------------------------------------------ toString

    [Fact]
    public void ToStringMatchesDart()
    {
        Assert.Equal("MethodCall(sample method, null)", new MethodCall("sample method").ToString());
        Assert.Equal("PlatformException(100, null, null, null)", new PlatformException("100").ToString());
        Assert.Equal("MissingPluginException(null)", new MissingPluginException().ToString());
    }

    // ------------------------------------------------------------------- helpers

    private static byte[] Bytes(byte[] header, byte[] payload) => [.. header, .. payload];

    private static void CheckEncoding<T>(MessageCodec<T> codec, T message, byte[] expected)
    {
        Assert.Equal(expected, codec.EncodeMessage(message)!.ToUint8List());
    }

    /// <summary>Mirrors Flutter's <c>checkEncodeDecode</c>: value equality plus byte-identical
    /// re-encoding of the decoded value.</summary>
    private static void CheckEncodeDecode<T>(MessageCodec<T> codec, T message)
    {
        ByteData? encoded = codec.EncodeMessage(message);
        if (message is null)
        {
            Assert.Null(encoded);
            Assert.Null(codec.DecodeMessage(encoded));
            return;
        }

        T? decoded = codec.DecodeMessage(encoded);
        Assert.True(DeepEquals(message, decoded), $"decoded {decoded} does not match {message}");
        Assert.Equal(encoded!.ToUint8List(), codec.EncodeMessage(decoded!)!.ToUint8List());
    }

    private static bool DeepEquals(object? left, object? right)
    {
        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        if (left is double leftDouble && right is double rightDouble)
        {
            return leftDouble.Equals(rightDouble);
        }

        if (left is IDictionary leftMap && right is IDictionary rightMap)
        {
            if (leftMap.Count != rightMap.Count)
            {
                return false;
            }

            foreach (DictionaryEntry entry in leftMap)
            {
                if (!rightMap.Contains(entry.Key) || !DeepEquals(entry.Value, rightMap[entry.Key]))
                {
                    return false;
                }
            }

            return true;
        }

        if (left is IList leftList && right is IList rightList)
        {
            if (left.GetType() != right.GetType() || leftList.Count != rightList.Count)
            {
                return false;
            }

            for (int i = 0; i < leftList.Count; i++)
            {
                if (!DeepEquals(leftList[i], rightList[i]))
                {
                    return false;
                }
            }

            return true;
        }

        return Equals(left, right);
    }
}
