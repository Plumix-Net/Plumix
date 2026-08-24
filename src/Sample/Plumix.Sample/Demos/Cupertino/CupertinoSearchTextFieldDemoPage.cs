using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/cupertino/cupertino_search_text_field_demo_page.dart

public sealed class CupertinoSearchTextFieldDemoPage : StatefulWidget
{
    public override State CreateState() => new CupertinoSearchTextFieldDemoPageState();
}

internal sealed class CupertinoSearchTextFieldDemoPageState : State
{
    private readonly TextEditingController _controller = new();
    private string _query = string.Empty;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12.0,
            children:
            [
                new Text("Cupertino search text field", fontSize: 20.0, color: Colors.Black),
                new Text(
                    "Localized placeholder, live clear button, custom icon treatment, and disabled state.",
                    fontSize: 14.0,
                    color: Color.FromUInt32(0x8A000000)),
                new CupertinoSearchTextField(
                    controller: _controller,
                    onChanged: value => SetState(() => _query = value)),
                new Text(
                    string.IsNullOrEmpty(_query) ? "No query" : $"Query: {_query}",
                    fontSize: 12.0,
                    color: Color.FromUInt32(0xFF607D8B)),
                new CupertinoSearchTextField(
                    placeholder: "Always-visible action",
                    itemColor: CupertinoColors.SystemBlue,
                    itemSize: 24.0,
                    suffixMode: OverlayVisibilityMode.Always),
                new CupertinoSearchTextField(
                    placeholder: "Disabled search",
                    enabled: false),
            ]);
    }

    public override void Dispose()
    {
        _controller.Dispose();
        base.Dispose();
    }
}
