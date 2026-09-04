using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/annotated_region.dart

namespace Plumix.Widgets;

public sealed class AnnotatedRegion<T> : SingleChildRenderObjectWidget where T : notnull
{
    public AnnotatedRegion(
        T value,
        Widget child,
        bool sized = true,
        Key? key = null) : base(
            child ?? throw new ArgumentNullException(nameof(child)),
            key)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
        Sized = sized;
    }

    public T Value { get; }

    public bool Sized { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderAnnotatedRegion<T>(
            value: Value,
            sized: Sized);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var annotatedRegion = (RenderAnnotatedRegion<T>)renderObject;
        annotatedRegion.Value = Value;
        annotatedRegion.Sized = Sized;
    }
}
