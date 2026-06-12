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

            if (projectile.owner == Main.myPlayer && TryAbsorbNearbyBlackSun(projectile))
                return;

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

        private static bool TryAbsorbNearbyBlackSun(Projectile projectile)
        {
            int sunType = ModContent.ProjectileType<DarksunFragmentBlackSun>();
            foreach (Projectile other in Main.ActiveProjectiles)
            {
                if (other.type != sunType || other.owner != projectile.owner)
                    continue;

                float absorbRadius = DarksunFragmentBlackSun.GetRadiusForLevel((int)other.ai[0]);
                if (Vector2.Distance(other.Center, projectile.Center) > absorbRadius)
                    continue;

                projectile.GetGlobalProjectile<DarksunFragmentOrbGlobalProjectile>().hitSomething = true;
                DarksunFragmentBlackSun.UpgradeOrExplode(other);
                projectile.Kill();
                return true;
            }

            return false;
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
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D softRing = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_03").Value;
            Texture2D reticle = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_04").Value;
            Vector2 drawPos = projectile.Center - Main.screenPosition;
            float opacity = MathHelper.Clamp(projectile.timeLeft / 18f, 0f, 1f);

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, drawPos, null, new Color(255, 190, 44, 0) * (0.34f * opacity), projectile.rotation, bloom.Size() * 0.5f, projectile.scale * 0.28f, SpriteEffects.None);
            for (int i = 0; i < 4; i++)
            {
                float rotation = Main.GlobalTimeWrappedHourly * (2.8f + i * 0.4f) + i * MathHelper.PiOver2;
                Color color = new Color(255, 205, 68) * (0.48f - i * 0.065f);
                color.A = 0;
                Main.EntitySpriteDraw(ring, drawPos, null, color, rotation, ring.Size() * 0.5f, projectile.scale * (0.2f + i * 0.035f), SpriteEffects.None);
            }
            Main.EntitySpriteDraw(softRing, drawPos, null, new Color(255, 150, 34, 0) * (0.28f * opacity), projectile.rotation * 0.7f, softRing.Size() * 0.5f, projectile.scale * 0.13f, SpriteEffects.None);
            Main.EntitySpriteDraw(reticle, drawPos, null, new Color(255, 218, 84, 0) * (0.18f * opacity), -projectile.rotation * 0.55f, reticle.Size() * 0.5f, projectile.scale * 0.11f, SpriteEffects.FlipHorizontally);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        private static void SpawnOrUpgradeBlackSun(Projectile projectile, Player owner)
        {
            int sunType = ModContent.ProjectileType<DarksunFragmentBlackSun>();
            foreach (Projectile other in Main.ActiveProjectiles)
            {
                if (other.type != sunType || other.owner != projectile.owner)
                    continue;

                float otherRadius = DarksunFragmentBlackSun.GetRadiusForLevel((int)other.ai[0]);
                if (Vector2.Distance(other.Center, projectile.Center) > otherRadius)
                    continue;

                DarksunFragmentBlackSun.UpgradeOrExplode(other);
                return;
            }

            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                Vector2.Zero,
                sunType,
                Math.Max(1, (int)(projectile.damage * 1f)),
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
