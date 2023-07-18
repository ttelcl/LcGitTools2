/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LcGitLib2.RepoTools;

using LcGitBup.Configuration;
using System.IO;
using System.Diagnostics.CodeAnalysis;

namespace LcGitBup;

/// <summary>
/// Represents a GIT repository for use with gitbup.exe.
/// </summary>
public class GitBupRepo
{
  private readonly JsonFileObject<RepoConfig> _repoConfig;

  /// <summary>
  /// Create a new GitBupRepo instance
  /// </summary>
  internal GitBupRepo(GitBupService owner, GitRepository repository)
  {
    Repository = repository;
    Owner = owner;
    _repoConfig = new JsonFileObject<RepoConfig>(Path.Combine(Repository.GitFolder, "gitbup.repo.json"));
  }

  /// <summary>
  /// The repository info
  /// </summary>
  public GitRepository Repository { get; init; }

  /// <summary>
  /// The owner object, providing the non-repo specific information
  /// </summary>
  public GitBupService Owner { get; init; }

  /// <summary>
  /// The full path of the gitbup repository configuration file
  /// </summary>
  public string RepoConfigName => _repoConfig.FileName;

  /// <summary>
  /// The repository name used for gitbup: the configured name, or the
  /// default if there is no configuration yet.
  /// Use <see cref="ChangeLabel(string)"/> to modify.
  /// </summary>
  public string RepoLabel => HasConfig ? _repoConfig.Content!.RepoName : Repository.Label;

  /// <summary>
  /// True if there is a configuration object loaded
  /// </summary>
  public bool HasConfig => _repoConfig.Content != null;

  /// <summary>
  /// True is there is a configuration object loaded and it defines an existing target folder
  /// </summary>
  public bool HasTarget =>
    _repoConfig.Content != null
    && !String.IsNullOrEmpty(_repoConfig.Content.TargetFolder)
    && Directory.Exists(_repoConfig.Content.TargetFolder);

  /// <summary>
  /// Retrieve the target folder. Returns false if there is no target folder
  /// configured, or if the configured folder does not exist.
  /// </summary>
  /// <param name="target">
  /// The path to the target folder if it was configured. Or null if the target
  /// was not configured. 
  /// </param>
  public bool TryGetTarget([NotNullWhen(true)] out string? target)
  {
    if(_repoConfig.Content != null)
    {
      if(!String.IsNullOrEmpty(_repoConfig.Content.TargetFolder))
      {
        target = _repoConfig.Content.TargetFolder;
        return Directory.Exists(target);
      }
    }
    target = null;
    return false;
  }

  /// <summary>
  /// Change the target folder to be undefined
  /// </summary>
  public void ChangeTargetNone()
  {
    if(_repoConfig.Content != null) // else: it is implicitly unset already
    {
      _repoConfig.Content.TargetFolder = null;
      _repoConfig.Save();
    }
  }

  /// <summary>
  /// Change the target folder to the specified existing folder.
  /// </summary>
  /// <param name="target">
  /// The new target
  /// </param>
  /// <exception cref="InvalidOperationException">
  /// Thrown if the target folder does not exist.
  /// </exception>
  public void ChangeTargetFull(string target)
  {
    target = Path.GetFullPath(target);
    if(!Directory.Exists(target))
    {
      throw new InvalidOperationException(
        $"The target folder does not exist: '{target}'");
    }
    var cfg = GetConfig();
    cfg.TargetFolder = target;
    _repoConfig.Save();
  }

  /// <summary>
  /// Change the target folder to a predefined folder locally inside
  /// the repository.
  /// </summary>
  public string ChangeTargetLocal()
  {
    var configFolder = Path.GetDirectoryName(_repoConfig.FileName);
    if(!Directory.Exists(configFolder))
    {
      throw new InvalidOperationException(
        $"Internal error: {configFolder} does not exist");
    }
    var targetFolder = Path.Combine(configFolder, "gitbup-bundles");
    if(!Directory.Exists(targetFolder))
    {
      Directory.CreateDirectory(targetFolder);
    }
    ChangeTargetFull(targetFolder);
    return targetFolder;
  }

  /// <summary>
  /// Change the target folder to a child of the indicated anchor folder
  /// </summary>
  /// <param name="gbConfig">
  /// The global gitbup configuration, providing the collection of known anchor folders
  /// </param>
  /// <param name="anchorTag">
  /// the tag string string identifying the anchor
  /// </param>
  public void ChangeTargetAnchored(GitBupConfig gbConfig, string anchorTag)
  {
    if(!gbConfig.AnchorFolders.TryGetValue(anchorTag, out var anchorFolder))
    {
      throw new InvalidOperationException(
        $"Unknown anchor folder tag: '{anchorTag}'.");
    }
    if(!Directory.Exists(anchorFolder))
    {
      throw new DirectoryNotFoundException(
        $"The anchor folder '{anchorTag}' no longer exists: {anchorFolder}");
    }
    var targetFolder = Path.Combine(anchorFolder, RepoLabel);
    if(!Directory.Exists(targetFolder))
    {
      Directory.CreateDirectory(targetFolder);
    }
    ChangeTargetFull(targetFolder);
  }

  /// <summary>
  /// Change the label used by gitbup.exe for this repository
  /// </summary>
  public void ChangeLabel(string newLabel)
  {
    if(String.IsNullOrEmpty(newLabel)
      || newLabel.IndexOfAny("\\/:;'\"".ToCharArray())>=0)
    {
      throw new ArgumentOutOfRangeException(
        nameof(newLabel), $"That repo name contains invalid characters: '{newLabel}'");
    }
    if(newLabel.StartsWith('.'))
    {
      throw new ArgumentOutOfRangeException(
        nameof(newLabel), "Invalid repo name (first character cannot be '.')");
    }
    var cfg = GetConfig();
    cfg.RepoName = newLabel;
    _repoConfig.Save();
  }

  /// <summary>
  /// Get the configuration object. If there was none, a new one is
  /// created with the default repo name and no target folder.
  /// </summary>
  public RepoConfig GetConfig()
  {
    if(!_repoConfig.HasFile || _repoConfig.Content == null)
    {
      var cfg = new RepoConfig(
        Repository.Label,
        null);
      _repoConfig.Content = cfg;
      _repoConfig.Save();
    }
    return _repoConfig.Content;
  }


}
