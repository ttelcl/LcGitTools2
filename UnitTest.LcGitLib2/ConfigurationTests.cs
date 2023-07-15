using System;

using LcGitLib2.GitRunning;

using Xunit;
using Xunit.Abstractions;

namespace UnitTest.LcGitLib2;

public class ConfigurationTests
{
  private readonly ITestOutputHelper _outputHelper;

  /*
   * Reminder on getting test output visible when testing from the console:
   *   dotnet test --logger:"console;verbosity=detailed"
   * (https://stackoverflow.com/a/61182227/271323)
   */

  public ConfigurationTests(ITestOutputHelper outputHelper)
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
}
