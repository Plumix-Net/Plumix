using Avalonia;

namespace Plumix.Rendering;

// Dart parity sources:
// - flutter/packages/flutter/lib/src/rendering/proxy_box.dart (RenderLeaderLayer, RenderFollowerLayer)
// - flutter/packages/flutter/lib/src/rendering/layer.dart (LayerLink, LeaderLayer, FollowerLayer)

public sealed class RenderLeaderLayer : RenderProxyBox
{
    private LayerLink _link;
    private LeaderLayer? _leaderLayer;
    private Size? _previousLayoutSize;

    public RenderLeaderLayer(LayerLink link, RenderBox? child = null)
    {
        _link = link ?? throw new ArgumentNullException(nameof(link));
        Child = child;
    }

    public LayerLink Link
    {
        get => _link;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(_link, value))
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
            if (_previousLayoutSize.HasValue)
            {
                _link.LeaderSize = _previousLayoutSize;
            }

            MarkNeedsPaint();
        }
    }

    protected override bool AlwaysNeedsCompositing => true;

    protected override void OnAttach()
    {
        base.OnAttach();
        _link.RegisterRenderLeader(this);
    }

    protected override void OnDetach()
    {
        _link.UnregisterRenderLeader(this);
        if (_leaderLayer?.Parent != null)
        {
            _leaderLayer.Parent.Remove(_leaderLayer);
        }

        _leaderLayer = null;
        base.OnDetach();
    }

    protected override void PerformLayout()
    {
        base.PerformLayout();
        _previousLayoutSize = Size;
        _link.LeaderSize = Size;
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (_leaderLayer == null)
        {
            _leaderLayer = new LeaderLayer(_link, offset);
        }
        else
        {
            _leaderLayer.Link = _link;
            _leaderLayer.Offset = offset;
        }

        ctx.PushLayer(_leaderLayer, childContext => base.Paint(childContext, default));
    }
}

public sealed class RenderFollowerLayer : RenderProxyBox
{
    private LayerLink _link;
    private bool _showWhenUnlinked;
    private Vector _offset;
    private Alignment _leaderAnchor;
    private Alignment _followerAnchor;
    private FollowerLayer? _followerLayer;

    public RenderFollowerLayer(
        LayerLink link,
        bool showWhenUnlinked = true,
        Vector offset = default,
        Alignment? leaderAnchor = null,
        Alignment? followerAnchor = null,
        RenderBox? child = null)
    {
        _link = link ?? throw new ArgumentNullException(nameof(link));
        _showWhenUnlinked = showWhenUnlinked;
        _offset = offset;
        _leaderAnchor = leaderAnchor ?? Alignment.TopLeft;
        _followerAnchor = followerAnchor ?? Alignment.TopLeft;
        Child = child;
    }

    public LayerLink Link
    {
        get => _link;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(_link, value))
            {
                return;
            }

            _link = value;
            MarkTransformDirty();
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
            MarkTransformDirty();
        }
    }

    public Vector Offset
    {
        get => _offset;
        set
        {
            if (_offset == value)
            {
                return;
            }

            _offset = value;
            MarkTransformDirty();
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
            MarkTransformDirty();
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
            MarkTransformDirty();
        }
    }

    public Matrix GetCurrentTransform()
    {
        return _followerLayer?.GetLastTransform() ?? Matrix.Identity;
    }

    protected override bool AlwaysNeedsCompositing => true;

    protected override void OnDetach()
    {
        if (_followerLayer?.Parent != null)
        {
            _followerLayer.Parent.Remove(_followerLayer);
        }

        _followerLayer = null;
        base.OnDetach();
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        if (_link.Leader == null && !_showWhenUnlinked)
        {
            return false;
        }

        return HitTestChildren(result, position);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (Child == null)
        {
            return false;
        }

        Matrix transform = GetCurrentTransform();
        if (!transform.TryInvert(out Matrix inverse))
        {
            return false;
        }

        var childParentData = (BoxParentData)Child.parentData!;
        Point transformedPosition = inverse.Transform(position) - childParentData.offset;
        return Child.HitTest(result, transformedPosition);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        if (Child == null || (_link.Leader == null && !_showWhenUnlinked))
        {
            return;
        }

        var childParentData = (BoxParentData)Child.parentData!;
        visitor(Child);
    }

    public override void ApplyPaintTransform(RenderObject child, ref Matrix transform)
    {
        base.ApplyPaintTransform(child, ref transform);
        transform = GetCurrentTransform() * transform;
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        Matrix? linkedTransform = ComputeLinkedTransform();
        if (_followerLayer == null)
        {
            _followerLayer = new FollowerLayer(
                _link,
                _showWhenUnlinked,
                offset,
                linkedTransform);
        }
        else
        {
            _followerLayer.Link = _link;
            _followerLayer.ShowWhenUnlinked = _showWhenUnlinked;
            _followerLayer.UnlinkedOffset = offset;
            _followerLayer.LinkedTransform = linkedTransform;
        }

        ctx.PushLayer(_followerLayer, childContext => base.Paint(childContext, default));
    }

    private Matrix? ComputeLinkedTransform()
    {
        RenderLeaderLayer? leader = _link.RenderLeader;
        if (leader == null
            || !_link.LeaderSize.HasValue
            || !leader.TryGetTransformFromRoot(out Matrix leaderToRoot)
            || !TryGetTransformFromRoot(out Matrix followerToRoot)
            || !followerToRoot.TryInvert(out Matrix rootToFollower))
        {
            return null;
        }

        Point leaderPoint = AlongSize(_leaderAnchor, _link.LeaderSize.Value) + _offset;
        Point followerPoint = AlongSize(_followerAnchor, Size);
        Matrix desiredToRoot = Matrix.CreateTranslation(-followerPoint.X, -followerPoint.Y)
                               * Matrix.CreateTranslation(leaderPoint.X, leaderPoint.Y)
                               * leaderToRoot;
        return desiredToRoot * rootToFollower;
    }

    private static Point AlongSize(Alignment alignment, Size size)
    {
        return new Point(
            size.Width * (alignment.X + 1.0) / 2.0,
            size.Height * (alignment.Y + 1.0) / 2.0);
    }

    private void MarkTransformDirty()
    {
        MarkNeedsPaint();
        MarkNeedsSemanticsUpdate();
    }
}
