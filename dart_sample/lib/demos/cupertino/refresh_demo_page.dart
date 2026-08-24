import 'package:cupertino_ui/cupertino_ui.dart';
import 'package:material_ui/material_ui.dart';

class CupertinoRefreshDemoPage extends StatefulWidget {
  const CupertinoRefreshDemoPage({super.key});

  @override
  State<CupertinoRefreshDemoPage> createState() =>
      _CupertinoRefreshDemoPageState();
}

class _CupertinoRefreshDemoPageState extends State<CupertinoRefreshDemoPage> {
  int _refreshCount = 0;
  String _status = 'Pull down from the top';

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'Cupertino sliver refresh',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Pull the list down past the indicator, then release. '
          'The sliver holds 60 px while refreshing.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Text(
          '$_status · refreshCount=$_refreshCount',
          style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
        ),
        Expanded(
          child: CustomScrollView(
            physics: const BouncingScrollPhysics(
              parent: AlwaysScrollableScrollPhysics(),
            ),
            slivers: <Widget>[
              CupertinoSliverRefreshControl(onRefresh: _handleRefresh),
              SliverFixedExtentList.builder(
                itemCount: 24,
                itemExtent: 54,
                itemBuilder: (BuildContext context, int index) => Container(
                  color: index.isEven ? Colors.white : const Color(0xFFF5F7FA),
                  padding: const EdgeInsets.symmetric(
                    horizontal: 12,
                    vertical: 10,
                  ),
                  child: Text(
                    'Cupertino refresh row #${index + 1}',
                    style: const TextStyle(fontSize: 13, color: Colors.black),
                  ),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Future<void> _handleRefresh() async {
    if (mounted) setState(() => _status = 'Refreshing');
    await Future<void>.delayed(const Duration(milliseconds: 650));
    if (mounted) {
      setState(() {
        _refreshCount += 1;
        _status = 'Refresh complete';
      });
    }
  }
}
