using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.SeedOfSilva
{
    internal sealed class SeedOfSilvaSunflower : SeedOfSilvaFlowerProjectile
    {
        public const float SunflowerRingRadius = 92f;

        private int flashTimer;

        protected override int FlowerSlot => 0;
        protected override BlossomFluxChloroplastPresetType FlowerPreset => BlossomFluxChloroplastPresetType.Chlo_ABreak;
        protected override string FlowerTexturePath => "CalamityLegendsComeBack/Accssory/BF/Skill/SeedOfSilva/SeedPack/Sunflower";

        public void TriggerFlash()
        {
            if (flashTimer <= 0 && !Main.dedServ)
            {
                for (int i = 0; i < 5; i++)
                {
                    Dust dust = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(SunflowerRingRadius * 0.55f, SunflowerRingRadius * 0.55f),
                        DustID.YellowTorch,
                        Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.5f, 3.8f),
                        100, new Color(255, 240, 100), Main.rand.NextFloat(0.7f, 1.0f));
                    dust.noGravity = true;
                }
            }
            flashTimer = System.Math.Max(flashTimer, 10);
        }

        protected override void UpdateCommon(Player owner, BFAccessoryPlayer accessoryPlayer)
        {
            if (flashTimer > 0)
                flashTimer--;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (IsBlooming)
                DrawBloomRing();

            return base.PreDraw(ref lightColor);
        }

        private void DrawBloomRing()
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 center = Projectile.Center - Main.screenPosition;

            float timePulse = 0.93f + 0.07f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 2.6f + Projectile.identity);
            float flashIntensity = flashTimer > 0 ? Utils.GetLerpValue(0f, 10f, flashTimer, true) : 0f;
            float ringRadius = SunflowerRingRadius * timePulse;

            Color ringColor = FlowerColor with { A = 0 };
            Color accentColor = FlowerAccentColor with { A = 0 };

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            int segments = 24;
            float segScale = ringRadius * MathHelper.TwoPi / (segments * bloom.Width) * 2.0f;
            float baseAlpha = 0.2f + flashIntensity * 0.38f;
            Color segColor = Color.Lerp(ringColor, accentColor, flashIntensity) * (baseAlpha * Projectile.Opacity);

            for (int i = 0; i < segments; i++)
            {
                float angle = MathHelper.TwoPi * i / segments;
                Vector2 pos = center + angle.ToRotationVector2() * ringRadius;
                Main.EntitySpriteDraw(bloom, pos, null, segColor, 0f, bloom.Size() * 0.5f, segScale, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(bloom, center, null, ringColor * (0.07f * Projectile.Opacity * timePulse), 0f, bloom.Size() * 0.5f, 0.18f, SpriteEffects.None, 0);

            if (flashIntensity > 0f)
                Main.EntitySpriteDraw(bloom, center, null, accentColor * (flashIntensity * 0.32f * Projectile.Opacity), 0f, bloom.Size() * 0.5f, 0.26f, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
