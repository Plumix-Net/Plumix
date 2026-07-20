using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/magnifier.dart

namespace Plumix.Widgets;

public delegate Widget? MagnifierBuilder(
    BuildContext context,
    MagnifierController controller,
    ValueNotifier<MagnifierInfo> magnifierInfo);

public sealed record MagnifierInfo(
    Point GlobalGesturePosition,
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
    private MagnifierRoute? _route;
    private NavigatorState? _navigator;

    public MagnifierController(AnimationController? animationController = null)
    {
        AnimationController = animationController;
        AnimationController?.SetValue(0);
    }

    public AnimationController? AnimationController { get; set; }

    public Route? OverlayEntry => _route;

    public bool Shown => _route != null
                         && (AnimationController?.Status is null
                             or AnimationStatus.Forward
                             or AnimationStatus.Completed);

    public async Task Show(
        BuildContext context,
        Func<BuildContext, Widget> builder,
        Widget? debugRequiredFor = null,
        Route? below = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        RemoveFromOverlay();

        _navigator = Navigator.Of(context, rootNavigator: true);
        _route = new MagnifierRoute(builder);
        _navigator.Push(_route);

        AnimationController? controller = AnimationController;
        if (controller == null)
        {
            return;
        }

        Task completion = WaitForStatus(controller, AnimationStatus.Completed);
        controller.Forward();
        await completion.ConfigureAwait(false);
    }

    public async Task Hide(bool removeFromOverlay = true)
    {
        if (_route == null)
        {
            return;
        }

        AnimationController? controller = AnimationController;
        if (controller != null)
        {
            Task completion = WaitForStatus(controller, AnimationStatus.Dismissed);
            controller.Reverse();
            await completion.ConfigureAwait(false);
        }

        if (removeFromOverlay)
        {
            RemoveFromOverlay();
        }
    }

    public void RemoveFromOverlay()
    {
        MagnifierRoute? route = _route;
        NavigatorState? navigator = _navigator;
        _route = null;
        _navigator = null;
        if (route != null && navigator != null)
        {
            navigator.RemoveRoute(route);
        }
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
        return new Rect(rect.Position + new Point(dx, dy), rect.Size);
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

    private sealed class MagnifierRoute : PageRoute
    {
        private readonly Func<BuildContext, Widget> _builder;

        public MagnifierRoute(Func<BuildContext, Widget> builder)
        {
            _builder = builder;
        }

        public override bool Opaque => false;

        public override Widget BuildPage(BuildContext context)
        {
            return new Stack(
                fit: StackFit.Expand,
                clipBehavior: Clip.None,
                children: [_builder(context)]);
        }
    }
}

public sealed class RawMagnifier : SingleChildRenderObjectWidget
{
    public RawMagnifier(
        Size size,
        Widget? child = null,
        MagnifierDecoration? decoration = null,
        Clip clipBehavior = Clip.None,
        Point focalPointOffset = default,
        double magnificationScale = 1.0,
        Key? key = null) : base(child, key)
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
        Decoration = decoration ?? new MagnifierDecoration();
        ClipBehavior = clipBehavior;
        FocalPointOffset = focalPointOffset;
        MagnificationScale = magnificationScale;
    }

    public Size Size { get; }

    public MagnifierDecoration Decoration { get; }

    public Clip ClipBehavior { get; }

    public Point FocalPointOffset { get; }

    public double MagnificationScale { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderMagnifier(
            Size,
            Decoration,
            ClipBehavior,
            FocalPointOffset,
            MagnificationScale);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var magnifier = (RenderMagnifier)renderObject;
        magnifier.RequestedSize = Size;
        magnifier.Decoration = Decoration;
        magnifier.ClipBehavior = ClipBehavior;
        magnifier.FocalPointOffset = FocalPointOffset;
        magnifier.MagnificationScale = MagnificationScale;
    }
}
