using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Mirrors flutter/packages/flutter/test/widgets/icon_theme_data_test.dart.
public sealed class IconThemeDataTests
{
    private static readonly Shadow ProbeShadow = new(
        color: Color.FromUInt32(0xAAAAAAAA),
        offset: new Point(1.0, 1.0),
        blurRadius: 1.0);

    [Fact]
    public void CopyWith_EqualityAndDiagnosticsCoverEveryField()
    {
        var data = new IconThemeData(
            Color: Color.FromUInt32(0xAAAAAAAA),
            Size: 16.0,
            Opacity: 0.5,
            Fill: 0.5,
            Weight: 600.0,
            Grade: 25.0,
            OpticalSize: 45.0,
            Shadows: [ProbeShadow],
            ApplyTextScaling: true);

        IconThemeData copy = data.CopyWith();
        Assert.Equal(data, copy);
        Assert.Equal(data.GetHashCode(), copy.GetHashCode());
        Assert.NotEmpty(data.ToString());

        if (!Constants.KDebugMode)
        {
            // `debugFillProperties` is an assert-only body in Dart, so it fills nothing outside a
            // debug build; the equality contract above holds in every build.
            return;
        }

        var diagnostics = new DiagnosticPropertiesBuilder();
        data.DebugFillProperties(diagnostics);
        Assert.Equal(
            ["size", "fill", "weight", "grade", "opticalSize", "color", "opacity", "shadows", "applyTextScaling"],
            diagnostics.Properties.Select(property => property.Name));
    }

    [Fact]
    public void Fallback_IsConcreteAndMatchesFlutterDefaults()
    {
        IconThemeData fallback = IconThemeData.Fallback;

        Assert.True(fallback.IsConcrete);
        Assert.Equal(24.0, fallback.Size);
        Assert.Equal(0.0, fallback.Fill);
        Assert.Equal(400.0, fallback.Weight);
        Assert.Equal(0.0, fallback.Grade);
        Assert.Equal(48.0, fallback.OpticalSize);
        Assert.Equal(Colors.Black, fallback.Color);
        Assert.Equal(1.0, fallback.Opacity);
        Assert.Null(fallback.Shadows);
        Assert.False(fallback.ApplyTextScaling);
        Assert.False(new IconThemeData().IsConcrete);
    }

    [Fact]
    public void Lerp_InterpolatesEveryContinuousFieldAndShadows()
    {
        var data = new IconThemeData(
            Color: Color.FromUInt32(0xAAAAAAAA),
            Size: 16.0,
            Opacity: 0.5,
            Fill: 0.5,
            Weight: 600.0,
            Grade: 25.0,
            OpticalSize: 45.0,
            Shadows: [ProbeShadow],
            ApplyTextScaling: true);

        IconThemeData lerped = IconThemeData.Lerp(data, IconThemeData.Fallback, 0.25);

        Assert.Equal(18.0, lerped.Size);
        Assert.Equal(0.375, lerped.Fill);
        Assert.Equal(550.0, lerped.Weight);
        Assert.Equal(18.75, lerped.Grade);
        Assert.Equal(45.75, lerped.OpticalSize);
        Assert.Equal(Color.FromUInt32(0xBF7F7F7F), lerped.Color);
        Assert.Equal(0.625, lerped.Opacity);
        Shadow shadow = Assert.Single(lerped.Shadows!);
        Assert.Equal(Color.FromUInt32(0xAAAAAAAA), shadow.Color);
        Assert.Equal(new Point(0.75, 0.75), shadow.Offset);
        Assert.Equal(0.75, shadow.BlurRadius);
        Assert.True(lerped.ApplyTextScaling);
    }

    [Fact]
    public void Lerp_HandlesNullEndpointsAndIdentity()
    {
        var data = new IconThemeData(
            Color: Colors.White,
            Size: 16.0,
            Opacity: 1.0,
            Fill: 0.5,
            Weight: 600.0,
            Grade: 25.0,
            OpticalSize: 45.0,
            Shadows: [new Shadow(color: Colors.White, offset: new Point(1.0, 1.0), blurRadius: 1.0)],
            ApplyTextScaling: true);

        IconThemeData fromNull = IconThemeData.Lerp(null, data, 0.25);
        Assert.Equal(4.0, fromNull.Size);
        Assert.Equal(Color.FromUInt32(0x40FFFFFF), fromNull.Color);
        Assert.Null(fromNull.ApplyTextScaling);

        IconThemeData toNull = IconThemeData.Lerp(data, null, 0.25);
        Assert.Equal(12.0, toNull.Size);
        Assert.Equal(Color.FromUInt32(0xBFFFFFFF), toNull.Color);
        Assert.True(toNull.ApplyTextScaling);

        IconThemeData bothNull = IconThemeData.Lerp(null, null, 0.25);
        Assert.Null(bothNull.Size);
        Assert.Null(bothNull.Color);
        Assert.Null(bothNull.Shadows);
        Assert.Same(data, IconThemeData.Lerp(data, data, 0.5));
    }

    [Fact]
    public void ConstructorRejectsInvalidVariationValuesAndClampsOpacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new IconThemeData(Fill: -0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IconThemeData(Fill: 1.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IconThemeData(Fill: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IconThemeData(Weight: 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IconThemeData(Weight: -0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IconThemeData(OpticalSize: 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IconThemeData(OpticalSize: -0.1));
        Assert.Equal(0.0, new IconThemeData(Opacity: -1.0).Opacity);
        Assert.Equal(1.0, new IconThemeData(Opacity: 2.0).Opacity);
        Assert.Equal(1.0, new IconThemeData(Opacity: double.NaN).Opacity);
    }
}
