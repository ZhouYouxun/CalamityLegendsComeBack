namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    internal static class BFReconLeftBalance
    {
        // ### 侦察普攻
        // 命中标记持续 30 帧。
        public const int MarkDuration = 30;

        // 追踪优化：缩短启动延迟，提高转向响应，让弹幕能绕更小的弧追踪，但不做锐角折返。
        public const int HomingDelayFrames = 18;
        public const float HomingTurnResponsiveness = 0.22f;
        public const float PriorityHomingTurnResponsiveness = 0.34f;
    }
}
