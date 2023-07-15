/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LcGitLib2.GitModels;

namespace LcGitLib2.RawLog;

/// <summary>
/// A collection of CommitIdNodes
/// </summary>
public class CommitMap
{
  private readonly Dictionary<GitId, CommitIdNode> _map;

  /// <summary>
  /// Create a new CommitIdNodeMap
  /// </summary>
  public CommitMap()
  {
    _map = new Dictionary<GitId, CommitIdNode>();
  }

  /// <summary>
  /// Get the node for the given ID, creating it if necessary
  /// </summary>
  public CommitIdNode Get(GitId id)
  {
    if(!_map.TryGetValue(id, out var node))
    {
      node = new CommitIdNode(id);
      _map.Add(id, node);
    }
    return node;
  }

  /// <summary>
  /// Find the node for the given ID, returning null if not found
  /// </summary>
  public CommitIdNode? Find(GitId id)
  {
    return _map.TryGetValue(id, out var node) ? node : null;
  }

  /// <summary>
  /// A collection containing all the nodes in this map
  /// </summary>
  public IReadOnlyCollection<CommitIdNode> Nodes => _map.Values;

  /// <summary>
  /// Return the nodes that were referenced as parents, but were never observed.
  /// </summary>
  public IEnumerable<CommitIdNode> MissingNodes => Nodes.Where(n => !n.Observed);

  /// <summary>
  /// Returns true if all nodes in the map were observed (<see cref="MissingNodes"/> is empty)
  /// </summary>
  public bool IsCompleted => !MissingNodes.Any();

  /// <summary>
  /// Return the nodes that were inserted but were never referenced as parent
  /// (that is: they don't have child nodes)
  /// </summary>
  public IEnumerable<CommitIdNode> Tips => Nodes.Where(n => n.Children.Count == 0);

  /// <summary>
  /// Return the nodes that were inserted but have no parent(s). Typical
  /// repositories only have one such node (the initial commit)
  /// </summary>
  public IEnumerable<CommitIdNode> Roots => Nodes.Where(n => n.Parents.Count == 0);

  /// <summary>
  /// Remove missing nodes (nodes not flagged as "observed")
  /// </summary>
  /// <returns></returns>
  public List<CommitIdNode> PruneMissing()
  {
    var missing = MissingNodes.ToList();
    // first: unlink children's parents
    foreach(var node in missing)
    {
      foreach(var childId in node.Children)
      {
        var child = Find(childId);
        if(child != null)
        {
          child.PruneMissingParent(node);
        }
      }
    }
    // second: remove the nodes
    foreach(var node in missing)
    {
      _map.Remove(node.Id);
    }
    return missing;
  }

  /// <summary>
  /// Insert a node with the given <paramref name="id"/> and
  /// <paramref name="parents"/> and mark it as observed. 
  /// Register the connections to each child node.
  /// </summary>
  public void Insert(GitId id, IEnumerable<GitId> parents)
  {
    var node = Get(id);
    if(node.Observed)
    {
      throw new InvalidOperationException(
        $"Duplicate node: {id}");
    }
    node.SetParents(parents);
    foreach(var parent in parents)
    {
      var parentNode = Get(parent);
      parentNode.AddChild(node);
    }
  }

  /// <summary>
  /// Insert a new <see cref="CommitIdNode"/> derived from the given
  /// <see cref="CommitEntry"/>.
  /// </summary>
  public void Insert(CommitEntry entry)
  {
    Insert(new GitId(entry.Id), entry.Parents.Select(sid => new GitId(sid)));
  }

}
