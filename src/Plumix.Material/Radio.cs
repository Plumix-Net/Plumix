using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/radio.dart (approximate)

public sealed class Radio<T> : StatefulWidget
{
    private const double DefaultSplashRadius = 20.0;
    private const double DefaultInnerRadius = 4.5;
    private readonly RadioType _radioType;

    private enum RadioType
    {
        Material,
        Adaptive
    }

    public const double Width = 20.0;

    public Radio(
        T value,
        T? groupValue = default,
        Action<T?>? onChanged = null,
        bool toggleable = false,
        MouseCursor? mouseCursor = null,
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
        bool? enabled = null,
        RadioGroupRegistry<T>? groupRegistry = null,
        Key? key = null)
        : this(
            value: value,
            groupValue: groupValue,
            onChanged: onChanged,
            toggleable: toggleable,
            mouseCursor: mouseCursor,
            activeColor: activeColor,
            fillColor: fillColor,
            overlayColor: overlayColor,
            focusColor: focusColor,
            hoverColor: hoverColor,
            materialTapTargetSize: materialTapTargetSize,
            backgroundColor: backgroundColor,
            side: side,
            innerRadius: innerRadius,
            splashRadius: splashRadius,
            focusNode: focusNode,
            autofocus: autofocus,
            enabled: enabled,
            groupRegistry: groupRegistry,
            useCupertinoCheckmarkStyle: false,
            radioType: RadioType.Material,
            key: key)
    {
    }

    private Radio(
        T value,
        T? groupValue,
        Action<T?>? onChanged,
        bool toggleable,
        MouseCursor? mouseCursor,
        Color? activeColor,
        MaterialStateProperty<Color?>? fillColor,
        MaterialStateProperty<Color?>? overlayColor,
        Color? focusColor,
        Color? hoverColor,
        MaterialTapTargetSize? materialTapTargetSize,
        MaterialStateProperty<Color?>? backgroundColor,
        BorderSide? side,
        MaterialStateProperty<double?>? innerRadius,
        double? splashRadius,
        FocusNode? focusNode,
        bool autofocus,
        bool? enabled,
        RadioGroupRegistry<T>? groupRegistry,
        bool useCupertinoCheckmarkStyle,
        RadioType radioType,
        Key? key = null) : base(key)
    {
        Value = value;
        GroupValue = groupValue;
        OnChanged = onChanged;
        Toggleable = toggleable;
        MouseCursor = mouseCursor;
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
        Enabled = enabled;
        GroupRegistry = groupRegistry;
        UseCupertinoCheckmarkStyle = useCupertinoCheckmarkStyle;
        _radioType = radioType;
    }

    public T Value { get; }

    public T? GroupValue { get; }

    public Action<T?>? OnChanged { get; }

    public bool Toggleable { get; }

    public MouseCursor? MouseCursor { get; }

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

    public bool? Enabled { get; }

    public RadioGroupRegistry<T>? GroupRegistry { get; }

    public bool UseCupertinoCheckmarkStyle { get; }

    public static Radio<T> Adaptive(
        T value,
        T? groupValue = default,
        Action<T?>? onChanged = null,
        bool toggleable = false,
        MouseCursor? mouseCursor = null,
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
        bool useCupertinoCheckmarkStyle = false,
        bool? enabled = null,
        RadioGroupRegistry<T>? groupRegistry = null,
        Key? key = null)
    {
        return new Radio<T>(
            value: value,
            groupValue: groupValue,
            onChanged: onChanged,
            toggleable: toggleable,
            mouseCursor: mouseCursor,
            activeColor: activeColor,
            fillColor: fillColor,
            overlayColor: overlayColor,
            focusColor: focusColor,
            hoverColor: hoverColor,
            materialTapTargetSize: materialTapTargetSize,
            backgroundColor: backgroundColor,
            side: side,
            innerRadius: innerRadius,
            splashRadius: splashRadius,
            focusNode: focusNode,
            autofocus: autofocus,
            enabled: enabled,
            groupRegistry: groupRegistry,
            useCupertinoCheckmarkStyle: useCupertinoCheckmarkStyle,
            radioType: RadioType.Adaptive,
            key: key);
    }

    public override State CreateState()
    {
        return new RadioState();
    }

    private sealed class RadioState : State, RadioClient<T>
    {
        private FocusNode? _focusNode;
        private bool _ownsFocusNode;
        private RadioGroupRegistry<T>? _registry;
        private LegacyRadioRegistry? _legacyRegistry;
        private bool _registryEnablesInteraction;

        private Radio<T> CurrentWidget => (Radio<T>)StateWidget;

        public bool Tristate => CurrentWidget.Toggleable;

        public T RadioValue => CurrentWidget.Value;

        public bool Enabled => ResolveEnabled();

        public FocusNode FocusNode => _focusNode!;

        public override void InitState()
        {
            AttachFocusNode(CurrentWidget.FocusNode);
        }

        public override void DidChangeDependencies()
        {
            SyncRegistry();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldRadio = (Radio<T>)oldWidget;
            if (!ReferenceEquals(oldRadio.FocusNode, CurrentWidget.FocusNode))
            {
                SetRegistry(null);
                DetachFocusNode(disposeOwned: true);
                AttachFocusNode(CurrentWidget.FocusNode);
            }

            SyncRegistry();
        }

        public override Widget Build(BuildContext context)
        {
            SyncRegistry();
            bool enabled = ResolveEnabled();
            if (CurrentWidget.Enabled == true && _registry is null)
            {
                throw new InvalidOperationException(
                    "Radio is enabled but has no onChanged callback or group registry.");
            }

            var theme = Theme.Of(context);
            if (IsAdaptiveCupertino(theme))
            {
                return new CupertinoRadio<T>(
                    value: CurrentWidget.Value,
                    groupValue: _registry is null ? default : _registry.GroupValue,
                    onChanged: enabled ? _registry?.OnChanged : null,
                    toggleable: CurrentWidget.Toggleable,
                    activeColor: CurrentWidget.ActiveColor,
                    focusColor: CurrentWidget.FocusColor,
                    useCheckmarkStyle: CurrentWidget.UseCupertinoCheckmarkStyle,
                    focusNode: _focusNode,
                    autofocus: CurrentWidget.Autofocus,
                    isDark: theme.Brightness == Brightness.Dark);
            }

            var radioTheme = RadioTheme.Of(context);
            bool selected = IsSelected();
            var selectedStates = BuildStates(enabled, selected: true);
            var tapTargetSize = CurrentWidget.MaterialTapTargetSize
                                ?? radioTheme.MaterialTapTargetSize
                                ?? theme.MaterialTapTargetSize;
            double splashRadius = ResolveSplashRadius(radioTheme);
            double innerRadius = ResolveInnerRadius(radioTheme, selectedStates);
            var shape = Plumix.Rendering.BorderRadius.Circular(Width / 2);

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
                                    BorderRadius: Plumix.Rendering.BorderRadius.Circular(innerRadius)))
                            : new SizedBox())),
                onPressed: enabled ? HandleTap : null,
                style: style,
                focusNode: _focusNode,
                mouseCursor: CurrentWidget.MouseCursor,
                isSelected: selected,
                includeSemanticSelected: false,
                isSemanticButton: false,
                isSemanticChecked: selected,
                splashRadius: splashRadius,
                autofocus: CurrentWidget.Autofocus);
        }

        public override void Dispose()
        {
            SetRegistry(null);
            DetachFocusNode(disposeOwned: true);
        }

        private void HandleTap()
        {
            if (!ResolveEnabled() || _registry is null)
            {
                return;
            }

            if (IsSelected())
            {
                if (CurrentWidget.Toggleable)
                {
                    _registry.OnChanged(default);
                }

                return;
            }

            _registry.OnChanged(CurrentWidget.Value);
        }

        private bool IsSelected()
        {
            return EqualityComparer<T?>.Default.Equals(
                CurrentWidget.Value,
                _registry is null ? default : _registry.GroupValue);
        }

        private bool ResolveEnabled()
        {
            return CurrentWidget.Enabled ?? (CurrentWidget.OnChanged is not null || _registryEnablesInteraction);
        }

        private void SyncRegistry()
        {
            RadioGroupRegistry<T>? registry = CurrentWidget.GroupRegistry ?? RadioGroup<T>.MaybeOf(Context);
            _registryEnablesInteraction = registry is not null;
            if (registry is null)
            {
                _legacyRegistry ??= new LegacyRadioRegistry(this);
                registry = _legacyRegistry;
            }

            SetRegistry(registry);
        }

        private void SetRegistry(RadioGroupRegistry<T>? registry)
        {
            if (ReferenceEquals(_registry, registry))
            {
                return;
            }

            _registry?.UnregisterClient(this);
            _registry = registry;
            _registry?.RegisterClient(this);
        }

        private void AttachFocusNode(FocusNode? focusNode)
        {
            _focusNode = focusNode ?? new FocusNode();
            _ownsFocusNode = focusNode is null;
        }

        private void DetachFocusNode(bool disposeOwned)
        {
            if (_focusNode is null)
            {
                return;
            }

            if (disposeOwned && _ownsFocusNode)
            {
                _focusNode.Dispose();
            }

            _focusNode = null;
            _ownsFocusNode = false;
        }

        private sealed class LegacyRadioRegistry : RadioGroupRegistry<T>
        {
            private readonly RadioState _state;

            public LegacyRadioRegistry(RadioState state)
            {
                _state = state;
            }

            public override T? GroupValue => _state.CurrentWidget.GroupValue;

            public override Action<T?> OnChanged => _state.CurrentWidget.OnChanged ?? Noop;

            public override void RegisterClient(RadioClient<T> radio)
            {
            }

            public override void UnregisterClient(RadioClient<T> radio)
            {
            }

            private static void Noop(T? value)
            {
            }
        }

        private double ResolveInnerRadius(RadioThemeData radioTheme, MaterialState states)
        {
            double resolved = CurrentWidget.InnerRadius?.Resolve(states)
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
            double resolved = CurrentWidget.SplashRadius
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
                double pressedOpacity = theme.UseMaterial3 ? 0.10 : 0.12;
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

        private bool IsAdaptiveCupertino(ThemeData theme)
        {
            if (CurrentWidget._radioType != RadioType.Adaptive)
            {
                return false;
            }

            return theme.Platform is TargetPlatform.IOS or TargetPlatform.MacOS;
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
