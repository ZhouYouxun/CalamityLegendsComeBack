using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    // A long-lived tracking bubble. It drifts until it has a target, then keeps accelerating until contact.
    internal sealed class SeasSearingPressureDepthCharge : ModProjectile, ILocalizedModType
    {
        private const int ArmFrames = 22;
        private const int ChaseRampFrames = 210;
        private const float HomingRange = 2200f;
        private bool detonated;

        private float DriftDirection => Projectile.ai[0];
        private float Age => Projectile.localAI[0];

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 720;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.ArmorPenetration = 20;
        }

        public override bool? CanDamage() => detonated ? null : false;

        public override void AI()
        {
            if (detonated)
            {
                Projectile.velocity = Vector2.Zero;
                return;
            }

            Projectile.localAI[0]++;
            NPC target = FindTarget();
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            if (Age < ArmFrames)
                Projectile.velocity *= 0.92f;
            else if (target != null)
            {
                float chaseProgress = Utils.GetLerpValue(ArmFrames, ArmFrames + ChaseRampFrames, Age, true);
                float chaseSpeed = MathHelper.Lerp(8.5f, 27f, chaseProgress);
                float prediction = MathHelper.Lerp(16f, 3f, chaseProgress);
                Vector2 aimPoint = target.Center + target.velocity * prediction;
                Vector2 desiredVelocity = (aimPoint - Projectile.Center).SafeNormalize(direction) * chaseSpeed;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, MathHelper.Lerp(0.075f, 0.18f, chaseProgress));
                direction = Projectile.velocity.SafeNormalize(direction);
            }

            float drift = (float)Math.Sin(Age * 0.27f + DriftDirection * 2.8f) * 0.16f;
            Projectile.velocity += direction.RotatedBy(MathHelper.PiOver2) * drift;
            Lighting.AddLight(Projectile.Center, Color.Lerp(SeasSearingPalette.PressureBlue, SeasSearingPalette.BiohazardLime, Utils.GetLerpValue(ArmFrames, ArmFrames + ChaseRampFrames, Age, true)).ToVector3() * 0.45f);

            if (!Main.dedServ && (int)Age % 3 == 0)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(7f, 7f),
                    Main.rand.NextBool() ? DustID.GemEmerald : DustID.Water,
                    -direction * Main.rand.NextFloat(0.4f, 1.8f), 120,
                    Color.Lerp(SeasSearingPalette.PressureBlue, SeasSearingPalette.BiohazardLime, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.55f, 0.9f));
                dust.noGravity = true;
            }

            if (Age >= ArmFrames && target != null && Vector2.DistanceSquared(Projectile.Center, target.Center) <= 56f * 56f)
                Detonate();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, 10, 14 * 60);
            target.AddBuff(BuffID.Venom, 300);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 300);
        }

        private void Detonate()
        {
            if (detonated)
                return;

            detonated = true;
            Vector2 center = Projectile.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.width = Projectile.height = 150;
            Projectile.Center = center;
            Projectile.timeLeft = 3;
            Projectile.netUpdate = true;

            SpawnBubbleExplosion(center);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.58f, Pitch = -0.42f }, center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (detonated)
                return false;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Vector2 center = Projectile.Center - Main.screenPosition;
            Color core = Color.Lerp(SeasSearingPalette.PressureBlue, SeasSearingPalette.BiohazardLime, Utils.GetLerpValue(ArmFrames, ArmFrames + ChaseRampFrames, Age, true));
            core.A = 0;

            float pulse = 0.72f + 0.12f * (float)Math.Sin(Age * 0.35f);
            Main.EntitySpriteDraw(bloom, center, null, core * 0.72f, 0f, bloom.Size() * 0.5f, 0.19f * pulse, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(ring, center, null, core * 0.56f, -Main.GlobalTimeWrappedHourly * 2.4f, ring.Size() * 0.5f, 0.21f * pulse, SpriteEffects.None, 0);
            return false;
        }

        private void SpawnBubbleExplosion(Vector2 center)
        {
            if (Main.dedServ)
                return;

            Color outer = SeasSearingPalette.PressureBlue;
            Color inner = SeasSearingPalette.BiohazardLime;
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero,
                outer, Vector2.One, 0f, 0.08f, 1.15f, 18));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero,
                inner, Vector2.One, MathHelper.PiOver4, 0.04f, 0.72f, 14));

            const int bubbleCount = 18;
            for (int i = 0; i < bubbleCount; i++)
            {
                Vector2 radial = (MathHelper.TwoPi * i / bubbleCount + Main.rand.NextFloat(-0.16f, 0.16f)).ToRotationVector2();
                float speed = Main.rand.NextFloat(3.5f, 9.5f);
                float scale = Main.rand.NextFloat(0.28f, 0.62f);
                Color color = Color.Lerp(outer, inner, Main.rand.NextFloat());

                GeneralParticleHandler.SpawnParticle(new WaterFlavoredParticle(
                    center + radial * Main.rand.NextFloat(0f, 8f), radial * speed - Vector2.UnitY * Main.rand.NextFloat(0f, 1.6f),
                    true, Main.rand.Next(22, 38), scale, color));

                if (i % 2 == 0)
                {
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        center + radial * Main.rand.NextFloat(2f, 10f), radial * speed * 0.45f,
                        false, Main.rand.Next(16, 28), scale * 0.42f, color, true, false, true));
                }
            }
        }

        private NPC FindTarget()
        {
            NPC best = null;
            float bestDistance = HomingRange * HomingRange;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;

                float distance = Vector2.DistanceSquared(Projectile.Center, npc.Center);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = npc;
                }
            }
            return best;
        }
    }
}
