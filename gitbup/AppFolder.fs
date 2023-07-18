module AppFolder

open System
open System.IO

open LcGitBup
open LcGitLib2.RepoTools

open ColorPrint
open CommonTools

type private FolderCommand =
  | CmdShow
  | CmdAnchor of string
  | CmdLocal
  | CmdNone

type private FolderOptions = {
  RepoPath: string
  Command: FolderCommand
}

let runFolder args =
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-h" :: _ ->
      None
    | "-C" :: path :: rest ->
      rest |> parseMore {o with RepoPath = path}
    | "-list" :: rest
    | "-show" :: rest ->
      rest |> parseMore {o with Command = CmdShow}
    | "-anchor" :: tag :: rest ->
      rest |> parseMore {o with Command = CmdAnchor(tag)}
    | "-local" :: rest ->
      rest |> parseMore {o with Command = CmdLocal}
    | "-none" :: rest ->
      rest |> parseMore {o with Command = CmdNone}
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
    let repoAnchor = if o.RepoPath |> String.IsNullOrEmpty then Environment.CurrentDirectory else o.RepoPath
    let repo = GitRepository.Locate(repoAnchor, false)
    if repo = null then
      cp $"\frNo repository found\f0 at \fy{repoAnchor}\f0!"
      1
    else
      let svc = new GitBupService()
      if svc.HasConfig |> not then
        cp $"Creating a minimal global gitbup configuration at \fb{svc.ConfigurationFile}\f0."
        svc.GetConfig() |> ignore
      let gbr = repo |> svc.GetRepo
      cp $"Using repo '\fg{gbr.RepoLabel}\f0' (\fc{repo.RepoFolder}\f0)"
      match o.Command with
      | CmdShow ->
        let ok, folder = gbr.TryGetTarget()
        if ok then
          cp $"The target folder is \fc{folder}\f0 ."
          0
        else
          cp $"\foNo target folder configured for this repository\f0."
          1
      | CmdAnchor(anchorTag) ->
        let anchorFolder = svc.FindAnchor(anchorTag)
        if anchorFolder = null then
          cp $"Unknown anchor tag: \fr{anchorTag}\f0."
          if svc.Anchors.Count = 0 then
            cp "No anchors have been configured on this computer (use \fogitbup anchor\f0 to manage them)"
          else
            let tags = svc.Anchors |> Seq.map (fun kvp -> kvp.Key)
            let tagtext = "\fy" + String.Join("\f0, \fy", tags) + "\f0"
            cp $"Known anchor tags on this computer: {tagtext}"
          1
        else
          if anchorFolder |> Directory.Exists |> not then
            cp $"The folder for tag '\fo{anchorTag}\f0' (\fy{anchorFolder}\f0) no longer exists"
            1
          else
            let path = Path.Combine(anchorFolder, gbr.RepoLabel)
            cp $"Setting the target folder to \fc{path}\f0."
            if path |> Directory.Exists |> not then
              path |> Directory.CreateDirectory |> ignore
            path |> gbr.ChangeTargetFull
            0
      | CmdLocal ->
        let folder = gbr.ChangeTargetLocal()
        cp $"Setting the target folder to \fc{folder}\f0 ."
        0
      | CmdNone ->
        gbr.ChangeTargetNone()
        cp "Changed this repository to \fbnot have a target folder\f0."
        0
  | None ->
    Usage.usage "name"
    0

