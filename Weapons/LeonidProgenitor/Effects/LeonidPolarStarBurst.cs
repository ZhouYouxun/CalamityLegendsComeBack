using CalamityMod.Particles;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;
using Microsoft.Xna.Framework;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects
{
    // Local-only, non-damaging five-point polar-star burst.
    public static class LeonidPolarStarBurst
    {
        public static void Spawn(Vector2 center, Vector2 launchVelocity, Color color, float scale = 1f)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = launchVelocity.SafeNormalize(Vector2.UnitY);
            Color core = Color.Lerp(color, LeonidVisualUtils.MoonWhite, 0.58f);
            GeneralParticleHandler.SpawnParticle(new StrongBloom(center, Vector2.Zero, core, 0.48f * scale, 18));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, forward * 0.35f, color,
                new Vector2(0.62f, 1.45f) * scale, forward.ToRotation(), 0.14f, 0.024f, 22));

            float baseAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < 5; i++)
            {
                Vector2 direction = (baseAngle + i * MathHelper.TwoPi / 5f).ToRotationVector2();
                Vector2 velocity = direction * Main.rand.NextFloat(2.2f, 3.8f) + forward * 0.65f;
                GeneralParticleHandler.SpawnParticle(new CustomSpark(center + direction * 7f, velocity,
                    "CalamityMod/Particles/BloomLineFade", false, 18, 0.026f, core * 0.85f,
                    new Vector2(3.5f, 0.85f) * scale, shrinkSpeed: 0.42f));
            }
        }
    }
}
