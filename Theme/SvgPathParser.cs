using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace CourseApp.Theme
{
    public static class SvgPathParser
    {
        public static GraphicsPath Parse(string data, int targetW, int targetH, int srcSize = 16)
        {
            var path = new GraphicsPath();
            if (string.IsNullOrWhiteSpace(data)) return path;

            float scale = Math.Min((float)targetW / srcSize, (float)targetH / srcSize);

            var tokens = Tokenize(data);
            int i = 0;
            char cmd = 'M';

            float x = 0, y = 0;
            float sx = 0, sy = 0;
            float px = 0, py = 0;

            while (i < tokens.Count)
            {
                if (IsCommand(tokens[i][0]))
                {
                    cmd = tokens[i][0];
                    i++;
                }

                try
                {
                    switch (char.ToUpper(cmd))
                    {
                        case 'M':
                            x = F(tokens[i++]); y = F(tokens[i++]);
                            sx = x; sy = y;
                            path.StartFigure();
                            cmd = char.IsUpper(cmd) ? 'L' : 'l';
                            break;

                        case 'L':
                            {
                                float nx = F(tokens[i++]);
                                float ny = F(tokens[i++]);
                                if (char.IsLower(cmd)) { nx += x; ny += y; }
                                path.AddLine(x * scale, y * scale, nx * scale, ny * scale);
                                x = nx; y = ny;
                                break;
                            }
                        case 'H':
                            {
                                float nx = F(tokens[i++]);
                                if (char.IsLower(cmd)) nx += x;
                                path.AddLine(x * scale, y * scale, nx * scale, y * scale);
                                x = nx;
                                break;
                            }
                        case 'V':
                            {
                                float ny = F(tokens[i++]);
                                if (char.IsLower(cmd)) ny += y;
                                path.AddLine(x * scale, y * scale, x * scale, ny * scale);
                                y = ny;
                                break;
                            }
                        case 'C':
                            {
                                float c1x = F(tokens[i++]), c1y = F(tokens[i++]);
                                float c2x = F(tokens[i++]), c2y = F(tokens[i++]);
                                float ex = F(tokens[i++]), ey = F(tokens[i++]);
                                if (char.IsLower(cmd))
                                {
                                    c1x += x; c1y += y;
                                    c2x += x; c2y += y;
                                    ex += x; ey += y;
                                }
                                path.AddBezier(x * scale, y * scale,
                                               c1x * scale, c1y * scale,
                                               c2x * scale, c2y * scale,
                                               ex * scale, ey * scale);
                                px = c2x; py = c2y;
                                x = ex; y = ey;
                                break;
                            }
                        case 'S':
                            {
                                float c2x = F(tokens[i++]), c2y = F(tokens[i++]);
                                float ex = F(tokens[i++]), ey = F(tokens[i++]);
                                if (char.IsLower(cmd))
                                {
                                    c2x += x; c2y += y;
                                    ex += x; ey += y;
                                }
                                float c1x = 2 * x - px;
                                float c1y = 2 * y - py;
                                path.AddBezier(x * scale, y * scale,
                                               c1x * scale, c1y * scale,
                                               c2x * scale, c2y * scale,
                                               ex * scale, ey * scale);
                                px = c2x; py = c2y;
                                x = ex; y = ey;
                                break;
                            }
                        case 'Q':
                            {
                                float qx = F(tokens[i++]), qy = F(tokens[i++]);
                                float ex = F(tokens[i++]), ey = F(tokens[i++]);
                                if (char.IsLower(cmd))
                                {
                                    qx += x; qy += y;
                                    ex += x; ey += y;
                                }
                                path.AddBezier(x * scale, y * scale,
                                               qx * scale, qy * scale,
                                               qx * scale, qy * scale,
                                               ex * scale, ey * scale);
                                px = qx; py = qy;
                                x = ex; y = ey;
                                break;
                            }
                        case 'T':
                            {
                                float ex = F(tokens[i++]), ey = F(tokens[i++]);
                                if (char.IsLower(cmd)) { ex += x; ey += y; }
                                float qx = 2 * x - px;
                                float qy = 2 * y - py;
                                path.AddBezier(x * scale, y * scale,
                                               qx * scale, qy * scale,
                                               qx * scale, qy * scale,
                                               ex * scale, ey * scale);
                                px = qx; py = qy;
                                x = ex; y = ey;
                                break;
                            }
                        case 'A':
                            {
                                float rx = F(tokens[i++]);
                                float ry = F(tokens[i++]);
                                float rot = F(tokens[i++]);
                                int largeArc = (int)F(tokens[i++]);
                                int sweep = (int)F(tokens[i++]);
                                float ex = F(tokens[i++]), ey = F(tokens[i++]);
                                if (char.IsLower(cmd)) { ex += x; ey += y; }

                                DrawArc(path, x, y, ex, ey, rx, ry, rot, largeArc, sweep, scale);
                                x = ex; y = ey;
                                break;
                            }
                        case 'Z':
                            path.CloseFigure();
                            x = sx; y = sy;
                            break;
                    }
                }
                catch { i++; }
            }

            return path;
        }

        // =====================================================
        // 椭圆弧 → 多段贝塞尔（SVG 规范 F.6.5）
        // =====================================================
        private static void DrawArc(GraphicsPath path,
            float x1, float y1, float x2, float y2,
            float rx, float ry, float phiDeg, int largeArc, int sweep,
            float scale)
        {
            // 端点到椭圆坐标系
            if (rx == 0 || ry == 0)
            {
                path.AddLine(x1 * scale, y1 * scale, x2 * scale, y2 * scale);
                return;
            }

            rx = Math.Abs(rx);
            ry = Math.Abs(ry);
            float phi = phiDeg * (float)Math.PI / 180f;

            float cosPhi = (float)Math.Cos(phi);
            float sinPhi = (float)Math.Sin(phi);

            // 步骤 1：计算 (x1', y1')
            float dx = (x1 - x2) / 2f;
            float dy = (y1 - y2) / 2f;

            float x1p = cosPhi * dx + sinPhi * dy;
            float y1p = -sinPhi * dx + cosPhi * dy;

            // 修正半径
            float rxSq = rx * rx;
            float rySq = ry * ry;
            float x1pSq = x1p * x1p;
            float y1pSq = y1p * y1p;

            float lambda = x1pSq / rxSq + y1pSq / rySq;
            if (lambda > 1)
            {
                float sq = (float)Math.Sqrt(lambda);
                rx *= sq;
                ry *= sq;
                rxSq = rx * rx;
                rySq = ry * ry;
            }

            // 步骤 2：计算 (cx', cy')
            float sign = (largeArc != sweep) ? 1f : -1f;
            float num = rxSq * rySq - rxSq * y1pSq - rySq * x1pSq;
            float den = rxSq * y1pSq + rySq * x1pSq;
            if (den == 0) den = 1f;
            float coef = sign * (float)Math.Sqrt(Math.Max(0, num / den));

            float cxp = coef * (rx * y1p / ry);
            float cyp = coef * -(ry * x1p / rx);

            // 步骤 3：计算 (cx, cy)
            float cx = cosPhi * cxp - sinPhi * cyp + (x1 + x2) / 2f;
            float cy = sinPhi * cxp + cosPhi * cyp + (y1 + y2) / 2f;

            // 步骤 4：计算起始角、扫过角
            float ux = (x1p - cxp) / rx;
            float uy = (y1p - cyp) / ry;
            float vx = (-x1p - cxp) / rx;
            float vy = (-y1p - cyp) / ry;

            float theta1 = Angle(1, 0, ux, uy);
            float deltaTheta = Angle(ux, uy, vx, vy);

            if (sweep == 0 && deltaTheta > 0) deltaTheta -= 2 * (float)Math.PI;
            else if (sweep == 1 && deltaTheta < 0) deltaTheta += 2 * (float)Math.PI;

            // 步骤 5：拆成多段贝塞尔（每段 ≤ 90°）
            int segments = (int)Math.Ceiling(Math.Abs(deltaTheta) / ((float)Math.PI / 2));
            if (segments < 1) segments = 1;

            float delta = deltaTheta / segments;
            float t = (4f / 3f) * (float)Math.Tan(delta / 4f);

            float curX = x1, curY = y1;
            float curTheta = theta1;

            for (int s = 0; s < segments; s++)
            {
                float nextTheta = curTheta + delta;

                // 起点切线控制点
                float cosT1 = (float)Math.Cos(curTheta);
                float sinT1 = (float)Math.Sin(curTheta);
                float cosT2 = (float)Math.Cos(nextTheta);
                float sinT2 = (float)Math.Sin(nextTheta);

                // 椭圆上的点（局部坐标 → 世界坐标）
                float ex1 = cx + rx * cosT1 * cosPhi - ry * sinT1 * sinPhi;
                float ey1 = cy + rx * cosT1 * sinPhi + ry * sinT1 * cosPhi;
                float ex2 = cx + rx * cosT2 * cosPhi - ry * sinT2 * sinPhi;
                float ey2 = cy + rx * cosT2 * sinPhi + ry * sinT2 * cosPhi;

                // 切线
                float dx1 = -rx * sinT1 * cosPhi - ry * cosT1 * sinPhi;
                float dy1 = -rx * sinT1 * sinPhi + ry * cosT1 * cosPhi;
                float dx2 = -rx * sinT2 * cosPhi - ry * cosT2 * sinPhi;
                float dy2 = -rx * sinT2 * sinPhi + ry * cosT2 * cosPhi;

                float c1x = ex1 + t * dx1;
                float c1y = ey1 + t * dy1;
                float c2x = ex2 - t * dx2;
                float c2y = ey2 - t * dy2;

                path.AddBezier(
                    curX * scale, curY * scale,
                    c1x * scale, c1y * scale,
                    c2x * scale, c2y * scale,
                    ex2 * scale, ey2 * scale);

                curX = ex2;
                curY = ey2;
                curTheta = nextTheta;
            }
        }

        /// <summary>两向量夹角（带符号）</summary>
        private static float Angle(float ux, float uy, float vx, float vy)
        {
            float dot = ux * vx + uy * vy;
            float len = (float)Math.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));
            if (len == 0) return 0;

            float ang = (float)Math.Acos(Math.Max(-1, Math.Min(1, dot / len)));
            float cross = ux * vy - uy * vx;
            if (cross < 0) ang = -ang;
            return ang;
        }

        // ---------- 工具 ----------
        private static bool IsCommand(char c)
        {
            switch (char.ToUpper(c))
            {
                case 'M': case 'L': case 'H': case 'V':
                case 'C': case 'S': case 'Q': case 'T':
                case 'A': case 'Z':
                    return true;
            }
            return false;
        }

        private static float F(string s) =>
            float.Parse(s, CultureInfo.InvariantCulture);

        // ---------- Tokenizer（支持科学计数法） ----------
        private static List<string> Tokenize(string s)
        {
            var list = new List<string>();
            int i = 0;
            while (i < s.Length)
            {
                char c = s[i];
                if (char.IsWhiteSpace(c) || c == ',') { i++; continue; }

                if (IsCommand(c)) { list.Add(c.ToString()); i++; continue; }

                int start = i;
                if (s[i] == '+' || s[i] == '-') i++;
                while (i < s.Length && char.IsDigit(s[i])) i++;
                if (i < s.Length && s[i] == '.')
                {
                    i++;
                    while (i < s.Length && char.IsDigit(s[i])) i++;
                }
                if (i < s.Length && (s[i] == 'e' || s[i] == 'E'))
                {
                    int save = i;
                    i++;
                    if (i < s.Length && (s[i] == '+' || s[i] == '-')) i++;
                    if (i < s.Length && char.IsDigit(s[i]))
                        while (i < s.Length && char.IsDigit(s[i])) i++;
                    else
                        i = save;
                }
                if (i == start) { i++; continue; }
                list.Add(s.Substring(start, i - start));
            }
            return list;
        }
    }
}