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
  bool _scaled = false;
  bool _rotated = false;
  bool _positioned = false;
  bool _rightToLeft = false;
  bool _emphasizedText = false;
  bool _raisedSurface = false;
  int _switcherValue = 0;
  bool _showSecondCrossFade = false;
  bool _expandedFraction = false;
  bool _visibleSliver = true;
  int _completedAnimations = 0;
  late final ScrollController _scrollController;

  @override
  void initState() {
    super.initState();
    _scrollController = ScrollController();
  }

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scrollbar(
      controller: _scrollController,
      thumbVisibility: true,
      child: SingleChildScrollView(
        controller: _scrollController,
        padding: const EdgeInsets.only(right: 12, bottom: 12),
        child: Column(
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
            const Text(
              'AnimatedScale + AnimatedRotation',
              style: TextStyle(fontSize: 20, color: Colors.black),
            ),
            const Text(
              'Scale and rotate around a bottom-right pivot; transform filtering follows the animated child.',
              style: TextStyle(fontSize: 14, color: Colors.black54),
            ),
            Row(
              spacing: 8,
              children: <Widget>[
                _buildButton(
                  label: _scaled ? 'Scale: 1.6' : 'Scale: 1.0',
                  onTap: _toggleScale,
                  width: 120,
                  background: const Color(0xFFE8E0F4),
                ),
                _buildButton(
                  label: _rotated ? 'Turns: 0.125' : 'Turns: 0',
                  onTap: _toggleRotation,
                  width: 128,
                  background: const Color(0xFFF7E6CF),
                ),
              ],
            ),
            Container(
              width: 220,
              height: 130,
              color: const Color(0xFFF3F5F8),
              child: Center(
                child: AnimatedRotation(
                  turns: _rotated ? 0.125 : 0,
                  duration: const Duration(milliseconds: 350),
                  alignment: Alignment.bottomRight,
                  filterQuality: FilterQuality.high,
                  curve: Curves.easeInOut,
                  onEnd: _handleAnimationEnd,
                  child: AnimatedScale(
                    scale: _scaled ? 1.6 : 1,
                    duration: const Duration(milliseconds: 350),
                    alignment: Alignment.bottomRight,
                    filterQuality: FilterQuality.high,
                    curve: Curves.easeInOut,
                    onEnd: _handleAnimationEnd,
                    child: Container(
                      width: 72,
                      height: 44,
                      color: const Color(0xFFB85C38),
                      child: const Center(
                        child: Text(
                          'turn',
                          style: TextStyle(fontSize: 14, color: Colors.white),
                        ),
                      ),
                    ),
                  ),
                ),
              ),
            ),
            const Text(
              'AnimatedPositioned + AnimatedPositionedDirectional',
              style: TextStyle(fontSize: 20, color: Colors.black),
            ),
            const Text(
              'Animate physical and logical Stack insets; switching direction resolves start/end immediately.',
              style: TextStyle(fontSize: 14, color: Colors.black54),
            ),
            Row(
              spacing: 8,
              children: <Widget>[
                _buildButton(
                  label: _positioned ? 'Position: end' : 'Position: start',
                  onTap: _togglePosition,
                  width: 132,
                  background: const Color(0xFFDDEBF7),
                ),
                _buildButton(
                  label: _rightToLeft ? 'Direction: RTL' : 'Direction: LTR',
                  onTap: _toggleDirection,
                  width: 132,
                  background: const Color(0xFFF4E6C8),
                ),
              ],
            ),
            Container(
              width: 240,
              height: 140,
              color: const Color(0xFFF3F5F8),
              child: Stack(
                children: <Widget>[
                  AnimatedPositioned(
                    left: _positioned ? 154 : 10,
                    top: _positioned ? 18 : 10,
                    width: _positioned ? 70 : 48,
                    height: 40,
                    duration: const Duration(milliseconds: 350),
                    curve: Curves.easeInOut,
                    onEnd: _handleAnimationEnd,
                    child: Container(
                      color: const Color(0xFF2A6F97),
                      child: const Center(
                        child: Text(
                          'left',
                          style: TextStyle(fontSize: 12, color: Colors.white),
                        ),
                      ),
                    ),
                  ),
                  Directionality(
                    textDirection: _rightToLeft
                        ? TextDirection.rtl
                        : TextDirection.ltr,
                    child: AnimatedPositionedDirectional(
                      start: _positioned ? 136 : 10,
                      top: 86,
                      width: _positioned ? 88 : 58,
                      height: 40,
                      duration: const Duration(milliseconds: 350),
                      curve: Curves.easeInOut,
                      onEnd: _handleAnimationEnd,
                      child: Container(
                        color: const Color(0xFF6A4C93),
                        child: const Center(
                          child: Text(
                            'start',
                            style: TextStyle(fontSize: 12, color: Colors.white),
                          ),
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const Text(
              'AnimatedDefaultTextStyle + AnimatedPhysicalModel',
              style: TextStyle(fontSize: 20, color: Colors.black),
            ),
            const Text(
              'Animate inherited typography and physical surface radius, elevation, fill, and shadow.',
              style: TextStyle(fontSize: 14, color: Colors.black54),
            ),
            Row(
              spacing: 8,
              children: <Widget>[
                _buildButton(
                  label: _emphasizedText ? 'Text: emphasized' : 'Text: normal',
                  onTap: _toggleTextStyle,
                  width: 144,
                  background: const Color(0xFFE7E0F2),
                ),
                _buildButton(
                  label: _raisedSurface ? 'Surface: raised' : 'Surface: flat',
                  onTap: _togglePhysicalModel,
                  width: 136,
                  background: const Color(0xFFE2EFE7),
                ),
              ],
            ),
            Row(
              spacing: 18,
              children: <Widget>[
                SizedBox(
                  width: 150,
                  child: AnimatedDefaultTextStyle(
                    style: TextStyle(
                      fontSize: _emphasizedText ? 22 : 14,
                      color: _emphasizedText
                          ? const Color(0xFF6A1B9A)
                          : const Color(0xFF264653),
                      fontWeight: _emphasizedText
                          ? FontWeight.bold
                          : FontWeight.normal,
                      letterSpacing: _emphasizedText ? 1.2 : 0.1,
                    ),
                    duration: const Duration(milliseconds: 350),
                    textAlign: TextAlign.center,
                    maxLines: 1,
                    curve: Curves.easeInOut,
                    onEnd: _handleAnimationEnd,
                    child: const Text('inherited style'),
                  ),
                ),
                AnimatedPhysicalModel(
                  color: _raisedSurface
                      ? const Color(0xFF2A9D8F)
                      : const Color(0xFF457B9D),
                  shadowColor: const Color(0xFF1D3557),
                  duration: const Duration(milliseconds: 350),
                  clipBehavior: Clip.antiAlias,
                  borderRadius: BorderRadius.circular(_raisedSurface ? 24 : 4),
                  elevation: _raisedSurface ? 12 : 0,
                  curve: Curves.easeInOut,
                  onEnd: _handleAnimationEnd,
                  child: const SizedBox(
                    width: 110,
                    height: 64,
                    child: Center(
                      child: Text(
                        'surface',
                        style: TextStyle(fontSize: 13, color: Colors.white),
                      ),
                    ),
                  ),
                ),
              ],
            ),
            const Text(
              'AnimatedSwitcher + AnimatedCrossFade',
              style: TextStyle(fontSize: 20, color: Colors.black),
            ),
            const Text(
              'Rapid keyed replacements keep outgoing switcher children while cross-fade also animates height.',
              style: TextStyle(fontSize: 14, color: Colors.black54),
            ),
            Row(
              spacing: 8,
              children: <Widget>[
                _buildButton(
                  label: 'Switcher: $_switcherValue',
                  onTap: _advanceSwitcher,
                  width: 128,
                  background: const Color(0xFFE4EAF4),
                ),
                _buildButton(
                  label: _showSecondCrossFade
                      ? 'Cross-fade: second'
                      : 'Cross-fade: first',
                  onTap: _toggleCrossFade,
                  width: 152,
                  background: const Color(0xFFF3E4D3),
                ),
              ],
            ),
            Container(
              width: 240,
              height: 90,
              color: const Color(0xFFF3F5F8),
              child: Center(
                child: AnimatedSwitcher(
                  duration: const Duration(milliseconds: 350),
                  reverseDuration: const Duration(milliseconds: 220),
                  switchInCurve: Curves.easeInOut,
                  switchOutCurve: Curves.easeInOut,
                  child: Container(
                    key: ValueKey<int>(_switcherValue),
                    width: 96,
                    height: 48,
                    color: _switcherValue.isEven
                        ? const Color(0xFF315A7D)
                        : const Color(0xFF9C4F63),
                    child: Center(
                      child: Text(
                        'child $_switcherValue',
                        style: const TextStyle(
                          fontSize: 13,
                          color: Colors.white,
                        ),
                      ),
                    ),
                  ),
                ),
              ),
            ),
            AnimatedCrossFade(
              firstChild: Container(
                width: 240,
                height: 54,
                color: const Color(0xFFDCEBF2),
                child: const Center(
                  child: Text(
                    'first / 54',
                    style: TextStyle(fontSize: 13, color: Colors.black),
                  ),
                ),
              ),
              secondChild: Container(
                width: 240,
                height: 92,
                color: const Color(0xFFF2D9DF),
                child: const Center(
                  child: Text(
                    'second / 92',
                    style: TextStyle(fontSize: 13, color: Colors.black),
                  ),
                ),
              ),
              crossFadeState: _showSecondCrossFade
                  ? CrossFadeState.showSecond
                  : CrossFadeState.showFirst,
              duration: const Duration(milliseconds: 350),
              reverseDuration: const Duration(milliseconds: 260),
              firstCurve: Curves.easeInOut,
              secondCurve: Curves.easeInOut,
              sizeCurve: Curves.easeInOut,
              onEnd: _handleAnimationEnd,
            ),
            const Text(
              'AnimatedFractionallySizedBox + SliverAnimatedOpacity',
              style: TextStyle(fontSize: 20, color: Colors.black),
            ),
            const Text(
              'Animate fractional layout and a sliver paint layer while preserving their child geometry.',
              style: TextStyle(fontSize: 14, color: Colors.black54),
            ),
            Row(
              spacing: 8,
              children: <Widget>[
                _buildButton(
                  label: _expandedFraction ? 'Fraction: 0.8' : 'Fraction: 0.4',
                  onTap: _toggleFraction,
                  width: 128,
                  background: const Color(0xFFDDEAF2),
                ),
                _buildButton(
                  label: _visibleSliver ? 'Sliver: visible' : 'Sliver: faded',
                  onTap: _toggleSliverOpacity,
                  width: 132,
                  background: const Color(0xFFF0E1EA),
                ),
              ],
            ),
            Container(
              width: 240,
              height: 120,
              color: const Color(0xFFF3F5F8),
              child: AnimatedFractionallySizedBox(
                duration: const Duration(milliseconds: 350),
                alignment: _expandedFraction
                    ? Alignment.bottomRight
                    : Alignment.topLeft,
                widthFactor: _expandedFraction ? 0.8 : 0.4,
                heightFactor: _expandedFraction ? 0.75 : 0.4,
                curve: Curves.easeInOut,
                onEnd: _handleAnimationEnd,
                child: Container(
                  color: const Color(0xFF3F7D6B),
                  child: const Center(
                    child: Text(
                      'fraction',
                      style: TextStyle(fontSize: 13, color: Colors.white),
                    ),
                  ),
                ),
              ),
            ),
            Container(
              width: 240,
              height: 100,
              color: const Color(0xFFF3F5F8),
              child: CustomScrollView(
                slivers: <Widget>[
                  SliverAnimatedOpacity(
                    opacity: _visibleSliver ? 1 : 0.15,
                    duration: const Duration(milliseconds: 350),
                    curve: Curves.easeInOut,
                    onEnd: _handleAnimationEnd,
                    sliver: const SliverToBoxAdapter(
                      child: ColoredBox(
                        color: Color(0xFF8E5572),
                        child: SizedBox(
                          height: 84,
                          child: Center(
                            child: Text(
                              'sliver opacity',
                              style: TextStyle(
                                fontSize: 13,
                                color: Colors.white,
                              ),
                            ),
                          ),
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ],
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

  void _toggleScale() {
    setState(() {
      _scaled = !_scaled;
    });
  }

  void _toggleRotation() {
    setState(() {
      _rotated = !_rotated;
    });
  }

  void _togglePosition() {
    setState(() {
      _positioned = !_positioned;
    });
  }

  void _toggleDirection() {
    setState(() {
      _rightToLeft = !_rightToLeft;
    });
  }

  void _toggleTextStyle() {
    setState(() {
      _emphasizedText = !_emphasizedText;
    });
  }

  void _togglePhysicalModel() {
    setState(() {
      _raisedSurface = !_raisedSurface;
    });
  }

  void _advanceSwitcher() {
    setState(() {
      _switcherValue += 1;
    });
  }

  void _toggleCrossFade() {
    setState(() {
      _showSecondCrossFade = !_showSecondCrossFade;
    });
  }

  void _toggleFraction() {
    setState(() {
      _expandedFraction = !_expandedFraction;
    });
  }

  void _toggleSliverOpacity() {
    setState(() {
      _visibleSliver = !_visibleSliver;
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
