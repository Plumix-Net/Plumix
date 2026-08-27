using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/app.dart (MaterialScrollBehavior)

public class MaterialScrollBehavior : ScrollBehavior
{
    public override TargetPlatform GetPlatform(BuildContext context)
    {
        return Theme.Of(context).Platform;
    }

    public override Widget BuildScrollbar(
        BuildContext context,
        Widget child,
        ScrollableDetails details)
    {
        if (ScrollDirectionUtils.AxisDirectionToAxis(details.Direction) == Axis.Horizontal)
        {
            return child;
        }

        return GetPlatform(context) switch
        {
            TargetPlatform.Linux or TargetPlatform.MacOS or TargetPlatform.Windows =>
                new Scrollbar(
                    child: child,
                    controller: details.Controller
                                ?? throw new InvalidOperationException(
                                    "A desktop Material scrollbar requires a ScrollController.")),
            _ => child,
        };
    }

    public override Widget BuildOverscrollIndicator(
        BuildContext context,
        Widget child,
        ScrollableDetails details)
    {
        ThemeData theme = Theme.Of(context);
        return GetPlatform(context) switch
        {
            TargetPlatform.IOS
                or TargetPlatform.Linux
                or TargetPlatform.MacOS
                or TargetPlatform.Windows => child,
            TargetPlatform.Android when theme.UseMaterial3 =>
                new StretchingOverscrollIndicator(
                    axisDirection: details.Direction,
                    clipBehavior: details.DecorationClipBehavior ?? Clip.HardEdge,
                    child: child),
            TargetPlatform.Android or TargetPlatform.Fuchsia =>
                new GlowingOverscrollIndicator(
                    axisDirection: details.Direction,
                    color: theme.ColorScheme.Secondary,
                    child: child),
            _ => child,
        };
    }
}
