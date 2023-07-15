/*
 * (c) 2021  VTT / ttelcl
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace LcGitLib.GraphModel
{
  /// <summary>
  /// A mapping from graph nodes (and graph node IDs) to some kind of value
  /// </summary>
  public class GraphNodeMap<TId, TValue>: IReadOnlyDictionary<TId, TValue>
    where TId : IEquatable<TId>
  {
    private readonly Dictionary<TId, TValue> _values;

    /// <summary>
    /// Create a new GraphNodeMap
    /// </summary>
    public GraphNodeMap(
      bool defaultWhenMissing)
    {
      _values = new Dictionary<TId, TValue>();
      DefaultWhenMissing = defaultWhenMissing;
    }

    /// <summary>
    /// If true, retrieving a value that is not present returns the default for
    /// TValue. If false, an exception is thrown instead.
    /// </summary>
    public bool DefaultWhenMissing { get; }

    /// <summary>
    /// Get the value for the given ID. This overload ignores the DefaultWhenMissing
    /// flag, using an explicit flag instead.
    /// </summary>
    /// <param name="id">
    /// The ID to lookup
    /// </param>
    /// <param name="defaultWhenMissing">
    /// If true, retrieving a value that is not present returns the default for
    /// TValue. If false, an exception is thrown instead.
    /// </param>
    public TValue this[TId id, bool defaultWhenMissing]
    {
      get {
        if(_values.TryGetValue(id, out var value))
        {
          return value;
        }
        else if(defaultWhenMissing)
        {
          return default(TValue);
        }
        else
        {
          throw new KeyNotFoundException(
            $"Missing graph node key: {id}");
        }
      }
    }

    /// <summary>
    /// Get or set the value for the given ID, using the missing value behaviour as
    /// implied by DefaultWhenMissing.
    /// </summary>
    /// <param name="id">
    /// The ID to lookup.
    /// </param>
    public TValue this[TId id]
    {
      get => this[id, DefaultWhenMissing];
      set {
        _values[id] = value;
      }
    }

    /// <summary>
    /// Remove a value by ID
    /// </summary>
    public bool Remove(TId id)
    {
      return _values.Remove(id);
    }

    /// <summary>
    /// Enumerates the keys in this map
    /// </summary>
    public IEnumerable<TId> Keys => _values.Keys;

    /// <summary>
    /// Enumerates the values in this map
    /// </summary>
    public IEnumerable<TValue> Values => _values.Values;

    /// <summary>
    /// Returns the number of items in this map
    /// </summary>
    public int Count => _values.Count;

    /// <summary>
    /// Test if the id is in this map
    /// </summary>
    public bool ContainsKey(TId key)
    {
      return _values.ContainsKey(key);
    }

    /// <summary>
    /// Try to get the value for the given ID, if available
    /// </summary>
    public bool TryGetValue(TId key, [MaybeNullWhen(false)] out TValue value)
    {
      return _values.TryGetValue(key, out value);
    }

    /// <summary>
    /// Enumerate the key-value pairs in this map
    /// </summary>
    public IEnumerator<KeyValuePair<TId, TValue>> GetEnumerator()
    {
      return _values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    // Nice try, but indexers cannot be generic themselves
    //public TValue this<TSeed>[Graph<TSeed, TId>.Node node]
    //  where TSeed : IGraphNodeSeed<TId>
    //{
    //  get {
    //    return this[node.Id];
    //  }
    //}

  }

  /// <summary>
  /// Specialization of GraphNodeMap{TId, TValue} for use with Graph{TSeed, TId}
  /// </summary>
  /// <typeparam name="TId">
  /// The type used as keys for nodes and values in this map
  /// </typeparam>
  /// <typeparam name="TValue">
  /// The type of values in this map
  /// </typeparam>
  /// <typeparam name="TSeed">
  /// The seed type embedded in nodes
  /// </typeparam>
  public class GraphNodeMap<TId, TValue, TSeed>:
    GraphNodeMap<TId, TValue>
    where TId : IEquatable<TId>
    where TSeed : IGraphNodeSeed<TId>
  {
    /// <summary>
    /// Create a new GraphNodeMap
    /// </summary>
    public GraphNodeMap(
      Graph<TId, TSeed> owner,
      bool defaultWhenMissing)
      : base(defaultWhenMissing)
    {
      Owner = owner;
    }

    /// <summary>
    /// The owning Graph. All nodes in the set must be part of this graph
    /// </summary>
    public Graph<TId, TSeed> Owner { get; }

    /// <summary>
    /// Get or set the value for the given node
    /// </summary>
    public TValue this[Graph<TId, TSeed>.Node node]
    {
      get => this[node.Id];
      set {
        this[node.Id] = value;
      }
    }

    /// <summary>
    /// Get the value for the given node, overriding the default DefaultWhenMissing mode
    /// </summary>
    public TValue this[Graph<TId, TSeed>.Node node, bool defaultWhenMissing]
    {
      get => this[node.Id, defaultWhenMissing];
    }

    /// <summary>
    /// Test if the node is in this map
    /// </summary>
    public bool ContainsKey(Graph<TId, TSeed>.Node node)
    {
      return ContainsKey(node.Id);
    }

    /// <summary>
    /// Try to get the value for the given node, if available
    /// </summary>
    public bool TryGetValue(Graph<TId, TSeed>.Node node, [MaybeNullWhen(false)] out TValue value)
    {
      return TryGetValue(node.Id, out value);
    }

    /// <summary>
    /// Remove a value by node ID
    /// </summary>
    public bool Remove(Graph<TId, TSeed>.Node node)
    {
      return Remove(node.Id);
    }

  }

  /// <summary>
  /// A specialized GraphNodeMap where the values are nodes themselves
  /// </summary>
  public class GraphNodeSet<TId, TSeed>: GraphNodeMap<TId, Graph<TId, TSeed>.Node, TSeed>
    where TId : IEquatable<TId>
    where TSeed : IGraphNodeSeed<TId>
  {
    /// <summary>
    /// Create a new empty GraphNodeSet
    /// </summary>
    public GraphNodeSet(
      Graph<TId, TSeed> owner,
      bool nullWhenMissing = false)
      : base(owner, nullWhenMissing)
    {
    }

    /// <summary>
    /// Insert a node in this set. If the argument is null, nothing is inserted.
    /// If the argument does not belong to this set's graph it is looked up first.
    /// </summary>
    /// <param name="node">
    /// The node to insert (or match and insert)
    /// </param>
    /// <returns>
    /// The node that was actually inserted, or null if the argument was null.
    /// </returns>
    public Graph<TId, TSeed>.Node Insert(Graph<TId, TSeed>.Node node)
    {
      if(node != null)
      {
        if(node.Owner != Owner)
        {
          node = Owner[node.Id];
        }
        this[node.Id] = node;
      }
      return node;
    }

    /// <summary>
    /// Insert a node by ID. An exception is thrown if the node ID is unknown
    /// </summary>
    public Graph<TId, TSeed>.Node Insert(TId id)
    {
      var node = Owner[id];
      this[node.Id] = node;
      return node;
    }

    /// <summary>
    /// Insert multiple nodes into this set
    /// </summary>
    /// <param name="ids">
    /// The ids of the nodes to insert
    /// </param>
    /// <param name="skipMissing">
    /// If false, missing nodes cause a KeyNotFound
    /// If true, missing nodes are ignored
    /// </param>
    /// <returns>
    /// Returns this set itself, for fluent APIs
    /// </returns>
    public GraphNodeSet<TId, TSeed> Insert(IEnumerable<TId> ids, bool skipMissing = false)
    {
      foreach(var node in Owner.FindMany(ids, skipMissing))
      {
        this[node.Id] = node;
      }
      return this;
    }

    /// <summary>
    /// Remove multiple nodes
    /// </summary>
    /// <param name="ids">
    /// The IDs of the nodes to remove. Missing IDs are ignored silently
    /// </param>
    /// <returns>
    /// Returns this set itself, for fluent APIs
    /// </returns>
    public GraphNodeSet<TId, TSeed> Remove(IEnumerable<TId> ids)
    {
      foreach(var id in ids)
      {
        Remove(id);
      }
      return this;
    }




  }

}
