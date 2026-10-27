using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using BoxShadow = Plumix.Rendering.BoxShadow;

// Dart parity source: material_ui/lib/src/magnifier.dart

namespace Plumix.Material;

public sealed class TextMagnifier : StatefulWidget
{
    public static readonly TimeSpan JumpBetweenLinesAnimationDuration = TimeSpan.FromMilliseconds(70);

    public TextMagnifier(ValueNotifier<MagnifierInfo> magnifierInfo, Key? key = null) : base(key)
    {
        MagnifierInfo = magnifierInfo ?? throw new ArgumentNullException(nameof(magnifierInfo));
    }

    public ValueNotifier<MagnifierInfo> MagnifierInfo { get; }

    public static TextMagnifierConfiguration AdaptiveMagnifierConfiguration { get; set; } =
        new(
            magnifierBuilder: BuildAdaptiveMagnifier,
            shouldDisplayHandlesInMagnifier: OperatingSystem.IsIOS());

    public override State CreateState() => new TextMagnifierState();

    private static Widget? BuildAdaptiveMagnifier(
        BuildContext context,
        MagnifierController controller,
        ValueNotifier<MagnifierInfo> magnifierInfo)
    {
        return Theme.Of(context).Platform switch
        {
            TargetPlatform.IOS => new CupertinoTextMagnifier(controller, magnifierInfo),
            TargetPlatform.Android => new TextMagnifier(magnifierInfo),
            _ => null,
        };
    }

    internal sealed class TextMagnifierState : State<TextMagnifier>
    {
        private Point? _magnifierPosition;
        private Point _extraFocalPointOffset;
        // A timer that unsets itself after an animation duration. If the timer exists, then the
        // magnifier animates its position; if not, it tracks the gesture directly.
        private Timer? _positionShouldBeAnimatedTimer;

        private TextMagnifier CurrentWidget => (TextMagnifier)StateWidget;

        internal Point? MagnifierPosition => _magnifierPosition;

        internal Point ExtraFocalPointOffset => _extraFocalPointOffset;

        internal bool PositionShouldBeAnimated => _positionShouldBeAnimatedTimer != null;

        public override void InitState()
        {
            base.InitState();
            CurrentWidget.MagnifierInfo.AddListener(DetermineMagnifierPositionAndFocalPoint);
        }

        public override void DidChangeDependencies()
        {
            DetermineMagnifierPositionAndFocalPoint();
            base.DidChangeDependencies();
        }

        public override void DidUpdateWidget(TextMagnifier oldWidget)
        {
            var oldMagnifier = (TextMagnifier)oldWidget;
            if (!ReferenceEquals(oldMagnifier.MagnifierInfo, CurrentWidget.MagnifierInfo))
            {
                oldMagnifier.MagnifierInfo.RemoveListener(DetermineMagnifierPositionAndFocalPoint);
                CurrentWidget.MagnifierInfo.AddListener(DetermineMagnifierPositionAndFocalPoint);
                DetermineMagnifierPositionAndFocalPoint();
            }

            base.DidUpdateWidget(oldWidget);
        }

        public override Widget Build(BuildContext context)
        {
            Point position = _magnifierPosition ?? default;
            return new AnimatedPositioned(
                left: position.X,
                top: position.Y,
                duration: PositionShouldBeAnimated
                    ? JumpBetweenLinesAnimationDuration
                    : TimeSpan.Zero,
                child: new Magnifier(additionalFocalPointOffset: _extraFocalPointOffset));
        }

        public override void Dispose()
        {
            CurrentWidget.MagnifierInfo.RemoveListener(DetermineMagnifierPositionAndFocalPoint);
            _positionShouldBeAnimatedTimer?.Cancel();
            base.Dispose();
        }

        private void DetermineMagnifierPositionAndFocalPoint()
        {
            MagnifierInfo selectionInfo = CurrentWidget.MagnifierInfo.Value;
            Size screenSize = MediaQuery.Of(Context).Size;
            var screenRect = new Rect(default, screenSize);
            var basicMagnifierOffset = new Point(
                Magnifier.DefaultMagnifierSize.Width / 2.0,
                Magnifier.DefaultMagnifierSize.Height + Magnifier.StandardVerticalFocalPointShift);

            double magnifierX = Math.Clamp(
                selectionInfo.GlobalGesturePosition.X,
                selectionInfo.CurrentLineBoundaries.Left,
                selectionInfo.CurrentLineBoundaries.Right);
            var unadjustedMagnifierRect = new Rect(
                new Point(magnifierX, selectionInfo.CaretRect.Center.Y) - basicMagnifierOffset,
                Magnifier.DefaultMagnifierSize);
            Rect adjustedMagnifierRect = MagnifierController.ShiftWithinBounds(
                unadjustedMagnifierRect,
                screenRect);
            Point finalMagnifierPosition = adjustedMagnifierRect.Position;

            double horizontalFocalInset =
                (Magnifier.DefaultMagnifierSize.Width / 2.0) / Magnifier.Magnification;
            double globalFocalPointX = selectionInfo.FieldBounds.Width < horizontalFocalInset * 2.0
                ? selectionInfo.FieldBounds.Center.X
                : Math.Clamp(
                    adjustedMagnifierRect.Center.X,
                    selectionInfo.FieldBounds.Left + horizontalFocalInset,
                    selectionInfo.FieldBounds.Right - horizontalFocalInset);
            double relativeFocalPointX = globalFocalPointX - adjustedMagnifierRect.Center.X;
            var focalPointAdjustment = new Point(
                relativeFocalPointX,
                unadjustedMagnifierRect.Top - adjustedMagnifierRect.Top);

            Timer? positionShouldBeAnimated = _positionShouldBeAnimatedTimer;
            if (_magnifierPosition.HasValue && finalMagnifierPosition.Y != _magnifierPosition.Value.Y)
            {
                if (_positionShouldBeAnimatedTimer is { IsActive: true })
                {
                    _positionShouldBeAnimatedTimer.Cancel();
                }

                // A timer that deletes itself when it completes; `Mounted`-safe, since the timer is
                // cancelled in `Dispose`.
                positionShouldBeAnimated = new Timer(
                    JumpBetweenLinesAnimationDuration,
                    () => SetState(() => _positionShouldBeAnimatedTimer = null));
            }

            SetState(() =>
            {
                _magnifierPosition = finalMagnifierPosition;
                _positionShouldBeAnimatedTimer = positionShouldBeAnimated;
                _extraFocalPointOffset = focalPointAdjustment;
            });
        }
    }
}

public sealed class Magnifier : StatelessWidget
{
    internal const double Magnification = 1.25;
    public static readonly Size DefaultMagnifierSize = new(77.37, 37.9);
    public const double StandardVerticalFocalPointShift = 22.0;

    public Magnifier(
        Point additionalFocalPointOffset = default,
        BorderRadius? borderRadius = null,
        Color? filmColor = null,
        IReadOnlyList<BoxShadow>? shadows = null,
        Clip clipBehavior = Clip.HardEdge,
        Size? size = null,
        Key? key = null) : base(key)
    {
        AdditionalFocalPointOffset = additionalFocalPointOffset;
        BorderRadius = borderRadius ?? Plumix.Rendering.BorderRadius.Circular(40);
        FilmColor = filmColor ?? Color.FromARGB(8, 158, 158, 158);
        Shadows = shadows ?? DefaultShadows;
        ClipBehavior = clipBehavior;
        Size = size ?? DefaultMagnifierSize;
    }

    public Point AdditionalFocalPointOffset { get; }

    public BorderRadius BorderRadius { get; }

    public Color FilmColor { get; }

    public IReadOnlyList<BoxShadow> Shadows { get; }

    public Clip ClipBehavior { get; }

    public Size Size { get; }

    internal static IReadOnlyList<BoxShadow> DefaultShadows { get; } =
    [
        new BoxShadow(
            color: Color.FromARGB(25, 0, 0, 0),
            offset: new Point(0, 2.0),
            blurRadius: 1.5,
            spreadRadius: 0.75),
    ];

    public override Widget Build(BuildContext context)
    {
        return new RawMagnifier(
            size: Size,
            decoration: new MagnifierDecoration(
                shape: new RoundedRectangleBorder(borderRadius:
                    Plumix.Rendering.BorderRadius.Circular(BorderRadius.Radius)),
                shadows: Shadows),
            clipBehavior: ClipBehavior,
            magnificationScale: Magnification,
            focalPointOffset: AdditionalFocalPointOffset + new Point(
                0,
                StandardVerticalFocalPointShift + (DefaultMagnifierSize.Height / 2.0)),
            child: new ColoredBox(FilmColor));
    }
}
