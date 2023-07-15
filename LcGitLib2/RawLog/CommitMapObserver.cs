/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LcGitLib2.RawLog
{
  /// <summary>
  /// An adapter that observes <see cref="CommitEntry"/> instances and
  /// inserts them in a <see cref="CommitMap"/>
  /// </summary>
  public class CommitMapObserver: IObserver<CommitEntry>
  {
    /// <summary>
    /// Create a new CommitMapObserver
    /// </summary>
    public CommitMapObserver(CommitMap target)
    {
      Target = target;
    }

    /// <summary>
    /// The target <see cref="CommitMap"/>
    /// </summary>
    public CommitMap Target { get; init; }

    /// <summary>
    /// Ignored. Implements <see cref="IObserver{T}.OnCompleted()"/>
    /// </summary>
    public void OnCompleted()
    {
      // ignore
    }

    /// <summary>
    /// Ignored. Implements <see cref="IObserver{T}.OnError(Exception)"/>
    /// </summary>
    public void OnError(Exception error)
    {
      // ignore
    }

    /// <summary>
    /// Implements <see cref="IObserver{T}.OnNext(T)"/> by inserting the
    /// observed commit into <see cref="Target"/>
    /// </summary>
    public void OnNext(CommitEntry value)
    {
      Target.Insert(value);
    }
  }
}
