using Plumix.Foundation;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/foundation/change_notifier.dart
// Covers flutter/packages/flutter/test/foundation/change_notifier_test.dart.

namespace Plumix.Tests;

public sealed class ChangeNotifierTests
{
    [Fact]
    public void AddRemoveAndHasListenersMatchFlutter()
    {
        using var notifier = new TestNotifier();
        var log = new List<string>();
        void Listener() => log.Add("listener");

        Assert.False(notifier.IsListenedTo);
        notifier.AddListener(Listener);
        notifier.AddListener(Listener);
        Assert.True(notifier.IsListenedTo);

        notifier.NotifyListeners();
        Assert.Equal(["listener", "listener"], log);

        log.Clear();
        notifier.RemoveListener(Listener);
        notifier.NotifyListeners();
        Assert.Equal(["listener"], log);

        notifier.RemoveListener(Listener);
        notifier.RemoveListener(Listener);
        Assert.False(notifier.IsListenedTo);
    }

    [Fact]
    public void MutatingListenerSkipsRemovedListenersAndDefersAddedListeners()
    {
        using var notifier = new TestNotifier();
        var log = new List<string>();
        void Listener1() => log.Add("listener1");
        void Listener3() => log.Add("listener3");
        void Listener4() => log.Add("listener4");
        void Listener2()
        {
            log.Add("listener2");
            notifier.RemoveListener(Listener1);
            notifier.RemoveListener(Listener3);
            notifier.AddListener(Listener4);
        }

        notifier.AddListener(Listener1);
        notifier.AddListener(Listener2);
        notifier.AddListener(Listener3);

        notifier.NotifyListeners();
        Assert.Equal(["listener1", "listener2"], log);

        log.Clear();
        notifier.NotifyListeners();
        Assert.Equal(["listener2", "listener4"], log);

        log.Clear();
        notifier.NotifyListeners();
        Assert.Equal(["listener2", "listener4", "listener4"], log);
    }

    [Fact]
    public void ListenerAddedAndRemovedDuringDispatchIsNotCalled()
    {
        using var notifier = new TestNotifier();
        var log = new List<string>();
        void Listener2() => log.Add("listener2");
        void Listener3() => log.Add("listener3");
        void Listener1()
        {
            log.Add("listener1");
            notifier.AddListener(Listener2);
            notifier.RemoveListener(Listener2);
            notifier.AddListener(Listener3);
        }

        notifier.AddListener(Listener1);
        notifier.NotifyListeners();

        Assert.Equal(["listener1"], log);
    }

    [Fact]
    public void SelfRemovingListenerDoesNotSkipLaterListeners()
    {
        using var notifier = new TestNotifier();
        var log = new List<string>();
        void Listener() => log.Add("listener");
        void SelfRemovingListener()
        {
            log.Add("selfRemovingListener");
            notifier.RemoveListener(SelfRemovingListener);
        }

        notifier.AddListener(SelfRemovingListener);
        notifier.AddListener(Listener);
        notifier.NotifyListeners();

        Assert.Equal(["selfRemovingListener", "listener"], log);
    }

    [Fact]
    public void NotifyListenersCanReenter()
    {
        using var notifier = new ValueNotifier<int>(1);
        var log = new List<int>();
        void Listener()
        {
            log.Add(notifier.Value);
            if (notifier.Value < 0)
            {
                notifier.Value = 0;
            }
        }

        notifier.AddListener(Listener);
        notifier.Value = -2;

        Assert.Equal([-2, 0], log);
    }

    [Fact]
    public void RemovalDuringDispatchCoversInPlaceDefragmentationPath()
    {
        using var notifier = new TestNotifier();
        var log = new List<string>();
        var listeners = new List<Action>();
        void AutoRemove()
        {
            notifier.RemoveListener(listeners[1]);
            notifier.RemoveListener(listeners[3]);
            notifier.RemoveListener(listeners[4]);
            notifier.RemoveListener(AutoRemove);
        }

        notifier.AddListener(AutoRemove);
        for (int index = 0; index < 12; index += 1)
        {
            int capturedIndex = index;
            void Listener() => log.Add($"listener{capturedIndex}");
            listeners.Add(Listener);
            notifier.AddListener(Listener);
        }

        string[] expected = [
            "listener0", "listener2", "listener5", "listener6", "listener7",
            "listener8", "listener9", "listener10", "listener11",
        ];
        notifier.NotifyListeners();
        Assert.Equal(expected, log);

        log.Clear();
        notifier.NotifyListeners();
        Assert.Equal(expected, log);
    }

    [Fact]
    public void ThrowingListenerIsReportedAndDoesNotStopDispatch()
    {
        using var notifier = new TestNotifier();
        var log = new List<string>();
        var reported = new List<FlutterErrorDetails>();
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = reported.Add;
        try
        {
            notifier.AddListener(() =>
            {
                log.Add("bad");
                throw new ArgumentException("bad listener");
            });
            notifier.AddListener(() => log.Add("good"));

            notifier.NotifyListeners();
        }
        finally
        {
            FlutterError.OnError = previous;
        }

        Assert.Equal(["bad", "good"], log);
        FlutterErrorDetails details = Assert.Single(reported);
        Assert.IsType<ArgumentException>(details.Exception);
        Assert.Equal("foundation library", details.Library);
        Assert.Contains("while dispatching notifications for TestNotifier", details.Context!.ToString());
    }

    [DebugOnlyFact]
    public void CannotDisposeDuringNotification()
    {
        var notifier = new TestNotifier();
        bool callbackDidFinish = false;
        FlutterErrorDetails? reported = null;
        FlutterExceptionHandler? previous = FlutterError.OnError;
        FlutterError.OnError = details => reported = details;
        try
        {
            notifier.AddListener(() =>
            {
                notifier.Dispose();
                callbackDidFinish = true;
            });
            notifier.NotifyListeners();
        }
        finally
        {
            FlutterError.OnError = previous;
        }

        Assert.False(callbackDidFinish);
        Assert.NotNull(reported);
        Assert.IsType<AssertionError>(reported!.Exception);
        notifier.Dispose();
    }

    [DebugOnlyFact]
    public void DisposedNotifierRejectsUseButAllowsRemoveListener()
    {
        var notifier = new TestNotifier();
        Action listener = () => { };
        notifier.AddListener(listener);
        notifier.Dispose();

        Assert.False(notifier.IsListenedTo);
        notifier.RemoveListener(listener);
        Assert.Throws<FlutterError>(() => notifier.AddListener(listener));
        Assert.Throws<FlutterError>(notifier.NotifyListeners);
        Assert.Throws<FlutterError>(notifier.Dispose);
        Assert.Throws<FlutterError>(() => ChangeNotifier.DebugAssertNotDisposed(notifier));
    }

    [Fact]
    public void MergeForwardsAnyEnumerableAndIgnoresNulls()
    {
        using var first = new TestNotifier();
        using var second = new TestNotifier();
        IEnumerable<IListenable?> sources = new HashSet<IListenable?> { first, null, second };
        IListenable merged = Listenable.Merge(sources);
        int notifications = 0;
        void Listener() => notifications += 1;

        merged.AddListener(Listener);
        first.NotifyListeners();
        second.NotifyListeners();
        Assert.Equal(2, notifications);

        merged.RemoveListener(Listener);
        first.NotifyListeners();
        second.NotifyListeners();
        Assert.Equal(2, notifications);
        Assert.False(first.IsListenedTo);
        Assert.False(second.IsListenedTo);
    }

    [Fact]
    public void MergeToStringMatchesFlutterShape()
    {
        using var notifier = new TestNotifier();

        Assert.Equal("Listenable.merge([])", Listenable.Merge().ToString());
        Assert.Equal("Listenable.merge([null])", Listenable.Merge([null]).ToString());
        Assert.Equal(
            "Listenable.merge([null, Instance of 'TestNotifier'])",
            Listenable.Merge([null, notifier]).ToString());
    }

    [Fact]
    public void ValueNotifierOnlyNotifiesForUnequalValues()
    {
        using var notifier = new ValueNotifier<double>(2.0);
        var values = new List<double>();
        notifier.AddListener(() => values.Add(notifier.Value));

        notifier.Value = 3.0;
        notifier.Value = 3.0;

        Assert.Equal([3.0], values);
        Assert.StartsWith("ValueNotifier<double>#", notifier.ToString(), StringComparison.Ordinal);
        Assert.EndsWith("(3)", notifier.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void LifecycleDispatchesMemoryEvents()
    {
        var events = new List<ObjectEvent>();
        FlutterMemoryAllocations.Instance.AddListener(events.Add);
        TestNotifier notifier;
        ValueNotifier<bool> valueNotifier;
        try
        {
            notifier = new TestNotifier();
            notifier.AddListener(() => { });
            notifier.Dispose();
            valueNotifier = new ValueNotifier<bool>(true);
            valueNotifier.Dispose();
        }
        finally
        {
            FlutterMemoryAllocations.Instance.RemoveListener(events.Add);
        }

        if (Constants.KDebugMode)
        {
            Assert.Equal(4, events.Count);
            Assert.Collection(
                events,
                @event => Assert.Same(notifier, Assert.IsType<ObjectCreated>(@event).Object),
                @event => Assert.Same(notifier, Assert.IsType<ObjectDisposed>(@event).Object),
                @event => Assert.Same(valueNotifier, Assert.IsType<ObjectCreated>(@event).Object),
                @event => Assert.Same(valueNotifier, Assert.IsType<ObjectDisposed>(@event).Object));
            ObjectCreated created = Assert.IsType<ObjectCreated>(events[0]);
            Assert.Equal("package:flutter/foundation.dart", created.Library);
            Assert.Equal("ChangeNotifier", created.ClassName);
        }
        else
        {
            Assert.Empty(events);
        }
    }

    private sealed class TestNotifier : ChangeNotifier
    {
        public bool IsListenedTo => HasListeners;
    }
}
