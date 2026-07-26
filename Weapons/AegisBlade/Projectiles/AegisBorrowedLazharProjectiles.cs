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
    /// 圣火光矢。挥舞庇护之刃时爆发散射（模仿 Entropic Claymore）。
    /// 前 35 帧保持游动波浪扩散，35 帧后激活强力拐弯追踪（CalamityUtils.HomeInOnSelectedNPC）。
    /// 视觉：增大体量缩放，强化 GlowOrb / Spark 粒子与多层高亮渲染和粗强拖尾。
    /// </summary>
    internal sealed class AegisBorrowedLazharLaser : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        public new string LocalizationCategory => "Projectiles.AegisBlade";
        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const float HomingRange = 850f;
        private const int HomingDelayUpdates = 35;
        private const float HeadBaseRadius = 12.5f;

        private int timer;
        private int spinDir = 100;
        private int waveOften = 40;
        private float sizeVariance = 1f;
        private NPC target;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 24;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 360;
            Projectile.penetrate = 4;
            Projectile.extraUpdates = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
            Projectile.ArmorPenetration = 15;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (spinDir == 100)
            {
                spinDir = Main.rand.NextBool() ? 1 : -1;
                waveOften = Main.rand.Next(12, 36);
                Projectile.scale = Main.rand.NextFloat(1.1f, 1.6f);
            }

            if (Projectile.numHits > 0)
                Projectile.extraUpdates = 2;
            else
                Projectile.extraUpdates = 1;

            sizeVariance = Utils.GetLerpValue(-5, 60, Projectile.timeLeft, true);
            Projectile.alpha = Math.Max(0, Projectile.alpha - 45);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            AegisVisuals.Light(Projectile.Center, 0.85f * Projectile.scale);

            // 模仿 EntropicFlechette 阶段划分：35 帧前扩散游走，35 帧后启动强力追踪/巡航
            if (timer < HomingDelayUpdates)
            {
                // S型/弧形轻微扭动，形成自然散射感
                Projectile.velocity = Projectile.velocity.RotatedBy(Main.rand.NextFloat(0.012f, 0.038f) * spinDir);
                if (timer % waveOften == 0)
                    spinDir *= -1;
            }
            else
            {
                if (timer == HomingDelayUpdates && !Main.dedServ)
                {
                    GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero,
                        AegisVisuals.Add(AegisVisuals.Core, 0.85f), AegisVisuals.TexBloom, Vector2.One,
                        0f, 0.05f, 0.5f, 12));
                    AegisVisuals.CoronaRing(Projectile.Center, 8, 0.6f, Projectile.rotation);
                }

                if (Projectile.numHits < 1)
                {
                    target = Projectile.Center.ClosestNPCAt(HomingRange);
                    if (target == null)
                    {
                        Vector2 moveToMouse = (owner.Calamity().mouseWorld - Projectile.Center).SafeNormalize(Vector2.UnitX);
                        if (Projectile.velocity.Length() < 16f)
                            Projectile.velocity += moveToMouse * 0.2f;
                        else
                            Projectile.velocity *= 0.96f;

                        if (timer % waveOften == 0)
                            spinDir *= -1;

                        Projectile.velocity = Projectile.velocity.RotatedBy(Main.rand.NextFloat(0.02f, 0.05f) * spinDir);
                    }
                    else
                    {
                        // 使用 CalamityUtils 强力强转向追踪 (速度 15，平滑系数 0.98)
                        CalamityUtils.HomeInOnSelectedNPC(Projectile, target, true, 0.55f, 15f, 0.98f);
                    }
                }
                else
                {
                    if (timer % waveOften == 0)
                        spinDir *= -1;

                    Projectile.velocity = Projectile.velocity.RotatedBy(Main.rand.NextFloat(0.03f, 0.07f) * spinDir * Utils.GetLerpValue(60, 180, timer, true));
                }
            }

            if (!Main.dedServ)
            {
                if (timer % 2 == 0)
                {
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        Projectile.Center,
                        -Projectile.velocity * 0.12f + Main.rand.NextVector2Circular(0.5f, 0.5f),
                        false,
                        8,
                        Main.rand.NextFloat(0.2f, 0.38f) * Projectile.scale,
                        AegisVisuals.RandomFlameColor(),
                        true,
                        true));
                }

                if (timer % 5 == 0)
                {
                    Vector2 side = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
                    Dust ember = Dust.NewDustPerfect(Projectile.Center,
                        AegisVisuals.ProfanedFireDust,
                        side * Main.rand.NextFloatDirection() * Main.rand.NextFloat(0.5f, 1.8f) - Projectile.velocity * 0.08f,
                        0, Color.White, Main.rand.NextFloat(0.8f, 1.35f));
                    ember.noGravity = true;
                }
            }

            timer++;
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
            AegisVisuals.DirectionalImpact(Projectile.Center, forward, 0.75f);
            AegisVisuals.EmberJet(Projectile.Center, -forward, 7, 0.85f, 0.55f);
            AegisVisuals.WarbannerConverge(target.Center, forward, 1.4f, 3, 1.0f);
            GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(Projectile.Center, Vector2.Zero,
                0.7f, AegisVisuals.Add(AegisVisuals.Gold, 1f), 16));
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            AegisVisuals.HolyDetonation(Projectile.Center, 0.55f, false);
            for (int i = 0; i < 6; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(4f, 4f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center, AegisVisuals.ProfanedFireDust, vel, 0, Color.White, Main.rand.NextFloat(0.9f, 1.4f));
                dust.noGravity = true;
            }
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
            float homingBoost = timer >= HomingDelayUpdates ? 1.25f : 1f;
            float radius = HeadBaseRadius * Projectile.scale * sizeVariance * homingBoost;

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);

            Main.EntitySpriteDraw(bloom, drawPosition, null,
                AegisVisuals.Add(AegisVisuals.Ember, 0.65f * opacity),
                0f, bloom.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(bloom, radius * 2.3f)), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(fire, drawPosition, null,
                AegisVisuals.Add(AegisVisuals.Gold, 0.85f * opacity * flicker),
                Projectile.rotation, fire.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(fire, radius * 1.2f), AegisVisuals.RadiusScale(fire, radius * 1.8f)),
                SpriteEffects.None, 0);
            Main.EntitySpriteDraw(orb, drawPosition, null,
                AegisVisuals.Add(AegisVisuals.Core, 0.95f * opacity * flicker),
                0f, orb.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(orb, radius * 0.5f)), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(star, drawPosition, null,
                AegisVisuals.Add(AegisVisuals.Core, 0.55f * opacity),
                Projectile.rotation, star.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(star, radius * 1.8f), AegisVisuals.RadiusScale(star, radius * 4.0f)),
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

            // ① 外焰：余烬红
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

            // ③ 内芯：白金细带
            Vector2[] corePoints = points.Take(Math.Min(14, points.Length)).ToArray();
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
            float waviness = (float)Math.Sin(completion * MathHelper.Pi * 1.5f + Main.GlobalTimeWrappedHourly * 18f) * 1.1f;
            return Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2) * waviness;
        }

        private float WidthFunction(float completion, Vector2 _)
        {
            const float ratio = 0.15f;
            float baseWidth = Projectile.scale * 20f * sizeVariance;
            if (completion < ratio)
                return MathF.Sin(completion / ratio * MathHelper.PiOver2) * baseWidth + 0.1f;

            return Utils.Remap(completion, ratio, 1f, baseWidth, 0f);
        }

        private Color ColorFunction(float completion, Vector2 _) =>
            AegisVisuals.TrailColor(completion, 1, Projectile.Opacity);

        private float OuterWidthFunction(float completion, Vector2 _) => WidthFunction(completion, _) * 1.6f;

        private Color OuterColorFunction(float completion, Vector2 _) =>
            AegisVisuals.TrailColor(completion, 0, Projectile.Opacity * 0.65f);

        private float CoreWidthFunction(float completion, Vector2 _) => WidthFunction(completion, _) * 0.45f;

        private Color CoreColorFunction(float completion, Vector2 _) =>
            AegisVisuals.TrailColor(completion, 2, Projectile.Opacity);
    }

    /// <summary>
    /// 净化激光炮式的短促天降光柱。完完全全模仿 HolyLaser.cs 的视觉效果与粒子生成，
    /// 从高空直接打穿敌人焦点，不转动也不跟随移动，呈固定方向直线轰击。
    /// </summary>
    internal sealed class AegisBorrowedOrbitalStrike : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        public new string LocalizationCategory => "Projectiles.AegisBlade";
        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int LaserLifetime = 24;
        private const float BeamLength = 2600f;
        private const float MaxScale = 1.35f;
        private int timer;

        private int TargetIndex => (int)Projectile.ai[0];
        private Vector2 Destination => new(Projectile.ai[1], Projectile.ai[2]);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = LaserLifetime;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;

            Vector2 destination = Destination;
            if (destination == Vector2.Zero)
                destination = Projectile.Center + Vector2.UnitY * 1000f;

            // 方向保持固定，从天空起点穿过敌人焦点
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            if (timer == 0)
            {
                TriggerIntersectionImpact(destination);
            }

            timer++;

            // 完美复刻 BaseLaserbeamProjectile 经典正弦粗细展开与收隐
            float progress = (float)timer / LaserLifetime;
            Projectile.scale = MathHelper.Clamp((float)Math.Sin(progress * MathHelper.Pi) * 4f * MaxScale, 0f, MaxScale);

            AegisVisuals.Light(destination, 2.2f * Projectile.scale);

            if (!Main.dedServ)
            {
                Color color1 = Color.Goldenrod;
                Color color2 = Color.Orange;

                // 完全匹配 HolyLaser.cs 的粒子生成逻辑
                for (int i = 0; i < 4; i++)
                {
                    Vector2 effectsPosition = Vector2.Lerp(Projectile.Center, Projectile.Center + Projectile.velocity * BeamLength, Main.rand.NextFloat());
                    Vector2 randomLineEffectPosition = effectsPosition + Main.rand.NextVector2Circular(8f, 8f);

                    if (i % 2 == 0)
                    {
                        Dust laserDust = Dust.NewDustPerfect(randomLineEffectPosition, DustID.FireworksRGB, Projectile.velocity * Main.rand.NextFloat(5f, 40f), Scale: Main.rand.NextFloat(0.8f, 1.15f));
                        laserDust.noGravity = true;
                        laserDust.color = Main.rand.NextBool(3) ? color2 : color1;

                        Dust laserDust2 = Dust.NewDustPerfect(randomLineEffectPosition, ModContent.DustType<CalamityMod.Dusts.LightDust>(), Projectile.velocity * Main.rand.NextFloat(5f, 40f), Scale: Main.rand.NextFloat(0.8f, 1.15f) * 1.5f);
                        laserDust2.noGravity = true;
                        laserDust2.color = Main.rand.NextBool(3) ? color2 : color1;
                        laserDust2.noLightEmittence = true;
                    }
                    else
                    {
                        Particle spark = new CustomSpark(randomLineEffectPosition, Projectile.velocity * Main.rand.NextFloat(1f, 10f), "CalamityMod/Particles/ProvidenceMarkParticle", false, 17, Main.rand.NextFloat(1.15f, 1.3f), Color.Lerp(color1, color2, Main.rand.NextFloat()), new Vector2(1.3f, 0.5f), true, false, 0, false, false, Main.rand.NextFloat(0.3f, 0.4f));
                        GeneralParticleHandler.SpawnParticle(spark);
                    }
                }
            }
        }

        private void TriggerIntersectionImpact(Vector2 destination)
        {
            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.55f, Pitch = 0.15f }, destination);
            SoundEngine.PlaySound(SoundID.DD2_BetsyFireballImpact with { Volume = 0.6f, Pitch = 0.45f }, destination);

            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/Providence/ProvidenceHolyRay") { Volume = 0.9f, Pitch = 0.15f }, destination);
            AegisVisuals.HolyDetonation(destination, 2.8f);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                destination, Vector2.Zero, AegisVisuals.Add(AegisVisuals.Gold, 1f),
                new Vector2(3.3f, 0.75f), Projectile.rotation, 0.04f, 1.45f, 22));

            GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(destination, Vector2.Zero,
                1.6f, AegisVisuals.Add(AegisVisuals.Core, 1f), 18));
            AegisVisuals.Screenshake(destination, 6.5f, 1400f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            Vector2 beamEnd = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * BeamLength;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, beamEnd, 50f * Projectile.scale, ref _);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            AegisVisuals.DirectionalImpact(target.Center, forward, 0.85f);
            AegisVisuals.EmberJet(target.Center, -forward, 8, 1.0f, 0.5f);
            GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(target.Center, Vector2.Zero,
                0.8f, AegisVisuals.Add(AegisVisuals.Gold, 1f), 16));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            if (Projectile.scale <= 0.01f)
                return false;

            Texture2D bloom = AegisVisuals.Tex(AegisVisuals.TexBloom);
            Texture2D startTex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/ProvidenceHolyRayStart", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Texture2D midTex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/ProvidenceHolyRayMid", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Texture2D endTex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/ProvidenceHolyRayEnd", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Vector2 beamDirection = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 drawHeadPos = Projectile.Center - Main.screenPosition;
            Vector2 drawScale = new(Projectile.scale * 1.35f);
            Color laserColor = Color.White with { A = 0 };

            // 完全模仿 HolyLaser / BaseLaserbeamProjectile 的三段式经典激光渲染
            Rectangle startFrame = startTex.Frame();
            Rectangle middleFrame = midTex.Frame();
            Rectangle endFrame = endTex.Frame();
            Main.EntitySpriteDraw(startTex, drawHeadPos, startFrame, laserColor, Projectile.rotation, startTex.Size() * 0.5f, drawScale, SpriteEffects.None, 0);
            float remaining = BeamLength - (startFrame.Height * 0.5f + endFrame.Height) * drawScale.Y;
            Vector2 segmentPos = Projectile.Center + beamDirection * startFrame.Height * 0.5f * drawScale.Y;
            float segmentStep = middleFrame.Height * drawScale.Y;
            for (float drawn = 0f; drawn + 1f < remaining;)
            {
                Main.EntitySpriteDraw(midTex, segmentPos - Main.screenPosition, middleFrame, laserColor, Projectile.rotation, midTex.Width * 0.5f * Vector2.UnitX, drawScale, SpriteEffects.None, 0);
                drawn += segmentStep;
                segmentPos += beamDirection * segmentStep;
            }
            Main.EntitySpriteDraw(endTex, segmentPos - Main.screenPosition, endFrame, laserColor, Projectile.rotation, endTex.Frame(1, 1, 0, 0).Top(), drawScale, SpriteEffects.None, 0);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            Vector2 destination = Destination;
            if (destination != Vector2.Zero)
            {
                Vector2 destDrawPos = destination - Main.screenPosition;
                AegisVisuals.DrawRuneSigil(destDrawPos, 94f, Main.GlobalTimeWrappedHourly * 9f, Projectile.scale, new Vector2(1f, 0.5f), 1.3f);
                Main.EntitySpriteDraw(bloom, destDrawPos, null, AegisVisuals.Add(AegisVisuals.Core, Projectile.scale), 0f, bloom.Size() * 0.5f, AegisVisuals.RadiusScale(bloom, 86f * Projectile.scale), SpriteEffects.None, 0);
            }
            Main.EntitySpriteDraw(bloom, drawHeadPos, null, AegisVisuals.Add(AegisVisuals.Core, Projectile.scale), 0f, bloom.Size() * 0.5f, AegisVisuals.RadiusScale(bloom, 54f * Projectile.scale), SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            // PreDraw renders the complete HolyLaser-style beam.
        }
    }
}
