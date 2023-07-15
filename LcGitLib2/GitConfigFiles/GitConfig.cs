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
using Newtonsoft.Json.Linq;

namespace LcGitLib2.GitConfigFiles
{
  /// <summary>
  /// Represents the content of a GIT config file (the "config" file in
  /// a GIT repository).
  /// See https://git-scm.com/docs/git-config#_syntax for the spec.
  /// </summary>
  public class GitConfig
  {
    private readonly Dictionary<string, GitConfigSection> _sections;

    /// <summary>
    /// Create a new GitConfig
    /// </summary>
    public GitConfig(IReadOnlyDictionary<string, GitConfigSection> sections=null)
    {
      _sections = new Dictionary<string, GitConfigSection>(StringComparer.InvariantCultureIgnoreCase);
      Sections = _sections;
      if(sections!=null)
      {
        foreach(var kvp in sections)
        {
          _sections[kvp.Key] = kvp.Value;
        }
      }
    }

    /// <summary>
    /// Create a new GitConfig and load it from the specified file
    /// </summary>
    public static GitConfig FromFile(string fileName)
    {
      var gcfg = new GitConfig();
      gcfg.LoadFrom(fileName);
      return gcfg;
    }

    /// <summary>
    /// The sections and subsections.
    /// </summary>
    [JsonProperty("sections")]
    public IReadOnlyDictionary<string, GitConfigSection> Sections { get; }

    /// <summary>
    /// Find a section, optionally creating it
    /// </summary>
    public GitConfigSection FindSection(string section, bool create = false)
    {
      if(!_sections.TryGetValue(section, out var gcs))
      {
        if(create)
        {
          gcs = new GitConfigSection(section);
          _sections[gcs.Section] = gcs;
          return gcs;
        }
        else
        {
          return null;
        }
      }
      else
      {
        return gcs;
      }
    }

    /// <summary>
    /// Find a subsection, optionally creating it and its containing section
    /// </summary>
    public GitConfigSubsection FindSubsection(string section, string subsection, bool create = false)
    {
      var gcs = FindSection(section, create);
      if(gcs==null)
      {
        return null;
      }
      var gcss = gcs.FindSubsection(subsection, create);
      return gcss;
    }

    /// <summary>
    /// Parse lines from a config file and insert them into this config object, 
    /// creating new sections and subsections and/or extending existing ones.
    /// </summary>
    public void ParseLines(IEnumerable<string> lines)
    {
      GitConfigSectionBase currentSection = null;
      foreach(var line in lines)
      {
        if(!String.IsNullOrWhiteSpace(line)) // else: ignore blank line
        {
          if(line.StartsWith("["))
          {
            // start new section
            var idx = line.IndexOf(']');
            if(idx<0)
            {
              throw new InvalidOperationException(
                $"Malformed section header, missing ']': {line}");
            }
            var idx2 = line.IndexOf(']', idx + 1);
            if(idx2>=0)
            {
              // fixing this would be a rather deep rabbit hole (there may be
              // ']' characters in the subsection name or in a comment or both)
              throw new NotSupportedException(
                $"Multiple ']' characters in header line: {line}");
            }
            var header = line.Substring(1, idx - 1);
            var parts = header.Split(new[] { ' ' }, 2);
            var sectionName = parts[0].ToLowerInvariant().Trim();
            var subsectionName = parts.Length > 1 ? parts[1].Trim() : null;
            if(String.IsNullOrEmpty(subsectionName))
            {
              subsectionName = null;
            }
            else
            {
              if(subsectionName.StartsWith('"'))
              {
                if(!subsectionName.EndsWith('"'))
                {
                  throw new InvalidOperationException(
                    $"Malformed quoting in subsection name: {header}");
                }
                subsectionName = subsectionName[1..^1]; // C#8 feature
              }
            }
            if(subsectionName==null)
            {
              currentSection = FindSection(sectionName, true);
            }
            else
            {
              currentSection = FindSubsection(sectionName, subsectionName, true);
            }
          }
          else if(line[0] == '#' || line[0] == ';')
          {
            // ignore (comment) line
          }
          else if(!Char.IsWhiteSpace(line[0]))
          {
            throw new InvalidOperationException(
              "Expecting a line starting with whitespace, '[', '#' or ';'");
          }
          else
          {
            if(currentSection==null)
            {
              throw new InvalidOperationException(
                $"Encountered a value line without preceding section header.");
            }
            var gcv = GitConfigValue.Parse(line);
            if(gcv!=null)
            {
              currentSection.AddValue(gcv);
            }
          }
        }
      }
    }

    /// <summary>
    /// Read config content from the given file
    /// </summary>
    public void LoadFrom(string fileName)
    {
      ParseLines(File.ReadLines(fileName));
    }

    /// <summary>
    /// Return the 'bare' flag in the 'core' section
    /// </summary>
    [JsonIgnore]
    public bool IsBare { get => FindSection("core").FindBoolean("bare"); }

    /// <summary>
    /// Return a map from remotes to their URLs
    /// </summary>
    public Dictionary<string, string> RemoteUrls()
    {
      var map = new Dictionary<string, string>();
      var remotes = FindSection("remote");
      if(remotes!=null)
      {
        foreach(var (key, value) in remotes.Subsections)
        {
          map[key] = value.FindValue("url", true);
        }
      }
      return map;
    }

    /// <summary>
    /// Returns a compact representation
    /// </summary>
    public JObject ToCompactJson(bool forceArrays)
    {
      var o = new JObject();
      var sections = _sections.Values.OrderBy(s => s.Section);
      foreach(var section in sections)
      {
        o[section.Section] = section.ToCompactJson(forceArrays);
      }
      return o;
    }

  }
}
