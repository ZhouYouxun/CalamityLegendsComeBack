using System;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.AegisBlade
{
    public class AegisBladePlayer : ModPlayer
    {
        // ── 能量 ──────────────────────────────────────────────────────────
        public float AegisEnergy = 0f;
        private bool energyWasReady = false;

        // ── 盾牌状态（由 AegisShieldHoldout 每帧写入） ────────────────────
        public bool ShieldRaising    = false;  // Phase 0：举盾过渡 = 完美格挡窗口
        public bool ShieldRaised     = false;  // Phase 1+：盾已举起
        public bool ShieldCharging   = false;  // Phase 2：正在蓄力
        public bool ShieldFullyCharged = false; // Phase 3：蓄力完成，等待指令

        // 供 AegisShieldHoldout 写入，消费后清零
        public bool WasHurtDuringRaise = false;
        public bool PerfectParryJustTriggered = false;

        // ── 完美格挡后的最高防御计时器 ───────────────────────────────────
        public int PerfectParryDefenseTimer = 0;

        // ── 挥剑状态（由 AegisSwingHoldout 写入） ────────────────────────
        public bool IsSwinging = false;

        // ── 终结技状态 ────────────────────────────────────────────────────
        public bool UltimateActive = false;
        public int  UltimateTimer  = 0;

        // ── 坚毅（Tenacity）── ────────────────────────────────────────────
        private int tenacityImmunityTimer = 0;  // 3秒免死窗口
        private int tenacityCooldown      = 0;  // 60秒冷却

        // ── 内部 ──────────────────────────────────────────────────────────
        private int pendingIFrames = 0;
        private readonly BalanceAegisBlade balance = new();

        private static readonly Color GoldColor   = new(255, 200, 60);
        private static readonly Color GoldOutline = new(255, 235, 140);

        // ── 速度检测 ──────────────────────────────────────────────────────
        public bool IsStationary => Player.velocity.Length() < 0.5f;

        public override void ResetEffects()
        {
            ShieldRaising    = false;
            ShieldRaised     = false;
            ShieldCharging   = false;
            ShieldFullyCharged = false;
            IsSwinging       = false;
            // WasHurtDuringRaise 由 ModifyHurt 写入、由 holdout 消费，不在此处清零
        }

        public override void PostUpdate()
        {
            if (pendingIFrames > 0)
            {
                Player.GiveUniversalIFrames(pendingIFrames, false);
                pendingIFrames = 0;
            }

            PerfectParryJustTriggered = false;

            if (PerfectParryDefenseTimer > 0) PerfectParryDefenseTimer--;
            if (tenacityImmunityTimer  > 0) tenacityImmunityTimer--;
            if (tenacityCooldown       > 0) tenacityCooldown--;

            UpdateEnergy();
            UpdateUltimate();
        }

        private void UpdateEnergy()
        {
            if (Player.HeldItem.type != ModContent.ItemType<AegisBlade>()) return;

            float regenRate = BalanceAegisBlade.EnergyRegenPerSecond / 60f;
            if (IsStationary) regenRate *= BalanceAegisBlade.EnergyRegenMultiplierStationary;

            AegisEnergy = Math.Min(AegisEnergy + regenRate, BalanceAegisBlade.EnergyMax);

            bool ready = AegisEnergy >= BalanceAegisBlade.EnergyMax;
            LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref energyWasReady, ready);
        }

        private void UpdateUltimate()
        {
            if (!UltimateActive) return;

            UltimateTimer--;
            // 70%移动速度减少
            Player.moveSpeed *= (1f - BalanceAegisBlade.UltimateSpeedReduction);
            Player.maxRunSpeed *= (1f - BalanceAegisBlade.UltimateSpeedReduction);
            // 每帧持续无敌
            Player.GiveUniversalIFrames(2, false);

            if (UltimateTimer <= 0) UltimateActive = false;
        }

        public override void PostUpdateEquips()
        {
            if (Player.HeldItem.type != ModContent.ItemType<AegisBlade>()) return;

            // ── 埃癸斯被动：根据速度提供防御 ────────────────────────────
            int aegisDefense;
            if (PerfectParryDefenseTimer > 0)
            {
                // 完美格挡后8秒：最高防御
                aegisDefense = balance.GetAegisMaxDefense();
            }
            else
            {
                float speed  = Player.velocity.Length();
                int reduction = (int)(speed / BalanceAegisBlade.AegisSpeedPerDefenseLoss);
                aegisDefense = Math.Clamp(
                    balance.GetAegisMaxDefense() - reduction,
                    BalanceAegisBlade.AegisMinDefense,
                    balance.GetAegisMaxDefense());
            }
            Player.statDefense += aegisDefense;

            // 举盾防御加成
            if (ShieldRaised || ShieldCharging || ShieldFullyCharged)
                Player.statDefense += BalanceAegisBlade.ShieldMaxDefenseBonus;

            // ── 壁垒被动：防御损伤-50%（CalamityMod defenseDamageRatio） ──
            if (!HasDamageDebuff())
                Player.Calamity().defenseDamageRatio *= (1.0 - BalanceAegisBlade.BulwarkDefenseDamageReduction);

            // ── 埃癸斯被动：完美格挡后8秒无视五毒 ───────────────────────
            if (PerfectParryDefenseTimer > 0)
                ApplyFivePoisonsImmunity();
        }

        private void ApplyFivePoisonsImmunity()
        {
            // 原版毒类减益
            Player.buffImmune[BuffID.Poisoned]        = true;
            Player.buffImmune[BuffID.Venom]           = true;
            Player.buffImmune[BuffID.OnFire]          = true;
            Player.buffImmune[BuffID.OnFire3]         = true;  // Hellfire
            Player.buffImmune[BuffID.CursedInferno]   = true;
            Player.buffImmune[BuffID.Ichor]           = true;
            Player.buffImmune[BuffID.Frostburn]       = true;
            Player.buffImmune[BuffID.ShadowFlame]     = true;
            Player.buffImmune[BuffID.BrokenArmor]     = true;
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (Player.HeldItem.type != ModContent.ItemType<AegisBlade>()) return;

            // ── 坚毅免死3秒窗口：吸收所有伤害 ───────────────────────────
            if (tenacityImmunityTimer > 0)
            {
                modifiers.SourceDamage *= 0f;
                modifiers.Knockback    *= 0f;
                return;
            }

            // ── 完美格挡（举盾前15帧内受到伤害） ────────────────────────
            if (ShieldRaising && !WasHurtDuringRaise)
            {
                WasHurtDuringRaise        = true;
                PerfectParryJustTriggered = true;
                PerfectParryDefenseTimer  = BalanceAegisBlade.PerfectParryDefenseDuration;
                AegisEnergy = Math.Min(
                    AegisEnergy + BalanceAegisBlade.PerfectParryEnergyGain,
                    BalanceAegisBlade.EnergyMax);
                pendingIFrames = Math.Max(pendingIFrames, BalanceAegisBlade.ParryIFrames);

                // 格挡掉全部伤害
                modifiers.SourceDamage *= 0f;
                modifiers.Knockback    *= 0f;
                return;
            }

            // ── 壁垒被动：无伤害性减益时接触伤害 -20%（静止翻倍） ────────
            if (!HasDamageDebuff())
            {
                float contactReduction = BalanceAegisBlade.BulwarkContactReduction;
                if (IsStationary) contactReduction *= BalanceAegisBlade.BulwarkStationaryMultiplier;
                modifiers.SourceDamage *= (1f - contactReduction);
            }
        }

        public override void PostHurt(Player.HurtInfo info)
        {
            if (Player.HeldItem.type != ModContent.ItemType<AegisBlade>()) return;

            // 应用挂起的无敌帧
            if (pendingIFrames > 0)
            {
                Player.GiveUniversalIFrames(pendingIFrames, false);
                pendingIFrames = 0;
            }

            // 终结技能量：挨打得8能量（完美格挡时已在ModifyHurt中给过）
            if (!PerfectParryJustTriggered)
            {
                AegisEnergy = Math.Min(
                    AegisEnergy + BalanceAegisBlade.EnergyOnBeingHitOrParry,
                    BalanceAegisBlade.EnergyMax);
            }
        }

        public override bool PreKill(double damage, int hitDirection, bool pvp,
            ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            if (Player.HeldItem.type != ModContent.ItemType<AegisBlade>()) return true;
            if (tenacityCooldown > 0) return true;

            // 坚毅：保留1点生命，3秒内不会死亡
            Player.statLife        = 1;
            tenacityImmunityTimer  = BalanceAegisBlade.TenacityImmunityDuration;
            tenacityCooldown       = BalanceAegisBlade.TenacityCooldownDuration;
            Player.GiveUniversalIFrames(BalanceAegisBlade.TenacityImmunityDuration, false);

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item67 with { Volume = 1.1f, Pitch = -0.5f }, Player.Center);
                for (int i = 0; i < 28; i++)
                {
                    Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(6f, 18f);
                    Dust d = Dust.NewDustPerfect(Player.Center, DustID.GoldFlame, vel, 0, GoldOutline, 2.0f);
                    d.noGravity = true;
                }
                GeneralParticleHandler.SpawnParticle(
                    new DirectionalPulseRing(Player.Center, Vector2.Zero, GoldColor, Vector2.One, 0f, 0.05f, 2.0f, 24));
                CombatText.NewText(
                    new Rectangle((int)Player.Center.X, (int)Player.Center.Y - 52, 1, 1),
                    GoldOutline, "坚毅！", true);
            }

            playSound = false;
            genGore   = false;
            return false; // 阻止死亡
        }

        public override void PreUpdateMovement()
        {
            int wallType = ModContent.ProjectileType<Projectiles.AegisWallProjectile>();
            int halfW = Projectiles.AegisWallProjectile.WallHalfWidth;
            int halfH = Projectiles.AegisWallProjectile.WallHalfHeight;

            foreach (Projectile proj in Main.ActiveProjectiles)
            {
                if (!proj.active || proj.type != wallType) continue;
                // 只对已完全升起的土墙做碰撞
                if (proj.ai[1] < 1f) continue;

                float wallX     = proj.Center.X;
                float wallTop   = proj.Center.Y - halfH;
                float wallBot   = proj.Center.Y + halfH;
                float wallLeft  = wallX - halfW;
                float wallRight = wallX + halfW;

                if (Player.position.Y + Player.height <= wallTop) continue;
                if (Player.position.Y >= wallBot) continue;

                float playerLeft  = Player.position.X;
                float playerRight = Player.position.X + Player.width;
                float nextLeft    = playerLeft  + Player.velocity.X;
                float nextRight   = playerRight + Player.velocity.X;

                if (playerRight <= wallLeft && nextRight > wallLeft)
                    Player.velocity.X = wallLeft - playerRight;
                else if (playerLeft >= wallRight && nextLeft < wallRight)
                    Player.velocity.X = wallRight - playerLeft;
                else if (playerRight > wallLeft && playerLeft < wallRight)
                {
                    float distL = Math.Abs(playerLeft - wallLeft);
                    float distR = Math.Abs(playerRight - wallRight);
                    if (distL < distR) Player.position.X = wallLeft - Player.width;
                    else               Player.position.X = wallRight;
                    Player.velocity.X = 0f;
                }
            }
        }

        public bool CanActivateUltimate => AegisEnergy >= BalanceAegisBlade.EnergyMax && !UltimateActive;

        public void ActivateUltimate()
        {
            AegisEnergy   = 0f;
            UltimateActive = true;
            UltimateTimer  = BalanceAegisBlade.UltimateDuration;
            energyWasReady = false;
        }

        private bool HasDamageDebuff()
        {
            return Player.onFire || Player.onFire2 || Player.poisoned || Player.venom ||
                   Player.onFrostBurn || Player.suffocating || Player.electrified;
        }
    }
}
