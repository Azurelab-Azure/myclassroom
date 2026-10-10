namespace CourseApp.Theme
{
    /// <summary>
    /// WinUI 3 风格设计令牌。
    /// 所有新组件统一从这里取值，保证圆角、间距、字号一致。
    /// </summary>
    public static class WinUI3Tokens
    {
        // =====================================================
        // 圆角
        // =====================================================
        /// <summary>卡片圆角</summary>
        public const int CardRadius = 8;

        /// <summary>控件圆角</summary>
        public const int ControlRadius = 4;

        /// <summary>对话框圆角</summary>
        public const int DialogRadius = 12;

        /// <summary>徽章圆角</summary>
        public const int BadgeRadius = 10;

        // =====================================================
        // 间距
        // =====================================================
        /// <summary>卡片内边距</summary>
        public const int CardPadding = 16;

        /// <summary>分区之间的间距</summary>
        public const int SectionGap = 24;

        /// <summary>列表项之间的间距</summary>
        public const int ItemGap = 8;

        /// <summary>行内元素间距</summary>
        public const int InlineGap = 6;

        /// <summary>页面外边距</summary>
        public const int PagePadding = 24;

        // =====================================================
        // 字号
        // =====================================================
        public const float FontCaption = 12f;
        public const float FontBody = 14f;
        public const float FontSubtitle = 20f;
        public const float FontTitle = 28f;
        public const float FontDisplay = 40f;

        // =====================================================
        // 控件尺寸
        // =====================================================
        public const int ControlHeight = 32;
        public const int ControlHeightLarge = 40;
        public const int AvatarSmall = 32;
        public const int AvatarMedium = 48;
        public const int AvatarLarge = 96;

        // =====================================================
        // 动效
        // =====================================================
        /// <summary>悬停抬升高度（px）</summary>
        public const int HoverLift = 1;

        /// <summary>动画帧间隔（ms）</summary>
        public const int AnimInterval = 16;

        /// <summary>动画缓动系数</summary>
        public const float AnimEase = 0.25f;
    }
}