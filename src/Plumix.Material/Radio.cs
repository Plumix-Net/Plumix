using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/radio.dart

public sealed class Radio<T> : StatefulWidget
{
    private readonly RadioType _radioType;

    private enum RadioType
    {
        Material,
        Adaptive
    }

    public const double Width = 16.0;

    public Radio(
        T value,
        T? groupValue = default,
        Action<T?>? onChanged = null,
        MouseCursor? mouseCursor = null,
        bool toggleable = false,
        Color? activeColor = null,
        WidgetStateProperty<Color?>? fillColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        WidgetStateProperty<Color?>? overlayColor = null,
        double? splashRadius = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        VisualDensity? visualDensity = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        bool? enabled = null,
        RadioGroupRegistry<T>? groupRegistry = null,
        WidgetStateProperty<Color?>? backgroundColor = null,
        WidgetStateBorderSide? side = null,
        WidgetStateProperty<double?>? innerRadius = null,
        Key? key = null)
        : this(
            value: value,
            groupValue: groupValue,
            onChanged: onChanged,
            mouseCursor: mouseCursor,
            toggleable: toggleable,
            activeColor: activeColor,
            fillColor: fillColor,
            focusColor: focusColor,
            hoverColor: hoverColor,
            overlayColor: overlayColor,
            splashRadius: splashRadius,
            materialTapTargetSize: materialTapTargetSize,
            visualDensity: visualDensity,
            focusNode: focusNode,
            autofocus: autofocus,
            enabled: enabled,
            groupRegistry: groupRegistry,
            backgroundColor: backgroundColor,
            side: side,
            innerRadius: innerRadius,
            useCupertinoCheckmarkStyle: false,
            radioType: RadioType.Material,
            key: key)
    {
    }

    private Radio(
        T value,
        T? groupValue,
        Action<T?>? onChanged,
        MouseCursor? mouseCursor,
        bool toggleable,
        Color? activeColor,
        WidgetStateProperty<Color?>? fillColor,
        Color? focusColor,
        Color? hoverColor,
        WidgetStateProperty<Color?>? overlayColor,
        double? splashRadius,
        MaterialTapTargetSize? materialTapTargetSize,
        VisualDensity? visualDensity,
        FocusNode? focusNode,
        bool autofocus,
        bool? enabled,
        RadioGroupRegistry<T>? groupRegistry,
        WidgetStateProperty<Color?>? backgroundColor,
        WidgetStateBorderSide? side,
        WidgetStateProperty<double?>? innerRadius,
        bool useCupertinoCheckmarkStyle,
        RadioType radioType,
        Key? key) : base(key)
    {
        Value = value;
        GroupValue = groupValue;
        OnChanged = onChanged;
        MouseCursor = mouseCursor;
        Toggleable = toggleable;
        ActiveColor = activeColor;
        FillColor = fillColor;
        FocusColor = focusColor;
        HoverColor = hoverColor;
        OverlayColor = overlayColor;
        SplashRadius = splashRadius;
        MaterialTapTargetSize = materialTapTargetSize;
        VisualDensity = visualDensity;
        FocusNode = focusNode;
        Autofocus = autofocus;
        Enabled = enabled;
        GroupRegistry = groupRegistry;
        BackgroundColor = backgroundColor;
        Side = side;
        InnerRadius = innerRadius;
        UseCupertinoCheckmarkStyle = useCupertinoCheckmarkStyle;
        _radioType = radioType;
    }

    public T Value { get; }

    public T? GroupValue { get; }

    public Action<T?>? OnChanged { get; }

    public MouseCursor? MouseCursor { get; }

    public bool Toggleable { get; }

    public Color? ActiveColor { get; }

    public WidgetStateProperty<Color?>? FillColor { get; }

    public Color? FocusColor { get; }

    public Color? HoverColor { get; }

    public WidgetStateProperty<Color?>? OverlayColor { get; }

    public double? SplashRadius { get; }

    public MaterialTapTargetSize? MaterialTapTargetSize { get; }

    public VisualDensity? VisualDensity { get; }

    public FocusNode? FocusNode { get; }

    public bool Autofocus { get; }

    public bool? Enabled { get; }

    public RadioGroupRegistry<T>? GroupRegistry { get; }

    public WidgetStateProperty<Color?>? BackgroundColor { get; }

    public WidgetStateBorderSide? Side { get; }

    public WidgetStateProperty<double?>? InnerRadius { get; }

    public bool UseCupertinoCheckmarkStyle { get; }

    public static Radio<T> Adaptive(
        T value,
        T? groupValue = default,
        Action<T?>? onChanged = null,
        MouseCursor? mouseCursor = null,
        bool toggleable = false,
        Color? activeColor = null,
        WidgetStateProperty<Color?>? fillColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        WidgetStateProperty<Color?>? overlayColor = null,
        double? splashRadius = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        VisualDensity? visualDensity = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        bool useCupertinoCheckmarkStyle = false,
        bool? enabled = null,
        RadioGroupRegistry<T>? groupRegistry = null,
        WidgetStateProperty<Color?>? backgroundColor = null,
        WidgetStateBorderSide? side = null,
        WidgetStateProperty<double?>? innerRadius = null,
        Key? key = null)
    {
        return new Radio<T>(
            value: value,
            groupValue: groupValue,
            onChanged: onChanged,
            mouseCursor: mouseCursor,
            toggleable: toggleable,
            activeColor: activeColor,
            fillColor: fillColor,
            focusColor: focusColor,
            hoverColor: hoverColor,
            overlayColor: overlayColor,
            splashRadius: splashRadius,
            materialTapTargetSize: materialTapTargetSize,
            visualDensity: visualDensity,
            focusNode: focusNode,
            autofocus: autofocus,
            enabled: enabled,
            groupRegistry: groupRegistry,
            backgroundColor: backgroundColor,
            side: side,
            innerRadius: innerRadius,
            useCupertinoCheckmarkStyle: useCupertinoCheckmarkStyle,
            radioType: RadioType.Adaptive,
            key: key);
    }

    public override State CreateState() => new RadioState();

    private sealed class RadioState : State
    {
        private const double DefaultInnerRadius = 4.5;
        private const double DefaultSplashRadius = 20.0;
        private const byte RadialReactionAlpha = 0x1F;

        private FocusNode? _focusNode;
        private bool _ownsFocusNode;
        private LegacyRadioRegistry? _legacyRegistry;
        private RadioPainter? _painter;

        private Radio<T> CurrentWidget => (Radio<T>)StateWidget;

        public override void InitState()
        {
            AttachFocusNode(CurrentWidget.FocusNode);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldRadio = (Radio<T>)oldWidget;
            if (!ReferenceEquals(oldRadio.FocusNode, CurrentWidget.FocusNode))
            {
                DetachFocusNode(disposeOwned: true);
                AttachFocusNode(CurrentWidget.FocusNode);
            }
        }

        public override Widget Build(BuildContext context)
        {
            ThemeData theme = Theme.Of(context);
            RadioGroupRegistry<T>? inheritedRegistry = CurrentWidget.GroupRegistry ?? RadioGroup<T>.MaybeOf(context);
            bool enabled = CurrentWidget.Enabled
                           ?? (CurrentWidget.OnChanged is not null || inheritedRegistry is not null);
            if (CurrentWidget.Enabled == true
                && CurrentWidget.OnChanged is null
                && inheritedRegistry is null)
            {
                throw new InvalidOperationException(
                    "An enabled Radio requires onChanged or a group registry.");
            }

            _legacyRegistry ??= new LegacyRadioRegistry(this);
            RadioGroupRegistry<T> effectiveRegistry = inheritedRegistry ?? _legacyRegistry;

            if (IsAdaptiveCupertino(theme))
            {
                return new CupertinoRadio<T>(
                    value: CurrentWidget.Value,
                    groupValue: CurrentWidget.GroupValue,
                    onChanged: CurrentWidget.OnChanged,
                    mouseCursor: CurrentWidget.MouseCursor,
                    toggleable: CurrentWidget.Toggleable,
                    activeColor: CurrentWidget.ActiveColor,
                    focusColor: CurrentWidget.FocusColor,
                    focusNode: _focusNode,
                    autofocus: CurrentWidget.Autofocus,
                    useCheckmarkStyle: CurrentWidget.UseCupertinoCheckmarkStyle,
                    enabled: enabled,
                    groupRegistry: effectiveRegistry);
            }

            RadioThemeData radioTheme = RadioTheme.Of(context);
            WidgetStateProperty<MouseCursor> mouseCursor = WidgetStateProperty<MouseCursor>.ResolveWith(
                states => ResolveMouseCursor(radioTheme, states));
            return new RawRadio<T>(
                value: CurrentWidget.Value,
                mouseCursor: mouseCursor,
                toggleable: CurrentWidget.Toggleable,
                focusNode: _focusNode!,
                autofocus: CurrentWidget.Autofocus,
                groupRegistry: effectiveRegistry,
                enabled: enabled,
                builder: (_, state) => BuildRadioPaint(theme, radioTheme, state));
        }

        public override void Dispose()
        {
            _painter?.Dispose();
            _painter = null;
            DetachFocusNode(disposeOwned: true);

            base.Dispose();
        }

        private Widget BuildRadioPaint(
            ThemeData theme,
            RadioThemeData radioTheme,
            RawRadioState<T> state)
        {
            _painter ??= new RadioPainter(
                state.PositionAnimation,
                state.ReactionAnimation,
                state.ReactionHoverFadeAnimation,
                state.ReactionFocusFadeAnimation);

            IReadOnlySet<WidgetState> states = state.States;
            IReadOnlySet<WidgetState> activeStates = WithSelected(states, selected: true);
            IReadOnlySet<WidgetState> inactiveStates = WithSelected(states, selected: false);
            Color? nonDefaultActiveColor = ResolveNonDefaultFillColor(radioTheme, activeStates);
            Color? nonDefaultInactiveColor = ResolveNonDefaultFillColor(radioTheme, inactiveStates);
            Color activeColor = nonDefaultActiveColor ?? ResolveDefaultFillColor(theme, activeStates);
            Color inactiveColor = nonDefaultInactiveColor ?? ResolveDefaultFillColor(theme, inactiveStates);
            Color activeBackgroundColor = ResolveBackgroundColor(radioTheme, activeStates);
            Color inactiveBackgroundColor = ResolveBackgroundColor(radioTheme, inactiveStates);
            BorderSide activeSide = ResolveSide(radioTheme, activeStates, activeColor);
            BorderSide inactiveSide = ResolveSide(radioTheme, inactiveStates, inactiveColor);
            double innerRadius = CurrentWidget.InnerRadius?.Resolve(activeStates)
                                 ?? radioTheme.InnerRadius?.Resolve(activeStates)
                                 ?? DefaultInnerRadius;
            Color activeReactionColor = ResolvePressedOverlayColor(
                theme,
                radioTheme,
                activeStates,
                nonDefaultActiveColor);
            Color inactiveReactionColor = ResolvePressedOverlayColor(
                theme,
                radioTheme,
                inactiveStates,
                nonDefaultInactiveColor);
            Color hoverColor = ResolveLegacyOverlayColor(
                theme,
                radioTheme,
                WithInteractionState(states, WidgetState.Hovered),
                CurrentWidget.HoverColor);
            Color focusColor = ResolveLegacyOverlayColor(
                theme,
                radioTheme,
                WithInteractionState(states, WidgetState.Focused),
                CurrentWidget.FocusColor);
            Color reactionColor = state.Selected ? activeReactionColor : inactiveReactionColor;
            if (state.PressPosition.HasValue)
            {
                hoverColor = reactionColor;
                focusColor = reactionColor;
            }

            double splashRadius = CurrentWidget.SplashRadius
                                  ?? radioTheme.SplashRadius
                                  ?? DefaultSplashRadius;
            _painter.Configure(
                activeColor: activeColor,
                inactiveColor: inactiveColor,
                activeBackgroundColor: activeBackgroundColor,
                inactiveBackgroundColor: inactiveBackgroundColor,
                activeSide: activeSide,
                inactiveSide: inactiveSide,
                innerRadius: innerRadius,
                splashRadius: splashRadius,
                reactionColor: activeReactionColor,
                inactiveReactionColor: inactiveReactionColor,
                hoverColor: hoverColor,
                focusColor: focusColor,
                downPosition: state.PressPosition,
                isFocused: state.States.Contains(WidgetState.Focused),
                isHovered: state.States.Contains(WidgetState.Hovered));

            MaterialTapTargetSize tapTargetSize = CurrentWidget.MaterialTapTargetSize
                                                  ?? radioTheme.MaterialTapTargetSize
                                                  ?? theme.MaterialTapTargetSize;
            VisualDensity visualDensity = CurrentWidget.VisualDensity
                                          ?? radioTheme.VisualDensity
                                          ?? theme.VisualDensity;
            double baseSize = tapTargetSize == Plumix.Material.MaterialTapTargetSize.Padded
                ? 48.0
                : 40.0;
            Vector densityAdjustment = visualDensity.BaseSizeAdjustment;
            var size = new Size(
                Math.Max(0.0, baseSize + densityAdjustment.X),
                Math.Max(0.0, baseSize + densityAdjustment.Y));
            return new CustomPaint(
                painter: _painter,
                size: size);
        }

        private Color? ResolveNonDefaultFillColor(
            RadioThemeData radioTheme,
            IReadOnlySet<WidgetState> states)
        {
            Color? widgetFill = CurrentWidget.FillColor?.Resolve(states);
            if (widgetFill.HasValue)
            {
                return widgetFill;
            }

            if (!states.Contains(WidgetState.Disabled)
                && states.Contains(WidgetState.Selected)
                && CurrentWidget.ActiveColor.HasValue)
            {
                return CurrentWidget.ActiveColor;
            }

            return radioTheme.FillColor?.Resolve(states);
        }

        private Color ResolveBackgroundColor(
            RadioThemeData radioTheme,
            IReadOnlySet<WidgetState> states)
        {
            return CurrentWidget.BackgroundColor?.Resolve(states)
                   ?? radioTheme.BackgroundColor?.Resolve(states)
                   ?? Colors.Transparent;
        }

        private BorderSide ResolveSide(
            RadioThemeData radioTheme,
            IReadOnlySet<WidgetState> states,
            Color fillColor)
        {
            return CurrentWidget.Side?.Resolve(states)
                   ?? radioTheme.Side?.Resolve(states)
                   ?? new BorderSide(fillColor, 2.0);
        }

        private Color ResolvePressedOverlayColor(
            ThemeData theme,
            RadioThemeData radioTheme,
            IReadOnlySet<WidgetState> states,
            Color? nonDefaultFillColor)
        {
            IReadOnlySet<WidgetState> pressedStates = WithInteractionState(states, WidgetState.Pressed);
            return CurrentWidget.OverlayColor?.Resolve(pressedStates)
                   ?? radioTheme.OverlayColor?.Resolve(pressedStates)
                   ?? (nonDefaultFillColor.HasValue
                       ? WithAlpha(nonDefaultFillColor.Value, RadialReactionAlpha)
                       : ResolveDefaultOverlayColor(theme, pressedStates));
        }

        private Color ResolveLegacyOverlayColor(
            ThemeData theme,
            RadioThemeData radioTheme,
            IReadOnlySet<WidgetState> states,
            Color? legacyColor)
        {
            return CurrentWidget.OverlayColor?.Resolve(states)
                   ?? legacyColor
                   ?? radioTheme.OverlayColor?.Resolve(states)
                   ?? ResolveDefaultOverlayColor(theme, states);
        }

        private MouseCursor ResolveMouseCursor(
            RadioThemeData radioTheme,
            IReadOnlySet<WidgetState> states)
        {
            MouseCursor? widgetCursor = CurrentWidget.MouseCursor is WidgetStateMouseCursor stateCursor
                ? stateCursor.Resolve(states)
                : CurrentWidget.MouseCursor;
            return widgetCursor
                   ?? radioTheme.MouseCursor?.Resolve(states)
                   ?? (OperatingSystem.IsBrowser() && !states.Contains(WidgetState.Disabled)
                       ? SystemMouseCursors.Click
                       : SystemMouseCursors.Basic);
        }

        private bool IsAdaptiveCupertino(ThemeData theme)
        {
            return CurrentWidget._radioType == RadioType.Adaptive
                   && theme.Platform is TargetPlatform.IOS or TargetPlatform.MacOS;
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

        private static IReadOnlySet<WidgetState> WithSelected(IReadOnlySet<WidgetState> states, bool selected)
        {
            var result = new HashSet<WidgetState>(states);
            if (selected)
            {
                result.Add(WidgetState.Selected);
            }
            else
            {
                result.Remove(WidgetState.Selected);
            }

            return result;
        }

        private static IReadOnlySet<WidgetState> WithInteractionState(
            IReadOnlySet<WidgetState> states,
            WidgetState interaction)
        {
            return new HashSet<WidgetState>(states) { interaction };
        }

        private static Color ResolveDefaultFillColor(ThemeData theme, IReadOnlySet<WidgetState> states)
        {
            bool disabled = states.Contains(WidgetState.Disabled);
            bool selected = states.Contains(WidgetState.Selected);
            if (!theme.UseMaterial3)
            {
                if (disabled)
                {
                    return theme.DisabledColor;
                }
                return selected ? theme.ColorScheme.Secondary : theme.UnselectedWidgetColor;
            }

            if (disabled)
            {
                return WithOpacity(theme.ColorScheme.OnSurface, 0.38);
            }
            if (selected)
            {
                return theme.ColorScheme.Primary;
            }
            if (states.Contains(WidgetState.Pressed)
                || states.Contains(WidgetState.Hovered)
                || states.Contains(WidgetState.Focused))
            {
                return theme.ColorScheme.OnSurface;
            }
            return theme.ColorScheme.OnSurfaceVariant;
        }

        private static Color ResolveDefaultOverlayColor(ThemeData theme, IReadOnlySet<WidgetState> states)
        {
            if (!theme.UseMaterial3)
            {
                if (states.Contains(WidgetState.Pressed))
                {
                    return WithAlpha(
                        ResolveDefaultFillColor(theme, states),
                        RadialReactionAlpha);
                }
                if (states.Contains(WidgetState.Hovered))
                {
                    return theme.HoverColor;
                }
                if (states.Contains(WidgetState.Focused))
                {
                    return theme.FocusColor;
                }
                return Colors.Transparent;
            }

            bool selected = states.Contains(WidgetState.Selected);
            if (selected)
            {
                if (states.Contains(WidgetState.Pressed))
                {
                    return WithOpacity(theme.ColorScheme.OnSurface, 0.10);
                }
                if (states.Contains(WidgetState.Hovered))
                {
                    return WithOpacity(theme.ColorScheme.Primary, 0.08);
                }
                if (states.Contains(WidgetState.Focused))
                {
                    return WithOpacity(theme.ColorScheme.Primary, 0.10);
                }
                return Colors.Transparent;
            }
            if (states.Contains(WidgetState.Pressed))
            {
                return WithOpacity(theme.ColorScheme.Primary, 0.10);
            }
            if (states.Contains(WidgetState.Hovered))
            {
                return WithOpacity(theme.ColorScheme.OnSurface, 0.08);
            }
            if (states.Contains(WidgetState.Focused))
            {
                return WithOpacity(theme.ColorScheme.OnSurface, 0.10);
            }
            return Colors.Transparent;
        }

        private static Color WithAlpha(Color color, byte alpha)
        {
            return Color.FromArgb(alpha, color.R, color.G, color.B);
        }

        private static Color WithOpacity(Color color, double opacity)
        {
            byte alpha = (byte)Math.Clamp(
                (int)Math.Round(byte.MaxValue * Math.Clamp(opacity, 0.0, 1.0)),
                0,
                byte.MaxValue);
            return Color.FromArgb(alpha, color.R, color.G, color.B);
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
    }
}

internal sealed class RadioPainter : ToggleablePainter
{
    private const double OuterRadius = 8.0;

    private Color _activeBackgroundColor;
    private Color _inactiveBackgroundColor;
    private BorderSide _activeSide;
    private BorderSide _inactiveSide;
    private double _innerRadius;

    public RadioPainter(
        Animation<double> position,
        Animation<double> reaction,
        Animation<double> reactionHoverFade,
        Animation<double> reactionFocusFade)
        : base(position, reaction, reactionHoverFade, reactionFocusFade)
    {
    }

    internal Color ActiveBackgroundColor => _activeBackgroundColor;

    internal Color InactiveBackgroundColor => _inactiveBackgroundColor;

    internal BorderSide ActiveSide => _activeSide;

    internal BorderSide InactiveSide => _inactiveSide;

    internal double InnerRadius => _innerRadius;

    internal Color ActiveReactionColor => ReactionColor;

    internal void Configure(
        Color activeColor,
        Color inactiveColor,
        Color activeBackgroundColor,
        Color inactiveBackgroundColor,
        BorderSide activeSide,
        BorderSide inactiveSide,
        double innerRadius,
        double splashRadius,
        Color reactionColor,
        Color inactiveReactionColor,
        Color hoverColor,
        Color focusColor,
        Point? downPosition,
        bool isFocused,
        bool isHovered)
    {
        ActiveColor = activeColor;
        InactiveColor = inactiveColor;
        _activeBackgroundColor = activeBackgroundColor;
        _inactiveBackgroundColor = inactiveBackgroundColor;
        _activeSide = activeSide;
        _inactiveSide = inactiveSide;
        _innerRadius = innerRadius;
        SplashRadius = splashRadius;
        ReactionColor = reactionColor;
        InactiveReactionColor = inactiveReactionColor;
        HoverColor = hoverColor;
        FocusColor = focusColor;
        DownPosition = downPosition;
        IsFocused = isFocused;
        IsHovered = isHovered;
        NotifyPainterChanged();
    }

    public override void Paint(PaintingContext context, Size size)
    {
        Point origin = new(size.Width / 2.0, size.Height / 2.0);
        PaintRadialReaction(context, origin);

        Color backgroundColor = LerpColor(
            _inactiveBackgroundColor,
            _activeBackgroundColor,
            Position.Value);
        context.Canvas.DrawCircle(
            new SolidColorBrush(backgroundColor),
            null,
            origin,
            OuterRadius);

        BorderSide side = MaterialThemeLerp.BorderSide(
            _inactiveSide,
            _activeSide,
            Position.Value) ?? _activeSide;
        Pen? pen = side.Width > 0.0
            ? new Pen(new SolidColorBrush(side.Color), side.Width)
            : null;
        context.Canvas.DrawCircle(
            new SolidColorBrush(Colors.Transparent),
            pen,
            origin,
            OuterRadius);

        if (Position.Value <= 0.0)
        {
            return;
        }

        Color innerColor = LerpColor(InactiveColor, ActiveColor, Position.Value);
        context.Canvas.DrawCircle(
            new SolidColorBrush(innerColor),
            null,
            origin,
            Math.Max(0.0, _innerRadius * Position.Value));
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate)
    {
        return !ReferenceEquals(this, oldDelegate);
    }
}
