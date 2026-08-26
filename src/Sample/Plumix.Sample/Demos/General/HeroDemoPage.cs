using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/general/hero_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class HeroDemoPage : StatefulWidget
{
    public override State CreateState() => new HeroDemoPageState();
}

internal sealed class HeroDemoPageState : State
{
    private bool _heroModeEnabled = true;

    public override Widget Build(BuildContext context)
    {
        return new SingleChildScrollView(
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 10,
                children:
                [
                    new Text("Hero", fontSize: 20, color: Colors.Black),
                    new Text(
                        "Tap a tile to push a detail route. The tile flies between the two routes along the "
                        + "MaterialRectArcTween MaterialApp installs on its HeroController.",
                        fontSize: 14,
                        color: Colors.DimGray),
                    new Row(
                        spacing: 8,
                        children:
                        [
                            BuildButton(
                                "HeroMode enabled",
                                () => SetHeroMode(true),
                                width: 140,
                                colorHex: "#FFDCE3ED"),
                            BuildButton(
                                "HeroMode disabled",
                                () => SetHeroMode(false),
                                width: 146,
                                colorHex: "#FFDCE3ED"),
                        ]),
                    new Text(
                        $"state: heroMode={(_heroModeEnabled ? "enabled" : "disabled")}",
                        fontSize: 12,
                        color: Colors.DarkSlateGray),
                    new Text(
                        "A disabled HeroMode hides the subtree from Hero._allHeroesFor, so the route "
                        + "transition runs without a flight.",
                        fontSize: 11,
                        color: Colors.DimGray),
                    BuildTile(
                        context,
                        tag: "hero-demo-plain",
                        label: "Default flight",
                        colorHex: "#FF1D3557",
                        useShuttleBuilder: false),
                    BuildTile(
                        context,
                        tag: "hero-demo-shuttle",
                        label: "Custom shuttle + placeholder",
                        colorHex: "#FFE07A5F",
                        useShuttleBuilder: true),
                    new Text(
                        "The second tile supplies a flightShuttleBuilder (what the overlay paints while the "
                        + "hero is in the air) and a placeholderBuilder (what each route shows in its place).",
                        fontSize: 11,
                        color: Colors.DimGray),
                ]));
    }

    private Widget BuildTile(BuildContext context, string tag, string label, string colorHex, bool useShuttleBuilder)
    {
        Widget hero = new Hero(
            tag: tag,
            flightShuttleBuilder: useShuttleBuilder ? BuildShuttle : null,
            placeholderBuilder: useShuttleBuilder ? BuildPlaceholder : null,
            child: BuildCard(label, colorHex, width: 150, height: 84, fontSize: 12));

        if (!_heroModeEnabled)
        {
            hero = new HeroMode(enabled: false, child: hero);
        }

        return new Row(
            children:
            [
                new GestureDetector(
                    onTap: () => Navigator.Of(context).Push(
                        new MaterialPageRoute(
                            builder: _ => new HeroDetailPage(
                                tag: tag,
                                label: label,
                                colorHex: colorHex,
                                useShuttleBuilder: useShuttleBuilder),
                            settings: new RouteSettings(Name: $"/hero/{tag}"))),
                    child: hero),
            ]);
    }

    private static Widget BuildShuttle(
        BuildContext flightContext,
        Animation<double> animation,
        HeroFlightDirection flightDirection,
        BuildContext fromHeroContext,
        BuildContext toHeroContext)
    {
        return new DecoratedBox(
            decoration: new BoxDecoration(
                Color: Color.Parse("#FF264653"),
                BorderRadius: BorderRadius.Circular(14)),
            child: new Center(
                child: new Text(
                    flightDirection == HeroFlightDirection.Push ? "flying →" : "flying ←",
                    fontSize: 12,
                    color: Colors.White)));
    }

    private static Widget BuildPlaceholder(BuildContext context, Size heroSize, Widget child)
    {
        return new SizedBox(
            width: heroSize.Width,
            height: heroSize.Height,
            child: new DecoratedBox(
                decoration: new BoxDecoration(
                    Color: Color.Parse("#FFF1F1F1"),
                    Border: Border.FromBorderSide(new BorderSide(Color.Parse("#FFBDBDBD"), 1)),
                    BorderRadius: BorderRadius.Circular(14)),
                child: new Center(
                    child: new Text("placeholder", fontSize: 11, color: Colors.DimGray))));
    }

    internal static Widget BuildCard(string label, string colorHex, double width, double height, double fontSize)
    {
        return new SizedBox(
            width: width,
            height: height,
            child: new DecoratedBox(
                decoration: new BoxDecoration(
                    Color: Color.Parse(colorHex),
                    BorderRadius: BorderRadius.Circular(14)),
                child: new Center(
                    child: new Text(label, fontSize: fontSize, color: Colors.White))));
    }

    private Widget BuildButton(string label, Action onTap, double width, string colorHex)
    {
        return new SizedBox(
            width: width,
            child: new CounterTapButton(
                label: label,
                onTap: onTap,
                background: Color.Parse(colorHex),
                foreground: Colors.Black,
                fontSize: 12,
                padding: new Thickness(10, 8)));
    }

    private void SetHeroMode(bool value)
    {
        SetState(() => _heroModeEnabled = value);
    }
}

internal sealed class HeroDetailPage : StatelessWidget
{
    public HeroDetailPage(
        string tag,
        string label,
        string colorHex,
        bool useShuttleBuilder,
        Key? key = null) : base(key)
    {
        Tag = tag;
        Label = label;
        ColorHex = colorHex;
        UseShuttleBuilder = useShuttleBuilder;
    }

    public string Tag { get; }

    public string Label { get; }

    public string ColorHex { get; }

    public bool UseShuttleBuilder { get; }

    public override Widget Build(BuildContext context)
    {
        return new Container(
            color: Color.Parse("#FFFDFDFD"),
            padding: new Thickness(16),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 12,
                children:
                [
                    new Text("Hero detail", fontSize: 20, color: Colors.Black),
                    new Hero(
                        tag: Tag,
                        flightShuttleBuilder: null,
                        child: HeroDemoPageState.BuildCard(Label, ColorHex, width: 288, height: 176, fontSize: 16)),
                    new Text(
                        "The destination hero owns the flight: its createRectTween, curve and "
                        + "flightShuttleBuilder win over the source hero's.",
                        fontSize: 12,
                        color: Colors.DimGray),
                    new CounterTapButton(
                        label: "Pop",
                        onTap: () => Navigator.Of(context).Pop(),
                        background: Color.Parse("#FFDCE3ED"),
                        foreground: Colors.Black,
                        fontSize: 12,
                        padding: new Thickness(10, 8)),
                ]));
    }
}
