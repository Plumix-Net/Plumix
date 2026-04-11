import 'package:flutter/material.dart';

class ListViewFixedExtentDemoPage extends StatelessWidget {
  const ListViewFixedExtentDemoPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'ListView with itemExtent',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Each row has fixed paint/layout extent and list has sliver padding.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Expanded(
          child: ListView.builder(
            itemCount: 50,
            itemExtent: 52,
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 20),
            itemBuilder: (BuildContext context, int index) {
              return Container(
                color: index.isEven
                    ? const Color(0xFFE8F5E9)
                    : const Color(0xFFE3F2FD),
                padding: const EdgeInsets.symmetric(
                  horizontal: 12,
                  vertical: 8,
                ),
                child: Text(
                  'fixed row #$index (itemExtent=52)',
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
