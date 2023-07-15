/*
 * (c) 2021  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace LcGitLib2.GitRunning
{
  /// <summary>
  /// Receives the list of arguments about to be passed to the git executable
  /// </summary>
  public interface IGitArgLogger
  {
    /// <summary>
    /// Report the list of arguments to be passed to GIT. These arguments are listed
    /// here unquoted (they will be quoted before building the command line)
    /// </summary>
    void ReportArguments(ProcessStartInfo psi);
  }

  /// <summary>
  /// Simple implementation of IGitArgLogger that redirects to a delegate
  /// </summary>
  public class GitArgLogger: IGitArgLogger
  {
    private Action<ProcessStartInfo> _reporter;

    /// <summary>
    /// Create a new GitArgLogger, redirecting calls to the given delegate
    /// </summary>
    public GitArgLogger(Action<ProcessStartInfo> reporter)
    {
      _reporter = reporter;
    }

    /// <summary>
    /// Implements IGitArgLogger
    /// </summary>
    public void ReportArguments(ProcessStartInfo psi)
    {
      _reporter?.Invoke(psi);
    }
  }
}

