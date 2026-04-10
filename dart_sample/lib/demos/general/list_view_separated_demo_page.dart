import 'package:flutter/material.dart';

import '../../counter_widgets.dart';

class ListViewSeparatedDemoPage extends StatelessWidget {
  const ListViewSeparatedDemoPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'ListView.Separated',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Uses separate builders for rows and separators.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Expanded(
          child: ListView.separated(
            itemCount: 30,
            padding: const EdgeInsets.all(12),
            itemBuilder: (BuildContext context, int index) {
              return SeparatedListItem(index: index);
            },
            separatorBuilder: (BuildContext context, int index) {
              return Container(
                height: 4,
                color: index.isEven
                    ? const Color(0xFFDCE3ED)
                    : const Color(0xFFE9EEF5),
              );
            },
          ),
        ),
      ],
    );
  }
}

class SeparatedListItem extends StatefulWidget {
  const SeparatedListItem({required this.index, super.key});

  final int index;

  @override
  State<SeparatedListItem> createState() => _SeparatedListItemState();
}

class _SeparatedListItemState extends State<SeparatedListItem> {
  int _taps = 0;

  @override
  Widget build(BuildContext context) {
    return CounterTapButton(
      label: 'item #${widget.index} taps=$_taps',
      onTap: () => setState(() => _taps += 1),
      background: widget.index.isEven ? const Color(0xFFEFF5FF) : Colors.white,
      foreground: Colors.black,
      fontSize: 13,
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
    );
  }
}
