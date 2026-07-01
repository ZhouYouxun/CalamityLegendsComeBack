using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.SubProjectiles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.B_HardMode
{
    public class DERule_Needler : DEBulletRule
    {
        private static readonly Color NeedleGreen = new(125, 255, 90);

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.Needler>();

        public override int ExtraUpdates => 2;

        public override void SetDefaults(Projectile projectile)
        {
            projectile.width = 10;
            projectile.height = 10;
            projectile.light = 0.45f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            DEBulletUtils.OrientToVelocity(projectile);
            DEBulletUtils.SimpleHoming(projectile, 360f, 0.035f, projectile.velocity.Length());
            DEBulletUtils.TrailDust(projectile, DustID.Poisoned, NeedleGreen, 0.82f, 0.18f);
            Lighting.AddLight(projectile.Center, NeedleGreen.ToVector3() * 0.35f);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Venom, 240);

            if (Main.myPlayer != projectile.owner)
                return;

            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < 7; i++)
            {
                float angle = MathHelper.Lerp(-1.15f, 1.15f, i / 6f);
                Vector2 velocity = forward.RotatedBy(angle) * Main.rand.NextFloat(8f, 13f);
                Projectile.NewProjectile(
                    projectile.GetSource_FromAI(),
                    target.Center + velocity.SafeNormalize(Vector2.UnitX) * 10f,
                    velocity,
                    ModContent.ProjectileType<DEBullet_NeedleSpike>(),
                    Math.Max(1, (int)(hit.Damage * 0.26f)),
                    projectile.knockBack * 0.35f,
                    projectile.owner);
            }

            DEBulletUtils.SpawnAreaBurst(projectile.GetSource_FromAI(), target.Center, Math.Max(1, (int)(hit.Damage * 0.28f)), projectile.knockBack, projectile.owner, DEBurstStyle.Needle, 58f);
        }

        public override string TooltipEffectEN => "Homing leaf needle; on hit, it bursts into several poisonous spike shards";
        public override string TooltipEffectZH => "追踪叶针弹，命中时爆出多枚毒性尖刺破片";
    }
}
