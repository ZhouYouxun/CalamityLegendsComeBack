using System;
using System.Linq;
using CalamityLegendsComeBack.Weapons.AegisBlade.Visuals;
using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.AegisBlade.Projectiles
{
    /// <summary>
    /// 圣火光矢。左键轮盘每 4 帧从刃缘甩出，延迟后转为追踪。
    /// 视觉：三层拖尾（余烬外焰 / 圣金主焰 / 白金内芯）+ 可见的火矢头部。
    /// 旧版只有一条金色拖尾，头部用的是 InvisibleProj —— 也就是"箭头是空气"。
    /// </summary>
    internal sealed class AegisBorrowedLazharLaser : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        public new string LocalizationCategory => "Projectiles.AegisBlade";
        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const float MinimumHomingSpeed = 30f * 0.67f;
        private const float HomingRange = 1200f;
        private const float HomingStrength = 0.14f;
        private const int HomingDelayUpdates = 60;
        private const float HeadRadius = 8.5f;
        private int timer;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 150;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 3;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            timer++;
            Projectile.alpha = Math.Max(0, Projectile.alpha - 35);

            float currentSpeed = Math.Max(Projectile.velocity.Length(), MinimumHomingSpeed);
            NPC target = timer >= HomingDelayUpdates ? FindClosestTarget(HomingRange) : null;
            if (target != null)
            {
                Vector2 desiredDirection = Projectile.SafeDirectionTo(target.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX));
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredDirection * currentSpeed, HomingStrength);
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            AegisVisuals.Light(Projectile.Center, 0.72f);

            if (Main.dedServ)
                return;

            if (timer % 3 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center,
                    -Projectile.velocity * 0.08f,
                    false,
                    7,
                    Main.rand.NextFloat(0.16f, 0.3f),
                    AegisVisuals.RandomFlameColor(),
                    true,
                    true));
            }

            // 侧向掉落的火屑：让高速光矢有"擦出火星"的质感
            if (timer % 6 == 0)
            {
                Vector2 side = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
                Dust ember = Dust.NewDustPerfect(Projectile.Center,
                    AegisVisuals.ProfanedFireDust,
                    side * Main.rand.NextFloatDirection() * Main.rand.NextFloat(0.4f, 1.4f) -
                    Projectile.velocity * 0.05f,
                    0, Color.White, Main.rand.NextFloat(0.7f, 1.15f));
                ember.noGravity = true;
            }

            // 追踪启动的那一刻：光矢自己"点着"，给一个明确的状态切换信号
            if (timer == HomingDelayUpdates)
            {
                GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero,
                    AegisVisuals.Add(AegisVisuals.Core, 0.75f), AegisVisuals.TexBloom, Vector2.One,
                    0f, 0.05f, 0.4f, 10));
                AegisVisuals.CoronaRing(Projectile.Center, 6, 0.45f, Projectile.rotation);
            }
        }

        private NPC FindClosestTarget(float range)
        {
            NPC bestTarget = null;
            float bestDistance = range;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage || npc.life <= 0 || !npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Vector2.Distance(Projectile.Center, npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestTarget = npc;
            }

            return bestTarget;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            AegisVisuals.DirectionalImpact(Projectile.Center, forward, 0.6f);
            AegisVisuals.EmberJet(Projectile.Center, -forward, 5, 0.7f, 0.5f);
            AegisVisuals.WarbannerConverge(target.Center, forward, 1.25f, 2, 0.9f);
            GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(Projectile.Center, Vector2.Zero,
                0.55f, AegisVisuals.Add(AegisVisuals.Gold, 1f), 15));
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            AegisVisuals.HolyDetonation(Projectile.Center, 0.4f, false);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            Texture2D fire = AegisVisuals.Tex(AegisVisuals.TexFireBody);
            Texture2D orb = AegisVisuals.Tex(AegisVisuals.TexOrbSoft);
            Texture2D star = AegisVisuals.Tex(AegisVisuals.TexStarThin);
            Texture2D bloom = AegisVisuals.Tex(AegisVisuals.TexBloom);

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float opacity = Projectile.Opacity;
            if (opacity <= 0.02f)
                return false;

            float flicker = 0.85f + 0.15f * MathF.Sin(Main.GlobalTimeWrappedHourly * 26f + Projectile.identity);
            // 追踪启动后头部略微鼓起，表现"加速"
            float homingBoost = timer >= HomingDelayUpdates ? 1.22f : 1f;
            float radius = HeadRadius * homingBoost;

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);

            Main.EntitySpriteDraw(bloom, drawPosition, null,
                AegisVisuals.Add(AegisVisuals.Ember, 0.55f * opacity),
                0f, bloom.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(bloom, radius * 2.1f)), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(fire, drawPosition, null,
                AegisVisuals.Add(AegisVisuals.Gold, 0.75f * opacity * flicker),
                Projectile.rotation, fire.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(fire, radius), AegisVisuals.RadiusScale(fire, radius * 1.5f)),
                SpriteEffects.None, 0);
            Main.EntitySpriteDraw(orb, drawPosition, null,
                AegisVisuals.Add(AegisVisuals.Core, 0.85f * opacity * flicker),
                0f, orb.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(orb, radius * 0.4f)), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(star, drawPosition, null,
                AegisVisuals.Add(AegisVisuals.Core, 0.42f * opacity),
                Projectile.rotation, star.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(star, radius * 1.5f), AegisVisuals.RadiusScale(star, radius * 3.4f)),
                SpriteEffects.None, 0);

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            Vector2[] points = BuildTrailPoints();
            if (points.Length < 2)
                return;

            var trailShader = GameShaders.Misc["CalamityMod:ImpFlameTrail"];

            // ① 外焰：余烬红，最宽，压在最底下
            trailShader.SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));
            PrimitiveRenderer.RenderTrail(
                points,
                new PrimitiveSettings(OuterWidthFunction, OuterColorFunction, OffsetFunction, true, true, trailShader),
                points.Length * 2);

            // ② 主焰：圣金
            PrimitiveRenderer.RenderTrail(
                points,
                new PrimitiveSettings(WidthFunction, ColorFunction, OffsetFunction, true, true, trailShader),
                points.Length * 2);

            // ③ 内芯：白金细带，只取前 12 个点，让"刃"集中在头部
            Vector2[] corePoints = points.Take(Math.Min(12, points.Length)).ToArray();
            if (corePoints.Length < 2)
                return;

            trailShader.SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));
            PrimitiveRenderer.RenderTrail(
                corePoints,
                new PrimitiveSettings(CoreWidthFunction, CoreColorFunction, OffsetFunction, true, true, trailShader),
                corePoints.Length * 2);
        }

        private Vector2[] BuildTrailPoints()
        {
            Vector2[] points = Projectile.oldPos
                .Where(pos => pos != Vector2.Zero)
                .Select(pos => pos + Projectile.Size * 0.5f)
                .ToArray();

            if (points.Length == 0)
                return new[] { Projectile.Center - Projectile.velocity, Projectile.Center };

            if (points[0] != Projectile.Center)
                points = new[] { Projectile.Center }.Concat(points).ToArray();

            return points;
        }

        private Vector2 OffsetFunction(float completion, Vector2 _)
        {
            float waviness = (float)Math.Sin(completion * MathHelper.Pi * 1.5f + Main.GlobalTimeWrappedHourly * 16f) * 0.8f;
            return Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2) * waviness;
        }

        private float WidthFunction(float completion, Vector2 _)
        {
            const float ratio = 0.15f;
            float baseWidth = Projectile.scale * 14f;
            if (completion < ratio)
                return MathF.Sin(completion / ratio * MathHelper.PiOver2) * baseWidth + 0.1f;

            return Utils.Remap(completion, ratio, 1f, baseWidth, 0f);
        }

        private Color ColorFunction(float completion, Vector2 _) =>
            AegisVisuals.TrailColor(completion, 1, Projectile.Opacity);

        private float OuterWidthFunction(float completion, Vector2 _) => WidthFunction(completion, _) * 1.55f;

        private Color OuterColorFunction(float completion, Vector2 _) =>
            AegisVisuals.TrailColor(completion, 0, Projectile.Opacity * 0.62f);

        private float CoreWidthFunction(float completion, Vector2 _) => WidthFunction(completion, _) * 0.42f;

        private Color CoreColorFunction(float completion, Vector2 _) =>
            AegisVisuals.TrailColor(completion, 2, Projectile.Opacity);
    }

    /// <summary>
    /// 天火轨道打击。从目标正上方约 1000 像素垂直贯落。
    /// 视觉参考 ProvidenceHolyRay：落点先亮起符文预警圈，光柱内部有滚动的能量流，
    /// 落地后留下一小段焦痕余辉 —— 而不是旧版"一条纯金色棍子砸下来就没了"。
    /// </summary>
    internal sealed class AegisBorrowedOrbitalStrike : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        public new string LocalizationCategory => "Projectiles.AegisBlade";
        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int MaxLifetime = 90;
        private const float BeamHeight = 1200f;
        private const int AfterglowFrames = 26;   // 落地后纯视觉的余辉时长（无伤害）
        private int timer;
        private bool impacted;
        private int afterglowTimer;

        private int TargetIndex => (int)Projectile.ai[0];
        private Vector2 Destination => new(Projectile.ai[1], Projectile.ai[2]);

        /// <summary>0 → 1：越接近落点，预警圈越亮。</summary>
        private float ApproachRatio
        {
            get
            {
                Vector2 destination = Destination;
                if (destination == Vector2.Zero)
                    return 0f;
                return Utils.GetLerpValue(900f, 60f, Math.Abs(destination.Y - Projectile.Center.Y), true);
            }
        }

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = MaxLifetime;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            timer++;

            if (impacted)
            {
                afterglowTimer++;
                Projectile.velocity = Vector2.Zero;
                AegisVisuals.Light(Projectile.Center, 1.4f * (1f - afterglowTimer / (float)AfterglowFrames));
                if (afterglowTimer >= AfterglowFrames)
                    Projectile.Kill();
                return;
            }

            if (timer == 1)
            {
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.48f, Pitch = 0.18f }, Projectile.Center);
                Player owner = Main.player[Projectile.owner];
                if (owner.active && Projectile.owner == Main.myPlayer)
                    owner.Calamity().GeneralScreenShakePower = Math.Max(owner.Calamity().GeneralScreenShakePower, 3f);
            }

            Vector2 destination = Destination;
            if (destination == Vector2.Zero)
                destination = Projectile.Center + Vector2.UnitY * 800f;

            Projectile.velocity = Vector2.UnitY * Math.Max(34f, Projectile.velocity.Y);
            Projectile.rotation = MathHelper.PiOver2;
            AegisVisuals.Light(Projectile.Center, 0.95f);

            if (!Main.dedServ)
            {
                if (timer % 3 == 0)
                {
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(12f, 8f),
                        Main.rand.NextVector2Circular(0.8f, 0.8f),
                        false,
                        10,
                        Main.rand.NextFloat(0.2f, 0.36f),
                        AegisVisuals.RandomFlameColor(),
                        true,
                        true));
                }

                // 光柱两侧被撕开的圣灰
                if (Main.rand.NextBool(2))
                {
                    GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                        Projectile.Center + new Vector2(Main.rand.NextFloat(-24f, 24f), Main.rand.NextFloat(-60f, 20f)),
                        new Vector2(Main.rand.NextFloat(-1.6f, 1.6f), Main.rand.NextFloat(-3.5f, -0.8f)),
                        Color.Lerp(AegisVisuals.Charred, Color.DarkSlateGray, Main.rand.NextFloat(0.3f, 0.85f)),
                        Color.Transparent, Main.rand.NextFloat(0.45f, 0.85f), Main.rand.Next(24, 40),
                        Main.rand.NextFloat(-0.05f, 0.05f)));
                }

                // 落点预警：火星从四周被吸向落点，提前告诉玩家"这里要挨砸"
                float approach = ApproachRatio;
                if (approach > 0.15f && Main.rand.NextBool(2))
                {
                    Vector2 inward = Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2();
                    GeneralParticleHandler.SpawnParticle(new SparkParticle(
                        destination + inward * Main.rand.NextFloat(60f, 130f),
                        -inward * Main.rand.NextFloat(2.5f, 6f) * approach, false,
                        Main.rand.Next(12, 20), Main.rand.NextFloat(0.5f, 1f),
                        AegisVisuals.Gradient(Main.rand.NextFloat(0.15f, 0.75f))));
                }
            }

            if (Projectile.Center.Y >= destination.Y)
            {
                Projectile.Center = destination;
                Impact();
            }
        }

        public override bool? CanDamage() => impacted ? false : null;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (impacted)
                return false;

            float collisionPoint = 0f;
            Vector2 startPoint = Projectile.Center - Vector2.UnitY * BeamHeight;
            Vector2 endPoint = Projectile.Center;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), startPoint, endPoint, 28f * Projectile.scale, ref collisionPoint);
        }

        private void Impact()
        {
            if (impacted)
                return;

            impacted = true;
            Projectile.friendly = false;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = AfterglowFrames + 2;
            SpawnExplosionParticles();
        }

        private void SpawnExplosionParticles()
        {
            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.52f, Pitch = 0.12f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.DD2_BetsyFireballImpact with { Volume = 0.55f, Pitch = 0.5f }, Projectile.Center);

            AegisVisuals.HolyDetonation(Projectile.Center, 2.1f);

            // 贴地铺开的横向冲击：天火砸地是往两边扫，不是往天上炸
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center, Vector2.Zero, AegisVisuals.Add(AegisVisuals.Gold, 0.95f),
                new Vector2(2.3f, 0.5f), 0f, 0.05f, 1.05f, 20));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center, Vector2.Zero, AegisVisuals.Add(AegisVisuals.Ember, 0.8f),
                new Vector2(3.1f, 0.34f), 0f, 0.04f, 1.45f, 26));

            for (int i = -1; i <= 1; i += 2)
            {
                Vector2 sweep = new Vector2(i, -0.25f).SafeNormalize(Vector2.UnitX);
                AegisVisuals.EmberJet(Projectile.Center, sweep, 8, 1.25f, 0.35f);
            }

            GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(Projectile.Center, Vector2.Zero,
                1.15f, AegisVisuals.Add(AegisVisuals.Core, 1f), 16));
            AegisVisuals.Screenshake(Projectile.Center, 3.6f, 1200f);
        }

        public override void OnKill(int timeLeft)
        {
            if (!impacted)
                SpawnExplosionParticles();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);

            if (impacted)
            {
                // ── 落地余辉：焦痕 + 收缩的白芯 ──
                float fade = 1f - afterglowTimer / (float)AfterglowFrames;
                Vector2 drawPosition = Projectile.Center - Main.screenPosition;
                AegisVisuals.DrawScorchDecal(drawPosition, Projectile.identity * 0.7f,
                    82f * (0.55f + 0.45f * (1f - fade)), fade * 0.9f, new Vector2(1.25f, 0.6f));
                AegisVisuals.DrawSolarCore(drawPosition, 30f * fade, fade,
                    Main.GlobalTimeWrappedHourly * 4f, new Vector2(1.3f, 0.7f));
            }
            else
            {
                // ── 落点预警圈：符文圣印 + 收紧的地面光环 ──
                Vector2 destination = Destination;
                if (destination != Vector2.Zero)
                {
                    float approach = ApproachRatio;
                    if (approach > 0.02f)
                    {
                        Vector2 markPosition = destination - Main.screenPosition;
                        float markRadius = MathHelper.Lerp(120f, 62f, approach);
                        AegisVisuals.DrawRuneSigil(markPosition, markRadius,
                            Main.GlobalTimeWrappedHourly * (1.5f + approach * 6f),
                            approach * 0.85f, new Vector2(1f, 0.42f), 0.85f + approach * 0.6f);

                        Texture2D ring = AegisVisuals.Tex(AegisVisuals.TexRingThick);
                        Main.EntitySpriteDraw(ring, markPosition, null,
                            AegisVisuals.Add(AegisVisuals.Ember, 0.5f * approach),
                            0f, ring.Size() * 0.5f,
                            new Vector2(AegisVisuals.RadiusScale(ring, markRadius * 1.15f),
                                        AegisVisuals.RadiusScale(ring, markRadius * 0.48f)),
                            SpriteEffects.None, 0);
                    }
                }

                // ── 光柱头部的日核 ──
                AegisVisuals.DrawSolarCore(Projectile.Center - Main.screenPosition, 26f, 1f,
                    Main.GlobalTimeWrappedHourly * 5f, new Vector2(0.8f, 1.25f));
            }

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            if (impacted)
                return;

            Vector2[] beamPoints = new Vector2[12];
            for (int i = 0; i < beamPoints.Length; i++)
            {
                float ratio = i / (float)(beamPoints.Length - 1);
                beamPoints[i] = Projectile.Center - new Vector2(0f, BeamHeight * (1f - ratio));
            }

            var trailShader = GameShaders.Misc["CalamityMod:ImpFlameTrail"];

            // ① 外焰：余烬色宽柱
            trailShader.SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));
            PrimitiveRenderer.RenderTrail(
                beamPoints,
                new PrimitiveSettings(OuterWidthFunction, OuterColorFunction, OffsetFunction, true, true, trailShader,
                    textureCycleLength: 2.4f, textureScrollOffset: -Main.GlobalTimeWrappedHourly * 2.6f),
                beamPoints.Length * 2);

            // ② 主焰：圣金，纹理沿柱身向下滚动 = 能量在流
            PrimitiveRenderer.RenderTrail(
                beamPoints,
                new PrimitiveSettings(WidthFunction, ColorFunction, OffsetFunction, true, true, trailShader,
                    textureCycleLength: 3.6f, textureScrollOffset: -Main.GlobalTimeWrappedHourly * 4.2f),
                beamPoints.Length * 2);

            // ③ 内芯：白金细柱
            trailShader.SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));
            PrimitiveRenderer.RenderTrail(
                beamPoints,
                new PrimitiveSettings(CoreWidthFunction, CoreColorFunction, OffsetFunction, true, true, trailShader,
                    textureCycleLength: 5f, textureScrollOffset: -Main.GlobalTimeWrappedHourly * 6f),
                beamPoints.Length * 2);
        }

        private Vector2 OffsetFunction(float completion, Vector2 _)
        {
            float shimmer = (float)Math.Sin(completion * MathHelper.TwoPi + Main.GlobalTimeWrappedHourly * 20f) * 1.5f;
            return Vector2.UnitX * shimmer;
        }

        private float WidthFunction(float completion, Vector2 _)
        {
            float progress = Projectile.timeLeft / (float)MaxLifetime;
            float taper = (float)Math.Sin(completion * MathHelper.Pi);
            return 26f * progress * (0.8f + taper * 0.2f) * Projectile.scale;
        }

        private Color ColorFunction(float completion, Vector2 _)
        {
            float progress = Projectile.timeLeft / (float)MaxLifetime;
            return AegisVisuals.TrailColor(completion, 1, progress);
        }

        private float OuterWidthFunction(float completion, Vector2 _) => WidthFunction(completion, _) * 1.6f;

        private Color OuterColorFunction(float completion, Vector2 _)
        {
            float progress = Projectile.timeLeft / (float)MaxLifetime;
            return AegisVisuals.TrailColor(completion, 0, progress * 0.6f);
        }

        private float CoreWidthFunction(float completion, Vector2 _)
        {
            float progress = Projectile.timeLeft / (float)MaxLifetime;
            float taper = (float)Math.Sin(completion * MathHelper.Pi);
            return 11f * progress * (0.9f + taper * 0.1f) * Projectile.scale;
        }

        private Color CoreColorFunction(float completion, Vector2 _)
        {
            float progress = Projectile.timeLeft / (float)MaxLifetime;
            return AegisVisuals.TrailColor(completion, 2, progress);
        }
    }
}
