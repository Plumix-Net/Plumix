using Avalonia;
using Plumix.Foundation;

namespace Plumix.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/media_query.dart (approximate)

public sealed record MediaQueryData(
    Size Size = default,
    double DevicePixelRatio = 1.0,
    Thickness Padding = default,
    Thickness ViewInsets = default,
    Thickness SystemGestureInsets = default,
    Thickness ViewPadding = default,
    double TextScaleFactor = 1.0)
{
    public MediaQueryData CopyWith(
        Size? size = null,
        double? devicePixelRatio = null,
        Thickness? padding = null,
        Thickness? viewInsets = null,
        Thickness? systemGestureInsets = null,
        Thickness? viewPadding = null,
        double? textScaleFactor = null)
    {
        return new MediaQueryData(
            Size: size ?? Size,
            DevicePixelRatio: devicePixelRatio ?? DevicePixelRatio,
            Padding: padding ?? Padding,
            ViewInsets: viewInsets ?? ViewInsets,
            SystemGestureInsets: systemGestureInsets ?? SystemGestureInsets,
            ViewPadding: viewPadding ?? ViewPadding,
            TextScaleFactor: textScaleFactor ?? TextScaleFactor);
    }

    public MediaQueryData RemovePadding(
        bool removeLeft = false,
        bool removeTop = false,
        bool removeRight = false,
        bool removeBottom = false)
    {
        if (!(removeLeft || removeTop || removeRight || removeBottom))
        {
            return this;
        }

        return CopyWith(
            padding: CopyThickness(
                Padding,
                left: removeLeft ? 0.0 : null,
                top: removeTop ? 0.0 : null,
                right: removeRight ? 0.0 : null,
                bottom: removeBottom ? 0.0 : null),
            viewPadding: CopyThickness(
                ViewPadding,
                left: removeLeft ? Math.Max(0.0, ViewPadding.Left - Padding.Left) : null,
                top: removeTop ? Math.Max(0.0, ViewPadding.Top - Padding.Top) : null,
                right: removeRight ? Math.Max(0.0, ViewPadding.Right - Padding.Right) : null,
                bottom: removeBottom ? Math.Max(0.0, ViewPadding.Bottom - Padding.Bottom) : null));
    }

    public MediaQueryData RemoveViewInsets(
        bool removeLeft = false,
        bool removeTop = false,
        bool removeRight = false,
        bool removeBottom = false)
    {
        if (!(removeLeft || removeTop || removeRight || removeBottom))
        {
            return this;
        }

        return CopyWith(
            viewPadding: CopyThickness(
                ViewPadding,
                left: removeLeft ? Math.Max(0.0, ViewPadding.Left - ViewInsets.Left) : null,
                top: removeTop ? Math.Max(0.0, ViewPadding.Top - ViewInsets.Top) : null,
                right: removeRight ? Math.Max(0.0, ViewPadding.Right - ViewInsets.Right) : null,
                bottom: removeBottom ? Math.Max(0.0, ViewPadding.Bottom - ViewInsets.Bottom) : null),
            viewInsets: CopyThickness(
                ViewInsets,
                left: removeLeft ? 0.0 : null,
                top: removeTop ? 0.0 : null,
                right: removeRight ? 0.0 : null,
                bottom: removeBottom ? 0.0 : null));
    }

    public MediaQueryData RemoveViewPadding(
        bool removeLeft = false,
        bool removeTop = false,
        bool removeRight = false,
        bool removeBottom = false)
    {
        if (!(removeLeft || removeTop || removeRight || removeBottom))
        {
            return this;
        }

        return CopyWith(
            padding: CopyThickness(
                Padding,
                left: removeLeft ? 0.0 : null,
                top: removeTop ? 0.0 : null,
                right: removeRight ? 0.0 : null,
                bottom: removeBottom ? 0.0 : null),
            viewPadding: CopyThickness(
                ViewPadding,
                left: removeLeft ? 0.0 : null,
                top: removeTop ? 0.0 : null,
                right: removeRight ? 0.0 : null,
                bottom: removeBottom ? 0.0 : null));
    }

    public static Thickness ComputePadding(Thickness viewPadding, Thickness viewInsets)
    {
        return new Thickness(
            Math.Max(0.0, viewPadding.Left - viewInsets.Left),
            Math.Max(0.0, viewPadding.Top - viewInsets.Top),
            Math.Max(0.0, viewPadding.Right - viewInsets.Right),
            Math.Max(0.0, viewPadding.Bottom - viewInsets.Bottom));
    }

    private static Thickness CopyThickness(
        Thickness source,
        double? left = null,
        double? top = null,
        double? right = null,
        double? bottom = null)
    {
        return new Thickness(
            left ?? source.Left,
            top ?? source.Top,
            right ?? source.Right,
            bottom ?? source.Bottom);
    }
}

public sealed class MediaQuery : InheritedWidget
{
    public MediaQuery(
        MediaQueryData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data;
        Child = child;
    }

    public MediaQueryData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((MediaQuery)oldWidget).Data, Data);
    }

    public static MediaQueryData Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException("No MediaQuery ancestor found for the given BuildContext.");
    }

    public static MediaQueryData? MaybeOf(BuildContext context)
    {
        return context.DependOnInherited<MediaQuery>()?.Data;
    }

    public static Thickness PaddingOf(BuildContext context) => Of(context).Padding;

    public static Thickness? MaybePaddingOf(BuildContext context) => MaybeOf(context)?.Padding;

    public static Thickness ViewInsetsOf(BuildContext context) => Of(context).ViewInsets;

    public static Thickness? MaybeViewInsetsOf(BuildContext context) => MaybeOf(context)?.ViewInsets;

    public static Thickness ViewPaddingOf(BuildContext context) => Of(context).ViewPadding;

    public static Thickness? MaybeViewPaddingOf(BuildContext context) => MaybeOf(context)?.ViewPadding;

    public static double TextScaleFactorOf(BuildContext context) => Of(context).TextScaleFactor;

    public static double? MaybeTextScaleFactorOf(BuildContext context) => MaybeOf(context)?.TextScaleFactor;

    public static Widget RemovePadding(
        BuildContext context,
        Widget child,
        bool removeLeft = false,
        bool removeTop = false,
        bool removeRight = false,
        bool removeBottom = false)
    {
        return new MediaQuery(
            data: Of(context).RemovePadding(
                removeLeft: removeLeft,
                removeTop: removeTop,
                removeRight: removeRight,
                removeBottom: removeBottom),
            child: child);
    }

    public static Widget RemoveViewInsets(
        BuildContext context,
        Widget child,
        bool removeLeft = false,
        bool removeTop = false,
        bool removeRight = false,
        bool removeBottom = false)
    {
        return new MediaQuery(
            data: Of(context).RemoveViewInsets(
                removeLeft: removeLeft,
                removeTop: removeTop,
                removeRight: removeRight,
                removeBottom: removeBottom),
            child: child);
    }

    public static Widget RemoveViewPadding(
        BuildContext context,
        Widget child,
        bool removeLeft = false,
        bool removeTop = false,
        bool removeRight = false,
        bool removeBottom = false)
    {
        return new MediaQuery(
            data: Of(context).RemoveViewPadding(
                removeLeft: removeLeft,
                removeTop: removeTop,
                removeRight: removeRight,
                removeBottom: removeBottom),
            child: child);
    }
}
