using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public class CosmicDischargeEndothermicSplit : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }

        public override void SetDefaults()
        {
            Projectile.width = 11;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 300;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.tileCollide = false;
            Projectile.coldDamage = true;
        }

        public override void AI()
        {
            Time++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 + MathHelper.Pi;
            Lighting.AddLight(Projectile.Center, Color.LightSkyBlue.ToVector3() * 0.49f);

            if (Time >= 10f)
            {
                NPC target = FindTarget(900f);
                if (target != null)
                {
                    Vector2 desiredVelocity = Projectile.SafeDirectionTo(target.Center) * 15f;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.08f);
                }
                else
                {
                    Projectile.velocity *= 1.0125f;
                }
            }

            if (Projectile.numUpdates % 3 == 0)
            {
                Color outerSparkColor = new Color(173, 216, 230);
                float scaleBoost = MathHelper.Clamp(Time * 0.01f, 0f, 1.8f);
                float outerSparkScale = 1.2f + scaleBoost;
                SparkParticle spark = new(Projectile.Center, Projectile.velocity, false, 7, outerSparkScale, outerSparkColor);
                GeneralParticleHandler.SpawnParticle(spark);
            }

            if (Main.rand.NextBool(4))
            {
                Dust iceDust = Dust.NewDustPerfect(Projectile.Center, DustID.SnowflakeIce, Projectile.velocity * 0.5f, 150, Color.LightBlue, 1.2f);
                iceDust.noGravity = true;
                iceDust.velocity *= 0.3f;
                iceDust.fadeIn = 1.5f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn, 300);
            target.AddBuff(BuffID.Chilled, 300);
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
