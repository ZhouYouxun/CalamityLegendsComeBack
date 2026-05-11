using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod;
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

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog.SZPC
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
            projectile.timeLeft = Math.Max(projectile.timeLeft, 420);

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
            float range = 1800f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage)
                    continue;

                float distance = Vector2.Distance(projectile.Center, npc.Center);
                if (distance < range)
                {
                    Vector2 pull = (projectile.Center - npc.Center).SafeNormalize(Vector2.UnitY);
                    float strength = MathHelper.Lerp(0.08f, 0.95f, 1f - distance / range);

                    npc.velocity += pull * strength;
                    if (distance < 95f)
                        npc.velocity *= 0.92f;

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

                    // 稳定持续伤害
                    if (lifeTimer % 8 == 0)
                        npc.StrikeNPC(npc.CalculateHitInfo(Math.Max(1, projectile.damage / 9), 0));
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

            TryReleaseDevourOrbs(projectile, owner, lifeFactor);
            Lighting.AddLight(projectile.Center, new Vector3(0.06f, 0.06f, 0.06f));
        }

        private static void TryReleaseDevourOrbs(Projectile projectile, Player owner, float lifeFactor)
        {
            if (projectile.owner != Main.myPlayer)
                return;

            int interval = (int)MathHelper.Lerp(42f, 7f, MathHelper.Clamp(lifeFactor, 0f, 1f));
            interval = Math.Max(5, interval);
            if (projectile.timeLeft % interval != 0)
                return;

            int count = lifeFactor > 0.72f ? 2 : 1;
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                Vector2 velocity = direction * Main.rand.NextFloat(7f, MathHelper.Lerp(11f, 19f, lifeFactor));
                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center + direction * Main.rand.NextFloat(14f, 28f),
                    velocity,
                    ModContent.ProjectileType<EndlessDevourJavOrbSmall>(),
                    (int)(projectile.damage * 0.45f),
                    projectile.knockBack * 0.35f,
                    projectile.owner,
                    0f,
                    Main.rand.NextFloat(MathHelper.TwoPi));
            }
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
            SpawnDarkPlasmaAccretionDeath(projectile, owner);
            SpawnDarkPlasmaDeathDamage(projectile);
            SpawnDarkPlasmaDeathOrbs(projectile);
            PlayDarkPlasmaDeathSounds(projectile);
        }

        private static void SpawnDarkPlasmaAccretionDeath(Projectile projectile, Player owner)
        {
            owner.SetScreenshake(8.5f);
            float power = 1.5f;
            float diskRotation = projectile.velocity.SafeNormalize(Vector2.UnitX).ToRotation() + Main.rand.NextFloat(-0.35f, 0.35f);

            for (int i = 0; i < 55; i++)
            {
                Color useColor = GetRandomDarkPlasmaBurstColor(owner);
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 radial = new Vector2((float)Math.Cos(angle) * 1.85f, (float)Math.Sin(angle) * 0.42f).RotatedBy(diskRotation).SafeNormalize(Vector2.UnitX);
                Vector2 tangent = radial.RotatedBy(MathHelper.PiOver2);
                Vector2 velocity = (tangent * Main.rand.NextFloat(4f, 11f) + radial * Main.rand.NextFloat(-2.6f, 4.6f)) * power;

                Dust dust = Dust.NewDustPerfect(
                    projectile.Center,
                    Main.rand.NextBool(6) ? ModContent.DustType<VoidDustInverted>() : ModContent.DustType<VoidDust>(),
                    velocity);
                dust.noGravity = true;
                dust.noLightEmittence = true;
                dust.scale = Main.rand.NextFloat(1.75f, 2.25f) * power;
                dust.color = useColor;

                if (i % 2 == 0)
                {
                    Color sparkColor = GetDarkPlasmaVisibleBurstColor(owner);
                    Vector2 sparkVelocity = new Vector2(0f, -40f * power)
                        .RotatedByRandom(100f)
                        .RotatedBy(diskRotation)
                        * Main.rand.NextFloat(0.1f, 1f);
                    Particle spark = new CustomSpark(
                        projectile.Center,
                        sparkVelocity,
                        "CalamityMod/Particles/Sparkle",
                        false,
                        40,
                        Main.rand.NextFloat(1.4f, 2.4f) * power,
                        sparkColor,
                        new Vector2(0.4f, 1.1f));
                    GeneralParticleHandler.SpawnParticle(spark);
                }
            }

            for (int i = 0; i < 3; i++)
            {
                Color useColor = GetDarkPlasmaVisibleBurstColor(owner);
                Particle softExplosion = new CustomPulse(
                    projectile.Center,
                    Vector2.Zero,
                    useColor,
                    "CalamityMod/Particles/SoftRoundExplosion",
                    Vector2.One,
                    Main.rand.NextFloat(-10f, 10f),
                    0f,
                    0.4f - i * 0.03f * power,
                    13);
                GeneralParticleHandler.SpawnParticle(softExplosion);
            }

            Particle accretionRing = new CustomPulse(
                projectile.Center,
                Vector2.Zero,
                Color.Black,
                "CalamityMod/Particles/BloomRing",
                new Vector2(1.75f, 0.48f),
                diskRotation,
                0.15f * power,
                2.5f * power,
                38,
                false);
            GeneralParticleHandler.SpawnParticle(accretionRing);

            int parts = 10;
            float rot = Main.rand.NextFloat(-9f, 9f);
            for (int i = 0; i < parts; i++)
            {
                Color useColor = GetDarkPlasmaVisibleBurstColor(owner);
                Vector2 smearVelocity = new Vector2(0f, -15f * (i % 2 == 0 ? 1.8f : 1f) * power)
                    .RotatedBy(i * MathHelper.TwoPi / parts)
                    .RotatedBy(rot)
                    .RotatedBy(diskRotation);
                smearVelocity.X *= 1.65f;
                smearVelocity.Y *= 0.58f;

                Particle smear = new CustomSpark(
                    projectile.Center,
                    smearVelocity,
                    "CalamityMod/Particles/VerticalSmearRagged",
                    false,
                    19,
                    3f * power,
                    useColor,
                    new Vector2(0.2f, 1f));
                GeneralParticleHandler.SpawnParticle(smear);
            }

            Particle blackCore = new CustomPulse(
                projectile.Center,
                Vector2.Zero,
                Color.Black,
                "CalamityMod/Particles/SmallBloom",
                Vector2.One,
                Main.rand.NextFloat(-10f, 10f),
                0f,
                1.2f * power,
                39,
                false);
            GeneralParticleHandler.SpawnParticle(blackCore);
        }

        private static Color GetRandomDarkPlasmaBurstColor(Player owner)
        {
            if (owner.shirtColor == Color.White && Main.rand.NextBool(10))
                return new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB);

            return Main.rand.Next(5) switch
            {
                0 => Color.Black,
                1 => new Color(8, 8, 8),
                2 => new Color(18, 18, 18),
                3 => new Color(34, 34, 34),
                _ => new Color(2, 2, 2)
            };
        }

        private static Color GetDarkPlasmaVisibleBurstColor(Player owner)
        {
            if (owner.shirtColor == Color.White && Main.rand.NextBool(10))
                return new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB);

            return Main.rand.Next(4) switch
            {
                0 => new Color(26, 26, 26),
                1 => new Color(38, 38, 38),
                2 => new Color(18, 18, 24),
                _ => new Color(46, 46, 52)
            };
        }

        private static void SpawnDarkPlasmaDeathDamage(Projectile projectile)
        {
            if (projectile.owner != Main.myPlayer)
                return;

            int projIndex = Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<global::CalamityLegendsComeBack.Weapons.SHPC.NewLegendSHPE>(),
                (int)(projectile.damage * 3f),
                projectile.knockBack,
                projectile.owner);

            if (!Main.projectile.IndexInRange(projIndex))
                return;

            Projectile proj = Main.projectile[projIndex];
            proj.width = 280;
            proj.height = 220;
            proj.Center = projectile.Center;
            proj.netUpdate = true;
        }

        private static void SpawnDarkPlasmaDeathOrbs(Projectile projectile)
        {
            if (projectile.owner != Main.myPlayer)
                return;

            for (int i = 0; i < 28; i++)
            {
                float angle = MathHelper.TwoPi * i / 28f + Main.rand.NextFloat(-0.08f, 0.08f);
                Vector2 direction = angle.ToRotationVector2();
                Vector2 orbVelocity = direction * Main.rand.NextFloat(10f, 22f);
                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center + direction * Main.rand.NextFloat(8f, 28f),
                    orbVelocity,
                    ModContent.ProjectileType<EndlessDevourJavOrbSmall>(),
                    (int)(projectile.damage * 0.72f),
                    projectile.knockBack * 0.5f,
                    projectile.owner,
                    0f,
                    Main.rand.NextFloat(MathHelper.TwoPi));
            }
        }

        private static void PlayDarkPlasmaDeathSounds(Projectile projectile)
        {
            for (int i = 0; i < 3; i++)
            {
                SoundStyle fire = new("CalamityMod/Sounds/Item/EarthMeteor");
                SoundEngine.PlaySound(fire with { Volume = 0.6f, Pitch = -0.1f * (i + 1), MaxInstances = 3 }, projectile.Center);
            }

            SoundStyle reflect = new("CalamityMod/Sounds/Item/ShadowboltReflect");
            SoundEngine.PlaySound(reflect with { Volume = 0.9f, Pitch = -0.4f }, projectile.Center);
        }

        // ================= PreDraw =================
        public override void PreDraw(Projectile projectile, Player owner, SpriteBatch spriteBatch)
        {
            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleVortex").Value;
            Texture2D bloom = TextureAssets.Extra[98].Value;
            Texture2D blade = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmearRagged").Value;

            Vector2 drawPos = projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 bloomOrigin = bloom.Size() * 0.5f;
            Vector2 bladeOrigin = blade.Size() * 0.5f;

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

            for (int i = 0; i < 7; i++)
            {
                Color c = Color.Lerp(new Color(30, 30, 30), Color.Black, i * 0.1f);
                c.A = 0;

                Main.EntitySpriteDraw(
                    texture,
                    drawPos,
                    null,
                    c * scale01 * 0.42f,
                    projectile.rotation * 3f - i * 0.15f,
                    origin,
                    MathHelper.Clamp(scale01 * 0.22f - i * 0.018f, 0f, 5f),
                    SpriteEffects.None
                );
            }



            // ===== 白色刀盘层：恢复旧版的亮刃切盘感 =====
            float bladePulse = 0.92f + (float)Math.Sin(portalTimer * 7f) * 0.08f;
            for (int i = 0; i < 8; i++)
            {
                float bladeRotation = projectile.rotation * -2.2f + i * MathHelper.TwoPi / 8f;
                Color bladeColor = Color.Lerp(Color.White, new Color(95, 95, 95), i / 7f);
                bladeColor.A = 0;

                Main.EntitySpriteDraw(
                    blade,
                    drawPos,
                    null,
                    bladeColor * scale01 * 0.34f,
                    bladeRotation,
                    bladeOrigin,
                    new Vector2(0.16f, 0.72f) * scale01 * bladePulse,
                    SpriteEffects.None
                );
            }

            Main.EntitySpriteDraw(
                bloom,
                drawPos,
                null,
                Color.White * 0.16f * scale01,
                projectile.rotation * 1.6f,
                bloomOrigin,
                0.18f * scale01 * bladePulse,
                SpriteEffects.None
            );



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
                float fbmScale = (0.12f + breathe * 0.13f) * scale01;

                // ===== 亮度变化 =====
                float brightness = 0.34f + breathe * 0.28f;

                // ===== 颜色（暗核描边风格）=====
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
                        fbmColor * 0.18f,
                        rotation,
                        fbmOrigin,
                        fbmScale,
                        SpriteEffects.None
                    );
                }
            }






            // ===== 新增：外圈黑色晕层 =====
            for (int i = 0; i < 2; i++)
            {
                float factor = 1f - i * 0.16f;
                Color c = new Color(10, 10, 10, 0) * 0.16f * factor;

                Main.EntitySpriteDraw(
                    bloom,
                    drawPos,
                    null,
                    c,
                    -projectile.rotation * (0.8f + i * 0.18f),
                    bloomOrigin,
                    (0.22f + i * 0.07f) * scale01,
                    SpriteEffects.None
                );
            }

            // ===== 新增：中心黑色呼吸点 =====
            for (int i = 0; i < 3; i++)
            {
                float pulse = 0.88f + (float)System.Math.Sin(portalTimer * 5f + i * 0.7f) * 0.12f;
                Color c = Color.Black * 0.14f * (1f - i * 0.18f);
                c.A = 0;

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
