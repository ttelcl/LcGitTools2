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
using Newtonsoft.Json.Linq;

namespace LcGitLib2.GitConfigFiles
{
  /// <summary>
  /// Common base for GIT Config sections and subsections
  /// </summary>
  public abstract class GitConfigSectionBase
  {
    private readonly List<GitConfigValue> _values;

    /// <summary>
    /// Create a new GitConfigSectionBase
    /// </summary>
    protected GitConfigSectionBase(string section, IEnumerable<GitConfigValue> values=null)
    {
      _values = new List<GitConfigValue>();
      Values = _values.AsReadOnly();
      Section = section.ToLowerInvariant();
      if(values!=null)
      {
        _values.AddRange(values);
      }
    }

    /// <summary>
    /// The bare section name (in lower case)
    /// </summary>
    [JsonProperty("section")]
    public string Section { get; }

    /// <summary>
    /// The values in this section. Value keys are not necessarily unique
    /// </summary>
    [JsonProperty("values")]
    public IReadOnlyList<GitConfigValue> Values { get; }

    /// <summary>
    /// Don't serialize values if there are none
    /// </summary>
    public bool ShouldSerializeValues()
    {
      return Values.Count > 0;
    }

    /// <summary>
    /// Add a previously parsed value. Null values are ignored
    /// </summary>
    public void AddValue(GitConfigValue value)
    {
      if(value != null)
      {
        _values.Add(value);
      }
    }

    /// <summary>
    /// Parse the value line and add it if it isn't blank
    /// </summary>
    public GitConfigValue AddLine(string line)
    {
      var gcv = GitConfigValue.Parse(line);
      if(gcv != null)
      {
        AddValue(gcv);
      }
      return gcv;
    }

    /// <summary>
    /// Enumerate the value objects for the given key. There may be zero, one or
    /// more values.
    /// </summary>
    public IEnumerable<string> FindValues(string key)
    {
      key = key.ToLowerInvariant();
      return _values.Where(gcv => gcv.Key == key).Select(gcv => gcv.Value);
    }

    /// <summary>
    /// Find the single value for key. Raise an exception if there are multiple
    /// results, or when there are none and the 'optional' flag is not set.
    /// </summary>
    public string FindValue(string key, bool optional=false)
    {
      var values = FindValues(key).ToList();
      if(values.Count>1)
      {
        throw new InvalidOperationException(
          $"Ambiguous result: there are multiple values for {key}");
      }
      if(values.Count<1 && !optional)
      {
        throw new KeyNotFoundException(
          $"No value found for key {key}");
      }
      return values.Count > 0 ? values[0] : null;
    }

    /// <summary>
    /// Find the single boolean value for the given key, raising an exception
    /// if there are no values or multiple values
    /// </summary>
    public bool FindBoolean(string key)
    {
      return GitConfigValue.ToBool(FindValue(key, false));
    }

    /// <summary>
    /// Find the single boolean value for the given key, raising an exception
    /// if there are multiple values, and returning the specified default if
    /// there are none.
    /// </summary>
    public bool FindBoolean(string key, bool defaultValue)
    {
      var value = FindValue(key, true);
      return value == null ? defaultValue : GitConfigValue.ToBool(value);
    }

    /// <summary>
    /// Export the values in a compact form
    /// </summary>
    public JObject CompactValues(bool forceArrays)
    {
      var container = new JObject();
      var grouped = _values.GroupBy(gcv => gcv.Key).OrderBy(x => x.Key).ToList();
      foreach(var group in grouped)
      {
        var values = group.ToList();
        if(values.Count>1 || forceArrays)
        {
          var ja = new JArray();
          foreach(var v in values)
          {
            ja.Add(SimpleJsonValue(v.Value));
          }
          container[group.Key] = ja;
        }
        else if(values.Count==1)
        {
          container[group.Key] = SimpleJsonValue(values[0].Value);
        }
        else
        {
          // should never happen
          container[group.Key] = JValue.CreateNull();
        }
      }
      return container;
    }

    /// <summary>
    /// Minimalistic JSON value compacter
    /// </summary>
    public static JValue SimpleJsonValue(string text)
    {
      switch(text.ToLowerInvariant())
      {
        case "true":
          return new JValue(true);
        case "false":
          return new JValue(false);
      }
      if(Int64.TryParse(text, out var number))
      {
        return new JValue(number);
      }
      return JValue.CreateString(text);
    }

    ///// <summary>
    ///// The key used to identify this section or subsection
    ///// </summary>
    //[JsonProperty("key")]
    //public string Key { get => GetKey(); }

    ///// <summary>
    ///// Get the key for thise section or subsection
    ///// </summary>
    ///// <returns></returns>
    //protected abstract string GetKey();

  }
}
