using System.Collections;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Avalonia;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/platform_menu_bar.dart
// Tests: flutter/packages/flutter/test/widgets/platform_menu_bar_test.dart
public sealed class PlatformMenuBarTests : IDisposable
{
    private readonly PlatformMenuDelegate _original = WidgetsBinding.Instance.PlatformMenuDelegate;
    private readonly MethodChannel _channel = new("test/platform_menu");
    private readonly MockMethodCallHandler _platform;
    private readonly DefaultPlatformMenuDelegate _delegate;

    public PlatformMenuBarTests()
    {
        _platform = new MockMethodCallHandler(_channel);
        _delegate = new DefaultPlatformMenuDelegate(_channel);
        WidgetsBinding.Instance.PlatformMenuDelegate = _delegate;
    }

    public void Dispose()
    {
        Scheduler.FlushMicrotasks();
        WidgetsBinding.Instance.PlatformMenuDelegate = _original;
        _channel.SetMethodCallHandler(null);
        _platform.Dispose();
    }

    // Flutter's two 'basic menu structure is transmitted to platform' cases.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Structure_MatchesFlutterIncludingIdsOfDiscardedDividers(bool useIntent)
    {
        PlatformMenuItem Leaf(string label, string? tooltip = null, IMenuSerializableShortcut? shortcut = null) =>
            new(label, tooltip, shortcut, onSelected: useIntent ? null : () => { },
                onSelectedIntent: useIntent ? new DoNothingIntent() : null);
        IReadOnlyList<PlatformMenuItem> menus =
        [
            new PlatformMenu("Menu 0", [Leaf("Sub Menu 00", "Sub Menu 00 Tooltip")], tooltip: "Menu 0 Tooltip"),
            new PlatformMenu("Menu 1",
            [
                new PlatformMenuItemGroup([Leaf("Sub Menu 10", "Sub Menu 10 Tooltip")]),
                new PlatformMenu("Sub Menu 11",
                [
                    new PlatformMenuItemGroup([
                        Leaf("Sub Sub Menu 110", shortcut:
                            new SingleActivator(LogicalKeyboardKey.KeyA, control: true))]),
                    new PlatformMenuItemGroup([
                        Leaf("Sub Sub Menu 111", "Sub Sub Menu 111 Tooltip",
                            new SingleActivator(LogicalKeyboardKey.KeyB, shift: true))]),
                    Leaf("Sub Sub Menu 112", shortcut: new SingleActivator(LogicalKeyboardKey.KeyC, alt: true)),
                    new PlatformMenuItemGroup([
                        Leaf("Sub Sub Menu 113", "Sub Sub Menu 113 Tooltip",
                            new SingleActivator(LogicalKeyboardKey.KeyD, meta: true))]),
                ]),
                Leaf("Sub Menu 12", "Sub Menu 12 Tooltip"),
            ]),
            new PlatformMenu("Menu 2", [new PlatformMenuItem("Sub Menu 20")], tooltip: "Menu 2 Tooltip"),
            new PlatformMenu("Menu 3", []),
        ];
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new PlatformMenuBar(menus, child: new SizedBox(width: 123, height: 45)));
        Assert.Equal(2, _platform.Log.Count); // Clear, then publish.
        Assert.Equal("Menu.setMenus", _platform.Log[^1].Method);
        AssertJson("""
            {"0":[
              {"id":2,"label":"Menu 0","tooltip":"Menu 0 Tooltip","enabled":true,"children":[
                {"id":1,"label":"Sub Menu 00","tooltip":"Sub Menu 00 Tooltip","enabled":true}]},
              {"id":18,"label":"Menu 1","enabled":true,"children":[
                {"id":4,"label":"Sub Menu 10","tooltip":"Sub Menu 10 Tooltip","enabled":true},
                {"id":5,"isDivider":true},
                {"id":16,"label":"Sub Menu 11","enabled":true,"children":[
                  {"id":7,"label":"Sub Sub Menu 110","enabled":true,"shortcutTrigger":97,"shortcutModifiers":8},
                  {"id":8,"isDivider":true},
                  {"id":10,"label":"Sub Sub Menu 111","tooltip":"Sub Sub Menu 111 Tooltip",
                    "enabled":true,"shortcutTrigger":98,"shortcutModifiers":2},
                  {"id":11,"isDivider":true},
                  {"id":12,"label":"Sub Sub Menu 112","enabled":true,"shortcutTrigger":99,"shortcutModifiers":4},
                  {"id":13,"isDivider":true},
                  {"id":14,"label":"Sub Sub Menu 113","tooltip":"Sub Sub Menu 113 Tooltip",
                    "enabled":true,"shortcutTrigger":100,"shortcutModifiers":1}]},
                {"id":17,"label":"Sub Menu 12","tooltip":"Sub Menu 12 Tooltip","enabled":true}]},
              {"id":20,"label":"Menu 2","tooltip":"Menu 2 Tooltip","enabled":true,"children":[
                {"id":19,"label":"Sub Menu 20","enabled":false}]},
              {"id":21,"label":"Menu 3","enabled":false,"children":[]}
            ]}
            """, _platform.Log[^1].Arguments);
        AssertJson("""{"0":[]}""", _platform.Log[0].Arguments);
        tester.PumpWidget(new SizedBox());
        AssertJson("""{"0":[]}""", _platform.Log[^1].Arguments);
    }

    [DebugOnlyFact]
    public void NestedBars_AssertWhenTheDelegateIsAlreadyLocked()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new PlatformMenuBar([], child: new PlatformMenuBar([])));
        Assert.IsType<AssertionError>(tester.TakeException());
    }

    [DebugOnlyFact]
    public void Diagnostics_DescribeMenuDataRatherThanTheWidgetChild()
    {
        var item = new PlatformMenuItem("label2", "tooltip2", new SingleActivator(LogicalKeyboardKey.KeyA));
        var bar = new PlatformMenuBar([item], child: new Text("Body"));
        string tree = Regex.Replace(bar.ToStringDeep(), "#[0-9a-f]+", "#00000");
        Assert.Equal(
            "PlatformMenuBar#00000\n"
            + " └─PlatformMenuItem#00000(label2)\n"
            + "     label: \"label2\"\n"
            + "     tooltip: \"tooltip2\"\n"
            + "     shortcut: SingleActivator#00000(keys: Key A)\n"
            + "     DISABLED\n",
            tree);
        var menu = new PlatformMenu("label", [new PlatformMenuItem("label")]);
        var properties = new DiagnosticPropertiesBuilder();
        menu.DebugFillProperties(properties);
        Assert.Equal(["label: \"label\""], properties.Properties
            .Where(property => !property.IsFiltered(DiagnosticLevel.Info)).Select(property => property.ToString()));
        Assert.Single(menu.ToDiagnosticsNode().GetChildren());
    }

    [Fact]
    public void PublishErrors_AreReportedForBothMountCallsAndDispose()
    {
        using var tester = new FrameworkDartTester();
        var errors = new List<FlutterErrorDetails>();
        FlutterExceptionHandler? previous = FlutterError.OnError;
        var error = new InvalidOperationException("Failed to set menu");
        var failingChannel = new FailingMenuChannel(error);
        WidgetsBinding.Instance.PlatformMenuDelegate = new DefaultPlatformMenuDelegate(failingChannel);
        FlutterError.OnError = errors.Add;
        try
        {
            tester.PumpWidget(new PlatformMenuBar([]));
            tester.Pump();
            Assert.Equal(2, errors.Count);
            tester.PumpWidget(new SizedBox());
            tester.Pump();
            Assert.Equal(3, errors.Count);
            Assert.All(errors, details =>
            {
                Assert.Same(error, details.Exception);
                Assert.Equal("widget library", details.Library);
                Assert.Contains("while setting the platform menu", details.Context!.ToString());
            });
        }
        finally
        {
            FlutterError.OnError = previous;
            failingChannel.SetMethodCallHandler(null);
        }
    }

    [Fact]
    public void Serialization_CharactersAndModifiersMatchFlutter()
    {
        AssertJson("""{"shortcutCharacter":"?","shortcutModifiers":0}""",
            ShortcutSerialization.ForCharacter("?").ToChannelRepresentation());
        AssertJson("""{"shortcutCharacter":"?","shortcutModifiers":13}""",
            ShortcutSerialization.ForCharacter("?", alt: true, control: true, meta: true).ToChannelRepresentation());
        AssertJson($$"""{"shortcutTrigger":{{LogicalKeyboardKey.Home.KeyId}},"shortcutModifiers":0}""",
            ShortcutSerialization.Modifier(LogicalKeyboardKey.Home).ToChannelRepresentation());
        AssertJson($$"""{"shortcutTrigger":{{LogicalKeyboardKey.Home.KeyId}},"shortcutModifiers":15}""",
            ShortcutSerialization.Modifier(LogicalKeyboardKey.Home, alt: true, control: true, meta: true, shift: true)
                .ToChannelRepresentation());
    }

    [Fact]
    public void Updates_CompareDescendantIdentitiesAndPreserveTheFirstUpdateSend()
    {
        using var tester = new FrameworkDartTester();
        var leaf = new PlatformMenuItem("A");
        var group = new PlatformMenuItemGroup([leaf]);
        var menu = new PlatformMenu("File", [group]);
        tester.PumpWidget(new PlatformMenuBar([menu]));
        Assert.Equal(2, _platform.Log.Count);
        tester.PumpWidget(new PlatformMenuBar([menu]));
        Assert.Equal(3, _platform.Log.Count); // Dart leaves the initial descendants list empty.
        tester.PumpWidget(new PlatformMenuBar([menu], child: new SizedBox(width: 50)));
        Assert.Equal(3, _platform.Log.Count);
        Assert.Equal([group], menu.Descendants); // Groups do not enumerate their members as descendants.
        tester.PumpWidget(new PlatformMenuBar([new PlatformMenu("File", [group])]));
        Assert.Equal(4, _platform.Log.Count);
    }

    [Fact]
    public async Task Callbacks_SelectOpenCloseAndUnknownMethodsFollowTheCurrentIdMap()
    {
        var calls = new List<string>();
        var leaf = new PlatformMenuItem("A", onSelected: () => calls.Add("selected"));
        var menu = new PlatformMenu("File", [leaf], onOpen: () => calls.Add("opened"),
            onClose: () => calls.Add("closed"));
        _delegate.SetMenus([menu]);
        await SendCallback("Menu.opened", 2);
        await SendCallback("Menu.selectedCallback", 1);
        await SendCallback("Menu.closed", 2);
        await SendCallback("other", 1);
        Assert.Equal(["opened", "selected", "closed"], calls);
        _delegate.SetMenus([leaf]);
        Assert.Equal(3, ((IDictionary)((IList)((IDictionary)_platform.Log[^1].Arguments!)["0"]!)[0]!)["id"]);
        await SendCallback("Menu.selectedCallback", 3);
        Assert.Equal(4, calls.Count);
        _delegate.ClearMenus();
        if (Constants.KDebugMode)
        {
            PlatformException error = await Assert.ThrowsAsync<PlatformException>(
                () => SendCallback("Menu.selectedCallback", 3));
            Assert.Contains("ID that was not recognized: 3", error.Message);
        }
        else
        {
            await SendCallback("Menu.selectedCallback", 3);
        }

        Assert.Equal(4, calls.Count);
    }

    [Fact]
    public async Task IntentSelection_InvokesTheActionAtPrimaryFocus()
    {
        using var tester = new FrameworkDartTester();
        using var focus = new FocusNode();
        int invoked = 0;
        tester.PumpWidget(new Actions(
            actions: new Dictionary<Type, FlutterAction>
            {
                [typeof(DoNothingIntent)] = new CallbackAction<DoNothingIntent>(_ => { invoked += 1; return null; }),
            },
            child: new Focus(focusNode: focus, child: new SizedBox())));
        focus.RequestFocus();
        tester.Pump();
        _delegate.SetMenus([new PlatformMenuItem("Action", onSelectedIntent: new DoNothingIntent())]);
        await SendCallback("Menu.selectedCallback", 1);
        Assert.Equal(1, invoked);
    }

    [Fact]
    public void Layout_ChildPassesThroughAndMissingChildIsAnEmptySizedBox()
    {
        using var tester = new FrameworkDartTester();
        var key = new ValueKey<string>("body");
        tester.PumpWidget(new Center(child: new PlatformMenuBar([], child: new SizedBox(123, 45, key: key))));
        Assert.Equal(new Size(123, 45), tester.GetSize(Find.ByKey(key)));
        tester.PumpWidget(new Center(child: new PlatformMenuBar([])));
        Assert.Equal(new Size(0, 0), tester.GetSize(Find.ByType<PlatformMenuBar>()));
    }

    [Theory]
    [InlineData(TargetPlatform.MacOS, true)]
    [InlineData(TargetPlatform.Android, false)]
    [InlineData(TargetPlatform.IOS, false)]
    [InlineData(TargetPlatform.Fuchsia, false)]
    [InlineData(TargetPlatform.Linux, false)]
    [InlineData(TargetPlatform.Windows, false)]
    public void ProvidedItems_SupportMatchesThePlatformAndSerializesTheOrdinal(TargetPlatform platform, bool supported)
    {
        TargetPlatform? previous = PlatformDefaults.DebugTargetPlatformOverride;
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        try
        {
            foreach (PlatformProvidedMenuItemType type in Enum.GetValues<PlatformProvidedMenuItemType>())
            {
                Assert.Equal(supported, PlatformProvidedMenuItem.HasMenu(type));
                var item = new PlatformProvidedMenuItem(type, enabled: false);
                Assert.Empty(item.Label);
                if (supported || !Constants.KDebugMode)
                {
                    _delegate.SetMenus([item]);
                    AssertJson($$"""
                        {"0":[{"id":{{(int)type + 1}},"enabled":false,"platformProvidedMenu":{{(int)type}}}]}
                        """, _platform.Log[^1].Arguments);
                }
                else
                {
                    Assert.Throws<ArgumentException>(() => _delegate.SetMenus([item]));
                }
            }
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = previous;
        }
    }

    [DebugOnlyFact]
    public void Assertions_RejectEmptyGroupsAndConflictingSelectionsAtTheSourceBoundary()
    {
        var group = new PlatformMenuItemGroup([]); // Construction itself is valid.
        Assert.Throws<AssertionError>(() => group.ToChannelRepresentation(_delegate, _ => 1));
        Assert.Throws<AssertionError>(() => new PlatformMenuItem("A", onSelected: () => { },
            onSelectedIntent: new DoNothingIntent()));
    }

    [Fact]
    public void TopLevelGroups_KeepDividersAndDisabledShortcutsStillSerialize()
    {
        var leaf = new PlatformMenuItem("A", tooltip: "", shortcut: new CharacterActivator("?"));
        _delegate.SetMenus([new PlatformMenuItemGroup([leaf])]);
        AssertJson("""
            {"0":[{"id":1,"isDivider":true},
            {"id":2,"label":"A","tooltip":"","enabled":false,"shortcutCharacter":"?","shortcutModifiers":0},
            {"id":3,"isDivider":true}]}
            """, _platform.Log[^1].Arguments);
    }

    private async Task SendCallback(string method, int id)
    {
        ByteData? reply = null;
        await _channel.BinaryMessenger.HandlePlatformMessage(
            _channel.Name, _channel.Codec.EncodeMethodCall(new MethodCall(method, id)), data => reply = data);
        Assert.NotNull(reply);
        _channel.Codec.DecodeEnvelope(reply!);
    }

    private static void AssertJson(string expected, object? actual) => Assert.True(
        JsonNode.DeepEquals(JsonNode.Parse(expected), JsonSerializer.SerializeToNode(actual)),
        JsonSerializer.Serialize(actual));

    private sealed class FailingMenuChannel(Exception error) : MethodChannel("test/failing_menu")
    {
        public override Task<T?> InvokeMethod<T>(string method, object? arguments = null) where T : default =>
            Task.FromException<T?>(error);
    }
}
