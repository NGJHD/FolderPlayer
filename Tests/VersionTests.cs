using System;
using System.Collections.Generic;

namespace AudioPlayer
{
    internal static class VersionTests
    {
        private static int failures = 0;

        private static void Check(bool condition, string what)
        {
            if (!condition)
            {
                failures++;
                Console.WriteLine("FAIL: " + what);
            }
        }

        public static int Main()
        {
            // Parse
            Check(VersionUtil.Parse("v1.2.0") == new Version(1, 2, 0), "v prefix");
            Check(VersionUtil.Parse("1.2.0") == new Version(1, 2, 0), "plain");
            Check(VersionUtil.Parse(" 2.1.0 ") == new Version(2, 1, 0), "whitespace");
            Check(VersionUtil.Parse("2.1.0.0") == new Version(2, 1, 0), "four parts, fourth ignored");
            Check(VersionUtil.Parse("1.2") == new Version(1, 2, 0), "two parts");
            Check(VersionUtil.Parse("v1.2.0-beta") == null, "prerelease suffix is unparseable");
            Check(VersionUtil.Parse("release-3") == null, "non numeric");
            Check(VersionUtil.Parse("") == null, "empty");
            Check(VersionUtil.Parse(null) == null, "null");
            Check(VersionUtil.Parse("1.2.3.4.5") == null, "too many parts");
            Check(VersionUtil.Parse("-1.0.0") == null, "negative");

            // Compare - the string comparison trap
            Check(VersionUtil.Compare(new Version(1, 10, 0), new Version(1, 9, 0)) > 0, "1.10.0 > 1.9.0");
            Check(VersionUtil.Compare(new Version(2, 1, 0), new Version(2, 1, 0)) == 0, "equal");
            Check(VersionUtil.Compare(new Version(2, 0, 1), new Version(2, 1, 0)) < 0, "minor beats build");
            Check(VersionUtil.Compare(new Version(1, 0), new Version(1, 0, 0)) == 0, "missing build reads as 0");

            // IsNewer
            Check(VersionUtil.IsNewer(new Version(2, 2, 0), new Version(2, 1, 0)), "newer");
            Check(!VersionUtil.IsNewer(new Version(2, 1, 0), new Version(2, 1, 0)), "same is not newer");
            Check(!VersionUtil.IsNewer(new Version(2, 0, 0), new Version(2, 1, 0)), "older is not newer");
            Check(!VersionUtil.IsNewer(null, new Version(2, 1, 0)), "unparseable tag is never newer");

            // PickReleaseAsset
            GitHubAsset zip = Asset("FolderPlayer-v2.2.0.zip");
            GitHubAsset other = Asset("FolderPlayer-v2.2.0.zip.sha256");
            GitHubAsset stray = Asset("symbols.zip");

            Check(VersionUtil.PickReleaseAsset(new List<GitHubAsset> { zip, other }) == zip, "the one zip");
            Check(VersionUtil.PickReleaseAsset(new List<GitHubAsset> { other }) == null, "no zip");
            Check(VersionUtil.PickReleaseAsset(new List<GitHubAsset>()) == null, "no assets");
            Check(VersionUtil.PickReleaseAsset(null) == null, "null assets");
            Check(VersionUtil.PickReleaseAsset(new List<GitHubAsset> { stray, zip }) == zip, "repo named zip wins");

            GitHubAsset noUrl = new GitHubAsset { Name = "x.zip", BrowserDownloadUrl = null };
            Check(VersionUtil.PickReleaseAsset(new List<GitHubAsset> { noUrl }) == null, "asset without a url");

            // FormatBytes
            Check(VersionUtil.FormatBytes(512) == "512 B", "bytes");
            Check(VersionUtil.FormatBytes(1605102).StartsWith("1.5"), "megabytes: " + VersionUtil.FormatBytes(1605102));

            Console.WriteLine(failures == 0 ? "all version tests passed" : failures + " failed");
            return failures;
        }

        private static GitHubAsset Asset(string name)
        {
            return new GitHubAsset { Name = name, Size = 1, BrowserDownloadUrl = "https://example.invalid/" + name };
        }
    }
}
