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

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.Barrier
{
    internal sealed class BarrierDurabilityCooldown : CooldownHandler
    {
        private BarrierPlayer BarrierPlayer => instance.player.GetModPlayer<BarrierPlayer>();
        private float AdjustedCompletion => BarrierPlayer.ShieldChargeRatio;

        public static new string ID => "SHPC_BarrierDurability";
        public override bool CanTickDown => false;
        public override bool SavedWithPlayer => false;
        public override bool PersistsThroughDeath => false;
        public override bool ShouldDisplay =>
            BarrierPlayer.BarrierEquipped &&
            BarrierPlayer.HoldingSHPC &&
            BarrierPlayer.ShieldCurrentHitPoints > 0;

        public override LocalizedText DisplayName =>
            Language.GetText("Mods.CalamityLegendsComeBack.Cooldowns.SHPC_BarrierDurability");

        public override string Texture => "CalamityLegendsComeBack/Weapons/SHPC/EXSkill/EXCoolDown";
        public override string OutlineTexture => "CalamityLegendsComeBack/Weapons/SHPC/EXSkill/EXCoolDownOutline";
        public override string OverlayTexture => "CalamityLegendsComeBack/Weapons/SHPC/EXSkill/EXCoolDownOverlay";

        public override Color OutlineColor => new(112, 244, 244);
        public override Color CooldownStartColor => Color.Lerp(new Color(49, 220, 221), new Color(99, 226, 142), AdjustedCompletion);
        public override Color CooldownEndColor => CooldownStartColor;

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
            int hitPoints = BarrierPlayer.ShieldCurrentHitPoints;
            DrawBorderStringEightWay(
                spriteBatch,
                FontAssets.MouseText.Value,
                hitPoints.ToString(),
                position + new Vector2(hitPoints > 9 ? -10f : -5f, 4f) * scale,
                Color.Lerp(new Color(49, 220, 221), Color.OrangeRed, 1f - AdjustedCompletion) * opacity,
                Color.Black * opacity,
                scale);
        }
    }

    internal sealed class BarrierRechargeCooldown : CooldownHandler
    {
        private BarrierPlayer BarrierPlayer => instance.player.GetModPlayer<BarrierPlayer>();
        private int DisplayValue => Math.Max(1, (int)Math.Ceiling(instance.timeLeft / 60f));

        public static new string ID => "SHPC_BarrierRecharge";
        public override bool CanTickDown => false;
        public override bool SavedWithPlayer => false;
        public override bool PersistsThroughDeath => false;
        public override bool ShouldDisplay =>
            BarrierPlayer.BarrierEquipped &&
            BarrierPlayer.HoldingSHPC &&
            BarrierPlayer.RechargeDelayRemainingFrames > 0;

        public override LocalizedText DisplayName =>
            Language.GetText("Mods.CalamityLegendsComeBack.Cooldowns.SHPC_BarrierRecharge");

        public override string Texture => "CalamityLegendsComeBack/Weapons/SHPC/EXSkill/EXCoolDown";
        public override string OutlineTexture => "CalamityLegendsComeBack/Weapons/SHPC/EXSkill/EXCoolDownOutline";
        public override string OverlayTexture => "CalamityLegendsComeBack/Weapons/SHPC/EXSkill/EXCoolDownOverlay";

        public override Color OutlineColor => new(194, 255, 67);
        public override Color CooldownStartColor => Color.Lerp(new Color(194, 255, 57), new Color(92, 187, 99), instance.Completion);
        public override Color CooldownEndColor => CooldownStartColor;

        public override void DrawExpanded(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            base.DrawExpanded(spriteBatch, position, opacity, scale);
            DrawCounter(spriteBatch, position, opacity, scale);
        }

        public override void DrawCompact(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            base.DrawCompact(spriteBatch, position, opacity, scale);
            DrawCounter(spriteBatch, position, opacity, scale);
        }

        private void DrawCounter(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            DrawBorderStringEightWay(
                spriteBatch,
                FontAssets.MouseText.Value,
                DisplayValue.ToString(),
                position + new Vector2(DisplayValue > 9 ? -10f : -5f, 4f) * scale,
                Color.White * opacity,
                Color.Black * opacity,
                scale);
        }
    }
}
