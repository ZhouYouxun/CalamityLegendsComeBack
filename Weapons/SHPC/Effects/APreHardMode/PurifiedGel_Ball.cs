using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Terraria.Audio;
using System;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.APreHardMode
{
    public class PurifiedGel_Ball : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        // 自定义计时器
        private int timer;
        private const int MaxRicochets = 2;
        private const float BounceTargetRange = 1100f;
        private const float MinRicochetSpeed = 8f;
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
            Projectile.penetrate = MaxRicochets + 1;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteBatch spriteBatch = Main.spriteBatch;
            Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = tex.Height / Main.projFrames[Type];
            Rectangle frame = new Rectangle(0, frameHeight * Projectile.frame, tex.Width, frameHeight);
            Vector2 frameOrigin = new Vector2(tex.Width * 0.5f, frameHeight * 0.5f);

            // ===== Aerialite风格拖尾 =====
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float interp = (float)Math.Cos(Projectile.timeLeft / 32f + Main.GlobalTimeWrappedHourly / 20f + i / (float)Projectile.oldPos.Length * MathHelper.Pi) * 0.5f + 0.5f;

                Color color = Color.Lerp(PurifiedGelPink, PurifiedGelBlue, interp) * 0.45f;
                color.A = 0;

                Vector2 pos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;

                float intensity = MathHelper.Lerp(0.2f, 1f, 1f - i / (float)Projectile.oldPos.Length);

                Vector2 scaleOuter = new Vector2(1.8f) * intensity;
                Vector2 scaleInner = new Vector2(1.8f) * intensity * 0.7f;

                Main.EntitySpriteDraw(tex, pos, frame, color, Projectile.rotation, frameOrigin, scaleOuter * 0.6f, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(tex, pos, frame, color * 0.5f, Projectile.rotation, frameOrigin, scaleInner * 0.6f, SpriteEffects.None, 0);
            }

            // 本体
            Main.EntitySpriteDraw(tex,
                Projectile.Center - Main.screenPosition,
                frame,
                lightColor,
                Projectile.rotation,
                frameOrigin,
                Projectile.scale,
                SpriteEffects.None,
                0);

            return false;
        }

        public override void AI()
        {
            timer++;
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 3 % Main.projFrames[Type];

            Projectile.rotation += 0.22f;

            Color pink = PurifiedGelPink;
            Color blue = PurifiedGelBlue;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = forward.RotatedBy(MathHelper.Pi / 2f);

            // ===== 原有双螺旋（加强数学感）=====
            float sine = (float)Math.Sin(timer * 0.45f);
            float squeeze = (float)Math.Cos(timer * 0.7f) * 0.6f;

            Vector2 offset = normal * sine * (10f + squeeze * 4f);

            for (int i = 0; i < 2; i++)
            {
                Vector2 pos = Projectile.Center + (i == 0 ? offset : -offset);

                Dust dust = Dust.NewDustPerfect(
                    pos,
                    ModContent.DustType<SquashDust>(),
                    -Projectile.velocity * Main.rand.NextFloat(0.3f, 0.8f)
                );

                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(1.7f, 2.2f);
                dust.color = Color.Lerp(pink, blue, Main.rand.NextFloat());
                dust.fadeIn = 1.6f;
            }

            // ===== Auric示波器结构（改色版）=====
            int points = 2;
            float amplitude = 5.5f;
            float freq = 0.35f;

            for (int i = 0; i < points; i++)
            {
                float t = timer + i * 0.4f;

                float wave1 = MathF.Sin(t * freq) * amplitude;
                float wave2 = MathF.Sin(t * freq + MathHelper.Pi) * amplitude;

                Vector2 pos1 = Projectile.Center + normal * wave1 - forward * (i * 4f);
                Vector2 pos2 = Projectile.Center + normal * wave2 - forward * (i * 4f);

                Dust d1 = Dust.NewDustPerfect(pos1, DustID.GemDiamond, Vector2.Zero, 100, pink, 0.9f);
                d1.noGravity = true;

                Dust d2 = Dust.NewDustPerfect(pos2, DustID.GemDiamond, Vector2.Zero, 100, blue, 0.9f);
                d2.noGravity = true;
            }

            // ===== 中轴能量火花 =====
            if (Main.rand.NextBool(2))
            {
                PointParticle spark = new PointParticle(
                    Projectile.Center,
                    -Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.8f, 2.5f),
                    false,
                    12,
                    Main.rand.NextFloat(0.7f, 1.1f),
                    Main.rand.NextBool() ? pink : blue
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }

            // ===== 轻量光点 =====
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(14f, 14f),
                    DustID.GemDiamond,
                    -Projectile.velocity * 0.2f
                );
                d.noGravity = true;
                d.scale = Main.rand.NextFloat(0.5f, 0.8f);
                d.color = Color.Lerp(pink, blue, Main.rand.NextFloat());
            }

            // 光照
            Lighting.AddLight(Projectile.Center, Color.Lerp(pink, blue, 0.5f).ToVector3() * 0.45f);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Vector2 reflectedVelocity = Projectile.velocity;

            if (Projectile.velocity.X != oldVelocity.X)
                reflectedVelocity.X = -oldVelocity.X;
            if (Projectile.velocity.Y != oldVelocity.Y)
                reflectedVelocity.Y = -oldVelocity.Y;

            if (reflectedVelocity == Vector2.Zero)
                reflectedVelocity = -oldVelocity;

            return !TryRicochet(Projectile.Center, oldVelocity, -1, reflectedVelocity);
        }

        private bool TryRicochet(Vector2 bouncePoint, Vector2 incomingVelocity, int excludedTargetIndex, Vector2 fallbackVelocity)
        {
            if (Projectile.localAI[0] >= MaxRicochets)
                return false;

            Projectile.localAI[0]++;
            SpawnBounceExplosion(bouncePoint, excludedTargetIndex);

            float speed = Math.Max(MinRicochetSpeed, incomingVelocity.Length());
            NPC target = FindBounceTarget(bouncePoint, BounceTargetRange, excludedTargetIndex);
            Vector2 targetVelocity = fallbackVelocity;

            if (target != null)
                targetVelocity = target.Center - bouncePoint;

            Projectile.velocity = targetVelocity.SafeNormalize(incomingVelocity.SafeNormalize(Vector2.UnitX)) * speed;
            Projectile.netUpdate = true;
            return true;
        }

        private void SpawnBounceExplosion(Vector2 center, int excludedTargetIndex)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                int explosionIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    center,
                    Vector2.Zero,
                    ModContent.ProjectileType<PurifiedGel_BallExplosion>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    excludedTargetIndex);

                if (explosionIndex >= 0 && explosionIndex < Main.maxProjectiles)
                {
                    Projectile explosion = Main.projectile[explosionIndex];
                    explosion.Center = center;
                    explosion.netUpdate = true;
                }
            }

            SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.65f, Pitch = 0.35f }, center);
        }

        private NPC FindBounceTarget(Vector2 bouncePoint, float maxRange, int excludedTargetIndex)
        {
            NPC closestTarget = null;
            float closestDistanceSq = maxRange * maxRange;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.whoAmI == excludedTargetIndex || !npc.CanBeChasedBy(Projectile, false))
                    continue;

                float distanceSq = Vector2.DistanceSquared(bouncePoint, npc.Center);
                if (distanceSq < closestDistanceSq)
                {
                    closestDistanceSq = distanceSq;
                    closestTarget = npc;
                }
            }

            return closestTarget;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Vector2 bouncePoint = Projectile.Center;
            Vector2 awayFromTarget = bouncePoint - target.Center;
            Vector2 fallbackVelocity = awayFromTarget.SafeNormalize(-Projectile.velocity.SafeNormalize(Vector2.UnitX));

            TryRicochet(bouncePoint, Projectile.velocity, target.whoAmI, fallbackVelocity);
        }
    }

    public class PurifiedGel_BallExplosion : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int ExplosionHitboxSize = 64;
        private const float ExplosionRadius = ExplosionHitboxSize / 2f;
        private static readonly Color PurifiedGelPink = new(255, 140, 200);
        private static readonly Color PurifiedGelBlue = new(120, 200, 255);

        public override void SetDefaults()
        {
            Projectile.width = ExplosionHitboxSize;
            Projectile.height = ExplosionHitboxSize;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 3;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanHitNPC(NPC target)
        {
            int excludedTargetIndex = (int)Projectile.ai[0];
            if (target.whoAmI == excludedTargetIndex)
                return false;

            return null;
        }

        public override void AI()
        {
            if (Projectile.localAI[0]++ > 0f)
                return;

            if (Main.dedServ)
                return;

            SpawnClassicDustExplosion(Projectile.Center);
            Lighting.AddLight(Projectile.Center, Color.Lerp(PurifiedGelPink, PurifiedGelBlue, 0.5f).ToVector3() * 0.9f);
        }

        private static void SpawnClassicDustExplosion(Vector2 center)
        {
            for (int i = 0; i < 30; i++)
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                float distance = Main.rand.NextFloat(4f, ExplosionRadius);
                Vector2 velocity = direction * Main.rand.NextFloat(2.5f, 7.5f);
                Color color = Color.Lerp(PurifiedGelPink, PurifiedGelBlue, Main.rand.NextFloat());

                Dust dust = Dust.NewDustPerfect(
                    center + direction * distance,
                    DustID.GemDiamond,
                    velocity,
                    80,
                    color,
                    Main.rand.NextFloat(1.15f, 1.75f));

                dust.noGravity = true;
                dust.fadeIn = 0.8f;
            }

            for (int i = 0; i < 18; i++)
            {
                Vector2 direction = (MathHelper.TwoPi * i / 18f).ToRotationVector2();
                Color color = Color.Lerp(PurifiedGelPink, PurifiedGelBlue, i / 17f);

                Dust dust = Dust.NewDustPerfect(
                    center + direction * ExplosionRadius,
                    DustID.UnusedWhiteBluePurple,
                    direction * Main.rand.NextFloat(1.5f, 4.5f),
                    100,
                    color,
                    Main.rand.NextFloat(0.9f, 1.35f));

                dust.noGravity = true;
            }
        }
    }
}
