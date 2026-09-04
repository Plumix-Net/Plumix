using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/two_dimensional_scroll_view.dart

namespace Plumix.Widgets;

/// <summary>
/// A widget that combines a <see cref="TwoDimensionalScrollable"/> and a
/// <see cref="TwoDimensionalViewport"/> to create an interactive scrolling pane of content in both
/// the vertical and horizontal dimensions.
/// </summary>
/// <remarks>Flutter's <c>TwoDimensionalScrollView</c>.</remarks>
public abstract class TwoDimensionalScrollView : StatelessWidget
{
    protected TwoDimensionalScrollView(
        TwoDimensionalChildDelegate @delegate,
        bool? primary = null,
        Axis mainAxis = Axis.Vertical,
        ScrollableDetails? verticalDetails = null,
        ScrollableDetails? horizontalDetails = null,
        ScrollCacheExtent? scrollCacheExtent = null,
        DiagonalDragBehavior diagonalDragBehavior = DiagonalDragBehavior.None,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior = null,
        Clip clipBehavior = Clip.HardEdge,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(@delegate);
        Delegate = @delegate;
        ScrollCacheExtent = scrollCacheExtent;
        DiagonalDragBehavior = diagonalDragBehavior;
        Primary = primary;
        MainAxis = mainAxis;
        VerticalDetails = verticalDetails ?? ScrollableDetails.Vertical();
        HorizontalDetails = horizontalDetails ?? ScrollableDetails.Horizontal();
        DragStartBehavior = dragStartBehavior;
        KeyboardDismissBehavior = keyboardDismissBehavior;
        HitTestBehavior = hitTestBehavior;
        ClipBehavior = clipBehavior;
    }

    /// <summary>Provides the children for this scroll view.</summary>
    public TwoDimensionalChildDelegate Delegate { get; }

    /// <summary>How much content beyond the visible area is laid out.</summary>
    public ScrollCacheExtent? ScrollCacheExtent { get; }

    /// <summary>How scrolling gestures lock to one axis, or move freely in both.</summary>
    public DiagonalDragBehavior DiagonalDragBehavior { get; }

    /// <summary>
    /// Whether the <see cref="MainAxis"/> is the primary scroll view associated with the parent
    /// <see cref="PrimaryScrollController"/>.
    /// </summary>
    public bool? Primary { get; }

    /// <summary>
    /// The major of the two axes: it decides how <see cref="Primary"/> applies, and should be given
    /// to the <see cref="TwoDimensionalViewport"/> as its paint order.
    /// </summary>
    public Axis MainAxis { get; }

    /// <summary>The configuration of the vertical axis.</summary>
    public ScrollableDetails VerticalDetails { get; }

    /// <summary>The configuration of the horizontal axis.</summary>
    public ScrollableDetails HorizontalDetails { get; }

    /// <summary>When drag gestures start; applies to both axes.</summary>
    public DragStartBehavior DragStartBehavior { get; }

    /// <summary>
    /// How the scroll view dismisses the on-screen keyboard, or null to take the ambient
    /// <see cref="ScrollBehavior"/>'s policy.
    /// </summary>
    public ScrollViewKeyboardDismissBehavior? KeyboardDismissBehavior { get; }

    /// <summary>How both scrollables behave during hit testing.</summary>
    public HitTestBehavior HitTestBehavior { get; }

    /// <summary>How the viewport clips content that overflows it.</summary>
    public Clip ClipBehavior { get; }

    /// <summary>
    /// Builds the two-dimensional viewport, most likely a subclass of
    /// <see cref="TwoDimensionalViewport"/>, from the offsets the scrollable supplies.
    /// </summary>
    public abstract Widget BuildViewport(
        BuildContext context,
        ViewportOffset verticalOffset,
        ViewportOffset horizontalOffset);

    public override Widget Build(BuildContext context)
    {
        if (ScrollDirectionUtils.AxisDirectionToAxis(VerticalDetails.Direction) != Axis.Vertical)
        {
            throw new AssertionError("TwoDimensionalScrollView.verticalDetails are not Axis.vertical.");
        }

        if (ScrollDirectionUtils.AxisDirectionToAxis(HorizontalDetails.Direction) != Axis.Horizontal)
        {
            throw new AssertionError("TwoDimensionalScrollView.horizontalDetails are not Axis.horizontal.");
        }

        ScrollableDetails mainAxisDetails = MainAxis == Axis.Vertical ? VerticalDetails : HorizontalDetails;
        bool effectivePrimary = Primary
                                ?? (mainAxisDetails.Controller is null
                                    && PrimaryScrollController.ShouldInherit(context, MainAxis));

        if (effectivePrimary)
        {
            // Using PrimaryScrollController for the main axis.
            if (mainAxisDetails.Controller != null)
            {
                throw new AssertionError(
                    "TwoDimensionalScrollView.primary was explicitly set to true, but a "
                    + "ScrollController was provided in the ScrollableDetails of the "
                    + "TwoDimensionalScrollView.mainAxis.");
            }

            mainAxisDetails = mainAxisDetails.CopyWith(controller: PrimaryScrollController.Of(context));
        }

        var scrollable = new TwoDimensionalScrollable(
            horizontalDetails: MainAxis == Axis.Horizontal ? mainAxisDetails : HorizontalDetails,
            verticalDetails: MainAxis == Axis.Vertical ? mainAxisDetails : VerticalDetails,
            diagonalDragBehavior: DiagonalDragBehavior,
            viewportBuilder: BuildViewport,
            dragStartBehavior: DragStartBehavior,
            hitTestBehavior: HitTestBehavior);

        // A further descendant scroll view must not inherit the same PrimaryScrollController.
        Widget scrollableResult = effectivePrimary
            ? PrimaryScrollController.None(scrollable)
            : scrollable;

        ScrollViewKeyboardDismissBehavior effectiveKeyboardDismissBehavior =
            KeyboardDismissBehavior
            ?? ScrollConfiguration.Of(context).GetKeyboardDismissBehavior(context);

        if (effectiveKeyboardDismissBehavior == ScrollViewKeyboardDismissBehavior.OnDrag)
        {
            return new NotificationListener<ScrollUpdateNotification>(
                child: scrollableResult,
                onNotification: notification =>
                {
                    FocusScopeNode currentScope = FocusScope.Of(context);
                    if (notification.DragDetails != null
                        && !currentScope.HasPrimaryFocus
                        && currentScope.HasFocus)
                    {
                        FocusManager.Instance.PrimaryFocus?.Unfocus();
                    }

                    return false;
                });
        }

        return scrollableResult;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new EnumProperty<Axis>("mainAxis", MainAxis));
        properties.Add(new EnumProperty<DiagonalDragBehavior>("diagonalDragBehavior", DiagonalDragBehavior));
        properties.Add(new FlagProperty(
            "primary",
            value: Primary,
            ifTrue: "using primary controller",
            showName: true));
        properties.Add(new DiagnosticsProperty<ScrollableDetails>(
            "verticalDetails",
            VerticalDetails,
            showName: false));
        properties.Add(new DiagnosticsProperty<ScrollableDetails>(
            "horizontalDetails",
            HorizontalDetails,
            showName: false));
    }
}
