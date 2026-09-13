using Avalonia;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/proxy_box.dart (RenderAnnotatedRegion).

namespace Plumix.Rendering;

public class RenderAnnotatedRegion<T> : RenderProxyBox where T : notnull
{
    private T _value;
    private bool _sized;
    private readonly LayerHandle<AnnotatedRegionLayer<T>> _layerHandle;

    public RenderAnnotatedRegion(T value, bool sized, RenderBox? child = null) : base(child)
    {
        _value = value;
        _sized = sized;
        _layerHandle = new LayerHandle<AnnotatedRegionLayer<T>>();
    }

    public T Value
    {
        get => _value;
        set
        {
            if (EqualityComparer<T>.Default.Equals(_value, value))
            {
                return;
            }

            _value = value;
            MarkNeedsPaint();
        }
    }

    public bool Sized
    {
        get => _sized;
        set
        {
            if (_sized == value)
            {
                return;
            }

            _sized = value;
            MarkNeedsPaint();
        }
    }

    public override bool AlwaysNeedsCompositing => true;

    public override void Paint(PaintingContext context, Point offset)
    {
        // Annotated region layers are not retained because they do not create engine layers.
        var layer = new AnnotatedRegionLayer<T>(
            Value,
            size: Sized ? Size : null,
            offset: Sized ? offset : null);
        _layerHandle.Layer = layer;
        context.PushLayer(layer, base.Paint, offset);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _layerHandle.Layer = null;
        base.Dispose();
    }
}
