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

using Newtonsoft.Json;

namespace LcGitLib2.FileUtilities;

/// <summary>
/// Defines how to write multiple objects as JSON
/// </summary>
public enum JsonMode
{
  /// <summary>
  /// Simulate the objects being wrapped in a JSON array, writing objects
  /// as single lines
  /// </summary>
  JsonArray,

  /// <summary>
  /// Simulate  the objects being wrapped in a JSON array, writing objects
  /// as indented blobs
  /// </summary>
  JsonArrayIndent,

  /// <summary>
  /// JSONL mode - write each object as a single line
  /// </summary>
  JsonL,

  /// <summary>
  /// MJSON mode - write each object as an indented JSON blob (followed by
  /// a line break)
  /// </summary>
  MultiJson,
}

/// <summary>
/// Implements an observer for writing objects as lines
/// of JSON
/// </summary>
public class JsonWritingObserver: IObserver<object>
{
  private string? _previousItem;

  /// <summary>
  /// Create a new JsonWritingObserver
  /// </summary>
  public JsonWritingObserver(JsonMode mode, IObserver<string> sink)
  {
    Mode = mode;
    Sink = sink;
    _previousItem = null;
    switch(Mode)
    {
      case JsonMode.JsonArray:
      case JsonMode.JsonArrayIndent:
        Sink.OnNext("[");
        break;
      case JsonMode.JsonL:
      case JsonMode.MultiJson:
        break;
      default:
        throw new InvalidOperationException(
          "Unknown JsonMode");
    }
  }

  /// <summary>
  /// Find a JSON mode that matches the extension of the given file name
  /// </summary>
  public static JsonMode ModeForExtension(string fileName)
  {
    var ext = Path.GetExtension(fileName).ToLowerInvariant();
    switch(ext)
    {
      case ".json":
      case ".jsonc":
        return JsonMode.JsonArrayIndent;
      case ".jsonl":
      case ".ljson":
        return JsonMode.JsonL;
      case ".mjson":
      case ".jsonm":
        return JsonMode.MultiJson;
      default:
        throw new InvalidOperationException(
          $"Unsupported JSON-like file extension: '{ext}'");
    }
  }

  /// <summary>
  /// How to write a series of JSON items
  /// </summary>
  public JsonMode Mode { get; }

  /// <summary>
  /// The sink to write to
  /// </summary>
  public IObserver<string> Sink { get; }

  /// <summary>
  /// Notify the writing is complete
  /// </summary>
  public void OnCompleted()
  {
    switch(Mode)
    {
      case JsonMode.JsonArray:
      case JsonMode.JsonArrayIndent:
        if(_previousItem != null)
        {
          Sink.OnNext(_previousItem);
        }
        Sink.OnNext("]");
        break;
      case JsonMode.JsonL:
      case JsonMode.MultiJson:
        break;
      default:
        throw new InvalidOperationException(
          "Unknown JsonMode");
    }
    Sink.OnCompleted();
  }

  /// <summary>
  /// Notify there was an error
  /// </summary>
  public void OnError(Exception error)
  {
    Sink.OnError(error);
  }

  /// <summary>
  /// Write the next value
  /// </summary>
  public void OnNext(object value)
  {
    string item;
    switch(Mode)
    {
      case JsonMode.JsonArray:
      case JsonMode.JsonL:
        item = JsonConvert.SerializeObject(value, Formatting.None);
        break;
      case JsonMode.JsonArrayIndent:
      case JsonMode.MultiJson:
        item = JsonConvert.SerializeObject(value, Formatting.Indented);
        break;
      default:
        throw new InvalidOperationException(
          "Unknown JsonMode");
    }
    switch(Mode)
    {
      case JsonMode.JsonArray:
      case JsonMode.JsonArrayIndent:
        if(_previousItem != null)
        {
          Sink.OnNext(_previousItem + ",");
        }
        _previousItem = item;
        break;
      case JsonMode.JsonL:
      case JsonMode.MultiJson:
        Sink.OnNext(item);
        break;
      default:
        throw new InvalidOperationException(
          "Unknown JsonMode");
    }
  }
}
