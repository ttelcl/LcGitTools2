﻿/*
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
    [JsonProperty("git-repo-roots")] IEnumerable<string>? gitRepoRoots = null,
    [JsonProperty("git-commit-count")] int gitCommitCount = 0,
    [JsonProperty("git-missing-count")] int gitMissingCommitCount = 0)
  {
    GitBundleTips = new List<string>(gitBundleTips ?? Array.Empty<string>()).AsReadOnly();
    GitRepoRoots = new List<string>(gitRepoRoots ?? Array.Empty<string>()).AsReadOnly();
    GitCommitCount = gitCommitCount;
    GitMissingCommitCount = gitMissingCommitCount;
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

  /// <summary>
  /// The total number of commits in the repo
  /// </summary>
  [JsonProperty("git-commit-count")]
  public int GitCommitCount { get; init; }

  /// <summary>
  /// The number of detected missing commits in the repo. Not serialized if 0.
  /// </summary>
  [JsonProperty("git-missing-count")]
  public int GitMissingCommitCount { get; init; }

  /// <summary>
  /// Determine if the <see cref="GitMissingCommitCount"/> property should be
  /// serialized
  /// </summary>
  public bool ShouldSerializeGitMissingCommitCount()
  {
    return GitMissingCommitCount > 0;
  }

  /// <summary>
  /// Calculates the set of added and removed commits when comparing this bundle
  /// with another bundle.
  /// </summary>
  /// <param name="ancestor">
  /// The bundle to compare with
  /// </param>
  /// <param name="added">
  /// Commits that are in this bundle but not the other
  /// </param>
  /// <param name="removed">
  /// Commits that are in the other bundle but not this
  /// </param>
  public void CompareToAncestor(
    BundleMetadata ancestor, out HashSet<string> added, out HashSet<string> removed)
  {
    added = new HashSet<string>(GitBundleTips);
    added.ExceptWith(ancestor.GitBundleTips);
    removed = new HashSet<string>(ancestor.GitBundleTips);
    removed.ExceptWith(GitBundleTips);
  }
}
