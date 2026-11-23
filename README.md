<p align="center">
  <img src="icon.png" alt="Plumix" width="120" />
</p>

# Plumix

Flutter-inspired UI framework for .NET — build declarative, widget-based UIs in C# or F# with Flutter's `Widget`/`Element`/`RenderObject` architecture.

[![Website](https://img.shields.io/badge/website-plumix.net-blue)](https://plumix.net/)
[![Plumix](https://img.shields.io/nuget/v/Plumix?label=Plumix&logo=nuget)](https://www.nuget.org/packages/Plumix/)
[![Plumix.Material](https://img.shields.io/nuget/v/Plumix.Material?label=Plumix.Material&logo=nuget)](https://www.nuget.org/packages/Plumix.Material/)
[![Plumix.Cupertino](https://img.shields.io/nuget/v/Plumix.Cupertino?label=Plumix.Cupertino&logo=nuget)](https://www.nuget.org/packages/Plumix.Cupertino/)
[![CI](https://github.com/Plumix-Net/Plumix/actions/workflows/ci.yml/badge.svg)](https://github.com/Plumix-Net/Plumix/actions/workflows/ci.yml)

**[plumix.net](https://plumix.net/)** · [NuGet packages](https://github.com/Plumix-Net/Plumix.Packages)

## Getting started

```bash
dotnet new install Plumix.Templates
dotnet new plumix-app -n HelloPlumix
cd HelloPlumix
dotnet run --project HelloPlumix.Desktop
```

`plumix-app` creates a shared widget project and one thin host project per platform. `--android false`,
`--ios false` or `--desktop false` leave a platform out; `--application-id` sets the Android application id and
iOS bundle id.

## Platforms

The framework owns layout, paint, gestures, semantics and focus; a host only provides the surface, input and the
platform's services, the split Flutter has between framework and engine.

| Platform | Host package | Host |
| --- | --- | --- |
| Android 7.0+ | `Plumix.Android` | Plumix's native host on Impeller (`PlumixActivity`): ports of the Flutter engine's Android embedding |
| iOS 15+ | `Plumix.iOS` | Plumix's native host on Impeller and Metal (`PlumixViewController`): ports of the Flutter engine's iOS embedding |
| Windows, macOS, Linux | `Plumix.Avalonia` | Avalonia (`PlumixApplication`) with Skia |
| Browser (WebAssembly) | `Plumix.Avalonia` | Avalonia.Browser |

All Plumix packages share one version.

## Vision

- Keep `Widget`/`Element`/`RenderObject` architecture as close as practical to Flutter.
- Make rewriting controls from Flutter (Dart) to C# straightforward, with minimal conceptual translation.
- Keep hosts thin: window, input, lifecycle and the drawing backend; layout and paint stay in the framework's render
  layer.

## Definition of Done

1. App UI is built with Flutter-like widgets and lifecycle primitives (`StatefulWidget`, `State`, `SetState`, reconciliation).
2. Render/layout/paint behavior is framework-owned (`RenderObject`/`RenderBox`/render pipeline), not host-control-driven UI logic.
3. Samples demonstrate real framework usage via widget host flow, not only low-level render demos.
4. Core primitives are stable and close enough to Flutter semantics for practical Dart-to-C# control porting.

## Project Tracking

- Changelog: [`CHANGELOG.md`](CHANGELOG.md)
- Global implementation status and roadmap: [`docs/FRAMEWORK_PLAN.md`](docs/FRAMEWORK_PLAN.md)
- AI-oriented context map and workflows: [`docs/ai/MODULE_INDEX.md`](docs/ai/MODULE_INDEX.md)
- Additional packages: [Plumix.Packages](https://github.com/Plumix-Net/Plumix.Packages)

## Example

### C#

```csharp
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace MyApp;

public sealed class MyApp : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        return new MaterialApp(
            home: new Scaffold(
                body: new Center(
                    child: new Column(
                        mainAxisAlignment: MainAxisAlignment.Center,
                        children:
                        [
                            new Text(
                                "Hello, Plumix!",
                                style: new TextStyle(FontSize: 32, FontWeight: FontWeight.Bold)
                            ),
                            new SizedBox(height: 16),
                            new Text("Flutter's widget model, written in C#."),
                            new SizedBox(height: 24),
                            new ElevatedButton(
                                onPressed: () => { /* handle tap */ },
                                child: new Text("Get Started")
                            )
                        ]
                    )
                )
            )
        );
    }
}
```

### F#

Plumix also ships first-class F# support: `Plumix.FSharp` provides Feliz-style `Ui.*` widget factories, and `Plumix.Elmish` hosts a standard [Elmish](https://elmish.github.io/elmish/) (MVU) program as a widget — Plumix's own element reconciliation does the diffing, so there is no extra virtual-DOM layer.

```fsharp
open Elmish
open Plumix.Elmish
open Plumix.FSharp

type Model = { Count: int }
type Msg = Increment

let init () = { Count = 0 }, Cmd.none

let update msg model =
    match msg with
    | Increment -> { model with Count = model.Count + 1 }, Cmd.none

let view model dispatch =
    Ui.scaffold (
        appBar = Ui.appBar (titleText = "Hello, Plumix!"),
        body =
            Ui.center (
                Ui.column (
                    mainAxisAlignment = MainAxisAlignment.Center,
                    spacing = 12.0,
                    children = [
                        Ui.text ("Hello, Plumix!", fontSize = 32.0, fontWeight = FontWeight.Bold)
                        Ui.text "Flutter's widget model, written in F#."
                        Ui.text (string model.Count, fontSize = 24.0)
                    ])),
        floatingActionButton =
            Ui.floatingActionButton (child = Ui.icon Icons.Add, onPressed = fun () -> dispatch Increment))

/// The MVU program as a plain Plumix widget — mount it anywhere in a widget tree.
let app () : Widget =
    Program.mkProgram init update view |> Program.toWidget
```

Prefer classic Flutter style? `StatefulWidget`/`SetState` work from F# too — see [`src/Sample/Plumix.FSharpSample`](src/Sample/Plumix.FSharpSample) for both variants side by side.

## Contributing

Plumix is developed AI-first: code contributions are expected to be produced with a frontier coding agent (Claude Opus 4.8, GPT-5.5, or newer). Bug reports and discussions are welcome from everyone. See [`CONTRIBUTING.md`](CONTRIBUTING.md) for the workflow, commands, and PR checklist.

Looking for something to work on? [`docs/CUPERTINO_TODO.md`](docs/CUPERTINO_TODO.md) lists the Cupertino widgets that still need porting — pick one, claim it, and send a PR.
