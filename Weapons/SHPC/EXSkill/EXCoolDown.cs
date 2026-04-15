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

namespace CalamityLegendsComeBack.Weapons.SHPC.EXSkill
{
    public class SHPC_EXCooldown : CooldownHandler
    {
        // 当前进度（0~1）
        private float AdjustedCompletion => instance.timeLeft / (float)NewLegend_EXPlayer.GetCurrentEXMax(instance.player);
        private int DisplayValue => Utils.Clamp(instance.timeLeft / NewLegend_EXPlayer.GetFramesPerDisplayUnit(instance.player), 0, NewLegend_EXPlayer.EXDisplayMax);

        private Color TextColor => Color.AliceBlue;
        private Color TextBorderColor = Color.Black;

        // ✅ 唯一ID（必须改）
        public static new string ID => "SHPC_EX";

        // ❌ 不自动下降（因为这是能量条）
        public override bool CanTickDown => false;

        // ✅ 只有手持SHPC时才显示
        public override bool ShouldDisplay =>
            instance.player.HeldItem.type == ModContent.ItemType<NewLegendSHPC>() &&
            instance.player.GetModPlayer<NewLegend_EXPlayer>().EXUnlocked;

        // 名字（可后续本地化）
        public override LocalizedText DisplayName =>
            Language.GetText("Mods.CalamityLegendsComeBack.Cooldowns.SHPC_EX");

        // 贴图（先用占位，不影响功能）
        public override string Texture => "CalamityLegendsComeBack/Weapons/SHPC/EXSkill/EXCoolDown";
        public override string OutlineTexture => "CalamityLegendsComeBack/Weapons/SHPC/EXSkill/EXCoolDownOutline";
        public override string OverlayTexture => "CalamityLegendsComeBack/Weapons/SHPC/EXSkill/EXCoolDownOverlay";

        public override Color OutlineColor => Color.DarkSlateGray;

        public override Color CooldownStartColor =>
            Color.Lerp(Color.Cyan, Color.DarkSlateGray, instance.Completion);

        public override Color CooldownEndColor =>
            Color.Lerp(Color.White, Color.DarkSlateGray, instance.Completion);

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

            Vector2 textOffset = new Vector2(DisplayValue > 9 ? -11f : -6f, 10f);

            DrawBorderStringEightWay(
                spriteBatch,
                FontAssets.MouseText.Value,
                DisplayValue.ToString(),
                position + textOffset * scale,
                TextColor,
                TextBorderColor,
                scale
            );
        }

        public override void DrawCompact(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            Texture2D sprite = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D outline = ModContent.Request<Texture2D>(OutlineTexture).Value;
            Texture2D overlay = ModContent.Request<Texture2D>(OverlayTexture).Value;

            // 外框
            spriteBatch.Draw(outline, position, null, OutlineColor * opacity, 0, outline.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            // 图标
            spriteBatch.Draw(sprite, position, null, Color.White * opacity, 0, sprite.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            // 进度遮罩
            int lostHeight = (int)Math.Ceiling(overlay.Height * AdjustedCompletion);
            Rectangle crop = new Rectangle(0, lostHeight, overlay.Width, overlay.Height - lostHeight);

            spriteBatch.Draw(
                overlay,
                position + Vector2.UnitY * lostHeight * scale,
                crop,
                OutlineColor * opacity * 0.9f,
                0,
                sprite.Size() * 0.5f,
                scale,
                SpriteEffects.None,
                0f
            );

            // 数字
            Vector2 textOffset = new Vector2(DisplayValue > 9 ? -11f : -6f, 10f);

            DrawBorderStringEightWay(
                spriteBatch,
                FontAssets.MouseText.Value,
                DisplayValue.ToString(),
                position + textOffset * scale,
                TextColor,
                TextBorderColor,
                scale
            );
        }
    }
}
