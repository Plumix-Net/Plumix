using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/widgets/accessibility_evaluations_test.dart
//
// Every test of accessibility_evaluations_test.dart, on flutter_test's default view (800x600 logical at a
// device pixel ratio of 3.0). The contrast evaluations rasterize the view through the headless Skia
// backend (`RasterBackend`), whose test paragraph engine paints glyphs as solid boxes like flutter_test's
// font.

namespace Plumix.Tests;

public sealed class AccessibilityEvaluationsDartParityTests : IDisposable
{
    private readonly bool _previousEnabled = Features.IsAccessibilityEvaluationsEnabled;
    private readonly FrameworkDartTester _tester = new(devicePixelRatio: 3.0);
    private readonly SemanticsHandle _handle;

    public AccessibilityEvaluationsDartParityTests()
    {
        // setUpAll: debugEnabledFeatureFlags.add('accessibility_evaluations').
        Features.IsAccessibilityEvaluationsEnabled = true;
        WidgetsBinding.Instance.DebugOverrideRootForTests(_tester.Root, _tester.Owner);
        _handle = _tester.EnsureSemantics();
    }

    public void Dispose()
    {
        _handle.Dispose();
        WidgetsBinding.Instance.DebugOverrideRootForTests(null, null);
        _tester.Dispose();
        Features.IsAccessibilityEvaluationsEnabled = _previousEnabled;
    }

    private static Task<EvaluationResult> Evaluate(AccessibilityEvaluation evaluation) =>
        evaluation.Evaluate(WidgetsBinding.Instance).AsTask();

    private void Pump(Widget widget)
    {
        _tester.PumpWidget(widget);
        _tester.Pump();
    }

    private static Widget OnWhite(Widget child) => new SizedBox(
        width: 100.0,
        height: 100.0,
        child: new ColoredBox(color: new Color(0xFFFFFFFF), child: new Center(child: child)));

    private static Widget Square(double dimension, Widget? child = null) =>
        new SizedBox(width: dimension, height: dimension, child: child);

    private static Widget Box10(Widget? child = null) => new SizedBox(width: 10.0, height: 10.0, child: child);

    // ------------------------------------------------------------------ MinimumTapTargetEvaluation

    private static readonly MinimumTapTargetEvaluation TapTarget = new(size: new Size(48.0, 48.0));

    [Fact]
    public async Task MinimumTapTarget_PassesForValidTargets()
    {
        Pump(new TestWidgetsApp(home: Square(48.0, new Semantics(label: "button", onTap: () => { }))));

        EvaluationResult result = await Evaluate(TapTarget);

        Assert.Empty(result.Violations);
    }

    [Fact]
    public async Task MinimumTapTarget_FailsForSmallTargets()
    {
        Pump(new TestWidgetsApp(
            home: new Center(child: Square(40.0, new Semantics(label: "button", onTap: () => { })))));

        EvaluationResult result = await Evaluate(TapTarget);

        Violation violation = Assert.Single(result.Violations);
        Assert.Contains("expected tap target size of at least Size(48.0, 48.0)", violation.Reason);
        Assert.EndsWith("but found Size(40.0, 40.0)\n", violation.Reason);
    }

    [Fact]
    public async Task MinimumTapTarget_SkipsHiddenNodes()
    {
        Pump(new TestWidgetsApp(
            home: Square(40.0, new Semantics(label: "button", onTap: () => { }, hidden: true))));

        EvaluationResult result = await Evaluate(TapTarget);

        Assert.Empty(result.Violations);
    }

    [Fact]
    public void MinimumTapTarget_CompletesSynchronously()
    {
        Pump(new TestWidgetsApp(
            home: new Center(child: Square(40.0, new Semantics(label: "button", onTap: () => { })))));

        ValueTask<EvaluationResult> pending = TapTarget.Evaluate(WidgetsBinding.Instance);

        Assert.True(pending.IsCompletedSuccessfully);
    }

    // ------------------------------------------------------------------ LabeledTapTargetEvaluation

    [Fact]
    public async Task LabeledTapTarget_PassesForLabeledTargets()
    {
        Pump(new TestWidgetsApp(home: Square(48.0, new Semantics(label: "button", onTap: () => { }))));

        EvaluationResult result = await Evaluate(new LabeledTapTargetEvaluation());

        Assert.Empty(result.Violations);
    }

    [Fact]
    public async Task LabeledTapTarget_FailsForUnlabeledTargets()
    {
        Pump(new TestWidgetsApp(home: Square(48.0, new Semantics(onTap: () => { }))));

        EvaluationResult result = await Evaluate(new LabeledTapTargetEvaluation());

        Violation violation = Assert.Single(result.Violations);
        Assert.Contains("expected tappable node to have semantic label", violation.Reason);
    }

    // ------------------------------------------------------------------ MinimumTextContrastEvaluation

    private static readonly MinimumTextContrastEvaluation TextContrast = new(
        minNormalTextContrastRatio: 4.5,
        minLargeTextContrastRatio: 3.0);

    [Fact]
    public async Task MinimumTextContrast_PassesForHighContrast()
    {
        Pump(new TestWidgetsApp(home: OnWhite(
            new Text("test", style: new TextStyle(Color: new Color(0xFF000000), FontSize: 14)))));

        EvaluationResult result = await Evaluate(TextContrast);

        Assert.True(result.Violations.Count == 0, string.Join("\n", result.Violations.Select(v => v.Reason)));
    }

    [Fact]
    public async Task MinimumTextContrast_FailsForLowContrast()
    {
        Pump(new TestWidgetsApp(home: OnWhite(
            new Text("test", style: new TextStyle(Color: new Color(0xFFEEEEEE), FontSize: 14)))));

        EvaluationResult result = await Evaluate(TextContrast);

        Violation violation = Assert.Single(result.Violations);
        Assert.Contains("Expected contrast ratio of at least 4.5", violation.Reason);
        Assert.Contains("for a font size of 14.0.\n", violation.Reason);
        Assert.Contains("light - Color(alpha: 1.0000, red: 1.0000, green: 1.0000, blue: 1.0000", violation.Reason);
        Assert.Contains("dark - Color(alpha: 1.0000, red: 0.9333, green: 0.9333, blue: 0.9333", violation.Reason);
        Assert.EndsWith(
            "See also: https://www.w3.org/TR/UNDERSTANDING-WCAG20/visual-audio-contrast-contrast.html",
            violation.Reason);
    }

    [Fact]
    public async Task MinimumTextContrast_UsesTheLargeTextRatioForBoldFourteenPointText()
    {
        // 0xFF767676 on white is about 4.5: below the normal ratio of 10, so only the large-text ratio
        // (bold text from 14 up) lets it pass.
        Pump(new TestWidgetsApp(home: OnWhite(new Text(
            "test",
            style: new TextStyle(Color: new Color(0xFF767676),
                FontSize: 14,
                FontWeight: Avalonia.Media.FontWeight.Bold)))));

        EvaluationResult result = await Evaluate(new MinimumTextContrastEvaluation(
            minNormalTextContrastRatio: 10.0,
            minLargeTextContrastRatio: 3.0));

        Assert.Empty(result.Violations);
    }

    // ------------------------------------------------------------------ UnlabeledLeafNodeEvaluation

    private static readonly UnlabeledLeafNodeEvaluation UnlabeledLeaf = new();

    private const string UnlabeledLeafReason = "expected leaf semantics node to have a label";

    [Fact]
    public async Task UnlabeledLeaf_PassesIfNodeHasLabel()
    {
        Pump(new TestWidgetsApp(home: new Semantics(container: true, label: "test", child: Box10())));

        Assert.Empty((await Evaluate(UnlabeledLeaf)).Violations);
    }

    [Fact]
    public async Task UnlabeledLeaf_PassesIfNodeHasTooltip()
    {
        Pump(new TestWidgetsApp(home: new Semantics(container: true, tooltip: "test", child: Box10())));

        Assert.Empty((await Evaluate(UnlabeledLeaf)).Violations);
    }

    [Fact]
    public async Task UnlabeledLeaf_PassesIfNodeHasValue()
    {
        Pump(new TestWidgetsApp(
            home: new Semantics(container: true, onTap: () => { }, value: "test", child: Box10())));

        Assert.Empty((await Evaluate(UnlabeledLeaf)).Violations);
    }

    [Fact]
    public async Task UnlabeledLeaf_PassesIfNodeHasHint()
    {
        Pump(new TestWidgetsApp(
            home: new Semantics(container: true, onTap: () => { }, hint: "test", child: Box10())));

        Assert.Empty((await Evaluate(UnlabeledLeaf)).Violations);
    }

    [Fact]
    public async Task UnlabeledLeaf_FailsIfNodeHasNoLabel()
    {
        Pump(new TestWidgetsApp(home: new Semantics(container: true, onTap: () => { }, child: Box10())));

        EvaluationResult result = await Evaluate(UnlabeledLeaf);

        Assert.Contains(UnlabeledLeafReason, Assert.Single(result.Violations).Reason);
    }

    [Fact]
    public async Task UnlabeledLeaf_FailsIfButtonNodeHasNoLabel()
    {
        Pump(new TestWidgetsApp(home: new Semantics(container: true, button: true, child: Box10())));

        EvaluationResult result = await Evaluate(UnlabeledLeaf);

        Assert.Contains(UnlabeledLeafReason, Assert.Single(result.Violations).Reason);
    }

    [Fact]
    public async Task UnlabeledLeaf_FailsIfFocusableNodeHasNoLabel()
    {
        Pump(new TestWidgetsApp(home: new Semantics(container: true, focusable: true, child: Box10())));

        EvaluationResult result = await Evaluate(UnlabeledLeaf);

        Assert.Contains(UnlabeledLeafReason, Assert.Single(result.Violations).Reason);
    }

    [Fact]
    public async Task UnlabeledLeaf_PassesIfActionableNodeIsHidden()
    {
        Pump(new TestWidgetsApp(
            home: new Semantics(container: true, hidden: true, button: true, child: Box10())));

        Assert.Empty((await Evaluate(UnlabeledLeaf)).Violations);
    }

    [Fact]
    public async Task UnlabeledLeaf_PassesIfNodeIsNotFocusableOrActionable()
    {
        Pump(new TestWidgetsApp(home: new Semantics(container: true, child: Box10())));

        Assert.Empty((await Evaluate(UnlabeledLeaf)).Violations);
    }

    [Fact]
    public async Task UnlabeledLeaf_PassesIfNodeIsNotALeaf()
    {
        Pump(new TestWidgetsApp(home: new Semantics(
            container: true,
            onTap: () => { },
            child: new Column(children: [new Semantics(label: "Child 1", onTap: () => { }, child: Box10())]))));

        Assert.Empty((await Evaluate(UnlabeledLeaf)).Violations);
    }

    [Fact]
    public async Task UnlabeledLeaf_PassesIfNodeMergesDescendants()
    {
        Pump(new TestWidgetsApp(home: new MergeSemantics(
            child: new Column(children: [new Semantics(label: "Child 1", onTap: () => { }, child: Box10())]))));

        Assert.Empty((await Evaluate(UnlabeledLeaf)).Violations);
    }

    [Fact]
    public async Task UnlabeledLeaf_FailsIfNodeMergesDescendantsButHasNoLabel()
    {
        Pump(new TestWidgetsApp(home: new MergeSemantics(
            child: new Semantics(container: true, onTap: () => { }, child: Box10()))));

        EvaluationResult result = await Evaluate(UnlabeledLeaf);

        Assert.Contains(UnlabeledLeafReason, Assert.Single(result.Violations).Reason);
    }

    // ------------------------------------------------------------------ MinimumNonTextContrastEvaluation

    private static readonly MinimumNonTextContrastEvaluation NonTextContrast = new();

    private const string NonTextReason = "Expected non-text control contrast ratio of at least 3.0";

    private static Widget Fill(uint color) => Square(50.0, new ColoredBox(color: new Color(color)));

    [Fact]
    public async Task MinimumNonTextContrast_PassesForHighContrastButton()
    {
        Pump(new TestWidgetsApp(home: OnWhite(new Semantics(button: true, container: true, child: Fill(0xFF000000)))));

        Assert.Empty((await Evaluate(NonTextContrast)).Violations);
    }

    [Fact]
    public async Task MinimumNonTextContrast_FailsForLowContrastButton()
    {
        Pump(new TestWidgetsApp(home: OnWhite(new Semantics(button: true, container: true, child: Fill(0xFFEEEEEE)))));

        EvaluationResult result = await Evaluate(NonTextContrast);

        Violation violation = Assert.Single(result.Violations);
        Assert.Contains(NonTextReason, violation.Reason);
        Assert.EndsWith("https://www.w3.org/WAI/WCAG22/Understanding/non-text-contrast.html", violation.Reason);
    }

    [Fact]
    public async Task MinimumNonTextContrast_PassesWhenTransparentAndBackgroundIsUniform()
    {
        Pump(new TestWidgetsApp(home: OnWhite(new Semantics(button: true, container: true, child: Square(50.0)))));

        Assert.Empty((await Evaluate(NonTextContrast)).Violations);
    }

    [Fact]
    public async Task MinimumNonTextContrast_FailsForLowContrastSlider()
    {
        Pump(new TestWidgetsApp(home: OnWhite(new Semantics(slider: true, container: true, child: Fill(0xFFEEEEEE)))));

        EvaluationResult result = await Evaluate(NonTextContrast);

        Assert.Contains(NonTextReason, Assert.Single(result.Violations).Reason);
    }

    [Fact]
    public async Task MinimumNonTextContrast_PassesForHighContrastTextField()
    {
        Pump(new TestWidgetsApp(
            home: OnWhite(new Semantics(textField: true, container: true, child: Fill(0xFF000000)))));

        Assert.Empty((await Evaluate(NonTextContrast)).Violations);
    }

    [Fact]
    public async Task MinimumNonTextContrast_FailsForLowContrastNodeWithOnTap()
    {
        Pump(new TestWidgetsApp(
            home: OnWhite(new Semantics(onTap: () => { }, container: true, child: Fill(0xFFEEEEEE)))));

        EvaluationResult result = await Evaluate(NonTextContrast);

        Assert.Contains(NonTextReason, Assert.Single(result.Violations).Reason);
    }

    // ------------------------------------------------------------------ TitleEvaluation

    private const string TitleReason = "Expected to find at least one Title widget, but none was found.";

    [Fact]
    public async Task Title_PassesIfThereIsAtLeastOneTitleWidget()
    {
        Pump(new TestWidgetsApp(home: new Title(title: "Title", color: new Color(0xFF000000), child: new SizedBox())));

        Assert.Empty((await Evaluate(new TitleEvaluation())).Violations);
    }

    [Fact]
    public async Task Title_PassesIfTitleWidgetIsDeeplyNested()
    {
        Pump(new TestWidgetsApp(home: new SizedBox(child: new Padding(
            EdgeInsets.All(8.0),
            child: new Center(
                child: new Title(title: "Nested Title", color: new Color(0xFF000000), child: new SizedBox()))))));

        Assert.Empty((await Evaluate(new TitleEvaluation())).Violations);
    }

    [Fact]
    public async Task Title_FailsIfThereIsNoTitleWidget()
    {
        Pump(new Directionality(TextDirection.Ltr, new SizedBox()));

        EvaluationResult result = await Evaluate(new TitleEvaluation());

        Violation violation = Assert.Single(result.Violations);
        Assert.Contains(TitleReason, violation.Reason);
        Assert.Same(_tester.RenderView.Owner!.SemanticsOwner!.RootNode, violation.Node);
    }

    [Fact]
    public async Task Title_FailsIfTitleWidgetIsMissingInDeeplyNestedTree()
    {
        Pump(new Directionality(TextDirection.Ltr, new SizedBox(child: new Padding(
            EdgeInsets.All(8.0),
            child: new Center(child: new Text("No title here"))))));

        EvaluationResult result = await Evaluate(new TitleEvaluation());

        Assert.Contains(TitleReason, Assert.Single(result.Violations).Reason);
    }

    // ------------------------------------------------------------------ feature flag

    [Fact]
    public void Evaluate_ThrowsSynchronouslyWhenTheFeatureFlagIsOff()
    {
        Features.IsAccessibilityEvaluationsEnabled = false;

        var exception = Assert.Throws<NotSupportedException>(() => TapTarget.Evaluate(WidgetsBinding.Instance));

        Assert.StartsWith("Accessibility evaluations APIs are not enabled.\n\n", exception.Message);
        Assert.EndsWith("2. Turn on the accessibility evaluations feature flag. (See flutter config --help)\n",
            exception.Message);
    }
}
