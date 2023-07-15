/*
 * (c) 2021  VTT / TTELCL
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
  /// Observer that captures observed items. Optionally filters items
  /// </summary>
  public class CapturingObserver<T>: IObserver<T>
  {
    private Func<T, bool> _filter;

    /// <summary>
    /// Create a new CapturingObserver
    /// </summary>
    public CapturingObserver(
      Func<T, bool> filter = null)
    {
      CapturedItems = new List<T>();
      _filter = filter;
    }

    /// <summary>
    /// True after OnCompleted is called
    /// </summary>
    public bool Completed { get; private set; }

    /// <summary>
    /// The most recent Exception sent to OnError (if any)
    /// </summary>
    public Exception LastError { get; private set; }

    /// <summary>
    /// True if there was an error recorded
    /// </summary>
    public bool HasError { get => LastError != null; }

    /// <summary>
    /// The list of items captured so far
    /// </summary>
    public List<T> CapturedItems { get; }

    /// <summary>
    /// Implements IObservable
    /// Callback called when completed
    /// </summary>
    public void OnCompleted()
    {
      Completed = true;
    }

    /// <summary>
    /// Implements IObservable
    /// Stores the error in the LastError field
    /// </summary>
    public void OnError(Exception error)
    {
      LastError = error;
    }

    /// <summary>
    /// Implements IObservable
    /// Adds the item to the CapturedItems list, if it meets the filter requirement
    /// </summary>
    public void OnNext(T value)
    {
      if(_filter==null || _filter(value))
      {
        CapturedItems.Add(value);
      }
    }
  }
}
