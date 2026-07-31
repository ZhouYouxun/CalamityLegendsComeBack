using CalamityLegendsComeBack.Accssory.BF.SeedOfSilva;
using CalamityLegendsComeBack.Accssory.BF.FairyDanceSeries;
using CalamityLegendsComeBack.Accssory.BF.SilvaHarp;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.Passive.PaRevo;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using Microsoft.Xna.Framework;
using CalamityMod.Projectiles.Healing;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.Common
{
    internal sealed class BFAccessoryPlayer : ModPlayer
    {
        public const float BadSeedRangedDamageBonus = 0.20f;
        public const float BadSeedRangedCritBonus = 10f;
        public const float BadSeedArrowSpeedBonus = 0.15f;
        public const float BadSeedBlossomFluxDamageBonus = 0.05f;
        public const float BadSeedBlossomFluxCritBonus = 8f;

        public int QuiverTier;
        public bool DominationQuiverEquipped;
        public bool SeedOfSilvaEquipped;
        public bool SilvaHarpEquipped;
        public bool PastLingeringEquipped;
        public bool FairyDanceEquipped;
        public bool RainbowSpiritDanceEquipped;
        public bool BadSeedEquipped;

        private int fairyAttackTimer = -1;
        private int fairyRespawnTimer;
        private int blossomFluxHitCounter;
        private int nextLacewingSlot;
        private int lifeAtFrameStart;
        private bool largeHealHandledThisFrame;
        private int silvaRecoveryAuraIndex = -1;
        private int silvaRecoveryAuraFollowTime;

        // ── 箭袋强化字段（每帧由 EquipQuiver 写入，ResetEffects 清零）──
        public float ChargeMultiplier = 1f;           // 所有战术右键蓄力时间乘数
        public int RecoveryExtraFlashes;              // 复苏右键额外治疗闪光数
        public int ReconExtraMarkFrames;              // 侦察右键额外标记持续帧数
        public bool BombardExplosionBonus;            // 歼灭右键爆炸范围提升（同调/主宰）
        public bool BombardRainDamageBonus;           // 歼灭右键弹幕伤害提升（共鸣/主宰）
        public int PlagueAdvancedTier;                // 侵染高级 Debuff 等级：0=无，1=第一批，2=全部

        public bool ArrowConversionEquipped;

        public bool HoldingBlossomFlux => Player.HeldItem?.type == ModContent.ItemType<NewLegendBlossomFlux>();
        public float BadSeedAttributeMultiplier => BadSeedEquipped ? 1f : 0f;
        public bool HasBadSeedAttributes => BadSeedAttributeMultiplier > 0f;

        public BlossomFluxChloroplastPresetType CurrentPreset =>
            HoldingBlossomFlux ? Player.GetModPlayer<BFRightUIPlayer>().CurrentPreset : BlossomFluxChloroplastPresetType.Chlo_BRecov;

        public override void ResetEffects()
        {
            QuiverTier = 0;
            DominationQuiverEquipped = false;
            SeedOfSilvaEquipped = false;
            SilvaHarpEquipped = false;
            PastLingeringEquipped = false;
            FairyDanceEquipped = false;
            RainbowSpiritDanceEquipped = false;
            BadSeedEquipped = false;
            ArrowConversionEquipped = false;

            ChargeMultiplier = 1f;
            RecoveryExtraFlashes = 0;
            ReconExtraMarkFrames = 0;
            BombardExplosionBonus = false;
            BombardRainDamageBonus = false;
            PlagueAdvancedTier = 0;
        }

        public override void UpdateDead()
        {
            ResetEffects();
            fairyAttackTimer = -1;
            fairyRespawnTimer = 0;
            blossomFluxHitCounter = 0;
            nextLacewingSlot = 0;
            silvaRecoveryAuraIndex = -1;
            silvaRecoveryAuraFollowTime = 0;
        }

        public override void PreUpdate()
        {
            lifeAtFrameStart = Player.statLife;
            largeHealHandledThisFrame = false;
        }

        public override void PostUpdateEquips()
        {
            if (BadSeedEquipped)
                Player.statDefense *= 0.5f;
        }

        public override void PostUpdate()
        {
            if (SeedOfSilvaEquipped && Player.whoAmI == Main.myPlayer)
                EnsureSilvaSeeds();

            if (SilvaHarpEquipped && Player.whoAmI == Main.myPlayer)
                EnsureSilvaHarpNotes();

            // 妖精舞：维持并驱动三只仙灵。虹灵舞不再继承这些仙灵，因此单独判断。
            if (FairyDanceEquipped)
            {
                if (Player.whoAmI == Main.myPlayer)
                {
                    EnsureFairies();
                    HandleFairyDashInput();
                }
            }
            else
            {
                fairyAttackTimer = -1;
            }

            // 虹灵舞：只保留自己的七彩草蛉与大回复机制，不依赖妖精舞的仙灵是否装备。
            if (RainbowSpiritDanceEquipped)
            {
                if (!largeHealHandledThisFrame && Player.statLife - lifeAtFrameStart >= 200)
                    HandleLargeHeal();
            }
            else
            {
                blossomFluxHitCounter = 0;
            }
        }

        internal void UpdateSilvaRecoveryAura()
        {
            if (!SilvaHarpEquipped || Player.whoAmI != Main.myPlayer)
                return;

            Projectile aura = GetSilvaRecoveryAura();
            if (aura is null)
            {
                silvaRecoveryAuraIndex = Projectile.NewProjectile(
                    Player.GetSource_FromThis(),
                    Player.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<AbsorberAura>(),
                    0,
                    0f,
                    Player.whoAmI,
                    0f,
                    0f,
                    SilvaRecoveryAuraTag);
                silvaRecoveryAuraFollowTime = 0;
                aura = GetSilvaRecoveryAura();
            }

            if (aura is null || silvaRecoveryAuraFollowTime >= 60)
                return;

            aura.Center = Player.Center;
            aura.velocity = Vector2.Zero;
            if (++silvaRecoveryAuraFollowTime == 60 || silvaRecoveryAuraFollowTime % 15 == 0)
                aura.netUpdate = true;
        }

        private const float SilvaRecoveryAuraTag = 7391f;

        private Projectile GetSilvaRecoveryAura()
        {
            if (BFArrowCommon.InBounds(silvaRecoveryAuraIndex, Main.maxProjectiles))
            {
                Projectile cached = Main.projectile[silvaRecoveryAuraIndex];
                if (cached.active && cached.owner == Player.whoAmI && cached.type == ModContent.ProjectileType<AbsorberAura>() && cached.ai[2] == SilvaRecoveryAuraTag)
                    return cached;
            }

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner == Player.whoAmI && projectile.type == ModContent.ProjectileType<AbsorberAura>() && projectile.ai[2] == SilvaRecoveryAuraTag)
                {
                    silvaRecoveryAuraIndex = projectile.whoAmI;
                    return projectile;
                }
            }

            silvaRecoveryAuraIndex = -1;
            silvaRecoveryAuraFollowTime = 0;
            return null;
        }

        private void EnsureSilvaHarpNotes()
        {
            bool[] existingSlots = new bool[SilvaHarpNote.NoteCount];
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != Player.whoAmI || projectile.ModProjectile is not SilvaHarpNote note)
                    continue;

                int slot = note.Slot;
                if (slot < 0 || slot >= existingSlots.Length || existingSlots[slot])
                {
                    projectile.Kill();
                    continue;
                }

                existingSlots[slot] = true;
            }

            for (int i = 0; i < existingSlots.Length; i++)
            {
                if (!existingSlots[i])
                    Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, ModContent.ProjectileType<SilvaHarpNote>(), 0, 0f, Player.whoAmI, i);
            }
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
            float multiplier = 1f;
            if (QuiverTier > 0)
            {
                int tierIndex = Utils.Clamp(QuiverTier - 1, 0, 3);
                multiplier = preset switch
                {
                    BlossomFluxChloroplastPresetType.Chlo_ABreak => convertedLeafArrow ? BreakthroughWoodArrowSpeed[tierIndex] : BreakthroughOtherArrowSpeed[tierIndex],
                    BlossomFluxChloroplastPresetType.Chlo_BRecov => RecoveryArrowSpeed[tierIndex],
                    BlossomFluxChloroplastPresetType.Chlo_CDetec => ReconArrowSpeed[tierIndex],
                    BlossomFluxChloroplastPresetType.Chlo_DBomb => BombardArrowSpeed[tierIndex],
                    BlossomFluxChloroplastPresetType.Chlo_EPlague => PlagueArrowSpeed[tierIndex],
                    _ => 1f
                };
            }

            return multiplier * (1f + BadSeedArrowSpeedBonus * BadSeedAttributeMultiplier);
        }

        // 箭袋速度加成：备用 / 同调 / 共鸣 / 主宰
        private static readonly float[] BreakthroughWoodArrowSpeed  = { 1.20f, 1.33f, 1.66f, 2.00f };
        private static readonly float[] BreakthroughOtherArrowSpeed = { 1.50f, 2.00f, 2.25f, 3.00f };
        private static readonly float[] RecoveryArrowSpeed          = { 1.20f, 1.33f, 1.66f, 2.00f };
        private static readonly float[] ReconArrowSpeed             = { 1.10f, 1.15f, 1.30f, 1.45f };
        private static readonly float[] BombardArrowSpeed           = { 1.10f, 1.20f, 1.40f, 1.60f };
        private static readonly float[] PlagueArrowSpeed            = { 1.20f, 1.33f, 1.66f, 2.00f };

        internal void TriggerFairyRightAttack()
        {
            if (!FairyDanceEquipped || Player.whoAmI != Main.myPlayer)
                return;

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner == Player.whoAmI && projectile.ModProjectile is FairyDanceWisp fairy)
                    fairy.TriggerDash(Main.MouseWorld, 120);
            }
        }

        internal void RegisterBlossomFluxHit()
        {
            if (!RainbowSpiritDanceEquipped || !HoldingBlossomFlux || Player.whoAmI != Main.myPlayer)
                return;

            blossomFluxHitCounter++;
            if (blossomFluxHitCounter < 60)
                return;

            blossomFluxHitCounter = 0;
            SpawnLacewing();
        }

        internal void NotifyLargeHeal(int healAmount)
        {
            if (healAmount < 200 || largeHealHandledThisFrame)
                return;

            largeHealHandledThisFrame = true;
            HandleLargeHeal();
        }

        private void EnsureFairies()
        {
            bool[] existingSlots = new bool[FairyDanceWisp.FairyCount];
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != Player.whoAmI || projectile.ModProjectile is not FairyDanceWisp)
                    continue;

                int slot = Utils.Clamp((int)projectile.ai[0], 0, existingSlots.Length - 1);
                if (existingSlots[slot])
                {
                    projectile.Kill();
                    continue;
                }

                existingSlots[slot] = true;
            }

            if (fairyRespawnTimer > 0)
            {
                fairyRespawnTimer--;
                return;
            }

            for (int i = 0; i < existingSlots.Length; i++)
            {
                if (existingSlots[i])
                    continue;

                Projectile.NewProjectile(
                    Player.GetSource_FromThis(),
                    Player.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<FairyDanceWisp>(),
                    0,
                    0f,
                    Player.whoAmI,
                    i);
            }
        }

        private void HandleFairyDashInput()
        {
            bool validInput = HoldingBlossomFlux && !Player.noItems && !Player.CCed && !Player.mouseInterface && !Main.blockMouse;
            bool leftHeld = validInput && Main.mouseLeft;
            bool rightHeld = validInput && Player.GetModPlayer<BFRightUIPlayer>().RightMouseHeld;
            if (!leftHeld && !rightHeld)
            {
                fairyAttackTimer = -1;
                return;
            }

            fairyAttackTimer++;
            // 三只仙灵各自错开 1/3 周期的攻击节拍：一只在突进时，另两只处在不同的突进/复位阶段，
            // 攻击时间明显错开；突进结束后不回到玩家身边，而是从半途直接再突进，形成连续的“舞”。
            int cadence = FairyDanceWisp.AttackCadence;
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != Player.whoAmI || projectile.ModProjectile is not FairyDanceWisp fairy)
                    continue;

                int variant = Utils.Clamp((int)projectile.ai[0], 0, FairyDanceWisp.FairyCount - 1);
                int phaseOffset = variant * cadence / FairyDanceWisp.FairyCount;
                int localTick = fairyAttackTimer - phaseOffset;
                if (localTick >= 0 && localTick % cadence == 0)
                    fairy.TriggerDash(Main.MouseWorld, rightHeld ? 30 : 20);
            }
        }

        private void SpawnLacewing()
        {
            int type = ModContent.ProjectileType<RainbowSpiritLacewing>();
            int count = 0;
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner == Player.whoAmI && projectile.type == type)
                    count++;
            }

            if (count >= RainbowSpiritLacewing.MaximumCount)
                return;

            Projectile.NewProjectile(
                Player.GetSource_FromThis(),
                Player.Center,
                Vector2.Zero,
                type,
                0,
                0f,
                Player.whoAmI,
                nextLacewingSlot++ % RainbowSpiritLacewing.MaximumCount);
        }

        private void HandleLargeHeal()
        {
            if (!RainbowSpiritDanceEquipped || !ConsumeFairy())
                return;

            int oldLife = Player.statLife;
            Player.statLife = System.Math.Min(Player.statLifeMax2, Player.statLife + 100);
            int actualHeal = Player.statLife - oldLife;
            if (actualHeal > 0)
                Player.HealEffect(actualHeal, true);

            Player.GetModPlayer<BFPassivePlayer>().AccelerateCooldown(60 * 60);
        }

        private bool ConsumeFairy()
        {
            // 虹灵舞不再有妖精舞的仙灵，大回复改为消耗自己的一只七彩草蛉。
            int lacewingType = ModContent.ProjectileType<RainbowSpiritLacewing>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != Player.whoAmI || projectile.type != lacewingType)
                    continue;

                projectile.Kill();
                return true;
            }

            return false;
        }

        private void EnsureSilvaSeeds()
        {
            // 所有花随当前战术转化为同一种：手持武器时取当前战术对应花并盛开，未手持武器时按默认战术显示为 4 颗种子。
            int desiredType = SeedOfSilvaFlowerProjectile.GetFlowerTypeForPreset(CurrentPreset);
            bool[] existingSlots = new bool[SeedOfSilvaFlowerProjectile.FlowerCount];

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != Player.whoAmI)
                    continue;

                if (projectile.ModProjectile is not SeedOfSilvaFlowerProjectile flower)
                    continue;

                // 类型不符（战术切换后残留的旧花）或槽位重复/越界都直接清掉，下面会按当前战术重新生成。
                int slot = flower.CurrentSlot;
                if (projectile.type != desiredType || slot < 0 || slot >= existingSlots.Length || existingSlots[slot])
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
                    desiredType,
                    0,
                    0f,
                    Player.whoAmI,
                    i); // ai[0] = 环绕槽位
            }
        }
    }
}
