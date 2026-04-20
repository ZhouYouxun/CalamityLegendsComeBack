using System;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow;
using CalamityMod.Cooldowns;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.Localization;
using Terraria.ModLoader;
using static CalamityMod.CalamityUtils;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill
{
    internal class BFEXCoolDown : CooldownHandler
    {
        private float ChargeCompletion => MathHelper.Clamp(1f - instance.timeLeft / (float)BFEXPlayer.UltimateCooldownFrames, 0f, 1f);
        private int DisplayValue => instance.timeLeft <= 0 ? 0 : (int)Math.Ceiling(instance.timeLeft / 60f);
        private Color PresetColor => BFArrowCommon.GetPresetColor(instance.player.GetModPlayer<BFRightUIPlayer>().CurrentPreset);

        private Color TextColor => Color.Lerp(new Color(216, 255, 216), Color.White, 0.35f);
        private Color TextBorderColor => Color.Black;

        public static new string ID => "BlossomFlux_EX";
        public override bool CanTickDown => false;

        public override bool ShouldDisplay =>
            instance.player.GetModPlayer<BFRightUIPlayer>().UltimateUnlocked &&
            (instance.player.HeldItem.type == ModContent.ItemType<NewLegendBlossomFlux>() || instance.timeLeft > 0);

        public override LocalizedText DisplayName =>
            Language.GetText("Mods.CalamityLegendsComeBack.Cooldowns.BlossomFlux_EX");

        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/EXSkill/BFEXCoolDown";
        public override string OutlineTexture => "CalamityLegendsComeBack/Weapons/BlossomFlux/EXSkill/BFEXCoolDownOutline";
        public override string OverlayTexture => "CalamityLegendsComeBack/Weapons/BlossomFlux/EXSkill/BFEXCoolDownOverlay";

        public override Color OutlineColor => Color.Lerp(PresetColor, Color.Black, 0.65f);

        public override Color CooldownStartColor =>
            Color.Lerp(PresetColor, Color.White, 0.18f + ChargeCompletion * 0.12f);

        public override Color CooldownEndColor =>
            Color.Lerp(Color.Lerp(PresetColor, Color.White, 0.45f), Color.White, ChargeCompletion * 0.18f);

        public override void ApplyBarShaders(float opacity)
        {
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseOpacity(opacity);
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseSaturation(ChargeCompletion);
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseColor(CooldownStartColor);
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseSecondaryColor(CooldownEndColor);
            GameShaders.Misc["CalamityMod:CircularBarShader"].Apply();
        }

        public override void DrawExpanded(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            base.DrawExpanded(spriteBatch, position, opacity, scale);

            DrawBorderStringEightWay(
                spriteBatch,
                FontAssets.MouseText.Value,
                DisplayValue.ToString(),
                position + new Vector2(-6f, 10f) * scale,
                TextColor,
                TextBorderColor,
                scale);
        }

        public override void DrawCompact(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            Texture2D sprite = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D outline = ModContent.Request<Texture2D>(OutlineTexture).Value;
            Texture2D overlay = ModContent.Request<Texture2D>(OverlayTexture).Value;

            spriteBatch.Draw(outline, position, null, OutlineColor * opacity, 0f, outline.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(sprite, position, null, Color.White * opacity, 0f, sprite.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            int filledHeight = Utils.Clamp((int)Math.Ceiling(overlay.Height * ChargeCompletion), 0, overlay.Height);
            if (filledHeight > 0)
            {
                int topTrim = overlay.Height - filledHeight;
                Rectangle crop = new Rectangle(0, topTrim, overlay.Width, filledHeight);

                spriteBatch.Draw(
                    overlay,
                    position + Vector2.UnitY * topTrim * scale,
                    crop,
                    OutlineColor * opacity * 0.9f,
                    0f,
                    sprite.Size() * 0.5f,
                    scale,
                    SpriteEffects.None,
                    0f);
            }

            DrawBorderStringEightWay(
                spriteBatch,
                FontAssets.MouseText.Value,
                DisplayValue.ToString(),
                position + new Vector2(-6f, 10f) * scale,
                TextColor,
                TextBorderColor,
                scale);
        }
    }
}
