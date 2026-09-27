using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/debug.dart

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

    /// <summary>Asserts that a Localizations ancestor provides MaterialLocalizations.</summary>
    public static bool DebugCheckHasMaterialLocalizations(BuildContext context)
    {
        if (!Constants.KDebugMode || Localizations.MaybeOf<MaterialLocalizations>(context) is not null)
        {
            return true;
        }

        var information = new List<DiagnosticsNode>
        {
            new ErrorSummary("No MaterialLocalizations found."),
            new ErrorDescription(
                $"{Diagnostics.DescribeType(context.Widget.GetType())} widgets require MaterialLocalizations "
                + "to be provided by a Localizations widget ancestor."),
            new ErrorDescription(
                "The material library uses Localizations to generate messages, "
                + "labels, and abbreviations."),
            new ErrorHint(
                "To introduce a MaterialLocalizations, either use a "
                + "MaterialApp at the root of your application to include them "
                + "automatically, or add a Localization widget with a "
                + "MaterialLocalizations delegate."),
        };
        information.AddRange(context.DescribeMissingAncestor(typeof(MaterialLocalizations)));
        throw new FlutterError(information);
    }

    /// <summary>Asserts that the current widget or an ancestor is a Scaffold.</summary>
    public static bool DebugCheckHasScaffold(BuildContext context)
    {
        if (!Constants.KDebugMode
            || context.Widget is Scaffold
            || context.FindAncestorWidgetOfExactType<Scaffold>() is not null)
        {
            return true;
        }

        var information = new List<DiagnosticsNode>
        {
            new ErrorSummary("No Scaffold widget found."),
            new ErrorDescription(
                $"{Diagnostics.DescribeType(context.Widget.GetType())} widgets require a Scaffold widget ancestor."),
        };
        information.AddRange(context.DescribeMissingAncestor(typeof(Scaffold)));
        information.Add(new ErrorHint(
            "Typically, the Scaffold widget is introduced by the MaterialApp or "
            + "WidgetsApp widget at the top of your application widget tree."));
        throw new FlutterError(information);
    }

    /// <summary>Asserts that a ScaffoldMessenger is an ancestor of the current widget.</summary>
    public static bool DebugCheckHasScaffoldMessenger(BuildContext context)
    {
        if (!Constants.KDebugMode || context.FindAncestorWidgetOfExactType<ScaffoldMessenger>() is not null)
        {
            return true;
        }

        var information = new List<DiagnosticsNode>
        {
            new ErrorSummary("No ScaffoldMessenger widget found."),
            new ErrorDescription(
                $"{Diagnostics.DescribeType(context.Widget.GetType())} widgets require a "
                + "ScaffoldMessenger widget ancestor."),
        };
        information.AddRange(context.DescribeMissingAncestor(typeof(ScaffoldMessenger)));
        information.Add(new ErrorHint(
            "Typically, the ScaffoldMessenger widget is introduced by the MaterialApp "
            + "at the top of your application widget tree."));
        throw new FlutterError(information);
    }
}
