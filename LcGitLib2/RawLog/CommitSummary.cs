/*
 * (c) 2021  VTT / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LcGitLib.GraphModel;

using Newtonsoft.Json;

namespace LcGitLib.RawLog
{
  /// <summary>
  /// Compacted variant of a raw CommitEntry
  /// </summary>
  public class CommitSummary: IGraphNodeSeed<long>
  {
    /// <summary>
    /// Create a new CommitSummary
    /// </summary>
    public CommitSummary(
      string commit,
      string timeStamp,
      string author,
      string label,
      IEnumerable<long> parents,
      string committer = null)
    {
      CommitId = commit;
      CommitTag = CommitId.AsCommitTag();
      TimeStamp = timeStamp;
      FullStamp = DateTimeOffset.ParseExact(
        TimeStamp,
        "yyyy-MM-dd HH:mm:ss zzz",
        CultureInfo.InvariantCulture);
      Author = author;
      Committer = committer ?? Author;
      Label = label;
      var parentList = new List<long>(parents);
      Parents = parentList.AsReadOnly();
    }

    /// <summary>
    /// Convert a CommitEntry to a CommitSummary (the conversion is lossy, so not reversible)
    /// </summary>
    public static CommitSummary FromEntry(CommitEntry e)
    {
      if(e == null)
      {
        return null;
      }
      var stamp = DateTimeOffset.FromUnixTimeSeconds(0);
      if(e.Author != null && e.Author.TimeStamp > stamp)
      {
        stamp = e.Author.TimeStamp;
      }
      if(e.Committer != null && e.Committer.TimeStamp > stamp)
      {
        stamp = e.Committer.TimeStamp;
      }
      var timeStamp = stamp.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
      var author =
        e.Author?.User ?? e.Committer?.User;
      var committer = e.Committer?.User;
      var label = e.MessageLines.Count > 0 ? e.MessageLines[0] : "";
      var parents =
        e.Parents.Select(p => p.AsCommitTag());
      return new CommitSummary(
        e.CommitId,
        timeStamp,
        author,
        label,
        parents,
        committer);
    }

    /// <summary>
    /// The numerical commit tag derived from the commit ID
    /// </summary>
    [JsonProperty("id")]
    public long CommitTag { get; }

    /// <summary>
    /// Implements IGraphNodeSeed{long}.Id by returning the CommitTag
    /// </summary>
    [JsonIgnore()]
    public long Id { get => CommitTag; }

    /// <summary>
    /// The full commit ID
    /// </summary>
    [JsonProperty("commit")]
    public string CommitId { get; }

    /// <summary>
    /// Time zoned time stamp, in the format "yyyy-MM-dd HH:mm:ss zzz".
    /// </summary>
    [JsonProperty("timeStamp")]
    public string TimeStamp { get; }

    /// <summary>
    /// TimeStamp parsed as DateTimeOffset
    /// </summary>
    [JsonIgnore()]
    public DateTimeOffset FullStamp { get; }

    /// <summary>
    /// The author
    /// </summary>
    [JsonProperty("author")]
    public string Author { get; }

    /// <summary>
    /// The committer (only serialized when different from Author)
    /// </summary>
    [JsonProperty("committer")]
    public string Committer { get; }

    /// <summary>
    /// Serialize committer only when different from author
    /// </summary>
    public bool ShouldSerializeCommitter()
    {
      return Committer != Author;
    }

    /// <summary>
    /// The first line of the message
    /// </summary>
    [JsonProperty("label")]
    public string Label { get; }

    /// <summary>
    /// The commit tags of the parent commits
    /// </summary>
    [JsonProperty("parents")]
    public IReadOnlyList<long> Parents { get; }
  }
}
