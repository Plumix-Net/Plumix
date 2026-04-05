using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Flutter.Foundation;
using Flutter.Rendering;
using Flutter.Widgets;

namespace Flutter.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/radio.dart (approximate)

public sealed class Radio<T> : StatefulWidget
{
    private const double DefaultSplashRadius = 20.0;
    private const double DefaultInnerRadius = 4.5;

    public const double Width = 20.0;

    public Radio(
        T value,
        T? groupValue,
        Action<T?>? onChanged,
        bool toggleable = false,
        Color? activeColor = null,
        MaterialStateProperty<Color?>? fillColor = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        MaterialStateProperty<Color?>? backgroundColor = null,
        BorderSide? side = null,
        MaterialStateProperty<double?>? innerRadius = null,
        double? splashRadius = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        Key? key = null) : base(key)
    {
        Value = value;
        GroupValue = groupValue;
        OnChanged = onChanged;
        Toggleable = toggleable;
        ActiveColor = activeColor;
        FillColor = fillColor;
        OverlayColor = overlayColor;
        FocusColor = focusColor;
        HoverColor = hoverColor;
        MaterialTapTargetSize = materialTapTargetSize;
        BackgroundColor = backgroundColor;
        Side = side;
        InnerRadius = innerRadius;
        SplashRadius = splashRadius;
        FocusNode = focusNode;
        Autofocus = autofocus;
    }

    public T Value { get; }

    public T? GroupValue { get; }

    public Action<T?>? OnChanged { get; }

    public bool Toggleable { get; }

    public Color? ActiveColor { get; }

    public MaterialStateProperty<Color?>? FillColor { get; }

    public MaterialStateProperty<Color?>? OverlayColor { get; }

    public Color? FocusColor { get; }

    public Color? HoverColor { get; }

    public MaterialTapTargetSize? MaterialTapTargetSize { get; }

    public MaterialStateProperty<Color?>? BackgroundColor { get; }

    public BorderSide? Side { get; }

    public MaterialStateProperty<double?>? InnerRadius { get; }

    public double? SplashRadius { get; }

    public FocusNode? FocusNode { get; }

    public bool Autofocus { get; }

    public override State CreateState()
    {
        return new RadioState();
    }

    private sealed class RadioState : State
    {
        private Radio<T> CurrentWidget => (Radio<T>)StateWidget;

        public override Widget Build(BuildContext context)
        {
            var theme = Theme.Of(context);
            var radioTheme = RadioTheme.Of(context);
            var enabled = CurrentWidget.OnChanged is not null;
            var selected = IsSelected();
            var selectedStates = BuildStates(enabled, selected: true);
            var tapTargetSize = CurrentWidget.MaterialTapTargetSize
                                ?? radioTheme.MaterialTapTargetSize
                                ?? theme.MaterialTapTargetSize;
            var splashRadius = ResolveSplashRadius(radioTheme);
            var innerRadius = ResolveInnerRadius(radioTheme, selectedStates);
            var shape = Flutter.Rendering.BorderRadius.Circular(Width / 2);

            var style = new ButtonStyle(
                ForegroundColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
                BackgroundColor: MaterialStateProperty<Color?>.ResolveWith(states => ResolveBackgroundColor(theme, radioTheme, states)),
                ShadowColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
                SurfaceTintColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
                OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states => ResolveOverlayColor(theme, radioTheme, states)),
                SplashColor: null,
                Elevation: MaterialStateProperty<double?>.All(0),
                IconColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
                IconSize: MaterialStateProperty<double?>.All(18),
                Side: MaterialStateProperty<BorderSide?>.ResolveWith(states => ResolveSide(theme, radioTheme, states)),
                Padding: MaterialStateProperty<Thickness?>.All(new Thickness(0)),
                Shape: MaterialStateProperty<BorderRadius?>.All(shape),
                MinimumSize: MaterialStateProperty<Size?>.All(new Size(Width, Width)),
                FixedSize: MaterialStateProperty<Size?>.All(new Size(Width, Width)),
                MaximumSize: MaterialStateProperty<Size?>.All(new Size(Width, Width)),
                Alignment: Alignment.Center,
                TapTargetSize: tapTargetSize);

            var dotColor = ResolveFillColor(theme, radioTheme, selectedStates);

            return new MaterialButtonCore(
                child: new SizedBox(
                    width: Width,
                    height: Width,
                    child: new Center(
                        child: selected
                            ? new Container(
                                width: innerRadius * 2,
                                height: innerRadius * 2,
                                decoration: new BoxDecoration(
                                    Color: dotColor,
                                    BorderRadius: Flutter.Rendering.BorderRadius.Circular(innerRadius)))
                            : new SizedBox())),
                onPressed: enabled ? HandleTap : null,
                style: style,
                focusNode: CurrentWidget.FocusNode,
                isSelected: selected,
                splashRadius: splashRadius,
                autofocus: CurrentWidget.Autofocus);
        }

        private void HandleTap()
        {
            if (CurrentWidget.OnChanged is null)
            {
                return;
            }

            if (IsSelected())
            {
                if (CurrentWidget.Toggleable)
                {
                    CurrentWidget.OnChanged.Invoke(default);
                }

                return;
            }

            CurrentWidget.OnChanged.Invoke(CurrentWidget.Value);
        }

        private bool IsSelected()
        {
            return EqualityComparer<T?>.Default.Equals(CurrentWidget.Value, CurrentWidget.GroupValue);
        }

        private double ResolveInnerRadius(RadioThemeData radioTheme, MaterialState states)
        {
            var resolved = CurrentWidget.InnerRadius?.Resolve(states)
                           ?? radioTheme.InnerRadius?.Resolve(states)
                           ?? DefaultInnerRadius;

            if (double.IsNaN(resolved) || double.IsInfinity(resolved))
            {
                return DefaultInnerRadius;
            }

            return Math.Clamp(resolved, 0, Width / 2);
        }

        private double ResolveSplashRadius(RadioThemeData radioTheme)
        {
            var resolved = CurrentWidget.SplashRadius
                           ?? radioTheme.SplashRadius
                           ?? DefaultSplashRadius;

            if (double.IsNaN(resolved) || double.IsInfinity(resolved) || resolved <= 0)
            {
                return DefaultSplashRadius;
            }

            return resolved;
        }

        private Color ResolveFillColor(ThemeData theme, RadioThemeData radioTheme, MaterialState states)
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

            var themeFill = radioTheme.FillColor?.Resolve(states);
            if (themeFill.HasValue)
            {
                return themeFill.Value;
            }

            return ResolveDefaultFillColor(theme, states);
        }

        private Color ResolveBackgroundColor(ThemeData theme, RadioThemeData radioTheme, MaterialState states)
        {
            var widgetBackground = CurrentWidget.BackgroundColor?.Resolve(states);
            if (widgetBackground.HasValue)
            {
                return widgetBackground.Value;
            }

            var themeBackground = radioTheme.BackgroundColor?.Resolve(states);
            if (themeBackground.HasValue)
            {
                return themeBackground.Value;
            }

            return Colors.Transparent;
        }

        private BorderSide ResolveSide(ThemeData theme, RadioThemeData radioTheme, MaterialState states)
        {
            if (CurrentWidget.Side.HasValue && !states.HasFlag(MaterialState.Selected))
            {
                return CurrentWidget.Side.Value;
            }

            if (radioTheme.Side.HasValue && !states.HasFlag(MaterialState.Selected))
            {
                return radioTheme.Side.Value;
            }

            return new BorderSide(ResolveFillColor(theme, radioTheme, states), 2);
        }

        private Color? ResolveOverlayColor(ThemeData theme, RadioThemeData radioTheme, MaterialState states)
        {
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

            var themeOverlay = radioTheme.OverlayColor?.Resolve(states);
            if (themeOverlay.HasValue)
            {
                return themeOverlay.Value;
            }

            return ResolveDefaultOverlayColor(theme, states);
        }

        private static Color ResolveDefaultFillColor(ThemeData theme, MaterialState states)
        {
            if (theme.UseMaterial3)
            {
                if (states.HasFlag(MaterialState.Selected))
                {
                    return states.HasFlag(MaterialState.Disabled)
                        ? MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.38)
                        : theme.PrimaryColor;
                }

                if (states.HasFlag(MaterialState.Disabled))
                {
                    return MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.38);
                }

                if (states.HasFlag(MaterialState.Pressed)
                    || states.HasFlag(MaterialState.Hovered)
                    || states.HasFlag(MaterialState.Focused))
                {
                    return theme.OnSurfaceColor;
                }

                return theme.OnSurfaceVariantColor;
            }

            if (states.HasFlag(MaterialState.Disabled))
            {
                return MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.38);
            }

            if (states.HasFlag(MaterialState.Selected))
            {
                return theme.PrimaryColor;
            }

            return MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.54);
        }

        private static Color? ResolveDefaultOverlayColor(ThemeData theme, MaterialState states)
        {
            if (!theme.UseMaterial3)
            {
                var baseColor = states.HasFlag(MaterialState.Selected)
                    ? theme.PrimaryColor
                    : MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.54);
                if (states.HasFlag(MaterialState.Pressed))
                {
                    return MaterialButtonCore.ApplyOpacity(baseColor, 0.12);
                }

                if (states.HasFlag(MaterialState.Hovered))
                {
                    return MaterialButtonCore.ApplyOpacity(baseColor, 0.08);
                }

                if (states.HasFlag(MaterialState.Focused))
                {
                    return MaterialButtonCore.ApplyOpacity(baseColor, 0.12);
                }

                return null;
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

        private static MaterialState BuildStates(bool enabled, bool selected)
        {
            var states = enabled
                ? MaterialState.None
                : MaterialState.Disabled;

            if (selected)
            {
                states |= MaterialState.Selected;
            }

            return states;
        }
    }
}
