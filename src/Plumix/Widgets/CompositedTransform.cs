using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart
// (CompositedTransformTarget, CompositedTransformFollower)

public sealed class CompositedTransformTarget : SingleChildRenderObjectWidget
{
    public CompositedTransformTarget(LayerLink link, Widget? child = null, Key? key = null) : base(child, key)
    {
        Link = link ?? throw new ArgumentNullException(nameof(link));
    }

    public LayerLink Link { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderLeaderLayer(Link);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderLeaderLayer)renderObject).Link = Link;
    }
}

public sealed class CompositedTransformFollower : SingleChildRenderObjectWidget
{
    public CompositedTransformFollower(
        LayerLink link,
        Widget? child = null,
        bool showWhenUnlinked = true,
        Vector offset = default,
        Alignment? targetAnchor = null,
        Alignment? followerAnchor = null,
        Key? key = null) : base(child, key)
    {
        Link = link ?? throw new ArgumentNullException(nameof(link));
        ShowWhenUnlinked = showWhenUnlinked;
        Offset = offset;
        TargetAnchor = targetAnchor ?? Alignment.TopLeft;
        FollowerAnchor = followerAnchor ?? Alignment.TopLeft;
    }

    public LayerLink Link { get; }

    public bool ShowWhenUnlinked { get; }

    public Vector Offset { get; }

    public Alignment TargetAnchor { get; }

    public Alignment FollowerAnchor { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderFollowerLayer(
            Link,
            ShowWhenUnlinked,
            Offset,
            TargetAnchor,
            FollowerAnchor);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var follower = (RenderFollowerLayer)renderObject;
        follower.Link = Link;
        follower.ShowWhenUnlinked = ShowWhenUnlinked;
        follower.Offset = Offset;
        follower.LeaderAnchor = TargetAnchor;
        follower.FollowerAnchor = FollowerAnchor;
    }
}
