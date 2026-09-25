using Avalonia.Media;
using Plumix.Foundation;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/title.dart

public sealed class Title : StatefulWidget
{
    public Title(
        Color color,
        Widget child,
        string title = "",
        Key? key = null) : base(key)
    {
        if (color.Alpha != byte.MaxValue)
        {
            throw new ArgumentException("Title color must be opaque.", nameof(color));
        }

        Color = color;
        Child = child ?? throw new ArgumentNullException(nameof(child));
        TitleText = title ?? throw new ArgumentNullException(nameof(title));
    }

    public string TitleText { get; }

    public Color Color { get; }

    public Widget Child { get; }

    public override State CreateState()
    {
        return new TitleState();
    }

    private sealed class TitleState : State<Title>
    {
        private Title CurrentWidget => (Title)StateWidget;

        public override void InitState()
        {
            base.InitState();
            UpdateChrome();
        }

        public override void DidUpdateWidget(Title oldWidget)
        {
            base.DidUpdateWidget(oldWidget);
            var oldTitle = (Title)oldWidget;
            if (!string.Equals(oldTitle.TitleText, CurrentWidget.TitleText, StringComparison.Ordinal)
                || oldTitle.Color != CurrentWidget.Color)
            {
                UpdateChrome();
            }
        }

        public override Widget Build(BuildContext context)
        {
            return CurrentWidget.Child;
        }

        private void UpdateChrome()
        {
            SystemChrome.SetApplicationSwitcherDescription(
                new ApplicationSwitcherDescription(
                    CurrentWidget.TitleText,
                    ToArgb(CurrentWidget.Color)));
        }

        private static uint ToArgb(Color color)
        {
            return color.ToARGB32();
        }
    }
}
