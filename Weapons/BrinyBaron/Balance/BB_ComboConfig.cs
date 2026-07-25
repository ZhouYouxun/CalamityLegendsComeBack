using System;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron
{
    public enum BBLeftClickComboType
    {
        SeafoamBlade,
        FormalWave,
        FormalWaveFaster,
        EnhancedWave,
        ThreeSeafoams,
        SixSeaSpirits,
        TenSeaSpirits,
        FiveShurikens,
        FiveParallelSeafoams,
        Tornado
    }

    // Every left-click projectile that uses local NPC immunity is exposed here.
    // Array index: 0 Pre-Hardmode, 1 Hardmode, 2 Post-Plantera, 3 Post-Moon Lord, 4 Post-DoG.
    public enum BBLeftProjectile
    {
        TrueMeleeSwing,
        SeafoamBlade,
        SwordWave,
        SeaSpirit,
        Shuriken,
        HomingLightOrb,
        WaterStream,
        TornadoBolt,
        TornadoWaterExplosion,
        SeafoamExplosion
    }

    internal static partial class BB_Balance
    {
        public static readonly BBLeftClickComboType[] Stage1_ComboSequence = { BBLeftClickComboType.SeafoamBlade };
        public static readonly BBLeftClickComboType[] Stage2_ComboSequence = { BBLeftClickComboType.ThreeSeafoams, BBLeftClickComboType.SixSeaSpirits, BBLeftClickComboType.FormalWave };
        public static readonly BBLeftClickComboType[] Stage3_ComboSequence = { BBLeftClickComboType.FormalWave, BBLeftClickComboType.TenSeaSpirits, BBLeftClickComboType.FiveShurikens, BBLeftClickComboType.EnhancedWave };
        public static readonly BBLeftClickComboType[] Stage4_ComboSequence = { BBLeftClickComboType.FormalWave, BBLeftClickComboType.TenSeaSpirits, BBLeftClickComboType.FiveShurikens, BBLeftClickComboType.EnhancedWave };
        public static readonly BBLeftClickComboType[] Stage5_ComboSequence = { BBLeftClickComboType.EnhancedWave, BBLeftClickComboType.TenSeaSpirits, BBLeftClickComboType.FiveShurikens, BBLeftClickComboType.EnhancedWave };

        // Local NPC immunity in ticks. -1 means this projectile can hit each NPC once only.
        public static readonly int[] TrueMeleeSwingHitCooldown = { 16, 16, 16, 16, 16 };
        public static readonly int[] SeafoamBladeHitCooldown = { 15, 15, 15, 15, 15 };
        public static readonly int[] SwordWaveHitCooldown = { 18, 18, 18, 18, 18 };
        public static readonly int[] SeaSpiritHitCooldown = { 15, 15, 15, 15, 15 };
        public static readonly int[] ShurikenHitCooldown = { 20, 20, 20, 20, 20 };
        public static readonly int[] HomingLightOrbHitCooldown = { 16, 16, 16, 16, 16 };
        public static readonly int[] WaterStreamHitCooldown = { 10, 10, 10, 10, 10 };
        public static readonly int[] TornadoBoltHitCooldown = { 12, 12, 12, 12, 12 };
        public static readonly int[] TornadoWaterExplosionHitCooldown = { -1, -1, -1, -1, -1 };
        public static readonly int[] SeafoamExplosionHitCooldown = { -1, -1, -1, -1, -1 };

        public static int GetLeftProjectileHitCooldown(BBLeftProjectile projectile)
        {
            int stage = Math.Clamp(GetGrowthStage(), 0, 4);
            return projectile switch
            {
                BBLeftProjectile.TrueMeleeSwing => TrueMeleeSwingHitCooldown[stage],
                BBLeftProjectile.SeafoamBlade => SeafoamBladeHitCooldown[stage],
                BBLeftProjectile.SwordWave => SwordWaveHitCooldown[stage],
                BBLeftProjectile.SeaSpirit => SeaSpiritHitCooldown[stage],
                BBLeftProjectile.Shuriken => ShurikenHitCooldown[stage],
                BBLeftProjectile.HomingLightOrb => HomingLightOrbHitCooldown[stage],
                BBLeftProjectile.WaterStream => WaterStreamHitCooldown[stage],
                BBLeftProjectile.TornadoBolt => TornadoBoltHitCooldown[stage],
                BBLeftProjectile.TornadoWaterExplosion => TornadoWaterExplosionHitCooldown[stage],
                BBLeftProjectile.SeafoamExplosion => SeafoamExplosionHitCooldown[stage],
                _ => -1
            };
        }
    }
}
