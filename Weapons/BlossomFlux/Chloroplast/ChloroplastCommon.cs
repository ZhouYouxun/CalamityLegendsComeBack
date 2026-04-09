using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast
{
    // 叶绿体预设共用的颜色、辉光和爆散工具统一收在这里。
    internal static class ChloroplastCommon
    {
        public static Color PresetColor(BlossomFluxChloroplastPresetType preset) => preset switch
        {
            BlossomFluxChloroplastPresetType.Chlo_ABreak => new Color(132, 255, 132),
            BlossomFluxChloroplastPresetType.Chlo_BRecov => new Color(110, 255, 186),
            BlossomFluxChloroplastPresetType.Chlo_CDetec => new Color(255, 90, 90),
            BlossomFluxChloroplastPresetType.Chlo_DBomb => new Color(255, 186, 110),
            BlossomFluxChloroplastPresetType.Chlo_EPlague => new Color(172, 228, 92),
            _ => Color.White
        };

        public static void DrawGlow(Projectile projectile, Color color, float scale = 1f, float intensity = 0.32f)
        {
            Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Color glowColor = color * intensity;

            Vector2[] offsets =
            {
                new Vector2(0f, -2f),
                new Vector2(1.5f, 1.5f),
                new Vector2(-1.5f, 1.5f)
            };

            foreach (Vector2 offset in offsets)
            {
                Main.EntitySpriteDraw(
                    texture,
                    drawPosition + offset,
                    frame,
                    glowColor,
                    projectile.rotation,
                    origin,
                    projectile.scale * scale,
                    SpriteEffects.None,
                    0);
            }
        }

        public static void SimpleBurst(Projectile projectile, int dustType, Color color, int amount, float speedMin, float speedMax, float scaleMin = 0.9f, float scaleMax = 1.3f)
        {
            for (int i = 0; i < amount; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    projectile.Center,
                    dustType,
                    Main.rand.NextVector2CircularEdge(3f, 3f) * Main.rand.NextFloat(speedMin, speedMax),
                    100,
                    color,
                    Main.rand.NextFloat(scaleMin, scaleMax));
                dust.noGravity = true;
            }
        }
    }
}
