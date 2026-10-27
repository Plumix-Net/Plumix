using System.Threading.Tasks.Sources;
using Avalonia;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/test/widgets/localizations_test.dart
// Async resource loading, delegate selection/reload contracts and application locale reporting.
[Collection(SchedulerTestCollection.Name)]
public sealed class LocalizationsDartParityTests : IDisposable
{
    private readonly Locale? _previousApplicationLocale = PlatformDispatcher.Instance.ApplicationLocale;

    // Root attachment and warm-up frames are Timer.run turns on a fake clock the test drives.
    private readonly FakeGestureTimers _turns = new();

    public LocalizationsDartParityTests()
    {
        SchedulerBinding.ResetForTests();
        RendererBinding.Instance.ResetFirstFrameSent();
        PlatformDispatcher.Instance.ResetApplicationLocaleForTests(null);
    }

    public void Dispose()
    {
        EventLoop.FlushMicrotasks();
        _turns.Dispose();
        PlatformDispatcher.Instance.ResetApplicationLocaleForTests(_previousApplicationLocale);
        SchedulerBinding.ResetForTests();
        RendererBinding.Instance.ResetFirstFrameSent();
    }

    [Fact]
    public void InitialAsyncLoad_HidesChildUntilAllResourcesCommit_ThenAllowsFirstFrame()
    {
        using var tester = CreateTester();
        var strings = new PendingDelegate<string>();
        var numbers = new PendingDelegate<List<int>>();
        tester.PumpWidget(Localized(new Locale("fo"), [strings, numbers], new Text("loaded")));

        Assert.Empty(tester.ElementsWithText("loaded"));
        Assert.False(RendererBinding.Instance.SendFramesToEngine);
        Assert.Null(PlatformDispatcher.Instance.ApplicationLocale);

        strings.Completion.SetResult("foo");
        EventLoop.FlushMicrotasks();
        tester.Pump();
        Assert.Empty(tester.ElementsWithText("loaded"));
        Assert.False(RendererBinding.Instance.SendFramesToEngine);

        // The locale reaches the platform before the first frame is allowed.
        bool framesAllowedWhenLocaleReported = true;
        void OnLocale(Locale locale) => framesAllowedWhenLocaleReported = RendererBinding.Instance.SendFramesToEngine;
        PlatformDispatcher.Instance.ApplicationLocaleChanged += OnLocale;
        try
        {
            numbers.Completion.SetResult([42]);
            EventLoop.FlushMicrotasks();
        }
        finally
        {
            PlatformDispatcher.Instance.ApplicationLocaleChanged -= OnLocale;
        }

        Assert.False(framesAllowedWhenLocaleReported);
        Assert.Equal(new Locale("fo"), PlatformDispatcher.Instance.ApplicationLocale);
        Assert.True(RendererBinding.Instance.SendFramesToEngine);
        Assert.Empty(tester.ElementsWithText("loaded"));
        EventLoop.FlushMicrotasks();
        tester.Pump();
        BuildContext context = tester.ElementOfType<Text>();
        Assert.Equal(new Locale("fo"), Localizations.LocaleOf(context));
        Assert.Equal("foo", Localizations.Of<string>(context));
        Assert.Equal([42], Localizations.Of<List<int>>(context));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LocaleCommit_ReportsToPlatformForBothScopeLevels(bool applicationLevel)
    {
        using var tester = CreateTester();
        var pending = new PendingDelegate<string>();
        var notifications = new List<Locale>();
        PlatformDispatcher.Instance.ApplicationLocaleChanged += notifications.Add;
        try
        {
            tester.PumpWidget(Localized(
                new Locale("fo"), [pending], new Text("loaded"), applicationLevel));
            Assert.Empty(notifications);
            pending.Completion.SetResult("foo");
            EventLoop.FlushMicrotasks();
            tester.Pump();
            Assert.Equal([new Locale("fo")], notifications);
            Assert.Equal(new Locale("fo"), PlatformDispatcher.Instance.ApplicationLocale);

            tester.PumpWidget(Localized(new Locale("fo"), [new SyncDelegate("same")], new Text("same")));
            Assert.Single(notifications);
            tester.PumpWidget(Localized(new Locale("zh"), [new SyncDelegate("next")], new Text("next")));
            Assert.Equal([new Locale("fo"), new Locale("zh")], notifications);
        }
        finally
        {
            PlatformDispatcher.Instance.ApplicationLocaleChanged -= notifications.Add;
        }
    }

    [Fact]
    public void PendingReload_RetainsLocaleResourcesAndDirection_ThenUpdatesExistingScopeBeforeNextFrame()
    {
        using var tester = CreateTester();
        BuildContext? context = null;
        var readings = new List<(Locale Locale, string Value, TextDirection Direction)>();
        var child = new Builder(current =>
        {
            context = current;
            readings.Add((Localizations.LocaleOf(current), Localizations.Of<string>(current),
                Directionality.Of(current)));
            return new SizedBox();
        });
        tester.PumpWidget(Localized(new Locale("en"), [new SyncDelegate("old")], child));
        var pendingStrings = new PendingDelegate<string>();
        var pendingWidgets = new PendingDelegate<WidgetsLocalizations>();
        tester.PumpWidget(new Localizations(
            new Locale("ar"), [pendingStrings, pendingWidgets], child));

        Assert.Equal([(new Locale("en"), "old", TextDirection.Ltr)], readings);
        Assert.Equal(new Locale("en"), Localizations.LocaleOf(context!));
        Assert.Equal("old", Localizations.Of<string>(context!));
        Assert.Equal(new Locale("ar"), tester.ElementsOfType<Semantics>()
            .Select(element => (Semantics)element.Widget).Single(widget => widget.LocaleForSubtree != null)
            .LocaleForSubtree);

        pendingStrings.Completion.SetResult("new");
        pendingWidgets.Completion.SetResult(new RtlWidgets());
        EventLoop.FlushMicrotasks();
        Assert.Equal(new Locale("ar"), Localizations.LocaleOf(context!));
        Assert.Equal("new", Localizations.Of<string>(context!));
        Assert.Equal(TextDirection.Ltr, Directionality.Of(context!));
        EventLoop.FlushMicrotasks();
        tester.Pump();
        Assert.Equal((new Locale("ar"), "new", TextDirection.Rtl), readings.Last());
        Assert.Equal(2, readings.Count);
    }

    [Fact]
    public void RemovingScopeBeforeAsyncCompletion_ReleasesDeferralWithoutCommittingLocale()
    {
        using var tester = CreateTester();
        var pending = new PendingDelegate<string>();
        tester.PumpWidget(Localized(new Locale("fo"), [pending], new Text("loaded")));
        tester.PumpWidget(new SizedBox());
        Assert.False(RendererBinding.Instance.SendFramesToEngine);
        pending.Completion.SetResult("foo");
        EventLoop.FlushMicrotasks();
        tester.Pump();
        Assert.True(RendererBinding.Instance.SendFramesToEngine);
        Assert.Null(PlatformDispatcher.Instance.ApplicationLocale);
        Assert.Empty(tester.ElementsWithText("loaded"));
        Assert.Null(tester.TakeException());
    }

    [Fact]
    public void IndependentScopes_HoldOneDeferralPerBatch()
    {
        using var tester = CreateTester();
        var first = new PendingDelegate<string>();
        var second = new PendingDelegate<string>();
        tester.PumpWidget(new Column(children:
        [
            Localized(new Locale("en"), [first], new Text("first")),
            Localized(new Locale("fr"), [second], new Text("second")),
        ]));
        first.Completion.SetResult("a");
        EventLoop.FlushMicrotasks();
        tester.Pump();
        Assert.Single(tester.ElementsWithText("first"));
        Assert.Empty(tester.ElementsWithText("second"));
        Assert.False(RendererBinding.Instance.SendFramesToEngine);
        second.Completion.SetResult("b");
        EventLoop.FlushMicrotasks();
        tester.Pump();
        Assert.True(RendererBinding.Instance.SendFramesToEngine);
        Assert.Single(tester.ElementsWithText("second"));
    }

    [Fact]
    public void OverlappingReloads_CommitInCompletionOrderAsPinnedDartDoes()
    {
        using var tester = CreateTester();
        var first = new PendingDelegate<string>();
        var second = new PendingDelegate<string>();
        var child = new Builder(context => new Text(Localizations.Of<string>(context)));
        tester.PumpWidget(Localized(new Locale("en"), [first], child));
        tester.PumpWidget(Localized(new Locale("fr"), [second], child));
        second.Completion.SetResult("second");
        EventLoop.FlushMicrotasks();
        tester.Pump();
        Assert.Single(tester.ElementsWithText("second"));
        Assert.False(RendererBinding.Instance.SendFramesToEngine);
        first.Completion.SetResult("first");
        EventLoop.FlushMicrotasks();
        tester.Pump();
        Assert.Single(tester.ElementsWithText("first"));
        Assert.Equal(new Locale("en"), Localizations.LocaleOf(tester.ElementOfType<Text>()));
        Assert.True(RendererBinding.Instance.SendFramesToEngine);
    }

    [Fact]
    public void WorkerThreadCompletion_OnlyMutatesFrameworkStateWhenMicrotasksDrain()
    {
        using var tester = CreateTester();
        var pending = new PendingDelegate<string>();
        int commitThread = -1;
        void OnLocale(Locale locale) => commitThread = Environment.CurrentManagedThreadId;
        PlatformDispatcher.Instance.ApplicationLocaleChanged += OnLocale;
        try
        {
            tester.PumpWidget(Localized(new Locale("fo"), [pending], new Text("loaded")));
            var worker = new Thread(() => pending.Completion.SetResult("foo"));
            worker.Start();
            worker.Join();
            Assert.Equal(-1, commitThread);
            Assert.Null(PlatformDispatcher.Instance.ApplicationLocale);
            EventLoop.FlushMicrotasks();
            Assert.Equal(Environment.CurrentManagedThreadId, commitThread);
            EventLoop.FlushMicrotasks();
            tester.Pump();
            Assert.Single(tester.ElementsWithText("loaded"));
        }
        finally
        {
            PlatformDispatcher.Instance.ApplicationLocaleChanged -= OnLocale;
        }
    }

    [Fact]
    public void AsyncFailure_ReportsErrorAndRetainsDartsOutstandingDeferral()
    {
        using var tester = CreateTester();
        var pending = new PendingDelegate<string>();
        var error = new InvalidOperationException("translations unavailable");
        tester.PumpWidget(Localized(new Locale("fo"), [pending], new Text("loaded")));
        try
        {
            pending.Completion.SetException(error);
            EventLoop.FlushMicrotasks();
            Assert.Same(error, tester.TakeException());
            Assert.False(RendererBinding.Instance.SendFramesToEngine);
            Assert.Null(PlatformDispatcher.Instance.ApplicationLocale);
            Assert.Empty(tester.ElementsWithText("loaded"));
        }
        finally
        {
            RendererBinding.Instance.AllowFirstFrame();
        }
    }

    [Fact]
    public void SynchronousDelegates_FilterBeforeLoading_AndSkipAlreadyClaimedTypes()
    {
        using var tester = CreateTester();
        var calls = new List<string>();
        var unsupported = new SyncDelegate("unsupported", calls, supported: false);
        var first = new SyncDelegate("first", calls);
        var ignored = new SyncDelegate("ignored", calls);
        tester.PumpWidget(Localized(new Locale("en"), [unsupported, first, ignored], new Text("loaded")));

        Assert.Equal(["supported:unsupported", "supported:first", "load:first"], calls);
        Assert.Equal("first", Localizations.Of<string>(tester.ElementOfType<Text>()));
        Assert.True(RendererBinding.Instance.SendFramesToEngine);
        Assert.Equal(0, _turns.PendingTimerCount);
    }

    [Fact]
    public void AlreadyCompletedTask_StillDefersFirstFrameAndCommitsInAMicrotask()
    {
        using var tester = CreateTester();
        var pending = new PendingDelegate<string>();
        pending.Completion.SetResult("cached");
        bool builtBeforeMicrotasks = false;
        var child = new Builder(context => new Text(Localizations.Of<string>(context)));
        tester.PumpWidget(new Builder(context =>
        {
            var localizations = Localized(new Locale("en"), [pending], child);
            // Frame callbacks run before pump's final microtask drain. They can observe the deferred
            // tree before the already-completed future's listener is delivered.
            SchedulerBinding.Instance.AddPostFrameCallback(_ =>
            {
                builtBeforeMicrotasks = tester.ElementsWithText("cached").Count != 0;
                Assert.False(RendererBinding.Instance.SendFramesToEngine);
                Assert.Null(PlatformDispatcher.Instance.ApplicationLocale);
            });
            return localizations;
        }));
        Assert.False(builtBeforeMicrotasks);
        Assert.Empty(tester.ElementsWithText("cached"));
        Assert.Equal(new Locale("en"), PlatformDispatcher.Instance.ApplicationLocale);
        tester.Pump();
        Assert.Single(tester.ElementsWithText("cached"));
    }

    [Fact]
    public void DirectValueTaskResult_PreservesImmediateResourceAvailability()
    {
        using var tester = CreateTester();
        tester.PumpWidget(Localized(new Locale("en"), [new DirectValueDelegate()], new Builder(context =>
        {
            Assert.True(RendererBinding.Instance.SendFramesToEngine);
            return new Text(Localizations.Of<string>(context));
        })));
        Assert.Single(tester.ElementsWithText("direct"));
        Assert.Equal(0, _turns.PendingTimerCount);
    }

    [Fact]
    public void CompletedValueTaskSource_IsConsumedOnceAndRetainsAsynchronousDelivery()
    {
        using var tester = CreateTester();
        var source = new CompletedSourceDelegate();
        tester.PumpWidget(Localized(new Locale("en"), [source], new Builder(context =>
            new Text(Localizations.Of<string>(context)))));
        Assert.Equal(1, source.ReadCount);
        Assert.Empty(tester.ElementsWithText("source"));
        tester.Pump();
        Assert.Single(tester.ElementsWithText("source"));
        Assert.Equal(1, source.ReadCount);
    }

    [Fact]
    public void PendingDelegate_ClaimsItsTypeBeforeLaterSynchronousDelegate()
    {
        using var tester = CreateTester();
        var pending = new PendingDelegate<string>();
        var calls = new List<string>();
        tester.PumpWidget(Localized(
            new Locale("en"), [pending, new SyncDelegate("ignored", calls)], new Text("loaded")));
        Assert.Empty(calls);
        pending.Completion.SetResult("first");
        EventLoop.FlushMicrotasks();
        tester.Pump();
        Assert.Equal("first", Localizations.Of<string>(tester.ElementOfType<Text>()));
    }

    [Fact]
    public void DelegateReloadPolicy_OnlyNotifiesDependenciesWhenResourcesChange()
    {
        using var tester = CreateTester();
        var values = new List<string>();
        var child = new Builder(context =>
        {
            values.Add(Localizations.Of<string>(context));
            return new SizedBox();
        });
        tester.PumpWidget(Localized(new Locale("en"), [new SyncDelegate("first")], child));
        tester.PumpWidget(Localized(new Locale("en"), [new SyncDelegate("ignored")], child));
        Assert.Equal(["first"], values);
        tester.PumpWidget(Localized(new Locale("en"), [new SyncDelegate("reloaded", reload: true)], child));
        Assert.Equal(["first", "reloaded"], values);
        tester.PumpWidget(Localized(new Locale("fr"), [new SyncDelegate("locale")], child));
        Assert.Equal(["first", "reloaded", "locale"], values);
    }

    [Fact]
    public void DelegateCountChange_ReloadsEvenWhenDelegatePoliciesReturnFalse()
    {
        using var tester = CreateTester();
        var child = new Builder(context => new Text(Localizations.Of<string>(context)));
        tester.PumpWidget(Localized(new Locale("en"), [new SyncDelegate("first")], child));
        tester.PumpWidget(Localized(new Locale("en"),
            [new SyncDelegate("updated"), new SyncDelegate("duplicate")], child));
        Assert.Single(tester.ElementsWithText("updated"));
    }

    [Fact]
    public void Override_PrependsDelegatesAndInheritsCommittedLocaleDuringPendingReload()
    {
        using var tester = CreateTester();
        var child = new Builder(context => Localizations.Override(
            context,
            delegates: [new SyncDelegate("override")],
            child: new Builder(inner => new Text(
                $"{Localizations.LocaleOf(inner).LanguageCode}:{Localizations.Of<string>(inner)}"))));
        tester.PumpWidget(Localized(new Locale("en"), [new SyncDelegate("base")], child));
        Assert.Single(tester.ElementsWithText("en:override"));
        var pending = new PendingDelegate<string>();
        tester.PumpWidget(Localized(new Locale("fr"), [pending], child));
        Assert.Single(tester.ElementsWithText("en:override"));
        pending.Completion.SetResult("base-fr");
        EventLoop.FlushMicrotasks();
        tester.Pump();
        Assert.Single(tester.ElementsWithText("fr:override"));
    }

    [Fact]
    public void ScopeKey_PreservesDescendantStateWhenSemanticsContainerChanges()
    {
        using var tester = CreateTester();
        var child = new StateProbe();
        tester.PumpWidget(Localized(new Locale("en"), [], child));
        State state = tester.ElementOfType<StateProbe>() is StatefulElement element ? element.State : null!;
        tester.PumpWidget(Localized(new Locale("en"), [], child, applicationLevel: true));
        Assert.Same(state, ((StatefulElement)tester.ElementOfType<StateProbe>()).State);
    }

    [Fact]
    public void MissingScope_ReturnsNullFromNullableLookups()
    {
        using var tester = CreateTester();
        tester.PumpWidget(new Builder(context =>
        {
            Assert.Null(Localizations.MaybeLocaleOf(context));
            Assert.Null(Localizations.MaybeOf<string>(context));
            Assert.Null(Localizations.Of<object>(context, typeof(string)));
            return new SizedBox();
        }));
    }

    [DebugOnlyFact]
    public void MissingScope_LocaleOfReportsFlutterDiagnostic()
    {
        using var tester = CreateTester();
        tester.PumpWidget(new Builder(context =>
        {
            var error = Assert.Throws<FlutterError>(() => Localizations.LocaleOf(context));
            Assert.Contains("does not include a Localizations ancestor", error.Message);
            return new SizedBox();
        }));
    }

    [DebugOnlyFact]
    public void MissingWidgetsResources_ReportTheWidgetsLocalizationsDiagnostic()
    {
        using var tester = CreateTester();
        tester.PumpWidget(new Builder(context =>
        {
            var error = Assert.Throws<FlutterError>(() => WidgetsLocalizations.Of(context));
            Assert.Contains("No WidgetsLocalizations found", error.Message);
            return new SizedBox();
        }));
    }

    [Fact]
    public void ExplicitResourceTypeLookup_UsesTheOverriddenDelegateType()
    {
        using var tester = CreateTester();
        tester.PumpWidget(Localized(new Locale("en"), [new AliasedDelegate()], new Builder(context =>
        {
            Assert.Null(Localizations.MaybeOf<string>(context));
            Assert.Equal("aliased", Localizations.Of<string>(context, typeof(object)));
            Assert.Throws<InvalidCastException>(() => Localizations.Of<WidgetsLocalizations>(context, typeof(object)));
            return new SizedBox();
        })));
    }

    [Fact]
    public void LocalizationAtZeroArea_DoesNotCrash()
    {
        using var tester = CreateTester();
        tester.PumpWidget(new Directionality(TextDirection.Ltr, new Center(
            new SizedBox(width: 0.0, height: 0.0,
                child: Localized(new Locale("fo"), [], new Text("X"))))));
        var box = Assert.IsAssignableFrom<RenderBox>(tester.ElementOfType<Localizations>().FindRenderObject());
        Assert.Equal(new Size(0.0, 0.0), box.Size);
        Assert.Null(tester.TakeException());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ApplicationShell_WaitsForCustomAsyncResources(bool cupertino)
    {
        using var tester = CreateTester();
        var pending = new PendingDelegate<string>();
        var home = new Builder(context => new Text(Localizations.Of<string>(context)));
        Widget app = cupertino
            ? new CupertinoApp(home: home, localizationsDelegates: [pending])
            : new MaterialApp(home: home, localizationsDelegates: [pending]);
        tester.PumpWidget(app);
        Assert.Empty(tester.ElementsWithText("ready"));
        Assert.False(RendererBinding.Instance.SendFramesToEngine);
        pending.Completion.SetResult("ready");
        EventLoop.FlushMicrotasks();
        tester.Pump();
        Assert.Single(tester.ElementsWithText("ready"));
        Assert.True(RendererBinding.Instance.SendFramesToEngine);
    }

    [Fact]
    public void FirstFrameRasterized_WaitsForLocalizationResourcesInTheRealBinding()
    {
        WidgetsBinding binding = WidgetsBinding.Instance;
        binding.ResetRootForTests();
        binding.ResetFirstFrameForTests();
        var pending = new PendingDelegate<string>();
        var host = new WidgetHost();
        try
        {
            host.RunApplication(Localized(new Locale("fo"), [pending], new SizedBox()));
            DrainTurns();
            PlatformDispatcher.Instance.OnReportTimings?.Invoke([]);
            Assert.False(binding.FirstFrameRasterized);
            Assert.False(binding.WaitUntilFirstFrameRasterized.IsCompleted);
            pending.Completion.SetResult("ready");
            EventLoop.FlushMicrotasks();
            DrainTurns();
            PlatformDispatcher.Instance.OnReportTimings!([]);
            Assert.True(binding.FirstFrameRasterized);
            Assert.True(binding.WaitUntilFirstFrameRasterized.IsCompleted);
            Assert.Equal(new Locale("fo"), PlatformDispatcher.Instance.ApplicationLocale);
        }
        finally
        {
            binding.ResetRootForTests();
            binding.ResetFirstFrameForTests();
        }
    }

    private void DrainTurns()
    {
        while (_turns.RunNextTimer())
        {
        }
    }

    private static Localizations Localized(
        Locale locale,
        IReadOnlyList<LocalizationsDelegate> delegates,
        Widget child,
        bool applicationLevel = false) => new(
            locale, [.. delegates, DefaultWidgetsLocalizations.Delegate], child, applicationLevel);

    private static FrameworkDartTester CreateTester()
    {
        var tester = new FrameworkDartTester();
        RendererBinding.Instance.ResetFirstFrameSent();
        return tester;
    }

    private sealed class PendingDelegate<T> : LocalizationsDelegate<T> where T : class
    {
        public TaskCompletionSource<T> Completion { get; } = new();
        public override bool IsSupported(Locale locale) => true;
        public override ValueTask<T> LoadTypedAsync(Locale locale) => new(Completion.Task);
        public override bool ShouldReload(LocalizationsDelegate oldDelegate) => false;
    }

    private sealed class SyncDelegate(
        string value,
        List<string>? calls = null,
        bool supported = true,
        bool reload = false) : LocalizationsDelegate<string>
    {
        public override bool IsSupported(Locale locale)
        {
            calls?.Add($"supported:{value}");
            return supported;
        }

        public override string LoadTyped(Locale locale)
        {
            calls?.Add($"load:{value}");
            return value;
        }

        public override bool ShouldReload(LocalizationsDelegate oldDelegate) => reload;
    }

    private sealed class RtlWidgets : DefaultWidgetsLocalizations
    {
        public override TextDirection TextDirection => TextDirection.Rtl;
    }

    private sealed class AliasedDelegate : LocalizationsDelegate<string>
    {
        public override Type ResourceType => typeof(object);
        public override bool IsSupported(Locale locale) => true;
        public override string LoadTyped(Locale locale) => "aliased";
        public override bool ShouldReload(LocalizationsDelegate oldDelegate) => false;
    }

    private sealed class DirectValueDelegate : LocalizationsDelegate<string>
    {
        public override bool IsSupported(Locale locale) => true;
        public override ValueTask<string> LoadTypedAsync(Locale locale) => new("direct");
        public override bool ShouldReload(LocalizationsDelegate oldDelegate) => false;
    }

    private sealed class CompletedSourceDelegate : LocalizationsDelegate<string>, IValueTaskSource<string>
    {
        public int ReadCount { get; private set; }
        public override bool IsSupported(Locale locale) => true;
        public override ValueTask<string> LoadTypedAsync(Locale locale) => new(this, 0);
        public override bool ShouldReload(LocalizationsDelegate oldDelegate) => false;
        public ValueTaskSourceStatus GetStatus(short token) => ValueTaskSourceStatus.Succeeded;

        public string GetResult(short token)
        {
            ReadCount += 1;
            if (ReadCount != 1)
            {
                throw new InvalidOperationException("A value task source must be consumed once.");
            }

            return "source";
        }

        public void OnCompleted(
            Action<object?> continuation,
            object? state,
            short token,
            ValueTaskSourceOnCompletedFlags flags) => throw new InvalidOperationException("Already completed.");
    }

    private sealed class StateProbe : StatefulWidget
    {
        public override State CreateState() => new ProbeState();

        private sealed class ProbeState : State<StateProbe>
        {
            public override Widget Build(BuildContext context) => new SizedBox();
        }
    }
}
