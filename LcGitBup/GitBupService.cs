/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LcGitBup.Configuration;

namespace LcGitBup;

/// <summary>
/// Persists global gitbup settings
/// </summary>
public class GitBupService
{
  private readonly JsonFileObject<GitBupConfig> _globalConfig;

  /// <summary>
  /// Create a new GitBupService object
  /// </summary>
  public GitBupService()
  {
    var appsFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    var gitbupGlobalFolder = Path.Combine(appsFolder, "gitbup");
    if(!Directory.Exists(gitbupGlobalFolder))
    {
      Directory.CreateDirectory(gitbupGlobalFolder);
    }
    var cfgName = Path.Combine(gitbupGlobalFolder, "gitbup.global-config.json");
    _globalConfig = new JsonFileObject<GitBupConfig>(cfgName);
  }

  /// <summary>
  /// Returns a read-only view on the collection of defined anchors.
  /// </summary>
  public IReadOnlyCollection<KeyValuePair<string, string>> Anchors {
    get {
      if(_globalConfig.HasFile && _globalConfig.Content!=null)
      {
        return _globalConfig.Content.AnchorFolders;
      }
      else
      {
        return Array.Empty<KeyValuePair<string, string>>();
      }
    }
  }

  /// <summary>
  /// Add, change or delete an anchor folder mapping
  /// </summary>
  /// <param name="anchorTag">
  /// The name that will be used to identify the anchor folder by the user
  /// </param>
  /// <param name="anchorFolder">
  /// The existing folder to be used as anchor folder, or null to delete
  /// the anchor tag.
  /// </param>
  /// <exception cref="DirectoryNotFoundException">
  /// Thrown when the anchor folder is not null but does not exist.
  /// </exception>
  public void SetAnchor(string anchorTag, string? anchorFolder)
  {
    if(String.IsNullOrEmpty(anchorFolder))
    {
      // delete the anchor, if it exists
      if(!_globalConfig.HasFile || _globalConfig.Content == null)
      {
        // There is no config, so there is nothing to delete.
        // (avoid initializing the config just to delete an entry)
      }
      else
      {
        var cfg = GetConfig();
        cfg.AnchorFolders.Remove(anchorTag);
        _globalConfig.Save();
      }
    }
    else
    {
      if(!Directory.Exists(anchorFolder))
      {
        throw new DirectoryNotFoundException(
          $"Directory not found: '{anchorFolder}'");
      }
      var cfg = GetConfig();
      cfg.AnchorFolders[anchorTag] = anchorFolder;
      _globalConfig.Save();
    }
  }

  /// <summary>
  /// Look up an anchor by tag, returning null if not found
  /// </summary>
  public string? FindAnchor(string anchorTag)
  {
    if(_globalConfig.HasFile && _globalConfig.Content!=null)
    {
      var map = _globalConfig.Content.AnchorFolders;
      return map.TryGetValue(anchorTag, out var anchor) ? anchor : null;
    }
    else
    {
      return null;
    }
  }

  /// <summary>
  /// Get the configuration object. If there was none, a new one is
  /// created with the default settings.
  /// </summary>
  public GitBupConfig GetConfig()
  {
    if(!_globalConfig.HasFile || _globalConfig.Content == null)
    {
      var cfg = new GitBupConfig();
      _globalConfig.Content = cfg;
      _globalConfig.Save();
    }
    return _globalConfig.Content;
  }

  /// <summary>
  /// True if there is any configuration present
  /// </summary>
  public bool HasConfig => _globalConfig.Content != null;

  /// <summary>
  /// The name of the configuration file
  /// </summary>
  public string ConfigurationFile => _globalConfig.FileName;
}
