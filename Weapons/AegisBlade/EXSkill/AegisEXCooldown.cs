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

namespace CalamityLegendsComeBack.Weapons.AegisBlade.EXSkill
{
    public class AegisEXCooldown : CooldownHandler
    {
        // AdjustedCompletion tracks how full the energy bar is (0 = empty, 1 = full)
        private float AdjustedCompletion => MathHelper.Clamp(instance.timeLeft / BalanceAegisBlade.EnergyMax, 0f, 1f);

        public static new string ID => "AegisBlade_EX";

        // Energy bar doesn't auto-drain — AegisBlade.HoldItem sets timeLeft manually each frame
        public override bool CanTickDown => false;

        public override bool ShouldDisplay =>
            instance.player.HeldItem.type == ModContent.ItemType<AegisBlade>();

        public override LocalizedText DisplayName =>
            Language.GetText("Mods.CalamityLegendsComeBack.Cooldowns.AegisEX");

        // Placeholder: reuse SHPC textures until AegisBlade-specific icons are created
        public override string Texture        => "CalamityLegendsComeBack/Weapons/SHPC/EXSkill/EXCoolDown";
        public override string OutlineTexture => "CalamityLegendsComeBack/Weapons/SHPC/EXSkill/EXCoolDownOutline";
        public override string OverlayTexture => "CalamityLegendsComeBack/Weapons/SHPC/EXSkill/EXCoolDownOverlay";

        public override Color OutlineColor => new Color(180, 150, 30);

        public override Color CooldownStartColor =>
            Color.Lerp(new Color(255, 145, 52), new Color(255, 200, 60), AdjustedCompletion);

        public override Color CooldownEndColor =>
            Color.Lerp(new Color(255, 200, 60), new Color(255, 245, 180), AdjustedCompletion);

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

            // Show numeric energy value
            int displayValue = Math.Clamp(instance.timeLeft, 0, (int)BalanceAegisBlade.EnergyMax);
            Vector2 textOffset = new Vector2(displayValue > 9 ? -11f : -6f, 10f);
            DrawBorderStringEightWay(
                spriteBatch, FontAssets.MouseText.Value, displayValue.ToString(),
                position + textOffset * scale, new Color(255, 245, 180), Color.Black, scale);
        }

        public override void DrawCompact(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            Texture2D sprite   = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D outline  = ModContent.Request<Texture2D>(OutlineTexture).Value;
            Texture2D overlay  = ModContent.Request<Texture2D>(OverlayTexture).Value;

            spriteBatch.Draw(outline, position, null, OutlineColor * opacity, 0f, outline.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(sprite,  position, null, Color.White  * opacity, 0f, sprite.Size()  * 0.5f, scale, SpriteEffects.None, 0f);

            // Overlay mask — crop from top based on how full the bar is
            int lostHeight = (int)Math.Ceiling(overlay.Height * AdjustedCompletion);
            Rectangle crop  = new Rectangle(0, lostHeight, overlay.Width, overlay.Height - lostHeight);
            spriteBatch.Draw(overlay, position + Vector2.UnitY * lostHeight * scale, crop,
                OutlineColor * opacity * 0.9f, 0f, sprite.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            int displayValue = Math.Clamp(instance.timeLeft, 0, (int)BalanceAegisBlade.EnergyMax);
            Vector2 textOffset = new Vector2(displayValue > 9 ? -11f : -6f, 10f);
            DrawBorderStringEightWay(
                spriteBatch, FontAssets.MouseText.Value, displayValue.ToString(),
                position + textOffset * scale, new Color(255, 245, 180), Color.Black, scale);
        }
    }
}
