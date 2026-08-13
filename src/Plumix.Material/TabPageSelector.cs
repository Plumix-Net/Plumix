using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/tabs.dart

/// <summary>
/// Displays a single circle with the specified border and background colors. Used by
/// <see cref="TabPageSelector"/> to indicate the selected page.
/// </summary>
public sealed class TabPageSelectorIndicator : StatelessWidget
{
    public TabPageSelectorIndicator(
        Color backgroundColor,
        Color borderColor,
        double size,
        BorderStyle borderStyle = BorderStyle.Solid,
        Key? key = null) : base(key)
    {
        BackgroundColor = backgroundColor;
        BorderColor = borderColor;
        Size = size;
        BorderStyle = borderStyle;
    }

    public Color BackgroundColor { get; }

    public Color BorderColor { get; }

    public double Size { get; }

    public BorderStyle BorderStyle { get; }

    public override Widget Build(BuildContext context)
    {
        return new Container(
            width: Size,
            height: Size,
            margin: new Thickness(4),
            decoration: new BoxDecoration(
                Color: BackgroundColor,
                Border: Plumix.Rendering.Border.FromBorderSide(new BorderSide(BorderColor, style: BorderStyle)),
                Shape: BoxShape.Circle));
    }
}

/// <summary>
/// Displays a row of small circular indicators, one per tab.
/// </summary>
public sealed class TabPageSelector : StatefulWidget
{
    public TabPageSelector(
        TabController? controller = null,
        double indicatorSize = 12.0,
        Color? color = null,
        Color? selectedColor = null,
        BorderStyle? borderStyle = null,
        Key? key = null) : base(key)
    {
        if (!double.IsFinite(indicatorSize) || indicatorSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(indicatorSize));
        }

        Controller = controller;
        IndicatorSize = indicatorSize;
        Color = color;
        SelectedColor = selectedColor;
        BorderStyle = borderStyle;
    }

    public TabController? Controller { get; }

    public double IndicatorSize { get; }

    public Color? Color { get; }

    public Color? SelectedColor { get; }

    public BorderStyle? BorderStyle { get; }

    public override State CreateState() => new TabPageSelectorState();

    private sealed class TabPageSelectorState : State
    {
        private CurvedAnimation? _animation;
        private TabController? _previousTabController;

        private TabPageSelector Current => (TabPageSelector)StateWidget;

        private TabController TabController => Current.Controller
                                               ?? DefaultTabController.MaybeOf(Context)
                                               ?? throw new InvalidOperationException(
                                                   "No TabController for TabPageSelector.\nWhen creating "
                                                   + "a TabPageSelector, you must either provide an "
                                                   + "explicit TabController, or you must ensure that "
                                                   + "there is a DefaultTabController above it.");

        public override void DidChangeDependencies()
        {
            if (_animation is null
                || !ReferenceEquals(_previousTabController?.Animation, TabController.Animation))
            {
                SetAnimation();
            }

            _previousTabController = TabController;
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            if (!ReferenceEquals(_previousTabController?.Animation, TabController.Animation))
            {
                SetAnimation();
            }

            _previousTabController = TabController;
        }

        public override void Dispose()
        {
            _animation?.Dispose();
            _animation = null;
        }

        public override Widget Build(BuildContext context)
        {
            Color fixColor = Current.Color ?? Colors.Transparent;
            Color fixSelectedColor = Current.SelectedColor ?? Theme.Of(context).ColorScheme.Secondary;
            TabController controller = TabController;

            return new AnimatedBuilder(
                _animation!,
                (builderContext, _) =>
                {
                    var children = new List<Widget>(controller.Length);
                    for (int tabIndex = 0; tabIndex < controller.Length; tabIndex++)
                    {
                        children.Add(BuildTabIndicator(tabIndex, controller, fixColor, fixSelectedColor));
                    }

                    return new Semantics(
                        label: MaterialLocalizations.Of(builderContext).TabLabel(
                            controller.Index,
                            controller.Length),
                        child: new Row(mainAxisSize: MainAxisSize.Min, children: children));
                });
        }

        private void SetAnimation()
        {
            _animation?.Dispose();
            _animation = new CurvedAnimation(
                parent: TabController.Animation!,
                curve: Curves.FastOutSlowIn);
        }

        private Widget BuildTabIndicator(
            int tabIndex,
            TabController controller,
            Color unselectedColor,
            Color selectedColor)
        {
            Color background;
            if (controller.IndexIsChanging)
            {
                double t = 1.0 - TabIndexProgress.Of(controller);
                if (controller.Index == tabIndex)
                {
                    background = LerpColor(unselectedColor, selectedColor, t);
                }
                else if (controller.PreviousIndex == tabIndex)
                {
                    background = LerpColor(selectedColor, unselectedColor, t);
                }
                else
                {
                    background = unselectedColor;
                }
            }
            else
            {
                double offset = controller.Offset;
                if (controller.Index == tabIndex)
                {
                    background = LerpColor(unselectedColor, selectedColor, 1.0 - Math.Abs(offset));
                }
                else if (controller.Index == tabIndex - 1 && offset > 0.0)
                {
                    background = LerpColor(unselectedColor, selectedColor, offset);
                }
                else if (controller.Index == tabIndex + 1 && offset < 0.0)
                {
                    background = LerpColor(unselectedColor, selectedColor, -offset);
                }
                else
                {
                    background = unselectedColor;
                }
            }

            return new TabPageSelectorIndicator(
                backgroundColor: background,
                borderColor: selectedColor,
                size: Current.IndicatorSize,
                borderStyle: Current.BorderStyle ?? global::Plumix.Rendering.BorderStyle.Solid);
        }

        private static Color LerpColor(Color from, Color to, double t) =>
            new ColorTween().Evaluate(Math.Clamp(t, 0, 1), from, to);
    }
}
