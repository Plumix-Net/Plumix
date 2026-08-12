using Avalonia;
using Avalonia.Media;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.Painting;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/rich_text_demo_page.dart (exact sample parity)

public sealed class RichTextDemoPage : StatefulWidget
{
    public override State CreateState() => new RichTextDemoPageState();
}

public sealed class RichTextDemoPageState : State
{
    private readonly TapGestureRecognizer _tapRecognizer = new();
    private int _taps;

    public override void InitState()
    {
        base.InitState();
        _tapRecognizer.OnTap = () => SetState(() => _taps += 1);
    }

    public override void Dispose()
    {
        _tapRecognizer.Dispose();
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12,
            children:
            [
                new Text("RichText + TextSpan + WidgetSpan", fontSize: 20, color: Colors.Black),
                new Text(
                    "One paragraph, many styles. Spans share a single line layout, carry their own gesture "
                    + "recognizers, and can embed inline widgets.",
                    fontSize: 14,
                    color: Colors.DimGray),
                BuildStyledParagraph(),
                new Text("Tapped the link span " + _taps + " times", fontSize: 14, color: Colors.DimGray),
                BuildInlineWidgetParagraph(),
                BuildAlignmentRow(),
            ]);
    }

    private Widget BuildStyledParagraph()
    {
        return new Container(
            color: Color.Parse("#FFF1F5F9"),
            padding: new Thickness(12),
            child: new RichText(
                text: new TextSpan(
                    text: "Can you ",
                    style: new TextStyle(FontSize: 18, Color: Color.Parse("#FF1D3557")),
                    children:
                    [
                        new TextSpan(
                            text: "find the",
                            style: new TextStyle(
                                Color: Color.Parse("#FF2A9D8F"),
                                FontWeight: FontWeight.Bold,
                                Decoration: Plumix.UI.TextDecoration.Underline),
                            recognizer: _tapRecognizer),
                        new TextSpan(text: " secret?"),
                    ])));
    }

    private static Widget BuildInlineWidgetParagraph()
    {
        return new Container(
            color: Color.Parse("#FFE7EDF6"),
            padding: new Thickness(12),
            child: Text.Rich(
                new TextSpan(
                    text: "Inline ",
                    children:
                    [
                        new WidgetSpan(new Container(
                            width: 40,
                            height: 20,
                            color: Color.Parse("#FFE9C46A"))),
                        new TextSpan(text: " widgets flow with the text."),
                    ]),
                style: new TextStyle(FontSize: 16, Color: Color.Parse("#FF1D3557"))));
    }

    private static Widget BuildAlignmentRow()
    {
        return new Container(
            color: Color.Parse("#FFF8EDEB"),
            padding: new Thickness(12),
            child: Text.Rich(
                new TextSpan(
                    text: "top ",
                    children:
                    [
                        BuildBadge(PlaceholderAlignment.Top, "#FFE63946"),
                        new TextSpan(text: " middle "),
                        BuildBadge(PlaceholderAlignment.Middle, "#FF2A9D8F"),
                        new TextSpan(text: " bottom "),
                        BuildBadge(PlaceholderAlignment.Bottom, "#FF457B9D"),
                    ]),
                style: new TextStyle(FontSize: 24, Color: Color.Parse("#FF1D3557"))));
    }

    private static InlineSpan BuildBadge(PlaceholderAlignment alignment, string color)
    {
        return new WidgetSpan(
            new Container(width: 18, height: 18, color: Color.Parse(color)),
            alignment);
    }
}
