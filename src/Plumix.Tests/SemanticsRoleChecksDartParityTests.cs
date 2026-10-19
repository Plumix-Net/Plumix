using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/semantics/semantics.dart (_DebugSemanticsRoleChecks)
// Tests: flutter/packages/flutter/test/widgets/semantics_role_checks_test.dart, semantics_checks_test.dart.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class SemanticsRoleChecksDartParityTests
{
    private static SemanticsConfiguration Config(SemanticsRole role) => new() { Role = role };

    private static SemanticsNode Node(SemanticsConfiguration config, params SemanticsNode[] children)
    {
        var node = new SemanticsNode { Rect = new Rect(0, 0, 50, 50) };
        node.UpdateWith(config, children);
        return node;
    }

    private static void Send(SemanticsConfiguration config, params SemanticsNode[] children)
    {
        using var owner = new SemanticsOwner();
        SemanticsNode root = SemanticsNode.Root(owner);
        root.Rect = new Rect(0, 0, 100, 100);
        root.UpdateWith(config, children);
        owner.SendSemanticsUpdate();
    }

    private static void Reject(string message, SemanticsConfiguration config, params SemanticsNode[] children) =>
        Assert.Equal(message, Assert.Throws<FlutterError>(() => Send(config, children)).Message);

    private static SemanticsConfiguration Tab(bool? selected = false, bool tap = true)
    {
        var config = Config(SemanticsRole.Tab);
        config.IsSelected = selected;
        if (tap)
        {
            config.OnTap = static () => { };
        }

        return config;
    }

    [DebugOnlyFact]
    public void TabsRequireSelectedStateBeforeTapAndAcceptDisabledOrBlockedActions()
    {
        Reject("A tab needs selected states", Tab(selected: null));
        Reject("A tab must have a tap action", Tab(tap: false));
        Send(Tab());
        var disabled = Tab(tap: false);
        disabled.IsEnabled = false;
        Send(disabled);
        var blocked = Tab(tap: false);
        blocked.IsBlockingUserActions = true;
        Send(blocked);
        blocked.IsSelected = null;
        Reject("A tab needs selected states", blocked);
    }

    [DebugOnlyFact]
    public void TabBarsRequireDirectTabChildren()
    {
        Reject("a TabBar cannot be empty", Config(SemanticsRole.TabBar));
        Reject("Children of TabBar must have the tab role",
            Config(SemanticsRole.TabBar), Node(Config(SemanticsRole.None)));
        Send(Config(SemanticsRole.TabBar), Node(Tab()));
    }

    [DebugOnlyFact]
    public void TablesRequireRowsAndRowsRequireCellsOrColumnHeaders()
    {
        Send(Config(SemanticsRole.Table));
        Reject("Children of Table must have the row role",
            Config(SemanticsRole.Table), Node(Config(SemanticsRole.Cell)));
        Reject("A row must be a child of a table", Config(SemanticsRole.Row));
        Reject(
            "Children of Row must have the cell or columnHeader role",
            Config(SemanticsRole.Table),
            Node(Config(SemanticsRole.Row), Node(Config(SemanticsRole.None))));
        Send(
            Config(SemanticsRole.Table),
            Node(Config(SemanticsRole.Row),
                Node(Config(SemanticsRole.Cell), Node(Config(SemanticsRole.Cell))),
                Node(Config(SemanticsRole.ColumnHeader))));
        Send(Config(SemanticsRole.Table), Node(Config(SemanticsRole.Row)));
        Reject("A cell must be a child of a row or another cell", Config(SemanticsRole.Cell));
        Reject("A columnHeader must be a child or another cell", Config(SemanticsRole.ColumnHeader));
    }

    [DebugOnlyFact]
    public void ListItemsRequireAnImmediateListParent()
    {
        Reject("Semantics node 0 has role SemanticsRole.listItem but doesn't have a parent",
            Config(SemanticsRole.ListItem));
        var item = Node(Config(SemanticsRole.ListItem));
        string message = $"Semantics node {item.Id} has role SemanticsRole.listItem, but its "
                         + "parent node 0 doesn't have the role SemanticsRole.list. "
                         + "Please assign the SemanticsRole.list to node 0";
        Reject(message, Config(SemanticsRole.None), item);
        Send(Config(SemanticsRole.List), Node(Config(SemanticsRole.ListItem)));
        var nested = Node(Config(SemanticsRole.None), Node(Config(SemanticsRole.ListItem)));
        Assert.Throws<FlutterError>(() => Send(Config(SemanticsRole.List), nested));
    }

    private static SemanticsNode Radio(bool? check, bool exclusive = true, params SemanticsNode[] children) =>
        Node(new SemanticsConfiguration { IsChecked = check, IsInMutuallyExclusiveGroup = exclusive }, children);

    [DebugOnlyFact]
    public void RadioGroupsFollowWrappersAndSkipNestedGroupsAndNonRadioControls()
    {
        Send(Config(SemanticsRole.RadioGroup), Radio(false, exclusive: false));
        Reject(
            "Radio groups must not have multiple checked children",
            Config(SemanticsRole.RadioGroup), Radio(true), Radio(true));
        Send(Config(SemanticsRole.RadioGroup), Radio(false), Radio(true));
        Send(Config(SemanticsRole.RadioGroup),
            Node(new SemanticsConfiguration { Label = "label", TextDirection = TextDirection.Ltr }, Radio(false)),
            Node(new SemanticsConfiguration { Label = "label", TextDirection = TextDirection.Ltr }, Radio(true)));
        Send(Config(SemanticsRole.RadioGroup), Node(new SemanticsConfiguration { IsToggled = true }));
        Send(Config(SemanticsRole.RadioGroup), Radio(true, exclusive: false), Radio(true));
        Send(Config(SemanticsRole.RadioGroup), Radio(true),
            Node(Config(SemanticsRole.RadioGroup), Radio(true), Radio(false)));
        // Dart does not recurse into a mutually exclusive node, including an unchecked participant.
        Send(Config(SemanticsRole.RadioGroup), Radio(true), Radio(false, true, Radio(true)));
        Reject(
            "Radio groups must not have multiple checked children",
            Config(SemanticsRole.RadioGroup), Radio(true),
            Node(Config(SemanticsRole.RadioGroup), Radio(true), Radio(true)));
    }

    [DebugOnlyTheory]
    [InlineData(SemanticsRole.Menu, "a menu cannot be empty")]
    [InlineData(SemanticsRole.MenuBar, "a menu bar cannot be empty")]
    public void MenusRequireChildrenButDoNotRestrictTheirRoles(SemanticsRole role, string message)
    {
        Reject(message, Config(role));
        Send(Config(role), Node(Config(SemanticsRole.None)));
    }

    [DebugOnlyTheory]
    [InlineData(SemanticsRole.MenuItem, "menu item")]
    [InlineData(SemanticsRole.MenuItemCheckbox, "menu item checkbox")]
    [InlineData(SemanticsRole.MenuItemRadio, "menu item radio")]
    public void MenuItemsRequireAnAncestorAndCheckboxAndRadioRequireCheckability(SemanticsRole role, string name)
    {
        var config = Config(role);
        if (role != SemanticsRole.MenuItem)
        {
            Reject($"a {name} must be checkable", config);
            config.IsChecked = false;
        }

        Reject($"A {name} must be a child of a menu or a menu bar", config);
        foreach (SemanticsRole parent in new[] { SemanticsRole.Menu, SemanticsRole.MenuBar })
        {
            Send(Config(parent), Node(config));
            Send(Config(parent), Node(Config(SemanticsRole.None), Node(config)));
            config.IsChecked = true;
            Send(Config(parent), Node(config));
            config.IsCheckStateMixed = true;
            Send(Config(parent), Node(config));
        }
    }

    [DebugOnlyTheory]
    [InlineData(SemanticsRole.Alert, "alert")]
    [InlineData(SemanticsRole.Status, "status")]
    public void AlertsAndStatusesRejectLiveRegion(SemanticsRole role, string name)
    {
        var config = Config(role);
        config.LiveRegion = true;
        Reject(
            $"Node 0 has role SemanticsRole.{name} but is also a live region. "
            + $"A node can not have SemanticsRole.{name} and be live region at the same time. "
            + "Either remove the role or the live region", config);
        config.LiveRegion = false;
        Send(config);
    }

    [DebugOnlyTheory]
    [InlineData(SemanticsRole.Complementary, "complementary")]
    [InlineData(SemanticsRole.ContentInfo, "contentInfo")]
    [InlineData(SemanticsRole.Main, "main")]
    [InlineData(SemanticsRole.Navigation, "navigation")]
    public void RepeatedLandmarksRequireLabelsAcrossTheWholeOwner(SemanticsRole role, string name)
    {
        Send(Config(role));
        Reject(
            $"The {name} landmark role should have a unique label as it is used more than once.",
            Config(SemanticsRole.None), Node(Config(role)),
            Node(Config(SemanticsRole.None), Node(Config(role))));
        var first = Config(role);
        first.Label = "first";
        first.TextDirection = TextDirection.Ltr;
        var second = Config(role);
        second.Label = "second";
        second.TextDirection = TextDirection.Ltr;
        Send(Config(SemanticsRole.None), Node(first), Node(second));
        // The pinned implementation checks for nonempty labels, not different labels.
        second.Label = "first";
        Send(Config(SemanticsRole.None), Node(first), Node(second));
    }

    [DebugOnlyTheory]
    [InlineData(SemanticsRole.Complementary, "complementary")]
    [InlineData(SemanticsRole.ContentInfo, "contentInfo")]
    [InlineData(SemanticsRole.Main, "main")]
    public void RestrictedLandmarksRejectEveryLandmarkAncestor(SemanticsRole role, string name)
    {
        foreach (SemanticsRole parent in new[]
                 { SemanticsRole.Complementary, SemanticsRole.ContentInfo, SemanticsRole.Main,
                   SemanticsRole.Navigation, SemanticsRole.Region })
        {
            var config = Config(parent);
            config.Label = "landmark";
            config.TextDirection = TextDirection.Ltr;
            Reject($"The {name} landmark role should not contained within any other landmark roles.", config,
                Node(Config(SemanticsRole.None), Node(Config(role))));
        }
    }

    [DebugOnlyFact]
    public void RegionsRequireLabelsAndNavigationMayNestInsideLandmarks()
    {
        Reject("A region role should include a label that describes the purpose of the content.",
            Config(SemanticsRole.Region));
        var region = Config(SemanticsRole.Region);
        region.Label = "Header 1";
        region.TextDirection = TextDirection.Ltr;
        Send(region);
        Send(Config(SemanticsRole.Main), Node(Config(SemanticsRole.Navigation)));
    }

    public static IEnumerable<object?[]> ProgressCases()
    {
        yield return [null, null, null, "A progress bar must have a value, a minValue, a maxValue."];
        yield return [null, "0", "10", "A progress bar must have a value, a minValue, a maxValue."];
        yield return ["5", null, null, "A progress bar must have a value, a minValue, a maxValue."];
        foreach ((string value, string min, string max) in new[]
                 { ("invalid", "0", "10"), ("invalid", "0%", "100%"), ("invalid", "invalid", "invalid"),
                   ("5", "0%", "100%"), ("1,0", "0", "10"), ("nan", "0", "10"), ("1_0", "0", "10") })
        {
            yield return [value, min, max,
                "Progress bar value, minValue, and maxValue must be valid numbers. "
                + $"value: \"{value}\", minValue: \"{min}\", maxValue: \"{max}\""];
        }

        yield return ["5", "10", "0", "Progress bar minValue (10) must be less than maxValue (0)"];
        yield return ["5", "5", "5", "Progress bar minValue (5) must be less than maxValue (5)"];
        yield return ["20", "0", "10", "Progress bar value (20) must be between minValue (0) and maxValue (10)"];
        yield return ["-1", "0", "10", "Progress bar value (-1) must be between minValue (0) and maxValue (10)"];
        yield return ["150%", "0", "10", "Progress bar percentage value (150%) must be between 0% and 100%"];
        yield return ["-1%", "0", "10", "Progress bar percentage value (-1%) must be between 0% and 100%"];
        foreach (string value in new[]
                 { "5", "50%", "0", "10", "0%", "100%", " 5 ", "5e0", "NaN", "-NaN", ".5", "5.", "\uFEFF 5 \uFEFF" })
        {
            yield return [value, "0", "10", null];
        }
    }

    [DebugOnlyTheory]
    [MemberData(nameof(ProgressCases))]
    public void ProgressBarsValidateNumbersPercentagesAndRanges(string? value, string? min, string? max, string? error)
    {
        var config = new SemanticsConfiguration
        {
            Role = SemanticsRole.ProgressBar,
            Value = value ?? string.Empty,
            MinValue = min,
            MaxValue = max,
            TextDirection = TextDirection.Ltr,
        };
        if (error is not null)
        {
            Reject(error, config);
        }
        else
        {
            Send(config);
        }
    }

    [DebugOnlyTheory]
    [InlineData(SemanticsRole.DragHandle)]
    [InlineData(SemanticsRole.SpinButton)]
    [InlineData(SemanticsRole.ComboBox)]
    [InlineData(SemanticsRole.Tooltip)]
    [InlineData(SemanticsRole.HotKey)]
    public void UnimplementedRolesReportThePinnedFlutterError(SemanticsRole role) =>
        Reject($"Missing checks for role SemanticsRole.{Diagnostics.EnumName(role)}", Config(role));

    [DebugOnlyTheory]
    [InlineData(SemanticsRole.None)]
    [InlineData(SemanticsRole.Dialog)]
    [InlineData(SemanticsRole.AlertDialog)]
    [InlineData(SemanticsRole.TabPanel)]
    [InlineData(SemanticsRole.List)]
    [InlineData(SemanticsRole.Form)]
    [InlineData(SemanticsRole.LoadingSpinner)]
    public void RolesWithoutSpecialChecksStillRunGeneralChecks(SemanticsRole role)
    {
        Send(Config(role));
        var config = Config(role);
        config.IsExpanded = false;
        config.OnExpand = static () => { };
        config.OnCollapse = static () => { };
        Reject("An expandable node cannot have both expand and collapse actions set at the same time.", config);
    }

    [DebugOnlyFact]
    public void ExpandedStateAndActionChecksMatchFlutter()
    {
        Send(new SemanticsConfiguration { IsExpanded = false });
        Send(new SemanticsConfiguration { IsExpanded = false, OnExpand = static () => { } });
        Send(new SemanticsConfiguration { IsExpanded = true, OnCollapse = static () => { } });
        Reject("An expanded node cannot have an expand action.",
            new SemanticsConfiguration { IsExpanded = true, OnExpand = static () => { } });
        Reject("A collapsed node cannot have a collapse action.",
            new SemanticsConfiguration { IsExpanded = false, OnCollapse = static () => { } });
        Send(new SemanticsConfiguration { OnExpand = static () => { }, OnCollapse = static () => { } });
    }

    [DebugOnlyTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void AccessibilityFocusBlockingRejectsKeyboardFocusability(bool focused) =>
        Reject("A node that is keyboard focusable cannot be set to accessibility unfocusable",
            new SemanticsConfiguration
            {
                IsFocused = focused,
                AccessibilityFocusBlockType = AccessibilityFocusBlockType.BlockNode,
            });

    [Fact]
    public void RoleValidationIsRemovedFromProfileAndRelease()
    {
        if (!Constants.KDebugMode)
        {
            Send(Config(SemanticsRole.Tab));
            Send(Config(SemanticsRole.Tooltip));
            Send(Config(SemanticsRole.Region));
        }
    }

    [DebugOnlyFact]
    public void MergedDataParticipatesInChecksButDispatchUsesTheNodesOwnRole()
    {
        var merged = new SemanticsConfiguration
        {
            Role = SemanticsRole.Tab,
            IsSemanticBoundary = true,
            IsMergingSemanticsOfDescendants = true,
        };
        Send(merged, Node(new SemanticsConfiguration { IsSelected = false, OnTap = static () => { } }));
        // A none-role merge root absorbs a tab but dispatch still chooses none, as in Dart.
        merged.Role = SemanticsRole.None;
        Send(merged, Node(Config(SemanticsRole.Tab)));
    }

    [DebugOnlyFact]
    public void WidgetPipelineReportsErrorsAndRecoversAfterAValidRebuild()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(TextDirection.Ltr,
            new Semantics(role: SemanticsRole.Tab, child: new SizedBox(width: 30, height: 20))));
        Assert.Equal("A tab needs selected states", Assert.IsType<FlutterError>(tester.TakeException()).Message);
        tester.PumpWidget(new Directionality(TextDirection.Ltr,
            new Semantics(role: SemanticsRole.Tab, selected: false, onTap: static () => { },
                child: new SizedBox(width: 30, height: 20))));
        Assert.Null(tester.TakeException());
    }

    [Fact]
    public void MirroredStorageDemoUsesValidListItemAndStatusRoles()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new Directionality(TextDirection.Ltr,
            new SizedBox(width: 800, height: 600, child: new StateStorageDemoPage())));
        Assert.Null(tester.TakeException());
        SemanticsNode[] nodes = Descendants(tester.SemanticsRootNode!).ToArray();
        SemanticsNode list = Assert.Single(nodes, node => node.Role == SemanticsRole.List);
        Assert.NotEmpty(list.Children);
        Assert.All(list.Children, node => Assert.Equal(SemanticsRole.ListItem, node.Role));
        Assert.Contains(nodes, node => node.Role == SemanticsRole.Status && node.Label.StartsWith("Observer:"));

        static IEnumerable<SemanticsNode> Descendants(SemanticsNode node)
        {
            yield return node;
            foreach (SemanticsNode child in node.Children)
            {
                foreach (SemanticsNode descendant in Descendants(child))
                {
                    yield return descendant;
                }
            }
        }
    }

}
