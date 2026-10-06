using System;
using System.Collections.Generic;

namespace CourseApp.Models
{
    /// <summary>学生。</summary>
    public class Student
    {
        public string Id { get; set; } = "";              // 学号
        public string Name { get; set; } = "";            // 姓名
        public string Gender { get; set; } = "";          // male / female
        public string Seat { get; set; } = "";            // "3,5" 第 3 行第 5 列
        public string Remark { get; set; } = "";          // 备注
        public string Photo { get; set; } = "";           // 照片路径
        public string BirthDate { get; set; } = "";       // 出生日期 yyyy-MM-dd
        public string ParentName { get; set; } = "";      // 家长姓名
        public string ParentPhone { get; set; } = "";     // 家长电话
        public string Address { get; set; } = "";         // 住址

        /// <summary>成绩记录（考试名 → 分数）</summary>
        public Dictionary<string, double> Scores { get; set; } = new();
    }
}