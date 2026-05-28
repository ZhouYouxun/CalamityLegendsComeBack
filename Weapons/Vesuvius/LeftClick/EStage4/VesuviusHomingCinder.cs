using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.EStage4
{
    public class VesuviusHomingCinder : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/Magic/RancorSmallCinder";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 3;
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (Projectile.localAI[1] == 0f)
            {
                Projectile.frame = Main.rand.Next(Main.projFrames[Type]);
                Projectile.localAI[1] = 1f;
            }

            Projectile.localAI[0]++;
            NPC target = FindTarget(760f);
            if (target != null && Projectile.localAI[0] > 10f)
            {
                Vector2 desiredVelocity = Projectile.SafeDirectionTo(target.Center + target.velocity * 8f) * 15.5f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.075f);
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, 0.65f, 0.18f, 0.04f);

            if (!Main.dedServ)
            {
                if (Main.rand.NextBool(2))
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f), DustID.Torch, -Projectile.velocity * Main.rand.NextFloat(0.08f, 0.18f), 100, Color.OrangeRed, Main.rand.NextFloat(0.8f, 1.35f));
                    dust.noGravity = true;
                }

                if (Main.rand.NextBool(4))
                {
                    Particle smoke = new TimedSmokeParticle(
                        Projectile.Center,
                        -Projectile.velocity * Main.rand.NextFloat(0.05f, 0.12f) + Main.rand.NextVector2Circular(0.6f, 0.6f),
                        Color.DimGray,
                        Color.Transparent,
                        Main.rand.NextFloat(0.45f, 0.82f),
                        0.64f,
                        Main.rand.Next(28, 44));
                    GeneralParticleHandler.SpawnParticle(smoke);
                }
            }
        }

        private NPC FindTarget(float range)
        {
            NPC bestTarget = null;
            float bestDistance = range;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Vector2.Distance(Projectile.Center, npc.Center);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = npc;
                }
            }

            return bestTarget;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 210);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], Color.OrangeRed * 0.7f);
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.White, Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
