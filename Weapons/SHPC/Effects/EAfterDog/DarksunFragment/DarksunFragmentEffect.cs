using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
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
        }

        public override void PostDraw(Projectile projectile, Player owner, SpriteBatch spriteBatch)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Vector2 drawPos = projectile.Center - Main.screenPosition;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (int i = 0; i < 4; i++)
            {
                float rotation = Main.GlobalTimeWrappedHourly * (2.8f + i * 0.4f) + i * MathHelper.PiOver2;
                Color color = (i % 2 == 0 ? new Color(255, 205, 68) : Color.Black) * 0.48f;
                color.A = 0;
                Main.EntitySpriteDraw(ring, drawPos, null, color, rotation, ring.Size() * 0.5f, projectile.scale * (0.2f + i * 0.035f), SpriteEffects.None);
            }

            Main.EntitySpriteDraw(bloom, drawPos, null, new Color(255, 190, 48, 0) * 0.44f, 0f, bloom.Size() * 0.5f, projectile.scale * 0.42f, SpriteEffects.None);
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
