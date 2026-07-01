using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.D_Endgame
{
    public class DERule_GoldenEagle : DEBulletRule
    {
        private static readonly Color Gold = new(255, 204, 65);

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.GoldenEagle>();

        public override float DamageMultiplier => 0.55f;
        public override float SpeedMultiplier => 1.18f;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            if (projectile.ai[1] != 0f || Main.myPlayer != projectile.owner)
                return;

            projectile.ai[1] = 1f;
            float[] spreads =
            {
                MathHelper.ToRadians(-8f),
                MathHelper.ToRadians(-4f),
                MathHelper.ToRadians(4f),
                MathHelper.ToRadians(8f)
            };

            foreach (float spread in spreads)
            {
                Projectile.NewProjectile(
                    projectile.GetSource_FromAI(),
                    projectile.Center,
                    projectile.velocity.RotatedBy(spread),
                    projectile.type,
                    projectile.damage,
                    projectile.knockBack,
                    projectile.owner,
                    projectile.ai[0],
                    1f);
            }
        }

        public override void AI(Projectile projectile, Player owner)
        {
            DEBulletUtils.OrientToVelocity(projectile);
            DEBulletUtils.TrailDust(projectile, DustID.GoldFlame, Gold, 1f, 0.12f);
            DEBulletUtils.GlowTrail(projectile, Gold, 1f);
            Lighting.AddLight(projectile.Center, Gold.ToVector3() * 0.55f);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Midas, 240);
            DEBulletUtils.ParticleBurst(projectile.Center, Gold, 0.65f);
        }

        public override string TooltipEffectEN => "Scatters five golden rounds at once";
        public override string TooltipEffectZH => "一次散射五发黄金弹幕";
    }
}
