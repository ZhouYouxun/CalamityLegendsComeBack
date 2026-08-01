using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.LeftClick
{
    internal class BFRecoveryLeafBuff : ModBuff
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/贴图/复苏之叶";

        public override LocalizedText Description
        {
            get
            {
                BFRecoveryLeftStats stats = BFRecoveryLeftBalance.GetStats();
                return base.Description.WithFormatArgs(stats.LifeRegen / 2, stats.Defense);
            }
        }

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }
    }

    internal sealed class BFRecoveryEcologyPlayer : ModPlayer
    {
        private const int FlashChanceDenominator = 5;

        private int leafTimeLeft;
        private int flashCooldown;
        private int flashWindowTimer;
        private int flashesInWindow;
        private float recoveryChargeDamageReduction;

        public int LeafTimeLeft => leafTimeLeft;

        public void SetRecoveryChargeDamageReduction(float damageReduction)
        {
            recoveryChargeDamageReduction = MathHelper.Clamp(damageReduction, 0f, 0.95f);
        }

        public void AddRecoveryLeaf(int timeToAdd)
        {
            BFRecoveryLeftStats stats = BFRecoveryLeftBalance.GetStats();
            leafTimeLeft = Utils.Clamp(leafTimeLeft + timeToAdd, 1, stats.LeafMaxTime);
            Player.AddBuff(ModContent.BuffType<BFRecoveryLeafBuff>(), leafTimeLeft);
        }

        public void TrySpawnRecoveryTransfer(Vector2 sourcePosition, bool markedTarget)
        {
            if (!Main.rand.NextBool(FlashChanceDenominator))
                return;

            BFRecoveryLeftStats stats = BFRecoveryLeftBalance.GetStats();
            int cooldownFrames = markedTarget ? stats.MarkedFlashCooldownFrames : GetMultiplayerFlashCooldown(stats.FlashCooldownFrames);
            bool windowLimited = !markedTarget && Main.netMode == NetmodeID.SinglePlayer;

            if (flashCooldown > 0)
                return;

            if (flashWindowTimer <= 0)
            {
                flashWindowTimer = stats.FlashWindowFrames;
                flashesInWindow = 0;
            }

            if (windowLimited && flashesInWindow >= stats.FlashWindowLimit)
                return;

            flashCooldown = cooldownFrames;
            flashesInWindow++;

            if (Player.whoAmI != Main.myPlayer)
                return;

            Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.4f, 5.8f);
            BFArrow_BRecovTransfer.Spawn(
                Player.GetSource_Misc("BlossomFluxRecoveryTransfer"),
                sourcePosition + Main.rand.NextVector2Circular(8f, 8f),
                velocity,
                Player.whoAmI,
                stats.FlashHealAmount,
                BFArrow_BRecovTransfer.LeftHitSpawnMode);
        }

        public override void ResetEffects()
        {
            recoveryChargeDamageReduction = 0f;
        }

        public override void PostUpdate()
        {
            if (flashCooldown > 0)
                flashCooldown--;

            if (flashWindowTimer > 0)
            {
                flashWindowTimer--;
                if (flashWindowTimer <= 0)
                    flashesInWindow = 0;
            }

            if (leafTimeLeft > 0)
                leafTimeLeft--;
        }

        public override void PostUpdateEquips()
        {
            if (leafTimeLeft <= 0)
                return;

            BFRecoveryLeftStats stats = BFRecoveryLeftBalance.GetStats();
            bool fullRecoveryForm = IsHoldingRecoveryMode();
            Player.statDefense += fullRecoveryForm ? stats.Defense : stats.Defense / 2;
            Player.endurance += fullRecoveryForm ? stats.DamageReduction : stats.DamageReduction * 0.5f;

            if (fullRecoveryForm)
            {
                ApplyRecoveryLeafImmunities(stats);
                EmitRecoveryParticles();
            }

            int buffType = ModContent.BuffType<BFRecoveryLeafBuff>();
            int buffIndex = Player.FindBuffIndex(buffType);
            if (buffIndex >= 0)
                Player.buffTime[buffIndex] = leafTimeLeft;
            else
                Player.AddBuff(buffType, leafTimeLeft);
        }

        public override void UpdateLifeRegen()
        {
            if (leafTimeLeft <= 0 || !IsHoldingRecoveryMode())
                return;

            BFRecoveryLeftStats stats = BFRecoveryLeftBalance.GetStats();
            if (stats.DebuffDamageMultiplier < 1f && Player.lifeRegen < 0)
                Player.lifeRegen = (int)(Player.lifeRegen * stats.DebuffDamageMultiplier);

            Player.lifeRegen += stats.LifeRegen + GetMissingHealthRegenBonus(stats);
            Player.lifeRegenTime += stats.RegenTimePerTick;

            if (stats.HealthThresholdRegenTime && Player.statLifeMax2 > 0)
            {
                float lifeRatio = Player.statLife / (float)Player.statLifeMax2;
                if (lifeRatio <= 0.25f)
                    Player.lifeRegenTime = System.Math.Max(Player.lifeRegenTime, 3600);
                else if (lifeRatio <= 0.5f)
                    Player.lifeRegenTime = System.Math.Max(Player.lifeRegenTime, 1800);
                else if (lifeRatio <= 0.75f)
                    Player.lifeRegenTime = System.Math.Max(Player.lifeRegenTime, 900);
            }
        }

        public override void NaturalLifeRegen(ref float regen)
        {
            if (leafTimeLeft <= 0 || !IsHoldingRecoveryMode())
                return;

            BFRecoveryLeftStats stats = BFRecoveryLeftBalance.GetStats();
            if (!stats.MovingRegenIgnoresPenalty)
                return;

            // 原版在玩家横向移动且未抓钩时先把自然回血乘以 0.5，
            // NaturalLifeRegen 钩子位于这一步之后；乘回 2 正好只抵消移动惩罚，
            // 不再错误改动 runSlowdown，也就不会把玩家的横向速度瞬间刹掉。
            if (Player.velocity.X != 0f && Player.grappling[0] <= 0)
                regen *= 2f;
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (recoveryChargeDamageReduction > 0f)
                modifiers.FinalDamage *= 1f - recoveryChargeDamageReduction;
        }

        private int GetMissingHealthRegenBonus(BFRecoveryLeftStats stats)
        {
            if (stats.LifeRegenPerMissingQuarter <= 0 || Player.statLifeMax2 <= 0)
                return 0;

            float lifeRatio = MathHelper.Clamp(Player.statLife / (float)Player.statLifeMax2, 0f, 1f);
            int missingQuarters = Utils.Clamp((int)System.Math.Ceiling((1f - lifeRatio) * 4f), 0, 4);
            return missingQuarters * stats.LifeRegenPerMissingQuarter;
        }

        private static int GetMultiplayerFlashCooldown(int baseCooldown)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return baseCooldown;

            int activePlayers = 0;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead)
                    activePlayers++;
            }

            int reduction = System.Math.Max(0, activePlayers - 1) * 30;
            return Utils.Clamp(baseCooldown - reduction, 60, baseCooldown);
        }

        private void ApplyRecoveryLeafImmunities(BFRecoveryLeftStats stats)
        {
            if (stats.ImmunePoisonAndFire)
            {
                Player.buffImmune[BuffID.Poisoned] = true;
                Player.buffImmune[BuffID.OnFire] = true;
                Player.buffImmune[BuffID.OnFire3] = true;
            }

            if (stats.ImmuneAcidVenom)
            {
                Player.buffImmune[BuffID.Venom] = true;
                TrySetCalamityBuffImmune("AcidVenom");
            }

            if (stats.ImmunePlague)
                TrySetCalamityBuffImmune<Plague>();

            if (!stats.ImmuneMostPreDragonDebuffs)
                return;

            Player.buffImmune[BuffID.Chilled] = true;
            Player.buffImmune[BuffID.Frostburn] = true;
            Player.buffImmune[BuffID.Frostburn2] = true;
            Player.buffImmune[BuffID.Electrified] = true;
            Player.buffImmune[BuffID.Burning] = true;
            Player.buffImmune[BuffID.CursedInferno] = true;
            Player.buffImmune[BuffID.Daybreak] = true;
            Player.buffImmune[BuffID.OnFire] = true;
            Player.buffImmune[BuffID.OnFire3] = true;
            Player.buffImmune[BuffID.ShadowFlame] = true;
            Player.buffImmune[BuffID.Poisoned] = true;
            Player.buffImmune[BuffID.Venom] = true;

            TrySetCalamityBuffImmune("AbsorberAffliction");
            TrySetCalamityBuffImmune("AcidVenom");
            TrySetCalamityBuffImmune<AstralInfectionDebuff>();
            TrySetCalamityBuffImmune<BrainRot>();
            TrySetCalamityBuffImmune<BrimstoneFlames>();
            TrySetCalamityBuffImmune<BurningBlood>();
            TrySetCalamityBuffImmune<CrushDepth>();
            TrySetCalamityBuffImmune<Dragonfire>();
            TrySetCalamityBuffImmune("Eutrophication");
            TrySetCalamityBuffImmune("GalvanicCorrosion");
            TrySetCalamityBuffImmune("GlacialState");
            TrySetCalamityBuffImmune<GodSlayerInferno>();
            TrySetCalamityBuffImmune<HadopelagicPressure>();
            TrySetCalamityBuffImmune<HolyFlames>();
            TrySetCalamityBuffImmune<Nightwither>();
            TrySetCalamityBuffImmune<Plague>();
            TrySetCalamityBuffImmune<RiptideDebuff>();
            TrySetCalamityBuffImmune<SagePoison>();
            TrySetCalamityBuffImmune<StaticDischarge>();
            TrySetCalamityBuffImmune<SulphuricPoisoning>();
            TrySetCalamityBuffImmune<VulnerabilityHex>();
            TrySetCalamityBuffImmune<WeakBrimstoneFlames>();
            TrySetCalamityBuffImmune("WhisperingDeath");
        }

        private void TrySetCalamityBuffImmune<T>() where T : ModBuff
        {
            int type = ModContent.BuffType<T>();
            if (type > 0 && type < Player.buffImmune.Length)
                Player.buffImmune[type] = true;
        }

        private void TrySetCalamityBuffImmune(string internalName)
        {
            if (!ModContent.TryFind("CalamityMod/" + internalName, out ModBuff buff))
                return;

            int type = buff.Type;
            if (type > 0 && type < Player.buffImmune.Length)
                Player.buffImmune[type] = true;
        }

        private void RemoveOldestOwnedRecoveryTransfer()
        {
            int flashType = ModContent.ProjectileType<BFArrow_BRecovTransfer>();
            Projectile oldestFlash = null;
            int lowestTimeLeft = int.MaxValue;

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != Player.whoAmI || projectile.type != flashType)
                    continue;

                if (projectile.timeLeft >= lowestTimeLeft)
                    continue;

                lowestTimeLeft = projectile.timeLeft;
                oldestFlash = projectile;
            }

            oldestFlash?.Kill();
        }

        private bool IsHoldingRecoveryMode()
        {
            return Player.active &&
                !Player.dead &&
                Player.HeldItem.type == ModContent.ItemType<NewLegendBlossomFlux>() &&
                Player.GetModPlayer<BFRightUIPlayer>().CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_BRecov;
        }

        private void EmitRecoveryParticles()
        {
            if (!IsHoldingRecoveryMode() || Main.dedServ || Main.rand.NextBool(3))
                return;

            Vector2 spawnPosition = Player.Center + Main.rand.NextVector2Circular(18f, 30f);
            Dust dust = Dust.NewDustPerfect(
                spawnPosition,
                Main.rand.NextBool(3) ? DustID.TerraBlade : DustID.GemEmerald,
                new Vector2(Main.rand.NextFloat(-0.35f, 0.35f), Main.rand.NextFloat(-5.8f, -3.2f)),
                100,
                Color.Lerp(new Color(108, 255, 142), Color.White, Main.rand.NextFloat(0.05f, 0.28f)),
                Main.rand.NextFloat(0.85f, 1.28f));
            dust.noGravity = true;
            dust.fadeIn = Main.rand.NextFloat(0.4f, 0.75f);

            Lighting.AddLight(Player.Center, new Vector3(0.08f, 0.34f, 0.12f));
        }
    }
}
