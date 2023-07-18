module AppName

open System
open System.IO

open LcGitBup
open LcGitLib2.RepoTools

open ColorPrint
open CommonTools

type private NameCommand =
  | CmdShow
  | CmdSet of string
  | CmdDefault

type private NameOptions = {
  RepoPath: string
  Command: NameCommand
}
  

let runName args =
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
    | "-set" :: name :: rest ->
      rest |> parseMore {o with Command = CmdSet(name)}
    | "-default" :: rest ->
      rest |> parseMore {o with Command = CmdDefault}
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
      cp $"Using repo '\fg{repo.Label}\f0' (\fc{repo.RepoFolder}\f0)"
      let svc = new GitBupService()
      if svc.HasConfig |> not then
        cp $"Creating a minimal global gitbup configuration at \fb{svc.ConfigurationFile}\f0."
        svc.GetConfig() |> ignore
      let gbr = repo |> svc.GetRepo
      match o.Command with
      | CmdShow ->
        cp $"The repository name is \fg{gbr.RepoLabel}\f0."
        0
      | CmdDefault ->
        cp $"Setting the repository name to \fg{repo.Label}\f0."
        repo.Label |> gbr.ChangeLabel
        0
      | CmdSet(name) ->
        cp $"Setting the repository name to \fg{name}\f0."
        name |> gbr.ChangeLabel
        0
  | None ->
    Usage.usage "name"
    0

