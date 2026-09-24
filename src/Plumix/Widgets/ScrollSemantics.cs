using System.Diagnostics;
using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/scrollable.dart

namespace Plumix.Widgets;

/// <summary>
/// The semantics boundary a <see cref="Scrollable"/> puts above its viewport. It reports the scroll
/// metrics and actions, and splits the viewport's semantics nodes into a scrolling pane and the
/// non-scrolling siblings a pinned header contributes.
/// </summary>
/// <remarks>Flutter's private <c>_ScrollSemantics</c>.</remarks>
internal sealed class ScrollSemantics : SingleChildRenderObjectWidget
{
    public ScrollSemantics(
        ScrollPosition position,
        bool allowImplicitScrolling,
        Axis axis,
        int? semanticChildCount,
        Widget? child = null,
        Key? key = null) : base(child, key)
    {
        Debug.Assert(semanticChildCount is null or >= 0);
        Position = position;
        AllowImplicitScrolling = allowImplicitScrolling;
        Axis = axis;
        SemanticChildCount = semanticChildCount;
    }

    public ScrollPosition Position { get; }

    public bool AllowImplicitScrolling { get; }

    public Axis Axis { get; }

    public int? SemanticChildCount { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderScrollSemantics(
            position: Position,
            allowImplicitScrolling: AllowImplicitScrolling,
            axis: Axis,
            semanticChildCount: SemanticChildCount);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var scrollSemantics = (RenderScrollSemantics)renderObject;
        scrollSemantics.AllowImplicitScrolling = AllowImplicitScrolling;
        scrollSemantics.Axis = Axis;
        scrollSemantics.Position = Position;
        scrollSemantics.SemanticChildCount = SemanticChildCount;
    }
}

/// <summary>
/// The render object behind <see cref="ScrollSemantics"/>.
/// </summary>
/// <remarks>
/// Flutter's private <c>_RenderScrollSemantics</c>. The four directional scroll actions live on the
/// <see cref="RenderSemanticsGestureHandler"/> the scrollable's <c>RawGestureDetector</c> creates
/// below this boundary, and merge up into the node formed here.
/// </remarks>
internal sealed class RenderScrollSemantics : RenderProxyBox
{
    private ScrollPosition _position;
    private bool _allowImplicitScrolling;
    private int? _semanticChildCount;
    private SemanticsNode? _innerNode;

    public RenderScrollSemantics(
        ScrollPosition position,
        bool allowImplicitScrolling,
        Axis axis,
        int? semanticChildCount,
        RenderBox? child = null)
    {
        _position = position;
        _allowImplicitScrolling = allowImplicitScrolling;
        _semanticChildCount = semanticChildCount;
        Axis = axis;
        Child = child;
        _position.AddListener(MarkNeedsSemanticsUpdate);
    }

    public Axis Axis { get; set; }

    public ScrollPosition Position
    {
        get => _position;
        set
        {
            if (ReferenceEquals(value, _position))
            {
                return;
            }

            _position.RemoveListener(MarkNeedsSemanticsUpdate);
            _position = value;
            _position.AddListener(MarkNeedsSemanticsUpdate);
            MarkNeedsSemanticsUpdate();
        }
    }

    public bool AllowImplicitScrolling
    {
        get => _allowImplicitScrolling;
        set
        {
            if (value == _allowImplicitScrolling)
            {
                return;
            }

            _allowImplicitScrolling = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public int? SemanticChildCount
    {
        get => _semanticChildCount;
        set
        {
            if (value == _semanticChildCount)
            {
                return;
            }

            _semanticChildCount = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        configuration.IsSemanticBoundary = true;
        configuration.HasImplicitScrolling = _allowImplicitScrolling;

        if (!_position.HaveDimensions)
        {
            return;
        }

        configuration.ScrollPosition = _position.Pixels;
        configuration.ScrollExtentMax = _position.MaxScrollExtent;
        configuration.ScrollExtentMin = _position.MinScrollExtent;
        configuration.ScrollChildCount = _semanticChildCount;
        if (_position.MaxScrollExtent > _position.MinScrollExtent && _allowImplicitScrolling)
        {
            configuration.OnScrollToOffset = HandleScrollToOffset;
        }
    }

    private void HandleScrollToOffset(Point targetOffset)
    {
        _position.JumpTo(Axis == Axis.Horizontal ? targetOffset.X : targetOffset.Y);
    }

    protected override void ClearOwnSemantics()
    {
        base.ClearOwnSemantics();
        _innerNode = null;
    }

    /// <summary>
    /// Splits the viewport's nodes into the scrolling pane and its non-scrolling siblings.
    /// </summary>
    protected override void AssembleSemanticsNode(
        SemanticsNode node,
        SemanticsConfiguration config,
        IReadOnlyList<SemanticsNode> children)
    {
        if (children.Count == 0 || !children[0].IsTagged(RenderViewport.UseTwoPaneSemantics))
        {
            _innerNode = null;
            base.AssembleSemanticsNode(node, config, children);
            return;
        }

        (_innerNode ??= new SemanticsNode(showOnScreen: () => ShowOnScreen())).Rect = node.Rect;

        int? firstVisibleIndex = null;
        var excluded = new List<SemanticsNode> { _innerNode };
        var included = new List<SemanticsNode>();
        foreach (SemanticsNode child in children)
        {
            Debug.Assert(child.IsTagged(RenderViewport.UseTwoPaneSemantics));
            if (child.IsTagged(RenderViewport.ExcludeFromScrolling))
            {
                excluded.Add(child);
            }
            else
            {
                if (!child.IsHidden)
                {
                    firstVisibleIndex ??= child.IndexInParent;
                }

                included.Add(child);
            }
        }

        config.ScrollIndex = firstVisibleIndex;
        node.UpdateWith(config: null, excluded);
        _innerNode.UpdateWith(config, included);
    }
}
