using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/date_picker_theme.dart

/// <summary>
/// Overrides the default values of visual properties for descendant <see cref="DatePickerDialog"/>
/// widgets.
/// </summary>
/// <remarks>
/// Descendant widgets obtain the current <see cref="DatePickerThemeData"/> object with
/// <see cref="DatePickerTheme.Of"/>. Instances of <see cref="DatePickerThemeData"/> can be customized
/// with <see cref="CopyWith"/>. Typically a <see cref="DatePickerTheme"/> is specified as part of the
/// overall <see cref="Theme"/> with <see cref="ThemeData.DatePickerTheme"/>.
/// <para>
/// All properties are null by default. When null, the <see cref="DatePickerDialog"/> computes its own
/// default values, typically based on the overall theme's <see cref="ThemeData.ColorScheme"/>,
/// <see cref="ThemeData.TextTheme"/>, and <see cref="ThemeData.IconTheme"/>.
/// </para>
/// <para>
/// Flutter declares an ordinary class that <c>_DatePickerDefaultsM2</c>/<c>_DatePickerDefaultsM3</c>
/// extend to override individual getters, so the members are <c>virtual</c>. Dart's <c>==</c> has no
/// runtime type check, so a defaults subclass equals a plain instance with the same getter values.
/// </para>
/// </remarks>
public class DatePickerThemeData : IDiagnosticable
{
    /// <summary>
    /// Creates a <see cref="DatePickerThemeData"/> that can be used to override default properties in a
    /// <see cref="DatePickerTheme"/> widget.
    /// </summary>
    /// <param name="inputDecorationTheme">
    /// Dart's <c>Object? inputDecorationTheme</c>: an <see cref="Plumix.Material.InputDecorationTheme"/> widget, whose
    /// <c>Data</c> is used, or an <see cref="InputDecorationThemeData"/>.
    /// </param>
    public DatePickerThemeData(
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
        object? inputDecorationTheme = null,
        ButtonStyle? cancelButtonStyle = null,
        ButtonStyle? confirmButtonStyle = null,
        Locale? locale = null,
        TextStyle? toggleButtonTextStyle = null,
        Color? subHeaderForegroundColor = null)
    {
        // TODO(bleroux): Clean this up once `InputDecorationTheme` is fully normalized.
        DebugAssertions.Assert(
            inputDecorationTheme == null
            || inputDecorationTheme is Plumix.Material.InputDecorationTheme
            || inputDecorationTheme is InputDecorationThemeData);
        BackgroundColor = backgroundColor;
        Elevation = elevation;
        ShadowColor = shadowColor;
        SurfaceTintColor = surfaceTintColor;
        Shape = shape;
        HeaderBackgroundColor = headerBackgroundColor;
        HeaderForegroundColor = headerForegroundColor;
        HeaderHeadlineStyle = headerHeadlineStyle;
        HeaderHelpStyle = headerHelpStyle;
        WeekdayStyle = weekdayStyle;
        DayStyle = dayStyle;
        DayForegroundColor = dayForegroundColor;
        DayBackgroundColor = dayBackgroundColor;
        DayOverlayColor = dayOverlayColor;
        DayShape = dayShape;
        TodayForegroundColor = todayForegroundColor;
        TodayBackgroundColor = todayBackgroundColor;
        TodayBorder = todayBorder;
        YearStyle = yearStyle;
        YearForegroundColor = yearForegroundColor;
        YearBackgroundColor = yearBackgroundColor;
        YearOverlayColor = yearOverlayColor;
        YearShape = yearShape;
        RangePickerBackgroundColor = rangePickerBackgroundColor;
        RangePickerElevation = rangePickerElevation;
        RangePickerShadowColor = rangePickerShadowColor;
        RangePickerSurfaceTintColor = rangePickerSurfaceTintColor;
        RangePickerShape = rangePickerShape;
        RangePickerHeaderBackgroundColor = rangePickerHeaderBackgroundColor;
        RangePickerHeaderForegroundColor = rangePickerHeaderForegroundColor;
        RangePickerHeaderHeadlineStyle = rangePickerHeaderHeadlineStyle;
        RangePickerHeaderHelpStyle = rangePickerHeaderHelpStyle;
        RangeSelectionBackgroundColor = rangeSelectionBackgroundColor;
        RangeSelectionOverlayColor = rangeSelectionOverlayColor;
        DividerColor = dividerColor;
        _inputDecorationTheme = inputDecorationTheme;
        CancelButtonStyle = cancelButtonStyle;
        ConfirmButtonStyle = confirmButtonStyle;
        Locale = locale;
        ToggleButtonTextStyle = toggleButtonTextStyle;
        SubHeaderForegroundColor = subHeaderForegroundColor;
    }

    /// <summary>Overrides the default value of [Dialog.BackgroundColor].</summary>
    public virtual Color? BackgroundColor { get; }

    /// <summary>Overrides the default value of [Dialog.Elevation].</summary>
    public virtual double? Elevation { get; }

    /// <summary>Overrides the default value of [Dialog.ShadowColor].</summary>
    public virtual Color? ShadowColor { get; }

    /// <summary>Overrides the default value of [Dialog.SurfaceTintColor].</summary>
    public virtual Color? SurfaceTintColor { get; }

    /// <summary>Overrides the default value of [Dialog.Shape].</summary>
    public virtual ShapeBorder? Shape { get; }

    /// <summary>Overrides the header's default background fill color.</summary>
    public virtual Color? HeaderBackgroundColor { get; }

    /// <summary>Overrides the header's default color used for text labels and icons.</summary>
    public virtual Color? HeaderForegroundColor { get; }

    /// <summary>Overrides the header's default headline text style.</summary>
    public virtual TextStyle? HeaderHeadlineStyle { get; }

    /// <summary>Overrides the header's default help text style.</summary>
    public virtual TextStyle? HeaderHelpStyle { get; }

    /// <summary>Overrides the default text style used for the row of weekday labels.</summary>
    public virtual TextStyle? WeekdayStyle { get; }

    /// <summary>Overrides the default text style used for each individual day label.</summary>
    public virtual TextStyle? DayStyle { get; }

    /// <summary>Overrides the default color used to paint the day labels.</summary>
    public virtual WidgetStateProperty<Color?>? DayForegroundColor { get; }

    /// <summary>Overrides the default color used to paint the background of the day labels.</summary>
    public virtual WidgetStateProperty<Color?>? DayBackgroundColor { get; }

    /// <summary>Overrides the default highlight color of a focused, hovered, or pressed day.</summary>
    public virtual WidgetStateProperty<Color?>? DayOverlayColor { get; }

    /// <summary>Overrides the default shape used to paint the shape decoration of the day labels.</summary>
    public virtual WidgetStateProperty<OutlinedBorder?>? DayShape { get; }

    /// <summary>Overrides the default color used to paint the current date label.</summary>
    public virtual WidgetStateProperty<Color?>? TodayForegroundColor { get; }

    /// <summary>Overrides the default color used to paint the background of the current date label.</summary>
    public virtual WidgetStateProperty<Color?>? TodayBackgroundColor { get; }

    /// <summary>Overrides the border used to paint the current date label.</summary>
    public virtual BorderSide? TodayBorder { get; }

    /// <summary>Overrides the default text style used to paint each of the year entries.</summary>
    public virtual TextStyle? YearStyle { get; }

    /// <summary>Overrides the default color used to paint the year labels.</summary>
    public virtual WidgetStateProperty<Color?>? YearForegroundColor { get; }

    /// <summary>Overrides the default color used to paint the background of the year labels.</summary>
    public virtual WidgetStateProperty<Color?>? YearBackgroundColor { get; }

    /// <summary>Overrides the default highlight color of a focused, hovered, or pressed year.</summary>
    public virtual WidgetStateProperty<Color?>? YearOverlayColor { get; }

    /// <summary>Overrides the default shape used to paint the shape decoration of the year labels.</summary>
    public virtual WidgetStateProperty<OutlinedBorder?>? YearShape { get; }

    /// <summary>Overrides the default [Scaffold.BackgroundColor] for [DateRangePickerDialog].</summary>
    public virtual Color? RangePickerBackgroundColor { get; }

    /// <summary>Overrides the default elevation of the full screen [DateRangePickerDialog].</summary>
    public virtual double? RangePickerElevation { get; }

    /// <summary>Overrides the color of the shadow painted below a full screen [DateRangePickerDialog].</summary>
    public virtual Color? RangePickerShadowColor { get; }

    /// <summary>Overrides the default surface tint of a full screen [DateRangePickerDialog].</summary>
    public virtual Color? RangePickerSurfaceTintColor { get; }

    /// <summary>Overrides the default overall shape of a full screen [DateRangePickerDialog].</summary>
    public virtual ShapeBorder? RangePickerShape { get; }

    /// <summary>Overrides the default background fill color for [DateRangePickerDialog].</summary>
    public virtual Color? RangePickerHeaderBackgroundColor { get; }

    /// <summary>Overrides the default header text and icon color of a full screen [DateRangePickerDialog].</summary>
    public virtual Color? RangePickerHeaderForegroundColor { get; }

    /// <summary>Overrides the default header headline style of a full screen [DateRangePickerDialog].</summary>
    public virtual TextStyle? RangePickerHeaderHeadlineStyle { get; }

    /// <summary>Overrides the default header help text style of a full screen [DateRangePickerDialog].</summary>
    public virtual TextStyle? RangePickerHeaderHelpStyle { get; }

    /// <summary>Overrides the default background color used to paint days in the selected range.</summary>
    public virtual Color? RangeSelectionBackgroundColor { get; }

    /// <summary>Overrides the default highlight color of a focused, hovered, or pressed day within the range.</summary>
    public virtual WidgetStateProperty<Color?>? RangeSelectionOverlayColor { get; }

    /// <summary>Overrides the default color used to paint the divider between the header and the body.</summary>
    public virtual Color? DividerColor { get; }

    // TODO(bleroux): Clean this up once `InputDecorationTheme` is fully normalized.
    /// <summary>Overrides the [InputDatePickerFormField]'s input decoration theme.</summary>
    public virtual InputDecorationThemeData? InputDecorationTheme
    {
        get
        {
            if (_inputDecorationTheme == null)
            {
                return null;
            }

            return _inputDecorationTheme is Plumix.Material.InputDecorationTheme theme
                ? theme.Data
                : (InputDecorationThemeData)_inputDecorationTheme;
        }
    }

    private readonly object? _inputDecorationTheme;

    /// <summary>Overrides the default style of the cancel button of a [DatePickerDialog].</summary>
    public virtual ButtonStyle? CancelButtonStyle { get; }

    /// <summary>Overrides the default style of the confirm (OK) button of a [DatePickerDialog].</summary>
    public virtual ButtonStyle? ConfirmButtonStyle { get; }

    /// <summary>An optional locale argument can be used to set the locale for the date picker.</summary>
    public virtual Locale? Locale { get; }

    /// <summary>Overrides the default text style used for the text of the toggle button.</summary>
    public virtual TextStyle? ToggleButtonTextStyle { get; }

    /// <summary>Overrides the default color used for text labels and icons of the sub header.</summary>
    public virtual Color? SubHeaderForegroundColor { get; }

    /// <summary>
    /// Creates a copy of this object with the given fields replaced with the new values.
    /// </summary>
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
        Plumix.Material.InputDecorationTheme? inputDecorationTheme = null,
        ButtonStyle? cancelButtonStyle = null,
        ButtonStyle? confirmButtonStyle = null,
        Locale? locale = null,
        TextStyle? toggleButtonTextStyle = null,
        Color? subHeaderForegroundColor = null)
    {
        return new DatePickerThemeData(
            backgroundColor: backgroundColor ?? BackgroundColor,
            elevation: elevation ?? Elevation,
            shadowColor: shadowColor ?? ShadowColor,
            surfaceTintColor: surfaceTintColor ?? SurfaceTintColor,
            shape: shape ?? Shape,
            headerBackgroundColor: headerBackgroundColor ?? HeaderBackgroundColor,
            headerForegroundColor: headerForegroundColor ?? HeaderForegroundColor,
            headerHeadlineStyle: headerHeadlineStyle ?? HeaderHeadlineStyle,
            headerHelpStyle: headerHelpStyle ?? HeaderHelpStyle,
            weekdayStyle: weekdayStyle ?? WeekdayStyle,
            dayStyle: dayStyle ?? DayStyle,
            dayForegroundColor: dayForegroundColor ?? DayForegroundColor,
            dayBackgroundColor: dayBackgroundColor ?? DayBackgroundColor,
            dayOverlayColor: dayOverlayColor ?? DayOverlayColor,
            dayShape: dayShape ?? DayShape,
            todayForegroundColor: todayForegroundColor ?? TodayForegroundColor,
            todayBackgroundColor: todayBackgroundColor ?? TodayBackgroundColor,
            todayBorder: todayBorder ?? TodayBorder,
            yearStyle: yearStyle ?? YearStyle,
            yearForegroundColor: yearForegroundColor ?? YearForegroundColor,
            yearBackgroundColor: yearBackgroundColor ?? YearBackgroundColor,
            yearOverlayColor: yearOverlayColor ?? YearOverlayColor,
            yearShape: yearShape ?? YearShape,
            rangePickerBackgroundColor: rangePickerBackgroundColor ?? RangePickerBackgroundColor,
            rangePickerElevation: rangePickerElevation ?? RangePickerElevation,
            rangePickerShadowColor: rangePickerShadowColor ?? RangePickerShadowColor,
            rangePickerSurfaceTintColor: rangePickerSurfaceTintColor ?? RangePickerSurfaceTintColor,
            rangePickerShape: rangePickerShape ?? RangePickerShape,
            rangePickerHeaderBackgroundColor: rangePickerHeaderBackgroundColor ?? RangePickerHeaderBackgroundColor,
            rangePickerHeaderForegroundColor: rangePickerHeaderForegroundColor ?? RangePickerHeaderForegroundColor,
            rangePickerHeaderHeadlineStyle: rangePickerHeaderHeadlineStyle ?? RangePickerHeaderHeadlineStyle,
            rangePickerHeaderHelpStyle: rangePickerHeaderHelpStyle ?? RangePickerHeaderHelpStyle,
            rangeSelectionBackgroundColor: rangeSelectionBackgroundColor ?? RangeSelectionBackgroundColor,
            rangeSelectionOverlayColor: rangeSelectionOverlayColor ?? RangeSelectionOverlayColor,
            dividerColor: dividerColor ?? DividerColor,
            inputDecorationTheme: (object?)inputDecorationTheme ?? InputDecorationTheme,
            cancelButtonStyle: cancelButtonStyle ?? CancelButtonStyle,
            confirmButtonStyle: confirmButtonStyle ?? ConfirmButtonStyle,
            locale: locale ?? Locale,
            toggleButtonTextStyle: toggleButtonTextStyle ?? ToggleButtonTextStyle,
            subHeaderForegroundColor: subHeaderForegroundColor ?? SubHeaderForegroundColor);
    }

    /// <summary>Linearly interpolates between two <see cref="DatePickerThemeData"/>.</summary>
    public static DatePickerThemeData Lerp(DatePickerThemeData? a, DatePickerThemeData? b, double t)
    {
        if (ReferenceEquals(a, b) && a is not null)
        {
            return a;
        }

        return new DatePickerThemeData(
            backgroundColor: Color.Lerp(a?.BackgroundColor, b?.BackgroundColor, t),
            elevation: MaterialThemeLerp.Double(a?.Elevation, b?.Elevation, t),
            shadowColor: Color.Lerp(a?.ShadowColor, b?.ShadowColor, t),
            surfaceTintColor: Color.Lerp(a?.SurfaceTintColor, b?.SurfaceTintColor, t),
            shape: ShapeBorder.Lerp(a?.Shape, b?.Shape, t),
            headerBackgroundColor: Color.Lerp(
                a?.HeaderBackgroundColor,
                b?.HeaderBackgroundColor,
                t),
            headerForegroundColor: Color.Lerp(
                a?.HeaderForegroundColor,
                b?.HeaderForegroundColor,
                t),
            headerHeadlineStyle: TextStyle.Lerp(a?.HeaderHeadlineStyle, b?.HeaderHeadlineStyle, t),
            headerHelpStyle: TextStyle.Lerp(a?.HeaderHelpStyle, b?.HeaderHelpStyle, t),
            weekdayStyle: TextStyle.Lerp(a?.WeekdayStyle, b?.WeekdayStyle, t),
            dayStyle: TextStyle.Lerp(a?.DayStyle, b?.DayStyle, t),
            dayForegroundColor: WidgetStateProperty<Color?>.Lerp(
                a?.DayForegroundColor,
                b?.DayForegroundColor,
                t,
                Color.Lerp),
            dayBackgroundColor: WidgetStateProperty<Color?>.Lerp(
                a?.DayBackgroundColor,
                b?.DayBackgroundColor,
                t,
                Color.Lerp),
            dayOverlayColor: WidgetStateProperty<Color?>.Lerp(
                a?.DayOverlayColor,
                b?.DayOverlayColor,
                t,
                Color.Lerp),
            dayShape: WidgetStateProperty<OutlinedBorder?>.Lerp(
                a?.DayShape,
                b?.DayShape,
                t,
                OutlinedBorder.Lerp),
            todayForegroundColor: WidgetStateProperty<Color?>.Lerp(
                a?.TodayForegroundColor,
                b?.TodayForegroundColor,
                t,
                Color.Lerp),
            todayBackgroundColor: WidgetStateProperty<Color?>.Lerp(
                a?.TodayBackgroundColor,
                b?.TodayBackgroundColor,
                t,
                Color.Lerp),
            todayBorder: LerpBorderSide(a?.TodayBorder, b?.TodayBorder, t),
            yearStyle: TextStyle.Lerp(a?.YearStyle, b?.YearStyle, t),
            yearForegroundColor: WidgetStateProperty<Color?>.Lerp(
                a?.YearForegroundColor,
                b?.YearForegroundColor,
                t,
                Color.Lerp),
            yearBackgroundColor: WidgetStateProperty<Color?>.Lerp(
                a?.YearBackgroundColor,
                b?.YearBackgroundColor,
                t,
                Color.Lerp),
            yearOverlayColor: WidgetStateProperty<Color?>.Lerp(
                a?.YearOverlayColor,
                b?.YearOverlayColor,
                t,
                Color.Lerp),
            yearShape: WidgetStateProperty<OutlinedBorder?>.Lerp(
                a?.YearShape,
                b?.YearShape,
                t,
                OutlinedBorder.Lerp),
            rangePickerBackgroundColor: Color.Lerp(
                a?.RangePickerBackgroundColor,
                b?.RangePickerBackgroundColor,
                t),
            rangePickerElevation: MaterialThemeLerp.Double(
                a?.RangePickerElevation,
                b?.RangePickerElevation,
                t),
            rangePickerShadowColor: Color.Lerp(
                a?.RangePickerShadowColor,
                b?.RangePickerShadowColor,
                t),
            rangePickerSurfaceTintColor: Color.Lerp(
                a?.RangePickerSurfaceTintColor,
                b?.RangePickerSurfaceTintColor,
                t),
            rangePickerShape: ShapeBorder.Lerp(a?.RangePickerShape, b?.RangePickerShape, t),
            rangePickerHeaderBackgroundColor: Color.Lerp(
                a?.RangePickerHeaderBackgroundColor,
                b?.RangePickerHeaderBackgroundColor,
                t),
            rangePickerHeaderForegroundColor: Color.Lerp(
                a?.RangePickerHeaderForegroundColor,
                b?.RangePickerHeaderForegroundColor,
                t),
            rangePickerHeaderHeadlineStyle: TextStyle.Lerp(
                a?.RangePickerHeaderHeadlineStyle,
                b?.RangePickerHeaderHeadlineStyle,
                t),
            rangePickerHeaderHelpStyle: TextStyle.Lerp(
                a?.RangePickerHeaderHelpStyle,
                b?.RangePickerHeaderHelpStyle,
                t),
            rangeSelectionBackgroundColor: Color.Lerp(
                a?.RangeSelectionBackgroundColor,
                b?.RangeSelectionBackgroundColor,
                t),
            rangeSelectionOverlayColor: WidgetStateProperty<Color?>.Lerp(
                a?.RangeSelectionOverlayColor,
                b?.RangeSelectionOverlayColor,
                t,
                Color.Lerp),
            dividerColor: Color.Lerp(a?.DividerColor, b?.DividerColor, t),
            inputDecorationTheme: t < 0.5 ? a?.InputDecorationTheme : b?.InputDecorationTheme,
            cancelButtonStyle: ButtonStyle.Lerp(a?.CancelButtonStyle, b?.CancelButtonStyle, t),
            confirmButtonStyle: ButtonStyle.Lerp(a?.ConfirmButtonStyle, b?.ConfirmButtonStyle, t),
            locale: t < 0.5 ? a?.Locale : b?.Locale,
            toggleButtonTextStyle: TextStyle.Lerp(
                a?.ToggleButtonTextStyle,
                b?.ToggleButtonTextStyle,
                t),
            subHeaderForegroundColor: Color.Lerp(
                a?.SubHeaderForegroundColor,
                b?.SubHeaderForegroundColor,
                t));
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(BackgroundColor);
        hash.Add(Elevation);
        hash.Add(ShadowColor);
        hash.Add(SurfaceTintColor);
        hash.Add(Shape);
        hash.Add(HeaderBackgroundColor);
        hash.Add(HeaderForegroundColor);
        hash.Add(HeaderHeadlineStyle);
        hash.Add(HeaderHelpStyle);
        hash.Add(WeekdayStyle);
        hash.Add(DayStyle);
        hash.Add(DayForegroundColor);
        hash.Add(DayBackgroundColor);
        hash.Add(DayOverlayColor);
        hash.Add(DayShape);
        hash.Add(TodayForegroundColor);
        hash.Add(TodayBackgroundColor);
        hash.Add(TodayBorder);
        hash.Add(YearStyle);
        hash.Add(YearForegroundColor);
        hash.Add(YearBackgroundColor);
        hash.Add(YearOverlayColor);
        hash.Add(YearShape);
        hash.Add(RangePickerBackgroundColor);
        hash.Add(RangePickerElevation);
        hash.Add(RangePickerShadowColor);
        hash.Add(RangePickerSurfaceTintColor);
        hash.Add(RangePickerShape);
        hash.Add(RangePickerHeaderBackgroundColor);
        hash.Add(RangePickerHeaderForegroundColor);
        hash.Add(RangePickerHeaderHeadlineStyle);
        hash.Add(RangePickerHeaderHelpStyle);
        hash.Add(RangeSelectionBackgroundColor);
        hash.Add(RangeSelectionOverlayColor);
        hash.Add(DividerColor);
        hash.Add(InputDecorationTheme);
        hash.Add(CancelButtonStyle);
        hash.Add(ConfirmButtonStyle);
        hash.Add(Locale);
        hash.Add(ToggleButtonTextStyle);
        hash.Add(SubHeaderForegroundColor);
        return hash.ToHashCode();
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return obj is DatePickerThemeData other
            && Equals(other.BackgroundColor, BackgroundColor)
            && other.Elevation == Elevation
            && Equals(other.ShadowColor, ShadowColor)
            && Equals(other.SurfaceTintColor, SurfaceTintColor)
            && Equals(other.Shape, Shape)
            && Equals(other.HeaderBackgroundColor, HeaderBackgroundColor)
            && Equals(other.HeaderForegroundColor, HeaderForegroundColor)
            && Equals(other.HeaderHeadlineStyle, HeaderHeadlineStyle)
            && Equals(other.HeaderHelpStyle, HeaderHelpStyle)
            && Equals(other.WeekdayStyle, WeekdayStyle)
            && Equals(other.DayStyle, DayStyle)
            && Equals(other.DayForegroundColor, DayForegroundColor)
            && Equals(other.DayBackgroundColor, DayBackgroundColor)
            && Equals(other.DayOverlayColor, DayOverlayColor)
            && Equals(other.DayShape, DayShape)
            && Equals(other.TodayForegroundColor, TodayForegroundColor)
            && Equals(other.TodayBackgroundColor, TodayBackgroundColor)
            && Nullable.Equals(other.TodayBorder, TodayBorder)
            && Equals(other.YearStyle, YearStyle)
            && Equals(other.YearForegroundColor, YearForegroundColor)
            && Equals(other.YearBackgroundColor, YearBackgroundColor)
            && Equals(other.YearOverlayColor, YearOverlayColor)
            && Equals(other.YearShape, YearShape)
            && Equals(other.RangePickerBackgroundColor, RangePickerBackgroundColor)
            && other.RangePickerElevation == RangePickerElevation
            && Equals(other.RangePickerShadowColor, RangePickerShadowColor)
            && Equals(other.RangePickerSurfaceTintColor, RangePickerSurfaceTintColor)
            && Equals(other.RangePickerShape, RangePickerShape)
            && Equals(other.RangePickerHeaderBackgroundColor, RangePickerHeaderBackgroundColor)
            && Equals(other.RangePickerHeaderForegroundColor, RangePickerHeaderForegroundColor)
            && Equals(other.RangePickerHeaderHeadlineStyle, RangePickerHeaderHeadlineStyle)
            && Equals(other.RangePickerHeaderHelpStyle, RangePickerHeaderHelpStyle)
            && Equals(other.RangeSelectionBackgroundColor, RangeSelectionBackgroundColor)
            && Equals(other.RangeSelectionOverlayColor, RangeSelectionOverlayColor)
            && Equals(other.DividerColor, DividerColor)
            && Equals(other.InputDecorationTheme, InputDecorationTheme)
            && Equals(other.CancelButtonStyle, CancelButtonStyle)
            && Equals(other.ConfirmButtonStyle, ConfirmButtonStyle)
            && Equals(other.Locale, Locale)
            && Equals(other.ToggleButtonTextStyle, ToggleButtonTextStyle)
            && Equals(other.SubHeaderForegroundColor, SubHeaderForegroundColor);
    }

    public static bool operator ==(DatePickerThemeData? left, DatePickerThemeData? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(DatePickerThemeData? left, DatePickerThemeData? right) => !(left == right);

    /// Dart's `Diagnosticable.toString`.
    public override string ToString()
    {
        IDiagnosticable self = this;
        return Constants.KDebugMode
            ? self.ToDiagnosticsNode(style: DiagnosticsTreeStyle.SingleLine).ToString(null, DiagnosticLevel.Info)
            : self.ToStringShort();
    }

    public virtual void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        object nullDefault = DiagnosticsDefaults.NullValue;
        properties.Add(new ColorProperty("backgroundColor", BackgroundColor, defaultValue: nullDefault));
        properties.Add(new DoubleProperty("elevation", Elevation, defaultValue: nullDefault));
        properties.Add(new ColorProperty("shadowColor", ShadowColor, defaultValue: nullDefault));
        properties.Add(new ColorProperty("surfaceTintColor", SurfaceTintColor, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<ShapeBorder>("shape", Shape, defaultValue: nullDefault));
        properties.Add(new ColorProperty("headerBackgroundColor", HeaderBackgroundColor, defaultValue: nullDefault));
        properties.Add(new ColorProperty("headerForegroundColor", HeaderForegroundColor, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<TextStyle>(
            "headerHeadlineStyle",
            HeaderHeadlineStyle,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<TextStyle>(
            "headerHelpStyle",
            HeaderHelpStyle,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<TextStyle>("weekDayStyle", WeekdayStyle, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<TextStyle>("dayStyle", DayStyle, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<Color?>>(
            "dayForegroundColor",
            DayForegroundColor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<Color?>>(
            "dayBackgroundColor",
            DayBackgroundColor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<Color?>>(
            "dayOverlayColor",
            DayOverlayColor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<OutlinedBorder?>>(
            "dayShape",
            DayShape,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<Color?>>(
            "todayForegroundColor",
            TodayForegroundColor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<Color?>>(
            "todayBackgroundColor",
            TodayBackgroundColor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<BorderSide?>("todayBorder", TodayBorder, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<TextStyle>("yearStyle", YearStyle, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<Color?>>(
            "yearForegroundColor",
            YearForegroundColor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<Color?>>(
            "yearBackgroundColor",
            YearBackgroundColor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<Color?>>(
            "yearOverlayColor",
            YearOverlayColor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<OutlinedBorder?>>(
            "yearShape",
            YearShape,
            defaultValue: nullDefault));
        properties.Add(new ColorProperty(
            "rangePickerBackgroundColor",
            RangePickerBackgroundColor,
            defaultValue: nullDefault));
        properties.Add(new DoubleProperty("rangePickerElevation", RangePickerElevation, defaultValue: nullDefault));
        properties.Add(new ColorProperty("rangePickerShadowColor", RangePickerShadowColor, defaultValue: nullDefault));
        properties.Add(new ColorProperty(
            "rangePickerSurfaceTintColor",
            RangePickerSurfaceTintColor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<ShapeBorder>(
            "rangePickerShape",
            RangePickerShape,
            defaultValue: nullDefault));
        properties.Add(new ColorProperty(
            "rangePickerHeaderBackgroundColor",
            RangePickerHeaderBackgroundColor,
            defaultValue: nullDefault));
        properties.Add(new ColorProperty(
            "rangePickerHeaderForegroundColor",
            RangePickerHeaderForegroundColor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<TextStyle>(
            "rangePickerHeaderHeadlineStyle",
            RangePickerHeaderHeadlineStyle,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<TextStyle>(
            "rangePickerHeaderHelpStyle",
            RangePickerHeaderHelpStyle,
            defaultValue: nullDefault));
        properties.Add(new ColorProperty(
            "rangeSelectionBackgroundColor",
            RangeSelectionBackgroundColor,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<WidgetStateProperty<Color?>>(
            "rangeSelectionOverlayColor",
            RangeSelectionOverlayColor,
            defaultValue: nullDefault));
        properties.Add(new ColorProperty("dividerColor", DividerColor, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<InputDecorationThemeData>(
            "inputDecorationTheme",
            InputDecorationTheme,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<ButtonStyle>(
            "cancelButtonStyle",
            CancelButtonStyle,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<ButtonStyle>(
            "confirmButtonStyle",
            ConfirmButtonStyle,
            defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<Locale>("locale", Locale, defaultValue: nullDefault));
        properties.Add(new DiagnosticsProperty<TextStyle>(
            "toggleButtonTextStyle",
            ToggleButtonTextStyle,
            defaultValue: nullDefault));
        properties.Add(new ColorProperty(
            "subHeaderForegroundColor",
            SubHeaderForegroundColor,
            defaultValue: nullDefault));
    }

    // Dart's private `_lerpBorderSide`. When both sides are non-null it lerps `a` toward a transparent,
    // zero-width copy of itself and ignores `b`; kept verbatim. Dart's `identical` on the two sides is
    // value equality here, as BorderSide is a value type.
    private static BorderSide? LerpBorderSide(BorderSide? a, BorderSide? b, double t)
    {
        if (Nullable.Equals(a, b))
        {
            return a;
        }

        if (a == null)
        {
            return BorderSide.Lerp(new BorderSide(width: 0, color: b!.Value.Color.WithAlpha(0)), b.Value, t);
        }

        return BorderSide.Lerp(a.Value, new BorderSide(width: 0, color: a.Value.Color.WithAlpha(0)), t);
    }}

/// <summary>
/// An inherited widget that overrides the default visual properties of <see cref="DatePickerDialog"/>s
/// in this widget's subtree.
/// </summary>
/// <remarks>
/// See also <see cref="ThemeData.DatePickerTheme"/>, which describes the overall theme information
/// for the application.
/// </remarks>
public class DatePickerTheme : InheritedTheme
{
    /// <summary>
    /// Creates a <see cref="DatePickerTheme"/> that controls visual parameters for descendent
    /// <see cref="DatePickerDialog"/>s.
    /// </summary>
    public DatePickerTheme(DatePickerThemeData data, Widget child, Key? key = null) : base(child, key)
    {
        Data = data;
    }

    /// <summary>Specifies the visual properties used by descendant <see cref="DatePickerDialog"/> widgets.</summary>
    public DatePickerThemeData Data { get; }

    /// <summary>
    /// The <see cref="Data"/> from the closest <see cref="DatePickerTheme"/> instance that encloses the
    /// given context, or <see cref="ThemeData.DatePickerTheme"/> when there is none.
    /// </summary>
    /// <remarks>
    /// Every property of the returned value may be null; widgets fall back to <see cref="Defaults"/>.
    /// </remarks>
    public static DatePickerThemeData Of(BuildContext context)
    {
        return MaybeOf(context) ?? Theme.Of(context).DatePickerTheme;
    }

    /// <summary>
    /// The <see cref="Data"/> from the closest instance of this class that encloses the given context,
    /// if any.
    /// </summary>
    public static DatePickerThemeData? MaybeOf(BuildContext context)
    {
        return context.DependOnInheritedWidgetOfExactType<DatePickerTheme>()?.Data;
    }

    /// <summary>
    /// A <see cref="DatePickerThemeData"/> used for the default properties of date pickers, depending
    /// on <see cref="ThemeData.UseMaterial3"/>.
    /// </summary>
    public static DatePickerThemeData Defaults(BuildContext context)
    {
        return Theme.Of(context).UseMaterial3
            ? new DatePickerDefaultsM3(context)
            : new DatePickerDefaultsM2(context);
    }

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new DatePickerTheme(data: Data, child: child);
    }

    public override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        Data != ((DatePickerTheme)oldWidget).Data;
}

// Hand coded defaults based on Material Design 2.
/// <summary>Dart's private <c>_DatePickerDefaultsM2</c>.</summary>
internal sealed class DatePickerDefaultsM2 : DatePickerThemeData
{
    private readonly BuildContext _context;
    private ThemeData? _theme;
    private ColorScheme? _colors;
    private TextTheme? _textTheme;
    private bool? _isDark;

    public DatePickerDefaultsM2(BuildContext context) : base(
        elevation: 24.0,
        shape: new RoundedRectangleBorder(borderRadius: BorderRadius.All(Radius.Circular(4.0))),
        dayShape: new WidgetStatePropertyAll<OutlinedBorder?>(new CircleBorder()),
        yearShape: new WidgetStatePropertyAll<OutlinedBorder?>(new StadiumBorder()),
        rangePickerElevation: 0.0,
        rangePickerShape: new RoundedRectangleBorder())
    {
        _context = context;
    }

    // Dart's `late final` fields: read from the context on first use.
    private ThemeData LateTheme => _theme ??= Theme.Of(_context);

    private ColorScheme LateColors => _colors ??= LateTheme.ColorScheme;

    private TextTheme LateTextTheme => _textTheme ??= LateTheme.TextTheme;

    private bool IsDark => _isDark ??= LateColors.Brightness == Brightness.Dark;

    public override Color? HeaderBackgroundColor => IsDark ? LateColors.Surface : LateColors.Primary;

    public override Color? SubHeaderForegroundColor => LateColors.OnSurface.WithOpacity(0.60);

    public override TextStyle? ToggleButtonTextStyle =>
        LateTextTheme.TitleSmall?.Apply(color: SubHeaderForegroundColor);

    public override ButtonStyle CancelButtonStyle => TextButton.StyleFrom();

    public override ButtonStyle ConfirmButtonStyle => TextButton.StyleFrom();

    public override Color? HeaderForegroundColor => IsDark ? LateColors.OnSurface : LateColors.OnPrimary;

    public override TextStyle? HeaderHeadlineStyle => LateTextTheme.HeadlineSmall;

    public override TextStyle? HeaderHelpStyle => LateTextTheme.LabelSmall;

    public override TextStyle? WeekdayStyle =>
        LateTextTheme.BodySmall?.Apply(color: LateColors.OnSurface.WithOpacity(0.60));

    public override TextStyle? DayStyle => LateTextTheme.BodySmall;

    public override WidgetStateProperty<Color?>? DayForegroundColor =>
        WidgetStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.Contains(WidgetState.Selected))
            {
                return LateColors.OnPrimary;
            }
            else if (states.Contains(WidgetState.Disabled))
            {
                return LateColors.OnSurface.WithOpacity(0.38);
            }

            return LateColors.OnSurface;
        });

    public override WidgetStateProperty<Color?>? DayBackgroundColor =>
        WidgetStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.Contains(WidgetState.Selected))
            {
                return LateColors.Primary;
            }

            return null;
        });

    public override WidgetStateProperty<Color?>? DayOverlayColor =>
        WidgetStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.Contains(WidgetState.Selected))
            {
                if (states.Contains(WidgetState.Pressed))
                {
                    return LateColors.OnPrimary.WithOpacity(0.38);
                }

                if (states.Contains(WidgetState.Hovered))
                {
                    return LateColors.OnPrimary.WithOpacity(0.08);
                }

                if (states.Contains(WidgetState.Focused))
                {
                    return LateColors.OnPrimary.WithOpacity(0.12);
                }
            }
            else
            {
                if (states.Contains(WidgetState.Pressed))
                {
                    return LateColors.OnSurfaceVariant.WithOpacity(0.12);
                }

                if (states.Contains(WidgetState.Hovered))
                {
                    return LateColors.OnSurfaceVariant.WithOpacity(0.08);
                }

                if (states.Contains(WidgetState.Focused))
                {
                    return LateColors.OnSurfaceVariant.WithOpacity(0.12);
                }
            }

            return null;
        });

    public override WidgetStateProperty<Color?>? TodayForegroundColor =>
        WidgetStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.Contains(WidgetState.Selected))
            {
                return LateColors.OnPrimary;
            }
            else if (states.Contains(WidgetState.Disabled))
            {
                return LateColors.OnSurface.WithOpacity(0.38);
            }

            return LateColors.Primary;
        });

    public override WidgetStateProperty<Color?>? TodayBackgroundColor => DayBackgroundColor;

    public override BorderSide? TodayBorder => new BorderSide(color: LateColors.Primary);

    public override TextStyle? YearStyle => LateTextTheme.BodyLarge;

    public override Color? RangePickerBackgroundColor => LateColors.Surface;

    public override Color? RangePickerShadowColor => Colors.Transparent;

    public override Color? RangePickerSurfaceTintColor => Colors.Transparent;

    public override Color? RangePickerHeaderBackgroundColor => IsDark ? LateColors.Surface : LateColors.Primary;

    public override Color? RangePickerHeaderForegroundColor => IsDark ? LateColors.OnSurface : LateColors.OnPrimary;

    public override TextStyle? RangePickerHeaderHeadlineStyle => LateTextTheme.HeadlineSmall;

    public override TextStyle? RangePickerHeaderHelpStyle => LateTextTheme.LabelSmall;

    public override Color? RangeSelectionBackgroundColor => LateColors.Primary.WithOpacity(0.12);

    public override WidgetStateProperty<Color?>? RangeSelectionOverlayColor =>
        WidgetStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.Contains(WidgetState.Selected))
            {
                if (states.Contains(WidgetState.Pressed))
                {
                    return LateColors.OnPrimary.WithOpacity(0.38);
                }

                if (states.Contains(WidgetState.Hovered))
                {
                    return LateColors.OnPrimary.WithOpacity(0.08);
                }

                if (states.Contains(WidgetState.Focused))
                {
                    return LateColors.OnPrimary.WithOpacity(0.12);
                }
            }
            else
            {
                if (states.Contains(WidgetState.Pressed))
                {
                    return LateColors.OnSurfaceVariant.WithOpacity(0.12);
                }

                if (states.Contains(WidgetState.Hovered))
                {
                    return LateColors.OnSurfaceVariant.WithOpacity(0.08);
                }

                if (states.Contains(WidgetState.Focused))
                {
                    return LateColors.OnSurfaceVariant.WithOpacity(0.12);
                }
            }

            return null;
        });
}

// BEGIN GENERATED TOKEN PROPERTIES - DatePicker

// Do not edit by hand. The code between the "BEGIN GENERATED" and
// "END GENERATED" comments are generated from data in the Material
// Design token database by the script:
//   dev/tools/gen_defaults/bin/gen_defaults.dart.

/// <summary>Dart's private <c>_DatePickerDefaultsM3</c>.</summary>
internal sealed class DatePickerDefaultsM3 : DatePickerThemeData
{
    private readonly BuildContext _context;
    private ThemeData? _theme;
    private ColorScheme? _colors;
    private TextTheme? _textTheme;

    public DatePickerDefaultsM3(BuildContext context) : base(
        elevation: 6.0,
        shape: new RoundedRectangleBorder(borderRadius: BorderRadius.All(Radius.Circular(28.0))),
        // TODO(tahatesser): Update this to use token when gen_defaults
        // supports `CircleBorder` for fully rounded corners.
        dayShape: new WidgetStatePropertyAll<OutlinedBorder?>(new CircleBorder()),
        yearShape: new WidgetStatePropertyAll<OutlinedBorder?>(new StadiumBorder()),
        rangePickerElevation: 0.0,
        rangePickerShape: new RoundedRectangleBorder())
    {
        _context = context;
    }

    // Dart's `late final` fields: read from the context on first use.
    private ThemeData LateTheme => _theme ??= Theme.Of(_context);

    private ColorScheme LateColors => _colors ??= LateTheme.ColorScheme;

    private TextTheme LateTextTheme => _textTheme ??= LateTheme.TextTheme;

    public override Color? BackgroundColor => LateColors.SurfaceContainerHigh;

    public override Color? SubHeaderForegroundColor => LateColors.OnSurface.WithOpacity(0.60);

    public override TextStyle? ToggleButtonTextStyle => LateTextTheme.TitleSmall?.Apply(
        color: SubHeaderForegroundColor);

    public override ButtonStyle CancelButtonStyle => TextButton.StyleFrom();

    public override ButtonStyle ConfirmButtonStyle => TextButton.StyleFrom();

    public override Color? ShadowColor => Colors.Transparent;

    public override Color? SurfaceTintColor => Colors.Transparent;

    public override Color? HeaderBackgroundColor => Colors.Transparent;

    public override Color? HeaderForegroundColor => LateColors.OnSurfaceVariant;

    public override TextStyle? HeaderHeadlineStyle => LateTextTheme.HeadlineLarge;

    public override TextStyle? HeaderHelpStyle => LateTextTheme.LabelLarge;

    public override TextStyle? WeekdayStyle => LateTextTheme.BodyLarge?.Apply(
        color: LateColors.OnSurface);

    public override TextStyle? DayStyle => LateTextTheme.BodyLarge;

    public override WidgetStateProperty<Color?>? DayForegroundColor =>
        WidgetStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.Contains(WidgetState.Selected))
            {
                return LateColors.OnPrimary;
            }
            else if (states.Contains(WidgetState.Disabled))
            {
                return LateColors.OnSurface.WithOpacity(0.38);
            }

            return LateColors.OnSurface;
        });

    public override WidgetStateProperty<Color?>? DayBackgroundColor =>
        WidgetStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.Contains(WidgetState.Selected))
            {
                return LateColors.Primary;
            }

            return null;
        });

    public override WidgetStateProperty<Color?>? DayOverlayColor =>
        WidgetStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.Contains(WidgetState.Selected))
            {
                if (states.Contains(WidgetState.Pressed))
                {
                    return LateColors.OnPrimary.WithOpacity(0.1);
                }

                if (states.Contains(WidgetState.Hovered))
                {
                    return LateColors.OnPrimary.WithOpacity(0.08);
                }

                if (states.Contains(WidgetState.Focused))
                {
                    return LateColors.OnPrimary.WithOpacity(0.1);
                }
            }
            else
            {
                if (states.Contains(WidgetState.Pressed))
                {
                    return LateColors.OnSurfaceVariant.WithOpacity(0.1);
                }

                if (states.Contains(WidgetState.Hovered))
                {
                    return LateColors.OnSurfaceVariant.WithOpacity(0.08);
                }

                if (states.Contains(WidgetState.Focused))
                {
                    return LateColors.OnSurfaceVariant.WithOpacity(0.1);
                }
            }

            return null;
        });

    public override WidgetStateProperty<Color?>? TodayForegroundColor =>
        WidgetStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.Contains(WidgetState.Selected))
            {
                return LateColors.OnPrimary;
            }
            else if (states.Contains(WidgetState.Disabled))
            {
                return LateColors.Primary.WithOpacity(0.38);
            }

            return LateColors.Primary;
        });

    public override WidgetStateProperty<Color?>? TodayBackgroundColor => DayBackgroundColor;

    public override BorderSide? TodayBorder => new BorderSide(color: LateColors.Primary);

    public override TextStyle? YearStyle => LateTextTheme.BodyLarge;

    public override WidgetStateProperty<Color?>? YearForegroundColor =>
        WidgetStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.Contains(WidgetState.Selected))
            {
                return LateColors.OnPrimary;
            }
            else if (states.Contains(WidgetState.Disabled))
            {
                return LateColors.OnSurfaceVariant.WithOpacity(0.38);
            }

            return LateColors.OnSurfaceVariant;
        });

    public override WidgetStateProperty<Color?>? YearBackgroundColor =>
        WidgetStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.Contains(WidgetState.Selected))
            {
                return LateColors.Primary;
            }

            return null;
        });

    public override WidgetStateProperty<Color?>? YearOverlayColor =>
        WidgetStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.Contains(WidgetState.Selected))
            {
                if (states.Contains(WidgetState.Pressed))
                {
                    return LateColors.OnPrimary.WithOpacity(0.1);
                }

                if (states.Contains(WidgetState.Hovered))
                {
                    return LateColors.OnPrimary.WithOpacity(0.08);
                }

                if (states.Contains(WidgetState.Focused))
                {
                    return LateColors.OnPrimary.WithOpacity(0.1);
                }
            }
            else
            {
                if (states.Contains(WidgetState.Pressed))
                {
                    return LateColors.OnSurfaceVariant.WithOpacity(0.1);
                }

                if (states.Contains(WidgetState.Hovered))
                {
                    return LateColors.OnSurfaceVariant.WithOpacity(0.08);
                }

                if (states.Contains(WidgetState.Focused))
                {
                    return LateColors.OnSurfaceVariant.WithOpacity(0.1);
                }
            }

            return null;
        });

    public override Color? RangePickerShadowColor => Colors.Transparent;

    public override Color? RangePickerSurfaceTintColor => Colors.Transparent;

    public override Color? RangeSelectionBackgroundColor => LateColors.SecondaryContainer;

    public override WidgetStateProperty<Color?>? RangeSelectionOverlayColor =>
        WidgetStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.Contains(WidgetState.Pressed))
            {
                return LateColors.OnPrimaryContainer.WithOpacity(0.1);
            }

            if (states.Contains(WidgetState.Hovered))
            {
                return LateColors.OnPrimaryContainer.WithOpacity(0.08);
            }

            if (states.Contains(WidgetState.Focused))
            {
                return LateColors.OnPrimaryContainer.WithOpacity(0.1);
            }

            return null;
        });

    public override Color? RangePickerHeaderBackgroundColor => Colors.Transparent;

    public override Color? RangePickerHeaderForegroundColor => LateColors.OnSurfaceVariant;

    public override TextStyle? RangePickerHeaderHeadlineStyle => LateTextTheme.TitleLarge;

    public override TextStyle? RangePickerHeaderHelpStyle => LateTextTheme.TitleSmall;
}

// END GENERATED TOKEN PROPERTIES - DatePicker
