using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/image_filter.dart

public sealed class ImageFiltered : SingleChildRenderObjectWidget
{
    public ImageFiltered(
        ImageFilter imageFilter,
        Widget? child = null,
        bool enabled = true,
        Key? key = null) : base(child, key)
    {
        ImageFilter = imageFilter ?? throw new ArgumentNullException(nameof(imageFilter));
        Enabled = enabled;
    }

    public ImageFilter ImageFilter { get; }

    public bool Enabled { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderImageFilter(ImageFilter, Enabled);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var imageFilter = (RenderImageFilter)renderObject;
        imageFilter.Enabled = Enabled;
        imageFilter.ImageFilter = ImageFilter;
    }
}
