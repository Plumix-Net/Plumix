using Plumix.UI;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: flutter/packages/flutter/test/services/clipboard_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class ClipboardTests
{
    [Fact]
    public async Task GetData_ReturnsTextAndUsesPlainTextFormat()
    {
        using var platform = new MockMethodCallHandler(
            SystemChannels.Platform,
            _ => new Dictionary<string, object?> { ["text"] = "Hello world" });

        ClipboardData? data = await Clipboard.GetData(Clipboard.KTextPlain);

        Assert.Equal("Hello world", data?.Text);
        Assert.Equal("Clipboard.getData", platform.Log[0].Method);
        Assert.Equal(Clipboard.KTextPlain, platform.Log[0].Arguments);
    }

    [Fact]
    public async Task GetData_ReturnsNullWhenPlatformHasNoData()
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);

        Assert.Null(await Clipboard.GetData(Clipboard.KTextPlain));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetData_RejectsMissingOrNullText(bool includeText)
    {
        using var platform = new MockMethodCallHandler(
            SystemChannels.Platform,
            _ => includeText
                ? new Dictionary<string, object?> { ["text"] = null }
                : new Dictionary<string, object?>());

        await Assert.ThrowsAsync<InvalidCastException>(() => Clipboard.GetData(Clipboard.KTextPlain));
    }

    [Fact]
    public async Task SetData_SendsTextMap()
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);

        await Clipboard.SetData(new ClipboardData("Hello world"));

        Assert.Equal("Clipboard.setData", platform.Log[0].Method);
        var data = Assert.IsAssignableFrom<System.Collections.IDictionary>(platform.Log[0].Arguments);
        Assert.Equal("Hello world", data["text"]);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task HasStrings_UsesPlatformResult(bool? available, bool expected)
    {
        using var platform = new MockMethodCallHandler(
            SystemChannels.Platform,
            _ => available is null ? null : new Dictionary<string, object?> { ["value"] = available.Value });

        Assert.Equal(expected, await Clipboard.HasStrings());
        Assert.Equal("Clipboard.hasStrings", platform.Log[0].Method);
        Assert.Equal(Clipboard.KTextPlain, platform.Log[0].Arguments);
    }
}
