using Avalonia;
using Avalonia.Media;
using Flutter;
using Flutter.Cupertino;
using Flutter.Foundation;
using Flutter.Rendering;
using Flutter.Widgets;

namespace Flutter.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/checkbox.dart (approximate)

public sealed class Checkbox : StatefulWidget
{
    private const double DefaultSplashRadius = 20.0;
    private readonly CheckboxType _checkboxType;

    private enum CheckboxType
    {
        Material,
        Adaptive
    }

    public const double Width = 18.0;

    public Checkbox(
        bool? value,
        Action<bool?>? onChanged,
        bool tristate = false,
        Color? activeColor = null,
        MaterialStateProperty<Color?>? fillColor = null,
        Color? checkColor = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        BorderRadius? shape = null,
        BorderSide? side = null,
        double? splashRadius = null,
        bool isError = false,
        string? semanticLabel = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        Key? key = null) : this(
            value: value,
            onChanged: onChanged,
            tristate: tristate,
            activeColor: activeColor,
            fillColor: fillColor,
            checkColor: checkColor,
            overlayColor: overlayColor,
            focusColor: focusColor,
            hoverColor: hoverColor,
            materialTapTargetSize: materialTapTargetSize,
            shape: shape,
            side: side,
            splashRadius: splashRadius,
            isError: isError,
            semanticLabel: semanticLabel,
            focusNode: focusNode,
            autofocus: autofocus,
            checkboxType: CheckboxType.Material,
            key: key)
    {
    }

    private Checkbox(
        bool? value,
        Action<bool?>? onChanged,
        bool tristate,
        Color? activeColor,
        MaterialStateProperty<Color?>? fillColor,
        Color? checkColor,
        MaterialStateProperty<Color?>? overlayColor,
        Color? focusColor,
        Color? hoverColor,
        MaterialTapTargetSize? materialTapTargetSize,
        BorderRadius? shape,
        BorderSide? side,
        double? splashRadius,
        bool isError,
        string? semanticLabel,
        FocusNode? focusNode,
        bool autofocus,
        CheckboxType checkboxType,
        Key? key = null) : base(key)
    {
        if (!tristate && value is null)
        {
            throw new ArgumentException("Checkbox value cannot be null when tristate is false.", nameof(value));
        }

        Value = value;
        OnChanged = onChanged;
        Tristate = tristate;
        ActiveColor = activeColor;
        FillColor = fillColor;
        CheckColor = checkColor;
        OverlayColor = overlayColor;
        FocusColor = focusColor;
        HoverColor = hoverColor;
        MaterialTapTargetSize = materialTapTargetSize;
        Shape = shape;
        Side = side;
        SplashRadius = splashRadius;
        IsError = isError;
        SemanticLabel = semanticLabel;
        FocusNode = focusNode;
        Autofocus = autofocus;
        _checkboxType = checkboxType;
    }

    public bool? Value { get; }

    public Action<bool?>? OnChanged { get; }

    public bool Tristate { get; }

    public Color? ActiveColor { get; }

    public MaterialStateProperty<Color?>? FillColor { get; }

    public Color? CheckColor { get; }

    public MaterialStateProperty<Color?>? OverlayColor { get; }

    public Color? FocusColor { get; }

    public Color? HoverColor { get; }

    public MaterialTapTargetSize? MaterialTapTargetSize { get; }

    public BorderRadius? Shape { get; }

    public BorderSide? Side { get; }

    public double? SplashRadius { get; }

    public bool IsError { get; }

    public string? SemanticLabel { get; }

    public FocusNode? FocusNode { get; }

    public bool Autofocus { get; }

    public static Checkbox Adaptive(
        bool? value,
        Action<bool?>? onChanged,
        bool tristate = false,
        Color? activeColor = null,
        MaterialStateProperty<Color?>? fillColor = null,
        Color? checkColor = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        BorderRadius? shape = null,
        BorderSide? side = null,
        double? splashRadius = null,
        bool isError = false,
        string? semanticLabel = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        Key? key = null)
    {
        return new Checkbox(
            value: value,
            onChanged: onChanged,
            tristate: tristate,
            activeColor: activeColor,
            fillColor: fillColor,
            checkColor: checkColor,
            overlayColor: overlayColor,
            focusColor: focusColor,
            hoverColor: hoverColor,
            materialTapTargetSize: materialTapTargetSize,
            shape: shape,
            side: side,
            splashRadius: splashRadius,
            isError: isError,
            semanticLabel: semanticLabel,
            focusNode: focusNode,
            autofocus: autofocus,
            checkboxType: CheckboxType.Adaptive,
            key: key);
    }

    public override State CreateState()
    {
        return new CheckboxState();
    }

    private sealed class CheckboxState : State
    {
        private AnimationController? _transitionController;
        private bool? _previousValue;
        private bool _isTransitioning;
        private double _transitionProgress = 1.0;

        private Checkbox CurrentWidget => (Checkbox)StateWidget;

        public override void InitState()
        {
            _previousValue = CurrentWidget.Value;
            _transitionController = new AnimationController(TimeSpan.FromMilliseconds(200))
            {
                Curve = Curves.EaseInOut
            };
            _transitionController.Changed += HandleTransitionTick;
            _transitionController.Completed += HandleTransitionCompleted;
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldCheckbox = (Checkbox)oldWidget;
            if (oldCheckbox.Value != CurrentWidget.Value)
            {
                _previousValue = oldCheckbox.Value;
                StartTransition();
            }
        }

        public override void Dispose()
        {
            if (_transitionController is null)
            {
                return;
            }

            _transitionController.Changed -= HandleTransitionTick;
            _transitionController.Completed -= HandleTransitionCompleted;
            _transitionController.Dispose();
            _transitionController = null;
        }

        public override Widget Build(BuildContext context)
        {
            var theme = Theme.Of(context);
            var checkboxTheme = CheckboxTheme.Of(context);
            if (IsAdaptiveCupertino(theme))
            {
                var adaptiveTapTargetSize = theme.Platform == TargetPlatform.MacOS
                    ? new Size(CupertinoCheckbox.Width, CupertinoCheckbox.Width)
                    : new Size(44, 44);
                return new CupertinoCheckbox(
                    value: CurrentWidget.Value,
                    tristate: CurrentWidget.Tristate,
                    onChanged: CurrentWidget.OnChanged,
                    activeColor: CurrentWidget.ActiveColor,
                    checkColor: CurrentWidget.CheckColor,
                    focusColor: CurrentWidget.FocusColor,
                    focusNode: CurrentWidget.FocusNode,
                    autofocus: CurrentWidget.Autofocus,
                    side: CurrentWidget.Side,
                    shape: CurrentWidget.Shape,
                    tapTargetSize: adaptiveTapTargetSize,
                    isDark: theme.Brightness == Brightness.Dark,
                    semanticLabel: CurrentWidget.SemanticLabel);
            }

            var enabled = CurrentWidget.OnChanged is not null;
            var isSelected = IsSelected(CurrentWidget.Value);
            var tapTargetSize = CurrentWidget.MaterialTapTargetSize
                                ?? checkboxTheme.MaterialTapTargetSize
                                ?? theme.MaterialTapTargetSize;
            var shape = CurrentWidget.Shape
                        ?? checkboxTheme.Shape
                        ?? Flutter.Rendering.BorderRadius.Circular(theme.UseMaterial3 ? 2 : 1);
            var splashRadius = ResolveSplashRadius(checkboxTheme);

            var style = new ButtonStyle(
                ForegroundColor: MaterialStateProperty<Color?>.ResolveWith(states => ResolveAnimatedCheckColor(theme, checkboxTheme, states)),
                BackgroundColor: MaterialStateProperty<Color?>.ResolveWith(states => ResolveAnimatedFillColor(theme, checkboxTheme, states)),
                ShadowColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
                SurfaceTintColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
                OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states => ResolveOverlayColor(theme, checkboxTheme, states)),
                SplashColor: null,
                Elevation: MaterialStateProperty<double?>.All(0),
                IconColor: MaterialStateProperty<Color?>.ResolveWith(states => ResolveAnimatedCheckColor(theme, checkboxTheme, states)),
                IconSize: MaterialStateProperty<double?>.All(14),
                Side: MaterialStateProperty<BorderSide?>.ResolveWith(states => ResolveAnimatedSide(theme, checkboxTheme, states)),
                Padding: MaterialStateProperty<Thickness?>.All(new Thickness(0)),
                Shape: MaterialStateProperty<BorderRadius?>.All(shape),
                MinimumSize: MaterialStateProperty<Size?>.All(new Size(Width, Width)),
                FixedSize: MaterialStateProperty<Size?>.All(new Size(Width, Width)),
                MaximumSize: MaterialStateProperty<Size?>.All(new Size(Width, Width)),
                Alignment: Alignment.Center,
                TapTargetSize: tapTargetSize);

            var indicatorColor = ResolveAnimatedCheckColor(
                theme,
                checkboxTheme,
                BuildStates(enabled, isSelected, CurrentWidget.IsError));

            return new MaterialButtonCore(
                child: new SizedBox(
                    width: Width,
                    height: Width,
                    child: new Center(child: BuildIndicator(indicatorColor))),
                onPressed: enabled ? HandleTap : null,
                style: style,
                focusNode: CurrentWidget.FocusNode,
                isSelected: isSelected,
                splashRadius: splashRadius,
                autofocus: CurrentWidget.Autofocus);
        }

        private void HandleTap()
        {
            CurrentWidget.OnChanged?.Invoke(NextValue());
        }

        private void StartTransition()
        {
            if (_transitionController is null)
            {
                return;
            }

            SetState(() =>
            {
                _isTransitioning = true;
                _transitionProgress = 0;
            });

            _transitionController.Forward(0);
        }

        private void HandleTransitionTick()
        {
            if (_transitionController is null || !_isTransitioning)
            {
                return;
            }

            SetState(() =>
            {
                _transitionProgress = Math.Clamp(_transitionController.Evaluate(), 0, 1);
            });
        }

        private void HandleTransitionCompleted()
        {
            SetState(() =>
            {
                _transitionProgress = 1;
                _isTransitioning = false;
                _previousValue = CurrentWidget.Value;
            });
        }

        private Widget BuildIndicator(Color color)
        {
            if (!_isTransitioning || _previousValue == CurrentWidget.Value)
            {
                return BuildStaticIndicator(CurrentWidget.Value, color);
            }

            var progress = Math.Clamp(_transitionProgress, 0, 1);
            var previousOpacity = Math.Clamp(1.0 - progress, 0, 1);
            var targetOpacity = progress;

            return new SizedBox(
                width: 14,
                height: 14,
                child: new Stack(
                    alignment: Alignment.Center,
                    children:
                    [
                        BuildFadedIndicator(_previousValue, color, previousOpacity),
                        BuildFadedIndicator(CurrentWidget.Value, color, targetOpacity)
                    ]));
        }

        private static Widget BuildStaticIndicator(bool? value, Color color)
        {
            return value switch
            {
                true => new Icon(Icons.Check, size: 14, color: color),
                null => new Container(width: 10, height: 2, color: color),
                _ => new SizedBox()
            };
        }

        private static Widget BuildFadedIndicator(bool? value, Color color, double opacity)
        {
            if (opacity <= 0.001 || value is false)
            {
                return new SizedBox();
            }

            return new Opacity(
                opacity,
                child: BuildStaticIndicator(value, color));
        }

        private bool? NextValue()
        {
            if (!CurrentWidget.Tristate)
            {
                return !(CurrentWidget.Value ?? false);
            }

            return CurrentWidget.Value switch
            {
                false => true,
                true => null,
                _ => false
            };
        }

        private Color ResolveAnimatedFillColor(ThemeData theme, CheckboxThemeData checkboxTheme, MaterialState states)
        {
            var effectiveStates = WithErrorState(states);
            var targetColor = ResolveFillColor(theme, checkboxTheme, effectiveStates);
            var previousColor = ResolveFillColor(
                theme,
                checkboxTheme,
                WithSelectedState(effectiveStates, IsSelected(_previousValue)));

            return LerpColor(previousColor, targetColor, ResolveTransitionFactor());
        }

        private Color ResolveFillColor(ThemeData theme, CheckboxThemeData checkboxTheme, MaterialState states)
        {
            var widgetFill = CurrentWidget.FillColor?.Resolve(states);
            if (widgetFill.HasValue)
            {
                return widgetFill.Value;
            }

            if (!states.HasFlag(MaterialState.Disabled)
                && states.HasFlag(MaterialState.Selected)
                && CurrentWidget.ActiveColor.HasValue)
            {
                return CurrentWidget.ActiveColor.Value;
            }

            var themedFill = checkboxTheme.FillColor?.Resolve(states);
            if (themedFill.HasValue)
            {
                return themedFill.Value;
            }

            return ResolveDefaultFillColor(theme, states);
        }

        private Color ResolveAnimatedCheckColor(ThemeData theme, CheckboxThemeData checkboxTheme, MaterialState states)
        {
            var effectiveStates = WithErrorState(states);
            var targetColor = ResolveCheckColor(theme, checkboxTheme, effectiveStates);
            var previousColor = ResolveCheckColor(
                theme,
                checkboxTheme,
                WithSelectedState(effectiveStates, IsSelected(_previousValue)));

            return LerpColor(previousColor, targetColor, ResolveTransitionFactor());
        }

        private Color ResolveCheckColor(ThemeData theme, CheckboxThemeData checkboxTheme, MaterialState states)
        {
            if (CurrentWidget.CheckColor.HasValue)
            {
                return CurrentWidget.CheckColor.Value;
            }

            var themedCheck = checkboxTheme.CheckColor?.Resolve(states);
            if (themedCheck.HasValue)
            {
                return themedCheck.Value;
            }

            return ResolveDefaultCheckColor(theme, states);
        }

        private Color? ResolveOverlayColor(ThemeData theme, CheckboxThemeData checkboxTheme, MaterialState states)
        {
            states = WithErrorState(states);
            if (states.HasFlag(MaterialState.Disabled))
            {
                return null;
            }

            var widgetOverlay = CurrentWidget.OverlayColor?.Resolve(states);
            if (widgetOverlay.HasValue)
            {
                return widgetOverlay.Value;
            }

            if (states.HasFlag(MaterialState.Hovered) && CurrentWidget.HoverColor.HasValue)
            {
                return CurrentWidget.HoverColor.Value;
            }

            if (states.HasFlag(MaterialState.Focused) && CurrentWidget.FocusColor.HasValue)
            {
                return CurrentWidget.FocusColor.Value;
            }

            if (states.HasFlag(MaterialState.Pressed) && CurrentWidget.ActiveColor.HasValue)
            {
                var pressedOpacity = theme.UseMaterial3 ? 0.10 : 0.12;
                return MaterialButtonCore.ApplyOpacity(CurrentWidget.ActiveColor.Value, pressedOpacity);
            }

            var themedOverlay = checkboxTheme.OverlayColor?.Resolve(states);
            if (themedOverlay.HasValue)
            {
                return themedOverlay.Value;
            }

            return ResolveDefaultOverlayColor(theme, states);
        }

        private BorderSide? ResolveAnimatedSide(ThemeData theme, CheckboxThemeData checkboxTheme, MaterialState states)
        {
            var effectiveStates = WithErrorState(states);
            var targetSide = ResolveSide(theme, checkboxTheme, effectiveStates);
            var previousSide = ResolveSide(
                theme,
                checkboxTheme,
                WithSelectedState(effectiveStates, IsSelected(_previousValue)));

            return LerpSide(previousSide, targetSide, ResolveTransitionFactor());
        }

        private BorderSide? ResolveSide(ThemeData theme, CheckboxThemeData checkboxTheme, MaterialState states)
        {
            if (CurrentWidget.Side.HasValue)
            {
                return states.HasFlag(MaterialState.Selected) ? null : CurrentWidget.Side.Value;
            }

            var themedSide = checkboxTheme.Side?.Resolve(states);
            if (themedSide.HasValue)
            {
                return themedSide.Value;
            }

            return ResolveDefaultSide(theme, states);
        }

        private double ResolveSplashRadius(CheckboxThemeData checkboxTheme)
        {
            var resolved = CurrentWidget.SplashRadius
                           ?? checkboxTheme.SplashRadius
                           ?? DefaultSplashRadius;

            if (double.IsNaN(resolved) || double.IsInfinity(resolved) || resolved <= 0)
            {
                return DefaultSplashRadius;
            }

            return resolved;
        }

        private double ResolveTransitionFactor()
        {
            if (!_isTransitioning || _previousValue == CurrentWidget.Value)
            {
                return 1;
            }

            return Math.Clamp(_transitionProgress, 0, 1);
        }

        private static Color ResolveDefaultFillColor(ThemeData theme, MaterialState states)
        {
            if (states.HasFlag(MaterialState.Disabled))
            {
                return states.HasFlag(MaterialState.Selected)
                    ? MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.38)
                    : Colors.Transparent;
            }

            if (states.HasFlag(MaterialState.Selected))
            {
                if (theme.UseMaterial3 && states.HasFlag(MaterialState.Error))
                {
                    return theme.ErrorColor;
                }

                return theme.PrimaryColor;
            }

            return Colors.Transparent;
        }

        private static Color ResolveDefaultCheckColor(ThemeData theme, MaterialState states)
        {
            if (states.HasFlag(MaterialState.Disabled))
            {
                return states.HasFlag(MaterialState.Selected)
                    ? (theme.UseMaterial3 ? theme.CanvasColor : Colors.White)
                    : Colors.Transparent;
            }

            if (states.HasFlag(MaterialState.Selected))
            {
                if (theme.UseMaterial3 && states.HasFlag(MaterialState.Error))
                {
                    return theme.OnErrorColor;
                }

                return theme.OnPrimaryColor;
            }

            return Colors.Transparent;
        }

        private static Color? ResolveDefaultOverlayColor(ThemeData theme, MaterialState states)
        {
            if (!theme.UseMaterial3)
            {
                if (states.HasFlag(MaterialState.Pressed) || states.HasFlag(MaterialState.Focused))
                {
                    return MaterialButtonCore.ApplyOpacity(theme.PrimaryColor, 0.12);
                }

                if (states.HasFlag(MaterialState.Hovered))
                {
                    return MaterialButtonCore.ApplyOpacity(theme.PrimaryColor, 0.08);
                }

                return null;
            }

            if (states.HasFlag(MaterialState.Error))
            {
                if (states.HasFlag(MaterialState.Pressed) || states.HasFlag(MaterialState.Focused))
                {
                    return MaterialButtonCore.ApplyOpacity(theme.ErrorColor, 0.10);
                }

                if (states.HasFlag(MaterialState.Hovered))
                {
                    return MaterialButtonCore.ApplyOpacity(theme.ErrorColor, 0.08);
                }
            }

            if (states.HasFlag(MaterialState.Selected))
            {
                if (states.HasFlag(MaterialState.Pressed))
                {
                    return MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.10);
                }

                if (states.HasFlag(MaterialState.Hovered))
                {
                    return MaterialButtonCore.ApplyOpacity(theme.PrimaryColor, 0.08);
                }

                if (states.HasFlag(MaterialState.Focused))
                {
                    return MaterialButtonCore.ApplyOpacity(theme.PrimaryColor, 0.10);
                }

                return null;
            }

            if (states.HasFlag(MaterialState.Pressed))
            {
                return MaterialButtonCore.ApplyOpacity(theme.PrimaryColor, 0.10);
            }

            if (states.HasFlag(MaterialState.Hovered))
            {
                return MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.08);
            }

            if (states.HasFlag(MaterialState.Focused))
            {
                return MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.10);
            }

            return null;
        }

        private static BorderSide ResolveDefaultSide(ThemeData theme, MaterialState states)
        {
            if (states.HasFlag(MaterialState.Disabled))
            {
                if (states.HasFlag(MaterialState.Selected))
                {
                    return new BorderSide(Colors.Transparent, theme.UseMaterial3 ? 0 : 2);
                }

                return new BorderSide(
                    MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.38),
                    2);
            }

            if (states.HasFlag(MaterialState.Selected))
            {
                return new BorderSide(Colors.Transparent, theme.UseMaterial3 ? 0 : 2);
            }

            if (theme.UseMaterial3)
            {
                if (states.HasFlag(MaterialState.Error))
                {
                    return new BorderSide(theme.ErrorColor, 2);
                }

                if (states.HasFlag(MaterialState.Pressed)
                    || states.HasFlag(MaterialState.Hovered)
                    || states.HasFlag(MaterialState.Focused))
                {
                    return new BorderSide(theme.OnSurfaceColor, 2);
                }

                return new BorderSide(theme.OnSurfaceVariantColor, 2);
            }

            return new BorderSide(
                MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.60),
                2);
        }

        private bool IsAdaptiveCupertino(ThemeData theme)
        {
            if (CurrentWidget._checkboxType != CheckboxType.Adaptive)
            {
                return false;
            }

            return theme.Platform is TargetPlatform.IOS or TargetPlatform.MacOS;
        }

        private static MaterialState BuildStates(bool enabled, bool selected, bool isError)
        {
            var states = enabled
                ? MaterialState.None
                : MaterialState.Disabled;

            if (selected)
            {
                states |= MaterialState.Selected;
            }

            if (isError)
            {
                states |= MaterialState.Error;
            }

            return states;
        }

        private static MaterialState WithSelectedState(MaterialState states, bool selected)
        {
            return selected
                ? states | MaterialState.Selected
                : states & ~MaterialState.Selected;
        }

        private MaterialState WithErrorState(MaterialState states)
        {
            return CurrentWidget.IsError
                ? states | MaterialState.Error
                : states & ~MaterialState.Error;
        }

        private static bool IsSelected(bool? value)
        {
            return value ?? true;
        }

        private static BorderSide? LerpSide(BorderSide? from, BorderSide? to, double t)
        {
            var clamped = Math.Clamp(t, 0, 1);
            if (clamped <= 0.001)
            {
                return from;
            }

            if (clamped >= 0.999)
            {
                return to;
            }

            if (from is null && to is null)
            {
                return null;
            }

            var fromSide = from ?? new BorderSide(Colors.Transparent, 0);
            var toSide = to ?? new BorderSide(Colors.Transparent, 0);
            var width = fromSide.Width + ((toSide.Width - fromSide.Width) * clamped);
            var color = LerpColor(fromSide.Color, toSide.Color, clamped);
            return new BorderSide(color, width);
        }

        private static Color LerpColor(Color from, Color to, double t)
        {
            var clamped = Math.Clamp(t, 0, 1);
            byte LerpByte(byte start, byte end)
            {
                return (byte)Math.Clamp((int)(start + ((end - start) * clamped)), 0, 255);
            }

            return Color.FromArgb(
                LerpByte(from.A, to.A),
                LerpByte(from.R, to.R),
                LerpByte(from.G, to.G),
                LerpByte(from.B, to.B));
        }
    }
}
