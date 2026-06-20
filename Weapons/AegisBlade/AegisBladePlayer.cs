using System;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.AegisBlade
{
    public class AegisBladePlayer : ModPlayer
    {
        // ── 能量 ──────────────────────────────────────────────────────────
        public float AegisEnergy = 0f;
        private bool energyWasReady = false;

        // ── 盾牌状态（由 AegisShieldHoldout 写入） ───────────────────────
        public bool ShieldRaising = false;      // 举盾前15帧 = 完美格挡窗口
        public bool ShieldRaised = false;       // 盾已完全举起
        public bool WasHurtDuringRaise = false; // 由 ModifyHurt 设置，供 holdout 读取
        public bool PerfectParryJustTriggered = false;

        // ── 完美格挡后的最高防御计时器 ───────────────────────────────────
        public int PerfectParryDefenseTimer = 0;

        // ── 挥剑状态（由 AegisSwingHoldout 写入） ────────────────────────
        public bool IsSwinging = false;

        // ── 终结技状态 ────────────────────────────────────────────────────
        public bool UltimateActive = false;
        public int UltimateTimer = 0;

        // ── 内部 ──────────────────────────────────────────────────────────
        private int pendingIFrames = 0;
        private readonly BalanceAegisBlade balance = new();

        // ── 速度检测 ──────────────────────────────────────────────────────
        public bool IsStationary => Player.velocity.Length() < 0.5f;

        public override void ResetEffects()
        {
            ShieldRaising = false;
            ShieldRaised = false;
            IsSwinging = false;
        }

        public override void PostUpdate()
        {
            if (pendingIFrames > 0)
            {
                Player.GiveUniversalIFrames(pendingIFrames, false);
                pendingIFrames = 0;
            }

            PerfectParryJustTriggered = false;

            if (PerfectParryDefenseTimer > 0)
                PerfectParryDefenseTimer--;

            UpdateEnergy();
            UpdateUltimate();
        }

        private void UpdateEnergy()
        {
            if (Player.HeldItem.type != ModContent.ItemType<AegisBlade>())
                return;

            float regenRate = BalanceAegisBlade.EnergyRegenPerSecond / 60f;
            if (IsStationary)
                regenRate *= BalanceAegisBlade.EnergyRegenMultiplierStationary;

            AegisEnergy = Math.Min(AegisEnergy + regenRate, BalanceAegisBlade.EnergyMax);

            bool ready = AegisEnergy >= BalanceAegisBlade.EnergyMax;
            LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref energyWasReady, ready);
        }

        private void UpdateUltimate()
        {
            if (!UltimateActive)
                return;

            UltimateTimer--;
            Player.moveSpeed *= (1f - BalanceAegisBlade.UltimateSpeedReduction);

            // 持续无敌帧（每帧刷新）
            Player.GiveUniversalIFrames(2, false);

            if (UltimateTimer <= 0)
                UltimateActive = false;
        }

        public override void PostUpdateEquips()
        {
            if (Player.HeldItem.type != ModContent.ItemType<AegisBlade>())
                return;

            // 埃癸斯被动：根据速度给防御
            int aegisDefense;
            if (PerfectParryDefenseTimer > 0)
            {
                aegisDefense = balance.GetAegisMaxDefense();
            }
            else
            {
                float speed = Player.velocity.Length();
                int reduction = (int)(speed / BalanceAegisBlade.AegisSpeedPerDefenseLoss);
                aegisDefense = Math.Clamp(balance.GetAegisMaxDefense() - reduction, BalanceAegisBlade.AegisMinDefense, balance.GetAegisMaxDefense());
            }
            Player.statDefense += aegisDefense;

            // 举盾防御加成：逐渐增加到 ShieldMaxDefenseBonus
            if (ShieldRaised)
                Player.statDefense += BalanceAegisBlade.ShieldMaxDefenseBonus;
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (Player.HeldItem.type != ModContent.ItemType<AegisBlade>())
                return;

            // ── 完美格挡（举盾前15帧内受到伤害） ────────────────────────
            if (ShieldRaising)
            {
                // 触发完美格挡
                WasHurtDuringRaise = true;
                PerfectParryJustTriggered = true;
                PerfectParryDefenseTimer = BalanceAegisBlade.PerfectParryDefenseDuration;
                AegisEnergy = Math.Min(AegisEnergy + BalanceAegisBlade.PerfectParryEnergyGain, BalanceAegisBlade.EnergyMax);
                pendingIFrames = Math.Max(pendingIFrames, BalanceAegisBlade.ParryIFrames);

                // 格挡掉全部伤害
                modifiers.SourceDamage *= 0f;
                modifiers.Knockback *= 0f;
                return;
            }

            // ── 后方防护被动（未举盾时，后方伤害 -30%） ──────────────────
            if (!ShieldRaised && !ShieldRaising)
            {
                bool isFromBehind = Player.direction == modifiers.HitDirection;
                if (isFromBehind)
                    modifiers.SourceDamage *= (1f - BalanceAegisBlade.RearDamageReduction);
            }

            // ── 壁垒被动（无伤害性减益时，接触伤害 -20%，静止翻倍） ──────
            if (!HasDamageDebuff())
            {
                float contactReduction = BalanceAegisBlade.BulwarkContactReduction;
                if (IsStationary)
                    contactReduction *= BalanceAegisBlade.BulwarkStationaryMultiplier;
                modifiers.SourceDamage *= (1f - contactReduction);
            }
        }

        public override void PostHurt(Player.HurtInfo info)
        {
            if (pendingIFrames > 0)
            {
                Player.GiveUniversalIFrames(pendingIFrames, false);
                pendingIFrames = 0;
            }
        }

        public override void PreUpdateMovement()
        {
            int wallType = ModContent.ProjectileType<Projectiles.AegisWallProjectile>();
            int halfW = Projectiles.AegisWallProjectile.WallHalfWidth;
            int halfH = Projectiles.AegisWallProjectile.WallHalfHeight;

            foreach (Projectile proj in Main.ActiveProjectiles)
            {
                if (!proj.active || proj.type != wallType) continue;

                float wallX    = proj.Center.X;
                float wallTop  = proj.Center.Y - halfH;
                float wallBot  = proj.Center.Y + halfH;
                float wallLeft = wallX - halfW;
                float wallRight= wallX + halfW;

                // 玩家是否在墙体垂直范围内
                if (Player.position.Y + Player.height <= wallTop) continue;
                if (Player.position.Y >= wallBot) continue;

                float playerLeft  = Player.position.X;
                float playerRight = Player.position.X + Player.width;
                float nextLeft    = playerLeft  + Player.velocity.X;
                float nextRight   = playerRight + Player.velocity.X;

                // 玩家在墙左侧，向右移动将穿墙 → 截断
                if (playerRight <= wallLeft && nextRight > wallLeft)
                    Player.velocity.X = wallLeft - playerRight;

                // 玩家在墙右侧，向左移动将穿墙 → 截断
                else if (playerLeft >= wallRight && nextLeft < wallRight)
                    Player.velocity.X = wallRight - playerLeft;

                // 玩家已陷入墙中 → 推出最近一侧
                else if (playerRight > wallLeft && playerLeft < wallRight)
                {
                    float distL = System.Math.Abs(playerLeft - wallLeft);
                    float distR = System.Math.Abs(playerRight - wallRight);
                    if (distL < distR)
                        Player.position.X = wallLeft - Player.width;
                    else
                        Player.position.X = wallRight;
                    Player.velocity.X = 0f;
                }
            }
        }

        public void GainEnergyOnHit()
        {
            AegisEnergy = Math.Min(AegisEnergy + BalanceAegisBlade.PerfectParryEnergyGain, BalanceAegisBlade.EnergyMax);
        }

        public bool CanActivateUltimate => AegisEnergy >= BalanceAegisBlade.EnergyMax && !UltimateActive;

        public void ActivateUltimate()
        {
            AegisEnergy = 0f;
            UltimateActive = true;
            UltimateTimer = BalanceAegisBlade.UltimateDuration;
        }

        // 检测是否带有伤害性减益（简化实现：检测常见DoT）
        private bool HasDamageDebuff()
        {
            return Player.onFire || Player.onFire2 || Player.poisoned || Player.venom ||
                   Player.burned || Player.suffocating || Player.electrified ||
                   Player.onFrostBurn || Player.shadowDodge;
        }
    }
}
