using System;
using System.Linq;

using Xunit;
using Xunit.Abstractions;

using LcGitLib2.GitRunning;
using LcGitBup.BundleModel;
using System.IO;

namespace UnitTest.LcGitLib2;

public class LcGitBupTests
{
  private readonly ITestOutputHelper _outputHelper;

  /*
   * Reminder on getting test output visible when testing from the console:
   *   dotnet test --logger:"console;verbosity=detailed"
   * (https://stackoverflow.com/a/61182227/271323)
   */

  public LcGitBupTests(ITestOutputHelper outputHelper)
  {
    _outputHelper = outputHelper;
  }

  [Fact]
  public void CanParseBundleFileNames()
  {
    var b = GitBupBundle.FromBundleName("");
    Assert.Null(b);

    b = GitBupBundle.FromBundleName(@"foobar.20230719-120000.-.t0.bundle");
    Assert.NotNull(b);
    Assert.Equal("foobar", b.Prefix);
    Assert.Equal("20230719-120000", b.Id);
    Assert.Null(b.RefId);
    Assert.Equal(0, b.Tier);
    Assert.Equal(Environment.CurrentDirectory, b.Folder);

    b = GitBupBundle.FromBundleName(@"baz\foobar.20230719-120000.-.t0.bundle");
    Assert.NotNull(b);
    Assert.Equal("foobar", b.Prefix);
    Assert.Equal("20230719-120000", b.Id);
    Assert.Null(b.RefId);
    Assert.Equal(0, b.Tier);
    Assert.Equal(Path.Combine(Environment.CurrentDirectory, "baz"), b.Folder);

    b = GitBupBundle.FromBundleName(@"foobar.20230719-120000.-.t1.bundle");
    Assert.Null(b);

    b = GitBupBundle.FromBundleName(@"foobar.20230719-120100.20230719-120000.t0.bundle");
    Assert.Null(b);

    b = GitBupBundle.FromBundleName(@"foobar.20230719-120100.20230719-120000.t1.bundle");
    Assert.NotNull(b);
    Assert.Equal("foobar", b.Prefix);
    Assert.Equal("20230719-120100", b.Id);
    Assert.Equal("20230719-120000", b.RefId);
    Assert.Equal(1, b.Tier);
    Assert.Equal(Environment.CurrentDirectory, b.Folder);
  }

  [Fact]
  public void CanCreateNewBundles()
  {
    var stamp0 = new DateTime(2023, 07, 19, 1, 2, 3, DateTimeKind.Utc);

    var b0 = GitBupBundle.NewTier0Bundle(@"C:\bundles", "test.prefix", stamp0);
    Assert.NotNull(b0);
    Assert.Equal(@"test.prefix.20230719-010203.-.t0.bundle", b0.BundleFileName);

    var stamp1 = stamp0.AddMinutes(42);
    var b1 = b0.DeriveBundle(stamp1);
    Assert.NotNull(b1);
    Assert.Equal(@"test.prefix.20230719-014403.20230719-010203.t1.bundle", b1.BundleFileName);
  }


}
