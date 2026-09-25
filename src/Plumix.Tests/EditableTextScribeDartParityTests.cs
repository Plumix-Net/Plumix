using Avalonia;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/test/widgets/editable_text_scribe_test.dart — Android
// stylus handwriting starts from `TextSelectionGestureDetectorBuilder.onTapDown` through `Scribe`.
public sealed class EditableTextScribeDartParityTests : IDisposable
{
    private readonly TextEditingController _controller = new("Lorem ipsum dolor sit amet");
    private readonly FocusNode _focusNode = new(debugLabel: "EditableText Node");

    public EditableTextScribeDartParityTests()
    {
        FocusManager.Instance.ResetForTests();
    }

    public void Dispose()
    {
        PlatformDefaults.DebugTargetPlatformOverride = null;
        FocusManager.Instance.ResetForTests();
    }

    private FrameworkDartTester PumpField(TargetPlatform platform = TargetPlatform.Android)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        var tester = new FrameworkDartTester(fakeGestureTimers: true);
        tester.PumpWidget(new MaterialApp(
            home: new Material.Material(
                child: new Align(
                    alignment: Alignment.TopLeft,
                    child: new SizedBox(
                        width: 400,
                        child: new TextField(controller: _controller, focusNode: _focusNode))))));
        tester.Pump();
        return tester;
    }

    private static Point FieldCenter(FrameworkDartTester tester)
    {
        EditableText.EditableTextState state = tester.State<EditableText.EditableTextState>();
        RenderEditable editable = state.RenderEditableObject;
        return editable.LocalToGlobal(new Point(20, editable.Size.Height / 2));
    }

    private static MockMethodCallHandler MockScribe(bool isFeatureAvailable) =>
        new(SystemChannels.Scribe, call => call.Method switch
        {
            "Scribe.isFeatureAvailable" => isFeatureAvailable,
            "Scribe.startStylusHandwriting" => null,
            _ => throw new InvalidOperationException($"Unexpected Scribe call {call.Method}"),
        });

    [Fact]
    public void WhenScribeIsAvailableHandwritingStartsOnTapDown()
    {
        using MockMethodCallHandler scribe = MockScribe(isFeatureAvailable: true);
        using FrameworkDartTester tester = PumpField();
        Assert.False(_focusNode.HasFocus);

        TestGesture gesture = tester.StartGesture(FieldCenter(tester), PointerDeviceKind.Stylus);
        tester.PumpAndSettle();

        Assert.Equal(["Scribe.isFeatureAvailable", "Scribe.startStylusHandwriting"], scribe.Methods);

        gesture.Up();
        tester.PumpAndSettle();
        Assert.True(_focusNode.HasFocus);
    }

    [Fact]
    public void WhenScribeIsUnavailableHandwritingDoesNotStart()
    {
        using MockMethodCallHandler scribe = MockScribe(isFeatureAvailable: false);
        using FrameworkDartTester tester = PumpField();

        TestGesture gesture = tester.StartGesture(FieldCenter(tester), PointerDeviceKind.Stylus);
        tester.PumpAndSettle();

        Assert.Equal(["Scribe.isFeatureAvailable"], scribe.Methods);
        gesture.Up();
        tester.PumpAndSettle();
    }

    [Theory]
    [InlineData(PointerDeviceKind.Mouse)]
    [InlineData(PointerDeviceKind.Touch)]
    public void TapDownMustComeFromAStylusToStartHandwriting(PointerDeviceKind kind)
    {
        using MockMethodCallHandler scribe = MockScribe(isFeatureAvailable: true);
        using FrameworkDartTester tester = PumpField();

        TestGesture gesture = tester.StartGesture(FieldCenter(tester), kind);
        tester.PumpAndSettle();

        Assert.Empty(scribe.Log);
        gesture.Up();
        tester.PumpAndSettle();
    }

    [Theory]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.Fuchsia)]
    [InlineData(TargetPlatform.Windows)]
    public void OnlyAndroidStartsScribe(TargetPlatform platform)
    {
        using MockMethodCallHandler scribe = MockScribe(isFeatureAvailable: true);
        using FrameworkDartTester tester = PumpField(platform);

        TestGesture gesture = tester.StartGesture(FieldCenter(tester), PointerDeviceKind.Stylus);
        tester.PumpAndSettle();

        Assert.Empty(scribe.Log);
        gesture.Up();
        tester.PumpAndSettle();
    }

    [Fact]
    public void StylusHandwritingDisabledDoesNotStartScribe()
    {
        using MockMethodCallHandler scribe = MockScribe(isFeatureAvailable: true);
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        using var tester = new FrameworkDartTester(fakeGestureTimers: true);
        var key = new LabeledGlobalKey<EditableText.EditableTextState>("editable");
        var builderDelegate = new FakeBuilderDelegate(key);
        var builder = new TextSelectionGestureDetectorBuilder(builderDelegate);
        tester.PumpWidget(new TestWidgetsApp(home: new Align(
            alignment: Alignment.TopLeft,
            child: new SizedBox(
                width: 400,
                child: builder.BuildGestureDetector(
                    behavior: HitTestBehavior.Translucent,
                    child: new EditableText(
                        _controller,
                        focusNode: _focusNode,
                        key: key,
                        stylusHandwritingEnabled: false,
                        maxLines: 1))))));
        tester.Pump();

        TestGesture gesture = tester.StartGesture(FieldCenter(tester), PointerDeviceKind.Stylus);
        tester.PumpAndSettle();

        Assert.Empty(scribe.Log);
        gesture.Up();
        tester.PumpAndSettle();
    }

    private sealed class FakeBuilderDelegate(GlobalKey<EditableText.EditableTextState> key)
        : ITextSelectionGestureDetectorBuilderDelegate
    {
        public GlobalKey<EditableText.EditableTextState> EditableTextKey { get; } = key;

        public bool ForcePressEnabled => true;

        public bool SelectionEnabled => true;
    }
}
