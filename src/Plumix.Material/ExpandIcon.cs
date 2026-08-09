using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/expand_icon.dart
public sealed class ExpandIcon : StatefulWidget
{
    public ExpandIcon(
        Action<bool>? onPressed,
        bool isExpanded = false,
        double size = 24,
        EdgeInsetsGeometry? padding = null,
        Color? color = null,
        Color? disabledColor = null,
        Color? expandedColor = null,
        Color? splashColor = null,
        Color? highlightColor = null,
        Key? key = null) : base(key)
    {
        IsExpanded = isExpanded;
        Size = size;
        OnPressed = onPressed;
        Padding = padding ?? EdgeInsetsGeometry.All(8.0);
        Color = color;
        DisabledColor = disabledColor;
        ExpandedColor = expandedColor;
        SplashColor = splashColor;
        HighlightColor = highlightColor;
    }

    public bool IsExpanded { get; }
    public double Size { get; }
    public Action<bool>? OnPressed { get; }
    public EdgeInsetsGeometry Padding { get; }
    public Color? Color { get; }
    public Color? DisabledColor { get; }
    public Color? ExpandedColor { get; }
    public Color? SplashColor { get; }
    public Color? HighlightColor { get; }

    public override State CreateState() => new ExpandIconState();

    private sealed class ExpandIconState : State
    {
        private static readonly TimeSpan ThemeAnimationDuration = TimeSpan.FromMilliseconds(200);
        private static readonly DoubleTween IconTurnTween = new(begin: 0.0, end: 0.5);

        private AnimationController? _controller;
        private CurvedAnimation? _curvedAnimation;
        private Animation<double>? _iconTurns;

        private ExpandIcon CurrentWidget => (ExpandIcon)StateWidget;

        public override void InitState()
        {
            _controller = new AnimationController(ThemeAnimationDuration, this);
            _curvedAnimation = new CurvedAnimation(_controller, Curves.FastOutSlowIn);
            _iconTurns = IconTurnTween.Animate(_curvedAnimation);
            if (CurrentWidget.IsExpanded)
            {
                _controller.SetValue(1.0);
            }
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldIcon = (ExpandIcon)oldWidget;
            if (oldIcon.IsExpanded == CurrentWidget.IsExpanded) return;
            if (CurrentWidget.IsExpanded)
            {
                _controller!.Forward();
            }
            else
            {
                _controller!.Reverse();
            }
        }

        public override Widget Build(BuildContext context)
        {
            var localizations = MaterialLocalizations.Of(context);
            string onTapHint = CurrentWidget.IsExpanded
                ? localizations.ExpandedIconTapHint
                : localizations.CollapsedIconTapHint;
            return new Semantics(
                onTapHint: CurrentWidget.OnPressed is null ? null : onTapHint,
                child: new IconButton(
                    padding: CurrentWidget.Padding,
                    iconSize: CurrentWidget.Size,
                    highlightColor: CurrentWidget.HighlightColor,
                    splashColor: CurrentWidget.SplashColor,
                    color: ResolveIconColor(Theme.Of(context)),
                    disabledColor: CurrentWidget.DisabledColor,
                    onPressed: CurrentWidget.OnPressed is null ? null : HandlePressed,
                    icon: new RotationTransition(
                        turns: _iconTurns!,
                        child: new Icon(Icons.ExpandMore))));
        }

        public override void Dispose()
        {
            _curvedAnimation?.Dispose();
            _controller?.Dispose();
            _iconTurns = null;
            _curvedAnimation = null;
            _controller = null;
        }

        private void HandlePressed()
        {
            CurrentWidget.OnPressed?.Invoke(CurrentWidget.IsExpanded);
        }

        private Color ResolveIconColor(ThemeData theme)
        {
            if (CurrentWidget.IsExpanded && CurrentWidget.ExpandedColor.HasValue)
            {
                return CurrentWidget.ExpandedColor.Value;
            }

            if (CurrentWidget.Color.HasValue) return CurrentWidget.Color.Value;
            return theme.Brightness == Brightness.Dark
                ? Avalonia.Media.Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF)
                : Avalonia.Media.Color.FromArgb(0x8A, 0x00, 0x00, 0x00);
        }
    }
}
