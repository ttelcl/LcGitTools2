using System;
using System.Linq;

using LcGitLib2.GitRunning;

using Xunit;
using Xunit.Abstractions;

namespace UnitTest.LcGitLib2;

public class LcGitLib2Tests
{
  private readonly ITestOutputHelper _outputHelper;

  /*
   * Reminder on getting test output visible when testing from the console:
   *   dotnet test --logger:"console;verbosity=detailed"
   * (https://stackoverflow.com/a/61182227/271323)
   */

  public LcGitLib2Tests(ITestOutputHelper outputHelper)
  {
    _outputHelper = outputHelper;
  }

  [Fact]
  public void CanFindGit()
  {
    var gitPath = LcGitConfig.LocateGitExecutable();
    Assert.NotNull(gitPath);
    _outputHelper.WriteLine($"found GIT at: '{gitPath}'");
  }

  [Fact]
  public void CanRunGit()
  {
    //var cfg = LcGitConfig.GetDefault();
    //Assert.NotNull(cfg);
    var host = new GitCommandHost(null, null);
    var cmd =
      host.NewCommand()
      .WithoutCommand()
      .AddPost1("--version");
    var lines = cmd.RunToLines(out var exitCode);
    Assert.NotNull(lines);
    Assert.True(lines.Count > 0);
    Assert.Equal(0, exitCode);
    _outputHelper.WriteLine($"Received {lines.Count} output lines");
    foreach(var line in lines)
    {
      _outputHelper.WriteLine($" -> '{line}'");
    }
  }

  [Fact]
  public void CanGetCommitMap()
  {
    var host = new GitCommandHost(null, null);
    var commitMap = host.LoadCommitMap(/*@"k:\src\github\Newtonsoft.Json"*/);
    Assert.NotNull(commitMap);
    var missing = commitMap.MissingNodes.Select(n => n.Id.ShortId).ToList()!;
    var roots = commitMap.Roots.Select(n => n.Id.ShortId).ToList()!;
    var tips = commitMap.Tips.Select(n => n.Id.ShortId).ToList()!;
    _outputHelper.WriteLine($"{missing.Count} Missing: {String.Join(", ", missing)}");
    _outputHelper.WriteLine($"{roots.Count} Roots: {String.Join(", ", roots)}");
    _outputHelper.WriteLine($"{tips.Count} Tips: {String.Join(", ", tips)}");
    Assert.NotEmpty(roots);
    Assert.NotEmpty(tips);

    if(missing.Count > 0)
    {
      _outputHelper.WriteLine("pruning!");
      var pruned = commitMap.PruneMissing();
      missing = commitMap.MissingNodes.Select(n => n.Id.ShortId).ToList()!;
      roots = commitMap.Roots.Select(n => n.Id.ShortId).ToList()!;
      tips = commitMap.Tips.Select(n => n.Id.ShortId).ToList()!;
      _outputHelper.WriteLine($"{missing.Count} Missing: {String.Join(", ", missing)}");
      _outputHelper.WriteLine($"{roots.Count} Roots: {String.Join(", ", roots)}");
      _outputHelper.WriteLine($"{tips.Count} Tips: {String.Join(", ", tips)}");
      Assert.Empty(missing);
      Assert.NotEmpty(roots);
      Assert.NotEmpty(tips);
    }
  }
}
