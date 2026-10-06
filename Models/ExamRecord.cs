using System;
using System.Collections.Generic;

namespace CourseApp.Models
{
    /// <summary>考试记录。</summary>
    public class ExamRecord
    {
        public string Name { get; set; } = "";            // 考试名（期中 / 期末 / 一模）
        public string Date { get; set; } = "";            // 考试日期 yyyy-MM-dd
        public string Subject { get; set; } = "";         // 科目
        public double FullScore { get; set; } = 100;      // 满分
        public double PassScore { get; set; } = 60;       // 及格线
        public Dictionary<string, double> Scores { get; set; } = new();  // 学号 → 分数
    }
}