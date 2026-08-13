using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;
using System.Reflection;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// material_ui/lib/src/theme.dart
// material_ui/lib/src/theme_data.dart
// material_ui/lib/src/page.dart
// material_ui/lib/src/page_transitions_theme.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialThemeAnimationTests : IDisposable
{
    public MaterialThemeAnimationTests()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
        NavigatorBackButtonDispatcher.ResetForTests();
    }

    [Fact]
    public void ThemeData_Lerp_InterpolatesColorsTypographyIconsAndDensity_AndSnapsDiscreteValues()
    {
        var begin = new ThemeData(
            brightness: Brightness.Light,
            primaryColor: Color.FromArgb(255, 0, 20, 40),
            textTheme: new MaterialTextTheme(
                bodyMedium: MaterialTextTheme.DefaultBodyMedium.CopyWith(fontSize: 10)),
            iconTheme: new IconThemeData(Color: Colors.Black, Size: 16),
            visualDensity: new VisualDensity(-2, 0),
            useMaterial3: false,
            platform: TargetPlatform.Windows,
            materialTapTargetSize: MaterialTapTargetSize.Padded,
            inputDecorationTheme: new InputDecorationThemeData(IsDense: false),
            buttonTheme: new ButtonThemeData(AlignedDropdown: false),
            splashFactory: Plumix.Material.InkSplash.SplashFactory,
            applyElevationOverlayColor: false);
        var end = new ThemeData(
            brightness: Brightness.Dark,
            primaryColor: Color.FromArgb(255, 100, 120, 140),
            textTheme: new MaterialTextTheme(
                bodyMedium: MaterialTextTheme.DefaultBodyMedium.CopyWith(fontSize: 20)),
            iconTheme: new IconThemeData(Color: Colors.White, Size: 24),
            visualDensity: new VisualDensity(2, 4),
            useMaterial3: true,
            platform: TargetPlatform.Linux,
            materialTapTargetSize: MaterialTapTargetSize.ShrinkWrap,
            inputDecorationTheme: new InputDecorationThemeData(IsDense: true),
            buttonTheme: new ButtonThemeData(AlignedDropdown: true),
            splashFactory: InkRipple.SplashFactory,
            applyElevationOverlayColor: true);

        ThemeData firstHalf = ThemeData.Lerp(begin, end, 0.25);
        ThemeData midpoint = ThemeData.Lerp(begin, end, 0.5);

        Assert.Same(begin, ThemeData.Lerp(begin, begin, 0.25));
        Assert.NotSame(begin, ThemeData.Lerp(begin, end, 0.0));
        Assert.NotSame(end, ThemeData.Lerp(begin, end, 1.0));
        Assert.Equal(Color.FromArgb(255, 25, 45, 65), firstHalf.PrimaryColor);
        Assert.Equal(Color.FromArgb(255, 50, 70, 90), midpoint.PrimaryColor);
        Assert.Equal(15, midpoint.TextTheme.BodyMedium.FontSize);
        Assert.Equal(20, midpoint.IconTheme.Size);
        Assert.Equal(new VisualDensity(0, 2), midpoint.VisualDensity);
        Assert.Equal(Brightness.Light, firstHalf.Brightness);
        Assert.False(firstHalf.UseMaterial3);
        Assert.Equal(Brightness.Dark, midpoint.Brightness);
        Assert.True(midpoint.UseMaterial3);
        Assert.Equal(TargetPlatform.Windows, firstHalf.Platform);
        Assert.Equal(TargetPlatform.Linux, midpoint.Platform);
        Assert.Equal(MaterialTapTargetSize.Padded, firstHalf.MaterialTapTargetSize);
        Assert.Equal(MaterialTapTargetSize.ShrinkWrap, midpoint.MaterialTapTargetSize);
        Assert.False(firstHalf.InputDecorationTheme.IsDense);
        Assert.True(midpoint.InputDecorationTheme.IsDense);
        Assert.False(firstHalf.ButtonTheme.AlignedDropdown);
        Assert.True(midpoint.ButtonTheme.AlignedDropdown);
        Assert.Same(Plumix.Material.InkSplash.SplashFactory, firstHalf.SplashFactory);
        Assert.Same(InkRipple.SplashFactory, midpoint.SplashFactory);
        Assert.False(firstHalf.ApplyElevationOverlayColor);
        Assert.True(midpoint.ApplyElevationOverlayColor);
    }

    [Fact]
    public void ThemeData_Lerp_DelegatesToEveryContinuousComponentThemeContract()
    {
        Type[] componentThemeTypes =
        [
            typeof(ActionIconThemeData),
            typeof(AppBarThemeData),
            typeof(BadgeThemeData),
            typeof(MaterialBannerThemeData),
            typeof(BottomAppBarThemeData),
            typeof(BottomNavigationBarThemeData),
            typeof(BottomSheetThemeData),
            typeof(ButtonBarThemeData),
            typeof(CardThemeData),
            typeof(CarouselViewThemeData),
            typeof(CheckboxThemeData),
            typeof(ChipThemeData),
            typeof(DataTableThemeData),
            typeof(DatePickerThemeData),
            typeof(DialogThemeData),
            typeof(DividerThemeData),
            typeof(DrawerThemeData),
            typeof(DropdownMenuThemeData),
            typeof(ElevatedButtonThemeData),
            typeof(ExpansionTileThemeData),
            typeof(FilledButtonThemeData),
            typeof(FloatingActionButtonThemeData),
            typeof(IconButtonThemeData),
            typeof(ListTileThemeData),
            typeof(MenuBarThemeData),
            typeof(MenuButtonThemeData),
            typeof(MenuThemeData),
            typeof(NavigationBarThemeData),
            typeof(NavigationDrawerThemeData),
            typeof(NavigationRailThemeData),
            typeof(OutlinedButtonThemeData),
            typeof(PopupMenuThemeData),
            typeof(ProgressIndicatorThemeData),
            typeof(RadioThemeData),
            typeof(ScrollbarThemeData),
            typeof(SearchBarThemeData),
            typeof(SearchViewThemeData),
            typeof(SegmentedButtonThemeData),
            typeof(SliderThemeData),
            typeof(SnackBarThemeData),
            typeof(SwitchThemeData),
            typeof(TabBarThemeData),
            typeof(TextButtonThemeData),
            typeof(TextSelectionThemeData),
            typeof(TimePickerThemeData),
            typeof(ToggleButtonsThemeData),
            typeof(TooltipThemeData),
        ];

        foreach (Type componentThemeType in componentThemeTypes)
        {
            MethodInfo? lerp = componentThemeType.GetMethod(
                "Lerp",
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(lerp);
        }

        var switchCursorBegin = new SystemMouseCursor("switch-begin");
        var switchCursorEnd = new SystemMouseCursor("switch-end");
        var begin = new ThemeData(
            dividerTheme: new DividerThemeData(Space: 4),
            tooltipTheme: new TooltipThemeData(Height: 8),
            sliderTheme: new SliderThemeData(TrackHeight: 2),
            switchTheme: new SwitchThemeData(
                ThumbColor: MaterialStateProperty<Color?>.All(Colors.Black),
                MouseCursor: MaterialStateProperty<MouseCursor?>.All(switchCursorBegin)),
            datePickerTheme: new DatePickerThemeData(RangePickerElevation: 4),
            snackBarTheme: new SnackBarThemeData(Elevation: 2),
            menuBarTheme: new MenuBarThemeData(
                new MenuStyle(Elevation: MaterialStateProperty<double?>.All(2))));
        var end = new ThemeData(
            dividerTheme: new DividerThemeData(Space: 12),
            tooltipTheme: new TooltipThemeData(Height: 24),
            sliderTheme: new SliderThemeData(TrackHeight: 10),
            switchTheme: new SwitchThemeData(
                ThumbColor: MaterialStateProperty<Color?>.All(Colors.White),
                MouseCursor: MaterialStateProperty<MouseCursor?>.All(switchCursorEnd)),
            datePickerTheme: new DatePickerThemeData(RangePickerElevation: 12),
            snackBarTheme: new SnackBarThemeData(Elevation: 10),
            menuBarTheme: new MenuBarThemeData(
                new MenuStyle(Elevation: MaterialStateProperty<double?>.All(10))));

        ThemeData result = ThemeData.Lerp(begin, end, 0.25);

        Assert.Equal(6, result.DividerTheme.Space);
        Assert.Equal(12, result.TooltipTheme.Height);
        Assert.Equal(4, result.SliderTheme.TrackHeight);
        Assert.Equal(Color.FromRgb(63, 63, 63), result.SwitchTheme.ThumbColor!.Resolve(MaterialState.None));
        Assert.Equal(switchCursorBegin, result.SwitchTheme.MouseCursor!.Resolve(MaterialState.None));
        Assert.Equal(6, result.DatePickerTheme.RangePickerElevation);
        Assert.Equal(4, result.SnackBarTheme.Elevation);
        Assert.Equal(4, result.MenuBarTheme.Style!.Elevation!.Resolve(MaterialState.None));
    }

    [Fact]
    public void ThemeData_Lerp_InterpolatesAndUnionsThemeExtensions()
    {
        var colorBegin = new ColorThemeExtension(
            Colors.Black,
            Color.Parse("#FFFFC107"));
        var colorEnd = new ColorThemeExtension(
            Colors.White,
            Color.Parse("#FF2196F3"));
        var textBegin = new TextThemeExtension(new TextStyle(FontSize: 50));
        var textEnd = new TextThemeExtension(new TextStyle(FontSize: 100));
        var beginOnly = new BeginOnlyThemeExtension(30);
        var endOnly = new EndOnlyThemeExtension(40);
        var begin = new ThemeData(extensions: [colorBegin, textBegin, beginOnly]);
        var end = new ThemeData(extensions: [colorEnd, textEnd, endOnly]);

        ThemeData result = ThemeData.Lerp(begin, end, 0.5);

        Assert.Equal(Color.Parse("#FF7F7F7F"), result.Extension<ColorThemeExtension>()!.First);
        Assert.Equal(Color.Parse("#FF90AB7D"), result.Extension<ColorThemeExtension>()!.Second);
        Assert.Equal(75, result.Extension<TextThemeExtension>()!.Style.FontSize);
        Assert.Same(beginOnly, result.Extension<BeginOnlyThemeExtension>());
        Assert.Same(endOnly, result.Extension<EndOnlyThemeExtension>());
        Assert.Equal(4, result.Extensions.Count);
    }

    [Fact]
    public void AnimatedTheme_InterpolatesFromCurrentThemeAcrossInterruptedUpdates_AndCallsOnEnd()
    {
        var owner = new BuildOwner();
        ThemeData? observedTheme = null;
        int completed = 0;
        var probe = new ThemeProbe(theme => observedTheme = theme);
        var begin = ThemeData.Light with { PrimaryColor = Color.FromRgb(0, 0, 0) };
        var firstTarget = ThemeData.Light with { PrimaryColor = Color.FromRgb(200, 100, 50) };
        var secondTarget = ThemeData.Light with { PrimaryColor = Color.FromRgb(20, 220, 120) };
        var root = new TestRootElement(new AnimatedTheme(
            data: begin,
            duration: TimeSpan.FromMilliseconds(200),
            child: probe,
            onEnd: () => completed++));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();
        Assert.Equal(begin.PrimaryColor, observedTheme!.PrimaryColor);

        root.Update(new AnimatedTheme(
            data: firstTarget,
            duration: TimeSpan.FromMilliseconds(200),
            child: probe,
            onEnd: () => completed++));
        owner.FlushBuild();

        double now = Scheduler.CurrentSeconds;
        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 0.1));
        owner.FlushBuild();
        Color interruptedColor = observedTheme!.PrimaryColor;
        Assert.NotEqual(begin.PrimaryColor, interruptedColor);
        Assert.NotEqual(firstTarget.PrimaryColor, interruptedColor);

        root.Update(new AnimatedTheme(
            data: secondTarget,
            duration: TimeSpan.FromMilliseconds(200),
            child: probe,
            onEnd: () => completed++));
        owner.FlushBuild();
        Assert.Equal(interruptedColor, observedTheme!.PrimaryColor);

        Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(now + 1.0));
        owner.FlushBuild();
        Assert.Equal(secondTarget.PrimaryColor, observedTheme.PrimaryColor);
        Assert.Equal(1, completed);

        root.Unmount();
    }

    [Fact]
    public void AnimatedTheme_ExposesFlutterDefaultsAndValidatesDuration()
    {
        var animatedTheme = new AnimatedTheme(
            data: ThemeData.Light,
            child: new SizedBox());

        Assert.Equal(TimeSpan.FromMilliseconds(200), animatedTheme.Duration);
        Assert.Equal(Curves.Linear(0.3), animatedTheme.Curve(0.3));
        Assert.Null(animatedTheme.OnEnd);
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimatedTheme(
            data: ThemeData.Light,
            child: new SizedBox(),
            duration: TimeSpan.FromMilliseconds(-1)));
    }

    [Fact]
    public void MaterialPageRoute_UsesPlatformBuilderDurationsAndTransitionComposition()
    {
        var builder = new RecordingPageTransitionsBuilder();
        var pageTransitions = new PageTransitionsTheme(
            new Dictionary<TargetPlatform, PageTransitionsBuilder>
            {
                [TargetPlatform.Windows] = builder,
            });
        var theme = ThemeData.Light with
        {
            Platform = TargetPlatform.Windows,
            PageTransitionsTheme = pageTransitions,
        };
        NavigatorState? navigator = null;
        var route = new MaterialPageRoute(
            builder: context =>
            {
                navigator ??= Navigator.Of(context);
                return new SizedBox(width: 10, height: 10);
            });
        var root = new TestRootElement(
            new Theme(
                theme,
                new Directionality(
                    Plumix.UI.TextDirection.Ltr,
                    new Navigator(route))));
        var owner = new BuildOwner();

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(navigator);
        Assert.Equal(TimeSpan.FromMilliseconds(120), route.TransitionDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(80), route.ReverseTransitionDuration);
        Assert.True(builder.BuildCount > 0);

        root.Unmount();
    }

    private sealed class ThemeProbe : StatelessWidget
    {
        private readonly Action<ThemeData> _onBuild;

        public ThemeProbe(Action<ThemeData> onBuild)
        {
            _onBuild = onBuild;
        }

        public override Widget Build(BuildContext context)
        {
            _onBuild(Theme.Of(context));
            return new SizedBox(width: 1, height: 1);
        }
    }

    private sealed class RecordingPageTransitionsBuilder : PageTransitionsBuilder
    {
        public int BuildCount { get; private set; }

        public override TimeSpan TransitionDuration => TimeSpan.FromMilliseconds(120);

        public override TimeSpan ReverseTransitionDuration => TimeSpan.FromMilliseconds(80);

        public override Widget BuildTransitions(
            PageRoute route,
            BuildContext context,
            Animation<double> animation,
            Animation<double> secondaryAnimation,
            Widget child)
        {
            BuildCount += 1;
            return child;
        }
    }

    private sealed class ColorThemeExtension : ThemeExtension<ColorThemeExtension>
    {
        public ColorThemeExtension(Color first, Color second)
        {
            First = first;
            Second = second;
        }

        public Color First { get; }

        public Color Second { get; }

        public override ColorThemeExtension Lerp(ColorThemeExtension? other, double t)
        {
            return other is null
                ? this
                : new ColorThemeExtension(
                    new ColorTween().Evaluate(t, First, other.First),
                    new ColorTween().Evaluate(t, Second, other.Second));
        }
    }

    private sealed class TextThemeExtension : ThemeExtension<TextThemeExtension>
    {
        public TextThemeExtension(TextStyle style)
        {
            Style = style;
        }

        public TextStyle Style { get; }

        public override TextThemeExtension Lerp(TextThemeExtension? other, double t)
        {
            return other is null
                ? this
                : new TextThemeExtension(TextStyle.Lerp(Style, other.Style, t));
        }
    }

    private sealed class BeginOnlyThemeExtension : ThemeExtension<BeginOnlyThemeExtension>
    {
        public BeginOnlyThemeExtension(double value)
        {
            Value = value;
        }

        public double Value { get; }

        public override BeginOnlyThemeExtension Lerp(BeginOnlyThemeExtension? other, double t)
        {
            return other is null ? this : new BeginOnlyThemeExtension(Value + ((other.Value - Value) * t));
        }
    }

    private sealed class EndOnlyThemeExtension : ThemeExtension<EndOnlyThemeExtension>
    {
        public EndOnlyThemeExtension(double value)
        {
            Value = value;
        }

        public double Value { get; }

        public override EndOnlyThemeExtension Lerp(EndOnlyThemeExtension? other, double t)
        {
            return other is null ? this : new EndOnlyThemeExtension(Value + ((other.Value - Value) * t));
        }
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
