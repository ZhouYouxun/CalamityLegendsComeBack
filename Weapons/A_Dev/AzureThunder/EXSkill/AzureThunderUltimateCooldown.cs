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
    // 万钧风雷能量条显示器：读 UltimateEnergy，但不让灾厄冷却系统自动递减。
    internal sealed class AzureThunderUltimateCooldown : CooldownHandler
    {
        public static new string ID => "AzureThunder_Ultimate";

        // 通过玩家状态换算填充比例和图标中心数字。
        private AzureThunderPlayer ThunderPlayer => instance.player.GetModPlayer<AzureThunderPlayer>();
        private float AdjustedCompletion => MathHelper.Clamp(ThunderPlayer.UltimateEnergy / (float)AzureThunderPlayer.UltimateEnergyMax, 0f, 1f);
        private int DisplayValue => ThunderPlayer.UltimateEnergy;

        // 终极能量是累积资源，不是自然冷却；只有手持青霆剑时显示。
        public override bool CanTickDown => false;
        public override bool ShouldDisplay => instance.player.HeldItem.type == ModContent.ItemType<AzureThunder>();

        public override LocalizedText DisplayName =>
            Language.GetText("Mods.CalamityLegendsComeBack.Cooldowns.AzureThunder_Ultimate");

        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/EXSkill/ThunderCharge";
        public override string OutlineTexture => "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/EXSkill/ThunderChargeDownOutline";
        public override string OverlayTexture => "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/EXSkill/ThunderChargeOverlay";

        public override Color OutlineColor => new(80, 54, 20);
        public override Color CooldownStartColor => Color.Lerp(new Color(70, 190, 255), AzureThunderColors.Yellow, AdjustedCompletion);
        public override Color CooldownEndColor => Color.Lerp(AzureThunderColors.PaleYellow, Color.White, AdjustedCompletion);

        public override void ApplyBarShaders(float opacity)
        {
            // 充能越高，圆形条颜色越偏向金色和白色。
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseOpacity(opacity);
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseSaturation(AdjustedCompletion);
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseColor(CooldownStartColor);
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseSecondaryColor(CooldownEndColor);
            GameShaders.Misc["CalamityMod:CircularBarShader"].Apply();
        }

        public override void DrawExpanded(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            // 展开栏保留灾厄默认外圈，再叠加能量数值。
            base.DrawExpanded(spriteBatch, position, opacity, scale);
            DrawCounter(spriteBatch, position, opacity, scale);
        }

        public override void DrawCompact(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            // 紧凑栏直接画图标、遮罩和数值，确保和一息万变外观一致。
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

            // 未充满的部分用棕色 outline 覆盖，让图标呈现纵向蓄能。
            int lostHeight = (int)Math.Ceiling(overlay.Height * (1f - AdjustedCompletion));
            Rectangle crop = new(0, lostHeight, overlay.Width, overlay.Height - lostHeight);
            spriteBatch.Draw(overlay, position + Vector2.UnitY * lostHeight * scale, crop, OutlineColor * opacity * 0.9f, 0f, sprite.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }

        private void DrawCounter(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            // 终极能量最大 240，三位数需要更靠左的文本锚点。
            Vector2 textOffset = new(DisplayValue > 99 ? -15f : DisplayValue > 9 ? -11f : -6f, 10f);
            DrawBorderStringEightWay(spriteBatch, FontAssets.MouseText.Value, DisplayValue.ToString(), position + textOffset * scale, Color.White * opacity, Color.Black * opacity, scale);
        }
    }
}
