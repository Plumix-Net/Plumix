namespace Plumix.FSharp

open System
open System.Collections.Generic
open Avalonia
open Avalonia.Media
open Plumix.Foundation
open Plumix.Material
open Plumix.Painting
open Plumix.Rendering
open Plumix.UI
open Plumix.Widgets

module private Interop =

    let inline nul (o: 'a option) : Nullable<'a> = Option.toNullable o

    let inline obj' o = Option.toObj o

    let edgeInsets (o: Thickness option) : Nullable<EdgeInsetsGeometry> =
        match o with
        | Some value ->
            Nullable(
                EdgeInsetsGeometry.FromLTRB(
                    value.Left,
                    value.Top,
                    value.Right,
                    value.Bottom))
        | None -> Nullable()

    let action (f: (unit -> unit) option) : Action =
        match f with
        | Some f -> Action(f)
        | None -> null

    let widgets (ws: Widget seq) : IReadOnlyList<Widget> = upcast Seq.toArray ws

    let widgetsOpt (ws: Widget seq option) : IReadOnlyList<Widget> =
        match ws with
        | Some ws -> widgets ws
        | None -> null

open Interop

/// Factory functions over the Plumix and Plumix.Material widget sets.
/// Every member returns `Widget` (except `Ui.appBar`, which returns the
/// `AppBar` type required by `Ui.scaffold`), so results compose without
/// upcasts and F# lists work everywhere a child list is expected.
[<AbstractClass; Sealed>]
type Ui private () =

    // ---- Core widgets ----

    static member text
        (
            data: string,
            ?fontSize: float,
            ?color: Color,
            ?fontWeight: FontWeight,
            ?textAlign: TextAlign,
            ?maxLines: int,
            ?overflow: TextOverflow,
            ?key: Key
        ) : Widget =
        Text(
            data,
            fontSize = nul fontSize,
            color = nul color,
            fontWeight = nul fontWeight,
            textAlign = defaultArg textAlign TextAlign.Start,
            maxLines = nul maxLines,
            overflow = defaultArg overflow TextOverflow.Clip,
            key = obj' key)

    static member icon(icon: IconData, ?size: float, ?color: Color, ?key: Key) : Widget =
        Icon(icon, size = nul size, color = nul color, key = obj' key)

    static member column
        (
            children: Widget seq,
            ?mainAxisAlignment: MainAxisAlignment,
            ?crossAxisAlignment: CrossAxisAlignment,
            ?mainAxisSize: MainAxisSize,
            ?spacing: float,
            ?key: Key
        ) : Widget =
        Column(
            children = widgets children,
            mainAxisAlignment = defaultArg mainAxisAlignment MainAxisAlignment.Start,
            crossAxisAlignment = defaultArg crossAxisAlignment CrossAxisAlignment.Center,
            mainAxisSize = defaultArg mainAxisSize MainAxisSize.Max,
            spacing = defaultArg spacing 0.0,
            key = obj' key)

    static member row
        (
            children: Widget seq,
            ?mainAxisAlignment: MainAxisAlignment,
            ?crossAxisAlignment: CrossAxisAlignment,
            ?mainAxisSize: MainAxisSize,
            ?spacing: float,
            ?key: Key
        ) : Widget =
        Row(
            children = widgets children,
            mainAxisAlignment = defaultArg mainAxisAlignment MainAxisAlignment.Start,
            crossAxisAlignment = defaultArg crossAxisAlignment CrossAxisAlignment.Center,
            mainAxisSize = defaultArg mainAxisSize MainAxisSize.Max,
            spacing = defaultArg spacing 0.0,
            key = obj' key)

    static member stack(children: Widget seq, ?alignment: AlignmentGeometry, ?fit: StackFit, ?key: Key) : Widget =
        Stack(
            children = widgets children,
            alignment = defaultArg alignment (AlignmentGeometry()),
            fit = defaultArg fit StackFit.Loose,
            key = obj' key)

    static member center(child: Widget, ?widthFactor: float, ?heightFactor: float, ?key: Key) : Widget =
        Center(child, widthFactor = nul widthFactor, heightFactor = nul heightFactor, key = obj' key)

    static member align
        (
            child: Widget,
            ?alignment: AlignmentGeometry,
            ?widthFactor: float,
            ?heightFactor: float,
            ?key: Key
        ) : Widget =
        Align(
            child,
            alignment = defaultArg alignment (AlignmentGeometry()),
            widthFactor = nul widthFactor,
            heightFactor = nul heightFactor,
            key = obj' key)

    static member padding(insets: Thickness, child: Widget, ?key: Key) : Widget =
        Padding(insets, child, key = obj' key)

    static member padding(all: float, child: Widget, ?key: Key) : Widget =
        Padding(Thickness(all), child, key = obj' key)

    static member sizedBox(?width: float, ?height: float, ?child: Widget, ?key: Key) : Widget =
        SizedBox(width = nul width, height = nul height, child = obj' child, key = obj' key)

    static member expanded(child: Widget, ?flex: int, ?key: Key) : Widget =
        Expanded(child, flex = defaultArg flex 1, key = obj' key)

    static member spacer(?flex: int, ?key: Key) : Widget =
        Spacer(flex = defaultArg flex 1, key = obj' key)

    static member container
        (
            ?child: Widget,
            ?color: Color,
            ?padding: Thickness,
            ?margin: Thickness,
            ?width: float,
            ?height: float,
            ?alignment: AlignmentGeometry,
            ?decoration: BoxDecoration,
            ?key: Key
        ) : Widget =
        Container(
            child = obj' child,
            color = nul color,
            padding = edgeInsets padding,
            margin = edgeInsets margin,
            width = nul width,
            height = nul height,
            alignment = nul alignment,
            decoration = obj' decoration,
            key = obj' key)

    // ---- Material widgets ----

    static member scaffold
        (
            body: Widget,
            ?appBar: AppBar,
            ?floatingActionButton: Widget,
            ?bottomNavigationBar: Widget,
            ?drawer: Widget,
            ?backgroundColor: Color,
            ?key: Key
        ) : Widget =
        Scaffold(
            body,
            appBar = obj' appBar,
            floatingActionButton = obj' floatingActionButton,
            bottomNavigationBar = obj' bottomNavigationBar,
            drawer = obj' drawer,
            backgroundColor = nul backgroundColor,
            key = obj' key)

    static member appBar
        (
            ?title: Widget,
            ?leading: Widget,
            ?actions: Widget seq,
            ?centerTitle: bool,
            ?key: Key
        ) : AppBar =
        AppBar(
            title = obj' title,
            leading = obj' leading,
            actions = widgetsOpt actions,
            centerTitle = nul centerTitle,
            key = obj' key)

    static member elevatedButton
        (
            child: Widget,
            ?onPressed: unit -> unit,
            ?foregroundColor: Color,
            ?backgroundColor: Color,
            ?key: Key
        ) : Widget =
        ElevatedButton(
            child,
            action onPressed,
            style =
                ElevatedButton.StyleFrom(
                    foregroundColor = nul foregroundColor,
                    backgroundColor = nul backgroundColor),
            key = obj' key)

    static member textButton
        (
            child: Widget,
            ?onPressed: unit -> unit,
            ?foregroundColor: Color,
            ?backgroundColor: Color,
            ?key: Key
        ) : Widget =
        TextButton(
            child,
            action onPressed,
            style =
                TextButton.StyleFrom(
                    foregroundColor = nul foregroundColor,
                    backgroundColor = nul backgroundColor),
            key = obj' key)

    static member outlinedButton
        (
            child: Widget,
            ?onPressed: unit -> unit,
            ?foregroundColor: Color,
            ?borderColor: Color,
            ?key: Key
        ) : Widget =
        OutlinedButton(
            child,
            action onPressed,
            style =
                OutlinedButton.StyleFrom(
                    foregroundColor = nul foregroundColor,
                    side = (match borderColor with
                            | Some color -> Nullable(BorderSide(color))
                            | None -> Nullable())),
            key = obj' key)

    static member floatingActionButton
        (
            ?child: Widget,
            ?onPressed: unit -> unit,
            ?tooltip: string,
            ?foregroundColor: Color,
            ?backgroundColor: Color,
            ?key: Key
        ) : Widget =
        FloatingActionButton(
            obj' child,
            action onPressed,
            tooltip = obj' tooltip,
            foregroundColor = nul foregroundColor,
            backgroundColor = nul backgroundColor,
            key = obj' key)

    static member theme(data: ThemeData, child: Widget, ?key: Key) : Widget =
        Theme(data, child, key = obj' key)

    static member scaffoldMessenger(child: Widget, ?key: Key) : Widget =
        ScaffoldMessenger(child, key = obj' key)
