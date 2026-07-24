using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;


namespace CalamityLegendsComeBack.Weapons.GaelsGreatsword
{
    internal sealed class GaelGreatswordPlayer : ModPlayer
    {
        public const int DarkEmberMax = 100;
        public const int GuardCooldownMax = 5 * 60;
        public const int ParryWindowFrames = 14;
        private const int LeftComboLength = 6;

        public int FollowupSlashWindow;
        public int FinisherCooldown;
        public int DarkEmbers;
        public int DarkEmberFlashTimer;
        public int GuardCooldown;
        public int GuardFlashTimer;
        public int ParryFlashTimer;

        private int holdingTimer;
        private int guardActiveTimer;
        private int guardStanceAge;
        private bool parryConsumedThisStance;
        private int parryPendingTimer;
        private Vector2 parryPendingSource;
        private int blackBloodAfterglowTimer;
        private int darkSoulFlightTime;
        private int blackBloodLifeCostCooldown;
        private int darkEmberDecayDelay;
        private int leftComboIndex;
        private int leftComboResetTimer;
        private int guardKnockbackTimer;
        private bool darkEmberWasReady;
        private Vector2 guardSourceCenter;
        private Vector2 guardKnockbackVelocity;

        public bool HoldingGael => holdingTimer > 0;
        public bool GuardActive => guardActiveTimer > 0 && GuardCooldown <= 0;
        public float GuardCooldownRatio => GuardCooldown <= 0 ? 0f : GuardCooldown / (float)GuardCooldownMax;
        public bool BlackBloodActive => blackBloodAfterglowTimer > 0;
        public bool BlackHumanityActive => HoldingGael && Player.statLife <= Player.statLifeMax2 / 2;
        public bool DarkEmberReady => DarkEmbers >= DarkEmberMax;
        public float DarkEmberRatio => MathHelper.Clamp(DarkEmbers / (float)DarkEmberMax, 0f, 1f);

        public override void PreUpdate()
        {
            if (holdingTimer > 0)
                holdingTimer--;
            if (FollowupSlashWindow > 0)
                FollowupSlashWindow--;
            if (FinisherCooldown > 0)
                FinisherCooldown--;
            if (GuardCooldown > 0)
                GuardCooldown--;
            if (GuardFlashTimer > 0)
                GuardFlashTimer--;
            if (ParryFlashTimer > 0)
                ParryFlashTimer--;
            if (parryPendingTimer > 0)
                parryPendingTimer--;
            if (guardActiveTimer > 0)
                guardActiveTimer--;
            if (leftComboResetTimer > 0)
            {
                leftComboResetTimer--;
                if (leftComboResetTimer <= 0)
                    leftComboIndex = 0;
            }
            if (blackBloodAfterglowTimer > 0)
                blackBloodAfterglowTimer--;
            if (blackBloodLifeCostCooldown > 0)
                blackBloodLifeCostCooldown--;
            if (DarkEmberFlashTimer > 0)
                DarkEmberFlashTimer--;
            UpdateDarkEmbers();
        }

        public override void PostUpdate()
        {
            if (guardKnockbackTimer <= 0)
                return;

            guardKnockbackTimer--;
            Player.velocity = guardKnockbackVelocity;
            Player.fallStart = (int)(Player.position.Y / 16f);
        }

        public override void UpdateDead()
        {
            FollowupSlashWindow = 0;
            FinisherCooldown = 0;
            GuardCooldown = 0;
            GuardFlashTimer = 0;
            ParryFlashTimer = 0;
            DarkEmbers = 0;
            DarkEmberFlashTimer = 0;
            holdingTimer = 0;
            guardActiveTimer = 0;
            guardStanceAge = 0;
            parryConsumedThisStance = false;
            parryPendingTimer = 0;
            parryPendingSource = Vector2.Zero;
            blackBloodAfterglowTimer = 0;
            darkSoulFlightTime = 0;
            blackBloodLifeCostCooldown = 0;
            darkEmberDecayDelay = 0;
            leftComboIndex = 0;
            leftComboResetTimer = 0;
            guardKnockbackTimer = 0;
            darkEmberWasReady = false;
            guardSourceCenter = Vector2.Zero;
            guardKnockbackVelocity = Vector2.Zero;
        }

        public bool ConsumeFollowupSlash()
        {
            if (FollowupSlashWindow <= 0)
                return false;

            FollowupSlashWindow = 0;
            return true;
        }

        public int ConsumeLeftComboIndex(bool followupSlash)
        {
            leftComboResetTimer = 180;

            if (followupSlash)
                return LeftComboLength - 1;

            int combo = leftComboIndex;
            leftComboIndex = (leftComboIndex + 1) % LeftComboLength;
            return combo;
        }

        public bool ParryWindowOpen => GuardActive && !parryConsumedThisStance && guardStanceAge <= ParryWindowFrames;

        public void SetGuardActive(Vector2 sourceCenter, int stanceAge)
        {
            guardActiveTimer = 2;
            guardSourceCenter = sourceCenter;
            guardStanceAge = stanceAge;
            if (stanceAge <= 1)
                parryConsumedThisStance = false;
            holdingTimer = 2;
            Player.noKnockback = true;
            Lighting.AddLight(Player.Center, 0.26f, 0.04f, 0.34f);
        }

        public void ApplyHeldEffects()
        {
            holdingTimer = 2;
            ApplyDarkSoulPassive();
            ApplyBloodAndFire();
            ApplyBlackHumanity();
            ApplyDarkEmberPassive();
        }

        public float TryPayBlackBloodCost()
        {
            if (!BlackBloodActive || blackBloodLifeCostCooldown > 0 || Player.statLife <= 1)
                return 1f;

            int cost = Math.Max(4, (int)MathF.Ceiling(Player.statLifeMax2 * 0.035f));
            Player.statLife = Math.Max(1, Player.statLife - cost);
            blackBloodLifeCostCooldown = 6;
            blackBloodAfterglowTimer = Math.Max(blackBloodAfterglowTimer, 180);
            AddDarkEmbers(4 + GaelGreatswordProgression.GetStage(), true);

            CombatText.NewText(Player.Hitbox, new Color(160, 18, 34), cost, true);
            SpawnBlackBloodPaidEffects();
            return 1.32f + GaelGreatswordProgression.GetStage() * 0.03f + DarkEmberRatio * 0.08f;
        }

        public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers)
        {
            TryBreakGuard(npc.Center, ref modifiers);
        }

        public override void ModifyHitByProjectile(Projectile proj, ref Player.HurtModifiers modifiers)
        {
            TryBreakGuard(proj.Center, ref modifiers);
        }

        public void RegisterGreatswordHit(NPC target, int baseEmbers, bool forceBloodWake = false)
        {
            AddDarkEmbers(baseEmbers + GaelGreatswordProgression.GetStage(), forceBloodWake);

            if (target != null && Main.myPlayer == Player.whoAmI && Main.rand.NextBool(3))
            {
                int echoDamage = Math.Max(1, (int)(Player.GetWeaponDamage(Player.HeldItem) * MathHelper.Lerp(0.16f, 0.28f, DarkEmberRatio)));
                Projectile.NewProjectile(Player.GetSource_FromThis(), target.Center, Vector2.Zero,
                    ModContent.ProjectileType<GaelGreatswordBloodEcho>(), echoDamage, 1.2f, Player.whoAmI, DarkEmberRatio);
            }
        }

        public bool ConsumeDarkEmbers(int amount)
        {
            if (DarkEmbers < amount)
                return false;

            DarkEmbers -= amount;
            DarkEmberFlashTimer = 20;
            darkEmberWasReady = false;
            SpawnDarkEmberBurst(28, 6.2f);
            return true;
        }

        public void AddDarkEmbers(int amount, bool forceBloodWake = false)
        {
            if (amount <= 0)
                return;

            bool wasReady = DarkEmberReady;
            DarkEmbers = Math.Clamp(DarkEmbers + amount, 0, DarkEmberMax);
            darkEmberDecayDelay = 6 * 60;
            DarkEmberFlashTimer = Math.Max(DarkEmberFlashTimer, 10);

            if (forceBloodWake)
                blackBloodAfterglowTimer = Math.Max(blackBloodAfterglowTimer, 180);

            if (DarkEmberReady && !wasReady)
                SpawnDarkEmberBurst(18, 4.4f);

            LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref darkEmberWasReady, DarkEmberReady);
        }

        private void ApplyDarkSoulPassive()
        {
            Player.slowFall = true;
            Player.noFallDmg = true;

            // After the Wall of Flesh, Gael no longer passively feeds Calamity's Rage bar.
            // From that point onward, Black Humanity is the only Rage-feeding route.
            if (!Main.hardMode)
                GaelGreatswordRageInterop.AddRage(Player, GaelGreatswordProgression.GetPassiveRagePerFrame());

            HandleDarkSoulFlight();
        }

        private void ApplyBloodAndFire()
        {
            bool belowHalfLife = Player.statLife <= Player.statLifeMax2 / 2;
            bool rageHigh = GaelGreatswordRageInterop.GetRageRatio(Player) >= 0.75f;

            if (belowHalfLife || rageHigh)
                blackBloodAfterglowTimer = 180;

            if (blackBloodAfterglowTimer > 0)
                Player.AddBuff(ModContent.BuffType<GaelGreatswordBlackBlood>(), 2);
        }

        private void ApplyBlackHumanity()
        {
            if (!BlackHumanityActive)
                return;

            Player.GetAttackSpeed(DamageClass.Melee) += 0.38f;
            GaelGreatswordRageInterop.AddRage(Player, Main.hardMode ? 0.18f : 0.12f);

            if (Main.rand.NextBool(5))
            {
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-1.4f, 1.4f), Main.rand.NextFloat(-2.2f, -0.3f));
                Dust dust = Dust.NewDustPerfect(Player.Center + Main.rand.NextVector2Circular(24f, 34f), DustID.Blood, velocity, 130, new Color(120, 0, 20), Main.rand.NextFloat(1f, 1.45f));
                dust.noGravity = true;
            }
        }

        private void ApplyDarkEmberPassive()
        {
            if (DarkEmbers <= 0)
                return;

            float ratio = DarkEmberRatio;
            Player.GetDamage(DamageClass.Melee) += ratio * 0.06f;
            Player.GetAttackSpeed(DamageClass.Melee) += ratio * 0.08f;
            Player.endurance += ratio * 0.025f;
            Lighting.AddLight(Player.Center, 0.24f * ratio, 0.04f * ratio, 0.05f * ratio);

            if (DarkEmberReady && Main.rand.NextBool(4))
            {
                Dust dust = Dust.NewDustPerfect(Player.Center + Main.rand.NextVector2Circular(Player.width * 0.8f, Player.height),
                    Main.rand.NextBool(3) ? DustID.Blood : (int)CalamityDusts.Brimstone,
                    Main.rand.NextVector2Circular(1.1f, 1.1f), 100, new Color(150, 20, 70), Main.rand.NextFloat(0.85f, 1.3f));
                dust.noGravity = true;
            }
        }

        private void UpdateDarkEmbers()
        {
            if (darkEmberDecayDelay > 0)
            {
                darkEmberDecayDelay--;
            }
            else if (!HoldingGael && DarkEmbers > 0 && Main.GameUpdateCount % 5 == 0)
            {
                DarkEmbers--;
            }

            LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref darkEmberWasReady, DarkEmberReady);
        }

        private void HandleDarkSoulFlight()
        {
            int maxFlightTime = GaelGreatswordProgression.GetFlightFrames();
            if (maxFlightTime <= 0)
                return;

            bool grounded = Player.velocity.Y == 0f || Player.sliding || Player.mount.Active;
            if (grounded)
                darkSoulFlightTime = maxFlightTime;

            if (!Player.controlJump || darkSoulFlightTime <= 0 || Player.mount.Active || Player.grappling[0] >= 0)
                return;

            darkSoulFlightTime--;
            Player.velocity.Y -= GaelGreatswordProgression.GetFlightAcceleration();
            Player.velocity.Y = Math.Max(Player.velocity.Y, -GaelGreatswordProgression.GetFlightTopSpeed());
            Player.wingTime = Math.Max(Player.wingTime, 2f);

            if (Main.rand.NextBool(3))
            {
                Vector2 position = Player.Bottom + Main.rand.NextVector2Circular(18f, 4f);
                Dust dust = Dust.NewDustPerfect(position, (int)CalamityDusts.Brimstone, new Vector2(Main.rand.NextFloat(-1.1f, 1.1f), Main.rand.NextFloat(1.4f, 3.2f)), 120, GaelGreatswordVisuals.CrimsonViolet, 1.1f);
                dust.noGravity = true;
            }
        }

        private void SpawnBlackBloodPaidEffects()
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.4f, 4.4f);
                Dust dust = Dust.NewDustPerfect(Player.Center + Main.rand.NextVector2Circular(18f, 28f), DustID.Blood, velocity, 80, new Color(145, 0, 26), Main.rand.NextFloat(1.1f, 1.7f));
                dust.noGravity = Main.rand.NextBool();
            }
        }

        private void SpawnDarkEmberBurst(int count, float speed)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(speed * 0.25f, speed);
                Dust dust = Dust.NewDustPerfect(Player.Center + Main.rand.NextVector2Circular(18f, 28f),
                    Main.rand.NextBool(3) ? DustID.Blood : (int)CalamityDusts.Brimstone,
                    velocity, 90, Main.rand.NextBool() ? new Color(175, 18, 42) : GaelGreatswordVisuals.CrimsonViolet,
                    Main.rand.NextFloat(1f, 1.65f));
                dust.noGravity = true;
            }
        }

        public override bool FreeDodge(Player.HurtInfo info)
        {
            if (parryPendingTimer <= 0)
                return false;

            parryPendingTimer = 0;
            ExecutePerfectParry(parryPendingSource);
            return true;
        }

        private void TryBreakGuard(Vector2 sourceCenter, ref Player.HurtModifiers modifiers)
        {
            if (!GuardActive)
                return;

            Vector2 resolvedSource = sourceCenter == Vector2.Zero ? guardSourceCenter : sourceCenter;

            // 举剑瞬间的完美格挡窗口：完全格开这次攻击，姿态不中断，也不进入冷却。
            if (ParryWindowOpen)
            {
                parryConsumedThisStance = true;
                parryPendingTimer = 2;
                parryPendingSource = resolvedSource;
                return;
            }

            modifiers.FinalDamage *= 0.5f;
            BreakGuard(resolvedSource);
        }

        private void ExecutePerfectParry(Vector2 sourceCenter)
        {
            GuardFlashTimer = 26;
            ParryFlashTimer = 30;
            Player.immune = true;
            Player.immuneTime = Math.Max(Player.immuneTime, 30);

            AddDarkEmbers(18 + GaelGreatswordProgression.GetStage() * 2, true);
            FollowupSlashWindow = 45;

            CombatText.NewText(Player.Hitbox, new Color(238, 214, 250), Language.GetTextValue("Mods.CalamityLegendsComeBack.Items.Weapons.NewLegendGaelsGreatsword.ParryText"));
            SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.95f, Pitch = 0.48f }, Player.Center);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.6f, Pitch = -0.15f }, Player.Center);

            Vector2 counterDirection = Player.Center.DirectionTo(sourceCenter);
            if (counterDirection == Vector2.Zero || float.IsNaN(counterDirection.X))
                counterDirection = Vector2.UnitX * Player.direction;
            counterDirection = counterDirection.SafeNormalize(Vector2.UnitX * Player.direction);

            SpawnParryEffects(counterDirection);

            if (Main.myPlayer != Player.whoAmI || Player.HeldItem == null)
                return;

            Player.Calamity().GeneralScreenShakePower = Math.Max(Player.Calamity().GeneralScreenShakePower, 5f);

            int weaponDamage = Player.GetWeaponDamage(Player.HeldItem);
            Vector2 novaCenter = Player.Center + counterDirection * 64f;
            Projectile.NewProjectile(Player.GetSource_FromThis(), novaCenter, Vector2.Zero,
                ModContent.ProjectileType<GaelGreatswordBloodEcho>(), Math.Max(1, (int)(weaponDamage * 0.85f)),
                6f, Player.whoAmI, 1f);

            for (int i = 0; i < 3; i++)
            {
                float spread = MathHelper.Lerp(-0.3f, 0.3f, i / 2f);
                Vector2 velocity = counterDirection.RotatedBy(spread) * Main.rand.NextFloat(9.5f, 12f);
                Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center + counterDirection * 30f, velocity,
                    ModContent.ProjectileType<GaelGreatswordVengefulSoul>(), Math.Max(1, (int)(weaponDamage * 0.4f)),
                    2f, Player.whoAmI);
            }
        }

        private void SpawnParryEffects(Vector2 counterDirection)
        {
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Player.Center + counterDirection * 48f,
                counterDirection * 3f, new Color(238, 214, 250), new Vector2(1.35f, 0.6f),
                counterDirection.ToRotation(), 0.14f, 0.9f, 22));
            GeneralParticleHandler.SpawnParticle(new StrongBloom(Player.Center + counterDirection * 40f, Vector2.Zero,
                new Color(238, 214, 250) * 0.7f, 0.85f, 14));

            for (int i = 0; i < 14; i++)
            {
                Vector2 velocity = counterDirection.RotatedByRandom(0.9f) * Main.rand.NextFloat(3f, 11f);
                GeneralParticleHandler.SpawnParticle(new CritSpark(Player.Center + counterDirection * 44f + Main.rand.NextVector2Circular(14f, 14f),
                    velocity, Color.White, Main.rand.NextBool() ? new Color(190, 18, 42) : new Color(122, 52, 182),
                    Main.rand.NextFloat(0.4f, 0.85f), Main.rand.Next(10, 18)));
            }
        }

        private void BreakGuard(Vector2 sourceCenter)
        {
            GuardCooldown = GuardCooldownMax;
            GuardFlashTimer = 28;
            guardActiveTimer = 0;

            Vector2 knockbackDirection = Player.Center.DirectionFrom(sourceCenter);
            if (knockbackDirection == Vector2.Zero)
                knockbackDirection = new Vector2(-Player.direction, -0.25f);
            knockbackDirection = knockbackDirection.SafeNormalize(new Vector2(-Player.direction, -0.25f));

            guardKnockbackVelocity = knockbackDirection * 14.5f + new Vector2(0f, -3.2f);
            guardKnockbackTimer = 5;
            Player.velocity = guardKnockbackVelocity;
            Player.fallStart = (int)(Player.position.Y / 16f);
            Player.immune = true;
            Player.immuneNoBlink = true;
            Player.immuneTime = Math.Max(Player.immuneTime, 12);

            int guardType = ModContent.ProjectileType<GaelGreatswordGuardHoldout>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == Player.whoAmI && projectile.type == guardType)
                    projectile.Kill();
            }

            AddDarkEmbers(12 + GaelGreatswordProgression.GetStage(), true);
            SpawnGuardBreakEffects(knockbackDirection);
            SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.82f, Pitch = -0.28f }, Player.Center);
        }

        private void SpawnGuardBreakEffects(Vector2 knockbackDirection)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 22; i++)
            {
                Vector2 velocity = (-knockbackDirection).RotatedBy(Main.rand.NextFloat(-0.85f, 0.85f)) * Main.rand.NextFloat(2.2f, 8.5f);
                Dust dust = Dust.NewDustPerfect(Player.Center + Main.rand.NextVector2Circular(20f, 34f),
                    Main.rand.NextBool(3) ? DustID.Blood : (int)CalamityDusts.Brimstone, velocity, 90,
                    Main.rand.NextBool() ? new Color(190, 18, 42) : GaelGreatswordVisuals.CrimsonViolet,
                    Main.rand.NextFloat(1f, 1.7f));
                dust.noGravity = true;
            }

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Player.Center, -knockbackDirection * 4f,
                new Color(190, 18, 42), new Vector2(1.6f, 0.52f), knockbackDirection.ToRotation(),
                0.18f, 0.05f, 24));
        }
    }
}
