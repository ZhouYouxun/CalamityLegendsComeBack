using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Particles;
using CalamityMod.Items.Materials;
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
            Vector2 forward = projectile.velocity.SafeNormalize(owner.direction == 0 ? Vector2.UnitX : new Vector2(owner.direction, 0f));
            if (forward == Vector2.Zero)
                forward = Vector2.UnitX;

            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 targetPoint = FindHistoricalTarget(projectile.Center, forward, 1080f);
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
                Vector2 midpoint = (spawnPosition + targetPoint) * 0.5f;
                float sideOffset = widerArc ? 144f : 88f;
                float forwardOffset = widerArc ? 112f : 64f;
                Vector2 controlPoint = midpoint + normal * sideSign * sideOffset + forward * forwardOffset;
                Color themeColor = AscendantSpirit_PROJ.RandomThemeColor();

                int projectileIndex = Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    spawnPosition,
                    launchDirection * 12f,
                    ModContent.ProjectileType<AscendantSpirit_PROJ>(),
                    damage,
                    projectile.knockBack,
                    owner.whoAmI);

                if (Main.projectile.IndexInRange(projectileIndex) && Main.projectile[projectileIndex].ModProjectile is AscendantSpirit_PROJ spiritProjectile)
                {
                    spiritProjectile.InitializeCurve(spawnPosition, controlPoint, targetPoint, themeColor, widerArc ? 40f : 34f);
                    Main.projectile[projectileIndex].netUpdate = true;
                }

                SpawnBezierReleaseParticles(spawnPosition, controlPoint, targetPoint, themeColor, forward, normal * sideSign);
            }

            for (int i = 0; i < 8; i++)
            {
                Vector2 sparkleVelocity = forward.RotatedByRandom(0.32f) * Main.rand.NextFloat(2f, 5f);
                SquishyLightParticle sparkle = new(
                    projectile.Center,
                    sparkleVelocity,
                    Main.rand.NextFloat(0.24f, 0.38f),
                    Color.Lerp(new Color(120, 160, 255), Color.White, Main.rand.NextFloat(0.18f, 0.45f)),
                    Main.rand.Next(10, 18));

                GeneralParticleHandler.SpawnParticle(sparkle);
            }
        }

        private static Vector2 FindHistoricalTarget(Vector2 center, Vector2 forward, float maxDistance)
        {
            NPC bestTarget = null;
            float bestDistance = maxDistance;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;

                Vector2 toNpc = npc.Center - center;
                float distance = toNpc.Length();
                if (distance >= bestDistance)
                    continue;

                Vector2 directionToNpc = toNpc.SafeNormalize(Vector2.Zero);
                if (Vector2.Dot(forward, directionToNpc) < 0.18f)
                    continue;

                bestDistance = distance;
                bestTarget = npc;
            }

            return bestTarget?.Center ?? center + forward * 600f;
        }

        private static void SpawnBezierReleaseParticles(Vector2 start, Vector2 control, Vector2 end, Color color, Vector2 forward, Vector2 side)
        {
            for (int i = 0; i < 5; i++)
            {
                float completion = i / 4f;
                Vector2 point = EvaluateQuadratic(start, control, end, completion);
                Vector2 tangent = EvaluateQuadraticDerivative(start, control, end, completion).SafeNormalize(forward);
                Vector2 crossVelocity = side * Main.rand.NextFloat(0.4f, 1.4f);

                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    point,
                    tangent * Main.rand.NextFloat(2.4f, 4.6f) + crossVelocity,
                    "CalamityMod/Particles/BloomLineSoftEdge",
                    false,
                    Main.rand.Next(10, 16),
                    Main.rand.NextFloat(0.022f, 0.036f),
                    Color.Lerp(color, Color.White, 0.28f),
                    new Vector2(Main.rand.NextFloat(1f, 1.35f), Main.rand.NextFloat(0.45f, 0.72f)),
                    shrinkSpeed: 0.84f));

                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    point,
                    tangent * Main.rand.NextFloat(0.3f, 1.1f) - crossVelocity * 0.25f,
                    Main.rand.NextFloat(0.16f, 0.24f),
                    color,
                    Main.rand.Next(10, 15)));
            }

            for (int i = 0; i < 3; i++)
            {
                Vector2 ringVelocity = (forward + side * Main.rand.NextFloat(-0.45f, 0.45f)).SafeNormalize(forward) * Main.rand.NextFloat(2.2f, 4.4f);
                GlowOrbParticle orb = new(
                    start + side * Main.rand.NextFloat(-8f, 8f),
                    ringVelocity,
                    false,
                    Main.rand.Next(9, 14),
                    Main.rand.NextFloat(0.38f, 0.55f),
                    Color.Lerp(color, Color.White, 0.18f),
                    true,
                    true);

                GeneralParticleHandler.SpawnParticle(orb);
            }
        }

        private static Vector2 EvaluateQuadratic(Vector2 start, Vector2 control, Vector2 end, float completion)
        {
            Vector2 firstLerp = Vector2.Lerp(start, control, completion);
            Vector2 secondLerp = Vector2.Lerp(control, end, completion);
            return Vector2.Lerp(firstLerp, secondLerp, completion);
        }

        private static Vector2 EvaluateQuadraticDerivative(Vector2 start, Vector2 control, Vector2 end, float completion)
        {
            return 2f * (1f - completion) * (control - start) + 2f * completion * (end - control);
        }
    }

    internal class AscendantSpiritEffectGlobalProjectile : GlobalProjectile
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override bool InstancePerEntity => true;

        public bool firstFrame;
    }
}
