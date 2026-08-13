using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/tab_bar_theme.dart

/// <summary>
/// Defines a theme for <see cref="TabBar"/> widgets. Every member is nullable; a null member means
/// the <see cref="TabBar"/> falls through to its own defaults.
/// </summary>
public sealed partial record TabBarThemeData
{
    public TabBarThemeData(
        Decoration? Indicator = null,
        Color? IndicatorColor = null,
        TabBarIndicatorSize? IndicatorSize = null,
        Color? DividerColor = null,
        double? DividerHeight = null,
        Color? LabelColor = null,
        EdgeInsetsGeometry? LabelPadding = null,
        TextStyle? LabelStyle = null,
        Color? UnselectedLabelColor = null,
        TextStyle? UnselectedLabelStyle = null,
        MaterialStateProperty<Color?>? OverlayColor = null,
        InteractiveInkFeatureFactory? SplashFactory = null,
        MaterialStateProperty<MouseCursor?>? MouseCursor = null,
        TabAlignment? TabAlignment = null,
        TextScaler? TextScaler = null,
        TabIndicatorAnimation? IndicatorAnimation = null,
        BorderRadius? SplashBorderRadius = null)
    {
        this.Indicator = Indicator;
        this.IndicatorColor = IndicatorColor;
        this.IndicatorSize = IndicatorSize;
        this.DividerColor = DividerColor;
        this.DividerHeight = DividerHeight;
        this.LabelColor = LabelColor;
        this.LabelPadding = LabelPadding;
        this.LabelStyle = LabelStyle;
        this.UnselectedLabelColor = UnselectedLabelColor;
        this.UnselectedLabelStyle = UnselectedLabelStyle;
        this.OverlayColor = OverlayColor;
        this.SplashFactory = SplashFactory;
        this.MouseCursor = MouseCursor;
        this.TabAlignment = TabAlignment;
        this.TextScaler = TextScaler;
        this.IndicatorAnimation = IndicatorAnimation;
        this.SplashBorderRadius = SplashBorderRadius;
    }

    /// <summary>Overrides the default value for <see cref="TabBar.Indicator"/>.</summary>
    public Decoration? Indicator { get; init; }

    public Color? IndicatorColor { get; init; }

    public TabBarIndicatorSize? IndicatorSize { get; init; }

    public Color? DividerColor { get; init; }

    public double? DividerHeight { get; init; }

    /// <summary>
    /// Overrides the default value for <see cref="TabBar.LabelColor"/>. If this is a
    /// <see cref="WidgetStateColor"/> its unselected resolution wins even when
    /// <see cref="UnselectedLabelColor"/> is non-null.
    /// </summary>
    public Color? LabelColor { get; init; }

    public EdgeInsetsGeometry? LabelPadding { get; init; }

    public TextStyle? LabelStyle { get; init; }

    public Color? UnselectedLabelColor { get; init; }

    public TextStyle? UnselectedLabelStyle { get; init; }

    public MaterialStateProperty<Color?>? OverlayColor { get; init; }

    public InteractiveInkFeatureFactory? SplashFactory { get; init; }

    public MaterialStateProperty<MouseCursor?>? MouseCursor { get; init; }

    public TabAlignment? TabAlignment { get; init; }

    public TextScaler? TextScaler { get; init; }

    public TabIndicatorAnimation? IndicatorAnimation { get; init; }

    public BorderRadius? SplashBorderRadius { get; init; }

    public TabBarThemeData CopyWith(
        Decoration? indicator = null,
        Color? indicatorColor = null,
        TabBarIndicatorSize? indicatorSize = null,
        Color? dividerColor = null,
        double? dividerHeight = null,
        Color? labelColor = null,
        EdgeInsetsGeometry? labelPadding = null,
        TextStyle? labelStyle = null,
        Color? unselectedLabelColor = null,
        TextStyle? unselectedLabelStyle = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        InteractiveInkFeatureFactory? splashFactory = null,
        MaterialStateProperty<MouseCursor?>? mouseCursor = null,
        TabAlignment? tabAlignment = null,
        TextScaler? textScaler = null,
        TabIndicatorAnimation? indicatorAnimation = null,
        BorderRadius? splashBorderRadius = null)
    {
        return new TabBarThemeData(
            Indicator: indicator ?? Indicator,
            IndicatorColor: indicatorColor ?? IndicatorColor,
            IndicatorSize: indicatorSize ?? IndicatorSize,
            DividerColor: dividerColor ?? DividerColor,
            DividerHeight: dividerHeight ?? DividerHeight,
            LabelColor: labelColor ?? LabelColor,
            LabelPadding: labelPadding ?? LabelPadding,
            LabelStyle: labelStyle ?? LabelStyle,
            UnselectedLabelColor: unselectedLabelColor ?? UnselectedLabelColor,
            UnselectedLabelStyle: unselectedLabelStyle ?? UnselectedLabelStyle,
            OverlayColor: overlayColor ?? OverlayColor,
            SplashFactory: splashFactory ?? SplashFactory,
            MouseCursor: mouseCursor ?? MouseCursor,
            TabAlignment: tabAlignment ?? TabAlignment,
            TextScaler: textScaler ?? TextScaler,
            IndicatorAnimation: indicatorAnimation ?? IndicatorAnimation,
            SplashBorderRadius: splashBorderRadius ?? SplashBorderRadius);
    }
}

/// <summary>
/// Applies a <see cref="TabBarThemeData"/> to descendant <see cref="TabBar"/> widgets.
/// </summary>
public sealed class TabBarTheme : InheritedTheme
{
    private readonly TabBarThemeData? _data;
    private readonly Decoration? _indicator;
    private readonly Color? _indicatorColor;
    private readonly TabBarIndicatorSize? _indicatorSize;
    private readonly Color? _dividerColor;
    private readonly double? _dividerHeight;
    private readonly Color? _labelColor;
    private readonly EdgeInsetsGeometry? _labelPadding;
    private readonly TextStyle? _labelStyle;
    private readonly Color? _unselectedLabelColor;
    private readonly TextStyle? _unselectedLabelStyle;
    private readonly MaterialStateProperty<Color?>? _overlayColor;
    private readonly InteractiveInkFeatureFactory? _splashFactory;
    private readonly MaterialStateProperty<MouseCursor?>? _mouseCursor;
    private readonly TabAlignment? _tabAlignment;
    private readonly TextScaler? _textScaler;
    private readonly TabIndicatorAnimation? _indicatorAnimation;

    public TabBarTheme(
        TabBarThemeData? data = null,
        Widget? child = null,
        Decoration? indicator = null,
        Color? indicatorColor = null,
        TabBarIndicatorSize? indicatorSize = null,
        Color? dividerColor = null,
        double? dividerHeight = null,
        Color? labelColor = null,
        EdgeInsetsGeometry? labelPadding = null,
        TextStyle? labelStyle = null,
        Color? unselectedLabelColor = null,
        TextStyle? unselectedLabelStyle = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        InteractiveInkFeatureFactory? splashFactory = null,
        MaterialStateProperty<MouseCursor?>? mouseCursor = null,
        TabAlignment? tabAlignment = null,
        TextScaler? textScaler = null,
        TabIndicatorAnimation? indicatorAnimation = null,
        Key? key = null) : base(key)
    {
        bool hasIndividualProperty = indicator is not null
                                     || indicatorColor is not null
                                     || indicatorSize is not null
                                     || dividerColor is not null
                                     || dividerHeight is not null
                                     || labelColor is not null
                                     || labelPadding is not null
                                     || labelStyle is not null
                                     || unselectedLabelColor is not null
                                     || unselectedLabelStyle is not null
                                     || overlayColor is not null
                                     || splashFactory is not null
                                     || mouseCursor is not null
                                     || tabAlignment is not null
                                     || textScaler is not null
                                     || indicatorAnimation is not null;
        if (data is not null && hasIndividualProperty)
        {
            throw new ArgumentException(
                "TabBarTheme accepts either a data object or individual properties, not both.",
                nameof(data));
        }

        _data = data;
        _indicator = indicator;
        _indicatorColor = indicatorColor;
        _indicatorSize = indicatorSize;
        _dividerColor = dividerColor;
        _dividerHeight = dividerHeight;
        _labelColor = labelColor;
        _labelPadding = labelPadding;
        _labelStyle = labelStyle;
        _unselectedLabelColor = unselectedLabelColor;
        _unselectedLabelStyle = unselectedLabelStyle;
        _overlayColor = overlayColor;
        _splashFactory = splashFactory;
        _mouseCursor = mouseCursor;
        _tabAlignment = tabAlignment;
        _textScaler = textScaler;
        _indicatorAnimation = indicatorAnimation;
        Child = child ?? new SizedBox();
    }

    public Widget Child { get; }

    public Decoration? Indicator => _data is not null ? _data.Indicator : _indicator;

    public Color? IndicatorColor => _data is not null ? _data.IndicatorColor : _indicatorColor;

    public TabBarIndicatorSize? IndicatorSize => _data is not null ? _data.IndicatorSize : _indicatorSize;

    public Color? DividerColor => _data is not null ? _data.DividerColor : _dividerColor;

    public double? DividerHeight => _data is not null ? _data.DividerHeight : _dividerHeight;

    public Color? LabelColor => _data is not null ? _data.LabelColor : _labelColor;

    public EdgeInsetsGeometry? LabelPadding => _data is not null ? _data.LabelPadding : _labelPadding;

    public TextStyle? LabelStyle => _data is not null ? _data.LabelStyle : _labelStyle;

    public Color? UnselectedLabelColor =>
        _data is not null ? _data.UnselectedLabelColor : _unselectedLabelColor;

    public TextStyle? UnselectedLabelStyle =>
        _data is not null ? _data.UnselectedLabelStyle : _unselectedLabelStyle;

    public MaterialStateProperty<Color?>? OverlayColor =>
        _data is not null ? _data.OverlayColor : _overlayColor;

    public InteractiveInkFeatureFactory? SplashFactory =>
        _data is not null ? _data.SplashFactory : _splashFactory;

    public MaterialStateProperty<MouseCursor?>? MouseCursor =>
        _data is not null ? _data.MouseCursor : _mouseCursor;

    public TabAlignment? TabAlignment => _data is not null ? _data.TabAlignment : _tabAlignment;

    public TextScaler? TextScaler => _data is not null ? _data.TextScaler : _textScaler;

    public TabIndicatorAnimation? IndicatorAnimation =>
        _data is not null ? _data.IndicatorAnimation : _indicatorAnimation;

    /// <summary>The properties of this theme, whether supplied directly or as individual fields.</summary>
    public TabBarThemeData Data => _data ?? new TabBarThemeData(
        Indicator: _indicator,
        IndicatorColor: _indicatorColor,
        IndicatorSize: _indicatorSize,
        DividerColor: _dividerColor,
        DividerHeight: _dividerHeight,
        LabelColor: _labelColor,
        LabelPadding: _labelPadding,
        LabelStyle: _labelStyle,
        UnselectedLabelColor: _unselectedLabelColor,
        UnselectedLabelStyle: _unselectedLabelStyle,
        OverlayColor: _overlayColor,
        SplashFactory: _splashFactory,
        MouseCursor: _mouseCursor,
        TabAlignment: _tabAlignment,
        TextScaler: _textScaler,
        IndicatorAnimation: _indicatorAnimation);

    public override Widget Build(BuildContext context) => Child;

    public override Widget Wrap(BuildContext context, Widget child) => new TabBarTheme(Data, child);

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(((TabBarTheme)oldWidget).Data, Data);

    /// <summary>
    /// Returns the nearest ancestor <see cref="TabBarTheme"/>'s data, falling back to
    /// <see cref="ThemeData.TabBarTheme"/>.
    /// </summary>
    public static TabBarThemeData Of(BuildContext context) =>
        context.DependOnInherited<TabBarTheme>()?.Data ?? Theme.Of(context).TabBarTheme;

    /// <summary>Dart parity: <c>TabBarTheme.copyWith</c>, which drops the key, child and data.</summary>
    public TabBarTheme CopyWith(
        Decoration? indicator = null,
        Color? indicatorColor = null,
        TabBarIndicatorSize? indicatorSize = null,
        Color? dividerColor = null,
        double? dividerHeight = null,
        Color? labelColor = null,
        EdgeInsetsGeometry? labelPadding = null,
        TextStyle? labelStyle = null,
        Color? unselectedLabelColor = null,
        TextStyle? unselectedLabelStyle = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        InteractiveInkFeatureFactory? splashFactory = null,
        MaterialStateProperty<MouseCursor?>? mouseCursor = null,
        TabAlignment? tabAlignment = null,
        TextScaler? textScaler = null,
        TabIndicatorAnimation? indicatorAnimation = null)
    {
        return new TabBarTheme(
            indicator: indicator ?? Indicator,
            indicatorColor: indicatorColor ?? IndicatorColor,
            indicatorSize: indicatorSize ?? IndicatorSize,
            dividerColor: dividerColor ?? DividerColor,
            dividerHeight: dividerHeight ?? DividerHeight,
            labelColor: labelColor ?? LabelColor,
            labelPadding: labelPadding ?? LabelPadding,
            labelStyle: labelStyle ?? LabelStyle,
            unselectedLabelColor: unselectedLabelColor ?? UnselectedLabelColor,
            unselectedLabelStyle: unselectedLabelStyle ?? UnselectedLabelStyle,
            overlayColor: overlayColor ?? OverlayColor,
            splashFactory: splashFactory ?? SplashFactory,
            mouseCursor: mouseCursor ?? MouseCursor,
            tabAlignment: tabAlignment ?? TabAlignment,
            textScaler: textScaler ?? TextScaler,
            indicatorAnimation: indicatorAnimation ?? IndicatorAnimation);
    }
}
