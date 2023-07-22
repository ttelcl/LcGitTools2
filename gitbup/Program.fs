// (c) 2023  ttelcl / ttelcl

open System

open CommonTools
open ExceptionTool
open Usage

let rec run arglist =
  match arglist with
  | "-v" :: rest ->
    verbose <- true
    rest |> run
  | "--help" :: _
  | "-help" :: _
  | "help" :: _
  | "-h" :: _
  | [] ->
    usage "all"
    0  // program return status code to the operating system; 0 == "OK"
  | "run" :: rest ->
    rest |> AppRun.runRun
  | "anchors" :: rest
  | "anchor" :: rest ->
    rest |> AppAnchor.runAnchor
  | "target" :: rest
  | "directory" :: rest
  | "folder" :: rest ->
    rest |> AppFolder.runFolder
  | "name" :: rest ->
    rest |> AppName.runName
  | "meta" :: rest
  | "metadata" :: rest ->
    rest |> AppMetadata.runMetadata
  | "status" :: rest ->
    rest |> AppStatus.runStatus
  | x :: _ ->
    failwith $"Unrecognized command: '{x}'"
    1

[<EntryPoint>]
let main args =
  try
    args |> Array.toList |> run
  with
  | ex ->
    ex |> fancyExceptionPrint verbose
    resetColor ()
    1



