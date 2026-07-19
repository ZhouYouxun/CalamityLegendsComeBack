using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityLegendsComeBack.Accssory.BF.FairyDance;
using CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using CalamityMod;
using CalamityMod.CalPlayer.Dashes;
using CalamityMod.Buffs.Cooldowns;
using CalamityMod.Cooldowns;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.Passive.PaRevo
{
    internal sealed class BFPassivePlayer : ModPlayer
    {
        public const int PassiveCooldownFrames = 180 * 60;
        public const int FinalStandDurationFrames = 5 * 60;
        public const int FinalStandRestoreLife = 100;

        private int finalStandBlockedHits;
        private BlossomFluxChloroplastPresetType finalStandPreset = BlossomFluxChloroplastPresetType.Chlo_ABreak;

        public int PassiveCooldownTimer;
        public int FinalStandTimer;

        // Do not depend on HoldItem's update order here. PreKill and cooldown charging both
        // need to recognise the weapon immediately on the frame it becomes held.
        private bool HoldingBlossomFluxNow => Player.HeldItem.type == ModContent.ItemType<NewLegendBlossomFlux>();
        private bool BadSeedActive => Player.GetModPlayer<BFAccessoryPlayer>().BadSeedEquipped;
        private bool SilvaHarpActive => Player.GetModPlayer<BFAccessoryPlayer>().SilvaHarpEquipped;
        public bool PassiveUnlocked => Main.hardMode;
        public bool FinalStandActive => FinalStandTimer > 0;
        public int PassiveChargeFramesRequired => BadSeedActive || SilvaHarpActive ? PassiveCooldownFrames / 2 : PassiveCooldownFrames;
        public bool PassiveReady => PassiveUnlocked && PassiveCooldownTimer >= PassiveChargeFramesRequired;
        public bool ShouldShowCooldownDisplay => HoldingBlossomFluxNow && PassiveUnlocked;

        public int DisplayFrames => FinalStandActive ? FinalStandTimer : PassiveCooldownTimer;
        public int ChargeSeconds => System.Math.Min(PassiveChargeFramesRequired / 60, PassiveCooldownTimer / 60);
        public int RequiredChargeSeconds => PassiveChargeFramesRequired / 60;
        public float PassiveChargeCompletion => PassiveChargeFramesRequired <= 0
            ? 1f
            : MathHelper.Clamp(PassiveCooldownTimer / (float)PassiveChargeFramesRequired, 0f, 1f);

        public override void UpdateDead()
        {
            FinalStandTimer = 0;
            finalStandBlockedHits = 0;
            finalStandPreset = BlossomFluxChloroplastPresetType.Chlo_ABreak;
            SyncPassiveDisplay();
        }

        public override void PostUpdate()
        {
            if (!PassiveUnlocked)
            {
                PassiveCooldownTimer = 0;
            }
            else if (HoldingBlossomFluxNow && (BadSeedActive || SilvaHarpActive || !AnyBossAlive()))
            {
                int requiredFrames = PassiveChargeFramesRequired;
                int previousTimer = PassiveCooldownTimer;
                PassiveCooldownTimer = System.Math.Min(requiredFrames, PassiveCooldownTimer + 1);
                if (previousTimer < requiredFrames && PassiveCooldownTimer >= requiredFrames && Player.whoAmI == Main.myPlayer && Player.active && !Player.dead)
                    SoundEngine.PlaySound(BlossomFluxSounds.PassivePlayerBuffSpawn, Player.Center);
            }

            SyncPassiveDisplay();
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            return;
        }

        public override void UpdateBadLifeRegen()
        {
            return;
        }

        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            if (!CanTriggerFinalStand())
                return true;

            if (CalamityReviveShouldGoFirst())
                return true;

            if (SilvaHarpActive)
                TriggerSilvaHarpRevival();
            else if (BadSeedActive)
                TriggerBadSeedRevival();
            else
                TriggerFinalStand();

            playSound = false;
            genGore = false;
            return false;
        }

        public void SyncPassiveDisplay()
        {
            if (!PassiveUnlocked && PassiveCooldownTimer <= 0 && !FinalStandActive)
            {
                if (Player.Calamity().cooldowns.TryGetValue(BFPassiveCoolDown.ID, out var hiddenCooldown))
                    hiddenCooldown.timeLeft = 0;

                return;
            }

            int displayFrames = ShouldShowCooldownDisplay ? System.Math.Max(1, DisplayFrames) : 0;
            if (Player.Calamity().cooldowns.TryGetValue(BFPassiveCoolDown.ID, out var cooldown))
            {
                cooldown.timeLeft = displayFrames;
            }
            else
            {
                Player.AddCooldown(BFPassiveCoolDown.ID, displayFrames);
            }
        }

        private bool CanTriggerFinalStand()
        {
            return Player.HeldItem.type == ModContent.ItemType<NewLegendBlossomFlux>() &&
                   PassiveUnlocked &&
                   PassiveReady;
        }

        private void TriggerFinalStand()
        {
            finalStandPreset = Player.GetModPlayer<BFRightUIPlayer>().CurrentPreset;
            FinalStandTimer = 0;
            PassiveCooldownTimer = 0;
            finalStandBlockedHits = 0;
            Player.HealPlayer(FinalStandRestoreLife);

            SpawnTriggerBurstFX(finalStandPreset);
            SpawnResolveSurvivalFX(finalStandPreset);

            if (Player.whoAmI == Main.myPlayer)
            {
                SoundEngine.PlaySound(BlossomFluxSounds.PassivePlayerSpecialAction1, Player.Center);
                SoundEngine.PlaySound(BlossomFluxSounds.PassivePlayerSpecialAction2, Player.Center);
            }
        }

        // 坏种保持原有效果：200 生命、枯萎、跳过蓄力的终结技，以及终结技持续时间减半。
        private void TriggerBadSeedRevival()
        {
            TriggerSpecialSeedRevival(200, 200, applyWithered: true, ultimateFlags: 3f);
        }

        // 竖琴继承坏种的正面分支：半冷却、首领战充能与立即发动终结技；
        // 舍弃防御减半、枯萎和终结技时长减半。
        private void TriggerSilvaHarpRevival()
        {
            TriggerSpecialSeedRevival(300, 300, applyWithered: false, ultimateFlags: 2f);
        }

        private void TriggerSpecialSeedRevival(int restoreLife, int healNotification, bool applyWithered, float ultimateFlags)
        {
            finalStandPreset = Player.GetModPlayer<BFRightUIPlayer>().CurrentPreset;
            FinalStandTimer = 0;
            PassiveCooldownTimer = 0;
            finalStandBlockedHits = 0;

            int restoredLife = System.Math.Min(Player.statLifeMax2, restoreLife);
            Player.statLife = System.Math.Max(1, restoredLife);
            Player.HealEffect(restoredLife, true);
            if (applyWithered)
                Player.AddBuff(ModContent.BuffType<Withered>(), 30 * 60);

            SpawnRecoveryField(finalStandPreset);
            SpawnTriggerBurstFX(finalStandPreset);
            Player.GetModPlayer<BFAccessoryPlayer>().NotifyLargeHeal(healNotification);
            SpawnSpecialSeedUltimate(ultimateFlags);

            if (Player.whoAmI == Main.myPlayer)
            {
                SoundEngine.PlaySound(BlossomFluxSounds.PassivePlayerSpecialAction1, Player.Center);
                SoundEngine.PlaySound(BlossomFluxSounds.PassivePlayerSpecialAction2, Player.Center);
            }
        }

        private void SpawnSpecialSeedUltimate(float flags)
        {
            if (Player.whoAmI != Main.myPlayer || Player.ownedProjectileCounts[ModContent.ProjectileType<BFEXWeapon>()] > 0)
                return;

            Vector2 direction = (Main.MouseWorld - Player.Center).SafeNormalize(Vector2.UnitX * Player.direction);
            Projectile.NewProjectile(
                Player.GetSource_FromThis(),
                Player.Center,
                direction,
                ModContent.ProjectileType<BFEXWeapon>(),
                Player.GetWeaponDamage(Player.HeldItem),
                Player.HeldItem.knockBack,
                Player.whoAmI,
                0f,
                flags);
        }

        internal void AccelerateCooldown(int frames)
        {
            if (frames <= 0 || FinalStandActive || PassiveCooldownTimer >= PassiveChargeFramesRequired)
                return;

            PassiveCooldownTimer = System.Math.Min(PassiveChargeFramesRequired, PassiveCooldownTimer + frames);
            SyncPassiveDisplay();
        }

        private void ResolveFinalStandSurvival()
        {
            int lifePenalty = (int)System.Math.Ceiling(Player.statLifeMax2 * 0.2f * finalStandBlockedHits);
            int targetLife = System.Math.Max(1, Player.statLifeMax2 - lifePenalty);
            if (Player.statLife < targetLife)
            {
                int healAmount = targetLife - Player.statLife;
                Player.statLife = targetLife;
                Player.HealEffect(healAmount, true);
                Player.GetModPlayer<BFAccessoryPlayer>().NotifyLargeHeal(healAmount);
            }
            else
            {
                Player.statLife = targetLife;
            }

            finalStandBlockedHits = 0;
            SpawnResolveSurvivalFX(finalStandPreset);

            if (Player.whoAmI == Main.myPlayer)
                SoundEngine.PlaySound(BlossomFluxSounds.PassivePlayerSpecialAction3, Player.Center);
        }

        private void NullifyFinalStandHit(ref Player.HurtInfo info)
        {
            info.Damage = 0;
        }

        private void ApplyFinalStandProtection()
        {
            Player.statLife = System.Math.Max(1, Player.statLife);
            Player.immuneNoBlink = true;
            Player.noFallDmg = true;
            Player.fireWalk = true;
            Player.lavaImmune = true;
            Player.lavaTime = Player.lavaMax;
            Player.breath = Player.breathMax;

            for (int i = Player.buffType.Length - 1; i >= 0; i--)
            {
                int buffType = Player.buffType[i];
                if (buffType <= 0 || buffType >= Main.debuff.Length || !Main.debuff[buffType])
                    continue;

                Player.DelBuff(i);
            }
        }

        private bool CalamityReviveShouldGoFirst()
        {
            var calamity = Player.Calamity();
            return calamity.nebulousCore && !Player.HasCooldown(NebulousCore.ID) ||
                calamity.DashID == GodslayerArmorDash.ID && Player.dashDelay < 0 ||
                calamity.silvaSet && calamity.silvaCountdown > 0 ||
                calamity.necroSet && calamity.necroReviveCounter == -1 ||
                calamity.permafrostsConcoction && !Player.HasCooldown(PermafrostConcoction.ID);
        }

        private static bool AnyBossAlive()
        {
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active || npc.friendly)
                    continue;

                if (npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc.type])
                    return true;
            }

            return false;
        }

        private void SpawnRecoveryField(BlossomFluxChloroplastPresetType preset)
        {
            if (Main.myPlayer != Player.whoAmI)
                return;

            int fieldType = ModContent.ProjectileType<BFPassiveRecoveryField>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.active && projectile.owner == Player.whoAmI && projectile.type == fieldType)
                    projectile.Kill();
            }

            Projectile.NewProjectile(
                Player.GetSource_FromThis(),
                Player.Center,
                Vector2.Zero,
                fieldType,
                0,
                0f,
                Player.whoAmI,
                (float)preset);
        }

        private void SpawnTriggerBurstFX(BlossomFluxChloroplastPresetType preset)
        {
            if (Main.dedServ)
                return;

            Color coreColor = BFArrowCommon.GetPresetColor(preset);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(preset);
            Vector2 center = Player.Center;

            GeneralParticleHandler.SpawnParticle(new StrongBloom(center, Vector2.Zero, Color.Lerp(coreColor, Color.White, 0.18f), 1.5f, 22));
            GeneralParticleHandler.SpawnParticle(new StrongBloom(center, Vector2.Zero, Color.White, 0.7f, 14));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero, Color.Lerp(coreColor, Color.White, 0.22f), new Vector2(1.2f, 1.2f), 0f, 0.28f, 0.038f, 20));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero, Color.Lerp(accentColor, Color.White, 0.3f), new Vector2(1.55f, 1.55f), MathHelper.PiOver4, 0.14f, 0.024f, 24));
            GeneralParticleHandler.SpawnParticle(new BloomLineVFX(center - Vector2.UnitY * 22f, Vector2.UnitY * 55f, 1.1f, Color.Lerp(coreColor, accentColor, 0.45f), 18));
            GeneralParticleHandler.SpawnParticle(new BloomLineVFX(center + Vector2.UnitY * 22f, -Vector2.UnitY * 55f, 1.1f, Color.Lerp(coreColor, accentColor, 0.45f), 18));

            for (int i = 0; i < 22; i++)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    center + Main.rand.NextVector2Circular(12f, 12f),
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.8f, 6.2f),
                    false,
                    Main.rand.Next(14, 24),
                    Main.rand.NextFloat(0.24f, 0.5f),
                    Color.Lerp(coreColor, Color.White, Main.rand.NextFloat(0.12f, 0.48f)),
                    true,
                    false,
                    true));
            }

            for (int i = 0; i < 8; i++)
            {
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    center + Main.rand.NextVector2Circular(20f, 20f),
                    Main.rand.NextVector2Circular(1.3f, 1.3f) + new Vector2(0f, Main.rand.NextFloat(-0.55f, 0.18f)),
                    Color.Lerp(coreColor, Color.White, 0.22f),
                    Color.Black,
                    Main.rand.NextFloat(0.5f, 0.82f),
                    Main.rand.Next(150, 210)));
            }

            for (int i = 0; i < 18; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    center,
                    Main.rand.NextBool(3) ? DustID.TerraBlade : DustID.GemEmerald,
                    Main.rand.NextVector2CircularEdge(3.8f, 3.8f) * Main.rand.NextFloat(1.8f, 5.2f),
                    90,
                    Color.Lerp(coreColor, Color.White, Main.rand.NextFloat(0.12f, 0.42f)),
                    Main.rand.NextFloat(1.0f, 1.65f));
                dust.noGravity = true;
            }
        }

        private void SpawnResolveSurvivalFX(BlossomFluxChloroplastPresetType preset)
        {
            if (Main.dedServ)
                return;

            Color coreColor = BFArrowCommon.GetPresetColor(preset);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(preset);
            Vector2 center = Player.Center;

            GeneralParticleHandler.SpawnParticle(new StrongBloom(center, Vector2.Zero, Color.Lerp(coreColor, Color.White, 0.25f), 1.1f, 16));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero, Color.Lerp(coreColor, Color.White, 0.28f), Vector2.One, 0f, 0.22f, 0.04f, 16));
            GeneralParticleHandler.SpawnParticle(new BloomLineVFX(center, -Vector2.UnitY * 48f, 0.92f, Color.Lerp(coreColor, accentColor, 0.52f), 14));
            GeneralParticleHandler.SpawnParticle(new BloomLineVFX(center, Vector2.UnitY * 48f, 0.92f, Color.Lerp(coreColor, accentColor, 0.52f), 14));

            for (int i = 0; i < 14; i++)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    center + Main.rand.NextVector2Circular(10f, 10f),
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.8f, 4.5f),
                    false,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.2f, 0.42f),
                    Color.Lerp(coreColor, Color.White, Main.rand.NextFloat(0.15f, 0.48f)),
                    true,
                    false,
                    true));
            }

            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    center,
                    Main.rand.NextBool(3) ? DustID.TerraBlade : DustID.GemEmerald,
                    Main.rand.NextVector2CircularEdge(3.0f, 3.0f) * Main.rand.NextFloat(1.5f, 4.2f),
                    90,
                    Color.Lerp(coreColor, Color.White, Main.rand.NextFloat(0.1f, 0.38f)),
                    Main.rand.NextFloat(1.0f, 1.45f));
                dust.noGravity = true;
            }
        }

        private void EmitFinalStandVisuals()
        {
            Color coreColor = BFArrowCommon.GetPresetColor(finalStandPreset);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(finalStandPreset);
            Lighting.AddLight(Player.Center, coreColor.ToVector3() * 0.34f);

            Vector2 bodyPoint = Player.Center + new Vector2(
                Main.rand.NextFloat(-Player.width * 0.42f, Player.width * 0.42f),
                Main.rand.NextFloat(-Player.height * 0.52f, Player.height * 0.28f));

            Vector2 riseVelocity = new Vector2(
                Main.rand.NextFloat(-0.55f, 0.55f),
                Main.rand.NextFloat(-2.8f, -1.3f));

            if (Main.rand.NextBool(2))
            {
                SquishyLightParticle lifeFlame = new(
                    bodyPoint,
                    riseVelocity.RotatedByRandom(0.42f),
                    Main.rand.NextFloat(0.42f, 0.7f),
                    Color.Lerp(coreColor, Color.White, Main.rand.NextFloat(0.12f, 0.3f)),
                    Main.rand.Next(12, 20),
                    1f,
                    Main.rand.NextFloat(1.3f, 2f));
                GeneralParticleHandler.SpawnParticle(lifeFlame);
            }

            if (Main.rand.NextBool(3))
            {
                GlowOrbParticle glowOrb = new(
                    bodyPoint + Main.rand.NextVector2Circular(6f, 8f),
                    riseVelocity * 0.12f,
                    false,
                    Main.rand.Next(9, 14),
                    Main.rand.NextFloat(0.28f, 0.45f),
                    Color.Lerp(accentColor, Color.White, Main.rand.NextFloat(0.18f, 0.36f)),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(glowOrb);
            }

            if (Main.rand.NextBool(2))
            {
                Dust glowDust = Dust.NewDustPerfect(
                    bodyPoint,
                    DustID.GemEmerald,
                    riseVelocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.45f, 1.2f),
                    100,
                    Color.Lerp(coreColor, accentColor, Main.rand.NextFloat(0.15f, 0.48f)),
                    Main.rand.NextFloat(0.95f, 1.35f));
                glowDust.noGravity = true;
            }
        }
    }
}
