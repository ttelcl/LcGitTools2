/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LcGitBup.Configuration;

/// <summary>
/// Repository-specific gitbup configuration
/// </summary>
public class RepoConfig
{
  /// <summary>
  /// Create a new LocalConfiguration
  /// </summary>
  public RepoConfig(
    string repoName,
    string? targetFolder)
  {
    OtherFields = new Dictionary<string, JToken?>();
    TargetFolder = targetFolder;
    RepoName = repoName;
  }

  /// <summary>
  /// The identifier tag to use for this repository
  /// </summary>
  [JsonProperty("repoName")]
  public string RepoName { get; internal set; }

  /// <summary>
  /// The folder where the backup bundles reside, or null if
  /// not yet configured.
  /// </summary>
  [JsonProperty("targetFolder")]
  public string? TargetFolder { get; internal set; }

  /// <summary>
  /// Contains fields from the JSON representation that are not
  /// explicitly handled otherwise (supporting extension)
  /// </summary>
  [JsonExtensionData]
  public Dictionary<string, JToken?> OtherFields { get; init; }

}
