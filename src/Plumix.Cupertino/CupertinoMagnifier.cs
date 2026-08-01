using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/cupertino/magnifier.dart

namespace Plumix.Cupertino;

public sealed class CupertinoTextMagnifier : StatefulWidget
{
    public CupertinoTextMagnifier(
        MagnifierController controller,
        ValueNotifier<MagnifierInfo> magnifierInfo,
        Curve? animationCurve = null,
        double dragResistance = 10.0,
        double hideBelowThreshold = 48.0,
        double horizontalScreenEdgePadding = 10.0,
        Key? key = null) : base(key)
    {
        if (!double.IsFinite(dragResistance) || dragResistance <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dragResistance));
        }

        if (!double.IsFinite(hideBelowThreshold) || hideBelowThreshold < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hideBelowThreshold));
        }

        if (!double.IsFinite(horizontalScreenEdgePadding) || horizontalScreenEdgePadding < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(horizontalScreenEdgePadding));
        }

        Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        MagnifierInfo = magnifierInfo ?? throw new ArgumentNullException(nameof(magnifierInfo));
        AnimationCurve = animationCurve ?? Curves.EaseOut;
        DragResistance = dragResistance;
        HideBelowThreshold = hideBelowThreshold;
        HorizontalScreenEdgePadding = horizontalScreenEdgePadding;
    }

    public Curve AnimationCurve { get; }

    public MagnifierController Controller { get; }

    public double DragResistance { get; }

    public double HideBelowThreshold { get; }

    public double HorizontalScreenEdgePadding { get; }

    public ValueNotifier<MagnifierInfo> MagnifierInfo { get; }

    public override State CreateState() => new CupertinoTextMagnifierState();

    internal sealed class CupertinoTextMagnifierState : State
    {
        private static readonly TimeSpan DragAnimationDuration = TimeSpan.FromMilliseconds(45);
        private Point _currentAdjustedMagnifierPosition;
        private double _verticalFocalPointAdjustment;
        private AnimationController? _ioAnimationController;

        private CupertinoTextMagnifier CurrentWidget => (CupertinoTextMagnifier)StateWidget;

        internal Point CurrentAdjustedMagnifierPosition => _currentAdjustedMagnifierPosition;

        internal double VerticalFocalPointAdjustment => _verticalFocalPointAdjustment;

        public override void InitState()
        {
            base.InitState();
            _ioAnimationController = new AnimationController(CupertinoMagnifier.InOutAnimationDuration, this)
            {
                Curve = CurrentWidget.AnimationCurve,
            };
            _ioAnimationController.Changed += HandleAnimationChanged;
            CurrentWidget.Controller.AnimationController = _ioAnimationController;
            CurrentWidget.MagnifierInfo.AddListener(DetermineMagnifierPositionAndFocalPoint);
        }

        public override void DidChangeDependencies()
        {
            DetermineMagnifierPositionAndFocalPoint();
            base.DidChangeDependencies();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldMagnifier = (CupertinoTextMagnifier)oldWidget;
            if (!ReferenceEquals(oldMagnifier.Controller, CurrentWidget.Controller))
            {
                if (ReferenceEquals(oldMagnifier.Controller.AnimationController, _ioAnimationController))
                {
                    oldMagnifier.Controller.AnimationController = null;
                }

                CurrentWidget.Controller.AnimationController = _ioAnimationController;
            }

            if (!ReferenceEquals(oldMagnifier.MagnifierInfo, CurrentWidget.MagnifierInfo))
            {
                oldMagnifier.MagnifierInfo.RemoveListener(DetermineMagnifierPositionAndFocalPoint);
                CurrentWidget.MagnifierInfo.AddListener(DetermineMagnifierPositionAndFocalPoint);
                DetermineMagnifierPositionAndFocalPoint();
            }

            _ioAnimationController!.Curve = CurrentWidget.AnimationCurve;
            base.DidUpdateWidget(oldWidget);
        }

        public override Widget Build(BuildContext context)
        {
            return new AnimatedPositioned(
                duration: DragAnimationDuration,
                curve: CurrentWidget.AnimationCurve,
                left: _currentAdjustedMagnifierPosition.X,
                top: _currentAdjustedMagnifierPosition.Y,
                child: new CupertinoMagnifier(
                    inOutAnimation: _ioAnimationController,
                    additionalFocalPointOffset: new Point(0, _verticalFocalPointAdjustment)));
        }

        public override void Dispose()
        {
            if (ReferenceEquals(CurrentWidget.Controller.AnimationController, _ioAnimationController))
            {
                CurrentWidget.Controller.AnimationController = null;
            }

            CurrentWidget.MagnifierInfo.RemoveListener(DetermineMagnifierPositionAndFocalPoint);
            if (_ioAnimationController != null)
            {
                _ioAnimationController.Changed -= HandleAnimationChanged;
                _ioAnimationController.Dispose();
                _ioAnimationController = null;
            }

            base.Dispose();
        }

        private void DetermineMagnifierPositionAndFocalPoint()
        {
            MagnifierInfo textEditingContext = CurrentWidget.MagnifierInfo.Value;
            double lineCenterY = textEditingContext.CaretRect.Center.Y;
            if (lineCenterY - textEditingContext.GlobalGesturePosition.Y < -CurrentWidget.HideBelowThreshold)
            {
                if (CurrentWidget.Controller.Shown)
                {
                    _ = CurrentWidget.Controller.Hide(removeFromOverlay: false);
                }

                return;
            }

            if (!CurrentWidget.Controller.Shown)
            {
                _ioAnimationController!.Forward();
            }

            double verticalLensPosition = Math.Max(
                lineCenterY,
                lineCenterY
                - ((lineCenterY - textEditingContext.GlobalGesturePosition.Y) / CurrentWidget.DragResistance));
            var rawPosition = new Point(
                textEditingContext.GlobalGesturePosition.X - (CupertinoMagnifier.DefaultSize.Width / 2.0),
                verticalLensPosition
                - (CupertinoMagnifier.DefaultSize.Height - CupertinoMagnifier.MagnifierAboveFocalPoint));
            Size screenSize = MediaQuery.Of(Context).Size;
            var paddedBounds = new Rect(
                CurrentWidget.HorizontalScreenEdgePadding,
                -(CupertinoMagnifier.DefaultSize.Height + CupertinoMagnifier.MagnifierAboveFocalPoint),
                Math.Max(0, screenSize.Width - (CurrentWidget.HorizontalScreenEdgePadding * 2.0)),
                screenSize.Height
                + ((CupertinoMagnifier.DefaultSize.Height + CupertinoMagnifier.MagnifierAboveFocalPoint) * 2.0));
            Point adjustedPosition = MagnifierController.ShiftWithinBounds(
                new Rect(rawPosition, CupertinoMagnifier.DefaultSize),
                paddedBounds).Position;

            SetState(() =>
            {
                _currentAdjustedMagnifierPosition = adjustedPosition;
                _verticalFocalPointAdjustment = lineCenterY - verticalLensPosition;
            });
        }

        private void HandleAnimationChanged()
        {
            if (Mounted)
            {
                SetState(() => { });
            }
        }
    }
}

public sealed class CupertinoMagnifier : StatelessWidget
{
    public const double MagnifierAboveFocalPoint = -26.0;
    public static readonly Size DefaultSize = new(80, 47.5);
    internal static readonly TimeSpan InOutAnimationDuration = TimeSpan.FromMilliseconds(150);

    public CupertinoMagnifier(
        Size? size = null,
        BorderRadius? borderRadius = null,
        Point additionalFocalPointOffset = default,
        BoxShadows? shadows = null,
        Clip clipBehavior = Clip.None,
        BorderSide? borderSide = null,
        Animation<double>? inOutAnimation = null,
        double magnificationScale = 1.0,
        Key? key = null) : base(key)
    {
        if (!double.IsFinite(magnificationScale) || magnificationScale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(magnificationScale));
        }

        Size = size ?? DefaultSize;
        BorderRadius = borderRadius ?? Plumix.Rendering.BorderRadius.Circular(60);
        AdditionalFocalPointOffset = additionalFocalPointOffset;
        Shadows = shadows ?? DefaultShadows;
        ClipBehavior = clipBehavior;
        BorderSide = borderSide ?? new BorderSide(Color.FromArgb(255, 0, 124, 255), 2);
        InOutAnimation = inOutAnimation;
        MagnificationScale = magnificationScale;
    }

    public Size Size { get; }

    public BorderRadius BorderRadius { get; }

    public Point AdditionalFocalPointOffset { get; }

    public BoxShadows Shadows { get; }

    public Clip ClipBehavior { get; }

    public BorderSide BorderSide { get; }

    public Animation<double>? InOutAnimation { get; }

    public double MagnificationScale { get; }

    internal static BoxShadows DefaultShadows { get; } = new(new BoxShadow
    {
        Blur = 11,
        Spread = 0.2,
        Color = Color.FromArgb(25, 0, 0, 0),
    });

    public override Widget Build(BuildContext context)
    {
        double animationValue = InOutAnimation?.Value ?? 1.0;
        var focalPointOffset = new Point(
            0,
            ((DefaultSize.Height / 2.0) - MagnifierAboveFocalPoint) * animationValue)
            + AdditionalFocalPointOffset;
        double translationY = (0 - MagnifierAboveFocalPoint) * (1.0 - animationValue);
        return new Plumix.Widgets.Transform(
            transform: Matrix.CreateTranslation(0, translationY),
            child: new RawMagnifier(
                size: Size,
                focalPointOffset: focalPointOffset,
                decoration: new MagnifierDecoration(
                    opacity: animationValue,
                    shape: ShapeBorder.RoundedRectangle(BorderRadius.Radius, BorderSide),
                    shadows: Shadows),
                clipBehavior: ClipBehavior,
                magnificationScale: MagnificationScale));
    }
}
