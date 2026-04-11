using System;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Passive
{
    public class SHPCPassiveOrb : ModProjectile, ILocalizedModType
    {
        private static readonly Color TechBlue = new(38, 170, 255);
        private static readonly Color TechBlueBright = new(160, 245, 255);
        private static readonly Color TechBlueDeep = new(28, 88, 200);

        public new string LocalizationCategory => "Projectiles.Misc";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public ref float time => ref Projectile.ai[0];
        public bool onSpawn = true;
        private Player targeted;
        private Player lastHitTarget;
        private int timesItCanHit = 1;
        public bool startAttackEffects = true;
        public int attackTime = 160;

        private Vector2 orbitAnchorCenter;
        private Vector2 orbitStartOffset;
        private float orbitAngle;
        private float orbitRadius;
        private float orbitRadiusTarget;
        private int orbitDirection;
        private bool orbitConfigured;

        private ref float FlightState => ref Projectile.localAI[0];
        private ref float FlightTimer => ref Projectile.localAI[1];

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 5;
            Projectile.timeLeft = 180;
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, TechBlue.ToVector3() * 0.5f);
            Player owner = Main.player[Projectile.owner];
            float targetDist = Vector2.Distance(owner.Center, Projectile.Center);

            //if (onSpawn)
            //{
            //    for (int i = 0; i <= 8; i++)
            //    {
            //        Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Electric);
            //        dust.scale = Main.rand.NextFloat(1.2f, 1.9f) * Projectile.scale;
            //        dust.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.5f) * Main.rand.NextFloat(5f, 7f);
            //        dust.noGravity = true;
            //        dust.color = Color.Lerp(TechBlueDeep, TechBlue, Main.rand.NextFloat());
            //        dust.fadeIn = 1f;
            //    }

            //    for (int i = 0; i <= 6; i++)
            //    {
            //        Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>());
            //        dust.scale = Main.rand.NextFloat(1.2f, 1.9f) * Projectile.scale;
            //        dust.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.3f) * Main.rand.NextFloat(7f, 9f);
            //        dust.noGravity = true;
            //        dust.color = Color.Lerp(TechBlue, TechBlueBright, Main.rand.NextFloat());
            //        dust.fadeIn = 0.3f;
            //    }

            //    onSpawn = false;
            //}

            if (time >= attackTime)
            {
                if (targeted == null)
                {
                    startAttackEffects = true;
                    targeted = owner;
                }

                Projectile.extraUpdates = 5;
                UpdateReturnFlight(targeted);

                if (startAttackEffects)
                {
                    SoundStyle pulse = new("CalamityMod/Sounds/Item/PulseSound");
                    SoundEngine.PlaySound(pulse with
                    {
                        Volume = 0.25f,
                        Pitch = Math.Max(0.6f, Main.rand.NextFloat(0.3f, 0.4f) + Projectile.numHits * 0.1f),
                        MaxInstances = 5
                    }, Projectile.Center);

                    for (int k = 0; k < 6; k++)
                    {
                        Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Electric);
                        dust.scale = Main.rand.NextFloat(1.2f, 1.9f) * Projectile.scale;
                        dust.velocity = Projectile.DirectionTo(targeted.Center).RotatedByRandom(0.5f) * Main.rand.NextFloat(5f, 9f);
                        dust.noGravity = true;
                        dust.color = Color.Lerp(TechBlue, TechBlueBright, Main.rand.NextFloat());
                        dust.fadeIn = 1f;
                    }
                    startAttackEffects = false;
                }
            }
            else
            {
                Projectile.velocity *= Projectile.numHits > 0 ? 0.955f : 0.97f;
            }

            float squash = Utils.GetLerpValue(1, 3, Projectile.velocity.Length(), true);
            if (targetDist < 1400f && squash > 0.2f)
            {
                Particle trail = new CustomSpark(
                    Projectile.Center,
                    Projectile.velocity * 0.01f,
                    "CalamityMod/Particles/DualTrail",
                    false,
                    13,
                    0.075f * Projectile.scale,
                    Color.Lerp(TechBlueDeep, TechBlueBright, 0.5f) * 0.6f * squash,
                    new Vector2(1 - 0.15f * squash, 1.5f),
                    true,
                    false,
                    shrinkSpeed: 0.2f * squash);
                GeneralParticleHandler.SpawnParticle(trail);
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            time++;
        }

        private void UpdateReturnFlight(Player owner)
        {
            FlightTimer++;

            if (FlightState == 0f)
            {
                Vector2 toOwner = owner.Center - Projectile.Center;
                float entryDistance = MathHelper.Clamp(toOwner.Length() * 0.45f, 54f, 110f);
                Vector2 entryDirection = toOwner.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.9f);
                Vector2 entryPoint = owner.Center + entryDirection * entryDistance;
                Vector2 desiredVelocity = (entryPoint - Projectile.Center).SafeNormalize(Vector2.UnitY) * 14f;

                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.18f);

                if (Vector2.Distance(Projectile.Center, entryPoint) < 26f || FlightTimer > 45f)
                {
                    ConfigureOrbit(owner);
                    FlightState = 1f;
                    FlightTimer = 0f;
                }
                return;
            }

            if (FlightState == 1f)
            {
                if (!orbitConfigured || Vector2.Distance(orbitAnchorCenter, owner.Center) > 28f)
                    ConfigureOrbit(owner);

                orbitAnchorCenter = owner.Center;
                orbitAngle += 0.15f * orbitDirection;

                if ((int)FlightTimer % 10 == 0)
                    orbitRadiusTarget = Main.rand.NextFloat(52f, 92f);

                orbitRadius = MathHelper.Lerp(orbitRadius, orbitRadiusTarget, 0.1f);

                Vector2 desiredPosition = orbitAnchorCenter + orbitAngle.ToRotationVector2() * orbitRadius;
                Vector2 desiredVelocity = (desiredPosition - Projectile.Center).SafeNormalize(Vector2.UnitY) * 13f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.22f);

                if (Math.Abs(orbitAngle) >= MathHelper.TwoPi)
                {
                    FlightState = 2f;
                    FlightTimer = 0f;
                }
                return;
            }

            Vector2 returnVelocity = (owner.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 15f;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, returnVelocity, 0.24f);

            if (Vector2.Distance(Projectile.Center, owner.Center) < 20f)
                Projectile.Kill();
        }

        private void ConfigureOrbit(Player owner)
        {
            orbitConfigured = true;
            orbitAnchorCenter = owner.Center;
            orbitStartOffset = Projectile.Center - owner.Center;

            if (orbitStartOffset.LengthSquared() < 16f)
                orbitStartOffset = Main.rand.NextVector2Unit() * Main.rand.NextFloat(56f, 84f);

            orbitAngle = orbitStartOffset.ToRotation();
            orbitRadius = MathHelper.Clamp(orbitStartOffset.Length(), 48f, 96f);
            orbitRadiusTarget = Main.rand.NextFloat(52f, 92f);
            orbitDirection = Main.rand.NextBool() ? 1 : -1;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Asset<Texture2D> orb = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");
            Vector2 squash = new Vector2(Utils.Remap(Projectile.velocity.Length(), 5, 10, 1, 0.6f), Utils.Remap(Projectile.velocity.Length(), 5, 10, 1, 2f));
            float timeleftFade = (float)Math.Pow(Utils.GetLerpValue(0, 40 * Projectile.extraUpdates, Projectile.timeLeft, true), 5);

            for (int i = 0; i < 6; i++)
            {
                Color orbColor = Color.Lerp(TechBlue, Color.White, i * 0.07f) with { A = 0 } * 0.5f;
                Vector2 scale = Projectile.scale * timeleftFade * squash * (0.05f + i * 0.01f) * 3;
                Main.EntitySpriteDraw(orb.Value, Projectile.Center - Main.screenPosition, null, orbColor, Projectile.rotation, orb.Size() * 0.5f, scale, SpriteEffects.None);
            }

            return false;
        }
    }
}
