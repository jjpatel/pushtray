module Pushnix.Notification

open System
open Gdk
open Notifications
open Pushnix.Utils

type NotificationData =
  { Summary: NotificationText
    Body: NotificationText
    DeviceInfo: string option
    Timestamp: string option
    Icon: Icon
    Actions: Action[]
    Dismissible: bool }

and NotificationText =
  | Text of string
  | TextWithFormat of (string * (string -> string))

and Icon =
  | Stock of string
  | Base64 of string
  | File of string

and Action =
  { Label: string
    Handler: (ActionArgs -> unit) }

type Format =
  | Full
  | Short

let private format =
  Cli.arg "--notify-format"
  |> Option.map (fun s ->
    match s.ToLower() with
    | "full" -> Full
    | "short" -> Short
    | _ -> Short)
  |> Option.fold (fun _ v -> v) Short

let private lineWrapWidth = int <| defaultArg (Cli.arg "--notify-wrap") "40"
let private padWidth = int <| defaultArg (Cli.arg "--notify-padding") "42"
let private leftPad = "  "

let private wrap width line =
  let rec loop remaining result words =
    match words with
    | head :: tail ->
      // TODO: Fix HTML tags being included as words
      let (acc, remain) =
        if String.length head > remaining then (sprintf "%s\n" head, width)
        else (head + " ", remaining - head.Length)
      loop remain (result + acc) tail
    | _ -> result
  String.split [|' '|] line |> (List.ofArray >> loop width "")

let private pad width line =
  (if String.length line < width then
    line + (String.replicate (width - line.Length) " ")
  else
    line)
  |> sprintf "%s%s" leftPad

let private prettify str =
  str
  |> String.split [|'\n'|]
  |> Array.collect (wrap lineWrapWidth >> String.split [|'\n'|])
  |> Array.map (pad padWidth)
  |> String.concat "\n"

let send data =
  let footer =
    match format with
    | Full ->
      sprintf "%s %s"
        (defaultArg data.DeviceInfo "")
        (defaultArg data.Timestamp "")
      |> prettify
      |> sprintf "\n<i>%s</i>"
    | Short -> ""

  let formatText = function
    | Text(str) -> prettify str
    | TextWithFormat(str, format) -> format <| prettify str
  let summary = formatText data.Summary
  let body = formatText data.Body + footer

  Gtk.Application.Invoke(fun _ _ ->
    let notification =
      match data.Icon with
      | Stock(str) -> new Notification(summary, body, str)
      | Base64(str) -> new Notification(summary, body, new Pixbuf(Convert.FromBase64String(str)))
      | File(path) -> new Notification(summary, body, new Pixbuf(path))

    [| { Label = "Dismiss"; Handler = fun _ -> notification.Close() } |]
    |> Array.append data.Actions
    |> Array.iter (fun a ->
        notification.AddAction(a.Label, a.Label, fun _ args -> a.Handler args))

    notification.Show())
