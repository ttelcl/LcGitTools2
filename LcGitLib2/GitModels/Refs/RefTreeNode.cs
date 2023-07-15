/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace LcGitLib.GitModels.Refs
{
  /// <summary>
  /// A node in a namespace tree of refs
  /// </summary>
  public class RefTreeNode
  {
    private readonly Dictionary<string, RefTreeNode> _children;

    /// <summary>
    /// Create a new RefTreeNode
    /// </summary>
    internal RefTreeNode(string shortName, RefTreeNode parent)
    {
      if(String.IsNullOrEmpty(shortName))
      {
        throw new ArgumentOutOfRangeException(nameof(shortName), "A ref path segment name cannot be empty");
      }
      if(shortName.Contains('/'))
      {
        throw new ArgumentOutOfRangeException(nameof(shortName), "A ref path segment name cannot contain '/'");
      }
      Parent = parent;
      ShortName = shortName;
      _children = new Dictionary<string, RefTreeNode>();
      if(parent == null)
      {
        FullName = shortName;
      }
      else
      {
        FullName = parent.FullName + "/" + shortName;
      }
    }

    /// <summary>
    /// The parent node (possibly null)
    /// </summary>
    public RefTreeNode Parent { get; init; }

    /// <summary>
    /// The short name of this node (path excluded)
    /// </summary>
    public string ShortName { get; init; }

    /// <summary>
    /// The full name of the node
    /// </summary>
    public string FullName { get; init; }

    /// <summary>
    /// Get or set the target object id, or null if there is none
    /// </summary>
    public GitId? Value { get; set; }

    /// <summary>
    /// Retrieve an existing direct child node or optionally create a new one
    /// </summary>
    /// <param name="name">
    /// The child node's name
    /// </param>
    /// <param name="create">
    /// Whether or not to create the node if it didn't exist yet.
    /// Default false.
    /// </param>
    /// <returns>
    /// The existing node, or the new node if it was created, or null
    /// if the node did not exist and <paramref name="create"/> was false.
    /// </returns>
    public RefTreeNode GetChild(string name, bool create = false)
    {
      if(_children.TryGetValue(name, out RefTreeNode child))
      {
        return child;
      }
      else
      {
        if(create)
        {
          var node = new RefTreeNode(name, this);
          _children.Add(name, node);
          return node;
        }
        else
        {
          return null;
        }
      }
    }

    /// <summary>
    /// Test if this node's full name starts with the given prefix
    /// </summary>
    /// <param name="prefix">
    /// The prefix to test for
    /// </param>
    /// <param name="tail">
    /// On match: the part of the full path after the prefix.
    /// Otherwise null
    /// </param>
    /// <returns>
    /// True if match, false otherwise
    /// </returns>
    public bool HasPrefix(string prefix, out string tail)
    {
      if(FullName.StartsWith(prefix))
      {
        tail = FullName.Substring(prefix.Length);
        return true;
      }
      else
      {
        tail = null;
        return false;
      }
    }

    /// <summary>
    /// Remove this node from its parent
    /// </summary>
    public void Delete()
    {
      if(Parent == null)
      {
        throw new InvalidOperationException(
          "Cannot delete the root of a tree");
      }
      Parent.DeleteChild(this);
    }

    internal void DeleteChild(RefTreeNode child)
    {
      _children.Remove(child.ShortName);
    }

    /// <summary>
    /// The children of this node
    /// </summary>
    public IReadOnlyCollection<RefTreeNode> Children { get => _children.Values; }

    /// <summary>
    /// Enumerate this and all descencent nodes
    /// </summary>
    public IEnumerable<RefTreeNode> EnumerateNodes()
    {
      yield return this;
      foreach(var node in Children.SelectMany(n => n.EnumerateNodes()))
      {
        yield return node;
      }
    }

    /// <summary>
    /// Convert the content of this node to a JSON representation.
    /// </summary>
    /// <returns>
    /// If the node has no children: a JSON string value or JSON null value. If 
    /// it has children: an object. If it has both: an object, with an extra
    /// key that carries the value.
    /// </returns>
    public JToken ContentToJson()
    {
      if(_children.Count > 0)
      {
        var job = new JObject();
        if(Value.HasValue)
        {
          job[""] = Value.Value.Id;
        }
        foreach(var child in Children)
        {
          job[child.ShortName] = child.ContentToJson();
        }
        return job;
      }
      else
      {
        if(Value.HasValue)
        {
          return new JValue(Value.Value.Id);
        }
        else
        {
          return JValue.CreateNull();
        }
      }
    }

    internal void CopyChildClones(RefTreeNode model, Func<RefTreeNode, bool> condition)
    {
      foreach(var modelChild in model.Children)
      {
        if(condition != null && condition(modelChild))
        {
          var child = GetChild(modelChild.ShortName, true);
          child.CopyChildClones(modelChild, condition);
        }
      }
    }
  }
}
