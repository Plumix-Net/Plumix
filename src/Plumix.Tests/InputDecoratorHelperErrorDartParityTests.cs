// Dart parity source: material_ui/lib/src/input_decorator.dart (_HelperError)
// Mirrors the errorText live-region test of material-ui-src/test/text_field_test.dart.

using Avalonia;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using static Plumix.Tests.SemanticsMatchers;
using MaterialWidget = Plumix.Material.Material;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class InputDecoratorHelperErrorDartParityTests : IDisposable
{
    private readonly List<OverlayEntry> _entries = [];

    public InputDecoratorHelperErrorDartParityTests()
    {
        FocusManager.Instance.ResetForTests();
        // flutter_test: defaultTargetPlatform == android.
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
    }

    public void Dispose()
    {
        PlatformDefaults.DebugTargetPlatformOverride = null;
        FocusManager.Instance.ResetForTests();
    }

    private static FrameworkDartTester CreateTester() =>
        new(fakeGestureTimers: true, semanticsEnabled: true, registerTestTextInput: true);

    // text_field_test.dart's `overlay`.
    private Widget Overlay(Widget child)
    {
        var entry = new OverlayEntry(builder: context => new Center(child: new MaterialWidget(child: child)));
        _entries.Add(entry);
        return OverlayWithEntry(entry);
    }

    // text_field_test.dart's `overlayWithEntry`.
    private static Widget OverlayWithEntry(OverlayEntry entry)
    {
        return new Localizations(
            locale: new Locale("en", "US"),
            delegates:
            [
                DefaultWidgetsLocalizations.Delegate,
                DefaultMaterialLocalizations.Delegate,
                DefaultCupertinoLocalizations.Delegate,
            ],
            child: new DefaultTextEditingShortcuts(
                child: new Directionality(
                    TextDirection.Ltr,
                    new MediaQuery(
                        data: new MediaQueryData(Size: new Size(800.0, 600.0)),
                        child: new Plumix.Widgets.Overlay(initialEntries: [entry])))));
    }

    private void TearDownEntries()
    {
        foreach (OverlayEntry entry in _entries)
        {
            entry.Remove();
            entry.Dispose();
        }

        _entries.Clear();
    }

    // Flutter: 'text_field_test.dart: InputDecoration errorText semantics (supportsAnnounce=$supportsAnnounce)'
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InputDecorationErrorTextSemantics(bool supportsAnnounce)
    {
        using FrameworkDartTester tester = CreateTester();
        var semantics = new SemanticsTester(tester);
        var controller = new TextEditingController();
        Key key = new UniqueKey();

        try
        {
            tester.PumpWidget(Overlay(
                child: new MediaQuery(
                    data: new MediaQueryData(SupportsAnnounce: supportsAnnounce),
                    child: new TextField(
                        key: key,
                        controller: controller,
                        decoration: new InputDecoration(
                            labelText: "label",
                            hintText: "hint",
                            errorText: "oh no!")))));

            // Dart's `hasSemantics(TestSemantics.root(children: [rootChild(children: [rootChild(...)])]))`
            // with ignoreTransform/ignoreRect/ignoreId.
            SemanticsNode root = semantics.RootNode;
            SemanticsNode rootChild = Assert.Single(root.Children);
            SemanticsNode field = Assert.Single(rootChild.Children);
            Assert.Same(tester.GetSemantics(Find.ByKey(key)), field);
            ExpectSemantics(
                field,
                MatchesSemantics(
                    label: "label",
                    hint: "oh no!",
                    textDirection: TextDirection.Ltr,
                    hasTapAction: true,
                    hasFocusAction: true,
                    isTextField: true,
                    isFocusable: true,
                    hasEnabledState: true,
                    isEnabled: true,
                    inputType: SemanticsInputType.Text,
                    currentValueLength: 0,
                    children:
                    [
                        MatchesSemantics(
                            label: "oh no!",
                            textDirection: TextDirection.Ltr,
                            isLiveRegion: !supportsAnnounce),
                    ]));
        }
        finally
        {
            semantics.Dispose();
            controller.Dispose();
            TearDownEntries();
        }
    }
}
