using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: cupertino_ui/test/date_picker_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoDatePickerTests : IDisposable
{
    private static readonly Size ViewSize = new(400.0, 300.0);

    public CupertinoDatePickerTests()
    {
        Scheduler.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
    }

    [Fact]
    public void DatePickerDefaultsAndValidationMatchFlutterContracts()
    {
        var before = DateTime.Now;
        var picker = new CupertinoDatePicker(_ => { });
        var after = DateTime.Now;

        Assert.Equal(CupertinoDatePickerMode.DateAndTime, picker.Mode);
        Assert.InRange(picker.InitialDateTime, before, after);
        Assert.Equal(1, picker.MinimumYear);
        Assert.Null(picker.MaximumYear);
        Assert.Equal(1, picker.MinuteInterval);
        Assert.False(picker.Use24hFormat);
        Assert.Null(picker.DateOrder);
        Assert.Null(picker.BackgroundColor);
        Assert.False(picker.ShowDayOfWeek);
        Assert.False(picker.ShowTimeSeparator);
        Assert.Equal(32.0, picker.ItemExtent);
        Assert.Equal(ChangeReportingBehavior.OnScrollUpdate, picker.ChangeReportingBehavior);

        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoDatePicker(_ => { }, itemExtent: 0.0));
        Assert.Throws<ArgumentException>(() => new CupertinoDatePicker(_ => { }, minuteInterval: 7));
        Assert.Throws<ArgumentException>(() => new CupertinoDatePicker(
            _ => { },
            initialDateTime: new DateTime(2025, 1, 1, 10, 1, 0),
            minuteInterval: 5));
        Assert.Throws<ArgumentException>(() => new CupertinoDatePicker(
            _ => { },
            mode: CupertinoDatePickerMode.Time,
            showDayOfWeek: true));
        Assert.Throws<ArgumentException>(() => new CupertinoDatePicker(
            _ => { },
            mode: CupertinoDatePickerMode.Date,
            showTimeSeparator: true));
        Assert.Throws<ArgumentException>(() => new CupertinoDatePicker(
            _ => { },
            initialDateTime: new DateTime(2025, 6, 14),
            selectableDayPredicate: date => date.Day >= 16));
    }

    [Fact]
    public void TimerPickerDefaultsAndValidationMatchFlutterContracts()
    {
        var picker = new CupertinoTimerPicker(_ => { });

        Assert.Equal(CupertinoTimerPickerMode.Hms, picker.Mode);
        Assert.Equal(TimeSpan.Zero, picker.InitialTimerDuration);
        Assert.Equal(1, picker.MinuteInterval);
        Assert.Equal(1, picker.SecondInterval);
        Assert.Equal(Alignment.Center, picker.Alignment.Resolve(TextDirection.Ltr));
        Assert.Null(picker.BackgroundColor);
        Assert.Equal(32.0, picker.ItemExtent);
        Assert.Equal(ChangeReportingBehavior.OnScrollUpdate, picker.ChangeReportingBehavior);

        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoTimerPicker(
            _ => { },
            initialTimerDuration: TimeSpan.FromDays(1.0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoTimerPicker(
            _ => { },
            initialTimerDuration: TimeSpan.FromSeconds(-1.0)));
        Assert.Throws<ArgumentException>(() => new CupertinoTimerPicker(_ => { }, minuteInterval: 7));
        Assert.Throws<ArgumentException>(() => new CupertinoTimerPicker(_ => { }, secondInterval: 7));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CupertinoTimerPicker(_ => { }, itemExtent: 0.0));
    }

    [Theory]
    [InlineData(CupertinoDatePickerMode.MonthYear, 2)]
    [InlineData(CupertinoDatePickerMode.Date, 3)]
    [InlineData(CupertinoDatePickerMode.Time, 3)]
    [InlineData(CupertinoDatePickerMode.DateAndTime, 4)]
    public void DatePickerModesComposeExpectedColumnsAndCustomOverlays(
        CupertinoDatePickerMode mode,
        int expectedColumns)
    {
        int overlayCount = 0;
        var picker = new CupertinoDatePicker(
            _ => { },
            mode: mode,
            initialDateTime: new DateTime(2018, 9, 15, 3, 14, 0),
            selectionOverlayBuilder: (_, columnCount, selectedIndex) =>
            {
                Assert.Equal(expectedColumns, columnCount);
                Assert.InRange(selectedIndex, 0, expectedColumns - 1);
                overlayCount++;
                return new ColoredBox(Colors.CornflowerBlue);
            });
        using var harness = new CupertinoThemeTestHarness(Wrap(picker));

        harness.Pump(ViewSize);

        Assert.Equal(expectedColumns, harness.FindWidgets<CupertinoPicker>().Count);
        Assert.Equal(expectedColumns, overlayCount);
        Assert.Equal(expectedColumns, harness.FindWidgets<ColoredBox>().Count);
        if (mode == CupertinoDatePickerMode.Date)
        {
            Assert.Contains(harness.FindWidgets<Text>(), text => text.Data == "September");
            Assert.Contains(harness.FindWidgets<Text>(), text => text.Data == "15");
            Assert.Contains(harness.FindWidgets<Text>(), text => text.Data == "2018");
        }

        if (mode == CupertinoDatePickerMode.MonthYear)
        {
            Assert.Contains(harness.FindWidgets<Text>(), text => text.Data == "September");
            Assert.Contains(harness.FindWidgets<Text>(), text => text.Data == "2018");
        }
    }

    [Theory]
    [InlineData(CupertinoTimerPickerMode.Hm, 2, 320.0)]
    [InlineData(CupertinoTimerPickerMode.Ms, 2, 320.0)]
    [InlineData(CupertinoTimerPickerMode.Hms, 3, 342.0)]
    public void TimerPickerModesUseIntrinsicGeometryAndOverlayCount(
        CupertinoTimerPickerMode mode,
        int expectedColumns,
        double expectedWidth)
    {
        int overlayCount = 0;
        var picker = new CupertinoTimerPicker(
            _ => { },
            mode: mode,
            initialTimerDuration: new TimeSpan(2, 30, 48),
            minuteInterval: 10,
            secondInterval: 12,
            itemExtent: 42.0,
            backgroundColor: CupertinoColors.Black,
            selectionOverlayBuilder: (_, columnCount, selectedIndex) =>
            {
                Assert.Equal(expectedColumns, columnCount);
                Assert.InRange(selectedIndex, 0, expectedColumns - 1);
                overlayCount++;
                return new ColoredBox(Colors.CornflowerBlue);
            });
        using var harness = new CupertinoThemeTestHarness(Wrap(picker));

        harness.Pump(new Size(800.0, 300.0));

        Assert.Equal(expectedColumns, harness.FindWidgets<CupertinoPicker>().Count);
        Assert.Equal(expectedColumns, overlayCount);
        Assert.All(harness.FindWidgets<CupertinoPicker>(), wheel =>
        {
            Assert.Equal(42.0, wheel.ItemExtent);
            Assert.Equal(CupertinoColors.Black, wheel.BackgroundColor?.Value);
        });
        Assert.Contains(harness.FindWidgets<SizedBox>(), box => box.Width == expectedWidth && box.Height == 216.0);
    }

    [Fact]
    public void DateAndTimerCallbacksReportCompleteValues()
    {
        DateTime? changedDate = null;
        using var dateHarness = new CupertinoThemeTestHarness(Wrap(new CupertinoDatePicker(
            value => changedDate = value,
            mode: CupertinoDatePickerMode.Date,
            initialDateTime: new DateTime(2018, 9, 15))));
        dateHarness.Pump(ViewSize);
        CupertinoPicker dayPicker = Assert.Single(
            dateHarness.FindWidgets<CupertinoPicker>(),
            wheel => wheel.ScrollController?.InitialItem == 14);
        dayPicker.OnSelectedItemChanged!.Invoke(15);
        Assert.Equal(new DateTime(2018, 9, 16), changedDate);

        TimeSpan? changedDuration = null;
        using var timerHarness = new CupertinoThemeTestHarness(Wrap(new CupertinoTimerPicker(
            value => changedDuration = value,
            initialTimerDuration: new TimeSpan(1, 2, 3))));
        timerHarness.Pump(ViewSize);
        CupertinoPicker hourPicker = Assert.Single(
            timerHarness.FindWidgets<CupertinoPicker>(),
            wheel => wheel.ChildDelegate.EstimatedChildCount == 24);
        hourPicker.OnSelectedItemChanged!.Invoke(2);
        Assert.Equal(new TimeSpan(2, 2, 3), changedDuration);
    }

    [Fact]
    public void BoundsAndPredicateExcludeInvalidDateEntries()
    {
        using var harness = new CupertinoThemeTestHarness(Wrap(new CupertinoDatePicker(
            _ => { },
            mode: CupertinoDatePickerMode.Date,
            initialDateTime: new DateTime(2025, 6, 16),
            minimumDate: new DateTime(2025, 6, 15),
            selectableDayPredicate: date => date.Day >= 16)));

        harness.Pump(ViewSize);

        Assert.NotEmpty(harness.FindWidgets<ExcludeSemantics>());
        Assert.Contains(
            harness.FindWidgets<Text>(),
            text => text.Data == "15" && text.Style?.Color == CupertinoColors.InactiveGray.Color);
        Assert.Contains(
            harness.FindWidgets<Text>(),
            text => text.Data == "16" && text.Style?.Color != CupertinoColors.InactiveGray.Color);
    }

    [Fact]
    public void SeparatorDynamicThemeAndZeroAreaMatchFlutterBehavior()
    {
        using var dark = new CupertinoThemeTestHarness(Wrap(
            new CupertinoDatePicker(
                _ => { },
                mode: CupertinoDatePickerMode.Time,
                initialDateTime: new DateTime(2025, 1, 1, 12, 15, 0),
                showTimeSeparator: true),
            PlatformBrightness.Dark));
        dark.Pump(ViewSize);
        Assert.Single(dark.FindWidgets<Text>(), text => text.Data == ":");
        Assert.Contains(
            dark.FindWidgets<Text>(),
            text => text.Style?.Color == CupertinoColors.Label.DarkColor);

        using var zero = new CupertinoThemeTestHarness(Wrap(new SizedBox(
            width: 0.0,
            height: 0.0,
            child: new CupertinoTimerPicker(_ => { }))));
        zero.Pump(new Size());
        Assert.Contains(zero.FindWidgets<SizedBox>(), box => box.Width == 0.0 && box.Height == 0.0);
    }

    [Fact]
    public void GetColumnWidthMeasuresTheWidestText()
    {
        double? narrow = null;
        double? wide = null;
        using var harness = new CupertinoThemeTestHarness(Wrap(new Builder(context =>
        {
            narrow = CupertinoDatePicker.GetColumnWidth(["11"], context);
            wide = CupertinoDatePicker.GetColumnWidth(["11", "September"], context);
            return new SizedBox();
        })));

        harness.Pump(ViewSize);

        Assert.NotNull(narrow);
        Assert.NotNull(wide);
        Assert.True(wide > narrow);
    }

    [Fact]
    public void PickerModesCannotChangeAfterTheFirstBuild()
    {
        DateTime initialDate = new(2025, 6, 16, 10, 30, 0);
        using var dateHarness = new CupertinoThemeTestHarness(Wrap(new CupertinoDatePicker(
            _ => { },
            mode: CupertinoDatePickerMode.Date,
            initialDateTime: initialDate)));
        dateHarness.Pump(ViewSize);
        Assert.Throws<InvalidOperationException>(() => dateHarness.PumpWidget(Wrap(new CupertinoDatePicker(
            _ => { },
            mode: CupertinoDatePickerMode.MonthYear,
            initialDateTime: initialDate))));

        using var timerHarness = new CupertinoThemeTestHarness(Wrap(new CupertinoTimerPicker(
            _ => { },
            mode: CupertinoTimerPickerMode.Hm)));
        timerHarness.Pump(ViewSize);
        Assert.Throws<InvalidOperationException>(() => timerHarness.PumpWidget(Wrap(new CupertinoTimerPicker(
            _ => { },
            mode: CupertinoTimerPickerMode.Hms))));
    }

    private static Widget Wrap(
        Widget child,
        PlatformBrightness brightness = PlatformBrightness.Light)
    {
        return new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                new MediaQueryData(PlatformBrightness: brightness),
                new Localizations(
                    locale: new Locale("en", "US"),
                    delegates:
                    [
                        DefaultCupertinoLocalizations.Delegate,
                        DefaultWidgetsLocalizations.Delegate,
                    ],
                    child: new CupertinoTheme(
                        new CupertinoThemeData(brightness: brightness),
                        new SizedBox(
                            width: ViewSize.Width,
                            height: ViewSize.Height,
                            child: child)))));
    }
}
