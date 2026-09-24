import 'package:flutter/gestures.dart';
import 'package:material_ui/material_ui.dart';

class RichTextDemoPage extends StatefulWidget {
  const RichTextDemoPage({super.key});

  @override
  State<RichTextDemoPage> createState() => _RichTextDemoPageState();
}

class _RichTextDemoPageState extends State<RichTextDemoPage> {
  late final TapGestureRecognizer _tapRecognizer;
  int _taps = 0;

  @override
  void initState() {
    super.initState();
    _tapRecognizer = TapGestureRecognizer()
      ..onTap = () => setState(() => _taps += 1);
  }

  @override
  void dispose() {
    _tapRecognizer.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 12,
      children: <Widget>[
        const Text(
          'RichText + TextSpan + WidgetSpan',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'One paragraph, many styles. Spans share a single line layout, carry their own gesture '
          'recognizers, and can embed inline widgets.',
          style: TextStyle(fontSize: 14, color: Colors.grey),
        ),
        _buildStyledParagraph(),
        Text(
          'Tapped the link span $_taps times',
          style: const TextStyle(fontSize: 14, color: Colors.grey),
        ),
        _buildInlineWidgetParagraph(),
        _buildAlignmentRow(),
        _buildDefaultTextStyleParagraph(),
        _buildOverflowAndStrut(),
      ],
    );
  }

  Widget _buildStyledParagraph() {
    return Container(
      color: const Color(0xFFF1F5F9),
      padding: const EdgeInsets.all(12),
      child: RichText(
        text: TextSpan(
          text: 'Can you ',
          style: const TextStyle(fontSize: 18, color: Color(0xFF1D3557)),
          children: <InlineSpan>[
            TextSpan(
              text: 'find the',
              style: const TextStyle(
                color: Color(0xFF2A9D8F),
                fontWeight: FontWeight.bold,
                decoration: TextDecoration.underline,
              ),
              recognizer: _tapRecognizer,
            ),
            const TextSpan(text: ' secret?'),
          ],
        ),
      ),
    );
  }

  static Widget _buildInlineWidgetParagraph() {
    return Container(
      color: const Color(0xFFE7EDF6),
      padding: const EdgeInsets.all(12),
      child: const Text.rich(
        TextSpan(
          text: 'Inline ',
          children: <InlineSpan>[
            WidgetSpan(
              child: SizedBox(
                width: 40,
                height: 20,
                child: ColoredBox(color: Color(0xFFE9C46A)),
              ),
            ),
            TextSpan(text: ' widgets flow with the text.'),
          ],
        ),
        style: TextStyle(fontSize: 16, color: Color(0xFF1D3557)),
      ),
    );
  }

  static Widget _buildAlignmentRow() {
    return Container(
      color: const Color(0xFFF8EDEB),
      padding: const EdgeInsets.all(12),
      child: Text.rich(
        TextSpan(
          text: 'top ',
          children: <InlineSpan>[
            _buildBadge(PlaceholderAlignment.top, const Color(0xFFE63946)),
            const TextSpan(text: ' middle '),
            _buildBadge(PlaceholderAlignment.middle, const Color(0xFF2A9D8F)),
            const TextSpan(text: ' bottom '),
            _buildBadge(PlaceholderAlignment.bottom, const Color(0xFF457B9D)),
          ],
        ),
        style: const TextStyle(fontSize: 24, color: Color(0xFF1D3557)),
      ),
    );
  }

  static Widget _buildOverflowAndStrut() {
    const longText =
        'Overflowing text can fade out, end with an ellipsis, or be clipped at the box edge.';
    const textStyle = TextStyle(fontSize: 16, color: Color(0xFF1D3557));
    return Container(
      color: const Color(0xFFEAF4EA),
      padding: const EdgeInsets.all(12),
      child: const Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 8,
        children: <Widget>[
          Text(
            longText,
            maxLines: 1,
            softWrap: false,
            overflow: TextOverflow.fade,
            style: textStyle,
          ),
          Text(
            longText,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: textStyle,
          ),
          Text(
            'A forced strut keeps\nboth lines 32 px apart.',
            style: textStyle,
            strutStyle: StrutStyle(
              fontSize: 16,
              height: 2,
              forceStrutHeight: true,
            ),
          ),
        ],
      ),
    );
  }

  static Widget _buildDefaultTextStyleParagraph() {
    return Container(
      color: const Color(0xFFF5F0FF),
      padding: const EdgeInsets.all(12),
      child: DefaultTextStyle(
        style: const TextStyle(fontSize: 16, color: Color(0xFF1D3557)),
        overflow: TextOverflow.ellipsis,
        maxLines: 1,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          spacing: 4,
          children: <Widget>[
            const Text(
              'DefaultTextStyle supplies the size, color, and one-line ellipsis.',
            ),
            DefaultTextStyle.merge(
              style: const TextStyle(fontWeight: FontWeight.bold),
              child: const Text(
                'A merged style keeps the inherited color and line limit.',
              ),
            ),
          ],
        ),
      ),
    );
  }

  static InlineSpan _buildBadge(PlaceholderAlignment alignment, Color color) {
    return WidgetSpan(
      alignment: alignment,
      child: SizedBox(width: 18, height: 18, child: ColoredBox(color: color)),
    );
  }
}
