using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/general/page_view_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class PageViewDemoPage : StatefulWidget
{
    public override State CreateState() => new PageViewDemoPageState();

    private sealed class PageViewDemoPageState : State
    {
        private static readonly Color[] PageColors =
        [
            new Color(0xFFE3F2FD),
            new Color(0xFFE8F5E9),
            new Color(0xFFFFF3E0),
            new Color(0xFFF3E5F5),
            new Color(0xFFE0F7FA),
            new Color(0xFFFCE4EC),
        ];

        private PageController? _controller;
        private int _page;

        public override void InitState() => _controller = new PageController(viewportFraction: 0.85);

        public override void Dispose()
        {
            _controller?.Dispose();
            _controller = null;

            base.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            return new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 10,
                children:
                [
                    new Text("PageView.builder", style: new TextStyle(FontSize: 20, Color: Colors.Black)),
                    new Text(
                        "Pages are built lazily by a sliver fill viewport; viewportFraction 0.85 pads both ends.",
                        style: new TextStyle(FontSize: 14, Color: MaterialColors.Black54)),
                    new Expanded(
                        child: PageView.Builder(
                            itemCount: PageColors.Length,
                            controller: _controller,
                            onPageChanged: page => SetState(() => _page = page),
                            itemBuilder: (_, index) => new Padding(
                                EdgeInsets.Symmetric(horizontal: 8, vertical: 12),
                                new Container(
                                    color: PageColors[index],
                                    padding: EdgeInsets.All(16),
                                    child: new Center(
                                        child: new Text(
                                            $"page #{index}",
                                            style: new TextStyle(FontSize: 24, Color: Colors.Black))))))),
                    new Row(
                        mainAxisAlignment: MainAxisAlignment.Center,
                        spacing: 12,
                        children:
                        [
                            new TextButton(
                                onPressed: () => _controller?.PreviousPage(
                                    TimeSpan.FromMilliseconds(300),
                                    Curves.Ease),
                                child: new Text("Previous")),
                            new Text(
                                $"page {_page + 1} of {PageColors.Length}",
                                style: new TextStyle(
                                    FontSize: 14,
                                    Color: Colors.Black)),
                            new TextButton(
                                onPressed: () => _controller?.NextPage(
                                    TimeSpan.FromMilliseconds(300),
                                    Curves.Ease),
                                child: new Text("Next")),
                        ]),
                ]);
        }
    }
}
