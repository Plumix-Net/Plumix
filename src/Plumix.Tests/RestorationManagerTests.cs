using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

/// <summary>Ports Flutter's <c>test/services/restoration_test.dart</c> manager coverage.</summary>
[Collection(SchedulerTestCollection.Name)]
public sealed class RestorationManagerTests : IDisposable
{
    public RestorationManagerTests() => Scheduler.ResetForTests();

    public void Dispose() => Scheduler.ResetForTests();

    [Fact]
    public void RootBucket_IsRetrievedFromTheEngineOnceAndCarriesItsData()
    {
        var manager = new TestRestorationManager
        {
            Data = RawRestorationData.Build(
                values: new Dictionary<object, object?> { ["value1"] = 10, ["value2"] = "Hello" },
                children: new Dictionary<object, object?>
                {
                    ["child1"] = RawRestorationData.Build(
                        values: new Dictionary<object, object?> { ["another value"] = 22 }),
                }),
        };

        RestorationBucket? bucket = null;
        manager.GetRootBucket(value => bucket = value);

        Assert.True(manager.EngineQueried);
        Assert.NotNull(bucket);
        Assert.Equal(10, bucket!.Read<int>("value1"));
        Assert.Equal("Hello", bucket.Read<string>("value2"));
        Assert.Equal(22, bucket.ClaimChild("child1", debugOwner: null).Read<int>("another value"));
    }

    [Fact]
    public void RootBucket_ResolvesSynchronouslyAfterTheFirstUpdate()
    {
        var manager = new TestRestorationManager { Data = RawRestorationData.Build() };
        RestorationBucket? first = null;
        manager.GetRootBucket(value => first = value);

        bool ranSynchronously = false;
        RestorationBucket? second = null;
        manager.GetRootBucket(value =>
        {
            second = value;
            ranSynchronously = true;
        });

        Assert.True(ranSynchronously);
        Assert.Same(first, second);
        Assert.Equal(2, manager.RootBucketAccessed);
    }

    [Fact]
    public void RootBucket_ReceivedBeforeRetrievalNeverQueriesTheEngine()
    {
        var manager = new TestRestorationManager();
        manager.RespondWith(
            enabled: true,
            data: RawRestorationData.Build(values: new Dictionary<object, object?> { ["foo"] = 33 }));

        RestorationBucket? bucket = null;
        manager.GetRootBucket(value => bucket = value);

        Assert.False(manager.EngineQueried);
        Assert.Equal(33, bucket!.Read<int>("foo"));
    }

    [Fact]
    public void RootBucket_ReceivedWhileTheRequestIsPendingCompletesIt()
    {
        var manager = new TestRestorationManager { AnswerSynchronously = false };
        RestorationBucket? bucket = null;
        manager.GetRootBucket(value => bucket = value);
        Assert.True(manager.EngineQueried);
        Assert.Null(bucket);

        manager.RespondWith(
            enabled: true,
            data: RawRestorationData.Build(values: new Dictionary<object, object?> { ["foo"] = 33 }));

        Assert.Equal(33, bucket!.Read<int>("foo"));
    }

    [Fact]
    public void RootBucket_IsReplacedWhenNewDataArrives()
    {
        var manager = new TestRestorationManager
        {
            Data = RawRestorationData.Build(values: new Dictionary<object, object?> { ["value1"] = 10 }),
        };
        RestorationBucket? bucket = null;
        manager.GetRootBucket(value => bucket = value);
        RestorationBucket? oldBucket = bucket;

        int notifications = 0;
        RestorationBucket? bucketDuringNotification = null;
        manager.AddListener(() =>
        {
            notifications++;
            manager.GetRootBucket(value => bucketDuringNotification = value);
        });

        manager.RespondWith(
            enabled: true,
            data: RawRestorationData.Build(
                values: new Dictionary<object, object?> { ["foo"] = 33 },
                children: new Dictionary<object, object?>
                {
                    ["childFoo"] = RawRestorationData.Build(
                        values: new Dictionary<object, object?> { ["bar"] = "Hello" }),
                }));

        Assert.Equal(1, notifications);
        Assert.NotSame(oldBucket, bucketDuringNotification);
        Assert.Equal(33, bucketDuringNotification!.Read<int>("foo"));
        Assert.Equal(0, bucketDuringNotification.Read<int>("value1"));
        Assert.Equal(
            "Hello",
            bucketDuringNotification.ClaimChild("childFoo", debugOwner: null).Read<string>("bar"));
    }

    [Fact]
    public void RootBucket_IsNullWhenRestorationIsDisabledAndTogglingNotifies()
    {
        var manager = new TestRestorationManager { Enabled = false };
        int notifications = 0;
        manager.AddListener(() => notifications++);

        RestorationBucket? bucket = null;
        bool delivered = false;
        manager.GetRootBucket(value =>
        {
            bucket = value;
            delivered = true;
        });

        Assert.True(delivered);
        Assert.Null(bucket);
        Assert.Equal(0, notifications);

        manager.RespondWith(enabled: true, data: RawRestorationData.Build());
        Assert.Equal(1, notifications);

        manager.RespondWith(enabled: false, data: null);
        Assert.Equal(2, notifications);
    }

    [Fact]
    public void FlushData_IsANoOpWhileAFrameIsScheduledAndSendsAtTheEndOfIt()
    {
        var manager = new TestRestorationManager { Data = RawRestorationData.Build() };
        RestorationBucket? bucket = null;
        manager.GetRootBucket(value => bucket = value);

        Scheduler.ScheduleFrame();
        bucket!.Write("foo", 10);
        manager.FlushData();
        Assert.Empty(manager.SentToEngine);

        Scheduler.PumpFrameForTests();

        Dictionary<object, object?> sent = Assert.Single(manager.SentToEngine);
        Assert.Equal(10, RawRestorationData.Values(sent)!["foo"]);
    }

    [Fact]
    public void FlushData_SendsImmediatelyWhenNoFrameIsScheduled()
    {
        var manager = new TestRestorationManager { Data = RawRestorationData.Build() };
        RestorationBucket? bucket = null;
        manager.GetRootBucket(value => bucket = value);

        bucket!.Write("foo", 10);
        manager.FlushData();

        Dictionary<object, object?> sent = Assert.Single(manager.SentToEngine);
        Assert.Equal(10, RawRestorationData.Values(sent)!["foo"]);
    }

    [Fact]
    public void IsReplacing_IsOnlyTrueForTheFrameAfterTheRootBucketIsReplaced()
    {
        var manager = new TestRestorationManager { Data = RawRestorationData.Build() };
        RestorationBucket? bucket = null;
        manager.GetRootBucket(value => bucket = value);

        Assert.False(manager.IsReplacing);
        Assert.False(bucket!.IsReplacing);

        manager.RespondWith(enabled: true, data: null);
        RestorationBucket? newBucket = null;
        manager.GetRootBucket(value => newBucket = value);

        Assert.NotSame(bucket, newBucket);
        Assert.True(manager.IsReplacing);
        Assert.True(newBucket!.IsReplacing);

        Scheduler.ScheduleFrame();
        Scheduler.PumpFrameForTests();

        Assert.False(manager.IsReplacing);
        Assert.False(newBucket.IsReplacing);
    }

    [Fact]
    public void IsReplacing_StaysFalseWhenRestorationIsTurnedOff()
    {
        var manager = new TestRestorationManager { Data = RawRestorationData.Build() };
        manager.GetRootBucket(_ => { });

        manager.RespondWith(enabled: false, data: null);

        RestorationBucket? bucket = null;
        manager.GetRootBucket(value => bucket = value);
        Assert.Null(bucket);
        Assert.False(manager.IsReplacing);
    }

    [Fact]
    public void DebugIsSerializableForRestoration_AcceptsOnlyTheCodecValueDomain()
    {
        Assert.True(RestorationSerialization.DebugIsSerializableForRestoration(null));
        Assert.True(RestorationSerialization.DebugIsSerializableForRestoration(147823));
        Assert.True(RestorationSerialization.DebugIsSerializableForRestoration(12.43));
        Assert.True(RestorationSerialization.DebugIsSerializableForRestoration(true));
        Assert.True(RestorationSerialization.DebugIsSerializableForRestoration("Hello World"));
        Assert.True(RestorationSerialization.DebugIsSerializableForRestoration(new List<int> { 12, 13, 14 }));
        Assert.True(RestorationSerialization.DebugIsSerializableForRestoration(
            new Dictionary<string, int> { ["v1"] = 10, ["v2"] = 23 }));
        Assert.True(RestorationSerialization.DebugIsSerializableForRestoration(
            new Dictionary<object, object?>
            {
                ["hello"] = new List<object?> { 1, 2, new Dictionary<object, object?> { ["a"] = "b" } },
            }));

        Assert.False(RestorationSerialization.DebugIsSerializableForRestoration(new object()));
        Assert.False(RestorationSerialization.DebugIsSerializableForRestoration(
            new List<object?> { new object() }));
    }

    [Fact]
    public void ScheduleSerializationFor_RejectsBucketsFromAnotherManager()
    {
        var manager = new TestRestorationManager();
        var other = new TestRestorationManager();
        var bucket = RestorationBucket.Root(other, RawRestorationData.Build());

        Assert.Throws<InvalidOperationException>(() => manager.ScheduleSerializationFor(bucket));
        Assert.Throws<InvalidOperationException>(() => manager.UnscheduleSerializationFor(bucket));
    }
}
