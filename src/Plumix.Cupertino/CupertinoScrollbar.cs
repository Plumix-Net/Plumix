using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: flutter/packages/flutter/lib/src/cupertino/scrollbar.dart

public sealed class CupertinoScrollbar : StatelessWidget
{
    public const double DefaultThickness = 3;
    public const double DefaultThicknessWhileDragging = 8;
    public const double DefaultRadius = 1.5;
    public const double DefaultRadiusWhileDragging = 4;

    public CupertinoScrollbar(
        Widget child,
        ScrollController? controller = null,
        bool? thumbVisibility = null,
        double thickness = DefaultThickness,
        double thicknessWhileDragging = DefaultThicknessWhileDragging,
        double radius = DefaultRadius,
        double radiusWhileDragging = DefaultRadiusWhileDragging,
        ScrollNotificationPredicate? notificationPredicate = null,
        ScrollbarOrientation? scrollbarOrientation = null,
        double mainAxisMargin = 3,
        Key? key = null) : base(key)
    {
        ValidatePositive(nameof(thickness), thickness);
        ValidatePositive(nameof(thicknessWhileDragging), thicknessWhileDragging);
        ValidateNonNegative(nameof(radius), radius);
        ValidateNonNegative(nameof(radiusWhileDragging), radiusWhileDragging);
        ValidateNonNegative(nameof(mainAxisMargin), mainAxisMargin);
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Controller = controller;
        ThumbVisibility = thumbVisibility;
        Thickness = thickness;
        ThicknessWhileDragging = thicknessWhileDragging;
        Radius = radius;
        RadiusWhileDragging = radiusWhileDragging;
        NotificationPredicate = notificationPredicate;
        ScrollbarOrientation = scrollbarOrientation;
        MainAxisMargin = mainAxisMargin;
    }

    public Widget Child { get; }
    public ScrollController? Controller { get; }
    public bool? ThumbVisibility { get; }
    public double Thickness { get; }
    public double ThicknessWhileDragging { get; }
    public double Radius { get; }
    public double RadiusWhileDragging { get; }
    public ScrollNotificationPredicate? NotificationPredicate { get; }
    public ScrollbarOrientation? ScrollbarOrientation { get; }
    public double MainAxisMargin { get; }

    public override Widget Build(BuildContext context) => new RawScrollbar(
        child: Child,
        controller: Controller,
        thumbVisibility: ThumbVisibility ?? false,
        shape: null,
        radius: null,
        thickness: null,
        thumbColor: null,
        minThumbLength: 36,
        minOverscrollLength: 8,
        trackVisibility: false,
        trackRadius: null,
        trackColor: null,
        trackBorderColor: null,
        fadeDuration: TimeSpan.FromMilliseconds(250),
        timeToFade: TimeSpan.FromMilliseconds(1200),
        pressDuration: TimeSpan.FromMilliseconds(100),
        notificationPredicate: NotificationPredicate,
        interactive: true,
        scrollbarOrientation: ScrollbarOrientation,
        mainAxisMargin: MainAxisMargin,
        crossAxisMargin: 3,
        padding: null,
        thumbColorResolver: _ => Color.FromArgb(0x59, 0, 0, 0),
        trackColorResolver: null,
        trackBorderColorResolver: null,
        thicknessResolver: states => states.HasFlag(ScrollbarInteractionState.Dragged)
            ? ThicknessWhileDragging
            : Thickness,
        radiusResolver: states => states.HasFlag(ScrollbarInteractionState.Dragged)
            ? RadiusWhileDragging
            : Radius,
        thumbVisibilityResolver: null,
        trackVisibilityResolver: null,
        trackTapEnabled: false,
        interactionChanged: null);

    private static void ValidatePositive(string name, double value)
    {
        if (!double.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(name);
    }

    private static void ValidateNonNegative(string name, double value)
    {
        if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(name);
    }
}
