/*
 * (c) 2021  VTT / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace LcGitLib.Cfg
{
  /// <summary>
  /// Provides a typed view over a ConfigObject
  /// </summary>
  public abstract class ConfigView<T>
  {
    /// <summary>
    /// Create a new ConfigView
    /// </summary>
    protected ConfigView(
      ConfigObject host,
      MissingBehaviour missingBehaviour = MissingBehaviour.Abort)
    {
      Host = host;
      MissingMode = missingBehaviour;
      if(MissingMode==MissingBehaviour.Create)
      {
        throw new ArgumentOutOfRangeException(
          nameof(missingBehaviour),
          "'MissingBehaviour.Create' is not supported here");
      }
    }

    /// <summary>
    /// The host object over which this is a view
    /// </summary>
    public ConfigObject Host { get; }

    /// <summary>
    /// Behaviour when the value is missing
    /// </summary>
    public MissingBehaviour MissingMode { get; }

    /// <summary>
    /// Get or set a typed value
    /// </summary>
    public T this[string name]
    {
      get {
        var jt = Host[name, MissingMode];
        return ConvertValue(jt);
      }
      set {
        var jt = AdaptValue(value);
        Host[name] = jt;
      }
    }

    /// <summary>
    /// Convert a JSON value from the store into a primitive
    /// </summary>
    /// <param name="jt">
    /// The value read from the store, possibly null (if MissingMode allows so)
    /// </param>
    /// <returns>
    /// The primitive value converted from raw value
    /// </returns>
    protected abstract T ConvertValue(JToken jt);

    /// <summary>
    /// Adapt a primitive value for insertion into the store
    /// </summary>
    protected abstract JToken AdaptValue(T t);

  }

  internal class StringConfigView: ConfigView<string>
  {
    public StringConfigView(
      ConfigObject host,
      bool required)
      : base(host, required ? MissingBehaviour.Abort : MissingBehaviour.ReturnNull)
    {
    }

    protected override JToken AdaptValue(string t)
    {
      if(t==null)
      {
        return JValue.CreateNull();
      }
      else
      {
        return JValue.CreateString(t);
      }
    }

    protected override string ConvertValue(JToken jt)
    {
      if(jt==null || jt.Type == JTokenType.Null)
      {
        return null;
      }
      else if(jt.Type == JTokenType.String)
      {
        return (string)jt;
      }
      else
      {
        throw new InvalidOperationException(
          $"Expecting a string or null value but got a '{jt.Type}'");
      }
    }
  }

  internal class BoolConfigView: ConfigView<bool?>
  {
    public BoolConfigView(
      ConfigObject host,
      MissingBehaviour missingBehaviour = MissingBehaviour.ReturnNull)
      : base(host, missingBehaviour)
    {
    }

    protected override JToken AdaptValue(bool? t)
    {
      if(t.HasValue)
      {
        return new JValue(t.Value);
      }
      else
      {
        return JValue.CreateNull();
      }
    }

    protected override bool? ConvertValue(JToken jt)
    {
      if(jt==null || jt.Type == JTokenType.Null)
      {
        return null;
      }
      else if(jt.Type == JTokenType.Boolean)
      {
        return (bool)jt;
      }
      else
      {
        throw new InvalidOperationException(
          $"Expecting a boolean or null value but got a '{jt.Type}'");
      }
    }

  }

  internal class LongConfigView: ConfigView<long?>
  {
    public LongConfigView(
      ConfigObject host,
      MissingBehaviour missingBehaviour = MissingBehaviour.ReturnNull)
      : base(host, missingBehaviour)
    {
    }

    protected override JToken AdaptValue(long? t)
    {
      if(t.HasValue)
      {
        return new JValue(t.Value);
      }
      else
      {
        return JValue.CreateNull();
      }
    }

    protected override long? ConvertValue(JToken jt)
    {
      if(jt == null || jt.Type == JTokenType.Null)
      {
        return null;
      }
      else if(jt.Type == JTokenType.Integer)
      {
        return (long)jt;
      }
      else
      {
        throw new InvalidOperationException(
          $"Expecting an integer or null value but got a '{jt.Type}'");
      }
    }

  }

}
