using System;
using System.IO;
using System.IO.Compression;

namespace PlanPeopleAvalonia.Services
{
    /// <summary>
    /// 完整的备份与还原服务。
    /// 相比于 v2.0 简单的文件夹复制，v3.0 采用单文件 ZIP 打包，包含所有 JSON 和 photos/ 目录。
    /// 保证内存占用极小（流式压缩）。
    /// </summary>
    public static class BackupService
    {
        /// <summary>
        /// 将当前所有数据（JSON + 照片）打包导出为 ZIP。
        /// </summary>
        /// <param name="targetZipPath">导出的目标 ZIP 路径</param>
        /// <returns>是否成功</returns>
        public static bool CreateFullBackup(string targetZipPath)
        {
            try
            {
                // 确保源目录存在
                AppPaths.EnsureAll();

                // 如果目标文件已存在，先删除，否则 ZipFile.CreateFromDirectory 会报错
                if (File.Exists(targetZipPath))
                {
                    File.Delete(targetZipPath);
                }

                // 将 BaseDir 下的所有文件和子目录（包括 photos）打包
                ZipFile.CreateFromDirectory(AppPaths.BaseDir, targetZipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
                return true;
            }
            catch (Exception ex)
            {
                try { File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backup_error.log"), $"{DateTime.Now}: {ex.Message}\n"); } catch { }
                return false;
            }
        }

        /// <summary>
        /// 从指定 ZIP 恢复数据。
        /// 注意：为了安全，恢复前会先对当前已有数据做一次快速备份。
        /// </summary>
        /// <param name="sourceZipPath">源 ZIP 路径</param>
        /// <returns>是否成功</returns>
        public static bool RestoreFullBackup(string sourceZipPath)
        {
            if (!File.Exists(sourceZipPath)) return false;

            try
            {
                // 1. 恢复前自我备份（防手抖）
                string safeBackup = Path.Combine(AppPaths.BaseDir, $"PreRestore_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
                CreateFullBackup(safeBackup);

                // 2. 清空当前数据目录（但保留刚才生成的安全备份）
                var dirInfo = new DirectoryInfo(AppPaths.BaseDir);
                foreach (var file in dirInfo.GetFiles())
                {
                    if (file.Name == Path.GetFileName(safeBackup)) continue;
                    file.Delete();
                }
                foreach (var dir in dirInfo.GetDirectories())
                {
                    dir.Delete(true);
                }

                // 3. 解压新数据
                ZipFile.ExtractToDirectory(sourceZipPath, AppPaths.BaseDir, overwriteFiles: true);
                
                return true;
            }
            catch (Exception ex)
            {
                try { File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "restore_error.log"), $"{DateTime.Now}: {ex.Message}\n"); } catch { }
                return false;
            }
        }
    }
}