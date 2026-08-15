using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class MaterialTimePickerThemeTests
{
    private static readonly IReadOnlySet<WidgetState> EmptySet = new HashSet<WidgetState>();
    private static readonly IReadOnlySet<WidgetState> SelectedSet = new HashSet<WidgetState> { WidgetState.Selected };

    [Fact]
    public void TimePickerThemeData_DefaultsAreNullAndCopyWithRoundTrips()
    {
        var data = new TimePickerThemeData();
        Assert.Null(data.BackgroundColor);
        Assert.Null(data.CancelButtonStyle);
        Assert.Null(data.ConfirmButtonStyle);
        Assert.Null(data.DayPeriodBorderSide);
        Assert.Null(data.DayPeriodColor);
        Assert.Null(data.DayPeriodShape);
        Assert.Null(data.DayPeriodTextColor);
        Assert.Null(data.DayPeriodTextStyle);
        Assert.Null(data.DialBackgroundColor);
        Assert.Null(data.DialHandColor);
        Assert.Null(data.DialTextColor);
        Assert.Null(data.DialTextStyle);
        Assert.Null(data.Elevation);
        Assert.Null(data.EntryModeIconColor);
        Assert.Null(data.HelpTextStyle);
        Assert.Null(data.HourMinuteColor);
        Assert.Null(data.HourMinuteShape);
        Assert.Null(data.HourMinuteTextColor);
        Assert.Null(data.HourMinuteTextStyle);
        Assert.Null(data.InputDecorationTheme);
        Assert.Null(data.Padding);
        Assert.Null(data.Shape);
        Assert.Null(data.TimeSelectorSeparatorColor);
        Assert.Null(data.TimeSelectorSeparatorTextStyle);

        Assert.Equal(data, data with { });
        Assert.Equal(data.GetHashCode(), (data with { }).GetHashCode());
    }

    [Fact]
    public void TimePickerThemeData_PlainDayPeriodColorIsWrappedButStateColorPassesThrough()
    {
        var plain = new TimePickerThemeData(DayPeriodColor: Colors.Red);
        Assert.Equal(Colors.Red, plain.DayPeriodColor!.Resolve(SelectedSet));
        Assert.Equal(MaterialColors.Transparent, plain.DayPeriodColor!.Resolve(EmptySet));

        var stateful = new TimePickerThemeData(DayPeriodColor: WidgetStateColor.ResolveWith(
            Colors.Blue,
            states => states.Contains(WidgetState.Selected) ? Colors.Green : Colors.Blue));
        Assert.Equal(Colors.Green, stateful.DayPeriodColor!.Resolve(SelectedSet));
        Assert.Equal(Colors.Blue, stateful.DayPeriodColor!.Resolve(EmptySet));
    }

    [Fact]
    public void TimePickerThemeData_LerpIsIdentityForSameInstanceAndInterpolatesFields()
    {
        var data = new TimePickerThemeData();
        Assert.Same(data, TimePickerThemeData.Lerp(data, data, 0.5));

        var a = new TimePickerThemeData(
            BackgroundColor: Color.FromArgb(255, 0, 0, 0),
            Elevation: 0,
            DialHandColor: Color.FromArgb(255, 0, 0, 0),
            HourMinuteColor: Color.FromArgb(255, 0, 0, 0),
            InputDecorationTheme: new InputDecorationThemeData(filled: true),
            Padding: EdgeInsetsGeometry.All(0));
        var b = new TimePickerThemeData(
            BackgroundColor: Color.FromArgb(255, 100, 100, 100),
            Elevation: 10,
            DialHandColor: Color.FromArgb(255, 100, 100, 100),
            HourMinuteColor: Color.FromArgb(255, 100, 100, 100),
            InputDecorationTheme: new InputDecorationThemeData(filled: false),
            Padding: EdgeInsetsGeometry.All(20));

        var mid = TimePickerThemeData.Lerp(a, b, 0.5);
        Assert.Equal(5, mid.Elevation);
        Assert.Equal(50, mid.BackgroundColor!.Value.R);
        Assert.Equal(50, mid.DialHandColor!.Value.R);
        // Dart lerps the state colors with Color.lerp, collapsing them to the default resolution.
        Assert.Equal(50, mid.HourMinuteColor!.DefaultValue.R);
        Assert.Equal(new Thickness(10), mid.Padding!.Value.Resolve(TextDirection.Ltr));
        // inputDecorationTheme is a discrete t < 0.5 switch, never interpolated.
        Assert.True(TimePickerThemeData.Lerp(a, b, 0.4).InputDecorationTheme!.Filled);
        Assert.False(TimePickerThemeData.Lerp(a, b, 0.6).InputDecorationTheme!.Filled);
    }

    [Fact]
    public void TimePickerThemeData_DayPeriodBorderSideLerpUsesTheNullWorkaround()
    {
        var side = new BorderSide(Colors.Red, 3);
        var withSide = new TimePickerThemeData(DayPeriodBorderSide: side);
        var without = new TimePickerThemeData();

        Assert.Null(TimePickerThemeData.Lerp(without, without, 0.5).DayPeriodBorderSide);
        Assert.Equal(side, TimePickerThemeData.Lerp(without, withSide, 0.5).DayPeriodBorderSide);
        Assert.Equal(side, TimePickerThemeData.Lerp(withSide, without, 0.5).DayPeriodBorderSide);
    }
}
