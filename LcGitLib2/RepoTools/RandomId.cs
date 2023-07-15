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
  /// Generate a new random id, consisting of lower case letters and digits
  /// and starting with an upper case letter
  /// </summary>
  /// <param name="size">
  /// The desired size in characters. Must be in range 3-12. See remarks
  /// section for effective entropy notes.
  /// </param>
  /// <returns>
  /// The newly generated ID. The first character is a letter (a-z: base 26),
  /// subsequent characters are letters or numbers (a-z, 0-9: base 36)
  /// </returns>
  /// <remarks>
  /// <para>
  /// The approximate effective entropy in bits for each of the <paramref name="size"/>
  /// values is:
  /// </para>
  /// <list type="bullet">
  /// <item>3: 15 bits</item>
  /// <item>4: 20 bits</item>
  /// <item>5: 25 bits</item>
  /// <item>6: 30 bits</item>
  /// <item>7: 35 bits</item>
  /// <item>8: 40 bits</item>
  /// <item>9: 46 bits</item>
  /// <item>10: 51 bits</item>
  /// <item>11: 56 bits</item>
  /// <item>12: 61 bits</item>
  /// </list>
  /// </remarks>
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
