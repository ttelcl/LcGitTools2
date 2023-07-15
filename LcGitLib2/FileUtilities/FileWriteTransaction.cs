/*
 * (c) 2021  ttelcl / ttelcl
 */

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LcGitLib2.FileUtilities
{
  /// <summary>
  /// Wraps managing a file to write in a transaction-like wrapper
  /// </summary>
  public partial class FileWriteTransaction
  {

    /// <summary>
    /// Create a new FileWriteTransaction
    /// </summary>
    /// <param name="fileName">
    /// The ultimate name of the file to write
    /// </param>
    public FileWriteTransaction(string fileName)
    {
      FileName = Path.GetFullPath(fileName);
      TempName = FileName + ".tmp";
    }

    /// <summary>
    /// The full path to the final file name
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// The name of the temporary file that is actually being written
    /// </summary>
    public string TempName { get; }

    /// <summary>
    /// Open the stream for binary writing
    /// </summary>
    public Stream Open()
    {
      return File.Create(TempName);
    }

    /// <summary>
    /// Open the stream for text writing
    /// </summary>
    public StreamWriter OpenText()
    {
      return new StreamWriter(Open());
    }

    /// <summary>
    /// Write a single object as JSON to the file (replacing the previously written content)
    /// </summary>
    public void WriteJson(object item, bool indent)
    {
      var json = JsonConvert.SerializeObject(item, indent ? Formatting.Indented : Formatting.None);
      File.WriteAllText(TempName, json);
    }

    /// <summary>
    /// Commits the file content that was written to the stream created by Open().
    /// It is assumed that that stream is closed already before this method is called.
    /// </summary>
    public void Commit()
    {
      if(File.Exists(TempName))
      {
        if(File.Exists(FileName))
        {
          var bakName = FileName + ".bak";
          if(File.Exists(bakName))
          {
            File.Delete(bakName);
          }
          File.Replace(TempName, FileName, bakName);
        }
        else
        {
          File.Move(TempName, FileName);
        }
      }
      else
      {
        throw new InvalidOperationException(
          "There is no intermediate file to commit");
      }
    }

  }
}
