using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Healing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFRavagerEffect
    {
        private const float DamageMultiplier = 0.42f;

        internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (!held)
            {
                KillExistingLaser(holdout);
                PristineFuryLeftEffectRegistry.Reset(holdout);
                return;
            }

            EnsureLaser(holdout);
            holdout.LeftTimer++;
            if (holdout.LeftTimer % 7 == 0)
                holdout.SpawnMuzzleBurst(PristineFuryMarkHelper.GetColor(holdout.CurrentMark), 0.5f);
        }

        private static void EnsureLaser(NewLegendPristineFuryHoldOut holdout)
        {
            int laserType = ModContent.ProjectileType<PFRavager_Laser>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == holdout.Projectile.owner && projectile.type == laserType && (int)projectile.ai[0] == holdout.Projectile.whoAmI)
                {
                    projectile.damage = holdout.GetScaledDamage(DamageMultiplier);
                    projectile.knockBack = holdout.Projectile.knockBack * 0.2f;
                    projectile.netUpdate = true;
                    return;
                }
            }

            int laser = Projectile.NewProjectile(
                holdout.Projectile.GetSource_FromThis(),
                holdout.GunTipPosition + holdout.AimDirection * 8f,
                holdout.AimDirection,
                laserType,
                holdout.GetScaledDamage(DamageMultiplier),
                holdout.Projectile.knockBack * 0.2f,
                holdout.Projectile.owner,
                holdout.Projectile.whoAmI);
            PFLeftEffectRules.ApplyTheme(laser, holdout.CurrentMark);
        }

        private static void KillExistingLaser(NewLegendPristineFuryHoldOut holdout)
        {
            int laserType = ModContent.ProjectileType<PFRavager_Laser>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == holdout.Projectile.owner && projectile.type == laserType && (int)projectile.ai[0] == holdout.Projectile.whoAmI)
                    projectile.Kill();
            }
        }
    }

    internal sealed class PFRavager_Laser : ModProjectile, ILocalizedModType
    {
        private const float MaxBeamLength = 980f;
        private const float CollisionWidth = 18f;
        private const int NumSamplePoints = 3;

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float HoldoutIndex => ref Projectile.ai[0];
        private ref float BeamLength => ref Projectile.ai[1];
        private ref float Timer => ref Projectile.localAI[0];
        private ref float HealCooldown => ref Projectile.localAI[1];
        private int selectedTargetIndex = -1;
        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92));

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Timer++;
            if (HealCooldown > 0f)
                HealCooldown--;

            int holdoutIndex = (int)HoldoutIndex;
            if (!Main.projectile.IndexInRange(holdoutIndex) || !Main.projectile[holdoutIndex].active || Main.projectile[holdoutIndex].ModProjectile is not NewLegendPristineFuryHoldOut holdout || holdout.CurrentMark != PristineFuryMark.Ravager)
            {
                Projectile.Kill();
                return;
            }

            Vector2 desiredDirection = holdout.AimDirection.SafeNormalize(Vector2.UnitX * holdout.Owner.direction);
            Vector2 currentDirection = Projectile.velocity.SafeNormalize(desiredDirection);
            Vector2 direction = Vector2.Lerp(currentDirection, desiredDirection, 0.34f).SafeNormalize(desiredDirection);
            Projectile.velocity = direction;
            Projectile.Center = holdout.GunTipPosition + direction * 12f;
            Projectile.rotation = direction.ToRotation();
            Projectile.timeLeft = 2;

            float[] laserScanResults = new float[NumSamplePoints];
            Collision.LaserScan(Projectile.Center, direction, 8f, MaxBeamLength, laserScanResults);
            float averageLength = 0f;
            for (int i = 0; i < laserScanResults.Length; i++)
                averageLength += laserScanResults[i];
            averageLength /= NumSamplePoints;
            BeamLength = MathHelper.Lerp(BeamLength <= 0f ? averageLength : BeamLength, averageLength, 0.45f);

            selectedTargetIndex = FindSingleTarget();
            Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * 0.52f);
            DelegateMethods.v3_1 = ThemeColor.ToVector3() * 0.42f;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + direction * BeamLength, CollisionWidth, DelegateMethods.CastLight);
            EmitBeamParticles(direction);
        }

        private int FindSingleTarget()
        {
            Vector2 start = Projectile.Center;
            Vector2 end = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * BeamLength;
            int targetIndex = -1;
            float bestDistance = BeamLength + 1f;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float _ = 0f;
                if (!Collision.CheckAABBvLineCollision(npc.Hitbox.TopLeft(), npc.Hitbox.Size(), start, end, CollisionWidth, ref _))
                    continue;

                float distance = Vector2.Distance(start, npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                targetIndex = npc.whoAmI;
            }

            return targetIndex;
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (selectedTargetIndex < 0)
                return false;

            return target.whoAmI == selectedTargetIndex;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            Vector2 start = Projectile.Center;
            Vector2 end = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * BeamLength;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, CollisionWidth, ref _);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<BurningBlood>(), 420);
            target.AddBuff(ModContent.BuffType<Laceration>(), 420);
            TryHealOwner(target);
            SpawnHitEffects(target.Center);
        }

        private void TryHealOwner(NPC target)
        {
            if (!Main.player.IndexInRange(Projectile.owner))
                return;

            Player owner = Main.player[Projectile.owner];
            if (HealCooldown <= 0f && owner.statLife < owner.statLifeMax2)
            {
                int heal = target.lifeMax > 5000 ? 2 : 1;
                owner.statLife = Math.Min(owner.statLife + heal, owner.statLifeMax2);
                owner.HealEffect(heal);
                HealCooldown = 12f;
            }

            if (Projectile.owner == Main.myPlayer && Main.rand.NextBool(7))
            {
                Vector2 velocity = (owner.Center - target.Center).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.45f) * Main.rand.NextFloat(3.2f, 5.6f);
                Projectile.NewProjectile(
                    Projectile.GetSource_OnHit(target),
                    target.Center,
                    velocity,
                    ModContent.ProjectileType<BloodstoneHealOrb>(),
                    20,
                    0f,
                    Projectile.owner);
            }
        }

        private void SpawnHitEffects(Vector2 center)
        {
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new CustomPulse(center, Vector2.Zero, ThemeColor * 0.52f, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.05f, 0.34f, 12, false));
            for (int i = 0; i < 4; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(12f, 12f),
                    Main.rand.NextBool(3) ? DustID.LifeDrain : DustID.GoldFlame,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.4f, 4.2f),
                    0,
                    Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.06f, 0.28f)),
                    Main.rand.NextFloat(0.8f, 1.35f));
                dust.noGravity = true;
            }
        }

        private void EmitBeamParticles(Vector2 direction)
        {
            if (Main.dedServ || BeamLength <= 12f)
                return;

            if (Main.rand.NextBool(2))
            {
                Vector2 position = Projectile.Center + direction * Main.rand.NextFloat(BeamLength) + direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-7f, 7f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    position,
                    Main.rand.NextVector2Circular(0.35f, 0.35f),
                    false,
                    Main.rand.Next(10, 16),
                    Main.rand.NextFloat(0.18f, 0.32f),
                    Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.06f, 0.32f)),
                    true,
                    false,
                    true));
            }

            if ((int)Timer % 5 == 0)
            {
                Vector2 end = Projectile.Center + direction * BeamLength;
                Dust dust = Dust.NewDustPerfect(
                    end + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextBool(3) ? DustID.LifeDrain : DustID.GoldFlame,
                    -direction.RotatedByRandom(0.45f) * Main.rand.NextFloat(0.8f, 2.4f),
                    0,
                    ThemeColor,
                    Main.rand.NextFloat(0.75f, 1.2f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (BeamLength <= 4f || Main.dedServ)
                return false;

            Texture2D line = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineSoftEdge").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 start = Projectile.Center - Main.screenPosition;
            Vector2 end = Projectile.Center + direction * BeamLength - Main.screenPosition;
            Vector2 mid = (start + end) * 0.5f;
            float fade = Utils.GetLerpValue(0f, 8f, Timer, true);
            Color theme = (Color.Lerp(ThemeColor, Color.White, 0.16f) with { A = 0 }) * fade;

            PFLeftEffectRules.BeginAdditive();
            Main.EntitySpriteDraw(line, mid, null, theme * 0.88f, direction.ToRotation() + MathHelper.PiOver2, line.Size() * 0.5f, new Vector2(0.32f, BeamLength / line.Height), SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(line, mid, null, (Color.White with { A = 0 }) * 0.32f * fade, direction.ToRotation() + MathHelper.PiOver2, line.Size() * 0.5f, new Vector2(0.12f, BeamLength / line.Height), SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(bloom, start, null, theme * 0.68f, Projectile.rotation, bloom.Size() * 0.5f, 0.22f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(bloom, end, null, theme * 0.52f, Projectile.rotation, bloom.Size() * 0.5f, 0.16f, SpriteEffects.None, 0f);
            PFLeftEffectRules.EndAdditive();

            return false;
        }
    }
}
