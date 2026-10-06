using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CourseApp.Services
{
    /// <summary>
    /// 检测"全屏放映"（PPT / WPS 演示 / 永中演示 / 希沃白板）。
    /// 可结束这些进程。
    /// </summary>
    public static class PresentationDetector
    {
        // =====================================================
        // Win32
        // =====================================================
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // =====================================================
        // 目标进程名单（小写，不含 .exe）
        // =====================================================
        private static readonly string[] PresentationProcesses =
        {
            "powerpnt",         // Microsoft PowerPoint
            "wpp",              // WPS 演示
            "wps",              // WPS 文字
            "et",               // WPS 表格
            "yozo",             // 永中 Office
            "yozopresentation", // 永中演示
            "yozowriter",       // 永中文字
            "seewo",            // 希沃白板
            "easinote",         // 希沃白板 5
            "easinote5",        // 希沃白板 5
            "enshare",          // 希沃传屏
            "classin",          // ClassIn
        };

        // =====================================================
        // 公共方法
        // =====================================================
        /// <summary>当前是否全屏放映。</summary>
        public static bool IsPresenting()
        {
            return !string.IsNullOrEmpty(GetPresentingProcessName());
        }

        /// <summary>
        /// 返回当前前台放映进程名（如 "powerpnt"），没有则返回 ""。
        /// </summary>
        public static string GetPresentingProcessName()
        {
            try
            {
                var hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return "";

                if (!GetWindowRect(hwnd, out var rect)) return "";

                int w = rect.Right - rect.Left;
                int h = rect.Bottom - rect.Top;

                var screen = Screen.FromHandle(hwnd);
                if (screen == null) return "";

                var b = screen.Bounds;

                // 全屏判断
                bool isFullscreen = w >= b.Width - 4 && h >= b.Height - 4;
                if (!isFullscreen) return "";

                GetWindowThreadProcessId(hwnd, out uint pid);
                if (pid == 0) return "";

                string procName = GetProcessName(pid);
                if (string.IsNullOrEmpty(procName)) return "";

                return IsPresentationProcess(procName) ? procName : "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>结束当前放映进程。</summary>
        public static bool KillPresentingProcess()
        {
            try
            {
                var name = GetPresentingProcessName();
                if (string.IsNullOrEmpty(name)) return false;

                var procs = Process.GetProcessesByName(name);
                bool killed = false;
                foreach (var p in procs)
                {
                    try
                    {
                        p.Kill();
                        p.WaitForExit(2000);
                        killed = true;
                    }
                    catch { }
                }
                return killed;
            }
            catch
            {
                return false;
            }
        }

        // =====================================================
        // 内部
        // =====================================================
        private static string GetProcessName(uint pid)
        {
            try
            {
                var proc = Process.GetProcessById((int)pid);
                return proc.ProcessName.ToLowerInvariant();
            }
            catch
            {
                return "";
            }
        }

        private static bool IsPresentationProcess(string procName)
        {
            if (string.IsNullOrEmpty(procName)) return false;
            foreach (var p in PresentationProcesses)
            {
                if (procName == p || procName.StartsWith(p))
                    return true;
            }
            return false;
        }
    }
}