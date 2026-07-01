using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.A_ClassicPistol
{
    public class DERule_PhoenixBlaster : DEBulletRule
    {
        private static readonly Color Fire = new(255, 118, 34);

        public override int GunItemType => ItemID.PhoenixBlaster;

        public override void SetDefaults(Projectile projectile)
        {
            projectile.light = 0.7f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            DEBulletUtils.OrientToVelocity(projectile);
            DEBulletUtils.TrailDust(projectile, DustID.Torch, Fire, 1.15f, 0.2f);
            DEBulletUtils.TrailDust(projectile, DustID.OrangeTorch, Color.OrangeRed, 0.95f, 0.12f);
            DEBulletUtils.GlowTrail(projectile, Color.Orange, 1.1f);
            Lighting.AddLight(projectile.Center, Fire.ToVector3() * 0.6f);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 240);

            if (Main.myPlayer == projectile.owner)
            {
                DEBulletUtils.SpawnAreaBurst(
                    projectile.GetSource_FromAI(),
                    projectile.Center,
                    Math.Max(1, (int)(hit.Damage * 0.45f)),
                    projectile.knockBack,
                    projectile.owner,
                    DEBurstStyle.Fire,
                    58f);
            }
        }

        public override bool OnTileCollide(Projectile projectile, Player owner, Vector2 oldVelocity)
        {
            if (Main.myPlayer == projectile.owner)
            {
                DEBulletUtils.SpawnAreaBurst(
                    projectile.GetSource_FromAI(),
                    projectile.Center,
                    Math.Max(1, (int)(projectile.damage * 0.35f)),
                    projectile.knockBack,
                    projectile.owner,
                    DEBurstStyle.Fire,
                    48f);
            }

            return true;
        }

        public override string TooltipEffectEN => "A normal bullet wreathed in flame particles; impact creates a small fire explosion";
        public override string TooltipEffectZH => "普通子弹附带火焰粒子，命中时产生小规模火焰爆炸";
    }
}
