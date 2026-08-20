using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/expansion_tile.dart

public enum ExpansionTileTransitionMode
{
    Fade,
    Scroll,
}

/// <summary>An iOS-style list tile that expands to reveal one child.</summary>
public sealed class CupertinoExpansionTile : StatefulWidget
{
    public CupertinoExpansionTile(
        Widget title,
        Widget child,
        ExpansibleController? controller = null,
        ExpansionTileTransitionMode transitionMode = ExpansionTileTransitionMode.Fade,
        Key? key = null) : base(key)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Controller = controller;
        TransitionMode = transitionMode;
    }

    public Widget Title { get; }

    public ExpansibleController? Controller { get; }

    public Widget Child { get; }

    public ExpansionTileTransitionMode TransitionMode { get; }

    public override State CreateState() => new CupertinoExpansionTileState();

    private sealed class CupertinoExpansionTileState : State
    {
        private static readonly TimeSpan AnimationDuration = TimeSpan.FromMilliseconds(250.0);
        private static readonly Animatable<double> QuarterTween = new DoubleTween(begin: 0.0, end: 0.25);
        private readonly GlobalKey<State> _headerKey =
            new LabeledGlobalKey<State>("CupertinoExpansionTile header");
        private readonly OverlayPortalController _fadeController = new();
        private ExpansibleController? _tileController;
        private Animation<double>? _iconTurns;

        private CupertinoExpansionTile CurrentWidget => (CupertinoExpansionTile)StateWidget;

        public override void InitState()
        {
            _tileController = CurrentWidget.Controller ?? new ExpansibleController();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldTile = (CupertinoExpansionTile)oldWidget;
            if (!ReferenceEquals(oldTile.Controller, CurrentWidget.Controller))
            {
                if (oldTile.Controller is null)
                {
                    _tileController!.Dispose();
                }

                _tileController = CurrentWidget.Controller ?? new ExpansibleController();
            }
        }

        public override void Dispose()
        {
            if (CurrentWidget.Controller is null)
            {
                _tileController!.Dispose();
            }
        }

        public override Widget Build(BuildContext context)
        {
            return new Expansible(
                controller: _tileController!,
                duration: AnimationDuration,
                curve: Curves.EaseInOut,
                headerBuilder: BuildHeader,
                bodyBuilder: (_, _) => CurrentWidget.Child,
                expansibleBuilder: BuildExpansible);
        }

        private Widget BuildIcon(BuildContext context, Animation<double> animation)
        {
            _iconTurns = animation.Drive(QuarterTween.Chain(new CurveTween(Curves.EaseInOut)));
            double dimension = CupertinoTheme.Of(context).TextTheme.TextStyle.FontSize ?? 17.0;
            return new RotationTransition(
                turns: _iconTurns,
                child: new SizedBox(
                    width: dimension,
                    height: dimension,
                    child: new Center(
                        child: new Icon(
                            CupertinoIcons.RightChevron,
                            color: CupertinoDynamicColor.Resolve(CupertinoColors.ActiveBlue, context),
                            size: 15.0,
                            fontWeight: FontWeight.Black))));
        }

        private void HandleHeaderTap()
        {
            _tileController!.Toggle();
            _fadeController.Show();
        }

        private Widget BuildHeader(BuildContext context, Animation<double> animation)
        {
            CupertinoLocalizations localizations = CupertinoLocalizations.Of(context);
            string onTapHint = _tileController!.IsExpanded
                ? localizations.ExpansionTileExpandedTapHint
                : localizations.ExpansionTileCollapsedTapHint;
            string? semanticsHint = PlatformDefaults.TargetPlatform switch
            {
                TargetPlatform.IOS or TargetPlatform.MacOS when _tileController.IsExpanded =>
                    $"{localizations.CollapsedHint}\n {localizations.ExpansionTileExpandedHint}",
                TargetPlatform.IOS or TargetPlatform.MacOS =>
                    $"{localizations.ExpandedHint}\n {localizations.ExpansionTileCollapsedHint}",
                _ => null,
            };
            return new Semantics(
                hint: semanticsHint,
                onTapHint: onTapHint,
                child: new CupertinoListTile(
                    key: _headerKey,
                    title: CurrentWidget.Title,
                    onTap: () =>
                    {
                        HandleHeaderTap();
                        return Task.CompletedTask;
                    },
                    trailing: BuildIcon(context, animation),
                    backgroundColorActivated: CupertinoColors.Transparent));
        }

        private Widget BuildExpansible(
            BuildContext context,
            Widget header,
            Widget body,
            Animation<double> animation)
        {
            bool animatingFade = animation.Status.IsAnimating()
                                 && CurrentWidget.TransitionMode == ExpansionTileTransitionMode.Fade;
            Widget child = new Column(
                mainAxisSize: MainAxisSize.Min,
                children:
                [
                    header,
                    animatingFade ? new Opacity(0.0, body) : body,
                ]);
            if (CurrentWidget.TransitionMode == ExpansionTileTransitionMode.Scroll)
            {
                return child;
            }

            return new LayoutBuilder((_, constraints) => new OverlayPortal(
                controller: _fadeController,
                overlayChildBuilder: overlayContext => BuildFadeOverlay(
                    overlayContext,
                    animation,
                    constraints),
                child: child));
        }

        private Widget BuildFadeOverlay(
            BuildContext overlayContext,
            Animation<double> animation,
            BoxConstraints constraints)
        {
            _ = overlayContext;
            BuildContext headerContext = _headerKey.CurrentContext
                ?? throw new InvalidOperationException("The expansion-tile header is not mounted.");
            var overlay = (RenderBox)(Overlay.Of(headerContext).Context.FindRenderObject()
                ?? throw new InvalidOperationException("The expansion-tile overlay is not mounted."));
            var headerBox = (RenderBox)(headerContext.FindRenderObject()
                ?? throw new InvalidOperationException("The expansion-tile header has no render box."));
            Point headerOffset = headerBox.LocalToGlobal(default, overlay);
            return new Positioned(
                top: headerOffset.Y + 44.0,
                left: headerOffset.X,
                child: new ConstrainedBox(
                    constraints: constraints,
                    child: new Visibility(
                        visible: animation.Status.IsAnimating(),
                        child: new FadeTransition(
                            opacity: animation,
                            child: CurrentWidget.Child))));
        }
    }
}
