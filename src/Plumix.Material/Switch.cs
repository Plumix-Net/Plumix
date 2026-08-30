using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using BoxShadow = Plumix.Rendering.BoxShadow;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/switch.dart

internal enum SwitchType
{
    Material,
    Adaptive
}

public sealed class Switch : StatelessWidget
{
    private readonly SwitchType _switchType;

    public Switch(
        bool value,
        Action<bool>? onChanged,
        Color? activeColor = null,
        Color? activeThumbColor = null,
        Color? activeTrackColor = null,
        Color? inactiveThumbColor = null,
        Color? inactiveTrackColor = null,
        ImageProvider? activeThumbImage = null,
        ImageErrorListener? onActiveThumbImageError = null,
        ImageProvider? inactiveThumbImage = null,
        ImageErrorListener? onInactiveThumbImageError = null,
        MaterialStateProperty<Color?>? thumbColor = null,
        MaterialStateProperty<Color?>? trackColor = null,
        MaterialStateProperty<Color?>? trackOutlineColor = null,
        MaterialStateProperty<double?>? trackOutlineWidth = null,
        MaterialStateProperty<Icon?>? thumbIcon = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        MouseCursor? mouseCursor = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        double? splashRadius = null,
        FocusNode? focusNode = null,
        Action<bool>? onFocusChange = null,
        bool autofocus = false,
        Thickness? padding = null,
        Key? key = null) : this(
            switchType: SwitchType.Material,
            applyCupertinoTheme: false,
            value: value,
            onChanged: onChanged,
            activeColor: activeColor,
            activeThumbColor: activeThumbColor,
            activeTrackColor: activeTrackColor,
            inactiveThumbColor: inactiveThumbColor,
            inactiveTrackColor: inactiveTrackColor,
            activeThumbImage: activeThumbImage,
            onActiveThumbImageError: onActiveThumbImageError,
            inactiveThumbImage: inactiveThumbImage,
            onInactiveThumbImageError: onInactiveThumbImageError,
            thumbColor: thumbColor,
            trackColor: trackColor,
            trackOutlineColor: trackOutlineColor,
            trackOutlineWidth: trackOutlineWidth,
            thumbIcon: thumbIcon,
            materialTapTargetSize: materialTapTargetSize,
            dragStartBehavior: dragStartBehavior,
            mouseCursor: mouseCursor,
            overlayColor: overlayColor,
            focusColor: focusColor,
            hoverColor: hoverColor,
            splashRadius: splashRadius,
            focusNode: focusNode,
            onFocusChange: onFocusChange,
            autofocus: autofocus,
            padding: padding,
            key: key)
    {
    }

    private Switch(
        SwitchType switchType,
        bool? applyCupertinoTheme,
        bool value,
        Action<bool>? onChanged,
        Color? activeColor,
        Color? activeThumbColor,
        Color? activeTrackColor,
        Color? inactiveThumbColor,
        Color? inactiveTrackColor,
        ImageProvider? activeThumbImage,
        ImageErrorListener? onActiveThumbImageError,
        ImageProvider? inactiveThumbImage,
        ImageErrorListener? onInactiveThumbImageError,
        MaterialStateProperty<Color?>? thumbColor,
        MaterialStateProperty<Color?>? trackColor,
        MaterialStateProperty<Color?>? trackOutlineColor,
        MaterialStateProperty<double?>? trackOutlineWidth,
        MaterialStateProperty<Icon?>? thumbIcon,
        MaterialTapTargetSize? materialTapTargetSize,
        DragStartBehavior dragStartBehavior,
        MouseCursor? mouseCursor,
        MaterialStateProperty<Color?>? overlayColor,
        Color? focusColor,
        Color? hoverColor,
        double? splashRadius,
        FocusNode? focusNode,
        Action<bool>? onFocusChange,
        bool autofocus,
        Thickness? padding,
        Key? key) : base(key)
    {
        if (activeThumbImage is null && onActiveThumbImageError is not null)
        {
            throw new ArgumentException(
                "onActiveThumbImageError requires activeThumbImage.",
                nameof(onActiveThumbImageError));
        }

        if (inactiveThumbImage is null && onInactiveThumbImageError is not null)
        {
            throw new ArgumentException(
                "onInactiveThumbImageError requires inactiveThumbImage.",
                nameof(onInactiveThumbImageError));
        }

        _switchType = switchType;
        ApplyCupertinoTheme = applyCupertinoTheme;
        Value = value;
        OnChanged = onChanged;
        ActiveColor = activeColor;
        ActiveThumbColor = activeThumbColor;
        ActiveTrackColor = activeTrackColor;
        InactiveThumbColor = inactiveThumbColor;
        InactiveTrackColor = inactiveTrackColor;
        ActiveThumbImage = activeThumbImage;
        OnActiveThumbImageError = onActiveThumbImageError;
        InactiveThumbImage = inactiveThumbImage;
        OnInactiveThumbImageError = onInactiveThumbImageError;
        ThumbColor = thumbColor;
        TrackColor = trackColor;
        TrackOutlineColor = trackOutlineColor;
        TrackOutlineWidth = trackOutlineWidth;
        ThumbIcon = thumbIcon;
        MaterialTapTargetSize = materialTapTargetSize;
        DragStartBehavior = dragStartBehavior;
        MouseCursor = mouseCursor;
        OverlayColor = overlayColor;
        FocusColor = focusColor;
        HoverColor = hoverColor;
        SplashRadius = splashRadius;
        FocusNode = focusNode;
        OnFocusChange = onFocusChange;
        Autofocus = autofocus;
        Padding = padding;
    }

    /// Creates a `Switch` that follows the ambient `ThemeData.platform`: Apple platforms get the
    /// Cupertino geometry, defaults and animation timings, every other platform the Material ones.
    /// Dart parity: `Switch.adaptive`.
    public static Switch Adaptive(
        bool value,
        Action<bool>? onChanged,
        Color? activeColor = null,
        Color? activeThumbColor = null,
        Color? activeTrackColor = null,
        Color? inactiveThumbColor = null,
        Color? inactiveTrackColor = null,
        ImageProvider? activeThumbImage = null,
        ImageErrorListener? onActiveThumbImageError = null,
        ImageProvider? inactiveThumbImage = null,
        ImageErrorListener? onInactiveThumbImageError = null,
        MaterialStateProperty<Color?>? thumbColor = null,
        MaterialStateProperty<Color?>? trackColor = null,
        MaterialStateProperty<Color?>? trackOutlineColor = null,
        MaterialStateProperty<double?>? trackOutlineWidth = null,
        MaterialStateProperty<Icon?>? thumbIcon = null,
        MaterialTapTargetSize? materialTapTargetSize = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        MouseCursor? mouseCursor = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        Color? focusColor = null,
        Color? hoverColor = null,
        double? splashRadius = null,
        FocusNode? focusNode = null,
        Action<bool>? onFocusChange = null,
        bool autofocus = false,
        Thickness? padding = null,
        bool? applyCupertinoTheme = null,
        Key? key = null)
    {
        return new Switch(
            switchType: SwitchType.Adaptive,
            applyCupertinoTheme: applyCupertinoTheme,
            value: value,
            onChanged: onChanged,
            activeColor: activeColor,
            activeThumbColor: activeThumbColor,
            activeTrackColor: activeTrackColor,
            inactiveThumbColor: inactiveThumbColor,
            inactiveTrackColor: inactiveTrackColor,
            activeThumbImage: activeThumbImage,
            onActiveThumbImageError: onActiveThumbImageError,
            inactiveThumbImage: inactiveThumbImage,
            onInactiveThumbImageError: onInactiveThumbImageError,
            thumbColor: thumbColor,
            trackColor: trackColor,
            trackOutlineColor: trackOutlineColor,
            trackOutlineWidth: trackOutlineWidth,
            thumbIcon: thumbIcon,
            materialTapTargetSize: materialTapTargetSize,
            dragStartBehavior: dragStartBehavior,
            mouseCursor: mouseCursor,
            overlayColor: overlayColor,
            focusColor: focusColor,
            hoverColor: hoverColor,
            splashRadius: splashRadius,
            focusNode: focusNode,
            onFocusChange: onFocusChange,
            autofocus: autofocus,
            padding: padding,
            key: key);
    }

    public bool Value { get; }

    public Action<bool>? OnChanged { get; }

    [Obsolete("Use ActiveThumbColor instead. Mirrors Flutter's deprecation after v3.31.0-2.0.pre.")]
    public Color? ActiveColor { get; }

    public Color? ActiveThumbColor { get; }

    public Color? ActiveTrackColor { get; }

    public Color? InactiveThumbColor { get; }

    public Color? InactiveTrackColor { get; }

    public ImageProvider? ActiveThumbImage { get; }

    public ImageErrorListener? OnActiveThumbImageError { get; }

    public ImageProvider? InactiveThumbImage { get; }

    public ImageErrorListener? OnInactiveThumbImageError { get; }

    public MaterialStateProperty<Color?>? ThumbColor { get; }

    public MaterialStateProperty<Color?>? TrackColor { get; }

    public MaterialStateProperty<Color?>? TrackOutlineColor { get; }

    public MaterialStateProperty<double?>? TrackOutlineWidth { get; }

    public MaterialStateProperty<Icon?>? ThumbIcon { get; }

    public MaterialTapTargetSize? MaterialTapTargetSize { get; }

    public DragStartBehavior DragStartBehavior { get; }

    public MouseCursor? MouseCursor { get; }

    public MaterialStateProperty<Color?>? OverlayColor { get; }

    public Color? FocusColor { get; }

    public Color? HoverColor { get; }

    public double? SplashRadius { get; }

    public FocusNode? FocusNode { get; }

    public Action<bool>? OnFocusChange { get; }

    public bool Autofocus { get; }

    public Thickness? Padding { get; }

    public bool? ApplyCupertinoTheme { get; }

    public override Widget Build(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);
        Color? effectiveActiveThumbColor = null;
        Color? effectiveActiveTrackColor = null;
#pragma warning disable CS0618
        Color? deprecatedActiveColor = ActiveColor;
#pragma warning restore CS0618
        switch (_switchType)
        {
            case SwitchType.Material:
                effectiveActiveThumbColor = deprecatedActiveColor;
                break;
            case SwitchType.Adaptive:
                switch (theme.Platform)
                {
                    case TargetPlatform.Android:
                    case TargetPlatform.Fuchsia:
                    case TargetPlatform.Linux:
                    case TargetPlatform.Windows:
                        effectiveActiveThumbColor = deprecatedActiveColor;
                        break;
                    case TargetPlatform.IOS:
                    case TargetPlatform.MacOS:
                        effectiveActiveTrackColor = deprecatedActiveColor;
                        break;
                }
                break;
        }

        return new MaterialSwitch(
            value: Value,
            onChanged: OnChanged,
            size: GetSwitchSize(context),
            switchType: _switchType,
            activeColor: deprecatedActiveColor,
            activeThumbColor: ActiveThumbColor ?? effectiveActiveThumbColor,
            activeTrackColor: ActiveTrackColor ?? effectiveActiveTrackColor,
            inactiveThumbColor: InactiveThumbColor,
            inactiveTrackColor: InactiveTrackColor,
            activeThumbImage: ActiveThumbImage,
            onActiveThumbImageError: OnActiveThumbImageError,
            inactiveThumbImage: InactiveThumbImage,
            onInactiveThumbImageError: OnInactiveThumbImageError,
            thumbColor: ThumbColor,
            trackColor: TrackColor,
            trackOutlineColor: TrackOutlineColor,
            trackOutlineWidth: TrackOutlineWidth,
            thumbIcon: ThumbIcon,
            materialTapTargetSize: MaterialTapTargetSize,
            dragStartBehavior: DragStartBehavior,
            mouseCursor: MouseCursor,
            overlayColor: OverlayColor,
            focusColor: FocusColor,
            hoverColor: HoverColor,
            splashRadius: SplashRadius,
            focusNode: FocusNode,
            onFocusChange: OnFocusChange,
            autofocus: Autofocus,
            applyCupertinoTheme: ApplyCupertinoTheme);
    }

    private Size GetSwitchSize(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);
        SwitchThemeData switchTheme = SwitchTheme.Of(context);
        if (_switchType == SwitchType.Adaptive)
        {
            Adaptation<SwitchThemeData> adaptation =
                theme.GetAdaptation<SwitchThemeData>() ?? new SwitchThemeAdaptation();
            switchTheme = adaptation.Adapt(theme, switchTheme);
        }

        SwitchThemeData defaults = theme.UseMaterial3
            ? SwitchDefaults.Material3(context)
            : SwitchDefaults.Material2(context);
        SwitchConfig switchConfig = theme.UseMaterial3
            ? new SwitchConfigM3(context)
            : new SwitchConfigM2();
        MaterialTapTargetSize effectiveMaterialTapTargetSize = MaterialTapTargetSize
                                                               ?? switchTheme.MaterialTapTargetSize
                                                               ?? theme.MaterialTapTargetSize;
        Thickness effectivePadding = Padding ?? switchTheme.Padding ?? defaults.Padding!.Value;
        double horizontal = effectivePadding.Left + effectivePadding.Right;
        double vertical = effectivePadding.Top + effectivePadding.Bottom;
        return effectiveMaterialTapTargetSize == Plumix.Material.MaterialTapTargetSize.Padded
            ? new Size(switchConfig.SwitchWidth + horizontal, switchConfig.SwitchHeight + vertical)
            : new Size(
                switchConfig.SwitchWidth + horizontal,
                switchConfig.SwitchHeightCollapsed + vertical);
    }
}

/// Dart's `_SwitchThemeAdaptation`: on Apple platforms `Switch.adaptive` ignores the ambient
/// `SwitchThemeData` entirely; everywhere else it keeps it.
internal sealed class SwitchThemeAdaptation : Adaptation<SwitchThemeData>
{
    public override SwitchThemeData Adapt(ThemeData theme, SwitchThemeData defaultValue)
    {
        return theme.Platform switch
        {
            TargetPlatform.IOS or TargetPlatform.MacOS => new SwitchThemeData(),
            _ => defaultValue,
        };
    }
}

/// Dart's `_MaterialSwitch`: the stateful half of `Switch`, sized by `Switch._getSwitchSize`.
internal sealed class MaterialSwitch : StatefulWidget
{
    public MaterialSwitch(
        bool value,
        Action<bool>? onChanged,
        Size size,
        SwitchType switchType,
        Color? activeColor,
        Color? activeThumbColor,
        Color? activeTrackColor,
        Color? inactiveThumbColor,
        Color? inactiveTrackColor,
        ImageProvider? activeThumbImage,
        ImageErrorListener? onActiveThumbImageError,
        ImageProvider? inactiveThumbImage,
        ImageErrorListener? onInactiveThumbImageError,
        MaterialStateProperty<Color?>? thumbColor,
        MaterialStateProperty<Color?>? trackColor,
        MaterialStateProperty<Color?>? trackOutlineColor,
        MaterialStateProperty<double?>? trackOutlineWidth,
        MaterialStateProperty<Icon?>? thumbIcon,
        MaterialTapTargetSize? materialTapTargetSize,
        DragStartBehavior dragStartBehavior,
        MouseCursor? mouseCursor,
        MaterialStateProperty<Color?>? overlayColor,
        Color? focusColor,
        Color? hoverColor,
        double? splashRadius,
        FocusNode? focusNode,
        Action<bool>? onFocusChange,
        bool autofocus,
        bool? applyCupertinoTheme,
        Key? key = null) : base(key)
    {
        Value = value;
        OnChanged = onChanged;
        Size = size;
        SwitchType = switchType;
        ActiveColor = activeColor;
        ActiveThumbColor = activeThumbColor;
        ActiveTrackColor = activeTrackColor;
        InactiveThumbColor = inactiveThumbColor;
        InactiveTrackColor = inactiveTrackColor;
        ActiveThumbImage = activeThumbImage;
        OnActiveThumbImageError = onActiveThumbImageError;
        InactiveThumbImage = inactiveThumbImage;
        OnInactiveThumbImageError = onInactiveThumbImageError;
        ThumbColor = thumbColor;
        TrackColor = trackColor;
        TrackOutlineColor = trackOutlineColor;
        TrackOutlineWidth = trackOutlineWidth;
        ThumbIcon = thumbIcon;
        MaterialTapTargetSize = materialTapTargetSize;
        DragStartBehavior = dragStartBehavior;
        MouseCursor = mouseCursor;
        OverlayColor = overlayColor;
        FocusColor = focusColor;
        HoverColor = hoverColor;
        SplashRadius = splashRadius;
        FocusNode = focusNode;
        OnFocusChange = onFocusChange;
        Autofocus = autofocus;
        ApplyCupertinoTheme = applyCupertinoTheme;
    }

    public bool Value { get; }

    public Action<bool>? OnChanged { get; }

    public Size Size { get; }

    public SwitchType SwitchType { get; }

    public Color? ActiveColor { get; }

    public Color? ActiveThumbColor { get; }

    public Color? ActiveTrackColor { get; }

    public Color? InactiveThumbColor { get; }

    public Color? InactiveTrackColor { get; }

    public ImageProvider? ActiveThumbImage { get; }

    public ImageErrorListener? OnActiveThumbImageError { get; }

    public ImageProvider? InactiveThumbImage { get; }

    public ImageErrorListener? OnInactiveThumbImageError { get; }

    public MaterialStateProperty<Color?>? ThumbColor { get; }

    public MaterialStateProperty<Color?>? TrackColor { get; }

    public MaterialStateProperty<Color?>? TrackOutlineColor { get; }

    public MaterialStateProperty<double?>? TrackOutlineWidth { get; }

    public MaterialStateProperty<Icon?>? ThumbIcon { get; }

    public MaterialTapTargetSize? MaterialTapTargetSize { get; }

    public DragStartBehavior DragStartBehavior { get; }

    public MouseCursor? MouseCursor { get; }

    public MaterialStateProperty<Color?>? OverlayColor { get; }

    public Color? FocusColor { get; }

    public Color? HoverColor { get; }

    public double? SplashRadius { get; }

    public FocusNode? FocusNode { get; }

    public Action<bool>? OnFocusChange { get; }

    public bool Autofocus { get; }

    public bool? ApplyCupertinoTheme { get; }

    public override State CreateState() => new MaterialSwitchState();
}

/// Dart's `_MaterialSwitchState`.
internal sealed class MaterialSwitchState : ToggleableState
{
    private SwitchPainter? _painter;
    private bool _needsPositionAnimation;

    private MaterialSwitch CurrentWidget => (MaterialSwitch)StateWidget;

    protected override bool IsInteractive => CurrentWidget.OnChanged is not null;

    protected override bool IsValueSelected => CurrentWidget.Value;

    internal SwitchPainter Painter => _painter!;

    public override void InitState()
    {
        base.InitState();
        _painter = new SwitchPainter(
            Position,
            Reaction,
            ReactionHoverFade,
            ReactionFocusFade,
            PositionController);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var previous = (MaterialSwitch)oldWidget;
        if (previous.Value == CurrentWidget.Value)
        {
            return;
        }

        // During a drag we may not be at the end of the animation; the curve is only reset once the
        // controller has settled, so an interrupted drag keeps its linear curve.
        if (Position.Value is 0.0 or 1.0)
        {
            if (CurrentWidget.SwitchType == SwitchType.Adaptive
                && Theme.Of(Context).Platform is TargetPlatform.IOS or TargetPlatform.MacOS)
            {
                PositionAnimation.Curve = Curves.Linear;
                PositionAnimation.ReverseCurve = Curves.Linear;
            }
            else
            {
                UpdateCurve();
            }
        }

        AnimateToValue(CurrentWidget.Value, tristate: false);
    }

    public override void Dispose()
    {
        _painter?.Dispose();
        _painter = null;
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        if (_needsPositionAnimation)
        {
            _needsPositionAnimation = false;
            AnimateToValue(CurrentWidget.Value, tristate: false);
        }

        ThemeData theme = Theme.Of(context);
        SwitchThemeData switchTheme = SwitchTheme.Of(context);
        if (CurrentWidget.SwitchType == SwitchType.Adaptive)
        {
            Adaptation<SwitchThemeData> adaptation =
                theme.GetAdaptation<SwitchThemeData>() ?? new SwitchThemeAdaptation();
            switchTheme = adaptation.Adapt(theme, switchTheme);
        }

        bool isCupertino = CurrentWidget.SwitchType == SwitchType.Adaptive
                           && theme.Platform is TargetPlatform.IOS or TargetPlatform.MacOS;
        bool applyCupertinoTheme = false;
        double disabledOpacity = 1.0;
        SwitchConfig switchConfig;
        SwitchThemeData defaults;
        if (isCupertino)
        {
            applyCupertinoTheme = CurrentWidget.ApplyCupertinoTheme
                                  ?? theme.CupertinoOverrideTheme?.ApplyThemeToAll
                                  ?? false;
            disabledOpacity = 0.5;
            switchConfig = new SwitchConfigCupertino(context);
            defaults = SwitchDefaults.Cupertino(context);
            ReactionController.Duration = TimeSpan.FromMilliseconds(200);
        }
        else
        {
            switchConfig = theme.UseMaterial3 ? new SwitchConfigM3(context) : new SwitchConfigM2();
            defaults = theme.UseMaterial3
                ? SwitchDefaults.Material3(context)
                : SwitchDefaults.Material2(context);
        }

        PositionController.Duration = TimeSpan.FromMilliseconds(switchConfig.ToggleDuration);
        Color cupertinoPrimaryColor = theme.CupertinoOverrideTheme?.PrimaryColor is { } overrideColor
            ? CupertinoDynamicColor.Resolve(overrideColor, context)
            : theme.ColorScheme.Primary;

        MaterialState baseStates = ToMaterialState(CurrentWidgetStates);
        MaterialState activeStates = baseStates | MaterialState.Selected;
        MaterialState inactiveStates = baseStates & ~MaterialState.Selected;
        MaterialState focusedStates = baseStates | MaterialState.Focused;
        MaterialState hoveredStates = baseStates | MaterialState.Hovered;
        MaterialState activePressedStates = activeStates | MaterialState.Pressed;
        MaterialState inactivePressedStates = inactiveStates | MaterialState.Pressed;

        Color? activeThumbColor = CurrentWidget.ThumbColor?.Resolve(activeStates)
                                  ?? CurrentWidget.ActiveThumbColor
                                  ?? switchTheme.ThumbColor?.Resolve(activeStates);
        Color effectiveActiveThumbColor = activeThumbColor
                                          ?? defaults.ThumbColor!.Resolve(activeStates)!.Value;
        Color? inactiveThumbColor = CurrentWidget.ThumbColor?.Resolve(inactiveStates)
                                    ?? CurrentWidget.InactiveThumbColor
                                    ?? switchTheme.ThumbColor?.Resolve(inactiveStates);
        Color effectiveInactiveThumbColor = inactiveThumbColor
                                            ?? defaults.ThumbColor!.Resolve(inactiveStates)!.Value;

        Color effectiveActiveTrackColor = CurrentWidget.TrackColor?.Resolve(activeStates)
                                          ?? CurrentWidget.ActiveTrackColor
                                          ?? (applyCupertinoTheme
                                              ? cupertinoPrimaryColor
                                              : switchTheme.TrackColor?.Resolve(activeStates))
                                          ?? WithAlpha(CurrentWidget.ActiveThumbColor, 0x80)
                                          ?? defaults.TrackColor!.Resolve(activeStates)!.Value;
        Color? effectiveActiveTrackOutlineColor =
            CurrentWidget.TrackOutlineColor?.Resolve(activeStates)
            ?? switchTheme.TrackOutlineColor?.Resolve(activeStates)
            ?? defaults.TrackOutlineColor!.Resolve(activeStates);
        double? effectiveActiveTrackOutlineWidth =
            CurrentWidget.TrackOutlineWidth?.Resolve(activeStates)
            ?? switchTheme.TrackOutlineWidth?.Resolve(activeStates)
            ?? defaults.TrackOutlineWidth?.Resolve(activeStates);

        Color effectiveInactiveTrackColor = CurrentWidget.TrackColor?.Resolve(inactiveStates)
                                            ?? CurrentWidget.InactiveTrackColor
                                            ?? switchTheme.TrackColor?.Resolve(inactiveStates)
                                            ?? defaults.TrackColor!.Resolve(inactiveStates)!.Value;
        Color? effectiveInactiveTrackOutlineColor =
            CurrentWidget.TrackOutlineColor?.Resolve(inactiveStates)
            ?? switchTheme.TrackOutlineColor?.Resolve(inactiveStates)
            ?? defaults.TrackOutlineColor?.Resolve(inactiveStates);
        double? effectiveInactiveTrackOutlineWidth =
            CurrentWidget.TrackOutlineWidth?.Resolve(inactiveStates)
            ?? switchTheme.TrackOutlineWidth?.Resolve(inactiveStates)
            ?? defaults.TrackOutlineWidth?.Resolve(inactiveStates);

        Icon? effectiveActiveIcon = CurrentWidget.ThumbIcon?.Resolve(activeStates)
                                    ?? switchTheme.ThumbIcon?.Resolve(activeStates);
        Icon? effectiveInactiveIcon = CurrentWidget.ThumbIcon?.Resolve(inactiveStates)
                                      ?? switchTheme.ThumbIcon?.Resolve(inactiveStates);
        Color effectiveActiveIconColor = effectiveActiveIcon?.Color
                                         ?? switchConfig.IconColor.Resolve(activeStates);
        Color effectiveInactiveIconColor = effectiveInactiveIcon?.Color
                                           ?? switchConfig.IconColor.Resolve(inactiveStates);

        Color effectiveFocusOverlayColor = CurrentWidget.OverlayColor?.Resolve(focusedStates)
                                           ?? CurrentWidget.FocusColor
                                           ?? switchTheme.OverlayColor?.Resolve(focusedStates)
                                           ?? (applyCupertinoTheme
                                               ? CupertinoFocusColor(cupertinoPrimaryColor)
                                               : (Color?)null)
                                           ?? defaults.OverlayColor!.Resolve(focusedStates)!.Value;
        Color effectiveHoverOverlayColor = CurrentWidget.OverlayColor?.Resolve(hoveredStates)
                                           ?? CurrentWidget.HoverColor
                                           ?? switchTheme.OverlayColor?.Resolve(hoveredStates)
                                           ?? defaults.OverlayColor!.Resolve(hoveredStates)!.Value;

        Color effectiveActivePressedThumbColor =
            CurrentWidget.ThumbColor?.Resolve(activePressedStates)
            ?? CurrentWidget.ActiveThumbColor
            ?? switchTheme.ThumbColor?.Resolve(activePressedStates)
            ?? defaults.ThumbColor!.Resolve(activePressedStates)!.Value;
        Color effectiveActivePressedOverlayColor =
            CurrentWidget.OverlayColor?.Resolve(activePressedStates)
            ?? switchTheme.OverlayColor?.Resolve(activePressedStates)
            ?? WithAlpha(activeThumbColor, RadialReactionAlpha)
            ?? defaults.OverlayColor!.Resolve(activePressedStates)!.Value;
        Color effectiveInactivePressedThumbColor =
            CurrentWidget.ThumbColor?.Resolve(inactivePressedStates)
            ?? CurrentWidget.InactiveThumbColor
            ?? switchTheme.ThumbColor?.Resolve(inactivePressedStates)
            ?? defaults.ThumbColor!.Resolve(inactivePressedStates)!.Value;
        Color effectiveInactivePressedOverlayColor =
            CurrentWidget.OverlayColor?.Resolve(inactivePressedStates)
            ?? switchTheme.OverlayColor?.Resolve(inactivePressedStates)
            ?? WithAlpha(inactiveThumbColor, RadialReactionAlpha)
            ?? defaults.OverlayColor!.Resolve(inactivePressedStates)!.Value;

        WidgetStateProperty<MouseCursor> effectiveMouseCursor =
            MaterialStateProperty<MouseCursor>.ResolveWith(states =>
                CurrentWidget.MouseCursor
                ?? switchTheme.MouseCursor?.Resolve(states)
                ?? defaults.MouseCursor!.Resolve(states)!);

        double effectiveActiveThumbRadius = effectiveActiveIcon is null
            ? switchConfig.ActiveThumbRadius
            : switchConfig.ThumbRadiusWithIcon;
        double effectiveInactiveThumbRadius =
            effectiveInactiveIcon is null && CurrentWidget.InactiveThumbImage is null
                ? switchConfig.InactiveThumbRadius
                : switchConfig.ThumbRadiusWithIcon;
        double effectiveSplashRadius = CurrentWidget.SplashRadius
                                       ?? switchTheme.SplashRadius
                                       ?? defaults.SplashRadius!.Value;

        _painter!.Configure(
            inactiveReactionColor: effectiveInactivePressedOverlayColor,
            reactionColor: effectiveActivePressedOverlayColor,
            hoverColor: effectiveHoverOverlayColor,
            focusColor: effectiveFocusOverlayColor,
            splashRadius: effectiveSplashRadius,
            downPosition: DownPosition,
            isFocused: IsFocused,
            isHovered: IsHovered,
            activeColor: effectiveActiveThumbColor,
            inactiveColor: effectiveInactiveThumbColor,
            activePressedColor: effectiveActivePressedThumbColor,
            inactivePressedColor: effectiveInactivePressedThumbColor,
            activeThumbImage: CurrentWidget.ActiveThumbImage,
            onActiveThumbImageError: CurrentWidget.OnActiveThumbImageError,
            inactiveThumbImage: CurrentWidget.InactiveThumbImage,
            onInactiveThumbImageError: CurrentWidget.OnInactiveThumbImageError,
            activeTrackColor: effectiveActiveTrackColor,
            activeTrackOutlineColor: effectiveActiveTrackOutlineColor,
            activeTrackOutlineWidth: effectiveActiveTrackOutlineWidth,
            inactiveTrackColor: effectiveInactiveTrackColor,
            inactiveTrackOutlineColor: effectiveInactiveTrackOutlineColor,
            inactiveTrackOutlineWidth: effectiveInactiveTrackOutlineWidth,
            configuration: ImageConfigurationUtils.CreateLocalImageConfiguration(context),
            isInteractive: IsInteractive,
            trackInnerLength: TrackInnerLength(switchConfig),
            textDirection: Directionality.Of(context),
            surfaceColor: theme.ColorScheme.Surface,
            inactiveThumbRadius: effectiveInactiveThumbRadius,
            activeThumbRadius: effectiveActiveThumbRadius,
            pressedThumbRadius: switchConfig.PressedThumbRadius,
            thumbOffset: switchConfig.ThumbOffset,
            trackHeight: switchConfig.TrackHeight,
            trackWidth: switchConfig.TrackWidth,
            activeIconColor: effectiveActiveIconColor,
            inactiveIconColor: effectiveInactiveIconColor,
            activeIcon: effectiveActiveIcon,
            inactiveIcon: effectiveInactiveIcon,
            iconTheme: IconTheme.Of(context),
            thumbShadow: switchConfig.ThumbShadow,
            transitionalThumbSize: switchConfig.TransitionalThumbSize,
            isCupertino: isCupertino);

        Widget toggleable = BuildToggleable(
            painter: _painter,
            size: CurrentWidget.Size,
            mouseCursor: effectiveMouseCursor,
            onTap: HandleTap,
            focusNode: CurrentWidget.FocusNode,
            onFocusChange: CurrentWidget.OnFocusChange,
            autofocus: CurrentWidget.Autofocus);

        return new Semantics(
            toggled: CurrentWidget.Value,
            child: new GestureDetector(
                excludeFromSemantics: true,
                onHorizontalDragStart: HandleDragStart,
                onHorizontalDragUpdate: HandleDragUpdate,
                onHorizontalDragEnd: HandleDragEnd,
                dragStartBehavior: CurrentWidget.DragStartBehavior,
                child: new Opacity(
                    IsInteractive ? 1.0 : disabledOpacity,
                    toggleable)));
    }

    internal const byte RadialReactionAlpha = 0x1F;

    internal static double TrackInnerLength(SwitchConfig config)
    {
        return config.TrackWidth - (2.0 * (config.TrackHeight / 2.0));
    }

    internal static MaterialState ToMaterialState(IReadOnlySet<WidgetState> states)
    {
        var result = MaterialState.None;
        foreach (WidgetState state in states)
        {
            result |= state switch
            {
                WidgetState.Hovered => MaterialState.Hovered,
                WidgetState.Focused => MaterialState.Focused,
                WidgetState.Pressed => MaterialState.Pressed,
                WidgetState.Disabled => MaterialState.Disabled,
                WidgetState.Selected => MaterialState.Selected,
                WidgetState.Dragged => MaterialState.Dragged,
                WidgetState.Error => MaterialState.Error,
                _ => MaterialState.None,
            };
        }

        return result;
    }

    private void UpdateCurve()
    {
        if (Theme.Of(Context).UseMaterial3)
        {
            PositionAnimation.Curve = Curves.EaseOutBack;
            PositionAnimation.ReverseCurve = Curves.Flipped(Curves.EaseOutBack);
        }
        else
        {
            PositionAnimation.Curve = Curves.EaseIn;
            PositionAnimation.ReverseCurve = Curves.EaseOut;
        }
    }

    private void HandleTap()
    {
        if (!IsInteractive)
        {
            return;
        }

        CurrentWidget.OnChanged?.Invoke(!CurrentWidget.Value);
        Context.FindRenderObject()?.SendSemanticsEvent(new TapSemanticEvent());
    }

    private void HandleDragStart(DragStartDetails details)
    {
        _ = details;
        if (IsInteractive)
        {
            ReactionController.Forward();
        }
    }

    private void HandleDragUpdate(DragUpdateDetails details)
    {
        if (!IsInteractive)
        {
            return;
        }

        PositionAnimation.Curve = Curves.Linear;
        PositionAnimation.ReverseCurve = null;
        SwitchConfig config = ResolveConfigForDrag();
        double delta = (details.PrimaryDelta ?? 0.0) / TrackInnerLength(config);
        PositionController.SetValue(PositionController.Value + (
            Directionality.Of(Context) == TextDirection.Rtl ? -delta : delta));
    }

    private void HandleDragEnd(DragEndDetails details)
    {
        _ = details;
        if (Position.Value >= 0.5 != CurrentWidget.Value)
        {
            CurrentWidget.OnChanged?.Invoke(!CurrentWidget.Value);
            SetState(() => _needsPositionAnimation = true);
        }
        else
        {
            AnimateToValue(CurrentWidget.Value, tristate: false);
        }

        ReactionController.Reverse();
    }

    private SwitchConfig ResolveConfigForDrag()
    {
        ThemeData theme = Theme.Of(Context);
        if (CurrentWidget.SwitchType == SwitchType.Adaptive
            && theme.Platform is TargetPlatform.IOS or TargetPlatform.MacOS)
        {
            return new SwitchConfigCupertino(Context);
        }

        return theme.UseMaterial3 ? new SwitchConfigM3(Context) : new SwitchConfigM2();
    }

    private static Color? WithAlpha(Color? color, byte alpha)
    {
        return color.HasValue
            ? Color.FromArgb(alpha, color.Value.R, color.Value.G, color.Value.B)
            : null;
    }

    /// Dart's `HSLColor.fromColor(primary.withOpacity(0.80)).withLightness(0.69)
    /// .withSaturation(0.835).toColor()`, used for the Cupertino focus ring.
    internal static Color CupertinoFocusColor(Color primary)
    {
        double red = primary.R / 255.0;
        double green = primary.G / 255.0;
        double blue = primary.B / 255.0;
        double maximum = Math.Max(red, Math.Max(green, blue));
        double minimum = Math.Min(red, Math.Min(green, blue));
        double delta = maximum - minimum;
        double hue;
        if (delta == 0.0)
        {
            hue = 0.0;
        }
        else if (maximum == red)
        {
            hue = ((green - blue) / delta) % 6.0;
        }
        else if (maximum == green)
        {
            hue = ((blue - red) / delta) + 2.0;
        }
        else
        {
            hue = ((red - green) / delta) + 4.0;
        }

        hue /= 6.0;
        if (hue < 0.0)
        {
            hue += 1.0;
        }

        const double saturation = 0.835;
        const double lightness = 0.69;
        double chroma = (1.0 - Math.Abs((2.0 * lightness) - 1.0)) * saturation;
        double hueSection = hue * 6.0;
        double secondary = chroma * (1.0 - Math.Abs((hueSection % 2.0) - 1.0));
        (double redPrime, double greenPrime, double bluePrime) = hueSection switch
        {
            < 1.0 => (chroma, secondary, 0.0),
            < 2.0 => (secondary, chroma, 0.0),
            < 3.0 => (0.0, chroma, secondary),
            < 4.0 => (0.0, secondary, chroma),
            < 5.0 => (secondary, 0.0, chroma),
            _ => (chroma, 0.0, secondary)
        };
        double match = lightness - (chroma / 2.0);
        return Color.FromArgb(
            0xCC,
            ToChannel(redPrime + match),
            ToChannel(greenPrime + match),
            ToChannel(bluePrime + match));
    }

    private static byte ToChannel(double value)
    {
        return (byte)Math.Clamp((int)Math.Round(value * 255.0), 0, 255);
    }
}

/// Dart's `_SwitchConfig` mixin: the geometry and motion constants of one switch flavour.
internal abstract class SwitchConfig
{
    public abstract double TrackHeight { get; }

    public abstract double TrackWidth { get; }

    public abstract double SwitchWidth { get; }

    public abstract double SwitchHeight { get; }

    public abstract double SwitchHeightCollapsed { get; }

    public abstract double ActiveThumbRadius { get; }

    public abstract double InactiveThumbRadius { get; }

    public abstract double PressedThumbRadius { get; }

    public abstract double ThumbRadiusWithIcon { get; }

    public abstract IReadOnlyList<BoxShadow>? ThumbShadow { get; }

    public abstract MaterialStateProperty<Color> IconColor { get; }

    public abstract double? ThumbOffset { get; }

    public abstract Size TransitionalThumbSize { get; }

    public abstract int ToggleDuration { get; }

    public abstract Size SwitchMinSize { get; }
}

/// Dart's `_SwitchConfigM2`.
internal sealed class SwitchConfigM2 : SwitchConfig
{
    public override double ActiveThumbRadius => 10.0;

    public override double InactiveThumbRadius => 10.0;

    public override double PressedThumbRadius => 10.0;

    public override double ThumbRadiusWithIcon => 10.0;

    public override double TrackHeight => 14.0;

    public override double TrackWidth => 33.0;

    public override Size SwitchMinSize => new(
        WidgetConstants.MinInteractiveDimension - 8.0,
        WidgetConstants.MinInteractiveDimension - 8.0);

    public override double SwitchWidth =>
        TrackWidth - (2.0 * (TrackHeight / 2.0)) + SwitchMinSize.Width;

    public override double SwitchHeight => SwitchMinSize.Height + 8.0;

    public override double SwitchHeightCollapsed => SwitchMinSize.Height;

    public override double? ThumbOffset => 0.5;

    public override Size TransitionalThumbSize => new(20.0, 20.0);

    public override int ToggleDuration => 200;

    public override IReadOnlyList<BoxShadow>? ThumbShadow => MaterialShadows.ElevationToShadow[1];

    public override MaterialStateProperty<Color> IconColor =>
        MaterialStateProperty<Color>.All(Colors.Transparent);
}

/// Dart's `_SwitchConfigM3`.
internal sealed class SwitchConfigM3 : SwitchConfig
{
    internal const double IconSize = 16.0;

    private readonly ColorScheme _colors;

    public SwitchConfigM3(BuildContext context)
    {
        _colors = Theme.Of(context).ColorScheme;
    }

    public override double ActiveThumbRadius => 24.0 / 2;

    public override double InactiveThumbRadius => 16.0 / 2;

    public override double PressedThumbRadius => 28.0 / 2;

    public override double ThumbRadiusWithIcon => 24.0 / 2;

    public override double TrackHeight => 32.0;

    public override double TrackWidth => 52.0;

    public override double SwitchWidth => 52.0;

    public override Size SwitchMinSize => new(
        WidgetConstants.MinInteractiveDimension,
        WidgetConstants.MinInteractiveDimension - 8.0);

    public override double SwitchHeight => SwitchMinSize.Height + 8.0;

    public override double SwitchHeightCollapsed => 40.0;

    public override double? ThumbOffset => null;

    public override Size TransitionalThumbSize => new(34.0, 22.0);

    public override int ToggleDuration => 300;

    public override IReadOnlyList<BoxShadow>? ThumbShadow => MaterialShadows.ElevationToShadow[0];

    public override MaterialStateProperty<Color> IconColor =>
        MaterialStateProperty<Color>.ResolveWith(states =>
        {
            if (states.HasFlag(MaterialState.Disabled))
            {
                return states.HasFlag(MaterialState.Selected)
                    ? _colors.OnSurface.WithOpacity(0.38)
                    : _colors.SurfaceContainerHighest.WithOpacity(0.38);
            }

            return states.HasFlag(MaterialState.Selected)
                ? _colors.OnPrimaryContainer
                : _colors.SurfaceContainerHighest;
        });
}

/// Dart's `_SwitchConfigCupertino`, used by `Switch.adaptive` on iOS and macOS.
internal sealed class SwitchConfigCupertino : SwitchConfig
{
    internal const double FocusTrackOutline = 3.5;

    private static readonly IReadOnlyList<BoxShadow> CupertinoThumbShadow =
    [
        new BoxShadow(Color.FromArgb(0x26, 0x00, 0x00, 0x00), new Point(0.0, 3.0), 8.0),
        new BoxShadow(Color.FromArgb(0x0F, 0x00, 0x00, 0x00), new Point(0.0, 3.0), 1.0),
    ];

    private readonly ColorScheme _colors;

    public SwitchConfigCupertino(BuildContext context)
    {
        _colors = Theme.Of(context).ColorScheme;
    }

    public override double ActiveThumbRadius => 14.0;

    public override double InactiveThumbRadius => 14.0;

    public override double PressedThumbRadius => 14.0;

    public override double ThumbRadiusWithIcon => 14.0;

    public override double TrackHeight => 31.0;

    public override double TrackWidth => 51.0;

    public override double SwitchWidth => 60.0;

    public override Size SwitchMinSize => new(
        WidgetConstants.MinInteractiveDimension - 8.0,
        WidgetConstants.MinInteractiveDimension - 8.0);

    public override double SwitchHeight => SwitchMinSize.Height + 8.0;

    public override double SwitchHeightCollapsed => SwitchMinSize.Height;

    public override double? ThumbOffset => null;

    public override Size TransitionalThumbSize => new(28.0, 28.0);

    public override int ToggleDuration => 140;

    public override IReadOnlyList<BoxShadow>? ThumbShadow => CupertinoThumbShadow;

    public override MaterialStateProperty<Color> IconColor =>
        MaterialStateProperty<Color>.ResolveWith(states => states.HasFlag(MaterialState.Disabled)
            ? _colors.OnSurface.WithOpacity(0.38)
            : _colors.OnPrimaryContainer);
}

/// Dart's `_SwitchDefaultsM2`, `_SwitchDefaultsM3` and `_SwitchDefaultsCupertino`.
internal static class SwitchDefaults
{
    /// Dart's `kRadialReactionRadius`.
    internal const double RadialReactionRadius = 20.0;

    public static SwitchThemeData Material2(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);
        ColorScheme colors = theme.ColorScheme;
        bool isDark = theme.Brightness == Brightness.Dark;
        return new SwitchThemeData(
            ThumbColor: MaterialStateProperty<Color?>.ResolveWith(states =>
            {
                if (states.HasFlag(MaterialState.Disabled))
                {
                    return isDark ? Colors.Grey.Shade800 : Colors.Grey.Shade400;
                }

                if (states.HasFlag(MaterialState.Selected))
                {
                    return colors.Secondary;
                }

                return isDark ? Colors.Grey.Shade400 : Colors.Grey.Shade50;
            }),
            TrackColor: MaterialStateProperty<Color?>.ResolveWith(states =>
            {
                if (states.HasFlag(MaterialState.Disabled))
                {
                    return isDark
                        ? Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF)
                        : Color.FromArgb(0x1F, 0x00, 0x00, 0x00);
                }

                if (states.HasFlag(MaterialState.Selected))
                {
                    return Color.FromArgb(
                        0x80,
                        colors.Secondary.R,
                        colors.Secondary.G,
                        colors.Secondary.B);
                }

                return isDark
                    ? Color.FromArgb(0x4D, 0xFF, 0xFF, 0xFF)
                    : Color.FromArgb(0x52, 0x00, 0x00, 0x00);
            }),
            TrackOutlineColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
            MaterialTapTargetSize: theme.MaterialTapTargetSize,
            MouseCursor: MaterialStateProperty<MouseCursor?>.ResolveWith(
                states => WidgetStateMouseCursor.Clickable.Resolve(FromMaterialState(states))),
            OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
            {
                if (states.HasFlag(MaterialState.Pressed))
                {
                    Color? thumb = states.HasFlag(MaterialState.Disabled)
                        ? isDark ? Colors.Grey.Shade800 : Colors.Grey.Shade400
                        : states.HasFlag(MaterialState.Selected)
                            ? colors.Secondary
                            : isDark ? Colors.Grey.Shade400 : Colors.Grey.Shade50;
                    return Color.FromArgb(
                        MaterialSwitchState.RadialReactionAlpha,
                        thumb.Value.R,
                        thumb.Value.G,
                        thumb.Value.B);
                }

                if (states.HasFlag(MaterialState.Hovered))
                {
                    return theme.HoverColor;
                }

                if (states.HasFlag(MaterialState.Focused))
                {
                    return theme.FocusColor;
                }

                return null;
            }),
            SplashRadius: RadialReactionRadius,
            Padding: new Thickness(0.0));
    }

    public static SwitchThemeData Material3(BuildContext context)
    {
        ColorScheme colors = Theme.Of(context).ColorScheme;
        return new SwitchThemeData(
            ThumbColor: MaterialStateProperty<Color?>.ResolveWith(states =>
            {
                if (states.HasFlag(MaterialState.Disabled))
                {
                    return states.HasFlag(MaterialState.Selected)
                        ? colors.Surface.WithOpacity(1.0)
                        : colors.OnSurface.WithOpacity(0.38);
                }

                if (states.HasFlag(MaterialState.Selected))
                {
                    return IsInteracting(states) ? colors.PrimaryContainer : colors.OnPrimary;
                }

                return IsInteracting(states) ? colors.OnSurfaceVariant : colors.Outline;
            }),
            TrackColor: MaterialStateProperty<Color?>.ResolveWith(states =>
            {
                if (states.HasFlag(MaterialState.Disabled))
                {
                    return states.HasFlag(MaterialState.Selected)
                        ? colors.OnSurface.WithOpacity(0.12)
                        : colors.SurfaceContainerHighest.WithOpacity(0.12);
                }

                return states.HasFlag(MaterialState.Selected)
                    ? colors.Primary
                    : colors.SurfaceContainerHighest;
            }),
            TrackOutlineColor: MaterialStateProperty<Color?>.ResolveWith(states =>
            {
                if (states.HasFlag(MaterialState.Selected))
                {
                    return Colors.Transparent;
                }

                return states.HasFlag(MaterialState.Disabled)
                    ? colors.OnSurface.WithOpacity(0.12)
                    : colors.Outline;
            }),
            TrackOutlineWidth: MaterialStateProperty<double?>.All(2.0),
            MouseCursor: MaterialStateProperty<MouseCursor?>.ResolveWith(
                states => WidgetStateMouseCursor.Clickable.Resolve(FromMaterialState(states))),
            OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
            {
                if (states.HasFlag(MaterialState.Selected))
                {
                    if (states.HasFlag(MaterialState.Pressed))
                    {
                        return colors.Primary.WithOpacity(0.1);
                    }

                    if (states.HasFlag(MaterialState.Hovered))
                    {
                        return colors.Primary.WithOpacity(0.08);
                    }

                    return states.HasFlag(MaterialState.Focused)
                        ? colors.Primary.WithOpacity(0.1)
                        : null;
                }

                if (states.HasFlag(MaterialState.Pressed))
                {
                    return colors.OnSurface.WithOpacity(0.1);
                }

                if (states.HasFlag(MaterialState.Hovered))
                {
                    return colors.OnSurface.WithOpacity(0.08);
                }

                return states.HasFlag(MaterialState.Focused)
                    ? colors.OnSurface.WithOpacity(0.1)
                    : null;
            }),
            SplashRadius: 40.0 / 2,
            Padding: new Thickness(4.0, 0.0, 4.0, 0.0));
    }

    public static SwitchThemeData Cupertino(BuildContext context)
    {
        Color activeTrack = CupertinoDynamicColor.Resolve(CupertinoColors.SystemGreen, context);
        Color inactiveTrack = CupertinoDynamicColor.Resolve(
            CupertinoColors.SecondarySystemFill,
            context);
        return new SwitchThemeData(
            ThumbColor: MaterialStateProperty<Color?>.All(Colors.White),
            TrackColor: MaterialStateProperty<Color?>.ResolveWith(
                states => states.HasFlag(MaterialState.Selected) ? activeTrack : inactiveTrack),
            TrackOutlineColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
            MouseCursor: MaterialStateProperty<MouseCursor?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Disabled)
                    ? SystemMouseCursors.Basic
                    : OperatingSystem.IsBrowser()
                        ? SystemMouseCursors.Click
                        : SystemMouseCursors.Basic),
            OverlayColor: MaterialStateProperty<Color?>.ResolveWith(
                states => states.HasFlag(MaterialState.Focused)
                    ? MaterialSwitchState.CupertinoFocusColor(activeTrack)
                    : Colors.Transparent),
            SplashRadius: 0.0);
    }

    private static bool IsInteracting(MaterialState states)
    {
        return states.HasFlag(MaterialState.Pressed)
               || states.HasFlag(MaterialState.Hovered)
               || states.HasFlag(MaterialState.Focused);
    }

    private static IReadOnlySet<WidgetState> FromMaterialState(MaterialState states)
    {
        var result = new HashSet<WidgetState>();
        if (states.HasFlag(MaterialState.Hovered)) result.Add(WidgetState.Hovered);
        if (states.HasFlag(MaterialState.Focused)) result.Add(WidgetState.Focused);
        if (states.HasFlag(MaterialState.Pressed)) result.Add(WidgetState.Pressed);
        if (states.HasFlag(MaterialState.Disabled)) result.Add(WidgetState.Disabled);
        if (states.HasFlag(MaterialState.Selected)) result.Add(WidgetState.Selected);
        if (states.HasFlag(MaterialState.Dragged)) result.Add(WidgetState.Dragged);
        if (states.HasFlag(MaterialState.Error)) result.Add(WidgetState.Error);
        return result;
    }
}

/// Dart's `_SwitchPainter`.
internal sealed class SwitchPainter : ToggleablePainter
{
    private readonly AnimationController _positionController;
    private readonly CurvedAnimation _colorAnimation;

    private bool _stopPressAnimation;
    private double? _pressedInactiveThumbRadius;
    private double? _pressedActiveThumbRadius;
    private double _pressedThumbExtension;

    private BoxPainter? _thumbPainter;
    private TextPainter? _textPainter;
    private Color? _cachedThumbColor;
    private ImageProvider? _cachedThumbImage;
    private ImageErrorListener? _cachedThumbImageError;
    private bool _isPainting;

    private Color _activePressedColor;
    private Color _inactivePressedColor;
    private ImageProvider? _activeThumbImage;
    private ImageErrorListener? _onActiveThumbImageError;
    private ImageProvider? _inactiveThumbImage;
    private ImageErrorListener? _onInactiveThumbImageError;
    private Color _activeTrackColor;
    private Color? _activeTrackOutlineColor;
    private double? _activeTrackOutlineWidth;
    private Color _inactiveTrackColor;
    private Color? _inactiveTrackOutlineColor;
    private double? _inactiveTrackOutlineWidth;
    private ImageConfiguration _configuration = ImageConfiguration.Empty;
    private bool _isInteractive;
    private double _trackInnerLength;
    private TextDirection _textDirection;
    private Color _surfaceColor;
    private double _inactiveThumbRadius;
    private double _activeThumbRadius;
    private double _pressedThumbRadius;
    private double? _thumbOffset;
    private double _trackHeight;
    private double _trackWidth;
    private Color _activeIconColor;
    private Color _inactiveIconColor;
    private Icon? _activeIcon;
    private Icon? _inactiveIcon;
    private IconThemeData _iconTheme = IconThemeData.Fallback;
    private IReadOnlyList<BoxShadow>? _thumbShadow;
    private Size _transitionalThumbSize;
    private bool _isCupertino;

    public SwitchPainter(
        Animation<double> position,
        Animation<double> reaction,
        Animation<double> reactionHoverFade,
        Animation<double> reactionFocusFade,
        AnimationController positionController)
        : base(position, reaction, reactionHoverFade, reactionFocusFade)
    {
        _positionController = positionController;
        _colorAnimation = new CurvedAnimation(positionController, Curves.EaseOut, Curves.EaseIn);
    }

    internal AnimationController PositionController => _positionController;

    internal Color ActiveTrackColor => _activeTrackColor;

    internal Color InactiveTrackColor => _inactiveTrackColor;

    internal Color? ActiveTrackOutlineColor => _activeTrackOutlineColor;

    internal Color? InactiveTrackOutlineColor => _inactiveTrackOutlineColor;

    internal double? ActiveTrackOutlineWidth => _activeTrackOutlineWidth;

    internal double? InactiveTrackOutlineWidth => _inactiveTrackOutlineWidth;

    internal Color ActivePressedColor => _activePressedColor;

    internal Color InactivePressedColor => _inactivePressedColor;

    internal Icon? ActiveIcon => _activeIcon;

    internal Icon? InactiveIcon => _inactiveIcon;

    internal Color ActiveIconColor => _activeIconColor;

    internal Color InactiveIconColor => _inactiveIconColor;

    internal double TrackWidth => _trackWidth;

    internal double TrackHeight => _trackHeight;

    internal double ActiveThumbRadius => _activeThumbRadius;

    internal double InactiveThumbRadius => _inactiveThumbRadius;

    internal double PressedThumbRadius => _pressedThumbRadius;

    internal bool IsCupertino => _isCupertino;

    internal bool IsInteractive => _isInteractive;

    internal Color SurfaceColor => _surfaceColor;

    internal IReadOnlyList<BoxShadow>? ThumbShadow => _thumbShadow;

    internal ImageProvider? ActiveThumbImage => _activeThumbImage;

    internal ImageProvider? InactiveThumbImage => _inactiveThumbImage;

    public void Configure(
        Color inactiveReactionColor,
        Color reactionColor,
        Color hoverColor,
        Color focusColor,
        double splashRadius,
        Point? downPosition,
        bool isFocused,
        bool isHovered,
        Color activeColor,
        Color inactiveColor,
        Color activePressedColor,
        Color inactivePressedColor,
        ImageProvider? activeThumbImage,
        ImageErrorListener? onActiveThumbImageError,
        ImageProvider? inactiveThumbImage,
        ImageErrorListener? onInactiveThumbImageError,
        Color activeTrackColor,
        Color? activeTrackOutlineColor,
        double? activeTrackOutlineWidth,
        Color inactiveTrackColor,
        Color? inactiveTrackOutlineColor,
        double? inactiveTrackOutlineWidth,
        ImageConfiguration configuration,
        bool isInteractive,
        double trackInnerLength,
        TextDirection textDirection,
        Color surfaceColor,
        double inactiveThumbRadius,
        double activeThumbRadius,
        double pressedThumbRadius,
        double? thumbOffset,
        double trackHeight,
        double trackWidth,
        Color activeIconColor,
        Color inactiveIconColor,
        Icon? activeIcon,
        Icon? inactiveIcon,
        IconThemeData iconTheme,
        IReadOnlyList<BoxShadow>? thumbShadow,
        Size transitionalThumbSize,
        bool isCupertino)
    {
        InactiveReactionColor = inactiveReactionColor;
        ReactionColor = reactionColor;
        HoverColor = hoverColor;
        FocusColor = focusColor;
        SplashRadius = splashRadius;
        DownPosition = downPosition;
        IsFocused = isFocused;
        IsHovered = isHovered;
        ActiveColor = activeColor;
        InactiveColor = inactiveColor;
        _activePressedColor = activePressedColor;
        _inactivePressedColor = inactivePressedColor;
        _activeThumbImage = activeThumbImage;
        _onActiveThumbImageError = onActiveThumbImageError;
        _inactiveThumbImage = inactiveThumbImage;
        _onInactiveThumbImageError = onInactiveThumbImageError;
        _activeTrackColor = activeTrackColor;
        _activeTrackOutlineColor = activeTrackOutlineColor;
        _activeTrackOutlineWidth = activeTrackOutlineWidth;
        _inactiveTrackColor = inactiveTrackColor;
        _inactiveTrackOutlineColor = inactiveTrackOutlineColor;
        _inactiveTrackOutlineWidth = inactiveTrackOutlineWidth;
        _configuration = configuration;
        _isInteractive = isInteractive;
        _trackInnerLength = trackInnerLength;
        _textDirection = textDirection;
        _surfaceColor = surfaceColor;
        _inactiveThumbRadius = inactiveThumbRadius;
        _activeThumbRadius = activeThumbRadius;
        _pressedThumbRadius = pressedThumbRadius;
        _thumbOffset = thumbOffset;
        _trackHeight = trackHeight;
        _trackWidth = trackWidth;
        _activeIconColor = activeIconColor;
        _inactiveIconColor = inactiveIconColor;
        _activeIcon = activeIcon;
        _inactiveIcon = inactiveIcon;
        _iconTheme = iconTheme;
        _thumbShadow = thumbShadow;
        _transitionalThumbSize = transitionalThumbSize;
        _isCupertino = isCupertino;
        NotifyPainterChanged();
    }

    public override void Paint(PaintingContext context, Size size)
    {
        double currentValue = Position.Value;
        double visualPosition = _textDirection == TextDirection.Rtl
            ? 1.0 - currentValue
            : currentValue;

        _stopPressAnimation = Reaction.Status == AnimationStatus.Reverse && !_stopPressAnimation;
        if (!_stopPressAnimation)
        {
            _pressedThumbExtension = _isCupertino ? Reaction.Value * 7.0 : 0.0;
            if (Reaction.Status == AnimationStatus.Completed)
            {
                _pressedInactiveThumbRadius = Lerp(
                    _inactiveThumbRadius,
                    _pressedThumbRadius,
                    Reaction.Value);
                _pressedActiveThumbRadius = Lerp(
                    _activeThumbRadius,
                    _pressedThumbRadius,
                    Reaction.Value);
            }

            if (currentValue == 0.0)
            {
                _pressedInactiveThumbRadius = Lerp(
                    _inactiveThumbRadius,
                    _pressedThumbRadius,
                    Reaction.Value);
                _pressedActiveThumbRadius = _activeThumbRadius;
            }

            if (currentValue == 1.0)
            {
                _pressedActiveThumbRadius = Lerp(
                    _activeThumbRadius,
                    _pressedThumbRadius,
                    Reaction.Value);
                _pressedInactiveThumbRadius = _inactiveThumbRadius;
            }
        }

        double inactiveRadius = _pressedInactiveThumbRadius ?? _inactiveThumbRadius;
        double activeRadius = _pressedActiveThumbRadius ?? _activeThumbRadius;
        Size inactiveThumbSize = _isCupertino
            ? new Size((inactiveRadius * 2.0) + _pressedThumbExtension, inactiveRadius * 2.0)
            : new Size(inactiveRadius * 2.0, inactiveRadius * 2.0);
        Size activeThumbSize = _isCupertino
            ? new Size((activeRadius * 2.0) + _pressedThumbExtension, activeRadius * 2.0)
            : new Size(activeRadius * 2.0, activeRadius * 2.0);

        Size thumbSize;
        if (_isCupertino)
        {
            thumbSize = Reaction.Status == AnimationStatus.Completed
                ? new Size(
                    (inactiveRadius * 2.0) + _pressedThumbExtension,
                    inactiveRadius * 2.0)
                : LerpSize(inactiveThumbSize, activeThumbSize, Position.Value);
        }
        else if (Reaction.Status == AnimationStatus.Completed)
        {
            thumbSize = new Size(_pressedThumbRadius * 2.0, _pressedThumbRadius * 2.0);
        }
        else
        {
            bool isForward = Position.Status is AnimationStatus.Dismissed or AnimationStatus.Forward;
            thumbSize = ThumbSizeAnimationValue(isForward, inactiveThumbSize, activeThumbSize);
        }

        double inset = _thumbOffset is null
            ? 0.0
            : 1.0 - (Math.Abs(currentValue - _thumbOffset.Value) * 2.0);
        thumbSize = new Size(thumbSize.Width - inset, thumbSize.Height - inset);

        double colorValue = _colorAnimation.Value;
        Color trackColor = Plumix.Painting.ColorUtilities.Lerp(_inactiveTrackColor, _activeTrackColor, colorValue);
        Color? trackOutlineColor =
            _inactiveTrackOutlineColor is null || _activeTrackOutlineColor is null
                ? null
                : Plumix.Painting.ColorUtilities.Lerp(
                    _inactiveTrackOutlineColor.Value,
                    _activeTrackOutlineColor.Value,
                    colorValue);
        double? trackOutlineWidth = Plumix.Painting.ColorUtilities.LerpDouble(
            _inactiveTrackOutlineWidth,
            _activeTrackOutlineWidth,
            colorValue);

        Color lerpedThumbColor;
        if (Reaction.Status != AnimationStatus.Dismissed)
        {
            lerpedThumbColor = Plumix.Painting.ColorUtilities.Lerp(
                _inactivePressedColor,
                _activePressedColor,
                colorValue);
        }
        else if (_positionController.Status == AnimationStatus.Forward)
        {
            lerpedThumbColor = Plumix.Painting.ColorUtilities.Lerp(_inactivePressedColor, ActiveColor, colorValue);
        }
        else if (_positionController.Status == AnimationStatus.Reverse)
        {
            lerpedThumbColor = Plumix.Painting.ColorUtilities.Lerp(InactiveColor, _activePressedColor, colorValue);
        }
        else
        {
            lerpedThumbColor = Plumix.Painting.ColorUtilities.Lerp(InactiveColor, ActiveColor, colorValue);
        }

        Color thumbColor = Plumix.Painting.ColorUtilities.AlphaBlend(lerpedThumbColor, _surfaceColor);
        Icon? thumbIcon = currentValue < 0.5 ? _inactiveIcon : _activeIcon;
        ImageProvider? thumbImage = currentValue < 0.5 ? _inactiveThumbImage : _activeThumbImage;
        ImageErrorListener? thumbImageError = currentValue < 0.5
            ? _onInactiveThumbImageError
            : _onActiveThumbImageError;

        var trackPaintOffset = new Point(
            (size.Width - _trackWidth) / 2.0,
            (size.Height - _trackHeight) / 2.0);
        double trackRadius = _trackHeight / 2.0;
        double additionalThumbRadius = (thumbSize.Height / 2.0) - trackRadius;
        double horizontalProgress = visualPosition * (_trackInnerLength - _pressedThumbExtension);
        var thumbPaintOffset = new Point(
            trackPaintOffset.X + trackRadius + (_pressedThumbExtension / 2.0)
            - (thumbSize.Width / 2.0) + horizontalProgress,
            trackPaintOffset.Y - additionalThumbRadius);
        var radialReactionOrigin = new Point(
            thumbPaintOffset.X + (thumbSize.Height / 2.0),
            size.Height / 2.0);

        var trackRect = new Rect(
            trackPaintOffset.X,
            trackPaintOffset.Y,
            _trackWidth,
            _trackHeight);
        context.Canvas.DrawRRect(
            RRect.FromRectAndRadius(trackRect, trackRadius),
            new SolidColorBrush(trackColor),
            null);
        if (trackOutlineColor.HasValue)
        {
            var outlineRect = new Rect(
                trackPaintOffset.X + 1.0,
                trackPaintOffset.Y + 1.0,
                _trackWidth - 2.0,
                _trackHeight - 2.0);
            context.Canvas.DrawRRect(
                RRect.FromRectAndRadius(outlineRect, trackRadius),
                null,
                new Pen(new SolidColorBrush(trackOutlineColor.Value), trackOutlineWidth ?? 2.0));
        }

        if (_isCupertino)
        {
            if (IsFocused)
            {
                context.Canvas.DrawRRect(
                    RRect.FromRectAndRadius(trackRect, trackRadius)
                        .Inflate(SwitchConfigCupertino.FocusTrackOutline / 2.0),
                    null,
                    new Pen(
                        new SolidColorBrush(FocusColor),
                        SwitchConfigCupertino.FocusTrackOutline));
            }

            context.PushClipRRect(
                false,
                new Point(0, 0),
                trackRect,
                RRect.FromRectAndRadius(trackRect, trackRadius),
                (clipped, _) =>
                {
                    PaintRadialReaction(clipped, radialReactionOrigin);
                    PaintThumb(
                        clipped,
                        thumbPaintOffset,
                        thumbSize,
                        colorValue,
                        thumbColor,
                        thumbImage,
                        thumbImageError,
                        thumbIcon);
                });
            return;
        }

        PaintRadialReaction(context, radialReactionOrigin);
        PaintThumb(
            context,
            thumbPaintOffset,
            thumbSize,
            colorValue,
            thumbColor,
            thumbImage,
            thumbImageError,
            thumbIcon);
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate) => true;

    public override void Dispose()
    {
        _textPainter?.Dispose();
        _textPainter = null;
        _thumbPainter?.Dispose();
        _thumbPainter = null;
        _cachedThumbColor = null;
        _cachedThumbImage = null;
        _cachedThumbImageError = null;
        _colorAnimation.Dispose();
        base.Dispose();
    }

    private Size ThumbSizeAnimationValue(bool isForward, Size inactiveThumbSize, Size activeThumbSize)
    {
        TweenSequence<Size> sequence = isForward
            ? new TweenSequence<Size>(
            [
                new TweenSequenceItem<Size>(
                    new SizeTween(inactiveThumbSize, _transitionalThumbSize)
                        .Chain(new CurveTween(Curves.Cubic(0.31, 0.00, 0.56, 1.00))),
                    11.0),
                new TweenSequenceItem<Size>(
                    new SizeTween(_transitionalThumbSize, activeThumbSize)
                        .Chain(new CurveTween(Curves.Cubic(0.20, 0.00, 0.00, 1.00))),
                    72.0),
                new TweenSequenceItem<Size>(new ConstantTween<Size>(activeThumbSize), 17.0),
            ])
            : new TweenSequence<Size>(
            [
                new TweenSequenceItem<Size>(new ConstantTween<Size>(inactiveThumbSize), 17.0),
                new TweenSequenceItem<Size>(
                    new SizeTween(inactiveThumbSize, _transitionalThumbSize)
                        .Chain(new CurveTween(
                            Curves.Flipped(Curves.Cubic(0.20, 0.00, 0.00, 1.00)))),
                    72.0),
                new TweenSequenceItem<Size>(
                    new SizeTween(_transitionalThumbSize, activeThumbSize)
                        .Chain(new CurveTween(
                            Curves.Flipped(Curves.Cubic(0.31, 0.00, 0.56, 1.00)))),
                    11.0),
            ]);
        return sequence.Transform(_positionController.Value);
    }

    private void PaintThumb(
        PaintingContext context,
        Point offset,
        Size thumbSize,
        double colorValue,
        Color thumbColor,
        ImageProvider? image,
        ImageErrorListener? imageError,
        Icon? icon)
    {
        var thumbBounds = new Rect(offset.X, offset.Y, thumbSize.Width, thumbSize.Height);
        double radius = thumbSize.Height / 2.0;
        if (_thumbShadow is { Count: > 0 })
        {
            context.Canvas.DrawRectangle(
                Brushes.Transparent,
                null,
                thumbBounds,
                BorderRadius.Circular(radius),
                _thumbShadow.ToAvalonia());
        }

        if (_isCupertino)
        {
            context.Canvas.DrawRRect(
                RRect.FromRectAndRadius(thumbBounds.Inflate(0.5), radius + 0.5),
                new SolidColorBrush(Color.FromArgb(0x0A, 0x00, 0x00, 0x00)),
                null);
        }

        try
        {
            _isPainting = true;
            if (_thumbPainter is null
                || _cachedThumbColor != thumbColor
                || !ReferenceEquals(_cachedThumbImage, image)
                || !ReferenceEquals(_cachedThumbImageError, imageError))
            {
                _thumbPainter?.Dispose();
                _cachedThumbColor = thumbColor;
                _cachedThumbImage = image;
                _cachedThumbImageError = imageError;
                var decoration = new ShapeDecoration(
                    Shape: new StadiumBorder(),
                    Color: thumbColor,
                    Image: image is null ? null : new DecorationImage(image, onError: imageError));
                _thumbPainter = decoration.CreateBoxPainter(HandleDecorationChanged);
            }

            _thumbPainter!.Paint(context, offset, _configuration with { Size = thumbSize });
        }
        finally
        {
            _isPainting = false;
        }

        if (icon?.IconData is not null)
        {
            PaintIcon(context, thumbBounds, icon, colorValue);
        }
    }

    private void PaintIcon(
        PaintingContext context,
        Rect thumbBounds,
        Icon icon,
        double colorValue)
    {
        Color iconColor = Plumix.Painting.ColorUtilities.Lerp(_inactiveIconColor, _activeIconColor, colorValue);
        double iconSize = icon.Size ?? SwitchConfigM3.IconSize;
        var style = new TextStyle(
            FontFamily: Icon.ResolveFontFamily(icon.IconData!),
            FontSize: iconSize,
            Color: iconColor,
            FontWeight: icon.FontWeight ?? ResolveFontWeight(icon.Weight ?? _iconTheme.Weight),
            Height: 1.0,
            LetterSpacing: 0.0);
        _textPainter ??= new TextPainter(textDirection: _textDirection, maxLines: 1);
        _textPainter.TextDirection = _textDirection;
        _textPainter.Text = new TextSpan(
            char.ConvertFromUtf32(icon.IconData!.CodePoint),
            style: style);
        _textPainter.Layout();
        var offset = new Point(
            thumbBounds.Left + ((thumbBounds.Width - iconSize) / 2.0),
            thumbBounds.Top + ((thumbBounds.Height - iconSize) / 2.0));
        _textPainter.Paint(context, offset);
    }

    private void HandleDecorationChanged()
    {
        if (!_isPainting)
        {
            NotifyPainterChanged();
        }
    }

    private static FontWeight ResolveFontWeight(double? weight)
    {
        double value = weight ?? 400.0;
        if (value < 150.0) return FontWeight.Thin;
        if (value < 250.0) return FontWeight.ExtraLight;
        if (value < 350.0) return FontWeight.Light;
        if (value < 450.0) return FontWeight.Normal;
        if (value < 550.0) return FontWeight.Medium;
        if (value < 650.0) return FontWeight.SemiBold;
        if (value < 750.0) return FontWeight.Bold;
        if (value < 850.0) return FontWeight.ExtraBold;
        return FontWeight.Black;
    }

    private static double Lerp(double from, double to, double t)
    {
        return from + ((to - from) * t);
    }

    private static Size LerpSize(Size from, Size to, double t)
    {
        return new Size(
            from.Width + ((to.Width - from.Width) * t),
            from.Height + ((to.Height - from.Height) * t));
    }

    private sealed class SizeTween : Tween<Size>
    {
        public SizeTween(Size begin, Size end)
        {
            Begin = begin;
            End = end;
        }

        public override Size Lerp(Size a, Size b, double t)
        {
            return LerpSize(a, b, t);
        }
    }
}
