using System.Collections.Generic;

namespace PlanPeopleAvalonia.Models;

public class Student
{
    public string Id { get; set; } = ""; // 学号
    public string Name { get; set; } = "";
    public string Gender { get; set; } = "";
    public string Seat { get; set; } = "";
    public string Duty { get; set; } = ""; // 职务
    public string ParentPhone { get; set; } = ""; // 家长电话
    public string BirthDate { get; set; } = "";
    public string Level { get; set; } = ""; // 等级
    public string HealthStatus { get; set; } = ""; // 健康状态
    public string Address { get; set; } = "";
    public string Remark { get; set; } = "";
    public string Badge { get; set; } = ""; // 徽章
    public string Info { get; set; } = ""; // 个人简介
    public string PhotoPath { get; set; } = ""; // 头像路径
    public int TotalPoints { get; set; } = 0; // 积分
    public string Photo { get; set; } = "";     // ★ 兼容旧代码

    public Dictionary<string, double> Scores { get; set; } = new();
}