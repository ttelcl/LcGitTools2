/*
 * (c) 2021  VTT / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LcGitLib.GitConfigFiles
{
  /// <summary>
  /// A section in a GIT config file
  /// </summary>
  public class GitConfigSection: GitConfigSectionBase
  {
    private readonly Dictionary<string, GitConfigSubsection> _subsections;

    /// <summary>
    /// Create a new GitConfigSection, initially empty
    /// </summary>
    public GitConfigSection(
      string section,
      IEnumerable<GitConfigValue> values=null,
      IReadOnlyDictionary<string,GitConfigSubsection> subsections=null)
      : base(section, values)
    {
      _subsections = new Dictionary<string, GitConfigSubsection>(StringComparer.InvariantCulture);
      Subsections = _subsections;
      if(subsections!=null)
      {
        foreach(var kvp in subsections)
        {
          _subsections[kvp.Key] = kvp.Value;
        }
      }
    }

    ///// <summary>
    ///// Implements GetKey to just return the section name
    ///// </summary>
    //protected override string GetKey()
    //{
    //  return Section;
    //}

    /// <summary>
    /// The subsections
    /// </summary>
    [JsonProperty("subsections")]
    public IReadOnlyDictionary<string, GitConfigSubsection> Subsections { get; }

    /// <summary>
    /// Tell the JSON serializer not to serialize subsections if there are none
    /// </summary>
    public bool ShouldSerializeSubsections()
    {
      return _subsections.Count > 0;
    }

    /// <summary>
    /// Find a subsection by name, optionally creating a blank one
    /// </summary>
    public GitConfigSubsection FindSubsection(string subsection, bool create=false)
    {
      if(!_subsections.TryGetValue(subsection, out var gcss))
      {
        if(create)
        {
          gcss = new GitConfigSubsection(Section, subsection);
          _subsections[subsection] = gcss;
        }
        else
        {
          gcss = null;
        }
      }
      return gcss;
    }


    /// <summary>
    /// Returns a compact representation
    /// </summary>
    public JObject ToCompactJson(bool forceArrays)
    {
      var o = new JObject();
      var smashed = !forceArrays;
      if(Values.Count>0)
      {
        if(smashed)
        {
          o = CompactValues(forceArrays);
        }
        else
        {
          o["values"] = CompactValues(forceArrays);
        }
      }
      if(Subsections.Count>0)
      {
        var sso = smashed ? o : new JObject();
        if(!smashed)
        {
          o["subsections"] = sso;
        }
        var subs = _subsections.Values.OrderBy(gcss => gcss.Subsection);
        foreach(var sub in subs)
        {
          sso[sub.Subsection] = sub.ToCompactJson(forceArrays);
        }
      }
      return o;
    }


  }
}