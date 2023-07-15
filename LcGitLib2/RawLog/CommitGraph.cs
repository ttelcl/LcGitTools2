/*
 * (c) 2021  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LcGitLib2.RawLog;

/// <summary>
/// A graph model for a set of related commits
/// </summary>
public class CommitGraph
{
  private readonly Dictionary<string, CommitNode> _nodes;

  /// <summary>
  /// Create a new CommitGraph
  /// </summary>
  /// <param name="entries">
  /// A complete set of commit entries, without missing parents
  /// </param>
  /// <param name="ignoreMissing">
  /// If true: allow missing parents and exclude edges for those missing parents
  /// </param>
  public CommitGraph(IEnumerable<CommitEntry> entries, bool ignoreMissing = false)
  {
    _nodes = new Dictionary<string, CommitNode>();
    Nodes = _nodes;
    foreach(var entry in entries)
    {
      var node = new CommitNode(this, entry);
      _nodes[node.Id] = node;
    }
    foreach(var node in _nodes.Values)
    {
      node.Connect(ignoreMissing);
    }
  }

  /// <summary>
  /// The nodes in this graph
  /// </summary>
  public IReadOnlyDictionary<string, CommitNode> Nodes { get; }

  /// <summary>
  /// Create a new NodeSet containing the given nodes
  /// </summary>
  public CommitNodeSet NewNodeSet(IEnumerable<CommitNode> nodes)
  {
    return new CommitNodeSet(this, nodes);
  }

  /// <summary>
  /// Create a new NodeSet containing the identified nodes from this graph
  /// </summary>
  public CommitNodeSet NewNodeSet(IEnumerable<string> ids)
  {
    return new CommitNodeSet(this, from id in ids select _nodes[id]);
  }

  /// <summary>
  /// Create a new empty NodeSet
  /// </summary>
  public CommitNodeSet NewNodeSet()
  {
    return new CommitNodeSet(this);
  }

  /// <summary>
  /// Create a hashset containing the union of all ids of parent nodes of
  /// the given nodes. 
  /// </summary>
  public HashSet<string> ParentUnion(IEnumerable<string> ids)
  {
    var result = new HashSet<string>();
    foreach(var id in ids)
    {
      var node = _nodes[id];
      foreach(var parent in node.Parents)
      {
        result.Add(parent.Id);
      }
    }
    return result;
  }

  /// <summary>
  /// Create a hashset containing the union of all ids of parent nodes of
  /// the given nodes. 
  /// </summary>
  /// <param name="ids">
  /// The ids of the nodes to look up the parents of.
  /// </param>
  /// <param name="exclude">
  /// Parents that appear in this set are not added to the result.
  /// An intended scenario is that 'ids' and 'exclude' are the same
  /// </param>
  public HashSet<string> ParentUnion(IEnumerable<string> ids, HashSet<string> exclude)
  {
    var result = new HashSet<string>();
    foreach(var id in ids)
    {
      var node = _nodes[id];
      foreach(var parent in node.Parents)
      {
        var pid = parent.Id;
        if(!exclude.Contains(pid))
        {
          result.Add(pid);
        }
      }
    }
    return result;
  }

}
