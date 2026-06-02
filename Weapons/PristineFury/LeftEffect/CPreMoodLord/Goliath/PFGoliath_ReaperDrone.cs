using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Typeless;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFGoliath_ReaperDrone : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/DraedonsArsenal/MountedScannerSummon";

        private const int CurveFrames = 30;
        private const float HomingSpeed = 16.8f;
        private const float TargetRange = 680f;
        private const float ExplosionVisualScale = 0.67f;
        private bool hasHit;

        private ref float CurveDirection => ref Projectile.ai[0];
        private ref float Phase => ref Projectile.ai[1];
        private ref float Timer => ref Projectile.localAI[0];
        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92));

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 170;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.scale = 0.74f;
        }

        public override void AI()
        {
            Timer++;
            Vector2 currentDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float currentSpeed = Projectile.velocity.Length();

            if (Timer <= CurveFrames)
            {
                float curveStrength = Utils.GetLerpValue(0f, CurveFrames, Timer, true);
                float wobble = (float)Math.Sin(Timer * 0.38f + Phase) * 0.012f;
                Projectile.velocity = Projectile.velocity.RotatedBy(CurveDirection * 0.032f * curveStrength + wobble) * 1.002f;
            }
            else
            {
                NPC target = FindTarget();
                if (target != null)
                {
                    Vector2 desiredDirection = Projectile.SafeDirectionTo(target.Center, currentDirection);
                    Vector2 desiredVelocity = desiredDirection * MathHelper.Lerp(currentSpeed, HomingSpeed, 0.08f);
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.075f);
                }
                else
                    Projectile.velocity *= 0.996f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.Opacity = Utils.GetLerpValue(0f, 10f, Timer, true) * Utils.GetLerpValue(0f, 20f, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * (0.46f + Projectile.Opacity * 0.34f));

            if (!Main.dedServ)
                SpawnFlightEffects();
        }

        private NPC FindTarget()
        {
            NPC closest = null;
            float bestDistance = TargetRange;
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

        private void SpawnFlightEffects()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = direction.RotatedBy(MathHelper.PiOver2);
            Color theme = Color.Lerp(ThemeColor, new Color(180, 255, 80), 0.18f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - direction * Main.rand.NextFloat(4f, 14f) + side * Main.rand.NextFloat(-6f, 6f),
                    Main.rand.NextBool(4) ? DustID.GreenTorch : DustID.GoldFlame,
                    -direction * Main.rand.NextFloat(0.6f, 1.8f) + side * Main.rand.NextFloat(-0.45f, 0.45f),
                    0,
                    Main.rand.NextBool(3) ? Color.Lerp(theme, new Color(160, 255, 74), 0.22f) : theme,
                    Main.rand.NextFloat(0.55f, 1f));

                dust.noGravity = true;
                dust.fadeIn = 1.1f;
            }

            if ((int)Timer % 3 == 0)
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(Projectile.Center - direction * Main.rand.NextFloat(4f, 14f) + side * Main.rand.NextFloat(-8f, 8f), -direction * Main.rand.NextFloat(0.45f, 1.2f), false, Main.rand.Next(8, 14), Main.rand.NextFloat(0.18f, 0.34f), Color.Lerp(theme, new Color(180, 255, 80), 0.18f), true, false, true));
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            CalamityUtils.CircularHitboxCollision(Projectile.Center, 18f * Projectile.scale, targetHitbox);

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            hasHit = true;
            target.AddBuff(ModContent.BuffType<Plague>(), 210);
        }

        public override void OnKill(int timeLeft)
        {
            if (!hasHit)
            {
                SpawnMissEffects();
                return;
            }

            SpawnPlagueBurst();
        }

        private void SpawnPlagueBurst()
        {
            Vector2 center = Projectile.Center;
            int sourceDamage = Projectile.damage;
            Color theme = Color.Lerp(ThemeColor, new Color(180, 255, 80), 0.12f);

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.24f, Pitch = 0.42f, PitchVariance = 0.16f }, center);

            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.ExpandHitboxBy((int)(30 * ExplosionVisualScale));
                Projectile.damage = Math.Max(1, (int)(sourceDamage * 0.58f));
                Projectile.penetrate = -1;
                Projectile.Damage();

                for (int i = 0; i < 2; i++)
                {
                    Vector2 beeVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4.2f, 8.2f);
                    int bee = Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        center,
                        beeVelocity,
                        ModContent.ProjectileType<BasicPlagueBee>(),
                        Math.Max(1, (int)(sourceDamage * 0.05f)),
                        0f,
                        Projectile.owner);

                    if (bee >= 0 && bee < Main.maxProjectiles)
                    {
                        Main.projectile[bee].penetrate = 1;
                        Main.projectile[bee].DamageType = DamageClass.Ranged;
                    }
                }
            }

            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center,
                Vector2.Zero,
                theme * 0.82f,
                "CalamityMod/Particles/FlameExplosion",
                Vector2.One,
                Main.rand.NextFloat(-10f, 10f),
                0.05f,
                0.17f * ExplosionVisualScale,
                16));

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center,
                Vector2.Zero,
                Color.Lerp(theme, new Color(155, 255, 70), 0.25f) * 0.65f,
                "CalamityMod/Particles/BloomCircle",
                Vector2.One,
                Main.rand.NextFloat(-10f, 10f),
                0.06f,
                0.23f * ExplosionVisualScale,
                14,
                true));

            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.4f, 7.2f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    center + Main.rand.NextVector2Circular(8f, 8f),
                    velocity,
                    false,
                    Main.rand.Next(18, 32),
                    Main.rand.NextFloat(0.35f, 0.72f) * ExplosionVisualScale,
                    Main.rand.NextBool(4) ? Color.Lerp(theme, new Color(180, 255, 80), 0.24f) : theme,
                    true,
                    true));
            }

            for (int i = 0; i < 8; i++)
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    center + Main.rand.NextVector2Circular(12f, 12f),
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.2f, 3.8f),
                    Color.Lerp(theme, Color.DarkOliveGreen, Main.rand.NextFloat(0.2f, 0.48f)),
                    Main.rand.Next(18, 32),
                    Main.rand.NextFloat(0.46f, 0.92f) * ExplosionVisualScale,
                    0.55f,
                    Main.rand.NextFloat(-0.07f, 0.07f),
                    glowing: true));
            }
        }

        private void SpawnMissEffects()
        {
            if (Main.dedServ)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Color theme = ThemeColor;
            for (int i = 0; i < 6; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - direction * Main.rand.NextFloat(4f, 18f) + Main.rand.NextVector2Circular(8f, 8f),
                    DustID.GoldFlame,
                    -direction.RotatedByRandom(0.35f) * Main.rand.NextFloat(0.5f, 1.6f),
                    0,
                    theme,
                    Main.rand.NextFloat(0.45f, 0.82f));

                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D body = TextureAssets.Projectile[Type].Value;
            Texture2D frontWing = ModContent.Request<Texture2D>("CalamityMod/Particles/XykWingOrange1").Value;
            Texture2D backWing = ModContent.Request<Texture2D>("CalamityMod/Particles/XykWingOrange2").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 origin = body.Size() * 0.5f;
            Color theme = ThemeColor;
            Color additiveTheme = (Color.Lerp(theme, new Color(180, 255, 80), 0.16f) with { A = 0 }) * Projectile.Opacity;
            Color bodyColor = Color.Lerp(theme, new Color(180, 255, 80), 0.08f) * Projectile.Opacity;

            PFLeftEffectRules.BeginAdditive();

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                additiveTheme * 0.18f,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                new Vector2(0.18f, 0.13f) * Projectile.scale,
                SpriteEffects.None,
                0f);

            for (int i = 1; i < Projectile.oldPos.Length; i += 3)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float opacity = (1f - i / (float)Projectile.oldPos.Length) * 0.22f * Projectile.Opacity;
                Vector2 afterimagePosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(body, afterimagePosition, null, additiveTheme * opacity, Projectile.rotation, origin, Projectile.scale * 0.96f, SpriteEffects.None, 0f);
            }

            DrawWingPair(frontWing, drawPosition + forward * 6.5f, forward, side, 0f, 1.78f, additiveTheme);
            DrawWingPair(backWing, drawPosition - forward * 9.5f, forward, side, MathHelper.Pi, 1.48f, additiveTheme * 0.96f);

            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 2.1f;
                Main.EntitySpriteDraw(body, drawPosition + offset, null, additiveTheme * 0.36f, Projectile.rotation, origin, Projectile.scale * 1.02f, SpriteEffects.None, 0f);
            }

            PFLeftEffectRules.EndAdditive();

            Main.EntitySpriteDraw(body, drawPosition, null, bodyColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        private void DrawWingPair(Texture2D wing, Vector2 root, Vector2 forward, Vector2 side, float phaseOffset, float size, Color color)
        {
            float time = Main.GlobalTimeWrappedHourly * 74f + Projectile.identity * 0.41f + phaseOffset;
            float flap = (float)Math.Sin(time);
            float snap = (float)Math.Sin(time * 1.73f) * 0.5f + 0.5f;

            for (int sideSign = -1; sideSign <= 1; sideSign += 2)
            {
                float sampleFlap = flap + 0.28f * sideSign;
                Vector2 wingDirection = (side * sideSign * (1.2f + Math.Abs(sampleFlap) * 0.3f) - forward * (0.42f - sampleFlap * 0.2f)).SafeNormalize(side * sideSign);
                Vector2 drawPosition = root + side * sideSign * (7.5f + snap * 3.6f) - forward * (2.4f + Math.Abs(sampleFlap) * 1.6f);
                float rotation = wingDirection.ToRotation() - MathHelper.PiOver2;
                Vector2 wingScale = new(0.16f * size * Projectile.scale, (0.13f + snap * 0.028f) * size * Projectile.scale);

                Main.EntitySpriteDraw(
                    wing,
                    drawPosition,
                    null,
                    color * 1.75f,
                    rotation,
                    new Vector2(wing.Width * 0.5f, 0f),
                    wingScale,
                    sideSign < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                    0f);

                Main.EntitySpriteDraw(
                    wing,
                    drawPosition - forward * 2f,
                    null,
                    (Color.White with { A = 0 }) * 0.24f * Projectile.Opacity,
                    rotation,
                    new Vector2(wing.Width * 0.5f, 0f),
                    wingScale * new Vector2(0.72f, 0.9f),
                    sideSign < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                    0f);
            }
        }
    }
}
