import 'package:flutter/material.dart';

class CustomSliversDemoPage extends StatelessWidget {
  const CustomSliversDemoPage({super.key});

  @override
  Widget build(BuildContext context) {
    return CustomScrollView(
      slivers: <Widget>[
        const PinnedHeaderSliver(
          child: SizedBox(
            height: 88,
            child: ColoredBox(
              color: Color(0xFFF8FAFF),
              child: Padding(
                padding: EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  spacing: 4,
                  children: <Widget>[
                    Text(
                      'PinnedHeaderSliver',
                      style: TextStyle(fontSize: 20, color: Colors.black),
                    ),
                    Text(
                      'This measured header remains pinned while the decorated '
                      'list scrolls behind it.',
                      style: TextStyle(fontSize: 14, color: Colors.black54),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
        DecoratedSliver(
          decoration: BoxDecoration(
            color: const Color(0xFFEAF4FF),
            border: Border.all(color: const Color(0xFF90CAF9), width: 2),
            borderRadius: BorderRadius.circular(18),
          ),
          sliver: SliverPadding(
            padding: const EdgeInsets.fromLTRB(12, 10, 12, 8),
            sliver: SliverFixedExtentList.builder(
              itemExtent: 42,
              itemCount: 18,
              itemBuilder: (BuildContext context, int index) {
                return Container(
                  color: index.isEven
                      ? const Color(0xCCFFFFFF)
                      : const Color(0xCCE8F5E9),
                  padding: const EdgeInsets.symmetric(
                    horizontal: 10,
                    vertical: 8,
                  ),
                  child: Text(
                    'decorated sliver row #$index',
                    style: const TextStyle(fontSize: 13, color: Colors.black),
                  ),
                );
              },
            ),
          ),
        ),
        SliverPadding(
          padding: const EdgeInsets.fromLTRB(12, 8, 12, 16),
          sliver: SliverList.builder(
            itemCount: 8,
            itemBuilder: (BuildContext context, int index) {
              return Container(
                color: const Color(0xFFF5F5F5),
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 10,
                ),
                child: Text(
                  'regular sliver row #$index',
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
