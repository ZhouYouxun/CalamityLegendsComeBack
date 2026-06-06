using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public class CosmicDischargeHookLimb : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Projectiles/Summon/EndoCooperLimbs_Glow";

        private ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 5;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 90;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.coldDamage = true;
        }

        public override void AI()
        {
            Time++;
            Projectile.rotation += 0.28f * Projectile.direction;

            if (Time >= 8f)
            {
                NPC target = FindTarget(600f);
                if (target != null)
                {
                    Vector2 desiredVelocity = Projectile.SafeDirectionTo(target.Center) * 13f;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.08f);
                }
            }

            Lighting.AddLight(Projectile.Center, CosmicDischargeCommon.FrostCoreColor.ToVector3() * 0.28f);

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool() ? 67 : 187,
                    Projectile.velocity.RotatedByRandom(0.55f) * Main.rand.NextFloat(0.15f, 0.65f),
                    120,
                    CosmicDischargeCommon.FrostGlowColor,
                    Main.rand.NextFloat(0.9f, 1.2f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Nightwither>(), 180);
            target.AddBuff(ModContent.BuffType<GlacialState>(), 120);
        }

        private NPC FindTarget(float maxDistance)
        {
            NPC result = null;
            float closest = maxDistance;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance < closest)
                {
                    closest = distance;
                    result = npc;
                }
            }

            return result;
        }
    }
}
