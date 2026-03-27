using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Dusts;
using CalamityMod.Enums;
using CalamityMod.Items.Materials;
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

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    public class DarkPlasmaEffect : DefaultEffect
    {
        public override int EffectID => 32;

        public override int AmmoType => ModContent.ItemType<DarkPlasma>();

        public override Color ThemeColor => new Color(20, 20, 20);
        public override Color StartColor => new Color(80, 80, 80);
        public override Color EndColor => new Color(5, 5, 5);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 1.5f;

        // 自定义计时器
        private float portalTimer;
        private int lifeTimer;

        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            portalTimer = 0f;
            lifeTimer = 0;

            projectile.velocity *= 0.8f;
            projectile.tileCollide = false;
            projectile.penetrate = -1;

            // 出生时先来一圈黑暗塌缩感
            for (int i = 0; i < 14; i++)
            {
                Vector2 dir = Main.rand.NextVector2CircularEdge(1f, 1f);
                Vector2 spawnPos = projectile.Center + dir * Main.rand.NextFloat(18f, 46f);
                Vector2 vel = (projectile.Center - spawnPos).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(1.5f, 4.5f);

                Dust dust = Dust.NewDustPerfect(
                    spawnPos,
                    ModContent.DustType<VoidDustInverted>(),
                    vel,
                    0,
                    Color.Lerp(new Color(90, 90, 90), Color.Black, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.8f, 1.35f)
                );
                dust.noGravity = true;
                dust.noLightEmittence = true;
            }

            Particle openPulse = new CustomPulse(
                projectile.Center,
                Vector2.Zero,
                new Color(35, 35, 35),
                "CalamityMod/Particles/SmallBloom",
                Vector2.One,
                Main.rand.NextFloat(-0.15f, 0.15f),
                0.9f,
                0f,
                22,
                false
            );
            GeneralParticleHandler.SpawnParticle(openPulse);
        }

        // ================= AI =================
        public override void AI(Projectile projectile, Player owner)
        {
            portalTimer += 0.03f;
            lifeTimer++;

            // ===== 缓慢追踪鼠标 =====
            Vector2 targetPos = Main.MouseWorld;
            Vector2 toTarget = targetPos - projectile.Center;
            float dist = toTarget.Length();

            if (dist > 10f)
            {
                Vector2 dir = toTarget / dist;
                projectile.velocity = (projectile.velocity * 25f + dir * 1.8f) / 26f;
            }

            projectile.rotation += 0.045f;

            // ===== 吸附敌人 =====
            float range = 320f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage)
                    continue;

                float distance = Vector2.Distance(projectile.Center, npc.Center);
                if (distance < range)
                {
                    Vector2 pull = (projectile.Center - npc.Center).SafeNormalize(Vector2.UnitY);
                    float strength = MathHelper.Lerp(0.04f, 0.28f, 1f - distance / range);

                    npc.velocity += pull * strength;

                    // 吸收轨迹 dust
                    if (Main.rand.NextBool(4))
                    {
                        Vector2 start = npc.Center + Main.rand.NextVector2Circular(24f, 24f);
                        Vector2 vel = (projectile.Center - start).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(1.5f, 5f);

                        Dust dust = Dust.NewDustPerfect(
                            start,
                            ModContent.DustType<VoidDustInverted>(),
                            vel,
                            0,
                            Color.Lerp(new Color(100, 100, 100), Color.Black, Main.rand.NextFloat()),
                            Main.rand.NextFloat(0.85f, 1.35f)
                        );
                        dust.noGravity = true;
                        dust.noLightEmittence = true;
                    }

                    // 低频持续伤害
                    if (Main.rand.NextBool(8))
                        npc.StrikeNPC(npc.CalculateHitInfo(projectile.damage / 6, 0));
                }
            }

            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 back = -forward;

            // ===== 1. 黑色主尾焰：CustomSpark =====
            if (Main.myPlayer == projectile.owner && Main.rand.NextBool(2))
            {
                Particle spark = new CustomSpark(
                    projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    back.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.8f, 2.8f),
                    "CalamityMod/Particles/GlowSpark2",
                    false,
                    Main.rand.Next(9, 14),
                    Main.rand.NextFloat(0.02f, 0.045f),
                    Color.Black * 0.75f,
                    new Vector2(Main.rand.NextFloat(0.9f, 1.4f), Main.rand.NextFloat(0.3f, 0.65f)),
                    false,
                    shrinkSpeed: 1.05f
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }

            // ===== 4. 黑烟吸收层：HeavySmokeParticle =====
            if (Main.rand.NextBool(3))
            {
                Particle smoke = new HeavySmokeParticle(
                    projectile.Center + Main.rand.NextVector2Circular(18f, 18f),
                    back.RotatedByRandom(0.7f) * Main.rand.NextFloat(0.15f, 1.1f),
                    Main.rand.NextBool(2) ? new Color(15, 15, 15) : new Color(45, 45, 45),
                    Main.rand.Next(18, 30),
                    Main.rand.NextFloat(0.55f, 1.05f),
                    0.42f,
                    Main.rand.NextFloat(-0.06f, 0.06f),
                    false
                );
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            // ===== 5. 轨道虚空尘（随寿命增强）=====

            // 👉 归一化寿命（越接近死亡 → 越接近1）
            float lifeFactor = 1f - projectile.timeLeft / 420f;
            lifeFactor = MathHelper.Clamp(lifeFactor, 0f, 1f);

            // 👉 平滑强化（避免前期突变）
            lifeFactor = (float)Math.Pow(lifeFactor, 1.4f);

            // ===== 频率：最多提升到原来的 ~3倍 =====
            int spawnChance = (int)MathHelper.Lerp(2f, 1f, lifeFactor); // 2→1（更容易触发）

            if (Main.rand.NextBool(spawnChance))
            {
                Vector2 circle = Main.rand.NextVector2CircularEdge(1f, 1f);

                // ===== 距离范围：逐渐扩大，但有限制 =====
                float minDist = MathHelper.Lerp(20f, 40f, lifeFactor);
                float maxDist = MathHelper.Lerp(90f, 160f, lifeFactor);

                Vector2 spawn = projectile.Center + circle * Main.rand.NextFloat(minDist, maxDist);

                // ===== 吸引强度：明显增强，但封顶 =====
                float velStrength = MathHelper.Lerp(0.045f, 0.18f, lifeFactor);

                Vector2 vel = (projectile.Center - spawn) * velStrength;

                Dust dust = Dust.NewDustPerfect(
                    spawn,
                    ModContent.DustType<VoidDustInverted>(),
                    vel,
                    0,
                    Main.rand.NextBool(3) ? new Color(120, 120, 120) : Color.Black,
                    MathHelper.Lerp(0.7f, 1.8f, lifeFactor) // 尺寸也略增强
                );

                dust.noGravity = true;
                dust.noLightEmittence = true;
            }


            // ===== 2. 黑色高速裂流（随寿命外扩增强）=====

            // 👉 同一套寿命因子（和黑洞吸附形成对照）
            lifeFactor = 1f - projectile.timeLeft / 420f;
            lifeFactor = MathHelper.Clamp(lifeFactor, 0f, 1f);

            // 👉 这里用更激进的曲线（爆裂感）
            lifeFactor = (float)Math.Pow(lifeFactor, 1.2f);

            // ===== 触发频率提升（最多≈2倍）=====
            int interval = (int)MathHelper.Lerp(3f, 1f, lifeFactor);

            if (lifeTimer % interval == 0)
            {
                // ===== 生成范围扩大（向外爆）=====
                float spawnRadius = MathHelper.Lerp(12f, 38f, lifeFactor);

                Vector2 spawnPos = projectile.Center + Main.rand.NextVector2Circular(spawnRadius, spawnRadius);

                // ===== 扩散角度变大 =====
                float spread = MathHelper.Lerp(0.65f, 1.4f, lifeFactor);

                // ===== 速度暴涨（核心）=====
                float speedMin = MathHelper.Lerp(1.2f, 3.5f, lifeFactor);
                float speedMax = MathHelper.Lerp(4.8f, 10.5f, lifeFactor);

                Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(speedMin, speedMax);

                Particle altSpark = new AltSparkParticle(
                    spawnPos,
                    vel,
                    false,
                    (int)MathHelper.Lerp(10f, 22f, lifeFactor), // 生命周期略增加
                    MathHelper.Lerp(0.7f, 1.35f, lifeFactor),   // 尺寸增强
                    Color.Black
                );

                GeneralParticleHandler.SpawnParticle(altSpark);
            }


            // ===== 7. 中心暗核呼吸 =====
            if (lifeTimer % 5 == 0)
            {
                Particle corePulse = new CustomPulse(
                    projectile.Center,
                    Vector2.Zero,
                    new Color(20, 20, 20),
                    "CalamityMod/Particles/SmallBloom",
                    Vector2.One,
                    Main.rand.NextFloat(-0.1f, 0.1f),
                    0.4f,
                    0f,
                    14,
                    false
                );
                GeneralParticleHandler.SpawnParticle(corePulse);
            }

            Lighting.AddLight(projectile.Center, new Vector3(0.06f, 0.06f, 0.06f));
        }

        // ================= ModifyHitNPC =================
        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
        }

        // ================= OnHitNPC =================
        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
        }

        // ================= OnKill =================
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            int projIndex = Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<NewLegendSHPE>(),
                projectile.damage,
                projectile.knockBack,
                projectile.owner
            );

            Projectile proj = Main.projectile[projIndex];
            proj.width = 250;
            proj.height = 250;

            // ===== 爆炸前两层黑核 =====
            for (int i = 0; i < 2; i++)
            {
                Particle blackCore = new CustomPulse(
                    projectile.Center,
                    Vector2.Zero,
                    Color.Black,
                    "CalamityMod/Particles/SmallBloom",
                    Vector2.One,
                    Main.rand.NextFloat(-0.25f, 0.25f),
                    2.4f + i * 0.55f,
                    0f,
                    70,
                    false
                );
                GeneralParticleHandler.SpawnParticle(blackCore);
            }

            // ===== 灰白外爆 =====
            Particle outerBloom = new CustomPulse(
                projectile.Center,
                Vector2.Zero,
                new Color(135, 135, 135),
                "CalamityMod/Particles/LargeBloom",
                Vector2.One,
                Main.rand.NextFloat(-0.25f, 0.25f),
                0f,
                0.92f,
                16,
                false
            );
            outerBloom.DrawLayer = GeneralDrawLayer.AfterEverything;
            GeneralParticleHandler.SpawnParticle(outerBloom);

            // ===== 暗雾爆开 =====
            for (int i = 0; i < 14; i++)
            {
                Particle smoke = new HeavySmokeParticle(
                    projectile.Center,
                    Main.rand.NextVector2Circular(4.5f, 4.5f),
                    Main.rand.NextBool(2) ? Color.Black : new Color(50, 50, 50),
                    Main.rand.Next(24, 40),
                    Main.rand.NextFloat(0.85f, 1.45f),
                    0.4f,
                    Main.rand.NextFloat(-0.08f, 0.08f),
                    false
                );
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            // ===== 黑色裂流 =====
            for (int i = 0; i < 18; i++)
            {
                Particle altSpark = new AltSparkParticle(
                    projectile.Center,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.5f, 8.5f),
                    false,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.75f, 1.2f),
                    Color.Black
                );
                GeneralParticleHandler.SpawnParticle(altSpark);
            }

            // ===== 长尾黑色火花 =====
            for (int i = 0; i < 10; i++)
            {
                Particle customSpark = new CustomSpark(
                    projectile.Center,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 6f),
                    "CalamityMod/Particles/GlowSpark2",
                    false,
                    Main.rand.Next(10, 15),
                    Main.rand.NextFloat(0.02f, 0.05f),
                    Color.Black * 0.9f,
                    new Vector2(Main.rand.NextFloat(1.1f, 1.8f), Main.rand.NextFloat(0.35f, 0.7f)),
                    false,
                    shrinkSpeed: 1.08f
                );
                GeneralParticleHandler.SpawnParticle(customSpark);
            }

            // ===== 虚空尘大爆散 =====
            for (int i = 0; i < 28; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    projectile.Center,
                    ModContent.DustType<VoidDustInverted>(),
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.5f, 8.5f),
                    0,
                    Main.rand.NextBool(3) ? new Color(125, 125, 125) : Color.Black,
                    Main.rand.NextFloat(0.9f, 1.6f)
                );
                dust.noGravity = true;
                dust.noLightEmittence = true;
            }

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.7f, Pitch = -0.45f }, projectile.Center);
        }

        // ================= PreDraw =================
        public override void PreDraw(Projectile projectile, Player owner, SpriteBatch spriteBatch)
        {
            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleVortex").Value;
            Texture2D bloom = TextureAssets.Extra[98].Value;

            Vector2 drawPos = projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 bloomOrigin = bloom.Size() * 0.5f;

            float scale01;

            if (projectile.timeLeft > 360)
                scale01 = Utils.GetLerpValue(420f, 360f, projectile.timeLeft, true);
            else if (projectile.timeLeft >= 60)
                scale01 = 1f;
            else
                scale01 = Utils.GetLerpValue(0f, 60f, projectile.timeLeft, true);


            MiscShaderData shader;

            if (GameShaders.Misc.TryGetValue("CalamityMod:BasicTrail", out shader))
            {
                shader.UseColor(new Color(15, 15, 20));
                shader.Apply();
            }

            for (int i = 0; i < 13; i++)
            {
                Color c = Color.Lerp(new Color(30, 30, 30), Color.Black, i * 0.1f);
                c.A = 0;

                Main.EntitySpriteDraw(
                    texture,
                    drawPos,
                    null,
                    c * scale01,
                    projectile.rotation * 3f - i * 0.15f,
                    origin,
                    MathHelper.Clamp(scale01 * 0.4f - i * 0.025f, 0f, 5f),
                    SpriteEffects.None
                );
            }





            {
                // ===== FBM黑洞辅助层（固定朝向 + 呼吸）=====

                // 贴图
                Texture2D fbm = ModContent.Request<Texture2D>(
                    "CalamityLegendsComeBack/Weapons/SHPC/Effects/DPreDog/fbmnoise2_007"
                ).Value;

                Vector2 fbmOrigin = fbm.Size() * 0.5f;

                // ===== 呼吸节奏 =====
                float breathe = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 1.2f) * 0.5f + 0.5f;

                // ===== 缩放（缓慢变化）=====
                float fbmScale = (0.2f + breathe * 0.25f) * scale01;

                // ===== 亮度变化 =====
                float brightness = 0.55f + breathe * 0.45f;

                // ===== 颜色（橙色高亮描边风格）=====
                Color fbmColor = new Color(20, 20, 20) * brightness;
                fbmColor.A = 0;

                // ===== 完全摆正（不随弹幕旋转）=====
                float rotation = 0f;

                // ===== 位置 =====
                Vector2 pos = projectile.Center - Main.screenPosition;

                // ===== 主体绘制 =====
                Main.EntitySpriteDraw(
                    fbm,
                    pos,
                    null,
                    fbmColor,
                    rotation,
                    fbmOrigin,
                    fbmScale,
                    SpriteEffects.None
                );

                // ===== 轻微外描边（强化橙色高亮）=====
                for (int i = 0; i < 4; i++)
                {
                    Vector2 offset = new Vector2(1.5f, 0).RotatedBy(i * MathHelper.PiOver2);

                    Main.EntitySpriteDraw(
                        fbm,
                        pos + offset,
                        null,
                        fbmColor * 0.4f,
                        rotation,
                        fbmOrigin,
                        fbmScale,
                        SpriteEffects.None
                    );
                }
            }






            // ===== 新增：外圈黑色晕层 =====
            for (int i = 0; i < 4; i++)
            {
                float factor = 1f - i * 0.16f;
                Color c = new Color(20, 20, 20, 0) * 0.45f * factor;

                Main.EntitySpriteDraw(
                    bloom,
                    drawPos,
                    null,
                    c,
                    -projectile.rotation * (0.8f + i * 0.18f),
                    bloomOrigin,
                    (0.42f + i * 0.12f) * scale01,
                    SpriteEffects.None
                );
            }

            // ===== 新增：中心灰白呼吸点 =====
            for (int i = 0; i < 3; i++)
            {
                float pulse = 0.88f + (float)System.Math.Sin(portalTimer * 5f + i * 0.7f) * 0.12f;
                Color c = new Color(105, 105, 105, 0) * 0.22f * (1f - i * 0.18f);

                Main.EntitySpriteDraw(
                    bloom,
                    drawPos,
                    null,
                    c,
                    projectile.rotation * (1.5f + i * 0.35f),
                    bloomOrigin,
                    (0.16f + i * 0.07f) * pulse * scale01,
                    SpriteEffects.None
                );
            }
        }
    }
}