using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/tabs.dart

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

public sealed class TabPageSelector : StatefulWidget
{
    public TabPageSelector(
        TabController? controller = null,
        double indicatorSize = 12,
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
        private TabController? _controller;

        private TabPageSelector Current => (TabPageSelector)StateWidget;

        public override void DidChangeDependencies()
        {
            UpdateController();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var old = (TabPageSelector)oldWidget;
            if (!ReferenceEquals(old.Controller, Current.Controller))
            {
                UpdateController();
            }
        }

        public override void Dispose()
        {
            DetachController();
        }

        public override Widget Build(BuildContext context)
        {
            TabController controller = _controller ?? ResolveController();
            Color unselectedColor = Current.Color ?? Colors.Transparent;
            Color selectedColor = Current.SelectedColor ?? Theme.Of(context).SecondaryColor;
            var children = new List<Widget>(controller.Length);
            for (int tabIndex = 0; tabIndex < controller.Length; tabIndex++)
            {
                children.Add(BuildTabIndicator(
                    tabIndex,
                    controller,
                    unselectedColor,
                    selectedColor));
            }

            return new Semantics(
                label: MaterialLocalizations.Of(context).TabLabel(
                    controller.Index,
                    controller.Length),
                child: new Row(
                    mainAxisSize: MainAxisSize.Min,
                    children: children));
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
                double t = 1 - IndexChangeProgress(controller);
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
                    background = LerpColor(unselectedColor, selectedColor, 1 - Math.Abs(offset));
                }
                else if (controller.Index == tabIndex - 1 && offset > 0)
                {
                    background = LerpColor(unselectedColor, selectedColor, offset);
                }
                else if (controller.Index == tabIndex + 1 && offset < 0)
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

        private void UpdateController()
        {
            TabController nextController = ResolveController();
            if (ReferenceEquals(nextController, _controller))
            {
                return;
            }

            DetachController();
            _controller = nextController;
            _controller.AddListener(HandleControllerChanged);
        }

        private TabController ResolveController()
        {
            return Current.Controller
                   ?? DefaultTabController.MaybeOf(Context)
                   ?? throw new InvalidOperationException(
                       "No TabController was provided and no DefaultTabController ancestor was found.");
        }

        private void DetachController()
        {
            _controller?.RemoveListener(HandleControllerChanged);
            _controller = null;
        }

        private void HandleControllerChanged()
        {
            if (Mounted)
            {
                SetState(() => { });
            }
        }

        private static double IndexChangeProgress(TabController controller)
        {
            double controllerValue = controller.AnimationValue;
            double previousIndex = controller.PreviousIndex;
            double currentIndex = controller.Index;
            if (!controller.IndexIsChanging)
            {
                return Math.Clamp(Math.Abs(currentIndex - controllerValue), 0, 1);
            }

            double distance = Math.Abs(currentIndex - previousIndex);
            return distance <= 0
                ? 1
                : Math.Clamp(Math.Abs(controllerValue - currentIndex) / distance, 0, 1);
        }

        private static Color LerpColor(Color from, Color to, double t)
        {
            return new ColorTween().Evaluate(Math.Clamp(t, 0, 1), from, to);
        }
    }
}
