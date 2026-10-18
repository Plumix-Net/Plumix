using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;

namespace Plumix.UI;

// C#-only engine adapter for engine/src/flutter/lib/ui/painting/multi_frame_codec.cc.
// Animation disposal/compositing lives in Skia's codec, never in the image widget or render object.
internal sealed class SkiaImageCodec : Codec
{
    private SKCodec? _codec;
    private readonly PixelSize _targetSize;
    private readonly SKCodecFrameInfo[] _frames;
    private SKBitmap? _requiredFrame;
    private int _requiredFrameIndex = -1;
    private int _nextFrame;

    internal SkiaImageCodec(SKCodec codec, PixelSize targetSize)
    {
        _codec = codec;
        _targetSize = targetSize;
        _frames = codec.FrameInfo;
        FrameCount = Math.Max(1, codec.FrameCount);
        RepetitionCount = FrameCount == 1 ? 0 : codec.RepetitionCount;
    }

    public override int FrameCount { get; }
    public override int RepetitionCount { get; }

    public override Task<FrameInfo> GetNextFrame()
    {
        SKCodec codec = _codec ?? throw new ObjectDisposedException(nameof(Codec));
        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var pixels = new SKBitmap(info);
        int priorFrame = -1;
        SKCodecFrameInfo metadata = _frames[_nextFrame];
        if (metadata.RequiredFrame >= 0 && _requiredFrame is not null
            && _requiredFrameIndex >= metadata.RequiredFrame && _requiredFrameIndex < _nextFrame)
        {
            using var canvas = new SKCanvas(pixels);
            using var paint = new SKPaint { BlendMode = SKBlendMode.Src };
            canvas.DrawBitmap(_requiredFrame, 0, 0, paint);
            priorFrame = _requiredFrameIndex;
        }

        // Reuse a valid non-restore-previous backdrop, as Flutter's engine does. With no suitable
        // backdrop, PriorFrame = -1 asks Skia to reconstruct all dependencies and apply disposal.
        // https://learn.microsoft.com/dotnet/api/skiasharp.skcodecoptions.priorframe
        SKCodecResult result = codec.GetPixels(info, pixels.GetPixels(), pixels.RowBytes,
            new SKCodecOptions(_nextFrame, priorFrame));
        if (result != SKCodecResult.Success)
        {
            return Task.FromException<FrameInfo>(new InvalidOperationException(
                $"Codec failed to decode frame {_nextFrame}: {result}."));
        }

        if (metadata.DisposalMethod != SKCodecAnimationDisposalMethod.RestorePrevious)
        {
            _requiredFrame?.Dispose();
            _requiredFrame = pixels.Copy();
            _requiredFrameIndex = _nextFrame;
        }

        using var bitmap = new Bitmap(PixelFormat.Rgba8888, AlphaFormat.Premul,
            pixels.GetPixels(), new PixelSize(info.Width, info.Height), new Vector(96, 96), pixels.RowBytes);
        Bitmap image = bitmap.CreateScaledBitmap(_targetSize, BitmapInterpolationMode.HighQuality);
        TimeSpan duration = _frames.Length > _nextFrame
            ? TimeSpan.FromMilliseconds(_frames[_nextFrame].Duration)
            : TimeSpan.Zero;
        _nextFrame = (_nextFrame + 1) % FrameCount;
        return Task.FromResult(new FrameInfo(duration, image));
    }

    public override void Dispose()
    {
        _codec?.Dispose();
        _codec = null;
        _requiredFrame?.Dispose();
        _requiredFrame = null;
    }
}
