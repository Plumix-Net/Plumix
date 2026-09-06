using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: cupertino_ui/test/localizations_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoLocalizationsTests : IDisposable
{
    public CupertinoLocalizationsTests()
    {
        Scheduler.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
    }

    [Fact]
    public void EnglishTranslationsMatchFlutterDefaults()
    {
        var localizations = new DefaultCupertinoLocalizations();

        Assert.Equal("2018", localizations.DatePickerYear(2018));
        Assert.Equal("January", localizations.DatePickerMonth(1));
        Assert.Equal("December", localizations.DatePickerStandaloneMonth(12));
        Assert.Equal("1", localizations.DatePickerDayOfMonth(1));
        Assert.Equal(" Mon 1 ", localizations.DatePickerDayOfMonth(1, 1));
        Assert.Equal("Thu Oct 4 ", localizations.DatePickerMediumDate(new DateTime(2018, 10, 4)));
        Assert.Equal("0", localizations.DatePickerHour(0));
        Assert.Equal("0 o'clock", localizations.DatePickerHourSemanticsLabel(0));
        Assert.Equal("01", localizations.DatePickerMinute(1));
        Assert.Equal("0 minutes", localizations.DatePickerMinuteSemanticsLabel(0));
        Assert.Equal("1 minute", localizations.DatePickerMinuteSemanticsLabel(1));
        Assert.Equal(DatePickerDateOrder.Mdy, localizations.DatePickerDateOrder);
        Assert.Equal(DatePickerDateTimeOrder.DateTimeDayPeriod, localizations.DatePickerDateTimeOrder);
        Assert.Equal("AM", localizations.AnteMeridiemAbbreviation);
        Assert.Equal("PM", localizations.PostMeridiemAbbreviation);
        Assert.Equal("Today", localizations.TodayLabel);
        Assert.Equal("Alert", localizations.AlertDialogLabel);
        Assert.Equal("Tab 1 of 3", localizations.TabSemanticsLabel(1, 3));

        Assert.Equal("2", localizations.TimerPickerHour(2));
        Assert.Equal("3", localizations.TimerPickerMinute(3));
        Assert.Equal("4", localizations.TimerPickerSecond(4));
        Assert.Equal("hour", localizations.TimerPickerHourLabel(1));
        Assert.Equal("hours", localizations.TimerPickerHourLabel(2));
        Assert.Equal(["hour", "hours"], localizations.TimerPickerHourLabels);
        Assert.Equal("min.", localizations.TimerPickerMinuteLabel(1));
        Assert.Equal(["min."], localizations.TimerPickerMinuteLabels);
        Assert.Equal("sec.", localizations.TimerPickerSecondLabel(1));
        Assert.Equal(["sec."], localizations.TimerPickerSecondLabels);

        Assert.Equal("Cut", localizations.CutButtonLabel);
        Assert.Equal("Copy", localizations.CopyButtonLabel);
        Assert.Equal("Paste", localizations.PasteButtonLabel);
        Assert.Equal("Clear", localizations.ClearButtonLabel);
        Assert.Equal("No Replacements Found", localizations.NoSpellCheckReplacementsLabel);
        Assert.Equal("Select All", localizations.SelectAllButtonLabel);
        Assert.Equal("Look Up", localizations.LookUpButtonLabel);
        Assert.Equal("Search Web", localizations.SearchWebButtonLabel);
        Assert.Equal("Share...", localizations.ShareButtonLabel);
        Assert.Equal("Search", localizations.SearchTextFieldPlaceholderLabel);
        Assert.Equal("Dismiss", localizations.ModalBarrierDismissLabel);
        Assert.Equal("Dismiss menu", localizations.MenuDismissLabel);
        Assert.Equal("Cancel", localizations.CancelButtonLabel);
        Assert.Equal("Back", localizations.BackButtonLabel);
        Assert.Equal("double tap to collapse", localizations.ExpansionTileExpandedHint);
        Assert.Equal("double tap to expand", localizations.ExpansionTileCollapsedHint);
        Assert.Equal("Collapse", localizations.ExpansionTileExpandedTapHint);
        Assert.Equal("Expand for more details", localizations.ExpansionTileCollapsedTapHint);
        Assert.Equal("Collapsed", localizations.ExpandedHint);
        Assert.Equal("Expanded", localizations.CollapsedHint);
    }

    [Fact]
    public void OrderingEnumsAndTabValidationMatchFlutterContracts()
    {
        Assert.Equal(
            [
                DatePickerDateTimeOrder.DateTimeDayPeriod,
                DatePickerDateTimeOrder.DateDayPeriodTime,
                DatePickerDateTimeOrder.TimeDayPeriodDate,
                DatePickerDateTimeOrder.DayPeriodTimeDate,
            ],
            Enum.GetValues<DatePickerDateTimeOrder>());
        Assert.Equal(
            [
                DatePickerDateOrder.Dmy,
                DatePickerDateOrder.Mdy,
                DatePickerDateOrder.Ymd,
                DatePickerDateOrder.Ydm,
            ],
            Enum.GetValues<DatePickerDateOrder>());
        var localizations = new DefaultCupertinoLocalizations();
        Assert.Throws<ArgumentOutOfRangeException>(() => localizations.TabSemanticsLabel(0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => localizations.TabSemanticsLabel(1, 0));
    }

    [Fact]
    public void DelegateSupportsOnlyEnglishAndLoadsTheDefaultResource()
    {
        LocalizationsDelegate<CupertinoLocalizations> localizationsDelegate =
            DefaultCupertinoLocalizations.Delegate;

        Assert.True(localizationsDelegate.IsSupported(new Locale("en", "US")));
        Assert.False(localizationsDelegate.IsSupported(new Locale("fr", "FR")));
        Assert.Same(
            DefaultCupertinoLocalizations.Instance,
            localizationsDelegate.LoadTyped(new Locale("en", "US")));
        Assert.False(localizationsDelegate.ShouldReload(localizationsDelegate));
        Assert.Equal("DefaultCupertinoLocalizations.delegate(en_US)", localizationsDelegate.ToString());
        Assert.Equal("Custom clear", new CustomCupertinoLocalizations().ClearButtonLabel);
    }

    [Fact]
    public void OfRequiresAndReturnsTheNearestCupertinoLocalizationResource()
    {
        Exception? missingException = null;
        var missingOwner = new BuildOwner();
        var missingRoot = new TestRootElement(new Builder(context =>
        {
            missingException = Record.Exception(() => CupertinoLocalizations.Of(context));
            return new SizedBox();
        }));

        MountAndFlush(missingRoot, missingOwner);

        var invalidOperation = Assert.IsType<InvalidOperationException>(missingException);
        Assert.Contains("No CupertinoLocalizations found", invalidOperation.Message);
        missingRoot.Unmount();

        CupertinoLocalizations? resolved = null;
        var localizedOwner = new BuildOwner();
        var localizedRoot = new TestRootElement(new Localizations(
            locale: new Locale("en", "US"),
            delegates:
            [
                DefaultCupertinoLocalizations.Delegate,
                DefaultWidgetsLocalizations.Delegate,
            ],
            child: new Builder(context =>
            {
                resolved = CupertinoLocalizations.Of(context);
                return new SizedBox();
            })));

        MountAndFlush(localizedRoot, localizedOwner);

        Assert.Same(DefaultCupertinoLocalizations.Instance, resolved);
        localizedRoot.Unmount();
    }

    [Fact]
    public void GlobalDelegatesResolveBothResourcesInAWidgetTree()
    {
        CupertinoLocalizations? cupertino = null;
        WidgetsLocalizations? widgets = null;
        var owner = new BuildOwner();
        var root = new TestRootElement(new Localizations(
            locale: new Locale("ar"),
            delegates: GlobalCupertinoLocalizations.Delegates,
            child: new Builder(context =>
            {
                cupertino = CupertinoLocalizations.Of(context);
                widgets = WidgetsLocalizations.Of(context);
                return new SizedBox();
            })));

        MountAndFlush(root, owner);

        Assert.IsType<CupertinoLocalizationAr>(cupertino);
        Assert.Equal(TextDirection.Rtl, Assert.IsType<WidgetsLocalizationAr>(widgets).TextDirection);
        root.Unmount();
    }

    private static void MountAndFlush(TestRootElement root, BuildOwner owner)
    {
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
    }

    private sealed class CustomCupertinoLocalizations : DefaultCupertinoLocalizations
    {
        public override string ClearButtonLabel => "Custom clear";
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        protected override void PerformRebuild()
        {
            base.PerformRebuild();
            _child = UpdateChild(_child, Widget, Slot);
        }

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild(force: true);
        }

        public override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        public override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
        }

        public override void Unmount()
        {
            if (_child != null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }
    }
}
