using Avalonia;
using Avalonia.Media;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity sources:
// flutter/packages/flutter/lib/src/material/page.dart
// flutter/packages/flutter/lib/src/material/page_transitions_theme.dart

public abstract class PageTransitionsBuilder
{
    public virtual TimeSpan TransitionDuration => TimeSpan.FromMilliseconds(300);

    public virtual TimeSpan ReverseTransitionDuration => TransitionDuration;

    public abstract Widget BuildTransitions(
        PageRoute route,
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child);
}

public sealed class ZoomPageTransitionsBuilder : PageTransitionsBuilder
{
    public ZoomPageTransitionsBuilder(
        bool allowSnapshotting = true,
        bool allowEnterRouteSnapshotting = true,
        Color? backgroundColor = null)
    {
        AllowSnapshotting = allowSnapshotting;
        AllowEnterRouteSnapshotting = allowEnterRouteSnapshotting;
        BackgroundColor = backgroundColor;
    }

    public bool AllowSnapshotting { get; }

    public bool AllowEnterRouteSnapshotting { get; }

    public Color? BackgroundColor { get; }

    public override Widget BuildTransitions(
        PageRoute route,
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child)
    {
        Animation<double> primaryOpacity = Map(
            animation,
            value => Interval(value, 0.0, 0.25));
        Animation<double> primaryScale = Map(
            animation,
            value => 0.85 + (0.15 * Curves.FastOutSlowIn(value)));
        Animation<double> secondaryOpacity = Map(
            secondaryAnimation,
            value => 1.0 - Interval(value, 0.08, 0.21));
        Animation<double> secondaryScale = Map(
            secondaryAnimation,
            value => 1.0 + (0.05 * Curves.FastOutSlowIn(value)));

        Widget transitioned = new FadeTransition(
            opacity: secondaryOpacity,
            child: new ScaleTransition(
                scale: secondaryScale,
                child: child));
        transitioned = new FadeTransition(
            opacity: primaryOpacity,
            child: new ScaleTransition(
                scale: primaryScale,
                child: transitioned));
        return new ColoredBox(
            color: BackgroundColor ?? Theme.Of(context).SurfaceColor,
            child: transitioned);
    }

    private static Animation<double> Map(Animation<double> parent, Func<double, double> transform)
    {
        return new MappedAnimation<double>(parent, transform);
    }

    private static double Interval(double value, double begin, double end)
    {
        return Math.Clamp((value - begin) / (end - begin), 0.0, 1.0);
    }
}

public sealed class FadeForwardsPageTransitionsBuilder : PageTransitionsBuilder
{
    public FadeForwardsPageTransitionsBuilder(Color? backgroundColor = null)
    {
        BackgroundColor = backgroundColor;
    }

    public Color? BackgroundColor { get; }

    public override TimeSpan TransitionDuration => TimeSpan.FromMilliseconds(450);

    public override Widget BuildTransitions(
        PageRoute route,
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child)
    {
        Animation<double> primaryOpacity = new MappedAnimation<double>(
            animation,
            value => Math.Clamp(value / 0.75, 0.0, 1.0));
        Animation<Vector> primaryPosition = new MappedAnimation<Vector>(
            animation,
            value => new Vector(0.25 * (1.0 - Curves.EaseInOut(value)), 0.0));
        Animation<double> secondaryOpacity = new MappedAnimation<double>(
            secondaryAnimation,
            value => 1.0 - Math.Clamp(value / 0.25, 0.0, 1.0));
        Animation<Vector> secondaryPosition = new MappedAnimation<Vector>(
            secondaryAnimation,
            value => new Vector(-0.25 * Curves.EaseInOut(value), 0.0));

        Widget transitioned = new FadeTransition(
            opacity: secondaryOpacity,
            child: new SlideTransition(
                position: secondaryPosition,
                child: child));
        transitioned = new FadeTransition(
            opacity: primaryOpacity,
            child: new SlideTransition(
                position: primaryPosition,
                child: transitioned));
        return new ColoredBox(
            color: BackgroundColor ?? Theme.Of(context).SurfaceColor,
            child: transitioned);
    }
}

public sealed class CupertinoPageTransitionsBuilder : PageTransitionsBuilder
{
    public override TimeSpan TransitionDuration => TimeSpan.FromMilliseconds(500);

    public override Widget BuildTransitions(
        PageRoute route,
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child)
    {
        if (route.FullscreenDialog)
        {
            Animation<Vector> verticalPosition = new MappedAnimation<Vector>(
                animation,
                value => new Vector(0.0, 1.0 - Curves.EaseOut(value)));
            return new SlideTransition(
                position: verticalPosition,
                child: child);
        }

        Animation<Vector> primaryPosition = new MappedAnimation<Vector>(
            animation,
            value => new Vector(1.0 - Curves.EaseOut(value), 0.0));
        Animation<Vector> secondaryPosition = new MappedAnimation<Vector>(
            secondaryAnimation,
            value => new Vector(-Curves.EaseOut(value) / 3.0, 0.0));
        return new SlideTransition(
            position: primaryPosition,
            textDirection: Directionality.Of(context),
            child: new SlideTransition(
                position: secondaryPosition,
                textDirection: Directionality.Of(context),
                child: child));
    }
}

public sealed record PageTransitionsTheme
{
    private static readonly IReadOnlyDictionary<TargetPlatform, PageTransitionsBuilder> DefaultBuilders =
        new Dictionary<TargetPlatform, PageTransitionsBuilder>
        {
            [TargetPlatform.Android] = new ZoomPageTransitionsBuilder(),
            [TargetPlatform.IOS] = new CupertinoPageTransitionsBuilder(),
            [TargetPlatform.MacOS] = new CupertinoPageTransitionsBuilder(),
            [TargetPlatform.Windows] = new ZoomPageTransitionsBuilder(),
            [TargetPlatform.Linux] = new ZoomPageTransitionsBuilder(),
        };

    public PageTransitionsTheme(IReadOnlyDictionary<TargetPlatform, PageTransitionsBuilder>? builders = null)
    {
        Builders = builders ?? DefaultBuilders;
    }

    public IReadOnlyDictionary<TargetPlatform, PageTransitionsBuilder> Builders { get; }

    public PageTransitionsBuilder Resolve(TargetPlatform platform)
    {
        if (Builders.TryGetValue(platform, out PageTransitionsBuilder? builder))
        {
            return builder;
        }

        return platform is TargetPlatform.IOS or TargetPlatform.MacOS
            ? new CupertinoPageTransitionsBuilder()
            : new ZoomPageTransitionsBuilder();
    }

    public Widget BuildTransitions(
        PageRoute route,
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child)
    {
        return Resolve(Theme.Of(context).Platform).BuildTransitions(
            route,
            context,
            animation,
            secondaryAnimation,
            child);
    }
}

public sealed class MaterialPageRoute : PageRoute
{
    private readonly WidgetBuilder _builder;

    public MaterialPageRoute(
        WidgetBuilder builder,
        RouteSettings? settings = null,
        bool maintainState = true,
        bool fullscreenDialog = false,
        bool allowSnapshotting = true) : base(settings, fullscreenDialog)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        MaintainState = maintainState;
        AllowSnapshotting = allowSnapshotting;
    }

    public bool MaintainState { get; }

    public override bool AllowSnapshotting { get; }

    public override TimeSpan TransitionDuration => ResolveBuilder().TransitionDuration;

    public override TimeSpan ReverseTransitionDuration => ResolveBuilder().ReverseTransitionDuration;

    public override bool CanTransitionTo(TransitionRoute nextRoute)
    {
        return nextRoute is MaterialPageRoute materialRoute && !materialRoute.FullscreenDialog;
    }

    public override bool CanTransitionFrom(TransitionRoute previousRoute)
    {
        return previousRoute is PageRoute && !FullscreenDialog;
    }

    public override Widget BuildPage(BuildContext context)
    {
        return new Semantics(
            scopesRoute: true,
            explicitChildNodes: true,
            child: _builder(context));
    }

    public override Widget BuildTransitions(
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child)
    {
        return Theme.Of(context).PageTransitionsTheme.BuildTransitions(
            this,
            context,
            animation,
            secondaryAnimation,
            child);
    }

    private PageTransitionsBuilder ResolveBuilder()
    {
        NavigatorState? navigator = Navigator;
        ThemeData theme = navigator is null ? ThemeData.Light : Theme.Of(navigator.Context);
        return theme.PageTransitionsTheme.Resolve(theme.Platform);
    }
}

internal sealed class MappedAnimation<T> : Animation<T>
{
    private readonly Animation<double> _parent;
    private readonly Func<double, T> _transform;

    public MappedAnimation(Animation<double> parent, Func<double, T> transform)
    {
        _parent = parent ?? throw new ArgumentNullException(nameof(parent));
        _transform = transform ?? throw new ArgumentNullException(nameof(transform));
    }

    public override T Value => _transform(_parent.Value);

    public override AnimationStatus Status => _parent.Status;

    public override void AddListener(Action listener) => _parent.AddListener(listener);

    public override void RemoveListener(Action listener) => _parent.RemoveListener(listener);

    public override void AddStatusListener(Action<AnimationStatus> listener)
    {
        _parent.AddStatusListener(listener);
    }

    public override void RemoveStatusListener(Action<AnimationStatus> listener)
    {
        _parent.RemoveStatusListener(listener);
    }
}
