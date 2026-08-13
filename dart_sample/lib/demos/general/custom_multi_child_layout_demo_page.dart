import 'package:material_ui/material_ui.dart';

import '../../counter_widgets.dart';

class CustomMultiChildLayoutDemoPage extends StatefulWidget {
  const CustomMultiChildLayoutDemoPage({super.key});

  @override
  State<CustomMultiChildLayoutDemoPage> createState() =>
      _CustomMultiChildLayoutDemoPageState();
}

class _CustomMultiChildLayoutDemoPageState
    extends State<CustomMultiChildLayoutDemoPage> {
  bool _centerMiddle = true;
  bool _rightToLeft = false;

  @override
  Widget build(BuildContext context) {
    final TextDirection textDirection = _rightToLeft
        ? TextDirection.rtl
        : TextDirection.ltr;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'CustomMultiChildLayout + NavigationToolbar',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'LayoutId slots drive dependent child constraints; '
          'NavigationToolbar applies the same delegate pipeline to leading, '
          'middle, and trailing content.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            _buildButton(
              label: _centerMiddle ? 'Middle: centered' : 'Middle: start',
              onTap: () {
                setState(() {
                  _centerMiddle = !_centerMiddle;
                });
              },
            ),
            _buildButton(
              label: _rightToLeft ? 'Direction: RTL' : 'Direction: LTR',
              onTap: () {
                setState(() {
                  _rightToLeft = !_rightToLeft;
                });
              },
            ),
          ],
        ),
        Directionality(
          textDirection: textDirection,
          child: ColoredBox(
            color: const Color(0xFFE7EDF6),
            child: SizedBox(
              height: 64,
              child: NavigationToolbar(
                leading: _buildSlot('L', 56, 64, const Color(0xFF1565C0)),
                middle: _buildSlot('MIDDLE', 150, 32, const Color(0xFF2E7D32)),
                trailing: _buildSlot('TRAIL', 72, 32, const Color(0xFFF57C00)),
                centerMiddle: _centerMiddle,
              ),
            ),
          ),
        ),
        Expanded(
          child: Container(
            color: const Color(0xFFF3F6FA),
            alignment: Alignment.center,
            child: SizedBox(
              width: 320,
              height: 170,
              child: CustomMultiChildLayout(
                delegate: FollowLeaderDemoDelegate(),
                children: <Widget>[
                  LayoutId(
                    id: DemoLayoutSlot.leader,
                    child: _buildSlot(
                      'LEADER',
                      96,
                      56,
                      const Color(0xFF6A1B9A),
                    ),
                  ),
                  LayoutId(
                    id: DemoLayoutSlot.follower,
                    child: _buildSlot(
                      'FOLLOWER',
                      140,
                      80,
                      const Color(0xFF00838F),
                    ),
                  ),
                  LayoutId(
                    id: DemoLayoutSlot.caption,
                    child: _buildSlot(
                      'same size',
                      100,
                      28,
                      const Color(0xFF455A64),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ],
    );
  }

  static Widget _buildSlot(
    String label,
    double width,
    double height,
    Color color,
  ) {
    return SizedBox(
      width: width,
      height: height,
      child: ColoredBox(
        color: color,
        child: Center(
          child: Text(
            label,
            style: const TextStyle(fontSize: 12, color: Colors.white),
          ),
        ),
      ),
    );
  }

  static Widget _buildButton({
    required String label,
    required VoidCallback onTap,
  }) {
    return SizedBox(
      width: 150,
      child: CounterTapButton(
        label: label,
        onTap: onTap,
        background: const Color(0xFFDCE3ED),
        foreground: Colors.black,
        fontSize: 12,
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      ),
    );
  }
}

enum DemoLayoutSlot { leader, follower, caption }

class FollowLeaderDemoDelegate extends MultiChildLayoutDelegate {
  @override
  void performLayout(Size size) {
    final Size leaderSize = layoutChild(
      DemoLayoutSlot.leader,
      BoxConstraints.loose(size),
    );
    positionChild(DemoLayoutSlot.leader, const Offset(16, 18));

    layoutChild(DemoLayoutSlot.follower, BoxConstraints.tight(leaderSize));
    positionChild(
      DemoLayoutSlot.follower,
      Offset(
        size.width - leaderSize.width - 16,
        size.height - leaderSize.height - 18,
      ),
    );

    final Size captionSize = layoutChild(
      DemoLayoutSlot.caption,
      BoxConstraints.loose(size),
    );
    positionChild(
      DemoLayoutSlot.caption,
      Offset(
        (size.width - captionSize.width) / 2,
        (size.height - captionSize.height) / 2,
      ),
    );
  }

  @override
  bool shouldRelayout(FollowLeaderDemoDelegate oldDelegate) => false;
}
