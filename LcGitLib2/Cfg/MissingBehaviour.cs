/*
 * (c) 2021  VTT / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LcGitLib.Cfg
{
  /// <summary>
  /// Options for behaviour when a requested item is missing
  /// </summary>
  public enum MissingBehaviour
  {
    /// <summary>
    /// Return null if the item is missing
    /// </summary>
    ReturnNull = 0,

    /// <summary>
    /// Insert a new item and return that new item if it was missing
    /// </summary>
    Create = 1,

    /// <summary>
    /// Throw an exception if the item is missing
    /// </summary>
    Abort = 2,
  }
}
