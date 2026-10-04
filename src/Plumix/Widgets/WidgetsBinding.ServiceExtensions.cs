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

    /// <summary>Resets the widget inspector notifiers.</summary>
    /// <remarks>
    /// Flutter's <c>WidgetsBinding.resetInternalState</c> override, which is test-only there too:
    /// disposes both inspector notifiers so the next read creates fresh ones.
    /// </remarks>
    internal void ResetInternalState()
    {
        Scheduler.ResetInternalState();
        _debugShowWidgetInspectorOverrideNotifierObject?.Dispose();
        _debugShowWidgetInspectorOverrideNotifierObject = null;
        _debugWidgetInspectorSelectionOnTapEnabledNotifierObject?.Dispose();
        _debugWidgetInspectorSelectionOnTapEnabledNotifierObject = null;
    }

    /// <summary>Registers the widgets library's service extensions.</summary>
    /// <remarks>
    /// Flutter's <c>WidgetsBinding.initServiceExtensions</c>, which <c>BindingBase</c> runs right after
    /// <c>initInstances</c>. <c>debugDumpFocusTree</c> (the focus manager has no diagnostics tree),
    /// <c>didSendFirstFrameRasterizedEvent</c> (no host reports frame timings) and
    /// <c>accessibilityEvaluations</c> (<c>_accessibility_evaluations.dart</c> is not ported) are not
    /// registered; see <c>docs/ai/BACKLOG.md</c>.
    /// </remarks>
    private void InitServiceExtensions()
    {
        if (!Constants.KReleaseMode)
        {
            BindingBase.RegisterServiceExtension(
                WidgetsServiceExtensions.DebugDumpApp.DartName(),
                static _ => Task.FromResult(new Dictionary<string, object?> { ["data"] = DebugDumpAppString() }));

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

    /// <remarks>Flutter's private <c>WidgetsBinding._forceRebuild</c>.</remarks>
    private Task ForceRebuild()
    {
        if (RootElement is { } rootElement)
        {
            BuildOwner.Reassemble(rootElement);
            return Scheduler.EndOfFrame;
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

    /// <summary>Records that a frame was built, reporting the first one.</summary>
    /// <remarks>
    /// The first-frame bookkeeping of Flutter's <c>WidgetsBinding.drawFrame</c>: the
    /// <c>Widgets built first useful frame</c> timeline instant and <c>_needToReportFirstFrame</c>. The
    /// rasterized half (<c>firstFrameRasterized</c>, the <c>Flutter.FirstFrame</c> event) waits on a
    /// timings callback no host reports; see <c>docs/ai/BACKLOG.md</c>.
    /// </remarks>
    private void ReportFirstFrameBuilt()
    {
        bool sendFramesToEngine = RendererBinding.Instance.SendFramesToEngine;
        if (!Constants.KReleaseMode)
        {
            if (_needToReportFirstFrame && sendFramesToEngine)
            {
                FlutterTimeline.InstantSync("Widgets built first useful frame");
            }
        }

        // A frame that is deferred is not the first frame sent to the engine that should be reported.
        _needToReportFirstFrame = !sendFramesToEngine && _needToReportFirstFrame;
    }
}
