using System;
using CalamityMod.Cooldowns;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.Localization;
using Terraria.ModLoader;
using static CalamityMod.CalamityUtils;

namespace CalamityLegendsComeBack.Weapons.PristineFury
{
    // 大招「劫火重燃」的能量条：借用 SHPC 大招同款贴图，重新上色为纯元的亮黄色。
    public class PristineFuryUltimateCooldown : CooldownHandler
    {
        private static readonly Color BrightYellow = new(255, 232, 64);
        private static readonly Color EmberOutline = new(70, 28, 10);

        private float AdjustedCompletion => instance.timeLeft / (float)PristineFuryPlayer.UltimateEnergyMax;
        private int DisplayValue => (int)MathHelper.Clamp(AdjustedCompletion * 100f, 0f, 100f);

        private Color TextColor => Color.AliceBlue;
        private Color TextBorderColor = Color.Black;

        public static new string ID => "PristineFury_Ultimate";

        public override bool CanTickDown => false;

        public override bool ShouldDisplay =>
            instance.player.HeldItem.type == ModContent.ItemType<NewLegendPristineFury>();

        public override LocalizedText DisplayName =>
            Language.GetText("Mods.CalamityLegendsComeBack.Cooldowns.PristineFury_Ultimate");

        public override string Texture => "CalamityLegendsComeBack/Weapons/SHPC/EXSkill/EXCoolDown";
        public override string OutlineTexture => "CalamityLegendsComeBack/Weapons/SHPC/EXSkill/EXCoolDownOutline";
        public override string OverlayTexture => "CalamityLegendsComeBack/Weapons/SHPC/EXSkill/EXCoolDownOverlay";

        public override Color OutlineColor => EmberOutline;

        public override Color CooldownStartColor =>
            Color.Lerp(BrightYellow, Color.White, AdjustedCompletion * 0.4f);

        public override Color CooldownEndColor =>
            Color.Lerp(EmberOutline, BrightYellow, AdjustedCompletion);

        public override void ApplyBarShaders(float opacity)
        {
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseOpacity(opacity);
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseSaturation(AdjustedCompletion);
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseColor(CooldownStartColor);
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseSecondaryColor(CooldownEndColor);
            GameShaders.Misc["CalamityMod:CircularBarShader"].Apply();
        }

        public override void DrawExpanded(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            base.DrawExpanded(spriteBatch, position, opacity, scale);

            Vector2 textOffset = new(DisplayValue > 9 ? -11f : -6f, 10f);
            DrawBorderStringEightWay(spriteBatch, FontAssets.MouseText.Value, DisplayValue.ToString(), position + textOffset * scale, TextColor, TextBorderColor, scale);
        }

        public override void DrawCompact(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            Texture2D sprite = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D outline = ModContent.Request<Texture2D>(OutlineTexture).Value;
            Texture2D overlay = ModContent.Request<Texture2D>(OverlayTexture).Value;

            spriteBatch.Draw(outline, position, null, OutlineColor * opacity, 0, outline.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(sprite, position, null, BrightYellow * opacity, 0, sprite.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            int lostHeight = (int)Math.Ceiling(overlay.Height * AdjustedCompletion);
            Rectangle crop = new(0, lostHeight, overlay.Width, overlay.Height - lostHeight);

            spriteBatch.Draw(
                overlay,
                position + Vector2.UnitY * lostHeight * scale,
                crop,
                OutlineColor * opacity * 0.9f,
                0,
                sprite.Size() * 0.5f,
                scale,
                SpriteEffects.None,
                0f);

            Vector2 textOffset = new(DisplayValue > 9 ? -11f : -6f, 10f);
            DrawBorderStringEightWay(spriteBatch, FontAssets.MouseText.Value, DisplayValue.ToString(), position + textOffset * scale, TextColor, TextBorderColor, scale);
        }
    }
}
