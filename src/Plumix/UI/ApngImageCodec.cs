using System.Buffers.Binary;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;

namespace Plumix.UI;

// C#-only engine adapter: the installed Skia codec has no animated PNG decoder.
// Implements PNG 3's animation chunks (https://www.w3.org/TR/png-3/#apng-frame-based-animation),
// verified against engine/src/flutter/testing/dart/codec_test.dart's APNG regression images.
internal sealed class ApngImageCodec : Codec
{
    private static ReadOnlySpan<byte> Signature => [137, 80, 78, 71, 13, 10, 26, 10];
    private readonly byte[] _header;
    private readonly List<byte[]> _sharedChunks;
    private readonly List<AnimationFrame> _frames;
    private SKBitmap? _canvas;
    private bool _disposed;
    private int _nextFrame;

    private ApngImageCodec(byte[] header, List<byte[]> sharedChunks, List<AnimationFrame> frames, uint plays)
    {
        _header = header;
        _sharedChunks = sharedChunks;
        _frames = frames;
        FrameCount = frames.Count;
        Width = checked((int)ReadUInt(header));
        Height = checked((int)ReadUInt(header.AsSpan(4)));
        TargetSize = new PixelSize(Width, Height);
        RepetitionCount = plays == 0 ? -1 : checked((int)(plays - 1));
    }

    internal int Width { get; }
    internal int Height { get; }
    internal PixelSize TargetSize { get; set; }
    public override int FrameCount { get; }
    public override int RepetitionCount { get; }

    internal static ApngImageCodec? TryCreate(ReadOnlySpan<byte> bytes)
    {
        if (!HasAnimationChunk(bytes)) return null;
        byte[]? header = null;
        uint? frameCount = null;
        uint plays = 0;
        uint sequence = 0;
        bool seenImageData = false;
        List<byte[]> shared = [];
        List<AnimationFrame> frames = [];
        AnimationFrame? frame = null;
        for (int offset = 8; offset <= bytes.Length - 12;)
        {
            uint length = ReadUInt(bytes[offset..]);
            if (length > bytes.Length - offset - 12) throw InvalidImage();
            int count = (int)length;
            ReadOnlySpan<byte> type = bytes.Slice(offset + 4, 4);
            ReadOnlySpan<byte> data = bytes.Slice(offset + 8, count);
            if (ReadUInt(bytes.Slice(offset + 8 + count, 4)) != ComputeCrc(type, data)) throw InvalidImage();
            if (type.SequenceEqual("IHDR"u8))
            {
                if (count != 13) throw InvalidImage();
                header = data.ToArray();
            }
            else if (type.SequenceEqual("acTL"u8))
            {
                if (count != 8 || frameCount.HasValue || ReadUInt(data) == 0) throw InvalidImage();
                frameCount = ReadUInt(data);
                plays = ReadUInt(data[4..]);
            }
            else if (type.SequenceEqual("fcTL"u8))
            {
                if (count != 26 || ReadUInt(data) != sequence++) throw InvalidImage();
                frame = new AnimationFrame(data[4..].ToArray());
                frames.Add(frame);
            }
            else if (type.SequenceEqual("IDAT"u8))
            {
                seenImageData = true;
                frame?.Data.Add(data.ToArray());
            }
            else if (type.SequenceEqual("fdAT"u8))
            {
                if (count < 4 || frame is null || ReadUInt(data) != sequence++) throw InvalidImage();
                frame.Data.Add(data[4..].ToArray());
            }
            else if (type.SequenceEqual("IEND"u8))
            {
                break;
            }
            else if (!seenImageData)
            {
                shared.Add(bytes.Slice(offset, count + 12).ToArray());
            }

            offset += count + 12;
        }

        if (frameCount is null) return null;
        if (header is null || frameCount != frames.Count) throw InvalidImage();
        return new ApngImageCodec(header, shared, frames, plays);
    }

    private static bool HasAnimationChunk(ReadOnlySpan<byte> bytes)
    {
        if (!bytes.StartsWith(Signature)) return false;
        for (int offset = 8; offset <= bytes.Length - 12;)
        {
            uint length = ReadUInt(bytes[offset..]);
            if (length > bytes.Length - offset - 12) return false;
            ReadOnlySpan<byte> type = bytes.Slice(offset + 4, 4);
            if (type.SequenceEqual("acTL"u8)) return true;
            if (type.SequenceEqual("IDAT"u8)) return false;
            offset += (int)length + 12;
        }

        return false;
    }

    public override Task<FrameInfo> GetNextFrame()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        try
        {
            return Task.FromResult(DecodeFrame());
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return Task.FromException<FrameInfo>(new InvalidOperationException("Codec failed to decode APNG frame.",
                exception));
        }
    }

    private FrameInfo DecodeFrame()
    {
        AnimationFrame frame = _frames[_nextFrame];
        ReadOnlySpan<byte> control = frame.Control;
        uint width = ReadUInt(control);
        uint height = ReadUInt(control[4..]);
        uint left = ReadUInt(control[8..]);
        uint top = ReadUInt(control[12..]);
        byte disposal = control[20];
        byte blend = control[21];
        // Use subtraction instead of addition so malformed unsigned offsets cannot wrap the bounds check.
        if (width == 0 || height == 0 || width > Width || height > Height
            || left > Width - width || top > Height - height || disposal > 2 || blend > 1)
        {
            throw InvalidImage();
        }

        _canvas ??= new SKBitmap(new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        if (_nextFrame == 0) _canvas.Erase(SKColors.Transparent);
        using SKBitmap? previous = disposal == 2 ? _canvas.Copy() : null;
        using var png = new MemoryStream();
        png.Write(Signature);
        byte[] header = (byte[])_header.Clone();
        BinaryPrimitives.WriteUInt32BigEndian(header, width);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4), height);
        WriteChunk(png, "IHDR"u8, header);
        foreach (byte[] chunk in _sharedChunks) png.Write(chunk);
        foreach (byte[] data in frame.Data) WriteChunk(png, "IDAT"u8, data);
        WriteChunk(png, "IEND"u8, []);
        png.Position = 0;
        using SKBitmap decoded = SKBitmap.Decode(png) ?? throw InvalidImage();
        using (var canvas = new SKCanvas(_canvas))
        using (var paint = new SKPaint { BlendMode = blend == 0 ? SKBlendMode.Src : SKBlendMode.SrcOver })
        {
            canvas.DrawBitmap(decoded, left, top, paint);
        }

        using var bitmap = new Bitmap(PixelFormat.Rgba8888, AlphaFormat.Premul, _canvas.GetPixels(),
            new PixelSize(Width, Height), new Vector(96, 96), _canvas.RowBytes);
        Bitmap image = bitmap.CreateScaledBitmap(TargetSize, BitmapInterpolationMode.HighQuality);
        if (disposal == 1)
        {
            using var canvas = new SKCanvas(_canvas);
            using var clear = new SKPaint { BlendMode = SKBlendMode.Clear };
            canvas.DrawRect(new SKRect(left, top, left + width, top + height), clear);
        }
        else if (previous is not null)
        {
            _canvas.Dispose();
            _canvas = previous.Copy();
        }

        int numerator = BinaryPrimitives.ReadUInt16BigEndian(control[16..]);
        int denominator = BinaryPrimitives.ReadUInt16BigEndian(control[18..]);
        int milliseconds = numerator * 1000 / (denominator == 0 ? 100 : denominator);
        _nextFrame = (_nextFrame + 1) % FrameCount;
        return new FrameInfo(TimeSpan.FromMilliseconds(milliseconds), image);
    }

    private static uint ReadUInt(ReadOnlySpan<byte> bytes) => BinaryPrimitives.ReadUInt32BigEndian(bytes);
    private static InvalidOperationException InvalidImage() => new("Invalid animated PNG data.");

    private static void WriteChunk(Stream stream, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        Span<byte> integer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(integer, (uint)data.Length);
        stream.Write(integer);
        stream.Write(type);
        stream.Write(data);
        BinaryPrimitives.WriteUInt32BigEndian(integer, ComputeCrc(type, data));
        stream.Write(integer);
    }

    private static uint ComputeCrc(ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        uint crc = uint.MaxValue;
        foreach (byte value in type) crc = UpdateCrc(crc, value);
        foreach (byte value in data) crc = UpdateCrc(crc, value);
        return crc ^ uint.MaxValue;
    }

    private static uint UpdateCrc(uint crc, byte value)
    {
        crc ^= value;
        for (int bit = 0; bit < 8; bit++) crc = (crc >> 1) ^ ((crc & 1) == 0 ? 0 : 0xedb88320);
        return crc;
    }

    public override void Dispose()
    {
        _disposed = true;
        _canvas?.Dispose();
        _canvas = null;
        _frames.Clear();
        _sharedChunks.Clear();
    }

    private sealed record AnimationFrame(byte[] Control)
    {
        internal List<byte[]> Data { get; } = [];
    }
}
