using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Tests;

/// <summary>
/// A <see cref="CustomClipper{T}"/> that clips to a fixed rectangle, mirroring the
/// <c>_FixedRectClipper</c> Flutter's own render tests use. Assigning <see cref="Rect"/> notifies
/// through the <c>reclip</c> listenable, the way a repainting clipper does in Dart.
/// </summary>
internal sealed class FixedRectClipper : CustomClipper<Rect>
{
    private readonly ClipperNotifier _notifier;
    private Rect _rect;

    private FixedRectClipper(Rect rect, ClipperNotifier notifier) : base(notifier)
    {
        _rect = rect;
        _notifier = notifier;
    }

    public FixedRectClipper(Rect rect) : this(rect, new ClipperNotifier())
    {
    }

    public Rect Rect
    {
        get => _rect;
        set
        {
            if (_rect == value)
            {
                return;
            }

            _rect = value;
            _notifier.Notify();
        }
    }

    public override Rect GetClip(Size size) => _rect;

    public override Rect GetApproximateClipRect(Size size) => _rect;

    public override bool ShouldReclip(CustomClipper<Rect> oldClipper) =>
        oldClipper is not FixedRectClipper other || other._rect != _rect;

    private sealed class ClipperNotifier : ChangeNotifier
    {
        public void Notify() => NotifyListeners();
    }
}
