// Dart parity source: flutter/packages/flutter/lib/src/widgets/form.dart
// Mirrors the announcement tests of flutter-src/packages/flutter/test/widgets/form_test.dart.

using Plumix.Foundation;
using Plumix.Material;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using static Plumix.Tests.SemanticsMatchers;
using MaterialWidget = Plumix.Material.Material;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class FormAnnouncementDartParityTests : IDisposable
{
    public FormAnnouncementDartParityTests()
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

    // Flutter: 'form_test.dart: Should announce only the first error message when validate returns errors and
    // announce = false' (supportsAnnounce: false) and 'Should not announce error message when validate returns
    // errors and announce = true' (supportsAnnounce: true). Dart's names are swapped relative to what each
    // scenario asserts; the body is what is ported.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ShouldAnnounceOnlyTheFirstErrorMessageWhenValidateReturnsErrors(bool supportsAnnounce)
    {
        using FrameworkDartTester tester = CreateTester();
        var formKey = new LabeledGlobalKey<FormState>(null);
        tester.PumpWidget(new MaterialApp(
            home: new MediaQuery(
                data: new MediaQueryData(SupportsAnnounce: supportsAnnounce),
                child: new Directionality(
                    TextDirection.Ltr,
                    new Center(
                        child: new MaterialWidget(
                            child: new Form(
                                key: formKey,
                                child: new Column(
                                    children:
                                    [
                                        new TextFormField(validator: _ => "First error message"),
                                        new TextFormField(validator: _ => "Second error message"),
                                    ]))))))));
        formKey.CurrentState!.Reset();
        tester.EnterText(Find.ByType<TextFormField>().First, string.Empty);
        tester.Pump();

        // Manually validate.
        Finds.Nothing(Find.Text("First error message"));
        Finds.Nothing(Find.Text("Second error message"));
        formKey.CurrentState!.Validate();
        tester.Pump();
        Finds.OneWidget(Find.Text("First error message"));
        Finds.OneWidget(Find.Text("Second error message"));

        if (supportsAnnounce)
        {
            List<CapturedAccessibilityAnnouncement> announcements = tester.TakeAnnouncements();
            CapturedAccessibilityAnnouncement announcement = Assert.Single(announcements);
            ExpectAnnouncement(
                announcement,
                "First error message",
                textDirection: TextDirection.Ltr,
                assertiveness: Assertiveness.Assertive);
        }
        else
        {
            Assert.Empty(tester.TakeAnnouncements());
        }
    }

    // Flutter: 'form_test.dart: Should announce error text when validateGranularly is called'
    [Fact]
    public void ShouldAnnounceErrorTextWhenValidateGranularlyIsCalled()
    {
        using FrameworkDartTester tester = CreateTester();
        var formKey = new LabeledGlobalKey<FormState>(null);
        const string validString = "Valid string";
        string? Validator(string? s) => s == validString ? null : "error";

        Widget Builder()
        {
            return new MaterialApp(
                home: new MediaQuery(
                    data: new MediaQueryData(SupportsAnnounce: true),
                    child: new Directionality(
                        TextDirection.Ltr,
                        new Center(
                            child: new MaterialWidget(
                                child: new Form(
                                    key: formKey,
                                    child: new ListView(
                                        children:
                                        [
                                            new TextFormField(
                                                initialValue: validString,
                                                validator: Validator,
                                                autovalidateMode: AutovalidateMode.Disabled),
                                            new TextFormField(
                                                initialValue: string.Empty,
                                                validator: Validator,
                                                autovalidateMode: AutovalidateMode.Disabled),
                                        ])))))));
        }

        tester.PumpWidget(Builder());
        Finds.Nothing(Find.Text("error"));

        formKey.CurrentState!.ValidateGranularly();

        tester.Pump();
        Finds.OneWidget(Find.Text("error"));

        CapturedAccessibilityAnnouncement announcement = Assert.Single(tester.TakeAnnouncements());
        ExpectAnnouncement(
            announcement,
            "error",
            textDirection: TextDirection.Ltr,
            assertiveness: Assertiveness.Assertive);
    }

    // Flutter: 'form_test.dart: Form reports error when SemanticsService.sendAnnouncement fails during
    // validation'
    [Fact]
    public void FormReportsErrorWhenSendAnnouncementFailsDuringValidation()
    {
        using IDisposable _ = TargetPlatformVariant.Override(TargetPlatform.MacOS);
        using FrameworkDartTester tester = CreateTester();
        var formKey = new LabeledGlobalKey<FormState>(null);
        const string validString = "Valid string";
        string? Validator(string? s) => s == validString ? null : "error";
        var errors = new List<FlutterErrorDetails>();
        FlutterExceptionHandler? originalOnError = FlutterError.OnError;
        FlutterError.OnError = details =>
        {
            string contextStr = details.Context?.ToString() ?? string.Empty;
            if (contextStr.Contains("while sending semantics announcement", StringComparison.Ordinal))
            {
                errors.Add(details);
                return;
            }

            originalOnError?.Invoke(details);
        };
        try
        {
            tester.Binding.SetMockMessageHandler(
                SystemChannels.Accessibility.Name,
                message =>
                {
                    var codec = new StandardMessageCodec();
                    object? decoded = codec.DecodeMessage(message);
                    if (decoded is IDictionary<object, object?> map
                        && map.TryGetValue("type", out object? type)
                        && Equals(type, "announce"))
                    {
                        var data = new ByteData(1);
                        data.Buffer[0] = 255; // Invalid type byte
                        return Task.FromResult<ByteData?>(data);
                    }

                    return Task.FromResult<ByteData?>(null); // Success for other events
                });

            tester.PumpWidget(new MediaQuery(
                data: new MediaQueryData(SupportsAnnounce: true),
                child: new Directionality(
                    TextDirection.Ltr,
                    new Center(
                        child: new Form(
                            key: formKey,
                            child: new ListView(
                                children:
                                [
                                    new FormField<string>(
                                        initialValue: string.Empty,
                                        validator: Validator,
                                        autovalidateMode: AutovalidateMode.Disabled,
                                        builder: state => new Container()),
                                ]))))));

            formKey.CurrentState!.Validate();
            tester.Pump();

            Assert.NotEmpty(errors);
            bool hasAnnouncementError = errors.Any(e =>
                e.Exception.ToString()!.Contains("FormatException", StringComparison.Ordinal)
                && e.Context!.ToString().Contains("while sending semantics announcement", StringComparison.Ordinal));
            Assert.True(hasAnnouncementError);
        }
        finally
        {
            FlutterError.OnError = originalOnError;
            tester.Binding.SetMockMessageHandler(SystemChannels.Accessibility.Name, null);
        }
    }

    // C#-only coverage: form.dart's iOS branch (`_kIOSAnnouncementDelayDuration`) has no Dart test.
    [Fact]
    public void IOSAnnouncementWaitsForTheAnnouncementDelay()
    {
        using IDisposable _ = TargetPlatformVariant.Override(TargetPlatform.IOS);
        using FrameworkDartTester tester = CreateTester();
        var formKey = new LabeledGlobalKey<FormState>(null);
        tester.PumpWidget(new MediaQuery(
            data: new MediaQueryData(SupportsAnnounce: true),
            child: new Directionality(
                TextDirection.Ltr,
                new Form(
                    key: formKey,
                    child: new FormField<string>(
                        validator: _ => "error",
                        builder: state => new Container())))));

        formKey.CurrentState!.Validate();
        tester.Pump();
        Assert.Empty(tester.TakeAnnouncements());
        tester.Pump(TimeSpan.FromMilliseconds(999));
        Assert.Empty(tester.TakeAnnouncements());
        tester.Pump(TimeSpan.FromMilliseconds(1));
        ExpectAnnouncement(
            Assert.Single(tester.TakeAnnouncements()),
            "error",
            textDirection: TextDirection.Ltr,
            assertiveness: Assertiveness.Assertive);
    }
}
