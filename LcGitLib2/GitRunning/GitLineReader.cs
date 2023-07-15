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

namespace LcGitLib2.GitRunning;

/// <summary>
/// Reads lines of text produced by GIT.exe, patching some
/// peculiarities that would trip up a normal Stream -> StreamReader.
/// </summary>
/// <remarks>
/// <para>
/// (1) The resulting lines are decoded as UTF8
/// </para>
/// <para>
/// (2) CR characters are translated to ' ' and do NOT count as end-of-line
/// </para>
/// </remarks>
public class GitLineReader
{
  private readonly byte[] _buffer;
  private int _endPtr;
  private bool _finished;

  /// <summary>
  /// Create a new GitLineReader
  /// </summary>
  public GitLineReader()
  {
    _buffer = new byte[65536];
    _endPtr = 0;
    _finished = false;
  }

  /// <summary>
  /// Read the next line, returning null when done.
  /// </summary>
  public string? ReadLine(Stream source)
  {
    var idx = FindFirst(0x0A);
    if(idx >= 0)
    {
      // If the last character before the LF is a CR, then ignore it
      var lineLength = (idx > 0 && _buffer[idx - 1] == 0x0D) ? idx - 1 : idx;
      // Patch any other 0x0D characters to space (0x20)
      for(var i = 0; i < lineLength; i++)
      {
        if(_buffer[i] == 0x0D)
        {
          _buffer[i] = 0x20;
        }
      }
      var line = Encoding.UTF8.GetString(_buffer, 0, lineLength);
      // shift the line out of the buffer
      var skip = idx + 1;
      var remaining = _endPtr - skip;
      //for(var i = 0; i < remaining; i++)
      //{
      //  _buffer[i] = _buffer[i + skip];
      //}
      Array.Copy(_buffer, skip, _buffer, 0, remaining);
      _endPtr = remaining;
      return line;
    }
    else
    {
      if(_finished)
      {
        // cannot refill. But there may be spare data left
        if(_endPtr > 0)
        {
          if(_endPtr >= _buffer.Length)
          {
            throw new NotSupportedException(
              "Line too long");
          }
          // fake a final line by just adding an extra LF
          _buffer[_endPtr++] = 0x0A;
        }
        else
        {
          // we really are finished.
          return null;
        }
      }
      else
      {
        if(_endPtr >= _buffer.Length)
        {
          throw new NotSupportedException(
            "Line too long");
        }
        // refill
        var n = source.Read(_buffer, _endPtr, _buffer.Length - _endPtr);
        if(n == 0)
        {
          _finished = true;
        }
        else
        {
          _endPtr += n;
        }
      }
      // recurse
      return ReadLine(source);
    }
  }

  /// <summary>
  /// Read all lines from the source
  /// </summary>
  public IEnumerable<string> ReadLines(Stream source)
  {
    string? line;
    while((line = ReadLine(source)) != null)
    {
      yield return line;
    }
  }

  private int FindFirst(byte character)
  {
    for(var i = 0; i < _endPtr; i++)
    {
      if(character == _buffer[i])
      {
        return i;
      }
    }
    return -1;
  }

}