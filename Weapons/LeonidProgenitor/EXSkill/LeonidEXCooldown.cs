using System;
using CalamityMod.Cooldowns;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.Localization;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;
using static CalamityMod.CalamityUtils;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor
{
    public class LeonidEXCooldown : CooldownHandler
    {
        // Current energy completion ratio (0.0 to 1.0)
        private float AdjustedCompletion => MathHelper.Clamp(ThunderPlayer.UltimateEnergy / 100f, 0f, 1f);
        private int DisplayValue => ThunderPlayer.UltimateEnergy;

        private LeonidProgenitorPlayer ThunderPlayer => instance.player.GetModPlayer<LeonidProgenitorPlayer>();

        private Color TextColor => Color.AliceBlue;
        private Color TextBorderColor => Color.Black;

        // Unique ID for the cooldown system
        public static new string ID => "LeonidProgenitor_EX";

        // Cooldown shouldn't automatically tick down; we sync it directly to the player's energy level
        public override bool CanTickDown => false;

        // Display the cooldown handler only when the player holds the weapon
        public override bool ShouldDisplay =>
            instance.player.HeldItem.type == ModContent.ItemType<LeonidProgenitor>();

        // Localized name of the cooldown/energy bar
        public override LocalizedText DisplayName =>
            Language.GetText("Mods.CalamityLegendsComeBack.Cooldowns.LeonidEX");

        // Textures copied to the weapon's own directory
        public override string Texture => "CalamityLegendsComeBack/Weapons/LeonidProgenitor/EXSkill/EXCoolDown";
        public override string OutlineTexture => "CalamityLegendsComeBack/Weapons/LeonidProgenitor/EXSkill/EXCoolDownOutline";
        public override string OverlayTexture => "CalamityLegendsComeBack/Weapons/LeonidProgenitor/EXSkill/EXCoolDownOverlay";

        public override Color OutlineColor => new Color(20, 36, 120);

        public override Color CooldownStartColor =>
            Color.Lerp(new Color(50, 110, 220), LeonidVisualUtils.StratusBlue, AdjustedCompletion);

        public override Color CooldownEndColor =>
            Color.Lerp(LeonidVisualUtils.MoonViolet, Color.White, AdjustedCompletion);

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

            // Cover uncharged portion from top to bottom
            int lostHeight = (int)Math.Ceiling(overlay.Height * (1f - AdjustedCompletion));
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
                0f
            );
        }

        private void DrawCounter(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            Vector2 textOffset = new Vector2(DisplayValue > 99 ? -15f : DisplayValue > 9 ? -11f : -6f, 10f);

            DrawBorderStringEightWay(
                spriteBatch,
                FontAssets.MouseText.Value,
                DisplayValue.ToString(),
                position + textOffset * scale,
                TextColor * opacity,
                TextBorderColor * opacity,
                scale
            );
        }
    }
}
