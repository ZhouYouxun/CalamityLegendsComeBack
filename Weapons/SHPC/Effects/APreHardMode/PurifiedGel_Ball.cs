using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Terraria.Audio;
using System;
using CalamityLegendsComeBack.Weapons.SHPC.RightClick;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.APreHardMode
{
    public class PurifiedGel_Ball : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.SHPC";

        private int timer;
        private const int MaxRicochets = 5;
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
            Projectile.penetrate = -1;
            Projectile.timeLeft = 380;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
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
            Vector2 reflected = Projectile.velocity;
            if (Projectile.velocity.X != oldVelocity.X) reflected.X = -oldVelocity.X;
            if (Projectile.velocity.Y != oldVelocity.Y) reflected.Y = -oldVelocity.Y;
            if (reflected == Vector2.Zero) reflected = -oldVelocity;
            return !TryRicochet(Projectile.Center, oldVelocity, reflected);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Vector2 away = (Projectile.Center - target.Center).SafeNormalize(-Projectile.velocity.SafeNormalize(Vector2.UnitX));
            TryRicochet(Projectile.Center, Projectile.velocity, away);
        }

        private bool TryRicochet(Vector2 bouncePoint, Vector2 incomingVelocity, Vector2 newDirection)
        {
            if (Projectile.localAI[0] >= MaxRicochets)
                return false;

            Projectile.localAI[0]++;
            SpawnBounceExplosion(bouncePoint);

            float speed = Math.Max(MinRicochetSpeed, incomingVelocity.Length());
            Projectile.velocity = newDirection.SafeNormalize(incomingVelocity.SafeNormalize(Vector2.UnitX)) * speed;
            Projectile.netUpdate = true;
            return true;
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
    }
}
