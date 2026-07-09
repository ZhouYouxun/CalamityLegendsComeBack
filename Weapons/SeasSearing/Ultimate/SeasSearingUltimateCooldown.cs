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

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    internal sealed class SeasSearingUltimateCooldown : CooldownHandler
    {
        public static new string ID => "SeasSearing_Ultimate";

        private SeasSearingPlayer SeasPlayer => instance.player.GetModPlayer<SeasSearingPlayer>();
        private float ReadyCompletion => 1f - MathHelper.Clamp(SeasPlayer.UltimateCooldown / (float)SeasSearingPlayer.UltimateCooldownFrames, 0f, 1f);
        private int DisplayValue => (int)MathHelper.Clamp(ReadyCompletion * 100f, 0f, 100f);

        public override bool CanTickDown => false;
        public override bool ShouldDisplay => instance.player.HeldItem.type == ModContent.ItemType<SeasSearing>() || SeasPlayer.UltimateCooldown > 0;

        public override LocalizedText DisplayName =>
            Language.GetText("Mods.CalamityLegendsComeBack.Cooldowns.SeasSearing_Ultimate");

        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/EXSkill/ThunderCharge";
        public override string OutlineTexture => "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/EXSkill/ThunderChargeDownOutline";
        public override string OverlayTexture => "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/EXSkill/ThunderChargeOverlay";

        public override Color OutlineColor => Color.Lerp(SeasSearingPalette.AbyssBlack, SeasSearingPalette.DeepBlue, 0.65f);
        public override Color CooldownStartColor => Color.Lerp(SeasSearingPalette.DeepBlue, SeasSearingPalette.RadioactiveCyan, ReadyCompletion);
        public override Color CooldownEndColor => Color.Lerp(SeasSearingPalette.ToxicGreen, Color.White, ReadyCompletion);

        public override void ApplyBarShaders(float opacity)
        {
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseOpacity(opacity);
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseSaturation(0.35f + ReadyCompletion * 0.65f);
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
            DrawIcon(spriteBatch, position, opacity, scale);
            DrawCounter(spriteBatch, position, opacity, scale);
        }

        private void DrawIcon(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            Texture2D sprite = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D outline = ModContent.Request<Texture2D>(OutlineTexture).Value;
            Texture2D overlay = ModContent.Request<Texture2D>(OverlayTexture).Value;

            spriteBatch.Draw(outline, position, null, OutlineColor * opacity, 0f, outline.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(sprite, position, null, Color.Lerp(SeasSearingPalette.RadioactiveCyan, Color.White, 0.25f + ReadyCompletion * 0.35f) * opacity, 0f, sprite.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            int lostHeight = (int)Math.Ceiling(overlay.Height * (1f - ReadyCompletion));
            Rectangle crop = new(0, lostHeight, overlay.Width, overlay.Height - lostHeight);
            spriteBatch.Draw(overlay, position + Vector2.UnitY * lostHeight * scale, crop, SeasSearingPalette.RadioactiveCyan * opacity * 0.9f, 0f, sprite.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }

        private void DrawCounter(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            Vector2 textOffset = new(DisplayValue > 99 ? -15f : DisplayValue > 9 ? -11f : -6f, 10f);
            DrawBorderStringEightWay(spriteBatch, FontAssets.MouseText.Value, DisplayValue.ToString(), position + textOffset * scale, Color.White * opacity, Color.Black * opacity, scale);
        }
    }
}
