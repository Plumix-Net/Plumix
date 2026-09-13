using Avalonia;
using Plumix.Gestures;
using Plumix.UI;
using Plumix.Foundation;

namespace Plumix.Rendering;

// Dart parity sources:
// - flutter/packages/flutter/lib/src/rendering/proxy_box.dart (RenderLeaderLayer, RenderFollowerLayer)
// - flutter/packages/flutter/lib/src/rendering/layer.dart (LayerLink, LeaderLayer, FollowerLayer)

/// <remarks>
/// Flutter's <c>RenderLeaderLayer</c>. The attach/detach registration with
/// <see cref="LayerLink"/> is Plumix-only: the follower resolves the leader's transform at paint time
/// from the leader render object instead of at composition time (see docs/ai/DIVERGENCES.md).
/// </remarks>
public class RenderLeaderLayer : RenderProxyBox
{
    private LayerLink _link;

    // The latest size of this render box, computed during the previous layout pass. It should always
    // be equal to Size, but can be read even when no layout or resize is in progress.
    private Size? _previousLayoutSize;

    public RenderLeaderLayer(LayerLink link, RenderBox? child = null) : base(child)
    {
        _link = link;
    }

    public LayerLink Link
    {
        get => _link;
        set
        {
            if (_link == value)
            {
                return;
            }

            _link.LeaderSize = null;
            if (Attached)
            {
                _link.UnregisterRenderLeader(this);
                value.RegisterRenderLeader(this);
            }

            _link = value;
            if (_previousLayoutSize != null)
            {
                _link.LeaderSize = _previousLayoutSize;
            }

            MarkNeedsPaint();
        }
    }

    public override bool AlwaysNeedsCompositing => true;

    protected override void OnAttach()
    {
        base.OnAttach();
        _link.RegisterRenderLeader(this);
    }

    protected override void OnDetach()
    {
        _link.UnregisterRenderLeader(this);
        base.OnDetach();
    }

    protected override void PerformLayout()
    {
        base.PerformLayout();
        _previousLayoutSize = Size;
        Link.LeaderSize = Size;
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        LeaderLayer leaderLayer;
        if (Layer == null)
        {
            leaderLayer = new LeaderLayer(link: Link, offset: offset);
            Layer = leaderLayer;
        }
        else
        {
            leaderLayer = (LeaderLayer)Layer;
            leaderLayer.Link = Link;
            leaderLayer.Offset = offset;
        }

        context.PushLayer(leaderLayer, base.Paint, default);
        if (Constants.KDebugMode)
        {
            leaderLayer.DebugCreator = DebugCreator;
        }
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<LayerLink>("link", Link));
    }
}

/// <remarks>
/// Flutter's <c>RenderFollowerLayer</c>. Dart hands <see cref="FollowerLayer"/> a linked offset and the
/// layer derives the transform while compositing; Plumix computes the same transform here at paint
/// time from the linked leader render object and stores it in <see cref="FollowerLayer.LinkedTransform"/>
/// (see docs/ai/DIVERGENCES.md). Dart's non-null <c>Alignment.topLeft</c> anchor defaults are
/// <c>null</c> parameters because <see cref="Alignment"/> is a struct with no constant top-left value.
/// </remarks>
public class RenderFollowerLayer : RenderProxyBox
{
    private LayerLink _link;
    private bool _showWhenUnlinked;
    private Point _offset;
    private Alignment _leaderAnchor;
    private Alignment _followerAnchor;

    public RenderFollowerLayer(
        LayerLink link,
        bool showWhenUnlinked = true,
        Point offset = default,
        Alignment? leaderAnchor = null,
        Alignment? followerAnchor = null,
        RenderBox? child = null) : base(child)
    {
        _link = link;
        _showWhenUnlinked = showWhenUnlinked;
        _offset = offset;
        _leaderAnchor = leaderAnchor ?? Alignment.TopLeft;
        _followerAnchor = followerAnchor ?? Alignment.TopLeft;
    }

    public LayerLink Link
    {
        get => _link;
        set
        {
            if (_link == value)
            {
                return;
            }

            _link = value;
            MarkNeedsPaint();
        }
    }

    public bool ShowWhenUnlinked
    {
        get => _showWhenUnlinked;
        set
        {
            if (_showWhenUnlinked == value)
            {
                return;
            }

            _showWhenUnlinked = value;
            MarkNeedsPaint();
        }
    }

    public Point Offset
    {
        get => _offset;
        set
        {
            if (_offset == value)
            {
                return;
            }

            _offset = value;
            MarkNeedsPaint();
        }
    }

    public Alignment LeaderAnchor
    {
        get => _leaderAnchor;
        set
        {
            if (_leaderAnchor == value)
            {
                return;
            }

            _leaderAnchor = value;
            MarkNeedsPaint();
        }
    }

    public Alignment FollowerAnchor
    {
        get => _followerAnchor;
        set
        {
            if (_followerAnchor == value)
            {
                return;
            }

            _followerAnchor = value;
            MarkNeedsPaint();
        }
    }

    /// <remarks>Dart's <c>detach()</c> override: <c>layer = null; super.detach()</c>.</remarks>
    protected override void OnDetach()
    {
        Layer = null;
        base.OnDetach();
    }

    public override bool AlwaysNeedsCompositing => true;

    /// <summary>The follower layer this render object painted into, if any.</summary>
    /// <remarks>Dart's <c>FollowerLayer? get layer</c> override.</remarks>
    protected internal new FollowerLayer? Layer
    {
        get => (FollowerLayer?)base.Layer;
        set => base.Layer = value;
    }

    public Matrix4 GetCurrentTransform()
    {
        return Layer?.GetLastTransform() ?? Matrix4.Identity();
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        // Disables the hit testing if this render object is hidden.
        if (Link.Leader == null && !ShowWhenUnlinked)
        {
            return false;
        }

        // RenderFollowerLayer objects don't check if they are themselves hit, because it's confusing
        // to think about how the untransformed size and the child's transformed position interact.
        return HitTestChildren(result, position);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        return result.AddWithPaintTransform(
            GetCurrentTransform(),
            position,
            (hitResult, transformed) => base.HitTestChildren(hitResult, transformed));
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        Size? leaderSize = Link.LeaderSize;
        if (Constants.KDebugMode
            && !(Link.LeaderSize != null || Link.Leader == null || LeaderAnchor == Alignment.TopLeft))
        {
            throw new AssertionError(
                $"{Link}: layer is linked to {Link.Leader} but a valid leaderSize is not set. "
                + "leaderSize is required when leaderAnchor is not Alignment.topLeft "
                + $"(current value is {LeaderAnchor}).");
        }

        Point effectiveLinkedOffset = leaderSize == null
            ? Offset
            : LeaderAnchor.AlongSize(leaderSize.Value) - FollowerAnchor.AlongSize(Size) + Offset;
        Matrix4? linkedTransform = ComputeLinkedTransform(effectiveLinkedOffset);
        FollowerLayer? layer = Layer;
        if (layer == null)
        {
            layer = new FollowerLayer(
                link: Link,
                showWhenUnlinked: ShowWhenUnlinked,
                unlinkedOffset: offset,
                linkedTransform: linkedTransform);
            Layer = layer;
        }
        else
        {
            layer.Link = Link;
            layer.ShowWhenUnlinked = ShowWhenUnlinked;
            layer.LinkedTransform = linkedTransform;
            layer.UnlinkedOffset = offset;
        }

        context.PushLayer(
            layer,
            base.Paint,
            default,
            childPaintBounds: new Rect(
                // We don't know where we'll end up, so we have no idea what our cull rect should be.
                new Point(double.NegativeInfinity, double.NegativeInfinity),
                new Point(double.PositiveInfinity, double.PositiveInfinity)));
        if (Constants.KDebugMode)
        {
            layer.DebugCreator = DebugCreator;
        }
    }

    /// <summary>
    /// Plumix-only: the transform Dart's <c>FollowerLayer._establishTransform</c> derives from the layer
    /// tree, computed from the render tree instead. It maps this box's coordinates to the leader's,
    /// translated by <paramref name="linkedOffset"/>.
    /// </summary>
    private Matrix4? ComputeLinkedTransform(Point linkedOffset)
    {
        RenderLeaderLayer? leader = Link.RenderLeader;
        if (leader == null
            || !leader.TryGetTransformFromRoot(out Matrix4 leaderToRoot)
            || !TryGetTransformFromRoot(out Matrix4 followerToRoot))
        {
            return null;
        }

        Matrix4 result = Matrix4.Copy(followerToRoot);
        if (result.Invert() == 0.0)
        {
            // We are in a degenerate transform, so there's not much we can do.
            return null;
        }

        result.Multiply(leaderToRoot);
        result.TranslateByDouble(linkedOffset.X, linkedOffset.Y, 0, 1);
        return result;
    }

    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        transform.Multiply(GetCurrentTransform());
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<LayerLink>("link", Link));
        properties.Add(new DiagnosticsProperty<bool>("showWhenUnlinked", ShowWhenUnlinked));
        properties.Add(new DiagnosticsProperty<Point>("offset", Offset));
        properties.Add(new TransformProperty("current transform matrix", GetCurrentTransform()));
    }
}
