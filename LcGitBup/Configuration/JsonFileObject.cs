/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace LcGitBup.Configuration;

/// <summary>
/// Combines a file name and a Json serializable object
/// </summary>
public class JsonFileObject<T> where T : class
{
  /// <summary>
  /// Create a new JsonConfigFile
  /// </summary>
  public JsonFileObject(
    string fileName)
  {
    FileName = Path.GetFullPath(fileName);
    TryLoad();
  }

  /// <summary>
  /// The name of the file to load or save the object
  /// </summary>
  public string FileName { get; init; }

  /// <summary>
  /// The target object, possibly null
  /// </summary>
  public T? Content { get; set; } 

  /// <summary>
  /// True if the backing file exists
  /// </summary>
  public bool HasFile { get => File.Exists(FileName); }

  /// <summary>
  /// Try to load the object from the file, returning true
  /// on success. On failure <see cref="Content"/> is set to null.
  /// </summary>
  /// <returns></returns>
  public bool TryLoad()
  {
    if(File.Exists(FileName))
    {
      var json = File.ReadAllText(FileName);
      if(!String.IsNullOrEmpty(json))
      {
        Content = JsonConvert.DeserializeObject<T>(json);
        return true;
      }
    }
    Content = null;
    return false;
  }

  /// <summary>
  /// Save the file, or delete the file if <see cref="Content"/> is null
  /// </summary>
  public void Save()
  {
    if(Content == null)
    {
      File.Delete(FileName);
    }
    else
    {
      var json = JsonConvert.SerializeObject(Content, Formatting.Indented);
      File.WriteAllText(FileName, json);
    }
  }
}
