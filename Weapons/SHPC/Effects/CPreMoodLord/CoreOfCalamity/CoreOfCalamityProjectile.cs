using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using CalamityLegendsComeBack.Weapons.SHPC;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.CoreOfCalamity
{
    /// <summary>
    /// 灾劫核心的主弹。
    /// 本体贴图故意保持透明，实际外观由宽灰色 shader 拖尾、核心辉光和平行残影共同组成。
    /// </summary>
    internal sealed class CoreOfCalamityEnergyOrb : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        private static readonly Color BrightGray = new(224, 230, 238);
        private static readonly Color MidGray = new(138, 146, 158);
        private static readonly Color DarkGray = new(50, 54, 64);
        private static readonly SoundStyle ImpactSound = new("CalamityLegendsComeBack/Sound/Other/DeltaForce/ASH12消音");

        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float StoredSpeed => ref Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 26;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 38;
            Projectile.height = 38;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 4;
            Projectile.timeLeft = 60;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            // 主弹保持匀速。StoredSpeed 只在第一帧记录，后续粒子或碰撞不会令它逐渐减速。
            if (StoredSpeed <= 0f)
                StoredSpeed = Math.Max(Projectile.velocity.Length(), 16.5f);

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Projectile.velocity = forward * StoredSpeed;
            Projectile.rotation = forward.ToRotation();

            Lighting.AddLight(Projectile.Center, MidGray.ToVector3() * 0.72f);
            SpawnFlightEffects(forward);
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(ImpactSound, Projectile.Center);
            SpawnExplosionEffects();

            if (Projectile.owner != Main.myPlayer)
                return;

            int explosion = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<NewLegendSHPE>(),
                Math.Max(1, (int)(Projectile.damage * 1.33f)),
                Projectile.knockBack,
                Projectile.owner);
            if (Main.projectile.IndexInRange(explosion))
            {
                Projectile hitbox = Main.projectile[explosion];
                Vector2 center = Projectile.Center;
                int explosionSize = new BalanceSHPC().GetDefaultOrbExplosionSize();
                hitbox.width = explosionSize;
                hitbox.height = explosionSize;
                hitbox.Center = center;
                hitbox.DamageType = DamageClass.Magic;
                hitbox.netUpdate = true;
            }

            // 原地向四面八方炸开 12 枚分裂弹，每枚的颜色都独立随机抽取（不保证四色轮换），
            // 速度带一点随机浮动但不会相差太多；颜色编号通过 ai[0] 同步给所有客户端。
            const int splitCount = 12;
            const float splitSpeedMin = 12f;
            const float splitSpeedMax = 16f;
            for (int i = 0; i < splitCount; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                float speed = Main.rand.NextFloat(splitSpeedMin, splitSpeedMax);
                int colorIndex = Main.rand.Next(CoreOfCalamitySplitOrb.Palette.Length);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    direction * speed,
                    ModContent.ProjectileType<CoreOfCalamitySplitOrb>(),
                    Math.Max(1, (int)(Projectile.damage * 0.3)),
                    Projectile.knockBack * 0.72f,
                    Projectile.owner,
                    colorIndex);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrawGrayEnergyCore(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX), Projectile.scale, 1f);
            return false;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            Vector2[] trailPoints = BuildTrailPoints(Projectile);
            if (trailPoints.Length < 2)
                return;

            // 外层使用较宽的灰色 streak，让透明主弹本身就表现为一条宽能量流。
            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            PrimitiveRenderer.RenderTrail(
                trailPoints,
                new PrimitiveSettings(
                    MainTrailWidth,
                    MainTrailColor,
                    (_, _) => Vector2.Zero,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:TrailStreak"]),
                trailPoints.Length * 2);

            // 内层窄亮线负责把灰色能量流的核心从背景中提出来。
            PrimitiveRenderer.RenderTrail(
                trailPoints.Take(Math.Min(15, trailPoints.Length)).ToArray(),
                new PrimitiveSettings(
                    (completion, _) => MainTrailWidth(completion, Vector2.Zero) * 0.42f,
                    (completion, _) => Color.Lerp(BrightGray, Color.Transparent, completion),
                    (_, _) => Vector2.Zero,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:TrailStreak"]),
                Math.Min(15, trailPoints.Length) * 2);
        }

        internal static Vector2[] BuildTrailPoints(Projectile projectile)
        {
            Vector2[] points = projectile.oldPos
                .Where(position => position != Vector2.Zero)
                .Select(position => position + projectile.Size * 0.5f)
                .ToArray();

            if (points.Length == 0)
                return new[] { projectile.Center - projectile.velocity, projectile.Center };

            if (points[0] != projectile.Center)
                points = new[] { projectile.Center }.Concat(points).ToArray();

            return points;
        }

        private float MainTrailWidth(float completion, Vector2 _)
        {
            float head = MathF.Sin(Utils.GetLerpValue(0f, 0.18f, completion, true) * MathHelper.PiOver2);
            return Projectile.scale * 38f * head * (1f - completion * 0.82f);
        }

        private Color MainTrailColor(float completion, Vector2 _)
        {
            Color color = Color.Lerp(BrightGray, MidGray, Utils.GetLerpValue(0f, 0.34f, completion, true));
            color = Color.Lerp(color, DarkGray, Utils.GetLerpValue(0.34f, 1f, completion, true));
            color *= 1f - completion;
            color.A = 0;
            return color;
        }

        private void SpawnFlightEffects(Vector2 forward)
        {
            Vector2 backward = -forward;

            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    backward * Main.rand.NextFloat(0.8f, 2.3f),
                    "CalamityMod/Particles/BloomCircle",
                    false,
                    Main.rand.Next(10, 17),
                    Main.rand.NextFloat(0.26f, 0.46f),
                    Color.Lerp(MidGray, BrightGray, Main.rand.NextFloat()),
                    new Vector2(1.7f, 0.2f),
                    true,
                    true,
                    extraRotation: -MathHelper.PiOver2,
                    shrinkSpeed: 0.16f,
                    glowOpacity: 0.78f));
            }

            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center - forward * Main.rand.NextFloat(12f, 30f) + forward.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-8f, 8f),
                    backward * Main.rand.NextFloat(1.2f, 3.8f),
                    "CalamityMod/Particles/SmallBloom",
                    false,
                    Main.rand.Next(11, 18),
                    Main.rand.NextFloat(0.18f, 0.36f),
                    Color.Lerp(BrightGray, Color.White, Main.rand.NextFloat(0.08f, 0.36f)),
                    new Vector2(1.65f, 0.34f),
                    true,
                    true,
                    extraRotation: -MathHelper.PiOver2,
                    shrinkSpeed: 0.42f,
                    glowOpacity: 0.76f));
            }

            Dust dust = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                DustID.TintableDustLighted,
                backward.RotatedByRandom(0.34f) * Main.rand.NextFloat(0.8f, 3.2f),
                80,
                Main.rand.NextBool(3) ? BrightGray : MidGray,
                Main.rand.NextFloat(0.82f, 1.35f));
            dust.noGravity = true;



            if (Main.rand.NextBool(1))
            {
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    backward.RotatedByRandom(0.45f) * Main.rand.NextFloat(0.25f, 1.4f),
                    Main.rand.NextFloat(0.28f, 0.48f),
                    Color.Lerp(MidGray, BrightGray, Main.rand.NextFloat(0.22f, 0.72f)),
                    Main.rand.Next(10, 16)));
            }
        }

        private void SpawnExplosionEffects()
        {
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                MidGray,
                "CalamityMod/Particles/BloomRing",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.16f,
                1.38f,
                22));

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                BrightGray,
                "CalamityMod/Particles/BloomCircle",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.74f,
                0.08f,
                18));

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                DarkGray,
                Vector2.One,
                0f,
                0.16f,
                1.1f,
                24));

            for (int i = 0; i < 42; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2.2f, 10.8f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.TintableDustLighted,
                    velocity,
                    80,
                    Color.Lerp(DarkGray, BrightGray, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.9f, 1.9f));
                dust.noGravity = true;
            }

            for (int i = 0; i < 16; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2.2f, 7.6f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center,
                    velocity,
                    "CalamityMod/Particles/BloomCircle",
                    false,
                    Main.rand.Next(12, 22),
                    Main.rand.NextFloat(0.35f, 0.72f),
                    Color.Lerp(MidGray, BrightGray, Main.rand.NextFloat()),
                    new Vector2(0.35f, 1.8f),
                    true,
                    true,
                    glowOpacity: 0.82f));
            }
        }

        internal static void DrawGrayEnergyCore(Vector2 center, Vector2 forward, float scale, float opacity)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/ForwardSmear").Value;
            Texture2D needle = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/SHPC/Effects/EAfterDog/Ascendant/AscendantSpirit_PROJ").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Vector2 screenCenter = center - Main.screenPosition;
            Vector2 perpendicular = forward.RotatedBy(MathHelper.PiOver2);

            // Arc Nova 风格的平行残影：越往后越暗、越窄，形成连续凝聚并向后拖开的结构。
            for (int side = -2; side <= 2; side++)
            {
                for (int segment = 1; segment <= 8; segment++)
                {
                    float fade = (1f - segment / 9f) * opacity;
                    Vector2 position = screenCenter - forward * segment * 7f + perpendicular * side * 4.5f;
                    Main.EntitySpriteDraw(
                        bloom,
                        position,
                        null,
                        MidGray with { A = 0 } * fade * 0.28f,
                        forward.ToRotation(),
                        bloom.Size() * 0.5f,
                        new Vector2(0.2f + fade * 0.16f, 0.07f + fade * 0.06f) * scale,
                        SpriteEffects.None);
                }
            }

            //Main.EntitySpriteDraw(
            //    smear,
            //    screenCenter - forward * 8f,
            //    null,
            //    MidGray with { A = 0 } * opacity * 0.72f,
            //    forward.ToRotation() + MathHelper.PiOver2,
            //    new Vector2(smear.Width * 0.5f, smear.Height),
            //    new Vector2(0.3f, 0.52f) * scale,
            //    SpriteEffects.None);

            for (int i = 0; i < 3; i++)
            {
                float layerScale = (1f - i * 0.22f) * scale;
                Main.EntitySpriteDraw(
                    bloom,
                    screenCenter,
                    null,
                    Color.Lerp(MidGray, BrightGray, i * 0.42f) with { A = 0 } * opacity * 0.8f,
                    Main.GlobalTimeWrappedHourly * (i % 2 == 0 ? 1f : -1f),
                    bloom.Size() * 0.5f,
                    new Vector2(0.42f, 0.34f) * layerScale,
                    SpriteEffects.None);
            }

            float pulse = 0.9f + 0.1f * MathF.Sin(Main.GlobalTimeWrappedHourly * 12f + center.X * 0.003f);
            Vector2 needleScale = new(0.58f, 1.84f);
            for (int i = 0; i < 10; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 10f).ToRotationVector2() * (2.2f + pulse * 1.4f) * scale;
                Main.EntitySpriteDraw(
                    needle,
                    screenCenter + offset,
                    null,
                    BrightGray with { A = 0 } * opacity * 0.32f,
                    forward.ToRotation() + MathHelper.PiOver2,
                    needle.Size() * 0.5f,
                    needleScale * scale,
                    SpriteEffects.None);
            }

            Main.EntitySpriteDraw(
                needle,
                screenCenter,
                null,
                Color.Lerp(MidGray, Color.White, 0.32f) with { A = 0 } * opacity * 0.82f,
                forward.ToRotation() + MathHelper.PiOver2,
                needle.Size() * 0.5f,
                needleScale * scale * pulse,
                SpriteEffects.None);

            for (int i = 0; i < 4; i++)
            {
                float rotation = forward.ToRotation() + MathHelper.PiOver2 * i + Main.GlobalTimeWrappedHourly * 0.9f;
                Main.EntitySpriteDraw(
                    star,
                    screenCenter,
                    null,
                    Color.Lerp(MidGray, BrightGray, 0.55f) with { A = 0 } * opacity * 0.34f,
                    rotation,
                    star.Size() * 0.5f,
                    new Vector2(0.16f, 0.78f) * scale * pulse,
                    SpriteEffects.None);
            }
        }
    }

    
}
