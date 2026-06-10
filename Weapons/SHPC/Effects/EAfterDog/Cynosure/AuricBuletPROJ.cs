using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Cynosure
{
    /// <summary>
    /// Cynosure 的主穿甲弹。飞行特效以 BloomLineSoftEdge 直绘拖尾为主体，
    /// 辅以对数螺旋、傅里叶合成波和利萨如轨道三层数学装饰粒子。
    /// </summary>
    public class CynosureArmorPiercingRound : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/SHPC/Effects/EAfterDog/Cynosure/AuricBuletPROJ";
        public new string LocalizationCategory => "Projectiles.SHPC";

        // 金源主题色
        private static readonly Color AuricCyan = new(30, 146, 255);
        private static readonly Color AuricGold = new(255, 214, 82);
        private static readonly Color AuricWhite = new(220, 240, 255);

        private bool PayloadReleased
        {
            get => Projectile.localAI[0] == 1f;
            set => Projectile.localAI[0] = value ? 1f : 0f;
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 28;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.extraUpdates = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.ArmorPenetration = 100;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Vector3(0.18f, 0.65f, 1f) * 0.72f);

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);
            float age = 300f - Projectile.timeLeft;

            // ═══════════════════════════════════════════════════
            // 装饰层 1：对数螺旋双臂 (Logarithmic Spiral)
            // r = a · e^(b·θ)，两条螺旋臂相位差 π
            // ═══════════════════════════════════════════════════
            {
                float spiralA = 2.8f;
                float spiralB = 0.12f;
                float theta = age * 0.42f;

                for (int arm = 0; arm < 2; arm++)
                {
                    float armPhase = arm * MathHelper.Pi;
                    float r = spiralA * MathF.Exp(spiralB * (theta + armPhase));
                    r = MathHelper.Clamp(r, 0f, 18f); // 限制螺旋半径

                    float spiralAngle = theta + armPhase;
                    Vector2 spiralOffset = forward * MathF.Cos(spiralAngle) * r * 0.35f
                                         + normal * MathF.Sin(spiralAngle) * r;

                    Color spiralColor = arm == 0 ? AuricCyan : AuricGold;
                    Dust spiral = Dust.NewDustPerfect(
                        Projectile.Center + spiralOffset - forward * 6f,
                        DustID.Electric,
                        -forward * 0.8f + normal * MathF.Cos(spiralAngle) * 0.3f,
                        0,
                        spiralColor,
                        0.65f);
                    spiral.noGravity = true;
                }
            }

            // ═══════════════════════════════════════════════════
            // 装饰层 2：傅里叶合成波 (Fourier Composite Wave)
            // y(t) = A₁sin(ω₁t) + A₂sin(2ω₁t + φ₂) + A₃sin(3ω₁t + φ₃)
            // 模拟示波器波形信号
            // ═══════════════════════════════════════════════════
            if (Projectile.numUpdates == 0 && age % 2f < 1f)
            {
                float t = age * 0.28f;
                float w1 = 1f;
                float y = 5.5f * MathF.Sin(w1 * t)
                        + 2.8f * MathF.Sin(2f * w1 * t + 0.7f)
                        + 1.4f * MathF.Sin(3f * w1 * t + 1.3f);

                Vector2 wavePos = Projectile.Center - forward * 14f + normal * y;
                float wavePhase = (t * 0.5f) % 1f;
                Color waveColor = Color.Lerp(AuricWhite, AuricCyan, wavePhase);

                GeneralParticleHandler.SpawnParticle(new GenericSparkle(
                    wavePos,
                    -forward * 0.6f,
                    waveColor,
                    Color.White,
                    0.22f,
                    4,
                    0f,
                    0.55f));
            }

            // ═══════════════════════════════════════════════════
            // 装饰层 3：利萨如图形轨道火花 (Lissajous Orbits)
            // x = A·sin(a·t + δ), y = B·sin(b·t)
            // 频率比 a:b = 3:2 → 经典8字交叉图形
            // ═══════════════════════════════════════════════════
            if (Projectile.numUpdates == 0 && age % 3f < 1f)
            {
                float lissT = age * 0.35f;
                float lissA = 12f;
                float lissB = 8f;

                for (int k = 0; k < 2; k++)
                {
                    float phase = k * MathHelper.Pi * 0.5f;
                    float lx = lissA * MathF.Sin(3f * (lissT + phase) + MathHelper.PiOver4);
                    float ly = lissB * MathF.Sin(2f * (lissT + phase));

                    Vector2 lissPos = Projectile.Center + forward * lx * 0.4f + normal * ly;
                    Color sparkColor = k == 0 ? AuricCyan : AuricGold;

                    GeneralParticleHandler.SpawnParticle(new CritSpark(
                        lissPos,
                        -forward * 1.2f + Main.player[Projectile.owner].velocity,
                        Color.White,
                        sparkColor,
                        0.4f,
                        8));
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 300);
            if (!PayloadReleased)
                ReleasePayload(target.whoAmI);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (!PayloadReleased)
                ReleasePayload(-1);
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            if (!PayloadReleased)
                ReleasePayload(-1);
        }

        private void ReleasePayload(int preferredTarget)
        {
            PayloadReleased = true;
            if (Projectile.owner != Main.myPlayer)
                return;

            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/AuricBulletHit") { Volume = 0.9f, Pitch = -0.12f }, Projectile.Center);
            SpawnImpactSparks();

            int burstDamage = Math.Max(1, (int)(Projectile.damage * 0.72f));
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<CynosureLightningExplosion>(), burstDamage, Projectile.knockBack, Projectile.owner);

            SpawnEllipse(preferredTarget, ellipseGroup: 0, count: 12);
            SpawnEllipse(preferredTarget, ellipseGroup: 1, count: 18);

            const int chargedCount = 12;
            for (int i = 0; i < chargedCount; i++)
            {
                float angle = MathHelper.TwoPi * i / chargedCount;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<CynosureChargedCell>(), Math.Max(1, (int)(Projectile.damage * 0.18f)),
                    Projectile.knockBack, Projectile.owner, preferredTarget, angle);
            }
        }

        private void SpawnEllipse(int preferredTarget, int ellipseGroup, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.TwoPi * i / count;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<CynosureAuricBall>(), Math.Max(1, (int)(Projectile.damage * 0.28f)),
                    Projectile.knockBack, Projectile.owner, preferredTarget, ellipseGroup, angle);
            }
        }

        private void SpawnImpactSparks()
        {
            // 蓝白扩散环
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center, Vector2.Zero,
                Color.Lerp(Color.Cyan, Color.White, 0.35f),
                "CalamityMod/Particles/BloomRing",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.045f, 0.42f, 18));

            // 金色等离子爆
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center, Vector2.Zero,
                AuricGold,
                "CalamityMod/Particles/PlasmaExplosion",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.035f, 0.28f, 16));

            // 辐射状辉光火花
            for (int i = 0; i < 28; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f, 20f);
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center, velocity, false,
                    Main.rand.Next(12, 22),
                    Main.rand.NextFloat(0.04f, 0.09f),
                    Main.rand.NextBool(4) ? Color.White : Color.Cyan,
                    new Vector2(2.4f, 0.55f), true));
            }

            // 玫瑰线图案爆炸火花
            for (int i = 0; i < 36; i++)
            {
                float angle = MathHelper.TwoPi * i / 36f;
                float rose = 0.78f + 0.32f * (float)Math.Sin(angle * 5f);
                Vector2 direction = angle.ToRotationVector2();
                Vector2 velocity = direction * Main.rand.NextFloat(4f, 15f) * rose;
                Color color = Color.Lerp(AuricGold, Color.Cyan, i % 2 == 0 ? 0.25f : 0.65f);
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center + direction * Main.rand.NextFloat(4f, 16f),
                    velocity, false,
                    Main.rand.Next(13, 24),
                    Main.rand.NextFloat(0.035f, 0.08f),
                    color,
                    new Vector2(2.2f, 0.5f), true));
            }
        }

        // ═══════════════════════════════════════════════════
        // Primitive Trail 基底层
        // ═══════════════════════════════════════════════════
        internal float TrailWidth(float completion, Vector2 _) => MathHelper.Lerp(14f, 0f, completion);

        internal Color TrailColor(float completion, Vector2 _)
        {
            Color color = Color.Lerp(AuricWhite, AuricCyan, completion * 0.7f);
            color = Color.Lerp(color, AuricGold, completion * completion);
            return color * (1f - completion * 0.85f);
        }

        // ═══════════════════════════════════════════════════
        // PreDraw：主体 BloomLineSoftEdge 直绘拖尾 + 弹芯辉光
        // ═══════════════════════════════════════════════════
        public override bool PreDraw(ref Color lightColor)
        {
            // --- 底层：Primitive Renderer 半透明彗尾 ---
            GameShaders.Misc["CalamityMod:OverpoweredTouhouSpearShader"]
                .SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));
            PrimitiveRenderer.RenderTrail(Projectile.oldPos,
                new PrimitiveSettings(TrailWidth, TrailColor, (_, _) => Projectile.Size * 0.5f,
                    shader: GameShaders.Misc["CalamityMod:OverpoweredTouhouSpearShader"]),
                42);

            // --- 主体层：BloomLineSoftEdge 直绘纤细发光拖尾 ---
            Texture2D bloomLine = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineSoftEdge").Value;
            Texture2D bloomCircle = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 bloomLineOrigin = bloomLine.Size() * 0.5f;
            Vector2 bloomCircleOrigin = bloomCircle.Size() * 0.5f;

            float velocityRotation = Projectile.velocity.ToRotation();
            float pulse = 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 12f);
            int trailLength = Projectile.oldPos.Length;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            // 主拖尾：遍历 oldPos，每个历史点绘制一条纤细的 BloomLineSoftEdge
            for (int i = trailLength - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = i / (float)trailLength;
                float inverseFade = 1f - completion;
                Vector2 trailPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;

                // 颜色渐变：白芯 → 蓝中 → 金尾
                Color lineColor;
                if (completion < 0.35f)
                    lineColor = Color.Lerp(AuricWhite, AuricCyan, completion / 0.35f);
                else
                    lineColor = Color.Lerp(AuricCyan, AuricGold, (completion - 0.35f) / 0.65f);
                lineColor.A = 0;

                float lineOpacity = inverseFade * 0.42f;
                float xScale = 0.016f + 0.004f * pulse * inverseFade;
                float yScale = 0.078f * inverseFade;

                // 主线
                Main.EntitySpriteDraw(
                    bloomLine, trailPosition, null,
                    lineColor * lineOpacity,
                    velocityRotation + MathHelper.PiOver2,
                    bloomLineOrigin,
                    new Vector2(xScale, yScale),
                    SpriteEffects.None, 0);

                // 偏移副线（双螺旋式装饰，正弦偏移产生DNA双链感）
                if (i % 2 == 0)
                {
                    float helixOffset = MathF.Sin(completion * MathHelper.TwoPi * 2.5f + Main.GlobalTimeWrappedHourly * 4f) * 4f * inverseFade;
                    Vector2 helixNormal = (velocityRotation + MathHelper.PiOver2).ToRotationVector2();
                    Vector2 offsetPos = trailPosition + helixNormal * helixOffset;
                    Color helixColor = Color.Lerp(AuricGold, AuricCyan, completion);
                    helixColor.A = 0;

                    Main.EntitySpriteDraw(
                        bloomLine, offsetPos, null,
                        helixColor * (lineOpacity * 0.55f),
                        velocityRotation + MathHelper.PiOver2,
                        bloomLineOrigin,
                        new Vector2(xScale * 0.6f, yScale * 0.7f),
                        SpriteEffects.None, 0);
                }
            }

            // --- 弹芯辉光 ---
            Vector2 corePos = Projectile.Center - Main.screenPosition;

            // 外层柔辉
            Main.EntitySpriteDraw(
                bloomCircle, corePos, null,
                AuricCyan with { A = 0 } * (0.28f + 0.08f * pulse),
                0f, bloomCircleOrigin,
                0.22f + 0.03f * pulse,
                SpriteEffects.None, 0);

            // 内层白芯
            Main.EntitySpriteDraw(
                bloomCircle, corePos, null,
                (Color.White with { A = 0 }) * 0.38f,
                0f, bloomCircleOrigin,
                0.08f,
                SpriteEffects.None, 0);

            // 十字星芒（4条极细 BloomLineSoftEdge 呈十字排列）
            for (int i = 0; i < 4; i++)
            {
                float starRot = velocityRotation + MathHelper.PiOver4 * i + Main.GlobalTimeWrappedHourly * 2.2f;
                Color starColor = i % 2 == 0 ? AuricCyan : AuricGold;
                starColor.A = 0;

                Main.EntitySpriteDraw(
                    bloomLine, corePos, null,
                    starColor * (0.25f + 0.08f * pulse),
                    starRot,
                    bloomLineOrigin,
                    new Vector2(0.008f, 0.12f + 0.02f * pulse),
                    SpriteEffects.None, 0);
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            // --- 弹丸本体 ---
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                Color.White,
                Projectile.rotation,
                texture.Size() * 0.5f,
                Projectile.scale,
                SpriteEffects.None);

            return false;
        }
    }
}
