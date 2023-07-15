/*
 * (c) 2021  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LcGitLib2.Cfg;

/// <summary>
/// Something that can be saved and has a "dirty" flag
/// </summary>
public interface ISavable
{
  /// <summary>
  /// True if this needs saving
  /// </summary>
  bool Dirty { get; }

  /// <summary>
  /// True if this item behaves read-only
  /// </summary>
  bool ReadOnly { get; }

  /// <summary>
  /// Set the dirty flag
  /// </summary>
  void SetDirty();

  /// <summary>
  /// Save this if the Dirty flag or force parameter are set.
  /// Clears the Dirty flag
  /// </summary>
  /// <param name="force">
  /// If true: ignore the dirty flag and save anyway
  /// </param>
  void Save(bool force = false);
}

/// <summary>
/// Dummy implementation of ISavable that throw an exception on SetDirty
/// </summary>
public class NotSavable: ISavable
{
  /// <summary>
  /// Always returns false
  /// </summary>
  public bool Dirty { get => false; }

  /// <summary>
  /// Ignored if force is false (and Dirty is false),
  /// throws an exception otherwise
  /// </summary>
  public void Save(bool force = false)
  {
    if(force || Dirty)
    {
      throw new InvalidOperationException(
        "This item is read-only");
    }
  }

  /// <summary>
  /// Throws an exception
  /// </summary>
  public void SetDirty()
  {
    throw new InvalidOperationException(
      "This item is read-only");
  }

  /// <summary>
  /// Always returns true
  /// </summary>
  public bool ReadOnly { get => true; }
}

