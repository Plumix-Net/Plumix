using Avalonia;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources: the SystemContextMenu tests of material_ui/test/text_field_test.dart,
// text_form_field_test.dart, search_anchor_test.dart, selectable_text_test.dart and of
// cupertino_ui/test/text_field_test.dart and text_form_field_row_test.dart: the design libraries'
// default context menu builders show the system context menu on iOS when it is supported.
public sealed class SystemContextMenuFieldsDartParityTests : IDisposable
{
    public SystemContextMenuFieldsDartParityTests()
    {
        FocusManager.Instance.ResetForTests();
    }

    public void Dispose()
    {
        PlatformDefaults.DebugTargetPlatformOverride = null;
        FocusManager.Instance.ResetForTests();
    }

    public static TheoryData<TargetPlatform> AllPlatforms => new(Enum.GetValues<TargetPlatform>());

    public static TheoryData<string> ReadOnlyFields => new("TextField", "TextFormField", "SearchBar",
        "CupertinoTextField", "CupertinoTextFormFieldRow");

    private static FrameworkDartTester CreateTester(TargetPlatform platform)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;

        // Dart sets the test value and then pumps `View(view: tester.view)` with `wrapWithView:
        // false`; that view differs from the bootstrap root's, so `MediaQuery.fromView` reads the
        // value again. Plumix's tester view is the bootstrap view itself, so the value is set before
        // the tester builds its root; the tester's teardown resets it.
        TestPlatformDispatcher.Instance.SupportsShowingSystemContextMenu = true;
        return new FrameworkDartTester(fakeGestureTimers: true);
    }

    private static Point TextOffsetToPosition(FrameworkDartTester tester, int offset)
    {
        RenderEditable editable = tester.State<EditableText.EditableTextState>().RenderEditableObject;
        IReadOnlyList<TextSelectionPoint> endpoints =
            editable.GetEndpointsForSelection(TextSelection.Collapsed(offset));
        Assert.Single(endpoints);
        Point global = editable.LocalToGlobal(endpoints[0].Point);
        return new Point(global.X, global.Y - 2.0);
    }

    // Double tap to select a word and show the menu.
    private static void DoubleTapAt(FrameworkDartTester tester, int offset, TimeSpan between, TimeSpan after)
    {
        Point position = TextOffsetToPosition(tester, offset);
        tester.TapAt(position);
        tester.Pump(between);
        tester.TapAt(position);
        tester.Pump(after);
    }

    private static void AssertSystemMenuAfterDoubleTap(FrameworkDartTester tester, Finder toolbar)
    {
        Finds.Nothing(toolbar);
        Finds.Nothing(Find.ByType<SystemContextMenu>());

        DoubleTapAt(tester, 1, TimeSpan.FromMilliseconds(50), SelectionOverlay.FadeDuration);

        Finds.Nothing(toolbar);
        Finds.OneWidget(Find.ByType<SystemContextMenu>());
    }

    [Fact]
    public void MaterialTextFieldUsesTheSystemContextMenuByDefaultIfSupported()
    {
        using FrameworkDartTester tester = CreateTester(TargetPlatform.IOS);
        tester.PumpWidget(new MaterialApp(home: new Material.Material(
            child: new TextField(controller: new TextEditingController("one two three")))));

        AssertSystemMenuAfterDoubleTap(tester, Find.ByType<AdaptiveTextSelectionToolbar>());
    }

    [Fact]
    public void MaterialTextFormFieldUsesTheSystemContextMenuByDefaultIfSupported()
    {
        using FrameworkDartTester tester = CreateTester(TargetPlatform.IOS);
        tester.PumpWidget(new MaterialApp(home: new Material.Material(
            child: new TextFormField(controller: new TextEditingController("one two three")))));

        AssertSystemMenuAfterDoubleTap(tester, Find.ByType<AdaptiveTextSelectionToolbar>());
    }

    [Fact]
    public void MaterialSearchBarUsesTheSystemContextMenuByDefaultIfSupported()
    {
        using FrameworkDartTester tester = CreateTester(TargetPlatform.IOS);
        tester.PumpWidget(new MaterialApp(home: new Material.Material(
            child: new SearchBar(controller: new TextEditingController("one two three")))));

        AssertSystemMenuAfterDoubleTap(tester, Find.ByType<AdaptiveTextSelectionToolbar>());
    }

    [Fact]
    public void CupertinoTextFieldUsesTheSystemContextMenuByDefaultIfSupported()
    {
        using FrameworkDartTester tester = CreateTester(TargetPlatform.IOS);
        tester.PumpWidget(new CupertinoApp(
            home: new CupertinoTextField(controller: new TextEditingController("one two three"))));

        AssertSystemMenuAfterDoubleTap(tester, Find.ByType<CupertinoAdaptiveTextSelectionToolbar>());
    }

    [Fact]
    public void CupertinoTextFormFieldRowUsesTheSystemContextMenuByDefaultIfSupported()
    {
        using FrameworkDartTester tester = CreateTester(TargetPlatform.IOS);
        tester.PumpWidget(new CupertinoApp(home: new Center(
            child: new CupertinoTextFormFieldRow(controller: new TextEditingController("one two three")))));

        AssertSystemMenuAfterDoubleTap(tester, Find.ByType<CupertinoAdaptiveTextSelectionToolbar>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IOSSystemContextMenuDoesNotHideSelectionHandlesOnSystemHide(bool cupertino)
    {
        using FrameworkDartTester tester = CreateTester(TargetPlatform.IOS);
        var controller = new TextEditingController("one two three");
        tester.PumpWidget(cupertino
            ? new CupertinoApp(home: new CupertinoTextField(controller: controller))
            : new MaterialApp(home: new Material.Material(child: new TextField(controller: controller))));

        DoubleTapAt(tester, 1, TimeSpan.FromMilliseconds(50), SelectionOverlay.FadeDuration);
        Finds.OneWidget(Find.ByType<SystemContextMenu>());

        SystemContextMenuDartParityTests.SendDismiss();
        tester.PumpAndSettle();

        Finds.Nothing(Find.ByType<SystemContextMenu>());
        Finds.NWidgets(
            Find.Descendant(Find.ByType<SelectionHandleOverlay>(), Find.ByType<CustomPaint>()),
            2);
    }

    [Theory]
    [MemberData(nameof(ReadOnlyFields))]
    public void ReadOnlyDisallowsSystemContextMenu(string field)
    {
        foreach (TargetPlatform platform in Enum.GetValues<TargetPlatform>())
        {
            AssertReadOnlyDisallowsSystemContextMenu(field, platform);
        }
    }

    // Regression test for https://github.com/flutter/flutter/issues/170521.
    private static void AssertReadOnlyDisallowsSystemContextMenu(string field, TargetPlatform platform)
    {
        FocusManager.Instance.ResetForTests();
        using FrameworkDartTester tester = CreateTester(platform);
        var controller = new TextEditingController("abcdefghijklmnopqr");
        bool readOnly = true;
        bool cupertino = field.StartsWith("Cupertino", StringComparison.Ordinal);
        StateSetter? setState = null;
        Widget BuildField() => field switch
        {
            "TextField" => new TextField(controller: controller, readOnly: readOnly),
            "TextFormField" => new TextFormField(controller: controller, readOnly: readOnly),
            "SearchBar" => new SearchBar(controller: controller, readOnly: readOnly),
            "CupertinoTextField" => new CupertinoTextField(controller: controller, readOnly: readOnly),
            _ => new CupertinoTextFormFieldRow(controller: controller, readOnly: readOnly),
        };
        var builder = new StatefulBuilder((context, localSetState) =>
        {
            setState = localSetState;
            return BuildField();
        });
        tester.PumpWidget(cupertino
            ? new CupertinoApp(home: new Center(child: builder))
            : new MaterialApp(home: new Material.Material(child: builder)));

        TimeSpan waitDuration = SelectionOverlay.FadeDuration > GestureConstants.DoubleTapTimeout
            ? SelectionOverlay.FadeDuration
            : GestureConstants.DoubleTapTimeout;
        Finder toolbar = cupertino
            ? Find.ByType<CupertinoAdaptiveTextSelectionToolbar>()
            : Find.ByType<AdaptiveTextSelectionToolbar>();

        DoubleTapAt(tester, 5, GestureConstants.DoubleTapTimeout / 2, waitDuration);
        Assert.Null(tester.TakeException());
        Finds.OneWidget(toolbar);
        Finds.Nothing(Find.ByType<SystemContextMenu>());

        setState!(() => readOnly = false);
        tester.Pump();

        // Close the menu and reset selection.
        tester.Tap(Find.Text("Copy"));
        tester.Pump(waitDuration);
        Finds.Nothing(toolbar);
        Finds.Nothing(Find.ByType<SystemContextMenu>());

        DoubleTapAt(tester, 5, GestureConstants.DoubleTapTimeout / 2, waitDuration);
        if (platform == TargetPlatform.IOS)
        {
            Finds.Nothing(toolbar);
            Finds.OneWidget(Find.ByType<SystemContextMenu>());
        }
        else
        {
            Finds.OneWidget(toolbar);
            Finds.Nothing(Find.ByType<SystemContextMenu>());
        }
    }

    [DebugOnlyFact]
    public void WhenSupportsShowingSystemContextMenuIsFalseSystemContextMenuThrows()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(new Builder(context => new MediaQuery(
            data: MediaQuery.Of(context).CopyWith(supportsShowingSystemContextMenu: false),
            child: new MaterialApp(home: new Scaffold(body: new Center(child: new TextField(
                controller: new TextEditingController("one two three"),
                contextMenuBuilder: (_, state) => SystemContextMenu.EditableText(state))))))));

        Finds.Nothing(Find.ByType<SystemContextMenu>());

        tester.Tap(Find.ByType<TextField>());
        Assert.True(tester.State<EditableText.EditableTextState>().ShowToolbar());
        tester.Pump();

        Assert.IsType<AssertionError>(tester.TakeException());
    }

    [Fact]
    public void SelectableTextDoesNotUseTheSystemContextMenuByDefaultEvenWhenSupported()
    {
        // Regression test for https://github.com/flutter/flutter/issues/169001.
        using FrameworkDartTester tester = CreateTester(TargetPlatform.IOS);
        tester.PumpWidget(new MaterialApp(home: new Material.Material(child: new SelectableText("one two three"))));

        Finds.Nothing(Find.ByType<CupertinoTextSelectionToolbar>());
        Finds.Nothing(Find.ByType<SystemContextMenu>());

        DoubleTapAt(tester, 1, TimeSpan.FromMilliseconds(50), SelectionOverlay.FadeDuration);

        Finds.OneWidget(Find.ByType<CupertinoTextSelectionToolbar>());
        Finds.Nothing(Find.ByType<SystemContextMenu>());
    }
}
