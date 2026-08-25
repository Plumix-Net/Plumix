using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class MaterialIconsTests
{
    private const string ExpectedManifestHash =
        "843068ed2052ffeb169a725c2ba32a4b6d4b6b8b7135741beaa64cdb561d8331";
    private const string ExpectedAliasManifestHash =
        "7b2d0c21162ddae0942442151d579839cd8cacbd1725aa3e2f6391a4c211d852";
    private const string ExpectedFontHash =
        "d9865b671a09d683d13a863089d8825e0f61a37696ce5d7d448bc8023aa62453";
    private const string ExpectedFontFamily =
        "avares://Plumix.Material/Assets/Fonts/MaterialIcons-Regular.otf#Material Icons";

    [Fact]
    public void GeneratedCatalog_MatchesPinnedDartManifest()
    {
        (string Name, IconData Data)[] icons = PublicIcons();
        Assert.Equal(8825, icons.Length);
        Assert.Equal(8825, icons.Select(icon => icon.Name).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(8622, icons.Select(icon => icon.Data.CodePoint).Distinct().Count());
        Assert.Equal(303, icons.Count(icon => icon.Data.MatchTextDirection));
        Assert.Equal(ExpectedManifestHash, Icons.ManifestSha256);
        Assert.Equal(8825, Icons.IconCount);

        Assert.All(icons, icon =>
        {
            Assert.Equal("MaterialIcons", icon.Data.FontFamily);
            Assert.Equal(Icons.IconFont, icon.Data.FontFamily);
            Assert.Null(icon.Data.FontPackage);
        });

        string manifest = string.Join(
            '\n',
            icons.Select(icon =>
                $"{icon.Name}=0x{icon.Data.CodePoint:x}:{icon.Data.MatchTextDirection.ToString().ToLowerInvariant()}"));
        Assert.Equal(ExpectedManifestHash, Sha256(manifest));
    }

    [Fact]
    public void GeneratedCatalog_KeepsFlutterCodePointsForTheHandWrittenSubsetItReplaced()
    {
        // The pre-generator catalog was hand-transcribed and drifted; these are the Dart values.
        Assert.Equal(0xe092, Icons.ArrowBack.CodePoint);
        Assert.Equal(0xe21a, Icons.Edit.CodePoint);
        Assert.Equal(0xe567, Icons.Search.CodePoint);
        Assert.Equal(0xe6bd, Icons.Visibility.CodePoint);
        Assert.Equal(0xe6be, Icons.VisibilityOff.CodePoint);
        Assert.Equal(0xe206, Icons.DragHandle.CodePoint);
        Assert.Equal(0xe15e, Icons.ChevronLeft.CodePoint);
        Assert.Equal(0xe15f, Icons.ChevronRight.CodePoint);
        Assert.Equal(0xe28b, Icons.FirstPage.CodePoint);
        Assert.Equal(0xf144, Icons.KeyboardOutlined.CodePoint);
        Assert.Equal(0xe047, Icons.Add.CodePoint);
        Assert.Equal(0xe5f9, Icons.Star.CodePoint);
    }

    [Fact]
    public void GeneratedCatalog_MarksExactlyFlutterDirectionalIcons()
    {
        Assert.True(Icons.ArrowBack.MatchTextDirection);
        Assert.True(Icons.ChevronLeft.MatchTextDirection);
        Assert.True(Icons.FirstPage.MatchTextDirection);
        Assert.True(Icons.ArrowBackIosNewRounded.MatchTextDirection);
        Assert.False(Icons.Add.MatchTextDirection);
        Assert.False(Icons.Star.MatchTextDirection);
        Assert.False(Icons.MoreVert.MatchTextDirection);
    }

    [Fact]
    public void GeneratedCatalog_PreservesEveryCodePointAliasGroup()
    {
        IGrouping<int, (string Name, IconData Data)>[] aliases = PublicIcons()
            .GroupBy(icon => icon.Data.CodePoint)
            .Where(group => group.Count() > 1)
            .OrderBy(group => group.Key)
            .ToArray();

        Assert.Equal(203, aliases.Length);
        Assert.Equal(2, aliases.Max(group => group.Count()));

        string manifest = string.Join(
            '\n',
            aliases.Select(group =>
                $"0x{group.Key:x}:{string.Join(',', group.Select(icon => icon.Name).Order(StringComparer.Ordinal))}"));
        Assert.Equal(ExpectedAliasManifestHash, Sha256(manifest));

        Assert.Equal(Icons.AirplanemodeActive, Icons.AirplanemodeOn);
        Assert.Equal(Icons.BookmarkBorder, Icons.BookmarkOutline);
        Assert.Equal(Icons.CardGiftcard, Icons.WalletGiftcard);
    }

    [Fact]
    public void GeneratedCatalog_ReturnsTheSameInstanceForRepeatedReads()
    {
        Assert.Same(Icons.Add, Icons.Add);
        Assert.Same(Icons.ZoomOutMapRounded, Icons.ZoomOutMapRounded);
    }

    [Theory]
    [InlineData(TargetPlatform.Android, false)]
    [InlineData(TargetPlatform.Fuchsia, false)]
    [InlineData(TargetPlatform.Linux, false)]
    [InlineData(TargetPlatform.Windows, false)]
    [InlineData(TargetPlatform.IOS, true)]
    [InlineData(TargetPlatform.MacOS, true)]
    public void PlatformAdaptiveIcons_ResolveTheAppleVariantOnlyOnApplePlatforms(
        TargetPlatform platform,
        bool cupertino)
    {
        TargetPlatform? previous = PlatformDefaults.DebugTargetPlatformOverride;
        PlatformDefaults.DebugTargetPlatformOverride = platform;
        try
        {
            PlatformAdaptiveIcons adaptive = Icons.Adaptive;

            Assert.Equal(cupertino ? Icons.ArrowBackIos : Icons.ArrowBack, adaptive.ArrowBack);
            Assert.Equal(
                cupertino ? Icons.ArrowBackIosOutlined : Icons.ArrowBackOutlined,
                adaptive.ArrowBackOutlined);
            Assert.Equal(
                cupertino ? Icons.ArrowBackIosRounded : Icons.ArrowBackRounded,
                adaptive.ArrowBackRounded);
            Assert.Equal(cupertino ? Icons.ArrowBackIosSharp : Icons.ArrowBackSharp, adaptive.ArrowBackSharp);
            Assert.Equal(cupertino ? Icons.ArrowForwardIos : Icons.ArrowForward, adaptive.ArrowForward);
            Assert.Equal(
                cupertino ? Icons.ArrowForwardIosOutlined : Icons.ArrowForwardOutlined,
                adaptive.ArrowForwardOutlined);
            Assert.Equal(
                cupertino ? Icons.ArrowForwardIosRounded : Icons.ArrowForwardRounded,
                adaptive.ArrowForwardRounded);
            Assert.Equal(
                cupertino ? Icons.ArrowForwardIosSharp : Icons.ArrowForwardSharp,
                adaptive.ArrowForwardSharp);
            Assert.Equal(
                cupertino ? Icons.FlipCameraIos : Icons.FlipCameraAndroid,
                adaptive.FlipCamera);
            Assert.Equal(
                cupertino ? Icons.FlipCameraIosOutlined : Icons.FlipCameraAndroidOutlined,
                adaptive.FlipCameraOutlined);
            Assert.Equal(
                cupertino ? Icons.FlipCameraIosRounded : Icons.FlipCameraAndroidRounded,
                adaptive.FlipCameraRounded);
            Assert.Equal(
                cupertino ? Icons.FlipCameraIosSharp : Icons.FlipCameraAndroidSharp,
                adaptive.FlipCameraSharp);
            Assert.Equal(cupertino ? Icons.MoreHoriz : Icons.MoreVert, adaptive.More);
            Assert.Equal(
                cupertino ? Icons.MoreHorizOutlined : Icons.MoreVertOutlined,
                adaptive.MoreOutlined);
            Assert.Equal(cupertino ? Icons.MoreHorizRounded : Icons.MoreVertRounded, adaptive.MoreRounded);
            Assert.Equal(cupertino ? Icons.MoreHorizSharp : Icons.MoreVertSharp, adaptive.MoreSharp);
            Assert.Equal(cupertino ? Icons.IosShare : Icons.Share, adaptive.Share);
            Assert.Equal(cupertino ? Icons.IosShareOutlined : Icons.ShareOutlined, adaptive.ShareOutlined);
            Assert.Equal(cupertino ? Icons.IosShareRounded : Icons.ShareRounded, adaptive.ShareRounded);
            Assert.Equal(cupertino ? Icons.IosShareSharp : Icons.ShareSharp, adaptive.ShareSharp);
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = previous;
        }
    }

    [Fact]
    public void BundledFont_ResolvesRepresentativeCatalogGlyphs()
    {
        _ = Icons.Add;
        var assetLoader = new StandardAssetLoader();
        using Stream font = assetLoader.Open(
            new Uri("avares://Plumix.Material/Assets/Fonts/MaterialIcons-Regular.otf"),
            baseUri: null);
        Assert.True(font.Length > 0);
        using var fontBytes = new MemoryStream();
        font.CopyTo(fontBytes);
        Assert.Equal(ExpectedFontHash, Convert.ToHexStringLower(SHA256.HashData(fontBytes.ToArray())));

        IconData[] representatives =
        [
            Icons.Add,
            Icons.Home,
            Icons.ArrowBack,
            Icons.ZoomOutMapOutlined,
            Icons.ZoomOutMapRounded,
        ];

        foreach (IconData icon in representatives)
        {
            var owner = new BuildOwner();
            var root = new TestRootElement(new Directionality(
                textDirection: TextDirection.Ltr,
                child: new Icon(icon, size: 128.0)));
            root.Attach(owner);
            root.Mount(parent: null, newSlot: null);
            owner.FlushBuild();

            var paragraph = FindDescendant<RenderParagraph>(root.ChildElement!.RenderObject);
            Assert.NotNull(paragraph);
            Assert.Equal(char.ConvertFromUtf32(icon.CodePoint), paragraph!.PlainText);
            Assert.Equal(new FontFamily(ExpectedFontFamily), paragraph.FontFamily);
        }
    }

    [Fact]
    public void DirectionalIcons_MirrorOnlyInRightToLeftLayouts()
    {
        var owner = new BuildOwner();
        var directionalRoot = new TestRootElement(new Directionality(
            textDirection: TextDirection.Rtl,
            child: new Icon(Icons.ArrowBack)));
        directionalRoot.Attach(owner);
        directionalRoot.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
        Assert.NotNull(FindDescendant<RenderTransform>(directionalRoot.ChildElement!.RenderObject));

        var plainRoot = new TestRootElement(new Directionality(
            textDirection: TextDirection.Rtl,
            child: new Icon(Icons.Add)));
        plainRoot.Attach(owner);
        plainRoot.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
        Assert.Null(FindDescendant<RenderTransform>(plainRoot.ChildElement!.RenderObject));
    }

    private static (string Name, IconData Data)[] PublicIcons()
    {
        return typeof(Icons)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(IconData))
            .Select(property => (property.Name, Data: (IconData)property.GetValue(obj: null)!))
            .OrderBy(icon => icon.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static string Sha256(string value)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexStringLower(bytes);
    }

    private static T? FindDescendant<T>(RenderObject? root) where T : RenderObject
    {
        if (root is null)
        {
            return null;
        }

        if (root is T match)
        {
            return match;
        }

        T? result = null;
        root.VisitChildren(child => result ??= FindDescendant<T>(child));
        return result;
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        public Element? ChildElement => _child;

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        internal override void Rebuild()
        {
            Dirty = false;
            _child = UpdateChild(_child, Widget, Slot);
        }

        internal override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild();
        }

        internal override void VisitChildren(Action<Element> visitor)
        {
            if (_child is not null)
            {
                visitor(_child);
            }
        }

        internal override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        internal override void Unmount()
        {
            if (_child is not null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot is not null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
            if (!Equals(oldSlot, newSlot))
            {
                throw new InvalidOperationException("TestRootElement does not support slot moves.");
            }
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot is not null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }
    }
}
