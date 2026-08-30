using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;

namespace Plumix.Rendering;

// Dart parity source:
// flutter/packages/flutter/lib/src/widgets/color_filter.dart
// flutter/packages/flutter/lib/src/widgets/image_filter.dart
// flutter/packages/flutter/lib/src/rendering/proxy_box.dart (RenderShaderMask, RenderBackdropFilter)

public delegate IBrush ShaderCallback(Rect bounds);

public sealed class RenderColorFilter : RenderProxyBox
{
    private ColorFilter _colorFilter;

    public RenderColorFilter(ColorFilter colorFilter, RenderBox? child = null)
    {
        _colorFilter = colorFilter ?? throw new ArgumentNullException(nameof(colorFilter));
        Child = child;
    }

    public ColorFilter ColorFilter
    {
        get => _colorFilter;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (_colorFilter == value)
            {
                return;
            }

            _colorFilter = value;
            MarkNeedsPaint();
        }
    }

    protected override bool AlwaysNeedsCompositing => Child != null;

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (Child is null)
        {
            return;
        }

        ColorFilterLayer layer = ctx.PushColorFilter(
            offset,
            ColorFilter,
            base.Paint,
            _layer as ColorFilterLayer);
        layer.FilterBounds = new Rect(offset, Size);
        _layer = layer;
    }
}

public sealed class RenderImageFilter : RenderProxyBox
{
    private ImageFilter _imageFilter;
    private bool _enabled;

    public RenderImageFilter(
        ImageFilter imageFilter,
        bool enabled = true,
        RenderBox? child = null)
    {
        _imageFilter = imageFilter ?? throw new ArgumentNullException(nameof(imageFilter));
        _enabled = enabled;
        Child = child;
    }

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value)
            {
                return;
            }

            bool wasRepaintBoundary = IsRepaintBoundary;
            _enabled = value;
            if (IsRepaintBoundary != wasRepaintBoundary)
            {
                MarkNeedsCompositingBitsUpdate();
            }

            MarkNeedsPaint();
        }
    }

    public ImageFilter ImageFilter
    {
        get => _imageFilter;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (_imageFilter == value)
            {
                return;
            }

            _imageFilter = value;
            MarkNeedsCompositedLayerUpdate();
        }
    }

    protected override bool AlwaysNeedsCompositing => Child != null && Enabled;

    public override bool IsRepaintBoundary => AlwaysNeedsCompositing;

    protected override OffsetLayer CreateCompositedLayer(OffsetLayer? oldLayer)
    {
        return oldLayer as ImageFilterLayer ?? new ImageFilterLayer();
    }

    protected override void UpdateCompositedLayer(OffsetLayer layer)
    {
        var imageFilterLayer = (ImageFilterLayer)layer;
        imageFilterLayer.ImageFilter = ImageFilter;
        imageFilterLayer.FilterBounds = new Rect(default, Size);
    }
}

public sealed class RenderBackdropFilter : RenderProxyBox
{
    private ImageFilterConfig _filterConfig;
    private bool _enabled;
    private BlendMode _blendMode;
    private BackdropKey? _backdropKey;

    public RenderBackdropFilter(
        ImageFilterConfig filterConfig,
        RenderBox? child = null,
        BlendMode blendMode = BlendMode.SourceOver,
        bool enabled = true,
        BackdropKey? backdropKey = null)
    {
        _filterConfig = filterConfig ?? throw new ArgumentNullException(nameof(filterConfig));
        _blendMode = blendMode;
        _enabled = enabled;
        _backdropKey = backdropKey;
        Child = child;
    }

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value)
            {
                return;
            }

            _enabled = value;
            MarkNeedsPaint();
        }
    }

    public ImageFilter Filter
    {
        get => FilterConfig.Filter
            ?? throw new InvalidOperationException(
                "Filter is only available when FilterConfig directly wraps an ImageFilter.");
        set => FilterConfig = new ImageFilterConfig(
            value ?? throw new ArgumentNullException(nameof(value)));
    }

    public ImageFilterConfig FilterConfig
    {
        get => _filterConfig;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (_filterConfig == value)
            {
                return;
            }

            _filterConfig = value;
            MarkNeedsPaint();
        }
    }

    public BlendMode BlendMode
    {
        get => _blendMode;
        set
        {
            if (_blendMode == value)
            {
                return;
            }

            _blendMode = value;
            MarkNeedsPaint();
        }
    }

    public BackdropKey? BackdropKey
    {
        get => _backdropKey;
        set
        {
            if (ReferenceEquals(_backdropKey, value))
            {
                return;
            }

            _backdropKey = value;
            MarkNeedsPaint();
        }
    }

    protected override bool AlwaysNeedsCompositing => Child != null;

    public override void Paint(PaintingContext context, Point offset)
    {
        if (!Enabled)
        {
            base.Paint(context, offset);
            return;
        }

        if (Child is null)
        {
            _layer = null;
            return;
        }

        ImageFilter effectiveFilter = FilterConfig.Resolve(
            new ImageFilterContext(new Rect(offset, Size)));
        var layer = _layer as BackdropFilterLayer ?? new BackdropFilterLayer();
        layer.ImageFilter = effectiveFilter;
        layer.BlendMode = BlendMode;
        layer.BackdropKey = BackdropKey;
        context.PushLayer(layer, base.Paint, offset);
        _layer = layer;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<ImageFilterConfig>(
            "filterConfig",
            FilterConfig,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new EnumProperty<BlendMode>("blendMode", BlendMode));
        properties.Add(new FlagProperty("enabled", Enabled, ifTrue: "enabled"));
    }
}

public sealed class RenderShaderMask : RenderProxyBox
{
    private ShaderCallback _shaderCallback;
    private BlendMode _blendMode;

    public RenderShaderMask(
        ShaderCallback shaderCallback,
        RenderBox? child = null,
        BlendMode blendMode = BlendMode.Modulate)
    {
        _shaderCallback = shaderCallback ?? throw new ArgumentNullException(nameof(shaderCallback));
        _blendMode = blendMode;
        Child = child;
    }

    public ShaderCallback ShaderCallback
    {
        get => _shaderCallback;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (_shaderCallback == value)
            {
                return;
            }

            _shaderCallback = value;
            MarkNeedsPaint();
        }
    }

    public BlendMode BlendMode
    {
        get => _blendMode;
        set
        {
            if (_blendMode == value)
            {
                return;
            }

            _blendMode = value;
            MarkNeedsPaint();
        }
    }

    protected override bool AlwaysNeedsCompositing => Child != null;

    public override void Paint(PaintingContext context, Point offset)
    {
        if (Child is null)
        {
            _layer = null;
            return;
        }

        var bounds = new Rect(default, Size);
        var layer = _layer as ShaderMaskLayer ?? new ShaderMaskLayer();
        layer.Shader = ShaderCallback(bounds)
            ?? throw new InvalidOperationException("ShaderCallback must return a non-null brush.");
        layer.MaskRect = new Rect(offset, Size);
        layer.BlendMode = BlendMode;
        context.PushLayer(layer, base.Paint, offset);
        _layer = layer;
    }
}
