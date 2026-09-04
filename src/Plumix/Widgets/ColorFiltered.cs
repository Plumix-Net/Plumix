using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/color_filter.dart

public sealed class ColorFiltered : SingleChildRenderObjectWidget
{
    public ColorFiltered(
        ColorFilter colorFilter,
        Widget? child = null,
        Key? key = null) : base(child, key)
    {
        ColorFilter = colorFilter ?? throw new ArgumentNullException(nameof(colorFilter));
    }

    public ColorFilter ColorFilter { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderColorFilter(ColorFilter);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderColorFilter)renderObject).ColorFilter = ColorFilter;
    }
}
