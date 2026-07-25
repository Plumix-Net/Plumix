using Plumix.Foundation;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/inherited_theme.dart

public abstract class InheritedTheme : InheritedWidget
{
    protected InheritedTheme(Key? key = null) : base(key)
    {
    }

    public abstract Widget Wrap(BuildContext context, Widget child);

    public static Widget CaptureAll(
        BuildContext context,
        Widget child,
        BuildContext? to = null)
    {
        ArgumentNullException.ThrowIfNull(child);
        return Capture(context, to).Wrap(child);
    }

    public static CapturedThemes Capture(BuildContext from, BuildContext? to = null)
    {
        if (to.HasValue && from.Equals(to.Value))
        {
            return new CapturedThemes([]);
        }

        var themes = new List<InheritedTheme>();
        var themeTypes = new HashSet<Type>();
        bool didFindAncestor = !to.HasValue;

        from.VisitAncestorElements(ancestor =>
        {
            if (to.HasValue && ReferenceEquals(ancestor, to.Value.Owner))
            {
                didFindAncestor = true;
                return false;
            }

            if (ancestor is InheritedElement
                && ancestor.Widget is InheritedTheme theme
                && themeTypes.Add(theme.GetType()))
            {
                themes.Add(theme);
            }

            return true;
        });

        if (!didFindAncestor)
        {
            throw new ArgumentException(
                "The provided 'to' context must be an ancestor of the 'from' context.",
                nameof(to));
        }

        return new CapturedThemes(themes);
    }
}

public sealed class CapturedThemes
{
    private readonly IReadOnlyList<InheritedTheme> _themes;

    internal CapturedThemes(IReadOnlyList<InheritedTheme> themes)
    {
        _themes = themes;
    }

    public Widget Wrap(Widget child)
    {
        ArgumentNullException.ThrowIfNull(child);
        return new CaptureAll(_themes, child);
    }

    private sealed class CaptureAll : StatelessWidget
    {
        private readonly IReadOnlyList<InheritedTheme> _themes;
        private readonly Widget _child;

        public CaptureAll(IReadOnlyList<InheritedTheme> themes, Widget child)
        {
            _themes = themes;
            _child = child;
        }

        public override Widget Build(BuildContext context)
        {
            Widget wrappedChild = _child;
            foreach (InheritedTheme theme in _themes)
            {
                wrappedChild = theme.Wrap(context, wrappedChild);
            }

            return wrappedChild;
        }
    }
}
