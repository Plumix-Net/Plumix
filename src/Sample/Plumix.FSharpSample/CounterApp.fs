namespace Plumix.FSharpSample

open Plumix.FSharp

/// Classic Flutter counter built with the Plumix.FSharp factory functions
/// (`Ui.*`) and StatefulWidget/SetState — the non-Elmish style; the app root
/// mounts the MVU variant from ElmishCounter.fs instead.
type CounterPage() =
    inherit StatefulWidget()

    override _.CreateState() = CounterPageState()

and CounterPageState() =
    inherit State()

    let mutable count = 0

    override this.Build(_context) =
        // F#: protected State.SetState is not callable from inside a lambda
        // (FS0491), so callbacks go through the public InvokeSetState helper.
        let update f = fun () -> this.InvokeSetState(fun () -> count <- f count)

        Ui.scaffold (
            appBar = Ui.appBar (titleText = "Plumix + F#"),
            body =
                Ui.center (
                    Ui.column (
                        mainAxisAlignment = MainAxisAlignment.Center,
                        spacing = 12.0,
                        children = [
                            Ui.text "You have pushed the button this many times:"
                            Ui.text (string count, fontSize = 34.0, color = Colors.DarkSlateBlue)
                            Ui.row (
                                mainAxisAlignment = MainAxisAlignment.Center,
                                spacing = 12.0,
                                children = [
                                    Ui.elevatedButton (Ui.text "-1", onPressed = update (fun c -> c - 1))
                                    Ui.elevatedButton (Ui.text "Reset", onPressed = update (fun _ -> 0))
                                ])
                        ])),
            floatingActionButton =
                Ui.floatingActionButton (
                    child = Ui.icon Icons.Add,
                    onPressed = update (fun c -> c + 1),
                    tooltip = "Increment"))

