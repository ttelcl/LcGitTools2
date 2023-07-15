/*
 * (c) 2021  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LcGitLib2.GraphModel;

/// <summary>
/// A mutable graph
/// </summary>
public class MutableGraph<TId, TSeed>
  where TSeed : IGraphNodeSeed<TId>
  where TId : IEquatable<TId>
{
  private Dictionary<TId, MutableNode> _nodes;

  /// <summary>
  /// Create a new Subgraph
  /// </summary>
  public MutableGraph()
  {
    _nodes = new Dictionary<TId, MutableNode>();
  }

  /// <summary>
  /// Create a MutableGraph from all seeds in a Graph, optionally
  /// connecting all edges
  /// </summary>
  public static MutableGraph<TId, TSeed> FromGraph(
    Graph<TId, TSeed> graph,
    bool connectEdges = true)
  {
    var mg = new MutableGraph<TId, TSeed>();
    mg.ImportNodes(from node in graph.Nodes select node.Seed);
    if(connectEdges)
    {
      mg.RestoreDefaultEdges();
    }
    return mg;
  }

  /// <summary>
  /// Add a new node to the graph, wrapping the given seed
  /// </summary>
  public MutableNode NewNode(TSeed seed)
  {
    if(_nodes.ContainsKey(seed.Id))
    {
      throw new InvalidOperationException(
        $"The graph already contains a node for ID {seed.Id}");
    }
    var node = new MutableNode(this, seed);
    _nodes[node.Id] = node;
    return node;
  }

  /// <summary>
  /// Add a new node to the graph, wrapping the given seed, or return
  /// the existing node
  /// </summary>
  public MutableNode MapNode(TSeed seed)
  {
    if(_nodes.TryGetValue(seed.Id, out var node))
    {
      return node;
    }
    else
    {
      node = new MutableNode(this, seed);
      _nodes[node.Id] = node;
      return node;
    }
  }

  /// <summary>
  /// Lookup the node for the given ID, returning null if not found
  /// </summary>
  public MutableNode this[TId id] {
    get {
      if(_nodes.TryGetValue(id, out var node))
      {
        return node;
      }
      else
      {
        return null;
      }
    }
  }

  /// <summary>
  /// The collection of the nodes of this MutableGraph
  /// </summary>
  public IReadOnlyCollection<MutableNode> Nodes { get => _nodes.Values; }

  /// <summary>
  /// The collection of the node IDs of this MutableGraph
  /// </summary>
  public IReadOnlyCollection<TId> NodeIds { get => _nodes.Keys; }

  /// <summary>
  /// Adds an edge between the child and the parent, if it didn't exist yet.
  /// Fails if the nodes are missing, unless ignoreMissingNodes is true
  /// (in which case nothing is added)
  /// </summary>
  public bool AddEdge(TId childId, TId parentId, bool ignoreMissingNodes = false)
  {
    if(!_nodes.TryGetValue(childId, out var child))
    {
      if(ignoreMissingNodes)
      {
        return false;
      }
      throw new InvalidOperationException(
        $"The child node was not added to this graph yet: {childId}");
    }
    if(!_nodes.TryGetValue(parentId, out var parent))
    {
      if(ignoreMissingNodes)
      {
        return false;
      }
      throw new InvalidOperationException(
        $"The child node was not added to this graph yet: {parentId}");
    }
    var addedChild = child.AddParent(parent);
    if(addedChild)
    {
      return parent.AddChild(child);
    }
    else
    {
      return false;
    }
  }

  /// <summary>
  /// Remove the edge betwen a child and a parent, if it exists
  /// </summary>
  public bool RemoveEdge(TId childId, TId parentId)
  {
    var child = this[childId];
    var parent = this[parentId];
    if(child != null && parent != null)
    {
      return child.RemoveParent(parentId) && parent.RemoveChild(childId);
    }
    else
    {
      return false;
    }
  }

  /// <summary>
  /// Remove a node and all of its edges
  /// </summary>
  public bool RemoveNode(TId id)
  {
    var node = this[id];
    if(node!=null)
    {
      var parents = node.Parents.ToList();
      foreach(var parent in parents)
      {
        RemoveEdge(id, parent.Id);
      }
      var children = node.Children.ToList();
      foreach(var child in children)
      {
        RemoveEdge(child.Id, id);
      }
      _nodes.Remove(id);
      return true;
    }
    else
    {
      return false;
    }
  }

  /// <summary>
  /// Restores all edges specified in the seeds for which both nodes
  /// are present
  /// </summary>
  public void RestoreDefaultEdges()
  {
    foreach(var node in _nodes.Values)
    {
      foreach(var parentId in node.Seed.Parents)
      {
        var parent = this[parentId];
        if(parent!=null)
        {
          AddEdge(node.Id, parentId);
        }
      }
    }
  }

  /// <summary>
  /// Add nodes for all seeds that are not already present, and optionally
  /// create the default edges
  /// </summary>
  public List<MutableNode> ImportNodes(IEnumerable<TSeed> seeds)
  {
    var list = new List<MutableNode>();
    foreach(var seed in seeds)
    {
      list.Add(MapNode(seed));
    }
    return list;
  }

  /// <summary>
  /// Use this MutableGraph to run Khan's algorithm to create a topological sort
  /// of the nodes from tips to roots.
  /// This MutableGraph will be partially destroyed in the process.
  /// </summary>
  public List<TSeed> TopologicalSortDestructive()
  {
    // Based on https://en.wikipedia.org/wiki/Topological_sorting
    // Edges are interpreted as going from a child to a parent,
    // so a node's incoming edges are node.Children and outgoing
    // edges are node.Parents
    var list = new List<TSeed>();
    var s = new Stack<MutableNode>();
    foreach(var node in Nodes)
    {
      if(node.Children.Count==0)
      {
        s.Push(node);
      }
    }
    while(s.Count>0)
    {
      var n = s.Pop();
      list.Add(n.Seed);
      var parents = n.Parents.ToList(); // need to make a copy to allow iteration while mutating
      foreach(var m in parents)
      {
        RemoveEdge(n.Id, m.Id);
        if(m.Children.Count==0)
        {
          s.Push(m);
        }
      }
    }
    var cycleNodes =
      Nodes
      .Where(node => node.Parents.Count > 0 || node.Children.Count > 0)
      .ToList();
    if(cycleNodes.Count>0)
    {
      throw new InvalidOperationException(
        $"Cannot determine topological sort: there are {cycleNodes.Count} nodes in cycles");
    }
    return list;
  }

  /// <summary>
  /// A node in this graph
  /// </summary>
  public class MutableNode
  {
    private readonly List<MutableNode> _parents;
    private readonly List<MutableNode> _children;

    internal MutableNode(MutableGraph<TId, TSeed> host, TSeed seed)
    {
      _parents = new List<MutableNode>();
      _children = new List<MutableNode>();
      Parents = _parents.AsReadOnly();
      Children = _children.AsReadOnly();
      Host = host;
      Seed = seed;
    }

    /// <summary>
    /// The hosting MutableGraph
    /// </summary>
    public MutableGraph<TId, TSeed> Host { get; }

    /// <summary>
    /// The seed defining the node's identity and default parents
    /// </summary>
    public TSeed Seed { get; }

    /// <summary>
    /// The node's ID
    /// </summary>
    public TId Id { get => Seed.Id; }

    /// <summary>
    /// The parent nodes
    /// </summary>
    public IReadOnlyList<MutableNode> Parents { get; }

    /// <summary>
    /// The child nodes
    /// </summary>
    public IReadOnlyList<MutableNode> Children { get; }

    /// <summary>
    /// Find the parent node with the given ID, returning null if not found
    /// </summary>
    public MutableNode Parent(TId id)
    {
      return _parents.FirstOrDefault(p => p.Id.Equals(id));
    }

    /// <summary>
    /// Find the child node with the given ID, returning null if not found
    /// </summary>
    public MutableNode Child(TId id)
    {
      return _children.FirstOrDefault(p => p.Id.Equals(id));
    }

    /// <summary>
    /// Adds the node to the parents list if it was not there yet
    /// </summary>
    /// <param name="parent">
    /// The node to add
    /// </param>
    /// <returns>
    /// True if the node was added, false if it was there already
    /// </returns>
    internal bool AddParent(MutableNode parent)
    {
      if(Parent(parent.Id) == null)
      {
        _parents.Add(parent);
        return true;
      }
      else
      {
        return false;
      }
    }

    /// <summary>
    /// Adds the node to the children list if it was not there yet
    /// </summary>
    /// <param name="child">
    /// The node to add
    /// </param>
    /// <returns>
    /// True if the node was added, false if it was there already
    /// </returns>
    internal bool AddChild(MutableNode child)
    {
      if(Child(child.Id) == null)
      {
        _children.Add(child);
        return true;
      }
      else
      {
        return false;
      }
    }

    /// <summary>
    /// Remove a parent by ID
    /// </summary>
    internal bool RemoveParent(TId id)
    {
      var parent = Parent(id);
      if(parent!=null)
      {
        return _parents.Remove(parent);
      }
      else
      {
        return false;
      }
    }

    /// <summary>
    /// Remove a child by ID
    /// </summary>
    internal bool RemoveChild(TId id)
    {
      var child = Child(id);
      if(child != null)
      {
        return _children.Remove(child);
      }
      else
      {
        return false;
      }
    }

  }

}
