using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/expand_icon.dart
public sealed class ExpandIcon : StatefulWidget
{
    public ExpandIcon(
        Action<bool>? onPressed,
        bool isExpanded = false,
        double size = 24,
        Thickness? padding = null,
        Color? color = null,
        Color? disabledColor = null,
        Color? expandedColor = null,
        Color? splashColor = null,
        Color? highlightColor = null,
        Key? key = null) : base(key)
    {
        if (!double.IsFinite(size) || size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "ExpandIcon size must be finite and positive.");
        }

        IsExpanded = isExpanded;
        Size = size;
        OnPressed = onPressed;
        Padding = padding ?? new Thickness(8);
        Color = color;
        DisabledColor = disabledColor;
        ExpandedColor = expandedColor;
        SplashColor = splashColor;
        HighlightColor = highlightColor;
    }

    public bool IsExpanded { get; }
    public double Size { get; }
    public Action<bool>? OnPressed { get; }
    public Thickness Padding { get; }
    public Color? Color { get; }
    public Color? DisabledColor { get; }
    public Color? ExpandedColor { get; }
    public Color? SplashColor { get; }
    public Color? HighlightColor { get; }

    public override State CreateState() => new ExpandIconState();

    private sealed class ExpandIconState : State
    {
        private static readonly TimeSpan ThemeAnimationDuration = TimeSpan.FromMilliseconds(200);
        private AnimationController? _controller;

        private ExpandIcon CurrentWidget => (ExpandIcon)StateWidget;

        public override void InitState()
        {
            _controller = new AnimationController(ThemeAnimationDuration) { Curve = Curves.FastOutSlowIn };
            _controller.Changed += HandleChanged;
            if (CurrentWidget.IsExpanded)
            {
                _controller.Forward(from: 1);
                _controller.Stop();
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
            var iconColor = ResolveIconColor(Theme.Of(context));
            double progress = _controller!.Evaluate();
            double angle = Math.PI * progress;
            double center = CurrentWidget.Size / 2;
            var rotation = new Matrix(
                Math.Cos(angle), Math.Sin(angle),
                -Math.Sin(angle), Math.Cos(angle),
                0, 0);
            Widget icon = new Plumix.Widgets.Transform(
                transform: Matrix.CreateTranslation(center, center)
                           * rotation
                           * Matrix.CreateTranslation(-center, -center),
                child: new Icon(Icons.ExpandMore, size: CurrentWidget.Size));

            icon = new IconButton(
                icon: icon,
                iconSize: CurrentWidget.Size,
                padding: CurrentWidget.Padding,
                color: iconColor,
                disabledColor: CurrentWidget.DisabledColor,
                highlightColor: CurrentWidget.HighlightColor,
                splashColor: CurrentWidget.SplashColor,
                onPressed: CurrentWidget.OnPressed is null
                    ? null
                    : () => CurrentWidget.OnPressed(CurrentWidget.IsExpanded));

            return new Semantics(
                hint: CurrentWidget.OnPressed is null ? null : onTapHint,
                child: icon);
        }

        public override void Dispose()
        {
            if (_controller is null) return;
            _controller.Changed -= HandleChanged;
            _controller.Dispose();
            _controller = null;
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

        private void HandleChanged() => SetState(() => { });
    }
}
