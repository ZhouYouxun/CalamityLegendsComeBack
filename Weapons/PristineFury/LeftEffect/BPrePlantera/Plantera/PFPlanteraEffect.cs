using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod.Particles;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFPlanteraEffect
    {
        private const int WarmupFrames = 15;

        internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (!held)
            {
                PristineFuryLeftEffectRegistry.Reset(holdout);
                return;
            }

            // Increment charge timer during hold
            holdout.LeftChargeTimer++;

            int cycleFrame = holdout.LeftChargeTimer;

            if (cycleFrame < WarmupFrames)
            {
                SpawnWarmupEffects(holdout);
            }
            else if (cycleFrame == 15)
            {
                FireOpticalArrow(holdout, 0);
            }
            else if (cycleFrame == 18)
            {
                FireOpticalArrow(holdout, 1);
            }
            else if (cycleFrame == 21)
            {
                FireOpticalArrow(holdout, 2);
            }
            else if (cycleFrame == 24)
            {
                FireOpticalArrow(holdout, 3);
            }
            else if (cycleFrame >= 25)
            {
                // Reset to 0 to restart the 15-frame charge and burst cycle
                holdout.LeftChargeTimer = 0;
            }
        }

        private static void SpawnWarmupEffects(NewLegendPristineFuryHoldOut holdout)
        {
            if (Main.dedServ)
                return;

            if (holdout.LeftChargeTimer == 1)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/ArcNovaDiffuserChargeStart") { Volume = 0.5f, Pitch = 0.25f, MaxInstances = 1 }, holdout.GunTipPosition);
            }

            Vector2 tip = holdout.GunTipPosition;
            Color themeColor = new Color(255, 150, 20); // Warm reddish-yellow theme

            // Collapsing particles into gun tip
            for (int i = 0; i < 2; i++)
            {
                float radius = Main.rand.NextFloat(18f, 36f);
                Vector2 spawnPos = tip + Main.rand.NextVector2CircularEdge(radius, radius);
                Vector2 velocity = (tip - spawnPos) * Main.rand.NextFloat(0.08f, 0.14f);
                Color color = Color.Lerp(themeColor, Color.White, Main.rand.NextFloat(0.1f, 0.35f));

                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    spawnPos,
                    velocity,
                    false,
                    10,
                    Main.rand.NextFloat(0.08f, 0.13f),
                    color * 0.82f,
                    true,
                    false,
                    true
                ));
            }

            if (Main.rand.NextBool(2))
            {
                float radius = Main.rand.NextFloat(20f, 40f);
                Vector2 spawnPos = tip + Main.rand.NextVector2CircularEdge(radius, radius);
                Vector2 velocity = (tip - spawnPos) * Main.rand.NextFloat(0.09f, 0.15f);
                Color color = Color.Lerp(themeColor, Color.White, Main.rand.NextFloat(0.2f, 0.45f));

                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    spawnPos,
                    velocity,
                    "CalamityMod/Particles/ThinEndedLine",
                    false,
                    11,
                    Main.rand.NextFloat(0.06f, 0.11f),
                    color,
                    new Vector2(0.35f, 1.25f),
                    true,
                    false
                ));
            }

            if (holdout.LeftChargeTimer % 5 == 1)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    tip,
                    Vector2.Zero,
                    themeColor * 0.42f,
                    Vector2.One * 0.85f,
                    holdout.AimDirection.ToRotation(),
                    -0.05f, // shrink rate
                    0.022f,
                    10
                ));
            }
        }

        private static void FireOpticalArrow(NewLegendPristineFuryHoldOut holdout, int shotIndex)
        {
            Vector2 direction = holdout.AimDirection;
            Vector2 side = direction.RotatedBy(MathHelper.PiOver2);

            // Alternating offsets for dual-barreled rail feel
            float sideOffset = 0f;
            if (shotIndex == 0) sideOffset = -8f;
            else if (shotIndex == 1) sideOffset = 8f;
            else if (shotIndex == 2) sideOffset = -4f;
            else if (shotIndex == 3) sideOffset = 4f;

            Vector2 spawnPosition = holdout.GunTipPosition + direction * 15f + side * sideOffset;
            Vector2 velocity = direction.RotatedBy(Main.rand.NextFloat(-0.015f, 0.015f)) * 16f; // Reduced speed by 33%

            // Spawn slightly behind muzzle to let the trail look complete on frame 1
            spawnPosition -= 3f * velocity;

            int projectileIndex = Projectile.NewProjectile(
                holdout.Projectile.GetSource_FromThis(),
                spawnPosition,
                velocity,
                ModContent.ProjectileType<PFPlantera_Flame>(),
                holdout.GetScaledDamage(0.85f),
                holdout.Projectile.knockBack,
                holdout.Projectile.owner);
            PFLeftEffectRules.ApplyTheme(projectileIndex, holdout.CurrentMark);

            holdout.ApplyRecoil(2.8f);
            holdout.TriggerMuzzleFlash(6);
            holdout.SpawnMuzzleBurst(new Color(255, 150, 20), 0.85f); // Reddish-yellow muzzle burst

            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/ProfanedGuardians/GuardianDash") { Volume = 0.15f, Pitch = Main.rand.NextFloat(-0.25f, -0.55f), MaxInstances = 6 }, holdout.GunTipPosition);
        }
    }
}
