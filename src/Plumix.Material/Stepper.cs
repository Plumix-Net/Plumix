using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using BoxShadow = Plumix.Rendering.BoxShadow;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/stepper.dart
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
    BoxBorder? Border = null,
    BoxShadow? BoxShadow = null,
    Gradient? Gradient = null,
    TextStyle? IndexStyle = null)
{
    public StepStyle CopyWith(
        Color? color = null,
        Color? errorColor = null,
        Color? connectorColor = null,
        double? connectorThickness = null,
        BoxBorder? border = null,
        BoxShadow? boxShadow = null,
        Gradient? gradient = null,
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
        EdgeInsetsGeometry? margin = null,
        WidgetStateProperty<Color>? connectorColor = null,
        double? connectorThickness = null,
        StepIconBuilder? stepIconBuilder = null,
        double? stepIconHeight = null,
        double? stepIconWidth = null,
        EdgeInsets? stepIconMargin = null,
        Clip clipBehavior = Clip.None,
        EdgeInsetsGeometry? headerPadding = null,
        EdgeInsetsGeometry? contentPadding = null,
        Key? key = null) : base(key)
    {
        Steps = steps ?? throw new ArgumentNullException(nameof(steps));
        if (currentStep < 0 || currentStep >= steps.Count) throw new ArgumentOutOfRangeException(nameof(currentStep));
        ValidateIconExtent(stepIconHeight, nameof(stepIconHeight));
        ValidateIconExtent(stepIconWidth, nameof(stepIconWidth));
        if (stepIconHeight.HasValue && stepIconWidth.HasValue && stepIconHeight != stepIconWidth)
        {
            throw new ArgumentException("When both icon dimensions are set, they must be equal.");
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
    public EdgeInsetsGeometry? Margin { get; }
    public WidgetStateProperty<Color>? ConnectorColor { get; }
    public double? ConnectorThickness { get; }
    public StepIconBuilder? StepIconBuilder { get; }
    public double? StepIconHeight { get; }
    public double? StepIconWidth { get; }
    public EdgeInsets? StepIconMargin { get; }
    public Clip ClipBehavior { get; }
    public EdgeInsetsGeometry? HeaderPadding { get; }
    public EdgeInsetsGeometry? ContentPadding { get; }

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
        private readonly List<GlobalKey> _keys = [];
        private readonly Dictionary<int, StepState> _oldStates = [];

        private Stepper CurrentWidget => (Stepper)StateWidget;

        public override void InitState()
        {
            for (int index = 0; index < CurrentWidget.Steps.Count; index++)
            {
                _keys.Add(new LabeledGlobalKey<State>($"Stepper step {index}"));
                _oldStates[index] = CurrentWidget.Steps[index].State;
            }
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldStepper = (Stepper)oldWidget;
            if (oldStepper.Steps.Count != CurrentWidget.Steps.Count)
            {
                throw new InvalidOperationException("Stepper steps length must not change without replacing its key.");
            }

            for (int index = 0; index < CurrentWidget.Steps.Count; index++)
            {
                _oldStates[index] = oldStepper.Steps[index].State;
            }
        }

        public override Widget Build(BuildContext context)
        {
            if (context.FindAncestorWidgetOfExactType<Stepper>() is not null)
            {
                throw new InvalidOperationException("Steppers must not be nested.");
            }

            var child = CurrentWidget.Type switch
            {
                StepperType.Vertical => BuildVertical(context),
                StepperType.Horizontal => BuildHorizontal(context),
                _ => throw new ArgumentOutOfRangeException(),
            };
            return child;
        }

        private Widget BuildVertical(BuildContext context)
        {
            var children = new List<Widget>(CurrentWidget.Steps.Count);
            for (int index = 0; index < CurrentWidget.Steps.Count; index++)
            {
                int captured = index;
                var step = CurrentWidget.Steps[index];
                var header = new InkWell(
                    canRequestFocus: step.State != StepState.Disabled,
                    onTap: step.State == StepState.Disabled
                        ? null
                        : () => HandleStepTapped(captured, ensureVisible: true),
                    child: BuildVerticalHeader(context, index));
                children.Add(new Column(
                    mainAxisSize: MainAxisSize.Min,
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    children: [header, BuildVerticalBody(context, index)],
                    key: _keys[index]));
            }

            return new ListView(
                controller: CurrentWidget.Controller,
                physics: CurrentWidget.Physics,
                shrinkWrap: true,
                children: children);
        }

        private Widget BuildVerticalHeader(BuildContext context, int index)
        {
            var step = CurrentWidget.Steps[index];
            bool previousActive = index > 0 && CurrentWidget.Steps[index - 1].IsActive;
            return new Padding(
                CurrentWidget.HeaderPadding ?? EdgeInsetsGeometry.Symmetric(horizontal: 24),
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
                    new Expanded(new Padding(
                        EdgeInsetsGeometry.DirectionalOnly(start: 12),
                        BuildHeaderText(context, index))),
                ]));
        }

        private Widget BuildVerticalBody(BuildContext context, int index)
        {
            double leftMargin = CurrentWidget.StepIconMargin?.Left ?? 0;
            double rightMargin = CurrentWidget.StepIconMargin?.Right ?? 0;
            TextDirection textDirection = Directionality.Of(context);
            Thickness padding = ResolveVerticalContentPadding(textDirection, leftMargin);
            Widget content = new Column(
                mainAxisSize: MainAxisSize.Min,
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children: [ClipContent(CurrentWidget.Steps[index].Content), BuildControls(context, index)]);
            content = new Padding(padding, content);

            double connectorOffset = 24 + ((leftMargin + rightMargin) / 2);
            var connector = new PositionedDirectional(
                start: connectorOffset,
                top: 0,
                bottom: 0,
                width: CurrentWidget.StepIconWidth ?? 24,
                child: new Center(
                    child: new SizedBox(
                        width: index == CurrentWidget.Steps.Count - 1
                            ? 0
                            : CurrentWidget.ConnectorThickness ?? 1,
                        child: new ColoredBox(ResolveConnectorColor(CurrentWidget.Steps[index].IsActive)))));
            var body = new AnimatedCrossFade(
                firstChild: new SizedBox(width: double.PositiveInfinity, height: 0),
                secondChild: content,
                crossFadeState: index == CurrentWidget.CurrentStep
                    ? CrossFadeState.ShowSecond
                    : CrossFadeState.ShowFirst,
                duration: MaterialConstants.ThemeAnimationDuration,
                firstCurve: Curves.Interval(0.0, 0.6, Curves.FastOutSlowIn),
                secondCurve: Curves.Interval(0.4, 1.0, Curves.FastOutSlowIn),
                sizeCurve: Curves.FastOutSlowIn);
            return new Stack(children: [connector, body]);
        }

        private Widget BuildHorizontal(BuildContext context)
        {
            var headerChildren = new List<Widget>();
            for (int index = 0; index < CurrentWidget.Steps.Count; index++)
            {
                int captured = index;
                var step = CurrentWidget.Steps[index];
                headerChildren.Add(new InkResponse(
                    canRequestFocus: step.State != StepState.Disabled,
                    onTap: step.State == StepState.Disabled
                        ? null
                        : () => HandleStepTapped(captured, ensureVisible: false),
                    child: new Row(
                        mainAxisSize: MainAxisSize.Min,
                        children:
                        [
                            BuildHorizontalIconAndLabel(context, index),
                            new Padding(
                                CurrentWidget.StepIconMargin.HasValue
                                    ? (EdgeInsetsGeometry)CurrentWidget.StepIconMargin.Value
                                    : EdgeInsetsGeometry.DirectionalOnly(start: 12),
                                BuildHeaderText(context, index)),
                        ])));
                if (index != CurrentWidget.Steps.Count - 1)
                {
                    var style = step.StepStyle;
                    headerChildren.Add(new Expanded(new Padding(
                        CurrentWidget.StepIconMargin.HasValue
                            ? (EdgeInsetsGeometry)CurrentWidget.StepIconMargin.Value
                            : EdgeInsetsGeometry.Symmetric(horizontal: 8),
                        new SizedBox(
                            height: style?.ConnectorThickness ?? CurrentWidget.ConnectorThickness ?? 1,
                            child: new ColoredBox(style?.ConnectorColor ?? ResolveConnectorColor(step.IsActive))))));
                }
            }

            Widget header = new Padding(
                CurrentWidget.HeaderPadding ?? EdgeInsetsGeometry.Symmetric(horizontal: 24),
                new SizedBox(
                    height: CurrentWidget.StepIconHeight.HasValue
                        ? CurrentWidget.StepIconHeight.Value * (HasLabels ? 2.5 : 2)
                        : null,
                    child: new Row(children: headerChildren)));
            header = new Material(elevation: CurrentWidget.Elevation ?? 2, child: header);

            var panels = CurrentWidget.Steps
                .Select((step, index) => (Widget)new Visibility(
                    visible: index == CurrentWidget.CurrentStep,
                    maintainState: true,
                    child: ClipContent(step.Content)))
                .ToArray();
            Widget panel = new AnimatedSize(
                duration: MaterialConstants.ThemeAnimationDuration,
                curve: Curves.FastOutSlowIn,
                child: new Column(
                    mainAxisSize: MainAxisSize.Min,
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    children: panels));
            Widget content = new ListView(
                controller: CurrentWidget.Controller,
                physics: CurrentWidget.Physics,
                padding: (CurrentWidget.ContentPadding ?? EdgeInsetsGeometry.All(24))
                    .Resolve(Directionality.Of(context)),
                children:
                [
                    panel,
                    BuildControls(context, CurrentWidget.CurrentStep),
                ]);
            return new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children: [header, new Expanded(content)]);
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
            StepState state = CurrentWidget.Steps[index].State;
            StepState oldState = _oldStates[index];
            if (state == oldState)
            {
                return state == StepState.Error
                    ? BuildTriangle(context, index, state)
                    : BuildCircle(context, index, state);
            }

            StepState circleState = state == StepState.Error ? oldState : state;
            StepState triangleState = state == StepState.Error ? state : oldState;
            return new AnimatedCrossFade(
                firstChild: BuildCircle(context, index, circleState),
                secondChild: BuildTriangle(context, index, triangleState),
                crossFadeState: state == StepState.Error
                    ? CrossFadeState.ShowSecond
                    : CrossFadeState.ShowFirst,
                duration: MaterialConstants.ThemeAnimationDuration,
                firstCurve: Curves.Interval(0.0, 0.6, Curves.FastOutSlowIn),
                secondCurve: Curves.Interval(0.4, 1.0, Curves.FastOutSlowIn),
                sizeCurve: Curves.FastOutSlowIn);
        }

        private Widget BuildCircle(BuildContext context, int index, StepState state)
        {
            var style = CurrentWidget.Steps[index].StepStyle;
            return new Padding(
                CurrentWidget.StepIconMargin.HasValue
                    ? (EdgeInsetsGeometry)CurrentWidget.StepIconMargin.Value
                    : EdgeInsetsGeometry.Symmetric(vertical: 8),
                new SizedBox(
                    width: CurrentWidget.StepIconWidth ?? 24,
                    height: CurrentWidget.StepIconHeight ?? 24,
                    child: new AnimatedContainer(
                        duration: MaterialConstants.ThemeAnimationDuration,
                        curve: Curves.FastOutSlowIn,
                        decoration: new BoxDecoration(
                            Color: style?.Color ?? ResolveCircleColor(index),
                            Gradient: style?.Gradient,
                            Border: style?.Border,
                            BoxShadows: style?.BoxShadow is { } shadow ? [shadow] : null,
                            Shape: BoxShape.Circle),
                        child: new Center(child: BuildIconChild(index, state)))));
        }

        private Widget BuildTriangle(BuildContext context, int index, StepState state)
        {
            double height = (CurrentWidget.StepIconHeight ?? 24) * 0.866025;
            Color color = ResolveErrorColor(context, index);
            return new Padding(
                CurrentWidget.StepIconMargin.HasValue
                    ? (EdgeInsetsGeometry)CurrentWidget.StepIconMargin.Value
                    : EdgeInsetsGeometry.Symmetric(vertical: 8),
                new SizedBox(
                    width: CurrentWidget.StepIconWidth ?? 24,
                    height: CurrentWidget.StepIconHeight ?? 24,
                    child: new Center(
                        child: new SizedBox(
                            width: CurrentWidget.StepIconWidth ?? 24,
                            height: height,
                            child: new CustomPaint(
                                painter: new TrianglePainter(color),
                                child: new Align(
                                    alignment: new Alignment(0, 0.8),
                                    child: BuildIconChild(index, state)))))));
        }

        private Widget BuildIconChild(int index, StepState state)
        {
            Widget? custom = CurrentWidget.StepIconBuilder?.Invoke(index, state);
            if (custom is not null)
            {
                return custom;
            }

            if (state == StepState.Editing)
            {
                return new Icon(Icons.Edit, size: 18, color: ResolveIconForeground(index));
            }
            if (state == StepState.Complete)
            {
                return new Icon(Icons.Check, size: 18, color: ResolveIconForeground(index));
            }
            if (state == StepState.Error)
            {
                return new DefaultTextStyle(
                    new TextStyle(FontSize: 12, Color: Colors.White),
                    new Text("!"));
            }

            TextStyle style = CurrentWidget.Steps[index].StepStyle?.IndexStyle
                              ?? new TextStyle(FontSize: 12, Color: ResolveIconForeground(index));
            return new DefaultTextStyle(style, new Text((index + 1).ToString()));
        }

        private Widget BuildHeaderText(BuildContext context, int index)
        {
            var step = CurrentWidget.Steps[index];
            var children = new List<Widget> { StyledText(context, index, step.Title) };
            if (step.Subtitle is not null)
            {
                children.Add(new Padding(
                    new Thickness(0, 2, 0, 0),
                    StyledText(context, index, step.Subtitle, subtitle: true)));
            }
            return new Column(
                mainAxisSize: MainAxisSize.Min,
                crossAxisAlignment: CrossAxisAlignment.Start,
                children: children);
        }

        private Widget StyledText(
            BuildContext context,
            int index,
            Widget child,
            bool subtitle = false,
            bool labelStyle = false)
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
                style = style.CopyWith(color: ResolveErrorColor(context, index));
            }
            return new AnimatedDefaultTextStyle(
                child: child,
                style: style,
                duration: MaterialConstants.ThemeAnimationDuration,
                curve: labelStyle ? Curves.Linear : Curves.FastOutSlowIn);
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
            string continueText = theme.UseMaterial3
                ? localizations.ContinueButtonLabel
                : localizations.ContinueButtonLabel.ToUpperInvariant();
            string cancelText = theme.UseMaterial3
                ? localizations.CancelButtonLabel
                : localizations.CancelButtonLabel.ToUpperInvariant();
            Color cancelColor = theme.Brightness == Brightness.Dark
                ? Color.FromArgb(0xB3, 0xFF, 0xFF, 0xFF)
                : Color.FromArgb(0x8A, 0, 0, 0);
            var continueStyle = new ButtonStyle(
                ForegroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Disabled)
                        ? null
                        : theme.Brightness == Brightness.Dark
                            ? theme.ColorScheme.OnSurface
                            : theme.ColorScheme.OnPrimary),
                BackgroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Disabled) || theme.Brightness == Brightness.Dark
                        ? null
                        : theme.ColorScheme.Primary),
                Padding: MaterialStateProperty<Thickness?>.All(new Thickness(16, 0)),
                Shape: MaterialStateProperty<OutlinedBorder?>.All(new RoundedRectangleBorder(borderRadius:
                    BorderRadius.Circular(2))));
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
                                style: continueStyle),
                            new Padding(
                                EdgeInsetsGeometry.DirectionalOnly(start: 8),
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

        private Widget ClipContent(Widget content) => new ClipRect(
            clipBehavior: CurrentWidget.ClipBehavior,
            child: content);

        private Color ResolveConnectorColor(bool active)
        {
            IReadOnlySet<WidgetState> states = active
                ? new HashSet<WidgetState> { WidgetState.Selected }
                : new HashSet<WidgetState> { WidgetState.Disabled };
            return CurrentWidget.ConnectorColor?.Resolve(states)
                   ?? (active ? Theme.Of(Context).ColorScheme.Primary : Color.Parse("#FFBDBDBD"));
        }

        private Color ResolveCircleColor(int index)
        {
            var theme = Theme.Of(Context);
            var step = CurrentWidget.Steps[index];
            IReadOnlySet<WidgetState> states = step.IsActive
                ? new HashSet<WidgetState> { WidgetState.Selected }
                : new HashSet<WidgetState> { WidgetState.Disabled };
            Color? connector = CurrentWidget.ConnectorColor?.Resolve(states);
            if (connector.HasValue) return connector.Value;
            if (theme.Brightness == Brightness.Dark)
            {
                return step.IsActive ? theme.ColorScheme.Secondary : theme.ColorScheme.Background;
            }
            return step.IsActive ? theme.ColorScheme.Primary : ApplyOpacity(theme.ColorScheme.OnSurface, 0.38);
        }

        private Color ResolveIconForeground(int index)
        {
            var theme = Theme.Of(Context);
            return theme.Brightness == Brightness.Dark && CurrentWidget.Steps[index].IsActive
                ? Color.FromArgb(0xDD, 0, 0, 0)
                : Colors.White;
        }

        private Color ResolveErrorColor(BuildContext context, int index)
        {
            var theme = Theme.Of(context);
            return CurrentWidget.Steps[index].StepStyle?.ErrorColor
                   ?? (theme.Brightness == Brightness.Dark ? Color.Parse("#FFEF5350") : Colors.Red);
        }

        private bool HasLabels => CurrentWidget.Steps.Any(step => step.Label is not null);

        private void HandleStepTapped(int index, bool ensureVisible)
        {
            BuildContext? stepContext = _keys[index].CurrentContext;
            if (ensureVisible && stepContext.HasValue)
            {
                _ = Scrollable.EnsureVisible(
                    stepContext.Value,
                    duration: MaterialConstants.ThemeAnimationDuration,
                    curve: Curves.FastOutSlowIn);
            }
            CurrentWidget.OnStepTapped?.Invoke(index);
        }

        private Thickness ResolveVerticalContentPadding(TextDirection textDirection, double iconMarginLeft)
        {
            EdgeInsetsGeometry padding = CurrentWidget.ContentPadding
                                         ?? EdgeInsetsGeometry.DirectionalOnly(start: 60, end: 24, bottom: 24);
            EdgeInsetsGeometry iconPadding = EdgeInsetsGeometry.DirectionalOnly(start: iconMarginLeft);
            return Add(padding.Resolve(textDirection), iconPadding.Resolve(textDirection));
        }

        private static Thickness Add(Thickness a, Thickness b)
        {
            return new Thickness(
                a.Left + b.Left,
                a.Top + b.Top,
                a.Right + b.Right,
                a.Bottom + b.Bottom);
        }

        private static Color ApplyOpacity(Color color, double opacity) => Color.FromArgb(
            (byte)Math.Round(color.A * Math.Clamp(opacity, 0, 1)), color.R, color.G, color.B);

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
