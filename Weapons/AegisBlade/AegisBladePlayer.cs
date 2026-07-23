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
        private const string RaisableShieldTexture = "CalamityLegendsComeBack/Weapons/AegisBlade/\u5E87\u62A4\u76FE\u724C\u591A\u5E27\u56FE";
        private const string RaisableShieldName = "AegisBladeRaisableShield";

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
        public bool BarrierThrowComboActive = false;
        public int BarrierThrowCooldown = 0;

        // ── 左右键双手蓄力速凝掩体 ─────────────────────────────────────────
        public float BarrierChargeTimer = 0f;
        public int BarrierCooldown = 0;
        public bool IsChargingBarrier => BarrierChargeTimer > 0f;

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

        public override void Load()
        {
            if (!Main.dedServ)
            {
                EquipLoader.AddEquipTexture(Mod, RaisableShieldTexture, EquipType.Shield,
                    name: RaisableShieldName);
            }
        }

        public override void UpdateVisibleVanityAccessories()
        {
            if (Player.HeldItem.type != ModContent.ItemType<AegisBlade>())
                return;

            int shieldSlot = EquipLoader.GetEquipSlot(Mod, RaisableShieldName, EquipType.Shield);
            if (shieldSlot < 0)
                return;

            Player.shield = shieldSlot;
            Player.cShield = 0;
        }

        public override void UpdateEquips()
        {
            if (Player.HeldItem.type == ModContent.ItemType<AegisBlade>())
                Player.hasRaisableShield = true;
        }

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
            if (BarrierThrowCooldown   > 0) BarrierThrowCooldown--;
            if (BarrierCooldown        > 0) BarrierCooldown--;

            UpdateEnergy();
            UpdateUltimate();
            UpdateBarrierCharge();
        }

        private void UpdateBarrierCharge()
        {
            if (Player.HeldItem.type != ModContent.ItemType<AegisBlade>())
            {
                BarrierChargeTimer = 0f;
                return;
            }

            bool holdingLeft = Player.Calamity().mouseLeft || (Main.myPlayer == Player.whoAmI && Main.mouseLeft);
            bool holdingRight = Player.Calamity().mouseRight || (Main.myPlayer == Player.whoAmI && Main.mouseRight);
            bool holdingBoth = holdingLeft && holdingRight &&
                               !Main.mapFullscreen && !Main.blockMouse && !Player.mouseInterface;

            if (holdingBoth && BarrierCooldown <= 0)
            {
                BarrierChargeTimer = Math.Min(BarrierChargeTimer + 1f, 60f);

                // 蓄力期间抑制挥剑与举盾状态
                ShieldRaising = false;
                ShieldRaised = false;
                ShieldCharging = false;
                ShieldFullyCharged = false;
                IsSwinging = false;

                // 生成头顶 UI 状态条
                int barType = ModContent.ProjectileType<Projectiles.AegisWallStatusBar>();
                if (Main.myPlayer == Player.whoAmI && Player.ownedProjectileCounts[barType] == 0)
                {
                    Projectile.NewProjectile(Player.GetSource_ItemUse(Player.HeldItem), Player.Center, Vector2.Zero, barType, 0, 0f, Player.whoAmI);
                }

                // 蓄力粒子：在鼠标和玩家周围散发圣火粒子
                if (!Main.dedServ && (int)BarrierChargeTimer % 3 == 0)
                {
                    Vector2 mouseWorld = AegisBlade.GetMouseWorld(Player);
                    Dust d = Dust.NewDustPerfect(mouseWorld + Main.rand.NextVector2Circular(20f, 20f), Visuals.AegisVisuals.ProfanedFireDust, -Vector2.UnitY * Main.rand.NextFloat(0.8f, 2.5f), 0, Color.White, 1.15f);
                    d.noGravity = true;
                }

                if (BarrierChargeTimer >= 60f)
                {
                    // 蓄力满 1 秒：在鼠标位置释放速凝掩体！
                    Vector2 mousePos = AegisBlade.GetMouseWorld(Player);
                    if (Main.myPlayer == Player.whoAmI)
                    {
                        int wallType = ModContent.ProjectileType<Projectiles.AegisWallProjectile>();
                        Projectile.NewProjectile(Player.GetSource_ItemUse(Player.HeldItem), mousePos, Vector2.Zero, wallType, 0, 0f, Player.whoAmI);
                    }

                    SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.9f, Pitch = -0.2f }, mousePos);
                    SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.85f, Pitch = 0.15f }, mousePos);

                    if (!Main.dedServ)
                    {
                        Visuals.AegisVisuals.HolyDetonation(mousePos, 1.75f);
                        Visuals.AegisVisuals.CoronaRing(mousePos, 14, 1.25f);
                        Visuals.AegisVisuals.Screenshake(mousePos, 2.6f, 750f);
                    }

                    BarrierChargeTimer = 0f;
                    BarrierCooldown = 35; // 冷却窗口，防止直接连续重复触发
                }
            }
            else
            {
                if (BarrierChargeTimer < 60f)
                    BarrierChargeTimer = 0f;
            }
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
            {
                Player.statDefense += BalanceAegisBlade.ShieldMaxDefenseBonus;
                Player.noKnockback = true;
            }

            // ── 壁垒被动：防御损伤-50%（CalamityMod defenseDamageRatio） ──
            if (!HasDamageDebuff())
                Player.Calamity().defenseDamageRatio *= (1.0 - BalanceAegisBlade.BulwarkDefenseDamageReduction);

            // ── 埃癸斯被动：完美格挡后8秒无视五毒 ───────────────────────
            if (PerfectParryDefenseTimer > 0)
                ApplyFivePoisonsImmunity();

            // ── 庇护土墙战障强化：当在升起的土墙 160 像素范围内时获得增益 ─────
            int wallType = ModContent.ProjectileType<Projectiles.AegisWallProjectile>();
            bool nearWall = false;
            foreach (Projectile proj in Main.ActiveProjectiles)
            {
                if (proj.active && proj.type == wallType && proj.ai[1] >= 1f)
                {
                    if (Vector2.DistanceSquared(Player.Center, proj.Center) <= 160f * 160f)
                    {
                        nearWall = true;
                        break;
                    }
                }
            }

            if (nearWall)
            {
                Player.statDefense += 20;
                Player.GetDamage(DamageClass.Generic) += 0.15f;
                if (!Main.dedServ && Main.rand.NextBool(6))
                {
                    Dust d = Dust.NewDustPerfect(Player.Center + Main.rand.NextVector2Circular(Player.width * 0.5f, Player.height * 0.5f),
                        DustID.GoldFlame, -Vector2.UnitY * Main.rand.NextFloat(0.5f, 1.5f), 0, GoldOutline, 0.85f);
                    d.noGravity = true;
                }
            }
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
            // 掩体不再阻挡玩家，玩家可自由穿越
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
