using System;
using System.Collections.Generic;

namespace PlanPeopleAvalonia.Models;

public class ExamRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = ""; // 考试名（如：期中考试）
    public string Date { get; set; } = ""; // 考试日期
    public string Subject { get; set; } = ""; // 科目（关联 Course.Name 或 Teacher.Subject）
    public double FullScore { get; set; } = 100; // 满分
    public double PassScore { get; set; } = 60; // 及格线

    // 学号 -> 分数
    public Dictionary<string, double> Scores { get; set; } = new();
}