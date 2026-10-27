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

namespace Plumix.Tests;

// Dart parity source: material_ui/test/time_picker_test.dart

public sealed partial class MaterialTimePickerDialogTests
{
    [Fact]
    public void RestorableTimeOfDayStoresMinuteBeforeHourAndNotifiesOnlyOnChange()
    {
        var property = new RestorableTimeOfDay(new TimeOfDay(18, 45));
        var raw = RawRestorationData.Build();
        var manager = new MockRestorationManager();
        var root = RestorationBucket.Root(manager, raw);
        using var harness = CreateHarness(new UnmanagedRestorationScope(
            bucket: root, child: new TimePropertyProbe(property)));
        int notifications = 0;
        property.AddListener(() => notifications++);

        Assert.Equal(new TimeOfDay(18, 45), property.Value);
        Assert.Equal(new[] { 45, 18 }, ((IEnumerable)property.ToPrimitives()).Cast<int>());
        Assert.Equal(new TimeOfDay(9, 12), property.FromPrimitives(new object[] { 12, 9 }));
        property.Value = new TimeOfDay(18, 45);
        Assert.Equal(0, notifications);
        property.Value = new TimeOfDay(21, 12);
        Assert.Equal(1, notifications);
        Assert.Equal(new[] { 12, 21 }, ((IEnumerable)property.ToPrimitives()).Cast<int>());
    }

    // Flutter's "TimePickerDialog restoration" covers both restart and an older saved snapshot.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RestorationDialRetainsTimePeriodAndHourMinuteMode(bool useMaterial3)
    {
        var manager = new MockRestorationManager();
        var raw = RawRestorationData.Build();
        var root = RestorationBucket.Root(manager, raw);
        Dictionary<object, object?> earlier;
        Dictionary<object, object?> later;
        var theme = ThemeData.Light with { UseMaterial3 = useMaterial3 };
        using (var harness = CreateHarness(new UnmanagedRestorationScope(
            bucket: root,
            child: new TimePickerDialog(new TimeOfDay(6, 45), restorationId: "dialog")), theme: theme))
        {
            harness.Pump(ViewSize);
            manager.DoSerialization();
            earlier = RawRestorationData.Copy(raw);
            var model = FindWidget<TimePickerModel>(harness);
            model.OnSelectedTimeChanged(new TimeOfDay(18, 45));
            model.OnHourMinuteModeChanged(HourMinuteMode.Minute);
            harness.Pump(ViewSize);
            manager.DoSerialization();
            later = RawRestorationData.Copy(raw);
        }

        using (var restored = CreateHarness(new UnmanagedRestorationScope(
            bucket: RestorationBucket.Root(manager, later),
            child: new TimePickerDialog(new TimeOfDay(6, 45), restorationId: "dialog")), theme: theme))
        {
            restored.Pump(ViewSize);
            var model = FindWidget<TimePickerModel>(restored);
            Assert.Equal(new TimeOfDay(18, 45), model.SelectedTime);
            Assert.Equal(HourMinuteMode.Minute, model.HourMinuteMode);
            Assert.Equal(new[] { 45, 18 },
                ((IEnumerable)RawRestorationData.Values(RawRestorationData.Child(later, "dialog")!)!
                    ["selected_time"]!).Cast<int>());
        }

        using var previous = CreateHarness(new UnmanagedRestorationScope(
            bucket: RestorationBucket.Root(manager, earlier),
            child: new TimePickerDialog(new TimeOfDay(6, 45), restorationId: "dialog")), theme: theme);
        previous.Pump(ViewSize);
        Assert.Equal(new TimeOfDay(6, 45), FindWidget<TimePickerModel>(previous).SelectedTime);
        Assert.Equal(HourMinuteMode.Hour, FindWidget<TimePickerModel>(previous).HourMinuteMode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RestorationInputRetainsTextPeriodValidationAndEntryMode(bool useMaterial3)
    {
        var manager = new MockRestorationManager();
        var raw = RawRestorationData.Build();
        var root = RestorationBucket.Root(manager, raw);
        Dictionary<object, object?> saved;
        var theme = ThemeData.Light with { UseMaterial3 = useMaterial3 };
        using (var harness = CreateHarness(new UnmanagedRestorationScope(
            bucket: root,
            child: new TimePickerDialog(new TimeOfDay(21, 12), restorationId: "dialog")), theme: theme))
        {
            harness.Pump(ViewSize);
            FindWidget<TimePickerModel>(harness).OnMinuteDoubleTapped();
            harness.Pump(ViewSize);
            var fields = FindWidgets<TextFormField>(harness);
            Assert.Equal(new[] { "9", "12" }, fields.Select(field => field.Controller!.Text));
            fields[0].Controller!.Text = "88";
            Assert.Equal(string.Empty, fields[0].Validator!("88"));
            harness.Pump(ViewSize);
            manager.DoSerialization();
            saved = RawRestorationData.Copy(raw);
        }

        using var restored = CreateHarness(new UnmanagedRestorationScope(
            bucket: RestorationBucket.Root(manager, saved),
            child: new TimePickerDialog(new TimeOfDay(7, 0), restorationId: "dialog")), theme: theme);
        restored.Pump(ViewSize);
        Assert.Equal(TimePickerEntryMode.Input, FindWidget<TimePickerModel>(restored).EntryMode);
        Assert.Equal(new TimeOfDay(21, 12), FindWidget<TimePickerModel>(restored).SelectedTime);
        Assert.Equal(new[] { "88", "12" },
            FindWidgets<TextFormField>(restored).Select(field => field.Controller!.Text));
        Assert.Contains(ParagraphTexts(restored), text => text == "Enter a valid time");
        var hourData = RawRestorationData.Child(saved, "hour_text_field")!;
        Assert.True((bool)RawRestorationData.Values(hourData)!["has_controller_been_set"]!);
        Assert.NotNull(RawRestorationData.Child(hourData, "hour_minute_text_form_field"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InputUsesLengthFormatterWithoutACounterAndStillValidatesNondigits(bool useMaterial3)
    {
        using var harness = CreateHarness(DialogRoute(new TimePickerDialog(
            new TimeOfDay(7, 0), initialEntryMode: TimePickerEntryMode.Input)),
            theme: ThemeData.Light with { UseMaterial3 = useMaterial3 });
        harness.Pump(ViewSize);
        var fields = FindWidgets<TextFormField>(harness);
        Assert.All(fields, field =>
        {
            Assert.Null(field.MaxLength);
            Assert.Null(field.BuildCounter);
            Assert.Equal(2, Assert.IsType<LengthLimitingTextInputFormatter>(
                Assert.Single(field.InputFormatters!)).MaxLength);
        });
        Assert.True(FocusManager.Instance.FocusNext());
        EventLoop.FlushMicrotasks();
        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.KeyA, control: true)));
        Assert.True(HostTextInput.InsertText("ABC"));
        harness.Pump(ViewSize);
        Assert.Equal("AB", fields[0].Controller!.Text);
        Assert.Equal(string.Empty, fields[0].Validator!("AB"));
    }

    [Theory]
    [InlineData("+1")]
    [InlineData(" 1")]
    [InlineData("1 ")]
    public void InputParsingAcceptsDartIntegerWhitespaceAndSigns(string input)
    {
        using var harness = CreateHarness(DialogRoute(new TimePickerDialog(
            new TimeOfDay(7, 0), initialEntryMode: TimePickerEntryMode.Input)));
        harness.Pump(ViewSize);
        Assert.Null(FindWidgets<TextFormField>(harness)[0].Validator!(input));
        Assert.Null(FindWidgets<TextFormField>(harness)[1].Validator!(input));
    }

    [Fact]
    public void WebFocusSelectsTheWholeHourAndMinute()
    {
        PlatformDefaults.DebugIsWebOverride = true;
        using var harness = CreateHarness(DialogRoute(new TimePickerDialog(
            new TimeOfDay(10, 45), initialEntryMode: TimePickerEntryMode.Input)));
        harness.Pump(ViewSize);
        var fields = FindWidgets<TextFormField>(harness);
        fields[0].FocusNode!.RequestFocus();
        EventLoop.FlushMicrotasks();
        Assert.Equal(new TextSelection(0, 2), fields[0].Controller!.Selection);
        fields[1].FocusNode!.RequestFocus();
        EventLoop.FlushMicrotasks();
        Assert.Equal(new TextSelection(0, 2), fields[1].Controller!.Selection);
    }

    [Fact]
    public void HourSubmissionKeepsScopeFocusAndMinuteSubmissionUnfocusesTheScope()
    {
        using var harness = CreateHarness(DialogRoute(new TimePickerDialog(
            new TimeOfDay(7, 0), initialEntryMode: TimePickerEntryMode.Input)));
        harness.Pump(ViewSize);
        var fields = FindWidgets<TextFormField>(harness);
        fields[0].FocusNode!.RequestFocus();
        EventLoop.FlushMicrotasks();
        fields[0].OnFieldSubmitted!("9");
        EventLoop.FlushMicrotasks();
        Assert.True(fields[0].FocusNode!.HasFocus);
        fields[1].FocusNode!.RequestFocus();
        EventLoop.FlushMicrotasks();
        fields[1].OnFieldSubmitted!("45");
        EventLoop.FlushMicrotasks();
        Assert.False(fields[1].FocusNode!.HasFocus);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ThemeResolvesMappedHourPeriodAndFieldStylesBeforeCopying(bool inputMode)
    {
        var hourStyle = WidgetStateTextStyle.FromMap(
        [
            new(WidgetState.Selected, new TextStyle(FontSize: 31)),
            new(WidgetStatesConstraint.Any, new TextStyle(FontSize: 23)),
        ]);
        var periodStyle = WidgetStateTextStyle.FromMap(
        [
            new(WidgetState.Selected, new TextStyle(FontSize: 19)),
            new(WidgetStatesConstraint.Any, new TextStyle(FontSize: 17)),
        ]);
        using var harness = CreateHarness(new TimePickerTheme(
            new TimePickerThemeData(HourMinuteTextStyle: hourStyle, DayPeriodTextStyle: periodStyle),
            DialogRoute(new TimePickerDialog(new TimeOfDay(7, 0),
                initialEntryMode: inputMode ? TimePickerEntryMode.Input : TimePickerEntryMode.Dial))));
        harness.Pump(ViewSize);
        Assert.Contains(FindWidgets<Text>(harness), text => text.Data == "AM" && text.Style?.FontSize == 19);
        Assert.Contains(FindWidgets<Text>(harness), text => text.Data == "PM" && text.Style?.FontSize == 17);
        if (inputMode)
        {
            var fields = FindWidgets<TextFormField>(harness);
            Assert.Equal(23, fields[0].Style!.FontSize);
            fields[0].FocusNode!.RequestFocus();
            EventLoop.FlushMicrotasks();
            harness.Pump(ViewSize);
            Assert.Equal(31, FindWidgets<TextFormField>(harness)[0].Style!.FontSize);
        }
        else
        {
            Assert.Contains(FindWidgets<Text>(harness), text => text.Data == "7" && text.Style?.FontSize == 31);
            Assert.Contains(FindWidgets<Text>(harness), text => text.Data == "00" && text.Style?.FontSize == 23);
        }
    }

    [Fact]
    public void SeparatorUsesExplicitHourMinuteThemeBeforeMaterial3Defaults()
    {
        using var harness = CreateHarness(new TimePickerTheme(
            new TimePickerThemeData(
                HourMinuteTextColor: Colors.Purple,
                HourMinuteTextStyle: new TextStyle(FontSize: 35)),
            DialogRoute(new TimePickerDialog(new TimeOfDay(7, 0)))));
        harness.Pump(ViewSize);
        var separator = FindWidgets<Text>(harness).Single(text => text.Data == ":");
        Assert.Equal(Colors.Purple, separator.Style!.Color);
        Assert.Equal(35, separator.Style.FontSize);
    }

    [Fact]
    public void NonlinearTextScalingUsesFourteenPointDialogScaleAndPeriodScale()
    {
        using var harness = CreateHarness(DialogRoute(new TimePickerDialog(new TimeOfDay(7, 0))),
            textScaler: new NonlinearPickerScaler());
        harness.Pump(ViewSize);
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView), box =>
            Close(box.AdditionalConstraints.MaxHeight, 468 * 1.05));
        var am = FindWidgets<Text>(harness).Single(text => text.Data == "AM");
        Assert.Equal(16 * 1.4, am.TextScaler!.Scale(16), precision: 5);
    }

    [Fact]
    public void PortraitHeaderAccountsForBothFortyEightPointPeriodTargets()
    {
        using var harness = CreateHarness(DialogRoute(new TimePickerDialog(new TimeOfDay(7, 0))));
        harness.Pump(ViewSize);
        Assert.Contains(FindWidgets<Padding>(harness), padding =>
            padding.Child is Text { Data: "Select time" }
            && padding.Insets.Resolve(TextDirection.Ltr).Bottom == 12);
    }

    [Fact]
    public void DialConvertsGlobalTapThroughTheRenderTransform()
    {
        using var harness = CreateHarness(DialogRoute(new Transform(
            Matrix4.Diagonal3Values(1.5, 1.5, 1),
            new TimePickerDialog(new TimeOfDay(7, 0)),
            alignment: Alignment.TopLeft)));
        harness.Pump(ViewSize);
        var dial = FindDescendants<RenderCustomPaint>(harness.RenderView)
            .Single(paint => paint.Painter is TimeDialPainter);
        Point point = dial.LocalToGlobal(new Point(dial.Size.Width / 2 + 50, dial.Size.Height / 2));
        DispatchPointerDown(GestureBinding.Instance, harness.RenderView, 1, point);
        DispatchPointerUp(GestureBinding.Instance, harness.RenderView, 1, point);
        harness.Pump(ViewSize);
        Assert.Equal(3, FindWidget<TimePickerModel>(harness).SelectedTime.HourOfPeriod);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShowTimePickerForwardsAnchoringBarrierAndNavigatorOptions(bool useRootNavigator)
    {
        BuildContext captured = null!;
        var rootObserver = new PickerRouteObserver();
        var innerObserver = new PickerRouteObserver();
        using var harness = CreateHarness(new Navigator(
            new BuilderPageRoute(_ => new Navigator(
                new BuilderPageRoute(_ => new CaptureContext(context => captured = context, new Text("Home"))),
                observers: [innerObserver])),
            observers: [rootObserver]));
        harness.Pump(ViewSize);
        var settings = new RouteSettings("time-picker");
        var anchor = new Point(400, 100);
        _ = MaterialTimePickers.ShowTimePicker(
            captured,
            new TimeOfDay(7, 0),
            barrierDismissible: false,
            barrierColor: Colors.Purple,
            barrierLabel: "Time barrier",
            useRootNavigator: useRootNavigator,
            routeSettings: settings,
            anchorPoint: anchor);
        PumpAnimation();
        harness.Pump(ViewSize);
        var route = Assert.IsType<DialogRoute<TimeOfDay?>>(
            (useRootNavigator ? rootObserver : innerObserver).LastPushed);
        Assert.Equal(anchor, route.AnchorPoint);
        Assert.False(route.BarrierDismissible);
        Assert.Equal(Colors.Purple, route.BarrierColor);
        Assert.Equal("Time barrier", route.BarrierLabel);
        Assert.Same(settings, route.Settings);
        Assert.IsNotType<DialogRoute<TimeOfDay?>>(
            (useRootNavigator ? innerObserver : rootObserver).LastPushed);
    }

    [Theory]
    [InlineData(TargetPlatform.Android, true)]
    [InlineData(TargetPlatform.Windows, true)]
    [InlineData(TargetPlatform.IOS, false)]
    [InlineData(TargetPlatform.MacOS, false)]
    public void DialVibrationDebouncesAndCancelsWhenDisposed(TargetPlatform platform, bool vibrates)
    {
        using var timers = new FakeGestureTimers();
        int vibrations = 0;
        SystemChannels.Platform.SetPlatformMethodCallHandler(call =>
        {
            if (call.Method == "HapticFeedback.vibrate") vibrations++;
            return Task.FromResult<object?>(null);
        });
        try
        {
            using (var harness = CreateHarness(DialogRoute(new TimePickerDialog(new TimeOfDay(7, 0))),
                theme: ThemeData.Light with { Platform = platform }))
            {
                harness.Pump(ViewSize);
                var model = FindWidget<TimePickerModel>(harness);
                model.OnSelectedTimeChanged(new TimeOfDay(8, 0));
                model.OnSelectedTimeChanged(new TimeOfDay(9, 0));
                model.OnHourMinuteModeChanged(HourMinuteMode.Minute);
                timers.Elapse(TimeSpan.FromMilliseconds(99));
                Assert.Equal(0, vibrations);
                timers.Elapse(TimeSpan.FromMilliseconds(1));
                Assert.Equal(vibrates ? 1 : 0, vibrations);
                model.OnSelectedTimeChanged(new TimeOfDay(9, 15));
            }

            timers.Elapse(TimeSpan.FromSeconds(1));
            Assert.Equal(vibrates ? 1 : 0, vibrations);
        }
        finally
        {
            SystemChannels.Platform.SetPlatformMethodCallHandler(null);
        }
    }

    [Fact]
    public async Task SystemFontsChannelRepaintsAnAttachedDialAndStopsAfterDetach()
    {
        var harness = CreateHarness(DialogRoute(new TimePickerDialog(new TimeOfDay(7, 0))));
        try
        {
            harness.Pump(ViewSize);
            var dial = FindDescendants<RenderCustomPaint>(harness.RenderView)
                .Single(paint => paint.Painter is TimeDialPainter);
            Assert.False(dial.DebugNeedsPaint);
            var messenger = ServicesBinding.Instance.DefaultBinaryMessenger;
            await messenger.HandlePlatformMessage(SystemChannels.System.Name,
                SystemChannels.System.Codec.EncodeMessage(
                    new Dictionary<string, object?> { ["type"] = "fontsChange" }),
                callback: null);
            if (Constants.KDebugMode) Assert.True(dial.DebugNeedsPaint);
            harness.Pump(ViewSize);
        }
        finally
        {
            harness.Dispose();
        }

        // Listener removal on detach must make another message safe after the painter is disposed.
        await PaintingBinding.Instance.HandleSystemMessage(
            new Dictionary<string, object?> { ["type"] = "fontsChange" });
    }

    private sealed record NonlinearPickerScaler : TextScaler
    {
        public override double Scale(double fontSize) => fontSize * (fontSize == 14 ? 1.05 : 1.4);
        public override double TextScaleFactor => 1.0;
    }

    private sealed class PickerRouteObserver : NavigatorObserver
    {
        public Route? LastPushed { get; private set; }
        public override void DidPush(Route route, Route? previousRoute) => LastPushed = route;
    }

    private sealed class TimePropertyProbe(RestorableTimeOfDay property) : StatefulWidget
    {
        public RestorableTimeOfDay Property { get; } = property;
        public override State CreateState() => new ProbeState();

        private sealed class ProbeState : RestorationState<TimePropertyProbe>
        {
            protected override string? RestorationId => "time";
            protected override void RestoreState(RestorationBucket? oldBucket, bool initialRestore) =>
                RegisterForRestoration(Widget.Property, "value");
            public override Widget Build(BuildContext context) => new SizedBox();
        }
    }
}
