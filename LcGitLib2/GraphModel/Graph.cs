/*
 * (c) 2021  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LcGitLib2.RawLog;

namespace LcGitLib2.GraphModel;

/// <summary>
/// A directed graph, built from nodes built from node seeds
/// </summary>
public class Graph<TId, TSeed>
  where TSeed : IGraphNodeSeed<TId>
  where TId : IEquatable<TId>
{
  private readonly GraphNodeSet<TId, TSeed> _nodes;
  private readonly List<Node> _roots;
  private readonly List<Node> _tips;
  private readonly List<Node> _nodesList;

  /// <summary>
  /// Create a new Graph
  /// </summary>
  public Graph(
    IEnumerable<TSeed> seeds,
    bool allowPruning)
  {
    _nodes = new GraphNodeSet<TId, TSeed>(this, false);
    _nodesList = new List<Node>();
    Nodes = _nodesList.AsReadOnly();
    _roots = new List<Node>();
    _tips = new List<Node>();
    Roots = _roots.AsReadOnly();
    Tips = _tips.AsReadOnly();
    var index = 0;
    foreach(var seed in seeds)
    {
      var node = new Node(this, seed, index++);
      _nodesList.Add(node);
      _nodes[node.Id] = node;
    }
    foreach(var node in _nodes.Values)
    {
      var pruneCount = node.LinkNodes(allowPruning);
      PrunedEdgeCount += pruneCount;
    }
    foreach(var node in _nodes.Values)
    {
      if(node.Parents.Count==0)
      {
        _roots.Add(node);
      }
      if(node.Children.Count == 0)
      {
        _tips.Add(node);
      }
    }
  }

  /// <summary>
  /// The root nodes, i.e. nodes that do not have any parents (after pruning)
  /// </summary>
  public IReadOnlyList<Node> Roots { get; }

  /// <summary>
  /// The tip nodes, i.e. nodes that do not have any children
  /// </summary>
  public IReadOnlyList<Node> Tips { get; }

  /// <summary>
  /// The number of edges specified in the seeds that were pruned
  /// because the referenced parent seed was missing.
  /// If this is not 0, the seed set provided to the constructor was
  /// incomplete.
  /// </summary>
  public int PrunedEdgeCount { get; private set; }

  /// <summary>
  /// Find a node by ID.
  /// </summary>
  public Node this[TId id, bool nullIfMissing = false] {
    get => _nodes[id, nullIfMissing];
  }

  /// <summary>
  /// The list of all nodes. Nodes[i].Index == i.
  /// </summary>
  public IReadOnlyList<Node> Nodes { get; }

  /// <summary>
  /// Enumerate all nodes (based on the ID mapping)
  /// </summary>
  public IEnumerable<Node> NodeValues { get => _nodes.Values; }

  /// <summary>
  /// Enumerate all keys
  /// </summary>
  public IEnumerable<TId> Ids { get => _nodes.Keys; }

  /// <summary>
  /// Lookup multiple nodes by ID.
  /// </summary>
  /// <param name="ids">
  /// The IDs to look up
  /// </param>
  /// <param name="skipMissing">
  /// If false: if an ID is unknown then throw a KeyNotFoundException.
  /// If true: if an ID is unknown then skip it.
  /// </param>
  /// <returns>
  /// The nodes that were succesfully found
  /// </returns>
  public IEnumerable<Node> FindMany(IEnumerable<TId> ids, bool skipMissing = false)
  {
    foreach(var id in ids)
    {
      var node = _nodes[id, skipMissing];
      if(node!=null)
      {
        yield return node;
      }
    }
  }

  /// <summary>
  /// Create a new, empty, GraphNodeSet
  /// </summary>
  public GraphNodeSet<TId, TSeed> NewSet(bool nullWhenMissing = false)
  {
    return new GraphNodeSet<TId, TSeed>(this, nullWhenMissing);
  }

  /// <summary>
  /// Create a new, empty, GraphNodeMap for the given value type
  /// </summary>
  public GraphNodeMap<TId, TValue, TSeed> NewMap<TValue>(bool defaultWhenMissing = false)
  {
    return new GraphNodeMap<TId, TValue, TSeed>(this, defaultWhenMissing);
  }

  /// <summary>
  /// Create a new GraphColorMap
  /// </summary>
  public GraphColorMap<TId, TSeed> NewColorMap(int fillValue = 0)
  {
    return new GraphColorMap<TId, TSeed>(this, fillValue);
  }

  /// <summary>
  /// Nodes in this graph
  /// </summary>
  public class Node
  {
    private readonly List<Node> _parents;
    private readonly List<Node> _children;

    internal Node(Graph<TId, TSeed> owner, TSeed seed, int index)
    {
      _parents = new List<Node>();
      _children = new List<Node>();
      Owner = owner;
      Seed = seed;
      Parents = _parents.AsReadOnly();
      Children = _children.AsReadOnly();
      Index = index;
    }

    /// <summary>
    /// The owning graph
    /// </summary>
    public Graph<TId, TSeed> Owner { get; }

    /// <summary>
    /// The seed (partial node and node content) for this node
    /// </summary>
    public TSeed Seed { get; }

    /// <summary>
    /// This node's ID
    /// </summary>
    public TId Id { get => Seed.Id; }

    /// <summary>
    /// An index that uniquely identifies the node in its graph
    /// (but has no meaning outside its graph)
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// The number of parents in the seed that could not be matched in the owner graph
    /// </summary>
    public int PrunedParentCount { get; private set; }

    /// <summary>
    /// The parent nodes
    /// </summary>
    public IReadOnlyList<Node> Parents { get; }

    /// <summary>
    /// The child nodes
    /// </summary>
    public IReadOnlyList<Node> Children { get; }

    internal int LinkNodes(bool allowPruning)
    {
      foreach(var parentId in Seed.Parents)
      {
        if(Owner._nodes.TryGetValue(parentId, out var parent))
        {
          _parents.Add(parent);
          parent._children.Add(this);
        }
        else if(allowPruning)
        {
          PrunedParentCount++;
        }
        else
        {
          throw new KeyNotFoundException(
            $"Missing parent {parentId} of node {Id}");
        }
      }
      return PrunedParentCount;
    }
  }

}

/// <summary>
/// Subclass of Graph using CommitSummary as seeds
/// </summary>
public class SummaryGraph: Graph<long, CommitSummary>
{
  /// <summary>
  /// Create a SummaryGraph
  /// </summary>
  public SummaryGraph(
    IEnumerable<CommitSummary> seeds,
    bool allowPruning)
    : base(seeds, allowPruning)
  {
  }
}

/// <summary>
/// Subclass of Graph using CommitEntry as seeds
/// </summary>
public class EntryGraph: Graph<string, CommitEntry>
{
  /// <summary>
  /// Create an EntryGraph
  /// </summary>
  public EntryGraph(IEnumerable<CommitEntry> seeds, bool allowPruning)
    : base(seeds, allowPruning)
  {
  }
}
