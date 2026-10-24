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
    private readonly DoubleTapGestureRecognizer _doubleTapRecognizer = new();
    private readonly LongPressGestureRecognizer _longPressRecognizer = new();
    private int _taps;
    private int _inlineActions;
    private bool _boldText;
    private bool _spacingOverrides;

    public override void InitState()
    {
        base.InitState();
        _tapRecognizer.OnTap = () => SetState(() => _taps += 1);
        _doubleTapRecognizer.OnDoubleTap = () => SetState(() => _inlineActions += 1);
        _longPressRecognizer.OnLongPress = () => SetState(() => _inlineActions += 1);
    }

    public override void Dispose()
    {
        _tapRecognizer.Dispose();
        _doubleTapRecognizer.Dispose();
        _longPressRecognizer.Dispose();
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new SingleChildScrollView(child: new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12,
            children:
            [
                new Text("RichText + TextSpan + WidgetSpan", style: new TextStyle(FontSize: 20, Color: Colors.Black)),
                new Text(
                    "One paragraph, many styles. Spans share a single line layout, carry their own gesture "
                    + "recognizers, and can embed inline widgets.",
                    style: new TextStyle(FontSize: 14, Color: MaterialColors.Grey)),
                BuildStyledParagraph(),
                new Text(
                    "Tapped the link span " + _taps + " times",
                    style: new TextStyle(
                        FontSize: 14,
                        Color: MaterialColors.Grey)),
                BuildPaintedParagraph(),
                BuildInlineWidgetParagraph(),
                BuildAccessibleInlineParagraph(),
                BuildAlignmentRow(),
                BuildDefaultTextStyleParagraph(),
                BuildOverflowAndStrut(),
                BuildParagraphEffects(),
                BuildAccessibilityOverrides(context),
            ]));
    }

    // MediaQuery.boldText and the text spacing overrides: Text and Text.rich apply them, RichText does not.
    private Widget BuildAccessibilityOverrides(BuildContext context)
    {
        var bodyStyle = new TextStyle(FontSize: 15, Color: new Color(0xFF1D3557));
        return new Container(
            color: new Color(0xFFFFF7ED),
            padding: new Thickness(12),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 8,
                children:
                [
                    new Row(
                        spacing: 8,
                        children:
                        [
                            BuildToggle("Bold text", _boldText, () => SetState(() => _boldText = !_boldText)),
                            BuildToggle(
                                "Spacing overrides",
                                _spacingOverrides,
                                () => SetState(() => _spacingOverrides = !_spacingOverrides)),
                        ]),
                    new MediaQuery(
                        data: MediaQuery.Of(context).CopyWith(boldText: _boldText),
                        child: MediaQuery.ApplyTextStyleOverrides(
                            lineHeightScaleFactorOverride: _spacingOverrides ? 1.8 : null,
                            letterSpacingOverride: _spacingOverrides ? 1.5 : null,
                            wordSpacingOverride: _spacingOverrides ? 6.0 : null,
                            paragraphSpacingOverride: null,
                            child: new Column(
                                crossAxisAlignment: CrossAxisAlignment.Stretch,
                                spacing: 4,
                                children:
                                [
                                    new Text(
                                        "Text follows the platform's bold and spacing settings.",
                                        style: bodyStyle),
                                    Text.Rich(new TextSpan(
                                        text: "Text.rich applies them ",
                                        style: bodyStyle,
                                        children:
                                        [
                                            new TextSpan(
                                                text: "to every span.",
                                                style: new TextStyle(Color: new Color(0xFFE76F51))),
                                        ])),
                                    new RichText(text: new TextSpan(
                                        text: "RichText ignores the overrides.",
                                        style: bodyStyle)),
                                ]))),
                ]));
    }

    private static Widget BuildToggle(string label, bool selected, System.Action onTap)
    {
        return new GestureDetector(
            onTap: onTap,
            child: new Container(
                color: selected ? new Color(0xFF2A9D8F) : new Color(0xFFE2E8F0),
                padding: new Thickness(10, 6),
                child: new Text(
                    label,
                    style: new TextStyle(
                        FontSize: 13,
                        Color: selected ? Colors.White : new Color(0xFF1D3557)))));
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

    private Widget BuildAccessibleInlineParagraph()
    {
        return new Container(
            color: new Color(0xFFF0FDF4),
            padding: new Thickness(12),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 6,
                children:
                [
                    new Text("Accessible inline actions", style: new TextStyle(FontWeight: FontWeight.Bold)),
                    new RichText(text: new TextSpan(
                        text: "Read ",
                        style: new TextStyle(FontSize: 16, Color: new Color(0xFF1D3557)),
                        children:
                        [
                            new TextSpan("ABC", spellOut: true, semanticsIdentifier: "inline-code"),
                            new TextSpan(" then "),
                            new TextSpan("double-tap", recognizer: _doubleTapRecognizer,
                                style: new TextStyle(Decoration: Plumix.UI.TextDecoration.Underline)),
                            new TextSpan(" or "),
                            new TextSpan("hold", recognizer: _longPressRecognizer,
                                style: new TextStyle(Decoration: Plumix.UI.TextDecoration.Underline)),
                            new TextSpan(" beside "),
                            new WidgetSpan(new Row(mainAxisSize: MainAxisSize.Min, spacing: 4, children:
                            [
                                new Semantics(container: true, child: new Text("first")),
                                new Semantics(container: true, child: new Text("second")),
                            ])),
                            new TextSpan("."),
                        ])),
                    new Text($"Activated inline actions {_inlineActions} times"),
                ]));
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
                        style: new TextStyle(FontSize: 16, Color: textColor)),
                    new Text(
                        longText,
                        maxLines: 1,
                        overflow: TextOverflow.Ellipsis,
                        style: new TextStyle(
                            FontSize: 16,
                            Color: textColor)),
                    new Text(
                        "A forced strut keeps\nboth lines 32 px apart.",
                        style: new TextStyle(FontSize: 16, Color: textColor),
                        strutStyle: new StrutStyle(FontSize: 16, Height: 2, ForceStrutHeight: true)),
                ]));
    }

    private static Widget BuildParagraphEffects()
    {
        return new Container(
            color: new Color(0xFFF0F9FF),
            padding: new Thickness(12),
            child: new RichText(
                textHeightBehavior: new TextHeightBehavior(false, false, TextLeadingDistribution.Even),
                strutStyle: new StrutStyle(FontSize: 18, Height: 1.5),
                text: new TextSpan(
                    style: new TextStyle(FontSize: 18, Color: new Color(0xFF1D3557)),
                    children:
                    [
                        new TextSpan("Shadows", style: new TextStyle(Shadows:
                            [new Shadow(new Color(0x883B82F6), new Point(3, 3), 2)])),
                        new TextSpan(" and "),
                        new TextSpan("outlined text\n", style: new TextStyle(Foreground: new Paint
                        {
                            Color = new Color(0xFFBE185D), Style = PaintingStyle.Stroke, StrokeWidth = 0.8,
                        })),
                        new TextSpan("Wide spacing", style: new TextStyle(LetterSpacing: 3, Height: 2)),
                        new TextSpan(" with "),
                        new TextSpan("thick underline", style: new TextStyle(
                            Decoration: Plumix.UI.TextDecoration.Underline, DecorationThickness: 3)),
                    ])));
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
