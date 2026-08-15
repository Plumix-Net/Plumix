import 'package:material_ui/material_ui.dart';

class NestedScrollViewDemoPage extends StatelessWidget {
  const NestedScrollViewDemoPage({super.key});

  @override
  Widget build(BuildContext context) {
    return NestedScrollView(
      headerSliverBuilder: (BuildContext headerContext, bool innerBoxIsScrolled) {
        return <Widget>[
          SliverOverlapAbsorber(
            handle: NestedScrollView.sliverOverlapAbsorberHandleFor(headerContext),
            sliver: SliverPersistentHeader(
              pinned: true,
              delegate: _NestedScrollViewHeaderDelegate(innerBoxIsScrolled),
            ),
          ),
          const SliverToBoxAdapter(
            child: ColoredBox(
              color: Color(0xFFE8EAF6),
              child: Padding(
                padding: EdgeInsets.symmetric(horizontal: 16, vertical: 14),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  spacing: 4,
                  children: <Widget>[
                    Text(
                      'Outer header sliver',
                      style: TextStyle(fontSize: 18, color: Colors.black),
                    ),
                    Text(
                      'This scrolls away completely before the body starts scrolling.',
                      style: TextStyle(fontSize: 13, color: Colors.black54),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ];
      },
      body: Builder(
        builder: (BuildContext bodyContext) {
          return CustomScrollView(
            slivers: <Widget>[
              SliverOverlapInjector(
                handle: NestedScrollView.sliverOverlapAbsorberHandleFor(bodyContext),
              ),
              SliverFixedExtentList.builder(
                itemCount: 40,
                itemExtent: 46,
                itemBuilder: (BuildContext context, int index) {
                  return Container(
                    color: index.isEven
                        ? const Color(0xFFF5F5F5)
                        : const Color(0xFFFFFFFF),
                    padding: const EdgeInsets.symmetric(
                      horizontal: 16,
                      vertical: 12,
                    ),
                    child: Text(
                      'body row #$index',
                      style: const TextStyle(fontSize: 14, color: Colors.black),
                    ),
                  );
                },
              ),
            ],
          );
        },
      ),
    );
  }
}

class _NestedScrollViewHeaderDelegate extends SliverPersistentHeaderDelegate {
  _NestedScrollViewHeaderDelegate(this.innerBoxIsScrolled);

  final bool innerBoxIsScrolled;

  @override
  double get minExtent => 72;

  @override
  double get maxExtent => 72;

  @override
  Widget build(BuildContext context, double shrinkOffset, bool overlapsContent) {
    return SizedBox(
      height: 72,
      child: ColoredBox(
        color: innerBoxIsScrolled
            ? const Color(0xFF90CAF9)
            : const Color(0xFFBBDEFB),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            spacing: 2,
            children: <Widget>[
              const Text(
                'NestedScrollView',
                style: TextStyle(fontSize: 20, color: Colors.black),
              ),
              Text(
                innerBoxIsScrolled
                    ? 'innerBoxIsScrolled: true'
                    : 'innerBoxIsScrolled: false',
                style: const TextStyle(fontSize: 13, color: Colors.black54),
              ),
            ],
          ),
        ),
      ),
    );
  }

  @override
  bool shouldRebuild(_NestedScrollViewHeaderDelegate oldDelegate) {
    return oldDelegate.innerBoxIsScrolled != innerBoxIsScrolled;
  }
}
