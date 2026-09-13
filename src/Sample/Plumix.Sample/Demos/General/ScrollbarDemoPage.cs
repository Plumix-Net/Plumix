using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;
using Plumix.UI;
using MaterialScrollbar = Plumix.Material.Scrollbar;

// Dart parity source (reference): dart_sample/lib/demos/general/scrollbar_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class ScrollbarDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new ScrollbarDemoPageState();
    }
}

internal sealed class ScrollbarDemoPageState : State
{
    private ScrollController _materialController = null!;
    private ScrollController _rawController = null!;
    private bool _safeArea;
    private bool _rtl;
    private int _shapeIndex;

    private OutlinedBorder? ThumbShape => _shapeIndex switch
    {
        1 => new CircleBorder(side: new BorderSide(Colors.DarkBlue, 1)),
        2 => new BeveledRectangleBorder(borderRadius: BorderRadius.Circular(4)),
        3 => new RoundedRectangleBorder(borderRadius: BorderRadius.Only(
            topLeft: 8, bottomRight: 8)),
        _ => null,
    };

    public override void InitState()
    {
        _materialController = new ScrollController();
        _rawController = new ScrollController();
    }

    public override void Dispose()
    {
        _materialController.Dispose();
        _rawController.Dispose();

        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("Scrollbar + RawScrollbar", fontSize: 20, color: Colors.Black),
                new Text(
                    "Material state theming/fade beside an always-visible raw track; both thumbs are draggable.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 4,
                    children:
                    [
                        new TextButton(
                            onPressed: () => SetState(() => _safeArea = !_safeArea),
                            child: new Text(_safeArea ? "Insets: on" : "Insets: off")),
                        new TextButton(
                            onPressed: () => SetState(() => _rtl = !_rtl),
                            child: new Text(_rtl ? "RTL" : "LTR")),
                        new TextButton(
                            onPressed: () => SetState(() => _shapeIndex = (_shapeIndex + 1) % 4),
                            child: new Text(new[] { "Ellipse", "Circle", "Beveled", "Corners" }[_shapeIndex])),
                    ]),
                new Expanded(
                    child: new Row(
                        spacing: 12,
                        children:
                        [
                            new Expanded(
                                child: BuildPane(
                                    "Material state theme",
                                    new ScrollbarTheme(
                                        data: new ScrollbarThemeData(
                                            trackVisibility: WidgetStateProperty<bool?>.ResolveWith(states =>
                                                states.Contains(WidgetState.Hovered)),
                                            thickness: WidgetStateProperty<double?>.ResolveWith(states =>
                                                states.Contains(WidgetState.Hovered) ? 12 : 8),
                                            thumbColor: WidgetStateProperty<Color?>.ResolveWith(states =>
                                                states.Contains(WidgetState.Dragged)
                                                    ? Color.Parse("#FF7B1FA2")
                                                    : Color.Parse("#FF1565C0"))),
                                        child: new MaterialScrollbar(
                                            controller: _materialController,
                                            child: BuildList(_materialController, "material"))))),
                            new Expanded(
                                child: BuildPane(
                                    "Raw + track",
                                    new Directionality(
                                        textDirection: _rtl ? TextDirection.Rtl : TextDirection.Ltr,
                                        child: new MediaQuery(
                                            data: MediaQuery.Of(context) with
                                            {
                                                Padding = _safeArea ? new Thickness(12, 24, 20, 40) : default,
                                            },
                                            child: new RawScrollbar(
                                                    controller: _rawController,
                                                    thumbVisibility: true,
                                                    trackVisibility: true,
                                                    thickness: 8,
                                                    radius: ThumbShape is null ? Radius.Elliptical(4, 8) : null,
                                                    shape: ThumbShape,
                                                    thumbColor: Color.Parse("#B3005E7A"),
                                                    trackColor: Color.Parse("#14005E7A"),
                                                    trackBorderColor: Color.Parse("#33005E7A"),
                                                    child: BuildList(_rawController, "raw")))))),
                        ])),
            ]);
    }

    private static Widget BuildPane(string label, Widget child) => new Column(
        crossAxisAlignment: CrossAxisAlignment.Stretch,
        spacing: 6,
        children:
        [
            new Text(label, fontSize: 13, color: Colors.DimGray),
            new Expanded(child: child),
        ]);

    private static Widget BuildList(ScrollController controller, string prefix) => ListView.Builder(
        itemCount: 70,
        controller: controller,
        itemExtent: 40,
        padding: new Thickness(10),
        itemBuilder: (_, index) => new Container(
            color: index % 2 == 0 ? Colors.White : Color.Parse("#FFF4F7FA"),
            padding: new Thickness(10, 8),
            child: new Text($"{prefix} row #{index}", fontSize: 13, color: Colors.Black)),
        addAutomaticKeepAlives: false);
}
