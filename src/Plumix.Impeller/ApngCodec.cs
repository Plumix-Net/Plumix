using System.Buffers.Binary;
using Plumix.UI;

namespace Plumix.Impeller;

// C#-only host infrastructure; the engine decodes animated PNG itself
// (engine/src/flutter/lib/ui/painting/image_generator_apng.cc), and platform decoders show only its default
// image. Implements PNG 3's animation chunks (https://www.w3.org/TR/png-3/#apng-frame-based-animation) as
// Plumix.Avalonia's ApngImageCodec does, verified against the same engine regression images: each frame's data
// becomes a standalone PNG the host decodes, composed onto a premultiplied RGBA canvas with the frame's blend
// and disposal ops.

/// <summary>An animated PNG's frames, decoded one at a time.</summary>
internal sealed class ApngCodec : Codec
{
    private static ReadOnlySpan<byte> Signature => [137, 80, 78, 71, 13, 10, 26, 10];

    private readonly byte[] _header;
    private readonly List<byte[]> _sharedChunks;
    private readonly List<AnimationFrame> _frames;
    private readonly DecodePng _decodePng;
    private readonly ScalePixels _scale;
    private byte[]? _canvas;
    private bool _disposed;
    private int _nextFrame;

    /// <summary>Decodes a standalone PNG into premultiplied RGBA8888.</summary>
    public delegate (byte[] Rgba, int Width, int Height) DecodePng(byte[] png);

    /// <summary>Scales premultiplied RGBA8888 pixels to the target size.</summary>
    public delegate byte[] ScalePixels(byte[] rgba, int width, int height, int targetWidth, int targetHeight);

    private ApngCodec(
        byte[] header,
        List<byte[]> sharedChunks,
        List<AnimationFrame> frames,
        uint plays,
        DecodePng decodePng,
        ScalePixels scale)
    {
        _header = header;
        _sharedChunks = sharedChunks;
        _frames = frames;
        _decodePng = decodePng;
        _scale = scale;
        FrameCount = frames.Count;
        Width = checked((int)ReadUInt(header));
        Height = checked((int)ReadUInt(header.AsSpan(4)));
        (TargetWidth, TargetHeight) = (Width, Height);
        RepetitionCount = plays == 0 ? -1 : checked((int)(plays - 1));
    }

    public int Width { get; }

    public int Height { get; }

    public int TargetWidth { get; set; }

    public int TargetHeight { get; set; }

    public override int FrameCount { get; }

    public override int RepetitionCount { get; }

    /// <summary>The codec of an animated PNG; null for anything else, including a PNG without animation.</summary>
    public static ApngCodec? TryCreate(ReadOnlySpan<byte> bytes, DecodePng decodePng, ScalePixels scale)
    {
        if (!HasAnimationChunk(bytes))
        {
            return null;
        }

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
            if (length > bytes.Length - offset - 12)
            {
                throw InvalidImage();
            }

            int count = (int)length;
            ReadOnlySpan<byte> type = bytes.Slice(offset + 4, 4);
            ReadOnlySpan<byte> data = bytes.Slice(offset + 8, count);
            if (ReadUInt(bytes.Slice(offset + 8 + count, 4)) != ComputeCrc(type, data))
            {
                throw InvalidImage();
            }

            if (type.SequenceEqual("IHDR"u8))
            {
                if (count != 13)
                {
                    throw InvalidImage();
                }

                header = data.ToArray();
            }
            else if (type.SequenceEqual("acTL"u8))
            {
                if (count != 8 || frameCount.HasValue || ReadUInt(data) == 0)
                {
                    throw InvalidImage();
                }

                frameCount = ReadUInt(data);
                plays = ReadUInt(data[4..]);
            }
            else if (type.SequenceEqual("fcTL"u8))
            {
                if (count != 26 || ReadUInt(data) != sequence++)
                {
                    throw InvalidImage();
                }

                frame = new AnimationFrame(data[4..].ToArray());
                frames.Add(frame);
            }
            else if (type.SequenceEqual("IDAT"u8))
            {
                // Image data before the first fcTL is the default image, which is not part of the animation.
                seenImageData = true;
                frame?.Data.Add(data.ToArray());
            }
            else if (type.SequenceEqual("fdAT"u8))
            {
                if (count < 4 || frame is null || ReadUInt(data) != sequence++)
                {
                    throw InvalidImage();
                }

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

        if (frameCount is null)
        {
            return null;
        }

        if (header is null || frameCount != frames.Count)
        {
            throw InvalidImage();
        }

        return new ApngCodec(header, shared, frames, plays, decodePng, scale);
    }

    public override Task<FrameInfo> GetNextFrame()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        try
        {
            return CompleteOnLaterTurn(Task.FromResult(DecodeFrame()));
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return CompleteOnLaterTurn(Task.FromException<FrameInfo>(
                new InvalidOperationException("Codec failed to decode APNG frame.", exception)));
        }
    }

    public override void Dispose()
    {
        _disposed = true;
        _canvas = null;
        _frames.Clear();
        _sharedChunks.Clear();
    }

    private static bool HasAnimationChunk(ReadOnlySpan<byte> bytes)
    {
        if (!bytes.StartsWith(Signature))
        {
            return false;
        }

        for (int offset = 8; offset <= bytes.Length - 12;)
        {
            uint length = ReadUInt(bytes[offset..]);
            if (length > bytes.Length - offset - 12)
            {
                return false;
            }

            ReadOnlySpan<byte> type = bytes.Slice(offset + 4, 4);
            if (type.SequenceEqual("acTL"u8))
            {
                return true;
            }

            if (type.SequenceEqual("IDAT"u8))
            {
                return false;
            }

            offset += (int)length + 12;
        }

        return false;
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
        // Subtraction instead of addition, so malformed unsigned offsets cannot wrap the bounds check.
        if (width == 0 || height == 0 || width > Width || height > Height
            || left > Width - width || top > Height - height || disposal > 2 || blend > 1)
        {
            throw InvalidImage();
        }

        _canvas ??= new byte[Width * Height * 4];
        if (_nextFrame == 0)
        {
            Array.Clear(_canvas);
        }

        byte[]? previous = disposal == 2 ? (byte[])_canvas.Clone() : null;
        (byte[] pixels, int decodedWidth, int decodedHeight) = _decodePng(FramePng(frame, width, height));
        if (decodedWidth != width || decodedHeight != height)
        {
            throw InvalidImage();
        }

        Compose(pixels, (int)left, (int)top, (int)width, (int)height, sourceOver: blend == 1);
        byte[] output = TargetWidth == Width && TargetHeight == Height
            ? (byte[])_canvas.Clone()
            : _scale(_canvas, Width, Height, TargetWidth, TargetHeight);
        if (disposal == 1)
        {
            ClearRect((int)left, (int)top, (int)width, (int)height);
        }
        else if (previous is not null)
        {
            _canvas = previous;
        }

        int numerator = BinaryPrimitives.ReadUInt16BigEndian(control[16..]);
        int denominator = BinaryPrimitives.ReadUInt16BigEndian(control[18..]);
        int milliseconds = numerator * 1000 / (denominator == 0 ? 100 : denominator);
        _nextFrame = (_nextFrame + 1) % FrameCount;
        return new FrameInfo(TimeSpan.FromMilliseconds(milliseconds),
            ImpellerImage.Wrap(output, TargetWidth, TargetHeight));
    }

    // The frame's IHDR (with the frame's size), the chunks before the image data and its data as IDAT.
    private byte[] FramePng(AnimationFrame frame, uint width, uint height)
    {
        using var png = new MemoryStream();
        png.Write(Signature);
        byte[] header = (byte[])_header.Clone();
        BinaryPrimitives.WriteUInt32BigEndian(header, width);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4), height);
        WriteChunk(png, "IHDR"u8, header);
        foreach (byte[] chunk in _sharedChunks)
        {
            png.Write(chunk);
        }

        foreach (byte[] data in frame.Data)
        {
            WriteChunk(png, "IDAT"u8, data);
        }

        WriteChunk(png, "IEND"u8, []);
        return png.ToArray();
    }

    // APNG_BLEND_OP_SOURCE replaces the region; APNG_BLEND_OP_OVER composites premultiplied source over it.
    private void Compose(byte[] source, int left, int top, int width, int height, bool sourceOver)
    {
        byte[] canvas = _canvas!;
        for (int y = 0; y < height; y++)
        {
            int sourceRow = y * width * 4;
            int canvasRow = (((top + y) * Width) + left) * 4;
            if (!sourceOver)
            {
                Buffer.BlockCopy(source, sourceRow, canvas, canvasRow, width * 4);
                continue;
            }

            for (int x = 0; x < width * 4; x += 4)
            {
                int inverseAlpha = 255 - source[sourceRow + x + 3];
                for (int channel = 0; channel < 4; channel++)
                {
                    int destination = canvasRow + x + channel;
                    canvas[destination] = (byte)(source[sourceRow + x + channel]
                                                 + ((canvas[destination] * inverseAlpha) + 127) / 255);
                }
            }
        }
    }

    private void ClearRect(int left, int top, int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            Array.Clear(_canvas!, (((top + y) * Width) + left) * 4, width * 4);
        }
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
        foreach (byte value in type)
        {
            crc = UpdateCrc(crc, value);
        }

        foreach (byte value in data)
        {
            crc = UpdateCrc(crc, value);
        }

        return crc ^ uint.MaxValue;
    }

    private static uint UpdateCrc(uint crc, byte value)
    {
        crc ^= value;
        for (int bit = 0; bit < 8; bit++)
        {
            crc = (crc >> 1) ^ ((crc & 1) == 0 ? 0 : 0xedb88320);
        }

        return crc;
    }

    private sealed record AnimationFrame(byte[] Control)
    {
        public List<byte[]> Data { get; } = [];
    }
}
