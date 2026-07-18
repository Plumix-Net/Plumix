import 'package:flutter/material.dart';

import '../../counter_widgets.dart';

class AlignDemoPage extends StatefulWidget {
  const AlignDemoPage({super.key});

  @override
  State<AlignDemoPage> createState() => _AlignDemoPageState();
}

class _AlignDemoPageState extends State<AlignDemoPage> {
  Alignment _alignment = Alignment.center;
  bool _shrinkWrap = false;
  bool _expandedPadding = false;
  bool _faded = false;
  bool _shifted = false;
  int _completedAnimations = 0;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'AnimatedAlign + AnimatedPadding',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Move the card and change its inset; both values transition implicitly with easeInOut.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: 'TopLeft',
              onTap: () => _setAlignment(Alignment.topLeft),
              width: 96,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'Center',
              onTap: () => _setAlignment(Alignment.center),
              width: 96,
              background: const Color(0xFFDCE3ED),
            ),
            _buildButton(
              label: 'BottomRight',
              onTap: () => _setAlignment(Alignment.bottomRight),
              width: 112,
              background: const Color(0xFFDCE3ED),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: _shrinkWrap ? 'Shrink: on' : 'Shrink: off',
              onTap: _toggleShrinkWrap,
              width: 120,
              background: const Color(0xFFE9F5EC),
            ),
            _buildButton(
              label: _expandedPadding ? 'Padding: 24' : 'Padding: 8',
              onTap: _togglePadding,
              width: 120,
              background: const Color(0xFFFFE8CC),
            ),
          ],
        ),
        Text(
          'alignment=${_alignmentLabel(_alignment)}, shrink=${_shrinkWrap ? 'on' : 'off'}, '
          'padding=${_expandedPadding ? 24 : 8}, completed=$_completedAnimations',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Container(
          width: 220,
          height: 140,
          color: const Color(0xFFE7EDF6),
          child: AnimatedPadding(
            padding: EdgeInsets.all(_expandedPadding ? 24 : 8),
            duration: const Duration(milliseconds: 350),
            curve: Curves.easeInOut,
            onEnd: _handleAnimationEnd,
            child: Container(
              color: Colors.white,
              child: AnimatedAlign(
                alignment: _alignment,
                duration: const Duration(milliseconds: 350),
                curve: Curves.easeInOut,
                onEnd: _handleAnimationEnd,
                widthFactor: _shrinkWrap ? 1.5 : null,
                heightFactor: _shrinkWrap ? 1.5 : null,
                child: Container(
                  width: 64,
                  height: 40,
                  color: const Color(0xFF1D3557),
                  child: const Center(
                    child: Text(
                      'A',
                      style: TextStyle(fontSize: 16, color: Colors.white),
                    ),
                  ),
                ),
              ),
            ),
          ),
        ),
        const Text(
          'AnimatedOpacity + AnimatedSlide',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Fade and move the same child by a size-relative offset; hit testing follows the slide.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: _faded ? 'Opacity: 0.2' : 'Opacity: 1.0',
              onTap: _toggleOpacity,
              width: 120,
              background: const Color(0xFFF4E1F0),
            ),
            _buildButton(
              label: _shifted ? 'Offset: (0.75,-0.5)' : 'Offset: zero',
              onTap: _toggleOffset,
              width: 160,
              background: const Color(0xFFE1F1F4),
            ),
          ],
        ),
        Container(
          width: 220,
          height: 110,
          color: const Color(0xFFF3F5F8),
          child: Center(
            child: AnimatedSlide(
              offset: _shifted ? const Offset(0.75, -0.5) : Offset.zero,
              duration: const Duration(milliseconds: 350),
              curve: Curves.easeInOut,
              onEnd: _handleAnimationEnd,
              child: AnimatedOpacity(
                opacity: _faded ? 0.2 : 1,
                duration: const Duration(milliseconds: 350),
                curve: Curves.easeInOut,
                onEnd: _handleAnimationEnd,
                child: Container(
                  width: 72,
                  height: 44,
                  color: const Color(0xFF7B2CBF),
                  child: const Center(
                    child: Text(
                      'move',
                      style: TextStyle(fontSize: 14, color: Colors.white),
                    ),
                  ),
                ),
              ),
            ),
          ),
        ),
      ],
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

  void _setAlignment(Alignment alignment) {
    setState(() {
      _alignment = alignment;
    });
  }

  void _toggleShrinkWrap() {
    setState(() {
      _shrinkWrap = !_shrinkWrap;
    });
  }

  void _togglePadding() {
    setState(() {
      _expandedPadding = !_expandedPadding;
    });
  }

  void _toggleOpacity() {
    setState(() {
      _faded = !_faded;
    });
  }

  void _toggleOffset() {
    setState(() {
      _shifted = !_shifted;
    });
  }

  void _handleAnimationEnd() {
    setState(() {
      _completedAnimations += 1;
    });
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
}
