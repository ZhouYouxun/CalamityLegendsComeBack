using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public class CosmicDischargeDoGConvergenceExplosion : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Time => ref Projectile.ai[0];
        private ref float Radius => ref Projectile.ai[1];

        public override void SetDefaults()
        {
            Projectile.width = 220;
            Projectile.height = 220;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => Time <= 16f || Projectile.timeLeft % 6 == 0;

        public override void AI()
        {
            Time++;
            if (Radius <= 0f)
                Radius = 145f;

            Projectile.Resize((int)(Radius * 2f), (int)(Radius * 2f));
            // Fade in over 8 frames, fade out over last 30 frames of the 120-frame lifetime.
            Projectile.Opacity = Utils.GetLerpValue(0f, 8f, Time, true) * Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, CosmicDischargeCommon.DoGSpecialColor.ToVector3() * 0.72f * Projectile.Opacity);

            if (Time == 1f)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DoGLaserWallBigAttack") { Volume = 0.58f, Pitch = 0.18f, MaxInstances = 3 }, Projectile.Center);
                CosmicDischargeCommon.SpawnRiftCrackProjectiles(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.owner, 8, 3f, 10f, 14f, 24f);

                if (!Main.dedServ)
                {
                    CosmicDischargeCommon.SpawnDistortionBurst(Projectile.Center, 12, 6, 58f, 38f);
                    CosmicDischargeCommon.SpawnCustomPulse(Projectile.Center, CosmicDischargeCommon.DoGWhiteColor, 0.3f, 2.8f, "CalamityMod/Particles/PlasmaExplosion", 22);
                    CosmicDischargeCommon.SpawnCustomPulse(Projectile.Center, CosmicDischargeCommon.DoGCyanColor, 0.5f, 3.2f, "CalamityMod/Particles/ShineExplosion1", 24);
                    GeneralParticleHandler.SpawnParticle(new DetailedExplosion(Projectile.Center, Vector2.Zero, CosmicDischargeCommon.DoGCyanColor, new Vector2(1.2f, 0.75f), 0f, 0.28f * 0.3f, 1.8f * 0.3f, 22));
                    GeneralParticleHandler.SpawnParticle(new DetailedExplosion(Projectile.Center, Vector2.Zero, CosmicDischargeCommon.DoGFuchsiaColor, new Vector2(0.75f, 1.2f), MathHelper.PiOver4, 0.28f * 0.3f, 1.6f * 0.3f, 20));
                    GeneralParticleHandler.SpawnParticle(new DetailedExplosion(Projectile.Center, Vector2.Zero, CosmicDischargeCommon.DoGWhiteColor, Vector2.One, MathHelper.Pi / 3f, 0.40f * 0.3f, 1.4f * 0.3f, 18));
                    GeneralParticleHandler.SpawnParticle(new StrongBloom(Projectile.Center, Vector2.Zero, CosmicDischargeCommon.DoGWhiteColor, 2.4f * 0.3f, 24));
                }
            }

            // Periodic explosion burst every 14 frames — sustained detonation feel.
            if (!Main.dedServ && Time % 14 == 0 && Time > 1f)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerAttack") { Volume = 0.30f, Pitch = Main.rand.NextFloat(-0.35f, 0.25f), MaxInstances = 6 }, Projectile.Center);

                for (int k = 0; k < 2; k++)
                {
                    Vector2 offset = Main.rand.NextVector2Circular(Radius * 0.55f, Radius * 0.55f);
                    GeneralParticleHandler.SpawnParticle(new DetailedExplosion(
                        Projectile.Center + offset,
                        Main.rand.NextVector2Circular(0.4f, 0.4f),
                        k == 0 ? CosmicDischargeCommon.DoGCyanColor : CosmicDischargeCommon.DoGFuchsiaColor,
                        new Vector2(Main.rand.NextFloat(0.7f, 1.15f), Main.rand.NextFloat(0.6f, 1.05f)),
                        Main.rand.NextFloat(MathHelper.TwoPi),
                        0.20f * 0.3f,
                        1.5f * 0.3f,
                        18));
                }
                GeneralParticleHandler.SpawnParticle(new StrongBloom(
                    Projectile.Center + Main.rand.NextVector2Circular(Radius * 0.45f, Radius * 0.45f),
                    Vector2.Zero,
                    CosmicDischargeCommon.DoGWhiteColor,
                    1.1f * 0.3f,
                    14));
            }

            // Pulse shockwave every 28 frames.
            if (!Main.dedServ && Time % 28 == 0 && Time > 1f)
            {
                GeneralParticleHandler.SpawnParticle(new PulseRing(
                    Projectile.Center,
                    Vector2.Zero,
                    CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGSpecialColor),
                    0.04f,
                    Radius / 95f * 0.3f * CosmicDischargeCommon.ShockwaveFinalScaleMultiplier,
                    20));
                CosmicDischargeCommon.SpawnCustomPulse(Projectile.Center, CosmicDischargeCommon.DoGSpecialColor * 0.65f, 0.18f, Radius / 50f, "CalamityMod/Particles/PlasmaExplosion", 18);
            }

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                GeneralParticleHandler.SpawnParticle(new StaticGlowLine(
                    Projectile.Center,
                    Projectile.Center + direction * Main.rand.NextFloat(60f, Radius),
                    direction * 0.4f,
                    14,
                    0.07f * 0.3f,
                    0.9f,
                    Main.rand.NextBool() ? CosmicDischargeCommon.DoGCyanColor : CosmicDischargeCommon.DoGFuchsiaColor));
                GeneralParticleHandler.SpawnParticle(new NanoParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(Radius * 0.7f, Radius * 0.7f),
                    Main.rand.NextVector2Circular(2f, 2f),
                    CosmicDischargeCommon.DoGSpecialColor,
                    0.35f * 0.3f,
                    18,
                    emitsLight: true));

                // Additional energy fragments flying outward.
                if (Main.rand.NextBool(3))
                {
                    Vector2 fragDir = Main.rand.NextVector2CircularEdge(1f, 1f);
                    GeneralParticleHandler.SpawnParticle(new BoltParticle(
                        Projectile.Center + fragDir * Main.rand.NextFloat(20f, Radius * 0.6f),
                        fragDir * Main.rand.NextFloat(1.5f, 4f),
                        false,
                        Main.rand.Next(8, 14),
                        Main.rand.NextFloat(0.28f, 0.50f) * 0.3f,
                        Main.rand.NextBool() ? CosmicDischargeCommon.DoGCyanColor : CosmicDischargeCommon.DoGFuchsiaColor,
                        new Vector2(0.08f, 3.0f),
                        true,
                        true));
                }
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = Vector2.Clamp(targetHitbox.Center.ToVector2(), targetHitbox.TopLeft(), targetHitbox.BottomRight());
            // Expand from 40% to full radius over first 18 frames for a dramatic initial shockwave.
            float pulseRadius = Radius * MathHelper.Lerp(0.4f, 1f, Utils.GetLerpValue(0f, 18f, Time, true));
            return Vector2.DistanceSquared(closest, Projectile.Center) <= pulseRadius * pulseRadius;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CosmicDischargeCommon.ApplyDoGDebuffs(target, 300);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D portal = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Melee/StreamGougePortal").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 bloomOrigin = bloom.Size() * 0.5f;
            Vector2 portalOrigin = portal.Size() * 0.5f;
            float pulse = 0.82f + 0.18f * MathF.Sin(Time * 0.55f);
            float scale = Radius / 110f * Projectile.Opacity;
            float rotation = Main.GlobalTimeWrappedHourly * 7.5f + Projectile.identity * 0.18f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, drawPosition, null, CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGFuchsiaColor) * 0.22f * Projectile.Opacity, 0f, bloomOrigin, scale * 1.35f * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(portal, drawPosition, null, Color.Black * 0.42f * Projectile.Opacity, rotation, portalOrigin, scale * 0.8f, SpriteEffects.None);
            Main.EntitySpriteDraw(portal, drawPosition, null, CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGCyanColor) * 0.55f * Projectile.Opacity, rotation * 0.6f, portalOrigin, scale * 0.8f, SpriteEffects.None);
            Main.EntitySpriteDraw(portal, drawPosition, null, CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGFuchsiaColor) * 0.55f * Projectile.Opacity, -rotation * 0.7f, portalOrigin, scale * 0.8f, SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
