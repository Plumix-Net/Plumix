using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Plumix.UI;

namespace Plumix.Impeller.Android;

// C#-only host infrastructure parity source: engine/src/flutter/lib/ui/painting/multi_frame_codec.cc. The engine
// decodes animated GIF and WebP with Skia's codecs; this host asks the platform's NDK decoder (AImageDecoder,
// animation API from Android 12) instead. Frames are decoded in order into one premultiplied RGBA canvas, which
// the decoder composes each frame onto (it restores "previous" frames itself; this codec clears the rect of a
// frame disposed to the background); each frame is then scaled to the requested target size.

/// <summary>An animated GIF or WebP decoded frame by frame with <c>AImageDecoder</c>.</summary>
[SupportedOSPlatform("android31.0")]
internal sealed unsafe class AndroidAnimatedImageCodec : Codec
{
    private const string Library = "jnigraphics";
    private const int Success = 0;
    private const int Finished = -10;
    private const int RepeatInfinite = int.MaxValue;
    private const int DisposeOpBackground = 2;

    private nint _decoder;
    private nint _encoded;
    private readonly int _width;
    private readonly int _height;
    private readonly int _targetWidth;
    private readonly int _targetHeight;
    private readonly Func<byte[], int, int, int, int, byte[]> _scale;
    private readonly nuint _stride;
    private readonly byte[] _canvas;
    private int _nextFrame;

    private AndroidAnimatedImageCodec(
        nint decoder,
        nint encoded,
        int width,
        int height,
        (int Width, int Height) target,
        Func<byte[], int, int, int, int, byte[]> scale,
        int frameCount)
    {
        _decoder = decoder;
        _encoded = encoded;
        _width = width;
        _height = height;
        (_targetWidth, _targetHeight) = target;
        _scale = scale;
        _stride = AImageDecoder_getMinimumStride(decoder);
        _canvas = new byte[checked((int)_stride * height)];
        FrameCount = frameCount;
        int repeatCount = AImageDecoder_getRepeatCount(decoder);
        RepetitionCount = repeatCount == RepeatInfinite ? -1 : repeatCount;
    }

    public override int FrameCount { get; }

    public override int RepetitionCount { get; }

    /// <summary>
    /// The codec of an animated image the platform decodes, at the size <paramref name="targetSize"/> picks; null
    /// for a still image or one the platform cannot decode, which the caller decodes as a single frame.
    /// </summary>
    public static AndroidAnimatedImageCodec? TryCreate(
        byte[] bytes,
        Func<int, int, (int Width, int Height)> targetSize,
        Func<byte[], int, int, int, int, byte[]> scale)
    {
        // The decoder reads from the buffer for its whole lifetime.
        nint encoded = (nint)NativeMemory.Alloc((nuint)bytes.Length);
        bytes.CopyTo(new Span<byte>((void*)encoded, bytes.Length));
        nint decoder = 0;
        try
        {
            if (AImageDecoder_createFromBuffer(encoded, (nuint)bytes.Length, &decoder) != Success
                || !AImageDecoder_isAnimated(decoder))
            {
                return null;
            }

            nint header = AImageDecoder_getHeaderInfo(decoder);
            int width = AImageDecoderHeaderInfo_getWidth(header);
            int height = AImageDecoderHeaderInfo_getHeight(header);

            // The platform reports no frame count: the frames are walked once, then the decoder rewinds.
            int frameCount = 1;
            while (AImageDecoder_advanceFrame(decoder) == Success)
            {
                frameCount++;
            }

            if (AImageDecoder_rewind(decoder) != Success)
            {
                return null;
            }

            var codec = new AndroidAnimatedImageCodec(
                decoder, encoded, width, height, targetSize(width, height), scale, frameCount);
            (decoder, encoded) = (0, 0);
            return codec;
        }
        finally
        {
            if (decoder != 0)
            {
                AImageDecoder_delete(decoder);
            }

            if (encoded != 0)
            {
                NativeMemory.Free((void*)encoded);
            }
        }
    }

    public override Task<FrameInfo> GetNextFrame()
    {
        ObjectDisposedException.ThrowIf(_decoder == 0, this);
        try
        {
            return CompleteOnLaterTurn(Task.FromResult(DecodeFrame()));
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return CompleteOnLaterTurn(Task.FromException<FrameInfo>(exception));
        }
    }

    public override void Dispose()
    {
        if (_decoder != 0)
        {
            AImageDecoder_delete(_decoder);
            _decoder = 0;
        }

        if (_encoded != 0)
        {
            NativeMemory.Free((void*)_encoded);
            _encoded = 0;
        }
    }

    private FrameInfo DecodeFrame()
    {
        nint info = AImageDecoderFrameInfo_create();
        try
        {
            if (AImageDecoder_getFrameInfo(_decoder, info) != Success)
            {
                throw new InvalidOperationException($"Codec failed to read frame {_nextFrame}.");
            }

            int result;
            fixed (byte* canvas = _canvas)
            {
                result = AImageDecoder_decodeImage(_decoder, (nint)canvas, _stride, (nuint)_canvas.Length);
            }

            if (result != Success)
            {
                throw new InvalidOperationException($"Codec failed to decode frame {_nextFrame}: {result}.");
            }

            byte[] rgba = TightRows();
            var duration = TimeSpan.FromTicks(AImageDecoderFrameInfo_getDuration(info) / 100);
            if (AImageDecoderFrameInfo_getDisposeOp(info) == DisposeOpBackground)
            {
                ClearRect(AImageDecoderFrameInfo_getFrameRect(info));
            }

            _nextFrame++;
            if (_nextFrame == FrameCount || AImageDecoder_advanceFrame(_decoder) != Success)
            {
                // The animation starts over on an empty canvas.
                _nextFrame = 0;
                AImageDecoder_rewind(_decoder);
                Array.Clear(_canvas);
            }

            if (_targetWidth != _width || _targetHeight != _height)
            {
                rgba = _scale(rgba, _width, _height, _targetWidth, _targetHeight);
            }

            return new FrameInfo(duration, ImpellerImage.Wrap(rgba, _targetWidth, _targetHeight));
        }
        finally
        {
            AImageDecoderFrameInfo_delete(info);
        }
    }

    private byte[] TightRows()
    {
        int rowBytes = _width * 4;
        if ((int)_stride == rowBytes)
        {
            return (byte[])_canvas.Clone();
        }

        byte[] rgba = new byte[rowBytes * _height];
        for (int y = 0; y < _height; y++)
        {
            Buffer.BlockCopy(_canvas, y * (int)_stride, rgba, y * rowBytes, rowBytes);
        }

        return rgba;
    }

    private void ClearRect(ARect rect)
    {
        int left = Math.Clamp(rect.Left, 0, _width);
        int right = Math.Clamp(rect.Right, left, _width);
        for (int y = Math.Clamp(rect.Top, 0, _height); y < Math.Clamp(rect.Bottom, 0, _height); y++)
        {
            Array.Clear(_canvas, (y * (int)_stride) + (left * 4), (right - left) * 4);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ARect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport(Library)]
    private static extern int AImageDecoder_createFromBuffer(nint buffer, nuint length, nint* outDecoder);

    [DllImport(Library)]
    private static extern void AImageDecoder_delete(nint decoder);

    [DllImport(Library)]
    private static extern nint AImageDecoder_getHeaderInfo(nint decoder);

    [DllImport(Library)]
    private static extern int AImageDecoderHeaderInfo_getWidth(nint info);

    [DllImport(Library)]
    private static extern int AImageDecoderHeaderInfo_getHeight(nint info);

    [DllImport(Library)]
    private static extern nuint AImageDecoder_getMinimumStride(nint decoder);

    [DllImport(Library)]
    private static extern int AImageDecoder_decodeImage(nint decoder, nint pixels, nuint stride, nuint size);

    [DllImport(Library)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool AImageDecoder_isAnimated(nint decoder);

    [DllImport(Library)]
    private static extern int AImageDecoder_getRepeatCount(nint decoder);

    [DllImport(Library)]
    private static extern int AImageDecoder_advanceFrame(nint decoder);

    [DllImport(Library)]
    private static extern int AImageDecoder_rewind(nint decoder);

    [DllImport(Library)]
    private static extern nint AImageDecoderFrameInfo_create();

    [DllImport(Library)]
    private static extern void AImageDecoderFrameInfo_delete(nint info);

    [DllImport(Library)]
    private static extern int AImageDecoder_getFrameInfo(nint decoder, nint info);

    [DllImport(Library)]
    private static extern long AImageDecoderFrameInfo_getDuration(nint info);

    [DllImport(Library)]
    private static extern ARect AImageDecoderFrameInfo_getFrameRect(nint info);

    [DllImport(Library)]
    private static extern int AImageDecoderFrameInfo_getDisposeOp(nint info);
}
