using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/date_picker_theme.dart

public partial record DatePickerThemeData(
    Color? BackgroundColor = null,
    double? Elevation = null,
    Color? ShadowColor = null,
    Color? SurfaceTintColor = null,
    ShapeBorder? Shape = null,
    Color? HeaderBackgroundColor = null,
    Color? HeaderForegroundColor = null,
    TextStyle? HeaderHeadlineStyle = null,
    TextStyle? HeaderHelpStyle = null,
    TextStyle? WeekdayStyle = null,
    TextStyle? DayStyle = null,
    WidgetStateProperty<Color?>? DayForegroundColor = null,
    WidgetStateProperty<Color?>? DayBackgroundColor = null,
    WidgetStateProperty<Color?>? DayOverlayColor = null,
    WidgetStateProperty<OutlinedBorder?>? DayShape = null,
    WidgetStateProperty<Color?>? TodayForegroundColor = null,
    WidgetStateProperty<Color?>? TodayBackgroundColor = null,
    BorderSide? TodayBorder = null,
    TextStyle? YearStyle = null,
    WidgetStateProperty<Color?>? YearForegroundColor = null,
    WidgetStateProperty<Color?>? YearBackgroundColor = null,
    WidgetStateProperty<Color?>? YearOverlayColor = null,
    WidgetStateProperty<OutlinedBorder?>? YearShape = null,
    Color? RangePickerBackgroundColor = null,
    double? RangePickerElevation = null,
    Color? RangePickerShadowColor = null,
    Color? RangePickerSurfaceTintColor = null,
    ShapeBorder? RangePickerShape = null,
    Color? RangePickerHeaderBackgroundColor = null,
    Color? RangePickerHeaderForegroundColor = null,
    TextStyle? RangePickerHeaderHeadlineStyle = null,
    TextStyle? RangePickerHeaderHelpStyle = null,
    Color? RangeSelectionBackgroundColor = null,
    WidgetStateProperty<Color?>? RangeSelectionOverlayColor = null,
    Color? DividerColor = null,
    InputDecorationThemeData? InputDecorationTheme = null,
    ButtonStyle? CancelButtonStyle = null,
    ButtonStyle? ConfirmButtonStyle = null,
    Locale? Locale = null,
    TextStyle? ToggleButtonTextStyle = null,
    Color? SubHeaderForegroundColor = null)
{
    public DatePickerThemeData CopyWith(
        Color? backgroundColor = null,
        double? elevation = null,
        Color? shadowColor = null,
        Color? surfaceTintColor = null,
        ShapeBorder? shape = null,
        Color? headerBackgroundColor = null,
        Color? headerForegroundColor = null,
        TextStyle? headerHeadlineStyle = null,
        TextStyle? headerHelpStyle = null,
        TextStyle? weekdayStyle = null,
        TextStyle? dayStyle = null,
        WidgetStateProperty<Color?>? dayForegroundColor = null,
        WidgetStateProperty<Color?>? dayBackgroundColor = null,
        WidgetStateProperty<Color?>? dayOverlayColor = null,
        WidgetStateProperty<OutlinedBorder?>? dayShape = null,
        WidgetStateProperty<Color?>? todayForegroundColor = null,
        WidgetStateProperty<Color?>? todayBackgroundColor = null,
        BorderSide? todayBorder = null,
        TextStyle? yearStyle = null,
        WidgetStateProperty<Color?>? yearForegroundColor = null,
        WidgetStateProperty<Color?>? yearBackgroundColor = null,
        WidgetStateProperty<Color?>? yearOverlayColor = null,
        WidgetStateProperty<OutlinedBorder?>? yearShape = null,
        Color? rangePickerBackgroundColor = null,
        double? rangePickerElevation = null,
        Color? rangePickerShadowColor = null,
        Color? rangePickerSurfaceTintColor = null,
        ShapeBorder? rangePickerShape = null,
        Color? rangePickerHeaderBackgroundColor = null,
        Color? rangePickerHeaderForegroundColor = null,
        TextStyle? rangePickerHeaderHeadlineStyle = null,
        TextStyle? rangePickerHeaderHelpStyle = null,
        Color? rangeSelectionBackgroundColor = null,
        WidgetStateProperty<Color?>? rangeSelectionOverlayColor = null,
        Color? dividerColor = null,
        InputDecorationThemeData? inputDecorationTheme = null,
        ButtonStyle? cancelButtonStyle = null,
        ButtonStyle? confirmButtonStyle = null,
        Locale? locale = null,
        TextStyle? toggleButtonTextStyle = null,
        Color? subHeaderForegroundColor = null)
    {
        return new DatePickerThemeData(
            BackgroundColor: backgroundColor ?? BackgroundColor,
            Elevation: elevation ?? Elevation,
            ShadowColor: shadowColor ?? ShadowColor,
            SurfaceTintColor: surfaceTintColor ?? SurfaceTintColor,
            Shape: shape ?? Shape,
            HeaderBackgroundColor: headerBackgroundColor ?? HeaderBackgroundColor,
            HeaderForegroundColor: headerForegroundColor ?? HeaderForegroundColor,
            HeaderHeadlineStyle: headerHeadlineStyle ?? HeaderHeadlineStyle,
            HeaderHelpStyle: headerHelpStyle ?? HeaderHelpStyle,
            WeekdayStyle: weekdayStyle ?? WeekdayStyle,
            DayStyle: dayStyle ?? DayStyle,
            DayForegroundColor: dayForegroundColor ?? DayForegroundColor,
            DayBackgroundColor: dayBackgroundColor ?? DayBackgroundColor,
            DayOverlayColor: dayOverlayColor ?? DayOverlayColor,
            DayShape: dayShape ?? DayShape,
            TodayForegroundColor: todayForegroundColor ?? TodayForegroundColor,
            TodayBackgroundColor: todayBackgroundColor ?? TodayBackgroundColor,
            TodayBorder: todayBorder ?? TodayBorder,
            YearStyle: yearStyle ?? YearStyle,
            YearForegroundColor: yearForegroundColor ?? YearForegroundColor,
            YearBackgroundColor: yearBackgroundColor ?? YearBackgroundColor,
            YearOverlayColor: yearOverlayColor ?? YearOverlayColor,
            YearShape: yearShape ?? YearShape,
            RangePickerBackgroundColor: rangePickerBackgroundColor ?? RangePickerBackgroundColor,
            RangePickerElevation: rangePickerElevation ?? RangePickerElevation,
            RangePickerShadowColor: rangePickerShadowColor ?? RangePickerShadowColor,
            RangePickerSurfaceTintColor: rangePickerSurfaceTintColor ?? RangePickerSurfaceTintColor,
            RangePickerShape: rangePickerShape ?? RangePickerShape,
            RangePickerHeaderBackgroundColor:
                rangePickerHeaderBackgroundColor ?? RangePickerHeaderBackgroundColor,
            RangePickerHeaderForegroundColor:
                rangePickerHeaderForegroundColor ?? RangePickerHeaderForegroundColor,
            RangePickerHeaderHeadlineStyle: rangePickerHeaderHeadlineStyle ?? RangePickerHeaderHeadlineStyle,
            RangePickerHeaderHelpStyle: rangePickerHeaderHelpStyle ?? RangePickerHeaderHelpStyle,
            RangeSelectionBackgroundColor: rangeSelectionBackgroundColor ?? RangeSelectionBackgroundColor,
            RangeSelectionOverlayColor: rangeSelectionOverlayColor ?? RangeSelectionOverlayColor,
            DividerColor: dividerColor ?? DividerColor,
            InputDecorationTheme: inputDecorationTheme ?? InputDecorationTheme,
            CancelButtonStyle: cancelButtonStyle ?? CancelButtonStyle,
            ConfirmButtonStyle: confirmButtonStyle ?? ConfirmButtonStyle,
            Locale: locale ?? Locale,
            ToggleButtonTextStyle: toggleButtonTextStyle ?? ToggleButtonTextStyle,
            SubHeaderForegroundColor: subHeaderForegroundColor ?? SubHeaderForegroundColor);
    }
}

public sealed class DatePickerTheme : InheritedTheme
{
    public DatePickerTheme(DatePickerThemeData data, Widget child, Key? key = null) : base(child, key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    public DatePickerThemeData Data { get; }

    public override Widget Wrap(BuildContext context, Widget child) => new DatePickerTheme(Data, child);

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(Data, ((DatePickerTheme)oldWidget).Data);

    public static DatePickerThemeData Of(BuildContext context) =>
        MaybeOf(context) ?? Theme.Of(context).DatePickerTheme;

    public static DatePickerThemeData? MaybeOf(BuildContext context) =>
        context.DependOnInherited<DatePickerTheme>()?.Data;

    public static DatePickerThemeData Defaults(BuildContext context) => Theme.Of(context).UseMaterial3
        ? new DatePickerDefaultsM3(context)
        : new DatePickerDefaultsM2(context);
}

internal sealed record DatePickerDefaultsM2 : DatePickerThemeData
{
    public DatePickerDefaultsM2(BuildContext context) : this(
        Theme.Of(context).ColorScheme,
        Theme.Of(context).TextTheme)
    {
    }

    private DatePickerDefaultsM2(ColorScheme colors, TextTheme textTheme) : this(
        colors,
        textTheme,
        DatePickerDefaultUtilities.ResolveDayBackground(colors),
        ResolveDayOverlay(colors))
    {
    }

    private DatePickerDefaultsM2(
        ColorScheme colors,
        TextTheme textTheme,
        WidgetStateProperty<Color?> dayBackground,
        WidgetStateProperty<Color?> dayOverlay) : base(
        Elevation: 24.0,
        Shape: new RoundedRectangleBorder(
            borderRadius: Plumix.Rendering.BorderRadius.Circular(4.0)),
        HeaderBackgroundColor: colors.Brightness == Brightness.Dark ? colors.Surface : colors.Primary,
        HeaderForegroundColor: colors.Brightness == Brightness.Dark ? colors.OnSurface : colors.OnPrimary,
        HeaderHeadlineStyle: textTheme.HeadlineSmall,
        HeaderHelpStyle: textTheme.LabelSmall,
        WeekdayStyle: textTheme.BodySmall.CopyWith(
            color: DatePickerDefaultUtilities.WithOpacity(colors.OnSurface, 0.60)),
        DayStyle: textTheme.BodySmall,
        DayForegroundColor: DatePickerDefaultUtilities.ResolveDayForeground(colors),
        DayBackgroundColor: dayBackground,
        DayOverlayColor: dayOverlay,
        DayShape: WidgetStateProperty<OutlinedBorder?>.All(new CircleBorder()),
        TodayForegroundColor: DatePickerDefaultUtilities.ResolveTodayForeground(colors, material3: false),
        TodayBackgroundColor: dayBackground,
        TodayBorder: new BorderSide(colors.Primary),
        YearStyle: textTheme.BodyLarge,
        YearShape: WidgetStateProperty<OutlinedBorder?>.All(new StadiumBorder()),
        RangePickerBackgroundColor: colors.Surface,
        RangePickerElevation: 0.0,
        RangePickerShadowColor: Colors.Transparent,
        RangePickerSurfaceTintColor: Colors.Transparent,
        RangePickerShape: new RoundedRectangleBorder(),
        RangePickerHeaderBackgroundColor:
            colors.Brightness == Brightness.Dark ? colors.Surface : colors.Primary,
        RangePickerHeaderForegroundColor:
            colors.Brightness == Brightness.Dark ? colors.OnSurface : colors.OnPrimary,
        RangePickerHeaderHeadlineStyle: textTheme.HeadlineSmall,
        RangePickerHeaderHelpStyle: textTheme.LabelSmall,
        RangeSelectionBackgroundColor: DatePickerDefaultUtilities.WithOpacity(colors.Primary, 0.12),
        RangeSelectionOverlayColor: dayOverlay,
        CancelButtonStyle: TextButton.StyleFrom(),
        ConfirmButtonStyle: TextButton.StyleFrom(),
        ToggleButtonTextStyle: DatePickerDefaultUtilities.ResolveToggleStyle(colors, textTheme),
        SubHeaderForegroundColor: DatePickerDefaultUtilities.WithOpacity(colors.OnSurface, 0.60))
    {
    }

    private static WidgetStateProperty<Color?> ResolveDayOverlay(ColorScheme colors) =>
        DatePickerDefaultUtilities.ResolveOverlay(
            colors,
            selectedPressedOpacity: 0.38,
            selectedFocusedOpacity: 0.12,
            unselectedPressedOpacity: 0.12,
            unselectedFocusedOpacity: 0.12);
}

internal sealed record DatePickerDefaultsM3 : DatePickerThemeData
{
    public DatePickerDefaultsM3(BuildContext context) : this(
        Theme.Of(context).ColorScheme,
        Theme.Of(context).TextTheme)
    {
    }

    private DatePickerDefaultsM3(ColorScheme colors, TextTheme textTheme) : this(
        colors,
        textTheme,
        DatePickerDefaultUtilities.ResolveDayBackground(colors),
        ResolveDayOverlay(colors))
    {
    }

    private DatePickerDefaultsM3(
        ColorScheme colors,
        TextTheme textTheme,
        WidgetStateProperty<Color?> dayBackground,
        WidgetStateProperty<Color?> dayOverlay) : base(
        BackgroundColor: colors.SurfaceContainerHigh,
        Elevation: 6.0,
        ShadowColor: Colors.Transparent,
        SurfaceTintColor: Colors.Transparent,
        Shape: new RoundedRectangleBorder(
            borderRadius: Plumix.Rendering.BorderRadius.Circular(28.0)),
        HeaderBackgroundColor: Colors.Transparent,
        HeaderForegroundColor: colors.OnSurfaceVariant,
        HeaderHeadlineStyle: textTheme.HeadlineLarge,
        HeaderHelpStyle: textTheme.LabelLarge,
        WeekdayStyle: textTheme.BodyLarge.CopyWith(color: colors.OnSurface),
        DayStyle: textTheme.BodyLarge,
        DayForegroundColor: DatePickerDefaultUtilities.ResolveDayForeground(colors),
        DayBackgroundColor: dayBackground,
        DayOverlayColor: dayOverlay,
        DayShape: WidgetStateProperty<OutlinedBorder?>.All(new CircleBorder()),
        TodayForegroundColor: DatePickerDefaultUtilities.ResolveTodayForeground(colors, material3: true),
        TodayBackgroundColor: dayBackground,
        TodayBorder: new BorderSide(colors.Primary),
        YearStyle: textTheme.BodyLarge,
        YearForegroundColor: ResolveYearForeground(colors),
        YearBackgroundColor: dayBackground,
        YearOverlayColor: dayOverlay,
        YearShape: WidgetStateProperty<OutlinedBorder?>.All(new StadiumBorder()),
        RangePickerElevation: 0.0,
        RangePickerShadowColor: Colors.Transparent,
        RangePickerSurfaceTintColor: Colors.Transparent,
        RangePickerShape: new RoundedRectangleBorder(),
        RangePickerHeaderBackgroundColor: Colors.Transparent,
        RangePickerHeaderForegroundColor: colors.OnSurfaceVariant,
        RangePickerHeaderHeadlineStyle: textTheme.TitleLarge,
        RangePickerHeaderHelpStyle: textTheme.TitleSmall,
        RangeSelectionBackgroundColor: colors.SecondaryContainer,
        RangeSelectionOverlayColor: ResolveRangeOverlay(colors),
        CancelButtonStyle: TextButton.StyleFrom(),
        ConfirmButtonStyle: TextButton.StyleFrom(),
        ToggleButtonTextStyle: DatePickerDefaultUtilities.ResolveToggleStyle(colors, textTheme),
        SubHeaderForegroundColor: DatePickerDefaultUtilities.WithOpacity(colors.OnSurface, 0.60))
    {
    }

    private static WidgetStateProperty<Color?> ResolveDayOverlay(ColorScheme colors) =>
        DatePickerDefaultUtilities.ResolveOverlay(
            colors,
            selectedPressedOpacity: 0.10,
            selectedFocusedOpacity: 0.10,
            unselectedPressedOpacity: 0.10,
            unselectedFocusedOpacity: 0.10);

    private static WidgetStateProperty<Color?> ResolveYearForeground(ColorScheme colors) =>
        WidgetStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.Contains(WidgetState.Selected)) return colors.OnPrimary;
            if (states.Contains(WidgetState.Disabled))
            {
                return DatePickerDefaultUtilities.WithOpacity(colors.OnSurfaceVariant, 0.38);
            }

            return colors.OnSurfaceVariant;
        });

    private static WidgetStateProperty<Color?> ResolveRangeOverlay(ColorScheme colors) =>
        WidgetStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.Contains(WidgetState.Pressed))
            {
                return DatePickerDefaultUtilities.WithOpacity(colors.OnPrimaryContainer, 0.10);
            }

            if (states.Contains(WidgetState.Hovered))
            {
                return DatePickerDefaultUtilities.WithOpacity(colors.OnPrimaryContainer, 0.08);
            }

            if (states.Contains(WidgetState.Focused))
            {
                return DatePickerDefaultUtilities.WithOpacity(colors.OnPrimaryContainer, 0.10);
            }
            return null;
        });
}

internal static class DatePickerDefaultUtilities
{
    public static WidgetStateProperty<Color?> ResolveDayForeground(ColorScheme colors) =>
        WidgetStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.Contains(WidgetState.Selected)) return colors.OnPrimary;
            if (states.Contains(WidgetState.Disabled)) return WithOpacity(colors.OnSurface, 0.38);
            return colors.OnSurface;
        });

    public static WidgetStateProperty<Color?> ResolveDayBackground(ColorScheme colors) =>
        WidgetStateProperty<Color?>.ResolveWith(states =>
            states.Contains(WidgetState.Selected) ? colors.Primary : null);

    public static WidgetStateProperty<Color?> ResolveTodayForeground(
        ColorScheme colors,
        bool material3) => WidgetStateProperty<Color?>.ResolveWith(states =>
    {
        if (states.Contains(WidgetState.Selected)) return colors.OnPrimary;
        if (states.Contains(WidgetState.Disabled))
        {
            return WithOpacity(material3 ? colors.Primary : colors.OnSurface, 0.38);
        }

        return colors.Primary;
    });

    public static WidgetStateProperty<Color?> ResolveOverlay(
        ColorScheme colors,
        double selectedPressedOpacity,
        double selectedFocusedOpacity,
        double unselectedPressedOpacity,
        double unselectedFocusedOpacity) => WidgetStateProperty<Color?>.ResolveWith(states =>
    {
        bool selected = states.Contains(WidgetState.Selected);
        Color baseColor = selected ? colors.OnPrimary : colors.OnSurfaceVariant;
        if (states.Contains(WidgetState.Pressed))
        {
            return WithOpacity(baseColor, selected ? selectedPressedOpacity : unselectedPressedOpacity);
        }

        if (states.Contains(WidgetState.Hovered)) return WithOpacity(baseColor, 0.08);
        if (states.Contains(WidgetState.Focused))
        {
            return WithOpacity(baseColor, selected ? selectedFocusedOpacity : unselectedFocusedOpacity);
        }

        return null;
    });

    public static TextStyle ResolveToggleStyle(ColorScheme colors, TextTheme textTheme) =>
        textTheme.TitleSmall.CopyWith(color: WithOpacity(colors.OnSurface, 0.60));

    public static Color WithOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Round(255 * Math.Clamp(opacity, 0.0, 1.0)),
        color.R,
        color.G,
        color.B);
}
