# PlanPeople Avalonia

A cross-platform desktop application for class management and timetabling, developed based on Avalonia UI.

## Project Introduction

PlanPeople Avalonia is a feature-rich educational management desktop application that supports timetable management, student information management, teacher information management, exam records, grade management, and more. It adopts an MVVM architecture design, supports multi-theme switching, and multi-language internationalization.

## Main Features

### 📚 Timetable Management
- Visual timetable editing, supporting drag-and-drop adjustment of course positions
- Supports multiple subject configurations and custom colors
- Course conflict detection
- Browse timetable by week

### 👨‍🎓 Student Management
- Student information entry (Name, Student ID, Seat, Role, etc.)
- Seat layout configuration
- Class duty arrangement
- Import/Export CSV

### 👨‍🏫 Teacher Management
- Teacher information management
- Teaching course association
- Teacher details display

### 📝 Exams & Grades
- Exam record management
- Grade entry and statistics
- Student points management
- Point records and classification

### ⚙️ System Functions
- Multi-theme support (Light, Dark, Fresh, Transparent)
- Multi-language support (Default: Simplified Chinese)
- Automatic update check
- Data backup and restoration
- Dynamic Island / Floating Window Mode
- Privacy policy management

## Tech Stack

- **Framework**: Avalonia UI 11.x
- **Language**: C# (.NET 8)
- **Architecture**: MVVM
- **Data Storage**: JSON local file storage
- **Icons**: SVG icon resources

## Project Structure

```
PlanPeopleAvalonia/
├── Models/              # Data Models
├── Services/            # Business Service Layer
├── ViewModels/          # View Models
├── Views/               # View Layer
│   ├── ScheduleComponents/   # Timetable Components
│   ├── StudentComponents/    # Student Components
│   ├── TeacherComponents/    # Teacher Components
│   ├── ExamComponents/       # Exam Components
│   └── SettingsComponents/   # Settings Components
├── Localization/        # Internationalization Resources
├── Styles/              # Style Resources
└── icons/               # Icon Resources
```

## Runtime Requirements

- .NET 8.0 SDK or higher
- Supports Windows, macOS, Linux systems

## Build & Run

```bash
# Clone project
git clone https://gitee.com/cyukk/plan-people-avalonia.git

# Enter project directory
cd plan-people-avalonia

# Restore dependencies
dotnet restore

# Run project
dotnet run

# Publish release version
dotnet publish -c Release
```

## Data Storage

Application data is stored by default in the user data directory:
- Windows: `%APPDATA%\PlanPeopleAvalonia\`
- macOS: `~/Library/Application Support/PlanPeopleAvalonia/`
- Linux: `~/.config/PlanPeopleAvalonia/`

Main data files:
- `students.json` - Student information
- `teachers.json` - Teacher information
- `courses.json` - Course data
- `schedules.json` - Schedule arrangements
- `config.json` - Application configuration

## Configuration Description

The application provides rich configuration options, including:
- Interface theme and color scheme
- Timetable display parameters
- Window behavior settings
- Data security and encryption
- Automatic backup strategy

## License

This project is open source under the MIT License.