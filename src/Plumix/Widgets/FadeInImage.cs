using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source:
// flutter/packages/flutter/lib/src/widgets/fade_in_image.dart

public sealed class FadeInImage : StatefulWidget
{
    private static readonly Animation<double> OpaqueAnimation = new ConstantDoubleAnimation(1.0);

    public FadeInImage(
        ImageProvider placeholder,
        ImageProvider image,
        ImageErrorWidgetBuilder? placeholderErrorBuilder = null,
        ImageErrorWidgetBuilder? imageErrorBuilder = null,
        bool excludeFromSemantics = false,
        string? imageSemanticLabel = null,
        TimeSpan? fadeOutDuration = null,
        Curve? fadeOutCurve = null,
        TimeSpan? fadeInDuration = null,
        Curve? fadeInCurve = null,
        Color? color = null,
        BitmapBlendingMode? colorBlendMode = null,
        Color? placeholderColor = null,
        BitmapBlendingMode? placeholderColorBlendMode = null,
        double? width = null,
        double? height = null,
        BoxFit? fit = null,
        BoxFit? placeholderFit = null,
        FilterQuality filterQuality = FilterQuality.Medium,
        FilterQuality? placeholderFilterQuality = null,
        AlignmentGeometry alignment = default,
        ImageRepeat repeat = ImageRepeat.NoRepeat,
        bool matchTextDirection = false,
        Key? key = null) : base(key)
    {
        TimeSpan effectiveFadeOutDuration = fadeOutDuration ?? TimeSpan.FromMilliseconds(300);
        TimeSpan effectiveFadeInDuration = fadeInDuration ?? TimeSpan.FromMilliseconds(700);
        ValidateDuration(effectiveFadeOutDuration, nameof(fadeOutDuration));
        ValidateDuration(effectiveFadeInDuration, nameof(fadeInDuration));
        ValidateDimension(width, nameof(width));
        ValidateDimension(height, nameof(height));

        Placeholder = placeholder ?? throw new ArgumentNullException(nameof(placeholder));
        Image = image ?? throw new ArgumentNullException(nameof(image));
        PlaceholderErrorBuilder = placeholderErrorBuilder;
        ImageErrorBuilder = imageErrorBuilder;
        ExcludeFromSemantics = excludeFromSemantics;
        ImageSemanticLabel = imageSemanticLabel;
        FadeOutDuration = effectiveFadeOutDuration;
        FadeOutCurve = fadeOutCurve ?? Curves.EaseOut;
        FadeInDuration = effectiveFadeInDuration;
        FadeInCurve = fadeInCurve ?? Curves.EaseIn;
        Color = color;
        ColorBlendMode = colorBlendMode;
        PlaceholderColor = placeholderColor;
        PlaceholderColorBlendMode = placeholderColorBlendMode;
        Width = width;
        Height = height;
        Fit = fit;
        PlaceholderFit = placeholderFit;
        FilterQuality = filterQuality;
        PlaceholderFilterQuality = placeholderFilterQuality;
        Alignment = alignment;
        Repeat = repeat;
        MatchTextDirection = matchTextDirection;
    }

    public ImageProvider Placeholder { get; }

    public ImageErrorWidgetBuilder? PlaceholderErrorBuilder { get; }

    public ImageProvider Image { get; }

    public ImageErrorWidgetBuilder? ImageErrorBuilder { get; }

    public bool ExcludeFromSemantics { get; }

    public string? ImageSemanticLabel { get; }

    public TimeSpan FadeOutDuration { get; }

    public Curve FadeOutCurve { get; }

    public TimeSpan FadeInDuration { get; }

    public Curve FadeInCurve { get; }

    public Color? Color { get; }

    public BitmapBlendingMode? ColorBlendMode { get; }

    public Color? PlaceholderColor { get; }

    public BitmapBlendingMode? PlaceholderColorBlendMode { get; }

    public double? Width { get; }

    public double? Height { get; }

    public BoxFit? Fit { get; }

    public BoxFit? PlaceholderFit { get; }

    public FilterQuality FilterQuality { get; }

    public FilterQuality? PlaceholderFilterQuality { get; }

    public AlignmentGeometry Alignment { get; }

    public ImageRepeat Repeat { get; }

    public bool MatchTextDirection { get; }

    public static FadeInImage MemoryNetwork(
        byte[] placeholder,
        string image,
        ImageErrorWidgetBuilder? placeholderErrorBuilder = null,
        ImageErrorWidgetBuilder? imageErrorBuilder = null,
        double placeholderScale = 1.0,
        double imageScale = 1.0,
        bool excludeFromSemantics = false,
        string? imageSemanticLabel = null,
        TimeSpan? fadeOutDuration = null,
        Curve? fadeOutCurve = null,
        TimeSpan? fadeInDuration = null,
        Curve? fadeInCurve = null,
        double? width = null,
        double? height = null,
        BoxFit? fit = null,
        Color? color = null,
        BitmapBlendingMode? colorBlendMode = null,
        Color? placeholderColor = null,
        BitmapBlendingMode? placeholderColorBlendMode = null,
        BoxFit? placeholderFit = null,
        FilterQuality filterQuality = FilterQuality.Medium,
        FilterQuality? placeholderFilterQuality = null,
        AlignmentGeometry alignment = default,
        ImageRepeat repeat = ImageRepeat.NoRepeat,
        bool matchTextDirection = false,
        int? placeholderCacheWidth = null,
        int? placeholderCacheHeight = null,
        int? imageCacheWidth = null,
        int? imageCacheHeight = null,
        Key? key = null)
    {
        return new FadeInImage(
            placeholder: ResizeImage.ResizeIfNeeded(
                placeholderCacheWidth,
                placeholderCacheHeight,
                new MemoryImage(placeholder, placeholderScale)),
            image: ResizeImage.ResizeIfNeeded(
                imageCacheWidth,
                imageCacheHeight,
                new NetworkImage(image, imageScale)),
            placeholderErrorBuilder: placeholderErrorBuilder,
            imageErrorBuilder: imageErrorBuilder,
            excludeFromSemantics: excludeFromSemantics,
            imageSemanticLabel: imageSemanticLabel,
            fadeOutDuration: fadeOutDuration,
            fadeOutCurve: fadeOutCurve,
            fadeInDuration: fadeInDuration,
            fadeInCurve: fadeInCurve,
            width: width,
            height: height,
            fit: fit,
            color: color,
            colorBlendMode: colorBlendMode,
            placeholderColor: placeholderColor,
            placeholderColorBlendMode: placeholderColorBlendMode,
            placeholderFit: placeholderFit,
            filterQuality: filterQuality,
            placeholderFilterQuality: placeholderFilterQuality,
            alignment: alignment,
            repeat: repeat,
            matchTextDirection: matchTextDirection,
            key: key);
    }

    public static FadeInImage AssetNetwork(
        string placeholder,
        string image,
        ImageErrorWidgetBuilder? placeholderErrorBuilder = null,
        ImageErrorWidgetBuilder? imageErrorBuilder = null,
        AssetBundle? bundle = null,
        double? placeholderScale = null,
        double imageScale = 1.0,
        bool excludeFromSemantics = false,
        string? imageSemanticLabel = null,
        TimeSpan? fadeOutDuration = null,
        Curve? fadeOutCurve = null,
        TimeSpan? fadeInDuration = null,
        Curve? fadeInCurve = null,
        double? width = null,
        double? height = null,
        BoxFit? fit = null,
        Color? color = null,
        BitmapBlendingMode? colorBlendMode = null,
        Color? placeholderColor = null,
        BitmapBlendingMode? placeholderColorBlendMode = null,
        BoxFit? placeholderFit = null,
        FilterQuality filterQuality = FilterQuality.Medium,
        FilterQuality? placeholderFilterQuality = null,
        AlignmentGeometry alignment = default,
        ImageRepeat repeat = ImageRepeat.NoRepeat,
        bool matchTextDirection = false,
        int? placeholderCacheWidth = null,
        int? placeholderCacheHeight = null,
        int? imageCacheWidth = null,
        int? imageCacheHeight = null,
        Key? key = null)
    {
        ImageProvider placeholderProvider = placeholderScale.HasValue
            ? new ExactAssetImage(placeholder, placeholderScale.Value, bundle)
            : new AssetImage(placeholder, bundle);
        return new FadeInImage(
            placeholder: ResizeImage.ResizeIfNeeded(
                placeholderCacheWidth,
                placeholderCacheHeight,
                placeholderProvider),
            image: ResizeImage.ResizeIfNeeded(
                imageCacheWidth,
                imageCacheHeight,
                new NetworkImage(image, imageScale)),
            placeholderErrorBuilder: placeholderErrorBuilder,
            imageErrorBuilder: imageErrorBuilder,
            excludeFromSemantics: excludeFromSemantics,
            imageSemanticLabel: imageSemanticLabel,
            fadeOutDuration: fadeOutDuration,
            fadeOutCurve: fadeOutCurve,
            fadeInDuration: fadeInDuration,
            fadeInCurve: fadeInCurve,
            width: width,
            height: height,
            fit: fit,
            color: color,
            colorBlendMode: colorBlendMode,
            placeholderColor: placeholderColor,
            placeholderColorBlendMode: placeholderColorBlendMode,
            placeholderFit: placeholderFit,
            filterQuality: filterQuality,
            placeholderFilterQuality: placeholderFilterQuality,
            alignment: alignment,
            repeat: repeat,
            matchTextDirection: matchTextDirection,
            key: key);
    }

    public override State CreateState() => new FadeInImageState();

    private static void ValidateDuration(TimeSpan duration, string parameterName)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void ValidateDimension(double? dimension, string parameterName)
    {
        if (dimension.HasValue && (!double.IsFinite(dimension.Value) || dimension.Value < 0.0))
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private sealed class FadeInImageState : State
    {
        private bool _targetLoaded;
        private readonly ProxyAnimation _imageAnimation = new(OpaqueAnimation);
        private readonly ProxyAnimation _placeholderAnimation = new(OpaqueAnimation);

        private FadeInImage CurrentWidget => (FadeInImage)StateWidget;

        public override Widget Build(BuildContext context)
        {
            Widget result = BuildImage(
                image: CurrentWidget.Image,
                errorBuilder: CurrentWidget.ImageErrorBuilder,
                opacity: _imageAnimation,
                fit: CurrentWidget.Fit,
                color: CurrentWidget.Color,
                colorBlendMode: CurrentWidget.ColorBlendMode,
                filterQuality: CurrentWidget.FilterQuality,
                frameBuilder: (frameContext, child, frame, wasSynchronouslyLoaded) =>
                {
                    if (wasSynchronouslyLoaded || frame.HasValue)
                    {
                        _targetLoaded = true;
                    }

                    return new AnimatedFadeOutFadeIn(
                        target: child,
                        targetProxyAnimation: _imageAnimation,
                        placeholder: BuildImage(
                            image: CurrentWidget.Placeholder,
                            errorBuilder: CurrentWidget.PlaceholderErrorBuilder,
                            opacity: _placeholderAnimation,
                            fit: CurrentWidget.PlaceholderFit ?? CurrentWidget.Fit,
                            color: CurrentWidget.PlaceholderColor,
                            colorBlendMode: CurrentWidget.PlaceholderColorBlendMode,
                            filterQuality: CurrentWidget.PlaceholderFilterQuality ?? CurrentWidget.FilterQuality),
                        placeholderProxyAnimation: _placeholderAnimation,
                        isTargetLoaded: _targetLoaded,
                        wasSynchronouslyLoaded: wasSynchronouslyLoaded,
                        fadeInDuration: CurrentWidget.FadeInDuration,
                        fadeOutDuration: CurrentWidget.FadeOutDuration,
                        fadeInCurve: CurrentWidget.FadeInCurve,
                        fadeOutCurve: CurrentWidget.FadeOutCurve);
                });

            if (!CurrentWidget.ExcludeFromSemantics)
            {
                result = new Semantics(
                    child: result,
                    label: CurrentWidget.ImageSemanticLabel ?? string.Empty,
                    flags: SemanticsFlags.IsImage,
                    container: CurrentWidget.ImageSemanticLabel is not null);
            }

            return result;
        }

        private Plumix.Widgets.Image BuildImage(
            ImageProvider image,
            ImageErrorWidgetBuilder? errorBuilder,
            Animation<double> opacity,
            BoxFit? fit,
            Color? color,
            BitmapBlendingMode? colorBlendMode,
            FilterQuality filterQuality,
            ImageFrameBuilder? frameBuilder = null)
        {
            return new Plumix.Widgets.Image(
                image: image,
                errorBuilder: errorBuilder,
                frameBuilder: frameBuilder,
                opacity: opacity,
                width: CurrentWidget.Width,
                height: CurrentWidget.Height,
                fit: fit,
                color: color,
                colorBlendMode: colorBlendMode,
                filterQuality: filterQuality,
                alignment: CurrentWidget.Alignment,
                repeat: CurrentWidget.Repeat,
                matchTextDirection: CurrentWidget.MatchTextDirection,
                gaplessPlayback: true,
                excludeFromSemantics: true);
        }
    }

    private sealed class AnimatedFadeOutFadeIn : StatefulWidget
    {
        public AnimatedFadeOutFadeIn(
            Widget target,
            ProxyAnimation targetProxyAnimation,
            Widget placeholder,
            ProxyAnimation placeholderProxyAnimation,
            bool isTargetLoaded,
            TimeSpan fadeInDuration,
            TimeSpan fadeOutDuration,
            Curve fadeInCurve,
            Curve fadeOutCurve,
            bool wasSynchronouslyLoaded)
        {
            Target = target;
            TargetProxyAnimation = targetProxyAnimation;
            Placeholder = placeholder;
            PlaceholderProxyAnimation = placeholderProxyAnimation;
            IsTargetLoaded = isTargetLoaded;
            FadeInDuration = fadeInDuration;
            FadeOutDuration = fadeOutDuration;
            FadeInCurve = fadeInCurve;
            FadeOutCurve = fadeOutCurve;
            WasSynchronouslyLoaded = wasSynchronouslyLoaded;
        }

        public Widget Target { get; }

        public ProxyAnimation TargetProxyAnimation { get; }

        public Widget Placeholder { get; }

        public ProxyAnimation PlaceholderProxyAnimation { get; }

        public bool IsTargetLoaded { get; }

        public TimeSpan FadeInDuration { get; }

        public TimeSpan FadeOutDuration { get; }

        public Curve FadeInCurve { get; }

        public Curve FadeOutCurve { get; }

        public bool WasSynchronouslyLoaded { get; }

        public override State CreateState() => new AnimatedFadeOutFadeInState();
    }

    private sealed class AnimatedFadeOutFadeInState : State
    {
        private AnimationController? _controller;
        private MappedDoubleAnimation? _targetOpacityAnimation;
        private MappedDoubleAnimation? _placeholderOpacityAnimation;
        private bool _placeholderRemoved;

        private AnimatedFadeOutFadeIn CurrentWidget => (AnimatedFadeOutFadeIn)StateWidget;

        public override void InitState()
        {
            if (CurrentWidget.WasSynchronouslyLoaded)
            {
                CurrentWidget.TargetProxyAnimation.Parent = OpaqueAnimation;
                CurrentWidget.PlaceholderProxyAnimation.Parent = new ConstantDoubleAnimation(0.0);
                _placeholderRemoved = true;
                return;
            }

            CurrentWidget.TargetProxyAnimation.Parent = new ConstantDoubleAnimation(
                CurrentWidget.IsTargetLoaded ? 1.0 : 0.0);
            CurrentWidget.PlaceholderProxyAnimation.Parent = new ConstantDoubleAnimation(
                CurrentWidget.IsTargetLoaded ? 0.0 : 1.0);
            _placeholderRemoved = CurrentWidget.IsTargetLoaded;
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldFade = (AnimatedFadeOutFadeIn)oldWidget;
            if (CurrentWidget.WasSynchronouslyLoaded)
            {
                CompleteSynchronously();
                return;
            }

            if (!oldFade.IsTargetLoaded && CurrentWidget.IsTargetLoaded)
            {
                StartTransition();
            }
        }

        public override Widget Build(BuildContext context)
        {
            if (CurrentWidget.WasSynchronouslyLoaded || _placeholderRemoved)
            {
                return CurrentWidget.Target;
            }

            return new Stack(
                fit: StackFit.Passthrough,
                alignment: Plumix.Rendering.Alignment.Center,
                children: [CurrentWidget.Target, CurrentWidget.Placeholder]);
        }

        public override void Dispose()
        {
            DisposeAnimations();
        }

        private void StartTransition()
        {
            double targetBegin = CurrentWidget.TargetProxyAnimation.Value;
            double placeholderBegin = CurrentWidget.PlaceholderProxyAnimation.Value;
            DisposeAnimations();
            _placeholderRemoved = false;
            TimeSpan duration = CurrentWidget.FadeOutDuration + CurrentWidget.FadeInDuration;
            if (duration == TimeSpan.Zero)
            {
                CompleteSynchronously();
                return;
            }

            _controller = new AnimationController(duration: duration, vsync: this);
            _targetOpacityAnimation = new MappedDoubleAnimation(
                _controller,
                value => EvaluateTarget(value, targetBegin));
            _placeholderOpacityAnimation = new MappedDoubleAnimation(
                _controller,
                value => EvaluatePlaceholder(value, placeholderBegin));
            CurrentWidget.TargetProxyAnimation.Parent = _targetOpacityAnimation;
            CurrentWidget.PlaceholderProxyAnimation.Parent = _placeholderOpacityAnimation;
            _controller.Completed += HandleCompleted;
            _controller.Forward(from: 0.0);
        }

        private double EvaluateTarget(double value, double begin)
        {
            double fadeOutMilliseconds = CurrentWidget.FadeOutDuration.TotalMilliseconds;
            double fadeInMilliseconds = CurrentWidget.FadeInDuration.TotalMilliseconds;
            double elapsed = value * (fadeOutMilliseconds + fadeInMilliseconds);
            if (fadeInMilliseconds <= 0.0)
            {
                return 1.0;
            }
            if (elapsed <= fadeOutMilliseconds)
            {
                return begin;
            }

            double progress = (elapsed - fadeOutMilliseconds) / fadeInMilliseconds;
            return Lerp(begin, 1.0, CurrentWidget.FadeInCurve(Math.Clamp(progress, 0.0, 1.0)));
        }

        private double EvaluatePlaceholder(double value, double begin)
        {
            double fadeOutMilliseconds = CurrentWidget.FadeOutDuration.TotalMilliseconds;
            double fadeInMilliseconds = CurrentWidget.FadeInDuration.TotalMilliseconds;
            if (fadeOutMilliseconds <= 0.0)
            {
                return 0.0;
            }

            double elapsed = value * (fadeOutMilliseconds + fadeInMilliseconds);
            double progress = elapsed / fadeOutMilliseconds;
            return Lerp(begin, 0.0, CurrentWidget.FadeOutCurve(Math.Clamp(progress, 0.0, 1.0)));
        }

        private void CompleteSynchronously()
        {
            DisposeAnimations();
            CurrentWidget.TargetProxyAnimation.Parent = OpaqueAnimation;
            CurrentWidget.PlaceholderProxyAnimation.Parent = new ConstantDoubleAnimation(0.0);
            _placeholderRemoved = true;
        }

        private void HandleCompleted()
        {
            SetState(() => _placeholderRemoved = true);
        }

        private void DisposeAnimations()
        {
            if (_controller is not null)
            {
                _controller.Completed -= HandleCompleted;
            }
            _targetOpacityAnimation?.Dispose();
            _placeholderOpacityAnimation?.Dispose();
            _controller?.Dispose();
            _targetOpacityAnimation = null;
            _placeholderOpacityAnimation = null;
            _controller = null;
        }

        private static double Lerp(double begin, double end, double t)
        {
            return begin + ((end - begin) * t);
        }
    }

    private sealed class ConstantDoubleAnimation : Animation<double>
    {
        public ConstantDoubleAnimation(double value)
        {
            Value = value;
        }

        public override double Value { get; }

        public override AnimationStatus Status => Value == 1.0
            ? AnimationStatus.Completed
            : AnimationStatus.Dismissed;

        public override void AddListener(Action listener)
        {
        }

        public override void RemoveListener(Action listener)
        {
        }

        public override void AddStatusListener(Action<AnimationStatus> listener)
        {
        }

        public override void RemoveStatusListener(Action<AnimationStatus> listener)
        {
        }
    }
}
