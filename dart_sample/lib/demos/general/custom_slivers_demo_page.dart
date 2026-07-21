import 'package:flutter/material.dart';
import 'package:flutter/rendering.dart';

class CustomSliversDemoPage extends StatelessWidget {
  const CustomSliversDemoPage({super.key});

  @override
  Widget build(BuildContext context) {
    return CustomScrollView(
      slivers: <Widget>[
        SliverMainAxisGroup(
          slivers: <Widget>[
            SliverResizingHeader(
              minExtentPrototype: const SizedBox(height: 64),
              maxExtentPrototype: const SizedBox(height: 140),
              child: ColoredBox(
                color: const Color(0xFFE8EAF6),
                child: Padding(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 16,
                    vertical: 12,
                  ),
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    spacing: 4,
                    children: const <Widget>[
                      Text(
                        'SliverResizingHeader',
                        style: TextStyle(fontSize: 20, color: Colors.black),
                      ),
                      Text(
                        '140px → 64px prototype extents',
                        style: TextStyle(fontSize: 13, color: Colors.black54),
                      ),
                    ],
                  ),
                ),
              ),
            ),
            SliverFixedExtentList.builder(
              itemCount: 5,
              itemExtent: 44,
              itemBuilder: (BuildContext context, int index) {
                return _extentCell(
                  'resizing-header group row #$index',
                  index.isEven
                      ? const Color(0xFFF5F5F5)
                      : const Color(0xFFFFFFFF),
                );
              },
            ),
          ],
        ),
        const SliverFloatingHeader(
          snapMode: FloatingHeaderSnapMode.overlay,
          child: SizedBox(
            height: 64,
            child: ColoredBox(
              color: Color(0xFFFFECB3),
              child: Padding(
                padding: EdgeInsets.symmetric(horizontal: 16, vertical: 10),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  spacing: 2,
                  children: <Widget>[
                    Text(
                      'SliverFloatingHeader',
                      style: TextStyle(fontSize: 18, color: Colors.black),
                    ),
                    Text(
                      'Reverse the scroll direction to reveal and snap it.',
                      style: TextStyle(fontSize: 12, color: Colors.black54),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
        SliverSafeArea(
          minimum: const EdgeInsets.fromLTRB(12, 8, 12, 0),
          sliver: SliverLayoutBuilder(
            builder: (BuildContext context, SliverConstraints constraints) {
              final bool compact = constraints.crossAxisExtent < 420;
              final double height = compact ? 104 : 88;
              final String width = constraints.crossAxisExtent.toStringAsFixed(
                0,
              );
              return PinnedHeaderSliver(
                child: SizedBox(
                  height: height,
                  child: ColoredBox(
                    color: const Color(0xFFF8FAFF),
                    child: Padding(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 12,
                        vertical: 10,
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        spacing: 4,
                        children: <Widget>[
                          const Text(
                            'SliverLayoutBuilder + SliverSafeArea',
                            style: TextStyle(fontSize: 20, color: Colors.black),
                          ),
                          Text(
                            '$width px safe cross-axis — '
                            '${compact ? "compact" : "wide"} header',
                            style: const TextStyle(
                              fontSize: 14,
                              color: Colors.black54,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
              );
            },
          ),
        ),
        SliverFillRemaining(
          hasScrollBody: false,
          child: ColoredBox(
            color: const Color(0xFFF3E5F5),
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                spacing: 8,
                children: const <Widget>[
                  Text(
                    'SliverFillRemaining',
                    style: TextStyle(fontSize: 22, color: Colors.black),
                  ),
                  Text(
                    'Non-scrollable child fills the first viewport below the '
                    'pinned header.',
                    textAlign: TextAlign.center,
                    style: TextStyle(fontSize: 13, color: Colors.black54),
                  ),
                ],
              ),
            ),
          ),
        ),
        SliverFillViewport(
          viewportFraction: 0.55,
          padEnds: true,
          allowImplicitScrolling: false,
          delegate: SliverChildListDelegate(<Widget>[
            _viewportPage('viewport page 1', const Color(0xFFE3F2FD)),
            _viewportPage('viewport page 2', const Color(0xFFE8F5E9)),
            _viewportPage('viewport page 3', const Color(0xFFFFF3E0)),
          ]),
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
          padding: const EdgeInsets.fromLTRB(12, 10, 12, 4),
          sliver: SliverPrototypeExtentList.builder(
            itemCount: 4,
            prototypeItem: const SizedBox(height: 54),
            itemBuilder: (BuildContext context, int index) {
              return _extentCell(
                'prototype extent row #$index',
                index.isEven
                    ? const Color(0xFFE8EAF6)
                    : const Color(0xFFF3E5F5),
              );
            },
          ),
        ),
        SliverPadding(
          padding: const EdgeInsets.fromLTRB(12, 4, 12, 8),
          sliver: SliverVariedExtentList.builder(
            itemCount: 6,
            itemExtentBuilder: (int index, SliverLayoutDimensions dimensions) =>
                switch (index % 3) {
                  0 => 38,
                  1 => 54,
                  _ => 46,
                },
            itemBuilder: (BuildContext context, int index) {
              return _extentCell(
                'varied extent row #$index',
                index.isEven
                    ? const Color(0xFFE0F2F1)
                    : const Color(0xFFE8F5E9),
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

  static Widget _viewportPage(String label, Color color) {
    return ColoredBox(
      color: color,
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          spacing: 6,
          children: <Widget>[
            Text(
              label,
              style: const TextStyle(fontSize: 20, color: Colors.black),
            ),
            const Text(
              '55% of the viewport · padded ends',
              style: TextStyle(fontSize: 13, color: Colors.black54),
            ),
          ],
        ),
      ),
    );
  }

  static Widget _extentCell(String label, Color color) {
    return ColoredBox(
      color: color,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
        child: Align(
          alignment: Alignment.centerLeft,
          child: Text(
            label,
            style: const TextStyle(fontSize: 13, color: Colors.black),
          ),
        ),
      ),
    );
  }
}
