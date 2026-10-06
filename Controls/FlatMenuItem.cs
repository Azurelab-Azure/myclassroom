using System;

namespace CourseApp.Controls
{
    /// <summary>
    /// 右键菜单项的数据模型。
    /// </summary>
    public class FlatMenuItem
    {
        public enum ItemType { Normal, Separator }

        public ItemType Type { get; set; } = ItemType.Normal;
        public string Icon { get; set; } = "";
        public string Text { get; set; } = "";
        public string Shortcut { get; set; } = "";
        public bool Enabled { get; set; } = true;
        public bool IsDanger { get; set; } = false;
        public Action? OnClick { get; set; }

        // ============ 工厂 ============
        public static FlatMenuItem Create(string text, Action? onClick = null,
            string icon = "", string shortcut = "", bool danger = false)
        {
            return new FlatMenuItem
            {
                Type = ItemType.Normal,
                Text = text ?? "",
                Icon = icon ?? "",
                Shortcut = shortcut ?? "",
                OnClick = onClick,
                IsDanger = danger,
            };
        }

        public static FlatMenuItem CreateDanger(string text, Action? onClick = null,
            string icon = "", string shortcut = "")
        {
            return Create(text, onClick, icon, shortcut, danger: true);
        }

        public static FlatMenuItem CreateSeparator()
        {
            return new FlatMenuItem { Type = ItemType.Separator };
        }

        // ============ 高度 ============
        public int GetHeight() => Type == ItemType.Separator ? 9 : 32;
    }
}