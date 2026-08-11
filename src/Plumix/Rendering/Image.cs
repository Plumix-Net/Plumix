using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.Foundation;
using Plumix.UI;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/image.dart

public sealed class RenderImage : RenderBox
{
    private IImage? _image;
    private double? _width;
    private double? _height;
    private double _scale;
    private Color? _color;
    private IValueListenable<double>? _opacity;
    private BitmapBlendingMode? _colorBlendMode;
    private BoxFit? _fit;
    private AlignmentGeometry _alignment;
    private ImageRepeat _repeat;
    private Rect? _centerSlice;
    private bool _matchTextDirection;
    private TextDirection? _textDirection;
    private bool _invertColors;
    private bool _isAntiAlias;
    private FilterQuality _filterQuality;
    private Alignment? _resolvedAlignment;
    private bool? _flipHorizontally;

    public RenderImage(
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
        TextDirection? textDirection = null,
        bool invertColors = false,
        bool isAntiAlias = false,
        FilterQuality filterQuality = FilterQuality.Medium)
    {
        ValidateDimension(width, nameof(width));
        ValidateDimension(height, nameof(height));
        _image = image;
        DebugImageLabel = debugImageLabel;
        _width = width;
        _height = height;
        _scale = MemoryImage.ValidateScale(scale);
        _color = color;
        _opacity = opacity;
        _colorBlendMode = colorBlendMode;
        _fit = fit;
        _alignment = alignment;
        _repeat = repeat;
        _centerSlice = centerSlice;
        _matchTextDirection = matchTextDirection;
        _textDirection = textDirection;
        _invertColors = invertColors;
        _isAntiAlias = isAntiAlias;
        _filterQuality = filterQuality;
    }

    public IImage? Image
    {
        get => _image;
        set
        {
            if (ReferenceEquals(_image, value))
            {
                return;
            }

            bool sizeChanged = _image?.Size != value?.Size;
            _image = value;
            MarkNeedsPaint();
            if (sizeChanged && (!_width.HasValue || !_height.HasValue))
            {
                MarkNeedsLayout();
            }
        }
    }

    public string? DebugImageLabel { get; set; }

    public double? Width
    {
        get => _width;
        set
        {
            ValidateDimension(value, nameof(value));
            if (_width == value)
            {
                return;
            }

            _width = value;
            MarkNeedsLayout();
        }
    }

    public double? Height
    {
        get => _height;
        set
        {
            ValidateDimension(value, nameof(value));
            if (_height == value)
            {
                return;
            }

            _height = value;
            MarkNeedsLayout();
        }
    }

    public double Scale
    {
        get => _scale;
        set
        {
            double validated = MemoryImage.ValidateScale(value);
            if (_scale.Equals(validated))
            {
                return;
            }

            _scale = validated;
            MarkNeedsLayout();
        }
    }

    public Color? Color
    {
        get => _color;
        set
        {
            if (_color == value)
            {
                return;
            }

            _color = value;
            MarkNeedsPaint();
        }
    }

    public IValueListenable<double>? Opacity
    {
        get => _opacity;
        set
        {
            if (ReferenceEquals(_opacity, value))
            {
                return;
            }

            if (Attached)
            {
                _opacity?.RemoveListener(HandleOpacityChanged);
            }

            _opacity = value;
            if (Attached)
            {
                _opacity?.AddListener(HandleOpacityChanged);
            }

            MarkNeedsPaint();
        }
    }

    public BitmapBlendingMode? ColorBlendMode
    {
        get => _colorBlendMode;
        set
        {
            if (_colorBlendMode == value)
            {
                return;
            }

            _colorBlendMode = value;
            MarkNeedsPaint();
        }
    }

    public BoxFit? Fit
    {
        get => _fit;
        set
        {
            if (_fit == value)
            {
                return;
            }

            _fit = value;
            MarkNeedsPaint();
        }
    }

    public AlignmentGeometry Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment == value)
            {
                return;
            }

            _alignment = value;
            MarkNeedsResolution();
        }
    }

    public ImageRepeat Repeat
    {
        get => _repeat;
        set
        {
            if (_repeat == value)
            {
                return;
            }

            _repeat = value;
            MarkNeedsPaint();
        }
    }

    public Rect? CenterSlice
    {
        get => _centerSlice;
        set
        {
            if (_centerSlice == value)
            {
                return;
            }

            _centerSlice = value;
            MarkNeedsPaint();
        }
    }

    public bool MatchTextDirection
    {
        get => _matchTextDirection;
        set
        {
            if (_matchTextDirection == value)
            {
                return;
            }

            _matchTextDirection = value;
            MarkNeedsResolution();
        }
    }

    public TextDirection? TextDirection
    {
        get => _textDirection;
        set
        {
            if (_textDirection == value)
            {
                return;
            }

            _textDirection = value;
            MarkNeedsResolution();
        }
    }

    public bool InvertColors
    {
        get => _invertColors;
        set
        {
            if (_invertColors == value)
            {
                return;
            }

            _invertColors = value;
            MarkNeedsPaint();
        }
    }

    public bool IsAntiAlias
    {
        get => _isAntiAlias;
        set
        {
            if (_isAntiAlias == value)
            {
                return;
            }

            _isAntiAlias = value;
            MarkNeedsPaint();
        }
    }

    public FilterQuality FilterQuality
    {
        get => _filterQuality;
        set
        {
            if (_filterQuality == value)
            {
                return;
            }

            _filterQuality = value;
            MarkNeedsPaint();
        }
    }

    protected override void PerformLayout()
    {
        Size = SizeForConstraints(Constraints);
    }

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        return !_width.HasValue && !_height.HasValue
            ? 0.0
            : SizeForConstraints(BoxConstraints.TightForFinite(height: height)).Width;
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        return SizeForConstraints(BoxConstraints.TightForFinite(height: height)).Width;
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        return !_width.HasValue && !_height.HasValue
            ? 0.0
            : SizeForConstraints(BoxConstraints.TightForFinite(width: width)).Height;
    }

    protected override double ComputeMaxIntrinsicHeight(double width)
    {
        return SizeForConstraints(BoxConstraints.TightForFinite(width: width)).Height;
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints) => SizeForConstraints(constraints);

    protected override bool HitTestSelf(Point position) => true;

    protected override void OnAttach()
    {
        base.OnAttach();
        _opacity?.AddListener(HandleOpacityChanged);
    }

    protected override void OnDetach()
    {
        _opacity?.RemoveListener(HandleOpacityChanged);
        base.OnDetach();
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (_image is null)
        {
            return;
        }

        Resolve();
        ColorFilter? colorFilter = _color.HasValue
            ? new ColorFilter.Mode(_color.Value, _colorBlendMode ?? BitmapBlendingMode.SourceIn)
            : null;
        ImagePainting.PaintImage(
            context: context,
            rect: new Rect(offset, Size),
            image: _image,
            scale: _scale,
            opacity: _opacity?.Value ?? 1.0,
            colorFilter: colorFilter,
            fit: _fit,
            alignment: _resolvedAlignment!.Value,
            centerSlice: _centerSlice,
            repeat: _repeat,
            flipHorizontally: _flipHorizontally!.Value,
            invertColors: _invertColors,
            filterQuality: _filterQuality,
            isAntiAlias: _isAntiAlias);
    }

    internal Size SizeForConstraints(BoxConstraints constraints)
    {
        BoxConstraints effective = BoxConstraints.TightFor(_width, _height).Enforce(constraints);
        if (_image is null)
        {
            return effective.Smallest;
        }

        return effective.ConstrainSizeAndAttemptToPreserveAspectRatio(
            new Size(_image.Size.Width / _scale, _image.Size.Height / _scale));
    }

    private static void ValidateDimension(double? value, string parameterName)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0.0))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Image dimensions must be finite and non-negative.");
        }
    }

    private void Resolve()
    {
        if (_resolvedAlignment.HasValue)
        {
            return;
        }

        if ((_alignment.IsDirectional || _matchTextDirection) && !_textDirection.HasValue)
        {
            throw new InvalidOperationException(
                "A text direction is required for directional image alignment or matchTextDirection.");
        }

        TextDirection direction = _textDirection ?? Plumix.UI.TextDirection.Ltr;
        _resolvedAlignment = _alignment.Resolve(direction);
        _flipHorizontally = _matchTextDirection && direction == Plumix.UI.TextDirection.Rtl;
    }

    private void MarkNeedsResolution()
    {
        _resolvedAlignment = null;
        _flipHorizontally = null;
        MarkNeedsPaint();
    }

    private void HandleOpacityChanged() => MarkNeedsPaint();
}
