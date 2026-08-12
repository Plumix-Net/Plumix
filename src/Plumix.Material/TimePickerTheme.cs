using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/time_picker_theme.dart

public sealed partial record TimePickerThemeData(
    Color? BackgroundColor = null,
    ButtonStyle? CancelButtonStyle = null,
    ButtonStyle? ConfirmButtonStyle = null,
    BorderSide? DayPeriodBorderSide = null,
    MaterialStateProperty<Color?>? DayPeriodColor = null,
    ShapeBorder? DayPeriodShape = null,
    MaterialStateProperty<Color?>? DayPeriodTextColor = null,
    TextStyle? DayPeriodTextStyle = null,
    Color? DialBackgroundColor = null,
    Color? DialHandColor = null,
    MaterialStateProperty<Color?>? DialTextColor = null,
    TextStyle? DialTextStyle = null,
    double? Elevation = null,
    Color? EntryModeIconColor = null,
    TextStyle? HelpTextStyle = null,
    MaterialStateProperty<Color?>? HourMinuteColor = null,
    ShapeBorder? HourMinuteShape = null,
    MaterialStateProperty<Color?>? HourMinuteTextColor = null,
    TextStyle? HourMinuteTextStyle = null,
    InputDecorationThemeData? InputDecorationTheme = null,
    Thickness? Padding = null,
    ShapeBorder? Shape = null,
    MaterialStateProperty<Color?>? TimeSelectorSeparatorColor = null,
    MaterialStateProperty<TextStyle?>? TimeSelectorSeparatorTextStyle = null);

public sealed class TimePickerTheme : InheritedWidget
{
    public TimePickerTheme(TimePickerThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public TimePickerThemeData Data { get; }
    public Widget Child { get; }
    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(Data, ((TimePickerTheme)oldWidget).Data);

    public static TimePickerThemeData Of(BuildContext context) =>
        context.DependOnInherited<TimePickerTheme>()?.Data ?? Theme.Of(context).TimePickerTheme;

    internal static TimePickerThemeData Defaults(BuildContext context)
    {
        var theme = Theme.Of(context);
        bool m3 = theme.UseMaterial3;
        var selectedFill = m3
            ? theme.PrimaryContainerColor
            : WithOpacity(theme.PrimaryColor, theme.Brightness == Brightness.Dark ? 0.24 : 0.12);
        var idleFill = m3 ? theme.SurfaceContainerHighestColor : WithOpacity(theme.OnSurfaceColor, 0.12);

        MaterialStateProperty<Color?> stateColor(Color selected, Color normal) =>
            MaterialStateProperty<Color?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Selected) ? selected : normal);

        return new TimePickerThemeData(
            BackgroundColor: m3 ? theme.SurfaceContainerHighColor : theme.SurfaceColor,
            CancelButtonStyle: TextButton.StyleFrom(foregroundColor: theme.PrimaryColor),
            ConfirmButtonStyle: TextButton.StyleFrom(foregroundColor: theme.PrimaryColor),
            DayPeriodBorderSide: new BorderSide(m3 ? theme.OutlineColor : WithOpacity(theme.OnSurfaceColor, 0.38)),
            DayPeriodColor: stateColor(selectedFill, Colors.Transparent),
            DayPeriodShape: new RoundedRectangleBorder(borderRadius:
                Plumix.Rendering.BorderRadius.Circular(m3 ? 8 : 4)),
            DayPeriodTextColor: stateColor(m3 ? theme.OnPrimaryContainerColor : theme.PrimaryColor, theme.OnSurfaceColor),
            DayPeriodTextStyle: theme.TextTheme.TitleMedium,
            DialBackgroundColor: m3 ? theme.SurfaceContainerHighestColor : WithOpacity(theme.OnSurfaceColor, theme.Brightness == Brightness.Dark ? 0.12 : 0.08),
            DialHandColor: theme.PrimaryColor,
            DialTextColor: stateColor(theme.OnPrimaryColor, theme.OnSurfaceColor),
            DialTextStyle: theme.TextTheme.BodyLarge,
            Elevation: m3 ? 6 : 24,
            EntryModeIconColor: m3 ? theme.OnSurfaceVariantColor : WithOpacity(theme.OnSurfaceColor, theme.Brightness == Brightness.Dark ? 1 : 0.60),
            HelpTextStyle: m3 ? theme.TextTheme.LabelSmall : theme.TextTheme.OverlineFallback(),
            HourMinuteColor: stateColor(selectedFill, idleFill),
            HourMinuteShape: new RoundedRectangleBorder(borderRadius:
                Plumix.Rendering.BorderRadius.Circular(m3 ? 8 : 4)),
            HourMinuteTextColor: stateColor(m3 ? theme.OnPrimaryContainerColor : theme.PrimaryColor, theme.OnSurfaceColor),
            HourMinuteTextStyle: theme.TextTheme.HeadlineMedium.CopyWith(fontSize: m3 ? 64 : 60),
            InputDecorationTheme: new InputDecorationThemeData(
                Filled: true,
                Border: new OutlineInputBorder(borderRadius: BorderRadius.Circular(m3 ? 8 : 4))),
            Padding: m3 ? new Thickness(24) : new Thickness(24, 20),
            Shape: new RoundedRectangleBorder(borderRadius: Plumix.Rendering.BorderRadius.Circular(m3 ? 28 : 4)),
            TimeSelectorSeparatorColor: MaterialStateProperty<Color?>.All(theme.OnSurfaceColor),
            TimeSelectorSeparatorTextStyle: MaterialStateProperty<TextStyle?>.All(
                theme.TextTheme.HeadlineMedium.CopyWith(fontSize: m3 ? 64 : 60)));
    }

    private static Color WithOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Round(color.A * Math.Clamp(opacity, 0, 1)), color.R, color.G, color.B);
}

internal static class TimePickerTextThemeExtensions
{
    public static TextStyle OverlineFallback(this TextTheme theme) =>
        theme.LabelSmall.CopyWith(letterSpacing: 1.5);
}
