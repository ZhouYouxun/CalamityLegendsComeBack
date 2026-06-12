using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using CalamityLegendsComeBack.Weapons.A_Olds.TheEnforcer;
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
        private static readonly SoundStyle ImpactSound = new("CalamityLegendsComeBack/Sound/Other/DeltaForce/ASH12消音")
        {
            Volume = 0.75f,
            PitchVariance = 0.1f,
            MaxInstances = 4
        };

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
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 4;
            Projectile.timeLeft = 300;
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
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.3f, Pitch = -0.45f }, Projectile.Center);
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
                hitbox.width = 250;
                hitbox.height = 250;
                hitbox.Center = center;
                hitbox.DamageType = DamageClass.Magic;
                hitbox.netUpdate = true;
            }

            // 前、左、右、后四枚分裂弹。颜色编号通过 ai[0] 同步给所有客户端。
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float[] splitRotations = { 0f, -MathHelper.PiOver2, MathHelper.PiOver2, MathHelper.Pi };
            for (int i = 0; i < splitRotations.Length; i++)
            {
                Vector2 direction = forward.RotatedBy(splitRotations[i]);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + direction * 20f,
                    direction * 11.5f,
                    ModContent.ProjectileType<CoreOfCalamitySplitOrb>(),
                    Math.Max(1, (int)(Projectile.damage * 1.15f)),
                    Projectile.knockBack * 0.72f,
                    Projectile.owner,
                    i);
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

    /// <summary>
    /// 主弹爆炸时的短命伤害判定。视觉由主弹自身生成，这里保持不可见。
    /// </summary>
    /// <summary>
    /// 灾劫核心爆炸后的四色分裂弹。
    /// 每帧固定向左拐一度，经过短暂展开后再对最近目标追加追踪修正。
    /// </summary>
    internal sealed class CoreOfCalamitySplitOrb : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        private const int ActivationDelay = 12;
        private const int HomingDelay = 21;
        private const float LeftTurnPerUpdate = -MathHelper.Pi / 108f;
        private const float MaxSpeed = 25f;
        private const float HomingTurnSpeed = 0.110f;
        private static readonly Color[] Palette =
        {
            new(24, 62, 188),
            new(106, 218, 255),
            new(232, 56, 62),
            new(255, 194, 62)
        };

        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];
        private Color OrbColor => Palette[Utils.Clamp((int)Projectile.ai[0], 0, Palette.Length - 1)];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 450;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Timer++;
            Projectile.tileCollide = false;
            if (Projectile.localAI[1] == 0f)
                Projectile.localAI[1] = 120f + Projectile.ai[0] * 90f;

            // 固定左转始终存在；开始追踪后，再在此基础上向最近敌人转向。
            float speed = MathHelper.Lerp(Projectile.velocity.Length(), MaxSpeed, 0.045f);
            float updateScale = 1f / (Projectile.extraUpdates + 1f);
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(LeftTurnPerUpdate * updateScale);
            if (Timer >= HomingDelay)
            {
                NPC target = Projectile.Center.ClosestNPCAt(1350f);
                if (target is not null)
                {
                    float desiredRotation = (target.Center - Projectile.Center).ToRotation();
                    direction = direction.ToRotation().AngleTowards(desiredRotation, HomingTurnSpeed * updateScale).ToRotationVector2();
                }
            }

            Projectile.velocity = direction * speed;
            Projectile.rotation = direction.ToRotation();
            Lighting.AddLight(Projectile.Center, OrbColor.ToVector3() * 0.58f);
            SpawnFlightEffects(direction);
            SpawnHyperiusFlightEffects(direction);
        }

        public override bool? CanDamage() => Timer >= ActivationDelay;

        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner == Main.myPlayer)
                SpawnDamageExplosion();

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                OrbColor,
                "CalamityMod/Particles/BloomRing",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.1f,
                0.54f,
                14));

            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.TintableDustLighted,
                    Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.2f, 5.4f),
                    70,
                    OrbColor,
                    Main.rand.NextFloat(0.7f, 1.3f));
                dust.noGravity = true;
            }
        }

        private void SpawnDamageExplosion()
        {
            int explosionIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<NewLegendSHPE>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner);

            if (!Main.projectile.IndexInRange(explosionIndex))
                return;

            Projectile explosion = Main.projectile[explosionIndex];
            int explosionSize = new BalanceSHPC().GetDefaultOrbExplosionSize();
            explosion.Resize(explosionSize, explosionSize);
            explosion.Center = Projectile.Center;
            explosion.DamageType = DamageClass.Magic;
            explosion.netUpdate = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 position = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (int i = 0; i < 3; i++)
            {
                Main.EntitySpriteDraw(
                    bloom,
                    position,
                    null,
                    Color.Lerp(OrbColor, Color.White, i * 0.35f) with { A = 0 } * 0.76f,
                    Projectile.rotation,
                    bloom.Size() * 0.5f,
                    new Vector2(0.21f, 0.15f) * (1f - i * 0.2f),
                        SpriteEffects.None);
            }

            TheNewEnforcerMagicCoreDraw.Draw(
                position,
                Projectile.rotation,
                Projectile.scale * 2.05f,
                0.86f,
                OrbColor,
                Projectile.identity);

            DrawHyperiusGlow(position);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            Vector2[] trailPoints = CoreOfCalamityEnergyOrb.BuildTrailPoints(Projectile);
            if (trailPoints.Length < 2)
                return;

            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            PrimitiveRenderer.RenderTrail(
                trailPoints,
                new PrimitiveSettings(
                    (completion, _) => MathF.Sin((1f - completion) * MathHelper.PiOver2) * 18f,
                    (completion, _) =>
                    {
                        Color color = Color.Lerp(OrbColor, Color.Transparent, completion);
                        color.A = 0;
                        return color;
                    },
                    (_, _) => Vector2.Zero,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:TrailStreak"]),
                trailPoints.Length * 2);
        }

        private void SpawnFlightEffects(Vector2 direction)
        {
            if (!Main.rand.NextBool(2))
                return;

            Dust dust = Dust.NewDustPerfect(
                Projectile.Center,
                DustID.TintableDustLighted,
                -direction.RotatedByRandom(0.32f) * Main.rand.NextFloat(0.6f, 2.4f),
                70,
                OrbColor,
                Main.rand.NextFloat(0.64f, 1.08f));
            dust.noGravity = true;
        }

        private void SpawnHyperiusFlightEffects(Vector2 direction)
        {
            if (Main.dedServ || (Projectile.numUpdates != 0 && Main.rand.NextBool(3)))
                return;

            float phase = Projectile.localAI[1] + Timer * 0.09f;
            Color secondary = Color.Lerp(OrbColor, Color.White, 0.42f);
            float wave = 0.12f + 0.08f * MathF.Sin(phase * 1.7f);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 waveDirection = direction.RotatedBy(side * wave);
                Vector2 orbPos = Projectile.Center + waveDirection * 5.5f - direction * 15f;
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    orbPos,
                    -direction * Main.rand.NextFloat(0.35f, 1.2f) + waveDirection.RotatedBy(MathHelper.PiOver2) * side * Main.rand.NextFloat(0.2f, 0.7f),
                    "CalamityMod/Particles/BloomCircle",
                    false,
                    Main.rand.Next(8, 13),
                    Main.rand.NextFloat(0.12f, 0.23f),
                    Main.rand.NextBool() ? OrbColor : secondary,
                    new Vector2(0.34f, 1.15f),
                    true,
                    true,
                    extraRotation: -MathHelper.PiOver2,
                    shrinkSpeed: 0.48f,
                    glowOpacity: 0.78f));
            }

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - direction * Main.rand.NextFloat(8f, 22f),
                    DustID.RainbowMk2,
                    -direction.RotatedByRandom(0.26f) * Main.rand.NextFloat(0.45f, 1.8f),
                    90,
                    secondary,
                    Main.rand.NextFloat(0.62f, 1.05f));
                dust.noGravity = true;
            }
        }

        private void DrawHyperiusGlow(Vector2 drawPosition)
        {
            Texture2D glowSpark = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            Color color = OrbColor with { A = 0 };
            Color white = Color.White with { A = 0 };
            float pulse = 0.92f + 0.08f * MathF.Sin(Timer * 0.24f + Projectile.identity);

            for (int i = 0; i < 4; i++)
            {
                float rotation = Projectile.rotation + MathHelper.PiOver2 * i + Timer * 0.025f;
                Main.EntitySpriteDraw(
                    glowSpark,
                    drawPosition,
                    null,
                    Color.Lerp(color, white, i * 0.16f) * (0.42f - i * 0.045f),
                    rotation,
                    glowSpark.Size() * 0.5f,
                    new Vector2(0.12f + i * 0.014f, 0.82f + i * 0.12f) * Projectile.scale * pulse,
                    SpriteEffects.None,
                    0f);
            }
        }
    }
}
