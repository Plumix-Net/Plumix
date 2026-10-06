using Plumix.Foundation;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Mirrors flutter/packages/flutter/test/widgets/icon_data_test.dart and icon_test.dart.
public sealed class IconDataTests
{
    [Fact]
    public void IconData_UsesOrderedFallbackValueEqualityHashAndSourceFormatting()
    {
        var data = new IconData(123);
        Assert.Equal(data, new IconData(123));
        Assert.Equal(data.GetHashCode(), new IconData(123).GetHashCode());
        Assert.NotEqual(data, data with { MatchTextDirection = true });
        Assert.NotEqual(data, data with { FontFamily = "f" });
        Assert.NotEqual(data, data with { FontPackage = "p" });
        Assert.NotEqual(data, data with { CodePoint = 124 });
        var fallback = data with { FontFamilyFallback = ["f", "g"] };
        var equal = data with { FontFamilyFallback = ["f", "g"] };
        Assert.Equal(fallback, equal);
        Assert.Equal(fallback.GetHashCode(), equal.GetHashCode());
        Assert.NotEqual(fallback, data with { FontFamilyFallback = ["g", "f"] });
        Assert.NotEqual(data, data with { FontFamilyFallback = [] });
        Assert.Equal("IconData(U+0007B)", data.ToString());
        Assert.Equal("IconData(U+F04B6)", new IconData(0xf04b6).ToString());
        (int codePoint, string? family, string? package, bool directional) = data;
        Assert.Equal(123, codePoint);
        Assert.Null(family);
        Assert.Null(package);
        Assert.False(directional);
    }

    [Fact]
    public void Diagnostics_IncludeTheCodePointOnlyForNonNullIcons()
    {
        var glyph = new IconData(101010);
        Dictionary<string, object?> json = new IconDataProperty("icon", glyph)
            .ToJsonMap(DiagnosticsSerializationDelegate.Create());
        Assert.Equal(glyph.CodePoint,
            Assert.IsType<Dictionary<string, object?>>(json["valueProperties"])["codePoint"]);
        Assert.False(new IconDataProperty("icon", null)
            .ToJsonMap(DiagnosticsSerializationDelegate.Create()).ContainsKey("valueProperties"));
    }
}
