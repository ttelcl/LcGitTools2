/*
 * (c) 2021  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace LcGitLib2.GitRunning;

/// <summary>
/// Holds configuration information used by this library
/// </summary>
public class LcGitConfig
{
  /// <summary>
  /// Create a new LcGitConfig
  /// </summary>
  private LcGitConfig(
    string gitPath,
    bool doCheck)
  {
    GitPath = gitPath;
    if(doCheck)
    {
      if(String.IsNullOrEmpty(GitPath))
      {
        throw new InvalidOperationException(
          $"Configuration error: 'git' executable configuration is missing");
      }
      if(!File.Exists(GitPath))
      {
        throw new InvalidOperationException(
          $"Configuration error: configured 'git' executable not found at {GitPath}");
      }
    }
  }

  /// <summary>
  /// Create a new LcGitConfig and check its validity
  /// </summary>
  public LcGitConfig(
    string gitPath)
    : this(gitPath, true)
  {
  }

  /// <summary>
  /// The location of the git executable
  /// </summary>
  public string GitPath { get; }

  private static string CalculateConfigFile()
  {
    var cfgFolder = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
      ".lcgitlib2");
    if(!Directory.Exists(cfgFolder))
    {
      Directory.CreateDirectory(cfgFolder);
    }
    return Path.Combine(cfgFolder, "lcgitlib2.gitconfig.json");
  }

  /// <summary>
  /// The location where the user's LcGitLib2 configuration file is expected
  /// </summary>
  public static string ConfigFile { get; } = CalculateConfigFile();

  /// <summary>
  /// Load the default LcGitLib configuration, falling back to default
  /// settings if the configuration file is missing. When falling back,
  /// no new configuration is written.
  /// </summary>
  public static LcGitConfig LoadDefault(bool doCheck = true)
  {
    if(!File.Exists(ConfigFile))
    {
      if(doCheck)
      {
        throw new InvalidOperationException(
          $"The lcgitlib config file is missing: {ConfigFile}");
      }
      var gitPath = LocateGitExecutable();
      if(gitPath == null)
      {
        // TODO: save a template instead
        throw new InvalidOperationException(
          "Unable to locate the default git executable on this system");
      }
      return new LcGitConfig(gitPath, doCheck);
    }
    else
    {
      var json = File.ReadAllText(ConfigFile);
      var cfg = JsonConvert.DeserializeObject<LcGitConfig>(json);
      if(cfg == null)
      {
        throw new InvalidOperationException(
          "Bad configuration: Configuration file content deserialized to null.");
      }
      return cfg;
    }
  }

  /// <summary>
  /// Search the git executable, without relying on our configuration file
  /// </summary>
  /// <returns>
  /// The full path to the git executable if found, or null if not found
  /// </returns>
  public static string? LocateGitExecutable()
  {
    if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
      return SearchInPath("git.exe");
    }
    else if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
      || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
    {
      return SearchInPath("git");
    }
    else
    {
      throw new InvalidOperationException(
        "Unrecognized operating system - don't know how to locate the git executable");
    }
  }

  private static string? SearchInPath(string targetFile)
  {
    var path = Environment.GetEnvironmentVariable("PATH");
    if(path == null)
    {
      throw new InvalidOperationException("Cannot access PATH");
    }
    var pathfolders = path.Split(Path.PathSeparator);
    foreach(var pathfolder in pathfolders)
    {
      var fnm = Path.Combine(pathfolder, targetFile);
      if(File.Exists(fnm))
      {
        return fnm;
      }
    }
    return null;
  }

}
