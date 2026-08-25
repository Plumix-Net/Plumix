using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/desktop_text_selection_toolbar.dart

/// <summary>A macOS-style desktop text-selection toolbar.</summary>
public sealed class CupertinoDesktopTextSelectionToolbar : StatelessWidget
{
    // The minimum padding from all edges of the selection toolbar to all edges of the screen.
    public const double ToolbarScreenPadding = 8.0;

    // These values were measured from a screenshot of the native context menu on macOS 13.2.
    public const double ToolbarWidth = 222.0;
    private const double ToolbarSaturationBoost = 3.0;
    private const double ToolbarBlurSigma = 20.0;
    private const double ToolbarBorderRadius = 8.0;

    private static readonly Thickness ToolbarPadding = new(6.0);

    private static readonly Plumix.Rendering.BoxShadow[] ToolbarShadow =
    [
        new(
            color: Color.FromArgb(60, 0, 0, 0),
            offset: new Point(0.0, 4.0),
            blurRadius: 10.0,
            spreadRadius: 0.5),
    ];

    private static readonly CupertinoDynamicColor ToolbarBorderColor =
        CupertinoDynamicColor.WithBrightness(
            Color.FromUInt32(0xFFB8B8B8),
            Color.FromUInt32(0xFF5B5B5B));

    private static readonly CupertinoDynamicColor ToolbarBackgroundColor =
        CupertinoDynamicColor.WithBrightness(
            Color.FromUInt32(0xB2FFFFFF),
            Color.FromUInt32(0xB2303030));

    public CupertinoDesktopTextSelectionToolbar(
        Point anchor,
        IReadOnlyList<Widget> children,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(children);
        if (children.Count == 0)
        {
            throw new ArgumentException("Toolbar children must not be empty.", nameof(children));
        }

        Anchor = anchor;
        Children = children;
    }

    public Point Anchor { get; }

    public IReadOnlyList<Widget> Children { get; }

    public override Widget Build(BuildContext context)
    {
        double paddingAbove = MediaQuery.PaddingOf(context).Top + ToolbarScreenPadding;
        var localAdjustment = new Vector(ToolbarScreenPadding, paddingAbove);

        return new Padding(
            new Thickness(
                ToolbarScreenPadding,
                paddingAbove,
                ToolbarScreenPadding,
                ToolbarScreenPadding),
            new CustomSingleChildLayout(
                new DesktopTextSelectionToolbarLayoutDelegate(Anchor - localAdjustment),
                DefaultToolbarBuilder(
                    context,
                    new Column(mainAxisSize: MainAxisSize.Min, children: Children))));
    }

    // Builds a toolbar just like the default Mac toolbar, with the right color background, padding
    // and rounded corners.
    private static Widget DefaultToolbarBuilder(BuildContext context, Widget child)
    {
        return new Container(
            width: ToolbarWidth,
            clipBehavior: Clip.HardEdge,
            decoration: new ShapeDecoration(
                Shadows: ToolbarShadow,
                Shape: new RoundedSuperellipseBorder(
                    borderRadius: BorderRadius.Circular(ToolbarBorderRadius))),
            child: new BackdropFilter(
                filter: new ImageFilter.Compose(
                    outer: new ImageFilter.ColorMatrix(SaturationMatrix(ToolbarSaturationBoost)),
                    inner: new ImageFilter.Blur(ToolbarBlurSigma, ToolbarBlurSigma)),
                child: new DecoratedBox(
                    decoration: new ShapeDecoration(
                        Color: ToolbarBackgroundColor.ResolveFrom(context),
                        Shape: new RoundedSuperellipseBorder(
                            side: new BorderSide(ToolbarBorderColor.ResolveFrom(context)),
                            borderRadius: BorderRadius.Circular(ToolbarBorderRadius))),
                    child: new Padding(ToolbarPadding, child))));
    }

    internal static IReadOnlyList<double> SaturationMatrix(double saturation)
    {
        double r = 0.213 * (1.0 - saturation);
        double g = 0.715 * (1.0 - saturation);
        double b = 0.072 * (1.0 - saturation);
        return
        [
            r + saturation, g, b, 0.0, 0.0,
            r, g + saturation, b, 0.0, 0.0,
            r, g, b + saturation, 0.0, 0.0,
            0.0, 0.0, 0.0, 1.0, 0.0,
        ];
    }
}
