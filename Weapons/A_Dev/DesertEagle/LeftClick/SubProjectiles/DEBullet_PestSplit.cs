using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.SubProjectiles
{
    /// <summary>
    /// PestilentDefiler 命中时分裂出的 3 颗瘟疫子弹（直飞，±40°/0°散射）。
    /// </summary>
    public class DEBullet_PestSplit : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityMod/Projectiles/Ranged/SlagBlast";

        private static readonly Color PestGreen = new(60, 220, 40);
        private static readonly Color PestDark = new(20, 140, 10);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 5;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 120;
            Projectile.extraUpdates = 2;
            Projectile.tileCollide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.light = 0.35f;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.TerraBlade,
                -Projectile.velocity * 0.3f, 100, PestGreen, 0.9f);
            dust.noGravity = true;

            if (!Main.dedServ && Main.rand.NextBool(5))
            {
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center, -Projectile.velocity * 0.12f,
                    false, 7, 0.014f, PestGreen, new Vector2(0.55f, 1.5f)));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Plague>(), 180);
        }
    }
}
