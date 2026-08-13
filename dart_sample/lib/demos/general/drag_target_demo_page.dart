import 'package:material_ui/material_ui.dart';

class DragTargetDemoPage extends StatefulWidget {
  const DragTargetDemoPage({super.key});

  @override
  State<DragTargetDemoPage> createState() => _DragTargetDemoPageState();
}

class _DragTargetDemoPageState extends State<DragTargetDemoPage> {
  final OverlayPortalController _portalController = OverlayPortalController(
    debugLabel: 'drag-demo',
  );
  final Object _portalGroup = Object();
  int _acceptedCount = 0;
  String _status = 'Drag either item onto the target.';

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 16,
        children: <Widget>[
          const Text(
            'Draggable + LongPressDraggable + DragTarget',
            style: TextStyle(fontSize: 20, color: Colors.black),
          ),
          const Text(
            'Long-press the plum to drag it; the immediate stone exercises '
            'rejectedData and onLeave.',
            style: TextStyle(fontSize: 14, color: Colors.black54),
          ),
          Wrap(
            spacing: 16,
            runSpacing: 16,
            children: <Widget>[
              _buildDraggable('plum', const Color(0xFF6750A4), longPress: true),
              _buildDraggable(
                'stone',
                const Color(0xFF5F6368),
                longPress: false,
              ),
              _buildTarget(),
            ],
          ),
          Container(
            color: const Color(0xFFF4F0FA),
            padding: const EdgeInsets.all(12),
            child: Text(
              'accepted=$_acceptedCount; $_status',
              style: const TextStyle(fontSize: 13, color: Color(0xFF332D41)),
            ),
          ),
          Align(
            alignment: Alignment.centerLeft,
            child: TextButton(onPressed: _reset, child: const Text('Reset')),
          ),
          const Text(
            'OverlayPortal + TapRegion',
            style: TextStyle(fontSize: 20, color: Colors.black),
          ),
          const Text(
            'Open the portal, interact with it as one grouped region, then tap '
            'elsewhere to dismiss.',
            style: TextStyle(fontSize: 14, color: Colors.black54),
          ),
          _buildPortalProbe(),
        ],
      ),
    );
  }

  Widget _buildPortalProbe() {
    return OverlayPortal.overlayChildLayoutBuilder(
      controller: _portalController,
      overlayChildBuilder:
          (BuildContext context, OverlayChildLayoutInfo info) {
        final Offset origin = MatrixUtils.transformPoint(
          info.childPaintTransform,
          Offset.zero,
        );
        return Positioned(
          left: origin.dx,
          top: origin.dy + info.childSize.height + 8,
          width: 220,
          height: 72,
          child: TapRegion(
            groupId: _portalGroup,
            onTapOutside: (_) {
              _portalController.hide();
              _setStatus('portal dismissed by outside tap');
            },
            behavior: HitTestBehavior.opaque,
            child: Container(
              color: const Color(0xFFEADDFF),
              padding: const EdgeInsets.all(12),
              alignment: Alignment.center,
              child: const Text(
                'Portal inherits page context.\nTap here, then outside.',
                style: TextStyle(fontSize: 13, color: Color(0xFF332D41)),
              ),
            ),
          ),
        );
      },
      child: TapRegion(
        groupId: _portalGroup,
        child: Align(
          alignment: Alignment.centerLeft,
          child: TextButton(
            onPressed: () {
              _portalController.toggle();
              _setStatus(
                _portalController.isShowing
                    ? 'portal opened'
                    : 'portal closed',
              );
            },
            child: const Text('Toggle portal'),
          ),
        ),
      ),
    );
  }

  Widget _buildDraggable(String data, Color color, {required bool longPress}) {
    final String label = longPress ? '$data\n(long press)' : data;
    final Widget tile = _buildTile(label, color, opacity: 1);
    final Widget childWhenDragging = _buildTile(label, color, opacity: 0.35);
    final Widget feedback = _buildTile(label, color, opacity: 0.9);
    if (longPress) {
      return LongPressDraggable<String>(
        data: data,
        childWhenDragging: childWhenDragging,
        feedback: feedback,
        hitTestBehavior: HitTestBehavior.opaque,
        onDragStarted: () => _setStatus('long-press dragging $data'),
        onDragCompleted: () => _setStatus('$data accepted'),
        onDraggableCanceled: (_, _) => _setStatus('$data not accepted'),
        child: tile,
      );
    }

    return Draggable<String>(
      data: data,
      childWhenDragging: childWhenDragging,
      feedback: feedback,
      hitTestBehavior: HitTestBehavior.opaque,
      onDragStarted: () => _setStatus('dragging $data'),
      onDragCompleted: () => _setStatus('$data accepted'),
      onDraggableCanceled: (_, _) => _setStatus('$data not accepted'),
      child: tile,
    );
  }

  Widget _buildTarget() {
    return DragTarget<String>(
      onWillAcceptWithDetails: (DragTargetDetails<String> details) =>
          details.data == 'plum',
      onAcceptWithDetails: (DragTargetDetails<String> details) {
        setState(() {
          _acceptedCount += 1;
          _status =
              '${details.data} dropped at '
              '(${details.offset.dx.toStringAsFixed(0)}, '
              '${details.offset.dy.toStringAsFixed(0)})';
        });
      },
      onLeave: (String? data) => _setStatus('${data ?? 'item'} left target'),
      builder:
          (
            BuildContext context,
            List<String?> candidates,
            List<dynamic> rejected,
          ) {
            final Color color = candidates.isNotEmpty
                ? const Color(0xFFD8F5D0)
                : rejected.isNotEmpty
                ? const Color(0xFFFFDAD6)
                : const Color(0xFFE7E0EC);
            final String label = candidates.isNotEmpty
                ? 'Release to accept'
                : rejected.isNotEmpty
                ? 'Rejected'
                : 'Drop target';
            return Container(
              width: 190,
              height: 96,
              color: color,
              alignment: Alignment.center,
              child: Text(
                label,
                style: const TextStyle(fontSize: 14, color: Colors.black),
              ),
            );
          },
    );
  }

  static Widget _buildTile(
    String label,
    Color color, {
    required double opacity,
  }) {
    return Opacity(
      opacity: opacity,
      child: Container(
        width: 96,
        height: 64,
        color: color,
        alignment: Alignment.center,
        child: Text(
          label,
          style: const TextStyle(fontSize: 14, color: Colors.white),
        ),
      ),
    );
  }

  void _setStatus(String status) {
    if (mounted) {
      setState(() => _status = status);
    }
  }

  void _reset() {
    setState(() {
      _acceptedCount = 0;
      _status = 'Drag either item onto the target.';
    });
  }
}
