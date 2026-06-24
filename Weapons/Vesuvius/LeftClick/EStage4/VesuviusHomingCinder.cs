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

                // Erratic homing — variable strength creates spiral / overshoot approach
                float homingStr = 0.048f + Main.rand.NextFloat(-0.028f, 0.05f);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, homingStr);

                // Occasional lateral veer — cinder catches a thermal and spirals
                if (Main.rand.NextBool(14))
                    Projectile.velocity += Projectile.velocity.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(0.06f, 0.24f);

                float speed = Projectile.velocity.Length();
                if (speed > 19f) Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * 19f;
                if (speed < 5f)  Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * 5f;
            }
            else if (target == null && Projectile.localAI[0] > 10f)
            {
                // No target — drift with random thermal buffeting
                Projectile.velocity += Main.rand.NextVector2Circular(0.45f, 0.45f);
                Projectile.velocity *= 0.97f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, 0.78f * VesuviusProjectileVisuals.VisualIntensity, 0.24f * VesuviusProjectileVisuals.VisualIntensity, 0.06f * VesuviusProjectileVisuals.VisualIntensity);

            SpawnSmokeAndDustTrail();
        }

        private void SpawnSmokeAndDustTrail()
        {
            if (Main.dedServ)
                return;

            Vector2 backward = -Projectile.velocity.SafeNormalize(Vector2.UnitY);
            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new SmallSmokeParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    backward.RotatedByRandom(0.32f) * Main.rand.NextFloat(0.3f, 1.6f),
                    Color.Lerp(VesuviusProjectileVisuals.ScoriaSmoke, VesuviusProjectileVisuals.LavaOrange, 0.16f),
                    Color.Black,
                    Main.rand.NextFloat(0.34f, 0.76f),
                    0.52f,
                    Main.rand.NextFloat(-0.06f, 0.06f)));
            }

            Dust dust = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                Main.rand.NextBool(3) ? DustID.Smoke : DustID.Torch,
                backward.RotatedByRandom(0.34f) * Main.rand.NextFloat(0.5f, 2.8f),
                110,
                Main.rand.NextBool(4) ? VesuviusProjectileVisuals.LavaGold : VesuviusProjectileVisuals.AshGray,
                Main.rand.NextFloat(0.55f, 1.05f));
            dust.noGravity = Main.rand.NextBool(3);
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

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 14; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.2f, 5.2f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool(2) ? DustID.Smoke : DustID.Torch,
                    velocity,
                    120,
                    Main.rand.NextBool(3) ? VesuviusProjectileVisuals.LavaGold : VesuviusProjectileVisuals.AshGray,
                    Main.rand.NextFloat(0.65f, 1.2f));
                dust.noGravity = Main.rand.NextBool(3);

                if (i % 2 == 0)
                {
                    GeneralParticleHandler.SpawnParticle(new SmallSmokeParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                        velocity * 0.28f,
                        Color.Lerp(VesuviusProjectileVisuals.ScoriaSmoke, VesuviusProjectileVisuals.LavaOrange, 0.12f),
                        Color.Black,
                        Main.rand.NextFloat(0.42f, 0.88f),
                        0.58f,
                        Main.rand.NextFloat(-0.06f, 0.06f)));
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);

            // Glowing afterimage trail
            for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
            {
                Vector2 oldCenter = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                float t = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;
                Color trailC = VesuviusProjectileVisuals.LavaOrange * (t * 0.32f * VesuviusProjectileVisuals.VisualIntensity);
                Main.EntitySpriteDraw(texture, oldCenter - Main.screenPosition, frame,
                    trailC, Projectile.rotation, frame.Size() * 0.5f,
                    Projectile.scale * MathHelper.Lerp(0.55f, 1f, t), SpriteEffects.None);
            }

            // Main cinder sprite
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame,
                Color.Lerp(lightColor, VesuviusProjectileVisuals.LavaGold, 0.55f),
                Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, SpriteEffects.None);

            return false;
        }
    }
}
