import 'package:flutter/material.dart';
import 'package:flutter/rendering.dart' show OverflowBoxFit;

import '../../counter_widgets.dart';

class OverflowBoxDemoPage extends StatefulWidget {
  const OverflowBoxDemoPage({super.key});

  @override
  State<OverflowBoxDemoPage> createState() => _OverflowBoxDemoPageState();
}

class _OverflowBoxDemoPageState extends State<OverflowBoxDemoPage> {
  OverflowBoxFit _fit = OverflowBoxFit.max;
  Alignment _alignment = Alignment.center;
  double? _maxWidth = 140;
  double? _maxHeight = 70;
  Size _requestedSize = const Size(90, 44);

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'OverflowBox + SizedOverflowBox',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'OverflowBox overrides child constraints and can stay parent-sized; SizedOverflowBox keeps fixed own size while child may overflow.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'Fit max',
              onTap: () => _setFit(OverflowBoxFit.max),
              width: 92,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'Fit child',
              onTap: () => _setFit(OverflowBoxFit.deferToChild),
              width: 94,
              background: const Color(0xFFDCE3ED),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'TopLeft',
              onTap: () => _setAlignment(Alignment.topLeft),
              width: 92,
              background: const Color(0xFFE9F5EC),
            ),
            _buildButton(
              label: 'Center',
              onTap: () => _setAlignment(Alignment.center),
              width: 92,
              background: const Color(0xFFE9F5EC),
            ),
            _buildButton(
              label: 'BottomRight',
              onTap: () => _setAlignment(Alignment.bottomRight),
              width: 112,
              background: const Color(0xFFE9F5EC),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'max=parent',
              onTap: () => _setMax(null, null),
              width: 98,
              background: const Color(0xFFF3E8D8),
            ),
            _buildButton(
              label: 'max=120x56',
              onTap: () => _setMax(120, 56),
              width: 98,
              background: const Color(0xFFF3E8D8),
            ),
            _buildButton(
              label: 'max=160x92',
              onTap: () => _setMax(160, 92),
              width: 98,
              background: const Color(0xFFF3E8D8),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'req 70x36',
              onTap: () => _setRequestedSize(const Size(70, 36)),
              width: 92,
              background: const Color(0xFFE4ECF7),
            ),
            _buildButton(
              label: 'req 90x44',
              onTap: () => _setRequestedSize(const Size(90, 44)),
              width: 92,
              background: const Color(0xFFE4ECF7),
            ),
            _buildButton(
              label: 'req 130x60',
              onTap: () => _setRequestedSize(const Size(130, 60)),
              width: 102,
              background: const Color(0xFFE4ECF7),
            ),
          ],
        ),
        Text(
          'fit=${_fitLabel(_fit)}, alignment=${_alignmentLabel(_alignment)}, max=${_maxLabel(_maxWidth, _maxHeight)}, requested=${_requestedSize.width.toStringAsFixed(0)}x${_requestedSize.height.toStringAsFixed(0)}',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Container(
          width: 260,
          height: 230,
          color: const Color(0xFFE7EDF6),
          padding: const EdgeInsets.all(10),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            spacing: 8,
            children: <Widget>[
              const Text(
                'OverflowBox preview',
                style: TextStyle(fontSize: 11, color: Colors.black54),
              ),
              Container(
                height: 72,
                color: Colors.white,
                padding: const EdgeInsets.all(6),
                child: Center(
                  child: OverflowBox(
                    alignment: _alignment,
                    maxWidth: _maxWidth,
                    maxHeight: _maxHeight,
                    fit: _fit,
                    child: _buildProbeCard(),
                  ),
                ),
              ),
              const Text(
                'SizedOverflowBox preview',
                style: TextStyle(fontSize: 11, color: Colors.black54),
              ),
              Container(
                height: 86,
                color: Colors.white,
                padding: const EdgeInsets.all(6),
                child: Center(
                  child: SizedOverflowBox(
                    size: _requestedSize,
                    alignment: _alignment,
                    child: _buildProbeCard(),
                  ),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildProbeCard() {
    return Container(
      width: 190,
      height: 86,
      decoration: BoxDecoration(
        color: const Color(0xFFCCE3FF),
        border: Border.all(color: const Color(0xFF1D3557), width: 2),
        borderRadius: BorderRadius.circular(10),
      ),
      child: const Center(
        child: Text(
          'child 190x86',
          style: TextStyle(fontSize: 12, color: Colors.black),
        ),
      ),
    );
  }

  Widget _buildButton({
    required String label,
    required VoidCallback onTap,
    required double width,
    required Color background,
  }) {
    return SizedBox(
      width: width,
      child: CounterTapButton(
        label: label,
        onTap: onTap,
        background: background,
        foreground: Colors.black,
        fontSize: 12,
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      ),
    );
  }

  void _setFit(OverflowBoxFit value) {
    setState(() {
      _fit = value;
    });
  }

  void _setAlignment(Alignment value) {
    setState(() {
      _alignment = value;
    });
  }

  void _setMax(double? maxWidth, double? maxHeight) {
    setState(() {
      _maxWidth = maxWidth;
      _maxHeight = maxHeight;
    });
  }

  void _setRequestedSize(Size value) {
    setState(() {
      _requestedSize = value;
    });
  }

  static String _fitLabel(OverflowBoxFit fit) {
    return fit == OverflowBoxFit.max ? 'max' : 'deferToChild';
  }

  static String _alignmentLabel(Alignment alignment) {
    if (alignment == Alignment.topLeft) {
      return 'topLeft';
    }

    if (alignment == Alignment.center) {
      return 'center';
    }

    if (alignment == Alignment.bottomRight) {
      return 'bottomRight';
    }

    return 'custom';
  }

  static String _maxLabel(double? maxWidth, double? maxHeight) {
    if (maxWidth == null && maxHeight == null) {
      return 'parent';
    }

    return '${maxWidth?.toStringAsFixed(0)}x${maxHeight?.toStringAsFixed(0)}';
  }
}
