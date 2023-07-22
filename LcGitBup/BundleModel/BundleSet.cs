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
using System.Threading.Tasks;

namespace LcGitBup.BundleModel;

/// <summary>
/// Describes a collection of related bundles. This collection may or may not
/// form a valid set of tiers.
/// </summary>
public class BundleSet
{
  private readonly Dictionary<string, GitBupBundle> _bundlesById;

  /// <summary>
  /// Create a new BundleSet. Includes a call to <see cref="AddMissing()"/>
  /// </summary>
  public BundleSet(
    string folder,
    string prefix)
  {
    _bundlesById = new Dictionary<string, GitBupBundle>(StringComparer.OrdinalIgnoreCase);
    AllBundles = _bundlesById.Values;
    Folder = String.IsNullOrEmpty(folder) ? Environment.CurrentDirectory : Path.GetFullPath(folder);
    Prefix = prefix;
    TierStack = new BundleStack(this);
    AddMissing();
  }

  /// <summary>
  /// A view on all the bundles in this set
  /// </summary>
  public IReadOnlyCollection<GitBupBundle> AllBundles { get; init; }

  /// <summary>
  /// The folder where the bundles are stored
  /// </summary>
  public string Folder { get; init; }

  /// <summary>
  /// The prefix for the bundles (logical identifier for the repository)
  /// </summary>
  public string Prefix { get; init; }

  /// <summary>
  /// The tier model of the bundles
  /// </summary>
  public BundleStack TierStack { get; init; }

  /// <summary>
  /// Add a new bundle. Does not do anything if its files don't exist.
  /// Does not do anything if the bundle is already present. Updates
  /// the tier stack afterward. Consider calling <see cref="DiscardUnused()"/>
  /// afterward.
  /// </summary>
  /// <param name="bundle">
  /// The bundle to add.
  /// </param>
  /// <returns>
  /// True if the bundle was added.
  /// </returns>
  public bool AddBundle(GitBupBundle bundle)
  {
    if(bundle.DoesExist())
    {
      if(!_bundlesById.ContainsKey(bundle.Folder))
      {
        _bundlesById[bundle.Id] = bundle;
        TierStack.Rebuild();
        return true;
      }
    }
    return false;
  }

  /// <summary>
  /// Find a bundle by its ID
  /// </summary>
  public GitBupBundle? FindById(string id)
  {
    return _bundlesById.TryGetValue(id, out var bundle) ? bundle : null;
  }

  /// <summary>
  /// Generate a new GitBupBundle with the proposed information for
  /// the next bundle.
  /// </summary>
  /// <param name="tier">
  /// The tier to aim for. If necessary this is lowered to the maximum
  /// valid value (the current stack depth). Range 0 - 9.
  /// </param>
  /// <returns></returns>
  public GitBupBundle NextBundle(int tier)
  {
    if(tier < 0 || tier > 9)
    {
      throw new ArgumentOutOfRangeException(nameof(tier), "Expecting a tier in the range 0-9");
    }
    var stamp = DateTime.UtcNow;
    var id = stamp.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
    if(tier > TierStack.Depth)
    {
      tier = TierStack.Depth;
    }
    if(tier > 0)
    {
      var reference = TierStack.Tiers[tier-1];
      return new GitBupBundle(Folder, Prefix, tier, id, reference.Id);
    }
    else
    {
      return new GitBupBundle(Folder, Prefix, 0, id, null);
    }
  }

  /// <summary>
  /// If a bundle with the given ID is present in this set then discard
  /// the bundle and remove it from the set. If it is part of the current
  /// tier stack an exception is raised.
  /// </summary>
  public void DiscardById(string id)
  {
    var bundle = FindById(id);
    if(bundle != null)
    {
      if(TierStack.Contains(bundle))
      {
        throw new InvalidOperationException(
          $"Cannot discard a bundle that is still in active use: {bundle.BundleFileName}");
      }
      _bundlesById.Remove(id);
      bundle.Discard();
    }
  }

  /// <summary>
  /// Discard a bundle in this set, removing it from this set and renaming the files.
  /// This operation is refused if the bundle is part of the current stack.
  /// </summary>
  public void DiscardBundle(GitBupBundle bundle)
  {
    DiscardById(bundle.Id);
  }

  /// <summary>
  /// Discard any bundles that are not in active use
  /// </summary>
  /// <returns>
  /// A collection of the original bundles that were discarded
  /// </returns>
  public IReadOnlyCollection<GitBupBundle> DiscardUnused()
  {
    var unused = _bundlesById.Values.Where(gbb => !TierStack.Contains(gbb)).ToList();
    foreach(var bundle in unused)
    {
      DiscardBundle(bundle);
    }
    return unused;
  }

  /// <summary>
  /// Delete old discarded bundles. The aim is to leave ay most one discarded bundle
  /// per tier (in addition to the non-discarded one, of course)
  /// </summary>
  public IReadOnlyList<string> Purge()
  {
    var purgedBundles = new List<GitBupBundle>();
    var di = new DirectoryInfo(Folder);
    if(di.Exists)
    {
      var candidates =
        di.GetFiles("*.t?.bundle.bak")
        .Where(fi => fi.Name.StartsWith(Prefix))
        .ToList();
      var discards = new List<GitBupBundle>();
      foreach(var candidate in candidates)
      {
        var name = candidate.FullName[..^4]; // strip off the ".bak" extension
        var gbb = GitBupBundle.FromBundleName(name);
        if(gbb != null)
        {
          discards.Add(gbb);
        }
      }
      var byTier = discards.GroupBy(gbb => gbb.Tier);
      foreach(var tier in byTier)
      {
        var ghostBundles = tier.OrderByDescending(gbb => gbb.Id).ToList();
        purgedBundles.AddRange(ghostBundles.Skip(1));
      }
    }
    var purgedFiles = new List<string>();
    foreach(var bundle in purgedBundles)
    {
      var purgedFile = bundle.FullBundleFileName + ".bak";
      if(File.Exists(purgedFile))
      {
        purgedFiles.Add(purgedFile);
        File.Delete(purgedFile);
      }
      var purgedMeta = bundle.FullMetaFileName + ".bak";
      if(File.Exists(purgedMeta))
      {
        purgedFiles.Add(purgedMeta);
        File.Delete(purgedMeta);
      }
    }
    return purgedFiles.AsReadOnly();
  }

  /// <summary>
  /// Find a referenced bundle (returning null if not found or there is no reference)
  /// </summary>
  public GitBupBundle? FindReferencedBundle(GitBupBundle bundle)
  {
    var rb = bundle.RefId == null ? null : FindById(bundle.RefId);
    if(rb != null)
    {
      if(rb.Tier + 1 != bundle.Tier)
      {
        throw new InvalidOperationException(
          $"Inconsistent tier linkage from {bundle.BundleFileName} to {rb.BundleFileName}");
      }
    }
    return rb;
  }

  /// <summary>
  /// Insert bundles in <see cref="Folder"/> that are missing from this set.
  /// </summary>
  /// <returns>
  /// A list of the bundles that were added
  /// </returns>
  public IReadOnlyList<GitBupBundle> AddMissing()
  {
    var bundleFileNames = Directory.GetFiles(Folder, "*.bundle");
    var list = new List<GitBupBundle>();
    foreach(var bundleFileName in bundleFileNames)
    {
      var gbb = GitBupBundle.FromBundleName(bundleFileName);
      if(gbb != null && gbb.Folder == Folder && gbb.Prefix == Prefix)
      {
        if(!_bundlesById.ContainsKey(gbb.Id))
        {
          _bundlesById[gbb.Id] = gbb;
        }
        list.Add(gbb);
      }
    }
    TierStack.Rebuild();
    return list;
  }

}