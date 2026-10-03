// Dart parity source: material_ui/lib/src/date_picker_theme.dart
// Mirrors material-ui-src/test/date_picker_theme_test.dart

using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;
using MaterialWidget = Plumix.Material.Material;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialDatePickerThemeDartParityTests : IDisposable
{
    private static readonly DatePickerThemeData DatePickerTheme = new(
        backgroundColor: new Color(0xfffffff0),
        elevation: 6,
        shadowColor: new Color(0xfffffff1),
        surfaceTintColor: new Color(0xfffffff2),
        shape: new RoundedRectangleBorder(),
        headerBackgroundColor: new Color(0xfffffff3),
        headerForegroundColor: new Color(0xfffffff4),
        headerHeadlineStyle: new TextStyle(FontSize: 10),
        headerHelpStyle: new TextStyle(FontSize: 11),
        weekdayStyle: new TextStyle(FontSize: 12),
        dayStyle: new TextStyle(FontSize: 13),
        dayForegroundColor: new WidgetStatePropertyAll<Color?>(new Color(0xfffffff5)),
        dayBackgroundColor: new WidgetStatePropertyAll<Color?>(new Color(0xfffffff6)),
        dayOverlayColor: new WidgetStatePropertyAll<Color?>(new Color(0xfffffff7)),
        dayShape: new WidgetStatePropertyAll<OutlinedBorder?>(new RoundedRectangleBorder()),
        todayForegroundColor: new WidgetStatePropertyAll<Color?>(new Color(0xfffffff8)),
        todayBackgroundColor: new WidgetStatePropertyAll<Color?>(new Color(0xfffffff9)),
        todayBorder: new BorderSide(width: 3, color: new Color(0x00000000)),
        yearStyle: new TextStyle(FontSize: 13),
        yearForegroundColor: new WidgetStatePropertyAll<Color?>(new Color(0xfffffffa)),
        yearBackgroundColor: new WidgetStatePropertyAll<Color?>(new Color(0xfffffffb)),
        yearOverlayColor: new WidgetStatePropertyAll<Color?>(new Color(0xfffffffc)),
        yearShape: new WidgetStatePropertyAll<OutlinedBorder?>(new RoundedRectangleBorder()),
        rangePickerBackgroundColor: new Color(0xfffffffd),
        rangePickerElevation: 7,
        rangePickerShadowColor: new Color(0xfffffffe),
        rangePickerSurfaceTintColor: new Color(0xffffffff),
        rangePickerShape: new RoundedRectangleBorder(),
        rangePickerHeaderBackgroundColor: new Color(0xffffff0f),
        rangePickerHeaderForegroundColor: new Color(0xffffff1f),
        rangePickerHeaderHeadlineStyle: new TextStyle(FontSize: 14),
        rangePickerHeaderHelpStyle: new TextStyle(FontSize: 15),
        rangeSelectionBackgroundColor: new Color(0xffffff2f),
        rangeSelectionOverlayColor: new WidgetStatePropertyAll<Color?>(new Color(0xffffff3f)),
        dividerColor: new Color(0xffffff4f),
        inputDecorationTheme: new InputDecorationTheme(
            fillColor: new Color(0xffffff5f),
            border: new UnderlineInputBorder()),
        cancelButtonStyle: new ButtonStyle(
            ForegroundColor: new WidgetStatePropertyAll<Color?>(new Color(0xffffff6f))),
        confirmButtonStyle: new ButtonStyle(
            ForegroundColor: new WidgetStatePropertyAll<Color?>(new Color(0xffffff7f))),
        locale: new Locale("en"),
        subHeaderForegroundColor: new Color(0xffffff8f),
        toggleButtonTextStyle: new TextStyle(FontSize: 13));

    private static readonly Size WideWindowSize = new(1920.0, 1080.0);
    private static readonly Size NarrowWindowSize = new(1070.0, 1770.0);

    public MaterialDatePickerThemeDartParityTests()
    {
        FocusManager.Instance.ResetForTests();
        // flutter_test: defaultTargetPlatform == android, debugDisableShadows == true.
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Android;
    }

    public void Dispose()
    {
        PlatformDefaults.DebugTargetPlatformOverride = null;
        FocusManager.Instance.ResetForTests();
    }

    private static readonly IReadOnlySet<WidgetState> NoStates = new HashSet<WidgetState>();

    private static IReadOnlySet<WidgetState> States(params WidgetState[] states) => new HashSet<WidgetState>(states);

    // Flutter: "DatePickerThemeData copyWith, ==, hashCode basics"
    [Fact]
    public void DatePickerThemeDataCopyWithEqualsHashCodeBasics()
    {
        Assert.Equal(new DatePickerThemeData(), new DatePickerThemeData().CopyWith());
        Assert.Equal(new DatePickerThemeData().GetHashCode(), new DatePickerThemeData().CopyWith().GetHashCode());
    }

    // Flutter: "DatePickerThemeData lerp special cases"
    [Fact]
    public void DatePickerThemeDataLerpSpecialCases()
    {
        var data = new DatePickerThemeData();
        Assert.Same(data, DatePickerThemeData.Lerp(data, data, 0.5));
    }

    // Flutter: "DatePickerThemeData defaults"
    [Fact]
    public void DatePickerThemeDataDefaults()
    {
        var theme = new DatePickerThemeData();
        Assert.Null(theme.BackgroundColor);
        Assert.Null(theme.Elevation);
        Assert.Null(theme.ShadowColor);
        Assert.Null(theme.SurfaceTintColor);
        Assert.Null(theme.Shape);
        Assert.Null(theme.HeaderBackgroundColor);
        Assert.Null(theme.HeaderForegroundColor);
        Assert.Null(theme.HeaderHeadlineStyle);
        Assert.Null(theme.HeaderHelpStyle);
        Assert.Null(theme.WeekdayStyle);
        Assert.Null(theme.DayStyle);
        Assert.Null(theme.DayForegroundColor);
        Assert.Null(theme.DayBackgroundColor);
        Assert.Null(theme.DayOverlayColor);
        Assert.Null(theme.DayShape);
        Assert.Null(theme.TodayForegroundColor);
        Assert.Null(theme.TodayBackgroundColor);
        Assert.Null(theme.TodayBorder);
        Assert.Null(theme.YearStyle);
        Assert.Null(theme.YearForegroundColor);
        Assert.Null(theme.YearBackgroundColor);
        Assert.Null(theme.YearOverlayColor);
        Assert.Null(theme.RangePickerBackgroundColor);
        Assert.Null(theme.RangePickerElevation);
        Assert.Null(theme.RangePickerShadowColor);
        Assert.Null(theme.RangePickerSurfaceTintColor);
        Assert.Null(theme.RangePickerShape);
        Assert.Null(theme.RangePickerHeaderBackgroundColor);
        Assert.Null(theme.RangePickerHeaderForegroundColor);
        Assert.Null(theme.RangePickerHeaderHeadlineStyle);
        Assert.Null(theme.RangePickerHeaderHelpStyle);
        Assert.Null(theme.RangeSelectionBackgroundColor);
        Assert.Null(theme.RangeSelectionOverlayColor);
        Assert.Null(theme.DividerColor);
        Assert.Null(theme.InputDecorationTheme);
        Assert.Null(theme.CancelButtonStyle);
        Assert.Null(theme.ConfirmButtonStyle);
        Assert.Null(theme.Locale);
        Assert.Null(theme.SubHeaderForegroundColor);
        Assert.Null(theme.ToggleButtonTextStyle);
    }

    // Flutter: "DatePickerTheme.defaults M3 defaults"
    [Fact]
    public void DatePickerThemeDefaultsM3Defaults()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, semanticsEnabled: true);
        DatePickerThemeData? m3 = null;
        ThemeData? theme = null;
        tester.PumpWidget(new MaterialApp(
            home: new Builder(context =>
            {
                m3 = Plumix.Material.DatePickerTheme.Defaults(context);
                theme = Theme.Of(context);
                return new Container();
            })));

        ColorScheme colorScheme = theme!.ColorScheme;
        TextTheme textTheme = theme.TextTheme;
        Assert.Equal(colorScheme.SurfaceContainerHigh, m3!.BackgroundColor);
        Assert.Equal(6, m3.Elevation);
        Assert.Equal(new Color(0x00000000), m3.ShadowColor); // Colors.transparent
        Assert.Equal(MaterialColors.Transparent, m3.SurfaceTintColor);
        Assert.Equal(
            new RoundedRectangleBorder(borderRadius: BorderRadius.All(Radius.Circular(28))),
            m3.Shape);
        Assert.Equal(new Color(0x00000000), m3.HeaderBackgroundColor); // Colors.transparent
        Assert.Equal(colorScheme.OnSurfaceVariant, m3.HeaderForegroundColor);
        Assert.Equal(textTheme.HeadlineLarge, m3.HeaderHeadlineStyle);
        Assert.Equal(textTheme.LabelLarge, m3.HeaderHelpStyle);
        Assert.Equal(textTheme.BodyLarge?.Apply(color: colorScheme.OnSurface), m3.WeekdayStyle);
        Assert.Equal(textTheme.BodyLarge, m3.DayStyle);
        Assert.Equal(colorScheme.OnSurface, m3.DayForegroundColor?.Resolve(NoStates));
        Assert.Equal(colorScheme.OnPrimary, m3.DayForegroundColor?.Resolve(States(WidgetState.Selected)));
        Assert.Equal(
            colorScheme.OnSurface.WithOpacity(0.38),
            m3.DayForegroundColor?.Resolve(States(WidgetState.Disabled)));
        Assert.Null(m3.DayBackgroundColor?.Resolve(NoStates));
        Assert.Equal(colorScheme.Primary, m3.DayBackgroundColor?.Resolve(States(WidgetState.Selected)));
        Assert.Null(m3.DayOverlayColor?.Resolve(NoStates));
        Assert.Equal(
            colorScheme.OnPrimary.WithOpacity(0.08),
            m3.DayOverlayColor?.Resolve(States(WidgetState.Selected, WidgetState.Hovered)));
        Assert.Equal(
            colorScheme.OnPrimary.WithOpacity(0.1),
            m3.DayOverlayColor?.Resolve(States(WidgetState.Selected, WidgetState.Focused)));
        Assert.Equal(
            colorScheme.OnSurfaceVariant.WithOpacity(0.08),
            m3.DayOverlayColor?.Resolve(States(WidgetState.Hovered)));
        Assert.Equal(
            colorScheme.OnSurfaceVariant.WithOpacity(0.1),
            m3.DayOverlayColor?.Resolve(States(WidgetState.Focused)));
        Assert.Equal(
            colorScheme.OnSurfaceVariant.WithOpacity(0.1),
            m3.DayOverlayColor?.Resolve(States(WidgetState.Pressed)));
        Assert.Equal(
            colorScheme.OnPrimary.WithOpacity(0.08),
            m3.DayOverlayColor?.Resolve(States(WidgetState.Selected, WidgetState.Hovered, WidgetState.Focused)));
        Assert.Equal(
            colorScheme.OnPrimary.WithOpacity(0.1),
            m3.DayOverlayColor?.Resolve(States(WidgetState.Selected, WidgetState.Hovered, WidgetState.Pressed)));
        Assert.Equal(
            colorScheme.OnSurfaceVariant.WithOpacity(0.08),
            m3.DayOverlayColor?.Resolve(States(WidgetState.Hovered, WidgetState.Focused)));
        Assert.Equal(
            colorScheme.OnSurfaceVariant.WithOpacity(0.1),
            m3.DayOverlayColor?.Resolve(States(WidgetState.Hovered, WidgetState.Pressed)));
        Assert.Equal(new CircleBorder(), m3.DayShape?.Resolve(NoStates));
        Assert.Equal(colorScheme.Primary, m3.TodayForegroundColor?.Resolve(NoStates));
        Assert.Equal(
            colorScheme.Primary.WithOpacity(0.38),
            m3.TodayForegroundColor?.Resolve(States(WidgetState.Disabled)));
        Assert.Equal(new BorderSide(color: colorScheme.Primary), m3.TodayBorder);
        Assert.Equal(textTheme.BodyLarge, m3.YearStyle);
        Assert.Equal(colorScheme.OnSurfaceVariant, m3.YearForegroundColor?.Resolve(NoStates));
        Assert.Equal(colorScheme.OnPrimary, m3.YearForegroundColor?.Resolve(States(WidgetState.Selected)));
        Assert.Equal(
            colorScheme.OnSurfaceVariant.WithOpacity(0.38),
            m3.YearForegroundColor?.Resolve(States(WidgetState.Disabled)));
        Assert.Null(m3.YearBackgroundColor?.Resolve(NoStates));
        Assert.Equal(colorScheme.Primary, m3.YearBackgroundColor?.Resolve(States(WidgetState.Selected)));
        Assert.Null(m3.YearOverlayColor?.Resolve(NoStates));
        Assert.Equal(
            colorScheme.OnPrimary.WithOpacity(0.08),
            m3.YearOverlayColor?.Resolve(States(WidgetState.Selected, WidgetState.Hovered)));
        Assert.Equal(
            colorScheme.OnPrimary.WithOpacity(0.1),
            m3.YearOverlayColor?.Resolve(States(WidgetState.Selected, WidgetState.Focused)));
        Assert.Equal(
            colorScheme.OnSurfaceVariant.WithOpacity(0.08),
            m3.YearOverlayColor?.Resolve(States(WidgetState.Hovered)));
        Assert.Equal(
            colorScheme.OnSurfaceVariant.WithOpacity(0.1),
            m3.YearOverlayColor?.Resolve(States(WidgetState.Focused)));
        Assert.Equal(
            colorScheme.OnSurfaceVariant.WithOpacity(0.1),
            m3.YearOverlayColor?.Resolve(States(WidgetState.Pressed)));
        Assert.Equal(0, m3.RangePickerElevation);
        Assert.Equal(new RoundedRectangleBorder(), m3.RangePickerShape);
        Assert.Equal(MaterialColors.Transparent, m3.RangePickerShadowColor);
        Assert.Equal(MaterialColors.Transparent, m3.RangePickerSurfaceTintColor);
        Assert.Null(m3.RangeSelectionOverlayColor?.Resolve(NoStates));
        Assert.Equal(MaterialColors.Transparent, m3.RangePickerHeaderBackgroundColor);
        Assert.Equal(colorScheme.OnSurfaceVariant, m3.RangePickerHeaderForegroundColor);
        Assert.Equal(textTheme.TitleLarge, m3.RangePickerHeaderHeadlineStyle);
        Assert.Equal(textTheme.TitleSmall, m3.RangePickerHeaderHelpStyle);
        Assert.Null(m3.DividerColor);
        Assert.Null(m3.InputDecorationTheme);
        Assert.Equal(
            FrameworkDartTester.IgnoringHashCodes(TextButton.StyleFrom().ToString()),
            FrameworkDartTester.IgnoringHashCodes(m3.CancelButtonStyle!.ToString()));
        Assert.Equal(
            FrameworkDartTester.IgnoringHashCodes(TextButton.StyleFrom().ToString()),
            FrameworkDartTester.IgnoringHashCodes(m3.ConfirmButtonStyle!.ToString()));
        Assert.Null(m3.Locale);
        Assert.Equal(colorScheme.OnSurface.WithOpacity(0.60), m3.SubHeaderForegroundColor);
        Assert.Equal(textTheme.TitleSmall?.Apply(color: m3.SubHeaderForegroundColor), m3.ToggleButtonTextStyle);
    }

    // Flutter: "DatePickerTheme.defaults M2 defaults"
    [Fact]
    public void DatePickerThemeDefaultsM2Defaults()
    {
        using var tester = new FrameworkDartTester(fakeGestureTimers: true, semanticsEnabled: true);
        DatePickerThemeData? m2 = null;
        ThemeData? theme = null;
        tester.PumpWidget(new MaterialApp(
            theme: new ThemeData(useMaterial3: false),
            home: new Builder(context =>
            {
                m2 = Plumix.Material.DatePickerTheme.Defaults(context);
                theme = Theme.Of(context);
                return new Container();
            })));

        ColorScheme colorScheme = theme!.ColorScheme;
        TextTheme textTheme = theme.TextTheme;
        Assert.Equal(24, m2!.Elevation);
        Assert.Equal(
            new RoundedRectangleBorder(borderRadius: BorderRadius.All(Radius.Circular(4.0))),
            m2.Shape);
        Assert.Equal(colorScheme.Primary, m2.HeaderBackgroundColor);
        Assert.Equal(colorScheme.OnPrimary, m2.HeaderForegroundColor);
        Assert.Equal(textTheme.HeadlineSmall, m2.HeaderHeadlineStyle);
        Assert.Equal(textTheme.LabelSmall, m2.HeaderHelpStyle);
        Assert.Equal(textTheme.BodySmall?.Apply(color: colorScheme.OnSurface.WithOpacity(0.60)), m2.WeekdayStyle);
        Assert.Equal(textTheme.BodySmall, m2.DayStyle);
        Assert.Equal(colorScheme.OnSurface, m2.DayForegroundColor?.Resolve(NoStates));
        Assert.Equal(colorScheme.OnPrimary, m2.DayForegroundColor?.Resolve(States(WidgetState.Selected)));
        Assert.Equal(
            colorScheme.OnSurface.WithOpacity(0.38),
            m2.DayForegroundColor?.Resolve(States(WidgetState.Disabled)));
        Assert.Null(m2.DayBackgroundColor?.Resolve(NoStates));
        Assert.Equal(colorScheme.Primary, m2.DayBackgroundColor?.Resolve(States(WidgetState.Selected)));
        Assert.Null(m2.DayOverlayColor?.Resolve(NoStates));
        Assert.Equal(
            colorScheme.OnPrimary.WithOpacity(0.08),
            m2.DayOverlayColor?.Resolve(States(WidgetState.Selected, WidgetState.Hovered)));
        Assert.Equal(
            colorScheme.OnPrimary.WithOpacity(0.12),
            m2.DayOverlayColor?.Resolve(States(WidgetState.Selected, WidgetState.Focused)));
        Assert.Equal(
            colorScheme.OnPrimary.WithOpacity(0.38),
            m2.DayOverlayColor?.Resolve(States(WidgetState.Selected, WidgetState.Pressed)));
        Assert.Equal(
            colorScheme.OnPrimary.WithOpacity(0.08),
            m2.DayOverlayColor?.Resolve(States(WidgetState.Selected, WidgetState.Hovered, WidgetState.Focused)));
        Assert.Equal(
            colorScheme.OnPrimary.WithOpacity(0.38),
            m2.DayOverlayColor?.Resolve(States(WidgetState.Selected, WidgetState.Hovered, WidgetState.Pressed)));
        Assert.Equal(
            colorScheme.OnSurfaceVariant.WithOpacity(0.08),
            m2.DayOverlayColor?.Resolve(States(WidgetState.Hovered)));
        Assert.Equal(
            colorScheme.OnSurfaceVariant.WithOpacity(0.12),
            m2.DayOverlayColor?.Resolve(States(WidgetState.Focused)));
        Assert.Equal(
            colorScheme.OnSurfaceVariant.WithOpacity(0.12),
            m2.DayOverlayColor?.Resolve(States(WidgetState.Pressed)));
        Assert.Equal(new CircleBorder(), m2.DayShape?.Resolve(NoStates));
        Assert.Equal(colorScheme.Primary, m2.TodayForegroundColor?.Resolve(NoStates));
        Assert.Equal(
            colorScheme.OnSurface.WithOpacity(0.38),
            m2.TodayForegroundColor?.Resolve(States(WidgetState.Disabled)));
        Assert.Equal(new BorderSide(color: colorScheme.Primary), m2.TodayBorder);
        Assert.Equal(textTheme.BodyLarge, m2.YearStyle);
        Assert.Equal(colorScheme.Surface, m2.RangePickerBackgroundColor);
        Assert.Equal(0, m2.RangePickerElevation);
        Assert.Equal(new RoundedRectangleBorder(), m2.RangePickerShape);
        Assert.Equal(MaterialColors.Transparent, m2.RangePickerShadowColor);
        Assert.Equal(MaterialColors.Transparent, m2.RangePickerSurfaceTintColor);
        Assert.Null(m2.RangeSelectionOverlayColor?.Resolve(NoStates));
        Assert.Equal(
            colorScheme.OnPrimary.WithOpacity(0.08),
            m2.RangeSelectionOverlayColor?.Resolve(States(WidgetState.Selected, WidgetState.Hovered)));
        Assert.Equal(
            colorScheme.OnPrimary.WithOpacity(0.12),
            m2.RangeSelectionOverlayColor?.Resolve(States(WidgetState.Selected, WidgetState.Focused)));
        Assert.Equal(
            colorScheme.OnPrimary.WithOpacity(0.38),
            m2.RangeSelectionOverlayColor?.Resolve(States(WidgetState.Selected, WidgetState.Pressed)));
        Assert.Equal(
            colorScheme.OnSurfaceVariant.WithOpacity(0.08),
            m2.RangeSelectionOverlayColor?.Resolve(States(WidgetState.Hovered)));
        Assert.Equal(
            colorScheme.OnSurfaceVariant.WithOpacity(0.12),
            m2.RangeSelectionOverlayColor?.Resolve(States(WidgetState.Focused)));
        Assert.Equal(
            colorScheme.OnSurfaceVariant.WithOpacity(0.12),
            m2.RangeSelectionOverlayColor?.Resolve(States(WidgetState.Pressed)));
        Assert.Equal(colorScheme.Primary, m2.RangePickerHeaderBackgroundColor);
        Assert.Equal(colorScheme.OnPrimary, m2.RangePickerHeaderForegroundColor);
        Assert.Equal(textTheme.HeadlineSmall, m2.RangePickerHeaderHeadlineStyle);
        Assert.Equal(textTheme.LabelSmall, m2.RangePickerHeaderHelpStyle);
        Assert.Null(m2.DividerColor);
        Assert.Null(m2.InputDecorationTheme);
        Assert.Equal(
            FrameworkDartTester.IgnoringHashCodes(TextButton.StyleFrom().ToString()),
            FrameworkDartTester.IgnoringHashCodes(m2.CancelButtonStyle!.ToString()));
        Assert.Equal(
            FrameworkDartTester.IgnoringHashCodes(TextButton.StyleFrom().ToString()),
            FrameworkDartTester.IgnoringHashCodes(m2.ConfirmButtonStyle!.ToString()));
        Assert.Null(m2.Locale);
        Assert.Equal(new StadiumBorder(), m2.YearShape?.Resolve(NoStates));
        Assert.Equal(colorScheme.OnSurface.WithOpacity(0.60), m2.SubHeaderForegroundColor);
        Assert.Equal(textTheme.TitleSmall?.Apply(color: m2.SubHeaderForegroundColor), m2.ToggleButtonTextStyle);
    }

    // Flutter: "Default DatePickerThemeData debugFillProperties"
    [Fact]
    public void DefaultDatePickerThemeDataDebugFillProperties()
    {
        var builder = new DiagnosticPropertiesBuilder();
        new DatePickerThemeData().DebugFillProperties(builder);

        List<string> description = builder.Properties
            .Where(node => !node.IsFiltered(DiagnosticLevel.Info))
            .Select(node => node.ToString())
            .ToList();

        Assert.Empty(description);
    }

    // Flutter: "DatePickerThemeData implements debugFillProperties"
    [Fact]
    public void DatePickerThemeDataImplementsDebugFillProperties()
    {
        var builder = new DiagnosticPropertiesBuilder();
        DatePickerTheme.DebugFillProperties(builder);

        List<string> description = builder.Properties
            .Where(node => !node.IsFiltered(DiagnosticLevel.Info))
            .Select(node => FrameworkDartTester.IgnoringHashCodes(node.ToString()))
            .ToList();

        Assert.Equal(
            new[]
            {
                $"backgroundColor: {new Color(0xfffffff0)}",
                "elevation: 6.0",
                $"shadowColor: {new Color(0xfffffff1)}",
                $"surfaceTintColor: {new Color(0xfffffff2)}",
                "shape: RoundedRectangleBorder(BorderSide(width: 0.0, style: none), BorderRadius.zero)",
                $"headerBackgroundColor: {new Color(0xfffffff3)}",
                $"headerForegroundColor: {new Color(0xfffffff4)}",
                "headerHeadlineStyle: TextStyle(inherit: true, size: 10.0)",
                "headerHelpStyle: TextStyle(inherit: true, size: 11.0)",
                "weekDayStyle: TextStyle(inherit: true, size: 12.0)",
                "dayStyle: TextStyle(inherit: true, size: 13.0)",
                $"dayForegroundColor: WidgetStatePropertyAll({new Color(0xfffffff5)})",
                $"dayBackgroundColor: WidgetStatePropertyAll({new Color(0xfffffff6)})",
                $"dayOverlayColor: WidgetStatePropertyAll({new Color(0xfffffff7)})",
                "dayShape: WidgetStatePropertyAll(RoundedRectangleBorder(BorderSide(width: 0.0, style: none), "
                + "BorderRadius.zero))",
                $"todayForegroundColor: WidgetStatePropertyAll({new Color(0xfffffff8)})",
                $"todayBackgroundColor: WidgetStatePropertyAll({new Color(0xfffffff9)})",
                "todayBorder: BorderSide(color: Color(alpha: 0.0000, red: 0.0000, green: 0.0000, blue: 0.0000, "
                + "colorSpace: ColorSpace.sRGB), width: 3.0)",
                "yearStyle: TextStyle(inherit: true, size: 13.0)",
                $"yearForegroundColor: WidgetStatePropertyAll({new Color(0xfffffffa)})",
                $"yearBackgroundColor: WidgetStatePropertyAll({new Color(0xfffffffb)})",
                $"yearOverlayColor: WidgetStatePropertyAll({new Color(0xfffffffc)})",
                "yearShape: WidgetStatePropertyAll(RoundedRectangleBorder(BorderSide(width: 0.0, style: none), "
                + "BorderRadius.zero))",
                $"rangePickerBackgroundColor: {new Color(0xfffffffd)}",
                "rangePickerElevation: 7.0",
                $"rangePickerShadowColor: {new Color(0xfffffffe)}",
                $"rangePickerSurfaceTintColor: {new Color(0xffffffff)}",
                "rangePickerShape: RoundedRectangleBorder(BorderSide(width: 0.0, style: none), BorderRadius.zero)",
                $"rangePickerHeaderBackgroundColor: {new Color(0xffffff0f)}",
                $"rangePickerHeaderForegroundColor: {new Color(0xffffff1f)}",
                "rangePickerHeaderHeadlineStyle: TextStyle(inherit: true, size: 14.0)",
                "rangePickerHeaderHelpStyle: TextStyle(inherit: true, size: 15.0)",
                $"rangeSelectionBackgroundColor: {new Color(0xffffff2f)}",
                $"rangeSelectionOverlayColor: WidgetStatePropertyAll({new Color(0xffffff3f)})",
                $"dividerColor: {new Color(0xffffff4f)}",
                $"inputDecorationTheme: InputDecorationThemeData#00000(fillColor: {new Color(0xffffff5f)}, "
                + "border: UnderlineInputBorder())",
                $"cancelButtonStyle: ButtonStyle#00000(foregroundColor: WidgetStatePropertyAll("
                + $"{new Color(0xffffff6f)}))",
                $"confirmButtonStyle: ButtonStyle#00000(foregroundColor: WidgetStatePropertyAll("
                + $"{new Color(0xffffff7f)}))",
                "locale: en",
                "toggleButtonTextStyle: TextStyle(inherit: true, size: 13.0)",
                $"subHeaderForegroundColor: {new Color(0xffffff8f)}",
            },
            description);
    }

    private static MaterialWidget FindDialogMaterial(FrameworkDartTester tester)
    {
        return tester.Widget<MaterialWidget>(
            Find.Descendant(of: Find.ByType<Dialog>(), matching: Find.ByType<MaterialWidget>()).First);
    }

    private static MaterialWidget FindHeaderMaterial(FrameworkDartTester tester, string text)
    {
        return tester.Widget<MaterialWidget>(
            Find.Ancestor(of: Find.Text(text), matching: Find.ByType<MaterialWidget>()).First);
    }

    private static ShapeDecoration? FindTextDecoration(FrameworkDartTester tester, string date)
    {
        Container container = tester.Widget<Container>(
            Find.Ancestor(of: Find.Text(date), matching: Find.ByType<Container>()).First);
        return container.Decoration as ShapeDecoration;
    }

    private static ShapeDecoration? FindDayDecoration(FrameworkDartTester tester, string day)
    {
        return tester.Widget<Ink>(Find.Ancestor(of: Find.Text(day), matching: Find.ByType<Ink>()))
            .Decoration as ShapeDecoration;
    }

    private static ButtonStyle ActionButtonStyle(FrameworkDartTester tester, string text)
    {
        return tester.Widget<TextButton>(Find.WidgetWithText<TextButton>(text)).Style!;
    }

    private static RenderObject InkFeatures(FrameworkDartTester tester) =>
        tester.AllRenderObjects.First(renderObject => renderObject is RenderInkFeatures);

    private static FrameworkDartTester CreateTester() =>
        new(fakeGestureTimers: true, semanticsEnabled: true, registerTestTextInput: true);

    private static Widget DatePickerApp(
        Widget child,
        DatePickerThemeData? datePickerTheme = null,
        bool useMaterial3 = true) => new MaterialApp(
        theme: new ThemeData(datePickerTheme: datePickerTheme, useMaterial3: useMaterial3),
        home: new Directionality(
            textDirection: TextDirection.Ltr,
            child: new MaterialWidget(child: new Center(child: child))));

    private static DatePickerDialog Jan2023Dialog(
        DatePickerEntryMode initialEntryMode = DatePickerEntryMode.Calendar,
        DatePickerMode initialCalendarMode = DatePickerMode.Day) => new(
        initialEntryMode: initialEntryMode,
        initialDate: new DateTime(2023, 1, 25),
        firstDate: new DateTime(2022, 1, 1),
        lastDate: new DateTime(2024, 12, 31),
        currentDate: new DateTime(2023, 1, 24),
        initialCalendarMode: initialCalendarMode);

    private static DateRangePickerDialog Jan2023RangeDialog() => new(
        firstDate: new DateTime(2023, 1, 1),
        lastDate: new DateTime(2023, 1, 31),
        initialDateRange: new DateTimeRange<DateTime>(
            start: new DateTime(2023, 1, 17),
            end: new DateTime(2023, 1, 20)),
        currentDate: new DateTime(2023, 1, 23));

    private static string StyleString(ButtonStyle? style) =>
        FrameworkDartTester.IgnoringHashCodes(style?.ToString() ?? "null");

    // Flutter: "DatePickerDialog uses ThemeData datePicker theme (calendar mode)"
    [Fact]
    public void DatePickerDialogUsesThemeDataDatePickerThemeCalendarMode()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(DatePickerApp(Jan2023Dialog(), DatePickerTheme));

        MaterialWidget material = FindDialogMaterial(tester);
        Assert.Equal(DatePickerTheme.BackgroundColor, material.Color);
        Assert.Equal(DatePickerTheme.Elevation, material.Elevation);
        Assert.Equal(DatePickerTheme.ShadowColor, material.ShadowColor);
        Assert.Equal(DatePickerTheme.SurfaceTintColor, material.SurfaceTintColor);
        Assert.Equal(DatePickerTheme.Shape, material.Shape);

        Text selectDate = tester.Widget<Text>(Find.Text("Select date"));
        MaterialWidget headerMaterial = FindHeaderMaterial(tester, "Select date");
        Assert.Equal(DatePickerTheme.HeaderForegroundColor, selectDate.Style?.Color);
        Assert.Equal(DatePickerTheme.HeaderHelpStyle?.FontSize, selectDate.Style?.FontSize);
        Assert.Equal(DatePickerTheme.HeaderBackgroundColor, headerMaterial.Color);

        Text weekday = tester.Widget<Text>(Find.Text("W"));
        Assert.Equal(DatePickerTheme.WeekdayStyle?.Color, weekday.Style?.Color);
        Assert.Equal(DatePickerTheme.WeekdayStyle?.FontSize, weekday.Style?.FontSize);

        Text selectedDate = tester.Widget<Text>(Find.Text("Wed, Jan 25"));
        Assert.Equal(DatePickerTheme.HeaderForegroundColor, selectedDate.Style?.Color);
        Assert.Equal(DatePickerTheme.HeaderHeadlineStyle?.FontSize, selectedDate.Style?.FontSize);

        Text day31 = tester.Widget<Text>(Find.Text("31"));
        ShapeDecoration day31Decoration = FindDayDecoration(tester, "31")!;
        Assert.Equal(DatePickerTheme.DayForegroundColor?.Resolve(NoStates), day31.Style?.Color);
        Assert.Equal(DatePickerTheme.DayStyle?.FontSize, day31.Style?.FontSize);
        Assert.Equal(DatePickerTheme.DayBackgroundColor?.Resolve(NoStates), day31Decoration.Color);
        Assert.Equal(DatePickerTheme.DayShape?.Resolve(NoStates), day31Decoration.Shape);

        Text day24 = tester.Widget<Text>(Find.Text("24")); // DatePickerDialog.currentDate
        ShapeDecoration day24Decoration = FindDayDecoration(tester, "24")!;
        var day24Shape = (OutlinedBorder)day24Decoration.Shape;
        Assert.Equal(DatePickerTheme.DayStyle?.FontSize, day24.Style?.FontSize);
        Assert.Equal(DatePickerTheme.TodayForegroundColor?.Resolve(NoStates), day24.Style?.Color);
        Assert.Equal(DatePickerTheme.TodayBackgroundColor?.Resolve(NoStates), day24Decoration.Color);
        Assert.Equal(
            DatePickerTheme.DayShape?.Resolve(NoStates)!.CopyWith(
                side: DatePickerTheme.TodayBorder?.CopyWith(
                    color: DatePickerTheme.TodayForegroundColor?.Resolve(NoStates))),
            day24Decoration.Shape);
        Assert.Equal(DatePickerTheme.TodayBorder?.Width, day24Shape.Side.Width);

        // Test the toggle mode button style.
        Text january2023 = tester.Widget<Text>(Find.Text("January 2023"));
        Assert.Equal(DatePickerTheme.ToggleButtonTextStyle?.FontSize, january2023.Style?.FontSize);
        Assert.Equal(DatePickerTheme.SubHeaderForegroundColor, january2023.Style?.Color);
        Icon arrowIcon = tester.Widget<Icon>(Find.ByIcon(Icons.ArrowDropDown));
        Assert.Equal(DatePickerTheme.SubHeaderForegroundColor, arrowIcon.Color);

        // Test the day overlay color.
        RenderObject inkFeatures = InkFeatures(tester);
        TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        gesture.AddPointer();
        gesture.MoveTo(tester.GetCenter(Find.Text("25")));
        tester.PumpAndSettle();
        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints.Circle(color: DatePickerTheme.DayOverlayColor?.Resolve(NoStates)));

        // Show the year selector.
        tester.Tap(Find.Text("January 2023"));
        tester.PumpAndSettle();

        Text year2022 = tester.Widget<Text>(Find.Text("2022"));
        ShapeDecoration year2022Decoration = FindTextDecoration(tester, "2022")!;
        Assert.Equal(DatePickerTheme.YearStyle?.FontSize, year2022.Style?.FontSize);
        Assert.Equal(DatePickerTheme.YearForegroundColor?.Resolve(NoStates), year2022.Style?.Color);
        Assert.Equal(DatePickerTheme.YearBackgroundColor?.Resolve(NoStates), year2022Decoration.Color);
        Assert.Equal(DatePickerTheme.YearShape?.Resolve(NoStates), year2022Decoration.Shape);

        Text year2023 = tester.Widget<Text>(Find.Text("2023")); // DatePickerDialog.currentDate
        ShapeDecoration year2023Decoration = FindTextDecoration(tester, "2023")!;
        Assert.Equal(DatePickerTheme.YearStyle?.FontSize, year2023.Style?.FontSize);
        Assert.Equal(DatePickerTheme.TodayForegroundColor?.Resolve(NoStates), year2023.Style?.Color);
        Assert.Equal(DatePickerTheme.TodayBackgroundColor?.Resolve(NoStates), year2023Decoration.Color);
        var roundedRectangleBorder = (RoundedRectangleBorder)year2023Decoration.Shape;
        Assert.Equal(DatePickerTheme.TodayBorder?.Width, roundedRectangleBorder.Side.Width);
        Assert.Equal(DatePickerTheme.TodayForegroundColor?.Resolve(NoStates), roundedRectangleBorder.Side.Color);

        // Test the year overlay color.
        gesture.MoveTo(tester.GetCenter(Find.Text("2024")));
        tester.PumpAndSettle();
        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints.Rect(color: DatePickerTheme.YearOverlayColor?.Resolve(NoStates)));

        ButtonStyle cancelButtonStyle = ActionButtonStyle(tester, "Cancel");
        Assert.Equal(StyleString(DatePickerTheme.CancelButtonStyle), StyleString(cancelButtonStyle));

        ButtonStyle confirmButtonStyle = ActionButtonStyle(tester, "OK");
        Assert.Equal(StyleString(DatePickerTheme.ConfirmButtonStyle), StyleString(confirmButtonStyle));
    }

    // Flutter: "DatePickerDialog uses ThemeData datePicker theme (input mode)"
    [Fact]
    public void DatePickerDialogUsesThemeDataDatePickerThemeInputMode()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(DatePickerApp(Jan2023Dialog(DatePickerEntryMode.Input), DatePickerTheme));

        MaterialWidget material = FindDialogMaterial(tester);
        Assert.Equal(DatePickerTheme.BackgroundColor, material.Color);
        Assert.Equal(DatePickerTheme.Elevation, material.Elevation);
        Assert.Equal(DatePickerTheme.ShadowColor, material.ShadowColor);
        Assert.Equal(DatePickerTheme.SurfaceTintColor, material.SurfaceTintColor);
        Assert.Equal(DatePickerTheme.Shape, material.Shape);

        Text selectDate = tester.Widget<Text>(Find.Text("Select date"));
        MaterialWidget headerMaterial = FindHeaderMaterial(tester, "Select date");
        Assert.Equal(DatePickerTheme.HeaderForegroundColor, selectDate.Style?.Color);
        Assert.Equal(DatePickerTheme.HeaderHelpStyle?.FontSize, selectDate.Style?.FontSize);
        Assert.Equal(DatePickerTheme.HeaderBackgroundColor, headerMaterial.Color);

        InputDecoration inputDecoration = tester.Widget<TextField>(Find.ByType<TextField>()).Decoration!;
        Assert.Equal(DatePickerTheme.InputDecorationTheme?.FillColor, inputDecoration.FillColor);

        ButtonStyle cancelButtonStyle = ActionButtonStyle(tester, "Cancel");
        Assert.Equal(StyleString(DatePickerTheme.CancelButtonStyle), StyleString(cancelButtonStyle));

        ButtonStyle confirmButtonStyle = ActionButtonStyle(tester, "OK");
        Assert.Equal(StyleString(DatePickerTheme.ConfirmButtonStyle), StyleString(confirmButtonStyle));
    }

    private static void ExpectRangePickerTheme(FrameworkDartTester tester, string helpText)
    {
        MaterialWidget material = FindDialogMaterial(tester);
        Assert.Equal(DatePickerTheme.BackgroundColor, material.Color);
        Assert.Equal(
            DatePickerTheme.RangePickerBackgroundColor,
            tester.Widget<Scaffold>(Find.ByType<Scaffold>()).BackgroundColor);
        Assert.Equal(DatePickerTheme.RangePickerElevation, material.Elevation);
        Assert.Equal(DatePickerTheme.RangePickerShadowColor, material.ShadowColor);
        Assert.Equal(DatePickerTheme.RangePickerSurfaceTintColor, material.SurfaceTintColor);
        Assert.Equal(DatePickerTheme.RangePickerShape, material.Shape);

        AppBar appBar = tester.Widget<AppBar>(Find.ByType<AppBar>());
        Assert.Equal(DatePickerTheme.RangePickerHeaderBackgroundColor, appBar.BackgroundColor);

        Text selectRange = tester.Widget<Text>(Find.Text(helpText));
        Assert.Equal(DatePickerTheme.RangePickerHeaderForegroundColor, selectRange.Style?.Color);
        Assert.Equal(DatePickerTheme.RangePickerHeaderHelpStyle?.FontSize, selectRange.Style?.FontSize);

        Text selectedDate = tester.Widget<Text>(Find.Text("Jan 17"));
        Assert.Equal(DatePickerTheme.RangePickerHeaderForegroundColor, selectedDate.Style?.Color);
        Assert.Equal(DatePickerTheme.RangePickerHeaderHeadlineStyle?.FontSize, selectedDate.Style?.FontSize);

        // Test the day overlay color.
        RenderObject inkFeatures = InkFeatures(tester);
        TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        gesture.AddPointer();
        gesture.MoveTo(tester.GetCenter(Find.Text("16")));
        tester.PumpAndSettle();
        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints.Circle(color: DatePickerTheme.DayOverlayColor?.Resolve(NoStates)));

        // Test the range selection overlay color.
        gesture.MoveTo(tester.GetCenter(Find.Text("18")));
        tester.PumpAndSettle();
        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints.Circle(color: DatePickerTheme.RangeSelectionOverlayColor?.Resolve(NoStates)));
    }

    // Flutter: "DateRangePickerDialog uses ThemeData datePicker theme"
    [Fact]
    public void DateRangePickerDialogUsesThemeDataDatePickerTheme()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(DatePickerApp(Jan2023RangeDialog(), DatePickerTheme));
        ExpectRangePickerTheme(tester, "Select range");
    }

    // Flutter: "Material2 - DateRangePickerDialog uses ThemeData datePicker theme"
    [Fact]
    public void Material2DateRangePickerDialogUsesThemeDataDatePickerTheme()
    {
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(DatePickerApp(Jan2023RangeDialog(), DatePickerTheme, useMaterial3: false));
        ExpectRangePickerTheme(tester, "SELECT RANGE");
    }

    // Flutter: "Dividers use DatePickerThemeData.dividerColor"
    [Fact]
    public void DividersUseDatePickerThemeDataDividerColor()
    {
        using FrameworkDartTester tester = CreateTester();

        void ShowPicker(Size size)
        {
            tester.View.PhysicalSize = size;
            tester.View.DevicePixelRatio = 1.0;
            tester.PumpWidget(DatePickerApp(Jan2023Dialog(), DatePickerTheme));
        }

        try
        {
            ShowPicker(WideWindowSize);

            // Test vertical divider.
            VerticalDivider verticalDivider = tester.Widget<VerticalDivider>(Find.ByType<VerticalDivider>());
            Assert.Equal(DatePickerTheme.DividerColor, verticalDivider.Color);

            // Test portrait layout.
            ShowPicker(NarrowWindowSize);

            // Test horizontal divider.
            Divider horizontalDivider = tester.Widget<Divider>(Find.ByType<Divider>());
            Assert.Equal(DatePickerTheme.DividerColor, horizontalDivider.Color);
        }
        finally
        {
            tester.View.Reset();
        }
    }

    // Flutter: "DatePicker uses ThemeData.inputDecorationTheme properties which are null in
    // DatePickerThemeData.inputDecorationTheme"
    [Fact]
    public void DatePickerUsesThemeDataInputDecorationThemePropertiesWhichAreNullInDatePickerTheme()
    {
        using FrameworkDartTester tester = CreateTester();

        Widget BuildWidget(InputDecorationThemeData? inputDecorationTheme = null, DatePickerThemeData? theme = null)
        {
            return new MaterialApp(
                theme: new ThemeData(inputDecorationTheme: inputDecorationTheme, datePickerTheme: theme),
                home: new Directionality(
                    textDirection: TextDirection.Ltr,
                    child: new MaterialWidget(
                        child: new Center(child: Jan2023Dialog(DatePickerEntryMode.Input)))));
        }

        // Test DatePicker with DatePickerThemeData.inputDecorationTheme.
        tester.PumpWidget(BuildWidget(
            inputDecorationTheme: new InputDecorationThemeData(filled: true),
            theme: DatePickerTheme));
        InputDecoration inputDecoration = tester.Widget<TextField>(Find.ByType<TextField>()).Decoration!;
        Assert.Equal(DatePickerTheme.InputDecorationTheme!.FillColor, inputDecoration.FillColor);
        Assert.Equal(DatePickerTheme.InputDecorationTheme!.Border, inputDecoration.Border);

        // Test DatePicker with ThemeData.inputDecorationTheme.
        tester.PumpWidget(BuildWidget(
            inputDecorationTheme: new InputDecorationThemeData(
                filled: true,
                fillColor: new Color(0xFF00FF00),
                border: new OutlineInputBorder())));
        tester.PumpAndSettle();

        inputDecoration = tester.Widget<TextField>(Find.ByType<TextField>()).Decoration!;
        Assert.Equal(new Color(0xFF00FF00), inputDecoration.FillColor);
        Assert.Equal(new OutlineInputBorder(), inputDecoration.Border);
    }

    // Flutter: "DatePickerDialog resolves DatePickerTheme.dayOverlayColor states"
    [Fact]
    public void DatePickerDialogResolvesDatePickerThemeDayOverlayColorStates()
    {
        WidgetStateProperty<Color?> dayOverlayColor = WidgetStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.Contains(WidgetState.Hovered))
            {
                return new Color(0xff00ff00);
            }

            if (states.Contains(WidgetState.Focused))
            {
                return new Color(0xffff00ff);
            }

            if (states.Contains(WidgetState.Pressed))
            {
                return new Color(0xffffff00);
            }

            return MaterialColors.Transparent;
        });

        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(DatePickerApp(
            new Focus(child: Jan2023Dialog()),
            new DatePickerThemeData(dayOverlayColor: dayOverlayColor)));

        // All days are painted on the same Material widget.
        // Use an arbitrary day to find this Material.
        RenderObject FindDayGridMaterial() => (RenderObject)MaterialWidget.Of(tester.Element(Find.Text("17")));

        // Test the hover overlay color.
        TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        gesture.AddPointer();
        gesture.MoveTo(tester.GetCenter(Find.Text("20")));
        tester.PumpAndSettle();

        PaintAssert.Paints(
            FindDayGridMaterial(),
            PaintPattern.Paints
                .Circle() // Today decoration.
                .Circle() // Selected day decoration.
                .Circle(color: dayOverlayColor.Resolve(States(WidgetState.Hovered))));

        // Test the pressed overlay color.
        gesture.Down(tester.GetCenter(Find.Text("20")));
        tester.PumpAndSettle();
        PaintAssert.Paints(
            FindDayGridMaterial(),
            PaintPattern.Paints
                .Circle() // Today decoration.
                .Circle() // Selected day decoration.
                .Circle(color: dayOverlayColor.Resolve(States(WidgetState.Hovered)))
                .Circle(color: dayOverlayColor.Resolve(States(WidgetState.Pressed))));

        gesture.RemovePointer();
        tester.PumpAndSettle();

        // Focus day selection.
        for (int i = 0; i < 5; i++)
        {
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);
            tester.PumpAndSettle();
        }

        // Test the focused overlay color.
        PaintAssert.Paints(
            FindDayGridMaterial(),
            PaintPattern.Paints
                .Circle() // Today decoration.
                .Circle() // Selected day decoration.
                .Circle(color: dayOverlayColor.Resolve(States(WidgetState.Focused))));
    }

    // Flutter: "DatePickerDialog resolves DatePickerTheme.yearOverlayColor states"
    [Fact]
    public void DatePickerDialogResolvesDatePickerThemeYearOverlayColorStates()
    {
        WidgetStateProperty<Color?> yearOverlayColor = WidgetStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.Contains(WidgetState.Hovered))
            {
                return new Color(0xff00ff00);
            }

            if (states.Contains(WidgetState.Focused))
            {
                return new Color(0xffff00ff);
            }

            if (states.Contains(WidgetState.Pressed))
            {
                return new Color(0xffffff00);
            }

            return MaterialColors.Transparent;
        });

        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(DatePickerApp(
            new Focus(child: Jan2023Dialog(initialCalendarMode: DatePickerMode.Year)),
            new DatePickerThemeData(yearOverlayColor: yearOverlayColor)));

        // Test the hover overlay color.
        RenderObject inkFeatures = InkFeatures(tester);
        TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        gesture.AddPointer();
        gesture.MoveTo(tester.GetCenter(Find.Text("2022")));
        tester.PumpAndSettle();
        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints.Rect(color: yearOverlayColor.Resolve(States(WidgetState.Hovered))));

        // Test the pressed overlay color.
        gesture.Down(tester.GetCenter(Find.Text("2022")));
        tester.PumpAndSettle();
        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints
                .Rect(color: yearOverlayColor.Resolve(States(WidgetState.Hovered)))
                .Rect(color: yearOverlayColor.Resolve(States(WidgetState.Pressed))));

        gesture.RemovePointer();
        tester.PumpAndSettle();

        // Focus year selection.
        for (int i = 0; i < 3; i++)
        {
            tester.SendKeyEvent(LogicalKeyboardKey.Tab);
            tester.PumpAndSettle();
        }

        // Test the focused overlay color.
        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints.Rect(color: yearOverlayColor.Resolve(States(WidgetState.Focused))));
    }

    // Flutter: "DateRangePickerDialog resolves DatePickerTheme.rangeSelectionOverlayColor states"
    [Fact]
    public void DateRangePickerDialogResolvesDatePickerThemeRangeSelectionOverlayColorStates()
    {
        WidgetStateProperty<Color?> rangeSelectionOverlayColor = WidgetStateProperty<Color?>.ResolveWith(states =>
        {
            if (states.Contains(WidgetState.Hovered))
            {
                return new Color(0xff00ff00);
            }

            if (states.Contains(WidgetState.Pressed))
            {
                return new Color(0xffffff00);
            }

            return MaterialColors.Transparent;
        });

        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(DatePickerApp(
            Jan2023RangeDialog(),
            new DatePickerThemeData(rangeSelectionOverlayColor: rangeSelectionOverlayColor)));

        // Test the hover overlay color.
        RenderObject inkFeatures = InkFeatures(tester);
        TestGesture gesture = tester.CreateGesture(kind: PointerDeviceKind.Mouse);
        gesture.AddPointer();
        gesture.MoveTo(tester.GetCenter(Find.Text("18")));
        tester.PumpAndSettle();
        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints.Circle(color: rangeSelectionOverlayColor.Resolve(States(WidgetState.Hovered))));

        // Test the pressed overlay color.
        gesture.Down(tester.GetCenter(Find.Text("18")));
        tester.PumpAndSettle();
        PaintAssert.Paints(
            inkFeatures,
            PaintPattern.Paints
                .Circle(color: rangeSelectionOverlayColor.Resolve(States(WidgetState.Hovered)))
                .Circle(color: rangeSelectionOverlayColor.Resolve(States(WidgetState.Pressed))));
    }

    // Flutter: "YearPicker maintains default year shape at textScaleFactor 1, 1.5, 2"
    [Fact]
    public void YearPickerMaintainsDefaultYearShapeAtTextScaleFactor112()
    {
        using FrameworkDartTester tester = CreateTester();
        double textScaleFactor = 1.0;
        Widget BuildFrame()
        {
            return new MaterialApp(
                home: new Builder(context => MediaQuery.WithClampedTextScaling(
                    minScaleFactor: textScaleFactor,
                    maxScaleFactor: textScaleFactor,
                    child: new Scaffold(
                        body: new YearPicker(
                            currentDate: new DateTime(2025, 1, 1),
                            firstDate: new DateTime(2021, 1, 1),
                            lastDate: new DateTime(2030, 1, 1),
                            selectedDate: new DateTime(2025, 1, 1),
                            onChanged: _ => { })))));
        }

        tester.PumpWidget(BuildFrame());

        // Find container whose child is text 2025.
        Finder yearContainer = Find.Ancestor(of: Find.Text("2025"), matching: Find.ByType<Container>()).First;

        PaintAssert.Paints(
            tester.RenderObject<RenderObject>(yearContainer),
            PaintPattern.Paints.RRect(
                rrect: RRect.FromLTRBR(0.5, 0.5, 71.5, 35.5, Radius.Circular(17.5)),
                color: new Color(0xFF6750A4)));

        textScaleFactor = 1.5;
        tester.PumpWidget(BuildFrame());

        PaintAssert.Paints(
            tester.RenderObject<RenderObject>(yearContainer),
            PaintPattern.Paints.RRect(
                rrect: RRect.FromLTRBR(0.5, 0.5, 107.5, 51.5, Radius.Circular(25.5)),
                color: new Color(0xFF6750A4)));

        textScaleFactor = 2;
        tester.PumpWidget(BuildFrame());

        PaintAssert.Paints(
            tester.RenderObject<RenderObject>(yearContainer),
            PaintPattern.Paints.RRect(
                rrect: RRect.FromLTRBR(0.5, 0.5, 143.5, 51.5, Radius.Circular(25.5)),
                color: new Color(0xFF6750A4)));
    }

    // Flutter: "YearPicker applies shape from DatePickerThemeData.yearShape correctly"
    [Fact]
    public void YearPickerAppliesShapeFromDatePickerThemeDataYearShapeCorrectly()
    {
        OutlinedBorder yearShpae = new CircleBorder();
        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(DatePickerApp(
            new YearPicker(
                currentDate: new DateTime(2025, 1, 1),
                firstDate: new DateTime(2021, 1, 1),
                lastDate: new DateTime(2030, 1, 1),
                selectedDate: new DateTime(2025, 1, 1),
                onChanged: _ => { }),
            DatePickerTheme.CopyWith(yearShape: WidgetStateProperty<OutlinedBorder?>.All(yearShpae))));

        ShapeDecoration year2022Decoration = FindTextDecoration(tester, "2022")!;
        OutlinedBorder year2022roundedRectangleBorder = (CircleBorder)year2022Decoration.Shape;
        Assert.Equal(0.0, year2022roundedRectangleBorder.Side.Width);
        Assert.Equal(yearShpae.Side.Color, year2022roundedRectangleBorder.Side.Color);

        ShapeDecoration year2025Decoration = FindTextDecoration(tester, "2025")!;
        OutlinedBorder year2022RoundedRectangleBorder = (CircleBorder)year2025Decoration.Shape;
        Assert.Equal(DatePickerTheme.TodayBorder?.Width, year2022RoundedRectangleBorder.Side.Width);
        Assert.Equal(
            DatePickerTheme.TodayForegroundColor?.Resolve(NoStates),
            year2022RoundedRectangleBorder.Side.Color);
    }

    // Flutter: "Toggle button uses DatePickerTheme.toggleButtonTextStyle.color when it is defined"
    [Fact]
    public void ToggleButtonUsesDatePickerThemeToggleButtonTextStyleColorWhenItIsDefined()
    {
        var toggleButtonTextColor = new Color(0xff00ff00);
        var subHeaderForegroundColor = new Color(0xffff0000);

        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(new MaterialApp(
            theme: new ThemeData(
                datePickerTheme: new DatePickerThemeData(
                    toggleButtonTextStyle: new TextStyle(Color: toggleButtonTextColor),
                    subHeaderForegroundColor: subHeaderForegroundColor)),
            home: Jan2023Dialog()));

        Text toggleButtonText = tester.Widget<Text>(Find.Text("January 2023"));
        Assert.Equal(toggleButtonTextColor, toggleButtonText.Style?.Color);
    }

    // Flutter: "Toggle button uses DatePickerTheme.subHeaderForegroundColor when
    // DatePickerTheme.toggleButtonTextStyle.color is not defined"
    [Fact]
    public void ToggleButtonUsesDatePickerThemeSubHeaderForegroundColorWhenToggleButtonTextStyleColorIsNotDefined()
    {
        var subHeaderForegroundColor = new Color(0xffff0000);

        using FrameworkDartTester tester = CreateTester();
        tester.PumpWidget(new MaterialApp(
            theme: new ThemeData(
                datePickerTheme: new DatePickerThemeData(
                    toggleButtonTextStyle: new TextStyle(),
                    subHeaderForegroundColor: subHeaderForegroundColor)),
            home: Jan2023Dialog()));

        Text toggleButtonText = tester.Widget<Text>(Find.Text("January 2023"));
        Assert.Equal(subHeaderForegroundColor, toggleButtonText.Style?.Color);
    }


    // ---- C#-only coverage of date_picker_theme.dart members Flutter's tests do not reach ----

    [Fact]
    public void LerpSwitchesDiscreteFieldsAtTheMidpointAndPortsLerpBorderSide()
    {
        var a = new DatePickerThemeData(
            locale: new Locale("en"),
            inputDecorationTheme: new InputDecorationThemeData(filled: true),
            todayBorder: new BorderSide(color: new Color(0xFF0000FF), width: 4));
        var b = new DatePickerThemeData(
            locale: new Locale("fr"),
            todayBorder: new BorderSide(color: new Color(0xFF00FF00), width: 2));

        Assert.Equal(new Locale("en"), DatePickerThemeData.Lerp(a, b, 0.25).Locale);
        Assert.Equal(new Locale("fr"), DatePickerThemeData.Lerp(a, b, 0.75).Locale);
        Assert.Same(a.InputDecorationTheme, DatePickerThemeData.Lerp(a, b, 0.25).InputDecorationTheme);
        Assert.Null(DatePickerThemeData.Lerp(a, b, 0.75).InputDecorationTheme);

        // Dart's `_lerpBorderSide` lerps a non-null `a` toward a transparent, zero-width copy of itself,
        // ignoring `b`.
        BorderSide half = DatePickerThemeData.Lerp(a, b, 0.5).TodayBorder!.Value;
        Assert.Equal(2.0, half.Width);
        Assert.Equal(new Color(0xFF0000FF).WithAlpha(0x80).Value, half.Color.Value, 1u);
        // From null it grows `b` from a transparent, zero-width copy.
        BorderSide fromNull = DatePickerThemeData.Lerp(new DatePickerThemeData(), b, 0.5).TodayBorder!.Value;
        Assert.Equal(1.0, fromNull.Width);
        Assert.Null(DatePickerThemeData.Lerp(new DatePickerThemeData(), new DatePickerThemeData(), 0.5).TodayBorder);
    }

    [Fact]
    public void InputDecorationThemeAcceptsTheWidgetOrItsData()
    {
        var data = new InputDecorationThemeData(filled: true);
        Assert.Same(data, new DatePickerThemeData(inputDecorationTheme: data).InputDecorationTheme);
        var widget = new InputDecorationTheme(data: data);
        Assert.Same(widget.Data, new DatePickerThemeData(inputDecorationTheme: widget).InputDecorationTheme);
        Assert.Same(
            widget.Data,
            new DatePickerThemeData().CopyWith(inputDecorationTheme: widget).InputDecorationTheme);
        Assert.Same(data, new DatePickerThemeData(inputDecorationTheme: data).CopyWith().InputDecorationTheme);
    }

    [DebugOnlyFact]
    public void InputDecorationThemeAssertsItsType()
    {
        Assert.Throws<AssertionError>(() => new DatePickerThemeData(inputDecorationTheme: "not a theme"));
    }

    [Fact]
    public void DatePickerThemeOfMaybeOfAndWrapFollowTheInheritedTheme()
    {
        using FrameworkDartTester tester = CreateTester();
        var local = new DatePickerThemeData(backgroundColor: new Color(0xFF123456));
        DatePickerThemeData? ofOutside = null;
        DatePickerThemeData? maybeOfOutside = null;
        DatePickerThemeData? captured = null;
        tester.PumpWidget(new MaterialApp(
            theme: new ThemeData(datePickerTheme: DatePickerTheme),
            home: new Column(children:
            [
                new Builder(context =>
                {
                    ofOutside = Plumix.Material.DatePickerTheme.Of(context);
                    maybeOfOutside = Plumix.Material.DatePickerTheme.MaybeOf(context);
                    return new SizedBox();
                }),
                new Plumix.Material.DatePickerTheme(
                    data: local,
                    child: new Builder(context =>
                    {
                        CapturedThemes themes = InheritedTheme.Capture(from: context, to: null);
                        return new Plumix.Material.DatePickerTheme(
                            data: new DatePickerThemeData(),
                            child: themes.Wrap(new Builder(inner =>
                            {
                                captured = Plumix.Material.DatePickerTheme.Of(inner);
                                return new SizedBox();
                            })));
                    })),
            ])));

        Assert.Same(DatePickerTheme, ofOutside);
        Assert.Null(maybeOfOutside);
        Assert.Same(local, captured);
    }

    [Fact]
    public void DefaultsCompareByTheirGetterValuesWithoutARuntimeTypeCheck()
    {
        using FrameworkDartTester tester = CreateTester();
        DatePickerThemeData? m3 = null;
        tester.PumpWidget(new MaterialApp(home: new Builder(context =>
        {
            m3 = Plumix.Material.DatePickerTheme.Defaults(context);
            return new SizedBox();
        })));

        // Dart's `==` has no runtimeType check: only the state-resolving getters, which build a new
        // property on every read, keep the defaults from equalling a plain copy of their values.
        Assert.NotEqual(new DatePickerThemeData(), m3);
        Assert.Same(m3!.CopyWith().Shape, m3.Shape);
    }
}
