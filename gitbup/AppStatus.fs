module AppStatus

open System
open System.Globalization
open System.IO

open Newtonsoft.Json

open LcGitLib2.RepoTools
open LcGitLib2.GitRunning

open LcGitBup
open LcGitBup.BundleModel
open LcGitBup.Configuration

open ColorPrint
open CommonTools

type private StatusCommand =
  | Show

type private MetaOptions = {
  RepoPath: string
  Command: StatusCommand
}

type private BundleInfo = {
  Bundle: GitBupBundle
  LocalStamp: DateTimeOffset
  Meta: BundleMetadata
  BundleSize: int64
}

let private collectBundleInfo (bundle: GitBupBundle) =
  let meta = bundle.ReadMetadata()
  let stamp =
    DateTimeOffset.ParseExact(
      bundle.Id,
      "yyyyMMdd-HHmmss",
      CultureInfo.InvariantCulture,
      DateTimeStyles.AdjustToUniversal ||| DateTimeStyles.AssumeUniversal).ToLocalTime()
  let fi = new FileInfo(bundle.FullBundleFileName)
  {
    Bundle = bundle
    LocalStamp = stamp
    Meta = meta
    BundleSize = fi.Length
  }

let private commits bi =
  bi.Meta.GitCommitCount

let private tips bi =
  bi.Meta.GitBundleTips.Count

let private bundleTime bi =
  bi.LocalStamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)

let runStatus args =
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
    | "-show" :: rest ->
      rest |> parseMore {o with Command = StatusCommand.Show}
    | x :: _ ->
      failwith $"Unrecognized argument '{x}'"
  let oo = args |> parseMore {
    RepoPath = null
    Command = StatusCommand.Show
  }
  match oo with
  | Some(o) ->
    let svc = new GitBupService()
    let repoAnchor = if o.RepoPath |> String.IsNullOrEmpty then Environment.CurrentDirectory else o.RepoPath
    let repo = GitRepository.Locate(repoAnchor, false)
    if repo = null then
      cp "\fyRepository\f0 gitbup configuration:"
      cp $"  \fyNo repository found at \fo{repoAnchor}\fy (nor its parents)\f0."
    else
      let repocfg = repo |> svc.GetRepo
      cp $"\fyRepository\f0 gitbup configuration for '\fc{repo.RepoFolder}\f0' (\fk{repocfg.RepoConfigName}\f0)"
      if repocfg.HasConfig then
        cp $"  Repository name:  \fg{repocfg.RepoLabel}\f0."
        let ok, target = repocfg.TryGetTarget()
        if ok then
          let exists = target |> Directory.Exists
          if exists then
            cp $"  Bundle directory: \fc{target}\f0."
            let bundles = repocfg.GetBundleInfo()
            if bundles.AllBundles.Count = 0 then
              cp "  \foNo backup bundles have been created yet\f0."
            else
              let tiers = bundles.TierStack
              if tiers.Tiers.Count = 0 then
                // Unusual case; may happen after changing the repo label but not the repo folder
                cp $"  \foNo bundles have been created yet \f0(for the tag '\fy{bundles.Prefix}\f0')."
              else
                cp $"  There are currently \fo{tiers.Tiers.Count}\f0 bundle tiers:"
                let bundles = tiers.Tiers |> Seq.toArray
                let bundleInfos = bundles |> Array.map collectBundleInfo
                let bundle = bundles[0]
                let bi = bundleInfos[0]
                // \fk{bundle.Prefix}\f0.
                cpx $"  t0: \fb{bundle.Id}\f0, \fy{bi |> bundleTime}\f0, \fo%8d{bi.BundleSize}\f0 bytes,"
                cp $" \fg{bi |> tips}\f0 tips, \fc%d{bi |> commits}\f0 commits"
                for i in 1..bundles.Length-1 do
                  let bundle = bundles[i]
                  let bi = bundleInfos[i]
                  let bi0 = bundleInfos[i-1]
                  let deltaCommit = commits(bi) - commits(bi0)
                  // \fk{bundle.Prefix}\f0.
                  cpx $"  t{i}: \fg{bundle.Id}\f0, \fy{bi |> bundleTime}\f0, \fo%8d{bi.BundleSize}\f0 bytes,"
                  cp $" \fg{bi |> tips}\f0 tips, \fc%d{bi |> commits}\f0 (\fb+{deltaCommit}\f0) commits"
                  ()
          else
            cp $"  Bundle directory: \fo{target}\f0 (\frdirectory does not exist\f0)."
        else
          cp $"  Bundle directory: \frNone Configured\f0."
      else
        cp "  \foThere is no gitbup configuration for this repository\f0."
    cp $"\fyGlobal\f0 gitbup configuration (\fk{svc.ConfigurationFile}\f0):"
    if svc.HasConfig then
      if svc.Anchors.Count > 0 then
        cp "  Available anchor folders:"
        for kvp in svc.Anchors do
          cp $"  \fg%12s{kvp.Key}\f0 = \fC{kvp.Value}\f0."
      else
        cp "  \foNo anchor folder defined\f0."
    else
      cp "  \foThere is no global gitbup configuration file on this system\f0."
    0
  | None ->
    Usage.usage "status"
    0
