namespace CalamityLegendsComeBack.Weapons.BrinyBaron
{
    // Right-click dash-only tuning. Keep damage, movement and cooldown
    // values here so they can be adjusted without touching the dash state machine.
    internal static partial class BB_Balance
    {
        public const float RightClickDamageMultiplier = 1.08f;
        public const int DefaultRightClickCooldown = 3 * 60;
        public const int VortexEyeCooldown = 6 * 60;

        public const int RightClickPrepareFrames = 8;
        public const int RightClickDashFrames = 32;
        // Return speed deliberately stays unchanged. Doubling these frames doubles the
        // rebound distance/commitment instead of making the dash itself faster.
        public const int RightClickReboundFrames = 24;
        public const float RightClickDashSpeed = 9.6f;
        public const float RightClickReboundSpeed = 9f;
        public const float DefaultReboundDashSpeedMultiplier = 0.6f;
        // Enemy-hit return only. This doubles the already-tuned rebound speed,
        // without changing the outgoing dash or a rebound caused by terrain.
        public const float RightClickEnemyReboundSpeedMultiplier = 2f;
        public const int RightClickEnemyHitIFrameDuration = 20;
        public const float RightClickDashTurnRate = 0.01f;
        public const float RightClickReadyBladeDistance = 28f;
        public const float RightClickDashBladeDistance = 20f;
        public const float RightClickReboundBladeDistance = 18f;
        public const int RightClickDashLocalHitCooldown = 24;
        public const int RightClickHitCooldownAfterEnemyHit = 60;
        public const int AbyssalBastionRightClickCooldown = 120;
        public const int AbyssalBastionDashFrames = 30;
        public const float AbyssalBastionDashSpeedMultiplier = 1.25f;

        public static readonly float[] RightClickGrowthSpeedMultipliers = { 4.4f, 5.0f, 5.4f, 5.8f, 6.0f };
        public static readonly float[] RightClickGrowthContactDamageMultipliers = { 3f, 3.5f, 4.75f, 5.25f, 6.00f };
        public static readonly bool[] RightClickGrowthEnemyReboundUnlocks = { false, true, true, true, true };

        public const int OceanHormoneUseTimeReduction = 12;
        public const int RightSpinShurikenInterval = 36;
        public const float RightSpinPostFishronBubbleRadiusMultiplier = 3f;
    }
}
