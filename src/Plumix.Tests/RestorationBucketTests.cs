using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

/// <summary>
/// Ports Flutter's <c>test/services/restoration_bucket_test.dart</c> and
/// <c>test/services/restoration_test.dart</c>.
/// </summary>
[Collection(SchedulerTestCollection.Name)]
public sealed class RestorationBucketTests : IDisposable
{
    private readonly MockRestorationManager _manager = new();

    public RestorationBucketTests() => Scheduler.ResetForTests();

    public void Dispose() => Scheduler.ResetForTests();

    [Fact]
    public void RootBucket_ExposesIdAndOwner()
    {
        var bucket = RestorationBucket.Root(_manager, RawRestorationData.Build());

        Assert.Equal("root", bucket.RestorationId);
        Assert.Same(_manager, bucket.DebugOwner);
        Assert.False(bucket.IsReplacing);
    }

    [Fact]
    public void Read_DoesNotScheduleSerialization()
    {
        var bucket = RestorationBucket.Root(
            _manager,
            RawRestorationData.Build(values: new Dictionary<object, object?> { ["value1"] = 10 }));

        Assert.Equal(10, bucket.Read<int>("value1"));
        Assert.True(bucket.Contains("value1"));
        Assert.Empty(_manager.Scheduled);
    }

    [Fact]
    public void Write_StoresValueAndSchedulesSerializationOnlyWhenItChanges()
    {
        var rawData = RawRestorationData.Build();
        var bucket = RestorationBucket.Root(_manager, rawData);

        bucket.Write("value1", 22);
        Assert.Same(bucket, Assert.Single(_manager.Scheduled));
        _manager.DoSerialization();
        Assert.Equal(22, RawRestorationData.Values(rawData)!["value1"]);

        bucket.Write("value1", 22);
        Assert.Empty(_manager.Scheduled);

        bucket.Write("value1", 23);
        Assert.Same(bucket, Assert.Single(_manager.Scheduled));
        _manager.DoSerialization();
        Assert.Equal(23, bucket.Read<int>("value1"));
    }

    [Fact]
    public void Write_NullIsStoredAsAnExplicitValue()
    {
        var bucket = RestorationBucket.Root(_manager, RawRestorationData.Build());

        bucket.Write<object?>("value1", null);

        Assert.True(bucket.Contains("value1"));
        Assert.Null(bucket.Read<object>("value1"));
        Assert.Same(bucket, Assert.Single(_manager.Scheduled));
    }

    [Fact]
    public void Remove_ReturnsAndDeletesValueAndDropsTheValuesMapWhenEmpty()
    {
        var rawData = RawRestorationData.Build(
            values: new Dictionary<object, object?> { ["value1"] = 10 });
        var bucket = RestorationBucket.Root(_manager, rawData);

        Assert.Equal(10, bucket.Remove<int>("value1"));
        Assert.Same(bucket, Assert.Single(_manager.Scheduled));
        _manager.DoSerialization();
        Assert.Null(RawRestorationData.Values(rawData));
        Assert.False(bucket.Contains("value1"));
    }

    [Fact]
    public void Remove_MissingValueDoesNotScheduleSerialization()
    {
        var bucket = RestorationBucket.Root(_manager, RawRestorationData.Build());

        Assert.Equal(0, bucket.Remove<int>("value1"));
        Assert.Empty(_manager.Scheduled);
    }

    [Fact]
    public void ChildBucket_ReadsAndWritesThroughTheParentRawData()
    {
        var rawData = RawRestorationData.Build(
            children: new Dictionary<object, object?>
            {
                ["child1"] = RawRestorationData.Build(
                    values: new Dictionary<object, object?> { ["foo"] = 10 }),
            });
        var root = RestorationBucket.Root(_manager, rawData);
        var child = root.ClaimChild("child1", debugOwner: "owner");

        Assert.Equal(10, child.Read<int>("foo"));
        child.Write("bar", 20);
        _manager.DoSerialization();

        Assert.Equal(20, RawRestorationData.Values(RawRestorationData.Child(rawData, "child1")!)!["bar"]);
    }

    [Fact]
    public void ClaimChild_WithExistingDataDoesNotScheduleSerialization()
    {
        var rawData = RawRestorationData.Build(
            children: new Dictionary<object, object?>
            {
                ["child1"] = RawRestorationData.Build(
                    values: new Dictionary<object, object?> { ["foo"] = 10 }),
            });
        var root = RestorationBucket.Root(_manager, rawData);

        var child = root.ClaimChild("child1", debugOwner: "owner");

        Assert.Equal(10, child.Read<int>("foo"));
        Assert.Empty(_manager.Scheduled);
    }

    [Fact]
    public void ClaimChild_WithoutExistingDataCreatesAnEmptyBucketAndSchedulesSerialization()
    {
        var rawData = RawRestorationData.Build();
        var root = RestorationBucket.Root(_manager, rawData);

        var child = root.ClaimChild("child1", debugOwner: "owner");
        Assert.Equal("child1", child.RestorationId);
        Assert.Same(root, Assert.Single(_manager.Scheduled));
        _manager.DoSerialization();

        Assert.NotNull(RawRestorationData.Child(rawData, "child1"));
    }

    [Fact]
    public void ClaimChild_TwiceThrowsOnFinalizeWhenTheIdIsNotGivenUp()
    {
        var root = RestorationBucket.Root(_manager, RawRestorationData.Build());
        root.ClaimChild("child1", debugOwner: "FirstClaim");
        root.ClaimChild("child1", debugOwner: "SecondClaim");

        var error = Assert.Throws<InvalidOperationException>(_manager.DoSerialization);

        Assert.Contains("Multiple owners claimed child RestorationBuckets with the same IDs.", error.Message);
        Assert.Contains("\"child1\" was claimed by:", error.Message);
        Assert.Contains("SecondClaim", error.Message);
        Assert.Contains("FirstClaim (current owner)", error.Message);
        Assert.Contains("MockManager", error.Message);
    }

    [Fact]
    public void ClaimChild_TwiceIsFineWhenTheFirstOwnerGivesTheIdUp()
    {
        var rawData = RawRestorationData.Build();
        var root = RestorationBucket.Root(_manager, RawRestorationData.Build());
        var first = root.ClaimChild("child1", debugOwner: "FirstClaim");
        first.Write("foo", 10);
        var second = root.ClaimChild("child1", debugOwner: "SecondClaim");
        second.Write("bar", 55);

        first.Dispose();
        _manager.DoSerialization();

        Assert.Equal(55, second.Read<int>("bar"));
        Assert.Equal(0, second.Read<int>("foo"));
        Assert.False(second.Contains("foo"));
        Assert.Empty(rawData);
    }

    [Fact]
    public void ClaimChild_ThreeTimesStillThrowsWhenOnlyOneOwnerGivesTheIdUp()
    {
        var root = RestorationBucket.Root(_manager, RawRestorationData.Build());
        var first = root.ClaimChild("child1", debugOwner: "FirstClaim");
        root.ClaimChild("child1", debugOwner: "SecondClaim");
        root.ClaimChild("child1", debugOwner: "ThirdClaim");

        first.Dispose();

        Assert.Throws<InvalidOperationException>(_manager.DoSerialization);
    }

    [Fact]
    public void UnclaimingAndReclaimingTheSameIdGivesAFreshBucket()
    {
        var root = RestorationBucket.Root(
            _manager,
            RawRestorationData.Build(
                children: new Dictionary<object, object?>
                {
                    ["child1"] = RawRestorationData.Build(
                        values: new Dictionary<object, object?> { ["foo"] = 10 }),
                }));

        var first = root.ClaimChild("child1", debugOwner: "first");
        Assert.Equal(10, first.Read<int>("foo"));
        first.Dispose();

        var second = root.ClaimChild("child1", debugOwner: "second");
        Assert.False(second.Contains("foo"));
    }

    [Fact]
    public void DisposingAChildRemovesItsDataRecursively()
    {
        var rawData = RawRestorationData.Build();
        var root = RestorationBucket.Root(_manager, rawData);
        var child = root.ClaimChild("child1", debugOwner: "owner");
        var grandChild = child.ClaimChild("grand", debugOwner: "owner");
        grandChild.Write("foo", 10);
        _manager.DoSerialization();
        Assert.NotNull(RawRestorationData.Child(RawRestorationData.Child(rawData, "child1")!, "grand"));

        child.Dispose();
        _manager.DoSerialization();

        Assert.Null(RawRestorationData.Children(rawData));
    }

    [Fact]
    public void Rename_ToTheSameIdIsANoOp()
    {
        var root = RestorationBucket.Root(_manager, RawRestorationData.Build());
        var child = root.ClaimChild("child1", debugOwner: "owner");
        _manager.DoSerialization();

        child.Rename("child1");

        Assert.Empty(_manager.Scheduled);
    }

    [Fact]
    public void Rename_MovesTheRawDataToTheNewId()
    {
        var rawData = RawRestorationData.Build();
        var root = RestorationBucket.Root(_manager, rawData);
        var child = root.ClaimChild("child1", debugOwner: "owner");
        child.Write("foo", 10);
        _manager.DoSerialization();
        var childRawData = RawRestorationData.Child(rawData, "child1");

        child.Rename("child2");
        _manager.DoSerialization();

        Assert.Equal("child2", child.RestorationId);
        Assert.Null(RawRestorationData.Child(rawData, "child1"));
        Assert.Same(childRawData, RawRestorationData.Child(rawData, "child2"));
    }

    [Fact]
    public void Rename_OntoAUsedIdThrowsWhenTheOwnerKeepsIt()
    {
        var root = RestorationBucket.Root(_manager, RawRestorationData.Build());
        root.ClaimChild("child2", debugOwner: "occupant");
        var child = root.ClaimChild("child1", debugOwner: "owner");
        child.Write("foo", 10);

        child.Rename("child2");

        Assert.Throws<InvalidOperationException>(_manager.DoSerialization);
    }

    [Fact]
    public void Rename_OntoAUsedIdSucceedsWhenTheOwnerGivesItUp()
    {
        var rawData = RawRestorationData.Build();
        var root = RestorationBucket.Root(_manager, rawData);
        var occupant = root.ClaimChild("child2", debugOwner: "occupant");
        var child = root.ClaimChild("child1", debugOwner: "owner");
        child.Write("foo", 10);

        child.Rename("child2");
        occupant.Dispose();
        _manager.DoSerialization();

        Assert.Equal(10, child.Read<int>("foo"));
        Assert.Null(RawRestorationData.Child(rawData, "child1"));
        Assert.Equal(10, RawRestorationData.Values(RawRestorationData.Child(rawData, "child2")!)!["foo"]);
    }

    [Fact]
    public void Rename_MovesAPendingChildOutOfTheWaitingList()
    {
        var rawData = RawRestorationData.Build();
        var root = RestorationBucket.Root(_manager, rawData);
        var first = root.ClaimChild("child1", debugOwner: "first");
        first.Write("foo", 10);
        var pending = root.ClaimChild("child1", debugOwner: "pending");

        pending.Rename("child2");
        _manager.DoSerialization();

        Assert.NotNull(RawRestorationData.Child(rawData, "child2"));
        Assert.Empty(RawRestorationData.Child(rawData, "child2")!);
        Assert.Equal(10, first.Read<int>("foo"));
    }

    [Fact]
    public void AdoptChild_IsANoOpForAnExistingChild()
    {
        var root = RestorationBucket.Root(_manager, RawRestorationData.Build());
        var child = root.ClaimChild("child1", debugOwner: "owner");
        _manager.DoSerialization();

        root.AdoptChild(child);

        Assert.Empty(_manager.Scheduled);
    }

    [Fact]
    public void AdoptChild_InsertsAFreshBucketAndPropagatesTheManager()
    {
        var rawData = RawRestorationData.Build();
        var root = RestorationBucket.Root(_manager, rawData);
        var orphan = RestorationBucket.Empty("child1", debugOwner: "owner");

        root.AdoptChild(orphan);
        Assert.Same(root, Assert.Single(_manager.Scheduled));
        _manager.DoSerialization();

        orphan.Write("foo", 10);
        Assert.Same(orphan, Assert.Single(_manager.Scheduled));
        _manager.DoSerialization();
        Assert.Equal(10, RawRestorationData.Values(RawRestorationData.Child(rawData, "child1")!)!["foo"]);
    }

    [Fact]
    public void AdoptChild_MovesTheDataFromTheOldParent()
    {
        var oldRaw = RawRestorationData.Build();
        var newRaw = RawRestorationData.Build();
        var oldRoot = RestorationBucket.Root(_manager, oldRaw);
        var newRoot = RestorationBucket.Root(_manager, newRaw);
        var child = oldRoot.ClaimChild("child1", debugOwner: "owner");
        child.Write("foo", 10);
        _manager.DoSerialization();
        var childRaw = RawRestorationData.Child(oldRaw, "child1");

        newRoot.AdoptChild(child);
        _manager.DoSerialization();

        Assert.Null(RawRestorationData.Children(oldRaw));
        Assert.Same(childRaw, RawRestorationData.Child(newRaw, "child1"));
        Assert.Equal(10, child.Read<int>("foo"));
    }

    [Fact]
    public void Bucket_ThrowsWhenUsedAfterDispose()
    {
        var root = RestorationBucket.Root(_manager, RawRestorationData.Build());
        var bucket = root.ClaimChild("child1", debugOwner: "owner");
        bucket.Dispose();

        Assert.Throws<InvalidOperationException>(() => bucket.DebugOwner);
        Assert.Throws<InvalidOperationException>(() => bucket.RestorationId);
        Assert.Throws<InvalidOperationException>(() => bucket.Read<int>("foo"));
        Assert.Throws<InvalidOperationException>(() => bucket.Write("foo", 10));
        Assert.Throws<InvalidOperationException>(() => bucket.Remove<int>("foo"));
        Assert.Throws<InvalidOperationException>(() => bucket.Contains("foo"));
        Assert.Throws<InvalidOperationException>(() => bucket.ClaimChild("child", debugOwner: null));
        Assert.Throws<InvalidOperationException>(() => bucket.AdoptChild(root));
        Assert.Throws<InvalidOperationException>(() => bucket.Rename("other"));
        Assert.Throws<InvalidOperationException>(bucket.Dispose);
    }
}
