/*
 * (c) 2021  ttelcl / ttelcl
 */

using System;
using System.IO;

namespace LcGitLib2.FileUtilities
{
  /// <summary>
  /// Implements an IObserver that writes lines to a stream
  /// </summary>
  public class LineWritingObserver: IObserver<string>
  {
    /// <summary>
    /// Create a line writing observer
    /// </summary>
    /// <param name="sink">
    /// The stream to write to
    /// </param>
    /// <param name="dispose">
    /// If true (default), completing this observer will dispose the sink stream.
    /// If false, completing this observer will not dispose the sink stream.
    /// </param>
    public LineWritingObserver(TextWriter sink, bool dispose = true)
    {
      Sink = sink;
      DoDispose = dispose;
    }

    /// <summary>
    /// Return a new IObserver{string} that send lines to STDOUT
    /// </summary>
    public static LineWritingObserver ToStdOut()
    {
      return new LineWritingObserver(Console.Out, false);
    }

    /// <summary>
    /// The opened stream to write to
    /// </summary>
    public TextWriter Sink { get; }

    /// <summary>
    /// If false, the sink will not be disposed when this observer is complete
    /// </summary>
    public bool DoDispose { get; }

    /// <summary>
    /// Close the Sink
    /// </summary>
    public void OnCompleted()
    {
      Sink.Dispose();
    }

    /// <summary>
    /// Implements IObserver. Ignores the error, assuming it will be handled otherwise
    /// </summary>
    public void OnError(Exception error)
    {
    }

    /// <summary>
    /// Writes the line
    /// </summary>
    public void OnNext(string value)
    {
      Sink.WriteLine(value);
    }
  }
}
