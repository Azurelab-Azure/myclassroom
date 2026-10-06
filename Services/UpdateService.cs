using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace CourseApp.Services
{
    /// <summary>GitHub Release 信息。</summary>
    public class ReleaseInfo
    {
        public string TagName { get; set; } = "";           // v1.0.1
        public string Name { get; set; } = "";              // 我的课表 v1.0.1
        public string Body { get; set; } = "";              // 更新说明
        public string HtmlUrl { get; set; } = "";           // GitHub 页面
        public string PublishedAt { get; set; } = "";       // 2026-10-07
        public List<ReleaseAsset> Assets { get; set; } = new();
    }

    /// <summary>Release 附件。</summary>
    public class ReleaseAsset
    {
        public string Name { get; set; } = "";
        public string BrowserDownloadUrl { get; set; } = "";
        public long Size { get; set; }
    }

    /// <summary>
    /// 软件更新服务：从 GitHub Releases 检查新版本。
    /// </summary>
    public static class UpdateService
    {
        // ★★★ 改成你自己的仓库 ★★★
        private const string Owner = "Azurelab-Azure";
        private const string Repo = "myclassroom";

        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(15),
        };

        static UpdateService()
        {
            // GitHub API 必须带 User-Agent
            _http.DefaultRequestHeaders.Add("User-Agent", "CourseApp-UpdateCheck");
            _http.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
        }

        /// <summary>
        /// 获取最新 Release 信息。失败返回 null。
        /// </summary>
        public static async Task<ReleaseInfo?> GetLatestReleaseAsync()
        {
            try
            {
                var url = $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest";
                var json = await _http.GetStringAsync(url);

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var info = new ReleaseInfo
                {
                    TagName = root.TryGetProperty("tag_name", out var t) ? t.GetString() ?? "" : "",
                    Name = root.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                    Body = root.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "",
                    HtmlUrl = root.TryGetProperty("html_url", out var h) ? h.GetString() ?? "" : "",
                    PublishedAt = root.TryGetProperty("published_at", out var p) ? p.GetString() ?? "" : "",
                };

                if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
                {
                    foreach (var a in assets.EnumerateArray())
                    {
                        info.Assets.Add(new ReleaseAsset
                        {
                            Name = a.TryGetProperty("name", out var an) ? an.GetString() ?? "" : "",
                            BrowserDownloadUrl = a.TryGetProperty("browser_download_url", out var ad) ? ad.GetString() ?? "" : "",
                            Size = a.TryGetProperty("size", out var asz) ? asz.GetInt64() : 0,
                        });
                    }
                }

                return info;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 检查是否有新版本。
        /// </summary>
        public static async Task<ReleaseInfo?> CheckForUpdateAsync()
        {
            var latest = await GetLatestReleaseAsync();
            if (latest == null) return null;

            if (IsNewerVersion(latest.TagName, AppInfo.Version))
                return latest;

            return null;
        }

        /// <summary>
        /// 下载更新包到临时目录。
        /// </summary>
        public static async Task<string?> DownloadUpdateAsync(ReleaseInfo release, Action<long, long>? progress = null)
        {
            // 优先找 Setup.exe
            var asset = release.Assets.Find(a => a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                     ?? release.Assets.Find(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

            if (asset == null) return null;

            try
            {
                var tempDir = Path.Combine(Path.GetTempPath(), "CourseApp-Update");
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

                var destPath = Path.Combine(tempDir, asset.Name);

                using var response = await _http.GetAsync(asset.BrowserDownloadUrl,
                    HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? asset.Size;
                long downloadedBytes = 0;

                using (var fs = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    var buffer = new byte[8192];
                    int read;
                    while ((read = await stream.ReadAsync(buffer)) > 0)
                    {
                        await fs.WriteAsync(buffer.AsMemory(0, read));
                        downloadedBytes += read;
                        progress?.Invoke(downloadedBytes, totalBytes);
                    }
                }

                return destPath;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 运行安装包并退出当前进程。
        /// </summary>
        public static void RunInstallerAndExit(string installerPath)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = installerPath,
                    UseShellExecute = true,
                });

                System.Windows.Forms.Application.Exit();
            }
            catch { }
        }

        // =====================================================
        // 版本比较
        // =====================================================
        private static bool IsNewerVersion(string remoteVersion, string localVersion)
        {
            try
            {
                var r = ParseVersion(remoteVersion);
                var l = ParseVersion(localVersion);
                return r > l;
            }
            catch
            {
                return false;
            }
        }

        private static Version ParseVersion(string v)
        {
            if (string.IsNullOrEmpty(v)) return new Version(0, 0);

            // 去掉 v 前缀
            if (v.StartsWith("v") || v.StartsWith("V")) v = v.Substring(1);

            // 去掉 -beta / -rc 等后缀
            int dash = v.IndexOf('-');
            if (dash > 0) v = v.Substring(0, dash);

            // 补齐 3 段
            var parts = v.Split('.');
            if (parts.Length == 1) v = v + ".0.0";
            else if (parts.Length == 2) v = v + ".0";

            return Version.TryParse(v, out var ver) ? ver : new Version(0, 0);
        }
    }
}