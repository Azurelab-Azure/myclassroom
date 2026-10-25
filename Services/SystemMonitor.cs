using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PlanPeopleAvalonia.Services
{
    /// <summary>
    /// 系统性能监控服务（精确获取 CPU 和 内存）。
    /// </summary>
    public static class SystemMonitor
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemTimes(out long lpIdleTime, out long lpKernelTime, out long lpUserTime);

        private static long _prevIdle = 0;
        private static long _prevKernel = 0;
        private static long _prevUser = 0;

        /// <summary>
        /// 获取精确的系统内存使用率（0-100）。
        /// </summary>
        public static int GetMemoryUsage()
        {
            var memStatus = new MEMORYSTATUSEX();
            memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            
            if (GlobalMemoryStatusEx(ref memStatus))
            {
                return (int)memStatus.dwMemoryLoad;
            }
            return 0;
        }

        /// <summary>
        /// 获取精确的 CPU 使用率（0-100）。
        /// </summary>
        public static int GetCpuUsage()
        {
            long idle, kernel, user;
            if (!GetSystemTimes(out idle, out kernel, out user)) return 0;

            // 第一次调用只记录数值
            if (_prevIdle == 0)
            {
                _prevIdle = idle;
                _prevKernel = kernel;
                _prevUser = user;
                return 0;
            }

            long cpuIdle = idle - _prevIdle;
            long cpuKernel = kernel - _prevKernel;
            long cpuUser = user - _prevUser;

            long total = cpuKernel + cpuUser;
            
            if (total > 0)
            {
                int usage = (int)(100 - (cpuIdle * 100 / total));
                _prevIdle = idle;
                _prevKernel = kernel;
                _prevUser = user;
                return Math.Max(0, Math.Min(100, usage));
            }
            return 0;
        }
    }
}