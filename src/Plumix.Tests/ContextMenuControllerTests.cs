using Avalonia;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/context_menu_controller.dart
// Flutter tests: flutter/packages/flutter/test/widgets/context_menu_controller_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class ContextMenuControllerTests : IDisposable
{
    private static readonly Size ViewSize = new(400, 300);

    public ContextMenuControllerTests()
    {
        Scheduler.ResetForTests();
        ContextMenuController.RemoveAny();
        FocusManager.Instance.ResetForTests();
    }

    public void Dispose()
    {
        ContextMenuController.RemoveAny();
        FocusManager.Instance.ResetForTests();
        GestureBinding.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void HidesAndShowsOnlyASingleMenuAcrossIndependentOverlays()
    {
        using var first = new Harness();
        using var second = new Harness();
        var controller1 = new ContextMenuController();
        var controller2 = new ContextMenuController();
        Assert.Null(controller1.OnRemove);
        Assert.False(controller1.IsShown);

        controller1.Show(first.Context, _ => new Text("first"));
        first.Pump();
        Assert.NotNull(first.FindText("first"));
        controller2.Show(second.Context, _ => new Text("second"));
        first.Pump();
        second.Pump();
        Assert.False(controller1.IsShown);
        Assert.True(controller2.IsShown);
        Assert.Null(first.FindText("first"));
        Assert.NotNull(second.FindText("second"));

        controller1.Remove();
        Assert.True(controller2.IsShown);
        controller2.Remove();
        second.Pump();
        Assert.Null(second.FindText("second"));
    }

    [Fact]
    public void ShowUpdatesInPlaceAndRemoveThenShowCreatesFreshState()
    {
        using var harness = new Harness();
        int removed = 0;
        var controller = new ContextMenuController(() => removed++);
        controller.Show(harness.Context, _ => new StatefulMenu(1));
        harness.Pump();
        OverlayEntry entry = harness.Overlay.Entries.Last();
        Assert.NotNull(harness.FindText("Initial: 1, Current: 1"));

        controller.Show(harness.Context, _ => new StatefulMenu(2));
        harness.Pump();
        Assert.Same(entry, harness.Overlay.Entries.Last());
        Assert.NotNull(harness.FindText("Initial: 1, Current: 2"));
        Assert.Equal(0, removed);

        controller.Remove();
        harness.Pump();
        Assert.False(entry.Mounted);
        Assert.Throws<ObjectDisposedException>(entry.MarkNeedsBuild);
        controller.Show(harness.Context, _ => new StatefulMenu(3));
        harness.Pump();
        Assert.NotSame(entry, harness.Overlay.Entries.Last());
        Assert.NotNull(harness.FindText("Initial: 3, Current: 3"));
        Assert.Equal(1, removed);
    }

    [Fact]
    public void MarkNeedsBuildDefersAndCoalescesBuilderUpdates()
    {
        using var harness = new Harness();
        int builds = 0;
        int value = 1;
        var controller = new ContextMenuController();
        controller.Show(harness.Context, _ =>
        {
            builds++;
            return new Text($"Value: {value}");
        });
        Assert.Equal(0, builds);
        harness.Pump();
        Assert.Equal(1, builds);
        value = 2;
        controller.MarkNeedsBuild();
        controller.MarkNeedsBuild();
        Assert.Equal(1, builds);
        harness.Pump();
        Assert.Equal(2, builds);
        Assert.NotNull(harness.FindText("Value: 2"));
    }

    [Fact]
    public void RemovalCallbackRunsAfterEntryRemovalBeforeIsShownClearsAndOnlyOnce()
    {
        using var harness = new Harness();
        int callbacks = 0;
        ContextMenuController? controller = null;
        controller = new ContextMenuController(() =>
        {
            callbacks++;
            Assert.True(controller!.IsShown);
            Assert.Single(harness.Overlay.Entries);
        });
        controller.Show(harness.Context, _ => new SizedBox());
        ContextMenuController.RemoveAny();
        Assert.False(controller.IsShown);
        controller.Remove();
        ContextMenuController.RemoveAny();
        Assert.Equal(1, callbacks);
    }

    [Fact]
    public void RootOverlayOwnsTheMenuAndBuilderUsesItsInheritedThemes()
    {
        var nestedKey = new LabeledGlobalKey<OverlayState>("nested");
        BuildContext? nestedContext = null;
        using var harness = new Harness(new DefaultTextStyle(
            new TextStyle(FontSize: 47),
            new Overlay(key: nestedKey, initialEntries:
            [
                new OverlayEntry(context =>
                {
                    nestedContext = context;
                    return new SizedBox();
                }),
            ])));
        var controller = new ContextMenuController();
        BuildContext? menuContext = null;
        double? inheritedFontSize = null;
        controller.Show(nestedContext!, context =>
        {
            menuContext = context;
            inheritedFontSize = DefaultTextStyle.Of(context).FontSize;
            return new Positioned(left: 30, top: 40, width: 120, height: 25, child: new Text("menu"));
        });
        harness.Pump();
        Assert.Equal(2, harness.Overlay.Entries.Count);
        Assert.Single(nestedKey.CurrentState!.Entries);
        Assert.Same(harness.Overlay, Overlay.Of(menuContext!));
        // The pinned source captures from the entry builder, not from the Show caller.
        Assert.Equal(19, inheritedFontSize);
        RenderParagraph paragraph = harness.FindText("menu")!;
        Assert.Equal(new Size(120, 25), paragraph.Size);
        Assert.Equal(new Point(30, 40), paragraph.GetPaintOffsetToRoot());
        OverlayEntry entry = harness.Overlay.Entries.Last();
        Assert.False(entry.Opaque);
        Assert.False(entry.MaintainState);
        Assert.False(entry.CanSizeOverlay);
    }

    [Fact]
    public void MenuDoesNotPushARouteTakeFocusOrInterceptOutsidePointerEvents()
    {
        using var focus = new FocusNode();
        int taps = 0;
        BuildContext? context = null;
        var route = new BuilderPageRoute(localContext =>
        {
            context = localContext;
            return new Focus(focusNode: focus, child: new GestureDetector(
                behavior: HitTestBehavior.Opaque,
                onTap: () => taps++,
                child: SizedBox.Expand()));
        });
        using var harness = new Harness(new Navigator(route));
        focus.RequestFocus();
        harness.Pump();
        NavigatorState navigator = Navigator.Of(context!);
        int entries = navigator.Overlay!.Entries.Count;
        var controller = new ContextMenuController();
        controller.Show(context!, _ => new Positioned(
            left: 200, top: 100, width: 100, height: 40, child: new Text("menu")));
        harness.Pump();
        Assert.True(focus.HasFocus);
        Assert.Same(route, navigator.CurrentRoute);
        Assert.Equal(entries, navigator.Overlay.Entries.Count);
        DateTime time = DateTime.UtcNow;
        GestureBinding.Instance.HandlePointerEvent(harness.View, new PointerDownEvent(
            1, PointerDeviceKind.Mouse, new Point(10, 10), PointerButtons.Primary, time));
        GestureBinding.Instance.HandlePointerEvent(harness.View, new PointerUpEvent(
            1, PointerDeviceKind.Mouse, new Point(10, 10), PointerButtons.None, time.AddMilliseconds(10)));
        Assert.Equal(1, taps);
        Assert.True(controller.IsShown);
    }

    [Fact]
    public void DirectAndEditableMenusReplaceEachOtherAndReportVisibility()
    {
        using var text = new TextEditingController("select me");
        using var focus = new FocusNode();
        var key = new LabeledGlobalKey<EditableText.EditableTextState>("editable");
        using var harness = new Harness(new EditableText(
            text, focusNode: focus, key: key,
            selectionControls: CupertinoTextSelectionHandleControls.Instance,
            contextMenuBuilder: (_, _) => new Text("built-in")));
        focus.RequestFocus();
        harness.Pump();
        EditableText.EditableTextState state = key.CurrentState!;
        Assert.True(state.ShowToolbar());
        harness.Pump();
        Assert.True(state.ContextMenuIsVisible);
        Assert.NotNull(harness.FindText("built-in"));
        var controller = new ContextMenuController();
        controller.Show(harness.Context, _ => new Text("direct"));
        harness.Pump();
        Assert.False(state.ContextMenuIsVisible);
        Assert.Null(harness.FindText("built-in"));
        Assert.NotNull(harness.FindText("direct"));
        Assert.True(state.ShowToolbar());
        harness.Pump();
        Assert.False(controller.IsShown);
        Assert.NotNull(harness.FindText("built-in"));
        Assert.Null(harness.FindText("direct"));
        controller.Remove();
        Assert.True(state.ContextMenuIsVisible);
        state.HideToolbar();
        harness.Pump();
        Assert.False(state.ContextMenuIsVisible);
        Assert.Null(harness.FindText("built-in"));
    }

    [Fact]
    public void MenuBuildsInsideTheNavigatorRootOverlay()
    {
        using var harness = new Harness(navigatorRoot: true);
        var controller = new ContextMenuController();
        controller.Show(harness.Context, _ => new Positioned(
            right: 16, top: 80, width: 260,
            child: new Plumix.Material.Material(
                elevation: 8,
                child: new Text("navigator menu"))));
        harness.Pump();
        Assert.NotNull(harness.FindText("navigator menu"));
    }

    [Fact]
    public void WidgetHostLeavesTheRootOverlayInsideApplicationThemeAndLocalizations()
    {
        BuildContext? context = null;
        var theme = new Plumix.Material.ThemeData(primaryColor: Avalonia.Media.Colors.Red);
        var host = new WidgetHost
        {
            RootWidget = new Plumix.Material.MaterialApp(
                theme: theme,
                locale: new Locale("en", "US"),
                home: new Builder(localContext =>
                {
                    context = localContext;
                    return new SizedBox();
                })),
        };
        try
        {
            OverlayState rootOverlay = Overlay.Of(context!, rootOverlay: true);
            Assert.Same(Navigator.Of(context!).Overlay, rootOverlay);
            Assert.Equal(TextDirection.Ltr, Directionality.Of(rootOverlay.Context));
            Assert.Equal(theme.PrimaryColor, Plumix.Material.Theme.Of(rootOverlay.Context).PrimaryColor);
            Assert.Equal(new Locale("en", "US"), Localizations.LocaleOf(rootOverlay.Context));
        }
        finally
        {
            host.RootWidget = null;
        }
    }

    [DebugOnlyFact]
    public void MissingOverlayReportsTheRequiredWidgetAndRemovesPreviousMenu()
    {
        using var harness = new Harness();
        BuildContext? missingContext = null;
        using var bare = new FocusLayoutHarness(new Builder(context =>
        {
            missingContext = context;
            return new SizedBox();
        }));
        var controller = new ContextMenuController();
        controller.Show(harness.Context, _ => new SizedBox());
        var next = new ContextMenuController();
        var required = new Text("requires overlay");
        FlutterError error = Assert.Throws<FlutterError>(() =>
            next.Show(missingContext!, _ => new SizedBox(), debugRequiredFor: required));
        Assert.Contains("No Overlay widget found", error.ToString());
        Assert.Contains("Text widgets require an Overlay", error.ToString());
        Assert.Contains(error.Diagnostics, node =>
            node is DiagnosticsProperty<Widget> property && ReferenceEquals(property.Value, required));
        Assert.False(controller.IsShown);
        Assert.False(next.IsShown);
    }

    [DebugOnlyFact]
    public void MarkNeedsBuildRequiresThisInstanceToBeShown()
    {
        using var harness = new Harness();
        var controller = new ContextMenuController();
        Assert.Throws<AssertionError>(controller.MarkNeedsBuild);
        var shown = new ContextMenuController();
        shown.Show(harness.Context, _ => new SizedBox());
        Assert.Throws<AssertionError>(controller.MarkNeedsBuild);
    }

    private sealed class StatefulMenu(int value) : StatefulWidget
    {
        public int Value { get; } = value;
        public override State CreateState() => new MenuState();

        private sealed class MenuState : State
        {
            private int _initial;
            public override void InitState()
            {
                base.InitState();
                _initial = ((StatefulMenu)StateWidget).Value;
            }
            public override Widget Build(BuildContext context) =>
                new Text($"Initial: {_initial}, Current: {((StatefulMenu)StateWidget).Value}");
        }
    }

    private sealed class Harness : IDisposable
    {
        private readonly FocusLayoutHarness _harness;
        public BuildContext Context { get; private set; } = null!;
        public OverlayState Overlay => Widgets.Overlay.Of(Context);
        public RenderView View => _harness.RenderView;

        public Harness(Widget? body = null, bool navigatorRoot = false)
        {
            Widget BuildBody(BuildContext context)
            {
                Context = context;
                return body ?? new SizedBox();
            }
            Widget root = navigatorRoot
                ? new Navigator(new BuilderPageRoute(BuildBody))
                : new Overlay(initialEntries: [new OverlayEntry(BuildBody)]);
            _harness = new FocusLayoutHarness(new Directionality(TextDirection.Ltr,
                new MediaQuery(new MediaQueryData(Size: ViewSize),
                    new DefaultTextStyle(new TextStyle(FontSize: 19), root))));
            Pump();
        }

        public void Pump() => _harness.Layout(ViewSize);
        public RenderParagraph? FindText(string text) =>
            OverlayVisibility.FindOnstage<RenderParagraph>(View, node => node.PlainText == text);

        public void Dispose()
        {
            ContextMenuController.RemoveAny();
            _harness.Dispose();
        }
    }
}
