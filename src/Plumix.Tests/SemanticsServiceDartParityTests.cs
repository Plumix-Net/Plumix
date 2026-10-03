using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Ports flutter/packages/flutter/test/semantics/semantics_service_test.dart.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class SemanticsServiceDartParityTests
{
    // Flutter: 'semantics_service_test.dart: Semantic announcement'
    [Fact]
    public async Task SemanticAnnouncement()
    {
        using var tester = new FrameworkDartTester();
        var log = new List<System.Collections.IDictionary>();
        BinaryMessenger messenger = ServicesBinding.Instance.DefaultBinaryMessenger;
        messenger.SetPlatformMessageHandler(SystemChannels.Accessibility.Name, message =>
        {
            log.Add(Assert.IsAssignableFrom<System.Collections.IDictionary>(
                SystemChannels.Accessibility.Codec.DecodeMessage(message)));
            return Task.FromResult<ByteData?>(null);
        });
        try
        {
            await SemanticsService.SendAnnouncement(tester.View, "announcement 1", TextDirection.Ltr);
            await SemanticsService.SendAnnouncement(
                tester.View,
                "announcement 2",
                TextDirection.Rtl,
                assertiveness: Assertiveness.Assertive);
        }
        finally
        {
            messenger.SetPlatformMessageHandler(SystemChannels.Accessibility.Name, null);
        }

        Assert.Equal(2, log.Count);
        AssertAnnouncement(
            log[0],
            new Dictionary<string, object?>
            {
                ["viewId"] = tester.View.ViewId,
                ["message"] = "announcement 1",
                ["textDirection"] = 1,
            });
        AssertAnnouncement(
            log[1],
            new Dictionary<string, object?>
            {
                ["viewId"] = tester.View.ViewId,
                ["message"] = "announcement 2",
                ["textDirection"] = 0,
                ["assertiveness"] = 1,
            });
    }

    // C#-only: the returned task carries the channel failure, which is what Dart's callers attach
    // `.catchError` to.
    [Fact]
    public async Task SendAnnouncementFaultsWhenTheChannelFails()
    {
        using var tester = new FrameworkDartTester();
        BinaryMessenger messenger = ServicesBinding.Instance.DefaultBinaryMessenger;
        messenger.SetPlatformMessageHandler(
            SystemChannels.Accessibility.Name,
            _ => Task.FromException<ByteData?>(new FormatException("invalid announcement response")));
        try
        {
            await Assert.ThrowsAsync<FormatException>(
                () => SemanticsService.SendAnnouncement(tester.View, "announcement", TextDirection.Ltr));
        }
        finally
        {
            messenger.SetPlatformMessageHandler(SystemChannels.Accessibility.Name, null);
        }
    }

    private static void AssertAnnouncement(
        System.Collections.IDictionary message,
        Dictionary<string, object?> expectedData)
    {
        Assert.Equal(2, message.Count);
        Assert.Equal("announce", message["type"]);
        var data = Assert.IsAssignableFrom<System.Collections.IDictionary>(message["data"]);
        Assert.Equal(expectedData.Count, data.Count);
        foreach ((string key, object? value) in expectedData)
        {
            Assert.Equal(value, data[key]);
        }
    }
}
