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

namespace LcGitLib2.GitConfigFiles;

/// <summary>
/// A key-value pair in a GIT config file. Beware that keys are not necessarily unique!
/// This model represents all values as strings, since the logic of detecting boolean and
/// integer values would be too fragile to be usable. Instead there are AsBoolean and AsInteger
/// properties
/// </summary>
public class GitConfigValue
{
  /// <summary>
  /// Create a new GitConfigValue
  /// </summary>
  public GitConfigValue(string key, string value)
  {
    Key = key;
    Value = value;
  }

  /// <summary>
  /// The key
  /// </summary>
  [JsonProperty("key")]
  public string Key { get; }

  /// <summary>
  /// The value, as a string
  /// </summary>
  [JsonProperty("value")]
  public string Value { get; }

  /// <summary>
  /// Interpret the value as a boolean.
  /// See https://git-scm.com/docs/git-config#Documentation/git-config.txt-boolean
  /// </summary>
  [JsonIgnore]
  public bool AsBoolean { get => ToBool(Value); }

  /// <summary>
  /// Interpret the given value as boolean
  /// </summary>
  public static bool ToBool(string value)
  {
    // See https://git-scm.com/docs/git-config#Documentation/git-config.txt-boolean
    switch(value.ToLowerInvariant())
    {
      case "false":
      case "0":
      case "off":
      case "no":
      case "":
        return false;
      case "true":
      case "1":
      case "on":
      case "yes":
        // note the case of "no '=' on the line" has to be handled in the parser already
        return true;
      default:
        throw new InvalidOperationException(
          $"Value '{value}' is not recognized as boolean");
    }
  }

  // TBD: 'AsInteger' (beware 'k', 'M', etc. suffixes)

  /// <summary>
  /// Parse a value line, returning the parsed GitConfigValue, or null if
  /// the line was whitespace-only or a comment
  /// </summary>
  public static GitConfigValue? Parse(string line)
  {
    if(String.IsNullOrEmpty(line))
    {
      // for now just ignore (the alternative is to treat this case as error)
      return null;
    }
    if(!Char.IsWhiteSpace(line[0]))
    {
      throw new InvalidOperationException(
        "Expecting lines in the value section to start with whitespace");
    }
    // test trailing '\' *before* trimming whitespace
    if(line[^1]=='\\')
    {
      throw new NotSupportedException(
        $"Continuation lines are currently not supported: {line}");
    }
    line = line.Trim();
    if(String.IsNullOrEmpty(line) || line[0] == '#' || line[0] == ';')
    {
      return null;
    }
    var parts = line.Split(new[] { '=' }, 2);
    var key = parts[0].Trim().ToLowerInvariant(); // key is case insensitive, according to the spec.
    var value = parts.Length > 1 ? parts[1].Trim() : "true"; // value is case sensitive. 'true' is the default value
    return new GitConfigValue(key, value);
  }

}
