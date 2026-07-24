using System;
using CalamityMod;
using CalamityMod.NPCs.NormalNPCs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle.Proj;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle
{
    internal enum AMRProgressionStage
    {
        Initial,
        EyeOfCthulhu,
        EvilBoss,
        WallOfFlesh,
        AnyMechanicalBoss,
        Plantera,
        MoonLord,
        Providence,
        DevourerOfGods,
        Finale
    }

    internal static class AMRBalance
    {
        private static readonly int[] BaseDamages =
        {
            168,
            220,
            300,
            480,
            700,
            1050,
            1818,
            3000,
            4200,
            7000
        };

        public const int InitialDamage = 168;
        public const int BaseFireInterval = 60;
        public const int ScopeChargeFrames = 90;
        public const int MinimumScopeChargeFrames = 12;
        public const int SlideFrames = 15;
        public const int SlideChainWindowFrames = 120; // 2 seconds window between slides
        public const int SlideCooldownFrames = 1800;   // 30 seconds cooldown after complete slide sequence
        public const int MaxSlideChainCount = 4;       // Up to 4 chained slides
        public const float FlightSpeedScale = 0.67f;

        public static AMRProgressionStage Stage
        {
            get
            {
                AMRProgressionStage stage = AMRProgressionStage.Initial;

                if (NPC.downedBoss1)
                    stage = AMRProgressionStage.EyeOfCthulhu;
                if (NPC.downedBoss2)
                    stage = AMRProgressionStage.EvilBoss;
                if (Main.hardMode)
                    stage = AMRProgressionStage.WallOfFlesh;
                if (NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3)
                    stage = AMRProgressionStage.AnyMechanicalBoss;
                if (NPC.downedPlantBoss)
                    stage = AMRProgressionStage.Plantera;
                if (NPC.downedMoonlord)
                    stage = AMRProgressionStage.MoonLord;
                if (DownedBossSystem.downedProvidence)
                    stage = AMRProgressionStage.Providence;
                if (DownedBossSystem.downedDoG)
                    stage = AMRProgressionStage.DevourerOfGods;
                if (DownedBossSystem.downedExoMechs || DownedBossSystem.downedCalamitas)
                    stage = AMRProgressionStage.Finale;

                return stage;
            }
        }

        public static int CurrentDamage => BaseDamages[(int)Stage];
        public static int FireInterval => BaseFireInterval;
        public static bool DeathMarkUnlocked => Stage >= AMRProgressionStage.EyeOfCthulhu;
        public static bool MetalJetUnlocked => Stage >= AMRProgressionStage.EvilBoss;
        public static bool SlideUnlocked => Stage >= AMRProgressionStage.WallOfFlesh;
        public static bool CalibrationUnlocked => Stage >= AMRProgressionStage.Plantera;
        public static bool CriticalOverflowUnlocked => Stage >= AMRProgressionStage.MoonLord;
        public static bool CoreRuptureUnlocked => Stage >= AMRProgressionStage.Providence;
        public static bool DimensionalSlideUnlocked => Stage >= AMRProgressionStage.DevourerOfGods;
        public static bool OnyxSequenceUnlocked => Stage >= AMRProgressionStage.Finale;

        // Stage 0 天赋：命中造成最大生命值真实伤害 (普通敌人 10%，Boss 0.5%，受 DR 提升)。
        // 假人 (原版靶子 / 灾厄超级假人) 不参与：超级假人 lifeMax = 9999999，10% 会打出 999999
        // 这种和武器真实输出毫无关系的数字，反而让玩家误判自己的伤害。
        public static bool IsTrainingDummy(NPC target) =>
            target.type == NPCID.TargetDummy ||
            target.type == ModContent.NPCType<SuperDummyNPC>();

        public static bool TryGetMaxLifeTrueDamage(NPC target, out int trueDamage)
        {
            trueDamage = 0;
            if (!target.active || target.lifeMax <= 5 || IsTrainingDummy(target))
                return false;

            bool isBoss = target.boss || target.realLife >= 0;
            float damage = target.lifeMax * (isBoss ? 0.005f : 0.10f);

            // DR (Damage Reduction) 加成
            float dr = target.Calamity().DR;
            if (dr > 0f)
                damage *= 1f + dr;

            trueDamage = Math.Max(1, (int)damage);
            return true;
        }

        public static float LeftProjectileSpeed => (32f + (int)Stage * 1.8f) * FlightSpeedScale;
        public static float RightProjectileSpeed(float charge) => MathHelper.Lerp(38f, 62f, charge) * FlightSpeedScale;
        public static float RightDamageMultiplier(float charge) => MathHelper.Lerp(1.0f, 5.0f, charge);
    }
}
