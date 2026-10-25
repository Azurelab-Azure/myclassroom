using System;

namespace PlanPeopleAvalonia.Services
{
    /// <summary>
    /// 检测"全屏放映"（跨平台兼容占位版）。
    /// </summary>
    public static class PresentationDetector
    {
        /// <summary>当前是否全屏放映。</summary>
        public static bool IsPresenting() => false;

        /// <summary>返回当前前台放映进程名，没有则返回空。</summary>
        public static string GetPresentingProcessName() => "";

        /// <summary>结束当前放映进程。</summary>
        public static bool KillPresentingProcess() => false;
    }
}