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
            Color.Parse("#FFE3F2FD"),
            Color.Parse("#FFE8F5E9"),
            Color.Parse("#FFFFF3E0"),
            Color.Parse("#FFF3E5F5"),
            Color.Parse("#FFE0F7FA"),
            Color.Parse("#FFFCE4EC"),
        ];

        private PageController? _controller;
        private int _page;

        public override void InitState() => _controller = new PageController(viewportFraction: 0.85);

        public override void Dispose()
        {
            _controller?.Dispose();
            _controller = null;
        }

        public override Widget Build(BuildContext context)
        {
            return new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 10,
                children:
                [
                    new Text("PageView.Builder", fontSize: 20, color: Colors.Black),
                    new Text(
                        "Pages are built lazily by a sliver fill viewport; viewportFraction 0.85 pads both ends.",
                        fontSize: 14,
                        color: Colors.DimGray),
                    new Expanded(
                        child: PageView.Builder(
                            itemCount: PageColors.Length,
                            controller: _controller,
                            onPageChanged: page => SetState(() => _page = page),
                            itemBuilder: (_, index) => new Padding(
                                new Thickness(8, 12),
                                new Container(
                                    color: PageColors[index],
                                    padding: new Thickness(16),
                                    child: new Center(
                                        child: new Text(
                                            $"page #{index}",
                                            fontSize: 24,
                                            color: Colors.Black)))))),
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
                            new Text($"page {_page + 1} of {PageColors.Length}", fontSize: 14, color: Colors.Black),
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
