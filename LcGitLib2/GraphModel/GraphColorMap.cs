/*
 * (c) 2021  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LcGitLib2.GraphModel
{
  /// <summary>
  /// A data structure for "Graph coloring" algorithms.
  /// Colored graphs are represented by GraphNodeMaps where each node maps to an integer value.
  /// </summary>
  public class GraphColorMap<TId, TSeed>: GraphNodeMap<TId, int, TSeed>
    where TId : IEquatable<TId>
    where TSeed : IGraphNodeSeed<TId>
  {
    /// <summary>
    /// Create a new GraphColorMap
    /// </summary>
    public GraphColorMap(Graph<TId, TSeed> owner, int fillValue = 0)
      : base(owner, false)
    {
      foreach(var id in Owner.Ids)
      {
        this[id] = fillValue;
      }
    }

    /// <summary>
    /// Set all nodes reachable from the roots by recursively applying the expander function
    /// to the specified color value.
    /// </summary>
    /// <param name="color">
    /// The value to set the nodes to. 
    /// </param>
    /// <param name="expander">
    /// The function to expand a node to another set of nodes. Typical example:
    /// node => node.Parents(). Consider using PickParents or PickChildren
    /// </param>
    /// <param name="rootIds">
    /// The ids of the nodes to start from
    /// </param>
    /// <returns>
    /// The number of marked nodes
    /// </returns>
    public int ColorRecursive(
      int color,
      Func<Graph<TId, TSeed>.Node, IEnumerable<Graph<TId, TSeed>.Node>> expander,
      IEnumerable<TId> rootIds)
    {
      var stack = new Stack<TId>(rootIds);
      int colored = 0;
      while(stack.Count > 0)
      {
        var id = stack.Pop();
        if(this[id] != color)
        {
          this[id] = color;
          colored++;
          var node = Owner[id];
          foreach(var parent in expander(node))
          {
            stack.Push(parent.Id);
          }
        }
      }
      return colored;
    }

    /// <summary>
    /// Set the mask bits of all nodes reachable from the roots by recursively applying
    /// the expander function to the specified bitmask value.
    /// </summary>
    /// <param name="bitmask">
    /// The bit mask to merge into the values
    /// </param>
    /// <param name="expander">
    /// The function to expand a node to another set of nodes. Typical example:
    /// node => node.Parents(). Consider using PickParents or PickChildren
    /// </param>
    /// <param name="rootIds">
    /// The ids of the nodes to start from
    /// </param>
    /// <returns>
    /// the number of marked nodes
    /// </returns>
    public int MaskRecursive(
      int bitmask,
      Func<Graph<TId, TSeed>.Node, IEnumerable<Graph<TId, TSeed>.Node>> expander,
      IEnumerable<TId> rootIds)
    {
      var stack = new Stack<TId>(rootIds);
      int colored = 0;
      while(stack.Count > 0)
      {
        var id = stack.Pop();
        var v = this[id];
        if((v & bitmask) != bitmask)
        {
          this[id] = v | bitmask;
          colored++;
          var node = Owner[id];
          foreach(var parent in expander(node))
          {
            stack.Push(parent.Id);
          }
        }
      }
      return colored;
    }

    /// <summary>
    /// Function delegate that selects parents from a node
    /// </summary>
    public Func<Graph<TId, TSeed>.Node, IEnumerable<Graph<TId, TSeed>.Node>> PickParents { get; } =
      node => node.Parents;

    /// <summary>
    /// Function delegate that selects children from a node
    /// </summary>
    public Func<Graph<TId, TSeed>.Node, IEnumerable<Graph<TId, TSeed>.Node>> PickChildren { get; } =
      node => node.Children;

    /// <summary>
    /// Count all distinct values
    /// </summary>
    public Dictionary<int, int> CountColors()
    {
      var counters = new Dictionary<int, int>();
      foreach(var value in Values)
      {
        if(!counters.TryGetValue(value, out var count))
        {
          count = 0;
        }
        counters[value] = count + 1;
      }
      return counters;
    }

    /// <summary>
    /// Return the node ids where the map values match the given value
    /// </summary>
    public IEnumerable<TId> WhereValueIs(int value)
    {
      return
        from kvp in this
        where kvp.Value == value
        select kvp.Key;
    }

    /// <summary>
    /// Return the node ids where the map values are in the given range
    /// </summary>
    public IEnumerable<TId> WhereValueBetween(int minValue, int maxValue)
    {
      return
        from kvp in this
        where kvp.Value >= minValue && kvp.Value <= maxValue
        select kvp.Key;
    }

    /// <summary>
    /// Return the node ids where the map values match the bit mask
    /// (that is: where (value &amp; bitmask) == bitMask )
    /// </summary>
    public IEnumerable<TId> WhereBitMask(int bitMask)
    {
      return
        from kvp in this
        where (kvp.Value & bitMask) == bitMask
        select kvp.Key;
    }

  }

}
