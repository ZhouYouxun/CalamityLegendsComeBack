using System;
using System.Collections.Generic;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.Combo;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.General;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Enums;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.LeftClick
{
    /// <summary>
    /// 左键近战三段连招：上挑斩(0) → 劈落斩(1) → 冲刺贯穿(2)。
    /// 沿用镀金长喙 BaseSwordHoldoutProjectile 的持握挥舞骨架。第三段仍是冲刺动作（更大更响、必暴），
    /// 但全程不施加任何强制位移。命中生成一次短促的冥蚀冲击爆炸（不产生裂隙/触手/追踪灵魂）。
    /// 挥舞主视觉为矛尖的短黑色 shader 残像 + 少量黑砂粒。
    /// 按住左键期间再按右键 → 切换为回旋斩迹（本体自杀并生成 SpinHoldout）。
    /// </summary>
    public class UmbralNadirHoldout : BaseSwordHoldoutProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Nadir";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Upgrade/Nadir/UmbralNadirSpear";
        public override Item BaseItem => ModContent.GetModItem(ModContent.ItemType<UmbralNadir>()).Item;

        // === BaseSwordHoldout 配置 ===
        public override bool AlternateSwings => false;   // 连招阶段由 UmbralNadirPlayer 掌控
        public override bool useAttackSpeed => true;
        public override bool useMeleeSize => true;
        public override int swingWidth => 220;
        public override float lineCollisionLength => 200f;
        public override int AfterImageLength => 0;
        public override bool drawSwordTrail => false;    // 改由本类 PreDraw 绘制矛尖黑色 shader 残像

        /// <summary>当前挥砍所处连招阶段（0 上挑 / 1 劈落 / 2 冲刺）。</summary>
        private int stage;
        private bool hasExploded;
        private bool spawnedSpin;
        private bool waveSpawned;
        private int boltsSpawned;
        private bool smearSpawned;
        private Particle voidSmear;
        private readonly List<Vector2> tipHistory = new();
        private readonly List<Vector2> bladeCenterHistory = new();
        private readonly List<float> bladeRotationHistory = new();

        // 单段挥砍"先快后慢"缓动（ease-out）：起手瞬间最快，随后减速收尾；每段独立，逐段重复
        private float SwingEase => 1f - MathF.Pow(1f - SwingCompletion, 2.6f);
        // 当前瞬时快慢（1=最快 起手，0=最慢 收尾），用来调制抛出的特效强度
        private float SwingSpeedFactor => MathF.Pow(1f - SwingCompletion, 1.55f);

        // 蓄→放的纯绘制尺寸律动（只作用于本体贴图，不改判定框/伤害/前探距离）：
        // 起手把武器收拢蓄势，挥出瞬间三次方弹回满尺寸，收势再轻微回落——移植镀金长喙的"压缩→释放"手感。
        // 全程 ≤1（绘制永不大于判定），所以没有"贴图比伤害框大"的违和。
        private float SwingDrawScale
        {
            get
            {
                if (inStartup)
                    return MathHelper.Lerp(1f, 0.82f, MathF.Pow(StartupCompletion, 1.4f));
                if (inCooldown)
                    return MathHelper.Lerp(1f, 0.94f, MathF.Pow(CooldownCompletion, 1.6f));
                float release = MathHelper.Clamp(SwingCompletion / 0.22f, 0f, 1f);
                return MathHelper.Lerp(0.82f, 1f, 1f - MathF.Pow(1f - release, 3f));
            }
        }

        private float StageDamageMult => stage switch
        {
            0 => UmbralNadirBalance.UpSlashDamageMult,
            1 => UmbralNadirBalance.DownSlashDamageMult,
            _ => UmbralNadirBalance.DashDamageMult,
        };

        public override void Defaults()
        {
            Projectile.width = Projectile.height = 120;
            Projectile.extraUpdates = 3; // 更顺滑的挥舞
            Projectile.noEnchantmentVisuals = true;
        }

        public override void Spawn()
        {
            Player player = Main.player[Projectile.owner];
            stage = player.GetModPlayer<UmbralNadirPlayer>().ConsumeComboStage();

            // 阶段尺寸成长（与近战体型加成叠乘）
            Projectile.scale *= UmbralNadirBalance.GetLeftScale();

            switch (stage)
            {
                case 0: // 上挑斩
                    StartupTime = 4; CooldownTime = 5; OffsetDistance = 48;
                    RotateInStartup = 0.35f; RotateInCooldown = 0.22f;
                    UseSound = SoundID.Item71;
                    break;
                case 1: // 劈落斩
                    StartupTime = 4; CooldownTime = 5; OffsetDistance = 48;
                    RotateInStartup = 0.35f; RotateInCooldown = 0.22f;
                    UseSound = SoundID.Item71;
                    break;
                default: // 冲刺贯穿（动作保留，但不推玩家）
                    StartupTime = 3; CooldownTime = 8; OffsetDistance = 30;
                    RotateInStartup = 0.5f; RotateInCooldown = 0.15f;
                    UseSound = SoundID.DD2_BetsysWrathImpact;
                    Projectile.CritChance = 100; // 必暴
                    break;
            }

            swingTime -= StartupTime + CooldownTime;
            if (swingTime < 6)
                swingTime = 6;
        }

        public override float SwingFunction()
        {
            // 用 ease-out 的 SwingEase 取代对称的 SmoothStep：每段都"先快后慢"，起手一挥而出、收尾自然减速
            return stage switch
            {
                0 => MathHelper.ToRadians(MathHelper.Lerp(-80f, 118f, SwingEase)),  // 上挑
                1 => MathHelper.ToRadians(MathHelper.Lerp(118f, -80f, SwingEase)),  // 劈落
                _ => MathHelper.ToRadians(MathHelper.Lerp(26f, -26f, SwingEase)),   // 冲刺（小幅贯穿）
            };
        }

        public override void AdditionalAI()
        {
            Player player = Main.player[Projectile.owner];

            // 双键：按住左键期间再按右键 → 切换回旋斩迹
            if (player.whoAmI == Main.myPlayer)
            {
                player.Calamity().rightClickListener = true;
                if (!spawnedSpin && Main.mouseLeft && Main.mouseRight && !Main.mapFullscreen && !Main.blockMouse)
                {
                    spawnedSpin = true;
                    int spinDamage = Math.Max(1, (int)(Projectile.damage * UmbralNadirBalance.SpinDamageMult));
                    // ai[0] = 左键基础伤害（供回旋满充能释放黑洞新星），ai[1] = 阶段尺寸
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), player.MountedCenter, Vector2.Zero,
                        ModContent.ProjectileType<UmbralNadirSpinHoldout>(), spinDamage, Projectile.knockBack,
                        Projectile.owner, Projectile.damage, UmbralNadirBalance.GetLeftScale());
                    Projectile.Kill();
                    return;
                }
            }

            player.GetModPlayer<UmbralNadirPlayer>().KeepComboAlive();

            // 冲刺段：只保留前探动作（OffsetDistance，同样先快后慢），不施加任何玩家位移
            if (stage == 2 && inSwing)
                OffsetDistance = (int)MathHelper.Lerp(-18f, 112f, SwingEase);

            // 随挥舞序列抛出弹幕（各有独立发射技巧）：
            // 上挑/劈落 = 领刃月波 + 沿挥舞一颗颗扇形扫射的散射虚空弹；冲刺 = 突入瞬间的音速激光矛
            if (inSwing && Projectile.owner == Main.myPlayer)
                FireSwingProjectiles(player);

            // 每子步（含 extraUpdates）连续抛出被真实挥速拖出的挥砍拖尾——镀金长喙"连续质感"的关键：
            // 不再逐真实帧盖一次贴图，而是让每个子步都续上一小段，连成一条被惯性拖出的连续裂缝。
            if (inSwing)
                EmitSwingTrail(player);

            // 记录矛尖世界坐标（每真实帧一次，让残像沿挥舞弧线铺开而不是挤成一团）
            if (Projectile.FinalExtraUpdate())
            {
                if (inSwing)
                {
                    Vector2 radial = (Projectile.Center - player.MountedCenter).SafeNormalize(Vector2.UnitX);
                    Vector2 tip = Projectile.Center + radial * 62f * Projectile.scale;
                    tipHistory.Insert(0, tip);
                    if (tipHistory.Count > 12)
                        tipHistory.RemoveAt(tipHistory.Count - 1);
                    bladeCenterHistory.Insert(0, Projectile.Center);
                    bladeRotationHistory.Insert(0, radial.ToRotation() + MathHelper.PiOver2 + MathHelper.PiOver4);
                    if (bladeCenterHistory.Count > 10)
                    {
                        bladeCenterHistory.RemoveAt(bladeCenterHistory.Count - 1);
                        bladeRotationHistory.RemoveAt(bladeRotationHistory.Count - 1);
                    }
                    UpdateVoidSmear(player);
                }
                else
                {
                    if (tipHistory.Count > 0)
                        tipHistory.RemoveAt(tipHistory.Count - 1);
                    if (bladeCenterHistory.Count > 0)
                    {
                        bladeCenterHistory.RemoveAt(bladeCenterHistory.Count - 1);
                        bladeRotationHistory.RemoveAt(bladeRotationHistory.Count - 1);
                    }
                }
            }
        }

        /// <summary>随挥舞进程抛出弹幕：上挑/劈落 = 领刃 + 一颗颗扇形扫射的散射虚空弹；冲刺 = 音速激光矛。</summary>
        private void FireSwingProjectiles(Player player)
        {
            if (stage < 2)
            {
                // 前两段也射出与冲刺段相同的贯穿光矛；SlashWave 文件保留，但不再由连招调用。
                if (!waveSpawned && SwingCompletion >= 0.30f)
                {
                    waveSpawned = true;
                    Vector2 dir = (-angle).SafeNormalize(Vector2.UnitX * player.direction);
                    int lanceDamage = Math.Max(1, (int)(Projectile.damage * StageDamageMult * UmbralNadirBalance.VoidLanceDamageMult));
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, dir * UmbralNadirBalance.VoidLanceFireSpeed,
                        ModContent.ProjectileType<UmbralNadirVoidLance>(), lanceDamage, Projectile.knockBack, Projectile.owner);
                    SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.4f, Pitch = 0.25f }, Projectile.Center);
                }

                // 散射虚空弹：随挥舞一颗颗打出，沿当前刀身方向铺成扇形（劈落比上挑多一颗）
                int total = UmbralNadirBalance.VoidBoltsPerSwing + stage;
                while (boltsSpawned < total && SwingCompletion >= 0.22f + 0.68f * boltsSpawned / (float)(total - 1))
                {
                    Vector2 bladeDir = (Projectile.Center - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
                    Vector2 vel = bladeDir.RotatedByRandom(0.16f) * Main.rand.NextFloat(8f, 10.5f);
                    int boltDamage = Math.Max(1, (int)(Projectile.damage * StageDamageMult * UmbralNadirBalance.VoidBoltDamageMult));
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel,
                        ModContent.ProjectileType<UmbralNadirVoidBolt>(), boltDamage, Projectile.knockBack * 0.4f, Projectile.owner);
                    SoundEngine.PlaySound(SoundID.Item104 with { Volume = 0.22f, Pitch = 0.4f + boltsSpawned * 0.05f }, Projectile.Center);
                    boltsSpawned++;
                }
            }
            else if (!waveSpawned && SwingCompletion >= 0.4f)
            {
                // 冲刺贯穿：突入瞬间射出音速激光矛
                waveSpawned = true;
                Vector2 dir = (-angle).SafeNormalize(Vector2.UnitX * player.direction);
                int lanceDamage = Math.Max(1, (int)(Projectile.damage * StageDamageMult * UmbralNadirBalance.VoidLanceDamageMult));
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, dir * UmbralNadirBalance.VoidLanceFireSpeed,
                    ModContent.ProjectileType<UmbralNadirVoidLance>(), lanceDamage, Projectile.knockBack, Projectile.owner);
            }
        }

        /// <summary>沿矛尖持续更新一道黑色刀光弧（CircularSmear），随挥舞旋转，快相更浓。</summary>
        private void UpdateVoidSmear(Player player)
        {
            Vector2 pivot = player.MountedCenter;
            Vector2 bladeDir = (Projectile.Center - pivot).SafeNormalize(Vector2.UnitX * player.direction);
            float rot = bladeDir.ToRotation() + MathHelper.PiOver2;
            float reach = (stage == 2 ? 100f : 122f) * Projectile.scale;
            float scale = reach / 78f; // CircularSmearSmokey 156px，半径 78
            Color col = Color.Lerp(Color.Black, UmbralNadirPalette.MeldGreenDeep, 0.16f) * MathHelper.Clamp(0.45f + SwingSpeedFactor, 0.4f, 1f);
            if (!smearSpawned)
            {
                smearSpawned = true;
                voidSmear = new CircularSmearSmokeyVFX(pivot, col, rot, scale);
                GeneralParticleHandler.SpawnParticle(voidSmear);
            }
            else if (voidSmear != null)
            {
                voidSmear.Position = pivot;
                voidSmear.Rotation = rot;
                voidSmear.Scale = scale;
                voidSmear.Color = col;
                voidSmear.Time = 0;
            }
        }

        /// <summary>
        /// 每子步连续抛出的挥砍拖尾。核心与镀金长喙同理：粒子速度不再各自围绕刀尖乱飞，
        /// 而是继承刀身"本子步相对玩家的真实位移"(oldPlayerOffset 差分)被拖出；配合 extraUpdates
        /// 逐子步续接——于是读作"一条被高速惯性拖出来的连续裂缝"，而非若干真实帧上盖出的贴图。
        /// 黑色撕裂丝带打底 + 被拖出的绿色事件视界拖丝 + 顶层亮绿边 + 稀疏吸向刀尖的碎渊尘，随"先快后慢"调制。
        /// </summary>
        private void EmitSwingTrail(Player player)
        {
            float speed = SwingSpeedFactor;                          // 1=起手最快，0=收尾最慢
            Vector2 offset = Projectile.Center - player.MountedCenter;
            Vector2 inertia = oldPlayerOffset - offset;             // 本子步刀身真实位移（指向拖尾方向，与镀金长喙同款）
            float swirl = inertia.Length();
            Vector2 drag = inertia.SafeNormalize(Vector2.Zero);
            Vector2 bladeDir = offset.SafeNormalize(Vector2.UnitX * player.direction);
            Vector2 tangent = bladeDir.RotatedBy(MathHelper.PiOver2);
            Vector2 tip = Projectile.Center + bladeDir * 58f * Projectile.scale;

            // ① 黑色撕裂丝带：沿刀身铺开、被真实挥速拖出——逐子步一条，连成一条连续裂缝
            Vector2 ripPos = Projectile.Center + bladeDir * Main.rand.NextFloat(20f, 92f) * Projectile.scale + tangent * Main.rand.NextFloat(-6f, 6f);
            Vector2 ripVel = drag * (swirl * 0.5f + 1.5f) + tangent.RotatedByRandom(0.25f) * Main.rand.NextFloat(0.4f, 1.8f) * speed;
            GeneralParticleHandler.SpawnParticle(new LineParticle(ripPos, ripVel, false, Main.rand.Next(9, 15),
                Main.rand.NextFloat(0.5f, 0.95f) * (0.55f + speed), Color.Black));

            // ② 被拖出的绿色事件视界拖丝（空气被刀尖撕开的余量，替代金色长喙的火焰尾焰）——快相更密
            if (Main.rand.NextFloat() < 0.35f + speed * 0.55f)
            {
                Vector2 streakPos = tip - drag * Main.rand.NextFloat(0f, 24f) * Projectile.scale + tangent * Main.rand.NextFloat(-9f, 9f);
                Vector2 streakVel = drag * (swirl * 0.6f + 1f);
                Color streakCol = Color.Lerp(UmbralNadirPalette.MeldGreenDeep, UmbralNadirPalette.MeldGreen, speed);
                streakCol.A = 0;
                GeneralParticleHandler.SpawnParticle(new CustomSpark(streakPos, streakVel, "CalamityMod/Particles/GlowSpark",
                    false, Main.rand.Next(8, 13), Main.rand.NextFloat(0.018f, 0.03f), streakCol,
                    new Vector2(0.5f, 1.5f), true, false), false, GeneralDrawLayer.AfterEverything);
            }

            // ③ 刀尖事件视界亮绿边火花（仅快相，稀疏，最顶层加色——给黑色运动一条能读出来的亮边）
            if (speed > 0.4f && Main.rand.NextFloat() < speed * 0.5f)
            {
                Vector2 edgeVel = (drag * swirl * 0.4f + tangent.RotatedByRandom(0.35f) * Main.rand.NextFloat(1.5f, 3.5f)) * speed;
                GeneralParticleHandler.SpawnParticle(new CustomSpark(tip, edgeVel, "CalamityMod/Particles/GlowSpark",
                    false, Main.rand.Next(10, 15), Main.rand.NextFloat(0.022f, 0.04f),
                    UmbralNadirPalette.MeldGreenBright with { A = 0 }, new Vector2(0.6f, 1.3f), true, false),
                    false, GeneralDrawLayer.AfterEverything);
            }

            // ④ 被吸向刀尖的碎渊尘（迷你黑洞感，稀疏；吸入向量里混一份真实拖速，让它也随挥舞被带着走）
            if (Main.rand.NextBool(6))
            {
                Vector2 edge = tip + Main.rand.NextVector2Unit() * Main.rand.NextFloat(28f, 66f) * Projectile.scale;
                Vector2 inward = (tip - edge).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(1.5f, 4.5f) + drag * swirl * 0.3f;
                Dust vd = Dust.NewDustPerfect(edge, ModContent.DustType<VoidDustInverted>(), inward, 0, UmbralNadirPalette.MeldGreen, Main.rand.NextFloat(0.7f, 1.2f));
                vd.noGravity = true;
                vd.color = UmbralNadirPalette.MeldGreen;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            base.ModifyHitNPC(target, ref modifiers); // 保留 base 的 HitDirectionOverride
            modifiers.SourceDamage *= StageDamageMult;
            // 冲刺段必暴由 Spawn() 里 Projectile.CritChance = 100 实现
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Voidfrost>(), 120 + stage * 60);

            // 呼应核心：每次近战命中都往敌人身上叠"蚀痕"，并为奇点充能
            UmbralCorrosionGlobalNPC.AddStacks(target, UmbralNadirBalance.GetLeftStackPerHit(stage));
            if (Projectile.owner == Main.myPlayer)
                Main.player[Projectile.owner].GetModPlayer<UmbralNadirPlayer>().AddCharge(UmbralNadirBalance.ChargePerLeftHit);

            // 每次挥砍只生成一次范围效果
            if (hasExploded || Projectile.owner != Main.myPlayer)
                return;
            hasExploded = true;

            float effectiveDamage = Projectile.damage * StageDamageMult;
            if (stage == 2)
            {
                // 冲刺贯穿 → 撕开持续黑洞奇点（DoT 基准 + 坍缩基准）
                int tick = Math.Max(1, (int)(effectiveDamage * UmbralNadirBalance.SingularityTickDamageMult));
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
                    ModContent.ProjectileType<UmbralNadirSingularity>(), tick, Projectile.knockBack, Projectile.owner, effectiveDamage);
            }
            else
            {
                // 上挑 / 劈落 → 一次短促黑洞冲击（含拉扯）
                int impactDamage = Math.Max(1, (int)(effectiveDamage * UmbralNadirBalance.GetImpactDamageMult(stage)));
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
                    ModContent.ProjectileType<UmbralNadirImpactExplosion>(), impactDamage, Projectile.knockBack, Projectile.owner, stage);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player player = Main.player[Projectile.owner];
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 armCenter = player.MountedCenter;
            bool dash = stage == 2;

            float SpearRotation(Vector2 center) =>
                (center - armCenter).SafeNormalize(Vector2.UnitX).ToRotation() + MathHelper.PiOver2 + MathHelper.PiOver4;

            // 直接绘制的大型刀盘不依赖粒子存续：武器一转动就能稳定看见完整黑绿斩盘。
            if (inSwing)
                DrawBladeDisc(armCenter, dash);

            // 矛尖黑色 shader 残像（吞光的黑矛尖）——由共享 Visuals 渲染，宽而明显，第三段更宽带扭曲
            if (inSwing && tipHistory.Count >= 2)
                UmbralNadirVisuals.RenderTipTrail(tipHistory, dash ? 46f : 32f, Projectile.scale, dash);

            // 武器本体的逐帧旋转残像；不再只有矛尖轨迹，整把武器的转动都能被读出来。
            for (int i = bladeCenterHistory.Count - 1; i >= 1; i--)
            {
                float history = 1f - i / (float)bladeCenterHistory.Count;
                float fade = history * history * (dash ? 0.42f : 0.3f);
                Vector2 afterPos = bladeCenterHistory[i] - Main.screenPosition;
                float afterRot = bladeRotationHistory[i];
                Main.EntitySpriteDraw(tex, afterPos, null, Color.Black * fade, afterRot, origin,
                    Projectile.scale * 1.08f, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(tex, afterPos, null, UmbralNadirPalette.MeldGreenDeep with { A = 0 } * (fade * 0.28f),
                    afterRot, origin, Projectile.scale, SpriteEffects.None, 0);
            }

            // 本体：纯黑剪影垫底（负光）+ 主体；乘以蓄→放律动尺寸（仅绘制，不动判定框/伤害/前探）
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float rot = SpearRotation(Projectile.Center);
            float coil = SwingDrawScale;
            Main.EntitySpriteDraw(tex, drawPos, null, Color.Black * 0.5f, rot, origin, Projectile.scale * coil * 1.08f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(tex, drawPos, null, lightColor, rot, origin, Projectile.scale * coil, SpriteEffects.None, 0);

            player.heldProj = Projectile.whoAmI;
            return false;
        }

        private void DrawBladeDisc(Vector2 pivot, bool dash)
        {
            Texture2D fullSmear = ModContent.Request<Texture2D>("CalamityMod/Particles/CircularSmearSmokey").Value;
            Texture2D halfSmear = ModContent.Request<Texture2D>("CalamityMod/Particles/SemiCircularSmearSwipe").Value;
            Vector2 bladeDir = (Projectile.Center - pivot).SafeNormalize(Vector2.UnitX);
            Vector2 drawPos = pivot - Main.screenPosition;
            float reach = (dash ? 122f : 148f) * Projectile.scale;
            float fullScale = reach * 2f / Math.Max(fullSmear.Width, fullSmear.Height);
            float halfScale = reach * 2.12f / Math.Max(halfSmear.Width, halfSmear.Height);
            float rotation = bladeDir.ToRotation() + MathHelper.PiOver2;
            float strength = MathHelper.Clamp(0.45f + SwingSpeedFactor * 0.75f, 0f, 1f);
            float pulse = 0.9f + 0.1f * MathF.Sin(Main.GlobalTimeWrappedHourly * 18f + Projectile.identity);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Main.EntitySpriteDraw(fullSmear, drawPos, null, UmbralNadirPalette.MeldGreenDeep with { A = 0 } * (0.58f * strength), -rotation * 0.35f,
                fullSmear.Size() * 0.5f, new Vector2(fullScale * 1.08f, fullScale * 0.9f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(fullSmear, drawPos, null,
                UmbralNadirPalette.MeldGreenDeep with { A = 0 } * (0.3f * strength * pulse), rotation * 0.7f,
                fullSmear.Size() * 0.5f, fullScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(halfSmear, drawPos, null,
                UmbralNadirPalette.MeldGreen with { A = 0 } * (0.46f * strength), rotation,
                halfSmear.Size() * 0.5f, new Vector2(halfScale, halfScale * (dash ? 0.72f : 1f)), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(halfSmear, drawPos, null, UmbralNadirPalette.MeldGreenDeep with { A = 0 } * (0.42f * strength), rotation + MathHelper.Pi,
                halfSmear.Size() * 0.5f, new Vector2(halfScale * 0.94f, halfScale), SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
