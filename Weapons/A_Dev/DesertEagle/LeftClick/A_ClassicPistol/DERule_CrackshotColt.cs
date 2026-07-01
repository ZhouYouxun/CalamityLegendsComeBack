using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.Slot;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.A_ClassicPistol
{
    public class DERule_CrackshotColt : DEBulletRule
    {
        private static readonly Color Gold = new(255, 214, 72);
        private static readonly Color GoldWhite = new(255, 248, 170);

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.CrackshotColt>();

        public override float GetShotExtra(DesertEagleSlotPlayer slotPlayer) => 1f;

        public override void SetDefaults(Projectile projectile)
        {
            projectile.light = 0.65f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            DEBulletUtils.OrientToVelocity(projectile);
            DEBulletUtils.TrailDust(projectile, DustID.GoldFlame, Gold, 1f, 0.18f);
            DEBulletUtils.GlowTrail(projectile, GoldWhite, 1.05f);
            Lighting.AddLight(projectile.Center, Gold.ToVector3() * 0.55f);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            DEBulletUtils.ParticleBurst(projectile.Center, Gold, 0.82f);
            DEBulletUtils.BurstDust(projectile.Center, GoldWhite, DustID.GoldFlame, 12, 5.5f, 1f);

            if (projectile.ai[1] <= 0f || Main.myPlayer != projectile.owner)
                return;

            NPC next = DEBulletUtils.FindTarget(projectile.Center, 560f, projectile, target);
            if (next == null)
                return;

            Vector2 direction = projectile.DirectionTo(next.Center);
            Projectile.NewProjectile(
                projectile.GetSource_FromAI(),
                projectile.Center + direction * 12f,
                direction * projectile.velocity.Length() * 1.08f,
                ModContent.ProjectileType<DELeftBullet>(),
                projectile.damage,
                projectile.knockBack,
                projectile.owner,
                projectile.ai[0],
                0f);
        }

        public override string TooltipEffectEN => "Fires a golden marksman round; on hit, it ricochets once to a nearby enemy";
        public override string TooltipEffectZH => "发射金色神枪手弹，命中后向附近敌人弹射一次";
    }
}
