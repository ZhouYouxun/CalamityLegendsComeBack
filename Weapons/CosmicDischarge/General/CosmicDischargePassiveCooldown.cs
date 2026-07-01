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

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    internal sealed class CosmicDischargePassiveCooldown : CooldownHandler
    {
        public static new string ID => "CosmicDischarge_RiftSeal";

        private CosmicDischargePlayer DischargePlayer => instance.player.GetModPlayer<CosmicDischargePlayer>();
        private float AdjustedCompletion => MathHelper.Clamp(DischargePlayer.PassiveCooldownTimer / (float)CosmicDischargePlayer.PassiveCooldownFrames, 0f, 1f);
        private int DisplayValue => DischargePlayer.PassiveCooldownTimer <= 0 ? 0 : (int)Math.Ceiling(DischargePlayer.PassiveCooldownTimer / 60f);

        public override bool CanTickDown => false;
        public override bool ShouldDisplay => DischargePlayer.ShouldShowPassiveCooldown;

        public override LocalizedText DisplayName =>
            Language.GetText("Mods.CalamityLegendsComeBack.Cooldowns.CosmicDischarge_RiftSeal");

        public override string Texture => "CalamityLegendsComeBack/Weapons/CosmicDischarge/General/CD/CosmicDischargeCooldown";
        public override string OutlineTexture => "CalamityLegendsComeBack/Weapons/CosmicDischarge/General/CD/CosmicDischargeCooldownOutline";
        public override string OverlayTexture => "CalamityLegendsComeBack/Weapons/CosmicDischarge/General/CD/CosmicDischargeCooldownOverlay";

        public override Color OutlineColor => new(22, 48, 70);
        public override Color CooldownStartColor => CosmicDischargeCommon.DoGPurpleColor;
        public override Color CooldownEndColor => CosmicDischargeCommon.DoGSpecialColor;

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
            DrawCounter(spriteBatch, position, opacity, scale);
        }

        public override void DrawCompact(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            Texture2D sprite = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D outline = ModContent.Request<Texture2D>(OutlineTexture).Value;
            Texture2D overlay = ModContent.Request<Texture2D>(OverlayTexture).Value;

            spriteBatch.Draw(outline, position, null, OutlineColor * opacity, 0f, outline.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(sprite, position, null, Color.White * opacity, 0f, sprite.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            int lostHeight = (int)Math.Ceiling(overlay.Height * AdjustedCompletion);
            Rectangle crop = new(0, lostHeight, overlay.Width, overlay.Height - lostHeight);
            spriteBatch.Draw(overlay, position + Vector2.UnitY * lostHeight * scale, crop, OutlineColor * opacity * 0.88f, 0f, sprite.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            DrawCounter(spriteBatch, position, opacity, scale);
        }

        private void DrawCounter(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            if (DisplayValue <= 0)
                return;

            Vector2 textOffset = new(DisplayValue > 99 ? -15f : DisplayValue > 9 ? -11f : -6f, 10f);
            DrawBorderStringEightWay(spriteBatch, FontAssets.MouseText.Value, DisplayValue.ToString(), position + textOffset * scale, Color.White * opacity, Color.Black * opacity, scale);
        }
    }
}
