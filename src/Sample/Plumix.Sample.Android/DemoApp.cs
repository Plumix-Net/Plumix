using Plumix.Material;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using ListView = Plumix.Widgets.ListView;

namespace Plumix.Sample.Android;

// C#-only sample code; no Dart parity source. A smoke test of the Impeller backend: shapes, paths, clips,
// gradients, shadows, opacity, text, images and scrolling.

/// <summary>The Impeller backend demo app.</summary>
internal static class DemoApp
{
    public static Widget Create() => new MaterialApp(
        debugShowCheckedModeBanner: false,
        title: "Plumix Sample",
        home: new CounterPage());

    private sealed class CounterPage : StatefulWidget
    {
        public override State CreateState() => new CounterPageState();
    }

    private sealed class CounterPageState : State<CounterPage>
    {
        private int _count;

        public override Widget Build(BuildContext context) => new Scaffold(
            appBar: new AppBar(title: new Text("Impeller")),
            floatingActionButton: new FloatingActionButton(
                onPressed: () => SetState(() => _count++),
                child: new Icon(Icons.Add)),
            body: ListView.Builder(
                itemCount: 61,
                itemBuilder: (_, index) => index == 0 ? Typography() : Tile(index - 1)));

        private static Widget Typography() => new Padding(
            insets: EdgeInsets.All(16),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Start,
                children:
                [
                    new Text("Glyph runs", style: new TextStyle(FontSize: 28, FontWeight: FontWeight.W300)),
                    new Text("Bold weight, synthesized", style: new TextStyle(FontWeight: FontWeight.Bold)),
                    new Text("Italic slant, synthesized", style: new TextStyle(FontStyle: FontStyle.Italic)),
                    new Text("Кириллица: съешь же ещё этих мягких булок"),
                    new Text("Underlined and struck",
                        style: new TextStyle(Decoration: TextDecoration.Underline | TextDecoration.LineThrough)),
                    new Text("Shadowed text", style: new TextStyle(FontSize: 20,
                        Shadows:
                        [
                            new Shadow(color: new Color(0x80000000), offset: new Offset(2, 2), blurRadius: 3),
                        ])),
                    new Text("A long line that does not fit on one line and ends with an ellipsis at the end",
                        maxLines: 1, overflow: TextOverflow.Ellipsis),
                    new Text("fi fl ffi — ligatures, kerning AVAWAY, "
                             + "fallback ☺ ✓ ∑ 😀 中文 العربية"),
                    new SizedBox(height: 12),
                    new TextField(decoration: new InputDecoration(labelText: "Type here", border:
                        new OutlineInputBorder())),
                    new SizedBox(height: 12),
                    new Row(children:
                    [
                        // An Android asset, decoded by BitmapFactory, drawn from an Impeller texture.
                        Plumix.Widgets.Image.Asset("plumix.png", width: 96, height: 96),
                        new SizedBox(width: 12),
                        new ClipOval(child: Plumix.Widgets.Image.Asset("plumix.png", width: 64, height: 64,
                            cacheWidth: 32)),
                        new SizedBox(width: 12),
                        new Opacity(opacity: 0.5,
                            child: Plumix.Widgets.Image.Asset("plumix.png", width: 48, height: 48)),
                    ]),
                ]));

        private Widget Tile(int index) => new Padding(
            insets: EdgeInsets.Symmetric(horizontal: 16, vertical: 8),
            child: new Container(
                height: 72,
                decoration: new BoxDecoration(
                    BorderRadius: BorderRadius.Circular(16),
                    Gradient: new LinearGradient(colors:
                    [
                        MaterialColors.Primaries[index % MaterialColors.Primaries.Count],
                        MaterialColors.Primaries[(index + 3) % MaterialColors.Primaries.Count],
                    ]),
                    BoxShadows: [new BoxShadow(color: new Color(0x40000000), blurRadius: 8, offset: new Offset(0, 4))]),
                child: new Row(children:
                [
                    new SizedBox(width: 16),
                    new CircleAvatar(child: new Text($"{(index + _count) % 100}")),
                    new SizedBox(width: 16),
                    new Expanded(child: new Text($"Item {index}, taps {_count}")),
                    new Opacity(opacity: 0.6, child: new Icon(index % 2 == 0 ? Icons.Star : Icons.Favorite)),
                    new SizedBox(width: 16),
                ])));
    }
}
