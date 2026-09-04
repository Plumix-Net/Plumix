import 'package:flutter/material.dart';

import '../../counter_widgets.dart';

class CustomElementDemoPage extends StatefulWidget {
  const CustomElementDemoPage({super.key});

  @override
  State<CustomElementDemoPage> createState() => _CustomElementDemoPageState();
}

class _CustomElementDemoPageState extends State<CustomElementDemoPage> {
  final ElementLifecycleLog _log = ElementLifecycleLog();
  int _revision = 0;
  int _generation = 0;
  bool _attached = true;

  @override
  void initState() {
    super.initState();
    _log.changed = _handleLogChanged;
  }

  @override
  void dispose() {
    _log.changed = null;
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'Custom Element',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'ElementLifecycleProbe is a widget declared in the sample package that returns its own '
          'Element from createElement. The element drives the child through updateChild and counts '
          'every lifecycle call the framework makes on it.',
          style: TextStyle(fontSize: 14, color: Colors.grey),
        ),
        Container(
          color: const Color(0xFFF4F7FA),
          padding: const EdgeInsets.all(12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            spacing: 6,
            children: <Widget>[
              _countLine('Mount', _log.mounts),
              _countLine('Rebuild', _log.rebuilds),
              _countLine('Update', _log.updates),
              _countLine('Deactivate', _log.deactivations),
              _countLine('Unmount', _log.unmounts),
            ],
          ),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _actionButton('Rebuild', () => setState(() {})),
            _actionButton('New child', () => setState(() => _revision += 1)),
            _actionButton('New key', () => setState(() => _generation += 1)),
            _actionButton(
              _attached ? 'Detach' : 'Attach',
              () => setState(() => _attached = !_attached),
            ),
          ],
        ),
        _buildProbe(),
      ],
    );
  }

  Widget _buildProbe() {
    if (!_attached) {
      return Container(
        color: const Color(0xFFF1F1F1),
        padding: const EdgeInsets.all(12),
        child: const Text(
          'probe detached',
          style: TextStyle(fontSize: 14, color: Colors.grey),
        ),
      );
    }

    return ElementLifecycleProbe(
      key: ValueKey<int>(_generation),
      log: _log,
      child: Container(
        color: const Color(0xFFE1F5FE),
        padding: const EdgeInsets.all(12),
        child: Text(
          'probe child revision $_revision',
          style: const TextStyle(fontSize: 14, color: Colors.black),
        ),
      ),
    );
  }

  static Widget _countLine(String label, int value) {
    return Text(
      '$label: $value',
      style: const TextStyle(fontSize: 14, color: Color(0xFF31506F)),
    );
  }

  Widget _actionButton(String label, VoidCallback onTap) {
    return Expanded(
      child: CounterTapButton(
        label: label,
        onTap: onTap,
        background: Colors.blueGrey,
        foreground: Colors.white,
        fontSize: 13,
      ),
    );
  }

  void _handleLogChanged() {
    if (!mounted) {
      return;
    }

    setState(() {});
  }
}

/// Lifecycle counters shared between the demo page and the element it inflates.
class ElementLifecycleLog {
  int mounts = 0;
  int rebuilds = 0;
  int updates = 0;
  int deactivations = 0;
  int unmounts = 0;

  VoidCallback? changed;

  void recordMount() => _record(() => mounts += 1);

  void recordRebuild() => _record(() => rebuilds += 1);

  void recordUpdate() => _record(() => updates += 1);

  void recordDeactivate() => _record(() => deactivations += 1);

  void recordUnmount() => _record(() => unmounts += 1);

  void _record(VoidCallback mutate) {
    mutate();

    // The counters change while the framework is building or finalizing the tree, so the page is told
    // after the frame instead of from inside the call that changed them.
    WidgetsBinding.instance.addPostFrameCallback((_) => changed?.call());
  }
}

/// A widget that pairs with its own [Element].
class ElementLifecycleProbe extends Widget {
  const ElementLifecycleProbe({
    required this.log,
    required this.child,
    super.key,
  });

  final ElementLifecycleLog log;

  final Widget child;

  @override
  ElementLifecycleProbeElement createElement() =>
      ElementLifecycleProbeElement(this);
}

/// The hand-written element behind [ElementLifecycleProbe].
class ElementLifecycleProbeElement extends Element {
  ElementLifecycleProbeElement(ElementLifecycleProbe super.widget);

  Element? _child;

  ElementLifecycleProbe get _probe => widget as ElementLifecycleProbe;

  @override
  bool get debugDoingBuild => false;

  @override
  Element? get renderObjectAttachingChild => _child;

  @override
  void mount(Element? parent, Object? newSlot) {
    super.mount(parent, newSlot);
    _probe.log.recordMount();
    rebuild();
  }

  @override
  void performRebuild() {
    super.performRebuild();
    _probe.log.recordRebuild();
    _child = updateChild(_child, _probe.child, slot);
  }

  @override
  void update(ElementLifecycleProbe newWidget) {
    super.update(newWidget);
    _probe.log.recordUpdate();
    rebuild(force: true);
  }

  @override
  void deactivate() {
    _probe.log.recordDeactivate();
    super.deactivate();
  }

  @override
  void visitChildren(ElementVisitor visitor) {
    if (_child != null) {
      visitor(_child!);
    }
  }

  @override
  void forgetChild(Element child) {
    assert(child == _child);
    _child = null;
    super.forgetChild(child);
  }

  @override
  void unmount() {
    _probe.log.recordUnmount();
    super.unmount();
  }
}
