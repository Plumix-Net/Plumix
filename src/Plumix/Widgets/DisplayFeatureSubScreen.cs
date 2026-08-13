using Avalonia;
using Plumix.Foundation;
using Plumix.UI;

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
        Point? anchorPoint = null,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        AnchorPoint = anchorPoint;
    }

    public Widget Child { get; }

    public Point? AnchorPoint { get; }

    public override Widget Build(BuildContext context)
    {
        var mediaQuery = MediaQuery.Of(context);
        Size parentSize = mediaQuery.Size;
        var wantedBounds = new Rect(default, parentSize);
        Point resolvedAnchorPoint = CapOffset(AnchorPoint ?? FallbackAnchorPoint(context), parentSize);
        List<Rect> subScreens = SubScreensInBounds(wantedBounds, AvoidBounds(mediaQuery));
        Rect closestSubScreen = ClosestToAnchorPoint(subScreens, resolvedAnchorPoint);

        return new Padding(
            new Thickness(
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

    private static Point FallbackAnchorPoint(BuildContext context)
    {
        return Directionality.Of(context) == TextDirection.Rtl
            ? new Point(double.MaxValue, 0.0)
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
                        newSubScreens.Add(new Rect(screen.Left, screen.Top, bounds.Left - screen.Left, screen.Height));
                    }

                    if (screen.Right > bounds.Right)
                    {
                        newSubScreens.Add(
                            new Rect(bounds.Right, screen.Top, screen.Right - bounds.Right, screen.Height));
                    }
                }
                else if (screen.Left >= bounds.Left && screen.Right <= bounds.Right)
                {
                    // The display feature splits this sub-screen horizontally.
                    if (screen.Top < bounds.Top)
                    {
                        newSubScreens.Add(new Rect(screen.Left, screen.Top, screen.Width, bounds.Top - screen.Top));
                    }

                    if (screen.Bottom > bounds.Bottom)
                    {
                        newSubScreens.Add(
                            new Rect(screen.Left, bounds.Bottom, screen.Width, screen.Bottom - bounds.Bottom));
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

    private static Rect ClosestToAnchorPoint(List<Rect> subScreens, Point anchorPoint)
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

    private static double DistanceFromPointToRect(Point point, Rect rect)
    {
        if (point.X < rect.Left)
        {
            if (point.Y < rect.Top)
            {
                return Distance(point, rect.TopLeft);
            }

            return point.Y > rect.Bottom ? Distance(point, rect.BottomLeft) : rect.Left - point.X;
        }

        if (point.X > rect.Right)
        {
            if (point.Y < rect.Top)
            {
                return Distance(point, rect.TopRight);
            }

            return point.Y > rect.Bottom ? Distance(point, rect.BottomRight) : point.X - rect.Right;
        }

        if (point.Y < rect.Top)
        {
            return rect.Top - point.Y;
        }

        return point.Y > rect.Bottom ? point.Y - rect.Bottom : 0.0;
    }

    private static double Distance(Point from, Point to)
    {
        double dx = from.X - to.X;
        double dy = from.Y - to.Y;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private static Point CapOffset(Point offset, Size maximum)
    {
        return new Point(
            Math.Min(Math.Max(0.0, offset.X), maximum.Width),
            Math.Min(Math.Max(0.0, offset.Y), maximum.Height));
    }
}
