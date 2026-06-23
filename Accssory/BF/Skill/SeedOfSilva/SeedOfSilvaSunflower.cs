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
                for (int i = 0; i < 9; i++)
                {
                    Dust dust = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(SunflowerRingRadius * 0.6f, SunflowerRingRadius * 0.6f),
                        DustID.YellowTorch,
                        Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.5f, 5.2f),
                        70, new Color(255, 245, 120), Main.rand.NextFloat(0.95f, 1.3f));
                    dust.noGravity = true;
                }
            }
            flashTimer = System.Math.Max(flashTimer, 14);
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
            float flashIntensity = flashTimer > 0 ? Utils.GetLerpValue(0f, 14f, flashTimer, true) : 0f;
            float ringRadius = SunflowerRingRadius * timePulse;

            Color ringColor = FlowerColor with { A = 0 };
            Color accentColor = FlowerAccentColor with { A = 0 };

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // Outer main ring
            int segments = 32;
            float segScale = ringRadius * MathHelper.TwoPi / (segments * bloom.Width) * 2.2f;
            float baseAlpha = 0.52f + flashIntensity * 0.55f;
            Color segColor = Color.Lerp(ringColor, accentColor, 0.28f + flashIntensity * 0.45f) * (baseAlpha * Projectile.Opacity);

            for (int i = 0; i < segments; i++)
            {
                float angle = MathHelper.TwoPi * i / segments;
                Vector2 pos = center + angle.ToRotationVector2() * ringRadius;
                Main.EntitySpriteDraw(bloom, pos, null, segColor, 0f, bloom.Size() * 0.5f, segScale, SpriteEffects.None, 0);
            }

            // Inner brighter accent ring at 82% radius
            float innerRadius = ringRadius * 0.82f;
            float innerSegScale = innerRadius * MathHelper.TwoPi / (segments * bloom.Width) * 1.5f;
            Color innerColor = Color.Lerp(ringColor, Color.White, 0.2f) with { A = 0 } * (0.28f * Projectile.Opacity);
            for (int i = 0; i < segments; i++)
            {
                float angle = MathHelper.TwoPi * i / segments + 0.05f;
                Vector2 pos = center + angle.ToRotationVector2() * innerRadius;
                Main.EntitySpriteDraw(bloom, pos, null, innerColor, 0f, bloom.Size() * 0.5f, innerSegScale, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(bloom, center, null, ringColor * (0.18f * Projectile.Opacity * timePulse), 0f, bloom.Size() * 0.5f, 0.24f, SpriteEffects.None, 0);

            if (flashIntensity > 0f)
                Main.EntitySpriteDraw(bloom, center, null, accentColor * (flashIntensity * 0.58f * Projectile.Opacity), 0f, bloom.Size() * 0.5f, 0.36f, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
