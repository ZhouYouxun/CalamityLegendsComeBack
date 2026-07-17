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
    /// </summary>damage
    public class CynosureArmorPiercingRound : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityMod/Projectiles/Typeless/ExoTankMissile";
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
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            Main.projFrames[Type] = 4;
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
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Type];
            Lighting.AddLight(Projectile.Center, new Vector3(0.18f, 0.65f, 1f) * 0.72f);

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);
            float age = 300f - Projectile.timeLeft;

            // ═══════════════════════════════════════════════════
            // 装饰层 1：对数螺旋双臂 (Logarithmic Spiral)
            // r = a · e^(b·θ)，两条螺旋臂相位差 π
            // ═══════════════════════════════════════════════════
            if (Projectile.numUpdates == 0)
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

            SpawnExtremeFlightEffects(age, forward, normal);
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

            int burstDamage = Math.Max(1, (int)(Projectile.damage * 0.95f));
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<CynosureLightningExplosion>(), burstDamage, Projectile.knockBack, Projectile.owner);

            SpawnEllipse(preferredTarget, ellipseGroup: 0, count: 30);
            SpawnEllipse(preferredTarget, ellipseGroup: 1, count: 30);

            const int chargedCount = 12;
            for (int i = 0; i < chargedCount; i++)
            {
                float angle = MathHelper.TwoPi * i / chargedCount;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<CynosureChargedCell>(), Math.Max(1, (int)(Projectile.damage * 0.32f)),
                    Projectile.knockBack, Projectile.owner, preferredTarget, angle);
            }
        }

        private void SpawnEllipse(int preferredTarget, int ellipseGroup, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.TwoPi * i / count;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<CynosureAuricBall>(), Math.Max(1, (int)(Projectile.damage * 0.48f)),
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

            CynosureVisuals.SpawnElectricBurst(Projectile.Center, 16, 4.5f, 22f);
            CynosureVisuals.SpawnScarletStyleBurst(Projectile.Center, 8, 10f, 18f, 1.0f);

            // 辐射状辉光火花
            for (int i = 0; i < 8; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f, 20f);
                Color color = Main.rand.NextBool(4) ? Color.White : Color.Cyan;
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    Projectile.Center,
                    velocity,
                    false,
                    Main.rand.Next(12, 22),
                    Main.rand.NextFloat(0.32f, 0.72f),
                    color));
            }

            // 玫瑰线图案爆炸火花
            for (int i = 0; i < 18; i++)
            {
                float angle = MathHelper.TwoPi * i / 72f;
                float rose = 0.78f + 0.32f * MathF.Sin(angle * 5f);
                Vector2 direction = angle.ToRotationVector2();
                Vector2 velocity = direction * Main.rand.NextFloat(4f, 17f) * rose;
                Color color = Color.Lerp(AuricGold, Color.Cyan, i % 2 == 0 ? 0.25f : 0.65f);
                GeneralParticleHandler.SpawnParticle(new CritSpark(
                    Projectile.Center + direction * Main.rand.NextFloat(4f, 16f),
                    velocity,
                    Color.White,
                    color,
                    Main.rand.NextFloat(0.32f, 0.64f),
                    Main.rand.Next(13, 22)));

                if (i % 6 == 0)
                {
                    Dust dust = Dust.NewDustPerfect(
                        Projectile.Center + direction * Main.rand.NextFloat(10f, 34f),
                        DustID.Electric,
                        direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(1.4f, 5.6f) + velocity * 0.12f,
                        0,
                        color,
                        Main.rand.NextFloat(0.92f, 1.55f));
                    dust.noGravity = true;
                }
            }
        }

        // ═══════════════════════════════════════════════════
        // Primitive Trail 基底层
        // ═══════════════════════════════════════════════════
        private void SpawnExtremeFlightEffects(float age, Vector2 forward, Vector2 normal)
        {
            if (Main.dedServ)
                return;

            float time = age * 0.18f + Projectile.identity * 0.071f;
            float speedPower = Utils.GetLerpValue(12f, 46f, Projectile.velocity.Length(), true);
            float pulse = 0.5f + 0.5f * MathF.Sin(time * 3.7f);
            Color blue = Color.Lerp(AuricCyan, AuricWhite, 0.24f + pulse * 0.32f);
            Color gold = Color.Lerp(AuricGold, Color.White, 0.12f + pulse * 0.22f);

            if (Projectile.numUpdates == 0)
            {
                for (int lane = -1; lane <= 1; lane += 2)
                {
                    float corkscrew = MathF.Sin(time * 5.1f + lane * MathHelper.PiOver2);
                    Vector2 lanePosition = Projectile.Center - forward * Main.rand.NextFloat(8f, 30f) + normal * lane * corkscrew * Main.rand.NextFloat(8f, 24f);
                    Vector2 laneVelocity = -forward * Main.rand.NextFloat(3.5f, 9f + speedPower * 8f) + normal * lane * Main.rand.NextFloat(0.8f, 3.2f);
                    GeneralParticleHandler.SpawnParticle(new LineParticle(
                        lanePosition,
                        laneVelocity,
                        false,
                        Main.rand.Next(10, 18),
                        Main.rand.NextFloat(0.28f, 0.62f),
                        lane > 0 ? blue : gold));
                }
            }

            if (Projectile.numUpdates == 0 && Main.rand.NextBool(4))
            {
                float petalAngle = time * 2.6f + Main.rand.NextFloat(MathHelper.TwoPi);
                float petalRadius = Main.rand.NextFloat(5f, 26f) * (0.65f + speedPower * 0.7f);
                Vector2 petalOffset = forward * MathF.Cos(petalAngle * 2f) * petalRadius * 0.24f + normal * MathF.Sin(petalAngle * 3f) * petalRadius;
                GeneralParticleHandler.SpawnParticle(new CritSpark(
                    Projectile.Center + petalOffset,
                    -forward * Main.rand.NextFloat(1.6f, 5.8f) - petalOffset.SafeNormalize(Vector2.Zero) * 0.4f,
                    Color.White,
                    Main.rand.NextBool(3) ? gold : blue,
                    Main.rand.NextFloat(0.22f, 0.44f),
                    Main.rand.Next(8, 14)));
            }

            if (Projectile.numUpdates == 0 && age % 8f < 1f)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    Projectile.Center - forward * 10f,
                    -forward * 1.1f,
                    Color.Lerp(blue, gold, pulse) * 0.58f,
                    new Vector2(0.48f, 1.75f + speedPower * 0.65f),
                    Projectile.rotation,
                    0.12f + speedPower * 0.035f,
                    0.018f,
                    12));
            }

            if (Projectile.numUpdates != 0 || !Main.rand.NextBool(2))
                return;

            for (int i = 0; i < 1; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - forward * Main.rand.NextFloat(2f, 18f) + normal * Main.rand.NextFloat(-9f, 9f),
                    Main.rand.NextBool(3) ? DustID.GoldCoin : DustID.Electric,
                    -forward * Main.rand.NextFloat(0.8f, 2.6f) + normal * Main.rand.NextFloat(-0.7f, 0.7f),
                    0,
                    Main.rand.NextBool(3) ? gold : blue,
                    Main.rand.NextFloat(0.56f, 0.96f));
                dust.noGravity = true;
            }
        }

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
                18);

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
            for (int i = trailLength - 1; i >= 0; i -= 3)
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
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 textureOrigin = frame.Size() * 0.5f;
            float outlinePulse = 1f + 0.18f * MathF.Sin(Main.GlobalTimeWrappedHourly * 18f + Projectile.identity * 0.37f);
            Color outlineColor = AuricGold with { A = 0 };

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (int i = 0; i < 4; i++)
            {
                float angle = MathHelper.TwoPi * i / 4f;
                Vector2 offset = angle.ToRotationVector2() * (2.2f + outlinePulse);
                Main.EntitySpriteDraw(
                    texture,
                    Projectile.Center + offset - Main.screenPosition,
                    frame,
                    outlineColor * 0.42f,
                    Projectile.rotation,
                    textureOrigin,
                    Projectile.scale * 1.06f,
                    SpriteEffects.None);
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                frame,
                Color.White,
                Projectile.rotation,
                textureOrigin,
                Projectile.scale,
                SpriteEffects.None);

            return false;
        }
    }
}
