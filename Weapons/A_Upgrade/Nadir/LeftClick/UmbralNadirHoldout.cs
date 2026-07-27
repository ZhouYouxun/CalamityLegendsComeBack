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
        private readonly List<Vector2> tipHistory = new();

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
                    StartupTime = 6; CooldownTime = 8; OffsetDistance = 48;
                    RotateInStartup = 0.35f; RotateInCooldown = 0.22f;
                    UseSound = SoundID.Item71;
                    break;
                case 1: // 劈落斩
                    StartupTime = 6; CooldownTime = 8; OffsetDistance = 48;
                    RotateInStartup = 0.35f; RotateInCooldown = 0.22f;
                    UseSound = SoundID.Item71;
                    break;
                default: // 冲刺贯穿（动作保留，但不推玩家）
                    StartupTime = 5; CooldownTime = 12; OffsetDistance = 30;
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
            return stage switch
            {
                0 => MathHelper.ToRadians(MathHelper.SmoothStep(-80f, 118f, SwingCompletion)),  // 上挑
                1 => MathHelper.ToRadians(MathHelper.SmoothStep(118f, -80f, SwingCompletion)),  // 劈落
                _ => MathHelper.ToRadians(MathHelper.SmoothStep(26f, -26f, SwingCompletion)),   // 冲刺（小幅贯穿）
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

            // 冲刺段：只保留前探动作（OffsetDistance），不施加任何玩家位移
            if (stage == 2 && inSwing)
                OffsetDistance = (int)MathHelper.Lerp(-18f, 112f, MathF.Pow(SwingCompletion, 1.5f));

            // 每段挥砍甩出一记刃波（中距离压制 + 远处叠蚀痕）
            if (inSwing && !waveSpawned && SwingCompletion >= 0.42f && Projectile.owner == Main.myPlayer)
            {
                waveSpawned = true;
                Vector2 dir = (-angle).SafeNormalize(Vector2.UnitX * player.direction);
                int waveDamage = Math.Max(1, (int)(Projectile.damage * StageDamageMult * 0.5f));
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, dir * 14f,
                    ModContent.ProjectileType<UmbralNadirSlashWave>(), waveDamage, Projectile.knockBack, Projectile.owner);
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.4f, Pitch = 0.25f }, Projectile.Center);
            }

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
                    SpawnBlackGrit(player);
                }
                else if (tipHistory.Count > 0)
                    tipHistory.RemoveAt(tipHistory.Count - 1);
            }
        }

        private void SpawnBlackGrit(Player player)
        {
            Vector2 bladeDir = (Projectile.Center - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
            int count = Main.rand.Next(1, 3);
            for (int i = 0; i < count; i++)
            {
                Vector2 pos = Projectile.Center + bladeDir * Main.rand.NextFloat(-32f, -10f) * Projectile.scale
                            + bladeDir.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-6f, 6f);
                Vector2 vel = -bladeDir.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.4f, 1.6f);
                // 小、短命、无发光的黑色粒屑
                if (Main.rand.NextBool())
                {
                    GeneralParticleHandler.SpawnParticle(new GenericBloom(pos, vel, Color.Black,
                        Main.rand.NextFloat(0.18f, 0.38f), Main.rand.Next(8, 14), true, false));
                }
                else
                {
                    Dust d = Dust.NewDustPerfect(pos, DustID.Shadowflame, vel, 120, Color.Black, Main.rand.NextFloat(0.7f, 1.1f));
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }
            // 深渊识别点（低频，每 3 真实帧最多 1 粒）
            if (Main.rand.NextBool(3))
            {
                Vector2 pos = Projectile.Center + bladeDir * Main.rand.NextFloat(20f, 56f) * Projectile.scale;
                Dust vd = Dust.NewDustPerfect(pos, ModContent.DustType<VoidDustInverted>());
                vd.noGravity = true;
                vd.velocity = -bladeDir * Main.rand.NextFloat(0.3f, 1.2f);
                vd.scale = Main.rand.NextFloat(0.7f, 1.1f);
                vd.color = Color.LightGreen;
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

            // 矛尖黑色 shader 残像（吞光的黑矛尖）——由共享 Visuals 渲染，宽而明显，第三段更宽带扭曲
            if (inSwing && tipHistory.Count >= 2)
                UmbralNadirVisuals.RenderTipTrail(tipHistory, dash ? 46f : 32f, Projectile.scale, dash);

            // 本体：纯黑剪影垫底（负光）+ 主体
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float rot = SpearRotation(Projectile.Center);
            Main.EntitySpriteDraw(tex, drawPos, null, Color.Black * 0.5f, rot, origin, Projectile.scale * 1.08f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(tex, drawPos, null, lightColor, rot, origin, Projectile.scale, SpriteEffects.None, 0);

            player.heldProj = Projectile.whoAmI;
            return false;
        }
    }
}
