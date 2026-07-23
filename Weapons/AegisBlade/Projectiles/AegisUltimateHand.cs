using System;
using CalamityLegendsComeBack.Weapons.AegisBlade.Visuals;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.AegisBlade.Projectiles
{
    // 终结技：神手沿玩家相对坐标下降，蓄力后握住玩家，赋予10秒无敌+70%减速。
    //
    // 视觉重做说明：旧版这里只有两个 BloomCircle 加一句"贴图待补充"，
    // 一个传奇终结技等于没有画面。既然没有神手贴图，就用绘制把它造出来：
    // 掌心（日核 + 护罩壳）、五根由火焰喷流贴图分节拼成的手指、通天的手腕光柱、
    // 地面符文圣印，四个部件按「张开 → 合拢 → 握住」三段状态联动。
    public class AegisUltimateHand : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private Player Owner => Main.player[Projectile.owner];

        private const int StateApproach = 0;
        private const int StateChargeUp = 1;
        private const int StateGripping = 2;

        private const int ApproachTime = 76;
        private const int ChargeUpTime = 28;

        private ref float State  => ref Projectile.ai[0];
        private ref float Timer  => ref Projectile.ai[1];

        private float relativeYOffset = -520f;
        private bool initialized = false;

        // ── 手部构造参数 ────────────────────────────────────────────────
        // 掌心朝下，五指从掌缘垂下。角度以「正下方」为基准左右分布，
        // 拇指（第 0 根）最短、中指（第 2 根）最长，比例照实际手型。
        private static readonly float[] FingerSpread = { -1.08f, -0.55f, 0f, 0.55f, 1.08f };
        private static readonly float[] FingerLength = { 0.70f, 0.93f, 1f, 0.90f, 0.68f };
        private const int FingerSegments = 3;
        private const float PalmRadius = 46f;
        private const float FingerReach = 104f;

        /// <summary>0 = 五指张开，1 = 完全握拢。</summary>
        private float GripClose
        {
            get
            {
                return (int)State switch
                {
                    StateApproach => Utils.GetLerpValue(0f, ApproachTime, Timer, true) * 0.28f,
                    StateChargeUp => MathHelper.Lerp(0.28f, 1f, Utils.GetLerpValue(0f, ChargeUpTime, Timer, true)),
                    _ => 1f,
                };
            }
        }

        /// <summary>握持阶段的剩余强度：终结技快结束时整只手逐渐淡去。</summary>
        private float GripFade
        {
            get
            {
                if ((int)State != StateGripping)
                    return 1f;
                AegisBladePlayer bladePlayer = Owner.GetModPlayer<AegisBladePlayer>();
                if (!bladePlayer.UltimateActive)
                    return 0f;
                return Utils.GetLerpValue(0f, 48f, bladePlayer.UltimateTimer, true) *
                       Utils.GetLerpValue(0f, 10f, Timer, true);
            }
        }

        private Vector2 PalmCenter => Projectile.Center - Vector2.UnitY * 40f;

        public override void SetDefaults()
        {
            Projectile.width  = Projectile.height = 80;
            Projectile.friendly    = false;
            Projectile.tileCollide = false;
            Projectile.penetrate   = -1;
            Projectile.timeLeft    = 80 + BalanceAegisBlade.UltimateDuration + ApproachTime + ChargeUpTime;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (!Owner.active || Owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (!initialized)
            {
                relativeYOffset = MathHelper.Clamp(Projectile.Center.Y - Owner.Center.Y, -680f, -260f);
                initialized = true;
            }

            Timer++;

            switch ((int)State)
            {
                case StateApproach: DoApproach(); break;
                case StateChargeUp: DoChargeUp(); break;
                case StateGripping: DoGripping(); break;
            }

            AegisVisuals.Light(PalmCenter, 1.5f);
            AegisVisuals.Light(Owner.Center, 0.9f);
        }

        // ── 接近阶段：从高空快速飞向玩家 ────────────────────────────────

        private void DoApproach()
        {
            float progress = MathHelper.Clamp(Timer / ApproachTime, 0f, 1f);
            float eased = MathHelper.SmoothStep(0f, 1f, progress);
            relativeYOffset = MathHelper.Lerp(relativeYOffset, MathHelper.Lerp(-520f, 0f, eased), 0.18f);
            Projectile.Center = Owner.Center + new Vector2(0f, relativeYOffset);

            if (!Main.dedServ)
            {
                EmitConvergingSpiral(Owner.Center, progress, false);

                // 圣印在玩家脚下被"烙"出来的过程中，火星从四周被吸进来
                if (Main.rand.NextBool(2))
                {
                    AegisVisuals.WarbannerConverge(Owner.Center,
                        Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2(),
                        0.8f + progress * 1.4f, 2, 1.1f);
                }

                // 掌心边缘持续掉落的火屑
                if (Main.rand.NextBool(2))
                    AegisVisuals.EmberDrip(PalmCenter, PalmRadius * 0.9f, 12f, 1.2f);

                AegisVisuals.Screenshake(Owner.Center, MathHelper.Lerp(0f, 2.2f, progress), 1400f);
            }

            if (Timer >= ApproachTime)
            {
                State = StateChargeUp;
                Timer = 0;
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.9f, Pitch = -0.3f }, Owner.Center);
            }
        }

        // ── 蓄力阶段：震颤 + 吸入粒子 ───────────────────────────────────

        private void DoChargeUp()
        {
            Projectile.Center = Owner.Center;

            if (!Main.dedServ)
            {
                float progress = MathHelper.Clamp(Timer / ChargeUpTime, 0f, 1f);
                EmitConvergingSpiral(Owner.Center, progress, true);

                // 五指合拢的过程中，指缝里被挤出来的火
                if (Main.rand.NextBool(2))
                {
                    int finger = Main.rand.Next(FingerSpread.Length);
                    float angle = MathHelper.PiOver2 + FingerSpread[finger] * (1f - GripClose * 0.6f);
                    Vector2 spawn = PalmCenter + angle.ToRotationVector2() * Main.rand.NextFloat(50f, 130f);
                    AegisVisuals.EmberJet(spawn, angle.ToRotationVector2().RotatedBy(MathHelper.PiOver2), 2,
                        0.6f + progress * 0.5f, 0.9f);
                }

                AegisVisuals.Screenshake(Owner.Center, MathHelper.Lerp(2.2f, 5.5f, progress), 1400f);
            }

            if (Timer >= ChargeUpTime)
            {
                State = StateGripping;
                Timer = 0;
                Owner.GetModPlayer<AegisBladePlayer>().ActivateUltimate();
                SoundEngine.PlaySound(SoundID.Item67 with { Volume = 1f, Pitch = 0.2f }, Owner.Center);
                SoundEngine.PlaySound(SoundID.DD2_BetsyFireballImpact with { Volume = 0.9f, Pitch = -0.35f }, Owner.Center);
                OnGripSnap();
            }
        }

        /// <summary>握拢的那一刻：整套视觉里最强的一次爆发。</summary>
        private void OnGripSnap()
        {
            if (Main.dedServ)
                return;

            AegisVisuals.HolyDetonation(Owner.Center, 4.2f);
            AegisVisuals.CoronaRing(Owner.Center, 28, 2.1f);
            AegisVisuals.CoronaRing(Owner.Center, 20, 1.3f, 0.16f);

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Owner.Center, Vector2.Zero, AegisVisuals.Add(AegisVisuals.Core, 1f),
                Vector2.One, 0f, 0.06f, 2.2f, 22));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Owner.Center, Vector2.Zero, AegisVisuals.Add(AegisVisuals.Gold, 0.95f),
                Vector2.One, 0f, 0.08f, 3.4f, 34));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Owner.Center, Vector2.Zero, AegisVisuals.Add(AegisVisuals.Ember, 0.9f),
                Vector2.One, 0f, 0.1f, 4.4f, 44));

            // 十六向火焰喷流：手指合拢时把周围的空气都点着了
            for (int i = 0; i < 16; i++)
            {
                float angle = MathHelper.TwoPi * i / 16f;
                AegisVisuals.EmberJet(Owner.Center + angle.ToRotationVector2() * 30f,
                    angle.ToRotationVector2(), 3, 1.5f, 0.22f);
            }

            for (int i = 0; i < 20; i++)
            {
                float angle = MathHelper.TwoPi * i / 20f;
                GeneralParticleHandler.SpawnParticle(new SparkleParticle(
                    Owner.Center + angle.ToRotationVector2() * 62f,
                    angle.ToRotationVector2() * 2.2f,
                    AegisVisuals.Add(AegisVisuals.Core, 1f), AegisVisuals.Add(AegisVisuals.Flame, 1f),
                    1.4f, 22, 0.05f, 2.2f));
            }

            AegisVisuals.Screenshake(Owner.Center, 9f, 2000f);
        }

        // ── 握持阶段：罩住玩家 ───────────────────────────────────────────

        private void DoGripping()
        {
            Projectile.Center = Owner.Center;

            if (!Main.dedServ)
            {
                if (Main.rand.NextBool(3))
                    EmitGripFilaments();

                // 掌罩内部的余烬雨：玩家被罩在一片持续燃烧的圣火里
                if (Main.rand.NextBool(2))
                    AegisVisuals.EmberDrip(Owner.Center - Vector2.UnitY * 46f, 40f, 16f, 1.1f);

                // 指缝间隔性喷火，让"握住"这个状态不是一张静止图
                if (Timer % 24 == 0)
                {
                    int finger = (int)(Timer / 24) % FingerSpread.Length;
                    float angle = MathHelper.PiOver2 + FingerSpread[finger] * 0.42f;
                    Vector2 tip = PalmCenter + angle.ToRotationVector2() * FingerReach * 1.1f;
                    AegisVisuals.EmberJet(tip, (angle + MathHelper.Pi).ToRotationVector2(), 4, 0.75f, 0.6f);
                    GeneralParticleHandler.SpawnParticle(new CustomPulse(tip, Vector2.Zero,
                        AegisVisuals.Add(AegisVisuals.Gold, 0.6f), AegisVisuals.TexBloom, Vector2.One,
                        0f, 0.05f, 0.42f, 12));
                }
            }

            if (!Owner.GetModPlayer<AegisBladePlayer>().UltimateActive)
                Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            // 松手：整只手化为余烬向上散去
            AegisVisuals.HolyDetonation(Owner.Center, 1.8f);
            AegisVisuals.CoronaRing(Owner.Center, 14, 1.1f);
            for (int i = 0; i < 26; i++)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Owner.Center + Main.rand.NextVector2Circular(52f, 60f),
                    new Vector2(Main.rand.NextFloat(-1.4f, 1.4f), -Main.rand.NextFloat(1.5f, 5f)),
                    false, Main.rand.Next(24, 42), Main.rand.NextFloat(0.2f, 0.42f),
                    AegisVisuals.RandomFlameColor(), true, false, true));
            }
        }

        private void EmitConvergingSpiral(Vector2 center, float progress, bool tight)
        {
            int count = tight ? 5 : 3;
            float baseRadius = tight ? MathHelper.Lerp(96f, 18f, progress) : MathHelper.Lerp(170f, 38f, progress);
            float spin = Main.GlobalTimeWrappedHourly * (tight ? 5.8f : 3.6f);
            for (int i = 0; i < count; i++)
            {
                float t = (Timer * 0.11f + i / (float)count) % 1f;
                float angle = spin + MathHelper.TwoPi * i / count + t * MathHelper.TwoPi * 1.618f;
                float radius = baseRadius * (1f - t * 0.55f);
                Vector2 offset = angle.ToRotationVector2() * radius;
                Vector2 tangent = offset.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2);
                Vector2 velocity = -offset.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(1.4f, tight ? 7.5f : 5.2f, progress) + tangent * (tight ? 1.2f : 0.7f);

                Dust dust = Dust.NewDustPerfect(center + offset, AegisVisuals.ProfanedFireDust, velocity,
                    0, Color.White, tight ? 1.35f : 1.05f);
                dust.noGravity = true;
                dust.fadeIn = 0.8f + progress * 0.5f;

                if (Main.rand.NextBool(3))
                {
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(center + offset, velocity * 0.7f,
                        false, Main.rand.Next(14, 24), Main.rand.NextFloat(0.14f, 0.28f),
                        AegisVisuals.RandomFlameColor(), true, false, true));
                }
            }
        }

        private void EmitGripFilaments()
        {
            float angle = Main.GlobalTimeWrappedHourly * 4.2f + Main.rand.NextFloat(MathHelper.TwoPi);
            float radius = Main.rand.NextFloat(28f, 62f);
            Vector2 offset = angle.ToRotationVector2() * radius;
            Vector2 velocity = -offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(1.4f, 3.6f);

            Dust dust = Dust.NewDustPerfect(Owner.Center + offset, AegisVisuals.ProfanedFireDust, velocity,
                0, Color.White, Main.rand.NextFloat(0.85f, 1.4f));
            dust.noGravity = true;
            dust.fadeIn = 0.9f;

            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(Owner.Center + offset, velocity,
                false, Main.rand.Next(14, 22), Main.rand.NextFloat(0.12f, 0.24f),
                AegisVisuals.RandomFlameColor(), true, false, true));
        }

        // ────────────────────────────────────────────────────────────────
        // 绘制：把一只神手用贴图拼出来
        // ────────────────────────────────────────────────────────────────

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            float overall = GripFade * ((int)State == StateApproach
                ? Utils.GetLerpValue(0f, 18f, Timer, true)
                : 1f);
            if (overall <= 0.01f)
                return false;

            Vector2 palm = PalmCenter - Main.screenPosition;
            float close = GripClose;

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);

            DrawGroundSigil(overall, close);
            DrawWrist(palm, overall);
            DrawFingers(palm, overall, close);
            DrawPalm(palm, overall, close);
            DrawGripDome(overall);

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        /// <summary>玩家脚下的巨型符文圣印。接近时展开，蓄力时急转，握住后稳定慢转。</summary>
        private void DrawGroundSigil(float overall, float close)
        {
            float radius = (int)State switch
            {
                StateApproach => MathHelper.Lerp(60f, 190f, Utils.GetLerpValue(0f, ApproachTime, Timer, true)),
                StateChargeUp => MathHelper.Lerp(190f, 150f, Utils.GetLerpValue(0f, ChargeUpTime, Timer, true)),
                _ => 150f + 8f * MathF.Sin(Main.GlobalTimeWrappedHourly * 1.6f),
            };

            float spin = (int)State switch
            {
                StateApproach => Main.GlobalTimeWrappedHourly * 1.1f,
                StateChargeUp => Main.GlobalTimeWrappedHourly * (1.1f + close * 8f),
                _ => Main.GlobalTimeWrappedHourly * 1.4f,
            };

            Vector2 sigilPosition = Owner.Center - Main.screenPosition + Vector2.UnitY * 26f;
            AegisVisuals.DrawRuneSigil(sigilPosition, radius, spin, overall * 0.85f,
                new Vector2(1f, 0.36f), 1.1f);
        }

        /// <summary>手腕：从掌背向上收束的光柱，暗示上方还有更大的东西。</summary>
        private void DrawWrist(Vector2 palm, float overall)
        {
            Texture2D beam = AegisVisuals.Tex(AegisVisuals.TexBeamLine);
            Texture2D radiance = AegisVisuals.Tex(AegisVisuals.TexRadianceSoft);

            // 接近阶段光柱最长（从天而降），握住后收成短短一截手腕
            float length = (int)State == StateApproach
                ? MathHelper.Lerp(900f, 260f, Utils.GetLerpValue(0f, ApproachTime, Timer, true))
                : 220f;

            Vector2 wristCenter = palm - Vector2.UnitY * (length * 0.5f + PalmRadius * 0.4f);

            Main.EntitySpriteDraw(beam, wristCenter, null,
                AegisVisuals.Add(AegisVisuals.Ember, 0.4f * overall),
                0f, beam.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(beam, PalmRadius * 1.35f),
                            AegisVisuals.RadiusScale(beam, length * 0.5f)),
                SpriteEffects.None, 0);
            Main.EntitySpriteDraw(beam, wristCenter, null,
                AegisVisuals.Add(AegisVisuals.Gold, 0.34f * overall),
                0f, beam.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(beam, PalmRadius * 0.78f),
                            AegisVisuals.RadiusScale(beam, length * 0.5f)),
                SpriteEffects.None, 0);
            Main.EntitySpriteDraw(beam, wristCenter, null,
                AegisVisuals.Add(AegisVisuals.Core, 0.22f * overall),
                0f, beam.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(beam, PalmRadius * 0.3f),
                            AegisVisuals.RadiusScale(beam, length * 0.5f)),
                SpriteEffects.None, 0);

            // 掌背的放射光芒
            Main.EntitySpriteDraw(radiance, palm, null,
                AegisVisuals.Add(AegisVisuals.Flame, 0.22f * overall),
                Main.GlobalTimeWrappedHourly * 0.5f, radiance.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(radiance, PalmRadius * 3.2f)), SpriteEffects.None, 0);
        }

        /// <summary>
        /// 五指。每根手指由 3 节火焰喷流首尾相接，每一节都比上一节更细更短，
        /// 并按 curl 逐节内扣 —— 于是「张开」和「握拢」是同一套结构的两个极值。
        /// </summary>
        private void DrawFingers(Vector2 palm, float overall, float close)
        {
            Texture2D plume = AegisVisuals.Tex(AegisVisuals.TexJet);
            Texture2D claw = AegisVisuals.Tex(AegisVisuals.TexStarPinch);

            float curl = MathHelper.Lerp(0.06f, 0.46f, close);
            float spreadScale = MathHelper.Lerp(1f, 0.46f, close);
            float breathe = 1f + 0.035f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2.2f);

            for (int f = 0; f < FingerSpread.Length; f++)
            {
                float baseAngle = MathHelper.PiOver2 + FingerSpread[f] * spreadScale;
                float totalLength = FingerReach * FingerLength[f] * breathe;
                Vector2 anchor = palm + baseAngle.ToRotationVector2() * (PalmRadius * 0.82f);
                float angle = baseAngle;
                // 屏幕坐标下正下方是 PiOver2：偏角为负的手指在右侧，内扣需要角度递增；
                // 偏角为正的手指在左侧，内扣需要角度递减。
                float curlSign = FingerSpread[f] <= 0f ? 1f : -1f;

                for (int s = 0; s < FingerSegments; s++)
                {
                    float segmentLength = totalLength * (0.42f - s * 0.09f);
                    float segmentWidth = PalmRadius * (0.32f - s * 0.06f);

                    // 外焰 → 主焰 → 白芯，三层压在同一节上
                    DrawFingerSegment(plume, anchor, angle, segmentLength * 1.08f, segmentWidth * 1.5f,
                        AegisVisuals.Add(AegisVisuals.Ember, 0.42f * overall));
                    DrawFingerSegment(plume, anchor, angle, segmentLength, segmentWidth,
                        AegisVisuals.Add(AegisVisuals.Gold, 0.55f * overall));
                    DrawFingerSegment(plume, anchor, angle, segmentLength * 0.82f, segmentWidth * 0.42f,
                        AegisVisuals.Add(AegisVisuals.Core, 0.42f * overall));

                    anchor += angle.ToRotationVector2() * segmentLength;
                    angle += curl * curlSign;
                }

                // 指尖：一枚收腰星芒当作"爪"
                Main.EntitySpriteDraw(claw, anchor, null,
                    AegisVisuals.Add(AegisVisuals.Core, 0.5f * overall),
                    angle + Main.GlobalTimeWrappedHourly * 1.6f, claw.Size() * 0.5f,
                    new Vector2(AegisVisuals.RadiusScale(claw, 15f)), SpriteEffects.None, 0);
            }
        }

        private static void DrawFingerSegment(Texture2D plume, Vector2 anchor, float angle, float length,
            float width, Color color)
        {
            // muzzle_04 是竖直向上的火焰，原点取底边中点，于是它从 anchor 沿 angle 方向长出去。
            Main.EntitySpriteDraw(plume, anchor, null, color, angle + MathHelper.PiOver2,
                new Vector2(plume.Width * 0.5f, plume.Height),
                new Vector2(width * 2f / plume.Width, length / plume.Height),
                SpriteEffects.None, 0);
        }

        /// <summary>掌心：日核 + 护罩壳。握拢时收缩变亮。</summary>
        private void DrawPalm(Vector2 palm, float overall, float close)
        {
            float radius = PalmRadius * MathHelper.Lerp(1.15f, 0.9f, close);
            float brightness = overall * MathHelper.Lerp(0.85f, 1.35f, close);

            AegisVisuals.DrawSolarCore(palm, radius, brightness,
                Main.GlobalTimeWrappedHourly * 2.2f, new Vector2(1.18f, 0.92f));

            Texture2D shell = AegisVisuals.Tex(AegisVisuals.TexBarrierShell);
            Main.EntitySpriteDraw(shell, palm, null,
                AegisVisuals.Add(AegisVisuals.Ember, 0.35f * overall),
                Main.GlobalTimeWrappedHourly * 0.4f, shell.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(shell, radius * 1.5f),
                            AegisVisuals.RadiusScale(shell, radius * 1.1f)),
                SpriteEffects.None, 0);
        }

        /// <summary>握持期间罩住玩家的圣火护罩，只在 StateGripping 出现。</summary>
        private void DrawGripDome(float overall)
        {
            if ((int)State != StateGripping)
                return;

            Texture2D shell = AegisVisuals.Tex(AegisVisuals.TexBarrierShell);
            Texture2D bloom = AegisVisuals.Tex(AegisVisuals.TexBloom);
            Vector2 center = Owner.Center - Main.screenPosition;
            float pulse = 0.82f + 0.18f * MathF.Sin(Main.GlobalTimeWrappedHourly * 3.2f);

            Main.EntitySpriteDraw(bloom, center, null,
                AegisVisuals.Add(AegisVisuals.Ember, 0.3f * overall * pulse),
                0f, bloom.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(bloom, 82f)), SpriteEffects.None, 0);

            Main.EntitySpriteDraw(shell, center, null,
                AegisVisuals.Add(AegisVisuals.Gold, 0.34f * overall * pulse),
                Main.GlobalTimeWrappedHourly * 0.6f, shell.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(shell, 66f)), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(shell, center, null,
                AegisVisuals.Add(AegisVisuals.Core, 0.2f * overall * pulse),
                -Main.GlobalTimeWrappedHourly * 0.85f, shell.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(shell, 52f)), SpriteEffects.None, 0);
        }
    }
}
