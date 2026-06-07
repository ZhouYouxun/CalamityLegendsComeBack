using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFPlantera_PseudoLaser : ModProjectile, ILocalizedModType
    {
        internal const float BeamLength = 380f;

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.timeLeft = 6;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (Main.dedServ)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Color beamColor = PFLeftEffectRules.GetThemeColor(Projectile, new Color(0, 255, 180));
            if (Main.rand.NextBool(2))
            {
                for (float offset = 0f; offset < BeamLength; offset += Main.rand.NextFloat(72f, 118f))
                {
                    Vector2 sparkPos = Projectile.Center + direction * offset + Main.rand.NextVector2Circular(4f, 4f);
                    Vector2 glowVelocity = direction * Main.rand.NextFloat(0.4f, 0.9f);
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        sparkPos,
                        glowVelocity,
                        false,
                        5,
                        Main.rand.NextFloat(0.08f, 0.16f),
                        Color.Lerp(beamColor, Color.White, Main.rand.NextFloat(0.1f, 0.35f)),
                        true,
                        false,
                        true
                    ));

                    if (Main.rand.NextBool(3))
                    {
                        GeneralParticleHandler.SpawnParticle(new CustomSpark(
                            sparkPos,
                            direction.RotatedByRandom(0.18f) * Main.rand.NextFloat(0.8f, 1.6f),
                            "CalamityMod/Particles/ThinEndedLine",
                            false,
                            Main.rand.Next(6, 10),
                            Main.rand.NextFloat(0.08f, 0.14f),
                            Color.Lerp(beamColor, Color.White, Main.rand.NextFloat(0.25f, 0.55f)),
                            new Vector2(0.38f, 1.25f),
                            true,
                            false));
                    }
                }
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + direction * BeamLength, 12f, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D bloomRing = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(0, 255, 180));
            float fade = Utils.GetLerpValue(0f, 3f, Projectile.timeLeft, true);
            Vector2 start = Projectile.Center - Main.screenPosition;
            Vector2 end = Projectile.Center + direction * BeamLength - Main.screenPosition;

            PFLeftEffectRules.BeginAdditive();

            Texture2D startTex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Boss/ProvidenceHolyRay", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Texture2D midTex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/ProvidenceHolyRayMid", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Texture2D endTex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/ProvidenceHolyRayEnd", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

            float drawScale = 0.16f;
            float rotation = direction.ToRotation() - MathHelper.PiOver2;
            Vector2 scaleVec = new Vector2(drawScale, drawScale);

            // Draw start piece
            Main.spriteBatch.Draw(startTex, start, null, theme * fade, rotation, startTex.Size() / 2f, scaleVec, SpriteEffects.None, 0f);

            float currentLength = BeamLength;
            currentLength -= (startTex.Height / 2 + endTex.Height) * drawScale;
            Vector2 center = Projectile.Center + direction * drawScale * startTex.Height / 2f;

            if (currentLength > 0f)
            {
                float lengthDrawn = 0f;
                int frameHeight = 36;
                int frameY = frameHeight * (Projectile.timeLeft / 3 % 4);
                Rectangle sourceRect = new Rectangle(0, frameY, midTex.Width, frameHeight);

                while (lengthDrawn + 1f < currentLength)
                {
                    if (currentLength - lengthDrawn < frameHeight * drawScale)
                    {
                        sourceRect.Height = (int)((currentLength - lengthDrawn) / drawScale);
                    }
                    if (sourceRect.Height <= 0)
                        break;

                    Main.spriteBatch.Draw(midTex, center - Main.screenPosition, sourceRect, theme * fade, rotation, new Vector2(sourceRect.Width / 2f, 0f), scaleVec, SpriteEffects.None, 0f);
                    lengthDrawn += sourceRect.Height * drawScale;
                    center += direction * sourceRect.Height * drawScale;

                    sourceRect.Y += frameHeight;
                    if (sourceRect.Y + sourceRect.Height > midTex.Height)
                    {
                        sourceRect.Y = 0;
                    }
                }
            }

            Vector2 endPos = center - Main.screenPosition;
            Main.spriteBatch.Draw(endTex, endPos, null, theme * fade, rotation, new Vector2(endTex.Width / 2f, 0f), scaleVec, SpriteEffects.None, 0f);

            // Origin glow
            Main.EntitySpriteDraw(bloom, start, null, theme * (0.82f * fade), 0f, bloom.Size() * 0.5f, 0.5f * drawScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloomRing, start, null, theme * (0.62f * fade), Main.GlobalTimeWrappedHourly * 2.4f, bloomRing.Size() * 0.5f, 0.74f * drawScale, SpriteEffects.None, 0);

            // End glow
            Main.EntitySpriteDraw(bloom, end, null, theme * (0.78f * fade), 0f, bloom.Size() * 0.5f, 0.46f * drawScale, SpriteEffects.None, 0);

            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }

    internal sealed class PFPlantera_Flame : ModProjectile, ILocalizedModType
    {
        private ref float Timer => ref Projectile.localAI[0];
        private ref float LaserFiredTimer => ref Projectile.localAI[1];
        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(0, 255, 180));

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 74;
            Projectile.extraUpdates = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (Projectile.numUpdates == 0)
                Timer++;

            Projectile.velocity = Projectile.velocity.RotatedBy((float)Math.Sin((Timer + Projectile.ai[0] * 11f) * 0.12f) * 0.006f);
            Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * 0.45f);
            ApplySubtleTracking();

            if (LaserFiredTimer <= 0f && Timer >= 12f && Projectile.numUpdates == 0)
                TryFireTargetedLaser();

            if (LaserFiredTimer > 0f)
            {
                LaserFiredTimer++;
                Projectile.velocity *= 0.94f;
                Projectile.Opacity = MathHelper.Clamp(1f - LaserFiredTimer / 14f, 0f, 1f);
                if (LaserFiredTimer >= 14f)
                    Projectile.Kill();
            }

            if (Main.dedServ)
                return;

            Color particleColor = Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.12f, 0.42f));
            if (Timer % 2f == 0f)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(3f, 3f),
                    -Projectile.velocity * 0.22f + Main.rand.NextVector2Circular(0.4f, 0.4f),
                    false,
                    Main.rand.Next(9, 14),
                    Main.rand.NextFloat(0.1f, 0.18f) * Projectile.scale,
                    particleColor * 0.85f,
                    true,
                    false,
                    true));
            }

            if (Projectile.numUpdates == 0 && Main.rand.NextBool(3))
            {
                Vector2 tangent = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center + tangent * Main.rand.NextFloat(-5f, 5f),
                    -Projectile.velocity * 0.12f + tangent * Main.rand.NextFloat(-0.8f, 0.8f),
                    "CalamityMod/Particles/ThinEndedLine",
                    false,
                    Main.rand.Next(7, 12),
                    Main.rand.NextFloat(0.08f, 0.13f),
                    Color.Lerp(particleColor, Color.White, Main.rand.NextFloat(0.15f, 0.38f)),
                    new Vector2(0.3f, 1.1f),
                    true,
                    false));
            }
        }

        private void ApplySubtleTracking()
        {
            if (Timer < 4f)
                return;

            NPC target = FindNearestTarget(780f);
            if (target is null)
                return;

            Vector2 currentDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 desiredDirection = (target.Center - Projectile.Center).SafeNormalize(currentDirection);
            float turn = MathHelper.Lerp(MathHelper.ToRadians(1.8f), MathHelper.ToRadians(5.6f), Utils.GetLerpValue(4f, 26f, Timer, true));
            Vector2 newDirection = currentDirection.ToRotation().AngleTowards(desiredDirection.ToRotation(), turn).ToRotationVector2();
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, newDirection * Projectile.velocity.Length(), 0.088f);
        }

        private void TryFireTargetedLaser()
        {
            NPC target = FindNearestTarget(PFPlantera_PseudoLaser.BeamLength);
            if (target is null)
                return;

            Vector2 direction = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX));
            if (Main.myPlayer == Projectile.owner)
            {
                int laser = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    direction,
                    ModContent.ProjectileType<PFPlantera_PseudoLaser>(),
                    Math.Max(1, (int)(Projectile.damage * 0.92f)),
                    Projectile.knockBack * 0.45f,
                    Projectile.owner);
                PFLeftEffectRules.ApplyTheme(laser, (PristineFuryMark)(int)Projectile.ai[2]);
            }

            LaserFiredTimer = 1f;
            SpawnLaserReleaseEffects(direction);
        }

        private NPC FindNearestTarget(float range)
        {
            NPC closest = null;
            float bestDistance = range;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                closest = npc;
            }

            return closest;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            CalamityMod.CalamityUtils.CircularHitboxCollision(Projectile.Center, 12f, targetHitbox);

        public override void OnKill(int timeLeft)
        {
            SpawnImpactEffects(Projectile.Center);
        }

        private void SpawnLaserReleaseEffects(Vector2 direction)
        {
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                direction * 0.6f,
                ThemeColor * 0.72f,
                new Vector2(0.44f, 1.45f),
                direction.ToRotation(),
                0.12f,
                0.022f,
                12));

            for (int i = 0; i < 5; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.34f) * Main.rand.NextFloat(1.8f, 3.8f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    velocity,
                    "CalamityMod/Particles/ThinEndedLine",
                    false,
                    Main.rand.Next(8, 13),
                    Main.rand.NextFloat(0.1f, 0.18f),
                    Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.22f, 0.5f)),
                    new Vector2(0.34f, 1.45f),
                    true,
                    false));
            }
        }

        private void SpawnImpactEffects(Vector2 center)
        {
            if (Main.dedServ)
                return;

            Color color = ThemeColor;

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center,
                Vector2.Zero,
                color * 0.75f,
                Vector2.One,
                Projectile.rotation,
                0.12f,
                0.85f,
                14));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D bloomRing = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D circularSmear = ModContent.Request<Texture2D>("CalamityMod/Particles/CircularSmear").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color theme = Color.Lerp(ThemeColor, Color.White, 0.15f) * Projectile.Opacity;
            Color themeA0 = theme with { A = 0 };

            float scale = (0.18f + (float)Math.Sin(Timer * 0.18f) * 0.04f) * Projectile.scale * (1f - Projectile.ai[0] * 0.15f);

            // Draw using default premultiplied alpha (so A = 0 draws additively in AlphaBlend)
            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                themeA0 * 0.62f,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                scale * 1.0f,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                circularSmear,
                drawPosition,
                null,
                themeA0 * 0.45f,
                Projectile.rotation * 1.5f + Timer * 0.06f,
                circularSmear.Size() * 0.5f,
                scale * 0.6f,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                bloomRing,
                drawPosition,
                null,
                themeA0 * 0.38f,
                -Projectile.rotation * 0.8f - Timer * 0.04f,
                bloomRing.Size() * 0.5f,
                scale * 0.42f,
                SpriteEffects.None,
                0f);

            return false;
        }
    }
}
