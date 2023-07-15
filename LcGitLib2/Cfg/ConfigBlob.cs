/*
 * (c) 2021  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LcGitLib2.Cfg;

/// <summary>
/// A generic extensible, not-really-typed configuration blob, backed by
/// a JSON file
/// </summary>
public class ConfigBlob: ISavable
{
  private JObject _content;

  /// <summary>
  /// Create a new ConfigBlob, and load it, if the backing file exists
  /// </summary>
  public ConfigBlob(
    string backingFile,
    bool readOnly = false)
  {
    BackingFile = Path.GetFullPath(backingFile);
    _content = new JObject();
    Reload();
    ReadOnly = readOnly;
    if(ReadOnly && Dirty)
    {
      Dirty = false;
    }
  }

  /// <summary>
  /// The name of the backing file
  /// </summary>
  public string BackingFile { get; }

  /// <summary>
  /// Return the UTC timestamp of the backing file, or null if it does not exist
  /// </summary>
  public DateTime? TimeStamp()
  {
    return File.Exists(BackingFile) ? File.GetLastWriteTimeUtc(BackingFile) : null;
  }

  /// <summary>
  /// Returns true if this blob's backing file is older than the other file.
  /// Returns false if the other file does not exist.
  /// Returns true if the other file exists but this blob's backing file doesn't.
  /// </summary>
  public bool IsOlderThan(string otherFile)
  {
    if(!File.Exists(otherFile))
    {
      return false;
    }
    if(!File.Exists(BackingFile))
    {
      return true;
    }
    var otherStamp = File.GetLastWriteTimeUtc(otherFile);
    var thisStamp = File.GetLastWriteTimeUtc(BackingFile);
    return thisStamp < otherStamp;
  }

  /// <summary>
  /// Returns true if the backing file exists
  /// </summary>
  public bool Exists { get => File.Exists(BackingFile); }

  /// <summary>
  /// True if there are changes that need saving
  /// </summary>
  public bool Dirty { get; private set; }

  /// <summary>
  /// True if this object acts read-only (implements ISavable)
  /// </summary>
  public bool ReadOnly { get; }

  /// <summary>
  /// The underlying store object. 
  /// </summary>
  public JObject Content { get => _content; }

  /// <summary>
  /// Set the "dirty" flag to true
  /// </summary>
  public void SetDirty()
  {
    Dirty = true;
  }

  /// <summary>
  /// The ConfigObject wrapping the root (reset after a Reload())
  /// </summary>
  public ConfigObject Root { get; private set; }

  /// <summary>
  /// Discard any changes and re-load the backing file if it exists
  /// </summary>
  private void Reload()
  {
    if(File.Exists(BackingFile))
    {
      var json = File.ReadAllText(BackingFile);
      _content = JsonConvert.DeserializeObject<JObject>(json) ?? new JObject();
      Dirty = false;
    }
    else
    {
      _content = new JObject();
      Dirty = true;
    }
    Root = new ConfigObject(this, _content, null);
  }

  /// <summary>
  /// Returns a helper object that calls Save on this ConfigBlob when disposed
  /// </summary>
  /// <param name="force">
  /// If true, the save is unconditional. If false, the object is only saved if
  /// the dirty flag was set.
  /// </param>
  public IDisposable SaveWhenDisposed(bool force = false)
  {
    return new SaveLock(this, force);
  }

  /// <summary>
  /// Save this blob if it is dirty or the 'force' flag is true
  /// </summary>
  public void Save(bool force = false)
  {
    var save = force || Dirty;
    if(save)
    {
      var dir = Path.GetDirectoryName(BackingFile);
      if(!Directory.Exists(dir))
      {
        Directory.CreateDirectory(dir);
      }
      var json = JsonConvert.SerializeObject(_content, Formatting.Indented);
      var tmpName = BackingFile + ".tmp";
      File.WriteAllText(tmpName, json + "\r\n");
      if(File.Exists(BackingFile))
      {
        var bakName = BackingFile + ".bak";
        if(File.Exists(bakName))
        {
          File.Delete(bakName);
        }
        File.Replace(tmpName, BackingFile, bakName);
      }
      else
      {
        File.Move(tmpName, BackingFile);
      }
    }
    Dirty = false;
  }

  /// <summary>
  /// Import the content of another ConfigObject into this blob's root
  /// </summary>
  public void Import(ConfigObject source, bool replace = false)
  {
    Root.Import(source, replace);
  }

  /// <summary>
  /// Import the content of another ConfigBlob into this blob's root
  /// </summary>
  public void Import(ConfigBlob source, bool replace = false)
  {
    Import(source.Root, replace);
  }

  /// <summary>
  /// Export this blob into another blob. Shorthand for target.Import(this, replace).
  /// Does not save the target!
  /// </summary>
  /// <param name="target">
  /// The target to export to
  /// </param>
  /// <param name="replace">
  /// If true, the target is cleared first (resulting in the target becoming a clone
  /// of this blob)
  /// </param>
  public void Export(ConfigBlob target, bool replace = false)
  {
    target.Import(Root, replace);
  }

  /// <summary>
  /// Loads the target blob and exports the content of this blob into it.
  /// Does not save the target!
  /// </summary>
  /// <param name="targetPath">
  /// The path of the target's backing file
  /// </param>
  /// <param name="replace">
  /// If true, the target is cleared first (resulting in the target becoming a clone
  /// of this blob)
  /// </param>
  /// <returns>
  /// The updated target blob (still requires saving)
  /// </returns>
  public ConfigBlob Export(string targetPath, bool replace = false)
  {
    var target = new ConfigBlob(targetPath);
    Export(target, replace);
    return target;
  }

  /// <summary>
  /// View entries as nullable strings.
  /// Entries with a value that is not null or a string throw an exception on read.
  /// </summary>
  public ConfigView<string> Strings { get => Root.Strings; }

  /// <summary>
  /// View entries as nullable integers.
  /// Entries with a value that is not null or an integer throw an exception on read.
  /// </summary>
  public ConfigView<long?> Integers { get => Root.Integers; }

  /// <summary>
  /// View entries as nullable booleans.
  /// Entries with a value that is not null or a boolean throw an exception on read.
  /// </summary>
  public ConfigView<bool?> Booleans { get => Root.Booleans; }

  private class SaveLock: IDisposable
  {
    private readonly ConfigBlob _target;
    private readonly bool _force;

    public SaveLock(ConfigBlob target, bool force)
    {
      _target = target;
      _force = force;
    }

    public void Dispose()
    {
      _target.Save(_force);
    }
  }
}
