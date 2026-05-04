using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.RightClick
{
    internal sealed class SHPCRight_HeatUI : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            SHPCRight_Player heatPlayer = owner.GetModPlayer<SHPCRight_Player>();
            Projectile.Center = owner.Center + new Vector2(0f, -56f);
            Projectile.timeLeft = 2;

            bool holdingHoldout = heatPlayer.HasActiveRightClickHoldout();
            bool holdingSHPC = heatPlayer.IsHoldingSHPCLike();
            bool hasHeat = heatPlayer.HasAnyHeat();
            bool shouldStay = hasHeat && (holdingHoldout || holdingSHPC || heatPlayer.HeatUiFadeTimer > 0);

            float targetOpacity = hasHeat && (holdingHoldout || holdingSHPC) ? 1f : 0f;
            float lerpAmount = targetOpacity > Projectile.Opacity ? 0.25f : 0.35f;
            Projectile.Opacity = MathHelper.Lerp(Projectile.Opacity, targetOpacity, lerpAmount);

            if (!shouldStay && Projectile.Opacity <= 0.03f)
                Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner)
                return false;

            Player owner = Main.player[Projectile.owner];
            SHPCRight_Player heatPlayer = owner.GetModPlayer<SHPCRight_Player>();
            if (heatPlayer.HasActiveRightClickHoldout() || !heatPlayer.HasAnyHeat() || Projectile.Opacity <= 0.03f)
                return false;

            Texture2D barBG = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/SHPC/RightClick/SHPCBarBack").Value;
            Texture2D barFG = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/SHPC/RightClick/SHPCBarFront").Value;

            float progress = heatPlayer.GetDetachedHeatProgress();
            int maxHeat = Utils.Clamp(heatPlayer.HeatMaxStage, 1, 5);
            float heatColorProgress = MathHelper.Clamp((heatPlayer.HeatStage + progress) / maxHeat, 0f, 1f);
            Color color = GetHeatBarColor(heatColorProgress) * Projectile.Opacity;

            Vector2 drawPos = owner.Center - Main.screenPosition + new Vector2(0f, -56f) - barBG.Size() / 1.5f;
            Rectangle frameCrop = new Rectangle(0, 0, (int)(barFG.Width * progress), barFG.Height);

            Main.spriteBatch.Draw(barBG, drawPos, null, color, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(barFG, drawPos, frameCrop, color * 0.8f, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 0f);

            return false;
        }

        private static Color GetHeatBarColor(float progress)
        {
            progress = MathHelper.Clamp(progress, 0f, 1f);

            if (progress < 0.25f)
                return Color.Lerp(new Color(70, 210, 255), new Color(120, 245, 255), progress / 0.25f);

            if (progress < 0.5f)
                return Color.Lerp(new Color(120, 245, 255), new Color(255, 230, 90), (progress - 0.25f) / 0.25f);

            if (progress < 0.75f)
                return Color.Lerp(new Color(255, 230, 90), new Color(255, 112, 67), (progress - 0.5f) / 0.25f);

            return Color.Lerp(new Color(255, 112, 67), Color.White, (progress - 0.75f) / 0.25f);
        }
    }
}
