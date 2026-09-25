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
    /// <summary>Dart's debugCheckHasDirectionality.</summary>
    public static bool DebugCheckHasDirectionality(
        BuildContext context,
        string? why = null,
        string? hint = null,
        string? alternative = null)
    {
        if (!Constants.KDebugMode
            || context.Widget is Directionality
            || context.GetElementForInheritedWidgetOfExactType<Directionality>() is not null)
        {
            return true;
        }

        var information = new List<DiagnosticsNode>
        {
            new ErrorSummary("No Directionality widget found."),
            new ErrorDescription(
                $"{Diagnostics.DescribeType(context.Widget.GetType())} widgets require a Directionality "
                + $"widget ancestor{(why is null ? string.Empty : $" {why}")}.\n"),
        };
        if (hint is not null)
        {
            information.Add(new ErrorHint(hint));
        }

        information.Add(context.DescribeWidget(
            "The specific widget that could not find a Directionality ancestor was"));
        information.Add(context.DescribeOwnershipChain("The ownership chain for the affected widget is"));
        information.Add(new ErrorHint(
            "Typically, the Directionality widget is introduced by the MaterialApp "
            + "or WidgetsApp widget at the top of your application widget tree. It "
            + "determines the ambient reading direction and is used, for example, to "
            + "determine how to lay out text, how to interpret \"start\" and \"end\" "
            + "values, and to resolve EdgeInsetsDirectional, "
            + "AlignmentDirectional, and other *Directional objects."));
        if (alternative is not null)
        {
            information.Add(new ErrorHint(alternative));
        }

        throw new FlutterError(information);
    }

    /// <summary>Dart's <c>debugCheckHasOverlay</c>: throws a <see cref="FlutterError"/> in debug
    /// builds when <paramref name="context"/> has no <see cref="Overlay"/> ancestor within the
    /// closest <see cref="LookupBoundary"/>.</summary>
    public static bool DebugCheckHasOverlay(BuildContext context)
    {
        if (!Constants.KDebugMode || LookupBoundary.FindAncestorWidgetOfExactType<Overlay>(context) is not null)
        {
            return true;
        }

        bool hiddenByBoundary = LookupBoundary.DebugIsHidingAncestorWidgetOfExactType<Overlay>(context);
        var information = new List<DiagnosticsNode>
        {
            new ErrorSummary(
                $"No Overlay widget found{(hiddenByBoundary ? " within the closest LookupBoundary" : string.Empty)}."),
        };
        if (hiddenByBoundary)
        {
            information.Add(new ErrorDescription(
                "There is an ancestor Overlay widget, but it is hidden by a LookupBoundary."));
        }

        information.Add(new ErrorDescription(
            $"{Diagnostics.DescribeType(context.Widget.GetType())} widgets require an Overlay "
            + "widget ancestor within the closest LookupBoundary.\n"
            + "An overlay lets widgets float on top of other widget children."));
        information.Add(new ErrorHint(
            "To introduce an Overlay widget, you can either directly "
            + "include one, or use a widget that contains an Overlay itself, "
            + "such as a Navigator, WidgetApp, MaterialApp, or CupertinoApp."));
        information.AddRange(context.DescribeMissingAncestor(typeof(Overlay)));
        throw new FlutterError(information);
    }

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

    /// <summary>
    /// Whether <paramref name="widget"/> was created by the application rather than by the framework
    /// or a package.
    /// </summary>
    /// <remarks>
    /// Dart's <c>debugIsWidgetLocalCreation</c> (<c>widget_inspector.dart</c>), which reads the creation
    /// location the <c>--track-widget-creation</c> kernel transformer records. Plumix has no creation
    /// location tracking, so this answers <see langword="false"/> — what Dart answers when that
    /// transformer is off (see <c>docs/ai/DIVERGENCES.md</c>).
    /// </remarks>
    public static bool DebugIsWidgetLocalCreation(Widget widget)
    {
        ArgumentNullException.ThrowIfNull(widget);
        return false;
    }

    /// <remarks>
    /// Dart's private <c>_isProfileBuildsEnabledFor</c> (<c>framework.dart</c>): whether building
    /// <paramref name="widget"/> is reported to the timeline.
    /// </remarks>
    internal static bool IsProfileBuildsEnabledFor(Widget widget)
    {
        return DebugProfileBuildsEnabled
            || (DebugProfileBuildsEnabledUserWidgets && DebugIsWidgetLocalCreation(widget));
    }

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
