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

/// <summary>
/// Container for all the information necessary to build a Stepper widget's foreground controls for
/// each step.
/// </summary>
public sealed class ControlsDetails
{
    public ControlsDetails(
        int currentStep,
        int stepIndex,
        Action? onStepCancel = null,
        Action? onStepContinue = null)
    {
        CurrentStep = currentStep;
        StepIndex = stepIndex;
        OnStepCancel = onStepCancel;
        OnStepContinue = onStepContinue;
    }

    /// <summary>The index that the stepper is currently on.</summary>
    public int CurrentStep { get; }

    /// <summary>The index of the step whose controls are being built.</summary>
    public int StepIndex { get; }

    /// <summary>The callback called when the 'continue' button is tapped.</summary>
    public Action? OnStepContinue { get; }

    /// <summary>The callback called when the 'cancel' button is tapped.</summary>
    public Action? OnStepCancel { get; }

    /// <summary>True if the indicated step is also the current active step.</summary>
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


/// <summary>
/// A material stepper widget that displays progress through a sequence of steps.
/// </summary>
public sealed class Stepper : StatefulWidget
{
    internal static readonly TextStyle KStepStyle = new(FontSize: 12.0, Color: Colors.White);
    internal static readonly Color KErrorLight = Colors.Red;
    internal static readonly Color KErrorDark = Colors.Red.Shade400;
    internal static readonly Color KCircleActiveLight = Colors.White;
    internal static readonly Color KCircleActiveDark = Colors.Black87;
    internal static readonly Color KDisabledLight = Colors.Black38;
    internal static readonly Color KDisabledDark = Colors.White38;
    internal const double KStepSize = 24.0;
    internal const double KTriangleSqrt = 0.866025; // sqrt(3.0) / 2.0
    internal const double KTriangleHeight = KStepSize * KTriangleSqrt;
    internal const double KMaxStepSize = 80.0;

    internal static readonly EdgeInsetsGeometry KDefaultVerticalContentPadding =
        EdgeInsetsDirectional.Only(start: 60.0, end: 24.0, bottom: 24.0);
    internal static readonly EdgeInsetsGeometry KDefaultHorizontalContentPadding = EdgeInsets.All(24.0);
    internal static readonly EdgeInsetsGeometry KDefaultHeaderPadding = EdgeInsets.Symmetric(horizontal: 24.0);

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
        IWidgetStateProperty<Color>? connectorColor = null,
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
        if (currentStep < 0 || currentStep >= steps.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(currentStep));
        }
        if (stepIconHeight is { } height && !(height >= KStepSize && height <= KMaxStepSize))
        {
            throw new ArgumentOutOfRangeException(
                nameof(stepIconHeight),
                $"stepIconHeight must be greater than {KStepSize} and less or equal to {KMaxStepSize}");
        }
        if (stepIconWidth is { } width && !(width >= KStepSize && width <= KMaxStepSize))
        {
            throw new ArgumentOutOfRangeException(
                nameof(stepIconWidth),
                $"stepIconWidth must be greater than {KStepSize} and less or equal to {KMaxStepSize}");
        }
        if (stepIconHeight.HasValue && stepIconWidth.HasValue && stepIconHeight != stepIconWidth)
        {
            throw new ArgumentException(
                "If either stepIconHeight or stepIconWidth is specified, both must be specified and "
                + "the values must be equal.");
        }

        Physics = physics;
        Controller = controller;
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

    /// <summary>The steps of the stepper whose titles, subtitles, icons always get shown.</summary>
    public IReadOnlyList<Step> Steps { get; }

    /// <summary>How the stepper's scroll view should respond to user input.</summary>
    public ScrollPhysics? Physics { get; }

    /// <summary>The scroll controller of the stepper's scroll view.</summary>
    public ScrollController? Controller { get; }

    /// <summary>The type of stepper that determines the layout.</summary>
    public StepperType Type { get; }

    /// <summary>The index into <see cref="Steps"/> of the current step whose content is displayed.</summary>
    public int CurrentStep { get; }

    /// <summary>The callback called when a step is tapped, with its index passed as an argument.</summary>
    public Action<int>? OnStepTapped { get; }

    /// <summary>The callback called when the 'continue' button is tapped.</summary>
    public Action? OnStepContinue { get; }

    /// <summary>The callback called when the 'cancel' button is tapped.</summary>
    public Action? OnStepCancel { get; }

    /// <summary>The callback for creating custom controls.</summary>
    public ControlsWidgetBuilder? ControlsBuilder { get; }

    /// <summary>The elevation of this stepper's Material when <see cref="Type"/> is horizontal.</summary>
    public double? Elevation { get; }

    /// <summary>Custom margin on vertical stepper.</summary>
    public EdgeInsetsGeometry? Margin { get; }

    /// <summary>Customize connected lines colors.</summary>
    public IWidgetStateProperty<Color>? ConnectorColor { get; }

    /// <summary>The thickness of the connecting lines.</summary>
    public double? ConnectorThickness { get; }

    /// <summary>Callback for creating custom icons for every step.</summary>
    public StepIconBuilder? StepIconBuilder { get; }

    /// <summary>Overrides the default step icon size height.</summary>
    public double? StepIconHeight { get; }

    /// <summary>Overrides the default step icon size width.</summary>
    public double? StepIconWidth { get; }

    /// <summary>Overrides the default step icon margin.</summary>
    public EdgeInsets? StepIconMargin { get; }

    /// <summary>The clip behavior applied to each step's content.</summary>
    public Clip ClipBehavior { get; }

    /// <summary>Padding for the header of the stepper.</summary>
    public EdgeInsetsGeometry? HeaderPadding { get; }

    /// <summary>Padding applied to each step's content.</summary>
    public EdgeInsetsGeometry? ContentPadding { get; }

    public override State CreateState() => new StepperState();

    private sealed class StepperState : State<Stepper>
    {
        private List<GlobalKey> _keys = [];
        private readonly Dictionary<int, StepState> _oldStates = [];

        private Stepper CurrentWidget => (Stepper)StateWidget;

        public override void InitState()
        {
            base.InitState();
            _keys = Enumerable.Range(0, CurrentWidget.Steps.Count)
                .Select(index => (GlobalKey)new LabeledGlobalKey<State>($"Stepper step {index}"))
                .ToList();

            for (int i = 0; i < CurrentWidget.Steps.Count; i += 1)
            {
                _oldStates[i] = CurrentWidget.Steps[i].State;
            }
        }

        private EdgeInsetsGeometry? StepIconMargin => CurrentWidget.StepIconMargin;

        private double? StepIconHeight => CurrentWidget.StepIconHeight;

        private double? StepIconWidth => CurrentWidget.StepIconWidth;

        private double HeightFactor => IsLabel() && StepIconHeight != null ? 2.5 : 2.0;

        public override void DidUpdateWidget(Stepper oldWidget)
        {
            base.DidUpdateWidget(oldWidget);
            DebugAssertions.Assert(CurrentWidget.Steps.Count == oldWidget.Steps.Count);

            for (int i = 0; i < oldWidget.Steps.Count; i += 1)
            {
                _oldStates[i] = oldWidget.Steps[i].State;
            }
        }

        private EdgeInsetsGeometry EffectiveHeaderPadding =>
            CurrentWidget.HeaderPadding ?? KDefaultHeaderPadding;

        private bool IsFirst(int index) => index == 0;

        private bool IsLast(int index) => CurrentWidget.Steps.Count - 1 == index;

        private bool IsCurrent(int index) => CurrentWidget.CurrentStep == index;

        private bool IsDark() => Theme.BrightnessOf(Context) == Brightness.Dark;

        private bool IsLabel()
        {
            foreach (Step step in CurrentWidget.Steps)
            {
                if (step.Label != null)
                {
                    return true;
                }
            }
            return false;
        }

        private StepStyle? GetStepStyle(int index) => CurrentWidget.Steps[index].StepStyle;

        private Color ConnectorColorFor(bool isActive)
        {
            ColorScheme colorScheme = Theme.Of(Context).ColorScheme;
            IReadOnlySet<WidgetState> states = new HashSet<WidgetState>
            {
                isActive ? WidgetState.Selected : WidgetState.Disabled,
            };
            Color? resolvedConnectorColor = CurrentWidget.ConnectorColor?.Resolve(states);

            return resolvedConnectorColor ?? (isActive ? colorScheme.Primary : Colors.Grey.Shade400);
        }

        private Widget BuildLine(bool visible, bool isActive) => new ColoredBox(
            ConnectorColorFor(isActive),
            child: new SizedBox(width: visible ? CurrentWidget.ConnectorThickness ?? 1.0 : 0.0, height: 16.0));

        private Widget BuildCircleChild(int index, bool oldState)
        {
            StepState state = oldState ? _oldStates[index] : CurrentWidget.Steps[index].State;
            if (CurrentWidget.StepIconBuilder?.Invoke(index, state) is { } icon)
            {
                return icon;
            }
            TextStyle? textStyle = GetStepStyle(index)?.IndexStyle;
            bool isDarkActive = IsDark() && CurrentWidget.Steps[index].IsActive;
            Color iconColor = isDarkActive ? KCircleActiveDark : KCircleActiveLight;
            textStyle ??= isDarkActive ? KStepStyle.CopyWith(color: Colors.Black87) : KStepStyle;

            return state switch
            {
                StepState.Indexed or StepState.Disabled => new Text($"{index + 1}", style: textStyle),
                StepState.Editing => new Icon(Icons.Edit, color: iconColor, size: 18.0),
                StepState.Complete => new Icon(Icons.Check, color: iconColor, size: 18.0),
                StepState.Error => new Center(child: new Text("!", style: KStepStyle)),
                _ => throw new ArgumentOutOfRangeException(nameof(state)),
            };
        }

        private Color CircleColor(int index)
        {
            bool isActive = CurrentWidget.Steps[index].IsActive;
            ColorScheme colorScheme = Theme.Of(Context).ColorScheme;
            IReadOnlySet<WidgetState> states = new HashSet<WidgetState>
            {
                isActive ? WidgetState.Selected : WidgetState.Disabled,
            };
            Color? resolvedConnectorColor = CurrentWidget.ConnectorColor?.Resolve(states);
            if (resolvedConnectorColor != null)
            {
                return resolvedConnectorColor;
            }
            if (!IsDark())
            {
                return isActive ? colorScheme.Primary : colorScheme.OnSurface.WithOpacity(0.38);
            }
            else
            {
                return isActive ? colorScheme.Secondary : colorScheme.Background;
            }
        }

        private Widget BuildCircle(int index, bool oldState)
        {
            StepStyle? style = GetStepStyle(index);
            return new Padding(
                StepIconMargin ?? EdgeInsets.Symmetric(vertical: 8.0),
                new SizedBox(
                    width: StepIconWidth ?? KStepSize,
                    height: StepIconHeight ?? KStepSize,
                    child: new AnimatedContainer(
                        curve: Curves.FastOutSlowIn,
                        duration: MaterialConstants.ThemeAnimationDuration,
                        decoration: new BoxDecoration(
                            Color: style?.Color ?? CircleColor(index),
                            Shape: BoxShape.Circle,
                            Border: style?.Border,
                            BoxShadows: style?.BoxShadow is { } boxShadow ? [boxShadow] : null,
                            Gradient: style?.Gradient),
                        child: new Center(
                            child: BuildCircleChild(
                                index,
                                oldState && CurrentWidget.Steps[index].State == StepState.Error)))));
        }

        private Widget BuildTriangle(int index, bool oldState)
        {
            Color? color = GetStepStyle(index)?.ErrorColor;
            color ??= IsDark() ? KErrorDark : KErrorLight;

            return new Padding(
                StepIconMargin ?? EdgeInsets.Symmetric(vertical: 8.0),
                new SizedBox(
                    width: StepIconWidth ?? KStepSize,
                    height: StepIconHeight ?? KStepSize,
                    child: new Center(
                        child: new SizedBox(
                            width: StepIconWidth ?? KStepSize,
                            height: StepIconHeight != null ? StepIconHeight.Value * KTriangleSqrt : KTriangleHeight,
                            child: new CustomPaint(
                                painter: new TrianglePainter(color: color),
                                child: new Align(
                                    alignment: new Alignment(0.0, 0.8),
                                    child: BuildCircleChild(
                                        index,
                                        oldState && CurrentWidget.Steps[index].State != StepState.Error)))))));
        }

        private Widget BuildIcon(int index)
        {
            if (CurrentWidget.Steps[index].State != _oldStates[index])
            {
                return new AnimatedCrossFade(
                    firstChild: BuildCircle(index, true),
                    secondChild: BuildTriangle(index, true),
                    firstCurve: new Interval(0.0, 0.6, Curves.FastOutSlowIn),
                    secondCurve: new Interval(0.4, 1.0, Curves.FastOutSlowIn),
                    sizeCurve: Curves.FastOutSlowIn,
                    crossFadeState: CurrentWidget.Steps[index].State == StepState.Error
                        ? CrossFadeState.ShowSecond
                        : CrossFadeState.ShowFirst,
                    duration: MaterialConstants.ThemeAnimationDuration);
            }
            else
            {
                if (CurrentWidget.Steps[index].State != StepState.Error)
                {
                    return BuildCircle(index, false);
                }
                else
                {
                    return BuildTriangle(index, false);
                }
            }
        }

        private Widget BuildVerticalControls(int stepIndex)
        {
            if (CurrentWidget.ControlsBuilder != null)
            {
                return CurrentWidget.ControlsBuilder(
                    Context,
                    new ControlsDetails(
                        currentStep: CurrentWidget.CurrentStep,
                        onStepContinue: CurrentWidget.OnStepContinue,
                        onStepCancel: CurrentWidget.OnStepCancel,
                        stepIndex: stepIndex));
            }

            Color cancelColor = Theme.BrightnessOf(Context) switch
            {
                Brightness.Light => Colors.Black54,
                Brightness.Dark => Colors.White70,
                _ => throw new ArgumentOutOfRangeException(),
            };

            ThemeData themeData = Theme.Of(Context);
            ColorScheme colorScheme = themeData.ColorScheme;
            MaterialLocalizations localizations = MaterialLocalizations.Of(Context);

            OutlinedBorder buttonShape = new RoundedRectangleBorder(
                borderRadius: BorderRadius.All(Radius.Circular(2)));
            EdgeInsetsGeometry buttonPadding = EdgeInsets.Symmetric(horizontal: 16.0);

            return new Padding(
                EdgeInsets.Only(top: 16.0),
                new SizedBox(
                    height: 48.0,
                    child: new Row(
                        children:
                        [
                            new TextButton(
                                onPressed: CurrentWidget.OnStepContinue,
                                style: new ButtonStyle(
                                    ForegroundColor: WidgetStateProperty<Color?>.ResolveWith(states =>
                                        states.Contains(WidgetState.Disabled)
                                            ? null
                                            : IsDark() ? colorScheme.OnSurface : colorScheme.OnPrimary),
                                    BackgroundColor: WidgetStateProperty<Color?>.ResolveWith(states =>
                                        IsDark() || states.Contains(WidgetState.Disabled)
                                            ? null
                                            : colorScheme.Primary),
                                    Padding: WidgetStateProperty<EdgeInsetsGeometry?>.All(buttonPadding),
                                    Shape: WidgetStateProperty<OutlinedBorder?>.All(buttonShape)),
                                child: new Text(themeData.UseMaterial3
                                    ? localizations.ContinueButtonLabel
                                    : localizations.ContinueButtonLabel.ToUpperInvariant())),
                            new Padding(
                                EdgeInsetsDirectional.Only(start: 8.0),
                                new TextButton(
                                    onPressed: CurrentWidget.OnStepCancel,
                                    style: TextButton.StyleFrom(
                                        foregroundColor: cancelColor,
                                        padding: buttonPadding,
                                        shape: buttonShape),
                                    child: new Text(themeData.UseMaterial3
                                        ? localizations.CancelButtonLabel
                                        : localizations.CancelButtonLabel.ToUpperInvariant()))),
                        ])));
        }

        private TextStyle TitleStyle(int index) => StateStyle(index, Theme.Of(Context).TextTheme.BodyLarge);

        private TextStyle SubtitleStyle(int index) => StateStyle(index, Theme.Of(Context).TextTheme.BodySmall);

        private TextStyle LabelStyle(int index) => StateStyle(index, Theme.Of(Context).TextTheme.BodyLarge);

        // Shared body of Dart's _titleStyle/_subtitleStyle/_labelStyle, which differ only in the base style.
        private TextStyle StateStyle(int index, TextStyle? baseStyle)
        {
            TextStyle style = baseStyle!;
            return CurrentWidget.Steps[index].State switch
            {
                StepState.Indexed or StepState.Editing or StepState.Complete => style,
                StepState.Disabled => style.CopyWith(color: IsDark() ? KDisabledDark : KDisabledLight),
                StepState.Error => style.CopyWith(color: IsDark() ? KErrorDark : KErrorLight),
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };
        }

        private Widget BuildHeaderText(int index)
        {
            var children = new List<Widget>
            {
                new AnimatedDefaultTextStyle(
                    style: TitleStyle(index),
                    duration: MaterialConstants.ThemeAnimationDuration,
                    curve: Curves.FastOutSlowIn,
                    child: CurrentWidget.Steps[index].Title),
            };
            if (CurrentWidget.Steps[index].Subtitle is { } subtitle)
            {
                children.Add(new Padding(
                    EdgeInsets.Only(top: 2.0),
                    new AnimatedDefaultTextStyle(
                        style: SubtitleStyle(index),
                        duration: MaterialConstants.ThemeAnimationDuration,
                        curve: Curves.FastOutSlowIn,
                        child: subtitle)));
            }
            return new Column(
                crossAxisAlignment: CrossAxisAlignment.Start,
                mainAxisSize: MainAxisSize.Min,
                children: children);
        }

        private Widget BuildLabelText(int index)
        {
            if (CurrentWidget.Steps[index].Label is { } label)
            {
                return new AnimatedDefaultTextStyle(
                    style: LabelStyle(index),
                    duration: MaterialConstants.ThemeAnimationDuration,
                    child: label);
            }

            return SizedBox.Shrink();
        }

        private Widget BuildVerticalHeader(int index)
        {
            bool isActive = CurrentWidget.Steps[index].IsActive;
            bool isPreviousActive = index > 0 && CurrentWidget.Steps[index - 1].IsActive;
            return new Padding(
                EffectiveHeaderPadding,
                new Row(
                    children:
                    [
                        new Column(
                            children:
                            [
                                // Line parts are always added in order for the ink splash to
                                // flood the tips of the connector lines.
                                BuildLine(!IsFirst(index), isPreviousActive),
                                BuildIcon(index),
                                BuildLine(!IsLast(index), isActive),
                            ]),
                        new Expanded(
                            new Padding(
                                EdgeInsetsDirectional.Only(start: 12.0),
                                BuildHeaderText(index))),
                    ]));
        }

        private Widget BuildVerticalBody(int index)
        {
            double? marginLeft = StepIconMargin?.Resolve(TextDirection.Ltr).Left;
            double? marginRight = StepIconMargin?.Resolve(TextDirection.Ltr).Right;
            double? additionalMarginLeft = marginLeft != null ? marginLeft / 2.0 : null;
            double? additionalMarginRight = marginRight != null ? marginRight / 2.0 : null;

            EdgeInsetsGeometry effectiveVerticalContentPadding =
                (CurrentWidget.ContentPadding ?? KDefaultVerticalContentPadding)
                .Add(EdgeInsetsDirectional.Only(start: marginLeft ?? 0.0));

            return new Stack(
                children:
                [
                    new PositionedDirectional(
                        // When add margin to step icon,
                        // the line should be aligned to the center of the step icon.
                        start: 24.0 + (additionalMarginLeft ?? 0.0) + (additionalMarginRight ?? 0.0),
                        top: 0.0,
                        bottom: 0.0,
                        width: StepIconWidth ?? KStepSize,
                        child: new Center(
                            // The line is drawn from the center of the circle vertically until
                            // it reaches the bottom and then horizontally to the edge of the
                            // stepper.
                            child: new SizedBox(
                                width: !IsLast(index) ? CurrentWidget.ConnectorThickness ?? 1.0 : 0.0,
                                height: double.PositiveInfinity,
                                child: new ColoredBox(ConnectorColorFor(CurrentWidget.Steps[index].IsActive))))),
                    new AnimatedCrossFade(
                        firstChild: new SizedBox(width: double.PositiveInfinity, height: 0),
                        secondChild: new Padding(
                            // Adjust [controlsBuilder] padding so that the content is
                            // centered vertically.
                            effectiveVerticalContentPadding,
                            new Column(
                                children:
                                [
                                    new ClipRect(
                                        clipBehavior: CurrentWidget.ClipBehavior,
                                        child: CurrentWidget.Steps[index].Content),
                                    BuildVerticalControls(index),
                                ])),
                        firstCurve: new Interval(0.0, 0.6, Curves.FastOutSlowIn),
                        secondCurve: new Interval(0.4, 1.0, Curves.FastOutSlowIn),
                        sizeCurve: Curves.FastOutSlowIn,
                        crossFadeState: IsCurrent(index) ? CrossFadeState.ShowSecond : CrossFadeState.ShowFirst,
                        duration: MaterialConstants.ThemeAnimationDuration),
                ]);
        }

        private Widget BuildVertical()
        {
            var children = new List<Widget>(CurrentWidget.Steps.Count);
            for (int i = 0; i < CurrentWidget.Steps.Count; i += 1)
            {
                int index = i;
                children.Add(new Column(
                    key: _keys[index],
                    children:
                    [
                        new InkWell(
                            onTap: CurrentWidget.Steps[index].State != StepState.Disabled
                                ? () =>
                                {
                                    // In the vertical case we need to scroll to the newly tapped
                                    // step.
                                    _ = Scrollable.EnsureVisible(
                                        _keys[index].CurrentContext!,
                                        curve: Curves.FastOutSlowIn,
                                        duration: MaterialConstants.ThemeAnimationDuration);

                                    CurrentWidget.OnStepTapped?.Invoke(index);
                                }
                                : null,
                            canRequestFocus: CurrentWidget.Steps[index].State != StepState.Disabled,
                            child: BuildVerticalHeader(index)),
                        BuildVerticalBody(index),
                    ]));
            }

            return new ListView(
                controller: CurrentWidget.Controller,
                shrinkWrap: true,
                physics: CurrentWidget.Physics,
                children: children);
        }

        private Widget BuildHorizontal()
        {
            EdgeInsetsGeometry effectiveHorizontalContentPadding =
                CurrentWidget.ContentPadding ?? KDefaultHorizontalContentPadding;

            var children = new List<Widget>();
            for (int i = 0; i < CurrentWidget.Steps.Count; i += 1)
            {
                int index = i;
                var iconColumn = new List<Widget>();
                if (CurrentWidget.Steps[index].Label != null)
                {
                    iconColumn.Add(new SizedBox(height: 24.0));
                }
                iconColumn.Add(new Center(child: BuildIcon(index)));
                if (CurrentWidget.Steps[index].Label != null)
                {
                    iconColumn.Add(new SizedBox(height: 24.0, child: BuildLabelText(index)));
                }

                children.Add(new InkResponse(
                    onTap: CurrentWidget.Steps[index].State != StepState.Disabled
                        ? () => CurrentWidget.OnStepTapped?.Invoke(index)
                        : null,
                    canRequestFocus: CurrentWidget.Steps[index].State != StepState.Disabled,
                    child: new Row(
                        children:
                        [
                            new SizedBox(
                                height: IsLabel() ? 104.0 : 72.0,
                                child: new Column(
                                    mainAxisAlignment: MainAxisAlignment.Center,
                                    children: iconColumn)),
                            new Padding(
                                StepIconMargin ?? EdgeInsetsDirectional.Only(start: 12.0),
                                BuildHeaderText(index)),
                        ])));
                if (!IsLast(index))
                {
                    children.Add(new Expanded(
                        new Padding(
                            StepIconMargin ?? EdgeInsets.Symmetric(horizontal: 8.0),
                            new SizedBox(
                                height: CurrentWidget.Steps[index].StepStyle?.ConnectorThickness
                                        ?? CurrentWidget.ConnectorThickness
                                        ?? 1.0,
                                child: new ColoredBox(
                                    CurrentWidget.Steps[index].StepStyle?.ConnectorColor
                                    ?? ConnectorColorFor(CurrentWidget.Steps[index].IsActive))))));
                }
            }

            var stepPanels = new List<Widget>();
            for (int i = 0; i < CurrentWidget.Steps.Count; i += 1)
            {
                stepPanels.Add(new Visibility(
                    maintainState: true,
                    visible: i == CurrentWidget.CurrentStep,
                    child: new ClipRect(
                        clipBehavior: CurrentWidget.ClipBehavior,
                        child: CurrentWidget.Steps[i].Content)));
            }

            return new Column(
                children:
                [
                    new Material(
                        elevation: CurrentWidget.Elevation ?? 2,
                        child: new Padding(
                            EffectiveHeaderPadding,
                            new SizedBox(
                                height: StepIconHeight != null ? StepIconHeight.Value * HeightFactor : null,
                                child: new Row(children: children)))),
                    new Expanded(
                        new ListView(
                            controller: CurrentWidget.Controller,
                            physics: CurrentWidget.Physics,
                            padding: effectiveHorizontalContentPadding.Resolve(Directionality.Of(Context)),
                            children:
                            [
                                new AnimatedSize(
                                    curve: Curves.FastOutSlowIn,
                                    duration: MaterialConstants.ThemeAnimationDuration,
                                    child: new Column(
                                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                                        children: stepPanels)),
                                BuildVerticalControls(CurrentWidget.CurrentStep),
                            ])),
                ]);
        }

        public override Widget Build(BuildContext context)
        {
            DebugAssertions.Assert(MaterialDebug.DebugCheckHasMaterial(context));
            DebugAssertions.Assert(MaterialDebug.DebugCheckHasMaterialLocalizations(context));
            if (Constants.KDebugMode && context.FindAncestorWidgetOfExactType<Stepper>() != null)
            {
                throw new FlutterError(
                    "Steppers must not be nested.\n"
                    + "The material specification advises that one should avoid embedding "
                    + "steppers within steppers. "
                    + "https://material.io/archive/guidelines/components/steppers.html#steppers-usage");
            }

            return CurrentWidget.Type switch
            {
                StepperType.Vertical => BuildVertical(),
                StepperType.Horizontal => BuildHorizontal(),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
    }

    /// <summary>Paints a triangle whose base is the bottom of the provided rectangle.</summary>
    private sealed class TrianglePainter(Color color) : CustomPainter
    {
        public Color Color { get; } = color;

        public override bool? HitTest(Point position) => true; // Hitting the rectangle is fine enough.

        public override bool ShouldRepaint(CustomPainter oldDelegate) =>
            oldDelegate is not TrianglePainter old || old.Color != Color;

        public override void Paint(PaintingContext context, Size size)
        {
            double @base = size.Width;
            double halfBase = size.Width / 2.0;
            double height = size.Height;
            List<Point> points = [new Point(0.0, height), new Point(@base, height), new Point(halfBase, 0.0)];

            var path = new Plumix.UI.Path();
            path.AddPolygon(points, true);
            context.Canvas.DrawPath(path, new Paint { Color = Color });
        }
    }
}
