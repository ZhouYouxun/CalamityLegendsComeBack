using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.RightClick
{
    internal sealed class SHPCRight_HeatUI : ModProjectile
    {
        private const int HeatBarOutlineDuration = 24;

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
            if (heatPlayer.ShouldSuppressHeatUI())
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.Center + new Vector2(0f, -56f);
            Projectile.timeLeft = 2;

            bool hasHeat = heatPlayer.HasAnyHeat();
            bool shouldStay = hasHeat || heatPlayer.HeatUiFadeTimer > 0;

            float targetOpacity = hasHeat ? 1f : heatPlayer.GetHeatUiFadeOpacity();
            float lerpAmount = targetOpacity > Projectile.Opacity ? 0.25f : 0.08f;
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
            if (heatPlayer.ShouldSuppressHeatUI() ||
                (!heatPlayer.HasAnyHeat() && heatPlayer.HeatUiFadeTimer <= 0) ||
                Projectile.Opacity <= 0.03f)
                return false;

            Texture2D barBG = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/SHPC/RightClick/SHPCBarBack").Value;
            Texture2D barFG = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/SHPC/RightClick/SHPCBarFront").Value;

            float progress = heatPlayer.GetDetachedHeatProgress();
            Color color = Color.White * Projectile.Opacity;

            Vector2 drawPos = owner.Center - Main.screenPosition + new Vector2(0f, -56f) - barBG.Size() / 1.5f;

            int heatLevel = heatPlayer.GetDisplayedHeatLevel();
            SHPCHeatBarDrawer.DrawHeatBackOutline(Main.spriteBatch, barBG, drawPos, heatLevel, Projectile.Opacity, 1.5f);
            SHPCHeatBarDrawer.DrawOutlinePulse(Main.spriteBatch, barBG, drawPos, 1.5f, Projectile.Opacity, heatPlayer.HeatBarOutlineTimer, HeatBarOutlineDuration);
            if (heatPlayer.HeatBarOutlineTimer > 0)
                heatPlayer.HeatBarOutlineTimer--;

            SHPCHeatBarDrawer.Draw(Main.spriteBatch, barBG, barFG, drawPos, progress, color, color, 1.5f);
            SHPCHeatBarDrawer.DrawHeatStar(Main.spriteBatch, barBG, drawPos, heatLevel, Projectile.Opacity, 1.5f);

            return false;
        }
    }
}
