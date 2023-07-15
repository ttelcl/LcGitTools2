/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LcGitLib2.GitModels;

/// <summary>
/// Represents a GIT identifier (expressed as a 40 character hex string)
/// </summary>
public readonly struct GitId: IEquatable<GitId>
{
  /// <summary>
  /// Create a new GitId
  /// </summary>
  /// <param name="id">
  /// The identifier, or null or empty as alias for "0000000000000000000000000000000000000000"
  /// </param>
  public GitId(string id)
  {
    if(string.IsNullOrEmpty(id))
    {
      id = "0000000000000000000000000000000000000000";
    }
    if(!IsValidId(id))
    {
      throw new ArgumentException(
        $"Not a valid GIT object id: '{id}'");
    }
    Id = id;
  }

  /// <summary>
  /// The identifier, a 40 character lower case hex string
  /// </summary>
  public string Id { get; init; }

  /// <summary>
  /// Check if the string is a validly formatted GIT object id string
  /// </summary>
  public static bool IsValidId(string id)
  {
    return Regex.IsMatch(id, "^[a-f0-9]{40}$");
  }

  /// <summary>
  /// The null GitId
  /// </summary>
  public static GitId Zero { get; } = new("0000000000000000000000000000000000000000");

  /// <summary>
  /// Override to return the Id string itself.
  /// </summary>
  public override string ToString()
  {
    return Id;
  }

  /// <inheritdoc/>
  public override bool Equals(object obj)
  {
    return obj is GitId id && Equals(id);
  }

  /// <inheritdoc/>
  public bool Equals(GitId other)
  {
    return Id == other.Id;
  }

  /// <inheritdoc/>
  public override int GetHashCode()
  {
    return HashCode.Combine(Id);
  }

  /// <inheritdoc/>
  public static bool operator ==(GitId left, GitId right)
  {
    return left.Equals(right);
  }

  /// <inheritdoc/>
  public static bool operator !=(GitId left, GitId right)
  {
    return !(left == right);
  }
}