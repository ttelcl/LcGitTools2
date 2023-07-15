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
    var homeFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    var cfgFolder = Path.Combine(homeFolder, ".lcgitlib");
    return Path.Combine(cfgFolder, "lcgitlib.cfg.json");
  }

  /// <summary>
  /// The location where the configuration file is expected
  /// </summary>
  public static string ConfigFile { get; } = CalculateConfigFile();

  /// <summary>
  /// Load the default LcGitLib configuration, falling back to default
  /// settings if the configuration file is missing
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
      throw new NotImplementedException(
        "WIP: auto-initialization");
      //return new LcGitConfig(
      //  @"C:\Program Files\Git\cmd\git.exe",
      //  Environment.MachineName,
      //  "",
      //  "",
      //  doCheck);
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

}
