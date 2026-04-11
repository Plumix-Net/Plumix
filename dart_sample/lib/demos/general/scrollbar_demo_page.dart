import 'package:flutter/material.dart';

class ScrollbarDemoPage extends StatefulWidget {
  const ScrollbarDemoPage({super.key});

  @override
  State<ScrollbarDemoPage> createState() => _ScrollbarDemoPageState();
}

class _ScrollbarDemoPageState extends State<ScrollbarDemoPage> {
  late final ScrollController _controller;

  @override
  void initState() {
    super.initState();
    _controller = ScrollController();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'Scrollbar + ScrollController',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'The thumb is computed from viewport/content metrics.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Expanded(
          child: Scrollbar(
            controller: _controller,
            child: ListView.builder(
              controller: _controller,
              itemCount: 70,
              itemExtent: 40,
              padding: const EdgeInsets.all(10),
              itemBuilder: (BuildContext context, int index) {
                return Container(
                  color: index.isEven ? Colors.white : const Color(0xFFF4F7FA),
                  padding: const EdgeInsets.symmetric(
                    horizontal: 10,
                    vertical: 8,
                  ),
                  child: Text(
                    'scroll row #$index',
                    style: const TextStyle(fontSize: 13, color: Colors.black),
                  ),
                );
              },
            ),
          ),
        ),
      ],
    );
  }
}
