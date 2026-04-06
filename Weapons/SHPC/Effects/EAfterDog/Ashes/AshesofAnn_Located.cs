using CalamityMod;
using CalamityMod.Graphics.Metaballs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Ashes
{
    internal class AshesofAnn_Located : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // 自定义计时器（禁止用localAI）
        private int timer = 0;

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.timeLeft = 19 * 3; // 生命周期
            Projectile.penetrate = -1;
            Projectile.friendly = true;
            Projectile.ignoreWater = false;
            Projectile.tileCollide = false;
        }

        public override bool? CanDamage()
        {
            return false; // 永远不造成伤害
        }

        public override void AI()
        {
            // 读取目标NPC
            int targetIndex = (int)Projectile.ai[0];

            if (targetIndex < 0 || targetIndex >= Main.npc.Length)
            {
                Projectile.Kill();
                return;
            }

            NPC target = Main.npc[targetIndex];

            if (!target.active)
            {
                Projectile.Kill();
                return;
            }

            // 始终粘在敌人身上
            Projectile.Center = target.Center;

            // 每5帧触发一次爆炸
            timer++;
            if (timer % 15 == 0)
            {
                // 大范围低伤害爆炸
                int explosionIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<AshesofAnn_SuperEXP>(),
                    (int)(Projectile.damage * 1.0f),
                    Projectile.knockBack,
                    Projectile.owner
                );

                //Projectile explosion = Main.projectile[explosionIndex];
                //explosion.width = 2500;
                //explosion.height = 2500;

                // 屏幕震动效果
                float shakePower = 35f; // 设置震动强度
                float distanceFactor = Utils.GetLerpValue(1000f, 0f, Projectile.Distance(Main.LocalPlayer.Center), true); // 距离衰减
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, shakePower * distanceFactor);



                CreateUltimateAshExplosionFX(Projectile.Center);
            }
        }
                        
        public override bool PreDraw(ref Color lightColor)
        {
            return false; // 完全透明，不绘制
        }







        private void CreateUltimateAshExplosionFX(Vector2 center)
        {
            // 爆炸音效
            SoundEngine.PlaySound(new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/最后通牒爆炸")
            {
                Volume = 1.35f,
                Pitch = -0.08f,
                PitchVariance = 0.16f,
                MaxInstances = 5
            }, center);

            Particle expandingPulse = new DirectionalPulseRing(
                center,
                Vector2.Zero,
                new Color(200, 30, 30), // 赤红写死
                new Vector2(1.2f, 1.2f),
                0f,
                0.5f,
                12f, // 改成12
                20
            );
            GeneralParticleHandler.SpawnParticle(expandingPulse);

            // 1. 最内核：高温内爆，只放亮而烫的熔岩球
            CreateAshInnerCollapse(center);

            // 2. 中层：黑红邪术魔法阵 + 六芒/圆环结构
            CreateAshMagicCircleDust(center);

            // 3. 中层空隙：黑烟为主，骷髅头为辅，比例约 8:2
            CreateAshSmokeAndSkulls(center);

            // 4. 外层：细碎火花与暗红碎屑，负责把整体撑到超大范围
            CreateAshOuterSparks(center);
        }

            private void CreateAshInnerCollapse(Vector2 center)
            {
                // ===== 1. 最中心：规则连通的高温核心 =====
                // 不是随机乱撒，而是用“涂黑的圆”思路铺满
                // 每一圈的相邻点距离都控制在半径直径以内，保证视觉上彼此连接
                CreateConnectedRancorCore(center);

                // ===== 2. 四条银河悬臂式 inward 弧臂 =====
                // 用 4 条向内弯曲的弧线来做“邪恶吸收感”
                // 每个点都带向心速度，同时保留少量切线速度，既有秩序也有混沌
                CreateConnectedInwardArms(center);

                // ===== 3. 核心高温 Dust =====
                // 保留你喜欢的核心区扩散感，但把扩散范围整体扩大到原来的约 3 倍
                // 同时做三层：内圈高热、中过渡、外圈无序碎裂
                for (int layer = 0; layer < 3; layer++)
                {
                    int count;
                    float minRadius;
                    float maxRadius;
                    float minSpeed;
                    float maxSpeed;
                    Color colorA;
                    Color colorB;

                    switch (layer)
                    {
                        case 0:
                            count = 70;
                            minRadius = 30f;
                            maxRadius = 120f;
                            minSpeed = 8f;
                            maxSpeed = 18f;
                            colorA = new Color(255, 120, 30);
                            colorB = new Color(255, 240, 150);
                            break;

                        case 1:
                            count = 90;
                            minRadius = 100f;
                            maxRadius = 220f;
                            minSpeed = 10f;
                            maxSpeed = 24f;
                            colorA = new Color(255, 80, 20);
                            colorB = new Color(255, 180, 70);
                            break;

                        default:
                            count = 120;
                            minRadius = 180f;
                            maxRadius = 360f; // 原来约 80，这里直接拉到约 3 倍以上
                            minSpeed = 12f;
                            maxSpeed = 30f;
                            colorA = new Color(200, 40, 10);
                            colorB = new Color(255, 150, 40);
                            break;
                    }

                    for (int i = 0; i < count; i++)
                    {
                        // 有序：基础放射
                        float angle = MathHelper.TwoPi * i / count;

                        // 无序：叠一点随机偏移和伪噪声扰动
                        float angleJitter = Main.rand.NextFloat(-0.08f, 0.08f);
                        float noise = (float)Math.Sin(angle * 3f + i * 0.37f) * 0.5f + 0.5f;
                        float radius = MathHelper.Lerp(minRadius, maxRadius, Main.rand.NextFloat() * 0.55f + noise * 0.45f);

                        Vector2 outward = (angle + angleJitter).ToRotationVector2();
                        Vector2 tangent = outward.RotatedBy(MathHelper.PiOver2);
                        Vector2 spawnPos = center + outward * radius;

                        Dust coreDust = Dust.NewDustPerfect(spawnPos, 264);
                        coreDust.noGravity = true;
                        coreDust.scale = Main.rand.NextFloat(1.6f, 3.2f);

                        // 速度：以向外爆散为主，同时掺一点切线旋感
                        coreDust.velocity =
                            outward * Main.rand.NextFloat(minSpeed, maxSpeed) +
                            tangent * Main.rand.NextFloat(-3.5f, 3.5f);

                        coreDust.color = Color.Lerp(colorA, colorB, Main.rand.NextFloat());
                    }
                }
            }

            private void CreateConnectedRancorCore(Vector2 center)
            {
                // 整体随机：统一旋转 + 轻微整体缩放
                float globalRotation = Main.rand.NextFloat(MathHelper.TwoPi);
                float globalScale = Main.rand.NextFloat(0.92f, 1.08f);

                // ===== 中心填充（防空洞）=====
                for (int i = 0; i < 8; i++)
                {
                    Vector2 offset = Main.rand.NextVector2Circular(16f, 16f);
                    float radius = Main.rand.NextFloat(60f, 90f) * globalScale;

                    RancorLavaMetaball.SpawnParticle(center + offset, radius);
                }

                // ===== 同心结构（核心逻辑）=====
                float[] ringRadii = new float[] { 28f, 56f, 84f, 112f, 136f };

                for (int ringIndex = 0; ringIndex < ringRadii.Length; ringIndex++)
                {
                    float ringRadius = ringRadii[ringIndex] * globalScale;

                    // 点密度随半径变化
                    float spacing = 48f;
                    int pointCount = Math.Max(8, (int)Math.Ceiling(MathHelper.TwoPi * ringRadius / spacing));

                    // 每圈额外随机偏移（但统一）
                    float ringOffset = Main.rand.NextFloat(-0.08f, 0.08f);

                    for (int i = 0; i < pointCount; i++)
                    {
                        float angle = MathHelper.TwoPi * i / pointCount + globalRotation + ringOffset;

                        // 轻微波动（保证不死板但仍连通）
                        float radialJitter = (float)Math.Sin(i * 1.4f + ringIndex * 0.7f) * 5f;

                        Vector2 pos = center + angle.ToRotationVector2() * (ringRadius + radialJitter);

                        float size = (34f + ringIndex * 5f) * globalScale + Main.rand.NextFloat(-4f, 4f);

                        RancorLavaMetaball.SpawnParticle(pos, size);
                    }
                }
            }
        private void CreateConnectedInwardArms(Vector2 center)
        {
            // 整体随机（统一作用）
            float globalRotation = Main.rand.NextFloat(MathHelper.TwoPi);
            float globalTightness = Main.rand.NextFloat(0.85f, 1.2f);

            int armCount = 4;
            int pointsPerArm = 24;

            for (int arm = 0; arm < armCount; arm++)
            {
                float armBaseAngle = MathHelper.TwoPi * arm / armCount + globalRotation;

                for (int i = 0; i < pointsPerArm; i++)
                {
                    float t = i / (float)(pointsPerArm - 1);

                    float radius = MathHelper.Lerp(90f, 480f, t);

                    // 螺旋强度带随机（但整条臂一致）
                    float spiral = MathHelper.Lerp(0.2f, 1.4f, t) * globalTightness;

                    // 轻微波动
                    float wave = (float)Math.Sin(t * MathHelper.TwoPi * 1.6f + arm * 0.7f) * 0.18f;

                    float angle = armBaseAngle + spiral + wave;

                    Vector2 outward = angle.ToRotationVector2();
                    Vector2 tangent = outward.RotatedBy(MathHelper.PiOver2);

                    Vector2 spawnPos = center + outward * radius;

                    Vector2 velocity =
                        -outward * MathHelper.Lerp(5f, 14f, t) +
                        tangent * MathHelper.Lerp(2.6f, 0.6f, t) * (arm % 2 == 0 ? 1f : -1f);

                    float size = MathHelper.Lerp(55f, 85f, 1f - t) + Main.rand.NextFloat(-4f, 4f);

                    GruesomeMetaball.SpawnParticle(spawnPos, velocity, size);

                    // ===== 补连接点（关键，防断裂）=====
                    if (i % 3 == 1 && i < pointsPerArm - 1)
                    {
                        float t2 = (i + 1) / (float)(pointsPerArm - 1);
                        float r2 = MathHelper.Lerp(90f, 480f, t2);

                        float spiral2 = MathHelper.Lerp(0.2f, 1.4f, t2) * globalTightness;
                        float wave2 = (float)Math.Sin(t2 * MathHelper.TwoPi * 1.6f + arm * 0.7f) * 0.18f;

                        float angle2 = armBaseAngle + spiral2 + wave2;

                        Vector2 pos2 = center + angle2.ToRotationVector2() * r2;

                        Vector2 mid = (spawnPos + pos2) * 0.5f;

                        Vector2 midDir = (mid - center).SafeNormalize(Vector2.Zero);
                        Vector2 midTan = midDir.RotatedBy(MathHelper.PiOver2);

                        Vector2 midVel =
                            -midDir * MathHelper.Lerp(6f, 12f, t) +
                            midTan * MathHelper.Lerp(2f, 0.5f, t) * (arm % 2 == 0 ? 1f : -1f);

                        GruesomeMetaball.SpawnParticle(mid, midVel, size * 0.9f);
                    }
                }
            }
        }


            private void CreateAshMagicCircleDust(Vector2 center)
            {
                // 三个固定主圈：40格、50格、60格
                float[] mainRings = new float[]
                {
                    40f * 16f,
                    50f * 16f,
                    60f * 16f
                };

                Color[] innerColors = new Color[]
                {
                    new Color(70, 0, 0),
                    new Color(45, 0, 0),
                    new Color(30, 0, 0)
                };

                Color[] outerColors = new Color[]
                {
                    new Color(170, 20, 20),
                    new Color(145, 15, 15),
                    new Color(110, 10, 10)
                };

                int[] ringCounts = new int[]
                {
                    48,
                    60,
                    72
                };

                float[] outwardSpeeds = new float[]
                {
                    1.2f,
                    1.0f,
                    0.85f
                };

                float[] tangentialSpeeds = new float[]
                {
                    1.9f,
                    1.6f,
                    1.35f
                };

                // 每次调用整体随机旋转一下，但三圈相互仍保持统一审美
                float globalRotation = Main.rand.NextFloat(MathHelper.TwoPi);

                for (int i = 0; i < mainRings.Length; i++)
                {
                    // 先画固定主圈
                    CreateAshCircleRing(
                        center,
                        mainRings[i],
                        ringCounts[i],
                        267,
                        innerColors[i],
                        outerColors[i],
                        outwardSpeeds[i],
                        tangentialSpeeds[i]
                    );

                    // 再在这一圈上随机散布“小圆环 / 小三角”
                    CreateAshScatterGlyphsOnRing(
                        center,
                        mainRings[i],
                        globalRotation,
                        innerColors[i],
                        outerColors[i]
                    );
                }
            }
        private void CreateAshScatterGlyphsOnRing(Vector2 center, float ringRadius, float globalRotation, Color innerColor, Color outerColor)
        {
            // 每一圈上随机散布一些符文位点
            int glyphCount = Main.rand.Next(8, 13);

            for (int i = 0; i < glyphCount; i++)
            {
                // 用“分区 + 抖动”保证随机但不至于扎堆
                float baseAngle = MathHelper.TwoPi * i / glyphCount;
                float randomOffset = Main.rand.NextFloat(-0.22f, 0.22f);
                float angle = baseAngle + randomOffset + globalRotation;

                Vector2 direction = angle.ToRotationVector2();
                Vector2 glyphCenter = center + direction * ringRadius;

                // 小圆环 / 小三角随机出现
                switch (Main.rand.Next(3))
                {
                    case 0:
                        {
                            // 小圆环
                            float smallRingRadius = Main.rand.NextFloat(28f, 52f);
                            int smallRingCount = Main.rand.Next(10, 16);

                            CreateAshCircleRing(
                                glyphCenter,
                                smallRingRadius,
                                smallRingCount,
                                267,
                                innerColor,
                                Color.Lerp(outerColor, new Color(220, 35, 35), 0.35f),
                                0.55f,
                                0.9f
                            );
                            break;
                        }

                    case 1:
                        {
                            // 正三角
                            float triangleRadius = Main.rand.NextFloat(26f, 46f);
                            CreateAshTriangle(
                                glyphCenter,
                                triangleRadius,
                                267,
                                Color.Lerp(innerColor, outerColor, 0.65f),
                                true
                            );
                            break;
                        }

                    default:
                        {
                            // 倒三角
                            float triangleRadius = Main.rand.NextFloat(26f, 46f);
                            CreateAshTriangle(
                                glyphCenter,
                                triangleRadius,
                                267,
                                Color.Lerp(innerColor, outerColor, 0.65f),
                                false
                            );
                            break;
                        }
                }

                // 每个位点再补一点朝外散的小碎屑，让它不显得太死板
                int shardCount = Main.rand.Next(4, 8);
                for (int j = 0; j < shardCount; j++)
                {
                    Dust shardDust = Dust.NewDustPerfect(glyphCenter + Main.rand.NextVector2Circular(12f, 12f), 267);
                    shardDust.noGravity = true;
                    shardDust.scale = Main.rand.NextFloat(1.0f, 1.45f);
                    shardDust.color = Color.Lerp(innerColor, outerColor, Main.rand.NextFloat());
                    shardDust.velocity =
                        direction * Main.rand.NextFloat(0.8f, 2.2f) +
                        direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-1.1f, 1.1f);
                }
            }
        }

        private void CreateAshCircleRing(Vector2 center, float radius, int count, int dustType, Color innerColor, Color outerColor, float outwardSpeed, float tangentialSpeed)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.TwoPi * i / count;
                Vector2 outward = angle.ToRotationVector2();
                Vector2 tangent = outward.RotatedBy(MathHelper.PiOver2);

                // 圆环位置
                Vector2 spawnPos = center + outward * radius;

                // 让 Dust 有“向外略扩散 + 沿切线旋转”的观感
                Vector2 velocity = outward * Main.rand.NextFloat(outwardSpeed * 0.8f, outwardSpeed * 1.25f) +
                                   tangent * Main.rand.NextFloat(-tangentialSpeed, tangentialSpeed);

                Dust ringDust = Dust.NewDustPerfect(spawnPos, dustType);
                ringDust.noGravity = true;
                ringDust.scale = Main.rand.NextFloat(1.15f, 1.85f);
                ringDust.color = Color.Lerp(innerColor, outerColor, Main.rand.NextFloat());
                ringDust.velocity = velocity;
                ringDust.rotation = angle;
                ringDust.fadeIn = Main.rand.NextFloat(0.8f, 1.4f);
            }
        }

        private void CreateAshTriangle(Vector2 center, float radius, int dustType, Color dustColor, bool upright)
        {
            float baseAngle = upright ? -MathHelper.PiOver2 : MathHelper.PiOver2;
            Vector2[] points = new Vector2[3];

            for (int i = 0; i < 3; i++)
                points[i] = center + (baseAngle + MathHelper.TwoPi * i / 3f).ToRotationVector2() * radius;

            for (int i = 0; i < 3; i++)
            {
                Vector2 start = points[i];
                Vector2 end = points[(i + 1) % 3];

                for (int j = 0; j < 28; j++)
                {
                    float t = j / 27f;
                    Vector2 pos = Vector2.Lerp(start, end, t);

                    // 边线 Dust 轻微向外扩散，同时带一点旋转感
                    Vector2 outward = (pos - center).SafeNormalize(Vector2.Zero);
                    Vector2 tangent = outward.RotatedBy(MathHelper.PiOver2);

                    Dust lineDust = Dust.NewDustPerfect(pos, dustType);
                    lineDust.noGravity = true;
                    lineDust.scale = Main.rand.NextFloat(1.15f, 1.7f);
                    lineDust.color = Color.Lerp(dustColor, Color.Black, Main.rand.NextFloat(0.35f));
                    lineDust.velocity = outward * Main.rand.NextFloat(0.8f, 1.9f) + tangent * Main.rand.NextFloat(-1.2f, 1.2f);
                }
            }
        }


        private void CreateAshSmokeAndSkulls(Vector2 center)
        {
            // ===== 1. 先铺一层高密度黑烟圆环 =====
            // 小半径 = 10×16，大半径 = 20×16
            float innerRadius = 10f * 16f;
            float outerRadius = 20f * 16f;

            // 用多圈 + 多点的方式把圆环铺密，同时让它向外旋转着散掉
            int ringLayers = 6;
            for (int layer = 0; layer < ringLayers; layer++)
            {
                float layerT = layer / (float)(ringLayers - 1);
                float radius = MathHelper.Lerp(innerRadius, outerRadius, layerT);

                // 外圈点更多，保证不会稀
                int pointCount = (int)MathHelper.Lerp(36f, 64f, layerT);

                // 每一层整体随机转一点，避免太死板
                float layerRotation = Main.rand.NextFloat(MathHelper.TwoPi);

                for (int i = 0; i < pointCount; i++)
                {
                    float angle = MathHelper.TwoPi * i / pointCount + layerRotation;
                    Vector2 outward = angle.ToRotationVector2();
                    Vector2 tangent = outward.RotatedBy(MathHelper.PiOver2);

                    // 位置轻微抖动，但整体仍然是圆环
                    Vector2 spawnPos = center
                        + outward * (radius + Main.rand.NextFloat(-10f, 10f))
                        + Main.rand.NextVector2Circular(4f, 4f);

                    // 向外 + 切线速度，形成“往外旋转着消失”的感觉
                    Vector2 smokeVel =
                        outward * Main.rand.NextFloat(1.2f, 2.8f) +
                        tangent * Main.rand.NextFloat(-2.2f, 2.2f);

                    float smokeScale = Main.rand.NextFloat(1.15f, 1.95f);

                    Particle smoke = new SmallSmokeParticle(
                        spawnPos,
                        smokeVel,
                        Color.DimGray,
                        Main.rand.NextBool() ? Color.SlateGray : Color.Black,
                        smokeScale,
                        100
                    );
                    GeneralParticleHandler.SpawnParticle(smoke);
                }
            }

            // ===== 2. 保留原本的随机黑烟填充 =====
            for (int i = 0; i < 64; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 direction = angle.ToRotationVector2();
                Vector2 tangent = direction.RotatedBy(MathHelper.PiOver2);

                // 中层范围，专门填满法阵与内爆之间的空隙
                Vector2 spawnPos = center + direction * Main.rand.NextFloat(180f, 860f);

                // 也给这层一点旋转 outward 的感觉
                Vector2 smokeVel =
                    direction * Main.rand.NextFloat(1.6f, 5.2f) +
                    tangent * Main.rand.NextFloat(-1.8f, 1.8f) +
                    Main.rand.NextVector2Circular(0.8f, 0.8f);

                float smokeScale = Main.rand.NextFloat(1.3f, 2.5f);

                Particle smoke = new SmallSmokeParticle(
                    spawnPos,
                    smokeVel,
                    Color.DimGray,
                    Main.rand.NextBool() ? Color.SlateGray : Color.Black,
                    smokeScale,
                    100
                );
                GeneralParticleHandler.SpawnParticle(smoke);
            }
        }

        private void CreateAshOuterSparks(Vector2 center)
        {
            // ===== 最外层细火花：负责把整体观感撑到超大直径 =====
            // 这里用更细、更远、更稀碎的 Dust 当 spark 层来铺外圈
            for (int i = 0; i < 180; i++)
            {
                float angle = MathHelper.TwoPi * i / 180f + Main.rand.NextFloat(-0.02f, 0.02f);
                Vector2 outward = angle.ToRotationVector2();
                Vector2 tangent = outward.RotatedBy(MathHelper.PiOver2);

                // 外层起始半径很大，确保整体视觉接近直径 2700
                Vector2 spawnPos = center + outward * Main.rand.NextFloat(980f, 1280f);

                Dust spark = Dust.NewDustPerfect(spawnPos, 264);
                spark.noGravity = true;
                spark.scale = Main.rand.NextFloat(0.85f, 1.45f);
                spark.color = Color.Lerp(new Color(255, 70, 30), new Color(255, 180, 80), Main.rand.NextFloat());

                // 小 outward + 强一点切线，形成“旋着散掉”的薄外圈
                spark.velocity = outward * Main.rand.NextFloat(1.4f, 3.4f) + tangent * Main.rand.NextFloat(-2.8f, 2.8f);
            }

            // ===== 再补一层暗红碎片，让外圈不至于全是亮点 =====
            for (int i = 0; i < 90; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 outward = angle.ToRotationVector2();

                Vector2 spawnPos = center + outward * Main.rand.NextFloat(760f, 1180f);
                Vector2 velocity = outward * Main.rand.NextFloat(2.4f, 5.5f) + outward.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-1.5f, 1.5f);

                float size = Main.rand.NextFloat(24f, 52f);
                GruesomeMetaball.SpawnParticle(spawnPos, velocity, size);
            }
        }








    }
}
