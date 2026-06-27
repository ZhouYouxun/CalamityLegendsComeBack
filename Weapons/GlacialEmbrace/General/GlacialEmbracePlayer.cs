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
        public int CurrentMode = 0; // 0 = 斩击, 1 = 突刺, 2 = 打击
        public int ModeTimer = 0;
        public const int ModeDuration = 300; // 5秒自动轮换
        public int FramesUntilModeSwitch => Math.Max(0, ModeDuration - ModeTimer);
        public int UltimateCharge = 0;
        public const int MaxUltimateCharge = 240;

        // 右键 QTE（连击被动）
        public bool QteActive = false;
        public int QteTimer = 0;
        public int QteMax = 50;
        public const int QteSuccessMin = 35; // 70% of 50
        public const int QteSuccessMax = 45; // 90% of 50
        public int ComboCount = 0;

        // 打击形态对齐
        public int StrikeAlignCooldown = 0;
        public bool StrikeAligned = false;
        public int LeftSpecialCooldown = 0;

        // 极光旋律 & 冰川神性
        public int AuroraMelodyTimer = 0;
        public int GlacialDivinityTimer = 0;
        public double LastSpecialHitTime = 0;

        // 连击属性
        public int LifeRegenBonus => ComboCount / 2;
        public int DefenseBonus => (ComboCount / 4) * 8;

        // 切换特效计时器（客户端显示用）
        private int modeFlashTimer = 0;

        public override void ResetEffects()
        {
            GlacialEmbraceMinion = false;
        }

        public override void UpdateLifeRegen()
        {
            if (GlacialEmbraceMinion && ComboCount > 0)
                Player.lifeRegen += LifeRegenBonus;
        }

        public override void PostUpdateEquips()
        {
            if (GlacialEmbraceMinion && ComboCount > 0)
                Player.statDefense += DefenseBonus;
            if (GlacialEmbraceMinion && AuroraMelodyTimer > 0)
                Player.statDefense += 10;
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (GlacialEmbraceMinion && AuroraMelodyTimer > 0)
                modifiers.SourceDamage *= 0.85f;
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
                LeftSpecialCooldown = 0;
                ModeTimer = 0;
                modeFlashTimer = 0;
                return;
            }

            if (!holdingWeapon)
            {
                ComboCount = 0;
                QteActive = false;
            }

            EnforceMinionLimit();

            if (AuroraMelodyTimer > 0) AuroraMelodyTimer--;
            if (GlacialDivinityTimer > 0) GlacialDivinityTimer--;
            if (LeftSpecialCooldown > 0) LeftSpecialCooldown--;
            if (modeFlashTimer > 0) modeFlashTimer--;

            // 形态自动切换（蓄力攻击期间暂停）
            bool chargingSpecial = Player.ownedProjectileCounts[ModContent.ProjectileType<GlacialEmbraceChargeHoldout>()] > 0;
            if (!chargingSpecial)
            {
                ModeTimer++;
                if (ModeTimer >= ModeDuration)
                    AdvanceMode();
            }

            // 打击模式对齐
            if (CurrentMode == 2 && GlacialEmbraceMinion)
            {
                if (StrikeAlignCooldown > 0)
                {
                    StrikeAlignCooldown--;
                    StrikeAligned = false;
                }
                else
                {
                    StrikeAligned = true;
                    AlignActiveSpikes();
                }
            }
            else
            {
                StrikeAligned = false;
                StrikeAlignCooldown = 0;
            }

            // 右键 QTE（仅本地玩家）
            if (holdingWeapon && Main.myPlayer == Player.whoAmI)
            {
                bool rightDown = Main.mouseRight;
                bool rightReleased = Main.mouseRightRelease;

                if (rightDown && !QteActive)
                {
                    QteActive = true;
                    QteTimer = 0;
                }
                else if (QteActive)
                {
                    if (!rightReleased && rightDown)
                    {
                        QteTimer++;
                        if (QteTimer > QteMax)
                            TriggerQteFail();
                    }
                    else
                    {
                        // 松开右键 → 判定
                        if (QteTimer >= QteSuccessMin && QteTimer <= QteSuccessMax)
                            TriggerQteSuccess();
                        else if (QteTimer > 5)
                            TriggerQteFail();
                        else
                        {
                            QteActive = false;
                            QteTimer = 0;
                        }
                    }
                }
            }

            // 终结技释放
            if (holdingWeapon && KeybindSystem.LegendarySkill?.JustPressed == true && UltimateCharge >= MaxUltimateCharge)
                TriggerUltimateAbility();
        }

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
                    if (modProj != null && !modProj.hibernating && !modProj.smashing && modProj.IsCirclingPlayer())
                        activeSpikes.Add(p);
                }
            }
            if (activeSpikes.Count == 0) return;

            Vector2 dir = (Player.Calamity().mouseWorld - Player.Center).SafeNormalize(Vector2.UnitX * Player.direction);
            Vector2 ortho = new Vector2(-dir.Y, dir.X);
            for (int i = 0; i < activeSpikes.Count; i++)
            {
                var modProj = activeSpikes[i].ModProjectile as IceSpikeMinion;
                modProj?.AlignForStrike(dir, ortho, i, activeSpikes.Count);
            }
        }

        private void AdvanceMode()
        {
            CurrentMode = (CurrentMode + 1) % 3;
            ModeTimer = 0;
            StrikeAligned = false;
            StrikeAlignCooldown = CurrentMode == 2 ? 120 : 0;
            modeFlashTimer = 30;

            GlacialEmbrace.SyncSpikesForMode(Player);

            // 切换粒子环
            int[] dustTypes = { DustID.Frost, DustID.Ice, DustID.Electric };
            int dType = dustTypes[CurrentMode];
            for (int i = 0; i < 28; i++)
            {
                float angle = MathHelper.TwoPi * i / 28f;
                float speed = Main.rand.NextFloat(3.5f, 8f);
                Vector2 vel = angle.ToRotationVector2() * speed;
                Dust d = Dust.NewDustDirect(Player.Center, 0, 0, dType, vel.X, vel.Y);
                d.scale = Main.rand.NextFloat(1.1f, 2.0f);
                d.noGravity = true;
            }

            float pitch = CurrentMode == 0 ? 0.0f : CurrentMode == 1 ? 0.25f : 0.5f;
            SoundEngine.PlaySound(SoundID.Item22 with { Pitch = pitch, Volume = 0.7f }, Player.Center);
        }

        public void TriggerQteSuccess()
        {
            ComboCount = Math.Min(20, ComboCount + 1);
            QteActive = false;
            QteTimer = 0;

            SoundEngine.PlaySound(SoundID.Item30 with { Pitch = 0.8f, Volume = 0.9f }, Player.Center);

            if (ComboCount % 5 == 0)
            {
                Player.wingTime = Math.Min(Player.wingTimeMax, Player.wingTime + 60);
                SoundEngine.PlaySound(SoundID.Item4 with { Pitch = 0.5f, Volume = 0.7f }, Player.Center);
            }

            if (ComboCount >= 10)
            {
                GlacialDivinityTimer = 300;
                SoundEngine.PlaySound(SoundID.Item29 with { Pitch = 0.2f, Volume = 0.8f }, Player.Center);
            }

            CombatText.NewText(Player.getRect(), new Color(0, 255, 255), $"Perfect!  ×{ComboCount}", true);
        }

        public void TriggerQteFail()
        {
            ComboCount = 0;
            QteActive = false;
            QteTimer = 0;
            GlacialDivinityTimer = 0;

            SoundEngine.PlaySound(SoundID.Dig with { Pitch = -0.4f, Volume = 0.8f }, Player.Center);
            CombatText.NewText(Player.getRect(), new Color(255, 80, 80), "Miss!", true);
        }

        public void OnSpecialHitNPC()
        {
            double currentFrame = Main.GameUpdateCount;
            double diff = currentFrame - LastSpecialHitTime;
            if (diff <= 45 && diff > 0)
                AuroraMelodyTimer = 300;
            LastSpecialHitTime = currentFrame;
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (GlacialEmbraceMinion && proj.owner == Player.whoAmI && ProjectileID.Sets.IsAWhip[proj.type])
                TriggerWhipFrenzy(target);
        }

        private void TriggerWhipFrenzy(NPC target)
        {
            ComboCount = Math.Min(20, ComboCount + 1);
            AuroraMelodyTimer = Math.Max(AuroraMelodyTimer, 180);
            GlacialDivinityTimer = Math.Max(GlacialDivinityTimer, 180);
            UltimateCharge = Math.Min(MaxUltimateCharge, UltimateCharge + 3);

            int spikeType = ModContent.ProjectileType<IceSpikeMinion>();
            int commanded = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == spikeType && p.owner == Player.whoAmI && p.ModProjectile is IceSpikeMinion spike)
                {
                    spike.CommandWhipFrenzy(target, CurrentMode);
                    commanded++;
                }
            }

            if (commanded > 0)
                SoundEngine.PlaySound(SoundID.Item30 with { Pitch = 0.35f, Volume = 0.62f }, target.Center);
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
                        candidates.Add(spike);
                }
            }
            if (candidates.Count == 0) return;

            var targets = candidates.OrderBy(x => Main.rand.Next()).Take(count);
            foreach (var spike in targets)
                spike.PierceThrust(Vector2.Zero);
        }

        private void TriggerUltimateAbility()
        {
            if (Player.ownedProjectileCounts[ModContent.ProjectileType<GlacialDrillProj>()] > 0)
                return;

            UltimateCharge = 0;
            var source = Player.GetSource_FromThis();
            Projectile.NewProjectile(source, Player.Center, Vector2.Zero,
                ModContent.ProjectileType<GlacialDrillProj>(),
                (int)(Player.HeldItem.damage * 2.5f), 4.5f, Player.whoAmI);

            SoundEngine.PlaySound(SoundID.Item123 with { Pitch = -0.2f, Volume = 1f }, Player.Center);
        }

        private void EnforceMinionLimit()
        {
            int spikeType = ModContent.ProjectileType<IceSpikeMinion>();
            float occupied = 0f;
            var spikes = new List<Projectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == Player.whoAmI)
                {
                    if (p.minion) occupied += p.minionSlots;
                    if (p.type == spikeType) spikes.Add(p);
                }
            }

            if (occupied > Player.maxMinions && spikes.Count > 0)
            {
                spikes = spikes.OrderBy(x => x.whoAmI).ToList();
                while (occupied > Player.maxMinions && spikes.Count > 0)
                {
                    Projectile oldest = spikes[0];
                    occupied -= oldest.minionSlots;
                    oldest.Kill();
                    spikes.RemoveAt(0);
                }
                GlacialEmbrace.RearrangeSpikes(Player);
            }
        }

        public bool IsModeFlashing => modeFlashTimer > 0;
        public float ModeFlashProgress => modeFlashTimer / 30f;
    }
}
