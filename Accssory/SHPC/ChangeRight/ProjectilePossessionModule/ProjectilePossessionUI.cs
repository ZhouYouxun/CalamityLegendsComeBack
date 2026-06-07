using CalamityLegendsComeBack.Weapons.SHPC.RightClick;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.ChangeRight.ProjectilePossessionModule
{
    internal sealed class ProjectilePossessionUI : ModProjectile
    {
        private const int BarPulseDuration = 24;

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

            Projectile.Center = owner.Center + new Vector2(0f, -74f);
            Projectile.timeLeft = 2;

            ProjectilePossessionModulePlayer possessionPlayer = owner.GetModPlayer<ProjectilePossessionModulePlayer>();
            bool shouldStay = possessionPlayer.HasActivePossessionHoldout() ||
                possessionPlayer.AbsorbedProjectileCount > 0 ||
                possessionPlayer.PossessionUiFadeTimer > 0;

            float targetOpacity = possessionPlayer.HasActivePossessionHoldout() || possessionPlayer.AbsorbedProjectileCount > 0
                ? 1f
                : possessionPlayer.GetPossessionUiOpacity();
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
            ProjectilePossessionModulePlayer possessionPlayer = owner.GetModPlayer<ProjectilePossessionModulePlayer>();
            if ((!possessionPlayer.HasActivePossessionHoldout() && possessionPlayer.AbsorbedProjectileCount <= 0 && possessionPlayer.PossessionUiFadeTimer <= 0) ||
                Projectile.Opacity <= 0.03f)
                return false;

            Texture2D barBG = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/SHPC/RightClick/SHPCBarBack").Value;
            Texture2D barFG = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/SHPC/RightClick/SHPCBarFront").Value;

            float progress = possessionPlayer.GetPossessionProgress();
            float opacity = Projectile.Opacity;
            Vector2 drawPos = owner.Center - Main.screenPosition + new Vector2(0f, -74f) - barBG.Size() / 1.5f;
            int heatLevel = possessionPlayer.GetDisplayedPossessionLevel();
            Color backColor = Color.White * opacity;
            Color frontColor = Color.Lerp(new Color(70, 210, 255), new Color(255, 88, 214), progress) * opacity;

            SHPCHeatBarDrawer.DrawHeatBackOutline(Main.spriteBatch, barBG, drawPos, heatLevel, opacity, 1.5f);
            SHPCHeatBarDrawer.DrawOutlinePulse(Main.spriteBatch, barBG, drawPos, 1.5f, opacity, possessionPlayer.PossessionBarPulseTimer, BarPulseDuration);
            if (possessionPlayer.PossessionBarPulseTimer > 0)
                possessionPlayer.PossessionBarPulseTimer--;

            SHPCHeatBarDrawer.Draw(Main.spriteBatch, barBG, barFG, drawPos, progress, backColor, frontColor, 1.5f);
            SHPCHeatBarDrawer.DrawHeatStar(Main.spriteBatch, barBG, drawPos, heatLevel, opacity, 1.5f);

            string countText = $"{possessionPlayer.AbsorbedProjectileCount}/{ProjectilePossessionModulePlayer.MaxAbsorbedProjectiles}";
            CalamityUtils.DrawBorderStringEightWay(
                Main.spriteBatch,
                FontAssets.MouseText.Value,
                countText,
                drawPos + new Vector2(48f, -8f),
                Color.Lerp(new Color(148, 238, 255), Color.White, 0.25f) * opacity,
                Color.Black * opacity,
                0.78f);

            return false;
        }
    }
}
