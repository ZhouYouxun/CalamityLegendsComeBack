using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack.ForShuriken
{
    internal static class BBShuriken_Hardmode_Effects
    {
        public static void DrawRotatingCopies(Projectile projectile, Texture2D projectileTexture)
        {
            Vector2 drawPos = projectile.Center - Main.screenPosition;
            Vector2 origin = projectileTexture.Size() * 0.5f;
            float drawScale = projectile.width / (float)projectileTexture.Width;
            float orbitRadius = projectile.width * 0.34f;
            float spin = Main.GlobalTimeWrappedHourly * 8.5f + projectile.identity * 0.37f;

            for (int i = 0; i < 4; i++)
            {
                float angle = spin + MathHelper.TwoPi * i / 4f;
                Vector2 offset = angle.ToRotationVector2() * orbitRadius;
                Color drawColor = Color.Lerp(new Color(15, 45, 85, 0), new Color(70, 170, 255, 0), 0.68f) * 0.45f;

                Main.EntitySpriteDraw(
                    projectileTexture,
                    drawPos + offset,
                    null,
                    drawColor,
                    projectile.rotation - spin * 0.7f,
                    origin,
                    drawScale * 0.92f,
                    SpriteEffects.None,
                    0);
            }
        }
    }
}
