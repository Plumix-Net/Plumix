import 'package:flutter/material.dart';

class DrawerHeadersDemoPage extends StatefulWidget {
  const DrawerHeadersDemoPage({super.key});

  @override
  State<DrawerHeadersDemoPage> createState() => _DrawerHeadersDemoPageState();
}

class _DrawerHeadersDemoPageState extends State<DrawerHeadersDemoPage> {
  bool _alternateDecoration = false;
  int _detailsPressed = 0;

  @override
  Widget build(BuildContext context) {
    final Color plainColor = _alternateDecoration
        ? Colors.blueGrey
        : Colors.indigo;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'DrawerHeader + UserAccountsDrawerHeader',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Status-bar padding, animated decoration, account pictures, details toggle, and semantics.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            TextButton(
              onPressed: () =>
                  setState(() => _alternateDecoration = !_alternateDecoration),
              child: Text(
                _alternateDecoration ? 'Decoration B' : 'Decoration A',
              ),
            ),
            Text('details=$_detailsPressed'),
          ],
        ),
        Expanded(
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            spacing: 12,
            children: <Widget>[
              Expanded(
                child: DrawerHeader(
                  decoration: BoxDecoration(color: plainColor),
                  child: const Align(
                    alignment: Alignment.bottomLeft,
                    child: Text(
                      'Plain header',
                      style: TextStyle(color: Colors.white),
                    ),
                  ),
                ),
              ),
              Expanded(
                child: UserAccountsDrawerHeader(
                  decoration: BoxDecoration(
                    color: _alternateDecoration
                        ? Colors.teal
                        : Colors.deepPurple,
                  ),
                  accountName: const Text('Ada Lovelace'),
                  accountEmail: const Text('ada@example.test'),
                  currentAccountPicture: const CircleAvatar(child: Text('AL')),
                  otherAccountsPictures: const <Widget>[
                    CircleAvatar(radius: 20, child: Text('GH')),
                    CircleAvatar(radius: 20, child: Text('CS')),
                  ],
                  onDetailsPressed: () => setState(() => _detailsPressed++),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}
