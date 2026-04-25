using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/scrollbar_demo_page.dart (exact sample parity)

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
    private ScrollController _controller = null!;

    public override void InitState()
    {
        _controller = new ScrollController();
    }

    public override void Dispose()
    {
        _controller.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                new Text("Scrollbar + ScrollController", fontSize: 20, color: Colors.Black),
                new Text(
                    "The thumb is computed from viewport/content metrics.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Expanded(
                    child: new Scrollbar(
                        controller: _controller,
                        child: ListView.Builder(
                            itemCount: 70,
                            controller: _controller,
                            itemExtent: 40,
                            padding: new Thickness(10),
                            itemBuilder: (_, index) => new Container(
                                color: index % 2 == 0 ? Colors.White : Color.Parse("#FFF4F7FA"),
                                padding: new Thickness(10, 8),
                                child: new Text($"scroll row #{index}", fontSize: 13, color: Colors.Black)),
                            addAutomaticKeepAlives: false))),
            ]);
    }
}
