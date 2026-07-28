using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/painting/decoration_image.dart

public enum ImageRepeat
{
    Repeat,
    RepeatX,
    RepeatY,
    NoRepeat,
}

public enum FilterQuality
{
    None,
    Low,
    Medium,
    High,
}

public enum BlendMode
{
    Clear,
    Source,
    Destination,
    SourceOver,
    DestinationOver,
    SourceIn,
    DestinationIn,
    SourceOut,
    DestinationOut,
    SourceAtop,
    DestinationAtop,
    Xor,
    Plus,
    Modulate,
    Screen,
    Overlay,
    Darken,
    Lighten,
    ColorDodge,
    ColorBurn,
    HardLight,
    SoftLight,
    Difference,
    Exclusion,
    Multiply,
    Hue,
    Saturation,
    Color,
    Luminosity,
}

public abstract record ColorFilter
{
    public sealed record Matrix : ColorFilter
    {
        public Matrix(IReadOnlyList<double> values)
        {
            ArgumentNullException.ThrowIfNull(values);
            if (values.Count != 20)
            {
                throw new ArgumentException("A color-filter matrix must contain 20 values.", nameof(values));
            }

            Values = values.ToArray();
        }

        public Matrix(params double[] values) : this((IReadOnlyList<double>)values) { }

        public IReadOnlyList<double> Values { get; }
    }

    public sealed record Mode : ColorFilter
    {
        public Mode(Color color, BlendMode blendMode)
        {
            Color = color;
            FlutterBlendMode = blendMode;
            BlendMode = ToBitmapBlendingMode(blendMode);
        }

        public Mode(Color color, Avalonia.Media.Imaging.BitmapBlendingMode blendMode)
        {
            Color = color;
            BlendMode = blendMode;
            FlutterBlendMode = FromBitmapBlendingMode(blendMode);
        }

        public Color Color { get; }

        public BlendMode FlutterBlendMode { get; }

        public Avalonia.Media.Imaging.BitmapBlendingMode BlendMode { get; }

        private static Avalonia.Media.Imaging.BitmapBlendingMode ToBitmapBlendingMode(BlendMode blendMode)
        {
            return blendMode switch
            {
                Rendering.BlendMode.Clear => Avalonia.Media.Imaging.BitmapBlendingMode.Source,
                Rendering.BlendMode.Source => Avalonia.Media.Imaging.BitmapBlendingMode.Source,
                Rendering.BlendMode.Destination => Avalonia.Media.Imaging.BitmapBlendingMode.Destination,
                Rendering.BlendMode.SourceOver => Avalonia.Media.Imaging.BitmapBlendingMode.SourceOver,
                Rendering.BlendMode.DestinationOver => Avalonia.Media.Imaging.BitmapBlendingMode.DestinationOver,
                Rendering.BlendMode.SourceIn => Avalonia.Media.Imaging.BitmapBlendingMode.SourceIn,
                Rendering.BlendMode.DestinationIn => Avalonia.Media.Imaging.BitmapBlendingMode.DestinationIn,
                Rendering.BlendMode.SourceOut => Avalonia.Media.Imaging.BitmapBlendingMode.SourceOut,
                Rendering.BlendMode.DestinationOut => Avalonia.Media.Imaging.BitmapBlendingMode.DestinationOut,
                Rendering.BlendMode.SourceAtop => Avalonia.Media.Imaging.BitmapBlendingMode.SourceAtop,
                Rendering.BlendMode.DestinationAtop => Avalonia.Media.Imaging.BitmapBlendingMode.DestinationAtop,
                Rendering.BlendMode.Xor => Avalonia.Media.Imaging.BitmapBlendingMode.Xor,
                Rendering.BlendMode.Plus => Avalonia.Media.Imaging.BitmapBlendingMode.Plus,
                Rendering.BlendMode.Modulate => Avalonia.Media.Imaging.BitmapBlendingMode.Multiply,
                Rendering.BlendMode.Screen => Avalonia.Media.Imaging.BitmapBlendingMode.Screen,
                Rendering.BlendMode.Overlay => Avalonia.Media.Imaging.BitmapBlendingMode.Overlay,
                Rendering.BlendMode.Darken => Avalonia.Media.Imaging.BitmapBlendingMode.Darken,
                Rendering.BlendMode.Lighten => Avalonia.Media.Imaging.BitmapBlendingMode.Lighten,
                Rendering.BlendMode.ColorDodge => Avalonia.Media.Imaging.BitmapBlendingMode.ColorDodge,
                Rendering.BlendMode.ColorBurn => Avalonia.Media.Imaging.BitmapBlendingMode.ColorBurn,
                Rendering.BlendMode.HardLight => Avalonia.Media.Imaging.BitmapBlendingMode.HardLight,
                Rendering.BlendMode.SoftLight => Avalonia.Media.Imaging.BitmapBlendingMode.SoftLight,
                Rendering.BlendMode.Difference => Avalonia.Media.Imaging.BitmapBlendingMode.Difference,
                Rendering.BlendMode.Exclusion => Avalonia.Media.Imaging.BitmapBlendingMode.Exclusion,
                Rendering.BlendMode.Multiply => Avalonia.Media.Imaging.BitmapBlendingMode.Multiply,
                Rendering.BlendMode.Hue => Avalonia.Media.Imaging.BitmapBlendingMode.Hue,
                Rendering.BlendMode.Saturation => Avalonia.Media.Imaging.BitmapBlendingMode.Saturation,
                Rendering.BlendMode.Color => Avalonia.Media.Imaging.BitmapBlendingMode.Color,
                Rendering.BlendMode.Luminosity => Avalonia.Media.Imaging.BitmapBlendingMode.Luminosity,
                _ => Avalonia.Media.Imaging.BitmapBlendingMode.SourceOver,
            };
        }

        private static BlendMode FromBitmapBlendingMode(Avalonia.Media.Imaging.BitmapBlendingMode blendMode)
        {
            return blendMode switch
            {
                Avalonia.Media.Imaging.BitmapBlendingMode.Source => Rendering.BlendMode.Source,
                Avalonia.Media.Imaging.BitmapBlendingMode.Destination => Rendering.BlendMode.Destination,
                Avalonia.Media.Imaging.BitmapBlendingMode.DestinationOver => Rendering.BlendMode.DestinationOver,
                Avalonia.Media.Imaging.BitmapBlendingMode.SourceIn => Rendering.BlendMode.SourceIn,
                Avalonia.Media.Imaging.BitmapBlendingMode.DestinationIn => Rendering.BlendMode.DestinationIn,
                Avalonia.Media.Imaging.BitmapBlendingMode.SourceOut => Rendering.BlendMode.SourceOut,
                Avalonia.Media.Imaging.BitmapBlendingMode.DestinationOut => Rendering.BlendMode.DestinationOut,
                Avalonia.Media.Imaging.BitmapBlendingMode.SourceAtop => Rendering.BlendMode.SourceAtop,
                Avalonia.Media.Imaging.BitmapBlendingMode.DestinationAtop => Rendering.BlendMode.DestinationAtop,
                Avalonia.Media.Imaging.BitmapBlendingMode.Xor => Rendering.BlendMode.Xor,
                Avalonia.Media.Imaging.BitmapBlendingMode.Plus => Rendering.BlendMode.Plus,
                Avalonia.Media.Imaging.BitmapBlendingMode.Screen => Rendering.BlendMode.Screen,
                Avalonia.Media.Imaging.BitmapBlendingMode.Overlay => Rendering.BlendMode.Overlay,
                Avalonia.Media.Imaging.BitmapBlendingMode.Darken => Rendering.BlendMode.Darken,
                Avalonia.Media.Imaging.BitmapBlendingMode.Lighten => Rendering.BlendMode.Lighten,
                Avalonia.Media.Imaging.BitmapBlendingMode.ColorDodge => Rendering.BlendMode.ColorDodge,
                Avalonia.Media.Imaging.BitmapBlendingMode.ColorBurn => Rendering.BlendMode.ColorBurn,
                Avalonia.Media.Imaging.BitmapBlendingMode.HardLight => Rendering.BlendMode.HardLight,
                Avalonia.Media.Imaging.BitmapBlendingMode.SoftLight => Rendering.BlendMode.SoftLight,
                Avalonia.Media.Imaging.BitmapBlendingMode.Difference => Rendering.BlendMode.Difference,
                Avalonia.Media.Imaging.BitmapBlendingMode.Exclusion => Rendering.BlendMode.Exclusion,
                Avalonia.Media.Imaging.BitmapBlendingMode.Multiply => Rendering.BlendMode.Multiply,
                Avalonia.Media.Imaging.BitmapBlendingMode.Hue => Rendering.BlendMode.Hue,
                Avalonia.Media.Imaging.BitmapBlendingMode.Saturation => Rendering.BlendMode.Saturation,
                Avalonia.Media.Imaging.BitmapBlendingMode.Color => Rendering.BlendMode.Color,
                Avalonia.Media.Imaging.BitmapBlendingMode.Luminosity => Rendering.BlendMode.Luminosity,
                _ => Rendering.BlendMode.SourceOver,
            };
        }
    }
}

public class DecorationImage : IEquatable<DecorationImage>
{
    public DecorationImage(
        ImageProvider image,
        ImageErrorListener? onError = null,
        ColorFilter? colorFilter = null,
        BoxFit? fit = null,
        Alignment alignment = default,
        Rect? centerSlice = null,
        ImageRepeat repeat = ImageRepeat.NoRepeat,
        bool matchTextDirection = false,
        double scale = 1.0,
        double opacity = 1.0,
        FilterQuality filterQuality = FilterQuality.Medium,
        bool invertColors = false,
        bool isAntiAlias = false)
    {
        Image = image ?? throw new ArgumentNullException(nameof(image));
        if (!double.IsFinite(scale) || scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale));
        if (!double.IsFinite(opacity) || opacity < 0 || opacity > 1) throw new ArgumentOutOfRangeException(nameof(opacity));
        if (centerSlice.HasValue && (fit is BoxFit.None or BoxFit.Cover))
        {
            throw new ArgumentException("centerSlice cannot be combined with BoxFit.None or BoxFit.Cover.", nameof(fit));
        }

        OnError = onError;
        ColorFilter = colorFilter;
        Fit = fit;
        Alignment = alignment;
        CenterSlice = centerSlice;
        Repeat = repeat;
        MatchTextDirection = matchTextDirection;
        Scale = scale;
        Opacity = opacity;
        FilterQuality = filterQuality;
        InvertColors = invertColors;
        IsAntiAlias = isAntiAlias;
    }

    public ImageProvider Image { get; }
    public ImageErrorListener? OnError { get; }
    public ColorFilter? ColorFilter { get; }
    public BoxFit? Fit { get; }
    public Alignment Alignment { get; }
    public Rect? CenterSlice { get; }
    public ImageRepeat Repeat { get; }
    public bool MatchTextDirection { get; }
    public double Scale { get; }
    public double Opacity { get; }
    public FilterQuality FilterQuality { get; }
    public bool InvertColors { get; }
    public bool IsAntiAlias { get; }

    public virtual DecorationImagePainter CreatePainter(Action onChanged)
    {
        return new DecorationImagePainter(this, onChanged ?? throw new ArgumentNullException(nameof(onChanged)));
    }

    public virtual bool Equals(DecorationImage? other)
    {
        return other is not null
               && Equals(Image, other.Image)
               && Equals(ColorFilter, other.ColorFilter)
               && Fit == other.Fit
               && Alignment == other.Alignment
               && CenterSlice == other.CenterSlice
               && Repeat == other.Repeat
               && MatchTextDirection == other.MatchTextDirection
               && Scale.Equals(other.Scale)
               && Opacity.Equals(other.Opacity)
               && FilterQuality == other.FilterQuality
               && InvertColors == other.InvertColors
               && IsAntiAlias == other.IsAntiAlias;
    }

    public override bool Equals(object? obj) => Equals(obj as DecorationImage);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Image);
        hash.Add(ColorFilter);
        hash.Add(Fit);
        hash.Add(Alignment);
        hash.Add(CenterSlice);
        hash.Add(Repeat);
        hash.Add(MatchTextDirection);
        hash.Add(Scale);
        hash.Add(Opacity);
        hash.Add(FilterQuality);
        hash.Add(InvertColors);
        hash.Add(IsAntiAlias);
        return hash.ToHashCode();
    }

    public static DecorationImage? Lerp(DecorationImage? a, DecorationImage? b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        if (ReferenceEquals(a, b) || Equals(a, b)) return a;
        return new BlendedDecorationImage(a, b, t);
    }
}

public class DecorationImagePainter : IDisposable
{
    private readonly DecorationImage _details;
    private readonly Action _onChanged;
    private readonly ImageStreamListener _listener;
    private ImageStream? _imageStream;
    private ImageInfo? _image;
    private bool _disposed;

    protected internal DecorationImagePainter(DecorationImage details, Action onChanged)
    {
        _details = details;
        _onChanged = onChanged;
        _listener = new ImageStreamListener(HandleImage, OnError: details.OnError);
    }

    public virtual void Paint(
        PaintingContext context,
        Rect rect,
        ImageConfiguration configuration,
        BorderRadius? clipRadius = null,
        double blend = 1.0,
        BoxShape shape = BoxShape.Rectangle,
        BitmapBlendingMode blendMode = BitmapBlendingMode.SourceOver)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_details.MatchTextDirection && configuration.TextDirection is null)
        {
            throw new InvalidOperationException(
                "DecorationImage.matchTextDirection requires a TextDirection in ImageConfiguration.");
        }

        var newStream = _details.Image.Resolve(configuration);
        if (!Equals(newStream.Key, _imageStream?.Key))
        {
            _imageStream?.RemoveListener(_listener);
            _imageStream = newStream;
            _imageStream.AddListener(_listener);
        }

        if (_image is null)
        {
            return;
        }

        ImagePainting.PaintImage(
            context: context,
            rect: rect,
            image: _image.Image,
            scale: _details.Scale * _image.Scale,
            opacity: _details.Opacity * blend,
            fit: _details.Fit,
            alignment: _details.Alignment,
            centerSlice: _details.CenterSlice,
            repeat: _details.Repeat,
            flipHorizontally: _details.MatchTextDirection && configuration.TextDirection == Plumix.UI.TextDirection.Rtl,
            clipRadius: clipRadius,
            shape: shape,
            colorFilter: _details.ColorFilter,
            filterQuality: _details.FilterQuality,
            invertColors: _details.InvertColors,
            isAntiAlias: _details.IsAntiAlias,
            blendMode: blendMode);
    }

    public virtual void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _imageStream?.RemoveListener(_listener);
        _imageStream = null;
        _image?.Dispose();
        _image = null;
    }

    private void HandleImage(ImageInfo image, bool synchronousCall)
    {
        if (_image is not null && _image.IsCloneOf(image))
        {
            image.Dispose();
            return;
        }

        _image?.Dispose();
        _image = image;
        if (!synchronousCall)
        {
            _onChanged();
        }
    }
}

internal sealed class BlendedDecorationImage : DecorationImage
{
    public BlendedDecorationImage(DecorationImage? a, DecorationImage? b, double t)
        : base(
            image: (b ?? a ?? throw new ArgumentException("At least one image is required.")).Image,
            onError: b?.OnError ?? a?.OnError,
            colorFilter: b?.ColorFilter ?? a?.ColorFilter,
            fit: b?.Fit ?? a?.Fit,
            alignment: b?.Alignment ?? a!.Alignment,
            centerSlice: b?.CenterSlice ?? a?.CenterSlice,
            repeat: b?.Repeat ?? a!.Repeat,
            matchTextDirection: b?.MatchTextDirection ?? a!.MatchTextDirection,
            scale: b?.Scale ?? a!.Scale,
            opacity: b?.Opacity ?? a!.Opacity,
            filterQuality: b?.FilterQuality ?? a!.FilterQuality,
            invertColors: b?.InvertColors ?? a!.InvertColors,
            isAntiAlias: b?.IsAntiAlias ?? a!.IsAntiAlias)
    {
        A = a;
        B = b;
        T = t;
    }

    public DecorationImage? A { get; }
    public DecorationImage? B { get; }
    public double T { get; }

    public override DecorationImagePainter CreatePainter(Action onChanged)
    {
        return new BlendedDecorationImagePainter(this, onChanged);
    }

    public override bool Equals(DecorationImage? other)
    {
        return other is BlendedDecorationImage blended
               && Equals(A, blended.A)
               && Equals(B, blended.B)
               && T.Equals(blended.T);
    }

    public override int GetHashCode() => HashCode.Combine(A, B, T);
}

internal sealed class BlendedDecorationImagePainter : DecorationImagePainter
{
    private readonly DecorationImagePainter? _a;
    private readonly DecorationImagePainter? _b;
    private readonly double _t;
    private bool _disposed;

    public BlendedDecorationImagePainter(BlendedDecorationImage details, Action onChanged)
        : base(details, onChanged)
    {
        _a = details.A?.CreatePainter(onChanged);
        _b = details.B?.CreatePainter(onChanged);
        _t = details.T;
    }

    public override void Paint(
        PaintingContext context,
        Rect rect,
        ImageConfiguration configuration,
        BorderRadius? clipRadius = null,
        double blend = 1.0,
        BoxShape shape = BoxShape.Rectangle,
        BitmapBlendingMode blendMode = BitmapBlendingMode.SourceOver)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _a?.Paint(
            context,
            rect,
            configuration,
            clipRadius,
            blend * (1 - _t),
            shape,
            blendMode);
        _b?.Paint(
            context,
            rect,
            configuration,
            clipRadius,
            blend * _t,
            shape,
            blendMode);
    }

    public override void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _a?.Dispose();
        _b?.Dispose();
        base.Dispose();
    }
}

public static class ImagePainting
{
    public static void PaintImage(
        PaintingContext context,
        Rect rect,
        IImage image,
        double scale = 1.0,
        double opacity = 1.0,
        ColorFilter? colorFilter = null,
        BoxFit? fit = null,
        Alignment alignment = default,
        Rect? centerSlice = null,
        ImageRepeat repeat = ImageRepeat.NoRepeat,
        bool flipHorizontally = false,
        bool invertColors = false,
        FilterQuality filterQuality = FilterQuality.Medium,
        bool isAntiAlias = false,
        BorderRadius? clipRadius = null,
        BoxShape shape = BoxShape.Rectangle,
        BitmapBlendingMode blendMode = BitmapBlendingMode.SourceOver)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(image);
        if (rect.Width <= 0 || rect.Height <= 0) return;
        if (!double.IsFinite(scale) || scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale));

        var plan = CreatePaintPlan(
            rect,
            image.Size,
            scale,
            fit,
            alignment,
            centerSlice,
            repeat,
            flipHorizontally);
        foreach (var destination in plan.DestinationRects)
        {
            var effectiveClipRect = plan.ClipRect ?? (clipRadius.HasValue ? rect : null);
            var ovalClipRect = shape == BoxShape.Circle ? InscribedSquare(rect) : (Rect?)null;
            if (plan.CenterSlicePixels.HasValue)
            {
                DrawNinePatch(
                    context,
                    image,
                    plan.SourceImageSize,
                    plan.CenterSlicePixels.Value,
                    destination,
                    scale,
                    opacity,
                    effectiveClipRect,
                    clipRadius,
                    ovalClipRect,
                    flipHorizontally,
                    plan.FlipAxisX,
                    filterQuality,
                    isAntiAlias,
                    blendMode);
            }
            else
            {
                context.DrawImage(
                    image: image,
                    sourceRect: plan.SourceRect,
                    destinationRect: destination,
                    opacity: opacity,
                    clipRect: effectiveClipRect,
                    clipRadius: clipRadius,
                    ovalClipRect: ovalClipRect,
                    flipHorizontally: flipHorizontally,
                    horizontalFlipAxisX: plan.FlipAxisX,
                    filterQuality: filterQuality,
                    isAntiAlias: isAntiAlias,
                    blendMode: blendMode);
            }
        }

        // Avalonia's public DrawingContext does not currently expose per-draw
        // color-matrix effects. The parity fields are retained so the backend
        // can adopt them without another public API change.
        _ = colorFilter;
        _ = invertColors;
    }

    internal static ImagePaintPlan CreatePaintPlan(
        Rect rect,
        Size rawImageSize,
        double scale,
        BoxFit? fit,
        Alignment alignment,
        Rect? centerSlice,
        ImageRepeat repeat,
        bool flipHorizontally)
    {
        if (rect.Width <= 0 || rect.Height <= 0 || rawImageSize.Width <= 0 || rawImageSize.Height <= 0)
        {
            return ImagePaintPlan.Empty;
        }

        var outputSize = rect.Size;
        var inputSize = rawImageSize;
        Vector? sliceBorder = null;
        if (centerSlice.HasValue)
        {
            sliceBorder = new Vector(
                (inputSize.Width / scale) - centerSlice.Value.Width,
                (inputSize.Height / scale) - centerSlice.Value.Height);
            outputSize = new Size(
                Math.Max(0, outputSize.Width - sliceBorder.Value.X),
                Math.Max(0, outputSize.Height - sliceBorder.Value.Y));
            inputSize = new Size(
                Math.Max(0, inputSize.Width - sliceBorder.Value.X * scale),
                Math.Max(0, inputSize.Height - sliceBorder.Value.Y * scale));
        }

        var effectiveFit = fit ?? (centerSlice.HasValue ? BoxFit.Fill : BoxFit.ScaleDown);
        if (centerSlice.HasValue && effectiveFit is BoxFit.None or BoxFit.Cover)
        {
            throw new InvalidOperationException("centerSlice requires a fit that keeps the complete source image visible.");
        }

        var fitted = BoxFitUtils.ApplyBoxFit(
            effectiveFit,
            new Size(inputSize.Width / scale, inputSize.Height / scale),
            outputSize);
        var sourceSize = new Size(fitted.Source.Width * scale, fitted.Source.Height * scale);
        var destinationSize = fitted.Destination;
        if (sliceBorder.HasValue)
        {
            destinationSize = new Size(
                destinationSize.Width + sliceBorder.Value.X,
                destinationSize.Height + sliceBorder.Value.Y);
            if (!SizesClose(sourceSize, inputSize))
            {
                throw new InvalidOperationException(
                    "centerSlice requires a fit that keeps the complete source image visible.");
            }
        }

        if (repeat != ImageRepeat.NoRepeat && destinationSize == rect.Size)
        {
            repeat = ImageRepeat.NoRepeat;
        }

        var effectiveDestinationAlignment = flipHorizontally
            ? new Alignment(-alignment.X, alignment.Y)
            : alignment;
        var destinationOffset = effectiveDestinationAlignment.AlongOffset(rect.Size, destinationSize);
        var destinationRect = new Rect(rect.Position + destinationOffset, destinationSize);
        var sourceOffset = alignment.AlongOffset(rawImageSize, sourceSize);
        var sourceRect = new Rect(sourceOffset, sourceSize);
        var destinations = GenerateImageTileRects(rect, destinationRect, repeat);
        Rect? centerSlicePixels = centerSlice.HasValue
            ? ScaleRect(centerSlice.Value, scale)
            : null;
        return new ImagePaintPlan(
            SourceImageSize: rawImageSize,
            SourceRect: sourceRect,
            DestinationRects: destinations,
            ClipRect: repeat == ImageRepeat.NoRepeat ? null : rect,
            CenterSlicePixels: centerSlicePixels,
            FlipAxisX: flipHorizontally ? rect.Center.X : null);
    }

    internal static IReadOnlyList<Rect> GenerateImageTileRects(
        Rect outputRect,
        Rect fundamentalRect,
        ImageRepeat repeat)
    {
        if (fundamentalRect.Width <= 0 || fundamentalRect.Height <= 0)
        {
            return [];
        }

        int startX = 0;
        int stopX = 0;
        int startY = 0;
        int stopY = 0;
        if (repeat is ImageRepeat.Repeat or ImageRepeat.RepeatX)
        {
            startX = (int)Math.Floor((outputRect.Left - fundamentalRect.Left) / fundamentalRect.Width);
            stopX = (int)Math.Ceiling((outputRect.Right - fundamentalRect.Right) / fundamentalRect.Width);
        }
        if (repeat is ImageRepeat.Repeat or ImageRepeat.RepeatY)
        {
            startY = (int)Math.Floor((outputRect.Top - fundamentalRect.Top) / fundamentalRect.Height);
            stopY = (int)Math.Ceiling((outputRect.Bottom - fundamentalRect.Bottom) / fundamentalRect.Height);
        }

        var result = new List<Rect>();
        for (int x = startX; x <= stopX; x++)
        {
            for (int y = startY; y <= stopY; y++)
            {
                result.Add(fundamentalRect.Translate(x * fundamentalRect.Width, y * fundamentalRect.Height));
            }
        }
        return result;
    }

    private static void DrawNinePatch(
        PaintingContext context,
        IImage image,
        Size imageSize,
        Rect centerSlice,
        Rect destination,
        double scale,
        double opacity,
        Rect? clipRect,
        BorderRadius? clipRadius,
        Rect? ovalClipRect,
        bool flipHorizontally,
        double? flipAxisX,
        FilterQuality filterQuality,
        bool isAntiAlias,
        BitmapBlendingMode blendMode)
    {
        foreach (var patch in GenerateNinePatchRects(imageSize, centerSlice, destination, scale))
        {
            context.DrawImage(
                image: image,
                sourceRect: patch.Source,
                destinationRect: patch.Destination,
                opacity: opacity,
                clipRect: clipRect,
                clipRadius: clipRadius,
                ovalClipRect: ovalClipRect,
                flipHorizontally: flipHorizontally,
                horizontalFlipAxisX: flipAxisX,
                filterQuality: filterQuality,
                isAntiAlias: isAntiAlias,
                blendMode: blendMode);
        }
    }

    internal static IReadOnlyList<ImagePatch> GenerateNinePatchRects(
        Size imageSize,
        Rect centerSlice,
        Rect destination,
        double scale)
    {
        if (!double.IsFinite(scale) || scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale));
        double[] sourceX = new[] { 0.0, centerSlice.Left, centerSlice.Right, imageSize.Width };
        double[] sourceY = new[] { 0.0, centerSlice.Top, centerSlice.Bottom, imageSize.Height };
        double left = centerSlice.Left / scale;
        double right = (imageSize.Width - centerSlice.Right) / scale;
        double top = centerSlice.Top / scale;
        double bottom = (imageSize.Height - centerSlice.Bottom) / scale;
        NormalizeBorders(destination.Width, ref left, ref right);
        NormalizeBorders(destination.Height, ref top, ref bottom);
        double[] destinationX = new[] { destination.Left, destination.Left + left, destination.Right - right, destination.Right };
        double[] destinationY = new[] { destination.Top, destination.Top + top, destination.Bottom - bottom, destination.Bottom };
        var result = new List<ImagePatch>(9);

        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                result.Add(new ImagePatch(
                    Source: new Rect(
                        sourceX[x], sourceY[y],
                        sourceX[x + 1] - sourceX[x], sourceY[y + 1] - sourceY[y]),
                    Destination: new Rect(
                        destinationX[x], destinationY[y],
                        destinationX[x + 1] - destinationX[x], destinationY[y + 1] - destinationY[y])));
            }
        }

        return result;
    }

    private static void NormalizeBorders(double extent, ref double leading, ref double trailing)
    {
        double sum = leading + trailing;
        if (sum <= extent || sum <= 0) return;
        double factor = extent / sum;
        leading *= factor;
        trailing *= factor;
    }

    private static Rect ScaleRect(Rect rect, double scale)
    {
        return new Rect(rect.X * scale, rect.Y * scale, rect.Width * scale, rect.Height * scale);
    }

    private static Rect InscribedSquare(Rect rect)
    {
        double side = Math.Min(rect.Width, rect.Height);
        return new Rect(
            rect.Center.X - (side / 2.0),
            rect.Center.Y - (side / 2.0),
            side,
            side);
    }

    private static bool SizesClose(Size first, Size second)
    {
        const double epsilon = 1e-9;
        return Math.Abs(first.Width - second.Width) <= epsilon
               && Math.Abs(first.Height - second.Height) <= epsilon;
    }
}

internal sealed record ImagePaintPlan(
    Size SourceImageSize,
    Rect SourceRect,
    IReadOnlyList<Rect> DestinationRects,
    Rect? ClipRect,
    Rect? CenterSlicePixels,
    double? FlipAxisX)
{
    public static ImagePaintPlan Empty { get; } = new(new Size(), new Rect(), [], null, null, null);
}

internal readonly record struct ImagePatch(Rect Source, Rect Destination);

internal static class RectImageExtensions
{
    public static Rect Translate(this Rect rect, double x, double y)
    {
        return new Rect(rect.X + x, rect.Y + y, rect.Width, rect.Height);
    }
}
