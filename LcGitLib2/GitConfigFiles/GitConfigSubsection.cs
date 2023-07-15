/*
 * (c) 2021  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LcGitLib2.GitConfigFiles;

/// <summary>
/// A subsection in a git config
/// </summary>
public class GitConfigSubsection: GitConfigSectionBase
{
  private readonly string _key;

  /// <summary>
  /// Create a new GitConfigSubsection
  /// </summary>
  public GitConfigSubsection(
    string section,
    string subsection,
    IEnumerable<GitConfigValue> values = null)
    : base(section, values)
  {
    Subsection = subsection;
    _key = Section + "." + subsection;
  }

  /// <summary>
  /// The subsection name (case sensitive!)
  /// </summary>
  [JsonProperty("subsection")]
  public string Subsection { get; }

  ///// <summary>
  ///// Overrides GetKey to return a string in the form {section}.{subsection}
  ///// </summary>
  //protected override string GetKey()
  //{
  //  return _key;
  //}

  /// <summary>
  /// Returns a compact representation
  /// </summary>
  public JObject ToCompactJson(bool forceArrays)
  {
    return CompactValues(forceArrays);
  }
}
