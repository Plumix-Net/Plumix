using System.Diagnostics;
using Plumix.Foundation;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/foundation/timeline.dart
// (mirrors flutter/packages/flutter/test/foundation/timeline_test.dart)

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class FlutterTimelineTests : IDisposable
{
    // IMPORTANT: keep this in sync with the same constant defined in foundation/Timeline.cs.
    private const int KSliceSize = 500;

    public FlutterTimelineTests()
    {
        FlutterTimeline.ResetForTests();
        if (!Constants.KReleaseMode)
        {
            FlutterTimeline.DebugReset();
            FlutterTimeline.DebugCollectionEnabled = false;
        }
    }

    public void Dispose() => FlutterTimeline.ResetForTests();

    [NonReleaseFact]
    public void DoesNotCollectWhenCollectionNotEnabled()
    {
        FlutterTimeline.StartSync("TEST");
        FlutterTimeline.FinishSync();
        Assert.Throws<InvalidOperationException>(() => FlutterTimeline.DebugCollect());
    }

    [NonReleaseFact]
    public void CollectsWhenCollectionIsEnabled()
    {
        FlutterTimeline.DebugCollectionEnabled = true;
        FlutterTimeline.StartSync("TEST");
        FlutterTimeline.FinishSync();
        AggregatedTimings data = FlutterTimeline.DebugCollect();
        Assert.Single(data.TimedBlocks);
        Assert.Single(data.AggregatedBlocks);

        AggregatedTimedBlock block = data.GetAggregated("TEST");
        Assert.Equal("TEST", block.Name);
        Assert.Equal(1, block.Count);

        // After collection the timeline is reset back to empty.
        AggregatedTimings data2 = FlutterTimeline.DebugCollect();
        Assert.Empty(data2.TimedBlocks);
        Assert.Empty(data2.AggregatedBlocks);
    }

    [NonReleaseFact]
    public void DeletesOldDataWhenReset()
    {
        FlutterTimeline.DebugCollectionEnabled = true;
        FlutterTimeline.StartSync("TEST");
        FlutterTimeline.FinishSync();
        FlutterTimeline.DebugReset();

        AggregatedTimings data = FlutterTimeline.DebugCollect();
        Assert.Empty(data.TimedBlocks);
        Assert.Empty(data.AggregatedBlocks);
    }

    [NonReleaseFact]
    public void ReportsZeroAggregationWhenRequestedMissingBlock()
    {
        FlutterTimeline.DebugCollectionEnabled = true;

        AggregatedTimings data = FlutterTimeline.DebugCollect();
        AggregatedTimedBlock block = data.GetAggregated("MISSING");
        Assert.Equal("MISSING", block.Name);
        Assert.Equal(0, block.Count);
        Assert.Equal(0, block.Duration);
    }

    [NonReleaseFact]
    public void MeasuresTheRuntimeOfAFunction()
    {
        FlutterTimeline.DebugCollectionEnabled = true;

        long start = FlutterTimeline.Now - 1;
        FlutterTimeline.TimeSync("TEST", () =>
        {
            var watch = Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < 5)
            {
            }

            watch.Stop();
        });
        long end = FlutterTimeline.Now + 1;

        AggregatedTimings data = FlutterTimeline.DebugCollect();
        Assert.Single(data.TimedBlocks);
        Assert.Single(data.AggregatedBlocks);

        TimedBlock block = data.TimedBlocks.Single();
        Assert.Equal("TEST", block.Name);
        Assert.True(block.Start >= start);
        Assert.True(block.End <= end);
        Assert.True(block.Duration > 0);

        AggregatedTimedBlock aggregated = data.GetAggregated("TEST");
        Assert.Equal("TEST", aggregated.Name);
        Assert.Equal(1, aggregated.Count);
        Assert.Equal(block.Duration, aggregated.Duration);
    }

    [NonReleaseFact]
    public void InstantSyncDoesNotCollectAnything()
    {
        FlutterTimeline.DebugCollectionEnabled = true;
        FlutterTimeline.InstantSync("TEST");

        AggregatedTimings data = FlutterTimeline.DebugCollect();
        Assert.Empty(data.TimedBlocks);
        Assert.Empty(data.AggregatedBlocks);
    }

    [NonReleaseFact]
    public void NowReturnsAValue()
    {
        FlutterTimeline.DebugCollectionEnabled = true;
        Assert.True(FlutterTimeline.Now > 0);
    }

    [NonReleaseFact]
    public void CanCollectMoreThanOneSliceOfData()
    {
        FlutterTimeline.DebugCollectionEnabled = true;

        for (int i = 0; i < 10 * KSliceSize; i++)
        {
            FlutterTimeline.StartSync("TEST");
            FlutterTimeline.FinishSync();
        }

        AggregatedTimings data = FlutterTimeline.DebugCollect();
        Assert.Equal(10 * KSliceSize, data.TimedBlocks.Count);
        Assert.Single(data.AggregatedBlocks);

        AggregatedTimedBlock block = data.GetAggregated("TEST");
        Assert.Equal("TEST", block.Name);
        Assert.Equal(10 * KSliceSize, block.Count);
    }

    [NonReleaseFact]
    public void CollectsBlocksInACorrectOrder()
    {
        FlutterTimeline.DebugCollectionEnabled = true;
        const int testCount = 7 * KSliceSize / 2;

        for (int i = 0; i < testCount; i++)
        {
            FlutterTimeline.StartSync($"TEST{i}");
            FlutterTimeline.FinishSync();
        }

        AggregatedTimings data = FlutterTimeline.DebugCollect();
        Assert.Equal(testCount, data.TimedBlocks.Count);
        Assert.Equal(
            Enumerable.Range(0, testCount).Select(i => $"TEST{i}"),
            data.TimedBlocks.Select(block => block.Name));
    }

    // Beyond timeline_test.dart: the contracts of timeline.dart itself.

    [NonReleaseFact]
    public void NestedBlocksAreRecordedInFinishOrderAndAggregatedByName()
    {
        FlutterTimeline.DebugCollectionEnabled = true;
        FlutterTimeline.StartSync("OUTER");
        FlutterTimeline.StartSync("INNER");
        FlutterTimeline.FinishSync();
        FlutterTimeline.StartSync("INNER");
        FlutterTimeline.FinishSync();
        FlutterTimeline.FinishSync();

        AggregatedTimings data = FlutterTimeline.DebugCollect();
        Assert.Equal(["INNER", "INNER", "OUTER"], data.TimedBlocks.Select(block => block.Name));
        TimedBlock outer = data.TimedBlocks[2];
        Assert.All(data.TimedBlocks.Take(2), inner =>
        {
            Assert.True(inner.Start >= outer.Start);
            Assert.True(inner.End <= outer.End);
        });
        Assert.Equal(["INNER", "OUTER"], data.AggregatedBlocks.Select(block => block.Name));
        Assert.Equal(2, data.GetAggregated("INNER").Count);
    }

    [NonReleaseFact]
    public void TimeSyncReturnsTheResultAndFinishesTheBlockWhenTheFunctionThrows()
    {
        FlutterTimeline.DebugCollectionEnabled = true;
        Assert.Equal(42, FlutterTimeline.TimeSync("RESULT", () => 42));
        Assert.Throws<InvalidOperationException>(() =>
            FlutterTimeline.TimeSync("THROWS", () => throw new InvalidOperationException("boom")));

        AggregatedTimings data = FlutterTimeline.DebugCollect();
        Assert.Equal(["RESULT", "THROWS"], data.TimedBlocks.Select(block => block.Name));
    }

    [NonReleaseFact]
    public void ChangingCollectionEnabledResetsTheBuffer()
    {
        FlutterTimeline.DebugCollectionEnabled = true;
        FlutterTimeline.StartSync("TEST");
        FlutterTimeline.FinishSync();
        FlutterTimeline.DebugCollectionEnabled = false;
        FlutterTimeline.DebugCollectionEnabled = true;

        Assert.Empty(FlutterTimeline.DebugCollect().TimedBlocks);
    }

    [DebugOnlyFact]
    public void DebugCollectWithUnfinishedBlocksAsserts()
    {
        FlutterTimeline.DebugCollectionEnabled = true;
        FlutterTimeline.StartSync("PENDING");
        try
        {
            AssertionError error = Assert.Throws<AssertionError>(() => FlutterTimeline.DebugCollect());
            Assert.Contains("waiting to be finished via the `finishSync` method:\nPENDING", error.Message);
        }
        finally
        {
            FlutterTimeline.FinishSync();
        }
    }

    [DebugOnlyFact]
    public void FinishSyncWithoutStartSyncAsserts()
    {
        FlutterTimeline.DebugCollectionEnabled = true;
        Assert.Throws<InvalidOperationException>(() => FlutterTimeline.FinishSync());
    }

    [Fact]
    public void ToStringMatchesDart()
    {
        Assert.Equal("TimedBlock(a, 1.0, 3.5, 2.5)", new TimedBlock("a", 1, 3.5).ToString());
        Assert.Equal("AggregatedTimedBlock(a, 2.5, 3)", new AggregatedTimedBlock("a", 2.5, 3).ToString());
    }

    [Fact]
    public void CollectionIsNotSupportedInReleaseMode()
    {
        if (!Constants.KReleaseMode)
        {
            return;
        }

        Assert.Throws<InvalidOperationException>(() => FlutterTimeline.DebugCollectionEnabled = true);
        Assert.Throws<InvalidOperationException>(() => FlutterTimeline.DebugCollect());
        Assert.Throws<InvalidOperationException>(() => FlutterTimeline.DebugReset());
    }
}
