using Avalonia;
using Plumix;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Physics;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/draggable_scrollable_sheet.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class DraggableScrollableSheetTests
{
    private const double ScreenHeight = 600.0;
    private const double ScreenWidth = 800.0;

    // ---------- Defaults and construction ----------

    [Fact]
    public void Defaults_MatchFlutter()
    {
        var sheet = new DraggableScrollableSheet(builder: (_, _) => new SizedBox());

        Assert.Equal(0.5, sheet.InitialChildSize);
        Assert.Equal(0.25, sheet.MinChildSize);
        Assert.Equal(1.0, sheet.MaxChildSize);
        Assert.True(sheet.Expand);
        Assert.False(sheet.Snap);
        Assert.Null(sheet.SnapSizes);
        Assert.Null(sheet.SnapAnimationDuration);
        Assert.Null(sheet.Controller);
        Assert.True(sheet.ShouldCloseOnMinExtent);
    }

    [Theory]
    [InlineData(-0.1, 0.5, 1.0)] // minChildSize < 0
    [InlineData(0.25, 0.5, 1.1)] // maxChildSize > 1
    [InlineData(0.6, 0.5, 1.0)] // minChildSize > initialChildSize
    [InlineData(0.25, 0.9, 0.8)] // initialChildSize > maxChildSize
    public void Constructor_RejectsOutOfOrderSizes(double minSize, double initialSize, double maxSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DraggableScrollableSheet(
            builder: (_, _) => new SizedBox(),
            minChildSize: minSize,
            initialChildSize: initialSize,
            maxChildSize: maxSize));
    }

    [Fact]
    public void Constructor_RejectsNonPositiveSnapAnimationDuration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DraggableScrollableSheet(
            builder: (_, _) => new SizedBox(),
            snapAnimationDuration: TimeSpan.Zero));
    }

    // ---------- Layout ----------

    [Fact]
    public void Layout_UsesInitialChildSizeOfAvailableHeight()
    {
        using var harness = new SheetHarness(initialChildSize: 0.25, maxChildSize: 0.6);

        Assert.Equal(new Rect(0, 450, ScreenWidth, 150), harness.SheetRect());
    }

    [Fact]
    public void Layout_ExpandFalseDoesNotFillTheParent()
    {
        using var expanded = new SheetHarness(initialChildSize: 0.25, expand: true, looseParent: true);
        using var collapsed = new SheetHarness(initialChildSize: 0.25, expand: false, looseParent: true);

        // Under loose constraints `expand` is what forces the sheet's box to take the whole parent;
        // without it the box is exactly its fraction of the available height.
        Assert.Equal(ScreenHeight, expanded.SheetBoxHeight(), 3);
        Assert.Equal(150.0, collapsed.SheetBoxHeight(), 3);
    }

    [Theory]
    [InlineData(0.6)]
    [InlineData(1.0)]
    public void Drag_MovesTheSheetByTheDraggedPixelsForAnyMaxChildSize(double maxChildSize)
    {
        using var harness = new SheetHarness(initialChildSize: 0.25, maxChildSize: maxChildSize);
        Assert.Equal(new Rect(0, 450, ScreenWidth, 150), harness.SheetRect());

        harness.Drag(-125);

        // The sheet grows by exactly the dragged pixels, regardless of how maxChildSize scales the
        // extent's available pixels.
        Assert.Equal(new Rect(0, 325, ScreenWidth, 275), harness.SheetRect());
    }

    [Fact]
    public void Drag_DownwardShrinksTheSheetWhenNotAtFullHeight()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5);

        harness.Drag(60);

        Assert.Equal(0.4, harness.Controller.Size, 6);
        Assert.Equal(0.0, harness.Position.Pixels);
    }

    [Fact]
    public void Drag_ScrollsTheListOnceTheSheetIsAtItsMaximum()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5);

        // Grows the sheet to its maximum, then keeps dragging: the surplus scrolls the list.
        harness.Drag(-ScreenHeight * 0.5);
        Assert.Equal(1.0, harness.Controller.Size, 6);
        Assert.Equal(0.0, harness.Position.Pixels);

        harness.Drag(-100);
        Assert.Equal(1.0, harness.Controller.Size, 6);
        Assert.Equal(100.0, harness.Position.Pixels, 3);

        // Dragging back down returns the list to its start before the sheet starts shrinking again.
        harness.Drag(100);
        Assert.Equal(0.0, harness.Position.Pixels, 3);
        Assert.Equal(1.0, harness.Controller.Size, 6);

        harness.Drag(60);
        Assert.Equal(0.9, harness.Controller.Size, 6);
    }

    [Fact]
    public void Drag_PastAMinimumOrMaximumBoundIsHandedToTheList()
    {
        using var harness = new SheetHarness(initialChildSize: 0.25, minChildSize: 0.25);

        // At the minimum, a further downward drag is not swallowed by the sheet.
        harness.Drag(50);

        Assert.Equal(0.25, harness.Controller.Size, 6);
    }

    [Fact]
    public void Drag_RespectsNeverScrollableScrollPhysics()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5, physics: new NeverScrollableScrollPhysics());

        // The sheet composes its physics over `AlwaysScrollableScrollPhysics`, but a refusing
        // ancestor still wins, so the scrollable never registers its drag recognizers.
        Assert.False(harness.Position.Physics.ShouldAcceptUserOffset(harness.Position));

        harness.Drag(-300);

        Assert.Equal(0.5, harness.Controller.Size, 6);
        Assert.Equal(0.0, harness.Position.Pixels);
    }

    [Fact]
    public void Drag_IsAcceptedWithoutScrollExtentBecauseTheSheetIsAlwaysScrollable()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5, physics: new ClampingScrollPhysics());

        Assert.True(harness.Position.Physics.ShouldAcceptUserOffset(harness.Position));
    }

    // ---------- Implied snap sizes ----------

    [Fact]
    public void SnapSizes_NullResolvesToMinAndMax()
    {
        using var harness = new SheetHarness(snap: true, snapSizes: null);

        Assert.Equal(new[] { 0.25, 1.0 }, harness.Extent.SnapSizes);
    }

    [Fact]
    public void SnapSizes_EmptyResolvesToMinAndMax()
    {
        using var harness = new SheetHarness(snap: true, snapSizes: []);

        Assert.Equal(new[] { 0.25, 1.0 }, harness.Extent.SnapSizes);
    }

    [Fact]
    public void SnapSizes_MinAndMaxAreImplicitlyAdded()
    {
        using var harness = new SheetHarness(snap: true, snapSizes: [0.5]);

        Assert.Equal(new[] { 0.25, 0.5, 1.0 }, harness.Extent.SnapSizes);
    }

    [Fact]
    public void SnapSizes_AlreadyPresentBoundsAreNotDuplicated()
    {
        using var harness = new SheetHarness(snap: true, snapSizes: [0.25, 0.5, 1.0]);

        Assert.Equal(new[] { 0.25, 0.5, 1.0 }, harness.Extent.SnapSizes);
    }

    [Theory]
    [InlineData(new[] { 0.9 })] // above maxChildSize
    [InlineData(new[] { 0.1 })] // below minChildSize
    [InlineData(new[] { 0.6, 0.6, 0.8 })] // not strictly ascending
    public void SnapSizes_InvalidTargetsThrow(double[] snapSizes)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SheetHarness(snap: true, snapSizes: snapSizes, maxChildSize: 0.8).Dispose());
    }

    [Fact]
    public void SnapSizes_ErrorMessagePointsAtTheInvalidEntry()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SheetHarness(snap: true, snapSizes: [0.5, 0.4], maxChildSize: 1.0).Dispose());

        Assert.Contains(">>> 0.4 <<<", error.Message, StringComparison.Ordinal);
        Assert.Contains("ascending order", error.Message, StringComparison.Ordinal);
    }

    // ---------- Extent math ----------

    [Fact]
    public void Extent_ConvertsBetweenSizeAndPixelsThroughMaxSize()
    {
        var extent = NewExtent(minSize: 0.25, maxSize: 0.5, initialSize: 0.25);
        extent.AvailablePixels = 300.0;

        // availablePixels is already scaled by maxSize, so the conversion divides it back out.
        Assert.Equal(150.0, extent.SizeToPixels(0.25));
        Assert.Equal(0.25, extent.PixelsToSize(150.0));
        Assert.Equal(150.0, extent.CurrentPixels);
    }

    [Fact]
    public void Extent_AddPixelDeltaGrowsForPositiveDeltaAndClampsAtBothBounds()
    {
        var extent = NewExtent(minSize: 0.25, maxSize: 1.0, initialSize: 0.5);
        extent.AvailablePixels = ScreenHeight;

        // The extent grows with a positive delta; the drag's own sign inversion happens one level up,
        // in the scroll position.
        extent.AddPixelDelta(60.0, context: null);
        Assert.Equal(0.6, extent.CurrentSize, 6);

        extent.AddPixelDelta(1000.0, context: null);
        Assert.Equal(1.0, extent.CurrentSize);
        Assert.True(extent.IsAtMax);

        extent.AddPixelDelta(-1000.0, context: null);
        Assert.Equal(0.25, extent.CurrentSize);
        Assert.True(extent.IsAtMin);
    }

    [Fact]
    public void Extent_AddPixelDeltaIsANoOpWithoutAvailablePixels()
    {
        var extent = NewExtent();
        extent.AvailablePixels = 0.0;

        extent.AddPixelDelta(100.0, context: null);

        Assert.Equal(0.5, extent.CurrentSize);
        // The drag still counts, so snapping resumes at the next release.
        Assert.True(extent.HasDragged);
        Assert.True(extent.HasChanged);
    }

    [Fact]
    public void Extent_StartActivityCancelsThePreviousActivity()
    {
        var extent = NewExtent();
        extent.AvailablePixels = ScreenHeight;
        int firstCancelled = 0;
        int secondCancelled = 0;

        extent.StartActivity(() => firstCancelled++);
        extent.StartActivity(() => secondCancelled++);
        Assert.Equal(1, firstCancelled);
        Assert.Equal(0, secondCancelled);

        // Any drag cancels the running activity, and only once.
        extent.AddPixelDelta(10.0, context: null);
        extent.AddPixelDelta(10.0, context: null);
        Assert.Equal(1, secondCancelled);
    }

    [Fact]
    public void Extent_CopyWithJumpsToTheNewInitialSizeOnlyWhileUnchanged()
    {
        var untouched = NewExtent(initialSize: 0.5);
        DraggableSheetExtent moved = Copy(untouched, initialSize: 0.6);
        Assert.Equal(0.6, moved.CurrentSize);

        var changed = NewExtent(initialSize: 0.5);
        changed.AvailablePixels = ScreenHeight;
        changed.AddPixelDelta(60.0, context: null);
        DraggableSheetExtent preserved = Copy(changed, initialSize: 0.9);
        Assert.Equal(0.6, preserved.CurrentSize, 6);
    }

    [Fact]
    public void Extent_CopyWithClampsThePreservedSizeIntoTheNewBounds()
    {
        var extent = NewExtent(initialSize: 0.5);
        extent.AvailablePixels = ScreenHeight;
        extent.AddPixelDelta(240.0, context: null);
        Assert.Equal(0.9, extent.CurrentSize, 6);

        DraggableSheetExtent clamped = Copy(extent, maxSize: 0.8);

        Assert.Equal(0.8, clamped.CurrentSize, 6);
        // Available pixels are not carried over; they are refreshed by the next layout.
        Assert.Equal(double.PositiveInfinity, clamped.AvailablePixels);
    }

    // ---------- Notification ----------

    [Fact]
    public void Notification_CarriesTheExtentsAndTheCloseFlag()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5, shouldCloseOnMinExtent: false);

        harness.Drag(60);

        DraggableScrollableNotification notification = Assert.Single(harness.Notifications);
        Assert.Equal(0.4, notification.Extent, 6);
        Assert.Equal(0.25, notification.MinExtent);
        Assert.Equal(1.0, notification.MaxExtent);
        Assert.Equal(0.5, notification.InitialExtent);
        Assert.False(notification.ShouldCloseOnMinExtent);
        Assert.Equal(0, notification.Depth);
    }

    [Fact]
    public void Notification_ShouldCloseOnMinExtentSurvivesAWidgetUpdate()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5, shouldCloseOnMinExtent: false);
        harness.Rebuild();

        harness.Drag(60);

        Assert.False(Assert.Single(harness.Notifications).ShouldCloseOnMinExtent);
    }

    [Fact]
    public void Notification_ReachingTheMinimumReportsExtentEqualToMinExtent()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5);

        harness.Drag(ScreenHeight);

        DraggableScrollableNotification last = harness.Notifications[^1];
        Assert.Equal(last.MinExtent, last.Extent);
        Assert.True(last.ShouldCloseOnMinExtent);
    }

    [Fact]
    public void Notification_IsNotDispatchedWhenTheSizeDoesNotChange()
    {
        using var harness = new SheetHarness(initialChildSize: 1.0, minChildSize: 0.25);

        harness.Drag(-100);

        Assert.Empty(harness.Notifications);
    }

    [Fact]
    public void Notification_RejectsExtentsOutsideTheReportedRange()
    {
        using var harness = new SheetHarness();

        Assert.Throws<ArgumentOutOfRangeException>(() => new DraggableScrollableNotification(
            extent: 1.2,
            minExtent: 0.25,
            maxExtent: 1.0,
            initialExtent: 0.5,
            context: harness.SheetContext));
    }

    // ---------- Controller ----------

    [Fact]
    public void Controller_IsNotAttachedBeforeTheSheetIsMounted()
    {
        var controller = new DraggableScrollableController();

        Assert.False(controller.IsAttached);
        Assert.Throws<InvalidOperationException>(() => controller.Size);
        Assert.Throws<InvalidOperationException>(() => controller.Pixels);
        Assert.Throws<InvalidOperationException>(() => controller.JumpTo(0.5));
        Assert.Throws<InvalidOperationException>(() => controller.PixelsToSize(0));
        Assert.Throws<InvalidOperationException>(() => controller.SizeToPixels(0));
        Assert.Throws<InvalidOperationException>(() => controller.Reset());
    }

    [Fact]
    public void Controller_ExposesSizeAndPixelsOnceAttached()
    {
        using var harness = new SheetHarness(initialChildSize: 0.25);

        Assert.True(harness.Controller.IsAttached);
        Assert.Equal(0.25, harness.Controller.Size);
        Assert.Equal(0.25 * ScreenHeight, harness.Controller.Pixels, 6);
        Assert.Equal(0.25 * ScreenHeight, harness.Controller.SizeToPixels(0.25), 6);
        Assert.Equal(0.25, harness.Controller.PixelsToSize(0.25 * ScreenHeight), 6);
    }

    [Fact]
    public void Controller_JumpToMovesTheSheetAndClampsIntoRange()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5);

        harness.Controller.JumpTo(0.8);
        Assert.Equal(0.8, harness.Controller.Size, 6);

        // A legal but out-of-bounds size lands on the bound rather than throwing.
        harness.Controller.JumpTo(0.0);
        Assert.Equal(0.25, harness.Controller.Size, 6);
    }

    [Fact]
    public void Controller_RejectsSizesOutsideTheUnitRangeAndAZeroDuration()
    {
        using var harness = new SheetHarness();

        Assert.Throws<ArgumentOutOfRangeException>(() => harness.Controller.JumpTo(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => harness.Controller.JumpTo(1.1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => harness.Controller.AnimateTo(-1, TimeSpan.FromMilliseconds(100), Curves.Linear));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => harness.Controller.AnimateTo(1.1, TimeSpan.FromMilliseconds(100), Curves.Linear));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => harness.Controller.AnimateTo(0.5, TimeSpan.Zero, Curves.Linear));
    }

    [Fact]
    public void Controller_ProgrammaticSizingDoesNotMoveTheListOffset()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5);
        harness.Drag(-ScreenHeight * 0.5);
        harness.Drag(-100);
        Assert.Equal(100.0, harness.Position.Pixels, 3);

        harness.Controller.JumpTo(0.8);

        Assert.Equal(0.8, harness.Controller.Size, 6);
        Assert.Equal(100.0, harness.Position.Pixels, 3);
    }

    [Fact]
    public void Controller_NotifiesOnEverySizeChangeButNotOnAttach()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5);
        List<double> sizes = [];
        harness.Controller.AddListener(() => sizes.Add(harness.Controller.Size));

        Assert.Empty(sizes);

        harness.Drag(60);
        harness.Controller.JumpTo(0.6);

        Assert.Equal(2, sizes.Count);
        Assert.Equal(0.4, sizes[0], 6);
        Assert.Equal(0.6, sizes[1], 6);
    }

    [Fact]
    public void Controller_DoesNotNotifyWhenAParameterChangeLeavesTheSizeAlone()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5, minChildSize: 0.25);
        List<double> sizes = [];
        harness.Controller.AddListener(() => sizes.Add(harness.Controller.Size));

        harness.Rebuild(minChildSize: 0.1);
        Assert.Empty(sizes);

        // The listener survives the extent replacement.
        harness.Controller.JumpTo(0.6);
        Assert.Equal([0.6], sizes);
    }

    [Fact]
    public void Controller_NotifiesWhenAParameterChangeForcesTheSize()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5);
        List<double> sizes = [];
        harness.Controller.AddListener(() => sizes.Add(harness.Controller.Size));

        harness.Rebuild(initialChildSize: 0.6);

        Assert.Equal([0.6], sizes);
    }

    [Fact]
    public async Task Controller_AnimateToReachesTheTargetAndSuspendsSnapping()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5, snap: true, snapSizes: [0.5, 1.0]);

        TickerFuture animation = harness.Controller.AnimateTo(
            0.7,
            TimeSpan.FromMilliseconds(100),
            Curves.Linear);
        harness.PumpFrame(0.0);
        harness.PumpFrame(0.05);
        Assert.InRange(harness.Controller.Size, 0.55, 0.65);

        harness.PumpFrame(0.06);
        await animation;

        Assert.Equal(0.7, harness.Controller.Size, 6);
        // Snapping stays disabled until the next user interaction.
        Assert.False(harness.Extent.HasDragged);
        Assert.True(harness.Extent.HasChanged);
    }

    [Fact]
    public async Task Controller_AnimateToIsInterruptedByADrag()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5);

        TickerFuture animation = harness.Controller.AnimateTo(
            1.0,
            TimeSpan.FromMilliseconds(200),
            Curves.Linear);
        harness.PumpFrame(0.0);
        harness.PumpFrame(0.1);
        double midpoint = harness.Controller.Size;
        Assert.InRange(midpoint, 0.6, 0.9);

        harness.Drag(60);
        harness.PumpFrame(0.2);

        // Flutter's ticker future never resolves for a canceled animation; only `orCancel` reports it.
        Assert.False(animation.Task.IsCompleted);
        await Assert.ThrowsAsync<TickerCanceled>(() => animation.OrCancel);
        Assert.Equal(midpoint - 0.1, harness.Controller.Size, 6);
    }

    [Fact]
    public async Task Controller_AnimateToIsInterruptedByJumpTo()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5);

        TickerFuture animation = harness.Controller.AnimateTo(
            1.0,
            TimeSpan.FromMilliseconds(200),
            Curves.Linear);
        harness.PumpFrame(0.0);
        harness.PumpFrame(0.05);
        harness.Controller.JumpTo(0.6);
        harness.PumpFrame(0.2);

        Assert.False(animation.Task.IsCompleted);
        await Assert.ThrowsAsync<TickerCanceled>(() => animation.OrCancel);
        Assert.Equal(0.6, harness.Controller.Size, 6);
    }

    [Fact]
    public async Task Controller_AnimateToSurvivesTheSheetBeingDisposedMidFlight()
    {
        var harness = new SheetHarness(initialChildSize: 0.5);
        DraggableScrollableController controller = harness.Controller;

        TickerFuture animation = controller.AnimateTo(1.0, TimeSpan.FromMilliseconds(200), Curves.Linear);
        harness.PumpFrame(0.0);
        harness.PumpFrame(0.05);
        harness.Dispose();

        Assert.False(animation.Task.IsCompleted);
        await Assert.ThrowsAsync<TickerCanceled>(() => animation.OrCancel);
        Assert.False(controller.IsAttached);
    }

    [Fact]
    public void Controller_CannotBeAttachedToTwoSheetsAtOnce()
    {
        using var harness = new SheetHarness();

        Assert.Throws<InvalidOperationException>(() =>
            new SheetHarness(controller: harness.Controller).Dispose());
    }

    [Fact]
    public void Controller_CanBeReusedAfterItsSheetIsDisposed()
    {
        var controller = new DraggableScrollableController();
        using (var first = new SheetHarness(controller: controller, initialChildSize: 0.5))
        {
            Assert.True(controller.IsAttached);
        }

        using var second = new SheetHarness(controller: controller, initialChildSize: 0.4);

        Assert.True(controller.IsAttached);
        Assert.Equal(0.4, controller.Size, 6);
    }

    [Fact]
    public void Controller_MovesToTheReplacementControllerOnUpdate()
    {
        var replacement = new DraggableScrollableController();
        using var harness = new SheetHarness(initialChildSize: 0.5);
        DraggableScrollableController original = harness.Controller;

        harness.Rebuild(controller: replacement);

        Assert.False(original.IsAttached);
        Assert.True(replacement.IsAttached);
        replacement.JumpTo(0.7);
        Assert.Equal(0.7, replacement.Size, 6);
    }

    [Fact]
    public void Controller_ResetReturnsTheSheetToItsInitialSizeWithoutSnappingAway()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5, snap: true, snapSizes: [0.5, 1.0]);
        harness.Drag(-ScreenHeight * 0.5);
        Assert.Equal(1.0, harness.Controller.Size, 6);

        harness.Controller.Reset();

        Assert.Equal(0.5, harness.Controller.Size, 6);
        Assert.False(harness.Extent.HasDragged);
        Assert.False(harness.Extent.HasChanged);
    }

    // ---------- Widget updates ----------

    [Fact]
    public void Update_NewInitialChildSizeMovesAnUntouchedSheetOnly()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5);

        harness.Rebuild(initialChildSize: 0.6);
        Assert.Equal(0.6, harness.Controller.Size, 6);

        harness.Drag(-60);
        Assert.Equal(0.7, harness.Controller.Size, 6);

        harness.Rebuild(initialChildSize: 0.3);
        Assert.Equal(0.7, harness.Controller.Size, 6);
    }

    [Fact]
    public void Update_ShrinkingMaxChildSizePullsTheSheetDown()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5);
        harness.Drag(-240);
        Assert.Equal(0.9, harness.Controller.Size, 6);

        harness.Rebuild(maxChildSize: 0.8);

        Assert.Equal(0.8, harness.Controller.Size, 6);
    }

    [Fact]
    public void Update_ReusesTheScrollControllerSoTheListKeepsItsOffset()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5);
        harness.Drag(-ScreenHeight * 0.5);
        harness.Drag(-100);
        Assert.Equal(100.0, harness.Position.Pixels, 3);

        harness.Rebuild(minChildSize: 0.1);

        Assert.Equal(100.0, harness.Position.Pixels, 3);
    }

    [Fact]
    public void Update_DoesNotRebuildTheUserBuilderWhileTheSheetResizes()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5);
        Assert.Equal(1, harness.BuildCount);

        harness.Drag(-60);
        harness.Drag(-60);

        Assert.Equal(1, harness.BuildCount);
    }

    [Fact]
    public void Update_WithoutScrollClientsDoesNotThrow()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5, attachScrollController: false);

        harness.Rebuild(snap: true, snapSizes: [0.5, 1.0]);

        Assert.False(harness.Controller.IsAttached);
    }

    // ---------- Actuator ----------

    [Fact]
    public void Actuator_ResetReturnsFalseWithoutAnActuator()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5, withActuator: false);

        Assert.False(DraggableScrollableActuator.Reset(harness.SheetContext));
    }

    [Fact]
    public void Actuator_ResetReturnsTheSheetToItsInitialSize()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5);
        harness.Drag(-180);
        Assert.Equal(0.8, harness.Controller.Size, 6);

        bool didReset = DraggableScrollableActuator.Reset(harness.SheetContext);
        harness.Pump();

        Assert.True(didReset);
        Assert.Equal(0.5, harness.Controller.Size, 6);
        Assert.False(harness.Extent.HasChanged);
    }

    [Fact]
    public void Actuator_ResetIsAOneShotRequest()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5);
        DraggableScrollableActuator.Reset(harness.SheetContext);
        harness.Pump();

        harness.Drag(-180);
        harness.Pump();

        // The cleared request must not reset the sheet a second time.
        Assert.Equal(0.8, harness.Controller.Size, 6);
    }

    // ---------- Ballistic and snapping ----------

    [Fact]
    public void GoBallistic_WithoutSnapDelegatesAZeroVelocityToTheList()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5);
        harness.Drag(-60);

        harness.Position.GoBallistic(0.0);

        Assert.IsType<IdleScrollActivity>(harness.Position.Activity);
    }

    [Fact]
    public void GoBallistic_FlingUpGrowsTheSheetAndThenHandsOffToTheList()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5);
        harness.Drag(-30);

        harness.Position.GoBallistic(2000.0);
        harness.PumpUntilSettled();

        Assert.Equal(1.0, harness.Controller.Size, 6);
        // The leftover velocity carried into the list rather than stopping at the boundary.
        Assert.True(harness.Position.Pixels > 0.0);
    }

    [Fact]
    public void GoBallistic_FlingDownShrinksTheSheetToItsMinimum()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5);
        harness.Drag(30);

        harness.Position.GoBallistic(-2000.0);
        harness.PumpUntilSettled();

        Assert.Equal(0.25, harness.Controller.Size, 6);
    }

    [Fact]
    public void GoBallistic_SnapsExactlyOntoMinChildSize()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5, snap: true, snapSizes: [0.5, 1.0]);
        harness.Drag(30);

        harness.Position.GoBallistic(-2000.0);
        harness.PumpUntilSettled();

        Assert.Equal(0.25, harness.Controller.Size);
        Assert.Equal(harness.Notifications[^1].MinExtent, harness.Notifications[^1].Extent);
    }

    [Fact]
    public void GoBallistic_ZeroVelocitySnapsToTheNearestTarget()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5, snap: true, snapSizes: [0.5, 0.75, 1.0]);

        harness.Drag(-ScreenHeight * 0.1);
        Assert.Equal(0.6, harness.Controller.Size, 6);

        harness.Position.GoBallistic(0.0);
        harness.PumpUntilSettled();

        Assert.Equal(0.5, harness.Controller.Size, 6);
    }

    [Fact]
    public void GoBallistic_FlingSnapsInTheDirectionOfMomentum()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5, snap: true, snapSizes: [0.5, 0.75, 1.0]);

        harness.Drag(-ScreenHeight * 0.06);
        Assert.Equal(0.56, harness.Controller.Size, 6);

        // Momentum wins over proximity: 0.56 is nearer 0.5, but the fling is upward.
        harness.Position.GoBallistic(1000.0);
        harness.PumpUntilSettled();

        Assert.Equal(0.75, harness.Controller.Size, 6);
    }

    [Fact]
    public void GoBallistic_DoesNotSnapAwayFromTheInitialSizeOnBuild()
    {
        using var harness = new SheetHarness(initialChildSize: 0.7, snap: true, snapSizes: [0.5, 1.0]);

        harness.Position.GoBallistic(0.0);
        harness.PumpUntilSettled();

        Assert.Equal(0.7, harness.Controller.Size, 6);
    }

    [Fact]
    public void GoBallistic_UsesTheRequestedSnapAnimationDuration()
    {
        using var harness = new SheetHarness(
            initialChildSize: 0.5,
            snap: true,
            snapSizes: [0.5, 1.0],
            snapAnimationDuration: TimeSpan.FromSeconds(2));
        harness.Drag(-ScreenHeight * 0.35);
        Assert.Equal(0.85, harness.Controller.Size, 6);

        harness.Position.GoBallistic(0.0);
        harness.PumpFrame(0.0);
        harness.PumpFrame(0.5);

        // A fixed duration crosses the remaining 0.15 in two seconds, so a quarter of it per frame.
        Assert.InRange(harness.Controller.Size, 0.86, 0.93);
        harness.PumpUntilSettled();
        Assert.Equal(1.0, harness.Controller.Size, 6);
    }

    [Fact]
    public void GoBallistic_LeavesNoLiveTickerWhenTheSheetIsDisposedMidFlight()
    {
        var harness = new SheetHarness(initialChildSize: 0.5);
        harness.Drag(-30);
        harness.Position.GoBallistic(2000.0);
        harness.PumpFrame(0.01);

        harness.Dispose();

        // Pumping after teardown must not throw or move anything.
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.5));
    }

    // ---------- Position absorption ----------

    [Fact]
    public void Position_AbsorbsAnInFlightDragWhenTheScrollPositionIsReplaced()
    {
        using var harness = new SheetHarness(initialChildSize: 0.5);
        int cancelled = 0;
        DraggableScrollableSheetScrollPosition original = harness.Position;
        original.Drag(new DragStartDetails(), () => cancelled++);

        harness.Rebuild(physics: new ClampingScrollPhysics());

        Assert.NotSame(original, harness.Position);
        Assert.Equal(0.5, harness.Controller.Size, 6);
        Assert.Equal(0, cancelled);
    }

    // ---------- SnappingSimulation ----------

    [Fact]
    public void SnappingSimulation_NegligibleVelocitySnapsToTheNearestTargetWithTiesGoingUp()
    {
        double[] snapSizes = [0.0, 100.0, 200.0];

        Assert.Equal(100.0, TargetOf(new SnappingSimulation(90.0, 0.0, snapSizes)));
        Assert.Equal(0.0, TargetOf(new SnappingSimulation(40.0, 0.0, snapSizes)));
        Assert.Equal(100.0, TargetOf(new SnappingSimulation(50.0, 0.0, snapSizes)));
    }

    [Fact]
    public void SnappingSimulation_MeaningfulVelocitySnapsInItsOwnDirection()
    {
        double[] snapSizes = [0.0, 100.0, 200.0];

        Assert.Equal(100.0, TargetOf(new SnappingSimulation(10.0, 1000.0, snapSizes)));
        Assert.Equal(0.0, TargetOf(new SnappingSimulation(90.0, -1000.0, snapSizes)));
    }

    [Fact]
    public void SnappingSimulation_RunsAtLeastAtTheMinimumSpeed()
    {
        var slowUp = new SnappingSimulation(10.0, 5.0, [0.0, 100.0]);
        var slowDown = new SnappingSimulation(90.0, -5.0, [0.0, 100.0]);

        Assert.Equal(SnappingSimulation.MinimumSpeed, slowUp.Velocity);
        Assert.Equal(-SnappingSimulation.MinimumSpeed, slowDown.Velocity);
    }

    [Fact]
    public void SnappingSimulation_FixedDurationOverridesTheVelocity()
    {
        var simulation = new SnappingSimulation(
            position: 10.0,
            initialVelocity: 5000.0,
            pixelSnapSizes: [0.0, 110.0],
            snapAnimationDuration: TimeSpan.FromMilliseconds(500));

        // The fixed duration wins over the fling velocity: 100px in half a second.
        Assert.Equal(200.0, simulation.Velocity, 6);
        Assert.Equal(110.0, simulation.X(0.5), 6);
        Assert.True(simulation.IsDone(0.5));
    }

    [Fact]
    public void SnappingSimulation_MovesAtAConstantVelocityAndStopsExactlyOnTarget()
    {
        var simulation = new SnappingSimulation(100.0, 1000.0, [0.0, 1700.0]);

        Assert.Equal(1600.0, simulation.Velocity);
        Assert.Equal(900.0, simulation.X(0.5), 6);
        Assert.Equal(1600.0, simulation.DX(0.5));
        Assert.False(simulation.IsDone(0.5));

        Assert.Equal(1700.0, simulation.X(2.0));
        Assert.Equal(0.0, simulation.DX(2.0));
        Assert.True(simulation.IsDone(1.0));
    }

    [Fact]
    public void SnappingSimulation_AlreadyOnATargetKeepsIt()
    {
        var simulation = new SnappingSimulation(100.0, 1000.0, [0.0, 100.0, 200.0]);

        Assert.Equal(100.0, TargetOf(simulation));
        Assert.True(simulation.IsDone(0.0));
    }


    // ---------- Helpers ----------

    private static double TargetOf(SnappingSimulation simulation) => simulation.X(double.MaxValue);

    private static DraggableSheetExtent NewExtent(
        double minSize = 0.25,
        double maxSize = 1.0,
        double initialSize = 0.5)
    {
        return new DraggableSheetExtent(
            minSize: minSize,
            maxSize: maxSize,
            snap: false,
            snapSizes: [minSize, maxSize],
            initialSize: initialSize);
    }

    private static DraggableSheetExtent Copy(
        DraggableSheetExtent extent,
        double? minSize = null,
        double? maxSize = null,
        double? initialSize = null)
    {
        double resolvedMin = minSize ?? extent.MinSize;
        double resolvedMax = maxSize ?? extent.MaxSize;
        return extent.CopyWith(
            minSize: resolvedMin,
            maxSize: resolvedMax,
            snap: extent.Snap,
            snapSizes: [resolvedMin, resolvedMax],
            initialSize: initialSize ?? extent.InitialSize,
            snapAnimationDuration: extent.SnapAnimationDuration,
            shouldCloseOnMinExtent: extent.ShouldCloseOnMinExtent);
    }

    /// <summary>
    /// Mounts one <see cref="DraggableScrollableSheet"/> over a fixed 800x600 viewport, wrapped in a
    /// <see cref="DraggableScrollableActuator"/> and a notification listener.
    /// </summary>
    private sealed class SheetHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly PipelineOwner _pipeline;
        private readonly HarnessRootElement _rootElement;
        private readonly bool _withActuator;
        private readonly bool _attachScrollController;
        private readonly bool _looseParent;
        private double _clockSeconds;
        private readonly List<DraggableScrollableNotification> _notifications = [];

        private double _initialChildSize;
        private double _minChildSize;
        private double _maxChildSize;
        private bool _expand;
        private bool _snap;
        private IReadOnlyList<double>? _snapSizes;
        private TimeSpan? _snapAnimationDuration;
        private bool _shouldCloseOnMinExtent;
        private ScrollPhysics? _physics;
        private DraggableScrollableController _controller;
        private BuildContext _sheetContext;
        private ScrollController? _sheetScrollController;

        public SheetHarness(
            double initialChildSize = 0.5,
            double minChildSize = 0.25,
            double maxChildSize = 1.0,
            bool expand = true,
            bool snap = false,
            IReadOnlyList<double>? snapSizes = null,
            TimeSpan? snapAnimationDuration = null,
            bool shouldCloseOnMinExtent = true,
            ScrollPhysics? physics = null,
            DraggableScrollableController? controller = null,
            bool withActuator = true,
            bool attachScrollController = true,
            bool looseParent = false)
        {
            _looseParent = looseParent;
            _initialChildSize = initialChildSize;
            _minChildSize = minChildSize;
            _maxChildSize = maxChildSize;
            _expand = expand;
            _snap = snap;
            _snapSizes = snapSizes;
            _snapAnimationDuration = snapAnimationDuration;
            _shouldCloseOnMinExtent = shouldCloseOnMinExtent;
            _physics = physics;
            _controller = controller ?? new DraggableScrollableController();
            _withActuator = withActuator;
            _attachScrollController = attachScrollController;

            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _rootElement = new HarnessRootElement(RenderView, BuildTree());
            _rootElement.Attach(_owner);
            _rootElement.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
            Pump();
        }

        public RenderView RenderView { get; }

        public DraggableScrollableController Controller => _controller;

        public IReadOnlyList<DraggableScrollableNotification> Notifications => _notifications;

        public int BuildCount { get; private set; }

        public BuildContext SheetContext => _sheetContext;

        public DraggableScrollableSheetScrollPosition Position =>
            (DraggableScrollableSheetScrollPosition)_sheetScrollController!.Position;

        public DraggableSheetExtent Extent =>
            ((DraggableScrollableSheetScrollController)_sheetScrollController!).Extent;

        public void Pump()
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(new Size(ScreenWidth, ScreenHeight));
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
        }

        /// <summary>
        /// Applies a drag of the given number of pixels; negative grows the sheet. Physics that
        /// refuse user offsets are honoured here the same way the scrollable honours them, by never
        /// registering its drag recognizers.
        /// </summary>
        public void Drag(double delta)
        {
            if (!Position.Physics.ShouldAcceptUserOffset(Position))
            {
                return;
            }

            // A real drag runs under a drag activity, which is what keeps the relayout the resizing
            // sheet causes from resetting the position to idle on every frame.
            Position.BeginDrag();
            Position.ApplyUserOffset(delta);
            Pump();
        }

        /// <summary>
        /// Advances a virtual clock by the given number of seconds and runs one frame. The clock has
        /// to be the harness's own: the scheduler's is wall-clock, so repeatedly pumping
        /// `CurrentSeconds + dt` would advance tickers by the real elapsed time instead of by `dt`.
        /// </summary>
        public void PumpFrame(double seconds)
        {
            _clockSeconds = Math.Max(_clockSeconds, Scheduler.CurrentSeconds) + seconds;
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(_clockSeconds));
            Pump();
        }

        /// <summary>
        /// Runs frames until the sheet stops resizing and its list position goes idle, or a frame
        /// budget runs out. The sheet's own ballistic runs on an animation controller rather than a
        /// scroll activity, so the size has to be watched as well.
        /// </summary>
        public void PumpUntilSettled()
        {
            double lastSize = Controller.Size;
            int stableFrames = 0;
            for (int frame = 0; frame < 480; frame++)
            {
                PumpFrame(1.0 / 60.0);
                stableFrames = Controller.Size == lastSize ? stableFrames + 1 : 0;
                lastSize = Controller.Size;
                if (stableFrames >= 3 && Position.Activity is IdleScrollActivity)
                {
                    return;
                }
            }
        }

        public void Rebuild(
            double? initialChildSize = null,
            double? minChildSize = null,
            double? maxChildSize = null,
            bool? snap = null,
            IReadOnlyList<double>? snapSizes = null,
            ScrollPhysics? physics = null,
            DraggableScrollableController? controller = null)
        {
            _initialChildSize = initialChildSize ?? _initialChildSize;
            _minChildSize = minChildSize ?? _minChildSize;
            _maxChildSize = maxChildSize ?? _maxChildSize;
            _snap = snap ?? _snap;
            _snapSizes = snapSizes ?? _snapSizes;
            _physics = physics ?? _physics;
            _controller = controller ?? _controller;
            _rootElement.UpdateChildWidget(BuildTree());
            Pump();
        }

        /// <summary>The global rect of the content the sheet sized, matching Flutter's list rect.</summary>
        public Rect SheetRect()
        {
            RenderBox content = SheetBox().Child
                                ?? throw new InvalidOperationException("The sheet has no content.");
            return new Rect(content.LocalToGlobal(default), content.Size);
        }

        /// <summary>The height of the sheet's own fractionally sized box.</summary>
        public double SheetBoxHeight() => SheetBox().Size.Height;

        public void Dispose()
        {
            _rootElement.Unmount();
        }

        private Widget BuildTree()
        {
            Widget sheet = new NotificationListener<DraggableScrollableNotification>(
                onNotification: notification =>
                {
                    _notifications.Add(notification);
                    return false;
                },
                child: new DraggableScrollableSheet(
                    initialChildSize: _initialChildSize,
                    minChildSize: _minChildSize,
                    maxChildSize: _maxChildSize,
                    expand: _expand,
                    snap: _snap,
                    snapSizes: _snapSizes,
                    snapAnimationDuration: _snapAnimationDuration,
                    controller: _controller,
                    shouldCloseOnMinExtent: _shouldCloseOnMinExtent,
                    builder: (context, scrollController) =>
                    {
                        BuildCount++;
                        _sheetContext = context;
                        _sheetScrollController = scrollController;
                        return _attachScrollController
                            ? new ListView(
                                controller: scrollController,
                                physics: _physics,
                                itemExtent: 25.0,
                                children: Enumerable
                                    .Range(0, 80)
                                    .Select(Widget (_) => new SizedBox(height: 25.0))
                                    .ToList())
                            : new SizedBox();
                    }));

            return WrapForConstraints(_withActuator ? new DraggableScrollableActuator(sheet) : sheet);
        }

        /// <summary>Finds the box the sheet's fractional sizing produced.</summary>
        private RenderFractionallySizedOverflowBox SheetBox()
        {
            RenderObject root = RenderView.Child ?? throw new InvalidOperationException("Nothing was laid out.");
            RenderFractionallySizedOverflowBox? found = null;
            void Visit(RenderObject node)
            {
                if (found != null)
                {
                    return;
                }

                if (node is RenderFractionallySizedOverflowBox box)
                {
                    found = box;
                    return;
                }

                node.VisitChildren(Visit);
            }

            Visit(root);
            return found ?? throw new InvalidOperationException("The sheet was not laid out.");
        }

        private Widget WrapForConstraints(Widget child) => _looseParent ? new Center(child: child) : child;

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;
            private Widget _childWidget;

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget)
            {
                _renderView = renderView;
                _childWidget = widget;
            }

            public override RenderObject? RenderObject => _child?.RenderObject;

            internal override Element? RenderObjectAttachingChild => _child;

            public void UpdateChildWidget(Widget widget)
            {
                _childWidget = widget;
                _child = UpdateChild(_child, _childWidget, Slot);
            }

            protected override void OnMount()
            {
                base.OnMount();
                Rebuild();
            }

            internal override void Rebuild()
            {
                Dirty = false;
                _child = UpdateChild(_child, _childWidget, Slot);
            }

            internal override void Update(Widget newWidget)
            {
                base.Update(newWidget);
                Rebuild();
            }

            internal override void ForgetChild(Element child)
            {
                if (ReferenceEquals(_child, child))
                {
                    _child = null;
                }
            }

            internal override void VisitChildren(Action<Element> visitor)
            {
                if (_child != null)
                {
                    visitor(_child);
                }
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
                if (child is RenderBox renderBox)
                {
                    _renderView.Child = renderBox;
                }
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (child is RenderBox renderBox && ReferenceEquals(_renderView.Child, renderBox))
                {
                    _renderView.Child = null;
                }
            }

            internal override void Unmount()
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
}
