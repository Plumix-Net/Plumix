using Avalonia;
using Avalonia.Media;

namespace Plumix.Rendering;

// Dart parity source:
// flutter/packages/flutter/lib/src/widgets/color_filter.dart
// flutter/packages/flutter/lib/src/widgets/image_filter.dart
// flutter/packages/flutter/lib/src/rendering/proxy_box.dart (RenderShaderMask)

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
            childContext => base.Paint(childContext, offset),
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
        context.PushLayer(
            layer,
            childContext => base.Paint(childContext, offset));
        _layer = layer;
    }
}
