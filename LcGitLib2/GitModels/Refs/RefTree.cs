/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace LcGitLib.GitModels.Refs
{
  /// <summary>
  /// A tree of GIT object refs. This is itself a RefTreeNode named "refs",
  /// with no parent
  /// </summary>
  public class RefTree: RefTreeNode
  {
    /// <summary>
    /// Create a new RefTree
    /// </summary>
    public RefTree()
      : base("refs", null)
    {
    }

    /// <summary>
    /// Get or set the GitId value of a ref
    /// </summary>
    /// <param name="path">
    /// The path identifying the ref
    /// </param>
    /// <returns>
    /// The GitId of the node that was found (possibly null), or null if the
    /// ref node wasn't found.
    /// </returns>
    public GitId? this[string path] {
      get => GetNode(path, false)?.Value;
      set {
        GetNode(path, true).Value = value;
      }
    }

    /// <summary>
    /// Get or create a node in the tree, given its full path
    /// </summary>
    /// <param name="path">
    /// The full path of the node, which must start with "refs/"
    /// </param>
    /// <param name="create">
    /// If true: the node is created and inserted if it does not yet exist.
    /// If false (default): the node is not create if it does not exist; null is returned instead
    /// </param>
    /// <returns>
    /// The node that was found or created, or null if not found and <paramref name="create"/>
    /// was false.
    /// </returns>
    public RefTreeNode GetNode(string path, bool create = false)
    {
      if(String.IsNullOrEmpty(path))
      {
        throw new ArgumentOutOfRangeException(
          nameof(path), "Path cannot be empty");
      }
      var parts = path.Split(new char[] { '/' });
      if(parts[0] != "refs")
      {
        // Technically speaking that error message is not correct, but 
        // "must start with 'refs/' or be exactly equal to 'refs'"
        // sounds overly convoluted.
        throw new ArgumentOutOfRangeException(
          nameof(path), "Path must start with 'refs/'");
      }
      RefTreeNode node = this;
      foreach(var part in parts.Skip(1))
      {
        var child = node.GetChild(part, create);
        if(child == null)
        {
          return null;
        }
        node = child;
      }
      return node;
    }

    /// <summary>
    /// Enumerate all refs in the tree that have been assigned a value
    /// </summary>
    public IEnumerable<RefTreeNode> EnumerateAllValuedNodes()
    {
      return
        EnumerateNodes()
        .Where(n => n.Value != null);
    }

    /// <summary>
    /// Try to parse a line of output of "git ls-remote" or "git show-ref" and insert its
    /// information in this tree
    /// </summary>
    /// <param name="line">
    /// The data line in the format {id}{whitespace}{refs/...}
    /// </param>
    /// <param name="node">
    /// On success: the node that was inserted or updated
    /// </param>
    /// <returns>
    /// True if the format of the line looked right and the data was inserted,
    /// false if the content wasn't recognized
    /// </returns>
    public bool TryParseAndInsert(string line, out RefTreeNode node)
    {
      if(String.IsNullOrEmpty(line))
      {
        node = null;
        return false;
      }
      var m = Regex.Match(line, @"^([a-f0-9]{40})\s+(refs(/[^/]+)+)$");
      if(m.Success)
      {
        var id = new GitId(m.Groups[1].Value);
        var path = m.Groups[2].Value;
        node = GetNode(path, true);
        node.Value = id;
        return true;
      }
      else
      {
        node = null;
        return false;
      }
    }

    /// <summary>
    /// Convert this tree to a JSON object
    /// </summary>
    public JObject ToJson()
    {
      var job = new JObject();
      job[ShortName] = ContentToJson();
      return job;
    }

    /// <summary>
    /// Create a deep clone, optionally restricted to nodes passed
    /// by the <paramref name="condition"/> filter.
    /// </summary>
    /// <param name="condition">
    /// If not null: a filter that allows skipping parts of the tree.
    /// Only child nodes for which this returns true are copied.
    /// </param>
    public RefTree Clone(Func<RefTreeNode, bool> condition = null)
    {
      var node = new RefTree();
      CopyChildClones(node, condition);
      return node;
    }
  }
}
