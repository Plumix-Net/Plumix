namespace Plumix.FSharpSample

open Avalonia
open Avalonia.Themes.Fluent
open Plumix
open Plumix.FSharp

type App() =
    inherit PlumixApplication()

    // FluentTheme is added in code instead of App.axaml: no XAML in the F# sample.
    override this.Initialize() = this.Styles.Add(FluentTheme())

    // Same Theme/ScaffoldMessenger shell as the C# sample, around the MVU counter.
    override _.CreateRootWidget() =
        Ui.theme (data = ThemeData.Light, child = Ui.scaffoldMessenger (ElmishCounter.widget ()))

    override _.CreateOptions() =
        PlumixOptions(
            Title = "Plumix F# Sample",
            InitialWindowSize = Size(350.0, 700.0))
