using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.B_HardMode
{
    public class DERule_Arietes41 : DEBulletRule
    {
        private static readonly Color LifeWhite = new(245, 255, 255);

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.Arietes41>();

        public override float SpeedMultiplier => 1.06f;

        public override void SetDefaults(Projectile projectile)
        {
            projectile.light = 0.6f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            DEBulletUtils.OrientToVelocity(projectile);
            DEBulletUtils.TrailDust(projectile, DustID.GemDiamond, LifeWhite, 0.9f, 0.16f);
            DEBulletUtils.GlowTrail(projectile, LifeWhite, 1f);
            Lighting.AddLight(projectile.Center, LifeWhite.ToVector3() * 0.45f);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            DEBulletUtils.SpawnLifeSteal(owner, target, projectile, (int)(hit.Damage * 0.05f), 0.75f);
            DEBulletUtils.ParticleBurst(projectile.Center, LifeWhite, 0.72f);
        }

        public override string TooltipEffectEN => "Fires a white life-stealing round";
        public override string TooltipEffectZH => "发射白色吸血弹";
    }
}
