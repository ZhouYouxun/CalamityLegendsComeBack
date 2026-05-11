using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.LeafProj
{
    internal class BFRecoveryLeafBuff : ModBuff
    {
        public override string Texture => "Terraria/Images/Projectile_0";

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
            int cooldownFrames = markedTarget ? stats.MarkedFlashCooldownFrames : stats.FlashCooldownFrames;
            bool windowLimited = !markedTarget;

            if (flashCooldown > 0)
                return;

            if (flashWindowTimer <= 0)
            {
                flashWindowTimer = stats.FlashWindowFrames;
                flashesInWindow = 0;
            }

            if (windowLimited && flashesInWindow >= stats.FlashWindowLimit)
            {
                RemoveOldestOwnedRecoveryTransfer();
                flashesInWindow = System.Math.Max(0, stats.FlashWindowLimit - 1);
            }

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
            Player.statDefense += stats.Defense;
            Player.endurance += stats.DamageReduction;
            EmitRecoveryParticles();

            int buffType = ModContent.BuffType<BFRecoveryLeafBuff>();
            int buffIndex = Player.FindBuffIndex(buffType);
            if (buffIndex >= 0)
                Player.buffTime[buffIndex] = leafTimeLeft;
            else
                Player.AddBuff(buffType, leafTimeLeft);
        }

        public override void UpdateLifeRegen()
        {
            if (leafTimeLeft <= 0)
                return;

            BFRecoveryLeftStats stats = BFRecoveryLeftBalance.GetStats();
            Player.lifeRegen += stats.LifeRegen + GetMissingHealthRegenBonus(stats);
            Player.lifeRegenTime += stats.RegenTimePerTick;
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
