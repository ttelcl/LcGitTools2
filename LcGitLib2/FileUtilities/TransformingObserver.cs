/*
 * (c) 2021  VTT / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LcGitLib.FileUtilities
{
  /// <summary>
  /// An IObserver that transforms incoming objects to other objects
  /// </summary>
  public class TransformingObserver<TSource, TSink> : IObserver<TSource>
  {
    private readonly Func<TSource, TSink> _transform;

    /// <summary>
    /// Create a new TransformingObserver
    /// </summary>
    /// <param name="transform">
    /// The transform to apply
    /// </param>
    /// <param name="sink">
    /// The sink observer to write to
    /// </param>
    public TransformingObserver(Func<TSource, TSink> transform, IObserver<TSink> sink)
    {
      _transform = transform;
      Sink = sink;
    }

    /// <summary>
    /// The sink that output values are written to
    /// </summary>
    public IObserver<TSink> Sink { get; }

    /// <summary>
    /// Calls Sink.OnCompleted()
    /// </summary>
    public void OnCompleted()
    {
      Sink.OnCompleted();
    }

    /// <summary>
    /// Calls Sink.OnError(error)
    /// </summary>
    public void OnError(Exception error)
    {
      Sink.OnError(error);
    }

    /// <summary>
    /// Transforms the value and passes it to the sink (unless DropDefault applies)
    /// </summary>
    public void OnNext(TSource sourceValue)
    {
      var sinkValue = _transform(sourceValue);
      Sink.OnNext(sinkValue);
    }
  }
}
