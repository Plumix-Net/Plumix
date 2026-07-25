using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart (MetaData)

namespace Plumix.Widgets;

public sealed class MetaData : SingleChildRenderObjectWidget
{
    public MetaData(
        object? metaData = null,
        HitTestBehavior behavior = HitTestBehavior.DeferToChild,
        Widget? child = null,
        Key? key = null) : base(child, key)
    {
        Metadata = metaData;
        Behavior = behavior;
    }

    // C# does not allow a member named MetaData inside the MetaData type.
    public object? Metadata { get; }

    public HitTestBehavior Behavior { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderMetaData(
            metaData: Metadata,
            behavior: Behavior);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var metadata = (RenderMetaData)renderObject;
        metadata.MetaData = Metadata;
        metadata.Behavior = Behavior;
    }
}
