<p align="center">
  <img src="icon.png" alt="Plumix" width="120" />
</p>

# Plumix

Flutter-inspired UI framework for .NET — build declarative, widget-based UIs in C# with Flutter's `Widget`/`Element`/`RenderObject` architecture.

[![Website](https://img.shields.io/badge/website-plumix.net-blue)](https://plumix.net/)
[![Plumix](https://img.shields.io/nuget/v/Plumix?label=Plumix&logo=nuget)](https://www.nuget.org/packages/Plumix/)
[![Plumix.Material](https://img.shields.io/nuget/v/Plumix.Material?label=Plumix.Material&logo=nuget)](https://www.nuget.org/packages/Plumix.Material/)
[![Plumix.Cupertino](https://img.shields.io/nuget/v/Plumix.Cupertino?label=Plumix.Cupertino&logo=nuget)](https://www.nuget.org/packages/Plumix.Cupertino/)
[![CI](https://github.com/Plumix-Net/Plumix/actions/workflows/ci.yml/badge.svg)](https://github.com/Plumix-Net/Plumix/actions/workflows/ci.yml)

**[plumix.net](https://plumix.net/)** · [NuGet packages](https://github.com/Plumix-Net/Plumix.Packages)

## Vision

- Keep `Widget`/`Element`/`RenderObject` architecture as close as practical to Flutter.
- Make rewriting controls from Flutter (Dart) to C# straightforward, with minimal conceptual translation.
- Reuse Avalonia mostly as platform infrastructure: app/window host, lifecycle, input plumbing, and drawing backend abstractions.
- Keep layout and paint behavior inside this framework's render layer.

## Definition of Done

1. App UI is built with Flutter-like widgets and lifecycle primitives (`StatefulWidget`, `State`, `SetState`, reconciliation).
2. Render/layout/paint behavior is framework-owned (`RenderObject`/`RenderBox`/render pipeline), not Avalonia-control-driven UI logic.
3. Samples demonstrate real framework usage via widget host flow, not only low-level render demos.
4. Core primitives are stable and close enough to Flutter semantics for practical Dart-to-C# control porting.

## Project Tracking

- Changelog: [`CHANGELOG.md`](CHANGELOG.md)
- Global implementation status and roadmap: [`docs/FRAMEWORK_PLAN.md`](docs/FRAMEWORK_PLAN.md)
- AI-oriented context map and workflows: [`docs/ai/MODULE_INDEX.md`](docs/ai/MODULE_INDEX.md)
- Additional packages: [Plumix.Packages](https://github.com/Plumix-Net/Plumix.Packages)

## Example

```csharp
using Avalonia.Media;
using Plumix.Widgets;

namespace MyApp;

public sealed class MyApp : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        return new Scaffold(
            body: new Center(
                child: new Column(
                    mainAxisAlignment: MainAxisAlignment.Center,
                    children:
                    [
                        new Text(
                            "Hello, Plumix!",
                            style: new TextStyle(fontSize: 32, fontWeight: FontWeight.Bold)
                        ),
                        new SizedBox(height: 16),
                        new Text("Flutter-like widgets, powered by .NET and Avalonia."),
                        new SizedBox(height: 24),
                        new ElevatedButton(
                            onPressed: () => { /* handle tap */ },
                            child: new Text("Get Started")
                        )
                    ]
                )
            )
        );
    }
}
```
