using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class CupertinoIconsTests
{
    private const string ExpectedManifestHash =
        "480b232efc56bd56e3faeba84f71542d223bba9f90ece32b8a8bcdd0a8e7eb49";
    private const string ExpectedAliasManifestHash =
        "bee931644c76c0d920e9a560269bfc2324a934f6e2d9db704657722cac2c0df5";
    private const string ExpectedFontHash =
        "67c44fe9183b002e79dde7f6977e2988661c9a3e4a3c5fce968787efdbed823c";
    private const string ExpectedFontFamily =
        "avares://Plumix.Cupertino/Assets/Fonts/CupertinoIcons.ttf#CupertinoIcons";

    [Fact]
    public void GeneratedCatalog_MatchesPinnedDartManifest()
    {
        var icons = CupertinoIcons.AllIcons;
        Assert.Equal(1322, icons.Count);
        Assert.Equal(1322, icons.Select(icon => icon.Name).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(1234, icons.Select(icon => icon.Data.CodePoint).Distinct().Count());
        Assert.Equal(ExpectedManifestHash, CupertinoIcons.ManifestSha256);

        Assert.All(icons, icon =>
        {
            Assert.Equal(CupertinoIcons.IconFont, icon.Data.FontFamily);
            Assert.Equal(CupertinoIcons.IconFontPackage, icon.Data.FontPackage);
        });

        string[] directional = icons.Where(icon => icon.Data.MatchTextDirection).Select(icon => icon.Name).ToArray();
        Assert.Equal(["left_chevron", "right_chevron", "back", "forward"], directional);

        string manifest = string.Join(
            '\n',
            icons.Select(icon =>
                $"{icon.Name}=0x{icon.Data.CodePoint:x}:{icon.Data.MatchTextDirection.ToString().ToLowerInvariant()}"));
        Assert.Equal(ExpectedManifestHash, Sha256(manifest));

        var publicIcons = typeof(CupertinoIcons)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(IconData))
            .ToDictionary(property => property.Name, StringComparer.Ordinal);
        Assert.Equal(1322, publicIcons.Count);
        Assert.All(icons, icon => Assert.True(publicIcons.ContainsKey(ToPascalCase(icon.Name))));
    }

    [Fact]
    public void GeneratedCatalog_PreservesEveryCodePointAliasGroup()
    {
        var aliases = CupertinoIcons.AllIcons
            .GroupBy(icon => icon.Data.CodePoint)
            .Where(group => group.Count() > 1)
            .OrderBy(group => group.Key)
            .ToArray();

        Assert.Equal(84, aliases.Length);
        Assert.Equal(4, aliases.Max(group => group.Count()));

        string manifest = string.Join(
            '\n',
            aliases.Select(group =>
                $"0x{group.Key:x}:{string.Join(',', group.Select(icon => icon.Name))}"));
        Assert.Equal(ExpectedAliasManifestHash, Sha256(manifest));

        Assert.Equal(CupertinoIcons.Create, CupertinoIcons.SquarePencil);
        Assert.Equal(CupertinoIcons.PlusCircled, CupertinoIcons.PlusCircle);
        Assert.Equal(CupertinoIcons.Videocam, CupertinoIcons.VideoCamera);
    }

    [Fact]
    public void BundledFont_ResolvesRepresentativeCatalogGlyphs()
    {
        _ = CupertinoIcons.Heart;
        var assetLoader = new StandardAssetLoader();
        using Stream font = assetLoader.Open(
            new Uri("avares://Plumix.Cupertino/Assets/Fonts/CupertinoIcons.ttf"),
            baseUri: null);
        Assert.True(font.Length > 0);
        using var fontBytes = new MemoryStream();
        font.CopyTo(fontBytes);
        Assert.Equal(ExpectedFontHash, Convert.ToHexStringLower(SHA256.HashData(fontBytes.ToArray())));

        IconData[] representatives =
        [
            CupertinoIcons.BatteryCharging,
            CupertinoIcons.Heart,
            CupertinoIcons.ArrowLeftRight,
            CupertinoIcons.VideocamCircleFill,
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
            child: new Icon(CupertinoIcons.Back)));
        directionalRoot.Attach(owner);
        directionalRoot.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
        Assert.NotNull(FindDescendant<RenderTransform>(directionalRoot.ChildElement!.RenderObject));

        var plainRoot = new TestRootElement(new Directionality(
            textDirection: TextDirection.Rtl,
            child: new Icon(CupertinoIcons.Heart)));
        plainRoot.Attach(owner);
        plainRoot.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
        Assert.Null(FindDescendant<RenderTransform>(plainRoot.ChildElement!.RenderObject));
    }

    private static string Sha256(string value)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexStringLower(bytes);
    }

    private static string ToPascalCase(string value)
    {
        return string.Concat(value.Split('_').Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
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
