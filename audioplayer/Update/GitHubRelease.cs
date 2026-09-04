using System.Runtime.Serialization;

namespace AudioPlayer
{
    //The slice of https://api.github.com/repos/<owner>/<repo>/releases/latest that this app
    //reads. DataContractJsonSerializer ignores every member not listed here, which is most
    //of the response, so there is no third-party JSON dependency to carry.

    [DataContract]
    internal sealed class GitHubAsset
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "size")]
        public long Size { get; set; }

        [DataMember(Name = "browser_download_url")]
        public string BrowserDownloadUrl { get; set; }
    }

    [DataContract]
    internal sealed class GitHubRelease
    {
        [DataMember(Name = "tag_name")]
        public string TagName { get; set; }

        [DataMember(Name = "html_url")]
        public string HtmlUrl { get; set; }

        [DataMember(Name = "assets")]
        public GitHubAsset[] Assets { get; set; }
    }
}
