using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using CalamityMod;
using CalamityLegendsComeBack.Weapons.GlacialEmbrace.EXSkill;
using CalamityLegendsComeBack.Weapons.GlacialEmbrace.LeftClick;

namespace CalamityLegendsComeBack.Weapons.GlacialEmbrace.General
{
    public class GlacialEmbracePlayer : ModPlayer
    {
        // 核心被动与增益状态
        public bool GlacialEmbraceMinion = false;
        public int CurrentMode = 0; // 0 = 斩击 (Slash), 1 = 突刺 (Pierce), 2 = 打击 (Strike)
        public int ModeTimer = 0;
        public const int ModeDuration = 600; // 10 秒自动轮换到下一组攻击方式
        public int FramesUntilModeSwitch => Math.Max(0, ModeDuration - ModeTimer);
        public int UltimateCharge = 0; // 充能上限 240
        public const int MaxUltimateCharge = 240;

        // 霜冻能乐 QTE 相关变量
        public int ComboCount = 0;
        public bool QteActive = false;
        public int QteTimer = 0;
        public int QteMax = 45;
        public int QteType = 0; // 0 = 模式切换, 1 = 特殊攻击释放

        // 打击形态对齐时间与状态
        public int StrikeAlignCooldown = 0;
        public bool StrikeAligned = false;

        // 极光旋律 & 冰川神性计时器
        public int AuroraMelodyTimer = 0;
        public int GlacialDivinityTimer = 0;
        public double LastSpecialHitTime = 0;

        // 连击属性加成
        public int LifeRegenBonus => ComboCount / 2; // 每 2 次 +1 Regen, 最大 10
        public int DefenseBonus => (ComboCount / 4) * 8; // 每 4 次 +8 Def, 最大 40

        public override void ResetEffects()
        {
            GlacialEmbraceMinion = false;
        }

        public override void UpdateLifeRegen()
        {
            if (GlacialEmbraceMinion && ComboCount > 0)
            {
                Player.lifeRegen += LifeRegenBonus;
            }
        }

        public override void PostUpdateEquips()
        {
            if (GlacialEmbraceMinion && ComboCount > 0)
            {
                Player.statDefense += DefenseBonus;
            }
            if (GlacialEmbraceMinion && AuroraMelodyTimer > 0)
            {
                Player.statDefense += 10;
            }
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (GlacialEmbraceMinion && AuroraMelodyTimer > 0)
            {
                modifiers.SourceDamage *= 0.85f; // 受到防前伤害减少15%
            }
        }

        public override void PreUpdate()
        {
            bool holdingWeapon = Player.HeldItem.type == ModContent.ItemType<GlacialEmbrace>();
            if (!GlacialEmbraceMinion)
            {
                ComboCount = 0;
                QteActive = false;
                AuroraMelodyTimer = 0;
                GlacialDivinityTimer = 0;
                StrikeAligned = false;
                StrikeAlignCooldown = 0;
                ModeTimer = 0;
                return;
            }

            if (!holdingWeapon)
            {
                ComboCount = 0;
                QteActive = false;
            }

            // 动态调整形态导致的召唤物栏位超出限制强制执行清理
            EnforceMinionLimit();

            // 被动计时器更新
            if (AuroraMelodyTimer > 0) AuroraMelodyTimer--;
            if (GlacialDivinityTimer > 0) GlacialDivinityTimer--;

            // 召唤物攻击方式自动轮换。蓄力特殊攻击期间暂停，避免刚松手就切组。
            bool chargingSpecial = Player.ownedProjectileCounts[ModContent.ProjectileType<GlacialEmbraceChargeHoldout>()] > 0;
            if (!chargingSpecial)
            {
                ModeTimer++;
                if (ModeTimer >= ModeDuration)
                    AdvanceMode(false);
            }

            // 打击形态对齐计时器更新
            if (CurrentMode == 2 && GlacialEmbraceMinion)
            {
                if (StrikeAlignCooldown > 0)
                {
                    StrikeAlignCooldown--;
                    StrikeAligned = false;
                }
                else
                {
                    // 冷却完毕，开始对齐
                    StrikeAligned = true;
                    AlignActiveSpikes();
                }
            }
            else
            {
                StrikeAligned = false;
                StrikeAlignCooldown = 0;
            }

            // QTE 倒计时逻辑
            if (QteActive)
            {
                QteTimer++;
                if (QteTimer > QteMax)
                {
                    // 超时未操作，判定卡点失败
                    TriggerQteFail();
                }
            }

            // 检测模式切换按键
            if (holdingWeapon && KeybindSystem.LegendaryWeaponFormSwitch?.JustPressed == true)
            {
                HandleModeSwitchKey();
            }

            // 检测终结技释放按键
            if (holdingWeapon && KeybindSystem.LegendarySkill?.JustPressed == true && UltimateCharge >= MaxUltimateCharge)
            {
                TriggerUltimateAbility();
            }
        }

        // 打击形态下的冰刺对齐驱动
        private void AlignActiveSpikes()
        {
            int spikeType = ModContent.ProjectileType<IceSpikeMinion>();
            var activeSpikes = new List<Projectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == spikeType && p.owner == Player.whoAmI)
                {
                    var modProj = p.ModProjectile as IceSpikeMinion;
                    if (modProj != null && !modProj.hibernating && !modProj.smashing)
                    {
                        activeSpikes.Add(p);
                    }
                }
            }

            if (activeSpikes.Count == 0) return;

            Vector2 dir = (Player.Calamity().mouseWorld - Player.Center).SafeNormalize(Vector2.UnitX * Player.direction);
            Vector2 ortho = new Vector2(-dir.Y, dir.X);

            for (int i = 0; i < activeSpikes.Count; i++)
            {
                var modProj = activeSpikes[i].ModProjectile as IceSpikeMinion;
                if (modProj != null)
                {
                    modProj.AlignForStrike(dir, ortho, i, activeSpikes.Count);
                }
            }
        }

        private void HandleModeSwitchKey()
        {
            if (Player.noItems || Player.CCed || Main.mapFullscreen)
                return;

            // 检查当前模式切换是否为卡点
            if (QteActive && QteType == 0)
            {
                // 卡点甜点区：在 QTEMax 的 70% 到 90% 范围内按键成功
                int sweetMin = (int)(QteMax * 0.7f);
                int sweetMax = (int)(QteMax * 0.9f);
                if (QteTimer >= sweetMin && QteTimer <= sweetMax)
                {
                    TriggerQteSuccess();
                }
                else
                {
                    TriggerQteFail();
                }
            }
            else
            {
                // 开启模式切换 QTE
                QteActive = true;
                QteTimer = 0;
                QteMax = 40; // 模式切换 QTE 时长为 40 帧
                QteType = 0;
            }

            AdvanceMode(true);
        }

        private void AdvanceMode(bool manual)
        {
            CurrentMode = (CurrentMode + 1) % 3;
            ModeTimer = 0;
            StrikeAligned = false;
            StrikeAlignCooldown = CurrentMode == 2 ? 120 : 0;

            GlacialEmbrace.SyncSpikesForMode(Player);
            SoundEngine.PlaySound(SoundID.Item22 with { Pitch = manual ? 0.35f : 0.05f, Volume = manual ? 0.65f : 0.45f }, Player.Center);
        }

        public void TriggerQteSuccess()
        {
            ComboCount = Math.Min(20, ComboCount + 1);
            QteActive = false;
            
            // 提示音效：清脆清亮的水晶声音
            SoundEngine.PlaySound(SoundID.Item30 with { Pitch = 0.8f, Volume = 0.9f }, Player.Center);

            // 每 5 次成功恢复 60 帧飞行时间
            if (ComboCount % 5 == 0)
            {
                Player.wingTime = Math.Min(Player.wingTimeMax, Player.wingTime + 60);
                // 播发额外闪光音效
                SoundEngine.PlaySound(SoundID.Item4 with { Pitch = 0.5f, Volume = 0.7f }, Player.Center);
            }

            // 满 10 次踩点触发“冰川神性”
            if (ComboCount >= 10)
            {
                GlacialDivinityTimer = 300; // 5秒
                SoundEngine.PlaySound(SoundID.Item29 with { Pitch = 0.2f, Volume = 0.8f }, Player.Center);
            }

            // 弹出完美的漂浮文字提示
            CombatText.NewText(Player.getRect(), new Color(0, 255, 255), $"Perfect! Combo: {ComboCount}", true);
        }

        public void TriggerQteFail()
        {
            ComboCount = 0;
            QteActive = false;
            GlacialDivinityTimer = 0; // 踩点失败自动结束冰川神性

            // 哑火失败音效：沉闷的石头碎裂声
            SoundEngine.PlaySound(SoundID.Dig with { Pitch = -0.4f, Volume = 0.8f }, Player.Center);
            CombatText.NewText(Player.getRect(), new Color(255, 80, 80), "Miss!", true);
        }

        public void OnSpecialHitNPC()
        {
            double currentFrame = Main.GameUpdateCount;
            double diff = currentFrame - LastSpecialHitTime;
            
            // 极光旋律判断：命中间隔不超过 45 帧（即 0.75 秒）
            if (diff <= 45 && diff > 0)
            {
                AuroraMelodyTimer = 300; // 获得 5 秒 Buff
            }
            LastSpecialHitTime = currentFrame;
        }

        // 鞭子命中敌人，触发 1/6 数量的钉入冰刺贯穿
        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (proj.owner == Player.whoAmI && ProjectileID.Sets.IsAWhip[proj.type])
            {
                TriggerEmbeddedSpikePierce(target, 1); // 鞭子触发 1 个冰刺贯穿
            }
        }

        public void TriggerEmbeddedSpikePierce(NPC target, int count)
        {
            int spikeType = ModContent.ProjectileType<IceSpikeMinion>();
            var candidates = new List<IceSpikeMinion>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == spikeType && p.owner == Player.whoAmI)
                {
                    var spike = p.ModProjectile as IceSpikeMinion;
                    if (spike != null && spike.embedded && spike.embedNPCIndex == target.whoAmI)
                    {
                        candidates.Add(spike);
                    }
                }
            }

            if (candidates.Count == 0) return;

            // 随机洗牌并挑选 count 个冰刺贯穿
            var rand = Main.rand;
            var targets = candidates.OrderBy(x => rand.Next()).Take(count);
            foreach (var spike in targets)
            {
                spike.PierceThrust(Vector2.Zero);
            }
        }

        private void TriggerUltimateAbility()
        {
            // 召唤终结技神钻
            if (Player.ownedProjectileCounts[ModContent.ProjectileType<GlacialDrillProj>()] > 0)
                return;

            UltimateCharge = 0; // 扣除所有能量

            // 寻找不占栏位冰块随从和冰刺，执行终结技大装配
            var source = Player.GetSource_FromThis();
            int drillProj = Projectile.NewProjectile(source, Player.Center, Vector2.Zero, ModContent.ProjectileType<GlacialDrillProj>(), (int)(Player.HeldItem.damage * 2.5f), 4.5f, Player.whoAmI);
            
            SoundEngine.PlaySound(SoundID.Item123 with { Pitch = -0.2f, Volume = 1f }, Player.Center);
        }

        private void EnforceMinionLimit()
        {
            int spikeType = ModContent.ProjectileType<IceSpikeMinion>();
            
            // 计算总占用的仆从栏位数
            float occupied = 0f;
            var spikes = new List<Projectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == Player.whoAmI)
                {
                    if (p.minion)
                    {
                        occupied += p.minionSlots;
                    }
                    if (p.type == spikeType)
                    {
                        spikes.Add(p);
                    }
                }
            }

            // 如果总占用栏位超出上限，回收并销毁最早的冰刺随从
            if (occupied > Player.maxMinions && spikes.Count > 0)
            {
                spikes = spikes.OrderBy(x => x.whoAmI).ToList();
                bool killedAny = false;
                while (occupied > Player.maxMinions && spikes.Count > 0)
                {
                    Projectile oldest = spikes[0];
                    occupied -= oldest.minionSlots;
                    oldest.Kill();
                    spikes.RemoveAt(0);
                    killedAny = true;
                }
                if (killedAny)
                {
                    GlacialEmbrace.RearrangeSpikes(Player);
                }
            }
        }
    }
}
