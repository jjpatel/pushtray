open Mono.Unix
open Mono.Unix.Native
open Gtk
open Pushtray
open Pushtray.Cli

let private exitOnSignal (signum: Signum) =
  Async.Start <|
    async {
      if (new UnixSignal(signum)).WaitOne() then
        Logger.info <| sprintf "Received %s, exiting..." (System.Enum.GetName(typeof<Signum>, signum))
        Application.Quit()
        exit 1
    }

let private connect() =
  Pushbullet.Stream.connect()
  Application.Init()

  if not <| argExists "--no-tray-icon" then
    TrayIcon.create <| Cli.argWithDefault "--icon-style" "light"

  // Ctrl-c doesn't seem to do anything after Application.Run() is called
  // so we'll handle SIGINT explicitly
  exitOnSignal Signum.SIGINT

  Application.Run()

let private sms() =
  Sms.send
    (argAsString "--device")
    (requiredArg "<number>")
    (requiredArg "<message>")

let private list() =
  if argExists "devices" then
    Pushbullet.devices |> Array.iter (fun d ->
      printfn "%s (%s %s)" d.Nickname d.Manufacturer d.Model)

let private printHelp() =
  printfn "%s" usageWithOptions

[<EntryPoint>]
let main argv =
  command "connect" connect
  command "sms" sms
  command "list" list
  commands [ "-h"; "--help" ] printHelp
  0
