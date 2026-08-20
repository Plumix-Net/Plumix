using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using BoxShadow = Plumix.Rendering.BoxShadow;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/thumb_painter.dart

public sealed class CupertinoThumbPainter
{
    private static readonly Color ThumbBorderColor = Color.FromUInt32(0x0A000000);
    private static readonly IReadOnlyList<BoxShadow> SliderShadows =
    [
        new(color: Color.FromUInt32(0x26000000), offset: new Point(0.0, 3.0), blurRadius: 8.0),
        new(color: Color.FromUInt32(0x29000000), offset: new Point(0.0, 1.0), blurRadius: 1.0),
        new(color: Color.FromUInt32(0x1A000000), offset: new Point(0.0, 3.0), blurRadius: 1.0),
    ];
    private static readonly IReadOnlyList<BoxShadow> SwitchShadows =
    [
        new(color: Color.FromUInt32(0x26000000), offset: new Point(0.0, 3.0), blurRadius: 8.0),
        new(color: Color.FromUInt32(0x0F000000), offset: new Point(0.0, 3.0), blurRadius: 1.0),
    ];

    public CupertinoThumbPainter(Color? color = null, IReadOnlyList<BoxShadow>? shadows = null)
        : this(color ?? CupertinoColors.White, shadows ?? SliderShadows, useSwitchShadows: false)
    {
    }

    private CupertinoThumbPainter(Color color, IReadOnlyList<BoxShadow> shadows, bool useSwitchShadows)
    {
        _ = useSwitchShadows;
        Color = color;
        Shadows = shadows;
    }

    public const double Radius = 14.0;

    public const double Extension = 7.0;

    public Color Color { get; }

    public IReadOnlyList<BoxShadow> Shadows { get; }

    public static CupertinoThumbPainter SwitchThumb(
        Color? color = null,
        IReadOnlyList<BoxShadow>? shadows = null)
    {
        return new CupertinoThumbPainter(
            color ?? CupertinoColors.White,
            shadows ?? SwitchShadows,
            useSwitchShadows: true);
    }

    public void Paint(PaintingContext context, Rect rect)
    {
        ArgumentNullException.ThrowIfNull(context);
        double radius = Math.Min(rect.Width, rect.Height) / 2.0;
        var borderRadius = BorderRadius.Circular(radius);
        context.DrawRectangle(
            new SolidColorBrush(Color),
            null,
            rect,
            borderRadius,
            Shadows.ToAvalonia());
        context.DrawRRect(
            RRect.FromRectAndRadius(rect.Inflate(0.5), radius + 0.5),
            new SolidColorBrush(ThumbBorderColor),
            null);
        context.DrawRRect(
            RRect.FromRectAndRadius(rect, radius),
            new SolidColorBrush(Color),
            null);
    }
}
