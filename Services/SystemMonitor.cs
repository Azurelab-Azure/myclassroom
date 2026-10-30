using System;
using System.Diagnostics;

namespace PlanPeopleAvalonia.Services
{
    /// <summary>
    /// 系统性能监控（跨平台安全降级版，移除了 kernel32 独占强绑定）。
    /// </summary>
    public static class SystemMonitor
    {
        /// <summary>获取当前进程使用的内存（MB）。全平台通用。</summary>
        public static double GetMemoryUsageMB()
        {
            try
            {
                using var proc = Process.GetCurrentProcess();
                return Math.Round(proc.WorkingSet64 / 1024.0 / 1024.0, 1);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 获取 CPU 使用率。由于跨平台差异，这里采用时间差估算，或者直接返回 0 避免崩溃。
        /// </summary>
        public static int GetCpuUsage()
        {
            // 如果你不在 UI 上显示全局 CPU 占用，这里直接返回 0 即可。
            // 如果要显示，建议使用 System.Diagnostics 库跨平台实现。
            return 0;
        }
    }
}