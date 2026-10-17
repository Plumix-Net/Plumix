using Avalonia;
using Avalonia.Media;
using Plumix.Developer;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.UI;
using Canvas = Plumix.UI.Canvas;
using Path = Plumix.UI.Path;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/painting/decoration_image.dart

/// <summary>How to paint any portions of a box not covered by an image.</summary>
public enum ImageRepeat
{
    /// <summary>Repeat the image in both the x and y directions until the box is filled.</summary>
    Repeat,

    /// <summary>Repeat the image in the x direction until the box is filled horizontally.</summary>
    RepeatX,

    /// <summary>Repeat the image in the y direction until the box is filled vertically.</summary>
    RepeatY,

    /// <summary>Leave uncovered portions of the box transparent.</summary>
    NoRepeat,
}

/// <summary>An image for a box decoration.</summary>
/// <remarks>Dart's <c>DecorationImage</c>. The image is painted using <see cref="ImagePainting.PaintImage"/>.</remarks>
public class DecorationImage : IEquatable<DecorationImage>
{
    private readonly ImageProvider _image;
    private readonly ImageErrorListener? _onError;
    private readonly ColorFilter? _colorFilter;
    private readonly BoxFit? _fit;
    private readonly AlignmentGeometry _alignment;
    private readonly Rect? _centerSlice;
    private readonly ImageRepeat _repeat;
    private readonly bool _matchTextDirection;
    private readonly double _scale;
    private readonly double _opacity;
    private readonly FilterQuality _filterQuality;
    private readonly bool _invertColors;
    private readonly bool _isAntiAlias;

    /// <summary>Creates an image to show in a <see cref="BoxDecoration"/>.</summary>
    public DecorationImage(
        ImageProvider image,
        ImageErrorListener? onError = null,
        ColorFilter? colorFilter = null,
        BoxFit? fit = null,
        AlignmentGeometry alignment = default,
        Rect? centerSlice = null,
        ImageRepeat repeat = ImageRepeat.NoRepeat,
        bool matchTextDirection = false,
        double scale = 1.0,
        double opacity = 1.0,
        FilterQuality filterQuality = FilterQuality.Medium,
        bool invertColors = false,
        bool isAntiAlias = false)
    {
        _image = image ?? throw new ArgumentNullException(nameof(image));
        _onError = onError;
        _colorFilter = colorFilter;
        _fit = fit;
        _alignment = alignment;
        _centerSlice = centerSlice;
        _repeat = repeat;
        _matchTextDirection = matchTextDirection;
        _scale = scale;
        _opacity = opacity;
        _filterQuality = filterQuality;
        _invertColors = invertColors;
        _isAntiAlias = isAntiAlias;
    }

    // Dart's `_BlendedDecorationImage implements DecorationImage`: a subclass that forwards every getter.
    private protected DecorationImage(ImageProvider image)
    {
        _image = image;
    }

    /// <summary>The image to be painted into the decoration.</summary>
    public virtual ImageProvider Image => _image;

    /// <summary>An optional error callback for errors emitted when loading <see cref="Image"/>.</summary>
    public virtual ImageErrorListener? OnError => _onError;

    /// <summary>A color filter to apply to the image before painting it.</summary>
    public virtual ColorFilter? ColorFilter => _colorFilter;

    /// <summary>How the image should be inscribed into the box.</summary>
    /// <remarks>Null means <see cref="BoxFit.ScaleDown"/> without a <see cref="CenterSlice"/> and
    /// <see cref="BoxFit.Fill"/> with one.</remarks>
    public virtual BoxFit? Fit => _fit;

    /// <summary>How to align the image within its bounds.</summary>
    public virtual AlignmentGeometry Alignment => _alignment;

    /// <summary>The center slice for a nine-patch image.</summary>
    public virtual Rect? CenterSlice => _centerSlice;

    /// <summary>How to paint any portions of the box that would not otherwise be covered by the image.</summary>
    public virtual ImageRepeat Repeat => _repeat;

    /// <summary>Whether to paint the image in the direction of the <see cref="TextDirection"/>.</summary>
    public virtual bool MatchTextDirection => _matchTextDirection;

    /// <summary>Defines image pixels to be shown per logical pixels.</summary>
    public virtual double Scale => _scale;

    /// <summary>
    /// If non-null, the value is multiplied with the opacity of each image pixel before painting onto the
    /// canvas.
    /// </summary>
    public virtual double Opacity => _opacity;

    /// <summary>Used to set the filterQuality of the image.</summary>
    public virtual FilterQuality FilterQuality => _filterQuality;

    /// <summary>Whether the colors of the image are inverted when drawn.</summary>
    public virtual bool InvertColors => _invertColors;

    /// <summary>Whether to paint the image with anti-aliasing.</summary>
    public virtual bool IsAntiAlias => _isAntiAlias;

    /// <summary>
    /// Creates a <see cref="DecorationImagePainter"/> for this <see cref="DecorationImage"/>.
    /// </summary>
    /// <remarks>
    /// The <paramref name="onChanged"/> argument will be called whenever the image needs to be repainted,
    /// e.g. because it is loading incrementally or because it is animated.
    /// </remarks>
    public virtual DecorationImagePainter CreatePainter(Action onChanged)
    {
        ArgumentNullException.ThrowIfNull(onChanged);
        return new DecorationImagePainterImpl(this, onChanged);
    }

    /// <inheritdoc />
    public virtual bool Equals(DecorationImage? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is not null
               && other.GetType() == GetType()
               && Equals(other.Image, Image)
               && Equals(other.ColorFilter, ColorFilter)
               && other.Fit == Fit
               && other.Alignment == Alignment
               && other.CenterSlice == CenterSlice
               && other.Repeat == Repeat
               && other.MatchTextDirection == MatchTextDirection
               && other.Scale.Equals(Scale)
               && other.Opacity.Equals(Opacity)
               && other.FilterQuality == FilterQuality
               && other.InvertColors == InvertColors
               && other.IsAntiAlias == IsAntiAlias;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as DecorationImage);

    /// <inheritdoc />
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

    /// <inheritdoc />
    public override string ToString()
    {
        var properties = new List<string> { $"{Image}" };
        if (ColorFilter is not null)
        {
            properties.Add($"{ColorFilter}");
        }

        if (Fit is { } fit
            && !(fit == BoxFit.Fill && CenterSlice is not null)
            && !(fit == BoxFit.ScaleDown && CenterSlice is null))
        {
            properties.Add(DartFormat.Enum(fit));
        }

        properties.Add($"{Alignment}");
        if (CenterSlice is { } centerSlice)
        {
            properties.Add($"centerSlice: {ImagePainting.RectToString(centerSlice)}");
        }

        if (Repeat != ImageRepeat.NoRepeat)
        {
            properties.Add(DartFormat.Enum(Repeat));
        }

        if (MatchTextDirection)
        {
            properties.Add("match text direction");
        }

        properties.Add($"scale {DartFormat.Fixed(Scale)}");
        properties.Add($"opacity {DartFormat.Fixed(Opacity)}");
        properties.Add(DartFormat.Enum(FilterQuality));
        if (InvertColors)
        {
            properties.Add("invert colors");
        }

        if (IsAntiAlias)
        {
            properties.Add("use anti-aliasing");
        }

        return $"DecorationImage({string.Join(", ", properties)})";
    }

    /// <summary>Linearly interpolates between two <see cref="DecorationImage"/>s.</summary>
    /// <remarks>
    /// The <paramref name="t"/> argument represents position on the timeline, with 0.0 meaning that the
    /// interpolation has not started, returning <paramref name="a"/>, and 1.0 meaning that it has finished,
    /// returning <paramref name="b"/>. Values in between paint both images, cross-faded.
    /// </remarks>
    public static DecorationImage? Lerp(DecorationImage? a, DecorationImage? b, double t)
    {
        if (ReferenceEquals(a, b) || t == 0.0)
        {
            return a;
        }

        if (t == 1.0)
        {
            return b;
        }

        return new BlendedDecorationImage(a, b, t);
    }
}

/// <summary>The scheme for painting a <see cref="DecorationImage"/>.</summary>
/// <remarks>
/// Dart's <c>abstract interface class DecorationImagePainter</c>, created by
/// <see cref="DecorationImage.CreatePainter"/>; it must be disposed when no longer needed.
/// </remarks>
public abstract class DecorationImagePainter : IDisposable
{
    /// <summary>Draw the image onto the given canvas.</summary>
    /// <remarks>
    /// The image is drawn at the position and size given by <paramref name="rect"/>. When
    /// <paramref name="clipPath"/> is non-null the image is clipped to it. <paramref name="blend"/> scales
    /// the image's opacity and <paramref name="blendMode"/> is the blend mode of the image draw.
    /// </remarks>
    public abstract void Paint(
        Canvas canvas,
        Rect rect,
        Path? clipPath,
        ImageConfiguration configuration,
        double blend = 1.0,
        BlendMode blendMode = BlendMode.SourceOver);

    /// <summary>Releases the resources used by this painter.</summary>
    public abstract void Dispose();
}

// Dart's `_DecorationImagePainter`.
internal sealed class DecorationImagePainterImpl : DecorationImagePainter
{
    private readonly DecorationImage _details;
    private readonly Action _onChanged;
    private ImageStream? _imageStream;
    private ImageInfo? _image;

    internal DecorationImagePainterImpl(DecorationImage details, Action onChanged)
    {
        _details = details;
        _onChanged = onChanged;
        if (Constants.KDebugMode)
        {
            FoundationDebug.DebugMaybeDispatchCreated("painting", "_DecorationImagePainter", this);
        }
    }

    public override void Paint(
        Canvas canvas,
        Rect rect,
        Path? clipPath,
        ImageConfiguration configuration,
        double blend = 1.0,
        BlendMode blendMode = BlendMode.SourceOver)
    {
        bool flipHorizontally = false;
        if (_details.MatchTextDirection)
        {
            if (Constants.KDebugMode && configuration.TextDirection is null)
            {
                throw new FlutterError(
                [
                    new ErrorSummary(
                        "DecorationImage.matchTextDirection can only be used when a TextDirection is available."),
                    new ErrorDescription(
                        "When DecorationImagePainter.paint() was called, there was no text direction provided "
                        + "in the ImageConfiguration object to match."),
                    new DiagnosticsProperty<DecorationImage>(
                        "The DecorationImage was",
                        _details,
                        style: DiagnosticsTreeStyle.ErrorProperty),
                    new DiagnosticsProperty<ImageConfiguration>(
                        "The ImageConfiguration was",
                        configuration,
                        style: DiagnosticsTreeStyle.ErrorProperty),
                ]);
            }

            if (configuration.TextDirection == TextDirection.Rtl)
            {
                flipHorizontally = true;
            }
        }

        ImageStream newImageStream = _details.Image.Resolve(configuration);
        if (!Equals(newImageStream.Key, _imageStream?.Key))
        {
            var listener = new ImageStreamListener(HandleImage, OnError: _details.OnError);
            _imageStream?.RemoveListener(listener);
            _imageStream = newImageStream;
            _imageStream.AddListener(listener);
        }

        if (_image is null)
        {
            return;
        }

        if (clipPath is not null)
        {
            canvas.Save();
            canvas.ClipPath(clipPath);
        }

        ImagePainting.PaintImage(
            canvas: canvas,
            rect: rect,
            image: _image.Image,
            debugImageLabel: _image.DebugLabel,
            scale: _details.Scale * _image.Scale,
            colorFilter: _details.ColorFilter,
            fit: _details.Fit,
            alignment: _details.Alignment.Resolve(configuration.TextDirection),
            centerSlice: _details.CenterSlice,
            repeat: _details.Repeat,
            flipHorizontally: flipHorizontally,
            opacity: _details.Opacity * blend,
            filterQuality: _details.FilterQuality,
            invertColors: _details.InvertColors,
            isAntiAlias: _details.IsAntiAlias,
            blendMode: blendMode);

        if (clipPath is not null)
        {
            canvas.Restore();
        }
    }

    private void HandleImage(ImageInfo value, bool synchronousCall)
    {
        // Dart's `_image == value` compares image handles, and every clone holds a handle of its own, so
        // only the very same info is equal; Plumix clones share one image, so compare the infos themselves.
        if (ReferenceEquals(_image, value))
        {
            return;
        }

        if (_image is not null && _image.IsCloneOf(value))
        {
            value.Dispose();
            return;
        }

        _image?.Dispose();
        _image = value;
        if (!synchronousCall)
        {
            _onChanged();
        }
    }

    public override void Dispose()
    {
        if (Constants.KDebugMode)
        {
            FoundationDebug.DebugMaybeDispatchDisposed(this);
        }

        _imageStream?.RemoveListener(new ImageStreamListener(HandleImage, OnError: _details.OnError));
        _image?.Dispose();
        _image = null;
    }

    public override string ToString() =>
        $"DecorationImagePainter(stream: {_imageStream?.ToString() ?? "null"}, "
        + $"image: {_image?.ToString() ?? "null"}) for {_details}";
}

/// <summary>Paints an image into the given rectangle on the canvas.</summary>
/// <remarks>Dart's top-level <c>paintImage</c> and the bookkeeping behind its debug image-size reports.</remarks>
public static class ImagePainting
{
    private static Dictionary<string, ImageSizeInfo> s_pendingImageSizeInfo = new();
    private static HashSet<ImageSizeInfo> s_lastFrameImageSizeInfo = new();

    // The colour matrix debugInvertOversizedImages paints an oversized image through.
    private static readonly double[] InvertedImageMatrix =
    [
        -1, 0, 0, 0, 255,
        0, -1, 0, 0, 255,
        0, 0, -1, 0, 255,
        0, 0, 0, 1, 0,
    ];

    /// <summary>
    /// Used by tests to reset any tracking of which images have been painted in the last frame.
    /// </summary>
    /// <remarks>Dart's <c>debugFlushLastFrameImageSizeInfo</c>; a no-op outside debug builds.</remarks>
    public static void DebugFlushLastFrameImageSizeInfo()
    {
        if (Constants.KDebugMode)
        {
            s_lastFrameImageSizeInfo = new HashSet<ImageSizeInfo>();
        }
    }

    /// <summary>Paints an image into the given rectangle on the canvas.</summary>
    /// <remarks>
    /// Dart's <c>paintImage</c>. The image is fitted into <paramref name="rect"/> by <paramref name="fit"/>
    /// (default <see cref="BoxFit.ScaleDown"/>, or <see cref="BoxFit.Fill"/> with a
    /// <paramref name="centerSlice"/>), placed by <paramref name="alignment"/>, repeated per
    /// <paramref name="repeat"/> and drawn with a paint carrying <paramref name="opacity"/>,
    /// <paramref name="colorFilter"/>, <paramref name="filterQuality"/>, <paramref name="invertColors"/>,
    /// <paramref name="isAntiAlias"/> and <paramref name="blendMode"/>.
    /// </remarks>
    public static void PaintImage(
        Canvas canvas,
        Rect rect,
        IImage image,
        string? debugImageLabel = null,
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
        BlendMode blendMode = BlendMode.SourceOver)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(image);
        if (rect.Width <= 0.0 || rect.Height <= 0.0)
        {
            return;
        }

        Size imagePixels = Canvas.ImageSize(image);
        double outputWidth = rect.Width;
        double outputHeight = rect.Height;
        double inputWidth = imagePixels.Width;
        double inputHeight = imagePixels.Height;
        double sliceBorderX = 0.0;
        double sliceBorderY = 0.0;
        if (centerSlice is { } slice)
        {
            sliceBorderX = (inputWidth / scale) - slice.Width;
            sliceBorderY = (inputHeight / scale) - slice.Height;
            outputWidth -= sliceBorderX;
            outputHeight -= sliceBorderY;
            inputWidth -= sliceBorderX * scale;
            inputHeight -= sliceBorderY * scale;
        }

        fit ??= centerSlice is null ? BoxFit.ScaleDown : BoxFit.Fill;
        if (Constants.KDebugMode && centerSlice is not null && fit is BoxFit.None or BoxFit.Cover)
        {
            throw new AssertionError("centerSlice == null || (fit != BoxFit.none && fit != BoxFit.cover)");
        }

        FittedSizes fittedSizes = ApplyBoxFit(
            fit.Value,
            inputWidth / scale,
            inputHeight / scale,
            outputWidth,
            outputHeight);
        double sourceWidth = fittedSizes.Source.Width * scale;
        double sourceHeight = fittedSizes.Source.Height * scale;
        double destinationWidth = fittedSizes.Destination.Width;
        double destinationHeight = fittedSizes.Destination.Height;
        if (centerSlice is not null)
        {
            outputWidth += sliceBorderX;
            outputHeight += sliceBorderY;
            destinationWidth += sliceBorderX;
            destinationHeight += sliceBorderY;
            if (Constants.KDebugMode && (sourceWidth != inputWidth || sourceHeight != inputHeight))
            {
                throw new AssertionError(
                    "centerSlice was used with a BoxFit that does not guarantee that the image is fully visible.");
            }
        }

        if (repeat != ImageRepeat.NoRepeat && destinationWidth == outputWidth && destinationHeight == outputHeight)
        {
            // There's no need to repeat the image because we're exactly filling the
            // output rect with the image.
            repeat = ImageRepeat.NoRepeat;
        }

        var paint = new Paint { IsAntiAlias = isAntiAlias };
        if (colorFilter is not null)
        {
            paint.ColorFilter = colorFilter;
        }

        paint.Color = Color.FromRGBO(0, 0, 0, Math.Clamp(opacity, 0.0, 1.0));
        paint.FilterQuality = filterQuality;
        paint.InvertColors = invertColors;
        paint.BlendMode = blendMode;
        double halfWidthDelta = (outputWidth - destinationWidth) / 2.0;
        double halfHeightDelta = (outputHeight - destinationHeight) / 2.0;
        double dx = halfWidthDelta + ((flipHorizontally ? -alignment.X : alignment.X) * halfWidthDelta);
        double dy = halfHeightDelta + (alignment.Y * halfHeightDelta);
        var destinationPosition = new Point(rect.Left + dx, rect.Top + dy);
        var destinationRect = new Rect(
            destinationPosition,
            new Size(Math.Max(0.0, destinationWidth), Math.Max(0.0, destinationHeight)));

        // Set to true if we added a saveLayer to the canvas to invert/flip the image.
        bool invertedCanvas = false;

        // Output size and destination rect are fully calculated.

        // Implement debug-mode and profile-mode features:
        //  - cacheWidth/cacheHeight warning
        //  - debugInvertOversizedImages
        //  - debugOnPaintImage
        //  - Flutter.ImageSizesForFrame events in timeline
        if (!Constants.KReleaseMode)
        {
            // We can use the devicePixelRatio of the views directly here (instead of going through a
            // MediaQuery) because if it changes, whatever is aware of the MediaQuery will be repainting the
            // image anyways. Furthermore, for the memory check below we just assume that all images are
            // decoded for the view with the highest device pixel ratio and use that as an upper bound for
            // the display size of the image.
            double maxDevicePixelRatio = PlatformDispatcher.Instance.Views.Aggregate(
                0.0,
                static (previousValue, view) => Math.Max(previousValue, view.DevicePixelRatio));
            int imageWidth = (int)imagePixels.Width;
            int imageHeight = (int)imagePixels.Height;
            var sizeInfo = new ImageSizeInfo(
                // Some ImageProvider implementations may not have given this.
                source: debugImageLabel ?? $"<Unknown Image({imageWidth}×{imageHeight})>",
                imageSize: new Size(imagePixels.Width, imagePixels.Height),
                displaySize: new Size(outputWidth * maxDevicePixelRatio, outputHeight * maxDevicePixelRatio));
            if (Constants.KDebugMode
                && PaintingDebug.DebugInvertOversizedImages
                && sizeInfo.DecodedSizeInBytes
                > sizeInfo.DisplaySizeInBytes + PaintingDebug.DebugImageOverheadAllowance)
            {
                int overheadInKilobytes = (sizeInfo.DecodedSizeInBytes - sizeInfo.DisplaySizeInBytes) / 1024;
                int displayWidth = (int)sizeInfo.DisplaySize.Width;
                int displayHeight = (int)sizeInfo.DisplaySize.Height;
                FlutterError.ReportError(new FlutterErrorDetails(
                    exception: $"Image {debugImageLabel ?? "null"} has a display size of "
                               + $"{displayWidth}×{displayHeight} but a decode size of "
                               + $"{imageWidth}×{imageHeight}, which uses an additional "
                               + $"{overheadInKilobytes}KB (assuming a device pixel ratio of "
                               + $"{DartFormat.Number(maxDevicePixelRatio)}).\n\n"
                               + "Consider resizing the asset ahead of time, supplying a cacheWidth "
                               + $"parameter of {displayWidth}, a cacheHeight parameter of "
                               + $"{displayHeight}, or using a ResizeImage.",
                    library: "painting library",
                    context: new ErrorDescription("while painting an image")));

                // Invert the colors of the canvas.
                canvas.SaveLayer(
                    destinationRect,
                    new Paint { ColorFilter = new ColorFilter.Matrix(InvertedImageMatrix) });

                // Flip the canvas vertically.
                double flipDy = -(rect.Top + (rect.Height / 2.0));
                canvas.Translate(0.0, -flipDy);
                canvas.Scale(1.0, -1.0);
                canvas.Translate(0.0, flipDy);
                invertedCanvas = true;
            }

            // Avoid emitting events that are the same as those emitted in the last frame.
            if (!s_lastFrameImageSizeInfo.Contains(sizeInfo))
            {
                s_pendingImageSizeInfo.TryGetValue(sizeInfo.Source!, out ImageSizeInfo? existingSizeInfo);
                if (existingSizeInfo is null
                    || existingSizeInfo.DisplaySizeInBytes < sizeInfo.DisplaySizeInBytes)
                {
                    s_pendingImageSizeInfo[sizeInfo.Source!] = sizeInfo;
                }

                PaintingDebug.DebugOnPaintImage?.Invoke(sizeInfo);
                SchedulerBinding.Instance.AddPostFrameCallback(
                    _ =>
                    {
                        s_lastFrameImageSizeInfo = s_pendingImageSizeInfo.Values.ToHashSet();
                        if (s_pendingImageSizeInfo.Count == 0)
                        {
                            return;
                        }

                        var eventData = new Dictionary<string, object?>();
                        foreach (ImageSizeInfo imageSizeInfo in s_pendingImageSizeInfo.Values)
                        {
                            eventData[imageSizeInfo.Source!] = imageSizeInfo.ToJson();
                        }

                        DeveloperService.PostEvent("Flutter.ImageSizesForFrame", eventData);
                        s_pendingImageSizeInfo = new Dictionary<string, ImageSizeInfo>();
                    },
                    debugLabel: "paintImage.recordImageSizes");
            }
        }

        bool needSave = centerSlice is not null || repeat != ImageRepeat.NoRepeat || flipHorizontally;
        if (needSave)
        {
            canvas.Save();
        }

        if (repeat != ImageRepeat.NoRepeat)
        {
            canvas.ClipRect(rect);
        }

        if (flipHorizontally)
        {
            double flipDx = -(rect.Left + (rect.Width / 2.0));
            canvas.Translate(-flipDx, 0.0);
            canvas.Scale(-1.0, 1.0);
            canvas.Translate(flipDx, 0.0);
        }

        if (centerSlice is null)
        {
            Rect sourceRect = alignment.Inscribe(
                new Size(Math.Max(0.0, sourceWidth), Math.Max(0.0, sourceHeight)),
                new Rect(0.0, 0.0, Math.Max(0.0, inputWidth), Math.Max(0.0, inputHeight)));
            if (repeat == ImageRepeat.NoRepeat)
            {
                canvas.DrawImageRect(image, sourceRect, destinationRect, paint);
            }
            else
            {
                foreach (Rect tileRect in GenerateImageTileRects(rect, destinationRect, repeat))
                {
                    canvas.DrawImageRect(image, sourceRect, tileRect, paint);
                }
            }
        }
        else
        {
            canvas.Scale(1 / scale);
            if (repeat == ImageRepeat.NoRepeat)
            {
                canvas.DrawImageNine(
                    image,
                    ScaleRect(centerSlice.Value, scale),
                    ScaleRect(destinationRect, scale),
                    paint);
            }
            else
            {
                foreach (Rect tileRect in GenerateImageTileRects(rect, destinationRect, repeat))
                {
                    canvas.DrawImageNine(
                        image,
                        ScaleRect(centerSlice.Value, scale),
                        ScaleRect(tileRect, scale),
                        paint);
                }
            }
        }

        if (needSave)
        {
            canvas.Restore();
        }

        if (invertedCanvas)
        {
            canvas.Restore();
        }
    }

    // Dart's `applyBoxFit`, over sizes that may be negative after a center slice is taken out (Dart's Size
    // keeps them; a non-positive size fits to nothing either way).
    private static FittedSizes ApplyBoxFit(
        BoxFit fit,
        double inputWidth,
        double inputHeight,
        double outputWidth,
        double outputHeight)
    {
        if (inputWidth <= 0.0 || inputHeight <= 0.0 || outputWidth <= 0.0 || outputHeight <= 0.0)
        {
            return new FittedSizes(default, default);
        }

        return BoxFitUtils.ApplyBoxFit(fit, new Size(inputWidth, inputHeight), new Size(outputWidth, outputHeight));
    }

    internal static IEnumerable<Rect> GenerateImageTileRects(
        Rect outputRect,
        Rect fundamentalRect,
        ImageRepeat repeat)
    {
        int startX = 0;
        int startY = 0;
        int stopX = 0;
        int stopY = 0;
        double strideX = fundamentalRect.Width;
        double strideY = fundamentalRect.Height;

        if (repeat is ImageRepeat.Repeat or ImageRepeat.RepeatX)
        {
            startX = (int)Math.Floor((outputRect.Left - fundamentalRect.Left) / strideX);
            stopX = (int)Math.Ceiling((outputRect.Right - fundamentalRect.Right) / strideX);
        }

        if (repeat is ImageRepeat.Repeat or ImageRepeat.RepeatY)
        {
            startY = (int)Math.Floor((outputRect.Top - fundamentalRect.Top) / strideY);
            stopY = (int)Math.Ceiling((outputRect.Bottom - fundamentalRect.Bottom) / strideY);
        }

        for (int i = startX; i <= stopX; ++i)
        {
            for (int j = startY; j <= stopY; ++j)
            {
                yield return fundamentalRect.Translate(new Vector(i * strideX, j * strideY));
            }
        }
    }

    private static Rect ScaleRect(Rect rect, double scale) =>
        new Rect(
            new Point(rect.Left * scale, rect.Top * scale),
            new Point(rect.Right * scale, rect.Bottom * scale));

    /// <summary>dart:ui <c>Rect.toString</c>.</summary>
    internal static string RectToString(Rect rect) =>
        $"Rect.fromLTRB({DartFormat.Fixed(rect.Left)}, {DartFormat.Fixed(rect.Top)}, "
        + $"{DartFormat.Fixed(rect.Right)}, {DartFormat.Fixed(rect.Bottom)})";
}

// Dart's `_BlendedDecorationImage implements DecorationImage`.
internal sealed class BlendedDecorationImage : DecorationImage
{
    public BlendedDecorationImage(DecorationImage? a, DecorationImage? b, double t)
        : base((b ?? a)?.Image ?? throw new ArgumentException("a or b must be non-null."))
    {
        A = a;
        B = b;
        T = t;
    }

    public DecorationImage? A { get; }

    public DecorationImage? B { get; }

    public double T { get; }

    public override ImageProvider Image => B?.Image ?? A!.Image;

    public override ImageErrorListener? OnError => B?.OnError ?? A!.OnError;

    public override ColorFilter? ColorFilter => B?.ColorFilter ?? A!.ColorFilter;

    public override BoxFit? Fit => B?.Fit ?? A!.Fit;

    public override AlignmentGeometry Alignment => B?.Alignment ?? A!.Alignment;

    public override Rect? CenterSlice => B?.CenterSlice ?? A!.CenterSlice;

    public override ImageRepeat Repeat => B?.Repeat ?? A!.Repeat;

    public override bool MatchTextDirection => B?.MatchTextDirection ?? A!.MatchTextDirection;

    public override double Scale => B?.Scale ?? A!.Scale;

    public override double Opacity => B?.Opacity ?? A!.Opacity;

    public override FilterQuality FilterQuality => B?.FilterQuality ?? A!.FilterQuality;

    public override bool InvertColors => B?.InvertColors ?? A!.InvertColors;

    public override bool IsAntiAlias => B?.IsAntiAlias ?? A!.IsAntiAlias;

    public override DecorationImagePainter CreatePainter(Action onChanged)
    {
        return new BlendedDecorationImagePainter(A?.CreatePainter(onChanged), B?.CreatePainter(onChanged), T);
    }

    public override bool Equals(DecorationImage? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is BlendedDecorationImage blended
               && Equals(blended.A, A)
               && Equals(blended.B, B)
               && blended.T.Equals(T);
    }

    public override int GetHashCode() => HashCode.Combine(A, B, T);

    public override string ToString() =>
        $"_BlendedDecorationImage({A?.ToString() ?? "null"}, {B?.ToString() ?? "null"}, {DartFormat.Number(T)})";
}

// Dart's `_BlendedDecorationImagePainter`.
internal sealed class BlendedDecorationImagePainter : DecorationImagePainter
{
    public BlendedDecorationImagePainter(DecorationImagePainter? a, DecorationImagePainter? b, double t)
    {
        A = a;
        B = b;
        T = t;
        if (Constants.KDebugMode)
        {
            FoundationDebug.DebugMaybeDispatchCreated("painting", "_BlendedDecorationImagePainter", this);
        }
    }

    public DecorationImagePainter? A { get; }

    public DecorationImagePainter? B { get; }

    public double T { get; }

    public override void Paint(
        Canvas canvas,
        Rect rect,
        Path? clipPath,
        ImageConfiguration configuration,
        double blend = 1.0,
        BlendMode blendMode = BlendMode.SourceOver)
    {
        canvas.SaveLayer(null, new Paint());
        A?.Paint(canvas, rect, clipPath, configuration, blend: blend * (1.0 - T), blendMode: blendMode);
        B?.Paint(
            canvas,
            rect,
            clipPath,
            configuration,
            blend: blend * T,
            blendMode: A is not null ? BlendMode.Plus : blendMode);
        canvas.Restore();
    }

    public override void Dispose()
    {
        if (Constants.KDebugMode)
        {
            FoundationDebug.DebugMaybeDispatchDisposed(this);
        }

        A?.Dispose();
        B?.Dispose();
    }

    public override string ToString() =>
        $"_BlendedDecorationImagePainter({A?.ToString() ?? "null"}, {B?.ToString() ?? "null"}, "
        + $"{DartFormat.Number(T)})";
}
