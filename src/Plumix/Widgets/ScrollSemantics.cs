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
        AxisDirection axisDirection,
        int? semanticChildCount,
        Action<DragUpdateDetails>? onSemanticDrag = null,
        Widget? child = null,
        Key? key = null) : base(child, key)
    {
        if (semanticChildCount is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(semanticChildCount));
        }

        Position = position;
        AllowImplicitScrolling = allowImplicitScrolling;
        AxisDirection = axisDirection;
        SemanticChildCount = semanticChildCount;
        OnSemanticDrag = onSemanticDrag;
    }

    public ScrollPosition Position { get; }

    public bool AllowImplicitScrolling { get; }

    public AxisDirection AxisDirection { get; }

    public int? SemanticChildCount { get; }

    public Action<DragUpdateDetails>? OnSemanticDrag { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderScrollSemantics(
            position: Position,
            allowImplicitScrolling: AllowImplicitScrolling,
            axisDirection: AxisDirection,
            semanticChildCount: SemanticChildCount)
        {
            OnSemanticDrag = OnSemanticDrag
        };
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var scrollSemantics = (RenderScrollSemantics)renderObject;
        scrollSemantics.AllowImplicitScrolling = AllowImplicitScrolling;
        scrollSemantics.AxisDirection = AxisDirection;
        scrollSemantics.Position = Position;
        scrollSemantics.SemanticChildCount = SemanticChildCount;
        scrollSemantics.OnSemanticDrag = OnSemanticDrag;
    }
}

/// <summary>
/// The render object behind <see cref="ScrollSemantics"/>.
/// </summary>
/// <remarks>
/// Flutter's private <c>_RenderScrollSemantics</c>, plus the four directional scroll actions. Flutter
/// puts those on the <c>RenderSemanticsGestureHandler</c> below this boundary and lets them merge up;
/// Plumix's compiler forms a node for every configuration that carries actions, so they are registered
/// here instead. The resulting node contents are the same.
/// </remarks>
internal sealed class RenderScrollSemantics : RenderProxyBox
{
    /// <summary>The fraction of the viewport a single semantic scroll action moves.</summary>
    /// <remarks>Flutter's <c>RenderSemanticsGestureHandler.scrollFactor</c>.</remarks>
    private const double ScrollFactor = 0.8;

    private ScrollPosition _position;
    private bool _allowImplicitScrolling;
    private int? _semanticChildCount;
    private SemanticsNode? _innerNode;

    public RenderScrollSemantics(
        ScrollPosition position,
        bool allowImplicitScrolling,
        AxisDirection axisDirection,
        int? semanticChildCount,
        RenderBox? child = null)
    {
        _position = position;
        _allowImplicitScrolling = allowImplicitScrolling;
        _semanticChildCount = semanticChildCount;
        AxisDirection = axisDirection;
        Child = child;
        _position.AddListener(MarkNeedsSemanticsUpdate);
    }

    public AxisDirection AxisDirection { get; set; }

    private Axis Axis => ScrollDirectionUtils.AxisDirectionToAxis(AxisDirection);

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

    /// <summary>
    /// Runs one synthetic drag through the scrollable's own drag pipeline, so a semantic scroll obeys
    /// the scroll physics exactly like a real drag.
    /// </summary>
    /// <remarks>Flutter's <c>_DefaultSemanticsGestureDelegate</c> drag replay.</remarks>
    public Action<DragUpdateDetails>? OnSemanticDrag { get; set; }

    /// <summary>
    /// The scroll actions the position currently accepts: the backward one while there is content
    /// before the current offset, the forward one while there is content after it.
    /// </summary>
    /// <remarks>Flutter's <c>ScrollPosition._updateSemanticActions</c>.</remarks>
    private SemanticsActions ValidActions
    {
        get
        {
            (SemanticsActions forward, SemanticsActions backward) = AxisDirection switch
            {
                AxisDirection.Up => (SemanticsActions.ScrollDown, SemanticsActions.ScrollUp),
                AxisDirection.Down => (SemanticsActions.ScrollUp, SemanticsActions.ScrollDown),
                AxisDirection.Left => (SemanticsActions.ScrollRight, SemanticsActions.ScrollLeft),
                _ => (SemanticsActions.ScrollLeft, SemanticsActions.ScrollRight)
            };

            SemanticsActions actions = SemanticsActions.None;
            if (_position.Pixels > _position.MinScrollExtent)
            {
                actions |= backward;
            }

            if (_position.Pixels < _position.MaxScrollExtent)
            {
                actions |= forward;
            }

            return actions;
        }
    }

    private bool IsValidAction(SemanticsActions action)
    {
        return _position.HaveDimensions && (ValidActions & action) != SemanticsActions.None;
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        configuration.IsSemanticBoundary = true;
        // Flutter keeps the viewport's nodes explicit through a `Semantics(explicitChildNodes: true)`
        // below this boundary. Plumix resolves explicit-child-node-ness at the boundary itself, so the
        // flag is declared here; the outcome — every viewport child stays its own node — is identical.
        configuration.ExplicitChildNodes = true;
        configuration.HasImplicitScrolling = _allowImplicitScrolling;

        if (OnSemanticDrag is not null)
        {
            if (Axis == Axis.Vertical)
            {
                if (IsValidAction(SemanticsActions.ScrollUp))
                {
                    configuration.OnScrollUp = PerformSemanticScrollUp;
                }

                if (IsValidAction(SemanticsActions.ScrollDown))
                {
                    configuration.OnScrollDown = PerformSemanticScrollDown;
                }
            }
            else
            {
                if (IsValidAction(SemanticsActions.ScrollLeft))
                {
                    configuration.OnScrollLeft = PerformSemanticScrollLeft;
                }

                if (IsValidAction(SemanticsActions.ScrollRight))
                {
                    configuration.OnScrollRight = PerformSemanticScrollRight;
                }
            }
        }

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

    private void PerformSemanticScrollLeft() => PerformSemanticScroll(Size.Width * -ScrollFactor, horizontal: true);

    private void PerformSemanticScrollRight() => PerformSemanticScroll(Size.Width * ScrollFactor, horizontal: true);

    private void PerformSemanticScrollUp() => PerformSemanticScroll(Size.Height * -ScrollFactor, horizontal: false);

    private void PerformSemanticScrollDown() => PerformSemanticScroll(Size.Height * ScrollFactor, horizontal: false);

    private void PerformSemanticScroll(double primaryDelta, bool horizontal)
    {
        if (OnSemanticDrag is not { } drag)
        {
            return;
        }

        var localCenter = new Point(Size.Width / 2.0, Size.Height / 2.0);
        var delta = horizontal ? new Point(primaryDelta, 0.0) : new Point(0.0, primaryDelta);
        drag(new DragUpdateDetails(
            GlobalPosition: LocalToGlobal(localCenter),
            LocalPosition: localCenter,
            Delta: delta,
            PrimaryDelta: primaryDelta));
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
            node.UpdateWith(config, children);
            return;
        }

        _innerNode ??= Owner!.SemanticsOwner.CreateDetachedNode();
        _innerNode.Rect = node.Rect;
        _innerNode.ShowOnScreenRequest = () => ShowOnScreen();

        int? firstVisibleIndex = null;
        var excluded = new List<SemanticsNode> { _innerNode };
        var included = new List<SemanticsNode>();
        foreach (SemanticsNode child in children)
        {
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
