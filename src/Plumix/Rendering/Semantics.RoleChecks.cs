using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Plumix.Foundation;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/semantics/semantics.dart (_DebugSemanticsRoleChecks)

namespace Plumix.Rendering;

internal static class DebugSemanticsRoleChecks
{
    [Conditional("DEBUG")]
    internal static void CheckSemanticsData(SemanticsNode node)
    {
        FlutterError? error = node.Role switch
        {
            SemanticsRole.Tab => SemanticsTab(node),
            SemanticsRole.TabBar => SemanticsTabBar(node),
            SemanticsRole.Table => SemanticsTable(node),
            SemanticsRole.Cell => SemanticsCell(node),
            SemanticsRole.Row => SemanticsRow(node),
            SemanticsRole.ColumnHeader => SemanticsColumnHeader(node),
            SemanticsRole.RadioGroup => SemanticsRadioGroup(node),
            SemanticsRole.Menu => SemanticsMenu(node),
            SemanticsRole.MenuBar => SemanticsMenuBar(node),
            SemanticsRole.MenuItem => SemanticsMenuItem(node),
            SemanticsRole.MenuItemCheckbox => SemanticsMenuItemCheckbox(node),
            SemanticsRole.MenuItemRadio => SemanticsMenuItemRadio(node),
            SemanticsRole.Alert or SemanticsRole.Status => NoLiveRegion(node),
            SemanticsRole.ListItem => SemanticsListItem(node),
            SemanticsRole.Complementary => SemanticsComplementary(node),
            SemanticsRole.ContentInfo => SemanticsContentInfo(node),
            SemanticsRole.Main => SemanticsMain(node),
            SemanticsRole.Navigation => SemanticsNavigation(node),
            SemanticsRole.Region => SemanticsRegion(node),
            SemanticsRole.ProgressBar => SemanticsProgressBar(node),
            SemanticsRole.DragHandle or SemanticsRole.SpinButton or SemanticsRole.ComboBox
                or SemanticsRole.Tooltip or SemanticsRole.HotKey => Unimplemented(node),
            SemanticsRole.AlertDialog or SemanticsRole.Dialog or SemanticsRole.None or SemanticsRole.TabPanel
                or SemanticsRole.List or SemanticsRole.Form or SemanticsRole.LoadingSpinner => null,
            _ => throw new ArgumentOutOfRangeException(nameof(node)),
        };
        error ??= SemanticsGeneral(node);
        if (error is not null)
        {
            throw error;
        }
    }

    private static string RoleName(SemanticsRole role) => $"SemanticsRole.{Diagnostics.EnumName(role)}";

    private static FlutterError Unimplemented(SemanticsNode node) =>
        new($"Missing checks for role {RoleName(node.GetSemanticsData().Role)}");

    private static double? TryParse(string value)
    {
        // Dart accepts decimal/exponent literals and case-sensitive NaN/Infinity, never thousands separators.
        int start = 0;
        int end = value.Length;
        while (start < end && (char.IsWhiteSpace(value[start]) || value[start] == '\uFEFF'))
        {
            start++;
        }

        while (end > start && (char.IsWhiteSpace(value[end - 1]) || value[end - 1] == '\uFEFF'))
        {
            end--;
        }

        string number = value[start..end];
        if (!Regex.IsMatch(number, @"^[+-]?(?:NaN|Infinity|(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)(?:[eE][+-]?[0-9]+)?)$"))
        {
            return null;
        }

        if (number.TrimStart('+', '-') == "NaN")
        {
            return double.NaN;
        }

        return double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out double result)
            ? result
            : null;
    }

    private static FlutterError? SemanticsProgressBar(SemanticsNode node)
    {
        SemanticsData data = node.GetSemanticsData();
        if (data.Value.Length == 0 || string.IsNullOrEmpty(data.MinValue) || string.IsNullOrEmpty(data.MaxValue))
        {
            return new FlutterError("A progress bar must have a value, a minValue, a maxValue.");
        }

        double? minVal = TryParse(data.MinValue);
        double? maxVal = TryParse(data.MaxValue);
        double? currentValue = TryParse(data.Value);
        double? percentValue = data.Value.EndsWith('%') ? TryParse(data.Value[..^1]) : null;
        if (minVal is null || maxVal is null || (currentValue is null && percentValue is null))
        {
            return new FlutterError(
                "Progress bar value, minValue, and maxValue must be valid numbers. "
                + $"value: \"{data.Value}\", minValue: \"{data.MinValue}\", maxValue: \"{data.MaxValue}\"");
        }

        if (minVal >= maxVal)
        {
            return new FlutterError(
                $"Progress bar minValue ({data.MinValue}) must be less than maxValue ({data.MaxValue})");
        }

        if (currentValue is not null)
        {
            if (currentValue < minVal || currentValue > maxVal)
            {
                return new FlutterError(
                    $"Progress bar value ({data.Value}) must be between minValue ({data.MinValue}) "
                    + $"and maxValue ({data.MaxValue})");
            }

            return null;
        }

        return percentValue < 0 || percentValue > 100
            ? new FlutterError($"Progress bar percentage value ({data.Value}) must be between 0% and 100%")
            : null;
    }

    private static FlutterError? SemanticsTab(SemanticsNode node)
    {
        SemanticsData data = node.GetSemanticsData();
        if (!data.Flags.HasFlag(SemanticsFlags.HasSelectedState))
        {
            return new FlutterError("A tab needs selected states");
        }

        if (node.AreUserActionsBlocked)
        {
            return null;
        }

        return !(data.Flags.HasFlag(SemanticsFlags.HasEnabledState) && !data.Flags.HasFlag(SemanticsFlags.IsEnabled))
               && !data.HasAction(SemanticsActions.Tap)
            ? new FlutterError("A tab must have a tap action")
            : null;
    }

    private static FlutterError? SemanticsTabBar(SemanticsNode node)
    {
        if (node.Children.Count < 1)
        {
            return new FlutterError("a TabBar cannot be empty");
        }

        FlutterError? error = null;
        node.VisitChildren(child =>
        {
            if (child.GetSemanticsData().Role != SemanticsRole.Tab)
            {
                error = new FlutterError("Children of TabBar must have the tab role");
            }

            return error is null;
        });
        return error;
    }

    private static FlutterError? SemanticsTable(SemanticsNode node)
    {
        FlutterError? error = null;
        node.VisitChildren(child =>
        {
            if (child.GetSemanticsData().Role != SemanticsRole.Row)
            {
                error = new FlutterError("Children of Table must have the row role");
            }

            return error is null;
        });
        return error;
    }

    private static FlutterError? SemanticsRow(SemanticsNode node)
    {
        if (node.Parent?.Role != SemanticsRole.Table)
        {
            return new FlutterError("A row must be a child of a table");
        }

        FlutterError? error = null;
        node.VisitChildren(child =>
        {
            if (child.GetSemanticsData().Role is not SemanticsRole.Cell and not SemanticsRole.ColumnHeader)
            {
                error = new FlutterError("Children of Row must have the cell or columnHeader role");
            }

            return error is null;
        });
        return error;
    }

    private static FlutterError? SemanticsCell(SemanticsNode node) =>
        node.Parent?.Role is not SemanticsRole.Row and not SemanticsRole.Cell
            ? new FlutterError("A cell must be a child of a row or another cell")
            : null;

    private static FlutterError? SemanticsColumnHeader(SemanticsNode node) =>
        node.Parent?.Role is not SemanticsRole.Row and not SemanticsRole.Cell
            ? new FlutterError("A columnHeader must be a child or another cell")
            : null;

    private static FlutterError? SemanticsRadioGroup(SemanticsNode node)
    {
        FlutterError? error = null;
        bool hasCheckedChild = false;
        bool ValidateRadioGroupChildren(SemanticsNode child)
        {
            SemanticsData data = child.GetSemanticsData();
            if (data.Role == SemanticsRole.RadioGroup)
            {
                return error is null;
            }

            if (!data.Flags.HasFlag(SemanticsFlags.IsInMutuallyExclusiveGroup))
            {
                child.VisitChildren(ValidateRadioGroupChildren);
                return error is null;
            }

            if (data.Flags.HasFlag(SemanticsFlags.HasCheckedState) && data.Flags.HasFlag(SemanticsFlags.IsChecked))
            {
                if (hasCheckedChild)
                {
                    error = new FlutterError("Radio groups must not have multiple checked children");
                    return false;
                }

                hasCheckedChild = true;
            }

            Debug.Assert(error is null);
            return true;
        }

        node.VisitChildren(ValidateRadioGroupChildren);
        return error;
    }

    private static FlutterError? SemanticsMenu(SemanticsNode node) =>
        node.Children.Count < 1 ? new FlutterError("a menu cannot be empty") : null;

    private static FlutterError? SemanticsMenuBar(SemanticsNode node) =>
        node.Children.Count < 1 ? new FlutterError("a menu bar cannot be empty") : null;

    private static FlutterError? SemanticsMenuItem(SemanticsNode node)
    {
        for (SemanticsNode? current = node.Parent; current is not null; current = current.Parent)
        {
            if (current.Role is SemanticsRole.Menu or SemanticsRole.MenuBar)
            {
                return null;
            }
        }

        return new FlutterError("A menu item must be a child of a menu or a menu bar");
    }

    private static FlutterError? SemanticsMenuItemCheckbox(SemanticsNode node)
    {
        if (!node.GetSemanticsData().Flags.HasFlag(SemanticsFlags.HasCheckedState))
        {
            return new FlutterError("a menu item checkbox must be checkable");
        }

        for (SemanticsNode? current = node.Parent; current is not null; current = current.Parent)
        {
            if (current.Role is SemanticsRole.Menu or SemanticsRole.MenuBar)
            {
                return null;
            }
        }

        return new FlutterError("A menu item checkbox must be a child of a menu or a menu bar");
    }

    private static FlutterError? SemanticsMenuItemRadio(SemanticsNode node)
    {
        if (!node.GetSemanticsData().Flags.HasFlag(SemanticsFlags.HasCheckedState))
        {
            return new FlutterError("a menu item radio must be checkable");
        }

        for (SemanticsNode? current = node.Parent; current is not null; current = current.Parent)
        {
            if (current.Role is SemanticsRole.Menu or SemanticsRole.MenuBar)
            {
                return null;
            }
        }

        return new FlutterError("A menu item radio must be a child of a menu or a menu bar");
    }

    private static FlutterError? NoLiveRegion(SemanticsNode node)
    {
        SemanticsData data = node.GetSemanticsData();
        return data.Flags.HasFlag(SemanticsFlags.IsLiveRegion)
            ? new FlutterError(
                $"Node {node.Id} has role {RoleName(data.Role)} but is also a live region. "
                + $"A node can not have {RoleName(data.Role)} and be live region at the same time. "
                + "Either remove the role or the live region")
            : null;
    }

    private static FlutterError? SemanticsListItem(SemanticsNode node)
    {
        SemanticsData data = node.GetSemanticsData();
        if (node.Parent is not { } parent)
        {
            return new FlutterError(
                $"Semantics node {node.Id} has role {RoleName(data.Role)} but doesn't have a parent");
        }

        return parent.GetSemanticsData().Role != SemanticsRole.List
            ? new FlutterError(
                $"Semantics node {node.Id} has role {RoleName(data.Role)}, but its "
                + $"parent node {parent.Id} doesn't have the role SemanticsRole.list. "
                + $"Please assign the SemanticsRole.list to node {parent.Id}")
            : null;
    }

    private static bool IsLandmarkRole(SemanticsData data) =>
        data.Role is SemanticsRole.Complementary or SemanticsRole.ContentInfo or SemanticsRole.Main
            or SemanticsRole.Navigation or SemanticsRole.Region;

    private static bool IsSameRoleExisted(SemanticsNode node) =>
        node.Owner!.RegisteredNodes.Count(other => other.GetSemanticsData().Role == node.Role) > 1;

    private static FlutterError? SemanticsComplementary(SemanticsNode node)
    {
        for (SemanticsNode? current = node.Parent; current is not null; current = current.Parent)
        {
            if (IsLandmarkRole(current.GetSemanticsData()))
            {
                return new FlutterError(
                    "The complementary landmark role should not contained within any other landmark roles.");
            }
        }

        return IsSameRoleExisted(node) && node.GetSemanticsData().Label.Length == 0
            ? new FlutterError(
                "The complementary landmark role should have a unique label as it is used more than once.")
            : null;
    }

    private static FlutterError? SemanticsContentInfo(SemanticsNode node)
    {
        for (SemanticsNode? current = node.Parent; current is not null; current = current.Parent)
        {
            if (IsLandmarkRole(current.GetSemanticsData()))
            {
                return new FlutterError(
                    "The contentInfo landmark role should not contained within any other landmark roles.");
            }
        }

        return IsSameRoleExisted(node) && node.GetSemanticsData().Label.Length == 0
            ? new FlutterError(
                "The contentInfo landmark role should have a unique label as it is used more than once.")
            : null;
    }

    private static FlutterError? SemanticsMain(SemanticsNode node)
    {
        for (SemanticsNode? current = node.Parent; current is not null; current = current.Parent)
        {
            if (IsLandmarkRole(current.GetSemanticsData()))
            {
                return new FlutterError("The main landmark role should not contained within any other landmark roles.");
            }
        }

        return IsSameRoleExisted(node) && node.GetSemanticsData().Label.Length == 0
            ? new FlutterError("The main landmark role should have a unique label as it is used more than once.")
            : null;
    }

    private static FlutterError? SemanticsNavigation(SemanticsNode node) =>
        IsSameRoleExisted(node) && node.GetSemanticsData().Label.Length == 0
            ? new FlutterError("The navigation landmark role should have a unique label as it is used more than once.")
            : null;

    private static FlutterError? SemanticsRegion(SemanticsNode node) =>
        node.GetSemanticsData().Label.Length == 0
            ? new FlutterError("A region role should include a label that describes the purpose of the content.")
            : null;

    private static FlutterError? SemanticsGeneral(SemanticsNode node)
    {
        SemanticsData data = node.GetSemanticsData();
        if (data.Flags.HasFlag(SemanticsFlags.HasExpandedState))
        {
            bool hasExpandAction = data.HasAction(SemanticsActions.Expand);
            bool hasCollapseAction = data.HasAction(SemanticsActions.Collapse);
            bool isExpanded = data.Flags.HasFlag(SemanticsFlags.IsExpanded);
            if (hasExpandAction && hasCollapseAction)
            {
                return new FlutterError(
                    "An expandable node cannot have both expand and collapse actions set at the same time.");
            }

            if (isExpanded && hasExpandAction)
            {
                return new FlutterError("An expanded node cannot have an expand action.");
            }

            if (!isExpanded && hasCollapseAction)
            {
                return new FlutterError("A collapsed node cannot have a collapse action.");
            }
        }

        return data.Flags.HasFlag(SemanticsFlags.IsAccessibilityFocusBlocked)
               && data.Flags.HasFlag(SemanticsFlags.IsFocusable)
            ? new FlutterError("A node that is keyboard focusable cannot be set to accessibility unfocusable")
            : null;
    }
}
