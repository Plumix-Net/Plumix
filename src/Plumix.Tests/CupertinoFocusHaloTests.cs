using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: cupertino_ui/test/cupertino_focus_halo_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoFocusHaloTests : IDisposable
{
    private static readonly Size ViewSize = new(400.0, 400.0);

    public CupertinoFocusHaloTests()
    {
        Scheduler.ResetForTests();
        FocusManager.Instance.ResetForTests();
    }

    public void Dispose()
    {
        FocusManager.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void WithRect_TracksFocusAcrossDescendantsAndBetweenGroups()
    {
        var groupOneFirst = new FocusNode();
        var groupOneSecond = new FocusNode();
        var groupTwo = new FocusNode();
        using var harness = new CupertinoThemeTestHarness(Wrap(new Column(children:
        [
            CupertinoFocusHalo.WithRect(new Column(children:
            [
                FocusBox(groupOneFirst),
                FocusBox(groupOneSecond),
            ])),
            CupertinoFocusHalo.WithRect(FocusBox(groupTwo)),
        ])));

        harness.Pump(ViewSize);
        IReadOnlyList<ShapeDecoration> initial = FindHaloDecorations(harness.RenderView);
        Assert.Equal(2, initial.Count);
        Assert.Equal(BorderSide.None, Assert.IsType<RoundedRectangleBorder>(initial[0].Shape).Side);
        Assert.Equal(BorderSide.None, Assert.IsType<RoundedRectangleBorder>(initial[1].Shape).Side);

        Assert.True(groupOneFirst.RequestFocus());
        harness.Pump(ViewSize);
        AssertFocused(FindHaloDecorations(harness.RenderView)[0]);
        AssertNotFocused(FindHaloDecorations(harness.RenderView)[1]);

        Assert.True(groupOneSecond.RequestFocus());
        harness.Pump(ViewSize);
        AssertFocused(FindHaloDecorations(harness.RenderView)[0]);
        AssertNotFocused(FindHaloDecorations(harness.RenderView)[1]);

        Assert.True(groupTwo.RequestFocus());
        harness.Pump(ViewSize);
        AssertNotFocused(FindHaloDecorations(harness.RenderView)[0]);
        AssertFocused(FindHaloDecorations(harness.RenderView)[1]);

        groupTwo.Unfocus();
        harness.Pump(ViewSize);
        Assert.All(FindHaloDecorations(harness.RenderView), AssertNotFocused);
    }

    [Fact]
    public void ShapeFactories_UseThePinnedBorderKindsAndRadius()
    {
        BorderRadius radius = BorderRadius.Circular(12.0);
        Widget rect = CupertinoFocusHalo.WithRect(new SizedBox(width: 100.0, height: 50.0));
        Widget roundedRect = CupertinoFocusHalo.WithRRect(
            new SizedBox(width: 100.0, height: 50.0),
            radius);
        Widget superellipse = CupertinoFocusHalo.WithRoundedSuperellipse(
            new SizedBox(width: 100.0, height: 50.0),
            radius);
        using var harness = new CupertinoThemeTestHarness(Wrap(new Column(children:
        [
            rect,
            roundedRect,
            superellipse,
        ])));

        harness.Pump(ViewSize);
        IReadOnlyList<ShapeDecoration> decorations = FindHaloDecorations(harness.RenderView);

        var rectBorder = Assert.IsType<RoundedRectangleBorder>(decorations[0].Shape);
        var roundedRectBorder = Assert.IsType<RoundedRectangleBorder>(decorations[1].Shape);
        var superellipseBorder = Assert.IsType<RoundedSuperellipseBorder>(decorations[2].Shape);
        Assert.True(rectBorder.BorderRadius.IsZero);
        Assert.Equal(radius, roundedRectBorder.BorderRadius.Resolve(TextDirection.Ltr));
        Assert.Equal(radius, superellipseBorder.BorderRadius.Resolve(TextDirection.Ltr));
    }

    [Fact]
    public void FocusedHalo_UsesThePinnedHslColorAndBorderWidth()
    {
        var focusNode = new FocusNode();
        using var harness = new CupertinoThemeTestHarness(Wrap(
            CupertinoFocusHalo.WithRRect(FocusBox(focusNode), BorderRadius.Circular(8.0))));

        Assert.True(focusNode.RequestFocus());
        harness.Pump(ViewSize);

        ShapeDecoration decoration = Assert.Single(FindHaloDecorations(harness.RenderView));
        BorderSide side = Assert.IsType<RoundedRectangleBorder>(decoration.Shape).Side;
        Assert.Equal(3.5, side.Width);
        Assert.Equal(BorderStyle.Solid, side.Style);
        Assert.Equal(0xCC6EADF2u, side.Color.ToUInt32());
    }

    [Fact]
    public void FocusedHalo_LaysOutAndPaintsAtZeroArea()
    {
        var focusNode = new FocusNode();
        using var harness = new CupertinoThemeTestHarness(Wrap(new SizedBox(
            width: 0.0,
            height: 0.0,
            child: CupertinoFocusHalo.WithRect(FocusBox(focusNode)))));

        Assert.True(focusNode.RequestFocus());
        harness.Pump(ViewSize);

        RenderDecoratedBox decorated = Assert.Single(FindAll<RenderDecoratedBox>(harness.RenderView));
        Assert.Equal(default, decorated.Size);
    }

    [Fact]
    public void HslColor_MatchesFlutterConversionAndInterpolationContracts()
    {
        HSLColor blue = HSLColor.FromColor(Color.FromUInt32(0xCC007AFF));
        Assert.Equal(0.8, blue.Alpha, 3);
        Assert.Equal(211.294, blue.Hue, 3);
        Assert.Equal(1.0, blue.Saturation, 3);
        Assert.Equal(0.5, blue.Lightness, 3);
        Assert.Equal(
            0xCC6EADF2u,
            blue.WithLightness(0.69).WithSaturation(0.835).ToColor().ToUInt32());

        HSLColor transparent = HSLColor.Lerp(null, blue, 0.5)!;
        Assert.Equal(0.4, transparent.Alpha, 3);
        Assert.Same(blue, HSLColor.Lerp(blue, blue, 0.5));
    }

    private static Widget Wrap(Widget child)
    {
        return new Directionality(TextDirection.Ltr, child);
    }

    private static Widget FocusBox(FocusNode focusNode)
    {
        return new Focus(
            focusNode: focusNode,
            child: new SizedBox(width: 100.0, height: 50.0));
    }

    private static void AssertFocused(ShapeDecoration decoration)
    {
        BorderSide side = Assert.IsAssignableFrom<OutlinedBorder>(decoration.Shape).Side;
        Assert.Equal(BorderStyle.Solid, side.Style);
        Assert.Equal(3.5, side.Width);
    }

    private static void AssertNotFocused(ShapeDecoration decoration)
    {
        Assert.Equal(BorderSide.None, Assert.IsAssignableFrom<OutlinedBorder>(decoration.Shape).Side);
    }

    private static IReadOnlyList<ShapeDecoration> FindHaloDecorations(RenderObject root)
    {
        return FindAll<RenderDecoratedBox>(root)
            .Select(box => box.DecorationValue)
            .OfType<ShapeDecoration>()
            .ToArray();
    }

    private static IReadOnlyList<T> FindAll<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null)
        {
            return result;
        }

        if (root is T typed)
        {
            result.Add(typed);
        }

        root.VisitChildren(child => result.AddRange(FindAll<T>(child)));
        return result;
    }
}
