/*
 * (c) 2021  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using LcGitLib2.GraphModel;

namespace LcGitLib2.RawLog;

/// <summary>
/// Represents a full commit entry as read from "git log --pretty=raw" output.
/// </summary>
public class CommitEntry: IGraphNodeSeed<string>
{
  private readonly List<string> _parents;
  private readonly List<string> _messageLines;

  /// <summary>
  /// Create a new CommitEntry
  /// </summary>
  public CommitEntry(
    string commit,
    string? tree,
    IEnumerable<string> parents,
    UserLine? author,
    UserLine? committer,
    Dictionary<string, string>? other,
    IEnumerable<string> message)
  {
    _parents = new List<string>(parents);
    _messageLines = new List<string>(message);
    Parents = _parents.AsReadOnly();
    MessageLines = _messageLines.AsReadOnly();
    CommitId = commit;
    TreeId = tree;
    Author = author;
    Committer = committer;
    Other = other ?? new Dictionary<string, string>();
    CommitTag = CommitId.AsCommitTag();
  }

  /// <summary>
  /// Return a new observer of log lines that pushes parsed CommitEntries
  /// to the given observer
  /// </summary>
  public static IObserver<string> ObserveFromLines(IObserver<CommitEntry> commitObserver)
  {
    return new CommitEntryBuilder().WrapObserver(commitObserver);
  }

  /// <summary>
  /// The full commit ID string
  /// </summary>
  [JsonProperty("commit")]
  public string CommitId { get; }

  /// <summary>
  /// Implements IGraphSeedNode{string}.Id (by returning CommitId)
  /// </summary>
  [JsonIgnore()]
  public string Id { get => CommitId; }

  /// <summary>
  /// The 'commit tag' - a 63 number derived from the Commit ID
  /// </summary>
  [JsonIgnore()]
  public long CommitTag { get; }

  /// <summary>
  /// The tree ID string
  /// </summary>
  [JsonProperty("tree")]
  public string? TreeId { get; }

  /// <summary>
  /// The parent commit(s)
  /// </summary>
  [JsonProperty("parents")]
  public IReadOnlyList<string> Parents { get; }

  /// <summary>
  /// The commit author
  /// </summary>
  [JsonProperty("author")]
  public UserLine? Author { get; }

  /// <summary>
  /// The committer (commit approver)
  /// </summary>
  [JsonProperty("committer")]
  public UserLine? Committer { get; }

  /// <summary>
  /// Only serialize committer if it is different from author
  /// </summary>
  public bool ShouldSerializeCommitter()
  {
    return Author == null || !Author.IsSame(Committer);
  }

  /// <summary>
  /// The full commit message, as separate lines
  /// </summary>
  [JsonProperty("message")]
  public IReadOnlyList<string> MessageLines { get; }

  /// <summary>
  /// Other (non-standard or rarely used) properties
  /// </summary>
  //[JsonExtensionData()]
  [JsonProperty("other")]
  public IDictionary<string, string> Other { get; }

  /// <summary>
  /// Control whether or not to serialize the 'other' dictionary.
  /// Returns false if it is empty
  /// </summary>
  public bool ShouldSerializeOther()
  {
    return Other.Count > 0;
  }

  /// <summary>
  /// Return the authoring time of the commit, falling back to the commit time
  /// if not available
  /// </summary>
  public DateTimeOffset AuthorTime()
  {
    if(Author!=null)
    {
      return Author.TimeStamp;
    }
    if(Committer!=null)
    {
      return Committer.TimeStamp;
    }
    throw new InvalidOperationException(
      "Neither author time nor commit time is known");
  }

  /// <summary>
  /// Return the commit time of the commit, falling back to the authoring time
  /// if not available
  /// </summary>
  public DateTimeOffset CommitTime()
  {
    if(Committer != null)
    {
      return Committer.TimeStamp;
    }
    if(Author != null)
    {
      return Author.TimeStamp;
    }
    throw new InvalidOperationException(
      "Neither author time nor commit time is known");
  }
}
