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

namespace LcGitLib2.RawLog;

/// <summary>
/// States a CommitEntryBuilder can be in
/// </summary>
public enum CommitEntryBuilderState
{
  /// <summary>
  /// Awaiting the first line
  /// </summary>
  Initial,

  /// <summary>
  /// At least one header field is being accumulated (but potentially incomplete)
  /// </summary>
  BuildingHeaders,

  /// <summary>
  /// Building the commit message (including the case where it is still empty)
  /// </summary>
  BuildingMessage,
}

/// <summary>
/// Helper class for parsing CommitEntry objects
/// </summary>
public class CommitEntryBuilder
{
  private readonly List<string> _parents;
  private readonly List<string> _message;
  private Dictionary<string, string> _other = null;
  private string _commit;
  private string _tree;
  private string _author;
  private string _committer;
  private string _headerKey;
  private string _headerValue;

  private static char[] __splitter = new[] { ' ' };

  /// <summary>
  /// Create a new CommitEntryBuilder
  /// </summary>
  public CommitEntryBuilder()
  {
    _parents = new List<string>();
    _message = new List<string>();
    Reset();
  }

  /// <summary>
  /// The builder state
  /// </summary>
  public CommitEntryBuilderState State { get; private set; }

  /// <summary>
  /// True when this builder is idle, in between builds
  /// </summary>
  public bool IsIdle { get => State == CommitEntryBuilderState.Initial; }

  /// <summary>
  /// Parse a raw commit log stream and return a sequence of the commit entries
  /// found
  /// </summary>
  public IEnumerable<CommitEntry> ParseStream(StreamReader source)
  {
    if(!IsIdle)
    {
      throw new InvalidOperationException(
        "A CommitEntry parse was already in progress");
    }
    string line;
    while((line = source.ReadLine())!=null)
    {
      var ce = ProcessNextLine(line);
      if(ce!=null)
      {
        yield return ce;
      }
    }
    if(!IsIdle)
    {
      throw new InvalidOperationException(
        "A CommitEntry parse was still in progress at EOF");
    }
  }

  /// <summary>
  /// Wrap this builder as a string observer that pushes parsed CommitEntries
  /// to the given CommitEntry observer
  /// </summary>
  public IObserver<string> WrapObserver(IObserver<CommitEntry> commitObserver)
  {
    return new LineTransformer(this, commitObserver);
  }

  private class LineTransformer: IObserver<string>
  {
    public LineTransformer(CommitEntryBuilder builder, IObserver<CommitEntry> sink)
    {
      Owner = builder;
      Sink = sink;
    }

    public CommitEntryBuilder Owner { get; }

    public IObserver<CommitEntry> Sink { get; }

    public void OnCompleted()
    {
      if(!Owner.IsIdle)
      {
        // known issue: the last (empty) line may be missing, simulate it!
        OnNext("");
      }
      Sink.OnCompleted();
    }

    public void OnError(Exception error)
    {
      Sink.OnError(error);
    }

    public void OnNext(string value)
    {
      var ce = Owner.ProcessNextLine(value);
      if(ce!=null)
      {
        Sink.OnNext(ce);
      }
    }
  }

  /// <summary>
  /// Process the next commit log line. Returns null while a parse is in
  /// progress, a CommitEntry when an entry is complete
  /// </summary>
  public CommitEntry ProcessNextLine(string line)
  {
    // Console.WriteLine($"**{State} [{line}]**");
    switch(State)
    {
      case CommitEntryBuilderState.Initial:
        if(!String.IsNullOrEmpty(line))
        {
          ParseHeader(line);
        }
        // Else: discard superfluous empty line
        return null;
      case CommitEntryBuilderState.BuildingHeaders:
        ParseHeader(line);
        return null;
      case CommitEntryBuilderState.BuildingMessage:
        if(String.IsNullOrEmpty(line))
        {
          return FinishEntry();
        }
        else
        {
          if(line.StartsWith("    "))
          {
            _message.Add(line.Substring(4));
          }
          else
          {
            /*
             * This may be a rare oddball case, where the commit message is completely missing,
             * but instead of an empty message, the next commit starts immediately.
             * This case was observed in the git@github.com:git/git.git repository.
             */
            if(line.StartsWith("commit"))
            {
              var retval = FinishEntry();
              ParseHeader(line);
              return retval;
            }
            else
            {
              throw new InvalidOperationException(
                $"Expecting commit message lines to start with 4 spaces, but found: '{line}'");
            }
          }
          return null;
        }
      default:
        throw new InvalidOperationException(
          "Unknown commit entry parser state");
    }
  }

  private CommitEntry FinishEntry()
  {
    if(State!=CommitEntryBuilderState.BuildingMessage)
    {
      throw new InvalidOperationException(
        "Attempt to extract an incomplete commit entry");
    }
    if(String.IsNullOrEmpty(_commit))
    {
      throw new InvalidOperationException(
        "Missing 'commit' header in commit entry");
    }
    var author = _author == null ? null : UserLine.Parse(_author);
    var committer = _committer == null ? null : UserLine.Parse(_committer);
    var entry = new CommitEntry(
      _commit,
      _tree,
      _parents,
      author,
      committer,
      _other,
      _message);
    Reset();
    return entry;
  }

  private void ParseHeader(string line)
  {
    if(string.IsNullOrEmpty(line))
    {
      if(!String.IsNullOrEmpty(_headerKey))
      {
        CompleteHeader();
      }
      State = CommitEntryBuilderState.BuildingMessage;
    }
    else
    {
      var parts = line.Split(__splitter, 2);
      if(parts.Length != 2)
      {
        throw new InvalidOperationException(
          $"Malformed header line '{line}'");
      }
      if(String.IsNullOrEmpty(parts[0]))
      {
        // This is a header value continuation line.
        // These are very uncommon, yet may appear...
        if(String.IsNullOrEmpty(_headerKey))
        {
          throw new InvalidOperationException(
            "Found a header continuation line without a preceding header line");
        }
        // Use a UNIX-style line separator
        _headerValue = _headerValue + "\n" + parts[1];
      }
      else
      {
        if(!String.IsNullOrEmpty(_headerKey))
        {
          CompleteHeader();
        }
        State = CommitEntryBuilderState.BuildingHeaders;
        _headerKey = parts[0];
        _headerValue = parts[1];
      }
    }
  }

  private void CompleteHeader()
  {
    switch(State)
    {
      case CommitEntryBuilderState.Initial:
        throw new InvalidOperationException("Unexpected blank line in input before any headers");
      case CommitEntryBuilderState.BuildingMessage:
        throw new InvalidOperationException("Internal error, unexpected method call");
      case CommitEntryBuilderState.BuildingHeaders:
        AddHeader(_headerKey, _headerValue);
        _headerKey = null;
        _headerValue = null;
        break;
      default:
        throw new InvalidOperationException("Unknown state");
    }
  }

  private void AddHeader(string key, string value)
  {
    switch(key)
    {
      case "commit":
        if(!String.IsNullOrEmpty(_commit))
        {
          throw new InvalidOperationException("Multiple 'commit' headers in header block");
        }
        _commit = value;
        break;
      case "tree":
        if(!String.IsNullOrEmpty(_tree))
        {
          throw new InvalidOperationException("Multiple 'tree' headers in header block");
        }
        _tree = value;
        break;
      case "author":
        if(!String.IsNullOrEmpty(_author))
        {
          throw new InvalidOperationException("Multiple 'author' headers in header block");
        }
        _author = value;
        break;
      case "committer":
        if(!String.IsNullOrEmpty(_committer))
        {
          throw new InvalidOperationException("Multiple 'committer' headers in header block");
        }
        _committer = value;
        break;
      case "parent":
        _parents.Add(value);
        break;
      default:
        if(_other==null)
        {
          _other = new Dictionary<string, string>();
        }
        if(_other.ContainsKey(key))
        {
          throw new InvalidOperationException(
            $"Multiple '{key}' (extra) headers in header block");
        }
        _other[key] = value;
        break;
    }
  }

  private void Reset()
  {
    State = CommitEntryBuilderState.Initial;
    _parents.Clear();
    _message.Clear();
    _other = null;
    _commit = null;
    _tree = null;
    _author = null;
    _committer = null;
    _headerKey = null;
    _headerValue = null;
  }

}
