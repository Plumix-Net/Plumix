import 'package:flutter/material.dart';

class CustomSliversDemoPage extends StatelessWidget {
  const CustomSliversDemoPage({super.key});

  @override
  Widget build(BuildContext context) {
    return CustomScrollView(
      slivers: <Widget>[
        const SliverToBoxAdapter(
          child: ColoredBox(
            color: Colors.white,
            child: Padding(
              padding: EdgeInsets.all(12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                spacing: 6,
                children: <Widget>[
                  Text(
                    'CustomScrollView + Slivers',
                    style: TextStyle(fontSize: 20, color: Colors.black),
                  ),
                  Text(
                    'SliverPadding and SliverFixedExtentList are used directly.',
                    style: TextStyle(fontSize: 14, color: Colors.black54),
                  ),
                ],
              ),
            ),
          ),
        ),
        SliverPadding(
          padding: const EdgeInsets.fromLTRB(12, 10, 12, 8),
          sliver: SliverFixedExtentList.builder(
            itemExtent: 42,
            itemCount: 18,
            itemBuilder: (BuildContext context, int index) {
              return Container(
                color: index.isEven
                    ? const Color(0xFFE8F5E9)
                    : const Color(0xFFEAF4FF),
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 8,
                ),
                child: Text(
                  'fixed sliver row #$index',
                  style: const TextStyle(fontSize: 13, color: Colors.black),
                ),
              );
            },
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
