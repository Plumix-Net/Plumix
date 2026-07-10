namespace Plumix.Elmish

open Avalonia.Threading
open Elmish
open Plumix.Foundation
open Plumix.Widgets

/// Hosts an Elmish program inside a Plumix widget. Every model change rebuilds
/// the widget tree returned by the program's view function, and Plumix's
/// element reconciliation diffs it against the previous tree — the same way a
/// StatefulWidget rebuild works, so no separate virtual DOM layer is involved.
type ElmishWidget<'arg, 'model, 'msg>(program: Program<'arg, 'model, 'msg, Widget>, arg: 'arg, ?key: Key) =
    inherit StatefulWidget(Option.toObj key)

    member internal _.Program = program
    member internal _.Arg = arg

    override _.CreateState() = ElmishWidgetState<'arg, 'model, 'msg>() :> State

and internal ElmishWidgetState<'arg, 'model, 'msg>() =
    inherit State()

    let mutable view: 'model -> Dispatch<'msg> -> Widget = fun _ _ -> Unchecked.defaultof<Widget>
    let mutable model = Unchecked.defaultof<'model>
    let mutable dispatch: Dispatch<'msg> = ignore
    let mutable started = false
    let mutable disposed = false

    override this.InitState() =
        let widget = this.StateWidget :?> ElmishWidget<'arg, 'model, 'msg>
        view <- Program.view widget.Program

        // Commands may dispatch from background threads while rebuilds must
        // happen on the UI thread, so the dispatch loop is marshaled there.
        let uiDispatch (innerDispatch: Dispatch<'msg>) : Dispatch<'msg> =
            fun msg ->
                if Dispatcher.UIThread.CheckAccess() then
                    innerDispatch msg
                else
                    Dispatcher.UIThread.Post(fun () -> innerDispatch msg)

        let setState newModel newDispatch =
            model <- newModel
            dispatch <- newDispatch
            // The initial setState arrives synchronously from runWithDispatch
            // below, before the first Build; only later changes need a rebuild.
            if started && not disposed then
                this.InvokeSetState(fun () -> ())

        widget.Program
        |> Program.withSetState setState
        |> Program.runWithDispatch uiDispatch widget.Arg

        started <- true

    // The Elmish loop has no termination hook wired here yet: subscriptions
    // started by the program outlive this widget. Guarding `disposed` only
    // stops rebuilds of a defunct element.
    override _.Dispose() = disposed <- true

    override _.Build(_context) = view model dispatch

/// `Program.toWidget` / `Program.toWidgetWith` complement the `Elmish.Program`
/// module: `Program.mkProgram init update view |> Program.toWidget`.
[<RequireQualifiedAccess>]
module Program =

    /// Hosts the program as a Plumix widget; the program starts when the
    /// widget is mounted.
    let toWidget (program: Program<unit, 'model, 'msg, Widget>) : Widget =
        ElmishWidget(program, ()) :> Widget

    /// Same as `toWidget`, passing an argument to the program's init.
    let toWidgetWith (arg: 'arg) (program: Program<'arg, 'model, 'msg, Widget>) : Widget =
        ElmishWidget(program, arg) :> Widget
