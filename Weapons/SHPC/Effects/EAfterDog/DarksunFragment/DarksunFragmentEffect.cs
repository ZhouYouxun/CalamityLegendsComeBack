using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityDarksunFragment = CalamityMod.Items.Materials.DarksunFragment;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.DarksunFragment
{
    internal class DarksunFragmentEffect : DefaultEffect
    {
        public const int DarksunEffectID = 42;

        public override int EffectID => DarksunEffectID;
        public override int AmmoType => ModContent.ItemType<CalamityDarksunFragment>();

        public override Color ThemeColor => new(30, 22, 10);
        public override Color StartColor => new(255, 210, 72);
        public override Color EndColor => new(5, 4, 3);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;
        public override float GlowScaleFactor => 0f;
        public override float GlowIntensityFactor => 0f;
        public override bool EnableDefaultSlowdown => false;

        public override void SetDefaults(Projectile projectile)
        {
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.penetrate = 1;
            projectile.timeLeft = 100;
            projectile.usesLocalNPCImmunity = true;
            projectile.localNPCHitCooldown = 10;
        }

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            SetDefaults(projectile);
            projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction) * 24f;
            projectile.GetGlobalProjectile<DarksunFragmentOrbGlobalProjectile>().hitSomething = false;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            projectile.ai[1] = 0f;
            projectile.ai[2] = 0f;
            projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction) * 24f;
            projectile.rotation += 0.38f * Math.Sign(projectile.velocity.X == 0f ? owner.direction : projectile.velocity.X);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    projectile.Center - projectile.velocity.SafeNormalize(Vector2.UnitX) * 12f + Main.rand.NextVector2Circular(8f, 8f),
                    DustID.GoldFlame,
                    -projectile.velocity.RotatedByRandom(0.28f) * Main.rand.NextFloat(0.08f, 0.22f),
                    0,
                    Main.rand.NextBool() ? new Color(255, 200, 55) : Color.Black,
                    Main.rand.NextFloat(0.8f, 1.25f));
                dust.noGravity = true;
            }

            Lighting.AddLight(projectile.Center, new Vector3(1f, 0.68f, 0.12f) * 0.45f);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            projectile.GetGlobalProjectile<DarksunFragmentOrbGlobalProjectile>().hitSomething = true;
            if (projectile.owner == Main.myPlayer)
                SpawnOrUpgradeBlackSun(projectile, owner);

            projectile.Kill();
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                projectile.Center,
                forward * 0.6f,
                new Color(255, 190, 48),
                new Vector2(1f, 2.2f),
                forward.ToRotation(),
                0.12f,
                0.025f,
                16));

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                projectile.Center,
                Vector2.Zero,
                new Color(255, 190, 48),
                "CalamityMod/Particles/BloomRing",
                Vector2.One,
                Main.rand.NextFloat(-0.3f, 0.3f),
                0.04f,
                0.2f,
                14));

            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = forward.RotatedByRandom(0.9f) * Main.rand.NextFloat(1.6f, 5.8f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    projectile.Center,
                    velocity,
                    "CalamityMod/Particles/ForwardSmear",
                    false,
                    Main.rand.Next(9, 16),
                    Main.rand.NextFloat(0.08f, 0.16f),
                    Main.rand.NextBool(3) ? new Color(18, 12, 3) : new Color(255, 198, 54),
                    new Vector2(0.32f, 1.2f)));
            }
        }

        public override void PostDraw(Projectile projectile, Player owner, SpriteBatch spriteBatch)
        {
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D face = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/ScreamyFace").Value;
            Vector2 drawPos = projectile.Center - Main.screenPosition;
            float opacity = MathHelper.Clamp(projectile.timeLeft / 18f, 0f, 1f);

            Main.spriteBatch.End();
            Effect shieldEffect = Filters.Scene["CalamityMod:HellBall"].GetShader().Shader;
            Main.spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.AlphaBlend,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                Main.Rasterizer,
                shieldEffect,
                Main.GameViewMatrix.TransformationMatrix);

            shieldEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.25f);
            shieldEffect.Parameters["blowUpPower"].SetValue(2.9f);
            shieldEffect.Parameters["blowUpSize"].SetValue(0.32f);
            shieldEffect.Parameters["noiseScale"].SetValue(0.58f);
            shieldEffect.Parameters["shieldOpacity"].SetValue(0.86f * opacity);
            shieldEffect.Parameters["shieldEdgeBlendStrenght"].SetValue(4f);
            shieldEffect.Parameters["shieldColor"].SetValue(new Color(32, 18, 4).ToVector3());
            shieldEffect.Parameters["shieldEdgeColor"].SetValue(new Color(255, 198, 48).ToVector3());

            Main.spriteBatch.Draw(
                face,
                drawPos,
                null,
                Color.White * opacity,
                projectile.rotation * 0.4f,
                face.Size() * 0.5f,
                projectile.scale * 0.17f,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            Texture2D[] vortexTextures =
            {
                ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/Sun/fbmnoise2_003").Value,
                ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/Sun/fbmnoise2_004").Value,
                ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/Sun/fbmnoise2_005").Value,
                ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/Sun/fbmnoise2_006").Value,
                ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/Sun/gradationline_004").Value
            };

            for (int i = 0; i < 3; i++)
            {
                float angle = Main.GlobalTimeWrappedHourly * MathHelper.TwoPi * (0.35f + i * 0.08f) + i * MathHelper.TwoPi / 3f;
                Color darkColor = new Color(16 + i * 5, 10 + i * 4, 2, 116 - i * 18) * opacity;
                Vector2 offset = angle.ToRotationVector2() * (2f + i * 1.5f);
                foreach (Texture2D vortex in vortexTextures)
                {
                    Main.EntitySpriteDraw(
                        vortex,
                        drawPos + offset,
                        null,
                        darkColor,
                        -angle + MathHelper.PiOver2,
                        vortex.Size() * 0.5f,
                        projectile.scale * (0.18f + i * 0.035f),
                        SpriteEffects.None);
                }
            }

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (int i = 0; i < 4; i++)
            {
                float rotation = Main.GlobalTimeWrappedHourly * (2.8f + i * 0.4f) + i * MathHelper.PiOver2;
                Color color = new Color(255, 205, 68) * (0.48f - i * 0.065f);
                color.A = 0;
                Main.EntitySpriteDraw(ring, drawPos, null, color, rotation, ring.Size() * 0.5f, projectile.scale * (0.2f + i * 0.035f), SpriteEffects.None);
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        private static void SpawnOrUpgradeBlackSun(Projectile projectile, Player owner)
        {
            int sunType = ModContent.ProjectileType<DarksunFragmentBlackSun>();
            float overlapDistance = DarksunFragmentBlackSun.BaseRadius * 2.1f;

            foreach (Projectile other in Main.ActiveProjectiles)
            {
                if (other.type != sunType || other.owner != projectile.owner)
                    continue;

                float otherRadius = DarksunFragmentBlackSun.GetRadiusForLevel((int)other.ai[0]);
                if (Vector2.Distance(other.Center, projectile.Center) > overlapDistance + otherRadius)
                    continue;

                other.ai[0] = MathHelper.Clamp(other.ai[0] + 1f, 1f, DarksunFragmentBlackSun.MaxLevel);
                other.timeLeft = DarksunFragmentBlackSun.Lifetime;
                other.netUpdate = true;
                DarksunFragmentBlackSun.SpawnUpgradeBurst(other.Center, (int)other.ai[0]);
                return;
            }

            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                Vector2.Zero,
                sunType,
                Math.Max(1, (int)(projectile.damage * 0.34f)),
                projectile.knockBack,
                owner.whoAmI,
                1f);
        }
    }

    internal class DarksunFragmentOrbGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public bool hitSomething;
    }
}
