using CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFGoliath_MouseCrosshair : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 10;
            Projectile.timeLeft = 42;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Lighting.AddLight(Projectile.Center, PFLeftEffectRules.GetThemeColor(Projectile, new Color(139, 242, 73)).ToVector3() * 0.55f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            BBSD_Lock_Effects.DrawTargetingReticle(Projectile.Center, null, true);
            return false;
        }
    }
}
