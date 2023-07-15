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
    string machineName,
    string stageFolder,
    string usbFolder,
    bool doCheck)
  {
    GitPath = gitPath;
    MachineName = machineName;
    StageFolder = stageFolder;
    UsbFolder = usbFolder;
    if(doCheck)
    {
      if(String.IsNullOrEmpty(GitPath))
      {
        throw new InvalidOperationException(
          $"Configuration error: GIT.exe configuration is missing");
      }
      if(!File.Exists(GitPath))
      {
        throw new InvalidOperationException(
          $"Configuration error: GIT.exe not found at {GitPath}");
      }
      if(String.IsNullOrEmpty(MachineName))
      {
        throw new InvalidOperationException(
          "No MachineName specified in configuration file");
      }
      if(String.IsNullOrEmpty(StageFolder))
      {
        throw new InvalidOperationException(
          "No staging folder root has been configured yet");
      }
      if(!Directory.Exists(StageFolder))
      {
        throw new InvalidOperationException(
          $"The configured stage folder root does not exist: {StageFolder}");
      }
      if(String.IsNullOrEmpty(UsbFolder))
      {
        throw new InvalidOperationException(
          "No USB folder root has been configured yet");
      }
    }
  }

  /// <summary>
  /// Create a new LcGitConfig and check its validity
  /// </summary>
  public LcGitConfig(
    string gitPath,
    string machineName,
    string stageFolder,
    string usbFolder)
    : this(gitPath, machineName, stageFolder, usbFolder, true)
  {
  }

  /// <summary>
  /// The location of the git executable
  /// </summary>
  public string GitPath { get; }

  /// <summary>
  /// The name to use for this computer
  /// </summary>
  public string MachineName { get; }

  /// <summary>
  /// The folder holding the stage repositories
  /// </summary>
  public string StageFolder { get; }

  /// <summary>
  /// The path holding the USB repositories. By definition this points to a volatile
  /// location: it may or may not be accessible
  /// </summary>
  public string UsbFolder { get; }

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
      return new LcGitConfig(
        @"C:\Program Files\Git\cmd\git.exe",
        Environment.MachineName,
        "",
        "",
        doCheck);
    }
    else
    {
      var json = File.ReadAllText(ConfigFile);
      return JsonConvert.DeserializeObject<LcGitConfig>(json);
    }
  }

}
