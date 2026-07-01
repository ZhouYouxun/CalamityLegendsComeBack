using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.C_PostPlantera
{
    public class DERule_Hellborn : DEBulletRule
    {
        private static readonly Color HellFire = new(255, 52, 24);

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.Hellborn>();

        public override float SpeedMultiplier => 0.95f;
        public override float DamageMultiplier => 1.08f;

        public override void SetDefaults(Projectile projectile)
        {
            projectile.width = 18;
            projectile.height = 18;
            projectile.light = 0.85f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            projectile.velocity *= 1.006f;
            DEBulletUtils.OrientToVelocity(projectile);
            DEBulletUtils.TrailDust(projectile, DustID.FireworksRGB, HellFire, 1.15f, 0.14f);
            DEBulletUtils.GlowTrail(projectile, HellFire, 1.2f);
            Lighting.AddLight(projectile.Center, HellFire.ToVector3() * 0.7f);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Dragonfire>(), 240);

            if (Main.myPlayer == projectile.owner)
                DEBulletUtils.SpawnAreaBurst(projectile.GetSource_FromAI(), target.Center, Math.Max(1, (int)(hit.Damage * 0.55f)), projectile.knockBack, projectile.owner, DEBurstStyle.Hellborn, 82f);
        }

        public override string TooltipEffectEN => "The bullet is hellfire; spinning Desert Eagle contact causes violent explosions and left-click overdrive";
        public override string TooltipEffectZH => "子弹附带地狱火；右键旋转本体撞敌会剧烈爆炸并短暂大幅提升左键射速";
    }
}
