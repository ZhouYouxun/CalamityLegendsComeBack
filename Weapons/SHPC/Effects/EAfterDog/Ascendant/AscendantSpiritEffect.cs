using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Dusts;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Ascendant
{
    internal class AscendantSpiritEffect : DefaultEffect
    {
        public override int EffectID => 36;

        public override int AmmoType => ModContent.ItemType<AscendantSpiritEssence>();

        public override Color ThemeColor => new Color(120, 160, 255);
        public override Color StartColor => new Color(200, 220, 255);
        public override Color EndColor => new Color(40, 60, 120);
        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;
        public override bool EnableDefaultSlowdown => false;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.GetGlobalProjectile<AscendantSpiritEffectGlobalProjectile>().firstFrame = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 2;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            AscendantSpiritEffectGlobalProjectile globalProjectile = projectile.GetGlobalProjectile<AscendantSpiritEffectGlobalProjectile>();
            if (!globalProjectile.firstFrame)
                return;

            globalProjectile.firstFrame = false;
            projectile.Kill();
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            if (owner.whoAmI != Main.myPlayer)
                return;

            Vector2 forward = projectile.velocity.SafeNormalize(owner.direction == 0 ? Vector2.UnitX : new Vector2(owner.direction, 0f));
            if (forward == Vector2.Zero)
                forward = Vector2.UnitX;

            Vector2 targetPoint = Main.MouseWorld;
            if (float.IsNaN(targetPoint.X) || float.IsNaN(targetPoint.Y) || Vector2.Distance(projectile.Center, targetPoint) < 24f)
                targetPoint = projectile.Center + forward * 560f;

            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);
            int damage = (int)(projectile.damage * 1.5f);
            float[] angleOffsets = { -0.42f, -0.18f, 0.18f, 0.42f };

            for (int i = 0; i < angleOffsets.Length; i++)
            {
                float angleOffset = angleOffsets[i];
                float sideSign = Math.Sign(angleOffset);
                if (sideSign == 0f)
                    sideSign = 1f;

                bool widerArc = Math.Abs(angleOffset) > 0.3f;
                Vector2 spawnOffset = normal * sideSign * (widerArc ? 9f : 4f) + forward * (widerArc ? 6f : 2f);
                Vector2 spawnPosition = projectile.Center + spawnOffset;
                Vector2 launchDirection = forward.RotatedBy(angleOffset).SafeNormalize(forward);
                Color themeColor = AscendantSpirit_PROJ.RandomThemeColor();
                float launchDelay = (widerArc ? 15f : 11f) + i * 1.15f;

                int projectileIndex = Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    spawnPosition,
                    launchDirection * 8.2f,
                    ModContent.ProjectileType<AscendantSpirit_PROJ>(),
                    damage,
                    projectile.knockBack,
                    owner.whoAmI,
                    targetPoint.X,
                    targetPoint.Y,
                    launchDelay);

                if (Main.projectile.IndexInRange(projectileIndex) && Main.projectile[projectileIndex].ModProjectile is AscendantSpirit_PROJ spiritProjectile)
                {
                    spiritProjectile.InitializeNeedle(targetPoint, themeColor, launchDelay);
                    Main.projectile[projectileIndex].netUpdate = true;
                }

                SpawnNeedleReleaseParticles(spawnPosition, launchDirection, themeColor, widerArc);
            }

            SpawnCentralReleaseParticles(projectile.Center, forward);
        }

        private static void SpawnNeedleReleaseParticles(Vector2 spawnPosition, Vector2 launchDirection, Color color, bool widerArc)
        {
            Vector2 normal = launchDirection.RotatedBy(MathHelper.PiOver2);
            int dustCount = widerArc ? 8 : 6;

            for (int i = 0; i < dustCount; i++)
            {
                Dust dust = Dust.NewDustPerfect(spawnPosition, ModContent.DustType<SquashDust>());
                dust.scale = Main.rand.NextFloat(0.95f, 1.55f);
                dust.velocity = launchDirection.RotatedByRandom(0.36f) * Main.rand.NextFloat(4.4f, 7.4f);
                dust.noGravity = true;
                dust.color = Color.Lerp(color, Color.White, Main.rand.NextFloat(0.08f, 0.3f));
                dust.fadeIn = Main.rand.NextFloat(1.1f, 2.2f);
            }

            Particle releaseBloom = new CustomSpark(
                spawnPosition,
                launchDirection * 0.5f,
                "CalamityMod/Particles/BloomCircle",
                false,
                widerArc ? 20 : 16,
                widerArc ? 0.34f : 0.26f,
                color,
                new Vector2(0.72f, 1.22f),
                glowCenter: true,
                shrinkSpeed: 0.18f,
                glowOpacity: 0.72f,
                extraRotation: launchDirection.ToRotation());
            GeneralParticleHandler.SpawnParticle(releaseBloom);

            for (int i = 0; i < 4; i++)
            {
                Particle star = new CustomSpark(
                    spawnPosition + normal * Main.rand.NextFloat(-5f, 5f),
                    launchDirection.RotatedByRandom(0.28f) * Main.rand.NextFloat(2.5f, 5.2f),
                    "CalamityMod/Particles/PulseStar",
                    false,
                    Main.rand.Next(13, 21),
                    Main.rand.NextFloat(0.08f, 0.16f),
                    Color.Lerp(color, Color.White, 0.2f),
                    Vector2.One,
                    glowCenter: true,
                    shrinkSpeed: 0.22f,
                    glowOpacity: 0.68f);
                GeneralParticleHandler.SpawnParticle(star);
            }

            for (int i = 0; i < 3; i++)
            {
                Particle forwardSmear = new CustomSpark(
                    spawnPosition + Main.rand.NextVector2CircularEdge(3f, 3f),
                    launchDirection * Main.rand.NextFloat(4f, 8f) + normal * Main.rand.NextFloat(-1.2f, 1.2f),
                    "CalamityMod/Particles/ForwardSmear",
                    false,
                    Main.rand.Next(9, 15),
                    Main.rand.NextFloat(0.11f, 0.19f),
                    Color.Lerp(color, Color.White, Main.rand.NextFloat(0.1f, 0.35f)),
                    Vector2.One,
                    shrinkSpeed: 0.34f);
                GeneralParticleHandler.SpawnParticle(forwardSmear);
            }
        }

        private static void SpawnCentralReleaseParticles(Vector2 center, Vector2 forward)
        {
            float rotation = Main.rand.NextFloat(MathHelper.TwoPi);

            for (int ring = 1; ring <= 4; ring++)
            {
                for (int i = 0; i < 5; i++)
                {
                    Color color = AscendantSpirit_PROJ.RandomThemeColor();
                    Dust dust = Dust.NewDustPerfect(center, ModContent.DustType<SquashDust>());
                    dust.scale = 4.8f - ring * 0.5f;
                    dust.velocity = -Vector2.UnitY.RotatedBy(MathHelper.TwoPi / 5f * i + rotation) * (ring * 1.35f + 1.5f);
                    dust.noGravity = true;
                    dust.color = color;
                    dust.fadeIn = 5.2f - ring * 0.36f;
                }
            }

            for (int i = 0; i < 12; i++)
            {
                Color color = AscendantSpirit_PROJ.RandomThemeColor();
                Particle sparkle = new CustomSpark(
                    center,
                    forward.RotatedByRandom(0.55f) * Main.rand.NextFloat(2.2f, 6f),
                    "CalamityMod/Particles/PulseStar",
                    false,
                    Main.rand.Next(14, 23),
                    Main.rand.NextFloat(0.08f, 0.17f),
                    Color.Lerp(color, Color.White, Main.rand.NextFloat(0.12f, 0.35f)),
                    Vector2.One,
                    glowCenter: true,
                    shrinkSpeed: 0.24f,
                    glowOpacity: 0.7f);
                GeneralParticleHandler.SpawnParticle(sparkle);
            }

            Particle bloom = new CustomSpark(
                center,
                Vector2.Zero,
                "CalamityMod/Particles/BloomCircle",
                false,
                24,
                0.74f,
                Color.Lerp(new Color(120, 160, 255), Color.White, 0.22f),
                Vector2.One,
                true,
                true,
                0,
                false,
                false,
                glowOpacity: 0.82f);
            GeneralParticleHandler.SpawnParticle(bloom);
        }
    }

    internal class AscendantSpiritEffectGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool firstFrame;
    }
}
