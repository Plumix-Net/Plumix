import 'package:material_ui/material_ui.dart';

import 'counter_scope.dart';
import 'counter_widgets.dart';

class CounterScreen extends StatelessWidget {
  const CounterScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final model = CounterScope.of(context);
    final globalBadgeKey = GlobalObjectKey<MovableBadgeState>(
      model.globalBadgeIdentity,
    );

    final scrollList = ListView.builder(
      itemCount: 30,
      itemExtent: 40,
      itemBuilder: (BuildContext context, int index) {
        return KeepAliveListItem(
          index: index,
          keepAlive: index.isEven,
          onTap: model.increment,
        );
      },
    );

    return Container(
      color: Colors.white,
      padding: const EdgeInsets.all(20),
      child: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          spacing: 12,
          children: <Widget>[
            const Text(
              'Flutter Counter',
              style: TextStyle(fontSize: 24, color: Colors.black),
            ),
            Text(
              'Count: ${model.count}',
              style: const TextStyle(fontSize: 18, color: Colors.indigo),
            ),
            Row(
              spacing: 12,
              children: <Widget>[
                Expanded(
                  child: CounterTapButton(
                    label: '-',
                    onTap: model.decrement,
                    background: Colors.grey,
                    foreground: Colors.black,
                    fontSize: 20,
                  ),
                ),
                Expanded(
                  child: CounterTapButton(
                    label: '+',
                    onTap: model.increment,
                    background: Colors.lightBlue,
                    foreground: Colors.black,
                    fontSize: 20,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            const Text(
              'Keyed List (ValueKey)',
              style: TextStyle(fontSize: 18, color: Colors.black),
            ),
            Row(
              spacing: 8,
              children: <Widget>[
                Expanded(
                  child: CounterTapButton(
                    label: 'Reverse',
                    onTap: model.reverseItems,
                    background: Colors.green,
                    foreground: Colors.white,
                    fontSize: 13,
                  ),
                ),
                Expanded(
                  child: CounterTapButton(
                    label: 'Insert Head',
                    onTap: model.insertHead,
                    background: Colors.blue,
                    foreground: Colors.white,
                    fontSize: 13,
                  ),
                ),
                Expanded(
                  child: CounterTapButton(
                    label: 'Remove Tail',
                    onTap: model.items.isEmpty ? null : model.removeTail,
                    background: Colors.red,
                    foreground: Colors.white,
                    fontSize: 13,
                  ),
                ),
              ],
            ),
            Column(
              spacing: 8,
              children: model.items
                  .map(
                    (int id) => KeyedListItem(id: id, key: ValueKey<int>(id)),
                  )
                  .toList(growable: false),
            ),
            const SizedBox(height: 12),
            const Text(
              'GlobalKey Reparent',
              style: TextStyle(fontSize: 18, color: Colors.black),
            ),
            Row(
              spacing: 8,
              children: <Widget>[
                Expanded(
                  child: Container(
                    color: const Color(0xFFE8F5E9),
                    padding: const EdgeInsets.all(8),
                    child: model.placeGlobalOnLeft
                        ? MovableBadge(key: globalBadgeKey)
                        : const Text(
                            'left slot',
                            style: TextStyle(color: Colors.grey),
                          ),
                  ),
                ),
                Expanded(
                  child: Container(
                    color: const Color(0xFFE3F2FD),
                    padding: const EdgeInsets.all(8),
                    child: !model.placeGlobalOnLeft
                        ? MovableBadge(key: globalBadgeKey)
                        : const Text(
                            'right slot',
                            style: TextStyle(color: Colors.grey),
                          ),
                  ),
                ),
              ],
            ),
            CounterTapButton(
              label: 'Move Global Widget',
              onTap: model.toggleGlobalPlacement,
              background: Colors.blueGrey,
              foreground: Colors.white,
              fontSize: 14,
            ),
            const SizedBox(height: 12),
            const Text(
              'Scrollable ListView.builder (even rows keepAlive)',
              style: TextStyle(fontSize: 18, color: Colors.black),
            ),
            SizedBox(
              height: 220,
              child: ColoredBox(
                color: const Color(0xFFF2F4F8),
                child: scrollList,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
