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
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill
{
    // BlossomFlux 专属的大招条显示，负责把 0 到 5 的蓄积直接映射到冷却 UI。
    internal class BFEXCoolDown : CooldownHandler
    {
        private float AdjustedCompletion => instance.timeLeft / (float)BFEXPlayer.MaxChargeFrames;
        private int DisplayValue => Utils.Clamp(instance.timeLeft / BFEXPlayer.FramesPerDisplayUnit, 0, BFEXPlayer.UltimateDisplayMax);

        private Color TextColor => new Color(220, 255, 220);
        private Color TextBorderColor => Color.Black;

        public static new string ID => "BlossomFlux_EX";
        public override bool CanTickDown => false;

        public override bool ShouldDisplay =>
            instance.player.HeldItem.type == ModContent.ItemType<NewLegendBlossomFlux>() &&
            instance.player.GetModPlayer<BFRightUIPlayer>().UltimateUnlocked;

        public override LocalizedText DisplayName =>
            Language.GetText("Mods.CalamityLegendsComeBack.Cooldowns.BlossomFlux_EX");

        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/EXSkill/BFEXCoolDown";
        public override string OutlineTexture => "CalamityLegendsComeBack/Weapons/BlossomFlux/EXSkill/BFEXCoolDownOutline";
        public override string OverlayTexture => "CalamityLegendsComeBack/Weapons/BlossomFlux/EXSkill/BFEXCoolDownOverlay";

        public override Color OutlineColor => new Color(26, 58, 28);

        public override Color CooldownStartColor =>
            Color.Lerp(new Color(102, 214, 140), new Color(34, 102, 56), instance.Completion);

        public override Color CooldownEndColor =>
            Color.Lerp(new Color(212, 255, 196), new Color(50, 120, 58), instance.Completion);

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

            int lostHeight = (int)Math.Ceiling(overlay.Height * AdjustedCompletion);
            Rectangle crop = new Rectangle(0, lostHeight, overlay.Width, overlay.Height - lostHeight);

            spriteBatch.Draw(
                overlay,
                position + Vector2.UnitY * lostHeight * scale,
                crop,
                OutlineColor * opacity * 0.9f,
                0f,
                sprite.Size() * 0.5f,
                scale,
                SpriteEffects.None,
                0f);

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
