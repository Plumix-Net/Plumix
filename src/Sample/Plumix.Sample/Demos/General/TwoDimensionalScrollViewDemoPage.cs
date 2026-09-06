using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/general/two_dimensional_scroll_view_demo_page.dart
// (exact sample parity)

namespace Plumix;

public sealed class TwoDimensionalScrollViewDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new TwoDimensionalScrollViewDemoPageState();
    }
}

internal sealed class TwoDimensionalScrollViewDemoPageState : State
{
    private const int ColumnCount = 20;
    private const int RowCount = 20;

    private readonly ScrollController _verticalController = new();
    private readonly ScrollController _horizontalController = new();
    private DiagonalDragBehavior _behavior = DiagonalDragBehavior.None;

    public override void Dispose()
    {
        _verticalController.Dispose();
        _horizontalController.Dispose();
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("TwoDimensionalScrollView", fontSize: 20, color: Colors.Black),
                new Text(
                    "One viewport scrolled by two positions at once. The diagonal drag behavior "
                    + "decides whether a drag locks to one axis or moves both.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BehaviorButton("Locked", DiagonalDragBehavior.None),
                        BehaviorButton("Weighted", DiagonalDragBehavior.WeightedEvent),
                        BehaviorButton("Continuous", DiagonalDragBehavior.WeightedContinuous),
                        BehaviorButton("Free", DiagonalDragBehavior.Free),
                    ]),
                new Expanded(
                    child: new SampleTableView(
                        @delegate: new TwoDimensionalChildBuilderDelegate(
                            BuildCell,
                            maxXIndex: ColumnCount - 1,
                            maxYIndex: RowCount - 1),
                        diagonalDragBehavior: _behavior,
                        verticalDetails: ScrollableDetails.Vertical(controller: _verticalController),
                        horizontalDetails: ScrollableDetails.Horizontal(controller: _horizontalController))),
            ]);
    }

    private Widget BehaviorButton(string label, DiagonalDragBehavior behavior)
    {
        return new Expanded(
            child: new CounterTapButton(
                label: label,
                onTap: () => SetState(() => _behavior = behavior),
                background: _behavior == behavior ? Colors.SteelBlue : Color.Parse("#FFB0BEC5"),
                foreground: _behavior == behavior ? Colors.White : Colors.Black,
                fontSize: 13));
    }

    private static Widget BuildCell(BuildContext context, ChildVicinity vicinity)
    {
        bool shaded = (vicinity.XIndex + vicinity.YIndex) % 2 == 0;
        return new Container(
            color: shaded ? Color.Parse("#FFE1F5FE") : Color.Parse("#FFFFF8E1"),
            padding: new Thickness(10, 8),
            child: new Text(
                $"R{vicinity.YIndex}:C{vicinity.XIndex}",
                fontSize: 13,
                color: Colors.Black));
    }
}

/// <summary>A minimal grid built on <see cref="TwoDimensionalScrollView"/>.</summary>
internal sealed class SampleTableView : TwoDimensionalScrollView
{
    public SampleTableView(
        TwoDimensionalChildBuilderDelegate @delegate,
        DiagonalDragBehavior diagonalDragBehavior,
        ScrollableDetails verticalDetails,
        ScrollableDetails horizontalDetails,
        Key? key = null) : base(
            @delegate,
            diagonalDragBehavior: diagonalDragBehavior,
            verticalDetails: verticalDetails,
            horizontalDetails: horizontalDetails,
            key: key)
    {
    }

    public override Widget BuildViewport(
        BuildContext context,
        ViewportOffset verticalOffset,
        ViewportOffset horizontalOffset)
    {
        return new SampleTableViewport(
            verticalOffset: verticalOffset,
            verticalAxisDirection: VerticalDetails.Direction,
            horizontalOffset: horizontalOffset,
            horizontalAxisDirection: HorizontalDetails.Direction,
            @delegate: (TwoDimensionalChildBuilderDelegate)Delegate,
            mainAxis: MainAxis,
            clipBehavior: ClipBehavior);
    }
}

internal sealed class SampleTableViewport : TwoDimensionalViewport
{
    public SampleTableViewport(
        ViewportOffset verticalOffset,
        AxisDirection verticalAxisDirection,
        ViewportOffset horizontalOffset,
        AxisDirection horizontalAxisDirection,
        TwoDimensionalChildBuilderDelegate @delegate,
        Axis mainAxis,
        Clip clipBehavior,
        Key? key = null) : base(
            verticalOffset,
            verticalAxisDirection,
            horizontalOffset,
            horizontalAxisDirection,
            @delegate,
            mainAxis,
            clipBehavior: clipBehavior,
            key: key)
    {
    }

    public override RenderTwoDimensionalViewport CreateRenderObject(BuildContext context)
    {
        return new RenderSampleTableViewport(
            horizontalOffset: HorizontalOffset,
            horizontalAxisDirection: HorizontalAxisDirection,
            verticalOffset: VerticalOffset,
            verticalAxisDirection: VerticalAxisDirection,
            @delegate: (TwoDimensionalChildBuilderDelegate)Delegate,
            mainAxis: MainAxis,
            childManager: (ITwoDimensionalChildManager)context,
            clipBehavior: ClipBehavior);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var viewport = (RenderSampleTableViewport)renderObject;
        viewport.HorizontalOffset = HorizontalOffset;
        viewport.HorizontalAxisDirection = HorizontalAxisDirection;
        viewport.VerticalOffset = VerticalOffset;
        viewport.VerticalAxisDirection = VerticalAxisDirection;
        viewport.MainAxis = MainAxis;
        viewport.Delegate = Delegate;
        viewport.ClipBehavior = ClipBehavior;
    }
}

internal sealed class RenderSampleTableViewport : RenderTwoDimensionalViewport
{
    private const double CellWidth = 140.0;
    private const double CellHeight = 44.0;

    public RenderSampleTableViewport(
        ViewportOffset horizontalOffset,
        AxisDirection horizontalAxisDirection,
        ViewportOffset verticalOffset,
        AxisDirection verticalAxisDirection,
        TwoDimensionalChildBuilderDelegate @delegate,
        Axis mainAxis,
        ITwoDimensionalChildManager childManager,
        Clip clipBehavior) : base(
            horizontalOffset,
            horizontalAxisDirection,
            verticalOffset,
            verticalAxisDirection,
            @delegate,
            mainAxis,
            childManager,
            clipBehavior: clipBehavior)
    {
    }

    protected override void LayoutChildSequence()
    {
        var builderDelegate = (TwoDimensionalChildBuilderDelegate)Delegate;
        int maxColumnIndex = builderDelegate.MaxXIndex ?? 0;
        int maxRowIndex = builderDelegate.MaxYIndex ?? 0;
        double horizontalPixels = HorizontalOffset.Pixels;
        double verticalPixels = VerticalOffset.Pixels;

        int leadingColumn = Math.Max((int)Math.Floor(horizontalPixels / CellWidth), 0);
        int leadingRow = Math.Max((int)Math.Floor(verticalPixels / CellHeight), 0);
        int trailingColumn = Math.Min(
            (int)Math.Ceiling((horizontalPixels + ViewportDimension.Width) / CellWidth),
            maxColumnIndex);
        int trailingRow = Math.Min(
            (int)Math.Ceiling((verticalPixels + ViewportDimension.Height) / CellHeight),
            maxRowIndex);

        double xLayoutOffset = (leadingColumn * CellWidth) - horizontalPixels;
        for (int column = leadingColumn; column <= trailingColumn; column++)
        {
            double yLayoutOffset = (leadingRow * CellHeight) - verticalPixels;
            for (int row = leadingRow; row <= trailingRow; row++)
            {
                var vicinity = new ChildVicinity(xIndex: column, yIndex: row);
                RenderBox? child = BuildOrObtainChildFor(vicinity);
                if (child != null)
                {
                    child.Layout(
                        Constraints.Tighten(width: CellWidth, height: CellHeight),
                        parentUsesSize: true);
                    ParentDataOf(child).LayoutOffset = new Point(xLayoutOffset, yLayoutOffset);
                }

                yLayoutOffset += CellHeight;
            }

            xLayoutOffset += CellWidth;
        }

        VerticalOffset.ApplyContentDimensions(
            0.0,
            Math.Max((CellHeight * (maxRowIndex + 1)) - ViewportDimension.Height, 0.0));
        HorizontalOffset.ApplyContentDimensions(
            0.0,
            Math.Max((CellWidth * (maxColumnIndex + 1)) - ViewportDimension.Width, 0.0));
    }
}
