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
using LcGitLib2.FileUtilities;
using LcGitLib2.GraphModel;
using LcGitLib2.RawLog;

namespace LcGitLib2.GitRunning
{
  /// <summary>
  /// Static methods running GIT and capturing the result somehow.
  /// Most are extension methods on GitCommandHost
  /// </summary>
  public static class GitOperations
  {
    /// <summary>
    /// Return information on the newest commit present in the repository
    /// (not necessarily the HEAD)
    /// </summary>
    public static CommitEntry NewestEntry(
      this GitCommandHost host,
      string startFolder=null)
    {
      startFolder = startFolder == null ? Environment.CurrentDirectory : Path.GetFullPath(startFolder);
      var cmd =
        host
        .NewCommand(true)
        .AddPre("-C", startFolder)
        .WithCommand("log")
        .AddPost("--all", "-n", "1", "--pretty=raw");
      var capture = new CapturingObserver<CommitEntry>();
      var sink =
        capture
        .ObserveAsCommitEntries();
      var status = host.RunToLineObserver(cmd, sink, true);
      if(status!=0)
      {
        throw new InvalidOperationException(
          $"'NewestEntry' GIT recipe: failed with status {status}");
      }
      if(capture.CapturedItems.Count==0)
      {
        throw new InvalidOperationException(
          $"'NewestEntry' GIT recipe: No results received");
      }
      if(capture.CapturedItems.Count > 1)
      {
        throw new InvalidOperationException(
          $"'NewestEntry' GIT recipe: Multiple results received");
      }
      return capture.CapturedItems[0];
    }

    /// <summary>
    /// Return CommitEntries that do not have any parents.
    /// For most repositories this will return precisely 1 Commit entry
    /// </summary>
    public static List<CommitEntry> RootEntries(
      this GitCommandHost host,
      string startFolder = null)
    {
      startFolder = startFolder == null ? Environment.CurrentDirectory : Path.GetFullPath(startFolder);
      var cmd =
        host
        .NewCommand(true)
        .AddPre("-C", startFolder)
        .WithCommand("log")
        .AddPost("--all", "--pretty=raw");
      var capture = new CapturingObserver<CommitEntry>(ce => ce.Parents.Count==0);
      var sink =
        capture
        .ObserveAsCommitEntries();
      var status = host.RunToLineObserver(cmd, sink, true);
      if(status != 0)
      {
        throw new InvalidOperationException(
          $"'RootEntries' GIT recipe: failed with status {status}");
      }
      return capture.CapturedItems;
    }

    /// <summary>
    /// OBSOLETE Capture the entire commit history and transform it into a commit graph
    /// </summary>
    public static CommitGraph LoadGraph(
      this GitCommandHost host,
      string startFolder = null)
    {
      startFolder = startFolder == null ? Environment.CurrentDirectory : Path.GetFullPath(startFolder);
      var cmd =
        host
        .NewCommand(true)
        .AddPre("-C", startFolder)
        .WithCommand("log")
        .AddPost("--all", "--pretty=raw");
      var capture = new CapturingObserver<CommitEntry>();
      var sink =
        capture
        .ObserveAsCommitEntries();
      var status = host.RunToLineObserver(cmd, sink, true);
      if(status != 0)
      {
        throw new InvalidOperationException(
          $"Capturing commits for building graph: failed with status {status}");
      }
      var graph = new CommitGraph(capture.CapturedItems);
      return graph;
    }

    /// <summary>
    /// Load the commit graph of a repository in Summary form
    /// </summary>
    /// <param name="host">
    /// The GIT.exe wrapper
    /// </param>
    /// <param name="startFolder">
    /// The folder used as anchor to find the repository to use (null to use
    /// Environment.CurrentDirectory)
    /// </param>
    /// <param name="allowPruning">
    /// If false, an exception is thrown if any of the referenced parents are missing.
    /// If true, such references are ignored, and their edges omitted from the graph
    /// (use SummaryGraph.PrunedEdgeCount to check if that happened)
    /// </param>
    /// <returns></returns>
    public static SummaryGraph LoadSummaryGraph(
      this GitCommandHost host,
      string startFolder = null,
      bool allowPruning = false)
    {
      var cmd =
        host
        .NewCommand(true)
        .AddPre("-C", startFolder)
        .WithCommand("log")
        .AddPost("--all", "--pretty=raw");
      var capture =
        new CapturingObserver<CommitSummary>();
      var sink =
        capture
        .ObserveTransformed<CommitEntry, CommitSummary>(e => CommitSummary.FromEntry(e))
        .ObserveAsCommitEntries();
      var status = host.RunToLineObserver(cmd, sink, true);
      if(status != 0)
      {
        throw new InvalidOperationException(
          $"Capturing commits for building graph: failed with status {status}");
      }
      var graph = new SummaryGraph(capture.CapturedItems, allowPruning);
      return graph;
    }

  }
}
