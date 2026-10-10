using System.Collections.Generic;

namespace CourseApp.Models
{
    /// <summary>
    /// 学生。
    /// 包含：基本信息、学籍信息、健康状况、综合评价、家长信息、积分徽章。
    /// </summary>
    public class Student
    {
        // =====================================================
        // 基本信息
        // =====================================================
        /// <summary>学号</summary>
        public string Id { get; set; } = "";

        /// <summary>姓名</summary>
        public string Name { get; set; } = "";

        /// <summary>性别：male / female</summary>
        public string Gender { get; set; } = "";

        /// <summary>座位："3,5" 表示第 3 行第 5 列</summary>
        public string Seat { get; set; } = "";

        /// <summary>备注</summary>
        public string Remark { get; set; } = "";

        /// <summary>照片路径</summary>
        public string Photo { get; set; } = "";

        /// <summary>出生日期 yyyy-MM-dd</summary>
        public string BirthDate { get; set; } = "";

        // =====================================================
        // 等级 / 职务
        // =====================================================
        /// <summary>等级：优 / 良 / 中 / 待提高</summary>
        public string Level { get; set; } = "";

        /// <summary>等级分数 0~100</summary>
        public int LevelScore { get; set; } = 0;

        /// <summary>职务：班长 / 学习委员 / 课代表 / 组长 / 无</summary>
        public string Duty { get; set; } = "";

        // =====================================================
        // 综合评价
        // =====================================================
        /// <summary>教师评语</summary>
        public string OverallComment { get; set; } = "";

        /// <summary>综合评分 0~100</summary>
        public int OverallScore { get; set; } = 0;

        // =====================================================
        // 健康状况
        // =====================================================
        /// <summary>健康状况：健康 / 近视 / 过敏 / 哮喘 / 其他</summary>
        public string HealthStatus { get; set; } = "";

        /// <summary>健康详细说明</summary>
        public string HealthNote { get; set; } = "";

        // =====================================================
        // 学籍档案
        // =====================================================
        /// <summary>档案号</summary>
        public string ArchiveNo { get; set; } = "";

        /// <summary>入学日期</summary>
        public string EnrollmentDate { get; set; } = "";

        /// <summary>政治面貌</summary>
        public string PoliticalStatus { get; set; } = "";

        /// <summary>籍贯</summary>
        public string Hometown { get; set; } = "";

        /// <summary>民族</summary>
        public string Ethnicity { get; set; } = "";

        // =====================================================
        // 家长信息
        // =====================================================
        /// <summary>家长姓名</summary>
        public string ParentName { get; set; } = "";

        /// <summary>家长电话</summary>
        public string ParentPhone { get; set; } = "";

        /// <summary>住址</summary>
        public string Address { get; set; } = "";

        // =====================================================
        // 积分 / 徽章
        // =====================================================
        /// <summary>累计积分</summary>
        public int TotalPoints { get; set; } = 0;

        /// <summary>徽章名称（空表示无徽章）</summary>
        public string Badge { get; set; } = "";

        // =====================================================
        // 成绩记录
        // =====================================================
        /// <summary>成绩记录（考试名 → 分数）</summary>
        public Dictionary<string, double> Scores { get; set; } = new();
    }
}