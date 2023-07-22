module AppAnchor

open System
open System.IO

open LcGitBup
open LcGitLib2.RepoTools

open ColorPrint
open CommonTools

type private AnchorCommand =
  | CmdShow
  | CmdSet of string * string 

type private AnchorOptions = {
  RepoPath: string
  Command: AnchorCommand
}

let private runSetAnchor tag folder =
  let svc = new GitBupService()
  cp $"Configuration file: \fk{svc.ConfigurationFile}\f0 "
  let folder = folder |> Path.GetFullPath
  if folder |> Directory.Exists |> not then
    failwith $"The directory does not exist: '{folder}'"
  let old = tag |> svc.FindAnchor
  if old <> null then
    cp $"\froverwriting previous tag value \fy{old}\f0"
  svc.SetAnchor(tag, folder)
  cp $"\fg%15s{tag}\f0 -> \fc%s{folder}\f0."
  0

let private runListAnchor () =
  let svc = new GitBupService()
  cp $"Configuration file: \fk{svc.ConfigurationFile}\f0 "
  if svc.HasConfig |> not then
    cp "\fogitbup is not yet configured on this computer\f0. \fgSet an anchor folder to get started\f0."
    1
  else
    let cfg = svc.GetConfig()
    if cfg.AnchorFolders.Count = 0 then
      cp "\foThere are no anchor folders defined yet\f0. \fgSet an anchor folder to get started\f0."
      1
    else
      for kvp in cfg.AnchorFolders do
        let targetExists = Directory.Exists(kvp.Value)
        let folderColor = if targetExists then "\fc" else "\fr"
        cp $"\fg%15s{kvp.Key}\f0 -> {folderColor}%s{kvp.Value}\f0."
      0

let runAnchor args =
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-h" :: _ ->
      None
    | "-C" :: path :: rest ->
      // parse it even though "anchor" doesn't use it
      rest |> parseMore {o with RepoPath = path}
    | "-list" :: rest
    | "-show" :: rest ->
      rest |> parseMore {o with Command = CmdShow}
    | "-set" :: tag :: folder :: rest ->
      rest |> parseMore {o with Command = CmdSet(tag, folder)}
    | [] ->
      Some(o)
    | x :: _ ->
      failwith $"Unrecognized argument: '{x}'"
  let oo = args |> parseMore {
    RepoPath = null
    Command = CmdShow
  }
  match oo with
  | Some(o) ->
    match o.Command with
    | CmdShow ->
      runListAnchor()
    | CmdSet(tag, folder) ->
      runSetAnchor tag folder
  | None ->
    Usage.usage "anchor"
    0

