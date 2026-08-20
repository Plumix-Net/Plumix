using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/page_scaffold.dart

/// <summary>Implements a single iOS application page's layout.</summary>
public sealed class CupertinoPageScaffold : StatefulWidget
{
    public CupertinoPageScaffold(
        Widget child,
        IObstructingPreferredSizeWidget? navigationBar = null,
        CupertinoDynamicColor? backgroundColor = null,
        bool resizeToAvoidBottomInset = true,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        if (navigationBar is not null && navigationBar is not Widget)
        {
            throw new ArgumentException(
                $"{nameof(IObstructingPreferredSizeWidget)} implementations must also be widgets.",
                nameof(navigationBar));
        }

        NavigationBar = navigationBar;
        BackgroundColor = backgroundColor;
        ResizeToAvoidBottomInset = resizeToAvoidBottomInset;
    }

    public IObstructingPreferredSizeWidget? NavigationBar { get; }

    public Widget Child { get; }

    public CupertinoDynamicColor? BackgroundColor { get; }

    public bool ResizeToAvoidBottomInset { get; }

    public override State CreateState() => new CupertinoPageScaffoldState();
}

internal sealed class CupertinoPageScaffoldState : State, WidgetsBindingObserver
{
    private static readonly TimeSpan StatusBarTapScrollDuration = TimeSpan.FromMilliseconds(500);
    private readonly LabeledGlobalKey<State> _statusBarKey = new("CupertinoPageScaffold status bar");

    private CupertinoPageScaffold CurrentWidget => (CupertinoPageScaffold)StateWidget;

    public override void InitState()
    {
        base.InitState();
        WidgetsBinding.Instance.AddObserver(this);
    }

    public override void Deactivate()
    {
        WidgetsBinding.Instance.RemoveObserver(this);
        base.Deactivate();
    }

    public override void Activate()
    {
        base.Activate();
        WidgetsBinding.Instance.AddObserver(this);
    }

    public override void Dispose()
    {
        WidgetsBinding.Instance.RemoveObserver(this);
        base.Dispose();
    }

    public void HandleStatusBarTap()
    {
        ScrollController? primaryScrollController = PrimaryScrollController.MaybeOf(Context);
        if (primaryScrollController is { HasClients: true }
            && HitTestableAtOrigin.IsHitTestableAtOrigin(_statusBarKey))
        {
            primaryScrollController.AnimateTo(
                0.0,
                duration: StatusBarTapScrollDuration,
                curve: Curves.LinearToEaseOut);
        }
    }

    public override Widget Build(BuildContext context)
    {
        Widget paddedContent = CurrentWidget.Child;
        Color backgroundColor = CupertinoDynamicColor.MaybeResolve(CurrentWidget.BackgroundColor, context)
                                ?? CupertinoTheme.Of(context).ScaffoldBackgroundColor.Value;

        MediaQueryData existingMediaQuery = MediaQuery.Of(context);
        IObstructingPreferredSizeWidget? navigationBar = CurrentWidget.NavigationBar;
        if (navigationBar is not null)
        {
            double topPadding = navigationBar.PreferredSize.Height + existingMediaQuery.Padding.Top;
            double bottomPadding = CurrentWidget.ResizeToAvoidBottomInset
                ? existingMediaQuery.ViewInsets.Bottom
                : 0.0;
            Thickness newViewInsets = CurrentWidget.ResizeToAvoidBottomInset
                ? CopyBottom(existingMediaQuery.ViewInsets, 0.0)
                : existingMediaQuery.ViewInsets;

            if (navigationBar.ShouldFullyObstruct(context))
            {
                paddedContent = new MediaQuery(
                    data: existingMediaQuery
                        .RemovePadding(removeTop: true)
                        .CopyWith(viewInsets: newViewInsets),
                    child: new Padding(
                        insets: new Thickness(0.0, topPadding, 0.0, bottomPadding),
                        child: paddedContent));
            }
            else
            {
                paddedContent = new MediaQuery(
                    data: existingMediaQuery.CopyWith(
                        padding: CopyTop(existingMediaQuery.Padding, topPadding),
                        viewInsets: newViewInsets),
                    child: new Padding(
                        insets: new Thickness(0.0, 0.0, 0.0, bottomPadding),
                        child: paddedContent));
            }
        }
        else if (CurrentWidget.ResizeToAvoidBottomInset)
        {
            paddedContent = new MediaQuery(
                data: existingMediaQuery.CopyWith(
                    viewInsets: CopyBottom(existingMediaQuery.ViewInsets, 0.0)),
                child: new Padding(
                    insets: new Thickness(0.0, 0.0, 0.0, existingMediaQuery.ViewInsets.Bottom),
                    child: paddedContent));
        }

        var children = new List<Widget> { paddedContent };
        if (navigationBar is not null)
        {
            var navigationBarWidget = (Widget)navigationBar;
            children.Add(new Positioned(
                top: 0.0,
                left: 0.0,
                right: 0.0,
                child: MediaQuery.WithNoTextScaling(context, navigationBarWidget)));
        }

        children.Add(new Positioned(
            top: 0.0,
            left: 0.0,
            right: 0.0,
            height: existingMediaQuery.Padding.Top,
            child: new HitTestableAtOrigin(_statusBarKey)));

        return new ScrollNotificationObserver(
            child: new DecoratedBox(
                decoration: new BoxDecoration(Color: backgroundColor),
                child: new CupertinoPageScaffoldBackgroundColor(
                    color: backgroundColor,
                    child: new Stack(children: children))));
    }

    private static Thickness CopyTop(Thickness source, double top)
    {
        return new Thickness(source.Left, top, source.Right, source.Bottom);
    }

    private static Thickness CopyBottom(Thickness source, double bottom)
    {
        return new Thickness(source.Left, source.Top, source.Right, bottom);
    }
}

/// <summary>Exposes the resolved page-scaffold background color to descendants.</summary>
public sealed class CupertinoPageScaffoldBackgroundColor : InheritedWidget
{
    public CupertinoPageScaffoldBackgroundColor(Color color, Widget child, Key? key = null) : base(key)
    {
        Color = color;
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public Color Color { get; }

    public Widget Child { get; }

    public static Color? MaybeOf(BuildContext context)
    {
        return context.DependOnInherited<CupertinoPageScaffoldBackgroundColor>()?.Color;
    }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return Color != ((CupertinoPageScaffoldBackgroundColor)oldWidget).Color;
    }
}

/// <summary>
/// A preferred-size widget that reports whether it fully obstructs the content behind it.
/// </summary>
public interface IObstructingPreferredSizeWidget : IPreferredSizeWidget
{
    bool ShouldFullyObstruct(BuildContext context);
}
