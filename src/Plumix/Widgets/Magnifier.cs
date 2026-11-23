using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Path = Plumix.UI.Path;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/magnifier.dart

namespace Plumix.Widgets;

public delegate Widget? MagnifierBuilder(
    BuildContext context,
    MagnifierController controller,
    ValueNotifier<MagnifierInfo> magnifierInfo);

public sealed record MagnifierInfo(
    Offset GlobalGesturePosition,
    Rect CaretRect,
    Rect FieldBounds,
    Rect CurrentLineBoundaries)
{
    public static MagnifierInfo Empty { get; } = new(default, default, default, default);
}

public sealed class TextMagnifierConfiguration
{
    public TextMagnifierConfiguration(
        MagnifierBuilder? magnifierBuilder = null,
        bool shouldDisplayHandlesInMagnifier = true)
    {
        MagnifierBuilder = magnifierBuilder ?? None;
        ShouldDisplayHandlesInMagnifier = shouldDisplayHandlesInMagnifier;
    }

    public MagnifierBuilder MagnifierBuilder { get; }

    public bool ShouldDisplayHandlesInMagnifier { get; }

    public static TextMagnifierConfiguration Disabled { get; } = new();

    private static Widget? None(
        BuildContext context,
        MagnifierController controller,
        ValueNotifier<MagnifierInfo> magnifierInfo)
    {
        return null;
    }
}

public sealed class MagnifierController
{
    private OverlayEntry? _overlayEntry;

    public MagnifierController(AnimationController? animationController = null)
    {
        AnimationController = animationController;
        AnimationController?.SetValue(0);
    }

    public AnimationController? AnimationController { get; set; }

    public OverlayEntry? OverlayEntry => _overlayEntry;

    public bool Shown => _overlayEntry is not null
                         && (AnimationController?.Status is null
                             or AnimationStatus.Forward
                             or AnimationStatus.Completed);

    public async Task Show(
        BuildContext context,
        Func<BuildContext, Widget> builder,
        Widget? debugRequiredFor = null,
        OverlayEntry? below = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        RemoveFromOverlay();

        OverlayState overlayState = Overlay.Of(context, rootOverlay: true);
        CapturedThemes capturedThemes = InheritedTheme.Capture(
            context,
            Navigator.MaybeOf(context)?.Context);
        _overlayEntry = new OverlayEntry(
            overlayContext => capturedThemes.Wrap(builder(overlayContext)));
        overlayState.Insert(_overlayEntry, below: below);

        AnimationController? controller = AnimationController;
        if (controller == null)
        {
            return;
        }

        Task completion = WaitForStatus(controller, AnimationStatus.Completed);
        controller.Forward();
        await completion;
    }

    public async Task Hide(bool removeFromOverlay = true)
    {
        if (_overlayEntry is null)
        {
            return;
        }

        AnimationController? controller = AnimationController;
        if (controller != null)
        {
            Task completion = WaitForStatus(controller, AnimationStatus.Dismissed);
            controller.Reverse();
            await completion;
        }

        if (removeFromOverlay)
        {
            RemoveFromOverlay();
        }
    }

    public void RemoveFromOverlay()
    {
        OverlayEntry? overlayEntry = _overlayEntry;
        if (overlayEntry is null)
        {
            return;
        }

        overlayEntry.Remove();
        overlayEntry.Dispose();
        _overlayEntry = null;
    }

    public static Rect ShiftWithinBounds(Rect rect, Rect bounds)
    {
        if (rect.Width > bounds.Width || rect.Height > bounds.Height)
        {
            throw new ArgumentException("rect must fit within bounds.", nameof(rect));
        }

        double dx = rect.Left < bounds.Left
            ? bounds.Left - rect.Left
            : rect.Right > bounds.Right
                ? bounds.Right - rect.Right
                : 0;
        double dy = rect.Top < bounds.Top
            ? bounds.Top - rect.Top
            : rect.Bottom > bounds.Bottom
                ? bounds.Bottom - rect.Bottom
                : 0;
        return rect.TopLeft + new Offset(dx, dy) & rect.Size;
    }

    private static Task WaitForStatus(AnimationController controller, AnimationStatus target)
    {
        if (controller.Status == target)
        {
            return Task.CompletedTask;
        }

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Action<AnimationStatus>? listener = null;
        listener = status =>
        {
            if (status != target)
            {
                return;
            }

            controller.RemoveStatusListener(listener!);
            completion.TrySetResult();
        };
        controller.AddStatusListener(listener);
        return completion.Task;
    }
}

/// <summary>
/// A magnifier that enlarges what lies behind it: Dart's <c>RawMagnifier</c>.
/// </summary>
/// <remarks>
/// With a backend that filters backdrops (the native Impeller host) it builds Dart's composition: the magnified
/// backdrop clipped to the shape, then the decoration in a younger sibling. Otherwise (Avalonia) it lays out a
/// <see cref="RenderMagnifier"/>, whose Plumix-only lens layer the host fills from a lens-free snapshot
/// (docs/ai/DIVERGENCES.md).
/// </remarks>
public sealed class RawMagnifier : StatelessWidget
{
    public RawMagnifier(
        Size size,
        Widget? child = null,
        MagnifierDecoration? decoration = null,
        Clip clipBehavior = Clip.None,
        Offset focalPointOffset = default,
        double magnificationScale = 1.0,
        Key? key = null) : base(key)
    {
        if (!double.IsFinite(size.Width) || !double.IsFinite(size.Height)
            || size.Width < 0 || size.Height < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

        if (!double.IsFinite(magnificationScale) || magnificationScale == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(magnificationScale));
        }

        Size = size;
        Child = child;
        Decoration = decoration ?? new MagnifierDecoration();
        ClipBehavior = clipBehavior;
        FocalPointOffset = focalPointOffset;
        MagnificationScale = magnificationScale;
    }

    public Size Size { get; }

    public Widget? Child { get; }

    public MagnifierDecoration Decoration { get; }

    public Clip ClipBehavior { get; }

    public Offset FocalPointOffset { get; }

    public double MagnificationScale { get; }

    public override Widget Build(BuildContext context)
    {
        if (!RenderBackend.Current.SupportsBackdropFilters)
        {
            return new LensMagnifier(this);
        }

        return new Stack(
            clipBehavior: Clip.None,
            alignment: Alignment.Center,
            children:
            [
                // The magnified image is clipped to the outer path of the shape.
                ClipPath.Shape(
                    shape: Decoration.Shape,
                    child: new Opacity(
                        Decoration.Opacity,
                        child: new Magnification(
                            FocalPointOffset,
                            MagnificationScale,
                            child: SizedBox.FromSize(Size, Child)))),
                // Because `BackdropFilter` will filter any widgets before it, we apply these styles after (i.e. in
                // a younger sibling) to avoid the magnifier from seeing its own styling.
                new IgnorePointer(
                    child: new Opacity(
                        Decoration.Opacity,
                        child: new ClipPath(
                            clipBehavior: ClipBehavior,
                            clipper: new NegativeClip(Decoration.Shape),
                            child: new DecoratedBox(
                                new ShapeDecoration(Decoration.Shape, Shadows: Decoration.Shadows),
                                child: SizedBox.FromSize(Size))))),
            ]);
    }

    // Dart's `_NegativeClip`: a clip that renders everything except the inside of a shape.
    private sealed class NegativeClip(ShapeBorder shape) : CustomClipper<Path>
    {
        private readonly ShapeBorder _shape = shape;

        public override Path GetClip(Size size)
        {
            var path = new Path { FillType = PathFillType.EvenOdd };
            path.AddRect(Rect.Largest);
            path.AddPath(_shape.GetInnerPath(Offset.Zero & size), Offset.Zero);
            return path;
        }

        public override bool ShouldReclip(CustomClipper<Path> oldClipper) =>
            oldClipper is not NegativeClip old || !Equals(old._shape, _shape);
    }

    // Dart's `_Magnifier`.
    private sealed class Magnification(Offset focalPointOffset, double magnificationScale, Widget? child)
        : SingleChildRenderObjectWidget(child)
    {
        public override RenderObject CreateRenderObject(BuildContext context) =>
            new RenderMagnification(focalPointOffset, magnificationScale);

        public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
        {
            var magnification = (RenderMagnification)renderObject;
            magnification.FocalPointOffset = focalPointOffset;
            magnification.MagnificationScale = magnificationScale;
        }
    }

    // The Plumix-only lens for backends without backdrop filters.
    private sealed class LensMagnifier(RawMagnifier magnifier) : SingleChildRenderObjectWidget(magnifier.Child)
    {
        public override RenderObject CreateRenderObject(BuildContext context) => new RenderMagnifier(
            magnifier.Size,
            magnifier.Decoration,
            magnifier.ClipBehavior,
            magnifier.FocalPointOffset,
            magnifier.MagnificationScale);

        public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
        {
            var render = (RenderMagnifier)renderObject;
            render.RequestedSize = magnifier.Size;
            render.Decoration = magnifier.Decoration;
            render.ClipBehavior = magnifier.ClipBehavior;
            render.FocalPointOffset = magnifier.FocalPointOffset;
            render.MagnificationScale = magnifier.MagnificationScale;
        }
    }
}
