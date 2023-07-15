/*
 * (c) 2021  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Newtonsoft.Json.Linq;

namespace LcGitLib2.Cfg;

/// <summary>
/// Wraps a JObject used to store settings
/// </summary>
public class ConfigObject
{
  private readonly JObject _content;

  /// <summary>
  /// Create a new ConfigObject
  /// </summary>
  internal ConfigObject(ISavable root, JObject content, string label)
  {
    Root = root ?? new NotSavable();
    IsRoot = label == null;
    Label = label ?? "<root>";
    _content = content;
    Strings = new StringConfigView(this, false);
    StringsRequired = new StringConfigView(this, true);
    Integers = new LongConfigView(this);
    Booleans = new BoolConfigView(this);
  }

  /// <summary>
  /// The underlying content object
  /// </summary>
  public JObject Content { get => _content; }

  /// <summary>
  /// The ConfigBlob that this ConfigObject resides in
  /// </summary>
  public ISavable Root { get; }

  /// <summary>
  /// Get a label identifying this ConfigObject
  /// </summary>
  public string Label { get; }

  /// <summary>
  /// True if this wraps a root object
  /// </summary>
  public bool IsRoot { get; }

  /// <summary>
  /// Returns true if the specified key exists. Note that its value may be null
  /// even if it exists.
  /// </summary>
  public bool Contains(string key)
  {
    return _content.ContainsKey(key);
  }

  /// <summary>
  /// True if all keys are present
  /// </summary>
  public bool ContainsAll(IEnumerable<string> keys)
  {
    return keys.All(key => _content.ContainsKey(key));
  }

  /// <summary>
  /// Lookup or create a child ConfigObject (persisted as an object-valued property)
  /// </summary>
  public ConfigObject Child(string name, MissingBehaviour missingBehaviour = MissingBehaviour.Abort)
  {
    var value = _content[name];
    if(value==null || value.Type == JTokenType.Null)
    {
      switch(missingBehaviour)
      {
        case MissingBehaviour.Abort:
          throw new KeyNotFoundException(
            $"Required child object is missing: '{name}' in '{Label}'");
        case MissingBehaviour.ReturnNull:
          return null;
        case MissingBehaviour.Create:
          if(Root.ReadOnly)
          {
            throw new InvalidOperationException(
              $"Cannot create child node '{name}' because the host is read-only");
          }
          var jo = new JObject();
          _content[name] = jo;
          Root.SetDirty();
          var childLabel = IsRoot ? name : (Label + "." + name);
          var co = new ConfigObject(Root, jo, childLabel);
          return co;
        default:
          throw new NotImplementedException(
            $"Unknown 'missing behaviour' mode {missingBehaviour}");
      }
    }
    if(value is JObject child)
    {
      var isRoot = Label.StartsWith("<");
      var childLabel = isRoot ? name : (Label + "." + name);
      return new ConfigObject(Root, child, childLabel);
    }
    else
    {
      throw new InvalidOperationException(
        $"Expecting element to be a JSON object, but got a '{value.Type}' for '{name}' in '{Label}'");
    }
  }

  /// <summary>
  /// Import the content of a JObject, overwriting existing primitive values
  /// and recursively importing child objects
  /// </summary>
  public void Import(JObject other, bool replace = false)
  {
    if(Root.ReadOnly)
    {
      throw new InvalidOperationException(
        $"Cannot import the external node because the host is read-only");
    }
    Root.SetDirty(); // assume there will be some changes
    if(replace)
    {
      Clear();
    }
    foreach(var prop in other.Properties())
    {
      if(prop.Value is JValue v)
      {
        _content[prop.Name] = new JValue(v);
      }
      else if(prop.Value is JObject jo)
      {
        var child = Child(prop.Name, MissingBehaviour.Create);
        child.Import(jo);
      }
      else
      {
        throw new InvalidOperationException(
          $"Unsupported JSON type: '{prop.Value.Type}'");
      }
    }
  }

  /// <summary>
  /// Import the content of another config object, overwriting existing primitive values
  /// and recursively importing child objects
  /// </summary>
  public void Import(ConfigObject other, bool replace = false)
  {
    Import(other.Content, replace);
  }

  /// <summary>
  /// Remove all values
  /// </summary>
  public void Clear()
  {
    if(Root.ReadOnly)
    {
      throw new InvalidOperationException(
        $"Cannot clear the object because the host is read-only");
    }
    Root.SetDirty();
    _content.RemoveAll();
  }

  /// <summary>
  /// Removes the named property
  /// </summary>
  public bool Remove(string key)
  {
    if(Root.ReadOnly)
    {
      throw new InvalidOperationException(
        $"Cannot remove key '{key}' because the host is read-only");
    }
    Root.SetDirty();
    return _content.Remove(key);
  }

  /// <summary>
  /// Get or set a raw value
  /// </summary>
  /// <param name="name">
  /// The name of the property to set
  /// </param>
  /// <param name="missingBehaviour">
  /// The behaviour when the item is missing. "Create" inserts a JSON null value
  /// </param>
  /// <returns>
  /// The value found, or null. JSON null values are returned as a C# null
  /// </returns>
  public JToken this[string name, MissingBehaviour missingBehaviour = MissingBehaviour.ReturnNull] {
    get {
      var v = _content[name];
      if(v==null || v.Type == JTokenType.Null)
      {
        switch(missingBehaviour)
        {
          case MissingBehaviour.Abort:
            throw new KeyNotFoundException(
              $"Required key is missing: '{name}' in '{Label}'");
          case MissingBehaviour.ReturnNull:
            return null;
          case MissingBehaviour.Create:
            if(v==null)
            {
              if(Root.ReadOnly)
              {
                throw new InvalidOperationException(
                  $"Cannot inject an explicit null for '{name}' because the host is read-only");
              }
              _content[name] = JValue.CreateNull();
              Root.SetDirty();
            }
            return null;
          default:
            throw new NotImplementedException(
              $"Unknown 'missing behaviour' mode {missingBehaviour}");
        }
      }
      else
      {
        return v;
      }
    }
    set {
      if(Root.ReadOnly)
      {
        throw new InvalidOperationException(
          $"Can set property '{name}' because the host is read-only");
      }
      var v = value ?? JValue.CreateNull();
      _content[name] = v;
      Root.SetDirty();
    }
  }

  /// <summary>
  /// View the items as string valued, allowing missing or null values
  /// Items that are not null or a string cause an exception on read.
  /// </summary>
  public ConfigView<string> Strings { get; }

  /// <summary>
  /// View the items as string valued, disallowing missing or null values
  /// Items that are not a string cause an exception on read.
  /// </summary>
  public ConfigView<string> StringsRequired { get; }

  /// <summary>
  /// View the items as nullable integers.
  /// Items that are not null or an integer cause an exception on read.
  /// </summary>
  public ConfigView<long?> Integers { get; }

  /// <summary>
  /// View the items as nullable booleans.
  /// Items that are not null or a boolean cause an exception on read.
  /// </summary>
  public ConfigView<bool?> Booleans { get; }

}
