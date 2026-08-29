using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialTimePickerDialogTests : IDisposable
{
    private static readonly Size ViewSize = new(500, 700);

    public MaterialTimePickerDialogTests()
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

    // ---- TimePickerThemeData contract -------------------------------------------------

    [Fact]
    public void TimePickerTheme_ResolvesLocalOverrideThenThemeData()
    {
        TimePickerThemeData? captured = null;
        var themeData = new TimePickerThemeData(DialHandColor: Colors.Purple);
        using var harness = CreateHarness(
            new TimePickerTheme(
                new TimePickerThemeData(DialHandColor: Colors.Orange),
                new CaptureContext(context => captured = TimePickerTheme.Of(context), new SizedBox())),
            theme: ThemeData.Light with { TimePickerTheme = themeData });
        harness.Pump(ViewSize);
        Assert.Equal(Colors.Orange, captured!.DialHandColor);

        TimePickerThemeData? fromTheme = null;
        using var plain = CreateHarness(
            new CaptureContext(context => fromTheme = TimePickerTheme.Of(context), new SizedBox()),
            theme: ThemeData.Light with { TimePickerTheme = themeData });
        plain.Pump(ViewSize);
        Assert.Equal(Colors.Purple, fromTheme!.DialHandColor);
    }

    // ---- Defaults -----------------------------------------------------------------------

    [Fact]
    public void Dial_UsesMaterial3SurfaceAndPrimaryDefaultsAndMaterial2LegacyOpacities()
    {
        using var m3 = CreateHarness(DialogRoute(new TimePickerDialog(new TimeOfDay(7, 0))));
        m3.Pump(ViewSize);
        var m3Painter = FindPainter(m3);
        var m3Scheme = ThemeData.Light.ColorScheme;
        Assert.Equal(m3Scheme.SurfaceContainerHighest, m3Painter.BackgroundColor);
        Assert.Equal(m3Scheme.Primary, m3Painter.HandColor);
        Assert.Equal(2, m3Painter.HandWidth);
        Assert.Equal(24, m3Painter.DotRadius);
        Assert.Equal(4, m3Painter.CenterRadius);

        using var m2 = CreateHarness(
            DialogRoute(new TimePickerDialog(new TimeOfDay(7, 0))),
            theme: ThemeData.Light with { UseMaterial3 = false });
        m2.Pump(ViewSize);
        var m2Painter = FindPainter(m2);
        var m2Scheme = ThemeData.Light.ColorScheme;
        Assert.Equal(WithOpacity(m2Scheme.OnSurface, 0.08), m2Painter.BackgroundColor);
        Assert.Equal(m2Scheme.Primary, m2Painter.HandColor);
        Assert.Equal(22, m2Painter.DotRadius);
    }

    [Fact]
    public void Dialog_UsesMaterial3ShapeElevationAndBackgroundDefaults()
    {
        using var harness = CreateHarness(DialogRoute(new TimePickerDialog(new TimeOfDay(7, 0))));
        harness.Pump(ViewSize);
        var dialog = FindWidget<Dialog>(harness);
        Assert.Equal(ThemeData.Light.ColorScheme.SurfaceContainerHigh, dialog.BackgroundColor);
        Assert.Equal(6, dialog.Elevation);
        var shape = Assert.IsType<RoundedRectangleBorder>(dialog.Shape);
        Assert.Equal(28, shape.BorderRadius.Physical.TopLeft);
        Assert.Equal(new Thickness(16, 24), dialog.InsetPadding);
    }

    [Fact]
    public void Dialog_InputModeDropsVerticalInsetPadding()
    {
        using var harness = CreateHarness(DialogRoute(new TimePickerDialog(
            new TimeOfDay(7, 0),
            initialEntryMode: TimePickerEntryMode.Input)));
        harness.Pump(ViewSize);
        Assert.Equal(new Thickness(16, 0), FindWidget<Dialog>(harness).InsetPadding);
    }

    // ---- Dial labels and formats --------------------------------------------------------

    [Fact]
    public void Dial_TwelveHourFormatBuildsTwelveLabelsStartingAtTwelve()
    {
        using var harness = CreateHarness(DialogRoute(new TimePickerDialog(new TimeOfDay(7, 0))));
        harness.Pump(ViewSize);
        var painter = FindPainter(harness);
        Assert.Equal(
            new[] { "12", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11" },
            painter.PrimaryLabels.Select(label => label.Text).ToArray());
        Assert.All(painter.PrimaryLabels, label => Assert.False(label.Inner));
        Assert.Equal(
            painter.PrimaryLabels.Select(label => label.Text),
            painter.SelectedLabels.Select(label => label.Text));
    }

    [Fact]
    public void Dial_Material3TwentyFourHourFormatBuildsDoubleRing()
    {
        using var harness = CreateHarness(
            DialogRoute(new TimePickerDialog(new TimeOfDay(23, 5))),
            alwaysUse24HourFormat: true);
        harness.Pump(ViewSize);
        var painter = FindPainter(harness);
        Assert.Equal(24, painter.PrimaryLabels.Count);
        Assert.Equal("00", painter.PrimaryLabels[0].Text);
        Assert.Equal("1", painter.PrimaryLabels[1].Text);
        Assert.Equal("23", painter.PrimaryLabels[23].Text);
        for (int index = 0; index < painter.PrimaryLabels.Count; index++)
        {
            Assert.Equal(index >= 12, painter.PrimaryLabels[index].Inner);
        }
    }

    [Fact]
    public void Dial_Material2TwentyFourHourFormatBuildsTwelveEvenLabels()
    {
        using var harness = CreateHarness(
            DialogRoute(new TimePickerDialog(new TimeOfDay(23, 5))),
            theme: ThemeData.Light with { UseMaterial3 = false },
            alwaysUse24HourFormat: true);
        harness.Pump(ViewSize);
        var painter = FindPainter(harness);
        Assert.Equal(
            new[] { "00", "02", "04", "06", "08", "10", "12", "14", "16", "18", "20", "22" },
            painter.PrimaryLabels.Select(label => label.Text).ToArray());
        Assert.All(painter.PrimaryLabels, label => Assert.False(label.Inner));
    }

    [Fact]
    public void Dial_TwentyFourHourFormatHidesTheDayPeriodControl()
    {
        using var harness = CreateHarness(
            DialogRoute(new TimePickerDialog(new TimeOfDay(23, 5))),
            alwaysUse24HourFormat: true);
        var semantics = harness.PumpAndGetSemantics(ViewSize);
        Assert.Null(FindSemantics(semantics, node => node.Label is "AM" or "PM"));
        Assert.Empty(FindDescendants<RenderDayPeriodInputPadding>(harness.RenderView));
    }

    // ---- Layout -------------------------------------------------------------------------

    [Fact]
    public void Dialog_PortraitDialModeUsesFlutterPortraitSize()
    {
        using var harness = CreateHarness(DialogRoute(new TimePickerDialog(new TimeOfDay(10, 30))));
        harness.Pump(ViewSize);
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView), box =>
            Close(box.AdditionalConstraints.MinWidth, 310)
            && Close(box.AdditionalConstraints.MaxWidth, 310)
            && Close(box.AdditionalConstraints.MinHeight, 216)
            && Close(box.AdditionalConstraints.MaxHeight, 468));
    }

    [Fact]
    public void Dialog_LandscapeDialModeUsesMaterial3And2Heights()
    {
        using var m3 = CreateHarness(
            DialogRoute(new TimePickerDialog(new TimeOfDay(10, 30), orientation: Orientation.Landscape)),
            mediaSize: new Size(900, 500));
        m3.Pump(new Size(900, 500));
        Assert.Contains(FindDescendants<RenderConstrainedBox>(m3.RenderView), box =>
            Close(box.AdditionalConstraints.MinWidth, 524) && Close(box.AdditionalConstraints.MaxHeight, 342));

        using var m2 = CreateHarness(
            DialogRoute(new TimePickerDialog(new TimeOfDay(10, 30), orientation: Orientation.Landscape)),
            theme: ThemeData.Light with { UseMaterial3 = false },
            mediaSize: new Size(900, 500));
        m2.Pump(new Size(900, 500));
        Assert.Contains(FindDescendants<RenderConstrainedBox>(m2.RenderView), box =>
            Close(box.AdditionalConstraints.MinWidth, 524) && Close(box.AdditionalConstraints.MaxHeight, 300));
    }

    [Fact]
    public void Dialog_ExplicitOrientationOverridesTheMediaQuery()
    {
        using var harness = CreateHarness(
            DialogRoute(new TimePickerDialog(new TimeOfDay(10, 30), orientation: Orientation.Landscape)),
            mediaSize: new Size(500, 900));
        harness.Pump(new Size(500, 900));
        // The landscape header is a fixed 216pt column; the portrait layout has no such box.
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView), box =>
            Close(box.AdditionalConstraints.MinWidth, 216) && Close(box.AdditionalConstraints.MaxWidth, 216));
    }

    [Fact]
    public void Dialog_InputModeWidthDropsThirtyTwoOnMaterial3AndTheDayPeriodOnTwentyFourHour()
    {
        using var twelve = CreateHarness(DialogRoute(new TimePickerDialog(
            new TimeOfDay(7, 0),
            initialEntryMode: TimePickerEntryMode.Input)));
        twelve.Pump(ViewSize);
        Assert.Contains(FindDescendants<RenderConstrainedBox>(twelve.RenderView), box =>
            Close(box.AdditionalConstraints.MinWidth, 312 - 32));

        using var twentyFour = CreateHarness(
            DialogRoute(new TimePickerDialog(new TimeOfDay(7, 0), initialEntryMode: TimePickerEntryMode.Input)),
            alwaysUse24HourFormat: true);
        twentyFour.Pump(ViewSize);
        Assert.Contains(FindDescendants<RenderConstrainedBox>(twentyFour.RenderView), box =>
            Close(box.AdditionalConstraints.MinWidth, 312 - 52 - 12));
    }

    [Fact]
    public void Dialog_LaysOutWithoutCrashingAtZeroAndTinyViewSizes()
    {
        foreach (var size in new[] { new Size(1, 1), new Size(100, 100), new Size(300, 300) })
        {
            using var harness = CreateHarness(DialogRoute(new TimePickerDialog(new TimeOfDay(7, 0))), mediaSize: size);
            harness.Pump(size);
        }

        using var shrunk = CreateHarness(
            new SizedBox(width: 0, height: 0, child: DialogRoute(new TimePickerDialog(new TimeOfDay(7, 0)))));
        shrunk.Pump(ViewSize);
    }

    [Fact]
    public void DayPeriodControl_MeetsTheMinimumInteractiveTapTarget()
    {
        using var harness = CreateHarness(DialogRoute(new TimePickerDialog(new TimeOfDay(7, 0))));
        harness.Pump(ViewSize);
        var padding = Assert.Single(FindDescendants<RenderDayPeriodInputPadding>(harness.RenderView));
        Assert.True(padding.Size.Width >= 48, $"width was {padding.Size.Width}");
        Assert.True(padding.Size.Height >= 96, $"height was {padding.Size.Height}");
    }

    [Fact]
    public void RenderDayPeriodInputPadding_RedirectsTapsInTheExpandedAreaOntoTheNearestHalf()
    {
        var child = new SizedBox(width: 52, height: 40);
        using var harness = new WidgetRenderHarness(new Directionality(
            TextDirection.Ltr,
            new Align(
                alignment: Alignment.TopLeft,
                child: new DayPeriodInputPadding(
                new Size(52, 96),
                Orientation.Portrait,
                new Listener(behavior: HitTestBehavior.Opaque, child: child)))));
        harness.Pump(ViewSize);
        var padding = Assert.Single(FindDescendants<RenderDayPeriodInputPadding>(harness.RenderView));
        Assert.Equal(new Size(52, 96), padding.Size);

        var topResult = new BoxHitTestResult();
        Assert.True(padding.HitTest(topResult, new Point(26, 2)));
        var bottomResult = new BoxHitTestResult();
        Assert.True(padding.HitTest(bottomResult, new Point(26, 94)));
        var outsideResult = new BoxHitTestResult();
        Assert.False(padding.HitTest(outsideResult, new Point(26, 200)));
    }

    // ---- Semantics ----------------------------------------------------------------------

    [Fact]
    public void Header_ExposesTheFormattedTimeAndHelpTextAndHidesTheSeparator()
    {
        using var harness = CreateHarness(DialogRoute(new TimePickerDialog(new TimeOfDay(7, 0))));
        var semantics = harness.PumpAndGetSemantics(ViewSize);
        Assert.NotNull(FindSemantics(semantics, node => HasLabelPart(node, "7:00 AM")));
        Assert.NotNull(FindSemantics(semantics, node => ContainsLabel(node, "Select time")));
        Assert.Null(FindSemantics(semantics, node => HasLabelPart(node, ":")));
    }

    [Fact]
    public void HourAndMinuteControls_ExposeModeAnnouncementValuesAndAdjustActions()
    {
        using var harness = CreateHarness(DialogRoute(new TimePickerDialog(new TimeOfDay(11, 0))));
        var semantics = harness.PumpAndGetSemantics(ViewSize);
        var hour = FindSemantics(semantics, node => node.Value == "Select hours 11");
        Assert.NotNull(hour);
        Assert.Equal("12", hour!.IncreasedValue);
        Assert.Equal("10", hour.DecreasedValue);
        Assert.True(hour.Actions.HasFlag(SemanticsActions.Increase));
        Assert.True(hour.Actions.HasFlag(SemanticsActions.Decrease));

        Assert.NotNull(FindSemantics(semantics, node => node.Value == "Select minutes 00"));
    }

    [Fact]
    public void HourSemanticsIncrementWrapsInsideTheDayPeriod()
    {
        using var harness = CreateHarness(DialogRoute(new TimePickerDialog(new TimeOfDay(11, 0))));
        var semantics = harness.PumpAndGetSemantics(ViewSize);
        var hour = FindSemantics(semantics, node => node.Value == "Select hours 11");
        Assert.True(hour!.PerformAction(SemanticsActions.Increase));
        semantics = harness.PumpAndGetSemantics(ViewSize);
        hour = FindSemantics(semantics, node => node.Value == "Select hours 12");
        Assert.NotNull(hour);
        Assert.True(hour!.PerformAction(SemanticsActions.Increase));
        semantics = harness.PumpAndGetSemantics(ViewSize);
        // 12 AM -> 1 AM: the day period is preserved.
        Assert.NotNull(FindSemantics(semantics, node => node.Value == "Select hours 1"));
    }

    [Fact]
    public void HourSemanticsIncrementWrapsThroughMidnightInTwentyFourHourMode()
    {
        using var harness = CreateHarness(
            DialogRoute(new TimePickerDialog(new TimeOfDay(23, 0))),
            alwaysUse24HourFormat: true);
        var semantics = harness.PumpAndGetSemantics(ViewSize);
        var hour = FindSemantics(semantics, node => node.Value == "Select hours 23");
        Assert.True(hour!.PerformAction(SemanticsActions.Increase));
        semantics = harness.PumpAndGetSemantics(ViewSize);
        Assert.NotNull(FindSemantics(semantics, node => node.Value == "Select hours 00"));
    }

    [Fact]
    public void MinuteSemanticsIncrementWrapsAndKeepsTheHour()
    {
        using var harness = CreateHarness(DialogRoute(new TimePickerDialog(new TimeOfDay(11, 59))));
        var semantics = harness.PumpAndGetSemantics(ViewSize);
        var minute = FindSemantics(semantics, node => node.Value == "Select minutes 59");
        Assert.Equal("00", minute!.IncreasedValue);
        Assert.True(minute.PerformAction(SemanticsActions.Increase));
        semantics = harness.PumpAndGetSemantics(ViewSize);
        Assert.NotNull(FindSemantics(semantics, node => node.Value == "Select minutes 00"));
        Assert.NotNull(FindSemantics(semantics, node => node.Value == "Select hours 11"));
    }

    [Fact]
    public void DayPeriodButtons_UseCheckedOffIOSAndSelectedOnIOS()
    {
        using var android = CreateHarness(
            DialogRoute(new TimePickerDialog(new TimeOfDay(14, 0))),
            theme: ThemeData.Light with { Platform = TargetPlatform.Android });
        var semantics = android.PumpAndGetSemantics(ViewSize);
        var pm = FindSemantics(semantics, node => ContainsLabel(node, "PM") && node.Flags.HasFlag(SemanticsFlags.IsButton));
        Assert.NotNull(pm);
        Assert.True(pm!.Flags.HasFlag(SemanticsFlags.HasCheckedState));
        Assert.True(pm.Flags.HasFlag(SemanticsFlags.IsChecked));
        Assert.True(pm.Flags.HasFlag(SemanticsFlags.IsInMutuallyExclusiveGroup));
        var am = FindSemantics(semantics, node => ContainsLabel(node, "AM") && node.Flags.HasFlag(SemanticsFlags.IsButton));
        Assert.False(am!.Flags.HasFlag(SemanticsFlags.IsChecked));

        using var ios = CreateHarness(
            DialogRoute(new TimePickerDialog(new TimeOfDay(14, 0))),
            theme: ThemeData.Light with { Platform = TargetPlatform.IOS });
        semantics = ios.PumpAndGetSemantics(ViewSize);
        var iosPm = FindSemantics(semantics, node => ContainsLabel(node, "PM") && node.Flags.HasFlag(SemanticsFlags.IsButton));
        Assert.True(iosPm!.Flags.HasFlag(SemanticsFlags.IsSelected));
        Assert.False(iosPm.Flags.HasFlag(SemanticsFlags.HasCheckedState));
    }

    [Fact]
    public void DayPeriodButton_TogglesTheSelectedTimeByTwelveHours()
    {
        using var harness = CreateHarness(DialogRoute(new TimePickerDialog(new TimeOfDay(7, 0))));
        var semantics = harness.PumpAndGetSemantics(ViewSize);
        var pm = FindSemantics(semantics, node => ContainsLabel(node, "PM") && node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(pm);
        Assert.True(pm!.PerformAction(SemanticsActions.Tap));
        semantics = harness.PumpAndGetSemantics(ViewSize);
        Assert.NotNull(FindSemantics(semantics, node => HasLabelPart(node, "7:00 PM")));
    }

    // ---- Entry modes --------------------------------------------------------------------

    [Fact]
    public void EntryMode_DialShowsTheKeyboardIconAndInputShowsTheClockIcon()
    {
        using var dial = CreateHarness(DialogRoute(new TimePickerDialog(new TimeOfDay(7, 0))));
        dial.Pump(ViewSize);
        Assert.Contains(FindWidgets<Icon>(dial), icon => Equals(icon.IconData, Icons.KeyboardOutlined));
        Assert.DoesNotContain(FindWidgets<Icon>(dial), icon => Equals(icon.IconData, Icons.AccessTime));

        using var input = CreateHarness(DialogRoute(new TimePickerDialog(
            new TimeOfDay(7, 0),
            initialEntryMode: TimePickerEntryMode.Input)));
        input.Pump(ViewSize);
        Assert.Contains(FindWidgets<Icon>(input), icon => Equals(icon.IconData, Icons.AccessTime));
    }

    [Fact]
    public void EntryMode_CustomSwitchIconsReplaceTheDefaults()
    {
        using var harness = CreateHarness(DialogRoute(new TimePickerDialog(
            new TimeOfDay(7, 0),
            switchToInputEntryModeIcon: new Icon(Icons.Add))));
        harness.Pump(ViewSize);
        Assert.Contains(FindWidgets<Icon>(harness), icon => Equals(icon.IconData, Icons.Add));
        Assert.DoesNotContain(FindWidgets<Icon>(harness), icon => Equals(icon.IconData, Icons.KeyboardOutlined));
    }

    [Fact]
    public void EntryMode_DialOnlyAndInputOnlyHideTheToggle()
    {
        using var dialOnly = CreateHarness(DialogRoute(new TimePickerDialog(
            new TimeOfDay(7, 0),
            initialEntryMode: TimePickerEntryMode.DialOnly)));
        dialOnly.Pump(ViewSize);
        Assert.DoesNotContain(FindWidgets<Icon>(dialOnly), icon => Equals(icon.IconData, Icons.KeyboardOutlined));
        Assert.Empty(FindWidgets<TextFormField>(dialOnly));

        using var inputOnly = CreateHarness(DialogRoute(new TimePickerDialog(
            new TimeOfDay(7, 0),
            initialEntryMode: TimePickerEntryMode.InputOnly)));
        inputOnly.Pump(ViewSize);
        Assert.DoesNotContain(FindWidgets<Icon>(inputOnly), icon => Equals(icon.IconData, Icons.AccessTime));
        Assert.Equal(2, FindWidgets<TextFormField>(inputOnly).Count);
    }

    [Fact]
    public void EntryMode_ToggleSwitchesModesAndReportsThroughTheCallback()
    {
        var reported = new List<TimePickerEntryMode>();
        using var harness = CreateHarness(DialogRoute(new TimePickerDialog(
            new TimeOfDay(7, 0),
            onEntryModeChanged: reported.Add)));
        var semantics = harness.PumpAndGetSemantics(ViewSize);
        var toggle = FindTappableUnderTooltip(semantics, "Switch to text input mode");
        Assert.NotNull(toggle);
        Assert.True(toggle!.PerformAction(SemanticsActions.Tap));
        harness.Pump(ViewSize);
        Assert.Equal([TimePickerEntryMode.Input], reported);
        Assert.Equal(2, FindWidgets<TextFormField>(harness).Count);

        semantics = harness.PumpAndGetSemantics(ViewSize);
        toggle = FindTappableUnderTooltip(semantics, "Switch to dial picker mode");
        Assert.NotNull(toggle);
        Assert.True(toggle!.PerformAction(SemanticsActions.Tap));
        harness.Pump(ViewSize);
        Assert.Equal([TimePickerEntryMode.Input, TimePickerEntryMode.Dial], reported);
        Assert.Empty(FindWidgets<TextFormField>(harness));
    }

    // ---- Labels and capitalization ------------------------------------------------------

    [Fact]
    public void Labels_AreUpperCasedOnMaterial2AndSentenceCasedOnMaterial3()
    {
        using var m3 = CreateHarness(DialogRoute(new TimePickerDialog(new TimeOfDay(7, 0))));
        m3.Pump(ViewSize);
        Assert.Contains(ParagraphTexts(m3), text => text == "Select time");
        Assert.Contains(ParagraphTexts(m3), text => text == "Cancel");
        Assert.Contains(ParagraphTexts(m3), text => text == "OK");

        using var m2 = CreateHarness(
            DialogRoute(new TimePickerDialog(new TimeOfDay(7, 0))),
            theme: ThemeData.Light with { UseMaterial3 = false });
        m2.Pump(ViewSize);
        Assert.Contains(ParagraphTexts(m2), text => text == "SELECT TIME");
        Assert.Contains(ParagraphTexts(m2), text => text == "CANCEL");
        Assert.Contains(ParagraphTexts(m2), text => text == "OK");
    }

    [Fact]
    public void Labels_InputModeUsesTheEnterTimeHelpTextAndHourMinuteLabels()
    {
        using var harness = CreateHarness(DialogRoute(new TimePickerDialog(
            new TimeOfDay(7, 0),
            initialEntryMode: TimePickerEntryMode.Input)));
        harness.Pump(ViewSize);
        Assert.Contains(ParagraphTexts(harness), text => text == "Enter time");
        Assert.Contains(ParagraphTexts(harness), text => text == "Hour");
        Assert.Contains(ParagraphTexts(harness), text => text == "Minute");
    }

    [Fact]
    public void Labels_OptionalTextParametersOverrideTheLocalizedStrings()
    {
        using var harness = CreateHarness(DialogRoute(new TimePickerDialog(
            new TimeOfDay(7, 0),
            initialEntryMode: TimePickerEntryMode.Input,
            helpText: "Pick it",
            cancelText: "Nope",
            confirmText: "Yep",
            hourLabelText: "Hrs",
            minuteLabelText: "Mins")));
        harness.Pump(ViewSize);
        var texts = ParagraphTexts(harness);
        Assert.Contains("Pick it", texts);
        Assert.Contains("Nope", texts);
        Assert.Contains("Yep", texts);
        Assert.Contains("Hrs", texts);
        Assert.Contains("Mins", texts);
    }

    [Fact]
    public void Separator_UsesTheFormatSpecificGlyphAndThemeOverrides()
    {
        Assert.Equal(":", TimeSelectorSeparator.SeparatorFor(TimeOfDayFormat.HColonMmSpaceA));
        Assert.Equal(":", TimeSelectorSeparator.SeparatorFor(TimeOfDayFormat.HHColonMm));
        Assert.Equal(".", TimeSelectorSeparator.SeparatorFor(TimeOfDayFormat.HHDotMm));
        Assert.Equal("h", TimeSelectorSeparator.SeparatorFor(TimeOfDayFormat.FrenchCanadian));

        var separatorColor = Color.Parse("#FF00FF00");
        using var harness = CreateHarness(new TimePickerTheme(
            new TimePickerThemeData(
                TimeSelectorSeparatorColor: MaterialStateProperty<Color?>.All(separatorColor),
                TimeSelectorSeparatorTextStyle: MaterialStateProperty<TextStyle?>.All(
                    new TextStyle(FontSize: 35))),
            DialogRoute(new TimePickerDialog(new TimeOfDay(7, 0)))));
        harness.Pump(ViewSize);
        var separator = Assert.Single(
            FindDescendants<RenderParagraph>(harness.RenderView).Where(p => p.PlainText == ":"));
        Assert.Equal(separatorColor, separator.Text.Style!.Color);
        Assert.Equal(35, separator.FontSize);
    }

    // ---- Input mode ---------------------------------------------------------------------

    [Fact]
    public void InputMode_PrefillsControllersUnlessEmptyInitialInputIsSet()
    {
        using var filled = CreateHarness(DialogRoute(new TimePickerDialog(
            new TimeOfDay(7, 0),
            initialEntryMode: TimePickerEntryMode.Input)));
        filled.Pump(ViewSize);
        Assert.Contains(ParagraphTexts(filled), text => text == "7");
        Assert.Contains(ParagraphTexts(filled), text => text == "00");

        using var empty = CreateHarness(DialogRoute(new TimePickerDialog(
            new TimeOfDay(7, 0),
            initialEntryMode: TimePickerEntryMode.Input,
            emptyInitialInput: true)));
        empty.Pump(ViewSize);
        Assert.DoesNotContain(ParagraphTexts(empty), text => text == "7");
    }

    [Fact]
    public void InputMode_ExposesTextFieldSemanticsForHourAndMinute()
    {
        using var harness = CreateHarness(DialogRoute(new TimePickerDialog(
            new TimeOfDay(7, 0),
            initialEntryMode: TimePickerEntryMode.Input)));
        var semantics = harness.PumpAndGetSemantics(ViewSize);
        Assert.NotNull(FindSemantics(semantics, node => HasLabelPart(node, "Hour")));
        Assert.NotNull(FindSemantics(semantics, node => HasLabelPart(node, "Minute")));
    }

    [Fact]
    public async Task InputMode_RejectsInvalidTextThenReturnsTheEnteredTime()
    {
        BuildContext captured = default;
        using var harness = CreateHarness(new Navigator(new BuilderPageRoute(context => new CaptureContext(
            value => captured = value,
            new Text("Home")))));
        harness.Pump(ViewSize);
        var result = MaterialTimePickers.ShowTimePicker(
            captured,
            initialTime: new TimeOfDay(9, 30),
            initialEntryMode: TimePickerEntryMode.Input,
            errorInvalidText: "Bad time");
        PumpAnimation();
        harness.Pump(ViewSize);

        Assert.True(FocusManager.Instance.FocusNext());
        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.KeyA, control: true)));
        Assert.True(FocusManager.Instance.HandleTextInput("99"));
        var semantics = harness.PumpAndGetSemantics(ViewSize);
        var ok = FindSemantics(semantics, node => node.Actions.HasFlag(SemanticsActions.Tap) && ContainsLabel(node, "OK"));
        Assert.NotNull(ok);
        Assert.True(ok!.PerformAction(SemanticsActions.Tap));
        harness.Pump(ViewSize);
        Assert.False(result.IsCompleted);
        Assert.Contains(ParagraphTexts(harness), text => text == "Bad time");

        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.KeyA, control: true)));
        // A complete, valid hour advances focus to the minute field on its own.
        Assert.True(FocusManager.Instance.HandleTextInput("11"));
        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.KeyA, control: true)));
        Assert.True(FocusManager.Instance.HandleTextInput("45"));
        semantics = harness.PumpAndGetSemantics(ViewSize);
        ok = FindSemantics(semantics, node => node.Actions.HasFlag(SemanticsActions.Tap) && ContainsLabel(node, "OK"));
        Assert.True(ok!.PerformAction(SemanticsActions.Tap));
        PumpAnimation();
        harness.Pump(ViewSize);
        Assert.Equal(new TimeOfDay(11, 45), await result);
    }

    [Fact]
    public async Task InputMode_UnmodifiedFieldsReturnTheInitialTime()
    {
        BuildContext captured = default;
        using var harness = CreateHarness(new Navigator(new BuilderPageRoute(context => new CaptureContext(
            value => captured = value,
            new Text("Home")))));
        harness.Pump(ViewSize);
        var result = MaterialTimePickers.ShowTimePicker(
            captured,
            initialTime: new TimeOfDay(7, 0),
            initialEntryMode: TimePickerEntryMode.Input);
        PumpAnimation();
        var semantics = harness.PumpAndGetSemantics(ViewSize);
        var ok = FindSemantics(semantics, node => node.Actions.HasFlag(SemanticsActions.Tap) && ContainsLabel(node, "OK"));
        Assert.True(ok!.PerformAction(SemanticsActions.Tap));
        PumpAnimation();
        harness.Pump(ViewSize);
        Assert.Equal(new TimeOfDay(7, 0), await result);
    }

    [Fact]
    public async Task Cancel_PopsWithoutAResult()
    {
        BuildContext captured = default;
        using var harness = CreateHarness(new Navigator(new BuilderPageRoute(context => new CaptureContext(
            value => captured = value,
            new Text("Home")))));
        harness.Pump(ViewSize);
        var result = MaterialTimePickers.ShowTimePicker(captured, initialTime: new TimeOfDay(7, 0));
        PumpAnimation();
        var semantics = harness.PumpAndGetSemantics(ViewSize);
        var cancel = FindSemantics(
            semantics,
            node => node.Actions.HasFlag(SemanticsActions.Tap) && ContainsLabel(node, "Cancel"));
        Assert.NotNull(cancel);
        Assert.True(cancel!.PerformAction(SemanticsActions.Tap));
        PumpAnimation();
        harness.Pump(ViewSize);
        Assert.Null(await result);
    }

    // ---- Dial interaction ---------------------------------------------------------------

    [Theory]
    [InlineData(0, -50, 12)]
    [InlineData(50, 0, 3)]
    [InlineData(0, 50, 6)]
    [InlineData(-50, 0, 9)]
    public void Dial_TapSelectsTheHourUnderThePointer(double dx, double dy, int expectedHourOfPeriod)
    {
        using var harness = CreateHarness(DialogRoute(new TimePickerDialog(new TimeOfDay(7, 0))));
        harness.Pump(ViewSize);
        TapDial(harness, dx, dy);
        harness.Pump(ViewSize);
        var semantics = harness.PumpAndGetSemantics(ViewSize);
        Assert.NotNull(FindSemantics(
            semantics,
            node => node.Value == $"Select hours {expectedHourOfPeriod}"));
    }

    [Fact]
    public void Dial_TapSwitchesFromHourToMinuteAndRoundsToFiveMinutes()
    {
        using var harness = CreateHarness(DialogRoute(new TimePickerDialog(new TimeOfDay(7, 0))));
        harness.Pump(ViewSize);
        TapDial(harness, 0, 50);
        harness.Pump(ViewSize);
        var semantics = harness.PumpAndGetSemantics(ViewSize);
        Assert.NotNull(FindSemantics(semantics, node => node.Value == "Select hours 6"));

        // The dial has switched to minutes: an off-increment position snaps to the nearest 5.
        TapDial(harness, -50, -5);
        harness.Pump(ViewSize);
        semantics = harness.PumpAndGetSemantics(ViewSize);
        Assert.NotNull(FindSemantics(semantics, node => node.Value == "Select minutes 45"));
    }

    [Fact]
    public void Dial_DragSelectsAnHourWithoutRoundingMinutes()
    {
        using var harness = CreateHarness(DialogRoute(new TimePickerDialog(new TimeOfDay(7, 0))));
        harness.Pump(ViewSize);
        DragDial(harness, new Point(0, 50), new Point(-50, 0));
        harness.Pump(ViewSize);
        var semantics = harness.PumpAndGetSemantics(ViewSize);
        Assert.NotNull(FindSemantics(semantics, node => node.Value == "Select hours 9"));
    }

    [Fact]
    public void Dial_ThemeOverridesFeedThePainterColors()
    {
        var dialBackground = Color.Parse("#FF123456");
        var hand = Color.Parse("#FFABCDEF");
        using var harness = CreateHarness(new TimePickerTheme(
            new TimePickerThemeData(DialBackgroundColor: dialBackground, DialHandColor: hand),
            DialogRoute(new TimePickerDialog(new TimeOfDay(10, 30)))));
        harness.Pump(ViewSize);
        var painter = FindPainter(harness);
        Assert.Equal(dialBackground, painter.BackgroundColor);
        Assert.Equal(hand, painter.HandColor);
    }

    [Fact]
    public void HourMinuteControl_UsesTheThemeShapeAndSelectedStateColors()
    {
        var selected = Color.Parse("#FF00AA00");
        var unselected = Color.Parse("#FF0000AA");
        using var harness = CreateHarness(new TimePickerTheme(
            new TimePickerThemeData(
                HourMinuteColor: WidgetStateColor.ResolveWith(
                    unselected,
                    states => states.Contains(WidgetState.Selected) ? selected : unselected),
                HourMinuteShape: new RoundedRectangleBorder(borderRadius: BorderRadius.Circular(16))),
            DialogRoute(new TimePickerDialog(new TimeOfDay(7, 0)))));
        harness.Pump(ViewSize);
        var materials = FindWidgets<Plumix.Material.Material>(harness);
        Assert.Contains(materials, material => material.Color == selected);
        Assert.Contains(materials, material => material.Color == unselected);
    }

    [Fact]
    public void EntryModeIconColor_IsPushedOntoTheIconButtonOnMaterial2Only()
    {
        using var m2 = CreateHarness(
            new TimePickerTheme(
                new TimePickerThemeData(EntryModeIconColor: Colors.Red),
                DialogRoute(new TimePickerDialog(new TimeOfDay(7, 0)))),
            theme: ThemeData.Light with { UseMaterial3 = false });
        m2.Pump(ViewSize);
        Assert.Contains(FindWidgets<IconButton>(m2), button => button.Color == Colors.Red);

        using var m3 = CreateHarness(new TimePickerTheme(
            new TimePickerThemeData(EntryModeIconColor: Colors.Red),
            DialogRoute(new TimePickerDialog(new TimeOfDay(7, 0)))));
        m3.Pump(ViewSize);
        Assert.All(FindWidgets<IconButton>(m3), button => Assert.Null(button.Color));
    }

    // ---- Helpers ------------------------------------------------------------------------

    private static Color WithOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Round(255 * opacity),
        color.R,
        color.G,
        color.B);

    private static Widget DialogRoute(Widget dialog) => new Navigator(new BuilderPageRoute(_ => dialog));

    private static void PumpAnimation()
    {
        double now = Scheduler.CurrentSeconds;
        AnimationPump.Prime();
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.01));
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.25));
    }

    private static TimeDialPainter FindPainter(WidgetRenderHarness harness) => Assert.IsType<TimeDialPainter>(
        FindDescendants<RenderCustomPaint>(harness.RenderView)
            .Select(paint => paint.Painter)
            .OfType<TimeDialPainter>()
            .First());

    private static void TapDial(WidgetRenderHarness harness, double dx, double dy)
    {
        var point = DialPoint(harness, dx, dy);
        var binding = GestureBinding.Instance;
        DispatchPointerDown(binding, harness.RenderView, 1, point);
        DispatchPointerUp(binding, harness.RenderView, 1, point);
    }

    private static void DragDial(WidgetRenderHarness harness, Point start, Point end)
    {
        var from = DialPoint(harness, start.X, start.Y);
        var to = DialPoint(harness, end.X, end.Y);
        var binding = GestureBinding.Instance;
        DispatchPointerDown(binding, harness.RenderView, 2, from);
        for (int step = 1; step <= 4; step++)
        {
            DispatchPointerMove(binding, harness.RenderView, 2, new Point(
                from.X + ((to.X - from.X) * step / 4.0),
                from.Y + ((to.Y - from.Y) * step / 4.0)));
        }

        DispatchPointerUp(binding, harness.RenderView, 2, to);
    }

    private static Point DialPoint(WidgetRenderHarness harness, double dx, double dy)
    {
        var dial = FindDescendants<RenderCustomPaint>(harness.RenderView)
            .First(paint => paint.Painter is TimeDialPainter);
        var origin = dial.GetPaintOffsetToRoot();
        return new Point(origin.X + (dial.Size.Width / 2) + dx, origin.Y + (dial.Size.Height / 2) + dy);
    }

    private static void DispatchPointerDown(GestureBinding binding, RenderView view, int pointer, Point position) =>
        binding.HandlePointerEvent(view, new PointerDownEvent(
            pointer: pointer,
            kind: PointerDeviceKind.Touch,
            position: position,
            buttons: PointerButtons.Primary,
            timestampUtc: DateTime.UtcNow));

    private static void DispatchPointerMove(GestureBinding binding, RenderView view, int pointer, Point position) =>
        binding.HandlePointerEvent(view, new PointerMoveEvent(
            pointer: pointer,
            kind: PointerDeviceKind.Touch,
            position: position,
            buttons: PointerButtons.Primary,
            down: true,
            timestampUtc: DateTime.UtcNow));

    private static void DispatchPointerUp(GestureBinding binding, RenderView view, int pointer, Point position) =>
        binding.HandlePointerEvent(view, new PointerUpEvent(
            pointer: pointer,
            kind: PointerDeviceKind.Touch,
            position: position,
            buttons: PointerButtons.None,
            timestampUtc: DateTime.UtcNow));

    private static List<string> ParagraphTexts(WidgetRenderHarness harness) =>
        FindDescendants<RenderParagraph>(harness.RenderView)
            .Select(paragraph => paragraph.PlainText)
            .ToList();

    private static WidgetRenderHarness CreateHarness(
        Widget child,
        ThemeData? theme = null,
        Size? mediaSize = null,
        bool alwaysUse24HourFormat = false) => new(
        new MediaQuery(
            new MediaQueryData(
                Size: mediaSize ?? ViewSize,
                AlwaysUse24HourFormat: alwaysUse24HourFormat),
            new Directionality(TextDirection.Ltr, new Theme(theme ?? ThemeData.Light, child))));

    private static bool Close(double a, double b) => Math.Abs(a - b) < 0.001;

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null) return result;
        if (root is T target) result.Add(target);
        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private static List<T> FindWidgets<T>(WidgetRenderHarness harness) where T : Widget
    {
        var result = new List<T>();
        void Visit(Element element)
        {
            if (element.Widget is T target) result.Add(target);
            element.VisitChildren(Visit);
        }

        harness.VisitRoot(Visit);
        return result;
    }

    private static T FindWidget<T>(WidgetRenderHarness harness) where T : Widget =>
        FindWidgets<T>(harness).First();

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

    private static SemanticsNode? FindTappableUnderTooltip(SemanticsNode? root, string tooltip)
    {
        var node = FindSemantics(root, candidate => candidate.Tooltip == tooltip);
        return node is null ? null : FindSemantics(node, candidate => candidate.Actions.HasFlag(SemanticsActions.Tap));
    }

    private static bool ContainsLabel(SemanticsNode node, string label)
    {
        if (HasLabelPart(node, label)) return true;
        return node.Children.Any(child => ContainsLabel(child, label));
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

        public void VisitRoot(Action<Element> visitor) => _rootElement.VisitChildren(visitor);

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
            return _pipeline.SemanticsOwner!.RootNode;
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

