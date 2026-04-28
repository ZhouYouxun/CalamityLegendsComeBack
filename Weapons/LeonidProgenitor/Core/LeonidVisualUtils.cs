using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core
{
    public static class LeonidVisualUtils
    {
        public static Color GetMeteorColor(int primaryEffectID, int secondaryEffectID)
        {
            Color fallbackA = new(68, 206, 255);
            Color fallbackB = new(216, 104, 255);

            LeonidMetalEntry primary = LeonidMetalRegistry.GetByEffectID(primaryEffectID);
            LeonidMetalEntry secondary = LeonidMetalRegistry.GetByEffectID(secondaryEffectID);

            if (primary != null && secondary != null)
                return Color.Lerp(primary.ThemeColor, secondary.ThemeColor, 0.5f);

            if (primary != null)
                return primary.ThemeColor;

            if (secondary != null)
                return secondary.ThemeColor;

            return Color.Lerp(fallbackA, fallbackB, 0.5f + 0.5f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 2.2f));
        }

        public static void SpawnDustBurst(Vector2 center, Color color, int count, float speed, float scale)
        {
            for (int i = 0; i < count; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    center,
                    DustID.TintableDustLighted,
                    Main.rand.NextVector2Circular(speed, speed),
                    100,
                    Color.Lerp(color, Color.White, Main.rand.NextFloat(0.35f)),
                    Main.rand.NextFloat(scale * 0.75f, scale * 1.25f));
                dust.noGravity = true;
            }
        }

        public static void DrawBloom(Vector2 drawPosition, Color color, float scale, float rotation = 0f)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            color.A = 0;
            Main.EntitySpriteDraw(
                bloom,
                drawPosition - Main.screenPosition,
                null,
                color,
                rotation,
                bloom.Size() * 0.5f,
                scale,
                SpriteEffects.None,
                0f);
        }

        public static void SpawnBloomBurst(Vector2 center, Color color, float scale, int lifetime)
        {
            if (Main.dedServ)
                return;

            color.A = 0;
            GeneralParticleHandler.SpawnParticle(new StrongBloom(center, Vector2.Zero, color, scale, lifetime));
        }
    }
}
