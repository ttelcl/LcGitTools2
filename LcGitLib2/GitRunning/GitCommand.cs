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
/// Collects the command line information to run GIT.
/// This is intended for fluent API use.
/// </summary>
public class GitCommand
{
  private readonly List<string> _preCommandArguments;
  private readonly List<string> _postCommandArguments;

  /// <summary>
  /// Create a new GitCommand
  /// </summary>
  internal GitCommand(GitCommandHost host)
  {
    Host = host;
    _preCommandArguments = new List<string>();
    _postCommandArguments = new List<string>();
  }

  /// <summary>
  /// The host object
  /// </summary>
  public GitCommandHost Host { get; }

  /// <summary>
  /// The main command
  /// </summary>
  public string MainCommand { get; private set; }

  /// <summary>
  /// True if a command was set
  /// </summary>
  public bool HasCommand { get => !String.IsNullOrEmpty(MainCommand); }

  internal ProcessStartInfo BakeCommand(
    bool shellExecute = false)
  {
    if(!HasCommand)
    {
      throw new InvalidOperationException(
        "no command was configured");
    }
    var psi = new ProcessStartInfo();
    psi.UseShellExecute = shellExecute;
    foreach(var arg in _preCommandArguments)
    {
      psi.ArgumentList.Add(arg);
    }
    psi.ArgumentList.Add(MainCommand);
    foreach(var arg in _postCommandArguments)
    {
      psi.ArgumentList.Add(arg);
    }
    psi.FileName = Host.GitPath;
    if(Host.GitTerminalPrompt)
    {
      psi.EnvironmentVariables.Remove("GIT_TERMINAL_PROMPT");
    }
    else
    {
      psi.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";
    }
    return psi;
  }

  /// <summary>
  /// Run the command and send the output to the console
  /// </summary>
  /// <param name="terminateInput">
  /// If true (default), the input to the GIT process will be closed, so it
  /// cannot ask for input.
  /// </param>
  public int RunToConsole(bool terminateInput = true)
  {
    return Host.RunToConsole(this, terminateInput);
  }

  /// <summary>
  /// Run the command and pass each returned line to the provided handler delegate
  /// </summary>
  public int RunToLineSequence(
    Action<IEnumerable<string>> handler,
    bool terminateInput = true)
  {
    return Host.RunToLineSequence(this, handler, terminateInput);
  }

  /// <summary>
  /// Run the command and capture the returned lines into a List
  /// </summary>
  public List<string> RunToLines(
    out int exitCode,
    bool terminateInput = true)
  {
    return Host.RunToLines(this, out exitCode, terminateInput);
  }

  /// <summary>
  /// Run the command and send each output line to the provided observer
  /// </summary>
  public int RunToLineObserver(
    IObserver<string> observer,
    bool terminateInput = true)
  {
    return Host.RunToLineObserver(this, observer, terminateInput);
  }

  // Fluent API primitives

  /// <summary>
  /// Generic add an argument before the main command
  /// </summary>
  /// <param name="argument">
  /// The argument string to add. The .NET framework takes care of quoting
  /// if needed, so do not pre-quote
  /// </param>
  /// <returns>
  /// Returns this same object, for fluent calls
  /// </returns>
  public GitCommand AddPre(string argument)
  {
    if(HasCommand)
    {
      throw new InvalidOperationException(
        "This argument must be provided before the main command");
    }
    _preCommandArguments.Add(argument);
    return this;
  }

  /// <summary>
  /// Generic add a arguments before the main command
  /// </summary>
  /// <param name="arguments">
  /// The argument strings to add. The .NET framework takes care of quoting
  /// if needed, so do not pre-quote
  /// </param>
  /// <returns>
  /// Returns this same object, for fluent calls
  /// </returns>
  public GitCommand AddPre(params string[] arguments)
  {
    if(HasCommand)
    {
      throw new InvalidOperationException(
        "These arguments must be provided before the main command");
    }
    foreach(var argument in arguments)
    {
      _preCommandArguments.Add(argument);
    }
    return this;
  }

  /// <summary>
  /// Generic add a arguments before the main command. 
  /// These arguments are injected before the main command, even if
  /// a main command is set already.
  /// </summary>
  /// <param name="arguments">
  /// The argument strings to add. The .NET framework takes care of quoting
  /// if needed, so do not pre-quote
  /// </param>
  /// <returns>
  /// Returns this same object, for fluent calls
  /// </returns>
  public GitCommand AddPreRetroActive(params string[] arguments)
  {
    foreach(var argument in arguments)
    {
      _preCommandArguments.Add(argument);
    }
    return this;
  }

  /// <summary>
  /// Generic add an argument after the main command
  /// </summary>
  /// <param name="argument">
  /// The argument string to add. The .NET framework takes care of quoting
  /// if needed, so do not pre-quote
  /// </param>
  /// <returns>
  /// Returns this same object, for fluent calls
  /// </returns>
  public GitCommand AddPost(string argument)
  {
    if(!HasCommand)
    {
      throw new InvalidOperationException(
        "This argument must be provided after the main command");
    }
    _postCommandArguments.Add(argument);
    return this;
  }

  /// <summary>
  /// Generic add an argument after the main command. This is an alias
  /// for <see cref="AddPost(string)"/>
  /// </summary>
  /// <param name="argument">
  /// The argument string to add. The .NET framework takes care of quoting
  /// if needed, so do not pre-quote
  /// </param>
  /// <returns>
  /// Returns this same object, for fluent calls
  /// </returns>
  public GitCommand AddPost1(string argument)
  {
    return AddPost(argument);
  }

  /// <summary>
  /// Generic add arguments after the main command
  /// </summary>
  /// <param name="arguments">
  /// The argument strings to add. The .NET framework takes care of quoting
  /// if needed, so do not pre-quote
  /// </param>
  /// <returns>
  /// Returns this same object, for fluent calls
  /// </returns>
  public GitCommand AddPost(params string[] arguments)
  {
    if(!HasCommand)
    {
      throw new InvalidOperationException(
        "These arguments must be provided after the main command");
    }
    foreach(var argument in arguments)
    {
      _postCommandArguments.Add(argument);
    }
    return this;
  }

  /// <summary>
  /// Generic conditional add arguments after the main command
  /// </summary>
  /// <param name="condition">
  /// When true: add the arguments. When false: do nothing
  /// </param>
  /// <param name="arguments">
  /// The argument strings to add. The .NET framework takes care of quoting
  /// if needed, so do not pre-quote
  /// </param>
  /// <returns>
  /// Returns this same object, for fluent calls
  /// </returns>
  public GitCommand AddPostIf(bool condition, params string[] arguments)
  {
    if(!HasCommand)
    {
      throw new InvalidOperationException(
        "These arguments must be provided after the main command");
    }
    return condition ? AddPost(arguments) : this;
  }

  /// <summary>
  /// Generic conditional add one argument after the main command
  /// </summary>
  /// <param name="condition">
  /// When true: add the arguments. When false: do nothing
  /// </param>
  /// <param name="argument">
  /// The argument string to add. The .NET framework takes care of quoting
  /// if needed, so do not pre-quote
  /// </param>
  /// <returns>
  /// Returns this same object, for fluent calls
  /// </returns>
  public GitCommand AddPostIf1(bool condition, string argument)
  {
    return condition ? AddPost(argument) : this;
  }

  /// <summary>
  /// Set the main command and returns this same object for chained calls.
  /// Use more specific commands to get a more specific return type
  /// </summary>
  /// <param name="mainCommand">
  /// The main GIT command
  /// </param>
  /// <returns>
  /// Returns this same object, for fluent calls.
  /// </returns>
  public GitCommand WithCommand(string mainCommand)
  {
    if(HasCommand)
    {
      throw new InvalidOperationException(
        $"Attempt to set mian command '{mainCommand}', but '{MainCommand}' was already set as command");
    }
    MainCommand = mainCommand;
    return this;
  }

  /// <summary>
  /// Change the initial folder, which will be used to determine the repository location
  /// ("-C" option)
  /// </summary>
  /// <param name="startFolder">
  /// The modified start folder, or null to not modify it.
  /// </param>
  public GitCommand WithFolder(string startFolder)
  {
    if(!String.IsNullOrEmpty(startFolder))
    {
      return AddPreRetroActive("-C", startFolder);
    }
    else
    {
      return this;
    }
  }

}
