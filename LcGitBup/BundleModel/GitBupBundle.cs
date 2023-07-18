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

namespace LcGitBup.BundleModel;

/// <summary>
/// Models a backup unit
/// </summary>
public class GitBupBundle
{
  /// <summary>
  /// Create a new GitBupBundle
  /// </summary>
  public GitBupBundle(
    string folder,
    string prefix,
    int tier,
    string id,
    string? refId)
  {
    Folder = folder;
    Prefix = prefix;
    Tier = tier;
    Id = id;
    RefId = refId;
    var refId2 = String.IsNullOrEmpty(RefId) ? "-" : RefId;
    BundleFileName = $"{Prefix}.{Id}.{refId2}.t{Tier}.bundle";
    MetaFileName = BundleFileName + ".meta.json";
  }

  public static GitBupBundle? FromBundleName(string fileName)
  {
    var shortName = Path.GetFileName(fileName);
    var segments = shortName.Split('.');
    if(segments.Length < 5)
    {
      return null;
    }
    var extension = segments[^1].ToLowerInvariant();
    if(extension != "bundle")
    {
      return null;
    }
    var tierText = segments[^2].ToLowerInvariant();
    if(tierText.Length != 2 || tierText[0] != 't' || tierText[1]<'0' || tierText[1]>'9')
    {
      return null;
    }
    var tier = tierText[1] - '0';
    var refId = segments[^3];
    if(tier == 0 && refId != "-")
    {
      return null;
    }
    var id = segments[^4];
    var prefix = String.Join(".", segments[0..^5]);
    throw new NotImplementedException();
  }

  /// <summary>
  /// The folder where the target bundle lives
  /// </summary>
  public string Folder { get; init; }

  /// <summary>
  /// The file prefix
  /// </summary>
  public string Prefix { get; init; }

  /// <summary>
  /// The tier of this backup. 0 for a full backup; 
  /// values 1-9 indicate an incremental backup that references
  /// another backup of <see cref="Tier"/> - 1.
  /// </summary>
  public int Tier { get; init; }

  /// <summary>
  /// The identifier for this bundle (derived from a timestamp in
  /// yyyyMMdd-HHmm form)
  /// </summary>
  public string Id { get; init; }

  /// <summary>
  /// The identifier of the reference bundle, or null for tier 0 files.
  /// </summary>
  public string? RefId { get; init; }

  /// <summary>
  /// The short file name derived from the other fields
  /// </summary>
  public string BundleFileName { get; init; }

  /// <summary>
  /// The filename for the metadata
  /// </summary>
  public string MetaFileName { get; init; }
}
