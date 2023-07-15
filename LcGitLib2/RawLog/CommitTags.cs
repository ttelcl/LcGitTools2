/*
 * (c) 2021  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LcGitLib2.RawLog;

/// <summary>
/// Conversions between full text Commit IDs and 8 byte numerical extract
/// </summary>
public static class CommitTags
{
  /// <summary>
  /// Convert the first 16 characters of the provided commit ID (or other long hex string)
  /// to a 64 bit number. The top bit is forced to 0 to ensure it is a validnon-negative
  /// long integer.
  /// </summary>
  /// <param name="commitId">
  /// The commit ID (or other GIT ID or similar)
  /// </param>
  public static long AsCommitTag(this string commitId)
  {
    if(String.IsNullOrEmpty(commitId) || commitId.Length<16)
    {
      throw new ArgumentOutOfRangeException(
        nameof(commitId),
        "Expecting a 16+ character hex ID as input");
    }
    var longTag =
      UInt64.Parse(commitId.Substring(0, 16), NumberStyles.HexNumber) & 0x7FFFFFFFFFFFFFFFUL;
    return (long)longTag;
  }

  /// <summary>
  /// Convert the CommitEntry to a CommitSummary
  /// </summary>
  public static CommitSummary ToSummary(this CommitEntry e)
  {
    return CommitSummary.FromEntry(e);
  }
}