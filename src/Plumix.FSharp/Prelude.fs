namespace Plumix.FSharp

// Re-exports of the commonly used Plumix/Avalonia types so that application
// code only needs `open Plumix.FSharp` (the underlying types are spread across
// Plumix.Widgets / Plumix.Rendering / Plumix.UI / Plumix.Foundation /
// Plumix.Material / Avalonia namespaces, which is invisible in C# but costs an
// `open` each in F#).

// Framework primitives
type Widget = Plumix.Widgets.Widget
type StatelessWidget = Plumix.Widgets.StatelessWidget
type StatefulWidget = Plumix.Widgets.StatefulWidget
type State = Plumix.Widgets.State
type BuildContext = Plumix.Widgets.BuildContext
type Key = Plumix.Foundation.Key

// Layout
type MainAxisAlignment = Plumix.Rendering.MainAxisAlignment
type MainAxisSize = Plumix.Rendering.MainAxisSize
type CrossAxisAlignment = Plumix.Rendering.CrossAxisAlignment
type Alignment = Plumix.Rendering.Alignment
type StackFit = Plumix.Rendering.StackFit

// Text
type TextAlign = Plumix.UI.TextAlign
type TextOverflow = Plumix.UI.TextOverflow

// Painting / geometry (the framework colour and the Avalonia primitives used across the widget API)
type Color = Plumix.UI.Color
type Colors = Plumix.Material.Colors
type FontWeight = Avalonia.Media.FontWeight
type Thickness = Avalonia.Thickness

// Material
type ThemeData = Plumix.Material.ThemeData
type Icons = Plumix.Material.Icons
type IconData = Plumix.Widgets.IconData
type AppBar = Plumix.Material.AppBar
