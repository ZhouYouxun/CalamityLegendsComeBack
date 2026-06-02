using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFEvilT2_Flame : ModProjectile, ILocalizedModType
    {
        private const int SegmentCount = 9;
        private const float SegmentSpacing = 12f;

        private struct WormSegment
        {
            internal Vector2 Position;
            internal float Rotation;
        }

        private readonly WormSegment[] segments = new WormSegment[SegmentCount];
        private bool initialized;

        private ref float Timer => ref Projectile.localAI[0];
        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92));

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 16;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 160;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }

        public override void AI()
        {
            Timer++;
            InitializeSegments();

            NPC target = Timer > 8f ? FindTarget(720f) : null;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float speed = Projectile.velocity.Length();
            float rushPhase = Timer % 58f;
            float rushPower = rushPhase < 36f ? Utils.GetLerpValue(0f, 36f, rushPhase, true) : Utils.GetLerpValue(58f, 36f, rushPhase, true);

            if (target != null)
            {
                Vector2 desiredDirection = Projectile.SafeDirectionTo(target.Center, direction);
                float maxTurn = MathHelper.Lerp(MathHelper.ToRadians(2.2f), MathHelper.ToRadians(10.5f), rushPower);
                direction = direction.ToRotation().AngleTowards(desiredDirection.ToRotation(), maxTurn).ToRotationVector2();
                speed = MathHelper.Lerp(speed, MathHelper.Lerp(7.5f, 18.2f, rushPower), 0.08f);
            }
            else
            {
                direction = direction.RotatedBy(Math.Sin(Timer * 0.11f + Projectile.ai[1]) * 0.035f);
                speed = MathHelper.Lerp(speed, 10.5f, 0.035f);
            }

            Projectile.velocity = direction * speed;
            Projectile.rotation = direction.ToRotation();
            Projectile.Opacity = Utils.GetLerpValue(0f, 14f, Timer, true) * Utils.GetLerpValue(0f, 24f, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * (0.45f + rushPower * 0.38f));

            UpdateSegments();
            EmitFlightEffects(direction, rushPower);
        }

        private void InitializeSegments()
        {
            if (initialized)
                return;

            initialized = true;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < segments.Length; i++)
            {
                segments[i].Position = Projectile.Center - direction * SegmentSpacing * (i + 1);
                segments[i].Rotation = Projectile.rotation;
            }
        }

        private void UpdateSegments()
        {
            Vector2 aheadPosition = Projectile.Center;
            float aheadRotation = Projectile.rotation;

            for (int i = 0; i < segments.Length; i++)
            {
                Vector2 offsetToDestination = aheadPosition - segments[i].Position;
                if (offsetToDestination == Vector2.Zero)
                    offsetToDestination = Projectile.velocity.SafeNormalize(Vector2.UnitX);

                if (aheadRotation != Projectile.rotation)
                {
                    float offsetAngle = MathHelper.WrapAngle(aheadRotation - Projectile.rotation) * 0.045f;
                    offsetToDestination = offsetToDestination.RotatedBy(offsetAngle);
                }

                segments[i].Rotation = offsetToDestination.ToRotation();
                segments[i].Position = aheadPosition - offsetToDestination.SafeNormalize(Vector2.UnitY) * SegmentSpacing;
                aheadPosition = segments[i].Position;
                aheadRotation = segments[i].Rotation;
            }
        }

        private NPC FindTarget(float range)
        {
            NPC closest = null;
            float bestDistance = range;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                closest = npc;
            }

            return closest;
        }

        private void EmitFlightEffects(Vector2 direction, float rushPower)
        {
            if (Main.dedServ)
                return;

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - direction * Main.rand.NextFloat(4f, 18f) + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.YellowTorch,
                    -direction * Main.rand.NextFloat(0.5f, 1.8f) + Main.rand.NextVector2Circular(0.25f, 0.25f),
                    30,
                    Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.04f, 0.24f)),
                    Main.rand.NextFloat(0.6f, 1.05f));
                dust.noGravity = true;
            }

            if ((int)Timer % 4 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    -direction * Main.rand.NextFloat(0.4f, 1.3f),
                    false,
                    Main.rand.Next(9, 15),
                    Main.rand.NextFloat(0.18f, 0.32f) * (1f + rushPower * 0.4f),
                    Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.08f, 0.32f)),
                    true,
                    false,
                    true));
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (CalamityUtils.CircularHitboxCollision(Projectile.Center, 18f, targetHitbox))
                return true;

            for (int i = 0; i < segments.Length; i++)
            {
                if (CalamityUtils.CircularHitboxCollision(segments[i].Position, 12f, targetHitbox))
                    return true;
            }

            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<BrainRot>(), 360);
            SpawnImpactEffects(target.Center);

            if (Projectile.numHits >= 5)
                Projectile.timeLeft = Math.Min(Projectile.timeLeft, 20);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.numHits > 0)
                Projectile.damage = Math.Max(1, (int)(Projectile.damage * 0.88f));
        }

        public override void OnKill(int timeLeft) => SpawnImpactEffects(Projectile.Center);

        private void SpawnImpactEffects(Vector2 center)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 14; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.5f, 5.6f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    center + Main.rand.NextVector2Circular(8f, 8f),
                    velocity,
                    false,
                    Main.rand.Next(12, 22),
                    Main.rand.NextFloat(0.28f, 0.62f),
                    Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.08f, 0.36f)),
                    true,
                    false,
                    true));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D line = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineSoftEdge").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Color theme = (Color.Lerp(ThemeColor, Color.White, 0.12f) with { A = 0 }) * Projectile.Opacity;
            Color inner = (Color.White with { A = 0 }) * Projectile.Opacity;

            PFLeftEffectRules.BeginAdditive();

            Vector2 previous = Projectile.Center;
            for (int i = 0; i < segments.Length; i++)
            {
                Vector2 current = segments[i].Position;
                Vector2 between = previous - current;
                float length = between.Length();
                if (length > 0.1f)
                {
                    Vector2 mid = (previous + current) * 0.5f - Main.screenPosition;
                    Main.EntitySpriteDraw(
                        line,
                        mid,
                        null,
                        theme * MathHelper.Lerp(0.48f, 0.18f, i / (float)segments.Length),
                        between.ToRotation() + MathHelper.PiOver2,
                        line.Size() * 0.5f,
                        new Vector2(0.16f, length / line.Height),
                        SpriteEffects.None,
                        0f);
                }

                float completion = i / (float)(segments.Length - 1);
                float scale = MathHelper.Lerp(0.16f, 0.08f, completion);
                Main.EntitySpriteDraw(
                    bloom,
                    current - Main.screenPosition,
                    null,
                    Color.Lerp(theme, inner, 0.28f) * (0.76f - completion * 0.26f),
                    segments[i].Rotation,
                    bloom.Size() * 0.5f,
                    scale,
                    SpriteEffects.None,
                    0f);

                previous = current;
            }

            Vector2 head = Projectile.Center - Main.screenPosition;
            float pulse = 0.9f + (float)Math.Sin(Timer * 0.2f) * 0.1f;
            Main.EntitySpriteDraw(bloom, head, null, theme * 0.92f, Projectile.rotation, bloom.Size() * 0.5f, 0.2f * pulse, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(bloom, head, null, inner * 0.34f, Projectile.rotation, bloom.Size() * 0.5f, 0.095f * pulse, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(star, head, null, theme * 0.58f, Projectile.rotation, star.Size() * 0.5f, new Vector2(0.1f, 0.65f) * pulse, SpriteEffects.None, 0f);

            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
}
