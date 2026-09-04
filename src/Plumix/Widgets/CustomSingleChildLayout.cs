using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart

/// <summary>Uses a delegate to size and position one child.</summary>
public sealed class CustomSingleChildLayout : SingleChildRenderObjectWidget
{
    public CustomSingleChildLayout(
        SingleChildLayoutDelegate layoutDelegate,
        Widget? child = null,
        Key? key = null) : base(child, key)
    {
        LayoutDelegate = layoutDelegate ?? throw new ArgumentNullException(nameof(layoutDelegate));
    }

    public SingleChildLayoutDelegate LayoutDelegate { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderCustomSingleChildLayoutBox(LayoutDelegate);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderCustomSingleChildLayoutBox)renderObject).LayoutDelegate = LayoutDelegate;
    }
}
