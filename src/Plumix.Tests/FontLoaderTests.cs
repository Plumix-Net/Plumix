using Plumix.UI;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/services/font_loader_test.dart

namespace Plumix.Tests;

public sealed class FontLoaderTests
{
    private sealed class TestFontLoader(string family) : FontLoader(family)
    {
        public List<byte[]> FontAssets { get; } = [];

        protected override Task LoadFont(ReadOnlyMemory<byte> list, string family)
        {
            FontAssets.Add(list.ToArray());
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task FontLoaderTest()
    {
        var loader = new TestFontLoader("TestFamily");
        byte[][] expectedAssets = [[100], [10, 20, 30], [200]];
        foreach (byte[] asset in expectedAssets)
        {
            loader.AddFont(Task.FromResult<ReadOnlyMemory<byte>>(asset));
        }

        await loader.Load();

        Assert.Equal(expectedAssets, loader.FontAssets);
    }

    [Fact]
    public async Task FontLoaderLoadsFontsInOrder()
    {
        var loader = new TestFontLoader("TestFamily");
        byte[][] expectedAssets = [[1], [2]];
        var completer = new TaskCompletionSource();

        // Deliberately delay the load of the first font.
        loader.AddFont(DelayedAsset(completer.Task, expectedAssets[0]));
        loader.AddFont(Task.FromResult<ReadOnlyMemory<byte>>(expectedAssets[1]));

        Task fontLoadComplete = loader.Load();
        completer.SetResult();
        await fontLoadComplete;

        Assert.Equal(expectedAssets, loader.FontAssets);

        static async Task<ReadOnlyMemory<byte>> DelayedAsset(Task gate, byte[] asset)
        {
            await gate;
            return asset;
        }
    }

    [Fact]
    public async Task LoadingTwiceThrows()
    {
        var loader = new TestFontLoader("TestFamily");
        await loader.Load();

        await Assert.ThrowsAsync<InvalidOperationException>(() => loader.Load());
        Assert.Throws<InvalidOperationException>(() => { loader.AddFont(new byte[] { 1 }); });
    }

    [Fact]
    public void LoadedFamily_ResolvesFromTheRuntimeCollection()
    {
        string family = TestFonts.Ahem;

        Assert.True(AvaloniaFonts.IsRegistered(family));
        Assert.Equal("fonts:Plumix#Ahem", AvaloniaFonts.Resolve(family).ToString());
        Avalonia.Media.FontFamily withFallback = AvaloniaFonts.Resolve(family, ["Unregistered"]);
        Assert.Equal(["Ahem", "Unregistered"], withFallback.FamilyNames);
    }

    [Fact]
    public async Task LoadFontFromList_RejectsDataThatIsNotAFont()
    {
        _ = TestFonts.Ahem; // the collection exists, so bad data is reported right away
        await Assert.ThrowsAsync<ArgumentException>(
            () => PlatformFonts.LoadFontFromList(new byte[] { 1, 2, 3 }, "Broken"));
    }

    [Fact]
    public void LoadedFamily_FallsBackToTheNearestFaceForAMissingWeight()
    {
        string family = TestFonts.Ahem;
        var typeface = new Avalonia.Media.Typeface(
            AvaloniaFonts.Resolve(family),
            Avalonia.Media.FontStyle.Normal,
            Avalonia.Media.FontWeight.ExtraBold);

        Assert.True(Avalonia.Media.FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphs));
        Assert.Equal("Ahem", glyphs.FamilyName);
    }
}
