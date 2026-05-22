using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury
{
    internal sealed class NewLegendPristineFuryBackpackPlayer : ModPlayer
    {
        public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
        {
            if (drawInfo.shadow != 0f)
                return;

            Player drawPlayer = drawInfo.drawPlayer;
            Item item = drawPlayer.HeldItem;
            if (drawPlayer.frozen ||
                item.IsAir ||
                item.type != ModContent.ItemType<NewLegendPristineFury>() ||
                drawPlayer.dead ||
                drawPlayer.wet && item.noWet ||
                drawPlayer.wings != 0 && drawPlayer.velocity.Y != 0f)
            {
                return;
            }

            Texture2D backpackTexture = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/PristineFury/Backpack_NewLegendPF").Value;
            SpriteEffects spriteEffects = drawPlayer.direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            int xOffset = 9;

            DrawData backpackDrawData = new(
                backpackTexture,
                new Vector2(
                    (int)(drawPlayer.position.X - Main.screenPosition.X + drawPlayer.width / 2 - xOffset * drawPlayer.direction) - 4f * drawPlayer.direction,
                    (int)(drawPlayer.position.Y - Main.screenPosition.Y + drawPlayer.height / 2 + 2f * drawPlayer.gravDir - 8f * drawPlayer.gravDir + drawPlayer.gfxOffY)),
                new Rectangle(0, 0, backpackTexture.Width, backpackTexture.Height),
                drawInfo.colorArmorBody,
                drawPlayer.bodyRotation,
                new Vector2(backpackTexture.Width / 2, backpackTexture.Height / 2),
                1f,
                spriteEffects,
                0);

            backpackDrawData.shader = 0;
            drawInfo.DrawDataCache.Add(backpackDrawData);
        }
    }
}
