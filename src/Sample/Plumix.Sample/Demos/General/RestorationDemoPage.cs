using System.Linq;
using Avalonia;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: dart_sample/lib/demos/general/restoration_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class RestorationDemoPage : StatefulWidget
{
    public override State CreateState() => new RestorationDemoPageState();

    private sealed class RestorationDemoPageState : RestorationState<RestorationDemoPage>
    {
        private readonly RestorableInt _counter = new(0);
        private readonly RestorableTextEditingController _note = new();

        protected override string? RestorationId => "restoration-demo";

        protected override void RestoreState(RestorationBucket? oldBucket, bool initialRestore)
        {
            RegisterForRestoration(_counter, "counter");
            RegisterForRestoration(_note, "note");
        }

        public override Widget Build(BuildContext context)
        {
            return new SingleChildScrollView(
                restorationId: "scroll",
                child: new Column(
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    spacing: 12,
                    children:
                    [
                        new Text(Bucket is null ? "State restoration: unavailable" : "State restoration: enabled"),
                        new Text(
                            "Increment the counter, write a note and scroll. On Android, enable Don't keep "
                            + "activities, background the app and reopen it. On iOS, background the app, "
                            + "terminate it through the debugger and reopen it. The page, values and scroll "
                            + "position are restored when the OS supplies saved state."),
                        new Text($"Restorable counter: {_counter.Value}"),
                        new OutlinedButton(
                            onPressed: () => SetState(() => _counter.Value++),
                            child: new Text("Increment")),
                        new TextField(
                            controller: _note.Value,
                            decoration: new InputDecoration(labelText: "Restorable note")),
                        .. Enumerable.Range(1, 24).Select(index => new Container(
                            height: 48,
                            alignment: Alignment.CenterLeft,
                            padding: new Thickness(12, 0),
                            child: new Text($"Restorable row {index}"))),
                    ]));
        }

        public override void Dispose()
        {
            _counter.Dispose();
            _note.Dispose();
            base.Dispose();
        }
    }
}
