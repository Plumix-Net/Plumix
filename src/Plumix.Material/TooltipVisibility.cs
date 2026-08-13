using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/tooltip_visibility.dart

public sealed class TooltipVisibility : StatelessWidget
{
    public TooltipVisibility(bool visible, Widget child, Key? key = null) : base(key)
    {
        Visible = visible;
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public bool Visible { get; }

    public Widget Child { get; }

    public static bool Of(BuildContext context)
    {
        return context.DependOnInherited<TooltipVisibilityScope>()?.Visible ?? true;
    }

    public override Widget Build(BuildContext context) => new TooltipVisibilityScope(Visible, Child);

    private sealed class TooltipVisibilityScope : InheritedWidget
    {
        public TooltipVisibilityScope(bool visible, Widget child) : base(key: null)
        {
            Visible = visible;
            Child = child;
        }

        public bool Visible { get; }

        public Widget Child { get; }

        public override Widget Build(BuildContext context) => Child;

        protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
            ((TooltipVisibilityScope)oldWidget).Visible != Visible;
    }
}
