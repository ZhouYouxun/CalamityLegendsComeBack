using System;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.Passive;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.LeftGeneral
{
    internal sealed class YC_AuricJudgementWave : ModProjectile, ILocalizedModType
    {
        private static readonly Color WaveRed = new(255, 76, 34);
        private static readonly Color WaveGold = new(255, 214, 86);
        private static readonly Color WaveWhite = new(255, 246, 196);

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Projectiles/Melee/JudgementProj";

        private ref float Timer => ref Projectile.ai[0];
        private float hitboxSize = 34f;
        // Track if we've already spawned the impact burst at the wave front
        private bool burstSpawned;

        public override void SetDefaults()
        {
            Projectile.width = 336;
            Projectile.height = 274;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 130;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.extraUpdates = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.scale = 0.44f + Projectile.ai[0] * 0.10f;
            YharimsCrystalHellBladeGlobalProjectile.Mark(Projectile, YCWeaponForm.Blade);
        }

        public override void AI()
        {
            Timer++;
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            Projectile.velocity *= 0.986f;
            Projectile.scale += 0.0056f;
            hitboxSize += 0.9f;

            if (Projectile.timeLeft < 42)
                Projectile.Opacity = Utils.GetLerpValue(0f, 42f, Projectile.timeLeft, true);

            Lighting.AddLight(Projectile.Center, Color.Lerp(WaveRed, WaveGold, 0.45f).ToVector3() * 0.5f);
            EmitWaveDust();
            EmitFlightTrail();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(new BalanceYharimsCrystal().GetFireDebuffType(), 240);
            TriggerImpactBurst(target.Center);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.numHits > 0)
                modifiers.SourceDamage *= 0.88f;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            CalamityUtils.CircularHitboxCollision(Projectile.Center, hitboxSize * Projectile.scale, targetHitbox);

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float fade = Projectile.Opacity * Utils.GetLerpValue(0f, 18f, Timer, true);
            Color color = Color.Lerp(WaveRed, WaveGold, 0.42f) with { A = 0 };

            // 5-layer staggered draw (based on Judgment report technique)
            for (int i = 0; i < 5; i++)
            {
                Vector2 offset = (Projectile.rotation + MathHelper.PiOver2).ToRotationVector2() * 9f * i;
                Main.spriteBatch.Draw(
                    texture,
                    Projectile.Center - Main.screenPosition + offset,
                    null,
                    color * fade * (0.72f - i * 0.08f),
                    Projectile.rotation,
                    texture.Size() * 0.5f,
                    new Vector2(1f + 0.015f * i, 1.2f - 0.06f * i) * Projectile.scale,
                    SpriteEffects.None,
                    0f);
            }

            // Bright leading-edge bloom glow at the wave front
            float pulseFront = 0.85f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 18f);
            Vector2 frontOffset = Projectile.velocity.SafeNormalize(Vector2.UnitY) * hitboxSize * Projectile.scale * 0.65f;
            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center + frontOffset - Main.screenPosition,
                null,
                WaveGold with { A = 0 } * fade * 0.55f * pulseFront,
                0f,
                bloom.Size() * 0.5f,
                Projectile.scale * 0.35f * pulseFront,
                SpriteEffects.None);

            // Wing-tip glow at left and right edges (Judgment report technique)
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 wingTip = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(side * MathHelper.ToRadians(100f)) * hitboxSize * Projectile.scale * 0.82f;
                Main.EntitySpriteDraw(
                    bloom,
                    wingTip - Main.screenPosition,
                    null,
                    Color.Lerp(WaveRed, WaveGold, 0.6f) with { A = 0 } * fade * 0.42f,
                    0f,
                    bloom.Size() * 0.5f,
                    Projectile.scale * 0.22f,
                    SpriteEffects.None);
            }

            return false;
        }

        private void EmitFlightTrail()
        {
            if (Main.dedServ || Projectile.velocity.LengthSquared() < 0.01f)
                return;

            // Every 2 frames: vertically elongated BloomCircle along the flight path (Judgment technique)
            if ((int)Timer % 2 == 0)
            {
                Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitY);
                // Place on the leading edge
                Vector2 trailPos = Projectile.Center + dir * hitboxSize * Projectile.scale * 0.5f + dir.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-hitboxSize * Projectile.scale * 0.3f, hitboxSize * Projectile.scale * 0.3f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    trailPos,
                    -dir * Main.rand.NextFloat(1f, 3f),
                    false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.22f, 0.42f) * Projectile.scale,
                    Main.rand.NextBool(3) ? WaveWhite : Color.Lerp(WaveRed, WaveGold, Main.rand.NextFloat(0.3f, 0.9f))));
            }

            // Wing-tip sparks (Judgment report: SparkParticle from wing tips)
            if ((int)Timer % 4 == 0)
            {
                Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitY);
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 wingPos = Projectile.Center + dir.RotatedBy(side * MathHelper.ToRadians(100f)) * hitboxSize * Projectile.scale * 0.82f;
                    Vector2 wingVel = dir.RotatedBy(side * MathHelper.ToRadians(185f)) * Main.rand.NextFloat(3f, 8f);
                    GeneralParticleHandler.SpawnParticle(new SparkParticle(
                        wingPos,
                        wingVel,
                        false,
                        Main.rand.Next(10, 18),
                        Main.rand.NextFloat(0.55f, 1.0f),
                        Main.rand.NextBool(3) ? WaveWhite : WaveGold));
                }
            }
        }

        private void EmitWaveDust()
        {
            if (Main.dedServ || !Main.rand.NextBool(3))
                return;

            Vector2 edge = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(1.8f) * hitboxSize * Projectile.scale;
            Dust dust = Dust.NewDustPerfect(edge, ModContent.DustType<SquashDust>(), -Projectile.velocity * Main.rand.NextFloat(0.3f, 0.8f), 0, Main.rand.NextBool() ? WaveGold : WaveRed, Main.rand.NextFloat(0.85f, 1.15f));
            dust.noGravity = true;

            if (Main.rand.NextBool(4))
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    edge,
                    -Projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.4f, 1.2f),
                    "CalamityMod/Particles/BloomCircle",
                    false,
                    Main.rand.Next(10, 15),
                    Main.rand.NextFloat(0.18f, 0.36f),
                    Main.rand.NextBool() ? WaveGold : Color.White,
                    new Vector2(0.45f, 1.1f),
                    shrinkSpeed: 0.65f));
            }
        }

        private void TriggerImpactBurst(Vector2 impactPoint)
        {
            if (burstSpawned || Main.dedServ)
                return;
            burstSpawned = true;

            // Large gold bloom at impact
            GeneralParticleHandler.SpawnParticle(new StrongBloom(impactPoint, Vector2.Zero, WaveGold, 0.85f, 14));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(impactPoint, Vector2.Zero, WaveGold, Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.08f, 1.8f, 20));

            // Sparkle spray at impact point
            for (int i = 0; i < 16; i++)
            {
                Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 12f);
                Dust d = Dust.NewDustPerfect(impactPoint, DustID.GoldFlame, vel, 0, Main.rand.NextBool(3) ? Color.White : WaveGold, Main.rand.NextFloat(0.8f, 1.3f));
                d.noGravity = true;
            }

            // GenericSparkle burst from Judgment technique
            for (int i = 0; i < 8; i++)
            {
                float angle = MathHelper.TwoPi * i / 8f;
                Vector2 sparkPos = impactPoint + angle.ToRotationVector2() * Main.rand.NextFloat(8f, 28f);
                GeneralParticleHandler.SpawnParticle(new GenericSparkle(
                    sparkPos,
                    angle.ToRotationVector2() * Main.rand.NextFloat(1f, 4f),
                    WaveWhite,
                    WaveGold,
                    Main.rand.NextFloat(0.4f, 0.7f),
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(-0.12f, 0.12f),
                    2.5f));
            }

            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.45f, Pitch = 0.18f }, impactPoint);
        }
    }
}
