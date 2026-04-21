using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core
{
    public class LeonidMetalGlobalItem : GlobalItem
    {
        private static Asset<Texture2D> BloomTexture => ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");
        private static Asset<Texture2D> StarTexture => ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/star_01");

        public override bool PreDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Player player = Main.LocalPlayer;
            if (player?.active != true)
                return true;

            if (!player.GetModPlayer<LeonidMetalPlayer>().TryGetHighlight(item, out Color highlightColor))
                return true;

            Texture2D bloom = BloomTexture.Value;
            Texture2D star = StarTexture.Value;
            float time = Main.GlobalTimeWrappedHourly * 3.5f + item.type * 0.031f;
            Vector2 pulseOffset = Vector2.UnitX.RotatedBy(time) * 1.8f * scale;
            Color outerGlow = highlightColor * 0.33f;
            Color innerGlow = Color.Lerp(highlightColor, Color.White, 0.35f) * 0.6f;

            spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);

            Main.spriteBatch.Draw(bloom, position, null, outerGlow, 0f, bloom.Size() * 0.5f, 0.55f * scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(bloom, position + pulseOffset, null, innerGlow, time * 0.4f, bloom.Size() * 0.5f, 0.35f * scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(bloom, position - pulseOffset, null, innerGlow, -time * 0.4f, bloom.Size() * 0.5f, 0.35f * scale, SpriteEffects.None, 0f);

            for (int i = 0; i < 2; i++)
            {
                float orbitAngle = time + MathHelper.Pi * i;
                Vector2 orbitOffset = orbitAngle.ToRotationVector2() * 10f * scale;
                Main.spriteBatch.Draw(star, position + orbitOffset, null, highlightColor * 0.45f, -orbitAngle, star.Size() * 0.5f, 0.17f * scale, SpriteEffects.None, 0f);
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
            return true;
        }
    }
}
