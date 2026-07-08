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
            node.Label == "Select year" && node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(toggle);
        Assert.True(toggle!.PerformAction(SemanticsActions.Tap));

        semantics = harness.PumpAndGetSemantics(new Size(420, 500));
        var year = FindSemantics(semantics, node =>
            node.Label == "2028" && node.Actions.HasFlag(SemanticsActions.Tap));
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

        var now = Scheduler.CurrentSeconds;
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

        for (var index = 0; index < 12 && FocusManager.Instance.PrimaryFocus?.OnKeyEvent?.Method.Name != "HandleGridKey"; index++)
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

        var first = FindSemantics(semantics, node => node.Label == "2024" && node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.NotNull(first);
        Assert.True(first!.PerformAction(SemanticsActions.Tap));
        Assert.Equal(new DateTime(2024, 11, 1), selected);

        var last = FindSemantics(semantics, node => node.Label == "2026" && node.Actions.HasFlag(SemanticsActions.Tap));
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
    public void CalendarDatePickerM2UsesFortyTwoPixelRows()
    {
        using var harness = CreateHarness(BuildPicker(), ThemeData.Light with { UseMaterial3 = false });
        harness.Pump(new Size(420, 500));
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView), box =>
            Close(box.AdditionalConstraints.MinHeight, 346) && Close(box.AdditionalConstraints.MaxHeight, 346));
    }

    private static CalendarDatePicker BuildPicker() => new(
        initialDate: new DateTime(2026, 7, 8),
        firstDate: new DateTime(2026, 1, 1),
        lastDate: new DateTime(2026, 12, 31),
        onDateChanged: _ => { });

    private static WidgetRenderHarness CreateHarness(Widget child, ThemeData? theme = null) => new(
        new MediaQuery(
            new MediaQueryData(Size: new Size(420, 500)),
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
}
