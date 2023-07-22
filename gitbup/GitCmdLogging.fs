module GitCmdLogging

open System
open System.Diagnostics
open System.IO

open Newtonsoft.Json

open LcGitLib2.GitRunning

open CommonTools

// let mutable bequiet = false

let private printArguments shortGit (psi:ProcessStartInfo) =
  if verbose then
    let gitName =
      if shortGit then
        Path.GetFileName(psi.FileName)
      else
        psi.FileName
    bcolor Color.DarkBlue
    eprintf " > "
    color Color.DarkYellow
    eprintf "%s" gitName
    let mutable phase = true
    for gitArg in psi.ArgumentList do
      if phase then
        color Color.Green
      else
        color Color.Yellow
      phase <- not phase
      eprintf " %s" gitArg
    resetColor()
    eprintfn ""
  ()

let makeLogger shortGit =
  let gal = new GitArgLogger(fun psi -> printArguments shortGit psi)
  gal :> IGitArgLogger

/// Return a new GitCommandHost. If the verbose flag is set,
/// it prints the git command to stderr. The shortGit flag
/// determines if those messages to stderr use the full or the
/// abbreviated name for the git executable.
let makeDefaultHost shortGit =
  let logger = shortGit |> makeLogger
  new GitCommandHost(logger, null)
