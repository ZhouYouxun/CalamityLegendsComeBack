//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
//using Terraria;
//using Terraria.DataStructures;
//using Terraria.ModLoader;

//namespace CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill.TurretMode
//{
//    // 炮台形态下围绕玩家绘制的花环魔法阵，纯视觉反馈。
//    internal sealed class BFTurretAuraLayer : PlayerDrawLayer
//    {
//        public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.HeldItem);

//        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) =>
//            drawInfo.drawPlayer.GetModPlayer<BFTurretModePlayer>().AuraStrength > 0.02f;

//        protected override void Draw(ref PlayerDrawSet drawInfo)
//        {
//            Player player = drawInfo.drawPlayer;
//            BFTurretModePlayer turretPlayer = player.GetModPlayer<BFTurretModePlayer>();
//            float intensity = turretPlayer.AuraStrength;
//            if (intensity <= 0.02f)
//                return;

//            Texture2D outerFlower = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/flower_015").Value;
//            Texture2D innerFlower = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/flower_011").Value;
//            Texture2D energyTexture = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/fx_EnergyBolt6").Value;

//            Vector2 center = player.MountedCenter + new Vector2(0f, player.gfxOffY + 2f) - Main.screenPosition;
//            float time = Main.GlobalTimeWrappedHourly;
//            Color outerColor = new Color(90, 255, 150, 0) * (0.45f * intensity);
//            Color innerColor = new Color(175, 255, 175, 0) * (0.55f * intensity);
//            Color boltColor = new Color(220, 255, 220, 0) * (0.35f * intensity);

//            DrawData outerRing = new DrawData(
//                outerFlower,
//                center,
//                null,
//                outerColor,
//                time * 0.95f,
//                outerFlower.Size() * 0.5f,
//                0.62f + intensity * 0.18f,
//                SpriteEffects.None,
//                0);
//            drawInfo.DrawDataCache.Add(outerRing);

//            DrawData innerRing = new DrawData(
//                innerFlower,
//                center,
//                null,
//                innerColor,
//                -time * 1.35f,
//                innerFlower.Size() * 0.5f,
//                0.42f + intensity * 0.14f,
//                SpriteEffects.None,
//                0);
//            drawInfo.DrawDataCache.Add(innerRing);

//            for (int i = 0; i < 3; i++)
//            {
//                float angle = time * (1.7f + 0.18f * i) + MathHelper.TwoPi / 3f * i;
//                Vector2 offset = angle.ToRotationVector2() * (18f + 6f * i);
//                offset.X *= 0.8f;

//                DrawData bolt = new DrawData(
//                    energyTexture,
//                    center + offset,
//                    null,
//                    boltColor,
//                    angle + MathHelper.PiOver2,
//                    energyTexture.Size() * 0.5f,
//                    0.18f + 0.04f * i + intensity * 0.07f,
//                    SpriteEffects.None,
//                    0);
//                drawInfo.DrawDataCache.Add(bolt);
//            }
//        }
//    }
//}
