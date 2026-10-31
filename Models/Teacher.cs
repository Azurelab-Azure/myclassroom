namespace PlanPeopleAvalonia.Models;

public class Teacher
{
    // 基础信息
    public string Name { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Title { get; set; } = "";      // 职称（如：高级教师）
    public string EmployeeId { get; set; } = ""; // 工号
    public string Gender { get; set; } = "";     // 性别
    public string Phone { get; set; } = "";      // 手机
    public string Email { get; set; } = "";      // 邮箱
    public string Office { get; set; } = "";     // 办公室
    public string Education { get; set; } = "";  // 学历
    public string Remark { get; set; } = "";     // 备注
    public string Info { get; set; } = "";       // 个人简介
    public string PhotoPath { get; set; } = "";  // 头像
    public string Photo { get; set; } = "";      // 兼容旧版
    
    // 额外补充
    public string PoliticalStatus { get; set; } = ""; // 政治面貌
    public string Hometown { get; set; } = "";        // 籍贯
}