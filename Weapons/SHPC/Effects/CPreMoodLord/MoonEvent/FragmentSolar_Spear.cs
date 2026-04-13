using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.MoonEvent
{
    public class FragmentSolar_Spear : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        // ===== 自定义计数器（禁止用localAI）=====
        private int hitCount;
        private int visualTimer;



        public override bool PreDraw(ref Color lightColor)
        {
            if (visualTimer < 10)
                return false;

            SpriteBatch sb = Main.spriteBatch;

            Texture2D tex = ModContent.Request<Texture2D>(
                Projectile.ModProjectile.Texture
            ).Value;

            Texture2D squareTex = ModContent.Request<Texture2D>(
                "CalamityMod/Particles/GlowSquareParticleThick"
            ).Value;

            Vector2 origin = tex.Size() * 0.5f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            // ======== 太阳调色盘 ========
            Color[] firePalette = new Color[]
            {
        new Color(255, 220, 120),
        new Color(255, 170, 60),
        new Color(255, 110, 30),
        new Color(255, 70, 20),
            };

            // ======== 动态描边颜色函数（旋转流动）========
            Color GetOutlineColor(float t)
            {
                float time = Main.GlobalTimeWrappedHourly * 2.2f;
                float idx = (t + time) % 1f;

                int i = (int)(idx * firePalette.Length) % firePalette.Length;
                int j = (i + 1) % firePalette.Length;

                float lerp = idx * firePalette.Length % 1f;

                Color c = Color.Lerp(firePalette[i], firePalette[j], lerp);
                c *= 0.8f;
                c.A = 0;
                return c;
            }

            // ======== EXO 丝带（不动）========
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
                     DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            float PrimitiveWidthFunction(float completionRatio, Vector2 vertexPos)
            {
                float w = Projectile.width * 3.55f;
                w *= MathHelper.SmoothStep(
                    0.5f,
                    1.0f,
                    Utils.GetLerpValue(0f, 0.25f, completionRatio, true)
                );
                return w;
            }

            Color PrimitiveTrailColor(float completionRatio, Vector2 vertexPos)
            {
                Color c = firePalette[
                    (int)(completionRatio * firePalette.Length) % firePalette.Length
                ];

                c *= Projectile.Opacity * (1f - completionRatio);

                float speedBoost =
                    Utils.GetLerpValue(1f, 6f, Projectile.velocity.Length(), true);

                c *= speedBoost;
                c.A = 0;
                return c;
            }

            Vector2 frontOffset = Projectile.velocity.SafeNormalize(Vector2.Zero) * (Projectile.width * 1.5f);

            Vector2[] shiftedOldPos = new Vector2[Projectile.oldPos.Length];
            for (int i = 0; i < Projectile.oldPos.Length; i++)
                shiftedOldPos[i] = Projectile.oldPos[i] + frontOffset;

            Vector2 PrimitiveOffsetFunction(float t, Vector2 vertexPos)
            {
                return Projectile.Size * 0.5f +
                       Projectile.velocity.SafeNormalize(Vector2.Zero) * Projectile.scale * 2f;
            }

            GameShaders.Misc["CalamityMod:SideStreakTrail"].UseImage1("Images/Misc/Perlin");

            PrimitiveRenderer.RenderTrail(
                shiftedOldPos,
                new(
                    PrimitiveWidthFunction,
                    PrimitiveTrailColor,
                    PrimitiveOffsetFunction,
                    shader: GameShaders.Misc["CalamityMod:SideStreakTrail"]
                ),
                60
            );

            // ======== 描边层（核心新增）========
            float timeRot = Main.GlobalTimeWrappedHourly * 3.5f;

            // —— 第一层：旋转方形描边（核心视觉）
            for (int i = 0; i < 3; i++)
            {
                float rot = timeRot + i * 0.6f;

                float scale = Projectile.scale * (0.8f + i * 0.25f);

                Color c = GetOutlineColor(i * 0.2f) * (0.45f - i * 0.12f);

                Main.EntitySpriteDraw(
                    squareTex,
                    drawPos,
                    null,
                    c,
                    rot + MathHelper.PiOver4,
                    squareTex.Size() * 0.5f,
                    scale,
                    SpriteEffects.None
                );
            }

            // —— 第二层：环绕旋转描边（真正的“动态描边”）
            float radius = 5f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f) * 1.5f;

            for (int i = 0; i < 10; i++)
            {
                float angle = MathHelper.TwoPi * i / 10f + timeRot;

                Vector2 offset = angle.ToRotationVector2() * radius;

                Color c = GetOutlineColor(i / 10f) * 0.6f;

                Main.EntitySpriteDraw(
                    tex,
                    drawPos + offset,
                    null,
                    c,
                    Projectile.rotation,
                    origin,
                    Projectile.scale * 1.05f,
                    SpriteEffects.None
                );
            }

            // —— 第三层：方向性拖曳描边（强化速度感）
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            for (int i = 0; i < 3; i++)
            {
                Vector2 offset = -forward * i * 3f;

                Color c = GetOutlineColor(i * 0.15f) * (0.4f - i * 0.12f);

                Main.EntitySpriteDraw(
                    tex,
                    drawPos + offset,
                    null,
                    c,
                    Projectile.rotation,
                    origin,
                    Projectile.scale,
                    SpriteEffects.None
                );
            }

            // ======== 回正常绘制 ========
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                     DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            // —— 主体
            sb.Draw(tex, drawPos, null, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);

            // ======== 普通拖尾 ========
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;

                float fade = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;

                Color c = new Color(255, 160, 60) * 0.35f * fade;
                c.A = 0;

                float scale = Projectile.scale * (0.6f + fade * 0.4f);

                sb.Draw(
                    tex,
                    pos,
                    null,
                    c,
                    Projectile.rotation,
                    origin,
                    scale,
                    SpriteEffects.None,
                    0f
                );
            }

            return false;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;

            Projectile.friendly = true;
            Projectile.hostile = false;

            Projectile.penetrate = 2; // 穿透两次
            Projectile.timeLeft = 180;

            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;

            Projectile.DamageType = DamageClass.Ranged;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;

            Projectile.extraUpdates = 1;
        }

        private int curveTimer = 0;     // 曲线阶段计时器
        private Vector2 curveVel;       // 曲线飞行方向
        // ================= OnSpawn =================
        public override void OnSpawn(IEntitySource source)
        {
            visualTimer = 0;
        }

        // ================= AI =================
        public override void AI()
        {
            if (Projectile.numUpdates == 0)
                visualTimer++;

            // 基础旋转（长矛感）
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;



            if (curveTimer > 0)
            {
                curveTimer--;

                // 曲线：轻微摇摆
                Projectile.velocity = Projectile.velocity.RotatedBy(MathHelper.ToRadians(0.8f)) * 0.99f;

                if (curveTimer == 0)
                {
                    // 曲线结束 → 开始重新追踪
                    NPC closestNPC = Main.npc
                        .Where(npc => npc.active && !npc.friendly && npc.life > 0)
                        .OrderBy(npc => Vector2.Distance(npc.Center, Projectile.Center))
                        .FirstOrDefault();

                    if (closestNPC != null)
                    {
                        Vector2 direction = closestNPC.Center - Projectile.Center;
                        Projectile.velocity = Vector2.Normalize(direction) * Projectile.velocity.Length();
                    }
                }
            }

            // 加速
            Projectile.velocity *= 1.01f;


            {
                // ===== 强化版日耀飞行特效 =====
                Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Vector2 backward = -forward;
                Vector2 side = forward.RotatedBy(MathHelper.PiOver2);

                // 1. 核心高温尾焰：贴着飞行路径后方连续喷射
                if (Main.rand.NextBool(1))
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 spawnPos = Projectile.Center - forward * Main.rand.NextFloat(4f, 14f) + Main.rand.NextVector2Circular(2f, 2f);
                        Vector2 velocity = backward * Main.rand.NextFloat(2.5f, 6.5f) + Main.rand.NextVector2Circular(1.2f, 1.2f);

                        //Dust d = Dust.NewDustPerfect(
                        //    spawnPos,
                        //    DustID.Smoke,
                        //    velocity,
                        //    0,
                        //    Color.Lerp(Color.Gray, Color.DarkGray, Main.rand.NextFloat(0.2f, 0.8f)),
                        //    Main.rand.NextFloat(1.15f, 1.9f)
                        //);
                        //d.noGravity = true;
                        //d.fadeIn = 0.35f;
                    }
                }

                // 2. 外焰抛洒：左右两侧甩出高热火花，增强“太阳爆燃”感
                if (Main.rand.NextBool(2))
                {
                    for (int i = 0; i < 2; i++)
                    {
                        float sideOffset = Main.rand.NextFloat(-8f, 8f);
                        Vector2 spawnPos = Projectile.Center + side * sideOffset + Main.rand.NextVector2Circular(2f, 2f);
                        Vector2 velocity = backward.RotatedByRandom(0.45f) * Main.rand.NextFloat(2f, 5f)
                                         + side * Main.rand.NextFloat(-1.6f, 1.6f);

                        Dust d = Dust.NewDustPerfect(
                            spawnPos,
                            DustID.Torch,
                            velocity,
                            0,
                            Color.Lerp(new Color(255, 135, 40), new Color(255, 210, 105), Main.rand.NextFloat()),
                            Main.rand.NextFloat(1.5f, 2.7f)
                        );
                        d.noGravity = true;
                        d.fadeIn = 2.5f;
                    }
                }

                // 3. 高温气泡 / 等离子火种：用 CustomSpark 做“鼓泡感”
                if (Main.rand.NextBool(2))
                {
                    float t = Main.GlobalTimeWrappedHourly * 7.2f + Projectile.identity * 0.37f;

                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 spawnPos = Projectile.Center
                            - forward * Main.rand.NextFloat(2f, 10f)
                            + side * Main.rand.NextFloat(-6f, 6f);

                        Vector2 velocity = backward * Main.rand.NextFloat(1.4f, 3.8f)
                            + side * Main.rand.NextFloat(-1.1f, 1.1f)
                            + Main.rand.NextVector2Circular(0.6f, 0.6f);

                        Particle spark = new CustomSpark(
                            spawnPos,
                            velocity,
                            "CalamityMod/Particles/ProvidenceMarkParticle",
                            false,
                            Main.rand.Next(16, 24),
                            Main.rand.NextFloat(0.85f, 1.2f),
                            Color.Lerp(new Color(255, 230, 125), new Color(255, 120, 35), 0.5f + 0.5f * (float)Math.Sin(t + i * 0.9f)),
                            new Vector2(Main.rand.NextFloat(1.1f, 1.45f), Main.rand.NextFloat(0.28f, 0.5f)),
                            true,
                            false,
                            Main.rand.NextFloat(-0.08f, 0.08f),
                            false,
                            false,
                            0.08f
                        );

                        GeneralParticleHandler.SpawnParticle(spark);
                    }
                }

                // 4. 前端炽亮压缩光：让矛尖看起来像在烧穿空气
                if (Main.rand.NextBool(3))
                {
                    Vector2 frontPos = Projectile.Center + forward * Main.rand.NextFloat(8f, 14f);
                    Vector2 velocity = forward * Main.rand.NextFloat(0.5f, 2.2f) + Main.rand.NextVector2Circular(0.4f, 0.4f);

                    Dust frontFlash = Dust.NewDustPerfect(
                        frontPos,
                        DustID.Torch,
                        velocity,
                        0,
                        Color.Lerp(Color.White, new Color(255, 215, 110), 0.65f),
                        Main.rand.NextFloat(1.2f, 1.9f)
                    );
                    frontFlash.noGravity = true;
                    frontFlash.fadeIn = 2.5f;
                }

                // 5. 动态照明：中心偏白，外圈偏橙
                Lighting.AddLight(
                    Projectile.Center,
                    new Vector3(1.25f, 0.72f, 0.18f) * 0.75f
                );
            }


        }

        // ================= ModifyHitNPC =================
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
        }

        // ================= OnHitNPC =================
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 瞄准最近的敌人并调整弹幕方向
            NPC closestNPC = Main.npc
                .Where(npc => npc.active && !npc.friendly && npc.life > 0 && npc.whoAmI != target.whoAmI)
                .OrderBy(npc => Vector2.Distance(npc.Center, Projectile.Center))
                .FirstOrDefault();


            if (closestNPC != null)
            {
                // 命中后先折射，偏移 ±10°
                float offset = Main.rand.NextBool() ? MathHelper.ToRadians(10f) : MathHelper.ToRadians(-10f);
                Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(offset);
                curveVel = dir * Projectile.velocity.Length();

                // 开始进入曲线阶段
                curveTimer = 30; // 例如持续 30 帧
                Projectile.velocity = curveVel;
            }

            hitCount++;

            // ===== 日耀爆炸（每次命中触发）=====
            SpawnSolarExplosion();

        }

        // ================= OnKill =================
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.8f, Pitch = 0.08f }, Projectile.Center);
            // 死亡也补一个爆炸
            SpawnSolarExplosion();
        }

        // ================= TileCollide =================
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return true;
        }

        private void SpawnSolarExplosion()
        {
            Vector2 center = Projectile.Center;

            // ===== 先打一层强光 =====
            Lighting.AddLight(center, new Vector3(1.8f, 1.05f, 0.28f) * 1.1f);

            // ===== 第一层：白热闪核，强调“太阳核心瞬爆” =====
            for (int i = 0; i < 14; i++)
            {
                Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(5.5f, 10.5f);

                Dust core = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(4f, 4f),
                    DustID.Torch,
                    vel,
                    0,
                    Color.Lerp(Color.White, new Color(255, 215, 110), Main.rand.NextFloat(0.35f, 0.75f)),
                    Main.rand.NextFloat(1.5f, 2.7f)
                );
                core.noGravity = true;
                core.fadeIn = 2.5f;
            }

            // ===== 第二层：高速放射火矛，做出“爆炸像恒星喷流” =====
            for (int i = 0; i < 24; i++)
            {
                float angle = MathHelper.TwoPi * i / 24f + Main.rand.NextFloat(-0.06f, 0.06f);
                Vector2 dir = angle.ToRotationVector2();

                // 主射流
                Dust jet = Dust.NewDustPerfect(
                    center,
                    DustID.Torch,
                    dir * Main.rand.NextFloat(8f, 15f),
                    0,
                    Color.Lerp(new Color(255, 235, 150), new Color(255, 120, 35), Main.rand.NextFloat()),
                    Main.rand.NextFloat(1.6f, 2.7f)
                );
                jet.noGravity = true;
                jet.fadeIn = 2.5f;

                // 副火花，贴着主射流喷
                Dust jetSpark = Dust.NewDustPerfect(
                    center + dir * Main.rand.NextFloat(2f, 8f),
                    DustID.Torch,
                    dir.RotatedByRandom(0.18f) * Main.rand.NextFloat(5f, 11f),
                    0,
                    Color.Lerp(Color.OrangeRed, Color.Yellow, Main.rand.NextFloat()),
                    Main.rand.NextFloat(1.0f, 1.6f)
                );
                jetSpark.noGravity = true;
            }

            // ===== 第三层：爆心热浪环，瞬间撑开 =====
            for (int i = 0; i < 18; i++)
            {
                float angle = MathHelper.TwoPi * i / 18f;
                Vector2 dir = angle.ToRotationVector2();

                Dust ring = Dust.NewDustPerfect(
                    center + dir * 10f,
                    DustID.Torch,
                    dir * Main.rand.NextFloat(3f, 6f),
                    0,
                    Color.Lerp(new Color(255, 180, 70), new Color(255, 80, 20), Main.rand.NextFloat()),
                    Main.rand.NextFloat(1.4f, 2.2f)
                );
                ring.noGravity = true;
                ring.fadeIn = 2.5f;
            }

            // ===== 第四层：高温气泡爆裂感，用 CustomSpark 做太阳表面鼓泡炸开 =====
            float t = Main.GlobalTimeWrappedHourly * 9f + Projectile.identity * 0.51f;
            for (int i = 0; i < 12; i++)
            {
                Vector2 dir = Main.rand.NextVector2CircularEdge(1f, 1f);
                Vector2 spawnPos = center + dir * Main.rand.NextFloat(2f, 14f);
                Vector2 velocity = dir * Main.rand.NextFloat(3.5f, 8.5f) + Main.rand.NextVector2Circular(1.2f, 1.2f);

                Particle spark = new CustomSpark(
                    spawnPos,
                    velocity,
                    "CalamityMod/Particles/ProvidenceMarkParticle",
                    false,
                    Main.rand.Next(18, 28),
                    Main.rand.NextFloat(0.95f, 1.35f),
                    Color.Lerp(new Color(255, 240, 165), new Color(255, 115, 25), 0.5f + 0.5f * (float)Math.Sin(t + i * 0.4f)),
                    new Vector2(Main.rand.NextFloat(1.25f, 1.7f), Main.rand.NextFloat(0.32f, 0.55f)),
                    true,
                    false,
                    Main.rand.NextFloat(-0.18f, 0.18f),
                    false,
                    false,
                    0.1f
                );

                GeneralParticleHandler.SpawnParticle(spark);
            }

            // ===== 第五层：近距离爆闪碎火，补一点极快的小碎屑 =====
            for (int i = 0; i < 20; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(1f, 1f) * Main.rand.NextFloat(4.5f, 12f);

                Dust ember = Dust.NewDustPerfect(
                    center,
                    DustID.Torch,
                    vel,
                    0,
                    Color.Lerp(Color.Yellow, Color.OrangeRed, Main.rand.NextFloat()),
                    Main.rand.NextFloat(1.1f, 1.6f)
                );
                ember.noGravity = true;
                ember.fadeIn = 2.5f;
            }

            for (int i = 0; i < 10; i++)
            {
                Vector2 smokeVelocity = Vector2.UnitX.RotatedByRandom(Math.PI).RotatedBy(Projectile.velocity.ToRotation()) * Main.rand.NextFloat(2.4f, 6.2f);
                Dust smoke = Dust.NewDustPerfect(
                    center,
                    DustID.Smoke,
                    smokeVelocity,
                    0,
                    Color.Lerp(Color.Gray, Color.DarkGray, Main.rand.NextFloat(0.15f, 0.7f)),
                    Main.rand.NextFloat(1.2f, 1.65f));
                smoke.noGravity = true;
            }
        }










    }
}
