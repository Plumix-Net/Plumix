// Dart parity source: material_ui/lib/src/input_date_picker_form_field.dart
// Mirrors material-ui-src/test/input_date_picker_form_field_test.dart

using Avalonia;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using static Plumix.Tests.SemanticsMatchers;
using MaterialWidget = Plumix.Material.Material;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class InputDatePickerFormFieldDartParityTests : IDisposable
{
    public InputDatePickerFormFieldDartParityTests()
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

    private static Widget InputDatePickerField(
        Key? key = null,
        DateTime? initialDate = null,
        DateTime? firstDate = null,
        DateTime? lastDate = null,
        Action<DateTime>? onDateSubmitted = null,
        Action<DateTime>? onDateSaved = null,
        SelectableDayPredicate? selectableDayPredicate = null,
        string? errorFormatText = null,
        string? errorInvalidText = null,
        string? fieldHintText = null,
        string? fieldLabelText = null,
        bool autofocus = false,
        Key? formKey = null,
        ThemeData? theme = null,
        IReadOnlyList<LocalizationsDelegate>? localizationsDelegates = null,
        bool acceptEmptyDate = false,
        FocusNode? focusNode = null)
    {
        return new MaterialApp(
            theme: theme ?? ThemeData.From(colorScheme: ColorScheme.Light()),
            localizationsDelegates: localizationsDelegates,
            home: new MaterialWidget(
                child: new Form(
                    key: formKey,
                    child: new InputDatePickerFormField(
                        key: key,
                        initialDate: initialDate ?? new DateTime(2016, 1, 15),
                        firstDate: firstDate ?? new DateTime(2001, 1, 1),
                        lastDate: lastDate ?? new DateTime(2031, 12, 31),
                        onDateSubmitted: onDateSubmitted,
                        onDateSaved: onDateSaved,
                        selectableDayPredicate: selectableDayPredicate,
                        errorFormatText: errorFormatText,
                        errorInvalidText: errorInvalidText,
                        fieldHintText: fieldHintText,
                        fieldLabelText: fieldLabelText,
                        autofocus: autofocus,
                        acceptEmptyDate: acceptEmptyDate,
                        focusNode: focusNode))));
    }

    private static TextField TextField(FrameworkDartTester tester)
    {
        return tester.Widget<TextField>(Find.ByType<TextField>());
    }

    private static TextEditingController TextFieldController(FrameworkDartTester tester)
    {
        return TextField(tester).Controller!;
    }

    private static double TextOpacity(FrameworkDartTester tester, string textValue)
    {
        FadeTransition opacityWidget = tester.Widget<FadeTransition>(
            Find.Ancestor(of: Find.Text(textValue), matching: Find.ByType<FadeTransition>()).First);
        return opacityWidget.Opacity.Value;
    }

    // ---- group('InputDatePickerFormField') ----

    // Flutter: "Initial date is the default"
    [Fact]
    public void InitialDateIsTheDefault()
    {
        using FrameworkDartTester tester = CreateTester();
        var formKey = new LabeledGlobalKey<FormState>(null);
        var initialDate = new DateTime(2016, 2, 21);
        DateTime? inputDate = null;
        tester.PumpWidget(InputDatePickerField(
            initialDate: initialDate,
            onDateSaved: date => inputDate = date,
            formKey: formKey));
        Assert.Equal("02/21/2016", TextFieldController(tester).Value.Text);
        formKey.CurrentState!.Save();
        Assert.Equal(initialDate, inputDate);
    }

    // Flutter: "Changing initial date is reflected in text value"
    [Fact]
    public void ChangingInitialDateIsReflectedInTextValue()
    {
        using FrameworkDartTester tester = CreateTester();
        var initialDate = new DateTime(2016, 2, 21);
        var updatedInitialDate = new DateTime(2016, 2, 23);
        tester.PumpWidget(InputDatePickerField(initialDate: initialDate));
        Assert.Equal("02/21/2016", TextFieldController(tester).Value.Text);

        tester.PumpWidget(InputDatePickerField(initialDate: updatedInitialDate));
        tester.PumpAndSettle();
        Assert.Equal("02/23/2016", TextFieldController(tester).Value.Text);
    }

    // Flutter: "Valid date entry"
    [Fact]
    public void ValidDateEntry()
    {
        using FrameworkDartTester tester = CreateTester();
        var formKey = new LabeledGlobalKey<FormState>(null);
        DateTime? inputDate = null;
        tester.PumpWidget(InputDatePickerField(onDateSaved: date => inputDate = date, formKey: formKey));

        TextFieldController(tester).Text = "02/21/2016";
        formKey.CurrentState!.Save();
        Assert.Equal(new DateTime(2016, 2, 21), inputDate);
    }

    // Flutter: "Invalid text entry shows errorFormat text"
    [Fact]
    public void InvalidTextEntryShowsErrorFormatText()
    {
        using FrameworkDartTester tester = CreateTester();
        var formKey = new LabeledGlobalKey<FormState>(null);
        DateTime? inputDate = null;
        tester.PumpWidget(InputDatePickerField(onDateSaved: date => inputDate = date, formKey: formKey));
        // Default errorFormat text
        Finds.Nothing(Find.Text("Invalid format."));
        tester.EnterText(Find.ByType<TextField>(), "foobar");
        Assert.False(formKey.CurrentState!.Validate());
        tester.PumpAndSettle();
        Assert.Null(inputDate);
        Finds.OneWidget(Find.Text("Invalid format."));

        // Change to a custom errorFormat text
        tester.PumpWidget(InputDatePickerField(
            onDateSaved: date => inputDate = date,
            errorFormatText: "That is not a date.",
            formKey: formKey));
        Assert.False(formKey.CurrentState!.Validate());
        tester.PumpAndSettle();
        Finds.Nothing(Find.Text("Invalid format."));
        Finds.OneWidget(Find.Text("That is not a date."));
    }

    // Flutter: "Valid text entry, but date outside first or last date shows bounds shows errorInvalid text"
    [Fact]
    public void ValidTextEntryButDateOutsideFirstOrLastDateShowsBoundsShowsErrorInvalidText()
    {
        using FrameworkDartTester tester = CreateTester();
        var formKey = new LabeledGlobalKey<FormState>(null);
        DateTime? inputDate = null;
        tester.PumpWidget(InputDatePickerField(
            firstDate: new DateTime(1966, 2, 21),
            lastDate: new DateTime(2040, 2, 23),
            onDateSaved: date => inputDate = date,
            formKey: formKey));
        // Default errorInvalid text
        Finds.Nothing(Find.Text("Out of range."));
        // Before first date
        tester.EnterText(Find.ByType<TextField>(), "02/21/1950");
        Assert.False(formKey.CurrentState!.Validate());
        tester.PumpAndSettle();
        Assert.Null(inputDate);
        Finds.OneWidget(Find.Text("Out of range."));
        // After last date
        tester.EnterText(Find.ByType<TextField>(), "02/23/2050");
        Assert.False(formKey.CurrentState!.Validate());
        tester.PumpAndSettle();
        Assert.Null(inputDate);
        Finds.OneWidget(Find.Text("Out of range."));

        tester.PumpWidget(InputDatePickerField(
            onDateSaved: date => inputDate = date,
            errorInvalidText: "Not in given range.",
            formKey: formKey));
        Assert.False(formKey.CurrentState!.Validate());
        tester.PumpAndSettle();
        Finds.Nothing(Find.Text("Out of range."));
        Finds.OneWidget(Find.Text("Not in given range."));
    }

    // Flutter: "selectableDatePredicate will be used to show errorInvalid if date is not selectable"
    [Fact]
    public void SelectableDatePredicateWillBeUsedToShowErrorInvalidIfDateIsNotSelectable()
    {
        using FrameworkDartTester tester = CreateTester();
        var formKey = new LabeledGlobalKey<FormState>(null);
        DateTime? inputDate = null;
        tester.PumpWidget(InputDatePickerField(
            initialDate: new DateTime(2016, 1, 16),
            onDateSaved: date => inputDate = date,
            selectableDayPredicate: date => date.Day % 2 == 0,
            formKey: formKey));
        // Default errorInvalid text
        Finds.Nothing(Find.Text("Out of range."));
        // Odd day shouldn't be valid
        tester.EnterText(Find.ByType<TextField>(), "02/21/1966");
        Assert.False(formKey.CurrentState!.Validate());
        tester.PumpAndSettle();
        Assert.Null(inputDate);
        Finds.OneWidget(Find.Text("Out of range."));
        // Even day is valid
        tester.EnterText(Find.ByType<TextField>(), "02/24/2030");
        Assert.True(formKey.CurrentState!.Validate());
        formKey.CurrentState!.Save();
        tester.PumpAndSettle();
        Assert.Equal(new DateTime(2030, 2, 24), inputDate);
        Finds.Nothing(Find.Text("Out of range."));
    }

    // Flutter: "Empty field shows hint text when focused"
    [Fact]
    public void EmptyFieldShowsHintTextWhenFocused()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(InputDatePickerField());
        // Focus on it
        tester.Tap(Find.ByType<TextField>());
        tester.PumpAndSettle();

        // Hint text should be invisible
        Assert.Equal(0.0, TextOpacity(tester, "mm/dd/yyyy"));
        TextFieldController(tester).Clear();
        tester.PumpAndSettle();
        // Hint text should be visible
        Assert.Equal(1.0, TextOpacity(tester, "mm/dd/yyyy"));

        // Change to a different hint text
        tester.PumpWidget(InputDatePickerField(fieldHintText: "Enter some date"));
        tester.PumpAndSettle();
        Finds.Nothing(Find.Text("mm/dd/yyyy"));
        Assert.Equal(1.0, TextOpacity(tester, "Enter some date"));
        tester.EnterText(Find.ByType<TextField>(), "foobar");
        tester.PumpAndSettle();
        Assert.Equal(0.0, TextOpacity(tester, "Enter some date"));
    }

    // Flutter: "Label text"
    [Fact]
    public void LabelText()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(InputDatePickerField());
        // Default label
        Finds.OneWidget(Find.Text("Enter Date"));

        tester.PumpWidget(InputDatePickerField(fieldLabelText: "Give me a date!"));
        Finds.Nothing(Find.Text("Enter Date"));
        Finds.OneWidget(Find.Text("Give me a date!"));
    }

    // Flutter: "Semantics"
    [Fact]
    public void Semantics()
    {
        using FrameworkDartTester tester = CreateTester();
        SemanticsHandle semantics = tester.EnsureSemantics();

        // Fill the clipboard so that the Paste option is available in the text
        // selection menu.
        using var clipboard = new MockClipboardPlatform();
        Clipboard.SetData(new ClipboardData("Clipboard data"));

        tester.PumpWidget(InputDatePickerField(autofocus: true));
        tester.PumpAndSettle();

        ExpectSemantics(
            tester.GetSemantics(Find.ByType<EditableText>()),
            MatchesSemantics(
                label: "Enter Date",
                isTextField: true,
                isFocusable: true,
                hasEnabledState: true,
                isEnabled: true,
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
        semantics.Dispose();
    }

    // Flutter: "ThemeData.inputDecorationTheme is honored"
    [Fact]
    public void ThemeDataInputDecorationThemeIsHonored()
    {
        using FrameworkDartTester tester = CreateTester();
        InputBorder border = InputBorder.None;
        tester.PumpWidget(InputDatePickerField(
            theme: ThemeData.From(colorScheme: ColorScheme.Light()) with
            {
                InputDecorationTheme = new InputDecorationThemeData(border: border),
            }));
        tester.PumpAndSettle();

        // Get the border and container color from the painter of the _BorderContainer
        // (this was cribbed from input_decorator_test.dart).
        CustomPaint customPaint = tester.Widget<CustomPaint>(Find.Descendant(
            of: Find.ByWidgetPredicate(w => w.GetType().Name == "BorderContainer"),
            matching: Find.ByWidgetPredicate(w => w is CustomPaint)));
        var inputBorderPainter = (InputBorderPainter)customPaint.ForegroundPainter!;
        // Plumix's painter receives the border the `_InputBorderTween` evaluated to.
        InputBorder actualBorder = inputBorderPainter.Border;
        Color containerColor = inputBorderPainter.BlendedColor;

        // Border should match
        Assert.Equal(border, actualBorder);

        // It shouldn't be filled, so the color should be transparent
        Assert.Equal(MaterialColors.Transparent, containerColor);
    }

    // Flutter: "Date text localization"
    [Fact]
    public void DateTextLocalization()
    {
        using FrameworkDartTester tester = CreateTester();
        LocalizationsDelegate[] delegates =
        [
            new TestMaterialLocalizationsDelegate(),
            DefaultWidgetsLocalizations.Delegate,
        ];
        tester.PumpWidget(InputDatePickerField(localizationsDelegates: delegates));
        tester.EnterText(Find.ByType<TextField>(), "01/01/2022");
        tester.PumpAndSettle();

        // Verify that the widget can be updated to a new value after the
        // entered text was transformed by the localization formatter.
        tester.PumpWidget(InputDatePickerField(
            initialDate: new DateTime(2017, 1, 1),
            localizationsDelegates: delegates));
    }

    // Flutter: "when an empty date is entered and acceptEmptyDate is true, then errorFormatText is not shown"
    [Fact]
    public void WhenAnEmptyDateIsEnteredAndAcceptEmptyDateIsTrueThenErrorFormatTextIsNotShown()
    {
        using FrameworkDartTester tester = CreateTester();
        var formKey = new LabeledGlobalKey<FormState>(null);
        const string errorFormatText = "That is not a date.";
        tester.PumpWidget(InputDatePickerField(
            errorFormatText: errorFormatText,
            formKey: formKey,
            acceptEmptyDate: true));
        tester.EnterText(Find.ByType<TextField>(), string.Empty);
        tester.PumpAndSettle();
        formKey.CurrentState!.Validate();
        tester.PumpAndSettle();
        Finds.Nothing(Find.Text(errorFormatText));
    }

    // Flutter: "when an empty date is entered and acceptEmptyDate is false, then errorFormatText is shown"
    [Fact]
    public void WhenAnEmptyDateIsEnteredAndAcceptEmptyDateIsFalseThenErrorFormatTextIsShown()
    {
        using FrameworkDartTester tester = CreateTester();
        var formKey = new LabeledGlobalKey<FormState>(null);
        const string errorFormatText = "That is not a date.";
        tester.PumpWidget(InputDatePickerField(errorFormatText: errorFormatText, formKey: formKey));
        tester.EnterText(Find.ByType<TextField>(), string.Empty);
        tester.PumpAndSettle();
        formKey.CurrentState!.Validate();
        tester.PumpAndSettle();
        Finds.OneWidget(Find.Text(errorFormatText));
    }

    // Flutter: "FocusNode can request focus"
    [Fact]
    public void FocusNodeCanRequestFocus()
    {
        using FrameworkDartTester tester = CreateTester();
        var focusNode = new FocusNode();
        try
        {
            tester.PumpWidget(InputDatePickerField(focusNode: focusNode));
            Assert.Same(focusNode, tester.Widget<TextField>(Find.ByType<TextField>()).FocusNode);
            Assert.False(focusNode.HasFocus);
            focusNode.RequestFocus();
            tester.PumpAndSettle();
            Assert.True(focusNode.HasFocus);
            focusNode.Unfocus();
            tester.PumpAndSettle();
            Assert.False(focusNode.HasFocus);
        }
        finally
        {
            focusNode.Dispose();
        }
    }

    // ---- group('Calendar Delegate') ----

    // Flutter: "Defaults to Gregorian calendar system"
    [Fact]
    public void DefaultsToGregorianCalendarSystem()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(
                child: new InputDatePickerFormField(
                    initialDate: new DateTime(2025, 2, 26),
                    firstDate: new DateTime(2025, 2, 1),
                    lastDate: new DateTime(2026, 5, 1)))));

        InputDatePickerFormField inputDatePickerField =
            tester.Widget<InputDatePickerFormField>(Find.ByType<InputDatePickerFormField>());
        Assert.IsAssignableFrom<GregorianCalendarDelegate>(inputDatePickerField.CalendarDelegate);
    }

    // Flutter: "Using custom calendar delegate implementation"
    [Fact]
    public void UsingCustomCalendarDelegateImplementation()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(
                child: new InputDatePickerFormField(
                    initialDate: new DateTime(2025, 2, 26),
                    firstDate: new DateTime(2025, 2, 1),
                    lastDate: new DateTime(2026, 5, 1),
                    calendarDelegate: new TestCalendarDelegate()))));

        InputDatePickerFormField inputDatePickerField =
            tester.Widget<InputDatePickerFormField>(Find.ByType<InputDatePickerFormField>());
        Assert.IsAssignableFrom<TestCalendarDelegate>(inputDatePickerField.CalendarDelegate);
    }

    // Flutter: "Displays calendar based on the calendar delegate"
    [Fact]
    public void DisplaysCalendarBasedOnTheCalendarDelegate()
    {
        using FrameworkDartTester tester = CreateTester();
        DateTime? selectedDate = null;

        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(
                child: new InputDatePickerFormField(
                    initialDate: new DateTime(2025, 2, 26),
                    firstDate: new DateTime(2025, 2, 1),
                    lastDate: new DateTime(2026, 5, 1),
                    onDateSubmitted: value => selectedDate = value,
                    calendarDelegate: new TestCalendarDelegate()))));

        Finder dateInput1 = Find.Descendant(of: Find.ByType<TextField>(), matching: Find.Text("2025..2..26"));
        Finds.OneWidget(dateInput1);

        tester.Tap(dateInput1);
        tester.PumpAndSettle();

        tester.EnterText(dateInput1, "2025..3..10");
        tester.TestTextInput.ReceiveAction(TextInputActionType.Done);
        tester.PumpAndSettle();

        Assert.Equal(new DateTime(2025, 3, 10), selectedDate);

        Finder dateInput2 = Find.Descendant(of: Find.ByType<TextField>(), matching: Find.Text("2025..3..10"));
        Finds.OneWidget(dateInput2);

        tester.Tap(dateInput2);
        tester.PumpAndSettle();

        tester.EnterText(dateInput2, "2025..4..21");
        tester.TestTextInput.ReceiveAction(TextInputActionType.Done);
        tester.PumpAndSettle();

        Assert.Equal(new DateTime(2025, 4, 21), selectedDate);
    }

    // Flutter: "InputDatePickerFormField does not crash at zero area"
    [Fact]
    public void InputDatePickerFormFieldDoesNotCrashAtZeroArea()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(new MaterialApp(
            home: new Scaffold(
                body: new Center(
                    child: SizedBox.Shrink(
                        child: new InputDatePickerFormField(
                            firstDate: new DateTime(2020, 1, 1),
                            lastDate: new DateTime(2030, 1, 1)))))));
        Assert.Equal(new Size(0, 0), tester.GetSize(Find.ByType<InputDatePickerFormField>()));
    }

    // Regression test for https://github.com/flutter/flutter/issues/177088.
    // Flutter: "Local InputDecorationTheme is honored"
    [Fact]
    public void LocalInputDecorationThemeIsHonored()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(new MaterialApp(
            home: new MaterialWidget(
                child: new Center(
                    child: new InputDecorationTheme(
                        data: new InputDecorationThemeData(filled: true),
                        child: new InputDatePickerFormField(
                            firstDate: new DateTime(2025, 2, 1),
                            lastDate: new DateTime(2026, 5, 1)))))));

        InputDecoration decoration = tester.Widget<TextField>(Find.ByType<TextField>()).Decoration!;
        Assert.True(decoration.Filled);
    }

    // ---- C#-only coverage of the constructor contract (Dart's asserts) ----

    [DebugOnlyFact]
    public void ConstructorAssertsDateContract()
    {
        Assert.Throws<AssertionError>(() => new InputDatePickerFormField(
            firstDate: new DateTime(2026, 2, 1),
            lastDate: new DateTime(2026, 1, 1)));
        AssertionError beforeFirst = Assert.Throws<AssertionError>(() => new InputDatePickerFormField(
            initialDate: new DateTime(2025, 12, 31),
            firstDate: new DateTime(2026, 1, 1),
            lastDate: new DateTime(2026, 2, 1)));
        Assert.Contains(
            "initialDate 2025-12-31 00:00:00.000 must be on or after firstDate 2026-01-01 00:00:00.000.",
            beforeFirst.Message);
        Assert.Throws<AssertionError>(() => new InputDatePickerFormField(
            initialDate: new DateTime(2026, 2, 2),
            firstDate: new DateTime(2026, 1, 1),
            lastDate: new DateTime(2026, 2, 1)));
        Assert.Throws<AssertionError>(() => new InputDatePickerFormField(
            initialDate: new DateTime(2026, 1, 15),
            firstDate: new DateTime(2026, 1, 1),
            lastDate: new DateTime(2026, 2, 1),
            selectableDayPredicate: _ => false));

        var field = new InputDatePickerFormField(
            initialDate: new DateTime(2026, 1, 15, 13, 45, 0),
            firstDate: new DateTime(2026, 1, 1, 8, 0, 0),
            lastDate: new DateTime(2026, 2, 1, 23, 0, 0));
        Assert.Equal(new DateTime(2026, 1, 15), field.InitialDate);
        Assert.Equal(new DateTime(2026, 1, 1), field.FirstDate);
        Assert.Equal(new DateTime(2026, 2, 1), field.LastDate);
        Assert.Null(field.KeyboardType);
        Assert.Same(GregorianCalendarDelegate.Instance, field.CalendarDelegate);
    }

    // C#-only: the field asks for Dart's default `TextInputType.datetime` keyboard.
    [Fact]
    public void KeyboardTypeDefaultsToDatetime()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(InputDatePickerField());
        Assert.Equal(TextInputType.Datetime, TextField(tester).KeyboardType);
    }

    private sealed class TestMaterialLocalizations : DefaultMaterialLocalizations
    {
        public override string FormatCompactDate(DateTime date) => $"{date.Month}/{date.Day}/{date.Year}";
    }

    private sealed class TestMaterialLocalizationsDelegate : LocalizationsDelegate<MaterialLocalizations>
    {
        public override bool IsSupported(Locale locale) => true;

        public override MaterialLocalizations LoadTyped(Locale locale) => new TestMaterialLocalizations();

        public override bool ShouldReload(LocalizationsDelegate oldDelegate) => false;
    }

    private sealed class TestCalendarDelegate : GregorianCalendarDelegate
    {
        public override string FormatCompactDate(DateTime date, MaterialLocalizations localizations)
        {
            return $"{date.Year}..{date.Month}..{date.Day}";
        }

        public override DateTime? ParseCompactDate(string? inputString, MaterialLocalizations localizations)
        {
            string[] parts = inputString!.Split("..");
            if (parts.Length != 3)
            {
                return null;
            }

            int year = int.TryParse(parts[0], out int y) ? y : 0;
            int month = int.TryParse(parts[1], out int m) ? m : 0;
            int day = int.TryParse(parts[2], out int d) ? d : 0;
            return DateUtils.DateTimeOf(year, month, day);
        }

        public override string DateHelpText(MaterialLocalizations localizations)
        {
            return "yyyy..mm..dd";
        }
    }
}
