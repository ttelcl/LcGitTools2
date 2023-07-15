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
/// Minimalistic commit descriptor, containing just IDs and connected IDs
/// </summary>
public class CommitIdNode
{
  private readonly List<GitId> _parents;
  private readonly List<GitId> _children;

  /// <summary>
  /// Create a new empty CommitIdNode
  /// </summary>
  internal CommitIdNode(GitId id)
  {
    Id = id;
    _parents = new List<GitId>();
    _children = new List<GitId>();
    Parents = _parents.AsReadOnly();
    Children = _children.AsReadOnly();
  }

  /// <summary>
  /// The node's ID
  /// </summary>
  public GitId Id { get; init; }

  /// <summary>
  /// The parent nodes
  /// </summary>
  public IReadOnlyList<GitId> Parents { get; init; }

  /// <summary>
  /// The child nodes
  /// </summary>
  public IReadOnlyList<GitId> Children { get; init; }

  /// <summary>
  /// True if this node itself was actually observed
  /// </summary>
  public bool Observed { get; private set; }

  internal void SetParents(IEnumerable<GitId> parents)
  {
    _parents.AddRange(parents);
    Observed = true;
  }

  internal void AddChild(CommitIdNode child)
  {
    _children.Add(child.Id);
  }

  internal void PruneMissingParent(CommitIdNode missingParent)
  {
    _parents.Remove(missingParent.Id);
  }
}
