using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle.Proj
{
    internal sealed class AMRCooldownStatusBar : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AntiMaterielRifle";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
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

            AMRPlayer amrPlayer = owner.GetModPlayer<AMRPlayer>();
            if (amrPlayer.SlideCooldown <= 0)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.Center + new Vector2(0f, 42f * owner.gravDir);
            Projectile.timeLeft = 2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];
            AMRPlayer amrPlayer = owner.GetModPlayer<AMRPlayer>();
            int cooldown = amrPlayer.SlideCooldown;
            if (cooldown <= 0)
                return false;

            float completion = 1f - MathHelper.Clamp(cooldown / (float)AMRBalance.SlideCooldownFrames, 0f, 1f);
            int barWidth = 42;
            int barHeight = 6;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Rectangle bgRect = new((int)drawPos.X - barWidth / 2, (int)drawPos.Y - barHeight / 2, barWidth, barHeight);
            Rectangle fgRect = new(bgRect.X + 1, bgRect.Y + 1, (int)((barWidth - 2) * completion), barHeight - 2);

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, bgRect, new Color(18, 14, 10) * 0.75f);

            Color barColor = completion >= 1f ? new Color(255, 202, 81) : Color.Lerp(new Color(130, 95, 30), new Color(255, 195, 58), completion);
            Main.spriteBatch.Draw(pixel, fgRect, barColor * 0.9f);

            return false;
        }
    }
}
