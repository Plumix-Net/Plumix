using Avalonia;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/widgets/focus_scope_test.dart (parity regression tests)

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class FocusScopeTests : IDisposable
{
    private static readonly Size ViewSize = new(200, 200);

    public FocusScopeTests()
    {
        FocusManager.Instance.ResetForTests();
    }

    public void Dispose()
    {
        FocusManager.Instance.ResetForTests();
    }

    [Fact]
    public void Focus_WithExternalFocusNodeLeavesTheNodesOwnConfigurationAlone()
    {
        var node = new FocusNode(debugLabel: "external", skipTraversal: true, canRequestFocus: false);
        using var harness = new FocusLayoutHarness(new Directionality(
            TextDirection.Ltr,
            Focus.WithExternalFocusNode(node, new SizedBox(width: 20, height: 20))));
        harness.Layout(new Size(200, 200));

        Assert.True(node.SkipTraversal);
        Assert.False(node.CanRequestFocus);
    }

    [Fact]
    public void Focus_ParentNodeReparentsTheNodeOutsideTheWidgetHierarchy()
    {
        var detachedParent = new FocusScopeNode(debugLabel: "detachedParent");
        var node = new FocusNode(debugLabel: "node");
        FocusManager.Instance.RegisterNode(detachedParent);
        using var harness = new FocusLayoutHarness(new Directionality(
            TextDirection.Ltr,
            new Focus(
                focusNode: new FocusNode(debugLabel: "widgetParent"),
                child: new Focus(
                    focusNode: node,
                    parentNode: detachedParent,
                    child: new SizedBox(width: 20, height: 20)))));
        harness.Layout(new Size(200, 200));

        Assert.Same(detachedParent, node.Parent);
        Assert.Same(detachedParent, node.EnclosingScope);
    }

    [Fact]
    public void Focus_MaybeOfSkipsScopeNodesUnlessScopeOkIsSet()
    {
        var scopeNode = new FocusScopeNode(debugLabel: "scope");
        BuildContext captured = default;
        using var harness = new FocusLayoutHarness(new Directionality(
            TextDirection.Ltr,
            FocusScope.WithExternalFocusNode(
                scopeNode,
                new Builder(context =>
                {
                    captured = context;
                    return new SizedBox(width: 20, height: 20);
                }))));
        harness.Layout(ViewSize);

        Assert.Null(Focus.MaybeOf(captured));
        Assert.Same(scopeNode, Focus.MaybeOf(captured, scopeOk: true));
        Assert.Same(scopeNode, FocusScope.Of(captured));
    }

    [Fact]
    public void FocusScope_OfFallsBackToTheRootScopeWithoutAFocusAncestor()
    {
        BuildContext captured = default;
        using var harness = new FocusLayoutHarness(new Directionality(
            TextDirection.Ltr,
            new Builder(context =>
            {
                captured = context;
                return new SizedBox(width: 20, height: 20);
            })));
        harness.Layout(ViewSize);

        Assert.Null(Focus.MaybeOf(captured, scopeOk: true));
        Assert.Same(FocusManager.Instance.RootScope, FocusScope.Of(captured));
    }

    [Fact]
    public void ExcludeFocus_BlocksDescendantsWithoutRemovingThemFromTheTree()
    {
        var blocked = new FocusNode(debugLabel: "blocked");
        using var harness = new FocusLayoutHarness(new Directionality(
            TextDirection.Ltr,
            new ExcludeFocus(
                child: new Focus(focusNode: blocked, child: new SizedBox(width: 20, height: 20)))));
        harness.Layout(ViewSize);

        Assert.NotNull(blocked.Parent);
        Assert.False(blocked.CanRequestFocus);
        Assert.False(blocked.RequestFocus());
    }

    [Fact]
    public void ExcludeFocus_WithExcludingFalseLeavesDescendantsFocusable()
    {
        var node = new FocusNode(debugLabel: "node");
        using var harness = new FocusLayoutHarness(new Directionality(
            TextDirection.Ltr,
            new ExcludeFocus(
                excluding: false,
                child: new Focus(focusNode: node, child: new SizedBox(width: 20, height: 20)))));
        harness.Layout(ViewSize);

        Assert.True(node.CanRequestFocus);
        Assert.True(node.RequestFocus());
    }

    [Fact]
    public void Focus_OnFocusChangeReportsTheAncestorInclusiveFocusState()
    {
        var parent = new FocusNode(debugLabel: "parent");
        var child = new FocusNode(debugLabel: "child");
        var parentChanges = new List<bool>();
        using var harness = new FocusLayoutHarness(new Directionality(
            TextDirection.Ltr,
            new Focus(
                focusNode: parent,
                onFocusChange: parentChanges.Add,
                child: new Focus(focusNode: child, child: new SizedBox(width: 20, height: 20)))));
        harness.Layout(ViewSize);

        Assert.True(child.RequestFocus());
        // Dart reports `hasFocus`, which is true for every node on the primary focus path.
        Assert.Contains(true, parentChanges);
        Assert.True(parent.HasFocus);
        Assert.False(parent.HasPrimaryFocus);
    }

    [Fact]
    public void FocusScope_KeepsItsFocusedChildAcrossAFocusHandoffToASiblingScope()
    {
        var firstScope = new FocusScopeNode(debugLabel: "first");
        var secondScope = new FocusScopeNode(debugLabel: "second");
        var firstChild = new FocusNode(debugLabel: "firstChild");
        var secondChild = new FocusNode(debugLabel: "secondChild");
        using var harness = new FocusLayoutHarness(new Directionality(
            TextDirection.Ltr,
            new Row(children:
            [
                FocusScope.WithExternalFocusNode(
                    firstScope,
                    new Focus(
                        focusNode: firstChild,
                        autofocus: true,
                        child: new SizedBox(width: 20, height: 20))),
                FocusScope.WithExternalFocusNode(
                    secondScope,
                    new Focus(focusNode: secondChild, child: new SizedBox(width: 20, height: 20))),
            ])));
        harness.Layout(ViewSize);

        Assert.Same(firstChild, firstScope.FocusedChild);
        Assert.True(secondChild.RequestFocus());
        Assert.Same(firstChild, firstScope.FocusedChild);
        Assert.Same(secondChild, secondScope.FocusedChild);

        Assert.True(firstScope.RequestFocus());
        Assert.Same(firstChild, FocusManager.Instance.PrimaryFocus);
    }
}
