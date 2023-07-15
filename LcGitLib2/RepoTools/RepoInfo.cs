/*
 * (c) 2021  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LcGitLib2.Cfg;

namespace LcGitLib2.RepoTools
{
  /// <summary>
  /// Static information about a repository, as a view on a ConfigBlob
  /// </summary>
  public class RepoInfo: RepoInfoBase
  {
    /// <summary>
    /// Create a new RepoInfoBase
    /// </summary>
    public RepoInfo(ConfigBlob host, bool allowUninitialized = false)
      : base(host.Root, allowUninitialized)
    {
      HostBlob = host;
    }

    /// <summary>
    /// The underlying ConfigBlob
    /// </summary>
    public ConfigBlob HostBlob { get; }

  }

  /// <summary>
  /// Static information about a repository, as a view on a ConfigObject
  /// </summary>
  public class RepoInfoBase
  {
    /// <summary>
    /// Create a new RepoInfoBase
    /// </summary>
    public RepoInfoBase(ConfigObject hostObject, bool allowUninitialized=false)
    {
      HostObject = hostObject;
      if(!allowUninitialized && !IsInitialized(HostObject))
      {
        throw new InvalidOperationException(
          "RepoInfo backing is not initialized");
      }
    }

    /// <summary>
    /// Return true if all required fields are present
    /// </summary>
    public static bool IsInitialized(ConfigObject co)
    {
      return co.ContainsAll(new[] {
           "location",
           "label",
           "primaryroot",
           "instanceid",
           "version",
           "bare",
        });
    }

    /// <summary>
    /// The underlying ConfigObject
    /// </summary>
    public ConfigObject HostObject { get; }

    /// <summary>
    /// The canonical location of this repository
    /// </summary>
    public string Location { get => HostObject.Strings["location"]; }

    /// <summary>
    /// A user-friendly label for this repository
    /// </summary>
    public string Label { get => HostObject.Strings["label"]; }

    /// <summary>
    /// The commit ID of the oldest commit in this repository.
    /// Currently multi-root repositories are not supported, so this
    /// is the one and only root commit of the repository.
    /// </summary>
    public string PrimaryRoot { get => HostObject.Strings["primaryroot"]; }

    /// <summary>
    /// A random identifier used to uniquely identify this repository
    /// instance. Other clones of the same repository would have a different
    /// value.
    /// </summary>
    public string InstanceId { get => HostObject.Strings["instanceid"]; }

    /// <summary>
    /// A shortened version of the instance ID
    /// </summary>
    public string ShortId { get => InstanceId.Substring(0, 6); }

    /// <summary>
    /// A shortened version of the primary root commit ID
    /// </summary>
    public string ShortRoot { get => PrimaryRoot.Substring(0, 8); }

    /// <summary>
    /// The format version (currently 1)
    /// </summary>
    public int Version { get => (int)(HostObject.Integers["version"] ?? 0); }

    /// <summary>
    /// If the repo is bare or not
    /// </summary>
    public bool Bare { get => HostObject.Booleans["bare"].Value; }

    /// <summary>
    /// Return a string representation of the root commit timestamp, if known,
    /// or an empty string otherwise
    /// </summary>
    public string Founded { get => HostObject.Strings["founded"] ?? ""; }

    /// <summary>
    /// Return a string identifying the role of the repo if set, or
    /// an empty string otherwise. Known roles are "stage" and "transfer"
    /// </summary>
    public string Role { get => HostObject.Strings["role"] ?? ""; }

    /// <summary>
    /// True if this repo is tagged as a stage repo
    /// </summary>
    public bool IsStage { get => Role == "stage"; }

    /// <summary>
    /// True if this repo is tagged as a transfer repo
    /// </summary>
    public bool IsTransfer { get => Role == "transfer"; }

    /// <summary>
    /// Access non-standard string properties
    /// </summary>
    public string Other(string key, string defaultValue=null)
    {
      return HostObject.Strings[key] ?? defaultValue;
    }

    /// <summary>
    /// True if the text is a substring (or fully) of the label,
    /// considered case-insensitive
    /// </summary>
    public bool MatchesLabel(string txt)
    {
      return Label.Contains(txt, StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// True if the text is a substring (or fully) of the label, considered case-insensitive,
    /// and the role matches exactly
    /// </summary>
    public bool MatchesLabelAndRole(string labelPart, string role, bool exact=false)
    {
      if(Role != role && role != "" && role != "*")
      {
        return false;
      }
      if(exact)
      {
        return Label.Equals(labelPart, StringComparison.InvariantCultureIgnoreCase);
      }
      else
      {
        return Label.Contains(labelPart, StringComparison.InvariantCultureIgnoreCase);
      }
    }

    /// <summary>
    /// True if the text is a substring (or fully) of the label, considered case-insensitive,
    /// and the role matches exactly. The argument must be of the form 'label::role'
    /// </summary>
    public bool MatchesLabelAndRole(string labelAndRole, bool exact=false)
    {
      var parts = labelAndRole.Split("::");
      return (parts.Length == 2 && MatchesLabelAndRole(parts[0], parts[1], exact));
    }

    /// <summary>
    /// Initialize a new repo-info compatible ConfigBlob. Does not save it yet.
    /// </summary>
    public static void InitializeNew(
      ConfigBlob target,
      string location,
      string label,
      string primaryroot,
      string instanceid,
      bool bare,
      int version)
    {
      instanceid = instanceid ?? RandomId.NewId(12);
      target.Root
        .Set("location", location)
        .Set("label", label)
        .Set("primaryroot", primaryroot)
        .Set("instanceid", instanceid)
        .Set("bare", bare)
        .Set("version", version)
        ;
    }

  }
}
