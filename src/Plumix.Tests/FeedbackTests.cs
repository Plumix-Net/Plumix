using System.Collections;
using Avalonia;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/test/widgets/feedback_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class FeedbackTests
{
    [Theory]
    [InlineData(TargetPlatform.Android, true)]
    [InlineData(TargetPlatform.Fuchsia, true)]
    [InlineData(TargetPlatform.IOS, false)]
    [InlineData(TargetPlatform.Linux, false)]
    [InlineData(TargetPlatform.MacOS, false)]
    [InlineData(TargetPlatform.Windows, false)]
    public async Task ForTap_SendsSemanticsAndPlatformSpecificSound(TargetPlatform platform, bool click)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        try
        {
            using var platformCalls = new MockMethodCallHandler(SystemChannels.Platform);
            using var accessibility = new AccessibilityRecorder();
            using var harness = BuildHarness(out BuildContext context);

            await Feedback.ForTap(context);

            AssertEvent(accessibility, "tap");
            Assert.Equal(click ? ["SystemSound.play"] : [], platformCalls.Methods);
            if (click)
            {
                Assert.Equal("SystemSoundType.click", platformCalls.Log[0].Arguments);
            }
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = null;
        }
    }

    [Theory]
    [InlineData(TargetPlatform.Android, "vibrate")]
    [InlineData(TargetPlatform.Fuchsia, "vibrate")]
    [InlineData(TargetPlatform.IOS, "ios")]
    [InlineData(TargetPlatform.Linux, "none")]
    [InlineData(TargetPlatform.MacOS, "none")]
    [InlineData(TargetPlatform.Windows, "none")]
    public async Task ForLongPress_SendsSemanticsAndPlatformSpecificFeedback(
        TargetPlatform platform,
        string expected)
    {
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        try
        {
            using var platformCalls = new MockMethodCallHandler(SystemChannels.Platform);
            using var accessibility = new AccessibilityRecorder();
            using var harness = BuildHarness(out BuildContext context);

            await Feedback.ForLongPress(context);

            AssertEvent(accessibility, "longPress");
            switch (expected)
            {
                case "vibrate":
                    Assert.Equal(["HapticFeedback.vibrate"], platformCalls.Methods);
                    Assert.Null(platformCalls.Log[0].Arguments);
                    break;
                case "ios":
                    Assert.Equal(["SystemSound.play", "HapticFeedback.vibrate"], platformCalls.Methods);
                    Assert.Equal("SystemSoundType.click", platformCalls.Log[0].Arguments);
                    Assert.Equal("HapticFeedbackType.heavyImpact", platformCalls.Log[1].Arguments);
                    break;
                default:
                    Assert.Empty(platformCalls.Log);
                    break;
            }
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = null;
        }
    }

    [Fact]
    public void Wrappers_KeepNullAndTriggerFeedbackBeforeCallback()
    {
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
        try
        {
            using var platformCalls = new MockMethodCallHandler(SystemChannels.Platform);
            using var accessibility = new AccessibilityRecorder();
            using var harness = BuildHarness(out BuildContext context);

            Assert.Null(Feedback.WrapForTap(null, context));
            Assert.Null(Feedback.WrapForLongPress(null, context));
            Assert.Empty(platformCalls.Log);

            int callbackCount = 0;
            Action tap = Feedback.WrapForTap(() =>
            {
                AssertEvent(accessibility, "tap");
                Assert.Equal("SystemSound.play", platformCalls.Methods[0]);
                callbackCount++;
            }, context)!;
            Action longPress = Feedback.WrapForLongPress(() =>
            {
                Assert.Equal("longPress", accessibility.Events[1]["type"]);
                Assert.Equal("HapticFeedback.vibrate", platformCalls.Methods[1]);
                callbackCount++;
            }, context)!;

            tap();
            longPress();

            Assert.Equal(2, callbackCount);
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = null;
        }
    }

    private static FocusLayoutHarness BuildHarness(out BuildContext context)
    {
        BuildContext? captured = null;
        var harness = new FocusLayoutHarness(new Semantics(
            container: true,
            label: "Feedback target",
            textDirection: TextDirection.Ltr,
            child: new Builder(buildContext =>
            {
                captured = buildContext;
                return new SizedBox(width: 40, height: 40);
            })));
        Assert.NotNull(harness.LayoutAndGetSemantics(new Size(100, 100)));
        context = captured!;
        Assert.NotNull(context.FindRenderObject());
        return harness;
    }

    private static void AssertEvent(AccessibilityRecorder accessibility, string type)
    {
        IDictionary eventMap = Assert.Single(accessibility.Events);
        Assert.Equal(type, eventMap["type"]);
        Assert.IsType<int>(eventMap["nodeId"]);
        Assert.Empty(Assert.IsAssignableFrom<IDictionary>(eventMap["data"]));
    }

    private sealed class AccessibilityRecorder : IDisposable
    {
        private readonly BinaryMessenger _messenger = ServicesBinding.Instance.DefaultBinaryMessenger;

        public AccessibilityRecorder()
        {
            _messenger.SetPlatformMessageHandler(SystemChannels.Accessibility.Name, message =>
            {
                Events.Add(Assert.IsAssignableFrom<IDictionary>(SystemChannels.Accessibility.Codec.DecodeMessage(
                    message)));
                return Task.FromResult<ByteData?>(null);
            });
        }

        public List<IDictionary> Events { get; } = [];

        public void Dispose() => _messenger.SetPlatformMessageHandler(SystemChannels.Accessibility.Name, null);
    }
}
