/*
 * (c) 2021  VTT / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LcGitLib.Cfg
{
  /// <summary>
  /// Extension methods on the other classes in this namespace
  /// </summary>
  public static class ConfigExtensions
  {
    /*
     * Design note:
     * 
     * For each supported type there is both a Set<type>() and a Set() method.
     * The Set() variant does not support null values though, to avoid overload
     * resolution conflicts.
     */

    /// <summary>
    /// Set a key to a string value in the config object.
    /// Returns the config object to enable a fluent syntax.
    /// </summary>
    public static ConfigObject SetString(
      this ConfigObject target,
      string key,
      string value)
    {
      target.Strings[key] = value;
      return target;
    }

    /// <summary>
    /// Set a key to a string value in the config object.
    /// Returns the config object to enable a fluent syntax.
    /// </summary>
    public static ConfigObject Set(
      this ConfigObject target,
      string key,
      string value)
    {
      return target.SetString(key, value);
    }

    /// <summary>
    /// Set a key to a nullable integer value in the config object.
    /// Returns the config object to enable a fluent syntax.
    /// </summary>
    public static ConfigObject SetInteger(
      this ConfigObject target,
      string key,
      long? value)
    {
      target.Integers[key] = value;
      return target;
    }

    /// <summary>
    /// Set a key to an integer value in the config object.
    /// Returns the config object to enable a fluent syntax.
    /// </summary>
    public static ConfigObject Set(
      this ConfigObject target,
      string key,
      long value)
    {
      return target.SetInteger(key, value);
    }

    /// <summary>
    /// Set a key to a nullable boolean value in the config object.
    /// Returns the config object to enable a fluent syntax.
    /// </summary>
    public static ConfigObject SetBoolean(
      this ConfigObject target,
      string key,
      bool? value)
    {
      target.Booleans[key] = value;
      return target;
    }

    /// <summary>
    /// Set a key to a boolean value in the config object.
    /// Returns the config object to enable a fluent syntax.
    /// </summary>
    public static ConfigObject Set(
      this ConfigObject target,
      string key,
      bool value)
    {
      return target.SetBoolean(key, value);
    }

  }
}
