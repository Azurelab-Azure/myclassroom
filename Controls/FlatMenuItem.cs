using System;

namespace CourseApp.Controls
{
    /// <summary>
    /// 右键菜单项的数据模型
    /// </summary>
    public class FlatMenuItem
    {
        /// <summary>菜单项类型</summary>
        public enum ItemType { Normal, Separator }

        /// <summary>菜单项类型</summary>
        public ItemType Type { get; set; } = ItemType.Normal;

        /// <summary>图标</summary>
        public string Icon { get; set; } = "";

        /// <summary>显示文本</summary>
        public string Text { get; set; } = "";

        /// <summary>快捷键提示文字</summary>
        public string Shortcut { get; set; } = "";

        /// <summary>是否可点击</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>是否为危险操作项</summary>
        public bool IsDanger { get; set; } = false;

        /// <summary>点击回调</summary>
        public Action? OnClick { get; set; }

        /// <summary>创建普通菜单项</summary>
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

        /// <summary>创建危险操作菜单项</summary>
        public static FlatMenuItem CreateDanger(string text, Action? onClick = null,
            string icon = "", string shortcut = "")
        {
            return Create(text, onClick, icon, shortcut, danger: true);
        }

        /// <summary>创建分隔线项</summary>
        public static FlatMenuItem CreateSeparator()
        {
            return new FlatMenuItem { Type = ItemType.Separator };
        }

        /// <summary>获取菜单项高度</summary>
        public int GetHeight() => Type == ItemType.Separator ? 9 : 32;
    }
}