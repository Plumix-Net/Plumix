namespace Plumix.FSharpSample

open Avalonia.Media
open Plumix.Material
open Plumix.Rendering
open Plumix.Widgets

/// Classic Flutter counter written against the raw (C#-shaped) Plumix API,
/// to exercise F# interop: subclassing, named/optional args, widget lists,
/// callbacks and SetState.
type CounterPage() =
    inherit StatefulWidget()

    override _.CreateState() = CounterPageState()

and CounterPageState() =
    inherit State()

    let mutable count = 0

    override this.Build(_context) =
        // F#: protected State.SetState is not callable from inside a lambda
        // (FS0405), so callbacks go through the public InvokeSetState helper.
        let changeBy delta =
            fun () -> this.InvokeSetState(fun () -> count <- count + delta)

        Scaffold(
            appBar = AppBar(titleText = "Plumix + F#"),
            body =
                Center(
                    child =
                        Column(
                            mainAxisAlignment = MainAxisAlignment.Center,
                            spacing = 12.0,
                            children =
                                [| Text("You have pushed the button this many times:")
                                   Text(string count, fontSize = 34.0, color = Colors.DarkSlateBlue)
                                   Row(
                                       mainAxisAlignment = MainAxisAlignment.Center,
                                       spacing = 12.0,
                                       children =
                                           [| ElevatedButton(child = Text("-1"), onPressed = changeBy -1)
                                              ElevatedButton(child = Text("Reset"), onPressed = (fun () -> this.InvokeSetState(fun () -> count <- 0))) |]) |])),
            floatingActionButton =
                FloatingActionButton(
                    child = Icon(Icons.Add),
                    onPressed = changeBy 1,
                    tooltip = "Increment"))

/// Root widget: same Theme/ScaffoldMessenger shell as the C# sample.
type FSharpCounterApp() =
    inherit StatelessWidget()

    override _.Build(_context) =
        Theme(
            data = ThemeData.Light,
            child = ScaffoldMessenger(child = CounterPage()))
