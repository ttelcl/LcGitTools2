module AppMetadata

open System
open System.Globalization
open System.IO

open Newtonsoft.Json

open LcGitLib2.RepoTools
open LcGitLib2.GitRunning

open LcGitBup
open LcGitBup.BundleModel

open ColorPrint
open CommonTools

type private MetaCommand =
  | Show
  | SaveAs of string
  | SaveAuto

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
    | "-saveas" :: file :: rest
    | "-save-as" :: file :: rest ->
      rest |> parseMore {o with Command = MetaCommand.SaveAs(file)}
    | "-save" :: rest
    | "-save-auto" :: rest ->
      rest |> parseMore {o with Command = MetaCommand.SaveAuto}
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

    let saveTo fileName =
      let tipNames = tips |> Array.map (fun cin -> cin.Id.Id)
      let rootNames = roots |> Array.map (fun cin -> cin.Id.Id)
      let bundleMeta = new BundleMetadata(tipNames, rootNames, commits.Length, pruned.Length);
      let json = JsonConvert.SerializeObject(bundleMeta, Formatting.Indented);
      let file = Path.GetFullPath(fileName)
      cp $"Saving \fg{file}\f0."
      File.WriteAllText(file, json);
      
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
    | MetaCommand.SaveAs(file) ->
      file |> saveTo
      0
    | MetaCommand.SaveAuto ->
      let svc = new GitBupService()
      let repoPath = if o.RepoPath |> String.IsNullOrEmpty then Environment.CurrentDirectory else o.RepoPath
      let repo = GitRepository.Locate(repoPath, true)
      let gbr = repo |> svc.GetRepo
      if gbr.HasTarget |> not then
        cp $"\foNo target folder has been set for that repository yet\f0. \fyUse \fg-save-as\fy instead\f0."
        1
      else
        let ok, target = gbr.TryGetTarget()
        if ok |> not then
          cp $"\foNo target folder has been set for that repository yet\f0. \fyUse \fg-save-as\fy instead\f0."
          1
        else
          let stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)
          let shortName = $"{gbr.RepoLabel}.{stamp}.meta.ignore.json"
          let fileName = Path.Combine(target, shortName)
          fileName |> saveTo
          0
  | None ->
    Usage.usage "metadata"
    0

