// Dart parity source: material_ui/lib/src/date_picker.dart
// Mirrors material-ui-src/test/date_picker_test.dart

using System.Collections;
using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using static Plumix.Tests.SemanticsMatchers;
using MaterialWidget = Plumix.Material.Material;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class DatePickerDartParityTests : IDisposable
{
    private const string GoldenSkip =
        "Golden-file test (matchesGoldenFile); Plumix has no golden comparison infrastructure.";

    private static readonly Size WideWindowSize = new(1920.0, 1080.0);
    private static readonly Size NarrowWindowSize = new(1070.0, 1770.0);

    // Dart's `late` test-scope variables, reset by `setUp` (xUnit builds a fresh instance per test).
    private DateTime _firstDate = new(2001, 1, 1);
    private DateTime _lastDate = new(2031, 12, 31);
    private DateTime? _initialDate = new(2016, 1, 15);
    private DateTime _today = new(2016, 1, 3);
    private SelectableDayPredicate? _selectableDayPredicate;
    private DatePickerEntryMode _initialEntryMode = DatePickerEntryMode.Calendar;
    private DatePickerMode _initialCalendarMode = DatePickerMode.Day;
    private DatePickerEntryMode _currentMode = DatePickerEntryMode.Calendar;

    private string? _cancelText;
    private string? _confirmText;
    private string? _errorFormatText;
    private string? _errorInvalidText;
    private string? _fieldHintText;
    private string? _fieldLabelText;
    private string? _helpText;
    private TextInputType? _keyboardType;

    private static readonly Finder NextMonthIcon = Find.ByWidgetPredicate(
        w => w is IconButton button && (button.Tooltip?.StartsWith("Next month", StringComparison.Ordinal) ?? false));

    private static readonly Finder PreviousMonthIcon = Find.ByWidgetPredicate(
        w => w is IconButton button
             && (button.Tooltip?.StartsWith("Previous month", StringComparison.Ordinal) ?? false));

    private static Finder SwitchToInputIcon => Find.ByIcon(Icons.Edit);

    private static Finder SwitchToCalendarIcon => Find.ByIcon(Icons.CalendarToday);

    public DatePickerDartParityTests()
    {
        FocusManager.Instance.ResetForTests();
        // flutter_test: defaultTargetPlatform == android, debugDisableShadows == true.
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
    }

    public void Dispose()
    {
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.Automatic;
        PlatformDefaults.DebugTargetPlatformOverride = null;
        FocusManager.Instance.ResetForTests();
    }

    // flutter_test's own view runs at a device pixel ratio of 3.0 (2400x1800 physical, 800x600 logical);
    // the Dart tests that only set `tester.view.physicalSize` rely on it.
    private static FrameworkDartTester CreateTester() =>
        new(fakeGestureTimers: true, devicePixelRatio: 3.0, semanticsEnabled: true, registerTestTextInput: true);

    private static TextField TextField(FrameworkDartTester tester) =>
        tester.Widget<TextField>(Find.ByType<TextField>());

    private void PrepareDatePicker(
        FrameworkDartTester tester,
        Action<Task<DateTime?>> callback,
        TextDirection textDirection = TextDirection.Ltr,
        bool useMaterial3 = false,
        ThemeData? theme = null,
        TextScaler? textScaler = null)
    {
        BuildContext? buttonContext = null;
        tester.PumpWidget(new MaterialApp(
            theme: theme ?? new ThemeData(useMaterial3: useMaterial3),
            home: new MediaQuery(
                data: new MediaQueryData(TextScaler: textScaler ?? TextScaler.NoScaling),
                child: new MaterialWidget(
                    child: new Builder(context => new ElevatedButton(
                        onPressed: () => buttonContext = context,
                        child: new Text("Go")))))));

        tester.Tap(Find.Text("Go"));
        Assert.NotNull(buttonContext);

        Task<DateTime?> date = MaterialDatePickers.ShowDatePicker(
            context: buttonContext!,
            initialDate: _initialDate,
            firstDate: _firstDate,
            lastDate: _lastDate,
            currentDate: _today,
            selectableDayPredicate: _selectableDayPredicate,
            initialDatePickerMode: _initialCalendarMode,
            initialEntryMode: _initialEntryMode,
            cancelText: _cancelText,
            confirmText: _confirmText,
            errorFormatText: _errorFormatText,
            errorInvalidText: _errorInvalidText,
            fieldHintText: _fieldHintText,
            fieldLabelText: _fieldLabelText,
            helpText: _helpText,
            keyboardType: _keyboardType,
            onDatePickerModeChange: value => _currentMode = value,
            builder: (_, child) => new Directionality(textDirection, child ?? new SizedBox()));

        tester.PumpAndSettle(TimeSpan.FromSeconds(1));
        callback(date);
    }

    // Dart's `await date`: the dialog future completes when the route pops.
    private static DateTime? Await(Task<DateTime?> task)
    {
        if (!task.IsCompleted)
        {
            Scheduler.FlushMicrotasks();
        }

        Assert.True(task.IsCompleted, "The date picker future has not completed.");
        return task.Result;
    }

    private static ShapeDecoration? FindDayDecoration(FrameworkDartTester tester, string day) =>
        tester.Widget<Ink>(Find.Ancestor(of: Find.Text(day), matching: Find.ByType<Ink>())).Decoration
            as ShapeDecoration;

    // All days are painted on the same Material widget.
    // Use an arbitrary day to find this Material.
    private static RenderObject FindDayGridMaterial(FrameworkDartTester tester) =>
        (RenderObject)MaterialWidget.Of(tester.Element(Find.Text("17")));

    private static MaterialWidget DialogMaterial(FrameworkDartTester tester) =>
        tester.Widget<MaterialWidget>(
            Find.Descendant(of: Find.ByType<Dialog>(), matching: Find.ByType<MaterialWidget>()).First);

    // A MaterialApp whose home is an ElevatedButton labelled 'X' that runs `onPressed` with its context.
    private static Widget ButtonApp(
        Action<BuildContext> onPressed,
        ThemeData? theme = null,
        IReadOnlyList<NavigatorObserver>? navigatorObservers = null,
        bool wrapInMaterial = true,
        string label = "X")
    {
        Widget button = new Center(
            child: new Builder(context => new ElevatedButton(
                child: new Text(label),
                onPressed: () => onPressed(context))));
        return new MaterialApp(
            theme: theme,
            navigatorObservers: navigatorObservers,
            home: wrapInMaterial ? new MaterialWidget(child: button) : button);
    }

    // ---- group('showDatePicker Dialog') ----

    // Flutter: 'date_picker_test.dart: showDatePicker Dialog Default dialog size'
    [Fact]
    public void ShowDatePickerDialogDefaultDialogSize()
    {
        using FrameworkDartTester tester = CreateTester();

        void ShowPicker(Size size)
        {
            tester.View.PhysicalSize = size;
            tester.View.DevicePixelRatio = 1.0;
            PrepareDatePicker(tester, _ => { }, useMaterial3: true);
        }

        var calendarLandscapeDialogSize = new Size(496.0, 346.0);
        var calendarPortraitDialogSizeM3 = new Size(360.0, 568.0);

        try
        {
            // Test landscape layout.
            ShowPicker(WideWindowSize);

            Size dialogContainerSize = tester.GetSize(Find.ByType<AnimatedContainer>());
            Assert.Equal(calendarLandscapeDialogSize, dialogContainerSize);

            // Close the dialog.
            tester.Tap(Find.Text("OK"));
            tester.PumpAndSettle();

            // Test portrait layout.
            ShowPicker(NarrowWindowSize);

            dialogContainerSize = tester.GetSize(Find.ByType<AnimatedContainer>());
            Assert.Equal(calendarPortraitDialogSizeM3, dialogContainerSize);
        }
        finally
        {
            tester.View.Reset();
        }
    }

    // Flutter: 'date_picker_test.dart: showDatePicker Dialog Default dialog properties'
    [Fact]
    public void ShowDatePickerDialogDefaultDialogProperties()
    {
        using FrameworkDartTester tester = CreateTester();
        var theme = new ThemeData();
        PrepareDatePicker(tester, _ =>
        {
            MaterialWidget dialogMaterial = DialogMaterial(tester);

            Assert.Equal(theme.ColorScheme.SurfaceContainerHigh, dialogMaterial.Color);
            Assert.Equal(MaterialColors.Transparent, dialogMaterial.ShadowColor);
            Assert.Equal(MaterialColors.Transparent, dialogMaterial.SurfaceTintColor);
            Assert.Equal(6.0, dialogMaterial.Elevation);
            Assert.Equal(
                new RoundedRectangleBorder(borderRadius: BorderRadius.All(Radius.Circular(28.0))),
                dialogMaterial.Shape);
            Assert.Equal(Clip.AntiAlias, dialogMaterial.ClipBehavior);

            Dialog dialog = tester.Widget<Dialog>(Find.ByType<Dialog>());
            Assert.Equal(new Thickness(16.0, 24.0), dialog.InsetPadding);
        }, useMaterial3: theme.UseMaterial3);
    }

    // Flutter: 'date_picker_test.dart: showDatePicker Dialog Material3 uses sentence case labels'
    [Fact]
    public void ShowDatePickerDialogMaterial3UsesSentenceCaseLabels()
    {
        using FrameworkDartTester tester = CreateTester();
        PrepareDatePicker(tester, _ => Finds.OneWidget(Find.Text("Select date")), useMaterial3: true);
    }

    // Flutter: 'date_picker_test.dart: showDatePicker Dialog Cancel, confirm, and help text is used'
    [Fact]
    public void ShowDatePickerDialogCancelConfirmAndHelpTextIsUsed()
    {
        using FrameworkDartTester tester = CreateTester();
        _cancelText = "nope";
        _confirmText = "yep";
        _helpText = "help";
        PrepareDatePicker(tester, _ =>
        {
            Finds.OneWidget(Find.Text(_cancelText!));
            Finds.OneWidget(Find.Text(_confirmText!));
            Finds.OneWidget(Find.Text(_helpText!));
        });
    }

    // Flutter: 'date_picker_test.dart: showDatePicker Dialog Initial date is the default'
    [Fact]
    public void ShowDatePickerDialogInitialDateIsTheDefault()
    {
        using FrameworkDartTester tester = CreateTester();
        PrepareDatePicker(tester, date =>
        {
            tester.Tap(Find.Text("OK"));
            Assert.Equal(new DateTime(2016, 1, 15), Await(date));
        });
    }

    // Flutter: 'date_picker_test.dart: showDatePicker Dialog Can cancel'
    [Fact]
    public void ShowDatePickerDialogCanCancel()
    {
        using FrameworkDartTester tester = CreateTester();
        PrepareDatePicker(tester, date =>
        {
            tester.Tap(Find.Text("CANCEL"));
            Assert.Null(Await(date));
        });
    }

    // Flutter: 'date_picker_test.dart: showDatePicker Dialog Can switch from calendar to input entry mode'
    [Fact]
    public void ShowDatePickerDialogCanSwitchFromCalendarToInputEntryMode()
    {
        using FrameworkDartTester tester = CreateTester();
        PrepareDatePicker(tester, _ =>
        {
            Finds.Nothing(Find.ByType<TextField>());
            tester.Tap(Find.ByIcon(Icons.Edit));
            tester.PumpAndSettle();
            Finds.OneWidget(Find.ByType<TextField>());
        });
    }

    // Flutter: 'date_picker_test.dart: showDatePicker Dialog Can switch from input to calendar entry mode'
    [Fact]
    public void ShowDatePickerDialogCanSwitchFromInputToCalendarEntryMode()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialEntryMode = DatePickerEntryMode.Input;
        PrepareDatePicker(tester, _ =>
        {
            Finds.OneWidget(Find.ByType<TextField>());
            tester.Tap(Find.ByIcon(Icons.CalendarToday));
            tester.PumpAndSettle();
            Finds.Nothing(Find.ByType<TextField>());
        });
    }

    // Flutter: 'date_picker_test.dart: showDatePicker Dialog Can not switch out of calendarOnly mode'
    [Fact]
    public void ShowDatePickerDialogCanNotSwitchOutOfCalendarOnlyMode()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialEntryMode = DatePickerEntryMode.CalendarOnly;
        PrepareDatePicker(tester, _ =>
        {
            Finds.Nothing(Find.ByType<TextField>());
            Finds.Nothing(Find.ByIcon(Icons.Edit));
        });
    }

    // Flutter: 'date_picker_test.dart: showDatePicker Dialog Can not switch out of inputOnly mode'
    [Fact]
    public void ShowDatePickerDialogCanNotSwitchOutOfInputOnlyMode()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialEntryMode = DatePickerEntryMode.InputOnly;
        PrepareDatePicker(tester, _ =>
        {
            Finds.OneWidget(Find.ByType<TextField>());
            Finds.Nothing(Find.ByIcon(Icons.CalendarToday));
        });
    }

    // Flutter: 'date_picker_test.dart: showDatePicker Dialog Switching to input mode keeps selected date'
    [Fact]
    public void ShowDatePickerDialogSwitchingToInputModeKeepsSelectedDate()
    {
        using FrameworkDartTester tester = CreateTester();
        PrepareDatePicker(tester, date =>
        {
            tester.Tap(Find.Text("12"));
            tester.Tap(Find.ByIcon(Icons.Edit));
            tester.PumpAndSettle();
            tester.Tap(Find.Text("OK"));
            Assert.Equal(new DateTime(2016, 1, 12), Await(date));
        });
    }

    // Flutter: 'date_picker_test.dart: showDatePicker Dialog Input only mode should validate date'
    [Fact]
    public void ShowDatePickerDialogInputOnlyModeShouldValidateDate()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialEntryMode = DatePickerEntryMode.InputOnly;
        PrepareDatePicker(tester, _ =>
        {
            // Enter text input mode and type an invalid date to get error.
            tester.EnterText(Find.ByType<TextField>(), "1234567");
            tester.Tap(Find.Text("OK"));
            tester.PumpAndSettle();
            Finds.OneWidget(Find.Text("Invalid format."));
        });
    }

    // Flutter: 'date_picker_test.dart: showDatePicker Dialog Switching to input mode resets input error state'
    [Fact]
    public void ShowDatePickerDialogSwitchingToInputModeResetsInputErrorState()
    {
        using FrameworkDartTester tester = CreateTester();
        PrepareDatePicker(tester, _ =>
        {
            // Enter text input mode and type an invalid date to get error.
            tester.Tap(Find.ByIcon(Icons.Edit));
            tester.PumpAndSettle();
            tester.EnterText(Find.ByType<TextField>(), "1234567");
            tester.Tap(Find.Text("OK"));
            tester.PumpAndSettle();
            Finds.OneWidget(Find.Text("Invalid format."));

            // Toggle to calendar mode and then back to input mode
            tester.Tap(Find.ByIcon(Icons.CalendarToday));
            tester.PumpAndSettle();
            tester.Tap(Find.ByIcon(Icons.Edit));
            tester.PumpAndSettle();
            Finds.Nothing(Find.Text("Invalid format."));

            // Edit the text, the error should not be showing until ok is tapped
            tester.EnterText(Find.ByType<TextField>(), "1234567");
            tester.PumpAndSettle();
            Finds.Nothing(Find.Text("Invalid format."));
        });
    }

    // Flutter: 'date_picker_test.dart: showDatePicker Dialog builder parameter'
    [Fact]
    public void ShowDatePickerDialogBuilderParameter()
    {
        using FrameworkDartTester tester = CreateTester();

        Widget BuildFrame(TextDirection textDirection) => ButtonApp(context =>
            _ = MaterialDatePickers.ShowDatePicker(
                context: context,
                initialDate: DateTime.Now,
                firstDate: new DateTime(2018, 1, 1),
                lastDate: new DateTime(2030, 1, 1),
                builder: (_, child) => new Directionality(textDirection, child ?? new SizedBox())));

        tester.PumpWidget(BuildFrame(TextDirection.Ltr));
        tester.Tap(Find.Text("X"));
        tester.PumpAndSettle();
        double ltrOkRight = tester.GetBottomRight(Find.Text("OK")).X;

        tester.Tap(Find.Text("OK")); // Dismiss the dialog.
        tester.PumpAndSettle();

        tester.PumpWidget(BuildFrame(TextDirection.Rtl));
        tester.Tap(Find.Text("X"));
        tester.PumpAndSettle();

        // Verify that the time picker is being laid out RTL.
        // We expect the left edge of the 'OK' button in the RTL
        // layout to match the gap between right edge of the 'OK'
        // button and the right edge of the 800 wide view.
        Assert.Equal(800 - ltrOkRight, tester.GetBottomLeft(Find.Text("OK")).X, 1e-10);
    }

    // ---- group('Barrier dismissible') ----

    private static Widget BarrierApp(
        NavigatorObserver? rootObserver,
        bool barrierDismissible = true,
        Color? barrierColor = null,
        string? barrierLabel = null) => ButtonApp(
        context => _ = MaterialDatePickers.ShowDatePicker(
            context: context,
            barrierColor: barrierColor,
            barrierLabel: barrierLabel,
            initialDate: DateTime.Now,
            firstDate: new DateTime(2018, 1, 1),
            lastDate: new DateTime(2030, 1, 1),
            barrierDismissible: barrierDismissible,
            builder: (_, _) => new SizedBox()),
        navigatorObservers: rootObserver == null ? null : [rootObserver]);

    // Flutter: 'date_picker_test.dart: showDatePicker Dialog Barrier dismissible Barrier is dismissible with
    // default parameter'
    [Fact]
    public void BarrierIsDismissibleWithDefaultParameter()
    {
        using FrameworkDartTester tester = CreateTester();
        var rootObserver = new DatePickerObserver();
        tester.PumpWidget(BarrierApp(rootObserver));

        // Open the dialog.
        tester.Tap(Find.ByType<ElevatedButton>());
        tester.PumpAndSettle();
        Assert.Equal(1, rootObserver.DatePickerCount);

        // Tap on the barrier.
        tester.TapAt(new Point(10.0, 10.0));
        tester.PumpAndSettle();
        Assert.Equal(0, rootObserver.DatePickerCount);
    }

    // Flutter: 'date_picker_test.dart: showDatePicker Dialog Barrier dismissible Barrier is not dismissible with
    // barrierDismissible is false'
    [Fact]
    public void BarrierIsNotDismissibleWithBarrierDismissibleIsFalse()
    {
        using FrameworkDartTester tester = CreateTester();
        var rootObserver = new DatePickerObserver();
        tester.PumpWidget(BarrierApp(rootObserver, barrierDismissible: false));

        // Open the dialog.
        tester.Tap(Find.ByType<ElevatedButton>());
        tester.PumpAndSettle();
        Assert.Equal(1, rootObserver.DatePickerCount);

        // Tap on the barrier, which shouldn't do anything this time.
        tester.TapAt(new Point(10.0, 10.0));
        tester.PumpAndSettle();
        Assert.Equal(1, rootObserver.DatePickerCount);
    }

    // Flutter: 'date_picker_test.dart: showDatePicker Dialog Barrier color'
    [Fact]
    public void ShowDatePickerDialogBarrierColor()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(BarrierApp(rootObserver: null));

        // Open the dialog.
        tester.Tap(Find.ByType<ElevatedButton>());
        tester.PumpAndSettle();
        Assert.Equal(MaterialColors.Black54, tester.Widget<ModalBarrier>(Find.ByType<ModalBarrier>().Last).Color);

        // Dismiss the dialog.
        tester.TapAt(new Point(10.0, 10.0));

        tester.PumpWidget(BarrierApp(rootObserver: null, barrierColor: MaterialColors.Pink));

        // Open the dialog.
        tester.Tap(Find.ByType<ElevatedButton>());
        tester.PumpAndSettle();
        Assert.Equal(MaterialColors.Pink, tester.Widget<ModalBarrier>(Find.ByType<ModalBarrier>().Last).Color);
    }

    // Flutter: 'date_picker_test.dart: showDatePicker Dialog Barrier Label'
    [Fact]
    public void ShowDatePickerDialogBarrierLabel()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(BarrierApp(rootObserver: null, barrierLabel: "Custom Label"));

        // Open the dialog.
        tester.Tap(Find.ByType<ElevatedButton>());
        tester.PumpAndSettle();
        Assert.Equal(
            "Custom Label",
            tester.Widget<ModalBarrier>(Find.ByType<ModalBarrier>().Last).SemanticsLabel);
    }

    // Flutter: 'date_picker_test.dart: showDatePicker Dialog uses nested navigator if useRootNavigator is false'
    [Fact]
    public void ShowDatePickerDialogUsesNestedNavigatorIfUseRootNavigatorIsFalse()
    {
        using FrameworkDartTester tester = CreateTester();
        var rootObserver = new DatePickerObserver();
        var nestedObserver = new DatePickerObserver();

        tester.PumpWidget(new MaterialApp(
            navigatorObservers: [rootObserver],
            home: new Navigator(
                observers: [nestedObserver],
                onGenerateRoute: settings => new MaterialPageRoute(
                    builder: context => new ElevatedButton(
                        onPressed: () => _ = MaterialDatePickers.ShowDatePicker(
                            context: context,
                            useRootNavigator: false,
                            initialDate: DateTime.Now,
                            firstDate: new DateTime(2018, 1, 1),
                            lastDate: new DateTime(2030, 1, 1),
                            builder: (_, _) => new SizedBox()),
                        child: new Text("Show Date Picker"))))));

        // Open the dialog.
        tester.Tap(Find.ByType<ElevatedButton>());

        Assert.Equal(0, rootObserver.DatePickerCount);
        Assert.Equal(1, nestedObserver.DatePickerCount);
    }

    // Flutter: 'date_picker_test.dart: showDatePicker Dialog honors DialogTheme for shape and elevation'
    [Fact]
    public void ShowDatePickerDialogHonorsDialogThemeForShapeAndElevation()
    {
        using FrameworkDartTester tester = CreateTester();
        // Test that the defaults work
        var datePickerDefaultDialogTheme = new DialogThemeData(
            Shape: new RoundedRectangleBorder(borderRadius: BorderRadius.All(Radius.Circular(4.0))),
            Elevation: 24);

        void OpenPicker(BuildContext context) => _ = MaterialDatePickers.ShowDatePicker(
            context: context,
            initialDate: DateTime.Now,
            firstDate: new DateTime(2018, 1, 1),
            lastDate: new DateTime(2030, 1, 1));

        tester.PumpWidget(ButtonApp(OpenPicker, theme: new ThemeData(useMaterial3: false), wrapInMaterial: false));
        tester.Tap(Find.Text("X"));
        tester.PumpAndSettle();
        MaterialWidget defaultDialogMaterial = DialogMaterial(tester);
        Assert.Equal(datePickerDefaultDialogTheme.Shape, defaultDialogMaterial.Shape);
        Assert.Equal(datePickerDefaultDialogTheme.Elevation, defaultDialogMaterial.Elevation);

        // Test that it honors ThemeData.dialogTheme settings
        var customDialogTheme = new DialogThemeData(
            Shape: new RoundedRectangleBorder(borderRadius: BorderRadius.All(Radius.Circular(40.0))),
            Elevation: 50);
        tester.PumpWidget(ButtonApp(
            OpenPicker,
            theme: new ThemeData(useMaterial3: false) with { DialogTheme = customDialogTheme },
            wrapInMaterial: false));
        tester.Pump(); // start theme animation
        tester.Pump(TimeSpan.FromSeconds(5)); // end theme animation
        MaterialWidget themeDialogMaterial = DialogMaterial(tester);
        Assert.Equal(customDialogTheme.Shape, themeDialogMaterial.Shape);
        Assert.Equal(customDialogTheme.Elevation, themeDialogMaterial.Elevation);
    }

    // Flutter: 'date_picker_test.dart: showDatePicker Dialog OK Cancel button layout'
    [Fact]
    public void ShowDatePickerDialogOkCancelButtonLayout()
    {
        using FrameworkDartTester tester = CreateTester();

        Widget BuildFrame(TextDirection textDirection) => ButtonApp(
            context => _ = MaterialDatePickers.ShowDatePicker(
                context: context,
                initialDate: new DateTime(2016, 1, 15),
                firstDate: new DateTime(2001, 1, 1),
                lastDate: new DateTime(2031, 12, 31),
                builder: (_, child) => new Directionality(textDirection, child ?? new SizedBox())),
            theme: new ThemeData(useMaterial3: false));

        try
        {
            // Default landscape layout.

            tester.PumpWidget(BuildFrame(TextDirection.Ltr));
            tester.Tap(Find.Text("X"));
            tester.PumpAndSettle();
            Assert.Equal(622, tester.GetBottomRight(Find.Text("OK")).X);
            Assert.Equal(594, tester.GetBottomLeft(Find.Text("OK")).X);
            Assert.Equal(560, tester.GetBottomRight(Find.Text("CANCEL")).X);
            tester.Tap(Find.Text("OK"));
            tester.PumpAndSettle();

            tester.PumpWidget(BuildFrame(TextDirection.Rtl));
            tester.Tap(Find.Text("X"));
            tester.PumpAndSettle();
            Assert.Equal(206, tester.GetBottomRight(Find.Text("OK")).X);
            Assert.Equal(178, tester.GetBottomLeft(Find.Text("OK")).X);
            Assert.Equal(324, tester.GetBottomRight(Find.Text("CANCEL")).X);
            tester.Tap(Find.Text("OK"));
            tester.PumpAndSettle();

            // Portrait layout.

            tester.View.PhysicalSize = new Size(900, 1200);

            tester.PumpWidget(BuildFrame(TextDirection.Ltr));
            tester.Tap(Find.Text("X"));
            tester.PumpAndSettle();
            Assert.Equal(258, tester.GetBottomRight(Find.Text("OK")).X);
            Assert.Equal(230, tester.GetBottomLeft(Find.Text("OK")).X);
            Assert.Equal(196, tester.GetBottomRight(Find.Text("CANCEL")).X);
            tester.Tap(Find.Text("OK"));
            tester.PumpAndSettle();

            tester.PumpWidget(BuildFrame(TextDirection.Rtl));
            tester.Tap(Find.Text("X"));
            tester.PumpAndSettle();
            Assert.Equal(70, tester.GetBottomRight(Find.Text("OK")).X);
            Assert.Equal(42, tester.GetBottomLeft(Find.Text("OK")).X);
            Assert.Equal(188, tester.GetBottomRight(Find.Text("CANCEL")).X);
            tester.Tap(Find.Text("OK"));
            tester.PumpAndSettle();
        }
        finally
        {
            tester.View.Reset();
        }
    }

    // Flutter: 'date_picker_test.dart: showDatePicker Dialog honors switchToInputEntryModeIcon'
    [Fact]
    public void ShowDatePickerDialogHonorsSwitchToInputEntryModeIcon()
    {
        using FrameworkDartTester tester = CreateTester();

        Widget BuildApp(bool? useMaterial3 = null, Icon? switchToInputEntryModeIcon = null) => ButtonApp(
            context => _ = MaterialDatePickers.ShowDatePicker(
                context: context,
                initialDate: DateTime.Now,
                firstDate: new DateTime(2018, 1, 1),
                lastDate: new DateTime(2030, 1, 1),
                switchToInputEntryModeIcon: switchToInputEntryModeIcon),
            theme: new ThemeData(useMaterial3: useMaterial3 ?? false),
            label: "Click X");

        tester.PumpWidget(BuildApp());
        tester.PumpAndSettle();
        tester.Tap(Find.ByType<ElevatedButton>());
        tester.PumpAndSettle();
        Finds.OneWidget(Find.ByIcon(Icons.Edit));
        tester.Tap(Find.Text("OK"));
        tester.PumpAndSettle();

        tester.PumpWidget(BuildApp(useMaterial3: true));
        tester.PumpAndSettle();
        tester.Tap(Find.ByType<ElevatedButton>());
        tester.PumpAndSettle();
        Finds.OneWidget(Find.ByIcon(Icons.EditOutlined));
        tester.Tap(Find.Text("OK"));
        tester.PumpAndSettle();

        tester.PumpWidget(BuildApp(switchToInputEntryModeIcon: new Icon(Icons.Keyboard)));
        tester.PumpAndSettle();
        tester.Tap(Find.ByType<ElevatedButton>());
        tester.PumpAndSettle();
        Finds.OneWidget(Find.ByIcon(Icons.Keyboard));
        tester.Tap(Find.Text("OK"));
        tester.PumpAndSettle();
    }

    // Flutter: 'date_picker_test.dart: showDatePicker Dialog honors switchToCalendarEntryModeIcon'
    [Fact]
    public void ShowDatePickerDialogHonorsSwitchToCalendarEntryModeIcon()
    {
        using FrameworkDartTester tester = CreateTester();

        Widget BuildApp(bool? useMaterial3 = null, Icon? switchToCalendarEntryModeIcon = null) => ButtonApp(
            context => _ = MaterialDatePickers.ShowDatePicker(
                context: context,
                initialDate: DateTime.Now,
                firstDate: new DateTime(2018, 1, 1),
                lastDate: new DateTime(2030, 1, 1),
                switchToCalendarEntryModeIcon: switchToCalendarEntryModeIcon,
                initialEntryMode: DatePickerEntryMode.Input),
            theme: new ThemeData(useMaterial3: useMaterial3 ?? false),
            label: "Click X");

        tester.PumpWidget(BuildApp());
        tester.PumpAndSettle();
        tester.Tap(Find.ByType<ElevatedButton>());
        tester.PumpAndSettle();
        Finds.OneWidget(Find.ByIcon(Icons.CalendarToday));
        tester.Tap(Find.Text("OK"));
        tester.PumpAndSettle();

        tester.PumpWidget(BuildApp(useMaterial3: true));
        tester.PumpAndSettle();
        tester.Tap(Find.ByType<ElevatedButton>());
        tester.PumpAndSettle();
        Finds.OneWidget(Find.ByIcon(Icons.CalendarToday));
        tester.Tap(Find.Text("OK"));
        tester.PumpAndSettle();

        tester.PumpWidget(BuildApp(switchToCalendarEntryModeIcon: new Icon(Icons.Favorite)));
        tester.PumpAndSettle();
        tester.Tap(Find.ByType<ElevatedButton>());
        tester.PumpAndSettle();
        Finds.OneWidget(Find.ByIcon(Icons.Favorite));
        tester.Tap(Find.Text("OK"));
        tester.PumpAndSettle();
    }

    // ---- group('Calendar mode') ----

    // Flutter: 'date_picker_test.dart: Calendar mode Default Calendar mode layout (Landscape)'
    [Fact]
    public void CalendarModeDefaultCalendarModeLayoutLandscape()
    {
        using FrameworkDartTester tester = CreateTester();
        Finder helpText = Find.Text("Select date");
        Finder headerText = Find.Text("Fri, Jan 15");
        Finder subHeaderText = Find.Text("January 2016");
        Finder cancelButtonText = Find.Text("Cancel");
        Finder okButtonText = Find.Text("OK");
        var insetPadding = new Thickness(16.0, 24.0);

        tester.View.PhysicalSize = WideWindowSize;
        try
        {
            tester.PumpWidget(new MaterialApp(
                home: new MaterialWidget(
                    child: new DatePickerDialog(
                        initialDate: _initialDate,
                        firstDate: _firstDate,
                        lastDate: _lastDate))));

            Finds.OneWidget(helpText);
            Finds.OneWidget(headerText);
            Finds.OneWidget(subHeaderText);
            Finds.OneWidget(cancelButtonText);
            Finds.OneWidget(okButtonText);

            // Test help text position.
            Point dialogTopLeft = tester.GetTopLeft(Find.ByType<AnimatedContainer>());
            Point helpTextTopLeft = tester.GetTopLeft(helpText);
            double insetHorizontal = insetPadding.Left + insetPadding.Right;
            Assert.Equal(dialogTopLeft.X + (insetHorizontal / 2), helpTextTopLeft.X);
            Assert.Equal(dialogTopLeft.Y + 16.0, helpTextTopLeft.Y);

            // Test header text position.
            Point headerTextTopLeft = tester.GetTopLeft(headerText);
            Point helpTextBottomLeft = tester.GetBottomLeft(helpText);
            Assert.Equal(dialogTopLeft.X + (insetHorizontal / 2), headerTextTopLeft.X);
            Assert.Equal(helpTextBottomLeft.Y + 16.0, headerTextTopLeft.Y);

            // Test switch button position.
            Finder switchButtonM3 = Find.WidgetWithIcon<IconButton>(Icons.EditOutlined);
            Point switchButtonTopLeft = tester.GetTopLeft(switchButtonM3);
            Point switchButtonBottomLeft = tester.GetBottomLeft(switchButtonM3);
            Point headerTextBottomLeft = tester.GetBottomLeft(headerText);
            Point dialogBottomLeft = tester.GetBottomLeft(Find.ByType<AnimatedContainer>());
            Assert.Equal(dialogTopLeft.X + 8.0, switchButtonTopLeft.X);
            Assert.Equal(headerTextBottomLeft.Y, switchButtonTopLeft.Y);
            Assert.Equal(dialogTopLeft.X + 8.0, switchButtonBottomLeft.X);
            Assert.Equal(dialogBottomLeft.Y - 6.0, switchButtonBottomLeft.Y);

            // Test vertical divider position.
            Finder divider = Find.ByType<VerticalDivider>();
            Point dividerTopLeft = tester.GetTopLeft(divider);
            Point headerTextTopRight = tester.GetTopRight(headerText);
            Assert.Equal(headerTextTopRight.X + 16.0, dividerTopLeft.X);
            Assert.Equal(dialogTopLeft.Y, dividerTopLeft.Y);

            // Test sub header text position.
            Point subHeaderTextTopLeft = tester.GetTopLeft(subHeaderText);
            Point dividerTopRight = tester.GetTopRight(divider);
            Assert.Equal(dividerTopRight.X + 24.0, subHeaderTextTopLeft.X);
            Assert.Equal(dialogTopLeft.Y + 16.0, subHeaderTextTopLeft.Y);

            // Test sub header icon position.
            Finder subHeaderIcon = Find.ByIcon(Icons.ArrowDropDown);
            Point subHeaderIconTopLeft = tester.GetTopLeft(subHeaderIcon);
            Point subHeaderTextTopRight = tester.GetTopRight(subHeaderText);
            Assert.Equal(subHeaderTextTopRight.X, subHeaderIconTopLeft.X);
            Assert.Equal(dialogTopLeft.Y + 14.0, subHeaderIconTopLeft.Y);

            // Test calendar page view position.
            Finder calendarPageView = Find.ByType<PageView>();
            Point calendarPageViewTopLeft = tester.GetTopLeft(calendarPageView);
            Point subHeaderTextBottomLeft = tester.GetBottomLeft(subHeaderText);
            Assert.Equal(dividerTopRight.X, calendarPageViewTopLeft.X);
            Assert.Equal(subHeaderTextBottomLeft.Y + 16.0, calendarPageViewTopLeft.Y);

            // Test month navigation icons position.
            Finder previousMonthButton = Find.WidgetWithIcon<IconButton>(Icons.ChevronLeft);
            Finder nextMonthButton = Find.WidgetWithIcon<IconButton>(Icons.ChevronRight);
            Point previousMonthButtonTopRight = tester.GetTopRight(previousMonthButton);
            Point nextMonthButtonTopRight = tester.GetTopRight(nextMonthButton);
            Point dialogTopRight = tester.GetTopRight(Find.ByType<AnimatedContainer>());
            Assert.Equal(dialogTopRight.X - 4.0, nextMonthButtonTopRight.X);
            Assert.Equal(dialogTopRight.Y + 2.0, nextMonthButtonTopRight.Y);
            Assert.Equal(nextMonthButtonTopRight.X - 48.0, previousMonthButtonTopRight.X);

            // Test action buttons position.
            Point dialogBottomRight = tester.GetBottomRight(Find.ByType<AnimatedContainer>());
            Point okButtonTopRight = tester.GetTopRight(Find.WidgetWithText<TextButton>("OK"));
            Point cancelButtonTopRight = tester.GetTopRight(Find.WidgetWithText<TextButton>("Cancel"));
            Point calendarPageViewBottomRight = tester.GetBottomRight(calendarPageView);
            Assert.Equal(dialogBottomRight.X - 8, okButtonTopRight.X);
            Assert.Equal(calendarPageViewBottomRight.Y + 2, okButtonTopRight.Y);
            Point okButtonTopLeft = tester.GetTopLeft(Find.WidgetWithText<TextButton>("OK"));
            Assert.Equal(okButtonTopLeft.X - 8, cancelButtonTopRight.X);
        }
        finally
        {
            tester.View.Reset();
        }
    }

    // Flutter: 'date_picker_test.dart: Calendar mode Default Calendar mode layout (Portrait)'
    [Fact]
    public void CalendarModeDefaultCalendarModeLayoutPortrait()
    {
        using FrameworkDartTester tester = CreateTester();
        Finder helpText = Find.Text("Select date");
        Finder headerText = Find.Text("Fri, Jan 15");
        Finder subHeaderText = Find.Text("January 2016");
        Finder cancelButtonText = Find.Text("Cancel");
        Finder okButtonText = Find.Text("OK");

        tester.View.PhysicalSize = NarrowWindowSize;
        try
        {
            tester.PumpWidget(new MaterialApp(
                home: new MaterialWidget(
                    child: new DatePickerDialog(
                        initialDate: _initialDate,
                        firstDate: _firstDate,
                        lastDate: _lastDate))));

            Finds.OneWidget(helpText);
            Finds.OneWidget(headerText);
            Finds.OneWidget(subHeaderText);
            Finds.OneWidget(cancelButtonText);
            Finds.OneWidget(okButtonText);

            // Test help text position.
            Point dialogTopLeft = tester.GetTopLeft(Find.ByType<AnimatedContainer>());
            Point helpTextTopLeft = tester.GetTopLeft(helpText);
            Assert.Equal(dialogTopLeft.X + 24.0, helpTextTopLeft.X);
            Assert.Equal(dialogTopLeft.Y + 16.0, helpTextTopLeft.Y);

            // Test header text position
            Point headerTextTextTopLeft = tester.GetTopLeft(headerText);
            Point helpTextBottomLeft = tester.GetBottomLeft(helpText);
            Assert.Equal(dialogTopLeft.X + 24.0, headerTextTextTopLeft.X);
            Assert.Equal(helpTextBottomLeft.Y + 28.0, headerTextTextTopLeft.Y);

            // Test switch button position.
            Finder switchButtonM3 = Find.WidgetWithIcon<IconButton>(Icons.EditOutlined);
            Point switchButtonTopRight = tester.GetTopRight(switchButtonM3);
            Point dialogTopRight = tester.GetTopRight(Find.ByType<AnimatedContainer>());
            Assert.Equal(dialogTopRight.X - 12.0, switchButtonTopRight.X);
            Assert.Equal(headerTextTextTopLeft.Y - 4.0, switchButtonTopRight.Y);

            // Test horizontal divider position.
            Finder divider = Find.ByType<Divider>();
            Point dividerTopLeft = tester.GetTopLeft(divider);
            Point headerTextBottomLeft = tester.GetBottomLeft(headerText);
            Assert.Equal(dialogTopLeft.X, dividerTopLeft.X);
            Assert.Equal(headerTextBottomLeft.Y + 16.0, dividerTopLeft.Y);

            // Test subHeaderText position.
            Point subHeaderTextTopLeft = tester.GetTopLeft(subHeaderText);
            Point dividerBottomLeft = tester.GetBottomLeft(divider);
            Assert.Equal(dialogTopLeft.X + 24.0, subHeaderTextTopLeft.X);
            Assert.Equal(dividerBottomLeft.Y + 16.0, subHeaderTextTopLeft.Y);

            // Test sub header icon position.
            Finder subHeaderIcon = Find.ByIcon(Icons.ArrowDropDown);
            Point subHeaderIconTopLeft = tester.GetTopLeft(subHeaderIcon);
            Point subHeaderTextTopRight = tester.GetTopRight(subHeaderText);
            Assert.Equal(subHeaderTextTopRight.X, subHeaderIconTopLeft.X);
            Assert.Equal(dividerBottomLeft.Y + 14.0, subHeaderIconTopLeft.Y);

            // Test month navigation icons position.
            Finder previousMonthButton = Find.WidgetWithIcon<IconButton>(Icons.ChevronLeft);
            Finder nextMonthButton = Find.WidgetWithIcon<IconButton>(Icons.ChevronRight);
            Point previousMonthButtonTopRight = tester.GetTopRight(previousMonthButton);
            Point nextMonthButtonTopRight = tester.GetTopRight(nextMonthButton);
            Assert.Equal(dialogTopRight.X - 4.0, nextMonthButtonTopRight.X);
            Assert.Equal(dividerBottomLeft.Y + 2.0, nextMonthButtonTopRight.Y);
            Assert.Equal(nextMonthButtonTopRight.X - 48.0, previousMonthButtonTopRight.X);

            // Test calendar page view position.
            Finder calendarPageView = Find.ByType<PageView>();
            Point calendarPageViewTopLeft = tester.GetTopLeft(calendarPageView);
            Point subHeaderTextBottomLeft = tester.GetBottomLeft(subHeaderText);
            Assert.Equal(dialogTopLeft.X, calendarPageViewTopLeft.X);
            Assert.Equal(subHeaderTextBottomLeft.Y + 16.0, calendarPageViewTopLeft.Y);

            // Test action buttons position.
            Point dialogBottomRight = tester.GetBottomRight(Find.ByType<AnimatedContainer>());
            Point okButtonTopRight = tester.GetTopRight(Find.WidgetWithText<TextButton>("OK"));
            Point cancelButtonTopRight = tester.GetTopRight(Find.WidgetWithText<TextButton>("Cancel"));
            Point calendarPageViewBottomRight = tester.GetBottomRight(calendarPageView);
            Point okButtonTopLeft = tester.GetTopLeft(Find.WidgetWithText<TextButton>("OK"));
            Assert.Equal(dialogBottomRight.X - 8, okButtonTopRight.X);
            Assert.Equal(calendarPageViewBottomRight.Y + 2, okButtonTopRight.Y);
            Assert.Equal(okButtonTopLeft.X - 8, cancelButtonTopRight.X);
        }
        finally
        {
            tester.View.Reset();
        }
    }

    // Flutter: 'date_picker_test.dart: Calendar mode Can select a day'
    [Fact]
    public void CalendarModeCanSelectADay()
    {
        using FrameworkDartTester tester = CreateTester();
        PrepareDatePicker(tester, date =>
        {
            tester.Tap(Find.Text("12"));
            tester.Tap(Find.Text("OK"));
            Assert.Equal(new DateTime(2016, 1, 12), Await(date));
        });
    }

    // Flutter: 'date_picker_test.dart: Calendar mode Can select a month'
    [Fact]
    public void CalendarModeCanSelectAMonth()
    {
        using FrameworkDartTester tester = CreateTester();
        PrepareDatePicker(tester, date =>
        {
            tester.Tap(PreviousMonthIcon);
            tester.PumpAndSettle(TimeSpan.FromSeconds(1));
            tester.Tap(Find.Text("25"));
            tester.Tap(Find.Text("OK"));
            Assert.Equal(new DateTime(2015, 12, 25), Await(date));
        });
    }

    // Flutter: 'date_picker_test.dart: Calendar mode Can select a year'
    [Fact]
    public void CalendarModeCanSelectAYear()
    {
        using FrameworkDartTester tester = CreateTester();
        PrepareDatePicker(tester, _ =>
        {
            tester.Tap(Find.Text("January 2016")); // Switch to year mode.
            tester.Pump();
            tester.Tap(Find.Text("2018"));
            tester.Pump();
            Finds.OneWidget(Find.Text("January 2018"));
        });
    }

    // Flutter: 'date_picker_test.dart: Calendar mode Can select a day with no initial date'
    [Fact]
    public void CalendarModeCanSelectADayWithNoInitialDate()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialDate = null;
        PrepareDatePicker(tester, date =>
        {
            tester.Tap(Find.Text("12"));
            tester.Tap(Find.Text("OK"));
            Assert.Equal(new DateTime(2016, 1, 12), Await(date));
        });
    }

    // Flutter: 'date_picker_test.dart: Calendar mode Can select a month with no initial date'
    [Fact]
    public void CalendarModeCanSelectAMonthWithNoInitialDate()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialDate = null;
        PrepareDatePicker(tester, date =>
        {
            tester.Tap(PreviousMonthIcon);
            tester.PumpAndSettle(TimeSpan.FromSeconds(1));
            tester.Tap(Find.Text("25"));
            tester.Tap(Find.Text("OK"));
            Assert.Equal(new DateTime(2015, 12, 25), Await(date));
        });
    }

    // Flutter: 'date_picker_test.dart: Calendar mode Can select a year with no initial date'
    [Fact]
    public void CalendarModeCanSelectAYearWithNoInitialDate()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialDate = null;
        PrepareDatePicker(tester, _ =>
        {
            tester.Tap(Find.Text("January 2016")); // Switch to year mode.
            tester.Pump();
            tester.Tap(Find.Text("2018"));
            tester.Pump();
            Finds.OneWidget(Find.Text("January 2018"));
        });
    }

    // Flutter: 'date_picker_test.dart: Calendar mode Selecting date does not change displayed month'
    [Fact]
    public void CalendarModeSelectingDateDoesNotChangeDisplayedMonth()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialDate = new DateTime(2020, 3, 15);
        PrepareDatePicker(tester, _ =>
        {
            tester.Tap(NextMonthIcon);
            tester.PumpAndSettle(TimeSpan.FromSeconds(1));
            Finds.OneWidget(Find.Text("April 2020"));
            tester.Tap(Find.Text("25"));
            tester.PumpAndSettle();
            Finds.OneWidget(Find.Text("April 2020"));
            // There isn't a 31 in April so there shouldn't be one if it is showing April
            Finds.Nothing(Find.Text("31"));
        });
    }

    // Flutter: 'date_picker_test.dart: Calendar mode Changing year does change selected date'
    [Fact]
    public void CalendarModeChangingYearDoesChangeSelectedDate()
    {
        using FrameworkDartTester tester = CreateTester();
        PrepareDatePicker(tester, date =>
        {
            tester.Tap(Find.Text("January 2016"));
            tester.Pump();
            tester.Tap(Find.Text("2018"));
            tester.Pump();
            tester.Tap(Find.Text("OK"));
            Assert.Equal(new DateTime(2018, 1, 15), Await(date));
        });
    }

    // Flutter: 'date_picker_test.dart: Calendar mode Changing year does not change the month'
    [Fact]
    public void CalendarModeChangingYearDoesNotChangeTheMonth()
    {
        using FrameworkDartTester tester = CreateTester();
        PrepareDatePicker(tester, _ =>
        {
            tester.Tap(NextMonthIcon);
            tester.PumpAndSettle();
            tester.Tap(NextMonthIcon);
            tester.PumpAndSettle();
            tester.Tap(Find.Text("March 2016"));
            tester.PumpAndSettle();
            tester.Tap(Find.Text("2018"));
            tester.PumpAndSettle();
            Finds.OneWidget(Find.Text("March 2018"));
        });
    }

    // Flutter: 'date_picker_test.dart: Calendar mode Can select a year and then a day'
    [Fact]
    public void CalendarModeCanSelectAYearAndThenADay()
    {
        using FrameworkDartTester tester = CreateTester();
        PrepareDatePicker(tester, date =>
        {
            tester.Tap(Find.Text("January 2016")); // Switch to year mode.
            tester.Pump();
            tester.Tap(Find.Text("2017"));
            tester.Pump();
            tester.Tap(Find.Text("19"));
            tester.Tap(Find.Text("OK"));
            Assert.Equal(new DateTime(2017, 1, 19), Await(date));
        });
    }

    // Flutter: 'date_picker_test.dart: Calendar mode Current year is visible in year picker'
    [Fact]
    public void CalendarModeCurrentYearIsVisibleInYearPicker()
    {
        using FrameworkDartTester tester = CreateTester();
        PrepareDatePicker(tester, _ =>
        {
            tester.Tap(Find.Text("January 2016")); // Switch to year mode.
            tester.Pump();
            Finds.OneWidget(Find.Text("2016"));
        });
    }

    // Flutter: 'date_picker_test.dart: Calendar mode Cannot select a day outside bounds'
    [Fact]
    public void CalendarModeCannotSelectADayOutsideBounds()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialDate = new DateTime(2017, 1, 15);
        _firstDate = _initialDate.Value;
        _lastDate = _initialDate.Value;
        PrepareDatePicker(tester, date =>
        {
            // Earlier than firstDate. Should be ignored.
            tester.Tap(Find.Text("10"));
            // Later than lastDate. Should be ignored.
            tester.Tap(Find.Text("20"));
            tester.Tap(Find.Text("OK"));
            // We should still be on the initial date.
            Assert.Equal(_initialDate, Await(date));
        });
    }

    // Flutter: 'date_picker_test.dart: Calendar mode Cannot select a month past last date'
    [Fact]
    public void CalendarModeCannotSelectAMonthPastLastDate()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialDate = new DateTime(2017, 1, 15);
        _firstDate = _initialDate.Value;
        _lastDate = new DateTime(2017, 2, 20);
        PrepareDatePicker(tester, _ =>
        {
            tester.Tap(NextMonthIcon);
            tester.PumpAndSettle(TimeSpan.FromSeconds(1));
            // Shouldn't be possible to keep going into March.
            Finds.Nothing(NextMonthIcon);
        });
    }

    // Flutter: 'date_picker_test.dart: Calendar mode Cannot select a month before first date'
    [Fact]
    public void CalendarModeCannotSelectAMonthBeforeFirstDate()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialDate = new DateTime(2017, 1, 15);
        _firstDate = new DateTime(2016, 12, 10);
        _lastDate = _initialDate.Value;
        PrepareDatePicker(tester, _ =>
        {
            tester.Tap(PreviousMonthIcon);
            tester.PumpAndSettle(TimeSpan.FromSeconds(1));
            // Shouldn't be possible to keep going into November.
            Finds.Nothing(PreviousMonthIcon);
        });
    }

    // Flutter: 'date_picker_test.dart: Calendar mode Cannot select disabled year'
    [Fact]
    public void CalendarModeCannotSelectDisabledYear()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialDate = new DateTime(2018, 7, 4);
        _firstDate = new DateTime(2018, 6, 9);
        _lastDate = new DateTime(2018, 12, 15);
        PrepareDatePicker(tester, date =>
        {
            tester.Tap(Find.Text("July 2018")); // Switch to year mode.
            tester.PumpAndSettle();
            tester.Tap(Find.Text("2016")); // Disabled, doesn't change the year.
            tester.PumpAndSettle();
            tester.Tap(Find.Text("OK"));
            tester.PumpAndSettle();
            Assert.Equal(new DateTime(2018, 7, 4), Await(date));
        });
    }

    // Flutter: 'date_picker_test.dart: Calendar mode Selecting firstDate year respects firstDate'
    [Fact]
    public void CalendarModeSelectingFirstDateYearRespectsFirstDate()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialDate = new DateTime(2018, 5, 4);
        _firstDate = new DateTime(2016, 6, 9);
        _lastDate = new DateTime(2019, 1, 15);
        PrepareDatePicker(tester, _ =>
        {
            tester.Tap(Find.Text("May 2018"));
            tester.PumpAndSettle();
            tester.Tap(Find.Text("2016"));
            tester.PumpAndSettle();
            // Month should be clamped to June as the range starts at June 2016
            Finds.OneWidget(Find.Text("June 2016"));
        });
    }

    // Flutter: 'date_picker_test.dart: Calendar mode Selecting lastDate year respects lastDate'
    [Fact]
    public void CalendarModeSelectingLastDateYearRespectsLastDate()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialDate = new DateTime(2018, 5, 4);
        _firstDate = new DateTime(2016, 6, 9);
        _lastDate = new DateTime(2019, 1, 15);
        PrepareDatePicker(tester, _ =>
        {
            tester.Tap(Find.Text("May 2018"));
            tester.PumpAndSettle();
            tester.Tap(Find.Text("2019"));
            tester.PumpAndSettle();
            // Month should be clamped to January as the range ends at January 2019
            Finds.OneWidget(Find.Text("January 2019"));
        });
    }

    // Flutter: 'date_picker_test.dart: Calendar mode Only predicate days are selectable'
    [Fact]
    public void CalendarModeOnlyPredicateDaysAreSelectable()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialDate = new DateTime(2017, 1, 16);
        _firstDate = new DateTime(2017, 1, 10);
        _lastDate = new DateTime(2017, 1, 20);
        _selectableDayPredicate = day => day.Day % 2 == 0;
        PrepareDatePicker(tester, date =>
        {
            tester.Tap(Find.Text("13")); // Odd, doesn't work.
            tester.Tap(Find.Text("10")); // Even, works.
            tester.Tap(Find.Text("17")); // Odd, doesn't work.
            tester.Tap(Find.Text("OK"));
            Assert.Equal(new DateTime(2017, 1, 10), Await(date));
        });
    }

    // Flutter: 'date_picker_test.dart: Calendar mode Can select initial calendar picker mode'
    [Fact]
    public void CalendarModeCanSelectInitialCalendarPickerMode()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialDate = new DateTime(2014, 1, 15);
        _initialCalendarMode = DatePickerMode.Year;
        PrepareDatePicker(tester, _ =>
        {
            tester.Pump();
            // 2018 wouldn't be available if the year picker wasn't showing.
            // The initial current year is 2014.
            tester.Tap(Find.Text("2018"));
            tester.Pump();
            Finds.OneWidget(Find.Text("January 2018"));
        });
    }

    // Flutter: 'date_picker_test.dart: Calendar mode currentDate is highlighted'
    [Fact]
    public void CalendarModeCurrentDateIsHighlighted()
    {
        using FrameworkDartTester tester = CreateTester();
        _today = new DateTime(2016, 1, 2);
        PrepareDatePicker(tester, _ =>
        {
            tester.Pump();
            var todayColor = new Color(0xff2196f3); // default primary color
            PaintAssert.Paints(
                FindDayGridMaterial(tester),
                // The current day should be painted with a circle outline
                PaintPattern.Paints.Circle(color: todayColor, style: PaintingStyle.Stroke, strokeWidth: 1.0));
        });
    }

    private void ExpectDayOverlayPaints(FrameworkDartTester tester, PaintPattern pattern) =>
        PaintAssert.Paints(FindDayGridMaterial(tester), pattern);

    // Flutter: 'date_picker_test.dart: Calendar mode Date picker dayOverlayColor resolves hovered state'
    [Fact]
    public void CalendarModeDatePickerDayOverlayColorResolvesHoveredState()
    {
        using FrameworkDartTester tester = CreateTester();
        var theme = new ThemeData();
        PrepareDatePicker(tester, _ => { }, theme: theme);

        Point center = tester.GetCenter(Find.Text("30"));
        TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        gesture.AddPointer();
        try
        {
            gesture.MoveTo(center);
            tester.PumpAndSettle();

            ExpectDayOverlayPaints(
                tester,
                PaintPattern.Paints
                    .Circle() // Today decoration.
                    .Circle() // Selected day decoration.
                    .Circle(color: theme.ColorScheme.OnSurfaceVariant.WithOpacity(0.08)));
        }
        finally
        {
            gesture.RemovePointer();
        }
    }

    // Flutter: 'date_picker_test.dart: Calendar mode Date picker dayOverlayColor resolves focused state'
    [Fact]
    public void CalendarModeDatePickerDayOverlayColorResolvesFocusedState()
    {
        using FrameworkDartTester tester = CreateTester();
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
        var theme = new ThemeData();
        PrepareDatePicker(tester, _ => { }, theme: theme);

        // Navigate to the grid.
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);

        // Navigate to day 30.
        tester.SendKeyEvent(LogicalKeyboardKey.ArrowDown);
        tester.SendKeyEvent(LogicalKeyboardKey.ArrowDown);
        tester.SendKeyEvent(LogicalKeyboardKey.ArrowRight);
        tester.PumpAndSettle();

        ExpectDayOverlayPaints(
            tester,
            PaintPattern.Paints
                .Circle() // Today decoration.
                .Circle() // Selected day decoration.
                .Circle(color: theme.ColorScheme.OnSurfaceVariant.WithOpacity(0.10)));
    }

    // Flutter: 'date_picker_test.dart: Calendar mode Date picker dayOverlayColor resolves pressed state'
    [Fact]
    public void CalendarModeDatePickerDayOverlayColorResolvesPressedState()
    {
        using FrameworkDartTester tester = CreateTester();
        var theme = new ThemeData();
        PrepareDatePicker(tester, _ => { }, theme: theme);

        Point center = tester.GetCenter(Find.Text("30"));
        TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        gesture.AddPointer();
        try
        {
            gesture.Down(center);
            tester.PumpAndSettle();

            ExpectDayOverlayPaints(
                tester,
                PaintPattern.Paints
                    .Circle() // Today decoration.
                    .Circle() // Selected day decoration.
                    .Circle() // Hovered decoration.
                    .Circle(color: theme.ColorScheme.OnSurfaceVariant.WithOpacity(0.10)));
            gesture.Up();
        }
        finally
        {
            gesture.RemovePointer();
        }
    }

    // Flutter: 'date_picker_test.dart: Calendar mode Date picker dayOverlayColor resolves selected and hovered
    // state'
    // Regression test for https://github.com/flutter/flutter/issues/130586.
    [Fact]
    public void CalendarModeDatePickerDayOverlayColorResolvesSelectedAndHoveredState()
    {
        using FrameworkDartTester tester = CreateTester();
        var theme = new ThemeData();
        PrepareDatePicker(tester, _ => { }, theme: theme);

        // Select day 30.
        tester.Tap(Find.Text("30"));
        tester.PumpAndSettle();
        ShapeDecoration day30Decoration = FindDayDecoration(tester, "30")!;
        Assert.Equal(theme.ColorScheme.Primary, day30Decoration.Color);

        Point center = tester.GetCenter(Find.Text("30"));
        TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        gesture.AddPointer();
        try
        {
            gesture.MoveTo(center);
            tester.PumpAndSettle();

            ExpectDayOverlayPaints(
                tester,
                PaintPattern.Paints
                    .Circle() // Today decoration.
                    .Circle() // Selected day decoration.
                    .Circle(color: theme.ColorScheme.OnPrimary.WithOpacity(0.08)));
        }
        finally
        {
            gesture.RemovePointer();
        }
    }

    // Flutter: 'date_picker_test.dart: Calendar mode Date picker dayOverlayColor resolves selected and focused
    // state'
    [Fact]
    public void CalendarModeDatePickerDayOverlayColorResolvesSelectedAndFocusedState()
    {
        using FrameworkDartTester tester = CreateTester();
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
        var theme = new ThemeData();
        PrepareDatePicker(tester, _ => { }, theme: theme);

        // Select day 30.
        tester.Tap(Find.Text("30"));
        tester.PumpAndSettle();
        ShapeDecoration day30Decoration = FindDayDecoration(tester, "30")!;
        Assert.Equal(theme.ColorScheme.Primary, day30Decoration.Color);

        // Navigate to the grid.
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.PumpAndSettle();

        // Day 30 is selected and focused.
        ExpectDayOverlayPaints(
            tester,
            PaintPattern.Paints
                .Circle() // Today decoration.
                .Circle() // Selected day decoration.
                .Circle(color: theme.ColorScheme.OnPrimary.WithOpacity(0.10)));
    }

    // Flutter: 'date_picker_test.dart: Calendar mode Date picker dayOverlayColor resolves selected and pressed
    // state'
    [Fact]
    public void CalendarModeDatePickerDayOverlayColorResolvesSelectedAndPressedState()
    {
        using FrameworkDartTester tester = CreateTester();
        var theme = new ThemeData();
        PrepareDatePicker(tester, _ => { }, theme: theme);

        // Select day 30.
        tester.Tap(Find.Text("30"));
        tester.PumpAndSettle();
        ShapeDecoration day30Decoration = FindDayDecoration(tester, "30")!;
        Assert.Equal(theme.ColorScheme.Primary, day30Decoration.Color);

        Point center = tester.GetCenter(Find.Text("30"));
        TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        gesture.AddPointer();
        try
        {
            gesture.Down(center);
            tester.PumpAndSettle();

            ExpectDayOverlayPaints(
                tester,
                PaintPattern.Paints
                    .Circle() // Today decoration.
                    .Circle() // Selected day decoration.
                    .Circle() // Hovered decoration.
                    .Circle(color: theme.ColorScheme.OnPrimary.WithOpacity(0.10)));
            gesture.Up();
        }
        finally
        {
            gesture.RemovePointer();
        }
    }

    // Flutter: 'date_picker_test.dart: Calendar mode Selecting date does not switch picker to year selection'
    [Fact]
    public void CalendarModeSelectingDateDoesNotSwitchPickerToYearSelection()
    {
        using FrameworkDartTester tester = CreateTester();
        _initialDate = new DateTime(2020, 5, 10);
        _initialCalendarMode = DatePickerMode.Year;
        PrepareDatePicker(tester, _ =>
        {
            tester.Pump();
            tester.Tap(Find.Text("2017"));
            tester.Pump();
            Finds.OneWidget(Find.Text("May 2017"));
            tester.Tap(Find.Text("10"));
            tester.Pump();
            Finds.OneWidget(Find.Text("May 2017"));
            Finds.Nothing(Find.Text("2017"));
        });
    }

    // Flutter: 'date_picker_test.dart: Calendar mode Calendar dialog contents are visible - textScaler 0.88, 1.0,
    // 2.0'
    [Fact(Skip = GoldenSkip)]
    public void CalendarModeCalendarDialogContentsAreVisibleTextScaler()
    {
    }

    // ---- group('Input mode') ----

    private void InputModeSetUp()
    {
        _firstDate = new DateTime(2015, 1, 1);
        _lastDate = new DateTime(2017, 12, 31);
        _initialDate = new DateTime(2016, 1, 15);
        _initialEntryMode = DatePickerEntryMode.Input;
    }

    // Flutter: 'date_picker_test.dart: Input mode Default InputDecoration'
    [Fact]
    public void InputModeDefaultInputDecoration()
    {
        using FrameworkDartTester tester = CreateTester();
        InputModeSetUp();
        PrepareDatePicker(tester, _ =>
        {
            InputDecoration decoration = tester.Widget<TextField>(Find.ByType<TextField>()).Decoration!;
            Assert.Equal(new OutlineInputBorder(), decoration.Border);
            Assert.False(decoration.Filled);
            Assert.Equal("mm/dd/yyyy", decoration.HintText);
            Assert.Equal("Enter Date", decoration.LabelText);
            Assert.Null(decoration.ErrorText);
        }, useMaterial3: true);
    }

    // Flutter: 'date_picker_test.dart: Input mode Initial entry mode is used'
    [Fact]
    public void InputModeInitialEntryModeIsUsed()
    {
        using FrameworkDartTester tester = CreateTester();
        InputModeSetUp();
        PrepareDatePicker(tester, _ => Finds.OneWidget(Find.ByType<TextField>()));
    }

    // Flutter: 'date_picker_test.dart: Input mode Hint, label, and help text is used'
    [Fact]
    public void InputModeHintLabelAndHelpTextIsUsed()
    {
        using FrameworkDartTester tester = CreateTester();
        InputModeSetUp();
        _cancelText = "nope";
        _confirmText = "yep";
        _fieldHintText = "hint";
        _fieldLabelText = "label";
        _helpText = "help";
        PrepareDatePicker(tester, _ =>
        {
            Finds.OneWidget(Find.Text(_cancelText!));
            Finds.OneWidget(Find.Text(_confirmText!));
            Finds.OneWidget(Find.Text(_fieldHintText!));
            Finds.OneWidget(Find.Text(_fieldLabelText!));
            Finds.OneWidget(Find.Text(_helpText!));
        });
    }

    // Flutter: 'date_picker_test.dart: Input mode KeyboardType is used'
    [Fact]
    public void InputModeKeyboardTypeIsUsed()
    {
        using FrameworkDartTester tester = CreateTester();
        InputModeSetUp();
        _keyboardType = TextInputType.Text;
        PrepareDatePicker(tester, _ =>
        {
            TextField field = TextField(tester);
            Assert.Equal(TextInputType.Text, field.KeyboardType);
        });
    }

    // Flutter: 'date_picker_test.dart: Input mode Initial date is the default'
    [Fact]
    public void InputModeInitialDateIsTheDefault()
    {
        using FrameworkDartTester tester = CreateTester();
        InputModeSetUp();
        PrepareDatePicker(tester, date =>
        {
            tester.Tap(Find.Text("OK"));
            Assert.Equal(new DateTime(2016, 1, 15), Await(date));
        });
    }

    // Flutter: 'date_picker_test.dart: Input mode Can toggle to calendar entry mode'
    [Fact]
    public void InputModeCanToggleToCalendarEntryMode()
    {
        using FrameworkDartTester tester = CreateTester();
        InputModeSetUp();
        PrepareDatePicker(tester, _ =>
        {
            Finds.OneWidget(Find.ByType<TextField>());
            tester.Tap(Find.ByIcon(Icons.CalendarToday));
            tester.PumpAndSettle();
            Finds.Nothing(Find.ByType<TextField>());
        });
    }

    // Flutter: 'date_picker_test.dart: Input mode Toggle to calendar mode keeps selected date'
    [Fact]
    public void InputModeToggleToCalendarModeKeepsSelectedDate()
    {
        using FrameworkDartTester tester = CreateTester();
        InputModeSetUp();
        PrepareDatePicker(tester, date =>
        {
            TextField field = TextField(tester);
            field.Controller!.Clear();

            tester.EnterText(Find.ByType<TextField>(), "12/25/2016");
            tester.Tap(Find.ByIcon(Icons.CalendarToday));
            tester.PumpAndSettle();
            tester.Tap(Find.Text("OK"));
            Assert.Equal(new DateTime(2016, 12, 25), Await(date));
        });
    }

    // Flutter: 'date_picker_test.dart: Input mode Entered text returns date'
    [Fact]
    public void InputModeEnteredTextReturnsDate()
    {
        using FrameworkDartTester tester = CreateTester();
        InputModeSetUp();
        PrepareDatePicker(tester, date =>
        {
            TextField field = TextField(tester);
            field.Controller!.Clear();

            tester.EnterText(Find.ByType<TextField>(), "12/25/2016");
            tester.Tap(Find.Text("OK"));
            Assert.Equal(new DateTime(2016, 12, 25), Await(date));
        });
    }

    // Flutter: 'date_picker_test.dart: Input mode Too short entered text shows error'
    [Fact]
    public void InputModeTooShortEnteredTextShowsError()
    {
        using FrameworkDartTester tester = CreateTester();
        InputModeSetUp();
        _errorFormatText = "oops";
        PrepareDatePicker(tester, _ =>
        {
            TextField field = TextField(tester);
            field.Controller!.Clear();

            tester.PumpAndSettle();
            tester.EnterText(Find.ByType<TextField>(), "1225");
            Finds.Nothing(Find.Text(_errorFormatText!));

            tester.Tap(Find.Text("OK"));
            tester.PumpAndSettle();
            Finds.OneWidget(Find.Text(_errorFormatText!));
        });
    }

    // Flutter: 'date_picker_test.dart: Input mode Bad format entered text shows error'
    [Fact]
    public void InputModeBadFormatEnteredTextShowsError()
    {
        using FrameworkDartTester tester = CreateTester();
        InputModeSetUp();
        _errorFormatText = "oops";
        PrepareDatePicker(tester, _ =>
        {
            TextField field = TextField(tester);
            field.Controller!.Clear();

            tester.PumpAndSettle();
            tester.EnterText(Find.ByType<TextField>(), "20 days, 3 months, 2003");
            Finds.OneWidget(Find.Text("20 days, 3 months, 2003"));
            Finds.Nothing(Find.Text(_errorFormatText!));

            tester.Tap(Find.Text("OK"));
            tester.PumpAndSettle();
            Finds.OneWidget(Find.Text(_errorFormatText!));
        });
    }

    // Flutter: 'date_picker_test.dart: Input mode Invalid entered text shows error'
    [Fact]
    public void InputModeInvalidEnteredTextShowsError()
    {
        using FrameworkDartTester tester = CreateTester();
        InputModeSetUp();
        _errorInvalidText = "oops";
        PrepareDatePicker(tester, _ =>
        {
            TextField field = TextField(tester);
            field.Controller!.Clear();

            tester.PumpAndSettle();
            tester.EnterText(Find.ByType<TextField>(), "08/10/1969");
            Finds.Nothing(Find.Text(_errorInvalidText!));

            tester.Tap(Find.Text("OK"));
            tester.PumpAndSettle();
            Finds.OneWidget(Find.Text(_errorInvalidText!));
        });
    }

    // Flutter: 'date_picker_test.dart: Input mode Invalid entered text shows error on autovalidate'
    // This is a regression test for https://github.com/flutter/flutter/issues/126397.
    [Fact]
    public void InputModeInvalidEnteredTextShowsErrorOnAutovalidate()
    {
        using FrameworkDartTester tester = CreateTester();
        InputModeSetUp();
        PrepareDatePicker(tester, _ =>
        {
            TextField field = TextField(tester);
            field.Controller!.Clear();

            // Enter some text to trigger autovalidate.
            tester.EnterText(Find.ByType<TextField>(), "xyz");
            tester.Tap(Find.Text("OK"));
            tester.PumpAndSettle();

            // Invalid format validation error should be shown.
            Finds.OneWidget(Find.Text("Invalid format."));

            // Clear the text.
            field.Controller!.Clear();

            // Enter an invalid date that is too long while autovalidate is still on.
            tester.EnterText(Find.ByType<TextField>(), "10/05/2023666777889");
            tester.Pump();

            // Invalid format validation error should be shown.
            Finds.OneWidget(Find.Text("Invalid format."));
            // Should not throw an exception.
            Assert.Null(tester.TakeException());
        });
    }

    // Flutter: 'date_picker_test.dart: Input mode Dialog contents do not overflow when resized during orientation
    // change'
    // This is a regression test for https://github.com/flutter/flutter/issues/131989.
    [Fact]
    public void InputModeDialogContentsDoNotOverflowWhenResizedDuringOrientationChange()
    {
        using FrameworkDartTester tester = CreateTester();
        InputModeSetUp();
        try
        {
            // Initial window size is wide for landscape mode.
            tester.View.PhysicalSize = WideWindowSize;
            tester.View.DevicePixelRatio = 1.0;

            PrepareDatePicker(tester, _ =>
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

    // Flutter: 'date_picker_test.dart: Input mode Text field stays visible when orientation is portrait and height
    // is reduced'
    // Regression test for https://github.com/flutter/flutter/issues/140311.
    [Fact]
    public void InputModeTextFieldStaysVisibleWhenOrientationIsPortraitAndHeightIsReduced()
    {
        using FrameworkDartTester tester = CreateTester();
        InputModeSetUp();
        try
        {
            tester.View.PhysicalSize = new Size(720, 1280);
            tester.View.DevicePixelRatio = 1.0;
            _initialEntryMode = DatePickerEntryMode.Input;

            // Text field and header are visible by default.
            PrepareDatePicker(tester, _ =>
            {
                Finds.OneWidget(Find.ByType<TextField>());
                Finds.OneWidget(Find.Text("Select date"));
            }, useMaterial3: true);

            // Simulate the portait mode on a device with a small display when the virtual
            // keyboard is visible.
            tester.View.ViewInsets = new Thickness(0, 0, 0, 1000);
            tester.PumpAndSettle();

            // Text field is visible and header is hidden.
            Finds.OneWidget(Find.ByType<TextField>());
            Finds.Nothing(Find.Text("Select date"));
        }
        finally
        {
            tester.View.Reset();
        }
    }

    // Flutter: 'date_picker_test.dart: Input mode Dialog contents are visible - textScaler 0.88, 1.0, 2.0'
    // This is a regression test for https://github.com/flutter/flutter/issues/139120.
    [Fact(Skip = GoldenSkip)]
    public void InputModeDialogContentsAreVisibleTextScaler()
    {
    }

    // ---- group('Semantics') ----

    // Flutter: 'date_picker_test.dart: Semantics calendar mode'
    [Fact]
    public void SemanticsCalendarMode()
    {
        using FrameworkDartTester tester = CreateTester();
        SemanticsHandle semantics = tester.EnsureSemantics();

        PrepareDatePicker(tester, _ =>
        {
            // Header
            ExpectSemantics(
                tester.GetSemantics(Find.Text("SELECT DATE")),
                MatchesSemantics(label: "SELECT DATE\nFri, Jan 15"));

            ExpectSemantics(
                tester.GetSemantics(Find.Text("3")),
                MatchesSemantics(
                    label: "3, Sunday, January 3, 2016, Today",
                    isButton: true,
                    hasEnabledState: true,
                    hasTapAction: true,
                    hasSelectedState: true,
                    hasFocusAction: true,
                    isFocusable: true,
                    isEnabled: true));

            // Input mode toggle button
            ExpectSemantics(
                tester.GetSemantics(SwitchToInputIcon),
                MatchesSemantics(
                    tooltip: "Switch to input",
                    isButton: true,
                    hasTapAction: true,
                    hasFocusAction: true,
                    isEnabled: true,
                    hasEnabledState: true,
                    isFocusable: true));

            // The semantics of the CalendarDatePicker are tested in its tests.

            // Ok/Cancel buttons
            ExpectSemantics(
                tester.GetSemantics(Find.Text("OK")),
                MatchesSemantics(
                    label: "OK",
                    isButton: true,
                    hasTapAction: true,
                    hasFocusAction: true,
                    isEnabled: true,
                    hasEnabledState: true,
                    isFocusable: true));
            ExpectSemantics(
                tester.GetSemantics(Find.Text("CANCEL")),
                MatchesSemantics(
                    label: "CANCEL",
                    isButton: true,
                    hasTapAction: true,
                    hasFocusAction: true,
                    isEnabled: true,
                    hasEnabledState: true,
                    isFocusable: true));
        });
        semantics.Dispose();
    }

    // Flutter: 'date_picker_test.dart: Semantics Calendar mode respects tap target guidelines in portrait
    // orientation'
    // Regression test for https://github.com/flutter/flutter/issues/158325.
    [Fact]
    public void SemanticsCalendarModeRespectsTapTargetGuidelinesInPortraitOrientation()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.View.PhysicalSize = new Size(400, 800);
        tester.View.DevicePixelRatio = 1.0;
        try
        {
            PrepareDatePicker(
                tester,
                _ =>
                {
                    Finds.OneWidget(Find.ByType<DatePickerDialog>());
                    AccessibilityGuideline.ExpectMeetsGuideline(
                        tester,
                        AccessibilityGuideline.AndroidTapTargetGuideline);
                },
                useMaterial3: true);
        }
        finally
        {
            tester.View.Reset();
        }
    }

    // Flutter: 'date_picker_test.dart: Semantics input mode'
    [Fact]
    public void SemanticsInputMode()
    {
        using FrameworkDartTester tester = CreateTester();
        // Fill the clipboard so that the Paste option is available in the text
        // selection menu.
        using var mockClipboard = new MockClipboardPlatform();
        Clipboard.SetData(new ClipboardData("Clipboard data"));

        SemanticsHandle semantics = tester.EnsureSemantics();

        _initialEntryMode = DatePickerEntryMode.Input;
        PrepareDatePicker(tester, _ =>
        {
            // Header
            ExpectSemantics(
                tester.GetSemantics(Find.Text("SELECT DATE")),
                MatchesSemantics(label: "SELECT DATE\nFri, Jan 15"));

            // Input mode toggle button
            ExpectSemantics(
                tester.GetSemantics(SwitchToCalendarIcon),
                MatchesSemantics(
                    tooltip: "Switch to calendar",
                    isButton: true,
                    hasTapAction: true,
                    hasFocusAction: true,
                    isEnabled: true,
                    hasEnabledState: true,
                    isFocusable: true));

            ExpectSemantics(
                tester.GetSemantics(Find.ByType<EditableText>()),
                MatchesSemantics(
                    label: "Enter Date",
                    isEnabled: true,
                    hasEnabledState: true,
                    isTextField: true,
                    isFocusable: true,
                    isFocused: true,
                    value: "01/15/2016",
                    hasTapAction: true,
                    hasFocusAction: true,
                    hasSetTextAction: true,
                    hasSetSelectionAction: true,
                    hasCopyAction: true,
                    hasCutAction: true,
                    hasPasteAction: true,
                    hasMoveCursorBackwardByCharacterAction: true,
                    hasMoveCursorBackwardByWordAction: true,
                    validationResult: SemanticsValidationResult.Valid));

            // Ok/Cancel buttons
            ExpectSemantics(
                tester.GetSemantics(Find.Text("OK")),
                MatchesSemantics(
                    label: "OK",
                    isButton: true,
                    hasTapAction: true,
                    hasFocusAction: true,
                    isEnabled: true,
                    hasEnabledState: true,
                    isFocusable: true));
            ExpectSemantics(
                tester.GetSemantics(Find.Text("CANCEL")),
                MatchesSemantics(
                    label: "CANCEL",
                    isButton: true,
                    hasTapAction: true,
                    hasFocusAction: true,
                    isEnabled: true,
                    hasEnabledState: true,
                    isFocusable: true));
        });
        semantics.Dispose();
    }

    // Flutter: 'date_picker_test.dart: Semantics datepicker dialog semantics node not focusable'
    // Regression test for https://github.com/flutter/flutter/pull/152705
    [Fact]
    public void SemanticsDatepickerDialogSemanticsNodeNotFocusable()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(
                child: new DatePickerDialog(
                    initialDate: _initialDate,
                    firstDate: _firstDate,
                    lastDate: _lastDate))));

        SemanticsNode node = tester.Semantics.Find(Find.ByType<DatePickerDialog>());
        SemanticsData semanticsData = node.GetSemanticsData();
        // Dart's `flagsCollection.isFocused == Tristate.none`: neither focusable nor focused.
        Assert.False(semanticsData.HasFlag(SemanticsFlags.IsFocusable));
        Assert.False(semanticsData.HasFlag(SemanticsFlags.IsFocused));
    }

    // ---- group('Keyboard navigation') ----

    // Flutter: 'date_picker_test.dart: Keyboard navigation Can toggle to calendar entry mode'
    [Fact]
    public void KeyboardNavigationCanToggleToCalendarEntryMode()
    {
        using FrameworkDartTester tester = CreateTester();
        PrepareDatePicker(tester, _ =>
        {
            Finds.Nothing(Find.ByType<TextField>());
            // Navigate to the entry toggle button and activate it
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);
            tester.SendKeyEvent(LogicalKeyboardKey.Space);
            tester.PumpAndSettle();
            // Should be in the input mode
            Finds.OneWidget(Find.ByType<TextField>());
        });
    }

    // Flutter: 'date_picker_test.dart: Keyboard navigation Can toggle to year mode'
    [Fact]
    public void KeyboardNavigationCanToggleToYearMode()
    {
        using FrameworkDartTester tester = CreateTester();
        PrepareDatePicker(tester, _ =>
        {
            Finds.Nothing(Find.Text("2016"));
            // Navigate to the year selector and activate it
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);
            tester.SendKeyEvent(LogicalKeyboardKey.Space);
            tester.PumpAndSettle();
            // The years should be visible
            Finds.OneWidget(Find.Text("2016"));
        });
    }

    // Flutter: 'date_picker_test.dart: Keyboard navigation Can navigate next/previous months'
    [Fact]
    public void KeyboardNavigationCanNavigateNextPreviousMonths()
    {
        using FrameworkDartTester tester = CreateTester();
        PrepareDatePicker(tester, _ =>
        {
            Finds.OneWidget(Find.Text("January 2016"));
            // Navigate to the previous month button and activate it twice
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);
            tester.SendKeyEvent(LogicalKeyboardKey.Space);
            tester.PumpAndSettle();
            tester.SendKeyEvent(LogicalKeyboardKey.Space);
            tester.PumpAndSettle();
            // Should be showing Nov 2015
            Finds.OneWidget(Find.Text("November 2015"));

            // Navigate to the next month button and activate it four times
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);
            tester.SendKeyEvent(LogicalKeyboardKey.Space);
            tester.PumpAndSettle();
            tester.SendKeyEvent(LogicalKeyboardKey.Space);
            tester.PumpAndSettle();
            tester.SendKeyEvent(LogicalKeyboardKey.Space);
            tester.PumpAndSettle();
            tester.SendKeyEvent(LogicalKeyboardKey.Space);
            tester.PumpAndSettle();
            // Should be on Mar 2016
            Finds.OneWidget(Find.Text("March 2016"));
        });
    }

    // Flutter: 'date_picker_test.dart: Keyboard navigation Can navigate date grid with arrow keys'
    [Fact]
    public void KeyboardNavigationCanNavigateDateGridWithArrowKeys()
    {
        using FrameworkDartTester tester = CreateTester();
        PrepareDatePicker(tester, date =>
        {
            // Navigate to the grid
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);

            // Navigate from Jan 15 to Jan 18 with arrow keys
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowLeft);
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowUp);
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowLeft);
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowDown);
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowDown);
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowLeft);
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowLeft);
            tester.PumpAndSettle();

            // Activate it
            tester.SendKeyEvent(LogicalKeyboardKey.Space);
            tester.PumpAndSettle();

            // Navigate out of the grid and to the OK button
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);

            // Activate OK
            tester.SendKeyEvent(LogicalKeyboardKey.Space);
            tester.PumpAndSettle();

            // Should have selected Jan 18
            Assert.Equal(new DateTime(2016, 1, 18), Await(date));
        });
    }

    // Flutter: 'date_picker_test.dart: Keyboard navigation Navigating with arrow keys scrolls months'
    [Fact]
    public void KeyboardNavigationNavigatingWithArrowKeysScrollsMonths()
    {
        using FrameworkDartTester tester = CreateTester();
        PrepareDatePicker(tester, date =>
        {
            // Navigate to the grid
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

            // Should have scrolled to Dec 2015
            Finds.OneWidget(Find.Text("December 2015"));

            // Navigate from Dec 31 to Nov 26 with arrow keys
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowUp);
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowUp);
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowUp);
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowUp);
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowUp);
            tester.PumpAndSettle();

            // Should have scrolled to Nov 2015
            Finds.OneWidget(Find.Text("November 2015"));

            // Activate it
            tester.SendKeyEvent(LogicalKeyboardKey.Space);
            tester.PumpAndSettle();

            // Navigate out of the grid and to the OK button
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);
            tester.PumpAndSettle();

            // Activate OK
            tester.SendKeyEvent(LogicalKeyboardKey.Space);
            tester.PumpAndSettle();

            // Should have selected Jan 18
            Assert.Equal(new DateTime(2015, 11, 26), Await(date));
        });
    }

    // Flutter: 'date_picker_test.dart: Keyboard navigation RTL text direction reverses the horizontal arrow key
    // navigation'
    [Fact]
    public void KeyboardNavigationRtlTextDirectionReversesTheHorizontalArrowKeyNavigation()
    {
        using FrameworkDartTester tester = CreateTester();
        PrepareDatePicker(tester, date =>
        {
            // Navigate to the grid
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);
            tester.PumpAndSettle();

            // Navigate from Jan 15 to 19 with arrow keys
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowRight);
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowRight);
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowRight);
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowRight);
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowDown);
            tester.SendKeyEvent(LogicalKeyboardKey.ArrowLeft);
            tester.PumpAndSettle();

            // Activate it
            tester.SendKeyEvent(LogicalKeyboardKey.Space);
            tester.PumpAndSettle();

            // Navigate out of the grid and to the OK button
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);
            tester.PumpAndSettle();

            // Activate OK
            tester.SendKeyEvent(LogicalKeyboardKey.Space);
            tester.PumpAndSettle();

            // Should have selected Jan 18
            Assert.Equal(new DateTime(2016, 1, 19), Await(date));
        }, textDirection: TextDirection.Rtl);
    }

    // ---- group('Screen configurations') ----
    // Test various combinations of screen sizes, orientations and text scales
    // to ensure the layout doesn't overflow and cause an exception to be thrown.

    // Regression tests for https://github.com/flutter/flutter/issues/21383
    // Regression tests for https://github.com/flutter/flutter/issues/19744
    // Regression tests for https://github.com/flutter/flutter/issues/17745

    // Common screen size roughly based on a Pixel 1
    private static readonly Size KCommonScreenSizePortrait = new(1070, 1770);
    private static readonly Size KCommonScreenSizeLandscape = new(1770, 1070);

    // Small screen size based on a LG K130
    private static readonly Size KSmallScreenSizePortrait = new(320, 521);
    private static readonly Size KSmallScreenSizeLandscape = new(521, 320);

    // Dart's `showPicker(tester, size, [textScaleFactor])`; like Dart, it ignores the text scale factor.
    private void ShowScreenConfigurationPicker(FrameworkDartTester tester, Size size)
    {
        tester.View.PhysicalSize = size;
        tester.View.DevicePixelRatio = 1.0;

        PrepareDatePicker(tester, _ => tester.Tap(Find.Text("OK")));
        tester.PumpAndSettle();
    }

    private void ExpectScreenConfigurationDoesNotThrow(Size size)
    {
        using FrameworkDartTester tester = CreateTester();
        try
        {
            ShowScreenConfigurationPicker(tester, size);
            Assert.Null(tester.TakeException());
        }
        finally
        {
            tester.View.Reset();
        }
    }

    // Flutter: 'date_picker_test.dart: Screen configurations common screen size - portrait'
    [Fact]
    public void ScreenConfigurationsCommonScreenSizePortrait() =>
        ExpectScreenConfigurationDoesNotThrow(KCommonScreenSizePortrait);

    // Flutter: 'date_picker_test.dart: Screen configurations common screen size - landscape'
    [Fact]
    public void ScreenConfigurationsCommonScreenSizeLandscape() =>
        ExpectScreenConfigurationDoesNotThrow(KCommonScreenSizeLandscape);

    // Flutter: 'date_picker_test.dart: Screen configurations common screen size - portrait - textScale 1.3'
    [Fact]
    public void ScreenConfigurationsCommonScreenSizePortraitTextScale13() =>
        ExpectScreenConfigurationDoesNotThrow(KCommonScreenSizePortrait);

    // Flutter: 'date_picker_test.dart: Screen configurations common screen size - landscape - textScale 1.3'
    [Fact]
    public void ScreenConfigurationsCommonScreenSizeLandscapeTextScale13() =>
        ExpectScreenConfigurationDoesNotThrow(KCommonScreenSizeLandscape);

    // Flutter: 'date_picker_test.dart: Screen configurations small screen size - portrait'
    [Fact]
    public void ScreenConfigurationsSmallScreenSizePortrait() =>
        ExpectScreenConfigurationDoesNotThrow(KSmallScreenSizePortrait);

    // Flutter: 'date_picker_test.dart: Screen configurations small screen size - landscape'
    [Fact]
    public void ScreenConfigurationsSmallScreenSizeLandscape() =>
        ExpectScreenConfigurationDoesNotThrow(KSmallScreenSizeLandscape);

    // Flutter: 'date_picker_test.dart: Screen configurations small screen size - portrait -textScale 1.3'
    [Fact]
    public void ScreenConfigurationsSmallScreenSizePortraitTextScale13() =>
        ExpectScreenConfigurationDoesNotThrow(KSmallScreenSizePortrait);

    // Flutter: 'date_picker_test.dart: Screen configurations small screen size - landscape - textScale 1.3'
    [Fact]
    public void ScreenConfigurationsSmallScreenSizeLandscapeTextScale13() =>
        ExpectScreenConfigurationDoesNotThrow(KSmallScreenSizeLandscape);

    // ---- group('showDatePicker avoids overlapping display features') ----

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

    // Flutter: 'date_picker_test.dart: showDatePicker avoids overlapping display features positioning with
    // anchorPoint'
    [Fact]
    public void ShowDatePickerAvoidsOverlappingDisplayFeaturesPositioningWithAnchorPoint()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(HingeApp(rtl: false));

        BuildContext context = tester.Element(Find.Text("Test"));
        _ = MaterialDatePickers.ShowDatePicker(
            context: context,
            initialDate: DateTime.Now,
            firstDate: new DateTime(2018, 1, 1),
            lastDate: new DateTime(2030, 1, 1),
            anchorPoint: new Point(1000, 0));
        tester.PumpAndSettle();

        // Should take the right side of the screen
        Assert.Equal(new Point(410.0, 0.0), tester.GetTopLeft(Find.ByType<DatePickerDialog>()));
        Assert.Equal(new Point(800.0, 600.0), tester.GetBottomRight(Find.ByType<DatePickerDialog>()));
    }

    // Flutter: 'date_picker_test.dart: showDatePicker avoids overlapping display features positioning with
    // Directionality'
    [Fact]
    public void ShowDatePickerAvoidsOverlappingDisplayFeaturesPositioningWithDirectionality()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(HingeApp(rtl: true));

        BuildContext context = tester.Element(Find.Text("Test"));
        _ = MaterialDatePickers.ShowDatePicker(
            context: context,
            initialDate: DateTime.Now,
            firstDate: new DateTime(2018, 1, 1),
            lastDate: new DateTime(2030, 1, 1));
        tester.PumpAndSettle();

        // By default it should place the dialog on the right screen
        Assert.Equal(new Point(410.0, 0.0), tester.GetTopLeft(Find.ByType<DatePickerDialog>()));
        Assert.Equal(new Point(800.0, 600.0), tester.GetBottomRight(Find.ByType<DatePickerDialog>()));
    }

    // Flutter: 'date_picker_test.dart: showDatePicker avoids overlapping display features positioning with
    // defaults'
    [Fact]
    public void ShowDatePickerAvoidsOverlappingDisplayFeaturesPositioningWithDefaults()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(HingeApp(rtl: false));

        BuildContext context = tester.Element(Find.Text("Test"));
        _ = MaterialDatePickers.ShowDatePicker(
            context: context,
            initialDate: DateTime.Now,
            firstDate: new DateTime(2018, 1, 1),
            lastDate: new DateTime(2030, 1, 1));
        tester.PumpAndSettle();

        // By default it should place the dialog on the left screen
        Assert.Equal(new Point(0, 0), tester.GetTopLeft(Find.ByType<DatePickerDialog>()));
        Assert.Equal(new Point(390.0, 600.0), tester.GetBottomRight(Find.ByType<DatePickerDialog>()));
    }

    // Flutter: 'date_picker_test.dart: DatePickerDialog is state restorable'
    [Fact]
    public void DatePickerDialogIsStateRestorable()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(new MaterialApp(
            restorationScopeId: "app",
            home: new RestorableDatePickerDialogTestWidget()));

        // The date picker should be closed.
        Finds.Nothing(Find.ByType<DatePickerDialog>());
        Finds.OneWidget(Find.Text("25/7/2021"));

        // Open the date picker.
        tester.Tap(Find.Text("X"));
        tester.PumpAndSettle();
        Finds.OneWidget(Find.ByType<DatePickerDialog>());

        TestRestorationData restorationData = tester.GetRestorationData();
        tester.RestartAndRestore();

        // The date picker should be open after restoring.
        Finds.OneWidget(Find.ByType<DatePickerDialog>());

        // Tap on the barrier.
        tester.TapAt(new Point(10.0, 10.0));
        tester.PumpAndSettle();

        // The date picker should be closed, the text value updated to the
        // newly selected date.
        Finds.Nothing(Find.ByType<DatePickerDialog>());
        Finds.OneWidget(Find.Text("25/7/2021"));

        // The date picker should be open after restoring.
        tester.RestoreFrom(restorationData);
        Finds.OneWidget(Find.ByType<DatePickerDialog>());

        // Select a different date.
        tester.Tap(Find.Text("30"));
        tester.PumpAndSettle();

        // Restart after the new selection. It should remain selected.
        tester.RestartAndRestore();

        // Close the date picker.
        tester.Tap(Find.Text("OK"));
        tester.PumpAndSettle();

        // The date picker should be closed, the text value updated to the
        // newly selected date.
        Finds.Nothing(Find.ByType<DatePickerDialog>());
        Finds.OneWidget(Find.Text("30/7/2021"));
    }

    // Flutter: 'date_picker_test.dart: DatePickerDialog state restoration - DatePickerEntryMode'
    [Fact]
    public void DatePickerDialogStateRestorationDatePickerEntryMode()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(new MaterialApp(
            restorationScopeId: "app",
            home: new RestorableDatePickerDialogTestWidget(
                datePickerEntryMode: DatePickerEntryMode.CalendarOnly)));

        // The date picker should be closed.
        Finds.Nothing(Find.ByType<DatePickerDialog>());
        Finds.OneWidget(Find.Text("25/7/2021"));

        // Open the date picker.
        tester.Tap(Find.Text("X"));
        tester.PumpAndSettle();
        Finds.OneWidget(Find.ByType<DatePickerDialog>());

        // Only in calendar mode and cannot switch out.
        Finds.Nothing(Find.ByType<TextField>());
        Finds.Nothing(Find.ByIcon(Icons.Edit));

        TestRestorationData restorationData = tester.GetRestorationData();
        tester.RestartAndRestore();

        // The date picker should be open after restoring.
        Finds.OneWidget(Find.ByType<DatePickerDialog>());
        // Only in calendar mode and cannot switch out.
        Finds.Nothing(Find.ByType<TextField>());
        Finds.Nothing(Find.ByIcon(Icons.Edit));

        // Tap on the barrier.
        tester.TapAt(new Point(10.0, 10.0));
        tester.PumpAndSettle();

        // The date picker should be closed, the text value should be the same
        // as before.
        Finds.Nothing(Find.ByType<DatePickerDialog>());
        Finds.OneWidget(Find.Text("25/7/2021"));

        // The date picker should be open after restoring.
        tester.RestoreFrom(restorationData);
        Finds.OneWidget(Find.ByType<DatePickerDialog>());
        // Only in calendar mode and cannot switch out.
        Finds.Nothing(Find.ByType<TextField>());
        Finds.Nothing(Find.ByIcon(Icons.Edit));
    }

    // Flutter: 'date_picker_test.dart: Test Callback on Toggle of DatePicker Mode'
    [Fact]
    public void TestCallbackOnToggleOfDatePickerMode()
    {
        using FrameworkDartTester tester = CreateTester();
        PrepareDatePicker(tester, _ =>
        {
            tester.Tap(Find.ByIcon(Icons.Edit));
            Assert.Equal(DatePickerEntryMode.Input, _currentMode);
            tester.PumpAndSettle();
            Finds.OneWidget(Find.ByType<TextField>());
            tester.Tap(Find.ByIcon(Icons.CalendarToday));
            Assert.Equal(DatePickerEntryMode.Calendar, _currentMode);
            tester.PumpAndSettle();
            Finds.Nothing(Find.ByType<TextField>());
        });
    }

    // Flutter: 'date_picker_test.dart: DatePickerDialog with updated insetPadding'
    [Fact]
    public void DatePickerDialogWithUpdatedInsetPadding()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(
                child: new DatePickerDialog(
                    initialDate: _initialDate,
                    firstDate: _firstDate,
                    lastDate: _lastDate,
                    insetPadding: new Thickness(10.0, 20.0, 30.0, 40.0)))));

        Dialog dialog = tester.Widget<Dialog>(Find.ByType<Dialog>());
        Assert.Equal(new Thickness(10.0, 20.0, 30.0, 40.0), dialog.InsetPadding);
    }

    // ---- group('Landscape input-only date picker headers use headlineSmall') ----
    // Regression test for https://github.com/flutter/flutter/issues/122056

    private void ShowHeadlinePicker(FrameworkDartTester tester, Size size)
    {
        tester.View.PhysicalSize = size;
        tester.View.DevicePixelRatio = 1.0;
        _initialEntryMode = DatePickerEntryMode.Input;
        PrepareDatePicker(tester, _ => { }, useMaterial3: true);
    }

    // Flutter: 'date_picker_test.dart: Landscape input-only date picker headers use headlineSmall portrait'
    [Fact]
    public void LandscapeInputOnlyDatePickerHeadersUseHeadlineSmallPortrait()
    {
        using FrameworkDartTester tester = CreateTester();
        try
        {
            ShowHeadlinePicker(tester, KCommonScreenSizePortrait);
            Assert.Equal(32, tester.Widget<Text>(Find.Text("Fri, Jan 15")).Style?.FontSize);
            tester.Tap(Find.Text("Cancel"));
            tester.PumpAndSettle();
        }
        finally
        {
            tester.View.Reset();
        }
    }

    // Flutter: 'date_picker_test.dart: Landscape input-only date picker headers use headlineSmall landscape'
    [Fact]
    public void LandscapeInputOnlyDatePickerHeadersUseHeadlineSmallLandscape()
    {
        using FrameworkDartTester tester = CreateTester();
        try
        {
            ShowHeadlinePicker(tester, KCommonScreenSizeLandscape);
            Assert.Equal(24, tester.Widget<Text>(Find.Text("Fri, Jan 15")).Style?.FontSize);
            tester.Tap(Find.Text("Cancel"));
            tester.PumpAndSettle();
        }
        finally
        {
            tester.View.Reset();
        }
    }

    // ---- group('Material 2') ----
    // These tests are only relevant for Material 2. Once Material 2
    // support is deprecated and the APIs are removed, these tests
    // can be deleted.

    // Flutter: 'date_picker_test.dart: Material 2 showDatePicker Dialog Default dialog size'
    [Fact]
    public void Material2ShowDatePickerDialogDefaultDialogSize()
    {
        using FrameworkDartTester tester = CreateTester();

        void ShowPicker(Size size)
        {
            tester.View.PhysicalSize = size;
            tester.View.DevicePixelRatio = 1.0;
            PrepareDatePicker(tester, _ => { });
        }

        var wideWindowSize = new Size(1920.0, 1080.0);
        var narrowWindowSize = new Size(1070.0, 1770.0);
        var calendarLandscapeDialogSize = new Size(496.0, 346.0);
        var calendarPortraitDialogSizeM2 = new Size(330.0, 518.0);

        try
        {
            // Test landscape layout.
            ShowPicker(wideWindowSize);

            Size dialogContainerSize = tester.GetSize(Find.ByType<AnimatedContainer>());
            Assert.Equal(calendarLandscapeDialogSize, dialogContainerSize);

            // Close the dialog.
            tester.Tap(Find.Text("OK"));
            tester.PumpAndSettle();

            // Test portrait layout.
            ShowPicker(narrowWindowSize);

            dialogContainerSize = tester.GetSize(Find.ByType<AnimatedContainer>());
            Assert.Equal(calendarPortraitDialogSizeM2, dialogContainerSize);
        }
        finally
        {
            tester.View.Reset();
        }
    }

    // Flutter: 'date_picker_test.dart: Material 2 showDatePicker Dialog Default dialog properties'
    [Fact]
    public void Material2ShowDatePickerDialogDefaultDialogProperties()
    {
        using FrameworkDartTester tester = CreateTester();
        var theme = new ThemeData(useMaterial3: false);
        PrepareDatePicker(tester, _ =>
        {
            MaterialWidget dialogMaterial = DialogMaterial(tester);

            Assert.Equal(theme.ColorScheme.Surface, dialogMaterial.Color);
            Assert.Equal(theme.ShadowColor, dialogMaterial.ShadowColor);
            Assert.Equal(24.0, dialogMaterial.Elevation);
            Assert.Equal(
                new RoundedRectangleBorder(borderRadius: BorderRadius.All(Radius.Circular(4.0))),
                dialogMaterial.Shape);
            Assert.Equal(Clip.AntiAlias, dialogMaterial.ClipBehavior);

            Dialog dialog = tester.Widget<Dialog>(Find.ByType<Dialog>());
            Assert.Equal(new Thickness(16.0, 24.0), dialog.InsetPadding);
        }, useMaterial3: theme.UseMaterial3);
    }

    // Flutter: 'date_picker_test.dart: Material 2 Input mode Default InputDecoration'
    [Fact]
    public void Material2InputModeDefaultInputDecoration()
    {
        using FrameworkDartTester tester = CreateTester();
        InputModeSetUp();
        PrepareDatePicker(tester, _ =>
        {
            InputDecoration decoration = tester.Widget<TextField>(Find.ByType<TextField>()).Decoration!;
            Assert.Equal(new UnderlineInputBorder(), decoration.Border);
            Assert.False(decoration.Filled);
            Assert.Equal("mm/dd/yyyy", decoration.HintText);
            Assert.Equal("Enter Date", decoration.LabelText);
            Assert.Null(decoration.ErrorText);
        });
    }

    // Flutter: 'date_picker_test.dart: Material 2 Date picker dayOverlayColor resolves hovered state'
    [Fact]
    public void Material2DatePickerDayOverlayColorResolvesHoveredState()
    {
        using FrameworkDartTester tester = CreateTester();
        var theme = new ThemeData(useMaterial3: false);
        PrepareDatePicker(tester, _ => { }, theme: theme);

        Point center = tester.GetCenter(Find.Text("30"));
        TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        gesture.AddPointer();
        gesture.MoveTo(center);
        tester.PumpAndSettle();

        ExpectDayOverlayPaints(
            tester,
            PaintPattern.Paints
                .Circle() // Today decoration.
                .Circle() // Selected day decoration.
                .Circle(color: theme.ColorScheme.OnSurfaceVariant.WithOpacity(0.08)));
    }

    // Flutter: 'date_picker_test.dart: Material 2 Date picker dayOverlayColor resolves focused state'
    [Fact]
    public void Material2DatePickerDayOverlayColorResolvesFocusedState()
    {
        using FrameworkDartTester tester = CreateTester();
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
        var theme = new ThemeData(useMaterial3: false);
        PrepareDatePicker(tester, _ => { }, theme: theme);

        // Navigate to the grid.
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);

        // Navigate to day 30.
        tester.SendKeyEvent(LogicalKeyboardKey.ArrowDown);
        tester.SendKeyEvent(LogicalKeyboardKey.ArrowDown);
        tester.SendKeyEvent(LogicalKeyboardKey.ArrowRight);
        tester.PumpAndSettle();

        ExpectDayOverlayPaints(
            tester,
            PaintPattern.Paints
                .Circle() // Today decoration.
                .Circle() // Selected day decoration.
                .Circle(color: theme.ColorScheme.OnSurfaceVariant.WithOpacity(0.12)));
    }

    // Flutter: 'date_picker_test.dart: Material 2 Date picker dayOverlayColor resolves pressed state'
    [Fact]
    public void Material2DatePickerDayOverlayColorResolvesPressedState()
    {
        using FrameworkDartTester tester = CreateTester();
        var theme = new ThemeData(useMaterial3: false);
        PrepareDatePicker(tester, _ => { }, theme: theme);

        Point center = tester.GetCenter(Find.Text("30"));
        TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        gesture.AddPointer();
        try
        {
            gesture.Down(center);
            tester.PumpAndSettle();

            ExpectDayOverlayPaints(
                tester,
                PaintPattern.Paints
                    .Circle() // Today decoration.
                    .Circle() // Selected day decoration.
                    .Circle() // Hovered decoration.
                    .Circle(color: theme.ColorScheme.OnSurfaceVariant.WithOpacity(0.12)));
            gesture.Up();
        }
        finally
        {
            gesture.RemovePointer();
        }
    }

    // Flutter: 'date_picker_test.dart: Material 2 Date picker dayOverlayColor resolves selected and hovered state'
    // Regression test for https://github.com/flutter/flutter/issues/130586.
    [Fact]
    public void Material2DatePickerDayOverlayColorResolvesSelectedAndHoveredState()
    {
        using FrameworkDartTester tester = CreateTester();
        var theme = new ThemeData(useMaterial3: false);
        PrepareDatePicker(tester, _ => { }, theme: theme);

        // Select day 30.
        tester.Tap(Find.Text("30"));
        tester.PumpAndSettle();
        ShapeDecoration day30Decoration = FindDayDecoration(tester, "30")!;
        Assert.Equal(theme.ColorScheme.Primary, day30Decoration.Color);

        Point center = tester.GetCenter(Find.Text("30"));
        TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        gesture.AddPointer();
        try
        {
            gesture.MoveTo(center);
            tester.PumpAndSettle();

            ExpectDayOverlayPaints(
                tester,
                PaintPattern.Paints
                    .Circle() // Today decoration.
                    .Circle() // Selected day decoration.
                    .Circle(color: theme.ColorScheme.OnPrimary.WithOpacity(0.08)));
        }
        finally
        {
            gesture.RemovePointer();
        }
    }

    // Flutter: 'date_picker_test.dart: Material 2 Date picker dayOverlayColor resolves selected and focused state'
    [Fact]
    public void Material2DatePickerDayOverlayColorResolvesSelectedAndFocusedState()
    {
        using FrameworkDartTester tester = CreateTester();
        FocusManager.Instance.HighlightStrategy = FocusHighlightStrategy.AlwaysTraditional;
        var theme = new ThemeData(useMaterial3: false);
        PrepareDatePicker(tester, _ => { }, theme: theme);

        // Select day 30.
        tester.Tap(Find.Text("30"));
        tester.PumpAndSettle();
        ShapeDecoration day30Decoration = FindDayDecoration(tester, "30")!;
        Assert.Equal(theme.ColorScheme.Primary, day30Decoration.Color);

        // Navigate to the grid.
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.SendKeyEvent(LogicalKeyboardKey.Tab);
        tester.PumpAndSettle();

        // Day 30 is selected and focused.
        ExpectDayOverlayPaints(
            tester,
            PaintPattern.Paints
                .Circle() // Today decoration.
                .Circle() // Selected day decoration.
                .Circle(color: theme.ColorScheme.OnPrimary.WithOpacity(0.12)));
    }

    // Flutter: 'date_picker_test.dart: Material 2 Date picker dayOverlayColor resolves selected and pressed state'
    [Fact]
    public void Material2DatePickerDayOverlayColorResolvesSelectedAndPressedState()
    {
        using FrameworkDartTester tester = CreateTester();
        var theme = new ThemeData(useMaterial3: false);
        PrepareDatePicker(tester, _ => { }, theme: theme);

        // Select day 30.
        tester.Tap(Find.Text("30"));
        tester.PumpAndSettle();
        ShapeDecoration day30Decoration = FindDayDecoration(tester, "30")!;
        Assert.Equal(theme.ColorScheme.Primary, day30Decoration.Color);

        Point center = tester.GetCenter(Find.Text("30"));
        TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        gesture.AddPointer();
        try
        {
            gesture.Down(center);
            tester.PumpAndSettle();

            ExpectDayOverlayPaints(
                tester,
                PaintPattern.Paints
                    .Circle() // Today decoration.
                    .Circle() // Selected day decoration.
                    .Circle() // Hovered decoration.
                    .Circle(color: theme.ColorScheme.OnPrimary.WithOpacity(0.38)));
            gesture.Up();
        }
        finally
        {
            gesture.RemovePointer();
        }
    }

    // ---- group('Calendar Delegate') ----

    // Flutter: 'date_picker_test.dart: Calendar Delegate Defaults to Gregorian calendar system'
    [Fact]
    public void CalendarDelegateDefaultsToGregorianCalendarSystem()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(
                child: new DatePickerDialog(
                    initialDate: _initialDate,
                    firstDate: _firstDate,
                    lastDate: _lastDate))));

        DatePickerDialog dialog = tester.Widget<DatePickerDialog>(Find.ByType<DatePickerDialog>());
        Assert.IsAssignableFrom<GregorianCalendarDelegate>(dialog.CalendarDelegate);
    }

    // Flutter: 'date_picker_test.dart: Calendar Delegate Using custom calendar delegate implementation'
    [Fact]
    public void CalendarDelegateUsingCustomCalendarDelegateImplementation()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(
                child: new DatePickerDialog(
                    initialDate: _initialDate,
                    firstDate: _firstDate,
                    lastDate: _lastDate,
                    calendarDelegate: new TestCalendarDelegate()))));

        DatePickerDialog dialog = tester.Widget<DatePickerDialog>(Find.ByType<DatePickerDialog>());
        Assert.IsType<TestCalendarDelegate>(dialog.CalendarDelegate);
    }

    // Flutter: 'date_picker_test.dart: Calendar Delegate Displays calendar based on the calendar delegate'
    [Fact]
    public void CalendarDelegateDisplaysCalendarBasedOnTheCalendarDelegate()
    {
        using FrameworkDartTester tester = CreateTester();

        Text GetLastDayText()
        {
            Finder dayFinder = Find.Descendant(of: Find.ByType<Ink>(), matching: Find.ByType<Text>());
            return tester.Widget<Text>(dayFinder.Last);
        }

        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(
                child: new DatePickerDialog(
                    initialDate: _initialDate,
                    firstDate: _firstDate,
                    lastDate: _lastDate,
                    calendarDelegate: new TestCalendarDelegate()))));

        Finder nextMonthButton = Find.ByIcon(Icons.ChevronRight);

        Text lastDayText = GetLastDayText();
        Finds.OneWidget(Find.Text("January 2016"));
        Assert.Equal("28", lastDayText.Data);

        tester.Tap(nextMonthButton);
        tester.PumpAndSettle();

        lastDayText = GetLastDayText();
        Finds.OneWidget(Find.Text("February 2016"));
        Assert.Equal("21", lastDayText.Data);

        tester.Tap(nextMonthButton);
        tester.PumpAndSettle();

        lastDayText = GetLastDayText();
        Finds.OneWidget(Find.Text("March 2016"));
        Assert.Equal("28", lastDayText.Data);
    }

    // Flutter: 'date_picker_test.dart: DatePickerDialog renders at zero area'
    [Fact]
    public void DatePickerDialogRendersAtZeroArea()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(new MaterialApp(
            home: new Center(
                child: SizedBox.Shrink(
                    child: new DatePickerDialog(firstDate: _firstDate, lastDate: _lastDate)))));
        Size size = tester.GetSize(Find.ByType<DatePickerDialog>());
        // Dart's `Size.isEmpty`.
        Assert.True(size.Width <= 0.0 || size.Height <= 0.0);
    }

    // date_picker_test.dart: _RestorableDatePickerDialogTestWidget.
    private sealed class RestorableDatePickerDialogTestWidget(
        DatePickerEntryMode datePickerEntryMode = DatePickerEntryMode.Calendar) : StatefulWidget
    {
        public DatePickerEntryMode DatePickerEntryMode { get; } = datePickerEntryMode;

        public override State CreateState() => new RestorableDatePickerDialogTestWidgetState();
    }

    private sealed class RestorableDatePickerDialogTestWidgetState
        : RestorationState<RestorableDatePickerDialogTestWidget>
    {
        private readonly RestorableDateTime _selectedDate = new(new DateTime(2021, 7, 25));
        private RestorableRouteFuture<DateTime?>? _routeFuture;

        protected override string? RestorationId => "scaffold_state";

        private RestorableRouteFuture<DateTime?> RestorableDatePickerRouteFuture =>
            _routeFuture ??= new RestorableRouteFuture<DateTime?>(
                onComplete: SelectDate,
                onPresent: (navigator, _) => navigator.RestorablePush(
                    DatePickerRoute,
                    arguments: new Dictionary<string, object?>
                    {
                        ["selectedDate"] = new DateTimeOffset(_selectedDate.Value).ToUnixTimeMilliseconds(),
                        ["datePickerEntryMode"] = (int)Widget.DatePickerEntryMode,
                    }));

        public override void Dispose()
        {
            _selectedDate.Dispose();
            RestorableDatePickerRouteFuture.Dispose();
            base.Dispose();
        }

        protected override void RestoreState(RestorationBucket? oldBucket, bool initialRestore)
        {
            RegisterForRestoration(_selectedDate, "selected_date");
            RegisterForRestoration(RestorableDatePickerRouteFuture, "date_picker_route_future");
        }

        private void SelectDate(DateTime? newSelectedDate)
        {
            if (newSelectedDate != null)
            {
                SetState(() => _selectedDate.Value = newSelectedDate.Value);
            }
        }

        // Anonymous restorable routes must be built by a static method, Plumix's stand-in for Dart's
        // `@pragma('vm:entry-point')` static function.
        private static Route DatePickerRoute(BuildContext context, object? arguments)
        {
            return new DialogRoute<DateTime?>(
                context,
                _ =>
                {
                    var args = (IDictionary)arguments!;
                    return new DatePickerDialog(
                        restorationId: "date_picker_dialog",
                        initialEntryMode: Enum.GetValues<DatePickerEntryMode>()[
                            Convert.ToInt32(args["datePickerEntryMode"])],
                        initialDate: DateTimeOffset
                            .FromUnixTimeMilliseconds(Convert.ToInt64(args["selectedDate"]))
                            .LocalDateTime,
                        firstDate: new DateTime(2021, 1, 1),
                        lastDate: new DateTime(2022, 1, 1));
                });
        }

        public override Widget Build(BuildContext context)
        {
            DateTime selectedDateTime = _selectedDate.Value;
            // Example: "25/7/1994"
            string selectedDateTimeString =
                $"{selectedDateTime.Day}/{selectedDateTime.Month}/{selectedDateTime.Year}";
            return new Scaffold(
                body: new Center(
                    child: new Column(
                        children:
                        [
                            new OutlinedButton(
                                onPressed: () => RestorableDatePickerRouteFuture.Present(),
                                child: new Text("X")),
                            new Text(selectedDateTimeString),
                        ])));
        }
    }

    // date_picker_test.dart: _DatePickerObserver.
    private sealed class DatePickerObserver : NavigatorObserver
    {
        public int DatePickerCount { get; private set; }

        private static bool IsDialogRoute(Route route) =>
            route.GetType().IsGenericType && route.GetType().GetGenericTypeDefinition() == typeof(DialogRoute<>);

        public override void DidPush(Route route, Route? previousRoute)
        {
            if (IsDialogRoute(route))
            {
                DatePickerCount++;
            }

            base.DidPush(route, previousRoute);
        }

        public override void DidPop(Route route, Route? previousRoute)
        {
            if (IsDialogRoute(route))
            {
                DatePickerCount--;
            }

            base.DidPop(route, previousRoute);
        }
    }

    // date_picker_test.dart: TestCalendarDelegate.
    private sealed class TestCalendarDelegate : GregorianCalendarDelegate
    {
        public override int GetDaysInMonth(int year, int month) => month % 2 == 0 ? 21 : 28;

        public override int FirstDayOffset(int year, int month, MaterialLocalizations localizations) => 1;
    }
}
