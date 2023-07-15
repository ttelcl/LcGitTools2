/*
 * (c) 2021  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LcGitLib2.RepoTools;

/// <summary>
/// Utility for generating random IDs
/// </summary>
public static class RandomId
{
  private static string __alphabet = "abcdefghijklmnopqrstuvwxyz0123456789";

  /// <summary>
  /// Generate a new random id
  /// </summary>
  /// <param name="size">
  /// The desired size in characters. Must be in range 3-12
  /// </param>
  /// <returns>
  /// The newly generated ID. The first character is a letter (a-z: base 26),
  /// subsequent characters are letters or numbers (a-z, 0-9: base 36)
  /// </returns>
  public static string NewId(int size)
  {
    if(size<3 || size>12)
    {
      throw new ArgumentOutOfRangeException(
        nameof(size),
        "Size must be in the range 3 to 12");
    }
    var idbuffer = new char[size];
    var bytebuffer = new byte[8];
    RandomNumberGenerator.Fill(bytebuffer);
    var rnd = BitConverter.ToUInt64(bytebuffer);
    var chi = (int)(rnd % 26);
    rnd /= 26;
    idbuffer[0] = Char.ToUpper(__alphabet[chi]);
    for(var i = 1; i<size; i++)
    {
      chi = (int)(rnd % 36);
      rnd /= 36;
      idbuffer[i] = __alphabet[chi];
    }
    return new String(idbuffer);
  }

}
