using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/stateful_builder_lookup_boundary_demo_page.dart

public sealed class StatefulBuilderLookupBoundaryDemoPage : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        int count = 0;
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 16,
            children:
            [
                new Text(
                    "StatefulBuilder + LookupBoundary",
                    style: new TextStyle(FontSize: 20, Color: MaterialColors.Black)),
                new Text(
                    "StatefulBuilder owns a local rebuild. LookupBoundary hides ancestors only from its bounded " +
                    "static lookup helpers.",
                    style: new TextStyle(FontSize: 14, Color: MaterialColors.Black54)),
                new Container(
                    color: new Color(0xFFF4F7FA),
                    padding: new Thickness(12),
                    child: new StatefulBuilder((builderContext, setState) =>
                        new Column(
                            crossAxisAlignment: CrossAxisAlignment.Start,
                            spacing: 8,
                            children:
                            [
                                new Text($"local count: {count}", style: new TextStyle(Color: new Color(0xFF31506F))),
                                new TextButton(
                                    onPressed: () => setState(() => count += 1),
                                    child: new Text("Increment local state")),
                            ]))),
                new DemoLookupScope(
                    label: "outer scope",
                    child: new LookupBoundary(
                        child: new Builder(builderContext =>
                        {
                            string bounded = LookupBoundary
                                .GetInheritedWidgetOfExactType<DemoLookupScope>(builderContext)
                                ?.Label ?? "hidden";
                            string regular =
                                builderContext.DependOnInheritedWidgetOfExactType<DemoLookupScope>()?.Label
                                ?? "missing";
                            return new Container(
                                color: new Color(0xFFE7EDF6),
                                padding: new Thickness(12),
                                child: new Column(
                                    crossAxisAlignment: CrossAxisAlignment.Start,
                                    spacing: 6,
                                    children:
                                    [
                                        new Text($"bounded lookup: {bounded}"),
                                        new Text($"regular context lookup: {regular}"),
                                    ]));
                        }))),
            ]);
    }

    private sealed class DemoLookupScope : InheritedWidget
    {
        public DemoLookupScope(string label, Widget child, Key? key = null) : base(child, key)
        {
            Label = label;
        }

        public string Label { get; }

        public override bool UpdateShouldNotify(InheritedWidget oldWidget)
        {
            return Label != ((DemoLookupScope)oldWidget).Label;
        }
    }
}
