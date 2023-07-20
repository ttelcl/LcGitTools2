module AppMetadata

open System
open System.IO

open LcGitLib2.RepoTools
open LcGitLib2.GitRunning

open LcGitBup.BundleModel

open ColorPrint
open CommonTools
open Newtonsoft.Json

type private MetaCommand =
  | Show
  | Save of string

type private MetaOptions = {
  RepoPath: string
  Command: MetaCommand
}

let runMetadata args =
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-h" :: _ ->
      None
    | "-C" :: path :: rest ->
      rest |> parseMore {o with RepoPath = path}
    | [] ->
      Some(o)
    | "-show" :: rest 
    | "-view" :: rest ->
      rest |> parseMore {o with Command = MetaCommand.Show}
    | "-save" :: file :: rest ->
      rest |> parseMore {o with Command = MetaCommand.Save(file)}
    | x :: _ ->
      failwith $"Unrecognized argument '{x}'"
  let oo = args |> parseMore {
    RepoPath = null
    Command = MetaCommand.Show
  }
  match oo with
  | Some(o) ->
    let host = GitCmdLogging.makeDefaultHost true
    let commitMap = host.LoadCommitMap(o.RepoPath)
    let pruned = commitMap.PruneMissing() |> Seq.toArray
    if pruned.Length > 0 then
      cp $"\foWarning\f0! \fyThe repository seems incomplete: \fb{pruned.Length}\fy commits are referenced but not present\f0!"
    let tips = commitMap.Tips |> Seq.toArray
    let roots = commitMap.Roots |> Seq.toArray
    let commits = commitMap.Nodes |> Seq.toArray

    cp $"There are \fb{commits.Length}\f0 commits in this repository, of which \fb{tips.Length}\f0 are tips and \fb{roots.Length}\f0 are roots."
    if tips.Length >= 100 then
      cp $"\foWarning\f0! \fyThe number of tip commits in this repository is excessively high; incremental bundles may be unreliable\f0!"

    match o.Command with
    | MetaCommand.Show ->
      if tips.Length + roots.Length > 64 then
        cp "\fo Too many tips + roots\f0: \fyUse \fg-save\fy to save to a file instead\f0."
        1
      else
        cp $"\fb{tips.Length}\f0 tip commits:"
        for tip in tips do
          cp $"  \fy{tip.Id.Id}\f0."
        cp $"\fb{roots.Length}\f0 root commits:"
        for root in roots do
          cp $"  \fc{root.Id.Id}\f0."
        0
    | MetaCommand.Save(file) ->
      let file = Path.GetFullPath(file)
      cp $"Saving \fg{file}\f0."
      let tipNames = tips |> Array.map (fun cin -> cin.Id.Id)
      let rootNames = roots |> Array.map (fun cin -> cin.Id.Id)
      let bundleMeta = new BundleMetadata(tipNames, rootNames)
      let json = JsonConvert.SerializeObject(bundleMeta, Formatting.Indented);
      File.WriteAllText(file, json);
      0
  | None ->
    Usage.usage "metadata"
    0

