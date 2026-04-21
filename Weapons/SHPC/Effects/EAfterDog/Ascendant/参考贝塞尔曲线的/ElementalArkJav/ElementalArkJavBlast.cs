using System;
using System.IO;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.Particles;
using CalamityMod.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.CalamityUtils;
using static Terraria.ModLoader.ModContent;
using CalamityMod;
using Terraria.DataStructures;

namespace CalamityThrowingSpear.Weapons.NewWeapons.DPreDog.ElementalArkJav
{
    public class ElementalArkJavBlast : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.NewWeapons.DPreDog";
        //public override string Texture => "CalamityMod/Projectiles/Melee/RendingScissorsRight"; //Umm actually the rending scissors are for aote mr programmer what the hel.. it gets changed in predraw anywyas
        // 透明贴图，取消剪刀的可见部分
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private bool initialized = false; // 标记是否已经初始化

        // 充能量引用
        public ref float Charge => ref Projectile.ai[0];

        // 标记是否处于冲刺状态
        public bool Dashing
        {
            get => Projectile.ai[1] == 1;
            set => Projectile.ai[1] = value ? 1f : 0f;
        }

        // 最大缝线数量，用于模拟撕裂的效果
        const int maxStitches = 8;

        // 当前撕裂路径中的缝线数量，根据撕裂进度计算
        public int CurrentStitches => (int)Math.Ceiling((1 - (float)Math.Sqrt(1f - (float)Math.Pow(MathHelper.Clamp(StitchProgress * 3f, 0f, 1f), 2f))) * maxStitches);

        // 缝线的旋转角度和生命周期
        public float[] StitchRotations = new float[maxStitches];
        public float[] StitchLifetimes = new float[maxStitches];

        // 撕裂的动画时间参数
        const float MaxTime = 70;
        const float SnapTime = 25f;
        const float HoldTime = 15f;

        // 动画计时器，用于控制撕裂动画的不同阶段
        public float SnapTimer => MaxTime - Projectile.timeLeft;
        public float HoldTimer => MaxTime - Projectile.timeLeft - SnapTime;
        public float StitchTimer => MaxTime - Projectile.timeLeft - SnapTime - (HoldTime / 2f);

        // 撕裂动画的进度
        public float SnapProgress => MathHelper.Clamp(SnapTimer / SnapTime, 0, 1);
        public float HoldProgress => MathHelper.Clamp(HoldTimer / HoldTime, 0, 1);
        public float StitchProgress => MathHelper.Clamp(StitchTimer / (MaxTime - (SnapTime + (HoldTime / 2f))), 0, 1);

        // 动画阶段：0-撕裂开始，1-持有状态，2-撕裂结束
        public int CurrentAnimation => (MaxTime - Projectile.timeLeft) <= SnapTime ? 0 : (MaxTime - Projectile.timeLeft) <= SnapTime + HoldTime ? 1 : 2;

        // 剪刀的当前位置，依靠撕裂位移比例
        public Vector2 scissorPosition => Projectile.Center + ThrustDisplaceRatio() * Projectile.velocity * 200f;

        // 玩家引用
        public Player Owner => Main.player[Projectile.owner];

        // 粒子 PolarStar，用于特效
        public Particle PolarStar;

        public override void SetDefaults()
        {
            // 初始化投射物的属性
            //Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = Projectile.height = 300;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = (int)MaxTime + 2;
        }

        // 控制是否可造成伤害
        public override bool? CanDamage()
        {
            return HoldProgress > 0;
        }

        public override void OnSpawn(IEntitySource source)
        {
            base.OnSpawn(source);
        }

        // 撞击判断，使用线性碰撞来模拟撕裂路径
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (HoldProgress == 0) return false;
            float collisionPoint = 0f;
            float bladeLength = ThrustDisplaceRatio() * 242f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + (Projectile.velocity * bladeLength), 30, ref collisionPoint);
        }

        // 不更新位置，因为撕裂的效果是瞬间完成的
        public override bool ShouldUpdatePosition() => false;


        // AI 控制，初始化和撕裂的视觉效果
        public override void AI()
        {
            if (!initialized) // 初始化
            {
                Projectile.timeLeft = (int)MaxTime;
                SoundEngine.PlaySound(SoundID.Item84 with { Volume = SoundID.Item84.Volume * 0.3f }, Projectile.Center);
                Projectile.velocity.Normalize();
                Projectile.rotation = Projectile.velocity.ToRotation();
                initialized = true;
                Projectile.netUpdate = true;

                // === 微分方程 · 冲击型法阵特效 ===
                for (int i = 0; i < 50; i++)
                {
                    float angle = MathHelper.TwoPi * i / 20f;
                    Vector2 offset = angle.ToRotationVector2() * Main.rand.NextFloat(40f, 80f); // 法阵环大小

                    Vector2 spawnPosition = Projectile.Center + offset;

                    // CritSpark（闪光碎片，关闭 Bloom）
                    Particle critSpark = new CritSpark(
                        spawnPosition,
                        offset.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(4f, 7f),
                        Color.White,
                        Color.LightYellow,
                        Main.rand.NextFloat(0.8f, 1.2f),
                        Main.rand.Next(18, 28),
                        0.08f,        // rotationSpeed
                        0f            // bloomScale (👈关闭 Bloom)
                    );
                    GeneralParticleHandler.SpawnParticle(critSpark);


                    // Spark（能量碎屑）
                    Particle spark = new SparkParticle(
                        spawnPosition,
                        offset.SafeNormalize(Vector2.Zero).RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(3f, 6f),
                        false,
                        25,
                        Main.rand.NextFloat(1.6f, 1.8f),
                        Color.LightGoldenrodYellow
                    );
                    GeneralParticleHandler.SpawnParticle(spark);

                    // Dust（等离子体微粒）
                    int dustIndex = Dust.NewDust(
                        spawnPosition,
                        0,
                        0,
                        DustID.GoldFlame,
                        offset.SafeNormalize(Vector2.Zero).X * Main.rand.NextFloat(2f, 4f),
                        offset.SafeNormalize(Vector2.Zero).Y * Main.rand.NextFloat(2f, 4f),
                        80,
                        Color.White,
                        Main.rand.NextFloat(1.8f, 1.9f)
                    );
                    Main.dust[dustIndex].noGravity = true;
                }

            }

            // 计算玩家和弹幕的实际距离
            Player player = Main.player[Projectile.owner];
            float actualDistance = Vector2.Distance(Projectile.Center, player.Center);

            // 设定一个最小的拉扯距离，例如242f
            float minDistance = 242f;
            float pullDistance = Math.Max(actualDistance, minDistance);

            // 更新撕裂长度参数
            ThrustDisplaceRatio(pullDistance); // 将拉扯距离传入以更新效果

            // 其他的效果逻辑
            Projectile.scale = 1.4f;
            HandleParticles();

            // 控制拉扯效果及路径上的粒子
            if (HoldTimer == 1)
            {
                // 此处生成特效逻辑保持不变，会根据 `pullDistance` 影响范围
                for (int i = 0; i < 20; i++)
                {
                    float positionAlongLine = MathHelper.Lerp(0f, pullDistance, Main.rand.NextFloat(0f, 1f));
                    Vector2 particlePosition = Projectile.Center + Projectile.velocity * positionAlongLine;
                    // 继续生成粒子
                }
            }
        }

        // 更新撕裂距离
        internal float ThrustDisplaceRatio(float distance) => PiecewiseAnimation(SnapProgress, new CurveSegment[] { anticipation, thrust }) * (distance / 242f);


        // 管理撕裂路径上的粒子效果，主要用于缝线和爆炸效果
        public void HandleParticles()
        {
            if (PolarStar == null) // 初始化 PolarStar 粒子
            {
                PolarStar = new GenericSparkle(Projectile.Center, Vector2.Zero, Color.White, Color.CornflowerBlue, Projectile.scale * 2f, 2, 0.1f, 5f, true);
                GeneralParticleHandler.SpawnParticle(PolarStar);
            }
            else if (HoldProgress <= 0.4f) // 更新 PolarStar 的位置和大小
            {
                PolarStar.Time = 0;
                PolarStar.Position = scissorPosition;
                PolarStar.Scale = Projectile.scale * 2f;
            }

            // 更新缝线粒子，模拟撕裂的缝合效果
            for (int i = 0; i < CurrentStitches; i++)
            {
                if (StitchRotations[i] == 0)
                {
                    StitchRotations[i] = Main.rand.NextFloat(-MathHelper.PiOver4, MathHelper.PiOver4) + MathHelper.PiOver2;
                    SoundEngine.PlaySound(i % 3 == 0 ? SoundID.Item63 : i % 3 == 1 ? SoundID.Item64 : SoundID.Item65, Owner.Center);
                    float positionAlongLine = (ThrustDisplaceRatio() * 242f / (float)maxStitches * 0.5f) + MathHelper.Lerp(0f, ThrustDisplaceRatio() * 242f, i / (float)maxStitches);
                    Vector2 stitchCenter = Projectile.Center + Projectile.velocity * positionAlongLine;
                    GeneralParticleHandler.SpawnParticle(new CritSpark(stitchCenter, Vector2.Zero, Color.White, Color.Cyan, 3f, 8, 0.1f, 3));

                    {
                        // === 撕裂路径增强特效（狂野且有序） ===

                        // 计算垂直方向
                        Vector2 perpendicular = Projectile.velocity.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.Zero);

                        for (int j = -1; j <= 1; j += 2) // 两侧各喷一次
                        {
                            // SparkParticle（更快，更有序的飞散火花）
                            Particle spark = new SparkParticle(
                                stitchCenter,
                                (Projectile.velocity * Main.rand.NextFloat(3f, 6f) + perpendicular * j * Main.rand.NextFloat(2f, 4f)) * 0.5f,
                                false,
                                20,
                                Main.rand.NextFloat(0.5f, 0.8f),
                                Color.LightYellow
                            );
                            GeneralParticleHandler.SpawnParticle(spark);

                            // Dust（更快更亮更可见的飞尘）
                            int dustIndex = Dust.NewDust(
                                stitchCenter,
                                0,
                                0,
                                DustID.Smoke,
                                (Projectile.velocity.X + perpendicular.X * j * Main.rand.NextFloat(1f, 2f)) * Main.rand.NextFloat(0.4f, 0.8f),
                                (Projectile.velocity.Y + perpendicular.Y * j * Main.rand.NextFloat(1f, 2f)) * Main.rand.NextFloat(0.4f, 0.8f),
                                60,
                                Color.White,
                                Main.rand.NextFloat(1.0f, 1.5f)
                            );
                            Main.dust[dustIndex].noGravity = true;
                            Main.dust[dustIndex].scale = Main.rand.NextFloat(0.8f, 1.2f);
                        }
                    }



                }
                StitchLifetimes[i]++;
            }
        }

        // 当投射物击中敌人时触发的效果
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 随机选择颜色生成一个脉冲环特效
            Color pulseColor = Main.rand.NextBool() ? (Main.rand.NextBool() ? Color.Orange : Color.Coral) : (Main.rand.NextBool() ? Color.OrangeRed : Color.Gold);
            Particle pulse = new PulseRing(target.Center, Vector2.Zero, pulseColor, 0.05f, 0.2f + Main.rand.NextFloat(0f, 1f), 30);
            GeneralParticleHandler.SpawnParticle(pulse);

            // 生成能量泄漏粒子效果，模拟击中时的能量散射效果
            for (int i = 0; i < 10; i++)
            {
                Vector2 particleSpeed = Projectile.velocity.RotatedByRandom(MathHelper.PiOver4 * 0.8f) * Main.rand.NextFloat(2.6f, 4f);
                Particle energyLeak = new SquishyLightParticle(target.Center, particleSpeed, Main.rand.NextFloat(0.3f, 0.6f), Color.Red, 60, 1, 1.5f, hueShift: 0.002f);
                GeneralParticleHandler.SpawnParticle(energyLeak);
            }
        }

        // 修改伤害值的衰减逻辑，击中次数越多，伤害降低得越多
        public const float DamageFalloffStrength = 0.15f; // 每次命中损失 15%
        public const float DamageFalloffSpeed = 1f;       // 命中计数倍数
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            //modifiers.SourceDamage *= (float)Math.Pow(1 - ArkoftheCosmos.blastFalloffStrenght, Projectile.numHits * ArkoftheCosmos.blastFalloffSpeed);

            float falloffFactor = (float)Math.Pow(
                1f - DamageFalloffStrength,
                Projectile.numHits * DamageFalloffSpeed
            );

            modifiers.SourceDamage *= falloffFactor;
        }

        // 当投射物消失时触发的效果
        public override void OnKill(int timeLeft)
        {
            // 如果玩家处于冲刺状态则停止
            if (Dashing)
            {
                Owner.velocity *= 0.1f; // 突然减速以停止冲刺
            }
            Owner.Calamity().LungingDown = false; // 取消冲刺标记
        }

        // 撕裂动画关键点，用于控制特效的不同阶段
        public CurveSegment anticipation = new CurveSegment(EasingType.SineBump, 0f, 0.2f, -0.1f);
        public CurveSegment thrust = new CurveSegment(EasingType.PolyOut, 0.3f, 0.2f, 3f, 3);
        internal float ThrustDisplaceRatio() => PiecewiseAnimation(SnapProgress, new CurveSegment[] { anticipation, thrust });

        public CurveSegment openMore = new CurveSegment(EasingType.SineBump, 0f, 0f, -0.15f);
        public CurveSegment close = new CurveSegment(EasingType.PolyIn, 0.35f, 0f, 1f, 4);
        public CurveSegment stayClosed = new CurveSegment(EasingType.Linear, 0.5f, 1f, 0f);
        internal float RotationRatio() => PiecewiseAnimation(SnapProgress, new CurveSegment[] { openMore, close, stayClosed });

        // 绘制前的特效和粒子渲染
        public override bool PreDraw(ref Color lightColor)
        {
            // 设置为添加性混合模式，用于增强光效
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // 绘制撕裂路径的光效线条
            Texture2D sliceTex = Request<Texture2D>("CalamityMod/Particles/BloomLine").Value;
            Color sliceColor = Color.Lerp(Color.OrangeRed, Color.White, SnapProgress);
            float rot = Projectile.rotation + MathHelper.PiOver2;
            Vector2 sliceScale = new Vector2(0.2f * (1 - SnapProgress), ThrustDisplaceRatio() * 242f);
            Main.EntitySpriteDraw(sliceTex, Projectile.Center - Main.screenPosition, null, sliceColor, rot, new Vector2(sliceTex.Width / 2f, sliceTex.Height), sliceScale, 0f, 0);

            // 绘制剪刀（可视化为两个刀刃合并效果），在 HoldProgress 的特定阶段内出现
            if (HoldProgress <= 0.4f)
            {
                Texture2D frontBlade = Request<Texture2D>("CalamityThrowingSpear/Weapons/NewWeapons/DPreDog/ElementalArkJav/EAJFragment").Value;
                Texture2D backBlade = Request<Texture2D>("CalamityThrowingSpear/Weapons/NewWeapons/DPreDog/ElementalArkJav/EAJBlade").Value;

                float snippingRotation = Projectile.rotation + MathHelper.PiOver4;
                float drawRotation = MathHelper.Lerp(snippingRotation - MathHelper.PiOver4, snippingRotation, RotationRatio());
                float drawRotationBack = MathHelper.Lerp(snippingRotation + MathHelper.PiOver4, snippingRotation, RotationRatio());

                Vector2 drawOrigin = new Vector2(33, 86);
                Vector2 drawOriginBack = new Vector2(44f, 86);
                Vector2 drawPosition = scissorPosition - Main.screenPosition;

                float opacity = (0.4f - HoldProgress) / 0.4f;
                Color drawColor = Color.Tomato * opacity * 0.9f;
                Color drawColorBack = Color.DeepSkyBlue * opacity * 0.9f;

                Main.EntitySpriteDraw(backBlade, drawPosition, null, drawColorBack, drawRotationBack, drawOriginBack, Projectile.scale, 0f, 0);
                Main.EntitySpriteDraw(frontBlade, drawPosition, null, drawColor * opacity, drawRotation, drawOrigin, Projectile.scale, 0f, 0);
            }

            // 绘制撕裂路径的光效线条和缝合效果
            if (HoldProgress > 0)
            {
                Texture2D lineTex = Request<Texture2D>("CalamityMod/Particles/ThinEndedLine").Value;

                Vector2 Shake = HoldProgress > 0.2f ? Vector2.Zero : Vector2.One.RotatedByRandom(MathHelper.TwoPi) * (1 - HoldProgress * 5f) * 0.5f;
                float raise = (float)Math.Sin(HoldProgress * MathHelper.PiOver2);

                Vector2 origin = new Vector2(lineTex.Width / 2f, lineTex.Height);
                float ripWidth = StitchProgress < 0.75f ? 0.2f : (1 - (StitchProgress - 0.75f) * 4f) * 0.2f;
                Vector2 scale = new Vector2(ripWidth, (ThrustDisplaceRatio() * 242f) / lineTex.Height);
                float lineOpacity = StitchProgress < 0.75f ? 1f : 1 - (StitchProgress - 0.75f) * 4f;

                Main.EntitySpriteDraw(lineTex, Projectile.Center - Main.screenPosition + Shake, null, Color.Lerp(Color.White, Color.OrangeRed * 0.7f, raise) * lineOpacity, rot, origin, scale, SpriteEffects.None, 0);

                // 绘制缝合线
                if (StitchProgress > 0)
                {
                    for (int i = 0; i < CurrentStitches; i++)
                    {
                        float positionAlongLine = (ThrustDisplaceRatio() * 242f / (float)maxStitches * 0.5f) + MathHelper.Lerp(0f, ThrustDisplaceRatio() * 242f, i / (float)maxStitches);
                        Vector2 stitchCenter = Projectile.Center + Projectile.velocity * positionAlongLine;

                        rot = Projectile.rotation + MathHelper.PiOver2 + StitchRotations[i];
                        origin = new Vector2(lineTex.Width / 2f, lineTex.Height / 2f);

                        float stitchLength = (float)Math.Sin(i / (float)(maxStitches - 1) * MathHelper.Pi) * 0.5f + 0.5f;
                        float stitchScale = (1f + (float)Math.Sin(MathHelper.Clamp(StitchLifetimes[i] / 7f, 0f, 1f) * MathHelper.Pi) * 0.3f) * 0.85f;
                        if (CurrentStitches == maxStitches)
                        {
                            stitchScale *= 1 - ((StitchTimer - (MaxTime - SnapTime - HoldTime * 0.5f) * 0.3f) / (MaxTime - SnapTime - HoldTime * 0.5f) * 0.7f) * 0.8f;
                        }
                        scale = new Vector2(0.2f, stitchLength) * stitchScale;

                        Color stitchColor = Color.Lerp(Color.White, Color.CornflowerBlue * 0.7f, (float)Math.Sin(MathHelper.Clamp(StitchLifetimes[i] / 7f, 0f, 1f) * MathHelper.PiOver2));

                        Main.EntitySpriteDraw(lineTex, stitchCenter - Main.screenPosition + Shake, null, stitchColor, rot, origin, scale, SpriteEffects.None, 0);
                    }
                }
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }

        // 传输和接收初始化状态的额外AI数据
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(initialized);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            initialized = reader.ReadBoolean();
        }


















    }
}
