using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/lib/src/semantics/semantics.dart
// Mirrors the SemanticsNode diagnostics tests of flutter/packages/flutter/test/semantics/semantics_test.dart.

[Collection(SchedulerTestCollection.Name)]
public sealed class SemanticsNodeDiagnosticsTests
{
    public SemanticsNodeDiagnosticsTests()
    {
        SemanticsNode.DebugResetSemanticsIdCounter();
    }

    [Fact]
    public void ToStringDeep_DoesNotThrowWithoutATransform()
    {
        var child1 = new SemanticsNode { Rect = FromLtrb(0.0, 0.0, 5.0, 5.0) };
        var child2 = new SemanticsNode { Rect = FromLtrb(5.0, 0.0, 10.0, 5.0) };
        var root = new SemanticsNode { Rect = FromLtrb(0.0, 0.0, 10.0, 5.0) };
        root.UpdateWith(config: null, childrenInInversePaintOrder: [child1, child2]);

        Assert.Null(root.Transform);
        Assert.Null(child1.Transform);
        Assert.Null(child2.Transform);
        Assert.Equal(
            "SemanticsNode#3\n"
            + " │ STALE\n"
            + " │ owner: null\n"
            + " │ Rect.fromLTRB(0.0, 0.0, 10.0, 5.0)\n"
            + " │\n"
            + " ├─SemanticsNode#1\n"
            + " │   STALE\n"
            + " │   owner: null\n"
            + " │   Rect.fromLTRB(0.0, 0.0, 5.0, 5.0)\n"
            + " │\n"
            + " └─SemanticsNode#2\n"
            + "     STALE\n"
            + "     owner: null\n"
            + "     Rect.fromLTRB(5.0, 0.0, 10.0, 5.0)\n",
            root.ToStringDeep());
    }

    [Fact]
    public void ToStringDeep_RespectsTheChildOrderParameter()
    {
        var child1 = new SemanticsNode { Rect = FromLtrb(15.0, 0.0, 20.0, 5.0) };
        var child2 = new SemanticsNode { Rect = FromLtrb(10.0, 0.0, 15.0, 5.0) };
        var root = new SemanticsNode { Rect = FromLtrb(0.0, 0.0, 20.0, 5.0) };
        root.UpdateWith(config: null, childrenInInversePaintOrder: [child1, child2]);
        const string Expected =
            "SemanticsNode#3\n"
            + " │ STALE\n"
            + " │ owner: null\n"
            + " │ Rect.fromLTRB(0.0, 0.0, 20.0, 5.0)\n"
            + " │\n"
            + " ├─SemanticsNode#1\n"
            + " │   STALE\n"
            + " │   owner: null\n"
            + " │   Rect.fromLTRB(15.0, 0.0, 20.0, 5.0)\n"
            + " │\n"
            + " └─SemanticsNode#2\n"
            + "     STALE\n"
            + "     owner: null\n"
            + "     Rect.fromLTRB(10.0, 0.0, 15.0, 5.0)\n";

        Assert.Equal(Expected, root.ToStringDeep());
        Assert.Equal(Expected, root.ToStringDeep(childOrder: DebugSemanticsDumpOrder.InverseHitTest));

        var child3 = new SemanticsNode { Rect = FromLtrb(0.0, 0.0, 10.0, 5.0) };
        child3.UpdateWith(
            config: null,
            childrenInInversePaintOrder:
            [
                new SemanticsNode { Rect = FromLtrb(5.0, 0.0, 10.0, 5.0) },
                new SemanticsNode { Rect = FromLtrb(0.0, 0.0, 5.0, 5.0) },
            ]);
        var rootComplex = new SemanticsNode { Rect = FromLtrb(0.0, 0.0, 25.0, 5.0) };
        rootComplex.UpdateWith(config: null, childrenInInversePaintOrder: [child1, child2, child3]);
        const string ExpectedComplex =
            "SemanticsNode#7\n"
            + " │ STALE\n"
            + " │ owner: null\n"
            + " │ Rect.fromLTRB(0.0, 0.0, 25.0, 5.0)\n"
            + " │\n"
            + " ├─SemanticsNode#1\n"
            + " │   STALE\n"
            + " │   owner: null\n"
            + " │   Rect.fromLTRB(15.0, 0.0, 20.0, 5.0)\n"
            + " │\n"
            + " ├─SemanticsNode#2\n"
            + " │   STALE\n"
            + " │   owner: null\n"
            + " │   Rect.fromLTRB(10.0, 0.0, 15.0, 5.0)\n"
            + " │\n"
            + " └─SemanticsNode#4\n"
            + "   │ STALE\n"
            + "   │ owner: null\n"
            + "   │ Rect.fromLTRB(0.0, 0.0, 10.0, 5.0)\n"
            + "   │\n"
            + "   ├─SemanticsNode#5\n"
            + "   │   STALE\n"
            + "   │   owner: null\n"
            + "   │   Rect.fromLTRB(5.0, 0.0, 10.0, 5.0)\n"
            + "   │\n"
            + "   └─SemanticsNode#6\n"
            + "       STALE\n"
            + "       owner: null\n"
            + "       Rect.fromLTRB(0.0, 0.0, 5.0, 5.0)\n";

        Assert.Equal(ExpectedComplex, rootComplex.ToStringDeep());
        Assert.Equal(
            ExpectedComplex,
            rootComplex.ToStringDeep(childOrder: DebugSemanticsDumpOrder.InverseHitTest));
    }

    [Fact]
    public void DebugProperties()
    {
        var minimalProperties = new SemanticsNode();
        Assert.Equal(
            "SemanticsNode#1\n"
            + "   Rect.fromLTRB(0.0, 0.0, 0.0, 0.0)\n"
            + "   invisible\n",
            minimalProperties.ToStringDeep());
        Assert.Equal(
            "SemanticsNode#1\n"
            + "   owner: null\n"
            + "   isMergedIntoParent: false\n"
            + "   mergeAllDescendantsIntoThisNode: false\n"
            + "   Rect.fromLTRB(0.0, 0.0, 0.0, 0.0)\n"
            + HiddenTail("[]", "[]"),
            minimalProperties.ToStringDeep(minLevel: DiagnosticLevel.Hidden));

        var config = new SemanticsConfiguration
        {
            IsSemanticBoundary = true,
            IsMergingSemanticsOfDescendants = true,
            OnScrollUp = static () => { },
            OnLongPress = static () => { },
            OnShowOnScreen = static () => { },
            IsChecked = false,
            IsSelected = true,
            IsButton = true,
            Label = "Use all the properties",
            TextDirection = TextDirection.Rtl,
            SortKey = new OrdinalSortKey(1.0),
        };
        var allProperties = new SemanticsNode
        {
            Rect = new Rect(50.0, 10.0, 20.0, 30.0),
            Transform = Matrix4.TranslationValues(10.0, 10.0, 0.0),
        };
        allProperties.UpdateWith(config);
        Assert.Equal(
            "SemanticsNode#2\n"
            + "   STALE\n"
            + "   owner: null\n"
            + "   merge boundary ⛔️\n"
            + "   Rect.fromLTRB(60.0, 20.0, 80.0, 50.0)\n"
            + "   actions: longPress, scrollUp, showOnScreen\n"
            + "   flags: hasCheckedState, isSelected, isButton, hasSelectedState\n"
            + "   label: \"Use all the properties\"\n"
            + "   textDirection: rtl\n"
            + "   sortKey: OrdinalSortKey#00000(order: 1.0)\n",
            IgnoringHashCodes(allProperties.ToStringDeep()));

        var scaled = new SemanticsNode
        {
            Rect = new Rect(50.0, 10.0, 20.0, 30.0),
            Transform = Matrix4.Diagonal3Values(10.0, 10.0, 1.0),
        };
        Assert.Equal(
            "SemanticsNode#3\n"
            + "   STALE\n"
            + "   owner: null\n"
            + "   Rect.fromLTRB(50.0, 10.0, 70.0, 40.0) scaled by 10.0x\n",
            scaled.ToStringDeep());
    }

    [Fact]
    public void BlockedActionsDebugProperties()
    {
        var config = new SemanticsConfiguration
        {
            IsBlockingUserActions = true,
            OnScrollUp = static () => { },
            OnLongPress = static () => { },
            OnShowOnScreen = static () => { },
            OnDidGainAccessibilityFocus = static () => { },
        };
        var blocked = new SemanticsNode
        {
            Rect = new Rect(50.0, 10.0, 20.0, 30.0),
            Transform = Matrix4.TranslationValues(10.0, 10.0, 0.0),
        };
        blocked.UpdateWith(config);

        Assert.Equal(
            "SemanticsNode#1\n"
            + "   STALE\n"
            + "   owner: null\n"
            + "   Rect.fromLTRB(60.0, 20.0, 80.0, 50.0)\n"
            + "   actions: didGainAccessibilityFocus, longPress🚫️, scrollUp🚫️,\n"
            + "     showOnScreen🚫️\n",
            blocked.ToStringDeep());
    }

    [Fact]
    public void ValidationResultDebugProperties()
    {
        var nodeWithValidationResult = new SemanticsNode();
        nodeWithValidationResult.UpdateWith(
            new SemanticsConfiguration { ValidationResult = SemanticsValidationResult.Valid });

        Assert.Equal(
            "SemanticsNode#1\n"
            + "   STALE\n"
            + "   owner: null\n"
            + "   Rect.fromLTRB(0.0, 0.0, 0.0, 0.0)\n"
            + "   invisible\n"
            + "   validationResult: valid\n",
            nodeWithValidationResult.ToStringDeep());
    }

    [Fact]
    public void CustomActionsDebugProperties()
    {
        var configuration = new SemanticsConfiguration();
        configuration.ReplaceCustomActionHandlers(new Dictionary<CustomSemanticsAction, Action>
        {
            [new CustomSemanticsAction("action1")] = static () => { },
            [new CustomSemanticsAction("action2")] = static () => { },
            [new CustomSemanticsAction("action3")] = static () => { },
        });
        var actionNode = new SemanticsNode();
        actionNode.UpdateWith(configuration);

        Assert.Equal(
            "SemanticsNode#1\n"
            + "   STALE\n"
            + "   owner: null\n"
            + "   isMergedIntoParent: false\n"
            + "   mergeAllDescendantsIntoThisNode: false\n"
            + "   Rect.fromLTRB(0.0, 0.0, 0.0, 0.0)\n"
            + HiddenTail("customAction", "action1, action2, action3"),
            actionNode.ToStringDeep(minLevel: DiagnosticLevel.Hidden));
    }

    [Fact]
    public void ToStringShort_IsTheTypeAndId()
    {
        var node = new SemanticsNode();

        Assert.Equal($"SemanticsNode#{node.Id}", node.ToStringShort());
    }

    /// <summary>The properties after the rect that every hidden-level dump of a plain node lists.</summary>
    private static string HiddenTail(string actions, string customActions) =>
        "   tags: null\n"
        + $"   actions: {actions}\n"
        + $"   customActions: {customActions}\n"
        + "   flags: []\n"
        + "   invisible\n"
        + "   isHidden: false\n"
        + "   identifier: \"\"\n"
        + "   traversalParentIdentifier: null\n"
        + "   traversalChildIdentifier: null\n"
        + "   label: \"\"\n"
        + "   value: \"\"\n"
        + "   increasedValue: \"\"\n"
        + "   decreasedValue: \"\"\n"
        + "   hint: \"\"\n"
        + "   tooltip: \"\"\n"
        + "   textDirection: null\n"
        + "   sortKey: null\n"
        + "   platformViewId: null\n"
        + "   maxValueLength: null\n"
        + "   currentValueLength: null\n"
        + "   scrollChildren: null\n"
        + "   scrollIndex: null\n"
        + "   scrollExtentMin: null\n"
        + "   scrollPosition: null\n"
        + "   scrollExtentMax: null\n"
        + "   indexInParent: null\n"
        + "   headingLevel: 0\n"
        + "   minValue: null\n"
        + "   maxValue: null\n";

    private static Rect FromLtrb(double left, double top, double right, double bottom) =>
        new(left, top, right - left, bottom - top);

    /// <summary>flutter_test's <c>equalsIgnoringHashCodes</c>: every <c>#xxxxx</c> becomes <c>#00000</c>.</summary>
    private static string IgnoringHashCodes(string value) =>
        System.Text.RegularExpressions.Regex.Replace(value, "#[0-9a-f]{5}", "#00000");
}
