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

                // The longer the lock lasts, the harder it turns and the faster it flies — a cinder catching fire
                float trackTime = Utils.GetLerpValue(10f, 90f, Projectile.localAI[0], true);
                float maxSpeed = MathHelper.Lerp(15.5f, 24f, trackTime);
                float homingStr = MathHelper.Lerp(0.045f, 0.095f, trackTime) + Main.rand.NextFloat(-0.02f, 0.03f);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, homingStr);

                // Occasional lateral veer — cinder catches a thermal and spirals
                if (Main.rand.NextBool(14))
                    Projectile.velocity += Projectile.velocity.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(0.06f, 0.24f);

                float speed = Projectile.velocity.Length();
                if (speed > maxSpeed) Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * maxSpeed;
                if (speed < 5f)  Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * 5f;
            }
            else if (target == null && Projectile.localAI[0] > 10f)
            {
                // No target — drift with random thermal buffeting
                Projectile.velocity += Main.rand.NextVector2Circular(0.45f, 0.45f);
                Projectile.velocity *= 0.97f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, 0.78f, 0.24f, 0.06f);

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
                    Main.rand.NextFloat(0.45f, 0.9f),
                    Main.rand.Next(100, 140),
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
                        Main.rand.NextFloat(0.55f, 1.05f),
                        Main.rand.Next(110, 150),
                        Main.rand.NextFloat(-0.06f, 0.06f)));
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null,
                VesuviusProjectileVisuals.AdditiveColor(VesuviusProjectileVisuals.LavaOrange) * 0.55f,
                0f, bloom.Size() * 0.5f, Projectile.scale * 0.5f, SpriteEffects.None);

            // Afterimages follow their own recorded rotation so the trail actually bends with
            // the cinder's homing arc instead of every ghost snapping to the current angle.
            for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                Vector2 oldCenter = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                float t = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;
                Color trailC = VesuviusProjectileVisuals.AdditiveColor(VesuviusProjectileVisuals.LavaOrange) * (t * 0.34f);
                Main.EntitySpriteDraw(texture, oldCenter - Main.screenPosition, frame,
                    trailC, Projectile.oldRot[i], frame.Size() * 0.5f,
                    Projectile.scale * MathHelper.Lerp(0.55f, 1f, t), SpriteEffects.None);
            }
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            // Calamity's RancorSmallCinder draws its body opaque white. Keeping that here is
            // what gives the cinder an actual burning core rather than a translucent haze.
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame,
                Color.White * Projectile.Opacity,
                Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
