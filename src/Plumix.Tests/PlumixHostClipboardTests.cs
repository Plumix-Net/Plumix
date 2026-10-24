using Avalonia.Input;
using Xunit;

namespace Plumix.Tests;

// C#-only infrastructure: the host's answer to `Clipboard.hasStrings`, which the engine gives from the
// pasteboard's types (`UIPasteboard.hasStrings`, `ClipboardManager.hasPrimaryClip`) without reading it.
public sealed class PlumixHostClipboardTests
{
    [Fact]
    public void HasStrings_AnswersFromFormats_WithoutReadingTheItems()
    {
        // Reading the items is what shows iOS's paste-permission alert; the fake throws on it.
        Assert.True(PlumixHost.OffersStrings(new FakeDataTransfer([DataFormat.Text])));
        Assert.True(PlumixHost.OffersStrings(new FakeDataTransfer([DataFormat.Bitmap, DataFormat.Text])));
        Assert.False(PlumixHost.OffersStrings(new FakeDataTransfer([DataFormat.Bitmap, DataFormat.File])));
        Assert.False(PlumixHost.OffersStrings(new FakeDataTransfer([])));
        Assert.False(PlumixHost.OffersStrings(null));
    }

    private sealed class FakeDataTransfer(IReadOnlyList<DataFormat> formats) : IAsyncDataTransfer
    {
        public IReadOnlyList<DataFormat> Formats => formats;

        public IReadOnlyList<IAsyncDataTransferItem> Items =>
            throw new InvalidOperationException("Reading clipboard items shows iOS's paste-permission alert.");

        public void Dispose()
        {
        }
    }
}
