using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/ink_decoration.dart

/// <summary>
/// Draws a decoration beneath its child so a descendant <see cref="InkWell"/>
/// or <see cref="InkResponse"/> remains visible above an opaque surface.
/// </summary>
public sealed class Ink : StatelessWidget
{
    public Ink(
        Thickness? padding = null,
        Color? color = null,
        Decoration? decoration = null,
        double? width = null,
        double? height = null,
        Widget? child = null,
        Key? key = null) : base(key)
    {
        if (color.HasValue && decoration is not null)
        {
            throw new ArgumentException("Cannot provide both color and decoration.", nameof(decoration));
        }

        ValidateDimension(width, nameof(width));
        ValidateDimension(height, nameof(height));
        ValidatePadding(padding);

        Padding = padding;
        Decoration = decoration ?? (color.HasValue ? new BoxDecoration(Color: color) : null);
        Width = width;
        Height = height;
        Child = child;
    }

    public Thickness? Padding { get; }

    public Decoration? Decoration { get; }

    public double? Width { get; }

    public double? Height { get; }

    public Widget? Child { get; }

    /// <summary>Creates an ink surface backed by a <see cref="DecorationImage"/>.</summary>
    public static Ink Image(
        ImageProvider image,
        Thickness? padding = null,
        ImageErrorListener? onImageError = null,
        ColorFilter? colorFilter = null,
        BoxFit? fit = null,
        Alignment alignment = default,
        Rect? centerSlice = null,
        ImageRepeat repeat = ImageRepeat.NoRepeat,
        bool matchTextDirection = false,
        double? width = null,
        double? height = null,
        Widget? child = null,
        Key? key = null)
    {
        ArgumentNullException.ThrowIfNull(image);
        return new Ink(
            padding: padding,
            decoration: new BoxDecoration(
                Image: new DecorationImage(
                    image: image,
                    onError: onImageError,
                    colorFilter: colorFilter,
                    fit: fit,
                    alignment: alignment,
                    centerSlice: centerSlice,
                    repeat: repeat,
                    matchTextDirection: matchTextDirection)),
            width: width,
            height: height,
            child: child,
            key: key);
    }

    public override Widget Build(BuildContext context)
    {
        Widget content = Child ?? new ConstrainedBox(BoxConstraints.Expand());
        if (Padding.HasValue)
        {
            content = new Padding(Padding.Value, content);
        }

        if (Decoration is not null)
        {
            MaterialInkController? controller = Material.MaybeOf(context);
            content = controller is null
                ? new DecoratedBox(Decoration, child: content)
                : new InkDecorationWidget(
                    decoration: Decoration,
                    isVisible: Visibility.Of(context),
                    configuration: ImageConfigurationUtils.CreateLocalImageConfiguration(context),
                    controller: controller,
                    child: content);
        }

        if (Width.HasValue || Height.HasValue)
        {
            content = new ConstrainedBox(BoxConstraints.TightFor(Width, Height), content);
        }

        return content;
    }

    private static void ValidateDimension(double? value, string parameterName)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Ink dimensions must be finite and non-negative.");
        }
    }

    private static void ValidatePadding(Thickness? padding)
    {
        if (padding.HasValue
            && (padding.Value.Left < 0
                || padding.Value.Top < 0
                || padding.Value.Right < 0
                || padding.Value.Bottom < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(padding), "Ink padding must be non-negative.");
        }
    }
}

internal sealed class InkDecorationWidget : SingleChildRenderObjectWidget
{
    public InkDecorationWidget(
        Decoration decoration,
        bool isVisible,
        ImageConfiguration configuration,
        MaterialInkController controller,
        Widget child) : base(child)
    {
        Decoration = decoration;
        IsVisible = isVisible;
        Configuration = configuration;
        Controller = controller;
    }

    public Decoration Decoration { get; }

    public bool IsVisible { get; }

    public ImageConfiguration Configuration { get; }

    public MaterialInkController Controller { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderInkDecoration(Decoration, IsVisible, Configuration, Controller);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var inkDecoration = (RenderInkDecoration)renderObject;
        inkDecoration.Decoration = Decoration;
        inkDecoration.IsVisible = IsVisible;
        inkDecoration.Configuration = Configuration;
        inkDecoration.Controller = Controller;
    }
}

internal sealed class RenderInkDecoration : RenderProxyBox, IMaterialInkFeature
{
    private Decoration _decoration;
    private bool _isVisible;
    private ImageConfiguration _configuration;
    private MaterialInkController _controller;
    private BoxPainter? _painter;

    public RenderInkDecoration(
        Decoration decoration,
        bool isVisible,
        ImageConfiguration configuration,
        MaterialInkController controller)
    {
        _decoration = decoration;
        _isVisible = isVisible;
        _configuration = configuration;
        _controller = controller;
        _controller.AddInkFeature(this);
    }

    public Decoration Decoration
    {
        get => _decoration;
        set
        {
            if (_decoration == value)
            {
                return;
            }

            DisposePainter();
            _decoration = value;
            _controller.MarkNeedsPaint();
        }
    }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value)
            {
                return;
            }

            _isVisible = value;
            _controller.MarkNeedsPaint();
        }
    }

    public ImageConfiguration Configuration
    {
        get => _configuration;
        set
        {
            if (_configuration == value)
            {
                return;
            }

            _configuration = value;
            _controller.MarkNeedsPaint();
        }
    }

    public MaterialInkController Controller
    {
        get => _controller;
        set
        {
            if (ReferenceEquals(_controller, value))
            {
                return;
            }

            _controller.RemoveInkFeature(this);
            _controller = value;
            _controller.AddInkFeature(this);
        }
    }

    RenderBox IMaterialInkFeature.ReferenceBox => this;

    public override void Paint(PaintingContext context, Avalonia.Point offset)
    {
        base.Paint(context, offset);
    }

    void IMaterialInkFeature.PaintFeature(PaintingContext context)
    {
        if (!_isVisible)
        {
            return;
        }

        _painter ??= _decoration.CreateBoxPainter(_controller.MarkNeedsPaint);
        _painter.Paint(context, default, _configuration.CopyWith(size: Size));
    }

    protected override void OnAttach()
    {
        base.OnAttach();
        _controller.AddInkFeature(this);
    }

    protected override void OnDetach()
    {
        _controller.RemoveInkFeature(this);
        DisposePainter();
        base.OnDetach();
    }

    private void DisposePainter()
    {
        _painter?.Dispose();
        _painter = null;
    }
}
