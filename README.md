# PlanPeople Avalonia

一款基于 Avalonia UI 开发的跨平台班级管理与课表安排桌面应用。

## 项目简介

PlanPeople Avalonia 是一款功能丰富的教育管理桌面应用程序，支持课程表管理、学生信息管理、教师信息管理、考试记录、成绩管理等功能。采用 MVVM 架构设计，支持多主题切换和多语言国际化。

## 主要功能

### 📚 课表管理
- 可视化课表编辑，支持拖拽调整课程位置
- 支持多个科目配置和自定义颜色
- 课程冲突检测
- 按周浏览课表

### 👨‍🎓 学生管理
- 学生信息录入（姓名、学号、座位、职务等）
- 座位布局配置
- 班级值日安排
- 导入/导出 CSV

### 👨‍🏫 教师管理
- 教师信息管理
- 授课课程关联
- 教师详情展示

### 📝 考试与成绩
- 考试记录管理
- 成绩录入与统计
- 学生积分管理
- 积分记录与分类

### ⚙️ 系统功能
- 多主题支持（浅色、深色、清新、透明）
- 多语言支持（默认简体中文）
- 自动更新检查
- 数据备份与恢复
- 灵动岛/悬浮窗模式
- 隐私政策管理

## 技术栈

- **框架**: Avalonia UI 11.x
- **语言**: C# (.NET 8)
- **架构**: MVVM
- **数据存储**: JSON 本地文件存储
- **图标**: SVG 图标资源

## 项目结构

```
PlanPeopleAvalonia/
├── Models/              # 数据模型
├── Services/            # 业务服务层
├── ViewModels/          # 视图模型
├── Views/               # 视图层
│   ├── ScheduleComponents/   # 课表组件
│   ├── StudentComponents/    # 学生组件
│   ├── TeacherComponents/    # 教师组件
│   ├── ExamComponents/       # 考试组件
│   └── SettingsComponents/   # 设置组件
├── Localization/        # 国际化资源
├── Styles/              # 样式资源
└── icons/               # 图标资源
```

## 运行要求

- .NET 8.0 SDK 或更高版本
- 支持 Windows、macOS、Linux 系统

## 构建与运行

```bash
# 克隆项目
git clone https://gitee.com/cyukk/plan-people-avalonia.git

# 进入项目目录
cd plan-people-avalonia

# 还原依赖
dotnet restore

# 运行项目
dotnet run

# 发布 release 版本
dotnet publish -c Release
```

## 数据存储

应用数据默认存储在用户数据目录下：
- Windows: `%APPDATA%\PlanPeopleAvalonia\`
- macOS: `~/Library/Application Support/PlanPeopleAvalonia/`
- Linux: `~/.config/PlanPeopleAvalonia/`

主要数据文件：
- `students.json` - 学生信息
- `teachers.json` - 教师信息
- `courses.json` - 课程数据
- `schedules.json` - 日程安排
- `config.json` - 应用配置

## 配置说明

应用提供丰富的配置选项，包括：
- 界面主题与配色
- 课程表显示参数
- 窗口行为设置
- 数据安全与加密
- 自动备份策略

## 许可证

本项目基于 MIT 许可证开源。