module AppRun

open System
open System.IO

open LcGitBup
open LcGitLib2.RepoTools

open ColorPrint
open CommonTools

type private RunOptions = {
  RepoPath: string
  Tier: int option
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
    cp $"DBG: tier = %O{o.Tier}"
    failwith "NYI"
    0
  | None ->
    Usage.usage "run"
    1
