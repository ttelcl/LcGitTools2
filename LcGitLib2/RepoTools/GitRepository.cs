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

using LcGitLib2.Cfg;
using LcGitLib2.GitConfigFiles;
using LcGitLib2.GitRunning;
using LcGitLib2.GraphModel;
using LcGitLib2.RawLog;

namespace LcGitLib2.RepoTools;

/// <summary>
/// Utility class representing a GIT repository, acting as entry point API
/// </summary>
public class GitRepository
{
  private string? _cachedLabel;

  /// <summary>
  /// Create a new GitRepository object, wrapping the repository with the given GIT folder.
  /// Consider using GitRepository.Locate() instead of this constructor
  /// </summary>
  public GitRepository(string gitFolder, string? label = null)
  {
    _cachedLabel = label;
    gitFolder = Path.GetFullPath(gitFolder);
    if(!Directory.Exists(gitFolder))
    {
      throw new DirectoryNotFoundException(
        $"Directory not found: {gitFolder}");
    }
    var kind = RepoUtilities.LooksLikeGitFolder(gitFolder);
    switch(kind)
    {
      default:
      case 0:
        throw new ArgumentOutOfRangeException(
          nameof(gitFolder),
          $"That folder does not look like a GIT folder: {gitFolder}");
      case 1:
        throw new NotSupportedException(
          $"Unsupported GIT folder name. Expecting the name to be '.git' or end with '.git': {gitFolder}");
      case 2:
        Bare = true;
        GitFolder = gitFolder;
        RepoFolder = null;
        break;
      case 3:
        Bare = false;
        GitFolder = gitFolder;
        RepoFolder = Path.GetDirectoryName(GitFolder);
        break;
    }
    LcGitFolder = Path.Combine(GitFolder, ".lcgitlib");
  }

  /// <summary>
  /// Locate the GIT folder for the repository containing the given anchor folder.
  /// Throws an exception if not found, unless mustExist is false.
  /// </summary>
  /// <param name="anchorFolder">
  /// The folder from where to start searching for a surrounding GIT repository.
  /// </param>
  /// <param name="mustExist">
  /// Determines the behaviour when no GIT repository is found: if true, an exception
  /// is thrown; if false null is returned.
  /// </param>
  public static GitRepository? Locate(string anchorFolder, bool mustExist = true)
  {
    var gitFolder = RepoUtilities.FindGitFolder(anchorFolder);
    if(gitFolder==null)
    {
      if(mustExist)
      {
        throw new InvalidOperationException(
          $"The folder is not part of a GIT repository: {anchorFolder}");
      }
      else
      {
        return null;
      }
    }
    else
    {
      return new GitRepository(gitFolder);
    }
  }

  /// <summary>
  /// True if this is a bare repository (based on the git repository name)
  /// </summary>
  public bool Bare { get; }

  /// <summary>
  /// The GIT administrative folder for the repository
  /// </summary>
  public string GitFolder { get; }

  /// <summary>
  /// The folder with our own additional files for this repo
  /// </summary>
  public string LcGitFolder { get; }

  /// <summary>
  /// The main repository folder (working directory), or null for Bare repositories.
  /// </summary>
  public string? RepoFolder { get; }

  /// <summary>
  /// The repository label, derived from the folder name. It can also be set
  /// in the constructor, or be set at any moment. Setting it to null causes
  /// it to be populated based on directory name.
  /// </summary>
  public string Label {
    get {
      if(String.IsNullOrEmpty(_cachedLabel))
      {
        var info = this.LoadInfo(false);
        _cachedLabel = info?.Label ?? RepoUtilities.RepoLabel(GitFolder);
      }
      return _cachedLabel;
    }
    set {
      _cachedLabel = value;
    }
  }

  /// <summary>
  /// Locate and parse GIT's "config" file for this repository
  /// </summary>
  public GitConfig LoadGitConfig()
  {
    return GitConfig.FromFile(Path.Combine(GitFolder, "config"));
  }

  /// <summary>
  /// Create a wrapper for an LcGit library repository-local configuration file
  /// (even if it does not exist yet). This method creates our own admin folder
  /// (LcGitFolder) if it does not yet exist.
  /// </summary>
  /// <param name="shortname">
  /// The short name of the file
  /// </param>
  public ConfigBlob LcGitConfigFile(string shortname)
  {
    if(!Directory.Exists(LcGitFolder))
    {
      Directory.CreateDirectory(LcGitFolder);
    }
    var fileName = Path.Combine(LcGitFolder, shortname);
    return new ConfigBlob(fileName);
  }

  /// <summary>
  /// OBSOLETE Load the repository's full commit graph
  /// </summary>
  /// <param name="commandHost">
  /// The command host used to invoke git.exe
  /// </param>
  /// <returns>
  /// A new CommitGraph instance
  /// </returns>
  public CommitGraph LoadGraph(GitCommandHost commandHost)
  {
    return commandHost.LoadGraph(GitFolder);
  }

  /// <summary>
  /// Load the repository's full graph as a SummaryGraph
  /// </summary>
  /// <param name="commandHost">
  /// The command host used to invoke GIT
  /// </param>
  /// <param name="allowPruning">
  /// If true, partial graphs are allowed and pruned.
  /// </param>
  /// <returns>
  /// A new SummaryGraph instance
  /// </returns>
  public SummaryGraph LoadSummaryGraph(GitCommandHost commandHost, bool allowPruning = false)
  {
    return commandHost.LoadSummaryGraph(GitFolder, allowPruning);
  }

  /// <summary>
  /// Load the existing Repo Info object
  /// </summary>
  public RepoInfo LoadInfo(bool mustExist = true)
  {
    var blob = LcGitConfigFile("repo-info.json");
    if(!blob.Exists)
    {
      if(!mustExist)
      {
        return null;
      }
      throw new InvalidOperationException(
        "This repo has not been initialized for lcgitlib use yet (file missing).");
    }
    if(!RepoInfo.IsInitialized(blob.Root))
    {
      if(!mustExist)
      {
        return null;
      }
      throw new InvalidOperationException(
        "This repo has not been initialized for lcgitlib use yet (incompatible content).");
    }
    return new RepoInfo(blob);
  }

  /// <summary>
  /// Initialize the repo info file. Fails if already done so
  /// </summary>
  public void InitializeRepoInfo(GitCommandHost commandHost)
  {
    var blob = LcGitConfigFile("repo-info.json");
    if(blob.Exists)
    {
      throw new InvalidOperationException(
        "This repo has already been initialized for lcgitlib use yet (file exists).");
    }
    var roots = commandHost.RootEntries(GitFolder);
    if(roots.Count>1)
    {
      throw new NotSupportedException(
        "This repository has multiple root commits, which is not yet supported by lcgitlib");
    }
    if(roots.Count < 1)
    {
      throw new NotSupportedException(
        "This repository is empty; it does not have a root commit to identify it");
    }
    var rootCommit = roots[0];
    var location = RepoFolder ?? GitFolder;
    var label = Label;
    RepoInfo.InitializeNew(
      blob,
      location,
      label,
      rootCommit.CommitId,
      RandomId.NewId(12),
      Bare,
      1);
    // add additional info
    blob.Root
      .Set("founded", rootCommit.Author.ZonedTime)
      ;
    //if(Bare && IsInStage())
    //{
    //  blob.Root.Set("role", "stage");
    //}
    blob.Save();
  }

}
