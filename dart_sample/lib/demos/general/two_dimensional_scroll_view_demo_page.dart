import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter/rendering.dart';

import '../../counter_widgets.dart';

class TwoDimensionalScrollViewDemoPage extends StatefulWidget {
  const TwoDimensionalScrollViewDemoPage({super.key});

  @override
  State<TwoDimensionalScrollViewDemoPage> createState() =>
      _TwoDimensionalScrollViewDemoPageState();
}

class _TwoDimensionalScrollViewDemoPageState
    extends State<TwoDimensionalScrollViewDemoPage> {
  static const int columnCount = 20;
  static const int rowCount = 20;

  final ScrollController _verticalController = ScrollController();
  final ScrollController _horizontalController = ScrollController();
  DiagonalDragBehavior _behavior = DiagonalDragBehavior.none;

  @override
  void dispose() {
    _verticalController.dispose();
    _horizontalController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'TwoDimensionalScrollView',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'One viewport scrolled by two positions at once. The diagonal drag behavior '
          'decides whether a drag locks to one axis or moves both.',
          style: TextStyle(fontSize: 14, color: Colors.grey),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _behaviorButton('Locked', DiagonalDragBehavior.none),
            _behaviorButton('Weighted', DiagonalDragBehavior.weightedEvent),
            _behaviorButton('Continuous', DiagonalDragBehavior.weightedContinuous),
            _behaviorButton('Free', DiagonalDragBehavior.free),
          ],
        ),
        Expanded(
          child: SampleTableView(
            delegate: TwoDimensionalChildBuilderDelegate(
              builder: _buildCell,
              maxXIndex: columnCount - 1,
              maxYIndex: rowCount - 1,
            ),
            diagonalDragBehavior: _behavior,
            verticalDetails:
                ScrollableDetails.vertical(controller: _verticalController),
            horizontalDetails:
                ScrollableDetails.horizontal(controller: _horizontalController),
          ),
        ),
      ],
    );
  }

  Widget _behaviorButton(String label, DiagonalDragBehavior behavior) {
    final bool selected = _behavior == behavior;
    return Expanded(
      child: CounterTapButton(
        label: label,
        onTap: () => setState(() => _behavior = behavior),
        background: selected ? Colors.blueGrey : const Color(0xFFB0BEC5),
        foreground: selected ? Colors.white : Colors.black,
        fontSize: 13,
      ),
    );
  }

  static Widget _buildCell(BuildContext context, ChildVicinity vicinity) {
    final bool shaded = (vicinity.xIndex + vicinity.yIndex) % 2 == 0;
    return Container(
      color: shaded ? const Color(0xFFE1F5FE) : const Color(0xFFFFF8E1),
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      child: Text(
        'R${vicinity.yIndex}:C${vicinity.xIndex}',
        style: const TextStyle(fontSize: 13, color: Colors.black),
      ),
    );
  }
}

/// A minimal grid built on [TwoDimensionalScrollView].
class SampleTableView extends TwoDimensionalScrollView {
  const SampleTableView({
    super.key,
    required TwoDimensionalChildBuilderDelegate super.delegate,
    super.diagonalDragBehavior,
    super.verticalDetails,
    super.horizontalDetails,
  });

  @override
  Widget buildViewport(
    BuildContext context,
    ViewportOffset verticalOffset,
    ViewportOffset horizontalOffset,
  ) {
    return SampleTableViewport(
      verticalOffset: verticalOffset,
      verticalAxisDirection: verticalDetails.direction,
      horizontalOffset: horizontalOffset,
      horizontalAxisDirection: horizontalDetails.direction,
      delegate: delegate as TwoDimensionalChildBuilderDelegate,
      mainAxis: mainAxis,
      clipBehavior: clipBehavior,
    );
  }
}

class SampleTableViewport extends TwoDimensionalViewport {
  const SampleTableViewport({
    super.key,
    required super.verticalOffset,
    required super.verticalAxisDirection,
    required super.horizontalOffset,
    required super.horizontalAxisDirection,
    required TwoDimensionalChildBuilderDelegate super.delegate,
    required super.mainAxis,
    super.clipBehavior,
  });

  @override
  RenderTwoDimensionalViewport createRenderObject(BuildContext context) {
    return RenderSampleTableViewport(
      horizontalOffset: horizontalOffset,
      horizontalAxisDirection: horizontalAxisDirection,
      verticalOffset: verticalOffset,
      verticalAxisDirection: verticalAxisDirection,
      delegate: delegate as TwoDimensionalChildBuilderDelegate,
      mainAxis: mainAxis,
      childManager: context as TwoDimensionalChildManager,
      clipBehavior: clipBehavior,
    );
  }

  @override
  void updateRenderObject(
    BuildContext context,
    covariant RenderSampleTableViewport renderObject,
  ) {
    renderObject
      ..horizontalOffset = horizontalOffset
      ..horizontalAxisDirection = horizontalAxisDirection
      ..verticalOffset = verticalOffset
      ..verticalAxisDirection = verticalAxisDirection
      ..mainAxis = mainAxis
      ..delegate = delegate
      ..clipBehavior = clipBehavior;
  }
}

class RenderSampleTableViewport extends RenderTwoDimensionalViewport {
  RenderSampleTableViewport({
    required super.horizontalOffset,
    required super.horizontalAxisDirection,
    required super.verticalOffset,
    required super.verticalAxisDirection,
    required TwoDimensionalChildBuilderDelegate super.delegate,
    required super.mainAxis,
    required super.childManager,
    super.clipBehavior,
  });

  static const double cellWidth = 140.0;
  static const double cellHeight = 44.0;

  @override
  void layoutChildSequence() {
    final builderDelegate = delegate as TwoDimensionalChildBuilderDelegate;
    final int maxColumnIndex = builderDelegate.maxXIndex ?? 0;
    final int maxRowIndex = builderDelegate.maxYIndex ?? 0;
    final double horizontalPixels = horizontalOffset.pixels;
    final double verticalPixels = verticalOffset.pixels;

    final int leadingColumn = math.max((horizontalPixels / cellWidth).floor(), 0);
    final int leadingRow = math.max((verticalPixels / cellHeight).floor(), 0);
    final int trailingColumn = math.min(
      ((horizontalPixels + viewportDimension.width) / cellWidth).ceil(),
      maxColumnIndex,
    );
    final int trailingRow = math.min(
      ((verticalPixels + viewportDimension.height) / cellHeight).ceil(),
      maxRowIndex,
    );

    double xLayoutOffset = (leadingColumn * cellWidth) - horizontalPixels;
    for (int column = leadingColumn; column <= trailingColumn; column++) {
      double yLayoutOffset = (leadingRow * cellHeight) - verticalPixels;
      for (int row = leadingRow; row <= trailingRow; row++) {
        final vicinity = ChildVicinity(xIndex: column, yIndex: row);
        final RenderBox? child = buildOrObtainChildFor(vicinity);
        if (child != null) {
          child.layout(
            constraints.tighten(width: cellWidth, height: cellHeight),
            parentUsesSize: true,
          );
          parentDataOf(child).layoutOffset = Offset(xLayoutOffset, yLayoutOffset);
        }
        yLayoutOffset += cellHeight;
      }
      xLayoutOffset += cellWidth;
    }

    verticalOffset.applyContentDimensions(
      0.0,
      math.max(cellHeight * (maxRowIndex + 1) - viewportDimension.height, 0.0),
    );
    horizontalOffset.applyContentDimensions(
      0.0,
      math.max(cellWidth * (maxColumnIndex + 1) - viewportDimension.width, 0.0),
    );
  }
}
