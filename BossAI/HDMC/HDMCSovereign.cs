using System;
using System.Collections.Generic;
using System.IO;
using CalamityMod;
using CalamityLegendsComeBack.Systems;
using CalamityLegendsComeBack.Weapons.A_Dev.HyperdimensionalMatrixCore;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using HDMCWeaponItem = CalamityLegendsComeBack.Weapons.A_Dev.HyperdimensionalMatrixCore.HyperdimensionalMatrixCore;

namespace CalamityLegendsComeBack.BossAI.HDMC
{
    /// <summary>
    /// 超维矩阵主宰——高维矩阵核心的母体。
    /// 5 阶段程序化线框 Boss：每个阶段解锁新的攻击模组池，
    /// 所有招式改造自武器版模组，弹幕全部为"图案化 + 预警制"，不硬追踪。
    ///
    /// 状态机（ai[0]）：
    ///   0  = 登场展开
    ///   1-30 = 攻击模组，每阶段 6 种（见 HDMCSovereignAttacks）
    ///   89 = 攻击间隙换位
    ///   90 = 阶段转换（重编译）
    ///   95 = 终章·数据奇点（生命 &lt;8% 触发一次）
    /// ai[1] = 当前状态计时器。
    /// </summary>
    public sealed partial class HDMCSovereign : ModNPC
    {
        // ── 状态常量 ──
        internal const int StateIntro      = 0;
        internal const int StateRepos      = 89;
        internal const int StateTransition = 90;
        internal const int StateFinale     = 95;

        internal const int AttackCount = 30;

        // ── 同步字段 ──
        internal int Phase = 1;
        internal int LastAttackA = -1;
        internal int LastAttackB = -1;
        internal bool FinaleUsed;
        internal float HoverSide = 1f;

        internal int State
        {
            get => (int)NPC.ai[0];
            set => NPC.ai[0] = value;
        }

        internal int Timer
        {
            get => (int)NPC.ai[1];
            set => NPC.ai[1] = value;
        }

        /// <summary>各阶段生命阈值：跌破即进入下一阶段。</summary>
        private static readonly float[] PhaseThresholds = { 0.78f, 0.58f, 0.38f, 0.18f };

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.NPCBestiaryDrawModifiers bestiaryData = new() { Hide = true };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, bestiaryData);
            NPCID.Sets.TrailCacheLength[Type] = 8;
            NPCID.Sets.TrailingMode[Type] = 3;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
        }

        public override void SetDefaults()
        {
            NPC.width = 108;
            NPC.height = 108;
            NPC.aiStyle = -1;
            NPC.lifeMax = 22_000;
            NPC.damage = 52;
            NPC.defense = 14;
            NPC.knockBackResist = 0f;
            NPC.value = 1_500_000f;
            NPC.npcSlots = 36f;
            NPC.boss = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.netAlways = true;
            NPC.HitSound = SoundID.NPCHit53;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.Calamity().canBreakPlayerDefense = true;
            NPC.DR_NERD(0.05f);
            Music = MusicID.LunarBoss;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)Phase);
            writer.Write((sbyte)LastAttackA);
            writer.Write((sbyte)LastAttackB);
            writer.Write(FinaleUsed);
            writer.Write(HoverSide);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Phase = reader.ReadByte();
            LastAttackA = reader.ReadSByte();
            LastAttackB = reader.ReadSByte();
            FinaleUsed = reader.ReadBoolean();
            HoverSide = reader.ReadSingle();
        }

        // ──────────────────────────────────────────────────
        // AI 主循环
        // ──────────────────────────────────────────────────

        public override void AI()
        {
            if (!ValidateTarget(out Player target))
            {
                DespawnBehavior();
                return;
            }

            float lifeRatio = NPC.life / (float)NPC.lifeMax;

            // 阶段跃迁检测（登场/转换/终章期间不重复触发）
            int desiredPhase = 1;
            for (int i = 0; i < PhaseThresholds.Length; i++)
            {
                if (lifeRatio < PhaseThresholds[i])
                    desiredPhase = i + 2;
            }
            if (desiredPhase > Phase && State != StateTransition && State != StateIntro && State != StateFinale)
            {
                Phase = desiredPhase;
                SwitchState(StateTransition);
            }

            // 终章触发：生命 <8%，一次性
            if (lifeRatio < 0.08f && !FinaleUsed &&
                State != StateFinale && State != StateTransition && State != StateIntro)
            {
                SwitchState(StateFinale);
            }

            Timer++;

            switch (State)
            {
                case StateIntro:
                    DoIntro(target);
                    break;
                case StateRepos:
                    DoReposition(target);
                    break;
                case StateTransition:
                    DoTransition(target);
                    break;
                case StateFinale:
                    DoFinale(target);
                    break;
                default:
                    ExecuteAttack(State, target);
                    break;
            }

            // 接触伤害只在正常战斗态启用
            NPC.damage = State is StateIntro or StateTransition or StateFinale ? 0 : NPC.defDamage;

            Lighting.AddLight(NPC.Center, HDMCUtil.DataColor(0.25f).ToVector3() * 0.7f);
        }

        private bool ValidateTarget(out Player target)
        {
            if (NPC.target < 0 || NPC.target >= Main.maxPlayers ||
                Main.player[NPC.target].dead || !Main.player[NPC.target].active)
                NPC.TargetClosest();

            target = Main.player[NPC.target];
            return target.active && !target.dead;
        }

        private void DespawnBehavior()
        {
            NPC.velocity.Y -= 0.5f;
            NPC.velocity.X *= 0.97f;
            if (NPC.timeLeft > 60)
                NPC.timeLeft = 60;
        }

        // ──────────────────────────────────────────────────
        // 状态：登场
        // ──────────────────────────────────────────────────

        private void DoIntro(Player target)
        {
            NPC.dontTakeDamage = true;
            NPC.velocity *= 0.92f;

            // 登场瞬间建立数据牢笼边界（中心 = 玩家当前位置，与灾厄克隆体竞技场同技术）
            if (Timer == 1 && Main.netMode != NetmodeID.MultiplayerClient &&
                !AnyProjectile(ModContent.ProjectileType<HDMCArena>()))
            {
                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(), target.Center, Vector2.Zero,
                    ModContent.ProjectileType<HDMCArena>(), 0, 0f, Main.myPlayer, NPC.whoAmI);
            }

            if (Timer == 1 && !Main.dedServ)
                SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndCompileStorm) { Volume = 0.7f }, NPC.Center);

            // 数据汇聚粒子（客户端）
            if (!Main.dedServ && Timer % 3 == 0 && Timer < 130)
            {
                Vector2 offset = Main.rand.NextVector2Unit() * Main.rand.NextFloat(200f, 420f);
                Vector2 pos = NPC.Center + offset;
                CalamityMod.Particles.GeneralParticleHandler.SpawnParticle(
                    new CalamityMod.Particles.GlowOrbParticle(
                        pos, -offset.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(4f, 9f),
                        false, 20, 0.5f, HDMCUtil.DataColor(Main.rand.NextFloat()), true, false, false));
            }

            if (Timer == 140 && !Main.dedServ)
            {
                HDMCUtil.DataBurstParticles(NPC.Center, 26, 14, 10f);
                Color c = HDMCUtil.DataColor(Main.GlobalTimeWrappedHourly * 0.5f);
                CLCBLightingBoltsSystem.Spawn_MatrixGeometryShatter(NPC.Center, c);
                CLCBLightingBoltsSystem.Spawn_MatrixDataBurst(NPC.Center, c, 1.3f);
                HDMCUtil.ScreenShake(NPC.Center, 5f, 1200f);
            }

            if (Timer >= 155)
            {
                NPC.dontTakeDamage = false;
                SwitchState(StateRepos);
            }
        }

        // ──────────────────────────────────────────────────
        // 状态：攻击间隙换位
        // ──────────────────────────────────────────────────

        /// <summary>
        /// 换位时长：P1 保留较长的"呼吸"给教学，越到后期越短——
        /// 消灭"Boss 散步"的空档，是"太简单"最直接的解药。
        /// </summary>
        private int ReposDuration => Phase switch { 1 => 30, 2 => 26, 3 => 22, 4 => 18, _ => 14 };

        private void DoReposition(Player target)
        {
            HoverBesideTarget(target, HoverSide * 430f, -250f, 20f, 20f);

            // 换位底流：P2+ 途中也吐低速环形弹（不追踪，纯占位），让换位不再是免费喘息。
            // 密度随阶段递增：P2 每 12 帧、P5 每 7 帧。
            int stream = Phase switch { <= 1 => 0, 2 => 12, 3 => 10, 4 => 8, _ => 7 };
            if (stream > 0 && Timer % stream == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float a = HDMCUtil.Hash01(NPC.whoAmI * 7 + Timer) * MathHelper.TwoPi;
                Vector2 dir = a.ToRotationVector2();
                SpawnHostile<HDMCLanceHostile>(NPC.Center + dir * 40f, dir * 5.5f, 0.45f, 8.5f, 6f);
            }

            if (Timer >= ReposDuration)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int next = PickNextAttack();
                    LastAttackB = LastAttackA;
                    LastAttackA = next;
                    HoverSide = -HoverSide;
                    SwitchState(next);
                    NPC.netUpdate = true;
                }
            }
        }

        /// <summary>
        /// 当前阶段的攻击模组池——每阶段 6 种互不重复的攻击方式。
        /// P1 校准 → P2 展开 → P3 过载 → P4 临界 → P5 编译风暴。
        /// </summary>
        internal int[] CurrentPool => Phase switch
        {
            1 => new[] { 1, 2, 3, 4, 5, 6 },
            2 => new[] { 7, 8, 9, 10, 11, 12 },
            3 => new[] { 13, 14, 15, 16, 17, 18 },
            4 => new[] { 19, 20, 21, 22, 23, 24 },
            _ => new[] { 25, 26, 27, 28, 29, 30 }
        };

        private int PickNextAttack()
        {
            int[] pool = CurrentPool;
            if (pool.Length == 1)
                return pool[0];

            List<int> candidates = new();
            foreach (int id in pool)
            {
                if (id != LastAttackA && id != LastAttackB)
                    candidates.Add(id);
            }
            if (candidates.Count == 0)
                candidates.AddRange(pool);

            return candidates[Main.rand.Next(candidates.Count)];
        }

        internal void SwitchState(int newState)
        {
            State = newState;
            Timer = 0;
            NPC.netUpdate = true;
        }

        // ──────────────────────────────────────────────────
        // 状态：阶段转换（重编译）
        // ──────────────────────────────────────────────────

        private void DoTransition(Player target)
        {
            NPC.dontTakeDamage = true;
            NPC.velocity *= 0.9f;

            if (Timer == 8 && !Main.dedServ)
            {
                SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndCompileStorm) { Volume = 0.8f }, NPC.Center);
                Color c = HDMCUtil.DataColor(Main.GlobalTimeWrappedHourly * 0.5f);
                CLCBLightingBoltsSystem.Spawn_MatrixGeometryShatter(NPC.Center, c);
                HDMCUtil.ScreenShake(NPC.Center, 4f, 1000f);
            }

            if (Timer == 60 && !Main.dedServ)
            {
                HDMCUtil.DataBurstParticles(NPC.Center, 20, 12, 9f);
                CLCBLightingBoltsSystem.Spawn_MatrixDataBurst(NPC.Center,
                    HDMCUtil.DataColor(0.2f), 1.1f);
            }

            if (Timer >= 115)
            {
                NPC.dontTakeDamage = false;
                SwitchState(StateRepos);
            }
        }

        // ──────────────────────────────────────────────────
        // 状态：终章·数据奇点
        // ──────────────────────────────────────────────────

        private void DoFinale(Player target)
        {
            if (Timer < 55)
            {
                HoverBesideTarget(target, 0f, -380f, 20f, 18f);
            }
            else
            {
                NPC.velocity *= 0.93f;
            }

            if (Timer == 55 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(), NPC.Center + Vector2.UnitY * 180f, Vector2.Zero,
                    ModContent.ProjectileType<HDMCSingularityHostile>(),
                    HDMCUtil.HostileDamage(NPC, 1.2f), 0f, Main.myPlayer);
            }

            if (Timer >= 380)
            {
                FinaleUsed = true;
                SwitchState(StateRepos);
            }
        }

        // ──────────────────────────────────────────────────
        // 移动
        // ──────────────────────────────────────────────────

        /// <summary>悬停到目标旁的锚点：惯性混合 + 距离自适应加速（防拉脱不硬贴脸）。</summary>
        internal void HoverBesideTarget(Player target, float xOff, float yOff, float speed, float inertia)
        {
            float t = Main.GlobalTimeWrappedHourly;
            Vector2 anchor = target.Center + new Vector2(
                xOff + (float)Math.Sin(t * 1.3f + NPC.whoAmI) * 26f,
                yOff + (float)Math.Sin(t * 1.9f) * 18f);

            float dist = Vector2.Distance(NPC.Center, anchor);
            float speedScale = MathHelper.Clamp(dist / 750f, 1f, 2.8f);
            Vector2 dir = (anchor - NPC.Center).SafeNormalize(Vector2.Zero);
            NPC.velocity = (NPC.velocity * (inertia - 1f) + dir * speed * speedScale) / inertia;

            // 距离过近时轻微减速，避免穿模贴脸
            if (dist < 130f)
                NPC.velocity *= 0.9f;
        }

        /// <summary>场上是否已存在指定类型的弹幕（防边界重复生成）。</summary>
        private static bool AnyProjectile(int type)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == type)
                    return true;
            }
            return false;
        }

        /// <summary>敌对弹幕生成快捷方式（仅服务器）。</summary>
        internal int SpawnHostile<T>(Vector2 pos, Vector2 vel, float damageMult, float ai0 = 0f, float ai1 = 0f)
            where T : ModProjectile
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return -1;
            return Projectile.NewProjectile(
                NPC.GetSource_FromAI(), pos, vel,
                ModContent.ProjectileType<T>(),
                HDMCUtil.HostileDamage(NPC, damageMult), 0f, Main.myPlayer, ai0, ai1);
        }

        // ──────────────────────────────────────────────────
        // 受击 / 掉落
        // ──────────────────────────────────────────────────

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (Main.dedServ)
                return;

            HDMCUtil.DataBurstParticles(NPC.Center + Main.rand.NextVector2Circular(40f, 40f), 2, 1, 5f);

            if (NPC.life <= 0)
            {
                HDMCUtil.DataBurstParticles(NPC.Center, 46, 26, 15f);
                Color c = HDMCUtil.DataColor(Main.GlobalTimeWrappedHourly * 0.5f);
                CLCBLightingBoltsSystem.Spawn_MatrixSingularityCollapse(NPC.Center);
                CLCBLightingBoltsSystem.Spawn_MatrixGeometryShatter(NPC.Center, c);
                CLCBLightingBoltsSystem.Spawn_MatrixDataBurst(NPC.Center, c, 1.6f);
                HDMCUtil.ScreenShake(NPC.Center, 9f, 2000f);
                SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndSingularity) { Volume = 0.9f }, NPC.Center);
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<HDMCWeaponItem>()));
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) => 0f;

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => State is not (StateIntro or StateTransition or StateFinale);
    }
}
