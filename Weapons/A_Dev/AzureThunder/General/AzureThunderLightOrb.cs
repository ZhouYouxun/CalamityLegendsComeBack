using System;
using CalamityLegendsComeBack.Accssory.TS;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder.General
{
    // 左键第一段释放的发光体：高速追踪目标，并用 PrimitiveRenderer 绘制雷光拖尾。
    internal sealed class AzureThunderLightOrb : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AzureThunder";

        public override string Texture =>
            "CalamityLegendsComeBack/Texture/KsTexture/light_03";

        private int timer;

        // 追踪期望速度上限，实际速度通过 Lerp 慢慢贴近。
        private const float MaxHomingSpeed = 51f;

        private static readonly Color[] AscendantTealPalette =
        {
            new(42, 255, 205),
            new(112, 246, 255),
            new(88, 255, 155)
        };

        private Color AscendantVisualColor
        {
            get
            {
                float rate = timer * 0.035f + Math.Abs(Projectile.identity) * 0.19f;
                int colorIndex = (int)(rate % AscendantTealPalette.Length);
                float colorInterpolant = rate % 1f;
                Color cyclingColor = Color.Lerp(AscendantTealPalette[colorIndex], AscendantTealPalette[(colorIndex + 1) % AscendantTealPalette.Length], colorInterpolant);
                return Color.Lerp(cyclingColor, Color.White, 0.12f);
            }
        }

        public override void SetStaticDefaults()
        {
            // 长拖尾缓存用于 Sylvestaff 风格 shader 轨迹。
            ProjectileID.Sets.TrailCacheLength[Type] = 44;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            // 光球是轻量多段魔法弹幕，最多穿透 3 个敌人。
            Projectile.width = 24;
            Projectile.height = 24;

            Projectile.friendly = true;
            Projectile.hostile = false;

            Projectile.DamageType = DamageClass.Magic;

            Projectile.penetrate = 3;

            Projectile.timeLeft = 550;

            Projectile.extraUpdates = 1;

            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
        }

        public override void AI()
        {
            timer++;

            // 贴图朝向跟随速度方向。
            Projectile.rotation =
                Projectile.velocity.ToRotation();

            // 常驻青色照明，强化发光体存在感。
            Lighting.AddLight(
                Projectile.Center,
                new Vector3(0.2f, 0.8f, 1f) * 1.2f);

            NPC target =
                AzureThunderPlayer.FindNearestTarget(
                    Projectile.Center,
                    850f);

            if (target != null)
            {
                // 找到目标后缓慢转向最高 51 速度，避免瞬间折角太硬。
                Vector2 desiredVelocity =
                    (target.Center - Projectile.Center)
                    .SafeNormalize(
                        Projectile.velocity.SafeNormalize(Vector2.UnitX))
                    * MaxHomingSpeed;

                Projectile.velocity =
                    Vector2.Lerp(
                        Projectile.velocity,
                        desiredVelocity,
                        0.055f);
            }
            else
            {
                // 无目标时略微加速直飞，保持射出去的动势。
                Projectile.velocity *= 1.012f;
            }

            // 青雷粒子：核心周围的 GlowOrb 负责亮点。
            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(
                    new GlowOrbParticle(
                        Projectile.Center +
                        Main.rand.NextVector2Circular(6f, 6f),

                        -Projectile.velocity *
                        Main.rand.NextFloat(0.04f, 0.12f),

                        false,

                        Main.rand.Next(8, 13),

                        Main.rand.NextFloat(0.22f, 0.38f),

                        Main.rand.NextBool()
                            ? Color.Cyan
                            : new Color(120, 220, 255),

                        true,
                        false,
                        true));
            }

            if (Main.rand.NextBool(3))
            {
                // LineParticle 向后喷射，补出细长电火花。
                Vector2 sparkVelocity =
                    -Projectile.velocity
                    .SafeNormalize(Vector2.UnitX)
                    .RotatedByRandom(0.35f)
                    * Main.rand.NextFloat(1.1f, 3.6f);

                GeneralParticleHandler.SpawnParticle(
                    new LineParticle(
                        Projectile.Center -
                        Projectile.velocity *
                        Main.rand.NextFloat(0.12f, 0.48f) +
                        Main.rand.NextVector2Circular(5f, 5f),

                        sparkVelocity,

                        false,

                        Main.rand.Next(10, 16),

                        Main.rand.NextFloat(0.45f, 0.75f),

                        Main.rand.NextBool(3)
                            ? Color.Cyan
                            : new Color(120, 220, 255)));
            }

            if (Main.rand.NextBool(4))
            {
                // 原版 RGB dust 作为第三层粒子，保证低端画质仍有反馈。
                Dust dust =
                    Dust.NewDustPerfect(
                        Projectile.Center -
                        Projectile.velocity *
                        Main.rand.NextFloat(0.2f, 0.7f) +
                        Main.rand.NextVector2Circular(7f, 7f),

                        DustID.FireworksRGB,

                        -Projectile.velocity *
                        Main.rand.NextFloat(0.015f, 0.055f),

                        0,

                        Main.rand.NextBool()
                            ? Color.Cyan
                            : new Color(120, 220, 255),

                        Main.rand.NextFloat(0.65f, 1f));

                dust.noGravity = true;
            }

            SpawnAscendantSpiritVisuals();

            EmitLivingArcs();

            EmitTrailLightning();
        }

        public override void OnHitNPC(
            NPC target,
            NPC.HitInfo hit,
            int damageDone)
        {
            // 命中附加电击、触发饰品联动，并播放短促电击音。
            AzureThunderAccessoryPlayer
                .ApplyAzureThunderAccessoryOnHit(
                    Projectile,
                    target);

            AzureThunderSounds.PlayOrbImpact(target.Center);

            SpawnDisappearanceEffects(target.Center);

            SpawnImpactArcs(target);
        }

        // 电弧配色：以青色为基调，混入光球当前的循环色保持整体协调。
        private Color ArcColor =>
            Color.Lerp(
                new Color(70, 255, 240),
                AscendantVisualColor,
                Main.rand.NextFloat(0.35f));

        // 飞行放电：周期性劈出会持续扭动的电弧。
        // 附近有敌人时电弧吸附成“静电触须”，否则绕核心空放。
        private void EmitLivingArcs()
        {
            if (Main.dedServ)
                return;

            // 用 identity 错开每颗光球的放电节拍，避免四颗同时闪。
            if ((timer + Projectile.identity % 7 * 2) % 14 != 0)
                return;

            bool bigArc = Main.rand.NextBool(4);

            NPC zapTarget = FindArcTarget(Projectile.Center, 280f, -1);
            if (zapTarget != null)
            {
                // 静电触须：电向敌人身体上的随机一点，两端都吸附跟随。
                Vector2 contactPoint =
                    zapTarget.Center +
                    Main.rand.NextVector2Circular(
                        zapTarget.width * 0.3f,
                        zapTarget.height * 0.3f);

                AzureThunderArcParticle tether =
                    new(
                        Projectile.Center,
                        contactPoint,
                        ArcColor,
                        bigArc ? 20 : 16,
                        bigArc ? 3.4f : 2.8f,
                        bigArc ? 1.4f : 1f);

                tether.AttachStart(Projectile);
                tether.AttachEnd(zapTarget);
                GeneralParticleHandler.SpawnParticle(tether);

                // 接触点补两粒电火花，强调“电到了”。
                for (int i = 0; i < 2; i++)
                {
                    Dust dust =
                        Dust.NewDustPerfect(
                            contactPoint,
                            DustID.Electric,
                            Main.rand.NextVector2Circular(2.6f, 2.6f),
                            0,
                            default,
                            Main.rand.NextFloat(0.5f, 0.8f));

                    dust.noGravity = true;
                    dust.color = ArcColor;
                }
            }
            else
            {
                // 空放电弧：无目标时绕核心随机方向劈出短弧，偶发大弧线。
                float arcLength =
                    Main.rand.NextFloat(52f, 104f) *
                    (bigArc ? 1.45f : 1f);

                AzureThunderArcParticle idleArc =
                    new(
                        Projectile.Center,
                        Projectile.Center +
                        Main.rand.NextVector2Unit() * arcLength,
                        ArcColor,
                        bigArc ? 18 : 14,
                        bigArc ? 3f : 2.4f,
                        bigArc ? 1.5f : 1f);

                idleArc.AttachStart(Projectile);
                GeneralParticleHandler.SpawnParticle(idleArc);
            }
        }

        // 轨迹缠绕电流：把整条历史飞行路径包进双螺旋电流里，
        // 并让电弧在轨迹上的随机两点之间反复跳动。快照留在世界里，
        // 弹幕飞远后旧路径依然带电，直到淡出。
        private void EmitTrailLightning()
        {
            if (Main.dedServ)
                return;

            bool spawnCrawler = timer % 10 == 0;
            bool spawnJumpArc = timer % 8 == 4;
            if (!spawnCrawler && !spawnJumpArc)
                return;

            Vector2[] trail = SnapshotTrail();
            if (trail == null)
                return;

            if (spawnCrawler)
            {
                // 1/5 概率出现更狂野的大缠绕。
                bool wild = Main.rand.NextBool(5);

                GeneralParticleHandler.SpawnParticle(
                    new AzureThunderTrailLightningParticle(
                        trail,
                        ArcColor,
                        Main.rand.Next(10, 16),
                        wild ? 2.8f : 2.2f,
                        Main.rand.NextFloat(10f, 16f) * (wild ? 1.6f : 1f),
                        wild ? 1.6f : 1.2f));
            }

            if (spawnJumpArc && trail.Length >= 14)
            {
                // 在轨迹上随机取两个相距足够远的点，让电弧跨过路径弯折跳动。
                int indexA = Main.rand.Next(trail.Length - 8);
                int indexB = Math.Min(
                    indexA + Main.rand.Next(8, 19),
                    trail.Length - 1);

                GeneralParticleHandler.SpawnParticle(
                    new AzureThunderArcParticle(
                        trail[indexA],
                        trail[indexB],
                        ArcColor,
                        Main.rand.Next(8, 13),
                        2.2f,
                        1.4f));
            }
        }

        // 收集有效历史轨迹：index 0 = 当前中心，之后是 oldPos 缓存（顶点坐标转中心坐标）。
        private Vector2[] SnapshotTrail()
        {
            int validCount = 0;
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    break;

                validCount++;
            }

            if (validCount < 6)
                return null;

            Vector2 halfSize = Projectile.Size * 0.5f;
            Vector2[] points = new Vector2[validCount + 1];
            points[0] = Projectile.Center;
            for (int i = 0; i < validCount; i++)
                points[i + 1] = Projectile.oldPos[i] + halfSize;

            return points;
        }

        // 命中放电：命中点炸出放射电弧，并向下一个敌人视觉链电（纯视觉，无伤害）。
        private void SpawnImpactArcs(NPC target)
        {
            if (Main.dedServ)
                return;

            int radialCount = Main.rand.Next(4, 6);
            for (int i = 0; i < radialCount; i++)
            {
                Vector2 arcDirection =
                    (MathHelper.TwoPi * i / radialCount +
                     Main.rand.NextFloat(-0.35f, 0.35f))
                    .ToRotationVector2();

                AzureThunderArcParticle radialArc =
                    new(
                        target.Center,
                        target.Center +
                        arcDirection *
                        Main.rand.NextFloat(76f, 128f),
                        ArcColor,
                        Main.rand.Next(12, 17),
                        2.4f,
                        1.25f);

                // 起点钉在目标身上，外端留在世界坐标，敌人移动时电弧被拉扯。
                radialArc.AttachStart(target);
                GeneralParticleHandler.SpawnParticle(radialArc);
            }

            NPC chainTarget = FindArcTarget(target.Center, 460f, target.whoAmI);
            if (chainTarget != null)
            {
                AzureThunderArcParticle chainArc =
                    new(
                        target.Center,
                        chainTarget.Center,
                        Color.Lerp(ArcColor, Color.White, 0.2f),
                        18,
                        3.6f,
                        0.85f);

                // 两端各吸附一个敌人，形成会跟着双方移动的活体电桥。
                chainArc.AttachStart(target);
                chainArc.AttachEnd(chainTarget);
                GeneralParticleHandler.SpawnParticle(chainArc);
            }
        }

        // 最近敌人搜索，可排除指定 NPC（用于链电时跳过刚命中的目标）。
        private static NPC FindArcTarget(
            Vector2 point,
            float maxDistance,
            int excludeIndex)
        {
            NPC bestTarget = null;
            float bestDistance = maxDistance;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.whoAmI == excludeIndex ||
                    !npc.CanBeChasedBy())
                    continue;

                float distance =
                    point.Distance(npc.Center);

                if (distance < bestDistance)
                {
                    bestTarget = npc;
                    bestDistance = distance;
                }
            }

            return bestTarget;
        }

        // Shader 颜色控制：拖尾从透明青白之间脉动。
        internal Color ColorFunction(
            float completionRatio,
            Vector2 vertexPos)
        {
            float opacity =
                MathF.Pow(
                    Utils.GetLerpValue(
                        1f,
                        0.64f,
                        completionRatio,
                        true),
                    3f);

            float colorInterpolant =
                MathF.Cos(
                    MathHelper.Pi *
                    completionRatio -
                    Main.GlobalTimeWrappedHourly * 7.2f)
                * 0.5f + 0.5f;

            Color cyan =
                new Color(70, 255, 255);

            Color blue =
                new Color(80, 170, 255);

            Color baseColor =
                CalamityUtils.MulticolorLerp(
                    colorInterpolant,
                    cyan,
                    Color.White,
                    blue,
                    Color.White);

            return baseColor * opacity;
        }

        // Shader 宽度控制：拖尾前端展开，后段用余弦轻微起伏。
        internal float WidthFunction(
            float completionRatio,
            Vector2 vertexPos)
        {
            float expansionCompletion =
                1f -
                MathF.Pow(
                    1f -
                    Utils.GetLerpValue(
                        0f,
                        0.3f,
                        completionRatio,
                        true),
                    2f);

            float undulation =
                MathF.Cos(
                    MathHelper.Pi *
                    completionRatio *
                    5f -
                    Main.GlobalTimeWrappedHourly * 23f)
                * 2.4f;

            float maxWidth =
                undulation + 26f;

            return MathHelper.Lerp(
                0f,
                Projectile.scale * maxWidth,
                expansionCompletion);
        }

        // Shader 偏移：让拖尾顶点围绕弹幕中心计算。
        internal Vector2 OffsetFunction(
            float completionRatio,
            Vector2 vertexPos)
        {
            return Projectile.Size * 0.5f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // bloom 绘制光球核心，trail shader 绘制高速拖尾。
            Texture2D bloom =
                ModContent.Request<Texture2D>(
                    "CalamityMod/ExtraTextures/BloomCirclePinpoint")
                .Value;
            Texture2D circularSmear = ModContent.Request<Texture2D>("CalamityMod/Particles/CircularSmear").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SimpleStar").Value;
            Texture2D fadeStreak = ModContent.Request<Texture2D>("CalamityMod/Particles/FadeStreak").Value;

            Vector2 drawPosition =
                Projectile.Center -
                Main.screenPosition;

            // 核心闪烁：随时间和弹幕 identity 错开，避免多颗完全同步。
            float pulse =
                1f +
                (float)Math.Sin(
                    Main.GlobalTimeWrappedHourly * 12f +
                    Projectile.identity)
                * 0.08f;

            Main.spriteBatch.EnterShaderRegion(
                BlendState.Additive);

            // 外层青雷光。
            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                new Color(90, 255, 255, 0) * 0.85f,
                0f,
                bloom.Size() * 0.5f,
                Projectile.scale * 0.9f * pulse,
                SpriteEffects.None);

            // 白色核心。
            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                Color.White with { A = 0 } * 0.55f,
                0f,
                bloom.Size() * 0.5f,
                Projectile.scale * 0.42f,
                SpriteEffects.None);

            DrawAscendantSpiritOverlay(circularSmear, star, fadeStreak, drawPosition, pulse);

            Main.spriteBatch.ExitShaderRegion();

            // SylvRay 同款 Shader 拖尾，使用旧位置缓存作为曲线输入。
            MiscShaderData rayShader =
                GameShaders.Misc[
                    "CalamityMod:SylvestaffProjectile"];

            rayShader.SetShaderTexture(
                ModContent.Request<Texture2D>(
                    "CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));

            PrimitiveRenderer.RenderTrail(
                Projectile.oldPos,

                new PrimitiveSettings(
                    WidthFunction,
                    ColorFunction,
                    OffsetFunction,
                    pixelate: false,
                    shader: rayShader),

                32);

            return false;
        }

        // 雷暴命中特效：命中点喷射电弧、dust 和两层脉冲。
        private void SpawnDisappearanceEffects(
            Vector2 impactCenter)
        {
            float fxScale = 2.4f;

            for (int i = 0; i < 18; i++)
            {
                Particle bolt =
                    new BoltParticle(
                        impactCenter,

                        (new Vector2(4f, 4f) * fxScale)
                        .RotatedByRandom(100f)
                        * Main.rand.NextFloat(0.3f, 1.9f),

                        true,

                        13,

                        Main.rand.NextFloat(0.1f, 0.16f)
                        * fxScale,

                        Main.rand.NextBool(4)
                            ? Color.Cyan
                            : new Color(120, 220, 255),

                        new Vector2(1.8f, 0.8f),

                        true,
                        true,
                        false,
                        0.7f);

                GeneralParticleHandler.SpawnParticle(bolt);

                Dust dust =
                    Dust.NewDustPerfect(
                        impactCenter,

                        DustID.Electric,

                        (new Vector2(5f, 5f) * fxScale)
                        .RotatedByRandom(100f)
                        * Main.rand.NextFloat(0.5f, 1f),

                        0,

                        default,

                        Main.rand.NextFloat(0.4f, 0.55f)
                        * fxScale);

                dust.noGravity = true;

                dust.color =
                    Main.rand.NextBool(4)
                        ? Color.Cyan
                        : new Color(120, 220, 255);
            }

            Particle pulse =
                new CustomPulse(
                    impactCenter,
                    Vector2.Zero,
                    Color.Cyan,
                    "CalamityMod/Particles/HighResFoggyCircleHardEdge",
                    Vector2.One,
                    0f,
                    0f,
                    0.12f,
                    10);

            GeneralParticleHandler.SpawnParticle(pulse);

            for (int i = 0; i < 2; i++)
            {
                Particle orb =
                    new CustomPulse(
                        impactCenter,
                        Vector2.Zero,
                        new Color(90, 255, 255),
                        "CalamityMod/Particles/BloomCircle",
                        Vector2.One,
                        Main.rand.NextFloat(-10f, 10f),
                        1.6f,
                        0.42f,
                        14);

                GeneralParticleHandler.SpawnParticle(orb);

                Particle whiteOrb =
                    new CustomPulse(
                        impactCenter,
                        Vector2.Zero,
                        Color.White,
                        "CalamityMod/Particles/BloomCircle",
                        Vector2.One,
                        Main.rand.NextFloat(-10f, 10f),
                        1f,
                        0.2f,
                        14);

                GeneralParticleHandler.SpawnParticle(whiteOrb);
            }
        }

        private void SpawnAscendantSpiritVisuals()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            Color visualColor = AscendantVisualColor;

            if (timer % 3 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center - direction * 18f + normal * Main.rand.NextFloat(-5f, 5f),
                    -direction * Main.rand.NextFloat(0.4f, 1.2f),
                    "CalamityMod/Particles/BloomCircle",
                    false,
                    Main.rand.Next(10, 15),
                    Main.rand.NextFloat(0.1f, 0.17f),
                    visualColor * 0.65f,
                    new Vector2(0.68f, 1.35f),
                    glowCenter: true,
                    shrinkSpeed: 0.46f,
                    glowOpacity: 0.52f));
            }

            if (Main.rand.NextBool(3))
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center + normal * Main.rand.NextFloat(-7f, 7f),
                    -direction * Main.rand.NextFloat(0.8f, 2.4f) + normal * Main.rand.NextFloat(-0.8f, 0.8f),
                    "CalamityMod/Particles/PulseStar",
                    false,
                    Main.rand.Next(12, 19),
                    Main.rand.NextFloat(0.075f, 0.13f),
                    Color.Lerp(visualColor, Color.White, 0.24f),
                    Vector2.One,
                    glowCenter: true,
                    shrinkSpeed: 0.28f,
                    glowOpacity: 0.66f));
            }

            if (Main.rand.NextBool(4))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    ModContent.DustType<SquashDust>());

                dust.scale = Main.rand.NextFloat(0.75f, 1.12f);
                dust.velocity = -direction * Main.rand.NextFloat(2.2f, 4.8f) + normal * Main.rand.NextFloat(-0.9f, 0.9f);
                dust.noGravity = true;
                dust.color = visualColor;
                dust.fadeIn = 2.8f;
            }
        }

        private void DrawAscendantSpiritOverlay(Texture2D circularSmear, Texture2D star, Texture2D fadeStreak, Vector2 drawPosition, float pulse)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float rotation = direction.ToRotation() + MathHelper.PiOver2;
            Color visualColor = AscendantVisualColor with { A = 0 };
            float opacity = Utils.GetLerpValue(0f, 12f, timer, true) * Utils.GetLerpValue(0f, 24f, Projectile.timeLeft, true);

            Main.EntitySpriteDraw(
                circularSmear,
                drawPosition,
                null,
                visualColor * 0.18f * opacity,
                rotation * 1.25f,
                circularSmear.Size() * 0.5f,
                Projectile.scale * 0.24f * pulse,
                SpriteEffects.None);

            for (int i = 0; i < 5; i++)
            {
                Vector2 armDirection = (MathHelper.TwoPi * i / 5f).ToRotationVector2().RotatedBy(rotation);
                float armBoost = i == 1 || i == 4 ? 1.24f : 1f;

                Main.EntitySpriteDraw(
                    fadeStreak,
                    drawPosition,
                    null,
                    visualColor * 0.18f * opacity,
                    armDirection.ToRotation(),
                    new Vector2(fadeStreak.Width * 0.5f, 0f),
                    new Vector2(0.33f * armBoost, 0.18f) * Projectile.scale,
                    SpriteEffects.FlipVertically);
            }

            Main.EntitySpriteDraw(
                star,
                drawPosition,
                null,
                Color.White with { A = 0 } * 0.16f * opacity,
                rotation,
                star.Size() * 0.5f,
                Projectile.scale * 0.16f * pulse,
                SpriteEffects.None);
        }
    }
}
