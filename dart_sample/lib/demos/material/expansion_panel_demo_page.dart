import 'package:flutter/material.dart';

class ExpansionPanelDemoPage extends StatefulWidget {
  const ExpansionPanelDemoPage({super.key});

  @override
  State<ExpansionPanelDemoPage> createState() => _ExpansionPanelDemoPageState();
}

class _ExpansionPanelDemoPageState extends State<ExpansionPanelDemoPage> {
  bool _detailsExpanded = true;
  bool _historyExpanded = false;
  String _lastRadioEvent = 'none';

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'ExpansionPanel + ExpansionPanelList',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Controlled panels, header tap gating, animated material gaps, and mutually exclusive radio panels.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        const Text(
          'Controlled list',
          style: TextStyle(fontSize: 14, color: Color(0xFF6750A4)),
        ),
        _buildControlledList(),
        Text(
          'Radio list · last callback: $_lastRadioEvent',
          style: const TextStyle(fontSize: 14, color: Color(0xFF006C4C)),
        ),
        _buildRadioList(),
      ],
    );
  }

  Widget _buildControlledList() {
    return ExpansionPanelList(
      materialGapSize: 12,
      dividerColor: const Color(0xFFE6E0E9),
      expandIconColor: const Color(0xFF6750A4),
      expansionCallback: (int index, bool expanded) {
        setState(() {
          if (index == 0) {
            _detailsExpanded = expanded;
          } else {
            _historyExpanded = expanded;
          }
        });
      },
      children: <ExpansionPanel>[
        ExpansionPanel(
          isExpanded: _detailsExpanded,
          headerBuilder: (_, bool expanded) =>
              _buildHeader('Account details', expanded),
          body: _buildBody('Name and contact preferences are synchronized.'),
          backgroundColor: const Color(0xFFFFFBFE),
        ),
        ExpansionPanel(
          isExpanded: _historyExpanded,
          canTapOnHeader: true,
          headerBuilder: (_, bool expanded) =>
              _buildHeader('Recent activity', expanded),
          body: _buildBody('Three successful synchronizations this week.'),
          backgroundColor: const Color(0xFFFFFBFE),
        ),
      ],
    );
  }

  Widget _buildRadioList() {
    return ExpansionPanelList.radio(
      initialOpenPanelValue: 'balanced',
      materialGapSize: 12,
      expansionCallback: (int index, bool expanded) =>
          setState(() => _lastRadioEvent = '$index:$expanded'),
      children: <ExpansionPanelRadio>[
        ExpansionPanelRadio(
          value: 'balanced',
          canTapOnHeader: true,
          headerBuilder: (_, bool expanded) =>
              _buildHeader('Balanced sync', expanded),
          body: _buildBody('Sync every hour while preserving battery.'),
        ),
        ExpansionPanelRadio(
          value: 'instant',
          canTapOnHeader: true,
          headerBuilder: (_, bool expanded) =>
              _buildHeader('Instant sync', expanded),
          body: _buildBody('Sync immediately after every local change.'),
        ),
      ],
    );
  }

  Widget _buildHeader(String label, bool expanded) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16),
      child: Text(
        '$label · ${expanded ? 'open' : 'closed'}',
        style: const TextStyle(fontSize: 14, color: Colors.black),
      ),
    );
  }

  Widget _buildBody(String label) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
      child: Text(
        label,
        style: const TextStyle(fontSize: 13, color: Color(0xFF49454F)),
      ),
    );
  }
}
