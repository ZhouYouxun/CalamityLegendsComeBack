using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.Slot;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.B_HardMode
{
    public class DERule_MidasPrime : DEBulletRule
    {
        private static readonly Color MidasGold = new(255, 188, 44);

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.MidasPrime>();

        public override float GetShotExtra(DesertEagleSlotPlayer slotPlayer) => 3f;

        public override void AI(Projectile projectile, Player owner)
        {
            DEBulletUtils.OrientToVelocity(projectile);
            DEBulletUtils.TrailDust(projectile, DustID.GoldFlame, MidasGold, 1.18f, 0.16f);
            DEBulletUtils.GlowTrail(projectile, Color.Gold, 1.2f);
            Lighting.AddLight(projectile.Center, MidasGold.ToVector3() * 0.62f);
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
            float consumedBounces = 3f - MathHelper.Clamp(projectile.ai[1], 0f, 3f);
            modifiers.SourceDamage *= 1f + consumedBounces * 0.18f;
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Midas, 300);
            DEBulletUtils.ParticleBurst(projectile.Center, MidasGold, 0.95f);

            if (projectile.ai[1] <= 0f)
            {
                if (Main.myPlayer == projectile.owner)
                    DEBulletUtils.SpawnAreaBurst(projectile.GetSource_FromAI(), target.Center, Math.Max(1, (int)(hit.Damage * 0.35f)), projectile.knockBack, projectile.owner, DEBurstStyle.Gold, 78f);
                return;
            }

            if (Main.myPlayer != projectile.owner)
                return;

            NPC next = DEBulletUtils.FindTarget(projectile.Center, 640f, projectile, target);
            if (next == null)
                return;

            Vector2 direction = projectile.DirectionTo(next.Center);
            Projectile.NewProjectile(
                projectile.GetSource_FromAI(),
                projectile.Center + direction * 12f,
                direction * projectile.velocity.Length() * 1.12f,
                ModContent.ProjectileType<DELeftBullet>(),
                projectile.damage,
                projectile.knockBack,
                projectile.owner,
                projectile.ai[0],
                projectile.ai[1] - 1f);
        }

        public override string TooltipEffectEN => "A stronger gold chain round; up to 3 ricochets, gaining damage after each jump";
        public override string TooltipEffectZH => "强化金色连锁弹，最多弹射3次，每次跳跃后伤害提高";
    }
}
