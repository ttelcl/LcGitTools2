/*
 * (c) 2021  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LcGitLib2.FileUtilities
{
  /// <summary>
  /// Description of CsvUtilities
  /// </summary>
  public static class CsvUtilities
  {
    private static char[] __badCharacters = "\",\r\n".ToCharArray();

    /// <summary>
    /// Return the CSV-quoted version of the string if it needs quoting,
    /// the original string itself otherwise
    /// </summary>
    public static string CsvQuote(string value)
    {
      if(value.IndexOfAny(__badCharacters)>=0)
      {
        var sb = new StringBuilder();
        sb.Append('"');
        foreach(var ch in value)
        {
          if(ch=='"')
          {
            sb.Append(ch);
            sb.Append(ch);
          }
          else
          {
            sb.Append(ch);
          }
        }
        sb.Append('"');
        return sb.ToString();
      }
      else
      {
        return value;
      }
    }
  }
}
