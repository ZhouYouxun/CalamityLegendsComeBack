using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.A_ClassicPistol
{
    public class DERule_Fungicide : DEBulletRule
    {
        private static readonly Color FungalBlue = new(48, 215, 255);

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.Fungicide>();

        public override void SetDefaults(Projectile projectile)
        {
            projectile.width = 12;
            projectile.height = 12;
            projectile.light = 0.55f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            projectile.rotation += 0.2f;
            DEBulletUtils.TrailDust(projectile, DustID.BlueTorch, FungalBlue, 0.95f, 0.2f);
            DEBulletUtils.GlowTrail(projectile, Color.Lerp(FungalBlue, Color.White, 0.2f), 1f);
            Lighting.AddLight(projectile.Center, FungalBlue.ToVector3() * 0.5f);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<DesertEagleFungicideGlobalNPC>().AddFungicideStack(target, projectile, hit.Damage);
        }

        public override bool OnTileCollide(Projectile projectile, Player owner, Vector2 oldVelocity)
        {
            DEBulletUtils.BurstDust(projectile.Center, FungalBlue, DustID.BlueTorch, 14, 5f, 1.05f);
            DEBulletUtils.ParticleBurst(projectile.Center, FungalBlue, 0.75f);
            return true;
        }

        public override string TooltipEffectEN => "Adds a blue fungal stack on hit; the 4th stack detonates in a small burst and clears";
        public override string TooltipEffectZH => "命中叠加蓝色真菌层数，第4层触发小范围爆炸并清空";
    }
}
