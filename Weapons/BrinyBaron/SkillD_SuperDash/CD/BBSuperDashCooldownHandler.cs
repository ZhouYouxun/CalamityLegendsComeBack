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

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    internal class BBSuperDashCooldownHandler : CooldownHandler
    {
        private BBSuperDashCooldownPlayer CooldownPlayer => instance.player.GetModPlayer<BBSuperDashCooldownPlayer>();
        private float AdjustedCompletion => CooldownPlayer.CooldownCompletion;
        private Color TextColor => Color.LightGoldenrodYellow;
        private Color TextBorderColor => Color.Black;

        public static new string ID => "BrinyBaron_SuperDash";
        public override bool CanTickDown => false;

        public override bool ShouldDisplay =>
            instance.player.HeldItem.type == ModContent.ItemType<NewLegendBrinyBaron>() &&
            CooldownPlayer.IsCoolingDown;

        public override LocalizedText DisplayName =>
            Language.GetText("Mods.CalamityLegendsComeBack.Cooldowns.BrinyBaron_SuperDash");

        public override string Texture => "CalamityLegendsComeBack/Weapons/BrinyBaron/SkillD_SuperDash/CD/BBSuperDashCooldown";
        public override string OutlineTexture => "CalamityLegendsComeBack/Weapons/BrinyBaron/SkillD_SuperDash/CD/BBSuperDashCooldownOutline";
        public override string OverlayTexture => "CalamityLegendsComeBack/Weapons/BrinyBaron/SkillD_SuperDash/CD/BBSuperDashCooldownOverlay";

        public override Color OutlineColor => new Color(66, 42, 20);

        public override Color CooldownStartColor =>
            Color.Lerp(new Color(120, 212, 255), new Color(30, 55, 85), instance.Completion);

        public override Color CooldownEndColor =>
            Color.Lerp(new Color(255, 232, 118), new Color(96, 60, 30), instance.Completion);

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
                Math.Max(1, (int)Math.Ceiling(CooldownPlayer.RemainingFrames / 60f)).ToString(),
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
                Math.Max(1, (int)Math.Ceiling(CooldownPlayer.RemainingFrames / 60f)).ToString(),
                position + new Vector2(-6f, 10f) * scale,
                TextColor,
                TextBorderColor,
                scale);
        }
    }
}
