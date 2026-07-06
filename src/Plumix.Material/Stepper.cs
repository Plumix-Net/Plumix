using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/stepper.dart
public enum StepState
{
    Indexed,
    Editing,
    Complete,
    Disabled,
    Error,
}

public enum StepperType
{
    Vertical,
    Horizontal,
}

public sealed record ControlsDetails(
    int CurrentStep,
    int StepIndex,
    Action? OnStepCancel = null,
    Action? OnStepContinue = null)
{
    public bool IsActive => CurrentStep == StepIndex;
}

public delegate Widget ControlsWidgetBuilder(BuildContext context, ControlsDetails details);
public delegate Widget? StepIconBuilder(int stepIndex, StepState stepState);

public sealed record StepStyle(
    Color? Color = null,
    Color? ErrorColor = null,
    Color? ConnectorColor = null,
    double? ConnectorThickness = null,
    BorderSide? Border = null,
    BoxShadow? BoxShadow = null,
    IBrush? Gradient = null,
    TextStyle? IndexStyle = null)
{
    public StepStyle CopyWith(
        Color? color = null,
        Color? errorColor = null,
        Color? connectorColor = null,
        double? connectorThickness = null,
        BorderSide? border = null,
        BoxShadow? boxShadow = null,
        IBrush? gradient = null,
        TextStyle? indexStyle = null) => this with
    {
        Color = color ?? Color,
        ErrorColor = errorColor ?? ErrorColor,
        ConnectorColor = connectorColor ?? ConnectorColor,
        ConnectorThickness = connectorThickness ?? ConnectorThickness,
        Border = border ?? Border,
        BoxShadow = boxShadow ?? BoxShadow,
        Gradient = gradient ?? Gradient,
        IndexStyle = indexStyle ?? IndexStyle,
    };

    public StepStyle Merge(StepStyle? style) => style is null ? this : CopyWith(
        color: style.Color,
        errorColor: style.ErrorColor,
        connectorColor: style.ConnectorColor,
        connectorThickness: style.ConnectorThickness,
        border: style.Border,
        boxShadow: style.BoxShadow,
        gradient: style.Gradient,
        indexStyle: style.IndexStyle);
}

public sealed record Step
{
    public Step(
        Widget title,
        Widget content,
        Widget? subtitle = null,
        StepState state = StepState.Indexed,
        bool isActive = false,
        Widget? label = null,
        StepStyle? stepStyle = null)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Subtitle = subtitle;
        State = state;
        IsActive = isActive;
        Label = label;
        StepStyle = stepStyle;
    }

    public Widget Title { get; }
    public Widget? Subtitle { get; }
    public Widget Content { get; }
    public StepState State { get; }
    public bool IsActive { get; }
    public Widget? Label { get; }
    public StepStyle? StepStyle { get; }
}

public sealed class Stepper : StatefulWidget
{
    public Stepper(
        IReadOnlyList<Step> steps,
        ScrollController? controller = null,
        ScrollPhysics? physics = null,
        StepperType type = StepperType.Vertical,
        int currentStep = 0,
        Action<int>? onStepTapped = null,
        Action? onStepContinue = null,
        Action? onStepCancel = null,
        ControlsWidgetBuilder? controlsBuilder = null,
        double? elevation = null,
        Thickness? margin = null,
        MaterialStateProperty<Color?>? connectorColor = null,
        double? connectorThickness = null,
        StepIconBuilder? stepIconBuilder = null,
        double? stepIconHeight = null,
        double? stepIconWidth = null,
        Thickness? stepIconMargin = null,
        Clip clipBehavior = Clip.None,
        Thickness? headerPadding = null,
        Thickness? contentPadding = null,
        Key? key = null) : base(key)
    {
        Steps = steps ?? throw new ArgumentNullException(nameof(steps));
        if (steps.Count == 0) throw new ArgumentException("Stepper requires at least one step.", nameof(steps));
        if (currentStep < 0 || currentStep >= steps.Count) throw new ArgumentOutOfRangeException(nameof(currentStep));
        ValidateIconExtent(stepIconHeight, nameof(stepIconHeight));
        ValidateIconExtent(stepIconWidth, nameof(stepIconWidth));
        if (stepIconHeight.HasValue && stepIconWidth.HasValue && stepIconHeight != stepIconWidth)
        {
            throw new ArgumentException("When both icon dimensions are set, they must be equal.");
        }
        if (connectorThickness.HasValue && (!double.IsFinite(connectorThickness.Value) || connectorThickness.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(connectorThickness));
        }
        if (elevation.HasValue && (!double.IsFinite(elevation.Value) || elevation.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(elevation));
        }

        Controller = controller;
        Physics = physics;
        Type = type;
        CurrentStep = currentStep;
        OnStepTapped = onStepTapped;
        OnStepContinue = onStepContinue;
        OnStepCancel = onStepCancel;
        ControlsBuilder = controlsBuilder;
        Elevation = elevation;
        Margin = margin;
        ConnectorColor = connectorColor;
        ConnectorThickness = connectorThickness;
        StepIconBuilder = stepIconBuilder;
        StepIconHeight = stepIconHeight;
        StepIconWidth = stepIconWidth;
        StepIconMargin = stepIconMargin;
        ClipBehavior = clipBehavior;
        HeaderPadding = headerPadding;
        ContentPadding = contentPadding;
    }

    public IReadOnlyList<Step> Steps { get; }
    public ScrollController? Controller { get; }
    public ScrollPhysics? Physics { get; }
    public StepperType Type { get; }
    public int CurrentStep { get; }
    public Action<int>? OnStepTapped { get; }
    public Action? OnStepContinue { get; }
    public Action? OnStepCancel { get; }
    public ControlsWidgetBuilder? ControlsBuilder { get; }
    public double? Elevation { get; }
    public Thickness? Margin { get; }
    public MaterialStateProperty<Color?>? ConnectorColor { get; }
    public double? ConnectorThickness { get; }
    public StepIconBuilder? StepIconBuilder { get; }
    public double? StepIconHeight { get; }
    public double? StepIconWidth { get; }
    public Thickness? StepIconMargin { get; }
    public Clip ClipBehavior { get; }
    public Thickness? HeaderPadding { get; }
    public Thickness? ContentPadding { get; }

    public override State CreateState() => new StepperState();

    private static void ValidateIconExtent(double? value, string name)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value is < 24 or > 80))
        {
            throw new ArgumentOutOfRangeException(name, "Step icon dimensions must be between 24 and 80.");
        }
    }

    private sealed class StepperState : State
    {
        private static readonly TimeSpan ThemeAnimationDuration = TimeSpan.FromMilliseconds(200);
        private readonly List<AnimationController> _bodyControllers = [];
        private readonly Dictionary<int, StepState> _oldStates = [];
        private readonly Dictionary<int, AnimationController> _iconControllers = [];

        private Stepper CurrentWidget => (Stepper)StateWidget;

        public override void InitState()
        {
            for (var index = 0; index < CurrentWidget.Steps.Count; index++)
            {
                _oldStates[index] = CurrentWidget.Steps[index].State;
                var body = CreateController(Curves.FastOutSlowIn);
                if (index == CurrentWidget.CurrentStep) SetControllerToEnd(body);
                _bodyControllers.Add(body);
            }
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldStepper = (Stepper)oldWidget;
            if (oldStepper.Steps.Count != CurrentWidget.Steps.Count)
            {
                throw new InvalidOperationException("Stepper steps length must not change without replacing its key.");
            }

            for (var index = 0; index < CurrentWidget.Steps.Count; index++)
            {
                if (index == CurrentWidget.CurrentStep) _bodyControllers[index].Forward();
                else _bodyControllers[index].Reverse();

                if (oldStepper.Steps[index].State != CurrentWidget.Steps[index].State)
                {
                    _oldStates[index] = oldStepper.Steps[index].State;
                    if (_iconControllers.Remove(index, out var oldController)) DisposeController(oldController);
                    var controller = CreateController(Curves.FastOutSlowIn);
                    _iconControllers[index] = controller;
                    controller.Forward(from: 0);
                }
            }
        }

        public override Widget Build(BuildContext context)
        {
            if (context.DependOnInherited<StepperScope>() is not null)
            {
                throw new InvalidOperationException("Steppers must not be nested.");
            }

            var child = CurrentWidget.Type switch
            {
                StepperType.Vertical => BuildVertical(context),
                StepperType.Horizontal => BuildHorizontal(context),
                _ => throw new ArgumentOutOfRangeException(),
            };
            return new StepperScope(child);
        }

        public override void Dispose()
        {
            foreach (var controller in _bodyControllers) DisposeController(controller);
            foreach (var controller in _iconControllers.Values) DisposeController(controller);
            _bodyControllers.Clear();
            _iconControllers.Clear();
        }

        private Widget BuildVertical(BuildContext context)
        {
            var children = new List<Widget>(CurrentWidget.Steps.Count);
            for (var index = 0; index < CurrentWidget.Steps.Count; index++)
            {
                var captured = index;
                var step = CurrentWidget.Steps[index];
                var header = new InkWell(
                    canRequestFocus: step.State != StepState.Disabled,
                    onTap: step.State == StepState.Disabled ? null : () => CurrentWidget.OnStepTapped?.Invoke(captured),
                    child: BuildVerticalHeader(context, index));
                children.Add(new Column(
                    mainAxisSize: MainAxisSize.Min,
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    children: [header, BuildVerticalBody(context, index)]));
            }

            Widget result = new SingleChildScrollView(
                controller: CurrentWidget.Controller,
                physics: CurrentWidget.Physics,
                child: new Column(
                    mainAxisSize: MainAxisSize.Min,
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    children: children));
            if (CurrentWidget.Margin.HasValue) result = new Padding(CurrentWidget.Margin.Value, result);
            return result;
        }

        private Widget BuildVerticalHeader(BuildContext context, int index)
        {
            var step = CurrentWidget.Steps[index];
            var previousActive = index > 0 && CurrentWidget.Steps[index - 1].IsActive;
            return new Padding(
                CurrentWidget.HeaderPadding ?? new Thickness(24, 0),
                new Row(children:
                [
                    new Column(
                        mainAxisSize: MainAxisSize.Min,
                        children:
                        [
                            BuildVerticalLine(index != 0, previousActive),
                            BuildIcon(context, index),
                            BuildVerticalLine(index != CurrentWidget.Steps.Count - 1, step.IsActive),
                        ]),
                    new Expanded(new Padding(new Thickness(12, 0, 0, 0), BuildHeaderText(context, index))),
                ]));
        }

        private Widget BuildVerticalBody(BuildContext context, int index)
        {
            var progress = _bodyControllers[index].Evaluate();
            var leftMargin = CurrentWidget.StepIconMargin?.Left ?? 0;
            var textDirection = Directionality.Of(context);
            var padding = CurrentWidget.ContentPadding ?? (textDirection == TextDirection.Rtl
                ? new Thickness(24, 0, 60 + leftMargin, 24)
                : new Thickness(60 + leftMargin, 0, 24, 24));
            Widget content = new Column(
                mainAxisSize: MainAxisSize.Min,
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children: [ClipContent(CurrentWidget.Steps[index].Content), BuildControls(context, index)]);
            content = new Padding(padding, content);
            content = new Opacity(progress, new Align(
                alignment: Alignment.TopCenter,
                heightFactor: progress,
                child: content));

            var connectorOffset = 24 + ((CurrentWidget.StepIconWidth ?? 24) / 2);
            var connector = new Positioned(
                left: textDirection == TextDirection.Rtl ? null : connectorOffset,
                right: textDirection == TextDirection.Rtl ? connectorOffset : null,
                top: 0,
                bottom: 0,
                width: index == CurrentWidget.Steps.Count - 1 ? 0 : CurrentWidget.ConnectorThickness ?? 1,
                child: new ColoredBox(ResolveConnectorColor(CurrentWidget.Steps[index].IsActive)));
            return new Stack(children: [connector, content]);
        }

        private Widget BuildHorizontal(BuildContext context)
        {
            var headerChildren = new List<Widget>();
            for (var index = 0; index < CurrentWidget.Steps.Count; index++)
            {
                var captured = index;
                var step = CurrentWidget.Steps[index];
                headerChildren.Add(new InkWell(
                    canRequestFocus: step.State != StepState.Disabled,
                    onTap: step.State == StepState.Disabled ? null : () => CurrentWidget.OnStepTapped?.Invoke(captured),
                    child: new Row(
                        mainAxisSize: MainAxisSize.Min,
                        children:
                        [
                            BuildHorizontalIconAndLabel(context, index),
                            new Padding(CurrentWidget.StepIconMargin ?? new Thickness(12, 0, 0, 0), BuildHeaderText(context, index)),
                        ])));
                if (index != CurrentWidget.Steps.Count - 1)
                {
                    var style = step.StepStyle;
                    headerChildren.Add(new Expanded(new Padding(
                        CurrentWidget.StepIconMargin ?? new Thickness(8, 0),
                        new SizedBox(
                            height: style?.ConnectorThickness ?? CurrentWidget.ConnectorThickness ?? 1,
                            child: new ColoredBox(style?.ConnectorColor ?? ResolveConnectorColor(step.IsActive))))));
                }
            }

            Widget header = new Padding(
                CurrentWidget.HeaderPadding ?? new Thickness(24, 0),
                new SizedBox(
                    height: CurrentWidget.StepIconHeight.HasValue
                        ? CurrentWidget.StepIconHeight.Value * (HasLabels ? 2.5 : 2)
                        : HasLabels ? 104 : 72,
                    child: new Row(children: headerChildren)));
            header = new DecoratedBox(
                new BoxDecoration(
                    Color: Theme.Of(context).SurfaceColor,
                    BoxShadows: BuildShadow(Theme.Of(context).ShadowColor, CurrentWidget.Elevation ?? 2)),
                header);

            Widget panel = new Column(
                mainAxisSize: MainAxisSize.Min,
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children: CurrentWidget.Steps
                    .Select((step, index) => (Widget)new Offstage(
                        offstage: index != CurrentWidget.CurrentStep,
                        child: ClipContent(step.Content)))
                    .ToArray());
            panel = new Column(
                mainAxisSize: MainAxisSize.Min,
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children: [panel, BuildControls(context, CurrentWidget.CurrentStep)]);
            panel = new SingleChildScrollView(
                controller: CurrentWidget.Controller,
                physics: CurrentWidget.Physics,
                padding: CurrentWidget.ContentPadding ?? new Thickness(24),
                child: panel);
            return new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children: [header, new Expanded(panel)]);
        }

        private Widget BuildHorizontalIconAndLabel(BuildContext context, int index)
        {
            var label = CurrentWidget.Steps[index].Label;
            return new SizedBox(
                height: HasLabels ? 104 : 72,
                child: new Column(
                    mainAxisAlignment: MainAxisAlignment.Center,
                    mainAxisSize: MainAxisSize.Min,
                    children:
                    [
                        label is null ? new SizedBox() : new SizedBox(height: 24),
                        BuildIcon(context, index),
                        label is null
                            ? new SizedBox()
                            : new SizedBox(height: 24, child: StyledText(context, index, label, labelStyle: true)),
                    ]));
        }

        private Widget BuildIcon(BuildContext context, int index)
        {
            if (_iconControllers.TryGetValue(index, out var transition) && transition.IsAnimating)
            {
                var progress = transition.Evaluate();
                return new Stack(
                    alignment: Alignment.Center,
                    children:
                    [
                        new Opacity(1 - progress, BuildIconForState(context, index, _oldStates[index])),
                        new Opacity(progress, BuildIconForState(context, index, CurrentWidget.Steps[index].State)),
                    ]);
            }
            return BuildIconForState(context, index, CurrentWidget.Steps[index].State);
        }

        private Widget BuildIconForState(BuildContext context, int index, StepState state)
        {
            var custom = CurrentWidget.StepIconBuilder?.Invoke(index, state);
            if (custom is not null) return WrapIconBox(custom, index, state == StepState.Error);
            Widget child = state switch
            {
                StepState.Editing => new Icon(Icons.Edit, size: 18, color: ResolveIconForeground(index)),
                StepState.Complete => new Icon(Icons.Check, size: 18, color: ResolveIconForeground(index)),
                StepState.Error => new Text("!", fontSize: 12, color: Colors.White),
                _ => new Text((index + 1).ToString(), fontSize: 12, color: ResolveIndexColor(index),
                    fontWeight: CurrentWidget.Steps[index].StepStyle?.IndexStyle?.FontWeight),
            };
            return WrapIconBox(child, index, state == StepState.Error);
        }

        private Widget WrapIconBox(Widget child, int index, bool error)
        {
            var width = CurrentWidget.StepIconWidth ?? 24;
            var height = CurrentWidget.StepIconHeight ?? 24;
            Widget decorated;
            if (error)
            {
                decorated = new CustomPaint(
                    painter: new TrianglePainter(CurrentWidget.Steps[index].StepStyle?.ErrorColor ?? Colors.Red),
                    child: new Align(alignment: new Alignment(0, 0.8), child: child));
            }
            else
            {
                var style = CurrentWidget.Steps[index].StepStyle;
                decorated = new DecoratedBox(
                    new BoxDecoration(
                        Color: style?.Gradient is null ? style?.Color ?? ResolveCircleColor(index) : null,
                        Brush: style?.Gradient,
                        Border: style?.Border,
                        BoxShadows: style?.BoxShadow is { } shadow ? new BoxShadows(shadow) : null,
                        Shape: BoxShape.Circle),
                    new Center(child: child));
            }
            return new Padding(
                CurrentWidget.StepIconMargin ?? new Thickness(0, 8),
                new SizedBox(width: width, height: error ? height * 0.866025 : height, child: decorated));
        }

        private Widget BuildHeaderText(BuildContext context, int index)
        {
            var step = CurrentWidget.Steps[index];
            var children = new List<Widget> { StyledText(context, index, step.Title) };
            if (step.Subtitle is not null)
            {
                children.Add(new Padding(new Thickness(0, 2, 0, 0), StyledText(context, index, step.Subtitle, subtitle: true)));
            }
            return new Column(
                mainAxisSize: MainAxisSize.Min,
                crossAxisAlignment: CrossAxisAlignment.Start,
                children: children);
        }

        private Widget StyledText(BuildContext context, int index, Widget child, bool subtitle = false, bool labelStyle = false)
        {
            var theme = Theme.Of(context);
            var style = subtitle ? theme.TextTheme.BodySmall : theme.TextTheme.BodyLarge;
            var state = CurrentWidget.Steps[index].State;
            if (state == StepState.Disabled)
            {
                style = style.CopyWith(color: theme.Brightness == Brightness.Dark
                    ? Color.FromArgb(0x61, 0xFF, 0xFF, 0xFF)
                    : Color.FromArgb(0x61, 0, 0, 0));
            }
            else if (state == StepState.Error)
            {
                style = style.CopyWith(color: CurrentWidget.Steps[index].StepStyle?.ErrorColor ?? Colors.Red);
            }
            return new DefaultTextStyle(style, child);
        }

        private Widget BuildControls(BuildContext context, int stepIndex)
        {
            var details = new ControlsDetails(
                CurrentStep: CurrentWidget.CurrentStep,
                StepIndex: stepIndex,
                OnStepCancel: CurrentWidget.OnStepCancel,
                OnStepContinue: CurrentWidget.OnStepContinue);
            if (CurrentWidget.ControlsBuilder is not null) return CurrentWidget.ControlsBuilder(context, details);

            var theme = Theme.Of(context);
            var localizations = MaterialLocalizations.Of(context);
            var continueText = theme.UseMaterial3
                ? localizations.ContinueButtonLabel
                : localizations.ContinueButtonLabel.ToUpperInvariant();
            var cancelText = theme.UseMaterial3
                ? localizations.CancelButtonLabel
                : localizations.CancelButtonLabel.ToUpperInvariant();
            var cancelColor = theme.Brightness == Brightness.Dark
                ? Color.FromArgb(0xB3, 0xFF, 0xFF, 0xFF)
                : Color.FromArgb(0x8A, 0, 0, 0);
            return new Padding(
                new Thickness(0, 16, 0, 0),
                new SizedBox(
                    height: 48,
                    child: new Row(
                        mainAxisSize: MainAxisSize.Min,
                        children:
                        [
                            new TextButton(
                                onPressed: CurrentWidget.OnStepContinue,
                                child: new Text(continueText),
                                style: TextButton.StyleFrom(
                                    foregroundColor: theme.Brightness == Brightness.Dark ? theme.OnSurfaceColor : theme.OnPrimaryColor,
                                    backgroundColor: theme.Brightness == Brightness.Dark ? null : theme.PrimaryColor,
                                    padding: new Thickness(16, 0),
                                    shape: BorderRadius.Circular(2))),
                            new Padding(
                                new Thickness(8, 0, 0, 0),
                                new TextButton(
                                    onPressed: CurrentWidget.OnStepCancel,
                                    child: new Text(cancelText),
                                    style: TextButton.StyleFrom(
                                        foregroundColor: cancelColor,
                                        padding: new Thickness(16, 0),
                                        shape: BorderRadius.Circular(2)))),
                        ])));
        }

        private Widget BuildVerticalLine(bool visible, bool active) => new ColoredBox(
            ResolveConnectorColor(active),
            new SizedBox(width: visible ? CurrentWidget.ConnectorThickness ?? 1 : 0, height: 16));

        private Widget ClipContent(Widget content) => CurrentWidget.ClipBehavior == Clip.None
            ? content
            : new ClipRect(child: content);

        private Color ResolveConnectorColor(bool active)
        {
            var state = active ? MaterialState.Selected : MaterialState.Disabled;
            return CurrentWidget.ConnectorColor?.Resolve(state)
                   ?? (active ? Theme.Of(Context).PrimaryColor : Color.Parse("#FFBDBDBD"));
        }

        private Color ResolveCircleColor(int index)
        {
            var theme = Theme.Of(Context);
            var step = CurrentWidget.Steps[index];
            var state = step.IsActive ? MaterialState.Selected : MaterialState.Disabled;
            var connector = CurrentWidget.ConnectorColor?.Resolve(state);
            if (connector.HasValue) return connector.Value;
            if (theme.Brightness == Brightness.Dark)
            {
                return step.IsActive ? theme.SecondaryColor : theme.CanvasColor;
            }
            return step.IsActive ? theme.PrimaryColor : ApplyOpacity(theme.OnSurfaceColor, 0.38);
        }

        private Color ResolveIconForeground(int index)
        {
            var theme = Theme.Of(Context);
            return theme.Brightness == Brightness.Dark && CurrentWidget.Steps[index].IsActive
                ? Color.FromArgb(0xDE, 0, 0, 0)
                : Colors.White;
        }

        private Color ResolveIndexColor(int index)
        {
            return CurrentWidget.Steps[index].StepStyle?.IndexStyle?.Color
                   ?? ResolveIconForeground(index);
        }

        private bool HasLabels => CurrentWidget.Steps.Any(step => step.Label is not null);

        private AnimationController CreateController(Curve curve)
        {
            var controller = new AnimationController(ThemeAnimationDuration) { Curve = curve };
            controller.Changed += HandleAnimationChanged;
            return controller;
        }

        private static void SetControllerToEnd(AnimationController controller)
        {
            controller.Forward(from: 1);
            controller.Stop();
        }

        private void DisposeController(AnimationController controller)
        {
            controller.Changed -= HandleAnimationChanged;
            controller.Dispose();
        }

        private void HandleAnimationChanged() => SetState(() => { });

        private static Color ApplyOpacity(Color color, double opacity) => Color.FromArgb(
            (byte)Math.Round(color.A * Math.Clamp(opacity, 0, 1)), color.R, color.G, color.B);

        private static BoxShadows? BuildShadow(Color color, double elevation)
        {
            if (elevation <= 0) return null;
            return new BoxShadows(new BoxShadow
            {
                Color = ApplyOpacity(color, 0.20),
                OffsetY = elevation,
                Blur = elevation * 2.4,
            });
        }
    }

    private sealed class StepperScope : InheritedWidget
    {
        public StepperScope(Widget child) => Child = child;

        public Widget Child { get; }

        public override Widget Build(BuildContext context) => Child;

        protected override bool UpdateShouldNotify(InheritedWidget oldWidget) => false;
    }

    private sealed class TrianglePainter(Color color) : CustomPainter
    {
        public Color Color { get; } = color;

        public override void Paint(PaintingContext context, Size size)
        {
            context.DrawPolygon(
                new SolidColorBrush(Color),
                pen: null,
                [new Point(0, size.Height), new Point(size.Width, size.Height), new Point(size.Width / 2, 0)]);
        }

        public override bool ShouldRepaint(CustomPainter oldDelegate) =>
            oldDelegate is not TrianglePainter old || old.Color != Color;

        public override bool? HitTest(Point position) => true;
    }
}
