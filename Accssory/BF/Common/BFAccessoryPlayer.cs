using CalamityLegendsComeBack.Accssory.BF.SeedOfSilva;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.Common
{
    internal sealed class BFAccessoryPlayer : ModPlayer
    {
        public const int SilvaHarpPassiveCooldownFrames = 30 * 60;
        public const int SilvaHarpFinalStandImmuneFrames = 10 * 60;

        public int QuiverTier;
        public bool DominationQuiverEquipped;
        public bool SeedOfSilvaEquipped;
        public bool SilvaHarpEquipped;
        public bool PastLingeringEquipped;
        public bool SwordsplosionQuiverEquipped;
        public bool BlightSpewerQuiverEquipped;

        // ── 箭袋强化字段（每帧由 EquipQuiver 写入，ResetEffects 清零）──
        public float ChargeMultiplier = 1f;           // 所有战术右键蓄力时间乘数
        public int BreakthroughExtraLoads;            // 突击右键额外最大装填数
        public int RecoveryExtraFlashes;              // 复苏右键额外治疗闪光数
        public int ReconExtraMarkFrames;              // 侦察右键额外标记持续帧数
        public bool BombardExplosionBonus;            // 歼灭右键爆炸范围提升（同调/主宰）
        public bool BombardRainDamageBonus;           // 歼灭右键弹幕伤害提升（共鸣/主宰）
        public int PlagueAdvancedTier;                // 侵染高级 Debuff 等级：0=无，1=第一批，2=全部

        public bool HoldingBlossomFlux => Player.HeldItem?.type == ModContent.ItemType<NewLegendBlossomFlux>();

        public BlossomFluxChloroplastPresetType CurrentPreset =>
            HoldingBlossomFlux ? Player.GetModPlayer<BFRightUIPlayer>().CurrentPreset : BlossomFluxChloroplastPresetType.Chlo_BRecov;

        public override void ResetEffects()
        {
            QuiverTier = 0;
            DominationQuiverEquipped = false;
            SeedOfSilvaEquipped = false;
            SilvaHarpEquipped = false;
            PastLingeringEquipped = false;
            SwordsplosionQuiverEquipped = false;
            BlightSpewerQuiverEquipped = false;

            ChargeMultiplier = 1f;
            BreakthroughExtraLoads = 0;
            RecoveryExtraFlashes = 0;
            ReconExtraMarkFrames = 0;
            BombardExplosionBonus = false;
            BombardRainDamageBonus = false;
            PlagueAdvancedTier = 0;
        }

        public override void UpdateDead()
        {
            ResetEffects();
        }

        public override void PostUpdate()
        {
            if (SeedOfSilvaEquipped && Player.whoAmI == Main.myPlayer)
                EnsureSilvaSeeds();
        }

        public override void UpdateLifeRegen()
        {
            if (!SilvaHarpEquipped)
                return;

            Player.lifeRegen += 10;
            if (Player.lifeRegen < 10)
                Player.lifeRegen = 10;
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            if (SilvaHarpEquipped)
                Player.immuneTime += 60;
        }

        public void EquipQuiver(int tier)
        {
            if (tier <= QuiverTier)
                return;

            QuiverTier = tier;

            ChargeMultiplier = tier switch
            {
                1 => 0.90f,
                2 => 0.80f,
                3 => 0.65f,
                4 => 0.50f,
                _ => 1f
            };

            BreakthroughExtraLoads = tier switch { 1 => 1, 2 => 2, 3 => 4, 4 => 6, _ => 0 };
            RecoveryExtraFlashes   = tier switch { 1 => 1, 2 => 2, 3 => 3, 4 => 4, _ => 0 };
            ReconExtraMarkFrames   = tier switch { 1 => 100, 2 => 300, 3 => 600, 4 => 900, _ => 0 };

            // 同调(2)和主宰(4)提升爆炸范围；共鸣(3)和主宰(4)提升弹幕伤害
            BombardExplosionBonus  = tier == 2 || tier >= 4;
            BombardRainDamageBonus = tier >= 3;

            // 共鸣(3)解锁第一批高级 Debuff；主宰(4)解锁全部
            PlagueAdvancedTier = tier >= 4 ? 2 : tier >= 3 ? 1 : 0;

            if (tier >= 4)
                DominationQuiverEquipped = true;
        }

        public float GetQuiverSpeedMultiplier(BlossomFluxChloroplastPresetType preset, bool convertedLeafArrow = false)
        {
            if (QuiverTier <= 0)
                return 1f;

            int tierIndex = Utils.Clamp(QuiverTier - 1, 0, 3);

            return preset switch
            {
                BlossomFluxChloroplastPresetType.Chlo_ABreak => convertedLeafArrow ? BreakthroughWoodArrowSpeed[tierIndex] : BreakthroughOtherArrowSpeed[tierIndex],
                BlossomFluxChloroplastPresetType.Chlo_BRecov => RecoveryArrowSpeed[tierIndex],
                BlossomFluxChloroplastPresetType.Chlo_CDetec => ReconArrowSpeed[tierIndex],
                BlossomFluxChloroplastPresetType.Chlo_DBomb => BombardArrowSpeed[tierIndex],
                BlossomFluxChloroplastPresetType.Chlo_EPlague => PlagueArrowSpeed[tierIndex],
                _ => 1f
            };
        }

        // 箭袋速度加成：备用 / 同调 / 共鸣 / 主宰
        private static readonly float[] BreakthroughWoodArrowSpeed  = { 1.20f, 1.33f, 1.66f, 2.00f };
        private static readonly float[] BreakthroughOtherArrowSpeed = { 1.50f, 2.00f, 2.25f, 3.00f };
        private static readonly float[] RecoveryArrowSpeed          = { 1.20f, 1.33f, 1.66f, 2.00f };
        private static readonly float[] ReconArrowSpeed             = { 1.10f, 1.15f, 1.30f, 1.45f };
        private static readonly float[] BombardArrowSpeed           = { 1.10f, 1.20f, 1.40f, 1.60f };
        private static readonly float[] PlagueArrowSpeed            = { 1.20f, 1.33f, 1.66f, 2.00f };

        private void EnsureSilvaSeeds()
        {
            bool[] existingSlots = new bool[SeedOfSilvaFlowerProjectile.FlowerCount];

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != Player.whoAmI)
                    continue;

                if (projectile.ModProjectile is not SeedOfSilvaFlowerProjectile flower)
                    continue;

                int slot = flower.CurrentSlot;
                if (slot < 0 || slot >= existingSlots.Length || existingSlots[slot])
                {
                    projectile.Kill();
                    continue;
                }

                existingSlots[slot] = true;
            }

            for (int i = 0; i < existingSlots.Length; i++)
            {
                if (existingSlots[i])
                    continue;

                Projectile.NewProjectile(
                    Player.GetSource_FromThis(),
                    Player.Center,
                    Vector2.Zero,
                    SeedOfSilvaFlowerProjectile.GetFlowerType(i),
                    0,
                    0f,
                    Player.whoAmI);
            }
        }
    }
}
