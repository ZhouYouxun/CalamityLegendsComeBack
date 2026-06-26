using System;
using CalamityLegendsComeBack.Accssory;
using CalamityLegendsComeBack.Weapons.Malachite;
using CalamityMod.Cooldowns;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.Localization;
using Terraria.ModLoader;
using static CalamityMod.CalamityUtils;

namespace CalamityLegendsComeBack.Weapons.Malachite.EXSkill
{
    internal sealed class MalachiteEXCooldown : CooldownHandler
    {
        public const int CooldownFrames = 30 * 60;

        public static new string ID => "Malachite_EX";

        private float ReadyCompletion => MathHelper.Clamp(1f - instance.timeLeft / (float)CooldownFrames, 0f, 1f);
        private int DisplayValue => Math.Max(0, (instance.timeLeft + 59) / 60);

        public override bool CanTickDown => true;

        public override bool ShouldDisplay =>
            instance.player.HeldItem.type == ModContent.ItemType<Malachite>() &&
            instance.player.GetModPlayer<LegendaryEmblemPlayer>().EXAccessoryEquipped;

        public override LocalizedText DisplayName =>
            Language.GetText("Mods.CalamityLegendsComeBack.Cooldowns.Malachite_EX");

        public override string Texture => "CalamityLegendsComeBack/Weapons/Malachite/EXSkill/EXCoolDown";
        public override string OutlineTexture => "CalamityLegendsComeBack/Weapons/Malachite/EXSkill/EXCoolDownOutline";
        public override string OverlayTexture => "CalamityLegendsComeBack/Weapons/Malachite/EXSkill/EXCoolDownOverlay";

        public override Color OutlineColor => new(12, 54, 28);
        public override Color CooldownStartColor => Color.Lerp(new Color(32, 82, 45), new Color(82, 255, 132), ReadyCompletion);
        public override Color CooldownEndColor => Color.Lerp(new Color(12, 30, 18), new Color(210, 255, 190), ReadyCompletion);

        public override void ApplyBarShaders(float opacity)
        {
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseOpacity(opacity);
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseSaturation(ReadyCompletion);
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

            int lostHeight = (int)Math.Ceiling(overlay.Height * ReadyCompletion);
            Rectangle crop = new(0, lostHeight, overlay.Width, overlay.Height - lostHeight);
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

            DrawCounter(spriteBatch, position, opacity, scale);
        }

        private void DrawCounter(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            if (instance.timeLeft <= 0)
                return;

            Vector2 textOffset = new(DisplayValue > 99 ? -15f : DisplayValue > 9 ? -11f : -6f, 10f);
            DrawBorderStringEightWay(
                spriteBatch,
                FontAssets.MouseText.Value,
                DisplayValue.ToString(),
                position + textOffset * scale,
                Color.White * opacity,
                Color.Black * opacity,
                scale);
        }
    }
}
