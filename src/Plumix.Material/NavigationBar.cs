using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using BoxShadow = Plumix.Rendering.BoxShadow;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/navigation_bar.dart

public enum NavigationDestinationLabelBehavior
{
    AlwaysShow,
    AlwaysHide,
    OnlyShowSelected,
}

public sealed class NavigationDestination : StatelessWidget
{
    public NavigationDestination(
        Widget icon,
        string label,
        Widget? selectedIcon = null,
        string? tooltip = null,
        bool enabled = true,
        Key? key = null) : base(key)
    {
        Icon = icon ?? throw new ArgumentNullException(nameof(icon));
        Label = label ?? throw new ArgumentNullException(nameof(label));
        SelectedIcon = selectedIcon;
        Tooltip = tooltip;
        Enabled = enabled;
    }

    public Widget Icon { get; }

    public Widget? SelectedIcon { get; }

    public string Label { get; }

    public string? Tooltip { get; }

    public bool Enabled { get; }

    public override Widget Build(BuildContext context)
    {
        throw new InvalidOperationException("NavigationDestination widgets must be children of NavigationBar.");
    }
}

public sealed class NavigationBar : StatelessWidget
{
    private static readonly TimeSpan DefaultAnimationDuration = TimeSpan.FromMilliseconds(500);

    public NavigationBar(
        IReadOnlyList<Widget> destinations,
        TimeSpan? animationDuration = null,
        int selectedIndex = 0,
        Action<int>? onDestinationSelected = null,
        Color? backgroundColor = null,
        double? elevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        Color? indicatorColor = null,
        ShapeBorder? indicatorShape = null,
        double? height = null,
        NavigationDestinationLabelBehavior? labelBehavior = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        MaterialStateProperty<TextStyle?>? labelTextStyle = null,
        Thickness? labelPadding = null,
        bool maintainBottomViewPadding = false,
        Key? key = null) : base(key)
    {
        if (destinations is null)
        {
            throw new ArgumentNullException(nameof(destinations));
        }

        if (destinations.Count < 2)
        {
            throw new ArgumentException("NavigationBar requires at least two destinations.", nameof(destinations));
        }

        if (selectedIndex < 0 || selectedIndex >= destinations.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(selectedIndex), "Selected index must be within destination range.");
        }

        if (animationDuration.HasValue && animationDuration.Value < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(animationDuration));
        }

        ValidatePositiveFinite(height, nameof(height));
        ValidateNonNegativeFinite(elevation, nameof(elevation));

        Destinations = destinations;
        AnimationDuration = animationDuration;
        SelectedIndex = selectedIndex;
        OnDestinationSelected = onDestinationSelected;
        BackgroundColor = backgroundColor;
        Elevation = elevation;
        ShadowColor = shadowColor;
        SurfaceTintColor = surfaceTintColor;
        IndicatorColor = indicatorColor;
        IndicatorShape = indicatorShape;
        Height = height;
        LabelBehavior = labelBehavior;
        OverlayColor = overlayColor;
        LabelTextStyle = labelTextStyle;
        LabelPadding = labelPadding;
        MaintainBottomViewPadding = maintainBottomViewPadding;
    }

    public TimeSpan? AnimationDuration { get; }
    public int SelectedIndex { get; }
    public IReadOnlyList<Widget> Destinations { get; }
    public Action<int>? OnDestinationSelected { get; }
    public Color? BackgroundColor { get; }
    public double? Elevation { get; }
    public Color? ShadowColor { get; }
    public Color? SurfaceTintColor { get; }
    public Color? IndicatorColor { get; }
    public ShapeBorder? IndicatorShape { get; }
    public double? Height { get; }
    public NavigationDestinationLabelBehavior? LabelBehavior { get; }
    public MaterialStateProperty<Color?>? OverlayColor { get; }
    public MaterialStateProperty<TextStyle?>? LabelTextStyle { get; }
    public Thickness? LabelPadding { get; }
    public bool MaintainBottomViewPadding { get; }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var navigationTheme = NavigationBarTheme.Of(context);
        var defaults = ResolveDefaults(theme);
        double effectiveHeight = Height ?? navigationTheme.Height ?? defaults.Height!.Value;
        double effectiveElevation = Elevation ?? navigationTheme.Elevation ?? defaults.Elevation!.Value;
        var effectiveBackground = BackgroundColor ?? navigationTheme.BackgroundColor ?? defaults.BackgroundColor!.Value;
        var effectiveShadow = ShadowColor ?? navigationTheme.ShadowColor ?? defaults.ShadowColor;
        var effectiveSurfaceTint = SurfaceTintColor ?? navigationTheme.SurfaceTintColor ?? defaults.SurfaceTintColor;
        var effectiveIndicatorColor = IndicatorColor ?? navigationTheme.IndicatorColor ?? defaults.IndicatorColor!.Value;
        var effectiveIndicatorShape = IndicatorShape ?? navigationTheme.IndicatorShape ?? defaults.IndicatorShape!;
        var effectiveLabelBehavior = LabelBehavior ?? navigationTheme.LabelBehavior ?? defaults.LabelBehavior!.Value;
        var effectiveLabelPadding = LabelPadding ?? navigationTheme.LabelPadding ?? defaults.LabelPadding ?? new Thickness(0, 4, 0, 0);

        var children = new List<Widget>(Destinations.Count);
        for (int index = 0; index < Destinations.Count; index++)
        {
            var destination = Destinations[index];
            int capturedIndex = index;
            Action onTap = OnDestinationSelected is null ? () => { } : () => OnDestinationSelected(capturedIndex);
            string indexLabel = MaterialLocalizations.Of(context).TabLabel(index + 1, Destinations.Count);
            Widget tile = destination is NavigationDestination navigationDestination
                ? new NavigationBarDestinationTile(
                    destination: navigationDestination,
                    selected: index == SelectedIndex,
                    indexLabel: indexLabel,
                    onTap: onTap,
                    duration: AnimationDuration ?? DefaultAnimationDuration,
                    labelBehavior: effectiveLabelBehavior,
                    indicatorColor: effectiveIndicatorColor,
                    indicatorShape: effectiveIndicatorShape,
                    labelTextStyle: ComposeReferenceProperty(LabelTextStyle, navigationTheme.LabelTextStyle, defaults.LabelTextStyle),
                    iconTheme: ComposeReferenceProperty<IconThemeData>(null, navigationTheme.IconTheme, defaults.IconTheme),
                    overlayColor: ComposeColorProperty(OverlayColor, navigationTheme.OverlayColor, defaults.OverlayColor),
                    labelPadding: effectiveLabelPadding,
                    height: effectiveHeight,
                    key: destination.Key ?? new ValueKey<int>(index))
                : new NavigationBarCustomDestinationTile(
                    child: destination,
                    selected: index == SelectedIndex,
                    indexLabel: indexLabel,
                    onTap: onTap,
                    height: effectiveHeight,
                    overlayColor: ComposeColorProperty(OverlayColor, navigationTheme.OverlayColor, defaults.OverlayColor),
                    key: destination.Key ?? new ValueKey<int>(index));
            children.Add(new Expanded(
                child: new MergeSemantics(
                    new Semantics(
                        role: SemanticsRole.Tab,
                        selected: index == SelectedIndex,
                        child: tile))));
        }

        Widget content = new SizedBox(
            height: effectiveHeight,
            child: new Row(children: children));

        content = new SafeArea(
            left: false,
            top: false,
            right: false,
            maintainBottomViewPadding: MaintainBottomViewPadding,
            child: new Semantics(
                role: SemanticsRole.TabBar,
                explicitChildNodes: true,
                container: true,
                child: content));

        return new global::Plumix.Material.Material(
            color: effectiveBackground,
            elevation: effectiveElevation,
            shadowColor: effectiveShadow,
            surfaceTintColor: effectiveSurfaceTint,
            child: content);
    }

    private static NavigationBarThemeData ResolveDefaults(ThemeData theme)
    {
        ColorScheme colors = theme.ColorScheme;
        if (!theme.UseMaterial3)
        {
            return new NavigationBarThemeData(
                Height: 80,
                BackgroundColor: ElevationOverlay.ColorWithOverlay(
                    colors.Surface,
                    colors.OnSurface,
                    3.0),
                Elevation: 0,
                IndicatorColor: NavigationSurfaceUtilities.WithOpacity(colors.Secondary, 0.24),
                IndicatorShape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(16)),
                LabelTextStyle: MaterialStateProperty<TextStyle?>.All(
                    theme.TextTheme.LabelSmall.CopyWith(color: colors.OnSurface)),
                IconTheme: MaterialStateProperty<IconThemeData?>.All(
                    new IconThemeData(Color: colors.OnSurface, Size: 24)),
                OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                    states.HasFlag(MaterialState.Pressed) || states.HasFlag(MaterialState.Focused)
                        ? NavigationSurfaceUtilities.WithOpacity(colors.OnSurface, 0.12)
                        : states.HasFlag(MaterialState.Hovered)
                            ? NavigationSurfaceUtilities.WithOpacity(colors.OnSurface, 0.04)
                            : null),
                LabelBehavior: NavigationDestinationLabelBehavior.AlwaysShow,
                LabelPadding: new Thickness(0, 4, 0, 0));
        }

        return new NavigationBarThemeData(
            Height: 80,
            BackgroundColor: colors.SurfaceContainer,
            Elevation: 3,
            ShadowColor: Colors.Transparent,
            SurfaceTintColor: Colors.Transparent,
            IndicatorColor: colors.SecondaryContainer,
            IndicatorShape: new StadiumBorder(),
            LabelTextStyle: MaterialStateProperty<TextStyle?>.ResolveWith(states =>
                theme.TextTheme.LabelMedium.CopyWith(color:
                    states.HasFlag(MaterialState.Disabled)
                        ? NavigationSurfaceUtilities.WithOpacity(colors.OnSurfaceVariant, 0.38)
                        : states.HasFlag(MaterialState.Selected)
                            ? colors.OnSurface
                            : colors.OnSurfaceVariant)),
            IconTheme: MaterialStateProperty<IconThemeData?>.ResolveWith(states =>
                new IconThemeData(
                    Color: states.HasFlag(MaterialState.Disabled)
                        ? NavigationSurfaceUtilities.WithOpacity(colors.OnSurfaceVariant, 0.38)
                        : states.HasFlag(MaterialState.Selected)
                            ? colors.OnSecondaryContainer
                            : colors.OnSurfaceVariant,
                    Size: 24)),
            OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Pressed) || states.HasFlag(MaterialState.Focused)
                    ? NavigationSurfaceUtilities.WithOpacity(colors.OnSurface, 0.10)
                    : states.HasFlag(MaterialState.Hovered)
                        ? NavigationSurfaceUtilities.WithOpacity(colors.OnSurface, 0.08)
                        : null),
            LabelBehavior: NavigationDestinationLabelBehavior.AlwaysShow,
            LabelPadding: new Thickness(0, 4, 0, 0));
    }

    private static MaterialStateProperty<T?> ComposeReferenceProperty<T>(
        MaterialStateProperty<T?>? widget,
        MaterialStateProperty<T?>? localTheme,
        MaterialStateProperty<T?>? defaults) where T : class
    {
        return MaterialStateProperty<T?>.ResolveWith(states =>
            widget?.Resolve(states) ?? localTheme?.Resolve(states) ?? defaults?.Resolve(states));
    }

    private static MaterialStateProperty<Color?> ComposeColorProperty(
        MaterialStateProperty<Color?>? widget,
        MaterialStateProperty<Color?>? localTheme,
        MaterialStateProperty<Color?>? defaults)
    {
        return MaterialStateProperty<Color?>.ResolveWith(states =>
        {
            var value = widget?.Resolve(states);
            if (value.HasValue) return value;
            value = localTheme?.Resolve(states);
            return value.HasValue ? value : defaults?.Resolve(states);
        });
    }

    private static void ValidatePositiveFinite(double? value, string name)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(name, "Value must be positive and finite.");
        }
    }

    private static void ValidateNonNegativeFinite(double? value, string name)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0))
        {
            throw new ArgumentOutOfRangeException(name, "Value must be non-negative and finite.");
        }
    }
}

public sealed class NavigationIndicator : StatelessWidget
{
    public NavigationIndicator(
        double animationValue,
        Color? color = null,
        double width = 64,
        double height = 32,
        BorderRadius? borderRadius = null,
        ShapeBorder? shape = null,
        Key? key = null) : base(key)
    {
        if (!double.IsFinite(animationValue)) throw new ArgumentOutOfRangeException(nameof(animationValue));
        if (!double.IsFinite(width) || width < 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (!double.IsFinite(height) || height < 0) throw new ArgumentOutOfRangeException(nameof(height));
        AnimationValue = Math.Clamp(animationValue, 0, 1);
        Color = color;
        Width = width;
        Height = height;
        BorderRadius = borderRadius ?? Plumix.Rendering.BorderRadius.Circular(16);
        Shape = shape;
    }

    public double AnimationValue { get; }
    public Color? Color { get; }
    public double Width { get; }
    public double Height { get; }
    public BorderRadius BorderRadius { get; }
    public ShapeBorder? Shape { get; }

    public override Widget Build(BuildContext context)
    {
        double scale = AnimationValue <= 0
            ? 0
            : 0.4 + (0.6 * Curves.EaseInOutCubicEmphasized(AnimationValue));
        var shape = Shape ?? new RoundedRectangleBorder(borderRadius:
            Plumix.Rendering.BorderRadius.Circular(BorderRadius.Radius));
        return Plumix.Widgets.Transform.Scale(
            scaleX: scale,
            scaleY: 1.0,
            alignment: Alignment.Center,
            child: new Opacity(
                opacity: AnimationValue,
                child: new SizedBox(
                    width: Width,
                    height: Height,
                    child: new DecoratedBox(
                        decoration: new BoxDecoration(
                            Color: Color ?? Theme.Of(context).ColorScheme.Secondary,
                            Border: ShapeBorderGeometry.SideOrNull(shape) is { } shapeSide
                                ? Plumix.Rendering.Border.FromBorderSide(shapeSide)
                                : null,
                            BorderRadius: ShapeBorderGeometry.ResolveRadius(shape))))));
    }
}

internal sealed class NavigationBarDestinationTile : StatefulWidget
{
    public NavigationBarDestinationTile(
        NavigationDestination destination,
        bool selected,
        string indexLabel,
        Action? onTap,
        TimeSpan duration,
        NavigationDestinationLabelBehavior labelBehavior,
        Color indicatorColor,
        ShapeBorder indicatorShape,
        MaterialStateProperty<TextStyle?> labelTextStyle,
        MaterialStateProperty<IconThemeData?> iconTheme,
        MaterialStateProperty<Color?> overlayColor,
        Thickness labelPadding,
        double height,
        Key? key = null) : base(key)
    {
        Destination = destination;
        Selected = selected;
        IndexLabel = indexLabel;
        OnTap = onTap;
        Duration = duration;
        LabelBehavior = labelBehavior;
        IndicatorColor = indicatorColor;
        IndicatorShape = indicatorShape;
        LabelTextStyle = labelTextStyle;
        IconTheme = iconTheme;
        OverlayColor = overlayColor;
        LabelPadding = labelPadding;
        Height = height;
    }

    public NavigationDestination Destination { get; }
    public bool Selected { get; }
    public string IndexLabel { get; }
    public Action? OnTap { get; }
    public TimeSpan Duration { get; }
    public NavigationDestinationLabelBehavior LabelBehavior { get; }
    public Color IndicatorColor { get; }
    public ShapeBorder IndicatorShape { get; }
    public MaterialStateProperty<TextStyle?> LabelTextStyle { get; }
    public MaterialStateProperty<IconThemeData?> IconTheme { get; }
    public MaterialStateProperty<Color?> OverlayColor { get; }
    public Thickness LabelPadding { get; }
    public double Height { get; }

    public override State CreateState() => new NavigationBarDestinationTileState();
}

internal sealed class NavigationBarDestinationTileState : State
{
    private readonly GlobalKey _iconKey = new LabeledGlobalKey<State>("NavigationBar destination icon");
    private AnimationController? _controller;

    private NavigationBarDestinationTile CurrentWidget => (NavigationBarDestinationTile)StateWidget;

    public override void InitState()
    {
        CreateController(CurrentWidget.Selected ? 1 : 0);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var old = (NavigationBarDestinationTile)oldWidget;
        if (old.Duration != CurrentWidget.Duration)
        {
            double value = _controller?.Value ?? (CurrentWidget.Selected ? 1 : 0);
            DisposeController();
            CreateController(value);
        }

        if (old.Selected != CurrentWidget.Selected)
        {
            if (CurrentWidget.Selected) _controller!.Forward(); else _controller!.Reverse();
        }
    }

    public override void Dispose()
    {
        DisposeController();
    }

    public override Widget Build(BuildContext context)
    {
        var widget = CurrentWidget;
        var destination = widget.Destination;
        double progress = _controller?.Value ?? (widget.Selected ? 1 : 0);
        var icon = widget.Selected && destination.SelectedIcon is not null
            ? destination.SelectedIcon
            : destination.Icon;
        MaterialState states = destination.Enabled
            ? widget.Selected ? MaterialState.Selected : MaterialState.None
            : MaterialState.Disabled;
        IconThemeData? iconTheme = widget.IconTheme.Resolve(states);

        Widget iconWithIndicator = new Stack(
            alignment: Alignment.Center,
            children:
            [
                new NavigationIndicator(
                    animationValue: progress,
                    color: widget.IndicatorColor,
                    shape: widget.IndicatorShape),
                iconTheme is null ? icon : IconTheme.Merge(data: iconTheme, child: icon)
            ]);

        double layoutProgress = widget.LabelBehavior switch
        {
            NavigationDestinationLabelBehavior.AlwaysShow => 1.0,
            NavigationDestinationLabelBehavior.AlwaysHide => 0.0,
            _ => Curves.EaseInOutCubicEmphasized(progress),
        };
        AnimationStatus layoutStatus = layoutProgress switch
        {
            <= 0.0 => AnimationStatus.Dismissed,
            >= 1.0 => AnimationStatus.Completed,
            _ => widget.Selected ? AnimationStatus.Forward : AnimationStatus.Reverse,
        };
        var layoutAnimation = new ConstantAnimation<double>(layoutProgress, layoutStatus);
        Widget label = new Padding(
            insets: widget.LabelPadding,
            child: new Text(
                destination.Label,
                style: widget.LabelTextStyle.Resolve(states),
                softWrap: false,
                maxLines: 1,
                overflow: TextOverflow.Ellipsis));
        Widget content = new NavigationBarDestinationLayout(
            icon: iconWithIndicator,
            iconKey: _iconKey,
            label: label,
            animation: layoutAnimation);

        Widget result = new NavigationBarIndicatorInkWell(
            iconKey: _iconKey,
            labelBehavior: widget.LabelBehavior,
            overlayColor: widget.OverlayColor,
            customBorder: widget.IndicatorShape,
            onTap: destination.Enabled ? widget.OnTap : null,
            child: new Row(children: [new Expanded(child: content)]));

        // Dart parity: `_NavigationBarDestinationTooltip`.
        result = new Tooltip(
            message: destination.Tooltip ?? destination.Label,
            verticalOffset: 42,
            preferBelow: false,
            excludeFromSemantics: true,
            child: result);

        // Dart parity: `_NavigationBarDestinationSemantics`.
        return new Semantics(
            enabled: destination.Enabled,
            flags: SemanticsFlags.IsButton,
            child: new Stack(
                alignment: Alignment.Center,
                children:
                [
                    result,
                    new Semantics(label: widget.IndexLabel)
                ]));
    }

    private void CreateController(double value)
    {
        _controller = new AnimationController(duration: CurrentWidget.Duration, vsync: this)
        {
            Curve = Curves.EaseInOut
        };
        _controller.Changed += HandleAnimationChanged;
        if (value >= 1) _controller.Forward(1); else if (value > 0) _controller.Forward(value);
    }

    private void HandleAnimationChanged()
    {
        SetState(() => { });
    }

    private void DisposeController()
    {
        if (_controller is null) return;
        _controller.Changed -= HandleAnimationChanged;
        _controller.Dispose();
        _controller = null;
    }
}

internal sealed class NavigationBarIndicatorInkWell : InkResponse
{
    private readonly GlobalKey _iconKey;

    public NavigationBarIndicatorInkWell(
        GlobalKey iconKey,
        NavigationDestinationLabelBehavior labelBehavior,
        MaterialStateProperty<Color?>? overlayColor,
        ShapeBorder? customBorder,
        Action? onTap,
        Widget child)
        : base(
            child: child,
            onTap: onTap,
            containedInkWell: true,
            highlightColor: Colors.Transparent,
            overlayColor: overlayColor,
            customBorder: customBorder)
    {
        _iconKey = iconKey ?? throw new ArgumentNullException(nameof(iconKey));
        LabelBehavior = labelBehavior;
    }

    public NavigationDestinationLabelBehavior LabelBehavior { get; }

    public override Func<Rect> GetRectCallback(RenderBox referenceBox)
    {
        ArgumentNullException.ThrowIfNull(referenceBox);
        return () =>
        {
            RenderBox iconBox = _iconKey.CurrentContext?.FindRenderObject() as RenderBox
                                ?? throw new InvalidOperationException(
                                    "The navigation destination icon must be laid out before resolving its ink rect.");
            Point iconTopLeft = iconBox.LocalToGlobal(default);
            Point localTopLeft = referenceBox.GlobalToLocal(iconTopLeft);
            return new Rect(localTopLeft, iconBox.Size);
        };
    }
}

internal sealed class NavigationBarDestinationLayout : StatelessWidget
{
    public NavigationBarDestinationLayout(
        Widget icon,
        GlobalKey iconKey,
        Widget label,
        Animation<double> animation,
        Key? key = null) : base(key)
    {
        Icon = icon ?? throw new ArgumentNullException(nameof(icon));
        IconKey = iconKey ?? throw new ArgumentNullException(nameof(iconKey));
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Animation = animation ?? throw new ArgumentNullException(nameof(animation));
    }

    public Widget Icon { get; }
    public GlobalKey IconKey { get; }
    public Widget Label { get; }
    public Animation<double> Animation { get; }

    public override Widget Build(BuildContext context)
    {
        return new CustomMultiChildLayout(
            @delegate: new NavigationDestinationLayoutDelegate(Animation),
            children:
            [
                new LayoutId(
                    NavigationDestinationLayoutDelegate.IconId,
                    new KeyedSubtree(Icon, IconKey)),
                new LayoutId(
                    NavigationDestinationLayoutDelegate.LabelId,
                    new FadeTransition(
                        opacity: Animation,
                        alwaysIncludeSemantics: true,
                        child: Label)),
            ]);
    }
}

internal sealed class NavigationDestinationLayoutDelegate : MultiChildLayoutDelegate
{
    public const int IconId = 1;
    public const int LabelId = 2;

    public NavigationDestinationLayoutDelegate(Animation<double> animation) : base(animation)
    {
        Animation = animation ?? throw new ArgumentNullException(nameof(animation));
    }

    public Animation<double> Animation { get; }

    public override void PerformLayout(Size size)
    {
        Size iconSize = LayoutChild(IconId, BoxConstraints.Loose(size));
        Size labelSize = LayoutChild(LabelId, BoxConstraints.Loose(size));
        double yPositionOffset = (iconSize.Height / 2.0)
                                 + ((labelSize.Height / 2.0) * Animation.Value);
        double iconYPosition = (size.Height / 2.0) - yPositionOffset;
        PositionChild(
            IconId,
            new Point((size.Width - iconSize.Width) / 2.0, iconYPosition));
        PositionChild(
            LabelId,
            new Point((size.Width - labelSize.Width) / 2.0, iconYPosition + iconSize.Height));
    }

    public override bool ShouldRelayout(MultiChildLayoutDelegate oldDelegate)
    {
        return oldDelegate is not NavigationDestinationLayoutDelegate old
               || !ReferenceEquals(old.Animation, Animation);
    }
}

internal sealed class NavigationBarCustomDestinationTile : StatelessWidget
{
    public NavigationBarCustomDestinationTile(
        Widget child,
        bool selected,
        string indexLabel,
        Action onTap,
        double height,
        MaterialStateProperty<Color?> overlayColor,
        Key? key = null) : base(key)
    {
        Child = child;
        Selected = selected;
        IndexLabel = indexLabel;
        OnTap = onTap;
        Height = height;
        OverlayColor = overlayColor;
    }

    public Widget Child { get; }
    public bool Selected { get; }
    public string IndexLabel { get; }
    public Action OnTap { get; }
    public double Height { get; }
    public MaterialStateProperty<Color?> OverlayColor { get; }

    public override Widget Build(BuildContext context)
    {
        Widget result = new InkResponse(
            containedInkWell: true,
            highlightColor: Colors.Transparent,
            overlayColor: OverlayColor,
            onTap: OnTap,
            child: new Row(children: [new Expanded(child: Child)]));

        return new Semantics(
            enabled: true,
            flags: SemanticsFlags.IsButton,
            child: new Stack(
                alignment: Alignment.Center,
                children:
                [
                    result,
                    new Semantics(label: IndexLabel)
                ]));
    }
}

internal static class NavigationSurfaceUtilities
{
    public static Color ApplySurfaceTint(Color background, Color surfaceTint, double elevation)
    {
        return ElevationOverlay.ApplySurfaceTint(background, surfaceTint, elevation);
    }

    public static BoxDecoration CreateDecoration(
        Color background,
        double elevation,
        Color? shadowColor,
        Color? surfaceTintColor,
        bool useMaterial3)
    {
        if (useMaterial3)
        {
            background = ElevationOverlay.ApplySurfaceTint(background, surfaceTintColor, elevation);
        }

        IReadOnlyList<BoxShadow>? shadows = null;
        if (elevation > 0 && shadowColor.HasValue && shadowColor.Value.A > 0)
        {
            shadows =
            [
                new BoxShadow(
                    color: WithOpacity(shadowColor.Value, 0.20),
                    offset: new Point(0, Math.Max(1, elevation * 0.5)),
                    blurRadius: Math.Max(2, elevation * 2.4)),
            ];
        }

        return new BoxDecoration(Color: background, BoxShadows: shadows);
    }

    public static Color WithOpacity(Color color, double opacity)
    {
        return Color.FromArgb(
            (byte)Math.Clamp((int)Math.Round(color.A * Math.Clamp(opacity, 0, 1)), 0, 255),
            color.R,
            color.G,
            color.B);
    }

    public static Color Blend(Color background, Color foreground, double opacity)
    {
        opacity = Math.Clamp(opacity, 0, 1) * (foreground.A / 255.0);
        byte Mix(byte a, byte b) => (byte)Math.Clamp((int)Math.Round(a + ((b - a) * opacity)), 0, 255);
        return Color.FromArgb(background.A, Mix(background.R, foreground.R), Mix(background.G, foreground.G), Mix(background.B, foreground.B));
    }
}
