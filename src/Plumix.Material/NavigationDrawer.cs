using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/navigation_drawer.dart

public sealed class NavigationDrawer : StatelessWidget
{
    private static readonly TimeSpan SelectionAnimationDuration = TimeSpan.FromMilliseconds(500);
    private static readonly Thickness DefaultTilePadding = new(12, 0);

    public NavigationDrawer(
        IReadOnlyList<Widget> children,
        Widget? header = null,
        Widget? footer = null,
        Color? backgroundColor = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        double? elevation = null,
        Color? indicatorColor = null,
        ShapeBorder? indicatorShape = null,
        Action<int>? onDestinationSelected = null,
        int? selectedIndex = 0,
        Thickness? tilePadding = null,
        Key? key = null) : base(key)
    {
        if (children is null) throw new ArgumentNullException(nameof(children));
        if (elevation.HasValue && (!double.IsFinite(elevation.Value) || elevation.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(elevation), "Elevation must be non-negative and finite.");
        }

        var effectiveTilePadding = tilePadding ?? DefaultTilePadding;
        if (effectiveTilePadding.Left < 0 || effectiveTilePadding.Top < 0
            || effectiveTilePadding.Right < 0 || effectiveTilePadding.Bottom < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tilePadding), "Tile padding must be non-negative.");
        }

        Children = children;
        Header = header;
        Footer = footer;
        BackgroundColor = backgroundColor;
        ShadowColor = shadowColor;
        SurfaceTintColor = surfaceTintColor;
        Elevation = elevation;
        IndicatorColor = indicatorColor;
        IndicatorShape = indicatorShape;
        OnDestinationSelected = onDestinationSelected;
        SelectedIndex = selectedIndex;
        TilePadding = effectiveTilePadding;
    }

    public IReadOnlyList<Widget> Children { get; }
    public Widget? Header { get; }
    public Widget? Footer { get; }
    public Color? BackgroundColor { get; }
    public Color? ShadowColor { get; }
    public Color? SurfaceTintColor { get; }
    public double? Elevation { get; }
    public Color? IndicatorColor { get; }
    public ShapeBorder? IndicatorShape { get; }
    public Action<int>? OnDestinationSelected { get; }
    public int? SelectedIndex { get; }
    public Thickness TilePadding { get; }

    public override Widget Build(BuildContext context)
    {
        int totalDestinations = Children.Count(child => child is NavigationDrawerDestination);
        var wrappedChildren = new List<Widget>(Children.Count);
        int destinationIndex = 0;
        foreach (var child in Children)
        {
            if (child is not NavigationDrawerDestination destination)
            {
                wrappedChildren.Add(child);
                continue;
            }

            int index = destinationIndex++;
            wrappedChildren.Add(new NavigationDrawerDestinationTile(
                destination: destination,
                index: index,
                totalDestinations: totalDestinations,
                selected: index == SelectedIndex,
                indicatorColor: IndicatorColor,
                indicatorShape: IndicatorShape,
                tilePadding: TilePadding,
                onTap: () => OnDestinationSelected?.Invoke(index),
                duration: SelectionAnimationDuration,
                key: destination.Key ?? new ValueKey<int>(index)));
        }

        var drawerTheme = NavigationDrawerTheme.Of(context);
        var columnChildren = new List<Widget>();
        if (Header is not null) columnChildren.Add(Header);
        columnChildren.Add(new Expanded(child: new ListView(children: wrappedChildren)));
        if (Footer is not null) columnChildren.Add(Footer);

        return new Drawer(
            backgroundColor: BackgroundColor ?? drawerTheme.BackgroundColor,
            shadowColor: ShadowColor ?? drawerTheme.ShadowColor,
            surfaceTintColor: SurfaceTintColor ?? drawerTheme.SurfaceTintColor,
            elevation: Elevation ?? drawerTheme.Elevation,
            child: new SafeArea(
                bottom: false,
                child: new Column(children: columnChildren)));
    }
}

public sealed class NavigationDrawerDestination : StatelessWidget
{
    public NavigationDrawerDestination(
        Widget icon,
        Widget label,
        Color? backgroundColor = null,
        Widget? selectedIcon = null,
        bool enabled = true,
        Key? key = null) : base(key)
    {
        Icon = icon ?? throw new ArgumentNullException(nameof(icon));
        Label = label ?? throw new ArgumentNullException(nameof(label));
        BackgroundColor = backgroundColor;
        SelectedIcon = selectedIcon;
        Enabled = enabled;
    }

    public Color? BackgroundColor { get; }
    public Widget Icon { get; }
    public Widget? SelectedIcon { get; }
    public Widget Label { get; }
    public bool Enabled { get; }

    public override Widget Build(BuildContext context)
    {
        throw new InvalidOperationException(
            "NavigationDrawerDestination widgets must be children of NavigationDrawer.");
    }
}

internal sealed class NavigationDrawerDestinationTile : StatefulWidget
{
    public NavigationDrawerDestinationTile(
        NavigationDrawerDestination destination,
        int index,
        int totalDestinations,
        bool selected,
        Color? indicatorColor,
        ShapeBorder? indicatorShape,
        Thickness tilePadding,
        Action onTap,
        TimeSpan duration,
        Key? key = null) : base(key)
    {
        Destination = destination;
        Index = index;
        TotalDestinations = totalDestinations;
        Selected = selected;
        IndicatorColor = indicatorColor;
        IndicatorShape = indicatorShape;
        TilePadding = tilePadding;
        OnTap = onTap;
        Duration = duration;
    }

    public NavigationDrawerDestination Destination { get; }
    public int Index { get; }
    public int TotalDestinations { get; }
    public bool Selected { get; }
    public Color? IndicatorColor { get; }
    public ShapeBorder? IndicatorShape { get; }
    public Thickness TilePadding { get; }
    public Action OnTap { get; }
    public TimeSpan Duration { get; }

    public override State CreateState() => new NavigationDrawerDestinationTileState();
}

internal sealed class NavigationDrawerDestinationTileState : State
{
    private AnimationController? _controller;

    private NavigationDrawerDestinationTile CurrentWidget =>
        (NavigationDrawerDestinationTile)StateWidget;

    public override void InitState()
    {
        CreateController(CurrentWidget.Selected ? 1 : 0);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var old = (NavigationDrawerDestinationTile)oldWidget;
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

    public override Widget Build(BuildContext context)
    {
        var widget = CurrentWidget;
        var destination = widget.Destination;
        var theme = Theme.Of(context);
        ColorScheme colors = theme.ColorScheme;
        var drawerTheme = NavigationDrawerTheme.Of(context);
        var states = destination.Enabled ? MaterialState.None : MaterialState.Disabled;
        if (widget.Selected) states |= MaterialState.Selected;

        double progress = _controller?.Evaluate() ?? (widget.Selected ? 1 : 0);
        var iconTheme = drawerTheme.IconTheme?.Resolve(states)
                        ?? ResolveDefaultIconTheme(theme, states);
        var labelStyle = drawerTheme.LabelTextStyle?.Resolve(states)
                         ?? ResolveDefaultLabelStyle(theme, states);
        var indicatorShape = widget.IndicatorShape
                             ?? drawerTheme.IndicatorShape
                             ?? new StadiumBorder();
        var indicatorSize = drawerTheme.IndicatorSize ?? new Size(336, 56);
        var indicatorColor = widget.IndicatorColor
                             ?? drawerTheme.IndicatorColor
                             ?? colors.SecondaryContainer;
        double tileHeight = drawerTheme.TileHeight ?? 56;

        var icon = widget.Selected && destination.SelectedIcon is not null
            ? destination.SelectedIcon
            : destination.Icon;
        Widget content = new Stack(
            alignment: Alignment.Center,
            children:
            [
                new NavigationIndicator(
                    animationValue: progress,
                    color: indicatorColor,
                    width: indicatorSize.Width,
                    height: indicatorSize.Height,
                    shape: indicatorShape),
                new Row(
                    children:
                    [
                        new SizedBox(width: 16),
                        new IconTheme(iconTheme, icon),
                        new SizedBox(width: 12),
                        new Expanded(
                            child: new DefaultTextStyle(
                                style: labelStyle,
                                child: destination.Label))
                    ])
            ]);

        var overlayColor = MaterialStateProperty<Color?>.ResolveWith(buttonStates =>
            buttonStates.HasFlag(MaterialState.Pressed) || buttonStates.HasFlag(MaterialState.Focused)
                ? NavigationSurfaceUtilities.WithOpacity(colors.OnSurface, 0.10)
                : buttonStates.HasFlag(MaterialState.Hovered)
                    ? NavigationSurfaceUtilities.WithOpacity(colors.OnSurface, 0.08)
                    : null);
        string indexLabel = MaterialLocalizations.Of(context).TabLabel(
            widget.Index + 1,
            widget.TotalDestinations);
        string textLabel = destination.Label is Text text ? $"{text.Data}\n{indexLabel}" : indexLabel;
        var buttonStyle = new ButtonStyle(
            ForegroundColor: MaterialStateProperty<Color?>.All(labelStyle.Color),
            BackgroundColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
            OverlayColor: overlayColor,
            SplashColor: overlayColor,
            IconColor: MaterialStateProperty<Color?>.All(iconTheme.Color),
            IconSize: MaterialStateProperty<double?>.All(iconTheme.Size),
            TextStyle: MaterialStateProperty<TextStyle?>.All(labelStyle),
            Padding: MaterialStateProperty<EdgeInsetsGeometry?>.All(default),
            Shape: MaterialStateProperty<OutlinedBorder?>.All(new RoundedRectangleBorder(borderRadius:
                ShapeBorderGeometry.ResolveRadius(indicatorShape))),
            MinimumSize: MaterialStateProperty<Size?>.All(new Size(0, tileHeight)),
            TapTargetSize: MaterialTapTargetSize.ShrinkWrap,
            Alignment: Alignment.Center);

        Widget result = new Padding(
            widget.TilePadding,
            new SizedBox(
                height: tileHeight,
                child: new MaterialButtonCore(
                    child: content,
                    onPressed: destination.Enabled ? widget.OnTap : null,
                    style: buttonStyle,
                    isSelected: widget.Selected,
                    isSemanticButton: true,
                    semanticLabel: textLabel,
                    clipBehavior: Clip.None)));

        if (destination.BackgroundColor.HasValue)
        {
            result = new DecoratedBox(
                new BoxDecoration(Color: destination.BackgroundColor.Value),
                result);
        }

        return new MergeSemantics(result);
    }

    public override void Dispose()
    {
        DisposeController();
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

    private void HandleAnimationChanged() => SetState(() => { });

    private void DisposeController()
    {
        if (_controller is null) return;
        _controller.Changed -= HandleAnimationChanged;
        _controller.Dispose();
        _controller = null;
    }

    private static IconThemeData ResolveDefaultIconTheme(ThemeData theme, MaterialState states)
    {
        ColorScheme colors = theme.ColorScheme;
        Color color = states.HasFlag(MaterialState.Disabled)
            ? NavigationSurfaceUtilities.WithOpacity(colors.OnSurfaceVariant, 0.38)
            : states.HasFlag(MaterialState.Selected)
                ? colors.OnSecondaryContainer
                : colors.OnSurfaceVariant;
        return new IconThemeData(Color: color, Size: 24);
    }

    private static TextStyle ResolveDefaultLabelStyle(ThemeData theme, MaterialState states)
    {
        ColorScheme colors = theme.ColorScheme;
        Color color = states.HasFlag(MaterialState.Disabled)
            ? NavigationSurfaceUtilities.WithOpacity(colors.OnSurfaceVariant, 0.38)
            : states.HasFlag(MaterialState.Selected)
                ? colors.OnSecondaryContainer
                : colors.OnSurfaceVariant;
        return theme.TextTheme.LabelLarge.CopyWith(color: color);
    }
}
