/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LcGitLib2.GraphModel;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LcGitBup.BundleModel;

/// <summary>
/// Contains information on terminal commits in the repo at the time the
/// bundle was created
/// </summary>
public class BundleMetadata
{
  /// <summary>
  /// Create a new BundleMetadata
  /// </summary>
  public BundleMetadata(
    [JsonProperty("git-bundle-tips")] IEnumerable<string> gitBundleTips,
    [JsonProperty("git-repo-roots")] IEnumerable<string>? gitRepoRoots = null)
  {
    GitBundleTips = new List<string>(gitBundleTips ?? Array.Empty<string>()).AsReadOnly();
    GitRepoRoots = new List<string>(gitRepoRoots ?? Array.Empty<string>()).AsReadOnly();
  }

  /// <summary>
  /// Lists the full commit ids of all "tips" of the repository at the
  /// time of capturing the bundle (commits that are not the parent of any
  /// other commit; that is: the tips are the newest commit(s))
  /// </summary>
  [JsonProperty("git-bundle-tips")]
  public IReadOnlyList<string> GitBundleTips { get; init; }

  /// <summary>
  /// Lists the full commit ids of all "roots" of the repository at the
  /// time of capturing the bundle (commits that have no parent; that is:
  /// the root(s) are the oldest commit(s).). Usually there is only one
  /// root in a repo.
  /// </summary>
  [JsonProperty("git-repo-roots")] 
  public IReadOnlyList<string> GitRepoRoots { get; init; }

}