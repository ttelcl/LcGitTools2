module AppRun

open System
open System.IO

open LcGitLib2.GitRunning
open LcGitLib2.RepoTools

open LcGitBup
open LcGitBup.BundleModel
open LcGitBup.Configuration

open ColorPrint
open CommonTools

type private RunOptions = {
  RepoPath: string
  Tier: int option
}

type private RunContext = {
  Options: RunOptions
  TargetFolder: string
  BupRepo: GitBupRepo
  Host: GitCommandHost
}

type private ExecuteContext = {
  Command: GitCommand
  Bundle: GitBupBundle
  CurrentMeta: BundleMetadata
  Bundles: BundleSet
}

let private bundleRun ectx =
  // actually execute GIT to create the bundle, and postprocess
  let cmd = ectx.Command
  let bundle = ectx.Bundle
  let meta = ectx.CurrentMeta
  let status = cmd.RunToConsole()
  if status = 0 then
    cp $"\fgSuccess\f0!"
    cp $"Saving metadata \fc{bundle.Folder}\f0{Path.DirectorySeparatorChar}\fy{bundle.MetaFileName}\f0."
    bundle.SaveMetadata(meta)
    ectx.Bundles.AddMissing() |> ignore
    let discards = ectx.Bundles.DiscardUnused()
    if discards.Count = 0 then
      cp "(no older bundles were discarded)"
    else
      cp "Discarding the following outdated bundles:"
      for discard in discards do
        cp $"  \fk{discard.BundleFileName}\f0."
  else
    cp $"\frError: status = {status}\f0."
  status

let private bundleFull context =
  let bundles = context.BupRepo.GetBundleInfo()
  let bundle = bundles.NextBundle(0)
  cp $"Creating new full-backup bundle \fc{bundle.Folder}\f0{Path.DirectorySeparatorChar}\fg{bundle.BundleFileName}\f0."
  let meta = context.BupRepo.GetCurrentRepoMetadata(context.Host)
  let cmd =
    context.Host.NewCommand()
      .WithFolder(context.BupRepo.Repository.RepoFolder)
      .WithCommand("bundle")
      .AddPost("create")
      .AddPost(bundle.FullBundleFileName)
      .AddPost("--all")
  bundleRun {
    Bundle = bundle
    Command = cmd
    CurrentMeta = meta
    Bundles = bundles
  }

let rec private bundleIncremental tier context =
  let bundles = context.BupRepo.GetBundleInfo()
  if bundles.TierStack.Depth < tier then
    let tierRequested = tier
    let tier = bundles.TierStack.Depth
    if tier = 0 then
      cp $"\foTier \fb{tierRequested}\fo is not available yet. \fyDowntiering to \fgtier 0\fy (full backup)\f0 ."
      context |> bundleFull
    else
      cp $"\foTier \fb{tierRequested}\fo is not available yet. \fyDowntiering to \fctier {tier}\f0."
      context |> bundleIncremental tier
  else
    let reference = bundles.TierStack.Tiers[tier-1]
    if reference.DoesExist() |> not then
      failwith $"Metadata file is missing: {reference.FullMetaFileName}"
    let refMeta = reference.ReadMetadata()
    if refMeta.GitBundleTips.Count > 64 then
      cp $"\foWARNING! The reference bundle \fy{reference.BundleFileName}\fy has too many tip commits to support incremental bundling\f0."
      cp $"\foFalling back to a full bundle\f0."
      context |> bundleFull
    else
      let bundle = bundles.NextBundle(tier)
      let meta = context.BupRepo.GetCurrentRepoMetadata(context.Host)
      let added, removed = meta.CompareToAncestor refMeta
      cp $"Using bundle \fC{reference.BundleFileName}\f0 as reference for incremental bundling."
      cp $"The set of tip commits has \fg{added.Count}\f0 additions and \fo{removed.Count}\f0 removals."
      if added.Count = 0 && removed.Count = 0 then
        // is it safe to abort before running git now?
        cp "\foAborting! \fyIt appears that the repository has not changed since the reference was bundled\f0."
        1
      else
        cp $"Creating new \fytier {bundle.Tier}\f0 bundle \fc{bundle.Folder}\f0{Path.DirectorySeparatorChar}\fg{bundle.BundleFileName}\f0."
        let cmd =
          context.Host.NewCommand()
            .WithFolder(context.BupRepo.Repository.RepoFolder)
            .WithCommand("bundle")
            .AddPost("create")
            .AddPost(bundle.FullBundleFileName)
            .AddPost("--all")
        for reftip in refMeta.GitBundleTips do
          let abbreviated = reftip.Substring(0,8)
          cmd.AddPost($"^{abbreviated}") |> ignore
        bundleRun {
          Bundle = bundle
          Command = cmd
          CurrentMeta = meta
          Bundles = bundles
        }


let runRun args =
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-h" :: _ ->
      None
    | [] ->
      Some(o)
    | "-C" :: path :: rest ->
      rest |> parseMore {o with RepoPath = path}
    | "-tier" :: "auto" :: rest ->
      rest |> parseMore {o with Tier = None}
    | "-tier" :: tier :: rest ->
      let ok, n = tier |> Int32.TryParse
      if ok |> not || n < 0 || n > 9 then
        failwith $"Expecting the argument to '-tier' to be 'auto' or a single digit"
      rest |> parseMore {o with Tier = Some(n)}
    | "-full" :: rest ->
      rest |> parseMore {o with Tier = Some(0)}
    | "-auto" :: rest ->
      rest |> parseMore {o with Tier = None}
    | x :: rest when x.Length = 3 && x.StartsWith("-t") && x[2]>='0' && x[2]<='9' ->
      let n = int(x[2]-'0');
      rest |> parseMore {o with Tier = Some(n)}
    | x :: _ ->
      failwith $"Unrecognized argument '{x}'"
  let oo = args |> parseMore {
    RepoPath = null
    Tier = None
  }
  match oo with
  | Some(o) ->
    let repoAnchor = if o.RepoPath |> String.IsNullOrEmpty then Environment.CurrentDirectory else o.RepoPath
    let repo = GitRepository.Locate(repoAnchor, false)
    if repo = null then
      cp $"'\fc{repoAnchor}\f0' \fris not part of a git repository\f0."
      1
    else
      let svc = new GitBupService()
      let repocfg = repo |> svc.GetRepo
      let hasTarget, target = repocfg.TryGetTarget()
      if hasTarget then
        let host = GitCmdLogging.makeDefaultHost true
        let context = {
          Options = o
          TargetFolder = target
          BupRepo = repocfg
          Host = host
        }
        match o.Tier with
        | None ->
          failwith "NYI - 'auto'"
          0
        | Some(0) ->
          context |> bundleFull
        | Some(tier) ->
          context |> bundleIncremental tier
      else
        if target |> String.IsNullOrEmpty then
          cp $"\frNo bundle target folder has been configured yet for repository \fo{repo.RepoFolder}\f0."
        else
          cp $"\frThe configured bundle target folder \fo{target}\f0 does not exist."
        1
  | None ->
    Usage.usage "run"
    1
