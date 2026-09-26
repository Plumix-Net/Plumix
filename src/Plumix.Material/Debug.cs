using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/debug.dart (subset: debugCheckHasMaterial)

/// <summary>The debug checks of Flutter's Material <c>debug.dart</c>.</summary>
public static class MaterialDebug
{
    /// <summary>
    /// Asserts that the given context has a <see cref="Material"/> ancestor within the closest
    /// <see cref="LookupBoundary"/>. Dart's <c>debugCheckHasMaterial</c>: throws a
    /// <see cref="FlutterError"/> in debug builds when there is none, and always returns true.
    /// </summary>
    public static bool DebugCheckHasMaterial(BuildContext context)
    {
        if (!Constants.KDebugMode || LookupBoundary.FindAncestorWidgetOfExactType<Material>(context) is not null)
        {
            return true;
        }

        bool hiddenByBoundary = LookupBoundary.DebugIsHidingAncestorWidgetOfExactType<Material>(context);
        var information = new List<DiagnosticsNode>
        {
            new ErrorSummary(
                $"No Material widget found{(hiddenByBoundary ? " within the closest LookupBoundary" : string.Empty)}."),
        };
        if (hiddenByBoundary)
        {
            information.Add(new ErrorDescription(
                "There is an ancestor Material widget, but it is hidden by a LookupBoundary."));
        }

        information.Add(new ErrorDescription(
            $"{Diagnostics.DescribeType(context.Widget.GetType())} widgets require a Material "
            + "widget ancestor within the closest LookupBoundary.\n"
            + "In Material Design, most widgets are conceptually \"printed\" on "
            + "a sheet of material. In Flutter's material library, that "
            + "material is represented by the Material widget. It is the "
            + "Material widget that renders ink splashes, for instance. "
            + "Because of this, many material library widgets require that "
            + "there be a Material widget in the tree above them."));
        information.Add(new ErrorHint(
            "To introduce a Material widget, you can either directly "
            + "include one, or use a widget that contains Material itself, "
            + "such as a Card, Dialog, Drawer, or Scaffold."));
        information.AddRange(context.DescribeMissingAncestor(typeof(Material)));
        throw new FlutterError(information);
    }
}
