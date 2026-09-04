using System;
using System.Collections.Generic;
using System.Globalization;

namespace AudioPlayer
{
    /// <summary>
    /// Version arithmetic and asset picking. Pure - no network, no file system - so it can be
    /// reasoned about, and exercised, without a release to point it at.
    /// </summary>
    internal static class VersionUtil
    {
        /// <summary>
        /// "v1.2.0", "1.2", "1.2.0.0" -> a three part Version. Returns null for anything that
        /// cannot be read as a version; callers must treat that as "not newer" rather than
        /// guessing, so an unparseable tag can never trigger an update.
        /// </summary>
        public static Version Parse(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            string trimmed = text.Trim();

            if (trimmed.Length > 0 && (trimmed[0] == 'v' || trimmed[0] == 'V'))
            {
                trimmed = trimmed.Substring(1);
            }

            string[] parts = trimmed.Split('.');

            if (parts.Length < 1 || parts.Length > 4)
            {
                return null;
            }

            int[] numbers = new int[3];

            for (int i = 0; i < parts.Length; i++)
            {
                int value;

                //Deliberately strict: no leading '+', no whitespace, no "1.2.0-beta". A tag this
                //does not understand is a tag this must not act on.
                if (!int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out value))
                {
                    return null;
                }

                //A fourth part is accepted (AssemblyVersion carries one) but ignored, because
                //release tags are three part.
                if (i < 3)
                {
                    numbers[i] = value;
                }
            }

            return new Version(numbers[0], numbers[1], numbers[2]);
        }

        /// <summary>
        /// Numeric comparison over major.minor.build. "1.10.0" beats "1.9.0", which a string
        /// comparison would get backwards.
        /// </summary>
        public static int Compare(Version left, Version right)
        {
            if (left.Major != right.Major)
            {
                return left.Major.CompareTo(right.Major);
            }

            if (left.Minor != right.Minor)
            {
                return left.Minor.CompareTo(right.Minor);
            }

            //Version leaves Build at -1 when it was never set; treat that as 0.
            int leftBuild = left.Build < 0 ? 0 : left.Build;
            int rightBuild = right.Build < 0 ? 0 : right.Build;

            return leftBuild.CompareTo(rightBuild);
        }

        public static bool IsNewer(Version candidate, Version current)
        {
            if (candidate == null || current == null)
            {
                return false;
            }

            return Compare(candidate, current) > 0;
        }

        /// <summary>
        /// The one downloadable file on the release. A release carrying no match, or several
        /// equally plausible ones, returns null rather than a guess.
        /// </summary>
        public static GitHubAsset PickReleaseAsset(IEnumerable<GitHubAsset> assets)
        {
            if (assets == null)
            {
                return null;
            }

            List<GitHubAsset> candidates = new List<GitHubAsset>();

            foreach (GitHubAsset asset in assets)
            {
                if (asset == null || string.IsNullOrEmpty(asset.Name) || string.IsNullOrEmpty(asset.BrowserDownloadUrl))
                {
                    continue;
                }

                if (asset.Name.EndsWith(AboutInfo.AssetSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Add(asset);
                }
            }

            if (candidates.Count == 1)
            {
                return candidates[0];
            }

            if (candidates.Count > 1)
            {
                //More than one zip: fall back to the one named after the repository, which is
                //what the release recipe in the README produces.
                string repoName = AboutInfo.Repo.Substring(AboutInfo.Repo.IndexOf('/') + 1);

                foreach (GitHubAsset asset in candidates)
                {
                    if (asset.Name.StartsWith(repoName, StringComparison.OrdinalIgnoreCase))
                    {
                        return asset;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// "1.6 MB". Shown next to a download so the user can tell a stalled transfer from a
        /// slow one.
        /// </summary>
        public static string FormatBytes(long bytes)
        {
            if (bytes < 1024)
            {
                return bytes + " B";
            }

            double kilobytes = bytes / 1024.0;

            if (kilobytes < 1024)
            {
                return kilobytes.ToString("0.0", CultureInfo.CurrentCulture) + " KB";
            }

            double megabytes = kilobytes / 1024.0;

            if (megabytes < 1024)
            {
                return megabytes.ToString("0.0", CultureInfo.CurrentCulture) + " MB";
            }

            return (megabytes / 1024.0).ToString("0.00", CultureInfo.CurrentCulture) + " GB";
        }
    }
}
