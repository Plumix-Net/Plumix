using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/navigation_rail.dart

public enum NavigationRailLabelType
{
    None,
    Selected,
    All,
}

public sealed class NavigationRailDestination
{
    public NavigationRailDestination(
        Widget icon,
        Widget label,
        Widget? selectedIcon = null,
        Color? indicatorColor = null,
        ShapeBorder? indicatorShape = null,
        Thickness? padding = null,
        bool disabled = false)
    {
        Icon = icon ?? throw new ArgumentNullException(nameof(icon));
        SelectedIcon = selectedIcon ?? icon;
        IndicatorColor = indicatorColor;
        IndicatorShape = indicatorShape;
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Padding = padding;
        Disabled = disabled;
    }

    public Widget Icon { get; }
    public Widget SelectedIcon { get; }
    public Color? IndicatorColor { get; }
    public ShapeBorder? IndicatorShape { get; }
    public Widget Label { get; }
    public Thickness? Padding { get; }
    public bool Disabled { get; }
}

public sealed class NavigationRail : StatefulWidget
{
    public NavigationRail(
        IReadOnlyList<NavigationRailDestination> destinations,
        int? selectedIndex,
        Color? backgroundColor = null,
        bool extended = false,
        Widget? leading = null,
        Widget? trailing = null,
        Action<int>? onDestinationSelected = null,
        double? elevation = null,
        double? groupAlignment = null,
        NavigationRailLabelType? labelType = null,
        TextStyle? unselectedLabelTextStyle = null,
        TextStyle? selectedLabelTextStyle = null,
        IconThemeData? unselectedIconTheme = null,
        IconThemeData? selectedIconTheme = null,
        double? minWidth = null,
        double? minExtendedWidth = null,
        bool? useIndicator = null,
        Color? indicatorColor = null,
        ShapeBorder? indicatorShape = null,
        bool leadingAtTop = true,
        bool trailingAtBottom = false,
        bool scrollable = false,
        MainAxisAlignment? mainAxisAlignment = null,
        Key? key = null) : base(key)
    {
        if (destinations is null) throw new ArgumentNullException(nameof(destinations));
        if (selectedIndex.HasValue && (selectedIndex.Value < 0 || selectedIndex.Value >= destinations.Count))
        {
            throw new ArgumentOutOfRangeException(nameof(selectedIndex));
        }

        if (elevation.HasValue && (!double.IsFinite(elevation.Value) || elevation.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(elevation), "Explicit elevation must be positive and finite.");
        }

        ValidatePositiveFinite(minWidth, nameof(minWidth));
        ValidatePositiveFinite(minExtendedWidth, nameof(minExtendedWidth));
        if (minWidth.HasValue && minExtendedWidth.HasValue && minExtendedWidth.Value < minWidth.Value)
        {
            throw new ArgumentException("Minimum extended width must be greater than or equal to minimum width.", nameof(minExtendedWidth));
        }

        if (groupAlignment.HasValue && (!double.IsFinite(groupAlignment.Value) || groupAlignment.Value < -1 || groupAlignment.Value > 1))
        {
            throw new ArgumentOutOfRangeException(nameof(groupAlignment), "Group alignment must be between -1 and 1.");
        }

        if (extended && labelType.HasValue && labelType.Value != NavigationRailLabelType.None)
        {
            throw new ArgumentException("An extended NavigationRail must use NavigationRailLabelType.None.", nameof(labelType));
        }

        Destinations = destinations;
        SelectedIndex = selectedIndex;
        BackgroundColor = backgroundColor;
        Extended = extended;
        Leading = leading;
        Trailing = trailing;
        OnDestinationSelected = onDestinationSelected;
        Elevation = elevation;
        GroupAlignment = groupAlignment;
        LabelType = labelType;
        UnselectedLabelTextStyle = unselectedLabelTextStyle;
        SelectedLabelTextStyle = selectedLabelTextStyle;
        UnselectedIconTheme = unselectedIconTheme;
        SelectedIconTheme = selectedIconTheme;
        MinWidth = minWidth;
        MinExtendedWidth = minExtendedWidth;
        UseIndicator = useIndicator;
        IndicatorColor = indicatorColor;
        IndicatorShape = indicatorShape;
        LeadingAtTop = leadingAtTop;
        TrailingAtBottom = trailingAtBottom;
        Scrollable = scrollable;
        MainAxisAlignment = mainAxisAlignment;
    }

    public Color? BackgroundColor { get; }
    public bool Extended { get; }
    public Widget? Leading { get; }
    public Widget? Trailing { get; }
    public IReadOnlyList<NavigationRailDestination> Destinations { get; }
    public int? SelectedIndex { get; }
    public Action<int>? OnDestinationSelected { get; }
    public double? Elevation { get; }
    public double? GroupAlignment { get; }
    public NavigationRailLabelType? LabelType { get; }
    public TextStyle? UnselectedLabelTextStyle { get; }
    public TextStyle? SelectedLabelTextStyle { get; }
    public IconThemeData? UnselectedIconTheme { get; }
    public IconThemeData? SelectedIconTheme { get; }
    public double? MinWidth { get; }
    public double? MinExtendedWidth { get; }
    public bool? UseIndicator { get; }
    public Color? IndicatorColor { get; }
    public ShapeBorder? IndicatorShape { get; }
    public bool LeadingAtTop { get; }
    public bool TrailingAtBottom { get; }
    public bool Scrollable { get; }
    public MainAxisAlignment? MainAxisAlignment { get; }

    public static double ExtendedAnimationValueOf(BuildContext context)
    {
        return context.DependOnInherited<NavigationRailExtendedAnimationScope>()?.Value
               ?? throw new InvalidOperationException("No NavigationRail ancestor was found.");
    }

    public override State CreateState() => new NavigationRailState();

    private static void ValidatePositiveFinite(double? value, string name)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(name, "Value must be positive and finite.");
        }
    }
}

internal sealed class NavigationRailState : State
{
    private static readonly TimeSpan ThemeAnimationDuration = TimeSpan.FromMilliseconds(200);
    private AnimationController? _extendedController;

    private NavigationRail CurrentWidget => (NavigationRail)StateWidget;

    public override void InitState()
    {
        _extendedController = new AnimationController(ThemeAnimationDuration)
        {
            Curve = Curves.EaseInOut
        };
        _extendedController.Changed += HandleExtendedChanged;
        if (CurrentWidget.Extended) _extendedController.Forward(1);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var old = (NavigationRail)oldWidget;
        if (old.Extended != CurrentWidget.Extended)
        {
            if (CurrentWidget.Extended) _extendedController!.Forward(); else _extendedController!.Reverse();
        }
    }

    public override void Dispose()
    {
        if (_extendedController is not null)
        {
            _extendedController.Changed -= HandleExtendedChanged;
            _extendedController.Dispose();
            _extendedController = null;
        }
    }

    public override Widget Build(BuildContext context)
    {
        var widget = CurrentWidget;
        var theme = Theme.Of(context);
        var railTheme = NavigationRailTheme.Of(context);
        var defaults = ResolveDefaults(theme);
        var backgroundColor = widget.BackgroundColor ?? railTheme.BackgroundColor ?? defaults.BackgroundColor!.Value;
        double elevation = widget.Elevation ?? railTheme.Elevation ?? defaults.Elevation!.Value;
        double minWidth = widget.MinWidth ?? railTheme.MinWidth ?? defaults.MinWidth!.Value;
        double minExtendedWidth = widget.MinExtendedWidth ?? railTheme.MinExtendedWidth ?? defaults.MinExtendedWidth!.Value;
        double groupAlignment = widget.GroupAlignment ?? railTheme.GroupAlignment ?? defaults.GroupAlignment!.Value;
        var labelType = widget.LabelType ?? railTheme.LabelType ?? defaults.LabelType!.Value;
        bool useIndicator = widget.UseIndicator ?? railTheme.UseIndicator ?? defaults.UseIndicator!.Value;
        var indicatorColor = widget.IndicatorColor ?? railTheme.IndicatorColor ?? defaults.IndicatorColor;
        var indicatorShape = widget.IndicatorShape ?? railTheme.IndicatorShape ?? defaults.IndicatorShape;
        var unselectedLabelStyle = widget.UnselectedLabelTextStyle ?? railTheme.UnselectedLabelTextStyle ?? defaults.UnselectedLabelTextStyle!;
        var selectedLabelStyle = widget.SelectedLabelTextStyle ?? railTheme.SelectedLabelTextStyle ?? defaults.SelectedLabelTextStyle!;
        IconThemeData unselectedIconTheme = widget.UnselectedIconTheme
                                            ?? railTheme.UnselectedIconTheme
                                            ?? defaults.UnselectedIconTheme!;
        if (!theme.UseMaterial3 && !unselectedIconTheme.Opacity.HasValue)
        {
            unselectedIconTheme = unselectedIconTheme.CopyWith(
                opacity: defaults.UnselectedIconTheme!.Opacity);
        }

        var selectedIconTheme = widget.SelectedIconTheme ?? railTheme.SelectedIconTheme ?? defaults.SelectedIconTheme!;
        double extendedProgress = _extendedController?.Evaluate() ?? (widget.Extended ? 1 : 0);
        double effectiveWidth = minWidth + ((minExtendedWidth - minWidth) * extendedProgress);

        var mainChildren = new List<Widget>();
        if (!widget.LeadingAtTop && widget.Leading is not null)
        {
            mainChildren.Add(widget.Leading);
            mainChildren.Add(new SizedBox(height: 8));
        }

        for (int index = 0; index < widget.Destinations.Count; index++)
        {
            var destination = widget.Destinations[index];
            bool selected = widget.SelectedIndex == index;
            int capturedIndex = index;
            mainChildren.Add(new NavigationRailDestinationTile(
                destination: destination,
                selected: selected,
                onTap: widget.OnDestinationSelected is null ? () => { } : () => widget.OnDestinationSelected(capturedIndex),
                indexLabel: MaterialLocalizations.Of(context).TabLabel(index, widget.Destinations.Count),
                minWidth: minWidth,
                extendedProgress: extendedProgress,
                labelType: labelType,
                iconTheme: selected ? selectedIconTheme : unselectedIconTheme,
                labelTextStyle: selected ? selectedLabelStyle : unselectedLabelStyle,
                useIndicator: useIndicator,
                indicatorColor: destination.IndicatorColor ?? indicatorColor,
                indicatorShape: destination.IndicatorShape ?? indicatorShape,
                useMaterial3: theme.UseMaterial3,
                key: new ValueKey<int>(index)));
        }

        if (!widget.TrailingAtBottom && widget.Trailing is not null)
        {
            mainChildren.Add(widget.Trailing);
        }

        Widget mainGroup = new Column(
            mainAxisSize: widget.MainAxisAlignment.HasValue ? MainAxisSize.Max : MainAxisSize.Min,
            mainAxisAlignment: widget.MainAxisAlignment ?? MainAxisAlignment.Start,
            children: mainChildren);
        if (widget.Scrollable)
        {
            mainGroup = new SingleChildScrollView(child: mainGroup);
        }

        var columnChildren = new List<Widget> { new SizedBox(height: 8) };
        if (widget.LeadingAtTop && widget.Leading is not null)
        {
            columnChildren.Add(widget.Leading);
            columnChildren.Add(new SizedBox(height: 8));
        }

        columnChildren.Add(new Flexible(
            child: new Align(
                alignment: new Alignment(0, groupAlignment),
                child: mainGroup)));
        if (widget.TrailingAtBottom && widget.Trailing is not null)
        {
            columnChildren.Add(widget.Trailing);
        }

        Widget content = new ConstrainedBox(
            constraints: new BoxConstraints(MinWidth: effectiveWidth),
            child: new Semantics(
                container: true,
                explicitChildNodes: true,
                child: new Column(children: columnChildren)));

        bool isRtl = Directionality.MaybeOf(context) == TextDirection.Rtl;
        content = new SafeArea(
            left: !isRtl,
            right: isRtl,
            child: content);

        content = new DecoratedBox(
            decoration: NavigationSurfaceUtilities.CreateDecoration(
                backgroundColor,
                elevation,
                theme.ShadowColor,
                Colors.Transparent,
                theme.UseMaterial3),
            child: content);

        return new NavigationRailExtendedAnimationScope(extendedProgress, content);
    }

    private static NavigationRailThemeData ResolveDefaults(ThemeData theme)
    {
        ColorScheme colors = theme.ColorScheme;
        if (!theme.UseMaterial3)
        {
            return new NavigationRailThemeData(
                BackgroundColor: colors.Surface,
                Elevation: 0,
                UnselectedLabelTextStyle: theme.TextTheme.BodyLarge.CopyWith(
                    color: NavigationSurfaceUtilities.WithOpacity(colors.OnSurface, 0.64)),
                SelectedLabelTextStyle: theme.TextTheme.BodyLarge.CopyWith(color: colors.Primary),
                UnselectedIconTheme: new IconThemeData(
                    Color: colors.OnSurface,
                    Size: 24,
                    Opacity: 0.64),
                SelectedIconTheme: new IconThemeData(
                    Color: colors.Primary,
                    Size: 24,
                    Opacity: 1.0),
                GroupAlignment: -1,
                LabelType: NavigationRailLabelType.None,
                UseIndicator: false,
                MinWidth: 72,
                MinExtendedWidth: 256);
        }

        return new NavigationRailThemeData(
            BackgroundColor: colors.Surface,
            Elevation: 0,
            UnselectedLabelTextStyle: theme.TextTheme.LabelMedium.CopyWith(color: colors.OnSurface),
            SelectedLabelTextStyle: theme.TextTheme.LabelMedium.CopyWith(color: colors.OnSurface),
            UnselectedIconTheme: new IconThemeData(Color: colors.OnSurfaceVariant, Size: 24),
            SelectedIconTheme: new IconThemeData(Color: colors.OnSecondaryContainer, Size: 24),
            GroupAlignment: -1,
            LabelType: NavigationRailLabelType.None,
            UseIndicator: true,
            IndicatorColor: colors.SecondaryContainer,
            IndicatorShape: ShapeBorder.Stadium(),
            MinWidth: 80,
            MinExtendedWidth: 256);
    }

    private void HandleExtendedChanged() => SetState(() => { });
}

internal sealed class NavigationRailDestinationTile : StatefulWidget
{
    public NavigationRailDestinationTile(
        NavigationRailDestination destination,
        bool selected,
        Action? onTap,
        string indexLabel,
        double minWidth,
        double extendedProgress,
        NavigationRailLabelType labelType,
        IconThemeData iconTheme,
        TextStyle labelTextStyle,
        bool useIndicator,
        Color? indicatorColor,
        ShapeBorder? indicatorShape,
        bool useMaterial3,
        Key? key = null) : base(key)
    {
        Destination = destination;
        Selected = selected;
        OnTap = onTap;
        IndexLabel = indexLabel;
        MinWidth = minWidth;
        ExtendedProgress = extendedProgress;
        LabelType = labelType;
        IconTheme = iconTheme;
        LabelTextStyle = labelTextStyle;
        UseIndicator = useIndicator;
        IndicatorColor = indicatorColor;
        IndicatorShape = indicatorShape;
        UseMaterial3 = useMaterial3;
    }

    public NavigationRailDestination Destination { get; }
    public bool Selected { get; }
    public Action? OnTap { get; }
    public string IndexLabel { get; }
    public double MinWidth { get; }
    public double ExtendedProgress { get; }
    public NavigationRailLabelType LabelType { get; }
    public IconThemeData IconTheme { get; }
    public TextStyle LabelTextStyle { get; }
    public bool UseIndicator { get; }
    public Color? IndicatorColor { get; }
    public ShapeBorder? IndicatorShape { get; }
    public bool UseMaterial3 { get; }

    public override State CreateState() => new NavigationRailDestinationTileState();
}

internal sealed class NavigationRailDestinationTileState : State
{
    private static readonly TimeSpan SelectionDuration = TimeSpan.FromMilliseconds(200);
    private AnimationController? _selectionController;
    private NavigationRailDestinationTile CurrentWidget => (NavigationRailDestinationTile)StateWidget;

    public override void InitState()
    {
        _selectionController = new AnimationController(SelectionDuration)
        {
            Curve = Curves.EaseInOut
        };
        _selectionController.Changed += HandleSelectionChanged;
        if (CurrentWidget.Selected) _selectionController.Forward(1);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        var old = (NavigationRailDestinationTile)oldWidget;
        if (old.Selected != CurrentWidget.Selected)
        {
            if (CurrentWidget.Selected) _selectionController!.Forward(); else _selectionController!.Reverse();
        }
    }

    public override void Dispose()
    {
        if (_selectionController is not null)
        {
            _selectionController.Changed -= HandleSelectionChanged;
            _selectionController.Dispose();
            _selectionController = null;
        }
    }

    public override Widget Build(BuildContext context)
    {
        var widget = CurrentWidget;
        var destination = widget.Destination;
        double selectionProgress = _selectionController?.Evaluate() ?? (widget.Selected ? 1 : 0);
        var icon = widget.Selected ? destination.SelectedIcon : destination.Icon;
        ColorScheme colors = Theme.Of(context).ColorScheme;
        Color disabledColor = NavigationSurfaceUtilities.WithOpacity(colors.OnSurface, 0.38);
        var iconTheme = destination.Disabled
            ? widget.IconTheme.CopyWith(color: disabledColor)
            : widget.IconTheme;
        var labelStyle = destination.Disabled
            ? widget.LabelTextStyle.CopyWith(color: disabledColor)
            : widget.LabelTextStyle;

        Widget iconPart = new Stack(
            alignment: Alignment.Center,
            children:
            [
                widget.UseIndicator
                    ? new NavigationIndicator(
                        animationValue: selectionProgress,
                        width: widget.UseMaterial3 ? 56 : 56,
                        height: widget.UseMaterial3 ? 32 : 56,
                        color: widget.IndicatorColor,
                        shape: widget.IndicatorShape)
                    : new SizedBox(),
                new IconTheme(iconTheme, icon)
            ]);

        Widget content;
        if (widget.ExtendedProgress > 0)
        {
            content = new Row(
                mainAxisSize: MainAxisSize.Min,
                spacing: 12 * widget.ExtendedProgress,
                children:
                [
                    new SizedBox(width: widget.MinWidth, child: new Center(iconPart)),
                    new Flexible(
                        child: new Opacity(
                            widget.ExtendedProgress,
                            new DefaultTextStyle(labelStyle, destination.Label)))
                ]);
            if (destination.Padding.HasValue)
            {
                content = new Padding(destination.Padding.Value, content);
            }
        }
        else
        {
            double labelOpacity = widget.LabelType switch
            {
                NavigationRailLabelType.All => 1,
                NavigationRailLabelType.Selected => selectionProgress,
                _ => 0
            };
            var children = new List<Widget> { iconPart };
            if (widget.LabelType != NavigationRailLabelType.None)
            {
                children.Add(new SizedBox(height: widget.UseMaterial3 ? 4 : 0));
                children.Add(new Opacity(
                    labelOpacity,
                    new DefaultTextStyle(labelStyle, destination.Label)));
            }

            content = new SizedBox(
                width: widget.MinWidth,
                child: new Padding(
                    insets: destination.Padding ?? new Thickness(8, widget.UseMaterial3 ? 6 : 16),
                    child: new Column(
                        mainAxisSize: MainAxisSize.Min,
                        children: children)));
        }

        Color primary = colors.Primary;
        bool primaryAlphaModified = primary.A < byte.MaxValue;
        Color splashColor = primaryAlphaModified
            ? primary
            : NavigationSurfaceUtilities.WithOpacity(primary, 0.12);
        Color hoverColor = primaryAlphaModified
            ? primary
            : NavigationSurfaceUtilities.WithOpacity(primary, 0.04);
        var style = new ButtonStyle(
            BackgroundColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
            OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Pressed)
                    ? splashColor
                    : states.HasFlag(MaterialState.Hovered)
                        ? hoverColor
                        : null),
            SplashColor: MaterialStateProperty<Color?>.All(splashColor),
            Padding: MaterialStateProperty<Thickness?>.All(default),
            Shape: MaterialStateProperty<BorderRadius?>.All(
                widget.IndicatorShape?.BorderRadius ?? Plumix.Rendering.BorderRadius.Circular(widget.MinWidth / 2)),
            MinimumSize: MaterialStateProperty<Size?>.All(new Size(widget.MinWidth, 0)),
            TapTargetSize: MaterialTapTargetSize.ShrinkWrap,
            Alignment: Alignment.Center);

        return new MergeSemantics(
            new MaterialButtonCore(
                child: content,
                onPressed: destination.Disabled ? null : widget.OnTap,
                style: style,
                isSelected: widget.Selected,
                isSemanticButton: true,
                semanticLabel: widget.IndexLabel,
                clipBehavior: Clip.None));
    }

    private void HandleSelectionChanged() => SetState(() => { });
}

internal sealed class NavigationRailExtendedAnimationScope : InheritedWidget
{
    public NavigationRailExtendedAnimationScope(double value, Widget child) : base()
    {
        Value = value;
        Child = child;
    }

    public double Value { get; }
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return Math.Abs(((NavigationRailExtendedAnimationScope)oldWidget).Value - Value) > double.Epsilon;
    }
}
