using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.SubProjectiles
{
    /// <summary>
    /// Fungicide 命中时爆裂生成的追踪蓝色孢子（×3）。
    /// </summary>
    public class DEBullet_FungiSpore : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityMod/Projectiles/Ranged/FungiOrb";

        private static readonly Color SporeBlue = new(40, 210, 255);
        private static readonly Color SporeGlow = new(10, 180, 240);

        private NPC HomeTarget
        {
            get
            {
                int idx = (int)Projectile.ai[0];
                return idx >= 0 && idx < Main.maxNPCs && Main.npc[idx].active ? Main.npc[idx] : null;
            }
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.light = 0.4f;
            Projectile.tileCollide = true;
        }

        public override void AI()
        {
            // 归巢至 ai[0] 指定的 NPC（若还存在）
            NPC target = HomeTarget;
            if (target != null)
            {
                float dist = Projectile.Distance(target.Center);
                if (dist > 30f)
                {
                    float turnSpeed = MathHelper.Lerp(0.06f, 0.18f, 1f - dist / 300f);
                    Vector2 desired = Projectile.DirectionTo(target.Center) * Projectile.velocity.Length();
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, turnSpeed);
                }
            }

            Projectile.rotation += 0.2f * Projectile.direction;

            // 蓝色尾迹 dust
            Dust dust = Dust.NewDustPerfect(Projectile.Center,
                DustID.BlueFairy, -Projectile.velocity * 0.4f, 100, SporeBlue, 0.8f);
            dust.noGravity = true;

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center, -Projectile.velocity * 0.15f + Main.rand.NextVector2Circular(1.5f, 1.5f),
                    false, 8, 0.018f, SporeGlow, new Vector2(0.8f, 0.8f)));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Venom, 240);
            SpawnSporeImpact(Projectile.Center);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SpawnSporeImpact(Projectile.Center);
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            SpawnSporeImpact(Projectile.Center);
        }

        private static void SpawnSporeImpact(Vector2 pos)
        {
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(pos, Vector2.Zero, SporeBlue, 0.6f, 18));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(pos, Vector2.Zero, SporeBlue * 0.8f,
                    "CalamityMod/Particles/HighResHollowCircleHardEdge", Vector2.One, 0f, 0f, 0.05f, 16, true, 0.8f));
            }
            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(pos, DustID.BlueFairy,
                    Main.rand.NextVector2Circular(5f, 5f), 100, SporeBlue, 1.1f);
                dust.noGravity = true;
            }
        }
    }
}
