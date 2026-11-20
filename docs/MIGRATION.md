# Migrating from 1.0.0-preview.4

Updated 2026-11-21. Before 1.0 the public API of `Plumix`, `Plumix.Material` and `Plumix.Cupertino` stopped
naming Avalonia types, so the widget API can outlive the Avalonia host. Hosting moved to a new package,
`Plumix.Avalonia`. Most widget code compiles unchanged; this note lists what does not.

## Packages

- Add `Plumix.Avalonia` to the application project (desktop, browser, iOS). It holds `PlumixApplication`,
  `PlumixHost`, `WidgetHost` and `PlumixOptions`, still in the `Plumix` namespace. `Plumix.Android` references it.
- `PlumixExtensions.Run(widget, lifetime, options)` is now `PlumixApplication.Run(...)`.
  `PlumixExtensions.RunApp`/`RunWidget` are unchanged.

## Geometry

Avalonia's `Point`, `Vector`, `Size` and `Rect` are replaced by `Plumix.UI.Offset`, `Size` and `Rect`, value types with
Dart's members. Drop `using Avalonia;` where it only served geometry.

| Before | After |
|---|---|
| `Point`, `Vector` | `Offset` |
| `p.X`, `p.Y` | `p.Dx`, `p.Dy` |
| `rect.X`, `rect.Y`, `rect.Position` | `rect.Left`, `rect.Top`, `rect.TopLeft` |
| `new Rect(x, y, w, h)` | `Rect.FromLTWH(x, y, w, h)` |
| `new Rect(point, size)`, `new Rect(size)` | `point & size`, `Offset.Zero & size` |
| `DartGeometry.RectFromLTRB(...)` and other `DartGeometry` members | `Rect.FromLTRB(...)`, `Offset.Lerp(...)`, ... |
| `rect.Translate(v)`, `rect.Union(r)`, `rect.Intersects(r)` | `rect.Shift(v)`, `rect.ExpandToInclude(r)`, `rect.Overlaps(r)` |
| `vector.Length`, `vector.SquaredLength` | `offset.Distance`, `offset.DistanceSquared` |
| `size.AspectRatio` (Avalonia) / `DartAspectRatio` | `size.AspectRatio` (Dart rules for zero height) |
| `Size.Infinity` | `Size.Infinite` |

Behavior follows Dart where Avalonia differed: `Rect.Contains` excludes the right and bottom edges, `Intersect` of
disjoint rects has negative size instead of being empty, `Deflate` does not clamp at zero, and `==` treats NaN as
unequal to itself (`Equals` still matches, so dictionaries work).

## Text

- `FontWeight` and `FontStyle` are `Plumix.UI` types. `FontWeight.Normal`/`Bold` are unchanged; Avalonia's other
  names become Dart's: `Thin` -> `W100`, `Light` -> `W300`, `Medium` -> `W500`, `SemiBold` -> `W600`,
  `ExtraBold` -> `W800`, `Black` -> `W900`. `(int)weight` is `weight.Value`; `(FontWeight)700` is `new FontWeight(700)`.
- A font family is a `string` (`TextStyle.FontFamily`, `StrutStyle`, `ThemeData(fontFamily:)`, `TextTheme.Apply`).
- Register custom fonts with `FontLoader` instead of an `avares://` family:

```csharp
await new FontLoader("Inter")
    .AddFont(PlatformAssetBundle.Root.LoadBytesAsync("fonts/Inter-Regular.ttf"))
    .Load();
var style = new TextStyle(FontFamily: "Inter");
```

  A font in another assembly is addressed as `packages/<Assembly>/<path>`.

## Images

- `RasterImage` (dart:ui `Image`) replaces Avalonia `IImage`/`Bitmap` in `ImageInfo`, `FrameInfo`, `RawImage`,
  `RenderImage`, `Canvas.DrawImage*`, `paintImage`, `ToImage`/`ToImageSync`, `ImageTexture.SetImage` and inspector
  screenshots. It is a disposable handle: `Width`, `Height`, `Clone()`, `IsCloneOf()`, `ToByteData()`.
- `colorBlendMode` (image widgets) and `ColorFilter.Mode` take `BlendMode` (`BitmapBlendingMode.Multiply` ->
  `BlendMode.Modulate`, `SourceIn` -> `BlendMode.SourceIn`).

## Painting

- `Gradient.CreateShader`, `Paint.Shader`, `ShaderCallback` and shader masks use `Plumix.UI.Shader`; a shader-mask
  callback returns `new LinearGradient([...]).CreateShader(bounds)` instead of an Avalonia brush.
- Draw with `Paint` and `Path`: the `Canvas` overloads taking `IBrush`/`IPen`/`Geometry` are no longer public.
  `canvas.DrawRectangle(brush: new SolidColorBrush(c), ...)` becomes
  `canvas.DrawRRect(RRect.FromRectXY(rect, rx, ry), new Paint { Color = c })`.
- `NotchedShape.GetOuterPath` returns a `Path`; `GradientTransform.Transform` returns a `Matrix4`.
- `PaintUtilities.PaintZigZag` takes a `Paint` (use `Style = PaintingStyle.Stroke`).

## Interop with Avalonia

Mixing Plumix and Avalonia code? `using Plumix.Hosting;` brings `ToAvalonia()`/`ToPlumix()` for `Color`,
`EdgeInsets`, `Matrix4`, `Offset`, `Size` and `Rect`, and `picture.Playback(drawingContext)`. The implicit
conversions `Color` <-> `Avalonia.Media.Color` and `Thickness` -> `EdgeInsets` are gone.
