using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using CourseApp.Services;

namespace CourseApp.Theme
{
    /// <summary>
    /// 图标常量：从 icons/ 目录读 SVG，返回 "P:&lt;path d&gt;" 字符串。
    /// 支持 SVG 元素：path / circle / ellipse / line / rect / polyline / polygon。
    /// 找不到文件返回 ""。
    /// </summary>
    public static class Icons
    {
        // =====================================================
        // 短名 → 相对 icons/ 的路径
        // =====================================================
        private static readonly Dictionary<string, string> Map = new()
        {
            ["Minimize"]      = "title/minimize.svg",
            ["Maximize"]      = "title/maximize.svg",
            ["Restore"]       = "title/restore.svg",
            ["Close"]         = "title/close.svg",
            ["App"]           = "title/app.svg",

            ["Prev"]          = "toolbar/prev.svg",
            ["Next"]          = "toolbar/next.svg",
            ["EditTime"]      = "toolbar/edit_time.svg",
            ["AddTeacher"]    = "toolbar/add_teacher.svg",
            ["Import"]        = "toolbar/import.svg",
            ["Export"]        = "toolbar/export.svg",
            ["Save"]          = "toolbar/save.svg",
            ["Search"]        = "toolbar/search.svg",

            ["Person"]        = "teacher/user.svg",
            ["Phone"]         = "teacher/phone.svg",
            ["Mail"]          = "teacher/mail.svg",
            ["Office"]        = "teacher/office.svg",
            ["SearchTeacher"] = "teacher/search_teacher.svg",

            ["Book"]          = "course/book.svg",
            ["CourseTeacher"] = "course/teacher.svg",
            ["Classroom"]     = "course/classroom.svg",
            ["Loop"]          = "course/loop.svg",
            ["Calendar"]      = "course/calendar.svg",
            ["Lock"]          = "course/lock.svg",
            ["Ok"]            = "course/ok.svg",
            ["Cancel"]        = "course/cancel.svg",

            ["Success"]       = "status/success.svg",
            ["Warning"]       = "status/warning.svg",
            ["Error"]         = "status/error.svg",
            ["Info"]          = "status/info.svg",

            ["Add"]           = "toolbar/add.svg",
            ["Edit"]          = "toolbar/edit.svg",
            ["Delete"]        = "toolbar/delete.svg",
            ["Copy"]          = "toolbar/copy.svg",
            ["Paste"]         = "toolbar/paste.svg",
            ["Cut"]           = "toolbar/cut.svg",
            ["SelectAll"]     = "toolbar/selectall.svg",
            ["Folder"]        = "toolbar/folder.svg",
            ["File"]          = "toolbar/file.svg",
            ["Settings"]      = "toolbar/settings.svg",
            ["Refresh"]       = "toolbar/refresh.svg",
            ["Clock"]         = "toolbar/clock.svg",
            ["Menu"]          = "toolbar/menu.svg",
            ["ChevronDown"]   = "toolbar/chevron_down.svg",
            ["ChevronUp"]     = "toolbar/chevron_up.svg",
            ["ChevronLeft"]   = "toolbar/chevron_left.svg",
            ["ChevronRight"]  = "toolbar/chevron_right.svg",
            ["ArrowUp"]       = "toolbar/arrow_up.svg",
            ["ArrowDown"]     = "toolbar/arrow_down.svg",
        };

        private static readonly Dictionary<string, string> _cache = new();

        // =====================================================
        // 正则：匹配所有基础图形
        // =====================================================
        private static readonly Regex PathRegex = new(
            @"<path[^>]*?\sd\s*=\s*""([^""]+)""",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private static readonly Regex CircleRegex = new(
            @"<circle[^>]*?\scx\s*=\s*""([^""]+)""[^>]*?\scy\s*=\s*""([^""]+)""[^>]*?\sr\s*=\s*""([^""]+)""",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // circle 属性顺序可能不同，补一条
        private static readonly Regex CircleRegex2 = new(
            @"<circle[^>]*?\sr\s*=\s*""([^""]+)""[^>]*?\scx\s*=\s*""([^""]+)""[^>]*?\scy\s*=\s*""([^""]+)""",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private static readonly Regex EllipseRegex = new(
            @"<ellipse[^>]*?\scx\s*=\s*""([^""]+)""[^>]*?\scy\s*=\s*""([^""]+)""[^>]*?\srx\s*=\s*""([^""]+)""[^>]*?\sry\s*=\s*""([^""]+)""",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private static readonly Regex LineRegex = new(
            @"<line[^>]*?\sx1\s*=\s*""([^""]+)""[^>]*?\sy1\s*=\s*""([^""]+)""[^>]*?\sx2\s*=\s*""([^""]+)""[^>]*?\sy2\s*=\s*""([^""]+)""",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private static readonly Regex RectRegex = new(
            @"<rect[^>]*?\sx\s*=\s*""([^""]+)""[^>]*?\sy\s*=\s*""([^""]+)""[^>]*?\swidth\s*=\s*""([^""]+)""[^>]*?\sheight\s*=\s*""([^""]+)""",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private static readonly Regex PolylineRegex = new(
            @"<(polyline|polygon)[^>]*?\spoints\s*=\s*""([^""]+)""",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // =====================================================
        // 取值
        // =====================================================
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            if (_cache.TryGetValue(key, out var v)) return v;

            string result = "";

            if (Map.TryGetValue(key, out var rel))
            {
                var full = Path.Combine(AppPaths.IconsDir,
                                        rel.Replace('/', Path.DirectorySeparatorChar));

                if (System.IO.File.Exists(full))
                {
                    try
                    {
                        var svg = System.IO.File.ReadAllText(full);
                        var path = ConvertSvgToPath(svg);
                        if (!string.IsNullOrEmpty(path))
                            result = "P:" + path;
                    }
                    catch { }
                }
            }

            _cache[key] = result;
            return result;
        }

        // =====================================================
        // SVG → path data
        // =====================================================
        private static string ConvertSvgToPath(string svg)
        {
            var sb = new StringBuilder();

            // ---------- <path d="..."> ----------
            foreach (Match m in PathRegex.Matches(svg))
            {
                var d = m.Groups[1].Value.Trim();
                if (d.Length == 0) continue;
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(d);
            }

            // ---------- <circle cx cy r> ----------
            foreach (Match m in CircleRegex.Matches(svg))
            {
                var cx = ParseF(m.Groups[1].Value);
                var cy = ParseF(m.Groups[2].Value);
                var r  = ParseF(m.Groups[3].Value);
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(CircleToPath(cx, cy, r));
            }
            foreach (Match m in CircleRegex2.Matches(svg))
            {
                var r  = ParseF(m.Groups[1].Value);
                var cx = ParseF(m.Groups[2].Value);
                var cy = ParseF(m.Groups[3].Value);
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(CircleToPath(cx, cy, r));
            }

            // ---------- <ellipse cx cy rx ry> ----------
            foreach (Match m in EllipseRegex.Matches(svg))
            {
                var cx = ParseF(m.Groups[1].Value);
                var cy = ParseF(m.Groups[2].Value);
                var rx = ParseF(m.Groups[3].Value);
                var ry = ParseF(m.Groups[4].Value);
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(EllipseToPath(cx, cy, rx, ry));
            }

            // ---------- <line x1 y1 x2 y2> ----------
            foreach (Match m in LineRegex.Matches(svg))
            {
                var x1 = ParseF(m.Groups[1].Value);
                var y1 = ParseF(m.Groups[2].Value);
                var x2 = ParseF(m.Groups[3].Value);
                var y2 = ParseF(m.Groups[4].Value);
                if (sb.Length > 0) sb.Append(' ');
                sb.Append($"M {Fmt(x1)} {Fmt(y1)} L {Fmt(x2)} {Fmt(y2)}");
            }

            // ---------- <rect x y w h> ----------
            foreach (Match m in RectRegex.Matches(svg))
            {
                var x = ParseF(m.Groups[1].Value);
                var y = ParseF(m.Groups[2].Value);
                var w = ParseF(m.Groups[3].Value);
                var h = ParseF(m.Groups[4].Value);
                if (sb.Length > 0) sb.Append(' ');
                sb.Append($"M {Fmt(x)} {Fmt(y)} H {Fmt(x + w)} V {Fmt(y + h)} H {Fmt(x)} Z");
            }

            // ---------- <polyline> / <polygon> ----------
            foreach (Match m in PolylineRegex.Matches(svg))
            {
                var isPolygon = m.Groups[1].Value.Equals("polygon", System.StringComparison.OrdinalIgnoreCase);
                var pts = m.Groups[2].Value;

                var nums = pts.Split(new[] { ' ', ',', '\t', '\r', '\n' },
                                     System.StringSplitOptions.RemoveEmptyEntries);
                if (nums.Length < 4) continue;

                var part = new StringBuilder();
                part.Append($"M {nums[0]} {nums[1]}");
                for (int i = 2; i + 1 < nums.Length; i += 2)
                    part.Append($" L {nums[i]} {nums[i + 1]}");
                if (isPolygon) part.Append(" Z");

                if (sb.Length > 0) sb.Append(' ');
                sb.Append(part);
            }

            return sb.ToString();
        }

        /// <summary>圆 → path（用两段半圆弧）</summary>
        private static string CircleToPath(float cx, float cy, float r)
        {
            // M cx,cy-r  A r r 0 1 1 cx,cy+r  A r r 0 1 1 cx,cy-r  Z
            return $"M {Fmt(cx)} {Fmt(cy - r)} " +
                   $"A {Fmt(r)} {Fmt(r)} 0 1 1 {Fmt(cx)} {Fmt(cy + r)} " +
                   $"A {Fmt(r)} {Fmt(r)} 0 1 1 {Fmt(cx)} {Fmt(cy - r)} Z";
        }

        /// <summary>椭圆 → path</summary>
        private static string EllipseToPath(float cx, float cy, float rx, float ry)
        {
            return $"M {Fmt(cx)} {Fmt(cy - ry)} " +
                   $"A {Fmt(rx)} {Fmt(ry)} 0 1 1 {Fmt(cx)} {Fmt(cy + ry)} " +
                   $"A {Fmt(rx)} {Fmt(ry)} 0 1 1 {Fmt(cx)} {Fmt(cy - ry)} Z";
        }

        private static float ParseF(string s)
        {
            float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v);
            return v;
        }

        private static string Fmt(float v)
        {
            return v.ToString("0.###", CultureInfo.InvariantCulture);
        }

        // =====================================================
        // 属性访问器
        // =====================================================
        public static string Minimize     => Get("Minimize");
        public static string Maximize     => Get("Maximize");
        public static string Restore      => Get("Restore");
        public static string Close        => Get("Close");
        public static string App          => Get("App");

        public static string Prev         => Get("Prev");
        public static string Next         => Get("Next");
        public static string EditTime     => Get("EditTime");
        public static string AddTeacher   => Get("AddTeacher");
        public static string Import       => Get("Import");
        public static string Export       => Get("Export");
        public static string Save         => Get("Save");
        public static string Search       => Get("Search");

        public static string Person       => Get("Person");
        public static string Phone        => Get("Phone");
        public static string Mail         => Get("Mail");
        public static string Office       => Get("Office");
        public static string SearchTeacher => Get("SearchTeacher");

        public static string Book         => Get("Book");
        public static string CourseTeacher => Get("CourseTeacher");
        public static string Classroom    => Get("Classroom");
        public static string Loop         => Get("Loop");
        public static string Calendar     => Get("Calendar");
        public static string Lock         => Get("Lock");
        public static string Ok           => Get("Ok");
        public static string Cancel       => Get("Cancel");

        public static string Success      => Get("Success");
        public static string Warning      => Get("Warning");
        public static string Error        => Get("Error");
        public static string Info         => Get("Info");

        public static string Add          => Get("Add");
        public static string Edit         => Get("Edit");
        public static string Delete       => Get("Delete");
        public static string Copy         => Get("Copy");
        public static string Paste        => Get("Paste");
        public static string Cut          => Get("Cut");
        public static string SelectAll    => Get("SelectAll");
        public static string ChevronDown  => Get("ChevronDown");
        public static string ChevronUp    => Get("ChevronUp");
        public static string ChevronLeft  => Get("ChevronLeft");
        public static string ChevronRight => Get("ChevronRight");
        public static string ArrowUp      => Get("ArrowUp");
        public static string ArrowDown    => Get("ArrowDown");
        public static string Folder       => Get("Folder");
        public static string FileIcon     => Get("File");
        public static string Settings     => Get("Settings");
        public static string Refresh      => Get("Refresh");
        public static string Clock        => Get("Clock");
        public static string Menu         => Get("Menu");
    }
}