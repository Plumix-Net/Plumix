// Dart parity source: material_ui/lib/src/calendar_date_picker.dart
// Mirrors material-ui-src/test/calendar_date_picker_test.dart

using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using static Plumix.Tests.SemanticsMatchers;
using MaterialWidget = Plumix.Material.Material;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class CalendarDatePickerDartParityTests : IDisposable
{
    public CalendarDatePickerDartParityTests()
    {
        FocusManager.Instance.ResetForTests();
        // flutter_test: defaultTargetPlatform == android.
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
    }

    public void Dispose()
    {
        PlatformDefaults.DebugTargetPlatformOverride = null;
        FocusManager.Instance.ResetForTests();
    }

    private static FrameworkDartTester CreateTester() =>
        new(fakeGestureTimers: true, semanticsEnabled: true, registerTestTextInput: true);

    private static readonly Finder NextMonthIcon = Find.ByWidgetPredicate(
        w => w is IconButton button
             && ((button.Tooltip?.StartsWith("Next month", StringComparison.Ordinal) ?? false)
                 || (((Icon)button.Icon).SemanticLabel?.StartsWith("Next month", StringComparison.Ordinal)
                     ?? false)));

    private static readonly Finder PreviousMonthIcon = Find.ByWidgetPredicate(
        w => w is IconButton button
             && ((button.Tooltip?.StartsWith("Previous month", StringComparison.Ordinal) ?? false)
                 || (((Icon)button.Icon).SemanticLabel?.StartsWith("Previous month", StringComparison.Ordinal)
                     ?? false)));

    private static Widget CalendarDatePickerApp(
        Key? key = null,
        DateTime? initialDate = null,
        DateTime? firstDate = null,
        DateTime? lastDate = null,
        DateTime? currentDate = null,
        Action<DateTime>? onDateChanged = null,
        Action<DateTime>? onDisplayedMonthChanged = null,
        DatePickerMode initialCalendarMode = DatePickerMode.Day,
        SelectableDayPredicate? selectableDayPredicate = null,
        TextDirection textDirection = TextDirection.Ltr,
        ThemeData? theme = null,
        bool? useMaterial3 = null)
    {
        return new MaterialApp(
            theme: theme ?? new ThemeData(useMaterial3: useMaterial3),
            home: new MaterialWidget(
                child: new Directionality(
                    textDirection: textDirection,
                    child: new CalendarDatePicker(
                        key: key,
                        initialDate: initialDate,
                        firstDate: firstDate ?? new DateTime(2001, 1, 1),
                        lastDate: lastDate ?? new DateTime(2031, 12, 31),
                        currentDate: currentDate ?? new DateTime(2016, 1, 3),
                        onDateChanged: onDateChanged ?? (_ => { }),
                        onDisplayedMonthChanged: onDisplayedMonthChanged,
                        initialCalendarMode: initialCalendarMode,
                        selectableDayPredicate: selectableDayPredicate))));
    }

    private static Widget YearPickerApp(
        Key? key = null,
        DateTime? selectedDate = null,
        DateTime? initialDate = null,
        DateTime? firstDate = null,
        DateTime? lastDate = null,
        DateTime? currentDate = null,
        Action<DateTime>? onChanged = null,
        TextDirection textDirection = TextDirection.Ltr)
    {
        return new MaterialApp(
            home: new MaterialWidget(
                child: new Directionality(
                    textDirection: textDirection,
                    child: new YearPicker(
                        key: key,
                        selectedDate: selectedDate ?? new DateTime(2016, 1, 15),
                        firstDate: firstDate ?? new DateTime(2001, 1, 1),
                        lastDate: lastDate ?? new DateTime(2031, 12, 31),
                        currentDate: currentDate ?? new DateTime(2016, 1, 3),
                        onChanged: onChanged ?? (_ => { })))));
    }

    private static RenderObject MaterialOf(FrameworkDartTester tester, Finder finder) =>
        (RenderObject)MaterialWidget.Of(tester.Element(finder));

    private static void ExpectEnabledIsFalse(SemanticsNode node)
    {
        // Dart: `flagsCollection.isEnabled == Tristate.isFalse`.
        Assert.True(node.Flags.HasFlag(SemanticsFlags.HasEnabledState));
        Assert.False(node.Flags.HasFlag(SemanticsFlags.IsEnabled));
    }

    // ---- group('CalendarDatePicker') ----

    // Flutter: 'CalendarDatePicker: Can select a day'
    [Fact]
    public void CanSelectADay()
    {
        using FrameworkDartTester tester = CreateTester();
        DateTime? selectedDate = null;
        tester.PumpWidget(CalendarDatePickerApp(
            initialDate: new DateTime(2016, 1, 15),
            onDateChanged: date => selectedDate = date));
        tester.Tap(Find.Text("12"));
        Assert.Equal(new DateTime(2016, 1, 12), selectedDate);
    }

    // Flutter: 'CalendarDatePicker: Can select a day with nothing first selected'
    [Fact]
    public void CanSelectADayWithNothingFirstSelected()
    {
        using FrameworkDartTester tester = CreateTester();
        DateTime? selectedDate = null;
        tester.PumpWidget(CalendarDatePickerApp(onDateChanged: date => selectedDate = date));
        tester.Tap(Find.Text("12"));
        Assert.Equal(new DateTime(2016, 1, 12), selectedDate);
    }

    // Flutter: 'CalendarDatePicker: Can select a month'
    [Fact]
    public void CanSelectAMonth()
    {
        using FrameworkDartTester tester = CreateTester();
        DateTime? displayedMonth = null;
        tester.PumpWidget(CalendarDatePickerApp(
            initialDate: new DateTime(2016, 1, 15),
            onDisplayedMonthChanged: date => displayedMonth = date));
        Finds.OneWidget(Find.Text("January 2016"));

        // Go back two months
        tester.Tap(PreviousMonthIcon);
        tester.PumpAndSettle();
        Finds.OneWidget(Find.Text("December 2015"));
        Assert.Equal(new DateTime(2015, 12, 1), displayedMonth);
        tester.Tap(PreviousMonthIcon);
        tester.PumpAndSettle();
        Finds.OneWidget(Find.Text("November 2015"));
        Assert.Equal(new DateTime(2015, 11, 1), displayedMonth);

        // Go forward a month
        tester.Tap(NextMonthIcon);
        tester.PumpAndSettle();
        Finds.OneWidget(Find.Text("December 2015"));
        Assert.Equal(new DateTime(2015, 12, 1), displayedMonth);
    }

    // Flutter: 'CalendarDatePicker: Can select a month with nothing first selected'
    [Fact]
    public void CanSelectAMonthWithNothingFirstSelected()
    {
        using FrameworkDartTester tester = CreateTester();
        DateTime? displayedMonth = null;
        tester.PumpWidget(CalendarDatePickerApp(onDisplayedMonthChanged: date => displayedMonth = date));
        Finds.OneWidget(Find.Text("January 2016"));

        // Go back two months
        tester.Tap(PreviousMonthIcon);
        tester.PumpAndSettle();
        Finds.OneWidget(Find.Text("December 2015"));
        Assert.Equal(new DateTime(2015, 12, 1), displayedMonth);
        tester.Tap(PreviousMonthIcon);
        tester.PumpAndSettle();
        Finds.OneWidget(Find.Text("November 2015"));
        Assert.Equal(new DateTime(2015, 11, 1), displayedMonth);

        // Go forward a month
        tester.Tap(NextMonthIcon);
        tester.PumpAndSettle();
        Finds.OneWidget(Find.Text("December 2015"));
        Assert.Equal(new DateTime(2015, 12, 1), displayedMonth);
    }

    // Flutter: 'CalendarDatePicker: Can select a year'
    [Fact]
    public void CanSelectAYear()
    {
        using FrameworkDartTester tester = CreateTester();
        DateTime? displayedMonth = null;
        tester.PumpWidget(CalendarDatePickerApp(
            initialDate: new DateTime(2016, 1, 15),
            onDisplayedMonthChanged: date => displayedMonth = date));

        tester.Tap(Find.Text("January 2016")); // Switch to year mode.
        tester.PumpAndSettle();
        tester.Tap(Find.Text("2018"));
        tester.PumpAndSettle();
        Finds.OneWidget(Find.Text("January 2018"));
        Assert.Equal(new DateTime(2018, 1, 1), displayedMonth);
    }

    // Flutter: 'CalendarDatePicker: Can select a year with nothing first selected'
    [Fact]
    public void CanSelectAYearWithNothingFirstSelected()
    {
        using FrameworkDartTester tester = CreateTester();
        DateTime? displayedMonth = null;
        tester.PumpWidget(CalendarDatePickerApp(onDisplayedMonthChanged: date => displayedMonth = date));

        tester.Tap(Find.Text("January 2016")); // Switch to year mode.
        tester.PumpAndSettle();
        tester.Tap(Find.Text("2018"));
        tester.PumpAndSettle();
        Finds.OneWidget(Find.Text("January 2018"));
        Assert.Equal(new DateTime(2018, 1, 1), displayedMonth);
    }

    // Flutter: 'CalendarDatePicker: Selecting date does not change displayed month'
    [Fact]
    public void SelectingDateDoesNotChangeDisplayedMonth()
    {
        using FrameworkDartTester tester = CreateTester();
        DateTime? selectedDate = null;
        DateTime? displayedMonth = null;
        tester.PumpWidget(CalendarDatePickerApp(
            initialDate: new DateTime(2020, 3, 15),
            onDateChanged: date => selectedDate = date,
            onDisplayedMonthChanged: date => displayedMonth = date));

        tester.Tap(NextMonthIcon);
        tester.PumpAndSettle();
        Finds.OneWidget(Find.Text("April 2020"));
        Assert.Equal(new DateTime(2020, 4, 1), displayedMonth);

        tester.Tap(Find.Text("25"));
        tester.PumpAndSettle();
        Finds.OneWidget(Find.Text("April 2020"));
        Assert.Equal(new DateTime(2020, 4, 1), displayedMonth);
        Assert.Equal(new DateTime(2020, 4, 25), selectedDate);
        // There isn't a 31 in April so there shouldn't be one if it is showing April.
        Finds.Nothing(Find.Text("31"));
    }

    // Flutter: 'CalendarDatePicker: Changing year does change selected date'
    [Fact]
    public void ChangingYearDoesChangeSelectedDate()
    {
        using FrameworkDartTester tester = CreateTester();
        DateTime? selectedDate = null;
        tester.PumpWidget(CalendarDatePickerApp(
            initialDate: new DateTime(2016, 1, 15),
            onDateChanged: date => selectedDate = date));
        tester.Tap(Find.Text("4"));
        Assert.Equal(new DateTime(2016, 1, 4), selectedDate);
        tester.Tap(Find.Text("January 2016"));
        tester.PumpAndSettle();
        tester.Tap(Find.Text("2018"));
        tester.PumpAndSettle();
        Assert.Equal(new DateTime(2018, 1, 4), selectedDate);
    }

    // Flutter: 'CalendarDatePicker: Changing year for february 29th'
    [Fact]
    public void ChangingYearForFebruary29th()
    {
        using FrameworkDartTester tester = CreateTester();
        DateTime? selectedDate = null;
        tester.PumpWidget(CalendarDatePickerApp(
            initialDate: new DateTime(2020, 2, 29),
            onDateChanged: date => selectedDate = date));
        tester.Tap(Find.Text("February 2020"));
        tester.PumpAndSettle();
        tester.Tap(Find.Text("2018"));
        tester.PumpAndSettle();
        Assert.Equal(new DateTime(2018, 2, 28), selectedDate);
        tester.Tap(Find.Text("February 2018"));
        tester.PumpAndSettle();
        tester.Tap(Find.Text("2020"));
        tester.PumpAndSettle();
        // Changing back to 2020 the 29th is not selected anymore.
        Assert.Equal(new DateTime(2020, 2, 28), selectedDate);
    }

    // Flutter: 'CalendarDatePicker: Changing year does not change the month'
    [Fact]
    public void ChangingYearDoesNotChangeTheMonth()
    {
        using FrameworkDartTester tester = CreateTester();
        DateTime? displayedMonth = null;
        tester.PumpWidget(CalendarDatePickerApp(
            initialDate: new DateTime(2016, 1, 15),
            onDisplayedMonthChanged: date => displayedMonth = date));
        tester.Tap(NextMonthIcon);
        tester.PumpAndSettle();
        tester.Tap(NextMonthIcon);
        tester.PumpAndSettle();
        tester.Tap(Find.Text("March 2016"));
        tester.PumpAndSettle();
        tester.Tap(Find.Text("2018"));
        tester.PumpAndSettle();
        Finds.OneWidget(Find.Text("March 2018"));
        Assert.Equal(new DateTime(2018, 3, 1), displayedMonth);
    }

    // Flutter: 'CalendarDatePicker: Can select a year and then a day'
    [Fact]
    public void CanSelectAYearAndThenADay()
    {
        using FrameworkDartTester tester = CreateTester();
        DateTime? selectedDate = null;
        tester.PumpWidget(CalendarDatePickerApp(
            initialDate: new DateTime(2016, 1, 15),
            onDateChanged: date => selectedDate = date));
        tester.Tap(Find.Text("January 2016")); // Switch to year mode.
        tester.PumpAndSettle();
        tester.Tap(Find.Text("2017"));
        tester.PumpAndSettle();
        tester.Tap(Find.Text("19"));
        Assert.Equal(new DateTime(2017, 1, 19), selectedDate);
    }

    // Flutter: 'CalendarDatePicker: Cannot select a day outside bounds'
    [Fact]
    public void CannotSelectADayOutsideBounds()
    {
        using FrameworkDartTester tester = CreateTester();
        var validDate = new DateTime(2017, 1, 15);
        DateTime? selectedDate = null;
        tester.PumpWidget(CalendarDatePickerApp(
            initialDate: validDate,
            firstDate: validDate,
            lastDate: validDate,
            onDateChanged: date => selectedDate = date));

        // Earlier than firstDate. Should be ignored.
        tester.Tap(Find.Text("10"));
        Assert.Null(selectedDate);

        // Later than lastDate. Should be ignored.
        tester.Tap(Find.Text("20"));
        Assert.Null(selectedDate);

        // This one is just right.
        tester.Tap(Find.Text("15"));
        Assert.Equal(validDate, selectedDate);
    }

    // Flutter: 'CalendarDatePicker: Cannot navigate to a month outside bounds'
    [Fact]
    public void CannotNavigateToAMonthOutsideBounds()
    {
        using FrameworkDartTester tester = CreateTester();
        DateTime? displayedMonth = null;
        tester.PumpWidget(CalendarDatePickerApp(
            firstDate: new DateTime(2016, 12, 15),
            initialDate: new DateTime(2017, 1, 15),
            lastDate: new DateTime(2017, 2, 15),
            onDisplayedMonthChanged: date => displayedMonth = date));

        tester.Tap(NextMonthIcon);
        tester.PumpAndSettle();
        Assert.Equal(new DateTime(2017, 2, 1), displayedMonth);
        // Shouldn't be possible to keep going forward into March.
        ExpectEnabledIsFalse(tester.GetSemantics(NextMonthIcon));

        tester.Tap(PreviousMonthIcon);
        tester.PumpAndSettle();
        tester.Tap(PreviousMonthIcon);
        tester.PumpAndSettle();
        Assert.Equal(new DateTime(2016, 12, 1), displayedMonth);
        // Shouldn't be possible to keep going backward into November.
        ExpectEnabledIsFalse(tester.GetSemantics(PreviousMonthIcon));
    }

    // Flutter: 'CalendarDatePicker: Cannot select disabled year'
    [Fact]
    public void CannotSelectDisabledYear()
    {
        using FrameworkDartTester tester = CreateTester();
        DateTime? displayedMonth = null;
        tester.PumpWidget(CalendarDatePickerApp(
            firstDate: new DateTime(2018, 6, 9),
            initialDate: new DateTime(2018, 7, 4),
            lastDate: new DateTime(2018, 12, 15),
            onDisplayedMonthChanged: date => displayedMonth = date));
        tester.Tap(Find.Text("July 2018")); // Switch to year mode.
        tester.PumpAndSettle();
        tester.Tap(Find.Text("2016")); // Disabled, doesn't change the year.
        tester.PumpAndSettle();
        tester.Tap(Find.Text("2020")); // Disabled, doesn't change the year.
        tester.PumpAndSettle();

        tester.Tap(Find.Text("2018"));
        tester.PumpAndSettle();
        // Nothing should have changed.
        Assert.Null(displayedMonth);
    }

    // Flutter: 'CalendarDatePicker: Selecting firstDate year respects firstDate'
    [Fact]
    public void SelectingFirstDateYearRespectsFirstDate()
    {
        using FrameworkDartTester tester = CreateTester();
        DateTime? selectedDate = null;
        DateTime? displayedMonth = null;
        tester.PumpWidget(CalendarDatePickerApp(
            firstDate: new DateTime(2016, 6, 9),
            initialDate: new DateTime(2018, 5, 4),
            lastDate: new DateTime(2019, 1, 15),
            onDateChanged: date => selectedDate = date,
            onDisplayedMonthChanged: date => displayedMonth = date));
        tester.Tap(Find.Text("May 2018"));
        tester.PumpAndSettle();
        tester.Tap(Find.Text("2016"));
        tester.PumpAndSettle();
        // Month should be clamped to June as the range starts at June 2016.
        Finds.OneWidget(Find.Text("June 2016"));
        Assert.Equal(new DateTime(2016, 6, 1), displayedMonth);
        Assert.Equal(new DateTime(2016, 6, 9), selectedDate);
    }

    // Flutter: 'CalendarDatePicker: Selecting lastDate year respects lastDate' (first of two)
    [Fact]
    public void SelectingLastDateYearRespectsLastDate()
    {
        using FrameworkDartTester tester = CreateTester();
        DateTime? selectedDate = null;
        DateTime? displayedMonth = null;
        tester.PumpWidget(CalendarDatePickerApp(
            firstDate: new DateTime(2016, 6, 9),
            initialDate: new DateTime(2018, 5, 4),
            lastDate: new DateTime(2019, 1, 15),
            onDateChanged: date => selectedDate = date,
            onDisplayedMonthChanged: date => displayedMonth = date));
        // Selected date is now 2018-05-04 (initialDate).
        tester.Tap(Find.Text("May 2018"));
        // Selected date is still 2018-05-04.
        tester.PumpAndSettle();
        tester.Tap(Find.Text("2019"));
        // Selected date would become 2019-05-04 but gets clamped to the month of lastDate, so 2019-01-04.
        tester.PumpAndSettle();
        Finds.OneWidget(Find.Text("January 2019"));
        Assert.Equal(new DateTime(2019, 1, 1), displayedMonth);
        Assert.Equal(new DateTime(2019, 1, 4), selectedDate);
    }

    // Flutter: 'CalendarDatePicker: Selecting lastDate year respects lastDate' (second of two)
    [Fact]
    public void SelectingLastDateYearRespectsLastDateDay()
    {
        using FrameworkDartTester tester = CreateTester();
        DateTime? selectedDate = null;
        DateTime? displayedMonth = null;
        tester.PumpWidget(CalendarDatePickerApp(
            firstDate: new DateTime(2016, 6, 9),
            initialDate: new DateTime(2018, 5, 15),
            lastDate: new DateTime(2019, 1, 4),
            onDateChanged: date => selectedDate = date,
            onDisplayedMonthChanged: date => displayedMonth = date));
        // Selected date is now 2018-05-15 (initialDate).
        tester.Tap(Find.Text("May 2018"));
        // Selected date is still 2018-05-15.
        tester.PumpAndSettle();
        tester.Tap(Find.Text("2019"));
        // Selected date would become 2019-05-15 but gets clamped to the month of lastDate, so 2019-01-15.
        // Day is now beyond the lastDate so that also gets clamped, to 2019-01-04.
        tester.PumpAndSettle();
        Finds.OneWidget(Find.Text("January 2019"));
        Assert.Equal(new DateTime(2019, 1, 1), displayedMonth);
        Assert.Equal(new DateTime(2019, 1, 4), selectedDate);
    }

    // Flutter: 'CalendarDatePicker: Only predicate days are selectable'
    [Fact]
    public void OnlyPredicateDaysAreSelectable()
    {
        using FrameworkDartTester tester = CreateTester();
        DateTime? selectedDate = null;
        tester.PumpWidget(CalendarDatePickerApp(
            firstDate: new DateTime(2017, 1, 10),
            initialDate: new DateTime(2017, 1, 16),
            lastDate: new DateTime(2017, 1, 20),
            onDateChanged: date => selectedDate = date,
            selectableDayPredicate: date => date.Day % 2 == 0));
        tester.Tap(Find.Text("13")); // Odd, doesn't work.
        Assert.Null(selectedDate);
        tester.Tap(Find.Text("10")); // Even, works.
        Assert.Equal(new DateTime(2017, 1, 10), selectedDate);
        tester.Tap(Find.Text("17")); // Odd, doesn't work.
        Assert.Equal(new DateTime(2017, 1, 10), selectedDate);
    }

    // Flutter: 'CalendarDatePicker: Can select initial calendar picker mode'
    [Fact]
    public void CanSelectInitialCalendarPickerMode()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(CalendarDatePickerApp(
            initialDate: new DateTime(2014, 1, 15),
            initialCalendarMode: DatePickerMode.Year));
        // 2018 wouldn't be available if the year picker wasn't showing.
        // The initial current year is 2014.
        tester.Tap(Find.Text("2018"));
        tester.PumpAndSettle();
        Finds.OneWidget(Find.Text("January 2018"));
    }

    // Flutter: 'CalendarDatePicker: Material2 - currentDate is highlighted'
    [Fact]
    public void Material2CurrentDateIsHighlighted()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(CalendarDatePickerApp(
            useMaterial3: false,
            initialDate: new DateTime(2016, 1, 15),
            currentDate: new DateTime(2016, 1, 2)));
        var todayColor = new Color(0xff2196f3); // default primary color
        PaintAssert.Paints(
            MaterialOf(tester, Find.Text("2")),
            // The current day should be painted with a circle outline.
            PaintPattern.Paints.Circle(color: todayColor, style: PaintingStyle.Stroke, strokeWidth: 1.0));
    }

    // Flutter: 'CalendarDatePicker: Material3 - currentDate is highlighted'
    [Fact]
    public void Material3CurrentDateIsHighlighted()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(CalendarDatePickerApp(
            initialDate: new DateTime(2016, 1, 15),
            currentDate: new DateTime(2016, 1, 2)));
        var todayColor = new Color(0xff6750a4); // default primary color
        PaintAssert.Paints(
            MaterialOf(tester, Find.Text("2")),
            // The current day should be painted with a circle outline.
            PaintPattern.Paints.Circle(color: todayColor, style: PaintingStyle.Stroke, strokeWidth: 1.0));
    }

    // Flutter: 'CalendarDatePicker: Material2 - currentDate is highlighted even if it is disabled'
    [Fact]
    public void Material2CurrentDateIsHighlightedEvenIfItIsDisabled()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(CalendarDatePickerApp(
            useMaterial3: false,
            firstDate: new DateTime(2016, 1, 3),
            lastDate: new DateTime(2016, 1, 31),
            currentDate: new DateTime(2016, 1, 2), // not between first and last
            initialDate: new DateTime(2016, 1, 5)));
        var disabledColor = new Color(0x61000000); // default disabled color
        PaintAssert.Paints(
            MaterialOf(tester, Find.Text("2")),
            // The current day should be painted with a circle outline.
            PaintPattern.Paints.Circle(color: disabledColor, style: PaintingStyle.Stroke, strokeWidth: 1.0));
    }

    // Flutter: 'CalendarDatePicker: Material3 - currentDate is highlighted even if it is disabled'
    [Fact]
    public void Material3CurrentDateIsHighlightedEvenIfItIsDisabled()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(CalendarDatePickerApp(
            firstDate: new DateTime(2016, 1, 3),
            lastDate: new DateTime(2016, 1, 31),
            currentDate: new DateTime(2016, 1, 2), // not between first and last
            initialDate: new DateTime(2016, 1, 5)));
        var disabledColor = new Color(0x616750a4); // default disabled color
        PaintAssert.Paints(
            MaterialOf(tester, Find.Text("2")),
            // The current day should be painted with a circle outline.
            PaintPattern.Paints.Circle(color: disabledColor, style: PaintingStyle.Stroke, strokeWidth: 1.0));
    }

    // Flutter: 'CalendarDatePicker: Non-null todayBorder color should be respected over foreground color'
    [Fact]
    public void NonNullTodayBorderColorShouldBeRespectedOverForegroundColor()
    {
        using FrameworkDartTester tester = CreateTester();
        Color customBorderColor = MaterialColors.Red;
        tester.PumpWidget(CalendarDatePickerApp(
            initialDate: new DateTime(2016, 1, 15),
            currentDate: new DateTime(2016, 1, 2),
            theme: new ThemeData(
                datePickerTheme: new DatePickerThemeData(
                    todayBorder: new BorderSide(color: customBorderColor),
                    todayForegroundColor: WidgetStateProperty<Color?>.All(MaterialColors.Blue)))));
        PaintAssert.Paints(
            MaterialOf(tester, Find.Text("2")),
            // The current day should be painted with the custom border color.
            PaintPattern.Paints.Circle(color: customBorderColor, style: PaintingStyle.Stroke, strokeWidth: 1.0));
    }

    // Flutter: 'CalendarDatePicker: Non-null todayBorder color is used even when disabled'
    [Fact]
    public void NonNullTodayBorderColorIsUsedEvenWhenDisabled()
    {
        using FrameworkDartTester tester = CreateTester();
        Color customBorderColor = MaterialColors.Red;
        tester.PumpWidget(CalendarDatePickerApp(
            firstDate: new DateTime(2016, 1, 3),
            lastDate: new DateTime(2016, 1, 31),
            currentDate: new DateTime(2016, 1, 2), // not between first and last
            initialDate: new DateTime(2016, 1, 5),
            theme: new ThemeData(
                datePickerTheme: new DatePickerThemeData(
                    todayBorder: new BorderSide(color: customBorderColor),
                    todayForegroundColor: WidgetStateProperty<Color?>.All(MaterialColors.Blue)))));
        PaintAssert.Paints(
            MaterialOf(tester, Find.Text("2")),
            // The current day should be painted with the custom border color,
            // not with foreground color opacity applied, even it's disabled day.
            PaintPattern.Paints.Circle(color: customBorderColor, style: PaintingStyle.Stroke, strokeWidth: 1.0));
    }

    // Flutter: 'CalendarDatePicker: Transparent todayBorder should fall back to foreground color'
    [Fact]
    public void TransparentTodayBorderShouldFallBackToForegroundColor()
    {
        using FrameworkDartTester tester = CreateTester();
        Color customForegroundColor = MaterialColors.Green;
        tester.PumpWidget(CalendarDatePickerApp(
            initialDate: new DateTime(2016, 1, 15),
            currentDate: new DateTime(2016, 1, 2),
            theme: new ThemeData(
                datePickerTheme: new DatePickerThemeData(
                    todayBorder: new BorderSide(color: new Color(0x00000000)),
                    todayForegroundColor: WidgetStateProperty<Color?>.All(customForegroundColor)))));
        PaintAssert.Paints(
            MaterialOf(tester, Find.Text("2")),
            // The current day should use the foreground color since
            // todayBorder color is transparent.
            PaintPattern.Paints.Circle(
                color: customForegroundColor,
                style: PaintingStyle.Stroke,
                strokeWidth: 1.0));
    }

    // Flutter: 'CalendarDatePicker: Selecting date does not switch picker to year selection'
    [Fact]
    public void SelectingDateDoesNotSwitchPickerToYearSelection()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(CalendarDatePickerApp(
            initialDate: new DateTime(2020, 5, 10),
            initialCalendarMode: DatePickerMode.Year));
        tester.Tap(Find.Text("2017"));
        tester.PumpAndSettle();
        Finds.OneWidget(Find.Text("May 2017"));
        tester.Tap(Find.Text("10"));
        tester.PumpAndSettle();
        Finds.OneWidget(Find.Text("May 2017"));
        Finds.Nothing(Find.Text("2017"));
    }

    // Flutter: 'CalendarDatePicker: Selecting disabled date does not change current selection'
    [Fact]
    public void SelectingDisabledDateDoesNotChangeCurrentSelection()
    {
        using FrameworkDartTester tester = CreateTester();
        static DateTime Day(int day) => new(2020, 5, day);

        DateTime selection = Day(2);
        tester.PumpWidget(CalendarDatePickerApp(
            initialDate: selection,
            firstDate: Day(2),
            lastDate: Day(3),
            onDateChanged: date => selection = date));

        tester.Tap(Find.Text("3"));
        tester.PumpAndSettle();
        Assert.Equal(Day(3), selection);
        tester.Tap(Find.Text("4"));
        tester.PumpAndSettle();
        Assert.Equal(Day(3), selection);
        tester.Tap(Find.Text("5"));
        tester.PumpAndSettle();
        Assert.Equal(Day(3), selection);
    }

    // Flutter: 'CalendarDatePicker: Updates to initialDate parameter are not reflected in the state
    // (useMaterial3=$useMaterial3)'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UpdatesToInitialDateParameterAreNotReflectedInTheState(bool useMaterial3)
    {
        using FrameworkDartTester tester = CreateTester();
        Key pickerKey = new UniqueKey();
        var initialDate = new DateTime(2020, 1, 21);
        var updatedDate = new DateTime(1976, 2, 23);
        var firstDate = new DateTime(1970, 1, 1);
        // Dart's `DateTime(2099, 31, 12)` rolls the month over: 2101-07-12.
        var lastDate = new DateTime(2101, 7, 12);
        Color selectedColor = useMaterial3
            ? new Color(0xff6750a4)
            : new Color(0xff2196f3); // default primary color

        tester.PumpWidget(CalendarDatePickerApp(
            key: pickerKey,
            useMaterial3: useMaterial3,
            initialDate: initialDate,
            firstDate: firstDate,
            lastDate: lastDate,
            onDateChanged: _ => { }));
        tester.PumpAndSettle();

        // Month should show as January 2020.
        Finds.OneWidget(Find.Text("January 2020"));
        // Selected date should be painted with a colored circle.
        PaintAssert.Paints(
            MaterialOf(tester, Find.Text("21")),
            PaintPattern.Paints.Circle(color: selectedColor, style: PaintingStyle.Fill));

        // Change to the updated initialDate.
        // This should have no effect, the initialDate is only the _initial_ date.
        tester.PumpWidget(CalendarDatePickerApp(
            key: pickerKey,
            useMaterial3: useMaterial3,
            initialDate: updatedDate,
            firstDate: firstDate,
            lastDate: lastDate,
            onDateChanged: _ => { }));
        // Wait for the page scroll animation to finish.
        tester.PumpAndSettle(TimeSpan.FromMilliseconds(200));

        // Month should show as January 2020 still.
        Finds.OneWidget(Find.Text("January 2020"));
        Finds.Nothing(Find.Text("February 1976"));
        // Selected date should be painted with a colored circle.
        PaintAssert.Paints(
            MaterialOf(tester, Find.Text("21")),
            PaintPattern.Paints.Circle(color: selectedColor, style: PaintingStyle.Fill));
    }

    // Flutter: 'CalendarDatePicker: Updates to initialCalendarMode parameter is not reflected in the state'
    [Fact]
    public void UpdatesToInitialCalendarModeParameterIsNotReflectedInTheState()
    {
        using FrameworkDartTester tester = CreateTester();
        Key pickerKey = new UniqueKey();

        tester.PumpWidget(CalendarDatePickerApp(
            key: pickerKey,
            initialDate: new DateTime(2016, 1, 15),
            initialCalendarMode: DatePickerMode.Year));
        tester.PumpAndSettle();

        // Should be in year mode.
        Finds.OneWidget(Find.Text("January 2016")); // Day/year selector
        Finds.Nothing(Find.Text("15")); // day 15 in grid
        Finds.OneWidget(Find.Text("2016")); // 2016 in year grid

        tester.PumpWidget(CalendarDatePickerApp(key: pickerKey, initialDate: new DateTime(2016, 1, 15)));
        tester.PumpAndSettle();

        // Should be in year mode still; updating an _initial_ parameter has no effect.
        Finds.OneWidget(Find.Text("January 2016")); // Day/year selector
        Finds.Nothing(Find.Text("15")); // day 15 in grid
        Finds.OneWidget(Find.Text("2016")); // 2016 in year grid
    }

    // Flutter: 'CalendarDatePicker: Dragging more than half the width should not cause a jump'
    [Fact]
    public void DraggingMoreThanHalfTheWidthShouldNotCauseAJump()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(CalendarDatePickerApp(initialDate: new DateTime(2016, 1, 15)));
        tester.PumpAndSettle();
        TestGesture gesture = tester.StartGesture(
            tester.GetCenter(Find.ByType<PageView>()),
            PointerDeviceKind.Touch);
        // This initial drag is required for the PageView to recognize the gesture, as it uses
        // DragStartBehavior.start. It does not count towards the drag distance.
        gesture.MoveBy(new Vector(100, 0));
        // Dragging for a bit less than half the width should reveal the previous month.
        gesture.MoveBy(new Vector(800.0 / 2 - 1, 0));
        tester.PumpAndSettle();
        Finds.OneWidget(Find.Text("January 2016"));
        Finds.NWidgets(Find.Text("1"), 2);
        // Dragging a bit over the half should still show both.
        gesture.MoveBy(new Vector(2, 0));
        tester.PumpAndSettle();
        Finds.OneWidget(Find.Text("December 2015"));
        Finds.NWidgets(Find.Text("1"), 2);
    }

    // ---- group('Keyboard navigation') ----

    // Flutter: 'CalendarDatePicker: Keyboard navigation: Can toggle to year mode'
    [Fact]
    public void KeyboardNavigationCanToggleToYearMode()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(CalendarDatePickerApp(initialDate: new DateTime(2016, 1, 15)));
        Finds.Nothing(Find.Text("2016"));
        Finds.OneWidget(Find.Text("January 2016"));
        // Navigate to the year selector and activate it.
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.SendKeyEvent(LogicalKeyboardKey.Space);
        tester.PumpAndSettle();
        // The years should be visible.
        Finds.OneWidget(Find.Text("2016"));
        Finds.OneWidget(Find.Text("January 2016"));
    }

    // Flutter: 'CalendarDatePicker: Keyboard navigation: Can navigate next/previous months'
    [Fact]
    public void KeyboardNavigationCanNavigateNextPreviousMonths()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(CalendarDatePickerApp(initialDate: new DateTime(2016, 1, 15)));
        Finds.OneWidget(Find.Text("January 2016"));
        // Navigate to the previous month button and activate it twice.
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.SendKeyEvent(LogicalKeyboardKey.Space);
        tester.PumpAndSettle();
        tester.SendKeyEvent(LogicalKeyboardKey.Space);
        tester.PumpAndSettle();
        // Should be showing Nov 2015
        Finds.OneWidget(Find.Text("November 2015"));

        // Navigate to the next month button and activate it four times.
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.SendKeyEvent(LogicalKeyboardKey.Space);
        tester.PumpAndSettle();
        tester.SendKeyEvent(LogicalKeyboardKey.Space);
        tester.PumpAndSettle();
        tester.SendKeyEvent(LogicalKeyboardKey.Space);
        tester.PumpAndSettle();
        tester.SendKeyEvent(LogicalKeyboardKey.Space);
        tester.PumpAndSettle();
        // Should be on Mar 2016.
        Finds.OneWidget(Find.Text("March 2016"));
    }

    // Flutter: 'CalendarDatePicker: Keyboard navigation: Can navigate date grid with arrow keys'
    [Fact]
    public void KeyboardNavigationCanNavigateDateGridWithArrowKeys()
    {
        using FrameworkDartTester tester = CreateTester();
        DateTime? selectedDate = null;
        tester.PumpWidget(CalendarDatePickerApp(
            initialDate: new DateTime(2016, 1, 15),
            onDateChanged: date => selectedDate = date));
        // Navigate to the grid.
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);

        // Navigate from Jan 15 to Jan 18 with arrow keys.
        tester.SendKeyEvent(LogicalKeyboardKey.ArrowLeft);
        tester.SendKeyEvent(LogicalKeyboardKey.ArrowUp);
        tester.SendKeyEvent(LogicalKeyboardKey.ArrowLeft);
        tester.SendKeyEvent(LogicalKeyboardKey.ArrowDown);
        tester.SendKeyEvent(LogicalKeyboardKey.ArrowDown);
        tester.SendKeyEvent(LogicalKeyboardKey.ArrowLeft);
        tester.SendKeyEvent(LogicalKeyboardKey.ArrowLeft);
        tester.PumpAndSettle();

        // Activate it.
        tester.SendKeyEvent(LogicalKeyboardKey.Space);
        tester.PumpAndSettle();

        // Should have selected Jan 18.
        Assert.Equal(new DateTime(2016, 1, 18), selectedDate);
    }

    // Flutter: 'CalendarDatePicker: Keyboard navigation: Navigating with arrow keys scrolls months'
    [Fact]
    public void KeyboardNavigationNavigatingWithArrowKeysScrollsMonths()
    {
        using FrameworkDartTester tester = CreateTester();
        DateTime? selectedDate = null;
        tester.PumpWidget(CalendarDatePickerApp(
            initialDate: new DateTime(2016, 1, 15),
            onDateChanged: date => selectedDate = date));
        // Navigate to the grid.
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.PumpAndSettle();

        // Navigate from Jan 15 to Dec 31 with arrow keys
        tester.SendKeyEvent(LogicalKeyboardKey.ArrowUp);
        tester.SendKeyEvent(LogicalKeyboardKey.ArrowUp);
        tester.SendKeyEvent(LogicalKeyboardKey.ArrowLeft);
        tester.PumpAndSettle();

        // Should have scrolled to Dec 2015.
        Finds.OneWidget(Find.Text("December 2015"));

        // Navigate from Dec 31 to Nov 26 with arrow keys.
        tester.SendKeyEvent(LogicalKeyboardKey.ArrowUp);
        tester.SendKeyEvent(LogicalKeyboardKey.ArrowUp);
        tester.SendKeyEvent(LogicalKeyboardKey.ArrowUp);
        tester.SendKeyEvent(LogicalKeyboardKey.ArrowUp);
        tester.SendKeyEvent(LogicalKeyboardKey.ArrowUp);
        tester.PumpAndSettle();

        // Should have scrolled to Nov 2015.
        Finds.OneWidget(Find.Text("November 2015"));

        // Activate it
        tester.SendKeyEvent(LogicalKeyboardKey.Space);
        tester.PumpAndSettle();

        // Should have selected Jan 18.
        Assert.Equal(new DateTime(2015, 11, 26), selectedDate);
    }

    // Flutter: 'CalendarDatePicker: Keyboard navigation: RTL text direction reverses the horizontal arrow
    // key navigation'
    [Fact]
    public void KeyboardNavigationRtlTextDirectionReversesTheHorizontalArrowKeyNavigation()
    {
        using FrameworkDartTester tester = CreateTester();
        DateTime? selectedDate = null;
        tester.PumpWidget(CalendarDatePickerApp(
            initialDate: new DateTime(2016, 1, 15),
            onDateChanged: date => selectedDate = date,
            textDirection: TextDirection.Rtl));
        // Navigate to the grid.
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.PumpAndSettle();

        // Navigate from Jan 15 to 19 with arrow keys.
        tester.SendKeyEvent(LogicalKeyboardKey.ArrowRight);
        tester.SendKeyEvent(LogicalKeyboardKey.ArrowRight);
        tester.SendKeyEvent(LogicalKeyboardKey.ArrowRight);
        tester.SendKeyEvent(LogicalKeyboardKey.ArrowRight);
        tester.SendKeyEvent(LogicalKeyboardKey.ArrowDown);
        tester.SendKeyEvent(LogicalKeyboardKey.ArrowLeft);
        tester.PumpAndSettle();

        // Activate it.
        tester.SendKeyEvent(LogicalKeyboardKey.Space);
        tester.PumpAndSettle();

        // Should have selected Jan 19.
        Assert.Equal(new DateTime(2016, 1, 19), selectedDate);
    }

    // ---- group('Haptic feedback') ----

    private static readonly TimeSpan HapticFeedbackInterval = TimeSpan.FromMilliseconds(10);

    // Flutter: 'CalendarDatePicker: Haptic feedback: Selecting date vibrates'
    [Fact]
    public void HapticFeedbackSelectingDateVibrates()
    {
        using var feedback = new FeedbackTester();
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(CalendarDatePickerApp(initialDate: new DateTime(2016, 1, 15)));
        tester.Tap(Find.Text("10"));
        tester.Pump(HapticFeedbackInterval);
        Assert.Equal(1, feedback.HapticCount);
        tester.Tap(Find.Text("12"));
        tester.Pump(HapticFeedbackInterval);
        Assert.Equal(2, feedback.HapticCount);
        tester.Tap(Find.Text("14"));
        tester.Pump(HapticFeedbackInterval);
        Assert.Equal(3, feedback.HapticCount);
    }

    // Flutter: 'CalendarDatePicker: Haptic feedback: Tapping unselectable date does not vibrate'
    [Fact]
    public void HapticFeedbackTappingUnselectableDateDoesNotVibrate()
    {
        using var feedback = new FeedbackTester();
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(CalendarDatePickerApp(
            initialDate: new DateTime(2016, 1, 10),
            selectableDayPredicate: date => date.Day % 2 == 0));
        tester.Tap(Find.Text("11"));
        tester.Pump(HapticFeedbackInterval);
        Assert.Equal(0, feedback.HapticCount);
        tester.Tap(Find.Text("13"));
        tester.Pump(HapticFeedbackInterval);
        Assert.Equal(0, feedback.HapticCount);
        tester.Tap(Find.Text("15"));
        tester.Pump(HapticFeedbackInterval);
        Assert.Equal(0, feedback.HapticCount);
    }

    // Flutter: 'CalendarDatePicker: Haptic feedback: Changing modes and year vibrates'
    [Fact]
    public void HapticFeedbackChangingModesAndYearVibrates()
    {
        using var feedback = new FeedbackTester();
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(CalendarDatePickerApp(initialDate: new DateTime(2016, 1, 15)));
        tester.Tap(Find.Text("January 2016"));
        tester.Pump(HapticFeedbackInterval);
        Assert.Equal(1, feedback.HapticCount);
        tester.Tap(Find.Text("2018"));
        tester.Pump(HapticFeedbackInterval);
        Assert.Equal(2, feedback.HapticCount);
    }

    // ---- group('Semantics') ----

    // Flutter: 'CalendarDatePicker: Semantics: day mode'
    [Fact]
    public void SemanticsDayMode()
    {
        using FrameworkDartTester tester = CreateTester();
        SemanticsHandle semantics = tester.EnsureSemantics();

        tester.PumpWidget(CalendarDatePickerApp(initialDate: new DateTime(2016, 1, 15)));

        // Year mode drop down button.
        ExpectSemantics(
            tester.GetSemantics(Find.Text("January 2016")),
            MatchesSemantics(
                label: "Select year\nJanuary 2016",
                isButton: true,
                hasTapAction: true,
                hasFocusAction: true,
                isFocusable: true));

        // Prev/Next month buttons.
        ExpectSemantics(
            tester.GetSemantics(PreviousMonthIcon),
            MatchesSemantics(
                tooltip: "Previous month",
                isButton: true,
                hasTapAction: true,
                hasFocusAction: true,
                isEnabled: true,
                hasEnabledState: true,
                isFocusable: true));
        ExpectSemantics(
            tester.GetSemantics(NextMonthIcon),
            MatchesSemantics(
                tooltip: "Next month",
                isButton: true,
                hasTapAction: true,
                hasFocusAction: true,
                isEnabled: true,
                hasEnabledState: true,
                isFocusable: true));

        // Day grid. Dart spells out days 1 through 30 one expectation at a time.
        for (int day = 1; day <= 30; day++)
        {
            var date = new DateTime(2016, 1, day);
            string today = day == 3 ? ", Today" : string.Empty;
            ExpectSemantics(
                tester.GetSemantics(Find.Text($"{day}")),
                MatchesSemantics(
                    label: $"{day}, {date.DayOfWeek}, January {day}, 2016{today}",
                    isButton: true,
                    hasEnabledState: true,
                    hasTapAction: true,
                    hasSelectedState: true,
                    hasFocusAction: true,
                    isSelected: day == 15,
                    isFocusable: true,
                    isEnabled: true));
        }

        semantics.Dispose();
    }

    // Flutter: 'CalendarDatePicker: Semantics: day mode disabled dates are announced'
    [Fact]
    public void SemanticsDayModeDisabledDatesAreAnnounced()
    {
        using FrameworkDartTester tester = CreateTester();
        SemanticsHandle semantics = tester.EnsureSemantics();

        tester.PumpWidget(CalendarDatePickerApp(
            initialDate: new DateTime(2016, 1, 15),
            firstDate: new DateTime(2016, 1, 15)));

        ExpectSemantics(
            tester.GetSemantics(Find.Text("14")),
            MatchesSemantics(
                label: "14, Thursday, January 14, 2016",
                hasEnabledState: true,
                hasSelectedState: true,
                isButton: true));
        semantics.Dispose();
    }

    // Flutter: 'CalendarDatePicker: Semantics: Disabled next and previous month buttons have meaningful
    // labels'
    [Fact]
    public void SemanticsDisabledNextAndPreviousMonthButtonsHaveMeaningfulLabels()
    {
        using FrameworkDartTester tester = CreateTester();
        SemanticsHandle semantics = tester.EnsureSemantics();

        tester.PumpWidget(CalendarDatePickerApp(
            firstDate: new DateTime(2016, 1, 14),
            initialDate: new DateTime(2016, 1, 15),
            lastDate: new DateTime(2016, 1, 16)));

        // Prev/Next month buttons.
        ExpectSemantics(
            tester.GetSemantics(PreviousMonthIcon),
            MatchesSemantics(label: "Previous month", isButton: true, hasEnabledState: true));
        ExpectSemantics(
            tester.GetSemantics(NextMonthIcon),
            MatchesSemantics(label: "Next month", isButton: true, hasEnabledState: true));

        semantics.Dispose();
    }

    // Flutter: 'CalendarDatePicker: Semantics: calendar year mode'
    [Fact]
    public void SemanticsCalendarYearMode()
    {
        using FrameworkDartTester tester = CreateTester();
        SemanticsHandle semantics = tester.EnsureSemantics();

        tester.PumpWidget(CalendarDatePickerApp(
            initialDate: new DateTime(2016, 1, 15),
            initialCalendarMode: DatePickerMode.Year));

        // Year mode drop down button.
        ExpectSemantics(
            tester.GetSemantics(Find.Text("January 2016")),
            MatchesSemantics(
                label: "Select year\nJanuary 2016",
                isButton: true,
                hasTapAction: true,
                hasFocusAction: true,
                isFocusable: true));

        // Year grid only shows 2010 - 2024.
        for (int year = 2010; year <= 2024; year++)
        {
            ExpectSemantics(
                tester.GetSemantics(Find.Text($"{year}")),
                MatchesSemantics(
                    label: $"{year}",
                    hasEnabledState: true,
                    hasTapAction: true,
                    hasFocusAction: true,
                    isSelected: year == 2016,
                    hasSelectedState: true,
                    isFocusable: true,
                    isEnabled: true,
                    isButton: true));
        }

        semantics.Dispose();
    }

    // Flutter: 'CalendarDatePicker: Semantics: calendar year mode disabled years are announced'
    [Fact]
    public void SemanticsCalendarYearModeDisabledYearsAreAnnounced()
    {
        using FrameworkDartTester tester = CreateTester();
        SemanticsHandle semantics = tester.EnsureSemantics();

        tester.PumpWidget(CalendarDatePickerApp(
            initialDate: new DateTime(2016, 1, 15),
            firstDate: new DateTime(2016, 1, 15),
            initialCalendarMode: DatePickerMode.Year));

        ExpectSemantics(
            tester.GetSemantics(Find.Text("2015")),
            MatchesSemantics(
                label: "2015",
                hasEnabledState: true,
                hasSelectedState: true,
                isButton: true));
        semantics.Dispose();
    }

    // This is a regression test for https://github.com/flutter/flutter/issues/143439.
    // Flutter: 'CalendarDatePicker: Semantics: Selected date Semantics announcement on onDateChanged'
    // (variant: TargetPlatformVariant.desktop())
    [Theory]
    [MemberData(nameof(TargetPlatformVariant.DesktopData), MemberType = typeof(TargetPlatformVariant))]
    public void SemanticsSelectedDateSemanticsAnnouncementOnOnDateChanged(TargetPlatform platform)
    {
        using IDisposable _ = TargetPlatformVariant.Override(platform);
        using FrameworkDartTester tester = CreateTester();
        SemanticsHandle semantics = tester.EnsureSemantics();
        var localizations = new DefaultMaterialLocalizations();
        var initialDate = new DateTime(2016, 1, 15);
        DateTime? selectedDate = null;

        tester.PumpWidget(CalendarDatePickerApp(
            initialDate: initialDate,
            onDateChanged: value => selectedDate = value));

        bool isToday = DateUtils.IsSameDay(initialDate, selectedDate);
        string semanticLabelSuffix = isToday ? $", {localizations.CurrentDateLabel}" : string.Empty;

        // The initial date should be announced.
        ExpectAnnouncement(
            tester.TakeAnnouncements().Last(),
            $"{localizations.FormatFullDate(initialDate)}{semanticLabelSuffix}");

        // Select a new date.
        tester.Tap(Find.Text("20"));
        tester.PumpAndSettle();

        // The selected date should be announced.
        ExpectAnnouncement(
            tester.TakeAnnouncements().Last(),
            $"{localizations.SelectedDateLabel} {localizations.FormatFullDate(selectedDate!.Value)}"
            + semanticLabelSuffix);

        // Select the initial date.
        tester.Tap(Find.Text("15"));

        // The initial date should be announced as selected.
        ExpectAnnouncement(
            tester.TakeAnnouncements().First(),
            $"{localizations.SelectedDateLabel} {localizations.FormatFullDate(initialDate)}{semanticLabelSuffix}");

        semantics.Dispose();
    }

    // This is a regression test for https://github.com/flutter/flutter/issues/141350.
    // Flutter: 'CalendarDatePicker: Default day selection overlay'
    [Fact]
    public void DefaultDaySelectionOverlay()
    {
        using FrameworkDartTester tester = CreateTester();
        var theme = new ThemeData();
        tester.PumpWidget(CalendarDatePickerApp(
            firstDate: new DateTime(2016, 12, 15),
            initialDate: new DateTime(2017, 1, 15),
            lastDate: new DateTime(2017, 2, 15),
            onDisplayedMonthChanged: _ => { },
            theme: theme));

        RenderObject inkFeatures = tester.AllRenderObjects.First(o => o is RenderInkFeatures);
        PaintAssert.DoesNotPaint(
            inkFeatures,
            PaintPattern.Paints.Circle(radius: 35.0, color: theme.ColorScheme.OnSurfaceVariant.WithOpacity(0.08)));
        Assert.Equal(0, PaintRecording.Record(inkFeatures).CountCalls("clipPath"));

        TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        gesture.AddPointer();
        gesture.MoveTo(tester.GetCenter(Find.Text("25")));
        tester.PumpAndSettle();
        inkFeatures = tester.AllRenderObjects.First(o => o is RenderInkFeatures);
        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints
                .Circle()
                .Circle(radius: 35.0, color: theme.ColorScheme.OnSurfaceVariant.WithOpacity(0.08)));
        Assert.Equal(1, PaintRecording.Record(inkFeatures).CountCalls("clipPath"));

        // Rect.fromCircle(center: const Offset(400.0, 241.0), radius: 35.0)
        var expectedClipRect = new Rect(365.0, 206.0, 70.0, 70.0);
        var expectedClipPath = new Plumix.UI.Path();
        expectedClipPath.AddRect(expectedClipRect);
        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints.ClipPath(
                coversSameAreaAs: expectedClipPath,
                areaToCompare: expectedClipRect,
                sampleSize: 100));
    }

    // Flutter: 'CalendarDatePicker: CalendarDatePicker renders at zero area'
    [Fact]
    public void CalendarDatePickerRendersAtZeroArea()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(new MaterialApp(
            home: new Scaffold(
                body: SizedBox.Shrink(
                    child: new CalendarDatePicker(
                        initialDate: new DateTime(2025, 1, 1),
                        firstDate: new DateTime(2024, 1, 1),
                        lastDate: new DateTime(2026, 1, 1),
                        onDateChanged: _ => { })))));

        tester.PumpWidget(new MaterialApp(
            home: new Scaffold(
                body: SizedBox.Shrink(
                    child: new CalendarDatePicker(
                        initialDate: new DateTime(2025, 1, 1),
                        firstDate: new DateTime(2024, 1, 1),
                        lastDate: new DateTime(2026, 1, 1),
                        onDateChanged: _ => { },
                        initialCalendarMode: DatePickerMode.Year)))));
    }

    // Flutter: 'CalendarDatePicker: Ink feature paints on inner Material'
    [Fact]
    public void InkFeaturePaintsOnInnerMaterial()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(CalendarDatePickerApp(
            firstDate: new DateTime(2025, 6, 1),
            initialDate: new DateTime(2025, 7, 20),
            lastDate: new DateTime(2025, 8, 31)));

        // Material outside the PageView.
        MaterialInkController outerMaterial = MaterialWidget.Of(
            tester.Element(Find.ByType<FocusableActionDetector>()));
        // Material directly wrapping the PageView.
        MaterialInkController innerMaterial = MaterialWidget.Of(tester.Element(Find.ByType<PageView>()));

        // Only the inner Material should have ink features.
        Assert.Null(((RenderInkFeatures)outerMaterial).DebugInkFeatures);
        Assert.Equal(31, ((RenderInkFeatures)innerMaterial).DebugInkFeatures!.Count);
    }

    // ---- group('when supportsAnnounce is true, check announcement') ----

    // Flutter: 'when supportsAnnounce is true, check announcement: Initial date announcement'
    // (variant: TargetPlatformVariant.only(TargetPlatform.android))
    [Fact]
    public void SupportsAnnounceInitialDateAnnouncement()
    {
        using IDisposable _ = TargetPlatformVariant.Override(TargetPlatform.Android);
        using FrameworkDartTester tester = CreateTester();
        SemanticsHandle semantics = tester.EnsureSemantics();
        var localizations = new DefaultMaterialLocalizations();
        var initialDate = new DateTime(2016, 1, 15);
        string expectedLabel = localizations.FormatFullDate(initialDate);

        tester.PumpWidget(new MediaQuery(
            data: new MediaQueryData(SupportsAnnounce: true),
            child: CalendarDatePickerApp(initialDate: initialDate)));

        List<CapturedAccessibilityAnnouncement> announcements = tester.TakeAnnouncements();
        Assert.Single(announcements);
        ExpectAnnouncement(announcements[0], expectedLabel, textDirection: TextDirection.Ltr);

        semantics.Dispose();
    }

    // Flutter: 'when supportsAnnounce is true, check announcement: CalendarDatePicker reports error when
    // SemanticsService.sendAnnouncement fails during navigation'
    // (variant: TargetPlatformVariant.only(TargetPlatform.android))
    [Fact]
    public void SupportsAnnounceReportsErrorWhenSendAnnouncementFailsDuringNavigation()
    {
        using IDisposable _ = TargetPlatformVariant.Override(TargetPlatform.Android);
        using FrameworkDartTester tester = CreateTester();
        var errors = new List<FlutterErrorDetails>();
        FlutterExceptionHandler? originalOnError = FlutterError.OnError;
        FlutterError.OnError = details => errors.Add(details);
        try
        {
            tester.Binding.SetMockMessageHandler(
                SystemChannels.Accessibility.Name,
                message =>
                {
                    var codec = new StandardMessageCodec();
                    object? decoded = codec.DecodeMessage(message);
                    if (decoded is IDictionary<object, object?> map
                        && map.TryGetValue("type", out object? type)
                        && Equals(type, "announce"))
                    {
                        var data = new ByteData(1);
                        data.Buffer[0] = 255; // Invalid type byte
                        return Task.FromResult<ByteData?>(data);
                    }

                    return Task.FromResult<ByteData?>(null); // Success for other events
                });

            var initialDate = new DateTime(2016, 1, 15);

            tester.PumpWidget(new MediaQuery(
                data: new MediaQueryData(SupportsAnnounce: true),
                child: CalendarDatePickerApp(initialDate: initialDate)));

            tester.Tap(NextMonthIcon);
            tester.Pump();

            Assert.NotEmpty(errors);
            bool hasAnnouncementError = errors.Any(e =>
                e.Exception.ToString()!.Contains("FormatException", StringComparison.Ordinal)
                && e.Context!.ToString().Contains("while sending semantics announcement", StringComparison.Ordinal));
            Assert.True(hasAnnouncementError);
        }
        finally
        {
            FlutterError.OnError = originalOnError;
            tester.Binding.SetMockMessageHandler(SystemChannels.Accessibility.Name, null);
        }
    }

    // Flutter: 'when supportsAnnounce is true, check announcement: Month navigation announcement on Android'
    [Fact]
    public void SupportsAnnounceMonthNavigationAnnouncementOnAndroid()
    {
        using IDisposable _ = TargetPlatformVariant.Override(TargetPlatform.Android);
        using FrameworkDartTester tester = CreateTester();
        SemanticsHandle semantics = tester.EnsureSemantics();
        var initialDate = new DateTime(2016, 1, 15);

        tester.PumpWidget(new MediaQuery(
            data: new MediaQueryData(SupportsAnnounce: true),
            child: CalendarDatePickerApp(initialDate: initialDate)));

        // Clear any initial announcements.
        tester.TakeAnnouncements();

        // Tap next month.
        tester.Tap(NextMonthIcon);
        tester.PumpAndSettle();

        const string expectedLabel = "February 2016";
        List<CapturedAccessibilityAnnouncement> announcements = tester.TakeAnnouncements();
        Assert.Single(announcements);
        ExpectAnnouncement(announcements[0], expectedLabel, textDirection: TextDirection.Ltr);

        semantics.Dispose();
    }

    // Flutter: 'when supportsAnnounce is true, check announcement: Mode toggle announcement on Android'
    [Fact]
    public void SupportsAnnounceModeToggleAnnouncementOnAndroid()
    {
        using IDisposable _ = TargetPlatformVariant.Override(TargetPlatform.Android);
        using FrameworkDartTester tester = CreateTester();
        SemanticsHandle semantics = tester.EnsureSemantics();
        var initialDate = new DateTime(2016, 1, 15);

        tester.PumpWidget(new MediaQuery(
            data: new MediaQueryData(SupportsAnnounce: true),
            child: CalendarDatePickerApp(initialDate: initialDate)));

        // Clear any initial announcements.
        tester.TakeAnnouncements();

        // Switch to year mode.
        Finder modeToggleButton = Find.ByTypeName("DatePickerModeToggleButton");
        tester.Tap(modeToggleButton);
        tester.PumpAndSettle();

        const string yearLabel = "2016";
        List<CapturedAccessibilityAnnouncement> announcements = tester.TakeAnnouncements();
        Assert.Single(announcements);
        ExpectAnnouncement(announcements[0], yearLabel, textDirection: TextDirection.Ltr);

        // Switch back to day mode.
        tester.Tap(modeToggleButton);
        tester.PumpAndSettle();

        const string dayLabel = "January 2016";
        announcements = tester.TakeAnnouncements();
        Assert.Single(announcements);
        ExpectAnnouncement(announcements[0], dayLabel, textDirection: TextDirection.Ltr);

        semantics.Dispose();
    }

    // ---- group('when supportsAnnounce is false, use live region to announce') ----

    private static Finder LiveRegion(string label) => Find.ByWidgetPredicate(
        widget => widget is Semantics semantics
                  && semantics.Properties.Label == label
                  && (semantics.Properties.LiveRegion ?? false));

    // Flutter: 'when supportsAnnounce is false, use live region to announce: Initial date announcement'
    [Fact]
    public void LiveRegionInitialDateAnnouncement()
    {
        using IDisposable _ = TargetPlatformVariant.Override(TargetPlatform.Android);
        using FrameworkDartTester tester = CreateTester();
        SemanticsHandle semantics = tester.EnsureSemantics();
        var localizations = new DefaultMaterialLocalizations();
        var initialDate = new DateTime(2016, 1, 15);
        string expectedLabel = localizations.FormatFullDate(initialDate);

        tester.PumpWidget(new MediaQuery(
            data: new MediaQueryData(),
            child: CalendarDatePickerApp(initialDate: initialDate)));

        // Verify that the live region exists and has the correct label.
        ExpectSemantics(
            tester.GetSemantics(LiveRegion(expectedLabel)),
            MatchesSemantics(label: expectedLabel, isLiveRegion: true));

        semantics.Dispose();
    }

    // Flutter: 'when supportsAnnounce is false, use live region to announce: Month navigation announcement
    // on Android'
    [Fact]
    public void LiveRegionMonthNavigationAnnouncementOnAndroid()
    {
        using IDisposable _ = TargetPlatformVariant.Override(TargetPlatform.Android);
        using FrameworkDartTester tester = CreateTester();
        SemanticsHandle semantics = tester.EnsureSemantics();
        var initialDate = new DateTime(2016, 1, 15);

        tester.PumpWidget(new MediaQuery(
            data: new MediaQueryData(),
            child: CalendarDatePickerApp(initialDate: initialDate)));

        // Tap next month.
        tester.Tap(NextMonthIcon);
        tester.PumpAndSettle();

        // The _MonthPicker live region should be updated.
        // Verify that the live region exists and has the correct label.
        const string expectedLabel = "February 2016";
        ExpectSemantics(
            tester.GetSemantics(LiveRegion(expectedLabel)),
            MatchesSemantics(label: expectedLabel, isLiveRegion: true));

        semantics.Dispose();
    }

    // Flutter: 'when supportsAnnounce is false, use live region to announce: Mode toggle announcement on
    // Android'
    [Fact]
    public void LiveRegionModeToggleAnnouncementOnAndroid()
    {
        using IDisposable _ = TargetPlatformVariant.Override(TargetPlatform.Android);
        using FrameworkDartTester tester = CreateTester();
        SemanticsHandle semantics = tester.EnsureSemantics();
        var initialDate = new DateTime(2016, 1, 15);

        tester.PumpWidget(new MediaQuery(
            data: new MediaQueryData(),
            child: CalendarDatePickerApp(initialDate: initialDate)));

        // Switch to year mode.
        Finder modeToggleButton = Find.ByTypeName("DatePickerModeToggleButton");
        tester.Tap(modeToggleButton);
        tester.PumpAndSettle();

        // Verify that the live region exists and has the correct label.
        const string yearLabel = "2016";
        ExpectSemantics(
            tester.GetSemantics(LiveRegion(yearLabel)),
            MatchesSemantics(label: yearLabel, isLiveRegion: true));

        // Switch back to day mode.
        tester.Tap(modeToggleButton);
        tester.PumpAndSettle();

        // Verify that the live region exists and has the correct label.
        const string dayLabel = "January 2016";
        ExpectSemantics(
            tester.GetSemantics(LiveRegion(dayLabel)),
            MatchesSemantics(label: dayLabel, isLiveRegion: true));

        semantics.Dispose();
    }

    // ---- group('YearPicker') ----

    // Flutter: 'YearPicker: Current year is visible in year picker'
    [Fact]
    public void YearPickerCurrentYearIsVisibleInYearPicker()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(YearPickerApp());
        Finds.OneWidget(Find.Text("2016"));
    }

    // Flutter: 'YearPicker: Can select a year'
    [Fact]
    public void YearPickerCanSelectAYear()
    {
        using FrameworkDartTester tester = CreateTester();
        DateTime? selectedDate = null;
        tester.PumpWidget(YearPickerApp(onChanged: date => selectedDate = date));
        tester.PumpAndSettle();
        tester.Tap(Find.Text("2018"));
        tester.PumpAndSettle();
        Assert.Equal(new DateTime(2018, 1, 1), selectedDate);
    }

    // Flutter: 'YearPicker: Cannot select disabled year'
    [Fact]
    public void YearPickerCannotSelectDisabledYear()
    {
        using FrameworkDartTester tester = CreateTester();
        DateTime? selectedYear = null;
        tester.PumpWidget(YearPickerApp(
            firstDate: new DateTime(2018, 6, 9),
            selectedDate: new DateTime(2018, 7, 4),
            lastDate: new DateTime(2018, 12, 15),
            onChanged: date => selectedYear = date));
        tester.Tap(Find.Text("2016")); // Disabled, doesn't change the year.
        tester.PumpAndSettle();
        Assert.Null(selectedYear);
        tester.Tap(Find.Text("2020")); // Disabled, doesn't change the year.
        tester.PumpAndSettle();
        Assert.Null(selectedYear);
        tester.Tap(Find.Text("2018"));
        tester.PumpAndSettle();
        Assert.Equal(new DateTime(2018, 7, 1), selectedYear);
    }

    // Flutter: 'YearPicker: Selecting year with no selected month uses earliest month'
    [Fact]
    public void YearPickerSelectingYearWithNoSelectedMonthUsesEarliestMonth()
    {
        using FrameworkDartTester tester = CreateTester();
        DateTime? selectedYear = null;
        tester.PumpWidget(YearPickerApp(
            firstDate: new DateTime(2018, 6, 9),
            lastDate: new DateTime(2019, 12, 15),
            onChanged: date => selectedYear = date));
        tester.Tap(Find.Text("2018"));
        Assert.Equal(new DateTime(2018, 6, 1), selectedYear);
        tester.PumpWidget(YearPickerApp(
            firstDate: new DateTime(2018, 6, 9),
            lastDate: new DateTime(2019, 12, 15),
            selectedDate: new DateTime(2018, 6, 1),
            onChanged: date => selectedYear = date));
        tester.Tap(Find.Text("2019"));
        Assert.Equal(new DateTime(2019, 6, 1), selectedYear);
    }

    // Flutter: 'YearPicker: Selecting year with no selected month uses January'
    [Fact]
    public void YearPickerSelectingYearWithNoSelectedMonthUsesJanuary()
    {
        using FrameworkDartTester tester = CreateTester();
        DateTime? selectedYear = null;
        tester.PumpWidget(YearPickerApp(
            firstDate: new DateTime(2018, 6, 9),
            lastDate: new DateTime(2019, 12, 15),
            onChanged: date => selectedYear = date));
        tester.Tap(Find.Text("2019"));
        Assert.Equal(new DateTime(2019, 1, 1), selectedYear); // january implied
        tester.PumpWidget(YearPickerApp(
            firstDate: new DateTime(2018, 6, 9),
            lastDate: new DateTime(2019, 12, 15),
            selectedDate: new DateTime(2018, 1, 1),
            onChanged: date => selectedYear = date));
        tester.Tap(Find.Text("2018"));
        Assert.Equal(new DateTime(2018, 6, 1), selectedYear);
    }

    // Flutter: 'YearPicker: YearPicker renders at zero area'
    [Fact]
    public void YearPickerRendersAtZeroArea()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(new MaterialApp(
            home: new Scaffold(
                body: SizedBox.Shrink(
                    child: new YearPicker(
                        selectedDate: new DateTime(2025, 1, 1),
                        firstDate: new DateTime(2024, 1, 1),
                        lastDate: new DateTime(2026, 1, 1),
                        onChanged: _ => { })))));
    }

    // Regression test for https://github.com/flutter/flutter/issues/155198.
    // Flutter: 'YearPicker: Ink features are painted on inner Material'
    [Fact]
    public void YearPickerInkFeaturesArePaintedOnInnerMaterial()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(YearPickerApp(
            firstDate: new DateTime(2020, 1, 1),
            lastDate: new DateTime(2030, 1, 1),
            selectedDate: new DateTime(2025, 1, 1)));

        Finds.NWidgets(Find.ByType<MaterialWidget>(), 2);

        // Material outside the GridView.
        MaterialInkController outerMaterial = MaterialWidget.Of(tester.Element(Find.ByType<YearPicker>()));
        // Material directly wrapping the GridView.
        MaterialInkController innerMaterial = MaterialWidget.Of(tester.Element(Find.ByType<GridView>()));

        Assert.NotSame(outerMaterial, innerMaterial);
        Assert.Null(((RenderInkFeatures)outerMaterial).DebugInkFeatures);
        Assert.Null(((RenderInkFeatures)innerMaterial).DebugInkFeatures);

        // Hover over the 2022 year item to trigger the ink highlight.
        TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        gesture.AddPointer(location: tester.GetCenter(Find.Text("2022")));
        try
        {
            tester.Pump();

            // Only the inner Material should have ink features.
            Assert.Null(((RenderInkFeatures)outerMaterial).DebugInkFeatures);
            Assert.Single(((RenderInkFeatures)innerMaterial).DebugInkFeatures!);
        }
        finally
        {
            gesture.RemovePointer();
        }
    }

    // ---- group('Calendar Delegate') ----

    // Flutter: 'Calendar Delegate: Defaults to Gregorian calendar system'
    [Fact]
    public void CalendarDelegateDefaultsToGregorianCalendarSystem()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(
                child: new CalendarDatePicker(
                    initialDate: new DateTime(2025, 2, 26),
                    firstDate: new DateTime(2025, 2, 1),
                    lastDate: new DateTime(2025, 5, 1),
                    onDateChanged: _ => { }))));

        CalendarDatePicker calendarPicker = tester.Widget<CalendarDatePicker>(Find.ByType<CalendarDatePicker>());
        Assert.IsAssignableFrom<GregorianCalendarDelegate>(calendarPicker.CalendarDelegate);

        Finder datePickerModeToggleButton = Find.Descendant(
            of: Find.ByType<InkWell>(),
            matching: Find.Text("February 2025"));
        tester.Tap(datePickerModeToggleButton);
        tester.PumpAndSettle();

        YearPicker yearPicker = tester.Widget<YearPicker>(Find.ByType<YearPicker>());
        Assert.IsAssignableFrom<GregorianCalendarDelegate>(yearPicker.CalendarDelegate);
    }

    // Flutter: 'Calendar Delegate: Using custom calendar delegate implementation'
    [Fact]
    public void CalendarDelegateUsingCustomCalendarDelegateImplementation()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(
                child: new CalendarDatePicker(
                    initialDate: new DateTime(2025, 2, 26),
                    firstDate: new DateTime(2025, 2, 1),
                    lastDate: new DateTime(2025, 5, 1),
                    onDateChanged: _ => { },
                    calendarDelegate: new TestCalendarDelegate()))));

        CalendarDatePicker calendarPicker = tester.Widget<CalendarDatePicker>(Find.ByType<CalendarDatePicker>());
        Assert.IsType<TestCalendarDelegate>(calendarPicker.CalendarDelegate);

        Finder datePickerModeToggleButton = Find.Descendant(
            of: Find.ByType<InkWell>(),
            matching: Find.Text("February 2025"));
        tester.Tap(datePickerModeToggleButton);
        tester.PumpAndSettle();

        YearPicker yearPicker = tester.Widget<YearPicker>(Find.ByType<YearPicker>());
        Assert.IsType<TestCalendarDelegate>(yearPicker.CalendarDelegate);
    }

    // ---- C#-only coverage of calendar_date_picker.dart members Flutter's tests do not reach ----

    [Fact]
    public void CalendarDatePickerStoresDateOnlyValues()
    {
        var picker = new CalendarDatePicker(
            initialDate: new DateTime(2026, 3, 12, 14, 0, 0),
            firstDate: new DateTime(2026, 1, 1, 9, 0, 0),
            lastDate: new DateTime(2026, 12, 31, 23, 0, 0),
            currentDate: new DateTime(2026, 3, 13, 8, 0, 0),
            onDateChanged: _ => { });

        Assert.Equal(new DateTime(2026, 3, 12), picker.InitialDate);
        Assert.Equal(new DateTime(2026, 1, 1), picker.FirstDate);
        Assert.Equal(new DateTime(2026, 12, 31), picker.LastDate);
        Assert.Equal(new DateTime(2026, 3, 13), picker.CurrentDate);
        Assert.Equal(DatePickerMode.Day, picker.InitialCalendarMode);
        Assert.Same(GregorianCalendarDelegate.Instance, picker.CalendarDelegate);
    }

    [DebugOnlyFact]
    public void CalendarDatePickerAssertsItsDateContract()
    {
        Assert.Throws<AssertionError>(() => new CalendarDatePicker(
            null, new DateTime(2026, 2, 1), new DateTime(2026, 1, 1), _ => { }));
        Assert.Throws<AssertionError>(() => new CalendarDatePicker(
            new DateTime(2025, 12, 31), new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), _ => { }));
        Assert.Throws<AssertionError>(() => new CalendarDatePicker(
            new DateTime(2027, 1, 1), new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), _ => { }));
        Assert.Throws<AssertionError>(() => new CalendarDatePicker(
            new DateTime(2026, 1, 2),
            new DateTime(2026, 1, 1),
            new DateTime(2026, 12, 31),
            _ => { },
            selectableDayPredicate: day => day.Day % 2 == 1));
        Assert.Throws<AssertionError>(() => new YearPicker(
            firstDate: new DateTime(2026, 2, 1),
            lastDate: new DateTime(2026, 1, 1),
            selectedDate: null,
            onChanged: _ => { }));
    }

    [Theory]
    [InlineData(true, 400.0, 800.0, 52.0 + (48.0 * 7))]
    [InlineData(true, 800.0, 600.0, 52.0 + (42.0 * 7))]
    [InlineData(false, 400.0, 800.0, 52.0 + (42.0 * 7))]
    public void MonthPickerHeightFollowsOrientationAndMaterialVersion(
        bool useMaterial3,
        double width,
        double height,
        double expectedHeight)
    {
        using FrameworkDartTester tester = CreateTester();
        tester.View.PhysicalSize = new Size(width, height);
        tester.View.DevicePixelRatio = 1.0;
        try
        {
            tester.PumpWidget(CalendarDatePickerApp(
                useMaterial3: useMaterial3,
                initialDate: new DateTime(2016, 1, 15)));

            Assert.Equal(expectedHeight, tester.GetSize(Find.ByTypeName("MonthPicker")).Height);
        }
        finally
        {
            tester.View.Reset();
        }
    }

    private sealed class TestCalendarDelegate : GregorianCalendarDelegate
    {
        public override int GetDaysInMonth(int year, int month)
        {
            return month % 2 == 0 ? 21 : 28;
        }

        public override int FirstDayOffset(int year, int month, MaterialLocalizations localizations)
        {
            return 1;
        }
    }
}
