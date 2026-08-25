module Plumix.FSharpSample.ElmishCounter

open Elmish
open Plumix.Elmish
open Plumix.FSharp

// The same counter as CounterPage, restructured as an Elmish (MVU) program.
// The view rebuilds the widget tree on every model change and Plumix's
// element reconciliation diffs it — no virtual DOM layer in between.

type Model = { Count: int }

type Msg =
    | Increment
    | Decrement
    | Reset
    | IncrementDelayed

let init () = { Count = 0 }, Cmd.none

let update msg model =
    match msg with
    | Increment -> { model with Count = model.Count + 1 }, Cmd.none
    | Decrement -> { model with Count = model.Count - 1 }, Cmd.none
    | Reset -> { model with Count = 0 }, Cmd.none
    | IncrementDelayed ->
        // Completes on a thread-pool thread; the host marshals the resulting
        // dispatch back to the UI thread.
        let delayed () =
            async {
                do! Async.Sleep 1000
                return Increment
            }

        model, Cmd.OfAsync.perform delayed () id

let view model dispatch =
    Ui.scaffold (
        appBar = Ui.appBar (title = Ui.text "Plumix + F# + Elmish"),
        body =
            Ui.center (
                Ui.column (
                    mainAxisAlignment = MainAxisAlignment.Center,
                    spacing = 12.0,
                    children = [
                        Ui.text "You have pushed the button this many times:"
                        Ui.text (string model.Count, fontSize = 34.0, color = Colors.DarkSlateBlue)
                        Ui.row (
                            mainAxisAlignment = MainAxisAlignment.Center,
                            spacing = 12.0,
                            children = [
                                Ui.elevatedButton (Ui.text "-1", onPressed = fun () -> dispatch Decrement)
                                Ui.elevatedButton (Ui.text "Reset", onPressed = fun () -> dispatch Reset)
                                Ui.elevatedButton (Ui.text "+1 in 1s", onPressed = fun () -> dispatch IncrementDelayed)
                            ])
                    ])),
        floatingActionButton =
            Ui.floatingActionButton (
                child = Ui.icon Icons.Add,
                onPressed = (fun () -> dispatch Increment),
                tooltip = "Increment"))

/// The MVU counter as a widget, ready to mount anywhere in a Plumix tree.
let widget () : Widget =
    Program.mkProgram init update view |> Program.toWidget
