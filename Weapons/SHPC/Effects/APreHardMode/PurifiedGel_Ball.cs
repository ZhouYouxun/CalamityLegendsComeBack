using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Terraria.Audio;
using Terraria.DataStructures;
using System;
using CalamityLegendsComeBack.Weapons.SHPC.RightClick;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.APreHardMode
{
    public class PurifiedGel_Ball : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.SHPC";

        private const int PhaseStraightFlight = 0;
        private const int PhaseSlowdown = 1;
        private const int PhaseHoming = 2;
        private const int StraightFlightPenetrate = 3;
        private const int HomingPenetrate = 1;
        private const int StraightFlightFrames = 40;
        private const int SlowdownFrames = 12;
        private const float SlowdownFactor = 0.98f;
        private const float BounceSpeedRetention = 1f;
        private int timer;
        private const float HomingRange = 1280f;
        private const float MinHomingSpeed = 10.5f;
        private const float MaxHomingSpeed = 19.5f;
        private const float HomingInertia = 16f;
        private const float NoTargetDamping = 0.99f;
        private const float WanderingTurnStrength = 0.006f;
        private static readonly Color PurifiedGelPink = new(255, 140, 200);
        private static readonly Color PurifiedGelBlue = new(120, 200, 255);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 8;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.penetrate = StraightFlightPenetrate;
            Projectile.timeLeft = 380;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.ai[0] = PhaseStraightFlight;
            Projectile.ai[1] = 0f;
            Projectile.penetrate = StraightFlightPenetrate;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = tex.Height / Main.projFrames[Type];
            Rectangle frame = new Rectangle(0, frameHeight * Projectile.frame, tex.Width, frameHeight);
            Vector2 frameOrigin = new Vector2(tex.Width * 0.5f, frameHeight * 0.5f);
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            // Bloom光圈（A=0 → 加法混合）
            // 拖尾
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float interp = (float)Math.Cos(Projectile.timeLeft / 32f + Main.GlobalTimeWrappedHourly / 20f + i / (float)Projectile.oldPos.Length * MathHelper.Pi) * 0.5f + 0.5f;
                Color trailColor = Color.Lerp(PurifiedGelPink, PurifiedGelBlue, interp) * 0.45f;
                trailColor.A = 0;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float intensity = MathHelper.Lerp(0.2f, 1f, 1f - i / (float)Projectile.oldPos.Length);
                //Main.EntitySpriteDraw(tex, pos, frame, trailColor, Projectile.rotation, frameOrigin, 1.8f * intensity * 0.6f, SpriteEffects.None, 0);
                //Main.EntitySpriteDraw(tex, pos, frame, trailColor * 0.5f, Projectile.rotation, frameOrigin, 1.8f * intensity * 0.6f * 0.7f, SpriteEffects.None, 0);
            }

            // 本体内光晕
            Color coreGlow = Color.Lerp(PurifiedGelPink, PurifiedGelBlue, 0.5f);
            coreGlow.A = 0;
            Main.EntitySpriteDraw(tex, drawPos, frame, coreGlow * 0.3f, Projectile.rotation, frameOrigin, Projectile.scale * 1.5f, SpriteEffects.None, 0);

            // 本体
            Main.EntitySpriteDraw(tex, drawPos, frame, lightColor, Projectile.rotation, frameOrigin, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }

        public override void AI()
        {
            timer++;
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 3 % Main.projFrames[Type];
            Projectile.rotation += 0.22f;
            UpdateMovementPhase();

            Color pink = PurifiedGelPink;
            Color blue = PurifiedGelBlue;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = forward.RotatedBy(MathHelper.Pi / 2f);

            // 双螺旋尘埃（削弱：每3帧只生成1个）
            if (Main.rand.NextBool(3))
            {
                float sine = (float)Math.Sin(timer * 0.45f);
                float squeeze = (float)Math.Cos(timer * 0.7f) * 0.6f;
                Vector2 offset = normal * sine * (10f + squeeze * 4f);
                int side = Main.rand.NextBool() ? 1 : -1;
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + offset * side,
                    ModContent.DustType<SquashDust>(),
                    -Projectile.velocity * Main.rand.NextFloat(0.3f, 0.8f) * 0.5f);
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(1.4f, 1.9f);
                dust.color = Color.Lerp(pink, blue, Main.rand.NextFloat());
                dust.fadeIn = 1.4f;
            }

            // 示波器宝石尘（削弱：隔帧）
            if (timer % 2 == 0)
            {
                float wave = MathF.Sin(timer * 0.35f) * 5.5f;
                Dust d = Dust.NewDustPerfect(Projectile.Center + normal * wave, DustID.GemDiamond, Vector2.Zero, 100, pink, 0.8f);
                d.noGravity = true;
            }

            // 中轴能量火花（削弱：缩小尺寸范围）
            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new PointParticle(
                    Projectile.Center,
                    -Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.6f, 2.0f) * 0.5f,
                    false, 10,
                    Main.rand.NextFloat(0.5f, 0.85f),
                    Main.rand.NextBool() ? pink : blue));
            }

            // 随机光点（削弱：25%概率，小尺寸）
            if (Main.rand.NextBool(4))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(12f, 12f),
                    DustID.GemDiamond,
                    -Projectile.velocity * 0.15f * 0.5f);
                d.noGravity = true;
                d.scale = Main.rand.NextFloat(0.35f, 0.6f);
                d.color = Color.Lerp(pink, blue, Main.rand.NextFloat());
            }

            Lighting.AddLight(Projectile.Center, Color.Lerp(pink, blue, 0.5f).ToVector3() * 0.25f);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if ((int)Projectile.ai[0] != PhaseStraightFlight)
                return false;

            Vector2 reflected = Projectile.velocity;
            if (Projectile.velocity.X != oldVelocity.X) reflected.X = -oldVelocity.X;
            if (Projectile.velocity.Y != oldVelocity.Y) reflected.Y = -oldVelocity.Y;
            if (reflected == Vector2.Zero) reflected = -oldVelocity;

            Projectile.velocity = reflected * BounceSpeedRetention;
            SpawnBounceExplosion(Projectile.Center);
            EnterSlowdownPhase();
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if ((int)Projectile.ai[0] != PhaseStraightFlight)
                return;

            Vector2 bounceDirection = (Projectile.Center - target.Center).SafeNormalize(-Projectile.velocity.SafeNormalize(Vector2.UnitX));
            float speed = Math.Max(Projectile.velocity.Length(), 0.1f);
            Projectile.velocity = bounceDirection * speed * BounceSpeedRetention;
            SpawnBounceExplosion(Projectile.Center);
            EnterSlowdownPhase();
        }

        private void UpdateMovementPhase()
        {
            int phase = (int)Projectile.ai[0];

            if (Projectile.numUpdates != 0)
            {
                if (phase == PhaseHoming)
                    HomeTowardTarget();

                return;
            }

            Projectile.ai[1]++;

            if (phase == PhaseStraightFlight)
            {
                if (Projectile.ai[1] >= StraightFlightFrames)
                    EnterSlowdownPhase();

                return;
            }

            if (phase == PhaseSlowdown)
            {
                Projectile.velocity *= SlowdownFactor;
                if (Projectile.ai[1] >= SlowdownFrames)
                    EnterHomingPhase();

                return;
            }

            HomeTowardTarget();
        }

        private void EnterSlowdownPhase()
        {
            Projectile.ai[0] = PhaseSlowdown;
            Projectile.ai[1] = 0f;
            Projectile.tileCollide = false;
            Projectile.netUpdate = true;
        }

        private void EnterHomingPhase()
        {
            Projectile.ai[0] = PhaseHoming;
            Projectile.ai[1] = 0f;
            Projectile.penetrate = HomingPenetrate;
            Projectile.tileCollide = false;
            Projectile.netUpdate = true;
        }

        private void HomeTowardTarget()
        {
            NPC target = FindNearestTarget(HomingRange);
            if (target == null)
            {
                FreeDrift(NoTargetDamping);
                return;
            }

            Vector2 currentVelocity = Projectile.velocity;
            float currentSpeed = currentVelocity.Length();
            if (currentSpeed < 0.1f)
                currentVelocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * 4f;

            Vector2 desiredDirection = (target.Center - Projectile.Center).SafeNormalize(currentVelocity.SafeNormalize(Vector2.UnitX));
            float warmup = Utils.GetLerpValue(0f, 36f, Projectile.ai[1], true);
            float closePressure = Utils.GetLerpValue(360f, 70f, Projectile.Distance(target.Center), true);
            float pullStrength = MathHelper.Lerp(0.35f, 1f, MathHelper.Max(warmup, closePressure * 0.75f));
            float targetSpeed = MathHelper.Lerp(MinHomingSpeed, MaxHomingSpeed, pullStrength);
            Vector2 desiredVelocity = desiredDirection * targetSpeed;

            Projectile.velocity = (currentVelocity * HomingInertia + desiredVelocity) / (HomingInertia + 1f);

            float sideSway = MathF.Sin((timer + Projectile.identity * 7f) * 0.075f) *
                MathHelper.Lerp(0.012f, 0.004f, pullStrength);
            Projectile.velocity = Projectile.velocity.RotatedBy(sideSway);

            if (Projectile.velocity.Length() > MaxHomingSpeed)
                Projectile.velocity = Projectile.velocity.SafeNormalize(desiredDirection) * MaxHomingSpeed;
        }

        private void FreeDrift(float damping)
        {
            float wander = MathF.Sin((timer + Projectile.identity * 5f) * 0.08f) * WanderingTurnStrength;
            Projectile.velocity = Projectile.velocity.RotatedBy(wander) * damping;
        }

        private NPC FindNearestTarget(float maxDistance)
        {
            NPC closestTarget = null;
            float closestDistance = maxDistance;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closestTarget = npc;
            }

            return closestTarget;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.28f, Pitch = 0.55f }, Projectile.Center);

            if (!Main.dedServ)
                SpawnDeathVisual(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitY));
        }

        private void SpawnBounceExplosion(Vector2 center)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                // 伤害冲击波：75×75范围
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    center, Vector2.Zero,
                    ModContent.ProjectileType<SHPCRight_Explosion>(),
                    (int)(Projectile.damage * 0.5), Projectile.knockBack, Projectile.owner,
                    -1f, 75f);
            }

            SoundEngine.PlaySound(SoundID.Item110 with { Volume = 0.65f, Pitch = 0.35f }, center);

            if (!Main.dedServ)
                SpawnBounceVisual(center);
        }

        private static void SpawnBounceVisual(Vector2 center)
        {
            // Bloom环扩散
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center, Vector2.Zero,
                new Color(255, 140, 200),
                "CalamityMod/Particles/BloomRing",
                Vector2.One, 0f, 0.055f, 0.24f, 14));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center, Vector2.Zero,
                new Color(120, 200, 255),
                "CalamityMod/Particles/BloomCircle",
                Vector2.One, 0f, 0.075f, 0.16f, 11));

            // 宝石尘爆散
            for (int i = 0; i < 14; i++)
            {
                Vector2 dir = Main.rand.NextVector2CircularEdge(1f, 1f);
                Color color = Color.Lerp(PurifiedGelPink, PurifiedGelBlue, Main.rand.NextFloat());
                Dust dust = Dust.NewDustPerfect(
                    center + dir * Main.rand.NextFloat(3f, 18f),
                    DustID.GemDiamond,
                    dir * Main.rand.NextFloat(2f, 6.5f) * 0.5f,
                    80, color, Main.rand.NextFloat(0.9f, 1.5f));
                dust.noGravity = true;
            }

            // 能量火花
            for (int i = 0; i < 4; i++)
            {
                GeneralParticleHandler.SpawnParticle(new PointParticle(
                    center,
                    Main.rand.NextVector2Circular(5.5f, 5.5f) * 0.5f,
                    false, 10,
                    Main.rand.NextFloat(0.5f, 1.1f),
                    Main.rand.NextBool() ? PurifiedGelPink : PurifiedGelBlue));
            }

            Lighting.AddLight(center, Color.Lerp(PurifiedGelPink, PurifiedGelBlue, 0.5f).ToVector3() * 0.9f);
        }

        private static void SpawnDeathVisual(Vector2 center, Vector2 forward)
        {
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center, Vector2.Zero,
                Color.Lerp(PurifiedGelPink, PurifiedGelBlue, 0.45f),
                "CalamityMod/Particles/BloomCircle",
                Vector2.One, 0f, 0.038f, 0.095f, 10));

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center, Vector2.Zero,
                new Color(255, 140, 200),
                "CalamityMod/Particles/BloomRing",
                Vector2.One, 0f, 0.026f, 0.12f, 12));

            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            for (int i = 0; i < 8; i++)
            {
                float spread = Main.rand.NextFloat(-0.9f, 0.9f);
                Vector2 velocity = (forward.RotatedBy(spread) * Main.rand.NextFloat(1.1f, 3.2f) + side * Main.rand.NextFloat(-0.7f, 0.7f)) * 0.45f;
                Color color = Color.Lerp(PurifiedGelPink, PurifiedGelBlue, Main.rand.NextFloat());
                Dust dust = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(5f, 5f),
                    DustID.GemDiamond,
                    velocity,
                    100, color, Main.rand.NextFloat(0.55f, 0.95f));
                dust.noGravity = true;
            }

            for (int i = 0; i < 3; i++)
            {
                GeneralParticleHandler.SpawnParticle(new PointParticle(
                    center + Main.rand.NextVector2Circular(4f, 4f),
                    Main.rand.NextVector2Circular(2.2f, 2.2f) * 0.35f,
                    false, 9,
                    Main.rand.NextFloat(0.38f, 0.72f),
                    Main.rand.NextBool() ? PurifiedGelPink : PurifiedGelBlue));
            }

            Lighting.AddLight(center, Color.Lerp(PurifiedGelPink, PurifiedGelBlue, 0.5f).ToVector3() * 0.35f);
        }
    }
}
