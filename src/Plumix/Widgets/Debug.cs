using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/debug.dart

namespace Plumix.Widgets;

/// <summary>
/// Dart's <c>RebuildDirtyWidgetCallback</c>: signature for
/// <see cref="WidgetsDebug.DebugOnRebuildDirtyWidget"/>.
/// </summary>
public delegate void RebuildDirtyWidgetCallback(Element e, bool builtOnce);

/// <summary>
/// The debug-only switches of Flutter's <c>widgets/debug.dart</c> that the widgets framework reads.
/// Every flag defaults to <see langword="false"/> (and <see cref="DebugOnRebuildDirtyWidget"/> to
/// <see langword="null"/>), and setting one has an effect only in debug builds.
/// </summary>
public static class WidgetsDebug
{
    /// <summary>Dart's <c>debugPrintRebuildDirtyWidgets</c>: log every widget as it rebuilds.</summary>
    public static bool DebugPrintRebuildDirtyWidgets { get; set; }

    /// <summary>
    /// Dart's <c>debugOnRebuildDirtyWidget</c>: invoked for every dirty widget that is rebuilt, with
    /// a flag saying whether that element had already been built once before.
    /// </summary>
    public static RebuildDirtyWidgetCallback? DebugOnRebuildDirtyWidget { get; set; }

    /// <summary>Dart's <c>debugPrintBuildScope</c>: log the entry and exit of each build scope.</summary>
    public static bool DebugPrintBuildScope { get; set; }

    /// <summary>
    /// Dart's <c>debugPrintScheduleBuildForStacks</c>: log a stack for every
    /// <c>BuildOwner.scheduleBuildFor</c> call.
    /// </summary>
    public static bool DebugPrintScheduleBuildForStacks { get; set; }

    /// <summary>
    /// Dart's <c>debugPrintGlobalKeyedWidgetLifecycle</c>: log the deactivation, reactivation and
    /// discarding of elements whose widget carries a <see cref="GlobalKey"/>.
    /// </summary>
    public static bool DebugPrintGlobalKeyedWidgetLifecycle { get; set; }

    /// <summary>Dart's <c>debugProfileBuildsEnabled</c>: adds a timeline event for every widget built.</summary>
    public static bool DebugProfileBuildsEnabled { get; set; }

    /// <summary>Dart's <c>debugProfileBuildsEnabledUserWidgets</c>: as above, user-created widgets only.</summary>
    public static bool DebugProfileBuildsEnabledUserWidgets { get; set; }

    /// <summary>Dart's <c>debugEnhanceBuildTimelineArguments</c>: adds widget properties to build events.</summary>
    public static bool DebugEnhanceBuildTimelineArguments { get; set; }

    /// <summary>Dart's <c>debugHighlightDeprecatedWidgets</c>: paints deprecated widgets in a bright colour.</summary>
    public static bool DebugHighlightDeprecatedWidgets { get; set; }

    /// <summary>
    /// Asserts that the given <paramref name="children"/> have no two widgets with the same key, and
    /// returns false so it can sit in a debug check. Dart's <c>debugChildrenHaveDuplicateKeys</c>.
    /// </summary>
    public static bool DebugChildrenHaveDuplicateKeys(
        Widget parent,
        IEnumerable<Widget> children,
        string? message = null)
    {
        if (Constants.KDebugMode && FirstNonUniqueKey(children) is { } nonUniqueKey)
        {
            throw new FlutterError(
                (message ?? "Duplicate keys found.\n"
                    + "If multiple keyed widgets exist as children of another widget, they must have unique keys.")
                + $"\n{parent} has multiple children with key {nonUniqueKey}.");
        }

        return false;
    }

    /// <summary>
    /// Asserts that the given <paramref name="items"/> have no two widgets with the same key. Dart's
    /// <c>debugItemsHaveDuplicateKeys</c>.
    /// </summary>
    public static bool DebugItemsHaveDuplicateKeys(IEnumerable<Widget> items)
    {
        if (Constants.KDebugMode && FirstNonUniqueKey(items) is { } nonUniqueKey)
        {
            throw new FlutterError($"Duplicate key found: {nonUniqueKey}.");
        }

        return false;
    }

    /// <summary>Dart's private <c>_firstNonUniqueKey</c>.</summary>
    private static Key? FirstNonUniqueKey(IEnumerable<Widget> widgets)
    {
        var keySet = new HashSet<Key>();
        foreach (Widget widget in widgets)
        {
            if (widget.Key == null)
            {
                continue;
            }

            if (!keySet.Add(widget.Key))
            {
                return widget.Key;
            }
        }

        return null;
    }
    /// <summary>
    /// Dart's <c>debugWidgetBuilderValue</c>: asserts that a build function did not return
    /// <see langword="null"/> and did not return the widget it was building for.
    /// </summary>
    public static void DebugWidgetBuilderValue(Widget widget, Widget? built)
    {
        if (!Constants.KDebugMode)
        {
            return;
        }

        if (built is null)
        {
            throw new FlutterError(
            [
                new ErrorSummary("A build function returned null."),
                new DiagnosticsProperty<Widget>(
                    "The offending widget is",
                    widget,
                    style: DiagnosticsTreeStyle.ErrorProperty),
                new ErrorDescription("Build functions must never return null."),
                new ErrorHint(
                    "To return an empty space that causes the building widget to fill available room, "
                    + "return \"Container()\". To return an empty space that takes as little room as "
                    + "possible, return \"Container(width: 0.0, height: 0.0)\"."),
            ]);
        }

        if (ReferenceEquals(widget, built))
        {
            throw new FlutterError(
            [
                new ErrorSummary("A build function returned context.widget."),
                new DiagnosticsProperty<Widget>(
                    "The offending widget is",
                    widget,
                    style: DiagnosticsTreeStyle.ErrorProperty),
                new ErrorDescription(
                    "Build functions must never return their BuildContext parameter's widget or a child "
                    + "that contains \"context.widget\". Doing so introduces a loop in the widget tree that "
                    + "can cause the app to crash."),
            ]);
        }
    }

    /// <summary>
    /// Dart's <c>debugAssertAllWidgetVarsUnset</c>: throws when any of the flags above is still set,
    /// so a test cannot leak a debug switch into the next one.
    /// </summary>
    public static bool DebugAssertAllWidgetVarsUnset(string reason)
    {
        if (!Constants.KDebugMode)
        {
            return true;
        }

        if (DebugPrintRebuildDirtyWidgets
            || DebugPrintBuildScope
            || DebugPrintScheduleBuildForStacks
            || DebugPrintGlobalKeyedWidgetLifecycle
            || DebugProfileBuildsEnabled
            || DebugProfileBuildsEnabledUserWidgets
            || DebugEnhanceBuildTimelineArguments
            || DebugHighlightDeprecatedWidgets
            || DebugOnRebuildDirtyWidget is not null)
        {
            throw new FlutterError(reason);
        }

        return true;
    }
}
