/*
 * (c) 2021  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace LcGitLib2.RawLog;

/// <summary>
/// A commit header line value refering to a user and a time stamp
/// </summary>
public class UserLine
{
  /// <summary>
  /// Create a new UserLine
  /// </summary>
  public UserLine(string user, long stamp, string timeZone)
  {
    User = user;
    Stamp = stamp;
    TimeZone = timeZone;
    var dto0 = DateTimeOffset.FromUnixTimeSeconds(Stamp);
    var offsetHours = Int32.Parse(TimeZone.Substring(1, 2));
    var offsetMinutes = Int32.Parse(TimeZone.Substring(3, 2));
    var offsetSign = TimeZone[0] == '-' ? -1 : 1;
    var offsetSpan = TimeSpan.FromMinutes(offsetSign * (offsetMinutes + 60 * offsetHours));
    TimeStamp = dto0.ToOffset(offsetSpan);
  }

  /// <summary>
  /// Parse the value part of a header line into a UserLine object
  /// </summary>
  public static UserLine Parse(string value)
  {
    var match = Regex.Match(
      value,
      @"^(.+) (\d{8}\d+) ([-+]\d{4})$");
    if(!match.Success)
    {
      throw new InvalidOperationException(
        $"Unexpected user+timestamp format in '{value}'");
    }
    var user = match.Groups[1].Value;
    var stamp = Int64.Parse(match.Groups[2].Value);
    var zone = match.Groups[3].Value;
    return new UserLine(user, stamp, zone);
  }

  /// <summary>
  /// The user name
  /// </summary>
  [JsonProperty("user")]
  public string User { get; }

  /// <summary>
  /// raw unix time stamp, in seconds since the epoch
  /// </summary>
  [JsonProperty("stamp")]
  public long Stamp { get; }

  /// <summary>
  /// raw time zone indicator
  /// </summary>
  [JsonProperty("zone")]
  public string TimeZone { get; }

  /// <summary>
  /// The interpreted time stamp 
  /// </summary>
  [JsonIgnore]
  public DateTimeOffset TimeStamp { get; }

  /// <summary>
  /// The interpreted timestamp, in textual form (derived from the raw values)
  /// </summary>
  [JsonProperty("stamptext")]
  public string ZonedTime { get => TimeStamp.ToString("yyyy-MM-dd HH:mm:ss zzz"); }

  /// <summary>
  /// Returns true if the other UserLine is equivalent
  /// </summary>
  public bool IsSame(UserLine? other)
  {
    if(other == null)
    {
      return false;
    }
    return User == other.User && Stamp == other.Stamp && TimeZone == other.TimeZone;
  }
}
