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
using LcGitLib2.GitRunning;
using LcGitLib2.RepoTools;

namespace LcGitLib2.MetaBase;

/// <summary>
/// Degrees of cache file updates
/// </summary>
public enum UpdateLevel
{
  /// <summary>
  /// Do not update, but use as-is
  /// </summary>
  NoUpdate = 0,

  /// <summary>
  /// Minimal update (integrate newer component files)
  /// </summary>
  Update = 1,

  /// <summary>
  /// Rebuild from scratch
  /// </summary>
  Rebuild = 2,
}

/// <summary>
/// Represents the stage folder and the database-like functionality it provides
/// </summary>
public class Stage
{
  /// <summary>
  /// Create a new Stage object (and verify it is set up in a compatible way)
  /// </summary>
  public Stage(
    LcGitConfig configuration)
  {
    Configuration = configuration;
    if(String.IsNullOrEmpty(StageFolder))
    {
      throw new InvalidOperationException(
        "The stage folder location is not configured yet");
    }
    if(!Path.IsPathFullyQualified(StageFolder) || !Path.IsPathRooted(StageFolder))
    {
      throw new InvalidOperationException(
        $"The stage folder location is not valid (expecting an absolute path): {StageFolder}");
    }
    if(!Directory.Exists(StageFolder))
    {
      throw new InvalidOperationException(
        $"The configured stage folder does not exist: {StageFolder}");
    }
    RepoInfoFolder = Path.Combine(StageFolder, RepoInfoFolderName);
    if(!Directory.Exists(RepoInfoFolder))
    {
      Directory.CreateDirectory(RepoInfoFolder);
    }
    StageConfig = new ConfigBlob(Path.Combine(StageFolder, StageTagFileName));
    if(StageVersion < 1L) // includes the case of the stage tag file not existing
    {
      throw new InvalidOperationException(
        "The stage folder has not been initialized yet (run 'lcgitutil db init')");
    }
  }

  /// <summary>
  /// The (short) name of the folder inside the stage that holds the RepoInfo
  /// database
  /// </summary>
  public const string RepoInfoFolderName = "_repo-info";

  /// <summary>
  /// The name of the file that tags this folder as a stage folder.
  /// The content is a stub only; it is its presence that is most relevant
  /// </summary>
  public const string StageTagFileName = "lcgitlib-stage-folder.json";

  /// <summary>
  /// Perform some one-time stage folder initialization tasks.
  /// Requires that the folder exists already.
  /// </summary>
  public static void SetupStage(string stageFolder)
  {
    stageFolder = Path.GetFullPath(stageFolder);
    if(!Directory.Exists(stageFolder))
    {
      throw new InvalidOperationException(
        $"The configured stage folder does not exist: {stageFolder}");
    }
    var repoInfoFolder = Path.Combine(stageFolder, RepoInfoFolderName);
    if(!Directory.Exists(repoInfoFolder))
    {
      Directory.CreateDirectory(repoInfoFolder);
    }
    var stageTagFile = Path.Combine(stageFolder, StageTagFileName);
    var tagBlob = new ConfigBlob(stageTagFile);
    var tagVersion = tagBlob.Integers["version"] ?? 0;
    if(tagVersion < 1)
    {
      tagVersion = 1;
      tagBlob.Integers["version"] = tagVersion;
    }
    tagBlob.Save();
  }

  /// <summary>
  /// Test if the file or folder is in a stage area, and if so,
  /// return the path of the stage folder. Otherwise return null
  /// </summary>
  public static string IsInStageArea(string path)
  {
    return _IsInStageArea(Path.GetFullPath(path));
  }

  /// <summary>
  /// Test if the folder is a stage folder
  /// </summary>
  public static bool IsStageFolder(string folder)
  {
    var tagFile = Path.Combine(folder, StageTagFileName);
    return File.Exists(tagFile);
  }

  private static string _IsInStageArea(string path)
  {
    if(String.IsNullOrEmpty(path))
    {
      return null;
    }
    var tagFile = Path.Combine(path, StageTagFileName);
    if(File.Exists(tagFile))
    {
      return path;
    }
    return _IsInStageArea(Path.GetDirectoryName(path));
  }

  /// <summary>
  /// The LcGitLib configuration object
  /// </summary>
  public LcGitConfig Configuration { get; }

  /// <summary>
  /// The stage configuration object
  /// </summary>
  public ConfigBlob StageConfig { get; }

  /// <summary>
  /// The stage area format version (currently 1)
  /// </summary>
  public long StageVersion { get => StageConfig.Integers["version"] ?? 0L; }

  /// <summary>
  /// The stage folder location
  /// </summary>
  public string StageFolder { get => Configuration.StageFolder; }

  /// <summary>
  /// The folder storing repo-info copies, effectively pointing to 
  /// "all known local repositories"
  /// </summary>
  public string RepoInfoFolder { get; }

  /// <summary>
  /// Import the repository's repo-info file if it is unknown or out of date
  /// </summary>
  /// <param name="repository">
  /// The repository
  /// </param>
  /// <param name="role">
  /// If not null: the expected role of the repository. If different from what
  /// was known before, the role will be updated
  /// </param>
  /// <returns>
  /// True if the information was updated, false if it was ok already
  /// </returns>
  public bool Register(GitRepository repository, string role = null)
  {
    var info = repository.LoadInfo(false);
    if(info==null)
    {
      throw new InvalidOperationException(
        "The repository has not been initialized for LcGitLib use yet");
    }
    if(role!=null)
    {
      if(info.Role!=role)
      {
        info.HostObject.Strings["role"] = role;
        info.HostBlob.Save(); // side effect: change file's timestamp
      }
    }
    var stageCopyShort = $"{info.ShortRoot}.{info.ShortId}.repo-info.json";
    var stageCopy = Path.Combine(RepoInfoFolder, stageCopyShort);
    var stageCopyBlob = new ConfigBlob(stageCopy);
    var isOutDated = stageCopyBlob.IsOlderThan(info.HostBlob.BackingFile);
    if(isOutDated)
    {
      stageCopyBlob.Import(info.HostObject, false); // for now: preserve local additions
      stageCopyBlob.Save();
      return true;
    }
    else
    {
      return false;
    }
  }

  /// <summary>
  /// Load the Repo DB file, optionally updating it
  /// </summary>
  /// <param name="level">
  /// The update behaviour
  /// </param>
  public RepoInfos LoadRepoDb(UpdateLevel level)
  {
    var backingFileName = Path.Combine(RepoInfoFolder, "combined.repo-infos.json");
    var backingBlob = new ConfigBlob(backingFileName);
    var ris = new RepoInfos(backingBlob);
    if(level>UpdateLevel.NoUpdate)
    {
      var refTime = ris.HostBlob.TimeStamp() ?? DateTime.MinValue;
      if(level == UpdateLevel.Rebuild)
      {
        refTime = DateTime.MinValue;
        ris.Clear();
      }
      // Console.WriteLine($"DBG>> Reference time is {refTime}");
      foreach(var fnm in Directory.EnumerateFiles(RepoInfoFolder, "*.repo-info.json"))
      {
        if(File.GetLastWriteTimeUtc(fnm)>=refTime)
        {
          // Console.WriteLine($"DBG>> Inserting {fnm}");
          var ri = new RepoInfo(new ConfigBlob(fnm));
          ris.Insert(ri);
        }
      }
      ris.HostBlob.Save();
    }
    return ris;
  }

  /// <summary>
  /// Create a stage repository by cloning a local work repository
  /// </summary>
  /// <param name="cmdHost">
  /// The command host used to execute GIT commands
  /// </param>
  /// <param name="source">
  /// The source repository to be cloned
  /// </param>
  /// <param name="label">
  /// Optional override for the new repository's name
  /// </param>
  /// <returns>
  /// The GitRepository representing the new stage repository
  /// </returns>
  public GitRepository CreateStageRepo(
    GitCommandHost cmdHost,
    GitRepository source,
    string label = null)
  {
    label = String.IsNullOrEmpty(label) ? source.Label : label;
    if(source.IsInStage())
    {
      throw new InvalidOperationException(
        "Expecting a repository outside the stage area as source");
    }
    if(source.Bare)
    {
      throw new InvalidOperationException(
        "Expecting a non-bare repository as source");
    }
    var srcInfo = source.LoadInfo(true); // fails if not lcgitlib-initialized
    var targetFolder = Path.Combine(StageFolder, label + ".git");
    if(Directory.Exists(targetFolder))
    {
      throw new InvalidOperationException(
        $"The target folder already exists: {targetFolder}");
    }
    var remoteName = $"{Configuration.MachineName}-{srcInfo.InstanceId.ToLower().Substring(0, 5)}";
    var remoteFolder = source.RepoFolder.Replace('\\', '/');
    var gitcfg = source.LoadGitConfig();
    var stageSection = gitcfg.FindSubsection("remote", "stage");
    if(stageSection!=null)
    {
      var url = stageSection.FindValue("url", true) ?? "";
      throw new InvalidOperationException(
        $"The source repo already has a remote named 'stage', pointing to '{url}'");
    }

    // Note that a simple "git clone --bare" does almost what we do ... but not entirely
    // For a stage repo, we do not want an "origin" at all
    // And we want a special fetch for the remote we are adding

    // Step 1: create the folder
    Directory.CreateDirectory(targetFolder);

    // Step 2: initialize it as a bare repository
    var cmd =
      cmdHost.NewCommand()
      .WithFolder(targetFolder)
      .WithCommand("init")
      .AddPost("--bare")
      ;
    var status = cmdHost.RunToConsole(cmd);
    if(status!=0)
    {
      throw new InvalidOperationException(
        $"GIT returned error {status}");
    }

    // Step 3: add our source as a remote to the new repository
    cmd =
      cmdHost.NewCommand()
      .WithFolder(targetFolder)
      .WithCommand("remote")
      .AddPost("add", remoteName, remoteFolder)
      ;
    status = cmdHost.RunToConsole(cmd);
    if(status != 0)
    {
      throw new InvalidOperationException(
        $"GIT returned error {status}");
    }

    // Step 4a: add additional fetch instruction, to fetch the source's remotes and tags too
    // Sample: git config --add remote.Y2018-hot1.fetch "+refs/remotes/*:refs/remotes/Y2018-hot1/r/*"
    cmd =
      cmdHost.NewCommand()
      .WithFolder(targetFolder)
      .WithCommand("config")
      .AddPost("--add")
      .AddPost($"remote.{remoteName}.fetch")
      .AddPost($"+refs/remotes/*:refs/remotes/{remoteName}/r/*")
      ;
    status = cmdHost.RunToConsole(cmd);
    if(status != 0)
    {
      throw new InvalidOperationException(
        $"GIT returned error {status}");
    }

    // Step 4b: but prevent recursion, by excluding the source's "stage" remote
    // Note: requires GIT 2.29+!
    // Sample: git config --add remote.Y2018-hot1.fetch "^refs/remotes/stage/*"
    cmd =
      cmdHost.NewCommand()
      .WithFolder(targetFolder)
      .WithCommand("config")
      .AddPost("--add")
      .AddPost($"remote.{remoteName}.fetch")
      .AddPost($"^refs/remotes/stage/*")
      ;
    status = cmdHost.RunToConsole(cmd);
    if(status != 0)
    {
      throw new InvalidOperationException(
        $"GIT returned error {status}");
    }

    // Step 5: Add this new stage repository as remote to the source
    cmd =
      cmdHost.NewCommand()
      .WithFolder(source.GitFolder)
      .WithCommand("remote")
      .AddPost("add", "stage", targetFolder.Replace('\\', '/'))
      ;
    status = cmdHost.RunToConsole(cmd);
    if(status != 0)
    {
      throw new InvalidOperationException(
        $"GIT returned error {status}");
    }

    // Step 6: Fetch the source
    cmd =
      cmdHost.NewCommand()
      .WithFolder(targetFolder)
      .WithCommand("fetch")
      .AddPost(remoteName)
      ;
    status = cmdHost.RunToConsole(cmd);
    if(status != 0)
    {
      throw new InvalidOperationException(
        $"GIT returned error {status}");
    }

    // Step 7: While we have all the content in remote refs, we do not have heads yet.
    // Push all branches from the source to the stage to fix
    cmd =
      cmdHost.NewCommand()
      .WithFolder(source.GitFolder)
      .WithCommand("push")
      .AddPost("--all", "stage")
      ;
    status = cmdHost.RunToConsole(cmd);
    if(status != 0)
    {
      throw new InvalidOperationException(
        $"GIT returned error {status}");
    }

    // Step 8: initialize our own repo-info
    var stageRepo = new GitRepository(targetFolder, label);
    stageRepo.InitializeRepoInfo(cmdHost);

    // Step 9: register this stage repo
    Register(stageRepo, "stage");

    return stageRepo;
  }
}
