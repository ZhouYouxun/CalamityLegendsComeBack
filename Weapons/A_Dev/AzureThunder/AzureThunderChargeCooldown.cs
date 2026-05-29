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

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    // 一息万变层数的灾厄冷却栏显示器，本身不倒计时，只读玩家层数。
    internal sealed class AzureThunderChargeCooldown : CooldownHandler
    {
        public static new string ID => "AzureThunder_ThunderCharge";

        // 当前玩家青霆状态，是图标填充和数字显示的唯一数据源。
        private AzureThunderPlayer ThunderPlayer => instance.player.GetModPlayer<AzureThunderPlayer>();
        private float AdjustedCompletion => MathHelper.Clamp(ThunderPlayer.ThunderCharge / (float)AzureThunderPlayer.ThunderChargeMax, 0f, 1f);
        private int DisplayValue => ThunderPlayer.ThunderCharge;

        // 层数条不能自然减少，只在玩家手持青霆剑时显示。
        public override bool CanTickDown => false;
        public override bool ShouldDisplay => instance.player.HeldItem.type == ModContent.ItemType<AzureThunder>();

        public override LocalizedText DisplayName =>
            Language.GetText("Mods.CalamityLegendsComeBack.Cooldowns.AzureThunder_ThunderCharge");

        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/EXSkill/ThunderCharge";
        public override string OutlineTexture => "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/EXSkill/ThunderChargeDownOutline";
        public override string OverlayTexture => "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/EXSkill/ThunderChargeOverlay";

        public override Color OutlineColor => new(24, 62, 92);
        public override Color CooldownStartColor => Color.Lerp(AzureThunderColors.Azure, AzureThunderColors.Yellow, AdjustedCompletion);
        public override Color CooldownEndColor => Color.Lerp(new Color(170, 238, 255), Color.White, AdjustedCompletion);

        public override void ApplyBarShaders(float opacity)
        {
            // 灾厄圆形条 shader 通过饱和度和主副色表现当前充能比例。
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseOpacity(opacity);
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseSaturation(AdjustedCompletion);
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseColor(CooldownStartColor);
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseSecondaryColor(CooldownEndColor);
            GameShaders.Misc["CalamityMod:CircularBarShader"].Apply();
        }

        public override void DrawExpanded(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            // 展开模式沿用基类圆条，再额外绘制层数数字。
            base.DrawExpanded(spriteBatch, position, opacity, scale);
            DrawCounter(spriteBatch, position, opacity, scale);
        }

        public override void DrawCompact(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            // 紧凑模式没有基类圆条，手动画图标和数字。
            DrawIcon(spriteBatch, position, opacity, scale);
            DrawCounter(spriteBatch, position, opacity, scale);
        }

        private void DrawIcon(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            Texture2D sprite = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D outline = ModContent.Request<Texture2D>(OutlineTexture).Value;
            Texture2D overlay = ModContent.Request<Texture2D>(OverlayTexture).Value;

            spriteBatch.Draw(outline, position, null, OutlineColor * opacity, 0f, outline.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(sprite, position, null, Color.White * opacity, 0f, sprite.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            // overlay 只绘制未填充部分，形成从上往下被盖住的充能效果。
            int lostHeight = (int)Math.Ceiling(overlay.Height * (1f - AdjustedCompletion));
            Rectangle crop = new(0, lostHeight, overlay.Width, overlay.Height - lostHeight);
            spriteBatch.Draw(overlay, position + Vector2.UnitY * lostHeight * scale, crop, OutlineColor * opacity * 0.9f, 0f, sprite.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }

        private void DrawCounter(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            // 层数最多 3，但仍按双位数预留偏移，避免未来扩展挤出图标。
            Vector2 textOffset = new(DisplayValue > 9 ? -11f : -6f, 10f);
            DrawBorderStringEightWay(spriteBatch, FontAssets.MouseText.Value, DisplayValue.ToString(), position + textOffset * scale, Color.White * opacity, Color.Black * opacity, scale);
        }
    }
}
