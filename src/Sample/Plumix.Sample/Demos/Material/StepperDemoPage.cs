using Avalonia;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/material/stepper_demo_page.dart
public sealed class StepperDemoPage : StatefulWidget
{
    public override State CreateState() => new StepperDemoPageState();
}

internal sealed class StepperDemoPageState : State
{
    private int _currentStep;
    private bool _horizontal;
    private bool _expanded;

    public override Widget Build(BuildContext context)
    {
        ColorScheme colors = Theme.Of(context).ColorScheme;
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("ExpandIcon + Stepper", fontSize: 20, color: Colors.Black),
                new Text(
                    "Flutter-shaped disclosure animation plus vertical/horizontal step progress, states, connectors, and controls.",
                    fontSize: 14,
                    color: Color.Parse("#8A000000")),
                new Row(
                    mainAxisSize: MainAxisSize.Min,
                    spacing: 8,
                    children:
                    [
                        new ExpandIcon(
                            isExpanded: _expanded,
                            onPressed: _ => SetState(() => _expanded = !_expanded),
                            expandedColor: Color.Parse("#FF6750A4")),
                        new Text(_expanded ? "Expanded" : "Collapsed", fontSize: 14),
                        new ExpandIcon(onPressed: null),
                        new Text("Disabled", fontSize: 14),
                        new TextButton(
                            child: new Text(_horizontal ? "Use vertical" : "Use horizontal"),
                            onPressed: () => SetState(() => _horizontal = !_horizontal)),
                    ]),
                new Text($"Current step: {_currentStep + 1} / 3", fontSize: 13, color: Color.Parse("#FF006C4C")),
                new Expanded(new Stepper(
                    type: _horizontal ? StepperType.Horizontal : StepperType.Vertical,
                    currentStep: _currentStep,
                    onStepTapped: index => SetState(() => _currentStep = index),
                    onStepContinue: () => SetState(() => _currentStep = Math.Min(2, _currentStep + 1)),
                    onStepCancel: () => SetState(() => _currentStep = Math.Max(0, _currentStep - 1)),
                    connectorColor: WidgetStateProperty<Color>.ResolveWith(states =>
                        states.Contains(WidgetState.Selected) ? colors.Primary : colors.OutlineVariant),
                    connectorThickness: 2,
                    headerPadding: EdgeInsetsGeometry.DirectionalOnly(start: 20, end: 20),
                    contentPadding: EdgeInsetsGeometry.DirectionalOnly(start: 56, end: 20, bottom: 20),
                    steps: BuildSteps(colors))),
            ]);
    }

    private IReadOnlyList<Step> BuildSteps(ColorScheme colors) =>
    [
        BuildStep(0, "Account", "Choose an account", "Account settings are ready.", colors),
        BuildStep(1, "Details", "Review preferences", "Notification and sync preferences.", colors),
        BuildStep(2, "Confirm", "Finish setup", "Everything is ready to submit.", colors),
    ];

    private Step BuildStep(int index, string title, string subtitle, string content, ColorScheme colors) => new(
        title: new Text(title),
        subtitle: new Text(subtitle),
        label: new Text((index + 1).ToString()),
        content: new Container(
            padding: new Thickness(12),
            color: Color.Parse("#FFF3EDF7"),
            child: new Text(content, fontSize: 14)),
        state: index < _currentStep
            ? StepState.Complete
            : index == _currentStep ? StepState.Editing : StepState.Indexed,
        isActive: index <= _currentStep,
        stepStyle: index == 2
            ? new StepStyle(
                Border: Plumix.Rendering.Border.FromBorderSide(new BorderSide(colors.Outline, 1)),
                Gradient: new LinearGradient([colors.SecondaryContainer, colors.PrimaryContainer]))
            : null);
}
