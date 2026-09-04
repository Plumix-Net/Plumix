using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity sources:
// - flutter/packages/flutter/lib/src/widgets/image.dart (Image)
// - flutter/packages/flutter/lib/src/widgets/basic.dart (RawImage)

public delegate Widget ImageFrameBuilder(
    BuildContext context,
    Widget child,
    int? frame,
    bool wasSynchronouslyLoaded);

public delegate Widget ImageLoadingBuilder(
    BuildContext context,
    Widget child,
    ImageChunkEvent? loadingProgress);

public delegate Widget ImageErrorWidgetBuilder(
    BuildContext context,
    Exception error,
    System.Diagnostics.StackTrace? stackTrace);

public sealed class Image : StatefulWidget
{
    public Image(
        ImageProvider image,
        ImageFrameBuilder? frameBuilder = null,
        ImageLoadingBuilder? loadingBuilder = null,
        ImageErrorWidgetBuilder? errorBuilder = null,
        string? semanticLabel = null,
        bool excludeFromSemantics = false,
        double? width = null,
        double? height = null,
        Color? color = null,
        IValueListenable<double>? opacity = null,
        BitmapBlendingMode? colorBlendMode = null,
        BoxFit? fit = null,
        AlignmentGeometry alignment = default,
        ImageRepeat repeat = ImageRepeat.NoRepeat,
        Rect? centerSlice = null,
        bool matchTextDirection = false,
        bool gaplessPlayback = false,
        bool isAntiAlias = false,
        FilterQuality filterQuality = FilterQuality.Medium,
        Key? key = null) : base(key)
    {
        ValidateDimension(width, nameof(width));
        ValidateDimension(height, nameof(height));
        ImageProvider = image ?? throw new ArgumentNullException(nameof(image));
        FrameBuilder = frameBuilder;
        LoadingBuilder = loadingBuilder;
        ErrorBuilder = errorBuilder;
        SemanticLabel = semanticLabel;
        ExcludeFromSemantics = excludeFromSemantics;
        Width = width;
        Height = height;
        Color = color;
        Opacity = opacity;
        ColorBlendMode = colorBlendMode;
        Fit = fit;
        Alignment = alignment;
        Repeat = repeat;
        CenterSlice = centerSlice;
        MatchTextDirection = matchTextDirection;
        GaplessPlayback = gaplessPlayback;
        IsAntiAlias = isAntiAlias;
        FilterQuality = filterQuality;
    }

    public ImageProvider ImageProvider { get; }
    public ImageFrameBuilder? FrameBuilder { get; }
    public ImageLoadingBuilder? LoadingBuilder { get; }
    public ImageErrorWidgetBuilder? ErrorBuilder { get; }
    public string? SemanticLabel { get; }
    public bool ExcludeFromSemantics { get; }
    public double? Width { get; }
    public double? Height { get; }
    public Color? Color { get; }
    public IValueListenable<double>? Opacity { get; }
    public BitmapBlendingMode? ColorBlendMode { get; }
    public BoxFit? Fit { get; }
    public AlignmentGeometry Alignment { get; }
    public ImageRepeat Repeat { get; }
    public Rect? CenterSlice { get; }
    public bool MatchTextDirection { get; }
    public bool GaplessPlayback { get; }
    public bool IsAntiAlias { get; }
    public FilterQuality FilterQuality { get; }

    public static Image Network(
        string source,
        double scale = 1.0,
        IReadOnlyDictionary<string, string>? headers = null,
        int? cacheWidth = null,
        int? cacheHeight = null,
        ImageFrameBuilder? frameBuilder = null,
        ImageLoadingBuilder? loadingBuilder = null,
        ImageErrorWidgetBuilder? errorBuilder = null,
        string? semanticLabel = null,
        bool excludeFromSemantics = false,
        double? width = null,
        double? height = null,
        Color? color = null,
        IValueListenable<double>? opacity = null,
        BitmapBlendingMode? colorBlendMode = null,
        BoxFit? fit = null,
        AlignmentGeometry alignment = default,
        ImageRepeat repeat = ImageRepeat.NoRepeat,
        Rect? centerSlice = null,
        bool matchTextDirection = false,
        bool gaplessPlayback = false,
        bool isAntiAlias = false,
        FilterQuality filterQuality = FilterQuality.Medium,
        Key? key = null)
    {
        ValidateCacheDimensions(cacheWidth, cacheHeight);
        return new Image(
            image: ResizeImage.ResizeIfNeeded(
                cacheWidth,
                cacheHeight,
                new NetworkImage(source, scale, headers)),
            frameBuilder: frameBuilder,
            loadingBuilder: loadingBuilder,
            errorBuilder: errorBuilder,
            semanticLabel: semanticLabel,
            excludeFromSemantics: excludeFromSemantics,
            width: width,
            height: height,
            color: color,
            opacity: opacity,
            colorBlendMode: colorBlendMode,
            fit: fit,
            alignment: alignment,
            repeat: repeat,
            centerSlice: centerSlice,
            matchTextDirection: matchTextDirection,
            gaplessPlayback: gaplessPlayback,
            isAntiAlias: isAntiAlias,
            filterQuality: filterQuality,
            key: key);
    }

    public static Image File(
        string filePath,
        double scale = 1.0,
        int? cacheWidth = null,
        int? cacheHeight = null,
        ImageFrameBuilder? frameBuilder = null,
        ImageErrorWidgetBuilder? errorBuilder = null,
        string? semanticLabel = null,
        bool excludeFromSemantics = false,
        double? width = null,
        double? height = null,
        Color? color = null,
        IValueListenable<double>? opacity = null,
        BitmapBlendingMode? colorBlendMode = null,
        BoxFit? fit = null,
        AlignmentGeometry alignment = default,
        ImageRepeat repeat = ImageRepeat.NoRepeat,
        Rect? centerSlice = null,
        bool matchTextDirection = false,
        bool gaplessPlayback = false,
        bool isAntiAlias = false,
        FilterQuality filterQuality = FilterQuality.Medium,
        Key? key = null)
    {
        ValidateCacheDimensions(cacheWidth, cacheHeight);
        return new Image(
            image: ResizeImage.ResizeIfNeeded(cacheWidth, cacheHeight, new FileImage(filePath, scale)),
            frameBuilder: frameBuilder,
            errorBuilder: errorBuilder,
            semanticLabel: semanticLabel,
            excludeFromSemantics: excludeFromSemantics,
            width: width,
            height: height,
            color: color,
            opacity: opacity,
            colorBlendMode: colorBlendMode,
            fit: fit,
            alignment: alignment,
            repeat: repeat,
            centerSlice: centerSlice,
            matchTextDirection: matchTextDirection,
            gaplessPlayback: gaplessPlayback,
            isAntiAlias: isAntiAlias,
            filterQuality: filterQuality,
            key: key);
    }

    public static Image Asset(
        string name,
        AssetBundle? bundle = null,
        double? scale = null,
        string? package = null,
        int? cacheWidth = null,
        int? cacheHeight = null,
        ImageFrameBuilder? frameBuilder = null,
        ImageErrorWidgetBuilder? errorBuilder = null,
        string? semanticLabel = null,
        bool excludeFromSemantics = false,
        double? width = null,
        double? height = null,
        Color? color = null,
        IValueListenable<double>? opacity = null,
        BitmapBlendingMode? colorBlendMode = null,
        BoxFit? fit = null,
        AlignmentGeometry alignment = default,
        ImageRepeat repeat = ImageRepeat.NoRepeat,
        Rect? centerSlice = null,
        bool matchTextDirection = false,
        bool gaplessPlayback = false,
        bool isAntiAlias = false,
        FilterQuality filterQuality = FilterQuality.Medium,
        Key? key = null)
    {
        ValidateCacheDimensions(cacheWidth, cacheHeight);
        ImageProvider provider = scale.HasValue
            ? new ExactAssetImage(name, scale.Value, bundle, package)
            : new AssetImage(name, bundle, package);
        return new Image(
            image: ResizeImage.ResizeIfNeeded(cacheWidth, cacheHeight, provider),
            frameBuilder: frameBuilder,
            errorBuilder: errorBuilder,
            semanticLabel: semanticLabel,
            excludeFromSemantics: excludeFromSemantics,
            width: width,
            height: height,
            color: color,
            opacity: opacity,
            colorBlendMode: colorBlendMode,
            fit: fit,
            alignment: alignment,
            repeat: repeat,
            centerSlice: centerSlice,
            matchTextDirection: matchTextDirection,
            gaplessPlayback: gaplessPlayback,
            isAntiAlias: isAntiAlias,
            filterQuality: filterQuality,
            key: key);
    }

    public static Image Memory(
        byte[] bytes,
        double scale = 1.0,
        int? cacheWidth = null,
        int? cacheHeight = null,
        ImageFrameBuilder? frameBuilder = null,
        ImageErrorWidgetBuilder? errorBuilder = null,
        string? semanticLabel = null,
        bool excludeFromSemantics = false,
        double? width = null,
        double? height = null,
        Color? color = null,
        IValueListenable<double>? opacity = null,
        BitmapBlendingMode? colorBlendMode = null,
        BoxFit? fit = null,
        AlignmentGeometry alignment = default,
        ImageRepeat repeat = ImageRepeat.NoRepeat,
        Rect? centerSlice = null,
        bool matchTextDirection = false,
        bool gaplessPlayback = false,
        bool isAntiAlias = false,
        FilterQuality filterQuality = FilterQuality.Medium,
        Key? key = null)
    {
        ValidateCacheDimensions(cacheWidth, cacheHeight);
        return new Image(
            image: ResizeImage.ResizeIfNeeded(cacheWidth, cacheHeight, new MemoryImage(bytes, scale)),
            frameBuilder: frameBuilder,
            errorBuilder: errorBuilder,
            semanticLabel: semanticLabel,
            excludeFromSemantics: excludeFromSemantics,
            width: width,
            height: height,
            color: color,
            opacity: opacity,
            colorBlendMode: colorBlendMode,
            fit: fit,
            alignment: alignment,
            repeat: repeat,
            centerSlice: centerSlice,
            matchTextDirection: matchTextDirection,
            gaplessPlayback: gaplessPlayback,
            isAntiAlias: isAntiAlias,
            filterQuality: filterQuality,
            key: key);
    }

    public override State CreateState() => new ImageState();

    private static void ValidateCacheDimensions(int? cacheWidth, int? cacheHeight)
    {
        if (cacheWidth is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cacheWidth));
        }

        if (cacheHeight is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cacheHeight));
        }
    }

    private static void ValidateDimension(double? value, string parameterName)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0.0))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Image dimensions must be finite and non-negative.");
        }
    }

    private sealed class ImageState : State
    {
        private ImageStream? _imageStream;
        private ImageStreamListener? _imageStreamListener;
        private ImageInfo? _imageInfo;
        private ImageChunkEvent? _loadingProgress;
        private Exception? _lastException;
        private System.Diagnostics.StackTrace? _lastStack;
        private int? _frameNumber;
        private bool _wasSynchronouslyLoaded;
        private bool _isListeningToStream;
        private bool _isPaused;
        private ImageStreamCompleterHandle? _completerHandle;

        private Image CurrentWidget => (Image)StateWidget;

        public override void DidChangeDependencies()
        {
            ResolveImage();
            _isPaused = !TickerMode.Of(Context) || (MediaQuery.MaybeDisableAnimationsOf(Context) ?? false);
            if (_isPaused && _frameNumber.HasValue)
            {
                StopListeningToStream(keepStreamAlive: true);
            }
            else
            {
                ListenToStream();
            }
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldImage = (Image)oldWidget;
            if (_isListeningToStream
                && (CurrentWidget.LoadingBuilder is null) != (oldImage.LoadingBuilder is null))
            {
                ImageStreamListener oldListener = GetListener();
                _imageStreamListener = null;
                _imageStream!.AddListener(GetListener());
                _imageStream.RemoveListener(oldListener);
            }

            if (!Equals(CurrentWidget.ImageProvider, oldImage.ImageProvider))
            {
                ResolveImage();
                ListenToStream();
            }
        }

        public override void Reassemble()
        {
            ResolveImage();
        }

        public override Widget Build(BuildContext context)
        {
            if (_lastException is not null && CurrentWidget.ErrorBuilder is not null)
            {
                return CurrentWidget.ErrorBuilder(context, _lastException, _lastStack);
            }

            Widget result = new RawImage(
                image: _imageInfo?.Image,
                debugImageLabel: _imageInfo?.DebugLabel,
                width: CurrentWidget.Width,
                height: CurrentWidget.Height,
                scale: _imageInfo?.Scale ?? 1.0,
                color: CurrentWidget.Color,
                opacity: CurrentWidget.Opacity,
                colorBlendMode: CurrentWidget.ColorBlendMode,
                fit: CurrentWidget.Fit,
                alignment: CurrentWidget.Alignment,
                repeat: CurrentWidget.Repeat,
                centerSlice: CurrentWidget.CenterSlice,
                matchTextDirection: CurrentWidget.MatchTextDirection,
                invertColors: MediaQuery.MaybeInvertColorsOf(context) ?? false,
                isAntiAlias: CurrentWidget.IsAntiAlias,
                filterQuality: CurrentWidget.FilterQuality);

            if (!CurrentWidget.ExcludeFromSemantics)
            {
                result = new Semantics(
                    child: result,
                    label: CurrentWidget.SemanticLabel ?? string.Empty,
                    flags: SemanticsFlags.IsImage,
                    container: CurrentWidget.SemanticLabel is not null);
            }

            if (CurrentWidget.FrameBuilder is not null)
            {
                result = CurrentWidget.FrameBuilder(
                    context,
                    result,
                    _frameNumber,
                    _wasSynchronouslyLoaded);
            }

            if (CurrentWidget.LoadingBuilder is not null)
            {
                result = CurrentWidget.LoadingBuilder(context, result, _loadingProgress);
            }

            return result;
        }

        public override void Dispose()
        {
            StopListeningToStream();
            _completerHandle?.Dispose();
            _completerHandle = null;
            ReplaceImage(null);
        }

        private void ResolveImage()
        {
            var configuration = ImageConfigurationUtils.CreateLocalImageConfiguration(
                Context,
                CurrentWidget.Width.HasValue && CurrentWidget.Height.HasValue
                    ? new Size(CurrentWidget.Width.Value, CurrentWidget.Height.Value)
                    : null);
            UpdateSourceStream(CurrentWidget.ImageProvider.Resolve(configuration));
        }

        private ImageStreamListener GetListener()
        {
            return _imageStreamListener ??= new ImageStreamListener(
                OnImage: HandleImageFrame,
                OnChunk: CurrentWidget.LoadingBuilder is null ? null : HandleImageChunk,
                OnError: CurrentWidget.ErrorBuilder is null ? null : HandleImageError,
                // Only suppress error reporting when errorBuilder is provided.
                ReportErrors: CurrentWidget.ErrorBuilder is null);
        }

        private void HandleImageFrame(ImageInfo imageInfo, bool synchronousCall)
        {
            SetState(() =>
            {
                ReplaceImage(imageInfo);
                _loadingProgress = null;
                _lastException = null;
                _lastStack = null;
                _frameNumber = _frameNumber.HasValue ? _frameNumber.Value + 1 : 0;
                _wasSynchronouslyLoaded |= synchronousCall;
            });
            if (_isPaused)
            {
                StopListeningToStream(keepStreamAlive: true);
            }
        }

        private void HandleImageChunk(ImageChunkEvent @event)
        {
            SetState(() =>
            {
                _loadingProgress = @event;
                _lastException = null;
                _lastStack = null;
            });
        }

        private void HandleImageError(Exception exception, System.Diagnostics.StackTrace? stackTrace)
        {
            SetState(() =>
            {
                _lastException = exception;
                _lastStack = stackTrace;
            });
        }

        private void ReplaceImage(ImageInfo? info)
        {
            ImageInfo? oldImage = _imageInfo;
            _imageInfo = info;
            if (oldImage is not null)
            {
                Scheduler.AddPostFrameCallback(_ => oldImage.Dispose());
            }
        }

        private void UpdateSourceStream(ImageStream newStream)
        {
            if (Equals(_imageStream?.Key, newStream.Key))
            {
                return;
            }

            if (_isListeningToStream)
            {
                _imageStream!.RemoveListener(GetListener());
            }

            if (!CurrentWidget.GaplessPlayback)
            {
                ReplaceImage(null);
            }

            _loadingProgress = null;
            _lastException = null;
            _lastStack = null;
            _frameNumber = null;
            _wasSynchronouslyLoaded = false;
            _imageStream = newStream;
            _imageStreamListener = null;
            if (_isListeningToStream)
            {
                _imageStream.AddListener(GetListener());
            }
        }

        private void ListenToStream()
        {
            if (_isListeningToStream)
            {
                return;
            }

            _isListeningToStream = true;
            _imageStream!.AddListener(GetListener());
            _completerHandle?.Dispose();
            _completerHandle = null;
        }

        private void StopListeningToStream(bool keepStreamAlive = false)
        {
            if (!_isListeningToStream)
            {
                return;
            }

            if (keepStreamAlive && _completerHandle is null && _imageStream?.Completer is not null)
            {
                _completerHandle = _imageStream.Completer.KeepAlive();
            }

            if (_imageStream?.Completer is not null && CurrentWidget.ErrorBuilder is not null)
            {
                _imageStream.Completer.AddEphemeralErrorListener((_, _) => { });
            }

            _imageStream!.RemoveListener(GetListener());
            _isListeningToStream = false;
        }
    }
}

public sealed class RawImage : LeafRenderObjectWidget
{
    public RawImage(
        IImage? image = null,
        string? debugImageLabel = null,
        double? width = null,
        double? height = null,
        double scale = 1.0,
        Color? color = null,
        IValueListenable<double>? opacity = null,
        BitmapBlendingMode? colorBlendMode = null,
        BoxFit? fit = null,
        AlignmentGeometry alignment = default,
        ImageRepeat repeat = ImageRepeat.NoRepeat,
        Rect? centerSlice = null,
        bool matchTextDirection = false,
        bool invertColors = false,
        FilterQuality filterQuality = FilterQuality.Medium,
        bool isAntiAlias = false,
        Key? key = null) : base(key)
    {
        Image = image;
        DebugImageLabel = debugImageLabel;
        Width = width;
        Height = height;
        Scale = MemoryImage.ValidateScale(scale);
        Color = color;
        Opacity = opacity;
        ColorBlendMode = colorBlendMode;
        Fit = fit;
        Alignment = alignment;
        Repeat = repeat;
        CenterSlice = centerSlice;
        MatchTextDirection = matchTextDirection;
        InvertColors = invertColors;
        FilterQuality = filterQuality;
        IsAntiAlias = isAntiAlias;
    }

    public IImage? Image { get; }
    public string? DebugImageLabel { get; }
    public double? Width { get; }
    public double? Height { get; }
    public double Scale { get; }
    public Color? Color { get; }
    public IValueListenable<double>? Opacity { get; }
    public BitmapBlendingMode? ColorBlendMode { get; }
    public BoxFit? Fit { get; }
    public AlignmentGeometry Alignment { get; }
    public ImageRepeat Repeat { get; }
    public Rect? CenterSlice { get; }
    public bool MatchTextDirection { get; }
    public bool InvertColors { get; }
    public FilterQuality FilterQuality { get; }
    public bool IsAntiAlias { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderImage(
            image: Image,
            debugImageLabel: DebugImageLabel,
            width: Width,
            height: Height,
            scale: Scale,
            color: Color,
            opacity: Opacity,
            colorBlendMode: ColorBlendMode,
            fit: Fit,
            alignment: Alignment,
            repeat: Repeat,
            centerSlice: CenterSlice,
            matchTextDirection: MatchTextDirection,
            textDirection: MatchTextDirection || Alignment.IsDirectional ? Directionality.Of(context) : null,
            invertColors: InvertColors,
            isAntiAlias: IsAntiAlias,
            filterQuality: FilterQuality);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var image = (RenderImage)renderObject;
        image.Image = Image;
        image.DebugImageLabel = DebugImageLabel;
        image.Width = Width;
        image.Height = Height;
        image.Scale = Scale;
        image.Color = Color;
        image.Opacity = Opacity;
        image.ColorBlendMode = ColorBlendMode;
        image.Fit = Fit;
        image.Alignment = Alignment;
        image.Repeat = Repeat;
        image.CenterSlice = CenterSlice;
        image.MatchTextDirection = MatchTextDirection;
        image.TextDirection = MatchTextDirection || Alignment.IsDirectional ? Directionality.Of(context) : null;
        image.InvertColors = InvertColors;
        image.IsAntiAlias = IsAntiAlias;
        image.FilterQuality = FilterQuality;
    }
}
