using Plumix.Foundation;
using Plumix.UI;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/display_feature_sub_screen.dart

/// <summary>
/// Positions <see cref="Child"/> in the largest sub-screen that does not overlap any display feature
/// (a hinge, fold, or cutout) and is closest to <see cref="AnchorPoint"/>.
/// </summary>
public sealed class DisplayFeatureSubScreen : StatelessWidget
{
    public DisplayFeatureSubScreen(
        Widget child,
        Offset? anchorPoint = null,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        AnchorPoint = anchorPoint;
    }

    public Widget Child { get; }

    public Offset? AnchorPoint { get; }

    public override Widget Build(BuildContext context)
    {
        var mediaQuery = MediaQuery.Of(context);
        Size parentSize = mediaQuery.Size;
        var wantedBounds = Offset.Zero & parentSize;
        Offset resolvedAnchorPoint = CapOffset(AnchorPoint ?? FallbackAnchorPoint(context), parentSize);
        List<Rect> subScreens = SubScreensInBounds(wantedBounds, AvoidBounds(mediaQuery));
        Rect closestSubScreen = ClosestToAnchorPoint(subScreens, resolvedAnchorPoint);

        return new Padding(
            EdgeInsets.FromLTRB(
                closestSubScreen.Left,
                closestSubScreen.Top,
                parentSize.Width - closestSubScreen.Right,
                parentSize.Height - closestSubScreen.Bottom),
            new MediaQuery(mediaQuery.RemoveDisplayFeatures(closestSubScreen), Child));
    }

    /// <summary>The bounds of the display features that content should avoid.</summary>
    public static List<Rect> AvoidBounds(MediaQueryData mediaQuery)
    {
        ArgumentNullException.ThrowIfNull(mediaQuery);
        var bounds = new List<Rect>();
        if (mediaQuery.DisplayFeatures is null)
        {
            return bounds;
        }

        foreach (DisplayFeature feature in mediaQuery.DisplayFeatures)
        {
            if (Math.Min(feature.Bounds.Width, feature.Bounds.Height) > 0.0
                || feature.State == DisplayFeatureState.PostureHalfOpened)
            {
                bounds.Add(feature.Bounds);
            }
        }

        return bounds;
    }

    private static Offset FallbackAnchorPoint(BuildContext context)
    {
        return Directionality.Of(context) == TextDirection.Rtl
            ? new Offset(double.MaxValue, 0.0)
            : default;
    }

    /// <summary>The sub-screens of <paramref name="wantedBounds"/> that avoid every supplied bound.</summary>
    public static List<Rect> SubScreensInBounds(Rect wantedBounds, IEnumerable<Rect> avoidBounds)
    {
        var subScreens = new List<Rect> { wantedBounds };
        foreach (Rect bounds in avoidBounds)
        {
            var newSubScreens = new List<Rect>();
            foreach (Rect screen in subScreens)
            {
                if (screen.Top >= bounds.Top && screen.Bottom <= bounds.Bottom)
                {
                    // The display feature splits this sub-screen vertically.
                    if (screen.Left < bounds.Left)
                    {
                        newSubScreens.Add(
                            Rect.FromLTWH(screen.Left, screen.Top, bounds.Left - screen.Left, screen.Height));
                    }

                    if (screen.Right > bounds.Right)
                    {
                        newSubScreens.Add(
                            Rect.FromLTWH(bounds.Right, screen.Top, screen.Right - bounds.Right, screen.Height));
                    }
                }
                else if (screen.Left >= bounds.Left && screen.Right <= bounds.Right)
                {
                    // The display feature splits this sub-screen horizontally.
                    if (screen.Top < bounds.Top)
                    {
                        newSubScreens.Add(
                            Rect.FromLTWH(screen.Left, screen.Top, screen.Width, bounds.Top - screen.Top));
                    }

                    if (screen.Bottom > bounds.Bottom)
                    {
                        newSubScreens.Add(
                            Rect.FromLTWH(screen.Left, bounds.Bottom, screen.Width, screen.Bottom - bounds.Bottom));
                    }
                }
                else
                {
                    newSubScreens.Add(screen);
                }
            }

            subScreens = newSubScreens;
        }

        return subScreens;
    }

    private static Rect ClosestToAnchorPoint(List<Rect> subScreens, Offset anchorPoint)
    {
        var closestScreen = default(Rect);
        double closestDistance = double.PositiveInfinity;
        foreach (Rect screen in subScreens)
        {
            double distance = DistanceFromPointToRect(anchorPoint, screen);
            if (distance < closestDistance)
            {
                closestScreen = screen;
                closestDistance = distance;
            }
        }

        return closestScreen;
    }

    private static double DistanceFromPointToRect(Offset point, Rect rect)
    {
        if (point.Dx < rect.Left)
        {
            if (point.Dy < rect.Top)
            {
                return Distance(point, rect.TopLeft);
            }

            return point.Dy > rect.Bottom ? Distance(point, rect.BottomLeft) : rect.Left - point.Dx;
        }

        if (point.Dx > rect.Right)
        {
            if (point.Dy < rect.Top)
            {
                return Distance(point, rect.TopRight);
            }

            return point.Dy > rect.Bottom ? Distance(point, rect.BottomRight) : point.Dx - rect.Right;
        }

        if (point.Dy < rect.Top)
        {
            return rect.Top - point.Dy;
        }

        return point.Dy > rect.Bottom ? point.Dy - rect.Bottom : 0.0;
    }

    private static double Distance(Offset from, Offset to)
    {
        double dx = from.Dx - to.Dx;
        double dy = from.Dy - to.Dy;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private static Offset CapOffset(Offset offset, Size maximum)
    {
        return new Offset(
            Math.Min(Math.Max(0.0, offset.Dx), maximum.Width),
            Math.Min(Math.Max(0.0, offset.Dy), maximum.Height));
    }
}
