import 'package:flutter/material.dart';

class StepperDemoPage extends StatefulWidget {
  const StepperDemoPage({super.key});

  @override
  State<StepperDemoPage> createState() => _StepperDemoPageState();
}

class _StepperDemoPageState extends State<StepperDemoPage> {
  int _currentStep = 0;
  bool _horizontal = false;
  bool _expanded = false;

  @override
  Widget build(BuildContext context) {
    final ColorScheme colors = Theme.of(context).colorScheme;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 10,
      children: <Widget>[
        const Text(
          'ExpandIcon + Stepper',
          style: TextStyle(fontSize: 20, color: Colors.black),
        ),
        const Text(
          'Flutter-shaped disclosure animation plus vertical/horizontal step progress, states, connectors, and controls.',
          style: TextStyle(fontSize: 14, color: Colors.black54),
        ),
        Row(
          mainAxisSize: MainAxisSize.min,
          spacing: 8,
          children: <Widget>[
            ExpandIcon(
              isExpanded: _expanded,
              onPressed: (_) => setState(() => _expanded = !_expanded),
              expandedColor: const Color(0xFF6750A4),
            ),
            Text(_expanded ? 'Expanded' : 'Collapsed'),
            const ExpandIcon(onPressed: null),
            const Text('Disabled'),
            TextButton(
              onPressed: () => setState(() => _horizontal = !_horizontal),
              child: Text(_horizontal ? 'Use vertical' : 'Use horizontal'),
            ),
          ],
        ),
        Text(
          'Current step: ${_currentStep + 1} / 3',
          style: const TextStyle(fontSize: 13, color: Color(0xFF006C4C)),
        ),
        Expanded(
          child: Stepper(
            type: _horizontal ? StepperType.horizontal : StepperType.vertical,
            currentStep: _currentStep,
            onStepTapped: (int index) => setState(() => _currentStep = index),
            onStepContinue: () =>
                setState(() => _currentStep = (_currentStep + 1).clamp(0, 2)),
            onStepCancel: () =>
                setState(() => _currentStep = (_currentStep - 1).clamp(0, 2)),
            connectorColor: WidgetStateProperty.resolveWith<Color>(
              (Set<WidgetState> states) => states.contains(WidgetState.selected)
                  ? colors.primary
                  : colors.outlineVariant,
            ),
            connectorThickness: 2,
            headerPadding: const EdgeInsetsDirectional.symmetric(
              horizontal: 20,
            ),
            contentPadding: const EdgeInsetsDirectional.only(
              start: 56,
              end: 20,
              bottom: 20,
            ),
            steps: _buildSteps(colors),
          ),
        ),
      ],
    );
  }

  List<Step> _buildSteps(ColorScheme colors) => <Step>[
    _buildStep(
      0,
      'Account',
      'Choose an account',
      'Account settings are ready.',
      colors,
    ),
    _buildStep(
      1,
      'Details',
      'Review preferences',
      'Notification and sync preferences.',
      colors,
    ),
    _buildStep(
      2,
      'Confirm',
      'Finish setup',
      'Everything is ready to submit.',
      colors,
    ),
  ];

  Step _buildStep(
    int index,
    String title,
    String subtitle,
    String content,
    ColorScheme colors,
  ) {
    return Step(
      title: Text(title),
      subtitle: Text(subtitle),
      label: Text('${index + 1}'),
      content: Container(
        padding: const EdgeInsets.all(12),
        color: const Color(0xFFF3EDF7),
        child: Text(content),
      ),
      state: index < _currentStep
          ? StepState.complete
          : index == _currentStep
          ? StepState.editing
          : StepState.indexed,
      isActive: index <= _currentStep,
      stepStyle: index == 2
          ? StepStyle(
              border: Border.all(color: colors.outline),
              gradient: LinearGradient(
                colors: <Color>[
                  colors.secondaryContainer,
                  colors.primaryContainer,
                ],
              ),
            )
          : null,
    );
  }
}
