using Plumix.Foundation;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/widgets/focus_manager_test.dart
// Dart parity source: flutter/packages/flutter/test/foundation/service_extensions_test.dart
//
// The diagnostics tests of focus_manager_test.dart ('implements debugFillProperties' of the FocusNode
// and FocusScopeNode groups, 'debugDescribeFocusTree produces correct output') and the
// 'Service extensions - debugDumpFocusTree' test.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class FocusDiagnosticsDartParityTests : IDisposable
{
    public FocusDiagnosticsDartParityTests() => FocusManager.Instance.ResetForTests();

    public void Dispose() => FocusManager.Instance.ResetForTests();

    // focus_manager_test.dart's `setupWidget`.
    private static BuildContext SetupWidget(FrameworkDartTester tester)
    {
        GlobalKey widgetKey = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(new Container(key: widgetKey));
        return widgetKey.CurrentContext!;
    }

    private static List<string> Describe(IDiagnosticable node)
    {
        var builder = new DiagnosticPropertiesBuilder();
        node.DebugFillProperties(builder);
        return builder.Properties.Select(static property => property.ToString()).ToList();
    }

    // FocusNode group: 'implements debugFillProperties'
    [DebugOnlyFact]
    public void FocusNodeImplementsDebugFillProperties()
    {
        using var focusNode = new FocusNode(debugLabel: "Label");
        Assert.Equal(
            [
                "context: null",
                "descendantsAreFocusable: true",
                "descendantsAreTraversable: true",
                "canRequestFocus: true",
                "hasFocus: false",
                "hasPrimaryFocus: false",
            ],
            Describe(focusNode));
    }

    // FocusScopeNode group: 'implements debugFillProperties'
    [DebugOnlyFact]
    public void FocusScopeNodeImplementsDebugFillProperties()
    {
        using var scope = new FocusScopeNode(debugLabel: "Scope Label");
        Assert.Equal(
            [
                "context: null",
                "descendantsAreFocusable: true",
                "descendantsAreTraversable: true",
                "canRequestFocus: true",
                "hasFocus: false",
                "hasPrimaryFocus: false",
            ],
            Describe(scope));
    }

    // FocusNode.toStringShort labels the focus state after the debug label.
    [DebugOnlyFact]
    public void ToStringShortDescribesIdentityLabelAndFocusState()
    {
        using var tester = new FrameworkDartTester();
        BuildContext context = SetupWidget(tester);
        using var parent = new FocusNode(debugLabel: "Parent");
        using var child = new FocusNode();
        FocusAttachment parentAttachment = parent.Attach(context);
        FocusAttachment childAttachment = child.Attach(context);
        parentAttachment.Reparent(parent: tester.Owner.FocusManager.RootScope);
        childAttachment.Reparent(parent: parent);
        Assert.Equal("FocusNode#00000(Parent)", FrameworkDartTester.IgnoringHashCodes(parent.ToStringShort()));
        Assert.Equal("FocusNode#00000", FrameworkDartTester.IgnoringHashCodes(child.ToStringShort()));

        child.RequestFocus();
        tester.Pump();

        Assert.Equal(
            "FocusNode#00000(Parent [IN FOCUS PATH])",
            FrameworkDartTester.IgnoringHashCodes(parent.ToStringShort()));
        Assert.Equal("FocusNode#00000([PRIMARY FOCUS])", FrameworkDartTester.IgnoringHashCodes(child.ToStringShort()));
        childAttachment.Detach();
        parentAttachment.Detach();
    }

    // FocusScopeNode group: 'debugDescribeFocusTree produces correct output'
    [DebugOnlyFact]
    public void DebugDescribeFocusTreeProducesCorrectOutput()
    {
        using var tester = new FrameworkDartTester();
        BuildContext context = SetupWidget(tester);
        using var scope1 = new FocusScopeNode(debugLabel: "Scope 1");
        FocusAttachment scope1Attachment = scope1.Attach(context);
        using var scope2 = new FocusScopeNode(); // No label, Just to test that it works.
        FocusAttachment scope2Attachment = scope2.Attach(context);
        using var parent1 = new FocusNode(debugLabel: "Parent 1");
        FocusAttachment parent1Attachment = parent1.Attach(context);
        using var parent2 = new FocusNode(debugLabel: "Parent 2");
        FocusAttachment parent2Attachment = parent2.Attach(context);
        using var child1 = new FocusNode(debugLabel: "Child 1");
        FocusAttachment child1Attachment = child1.Attach(context);
        using var child2 = new FocusNode(); // No label, Just to test that it works.
        FocusAttachment child2Attachment = child2.Attach(context);
        using var child3 = new FocusNode(debugLabel: "Child 3");
        FocusAttachment child3Attachment = child3.Attach(context);
        using var child4 = new FocusNode(debugLabel: "Child 4");
        FocusAttachment child4Attachment = child4.Attach(context);
        scope1Attachment.Reparent(parent: tester.Owner.FocusManager.RootScope);
        scope2Attachment.Reparent(parent: tester.Owner.FocusManager.RootScope);
        parent1Attachment.Reparent(parent: scope1);
        parent2Attachment.Reparent(parent: scope2);
        child1Attachment.Reparent(parent: parent1);
        child2Attachment.Reparent(parent: parent1);
        child3Attachment.Reparent(parent: parent2);
        child4Attachment.Reparent(parent: parent2);
        child4.RequestFocus();
        tester.Pump();

        string description = FrameworkDartTester.IgnoringHashCodes(FocusManager.DebugDescribeFocusTree());

        // The `primaryFocusCreator` lines are the element creator chain, whose private widget names are
        // Plumix's (Dart: `Container-[GlobalKey#00000] ← _FocusInheritedScope ← ... ← View ← [root]`),
        // as is the View scope's `FocusScopeWithExternalFocusNode` context (Dart's
        // `_FocusScopeWithExternalFocusNode`) and the `FocusTraversalGroupNode` type name.
        string creatorChain = ((Element)context).DebugGetCreatorChain(20);
        string[] lines = description.Split('\n');
        Assert.Equal("FocusManager#00000", lines[0]);
        Assert.Equal(" │ primaryFocus: FocusNode#00000(Child 4 [PRIMARY FOCUS])", lines[1]);
        Assert.StartsWith(" │ primaryFocusCreator: Container-[GlobalKey#00000] ←", lines[2]);
        Assert.Equal(
            FrameworkDartTester.IgnoringHashCodes(creatorChain).Replace(" ", string.Empty, StringComparison.Ordinal),
            string.Concat(lines.Skip(2).TakeWhile(static line => line != " │")
                .Select(static line => line.Replace(" │ primaryFocusCreator:", string.Empty, StringComparison.Ordinal)
                    .Replace(" │", string.Empty, StringComparison.Ordinal)
                    .Replace(" ", string.Empty, StringComparison.Ordinal))));
        string tree = string.Join('\n', lines.SkipWhile(static line => line != " │"));
        Assert.Equal(
            " │\n"
            + " └─rootScope: FocusScopeNode#00000(Root Focus Scope [IN FOCUS PATH])\n"
            + "   │ IN FOCUS PATH\n"
            + "   │ focusedChildren: FocusScopeNode#00000([IN FOCUS PATH])\n"
            + "   │\n"
            + "   ├─Child 1: FocusTraversalGroupNode#00000(FocusTraversalGroup)\n"
            + "   │ │ context: Focus\n"
            + "   │ │ NOT FOCUSABLE\n"
            + "   │ │\n"
            + "   │ └─Child 1: FocusScopeNode#00000(View Scope)\n"
            + "   │     context: FocusScopeWithExternalFocusNode\n"
            + "   │\n"
            + "   ├─Child 2: FocusScopeNode#00000(Scope 1)\n"
            + "   │ │ context: Container-[GlobalKey#00000]\n"
            + "   │ │\n"
            + "   │ └─Child 1: FocusNode#00000(Parent 1)\n"
            + "   │   │ context: Container-[GlobalKey#00000]\n"
            + "   │   │\n"
            + "   │   ├─Child 1: FocusNode#00000(Child 1)\n"
            + "   │   │   context: Container-[GlobalKey#00000]\n"
            + "   │   │\n"
            + "   │   └─Child 2: FocusNode#00000\n"
            + "   │       context: Container-[GlobalKey#00000]\n"
            + "   │\n"
            + "   └─Child 3: FocusScopeNode#00000([IN FOCUS PATH])\n"
            + "     │ context: Container-[GlobalKey#00000]\n"
            + "     │ IN FOCUS PATH\n"
            + "     │ focusedChildren: FocusNode#00000(Child 4 [PRIMARY FOCUS])\n"
            + "     │\n"
            + "     └─Child 1: FocusNode#00000(Parent 2 [IN FOCUS PATH])\n"
            + "       │ context: Container-[GlobalKey#00000]\n"
            + "       │ IN FOCUS PATH\n"
            + "       │\n"
            + "       ├─Child 1: FocusNode#00000(Child 3)\n"
            + "       │   context: Container-[GlobalKey#00000]\n"
            + "       │\n"
            + "       └─Child 2: FocusNode#00000(Child 4 [PRIMARY FOCUS])\n"
            + "           context: Container-[GlobalKey#00000]\n"
            + "           PRIMARY FOCUS\n",
            tree);
        foreach (FocusAttachment attachment in new[]
                 {
                     child4Attachment, child3Attachment, child2Attachment, child1Attachment,
                     parent2Attachment, parent1Attachment, scope2Attachment, scope1Attachment,
                 })
        {
            attachment.Detach();
        }
    }

    // service_extensions_test.dart: 'Service extensions - debugDumpFocusTree'
    [DebugOnlyFact]
    public async Task DebugDumpFocusTreeServiceExtension()
    {
        Assert.Contains("ext.flutter.debugDumpFocusTree", BindingBase.ServiceExtensionMethods);

        // flutter_test's `testExtension` calls the registered callback directly, bypassing the VM
        // wrapper (whose zero-delay timer the test host does not run).
        Dictionary<string, object?> result =
            await BindingBase.RegisteredCallbacks["debugDumpFocusTree"](new Dictionary<string, string>());

        string data = Assert.IsType<string>(result["data"]);
        Assert.Matches(
            "^FocusManager#[0-9a-f]{5}\n"
            + " └─rootScope: FocusScopeNode#[0-9a-f]{5}\\(Root Focus Scope\\)\n$",
            data);
        Assert.Equal(WidgetsBinding.Instance.FocusManager.ToStringDeep(), data);
    }
}
