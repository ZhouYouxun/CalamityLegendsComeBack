using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Projectiles.BaseProjectiles;
using CalamityMod;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using CalamityMod.Particles;
using Terraria.DataStructures;
using CalamityMod.Buffs.DamageOverTime;
using Terraria.Audio;

namespace CalamityLegendsComeBack.Weapons.SHPC.EXSkill
{
    internal class TEM00LeftSuperLazer : BaseLaserbeamProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.NewWeapons.EAfterDog";

        public int OwnerIndex
        {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        public override float MaxScale => 0.7f;
        public override float MaxLaserLength => 2000f;
        public override float Lifetime => 1800000; // 持续 X 帧
        public override Color LaserOverlayColor => new Color(90, 200, 255, 130); // 科技蓝(带Alpha)
        public override Color LightCastColor => Color.White;
        public override Texture2D LaserBeginTexture => Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
        public override Texture2D LaserMiddleTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/AresLaserBeamMiddle", AssetRequestMode.ImmediateLoad).Value;
        public override Texture2D LaserEndTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/AresLaserBeamEnd", AssetRequestMode.ImmediateLoad).Value;
        public override string Texture => "CalamityMod/Projectiles/Boss/AresLaserBeamStart";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 5;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 180000;
            Projectile.usesLocalNPCImmunity = true; // 弹幕使用本地无敌帧
            Projectile.localNPCHitCooldown = 1; // 无敌帧冷却时间为1帧
        }



        // 替换 AttachToSomething() 为下面版本：
        // 功能：每帧检查父弹幕，父存在 -> 刷新 timeLeft（保持永生）；父不存在 -> Kill()
        // 已使用类内的 OwnerIndex 属性 (ai[0]) 与之前的类型兼容判断。
        public override void AttachToSomething()
        {
            int ownerIndex = OwnerIndex; // 从 ai[0] 读取父弹幕索引
                                         // 索引越界或无效直接自毁
            if (!ownerIndex.WithinBounds(Main.maxProjectiles))
            {
                Projectile.Kill();
                return;
            }

            Projectile ownerProj = Main.projectile[ownerIndex];

            // 允许的父弹幕类型（兼容旧类型与 TEM00Left）
            int exType = ModContent.ProjectileType<NL_SHPC_EXWeapon>();

            // 父弹幕存在且活跃且为我们接受的类型 -> 绑定位置和朝向，并刷新寿命
            if (ownerProj != null && ownerProj.active && ownerProj.type == exType)
            {
                // 绑定到父弹幕的“枪口”位置与朝向（与你原实现一致）
                Projectile.Center = ownerProj.Center + (ownerProj.rotation - MathHelper.PiOver4).ToRotationVector2() * 18f;
                Projectile.rotation = (ownerProj.rotation );

                // --------- 关键：只要父弹幕存在，就不断刷新自身寿命，确保永远不死 ---------
                // 这里把 timeLeft 设为 2，基类/引擎会每帧调用 AttachToSomething 并把 timeLeft 再设回 2
                // 因此只要父弹幕活着，本弹幕就会一直存活；父消失则下方分支会 Kill()
                Projectile.timeLeft = 2;

                // （可选）如果你需要把“持续存在”状态同步到客户端，可在首次绑定时做一次 netUpdate
                // 但频繁 netUpdate 会增加流量，所以这里不每帧调用
            }
            else
            {
                // 父弹幕不存在/类型不匹配 -> 自毁
                // 为了多人同步，先设置 netUpdate 再 Kill()
                Projectile.netUpdate = true;
                Projectile.Kill();
            }
        }


        public override void UpdateLaserMotion()
        {
            // 不再寻敌，逻辑完全交给 AttachToSomething()
            Projectile.velocity = Projectile.rotation.ToRotationVector2();
        }


        // 命中VFX节流（避免每帧命中都刷爆）
        private int impactVfxCooldown;


        public override void OnSpawn(IEntitySource source)
        {
            impactVfxCooldown = 0; // 初始化节流计时器
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 仍可保留你的Debuff
            target.AddBuff(ModContent.BuffType<MiracleBlight>(), 300); // 超位崩解
            SoundEngine.PlaySound(SoundID.Item132 with { Volume = 0.8f, Pitch = -0.0f }, Projectile.Center);

            //// 8帧节流，避免localNPCHitCooldown=1导致每帧都生成海量粒子
            //if (impactVfxCooldown > 0)
            //    return;
            //impactVfxCooldown = 8;

            // ===== 计算与光束轴对齐的“命中点” =====
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            float t = Vector2.Dot(target.Center - Projectile.Center, dir);
            t = MathHelper.Clamp(t, 0f, LaserLength); // 限制在光束范围
            Vector2 hitPos = Projectile.Center + dir * t;
            Vector2 nrm = dir.RotatedBy(MathHelper.PiOver2); // 法线（用于“方”感的左右分布）

            // ===== 颜色与随机工具 =====
            Color coreC = new Color(120, 220, 255) * 1.45f; // 亮一些的科技蓝
            Color edgeC = new Color(90, 200, 255) * 1.35f; // 稍暗边缘蓝
            Color flashC = new Color(170, 235, 255) * 1.60f; // 接近白的电光蓝

            // ===== Ⅰ. 破碎环 =====
            int ringCount = Main.rand.Next(24, 33); // 24~32
            for (int i = 0; i < ringCount; i++)
            {
                float ang = MathHelper.TwoPi * i / ringCount + Main.rand.NextFloat(-0.08f, 0.08f);
                Vector2 v = ang.ToRotationVector2() * Main.rand.NextFloat(4f, 10f);
                SquareParticle sq = new SquareParticle(
                    hitPos + v.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(4f, 12f), // 初始离散
                    v,
                    false,
                    26 + Main.rand.Next(16),               // 26~41帧
                    1.6f + Main.rand.NextFloat(0.9f),     // 1.6~2.5
                    i % 3 == 0 ? flashC : edgeC
                );
                GeneralParticleHandler.SpawnParticle(sq);
            }

            // ===== Ⅱ. 切削流（沿光束切线，强调“方截面”）=====
            int shearCount = Main.rand.Next(10, 15); // 10~14
            for (int i = 0; i < shearCount; i++)
            {
                // 在法线两侧，沿切线（±nrm）+少量dir抖动
                float side = Main.rand.NextBool() ? 1f : -1f;
                Vector2 vel = nrm * side * Main.rand.NextFloat(5f, 11f) + dir * Main.rand.NextFloat(-2.5f, 2.5f);
                SquareParticle sq = new SquareParticle(
                    hitPos + nrm * side * Main.rand.NextFloat(6f, 14f),
                    vel,
                    false,
                    22 + Main.rand.Next(10),               // 22~31帧
                    1.7f + Main.rand.NextFloat(0.7f),     // 1.7~2.4
                    edgeC
                );
                GeneralParticleHandler.SpawnParticle(sq);
            }

            // ===== Ⅲ. 核心碎片（高亮、短命、速度快）=====
            int coreCount = Main.rand.Next(10, 15); // 10~14
            for (int i = 0; i < coreCount; i++)
            {
                Vector2 v = Main.rand.NextVector2Circular(8f, 8f) + dir * Main.rand.NextFloat(2f, 6f);
                SquareParticle sq = new SquareParticle(
                    hitPos + Main.rand.NextVector2Circular(6f, 6f),
                    v,
                    false,
                    14 + Main.rand.Next(6),                // 14~19帧
                    1.4f + Main.rand.NextFloat(0.6f),     // 1.4~2.0
                    flashC
                );
                GeneralParticleHandler.SpawnParticle(sq);
            }

            // 可选：轻音效（与小激光区分开）
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, hitPos);
        }


        public override void ExtraBehavior()
        {
            //if (impactVfxCooldown > 0) impactVfxCooldown--; // 递减命中爆破节流


            // ===== 科技蓝主光照 =====
            Lighting.AddLight(Projectile.Center, 0.10f, 0.28f, 0.55f); // 柔和科技蓝环境光

            // ===== 预计算束体信息 =====
            if (Projectile.velocity == Vector2.Zero)
                return; // 安全保护：没有方向时不生成粒子

            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 nrm = dir.RotatedBy(MathHelper.PiOver2); // 右手法线（±就是两侧边缘）
            float halfWidth = (Projectile.scale * Projectile.width + 180f) * 0.5f; // 与 LaserWidthFunction 对齐（你的宽函数）

            // 沿着光束取样的步长（越小越密）
            float step = 56f; // 经验值：适中密度（可 40~72 调整）
            int sampleCount = (int)(LaserLength / step);
            sampleCount = Utils.Clamp(sampleCount, 3, 24); // 防失控

            // 末端位置（便于在端点加额外亮点）
            Vector2 endPos = Projectile.Center + dir * LaserLength;

            // ===== 1) 光束“方边”——SquareParticle 落在两侧边缘 =====
            for (int i = 1; i <= sampleCount; i++)
            {
                if (!Main.rand.NextBool(5)) // 1/5 概率生成（节流）
                    continue;

                float t = i / (float)(sampleCount + 1);
                Vector2 basePos = Vector2.Lerp(Projectile.Center, endPos, t);

                // 在 ±法线方向偏移到“边缘”，做几何轮廓
                Vector2 leftEdge = basePos - nrm * halfWidth * Main.rand.NextFloat(0.88f, 1.08f);
                Vector2 rightEdge = basePos + nrm * halfWidth * Main.rand.NextFloat(0.88f, 1.08f);

                // 让方块粒子沿着“边缘切线”轻漂，像是能量流经棱边
                Vector2 glideVelL = dir * Main.rand.NextFloat(0.4f, 1.0f) + nrm * Main.rand.NextFloat(-0.3f, 0.3f);
                Vector2 glideVelR = dir * Main.rand.NextFloat(0.4f, 1.0f) + nrm * Main.rand.NextFloat(-0.3f, 0.3f);

                // 左侧
                SquareParticle sqL = new SquareParticle(
                    leftEdge,
                    glideVelL,
                    false,
                    24 + Main.rand.Next(8),             // 24~31 帧
                    1.6f + Main.rand.NextFloat(0.7f),   // 1.6~2.3
                    new Color(90, 200, 255) * 1.35f     // 科技蓝，略增亮
                );
                GeneralParticleHandler.SpawnParticle(sqL);

                // 右侧
                if (Main.rand.NextBool()) // 1/2 再补一侧，避免过密
                {
                    SquareParticle sqR = new SquareParticle(
                        rightEdge,
                        glideVelR,
                        false,
                        24 + Main.rand.Next(8),
                        1.6f + Main.rand.NextFloat(0.7f),
                        new Color(90, 200, 255) * 1.35f
                    );
                    GeneralParticleHandler.SpawnParticle(sqR);
                }
            }

            // ===== 2) 光束“脉冲核”——GlowOrb 沿中心线/端点闪烁 =====
            // 中轴脉冲：稀疏而快消，体现能量跳动
            if (Main.rand.NextBool(3))
            {
                float centerT = Main.rand.NextFloat(0.08f, 0.92f);
                Vector2 pulsePos = Vector2.Lerp(Projectile.Center, endPos, centerT);
                GlowOrbParticle orb = new GlowOrbParticle(
                    pulsePos,
                    Vector2.Zero,
                    false,
                    5,                    // 快速消散
                    0.95f + Main.rand.NextFloat(0.25f),
                    new Color(170, 235, 255), // 偏白的浅青蓝
                    true,
                    false,
                    true
                );
                GeneralParticleHandler.SpawnParticle(orb);
            }

            // 端点高亮：每隔几帧给端点加一个“爆点”，强调终端能量
            if (Main.rand.NextBool(4))
            {
                GlowOrbParticle tip = new GlowOrbParticle(
                    endPos,
                    -dir * Main.rand.NextFloat(0.25f, 0.6f), // 轻微回喷
                    false,
                    7,
                    1.15f + Main.rand.NextFloat(0.25f),
                    new Color(120, 210, 255),
                    true,
                    false,
                    true
                );
                GeneralParticleHandler.SpawnParticle(tip);
            }
        }


        public float LaserWidthFunction(float completionRatio, Vector2 vertexPos) => Projectile.scale * Projectile.width + 180;

        public static Color LaserColorFunction(float completionRatio, Vector2 vertexPos)
        {
            // 轻呼吸 + 扭曲，色相在浅青(靠近白) 与 天蓝之间摆动
            float osc = (float)Math.Sin(Main.GlobalTimeWrappedHourly * -3.2f + completionRatio * 23f) * 0.5f + 0.5f;
            Color c1 = new Color(170, 235, 255);  // 浅青蓝（近白的科技光）
            Color c2 = new Color(70, 160, 255);   // 天蓝（偏冷，金属感）
            return Color.Lerp(c1, c2, osc * 0.75f);
        }


        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.velocity == Vector2.Zero)
                return false;

            Vector2 laserEnd = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * LaserLength;
            Vector2[] baseDrawPoints = new Vector2[8];
            for (int i = 0; i < baseDrawPoints.Length; i++)
                baseDrawPoints[i] = Vector2.Lerp(Projectile.Center, laserEnd, i / (float)(baseDrawPoints.Length - 1f));

            GameShaders.Misc["CalamityMod:ArtemisLaser"].UseColor(new Color(90, 200, 255));
            GameShaders.Misc["CalamityMod:ArtemisLaser"].UseImage1("Images/Extra_191"); // 横向的黑色背景纹理
            GameShaders.Misc["CalamityMod:ArtemisLaser"].UseImage2("Images/Misc/Perlin");

            PrimitiveRenderer.RenderTrail(baseDrawPoints, new(LaserWidthFunction, LaserColorFunction, shader: GameShaders.Misc["CalamityMod:ArtemisLaser"]), 64);
            return false;
        }


    }
}
