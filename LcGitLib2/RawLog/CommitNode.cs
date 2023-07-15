/*
 * (c) 2021  VTT / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LcGitLib.RawLog
{
  /// <summary>
  /// Wraps a CommitEntry for use in a graph model
  /// </summary>
  public class CommitNode
  {
    private readonly List<CommitNode> _parents;
    private readonly List<CommitNode> _children;

    /// <summary>
    /// Create a new CommitNode
    /// </summary>
    internal CommitNode(CommitGraph owner, CommitEntry entry)
    {
      Owner = owner;
      Entry = entry;
      _parents = new List<CommitNode>();
      Parents = _parents.AsReadOnly();
      _children = new List<CommitNode>();
      Children = _children.AsReadOnly();
    }

    /// <summary>
    /// The owning Graph
    /// </summary>
    public CommitGraph Owner { get; }

    /// <summary>
    /// The underlying commit entry
    /// </summary>
    public CommitEntry Entry { get; }

    /// <summary>
    /// The commit unique ID
    /// </summary>
    public string Id { get => Entry.CommitId; }

    /// <summary>
    /// The parent nodes
    /// </summary>
    public IReadOnlyList<CommitNode> Parents { get; }

    /// <summary>
    /// The child nodes
    /// </summary>
    public IReadOnlyList<CommitNode> Children { get; }

    /// <summary>
    /// Add the connections between this node and their parents
    /// </summary>
    internal void Connect(bool ignoreMissing)
    {
      foreach(var pe in Entry.Parents)
      {
        if(!Owner.Nodes.TryGetValue(pe, out var pnode))
        {
          if(!ignoreMissing)
          {
            throw new InvalidOperationException(
              $"Graph error: parent '{pe}' of entry '{Id}' is missing");
          }
          else
          {
            // ignore, just do not add
          }
        }
        else
        {
          _parents.Add(pnode);
          pnode.RegisterChild(this);
        }
      }
    }

    private void RegisterChild(CommitNode child)
    {
      _children.Add(child);
    }

  }
}
