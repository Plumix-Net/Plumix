using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/checkbox.dart

public sealed class Checkbox : StatefulWidget
{
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
        MouseCursor? mouseCursor = null,
        Color? activeColor = null,
        MaterialStateProperty<Color?>? fillColor = null,
        Color? checkColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        double? splashRadius = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        VisualDensity? visualDensity = null,
        ShapeBorder? shape = null,
        WidgetStateBorderSide? side = null,
        bool isError = false,
        string? semanticLabel = null,
        FocusNode? focusNode = null,
        bool autofocus = false,
        Key? key = null) : this(
            value: value,
            onChanged: onChanged,
            tristate: tristate,
            mouseCursor: mouseCursor,
            activeColor: activeColor,
            fillColor: fillColor,
            checkColor: checkColor,
            focusColor: focusColor,
            hoverColor: hoverColor,
            overlayColor: overlayColor,
            splashRadius: splashRadius,
            materialTapTargetSize: materialTapTargetSize,
            visualDensity: visualDensity,
            shape: shape,
            side: side,
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
        MouseCursor? mouseCursor,
        Color? activeColor,
        MaterialStateProperty<Color?>? fillColor,
        Color? checkColor,
        Color? focusColor,
        Color? hoverColor,
        MaterialStateProperty<Color?>? overlayColor,
        double? splashRadius,
        MaterialTapTargetSize? materialTapTargetSize,
        VisualDensity? visualDensity,
        ShapeBorder? shape,
        WidgetStateBorderSide? side,
        bool isError,
        string? semanticLabel,
        FocusNode? focusNode,
        bool autofocus,
        CheckboxType checkboxType,
        Key? key) : base(key)
    {
        if (!tristate && value is null)
        {
            throw new ArgumentException(
                "Checkbox value cannot be null when tristate is false.",
                nameof(value));
        }

        Value = value;
        OnChanged = onChanged;
        Tristate = tristate;
        MouseCursor = mouseCursor;
        ActiveColor = activeColor;
        FillColor = fillColor;
        CheckColor = checkColor;
        FocusColor = focusColor;
        HoverColor = hoverColor;
        OverlayColor = overlayColor;
        SplashRadius = splashRadius;
        MaterialTapTargetSize = materialTapTargetSize;
        VisualDensity = visualDensity;
        Shape = shape;
        Side = side;
        IsError = isError;
        SemanticLabel = semanticLabel;
        FocusNode = focusNode;
        Autofocus = autofocus;
        _checkboxType = checkboxType;
    }

    public bool? Value { get; }

    public Action<bool?>? OnChanged { get; }

    public bool Tristate { get; }

    public MouseCursor? MouseCursor { get; }

    public Color? ActiveColor { get; }

    public MaterialStateProperty<Color?>? FillColor { get; }

    public Color? CheckColor { get; }

    public Color? FocusColor { get; }

    public Color? HoverColor { get; }

    public MaterialStateProperty<Color?>? OverlayColor { get; }

    public double? SplashRadius { get; }

    public MaterialTapTargetSize? MaterialTapTargetSize { get; }

    public VisualDensity? VisualDensity { get; }

    public ShapeBorder? Shape { get; }

    public WidgetStateBorderSide? Side { get; }

    public bool IsError { get; }

    public string? SemanticLabel { get; }

    public FocusNode? FocusNode { get; }

    public bool Autofocus { get; }

    public static Checkbox Adaptive(
        bool? value,
        Action<bool?>? onChanged,
        bool tristate = false,
        MouseCursor? mouseCursor = null,
        Color? activeColor = null,
        MaterialStateProperty<Color?>? fillColor = null,
        Color? checkColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        double? splashRadius = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        VisualDensity? visualDensity = null,
        ShapeBorder? shape = null,
        WidgetStateBorderSide? side = null,
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
            mouseCursor: mouseCursor,
            activeColor: activeColor,
            fillColor: fillColor,
            checkColor: checkColor,
            focusColor: focusColor,
            hoverColor: hoverColor,
            overlayColor: overlayColor,
            splashRadius: splashRadius,
            materialTapTargetSize: materialTapTargetSize,
            visualDensity: visualDensity,
            shape: shape,
            side: side,
            isError: isError,
            semanticLabel: semanticLabel,
            focusNode: focusNode,
            autofocus: autofocus,
            checkboxType: CheckboxType.Adaptive,
            key: key);
    }

    public override State CreateState() => new CheckboxState();

    private sealed class CheckboxState : ToggleableState
    {
        private const double DefaultSplashRadius = 20.0;
        private const byte RadialReactionAlpha = 0x1F;

        private CheckboxPainter? _painter;
        private bool? _previousValue;

        private Checkbox CurrentWidget => (Checkbox)StateWidget;

        protected override bool IsInteractive => CurrentWidget.OnChanged is not null;

        protected override bool IsValueSelected => IsSelected(CurrentWidget.Value);

        public override void InitState()
        {
            base.InitState();
            _previousValue = CurrentWidget.Value;
            _painter = new CheckboxPainter(
                Position,
                Reaction,
                ReactionHoverFade,
                ReactionFocusFade);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldCheckbox = (Checkbox)oldWidget;
            base.DidUpdateWidget(oldWidget);
            if (oldCheckbox.Value == CurrentWidget.Value)
            {
                return;
            }

            _previousValue = oldCheckbox.Value;
            AnimateToValue(CurrentWidget.Value, CurrentWidget.Tristate);
        }

        public override void Dispose()
        {
            _painter?.Dispose();
            _painter = null;
            base.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            ThemeData theme = Theme.Of(context);
            if (IsAdaptiveCupertino(theme))
            {
                return BuildAdaptive(theme);
            }

            CheckboxThemeData checkboxTheme = CheckboxTheme.Of(context);
            MaterialState states = WithError(ToMaterialState(CurrentWidgetStates));
            MaterialState activeStates = WithSelected(states, selected: true);
            MaterialState inactiveStates = WithSelected(states, selected: false);

            Color? nonDefaultActiveColor = ResolveNonDefaultFillColor(
                checkboxTheme,
                activeStates);
            Color? nonDefaultInactiveColor = ResolveNonDefaultFillColor(
                checkboxTheme,
                inactiveStates);
            Color activeColor = nonDefaultActiveColor ?? ResolveDefaultFillColor(theme, activeStates);
            Color inactiveColor = nonDefaultInactiveColor ?? ResolveDefaultFillColor(theme, inactiveStates);
            Color checkColor = ResolveCheckColor(theme, checkboxTheme, states);
            BorderSide? activeSide = ResolveSide(theme, checkboxTheme, activeStates);
            BorderSide? inactiveSide = ResolveSide(theme, checkboxTheme, inactiveStates);
            Color activeReactionColor = ResolvePressedOverlayColor(
                theme,
                checkboxTheme,
                activeStates,
                nonDefaultActiveColor);
            Color inactiveReactionColor = ResolvePressedOverlayColor(
                theme,
                checkboxTheme,
                inactiveStates,
                nonDefaultInactiveColor);
            Color hoverColor = ResolveLegacyOverlayColor(
                theme,
                checkboxTheme,
                WithInteractionState(states, MaterialState.Hovered),
                CurrentWidget.HoverColor);
            Color focusColor = ResolveLegacyOverlayColor(
                theme,
                checkboxTheme,
                WithInteractionState(states, MaterialState.Focused),
                CurrentWidget.FocusColor);
            Color reactionColor = IsValueSelected
                ? activeReactionColor
                : inactiveReactionColor;
            if (DownPosition.HasValue)
            {
                hoverColor = reactionColor;
                focusColor = reactionColor;
            }

            ShapeBorder shape = CurrentWidget.Shape
                                ?? checkboxTheme.Shape
                                ?? new RoundedRectangleBorder(borderRadius:
                                    Plumix.Rendering.BorderRadius.Circular(theme.UseMaterial3 ? 2.0 : 1.0));
            double splashRadius = CurrentWidget.SplashRadius
                                  ?? checkboxTheme.SplashRadius
                                  ?? DefaultSplashRadius;
            if (!double.IsFinite(splashRadius) || splashRadius <= 0.0)
            {
                splashRadius = DefaultSplashRadius;
            }

            _painter!.Configure(
                value: CurrentWidget.Value,
                previousValue: _previousValue,
                activeColor: activeColor,
                inactiveColor: inactiveColor,
                checkColor: checkColor,
                activeSide: activeSide,
                inactiveSide: inactiveSide,
                shape: shape,
                splashRadius: splashRadius,
                reactionColor: activeReactionColor,
                inactiveReactionColor: inactiveReactionColor,
                hoverColor: hoverColor,
                focusColor: focusColor);

            MaterialTapTargetSize tapTargetSize = CurrentWidget.MaterialTapTargetSize
                                                  ?? checkboxTheme.MaterialTapTargetSize
                                                  ?? theme.MaterialTapTargetSize;
            VisualDensity visualDensity = CurrentWidget.VisualDensity
                                          ?? checkboxTheme.VisualDensity
                                          ?? (theme.UseMaterial3
                                              ? Plumix.Material.VisualDensity.Standard
                                              : theme.VisualDensity);
            double baseSize = tapTargetSize == Plumix.Material.MaterialTapTargetSize.Padded
                ? 48.0
                : 40.0;
            Vector densityAdjustment = visualDensity.BaseSizeAdjustment;
            var size = new Size(
                Math.Max(0.0, baseSize + densityAdjustment.X),
                Math.Max(0.0, baseSize + densityAdjustment.Y));
            MouseCursor mouseCursor = ResolveMouseCursor(checkboxTheme, states);

            Widget toggleable = BuildToggleable(
                painter: _painter,
                size: size,
                mouseCursor: mouseCursor,
                onTap: HandleTap,
                focusNode: CurrentWidget.FocusNode,
                autofocus: CurrentWidget.Autofocus);
            return new Semantics(
                label: CurrentWidget.SemanticLabel,
                flags: IsInteractive ? SemanticsFlags.IsEnabled : SemanticsFlags.None,
                @checked: CurrentWidget.Value ?? false,
                mixed: CurrentWidget.Tristate ? CurrentWidget.Value is null : null,
                onTap: IsInteractive ? HandleTap : null,
                child: toggleable);
        }

        private Widget BuildAdaptive(ThemeData theme)
        {
            MaterialState states = ToMaterialState(CurrentWidgetStates);
            BorderSide? side = CurrentWidget.Side?.Resolve(states);
            var tapTargetSize = theme.Platform == TargetPlatform.MacOS
                ? new Size(CupertinoCheckbox.Width, CupertinoCheckbox.Width)
                : new Size(44.0, 44.0);
            return new CupertinoCheckbox(
                value: CurrentWidget.Value,
                tristate: CurrentWidget.Tristate,
                onChanged: CurrentWidget.OnChanged,
                mouseCursor: ResolveWidgetMouseCursor(CurrentWidget.MouseCursor, states),
                activeColor: CurrentWidget.ActiveColor,
                checkColor: CurrentWidget.CheckColor,
                focusColor: CurrentWidget.FocusColor,
                focusNode: CurrentWidget.FocusNode,
                autofocus: CurrentWidget.Autofocus,
                side: side,
                shape: ShapeBorderGeometry.ResolveRadiusOrNull(CurrentWidget.Shape),
                tapTargetSize: tapTargetSize,
                isDark: theme.Brightness == Brightness.Dark,
                semanticLabel: CurrentWidget.SemanticLabel);
        }

        private void HandleTap()
        {
            CurrentWidget.OnChanged?.Invoke(NextValue());
            SemanticsService.SendEvent(
                new TapSemanticEvent(Context.FindRenderObject()?.SemanticsNodeId));
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
                _ => false,
            };
        }

        private Color? ResolveNonDefaultFillColor(
            CheckboxThemeData checkboxTheme,
            MaterialState states)
        {
            Color? widgetFill = CurrentWidget.FillColor?.Resolve(states);
            if (widgetFill.HasValue)
            {
                return widgetFill;
            }

            if (!states.HasFlag(MaterialState.Disabled)
                && states.HasFlag(MaterialState.Selected)
                && CurrentWidget.ActiveColor.HasValue)
            {
                return CurrentWidget.ActiveColor;
            }

            return checkboxTheme.FillColor?.Resolve(states);
        }

        private Color ResolveCheckColor(
            ThemeData theme,
            CheckboxThemeData checkboxTheme,
            MaterialState states)
        {
            return CurrentWidget.CheckColor
                   ?? checkboxTheme.CheckColor?.Resolve(states)
                   ?? ResolveDefaultCheckColor(theme, states);
        }

        private BorderSide? ResolveSide(
            ThemeData theme,
            CheckboxThemeData checkboxTheme,
            MaterialState states)
        {
            BorderSide? widgetSide = CurrentWidget.Side?.Resolve(states);
            if (widgetSide.HasValue)
            {
                return widgetSide;
            }

            BorderSide? themeSide = checkboxTheme.Side?.Resolve(states);
            return themeSide ?? ResolveDefaultSide(theme, states);
        }

        private Color ResolvePressedOverlayColor(
            ThemeData theme,
            CheckboxThemeData checkboxTheme,
            MaterialState states,
            Color? nonDefaultFillColor)
        {
            MaterialState pressedStates = WithInteractionState(states, MaterialState.Pressed);
            return CurrentWidget.OverlayColor?.Resolve(pressedStates)
                   ?? checkboxTheme.OverlayColor?.Resolve(pressedStates)
                   ?? (nonDefaultFillColor.HasValue
                       ? WithAlpha(nonDefaultFillColor.Value, RadialReactionAlpha)
                       : ResolveDefaultOverlayColor(theme, pressedStates));
        }

        private Color ResolveLegacyOverlayColor(
            ThemeData theme,
            CheckboxThemeData checkboxTheme,
            MaterialState states,
            Color? legacyColor)
        {
            return CurrentWidget.OverlayColor?.Resolve(states)
                   ?? legacyColor
                   ?? checkboxTheme.OverlayColor?.Resolve(states)
                   ?? ResolveDefaultOverlayColor(theme, states);
        }

        private MouseCursor ResolveMouseCursor(
            CheckboxThemeData checkboxTheme,
            MaterialState states)
        {
            return ResolveWidgetMouseCursor(CurrentWidget.MouseCursor, states)
                   ?? checkboxTheme.MouseCursor?.Resolve(states)
                   ?? (OperatingSystem.IsBrowser()
                       && !states.HasFlag(MaterialState.Disabled)
                           ? SystemMouseCursors.Click
                           : SystemMouseCursors.Basic);
        }

        private static MouseCursor? ResolveWidgetMouseCursor(
            MouseCursor? cursor,
            MaterialState states)
        {
            return cursor is WidgetStateMouseCursor stateCursor
                ? stateCursor.Resolve(states)
                : cursor;
        }

        private bool IsAdaptiveCupertino(ThemeData theme)
        {
            return CurrentWidget._checkboxType == CheckboxType.Adaptive
                   && theme.Platform is TargetPlatform.IOS or TargetPlatform.MacOS;
        }

        private MaterialState WithError(MaterialState states)
        {
            return CurrentWidget.IsError
                ? states | MaterialState.Error
                : states & ~MaterialState.Error;
        }

        private static MaterialState WithSelected(MaterialState states, bool selected)
        {
            return selected
                ? states | MaterialState.Selected
                : states & ~MaterialState.Selected;
        }

        private static MaterialState WithInteractionState(
            MaterialState states,
            MaterialState interaction)
        {
            return states | interaction;
        }

        private static bool IsSelected(bool? value) => value ?? true;

        private static MaterialState ToMaterialState(IReadOnlySet<WidgetState> states)
        {
            MaterialState result = MaterialState.None;
            if (states.Contains(WidgetState.Disabled))
            {
                result |= MaterialState.Disabled;
            }
            if (states.Contains(WidgetState.Selected))
            {
                result |= MaterialState.Selected;
            }
            if (states.Contains(WidgetState.Hovered))
            {
                result |= MaterialState.Hovered;
            }
            if (states.Contains(WidgetState.Focused))
            {
                result |= MaterialState.Focused;
            }
            if (states.Contains(WidgetState.Pressed))
            {
                result |= MaterialState.Pressed;
            }
            return result;
        }

        private static Color ResolveDefaultFillColor(ThemeData theme, MaterialState states)
        {
            bool disabled = states.HasFlag(MaterialState.Disabled);
            bool selected = states.HasFlag(MaterialState.Selected);
            if (!theme.UseMaterial3)
            {
                if (disabled)
                {
                    return selected ? theme.DisabledColor : Colors.Transparent;
                }
                return selected ? theme.ColorScheme.Secondary : Colors.Transparent;
            }

            if (disabled)
            {
                return selected
                    ? WithOpacity(theme.ColorScheme.OnSurface, 0.38)
                    : Colors.Transparent;
            }
            if (selected && states.HasFlag(MaterialState.Error))
            {
                return theme.ColorScheme.Error;
            }
            return selected ? theme.ColorScheme.Primary : Colors.Transparent;
        }

        private static Color ResolveDefaultCheckColor(ThemeData theme, MaterialState states)
        {
            if (!theme.UseMaterial3)
            {
                return Colors.White;
            }

            bool disabled = states.HasFlag(MaterialState.Disabled);
            bool selected = states.HasFlag(MaterialState.Selected);
            if (disabled)
            {
                return selected ? theme.ColorScheme.Surface : Colors.Transparent;
            }
            if (selected && states.HasFlag(MaterialState.Error))
            {
                return theme.ColorScheme.OnError;
            }
            return selected ? theme.ColorScheme.OnPrimary : Colors.Transparent;
        }

        private static BorderSide ResolveDefaultSide(ThemeData theme, MaterialState states)
        {
            bool disabled = states.HasFlag(MaterialState.Disabled);
            bool selected = states.HasFlag(MaterialState.Selected);
            if (!theme.UseMaterial3)
            {
                Color color = disabled
                    ? selected ? Colors.Transparent : theme.DisabledColor
                    : selected ? Colors.Transparent : theme.UnselectedWidgetColor;
                return new BorderSide(color, 2.0);
            }

            if (disabled)
            {
                return selected
                    ? new BorderSide(Colors.Transparent, 2.0)
                    : new BorderSide(WithOpacity(theme.ColorScheme.OnSurface, 0.38), 2.0);
            }
            if (selected)
            {
                return new BorderSide(Colors.Transparent, 0.0);
            }
            if (states.HasFlag(MaterialState.Error))
            {
                return new BorderSide(theme.ColorScheme.Error, 2.0);
            }
            if (states.HasFlag(MaterialState.Pressed)
                || states.HasFlag(MaterialState.Hovered)
                || states.HasFlag(MaterialState.Focused))
            {
                return new BorderSide(theme.ColorScheme.OnSurface, 2.0);
            }
            return new BorderSide(theme.ColorScheme.OnSurfaceVariant, 2.0);
        }

        private static Color ResolveDefaultOverlayColor(ThemeData theme, MaterialState states)
        {
            if (!theme.UseMaterial3)
            {
                if (states.HasFlag(MaterialState.Pressed))
                {
                    return WithAlpha(
                        ResolveDefaultFillColor(theme, states),
                        RadialReactionAlpha);
                }
                if (states.HasFlag(MaterialState.Hovered))
                {
                    return theme.HoverColor;
                }
                if (states.HasFlag(MaterialState.Focused))
                {
                    return theme.FocusColor;
                }
                return Colors.Transparent;
            }

            bool error = states.HasFlag(MaterialState.Error);
            bool selected = states.HasFlag(MaterialState.Selected);
            bool pressed = states.HasFlag(MaterialState.Pressed);
            bool hovered = states.HasFlag(MaterialState.Hovered);
            bool focused = states.HasFlag(MaterialState.Focused);
            if (error)
            {
                if (pressed || focused)
                {
                    return WithOpacity(theme.ColorScheme.Error, 0.10);
                }
                if (hovered)
                {
                    return WithOpacity(theme.ColorScheme.Error, 0.08);
                }
            }
            if (selected)
            {
                if (pressed)
                {
                    return WithOpacity(theme.ColorScheme.OnSurface, 0.10);
                }
                if (hovered)
                {
                    return WithOpacity(theme.ColorScheme.Primary, 0.08);
                }
                if (focused)
                {
                    return WithOpacity(theme.ColorScheme.Primary, 0.10);
                }
                return Colors.Transparent;
            }
            if (pressed)
            {
                return WithOpacity(theme.ColorScheme.Primary, 0.10);
            }
            if (hovered)
            {
                return WithOpacity(theme.ColorScheme.OnSurface, 0.08);
            }
            if (focused)
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
    }
}

internal sealed class CheckboxPainter : ToggleablePainter
{
    private const double EdgeSize = 18.0;
    private const double StrokeWidth = 2.0;

    private bool? _value;
    private bool? _previousValue;
    private Color _activeColor;
    private Color _inactiveColor;
    private Color _checkColor;
    private BorderSide? _activeSide;
    private BorderSide? _inactiveSide;
    private ShapeBorder _shape = new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(2.0));
    private Color _inactiveReactionColor;

    public CheckboxPainter(
        Animation<double> position,
        Animation<double> reaction,
        Animation<double> reactionHoverFade,
        Animation<double> reactionFocusFade)
        : base(position, reaction, reactionHoverFade, reactionFocusFade)
    {
    }

    internal bool? Value => _value;

    internal bool? PreviousValue => _previousValue;

    internal Color ActiveColor => _activeColor;

    internal Color InactiveColor => _inactiveColor;

    internal Color CheckColor => _checkColor;

    internal BorderSide? ActiveSide => _activeSide;

    internal BorderSide? InactiveSide => _inactiveSide;

    internal ShapeBorder Shape => _shape;

    internal Color ActiveReactionColor => ReactionColor;

    internal Color InactiveReactionColor => _inactiveReactionColor;

    internal Color ResolvedHoverColor => HoverColor;

    internal Color ResolvedFocusColor => FocusColor;

    internal double ResolvedSplashRadius => SplashRadius;

    internal void Configure(
        bool? value,
        bool? previousValue,
        Color activeColor,
        Color inactiveColor,
        Color checkColor,
        BorderSide? activeSide,
        BorderSide? inactiveSide,
        ShapeBorder shape,
        double splashRadius,
        Color reactionColor,
        Color inactiveReactionColor,
        Color hoverColor,
        Color focusColor)
    {
        _value = value;
        _previousValue = previousValue;
        _activeColor = activeColor;
        _inactiveColor = inactiveColor;
        _checkColor = checkColor;
        _activeSide = activeSide;
        _inactiveSide = inactiveSide;
        _shape = shape;
        SplashRadius = splashRadius;
        ReactionColor = reactionColor;
        _inactiveReactionColor = inactiveReactionColor;
        HoverColor = hoverColor;
        FocusColor = focusColor;
        NotifyPainterChanged();
    }

    public override void Paint(PaintingContext context, Size size)
    {
        var center = new Point(size.Width / 2.0, size.Height / 2.0);
        PaintRadialReaction(context, center, _inactiveReactionColor);

        var origin = new Point(
            center.X - (EdgeSize / 2.0),
            center.Y - (EdgeSize / 2.0));
        double normalized = Position.Status is AnimationStatus.Forward or AnimationStatus.Completed
            ? Position.Value
            : 1.0 - Position.Value;

        if (_previousValue == false || _value == false)
        {
            double t = _value == false ? 1.0 - normalized : normalized;
            if (t <= 0.5)
            {
                BorderSide? side = MaterialThemeLerp.BorderSide(
                    _inactiveSide,
                    _activeSide,
                    t * 2.0);
                DrawBox(context, origin, t, side);
                return;
            }

            DrawBox(context, origin, t, _activeSide);
            double shrink = (t - 0.5) * 2.0;
            if (_value is null || _previousValue is null)
            {
                DrawDash(context, origin, shrink);
            }
            else
            {
                DrawCheck(context, origin, shrink);
            }
            return;
        }

        DrawBox(context, origin, 1.0, _activeSide);
        if (normalized <= 0.5)
        {
            double shrink = 1.0 - (normalized * 2.0);
            if (_previousValue is null)
            {
                DrawDash(context, origin, shrink);
            }
            else
            {
                DrawCheck(context, origin, shrink);
            }
        }
        else
        {
            double shrink = (normalized - 0.5) * 2.0;
            if (_value is null)
            {
                DrawDash(context, origin, shrink);
            }
            else
            {
                DrawCheck(context, origin, shrink);
            }
        }
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate)
    {
        return !ReferenceEquals(this, oldDelegate);
    }

    private void DrawBox(
        PaintingContext context,
        Point origin,
        double t,
        BorderSide? side)
    {
        Rect rect = OuterRectAt(origin, t);
        Color color = ColorAt(t);
        var brush = new SolidColorBrush(color);
        Pen? pen = side is { Width: > 0.0 }
            ? new Pen(new SolidColorBrush(side.Value.Color), side.Value.Width)
            : null;
        if (_shape is CircleBorder)
        {
            double radius = Math.Min(rect.Width, rect.Height) / 2.0;
            context.DrawCircle(brush, pen, rect.Center, radius);
            return;
        }

        context.DrawRectangle(brush, pen, rect, ShapeBorderGeometry.ResolveRadius(_shape));
    }

    private Rect OuterRectAt(Point origin, double t)
    {
        double inset = 1.0 - (Math.Abs(t - 0.5) * 2.0);
        double size = EdgeSize - (inset * 2.0);
        return new Rect(
            origin.X + inset,
            origin.Y + inset,
            size,
            size);
    }

    private Color ColorAt(double t)
    {
        return t >= 0.25
            ? _activeColor
            : LerpColor(_inactiveColor, _activeColor, t * 4.0);
    }

    private void DrawCheck(PaintingContext context, Point origin, double t)
    {
        Point start = origin + new Vector(EdgeSize * 0.15, EdgeSize * 0.45);
        Point middle = origin + new Vector(EdgeSize * 0.40, EdgeSize * 0.70);
        Point end = origin + new Vector(EdgeSize * 0.85, EdgeSize * 0.25);
        var pen = new Pen(new SolidColorBrush(_checkColor), StrokeWidth);
        if (t <= 0.5)
        {
            context.DrawLine(pen, start, LerpPoint(start, middle, t * 2.0));
            return;
        }

        context.DrawLine(pen, start, middle);
        context.DrawLine(pen, middle, LerpPoint(middle, end, (t - 0.5) * 2.0));
    }

    private void DrawDash(PaintingContext context, Point origin, double t)
    {
        Point start = origin + new Vector(EdgeSize * 0.20, EdgeSize * 0.50);
        Point middle = origin + new Vector(EdgeSize * 0.50, EdgeSize * 0.50);
        Point end = origin + new Vector(EdgeSize * 0.80, EdgeSize * 0.50);
        var pen = new Pen(new SolidColorBrush(_checkColor), StrokeWidth);
        context.DrawLine(
            pen,
            LerpPoint(middle, start, t),
            LerpPoint(middle, end, t));
    }

    private static Point LerpPoint(Point from, Point to, double t)
    {
        double clampedT = Math.Clamp(t, 0.0, 1.0);
        return new Point(
            from.X + ((to.X - from.X) * clampedT),
            from.Y + ((to.Y - from.Y) * clampedT));
    }
}
