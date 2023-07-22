/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LcGitBup.BundleModel;

/// <summary>
/// Models a consistent stack of bundles (one bundle per tier that references
/// the previous tier)
/// </summary>
public class BundleStack
{
  private readonly List<GitBupBundle> _stack;

  /// <summary>
  /// Create a new BundleStack
  /// </summary>
  internal BundleStack(
    BundleSet owner)
  {
    _stack = new List<GitBupBundle>();
    Owner = owner;
    Tiers = _stack.AsReadOnly();
    Rebuild();
  }

  /// <summary>
  /// Rebuild the stack based on the bundles found in the <see cref="Owner"/>
  /// </summary>
  public void Rebuild()
  {
    var latestBundle = Owner.AllBundles.MaxBy(gbb => gbb.Id);
    _stack.Clear();
    if(latestBundle == null)
    {
      return;
    }
    _stack.Add(latestBundle);
    while(latestBundle != null && latestBundle.Tier > 0)
    {
      var previousBundle = Owner.FindReferencedBundle(latestBundle);
      if(previousBundle == null)
      {
        throw new InvalidOperationException(
          $"The bundle referenced by '{latestBundle.BundleFileName}' is missing");
      }
      _stack.Add(previousBundle);
      latestBundle = previousBundle;
    }
    _stack.Reverse();
  }

  /// <summary>
  /// The BundleSet that this BundleStack builds upon
  /// </summary>
  public BundleSet Owner { get; init; }

  /// <summary>
  /// The depth of the stack: the maximum tier + 1
  /// </summary>
  public int Depth => _stack.Count;

  /// <summary>
  /// A read-only view on the tiers in this stack
  /// </summary>
  public IReadOnlyList<GitBupBundle> Tiers {  get; init; }

  /// <summary>
  /// Check if this stack contains a bundle with the same
  /// tier and id as the given bundle
  /// </summary>
  public bool Contains(GitBupBundle bundle)
  {
    if(bundle.Tier < Depth)
    {
      if(_stack[bundle.Tier].Id == bundle.Id)
      {
        return true;
      }
    }
    return false;
  }
}
