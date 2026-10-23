using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/reorderable_list.dart

internal sealed class ReorderDragInfo : Drag, IDisposable
{
    private readonly Axis _scrollDirection;
    private readonly Action<ReorderDragInfo, Point, Point>? _onUpdate;
    private readonly Action<ReorderDragInfo>? _onEnd;
    private readonly Action<ReorderDragInfo>? _onCancel;
    private readonly Action? _onDropCompleted;
    private readonly ReorderItemProxyDecorator? _proxyDecorator;
    private readonly ITickerProvider _tickerProvider;
    private readonly DragBoundaryDelegate<Rect>? _boundary;
    private readonly SliverReorderableListState _listState;
    private readonly Widget _child;
    private readonly BoxConstraints _itemLayoutConstraints;
    private readonly CapturedThemes _capturedThemes;
    private AnimationController? _proxyAnimation;
    private Point _rawDragPosition;

    internal ReorderDragInfo(
        ReorderableItemState item,
        Point initialPosition,
        Axis scrollDirection,
        Action<ReorderDragInfo, Point, Point>? onUpdate,
        Action<ReorderDragInfo>? onCancel,
        Action<ReorderDragInfo>? onEnd,
        Action? onDropCompleted,
        ReorderItemProxyDecorator? proxyDecorator,
        ITickerProvider tickerProvider)
    {
        if (Constants.KDebugMode)
        {
            FoundationDebug.DebugMaybeDispatchCreated("widgets", "_DragInfo", this);
        }

        var box = (RenderBox)item.Context.FindRenderObject()!;
        _scrollDirection = scrollDirection;
        _onUpdate = onUpdate;
        _onCancel = onCancel;
        _onEnd = onEnd;
        _onDropCompleted = onDropCompleted;
        _proxyDecorator = proxyDecorator;
        _tickerProvider = tickerProvider;
        _listState = item.ListState;
        Index = item.Index;
        _child = item.Child;
        _capturedThemes = item.CapturedThemes;
        Point localPosition = box.GlobalToLocal(initialPosition);
        DragOffset = new Vector(localPosition.X, localPosition.Y);
        ItemSize = box.Size;
        Origin = box.LocalToGlobal(default);
        _rawDragPosition = initialPosition;
        _boundary = _listState.CurrentWidget.DragBoundaryProvider is { } provider
            ? provider(_listState.Context)
            : DragBoundary.ForRectMaybeOf(_listState.Context);
        DragPosition = AdjustedDragOffset(initialPosition);
        ItemExtent = scrollDirection == Axis.Vertical ? ItemSize.Height : ItemSize.Width;
        _itemLayoutConstraints = box.Constraints;
    }

    internal int Index { get; }
    internal Point Origin { get; }
    internal Point DragPosition { get; private set; }
    internal Vector DragOffset { get; }
    internal Size ItemSize { get; }
    internal double ItemExtent { get; }

    public void Dispose()
    {
        if (Constants.KDebugMode)
        {
            FoundationDebug.DebugMaybeDispatchDisposed(this);
        }

        _proxyAnimation?.Dispose();
        _proxyAnimation = null;
    }

    internal void StartDrag()
    {
        _proxyAnimation = new AnimationController(duration: TimeSpan.FromMilliseconds(250), vsync: _tickerProvider);
        _proxyAnimation.AddStatusListener(HandleStatus);
        _proxyAnimation.Forward();
    }

    public override void Update(DragUpdateDetails details)
    {
        Vector delta = _scrollDirection == Axis.Vertical
            ? new Vector(0, details.Delta.Y)
            : new Vector(details.Delta.X, 0);
        _rawDragPosition += delta;
        DragPosition = AdjustedDragOffset(_rawDragPosition);
        _onUpdate?.Invoke(this, DragPosition, details.Delta);
    }

    public override void End(DragEndDetails details)
    {
        _proxyAnimation!.Reverse();
        _onEnd?.Invoke(this);
    }

    public override void Cancel()
    {
        _proxyAnimation?.Dispose();
        _proxyAnimation = null;
        _onCancel?.Invoke(this);
    }

    private Point AdjustedDragOffset(Point position) => _boundary is null
        ? position
        : _boundary.NearestPositionWithinBoundary(new Rect(position - DragOffset, ItemSize)).TopLeft + DragOffset;

    private void HandleStatus(AnimationStatus status)
    {
        if (status == AnimationStatus.Dismissed)
        {
            _proxyAnimation?.Dispose();
            _proxyAnimation = null;
            _onDropCompleted?.Invoke();
        }
    }

    internal Widget CreateProxy(BuildContext context) => _capturedThemes.Wrap(new ReorderDragItemProxy(
        _listState, Index, _child, DragPosition - DragOffset - OverlayOrigin(context),
        ItemSize, _itemLayoutConstraints, _proxyAnimation!, _proxyDecorator));

    internal static Vector OverlayOrigin(BuildContext context)
    {
        var box = (RenderBox)Overlay.Of(context).Context.FindRenderObject()!;
        Point origin = box.LocalToGlobal(default);
        return new Vector(origin.X, origin.Y);
    }
}

internal sealed class ReorderDragItemProxy(
    SliverReorderableListState listState,
    int index,
    Widget child,
    Point position,
    Size size,
    BoxConstraints constraints,
    AnimationController animation,
    ReorderItemProxyDecorator? proxyDecorator) : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        Widget proxyChild = proxyDecorator?.Invoke(child, index, animation.View) ?? child;
        Vector overlayOrigin = ReorderDragInfo.OverlayOrigin(context);
        return MediaQuery.RemovePadding(context, new AnimatedBuilder(
            animation,
            (buildContext, animatedChild) =>
            {
                Point effectivePosition = position;
                if (listState.FinalDropPosition is { } dropPosition)
                {
                    Point target = dropPosition - overlayOrigin;
                    effectivePosition = target + ((position - target) * Curves.EaseOut.Transform(animation.Value));
                }

                return new Positioned(
                    left: effectivePosition.X,
                    top: effectivePosition.Y,
                    child: new SizedBox(
                        width: size.Width,
                        height: size.Height,
                        child: new OverflowBox(
                            minWidth: constraints.MinWidth,
                            minHeight: constraints.MinHeight,
                            maxWidth: constraints.MaxWidth,
                            maxHeight: constraints.MaxHeight,
                            alignment: listState.ScrollAxis == Axis.Horizontal
                                ? Alignment.CenterLeft
                                : Alignment.TopCenter,
                            child: animatedChild)));
            },
            child: proxyChild), removeTop: true);
    }
}
