using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
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
        private const int LeafTimePerFlash = 5 * 60;
        private const int FlashChanceDenominator = 5;

        private readonly BalanceBlossomFlux balance = new();
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
            BalanceBlossomFlux.RecoveryLeafStats stats = balance.GetRecoveryLeafStats();
            leafTimeLeft = Utils.Clamp(leafTimeLeft + timeToAdd, 1, stats.LeafMaxTime);
            Player.AddBuff(ModContent.BuffType<BFRecoveryLeafBuff>(), leafTimeLeft);
        }

        public void TrySpawnRecoveryFlash(Vector2 sourcePosition, bool markedTarget)
        {
            if (!Main.rand.NextBool(FlashChanceDenominator))
                return;

            BalanceBlossomFlux.RecoveryLeafStats stats = balance.GetRecoveryLeafStats();
            bool multiplayer = Main.netMode != NetmodeID.SinglePlayer || CountActivePlayers() > 1;
            int cooldownFrames = markedTarget ? 75 : GetMultiplayerAdjustedCooldown(stats.FlashCooldownFrames);
            bool windowLimited = !multiplayer && !markedTarget;

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

            Projectile.NewProjectile(
                Player.GetSource_Misc("BlossomFluxRecoveryFlash"),
                sourcePosition,
                -Vector2.UnitY.RotatedByRandom(0.55f) * Main.rand.NextFloat(1.6f, 3.2f),
                ModContent.ProjectileType<BFRecoveryFlash>(),
                0,
                0f,
                Player.whoAmI,
                stats.FlashHealAmount,
                0f);
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

            BalanceBlossomFlux.RecoveryLeafStats stats = balance.GetRecoveryLeafStats();
            bool fullRecoveryForm = IsHoldingRecoveryMode();
            float halfFactor = fullRecoveryForm ? 1f : 0.5f;

            Player.statDefense += (int)(stats.Defense * halfFactor);
            Player.endurance += stats.DamageReduction * halfFactor;

            if (fullRecoveryForm)
            {
                ApplyRecoveryImmunities(stats);
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

            BalanceBlossomFlux.RecoveryLeafStats stats = balance.GetRecoveryLeafStats();
            Player.lifeRegen += stats.LifeRegen;
            Player.lifeRegenTime += stats.RegenTimePerTick;

            if (stats.MovingRegenBoost && Player.velocity.LengthSquared() > 0.25f)
                Player.lifeRegen += System.Math.Max(1, stats.LifeRegen / 2);

            ApplyHealthBasedRegenFloor(stats);

            if (stats.DebuffDamageReduction > 0f && Player.lifeRegen < 0)
                Player.lifeRegen = (int)(Player.lifeRegen * (1f - stats.DebuffDamageReduction));
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (recoveryChargeDamageReduction > 0f)
                modifiers.FinalDamage *= 1f - recoveryChargeDamageReduction;
        }

        private bool IsHoldingRecoveryMode()
        {
            return Player.active &&
                !Player.dead &&
                Player.HeldItem.type == ModContent.ItemType<NewLegendBlossomFlux>() &&
                Player.GetModPlayer<BFRightUIPlayer>().CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_BRecov;
        }

        private static int CountActivePlayers()
        {
            int count = 0;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead)
                    count++;
            }

            return System.Math.Max(1, count);
        }

        private static int GetMultiplayerAdjustedCooldown(int baseCooldown)
        {
            int activePlayers = CountActivePlayers();
            return System.Math.Max(60, baseCooldown - (activePlayers - 1) * 30);
        }

        private void ApplyHealthBasedRegenFloor(BalanceBlossomFlux.RecoveryLeafStats stats)
        {
            if (!stats.MoonLordHealthRegenFloor && !stats.YharonHealthRegenFloor)
                return;

            float lifeRatio = Player.statLifeMax2 <= 0 ? 1f : Player.statLife / (float)Player.statLifeMax2;
            if (lifeRatio <= 0.25f)
                Player.lifeRegenTime = System.Math.Max(Player.lifeRegenTime, stats.YharonHealthRegenFloor ? 4800 : 3600);
            else if (lifeRatio <= 0.5f)
                Player.lifeRegenTime = System.Math.Max(Player.lifeRegenTime, stats.YharonHealthRegenFloor ? 2400 : 1800);
            else if (lifeRatio <= 0.75f)
                Player.lifeRegenTime = System.Math.Max(Player.lifeRegenTime, stats.YharonHealthRegenFloor ? 1200 : 900);
        }

        private void ApplyRecoveryImmunities(BalanceBlossomFlux.RecoveryLeafStats stats)
        {
            if (stats.ImmunePoisonAndFire)
            {
                Player.buffImmune[BuffID.Poisoned] = true;
                Player.buffImmune[BuffID.OnFire] = true;
            }

            if (stats.ImmuneVenom)
                Player.buffImmune[BuffID.Venom] = true;

            if (stats.ImmunePlague)
                Player.buffImmune[ModContent.BuffType<Plague>()] = true;

            if (!stats.BroadDebuffImmunity)
                return;

            Player.buffImmune[BuffID.Bleeding] = true;
            Player.buffImmune[BuffID.Burning] = true;
            Player.buffImmune[BuffID.CursedInferno] = true;
            Player.buffImmune[BuffID.Frostburn] = true;
            Player.buffImmune[BuffID.Ichor] = true;
            Player.buffImmune[BuffID.OnFire] = true;
            Player.buffImmune[BuffID.Poisoned] = true;
            Player.buffImmune[BuffID.ShadowFlame] = true;
            Player.buffImmune[BuffID.Venom] = true;
            Player.buffImmune[ModContent.BuffType<AstralInfectionDebuff>()] = true;
            Player.buffImmune[ModContent.BuffType<Dragonfire>()] = true;
            Player.buffImmune[ModContent.BuffType<Irradiated>()] = true;
            Player.buffImmune[ModContent.BuffType<Plague>()] = true;
            Player.buffImmune[ModContent.BuffType<WhisperingDeath>()] = true;
            Player.buffImmune[ModContent.BuffType<WitherDebuff>()] = true;
        }

        private void EmitRecoveryParticles()
        {
            if (Main.dedServ || Main.rand.NextBool(3))
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
