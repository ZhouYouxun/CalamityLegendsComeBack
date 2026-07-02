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
using CalamityLegendsComeBack.Weapons.GlacialEmbrace.Passive;

namespace CalamityLegendsComeBack.Weapons.GlacialEmbrace.General
{
    public class GlacialEmbracePlayer : ModPlayer
    {
        // ── 极地三相封印：FrostSealState ──────────────────────────────────────
        public struct FrostSealState
        {
            public byte RiftStacks;    // 裂痕印记（斩击模式命中），0-3
            public byte PierceStacks;  // 穿刺印记（突刺模式命中），0-3
            public byte StrikeStacks;  // 重创印记（打击模式命中），0-3
            public int  RiftFadeTimer;
            public int  PierceFadeTimer;
            public int  StrikeFadeTimer;
            public bool Sealed;
            public int  SealTimer;
            public byte CascadeDepth;  // 已经发生的连锁层数，上限 3

            public bool HasAnyMark  => RiftStacks > 0 || PierceStacks > 0 || StrikeStacks > 0;
            public bool HasAllTypes => RiftStacks > 0 && PierceStacks > 0 && StrikeStacks > 0;
            public int  TotalBonusStacks =>
                Math.Max(0, RiftStacks  - 1)
              + Math.Max(0, PierceStacks - 1)
              + Math.Max(0, StrikeStacks - 1);
        }

        // ── 封印系统常量 ──────────────────────────────────────────────────────
        public const int RiftMarkDuration    = 360;   // 裂痕印记持续 6 秒（衰减最快）
        public const int PierceMarkDuration  = 480;   // 穿刺印记持续 8 秒
        public const int StrikeMarkDuration  = 600;   // 重创印记持续 10 秒（衰减最慢）
        public const int SealCountdown       = 180;   // 封印倒计时 3 秒后爆发
        public const int ResonanceEchoDuration = 180; // 共鸣余波持续 3 秒

        // ── 核心被动与增益状态 ────────────────────────────────────────────────
        public bool GlacialEmbraceMinion = false;
        public bool HasGlacialEmbraceInInventory { get; private set; } = false;
        public bool AncientIceShieldActive { get; private set; } = false;
        public int HeldSupportClass { get; private set; } = -1;
        public int  CurrentMode = 0;          // 0 = 斩击, 1 = 突刺, 2 = 打击
        public int  ModeTimer   = 0;
        public const int ModeDuration = 300;  // 5 秒自动轮换
        public int  FramesUntilModeSwitch => Math.Max(0, ModeDuration - ModeTimer);
        public int  UltimateCharge = 0;
        public const int MaxUltimateCharge = 240;
        public const int SupportClassMelee = 0;
        public const int SupportClassRanged = 1;
        public const int SupportClassMagic = 2;
        public const int SupportClassRogue = 3;
        private const int StillnessFramesForShield = 45;
        private const int AncientIceShieldDefense = 36;
        private const float AncientIceShieldDamageReduction = 0.22f;
        private const int SupportRespawnInterval = 24;
        private int stillnessTimer = 0;
        private int supportSpawnTimer = 0;

        // ── 右键 QTE ─────────────────────────────────────────────────────────
        public bool QteActive  = false;
        public int  QteTimer   = 0;
        public int  QteMax     = 50;
        public const int QteSuccessMin = 35;
        public const int QteSuccessMax = 45;
        public int  ComboCount = 0;

        // ── 打击形态对齐 ──────────────────────────────────────────────────────
        public int  StrikeAlignCooldown = 0;
        public bool StrikeAligned       = false;
        public int  LeftSpecialCooldown = 0;

        // ── 极光旋律 & 冰川神性 ───────────────────────────────────────────────
        public int    AuroraMelodyTimer    = 0;
        public int    GlacialDivinityTimer = 0;
        public double LastSpecialHitTime   = 0;

        // ── 连击属性 ──────────────────────────────────────────────────────────
        public int LifeRegenBonus => ComboCount / 2;
        public int DefenseBonus   => (ComboCount / 4) * 8;

        // ── 极地三相封印 ──────────────────────────────────────────────────────
        public Dictionary<int, FrostSealState> SealStates = new Dictionary<int, FrostSealState>();
        public int ResonanceEchoTimer = 0;
        private List<(int npcId, byte depth)> pendingCascades = new List<(int, byte)>();

        // ── 切换特效计时器 ────────────────────────────────────────────────────
        private int modeFlashTimer = 0;

        // ─────────────────────────────────────────────────────────────────────

        public override void ResetEffects()
        {
            GlacialEmbraceMinion = false;
            HasGlacialEmbraceInInventory = HasGlacialEmbraceItem();
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
            if (AncientIceShieldActive)
            {
                Player.statDefense += AncientIceShieldDefense;
                Player.endurance += AncientIceShieldDamageReduction;
            }
            if (GlacialEmbraceMinion && AuroraMelodyTimer > 0)
                Player.statDefense += 10;
            if (GlacialEmbraceMinion && ResonanceEchoTimer > 0)
                Player.statDefense += 8; // 共鸣余波额外防御
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (GlacialEmbraceMinion && AuroraMelodyTimer > 0)
                modifiers.SourceDamage *= 0.85f;
            if (AncientIceShieldActive)
                modifiers.SourceDamage *= 1f - AncientIceShieldDamageReduction;
        }

        public override void PreUpdate()
        {
            HasGlacialEmbraceInInventory = HasGlacialEmbraceItem();
            if (Player.HasBuff(ModContent.BuffType<GlacialEmbraceBuff>()))
                GlacialEmbraceMinion = true;

            bool holdingWeapon = Player.HeldItem.type == ModContent.ItemType<GlacialEmbrace>();
            HeldSupportClass = GetHeldSupportClass();
            UpdateAncientIceShield();

            if (!GlacialEmbraceMinion)
            {
                ComboCount          = 0;
                QteActive           = false;
                AuroraMelodyTimer   = 0;
                GlacialDivinityTimer = 0;
                StrikeAligned       = false;
                StrikeAlignCooldown = 0;
                LeftSpecialCooldown = 0;
                ModeTimer           = 0;
                modeFlashTimer      = 0;
                SealStates.Clear();
                pendingCascades.Clear();
                ResonanceEchoTimer  = 0;
                HeldSupportClass = -1;
                AncientIceShieldActive = false;
                stillnessTimer = 0;
                return;
            }

            if (!holdingWeapon)
            {
                ComboCount = 0;
                QteActive  = false;
            }

            EnforceMinionLimit();
            EnsureSupportFamiliar();

            if (AuroraMelodyTimer   > 0) AuroraMelodyTimer--;
            if (GlacialDivinityTimer > 0) GlacialDivinityTimer--;
            if (LeftSpecialCooldown > 0) LeftSpecialCooldown--;
            if (modeFlashTimer      > 0) modeFlashTimer--;

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
                if (StrikeAlignCooldown > 0) { StrikeAlignCooldown--; StrikeAligned = false; }
                else { StrikeAligned = true; AlignActiveSpikes(); }
            }
            else { StrikeAligned = false; StrikeAlignCooldown = 0; }

            // 右键 QTE（仅本地玩家）
            if (holdingWeapon && Main.myPlayer == Player.whoAmI)
            {
                bool rightDown     = Main.mouseRight;
                bool rightReleased = Main.mouseRightRelease;

                if (rightDown && !QteActive)
                {
                    QteActive = true;
                    QteTimer  = 0;
                }
                else if (QteActive)
                {
                    if (!rightReleased && rightDown)
                    {
                        QteTimer++;
                        if (QteTimer > QteMax) TriggerQteFail();
                    }
                    else
                    {
                        if (QteTimer >= QteSuccessMin && QteTimer <= QteSuccessMax)
                            TriggerQteSuccess();
                        else if (QteTimer > 5)
                            TriggerQteFail();
                        else { QteActive = false; QteTimer = 0; }
                    }
                }
            }

            // 三相封印帧更新
            UpdateSealStates();

            // 终结技释放
            if (holdingWeapon && KeybindSystem.LegendarySkill?.JustPressed == true && UltimateCharge >= MaxUltimateCharge)
                TriggerUltimateAbility();
        }

        // ─────────────────────────────────────────────────────────────────────

        private void AlignActiveSpikes()
        {
            int spikeType  = ModContent.ProjectileType<IceSpikeMinion>();
            var activeSpikes = new List<Projectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (!p.active || p.type != spikeType || p.owner != Player.whoAmI) continue;
                var mp = p.ModProjectile as IceSpikeMinion;
                if (mp != null && !mp.hibernating && !mp.smashing && mp.IsCirclingPlayer())
                    activeSpikes.Add(p);
            }
            if (activeSpikes.Count == 0) return;

            Vector2 dir   = (Player.Calamity().mouseWorld - Player.Center).SafeNormalize(Vector2.UnitX * Player.direction);
            Vector2 ortho = new Vector2(-dir.Y, dir.X);
            for (int i = 0; i < activeSpikes.Count; i++)
                (activeSpikes[i].ModProjectile as IceSpikeMinion)?.AlignForStrike(dir, ortho, i, activeSpikes.Count);
        }

        private void AdvanceMode()
        {
            CurrentMode         = (CurrentMode + 1) % 3;
            ModeTimer           = 0;
            StrikeAligned       = false;
            StrikeAlignCooldown = CurrentMode == 2 ? 120 : 0;
            modeFlashTimer      = 30;

            GlacialEmbrace.SyncSpikesForMode(Player);

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

            float pitch = CurrentMode == 0 ? 0f : CurrentMode == 1 ? 0.25f : 0.5f;
            SoundEngine.PlaySound(SoundID.Item22 with { Pitch = pitch, Volume = 0.7f }, Player.Center);
        }

        public void TriggerQteSuccess()
        {
            ComboCount = Math.Min(20, ComboCount + 1);
            QteActive  = false;
            QteTimer   = 0;

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

            // QTE 完美命中→触发最近封印提前爆发
            TryEarlyDetonation();
        }

        public void TriggerQteFail()
        {
            ComboCount       = 0;
            QteActive        = false;
            QteTimer         = 0;
            GlacialDivinityTimer = 0;

            SoundEngine.PlaySound(SoundID.Dig with { Pitch = -0.4f, Volume = 0.8f }, Player.Center);
            CombatText.NewText(Player.getRect(), new Color(255, 80, 80), "Miss!", true);
        }

        public void OnSpecialHitNPC()
        {
            double cur  = Main.GameUpdateCount;
            double diff = cur - LastSpecialHitTime;
            if (diff <= 45 && diff > 0)
                AuroraMelodyTimer = 300;
            LastSpecialHitTime = cur;
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (GlacialEmbraceMinion && proj.owner == Player.whoAmI && ProjectileID.Sets.IsAWhip[proj.type])
                TriggerWhipFrenzy(target);
        }

        private void TriggerWhipFrenzy(NPC target)
        {
            ComboCount       = Math.Min(20, ComboCount + 1);
            AuroraMelodyTimer   = Math.Max(AuroraMelodyTimer, 180);
            GlacialDivinityTimer = Math.Max(GlacialDivinityTimer, 180);
            UltimateCharge   = Math.Min(MaxUltimateCharge, UltimateCharge + 3);

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
            int spikeType   = ModContent.ProjectileType<IceSpikeMinion>();
            var candidates  = new List<IceSpikeMinion>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (!p.active || p.type != spikeType || p.owner != Player.whoAmI) continue;
                var spike = p.ModProjectile as IceSpikeMinion;
                if (spike != null && spike.embedded && spike.embedNPCIndex == target.whoAmI)
                    candidates.Add(spike);
            }
            if (candidates.Count == 0) return;

            foreach (var spike in candidates.OrderBy(x => Main.rand.Next()).Take(count))
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

        // ── 极地三相封印系统 ──────────────────────────────────────────────────

        /// <summary>
        /// 由冰刺命中时调用，向目标添加当前模式对应的印记。
        /// 三种印记集齐后自动进入封印倒计时。
        /// </summary>
        public void ApplyFrostMark(NPC target, int mode, byte stacks = 1)
        {
            if (!GlacialEmbraceMinion || target == null || !target.active) return;
            int id = target.whoAmI;
            if (!SealStates.TryGetValue(id, out var state))
                state = new FrostSealState();

            int divBonus = GlacialDivinityTimer > 0 ? 180 : 0; // 神性激活时延长持续时间 3 秒

            switch (mode)
            {
                case 0: // 斩击 → 裂痕印记，衰减最快
                    state.RiftStacks    = (byte)Math.Min(3, state.RiftStacks + stacks);
                    state.RiftFadeTimer = RiftMarkDuration + divBonus;
                    break;
                case 1: // 突刺 → 穿刺印记
                    state.PierceStacks    = (byte)Math.Min(3, state.PierceStacks + stacks);
                    state.PierceFadeTimer = PierceMarkDuration + divBonus;
                    break;
                case 2: // 打击 → 重创印记，衰减最慢
                    state.StrikeStacks    = (byte)Math.Min(3, state.StrikeStacks + stacks);
                    state.StrikeFadeTimer = StrikeMarkDuration + divBonus;
                    break;
            }

            // 三种印记集齐 → 触发封印倒计时
            if (!state.Sealed && state.HasAllTypes)
            {
                state.Sealed    = true;
                state.SealTimer = SealCountdown;
                if (Main.myPlayer == Player.whoAmI)
                {
                    SoundEngine.PlaySound(SoundID.Item29 with { Pitch = 0.35f, Volume = 0.75f }, target.Center);
                    CombatText.NewText(target.getRect(), new Color(0, 210, 255), "SEALED!", true);
                }
            }

            SealStates[id] = state;
        }

        // 连锁爆炸：将附近敌人直接标记三相并设置快速倒计时
        private void CascadeChain(int npcId, byte depth)
        {
            if (!Main.npc.IndexInRange(npcId)) return;
            NPC npc = Main.npc[npcId];
            if (!npc.active || npc.friendly) return;

            if (!SealStates.TryGetValue(npcId, out var state))
                state = new FrostSealState();

            const int cascadeTime = 240; // 4 秒标记持续
            state.RiftStacks    = (byte)Math.Max((int)state.RiftStacks,   1);
            state.PierceStacks  = (byte)Math.Max((int)state.PierceStacks, 1);
            state.StrikeStacks  = (byte)Math.Max((int)state.StrikeStacks, 1);
            state.RiftFadeTimer   = Math.Max(state.RiftFadeTimer,   cascadeTime);
            state.PierceFadeTimer = Math.Max(state.PierceFadeTimer, cascadeTime);
            state.StrikeFadeTimer = Math.Max(state.StrikeFadeTimer, cascadeTime);
            state.Sealed        = true;
            state.SealTimer     = 30;  // 0.5 秒快速引爆，制造连锁节奏感
            state.CascadeDepth  = depth;
            SealStates[npcId]   = state;
        }

        // 每帧更新所有印记计时，处理封印倒计时与连锁爆炸
        private void UpdateSealStates()
        {
            if (ResonanceEchoTimer > 0) ResonanceEchoTimer--;
            if (SealStates.Count == 0 && pendingCascades.Count == 0) return;

            var toRemove   = new List<int>(4);
            var toDetonate = new List<(int id, FrostSealState state)>(4);

            foreach (var kvp in SealStates)
            {
                int id = kvp.Key;
                var s  = kvp.Value;

                // NPC 已死亡 / 不存在 → 放弃封印（印记随 NPC 消散）
                if (!Main.npc.IndexInRange(id) || !Main.npc[id].active || Main.npc[id].life <= 0)
                {
                    toRemove.Add(id);
                    continue;
                }

                // 逐帧衰减各印记计时器
                if (s.RiftStacks   > 0) { s.RiftFadeTimer--;   if (s.RiftFadeTimer   <= 0) s.RiftStacks   = 0; }
                if (s.PierceStacks > 0) { s.PierceFadeTimer--; if (s.PierceFadeTimer <= 0) s.PierceStacks = 0; }
                if (s.StrikeStacks > 0) { s.StrikeFadeTimer--; if (s.StrikeFadeTimer <= 0) s.StrikeStacks = 0; }

                // 某种印记消失 → 解除封印
                if (s.Sealed && !s.HasAllTypes)
                {
                    s.Sealed    = false;
                    s.SealTimer = 0;
                }

                // 封印倒计时
                if (s.Sealed)
                {
                    s.SealTimer--;
                    if (s.SealTimer <= 0)
                    {
                        toDetonate.Add((id, s));
                        toRemove.Add(id);
                        continue;
                    }
                }

                if (!s.HasAnyMark)
                    toRemove.Add(id);
                else
                    SealStates[id] = s;
            }

            foreach (int id in toRemove)
                SealStates.Remove(id);

            // 触发所有到期的爆发
            foreach (var (id, state) in toDetonate)
                TriggerSealDetonation(id, state);

            // 处理连锁传播（由 TriggerSealDetonation 填充）
            foreach (var (cid, depth) in pendingCascades)
                CascadeChain(cid, depth);
            pendingCascades.Clear();
        }

        private void TriggerSealDetonation(int npcId, FrostSealState state)
        {
            if (!Main.npc.IndexInRange(npcId)) return;
            NPC npc = Main.npc[npcId];
            if (!npc.active) return;

            // 伤害计算：基础 3× + 额外叠层加成 15% 每层，神性激活 1.5×，连锁层次递减 20%
            int baseDmg = Player.HeldItem.type == ModContent.ItemType<GlacialEmbrace>()
                ? Player.HeldItem.damage : 48;
            float multiplier = (3.0f + state.TotalBonusStacks * 0.15f)
                             * (GlacialDivinityTimer > 0 ? 1.5f : 1f)
                             * MathF.Pow(0.8f, state.CascadeDepth);
            int finalDmg = Math.Max(1, (int)(baseDmg * multiplier));

            if (Main.myPlayer == Player.whoAmI)
            {
                var src = Player.GetSource_FromThis();

                // 冰晶封印爆发弹幕
                int dProj = Projectile.NewProjectile(src, npc.Center, Vector2.Zero,
                    ModContent.ProjectileType<FrostSealDetonation>(), finalDmg, 8f, Player.whoAmI);
                if (Main.projectile.IndexInRange(dProj))
                {
                    Main.projectile[dProj].DamageType = DamageClass.Summon;
                    Main.projectile[dProj].friendly   = true;
                }

                // 神性激活时额外留下余辉残区
                if (GlacialDivinityTimer > 0)
                {
                    int aProj = Projectile.NewProjectile(src, npc.Center, Vector2.Zero,
                        ModContent.ProjectileType<FrostSealAfterglow>(),
                        (int)(finalDmg * 0.6f), 0f, Player.whoAmI);
                    if (Main.projectile.IndexInRange(aProj))
                    {
                        Main.projectile[aProj].DamageType = DamageClass.Summon;
                        Main.projectile[aProj].friendly   = true;
                    }
                }
            }

            // 玩家奖励
            ComboCount         = Math.Min(20, ComboCount + 1 + state.CascadeDepth);
            UltimateCharge     = Math.Min(MaxUltimateCharge, UltimateCharge + 15);
            AuroraMelodyTimer  = Math.Max(AuroraMelodyTimer, 180);
            ResonanceEchoTimer = ResonanceEchoDuration;

            string burstText = state.CascadeDepth == 0
                ? "CRYOBURST!"
                : $"CHAIN ×{state.CascadeDepth + 1}!";
            CombatText.NewText(npc.getRect(), new Color(255, 240, 80), burstText, true);

            // 向附近敌人传播连锁（最多 3 层，每次最多扩散 3 个）
            if (state.CascadeDepth < 3 && Main.myPlayer == Player.whoAmI)
            {
                byte nextDepth = (byte)(state.CascadeDepth + 1);
                int  chained   = 0;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC nearby = Main.npc[i];
                    if (!nearby.active || nearby.friendly || nearby.whoAmI == npcId) continue;
                    if (!nearby.CanBeChasedBy()) continue;
                    if (Vector2.Distance(nearby.Center, npc.Center) > 300f) continue;
                    pendingCascades.Add((nearby.whoAmI, nextDepth));
                    if (++chained >= 3) break;
                }
            }
        }

        /// <summary>
        /// QTE 完美命中时，立即引爆最快到期的封印敌人（提前触发）。
        /// </summary>
        public void TryEarlyDetonation()
        {
            int bestId      = -1;
            int lowestTimer = int.MaxValue;
            foreach (var kvp in SealStates)
            {
                if (!kvp.Value.Sealed) continue;
                if (kvp.Value.SealTimer < lowestTimer)
                {
                    lowestTimer = kvp.Value.SealTimer;
                    bestId      = kvp.Key;
                }
            }
            if (bestId < 0) return;

            var s = SealStates[bestId];
            SealStates.Remove(bestId);

            if (!Main.npc.IndexInRange(bestId)) return;
            NPC npc = Main.npc[bestId];
            if (!npc.active) return;

            SoundEngine.PlaySound(SoundID.Item122 with { Pitch = 0.25f, Volume = 0.9f }, npc.Center);
            CombatText.NewText(npc.getRect(), new Color(0, 255, 190), "EARLY BURST!", true);
            TriggerSealDetonation(bestId, s);
        }

        // ─────────────────────────────────────────────────────────────────────

        private bool HasGlacialEmbraceItem()
        {
            int glacialType = ModContent.ItemType<GlacialEmbrace>();
            for (int i = 0; i < Player.inventory.Length; i++)
            {
                Item item = Player.inventory[i];
                if (item != null && !item.IsAir && item.type == glacialType)
                    return true;
            }
            return false;
        }

        private int GetHeldSupportClass()
        {
            if (!HasGlacialEmbraceInInventory)
                return -1;

            Item item = Player.HeldItem;
            if (item == null || item.IsAir || item.damage <= 0 || item.type == ModContent.ItemType<GlacialEmbrace>())
                return -1;

            DamageClass damageClass = item.DamageType;
            if (damageClass == DamageClass.Melee || damageClass == DamageClass.MeleeNoSpeed)
                return SupportClassMelee;
            if (damageClass == DamageClass.Ranged)
                return SupportClassRanged;
            if (damageClass == DamageClass.Magic)
                return SupportClassMagic;
            if (damageClass == ModContent.GetInstance<RogueDamageClass>())
                return SupportClassRogue;

            return -1;
        }

        private void UpdateAncientIceShield()
        {
            bool still = HasGlacialEmbraceInInventory
                && Math.Abs(Player.velocity.X) < 0.08f
                && Math.Abs(Player.velocity.Y) < 0.08f
                && !Player.controlLeft
                && !Player.controlRight
                && !Player.controlJump
                && !Player.controlDown
                && !Player.controlUp;

            stillnessTimer = still ? Math.Min(StillnessFramesForShield, stillnessTimer + 1) : 0;
            AncientIceShieldActive = stillnessTimer >= StillnessFramesForShield;

            if (Main.myPlayer == Player.whoAmI
                && AncientIceShieldActive
                && Player.ownedProjectileCounts[ModContent.ProjectileType<AncientIceShieldVisual>()] <= 0)
            {
                Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero,
                    ModContent.ProjectileType<AncientIceShieldVisual>(), 0, 0f, Player.whoAmI);
            }
        }

        private void EnsureSupportFamiliar()
        {
            if (Main.myPlayer != Player.whoAmI || !HasGlacialEmbraceInInventory || HeldSupportClass < 0)
                return;

            if (supportSpawnTimer > 0)
            {
                supportSpawnTimer--;
                return;
            }

            int familiarType = ModContent.ProjectileType<GlacialWeaponFamiliar>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == Player.whoAmI && projectile.type == familiarType
                    && (int)projectile.ai[0] == HeldSupportClass)
                    return;
            }

            int damage = Math.Max(1, (int)(Player.GetWeaponDamage(Player.HeldItem) * 0.65f));
            Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero,
                familiarType, damage, 2f, Player.whoAmI, HeldSupportClass);
            supportSpawnTimer = SupportRespawnInterval;
        }

        private void EnforceMinionLimit()
        {
            int spikeType = ModContent.ProjectileType<IceSpikeMinion>();
            float occupied = 0f;
            var spikes = new List<Projectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (!p.active || p.owner != Player.whoAmI) continue;
                if (p.minion) occupied += p.minionSlots;
                if (p.type == spikeType) spikes.Add(p);
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

        public bool  IsModeFlashing    => modeFlashTimer > 0;
        public float ModeFlashProgress => modeFlashTimer / 30f;
    }
}
