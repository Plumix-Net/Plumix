using System.Text;
using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/binding.dart

namespace Plumix.Widgets;

/// <summary>
/// The service-extension, first-frame and widget-inspector half of Flutter's <c>WidgetsBinding</c>.
/// </summary>
public partial class WidgetsBinding
{
    private ValueNotifier<bool>? _debugShowWidgetInspectorOverrideNotifierObject;
    private ValueNotifier<bool>? _debugWidgetInspectorSelectionOnTapEnabledNotifierObject;
    private bool _needToReportFirstFrame = true;
    private TaskCompletionSource _firstFrameCompleter = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Element? _debugRootElementOverrideForTests;
    private BuildOwner? _debugBuildOwnerOverrideForTests;

    /// <summary>
    /// Whether the inspector is in select mode. In select mode, pointer interactions trigger widget
    /// selection instead of normal interactions; otherwise the previously selected widget is
    /// highlighted but the application can be interacted with normally.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>WidgetsBinding.debugShowWidgetInspectorOverride</c>; the value of
    /// <see cref="DebugShowWidgetInspectorOverrideNotifier"/>.
    /// </remarks>
    public bool DebugShowWidgetInspectorOverride
    {
        get => DebugShowWidgetInspectorOverrideNotifier.Value;
        set => DebugShowWidgetInspectorOverrideNotifier.Value = value;
    }

    /// <summary>Notifier wrapper for <see cref="DebugShowWidgetInspectorOverride"/>.</summary>
    /// <remarks>Flutter's <c>WidgetsBinding.debugShowWidgetInspectorOverrideNotifier</c>.</remarks>
    public ValueNotifier<bool> DebugShowWidgetInspectorOverrideNotifier =>
        _debugShowWidgetInspectorOverrideNotifierObject ??= new ValueNotifier<bool>(false);

    /// <summary>
    /// The notifier for whether or not taps on the device will trigger widget selection when the
    /// widget inspector is enabled.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>WidgetsBinding.debugWidgetInspectorSelectionOnTapEnabled</c>. When false, the
    /// application can be interacted with as normal while the inspector's selection stays visible.
    /// </remarks>
    public ValueNotifier<bool> DebugWidgetInspectorSelectionOnTapEnabled =>
        _debugWidgetInspectorSelectionOnTapEnabledNotifierObject ??= new ValueNotifier<bool>(true);

    /// <summary>
    /// If true, <see cref="WidgetsApp"/> will not insert a <see cref="WidgetInspector"/> into the
    /// widget tree, even when the inspector is enabled.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>WidgetsBinding.debugExcludeRootWidgetInspector</c>, for applications that insert
    /// their own <see cref="WidgetInspector"/> lower in the tree.
    /// </remarks>
    public bool DebugExcludeRootWidgetInspector { get; set; }

    /// <summary>Whether the Flutter engine has rasterized the first frame.</summary>
    /// <remarks>
    /// Flutter's <c>WidgetsBinding.firstFrameRasterized</c>: set once the platform reported the timings
    /// of the first frame that <see cref="DrawFrame"/> sent (a host reports them after it rasterized
    /// the frame); usually followed by <see cref="WaitUntilFirstFrameRasterized"/>.
    /// </remarks>
    public bool FirstFrameRasterized => _firstFrameCompleter.Task.IsCompleted;

    /// <summary>A task that completes when the Flutter engine has rasterized the first frame.</summary>
    /// <remarks>Flutter's <c>WidgetsBinding.waitUntilFirstFrameRasterized</c>.</remarks>
    public Task WaitUntilFirstFrameRasterized => _firstFrameCompleter.Task;

    /// <summary>Whether the first frame has finished building.</summary>
    /// <remarks>
    /// Flutter's <c>WidgetsBinding.debugDidSendFirstFrameEvent</c>; also available as the
    /// <c>ext.flutter.didSendFirstFrameEvent</c> service extension.
    /// </remarks>
    public bool DebugDidSendFirstFrameEvent => !_needToReportFirstFrame;

    /// <summary>The element <see cref="RootElement"/> reports while a widget test drives its own tree.</summary>
    /// <remarks>
    /// flutter_test's binding is the widgets binding, so <c>WidgetsBinding.instance.rootElement</c> is
    /// the tester's root; Plumix's tester owns a separate build owner and root and installs them here.
    /// </remarks>
    internal void DebugOverrideRootForTests(Element? rootElement, BuildOwner? buildOwner)
    {
        _debugRootElementOverrideForTests = rootElement;
        _debugBuildOwnerOverrideForTests = buildOwner;
    }

    /// <summary>Forgets that the first frame was reported, as a fresh binding starts.</summary>
    /// <remarks>Dart's binding tests get a fresh binding per test file; the process-wide one needs this.</remarks>
    internal void ResetFirstFrameForTests()
    {
        _needToReportFirstFrame = true;
        _firstFrameCompleter = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>Resets the widget inspector notifiers.</summary>
    /// <remarks>
    /// Flutter's <c>WidgetsBinding.resetInternalState</c> override, which is test-only there too:
    /// disposes both inspector notifiers so the next read creates fresh ones.
    /// </remarks>
    internal void ResetInternalState()
    {
        SchedulerBinding.Instance.ResetInternalState();
        _debugShowWidgetInspectorOverrideNotifierObject?.Dispose();
        _debugShowWidgetInspectorOverrideNotifierObject = null;
        _debugWidgetInspectorSelectionOnTapEnabledNotifierObject?.Dispose();
        _debugWidgetInspectorSelectionOnTapEnabledNotifierObject = null;
    }

    /// <summary>Registers the widgets library's service extensions.</summary>
    /// <remarks>
    /// Flutter's <c>WidgetsBinding.initServiceExtensions</c>, which <c>BindingBase</c> runs right after
    /// <c>initInstances</c>.
    /// </remarks>
    private void InitServiceExtensions()
    {
        if (!Constants.KReleaseMode)
        {
            BindingBase.RegisterServiceExtension(
                WidgetsServiceExtensions.DebugDumpApp.DartName(),
                static _ => Task.FromResult(new Dictionary<string, object?> { ["data"] = DebugDumpAppString() }));

            BindingBase.RegisterServiceExtension(
                WidgetsServiceExtensions.DebugDumpFocusTree.DartName(),
                _ => Task.FromResult(new Dictionary<string, object?> { ["data"] = FocusManager.ToStringDeep() }));

            if (!Constants.KIsWeb)
            {
                BindingBase.RegisterBoolServiceExtension(
                    WidgetsServiceExtensions.ShowPerformanceOverlay.DartName(),
                    static () => Task.FromResult(WidgetsApp.ShowPerformanceOverlayOverride),
                    value =>
                    {
                        if (WidgetsApp.ShowPerformanceOverlayOverride == value)
                        {
                            return Task.CompletedTask;
                        }

                        WidgetsApp.ShowPerformanceOverlayOverride = value;
                        return ForceRebuild();
                    });
            }

            BindingBase.RegisterServiceExtension(
                WidgetsServiceExtensions.DidSendFirstFrameEvent.DartName(),
                _ => Task.FromResult(new Dictionary<string, object?>
                {
                    // This is defined to return a STRING, not a boolean.
                    // Devtools, the Intellij plugin, and the flutter tool all depend
                    // on it returning a string and not a boolean.
                    ["enabled"] = _needToReportFirstFrame ? "false" : "true",
                }));

            BindingBase.RegisterServiceExtension(
                WidgetsServiceExtensions.DidSendFirstFrameRasterizedEvent.DartName(),
                _ => Task.FromResult(new Dictionary<string, object?>
                {
                    // This is defined to return a STRING, not a boolean.
                    // Devtools, the Intellij plugin, and the flutter tool all depend
                    // on it returning a string and not a boolean.
                    ["enabled"] = FirstFrameRasterized ? "true" : "false",
                }));

            // Expose the ability to send Widget rebuilds as [Timeline] events.
            BindingBase.RegisterBoolServiceExtension(
                WidgetsServiceExtensions.ProfileWidgetBuilds.DartName(),
                static () => Task.FromResult(WidgetsDebug.DebugProfileBuildsEnabled),
                static value =>
                {
                    WidgetsDebug.DebugProfileBuildsEnabled = value;
                    return Task.CompletedTask;
                });
            BindingBase.RegisterBoolServiceExtension(
                WidgetsServiceExtensions.ProfileUserWidgetBuilds.DartName(),
                static () => Task.FromResult(WidgetsDebug.DebugProfileBuildsEnabledUserWidgets),
                static value =>
                {
                    WidgetsDebug.DebugProfileBuildsEnabledUserWidgets = value;
                    return Task.CompletedTask;
                });

            BindingBase.RegisterServiceExtension(
                WidgetsServiceExtensions.AccessibilityEvaluations.DartName(),
                async parameters =>
                {
                    if (!parameters.TryGetValue("type", out string? type))
                    {
                        throw new Exception("type parameter is required");
                    }

                    switch (type)
                    {
                        case "MinimumTextContrastEvaluation":
                            if (parameters.TryGetValue("minNormalTextContrastRatio", out string? minNormal)
                                && parameters.TryGetValue("minLargeTextContrastRatio", out string? minLarge))
                            {
                                EvaluationResult result = await new MinimumTextContrastEvaluation(
                                        minNormalTextContrastRatio: ParseDartDouble(minNormal),
                                        minLargeTextContrastRatio: ParseDartDouble(minLarge))
                                    .Evaluate(this);
                                return FormatEvaluationResult(result.Violations);
                            }

                            throw new Exception("Invalid arguments");
                        case "MinimumTapTargetEvaluation":
                            if (parameters.TryGetValue("targetSize", out string? targetSize))
                            {
                                double dimension = ParseDartDouble(targetSize);
                                EvaluationResult result =
                                    await new MinimumTapTargetEvaluation(size: new Avalonia.Size(dimension, dimension))
                                        .Evaluate(this);
                                return FormatEvaluationResult(result.Violations);
                            }

                            throw new Exception("Invalid arguments");
                        case "LabeledTapTargetEvaluation":
                        {
                            EvaluationResult result = await new LabeledTapTargetEvaluation().Evaluate(this);
                            return FormatEvaluationResult(result.Violations);
                        }

                        default:
                            throw new Exception($"unknown type: {type}");
                    }
                });
        }

        if (Constants.KDebugMode)
        {
            BindingBase.RegisterBoolServiceExtension(
                WidgetsServiceExtensions.DebugAllowBanner.DartName(),
                static () => Task.FromResult(WidgetsApp.DebugAllowBannerOverride),
                value =>
                {
                    if (WidgetsApp.DebugAllowBannerOverride == value)
                    {
                        return Task.CompletedTask;
                    }

                    WidgetsApp.DebugAllowBannerOverride = value;
                    return ForceRebuild();
                });

            WidgetInspectorService.Instance.InitServiceExtensions(BindingBase.RegisterServiceExtension);
        }
    }

    /// <remarks>Flutter's private <c>WidgetsBinding._formatEvaluationResult</c>.</remarks>
    private static Dictionary<string, object?> FormatEvaluationResult(List<Violation> violations) => new()
    {
        ["result"] = violations
            .Select(violation => new Dictionary<string, string>
            {
                ["nodeId"] = violation.Node.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["message"] = violation.Reason,
            })
            .ToList(),
    };

    /// <remarks>Dart's <c>double.parse</c>, which throws a <see cref="FormatException"/>.</remarks>
    private static double ParseDartDouble(string source) =>
        double.Parse(
            source,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture);

    /// <remarks>Flutter's private <c>WidgetsBinding._forceRebuild</c>.</remarks>
    private Task ForceRebuild()
    {
        if (RootElement is { } rootElement)
        {
            BuildOwner.Reassemble(rootElement);
            return SchedulerBinding.Instance.EndOfFrame;
        }

        return Task.CompletedTask;
    }

    /// <summary>The text <see cref="DebugDumpApp"/> prints.</summary>
    /// <remarks>Flutter's private <c>_debugDumpAppString</c>.</remarks>
    internal static string DebugDumpAppString()
    {
        string mode = Constants.KDebugMode
            ? "DEBUG MODE"
            : Constants.KReleaseMode
                ? "RELEASE MODE"
                : "PROFILE MODE";
        var buffer = new StringBuilder();
        buffer.Append(Instance.GetType().Name).Append(" - ").Append(mode).Append('\n');
        if (Instance.RootElement is { } rootElement)
        {
            buffer.Append(rootElement.ToStringDeep()).Append('\n');
        }
        else
        {
            buffer.Append("<no tree currently mounted>").Append('\n');
        }

        return buffer.ToString();
    }

    /// <summary>Print a string representation of the currently running app.</summary>
    /// <remarks>Flutter's <c>debugDumpApp</c>.</remarks>
    public static void DebugDumpApp()
    {
        Print.DebugPrint(DebugDumpAppString());
    }
}
