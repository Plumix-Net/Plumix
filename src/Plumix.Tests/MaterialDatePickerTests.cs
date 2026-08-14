using Avalonia;
using Avalonia.Media;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialDatePickerTests : IDisposable
{
    public MaterialDatePickerTests()
    {
        Scheduler.ResetForTests();
        GestureBinding.Instance.ResetForTests();
        FocusManager.Instance.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
        GestureBinding.Instance.ResetForTests();
        FocusManager.Instance.ResetForTests();
    }

    [Fact]
    public void DateUtilsAndGregorianDelegateMatchFlutterCalendarMath()
    {
        var localizations = new MondayFirstLocalizations();
        var date = new DateTime(2024, 2, 29, 17, 42, 1);

        Assert.Equal(new DateTime(2024, 2, 29), DateUtils.DateOnly(date));
        Assert.True(DateUtils.IsSameDay(date, new DateTime(2024, 2, 29)));
        Assert.True(DateUtils.IsSameMonth(date, new DateTime(2024, 2, 1)));
        Assert.Equal(7, DateUtils.MonthDelta(new DateTime(2019, 6, 15), new DateTime(2020, 1, 1)));
        Assert.Equal(new DateTime(2019, 4, 1), DateUtils.AddMonthsToMonthDate(new DateTime(2019, 1, 15), 3));
        Assert.Equal(29, DateUtils.GetDaysInMonth(2024, 2));
        Assert.Equal(4, DateUtils.FirstDayOffset(2017, 9, localizations));
        Assert.Equal(TimeSpan.FromDays(2), new DateTimeRange<DateTime>(
            new DateTime(2024, 2, 27), new DateTime(2024, 2, 29)).Duration);

        var calendar = GregorianCalendarDelegate.Instance;
        Assert.Equal("February 2024", calendar.FormatMonthYear(date, DefaultMaterialLocalizations.Instance));
        Assert.Equal(new DateTime(2024, 2, 29), calendar.ParseCompactDate("02/29/2024", DefaultMaterialLocalizations.Instance));
        Assert.Null(calendar.ParseCompactDate("02/30/2024", DefaultMaterialLocalizations.Instance));
        Assert.Null(calendar.ParseCompactDate("2024-02-29", DefaultMaterialLocalizations.Instance));
    }

    [Fact]
    public void CalendarDatePickerNormalizesAndValidatesDateContract()
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

        Assert.Throws<ArgumentException>(() => new CalendarDatePicker(
            null, new DateTime(2026, 2, 1), new DateTime(2026, 1, 1), _ => { }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CalendarDatePicker(
            new DateTime(2025, 12, 31), new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), _ => { }));
        Assert.Throws<ArgumentException>(() => new CalendarDatePicker(
            new DateTime(2026, 1, 2), new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), _ => { },
            selectableDayPredicate: day => day.Day % 2 == 1));
    }

    [Fact]
    public void CalendarDatePickerM3GridUsesExpectedHeightAndDaySemantics()
    {
        using var harness = CreateHarness(new CalendarDatePicker(
            initialDate: new DateTime(2026, 3, 12),
            firstDate: new DateTime(2026, 1, 1),
            lastDate: new DateTime(2026, 12, 31),
            currentDate: new DateTime(2026, 3, 13),
            onDateChanged: _ => { }));

        var semantics = harness.PumpAndGetSemantics(new Size(420, 500));
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView), box =>
            Close(box.AdditionalConstraints.MinHeight, 388) && Close(box.AdditionalConstraints.MaxHeight, 388));
        var selected = FindSemantics(semantics, node =>
            node.Label?.StartsWith("12, Thursday, March 12, 2026", StringComparison.Ordinal) == true);
        var today = FindSemantics(semantics, node =>
            node.Label?.Contains("Friday, March 13, 2026, Today", StringComparison.Ordinal) == true);
        Assert.NotNull(selected);
        Assert.True(selected!.Flags.HasFlag(SemanticsFlags.IsSelected));
        Assert.NotNull(today);
    }

    [Fact]
    public void CalendarDatePickerSelectsEnabledDayAndPredicateDisablesDay()
    {
        DateTime? selected = null;
        using var harness = CreateHarness(new CalendarDatePicker(
            initialDate: new DateTime(2026, 3, 12),
            firstDate: new DateTime(2026, 3, 1),
            lastDate: new DateTime(2026, 3, 31),
            currentDate: new DateTime(2026, 3, 13),
            selectableDayPredicate: day => day.Day != 14,
            onDateChanged: value => selected = value));
        var semantics = harness.PumpAndGetSemantics(new Size(420, 500));
        var enabled = FindSemantics(semantics, node =>
            node.Label?.StartsWith("15, Sunday, March 15, 2026", StringComparison.Ordinal) == true);
        var disabled = FindSemantics(semantics, node =>
            node.Label?.StartsWith("14, Saturday, March 14, 2026", StringComparison.Ordinal) == true);

        Assert.NotNull(enabled);
        Assert.True(enabled!.PerformAction(SemanticsActions.Tap));
        Assert.Equal(new DateTime(2026, 3, 15), selected);
        Assert.NotNull(disabled);
        Assert.False(disabled!.Flags.HasFlag(SemanticsFlags.IsEnabled));
        Assert.False(disabled.Actions.HasFlag(SemanticsActions.Tap));
    }

    [Fact]
    public void CalendarDatePickerTogglesYearModeAndReturnsAfterYearSelection()
    {
        DateTime? selected = null;
        using var harness = CreateHarness(new CalendarDatePicker(
            initialDate: new DateTime(2026, 3, 31),
            firstDate: new DateTime(2024, 5, 1),
            lastDate: new DateTime(2028, 12, 31),
            currentDate: new DateTime(2026, 3, 13),
            onDateChanged: value => selected = value));
        var semantics = harness.PumpAndGetSemantics(new Size(420, 500));
        var toggle = FindSemantics(semantics, node =>
            HasLabelPart(node, "Select year") && node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(toggle);
        Assert.True(toggle!.PerformAction(SemanticsActions.Tap));

        semantics = harness.PumpAndGetSemantics(new Size(420, 500));
        var year = FindSemantics(semantics, node =>
            HasLabelPart(node, "2028") && node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(year);
        Assert.True(year!.PerformAction(SemanticsActions.Tap));
        harness.Pump(new Size(420, 500));

        Assert.Equal(new DateTime(2028, 3, 31), selected);
        Assert.NotNull(FindSemantics(harness.PumpAndGetSemantics(new Size(420, 500)), node =>
            node.Label?.StartsWith("31, Friday, March 31, 2028", StringComparison.Ordinal) == true));
    }

    [Fact]
    public void CalendarDatePickerNextMonthActionAnimatesAndReportsDisplayedMonth()
    {
        DateTime? displayed = null;
        using var harness = CreateHarness(new CalendarDatePicker(
            initialDate: new DateTime(2026, 3, 12),
            firstDate: new DateTime(2026, 1, 1),
            lastDate: new DateTime(2026, 12, 31),
            onDateChanged: _ => { },
            onDisplayedMonthChanged: value => displayed = value));
        var semantics = harness.PumpAndGetSemantics(new Size(420, 500));
        var next = FindSemantics(semantics, node =>
            node.Label?.Contains("Next month", StringComparison.Ordinal) == true
            && node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.True(next is not null, DumpSemantics(semantics));
        Assert.True(next!.PerformAction(SemanticsActions.Tap));

        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.25));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.35));
        harness.Pump(new Size(420, 500));

        Assert.Equal(new DateTime(2026, 4, 1), displayed);
    }

    [Fact]
    public void CalendarDatePickerKeyboardMovesFocusedDayAcrossMonthAndSelectsOnEnter()
    {
        DateTime? selected = null;
        DateTime? displayed = null;
        using var harness = CreateHarness(new CalendarDatePicker(
            initialDate: new DateTime(2026, 3, 31),
            firstDate: new DateTime(2026, 1, 1),
            lastDate: new DateTime(2026, 12, 31),
            onDateChanged: value => selected = value,
            onDisplayedMonthChanged: value => displayed = value));
        harness.Pump(new Size(420, 500));

        for (int index = 0;
             index < 64 && FocusManager.Instance.PrimaryFocus?.OnKeyEvent?.Method.Name != "HandleGridKey";
             index++)
        {
            Assert.True(FocusManager.Instance.FocusNext());
        }
        Assert.Equal("HandleGridKey", FocusManager.Instance.PrimaryFocus?.OnKeyEvent?.Method.Name);
        Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent("ArrowRight", isDown: true)));
        harness.Pump(new Size(420, 500));
        Assert.Equal(new DateTime(2026, 4, 1), displayed);

        Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent("Enter", isDown: true)));
        Assert.Equal(new DateTime(2026, 4, 1), selected);
    }

    [Fact]
    public void YearPickerPreservesMonthAndClampsLastBoundaryMonth()
    {
        DateTime? selected = null;
        using var harness = CreateHarness(new YearPicker(
            firstDate: new DateTime(2024, 5, 1),
            lastDate: new DateTime(2026, 3, 31),
            selectedDate: new DateTime(2025, 11, 1),
            currentDate: new DateTime(2025, 6, 1),
            onChanged: value => selected = value));
        var semantics = harness.PumpAndGetSemantics(new Size(420, 320));

        var first = FindSemantics(
            semantics,
            node => HasLabelPart(node, "2024") && node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(first);
        Assert.True(first!.PerformAction(SemanticsActions.Tap));
        Assert.Equal(new DateTime(2024, 11, 1), selected);

        var last = FindSemantics(
            semantics,
            node => HasLabelPart(node, "2026") && node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(last);
        Assert.True(last!.PerformAction(SemanticsActions.Tap));
        Assert.Equal(new DateTime(2026, 3, 1), selected);
    }

    [Fact]
    public void DatePickerThemeLocalOverridesDriveSelectedDayAndYear()
    {
        var localDay = Color.Parse("#FF445566");
        var localYear = Color.Parse("#FF778899");
        var localTheme = new DatePickerThemeData(
            DayBackgroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Selected) ? localDay : null),
            YearBackgroundColor: MaterialStateProperty<Color?>.ResolveWith(states =>
                states.HasFlag(MaterialState.Selected) ? localYear : null));

        using var dayHarness = CreateHarness(new DatePickerTheme(localTheme, new CalendarDatePicker(
            initialDate: new DateTime(2026, 3, 12),
            firstDate: new DateTime(2026, 1, 1),
            lastDate: new DateTime(2026, 12, 31),
            onDateChanged: _ => { })));
        dayHarness.Pump(new Size(420, 500));
        Assert.Contains(FindDescendants<RenderDecoratedBox>(dayHarness.RenderView), box =>
            box.Decoration.Color == localDay && box.Decoration.Shape == BoxShape.Circle);

        using var yearHarness = CreateHarness(new DatePickerTheme(localTheme, new YearPicker(
            firstDate: new DateTime(2024, 1, 1),
            lastDate: new DateTime(2028, 12, 31),
            selectedDate: new DateTime(2026, 1, 1),
            currentDate: new DateTime(2025, 1, 1),
            onChanged: _ => { })));
        yearHarness.Pump(new Size(420, 320));
        Assert.Contains(FindDescendants<RenderDecoratedBox>(yearHarness.RenderView), box =>
            box.Decoration.Color == localYear);
    }

    [Fact]
    public void DatePickerThemeDefaultsUseExactM2AndM3ColorSchemeRoles()
    {
        var primary = Color.Parse("#FF102030");
        var onPrimary = Color.Parse("#FF405060");
        var onPrimaryContainer = Color.Parse("#FF708090");
        var surface = Color.Parse("#FF90A0B0");
        var onSurface = Color.Parse("#FFB0C0D0");
        var onSurfaceVariant = Color.Parse("#FFD0E0F0");
        var secondaryContainer = Color.Parse("#FF314253");
        var surfaceContainerHigh = Color.Parse("#FF647586");
        var scheme = ColorScheme.Material3Light with
        {
            Primary = primary,
            OnPrimary = onPrimary,
            OnPrimaryContainer = onPrimaryContainer,
            Surface = surface,
            OnSurface = onSurface,
            OnSurfaceVariant = onSurfaceVariant,
            SecondaryContainer = secondaryContainer,
            SurfaceContainerHigh = surfaceContainerHigh,
        };
        var material3Theme = new ThemeData(colorScheme: scheme, useMaterial3: true) with
        {
            PrimaryColor = Colors.Crimson,
            OnPrimaryColor = Colors.Cyan,
            SurfaceColor = Colors.Gold,
            OnSurfaceColor = Colors.Green,
        };
        DatePickerThemeData? material3 = null;
        using (var harness = CreateHarness(
                   new CaptureDatePickerDefaults(value => material3 = value),
                   material3Theme))
        {
            harness.Pump(new Size(200, 100));
        }

        Assert.NotNull(material3);
        Assert.Equal(surfaceContainerHigh, material3!.BackgroundColor);
        Assert.Equal(6.0, material3.Elevation);
        Assert.Equal(Colors.Transparent, material3.ShadowColor);
        Assert.Equal(Colors.Transparent, material3.HeaderBackgroundColor);
        Assert.Equal(onSurfaceVariant, material3.HeaderForegroundColor);
        Assert.Equal(32.0, material3.HeaderHeadlineStyle!.FontSize);
        Assert.Equal(primary, material3.DayBackgroundColor!.Resolve(MaterialState.Selected));
        Assert.Equal(
            WithOpacity(onPrimary, 0.10),
            material3.DayOverlayColor!.Resolve(MaterialState.Selected | MaterialState.Pressed));
        Assert.Equal(onSurfaceVariant, material3.YearForegroundColor!.Resolve(MaterialState.None));
        Assert.Null(material3.RangePickerBackgroundColor);
        Assert.Equal(Colors.Transparent, material3.RangePickerHeaderBackgroundColor);
        Assert.Equal(onSurfaceVariant, material3.RangePickerHeaderForegroundColor);
        Assert.Equal(14.0, material3.RangePickerHeaderHelpStyle!.FontSize);
        Assert.Equal(secondaryContainer, material3.RangeSelectionBackgroundColor);
        Assert.Equal(
            WithOpacity(onPrimaryContainer, 0.10),
            material3.RangeSelectionOverlayColor!.Resolve(MaterialState.Selected | MaterialState.Pressed));
        Assert.Null(material3.DividerColor);
        Assert.NotNull(material3.CancelButtonStyle);
        Assert.NotNull(material3.ConfirmButtonStyle);

        var material2Theme = new ThemeData(colorScheme: scheme, useMaterial3: false) with
        {
            PrimaryColor = Colors.Crimson,
            OnPrimaryColor = Colors.Cyan,
            SurfaceColor = Colors.Gold,
        };
        DatePickerThemeData? material2 = null;
        using (var harness = CreateHarness(
                   new CaptureDatePickerDefaults(value => material2 = value),
                   material2Theme))
        {
            harness.Pump(new Size(200, 100));
        }

        Assert.NotNull(material2);
        Assert.Null(material2!.BackgroundColor);
        Assert.Equal(24.0, material2.Elevation);
        Assert.Null(material2.ShadowColor);
        Assert.Null(material2.SurfaceTintColor);
        Assert.Equal(primary, material2.HeaderBackgroundColor);
        Assert.Equal(onPrimary, material2.HeaderForegroundColor);
        Assert.Null(material2.YearForegroundColor);
        Assert.Null(material2.YearBackgroundColor);
        Assert.Null(material2.YearOverlayColor);
        Assert.Equal(surface, material2.RangePickerBackgroundColor);
        Assert.Equal(0.0, material2.RangePickerElevation);
        Assert.Equal(
            WithOpacity(onPrimary, 0.38),
            material2.DayOverlayColor!.Resolve(MaterialState.Selected | MaterialState.Pressed));
        Assert.Equal(material2.DayOverlayColor, material2.RangeSelectionOverlayColor);
        Assert.Null(material2.DividerColor);
    }

    [Fact]
    public void DatePickerThemeDataCopyLerpAndInheritedCaptureMatchFlutter()
    {
        var localeA = new Locale("en", countryCode: "GB");
        var localeB = new Locale("fr", countryCode: "FR");
        var dayShape = MaterialStateProperty<OutlinedBorder?>.All(new CircleBorder());
        var source = new DatePickerThemeData(
            BackgroundColor: Colors.Beige,
            DayShape: dayShape,
            Locale: localeA);
        var copy = source.CopyWith(headerBackgroundColor: Colors.Crimson);

        Assert.Equal(Colors.Beige, copy.BackgroundColor);
        Assert.Same(dayShape, copy.DayShape);
        Assert.Equal(localeA, copy.Locale);
        Assert.Equal(Colors.Crimson, copy.HeaderBackgroundColor);
        Assert.Same(source, DatePickerThemeData.Lerp(source, source, 0.25));
        Assert.Equal(localeA, DatePickerThemeData.Lerp(source, source with { Locale = localeB }, 0.25).Locale);
        Assert.Equal(localeB, DatePickerThemeData.Lerp(source, source with { Locale = localeB }, 0.75).Locale);

        DatePickerThemeData? captured = null;
        var localTheme = new DatePickerThemeData(BackgroundColor: Colors.CornflowerBlue);
        using var harness = CreateHarness(new DatePickerTheme(
            localTheme,
            new CaptureAndOverrideDatePickerTheme(value => captured = value)));
        harness.Pump(new Size(200, 100));

        Assert.Same(localTheme, captured);
    }

    [Fact]
    public void CalendarDatePickerM2UsesFortyTwoPixelRows()
    {
        using var harness = CreateHarness(BuildPicker(), ThemeData.Light with { UseMaterial3 = false });
        harness.Pump(new Size(420, 500));
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView), box =>
            Close(box.AdditionalConstraints.MinHeight, 346) && Close(box.AdditionalConstraints.MaxHeight, 346));
    }

    [Fact]
    public void YearPickerM2UsesTextThemeWhenYearStateColorsAreNull()
    {
        using var harness = CreateHarness(
            new YearPicker(
                firstDate: new DateTime(2024, 1, 1),
                lastDate: new DateTime(2028, 12, 31),
                selectedDate: new DateTime(2026, 1, 1),
                currentDate: new DateTime(2025, 1, 1),
                onChanged: _ => { }),
            ThemeData.Light with { UseMaterial3 = false });
        harness.Pump(new Size(420, 320));

        Assert.Contains(
            FindDescendants<RenderParagraph>(harness.RenderView),
            paragraph => paragraph.PlainText == "2026");
    }

    [Fact]
    public void InputDatePickerFormField_NormalizesDefaultsAndValidatesContracts()
    {
        var field = new InputDatePickerFormField(
            initialDate: new DateTime(2026, 3, 12, 14, 30, 0),
            firstDate: new DateTime(2026, 1, 1, 8, 0, 0),
            lastDate: new DateTime(2026, 12, 31, 22, 0, 0));
        Assert.Equal(new DateTime(2026, 3, 12), field.InitialDate);
        Assert.Equal(new DateTime(2026, 1, 1), field.FirstDate);
        Assert.Equal(new DateTime(2026, 12, 31), field.LastDate);
        Assert.False(field.Autofocus);
        Assert.False(field.AcceptEmptyDate);

        Assert.Throws<ArgumentException>(() => new InputDatePickerFormField(
            firstDate: new DateTime(2026, 2, 1),
            lastDate: new DateTime(2026, 1, 1)));
        Assert.Throws<ArgumentException>(() => new InputDatePickerFormField(
            initialDate: new DateTime(2026, 3, 14),
            firstDate: new DateTime(2026, 1, 1),
            lastDate: new DateTime(2026, 12, 31),
            selectableDayPredicate: date => date.DayOfWeek is not DayOfWeek.Saturday));
    }

    [Fact]
    public void InputDatePickerFormField_FormatsValidatesSavesAndSubmitsDates()
    {
        var formKey = new LabeledGlobalKey<FormState>("input-date-form");
        DateTime? saved = null;
        DateTime? submitted = null;
        using var harness = CreateHarness(new Form(
            key: formKey,
            child: new InputDatePickerFormField(
                initialDate: new DateTime(2026, 3, 12),
                firstDate: new DateTime(2026, 1, 1),
                lastDate: new DateTime(2026, 12, 31),
                selectableDayPredicate: date => date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday,
                errorFormatText: "Bad format",
                errorInvalidText: "Unavailable date",
                onDateSaved: value => saved = value,
                onDateSubmitted: value => submitted = value,
                autofocus: true)));
        harness.Pump(new Size(420, 180));
        var textState = Assert.IsType<TextFormFieldState>(Assert.Single(formKey.CurrentState!.Fields));
        Assert.Equal("03/12/2026", textState.EffectiveController.Text);
        Assert.Equal(new TextSelection(0, 10), textState.EffectiveController.Selection);

        textState.EffectiveController.Text = "not-a-date";
        Assert.False(formKey.CurrentState.Validate());
        harness.Pump(new Size(420, 180));
        Assert.Contains(
            FindDescendants<RenderParagraph>(harness.RenderView),
            paragraph => paragraph.PlainText == "Bad format");

        textState.EffectiveController.Text = "03/15/2026";
        Assert.False(formKey.CurrentState.Validate());
        Assert.Equal("Unavailable date", textState.ErrorText);

        textState.EffectiveController.Text = "03/16/2026";
        Assert.True(formKey.CurrentState.Validate());
        formKey.CurrentState.Save();
        Assert.Equal(new DateTime(2026, 3, 16), saved);
        Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent("Enter", isDown: true)));
        Assert.Equal(new DateTime(2026, 3, 16), submitted);
    }

    [Fact]
    public void InputDatePickerFormField_AcceptEmptyDateAllowsValidationWithoutCallbacks()
    {
        var formKey = new LabeledGlobalKey<FormState>("empty-date-form");
        int saves = 0;
        using var harness = CreateHarness(new Form(
            key: formKey,
            child: new InputDatePickerFormField(
                firstDate: new DateTime(2026, 1, 1),
                lastDate: new DateTime(2026, 12, 31),
                acceptEmptyDate: true,
                onDateSaved: _ => saves++)));
        harness.Pump(new Size(420, 160));
        Assert.True(formKey.CurrentState!.Validate());
        formKey.CurrentState.Save();
        Assert.Equal(0, saves);
    }

    [Fact]
    public void InputDatePickerFormField_DatePickerInputThemeOverridesAmbientBorder()
    {
        var theme = ThemeData.Light with
        {
            InputDecorationTheme = new InputDecorationThemeData(Border: new UnderlineInputBorder()),
            DatePickerTheme = new DatePickerThemeData(
                InputDecorationTheme: new InputDecorationThemeData(Border: new OutlineInputBorder())),
        };
        using var harness = CreateHarness(new InputDatePickerFormField(
            firstDate: new DateTime(2026, 1, 1),
            lastDate: new DateTime(2026, 12, 31)), theme);
        harness.Pump(new Size(420, 160));
        Assert.Contains(FindDescendants<RenderCustomPaint>(harness.RenderView), paint =>
            paint.Painter is InputBorderPainter { Border: OutlineInputBorder });
    }

    [Fact]
    public void DatePickerDialog_ExposesFlutterDefaultsSizesAndEntryModePolicy()
    {
        var dialog = new DatePickerDialog(
            initialDate: new DateTime(2026, 3, 12),
            firstDate: new DateTime(2026, 1, 1),
            lastDate: new DateTime(2026, 12, 31));
        Assert.Equal(DatePickerEntryMode.Calendar, dialog.InitialEntryMode);
        Assert.Equal(DatePickerMode.Day, dialog.InitialCalendarMode);
        Assert.Equal(new Thickness(16, 24), dialog.InsetPadding);
        Assert.Equal(new DateTime(2026, 3, 12), dialog.InitialDate);
        Assert.Throws<ArgumentOutOfRangeException>(() => new DatePickerDialog(
            firstDate: new DateTime(2026, 1, 1),
            lastDate: new DateTime(2026, 12, 31),
            insetPadding: new Thickness(-1)));

        using var harness = CreateHarness(new Navigator(new BuilderPageRoute(_ => dialog)));
        harness.Pump(new Size(500, 700));
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView), box =>
            Close(box.AdditionalConstraints.MinWidth, 360)
            && Close(box.AdditionalConstraints.MaxWidth, 360)
            && Close(box.AdditionalConstraints.MinHeight, 568)
            && Close(box.AdditionalConstraints.MaxHeight, 568));
    }

    [Fact]
    public void DatePickerDialog_TogglesCalendarAndInputModesWithSourceCallbacks()
    {
        var modes = new List<DatePickerEntryMode>();
        using var harness = CreateHarness(new Navigator(new BuilderPageRoute(_ => new DatePickerDialog(
            initialDate: new DateTime(2026, 3, 12),
            firstDate: new DateTime(2026, 1, 1),
            lastDate: new DateTime(2026, 12, 31),
            onDatePickerModeChange: modes.Add))));
        var semantics = harness.PumpAndGetSemantics(new Size(500, 700));
        var inputToggle = FindSemantics(semantics, node =>
            HasLabelPart(node, "Switch to input") && node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.True(inputToggle is not null, DumpSemantics(semantics));
        Assert.True(inputToggle!.PerformAction(SemanticsActions.Tap));

        semantics = harness.PumpAndGetSemantics(new Size(500, 700));
        Assert.Equal(DatePickerEntryMode.Input, Assert.Single(modes));
        Assert.NotNull(FindSemantics(semantics, node => HasLabelPart(node, "Switch to calendar")));
        Assert.Contains(
            FindDescendants<RenderParagraph>(harness.RenderView),
            paragraph => paragraph.PlainText == "Enter date");
    }

    [Fact]
    public void DatePickerDialog_OnlyModesHideToggleAndThemeHeaderSurfaceActions()
    {
        var pickerTheme = new DatePickerThemeData(
            BackgroundColor: Colors.Purple,
            HeaderBackgroundColor: Colors.Orange,
            CancelButtonStyle: TextButton.StyleFrom(foregroundColor: Colors.Green),
            ConfirmButtonStyle: TextButton.StyleFrom(foregroundColor: Colors.Red));
        using var harness = CreateHarness(
            new DatePickerTheme(
                pickerTheme,
                new Navigator(new BuilderPageRoute(_ => new DatePickerDialog(
                    initialDate: new DateTime(2026, 3, 12),
                    firstDate: new DateTime(2026, 1, 1),
                    lastDate: new DateTime(2026, 12, 31),
                    initialEntryMode: DatePickerEntryMode.InputOnly,
                    helpText: "Choose birthday",
                    cancelText: "BACK",
                    confirmText: "USE")))));
        var semantics = harness.PumpAndGetSemantics(new Size(500, 700));
        Assert.Null(FindSemantics(semantics, node =>
            node.Label is "Switch to input" or "Switch to calendar"));
        Assert.Contains(FindDescendants<RenderColoredBox>(harness.RenderView), box => box.Color == Colors.Orange);
        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView), box => box.Decoration.Color == Colors.Purple);
        Assert.Contains(
            FindDescendants<RenderParagraph>(harness.RenderView),
            paragraph => paragraph.PlainText == "Choose birthday");
        Assert.Contains(
            FindDescendants<RenderParagraph>(harness.RenderView),
            paragraph => paragraph.PlainText == "BACK");
        Assert.Contains(
            FindDescendants<RenderParagraph>(harness.RenderView),
            paragraph => paragraph.PlainText == "USE");
    }

    [Fact]
    public void DatePickerDialog_M2PortraitAndM3InputLandscapeUseSourceSizes()
    {
        using var m2 = CreateHarness(
            new Navigator(new BuilderPageRoute(_ => new DatePickerDialog(
                firstDate: new DateTime(2026, 1, 1),
                lastDate: new DateTime(2026, 12, 31)))),
            ThemeData.Light with { UseMaterial3 = false },
            new Size(500, 700));
        m2.Pump(new Size(500, 700));
        Assert.Contains(FindDescendants<RenderConstrainedBox>(m2.RenderView), box =>
            Close(box.AdditionalConstraints.MinWidth, 330)
            && Close(box.AdditionalConstraints.MaxWidth, 330)
            && Close(box.AdditionalConstraints.MinHeight, 518)
            && Close(box.AdditionalConstraints.MaxHeight, 518));

        using var landscape = CreateHarness(
            new Navigator(new BuilderPageRoute(_ => new DatePickerDialog(
                firstDate: new DateTime(2026, 1, 1),
                lastDate: new DateTime(2026, 12, 31),
                initialEntryMode: DatePickerEntryMode.InputOnly))),
            mediaSize: new Size(700, 500));
        landscape.Pump(new Size(700, 500));
        Assert.Contains(FindDescendants<RenderConstrainedBox>(landscape.RenderView), box =>
            Close(box.AdditionalConstraints.MinWidth, 496)
            && Close(box.AdditionalConstraints.MaxWidth, 496)
            && Close(box.AdditionalConstraints.MinHeight, 160)
            && Close(box.AdditionalConstraints.MaxHeight, 160));
    }

    [Fact]
    public async Task ShowDatePicker_InputModeRejectsInvalidThenReturnsSavedDate()
    {
        BuildContext captured = default;
        using var harness = CreateHarness(new Navigator(new BuilderPageRoute(context => new CaptureContext(
            value => captured = value,
            new Text("Home")))));
        harness.Pump(new Size(500, 700));
        var result = MaterialDatePickers.ShowDatePicker(
            captured,
            initialDate: new DateTime(2026, 3, 12),
            firstDate: new DateTime(2026, 1, 1),
            lastDate: new DateTime(2026, 12, 31),
            initialEntryMode: DatePickerEntryMode.Input,
            errorFormatText: "Bad date");
        PumpAnimation();
        var semantics = harness.PumpAndGetSemantics(new Size(500, 700));
        Assert.False(result.IsCompleted);
        Assert.True(FocusManager.Instance.HandleTextInput("bad"));

        var ok = FindSemantics(semantics, node =>
            node.Actions.HasFlag(SemanticsActions.Tap) && ContainsLabel(node, "OK"));
        Assert.True(ok is not null, DumpSemantics(semantics));
        Assert.True(ok!.PerformAction(SemanticsActions.Tap));
        harness.Pump(new Size(500, 700));
        Assert.False(result.IsCompleted);
        Assert.Contains(
            FindDescendants<RenderParagraph>(harness.RenderView),
            paragraph => paragraph.PlainText == "Bad date");

        Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent("A", isDown: true, isControlPressed: true)));
        Assert.True(FocusManager.Instance.HandleTextInput("03/16/2026"));
        semantics = harness.PumpAndGetSemantics(new Size(500, 700));
        ok = FindSemantics(semantics, node =>
            node.Actions.HasFlag(SemanticsActions.Tap) && ContainsLabel(node, "OK"));
        Assert.True(ok!.PerformAction(SemanticsActions.Tap));
        PumpAnimation();
        harness.Pump(new Size(500, 700));
        Assert.Equal(new DateTime(2026, 3, 16), await result);
    }

    [Fact]
    public async Task ShowDatePicker_CalendarSelectionUpdatesHeaderAndConfirmedResult()
    {
        BuildContext captured = default;
        using var harness = CreateHarness(new Navigator(new BuilderPageRoute(context => new CaptureContext(
            value => captured = value,
            new Text("Home")))));
        harness.Pump(new Size(500, 700));
        var result = MaterialDatePickers.ShowDatePicker(
            captured,
            initialDate: new DateTime(2026, 3, 12),
            firstDate: new DateTime(2026, 3, 1),
            lastDate: new DateTime(2026, 3, 31));
        PumpAnimation();
        var semantics = harness.PumpAndGetSemantics(new Size(500, 700));
        var day = FindSemantics(semantics, node =>
            node.Label?.StartsWith("16, Monday, March 16, 2026", StringComparison.Ordinal) == true
            && node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(day);
        Assert.True(day!.PerformAction(SemanticsActions.Tap));
        harness.Pump(new Size(500, 700));
        Assert.Contains(
            FindDescendants<RenderParagraph>(harness.RenderView),
            paragraph => paragraph.PlainText == "Mon, Mar 16");

        semantics = harness.PumpAndGetSemantics(new Size(500, 700));
        var ok = FindSemantics(semantics, node =>
            node.Actions.HasFlag(SemanticsActions.Tap) && ContainsLabel(node, "OK"));
        Assert.True(ok!.PerformAction(SemanticsActions.Tap));
        PumpAnimation();
        harness.Pump(new Size(500, 700));
        Assert.Equal(new DateTime(2026, 3, 16), await result);
    }

    [Fact]
    public void TimeOfDayMatchesFlutterValueAndFormattingContract()
    {
        var midnight = new TimeOfDay(0, 5);
        var afternoon = new TimeOfDay(15, 42);

        Assert.Equal(DayPeriod.Am, midnight.Period);
        Assert.Equal(12, midnight.HourOfPeriod);
        Assert.Equal(DayPeriod.Pm, afternoon.Period);
        Assert.Equal(3, afternoon.HourOfPeriod);
        Assert.True(midnight.IsBefore(afternoon));
        Assert.Equal(new TimeOfDay(15, 7), afternoon.Replacing(minute: 7));
        Assert.Equal("TimeOfDay(15:42)", afternoon.ToString());
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimeOfDay(24, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimeOfDay(0, 60));

        Assert.Equal("3:42 PM", DefaultMaterialLocalizations.Instance.FormatTimeOfDay(afternoon));
        Assert.Equal("15:42", DefaultMaterialLocalizations.Instance.FormatTimeOfDay(afternoon, alwaysUse24HourFormat: true));
    }

    [Fact]
    public void DateRangePickerDialog_NormalizesAndValidatesRangeContract()
    {
        var dialog = new DateRangePickerDialog(
            initialDateRange: new DateTimeRange<DateTime>(
                new DateTime(2026, 3, 10, 14, 0, 0),
                new DateTime(2026, 3, 15, 18, 0, 0)),
            firstDate: new DateTime(2026, 1, 1, 9, 0, 0),
            lastDate: new DateTime(2026, 12, 31, 23, 0, 0));
        Assert.Equal(new DateTime(2026, 3, 10), dialog.InitialDateRange!.Start);
        Assert.Equal(new DateTime(2026, 3, 15), dialog.InitialDateRange.End);
        Assert.Equal(DatePickerEntryMode.Calendar, dialog.InitialEntryMode);

        Assert.Throws<ArgumentException>(() => new DateRangePickerDialog(
            firstDate: new DateTime(2026, 2, 1),
            lastDate: new DateTime(2026, 1, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DateRangePickerDialog(
            initialDateRange: new DateTimeRange<DateTime>(new DateTime(2025, 12, 31), new DateTime(2026, 1, 2)),
            firstDate: new DateTime(2026, 1, 1),
            lastDate: new DateTime(2026, 12, 31)));
    }

    [Fact]
    public void DateRangePickerDialog_CalendarSelectionUsesRangeStatesAndPredicate()
    {
        var rangeColor = Color.Parse("#FF345678");
        using var harness = CreateHarness(new DatePickerTheme(
            new DatePickerThemeData(RangeSelectionBackgroundColor: rangeColor),
            new Navigator(new BuilderPageRoute(_ => new DateRangePickerDialog(
                firstDate: new DateTime(2026, 3, 1),
                lastDate: new DateTime(2026, 3, 31),
                currentDate: new DateTime(2026, 3, 13),
                selectableDayPredicate: (day, _, _) => day.Day != 14)))));
        var semantics = harness.PumpAndGetSemantics(new Size(500, 700));
        var start = FindSemantics(semantics, node => node.Label?.StartsWith("10, Tuesday, March 10, 2026") == true);
        var disabled = FindSemantics(semantics, node => node.Label?.StartsWith("14, Saturday, March 14, 2026") == true);
        Assert.NotNull(start);
        Assert.True(start!.PerformAction(SemanticsActions.Tap));
        Assert.NotNull(disabled);
        Assert.False(disabled!.Actions.HasFlag(SemanticsActions.Tap));

        semantics = harness.PumpAndGetSemantics(new Size(500, 700));
        var end = FindSemantics(semantics, node => node.Label?.StartsWith("16, Monday, March 16, 2026") == true);
        Assert.NotNull(end);
        Assert.True(end!.PerformAction(SemanticsActions.Tap));
        harness.Pump(new Size(500, 700));
        Assert.Contains(FindDescendants<RenderCustomPaint>(harness.RenderView), paint =>
            paint.Painter is DateRangeHighlightPainter painter
            && painter.Color == rangeColor);
    }

    [Fact]
    public async Task ShowDateRangePicker_CalendarReturnsSelectedRange()
    {
        BuildContext captured = default;
        using var harness = CreateHarness(new Navigator(new BuilderPageRoute(context => new CaptureContext(
            value => captured = value,
            new Text("Home")))));
        harness.Pump(new Size(500, 700));
        var result = MaterialDatePickers.ShowDateRangePicker(
            captured,
            firstDate: new DateTime(2026, 3, 1),
            lastDate: new DateTime(2026, 3, 31),
            currentDate: new DateTime(2026, 3, 13));
        PumpAnimation();
        var semantics = harness.PumpAndGetSemantics(new Size(500, 700));
        var start = FindSemantics(semantics, node => node.Label?.StartsWith("10, Tuesday, March 10, 2026") == true);
        Assert.True(start!.PerformAction(SemanticsActions.Tap));
        semantics = harness.PumpAndGetSemantics(new Size(500, 700));
        var end = FindSemantics(semantics, node => node.Label?.StartsWith("16, Monday, March 16, 2026") == true);
        Assert.True(end!.PerformAction(SemanticsActions.Tap));
        semantics = harness.PumpAndGetSemantics(new Size(500, 700));
        var save = FindSemantics(semantics, node => node.Actions.HasFlag(SemanticsActions.Tap) && ContainsLabel(node, "Save"));
        Assert.NotNull(save);
        Assert.True(save!.PerformAction(SemanticsActions.Tap));
        PumpAnimation();
        harness.Pump(new Size(500, 700));
        var range = await result;
        Assert.Equal(new DateTime(2026, 3, 10), range!.Start);
        Assert.Equal(new DateTime(2026, 3, 16), range.End);
    }

    private static void PumpAnimation()
    {
        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.25));
    }

    private static CalendarDatePicker BuildPicker() => new(
        initialDate: new DateTime(2026, 7, 8),
        firstDate: new DateTime(2026, 1, 1),
        lastDate: new DateTime(2026, 12, 31),
        onDateChanged: _ => { });

    private static WidgetRenderHarness CreateHarness(
        Widget child,
        ThemeData? theme = null,
        Size? mediaSize = null) => new(
        new MediaQuery(
            new MediaQueryData(Size: mediaSize ?? new Size(420, 500)),
            new Directionality(TextDirection.Ltr, new Theme(theme ?? ThemeData.Light, child))));

    private static bool Close(double a, double b) => Math.Abs(a - b) < 0.001;

    private static Color WithOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Round(255 * opacity),
        color.R,
        color.G,
        color.B);

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null) return result;
        if (root is T target) result.Add(target);
        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private static SemanticsNode? FindSemantics(SemanticsNode? node, Func<SemanticsNode, bool> predicate)
    {
        if (node is null) return null;
        if (predicate(node)) return node;
        foreach (var child in node.Children)
        {
            var result = FindSemantics(child, predicate);
            if (result is not null) return result;
        }
        return null;
    }

    private static bool ContainsLabel(SemanticsNode node, string label)
    {
        if (node.Label == label) return true;
        return node.Children.Any(child => ContainsLabel(child, label));
    }

    private static string DumpSemantics(SemanticsNode? node, int depth = 0)
    {
        if (node is null) return "<null>";
        var lines = new List<string>
        {
            $"{new string(' ', depth * 2)}label={node.Label ?? "<null>"}; actions={node.Actions}; flags={node.Flags}"
        };
        foreach (var child in node.Children) lines.Add(DumpSemantics(child, depth + 1));
        return string.Join(Environment.NewLine, lines);
    }

    private sealed class MondayFirstLocalizations : MaterialLocalizations
    {
        public override int FirstDayOfWeekIndex => 1;
        public override string TabLabel(int tabIndex, int tabCount) => $"{tabIndex + 1}/{tabCount}";
    }

    private sealed class CaptureContext : StatelessWidget
    {
        private readonly Action<BuildContext> _capture;
        private readonly Widget _child;

        public CaptureContext(Action<BuildContext> capture, Widget child)
        {
            _capture = capture;
            _child = child;
        }

        public override Widget Build(BuildContext context)
        {
            _capture(context);
            return _child;
        }
    }

    private sealed class CaptureDatePickerDefaults : StatelessWidget
    {
        private readonly Action<DatePickerThemeData> _capture;

        public CaptureDatePickerDefaults(Action<DatePickerThemeData> capture)
        {
            _capture = capture;
        }

        public override Widget Build(BuildContext context)
        {
            _capture(DatePickerTheme.Defaults(context));
            return new SizedBox();
        }
    }

    private sealed class CaptureAndOverrideDatePickerTheme : StatelessWidget
    {
        private readonly Action<DatePickerThemeData> _capture;

        public CaptureAndOverrideDatePickerTheme(Action<DatePickerThemeData> capture)
        {
            _capture = capture;
        }

        public override Widget Build(BuildContext context)
        {
            CapturedThemes capturedThemes = InheritedTheme.Capture(context);
            return new DatePickerTheme(
                new DatePickerThemeData(BackgroundColor: Colors.Crimson),
                capturedThemes.Wrap(new Builder(capturedContext =>
                {
                    _capture(DatePickerTheme.Of(capturedContext));
                    return new SizedBox();
                })));
        }
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _rootElement;
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget rootWidget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _rootElement = new HarnessRootElement(RenderView, rootWidget);
            _rootElement.Attach(_owner);
            _rootElement.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        public SemanticsNode? PumpAndGetSemantics(Size size)
        {
            Pump(size);
            _pipeline.RequestSemanticsUpdate();
            _pipeline.FlushSemantics();
            return _pipeline.SemanticsOwner.RootNode;
        }

        public void Dispose() => _rootElement.Unmount();

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;
            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget) => _renderView = renderView;
            public override RenderObject? RenderObject => _child?.RenderObject;
            internal override Element? RenderObjectAttachingChild => _child;
            protected override void OnMount() { base.OnMount(); Rebuild(); }
            internal override void Rebuild() { Dirty = false; _child = UpdateChild(_child, Widget, Slot); }
            internal override void Update(Widget newWidget) { base.Update(newWidget); Rebuild(); }
            internal override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
            internal override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
            public void InsertRenderObjectChild(RenderObject child, object? slot) => _renderView.Child = (RenderBox)child;
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot) { if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null; }
            internal override void Unmount() { if (_child is not null) { UnmountChild(_child); _child = null; } base.Unmount(); }
        }
    }

    /// <summary>
    /// Whether one of the node's merged label parts is <paramref name="part"/>. A merged node joins
    /// the labels it absorbed with a newline, exactly like Flutter's <c>_concatAttributedString</c>.
    /// </summary>
    private static bool HasLabelPart(SemanticsNode node, string part) =>
        node.Label?.Split('\n').Contains(part) == true;
}

