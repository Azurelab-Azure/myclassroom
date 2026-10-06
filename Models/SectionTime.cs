namespace CourseApp.Models
{
    /// <summary>
    /// 节次 / 特殊时段。
    ///   Type = "normal"  ：普通节次（有 Section 编号）
    ///   Type = "special" ：特殊时段（早自习 / 午休 / 晚自习 / 眼保健操），Section = 0
    /// </summary>
    public class SectionTime
    {
        /// <summary>类型："normal" 或 "special"</summary>
        public string Type { get; set; } = "normal";

        /// <summary>普通节次编号（1 ~ N）；特殊时段为 0</summary>
        public int Section { get; set; } = 0;

        /// <summary>特殊时段名称（如"早自习"）；普通节次为空</summary>
        public string Name { get; set; } = "";

        /// <summary>开始时间（"HH:mm"，如 "08:00"）</summary>
        public string StartTime { get; set; } = "";

        /// <summary>结束时间（"HH:mm"，如 "08:45"）</summary>
        public string EndTime { get; set; } = "";
    }
}