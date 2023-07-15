/*
 * (c) 2021  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LcGitLib2.FileUtilities;

namespace LcGitLib2.RawLog
{
  /// <summary>
  /// Description of CommitCsvAdapter
  /// </summary>
  public class CommitCsvAdapter: IObserver<CommitEntry>
  {
    /// <summary>
    /// Create a new CommitCsvAdapter
    /// </summary>
    public CommitCsvAdapter(IObserver<string> lineSink)
    {
      LineSink = lineSink;
      WriteHeader();
    }

    /// <summary>
    /// The sink where the CSV-formatted data is written
    /// </summary>
    public IObserver<string> LineSink { get; }

    /// <summary>
    /// Passed on to the sink
    /// </summary>
    public void OnCompleted()
    {
      LineSink.OnCompleted();
    }

    /// <summary>
    /// Passed on to the sink
    /// </summary>
    public void OnError(Exception error)
    {
      LineSink.OnError(error);
    }

    /// <summary>
    /// Write the next line
    /// </summary>
    public void OnNext(CommitEntry ce)
    {
      if(ce!=null)
      {
        WriteRow(
          ce.CommitId,
          ce.Author == null ? "" : ce.Author.ZonedTime,
          ce.Parents.Count > 0 ? ce.Parents[0].Substring(0, 8) : "",
          ce.Parents.Count > 1 ? ce.Parents[1].Substring(0, 8) : "",
          ce.Parents.Count,
          ce.MessageLines.Count,
          ce.MessageLines.Count > 0 ? CsvUtilities.CsvQuote(ce.MessageLines[0]) : "",
          ce.Author == null ? "" : CsvUtilities.CsvQuote(ce.Author.User),
          ce.Committer == null ? "" : CsvUtilities.CsvQuote(ce.Committer.User),
          ce.Committer == null ? "" : ce.Committer.ZonedTime,
          ce.TreeId.Substring(0, 8)
          );
      }
    }

    private void WriteHeader()
    {
      var line =
        "commit,author-time,parent1,parent2,parents," +
        "lines,message,author,committer,commit-time,tree";
      LineSink.OnNext(line);
    }

    private void WriteRow(
      string commit,
      string authorTime,
      string parent1,
      string parent2,
      int parentCount,
      int messageLineCount,
      string message,
      string author,
      string committer,
      string commitTime,
      string tree
      )
    {
      var line =
        $"{commit},{authorTime},{parent1},{parent2},{parentCount}," +
        $"{messageLineCount},{message},{author},{committer},{commitTime},{tree}";
      LineSink.OnNext(line);
    }
  }
}
