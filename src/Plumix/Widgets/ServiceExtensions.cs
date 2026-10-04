// Dart parity source: flutter/packages/flutter/lib/src/widgets/service_extensions.dart

namespace Plumix.Widgets;

/// <summary>Service extension constants for the widgets library.</summary>
/// <remarks>
/// Flutter's <c>WidgetsServiceExtensions</c>. Each extension is registered under
/// <c>ext.flutter.&lt;name&gt;</c>, where the name is the Dart enum value's name
/// (<see cref="ServiceExtensionNames.DartName(WidgetsServiceExtensions)"/>).
/// </remarks>
public enum WidgetsServiceExtensions
{
    /// <summary>Dumps the widget tree; <c>debugDumpApp</c>.</summary>
    DebugDumpApp,

    /// <summary>Dumps the focus tree; <c>debugDumpFocusTree</c>.</summary>
    DebugDumpFocusTree,

    /// <summary>Overlays a performance chart; <c>showPerformanceOverlay</c>.</summary>
    ShowPerformanceOverlay,

    /// <summary>Whether the first frame was built; <c>didSendFirstFrameEvent</c>.</summary>
    DidSendFirstFrameEvent,

    /// <summary>Whether the first frame was rasterized; <c>didSendFirstFrameRasterizedEvent</c>.</summary>
    DidSendFirstFrameRasterizedEvent,

    /// <summary>Hot-reloads changed widget build methods only; <c>fastReassemble</c>.</summary>
    FastReassemble,

    /// <summary>Records every widget build to the timeline; <c>profileWidgetBuilds</c>.</summary>
    ProfileWidgetBuilds,

    /// <summary>Records user widget builds to the timeline; <c>profileUserWidgetBuilds</c>.</summary>
    ProfileUserWidgetBuilds,

    /// <summary>Shows or hides the debug banner; <c>debugAllowBanner</c>.</summary>
    DebugAllowBanner,

    /// <summary>Runs accessibility evaluations; <c>accessibilityEvaluations</c>.</summary>
    AccessibilityEvaluations,
}

/// <summary>Service extension constants for the widget inspector.</summary>
/// <remarks>
/// Flutter's <c>WidgetInspectorServiceExtensions</c>. Each extension is registered under
/// <c>ext.flutter.inspector.&lt;name&gt;</c>, where the name is the Dart enum value's name.
/// </remarks>
public enum WidgetInspectorServiceExtensions
{
    /// <summary><c>structuredErrors</c>.</summary>
    StructuredErrors,

    /// <summary><c>show</c>.</summary>
    Show,

    /// <summary><c>trackRebuildDirtyWidgets</c>.</summary>
    TrackRebuildDirtyWidgets,

    /// <summary><c>widgetLocationIdMap</c>.</summary>
    WidgetLocationIdMap,

    /// <summary><c>trackRepaintWidgets</c>.</summary>
    TrackRepaintWidgets,

    /// <summary><c>disposeAllGroups</c>.</summary>
    DisposeAllGroups,

    /// <summary><c>disposeGroup</c>.</summary>
    DisposeGroup,

    /// <summary><c>isWidgetTreeReady</c>.</summary>
    IsWidgetTreeReady,

    /// <summary><c>disposeId</c>.</summary>
    DisposeId,

    /// <summary><c>setPubRootDirectories</c>.</summary>
    [Obsolete("Use AddPubRootDirectories instead. This feature was deprecated after v3.18.0-2.0.pre.")]
    SetPubRootDirectories,

    /// <summary><c>addPubRootDirectories</c>.</summary>
    AddPubRootDirectories,

    /// <summary><c>removePubRootDirectories</c>.</summary>
    RemovePubRootDirectories,

    /// <summary><c>getPubRootDirectories</c>.</summary>
    GetPubRootDirectories,

    /// <summary><c>setSelectionById</c>.</summary>
    SetSelectionById,

    /// <summary><c>getParentChain</c>.</summary>
    GetParentChain,

    /// <summary><c>getProperties</c>.</summary>
    GetProperties,

    /// <summary><c>getChildren</c>.</summary>
    GetChildren,

    /// <summary><c>getChildrenSummaryTree</c>.</summary>
    GetChildrenSummaryTree,

    /// <summary><c>getChildrenDetailsSubtree</c>.</summary>
    GetChildrenDetailsSubtree,

    /// <summary><c>getRootWidget</c>.</summary>
    GetRootWidget,

    /// <summary><c>getRootWidgetTree</c>.</summary>
    GetRootWidgetTree,

    /// <summary><c>getRootWidgetSummaryTree</c>.</summary>
    GetRootWidgetSummaryTree,

    /// <summary><c>getRootWidgetSummaryTreeWithPreviews</c>.</summary>
    GetRootWidgetSummaryTreeWithPreviews,

    /// <summary><c>getDetailsSubtree</c>.</summary>
    GetDetailsSubtree,

    /// <summary><c>getSelectedWidget</c>.</summary>
    GetSelectedWidget,

    /// <summary><c>getSelectedSummaryWidget</c>.</summary>
    GetSelectedSummaryWidget,

    /// <summary><c>isWidgetCreationTracked</c>.</summary>
    IsWidgetCreationTracked,

    /// <summary><c>screenshot</c>.</summary>
    Screenshot,

    /// <summary><c>getLayoutExplorerNode</c>.</summary>
    GetLayoutExplorerNode,

    /// <summary><c>setFlexFit</c>.</summary>
    SetFlexFit,

    /// <summary><c>setFlexFactor</c>.</summary>
    SetFlexFactor,

    /// <summary><c>setFlexProperties</c>.</summary>
    SetFlexProperties,
}

/// <summary>The Dart <c>.name</c> of the widgets service-extension enums.</summary>
public static class ServiceExtensionNames
{
    /// <summary>The extension's Dart name, e.g. <c>debugDumpApp</c>.</summary>
    public static string DartName(this WidgetsServiceExtensions extension) => LowerFirst(extension.ToString());

    /// <summary>The extension's Dart name, e.g. <c>getRootWidgetTree</c>.</summary>
    public static string DartName(this WidgetInspectorServiceExtensions extension) =>
        LowerFirst(extension.ToString());

    private static string LowerFirst(string name) => char.ToLowerInvariant(name[0]) + name[1..];
}
