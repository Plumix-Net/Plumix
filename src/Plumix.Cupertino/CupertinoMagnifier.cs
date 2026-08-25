using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using BoxShadow = Plumix.Rendering.BoxShadow;

// Dart parity source: cupertino_ui/lib/src/magnifier.dart

namespace Plumix.Cupertino;

/// <summary>
/// A <see cref="CupertinoMagnifier"/> used for magnifying text in cases where a user's finger may be
/// blocking the point of interest, like a selection handle.
/// </summary>
/// <remarks>
/// Delegates styling to <see cref="CupertinoMagnifier"/> with its position depending on
/// <see cref="MagnifierInfo"/>: it stays inside the screen width with
/// <see cref="HorizontalScreenEdgePadding"/> padding, hides once the gesture drops
/// <see cref="HideBelowThreshold"/> units below the line it sits on, follows the gesture's x
/// coordinate, and drags downward with a <see cref="DragResistance"/> divisor.
/// </remarks>
public sealed class CupertinoTextMagnifier : StatefulWidget
{
    /// <summary>The duration that the magnifier drags behind its final position.</summary>
    private static readonly TimeSpan DragAnimationDuration = TimeSpan.FromMilliseconds(45);

    public CupertinoTextMagnifier(
        MagnifierController controller,
        ValueNotifier<MagnifierInfo> magnifierInfo,
        Curve? animationCurve = null,
        double dragResistance = 10.0,
        double hideBelowThreshold = 48.0,
        double horizontalScreenEdgePadding = 10.0,
        Key? key = null) : base(key)
    {
        Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        MagnifierInfo = magnifierInfo ?? throw new ArgumentNullException(nameof(magnifierInfo));
        AnimationCurve = animationCurve ?? Curves.EaseOut;
        DragResistance = dragResistance;
        HideBelowThreshold = hideBelowThreshold;
        HorizontalScreenEdgePadding = horizontalScreenEdgePadding;
    }

    /// <summary>The curve used for the in / out animations.</summary>
    public Curve AnimationCurve { get; }

    /// <summary>This magnifier's controller, used to show / hide without leaving the overlay.</summary>
    public MagnifierController Controller { get; }

    /// <summary>A drag resistance on the downward Y position of the lens.</summary>
    public double DragResistance { get; }

    /// <summary>
    /// The difference in Y between the gesture position and the caret center at which the magnifier
    /// hides itself.
    /// </summary>
    public double HideBelowThreshold { get; }

    /// <summary>The padding on either edge of the screen no part of the magnifier may cross.</summary>
    public double HorizontalScreenEdgePadding { get; }

    /// <summary>The notifier this magnifier determines its own positioning from.</summary>
    public ValueNotifier<MagnifierInfo> MagnifierInfo { get; }

    public override State CreateState() => new CupertinoTextMagnifierState();

    internal sealed class CupertinoTextMagnifierState : State
    {
        // Initialize to dummy values for the event that the initial call to
        // DetermineMagnifierPositionAndFocalPoint calls hide, and thus does not set these values.
        private Point _currentAdjustedMagnifierPosition;
        private double _verticalFocalPointAdjustment;
        private AnimationController? _ioAnimationController;
        private CurvedAnimation? _ioCurvedAnimation;
        private Animation<double>? _ioAnimation;

        private CupertinoTextMagnifier CurrentWidget => (CupertinoTextMagnifier)StateWidget;

        internal Point CurrentAdjustedMagnifierPosition => _currentAdjustedMagnifierPosition;

        internal double VerticalFocalPointAdjustment => _verticalFocalPointAdjustment;

        public override void InitState()
        {
            base.InitState();
            _ioAnimationController = new AnimationController(
                value: 0,
                duration: CupertinoMagnifier.InOutAnimationDuration,
                vsync: this);
            _ioAnimationController.Changed += HandleAnimationChanged;

            CurrentWidget.Controller.AnimationController = _ioAnimationController;
            CurrentWidget.MagnifierInfo.AddListener(DetermineMagnifierPositionAndFocalPoint);
            _ioCurvedAnimation = new CurvedAnimation(
                parent: _ioAnimationController,
                curve: CurrentWidget.AnimationCurve);
            _ioAnimation = new DoubleTween(0.0, 1.0).Animate(_ioCurvedAnimation);
        }

        public override void DidChangeDependencies()
        {
            DetermineMagnifierPositionAndFocalPoint();
            base.DidChangeDependencies();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldMagnifier = (CupertinoTextMagnifier)oldWidget;
            if (!ReferenceEquals(oldMagnifier.MagnifierInfo, CurrentWidget.MagnifierInfo))
            {
                oldMagnifier.MagnifierInfo.RemoveListener(DetermineMagnifierPositionAndFocalPoint);
                CurrentWidget.MagnifierInfo.AddListener(DetermineMagnifierPositionAndFocalPoint);
            }

            base.DidUpdateWidget(oldWidget);
        }

        public override Widget Build(BuildContext context)
        {
            var themeData = CupertinoTheme.Of(context);
            return new AnimatedPositioned(
                duration: DragAnimationDuration,
                curve: CurrentWidget.AnimationCurve,
                left: _currentAdjustedMagnifierPosition.X,
                top: _currentAdjustedMagnifierPosition.Y,
                child: new CupertinoMagnifier(
                    inOutAnimation: _ioAnimation,
                    additionalFocalPointOffset: new Point(0, _verticalFocalPointAdjustment),
                    borderSide: new BorderSide(themeData.PrimaryColor, 2.0)));
        }

        public override void Dispose()
        {
            CurrentWidget.Controller.AnimationController = null;
            if (_ioAnimationController != null)
            {
                _ioAnimationController.Changed -= HandleAnimationChanged;
                _ioAnimationController.Dispose();
                _ioAnimationController = null;
            }

            _ioCurvedAnimation?.Dispose();
            _ioCurvedAnimation = null;
            _ioAnimation = null;
            CurrentWidget.MagnifierInfo.RemoveListener(DetermineMagnifierPositionAndFocalPoint);
            base.Dispose();
        }

        private void DetermineMagnifierPositionAndFocalPoint()
        {
            MagnifierInfo textEditingContext = CurrentWidget.MagnifierInfo.Value;

            // The exact Y of the center of the current line.
            double verticalCenterOfCurrentLine = textEditingContext.CaretRect.Center.Y;

            // If the magnifier is currently showing, but we have dragged out of threshold, hide it.
            if (verticalCenterOfCurrentLine - textEditingContext.GlobalGesturePosition.Y
                < -CurrentWidget.HideBelowThreshold)
            {
                // Only signal a hide if we are currently showing.
                if (CurrentWidget.Controller.Shown)
                {
                    _ = CurrentWidget.Controller.Hide(removeFromOverlay: false);
                }

                return;
            }

            // If we are gone, but got to this point, we shouldn't be: show.
            if (!CurrentWidget.Controller.Shown)
            {
                _ioAnimationController!.Forward();
            }

            // Never go above the center of the line, but have some resistance going downward if the
            // drag goes too far.
            double verticalPositionOfLens = Math.Max(
                verticalCenterOfCurrentLine,
                verticalCenterOfCurrentLine
                - ((verticalCenterOfCurrentLine - textEditingContext.GlobalGesturePosition.Y)
                   / CurrentWidget.DragResistance));

            // The raw position, tracking the gesture directly.
            var rawMagnifierPosition = new Point(
                textEditingContext.GlobalGesturePosition.X - (CupertinoMagnifier.DefaultSize.Width / 2.0),
                verticalPositionOfLens
                - (CupertinoMagnifier.DefaultSize.Height - CupertinoMagnifier.MagnifierAboveFocalPoint));

            Size screenSize = MediaQuery.Of(Context).Size;

            // Adjust the magnifier position so that it never exists outside the horizontal padding.
            // iOS doesn't reposition for Y, so the vertical threshold is expanded far enough to send
            // the whole magnifier out of bounds if need be.
            double verticalSlack =
                CupertinoMagnifier.DefaultSize.Height + CupertinoMagnifier.MagnifierAboveFocalPoint;
            var paddedBounds = new Rect(
                CurrentWidget.HorizontalScreenEdgePadding,
                -verticalSlack,
                Math.Max(0, screenSize.Width - (CurrentWidget.HorizontalScreenEdgePadding * 2.0)),
                screenSize.Height + (verticalSlack * 2.0));
            Point adjustedMagnifierPosition = MagnifierController.ShiftWithinBounds(
                new Rect(rawMagnifierPosition, CupertinoMagnifier.DefaultSize),
                paddedBounds).Position;

            SetState(() =>
            {
                _currentAdjustedMagnifierPosition = adjustedMagnifierPosition;

                // The lens should always point to the center of the line.
                _verticalFocalPointAdjustment = verticalCenterOfCurrentLine - verticalPositionOfLens;
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

/// <summary>
/// A <see cref="RawMagnifier"/> used for magnifying text in cases where a user's finger may be
/// blocking the point of interest, like a selection handle. Handles styling and transitions;
/// positioning is left to <see cref="CupertinoTextMagnifier"/>.
/// </summary>
public sealed class CupertinoMagnifier : StatelessWidget
{
    /// <summary>The vertical offset that the magnifier is along the Y axis above the focal point.</summary>
    public const double MagnifierAboveFocalPoint = -26.0;

    /// <summary>The default size of the magnifier, which positioners may depend on.</summary>
    public static readonly Size DefaultSize = new(80, 47.5);

    /// <summary>The duration that this magnifier animates in / out for.</summary>
    internal static readonly TimeSpan InOutAnimationDuration = TimeSpan.FromMilliseconds(150);

    public CupertinoMagnifier(
        Size? size = null,
        BorderRadius? borderRadius = null,
        Point additionalFocalPointOffset = default,
        IReadOnlyList<BoxShadow>? shadows = null,
        Clip clipBehavior = Clip.None,
        BorderSide? borderSide = null,
        Animation<double>? inOutAnimation = null,
        double magnificationScale = 1.0,
        Key? key = null) : base(key)
    {
        if (!(magnificationScale > 0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(magnificationScale),
                magnificationScale,
                "The magnification scale should be greater than zero.");
        }

        Size = size ?? DefaultSize;
        BorderRadius = borderRadius ?? DefaultBorderRadius;
        AdditionalFocalPointOffset = additionalFocalPointOffset;
        Shadows = shadows ?? DefaultShadows;
        ClipBehavior = clipBehavior;
        BorderSide = borderSide ?? DefaultBorderSide;
        InOutAnimation = inOutAnimation;
        MagnificationScale = magnificationScale;
    }

    /// <summary>The size of this magnifier, excluding <see cref="BorderSide"/> and <see cref="Shadows"/>.</summary>
    public Size Size { get; }

    /// <summary>The border radius of this magnifier's `RoundedRectangleBorder` shape.</summary>
    public BorderRadius BorderRadius { get; }

    /// <summary>
    /// Any additional focal point offset, applied over the regular offset defined in
    /// <see cref="MagnifierAboveFocalPoint"/>.
    /// </summary>
    public Point AdditionalFocalPointOffset { get; }

    /// <summary>A list of shadows cast by the magnifier.</summary>
    public IReadOnlyList<BoxShadow> Shadows { get; }

    /// <summary>Whether and how to clip the <see cref="Shadows"/> that render inside the loupe.</summary>
    public Clip ClipBehavior { get; }

    /// <summary>The border, or "rim", of this magnifier.</summary>
    public BorderSide BorderSide { get; }

    /// <summary>
    /// This magnifier's in / out animation. <see cref="CupertinoMagnifier"/> has no knowledge of
    /// shown / hidden state, so this animation is driven by an external actor.
    /// </summary>
    public Animation<double>? InOutAnimation { get; }

    /// <summary>The magnification scale for the magnifier; 1.0 applies no magnification.</summary>
    public double MagnificationScale { get; }

    internal static BorderRadius DefaultBorderRadius { get; } =
        Plumix.Rendering.BorderRadius.All(Radius.Elliptical(60, 50));

    internal static BorderSide DefaultBorderSide { get; } =
        new(Color.FromArgb(255, 0, 124, 255), 2.0);

    internal static IReadOnlyList<BoxShadow> DefaultShadows { get; } =
    [
        new BoxShadow(
            color: Color.FromArgb(25, 0, 0, 0),
            blurRadius: 11,
            spreadRadius: 0.2,
            blurStyle: BlurStyle.Outer),
    ];

    public override Widget Build(BuildContext context)
    {
        double animationValue = InOutAnimation?.Value ?? 1.0;

        // Dart calls `focalPointOffset.scale(1, inOutAnimation?.value ?? 1)` and discards the result
        // (`Offset.scale` returns a new offset), so the focal point does not animate upstream either.
        var focalPointOffset =
            new Point(0, (DefaultSize.Height / 2.0) - MagnifierAboveFocalPoint) + AdditionalFocalPointOffset;

        return Widgets.Transform.Translate(
            offset: LerpOffset(new Point(0, -MagnifierAboveFocalPoint), default, animationValue),
            child: new RawMagnifier(
                size: Size,
                focalPointOffset: focalPointOffset,
                decoration: new MagnifierDecoration(
                    opacity: animationValue,
                    shape: new RoundedRectangleBorder(BorderSide, BorderRadius),
                    shadows: Shadows),
                clipBehavior: ClipBehavior,
                magnificationScale: MagnificationScale));
    }

    private static Point LerpOffset(Point a, Point b, double t)
    {
        return new Point(a.X + ((b.X - a.X) * t), a.Y + ((b.Y - a.Y) * t));
    }
}
