using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.SubProjectiles
{
    /// <summary>
    /// SlagMagnum 命中时散射的碎石片（×4，带重力）。
    /// </summary>
    public class DEBullet_ShrapnelShard : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityMod/Projectiles/Ranged/SlagBlast";

        private static readonly Color SlagOrange = new(210, 120, 30);

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 50;
            Projectile.tileCollide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.45f;   // 重力
            Projectile.rotation += 0.22f;

            Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Sand,
                -Projectile.velocity * 0.3f, 100, SlagOrange, 0.85f);
            dust.noGravity = true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<ArmorCrunch>(), 120);
        }
    }
}
