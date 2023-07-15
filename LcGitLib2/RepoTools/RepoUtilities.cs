/*
 * (c) 2021  VTT / TTELCL
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using LcGitLib.Cfg;

namespace LcGitLib.RepoTools
{
  /// <summary>
  /// Description of RepoUtilities
  /// </summary>
  public static class RepoUtilities
  {
    /// <summary>
    /// Locate the GIT folder for the given directory.
    /// </summary>
    /// <param name="startFolder">
    /// The directory from where to start searching
    /// </param>
    /// <returns>
    /// Returns null if not found or if a .nogit folder or file is found
    /// before a .git folder.
    /// Returns a ".git" folder for full repositories, or the "xxxxx.git" bare
    /// repository folder for bare repositories.
    /// </returns>
    public static string FindGitFolder(string startFolder)
    {
      if(String.IsNullOrEmpty(startFolder)) // terminate recursion
      {
        return null;
      }
      startFolder = NormalizeDirectoryName(startFolder);
      var score = LooksLikeGitFolder(startFolder);
      switch(score)
      {
        case 0:
          break; // handle after the switch
        case 1:
          throw new InvalidOperationException(
            $"This folder looks like a GIT folder but has a bad name: {startFolder}");
        case 2:
          return startFolder;
        case 3:
          return startFolder;
        default:
          throw new InvalidOperationException(
            "Unexpected git probe result");
      }
      var nogitItem = Path.Combine(startFolder, ".nogit");
      if(File.Exists(nogitItem) || Directory.Exists(nogitItem))
      {
        // Abort immediately
        return null;
      }
      var gitFolder = Path.Combine(startFolder, ".git");
      if(LooksLikeGitFolder(gitFolder)==3)
      {
        return gitFolder;
      }
      // Trick alert: Path.GetDirectoryName(@"C:\") returns null
      return FindGitFolder(Path.GetDirectoryName(startFolder));
    }

    /// <summary>
    /// Get a label for the repo, based on the name of the repo folder or
    /// the git folder in case of bare repos
    /// </summary>
    public static string RepoLabel(string startFolder)
    {
      var gitFolder = FindGitFolder(startFolder);
      if(gitFolder==null)
      {
        return null;
      }
      var shortName = Path.GetFileName(gitFolder).ToLowerInvariant();
      if(shortName == ".git")
      {
        var repoFolder = Path.GetDirectoryName(gitFolder);
        return Path.GetFileName(repoFolder);
      }
      else
      {
        if(!shortName.EndsWith(".git"))
        {
          throw new InvalidOperationException(
            "unexpected git folder name");
        }
        return Path.GetFileName(gitFolder)[..^4];
      }
    }

    /// <summary>
    /// Find the GIT config file for the repository the search anchor is in
    /// </summary>
    public static string FindGitConfigFile(string searchAnchorFolder)
    {
      var gitFolder = FindGitFolder(searchAnchorFolder);
      if(gitFolder==null)
      {
        return null;
      }
      var cfgFile = Path.Combine(gitFolder, "config");
      if(File.Exists(cfgFile))
      {
        return cfgFile;
      }
      else
      {
        return null;
      }
    }

    /// <summary>
    /// Returns a score for how likely the folder looks like it may be a .git folder
    /// (either as the ".git" folder inside a work folder or as a bare repository)
    /// Returns 0 if it is not a GIT folder.
    /// Returns 1 if it looks like a GIT folder, but has an unusual name.
    /// Returns 2 if it looks like a GIT folder and its name ends with ".git" but
    /// has more text before that. Most likely this is a bare repository
    /// Returns 3 if it looks like a GIT folder and its name is exactly ".git".
    /// </summary>
    public static int LooksLikeGitFolder(string folder)
    {
      folder = NormalizeDirectoryName(folder);
      if(!Directory.Exists(folder))
      {
        return 0;
      }
      if(!File.Exists(Path.Combine(folder, "config")))
      {
        return 0;
      }
      if(!Directory.Exists(Path.Combine(folder, "objects")))
      {
        return 0;
      }
      var name = Path.GetFileName(folder);
      if(name == ".git")
      {
        return 3;
      }
      if(name.EndsWith(".git"))
      {
        return 2;
      }
      return 1;
    }

    /// <summary>
    /// Return a normalized directory name: the full path, with trailing
    /// path separators removed if possible.
    /// </summary>
    public static string NormalizeDirectoryName(string folder)
    {
      if(String.IsNullOrEmpty(folder))
      {
        throw new InvalidOperationException("Folder name is null or empty");
      }
      // While this looks more complex than necessary, this handles boundary cases
      // correctly. In particular, it does not strip the separator after "C:\"
      return Path.GetDirectoryName(Path.Combine(Path.GetFullPath(folder), "x"));
    }

  }
}
