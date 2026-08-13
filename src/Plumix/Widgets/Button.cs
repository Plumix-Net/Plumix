using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;

// Dart parity sources (reference, adapted):
// flutter/packages/flutter/lib/src/widgets/basic.dart
// material_ui/lib/src/text_button.dart

namespace Plumix.Widgets;

public sealed class Button : LeafRenderObjectWidget
{
    public Button(
        string label,
        Action? onPressed,
        Color? background = null,
        Color? foreground = null,
        double? fontSize = null,
        Thickness? padding = null,
        Key? key = null) : base(key)
    {
        Label = label;
        OnPressed = onPressed;
        Background = background ?? Colors.LightGray;
        Foreground = foreground ?? Colors.Black;
        FontSize = fontSize ?? 16;
        Padding = padding;
    }

    public string Label { get; }

    public Action? OnPressed { get; }

    public Color Background { get; }

    public Color Foreground { get; }

    public double FontSize { get; }

    public Thickness? Padding { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderButton(Label, OnPressed, Background, Foreground, FontSize, Padding);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var button = (RenderButton)renderObject;
        button.Label = Label;
        button.OnPressed = OnPressed;
        button.Background = Background;
        button.Foreground = Foreground;
        button.FontSize = FontSize;
    }
}
