using System;
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
    // 右键举盾holdout。四个阶段：
    // Phase 0（0-14帧）：举盾过渡 = 完美格挡窗口
    // Phase 1（15帧+）：盾已完全举起，防御加成；松开右键释放幻影
    // Phase 2（机械BOSS后解锁）：蓄力中，松开右键释放普通幻影
    // Phase 3（蓄力完成）：蓄力就绪；左键=下插土墙，右键松开=强化幻影
    public class AegisShieldHoldout : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/AegisBlade/庇护盾牌";

        private Player Owner => Main.player[Projectile.owner];
        private AegisBladePlayer BladePlayer => Owner.GetModPlayer<AegisBladePlayer>();

        // Phase 枚举
        private const int PhaseRaising      = 0;
        private const int PhaseRaised       = 1;
        private const int PhaseCharging     = 2;
        private const int PhaseFullyCharged = 3;

        private int   phase       = PhaseRaising;
        private int   phaseTimer  = 0;   // 通用相位计时器
        private int   raisedTimer = 0;   // Phase1累积时间（决定何时开始蓄力）
        private bool  perfectParryFired = false;   // 本次举盾是否已触发完美格挡
        private float displayScale      = 0f;
        private float chargeGlow        = 0f;      // 蓄力光晕强度 0→1

        // Phase 3 左键检测（上一帧状态，防止持续触发）
        private bool prevLeftDown = false;
        // Phase 3 是否已经派出了投射物（防止双杀）
        private bool discharged = false;

        private readonly BalanceAegisBlade balance = new();

        private static readonly Color GoldColor   = new(255, 200, 60);
        private static readonly Color GoldOutline = new(255, 235, 140);
        private static readonly Color ChargeColor = new(255, 240, 100);

        public override void SetDefaults()
        {
            Projectile.width  = Projectile.height = 70;
            Projectile.friendly   = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.penetrate  = -1;
            Projectile.timeLeft   = 4;
            Projectile.noEnchantmentVisuals = true;
        }

        private bool IsRightHeld()
        {
            if (Main.myPlayer != Projectile.owner) return true;
            return Owner.Calamity().mouseRight
                   && !Main.mapFullscreen && !Main.blockMouse && !Owner.mouseInterface;
        }

        // 检测左键"刚刚按下"（防持续）
        private bool IsLeftJustPressed(bool curLeft)
        {
            bool justPressed = curLeft && !prevLeftDown;
            prevLeftDown = curLeft;
            return justPressed;
        }

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != ModContent.ItemType<AegisBlade>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 4;
            phaseTimer++;

            bool curLeft = Main.mouseLeft && !Main.mapFullscreen && !Main.blockMouse && !Owner.mouseInterface;

            switch (phase)
            {
                case PhaseRaising:      DoPhaseRaising(); break;
                case PhaseRaised:       DoPhaseRaised(curLeft); break;
                case PhaseCharging:     DoPhaseCharging(curLeft); break;
                case PhaseFullyCharged: DoPhaseFullyCharged(curLeft); break;
            }

            // Phase 3以外每帧跟踪左键状态，确保进入Phase 3时"刚按下"检测正确
            if (phase != PhaseFullyCharged)
                prevLeftDown = curLeft;

            UpdateShieldPosition();
        }

        // ── Phase 0：举盾过渡，完美格挡窗口 ──────────────────────────────

        private void DoPhaseRaising()
        {
            float raiseProg = (float)phaseTimer / BalanceAegisBlade.ShieldRaiseFrames;
            displayScale    = MathHelper.Lerp(0.3f, 1f, CalamityUtils.SineInOutEasing(raiseProg, 1));

            BladePlayer.ShieldRaising = true;
            BladePlayer.ShieldRaised  = false;

            // 检测完美格挡
            if (!perfectParryFired && BladePlayer.WasHurtDuringRaise)
            {
                perfectParryFired             = true;
                BladePlayer.WasHurtDuringRaise = false;
                OnPerfectParry();
            }

            // 举盾期间松开右键：取消
            if (!IsRightHeld())
            {
                Projectile.Kill();
                return;
            }

            if (phaseTimer >= BalanceAegisBlade.ShieldRaiseFrames)
            {
                phase      = PhaseRaised;
                phaseTimer = 0;
                raisedTimer = 0;
            }
        }

        // ── Phase 1：盾已举起 ────────────────────────────────────────────

        private void DoPhaseRaised(bool curLeft)
        {
            displayScale = MathHelper.Lerp(displayScale, 1f, 0.2f);
            raisedTimer++;

            BladePlayer.ShieldRaised  = true;
            BladePlayer.ShieldRaising = false;

            // 粒子点缀
            if (!Main.dedServ && Main.rand.NextBool(5)) EmitIdleParticle();

            // 松开右键 → 普通幻影
            if (!IsRightHeld())
            {
                if (Main.myPlayer == Projectile.owner && !discharged)
                {
                    discharged = true;
                    ReleasePhantom(isPerfect: perfectParryFired, isCharged: false);
                }
                Projectile.Kill();
                return;
            }

            // 达到蓄力延迟且已解锁蓄力 → 进入Phase 2
            if (BalanceAegisBlade.ChargeUnlocked() && raisedTimer >= BalanceAegisBlade.ChargeHoldDelay)
            {
                phase      = PhaseCharging;
                phaseTimer = 0;
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.65f, Pitch = 0.1f }, Owner.Center);
            }
        }

        // ── Phase 2：蓄力中 ──────────────────────────────────────────────

        private void DoPhaseCharging(bool curLeft)
        {
            float chargeProg = (float)phaseTimer / BalanceAegisBlade.ChargeDuration;
            chargeGlow       = MathHelper.Clamp(chargeGlow + 0.04f, 0f, 1f);

            BladePlayer.ShieldRaised   = true;
            BladePlayer.ShieldCharging = true;

            // 蓄力粒子
            if (!Main.dedServ && Main.rand.NextBool(2)) EmitChargeParticle(chargeProg);

            // 松开右键（蓄力未完成）→ 普通幻影，取消蓄力
            if (!IsRightHeld())
            {
                if (Main.myPlayer == Projectile.owner && !discharged)
                {
                    discharged = true;
                    ReleasePhantom(isPerfect: perfectParryFired, isCharged: false);
                }
                Projectile.Kill();
                return;
            }

            // 蓄力完成 → Phase 3
            if (phaseTimer >= BalanceAegisBlade.ChargeDuration)
            {
                phase      = PhaseFullyCharged;
                phaseTimer = 0;
                chargeGlow = 1f;
                SoundEngine.PlaySound(SoundID.Item67 with { Volume = 0.9f, Pitch = 0.2f }, Owner.Center);
                OnChargeComplete();
            }
        }

        // ── Phase 3：蓄力就绪 ────────────────────────────────────────────

        private void DoPhaseFullyCharged(bool curLeft)
        {
            BladePlayer.ShieldRaised       = true;
            BladePlayer.ShieldFullyCharged = true;

            // 蓄力就绪光效
            if (!Main.dedServ && Main.rand.NextBool(3)) EmitChargedIdleParticle();

            // 左键刚按下 → 刀刃下插 + 土墙
            bool leftJust = IsLeftJustPressed(curLeft);
            if (leftJust && Main.myPlayer == Projectile.owner && !discharged)
            {
                discharged = true;
                TriggerBladePlunge();
                Projectile.Kill();
                return;
            }

            // 松开右键 → 强化幻影（蓄力总是强化）
            if (!IsRightHeld())
            {
                if (Main.myPlayer == Projectile.owner && !discharged)
                {
                    discharged = true;
                    ReleasePhantom(isPerfect: true, isCharged: true);
                }
                Projectile.Kill();
                return;
            }
        }

        // ── 完美格挡触发 ─────────────────────────────────────────────────

        private void OnPerfectParry()
        {
            if (Main.dedServ) return;

            SoundEngine.PlaySound(SoundID.DD2_WitherBeastCrystalImpact with { Volume = 1.1f, Pitch = 0.3f }, Owner.Center);

            for (int i = 0; i < 20; i++)
            {
                Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(5f, 14f);
                Dust d = Dust.NewDustPerfect(Owner.Center, DustID.GoldFlame, vel, 0, GoldOutline, 1.8f);
                d.noGravity = true;
            }
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Owner.Center, Vector2.Zero, GoldColor, Vector2.One, 0f, 0.05f, 1.5f, 20));

            // "惊人一刻！"
            CombatText.NewText(
                new Rectangle((int)Owner.Center.X, (int)Owner.Center.Y - 42, 1, 1),
                GoldOutline, "惊人一刻！", true);
        }

        // ── 蓄力完成爆闪 ─────────────────────────────────────────────────

        private void OnChargeComplete()
        {
            if (Main.dedServ) return;

            for (int i = 0; i < 24; i++)
            {
                float ang = MathHelper.TwoPi * i / 24f;
                Vector2 vel = ang.ToRotationVector2() * Main.rand.NextFloat(4f, 12f);
                Dust d = Dust.NewDustPerfect(Owner.Center, DustID.GoldFlame, vel, 0, ChargeColor, 1.6f);
                d.noGravity = true;
            }
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Owner.Center, Vector2.Zero, ChargeColor, Vector2.One, 0f, 0.06f, 1.8f, 22));
        }

        // ── 释放幻影 ──────────────────────────────────────────────────────

        private void ReleasePhantom(bool isPerfect, bool isCharged)
        {
            Vector2 mouse = Owner.Calamity().mouseWorld;
            if (mouse == Vector2.Zero) mouse = Main.MouseWorld;
            Vector2 dir = Owner.Center.DirectionTo(mouse).SafeNormalize(Vector2.UnitX * Owner.direction);

            // 强化版才锁定BOSS
            bool enhanced = isPerfect || isCharged;
            if (enhanced)
            {
                NPC boss = FindNearestBoss();
                if (boss != null)
                    dir = Owner.Center.DirectionTo(boss.Center).SafeNormalize(dir);
            }

            int phantomDamage = balance.GetShieldPhantomDamage();

            Projectile.NewProjectile(Projectile.GetSource_FromThis(),
                Owner.Center, dir * 18f,
                ModContent.ProjectileType<AegisShieldPhantom>(),
                phantomDamage, enhanced ? 8f : 6f, Projectile.owner,
                enhanced ? 1f : 0f);

            SoundEngine.PlaySound(SoundID.Item84 with { Volume = 0.9f, Pitch = enhanced ? 0.35f : 0.1f }, Owner.Center);
        }

        // ── 触发刀刃下插 ─────────────────────────────────────────────────

        private void TriggerBladePlunge()
        {
            int plungeDamage = balance.GetBladePlungeDamage();

            Projectile.NewProjectile(Projectile.GetSource_FromThis(),
                Owner.Center,
                Vector2.UnitY * 26f,     // 垂直向下
                ModContent.ProjectileType<AegisBladeThrown>(),
                plungeDamage, 6f, Projectile.owner);

            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 1f, Pitch = -0.4f }, Owner.Center);
        }

        private NPC FindNearestBoss()
        {
            float bestDist = 700f;
            NPC best = null;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.boss || !npc.CanBeChasedBy()) continue;
                float dist = Owner.Distance(npc.Center);
                if (dist < bestDist) { bestDist = dist; best = npc; }
            }
            return best;
        }

        // ── 粒子效果 ──────────────────────────────────────────────────────

        private void EmitIdleParticle()
        {
            Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(22f, 22f);
            Vector2 vel = -Vector2.UnitY * Main.rand.NextFloat(0.5f, 1.8f);
            GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(pos, vel, false,
                Main.rand.Next(10, 18), 0.03f, GoldColor,
                new Vector2(1f, 0.3f), true, false, 0.7f));
        }

        private void EmitChargeParticle(float chargeProg)
        {
            // 粒子从外向内汇聚到盾上
            float radius = MathHelper.Lerp(120f, 30f, chargeProg);
            Vector2 orbit = Main.rand.NextVector2CircularEdge(1f, 1f) * radius;
            Vector2 vel   = (Projectile.Center - (Projectile.Center + orbit)).SafeNormalize(Vector2.UnitY)
                             * Main.rand.NextFloat(5f, 12f);
            GeneralParticleHandler.SpawnParticle(new CustomSpark(
                Projectile.Center + orbit, vel,
                "CalamityMod/Particles/Sparkle", false,
                Main.rand.Next(8, 16),
                MathHelper.Lerp(0.6f, 1.4f, chargeProg),
                Main.rand.NextBool(2) ? GoldOutline : GoldColor,
                new Vector2(0.3f, 1.3f), true, true, shrinkSpeed: 0.14f));
        }

        private void EmitChargedIdleParticle()
        {
            // 蓄力完成后的脉冲粒子
            float angle = Main.GlobalTimeWrappedHourly * 4f + Main.rand.NextFloat(MathHelper.TwoPi);
            Vector2 pos = Projectile.Center + angle.ToRotationVector2() * Main.rand.NextFloat(15f, 35f);
            Vector2 vel = angle.ToRotationVector2() * Main.rand.NextFloat(1.5f, 4f);
            GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(pos, vel, false,
                Main.rand.Next(6, 14), 0.05f,
                Main.rand.NextBool(3) ? GoldOutline : ChargeColor,
                new Vector2(1.2f, 0.3f), true, false, 0.8f));
        }

        // ── 盾牌位置 ──────────────────────────────────────────────────────

        private void UpdateShieldPosition()
        {
            int backDir = -Owner.direction;
            Vector2 shieldOffset = new Vector2(backDir * 20f, 0f);
            Projectile.Center    = Owner.Center + shieldOffset;
            Projectile.rotation  = -MathHelper.PiOver4 * Owner.direction;
            Owner.heldProj       = Projectile.whoAmI;
        }

        // ── 绘制 ──────────────────────────────────────────────────────────

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            Texture2D shieldTex = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloom     = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPos     = Projectile.Center - Main.screenPosition;
            float rotation      = Projectile.rotation;
            float drawScale     = displayScale;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            // ── 完美格挡金色描边 ─────────────────────────────────────────
            if (perfectParryFired)
            {
                for (int i = 0; i < 8; i++)
                {
                    float angle  = MathHelper.TwoPi * i / 8f + Main.GlobalTimeWrappedHourly * 2.5f;
                    Vector2 off  = angle.ToRotationVector2() * 5f;
                    Main.EntitySpriteDraw(shieldTex, drawPos + off, null,
                        GoldOutline with { A = 0 } * 0.55f,
                        rotation, shieldTex.Size() * 0.5f, drawScale, SpriteEffects.None, 0);
                }
            }

            // ── 蓄力光晕 ─────────────────────────────────────────────────
            if (phase >= PhaseCharging)
            {
                float pulse = chargeGlow * (0.4f + 0.15f * MathF.Sin(Main.GlobalTimeWrappedHourly * 8f));
                Main.EntitySpriteDraw(bloom, drawPos, null,
                    ChargeColor with { A = 0 } * pulse,
                    0f, bloom.Size() * 0.5f, drawScale * (1f + chargeGlow * 0.5f), SpriteEffects.None, 0);
            }

            // ── 基础光晕 ─────────────────────────────────────────────────
            float baseGlow = 0.25f + 0.08f * MathF.Sin(Main.GlobalTimeWrappedHourly * 3.5f);
            Main.EntitySpriteDraw(bloom, drawPos, null,
                GoldColor with { A = 0 } * baseGlow * drawScale,
                0f, bloom.Size() * 0.5f, drawScale, SpriteEffects.None, 0);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            // ── 盾本体 ───────────────────────────────────────────────────
            Main.EntitySpriteDraw(shieldTex, drawPos, null, lightColor,
                rotation, shieldTex.Size() * 0.5f, drawScale, SpriteEffects.None, 0);

            // ── 蓄力进度条（相位2/3显示） ────────────────────────────────
            if (Main.myPlayer == Projectile.owner && phase >= PhaseCharging)
                DrawChargeBar();

            return false;
        }

        private void DrawChargeBar()
        {
            Texture2D barBG = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarBack").Value;
            Texture2D barFG = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarFront").Value;

            float progress = phase == PhaseCharging
                ? Math.Clamp((float)phaseTimer / BalanceAegisBlade.ChargeDuration, 0f, 1f)
                : 1f;

            Vector2 drawPos  = Owner.Center - Main.screenPosition + new Vector2(-barBG.Width * 0.75f, -60f);
            Rectangle frame  = new Rectangle(0, 0, (int)(progress * barFG.Width), barFG.Height);
            Color barColor   = phase == PhaseFullyCharged ? GoldOutline : ChargeColor;

            Main.spriteBatch.Draw(barBG, drawPos, barColor * 0.9f);
            Main.spriteBatch.Draw(barFG, drawPos, frame, barColor * 0.75f);
        }
    }
}
