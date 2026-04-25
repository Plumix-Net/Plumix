using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/tooltip.dart (baseline subset)

public sealed class Tooltip : StatefulWidget
{
    private const double DefaultVerticalOffset = 24.0;

    public Tooltip(
        string message,
        Widget child,
        bool preferBelow = true,
        double verticalOffset = DefaultVerticalOffset,
        bool excludeFromSemantics = false,
        Key? key = null) : base(key)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Tooltip message must be non-empty.", nameof(message));
        }

        if (!double.IsFinite(verticalOffset) || verticalOffset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(verticalOffset), "Tooltip verticalOffset must be finite and non-negative.");
        }

        Message = message;
        Child = child ?? throw new ArgumentNullException(nameof(child));
        PreferBelow = preferBelow;
        VerticalOffset = verticalOffset;
        ExcludeFromSemantics = excludeFromSemantics;
    }

    public string Message { get; }

    public Widget Child { get; }

    public bool PreferBelow { get; }

    public double VerticalOffset { get; }

    public bool ExcludeFromSemantics { get; }

    public override State CreateState()
    {
        return new TooltipState();
    }

    private sealed class TooltipState : State
    {
        private static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(150);

        private AnimationController? _fadeController;
        private bool _isMounted;
        private bool _isShown;

        private Tooltip CurrentWidget => (Tooltip)StateWidget;

        public override void InitState()
        {
            _fadeController = new AnimationController(FadeDuration)
            {
                Curve = Curves.EaseOut,
            };
            _fadeController.Changed += HandleAnimationTick;
            _fadeController.Dismissed += HandleAnimationDismissed;
            _isMounted = true;
        }

        public override void Dispose()
        {
            _isMounted = false;
            if (_fadeController is not null)
            {
                _fadeController.Changed -= HandleAnimationTick;
                _fadeController.Dismissed -= HandleAnimationDismissed;
                _fadeController.Dispose();
                _fadeController = null;
            }
        }

        public override Widget Build(BuildContext context)
        {
            Widget result = new Listener(
                behavior: HitTestBehavior.DeferToChild,
                onPointerEnter: _ => ShowTooltip(),
                onPointerExit: _ => HideTooltip(),
                onPointerDown: _ => HideTooltip(),
                child: new GestureDetector(
                    behavior: HitTestBehavior.DeferToChild,
                    onLongPress: ShowTooltip,
                    child: CurrentWidget.Child));

            var opacity = _fadeController?.Evaluate() ?? 0.0;
            if (!_isShown && opacity <= 0)
            {
                return result;
            }

            var bubble = BuildBubble(opacity);
            if (CurrentWidget.ExcludeFromSemantics)
            {
                bubble = new Semantics(
                    flags: SemanticsFlags.IsHidden,
                    child: bubble);
            }

            var positionedBubble = CurrentWidget.PreferBelow
                ? new Positioned(
                    left: 0,
                    right: 0,
                    bottom: -CurrentWidget.VerticalOffset,
                    child: new Center(child: bubble))
                : new Positioned(
                    left: 0,
                    right: 0,
                    top: -CurrentWidget.VerticalOffset,
                    child: new Center(child: bubble));

            return new Stack(
                children:
                [
                    result,
                    positionedBubble,
                ]);
        }

        private Widget BuildBubble(double opacity)
        {
            return new Opacity(
                opacity,
                child: new DecoratedBox(
                    decoration: new BoxDecoration(
                        Color: Color.FromArgb(0xE6, 0x61, 0x61, 0x61),
                        BorderRadius: BorderRadius.Circular(4)),
                    child: new Padding(
                        new Thickness(8, 4, 8, 4),
                        child: new Text(
                            CurrentWidget.Message,
                            color: Colors.White,
                            fontSize: 12,
                            softWrap: false,
                            maxLines: 1,
                            overflow: TextOverflow.Clip))));
        }

        private void ShowTooltip()
        {
            if (_isShown)
            {
                return;
            }

            SetState(() =>
            {
                _isShown = true;
            });

            _fadeController?.Forward(from: 0);
        }

        private void HideTooltip()
        {
            if (!_isShown)
            {
                return;
            }

            var from = _fadeController?.Value ?? 0;
            _fadeController?.Reverse(from: from);
        }

        private void HandleAnimationTick()
        {
            if (!_isMounted)
            {
                return;
            }

            SetState(() => { });
        }

        private void HandleAnimationDismissed()
        {
            if (!_isMounted)
            {
                return;
            }

            SetState(() =>
            {
                _isShown = false;
            });
        }
    }
}
