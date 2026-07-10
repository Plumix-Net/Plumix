module Plumix.FSharpSample.Program

open System
open Avalonia

// Avalonia configuration, also used by the visual designer / previewer.
[<CompiledName "BuildAvaloniaApp">]
let buildAvaloniaApp () =
    AppBuilder
        .Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace()

[<EntryPoint; STAThread>]
let main args =
    buildAvaloniaApp().StartWithClassicDesktopLifetime(args)
