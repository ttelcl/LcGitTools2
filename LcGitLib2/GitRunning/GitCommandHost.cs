/*
 * (c) 2021  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LcGitLib2.GitRunning;

/// <summary>
/// Host object for creating and running GIT commands
/// </summary>
public class GitCommandHost
{
  /// <summary>
  /// Create a new GitCommandHost
  /// </summary>
  /// <param name="argumentLogger">
  /// The logger object that will be called back before executing a GIT subprocess.
  /// This can be null.
  /// </param>
  /// <param name="configuration">
  /// The library configuration object, or null to use the default as loaded from
  /// ~/.lcgitlib/lcgitlib.cfg.json or the hardcoded default if that file is missing.
  /// </param>
  public GitCommandHost(IGitArgLogger argumentLogger, LcGitConfig configuration = null)
  {
    ArgumentLogger = argumentLogger;
    Configuration = configuration ?? LcGitConfig.LoadDefault();
  }

  /// <summary>
  /// The configuration object for this library
  /// </summary>
  public LcGitConfig Configuration { get; }

  /// <summary>
  /// The path to the GIT executable
  /// </summary>
  public string GitPath { get => Configuration.GitPath; }

  /// <summary>
  /// The handler for logging arguments passed to GIT
  /// </summary>
  public IGitArgLogger ArgumentLogger { get; set; }

  /// <summary>
  /// If this is set to false, the environment variable "GIT_TERMINAL_PROMPT" will be set to 0
  /// See https://git-scm.com/docs/git#Documentation/git.txt-codeGITTERMINALPROMPTcode .
  /// Note: unclear if this actually works
  /// </summary>
  public bool GitTerminalPrompt { get; set; }

  /// <summary>
  /// Return a new command and register the "--no-pager" flag (unless
  /// explicitly specified otherwise)
  /// </summary>
  public GitCommand NewCommand(bool noPager = true)
  {
    var cmd = new GitCommand(this);
    if(noPager)
    {
      cmd.AddPre("--no-pager");
    }
    return cmd;
  }

  /// <summary>
  /// Run the command and send the output to the console
  /// </summary>
  public int RunToConsole(GitCommand cmd, bool terminateInput = true)
  {
    var psi = cmd.BakeCommand();
    psi.RedirectStandardOutput = false;
    psi.RedirectStandardError = false;
    psi.RedirectStandardInput = terminateInput;
    ReportCall(psi);
    using(var process = Process.Start(psi))
    {
      if(terminateInput)
      {
        process.StandardInput.Close();
      }
      process.WaitForExit();
      return process.ExitCode;
    }
  }

  /// <summary>
  /// Run the git process and invoke the given handler delegate to process
  /// all lines it writes to its stdout.
  /// </summary>
  /// <param name="cmd">
  /// The command to execute
  /// </param>
  /// <param name="handler">
  /// The delegate that receives an enumerable of the produced output lines.
  /// This delegate MUST process all lines, otherwise a dangling child process is left.
  /// </param>
  /// <param name="terminateInput">
  /// True to close the standard input
  /// </param>
  /// <returns>
  /// The process exit code
  /// </returns>
  public int RunToLineSequence(
    GitCommand cmd,
    Action<IEnumerable<string>> handler,
    bool terminateInput = true)
  {
    var psi = cmd.BakeCommand();
    psi.RedirectStandardOutput = true;
    psi.StandardOutputEncoding = Encoding.UTF8; // !
    psi.RedirectStandardError = false;
    psi.RedirectStandardInput = terminateInput;
    ReportCall(psi);
    using(var process = Process.Start(psi))
    {
      if(terminateInput)
      {
        process.StandardInput.Close();
      }
      handler.Invoke(ProcessOutputLines(process));
      process.WaitForExit();
      return process.ExitCode;
    }
  }

  /// <summary>
  /// Run the GIT process and capture the output as a list of lines
  /// </summary>
  /// <param name="cmd">
  /// The command to run
  /// </param>
  /// <param name="exitCode">
  /// The process exit code
  /// </param>
  /// <param name="terminateInput">
  /// True to close standard input on the child process
  /// </param>
  /// <returns>
  /// The captured list of lines
  /// </returns>
  public List<string> RunToLines(
    GitCommand cmd,
    out int exitCode,
    bool terminateInput = true)
  {
    var list = new List<string>();
    exitCode = RunToLineSequence(cmd, lines => list.AddRange(lines), terminateInput);
    return list;
  }

  /// <summary>
  /// Run the GIT process and send each output line to the given observer
  /// </summary>
  /// <param name="cmd">
  /// The command to run
  /// </param>
  /// <param name="observer">
  /// The observer to receive the output lines (and error notifications).
  /// Its OnCompleted method will be called even if an exception occurs.
  /// </param>
  /// <param name="terminateInput">
  /// True to close the child process standard input
  /// </param>
  /// <returns>
  /// The status code
  /// </returns>
  public int RunToLineObserver(
    GitCommand cmd,
    IObserver<string> observer,
    bool terminateInput = true)
  {
    var psi = cmd.BakeCommand();
    psi.RedirectStandardOutput = true;
    psi.StandardOutputEncoding = Encoding.UTF8; // !
    psi.RedirectStandardError = false;
    psi.RedirectStandardInput = terminateInput;
    ReportCall(psi);
    try
    {
      using(var process = Process.Start(psi))
      {
        if(terminateInput)
        {
          process.StandardInput.Close();
        }
        //string line;
        //while((line = process.StandardOutput.ReadLine()) != null)
        //{
        //  observer.OnNext(line);
        //}
        var lineReader = new GitLineReader();
        foreach(var line in lineReader.ReadLines(process.StandardOutput.BaseStream))
        {
          observer.OnNext(line);
        }
        process.WaitForExit();
        return process.ExitCode;
      }
    }
    catch(Exception ex)
    {
      observer.OnError(ex);
      throw;
    }
    finally
    {
      observer.OnCompleted();
    }
  }

  private static IEnumerable<string> ProcessOutputLines(Process process)
  {
    var lineReader = new GitLineReader();
    foreach(var line in lineReader.ReadLines(process.StandardOutput.BaseStream))
    {
      yield return line;
    }
  }

  private void ReportCall(ProcessStartInfo psi)
  {
    if(ArgumentLogger != null)
    {
      ArgumentLogger.ReportArguments(psi);
    }
  }

}
