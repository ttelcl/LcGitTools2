/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LcGitBup.Configuration;

/// <summary>
/// Global configuration
/// </summary>
public class GitBupConfig
{
  /// <summary>
  /// Create a new GitBupConfig
  /// </summary>
  public GitBupConfig()
  {
    OtherFields = new Dictionary<string, JToken?>();
    AnchorFolders = new Dictionary<string, string>();
  }

  /// <summary>
  /// Anchor folders: maps anchor tags to their full foldr paths
  /// </summary>
  [JsonProperty("anchorFolders")]
  public Dictionary<string, string> AnchorFolders { get; init; }

  /// <summary>
  /// Contains fields from the JSON representation that are not
  /// explicitly handled otherwise (supporting extension)
  /// </summary>
  [JsonExtensionData]
  public Dictionary<string, JToken?> OtherFields { get; init; }

}
