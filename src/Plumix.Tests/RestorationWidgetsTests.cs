using Avalonia;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

/// <summary>
/// Ports Flutter's <c>test/widgets/restoration_scope_test.dart</c>,
/// <c>root_restoration_scope_test.dart</c> and <c>restoration_mixin_test.dart</c>.
/// </summary>
[Collection(SchedulerTestCollection.Name)]
public sealed class RestorationWidgetsTests : IDisposable
{
    private readonly MockRestorationManager _manager = new();
    private readonly RestorationManager _previousInstance = RestorationManager.Instance;

    public RestorationWidgetsTests() => Scheduler.ResetForTests();

    public void Dispose()
    {
        RestorationManager.Instance = _previousInstance;
        Scheduler.ResetForTests();
    }

    // ---------------------------------------------------------------- UnmanagedRestorationScope

    [Fact]
    public void UnmanagedRestorationScope_ExposesItsBucketAndNotifiesOnReplacement()
    {
        var first = RestorationBucket.Root(_manager, RawRestorationData.Build());
        var second = RestorationBucket.Root(_manager, RawRestorationData.Build());
        RestorationBucket? seen = null;

        using var harness = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: first,
            child: new BucketSpy(bucket => seen = bucket)));
        Assert.Same(first, seen);

        harness.Update(new UnmanagedRestorationScope(
            bucket: second,
            child: new BucketSpy(bucket => seen = bucket)));
        Assert.Same(second, seen);
    }

    [Fact]
    public void UnmanagedRestorationScope_NullBucketDisablesRestorationBelow()
    {
        RestorationBucket? seen = RestorationBucket.Empty("x", debugOwner: null);

        using var harness = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: null,
            child: new BucketSpy(bucket => seen = bucket)));

        Assert.Null(seen);
    }

    [Fact]
    public void RestorationScope_OfThrowsWhenNoScopeIsFound()
    {
        InvalidOperationException? error = null;

        using var harness = new RestorationHarness(new Builder(context =>
        {
            try
            {
                RestorationScope.Of(context);
            }
            catch (InvalidOperationException exception)
            {
                error = exception;
            }

            return new SizedBox(width: 0.0, height: 0.0);
        }));

        Assert.NotNull(error);
        Assert.Contains("State restoration must be enabled for a RestorationScope", error!.Message);
    }

    // ------------------------------------------------------------------------ RestorationScope

    [Fact]
    public void RestorationScope_ClaimsAChildBucketAndExposesItToDescendants()
    {
        var rawData = RawRestorationData.Build();
        var root = RestorationBucket.Root(_manager, rawData);
        RestorationBucket? seen = null;

        using var harness = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: root,
            child: new RestorationScope(
                restorationId: "child1",
                child: new BucketSpy(bucket => seen = bucket))));
        _manager.DoSerialization();

        Assert.NotNull(seen);
        Assert.Equal("child1", seen!.RestorationId);
        Assert.NotNull(RawRestorationData.Child(rawData, "child1"));
    }

    [Fact]
    public void RestorationScope_BucketCarriesTheDataStoredForItsId()
    {
        var root = RestorationBucket.Root(
            _manager,
            RawRestorationData.Build(
                children: new Dictionary<object, object?>
                {
                    ["child1"] = RawRestorationData.Build(
                        values: new Dictionary<object, object?> { ["foo"] = 22 }),
                }));
        RestorationBucket? seen = null;

        using var harness = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: root,
            child: new RestorationScope(
                restorationId: "child1",
                child: new BucketSpy(bucket => seen = bucket))));

        Assert.Equal(22, seen!.Read<int>("foo"));
    }

    [Fact]
    public void RestorationScope_RenamesTheExistingBucketWhenTheIdChanges()
    {
        var rawData = RawRestorationData.Build();
        var root = RestorationBucket.Root(_manager, rawData);
        RestorationBucket? seen = null;

        using var harness = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: root,
            child: new RestorationScope(
                restorationId: "child1",
                child: new BucketSpy(bucket => seen = bucket))));
        RestorationBucket? original = seen;
        original!.Write("foo", 22);

        harness.Update(new UnmanagedRestorationScope(
            bucket: root,
            child: new RestorationScope(
                restorationId: "child2",
                child: new BucketSpy(bucket => seen = bucket))));
        _manager.DoSerialization();

        Assert.Same(original, seen);
        Assert.Equal("child2", seen!.RestorationId);
        Assert.Equal(22, seen.Read<int>("foo"));
        Assert.Null(RawRestorationData.Child(rawData, "child1"));
        Assert.NotNull(RawRestorationData.Child(rawData, "child2"));
    }

    [Fact]
    public void RestorationScope_RemovingItFromTheTreeDeletesItsData()
    {
        var rawData = RawRestorationData.Build();
        var root = RestorationBucket.Root(_manager, rawData);

        using var harness = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: root,
            child: new RestorationScope(
                restorationId: "child1",
                child: new BucketSpy(_ => { }))));
        _manager.DoSerialization();
        Assert.NotNull(RawRestorationData.Child(rawData, "child1"));

        harness.Update(new UnmanagedRestorationScope(
            bucket: root,
            child: new SizedBox(width: 0.0, height: 0.0)));
        _manager.DoSerialization();

        Assert.Null(RawRestorationData.Children(rawData));
    }

    [Fact]
    public void RestorationScope_NullIdGivesDescendantsNoBucketAndTogglingBackClaimsOne()
    {
        var root = RestorationBucket.Root(_manager, RawRestorationData.Build());
        RestorationBucket? seen = null;

        using var harness = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: root,
            child: new RestorationScope(
                restorationId: null,
                child: new BucketSpy(bucket => seen = bucket))));
        Assert.Null(seen);

        harness.Update(new UnmanagedRestorationScope(
            bucket: root,
            child: new RestorationScope(
                restorationId: "child1",
                child: new BucketSpy(bucket => seen = bucket))));
        Assert.NotNull(seen);

        harness.Update(new UnmanagedRestorationScope(
            bucket: root,
            child: new RestorationScope(
                restorationId: null,
                child: new BucketSpy(bucket => seen = bucket))));
        Assert.Null(seen);
    }

    [Fact]
    public void RestorationScope_WithoutAnAncestorScopeGivesDescendantsNoBucket()
    {
        RestorationBucket? seen = RestorationBucket.Empty("x", debugOwner: null);

        using var harness = new RestorationHarness(new RestorationScope(
            restorationId: "child1",
            child: new BucketSpy(bucket => seen = bucket)));

        Assert.Null(seen);
    }

    // -------------------------------------------------------------------- RootRestorationScope

    [Fact]
    public void RootRestorationScope_DoesNotAskForTheRootBucketWhenInsideAScope()
    {
        var manager = new TestRestorationManager { Data = RawRestorationData.Build() };
        RestorationManager.Instance = manager;
        var ancestor = RestorationBucket.Root(_manager, RawRestorationData.Build());
        RestorationBucket? seen = null;

        using var harness = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: ancestor,
            child: new RootRestorationScope(
                restorationId: "root-child",
                child: new BucketSpy(bucket => seen = bucket))));

        Assert.Equal(0, manager.RootBucketAccessed);
        Assert.Equal("root-child", seen!.RestorationId);
    }

    [Fact]
    public void RootRestorationScope_WaitsForTheRootBucketBeforeBuildingItsChild()
    {
        var manager = new TestRestorationManager
        {
            AnswerSynchronously = false,
            Data = RawRestorationData.Build(),
        };
        RestorationManager.Instance = manager;
        RestorationBucket? seen = null;
        bool built = false;

        using var harness = new RestorationHarness(new RootRestorationScope(
            restorationId: "root-child",
            child: new BucketSpy(bucket =>
            {
                built = true;
                seen = bucket;
            })));

        Assert.False(built);
        Assert.Equal(1, manager.RootBucketAccessed);

        manager.RespondWith(enabled: true, data: RawRestorationData.Build());
        harness.FlushBuild();

        Assert.True(built);
        Assert.Equal("root-child", seen!.RestorationId);
        Assert.Equal(1, manager.RootBucketAccessed);
    }

    [Fact]
    public void RootRestorationScope_RendersImmediatelyWhenTheRootIsAvailableSynchronously()
    {
        var manager = new TestRestorationManager { Data = RawRestorationData.Build() };
        RestorationManager.Instance = manager;
        bool built = false;

        using var harness = new RestorationHarness(new RootRestorationScope(
            restorationId: "root-child",
            child: new BucketSpy(_ => built = true)));

        Assert.True(built);
    }

    [Fact]
    public void RootRestorationScope_NeverAsksForTheRootBucketWhenTheIdIsNull()
    {
        var manager = new TestRestorationManager { Data = RawRestorationData.Build() };
        RestorationManager.Instance = manager;
        RestorationBucket? seen = RestorationBucket.Empty("x", debugOwner: null);

        using var harness = new RestorationHarness(new RootRestorationScope(
            restorationId: null,
            child: new BucketSpy(bucket => seen = bucket)));

        Assert.Equal(0, manager.RootBucketAccessed);
        Assert.Null(seen);
    }

    [Fact]
    public void RootRestorationScope_InjectsTheNewRootWhenTheOldOneIsDecommissioned()
    {
        var manager = new TestRestorationManager { Data = RawRestorationData.Build() };
        RestorationManager.Instance = manager;
        RestorationBucket? seen = null;

        using var harness = new RestorationHarness(new RootRestorationScope(
            restorationId: "root-child",
            child: new BucketSpy(bucket => seen = bucket)));
        RestorationBucket? original = seen;

        manager.RespondWith(
            enabled: true,
            data: RawRestorationData.Build(
                children: new Dictionary<object, object?>
                {
                    ["root-child"] = RawRestorationData.Build(
                        values: new Dictionary<object, object?> { ["foo"] = 22 }),
                }));
        harness.FlushBuild();

        Assert.NotSame(original, seen);
        Assert.Equal(22, seen!.Read<int>("foo"));
    }

    [Fact]
    public void RootRestorationScope_InjectsNullWhenRestorationIsDisabled()
    {
        var manager = new TestRestorationManager { Enabled = false };
        RestorationManager.Instance = manager;
        RestorationBucket? seen = RestorationBucket.Empty("x", debugOwner: null);
        bool built = false;

        using var harness = new RestorationHarness(new RootRestorationScope(
            restorationId: "root-child",
            child: new BucketSpy(bucket =>
            {
                built = true;
                seen = bucket;
            })));

        Assert.True(built);
        Assert.Null(seen);
    }

    // --------------------------------------------------------------------- RestorationState

    [Fact]
    public void RestorationState_ClaimsABucketAndInitializesThePropertyFromItsDefault()
    {
        var rawData = RawRestorationData.Build();
        var root = RestorationBucket.Root(_manager, rawData);
        var property = new TestRestorableProperty(10);
        RestorableWidgetState? state = null;

        using var harness = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: root,
            child: new RestorableWidget("widget", property, s => state = s)));
        _manager.DoSerialization();

        Assert.Equal(new[] { "createDefaultValue", "initWithValue", "toPrimitives" }, property.Log);
        Assert.Equal(10, property.Value);
        Assert.Equal("widget", state!.Bucket!.RestorationId);
        Assert.Equal(10, RawRestorationData.Values(RawRestorationData.Child(rawData, "widget")!)!["foo"]);
        Assert.Empty(state.ToggleBucketLog);
        Assert.Null(Assert.Single(state.RestoreStateLog));
        Assert.True(Assert.Single(state.InitialRestoreLog));
    }

    [Fact]
    public void RestorationState_RestoresThePropertyFromTheClaimedBucketData()
    {
        var root = RestorationBucket.Root(
            _manager,
            RawRestorationData.Build(
                children: new Dictionary<object, object?>
                {
                    ["widget"] = RawRestorationData.Build(
                        values: new Dictionary<object, object?> { ["foo"] = 22 }),
                }));
        var property = new TestRestorableProperty(10);

        using var harness = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: root,
            child: new RestorableWidget("widget", property)));

        Assert.Equal(new[] { "fromPrimitives", "initWithValue" }, property.Log);
        Assert.Equal(22, property.Value);
    }

    [Fact]
    public void RestorationState_RenamesTheBucketWhenTheIdChangesWithoutTouchingProperties()
    {
        var rawData = RawRestorationData.Build();
        var root = RestorationBucket.Root(_manager, rawData);
        var property = new TestRestorableProperty(10);
        RestorableWidgetState? state = null;

        using var harness = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: root,
            child: new RestorableWidget("widget", property, s => state = s)));
        RestorationBucket? original = state!.Bucket;
        property.Log.Clear();

        harness.Update(new UnmanagedRestorationScope(
            bucket: root,
            child: new RestorableWidget("other", property)));
        _manager.DoSerialization();

        Assert.Same(original, state.Bucket);
        Assert.Equal("other", state.Bucket!.RestorationId);
        Assert.Equal(10, state.Bucket.Read<int>("foo"));
        Assert.Empty(property.Log);
        Assert.Single(state.RestoreStateLog);
        Assert.Empty(state.ToggleBucketLog);
        Assert.Null(RawRestorationData.Child(rawData, "widget"));
    }

    [Fact]
    public void RestorationState_RemovingTheWidgetDeletesItsData()
    {
        var rawData = RawRestorationData.Build();
        var root = RestorationBucket.Root(_manager, rawData);

        using var harness = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: root,
            child: new RestorableWidget("widget", new TestRestorableProperty(10))));
        _manager.DoSerialization();
        Assert.NotNull(RawRestorationData.Child(rawData, "widget"));

        harness.Update(new UnmanagedRestorationScope(
            bucket: root,
            child: new SizedBox(width: 0.0, height: 0.0)));
        _manager.DoSerialization();

        Assert.Null(RawRestorationData.Children(rawData));
    }

    [Fact]
    public void RestorationState_TogglingTheIdBetweenNullAndNonNullMovesTheDataAndTheBucket()
    {
        var rawData = RawRestorationData.Build(
            children: new Dictionary<object, object?>
            {
                ["widget"] = RawRestorationData.Build(
                    values: new Dictionary<object, object?> { ["foo"] = 22 }),
            });
        var root = RestorationBucket.Root(_manager, rawData);
        var property = new TestRestorableProperty(10);
        RestorableWidgetState? state = null;

        using var harness = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: root,
            child: new RestorableWidget(null, property, s => state = s)));

        Assert.Null(state!.Bucket);
        Assert.Equal(new[] { "createDefaultValue", "initWithValue" }, property.Log);
        Assert.Equal(22, RawRestorationData.Values(RawRestorationData.Child(rawData, "widget")!)!["foo"]);

        property.Log.Clear();
        harness.Update(new UnmanagedRestorationScope(
            bucket: root,
            child: new RestorableWidget("widget", property)));
        _manager.DoSerialization();

        Assert.NotNull(state.Bucket);
        Assert.Equal(new[] { "toPrimitives" }, property.Log);
        Assert.Single(state.RestoreStateLog);
        Assert.Null(Assert.Single(state.ToggleBucketLog));
        Assert.Equal(10, RawRestorationData.Values(RawRestorationData.Child(rawData, "widget")!)!["foo"]);

        property.Log.Clear();
        RestorationBucket? claimed = state.Bucket;
        harness.Update(new UnmanagedRestorationScope(
            bucket: root,
            child: new RestorableWidget(null, property)));
        _manager.DoSerialization();

        Assert.Null(state.Bucket);
        Assert.Empty(property.Log);
        Assert.Single(state.RestoreStateLog);
        Assert.Equal(2, state.ToggleBucketLog.Count);
        Assert.Same(claimed, state.ToggleBucketLog[1]);
        Assert.Null(RawRestorationData.Children(rawData));
    }

    [Fact]
    public void RestorationState_RestoresAgainWhenTheHostReplacesTheRestorationData()
    {
        var manager = new TestRestorationManager { Data = RawRestorationData.Build() };
        RestorationManager.Instance = manager;
        var property = new TestRestorableProperty(10);
        RestorableWidgetState? state = null;

        using var harness = new RestorationHarness(new RootRestorationScope(
            restorationId: "root-child",
            child: new RestorableWidget("widget", property, s => state = s)));
        RestorationBucket? oldBucket = state!.Bucket;
        Assert.Equal(10, property.Value);
        property.Log.Clear();

        manager.RespondWith(
            enabled: true,
            data: RawRestorationData.Build(
                children: new Dictionary<object, object?>
                {
                    ["root-child"] = RawRestorationData.Build(
                        children: new Dictionary<object, object?>
                        {
                            ["widget"] = RawRestorationData.Build(
                                values: new Dictionary<object, object?> { ["foo"] = 42 }),
                        }),
                }));
        harness.FlushBuild();

        Assert.Equal(new[] { "fromPrimitives", "initWithValue" }, property.Log);
        Assert.Equal(42, property.Value);
        Assert.Equal(2, state.RestoreStateLog.Count);
        Assert.Same(oldBucket, state.RestoreStateLog[1]);
        Assert.False(state.InitialRestoreLog[1]);
        Assert.Empty(state.ToggleBucketLog);
    }

    [Fact]
    public void RestorationState_CannotRegisterTheSamePropertyTwice()
    {
        var root = RestorationBucket.Root(_manager, RawRestorationData.Build());
        var property = new TestRestorableProperty(10);
        RestorableWidgetState? state = null;

        using var harness = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: root,
            child: new RestorableWidget("widget", property, s => state = s)));

        Assert.Throws<InvalidOperationException>(() => state!.RegisterAdditional(property, "other"));
    }

    [Fact]
    public void RestorationState_CannotRegisterTwoPropertiesUnderTheSameId()
    {
        var root = RestorationBucket.Root(_manager, RawRestorationData.Build());
        RestorableWidgetState? state = null;

        using var harness = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: root,
            child: new RestorableWidget("widget", new TestRestorableProperty(10), s => state = s)));

        Assert.Throws<InvalidOperationException>(
            () => state!.RegisterAdditional(new TestRestorableProperty(1), "foo"));
    }

    [Fact]
    public void RestorationState_DisabledPropertyDataIsRemovedAndRestoredWhenReEnabled()
    {
        var root = RestorationBucket.Root(_manager, RawRestorationData.Build());
        var property = new TestRestorableProperty(10);
        RestorableWidgetState? state = null;

        using var harness = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: root,
            child: new RestorableWidget("widget", property, s => state = s)));
        Assert.True(state!.Bucket!.Contains("foo"));

        property.SetEnabled(false);
        Assert.False(state.Bucket.Contains("foo"));

        property.Log.Clear();
        property.Value = 30;
        Assert.Empty(property.Log);
        Assert.False(state.Bucket.Contains("foo"));

        property.Log.Clear();
        property.SetEnabled(true);
        Assert.Equal(new[] { "toPrimitives" }, property.Log);
        Assert.Equal(30, state.Bucket.Read<int>("foo"));
    }

    [Fact]
    public void RestorationState_UnregisteringAPropertyRemovesItsData()
    {
        var root = RestorationBucket.Root(_manager, RawRestorationData.Build());
        var extra = new TestRestorableProperty(11);
        RestorableWidgetState? state = null;

        using var harness = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: root,
            child: new RestorableWidget("widget", new TestRestorableProperty(10), s => state = s)));
        state!.RegisterAdditional(extra, "additional");
        Assert.Equal(11, state.Bucket!.Read<int>("additional"));

        state.UnregisterAdditional(extra);

        Assert.False(state.Bucket.Contains("additional"));
    }

    [Fact]
    public void RestorationState_DisposingAPropertyUnregistersItButKeepsItsData()
    {
        var root = RestorationBucket.Root(_manager, RawRestorationData.Build());
        var extra = new TestRestorableProperty(11);
        RestorableWidgetState? state = null;

        using var harness = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: root,
            child: new RestorableWidget("widget", new TestRestorableProperty(10), s => state = s)));
        state!.RegisterAdditional(extra, "additional");

        extra.Dispose();
        Assert.Equal(11, state.Bucket!.Read<int>("additional"));

        var replacement = new TestRestorableProperty(22);
        state.RegisterAdditional(replacement, "additional");
        Assert.Equal(11, replacement.Value);
    }

    [Fact]
    public void RestorableProperty_ThrowsWhenDisposedTwice()
    {
        var property = new TestRestorableProperty(10);
        property.Dispose();

        Assert.Throws<ObjectDisposedException>(property.Dispose);
    }

    [Fact]
    public void RestorationState_ThrowsWhenAPreviouslyRegisteredPropertyIsNotReRegistered()
    {
        var manager = new TestRestorationManager { Data = RawRestorationData.Build() };
        RestorationManager.Instance = manager;
        var extra = new TestRestorableProperty(11);
        RestorableWidgetState? state = null;

        using var harness = new RestorationHarness(new RootRestorationScope(
            restorationId: "root-child",
            child: new RestorableWidget("widget", new TestRestorableProperty(10), s => state = s)));
        state!.RegisterAdditional(extra, "additional");

        manager.RespondWith(enabled: true, data: RawRestorationData.Build());

        Assert.Throws<InvalidOperationException>(harness.FlushBuild);
    }
}
