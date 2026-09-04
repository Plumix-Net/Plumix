using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/tabs.dart

/// <summary>Defines how the bounds of the selected tab indicator are computed.</summary>
public enum TabBarIndicatorSize
{
    /// <summary>The tab indicator's bounds are as wide as the space occupied by the tab in the tab bar.</summary>
    Tab,

    /// <summary>The tab's bounds are only as wide as the (centered) tab widget itself.</summary>
    Label,
}

/// <summary>Defines how tabs are aligned horizontally in a <see cref="TabBar"/>.</summary>
public enum TabAlignment
{
    /// <summary>Tabs are aligned to the start of the tab bar. Only valid when scrollable.</summary>
    Start,

    /// <summary>Start-aligned with a 52 logical-pixel offset. Only valid when scrollable.</summary>
    StartOffset,

    /// <summary>Tabs are stretched to fill the tab bar. Only valid when not scrollable.</summary>
    Fill,

    /// <summary>Tabs are centered in the tab bar.</summary>
    Center,
}

/// <summary>Defines how the tab indicator animates when the selected tab changes.</summary>
public enum TabIndicatorAnimation
{
    /// <summary>The indicator moves at a constant rate.</summary>
    Linear,

    /// <summary>The indicator stretches toward the destination tab and settles onto it.</summary>
    Elastic,
}

/// <summary>Signature for <see cref="TabBar.OnHover"/> and <see cref="TabBar.OnFocusChange"/>.</summary>
public delegate void TabValueChanged<in T>(T value, int index);

/// <summary>
/// A Material Design <see cref="TabBar"/> tab: an optional icon and an optional label.
/// </summary>
public sealed class Tab : StatelessWidget, IPreferredSizeWidget
{
    internal const double TabHeight = 46.0;
    internal const double TextAndIconTabHeight = 72.0;

    public Tab(
        string? text = null,
        Widget? icon = null,
        EdgeInsetsGeometry? iconMargin = null,
        double? height = null,
        Widget? child = null,
        Key? key = null) : base(key)
    {
        if (text is null && child is null && icon is null)
        {
            throw new ArgumentException(
                "Tab requires at least one of text, child, or icon to be non-null.");
        }

        if (text is not null && child is not null)
        {
            throw new ArgumentException(
                "Provide either text or child, not both, when creating a Tab.");
        }

        if (height.HasValue && (!double.IsFinite(height.Value) || height.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        Text = text;
        Icon = icon;
        IconMargin = iconMargin;
        Height = height;
        Child = child;
    }

    public string? Text { get; }

    public Widget? Child { get; }

    public Widget? Icon { get; }

    /// <summary>The margin added around the tab's icon; only used when both an icon and a label exist.</summary>
    public EdgeInsetsGeometry? IconMargin { get; }

    public double? Height { get; }

    public Size PreferredSize
    {
        get
        {
            if (Height is { } height)
            {
                return new Size(0, height);
            }

            return (Text is not null || Child is not null) && Icon is not null
                ? new Size(0, TextAndIconTabHeight)
                : new Size(0, TabHeight);
        }
    }

    public override Widget Build(BuildContext context)
    {
        double calculatedHeight;
        Widget label;
        if (Icon is null)
        {
            calculatedHeight = TabHeight;
            label = BuildLabelText();
        }
        else if (Text is null && Child is null)
        {
            calculatedHeight = TabHeight;
            label = Icon;
        }
        else
        {
            calculatedHeight = TextAndIconTabHeight;
            EdgeInsetsGeometry effectiveIconMargin = IconMargin
                ?? (Theme.Of(context).UseMaterial3
                    ? TabsPrimaryDefaultsM3.IconMargin
                    : TabsDefaultsM2.IconMargin);
            label = new Column(
                mainAxisAlignment: MainAxisAlignment.Center,
                children:
                [
                    new Padding(effectiveIconMargin, Icon),
                    BuildLabelText(),
                ]);
        }

        return new SizedBox(
            height: Height ?? calculatedHeight,
            child: new Center(widthFactor: 1.0, child: label));
    }

    private Widget BuildLabelText() =>
        Child ?? new Text(Text!, softWrap: false, overflow: TextOverflow.Fade);
}

// Dart parity source: material_ui/lib/src/tabs.dart (_TabStyle)
internal sealed class TabStyle : AnimatedWidget
{
    public TabStyle(
        Animation<double> animation,
        bool isSelected,
        bool isPrimary,
        WidgetStateColor? labelColor,
        Color? unselectedLabelColor,
        TextStyle? labelStyle,
        TextStyle? unselectedLabelStyle,
        TabBarDefaults defaults,
        Widget child) : base(animation)
    {
        Animation = animation;
        IsSelected = isSelected;
        IsPrimary = isPrimary;
        LabelColor = labelColor;
        UnselectedLabelColor = unselectedLabelColor;
        LabelStyle = labelStyle;
        UnselectedLabelStyle = unselectedLabelStyle;
        Defaults = defaults;
        Child = child;
    }

    public Animation<double> Animation { get; }

    public bool IsSelected { get; }

    public bool IsPrimary { get; }

    public WidgetStateColor? LabelColor { get; }

    public Color? UnselectedLabelColor { get; }

    public TextStyle? LabelStyle { get; }

    public TextStyle? UnselectedLabelStyle { get; }

    public TabBarDefaults Defaults { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        var themeData = Theme.Of(context);
        TabBarThemeData tabBarTheme = TabBarTheme.Of(context);
        var states = new HashSet<WidgetState>();
        if (IsSelected)
        {
            _ = states.Add(WidgetState.Selected);
        }

        TextStyle selectedStyle = Defaults.LabelStyle!
            .Merge(LabelStyle ?? tabBarTheme.LabelStyle)
            .CopyWith(inherit: true);
        TextStyle unselectedStyle = Defaults.UnselectedLabelStyle!
            .Merge(UnselectedLabelStyle ?? tabBarTheme.UnselectedLabelStyle ?? LabelStyle)
            .CopyWith(inherit: true);
        TextStyle textStyle = IsSelected
            ? TextStyle.Lerp(selectedStyle, unselectedStyle, Animation.Value)
            : TextStyle.Lerp(unselectedStyle, selectedStyle, Animation.Value);

        Color defaultIconColor = themeData.ColorScheme.Brightness == Brightness.Light
            ? DefaultIconDarkColor
            : DefaultIconLightColor;
        IconThemeData ambientIconTheme = IconTheme.Of(context);
        IconThemeData? customIconTheme = ambientIconTheme.Color != defaultIconColor
            ? ambientIconTheme
            : null;
        Color iconColor = ResolveWithLabelColor(context, tabBarTheme, themeData, customIconTheme)
            .Resolve(states);
        Color labelColor = ResolveWithLabelColor(context, tabBarTheme, themeData, iconTheme: null)
            .Resolve(states);

        return new DefaultTextStyle(
            style: textStyle.CopyWith(color: labelColor),
            child: IconTheme.Merge(
                data: new IconThemeData(Color: iconColor, Size: customIconTheme?.Size ?? 24.0),
                child: Child));
    }

    // Mirrors Flutter's `kDefaultIconLightColor`/`kDefaultIconDarkColor` from `material/constants.dart`.
    internal static readonly Color DefaultIconLightColor = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);

    internal static readonly Color DefaultIconDarkColor = Color.FromArgb(0xDD, 0x00, 0x00, 0x00);

    private WidgetStateColor ResolveWithLabelColor(
        BuildContext context,
        TabBarThemeData tabBarTheme,
        ThemeData themeData,
        IconThemeData? iconTheme)
    {
        // labelStyle.color and unselectedLabelStyle.color are ignored when labelColor or
        // unselectedLabelColor are set, matching Dart's chain order.
        WidgetStateColor selectedColor = LabelColor
                                         ?? tabBarTheme.LabelColor
                                         ?? LabelStyle?.Color
                                         ?? tabBarTheme.LabelStyle?.Color
                                         ?? Defaults.LabelColor!;

        Color unselectedColor;
        if (!selectedColor.IsConstantColor)
        {
            // A state-resolving labelColor takes precedence over unselectedLabelColor.
            unselectedColor = selectedColor.Resolve(new HashSet<WidgetState>());
        }
        else
        {
            unselectedColor = UnselectedLabelColor
                              ?? tabBarTheme.UnselectedLabelColor
                              ?? UnselectedLabelStyle?.Color
                              ?? tabBarTheme.UnselectedLabelStyle?.Color
                              ?? iconTheme?.Color
                              ?? (themeData.UseMaterial3
                                  ? Defaults.UnselectedLabelColor!.Value
                                  : WithAlpha(selectedColor.DefaultValue, 0xB2));
        }

        Color selected = selectedColor.IsConstantColor
            ? selectedColor.DefaultValue
            : selectedColor.Resolve(new HashSet<WidgetState> { WidgetState.Selected });
        return WidgetStateColor.ResolveWith(states => states.Contains(WidgetState.Selected)
            ? LerpColor(selected, unselectedColor, Animation.Value)
            : LerpColor(unselectedColor, selected, Animation.Value));
    }

    internal static Color WithAlpha(Color color, int alpha) =>
        Color.FromArgb((byte)alpha, color.R, color.G, color.B);

    internal static Color WithOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Round(255 * Math.Clamp(opacity, 0.0, 1.0)),
        color.R,
        color.G,
        color.B);

    internal static Color LerpColor(Color a, Color b, double t) => Color.FromArgb(
        (byte)Math.Clamp(Math.Round(a.A + ((b.A - a.A) * t)), 0, 255),
        (byte)Math.Clamp(Math.Round(a.R + ((b.R - a.R) * t)), 0, 255),
        (byte)Math.Clamp(Math.Round(a.G + ((b.G - a.G) * t)), 0, 255),
        (byte)Math.Clamp(Math.Round(a.B + ((b.B - a.B) * t)), 0, 255));
}

/// <summary>
/// Dart parity: <c>_TabsDefaultsM2</c> / <c>_TabsPrimaryDefaultsM3</c> / <c>_TabsSecondaryDefaultsM3</c>.
/// Dart lets those classes extend <c>TabBarThemeData</c>; C# keeps a separate contract because
/// <see cref="TabBarThemeData"/> is a sealed record.
/// </summary>
internal abstract class TabBarDefaults
{
    public abstract TabBarIndicatorSize IndicatorSize { get; }

    public abstract TabAlignment TabAlignment { get; }

    public virtual Color? IndicatorColor => null;

    public virtual Color? DividerColor => null;

    public virtual double? DividerHeight => null;

    public virtual WidgetStateColor? LabelColor => null;

    public virtual TextStyle? LabelStyle => null;

    public virtual Color? UnselectedLabelColor => null;

    public virtual TextStyle? UnselectedLabelStyle => null;

    public virtual InteractiveInkFeatureFactory? SplashFactory => null;

    public virtual MaterialStateProperty<Color?>? OverlayColor => null;

    public virtual BorderRadius? SplashBorderRadius => null;
}

// Dart parity source: material_ui/lib/src/tabs.dart (_TabsDefaultsM2)
internal sealed class TabsDefaultsM2 : TabBarDefaults
{
    /// <summary>Dart's <c>Colors.blue</c>, the Material 2 light primary color.</summary>
    private static readonly Color MaterialBlue = Color.FromArgb(0xFF, 0x21, 0x96, 0xF3);

    /// <summary>Dart's <c>Colors.grey[900]</c>, the Material 2 dark primary color.</summary>
    private static readonly Color MaterialGrey900 = Color.FromArgb(0xFF, 0x21, 0x21, 0x21);

    internal static readonly EdgeInsetsGeometry IconMargin = EdgeInsetsGeometry.Only(bottom: 10);

    private readonly ThemeData _theme;
    private readonly bool _isScrollable;

    public TabsDefaultsM2(BuildContext context, bool isScrollable)
    {
        _theme = Theme.Of(context);
        _isScrollable = isScrollable;
    }

    public override TabBarIndicatorSize IndicatorSize => TabBarIndicatorSize.Tab;

    public override TabAlignment TabAlignment =>
        _isScrollable ? global::Plumix.Material.TabAlignment.Start : global::Plumix.Material.TabAlignment.Fill;

    public override Color? IndicatorColor
    {
        get
        {
            Color primaryColor = _theme.Brightness == Brightness.Dark ? MaterialGrey900 : MaterialBlue;
            return _theme.ColorScheme.Secondary == primaryColor ? Colors.White : _theme.ColorScheme.Secondary;
        }
    }

    public override WidgetStateColor? LabelColor => _theme.PrimaryTextTheme.BodyLarge.Color
                                                    ?? _theme.ColorScheme.OnPrimary;

    public override TextStyle? LabelStyle => _theme.PrimaryTextTheme.BodyLarge;

    public override TextStyle? UnselectedLabelStyle => _theme.PrimaryTextTheme.BodyLarge;

    public override InteractiveInkFeatureFactory? SplashFactory => _theme.SplashFactory;
}

// Dart parity source: material_ui/lib/src/tabs.dart (_TabsPrimaryDefaultsM3)
internal sealed class TabsPrimaryDefaultsM3 : TabBarDefaults
{
    internal static readonly EdgeInsetsGeometry IconMargin = EdgeInsetsGeometry.Only(bottom: 2);

    private readonly ThemeData _theme;
    private readonly bool _isScrollable;

    public TabsPrimaryDefaultsM3(BuildContext context, bool isScrollable)
    {
        _theme = Theme.Of(context);
        _isScrollable = isScrollable;
    }

    /// <summary>Dart's <c>_TabsPrimaryDefaultsM3.indicatorWeight</c>.</summary>
    internal static double IndicatorWeightFor(TabBarIndicatorSize indicatorSize) =>
        indicatorSize == TabBarIndicatorSize.Label ? 3.0 : 2.0;

    public override TabBarIndicatorSize IndicatorSize => TabBarIndicatorSize.Label;

    public override TabAlignment TabAlignment =>
        _isScrollable ? global::Plumix.Material.TabAlignment.StartOffset : global::Plumix.Material.TabAlignment.Fill;

    public override Color? DividerColor => _theme.ColorScheme.OutlineVariant;

    public override double? DividerHeight => 1.0;

    public override Color? IndicatorColor => _theme.ColorScheme.Primary;

    public override WidgetStateColor? LabelColor => _theme.ColorScheme.Primary;

    public override TextStyle? LabelStyle => _theme.TextTheme.TitleSmall;

    public override Color? UnselectedLabelColor => _theme.ColorScheme.OnSurfaceVariant;

    public override TextStyle? UnselectedLabelStyle => _theme.TextTheme.TitleSmall;

    public override InteractiveInkFeatureFactory? SplashFactory => _theme.SplashFactory;

    public override MaterialStateProperty<Color?>? OverlayColor =>
        MaterialStateProperty<Color?>.ResolveWith(states =>
        {
            ColorScheme colors = _theme.ColorScheme;
            if (states.HasFlag(MaterialState.Selected))
            {
                if (states.HasFlag(MaterialState.Pressed))
                {
                    return TabStyle.WithOpacity(colors.Primary, 0.1);
                }

                if (states.HasFlag(MaterialState.Hovered))
                {
                    return TabStyle.WithOpacity(colors.Primary, 0.08);
                }

                return states.HasFlag(MaterialState.Focused)
                    ? TabStyle.WithOpacity(colors.Primary, 0.1)
                    : null;
            }

            if (states.HasFlag(MaterialState.Pressed))
            {
                return TabStyle.WithOpacity(colors.Primary, 0.1);
            }

            if (states.HasFlag(MaterialState.Hovered))
            {
                return TabStyle.WithOpacity(colors.OnSurface, 0.08);
            }

            return states.HasFlag(MaterialState.Focused)
                ? TabStyle.WithOpacity(colors.OnSurface, 0.1)
                : null;
        });
}

// Dart parity source: material_ui/lib/src/tabs.dart (_TabsSecondaryDefaultsM3)
internal sealed class TabsSecondaryDefaultsM3 : TabBarDefaults
{
    /// <summary>Dart's <c>_TabsSecondaryDefaultsM3.indicatorWeight</c>.</summary>
    internal const double IndicatorWeight = 2.0;

    private readonly ThemeData _theme;
    private readonly bool _isScrollable;

    public TabsSecondaryDefaultsM3(BuildContext context, bool isScrollable)
    {
        _theme = Theme.Of(context);
        _isScrollable = isScrollable;
    }

    public override TabBarIndicatorSize IndicatorSize => TabBarIndicatorSize.Tab;

    public override TabAlignment TabAlignment =>
        _isScrollable ? global::Plumix.Material.TabAlignment.StartOffset : global::Plumix.Material.TabAlignment.Fill;

    public override Color? DividerColor => _theme.ColorScheme.OutlineVariant;

    public override double? DividerHeight => 1.0;

    public override Color? IndicatorColor => _theme.ColorScheme.Primary;

    public override WidgetStateColor? LabelColor => _theme.ColorScheme.OnSurface;

    public override TextStyle? LabelStyle => _theme.TextTheme.TitleSmall;

    public override Color? UnselectedLabelColor => _theme.ColorScheme.OnSurfaceVariant;

    public override TextStyle? UnselectedLabelStyle => _theme.TextTheme.TitleSmall;

    public override InteractiveInkFeatureFactory? SplashFactory => _theme.SplashFactory;

    public override MaterialStateProperty<Color?>? OverlayColor =>
        MaterialStateProperty<Color?>.ResolveWith(states =>
        {
            Color onSurface = _theme.ColorScheme.OnSurface;
            if (states.HasFlag(MaterialState.Pressed))
            {
                return TabStyle.WithOpacity(onSurface, 0.1);
            }

            if (states.HasFlag(MaterialState.Hovered))
            {
                return TabStyle.WithOpacity(onSurface, 0.08);
            }

            return states.HasFlag(MaterialState.Focused)
                ? TabStyle.WithOpacity(onSurface, 0.1)
                : null;
        });
}

/// <summary>Reports the laid-out leading edges of every tab plus the trailing edge of the last one.</summary>
internal delegate void TabLayoutCallback(IReadOnlyList<double> xOffsets, TextDirection textDirection, double width);

// Dart parity source: material_ui/lib/src/tabs.dart (_TabLabelBarRenderer)
internal sealed class TabLabelBarRenderer : RenderFlex
{
    public TabLabelBarRenderer(
        Axis direction,
        MainAxisSize mainAxisSize,
        MainAxisAlignment mainAxisAlignment,
        CrossAxisAlignment crossAxisAlignment,
        TextDirection textDirection,
        VerticalDirection verticalDirection,
        TabLayoutCallback onPerformLayout) : base(
        children: null,
        direction: direction,
        mainAxisSize: mainAxisSize,
        mainAxisAlignment: mainAxisAlignment,
        crossAxisAlignment: crossAxisAlignment,
        textDirection: textDirection,
        verticalDirection: verticalDirection)
    {
        OnPerformLayout = onPerformLayout;
    }

    public TabLayoutCallback OnPerformLayout { get; set; }

    protected override void PerformLayout()
    {
        base.PerformLayout();
        // xOffsets[i] is the leading edge of tab i; the last entry is the trailing edge of the bar.
        var xOffsets = new List<double>();
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            xOffsets.Add(((FlexParentData)child.parentData!).offset.X);
        }

        TextDirection direction = TextDirection ?? Plumix.UI.TextDirection.Ltr;
        if (direction == Plumix.UI.TextDirection.Rtl)
        {
            xOffsets.Insert(0, Size.Width);
        }
        else
        {
            xOffsets.Add(Size.Width);
        }

        OnPerformLayout(xOffsets, direction, Size.Width);
    }
}

// Dart parity source: material_ui/lib/src/tabs.dart (_TabLabelBar)
internal sealed class TabLabelBar : Flex
{
    public TabLabelBar(
        IReadOnlyList<Widget> children,
        TabLayoutCallback onPerformLayout,
        MainAxisSize mainAxisSize) : base(
        direction: Axis.Horizontal,
        children: children,
        mainAxisSize: mainAxisSize,
        mainAxisAlignment: MainAxisAlignment.Start,
        crossAxisAlignment: CrossAxisAlignment.Center,
        verticalDirection: VerticalDirection.Down)
    {
        OnPerformLayout = onPerformLayout;
    }

    public TabLayoutCallback OnPerformLayout { get; }

    public override RenderObject CreateRenderObject(BuildContext context) => new TabLabelBarRenderer(
        direction: Direction,
        mainAxisSize: MainAxisSize,
        mainAxisAlignment: MainAxisAlignment,
        crossAxisAlignment: CrossAxisAlignment,
        textDirection: Directionality.Of(context),
        verticalDirection: VerticalDirection,
        onPerformLayout: OnPerformLayout);

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        base.UpdateRenderObject(context, renderObject);
        ((TabLabelBarRenderer)renderObject).OnPerformLayout = OnPerformLayout;
    }
}

// Dart parity source: material_ui/lib/src/tabs.dart (_DividerPainter)
internal sealed class DividerPainter : CustomPainter
{
    public DividerPainter(Color dividerColor, double dividerHeight)
    {
        DividerColor = dividerColor;
        DividerHeight = dividerHeight;
    }

    public Color DividerColor { get; }

    public double DividerHeight { get; }

    public override void Paint(PaintingContext context, Size size)
    {
        if (DividerHeight <= 0.0)
        {
            return;
        }

        double y = size.Height - (DividerHeight / 2);
        context.Canvas.DrawLine(
            new Pen(new SolidColorBrush(DividerColor), DividerHeight),
            new Point(0, y),
            new Point(size.Width, y));
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate)
    {
        var old = (DividerPainter)oldDelegate;
        return old.DividerColor != DividerColor || old.DividerHeight != DividerHeight;
    }
}

/// <summary>Dart parity: the private <c>ChangeNotifier</c> that lets an indicator repaint itself.</summary>
internal sealed class IndicatorPainterNotifier : ChangeNotifier
{
    public void Notify() => NotifyListeners();
}

// Dart parity source: material_ui/lib/src/tabs.dart (_IndicatorPainter)
internal sealed class IndicatorPainter : CustomPainter
{
    private readonly IndicatorPainterNotifier _repaint;
    private IReadOnlyList<double>? _currentTabOffsets;
    private TextDirection? _currentTextDirection;
    private BoxPainter? _painter;
    private bool _needsPaint;

    private IndicatorPainter(
        TabController controller,
        Decoration indicator,
        TabBarIndicatorSize indicatorSize,
        EdgeInsetsGeometry indicatorPadding,
        IReadOnlyList<GlobalKey> tabKeys,
        IReadOnlyList<EdgeInsetsGeometry> labelPaddings,
        IndicatorPainter? old,
        Color? dividerColor,
        double? dividerHeight,
        bool showDivider,
        double? devicePixelRatio,
        TabIndicatorAnimation indicatorAnimation,
        TextDirection textDirection,
        IndicatorPainterNotifier repaint) : base(Listenable.Merge(controller.Animation, repaint))
    {
        Controller = controller;
        Indicator = indicator;
        IndicatorSize = indicatorSize;
        IndicatorPadding = indicatorPadding;
        TabKeys = tabKeys;
        LabelPaddings = labelPaddings;
        DividerColor = dividerColor;
        DividerHeight = dividerHeight;
        ShowDivider = showDivider;
        DevicePixelRatio = devicePixelRatio;
        IndicatorAnimation = indicatorAnimation;
        TextDirection = textDirection;
        _repaint = repaint;
        if (old is not null)
        {
            SaveTabOffsets(old._currentTabOffsets, old._currentTextDirection);
        }
    }

    public static IndicatorPainter Create(
        TabController controller,
        Decoration indicator,
        TabBarIndicatorSize indicatorSize,
        EdgeInsetsGeometry indicatorPadding,
        IReadOnlyList<GlobalKey> tabKeys,
        IReadOnlyList<EdgeInsetsGeometry> labelPaddings,
        IndicatorPainter? old,
        Color? dividerColor,
        double? dividerHeight,
        bool showDivider,
        double? devicePixelRatio,
        TabIndicatorAnimation indicatorAnimation,
        TextDirection textDirection)
    {
        return new IndicatorPainter(
            controller,
            indicator,
            indicatorSize,
            indicatorPadding,
            tabKeys,
            labelPaddings,
            old,
            dividerColor,
            dividerHeight,
            showDivider,
            devicePixelRatio,
            indicatorAnimation,
            textDirection,
            new IndicatorPainterNotifier());
    }

    public TabController Controller { get; }

    public Decoration Indicator { get; }

    public TabBarIndicatorSize IndicatorSize { get; }

    public EdgeInsetsGeometry IndicatorPadding { get; }

    public IReadOnlyList<GlobalKey> TabKeys { get; }

    public IReadOnlyList<EdgeInsetsGeometry> LabelPaddings { get; }

    public Color? DividerColor { get; }

    public double? DividerHeight { get; }

    public bool ShowDivider { get; }

    public double? DevicePixelRatio { get; }

    public TabIndicatorAnimation IndicatorAnimation { get; }

    public TextDirection TextDirection { get; }

    /// <summary>The most recently painted indicator rect, exposed for tests and parity probes.</summary>
    internal Rect? CurrentRect { get; private set; }

    internal int MaxTabIndex => (_currentTabOffsets?.Count ?? 1) - 2;

    public void MarkNeedsPaint()
    {
        _needsPaint = true;
        _repaint.Notify();
    }

    public void SaveTabOffsets(IReadOnlyList<double>? tabOffsets, TextDirection? textDirection)
    {
        _currentTabOffsets = tabOffsets;
        _currentTextDirection = textDirection;
    }

    public double CenterOf(int tabIndex)
    {
        IReadOnlyList<double> offsets = _currentTabOffsets
            ?? throw new InvalidOperationException("Tab offsets have not been laid out yet.");
        int index = Math.Clamp(tabIndex, 0, Math.Max(0, MaxTabIndex));
        return (offsets[index] + offsets[index + 1]) / 2.0;
    }

    public Rect IndicatorRect(Size tabBarSize, int tabIndex)
    {
        IReadOnlyList<double> offsets = _currentTabOffsets
            ?? throw new InvalidOperationException("Tab offsets have not been laid out yet.");
        double tabLeft;
        double tabRight;
        if (_currentTextDirection == Plumix.UI.TextDirection.Rtl)
        {
            tabLeft = offsets[tabIndex + 1];
            tabRight = offsets[tabIndex];
        }
        else
        {
            tabLeft = offsets[tabIndex];
            tabRight = offsets[tabIndex + 1];
        }

        if (IndicatorSize == TabBarIndicatorSize.Label)
        {
            double tabWidth = TabKeys[tabIndex].CurrentContext?.Size?.Width ?? tabRight - tabLeft;
            Thickness insets = LabelPaddings[tabIndex]
                .Resolve(_currentTextDirection ?? Plumix.UI.TextDirection.Ltr);
            double delta = ((tabRight - tabLeft) - (tabWidth + insets.Left + insets.Right)) / 2.0;
            tabLeft += delta + insets.Left;
            tabRight = tabLeft + tabWidth;
        }

        Thickness indicatorInsets = IndicatorPadding
            .Resolve(_currentTextDirection ?? Plumix.UI.TextDirection.Ltr);
        var rect = new Rect(tabLeft, 0.0, tabRight - tabLeft, tabBarSize.Height);
        if (rect.Width < indicatorInsets.Left + indicatorInsets.Right
            || rect.Height < indicatorInsets.Top + indicatorInsets.Bottom)
        {
            throw new InvalidOperationException(
                $"indicatorPadding insets should be less than Tab Size\nRect Size : {rect.Size}, "
                + $"Insets: {indicatorInsets}");
        }

        return new Rect(
            rect.Left + indicatorInsets.Left,
            rect.Top + indicatorInsets.Top,
            rect.Width - indicatorInsets.Left - indicatorInsets.Right,
            rect.Height - indicatorInsets.Top - indicatorInsets.Bottom);
    }

    public override void Paint(PaintingContext context, Size size)
    {
        _needsPaint = false;
        _painter ??= Indicator.CreateBoxPainter(MarkNeedsPaint);
        if (_currentTabOffsets is null || _currentTabOffsets.Count < 2 || Controller.Animation is null)
        {
            return;
        }

        double value = Controller.Animation.Value;
        CurrentRect = IndicatorAnimation == TabIndicatorAnimation.Linear
            ? ApplyLinearEffect(size, value)
            : ApplyElasticEffect(size, value);

        if (ShowDivider && DividerHeight is > 0)
        {
            double y = size.Height - (DividerHeight.Value / 2);
            context.Canvas.DrawLine(
                new Pen(new SolidColorBrush(DividerColor!.Value), DividerHeight.Value),
                new Point(0, y),
                new Point(size.Width, y));
        }

        var configuration = new ImageConfiguration(
            Size: CurrentRect!.Value.Size,
            TextDirection: _currentTextDirection,
            DevicePixelRatio: DevicePixelRatio);
        _painter.Paint(context, CurrentRect.Value.Position, configuration);
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate)
    {
        var old = (IndicatorPainter)oldDelegate;
        return _needsPaint
               || !ReferenceEquals(Controller, old.Controller)
               || !Equals(Indicator, old.Indicator)
               || TabKeys.Count != old.TabKeys.Count
               || !OffsetsEqual(_currentTabOffsets, old._currentTabOffsets)
               || _currentTextDirection != old._currentTextDirection;
    }

    public override void Dispose()
    {
        _painter?.Dispose();
        _repaint.Dispose();
        base.Dispose();
    }

    /// <summary>Dart's <c>decelerateInterpolation</c>: an ease-out sine.</summary>
    internal static double DecelerateInterpolation(double fraction) => Math.Sin(fraction * Math.PI / 2.0);

    /// <summary>Dart's <c>accelerateInterpolation</c>: an ease-in sine.</summary>
    internal static double AccelerateInterpolation(double fraction) => 1.0 - Math.Cos(fraction * Math.PI / 2.0);

    private Rect ApplyLinearEffect(Size size, double value)
    {
        double index = Controller.Index;
        bool ltr = index > value;
        int from = Math.Clamp(
            (int)(ltr ? Math.Floor(value) : Math.Ceiling(value)),
            0,
            MaxTabIndex);
        int to = Math.Clamp(ltr ? from + 1 : from - 1, 0, MaxTabIndex);
        return LerpRect(IndicatorRect(size, from), IndicatorRect(size, to), Math.Abs(value - from));
    }

    private Rect ApplyElasticEffect(Size size, double value)
    {
        double index = Controller.Index;
        double progressLeft = Math.Abs(index - value);
        bool useEdgeTabs = progressLeft == 0.0 || !Controller.IndexIsChanging;

        int to = useEdgeTabs
            ? Math.Clamp(
                (int)(TextDirection == Plumix.UI.TextDirection.Ltr ? Math.Ceiling(value) : Math.Floor(value)),
                0,
                MaxTabIndex)
            : Controller.Index;
        int from = useEdgeTabs
            ? Math.Clamp(
                TextDirection == Plumix.UI.TextDirection.Ltr ? to - 1 : to + 1,
                0,
                MaxTabIndex)
            : Controller.PreviousIndex;

        Rect toRect = IndicatorRect(size, to);
        Rect fromRect = IndicatorRect(size, from);
        Rect rect = LerpRect(fromRect, toRect, Math.Abs(value - from));
        if (Controller.Animation!.Status.IsCompleted())
        {
            return rect;
        }

        double tabChangeProgress;
        if (Controller.IndexIsChanging)
        {
            int tabsDelta = Math.Abs(Controller.Index - Controller.PreviousIndex);
            if (tabsDelta != 0)
            {
                progressLeft /= tabsDelta;
            }

            tabChangeProgress = 1 - Math.Clamp(progressLeft, 0.0, 1.0);
        }
        else
        {
            tabChangeProgress = Math.Abs(index - value);
        }

        if (tabChangeProgress == 1.0)
        {
            return rect;
        }

        bool isMovingRight = TextDirection == Plumix.UI.TextDirection.Ltr
            ? Controller.IndexIsChanging ? index > value : value > index
            : Controller.IndexIsChanging ? value > index : index > value;
        double leftFraction = isMovingRight
            ? AccelerateInterpolation(tabChangeProgress)
            : DecelerateInterpolation(tabChangeProgress);
        double rightFraction = isMovingRight
            ? DecelerateInterpolation(tabChangeProgress)
            : AccelerateInterpolation(tabChangeProgress);

        double lerpRectLeft;
        double lerpRectRight;
        if (Controller.IndexIsChanging)
        {
            lerpRectLeft = Lerp(fromRect.Left, toRect.Left, leftFraction);
            lerpRectRight = Lerp(fromRect.Right, toRect.Right, rightFraction);
        }
        else
        {
            lerpRectLeft = isMovingRight
                ? Lerp(fromRect.Left, toRect.Left, leftFraction)
                : Lerp(toRect.Left, fromRect.Left, leftFraction);
            lerpRectRight = isMovingRight
                ? Lerp(fromRect.Right, toRect.Right, rightFraction)
                : Lerp(toRect.Right, fromRect.Right, rightFraction);
        }

        return new Rect(
            lerpRectLeft,
            rect.Top,
            Math.Max(0.0, lerpRectRight - lerpRectLeft),
            rect.Height);
    }

    private static bool OffsetsEqual(IReadOnlyList<double>? a, IReadOnlyList<double>? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null || a.Count != b.Count)
        {
            return false;
        }

        for (int index = 0; index < a.Count; index++)
        {
            if (a[index] != b[index])
            {
                return false;
            }
        }

        return true;
    }

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);

    private static Rect LerpRect(Rect a, Rect b, double t) => new(
        Lerp(a.Left, b.Left, t),
        Lerp(a.Top, b.Top, t),
        Math.Max(0.0, Lerp(a.Right, b.Right, t) - Lerp(a.Left, b.Left, t)),
        Math.Max(0.0, Lerp(a.Bottom, b.Bottom, t) - Lerp(a.Top, b.Top, t)));
}

// Dart parity source: material_ui/lib/src/tabs.dart (_indexChangeProgress)
internal static class TabIndexProgress
{
    public static double Of(TabController controller)
    {
        double controllerValue = controller.Animation!.Value;
        double previousIndex = controller.PreviousIndex;
        double currentIndex = controller.Index;

        if (!controller.IndexIsChanging)
        {
            return Math.Clamp(Math.Abs(currentIndex - controllerValue), 0.0, 1.0);
        }

        double distance = Math.Abs(currentIndex - previousIndex);
        return distance == 0.0 ? 1.0 : Math.Abs(controllerValue - currentIndex) / distance;
    }
}

// Dart parity source: material_ui/lib/src/tabs.dart (_ChangeAnimation)
internal sealed class ChangeAnimation : AnimationWithParentMixin<double>
{
    private readonly TabController _controller;

    public ChangeAnimation(TabController controller) => _controller = controller;

    public override Animation<double> Parent => _controller.Animation!;

    public override double Value => TabIndexProgress.Of(_controller);

    public override void RemoveListener(Action listener)
    {
        if (_controller.Animation is not null)
        {
            base.RemoveListener(listener);
        }
    }

    public override void RemoveStatusListener(Action<AnimationStatus> listener)
    {
        if (_controller.Animation is not null)
        {
            base.RemoveStatusListener(listener);
        }
    }
}

// Dart parity source: material_ui/lib/src/tabs.dart (_DragAnimation)
internal sealed class DragAnimation : AnimationWithParentMixin<double>
{
    private readonly TabController _controller;
    private readonly int _index;

    public DragAnimation(TabController controller, int index)
    {
        _controller = controller;
        _index = index;
    }

    public override Animation<double> Parent => _controller.Animation!;

    public override double Value
    {
        get
        {
            double controllerMaxValue = _controller.Length - 1;
            double controllerValue = Math.Clamp(_controller.Animation!.Value, 0.0, controllerMaxValue);
            return Math.Clamp(Math.Abs(controllerValue - _index), 0.0, 1.0);
        }
    }

    public override void RemoveListener(Action listener)
    {
        if (_controller.Animation is not null)
        {
            base.RemoveListener(listener);
        }
    }

    public override void RemoveStatusListener(Action<AnimationStatus> listener)
    {
        if (_controller.Animation is not null)
        {
            base.RemoveStatusListener(listener);
        }
    }
}

// Dart parity source: material_ui/lib/src/tabs.dart (_TabBarScrollPosition)
internal sealed class TabBarScrollPosition : ScrollPosition
{
    private readonly ITabBarScrollHost _tabBar;
    private bool _viewportDimensionWasNonZero;
    private bool _needsPixelsCorrection = true;

    public TabBarScrollPosition(
        ScrollPhysics physics,
        IScrollContext context,
        ScrollPosition? oldPosition,
        ITabBarScrollHost tabBar)
        : base(physics, context, initialPixels: null, oldPosition: oldPosition)
    {
        _tabBar = tabBar;
    }

    public override bool ApplyContentDimensions(double minScrollExtent, double maxScrollExtent)
    {
        bool result = true;
        if (!_viewportDimensionWasNonZero)
        {
            _viewportDimensionWasNonZero = ViewportDimension != 0.0;
        }

        // If the viewport never had a non-zero dimension, or a controller/scroll-controller swap
        // asked for it, the initial scroll offset is (re)computed rather than preserved.
        if (!_viewportDimensionWasNonZero || _needsPixelsCorrection)
        {
            _needsPixelsCorrection = false;
            _ = CorrectPixels(_tabBar.InitialScrollOffset(ViewportDimension, minScrollExtent, maxScrollExtent));
            result = false;
        }

        return base.ApplyContentDimensions(minScrollExtent, maxScrollExtent) && result;
    }

    public void MarkNeedsPixelsCorrection() => _needsPixelsCorrection = true;
}

/// <summary>The <see cref="TabBar"/> state contract a <see cref="TabBarScrollController"/> needs.</summary>
internal interface ITabBarScrollHost
{
    double InitialScrollOffset(double viewportWidth, double minExtent, double maxExtent);
}

/// <summary>
/// A <see cref="ScrollController"/> that keeps a scrollable <see cref="TabBar"/>'s initial offset
/// centered on the selected tab.
/// </summary>
public sealed class TabBarScrollController : ScrollController
{
    internal ITabBarScrollHost? TabBarState { get; set; }

    /// <summary>Dart parity: <c>debugCheckHasTabBarState</c>.</summary>
    public bool DebugCheckHasTabBarState() => TabBarState is not null
        ? true
        : throw new InvalidOperationException("This TabBarScrollController is not attached to any TabBar.");

    public override ScrollPosition CreateScrollPosition(
        ScrollPhysics physics,
        IScrollContext context,
        ScrollPosition? oldPosition)
    {
        _ = DebugCheckHasTabBarState();
        return new TabBarScrollPosition(physics, context, oldPosition, TabBarState!);
    }

    public override void Dispose()
    {
        TabBarState = null;
        base.Dispose();
    }
}

/// <summary>
/// A Material Design widget that displays a horizontal row of tabs.
/// </summary>
public sealed class TabBar : StatefulWidget, IPreferredSizeWidget
{
    private const double StartOffset = 52.0;

    public TabBar(
        IReadOnlyList<Widget> tabs,
        TabController? controller = null,
        TabBarScrollController? scrollController = null,
        bool isScrollable = false,
        EdgeInsetsGeometry? padding = null,
        Color? indicatorColor = null,
        bool automaticIndicatorColorAdjustment = true,
        double indicatorWeight = 2.0,
        EdgeInsetsGeometry? indicatorPadding = null,
        Decoration? indicator = null,
        TabBarIndicatorSize? indicatorSize = null,
        Color? dividerColor = null,
        double? dividerHeight = null,
        WidgetStateColor? labelColor = null,
        TextStyle? labelStyle = null,
        EdgeInsetsGeometry? labelPadding = null,
        Color? unselectedLabelColor = null,
        TextStyle? unselectedLabelStyle = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        MaterialStateProperty<Color?>? overlayColor = null,
        MouseCursor? mouseCursor = null,
        bool? enableFeedback = null,
        Action<int>? onTap = null,
        TabValueChanged<bool>? onHover = null,
        TabValueChanged<bool>? onFocusChange = null,
        ScrollPhysics? physics = null,
        InteractiveInkFeatureFactory? splashFactory = null,
        BorderRadius? splashBorderRadius = null,
        TabAlignment? tabAlignment = null,
        TextScaler? textScaler = null,
        TabIndicatorAnimation? indicatorAnimation = null,
        Key? key = null) : this(
        tabs,
        isPrimary: true,
        controller,
        scrollController,
        isScrollable,
        padding,
        indicatorColor,
        automaticIndicatorColorAdjustment,
        indicatorWeight,
        indicatorPadding,
        indicator,
        indicatorSize,
        dividerColor,
        dividerHeight,
        labelColor,
        labelStyle,
        labelPadding,
        unselectedLabelColor,
        unselectedLabelStyle,
        dragStartBehavior,
        overlayColor,
        mouseCursor,
        enableFeedback,
        onTap,
        onHover,
        onFocusChange,
        physics,
        splashFactory,
        splashBorderRadius,
        tabAlignment,
        textScaler,
        indicatorAnimation,
        key)
    {
    }

    private TabBar(
        IReadOnlyList<Widget> tabs,
        bool isPrimary,
        TabController? controller,
        TabBarScrollController? scrollController,
        bool isScrollable,
        EdgeInsetsGeometry? padding,
        Color? indicatorColor,
        bool automaticIndicatorColorAdjustment,
        double indicatorWeight,
        EdgeInsetsGeometry? indicatorPadding,
        Decoration? indicator,
        TabBarIndicatorSize? indicatorSize,
        Color? dividerColor,
        double? dividerHeight,
        WidgetStateColor? labelColor,
        TextStyle? labelStyle,
        EdgeInsetsGeometry? labelPadding,
        Color? unselectedLabelColor,
        TextStyle? unselectedLabelStyle,
        DragStartBehavior dragStartBehavior,
        MaterialStateProperty<Color?>? overlayColor,
        MouseCursor? mouseCursor,
        bool? enableFeedback,
        Action<int>? onTap,
        TabValueChanged<bool>? onHover,
        TabValueChanged<bool>? onFocusChange,
        ScrollPhysics? physics,
        InteractiveInkFeatureFactory? splashFactory,
        BorderRadius? splashBorderRadius,
        TabAlignment? tabAlignment,
        TextScaler? textScaler,
        TabIndicatorAnimation? indicatorAnimation,
        Key? key) : base(key)
    {
        Tabs = tabs ?? throw new ArgumentNullException(nameof(tabs));
        if (indicator is null && (!double.IsFinite(indicatorWeight) || indicatorWeight <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(indicatorWeight));
        }

        if (dividerHeight.HasValue && !double.IsFinite(dividerHeight.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(dividerHeight));
        }

        IsPrimary = isPrimary;
        Controller = controller;
        ScrollController = scrollController;
        IsScrollable = isScrollable;
        Padding = padding;
        IndicatorColor = indicatorColor;
        AutomaticIndicatorColorAdjustment = automaticIndicatorColorAdjustment;
        IndicatorWeight = indicatorWeight;
        IndicatorPadding = indicatorPadding ?? EdgeInsetsGeometry.Zero;
        Indicator = indicator;
        IndicatorSize = indicatorSize;
        DividerColor = dividerColor;
        DividerHeight = dividerHeight;
        LabelColor = labelColor;
        LabelStyle = labelStyle;
        LabelPadding = labelPadding;
        UnselectedLabelColor = unselectedLabelColor;
        UnselectedLabelStyle = unselectedLabelStyle;
        DragStartBehavior = dragStartBehavior;
        OverlayColor = overlayColor;
        MouseCursor = mouseCursor;
        EnableFeedback = enableFeedback;
        OnTap = onTap;
        OnHover = onHover;
        OnFocusChange = onFocusChange;
        Physics = physics;
        SplashFactory = splashFactory;
        SplashBorderRadius = splashBorderRadius;
        TabAlignment = tabAlignment;
        TextScaler = textScaler;
        IndicatorAnimation = indicatorAnimation;
    }

    /// <summary>Creates a Material Design secondary tab bar.</summary>
    public static TabBar Secondary(
        IReadOnlyList<Widget> tabs,
        TabController? controller = null,
        TabBarScrollController? scrollController = null,
        bool isScrollable = false,
        EdgeInsetsGeometry? padding = null,
        Color? indicatorColor = null,
        bool automaticIndicatorColorAdjustment = true,
        double indicatorWeight = 2.0,
        EdgeInsetsGeometry? indicatorPadding = null,
        Decoration? indicator = null,
        TabBarIndicatorSize? indicatorSize = null,
        Color? dividerColor = null,
        double? dividerHeight = null,
        WidgetStateColor? labelColor = null,
        TextStyle? labelStyle = null,
        EdgeInsetsGeometry? labelPadding = null,
        Color? unselectedLabelColor = null,
        TextStyle? unselectedLabelStyle = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        MaterialStateProperty<Color?>? overlayColor = null,
        MouseCursor? mouseCursor = null,
        bool? enableFeedback = null,
        Action<int>? onTap = null,
        TabValueChanged<bool>? onHover = null,
        TabValueChanged<bool>? onFocusChange = null,
        ScrollPhysics? physics = null,
        InteractiveInkFeatureFactory? splashFactory = null,
        BorderRadius? splashBorderRadius = null,
        TabAlignment? tabAlignment = null,
        TextScaler? textScaler = null,
        TabIndicatorAnimation? indicatorAnimation = null,
        Key? key = null) => new(
        tabs,
        isPrimary: false,
        controller,
        scrollController,
        isScrollable,
        padding,
        indicatorColor,
        automaticIndicatorColorAdjustment,
        indicatorWeight,
        indicatorPadding,
        indicator,
        indicatorSize,
        dividerColor,
        dividerHeight,
        labelColor,
        labelStyle,
        labelPadding,
        unselectedLabelColor,
        unselectedLabelStyle,
        dragStartBehavior,
        overlayColor,
        mouseCursor,
        enableFeedback,
        onTap,
        onHover,
        onFocusChange,
        physics,
        splashFactory,
        splashBorderRadius,
        tabAlignment,
        textScaler,
        indicatorAnimation,
        key);

    public IReadOnlyList<Widget> Tabs { get; }

    public TabController? Controller { get; }

    public TabBarScrollController? ScrollController { get; }

    public bool IsScrollable { get; }

    public EdgeInsetsGeometry? Padding { get; }

    public Color? IndicatorColor { get; }

    public bool AutomaticIndicatorColorAdjustment { get; }

    public double IndicatorWeight { get; }

    public EdgeInsetsGeometry IndicatorPadding { get; }

    public Decoration? Indicator { get; }

    public TabBarIndicatorSize? IndicatorSize { get; }

    public Color? DividerColor { get; }

    public double? DividerHeight { get; }

    public WidgetStateColor? LabelColor { get; }

    public TextStyle? LabelStyle { get; }

    public EdgeInsetsGeometry? LabelPadding { get; }

    public Color? UnselectedLabelColor { get; }

    public TextStyle? UnselectedLabelStyle { get; }

    public DragStartBehavior DragStartBehavior { get; }

    public MaterialStateProperty<Color?>? OverlayColor { get; }

    public MouseCursor? MouseCursor { get; }

    public bool? EnableFeedback { get; }

    public Action<int>? OnTap { get; }

    public TabValueChanged<bool>? OnHover { get; }

    public TabValueChanged<bool>? OnFocusChange { get; }

    public ScrollPhysics? Physics { get; }

    public InteractiveInkFeatureFactory? SplashFactory { get; }

    public BorderRadius? SplashBorderRadius { get; }

    public TabAlignment? TabAlignment { get; }

    public TextScaler? TextScaler { get; }

    public TabIndicatorAnimation? IndicatorAnimation { get; }

    /// <summary>Whether this bar is a primary (true) or secondary (false) Material 3 tab bar.</summary>
    internal bool IsPrimary { get; }

    public Size PreferredSize
    {
        get
        {
            double maxHeight = Tab.TabHeight;
            foreach (Widget item in Tabs)
            {
                if (item is IPreferredSizeWidget preferred)
                {
                    maxHeight = Math.Max(preferred.PreferredSize.Height, maxHeight);
                }
            }

            return new Size(0, maxHeight + IndicatorWeight);
        }
    }

    /// <summary>Whether any tab in this bar has both a label and an icon.</summary>
    public bool TabHasTextAndIcon
    {
        get
        {
            foreach (Widget item in Tabs)
            {
                if (item is IPreferredSizeWidget preferred
                    && preferred.PreferredSize.Height == Tab.TextAndIconTabHeight)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public override State CreateState() => new TabBarState();

    private sealed class TabBarState : State, ITabBarScrollHost
    {
        private readonly List<GlobalKey> _tabKeys = [];
        private readonly List<EdgeInsetsGeometry> _labelPaddings = [];
        private IndicatorPainter? _indicatorPainter;
        private TabController? _controller;
        private TabBarScrollController? _internalScrollController;
        private int _currentIndex;
        private double _tabStripWidth;

        private TabBar Current => (TabBar)StateWidget;

        private bool ControllerIsValid => _controller?.Animation is not null;

        private TabBarScrollController EffectiveScrollController
        {
            get
            {
                if (Current.ScrollController is { } external)
                {
                    _internalScrollController?.Dispose();
                    _internalScrollController = null;
                    return external;
                }

                return _internalScrollController ??= new TabBarScrollController();
            }
        }

        public override void InitState()
        {
            for (int index = 0; index < Current.Tabs.Count; index++)
            {
                _tabKeys.Add(new LabeledGlobalKey<State>("TabBar tab"));
                _labelPaddings.Add(EdgeInsetsGeometry.Zero);
            }
        }

        public override void DidChangeDependencies()
        {
            UpdateScrollController();
            UpdateTabController();
            InitIndicatorPainter();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var old = (TabBar)oldWidget;
            if (!ReferenceEquals(old.Controller, Current.Controller)
                || !ReferenceEquals(old.ScrollController, Current.ScrollController))
            {
                UpdateScrollController(old.ScrollController);
                UpdateTabController();
                InitIndicatorPainter();
                if (EffectiveScrollController.PrimaryPosition is TabBarScrollPosition position)
                {
                    position.MarkNeedsPixelsCorrection();
                }
            }
            else if (old.IndicatorColor != Current.IndicatorColor
                     || old.IndicatorWeight != Current.IndicatorWeight
                     || old.IndicatorSize != Current.IndicatorSize
                     || !old.IndicatorPadding.Equals(Current.IndicatorPadding)
                     || !Equals(old.Indicator, Current.Indicator)
                     || old.DividerColor != Current.DividerColor
                     || old.DividerHeight != Current.DividerHeight
                     || old.IndicatorAnimation != Current.IndicatorAnimation)
            {
                InitIndicatorPainter();
            }

            if (Current.Tabs.Count > _tabKeys.Count)
            {
                int delta = Current.Tabs.Count - _tabKeys.Count;
                for (int index = 0; index < delta; index++)
                {
                    _tabKeys.Add(new LabeledGlobalKey<State>("TabBar tab"));
                    _labelPaddings.Add(EdgeInsetsGeometry.Zero);
                }
            }
            else if (Current.Tabs.Count < _tabKeys.Count)
            {
                _tabKeys.RemoveRange(Current.Tabs.Count, _tabKeys.Count - Current.Tabs.Count);
                _labelPaddings.RemoveRange(Current.Tabs.Count, _labelPaddings.Count - Current.Tabs.Count);
            }
        }

        public override void Dispose()
        {
            _indicatorPainter?.Dispose();
            if (ControllerIsValid)
            {
                _controller!.Animation!.RemoveListener(HandleTabControllerAnimationTick);
                _controller.RemoveListener(HandleTabControllerTick);
            }

            _controller = null;
            _internalScrollController?.Dispose();
            if (Current.ScrollController is { } external)
            {
                external.TabBarState = null;
            }
        }

        public override Widget Build(BuildContext context)
        {
            var theme = Theme.Of(context);
            TabBarThemeData tabBarTheme = TabBarTheme.Of(context);
            TabBarDefaults defaults = ResolveDefaults(context);
            MaterialLocalizations localizations = MaterialLocalizations.Of(context);
            TabAlignment effectiveTabAlignment = Current.TabAlignment
                                                 ?? tabBarTheme.TabAlignment
                                                 ?? defaults.TabAlignment;
            ValidateTabAlignment(effectiveTabAlignment);
            if (_controller is null)
            {
                UpdateTabController();
            }

            if (_controller!.Length != Current.Tabs.Count)
            {
                throw new InvalidOperationException(
                    $"Controller's length property ({_controller.Length}) does not match the number of "
                    + $"tabs ({Current.Tabs.Count}) present in TabBar's tabs property.");
            }

            if (_controller.Length == 0)
            {
                return new LimitedBox(
                    maxWidth: 0.0,
                    child: new SizedBox(
                        width: double.PositiveInfinity,
                        height: Tab.TabHeight + Current.IndicatorWeight));
            }

            EdgeInsetsGeometry labelPadding = Current.LabelPadding
                                              ?? tabBarTheme.LabelPadding
                                              ?? MaterialConstants.TabLabelPadding;
            bool tabHasTextAndIcon = Current.TabHasTextAndIcon;
            var wrappedTabs = new List<Widget>(Current.Tabs.Count);
            for (int index = 0; index < Current.Tabs.Count; index++)
            {
                EdgeInsetsGeometry adjustedPadding = labelPadding;
                if (tabHasTextAndIcon
                    && Current.Tabs[index] is IPreferredSizeWidget preferred
                    && preferred.PreferredSize.Height == Tab.TabHeight)
                {
                    double verticalAdjustment = (Tab.TextAndIconTabHeight - Tab.TabHeight) / 2.0;
                    adjustedPadding = labelPadding.Add(
                        EdgeInsetsGeometry.Symmetric(vertical: verticalAdjustment));
                }

                _labelPaddings[index] = adjustedPadding;
                wrappedTabs.Add(new Center(
                    heightFactor: 1.0,
                    child: new Padding(
                        adjustedPadding,
                        new KeyedSubtree(Current.Tabs[index], _tabKeys[index]))));
            }

            if (_controller.Index != _currentIndex)
            {
                _currentIndex = _controller.Index;
            }

            if (_controller.IndexIsChanging)
            {
                var animation = new ChangeAnimation(_controller);
                wrappedTabs[_currentIndex] = BuildStyledTab(
                    wrappedTabs[_currentIndex], isSelected: true, animation, defaults);
                wrappedTabs[_controller.PreviousIndex] = BuildStyledTab(
                    wrappedTabs[_controller.PreviousIndex], isSelected: false, animation, defaults);
            }
            else
            {
                wrappedTabs[_currentIndex] = BuildStyledTab(
                    wrappedTabs[_currentIndex],
                    isSelected: true,
                    new DragAnimation(_controller, _currentIndex),
                    defaults);
                if (_currentIndex > 0)
                {
                    wrappedTabs[_currentIndex - 1] = BuildStyledTab(
                        wrappedTabs[_currentIndex - 1],
                        isSelected: false,
                        new ReverseAnimation(new DragAnimation(_controller, _currentIndex - 1)),
                        defaults);
                }

                if (_currentIndex < Current.Tabs.Count - 1)
                {
                    wrappedTabs[_currentIndex + 1] = BuildStyledTab(
                        wrappedTabs[_currentIndex + 1],
                        isSelected: false,
                        new ReverseAnimation(new DragAnimation(_controller, _currentIndex + 1)),
                        defaults);
                }
            }

            for (int index = 0; index < wrappedTabs.Count; index++)
            {
                int tabIndex = index;
                MaterialState selectedState = tabIndex == _currentIndex
                    ? MaterialState.Selected
                    : MaterialState.None;
                // The default overlay resolves the tab's own selected state on top of the
                // interaction states the ink well supplies.
                MaterialStateProperty<Color?> defaultOverlay = MaterialStateProperty<Color?>.ResolveWith(
                    states => defaults.OverlayColor?.Resolve(states | selectedState));
                MouseCursor effectiveMouseCursor = Current.MouseCursor
                                                   ?? tabBarTheme.MouseCursor?.Resolve(selectedState)
                                                   ?? SystemMouseCursors.Click;
                Widget child = wrappedTabs[tabIndex];
                child = new Semantics(
                    role: SemanticsRole.Tab,
                    child: new Stack(children:
                    [
                        child,
                        new Semantics(
                            selected: tabIndex == _currentIndex,
                            label: localizations.TabLabel(tabIndex + 1, Current.Tabs.Count)),
                    ]));
                child = new InkWell(
                    mouseCursor: effectiveMouseCursor,
                    onTap: () => HandleTap(tabIndex),
                    onHover: value => Current.OnHover?.Invoke(value, tabIndex),
                    onFocusChange: value => Current.OnFocusChange?.Invoke(value, tabIndex),
                    enableFeedback: Current.EnableFeedback ?? true,
                    overlayColor: Current.OverlayColor ?? tabBarTheme.OverlayColor ?? defaultOverlay,
                    splashFactory: Current.SplashFactory
                                   ?? tabBarTheme.SplashFactory
                                   ?? defaults.SplashFactory,
                    borderRadius: Current.SplashBorderRadius
                                  ?? tabBarTheme.SplashBorderRadius
                                  ?? defaults.SplashBorderRadius,
                    child: new Padding(
                        new Thickness(0, 0, 0, Current.IndicatorWeight),
                        child));
                child = new MergeSemantics(child);
                if (!Current.IsScrollable && effectiveTabAlignment == global::Plumix.Material.TabAlignment.Fill)
                {
                    child = new Expanded(child);
                }

                wrappedTabs[tabIndex] = child;
            }

            Widget tabBar = new Semantics(
                role: SemanticsRole.TabBar,
                container: true,
                explicitChildNodes: true,
                child: new CustomPaint(
                    painter: _indicatorPainter,
                    child: new TabStyle(
                        animation: AlwaysDismissed,
                        isSelected: false,
                        isPrimary: Current.IsPrimary,
                        labelColor: Current.LabelColor,
                        unselectedLabelColor: Current.UnselectedLabelColor,
                        labelStyle: Current.LabelStyle,
                        unselectedLabelStyle: Current.UnselectedLabelStyle,
                        defaults: defaults,
                        child: new TabLabelBar(
                            children: wrappedTabs,
                            onPerformLayout: SaveTabOffsets,
                            mainAxisSize: effectiveTabAlignment == global::Plumix.Material.TabAlignment.Fill
                                ? MainAxisSize.Max
                                : MainAxisSize.Min))));

            if (Current.IsScrollable)
            {
                bool startOffsetAligned =
                    effectiveTabAlignment == global::Plumix.Material.TabAlignment.StartOffset;
                EdgeInsetsGeometry? effectivePadding = startOffsetAligned
                    ? EdgeInsetsGeometry.DirectionalOnly(start: StartOffset)
                        .Add(Current.Padding ?? EdgeInsetsGeometry.Zero)
                    : Current.Padding;
                tabBar = new ScrollConfiguration(
                    behavior: ScrollConfiguration.Of(context).CopyWith(overscroll: false),
                    child: new SingleChildScrollView(
                        scrollDirection: Axis.Horizontal,
                        controller: EffectiveScrollController,
                        padding: effectivePadding?.Resolve(Directionality.Of(context)),
                        physics: Current.Physics,
                        child: tabBar));
                if (theme.UseMaterial3)
                {
                    bool centerAligned =
                        effectiveTabAlignment == global::Plumix.Material.TabAlignment.Center;
                    AlignmentGeometry effectiveAlignment = centerAligned
                        ? Alignment.Center
                        : AlignmentDirectional.CenterStart;
                    Color scrollableDividerColor = Current.DividerColor
                                                   ?? tabBarTheme.DividerColor
                                                   ?? defaults.DividerColor!.Value;
                    double scrollableDividerHeight = Current.DividerHeight
                                                     ?? tabBarTheme.DividerHeight
                                                     ?? defaults.DividerHeight!.Value;
                    tabBar = new Align(
                        heightFactor: 1.0,
                        widthFactor: scrollableDividerHeight > 0 ? null : 1.0,
                        alignment: effectiveAlignment,
                        child: tabBar);
                    if (scrollableDividerColor != Colors.Transparent && scrollableDividerHeight > 0)
                    {
                        tabBar = new CustomPaint(
                            painter: new DividerPainter(scrollableDividerColor, scrollableDividerHeight),
                            child: tabBar);
                    }
                }
            }
            else if (Current.Padding is { } padding)
            {
                tabBar = new Padding(padding, tabBar);
            }

            TextScaler? effectiveTextScaler = Current.TextScaler ?? tabBarTheme.TextScaler;
            MediaQueryData ambientMedia = MediaQuery.MaybeOf(context) ?? new MediaQueryData();
            tabBar = new MediaQuery(
                ambientMedia.CopyWith(textScaler: effectiveTextScaler),
                tabBar);

            return new Material(type: MaterialType.Transparency, child: tabBar);
        }

        public double InitialScrollOffset(double viewportWidth, double minExtent, double maxExtent) =>
            TabScrollOffset(_currentIndex, viewportWidth, minExtent, maxExtent);

        private static readonly Animation<double> AlwaysDismissed =
            new ConstantAnimation<double>(0.0, AnimationStatus.Dismissed);

        private TabBarDefaults ResolveDefaults(BuildContext context)
        {
            if (!Theme.Of(context).UseMaterial3)
            {
                return new TabsDefaultsM2(context, Current.IsScrollable);
            }

            return Current.IsPrimary
                ? new TabsPrimaryDefaultsM3(context, Current.IsScrollable)
                : new TabsSecondaryDefaultsM3(context, Current.IsScrollable);
        }

        private Widget BuildStyledTab(
            Widget child,
            bool isSelected,
            Animation<double> animation,
            TabBarDefaults defaults) => new TabStyle(
            animation: animation,
            isSelected: isSelected,
            isPrimary: Current.IsPrimary,
            labelColor: Current.LabelColor,
            unselectedLabelColor: Current.UnselectedLabelColor,
            labelStyle: Current.LabelStyle,
            unselectedLabelStyle: Current.UnselectedLabelStyle,
            defaults: defaults,
            child: child);

        private void ValidateTabAlignment(TabAlignment tabAlignment)
        {
            if (Current.IsScrollable && tabAlignment == global::Plumix.Material.TabAlignment.Fill)
            {
                throw new ArgumentException($"{tabAlignment} is only valid for non-scrollable tab bars.");
            }

            if (!Current.IsScrollable
                && tabAlignment is global::Plumix.Material.TabAlignment.Start
                    or global::Plumix.Material.TabAlignment.StartOffset)
            {
                throw new ArgumentException($"{tabAlignment} is only valid for scrollable tab bars.");
            }
        }

        private void UpdateScrollController(TabBarScrollController? oldScrollController = null)
        {
            if (!ReferenceEquals(oldScrollController, Current.ScrollController)
                && oldScrollController is not null)
            {
                oldScrollController.TabBarState = null;
            }

            if (Current.ScrollController is { } external)
            {
                if (_internalScrollController is not null)
                {
                    _internalScrollController.TabBarState = null;
                }

                external.TabBarState = this;
                return;
            }

            _internalScrollController ??= new TabBarScrollController();
            _internalScrollController.TabBarState = this;
        }

        private void UpdateTabController()
        {
            TabController newController = Current.Controller
                                          ?? DefaultTabController.MaybeOf(Context)
                                          ?? throw new InvalidOperationException(
                                              "No TabController for TabBar.\nWhen creating a TabBar, you "
                                              + "must either provide an explicit TabController using the "
                                              + "\"controller\" property, or you must ensure that there is "
                                              + "a DefaultTabController above the TabBar.");
            if (ReferenceEquals(newController, _controller))
            {
                return;
            }

            if (ControllerIsValid)
            {
                _controller!.Animation!.RemoveListener(HandleTabControllerAnimationTick);
                _controller.RemoveListener(HandleTabControllerTick);
            }

            _controller = newController;
            _controller.Animation!.AddListener(HandleTabControllerAnimationTick);
            _controller.AddListener(HandleTabControllerTick);
            _currentIndex = _controller.Index;
        }

        private void InitIndicatorPainter()
        {
            var theme = Theme.Of(Context);
            TabBarThemeData tabBarTheme = TabBarTheme.Of(Context);
            TabBarDefaults defaults = ResolveDefaults(Context);
            TabBarIndicatorSize indicatorSize = Current.IndicatorSize
                                                ?? tabBarTheme.IndicatorSize
                                                ?? defaults.IndicatorSize;
            TabIndicatorAnimation defaultTabIndicatorAnimation =
                indicatorSize == TabBarIndicatorSize.Label
                    ? TabIndicatorAnimation.Elastic
                    : TabIndicatorAnimation.Linear;

            IndicatorPainter? oldPainter = _indicatorPainter;
            _indicatorPainter = !ControllerIsValid
                ? null
                : IndicatorPainter.Create(
                    controller: _controller!,
                    indicator: GetIndicator(indicatorSize, theme, tabBarTheme, defaults),
                    indicatorSize: indicatorSize,
                    indicatorPadding: Current.IndicatorPadding,
                    tabKeys: _tabKeys,
                    labelPaddings: _labelPaddings,
                    old: oldPainter,
                    dividerColor: Current.DividerColor ?? tabBarTheme.DividerColor ?? defaults.DividerColor,
                    dividerHeight: Current.DividerHeight ?? tabBarTheme.DividerHeight ?? defaults.DividerHeight,
                    showDivider: theme.UseMaterial3 && !Current.IsScrollable,
                    devicePixelRatio: MediaQuery.MaybeOf(Context)?.DevicePixelRatio,
                    indicatorAnimation: Current.IndicatorAnimation
                                        ?? tabBarTheme.IndicatorAnimation
                                        ?? defaultTabIndicatorAnimation,
                    textDirection: Directionality.Of(Context));
            oldPainter?.Dispose();
        }

        private Decoration GetIndicator(
            TabBarIndicatorSize indicatorSize,
            ThemeData theme,
            TabBarThemeData tabBarTheme,
            TabBarDefaults defaults)
        {
            if (Current.Indicator is { } widgetIndicator)
            {
                return widgetIndicator;
            }

            if (tabBarTheme.Indicator is { } themeIndicator)
            {
                return themeIndicator;
            }

            Color color = Current.IndicatorColor
                          ?? tabBarTheme.IndicatorColor
                          ?? defaults.IndicatorColor!.Value;
            // ThemeData tries to avoid this by having the indicatorColor avoid the material color.
            if (Current.AutomaticIndicatorColorAdjustment
                && color == Material.MaybeOf(Context)?.Color)
            {
                color = Colors.White;
            }

            double effectiveIndicatorWeight = theme.UseMaterial3
                ? Math.Max(
                    Current.IndicatorWeight,
                    Current.IsPrimary
                        ? TabsPrimaryDefaultsM3.IndicatorWeightFor(indicatorSize)
                        : TabsSecondaryDefaultsM3.IndicatorWeight)
                : Current.IndicatorWeight;
            bool primaryWithLabelIndicator = indicatorSize == TabBarIndicatorSize.Label && Current.IsPrimary;
            BorderRadius? effectiveBorderRadius = theme.UseMaterial3 && primaryWithLabelIndicator
                ? BorderRadius.Only(
                    topLeft: effectiveIndicatorWeight,
                    topRight: effectiveIndicatorWeight)
                : null;
            return new UnderlineTabIndicator(
                borderRadius: effectiveBorderRadius,
                borderSide: new BorderSide(color, effectiveIndicatorWeight));
        }

        private void HandleTabControllerAnimationTick()
        {
            if (!Mounted || _controller is null)
            {
                return;
            }

            if (!_controller.IndexIsChanging && Current.IsScrollable)
            {
                _currentIndex = _controller.Index;
                ScrollToControllerValue();
            }
        }

        private void HandleTabControllerTick()
        {
            if (!Mounted || _controller is null)
            {
                return;
            }

            if (_controller.Index != _currentIndex)
            {
                _currentIndex = _controller.Index;
                if (Current.IsScrollable)
                {
                    ScrollToCurrentIndex();
                }
            }

            SetState(() => { });
        }

        private void SaveTabOffsets(
            IReadOnlyList<double> tabOffsets,
            TextDirection textDirection,
            double width)
        {
            _tabStripWidth = width;
            _indicatorPainter?.SaveTabOffsets(tabOffsets, textDirection);
        }

        private void HandleTap(int index)
        {
            _controller!.AnimateTo(index);
            Current.OnTap?.Invoke(index);
        }

        private double TabScrollOffset(int index, double viewportWidth, double minExtent, double maxExtent)
        {
            if (!Current.IsScrollable)
            {
                return 0.0;
            }

            double tabCenter = _indicatorPainter?.CenterOf(index) ?? 0.0;
            double paddingStart;
            if (Directionality.Of(Context) == Plumix.UI.TextDirection.Rtl)
            {
                paddingStart = Current.Padding?.Resolve(Plumix.UI.TextDirection.Rtl).Right ?? 0;
                tabCenter = _tabStripWidth - tabCenter;
            }
            else
            {
                paddingStart = Current.Padding?.Resolve(Plumix.UI.TextDirection.Ltr).Left ?? 0;
            }

            return Math.Clamp(tabCenter + paddingStart - (viewportWidth / 2.0), minExtent, maxExtent);
        }

        private double TabCenteredScrollOffset(int index)
        {
            ScrollPosition position = EffectiveScrollController.Position;
            return TabScrollOffset(
                index,
                position.ViewportDimension,
                position.MinScrollExtent,
                position.MaxScrollExtent);
        }

        private void ScrollToCurrentIndex()
        {
            if (!EffectiveScrollController.HasClients)
            {
                return;
            }

            EffectiveScrollController.AnimateTo(
                TabCenteredScrollOffset(_currentIndex),
                MaterialConstants.TabScrollDuration,
                Curves.Ease);
        }

        private void ScrollToControllerValue()
        {
            if (!EffectiveScrollController.HasClients || _indicatorPainter is null)
            {
                return;
            }

            double? leading = _currentIndex > 0 ? TabCenteredScrollOffset(_currentIndex - 1) : null;
            double middle = TabCenteredScrollOffset(_currentIndex);
            double? trailing = _currentIndex < _indicatorPainter.MaxTabIndex
                ? TabCenteredScrollOffset(_currentIndex + 1)
                : null;

            double index = _controller!.Index;
            double value = _controller.Animation!.Value;
            double offset;
            if (value == index - 1.0)
            {
                offset = leading ?? middle;
            }
            else if (value == index + 1.0)
            {
                offset = trailing ?? middle;
            }
            else if (value == index)
            {
                offset = middle;
            }
            else if (value < index)
            {
                offset = leading is null ? middle : Lerp(middle, leading.Value, index - value);
            }
            else
            {
                offset = trailing is null ? middle : Lerp(middle, trailing.Value, value - index);
            }

            EffectiveScrollController.JumpTo(offset);
        }

        private static double Lerp(double a, double b, double t) => a + ((b - a) * t);
    }
}

/// <summary>
/// A page view that displays the widget which corresponds to the currently selected tab.
/// </summary>
public sealed class TabBarView : StatefulWidget
{
    public TabBarView(
        IReadOnlyList<Widget> children,
        TabController? controller = null,
        ScrollPhysics? physics = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        double viewportFraction = 1.0,
        Clip clipBehavior = Clip.HardEdge,
        Key? key = null) : base(key)
    {
        if (!double.IsFinite(viewportFraction) || viewportFraction <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(viewportFraction));
        }

        Children = children ?? throw new ArgumentNullException(nameof(children));
        Controller = controller;
        Physics = physics;
        DragStartBehavior = dragStartBehavior;
        ViewportFraction = viewportFraction;
        ClipBehavior = clipBehavior;
    }

    public IReadOnlyList<Widget> Children { get; }

    public TabController? Controller { get; }

    public ScrollPhysics? Physics { get; }

    public DragStartBehavior DragStartBehavior { get; }

    public double ViewportFraction { get; }

    public Clip ClipBehavior { get; }

    public override State CreateState() => new TabBarViewState();

    private sealed class TabBarViewState : State
    {
        private TabController? _controller;
        private PageController? _pageController;
        private IReadOnlyList<Widget> _childrenWithKey = [];
        private int _currentIndex;
        private int _warpUnderwayCount;
        private int _scrollUnderwayCount;

        private TabBarView Current => (TabBarView)StateWidget;

        private bool ControllerIsValid => _controller?.Animation is not null;

        /// <summary>
        /// The page view's current page, or null while no page view is attached to the controller.
        /// Flutter reads <c>_pageController.page</c> directly and relies on its debug assert.
        /// </summary>
        private double? CurrentPage => _pageController?.HasClients == true ? _pageController.Page : null;

        public override void InitState() => UpdateChildren();

        public override void DidChangeDependencies()
        {
            UpdateTabController();
            _currentIndex = _controller!.Index;
            if (_pageController is null)
            {
                _pageController = new PageController(
                    initialPage: _currentIndex,
                    viewportFraction: Current.ViewportFraction);
            }
            else if (_pageController.HasClients)
            {
                _pageController.JumpToPage(_currentIndex);
            }
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var old = (TabBarView)oldWidget;
            if (!ReferenceEquals(old.Controller, Current.Controller))
            {
                UpdateTabController();
                _currentIndex = _controller!.Index;
                JumpToPage(_currentIndex);
            }

            if (old.ViewportFraction != Current.ViewportFraction)
            {
                _pageController?.Dispose();
                _pageController = new PageController(
                    initialPage: _currentIndex,
                    viewportFraction: Current.ViewportFraction);
            }

            // While a warp is underway the child list is temporarily reordered; do not clobber it.
            if (!ReferenceEquals(old.Children, Current.Children) && _warpUnderwayCount == 0)
            {
                UpdateChildren();
            }
        }

        public override void Dispose()
        {
            if (ControllerIsValid)
            {
                _controller!.Animation!.RemoveListener(HandleTabControllerAnimationTick);
            }

            _controller = null;
            _pageController?.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            if (_controller!.Length != Current.Children.Count)
            {
                throw new InvalidOperationException(
                    $"Controller's length property ({_controller.Length}) does not match the number of "
                    + $"children ({Current.Children.Count}) present in TabBarView's children property.");
            }

            return new NotificationListener<ScrollNotification>(
                onNotification: HandleScrollNotification,
                child: new PageView(
                    children: _childrenWithKey,
                    controller: _pageController,
                    physics: new PageScrollPhysics(Current.Physics ?? new ClampingScrollPhysics()),
                    dragStartBehavior: Current.DragStartBehavior,
                    clipBehavior: Current.ClipBehavior));
        }

        private void UpdateTabController()
        {
            TabController newController = Current.Controller
                                          ?? DefaultTabController.MaybeOf(Context)
                                          ?? throw new InvalidOperationException(
                                              "No TabController for TabBarView.\nWhen creating a "
                                              + "TabBarView, you must either provide an explicit "
                                              + "TabController using the \"controller\" property, or you "
                                              + "must ensure that there is a DefaultTabController above "
                                              + "the TabBarView.");
            if (ReferenceEquals(newController, _controller))
            {
                return;
            }

            if (ControllerIsValid)
            {
                _controller!.Animation!.RemoveListener(HandleTabControllerAnimationTick);
            }

            _controller = newController;
            _controller.Animation!.AddListener(HandleTabControllerAnimationTick);
        }

        private void UpdateChildren()
        {
            _childrenWithKey = Current.Children
                .Select(child => (Widget)new Semantics(role: SemanticsRole.TabPanel, child: child))
                .ToArray();
        }

        private void JumpToPage(int page)
        {
            _warpUnderwayCount += 1;
            _pageController!.JumpToPage(page);
            _warpUnderwayCount -= 1;
        }

        private void AnimateToPage(int page, TimeSpan duration, Curve curve)
        {
            _warpUnderwayCount += 1;
            _pageController!.AnimateToPage(page, duration, curve);
            _warpUnderwayCount -= 1;
        }

        private void HandleTabControllerAnimationTick()
        {
            if (_scrollUnderwayCount > 0 || !_controller!.IndexIsChanging)
            {
                return;
            }

            if (_controller.Index != _currentIndex)
            {
                _currentIndex = _controller.Index;
                WarpToCurrentIndex();
            }
        }

        private void WarpToCurrentIndex()
        {
            if (!Mounted || CurrentPage == _currentIndex)
            {
                return;
            }

            bool adjacentDestination = Math.Abs(_currentIndex - _controller!.PreviousIndex) == 1;
            if (adjacentDestination)
            {
                WarpToAdjacentTab(_controller.AnimationDuration);
            }
            else
            {
                WarpToNonAdjacentTab(_controller.AnimationDuration);
            }
        }

        private void WarpToAdjacentTab(TimeSpan duration)
        {
            if (duration == TimeSpan.Zero)
            {
                JumpToPage(_currentIndex);
            }
            else
            {
                AnimateToPage(_currentIndex, duration, Curves.Ease);
            }

            if (Mounted)
            {
                SetState(UpdateChildren);
            }
        }

        private void WarpToNonAdjacentTab(TimeSpan duration)
        {
            int previousIndex = _controller!.PreviousIndex;
            int initialPage = _currentIndex > previousIndex ? _currentIndex - 1 : _currentIndex + 1;

            SetState(() =>
            {
                // Only the initial and final page are built; the pages in between are skipped by
                // temporarily swapping the child that sits at the staging index.
                Widget[] children = _childrenWithKey.ToArray();
                (children[initialPage], children[previousIndex]) =
                    (children[previousIndex], children[initialPage]);
                _childrenWithKey = children;
            });
            JumpToPage(initialPage);

            if (duration == TimeSpan.Zero)
            {
                JumpToPage(_currentIndex);
            }
            else
            {
                AnimateToPage(_currentIndex, duration, Curves.Ease);
            }

            if (Mounted)
            {
                SetState(UpdateChildren);
            }
        }

        private void SyncControllerOffset()
        {
            _controller!.Offset = Math.Clamp(
                (CurrentPage ?? _currentIndex) - _controller.Index,
                -1.0,
                1.0);
        }

        private bool HandleScrollNotification(ScrollNotification notification)
        {
            if (_warpUnderwayCount > 0 || _scrollUnderwayCount > 0)
            {
                return false;
            }

            if (notification.Depth != 0 || !ControllerIsValid)
            {
                return false;
            }

            _scrollUnderwayCount += 1;
            double page = CurrentPage ?? _currentIndex;
            if (notification is ScrollUpdateNotification && !_controller!.IndexIsChanging)
            {
                bool pageChanged = Math.Abs(page - _controller.Index) > 1.0;
                if (pageChanged)
                {
                    _controller.Index = (int)Math.Round(page);
                    _currentIndex = _controller.Index;
                }

                SyncControllerOffset();
            }
            else if (notification is ScrollEndNotification)
            {
                _controller!.Index = (int)Math.Round(page);
                _currentIndex = _controller.Index;
                if (!_controller.IndexIsChanging)
                {
                    SyncControllerOffset();
                }
            }

            _scrollUnderwayCount -= 1;
            return false;
        }
    }
}
