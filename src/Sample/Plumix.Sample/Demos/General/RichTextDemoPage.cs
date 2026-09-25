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
                BuildPaintedParagraph(),
                BuildInlineWidgetParagraph(),
                BuildAlignmentRow(),
                BuildDefaultTextStyleParagraph(),
                BuildOverflowAndStrut(),
            ]);
    }

    private Widget BuildStyledParagraph()
    {
        return new Container(
            color: new Color(0xFFF1F5F9),
            padding: new Thickness(12),
            child: new RichText(
                text: new TextSpan(
                    text: "Can you ",
                    style: new TextStyle(FontSize: 18, Color: new Color(0xFF1D3557)),
                    children:
                    [
                        new TextSpan(
                            text: "find the",
                            style: new TextStyle(
                                Color: new Color(0xFF2A9D8F),
                                FontWeight: FontWeight.Bold,
                                Decoration: Plumix.UI.TextDecoration.Underline),
                            recognizer: _tapRecognizer),
                        new TextSpan(text: " secret?"),
                    ])));
    }

    private static Widget BuildPaintedParagraph()
    {
        return new Container(
            color: new Color(0xFFFDF6E3),
            padding: new Thickness(12),
            child: Text.Rich(
                new TextSpan(
                    text: "A ",
                    children:
                    [
                        new TextSpan(
                            text: "highlighted",
                            style: new TextStyle(BackgroundColor: new Color(0xFFFFE8A3))),
                        new TextSpan(text: " word, a "),
                        new TextSpan(
                            text: "painted",
                            style: new TextStyle(
                                Foreground: new Paint { Color = new Color(0xFFE63946) },
                                FontWeight: FontWeight.Bold)),
                        new TextSpan(text: " one, and tabular figures: "),
                        new TextSpan(
                            text: "1111 / 8888",
                            style: new TextStyle(FontFeatures: [Plumix.UI.FontFeature.TabularFigures()])),
                    ]),
                style: new TextStyle(FontSize: 16, Color: new Color(0xFF1D3557))));
    }

    private static Widget BuildInlineWidgetParagraph()
    {
        return new Container(
            color: new Color(0xFFE7EDF6),
            padding: new Thickness(12),
            child: Text.Rich(
                new TextSpan(
                    text: "Inline ",
                    children:
                    [
                        new WidgetSpan(new Container(
                            width: 40,
                            height: 20,
                            color: new Color(0xFFE9C46A))),
                        new TextSpan(text: " widgets flow with the text."),
                    ]),
                style: new TextStyle(FontSize: 16, Color: new Color(0xFF1D3557))));
    }

    private static Widget BuildAlignmentRow()
    {
        return new Container(
            color: new Color(0xFFF8EDEB),
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
                style: new TextStyle(FontSize: 24, Color: new Color(0xFF1D3557))));
    }

    private static Widget BuildOverflowAndStrut()
    {
        const string longText = "Overflowing text can fade out, end with an ellipsis, or be clipped at the box edge.";
        Color textColor = new Color(0xFF1D3557);
        return new Container(
            color: new Color(0xFFEAF4EA),
            padding: new Thickness(12),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 8,
                children:
                [
                    new Text(
                        longText,
                        maxLines: 1,
                        softWrap: false,
                        overflow: TextOverflow.Fade,
                        fontSize: 16,
                        color: textColor),
                    new Text(longText, maxLines: 1, overflow: TextOverflow.Ellipsis, fontSize: 16, color: textColor),
                    new Text(
                        "A forced strut keeps\nboth lines 32 px apart.",
                        fontSize: 16,
                        color: textColor,
                        strutStyle: new StrutStyle(FontSize: 16, Height: 2, ForceStrutHeight: true)),
                ]));
    }

    private static Widget BuildDefaultTextStyleParagraph()
    {
        return new Container(
            color: new Color(0xFFF5F0FF),
            padding: new Thickness(12),
            child: new DefaultTextStyle(
                style: new TextStyle(FontSize: 16, Color: new Color(0xFF1D3557)),
                overflow: TextOverflow.Ellipsis,
                maxLines: 1,
                child: new Column(
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    spacing: 4,
                    children:
                    [
                        new Text("DefaultTextStyle supplies the size, color, and one-line ellipsis."),
                        DefaultTextStyle.Merge(
                            style: new TextStyle(FontWeight: FontWeight.Bold),
                            child: new Text("A merged style keeps the inherited color and line limit.")),
                    ])));
    }

    private static InlineSpan BuildBadge(PlaceholderAlignment alignment, string color)
    {
        return new WidgetSpan(
            new Container(width: 18, height: 18, color: (Color)Avalonia.Media.Color.Parse(color)),
            alignment);
    }
}
