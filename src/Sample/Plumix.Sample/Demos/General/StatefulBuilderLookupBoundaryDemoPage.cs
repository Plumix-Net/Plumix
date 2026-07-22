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
                new Text("StatefulBuilder + LookupBoundary", fontSize: 20, color: Colors.Black),
                new Text(
                    "StatefulBuilder owns a local rebuild. LookupBoundary hides ancestors only from its bounded " +
                    "static lookup helpers.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Container(
                    color: Color.Parse("#FFF4F7FA"),
                    padding: new Thickness(12),
                    child: new StatefulBuilder((builderContext, setState) =>
                        new Column(
                            crossAxisAlignment: CrossAxisAlignment.Start,
                            spacing: 8,
                            children:
                            [
                                new Text($"local count: {count}", color: Color.Parse("#FF31506F")),
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
                            string regular = builderContext.DependOnInherited<DemoLookupScope>()?.Label ?? "missing";
                            return new Container(
                                color: Color.Parse("#FFE7EDF6"),
                                padding: new Thickness(12),
                                child: new Column(
                                    crossAxisAlignment: CrossAxisAlignment.Start,
                                    spacing: 6,
                                    children:
                                    [
                                        new Text($"bounded lookup: {bounded}", color: Colors.Black),
                                        new Text($"regular context lookup: {regular}", color: Colors.Black),
                                    ]));
                        }))),
            ]);
    }

    private sealed class DemoLookupScope : InheritedWidget
    {
        public DemoLookupScope(string label, Widget child, Key? key = null) : base(key)
        {
            Label = label;
            Child = child;
        }

        public string Label { get; }

        public Widget Child { get; }

        public override Widget Build(BuildContext context) => Child;

        protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
        {
            return Label != ((DemoLookupScope)oldWidget).Label;
        }
    }
}
