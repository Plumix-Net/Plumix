using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/cupertino/cupertino_text_field_demo_page.dart

public sealed class CupertinoTextFieldDemoPage : StatefulWidget
{
    public override State CreateState() => new CupertinoTextFieldDemoPageState();
}

internal sealed class CupertinoTextFieldDemoPageState : State
{
    private readonly TextEditingController _controller = new();
    private string _value = string.Empty;

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12.0,
            children:
            [
                new Text("Cupertino text fields", fontSize: 20.0, color: Colors.Black),
                new Text(
                    "Rounded, borderless, multiline, disabled, attachment, placeholder, and clear-button states.",
                    fontSize: 14.0,
                    color: Color.FromUInt32(0x8A000000)),
                new CupertinoTextField(
                    controller: _controller,
                    placeholder: "Search settings",
                    prefix: new Padding(
                        EdgeInsetsGeometry.Symmetric(horizontal: 6.0),
                        new Icon(CupertinoIcons.Search, size: 18.0)),
                    clearButtonMode: OverlayVisibilityMode.Editing,
                    onChanged: value => SetState(() => _value = value)),
                new Text(
                    string.IsNullOrEmpty(_value) ? "No query" : $"Query: {_value}",
                    fontSize: 12.0,
                    color: Color.FromUInt32(0xFF607D8B)),
                CupertinoTextField.Borderless(
                    placeholder: "Borderless multiline note",
                    minLines: 2,
                    maxLines: 4,
                    decoration: new BoxDecoration(
                        Color: Color.FromUInt32(0xFFF2F2F7),
                        BorderRadius: BorderRadius.Circular(8.0))),
                new CupertinoTextField(
                    enabled: false,
                    placeholder: "Disabled field"),
            ]);
    }

    public override void Dispose()
    {
        _controller.Dispose();
        base.Dispose();
    }
}
