using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class MaterialColorSchemeTests
{
    [Fact]
    public void ColorScheme_Constructor_UsesFlutterRoleFallbacks()
    {
        var scheme = ColorScheme.Light();

        Assert.Equal(scheme.Primary, scheme.PrimaryContainer);
        Assert.Equal(scheme.OnPrimary, scheme.OnPrimaryContainer);
        Assert.Equal(scheme.Secondary, scheme.Tertiary);
        Assert.Equal(scheme.OnSecondary, scheme.OnTertiary);
        Assert.Equal(scheme.Surface, scheme.SurfaceContainerHighest);
        Assert.Equal(scheme.OnSurface, scheme.OnSurfaceVariant);
        Assert.Equal(scheme.OnBackground, scheme.Outline);
        Assert.Equal(Colors.Black, scheme.Shadow);
        Assert.Equal(scheme.Primary, scheme.SurfaceTint);
    }

    [Fact]
    public void ColorScheme_FromSeed_MatchesFlutterTonalSpotLightBlue()
    {
        var scheme = ColorScheme.FromSeed(Color.Parse("#FF2196F3"));

        Assert.Equal(Color.Parse("#FF36618E"), scheme.Primary);
        Assert.Equal(Colors.White, scheme.OnPrimary);
        Assert.Equal(Color.Parse("#FFD1E4FF"), scheme.PrimaryContainer);
        Assert.Equal(Color.Parse("#FF194975"), scheme.OnPrimaryContainer);
        Assert.Equal(Color.Parse("#FF535F70"), scheme.Secondary);
        Assert.Equal(Color.Parse("#FF6B5778"), scheme.Tertiary);
        Assert.Equal(Color.Parse("#FFBA1A1A"), scheme.Error);
        Assert.Equal(Color.Parse("#FFF8F9FF"), scheme.Surface);
        Assert.Equal(Color.Parse("#FFECEEF4"), scheme.SurfaceContainer);
        Assert.Equal(Color.Parse("#FF191C20"), scheme.OnSurface);
        Assert.Equal(Color.Parse("#FF73777F"), scheme.Outline);
        Assert.Equal(Color.Parse("#FFA0CAFD"), scheme.InversePrimary);
        Assert.Equal(Brightness.Light, scheme.Brightness);
    }

    [Fact]
    public void ColorScheme_FromSeed_MatchesFlutterTonalSpotDarkBlue()
    {
        var scheme = ColorScheme.FromSeed(Color.Parse("#FF2196F3"), Brightness.Dark);

        Assert.Equal(Color.Parse("#FFA0CAFD"), scheme.Primary);
        Assert.Equal(Color.Parse("#FF003258"), scheme.OnPrimary);
        Assert.Equal(Color.Parse("#FF194975"), scheme.PrimaryContainer);
        Assert.Equal(Color.Parse("#FFD1E4FF"), scheme.OnPrimaryContainer);
        Assert.Equal(Color.Parse("#FFBBC7DB"), scheme.Secondary);
        Assert.Equal(Color.Parse("#FFD6BEE4"), scheme.Tertiary);
        Assert.Equal(Color.Parse("#FFFFB4AB"), scheme.Error);
        Assert.Equal(Color.Parse("#FF111418"), scheme.Surface);
        Assert.Equal(Color.Parse("#FF1D2024"), scheme.SurfaceContainer);
        Assert.Equal(Color.Parse("#FFE1E2E8"), scheme.OnSurface);
        Assert.Equal(Color.Parse("#FF8D9199"), scheme.Outline);
        Assert.Equal(Brightness.Dark, scheme.Brightness);
    }

    [Fact]
    public void ColorScheme_FromSeed_SupportsEveryFlutterVariant()
    {
        DynamicSchemeVariant[] variants = Enum.GetValues<DynamicSchemeVariant>();
        var primaries = new HashSet<Color>();

        foreach (DynamicSchemeVariant variant in variants)
        {
            var scheme = ColorScheme.FromSeed(
                seedColor: Color.Parse("#FF6559F5"),
                dynamicSchemeVariant: variant);
            primaries.Add(scheme.Primary);
        }

        Assert.True(primaries.Count >= 8);
        Assert.Equal(
            Color.Parse("#FF4C3CDB"),
            ColorScheme.FromSeed(
                seedColor: Color.Parse("#FF6559F5"),
                dynamicSchemeVariant: DynamicSchemeVariant.Fidelity).Primary);
    }

    [Fact]
    public void ColorScheme_FromSeed_ExplicitRolesOverrideGeneratedValues()
    {
        var scheme = ColorScheme.FromSeed(
            seedColor: Colors.Blue,
            primary: Colors.Red,
            tertiaryContainer: Colors.Green,
            surfaceContainerHighest: Colors.Yellow,
            scrim: Colors.White);

        Assert.Equal(Colors.Red, scheme.Primary);
        Assert.Equal(Colors.Green, scheme.TertiaryContainer);
        Assert.Equal(Colors.Yellow, scheme.SurfaceContainerHighest);
        Assert.Equal(Colors.White, scheme.Scrim);
    }

    [Theory]
    [InlineData(-1.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ColorScheme_FromSeed_RejectsInvalidContrast(double contrastLevel)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ColorScheme.FromSeed(Colors.Blue, contrastLevel: contrastLevel));
    }

    [Fact]
    public void ColorScheme_CopyWith_AndLerp_CoverExtendedRoles()
    {
        var begin = ColorScheme.Material3Light;
        var end = begin.CopyWith(
            brightness: Brightness.Dark,
            primaryFixed: Colors.Black,
            tertiaryContainer: Colors.Red,
            surfaceContainerHighest: Colors.Blue,
            scrim: Colors.White);

        var midpoint = ColorScheme.Lerp(begin, end, 0.5);

        Assert.Equal(Brightness.Dark, midpoint.Brightness);
        Assert.NotEqual(begin.PrimaryFixed, midpoint.PrimaryFixed);
        Assert.NotEqual(begin.TertiaryContainer, midpoint.TertiaryContainer);
        Assert.NotEqual(begin.SurfaceContainerHighest, midpoint.SurfaceContainerHighest);
        Assert.NotEqual(begin.Scrim, midpoint.Scrim);
        Assert.Equal(end, ColorScheme.Lerp(begin, end, 1.0));
    }

    [Fact]
    public void ThemeData_ColorScheme_DrivesLegacyColorSurface()
    {
        var scheme = ColorScheme.FromSeed(Color.Parse("#FF006495"), Brightness.Dark);
        var theme = new ThemeData(colorScheme: scheme);

        Assert.Equal(scheme, theme.ColorScheme);
        Assert.Equal(scheme.Brightness, theme.Brightness);
        Assert.Equal(scheme.Surface, theme.PrimaryColor);
        Assert.Equal(scheme.Secondary, theme.SecondaryColor);
        Assert.Equal(scheme.PrimaryContainer, theme.PrimaryContainerColor);
        Assert.Equal(scheme.OnPrimaryContainer, theme.OnPrimaryContainerColor);
        Assert.Equal(scheme.SurfaceContainer, theme.SurfaceContainerColor);
        Assert.Equal(scheme.OnSurface, theme.OnSurfaceColor);
        Assert.Equal(scheme.Outline, theme.DividerColor);
        Assert.Equal(scheme.Error, theme.ErrorColor);
    }

    [Fact]
    public void ThemeData_ColorSchemeSeed_GeneratesScheme_AndGuardsConflicts()
    {
        Color seed = Color.Parse("#FF006495");
        var theme = new ThemeData(colorSchemeSeed: seed, brightness: Brightness.Dark);

        Assert.Equal(ColorScheme.FromSeed(seed, Brightness.Dark), theme.ColorScheme);
        Assert.Throws<ArgumentException>(
            () => new ThemeData(colorSchemeSeed: seed, colorScheme: ColorScheme.Light()));
        Assert.Throws<ArgumentException>(
            () => new ThemeData(colorSchemeSeed: seed, primaryColor: Colors.Red));
        Assert.Throws<ArgumentException>(
            () => new ThemeData(brightness: Brightness.Dark, colorScheme: ColorScheme.Light()));
    }

    [Fact]
    public void TextTheme_ExposesCompleteMaterial2021TypeScale()
    {
        TextTheme theme = MaterialTextTheme.Fallback;

        Assert.Equal(57, theme.DisplayLarge.FontSize);
        Assert.Equal(45, theme.DisplayMedium.FontSize);
        Assert.Equal(36, theme.DisplaySmall.FontSize);
        Assert.Equal(32, theme.HeadlineLarge.FontSize);
        Assert.Equal(28, theme.HeadlineMedium.FontSize);
        Assert.Equal(24, theme.HeadlineSmall.FontSize);
        Assert.Equal(22, theme.TitleLarge.FontSize);
        Assert.Equal(16, theme.TitleMedium.FontSize);
        Assert.Equal(14, theme.TitleSmall.FontSize);
        Assert.Equal(16, theme.BodyLarge.FontSize);
        Assert.Equal(14, theme.BodyMedium.FontSize);
        Assert.Equal(12, theme.BodySmall.FontSize);
        Assert.Equal(14, theme.LabelLarge.FontSize);
        Assert.Equal(12, theme.LabelMedium.FontSize);
        Assert.Equal(11, theme.LabelSmall.FontSize);
    }

    [Fact]
    public void TextTheme_CopyMergeApplyAndLerp_FollowFlutterComposition()
    {
        TextTheme begin = MaterialTextTheme.Fallback;
        TextTheme merged = begin.Merge(new TextTheme(
            titleLarge: new TextStyle(Color: Colors.Red),
            bodyMedium: new TextStyle(FontWeight: FontWeight.Bold)));
        TextTheme applied = merged.Apply(
            fontSizeFactor: 2.0,
            letterSpacingDelta: 1.0,
            displayColor: Colors.Blue,
            bodyColor: Colors.Green);
        TextTheme midpoint = TextTheme.Lerp(begin, applied, 0.5);

        Assert.Equal(22, merged.TitleLarge.FontSize);
        Assert.Equal(Colors.Red, merged.TitleLarge.Color);
        Assert.Equal(FontWeight.Bold, merged.BodyMedium.FontWeight);
        Assert.Equal(44, applied.TitleLarge.FontSize);
        Assert.Equal(Colors.Green, applied.TitleLarge.Color);
        Assert.Equal(Colors.Blue, applied.DisplayLarge.Color);
        Assert.Equal(33, midpoint.TitleLarge.FontSize);
    }

    [Fact]
    public void Typography_Material2021_UsesSchemeColorsAndPlatformFont()
    {
        var scheme = ColorScheme.Material3Dark;
        Typography typography = Typography.Material2021(
            platform: TargetPlatform.Windows,
            colorScheme: scheme);

        Assert.Equal(scheme.Surface, typography.Black.BodyMedium.Color);
        Assert.Equal(scheme.OnSurface, typography.White.BodyMedium.Color);
        Assert.Equal(new FontFamily("Segoe UI"), typography.Black.BodyMedium.FontFamily);
        Assert.Null(typography.EnglishLike.BodyMedium.FontFamily);
        Assert.Equal(TextLeadingDistribution.Even, typography.EnglishLike.BodyMedium.LeadingDistribution);
        Assert.Same(
            typography.Dense,
            typography.GeometryThemeFor(ScriptCategory.Dense));
    }

    [Fact]
    public void Typography_LegacyGeometry_MatchesFlutter2014And2018Tables()
    {
        Assert.Equal(Typography.Material2018(), new Typography());
        AssertGeometry(
            Typography.EnglishLike2014,
            [112, 56, 45, 40, 34, 24, 20, 16, 14, 14, 14, 12, 14, 12, 10],
            [100, 400, 400, 400, 400, 400, 500, 400, 500, 500, 400, 400, 500, 400, 400],
            [null, null, null, null, null, null, null, null, 0.1, null, null, null, null, null, 1.5],
            TextBaseline.Alphabetic);
        AssertGeometry(
            Typography.Dense2014,
            [112, 56, 45, 40, 34, 24, 21, 17, 15, 15, 15, 13, 15, 12, 11],
            [100, 400, 400, 400, 400, 400, 500, 400, 500, 500, 400, 400, 500, 400, 400],
            new double?[15],
            TextBaseline.Ideographic);
        AssertGeometry(
            Typography.Tall2014,
            [112, 56, 45, 40, 34, 24, 21, 17, 15, 15, 15, 13, 15, 12, 11],
            [400, 400, 400, 400, 400, 400, 700, 400, 500, 700, 400, 400, 700, 400, 400],
            new double?[15],
            TextBaseline.Alphabetic);
        AssertGeometry(
            Typography.EnglishLike2018,
            [96, 60, 48, 40, 34, 24, 20, 16, 14, 16, 14, 12, 14, 11, 10],
            [300, 300, 400, 400, 400, 400, 500, 400, 500, 400, 400, 400, 500, 400, 400],
            [-1.5, -0.5, 0.0, 0.25, 0.25, 0.0, 0.15, 0.15, 0.1, 0.5, 0.25, 0.4, 1.25, 1.5, 1.5],
            TextBaseline.Alphabetic);
        AssertGeometry(
            Typography.Dense2018,
            [96, 60, 48, 40, 34, 24, 21, 17, 15, 17, 15, 13, 15, 12, 11],
            [100, 100, 400, 400, 400, 400, 500, 400, 500, 400, 400, 400, 500, 400, 400],
            new double?[15],
            TextBaseline.Ideographic);
        AssertGeometry(
            Typography.Tall2018,
            [96, 60, 48, 40, 34, 24, 21, 17, 15, 17, 15, 13, 15, 12, 11],
            [400, 400, 400, 400, 400, 400, 700, 400, 500, 700, 400, 400, 700, 400, 400],
            new double?[15],
            TextBaseline.Alphabetic);
    }

    [Fact]
    public void Typography_NullPlatformRequiresBothExplicitColorThemes()
    {
        Assert.Throws<ArgumentException>(() => Typography.Material2014(platform: null));

        Typography typography = Typography.Material2014(
            platform: null,
            black: Typography.BlackMountainView,
            white: Typography.WhiteMountainView);

        Assert.Same(Typography.BlackMountainView, typography.Black);
        Assert.Same(Typography.WhiteMountainView, typography.White);
    }

    [Fact]
    public void Typography_PlatformThemes_UseFlutterFontsColorsAndFallbacks()
    {
        Assert.All(Styles(Typography.BlackRedmond), style =>
        {
            Assert.Equal(new FontFamily("Segoe UI"), style.FontFamily);
            Assert.Equal(Plumix.UI.TextDecoration.None, style.Decoration);
        });
        Assert.All(Styles(Typography.BlackRedwoodCity), style =>
            Assert.Equal(new FontFamily(".AppleSystemUIFont"), style.FontFamily));
        Assert.All(Styles(Typography.BlackHelsinki), style =>
        {
            Assert.Equal(new FontFamily("Roboto"), style.FontFamily);
            Assert.Equal(
                ["Ubuntu", "Adwaita Sans", "Cantarell", "DejaVu Sans", "Liberation Sans", "Arial"],
                style.FontFamilyFallback);
        });

        TextStyle[] cupertino = Styles(Typography.BlackCupertino);
        Assert.All(cupertino[..7], style =>
            Assert.Equal(new FontFamily("CupertinoSystemDisplay"), style.FontFamily));
        Assert.All(cupertino[7..], style =>
            Assert.Equal(new FontFamily("CupertinoSystemText"), style.FontFamily));
        Assert.Equal(Color.FromArgb(0x8A, 0, 0, 0), Typography.BlackMountainView.DisplayLarge.Color);
        Assert.Equal(Color.FromArgb(0xDD, 0, 0, 0), Typography.BlackMountainView.BodyMedium.Color);
        Assert.Equal(Colors.Black, Typography.BlackMountainView.LabelSmall.Color);
        Assert.Equal(Color.FromArgb(0xB3, 255, 255, 255), Typography.WhiteMountainView.DisplayLarge.Color);
        Assert.Equal(Colors.White, Typography.WhiteMountainView.BodyMedium.Color);
    }

    [Fact]
    public void ThemeData_UsesMaterial2014ForM2_AndLocalizesGeometryByScriptCategory()
    {
        var raw = new ThemeData(useMaterial3: false, platform: TargetPlatform.Android);

        Assert.Equal(Typography.Material2014(platform: TargetPlatform.Android), raw.Typography);
        Assert.Null(raw.TextTheme.BodyMedium.FontSize);

        ThemeData? localized = null;
        var root = new TestRootElement(
            new MaterialLocalizationsScope(
                new TestMaterialLocalizations(ScriptCategory.Dense),
                new Theme(
                    raw,
                    new Builder(context =>
                    {
                        localized = Theme.Of(context);
                        return new SizedBox();
                    }))));
        var owner = new BuildOwner();
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(localized);
        Assert.Equal(15, localized.TextTheme.BodyMedium.FontSize);
        Assert.Equal(TextBaseline.Ideographic, localized.TextTheme.BodyMedium.TextBaseline);
        Assert.Equal(new FontFamily("Roboto"), localized.TextTheme.BodyMedium.FontFamily);
        Assert.Equal(Color.FromArgb(0xDD, 0, 0, 0), localized.TextTheme.BodyMedium.Color);

        root.Unmount();
    }

    [Fact]
    public void ThemeData_AppliesConstructorFontsBeforeUserTextThemeOverrides()
    {
        string[] fallback = ["Fallback One", "Fallback Two"];
        var theme = new ThemeData(
            fontFamily: new FontFamily("App Font"),
            fontFamilyFallback: fallback,
            package: "fonts.package",
            textTheme: new TextTheme(
                bodyMedium: new TextStyle(FontFamily: new FontFamily("Body Override"))));

        Assert.Equal(new FontFamily("App Font"), theme.TextTheme.TitleLarge.FontFamily);
        Assert.Equal(fallback, theme.TextTheme.TitleLarge.FontFamilyFallback);
        Assert.Equal("fonts.package", theme.TextTheme.TitleLarge.Package);
        Assert.Equal(new FontFamily("Body Override"), theme.TextTheme.BodyMedium.FontFamily);
        Assert.Equal(new FontFamily("App Font"), theme.PrimaryTextTheme.BodyMedium.FontFamily);
    }

    [Fact]
    public void ThemeData_Localize_MemoizesByIdentity_AndPreservesLocaleGeometry()
    {
        var theme = new ThemeData(useMaterial3: false);
        TextTheme geometry = Typography.Tall2014;

        ThemeData first = ThemeData.Localize(theme, geometry);
        ThemeData second = ThemeData.Localize(theme, geometry);
        ThemeData third = ThemeData.Localize(theme, geometry.CopyWith());

        Assert.Same(first, second);
        Assert.NotSame(first, third);
        Assert.Equal(15, first.TextTheme.BodyLarge.FontSize);
        Assert.Equal(FontWeight.Bold, first.TextTheme.BodyLarge.FontWeight);
        Assert.Equal(TextBaseline.Alphabetic, first.TextTheme.BodyLarge.TextBaseline);
        Assert.Equal(theme.TextTheme.BodyLarge.Color, first.TextTheme.BodyLarge.Color);
    }

    [Fact]
    public void MaterialLocalizations_MapsFlutterScriptCategories()
    {
        string[] dense = ["bo", "hi", "ja", "km", "ko", "mr", "ta", "zh"];
        string[] tall =
        [
            "ar", "bn", "fa", "gu", "kn", "lo", "ml", "my", "ne", "or", "pa", "ps", "te", "th", "ug",
            "ur",
        ];

        Assert.All(dense, language => Assert.Equal(
            ScriptCategory.Dense,
            DefaultMaterialLocalizations.Delegate.LoadTyped(new Locale(language)).ScriptCategory));
        Assert.All(tall, language => Assert.Equal(
            ScriptCategory.Tall,
            DefaultMaterialLocalizations.Delegate.LoadTyped(new Locale(language)).ScriptCategory));
        Assert.Equal(
            ScriptCategory.EnglishLike,
            DefaultMaterialLocalizations.Delegate.LoadTyped(new Locale("he")).ScriptCategory);
        Assert.Equal(
            ScriptCategory.EnglishLike,
            DefaultMaterialLocalizations.Delegate.LoadTyped(new Locale("vi")).ScriptCategory);
    }

    [Fact]
    public void ThemeData_Lerp_InterpolatesColorSchemeAndTypography()
    {
        var begin = new ThemeData(colorScheme: ColorScheme.Material3Light);
        var end = new ThemeData(colorScheme: ColorScheme.Material3Dark);

        ThemeData midpoint = ThemeData.Lerp(begin, end, 0.5);

        Assert.Equal(
            ColorScheme.Lerp(begin.ColorScheme, end.ColorScheme, 0.5),
            midpoint.ColorScheme);
        Assert.Equal(
            Typography.Lerp(begin.Typography, end.Typography, 0.5),
            midpoint.Typography);
    }

    private static void AssertGeometry(
        TextTheme theme,
        IReadOnlyList<double> sizes,
        IReadOnlyList<int> weights,
        IReadOnlyList<double?> letterSpacing,
        TextBaseline baseline)
    {
        TextStyle[] styles = Styles(theme);
        Assert.Equal(15, styles.Length);
        for (int index = 0; index < styles.Length; index++)
        {
            Assert.Equal(sizes[index], styles[index].FontSize);
            Assert.Equal(weights[index], WeightValue(styles[index].FontWeight));
            Assert.Equal(letterSpacing[index], styles[index].LetterSpacing);
            Assert.Equal(baseline, styles[index].TextBaseline);
            Assert.False(styles[index].Inherit);
            Assert.Null(styles[index].Height);
        }
    }

    private static TextStyle[] Styles(TextTheme theme) =>
    [
        theme.DisplayLarge,
        theme.DisplayMedium,
        theme.DisplaySmall,
        theme.HeadlineLarge,
        theme.HeadlineMedium,
        theme.HeadlineSmall,
        theme.TitleLarge,
        theme.TitleMedium,
        theme.TitleSmall,
        theme.BodyLarge,
        theme.BodyMedium,
        theme.BodySmall,
        theme.LabelLarge,
        theme.LabelMedium,
        theme.LabelSmall,
    ];

    private static int WeightValue(FontWeight? weight)
    {
        if (weight == FontWeight.Thin) return 100;
        if (weight == FontWeight.Light) return 300;
        if (weight == FontWeight.Medium) return 500;
        if (weight == FontWeight.Bold) return 700;
        return 400;
    }

    private sealed class TestMaterialLocalizations(ScriptCategory scriptCategory) : MaterialLocalizations
    {
        public override ScriptCategory ScriptCategory { get; } = scriptCategory;

        public override string TabLabel(int tabIndex, int tabCount) => $"{tabIndex + 1}/{tabCount}";
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

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

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
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
    }
}
