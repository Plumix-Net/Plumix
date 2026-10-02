using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/decorated_sliver.dart

namespace Plumix.Widgets;

public sealed class DecoratedSliver : SingleChildRenderObjectWidget
{
    public DecoratedSliver(
        Decoration decoration,
        DecorationPosition position = DecorationPosition.Background,
        Widget? sliver = null,
        Key? key = null) : base(sliver, key)
    {
        Decoration = decoration ?? throw new ArgumentNullException(nameof(decoration));
        Position = position;
    }

    public Decoration Decoration { get; }

    public DecorationPosition Position { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderDecoratedSliver(
            decoration: Decoration,
            position: Position,
            configuration: ImageConfigurationUtils.CreateLocalImageConfiguration(context));
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var decoratedSliver = (RenderDecoratedSliver)renderObject;
        decoratedSliver.Decoration = Decoration;
        decoratedSliver.Position = Position;
        decoratedSliver.Configuration = ImageConfigurationUtils.CreateLocalImageConfiguration(context);
    }
}
