open Gtk
open Pushtray
open Pushtray.TrayIcon
open Pushtray.Cli

let private connect() =
  Application.Init()
  let trayIcon =
    if not args.Options.NoTrayIcon then Some <| TrayIcon(args.Options.IconStyle)
    else None
  Async.Start <|
    async {
      Pushbullet.Stream.connect trayIcon args.Options
    }
  Application.Run()

let private sms() =
  Sms.send
    (Pushbullet.requestAccountData args.Options)
    args.Options.Device
    (required args.Positional.Number)
    (required args.Positional.Message)

let private list() =
  command "devices" <| fun () ->
    (Pushbullet.requestAccountData args.Options).Devices
    |> Array.iter (fun d ->
      printfn "%s (%s %s)" d.Nickname d.Manufacturer d.Model)

let private help() =
  printfn "%s" usageWithOptions

[<EntryPoint>]
let main argv =
  Logger.setMinLogLevel Cli.args.Options.Log
  command "connect" connect
  command "sms" sms
  command "list" list
  commands [ "-h"; "--help" ] help
  0
