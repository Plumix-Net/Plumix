using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/widgets/accessibility_evaluations_service_extension_test.dart
//
// Dart's `AccessibilityEvaluationTestBinding.testExtension` calls the registered callback directly, so the
// results carry no `type`/`method` keys and errors are not wrapped into an error response; the callback
// registered with `BindingBase` is invoked the same way here.

namespace Plumix.Tests;

public sealed class AccessibilityEvaluationsServiceExtensionDartParityTests : IDisposable
{
    private readonly bool _previousEnabled = Features.IsAccessibilityEvaluationsEnabled;
    private readonly FrameworkDartTester _tester = new(devicePixelRatio: 3.0);
    private readonly SemanticsHandle _handle;

    public AccessibilityEvaluationsServiceExtensionDartParityTests()
    {
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

    private static Task<Dictionary<string, object?>> RunEvaluation(Dictionary<string, string> parameters) =>
        BindingBase.RegisteredCallbacks[WidgetsServiceExtensions.AccessibilityEvaluations.DartName()](parameters);

    private static List<Dictionary<string, string>> Violations(Dictionary<string, object?> result) =>
        Assert.IsType<List<Dictionary<string, string>>>(result["result"]);

    private void Pump(Widget widget)
    {
        _tester.PumpWidget(widget);
        _tester.Pump();
    }

    private void PumpButton(double dimension, string? label) =>
        Pump(new TestWidgetsApp(home: new Center(child: new SizedBox(
            width: dimension,
            height: dimension,
            child: new Semantics(label: label, onTap: () => { })))));

    private void PumpTestWidget() => PumpButton(48.0, "button");

    [NonReleaseFact]
    public void IsRegisteredWithTheWidgetsBinding()
    {
        Assert.Contains("ext.flutter.accessibilityEvaluations", BindingBase.ServiceExtensionMethods);
    }

    [NonReleaseFact]
    public async Task ReturnsViolationsForMinimumTapTargetEvaluation()
    {
        PumpButton(40.0, "small button");

        Dictionary<string, object?> result = await RunEvaluation(new Dictionary<string, string>
        {
            ["type"] = "MinimumTapTargetEvaluation",
            ["targetSize"] = "48.0",
        });

        Assert.Contains("result", result.Keys);
        List<Dictionary<string, string>> violations = Violations(result);
        Assert.NotEmpty(violations);
        Assert.Contains(violations, violation => violation["message"].Contains("expected tap target size"));
        Assert.All(violations, violation => Assert.True(int.TryParse(violation["nodeId"], out _)));
    }

    [NonReleaseFact]
    public async Task ReturnsViolationsForLabeledTapTargetEvaluation()
    {
        PumpButton(48.0, label: null);

        Dictionary<string, object?> result = await RunEvaluation(new Dictionary<string, string>
        {
            ["type"] = "LabeledTapTargetEvaluation",
        });

        Assert.Contains("result", result.Keys);
        List<Dictionary<string, string>> violations = Violations(result);
        Assert.NotEmpty(violations);
        Assert.Contains(
            violations,
            violation => violation["message"].Contains("expected tappable node to have semantic label"));
    }

    [NonReleaseFact]
    public async Task ReturnsViolationsForMinimumTextContrastEvaluation()
    {
        Pump(new TestWidgetsApp(home: new ColoredBox(
            color: new Color(0xFFFFFFFF),
            child: new Center(child: new SizedBox(
                width: 100,
                height: 100,
                child: new Center(child: new Text(
                    "low contrast",
                    style: new TextStyle(Color: new Color(0xFFEEEEEE), FontSize: 14))))))));

        Dictionary<string, object?> result = await RunEvaluation(new Dictionary<string, string>
        {
            ["type"] = "MinimumTextContrastEvaluation",
            ["minNormalTextContrastRatio"] = "4.5",
            ["minLargeTextContrastRatio"] = "3.0",
        });

        Assert.Contains("result", result.Keys);
        List<Dictionary<string, string>> violations = Violations(result);
        Assert.NotEmpty(violations);
        Assert.Contains(violations, violation => violation["message"].Contains("Expected contrast ratio"));
    }

    [NonReleaseFact]
    public async Task ReturnsEmptyListsWhenNoViolations()
    {
        PumpButton(48.0, "valid button");

        Dictionary<string, object?> tapTarget = await RunEvaluation(new Dictionary<string, string>
        {
            ["type"] = "MinimumTapTargetEvaluation",
            ["targetSize"] = "48.0",
        });
        Assert.Empty(Violations(tapTarget));

        Dictionary<string, object?> labeled = await RunEvaluation(new Dictionary<string, string>
        {
            ["type"] = "LabeledTapTargetEvaluation",
        });
        Assert.Empty(Violations(labeled));

        Dictionary<string, object?> contrast = await RunEvaluation(new Dictionary<string, string>
        {
            ["type"] = "MinimumTextContrastEvaluation",
            ["minNormalTextContrastRatio"] = "4.5",
            ["minLargeTextContrastRatio"] = "3.0",
        });
        Assert.Empty(Violations(contrast));
    }

    [NonReleaseFact]
    public async Task HonorsTargetSizeParameter()
    {
        PumpTestWidget();

        Dictionary<string, object?> result48 = await RunEvaluation(new Dictionary<string, string>
        {
            ["type"] = "MinimumTapTargetEvaluation",
            ["targetSize"] = "48.0",
        });
        Assert.Empty(Violations(result48));

        Dictionary<string, object?> result50 = await RunEvaluation(new Dictionary<string, string>
        {
            ["type"] = "MinimumTapTargetEvaluation",
            ["targetSize"] = "50.0",
        });
        List<Dictionary<string, string>> violations = Violations(result50);
        Assert.NotEmpty(violations);
        Assert.Contains("expected tap target size of at least Size(50.0, 50.0)", violations[0]["message"]);
    }

    [NonReleaseFact]
    public async Task ReturnsErrorWhenTypeIsMissing()
    {
        PumpTestWidget();

        Exception error = await Assert.ThrowsAsync<Exception>(() => RunEvaluation([]));

        Assert.Contains("type parameter is required", error.Message);
    }

    [NonReleaseFact]
    public async Task ReturnsErrorWhenTypeIsUnknown()
    {
        PumpTestWidget();

        Exception error = await Assert.ThrowsAsync<Exception>(
            () => RunEvaluation(new Dictionary<string, string> { ["type"] = "UnknownType" }));

        Assert.Contains("unknown type: UnknownType", error.Message);
    }

    [NonReleaseFact]
    public async Task ReturnsErrorWhenMinimumTextContrastEvaluationParamsAreMissing()
    {
        PumpTestWidget();

        Exception error = await Assert.ThrowsAsync<Exception>(
            () => RunEvaluation(new Dictionary<string, string> { ["type"] = "MinimumTextContrastEvaluation" }));

        Assert.Contains("Invalid arguments", error.Message);
    }

    [NonReleaseFact]
    public async Task ReturnsErrorWhenMinimumTextContrastEvaluationParamsAreMalformed()
    {
        PumpTestWidget();

        await Assert.ThrowsAsync<FormatException>(() => RunEvaluation(new Dictionary<string, string>
        {
            ["type"] = "MinimumTextContrastEvaluation",
            ["minNormalTextContrastRatio"] = "foo",
            ["minLargeTextContrastRatio"] = "3.0",
        }));
    }

    [NonReleaseFact]
    public async Task ReturnsErrorWhenMinimumTapTargetEvaluationTargetSizeIsMissing()
    {
        PumpTestWidget();

        Exception error = await Assert.ThrowsAsync<Exception>(
            () => RunEvaluation(new Dictionary<string, string> { ["type"] = "MinimumTapTargetEvaluation" }));

        Assert.Contains("Invalid arguments", error.Message);
    }

    [NonReleaseFact]
    public async Task ReturnsErrorWhenMinimumTapTargetEvaluationTargetSizeIsMalformed()
    {
        PumpTestWidget();

        await Assert.ThrowsAsync<FormatException>(() => RunEvaluation(new Dictionary<string, string>
        {
            ["type"] = "MinimumTapTargetEvaluation",
            ["targetSize"] = "foo",
        }));
    }

    [NonReleaseFact]
    public void WrapsTheFeatureFlagErrorIntoAnExtensionErrorResponse()
    {
        PumpTestWidget();
        Features.IsAccessibilityEvaluationsEnabled = false;
        FlutterExceptionHandler? previous = FlutterError.OnError;
        var reported = new List<FlutterErrorDetails>();
        FlutterError.OnError = reported.Add;
        try
        {
            // The wrapper waits one outer event-loop turn, which the fake loop runs.
            ServiceExtensionResponse response = FakeGestureTimers.InvokeServiceExtension(
                "ext.flutter.accessibilityEvaluations",
                new Dictionary<string, string> { ["type"] = "LabeledTapTargetEvaluation" });

            Assert.True(response.IsError);
            Assert.Equal(ServiceExtensionResponse.ExtensionError, response.ErrorCode);
            Assert.Contains("Accessibility evaluations APIs are not enabled.", response.ErrorDetail);
            Assert.IsType<NotSupportedException>(Assert.Single(reported).Exception);
        }
        finally
        {
            FlutterError.OnError = previous;
        }
    }
}
