using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    internal class SHPCNecroplasmDamage : ModProjectile, ILocalizedModType
    {
        private static readonly Color OuterPink = new(255, 80, 180);
        private static readonly Color InnerPink = new(255, 188, 226);
        private static readonly Color DeepPink = new(200, 40, 140);

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 20;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;

            Projectile.friendly = true;
            Projectile.tileCollide = false;

            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        // ===== 飞行逻辑：纯减速 =====
        public override void AI()
        {
            timer++; // 计时器

            if (!Main.dedServ)
            {
                // Shadowbolt's opening flash is translated into a short, flattened pink ripple.
                // It never changes this projectile's original direction or damage timing.
                if (timer == 14)
                    SpawnLaunchRipple();

                if (timer >= 22 && timer % 4 == 0)
                    SpawnTrailingMote();
            }

            if (timer > 20)
            {
                NPC target = FindTarget(1400f);
                if (target != null)
                {
                    if (!trackingFlarePlayed && !Main.dedServ)
                    {
                        SpawnTrackingRipple();
                        trackingFlarePlayed = true;
                    }

                    float trackingPower = Utils.GetLerpValue(20f, 120f, timer, true);
                    float lateLifeBoost = Projectile.timeLeft < 90 ? 2f : 1f;
                    float speed = MathHelper.Lerp(8f, 19f, trackingPower) * lateLifeBoost;
                    Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)) * speed;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, MathHelper.Lerp(0.08f, 0.34f, trackingPower));
                    return;
                }
            }

            Projectile.velocity *= 0.965f;
        }

        private NPC FindTarget(float maxDistance)
        {
            NPC result = null;
            float bestDistance = maxDistance;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                result = npc;
            }

            return result;
        }

        private void SpawnLaunchRipple()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float rotation = direction.ToRotation();
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                Color.Lerp(OuterPink, InnerPink, Main.rand.NextFloat()),
                "CalamityMod/Particles/SmallBloomRing",
                new Vector2(0.78f, 0.32f),
                rotation,
                0.12f,
                0.70f,
                14,
                true));

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                direction * 0.12f,
                InnerPink,
                new Vector2(0.28f, 0.64f),
                rotation,
                0.07f,
                0.52f,
                12));
        }

        private void SpawnTrackingRipple()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                direction * 0.16f,
                Color.Lerp(OuterPink, DeepPink, 0.4f),
                new Vector2(0.38f, 0.78f),
                direction.ToRotation(),
                0.06f,
                0.68f,
                16));
        }

        private void SpawnTrailingMote()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                Projectile.Center - direction * Main.rand.NextFloat(5f, 12f) + Main.rand.NextVector2Circular(2f, 2f),
                -direction * Main.rand.NextFloat(0.3f, 1.15f),
                false,
                Main.rand.Next(10, 15),
                Main.rand.NextFloat(0.09f, 0.14f),
                Color.Lerp(OuterPink, InnerPink, Main.rand.NextFloat()),
                true,
                false));
        }

        // ===== 视觉完全复刻 =====
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D lightTexture = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SmallGreyscaleCircle").Value;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float colorInterpolation =
                    (float)Math.Cos(
                        Projectile.timeLeft / 32f +
                        Main.GlobalTimeWrappedHourly / 20f +
                        i / (float)Projectile.oldPos.Length * MathHelper.Pi
                    ) * 0.5f + 0.5f;

                Color color = Color.Lerp(
                    new Color(255, 80, 180),
                    new Color(200, 40, 140),
                    colorInterpolation
                ) * 0.8f;

                color.A = 255;

                Vector2 drawPosition =
                    Projectile.oldPos[i]
                    + lightTexture.Size() * 0.5f
                    - Main.screenPosition
                    + new Vector2(0f, Projectile.gfxOffY)
                    + new Vector2(-28f, -28f);

                Color outerColor = color;
                Color innerColor = color * 0.5f;

                float intensity =
                    0.9f + 0.15f *
                    (float)Math.Cos(Main.GlobalTimeWrappedHourly % 60f * MathHelper.TwoPi);

                intensity *= MathHelper.Lerp(
                    0.15f,
                    1f,
                    1f - i / (float)Projectile.oldPos.Length
                );

                if (Projectile.timeLeft <= 60)
                    intensity *= Projectile.timeLeft / 60f;

                Vector2 outerScale = new Vector2(1f) * intensity;
                Vector2 innerScale = new Vector2(1f) * intensity * 0.7f;

                outerColor *= intensity;
                innerColor *= intensity;

                Main.EntitySpriteDraw(
                    lightTexture,
                    drawPosition,
                    null,
                    outerColor,
                    0f,
                    lightTexture.Size() * 0.5f,
                    outerScale * 0.6f,
                    SpriteEffects.None,
                    0
                );

                Main.EntitySpriteDraw(
                    lightTexture,
                    drawPosition,
                    null,
                    innerColor,
                    0f,
                    lightTexture.Size() * 0.5f,
                    innerScale * 0.6f,
                    SpriteEffects.None,
                    0
                );
            }

            DrawSpectralComet(lightTexture);
            return false;
        }

        // A rose-colored, horizontally stretched spectral wake. This is deliberately made from
        // the orb's existing circle texture instead of Shadowbolt's tall GlowSpark silhouette.
        private void DrawSpectralComet(Texture2D lightTexture)
        {
            if (timer < 20 || Projectile.velocity.LengthSquared() < 0.01f)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float lifeFade = Utils.GetLerpValue(0f, 24f, timer, true) * Utils.GetLerpValue(0f, 48f, Projectile.timeLeft, true);
            float speedStretch = MathHelper.Clamp(Projectile.velocity.Length() / 13f, 0.65f, 1.35f);
            float pulse = 0.88f + MathF.Sin(Main.GlobalTimeWrappedHourly * 8f + Projectile.identity) * 0.12f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (int i = 0; i < 3; i++)
            {
                float layerProgress = i / 2f;
                float layerFade = (1f - layerProgress * 0.52f) * lifeFade;
                Color color = Color.Lerp(InnerPink, DeepPink, layerProgress) * (0.22f * layerFade * pulse);
                Vector2 position = drawPosition - direction * (5f + i * 7f);
                Vector2 scale = new(1.35f + speedStretch * 0.78f, 0.20f + i * 0.07f);
                scale *= (1f - layerProgress * 0.16f) * pulse;

                Main.EntitySpriteDraw(
                    lightTexture,
                    position,
                    null,
                    color,
                    direction.ToRotation(),
                    lightTexture.Size() * 0.5f,
                    scale,
                    SpriteEffects.None,
                    0);
            }
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        // ===== 全部留空 =====
        public override void OnSpawn(Terraria.DataStructures.IEntitySource source) { }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.dedServ)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                direction * 0.08f,
                InnerPink,
                "CalamityMod/Particles/SmallBloomRing",
                new Vector2(0.68f, 0.28f),
                direction.ToRotation(),
                0.08f,
                0.64f,
                12,
                true));
        }

        public override void OnKill(int timeLeft) { }


        private int timer;
        private bool trackingFlarePlayed;

        public override bool? CanDamage()
        {
            if (timer < 20)
                return false;
            return null;
        }
    }
}
