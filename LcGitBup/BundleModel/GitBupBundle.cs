/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace LcGitBup.BundleModel;

/// <summary>
/// Models a backup unit
/// </summary>
public class GitBupBundle
{
  /// <summary>
  /// Create a new GitBupBundle
  /// </summary>
  /// <param name="folder">
  /// The folder in which the bundle exists (or will exist)
  /// </param>
  /// <param name="prefix">
  /// The prefix of the file name, indicating the repository
  /// </param>
  /// <param name="id">
  /// The backup identifier, which is derived from a UTC timestamp in "yyyyMMdd-HHmmss" form
  /// </param>
  /// <param name="refId">
  /// The id of the referenced bundle. This must be "-" for a tier 0 bundle,
  /// or a UTC timestamp in "yyyyMMdd-HHmmss" form otherwise.
  /// </param>
  /// <param name="tier">
  /// The backup tier. 0 indicates a full backup, higher numbers indicate
  /// an incremental backup referencing a previous tier backup.
  /// Pass -1 for GitBup V2 bundles (which do not use tiers).
  /// </param>
  /// <exception cref="ArgumentOutOfRangeException"></exception>
  public GitBupBundle(
    string folder,
    string prefix,
    string id,
    string? refId,
    int tier = -1)
  {
    Folder = folder;
    Prefix = prefix;
    Tier = tier;
    Id = id;
    RefId = String.IsNullOrEmpty(refId) ? null : refId; // normalize "" to null
    var refId2 = String.IsNullOrEmpty(RefId) ? "-" : RefId;
    BundleFileName =
      IsV2 
      ? $"{Prefix}.{Id}.{refId2}.bundle"
      : $"{Prefix}.{Id}.{refId2}.t{Tier}.bundle";
    MetaFileName = BundleFileName + ".meta.json";
    if(!IsValidId(id))
    {
      throw new ArgumentOutOfRangeException(
        nameof(id), $"The id ({id}) is not in the expected format");
    }
    if(!String.IsNullOrEmpty(refId))
    {
      if(!IsValidId(refId))
      {
        throw new ArgumentOutOfRangeException(
          nameof(refId), $"The reference id ({refId}) is not in the expected format");
      }
      if(StringComparer.InvariantCultureIgnoreCase.Compare(id, refId) <= 0)
      {
        throw new ArgumentOutOfRangeException(
          nameof(refId),
          $"The reference id ({refId}) is expected to be older than the own id ({id})");
      }
      if(Tier == 0)
      {
        throw new ArgumentOutOfRangeException(
          nameof(refId), $"Expecting the reference id to be '-' for a tier 0 bundle");
      }
    }
  }

  /// <summary>
  /// Build a GitBupBundle by parsing a bundle file name
  /// </summary>
  /// <param name="fileName">
  /// The bundle file name to parse. The directory part is used
  /// to derive the <see cref="Folder"/> property, so that should
  /// not be omitted.
  /// </param>
  /// <returns>
  /// A new <see cref="GitBupBundle"/> instance if successful, or null
  /// if the name did not match requirements
  /// </returns>
  public static GitBupBundle? FromBundleName(string fileName)
  {
    if(String.IsNullOrEmpty(fileName))
    {
      return null;
    }
    var fullName = Path.GetFullPath(fileName);
    string folder = Path.GetDirectoryName(fullName)!;
    var shortName = Path.GetFileName(fullName);
    var segments = shortName.Split('.');
    if(segments.Length < 4)
    {
      // Mimimum segment count for V2 is 4, for V1 is 5
      // but at this point we don't know if it is V1 or V2
      return null;
    }
    var extension = segments[^1].ToLowerInvariant();
    if(extension != "bundle")
    {
      return null;
    }
    var tierText = segments[^2].ToLowerInvariant();
    var isV1 =
      tierText.Length == 2
      && tierText[0] == 't'
      && tierText[1] >= '0'
      && tierText[1] <= '9';
    if(isV1 && segments.Length < 5)
    {
      return null;
    }
    int tier;
    if(isV1)
    {
      if(tierText.Length != 2 || tierText[0] != 't' || tierText[1]<'0' || tierText[1]>'9')
      {
        return null;
      }
      tier = tierText[1] - '0';
    }
    else
    {
      tier = -1;
    }
    var refId = isV1 ? segments[^3] : segments[^2];
    if(isV1)
    {
      if(tier == 0 && refId != "-")
      {
        return null;
      }
      if(tier < 0 || tier > 9)
      {
        return null;
      }
      if(tier > 0 && !IsValidId(refId))
      {
        return null;
      }
    }
    else
    {
      if(refId != "-" && !IsValidId(refId))
      {
        return null;
      }
    }
    var id = isV1 ? segments[^4] : segments[^3];
    if(!IsValidId(id))
    {
      return null;
    }
    var head = isV1 ? segments[..^4] : segments[..^3];
    var prefix = String.Join(".", head);
    string? refId2 = refId == "-" ? null : refId;
    return new GitBupBundle(folder, prefix, id, refId2, tier);
  }

  /// <summary>
  /// Create a V1 GitBupBundle instance for a new tier 0 bundle
  /// </summary>
  /// <param name="folder">
  /// The folder where the bundle is expected to be created
  /// </param>
  /// <param name="prefix">
  /// The bundle prefix (repository name)
  /// </param>
  /// <param name="overrideStamp">
  /// Normally null (default). If not null: override the time stamp
  /// used to derive the bundle ID. This is intended for use in unit tests only.
  /// </param>
  /// <returns>
  /// A new <see cref="GitBupBundle"/> (for which the backing file does not yet exist)
  /// </returns>
  public static GitBupBundle NewTier0Bundle(
    string folder, string prefix, DateTime? overrideStamp = null)
  {
    var stamp = (overrideStamp ?? DateTime.UtcNow).ToUniversalTime();
    var id = stamp.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
    folder = String.IsNullOrEmpty(folder) ? Environment.CurrentDirectory : folder;
    return new GitBupBundle(folder, prefix, id, null, 0);
  }

  /// <summary>
  /// Create a V2 GitBupBundle instance for a new root bundle
  /// </summary>
  /// <param name="folder">
  /// The folder where the bundle is expected to be created
  /// </param>
  /// <param name="prefix">
  /// The bundle prefix (repository name)
  /// </param>
  /// <param name="overrideStamp">
  /// Normally null (default). If not null: override the time stamp
  /// used to derive the bundle ID. This is intended for use in unit tests only.
  /// </param>
  /// <returns>
  /// A new <see cref="GitBupBundle"/> (for which the backing file does not yet exist)
  /// </returns>
  public static GitBupBundle NewV2RootBundle(
    string folder, string prefix, DateTime? overrideStamp = null)
  {
    var stamp = (overrideStamp ?? DateTime.UtcNow).ToUniversalTime();
    var id = stamp.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
    folder = String.IsNullOrEmpty(folder) ? Environment.CurrentDirectory : folder;
    return new GitBupBundle(folder, prefix, id, null, -1);
  }

  /// <summary>
  /// Derive the <see cref="GitBupBundle"/> instance for a next tier bundle
  /// referencing this bundle
  /// </summary>
  /// <param name="overrideStamp">
  /// Normally null (default). If not null: override the time stamp
  /// used to derive the bundle ID. This is intended for use in unit tests only.
  /// </param>
  /// <returns>
  /// A new <see cref="GitBupBundle"/> (for which the backing file does not yet exist)
  /// </returns>
  public GitBupBundle DeriveBundle(DateTime? overrideStamp = null)
  {
    var stamp = (overrideStamp ?? DateTime.UtcNow).ToUniversalTime();
    var id = stamp.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
    var nextTier = IsV2 ? -1 : Tier + 1;
    return new GitBupBundle(Folder, Prefix, id, Id, nextTier);
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
  /// This value is -1 (or more generally: negative) if the bundle is a GitBup V2
  /// bundle. GitBup V2 does not use tiers.
  /// </summary>
  public int Tier { get; init; }

  /// <summary>
  /// True if this bundle is a GitBup V2 bundle. GitBup V2 bundles do not use
  /// tiers like V1 bundles do. Shorthand for <see cref="Tier"/> &lt; 0.
  /// </summary>
  public bool IsV2 => Tier < 0;

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
  /// The full file name for the bundle file
  /// </summary>
  public string FullBundleFileName => Path.Combine(Folder, BundleFileName);

  /// <summary>
  /// The short filename for the metadata
  /// </summary>
  public string MetaFileName { get; init; }

  /// <summary>
  /// The full file name for the metadata file
  /// </summary>
  public string FullMetaFileName => Path.Combine(Folder, MetaFileName);

  /// <summary>
  /// Return true if the bundle and the metadata files exist
  /// </summary>
  public bool DoesExist() => File.Exists(FullBundleFileName) && File.Exists(FullMetaFileName);

  /// <summary>
  /// Read the bundle's metadata. Check <see cref="DoesExist()"/> to make sure
  /// the metadata file actually exists
  /// </summary>
  /// <returns>
  /// A new BundleMetadata instance
  /// </returns>
  public BundleMetadata ReadMetadata()
  {
    var json = File.ReadAllText(FullMetaFileName);
    return JsonConvert.DeserializeObject<BundleMetadata>(json) 
      ?? throw new InvalidDataException($"Error reading {MetaFileName}");
  }

  /// <summary>
  /// Save the provided metadata as the metadata for this bundle
  /// (overwriting any existing metdata)
  /// </summary>
  /// <param name="metadata">
  /// The metadata object to persist
  /// </param>
  public void SaveMetadata(BundleMetadata metadata)
  {
    var json = JsonConvert.SerializeObject(metadata, Formatting.Indented);
    File.WriteAllText(FullMetaFileName, json);
  }

  /// <summary>
  /// Soft-delete the bundle and metadata files by renaming them.
  /// </summary>
  public void Discard()
  {
    if(File.Exists(FullBundleFileName))
    {
      File.Move(FullBundleFileName, FullBundleFileName+".bak", true);
    }
    if(File.Exists(FullMetaFileName))
    {
      File.Move(FullMetaFileName, FullMetaFileName+".bak", true);
    }
  }

  /// <summary>
  /// Check if this GitBupBundle is directly referencing the 
  /// given parent bundle.
  /// </summary>
  public bool IsReferencing(GitBupBundle parent)
  {
    return
      (Tier < 0 || Tier == parent.Tier + 1)
      && Prefix == parent.Prefix
      && RefId == parent.Id
      ;
  }

  /// <summary>
  /// Check if the string is a valid (and nondegenerate) GitBup bundle ID
  /// </summary>
  public static bool IsValidId(string id)
  {
    return __BundleIdRegex.IsMatch(id);
  }

  private static readonly Regex __BundleIdRegex =
    new Regex(@"^2\d{7}-\d{6}$");
}
