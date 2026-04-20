using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast
{
    // 轰炸叶绿体：负责在左键尾音里补上一口小范围爆发。
    internal sealed class Chlo_DBomb : BlossomFluxChloroplastPreset
    {
        public static Chlo_DBomb Instance { get; } = new();

        private Chlo_DBomb()
        {
        }

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            projectile.penetrate = 1;
            projectile.timeLeft = 120;
            projectile.localNPCHitCooldown = 10;
        }

        public override void AI(Projectile projectile)
        {
            Lighting.AddLight(projectile.Center, ChloroplastCommon.PresetColor(BlossomFluxChloroplastPresetType.Chlo_DBomb).ToVector3() * 0.34f);
            ChloroplastCommon.EmitTrail(projectile, BlossomFluxChloroplastPresetType.Chlo_DBomb, 1.04f);
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<BFFuckyouExplosion>(),
                (int)(projectile.damage * 0.55f),
                projectile.knockBack * 0.5f,
                projectile.owner);

            ChloroplastCommon.EmitBurst(projectile, BlossomFluxChloroplastPresetType.Chlo_DBomb, 14, 1.1f, 4.2f);
        }

        public override void OnKill(Projectile projectile, int timeLeft)
        {
            ChloroplastCommon.EmitBurst(projectile, BlossomFluxChloroplastPresetType.Chlo_DBomb, 12, 1.1f, 3.6f);
        }

        public override bool OnTileCollide(Projectile projectile, Vector2 oldVelocity)
        {
            return true;
        }

        public override bool? CanDamage(Projectile projectile)
        {
            return null;
        }

        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            ChloroplastCommon.DrawPresetProjectile(projectile, BlossomFluxChloroplastPresetType.Chlo_DBomb, lightColor, 1.03f);
            return false;
        }
    }
}
