/*
 * (c) 2021  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LcGitLib2.GraphModel;

/// <summary>
/// An object that provides minimal information used construct Graph Nodes
/// </summary>
/// <typeparam name="TId">
/// The type used as node identifiers
/// </typeparam>
public interface IGraphNodeSeed<out TId>
  where TId : IEquatable<TId>
{
  /// <summary>
  /// The identifier for this node seed
  /// </summary>
  TId Id { get; }

  /// <summary>
  /// The parents for this node.
  /// </summary>
  IReadOnlyList<TId> Parents { get; }
}

