using System;
using System.IO;
using System.Threading.Tasks;

namespace PlanPeopleAvalonia.Services
{
    /// <summary>
    /// v3.0 数据迁移服务。
    /// 负责将 v2.0 (CourseApp) 的数据平滑迁移至 v3.0 (PlanPeople)。
    /// 严格遵循：只读老数据、只写新目录、迁移前备份。
    /// </summary>
    public static class DataMigrationService
    {
        /// <summary>
        /// 检查并执行 v2.0 到 v3.0 的数据迁移。
        /// </summary>
        /// <returns>如果执行了迁移，返回 true；如果无需迁移或失败，返回 false。</returns>
        public static bool TryMigrateFromV2()
        {
            // 如果新目录已经有配置文件，说明已经是 v3.0 用户，无需迁移
            if (File.Exists(AppPaths.ConfigJson))
                return false;

            string legacyDir = AppPaths.GetLegacyV2Dir();
            if (string.IsNullOrEmpty(legacyDir))
                return false;

            try
            {
                // 1. 先备份老数据（在原位置打压缩包，确保绝对安全）
                string backupZip = Path.Combine(legacyDir, $"CourseApp_backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
                System.IO.Compression.ZipFile.CreateFromDirectory(legacyDir, backupZip);

                // 2. 确保新目录存在
                AppPaths.EnsureAll();

                // 3. 复制核心 JSON 数据（使用白名单机制）
                string[] coreFiles = {
                    "courses.json", "sections.json", "teachers.json", "config.json",
                    "students.json", "duty.json", "exams.json", "seats.json",
                    "committees.json", "representatives.json", "scores.json",
                    "score_categories.json", "overlay.json", "schedules.json"
                };

                foreach (var file in coreFiles)
                {
                    string src = Path.Combine(legacyDir, file);
                    string dst = Path.Combine(AppPaths.BaseDir, file);
                    if (File.Exists(src))
                    {
                        File.Copy(src, dst, overwrite: true);
                    }
                }

                // 4. 复制照片文件夹
                string srcPhotos = Path.Combine(legacyDir, "photos");
                if (Directory.Exists(srcPhotos))
                {
                    CopyDirectory(srcPhotos, AppPaths.PhotosDir);
                }

                return true;
            }
            catch (Exception ex)
            {
                // 迁移失败时，记录日志（不弹窗打断启动）
                try { File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "migration_error.log"), $"{DateTime.Now}: {ex.Message}\n{ex.StackTrace}"); } catch { }
                return false;
            }
        }

        /// <summary>
        /// 递归复制目录。
        /// </summary>
        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);
            
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite: true);
            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destinationDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, destSubDir);
            }
        }
    }
}