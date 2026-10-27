using Avalonia;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity sources: flutter/packages/flutter/test/widgets/selectable_region_test.dart,
// material_ui/test/selection_area_test.dart, material_ui/test/desktop_text_selection_test.dart.
namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class SelectableRegionDartParityTests : IDisposable
{
    public void Dispose()
    {
        PlatformSelectableRegionContextMenu.ResetForTests();
        BrowserContextMenu.ResetForTests();
        PlatformDefaults.DebugTargetPlatformOverride = null;
        PlatformDefaults.DebugIsWebOverride = null;
    }

    [Theory]
    [InlineData(TargetPlatform.Android)]
    [InlineData(TargetPlatform.Fuchsia)]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.MacOS)]
    [InlineData(TargetPlatform.Windows)]
    [InlineData(TargetPlatform.Linux)]
    public void SelectionAreaUsesCorrectSelectionControls(TargetPlatform platform)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(App(new SelectionArea(new Column(children:
        [
            new Text("hello"), new TextField(), new SelectableText("selectable"),
        ])), platform));
        var region = (SelectableRegion)tester.ElementOfType<SelectableRegion>().Widget;
        TextSelectionControls expected = platform switch
        {
            TargetPlatform.Android or TargetPlatform.Fuchsia => MaterialTextSelectionHandleControls.Instance,
#pragma warning disable CS0618
            TargetPlatform.IOS => CupertinoTextSelectionHandleControls.Instance,
#pragma warning restore CS0618
            TargetPlatform.MacOS => CupertinoDesktopTextSelectionControls.HandleControls,
            _ => DesktopTextSelectionControls.HandleControls,
        };
        Assert.Same(expected, region.SelectionControls);
        foreach (Element element in tester.ElementsOfType<EditableText>())
        {
            Assert.Same(expected, ((EditableText)element.Widget).SelectionControls);
        }
        Assert.Same(TextMagnifier.AdaptiveMagnifierConfiguration, region.MagnifierConfiguration);
    }

    [Fact]
    public void SelectionAreaPreservesExplicitNullBuilderAndControlOverrides()
    {
        Assert.NotNull(new SelectionArea(new Text("hello")).ContextMenuBuilder);
        Assert.Null(new SelectionArea(new Text("hello"), contextMenuBuilder: null).ContextMenuBuilder);
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(App(new SelectionArea(
            new Text("hello"),
            contextMenuBuilder: null,
            selectionControls: MaterialTextSelectionControls.Instance,
            magnifierConfiguration: TextMagnifierConfiguration.Disabled)));
        var region = (SelectableRegion)tester.ElementOfType<SelectableRegion>().Widget;
        Assert.Null(region.ContextMenuBuilder);
        Assert.Same(MaterialTextSelectionControls.Instance, region.SelectionControls);
        Assert.Same(TextMagnifierConfiguration.Disabled, region.MagnifierConfiguration);
    }

    [Theory]
    [InlineData(TargetPlatform.Android, false)]
    [InlineData(TargetPlatform.IOS, false)]
    [InlineData(TargetPlatform.Fuchsia, true)]
    [InlineData(TargetPlatform.Linux, true)]
    [InlineData(TargetPlatform.MacOS, true)]
    [InlineData(TargetPlatform.Windows, true)]
    public void BrowserMenuGatesToolbarAndComposition(TargetPlatform platform, bool native)
    {
        PlatformDefaults.DebugIsWebOverride = true;
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(App(new SelectionArea(new Text("How are you?")), platform));
        SelectableRegionState state = tester.State<SelectableRegionState>();
        state.SelectAll();
        Assert.Equal(native, tester.ElementsOfType<PlatformSelectableRegionContextMenu>().Count == 1);
        Assert.Equal(!native, state.ShowToolbar());
        tester.Pump();
        Assert.Equal(!native, state.ContextMenuIsVisible);
        Assert.DoesNotContain(state.ContextMenuButtonItems, item => item.Type == ContextMenuButtonType.Share);
    }

    [Fact]
    public async Task WebCanShowFlutterContextMenuWhenBrowserMenuIsDisabled()
    {
        PlatformDefaults.DebugIsWebOverride = true;
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Linux;
        using var channel = new MockMethodCallHandler(SystemChannels.ContextMenu);
        await BrowserContextMenu.DisableContextMenu();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(App(new SelectionArea(new Text("How are you?")), TargetPlatform.Linux));
        SelectableRegionState state = tester.State<SelectableRegionState>();
        state.SelectAll(SelectionChangedCause.Toolbar);
        tester.PumpAndSettle();
        Assert.True(state.ContextMenuIsVisible);
        Assert.Empty(tester.ElementsOfType<PlatformSelectableRegionContextMenu>());
        state.HideToolbar();
        Assert.False(state.ContextMenuIsVisible);
        await BrowserContextMenu.EnableContextMenu();
        Assert.Equal(["disableContextMenu", "enableContextMenu"], channel.Methods);
    }

    [Fact]
    public void LegacyToolbarWorksWithoutContextMenuBuilderAndReadsSyntheticValue()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(App(new SelectionArea(
            new Text("copy me"), contextMenuBuilder: null,
            selectionControls: MaterialTextSelectionControls.Instance)));
        SelectableRegionState state = tester.State<SelectableRegionState>();
        state.SelectAll();
#pragma warning disable CS0618
        Assert.Equal("__", state.TextEditingValue.Text);
        Assert.Equal(new TextSelection(0, 1), state.TextEditingValue.Selection);
        Assert.True(MaterialTextSelectionControls.Instance.CanCopy(state));
#pragma warning restore CS0618
        Assert.True(state.ShowToolbar());
        tester.PumpAndSettle();
        Assert.True(state.ContextMenuIsVisible);
        state.ClearSelection();
#pragma warning disable CS0618
        Assert.Equal(TextSelection.Collapsed(1), state.TextEditingValue.Selection);
#pragma warning restore CS0618
    }

    [Theory]
    [InlineData(TargetPlatform.Linux, false)]
    [InlineData(TargetPlatform.MacOS, false)]
    [InlineData(TargetPlatform.Windows, false)]
    [InlineData(TargetPlatform.Fuchsia, false)]
    [InlineData(TargetPlatform.Android, true)]
    [InlineData(TargetPlatform.IOS, true)]
    public void OrientationChangeOnlyHidesToolbarOnMobile(TargetPlatform platform, bool hide)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        using var tester = new FrameworkDartTester();
        var child = new SelectionArea(new Text("hello"));
        Widget Root(Size size) => App(new MediaQuery(new MediaQueryData(Size: size), child), platform);
        tester.PumpWidget(Root(new Size(300, 600)));
        SelectableRegionState state = tester.State<SelectableRegionState>();
        state.SelectAll(SelectionChangedCause.Toolbar);
        tester.PumpAndSettle();
        Assert.True(state.ContextMenuIsVisible);
        tester.PumpWidget(Root(new Size(600, 300)));
        Assert.Equal(!hide, state.ContextMenuIsVisible);
        Assert.Equal(platform != TargetPlatform.Android, state.SelectionOverlay?.HandlesAreInserted ?? false);
    }

    [Theory]
    [InlineData(TargetPlatform.Linux)]
    [InlineData(TargetPlatform.MacOS)]
    public void DoesNotOverrideFocusedTextFieldKeyboardShortcuts(TargetPlatform platform)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        using var controller = new TextEditingController("editable text");
        using var fieldFocus = new FocusNode();
        using var regionFocus = new FocusNode();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(App(new SelectionArea(
            focusNode: regionFocus,
            child: new Column(children:
            [
                new Text("static text"),
                new TextField(controller: controller, focusNode: fieldFocus),
            ])), platform));
        fieldFocus.RequestFocus();
        tester.Pump();
        SelectAllKey(tester, platform);
        Assert.Equal(new TextSelection(0, 13), controller.Selection);
        Assert.Null(tester.State<SelectableRegionState>().SelectedContent);

        regionFocus.RequestFocus();
        tester.Pump();
        SelectAllKey(tester, platform);
        Assert.Equal("static text", tester.State<SelectableRegionState>().SelectedContent?.PlainText);
    }

    [Fact]
    public void RegionActionCanBeOverriddenAndCallingActionRunsTheDefault()
    {
        var action = new SelectAllOverride();
        using var focus = new FocusNode();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(App(new Actions(
            actions: new Dictionary<Type, FlutterAction> { [typeof(SelectAllTextIntent)] = action },
            child: new SelectionArea(new Text("all content"), focusNode: focus))));
        focus.RequestFocus();
        tester.Pump();
        SelectAllKey(tester, TargetPlatform.Android);
        Assert.Equal(1, action.Count);
        Assert.True(action.SawCallingAction);
        Assert.Null(action.CallingAction);
        Assert.Equal("all content", tester.State<SelectableRegionState>().SelectedContent?.PlainText);
    }

    [Fact]
    public void NativeMenuSelectsWordThroughTransformedRegionAndHonorsOverlayHitTests()
    {
        PlatformDefaults.DebugIsWebOverride = true;
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Linux;
        using var focus = new FocusNode();
        using var tester = new FrameworkDartTester();
        Widget Region() => new Align(alignment: Alignment.TopLeft, child: new Padding(new EdgeInsets(100, 80, 0, 0),
            new SizedBox(width: 250, height: 100,
                child: new SelectionArea(new Text("hello world"), focusNode: focus))));
        tester.PumpWidget(App(Region(), TargetPlatform.Linux));
        focus.RequestFocus();
        tester.Pump();
        var surface = (RenderBox)tester.ElementOfType<PlatformSelectableRegionContextMenu>().RenderObject!;
        Point position = surface.LocalToGlobal(new Point(5, 5));
        Assert.Equal(new PlatformSelectionContextMenuResult(true, "hello"),
            PlatformSelectableRegionContextMenu.HandleMouseDown(tester.RenderView, position, 2));
        Assert.Equal("hello", tester.State<SelectableRegionState>().SelectedContent?.PlainText);
        for (int button = 0; button < 5; button++)
        {
            if (button == 2) continue;
            Assert.Equal(new PlatformSelectionContextMenuResult(false, ""),
                PlatformSelectableRegionContextMenu.HandleMouseDown(tester.RenderView, position, button));
        }
        Assert.Null(PlatformSelectableRegionContextMenu.HandleMouseDown(tester.RenderView, default, 2));

        tester.PumpWidget(App(new Stack(children:
        [
            Region(),
            Positioned.Fill(new ColoredBox(Colors.Red)),
        ]), TargetPlatform.Linux));
        Assert.Null(PlatformSelectableRegionContextMenu.HandleMouseDown(tester.RenderView, position, 2));
    }

    [Fact]
    public void RegionPreservesAncestorCursorInEmptySpaceAndInnerCursor()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(App(new MouseRegion(cursor: SystemMouseCursors.Grab,
            child: new SelectionArea(new Center(new MouseRegion(cursor: SystemMouseCursors.Forbidden,
                child: new SizedBox(width: 40, height: 40)))))));
        MouseCursor? CursorAt(Point position)
        {
            var hits = new HitTestResult();
            tester.RenderView.HitTest(hits, position);
            return hits.Path.Select(entry => entry.Target).OfType<RenderMouseRegion>()
                .Select(region => region.Cursor).First(cursor => cursor != MouseCursor.Defer);
        }
        Assert.Same(SystemMouseCursors.Grab, CursorAt(new Point(10, 10)));
        Assert.Same(SystemMouseCursors.Forbidden, CursorAt(new Point(400, 300)));
    }

    [Fact]
    public void ProcessTextMenuCapturesReadOnlyContentAndWaitsBeforeHidingToolbar()
    {
        using var channel = new MockMethodCallHandler(SystemChannels.ProcessText, call => call.Method switch
        {
            "ProcessText.queryTextActions" => new Dictionary<string, object?> { ["translate"] = "Translate" },
            _ => "ignored replacement",
        });
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(App(new SelectionArea(new Text("hello world"))));
        SelectableRegionState state = tester.State<SelectableRegionState>();
        Assert.DoesNotContain(state.ContextMenuButtonItems, item => item.Label == "Translate");
        state.SelectAll(SelectionChangedCause.Toolbar);
        tester.PumpAndSettle();
        ContextMenuButtonItem translate = Assert.Single(
            state.ContextMenuButtonItems, item => item.Label == "Translate");
        var completion = new TaskCompletionSource<object?>();
        MethodCall? request = null;
        SystemChannels.ProcessText.SetPlatformMethodCallHandler(call =>
        {
            request = call;
            return completion.Task;
        });
        translate.OnPressed!();
        Assert.True(state.ContextMenuIsVisible);
        MethodCall call = Assert.IsType<MethodCall>(request);
        Assert.Equal(new object?[] { "translate", "hello world", true },
            ((System.Collections.IEnumerable)call.Arguments!).Cast<object?>());
        completion.SetResult("ignored replacement");
        Assert.True(EventLoopPump.SpinUntil(() => !state.ContextMenuIsVisible));
        Assert.Equal("hello world", state.SelectedContent?.PlainText);
    }

    [Fact]
    public void SelectionAreaDoesNotCrashAtZeroArea()
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(App(new Center(new SizedBox(width: 0, height: 0,
            child: new SelectionArea(new Text("hello"))))));
        Assert.Equal(default, tester.GetSize(tester.ElementOfType<SelectionArea>()));
        Assert.Null(tester.TakeException());
    }

    [Fact]
    public void DesktopControlsHaveNoHandlesAndCanSelectAllPartialSelections()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Linux;
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(App(new SelectionArea(new Text("hello"),
            selectionControls: DesktopTextSelectionControls.Instance), TargetPlatform.Linux));
        SelectableRegionState state = tester.State<SelectableRegionState>();
        TextSelectionControls controls = DesktopTextSelectionControls.Instance;
        Assert.Equal(default, controls.GetHandleSize(20));
        Assert.Equal(default, controls.GetHandleAnchor(TextSelectionHandleType.Left, 20));
        Assert.IsType<SizedBox>(controls.BuildHandle(state.Context, TextSelectionHandleType.Left, 20));
#pragma warning disable CS0618
        state.TextEditingValue = new TextEditingValue("hello", new TextSelection(1, 4));
        Assert.True(controls.CanSelectAll(state));
        state.TextEditingValue = new TextEditingValue("hello", new TextSelection(5, 0));
        Assert.False(controls.CanSelectAll(state));
        state.TextEditingValue = new TextEditingValue("", TextSelection.Collapsed(0));
        Assert.False(controls.CanSelectAll(state));
#pragma warning restore CS0618
        state.SelectAll();
        Assert.True(state.ShowToolbar());
        tester.PumpAndSettle();
        Assert.Single(tester.ElementsOfType<DesktopTextSelectionToolbar>());
        Assert.Contains(tester.ElementsOfType<DesktopTextSelectionToolbarButton>(),
            element => ((DesktopTextSelectionToolbarButton)element.Widget).Child is Text { Data: "Copy" });
#pragma warning disable CS0618
        controls.HandleSelectAll(state);
#pragma warning restore CS0618
        Assert.False(state.ContextMenuIsVisible);
        Assert.Equal("hello", state.SelectedContent?.PlainText);
    }

    [Fact]
    public void RegionUsesExternalFocusNodeWithoutOverwritingItsKeyboardHandler()
    {
        int keys = 0;
        using var focus = new FocusNode(onKeyEvent: (_, _) =>
        {
            keys++;
            return KeyEventResult.Handled;
        });
        FocusOnKeyEventCallback? callback = focus.OnKeyEvent;
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(App(new SelectionArea(new Text("hello"), focusNode: focus)));
        focus.RequestFocus();
        tester.Pump();
        tester.SendKeyEvent(LogicalKeyboardKey.KeyB);
        Assert.Same(callback, focus.OnKeyEvent);
        Assert.Equal(2, keys);
    }

    [Theory]
    [InlineData(AppLifecycleState.Inactive, false)]
    [InlineData(AppLifecycleState.Resumed, true)]
    public void LosingFocusOnlyClearsSelectionWhenResumed(AppLifecycleState lifecycle, bool clear)
    {
        using var focus = new FocusNode();
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(App(new SelectionArea(new Text("hello"), focusNode: focus)));
        focus.RequestFocus();
        tester.Pump();
        SelectableRegionState state = tester.State<SelectableRegionState>();
        state.SelectAll();
        SchedulerBinding.Instance.HandleAppLifecycleStateChanged(lifecycle);
        focus.Unfocus();
        EventLoop.FlushMicrotasks();
        Assert.Equal(clear ? null : "hello", state.SelectedContent?.PlainText);
    }

    private static Widget App(Widget child, TargetPlatform platform = TargetPlatform.Android)
        => new MaterialApp(theme: new ThemeData(platform: platform), home: child);

    private static void SelectAllKey(FrameworkDartTester tester, TargetPlatform platform)
    {
        LogicalKeyboardKey modifier = platform == TargetPlatform.MacOS
            ? LogicalKeyboardKey.MetaLeft : LogicalKeyboardKey.ControlLeft;
        tester.SendKeyDownEvent(modifier);
        tester.SendKeyEvent(LogicalKeyboardKey.KeyA);
        tester.SendKeyUpEvent(modifier);
        tester.Pump();
    }

    private sealed class SelectAllOverride : ContextAction<SelectAllTextIntent>
    {
        public int Count { get; private set; }
        public bool SawCallingAction { get; private set; }

        public override object? Invoke(SelectAllTextIntent intent, BuildContext? context)
        {
            Count++;
            SawCallingAction = CallingAction is not null;
            return CallingAction?.Invoke(intent);
        }
    }
}
