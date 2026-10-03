// Dart parity source: material_ui/lib/src/date_picker.dart
// Mirrors material-ui-src/test/date_range_picker_test.dart

using System.Collections;
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
public sealed class DateRangePickerDartParityTests : IDisposable
{
    private static readonly Size WideWindowSize = new(1920.0, 1080.0);
    private static readonly Size NarrowWindowSize = new(1070.0, 1770.0);

    // Dart's `late` test-scope variables, reset by `setUp` (xUnit builds a fresh instance per test).
    private DateTime _firstDate = new(2015, 1, 1);
    private DateTime _lastDate = new(2016, 12, 31);
    private DateTime? _currentDate;
    private DateTimeRange<DateTime>? _initialDateRange = new(
        start: new DateTime(2016, 1, 15),
        end: new DateTime(2016, 1, 25));
    private DatePickerEntryMode _initialEntryMode = DatePickerEntryMode.Calendar;

    private string? _cancelText;
    private string? _confirmText;
    private string? _errorInvalidRangeText;
    private string? _errorFormatText;
    private string? _errorInvalidText;
    private string? _fieldStartHintText;
    private string? _fieldEndHintText;
    private string? _fieldStartLabelText;
    private string? _fieldEndLabelText;
    private string? _helpText;
    private string? _saveText;

    public DateRangePickerDartParityTests()
    {
        FocusManager.Instance.ResetForTests();
        // flutter_test: defaultTargetPlatform == android, debugDisableShadows == true.
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
    }

    public void Dispose()
    {
        PlatformDefaults.DebugTargetPlatformOverride = null;
        FocusManager.Instance.ResetForTests();
    }

    private static FrameworkDartTester CreateTester() =>
        new(fakeGestureTimers: true, semanticsEnabled: true, registerTestTextInput: true);

    private void PreparePicker(
        FrameworkDartTester tester,
        Action<Task<DateTimeRange<DateTime>?>> callback,
        TextDirection textDirection = TextDirection.Ltr,
        bool useMaterial3 = false,
        SelectableDayForRangePredicate? selectableDayPredicate = null,
        CalendarDelegate<DateTime>? calendarDelegate = null)
    {
        BuildContext? buttonContext = null;
        tester.PumpWidget(new MaterialApp(
            theme: new ThemeData(useMaterial3: useMaterial3),
            home: new MaterialWidget(
                child: new Builder(context => new ElevatedButton(
                    onPressed: () => buttonContext = context,
                    child: new Text("Go"))))));

        tester.Tap(Find.Text("Go"));
        Assert.NotNull(buttonContext);

        Task<DateTimeRange<DateTime>?> range = MaterialDatePickers.ShowDateRangePicker(
            context: buttonContext!,
            initialDateRange: _initialDateRange,
            firstDate: _firstDate,
            lastDate: _lastDate,
            currentDate: _currentDate,
            initialEntryMode: _initialEntryMode,
            cancelText: _cancelText,
            confirmText: _confirmText,
            errorInvalidRangeText: _errorInvalidRangeText,
            errorFormatText: _errorFormatText,
            errorInvalidText: _errorInvalidText,
            fieldStartHintText: _fieldStartHintText,
            fieldEndHintText: _fieldEndHintText,
            fieldStartLabelText: _fieldStartLabelText,
            fieldEndLabelText: _fieldEndLabelText,
            helpText: _helpText,
            saveText: _saveText,
            selectableDayPredicate: selectableDayPredicate,
            builder: (_, child) => new Directionality(textDirection, child ?? new SizedBox()),
            calendarDelegate: calendarDelegate ?? GregorianCalendarDelegate.Instance);

        tester.PumpAndSettle(TimeSpan.FromSeconds(1));
        callback(range);
    }

    // Dart's `await range`: the dialog future completes when the route pops.
    private static T? Await<T>(Task<T?> task)
    {
        if (!task.IsCompleted)
        {
            Scheduler.FlushMicrotasks();
        }

        Assert.True(task.IsCompleted, "The date range picker future has not completed.");
        return task.Result;
    }

    private static DateTimeRange<DateTime> Range(DateTime start, DateTime end) => new(start: start, end: end);

    private static void AssertPoint(double expected, double actual, double epsilon = 1e-5) =>
        Assert.InRange(actual, expected - epsilon, expected + epsilon);

    private static MaterialWidget DialogMaterial(FrameworkDartTester tester) =>
        tester.Widget<MaterialWidget>(
            Find.Descendant(of: Find.ByType<Dialog>(), matching: Find.ByType<MaterialWidget>()).First);

    // Flutter: 'date_range_picker_test.dart: Default layout (calendar mode)'
    [Fact]
    public void DefaultLayoutCalendarMode()
    {
        using FrameworkDartTester tester = CreateTester();
        PreparePicker(tester, _ =>
        {
            Finder helpText = Find.Text("Select range");
            Finder firstDateHeaderText = Find.Text("Jan 15");
            Finder lastDateHeaderText = Find.Text("Jan 25, 2016");
            Finder saveText = Find.Text("Save");

            Finds.OneWidget(helpText);
            Finds.OneWidget(firstDateHeaderText);
            Finds.OneWidget(lastDateHeaderText);
            Finds.OneWidget(saveText);

            // Test the close button position.
            Point closeButtonBottomRight = tester.GetBottomRight(
                Find.Ancestor(of: Find.ByType<IconButton>(), matching: Find.ByType<Center>()));
            Point helpTextTopLeft = tester.GetTopLeft(helpText);
            Assert.Equal(56.0, closeButtonBottomRight.X);
            Assert.Equal(helpTextTopLeft.Y, closeButtonBottomRight.Y);

            // Test the save and entry buttons position.
            Point saveButtonBottomLeft = tester.GetBottomLeft(Find.ByType<TextButton>());
            Point entryButtonBottomLeft = tester.GetBottomLeft(
                Find.WidgetWithIcon<IconButton>(Icons.EditOutlined));
            AssertPoint(711.6, saveButtonBottomLeft.X);
            Assert.Equal(helpTextTopLeft.Y, saveButtonBottomLeft.Y);
            Assert.Equal(saveButtonBottomLeft.X - 48.0, entryButtonBottomLeft.X);
            Assert.Equal(helpTextTopLeft.Y, entryButtonBottomLeft.Y);

            // Test help text position.
            Point helpTextBottomLeft = tester.GetBottomLeft(helpText);
            Assert.Equal(72.0, helpTextBottomLeft.X);
            Assert.Equal(closeButtonBottomRight.Y + 20.0, helpTextBottomLeft.Y);

            // Test the header position.
            Point firstDateHeaderTopLeft = tester.GetTopLeft(firstDateHeaderText);
            Point lastDateHeaderTopLeft = tester.GetTopLeft(lastDateHeaderText);
            Assert.Equal(72.0, firstDateHeaderTopLeft.X);
            Assert.Equal(helpTextBottomLeft.Y + 8.0, firstDateHeaderTopLeft.Y);
            Point firstDateHeaderTopRight = tester.GetTopRight(firstDateHeaderText);
            Assert.Equal(firstDateHeaderTopRight.X + 66.0, lastDateHeaderTopLeft.X);
            Assert.Equal(helpTextBottomLeft.Y + 8.0, lastDateHeaderTopLeft.Y);

            // Test the day headers position.
            Point dayHeadersGridTopLeft = tester.GetTopLeft(Find.ByType<GridView>().First);
            Point firstDateHeaderBottomLeft = tester.GetBottomLeft(firstDateHeaderText);
            Assert.Equal((800 - 384) / 2.0, dayHeadersGridTopLeft.X);
            Assert.Equal(firstDateHeaderBottomLeft.Y + 16.0, dayHeadersGridTopLeft.Y);

            // Test the calendar custom scroll view position.
            Point calendarScrollViewTopLeft = tester.GetTopLeft(Find.ByType<CustomScrollView>());
            Point dayHeadersGridBottomLeft = tester.GetBottomLeft(Find.ByType<GridView>().First);
            Assert.Equal(0.0, calendarScrollViewTopLeft.X);
            Assert.Equal(dayHeadersGridBottomLeft.Y, calendarScrollViewTopLeft.Y);
        }, useMaterial3: true);
    }

    // Flutter: 'date_range_picker_test.dart: Default Dialog properties (calendar mode)'
    [Fact]
    public void DefaultDialogPropertiesCalendarMode()
    {
        using FrameworkDartTester tester = CreateTester();
        var theme = new ThemeData();
        PreparePicker(tester, _ =>
        {
            MaterialWidget dialogMaterial = DialogMaterial(tester);

            Assert.Equal(theme.ColorScheme.SurfaceContainerHigh, dialogMaterial.Color);
            Assert.Equal(MaterialColors.Transparent, dialogMaterial.ShadowColor);
            Assert.Equal(MaterialColors.Transparent, dialogMaterial.SurfaceTintColor);
            Assert.Equal(0.0, dialogMaterial.Elevation);
            Assert.Equal(new RoundedRectangleBorder(), dialogMaterial.Shape);
            Assert.Equal(Clip.AntiAlias, dialogMaterial.ClipBehavior);

            Dialog dialog = tester.Widget<Dialog>(Find.ByType<Dialog>());
            Assert.Equal(new Thickness(0), dialog.InsetPadding);
        }, useMaterial3: theme.UseMaterial3);
    }

    // Flutter: 'date_range_picker_test.dart: Default Dialog properties (input mode)'
    // (Dart's top-level copy, which runs in calendar mode.)
    [Fact]
    public void DefaultDialogPropertiesInputModeTopLevel()
    {
        using FrameworkDartTester tester = CreateTester();
        var theme = new ThemeData();
        PreparePicker(tester, _ =>
        {
            MaterialWidget dialogMaterial = DialogMaterial(tester);

            Assert.Equal(theme.ColorScheme.SurfaceContainerHigh, dialogMaterial.Color);
            Assert.Equal(MaterialColors.Transparent, dialogMaterial.ShadowColor);
            Assert.Equal(MaterialColors.Transparent, dialogMaterial.SurfaceTintColor);
            Assert.Equal(0.0, dialogMaterial.Elevation);
            Assert.Equal(new RoundedRectangleBorder(), dialogMaterial.Shape);
            Assert.Equal(Clip.AntiAlias, dialogMaterial.ClipBehavior);

            Dialog dialog = tester.Widget<Dialog>(Find.ByType<Dialog>());
            Assert.Equal(new Thickness(0), dialog.InsetPadding);
        }, useMaterial3: theme.UseMaterial3);
    }

    // Flutter: 'date_range_picker_test.dart: Scaffold and AppBar defaults'
    [Fact]
    public void ScaffoldAndAppBarDefaults()
    {
        using FrameworkDartTester tester = CreateTester();
        var theme = new ThemeData();
        PreparePicker(tester, _ =>
        {
            Scaffold scaffold = tester.Widget<Scaffold>(Find.ByType<Scaffold>());
            Assert.Null(scaffold.BackgroundColor);

            AppBar appBar = tester.Widget<AppBar>(Find.ByType<AppBar>());
            var iconTheme = new IconThemeData(Color: theme.ColorScheme.OnSurfaceVariant);
            Assert.Equal(iconTheme, appBar.IconTheme);
            Assert.Equal(iconTheme, appBar.ActionsIconTheme);
            Assert.Equal(0, appBar.Elevation);
            Assert.Equal(0, appBar.ScrolledUnderElevation);
            Assert.Equal(MaterialColors.Transparent, appBar.BackgroundColor);
        }, useMaterial3: theme.UseMaterial3);
    }

    // group('Landscape input-only date picker headers use headlineSmall')
    // Regression test for https://github.com/flutter/flutter/issues/122056

    // Common screen size roughly based on a Pixel 1
    private static readonly Size KCommonScreenSizePortrait = new(1070, 1770);
    private static readonly Size KCommonScreenSizeLandscape = new(1770, 1070);

    private void ShowInputPicker(FrameworkDartTester tester, Size size)
    {
        tester.View.PhysicalSize = size;
        tester.View.DevicePixelRatio = 1.0;
        _initialEntryMode = DatePickerEntryMode.Input;
        PreparePicker(tester, _ => { }, useMaterial3: true);
    }

    // Flutter: 'date_range_picker_test.dart: Landscape input-only date picker headers use headlineSmall portrait'
    [Fact]
    public void LandscapeInputOnlyDatePickerHeadersUseHeadlineSmallPortrait()
    {
        using FrameworkDartTester tester = CreateTester();
        try
        {
            ShowInputPicker(tester, KCommonScreenSizePortrait);
            Assert.Equal(32, tester.Widget<Text>(Find.Text("Jan 15 – Jan 25, 2016")).Style?.FontSize);
            tester.Tap(Find.Text("Cancel"));
            tester.PumpAndSettle();
        }
        finally
        {
            tester.View.Reset();
        }
    }

    // Flutter: 'date_range_picker_test.dart: Landscape input-only date picker headers use headlineSmall landscape'
    [Fact]
    public void LandscapeInputOnlyDatePickerHeadersUseHeadlineSmallLandscape()
    {
        using FrameworkDartTester tester = CreateTester();
        try
        {
            ShowInputPicker(tester, KCommonScreenSizeLandscape);
            Assert.Equal(24, tester.Widget<Text>(Find.Text("Jan 15 – Jan 25, 2016")).Style?.FontSize);
            tester.Tap(Find.Text("Cancel"));
            tester.PumpAndSettle();
        }
        finally
        {
            tester.View.Reset();
        }
    }

    // Flutter: 'date_range_picker_test.dart: Save and help text is used'
    [Fact]
    public void SaveAndHelpTextIsUsed()
    {
        using FrameworkDartTester tester = CreateTester();
        _helpText = "help";
        _saveText = "make it so";
        PreparePicker(tester, _ =>
        {
            Finds.OneWidget(Find.Text(_helpText!));
            Finds.OneWidget(Find.Text(_saveText!));
        });
    }

    // Flutter: 'date_range_picker_test.dart: Long helpText does not cutoff the save button'
    [Fact]
    public void LongHelpTextDoesNotCutoffTheSaveButton()
    {
        using FrameworkDartTester tester = CreateTester();
        _helpText = string.Concat(Enumerable.Repeat("long helpText", 100));
        _saveText = "make it so";
        PreparePicker(tester, _ =>
        {
            Finds.OneWidget(Find.Text(_helpText!));
            Finds.OneWidget(Find.Text(_saveText!));
            Assert.Null(tester.TakeException());
        });
    }

    // Flutter: 'date_range_picker_test.dart: Material3 has sentence case labels'
    [Fact]
    public void Material3HasSentenceCaseLabels()
    {
        using FrameworkDartTester tester = CreateTester();
        PreparePicker(tester, _ =>
        {
            Finds.OneWidget(Find.Text("Save"));
            Finds.OneWidget(Find.Text("Select range"));
        }, useMaterial3: true);
    }

    // Flutter: 'date_range_picker_test.dart: Initial date is the default'
    [Fact]
    public void InitialDateIsTheDefault()
    {
        using FrameworkDartTester tester = CreateTester();
        PreparePicker(tester, range =>
        {
            tester.Tap(Find.Text("SAVE"));
            Assert.Equal(Range(new DateTime(2016, 1, 15), new DateTime(2016, 1, 25)), Await(range));
        });
    }

    // Flutter: 'date_range_picker_test.dart: Last month header should be visible if last date is selected'
    [Fact]
    public void LastMonthHeaderShouldBeVisibleIfLastDateIsSelected()
    {
        using FrameworkDartTester tester = CreateTester();
        _firstDate = new DateTime(2015, 1, 1);
        _lastDate = new DateTime(2016, 12, 31);
        _initialDateRange = Range(_lastDate, _lastDate);
        PreparePicker(tester, _ =>
        {
            // December header should be showing, but no November
            Finds.OneWidget(Find.Text("December 2016"));
            Finds.Nothing(Find.Text("November 2016"));
        });
    }

    // Flutter: 'date_range_picker_test.dart: First month header should be visible if first date is selected'
    [Fact]
    public void FirstMonthHeaderShouldBeVisibleIfFirstDateIsSelected()
    {
        using FrameworkDartTester tester = CreateTester();
        _firstDate = new DateTime(2015, 1, 1);
        _lastDate = new DateTime(2016, 12, 31);
        _initialDateRange = Range(_firstDate, _firstDate);
        PreparePicker(tester, _ =>
        {
            // January and February headers should be showing, but no March
            Finds.OneWidget(Find.Text("January 2015"));
            Finds.OneWidget(Find.Text("February 2015"));
            Finds.Nothing(Find.Text("March 2015"));
        });
    }

    // Flutter: 'date_range_picker_test.dart: Current month header should be visible if no date is selected'
    [Fact]
    public void CurrentMonthHeaderShouldBeVisibleIfNoDateIsSelected()
    {
        using FrameworkDartTester tester = CreateTester();
        _firstDate = new DateTime(2015, 1, 1);
        _lastDate = new DateTime(2016, 12, 31);
        _currentDate = new DateTime(2016, 9, 1);
        _initialDateRange = null;

        PreparePicker(tester, _ =>
        {
            // September and October headers should be showing, but no August
            Finds.OneWidget(Find.Text("September 2016"));
            Finds.OneWidget(Find.Text("October 2016"));
            Finds.Nothing(Find.Text("August 2016"));
        });
    }

    // Flutter: 'date_range_picker_test.dart: Can cancel'
    [Fact]
    public void CanCancel()
    {
        using FrameworkDartTester tester = CreateTester();
        PreparePicker(tester, range =>
        {
            tester.Tap(Find.ByIcon(Icons.Close));
            Assert.Null(Await(range));
        });
    }

    // Flutter: 'date_range_picker_test.dart: Can select a range'
    [Fact]
    public void CanSelectARange()
    {
        using FrameworkDartTester tester = CreateTester();
        PreparePicker(tester, range =>
        {
            tester.Tap(Find.Text("12").First);
            tester.Tap(Find.Text("14").First);
            tester.Tap(Find.Text("SAVE"));
            Assert.Equal(Range(new DateTime(2016, 1, 12), new DateTime(2016, 1, 14)), Await(range));
        });
    }

    // Flutter: 'date_range_picker_test.dart: Tapping earlier date resets selected range'
    [Fact]
    public void TappingEarlierDateResetsSelectedRange()
    {
        using FrameworkDartTester tester = CreateTester();
        PreparePicker(tester, range =>
        {
            tester.Tap(Find.Text("12").First);
            tester.Tap(Find.Text("11").First);
            tester.Tap(Find.Text("15").First);
            tester.Tap(Find.Text("SAVE"));
            Assert.Equal(Range(new DateTime(2016, 1, 11), new DateTime(2016, 1, 15)), Await(range));
        });
    }

    // Flutter: 'date_range_picker_test.dart: Can select single day range'
    [Fact]
    public void CanSelectSingleDayRange()
    {
        using FrameworkDartTester tester = CreateTester();
        PreparePicker(tester, range =>
        {
            tester.Tap(Find.Text("12").First);
            tester.Tap(Find.Text("12").First);
            tester.Tap(Find.Text("SAVE"));
            Assert.Equal(Range(new DateTime(2016, 1, 12), new DateTime(2016, 1, 12)), Await(range));
        });
    }

    // Flutter: 'date_range_picker_test.dart: Cannot select a day outside bounds'
    [Fact]
    public void CannotSelectADayOutsideBounds()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialDateRange = Range(new DateTime(2017, 1, 13), new DateTime(2017, 1, 15));
        _firstDate = new DateTime(2017, 1, 12);
        _lastDate = new DateTime(2017, 1, 16);
        PreparePicker(tester, range =>
        {
            // Earlier than firstDate. Should be ignored.
            tester.Tap(Find.Text("10"));
            // Later than lastDate. Should be ignored.
            tester.Tap(Find.Text("20"));
            tester.Tap(Find.Text("SAVE"));
            // We should still be on the initial date.
            Assert.Equal(_initialDateRange, Await(range));
        });
    }

    // Flutter: 'date_range_picker_test.dart: Can select a range even if the range includes non selectable days'
    [Fact]
    public void CanSelectARangeEvenIfTheRangeIncludesNonSelectableDays()
    {
        using FrameworkDartTester tester = CreateTester();
        PreparePicker(tester, range =>
        {
            tester.Tap(Find.Text("12").First);
            tester.Tap(Find.Text("14").First);
            tester.Tap(Find.Text("SAVE"));
            // The day 13 is not selectable, but the range is still valid.
            Assert.Equal(Range(new DateTime(2016, 1, 12), new DateTime(2016, 1, 14)), Await(range));
        }, selectableDayPredicate: (day, _, _) => day.Day != 13);
    }

    // Flutter: 'date_range_picker_test.dart: Cannot select a day inside bounds but not selectable'
    [Fact]
    public void CannotSelectADayInsideBoundsButNotSelectable()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialDateRange = Range(new DateTime(2017, 1, 13), new DateTime(2017, 1, 14));
        _firstDate = new DateTime(2017, 1, 12);
        _lastDate = new DateTime(2017, 1, 16);
        PreparePicker(tester, range =>
        {
            // Non-selectable date. Should be ignored.
            tester.Tap(Find.Text("15"));
            tester.Tap(Find.Text("SAVE"));
            // We should still be on the initial date.
            Assert.Equal(_initialDateRange, Await(range));
        }, selectableDayPredicate: (day, _, _) => day.Day != 15);
    }

    // Flutter: 'date_range_picker_test.dart: Selectable date becoming non selectable when selected start day'
    [Fact]
    public void SelectableDateBecomingNonSelectableWhenSelectedStartDay()
    {
        using FrameworkDartTester tester = CreateTester();
        PreparePicker(
            tester,
            range =>
            {
                tester.Tap(Find.Text("12").First);
                tester.PumpAndSettle();
                tester.Tap(Find.Text("11").First);
                tester.PumpAndSettle();
                tester.Tap(Find.Text("14").First);
                tester.PumpAndSettle();
                tester.Tap(Find.Text("SAVE"));
                Assert.Equal(Range(new DateTime(2016, 1, 12), new DateTime(2016, 1, 14)), Await(range));
            },
            selectableDayPredicate: (day, selectedStart, selectedEnd) =>
            {
                if (selectedEnd == null && selectedStart != null)
                {
                    return day == selectedStart || day > selectedStart.Value;
                }

                return true;
            });
    }

    // Flutter: 'date_range_picker_test.dart: selectableDayPredicate should be called with the selected start and
    // end dates'
    [Fact]
    public void SelectableDayPredicateShouldBeCalledWithTheSelectedStartAndEndDates()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialDateRange = Range(new DateTime(2017, 1, 13), new DateTime(2017, 1, 15));
        _firstDate = new DateTime(2017, 1, 12);
        _lastDate = new DateTime(2017, 1, 16);
        PreparePicker(
            tester,
            _ => { },
            selectableDayPredicate: (_, selectedStartDate, selectedEndDate) =>
            {
                Assert.Equal(new DateTime(2017, 1, 13), selectedStartDate);
                Assert.Equal(new DateTime(2017, 1, 15), selectedEndDate);
                return true;
            });
    }

    // Flutter: 'date_range_picker_test.dart: Can switch from calendar to input entry mode'
    [Fact]
    public void CanSwitchFromCalendarToInputEntryMode()
    {
        using FrameworkDartTester tester = CreateTester();
        PreparePicker(tester, _ =>
        {
            Finds.Nothing(Find.ByType<TextField>());
            tester.Tap(Find.ByIcon(Icons.Edit));
            tester.PumpAndSettle();
            Finds.NWidgets(Find.ByType<TextField>(), 2);
        });
    }

    // Flutter: 'date_range_picker_test.dart: Can switch from input to calendar entry mode'
    [Fact]
    public void CanSwitchFromInputToCalendarEntryMode()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialEntryMode = DatePickerEntryMode.Input;
        PreparePicker(tester, _ =>
        {
            Finds.NWidgets(Find.ByType<TextField>(), 2);
            tester.Tap(Find.ByIcon(Icons.CalendarToday));
            tester.PumpAndSettle();
            Finds.Nothing(Find.ByType<TextField>());
        });
    }

    // Flutter: 'date_range_picker_test.dart: Can not switch out of calendarOnly mode'
    [Fact]
    public void CanNotSwitchOutOfCalendarOnlyMode()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialEntryMode = DatePickerEntryMode.CalendarOnly;
        PreparePicker(tester, _ =>
        {
            Finds.Nothing(Find.ByType<TextField>());
            Finds.Nothing(Find.ByIcon(Icons.Edit));
        });
    }

    // Flutter: 'date_range_picker_test.dart: Can not switch out of inputOnly mode'
    [Fact]
    public void CanNotSwitchOutOfInputOnlyMode()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialEntryMode = DatePickerEntryMode.InputOnly;
        PreparePicker(tester, _ =>
        {
            Finds.NWidgets(Find.ByType<TextField>(), 2);
            Finds.Nothing(Find.ByIcon(Icons.CalendarToday));
        });
    }

    // Flutter: 'date_range_picker_test.dart: Input only mode should validate date'
    [Fact]
    public void InputOnlyModeShouldValidateDate()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialEntryMode = DatePickerEntryMode.InputOnly;
        _errorInvalidText = "oops";
        PreparePicker(tester, _ =>
        {
            tester.EnterText(Find.ByType<TextField>().At(0), "08/08/2014");
            tester.EnterText(Find.ByType<TextField>().At(1), "08/08/2014");
            Finds.Nothing(Find.Text(_errorInvalidText!));

            tester.Tap(Find.Text("OK"));
            tester.PumpAndSettle();
            Finds.NWidgets(Find.Text(_errorInvalidText!), 2);
        });
    }

    // Flutter: 'date_range_picker_test.dart: Switching to input mode keeps selected date'
    [Fact]
    public void SwitchingToInputModeKeepsSelectedDate()
    {
        using FrameworkDartTester tester = CreateTester();
        PreparePicker(tester, range =>
        {
            tester.Tap(Find.Text("12").First);
            tester.Tap(Find.Text("14").First);
            tester.Tap(Find.ByIcon(Icons.Edit));
            tester.PumpAndSettle();
            tester.Tap(Find.Text("OK"));
            Assert.Equal(Range(new DateTime(2016, 1, 12), new DateTime(2016, 1, 14)), Await(range));
        });
    }

    // ---- group('Toggle from input entry mode validates dates') ----

    // Flutter: 'date_range_picker_test.dart: Toggle from input entry mode validates dates Invalid start date'
    [Fact]
    public void ToggleFromInputEntryModeValidatesDatesInvalidStartDate()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialEntryMode = DatePickerEntryMode.Input;
        // Invalid start date should have neither a start nor end date selected in
        // calendar mode
        PreparePicker(tester, _ =>
        {
            tester.EnterText(Find.ByType<TextField>().At(0), "12/27/1918");
            tester.EnterText(Find.ByType<TextField>().At(1), "12/25/2016");
            tester.Tap(Find.ByIcon(Icons.CalendarToday));
            tester.PumpAndSettle();

            Finds.OneWidget(Find.Text("Start Date"));
            Finds.OneWidget(Find.Text("End Date"));
        });
    }

    // Flutter: 'date_range_picker_test.dart: Toggle from input entry mode validates dates Non-selectable start date'
    [Fact]
    public void ToggleFromInputEntryModeValidatesDatesNonSelectableStartDate()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialEntryMode = DatePickerEntryMode.Input;
        // Even if start and end dates are selected, the start date is not selectable
        // ending up to no date selected at all in calendar mode.
        PreparePicker(
            tester,
            _ =>
            {
                tester.EnterText(Find.ByType<TextField>().At(0), "12/24/2016");
                tester.EnterText(Find.ByType<TextField>().At(1), "12/25/2016");
                tester.Tap(Find.ByIcon(Icons.CalendarToday));
                tester.PumpAndSettle();

                Finds.OneWidget(Find.Text("Start Date"));
                Finds.OneWidget(Find.Text("End Date"));
            },
            selectableDayPredicate: (day, _, _) => day != new DateTime(2016, 12, 24));
    }

    // Flutter: 'date_range_picker_test.dart: Toggle from input entry mode validates dates Invalid end date'
    [Fact]
    public void ToggleFromInputEntryModeValidatesDatesInvalidEndDate()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialEntryMode = DatePickerEntryMode.Input;
        // Invalid end date should only have a start date selected
        PreparePicker(tester, _ =>
        {
            tester.EnterText(Find.ByType<TextField>().At(0), "12/24/2016");
            tester.EnterText(Find.ByType<TextField>().At(1), "12/25/2050");
            tester.Tap(Find.ByIcon(Icons.CalendarToday));
            tester.PumpAndSettle();

            Finds.OneWidget(Find.Text("Dec 24"));
            Finds.OneWidget(Find.Text("End Date"));
        });
    }

    // Flutter: 'date_range_picker_test.dart: Toggle from input entry mode validates dates Non-selectable end date'
    [Fact]
    public void ToggleFromInputEntryModeValidatesDatesNonSelectableEndDate()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialEntryMode = DatePickerEntryMode.Input;
        // The end date is not selectable, so only the start date should be selected.
        PreparePicker(
            tester,
            _ =>
            {
                tester.EnterText(Find.ByType<TextField>().At(0), "12/24/2016");
                tester.EnterText(Find.ByType<TextField>().At(1), "12/25/2016");
                tester.Tap(Find.ByIcon(Icons.CalendarToday));
                tester.PumpAndSettle();

                Finds.OneWidget(Find.Text("Dec 24"));
                Finds.OneWidget(Find.Text("End Date"));
            },
            selectableDayPredicate: (day, _, _) => day != new DateTime(2016, 12, 25));
    }

    // Flutter: 'date_range_picker_test.dart: Toggle from input entry mode validates dates Invalid range'
    [Fact]
    public void ToggleFromInputEntryModeValidatesDatesInvalidRange()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialEntryMode = DatePickerEntryMode.Input;
        // Start date after end date should just use the start date
        PreparePicker(tester, _ =>
        {
            tester.EnterText(Find.ByType<TextField>().At(0), "12/25/2016");
            tester.EnterText(Find.ByType<TextField>().At(1), "12/24/2016");
            tester.Tap(Find.ByIcon(Icons.CalendarToday));
            tester.PumpAndSettle();

            Finds.OneWidget(Find.Text("Dec 25"));
            Finds.OneWidget(Find.Text("End Date"));
        });
    }

    // Flutter: 'date_range_picker_test.dart: OK Cancel button layout'
    [Fact]
    public void OkCancelButtonLayout()
    {
        using FrameworkDartTester tester = CreateTester();

        Widget BuildFrame(TextDirection textDirection)
        {
            return new MaterialApp(
                theme: new ThemeData(useMaterial3: false),
                home: new MaterialWidget(
                    child: new Center(
                        child: new Builder(context => new ElevatedButton(
                            child: new Text("X"),
                            onPressed: () =>
                            {
                                _ = MaterialDatePickers.ShowDateRangePicker(
                                    context: context,
                                    firstDate: new DateTime(2001, 1, 1),
                                    lastDate: new DateTime(2031, 12, 31),
                                    builder: (_, child) => new Directionality(
                                        textDirection,
                                        child ?? new SizedBox()));
                            })))));
        }

        void ShowOkCancelDialog(TextDirection textDirection)
        {
            tester.PumpWidget(BuildFrame(textDirection));
            tester.Tap(Find.Text("X"));
            tester.PumpAndSettle();
            tester.Tap(Find.ByIcon(Icons.Edit));
            tester.PumpAndSettle();
        }

        void DismissOkCancelDialog()
        {
            tester.Tap(Find.Text("CANCEL"));
            tester.PumpAndSettle();
        }

        ShowOkCancelDialog(TextDirection.Ltr);
        Assert.Equal(622, tester.GetBottomRight(Find.Text("OK")).X);
        Assert.Equal(594, tester.GetBottomLeft(Find.Text("OK")).X);
        Assert.Equal(560, tester.GetBottomRight(Find.Text("CANCEL")).X);
        DismissOkCancelDialog();

        ShowOkCancelDialog(TextDirection.Rtl);
        Assert.Equal(206, tester.GetBottomRight(Find.Text("OK")).X);
        Assert.Equal(178, tester.GetBottomLeft(Find.Text("OK")).X);
        Assert.Equal(324, tester.GetBottomRight(Find.Text("CANCEL")).X);
        DismissOkCancelDialog();
    }

    // ---- group('Haptic feedback') ----

    private static readonly TimeSpan HapticFeedbackInterval = TimeSpan.FromMilliseconds(10);

    private void SetUpHapticFeedback()
    {
        _initialDateRange = Range(new DateTime(2017, 1, 15), new DateTime(2017, 1, 17));
        _firstDate = new DateTime(2017, 1, 10);
        _lastDate = new DateTime(2018, 1, 20);
    }

    // Flutter: 'date_range_picker_test.dart: Haptic feedback Selecting dates vibrates'
    [Fact]
    public void HapticFeedbackSelectingDatesVibrates()
    {
        using var feedback = new FeedbackTester();
        SetUpHapticFeedback();
        using FrameworkDartTester tester = CreateTester();
        PreparePicker(tester, _ =>
        {
            tester.Tap(Find.Text("10").First);
            tester.Pump(HapticFeedbackInterval);
            Assert.Equal(1, feedback.HapticCount);
            tester.Tap(Find.Text("12").First);
            tester.Pump(HapticFeedbackInterval);
            Assert.Equal(2, feedback.HapticCount);
            tester.Tap(Find.Text("14").First);
            tester.Pump(HapticFeedbackInterval);
            Assert.Equal(3, feedback.HapticCount);
        });
    }

    // Flutter: 'date_range_picker_test.dart: Haptic feedback Tapping unselectable date does not vibrate'
    [Fact]
    public void HapticFeedbackTappingUnselectableDateDoesNotVibrate()
    {
        using var feedback = new FeedbackTester();
        SetUpHapticFeedback();
        using FrameworkDartTester tester = CreateTester();
        PreparePicker(tester, _ =>
        {
            tester.Tap(Find.Text("8").First);
            tester.Pump(HapticFeedbackInterval);
            Assert.Equal(0, feedback.HapticCount);
        });
    }

    // ---- group('Keyboard navigation') ----

    // Flutter: 'date_range_picker_test.dart: Keyboard navigation Can toggle to calendar entry mode'
    [Fact]
    public void KeyboardNavigationCanToggleToCalendarEntryMode()
    {
        using FrameworkDartTester tester = CreateTester();
        PreparePicker(tester, _ =>
        {
            Finds.Nothing(Find.ByType<TextField>());
            // Navigate to the entry toggle button and activate it
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);
            tester.SendKeyEvent(LogicalKeyboardKey.Space);
            tester.PumpAndSettle();
            // Should be in the input mode
            Finds.NWidgets(Find.ByType<TextField>(), 2);
        });
    }

    // Flutter: 'date_range_picker_test.dart: Keyboard navigation Can navigate date grid with arrow keys'
    [Fact]
    public void KeyboardNavigationCanNavigateDateGridWithArrowKeys()
    {
        using FrameworkDartTester tester = CreateTester();
        PreparePicker(tester, range =>
        {
            // Navigate to the grid
            SendKeys(tester, LogicalKeyboardKey.Tab, 4);
            tester.PumpAndSettle();

            // Navigate from Jan 15 to Jan 18 with arrow keys
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowLeft);
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowUp);
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowLeft);
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowDown);
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowDown);
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowLeft);
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowLeft);
            tester.PumpAndSettle();

            // Activate it to select the beginning of the range
            tester.SendKeyEvent(LogicalKeyboardKey.Space);
            tester.PumpAndSettle();

            // Navigate to Jan 29
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowDown);
            SendKeys(tester, LogicalKeyboardKey.ArrowRight, 4);
            tester.PumpAndSettle();

            // Activate it to select the end of the range
            tester.SendKeyEvent(LogicalKeyboardKey.Space);
            tester.PumpAndSettle();

            // Navigate out of the grid and to the OK button
            SendKeys(tester, LogicalKeyboardKey.Tab, 3);

            // Activate OK
            tester.SendKeyEvent(LogicalKeyboardKey.Space);
            tester.PumpAndSettle();

            // Should have selected Jan 18 - Jan 29
            Assert.Equal(Range(new DateTime(2016, 1, 18), new DateTime(2016, 1, 29)), Await(range));
        });
    }

    // Flutter: 'date_range_picker_test.dart: Keyboard navigation Navigating with arrow keys scrolls as needed'
    [Fact]
    public void KeyboardNavigationNavigatingWithArrowKeysScrollsAsNeeded()
    {
        using FrameworkDartTester tester = CreateTester();
        PreparePicker(tester, range =>
        {
            // Jan and Feb headers should be showing, but no March
            Finds.OneWidget(Find.Text("January 2016"));
            Finds.OneWidget(Find.Text("February 2016"));
            Finds.Nothing(Find.Text("March 2016"));

            // Navigate to the grid
            SendKeys(tester, LogicalKeyboardKey.Tab, 4);

            // Navigate from Jan 15 to Jan 18 with arrow keys
            SendKeys(tester, LogicalKeyboardKey.ArrowLeft, 4);
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowDown);
            tester.PumpAndSettle();

            // Activate it to select the beginning of the range
            tester.SendKeyEvent(LogicalKeyboardKey.Space);
            tester.PumpAndSettle();

            // Navigate to Mar 17
            SendKeys(tester, LogicalKeyboardKey.ArrowDown, 8);
            SendKeys(tester, LogicalKeyboardKey.ArrowRight, 3);
            tester.PumpAndSettle();

            // Jan should have scrolled off, Mar should be visible
            Finds.Nothing(Find.Text("January 2016"));
            Finds.OneWidget(Find.Text("February 2016"));
            Finds.OneWidget(Find.Text("March 2016"));

            // Activate it to select the end of the range
            tester.SendKeyEvent(LogicalKeyboardKey.Space);
            tester.PumpAndSettle();

            // Navigate out of the grid and to the OK button
            SendKeys(tester, LogicalKeyboardKey.Tab, 3);

            // Activate OK
            tester.SendKeyEvent(LogicalKeyboardKey.Space);
            tester.PumpAndSettle();

            // Should have selected Jan 18 - Mar 17
            Assert.Equal(Range(new DateTime(2016, 1, 18), new DateTime(2016, 3, 17)), Await(range));
        });
    }

    // Flutter: 'date_range_picker_test.dart: Keyboard navigation RTL text direction reverses the horizontal arrow
    // key navigation'
    [Fact]
    public void KeyboardNavigationRtlTextDirectionReversesTheHorizontalArrowKeyNavigation()
    {
        using FrameworkDartTester tester = CreateTester();
        PreparePicker(tester, range =>
        {
            // Navigate to the grid
            SendKeys(tester, LogicalKeyboardKey.Tab, 4);

            // Navigate from Jan 15 to 19 with arrow keys
            SendKeys(tester, LogicalKeyboardKey.ArrowRight, 4);
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowDown);
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowLeft);
            tester.PumpAndSettle();

            // Activate it
            tester.SendKeyEvent(LogicalKeyboardKey.Space);
            tester.PumpAndSettle();

            // Navigate to Jan 21
            SendKeys(tester, LogicalKeyboardKey.ArrowLeft, 2);
            tester.PumpAndSettle();

            // Activate it
            tester.SendKeyEvent(LogicalKeyboardKey.Space);
            tester.PumpAndSettle();

            // Navigate out of the grid and to the OK button
            SendKeys(tester, LogicalKeyboardKey.Tab, 3);

            // Activate OK
            tester.SendKeyEvent(LogicalKeyboardKey.Space);
            tester.PumpAndSettle();

            // Should have selected Jan 19 - Mar 21
            Assert.Equal(Range(new DateTime(2016, 1, 19), new DateTime(2016, 1, 21)), Await(range));
        }, textDirection: TextDirection.Rtl);
    }

    private static void SendKeys(FrameworkDartTester tester, LogicalKeyboardKey key, int count)
    {
        for (int i = 0; i < count; i++)
        {
            tester.SendKeyEvent(key);
        }
    }

    // ---- group('Input mode') ----

    private void SetUpInputMode()
    {
        _firstDate = new DateTime(2015, 1, 1);
        _lastDate = new DateTime(2017, 12, 31);
        _initialDateRange = Range(new DateTime(2017, 1, 15), new DateTime(2017, 1, 17));
        _initialEntryMode = DatePickerEntryMode.Input;
    }

    // Flutter: 'date_range_picker_test.dart: Input mode Default Dialog properties (input mode)'
    [Fact]
    public void InputModeDefaultDialogPropertiesInputMode()
    {
        SetUpInputMode();
        using FrameworkDartTester tester = CreateTester();
        var theme = new ThemeData();
        PreparePicker(tester, _ =>
        {
            MaterialWidget dialogMaterial = DialogMaterial(tester);

            Assert.Equal(theme.ColorScheme.SurfaceContainerHigh, dialogMaterial.Color);
            Assert.Equal(MaterialColors.Transparent, dialogMaterial.ShadowColor);
            Assert.Equal(MaterialColors.Transparent, dialogMaterial.SurfaceTintColor);
            Assert.Equal(6.0, dialogMaterial.Elevation);
            Assert.Equal(
                new RoundedRectangleBorder(borderRadius: BorderRadius.Circular(28.0)),
                dialogMaterial.Shape);
            Assert.Equal(Clip.AntiAlias, dialogMaterial.ClipBehavior);

            Dialog dialog = tester.Widget<Dialog>(Find.ByType<Dialog>());
            Assert.Equal(new Thickness(16.0, 24.0), dialog.InsetPadding);
        }, useMaterial3: theme.UseMaterial3);
    }

    // Flutter: 'date_range_picker_test.dart: Input mode Default InputDecoration'
    [Fact]
    public void InputModeDefaultInputDecoration()
    {
        SetUpInputMode();
        using FrameworkDartTester tester = CreateTester();
        PreparePicker(tester, _ =>
        {
            InputDecoration startDateDecoration = tester.Widget<TextField>(Find.ByType<TextField>().First).Decoration!;
            Assert.Equal(new OutlineInputBorder(), startDateDecoration.Border);
            Assert.False(startDateDecoration.Filled);
            Assert.Equal("mm/dd/yyyy", startDateDecoration.HintText);
            Assert.Equal("Start Date", startDateDecoration.LabelText);
            Assert.Null(startDateDecoration.ErrorText);

            InputDecoration endDateDecoration = tester.Widget<TextField>(Find.ByType<TextField>().Last).Decoration!;
            Assert.Equal(new OutlineInputBorder(), endDateDecoration.Border);
            Assert.False(endDateDecoration.Filled);
            Assert.Equal("mm/dd/yyyy", endDateDecoration.HintText);
            Assert.Equal("End Date", endDateDecoration.LabelText);
            Assert.Null(endDateDecoration.ErrorText);
        }, useMaterial3: true);
    }

    // Flutter: 'date_range_picker_test.dart: Input mode Initial entry mode is used'
    [Fact]
    public void InputModeInitialEntryModeIsUsed()
    {
        SetUpInputMode();
        using FrameworkDartTester tester = CreateTester();
        PreparePicker(tester, _ => Finds.NWidgets(Find.ByType<TextField>(), 2));
    }

    // Flutter: 'date_range_picker_test.dart: Input mode All custom strings are used'
    [Fact]
    public void InputModeAllCustomStringsAreUsed()
    {
        SetUpInputMode();
        using FrameworkDartTester tester = CreateTester();
        _initialDateRange = null;
        _cancelText = "nope";
        _confirmText = "yep";
        _fieldStartHintText = "hint1";
        _fieldEndHintText = "hint2";
        _fieldStartLabelText = "label1";
        _fieldEndLabelText = "label2";
        _helpText = "help";
        PreparePicker(tester, _ =>
        {
            Finds.OneWidget(Find.Text(_cancelText!));
            Finds.OneWidget(Find.Text(_confirmText!));
            Finds.OneWidget(Find.Text(_fieldStartHintText!));
            Finds.OneWidget(Find.Text(_fieldEndHintText!));
            Finds.OneWidget(Find.Text(_fieldStartLabelText!));
            Finds.OneWidget(Find.Text(_fieldEndLabelText!));
            Finds.OneWidget(Find.Text(_helpText!));
        });
    }

    // Flutter: 'date_range_picker_test.dart: Input mode Initial date is the default'
    [Fact]
    public void InputModeInitialDateIsTheDefault()
    {
        SetUpInputMode();
        using FrameworkDartTester tester = CreateTester();
        PreparePicker(tester, range =>
        {
            tester.Tap(Find.Text("OK"));
            Assert.Equal(Range(new DateTime(2017, 1, 15), new DateTime(2017, 1, 17)), Await(range));
        });
    }

    // Flutter: 'date_range_picker_test.dart: Input mode Can toggle to calendar entry mode'
    [Fact]
    public void InputModeCanToggleToCalendarEntryMode()
    {
        SetUpInputMode();
        using FrameworkDartTester tester = CreateTester();
        PreparePicker(tester, _ =>
        {
            Finds.NWidgets(Find.ByType<TextField>(), 2);
            tester.Tap(Find.ByIcon(Icons.CalendarToday));
            tester.PumpAndSettle();
            Finds.Nothing(Find.ByType<TextField>());
        });
    }

    // Flutter: 'date_range_picker_test.dart: Input mode Toggle to calendar mode keeps selected date'
    [Fact]
    public void InputModeToggleToCalendarModeKeepsSelectedDate()
    {
        SetUpInputMode();
        using FrameworkDartTester tester = CreateTester();
        _initialDateRange = null;
        PreparePicker(tester, range =>
        {
            tester.EnterText(Find.ByType<TextField>().At(0), "12/25/2016");
            tester.EnterText(Find.ByType<TextField>().At(1), "12/27/2016");
            tester.Tap(Find.ByIcon(Icons.CalendarToday));
            tester.PumpAndSettle();
            tester.Tap(Find.Text("SAVE"));

            Assert.Equal(Range(new DateTime(2016, 12, 25), new DateTime(2016, 12, 27)), Await(range));
        });
    }

    // Flutter: 'date_range_picker_test.dart: Input mode Entered text returns range'
    [Fact]
    public void InputModeEnteredTextReturnsRange()
    {
        SetUpInputMode();
        using FrameworkDartTester tester = CreateTester();
        _initialDateRange = null;
        PreparePicker(tester, range =>
        {
            tester.EnterText(Find.ByType<TextField>().At(0), "12/25/2016");
            tester.EnterText(Find.ByType<TextField>().At(1), "12/27/2016");
            tester.Tap(Find.Text("OK"));

            Assert.Equal(Range(new DateTime(2016, 12, 25), new DateTime(2016, 12, 27)), Await(range));
        });
    }

    // Flutter: 'date_range_picker_test.dart: Input mode Too short entered text shows error'
    [Fact]
    public void InputModeTooShortEnteredTextShowsError()
    {
        SetUpInputMode();
        using FrameworkDartTester tester = CreateTester();
        _initialDateRange = null;
        _errorFormatText = "oops";
        PreparePicker(tester, _ =>
        {
            tester.EnterText(Find.ByType<TextField>().At(0), "12/25");
            tester.EnterText(Find.ByType<TextField>().At(1), "12/25");
            Finds.Nothing(Find.Text(_errorFormatText!));

            tester.Tap(Find.Text("OK"));
            tester.PumpAndSettle();
            Finds.NWidgets(Find.Text(_errorFormatText!), 2);
        });
    }

    // Flutter: 'date_range_picker_test.dart: Input mode Bad format entered text shows error'
    [Fact]
    public void InputModeBadFormatEnteredTextShowsError()
    {
        SetUpInputMode();
        using FrameworkDartTester tester = CreateTester();
        _initialDateRange = null;
        _errorFormatText = "oops";
        PreparePicker(tester, _ =>
        {
            tester.EnterText(Find.ByType<TextField>().At(0), "20202014");
            tester.EnterText(Find.ByType<TextField>().At(1), "20212014");
            Finds.Nothing(Find.Text(_errorFormatText!));

            tester.Tap(Find.Text("OK"));
            tester.PumpAndSettle();
            Finds.NWidgets(Find.Text(_errorFormatText!), 2);
        });
    }

    // Flutter: 'date_range_picker_test.dart: Input mode Invalid entered text shows error'
    [Fact]
    public void InputModeInvalidEnteredTextShowsError()
    {
        SetUpInputMode();
        using FrameworkDartTester tester = CreateTester();
        _initialDateRange = null;
        _errorInvalidText = "oops";
        PreparePicker(tester, _ =>
        {
            tester.EnterText(Find.ByType<TextField>().At(0), "08/08/2014");
            tester.EnterText(Find.ByType<TextField>().At(1), "08/08/2014");
            Finds.Nothing(Find.Text(_errorInvalidText!));

            tester.Tap(Find.Text("OK"));
            tester.PumpAndSettle();
            Finds.NWidgets(Find.Text(_errorInvalidText!), 2);
        });
    }

    // Flutter: 'date_range_picker_test.dart: Input mode End before start date shows error'
    [Fact]
    public void InputModeEndBeforeStartDateShowsError()
    {
        SetUpInputMode();
        using FrameworkDartTester tester = CreateTester();
        _initialDateRange = null;
        _errorInvalidRangeText = "oops";
        PreparePicker(tester, _ =>
        {
            tester.EnterText(Find.ByType<TextField>().At(0), "12/27/2016");
            tester.EnterText(Find.ByType<TextField>().At(1), "12/25/2016");
            Finds.Nothing(Find.Text(_errorInvalidRangeText!));

            tester.Tap(Find.Text("OK"));
            tester.PumpAndSettle();
            Finds.OneWidget(Find.Text(_errorInvalidRangeText!));
        });
    }

    // Flutter: 'date_range_picker_test.dart: Input mode Error text only displayed for invalid date'
    [Fact]
    public void InputModeErrorTextOnlyDisplayedForInvalidDate()
    {
        SetUpInputMode();
        using FrameworkDartTester tester = CreateTester();
        _initialDateRange = null;
        _errorInvalidText = "oops";
        PreparePicker(tester, _ =>
        {
            tester.EnterText(Find.ByType<TextField>().At(0), "12/27/2016");
            tester.EnterText(Find.ByType<TextField>().At(1), "01/01/2018");
            Finds.Nothing(Find.Text(_errorInvalidText!));

            tester.Tap(Find.Text("OK"));
            tester.PumpAndSettle();
            Finds.OneWidget(Find.Text(_errorInvalidText!));
        });
    }

    // Flutter: 'date_range_picker_test.dart: Input mode End before start date does not get passed to calendar mode'
    [Fact]
    public void InputModeEndBeforeStartDateDoesNotGetPassedToCalendarMode()
    {
        SetUpInputMode();
        using FrameworkDartTester tester = CreateTester();
        _initialDateRange = null;
        PreparePicker(tester, _ =>
        {
            tester.EnterText(Find.ByType<TextField>().At(0), "12/27/2016");
            tester.EnterText(Find.ByType<TextField>().At(1), "12/25/2016");

            tester.Tap(Find.ByIcon(Icons.CalendarToday));
            tester.PumpAndSettle();
            tester.Tap(Find.Text("SAVE"));
            tester.PumpAndSettle();

            // Save button should be disabled, so dialog should still be up
            // with the first date selected, but no end date
            Finds.OneWidget(Find.Text("Dec 27"));
            Finds.OneWidget(Find.Text("End Date"));
        });
    }

    // Flutter: 'date_range_picker_test.dart: Input mode Input decoration theme is honored'
    [Fact]
    public void InputModeInputDecorationThemeIsHonored()
    {
        SetUpInputMode();
        using FrameworkDartTester tester = CreateTester();

        // Given a custom paint for an input decoration, extract the border and
        // fill color and test them against the expected values.
        void TestInputDecorator(CustomPaint decoratorPaint, InputBorder expectedBorder, Color expectedContainerColor)
        {
            var inputBorderPainter = (InputBorderPainter)decoratorPaint.ForegroundPainter!;
            // Plumix's painter receives the border the `_InputBorderTween` evaluated to.
            InputBorder actualBorder = inputBorderPainter.Border;
            Color containerColor = inputBorderPainter.BlendedColor;

            Assert.Equal(expectedBorder, actualBorder);
            Assert.Equal(expectedContainerColor, containerColor);
        }

        BuildContext? buttonContext = null;
        InputBorder border = InputBorder.None;
        tester.PumpWidget(new MaterialApp(
            theme: new ThemeData(inputDecorationTheme: new InputDecorationThemeData(border: border)),
            home: new MaterialWidget(
                child: new Builder(context => new ElevatedButton(
                    onPressed: () => buttonContext = context,
                    child: new Text("Go"))))));

        tester.Tap(Find.Text("Go"));
        Assert.NotNull(buttonContext);

        _ = MaterialDatePickers.ShowDateRangePicker(
            context: buttonContext!,
            initialDateRange: _initialDateRange,
            firstDate: _firstDate,
            lastDate: _lastDate,
            initialEntryMode: DatePickerEntryMode.Input);
        tester.PumpAndSettle();

        Finder borderContainers = Find.Descendant(
            of: Find.ByWidgetPredicate(w => w.GetType().Name == "BorderContainer"),
            matching: Find.ByWidgetPredicate(w => w is CustomPaint));

        // Test the start date text field
        TestInputDecorator(tester.Widget<CustomPaint>(borderContainers.First), border, MaterialColors.Transparent);

        // Test the end date text field
        TestInputDecorator(tester.Widget<CustomPaint>(borderContainers.Last), border, MaterialColors.Transparent);
    }

    // This is a regression test for https://github.com/flutter/flutter/issues/131989.
    // Flutter: 'date_range_picker_test.dart: Input mode Dialog contents do not overflow when resized from
    // landscape to portrait'
    [Fact]
    public void InputModeDialogContentsDoNotOverflowWhenResizedFromLandscapeToPortrait()
    {
        SetUpInputMode();
        using FrameworkDartTester tester = CreateTester();
        try
        {
            // Initial window size is wide for landscape mode.
            tester.View.PhysicalSize = WideWindowSize;
            tester.View.DevicePixelRatio = 1.0;

            PreparePicker(tester, _ =>
            {
                // Change window size to narrow for portrait mode.
                tester.View.PhysicalSize = NarrowWindowSize;
                tester.Pump();
                Assert.Null(tester.TakeException());
            });
        }
        finally
        {
            tester.View.Reset();
        }
    }

    // Regression test for https://github.com/flutter/flutter/issues/140311.
    // Flutter: 'date_range_picker_test.dart: Input mode Text field stays visible when orientation is portrait and
    // height is reduced'
    [Fact]
    public void InputModeTextFieldStaysVisibleWhenOrientationIsPortraitAndHeightIsReduced()
    {
        SetUpInputMode();
        using FrameworkDartTester tester = CreateTester();
        try
        {
            tester.View.PhysicalSize = new Size(720, 1280);
            tester.View.DevicePixelRatio = 1.0;
            _initialEntryMode = DatePickerEntryMode.Input;

            // Text fields and header are visible by default.
            PreparePicker(tester, _ =>
            {
                Finds.NWidgets(Find.ByType<TextField>(), 2);
                Finds.One(Find.Text("Select range"));
            }, useMaterial3: true);

            // Simulate the portait mode on a device with a small display when the virtual
            // keyboard is visible.
            tester.View.ViewInsets = new Thickness(0, 0, 0, 1000);
            tester.PumpAndSettle();

            // Text fields are visible and header is hidden
            Finds.NWidgets(Find.ByType<TextField>(), 2);
            Finds.Nothing(Find.Text("Select range"));
        }
        finally
        {
            tester.View.Reset();
        }
    }

    // Flutter: 'date_range_picker_test.dart: DatePickerDialog is state restorable'
    [Fact]
    public void DatePickerDialogIsStateRestorable()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(new MaterialApp(
            theme: new ThemeData(useMaterial3: false),
            restorationScopeId: "app",
            home: new RestorableDateRangePickerDialogTestWidget()));

        // The date range picker should be closed.
        Finds.Nothing(Find.ByType<DateRangePickerDialog>());
        Finds.OneWidget(Find.Text("1/1/2021 to 5/1/2021"));

        // Open the date range picker.
        tester.Tap(Find.Text("X"));
        tester.PumpAndSettle();
        Finds.OneWidget(Find.ByType<DateRangePickerDialog>());

        TestRestorationData restorationData = tester.GetRestorationData();
        tester.RestartAndRestore();

        // The date range picker should be open after restoring.
        Finds.OneWidget(Find.ByType<DateRangePickerDialog>());

        // Close the date range picker.
        tester.Tap(Find.ByIcon(Icons.Close));
        tester.PumpAndSettle();

        // The date range picker should be closed, the text value updated to the
        // newly selected date.
        Finds.Nothing(Find.ByType<DateRangePickerDialog>());
        Finds.OneWidget(Find.Text("1/1/2021 to 5/1/2021"));

        // The date range picker should be open after restoring.
        tester.RestoreFrom(restorationData);
        Finds.OneWidget(Find.ByType<DateRangePickerDialog>());

        // // Select a different date and close the date range picker.
        tester.Tap(Find.Text("12").First);
        tester.PumpAndSettle();
        tester.Tap(Find.Text("14").First);
        tester.PumpAndSettle();

        // Restart after the new selection. It should remain selected.
        tester.RestartAndRestore();

        // Close the date range picker.
        tester.Tap(Find.Text("SAVE"));
        tester.PumpAndSettle();

        // The date range picker should be closed, the text value updated to the
        // newly selected date.
        Finds.Nothing(Find.ByType<DateRangePickerDialog>());
        Finds.OneWidget(Find.Text("12/1/2021 to 14/1/2021"));
    }

    // Flutter: 'date_range_picker_test.dart: DateRangePickerDialog state restoration - DatePickerEntryMode'
    [Fact]
    public void DateRangePickerDialogStateRestorationDatePickerEntryMode()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(new MaterialApp(
            restorationScopeId: "app",
            home: new RestorableDateRangePickerDialogTestWidget(
                datePickerEntryMode: DatePickerEntryMode.CalendarOnly)));

        // The date range picker should be closed.
        Finds.Nothing(Find.ByType<DateRangePickerDialog>());
        Finds.OneWidget(Find.Text("1/1/2021 to 5/1/2021"));

        // Open the date range picker.
        tester.Tap(Find.Text("X"));
        tester.PumpAndSettle();
        Finds.OneWidget(Find.ByType<DateRangePickerDialog>());

        // Only in calendar mode and cannot switch out.
        Finds.Nothing(Find.ByType<TextField>());
        Finds.Nothing(Find.ByIcon(Icons.Edit));

        TestRestorationData restorationData = tester.GetRestorationData();
        tester.RestartAndRestore();

        // The date range picker should be open after restoring.
        Finds.OneWidget(Find.ByType<DateRangePickerDialog>());
        // Only in calendar mode and cannot switch out.
        Finds.Nothing(Find.ByType<TextField>());
        Finds.Nothing(Find.ByIcon(Icons.Edit));

        // Tap on the barrier.
        tester.Tap(Find.ByIcon(Icons.Close));
        tester.PumpAndSettle();

        // The date range picker should be closed, the text value should be the same
        // as before.
        Finds.Nothing(Find.ByType<DateRangePickerDialog>());
        Finds.OneWidget(Find.Text("1/1/2021 to 5/1/2021"));

        // The date range picker should be open after restoring.
        tester.RestoreFrom(restorationData);
        Finds.OneWidget(Find.ByType<DateRangePickerDialog>());
        // Only in calendar mode and cannot switch out.
        Finds.Nothing(Find.ByType<TextField>());
        Finds.Nothing(Find.ByIcon(Icons.Edit));
    }

    // ---- group('showDateRangePicker avoids overlapping display features') ----

    private static Widget HingeApp(bool rtl) => new MaterialApp(
        builder: (_, child) => new MediaQuery(
            // Display has a vertical hinge down the middle
            data: new MediaQueryData(
                Size: new Size(800, 600),
                DisplayFeatures:
                [
                    new DisplayFeature(
                        Bounds: new Rect(new Point(390, 0), new Point(410, 600)),
                        Type: DisplayFeatureType.Hinge,
                        State: DisplayFeatureState.Unknown),
                ]),
            child: rtl ? new Directionality(TextDirection.Rtl, child!) : child!),
        home: new Center(child: new Text("Test")));

    // Flutter: 'date_range_picker_test.dart: showDateRangePicker avoids overlapping display features positioning
    // with anchorPoint'
    [Fact]
    public void ShowDateRangePickerAvoidsOverlappingDisplayFeaturesPositioningWithAnchorPoint()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(HingeApp(rtl: false));

        BuildContext context = tester.Element(Find.Text("Test"));
        _ = MaterialDatePickers.ShowDateRangePicker(
            context: context,
            firstDate: new DateTime(2018, 1, 1),
            lastDate: new DateTime(2030, 1, 1),
            anchorPoint: new Point(1000, 0));
        tester.PumpAndSettle();

        // Should take the right side of the screen
        Assert.Equal(new Point(410.0, 0.0), tester.GetTopLeft(Find.ByType<DateRangePickerDialog>()));
        Assert.Equal(new Point(800.0, 600.0), tester.GetBottomRight(Find.ByType<DateRangePickerDialog>()));
    }

    // Flutter: 'date_range_picker_test.dart: showDateRangePicker avoids overlapping display features positioning
    // with Directionality'
    [Fact]
    public void ShowDateRangePickerAvoidsOverlappingDisplayFeaturesPositioningWithDirectionality()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(HingeApp(rtl: true));

        BuildContext context = tester.Element(Find.Text("Test"));
        _ = MaterialDatePickers.ShowDateRangePicker(
            context: context,
            firstDate: new DateTime(2018, 1, 1),
            lastDate: new DateTime(2030, 1, 1),
            anchorPoint: new Point(1000, 0));
        tester.PumpAndSettle();

        // By default it should place the dialog on the right screen
        Assert.Equal(new Point(410.0, 0.0), tester.GetTopLeft(Find.ByType<DateRangePickerDialog>()));
        Assert.Equal(new Point(800.0, 600.0), tester.GetBottomRight(Find.ByType<DateRangePickerDialog>()));
    }

    // Flutter: 'date_range_picker_test.dart: showDateRangePicker avoids overlapping display features positioning
    // with defaults'
    [Fact]
    public void ShowDateRangePickerAvoidsOverlappingDisplayFeaturesPositioningWithDefaults()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(HingeApp(rtl: false));

        BuildContext context = tester.Element(Find.Text("Test"));
        _ = MaterialDatePickers.ShowDateRangePicker(
            context: context,
            firstDate: new DateTime(2018, 1, 1),
            lastDate: new DateTime(2030, 1, 1));
        tester.PumpAndSettle();

        // By default it should place the dialog on the left screen
        Assert.Equal(new Point(0, 0), tester.GetTopLeft(Find.ByType<DateRangePickerDialog>()));
        Assert.Equal(new Point(390.0, 600.0), tester.GetBottomRight(Find.ByType<DateRangePickerDialog>()));
    }

    // Flutter: 'date_range_picker_test.dart: Semantics calendar mode'
    [Fact]
    public void SemanticsCalendarMode()
    {
        using FrameworkDartTester tester = CreateTester();
        SemanticsHandle semantics = tester.EnsureSemantics();
        _currentDate = new DateTime(2016, 1, 30);
        PreparePicker(tester, _ =>
        {
            ExpectSemantics(
                tester.GetSemantics(Find.Text("30")),
                MatchesSemantics(
                    label: "30, Saturday, January 30, 2016, Today",
                    hasTapAction: true,
                    hasFocusAction: true,
                    hasSelectedState: true,
                    isFocusable: true));
        });
        semantics.Dispose();
    }

    public static TheoryData<string> KeyboardTypes => new() { "null", "emailAddress" };

    // Flutter: 'date_range_picker_test.dart: DateRangePicker takes keyboardType $keyboardType'
    [Theory]
    [MemberData(nameof(KeyboardTypes))]
    public void DateRangePickerTakesKeyboardType(string keyboardTypeName)
    {
        TextInputType? keyboardType = keyboardTypeName == "null" ? null : TextInputType.EmailAddress;
        using FrameworkDartTester tester = CreateTester();
        BuildContext? buttonContext = null;
        InputBorder border = InputBorder.None;
        tester.PumpWidget(new MaterialApp(
            theme: new ThemeData(inputDecorationTheme: new InputDecorationThemeData(border: border)),
            home: new MaterialWidget(
                child: new Builder(context => new ElevatedButton(
                    onPressed: () => buttonContext = context,
                    child: new Text("Go"))))));

        tester.Tap(Find.Text("Go"));
        Assert.NotNull(buttonContext);

        if (keyboardType == null)
        {
            // If no keyboardType, expect the default.
            _ = MaterialDatePickers.ShowDateRangePicker(
                context: buttonContext!,
                initialDateRange: _initialDateRange,
                firstDate: _firstDate,
                lastDate: _lastDate,
                initialEntryMode: DatePickerEntryMode.Input);
        }
        else
        {
            // If there is a keyboardType, expect it to be passed through.
            _ = MaterialDatePickers.ShowDateRangePicker(
                context: buttonContext!,
                initialDateRange: _initialDateRange,
                firstDate: _firstDate,
                lastDate: _lastDate,
                initialEntryMode: DatePickerEntryMode.Input,
                keyboardType: keyboardType);
        }

        tester.PumpAndSettle();

        DateRangePickerDialog picker = tester.Widget<DateRangePickerDialog>(Find.ByType<DateRangePickerDialog>());
        Assert.Equal(keyboardType ?? TextInputType.Datetime, picker.KeyboardType);
    }

    // Flutter: 'date_range_picker_test.dart: honors switchToInputEntryModeIcon'
    [Fact]
    public void HonorsSwitchToInputEntryModeIcon()
    {
        using FrameworkDartTester tester = CreateTester();

        Widget BuildApp(bool? useMaterial3 = null, Icon? switchToInputEntryModeIcon = null)
        {
            return new MaterialApp(
                theme: new ThemeData(useMaterial3: useMaterial3 ?? false),
                home: new MaterialWidget(
                    child: new Builder(context => new ElevatedButton(
                        child: new Text("Click X"),
                        onPressed: () =>
                        {
                            _ = MaterialDatePickers.ShowDateRangePicker(
                                context: context,
                                firstDate: new DateTime(2020, 1, 1),
                                lastDate: new DateTime(2030, 1, 1),
                                switchToInputEntryModeIcon: switchToInputEntryModeIcon);
                        }))));
        }

        tester.PumpWidget(BuildApp());
        tester.PumpAndSettle();
        tester.Tap(Find.ByType<ElevatedButton>());
        tester.PumpAndSettle();
        Finds.OneWidget(Find.ByIcon(Icons.Edit));
        tester.Tap(Find.ByIcon(Icons.Close));
        tester.PumpAndSettle();

        tester.PumpWidget(BuildApp(useMaterial3: true));
        tester.PumpAndSettle();
        tester.Tap(Find.ByType<ElevatedButton>());
        tester.PumpAndSettle();
        Finds.OneWidget(Find.ByIcon(Icons.EditOutlined));
        tester.Tap(Find.ByIcon(Icons.Close));
        tester.PumpAndSettle();

        tester.PumpWidget(BuildApp(switchToInputEntryModeIcon: new Icon(Icons.Keyboard)));
        tester.PumpAndSettle();
        tester.Tap(Find.ByType<ElevatedButton>());
        tester.PumpAndSettle();
        Finds.OneWidget(Find.ByIcon(Icons.Keyboard));
        tester.Tap(Find.ByIcon(Icons.Close));
        tester.PumpAndSettle();
    }

    // Flutter: 'date_range_picker_test.dart: honors switchToCalendarEntryModeIcon'
    [Fact]
    public void HonorsSwitchToCalendarEntryModeIcon()
    {
        using FrameworkDartTester tester = CreateTester();

        Widget BuildApp(bool? useMaterial3 = null, Icon? switchToCalendarEntryModeIcon = null)
        {
            return new MaterialApp(
                theme: new ThemeData(useMaterial3: useMaterial3 ?? false),
                home: new MaterialWidget(
                    child: new Builder(context => new ElevatedButton(
                        child: new Text("Click X"),
                        onPressed: () =>
                        {
                            _ = MaterialDatePickers.ShowDateRangePicker(
                                context: context,
                                firstDate: new DateTime(2020, 1, 1),
                                lastDate: new DateTime(2030, 1, 1),
                                switchToCalendarEntryModeIcon: switchToCalendarEntryModeIcon,
                                initialEntryMode: DatePickerEntryMode.Input,
                                cancelText: "CANCEL");
                        }))));
        }

        tester.PumpWidget(BuildApp());
        tester.PumpAndSettle();
        tester.Tap(Find.ByType<ElevatedButton>());
        tester.PumpAndSettle();
        Finds.OneWidget(Find.ByIcon(Icons.CalendarToday));
        tester.Tap(Find.Text("CANCEL"));
        tester.PumpAndSettle();

        tester.PumpWidget(BuildApp(useMaterial3: true));
        tester.PumpAndSettle();
        tester.Tap(Find.ByType<ElevatedButton>());
        tester.PumpAndSettle();
        Finds.OneWidget(Find.ByIcon(Icons.CalendarToday));
        tester.Tap(Find.Text("CANCEL"));
        tester.PumpAndSettle();

        tester.PumpWidget(BuildApp(switchToCalendarEntryModeIcon: new Icon(Icons.Favorite)));
        tester.PumpAndSettle();
        tester.Tap(Find.ByType<ElevatedButton>());
        tester.PumpAndSettle();
        Finds.OneWidget(Find.ByIcon(Icons.Favorite));
        tester.Tap(Find.Text("CANCEL"));
        tester.PumpAndSettle();
    }

    // This is a regression test for https://github.com/flutter/flutter/issues/154393.
    // Flutter: 'date_range_picker_test.dart: DateRangePicker close button shape should be square'
    [Fact]
    public void DateRangePickerCloseButtonShapeShouldBeSquare()
    {
        using FrameworkDartTester tester = CreateTester();
        PreparePicker(tester, _ =>
        {
            var theme = new ThemeData();
            Finder buttonFinder = Find.WidgetWithIcon<IconButton>(Icons.Close);
            Assert.Equal(new Size(48.0, 48.0), tester.GetSize(buttonFinder));

            // Test the close button overlay size is square.
            TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
            gesture.AddPointer();
            gesture.MoveTo(tester.GetCenter(buttonFinder));
            tester.PumpAndSettle();
            PaintAssert.Paints(
                buttonFinder,
                PaintPattern.Paints.Rect(
                    rect: new Rect(0.0, 0.0, 40.0, 40.0),
                    color: theme.ColorScheme.OnSurfaceVariant.WithOpacity(0.08)));
        }, useMaterial3: true);
    }

    // ---- group('Material 2') ----
    // These tests are only relevant for Material 2. Once Material 2
    // support is deprecated and the APIs are removed, these tests
    // can be deleted.

    // Flutter: 'date_range_picker_test.dart: Material 2 Default layout (calendar mode)'
    [Fact]
    public void Material2DefaultLayoutCalendarMode()
    {
        using FrameworkDartTester tester = CreateTester();
        PreparePicker(tester, _ =>
        {
            Finder helpText = Find.Text("SELECT RANGE");
            Finder firstDateHeaderText = Find.Text("Jan 15");
            Finder lastDateHeaderText = Find.Text("Jan 25, 2016");
            Finder saveText = Find.Text("SAVE");

            Finds.OneWidget(helpText);
            Finds.OneWidget(firstDateHeaderText);
            Finds.OneWidget(lastDateHeaderText);
            Finds.OneWidget(saveText);

            // Test the close button position.
            Point closeButtonBottomRight = tester.GetBottomRight(Find.ByType<CloseButton>());
            Point helpTextTopLeft = tester.GetTopLeft(helpText);
            Assert.Equal(56.0, closeButtonBottomRight.X);
            Assert.Equal(helpTextTopLeft.Y - 6.0, closeButtonBottomRight.Y);

            // Test the save and entry buttons position.
            Point saveButtonBottomLeft = tester.GetBottomLeft(Find.ByType<TextButton>());
            Point entryButtonBottomLeft = tester.GetBottomLeft(Find.WidgetWithIcon<IconButton>(Icons.Edit));
            Assert.Equal(800 - 80.0, saveButtonBottomLeft.X);
            Assert.Equal(helpTextTopLeft.Y - 6.0, saveButtonBottomLeft.Y);
            Assert.Equal(saveButtonBottomLeft.X - 48.0, entryButtonBottomLeft.X);
            Assert.Equal(helpTextTopLeft.Y - 6.0, entryButtonBottomLeft.Y);

            // Test help text position.
            Point helpTextBottomLeft = tester.GetBottomLeft(helpText);
            Assert.Equal(72.0, helpTextBottomLeft.X);
            Assert.Equal(closeButtonBottomRight.Y + 16.0, helpTextBottomLeft.Y);

            // Test the header position.
            Point firstDateHeaderTopLeft = tester.GetTopLeft(firstDateHeaderText);
            Point lastDateHeaderTopLeft = tester.GetTopLeft(lastDateHeaderText);
            Assert.Equal(72.0, firstDateHeaderTopLeft.X);
            Assert.Equal(helpTextBottomLeft.Y + 8.0, firstDateHeaderTopLeft.Y);
            Point firstDateHeaderTopRight = tester.GetTopRight(firstDateHeaderText);
            Assert.Equal(firstDateHeaderTopRight.X + 72.0, lastDateHeaderTopLeft.X);
            Assert.Equal(helpTextBottomLeft.Y + 8.0, lastDateHeaderTopLeft.Y);

            // Test the day headers position.
            Point dayHeadersGridTopLeft = tester.GetTopLeft(Find.ByType<GridView>().First);
            Point firstDateHeaderBottomLeft = tester.GetBottomLeft(firstDateHeaderText);
            Assert.Equal((800 - 384) / 2.0, dayHeadersGridTopLeft.X);
            Assert.Equal(firstDateHeaderBottomLeft.Y + 16.0, dayHeadersGridTopLeft.Y);

            // Test the calendar custom scroll view position.
            Point calendarScrollViewTopLeft = tester.GetTopLeft(Find.ByType<CustomScrollView>());
            Point dayHeadersGridBottomLeft = tester.GetBottomLeft(Find.ByType<GridView>().First);
            Assert.Equal(0.0, calendarScrollViewTopLeft.X);
            Assert.Equal(dayHeadersGridBottomLeft.Y, calendarScrollViewTopLeft.Y);
        });
    }

    // Flutter: 'date_range_picker_test.dart: Material 2 Default Dialog properties (calendar mode)'
    [Fact]
    public void Material2DefaultDialogPropertiesCalendarMode()
    {
        using FrameworkDartTester tester = CreateTester();
        var theme = new ThemeData(useMaterial3: false);
        PreparePicker(tester, _ =>
        {
            MaterialWidget dialogMaterial = DialogMaterial(tester);

            Assert.Equal(theme.ColorScheme.Surface, dialogMaterial.Color);
            Assert.Equal(MaterialColors.Transparent, dialogMaterial.ShadowColor);
            Assert.Equal(MaterialColors.Transparent, dialogMaterial.SurfaceTintColor);
            Assert.Equal(0.0, dialogMaterial.Elevation);
            Assert.Equal(new RoundedRectangleBorder(), dialogMaterial.Shape);
            Assert.Equal(Clip.AntiAlias, dialogMaterial.ClipBehavior);

            Dialog dialog = tester.Widget<Dialog>(Find.ByType<Dialog>());
            Assert.Equal(new Thickness(0), dialog.InsetPadding);
        });
    }

    // Flutter: 'date_range_picker_test.dart: Material 2 Scaffold and AppBar defaults'
    [Fact]
    public void Material2ScaffoldAndAppBarDefaults()
    {
        using FrameworkDartTester tester = CreateTester();
        var theme = new ThemeData(useMaterial3: false);
        PreparePicker(tester, _ =>
        {
            Scaffold scaffold = tester.Widget<Scaffold>(Find.ByType<Scaffold>());
            Assert.Equal(theme.ColorScheme.Surface, scaffold.BackgroundColor);

            AppBar appBar = tester.Widget<AppBar>(Find.ByType<AppBar>());
            var iconTheme = new IconThemeData(Color: theme.ColorScheme.OnPrimary);
            Assert.Equal(iconTheme, appBar.IconTheme);
            Assert.Equal(iconTheme, appBar.ActionsIconTheme);
            Assert.Null(appBar.Elevation);
            Assert.Null(appBar.ScrolledUnderElevation);
            Assert.Equal(theme.ColorScheme.Primary, appBar.BackgroundColor);
        });
    }

    // Flutter: 'date_range_picker_test.dart: Material 2 Input mode Default Dialog properties (input mode)'
    [Fact]
    public void Material2InputModeDefaultDialogPropertiesInputMode()
    {
        SetUpInputMode();
        using FrameworkDartTester tester = CreateTester();
        var theme = new ThemeData(useMaterial3: false);
        PreparePicker(tester, _ =>
        {
            MaterialWidget dialogMaterial = DialogMaterial(tester);

            Assert.Equal(theme.ColorScheme.Surface, dialogMaterial.Color);
            Assert.Equal(theme.ShadowColor, dialogMaterial.ShadowColor);
            Assert.Null(dialogMaterial.SurfaceTintColor);
            Assert.Equal(24.0, dialogMaterial.Elevation);
            Assert.Equal(
                new RoundedRectangleBorder(borderRadius: BorderRadius.Circular(4.0)),
                dialogMaterial.Shape);
            Assert.Equal(Clip.AntiAlias, dialogMaterial.ClipBehavior);

            Dialog dialog = tester.Widget<Dialog>(Find.ByType<Dialog>());
            Assert.Equal(new Thickness(16.0, 24.0), dialog.InsetPadding);
        });
    }

    // Flutter: 'date_range_picker_test.dart: Material 2 Input mode Default InputDecoration'
    [Fact]
    public void Material2InputModeDefaultInputDecoration()
    {
        SetUpInputMode();
        using FrameworkDartTester tester = CreateTester();
        PreparePicker(tester, _ =>
        {
            InputDecoration startDateDecoration = tester.Widget<TextField>(Find.ByType<TextField>().First).Decoration!;
            Assert.Equal(new UnderlineInputBorder(), startDateDecoration.Border);
            Assert.False(startDateDecoration.Filled);
            Assert.Equal("mm/dd/yyyy", startDateDecoration.HintText);
            Assert.Equal("Start Date", startDateDecoration.LabelText);
            Assert.Null(startDateDecoration.ErrorText);

            InputDecoration endDateDecoration = tester.Widget<TextField>(Find.ByType<TextField>().Last).Decoration!;
            Assert.Equal(new UnderlineInputBorder(), endDateDecoration.Border);
            Assert.False(endDateDecoration.Filled);
            Assert.Equal("mm/dd/yyyy", endDateDecoration.HintText);
            Assert.Equal("End Date", endDateDecoration.LabelText);
            Assert.Null(endDateDecoration.ErrorText);
        });
    }

    // ---- group('Calendar Delegate') ----

    // Flutter: 'date_range_picker_test.dart: Calendar Delegate Defaults to Gregorian calendar system'
    [Fact]
    public void CalendarDelegateDefaultsToGregorianCalendarSystem()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(
                child: new DateRangePickerDialog(
                    initialDateRange: _initialDateRange,
                    firstDate: _firstDate,
                    lastDate: _lastDate))));

        DateRangePickerDialog dialog = tester.Widget<DateRangePickerDialog>(Find.ByType<DateRangePickerDialog>());
        Assert.IsAssignableFrom<GregorianCalendarDelegate>(dialog.CalendarDelegate);
    }

    // Flutter: 'date_range_picker_test.dart: Calendar Delegate Using custom calendar delegate implementation'
    [Fact]
    public void CalendarDelegateUsingCustomCalendarDelegateImplementation()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(
                child: new DateRangePickerDialog(
                    initialDateRange: _initialDateRange,
                    firstDate: _firstDate,
                    lastDate: _lastDate,
                    calendarDelegate: new TestCalendarDelegate()))));

        DateRangePickerDialog dialog = tester.Widget<DateRangePickerDialog>(Find.ByType<DateRangePickerDialog>());
        Assert.IsType<TestCalendarDelegate>(dialog.CalendarDelegate);
    }

    // Flutter: 'date_range_picker_test.dart: Calendar Delegate showDateRangePicker uses gregorian calendar
    // delegate by default'
    [Fact]
    public void CalendarDelegateShowDateRangePickerUsesGregorianCalendarDelegateByDefault()
    {
        using FrameworkDartTester tester = CreateTester();
        PreparePicker(tester, _ =>
        {
            Finds.OneWidget(Find.Text("Select range"));
            Finds.OneWidget(Find.Text("Jan 15"));
            Finds.OneWidget(Find.Text("Jan 25, 2016"));
            Finds.OneWidget(Find.Text("Save"));

            DateRangePickerDialog dialog = tester.Widget<DateRangePickerDialog>(
                Find.ByType<DateRangePickerDialog>());
            Assert.IsAssignableFrom<GregorianCalendarDelegate>(dialog.CalendarDelegate);
        }, useMaterial3: true);
    }

    // Flutter: 'date_range_picker_test.dart: Calendar Delegate showDateRangePicker using custom calendar delegate
    // implementation'
    [Fact]
    public void CalendarDelegateShowDateRangePickerUsingCustomCalendarDelegateImplementation()
    {
        using FrameworkDartTester tester = CreateTester();
        PreparePicker(
            tester,
            _ =>
            {
                Finds.OneWidget(Find.Text("Select range"));
                Finds.OneWidget(Find.Text("Jan 15"));
                Finds.OneWidget(Find.Text("Jan 25, 2016"));
                Finds.OneWidget(Find.Text("Save"));

                DateRangePickerDialog dialog = tester.Widget<DateRangePickerDialog>(
                    Find.ByType<DateRangePickerDialog>());
                Assert.IsType<TestCalendarDelegate>(dialog.CalendarDelegate);
            },
            useMaterial3: true,
            calendarDelegate: new TestCalendarDelegate());
    }

    // Flutter: 'date_range_picker_test.dart: Calendar Delegate Displays calendar based on the calendar delegate'
    [Fact]
    public void CalendarDelegateDisplaysCalendarBasedOnTheCalendarDelegate()
    {
        using FrameworkDartTester tester = CreateTester();

        Finder GetMonthItem()
        {
            Finder dayItem = Find.Descendant(of: Find.ByType<ConstrainedBox>(), matching: Find.Text("1"));
            return Find.Ancestor(of: dayItem, matching: Find.ByType<Column>());
        }

        int GetDayCount(Finder parent)
        {
            Finder dayItem = Find.Descendant(
                of: parent,
                matching: Find.Descendant(of: Find.ByType<InkResponse>(), matching: Find.ByType<Text>()));
            return tester.WidgetList<Widget>(dayItem).Count;
        }

        Text GetMonthYear(Finder parent)
        {
            return tester.Widget<Text>(
                Find.Descendant(
                        of: parent,
                        matching: Find.Descendant(of: Find.ByType<ConstrainedBox>(), matching: Find.ByType<Text>()))
                    .First);
        }

        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(
                child: new DateRangePickerDialog(
                    initialDateRange: _initialDateRange,
                    firstDate: _firstDate,
                    lastDate: _lastDate,
                    calendarDelegate: new TestCalendarDelegate()))));

        Finder monthItem = GetMonthItem();

        Finder firstMonthItem = monthItem.At(0);
        Assert.Equal("January 2016", GetMonthYear(firstMonthItem).Data);
        Assert.Equal(28, GetDayCount(firstMonthItem));

        Finder secondMonthItem = monthItem.At(2);
        Assert.Equal("February 2016", GetMonthYear(secondMonthItem).Data);
        Assert.Equal(21, GetDayCount(secondMonthItem));
    }

    // Flutter: 'date_range_picker_test.dart: DateRangePickerDialog does not crash at zero area'
    [Fact]
    public void DateRangePickerDialogDoesNotCrashAtZeroArea()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(new MaterialApp(
            home: new Center(
                child: SizedBox.Shrink(
                    child: new DateRangePickerDialog(firstDate: _firstDate, lastDate: _lastDate)))));
        Assert.Equal(new Size(0, 0), tester.GetSize(Find.ByType<DateRangePickerDialog>()));
    }

    // Regression test for https://github.com/flutter/flutter/issues/177083.
    // Flutter: 'date_range_picker_test.dart: Local InputDecorationTheme is honored'
    [Fact]
    public void LocalInputDecorationThemeIsHonored()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(new MaterialApp(
            home: new Center(
                child: new InputDecorationTheme(
                    data: new InputDecorationThemeData(filled: true),
                    child: new DateRangePickerDialog(
                        firstDate: _firstDate,
                        lastDate: _lastDate,
                        currentDate: new DateTime(2016, 1, 30),
                        initialEntryMode: DatePickerEntryMode.InputOnly)))));

        InputDecoration startDateDecoration = tester.Widget<TextField>(Find.ByType<TextField>().First).Decoration!;

        Assert.True(startDateDecoration.Filled);
    }

    // Regression test for https://github.com/flutter/flutter/issues/177441.
    // Flutter: 'date_range_picker_test.dart: DateRangePickerDialog.currentDate is optional'
    [Fact]
    public void DateRangePickerDialogCurrentDateIsOptional()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(new MaterialApp(
            home: new Center(
                child: new InputDecorationTheme(
                    data: new InputDecorationThemeData(filled: true),
                    child: new DateRangePickerDialog(
                        firstDate: _firstDate,
                        lastDate: _lastDate,
                        initialEntryMode: DatePickerEntryMode.InputOnly)))));

        Assert.Null(tester.TakeException());
    }

    // Flutter: 'date_range_picker_test.dart: DateRangePicker respects DatePickerTheme.dayShape'
    [Fact]
    public void DateRangePickerRespectsDatePickerThemeDayShape()
    {
        using FrameworkDartTester tester = CreateTester();
        OutlinedBorder customShape = new BeveledRectangleBorder(borderRadius: BorderRadius.Circular(10));

        tester.PumpWidget(new MaterialApp(
            theme: new ThemeData(
                datePickerTheme: new DatePickerThemeData(
                    dayShape: new WidgetStatePropertyAll<OutlinedBorder?>(customShape))),
            home: new MaterialWidget(
                child: new Builder(context => new ElevatedButton(
                    onPressed: () => _ = MaterialDatePickers.ShowDateRangePicker(
                        context: context,
                        firstDate: new DateTime(2023, 1, 1),
                        lastDate: new DateTime(2024, 1, 1),
                        initialDateRange: Range(new DateTime(2023, 1, 15), new DateTime(2023, 1, 20))),
                    child: new Text("Open Picker"))))));

        tester.Tap(Find.Text("Open Picker"));
        tester.PumpAndSettle();

        Finder selectedDayText = Find.Text("15");

        Finder dayContainerFinder = Find.Ancestor(
                of: selectedDayText,
                matching: Find.ByWidgetPredicate(widget =>
                    widget is Container container && container.Decoration is ShapeDecoration))
            .First;

        Container dayContainer = tester.Widget<Container>(dayContainerFinder);
        var decoration = (ShapeDecoration)dayContainer.Decoration!;

        Assert.Equal(customShape, decoration.Shape);
    }

    // date_range_picker_test.dart: _RestorableDateRangePickerDialogTestWidget.
    private sealed class RestorableDateRangePickerDialogTestWidget(
        DatePickerEntryMode datePickerEntryMode = DatePickerEntryMode.Calendar) : StatefulWidget
    {
        public DatePickerEntryMode DatePickerEntryMode { get; } = datePickerEntryMode;

        public override State CreateState() => new RestorableDateRangePickerDialogTestWidgetState();
    }

    private sealed class RestorableDateRangePickerDialogTestWidgetState
        : RestorationState<RestorableDateRangePickerDialogTestWidget>
    {
        private readonly RestorableDateTimeN _startDate = new(new DateTime(2021, 1, 1));
        private readonly RestorableDateTimeN _endDate = new(new DateTime(2021, 1, 5));
        private RestorableRouteFuture<DateTimeRange<DateTime>?>? _routeFuture;

        protected override string? RestorationId => "scaffold_state";

        private RestorableRouteFuture<DateTimeRange<DateTime>?> RestorableDateRangePickerRouteFuture =>
            _routeFuture ??= new RestorableRouteFuture<DateTimeRange<DateTime>?>(
                onComplete: SelectDateRange,
                onPresent: (navigator, _) => navigator.RestorablePush(
                    DateRangePickerRoute,
                    arguments: new Dictionary<string, object?>
                    {
                        ["datePickerEntryMode"] = (int)Widget.DatePickerEntryMode,
                    }));

        public override void Dispose()
        {
            _startDate.Dispose();
            _endDate.Dispose();
            RestorableDateRangePickerRouteFuture.Dispose();
            base.Dispose();
        }

        protected override void RestoreState(RestorationBucket? oldBucket, bool initialRestore)
        {
            RegisterForRestoration(_startDate, "start_date");
            RegisterForRestoration(_endDate, "end_date");
            RegisterForRestoration(RestorableDateRangePickerRouteFuture, "date_picker_route_future");
        }

        private void SelectDateRange(DateTimeRange<DateTime>? newSelectedDate)
        {
            if (newSelectedDate != null)
            {
                SetState(() =>
                {
                    _startDate.Value = newSelectedDate.Start;
                    _endDate.Value = newSelectedDate.End;
                });
            }
        }

        // Anonymous restorable routes must be built by a static method, Plumix's stand-in for Dart's
        // `@pragma('vm:entry-point')` static function.
        private static Route DateRangePickerRoute(BuildContext context, object? arguments)
        {
            return new DialogRoute<DateTimeRange<DateTime>?>(
                context,
                _ =>
                {
                    var args = (IDictionary)arguments!;
                    return new DateRangePickerDialog(
                        restorationId: "date_picker_dialog",
                        initialEntryMode: Enum.GetValues<DatePickerEntryMode>()[
                            Convert.ToInt32(args["datePickerEntryMode"])],
                        firstDate: new DateTime(2021, 1, 1),
                        currentDate: new DateTime(2021, 1, 25),
                        lastDate: new DateTime(2022, 1, 1));
                });
        }

        public override Widget Build(BuildContext context)
        {
            DateTime? startDateTime = _startDate.Value;
            DateTime? endDateTime = _endDate.Value;
            // Example: "25/7/1994"
            string startDateTimeString = $"{startDateTime?.Day}/{startDateTime?.Month}/{startDateTime?.Year}";
            string endDateTimeString = $"{endDateTime?.Day}/{endDateTime?.Month}/{endDateTime?.Year}";
            return new Scaffold(
                body: new Center(
                    child: new Column(
                        children:
                        [
                            new OutlinedButton(
                                onPressed: () => RestorableDateRangePickerRouteFuture.Present(),
                                child: new Text("X")),
                            new Text($"{startDateTimeString} to {endDateTimeString}"),
                        ])));
        }
    }

    // date_range_picker_test.dart: TestCalendarDelegate.
    private sealed class TestCalendarDelegate : GregorianCalendarDelegate
    {
        public override int GetDaysInMonth(int year, int month) => month % 2 == 0 ? 21 : 28;

        public override int FirstDayOffset(int year, int month, MaterialLocalizations localizations) => 1;
    }
}
