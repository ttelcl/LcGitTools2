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

namespace LcGitLib2.RepoTools;

/// <summary>
/// A collection of multiple repo-info objects
/// </summary>
public class RepoInfos
{
  private readonly Dictionary<string, RepoBucket> _buckets;

  /// <summary>
  /// Create a new RepoInfos
  /// </summary>
  public RepoInfos(
    ConfigBlob backingBlob)
  {
    _buckets = new Dictionary<string, RepoBucket>(StringComparer.InvariantCultureIgnoreCase);
    HostBlob = backingBlob;
    // materialize the buckets
    foreach(var bid in BucketIds)
    {
      _buckets[bid] = new RepoBucket(this, bid);
    }
  }

  /// <summary>
  /// The file storing the entire collection
  /// </summary>
  public ConfigBlob HostBlob { get; }

  /// <summary>
  /// Enumerate the known bucket IDs
  /// </summary>
  public IEnumerable<string> BucketIds {
    get {
      foreach(var prop in HostBlob.Content.Properties())
      {
        var key = prop.Name;
        if(key.Length==40)
        {
          yield return key;
        }
      }
    }
  }

  /// <summary>
  /// Enumerate the buckets
  /// </summary>
  public IEnumerable<RepoBucket> Buckets {
    get {
      foreach(var bid in BucketIds)
      {
        yield return this[bid, true];
      }
    }
  }

  /// <summary>
  /// Enumerate all repositories
  /// </summary>
  public IEnumerable<RepoInfoBase> Repositories { get => Buckets.SelectMany(bucket => bucket.Repositories); }

  /// <summary>
  /// Return the bucket holding RepoInfo objects for the given root commit ID
  /// </summary>
  /// <param name="rootId">
  /// The root commit ID
  /// </param>
  /// <param name="create">
  /// If true: create the bucket if it didn't exist yet, otherwise return null instead
  /// </param>
  public RepoBucket this[string rootId, bool create = false] {
    get {
      if(!_buckets.TryGetValue(rootId, out var bucket))
      {
        if(create)
        {
          bucket = new RepoBucket(this, rootId);
          _buckets[rootId] = bucket;
          return bucket;
        }
        else
        {
          return null;
        }
      }
      else
      {
        return bucket;
      }
    }
  }

  /// <summary>
  /// Return the registered repo information, or null if not found
  /// </summary>
  public RepoInfoBase this[string rootId, string repoId] {
    get {
      var bucket = this[rootId, false];
      if(bucket!=null)
      {
        return bucket[repoId];
      }
      return null;
    }
  }

  /// <summary>
  /// Find the existing registration for the given RepoInfo, or null if it wasn't
  /// registered yet
  /// </summary>
  public RepoInfoBase this[RepoInfo ri] { get => this[ri.PrimaryRoot, ri.InstanceId]; }

  /// <summary>
  /// Returns repos for which the label contains the label part and the role is
  /// as specified
  /// </summary>
  public IEnumerable<RepoInfoBase> LabelRoleMatches(string labelPart, string role)
  {
    return Repositories.Where(repo => repo.MatchesLabelAndRole(labelPart, role));
  }

  /// <summary>
  /// Returns repos for which the label contains the label part and the role is
  /// as specified. The argument must be of the shape "label::role"
  /// </summary>
  public IEnumerable<RepoInfoBase> LabelRoleMatches(string labelAndRole)
  {
    var parts = labelAndRole.Split("::");
    if(parts.Length==2)
    {
      return LabelRoleMatches(parts[0], parts[1]);
    }
    else
    {
      return new RepoInfoBase[0];
    }
  }

  /// <summary>
  /// Insert an external RepoInfo into this db
  /// </summary>
  public RepoInfoBase Insert(RepoInfoBase ri)
  {
    var bucket = this[ri.PrimaryRoot, true];
    return bucket.Put(ri);
  }

  /// <summary>
  /// Remove all content
  /// </summary>
  internal void Clear()
  {
    HostBlob.Root.Clear();
    _buckets.Clear();
  }

}

/// <summary>
/// Stores RepoInfo objects sharing a common Root commit ID
/// </summary>
public class RepoBucket
{
  private readonly Dictionary<string, RepoInfoBase> _cache;

  internal RepoBucket(RepoInfos container, string rootId)
  {
    _cache = new Dictionary<string, RepoInfoBase>(StringComparer.InvariantCultureIgnoreCase);
    Container = container;
    RootId = rootId;
    Bucket = Container.HostBlob.Root.Child(rootId, MissingBehaviour.Create);
    // Hydrate the cache
    foreach(var repoId in RepoIds)
    {
      var co = Bucket.Child(repoId, MissingBehaviour.ReturnNull);
      if(co!=null)
      {
        var ri = new RepoInfoBase(co);
        _cache[ri.InstanceId] = ri;
      }
    }
  }

  /// <summary>
  /// The RepoInfos db this is part of
  /// </summary>
  public RepoInfos Container { get; }

  /// <summary>
  /// The Root Commit ID shared by the RepoInfos inside
  /// </summary>
  public string RootId { get; }

  /// <summary>
  /// The JObject backing this bucket
  /// </summary>
  public ConfigObject Bucket { get; }

  /// <summary>
  /// Enumerate the contained repo IDs
  /// </summary>
  public IEnumerable<string> RepoIds {
    get {
      foreach(var prop in Bucket.Content.Properties())
      {
        var key = prop.Name;
        if(key.Length==12)
        {
          yield return key;
        }
      }
    }
  }

  /// <summary>
  /// Enumerate the repositories in this bucket
  /// </summary>
  public IEnumerable<RepoInfoBase> Repositories {
    get {
      foreach(var repoId in RepoIds)
      {
        yield return this[repoId];
      }
    }
  }

  /// <summary>
  /// Get the indicated RepoInfo if it exists, return null otherwise.
  /// A cached instance is returned if available.
  /// </summary>
  public RepoInfoBase this[string repoId] {
    get {
      if(_cache.TryGetValue(repoId, out var repoInfo))
      {
        return repoInfo;
      }
      else
      {
        // try to instantiate it if possible
        var co = Bucket.Child(repoId, MissingBehaviour.ReturnNull);
        if(co==null)
        {
          return null;
        }
        else
        {
          var ri = new RepoInfoBase(co);
          _cache[ri.InstanceId] = ri;
          return ri;
        }
      }
    }
  }

  internal RepoInfoBase Put(RepoInfoBase ri)
  {
    if(ri.PrimaryRoot != RootId)
    {
      throw new InvalidOperationException(
        "Root commit id does not match this bucket");
    }
    _cache.Remove(ri.InstanceId);
    Bucket.Remove(ri.InstanceId);
    var child = Bucket.Child(ri.InstanceId, MissingBehaviour.Create);
    child.Import(ri.HostObject);
    var ri2 = new RepoInfoBase(child);
    _cache[ri2.InstanceId] = ri2;
    return ri2;
  }


}
