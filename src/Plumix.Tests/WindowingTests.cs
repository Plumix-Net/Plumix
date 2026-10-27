using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/_window.dart
// flutter/packages/flutter/lib/src/widgets/dialog.dart
// flutter/packages/flutter/lib/src/foundation/_features.dart
// Mirrors flutter/packages/flutter/test/widgets/windowing_test.dart (every test, the per-window-type
// tests as theories), plus the registry, manager, dialog-route and owner contracts that file leaves to
// the platform owners.

[Collection(SchedulerTestCollection.Name)]
public sealed class WindowingTests : IDisposable
{
    private static int s_nextViewId = 9100;

    private readonly bool _previousEnabled = Features.IsWindowingEnabled;
    private readonly WindowingOwner _previousOwner;

    public WindowingTests()
    {
        Features.IsWindowingEnabled = true;
        _previousOwner = WidgetsBinding.Instance.WindowingOwner;
        Features.IsWindowingEnabled = _previousEnabled;
    }

    public void Dispose()
    {
        Features.IsWindowingEnabled = true;
        WidgetsBinding.Instance.WindowingOwner = _previousOwner;
        Features.IsWindowingEnabled = _previousEnabled;
    }

    public enum WindowKind
    {
        Regular,
        Dialog,
        Tooltip,
        Popup,
        Satellite,
    }

    // isWindowingEnabled is false

    [Fact]
    public void Disabled_CreateDefaultOwner_ReturnsAWindowingOwner()
    {
        Features.IsWindowingEnabled = false;

        Assert.IsAssignableFrom<WindowingOwner>(Windowing.CreateDefaultWindowingOwner());
    }

    [Fact]
    public void Disabled_DefaultOwner_ThrowsWhenAccessingCreateRegularWindowController()
    {
        Features.IsWindowingEnabled = false;
        WindowingOwner owner = Windowing.CreateDefaultWindowingOwner();

        Assert.Throws<NotSupportedException>(() => owner.CreateRegularWindowController(
            @delegate: new RegularWindowControllerDelegate(),
            resizable: true));
    }

    [Fact]
    public void Disabled_DefaultOwner_ThrowsWhenAccessingCreateDialogWindowController()
    {
        Features.IsWindowingEnabled = false;
        WindowingOwner owner = Windowing.CreateDefaultWindowingOwner();

        Assert.Throws<NotSupportedException>(() => owner.CreateDialogWindowController(
            @delegate: new DialogWindowControllerDelegate(),
            resizable: true));
    }

    [Fact]
    public void Disabled_DefaultOwner_ThrowsNotImplementedForAnchoredAndSatelliteWindows()
    {
        Features.IsWindowingEnabled = false;
        WindowingOwner owner = Windowing.CreateDefaultWindowingOwner();
        var parent = new StubRegularWindowController(NewView());

        Exception tooltip = Assert.Throws<NotImplementedException>(() => owner.CreateTooltipWindowController(
            new TooltipWindowControllerDelegate(),
            BoxConstraints.Unbounded,
            default,
            new WindowPositioner(),
            parent));
        Assert.Throws<NotImplementedException>(() => owner.CreatePopupWindowController(
            new PopupWindowControllerDelegate(),
            BoxConstraints.Unbounded,
            default,
            new WindowPositioner(),
            parent));
        Assert.Throws<NotImplementedException>(() => owner.CreateSatelliteWindowController(
            new SatelliteWindowControllerDelegate(),
            parent,
            new WindowPositioner(),
            resizable: true));
        Assert.StartsWith("Windowing APIs are not enabled.", tooltip.Message);
    }

    [Theory]
    [InlineData(WindowKind.Regular)]
    [InlineData(WindowKind.Dialog)]
    [InlineData(WindowKind.Tooltip)]
    [InlineData(WindowKind.Popup)]
    [InlineData(WindowKind.Satellite)]
    public void Disabled_WindowWidgets_ThrowUnsupportedError(WindowKind kind)
    {
        Features.IsWindowingEnabled = false;
        // The `empty` constructors do not check the flag, so a stub can be made while disabled.
        BaseWindowController controller = NewController(kind);

        Assert.Throws<NotSupportedException>(() => NewWindow(kind, controller, new Text("Test")));
    }

    [Fact]
    public void Disabled_AccessingWindowScopeOf_ThrowsUnsupportedError()
    {
        Features.IsWindowingEnabled = false;
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(new LookupBoundary(child: new Container(key: key)));

        Assert.Throws<NotSupportedException>(() => WindowScope.Of(key.CurrentContext!));
    }

    [Fact]
    public void Disabled_BindingWindowingOwner_Throws()
    {
        Features.IsWindowingEnabled = false;

        NotSupportedException error = Assert.Throws<NotSupportedException>(
            () => WidgetsBinding.Instance.WindowingOwner);
        Assert.Equal(Windowing.WindowingDisabledErrorMessage, error.Message);
        Assert.EndsWith("issues/30701.\n", error.Message);
        Assert.Throws<NotSupportedException>(() => WidgetsBinding.Instance.WindowingOwner = new TestWindowingOwner());
    }

    [Fact]
    public void Disabled_ControllerFactoriesAndRegistryThrow()
    {
        Features.IsWindowingEnabled = false;

        Assert.Throws<NotSupportedException>(() => RegularWindowController.Create(new Size(100, 100)));
        Assert.Throws<NotSupportedException>(() => DialogWindowController.SizedToContent());
        Assert.Throws<NotSupportedException>(() => new WindowRegistry());
        Assert.Throws<NotSupportedException>(() => new RegularWindowControllerDelegate().OnWindowDestroyed());
    }

    // isWindowingEnabled is true

    [Theory]
    [InlineData(WindowKind.Regular)]
    [InlineData(WindowKind.Dialog)]
    [InlineData(WindowKind.Satellite)]
    public void Enabled_WindowWidgets_DoNotThrow(WindowKind kind)
    {
        Features.IsWindowingEnabled = true;
        using var tester = new FrameworkDartTester();
        BaseWindowController controller = NewController(kind, tester);

        tester.PumpWidget(NewWindow(kind, controller, new Container()), wrapWithView: false);

        Assert.Single(tester.ElementsOfType<WindowScope>());
        tester.PumpWidget(new SizedBox());
        controller.Dispose();
    }

    [Theory]
    [InlineData(WindowKind.Regular)]
    [InlineData(WindowKind.Dialog)]
    [InlineData(WindowKind.Tooltip)]
    [InlineData(WindowKind.Popup)]
    [InlineData(WindowKind.Satellite)]
    public void Enabled_WindowScopeOfAndMaybeOf_ReturnTheController(WindowKind kind)
    {
        Features.IsWindowingEnabled = true;
        BuildContext context = PumpWindow(kind, out FrameworkDartTester tester, out BaseWindowController controller);
        using (tester)
        {
            BaseWindowController of = WindowScope.Of(context);
            BaseWindowController? maybe = WindowScope.MaybeOf(context);

            Assert.Same(controller, of);
            Assert.Same(controller, maybe);
            Assert.IsType(ControllerType(kind), of);
            tester.PumpWidget(new SizedBox());
            controller.Dispose();
        }
    }

    [Theory]
    [InlineData(WindowKind.Regular)]
    [InlineData(WindowKind.Dialog)]
    [InlineData(WindowKind.Tooltip)]
    [InlineData(WindowKind.Popup)]
    [InlineData(WindowKind.Satellite)]
    public void Enabled_WindowScopeContentSizeOf_IsTheControllerContentSize(WindowKind kind)
    {
        Features.IsWindowingEnabled = true;
        BuildContext context = PumpWindow(kind, out FrameworkDartTester tester, out BaseWindowController controller);
        using (tester)
        {
            Assert.Equal(new Size(0, 0), WindowScope.ContentSizeOf(context));
            Assert.Equal(new Size(0, 0), WindowScope.MaybeContentSizeOf(context));
            tester.PumpWidget(new SizedBox());
            controller.Dispose();
        }
    }

    [Theory]
    [InlineData(WindowKind.Regular, "Stub Window")]
    [InlineData(WindowKind.Dialog, "Stub Window")]
    [InlineData(WindowKind.Tooltip, "")]
    [InlineData(WindowKind.Popup, "")]
    [InlineData(WindowKind.Satellite, "Stub Satellite Window")]
    public void Enabled_WindowScopeTitleOf(WindowKind kind, string expected)
    {
        Features.IsWindowingEnabled = true;
        BuildContext context = PumpWindow(kind, out FrameworkDartTester tester, out BaseWindowController controller);
        using (tester)
        {
            Assert.Equal(expected, WindowScope.TitleOf(context));
            Assert.Equal(expected, WindowScope.MaybeTitleOf(context));
            tester.PumpWidget(new SizedBox());
            controller.Dispose();
        }
    }

    [Theory]
    [InlineData(WindowKind.Regular, true)]
    [InlineData(WindowKind.Dialog, true)]
    [InlineData(WindowKind.Tooltip, false)]
    [InlineData(WindowKind.Popup, true)]
    [InlineData(WindowKind.Satellite, true)]
    public void Enabled_WindowScopeIsActivatedOf(WindowKind kind, bool expected)
    {
        Features.IsWindowingEnabled = true;
        BuildContext context = PumpWindow(kind, out FrameworkDartTester tester, out BaseWindowController controller);
        using (tester)
        {
            Assert.Equal(expected, WindowScope.IsActivatedOf(context));
            Assert.Equal(expected, WindowScope.MaybeIsActivatedOf(context));
            tester.PumpWidget(new SizedBox());
            controller.Dispose();
        }
    }

    [Theory]
    [InlineData(WindowKind.Regular)]
    [InlineData(WindowKind.Dialog)]
    [InlineData(WindowKind.Tooltip)]
    [InlineData(WindowKind.Popup)]
    [InlineData(WindowKind.Satellite)]
    public void Enabled_WindowScopeStateAccessors_AreFalseForTheStubs(WindowKind kind)
    {
        Features.IsWindowingEnabled = true;
        BuildContext context = PumpWindow(kind, out FrameworkDartTester tester, out BaseWindowController controller);
        using (tester)
        {
            Assert.False(WindowScope.IsMinimizedOf(context));
            Assert.False(WindowScope.MaybeIsMinimizedOf(context));
            Assert.False(WindowScope.IsMaximizedOf(context));
            Assert.False(WindowScope.MaybeIsMaximizedOf(context));
            Assert.False(WindowScope.IsFullscreenOf(context));
            Assert.False(WindowScope.MaybeIsFullscreenOf(context));
            tester.PumpWidget(new SizedBox());
            controller.Dispose();
        }
    }

    [DebugOnlyFact]
    public void Enabled_WindowScopeOf_WithoutAScope_ThrowsAndMaybeOfReturnsNull()
    {
        Features.IsWindowingEnabled = true;
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(new Container(key: key));

        FlutterError error = Assert.Throws<FlutterError>(() => WindowScope.Of(key.CurrentContext!));
        Assert.Contains("No WindowScope found in context.", error.ToString());
        Assert.Contains("Container widgets require a WindowScope widget ancestor.", error.ToString());
        Assert.Null(WindowScope.MaybeOf(key.CurrentContext!));
        Assert.Null(WindowScope.MaybeTitleOf(key.CurrentContext!));
        Assert.Null(WindowScope.MaybeIsActivatedOf(key.CurrentContext!));
    }

    [Fact]
    public void WindowWidget_ComposesWindowScopeOutsideTheControllersView()
    {
        Features.IsWindowingEnabled = true;
        using var tester = new FrameworkDartTester();
        var controller = new StubRegularWindowController(NewView(tester));
        FlutterView? view = null;
        BaseWindowController? scoped = null;
        tester.PumpWidget(
            new RegularWindow(controller, new Builder(context =>
            {
                view = View.Of(context);
                scoped = WindowScope.Of(context);
                return new SizedBox();
            })),
            wrapWithView: false);

        Assert.Same(controller.RootView, view);
        Assert.Same(controller, scoped);
        Element scope = tester.ElementOfType<WindowScope>();
        Assert.Contains(tester.ElementsOfType<View>(), element => IsAncestor(scope, element));
        Assert.Contains(
            RendererBinding.Instance.RenderViews,
            renderView => ReferenceEquals(renderView.FlutterView, controller.RootView));
        tester.PumpWidget(new SizedBox());
        controller.Dispose();
    }

    [Fact]
    public void WindowScope_SameControllerNotifying_RebuildsWithoutNotifyingAspectDependents()
    {
        Features.IsWindowingEnabled = true;
        using var tester = new FrameworkDartTester();
        var controller = new StubRegularWindowController(NewView(tester));
        int builds = 0;
        tester.PumpWidget(
            new RegularWindow(controller, new Builder(context =>
            {
                builds++;
                _ = WindowScope.TitleOf(context);
                return new SizedBox();
            })),
            wrapWithView: false);
        Assert.Equal(1, builds);

        controller.StubTitle = "Changed";
        controller.NotifyListeners();
        tester.Pump();

        // Dart's `updateShouldNotify` compares controller identity, and the child instance is reused.
        Assert.Equal(1, builds);
        tester.PumpWidget(new SizedBox());
        controller.Dispose();
    }

    [Fact]
    public void WindowScope_NewController_NotifiesOnlyTheAspectsThatChanged()
    {
        Features.IsWindowingEnabled = true;
        using var tester = new FrameworkDartTester();
        FlutterView view = NewView(tester);
        var first = new StubRegularWindowController(view);
        var second = new StubRegularWindowController(view) { StubTitle = "Second" };
        int titleBuilds = 0;
        int maximizedBuilds = 0;
        var child = new Column(children:
        [
            new Builder(context =>
            {
                titleBuilds++;
                _ = WindowScope.TitleOf(context);
                return new SizedBox();
            }),
            new Builder(context =>
            {
                maximizedBuilds++;
                _ = WindowScope.IsMaximizedOf(context);
                return new SizedBox();
            }),
        ]);
        tester.PumpWidget(new Directionality(TextDirection.Ltr, new WindowScope(first, child)));
        tester.PumpWidget(new Directionality(TextDirection.Ltr, new WindowScope(second, child)));

        Assert.Equal(2, titleBuilds);
        // Dart's debug-only `_debugCheckHasWindowController` registers an aspect-less dependency, so in
        // a debug build every dependent rebuilds when the controller changes.
        Assert.Equal(Constants.KDebugMode ? 2 : 1, maximizedBuilds);
        first.Dispose();
        second.Dispose();
    }

    [Fact]
    public void BaseWindowController_RootView_IsLateFinal()
    {
        var controller = new UnsetRegularWindowController();

        Assert.Throws<InvalidOperationException>(() => controller.RootView);
        controller.SetView(NewView());
        Assert.Throws<InvalidOperationException>(() => controller.SetView(NewView()));
    }

    [Fact]
    public void PopupWindowController_ActivationWalksToTheClosestRegularOrDialogAncestor()
    {
        var regular = new StubRegularWindowController(NewView()) { StubActivated = false };
        var inner = new StubPopupWindowController(NewView(), regular);
        var outer = new StubPopupWindowController(NewView(), inner);

        Assert.False(outer.IsActivated);
        outer.Activate();
        Assert.Equal(1, regular.ActivateCount);
        regular.StubActivated = true;
        Assert.True(outer.IsActivated);

        var underTooltip = new StubPopupWindowController(
            NewView(),
            new StubTooltipWindowController(NewView(), regular));
        Assert.Throws<InvalidOperationException>(() => underTooltip.Activate());
        Assert.Throws<InvalidOperationException>(() => underTooltip.IsActivated);
    }

    [Fact]
    public void Delegates_DefaultCloseRequestDestroysTheWindow()
    {
        Features.IsWindowingEnabled = true;
        var regular = new StubRegularWindowController(NewView());
        new RegularWindowControllerDelegate().OnWindowCloseRequested(regular);
        Assert.Equal(1, regular.DestroyCount);

        var dialog = new StubDialogWindowController(NewView());
        new DialogWindowControllerDelegate().OnWindowCloseRequested(dialog);
        Assert.Equal(1, dialog.DestroyCount);

        var satellite = new StubSatelliteWindowController(NewView(), regular);
        new SatelliteWindowControllerDelegate().OnWindowCloseRequested(satellite);
        Assert.Equal(1, satellite.DestroyCount);
    }

    [Fact]
    public void ControllerFactories_DelegateToTheBindingsOwner()
    {
        Features.IsWindowingEnabled = true;
        var owner = new TestWindowingOwner();
        WidgetsBinding.Instance.WindowingOwner = owner;

        RegularWindowController regular = RegularWindowController.Create(new Size(300, 200), title: "Main");
        Assert.Equal(new Size(300, 200), owner.LastSize);
        Assert.True(owner.LastResizable);
        Assert.Equal("Main", owner.LastTitle);
        Assert.IsType<RegularWindowControllerDelegate>(owner.LastRegularDelegate);

        RegularWindowController.SizedToContent(title: "Content");
        Assert.Null(owner.LastSize);
        Assert.False(owner.LastResizable);

        DialogWindowController dialog = DialogWindowController.Create(new Size(10, 20), parent: regular);
        Assert.Same(regular, dialog.Parent);
        Assert.True(owner.LastResizable);

        if (Constants.KDebugMode)
        {
            // The size-versus-constraints checks are Dart `assert`s.
            Assert.Throws<AssertionError>(() => RegularWindowController.Create(
                new Size(300, 200),
                constraints: new BoxConstraints(MaxWidth: 100)));
            Assert.Throws<AssertionError>(() => DialogWindowController.Create(
                new Size(300, 200),
                constraints: new BoxConstraints(MinHeight: 300)));
        }
    }

    [Fact]
    public void WindowRegistry_RegisterAndUnregisterNotifyInOrder()
    {
        Features.IsWindowingEnabled = true;
        var registry = new WindowRegistry();
        int notifications = 0;
        registry.AddListener(() => notifications++);
        var first = new WindowEntry(new StubRegularWindowController(NewView()), _ => new SizedBox());
        var second = new WindowEntry(new StubDialogWindowController(NewView()), _ => new SizedBox());

        registry.Register(first);
        registry.Register(second);
        Assert.Equal([first, second], registry.Windows);
        registry.Unregister(first);
        Assert.Equal([second], registry.Windows);
        // Unregistering an absent entry still notifies.
        registry.Unregister(first);

        Assert.Equal(4, notifications);
        Assert.NotSame(registry.Windows, registry.Windows);
    }

    [Fact]
    public void WindowManager_InsideAView_AnchorsTheRegisteredWindows()
    {
        Features.IsWindowingEnabled = true;
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(new WindowManager(child: new Container(key: key)));

        WindowRegistry registry = WindowRegistry.Of(key.CurrentContext!);
        Assert.Empty(tester.ElementsOfType<RegularWindow>());
        ViewAnchor anchor = (ViewAnchor)tester.ElementOfType<ViewAnchor>().Widget;
        Assert.Null(anchor.ViewWidget);

        var controller = new StubRegularWindowController(NewView(tester));
        var entry = new WindowEntry(controller, _ => new Text("Window content", textDirection: TextDirection.Ltr));
        registry.Register(entry);
        tester.Pump();

        Assert.Single(tester.ElementsOfType<RegularWindow>());
        Assert.Single(tester.ElementsWithText("Window content"));
        Assert.NotNull(key.CurrentContext);

        registry.Unregister(entry);
        tester.Pump();
        Assert.Empty(tester.ElementsOfType<RegularWindow>());
        tester.PumpWidget(new SizedBox());
        controller.Dispose();
    }

    [Fact]
    public void WindowManager_OutsideAnyView_RendersOnlyTheWindows()
    {
        Features.IsWindowingEnabled = true;
        using var tester = new FrameworkDartTester();
        var child = new Text("App", textDirection: TextDirection.Ltr);
        tester.PumpWidget(new WindowManager(child: child), wrapWithView: false);

        Assert.Single(tester.ElementsOfType<ViewCollection>());
        Assert.Empty(tester.ElementsWithText("App"));
        tester.PumpWidget(new SizedBox());
    }

    [Fact]
    public void WindowManager_WhileDisabled_ThrowsFromItsRegistry()
    {
        Features.IsWindowingEnabled = false;
        using var tester = new FrameworkDartTester();

        tester.PumpWidget(new WindowManager(child: new SizedBox()));

        Assert.IsType<NotSupportedException>(tester.TakeException());
    }

    [DebugOnlyFact]
    public void WindowRegistryOf_WithoutAManager_Throws()
    {
        Features.IsWindowingEnabled = true;
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(new Container(key: key));

        FlutterError error = Assert.Throws<FlutterError>(() => WindowRegistry.Of(key.CurrentContext!));
        Assert.Contains("No WindowRegistry found in context.", error.ToString());
        Assert.Null(WindowRegistry.MaybeOf(key.CurrentContext!));
    }

    [Fact]
    public void WidgetsApp_InsertsAWindowManagerOnlyWhenWindowingIsEnabled()
    {
        Features.IsWindowingEnabled = false;
        using (var tester = new FrameworkDartTester())
        {
            tester.PumpWidget(NewApp(new SizedBox()));
            Assert.Empty(tester.ElementsOfType<WindowManager>());
        }

        Features.IsWindowingEnabled = true;
        using (var tester = new FrameworkDartTester())
        {
            tester.PumpWidget(NewApp(new SizedBox()));
            Assert.Single(tester.ElementsOfType<WindowManager>());
            tester.PumpWidget(new SizedBox());
        }
    }

    [Fact]
    public void ShowRawDialog_WithWindowing_OpensADialogWindowAndPopDestroysIt()
    {
        Features.IsWindowingEnabled = true;
        var owner = new TestWindowingOwner();
        WidgetsBinding.Instance.WindowingOwner = owner;
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(NewApp(new SizedBox(key: key)));

        Task<string?> result = RawDialogs.ShowRawDialog<string>(
            key.CurrentContext!,
            _ => new Text("Dialog body", textDirection: TextDirection.Ltr),
            routeBuilder: (_, _) => throw new InvalidOperationException("ignored with windowing"));
        tester.Pump();

        StubDialogWindowController controller = Assert.Single(owner.Dialogs);
        Assert.Null(owner.LastSize);
        Assert.Equal("Dialog", owner.LastTitle);
        Assert.False(owner.LastResizable);
        Assert.Null(controller.Parent);
        Assert.Single(tester.ElementsOfType<DialogWindow>());
        Assert.Single(tester.ElementsWithText("Dialog body"));

        // A close request pops the route, which unregisters the window and destroys it.
        owner.LastDialogDelegate!.OnWindowCloseRequested(controller);
        tester.PumpAndSettle();

        Assert.True(result.IsCompleted);
        Assert.Null(result.Result);
        Assert.Equal(1, controller.DestroyCount);
        Assert.Empty(tester.ElementsOfType<DialogWindow>());
        tester.PumpWidget(new SizedBox());
        Assert.True(controller.IsDisposed);
    }

    [Fact]
    public void ShowRawDialog_FullscreenInsideAWindow_UsesTheParentsContentSize()
    {
        Features.IsWindowingEnabled = true;
        var owner = new TestWindowingOwner();
        WidgetsBinding.Instance.WindowingOwner = owner;
        using var tester = new FrameworkDartTester();
        var parent = new StubRegularWindowController(NewView(tester)) { StubContentSize = new Size(640, 480) };
        var key = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(new RegularWindow(parent, NewApp(new SizedBox(key: key))), wrapWithView: false);

        _ = RawDialogs.ShowRawDialog<object>(
            key.CurrentContext!,
            _ => new SizedBox(),
            fullscreenDialog: true);
        tester.Pump();

        StubDialogWindowController dialog = Assert.Single(owner.Dialogs);
        Assert.Same(parent, dialog.Parent);
        Assert.Equal(new Size(640, 480), owner.LastSize);
        Assert.True(owner.LastResizable);
        tester.PumpWidget(new SizedBox());
        parent.Dispose();
    }

    [Fact]
    public void ShowRawDialog_UnsupportedOwner_ReportsTheErrorAndFallsBackToARoute()
    {
        Features.IsWindowingEnabled = true;
        WidgetsBinding.Instance.WindowingOwner = new TestWindowingOwner { ThrowUnsupported = true };
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(NewApp(new SizedBox(key: key)));

        _ = RawDialogs.ShowRawDialog<object>(
            key.CurrentContext!,
            _ => new Text("Routed dialog", textDirection: TextDirection.Ltr));
        object? error = tester.TakeException();
        tester.PumpAndSettle();

        Assert.IsType<NotSupportedException>(error);
        Assert.Single(tester.ElementsWithText("Routed dialog"));
        Assert.Empty(tester.ElementsOfType<DialogWindow>());
    }

    [Fact]
    public void ShowRawDialog_WithoutWindowing_UsesTheRouteBuilder()
    {
        Features.IsWindowingEnabled = false;
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(NewApp(new SizedBox(key: key)));
        Route? built = null;

        _ = RawDialogs.ShowRawDialog<object>(
            key.CurrentContext!,
            _ => new Text("Built dialog", textDirection: TextDirection.Ltr),
            routeBuilder: (_, builder) => built = new RawDialogRoute<object>(
                pageBuilder: (context, _, _) => builder(context)));
        tester.PumpAndSettle();

        Assert.NotNull(built);
        Assert.Single(tester.ElementsWithText("Built dialog"));
    }

    [DebugOnlyFact]
    public void ShowRawDialog_WithoutWidgetsLocalizations_Throws()
    {
        using var tester = new FrameworkDartTester();
        var key = new LabeledGlobalKey<State>(null);
        tester.PumpWidget(new Container(key: key));

        FlutterError error = Assert.Throws<FlutterError>(
            () => { _ = RawDialogs.ShowRawDialog<object>(key.CurrentContext!, _ => new SizedBox()); });
        Assert.Contains("No WidgetsLocalizations found.", error.ToString());
        Assert.Contains("Container widgets require WidgetsLocalizations", error.ToString());
    }

    [Fact]
    public void MaterialShowDialog_WithWindowing_PopFromTheWindowsContentClosesIt()
    {
        Features.IsWindowingEnabled = true;
        var owner = new TestWindowingOwner();
        WidgetsBinding.Instance.WindowingOwner = owner;
        using var tester = new FrameworkDartTester();
        BuildContext? homeContext = null;
        tester.PumpWidget(new MaterialApp(home: new Builder(context =>
        {
            homeContext = context;
            return new SizedBox();
        })));
        BuildContext? dialogContext = null;

        Task<string?> result = MaterialDialogs.ShowDialog<string>(homeContext!, _ => new Builder(context =>
        {
            dialogContext = context;
            return new Text("Window dialog");
        }));
        tester.Pump();

        StubDialogWindowController controller = Assert.Single(owner.Dialogs);
        Assert.Single(tester.ElementsOfType<DialogWindow>());
        Assert.Single(tester.ElementsWithText("Window dialog"));
        // The dialog fills its window: no insets, no rounded shape, expanded constraints.
        DialogThemeData windowTheme = DialogTheme.Of(dialogContext!);
        Assert.Equal(EdgeInsets.Zero, windowTheme.InsetPadding);
        Assert.Equal(BoxConstraints.Expand(), windowTheme.Constraints);
        Assert.Equal(Theme.Of(homeContext!), Theme.Of(dialogContext!));

        // The window's content is outside the app's navigator; the shim forwards its pop.
        Navigator.Of(dialogContext!).Pop("closed");
        tester.PumpAndSettle();

        Assert.True(result.IsCompleted);
        Assert.Equal("closed", result.Result);
        Assert.Equal(1, controller.DestroyCount);
        Assert.Empty(tester.ElementsOfType<DialogWindow>());
    }

    [Fact]
    public void MaterialShowDialog_WithoutWindowing_PushesADialogRoute()
    {
        Features.IsWindowingEnabled = false;
        using var tester = new FrameworkDartTester();
        BuildContext? homeContext = null;
        tester.PumpWidget(new MaterialApp(home: new Builder(context =>
        {
            homeContext = context;
            return new SizedBox();
        })));

        _ = MaterialDialogs.ShowDialog<object>(homeContext!, _ => new Text("Routed"));
        tester.PumpAndSettle();

        Assert.Single(tester.ElementsWithText("Routed"));
        Assert.Contains(tester.AllElements(), element => element.Widget is ModalBarrier);
    }

    [Fact]
    public void Navigator_OnPopPageReturningFalse_KeepsTheOnlyRoute()
    {
        using var tester = new FrameworkDartTester();
        var pops = new List<object?>();
        BuildContext? pageContext = null;
#pragma warning disable CS0618
        tester.PumpWidget(new Directionality(TextDirection.Ltr, new Navigator(
            pages: [new BuilderTestPage(context =>
            {
                pageContext = context;
                return new SizedBox();
            })],
            onPopPage: (_, result) =>
            {
                pops.Add(result);
                return false;
            })));
#pragma warning restore CS0618

        Navigator.Of(pageContext!).Pop("first");
        tester.Pump();

        Assert.Equal(["first"], pops);
        Assert.NotNull(pageContext);
        Assert.Single(tester.ElementsOfType<SizedBox>());
    }

    [Fact]
    public void PlatformDispatcher_ViewsTrackAddedAndRemovedViews()
    {
        FlutterView view = NewView();
        int metricsChanges = 0;
        PlatformDispatcher dispatcher = PlatformDispatcher.Instance;
        Action? previous = dispatcher.OnMetricsChanged;
        dispatcher.OnMetricsChanged = () => metricsChanges++;
        try
        {
            dispatcher.AddView(view);
            dispatcher.AddView(view);
            Assert.Same(view, dispatcher.View(view.ViewId));
            Assert.Contains(view, dispatcher.Views);

            dispatcher.RemoveView(view);
            Assert.Null(dispatcher.View(view.ViewId));
            Assert.DoesNotContain(view, dispatcher.Views);
            Assert.Equal(2, metricsChanges);
        }
        finally
        {
            dispatcher.OnMetricsChanged = previous;
        }
    }

    private static FlutterView NewView(FrameworkDartTester? tester = null)
    {
        // Dart's `FakeView(tester.view)`: the test view's metrics under another id.
        Size size = tester?.View.FlutterView.PhysicalSize ?? new Size(800, 600);
        double ratio = tester?.View.FlutterView.DevicePixelRatio ?? 1.0;
        return new FlutterView(size, ratio, Interlocked.Increment(ref s_nextViewId));
    }

    private static Widget NewApp(Widget home)
    {
        return new WidgetsApp(
            color: Colors.Blue,
            home: home,
            pageRouteBuilder: (settings, builder) => new BuilderPageRoute(
                context => builder(context),
                settings));
    }

    private static BaseWindowController NewController(WindowKind kind, FrameworkDartTester? tester = null)
    {
        FlutterView view = NewView(tester);
        return kind switch
        {
            WindowKind.Regular => new StubRegularWindowController(view),
            WindowKind.Dialog => new StubDialogWindowController(view),
            WindowKind.Tooltip => new StubTooltipWindowController(view),
            WindowKind.Popup => new StubPopupWindowController(view),
            WindowKind.Satellite => new StubSatelliteWindowController(view),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    private static Type ControllerType(WindowKind kind) => kind switch
    {
        WindowKind.Regular => typeof(StubRegularWindowController),
        WindowKind.Dialog => typeof(StubDialogWindowController),
        WindowKind.Tooltip => typeof(StubTooltipWindowController),
        WindowKind.Popup => typeof(StubPopupWindowController),
        _ => typeof(StubSatelliteWindowController),
    };

    private static Widget NewWindow(WindowKind kind, BaseWindowController controller, Widget child)
    {
        return kind switch
        {
            WindowKind.Regular => new RegularWindow((RegularWindowController)controller, child),
            WindowKind.Dialog => new DialogWindow((DialogWindowController)controller, child),
            WindowKind.Tooltip => new TooltipWindow((TooltipWindowController)controller, child),
            WindowKind.Popup => new PopupWindow((PopupWindowController)controller, child),
            WindowKind.Satellite => new SatelliteWindow((SatelliteWindowController)controller, child),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    private static BuildContext PumpWindow(
        WindowKind kind,
        out FrameworkDartTester tester,
        out BaseWindowController controller)
    {
        tester = new FrameworkDartTester();
        controller = NewController(kind, tester);
        BuildContext? captured = null;
        tester.PumpWidget(
            NewWindow(kind, controller, new Builder(context =>
            {
                captured = context;
                return new Container();
            })),
            wrapWithView: false);
        return captured!;
    }

    private static bool IsAncestor(Element ancestor, Element descendant)
    {
        bool found = false;
        descendant.VisitAncestorElements(element =>
        {
            found = ReferenceEquals(element, ancestor);
            return !found;
        });
        return found;
    }

    private sealed class UnsetRegularWindowController : StubRegularWindowControllerBase
    {
        public void SetView(FlutterView view) => RootView = view;
    }

    private class StubRegularWindowControllerBase : RegularWindowController
    {
        public string StubTitle { get; set; } = "Stub Window";

        public bool StubActivated { get; set; } = true;

        public Size StubContentSize { get; set; } = new(0, 0);

        public int ActivateCount { get; private set; }

        public int DestroyCount { get; private set; }

        public override Size ContentSize => StubContentSize;

        public override string Title => StubTitle;

        public override bool IsActivated => StubActivated;

        public override bool IsMaximized => false;

        public override bool IsMinimized => false;

        public override bool IsFullscreen => false;

        public override void SetSize(Size size)
        {
        }

        public override void SetConstraints(BoxConstraints constraints)
        {
        }

        public override void SetTitle(string title)
        {
        }

        public override void Activate() => ActivateCount++;

        public override void SetMaximized(bool maximized)
        {
        }

        public override void SetMinimized(bool minimized)
        {
        }

        public override void SetFullscreen(bool fullscreen, Display? display = null)
        {
        }

        public override void Destroy() => DestroyCount++;
    }

    private sealed class StubRegularWindowController : StubRegularWindowControllerBase
    {
        public StubRegularWindowController(FlutterView view)
        {
            RootView = view;
        }
    }

    private sealed class StubDialogWindowController : DialogWindowController
    {
        public StubDialogWindowController(FlutterView view, BaseWindowController? parent = null)
        {
            RootView = view;
            Parent = parent;
        }

        public int DestroyCount { get; private set; }

        public bool IsDisposed { get; private set; }

        public override BaseWindowController? Parent { get; }

        public override Size ContentSize => new(0, 0);

        public override string Title => "Stub Window";

        public override bool IsActivated => true;

        public override bool IsMinimized => false;

        public override void SetSize(Size size)
        {
        }

        public override void SetConstraints(BoxConstraints constraints)
        {
        }

        public override void SetTitle(string title)
        {
        }

        public override void Activate()
        {
        }

        public override void SetMinimized(bool minimized)
        {
        }

        public override void Destroy() => DestroyCount++;

        public override void Dispose()
        {
            IsDisposed = true;
            base.Dispose();
        }
    }

    private sealed class StubTooltipWindowController : TooltipWindowController
    {
        private readonly BaseWindowController? _parent;

        public StubTooltipWindowController(FlutterView view, BaseWindowController? parent = null)
        {
            RootView = view;
            _parent = parent;
        }

        // Dart's stub returns a new stub regular controller on every read.
        public override BaseWindowController Parent => _parent ?? new StubRegularWindowController(NewView());

        public override Size ContentSize => new(0, 0);

        public override void SetConstraints(BoxConstraints constraints)
        {
        }

        public override void UpdatePosition(Rect? anchorRect = null, WindowPositioner? positioner = null)
        {
        }

        public override void Destroy()
        {
        }
    }

    private sealed class StubPopupWindowController : PopupWindowController
    {
        private readonly BaseWindowController? _parent;

        public StubPopupWindowController(FlutterView view, BaseWindowController? parent = null)
        {
            RootView = view;
            _parent = parent;
        }

        public override BaseWindowController Parent => _parent ?? new StubRegularWindowController(NewView());

        public override Size ContentSize => new(0, 0);

        public override Point OffsetFromParent => default;

        public override void SetConstraints(BoxConstraints constraints)
        {
        }

        public override void UpdatePosition(Rect? anchorRect = null, WindowPositioner? positioner = null)
        {
        }

        public override void Destroy()
        {
        }
    }

    private sealed class StubSatelliteWindowController : SatelliteWindowController
    {
        private readonly BaseWindowController? _parent;

        public StubSatelliteWindowController(FlutterView view, BaseWindowController? parent = null)
        {
            RootView = view;
            _parent = parent;
        }

        public int DestroyCount { get; private set; }

        public override BaseWindowController Parent => _parent ?? new StubRegularWindowController(NewView());

        public override Size ContentSize => new(0, 0);

        public override string Title => "Stub Satellite Window";

        public override bool IsActivated => true;

        public override void SetParent(BaseWindowController parent)
        {
        }

        public override void SetSize(Size size)
        {
        }

        public override void SetConstraints(BoxConstraints constraints)
        {
        }

        public override void SetTitle(string title)
        {
        }

        public override void Activate()
        {
        }

        public override void Destroy() => DestroyCount++;
    }

    private sealed record BuilderTestPage : Page
    {
        public BuilderTestPage(WidgetBuilder builder)
        {
            Builder = builder;
        }

        public WidgetBuilder Builder { get; }

        public override Route CreateRoute(BuildContext context) => new PageRouteBuilder(
            settings: this,
            pageBuilder: (pageContext, _, _) => Builder(pageContext),
            transitionDuration: TimeSpan.Zero,
            reverseTransitionDuration: TimeSpan.Zero);
    }

    /// <summary>A windowing owner that hands out stub controllers and records what it was asked for.</summary>
    private sealed class TestWindowingOwner : WindowingOwner
    {
        public bool ThrowUnsupported { get; init; }

        public List<StubDialogWindowController> Dialogs { get; } = [];

        public Size? LastSize { get; private set; }

        public bool LastResizable { get; private set; }

        public string? LastTitle { get; private set; }

        public RegularWindowControllerDelegate? LastRegularDelegate { get; private set; }

        public DialogWindowControllerDelegate? LastDialogDelegate { get; private set; }

        public override RegularWindowController CreateRegularWindowController(
            RegularWindowControllerDelegate @delegate,
            bool resizable,
            Size? size = null,
            BoxConstraints? constraints = null,
            string? title = null)
        {
            Record(size, resizable, title);
            LastRegularDelegate = @delegate;
            return new StubRegularWindowController(NewView());
        }

        public override DialogWindowController CreateDialogWindowController(
            DialogWindowControllerDelegate @delegate,
            bool resizable,
            Size? size = null,
            BoxConstraints? constraints = null,
            BaseWindowController? parent = null,
            string? title = null)
        {
            if (ThrowUnsupported)
            {
                throw new NotSupportedException("Windowing is unsupported on this platform.");
            }

            Record(size, resizable, title);
            LastDialogDelegate = @delegate;
            var dialog = new StubDialogWindowController(NewView(), parent);
            Dialogs.Add(dialog);
            return dialog;
        }

        public override TooltipWindowController CreateTooltipWindowController(
            TooltipWindowControllerDelegate @delegate,
            BoxConstraints constraints,
            Rect anchorRect,
            WindowPositioner positioner,
            BaseWindowController parent) => new StubTooltipWindowController(NewView(), parent);

        public override PopupWindowController CreatePopupWindowController(
            PopupWindowControllerDelegate @delegate,
            BoxConstraints constraints,
            Rect anchorRect,
            WindowPositioner positioner,
            BaseWindowController parent) => new StubPopupWindowController(NewView(), parent);

        public override SatelliteWindowController CreateSatelliteWindowController(
            SatelliteWindowControllerDelegate @delegate,
            BaseWindowController parent,
            WindowPositioner initialPositioner,
            bool resizable,
            Rect? initialAnchorRect = null,
            Size? size = null,
            BoxConstraints? constraints = null,
            string? title = null) => new StubSatelliteWindowController(NewView(), parent);

        private void Record(Size? size, bool resizable, string? title)
        {
            LastSize = size;
            LastResizable = resizable;
            LastTitle = title;
        }
    }
}
