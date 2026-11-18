using System;
using System.IO;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace MicMute
{
    internal static class GitHubUpdates
    {
        private const string Latest = "https://api.github.com/repos/kalderbabasina/MicMute/releases/latest";
        private const string Releases = "https://github.com/kalderbabasina/MicMute/releases/";
        private const string SetupName = "MicMute-Setup.exe";
        private const int MaxInstallerBytes = 16 * 1024 * 1024;
        private const int MaxMetadataBytes = 1024 * 1024;
        private const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\{A944DB73-41EF-4860-A813-7EA43D27A59D}_is1";

        internal sealed class Release
        {
            internal Version Version;
            internal string Page, InstallerUrl, Digest;
            internal int Size;
        }

        [DataContract]
        private sealed class ReleaseData
        {
            [DataMember(Name = "tag_name")] public string Tag { get; set; }
            [DataMember(Name = "draft")] public bool Draft { get; set; }
            [DataMember(Name = "prerelease")] public bool Prerelease { get; set; }
            [DataMember(Name = "assets")] public AssetData[] Assets { get; set; }
        }

        [DataContract]
        private sealed class AssetData
        {
            [DataMember(Name = "name")] public string Name { get; set; }
            [DataMember(Name = "state")] public string State { get; set; }
            [DataMember(Name = "size")] public int Size { get; set; }
            [DataMember(Name = "digest")] public string Digest { get; set; }
            [DataMember(Name = "browser_download_url")] public string Url { get; set; }
        }

        private static HttpClient CreateClient(int limit)
        {
            var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false })
            {
                Timeout = TimeSpan.FromSeconds(20),
                MaxResponseContentBufferSize = limit
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MicMute/" + Program.Version);
            return client;
        }

        internal static async Task<Release> CheckAsync(Version current, CancellationToken cancellation)
        {
            using (var client = CreateClient(MaxMetadataBytes))
            {
                client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
                client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2026-03-10");
                using (var response = await client.GetAsync(Latest, cancellation).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    return Parse(await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false), current);
                }
            }
        }

        internal static Release Parse(byte[] body, Version current)
        {
            if (body.Length > MaxMetadataBytes) throw new InvalidDataException("GitHub release metadata is too large.");
            ReleaseData data;
            using (var stream = new MemoryStream(body, false))
                data = (ReleaseData)new DataContractJsonSerializer(typeof(ReleaseData)).ReadObject(stream);
            if (data == null || data.Draft || data.Prerelease) return null;
            Version version;
            if (String.IsNullOrEmpty(data.Tag) || data.Tag.Length > 32 ||
                !data.Tag.StartsWith("v", StringComparison.Ordinal) || !Version.TryParse(data.Tag.Substring(1), out version) ||
                version.Build < 0 || version.Revision >= 0 || data.Tag != "v" + version || version <= current)
                return null;
            var release = new Release { Version = version, Page = Releases + "tag/" + data.Tag };
            string officialInstaller = Releases + "download/" + data.Tag + "/" + SetupName;
            foreach (var asset in data.Assets ?? new AssetData[0])
            {
                if (asset == null || asset.Name != SetupName || asset.State != "uploaded" || asset.Url != officialInstaller ||
                    asset.Size <= 0 || asset.Size > MaxInstallerBytes || !IsDigest(asset.Digest)) continue;
                release.InstallerUrl = officialInstaller;
                release.Digest = asset.Digest;
                release.Size = asset.Size;
                break;
            }
            return release;
        }

        private static bool IsDigest(string digest)
        { return digest != null && Regex.IsMatch(digest, @"\Asha256:[a-fA-F0-9]{64}\z"); }

        internal static bool MatchesDigest(byte[] bytes, string expected, int size)
        {
            if (bytes.Length != size || !IsDigest(expected)) return false;
            using (var sha = SHA256.Create())
                return String.Equals("sha256:" + BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", ""),
                    expected, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsDownloadRedirect(Uri target)
        {
            return target != null && target.IsAbsoluteUri && target.Scheme == Uri.UriSchemeHttps && target.IsDefaultPort &&
                target.UserInfo.Length == 0 && target.Host == "release-assets.githubusercontent.com";
        }

        internal static async Task<byte[]> DownloadAsync(Release release, CancellationToken cancellation)
        {
            using (var client = CreateClient(release.Size))
            {
                Uri uri = new Uri(release.InstallerUrl);
                for (int hop = 0; hop < 3; hop++)
                {
                    using (var response = await client.GetAsync(uri, cancellation).ConfigureAwait(false))
                    {
                        int status = (int)response.StatusCode;
                        if (status == 301 || status == 302 || status == 303 || status == 307 || status == 308)
                        {
                            uri = response.Headers.Location;
                            if (!IsDownloadRedirect(uri)) throw new InvalidDataException("Unexpected GitHub download host.");
                            continue;
                        }
                        response.EnsureSuccessStatusCode();
                        byte[] bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                        if (!MatchesDigest(bytes, release.Digest, release.Size))
                            throw new InvalidDataException("GitHub installer SHA-256 did not match.");
                        return bytes;
                    }
                }
            }
            throw new InvalidDataException("Too many GitHub download redirects.");
        }

        internal static bool IsInstalledCopy(string executable)
        {
            foreach (RegistryView view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
            {
                using (RegistryKey root = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view))
                using (RegistryKey key = root.OpenSubKey(UninstallKey))
                {
                    string folder = key == null ? null : key.GetValue("InstallLocation") as string;
                    if (!String.IsNullOrEmpty(folder) && String.Equals(Path.Combine(folder, "MicMute2.exe"),
                        executable, StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            return false;
        }

        [ComImport, Guid("73DB1241-1E85-4581-8E4F-A81E1D0F8C57"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAttachmentExecute
        {
            void SetClientTitle([MarshalAs(UnmanagedType.LPWStr)] string title);
            void SetClientGuid(ref Guid client);
            void SetLocalPath([MarshalAs(UnmanagedType.LPWStr)] string localPath);
            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string fileName);
            void SetSource([MarshalAs(UnmanagedType.LPWStr)] string source);
            void SetReferrer([MarshalAs(UnmanagedType.LPWStr)] string referrer);
            void CheckPolicy();
            void Prompt(IntPtr owner, int prompt, out int action);
            void Save();
        }

        internal static void LaunchInstaller(Release release, byte[] bytes)
        {
            if (!MatchesDigest(bytes, release.Digest, release.Size))
                throw new InvalidDataException("GitHub installer SHA-256 did not match.");
            string folder = Path.Combine(Path.GetTempPath(), "MicMute-update-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, SetupName);
            bool launched = false;
            try
            {
                using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    output.Write(bytes, 0, bytes.Length);
                // Preserve Windows attachment/antivirus policy before ShellExecute. Never unblock the file.
                ApplyAttachmentPolicy(path, release.InstallerUrl);
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (file.Length != bytes.Length) throw new InvalidDataException("Installer changed after download.");
                    using (var reader = new BinaryReader(file))
                        if (!MatchesDigest(reader.ReadBytes(release.Size), release.Digest, release.Size))
                            throw new InvalidDataException("Installer changed after download.");
                    using (Process.Start(new ProcessStartInfo(path) { UseShellExecute = true, WorkingDirectory = folder }))
                        launched = true;
                }
            }
            finally
            {
                if (!launched)
                {
                    try { File.Delete(path); Directory.Delete(folder); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
        }

        internal static void ApplyAttachmentPolicy(string path, string sourceUrl)
        {
            var attachment = (IAttachmentExecute)Activator.CreateInstance(
                Type.GetTypeFromCLSID(new Guid("4125DD96-E03A-4103-8F70-E0597D803B9C"), true));
            try
            {
                attachment.SetLocalPath(path);
                attachment.SetSource(sourceUrl);
                attachment.CheckPolicy();
                attachment.Save();
            }
            finally { Marshal.ReleaseComObject(attachment); }
        }
    }
}
