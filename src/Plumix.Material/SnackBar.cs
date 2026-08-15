using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/snack_bar.dart

/// Specify how a [SnackBar] was closed.
public enum SnackBarClosedReason
{
    /// The snack bar was closed after the user tapped a [SnackBarAction].
    Action,

    /// The snack bar was closed through a [SemanticsAction.dismiss].
    Dismiss,

    /// The snack bar was closed by a user's swipe.
    Swipe,

    /// The snack bar was closed by the `ScaffoldFeatureController` close callback or by calling
    /// `ScaffoldMessengerState.hideCurrentSnackBar` directly.
    Hide,

    /// The snack bar was closed by a call to `ScaffoldMessengerState.removeCurrentSnackBar`.
    Remove,

    /// The snack bar was closed because its timer expired.
    Timeout,
}

internal static class SnackBarConstants
{
    internal const double SingleLineVerticalPadding = 14.0;

    internal static readonly TimeSpan TransitionDuration = TimeSpan.FromMilliseconds(250);

    internal static readonly TimeSpan DisplayDuration = TimeSpan.FromMilliseconds(4000);

    internal static readonly Curve HeightCurve = Curves.FastOutSlowIn;

    internal static readonly Curve M3HeightCurve = Curves.EaseInOutQuart;

    internal static readonly Curve FadeInCurve = Curves.Interval(0.4, 1.0);

    internal static readonly Curve M3FadeInCurve = Curves.Interval(0.4, 0.6, Curves.EaseInCirc);

    internal static readonly Curve FadeOutCurve = Curves.Interval(0.72, 1.0, Curves.FastOutSlowIn);
}

/// A button for a [SnackBar], known as an "action".
///
/// Snack bar actions are always enabled. Instead of disabling a snack bar action, avoid including it
/// in the snack bar in the first place.
///
/// Snack bar actions can only be pressed once. Subsequent presses are ignored.
public sealed class SnackBarAction : StatefulWidget
{
    public SnackBarAction(
        string label,
        Action onPressed,
        WidgetStateColor? textColor = null,
        Color? disabledTextColor = null,
        WidgetStateColor? backgroundColor = null,
        Color? disabledBackgroundColor = null,
        Key? key = null) : base(key)
    {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        OnPressed = onPressed ?? throw new ArgumentNullException(nameof(onPressed));
        if (backgroundColor is { IsConstantColor: false } && disabledBackgroundColor is not null)
        {
            throw new ArgumentException(
                "disabledBackgroundColor must not be provided when background color is a WidgetStateColor",
                nameof(disabledBackgroundColor));
        }

        TextColor = textColor;
        DisabledTextColor = disabledTextColor;
        BackgroundColor = backgroundColor;
        DisabledBackgroundColor = disabledBackgroundColor;
    }

    public WidgetStateColor? TextColor { get; }

    public Color? DisabledTextColor { get; }

    public WidgetStateColor? BackgroundColor { get; }

    public Color? DisabledBackgroundColor { get; }

    public string Label { get; }

    public Action OnPressed { get; }

    public override State CreateState() => new SnackBarActionState();

    private sealed class SnackBarActionState : State
    {
        private bool _haveTriggeredAction;

        private SnackBarAction CurrentWidget => (SnackBarAction)StateWidget;

        private void HandlePressed()
        {
            if (_haveTriggeredAction)
            {
                return;
            }

            SetState(() => _haveTriggeredAction = true);
            CurrentWidget.OnPressed();
            ScaffoldMessenger.Of(Context).HideCurrentSnackBar(SnackBarClosedReason.Action);
        }

        public override Widget Build(BuildContext context)
        {
            SnackBarAction widget = CurrentWidget;
            SnackBarThemeData defaults = Theme.Of(context).UseMaterial3
                ? new SnackBarDefaultsM3(context)
                : new SnackBarDefaultsM2(context);
            SnackBarThemeData snackBarTheme = SnackBarTheme.Of(context);

            MaterialStateProperty<Color?> ResolveForegroundColor()
            {
                // Dart checks `x is WidgetStateColor` down the chain with `else if`, so a plain
                // widget color short-circuits the theme/defaults probes and falls through.
                if (widget.TextColor is { IsConstantColor: false } widgetStateColor)
                {
                    return Bridge(widgetStateColor);
                }

                if (widget.TextColor is null && snackBarTheme.ActionTextColor is { IsConstantColor: false } themeColor)
                {
                    return Bridge(themeColor);
                }

                if (widget.TextColor is null
                    && snackBarTheme.ActionTextColor is null
                    && defaults.ActionTextColor is { IsConstantColor: false } defaultColor)
                {
                    return Bridge(defaultColor);
                }

                return MaterialStateProperty<Color?>.ResolveWith(states => states.HasFlag(MaterialState.Disabled)
                    ? widget.DisabledTextColor
                      ?? snackBarTheme.DisabledActionTextColor
                      ?? defaults.DisabledActionTextColor!.Value
                    : (Color)(widget.TextColor ?? snackBarTheme.ActionTextColor ?? defaults.ActionTextColor!));
            }

            MaterialStateProperty<Color?> ResolveBackgroundColor()
            {
                if (widget.BackgroundColor is { IsConstantColor: false } widgetStateColor)
                {
                    return Bridge(widgetStateColor);
                }

                if (snackBarTheme.ActionBackgroundColor is { IsConstantColor: false } themeColor)
                {
                    return Bridge(themeColor);
                }

                return MaterialStateProperty<Color?>.ResolveWith(states => states.HasFlag(MaterialState.Disabled)
                    ? widget.DisabledBackgroundColor
                      ?? snackBarTheme.DisabledActionBackgroundColor
                      ?? Colors.Transparent
                    : (Color)(widget.BackgroundColor ?? snackBarTheme.ActionBackgroundColor
                        ?? new WidgetStateColor(Colors.Transparent)));
            }

            MaterialStateProperty<Color?> foregroundColor = ResolveForegroundColor();
            return new TextButton(
                child: new Text(widget.Label),
                onPressed: _haveTriggeredAction ? null : HandlePressed,
                style: TextButton.StyleFrom(overlayColor: foregroundColor.Resolve(MaterialState.None)) with
                {
                    ForegroundColor = foregroundColor,
                    BackgroundColor = ResolveBackgroundColor(),
                });
        }

        private static MaterialStateProperty<Color?> Bridge(WidgetStateColor color) =>
            MaterialStateProperty<Color?>.ResolveWith(states => color.Resolve(states));
    }
}

/// A lightweight message with an optional action which briefly displays at the bottom of the screen.
public sealed class SnackBar : StatefulWidget
{
    public SnackBar(
        Widget content,
        Color? backgroundColor = null,
        double? elevation = null,
        EdgeInsetsGeometry? margin = null,
        EdgeInsetsGeometry? padding = null,
        double? width = null,
        ShapeBorder? shape = null,
        HitTestBehavior? hitTestBehavior = null,
        SnackBarBehavior? behavior = null,
        SnackBarAction? action = null,
        double? actionOverflowThreshold = null,
        bool? showCloseIcon = null,
        Color? closeIconColor = null,
        TimeSpan? duration = null,
        bool? persist = null,
        Animation<double>? animation = null,
        Action? onVisible = null,
        DismissDirection? dismissDirection = null,
        Clip clipBehavior = Clip.HardEdge,
        Key? key = null) : base(key)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        if (elevation is < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(elevation));
        }

        if (width is not null && margin is not null)
        {
            throw new ArgumentException("Width and margin can not be used together", nameof(width));
        }

        if (actionOverflowThreshold is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actionOverflowThreshold),
                "Action overflow threshold must be between 0 and 1 inclusive");
        }

        BackgroundColor = backgroundColor;
        Elevation = elevation;
        Margin = margin;
        Padding = padding;
        Width = width;
        Shape = shape;
        HitTestBehavior = hitTestBehavior;
        Behavior = behavior;
        Action = action;
        ActionOverflowThreshold = actionOverflowThreshold;
        ShowCloseIcon = showCloseIcon;
        CloseIconColor = closeIconColor;
        Duration = duration ?? SnackBarConstants.DisplayDuration;
        Persist = persist ?? action is not null;
        Animation = animation;
        OnVisible = onVisible;
        DismissDirection = dismissDirection;
        ClipBehavior = clipBehavior;
    }

    /// The transition duration a `ScaffoldMessenger` drives a snack bar with.
    public static TimeSpan TransitionDuration => SnackBarConstants.TransitionDuration;

    /// The default `duration` — how long the bar stays up once it is fully shown.
    public static TimeSpan DefaultDuration => SnackBarConstants.DisplayDuration;

    public Widget Content { get; }

    public Color? BackgroundColor { get; }

    public double? Elevation { get; }

    public EdgeInsetsGeometry? Margin { get; }

    public EdgeInsetsGeometry? Padding { get; }

    public double? Width { get; }

    public ShapeBorder? Shape { get; }

    public HitTestBehavior? HitTestBehavior { get; }

    public SnackBarBehavior? Behavior { get; }

    public SnackBarAction? Action { get; }

    public double? ActionOverflowThreshold { get; }

    public bool? ShowCloseIcon { get; }

    public Color? CloseIconColor { get; }

    public TimeSpan Duration { get; }

    /// Whether the snack bar ignores its `Duration` timeout. Defaults to `Action != null`.
    public bool Persist { get; }

    public Animation<double>? Animation { get; }

    public Action? OnVisible { get; }

    public DismissDirection? DismissDirection { get; }

    public Clip ClipBehavior { get; }

    /// Creates the controller a `ScaffoldMessenger` drives its snack bars with.
    public static AnimationController CreateAnimationController(
        TimeSpan? duration = null,
        TimeSpan? reverseDuration = null)
    {
        return new AnimationController(
            duration: duration ?? SnackBarConstants.TransitionDuration,
            reverseDuration: reverseDuration);
    }

    /// Copies this snack bar with the animation the messenger drives it with.
    public SnackBar WithAnimation(Animation<double> newAnimation, Key? fallbackKey = null)
    {
        ArgumentNullException.ThrowIfNull(newAnimation);
        return new SnackBar(
            key: Key ?? fallbackKey,
            content: Content,
            backgroundColor: BackgroundColor,
            elevation: Elevation,
            margin: Margin,
            padding: Padding,
            width: Width,
            shape: Shape,
            hitTestBehavior: HitTestBehavior,
            behavior: Behavior,
            action: Action,
            actionOverflowThreshold: ActionOverflowThreshold,
            showCloseIcon: ShowCloseIcon,
            closeIconColor: CloseIconColor,
            duration: Duration,
            persist: Persist,
            animation: newAnimation,
            onVisible: OnVisible,
            dismissDirection: DismissDirection,
            clipBehavior: ClipBehavior);
    }

    public override State CreateState() => new SnackBarState();

    private sealed class SnackBarState : State
    {
        private readonly Key _dismissibleKey = new UniqueKey();
        private bool _wasVisible;
        private CurvedAnimation? _heightAnimation;
        private CurvedAnimation? _fadeInAnimation;
        private CurvedAnimation? _fadeInM3Animation;
        private CurvedAnimation? _fadeOutAnimation;
        private CurvedAnimation? _heightM3Animation;

        private SnackBar CurrentWidget => (SnackBar)StateWidget;

        public override void InitState()
        {
            base.InitState();
            CurrentWidget.Animation?.AddStatusListener(OnAnimationStatusChanged);
            SetAnimations();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            base.DidUpdateWidget(oldWidget);
            var oldSnackBar = (SnackBar)oldWidget;
            if (ReferenceEquals(CurrentWidget.Animation, oldSnackBar.Animation))
            {
                return;
            }

            oldSnackBar.Animation?.RemoveStatusListener(OnAnimationStatusChanged);
            CurrentWidget.Animation?.AddStatusListener(OnAnimationStatusChanged);
            DisposeAnimations();
            SetAnimations();
        }

        public override void Dispose()
        {
            CurrentWidget.Animation?.RemoveStatusListener(OnAnimationStatusChanged);
            DisposeAnimations();
            base.Dispose();
        }

        private void SetAnimations()
        {
            Animation<double>? parent = CurrentWidget.Animation;
            if (parent is null)
            {
                return;
            }

            _heightAnimation = new CurvedAnimation(parent, SnackBarConstants.HeightCurve);
            _fadeInAnimation = new CurvedAnimation(parent, SnackBarConstants.FadeInCurve);
            _fadeInM3Animation = new CurvedAnimation(parent, SnackBarConstants.M3FadeInCurve);
            // Material 3 has a height animation on entry but a direct fade out on exit, so both
            // exit-side curves are pinned by a zero threshold reverse curve.
            _fadeOutAnimation = new CurvedAnimation(
                parent,
                SnackBarConstants.FadeOutCurve,
                Curves.Threshold(0.0));
            _heightM3Animation = new CurvedAnimation(
                parent,
                SnackBarConstants.M3HeightCurve,
                Curves.Threshold(0.0));
        }

        private void DisposeAnimations()
        {
            _heightAnimation?.Dispose();
            _fadeInAnimation?.Dispose();
            _fadeInM3Animation?.Dispose();
            _fadeOutAnimation?.Dispose();
            _heightM3Animation?.Dispose();
            _heightAnimation = null;
            _fadeInAnimation = null;
            _fadeInM3Animation = null;
            _fadeOutAnimation = null;
            _heightM3Animation = null;
        }

        private void OnAnimationStatusChanged(AnimationStatus status)
        {
            if (status != AnimationStatus.Completed)
            {
                return;
            }

            if (CurrentWidget.OnVisible is not null && !_wasVisible)
            {
                CurrentWidget.OnVisible();
            }

            _wasVisible = true;
        }

        public override Widget Build(BuildContext context)
        {
            SnackBar widget = CurrentWidget;
            bool accessibleNavigation = MediaQuery.AccessibleNavigationOf(context);
            ThemeData theme = Theme.Of(context);
            ColorScheme colorScheme = theme.ColorScheme;
            SnackBarThemeData snackBarTheme = SnackBarTheme.Of(context);
            bool isThemeDark = theme.Brightness == Brightness.Dark;
            Color buttonColor = isThemeDark ? colorScheme.Primary : colorScheme.Secondary;
            SnackBarThemeData defaults = theme.UseMaterial3
                ? new SnackBarDefaultsM3(context)
                : new SnackBarDefaultsM2(context);

            // SnackBar uses a theme that is the opposite brightness from the surrounding theme, so
            // that the action button and content read against the inverted surface. Material 3
            // tokens are already inverted, so M3 keeps the ambient theme.
            var brightness = isThemeDark ? Brightness.Light : Brightness.Dark;
            ThemeData effectiveTheme = theme.UseMaterial3
                ? theme
                : theme with
                {
                    ColorScheme = new ColorScheme(
                        brightness: brightness,
                        primary: colorScheme.OnPrimary,
                        onPrimary: colorScheme.Primary,
                        secondary: buttonColor,
                        onSecondary: colorScheme.Secondary,
                        error: colorScheme.OnError,
                        onError: colorScheme.Error,
                        surface: colorScheme.OnSurface,
                        onSurface: colorScheme.Surface,
                        background: defaults.BackgroundColor,
                        onBackground: colorScheme.Background),
                };

            TextStyle? contentTextStyle = snackBarTheme.ContentTextStyle ?? defaults.ContentTextStyle;
            SnackBarBehavior snackBarBehavior = widget.Behavior ?? snackBarTheme.Behavior ?? defaults.Behavior!.Value;
            bool isFloatingSnackBar = snackBarBehavior == SnackBarBehavior.Floating;
            bool showCloseIcon = widget.ShowCloseIcon ?? snackBarTheme.ShowCloseIcon ?? defaults.ShowCloseIcon!.Value;
            double horizontalPadding = isFloatingSnackBar ? 16.0 : 24.0;
            EdgeInsetsGeometry padding = widget.Padding
                                         ?? EdgeInsetsDirectional.Only(
                                             start: horizontalPadding,
                                             end: widget.Action is not null || showCloseIcon
                                                 ? 0
                                                 : horizontalPadding);

            double? width = widget.Width ?? snackBarTheme.Width;
            ValidateBehavior(widget, snackBarTheme, snackBarBehavior, width);

            double actionHorizontalMargin =
                (widget.Padding?.Resolve(TextDirection.Ltr).Right ?? horizontalPadding) / 2;
            double iconHorizontalMargin =
                (widget.Padding?.Resolve(TextDirection.Ltr).Right ?? horizontalPadding) / 12.0;

            IconButton? iconButton = showCloseIcon
                ? new IconButton(
                    key: StandardComponentType.CloseButton.Key(),
                    icon: new Icon(Icons.Close),
                    iconSize: 24.0,
                    color: widget.CloseIconColor ?? snackBarTheme.CloseIconColor ?? defaults.CloseIconColor,
                    onPressed: () => ScaffoldMessenger.Of(context)
                        .HideCurrentSnackBar(SnackBarClosedReason.Dismiss),
                    tooltip: MaterialLocalizations.Of(context).CloseButtonTooltip)
                : null;

            // Calculate combined width of Action, Icon and their padding, if they are present.
            using var actionTextPainter = new TextPainter(
                text: new TextSpan(text: widget.Action?.Label ?? string.Empty, style: theme.TextTheme.LabelLarge),
                maxLines: 1,
                textDirection: TextDirection.Ltr);
            actionTextPainter.Layout();
            double actionAndIconWidth = actionTextPainter.Size.Width
                                        + (widget.Action is not null ? actionHorizontalMargin : 0)
                                        + (showCloseIcon ? iconButton!.IconSize ?? 0 + iconHorizontalMargin : 0);

            Thickness margin = widget.Margin?.Resolve(TextDirection.Ltr)
                               ?? snackBarTheme.InsetPadding
                               ?? defaults.InsetPadding!.Value;
            double snackBarWidth = widget.Width ?? MediaQuery.WidthOf(context) - (margin.Left + margin.Right);
            // Action and Icon will overflow to a new line if their width is greater than the width
            // of the SnackBar times the actionOverflowThreshold.
            double actionOverflowThreshold = widget.ActionOverflowThreshold
                                             ?? snackBarTheme.ActionOverflowThreshold
                                             ?? defaults.ActionOverflowThreshold!.Value;
            bool willOverflowAction = actionAndIconWidth / snackBarWidth > actionOverflowThreshold;

            var maybeActionAndIcon = new List<Widget>();
            if (widget.Action is not null)
            {
                maybeActionAndIcon.Add(new Padding(
                    new Thickness(actionHorizontalMargin, 0),
                    new TextButtonTheme(
                        data: new TextButtonThemeData(style: TextButton.StyleFrom(
                            foregroundColor: buttonColor,
                            padding: new Thickness(horizontalPadding, 0))),
                        child: widget.Action)));
            }

            if (showCloseIcon)
            {
                maybeActionAndIcon.Add(new Padding(new Thickness(iconHorizontalMargin, 0), iconButton!));
            }

            var rowChildren = new List<Widget>
            {
                new Expanded(new Padding(
                    widget.Padding is null
                        ? new Thickness(0, SnackBarConstants.SingleLineVerticalPadding)
                        : default,
                    new DefaultTextStyle(contentTextStyle!, widget.Content))),
            };

            if (!willOverflowAction)
            {
                rowChildren.AddRange(maybeActionAndIcon);
            }
            else
            {
                rowChildren.Add(new SizedBox(width: snackBarWidth * 0.4));
            }

            var wrapChildren = new List<Widget> { new Row(children: rowChildren) };
            if (willOverflowAction)
            {
                wrapChildren.Add(new Padding(
                    new Thickness(0, 0, 0, SnackBarConstants.SingleLineVerticalPadding),
                    new Row(mainAxisAlignment: MainAxisAlignment.End, children: maybeActionAndIcon)));
            }

            Widget snackBar = new Padding(padding, new Wrap(children: wrapChildren));
            if (!isFloatingSnackBar)
            {
                snackBar = new SafeArea(top: false, child: snackBar);
            }

            if (!accessibleNavigation && !theme.UseMaterial3)
            {
                snackBar = new FadeTransition(opacity: _fadeOutAnimation!, child: snackBar);
            }

            double elevation = widget.Elevation ?? snackBarTheme.Elevation ?? defaults.Elevation!.Value;
            Color backgroundColor = widget.BackgroundColor
                                    ?? snackBarTheme.BackgroundColor
                                    ?? defaults.BackgroundColor!.Value;
            ShapeBorder? shape = widget.Shape
                                 ?? snackBarTheme.Shape
                                 ?? (isFloatingSnackBar ? defaults.Shape : null);

            snackBar = new Material(
                shape: shape,
                elevation: elevation,
                color: backgroundColor,
                clipBehavior: widget.ClipBehavior,
                child: new Theme(effectiveTheme, snackBar));

            if (isFloatingSnackBar)
            {
                // If width is provided, do not include horizontal margins.
                snackBar = width is not null
                    ? new Padding(
                        new Thickness(0, margin.Top, 0, margin.Bottom),
                        new SizedBox(width: width, child: snackBar))
                    : new Padding(margin, snackBar);
                snackBar = new SafeArea(top: false, bottom: false, child: snackBar);
            }

            snackBar = new Semantics(
                container: true,
                liveRegion: true,
                onDismiss: () => ScaffoldMessenger.Of(context).RemoveCurrentSnackBar(SnackBarClosedReason.Dismiss),
                child: new Dismissible(
                    key: _dismissibleKey,
                    resizeDuration: null,
                    direction: widget.DismissDirection
                               ?? snackBarTheme.DismissDirection
                               ?? Plumix.Widgets.DismissDirection.Down,
                    behavior: widget.HitTestBehavior
                              ?? (widget.Margin is not null || snackBarTheme.InsetPadding is not null
                                  ? Plumix.Rendering.HitTestBehavior.DeferToChild
                                  : Plumix.Rendering.HitTestBehavior.Opaque),
                    onDismissed: _ => ScaffoldMessenger.Of(context)
                        .RemoveCurrentSnackBar(SnackBarClosedReason.Swipe),
                    child: snackBar));

            Widget snackBarTransition;
            if (accessibleNavigation)
            {
                snackBarTransition = snackBar;
            }
            else if (isFloatingSnackBar && !theme.UseMaterial3)
            {
                snackBarTransition = new FadeTransition(opacity: _fadeInAnimation!, child: snackBar);
            }
            else if (isFloatingSnackBar && theme.UseMaterial3)
            {
                snackBarTransition = new FadeTransition(
                    opacity: _fadeInM3Animation!,
                    child: new ValueListenableBuilder<double>(
                        valueListenable: _heightM3Animation!,
                        builder: (_, value, child) => new Align(
                            alignment: Alignment.BottomLeft,
                            heightFactor: value,
                            child: child),
                        child: snackBar));
            }
            else
            {
                snackBarTransition = new ValueListenableBuilder<double>(
                    valueListenable: _heightAnimation!,
                    builder: (_, value, child) => new Align(
                        alignment: AlignmentDirectional.TopStart,
                        heightFactor: value,
                        child: child),
                    child: snackBar);
            }

            // Dart derives the tag from `widget.content.toString()`, which is content-specific
            // because every Dart widget prints its fields. Plumix widgets inherit `object.ToString`,
            // so this collapses to one tag per content *type*; see `docs/ai/DIVERGENCES.md`.
            return new Hero(
                tag: $"<SnackBar Hero tag - {widget.Content}>",
                transitionOnUserGestures: true,
                child: new ClipRect(clipBehavior: widget.ClipBehavior, child: snackBarTransition));
        }

        private static void ValidateBehavior(
            SnackBar widget,
            SnackBarThemeData snackBarTheme,
            SnackBarBehavior behavior,
            double? width)
        {
            if (behavior == SnackBarBehavior.Floating)
            {
                return;
            }

            string Source() => widget.Behavior is not null
                ? "SnackBarBehavior.fixed was set in the SnackBar constructor."
                : snackBarTheme.Behavior is not null
                    ? "SnackBarBehavior.fixed was set by the inherited SnackBarThemeData."
                    : "SnackBarBehavior.fixed was set by default.";

            if (widget.Margin is not null)
            {
                throw new InvalidOperationException($"Margin can only be used with floating behavior. {Source()}");
            }

            if (width is not null)
            {
                throw new InvalidOperationException($"Width can only be used with floating behavior. {Source()}");
            }
        }
    }
}

// Dart parity source: material_ui/lib/src/snack_bar.dart (_SnackbarDefaultsM2).
internal sealed class SnackBarDefaultsM2 : SnackBarThemeData
{
    private readonly ThemeData _theme;
    private readonly ColorScheme _colors;

    internal SnackBarDefaultsM2(BuildContext context) : base(elevation: 6.0)
    {
        _theme = Theme.Of(context);
        _colors = _theme.ColorScheme;
    }

    public override Color? BackgroundColor => _theme.Brightness == Brightness.Light
        ? ColorUtilities.AlphaBlend(ColorUtilities.WithOpacity(_colors.OnSurface, 0.80), _colors.Surface)
        : _colors.OnSurface;

    public override TextStyle? ContentTextStyle => new ThemeData(
        useMaterial3: _theme.UseMaterial3,
        brightness: _theme.Brightness == Brightness.Light ? Brightness.Dark : Brightness.Light)
        .TextTheme.TitleMedium;

    public override SnackBarBehavior? Behavior => SnackBarBehavior.Fixed;

    public override WidgetStateColor? ActionTextColor => _colors.Secondary;

    public override Color? DisabledActionTextColor => ColorUtilities.WithOpacity(
        _colors.OnSurface,
        _theme.Brightness == Brightness.Light ? 0.38 : 0.3);

    public override ShapeBorder? Shape => new RoundedRectangleBorder(
        borderRadius: Plumix.Rendering.BorderRadius.Circular(4.0));

    public override Thickness? InsetPadding => new Thickness(15.0, 5.0, 15.0, 10.0);

    public override bool? ShowCloseIcon => false;

    public override Color? CloseIconColor => _colors.OnSurface;

    public override double? ActionOverflowThreshold => 0.25;
}

// Dart parity source: material_ui/lib/src/snack_bar.dart (_SnackbarDefaultsM3).
internal sealed class SnackBarDefaultsM3 : SnackBarThemeData
{
    private readonly ThemeData _theme;
    private readonly ColorScheme _colors;

    internal SnackBarDefaultsM3(BuildContext context)
    {
        _theme = Theme.Of(context);
        _colors = _theme.ColorScheme;
    }

    public override Color? BackgroundColor => _colors.InverseSurface;

    public override WidgetStateColor? ActionTextColor => WidgetStateColor.ResolveWith(
        _colors.InversePrimary,
        _ => _colors.InversePrimary);

    public override Color? DisabledActionTextColor => _colors.InversePrimary;

    public override TextStyle? ContentTextStyle =>
        _theme.TextTheme.BodyMedium.CopyWith(color: _colors.OnInverseSurface);

    public override double? Elevation => 6.0;

    public override ShapeBorder? Shape => new RoundedRectangleBorder(
        borderRadius: Plumix.Rendering.BorderRadius.Circular(4.0));

    public override SnackBarBehavior? Behavior => SnackBarBehavior.Fixed;

    public override Thickness? InsetPadding => new Thickness(15.0, 5.0, 15.0, 10.0);

    public override bool? ShowCloseIcon => false;

    public override Color? CloseIconColor => _colors.OnInverseSurface;

    public override double? ActionOverflowThreshold => 0.25;
}

internal static class ColorUtilities
{
    internal static Color WithOpacity(Color color, double opacity)
    {
        return Color.FromArgb(
            (byte)Math.Round(255 * Math.Clamp(opacity, 0, 1)),
            color.R,
            color.G,
            color.B);
    }

    internal static Color AlphaBlend(Color foreground, Color background)
    {
        double alpha = foreground.A / 255.0;
        byte Blend(byte foregroundChannel, byte backgroundChannel) =>
            (byte)Math.Round((foregroundChannel * alpha) + (backgroundChannel * (1 - alpha)));
        return Color.FromArgb(
            255,
            Blend(foreground.R, background.R),
            Blend(foreground.G, background.G),
            Blend(foreground.B, background.B));
    }
}
