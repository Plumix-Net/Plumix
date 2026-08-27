using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Transform = Plumix.Widgets.Transform;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/bottom_navigation_bar.dart

/// <summary>Dart's `BottomNavigationBarType`.</summary>
public enum BottomNavigationBarType
{
    /// <summary>The items have fixed width.</summary>
    Fixed,

    /// <summary>The location and size of the items animate and the labels fade in on tap.</summary>
    Shifting,
}

/// <summary>Dart's `BottomNavigationBarLandscapeLayout`.</summary>
public enum BottomNavigationBarLandscapeLayout
{
    /// <summary>Items are spread out over the whole available width.</summary>
    Spread,

    /// <summary>Items are horizontally centered within a portrait-width box.</summary>
    Centered,

    /// <summary>Each item's icon and label sit side by side in a row.</summary>
    Linear,
}

public sealed class BottomNavigationBar : StatefulWidget
{
    public BottomNavigationBar(
        IReadOnlyList<BottomNavigationBarItem> items,
        Action<int>? onTap = null,
        int currentIndex = 0,
        double? elevation = null,
        BottomNavigationBarType? type = null,
        Color? fixedColor = null,
        Color? backgroundColor = null,
        double iconSize = 24.0,
        Color? selectedItemColor = null,
        Color? unselectedItemColor = null,
        IconThemeData? selectedIconTheme = null,
        IconThemeData? unselectedIconTheme = null,
        double selectedFontSize = 14.0,
        double unselectedFontSize = 12.0,
        TextStyle? selectedLabelStyle = null,
        TextStyle? unselectedLabelStyle = null,
        bool? showSelectedLabels = null,
        bool? showUnselectedLabels = null,
        MouseCursor? mouseCursor = null,
        bool? enableFeedback = null,
        BottomNavigationBarLandscapeLayout? landscapeLayout = null,
        bool useLegacyColorScheme = true,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count < 2)
        {
            throw new ArgumentException("BottomNavigationBar requires at least two items.", nameof(items));
        }

        if (items.Any(item => item.Label is null))
        {
            throw new ArgumentException("Every item must have a non-null label", nameof(items));
        }

        if (currentIndex < 0 || currentIndex >= items.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(currentIndex), "Current index must be within item range.");
        }

        if (elevation is < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(elevation), "Elevation must be non-negative.");
        }

        if (iconSize < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(iconSize), "Icon size must be non-negative.");
        }

        if (selectedItemColor is not null && fixedColor is not null)
        {
            throw new ArgumentException(
                "Either selectedItemColor or fixedColor can be specified, but not both",
                nameof(selectedItemColor));
        }

        if (selectedFontSize < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(selectedFontSize),
                "Selected font size must be non-negative.");
        }

        if (unselectedFontSize < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unselectedFontSize),
                "Unselected font size must be non-negative.");
        }

        Items = items;
        OnTap = onTap;
        CurrentIndex = currentIndex;
        Elevation = elevation;
        Type = type;
        BackgroundColor = backgroundColor;
        IconSize = iconSize;
        SelectedItemColor = selectedItemColor ?? fixedColor;
        UnselectedItemColor = unselectedItemColor;
        SelectedIconTheme = selectedIconTheme;
        UnselectedIconTheme = unselectedIconTheme;
        SelectedFontSize = selectedFontSize;
        UnselectedFontSize = unselectedFontSize;
        SelectedLabelStyle = selectedLabelStyle;
        UnselectedLabelStyle = unselectedLabelStyle;
        ShowSelectedLabels = showSelectedLabels;
        ShowUnselectedLabels = showUnselectedLabels;
        MouseCursor = mouseCursor;
        EnableFeedback = enableFeedback;
        LandscapeLayout = landscapeLayout;
        UseLegacyColorScheme = useLegacyColorScheme;
    }

    public IReadOnlyList<BottomNavigationBarItem> Items { get; }

    public Action<int>? OnTap { get; }

    public int CurrentIndex { get; }

    public double? Elevation { get; }

    public BottomNavigationBarType? Type { get; }

    /// <summary>Dart's `BottomNavigationBar.fixedColor` getter — an alias of `selectedItemColor`.</summary>
    public Color? FixedColor => SelectedItemColor;

    public Color? BackgroundColor { get; }

    public double IconSize { get; }

    public Color? SelectedItemColor { get; }

    public Color? UnselectedItemColor { get; }

    public IconThemeData? SelectedIconTheme { get; }

    public IconThemeData? UnselectedIconTheme { get; }

    public double SelectedFontSize { get; }

    public double UnselectedFontSize { get; }

    public TextStyle? SelectedLabelStyle { get; }

    public TextStyle? UnselectedLabelStyle { get; }

    public bool? ShowSelectedLabels { get; }

    public bool? ShowUnselectedLabels { get; }

    public MouseCursor? MouseCursor { get; }

    public bool? EnableFeedback { get; }

    public BottomNavigationBarLandscapeLayout? LandscapeLayout { get; }

    public bool UseLegacyColorScheme { get; }

    public override State CreateState() => new BottomNavigationBarState();
}

/// <summary>Dart's private `_BottomNavigationTile`.</summary>
internal sealed class BottomNavigationTile : StatelessWidget
{
    internal BottomNavigationTile(
        BottomNavigationBarType type,
        BottomNavigationBarItem item,
        Animation<double> animation,
        double iconSize,
        Action? onTap = null,
        ColorTween? labelColorTween = null,
        ColorTween? iconColorTween = null,
        double? flex = null,
        bool selected = false,
        IconThemeData? selectedIconTheme = null,
        IconThemeData? unselectedIconTheme = null,
        bool showSelectedLabels = true,
        bool showUnselectedLabels = true,
        string? indexLabel = null,
        MouseCursor? mouseCursor = null,
        bool enableFeedback = true,
        BottomNavigationBarLandscapeLayout layout = BottomNavigationBarLandscapeLayout.Spread,
        TextStyle? selectedLabelStyle = null,
        TextStyle? unselectedLabelStyle = null,
        Key? key = null) : base(key)
    {
        Type = type;
        Item = item;
        Animation = animation;
        IconSize = iconSize;
        OnTap = onTap;
        LabelColorTween = labelColorTween;
        IconColorTween = iconColorTween;
        Flex = flex;
        Selected = selected;
        SelectedIconTheme = selectedIconTheme;
        UnselectedIconTheme = unselectedIconTheme;
        ShowSelectedLabels = showSelectedLabels;
        ShowUnselectedLabels = showUnselectedLabels;
        IndexLabel = indexLabel;
        MouseCursor = mouseCursor;
        EnableFeedback = enableFeedback;
        Layout = layout;
        SelectedLabelStyle = selectedLabelStyle;
        UnselectedLabelStyle = unselectedLabelStyle;
    }

    internal BottomNavigationBarType Type { get; }

    internal BottomNavigationBarItem Item { get; }

    internal Animation<double> Animation { get; }

    internal double IconSize { get; }

    internal Action? OnTap { get; }

    internal ColorTween? LabelColorTween { get; }

    internal ColorTween? IconColorTween { get; }

    internal double? Flex { get; }

    internal bool Selected { get; }

    internal IconThemeData? SelectedIconTheme { get; }

    internal IconThemeData? UnselectedIconTheme { get; }

    internal TextStyle? SelectedLabelStyle { get; }

    internal TextStyle? UnselectedLabelStyle { get; }

    internal string? IndexLabel { get; }

    internal bool ShowSelectedLabels { get; }

    internal bool ShowUnselectedLabels { get; }

    internal MouseCursor? MouseCursor { get; }

    internal bool EnableFeedback { get; }

    internal BottomNavigationBarLandscapeLayout Layout { get; }

    public override Widget Build(BuildContext context)
    {
        // In order to use the flex container to grow the tile during animation, we need to divide the
        // changes in flex allotment into smaller pieces to avoid overanimating.
        int size = Type switch
        {
            BottomNavigationBarType.Fixed => 1,
            _ => (int)Math.Round((Flex ?? 1.0) * 1000.0),
        };

        double selectedFontSize = SelectedLabelStyle?.FontSize ?? 0.0;

        double selectedIconSize = SelectedIconTheme?.Size ?? IconSize;
        double unselectedIconSize = UnselectedIconTheme?.Size ?? IconSize;

        // The amount that the selected icon is bigger than the unselected icons,
        // (or zero if the selected icon is not bigger than the unselected icons).
        double selectedIconDiff = Math.Max(selectedIconSize - unselectedIconSize, 0);

        // The amount that the unselected icons are bigger than the selected icon,
        // (or zero if the unselected icons are not any bigger than the selected icon).
        double unselectedIconDiff = Math.Max(unselectedIconSize - selectedIconSize, 0);

        // The effective title is the smaller of the two font sizes; the padding is
        // driven by the difference between the two icon sizes so that the icons stay
        // vertically centered while they resize.
        double bottomPadding;
        double topPadding;
        if (ShowSelectedLabels && !ShowUnselectedLabels)
        {
            bottomPadding = new DoubleTween(
                begin: selectedIconDiff / 2.0,
                end: (selectedFontSize / 2.0) - (unselectedIconDiff / 2.0)).Evaluate(Animation.Value);
            topPadding = new DoubleTween(
                begin: selectedFontSize + (selectedIconDiff / 2.0),
                end: (selectedFontSize / 2.0) - (unselectedIconDiff / 2.0)).Evaluate(Animation.Value);
        }
        else if (!ShowSelectedLabels && !ShowUnselectedLabels)
        {
            bottomPadding = new DoubleTween(
                begin: selectedIconDiff / 2.0,
                end: unselectedIconDiff / 2.0).Evaluate(Animation.Value);
            topPadding = new DoubleTween(
                begin: selectedFontSize + (selectedIconDiff / 2.0),
                end: selectedFontSize + (unselectedIconDiff / 2.0)).Evaluate(Animation.Value);
        }
        else
        {
            bottomPadding = new DoubleTween(
                begin: (selectedFontSize / 2.0) + (selectedIconDiff / 2.0),
                end: (selectedFontSize / 2.0) + (unselectedIconDiff / 2.0)).Evaluate(Animation.Value);
            topPadding = new DoubleTween(
                begin: (selectedFontSize / 2.0) + (selectedIconDiff / 2.0),
                end: (selectedFontSize / 2.0) + (unselectedIconDiff / 2.0)).Evaluate(Animation.Value);
        }

        string? effectiveTooltip = Item.Tooltip == string.Empty ? null : Item.Tooltip;

        Widget result = new InkResponse(
            onTap: OnTap,
            mouseCursor: MouseCursor,
            enableFeedback: EnableFeedback,
            child: new Padding(
                insets: new Thickness(0, topPadding, 0, bottomPadding),
                child: new BottomNavigationTileContent(
                    layout: Layout,
                    icon: new BottomNavigationTileIcon(
                        colorTween: IconColorTween,
                        animation: Animation,
                        iconSize: IconSize,
                        selected: Selected,
                        item: Item,
                        selectedIconTheme: SelectedIconTheme,
                        unselectedIconTheme: UnselectedIconTheme),
                    label: new BottomNavigationTileLabel(
                        colorTween: LabelColorTween,
                        animation: Animation,
                        item: Item,
                        selectedLabelStyle: SelectedLabelStyle,
                        unselectedLabelStyle: UnselectedLabelStyle,
                        showSelectedLabels: ShowSelectedLabels,
                        showUnselectedLabels: ShowUnselectedLabels))));

        if (effectiveTooltip is not null)
        {
            result = new Tooltip(
                message: effectiveTooltip,
                preferBelow: false,
                verticalOffset: selectedIconSize + selectedFontSize,
                excludeFromSemantics: true,
                child: result);
        }

        return new Expanded(
            flex: size,
            child: new Semantics(
                selected: Selected,
                flags: SemanticsFlags.IsButton,
                container: true,
                child: new Stack(
                    children:
                    [
                        result,
                        new Semantics(label: IndexLabel),
                    ])));
    }
}

/// <summary>Dart's private `_Tile` — the icon/label pair, laid out per landscape layout.</summary>
internal sealed class BottomNavigationTileContent : StatelessWidget
{
    internal BottomNavigationTileContent(
        BottomNavigationBarLandscapeLayout layout,
        Widget icon,
        Widget label,
        Key? key = null) : base(key)
    {
        Layout = layout;
        Icon = icon;
        Label = label;
    }

    internal BottomNavigationBarLandscapeLayout Layout { get; }

    internal Widget Icon { get; }

    internal Widget Label { get; }

    public override Widget Build(BuildContext context)
    {
        if (MediaQuery.OrientationOf(context) == Orientation.Landscape
            && Layout == BottomNavigationBarLandscapeLayout.Linear)
        {
            return new Align(
                heightFactor: 1,
                child: new Row(
                    mainAxisSize: MainAxisSize.Min,
                    spacing: 8,
                    children:
                    [
                        Icon,
                        new Flexible(child: new IntrinsicWidth(child: Label)),
                    ]));
        }

        return new Column(
            mainAxisAlignment: MainAxisAlignment.SpaceBetween,
            mainAxisSize: MainAxisSize.Min,
            children: [Icon, Label]);
    }
}

/// <summary>Dart's private `_TileIcon`.</summary>
internal sealed class BottomNavigationTileIcon : StatelessWidget
{
    internal BottomNavigationTileIcon(
        ColorTween? colorTween,
        Animation<double> animation,
        double iconSize,
        bool selected,
        BottomNavigationBarItem item,
        IconThemeData? selectedIconTheme,
        IconThemeData? unselectedIconTheme,
        Key? key = null) : base(key)
    {
        ColorTween = colorTween;
        Animation = animation;
        IconSize = iconSize;
        Selected = selected;
        Item = item;
        SelectedIconTheme = selectedIconTheme;
        UnselectedIconTheme = unselectedIconTheme;
    }

    internal ColorTween? ColorTween { get; }

    internal Animation<double> Animation { get; }

    internal double IconSize { get; }

    internal bool Selected { get; }

    internal BottomNavigationBarItem Item { get; }

    internal IconThemeData? SelectedIconTheme { get; }

    internal IconThemeData? UnselectedIconTheme { get; }

    public override Widget Build(BuildContext context)
    {
        Color? iconColor = ColorTween?.Evaluate(Animation.Value);
        var defaultIconTheme = new IconThemeData(Color: iconColor, Size: IconSize);
        IconThemeData iconThemeData = IconThemeData.Lerp(
            defaultIconTheme.Merge(UnselectedIconTheme),
            defaultIconTheme.Merge(SelectedIconTheme),
            Animation.Value);

        return new Align(
            alignment: Alignment.TopCenter,
            heightFactor: 1.0,
            child: new IconTheme(
                data: iconThemeData,
                child: Selected ? Item.ActiveIcon : Item.Icon));
    }
}

/// <summary>Dart's private `_Label`.</summary>
internal sealed class BottomNavigationTileLabel : StatelessWidget
{
    internal BottomNavigationTileLabel(
        ColorTween? colorTween,
        Animation<double> animation,
        BottomNavigationBarItem item,
        TextStyle? selectedLabelStyle,
        TextStyle? unselectedLabelStyle,
        bool showSelectedLabels,
        bool showUnselectedLabels,
        Key? key = null) : base(key)
    {
        ColorTween = colorTween;
        Animation = animation;
        Item = item;
        SelectedLabelStyle = selectedLabelStyle;
        UnselectedLabelStyle = unselectedLabelStyle;
        ShowSelectedLabels = showSelectedLabels;
        ShowUnselectedLabels = showUnselectedLabels;
    }

    internal ColorTween? ColorTween { get; }

    internal Animation<double> Animation { get; }

    internal BottomNavigationBarItem Item { get; }

    internal TextStyle? SelectedLabelStyle { get; }

    internal TextStyle? UnselectedLabelStyle { get; }

    internal bool ShowSelectedLabels { get; }

    internal bool ShowUnselectedLabels { get; }

    public override Widget Build(BuildContext context)
    {
        double? selectedFontSize = SelectedLabelStyle?.FontSize;
        double? unselectedFontSize = UnselectedLabelStyle?.FontSize;

        TextStyle customStyle = TextStyle.Lerp(
            UnselectedLabelStyle ?? new TextStyle(),
            SelectedLabelStyle ?? new TextStyle(),
            Animation.Value);

        double scale = new DoubleTween(
            begin: (unselectedFontSize ?? 0.0) / (selectedFontSize ?? 1.0),
            end: 1.0).Evaluate(Animation.Value);

        Widget text = DefaultTextStyle.Merge(
            style: customStyle.CopyWith(
                fontSize: selectedFontSize,
                color: ColorTween?.Evaluate(Animation.Value)),
            child: new Transform(
                transform: Matrix4.Diagonal3Values(scale, scale, scale),
                alignment: Alignment.BottomCenter,
                child: new Text(Item.Label!, semanticsLabel: Item.SemanticsLabel)));

        if (!ShowUnselectedLabels && !ShowSelectedLabels)
        {
            text = Visibility.Maintain(visible: false, child: text);
        }
        else if (!ShowUnselectedLabels)
        {
            text = new FadeTransition(
                alwaysIncludeSemantics: true,
                opacity: Animation,
                child: text);
        }
        else if (!ShowSelectedLabels)
        {
            text = new FadeTransition(
                alwaysIncludeSemantics: true,
                opacity: new DoubleTween(begin: 1.0, end: 0.0).Animate(Animation),
                child: text);
        }

        text = new Align(alignment: Alignment.BottomCenter, heightFactor: 1.0, child: text);

        if (Item.Label is not null)
        {
            // Do not grow text in bottom navigation bar when we can add more anyway.
            text = MediaQuery.WithClampedTextScaling(context, text, maxScaleFactor: 1.0);
        }

        return text;
    }
}

internal sealed class BottomNavigationBarState : State
{
    private static readonly DoubleTween FlexTween = new(begin: 1.0, end: 1.5);

    private readonly Queue<BottomNavigationBarCircle> _circles = new();

    private List<AnimationController> _controllers = [];
    private List<CurvedAnimation> _animations = [];

    /// <summary>
    /// A queue of color splashes currently being animated.
    /// </summary>
    private Color? _backgroundColor;

    internal IReadOnlyList<CurvedAnimation> Animations => _animations;

    private BottomNavigationBar Widget => (BottomNavigationBar)StateWidget;

    private BottomNavigationBarType EffectiveType(BottomNavigationBarThemeData bottomTheme)
    {
        return Widget.Type
               ?? bottomTheme.Type
               ?? (Widget.Items.Count <= 3
                   ? BottomNavigationBarType.Fixed
                   : BottomNavigationBarType.Shifting);
    }

    private bool DefaultShowUnselected(BottomNavigationBarType type)
    {
        return type switch
        {
            BottomNavigationBarType.Shifting => false,
            _ => true,
        };
    }

    public override void InitState()
    {
        base.InitState();
        ResetState();
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var old = (BottomNavigationBar)oldWidget;

        // No animated segue if the length of the items list changes.
        if (Widget.Items.Count != old.Items.Count)
        {
            ResetState();
            return;
        }

        if (Widget.CurrentIndex != old.CurrentIndex)
        {
            var bottomTheme = BottomNavigationBarTheme.Of(Context);
            if (EffectiveType(bottomTheme) == BottomNavigationBarType.Shifting)
            {
                PushCircle(Widget.CurrentIndex);
            }

            _controllers[old.CurrentIndex].Reverse();
            _controllers[Widget.CurrentIndex].Forward();
        }
        else
        {
            if (_backgroundColor != Widget.Items[Widget.CurrentIndex].BackgroundColor)
            {
                _backgroundColor = Widget.Items[Widget.CurrentIndex].BackgroundColor;
            }
        }
    }

    public override void Dispose()
    {
        foreach (AnimationController controller in _controllers)
        {
            controller.Dispose();
        }

        foreach (BottomNavigationBarCircle circle in _circles)
        {
            circle.Dispose();
        }

        foreach (CurvedAnimation animation in _animations)
        {
            animation.Dispose();
        }

        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        var bottomTheme = BottomNavigationBarTheme.Of(context);
        BottomNavigationBarType type = EffectiveType(bottomTheme);
        BottomNavigationBarLandscapeLayout layout = Widget.LandscapeLayout
                                                    ?? bottomTheme.LandscapeLayout
                                                    ?? BottomNavigationBarLandscapeLayout.Spread;
        double additionalBottomPadding = MediaQuery.ViewPaddingOf(context).Bottom;

        Color? backgroundColor = type switch
        {
            BottomNavigationBarType.Fixed => Widget.BackgroundColor ?? bottomTheme.BackgroundColor,
            _ => _backgroundColor,
        };

        return new Semantics(
            explicitChildNodes: true,
            child: new BottomNavigationBarSurface(
                layout: layout,
                elevation: Widget.Elevation ?? bottomTheme.Elevation ?? 8.0,
                color: backgroundColor,
                child: new ConstrainedBox(
                    constraints: new BoxConstraints(
                        MinHeight: MaterialConstants.BottomNavigationBarHeight + additionalBottomPadding),
                    child: new CustomPaint(
                        painter: new BottomNavigationBarRadialPainter(
                            circles: [.. _circles],
                            textDirection: Directionality.Of(context)),
                        child: new Material(
                            // Splashes.
                            type: MaterialType.Transparency,
                            child: new Padding(
                                insets: new Thickness(0, 0, 0, additionalBottomPadding),
                                child: MediaQuery.RemovePadding(
                                    context: context,
                                    removeBottom: true,
                                    child: DefaultTextStyle.Merge(
                                        overflow: TextOverflow.Ellipsis,
                                        child: new Row(
                                            mainAxisAlignment: MainAxisAlignment.SpaceBetween,
                                            children: CreateTiles(context, bottomTheme, type, layout))))))))));
    }

    private void ResetState()
    {
        foreach (AnimationController controller in _controllers)
        {
            controller.Dispose();
        }

        foreach (BottomNavigationBarCircle circle in _circles)
        {
            circle.Dispose();
        }

        foreach (CurvedAnimation animation in _animations)
        {
            animation.Dispose();
        }

        _circles.Clear();

        _controllers = new List<AnimationController>(Widget.Items.Count);
        _animations = new List<CurvedAnimation>(Widget.Items.Count);
        for (int index = 0; index < Widget.Items.Count; index++)
        {
            var controller = new AnimationController(
                duration: MaterialConstants.ThemeAnimationDuration,
                vsync: this);
            controller.AddListener(Rebuild);
            _controllers.Add(controller);
        }

        for (int index = 0; index < Widget.Items.Count; index++)
        {
            _animations.Add(new CurvedAnimation(
                parent: _controllers[index],
                curve: Curves.FastOutSlowIn,
                reverseCurve: Curves.Flipped(Curves.FastOutSlowIn)));
        }

        _controllers[Widget.CurrentIndex].SetValue(1.0);
        _backgroundColor = Widget.Items[Widget.CurrentIndex].BackgroundColor;
    }

    private void Rebuild()
    {
        SetState(() =>
        {
            // Rebuilding when any of the controllers tick, i.e. when the items are
            // animated.
        });
    }

    private void PushCircle(int index)
    {
        if (Widget.Items[index].BackgroundColor is not { } color)
        {
            return;
        }

        var circle = new BottomNavigationBarCircle(
            state: this,
            index: index,
            color: color,
            vsync: this);
        circle.Controller.AddStatusListener(status =>
        {
            if (!status.IsCompleted())
            {
                return;
            }

            SetState(() =>
            {
                BottomNavigationBarCircle removed = _circles.Dequeue();
                _backgroundColor = removed.Color;
                removed.Dispose();
            });
        });
        _circles.Enqueue(circle);
    }

    internal double EvaluateFlex(Animation<double> animation) => FlexTween.Evaluate(animation.Value);

    private List<Widget> CreateTiles(
        BuildContext context,
        BottomNavigationBarThemeData bottomTheme,
        BottomNavigationBarType type,
        BottomNavigationBarLandscapeLayout layout)
    {
        MaterialLocalizations localizations = MaterialLocalizations.Of(context);
        ThemeData themeData = Theme.Of(context);

        Color themeColor = themeData.Brightness switch
        {
            Brightness.Light => themeData.ColorScheme.Primary,
            _ => themeData.ColorScheme.Secondary,
        };

        ColorTween colorTween;
        ColorTween labelColorTween;
        ColorTween iconColorTween;

        TextStyle effectiveSelectedLabelStyle = EffectiveTextStyle(
            Widget.SelectedLabelStyle ?? bottomTheme.SelectedLabelStyle,
            Widget.SelectedFontSize);
        TextStyle effectiveUnselectedLabelStyle = EffectiveTextStyle(
            Widget.UnselectedLabelStyle ?? bottomTheme.UnselectedLabelStyle,
            Widget.UnselectedFontSize);

        IconThemeData effectiveSelectedIconTheme = EffectiveIconTheme(
            Widget.SelectedIconTheme ?? bottomTheme.SelectedIconTheme,
            Widget.SelectedItemColor ?? bottomTheme.SelectedItemColor ?? themeColor);
        IconThemeData effectiveUnselectedIconTheme = EffectiveIconTheme(
            Widget.UnselectedIconTheme ?? bottomTheme.UnselectedIconTheme,
            Widget.UnselectedItemColor ?? bottomTheme.UnselectedItemColor ?? themeData.UnselectedWidgetColor);

        switch (type)
        {
            case BottomNavigationBarType.Fixed:
                colorTween = new ColorTween(
                    begin: Widget.UnselectedItemColor
                           ?? bottomTheme.UnselectedItemColor
                           ?? themeData.UnselectedWidgetColor,
                    end: Widget.SelectedItemColor
                         ?? bottomTheme.SelectedItemColor
                         ?? Widget.FixedColor
                         ?? themeColor);
                labelColorTween = new ColorTween(
                    begin: effectiveUnselectedLabelStyle.Color
                           ?? Widget.UnselectedItemColor
                           ?? bottomTheme.UnselectedItemColor
                           ?? themeData.UnselectedWidgetColor,
                    end: effectiveSelectedLabelStyle.Color
                         ?? Widget.SelectedItemColor
                         ?? bottomTheme.SelectedItemColor
                         ?? Widget.FixedColor
                         ?? themeColor);

                // Dart reads the *selected* icon theme for `begin` and the *unselected* one for `end`
                // here; reproduced verbatim (`bottom_navigation_bar.dart`, fixed `iconColorTween`).
                iconColorTween = new ColorTween(
                    begin: effectiveSelectedIconTheme.Color
                           ?? Widget.UnselectedItemColor
                           ?? bottomTheme.UnselectedItemColor
                           ?? themeData.UnselectedWidgetColor,
                    end: effectiveUnselectedIconTheme.Color
                         ?? Widget.SelectedItemColor
                         ?? bottomTheme.SelectedItemColor
                         ?? Widget.FixedColor
                         ?? themeColor);
                break;
            default:
                colorTween = new ColorTween(
                    begin: Widget.UnselectedItemColor
                           ?? bottomTheme.UnselectedItemColor
                           ?? themeData.ColorScheme.Surface,
                    end: Widget.SelectedItemColor
                         ?? bottomTheme.SelectedItemColor
                         ?? themeData.ColorScheme.Surface);
                labelColorTween = new ColorTween(
                    begin: effectiveUnselectedLabelStyle.Color
                           ?? Widget.UnselectedItemColor
                           ?? bottomTheme.UnselectedItemColor
                           ?? themeData.ColorScheme.Surface,
                    end: effectiveSelectedLabelStyle.Color
                         ?? Widget.SelectedItemColor
                         ?? bottomTheme.SelectedItemColor
                         ?? themeColor);
                iconColorTween = new ColorTween(
                    begin: effectiveUnselectedIconTheme.Color
                           ?? Widget.UnselectedItemColor
                           ?? bottomTheme.UnselectedItemColor
                           ?? themeData.ColorScheme.Surface,
                    end: effectiveSelectedIconTheme.Color
                         ?? Widget.SelectedItemColor
                         ?? bottomTheme.SelectedItemColor
                         ?? themeColor);
                break;
        }

        bool showSelectedLabels = Widget.ShowSelectedLabels ?? bottomTheme.ShowSelectedLabels ?? true;
        bool showUnselectedLabels = Widget.ShowUnselectedLabels
                                    ?? bottomTheme.ShowUnselectedLabels
                                    ?? DefaultShowUnselected(type);

        var tiles = new List<Widget>(Widget.Items.Count);
        for (int index = 0; index < Widget.Items.Count; index++)
        {
            int itemIndex = index;
            var states = new HashSet<WidgetState>();
            if (itemIndex == Widget.CurrentIndex)
            {
                states.Add(WidgetState.Selected);
            }

            // Dart's `WidgetStateProperty.resolveAs<MouseCursor?>`: in Dart a `WidgetStateMouseCursor`
            // *is* a `WidgetStateProperty`, so a stateful widget-level cursor resolves here too.
            MouseCursor? widgetCursor = Widget.MouseCursor is WidgetStateMouseCursor stateCursor
                ? stateCursor.Resolve(states)
                : Widget.MouseCursor;
            MouseCursor? effectiveMouseCursor = widgetCursor
                                                ?? bottomTheme.MouseCursor?.Resolve(states)
                                                ?? WidgetStateMouseCursor.Clickable.Resolve(states);

            tiles.Add(new BottomNavigationTile(
                key: Widget.Items[itemIndex].Key,
                type: type,
                item: Widget.Items[itemIndex],
                animation: _animations[itemIndex],
                iconSize: Widget.IconSize,
                selectedIconTheme: Widget.UseLegacyColorScheme
                    ? Widget.SelectedIconTheme ?? bottomTheme.SelectedIconTheme
                    : effectiveSelectedIconTheme,
                unselectedIconTheme: Widget.UseLegacyColorScheme
                    ? Widget.UnselectedIconTheme ?? bottomTheme.UnselectedIconTheme
                    : effectiveUnselectedIconTheme,
                selectedLabelStyle: effectiveSelectedLabelStyle,
                unselectedLabelStyle: effectiveUnselectedLabelStyle,
                enableFeedback: Widget.EnableFeedback ?? bottomTheme.EnableFeedback ?? true,
                onTap: () => Widget.OnTap?.Invoke(itemIndex),
                labelColorTween: Widget.UseLegacyColorScheme ? colorTween : labelColorTween,
                iconColorTween: Widget.UseLegacyColorScheme ? colorTween : iconColorTween,
                flex: EvaluateFlex(_animations[itemIndex]),
                selected: itemIndex == Widget.CurrentIndex,
                showSelectedLabels: showSelectedLabels,
                showUnselectedLabels: showUnselectedLabels,
                indexLabel: localizations.TabLabel(tabIndex: itemIndex + 1, tabCount: Widget.Items.Count),
                mouseCursor: effectiveMouseCursor,
                layout: layout));
        }

        return tiles;
    }

    private static TextStyle EffectiveTextStyle(TextStyle? textStyle, double fontSize)
    {
        textStyle ??= new TextStyle();

        // Prefer the font size on textStyle if present.
        return textStyle.FontSize is null ? textStyle.CopyWith(fontSize: fontSize) : textStyle;
    }

    private static IconThemeData EffectiveIconTheme(IconThemeData? iconTheme, Color? itemColor)
    {
        // Prefer the iconTheme over itemColor if present.
        return iconTheme ?? new IconThemeData(Color: itemColor);
    }
}

/// <summary>Dart's private `_Bar` — the bar's `Material` surface and its landscape alignment.</summary>
internal sealed class BottomNavigationBarSurface : StatelessWidget
{
    internal BottomNavigationBarSurface(
        Widget child,
        BottomNavigationBarLandscapeLayout layout,
        double elevation,
        Color? color = null,
        Key? key = null) : base(key)
    {
        Child = child;
        Layout = layout;
        Elevation = elevation;
        Color = color;
    }

    internal Widget Child { get; }

    internal BottomNavigationBarLandscapeLayout Layout { get; }

    internal double Elevation { get; }

    internal Color? Color { get; }

    public override Widget Build(BuildContext context)
    {
        Widget alignedChild = Child;
        if (MediaQuery.OrientationOf(context) == Orientation.Landscape
            && Layout == BottomNavigationBarLandscapeLayout.Centered)
        {
            alignedChild = new Align(
                alignment: Alignment.BottomCenter,
                heightFactor: 1,
                child: new SizedBox(width: MediaQuery.HeightOf(context), child: Child));
        }

        return new Material(elevation: Elevation, color: Color, child: alignedChild);
    }
}

/// <summary>
/// Dart's private `_Circle` — a splash of the newly selected item's background color, expanding
/// from that item's horizontal center.
/// </summary>
internal sealed class BottomNavigationBarCircle
{
    internal BottomNavigationBarCircle(
        BottomNavigationBarState state,
        int index,
        Color color,
        ITickerProvider vsync)
    {
        State = state;
        Index = index;
        Color = color;
        Controller = new AnimationController(
            duration: MaterialConstants.ThemeAnimationDuration,
            vsync: vsync);
        Animation = new CurvedAnimation(parent: Controller, curve: Curves.FastOutSlowIn);
        Controller.Forward();
    }

    internal BottomNavigationBarState State { get; }

    internal int Index { get; }

    internal Color Color { get; }

    internal AnimationController Controller { get; }

    internal CurvedAnimation Animation { get; }

    /// <summary>
    /// The fraction of the bar's width at which this circle's center sits, computed from the flex
    /// weights of the tiles that lead it.
    /// </summary>
    internal double HorizontalLeadingOffset
    {
        get
        {
            double WeightSum(IEnumerable<CurvedAnimation> animations) =>
                animations.Sum(State.EvaluateFlex);

            double allWeights = WeightSum(State.Animations);

            // These weights sum to the start edge of the indexed item.
            double leadingWeights = WeightSum(State.Animations.Take(Index));

            // Add half of its flex value in order to get to the center.
            return (leadingWeights + (State.EvaluateFlex(State.Animations[Index]) / 2.0)) / allWeights;
        }
    }

    internal void Dispose()
    {
        Controller.Dispose();
        Animation.Dispose();
    }
}

/// <summary>Dart's private `_RadialPainter` — paints the background color splashes.</summary>
internal sealed class BottomNavigationBarRadialPainter : CustomPainter
{
    internal BottomNavigationBarRadialPainter(
        IReadOnlyList<BottomNavigationBarCircle> circles,
        TextDirection textDirection)
    {
        Circles = circles;
        TextDirection = textDirection;
    }

    internal IReadOnlyList<BottomNavigationBarCircle> Circles { get; }

    internal TextDirection TextDirection { get; }

    // Computes the maximum radius attainable such that at least one of the bounding
    // rectangle's corners touches the edge of the circle. Drawing a circle larger than this radius
    // is not needed, since there is no perceivable difference within the cropped rectangle.
    private static double MaxRadius(Point center, Size size)
    {
        double maxX = Math.Max(center.X, size.Width - center.X);
        double maxY = Math.Max(center.Y, size.Height - center.Y);
        return Math.Sqrt((maxX * maxX) + (maxY * maxY));
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate)
    {
        if (oldDelegate is not BottomNavigationBarRadialPainter oldPainter)
        {
            return true;
        }

        if (TextDirection != oldPainter.TextDirection)
        {
            return true;
        }

        if (ReferenceEquals(Circles, oldPainter.Circles))
        {
            return false;
        }

        if (Circles.Count != oldPainter.Circles.Count)
        {
            return true;
        }

        for (int index = 0; index < Circles.Count; index++)
        {
            if (!ReferenceEquals(Circles[index], oldPainter.Circles[index]))
            {
                return true;
            }
        }

        return false;
    }

    public override void Paint(PaintingContext context, Size size)
    {
        foreach (BottomNavigationBarCircle circle in Circles)
        {
            var brush = new SolidColorBrush(circle.Color);
            var rect = new Rect(0.0, 0.0, size.Width, size.Height);
            double leftFraction = TextDirection switch
            {
                TextDirection.Rtl => 1.0 - circle.HorizontalLeadingOffset,
                _ => circle.HorizontalLeadingOffset,
            };
            var center = new Point(leftFraction * size.Width, size.Height / 2.0);
            double radius = new DoubleTween(begin: 0.0, end: MaxRadius(center, size))
                .Evaluate(circle.Animation.Value);
            context.PushClipRect(
                rect,
                clipped => clipped.DrawCircle(brush, null, center, radius));
        }
    }
}
