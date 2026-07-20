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
                      'Decorated and grouped slivers share Flutter\'s layout '
                      'protocol.',
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
              itemCount: 8,
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
        SliverMainAxisGroup(
          slivers: <Widget>[
            const PinnedHeaderSliver(
              child: SizedBox(
                height: 56,
                child: ColoredBox(
                  color: Color(0xFFFFF3E0),
                  child: Padding(
                    padding: EdgeInsets.symmetric(horizontal: 12, vertical: 9),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      spacing: 2,
                      children: <Widget>[
                        Text(
                          'SliverMainAxisGroup',
                          style: TextStyle(fontSize: 17, color: Colors.black),
                        ),
                        Text(
                          'This header stops pinning at the end of its group.',
                          style: TextStyle(fontSize: 12, color: Colors.black54),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ),
            SliverCrossAxisGroup(
              slivers: <Widget>[
                SliverFixedExtentList.builder(
                  itemExtent: 38,
                  itemCount: 8,
                  itemBuilder: (BuildContext context, int index) {
                    return _groupCell('1x #$index', const Color(0xFFE3F2FD));
                  },
                ),
                SliverConstrainedCrossAxis(
                  maxExtent: 96,
                  sliver: SliverFixedExtentList.builder(
                    itemExtent: 46,
                    itemCount: 6,
                    itemBuilder: (BuildContext context, int index) {
                      return _groupCell('96 #$index', const Color(0xFFFFF9C4));
                    },
                  ),
                ),
                SliverCrossAxisExpanded(
                  flex: 2,
                  sliver: SliverFixedExtentList.builder(
                    itemExtent: 34,
                    itemCount: 10,
                    itemBuilder: (BuildContext context, int index) {
                      return _groupCell('2x #$index', const Color(0xFFE8F5E9));
                    },
                  ),
                ),
              ],
            ),
          ],
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

  static Widget _groupCell(String label, Color color) {
    return ColoredBox(
      color: color,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 8),
        child: Text(
          label,
          style: const TextStyle(fontSize: 12, color: Colors.black),
        ),
      ),
    );
  }
}
