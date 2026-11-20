namespace Plumix.FSharp

// Re-exports of the commonly used Plumix types so that application
// code only needs `open Plumix.FSharp` (the underlying types are spread across
// Plumix.Widgets / Plumix.Rendering / Plumix.UI / Plumix.Foundation /
// Plumix.Material namespaces, which is invisible in C# but costs an
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

// Painting / geometry / text (the framework value types used across the widget API)
type Color = Plumix.UI.Color
type Offset = Plumix.UI.Offset
type Size = Plumix.UI.Size
type Rect = Plumix.UI.Rect
type Colors = Plumix.Material.Colors
type FontWeight = Plumix.UI.FontWeight
type FontStyle = Plumix.UI.FontStyle
type EdgeInsets = Plumix.Rendering.EdgeInsets
type EdgeInsetsGeometry = Plumix.Rendering.EdgeInsetsGeometry

// Material
type ThemeData = Plumix.Material.ThemeData
type Icons = Plumix.Material.Icons
type IconData = Plumix.Widgets.IconData
type AppBar = Plumix.Material.AppBar
