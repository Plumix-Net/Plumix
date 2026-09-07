using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/scrollable.dart (parity tests)

namespace Plumix.Tests;

/// <summary>
/// Covers the parts of <see cref="Scrollable"/> that the <c>_ScrollableScope</c> inherited widget and
/// the restoration bucket drive: the dependency <see cref="Scrollable.MaybeOf"/> registers, the axis
/// filter it applies, and the offset the state persists.
/// </summary>
public sealed class ScrollableTests
{
    private const double ItemHeight = 100.0;

    private static readonly Size Surface = new(300, 400);

    private readonly MockRestorationManager _manager = new();

    [Fact]
    public void MaybeOf_ResolvesTheEnclosingScrollableAndRegistersADependency()
    {
        BuildContext? itemContext = null;
        using var harness = new RestorationHarness(Wrap(BuildList(
            controller: null,
            onItemBuilt: context => itemContext = context)));
        harness.Pump(Surface);

        Assert.NotNull(itemContext);
        Scrollable.ScrollableState? state = Scrollable.MaybeOf(itemContext!);
        Assert.NotNull(state);
        Assert.Same(state, Scrollable.Of(itemContext!));
        Assert.Equal(AxisDirection.Down, state!.AxisDirection);
    }

    [Fact]
    public void MaybeOf_DoesNotResolveTheScrollableFromItsOwnContext()
    {
        BuildContext? scrollableContext = null;
        using var harness = new RestorationHarness(Wrap(new Builder(context =>
        {
            scrollableContext = context;
            return BuildList(controller: null, onItemBuilt: null);
        })));
        harness.Pump(Surface);

        Assert.Null(Scrollable.MaybeOf(scrollableContext!));
    }

    [Fact]
    public void MaybeOf_WithAnAxis_SkipsScrollablesOnOtherAxes()
    {
        BuildContext? itemContext = null;
        using var harness = new RestorationHarness(Wrap(new ListView(
            scrollDirection: Axis.Horizontal,
            children:
            [
                new SizedBox(
                    width: ItemHeight,
                    child: BuildList(controller: null, onItemBuilt: context => itemContext = context)),
            ])));
        harness.Pump(Surface);

        Scrollable.ScrollableState vertical = Scrollable.Of(itemContext!, Axis.Vertical);
        Scrollable.ScrollableState horizontal = Scrollable.Of(itemContext!, Axis.Horizontal);
        Assert.NotSame(vertical, horizontal);
        Assert.Equal(AxisDirection.Down, vertical.AxisDirection);
        Assert.Equal(AxisDirection.Right, horizontal.AxisDirection);
    }

    [Fact]
    public void Of_WithoutAnAncestor_Throws()
    {
        BuildContext? context = null;
        using var harness = new RestorationHarness(Wrap(new Builder(buildContext =>
        {
            context = buildContext;
            return new SizedBox();
        })));
        harness.Pump(Surface);

        Assert.Null(Scrollable.MaybeOf(context!));
        FlutterError error = Assert.Throws<FlutterError>(() => Scrollable.Of(context!));
        Assert.Contains("does not contain a Scrollable widget", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Of_ResolvesFromAScrollNotificationContext()
    {
        using var controller = new ScrollController();
        Scrollable.ScrollableState? fromNotification = null;
        using var harness = new RestorationHarness(Wrap(new NotificationListener<ScrollNotification>(
            onNotification: notification =>
            {
                fromNotification ??= Scrollable.MaybeOf(notification.Context!);
                return false;
            },
            child: BuildList(controller: controller, onItemBuilt: null))));
        harness.Pump(Surface);

        controller.JumpTo(50);
        harness.Pump(Surface);

        Assert.NotNull(fromNotification);
        Assert.Same(controller.Position, fromNotification!.Position);
    }

    [Fact]
    public void DeltaToScrollOrigin_TracksThePositionAlongTheAxisDirection()
    {
        using var controller = new ScrollController();
        BuildContext? itemContext = null;
        using var harness = new RestorationHarness(Wrap(BuildList(
            controller: controller,
            onItemBuilt: context => itemContext = context)));
        harness.Pump(Surface);

        controller.JumpTo(120);
        harness.Pump(Surface);

        Scrollable.ScrollableState state = Scrollable.Of(itemContext!);
        Assert.Equal(new Point(0, 120), state.DeltaToScrollOrigin);
    }

    [Fact]
    public void ResolvedPhysics_AppliesTheWidgetPhysicsOnTopOfTheAmbientBehavior()
    {
        BuildContext? itemContext = null;
        using var harness = new RestorationHarness(Wrap(BuildList(
            controller: null,
            onItemBuilt: context => itemContext = context)));
        harness.Pump(Surface);

        ScrollPhysics? physics = Scrollable.Of(itemContext!).ResolvedPhysics;
        Assert.IsType<AlwaysScrollableScrollPhysics>(physics);
        Assert.NotNull(physics!.Parent);
    }

    [Fact]
    public void DebugFillProperties_ReportsAxisDirectionPhysicsAndRestorationId()
    {
        var properties = new DiagnosticPropertiesBuilder();
        new Scrollable(
            viewportBuilder: static (_, offset) => new Viewport(offset: offset, slivers: []),
            axisDirection: AxisDirection.Left,
            restorationId: "scroller").DebugFillProperties(properties);

        List<string> names = properties.Properties.Select(property => property.Name!).ToList();
        Assert.Contains("axisDirection", names);
        Assert.Contains("physics", names);
        Assert.Contains("restorationId", names);
    }

    // -------------------------------------------------------------------------------------------
    // Restoration
    // -------------------------------------------------------------------------------------------

    [Fact]
    public void ScrollOffset_IsRestoredFromTheRestorationBucket()
    {
        var root = RestorationBucket.Root(
            _manager,
            RawRestorationData.Build(
                children: new Dictionary<object, object?>
                {
                    ["list"] = RawRestorationData.Build(
                        values: new Dictionary<object, object?> { ["offset"] = 150.0 }),
                }));
        using var controller = new ScrollController();

        using var harness = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: root,
            child: Wrap(BuildList(controller: controller, onItemBuilt: null, restorationId: "list"))));
        harness.Pump(Surface);

        Assert.Equal(150.0, controller.Offset, precision: 6);
    }

    [Fact]
    public void ScrollOffset_IsWrittenToTheRestorationBucketWhenScrollingEnds()
    {
        Dictionary<object, object?> rawData = RawRestorationData.Build();
        var root = RestorationBucket.Root(_manager, rawData);
        using var controller = new ScrollController();

        using var harness = new RestorationHarness(new UnmanagedRestorationScope(
            bucket: root,
            child: Wrap(BuildList(controller: controller, onItemBuilt: null, restorationId: "list"))));
        harness.Pump(Surface);

        controller.JumpTo(180);
        harness.Pump(Surface);
        _manager.DoSerialization();

        Dictionary<object, object?>? child = RawRestorationData.Child(rawData, "list");
        Assert.NotNull(child);
        Dictionary<object, object?>? values = RawRestorationData.Values(child!);
        Assert.NotNull(values);
        Assert.Equal(180.0, Assert.IsType<double>(values!["offset"]), precision: 6);
    }

    // -------------------------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------------------------

    private static Widget Wrap(Widget child) => new Directionality(TextDirection.Ltr, child);

    private static Widget BuildList(
        ScrollController? controller,
        Action<BuildContext>? onItemBuilt,
        string? restorationId = null)
    {
        return ListView.Builder(
            itemCount: 20,
            itemExtent: ItemHeight,
            controller: controller,
            restorationId: restorationId,
            itemBuilder: (context, _) =>
            {
                onItemBuilt?.Invoke(context);
                return new SizedBox(height: ItemHeight);
            });
    }
}
