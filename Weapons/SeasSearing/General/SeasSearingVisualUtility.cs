using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    internal static class SeasSearingVisualUtility
    {
        public static void ShakeAt(Vector2 center, float power, float range = 1600f)
        {
            if (Main.dedServ) return;
            Player player = Main.LocalPlayer;
            float distanceFactor = 1f - MathHelper.Clamp(Vector2.Distance(player.Center, center) / range, 0f, 1f);
            if (distanceFactor <= 0f) return;
            player.Calamity().GeneralScreenShakePower = Math.Max(player.Calamity().GeneralScreenShakePower, power * distanceFactor);
        }

        public static void SpawnAbyssDust(Vector2 center, int count, float speed, float radius, float scale = 1f)
        {
            if (Main.dedServ) return;
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(radius, radius);
                Vector2 velocity = offset.SafeNormalize(Main.rand.NextVector2CircularEdge(1f, 1f)) * Main.rand.NextFloat(speed * 0.25f, speed);
                Color color = SeasSearingPalette.PollutionColor(Main.rand.NextFloat());
                Dust dust = Dust.NewDustPerfect(
                    center + offset,
                    Main.rand.NextBool(3) ? DustID.Water : DustID.GemEmerald,
                    velocity, 125, color,
                    Main.rand.NextFloat(0.55f, 1.15f) * scale);
                dust.noGravity = true;
                dust.fadeIn = scale;
            }
        }

        public static void SpawnPressureRing(Vector2 center, float speed, float radius, int count, Color color)
        {
            if (Main.dedServ) return;
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = (MathHelper.TwoPi * i / count).ToRotationVector2() * speed;
                Dust dust = Dust.NewDustPerfect(
                    center + velocity.SafeNormalize(Vector2.UnitY) * radius,
                    DustID.GemDiamond, velocity, 110, color,
                    Main.rand.NextFloat(0.75f, 1.2f));
                dust.noGravity = true;
            }
        }

        public static void SpawnGradeBurst(Vector2 center, int grade, int count)
        {
            if (Main.dedServ) return;
            Color baseColor = SeasSearingPalette.GradeColor(grade);
            for (int i = 0; i < count; i++)
            {
                int dustType = grade >= 4 ? DustID.Vortex : (grade >= 3 ? 89 : (Main.rand.NextBool(3) ? DustID.Water : DustID.GemEmerald));
                Vector2 velocity = Main.rand.NextVector2Circular(1f, 1f) * Main.rand.NextFloat(1.5f + grade * 0.8f, 3f + grade * 1.4f);
                Dust d = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(10f, 10f),
                    dustType, velocity, 100,
                    Color.Lerp(baseColor, SeasSearingPalette.RadioactiveCyan, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.6f, 1.1f + grade * 0.1f));
                d.noGravity = true;
            }
        }

        public static void PlayDeepShot(Vector2 position, float pitch = 0f)
        {
            SoundEngine.PlaySound(SoundID.Item11 with { Volume = 0.74f, Pitch = -0.18f + pitch, PitchVariance = 0.08f, MaxInstances = 6 }, position);
            SoundEngine.PlaySound(SoundID.Item36 with { Volume = 0.34f, Pitch = -0.45f + pitch, PitchVariance = 0.05f, MaxInstances = 6 }, position);
        }
    }
}
