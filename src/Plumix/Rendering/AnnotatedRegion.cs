using Avalonia;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/proxy_box.dart (RenderAnnotatedRegion).

namespace Plumix.Rendering;

public sealed class RenderAnnotatedRegion<T> : RenderProxyBox where T : notnull
{
    private T _value;
    private bool _sized;
    private AnnotatedRegionLayer<T>? _annotationLayer;

    public RenderAnnotatedRegion(
        T value,
        bool sized = true,
        RenderBox? child = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        _value = value;
        _sized = sized;
        Child = child;
    }

    public T Value
    {
        get => _value;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (Equals(_value, value))
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

    protected override bool AlwaysNeedsCompositing => true;

    public override void Paint(PaintingContext context, Point offset)
    {
        _annotationLayer = new AnnotatedRegionLayer<T>(
            value: _value,
            size: _sized ? Size : null,
            offset: _sized ? offset : null);
        context.PushLayer(
            _annotationLayer,
            childContext => base.Paint(childContext, offset));
    }

    protected override void OnDetach()
    {
        _annotationLayer?.Parent?.Remove(_annotationLayer);
        _annotationLayer = null;
        base.OnDetach();
    }
}
