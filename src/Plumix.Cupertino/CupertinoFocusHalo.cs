using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/cupertino_focus_halo.dart

/// Applies an iOS-style focus border while any focus node in its child subtree has focus.
public sealed class CupertinoFocusHalo : StatefulWidget
{
    private readonly BorderRadiusGeometry _borderRadius;
    private readonly Func<BorderSide, BorderRadiusGeometry, ShapeBorder> _shapeBuilder;

    private CupertinoFocusHalo(
        Widget child,
        BorderRadiusGeometry borderRadius,
        Func<BorderSide, BorderRadiusGeometry, ShapeBorder> shapeBuilder,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        _borderRadius = borderRadius;
        _shapeBuilder = shapeBuilder;
    }

    public Widget Child { get; }

    public static CupertinoFocusHalo WithRect(Widget child, Key? key = null)
    {
        return new CupertinoFocusHalo(
            child,
            BorderRadius.Zero,
            static (side, borderRadius) => new RoundedRectangleBorder(side, borderRadius),
            key);
    }

    public static CupertinoFocusHalo WithRRect(
        Widget child,
        BorderRadiusGeometry borderRadius,
        Key? key = null)
    {
        return new CupertinoFocusHalo(
            child,
            borderRadius,
            static (side, radius) => new RoundedRectangleBorder(side, radius),
            key);
    }

    public static CupertinoFocusHalo WithRoundedSuperellipse(
        Widget child,
        BorderRadiusGeometry borderRadius,
        Key? key = null)
    {
        return new CupertinoFocusHalo(
            child,
            borderRadius,
            static (side, radius) => new RoundedSuperellipseBorder(side, radius),
            key);
    }

    public override State CreateState() => new CupertinoFocusHaloState();

    private sealed class CupertinoFocusHaloState : State
    {
        private const double FocusColorOpacity = 0.80;
        private const double FocusColorBrightness = 0.69;
        private const double FocusColorSaturation = 0.835;
        private const double FocusBorderWidth = 3.5;

        private bool _childHasFocus;

        private CupertinoFocusHalo Current => (CupertinoFocusHalo)StateWidget;

        private static Color EffectiveFocusOutlineColor
        {
            get
            {
                Color activeBlue = CupertinoColors.ActiveBlue.Value;
                Color translucentBlue = Color.FromArgb(
                    (byte)Math.Round(FocusColorOpacity * 255.0, MidpointRounding.AwayFromZero),
                    activeBlue.R,
                    activeBlue.G,
                    activeBlue.B);
                return HSLColor.FromColor(translucentBlue)
                    .WithLightness(FocusColorBrightness)
                    .WithSaturation(FocusColorSaturation)
                    .ToColor();
            }
        }

        public override Widget Build(BuildContext context)
        {
            BorderSide side = _childHasFocus
                ? new BorderSide(EffectiveFocusOutlineColor, FocusBorderWidth)
                : BorderSide.None;
            return new Focus(
                canRequestFocus: false,
                skipTraversal: true,
                includeSemantics: false,
                onFocusChange: hasFocus => SetState(() => _childHasFocus = hasFocus),
                child: new DecoratedBox(
                    position: DecorationPosition.Foreground,
                    decoration: new ShapeDecoration(Current._shapeBuilder(side, Current._borderRadius)),
                    child: Current.Child));
        }
    }
}
