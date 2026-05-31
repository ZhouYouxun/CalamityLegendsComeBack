using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace CalamityLegendsComeBack.Systems
{
    internal interface IScreenOverlayProjectile
    {
    }

    internal sealed class ScreenOverlayProjectileSystem : ModSystem
    {
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            // 这些面板必须在世界水体滤镜之后绘制，否则会像世界物体一样被水面扭曲。
            // 但它们也不能追加到接口列表末尾：原版鼠标层名为 "Vanilla: Cursor"，
            // 把面板插在它前面，鼠标和后续原版接口就会始终覆盖在矩阵 UI 上方。
            int cursorLayerIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Cursor");
            if (cursorLayerIndex < 0)
                cursorLayerIndex = 0;

            layers.Insert(cursorLayerIndex, new LegacyGameInterfaceLayer(
                "CalamityLegendsComeBack: Screen Overlay Projectiles",
                DrawScreenOverlayProjectiles,
                InterfaceScaleType.Game));
        }

        private static bool DrawScreenOverlayProjectiles()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.ModProjectile is not IScreenOverlayProjectile)
                    continue;

                Color lightColor = Color.White;
                projectile.ModProjectile.PreDraw(ref lightColor);
            }

            return true;
        }
    }
}
