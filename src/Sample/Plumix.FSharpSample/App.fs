namespace Plumix.FSharpSample

open Avalonia
open Avalonia.Themes.Fluent
open Plumix

type App() =
    inherit PlumixApplication()

    // FluentTheme is added in code instead of App.axaml: no XAML in the F# sample.
    override this.Initialize() = this.Styles.Add(FluentTheme())

    override _.CreateRootWidget() = FSharpCounterApp()

    override _.CreateOptions() =
        PlumixOptions(
            Title = "Plumix F# Sample",
            InitialWindowSize = Size(350.0, 700.0))
