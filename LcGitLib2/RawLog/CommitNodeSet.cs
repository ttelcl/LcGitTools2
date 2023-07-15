/*
 * (c) 2021  ttelcl / ttelcl
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LcGitLib2.RawLog
{
  /// <summary>
  /// An unordered set of CommitNodes, a subset of the nodes in a CommitGraph
  /// </summary>
  public class CommitNodeSet: ICollection<CommitNode>
  {
    private readonly Dictionary<string, CommitNode> _entries;

    /// <summary>
    /// Create a new CommitNodeSet
    /// </summary>
    internal CommitNodeSet(CommitGraph owner, IEnumerable<CommitNode> nodes=null)
    {
      Owner = owner;
      _entries = new Dictionary<string, CommitNode>();
      Nodes = _entries.Values;
      if(nodes!=null)
      {
        foreach(var node in nodes)
        {
          Add(node);
        }
      }
    }

    /// <summary>
    /// The owning graph
    /// </summary>
    public CommitGraph Owner { get; }

    /// <summary>
    /// The nodes in the collection
    /// </summary>
    public IReadOnlyCollection<CommitNode> Nodes { get; }
    
    /// <summary>
    /// The number of nodes in this set
    /// </summary>
    public int Count { get => _entries.Count; }
    
    /// <summary>
    /// Implements ICollection
    /// </summary>
    public bool IsReadOnly { get => false; }

    /// <summary>
    /// Export this set as a HasSet of ID strings
    /// </summary>
    public HashSet<string> ToIdSet()
    {
      return _entries.Keys.ToHashSet();
    }

    /// <summary>
    /// Add a single node
    /// </summary>
    /// <param name="node">
    /// The node (from the same owner graph)
    /// </param>
    /// <returns>
    /// True if the node was added, false if it was already present
    /// </returns>
    public bool Add(CommitNode node)
    {
      if(node.Owner!=Owner)
      {
        throw new InvalidOperationException(
          "Attempt to mix nodes from different graphs");
      }
      if(!_entries.ContainsKey(node.Id))
      {
        _entries[node.Id] = node;
        return true;
      }
      else
      {
        return false;
      }
    }

    /// <summary>
    /// Add a node by its ID
    /// </summary>
    public bool Add(string id)
    {
      if(!_entries.ContainsKey(id))
      {
        var node = Owner.Nodes[id];
        _entries[node.Id] = node;
        return true;
      }
      else
      {
        return false;
      }
    }

    void ICollection<CommitNode>.Add(CommitNode item)
    {
      Add(item);
    }

    /// <summary>
    /// Remove a node
    /// </summary>
    public bool Remove(CommitNode node)
    {
      if(node.Owner != Owner)
      {
        throw new InvalidOperationException(
          "Attempt to mix nodes from different graphs");
      }
      return Remove(node.Id);
    }

    /// <summary>
    /// Remove a node by its ID
    /// </summary>
    public bool Remove(string id)
    {
      return _entries.Remove(id);
    }

    /// <summary>
    /// Add all of the nodes
    /// </summary>
    public void AddRange(IEnumerable<CommitNode> nodes)
    {
      foreach(var node in nodes)
      {
        Add(node);
      }
    }

    /// <summary>
    /// Add all of the nodes
    /// </summary>
    public void AddRange(IEnumerable<string> ids)
    {
      foreach(var id in ids)
      {
        Add(id);
      }
    }

    /// <summary>
    /// Remove all the nodes by the given ids
    /// </summary>
    public void RemoveRange(IEnumerable<string> ids)
    {
      foreach(var id in ids)
      {
        _entries.Remove(id);
      }
    }

    /// <summary>
    /// True if the node is in this set
    /// </summary>
    public bool Contains(CommitNode node)
    {
      if(node.Owner != Owner)
      {
        throw new InvalidOperationException(
          "Attempt to mix nodes from different graphs");
      }
      return _entries.ContainsKey(node.Id);
    }

    /// <summary>
    /// True if the node with the given ID is in this set
    /// </summary>
    public bool Contains(string id)
    {
      return _entries.ContainsKey(id);
    }

    /// <summary>
    /// Returns the nodes of the input sequence that are contained in this set
    /// </summary>
    public IEnumerable<CommitNode> Contained(IEnumerable<CommitNode> nodes)
    {
      foreach(var node in nodes)
      {
        if(_entries.ContainsKey(node.Id))
        {
          yield return node;
        }
      }
    }

    /// <summary>
    /// Returns the nodes of the input sequence that are not contained in this set
    /// </summary>
    public IEnumerable<CommitNode> Missing(IEnumerable<CommitNode> nodes)
    {
      foreach(var node in nodes)
      {
        if(!_entries.ContainsKey(node.Id))
        {
          yield return node;
        }
      }
    }

    /// <summary>
    /// Returns the node ids of the input sequence that are contained in this set
    /// </summary>
    public IEnumerable<string> Contained(IEnumerable<string> ids)
    {
      foreach(var id in ids)
      {
        if(_entries.ContainsKey(id))
        {
          yield return id;
        }
      }
    }

    /// <summary>
    /// Returns the node ids of the input sequence that are not contained in this set
    /// </summary>
    public IEnumerable<string> Missing(IEnumerable<string> ids)
    {
      foreach(var id in ids)
      {
        if(!_entries.ContainsKey(id))
        {
          yield return id;
        }
      }
    }

    /// <summary>
    /// Enumerate nodes in this collection
    /// </summary>
    public IEnumerator<CommitNode> GetEnumerator()
    {
      return _entries.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    /// <summary>
    /// Implements ICollection
    /// </summary>
    public void Clear()
    {
      _entries.Clear();
    }

    /// <summary>
    /// Implements ICollection
    /// </summary>
    public void CopyTo(CommitNode[] array, int arrayIndex)
    {
      var i = arrayIndex;
      foreach(var node in _entries.Values)
      {
        array[i++] = node;
      }
    }

  }
}
