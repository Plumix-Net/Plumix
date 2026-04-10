import 'package:flutter/material.dart';

import '../../counter_widgets.dart';

class ListViewReverseDemoPage extends StatefulWidget {
  const ListViewReverseDemoPage({super.key});

  @override
  State<ListViewReverseDemoPage> createState() =>
      _ListViewReverseDemoPageState();
}

class _ListViewReverseDemoPageState extends State<ListViewReverseDemoPage> {
  final List<int> _messages = List<int>.generate(20, (int index) => index + 1);
  int _nextMessageId = 21;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'ListView reverse=true',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Direction is inverted; drag/pointer behavior follows Flutter axisDirection mapping.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            Expanded(
              child: CounterTapButton(
                label: 'Push message',
                onTap: () => setState(() => _messages.add(_nextMessageId++)),
                background: Colors.blue,
                foreground: Colors.white,
                fontSize: 13,
              ),
            ),
            Expanded(
              child: CounterTapButton(
                label: 'Pop message',
                onTap: _messages.isEmpty
                    ? null
                    : () => setState(() => _messages.removeLast()),
                background: Colors.red,
                foreground: Colors.white,
                fontSize: 13,
              ),
            ),
          ],
        ),
        Expanded(
          child: ListView.builder(
            itemCount: _messages.length,
            reverse: true,
            itemExtent: 44,
            padding: const EdgeInsets.all(12),
            itemBuilder: (BuildContext context, int index) {
              final int id = _messages[index];
              return Container(
                color: id.isEven ? const Color(0xFFFFF3E0) : Colors.white,
                padding: const EdgeInsets.symmetric(
                  horizontal: 12,
                  vertical: 8,
                ),
                child: Text(
                  'message #$id',
                  style: const TextStyle(fontSize: 13, color: Colors.black),
                ),
              );
            },
          ),
        ),
      ],
    );
  }
}
