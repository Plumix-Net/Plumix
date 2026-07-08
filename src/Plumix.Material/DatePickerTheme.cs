using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/date_picker_theme.dart

public sealed record DatePickerThemeData(
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
    MaterialStateProperty<Color?>? DayForegroundColor = null,
    MaterialStateProperty<Color?>? DayBackgroundColor = null,
    MaterialStateProperty<Color?>? DayOverlayColor = null,
    MaterialStateProperty<ShapeBorder?>? DayShape = null,
    MaterialStateProperty<Color?>? TodayForegroundColor = null,
    MaterialStateProperty<Color?>? TodayBackgroundColor = null,
    BorderSide? TodayBorder = null,
    TextStyle? YearStyle = null,
    MaterialStateProperty<Color?>? YearForegroundColor = null,
    MaterialStateProperty<Color?>? YearBackgroundColor = null,
    MaterialStateProperty<Color?>? YearOverlayColor = null,
    MaterialStateProperty<ShapeBorder?>? YearShape = null,
    Color? DividerColor = null,
    InputDecorationThemeData? InputDecorationTheme = null,
    ButtonStyle? CancelButtonStyle = null,
    ButtonStyle? ConfirmButtonStyle = null,
    TextStyle? ToggleButtonTextStyle = null,
    Color? SubHeaderForegroundColor = null,
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
    MaterialStateProperty<Color?>? RangeSelectionOverlayColor = null);

public sealed class DatePickerTheme : InheritedWidget
{
    public DatePickerTheme(DatePickerThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public DatePickerThemeData Data { get; }
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(Data, ((DatePickerTheme)oldWidget).Data);

    public static DatePickerThemeData Of(BuildContext context) =>
        context.DependOnInherited<DatePickerTheme>()?.Data ?? Theme.Of(context).DatePickerTheme;

    internal static DatePickerThemeData Defaults(BuildContext context)
    {
        var theme = Theme.Of(context);
        var m3 = theme.UseMaterial3;
        var pressedOpacity = m3 ? 0.10 : 0.12;
        var dayStyle = m3 ? theme.TextTheme.BodyLarge : theme.TextTheme.BodySmall;
        var weekdayStyle = m3
            ? theme.TextTheme.BodyLarge.CopyWith(color: theme.OnSurfaceColor)
            : theme.TextTheme.BodySmall.CopyWith(color: ApplyOpacity(theme.OnSurfaceColor, 0.60));
        var yearStyle = theme.TextTheme.BodyLarge;

        MaterialStateProperty<Color?> foreground(bool year = false) => MaterialStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.HasFlag(MaterialState.Selected)) return theme.OnPrimaryColor;
            if (states.HasFlag(MaterialState.Disabled))
            {
                var disabled = year && m3 ? theme.OnSurfaceVariantColor : theme.OnSurfaceColor;
                return ApplyOpacity(disabled, 0.38);
            }
            return year && m3 ? theme.OnSurfaceVariantColor : theme.OnSurfaceColor;
        });

        MaterialStateProperty<Color?> background() => MaterialStateProperty<Color?>.ResolveWith(states =>
            states.HasFlag(MaterialState.Selected) ? theme.PrimaryColor : null);

        MaterialStateProperty<Color?> overlay() => MaterialStateProperty<Color?>.ResolveWith(states =>
        {
            var baseColor = states.HasFlag(MaterialState.Selected)
                ? theme.OnPrimaryColor
                : theme.OnSurfaceVariantColor;
            if (states.HasFlag(MaterialState.Pressed)) return ApplyOpacity(baseColor, pressedOpacity);
            if (states.HasFlag(MaterialState.Hovered)) return ApplyOpacity(baseColor, 0.08);
            if (states.HasFlag(MaterialState.Focused)) return ApplyOpacity(baseColor, pressedOpacity);
            return null;
        });

        return new DatePickerThemeData(
            BackgroundColor: m3 ? theme.SurfaceContainerHighColor : theme.SurfaceColor,
            Elevation: m3 ? 6 : 24,
            ShadowColor: m3 ? Colors.Transparent : theme.ShadowColor,
            SurfaceTintColor: Colors.Transparent,
            Shape: ShapeBorder.RoundedRectangle(m3 ? 28 : 4),
            HeaderBackgroundColor: m3 ? Colors.Transparent : theme.PrimaryColor,
            HeaderForegroundColor: m3 ? theme.OnSurfaceVariantColor : theme.OnPrimaryColor,
            HeaderHeadlineStyle: m3 ? theme.TextTheme.HeadlineMedium : theme.TextTheme.HeadlineSmall,
            HeaderHelpStyle: m3 ? theme.TextTheme.LabelLarge : theme.TextTheme.LabelSmall,
            WeekdayStyle: weekdayStyle,
            DayStyle: dayStyle,
            DayForegroundColor: foreground(),
            DayBackgroundColor: background(),
            DayOverlayColor: overlay(),
            DayShape: MaterialStateProperty<ShapeBorder?>.All(ShapeBorder.Circle()),
            TodayForegroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
            {
                if (states.HasFlag(MaterialState.Selected)) return theme.OnPrimaryColor;
                if (states.HasFlag(MaterialState.Disabled))
                {
                    return ApplyOpacity(m3 ? theme.PrimaryColor : theme.OnSurfaceColor, 0.38);
                }
                return theme.PrimaryColor;
            }),
            TodayBackgroundColor: background(),
            TodayBorder: new BorderSide(theme.PrimaryColor),
            YearStyle: yearStyle,
            YearForegroundColor: foreground(year: true),
            YearBackgroundColor: background(),
            YearOverlayColor: overlay(),
            YearShape: MaterialStateProperty<ShapeBorder?>.All(ShapeBorder.Stadium()),
            DividerColor: theme.DividerColor,
            ToggleButtonTextStyle: theme.TextTheme.TitleSmall.CopyWith(color: ApplyOpacity(theme.OnSurfaceColor, 0.60)),
            SubHeaderForegroundColor: ApplyOpacity(theme.OnSurfaceColor, 0.60),
            RangePickerBackgroundColor: m3 ? theme.SurfaceContainerLowColor : theme.SurfaceColor,
            RangePickerElevation: m3 ? 0 : 24,
            RangePickerShadowColor: Colors.Transparent,
            RangePickerSurfaceTintColor: Colors.Transparent,
            RangePickerShape: ShapeBorder.RoundedRectangle(0),
            RangePickerHeaderBackgroundColor: m3 ? theme.SurfaceContainerLowColor : theme.PrimaryColor,
            RangePickerHeaderForegroundColor: m3 ? theme.OnSurfaceColor : theme.OnPrimaryColor,
            RangePickerHeaderHeadlineStyle: m3 ? theme.TextTheme.TitleLarge : theme.TextTheme.HeadlineSmall,
            RangePickerHeaderHelpStyle: m3 ? theme.TextTheme.LabelLarge : theme.TextTheme.LabelSmall,
            RangeSelectionBackgroundColor: m3 ? theme.SecondaryContainerColor : ApplyOpacity(theme.PrimaryColor, 0.12),
            RangeSelectionOverlayColor: overlay());
    }

    private static Color ApplyOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Round(color.A * Math.Clamp(opacity, 0, 1)), color.R, color.G, color.B);
}
